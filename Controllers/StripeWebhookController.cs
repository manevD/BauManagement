using BauManagement.Configuration;
using BauManagement.Data;
using BauManagement.Models;
using BauManagement.Services;
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

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("Stripe webhook received without signature.");
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(_stripeOptions.WebhookSecret))
        {
            _logger.LogError("Stripe WebhookSecret is missing.");
            return StatusCode(500);
        }

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                _stripeOptions.WebhookSecret);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature.");
            return BadRequest();
        }

        _logger.LogInformation(
            "Stripe webhook received: {EventType} ({EventId})",
            stripeEvent.Type,
            stripeEvent.Id);

        try
        {
            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                    await HandleCheckoutSessionCompleted(stripeEvent);
                    break;

                case EventTypes.CustomerSubscriptionCreated:
                case EventTypes.CustomerSubscriptionUpdated:
                    await HandleSubscription(stripeEvent);
                    break;

                case EventTypes.CustomerSubscriptionDeleted:
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;

                case EventTypes.InvoicePaymentFailed:
                    await HandleInvoicePaymentFailed(stripeEvent);
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
                "Error processing Stripe event {EventType} ({EventId})",
                stripeEvent.Type,
                stripeEvent.Id);

            return StatusCode(500);
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null) return;

        var companyId = GetCompanyIdFromMetadata(session.Metadata);
        if (!companyId.HasValue)
        {
            _logger.LogWarning("Checkout session {SessionId} has no CompanyId metadata.", session.Id);
            return;
        }

        var company = await _db.Companies.FirstOrDefaultAsync(x => x.Id == companyId.Value);
        if (company is null)
        {
            _logger.LogWarning("Company {CompanyId} not found for Checkout session {SessionId}.", companyId.Value, session.Id);
            return;
        }

        if (!string.IsNullOrWhiteSpace(session.CustomerId))
            company.StripeCustomerId = session.CustomerId;

        if (!string.IsNullOrWhiteSpace(session.SubscriptionId))
            company.StripeSubscriptionId = session.SubscriptionId;

        // Ako je checkout završen i plaćen, odmah aktiviraj firmu
        if (string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            company.SubscriptionStatus = SubscriptionStatus.Active;
            company.IsSubscriptionActive = true;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Checkout completed for Company {CompanyId}. Customer={CustomerId}, Subscription={SubscriptionId}",
            company.Id, session.CustomerId, session.SubscriptionId);
    }

    private async Task HandleSubscription(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null) return;

        var company = await FindCompanyAsync(subscription);
        if (company is null)
        {
            _logger.LogWarning(
                "Company not found for Stripe subscription {SubscriptionId}, Customer={CustomerId}.",
                subscription.Id, subscription.CustomerId);
            return;
        }

        company.StripeSubscriptionId = subscription.Id;
        if (!string.IsNullOrWhiteSpace(subscription.CustomerId))
            company.StripeCustomerId = subscription.CustomerId;

        var item = subscription.Items.Data.FirstOrDefault();
        if (item is not null)
        {
            company.StripePriceId = item.Price.Id;
            var plan = GetPlanFromPrice(item.Price.Id);
            if (plan.HasValue)
                company.SubscriptionPlan = plan.Value;

            company.CurrentPeriodEnd = item.CurrentPeriodEnd;
        }

        if (subscription.TrialEnd.HasValue)
            company.TrialEndDate = subscription.TrialEnd.Value;

        company.SubscriptionStatus = StripeService.MapSubscriptionStatus(subscription.Status);
        company.IsSubscriptionActive =
            subscription.Status == "active" ||
            subscription.Status == "trialing";

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Company {CompanyId} synchronized from Stripe subscription {SubscriptionId}. Status={Status}, Price={PriceId}",
            company.Id, subscription.Id, subscription.Status, company.StripePriceId);
    }

    private async Task<Company?> FindCompanyAsync(Subscription subscription)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(x => x.StripeSubscriptionId == subscription.Id);

        if (company is not null)
            return company;

        if (!string.IsNullOrWhiteSpace(subscription.CustomerId))
        {
            company = await _db.Companies
                .FirstOrDefaultAsync(x => x.StripeCustomerId == subscription.CustomerId);
        }

        return company;
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null) return;

        var company = await FindCompanyAsync(subscription);
        if (company is null) return;

        company.IsSubscriptionActive = false;
        company.SubscriptionStatus = SubscriptionStatus.Cancelled;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Subscription {SubscriptionId} ended for Company {CompanyId}.", subscription.Id, company.Id);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice is null || string.IsNullOrWhiteSpace(invoice.CustomerId)) return;

        var company = await _db.Companies.FirstOrDefaultAsync(x => x.StripeCustomerId == invoice.CustomerId);
        if (company is null) return;

        company.IsSubscriptionActive = false;
        company.SubscriptionStatus = SubscriptionStatus.PastDue;

        await _db.SaveChangesAsync();

        _logger.LogWarning("Stripe payment failed for Company {CompanyId}, Customer={CustomerId}.", company.Id, invoice.CustomerId);
    }

    private static Guid? GetCompanyIdFromMetadata(Dictionary<string, string>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue("CompanyId", out var value))
            return null;

        return Guid.TryParse(value, out var id) ? id : null;
    }

    private SubscriptionPlan? GetPlanFromPrice(string? priceId)
    {
        if (string.IsNullOrWhiteSpace(priceId)) return null;

        if (priceId == _stripeOptions.PriceS) return SubscriptionPlan.S;
        if (priceId == _stripeOptions.PriceM) return SubscriptionPlan.M;
        if (priceId == _stripeOptions.PriceL) return SubscriptionPlan.L;
        if (priceId == _stripeOptions.PriceXL) return SubscriptionPlan.XL;

        _logger.LogError("Unknown Stripe Price ID: {PriceId}", priceId);
        return null;
    }
}