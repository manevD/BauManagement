using System.Text.Json.Serialization;

namespace BauManagement.Services
{
    public sealed class LexwareService
    {
        private readonly HttpClient _http;

        public LexwareService(HttpClient http)
        {
            _http = http;
        }

        public async Task<LexwareVoucherList?> GetInvoicesAsync(
            int page = 0,
            CancellationToken cancellationToken = default)
        {
            var url =
                $"v1/voucherlist?voucherType=invoice,salesinvoice&page={page}&size=100";

            using var response = await _http.GetAsync(
                url, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<LexwareVoucherList>(
                cancellationToken: cancellationToken);
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
