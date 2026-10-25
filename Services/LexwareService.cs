using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BauManagement.Services
{
    public sealed class LexwareService
    {
        private const string ApiBaseUrl = "https://api.lexware.io/";
        private readonly HttpClient _http;
        private readonly ILogger<LexwareService> _logger;

        public LexwareService(
            HttpClient http,
            IConfiguration configuration,
            ILogger<LexwareService> logger)
        {
            _http = http;
            _logger = logger;

            // Expected in appsettings.Production.json:
            // "Lexware": { "ApiKey": "YOUR_API_KEY" }
            var apiKey = configuration["Lexware:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Lexware API key is missing. Configure 'Lexware:ApiKey' in the production configuration.");
            }

            // Ensure requests go to Lexware's API, even if BaseAddress wasn't configured elsewhere.
            _http.BaseAddress ??= new Uri(ApiBaseUrl);
            _http.Timeout = TimeSpan.FromSeconds(30);

            // Set the Bearer token once on this service's HttpClient.
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey.Trim());

            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<LexwareVoucherList?> GetInvoicesAsync(
            int page = 0,
            CancellationToken cancellationToken = default)
        {
            if (page < 0)
                throw new ArgumentOutOfRangeException(nameof(page));

            var url = $"v1/voucherlist?voucherType=invoice,salesinvoice&page={page}&size=100";

            using var response = await _http.GetAsync(url, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Keep the API response in server logs only; never return the API key to the UI.
                _logger.LogError(
                    "Lexware API request failed. StatusCode: {StatusCode}; Response: {ResponseBody}",
                    (int)response.StatusCode,
                    Limit(responseBody, 2000));

                var safeMessage = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Lexware hat den API-Schlüssel nicht akzeptiert (401 Unauthorized). Prüfe den Schlüssel.",
                    HttpStatusCode.Forbidden => "Lexware verweigert den Zugriff (403 Forbidden). Prüfe die API-Berechtigungen.",
                    HttpStatusCode.NotFound => "Der Lexware-Endpunkt wurde nicht gefunden (404). Prüfe API-URL und Endpoint.",
                    HttpStatusCode.TooManyRequests => "Lexware hat zu viele Anfragen erkannt (429). Bitte später erneut versuchen.",
                    _ => $"Lexware API antwortete mit HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). Details stehen im Server-Log."
                };

                throw new HttpRequestException(
                    safeMessage,
                    null,
                    response.StatusCode);
            }

            try
            {
                return JsonSerializer.Deserialize<LexwareVoucherList>(
                    responseBody,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Lexware API returned invalid JSON for voucherlist.");
                throw new InvalidOperationException(
                    "Lexware hat eine unerwartete Antwort zurückgegeben. Details stehen im Server-Log.",
                    ex);
            }
        }

        private static string Limit(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength
                ? value
                : value[..maxLength] + "…";
        }
    }

    public sealed class LexwareVoucherList
    {
        [JsonPropertyName("content")]
        public List<LexwareVoucher> Content { get; set; } = new();
    }

    public sealed class LexwareVoucher
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("voucherType")]
        public string VoucherType { get; set; } = "";

        [JsonPropertyName("voucherNumber")]
        public string? VoucherNumber { get; set; }

        [JsonPropertyName("contactName")]
        public string? ContactName { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("openAmount")]
        public decimal? OpenAmount { get; set; }

        [JsonPropertyName("voucherStatus")]
        public string? VoucherStatus { get; set; }

        [JsonPropertyName("voucherDate")]
        public DateTime? VoucherDate { get; set; }
    }
}
