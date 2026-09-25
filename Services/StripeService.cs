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
        var customerService = new CustomerService();

        var options = new CustomerCreateOptions
        {
            Email = email,
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
    // CREATE CHECKOUT
    // ============================================================

    public async Task<Session> CreateCheckoutSessionAsync(
        Company company,
        string customerId)
    {
        var priceId =
            GetPriceId(company.SubscriptionPlan);

        var baseUrl =
            _navigationManager.BaseUri.TrimEnd('/');

        var successUrl =
            $"{baseUrl}/stripe/success" +
            $"?session_id={{CHECKOUT_SESSION_ID}}" +
            $"&companyId={company.Id}";

        var cancelUrl =
            $"{baseUrl}/stripe/cancel" +
            $"?companyId={company.Id}";

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

            SubscriptionData =
                new SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = 7,

                    Metadata =
                        new Dictionary<string, string>
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
                },

            Metadata =
                new Dictionary<string, string>
                {
                    {
                        "CompanyId",
                        company.Id.ToString()
                    },
                    {
                        "SubscriptionPlan",
                        company.SubscriptionPlan.ToString()
                    }
                },

            BillingAddressCollection = "required",

            AllowPromotionCodes = true
        };

        var service = new SessionService();

        return await service.CreateAsync(options);
    }


    // ============================================================
    // CHECKOUT URL
    // ============================================================

    public string GetCheckoutUrl(Session session)
    {
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
        if (string.IsNullOrWhiteSpace(
                company.StripeSubscriptionId))
        {
            throw new InvalidOperationException(
                "Für diese Firma wurde kein Stripe-Abonnement gefunden.");
        }

        var newPriceId =
            GetPriceId(newPlan);

        var subscriptionService =
            new SubscriptionService();

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

                /*
                 * Stripe berechnet die anteilige
                 * Preisänderung automatisch.
                 */
                ProrationBehavior = "always_invoice"
            };

        await subscriptionService.UpdateAsync(
            company.StripeSubscriptionId,
            options);
    }


    // ============================================================
    // CANCEL SUBSCRIPTION
    // ============================================================

    public async Task CancelSubscriptionAsync(
        Company company)
    {
        if (string.IsNullOrWhiteSpace(
                company.StripeSubscriptionId))
        {
            return;
        }

        var subscriptionService =
            new SubscriptionService();

        await subscriptionService.CancelAsync(
            company.StripeSubscriptionId);
    }
}