using BauManagement.Configuration;
using BauManagement.Data;
using BauManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace BauManagement.Controllers;

[ApiController]
[Route("api/stripe/webhook")]
public class StripeWebhookController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        ApplicationDbContext db,
        IOptions<StripeOptions> stripeOptions,
        ILogger<StripeWebhookController> logger)
    {
        _db = db;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }


    // ============================================================
    // WEBHOOK
    // ============================================================

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandleWebhook()
    {
        var json =
            await new StreamReader(
                HttpContext.Request.Body)
            .ReadToEndAsync();

        var signature =
            Request.Headers["Stripe-Signature"]
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning(
                "Stripe webhook received without signature.");

            return BadRequest();
        }


        Event stripeEvent;

        try
        {
            stripeEvent =
                EventUtility.ConstructEvent(
                    json,
                    signature,
                    _stripeOptions.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid Stripe webhook signature.");

            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while constructing Stripe webhook event.");

            return BadRequest();
        }


        _logger.LogInformation(
            "Stripe webhook received: {EventType}",
            stripeEvent.Type);


        try
        {
            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:

                    await HandleCheckoutSessionCompleted(
                        stripeEvent);

                    break;


                case EventTypes.CustomerSubscriptionCreated:

                    await HandleSubscriptionCreated(
                        stripeEvent);

                    break;


                case EventTypes.CustomerSubscriptionUpdated:

                    await HandleSubscriptionUpdated(
                        stripeEvent);

                    break;


                case EventTypes.CustomerSubscriptionDeleted:

                    await HandleSubscriptionDeleted(
                        stripeEvent);

                    break;


                case EventTypes.InvoicePaymentFailed:

                    await HandleInvoicePaymentFailed(
                        stripeEvent);

                    break;


                default:

                    _logger.LogInformation(
                        "Stripe event ignored: {EventType}",
                        stripeEvent.Type);

                    break;
            }


            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing Stripe event {EventType}",
                stripeEvent.Type);

            return StatusCode(500);
        }
    }


    // ============================================================
    // CHECKOUT COMPLETED
    // ============================================================

    private async Task HandleCheckoutSessionCompleted(
        Event stripeEvent)
    {
        var session =
            stripeEvent.Data.Object
                as Session;

        if (session is null)
            return;


        var companyId =
            GetCompanyIdFromMetadata(
                session.Metadata);


        if (companyId is null)
        {
            _logger.LogWarning(
                "Checkout session {SessionId} has no CompanyId.",
                session.Id);

            return;
        }


        var company =
            await _db.Companies
                .FirstOrDefaultAsync(
                    x => x.Id == companyId.Value);


        if (company is null)
        {
            _logger.LogWarning(
                "Company {CompanyId} not found.",
                companyId);

            return;
        }


        if (!string.IsNullOrWhiteSpace(
                session.CustomerId))
        {
            company.StripeCustomerId =
                session.CustomerId;
        }


        if (!string.IsNullOrWhiteSpace(
                session.SubscriptionId))
        {
            company.StripeSubscriptionId =
                session.SubscriptionId;
        }


        /*
         * Checkout hat einen 7-Tage-Trial.
         *
         * Deshalb hier NICHT direkt Active setzen.
         * customer.subscription.created / updated
         * liefert den tatsächlichen Stripe-Status.
         */

        company.IsSubscriptionActive = true;

        company.SubscriptionStatus =
            SubscriptionStatus.Trial;


        await _db.SaveChangesAsync();


        _logger.LogInformation(
            "Checkout completed for Company {CompanyId}.",
            company.Id);
    }


    // ============================================================
    // SUBSCRIPTION CREATED
    // ============================================================

    private async Task HandleSubscriptionCreated(
        Event stripeEvent)
    {
        var subscription =
            stripeEvent.Data.Object
                as Subscription;

        if (subscription is null)
            return;


        await UpdateCompanyFromSubscription(
            subscription);
    }


    // ============================================================
    // SUBSCRIPTION UPDATED
    // ============================================================

    private async Task HandleSubscriptionUpdated(
        Event stripeEvent)
    {
        var subscription =
            stripeEvent.Data.Object
                as Subscription;

        if (subscription is null)
            return;


        await UpdateCompanyFromSubscription(
            subscription);
    }


    // ============================================================
    // UPDATE COMPANY FROM STRIPE SUBSCRIPTION
    // ============================================================

    private async Task UpdateCompanyFromSubscription(
        Subscription subscription)
    {
        var company =
            await _db.Companies
                .FirstOrDefaultAsync(
                    x =>
                        x.StripeSubscriptionId ==
                        subscription.Id);


        /*
         * Falls SubscriptionId noch nicht gespeichert wurde,
         * versuchen wir über CustomerId zu finden.
         */

        if (company is null &&
            !string.IsNullOrWhiteSpace(
                subscription.CustomerId))
        {
            company =
                await _db.Companies
                    .FirstOrDefaultAsync(
                        x =>
                            x.StripeCustomerId ==
                            subscription.CustomerId);
        }


        if (company is null)
        {
            _logger.LogWarning(
                "Company not found for Stripe subscription {SubscriptionId}.",
                subscription.Id);

            return;
        }


        company.StripeSubscriptionId =
            subscription.Id;


        company.StripeCustomerId =
            subscription.CustomerId;


        // ========================================================
        // PRICE / PLAN
        // ========================================================
        // ========================================================
        // PRICE / PLAN / PERIOD
        // ========================================================

        var item =
            subscription.Items.Data.FirstOrDefault();

        if (item is not null)
        {
            company.StripePriceId =
                item.Price.Id;

            company.SubscriptionPlan =
                GetPlanFromPrice(item.Price.Id);

            company.CurrentPeriodEnd =
                item.CurrentPeriodEnd;
        }


        // ========================================================
        // TRIAL
        // ========================================================

        if (subscription.TrialEnd.HasValue)
        {
            company.TrialEndDate =
                subscription.TrialEnd.Value;
        }


        // ========================================================
        // STATUS
        // ========================================================

        company.SubscriptionStatus =
            MapSubscriptionStatus(
                subscription.Status);


        company.IsSubscriptionActive =
            subscription.Status == "trialing"
            ||
            subscription.Status == "active";


        await _db.SaveChangesAsync();


        _logger.LogInformation(
            "Company {CompanyId} updated from Stripe subscription {SubscriptionId}. Status: {Status}",
            company.Id,
            subscription.Id,
            subscription.Status);
    }


    // ============================================================
    // SUBSCRIPTION DELETED
    // ============================================================

    private async Task HandleSubscriptionDeleted(
        Event stripeEvent)
    {
        var subscription =
            stripeEvent.Data.Object
                as Subscription;

        if (subscription is null)
            return;


        var company =
            await _db.Companies
                .FirstOrDefaultAsync(
                    x =>
                        x.StripeSubscriptionId ==
                        subscription.Id);


        if (company is null)
            return;


        company.IsSubscriptionActive =
            false;


        company.SubscriptionStatus =
            SubscriptionStatus.Cancelled;


        await _db.SaveChangesAsync();


        _logger.LogInformation(
            "Subscription {SubscriptionId} cancelled for Company {CompanyId}.",
            subscription.Id,
            company.Id);
    }


    // ============================================================
    // PAYMENT FAILED
    // ============================================================

    private async Task HandleInvoicePaymentFailed(
        Event stripeEvent)
    {
        var invoice =
            stripeEvent.Data.Object
                as Invoice;

        if (invoice is null)
            return;


        var company =
            await _db.Companies
                .FirstOrDefaultAsync(
                    x =>
                        x.StripeCustomerId ==
                        invoice.CustomerId);


        if (company is null)
        {
            _logger.LogWarning(
                "Company not found for Stripe customer {CustomerId}.",
                invoice.CustomerId);

            return;
        }


        company.IsSubscriptionActive =
            false;


        company.SubscriptionStatus =
            SubscriptionStatus.PastDue;


        await _db.SaveChangesAsync();


        _logger.LogWarning(
            "Payment failed for Company {CompanyId}.",
            company.Id);
    }


    // ============================================================
    // METADATA → COMPANY ID
    // ============================================================

    private static Guid? GetCompanyIdFromMetadata(
        Dictionary<string, string>? metadata)
    {
        if (metadata is null)
            return null;


        if (!metadata.TryGetValue(
                "CompanyId",
                out var companyIdValue))
        {
            return null;
        }


        if (Guid.TryParse(
                companyIdValue,
                out var companyId))
        {
            return companyId;
        }


        return null;
    }


    // ============================================================
    // PRICE → PLAN
    // ============================================================

    private SubscriptionPlan GetPlanFromPrice(
        string priceId)
    {
        if (priceId ==
            _stripeOptions.PriceS)
        {
            return SubscriptionPlan.S;
        }


        if (priceId ==
            _stripeOptions.PriceM)
        {
            return SubscriptionPlan.M;
        }


        if (priceId ==
            _stripeOptions.PriceL)
        {
            return SubscriptionPlan.L;
        }


        if (priceId ==
            _stripeOptions.PriceXL)
        {
            return SubscriptionPlan.XL;
        }


        _logger.LogWarning(
            "Unknown Stripe Price ID: {PriceId}",
            priceId);


        /*
         * Wenn Stripe einen unbekannten Price liefert,
         * behalten wir den bisherigen Plan.
         */
        throw new InvalidOperationException(
            $"Unbekannte Stripe Price ID: {priceId}");
    }


    // ============================================================
    // STRIPE STATUS → OUR STATUS
    // ============================================================

    private static SubscriptionStatus MapSubscriptionStatus(
        string? stripeStatus)
    {
        return stripeStatus switch
        {
            "trialing" =>
                SubscriptionStatus.Trial,

            "active" =>
                SubscriptionStatus.Active,

            "past_due" =>
                SubscriptionStatus.PastDue,

            "canceled" =>
                SubscriptionStatus.Cancelled,

            "unpaid" =>
                SubscriptionStatus.PastDue,

            "incomplete" =>
                SubscriptionStatus.PastDue,

            "incomplete_expired" =>
                SubscriptionStatus.Expired,

            _ =>
                SubscriptionStatus.Expired
        };
    }
}