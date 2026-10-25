using Microsoft.Extensions.Logging;
using Stripe;

namespace BauManagement.Services;

/// <summary>
/// Loads paid subscription invoices for the Stripe Customer belonging to the current company.
/// This service uses Stripe's official API through Stripe.NET.
/// </summary>
public sealed class StripeInvoiceService
{
    private readonly StripeClient _stripeClient;
    private readonly ILogger<StripeInvoiceService> _logger;

    public StripeInvoiceService(
        IConfiguration configuration,
        ILogger<StripeInvoiceService> logger)
    {
        _logger = logger;

        // Use the same setting as the existing Stripe integration.
        var secretKey = configuration["Stripe:SecretKey"];

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "Stripe:SecretKey is not configured. Configure it in user-secrets or environment variables.");
        }

        _stripeClient = new StripeClient(secretKey);
    }

    /// <summary>
    /// Returns every paid invoice for the supplied Stripe Customer ID.
    /// The caller must obtain this ID from the company associated with the current user,
    /// never directly from a query string or browser input.
    /// </summary>
    public async Task<IReadOnlyList<StripeInvoiceDto>> GetPaidInvoicesAsync(
        string stripeCustomerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stripeCustomerId))
            throw new ArgumentException("Stripe Customer ID is required.", nameof(stripeCustomerId));

        var invoiceService = new InvoiceService(_stripeClient);
        var results = new List<StripeInvoiceDto>();
        string? startingAfter = null;

        try
        {
            while (true)
            {
                var options = new InvoiceListOptions
                {
                    Customer = stripeCustomerId,
                    Status = "paid",
                    Limit = 100,
                    StartingAfter = startingAfter
                };

                var page = await invoiceService.ListAsync(
                    options,
                    requestOptions: null,
                    cancellationToken: cancellationToken);

                foreach (var invoice in page.Data)
                {
                    // Keep only fully paid invoices, regardless of how the API list is returned.
                    if (!string.Equals(invoice.Status, "paid", StringComparison.OrdinalIgnoreCase))
                        continue;

                    results.Add(new StripeInvoiceDto
                    {
                        Id = invoice.Id,
                        Number = invoice.Number,
                        Created = invoice.Created,
                        PaidAt = invoice.StatusTransitions?.PaidAt,
                        AmountPaid = invoice.AmountPaid,
                        Currency = invoice.Currency ?? "eur",
                        InvoicePdf = invoice.InvoicePdf,
                        HostedInvoiceUrl = invoice.HostedInvoiceUrl
                    });
                }

                if (!page.HasMore || page.Data.Count == 0)
                    break;

                startingAfter = page.Data[^1].Id;
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(
                ex,
                "Stripe invoice request failed for Customer {StripeCustomerId}. Stripe error: {StripeError}",
                stripeCustomerId,
                ex.StripeError?.Message);

            throw new InvalidOperationException(
                "Stripe-Rechnungen konnten nicht geladen werden. Bitte prüfe die Serverprotokolle.",
                ex);
        }

        return results
            .OrderByDescending(x => x.PaidAt ?? x.Created)
            .ToList();
    }

    /// <summary>
    /// Converts Stripe's smallest currency unit to a decimal amount.
    /// </summary>
    public static decimal ToMajorCurrencyUnit(long amount, string? currency)
    {
        var zeroDecimalCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga",
            "pyg", "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf"
        };

        return zeroDecimalCurrencies.Contains(currency ?? string.Empty)
            ? amount
            : amount / 100m;
    }
}

public sealed class StripeInvoiceDto
{
    public string Id { get; init; } = string.Empty;
    public string? Number { get; init; }
    public DateTime Created { get; init; }
    public DateTime? PaidAt { get; init; }
    public long AmountPaid { get; init; }
    public string Currency { get; init; } = "eur";
    public string? InvoicePdf { get; init; }
    public string? HostedInvoiceUrl { get; init; }

    public decimal AmountPaidMajor =>
        StripeInvoiceService.ToMajorCurrencyUnit(AmountPaid, Currency);
}
