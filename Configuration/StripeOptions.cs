
namespace BauManagement.Configuration;

public class StripeOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    public string PriceS { get; set; } = "price_1UJbX43hKrvDbUq7C2iyGkYg";
    public string PriceM { get; set; } = "price_1UJbXR3hKrvDbUq715RTpuqL";
    public string PriceL { get; set; } = "price_1UJbXj3hKrvDbUq7fxU5ovfn";
    public string PriceXL { get; set; } = "price_1UJbYR3hKrvDbUq7NcXwhPtj";
}
