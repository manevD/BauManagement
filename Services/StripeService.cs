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

                        // Deutschland
                        Country = "DE"
                    },

            Metadata = new Dictionary<string, string>
            {
                {
                    "CompanyId",
                    company.Id.ToString()
                },
                {
                    "SubscriptionPlan",
                    company.SubscriptionPlan.ToString()
                }
            }
        };

        return await customerService.CreateAsync(options);
    }

    // ============================================================
    // CREATE CHECKOUT SESSION
    // ============================================================
    //
    // IMPORTANT:
    //
    // This creates a REAL Stripe subscription.
    //
    // The Stripe Price MUST be recurring monthly.
    //
    // Example:
    //
    // S  = 35 € / month
    // M  = 70 € / month
    // L  = 140 € / month
    // XL = 200 € / month
    //
    // There is NO 1-year period here.
    //
    // Stripe automatically charges the customer every month
    // until the subscription is cancelled.
    //
    // ============================================================

    public async Task<Session> CreateCheckoutSessionAsync(
        Company company,
        string customerId,
        SubscriptionPlan plan)
    {
        if (company is null)
        {
            throw new ArgumentNullException(nameof(company));
        }

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new ArgumentException(
                "Stripe Customer ID fehlt.",
                nameof(customerId));
        }

        var priceId = GetPriceId(plan);

        if (string.IsNullOrWhiteSpace(priceId))
        {
            throw new InvalidOperationException(
                $"Für den Plan {plan} wurde keine Stripe Price ID konfiguriert.");
        }

        var baseUrl =
            _navigationManager.BaseUri.TrimEnd('/');

        var successUrl =
            $"{baseUrl}/stripe/success" +
            $"?session_id={{CHECKOUT_SESSION_ID}}" +
            $"&companyId={company.Id}";

        var cancelUrl =
            $"{baseUrl}/stripe/cancel" +
            $"?companyId={company.Id}";

        var metadata =
            new Dictionary<string, string>
            {
                {
                    "CompanyId",
                    company.Id.ToString()
                },
                {
                    "SubscriptionPlan",
                    plan.ToString()
                }
            };

        var options = new SessionCreateOptions
        {
            // VERY IMPORTANT
            // This must be "subscription".
            Mode = "subscription",

            // Existing Stripe Customer
            Customer = customerId,

            SuccessUrl = successUrl,

            CancelUrl = cancelUrl,

            // ====================================================
            // MONTHLY RECURRING PRICE
            // ====================================================

            LineItems =
                new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },

            // ====================================================
            // SUBSCRIPTION METADATA
            // ====================================================

            SubscriptionData =
                new SessionSubscriptionDataOptions
                {
                    Metadata = metadata
                },

            // ====================================================
            // CHECKOUT METADATA
            // ====================================================

            Metadata = metadata,

            // Customer must provide billing address
            BillingAddressCollection = "required",

            // Allow Stripe promotion codes
            AllowPromotionCodes = true
        };

        var sessionService =
            new SessionService();

        return await sessionService.CreateAsync(options);
    }

    // ============================================================
    // COMPATIBILITY OVERLOAD
    // ============================================================

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
    // CHECKOUT URL
    // ============================================================

    public string GetCheckoutUrl(Session session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

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
    //
    // Example:
    //
    // S 35 € -> M 70 €
    //
    // Stripe changes the recurring Price.
    //
    // ============================================================

    public async Task ChangeSubscriptionPlanAsync(
        Company company,
        SubscriptionPlan newPlan)
    {
        if (company is null)
        {
            throw new ArgumentNullException(nameof(company));
        }

        if (string.IsNullOrWhiteSpace(
                company.StripeSubscriptionId))
        {
            throw new InvalidOperationException(
                "Für diese Firma wurde kein Stripe-Abonnement gefunden.");
        }

        var newPriceId =
            GetPriceId(newPlan);

        if (string.IsNullOrWhiteSpace(newPriceId))
        {
            throw new InvalidOperationException(
                $"Für den Plan {newPlan} wurde keine Stripe Price ID konfiguriert.");
        }

        var subscriptionService =
            new Stripe.SubscriptionService();

        var subscription =
            await subscriptionService.GetAsync(
                company.StripeSubscriptionId);

        var item =
            subscription.Items.Data.FirstOrDefault();

        if (item is null)
        {
            throw new InvalidOperationException(
                "Das Stripe-Abonnement enthält kein Subscription-Item.");
        }

        var options =
            new SubscriptionUpdateOptions
            {
                Items =
                    new List<SubscriptionItemOptions>
                    {
                        new()
                        {
                            Id = item.Id,
                            Price = newPriceId
                        }
                    },

                // Stripe calculates the difference
                // and creates the appropriate invoice.
                ProrationBehavior = "always_invoice"
            };

        await subscriptionService.UpdateAsync(
            company.StripeSubscriptionId,
            options);
    }

    // ============================================================
    // CANCEL SUBSCRIPTION
    // ============================================================
    //
    // IMPORTANT:
    //
    // We do NOT immediately delete the subscription.
    //
    // Instead:
    //
    // CancelAtPeriodEnd = true
    //
    // This means:
    //
    // Customer paid for the current month
    //          ↓
    // Customer clicks "Kündigen"
    //          ↓
    // No next monthly payment
    //          ↓
    // Customer keeps access until CurrentPeriodEnd
    //          ↓
    // Subscription ends automatically
    //
    // ============================================================

    public async Task CancelSubscriptionAsync(
        Company company)
    {
        if (company is null)
        {
            throw new ArgumentNullException(nameof(company));
        }

        if (string.IsNullOrWhiteSpace(
                company.StripeSubscriptionId))
        {
            return;
        }

        var subscriptionService =
            new Stripe.SubscriptionService();

        var options =
            new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            };

        await subscriptionService.UpdateAsync(
            company.StripeSubscriptionId,
            options);
    }

    // ============================================================
    // RESUME CANCELLED SUBSCRIPTION
    // ============================================================
    //
    // If the customer cancelled at period end but the current
    // paid period has not ended yet, the subscription can be
    // reactivated.
    //
    // ============================================================

    public async Task ResumeSubscriptionAsync(
        Company company)
    {
        if (company is null)
        {
            throw new ArgumentNullException(nameof(company));
        }

        if (string.IsNullOrWhiteSpace(
                company.StripeSubscriptionId))
        {
            throw new InvalidOperationException(
                "Für diese Firma wurde kein Stripe-Abonnement gefunden.");
        }

        var subscriptionService =
            new Stripe.SubscriptionService();

        var options =
            new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = false
            };

        await subscriptionService.UpdateAsync(
            company.StripeSubscriptionId,
            options);
    }
}