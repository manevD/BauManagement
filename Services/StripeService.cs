using BauManagement.Configuration;
using BauManagement.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace BauManagement.Services;

public class StripeService
{
    private readonly StripeOptions _options;
    private readonly NavigationManager _navigationManager;

    public StripeService(
        IOptions<StripeOptions> options,
        NavigationManager navigationManager)
    {
        _options = options.Value;
        _navigationManager = navigationManager;
    }

    // ============================================================
    // PRICE ID
    // ============================================================

    public string GetPriceId(SubscriptionPlan plan)
    {
        return plan switch
        {
            SubscriptionPlan.S => _options.PriceS,
            SubscriptionPlan.M => _options.PriceM,
            SubscriptionPlan.L => _options.PriceL,
            SubscriptionPlan.XL => _options.PriceXL,

            _ => throw new ArgumentOutOfRangeException(
                nameof(plan),
                plan,
                "Ungültiger Stripe-Plan.")
        };
    }

    // ============================================================
    // CREATE CUSTOMER
    // ============================================================

    public async Task<Customer> CreateCustomerAsync(
        Company company,
        string email)
    {
        if (company is null)
            throw new ArgumentNullException(nameof(company));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "E-Mail-Adresse fehlt.",
                nameof(email));

        var customerService = new CustomerService();

        var options = new CustomerCreateOptions
        {
            Email = email.Trim(),
            Name = company.Name,
            Phone = company.Phone,

            Address =
                string.IsNullOrWhiteSpace(company.Address)
                && string.IsNullOrWhiteSpace(company.City)
                && string.IsNullOrWhiteSpace(company.PostalCode)
                    ? null
                    : new AddressOptions
                    {
                        Line1 = company.Address,
                        PostalCode = company.PostalCode,
                        City = company.City,
                        Country = "DE"
                    },

            Metadata = new Dictionary<string, string>
            {
                { "CompanyId", company.Id.ToString() },
                { "SubscriptionPlan", company.SubscriptionPlan.ToString() }
            }
        };

        return await customerService.CreateAsync(options);
    }

    // ============================================================
    // CREATE CHECKOUT SESSION
    // ============================================================

    public async Task<Session> CreateCheckoutSessionAsync(
        Company company,
        string customerId,
        SubscriptionPlan plan)
    {
        if (company is null)
            throw new ArgumentNullException(nameof(company));

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException(
                "Stripe Customer ID fehlt.",
                nameof(customerId));

        var priceId = GetPriceId(plan);

        if (string.IsNullOrWhiteSpace(priceId))
        {
            throw new InvalidOperationException(
                $"Für den Plan {plan} wurde keine Stripe Price ID konfiguriert.");
        }

        var baseUrl = _navigationManager.BaseUri.TrimEnd('/');

        var successUrl =
            $"{baseUrl}/company" +
            $"?payment=success" +
            $"&session_id={{CHECKOUT_SESSION_ID}}";

        var cancelUrl =
            $"{baseUrl}/company" +
            $"?payment=cancel";

        var metadata = new Dictionary<string, string>
        {
            { "CompanyId", company.Id.ToString() },
            { "SubscriptionPlan", plan.ToString() }
        };

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = priceId,
                    Quantity = 1
                }
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = metadata
            },
            Metadata = metadata,
            BillingAddressCollection = "required",
            AllowPromotionCodes = true
        };

        var sessionService = new SessionService();
        return await sessionService.CreateAsync(options);
    }

    public Task<Session> CreateCheckoutSessionAsync(
        Company company,
        string customerId)
    {
        return CreateCheckoutSessionAsync(
            company,
            customerId,
            company.SubscriptionPlan);
    }

    // ============================================================
    // VERIFY / SYNC SUCCESSFUL CHECKOUT
    // ============================================================

    public async Task<bool> SyncCheckoutSessionAsync(
        string sessionId,
        Company company)
    {
        if (company is null)
            throw new ArgumentNullException(nameof(company));

        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        var sessionService = new SessionService();
        var session = await sessionService.GetAsync(sessionId);

        if (session is null)
            return false;

        // CUSTOMER CHECK
        if (!string.IsNullOrWhiteSpace(company.StripeCustomerId) &&
            !string.Equals(
                company.StripeCustomerId,
                session.CustomerId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Die Stripe Checkout Session gehört nicht zu diesem Kunden.");
        }

        // PAYMENT STATUS CHECK
        if (!string.Equals(session.Status, "complete", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(session.SubscriptionId))
            return false;

        var subscriptionService = new Stripe.SubscriptionService();
        var subscription = await subscriptionService.GetAsync(session.SubscriptionId);

        if (subscription is null)
            return false;

        // AŽURIRANJE PODATAKA O KOMPANIJI
        company.StripeCustomerId = subscription.CustomerId;
        company.StripeSubscriptionId = subscription.Id;

        var item = subscription.Items.Data.FirstOrDefault();

        if (item is not null)
        {
            company.StripePriceId = item.Price.Id;
            company.SubscriptionPlan = GetPlanFromPrice(item.Price.Id);
            company.CurrentPeriodEnd = item.CurrentPeriodEnd;
        }

        if (subscription.TrialEnd.HasValue)
        {
            company.TrialEndDate = subscription.TrialEnd.Value;
        }

        // KLJUČNA PROMJENA: Pošto je Checkout plaćen i završen, pretplata MORA postati Aktivna
        company.SubscriptionStatus = SubscriptionStatus.Active;
        company.IsSubscriptionActive = true;

        return true;
    }

    // ============================================================
    // CHECKOUT URL
    // ============================================================

    public string GetCheckoutUrl(Session session)
    {
        if (session is null)
            throw new ArgumentNullException(nameof(session));

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException(
                "Stripe Checkout URL wurde nicht erstellt.");
        }

        return session.Url;
    }

    // ============================================================
    // CHANGE SUBSCRIPTION PLAN
    // ============================================================

    public async Task ChangeSubscriptionPlanAsync(
        Company company,
        SubscriptionPlan newPlan)
    {
        if (company is null)
            throw new ArgumentNullException(nameof(company));

        if (string.IsNullOrWhiteSpace(company.StripeSubscriptionId))
        {
            throw new InvalidOperationException(
                "Für diese Firma wurde kein Stripe-Abonnement gefunden.");
        }

        var newPriceId = GetPriceId(newPlan);

        if (string.IsNullOrWhiteSpace(newPriceId))
        {
            throw new InvalidOperationException(
                $"Für den Plan {newPlan} wurde keine Stripe Price ID konfiguriert.");
        }

        var subscriptionService = new Stripe.SubscriptionService();
        var subscription = await subscriptionService.GetAsync(company.StripeSubscriptionId);

        var item = subscription.Items.Data.FirstOrDefault();

        if (item is null)
        {
            throw new InvalidOperationException(
                "Das Stripe-Abonnement enthält kein Subscription-Item.");
        }

        var options = new SubscriptionUpdateOptions
        {
            Items = new List<SubscriptionItemOptions>
            {
                new()
                {
                    Id = item.Id,
                    Price = newPriceId
                }
            },
            ProrationBehavior = "always_invoice"
        };

        await subscriptionService.UpdateAsync(company.StripeSubscriptionId, options);
    }

    // ============================================================
    // CANCEL / RESUME SUBSCRIPTION
    // ============================================================

    public async Task CancelSubscriptionAsync(Company company)
    {
        if (company is null)
            throw new ArgumentNullException(nameof(company));

        if (string.IsNullOrWhiteSpace(company.StripeSubscriptionId))
            return;

        var subscriptionService = new Stripe.SubscriptionService();
        var options = new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = true
        };

        await subscriptionService.UpdateAsync(company.StripeSubscriptionId, options);
    }

    public async Task ResumeSubscriptionAsync(Company company)
    {
        if (company is null)
            throw new ArgumentNullException(nameof(company));

        if (string.IsNullOrWhiteSpace(company.StripeSubscriptionId))
        {
            throw new InvalidOperationException(
                "Für diese Firma wurde kein Stripe-Abonnement gefunden.");
        }

        var subscriptionService = new Stripe.SubscriptionService();
        var options = new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = false
        };

        await subscriptionService.UpdateAsync(company.StripeSubscriptionId, options);
    }
    public async Task<bool> SyncCompanySubscriptionAsync(Company company)
    {
        if (company is null) return false;

        var subscriptionService = new Stripe.SubscriptionService();
        Stripe.Subscription? subscription = null;

        // 1. Ako ima SubscriptionId, zemi go direktno
        if (!string.IsNullOrWhiteSpace(company.StripeSubscriptionId))
        {
            subscription = await subscriptionService.GetAsync(company.StripeSubscriptionId);
        }
        // 2. Ako nema SubscriptionId, no ima CustomerId, najdi ja poslednata pretplata
        else if (!string.IsNullOrWhiteSpace(company.StripeCustomerId))
        {
            var options = new Stripe.SubscriptionListOptions
            {
                Customer = company.StripeCustomerId,
                Limit = 1
            };
            var list = await subscriptionService.ListAsync(options);
            subscription = list.Data.FirstOrDefault();
        }

        if (subscription is null) return false;

        // Ažuriranje na podatocite vo kompanijata
        company.StripeSubscriptionId = subscription.Id;
        company.StripeCustomerId = subscription.CustomerId;

        var item = subscription.Items.Data.FirstOrDefault();
        if (item is not null)
        {
            company.StripePriceId = item.Price.Id;
            company.SubscriptionPlan = GetPlanFromPrice(item.Price.Id);
            company.CurrentPeriodEnd = item.CurrentPeriodEnd;
        }

        // Ako statusot na Stripe e "active" ili "trialing", aktiviraj ja kompanijata
        if (subscription.Status == "active" || subscription.Status == "trialing")
        {
            company.SubscriptionStatus = SubscriptionStatus.Active;
            company.IsSubscriptionActive = true;
        }
        else
        {
            company.SubscriptionStatus = MapSubscriptionStatus(subscription.Status);
            company.IsSubscriptionActive = false;
        }

        return true;
    }
    // ============================================================
    // HELPER METHODS
    // ============================================================

    public SubscriptionPlan GetPlanFromPrice(string priceId)
    {
        if (priceId == _options.PriceS) return SubscriptionPlan.S;
        if (priceId == _options.PriceM) return SubscriptionPlan.M;
        if (priceId == _options.PriceL) return SubscriptionPlan.L;
        if (priceId == _options.PriceXL) return SubscriptionPlan.XL;

        throw new InvalidOperationException($"Unbekannte Stripe Price ID: {priceId}");
    }

    public static SubscriptionStatus MapSubscriptionStatus(string? stripeStatus)
    {
        return stripeStatus switch
        {
            "trialing" => SubscriptionStatus.Trial,
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "unpaid" => SubscriptionStatus.PastDue,
            "incomplete" => SubscriptionStatus.PastDue,
            "incomplete_expired" => SubscriptionStatus.Expired,
            "canceled" => SubscriptionStatus.Cancelled,
            _ => SubscriptionStatus.Expired
        };
    }
}