using System.Text.Json;

namespace GLMS.API.Services
{
    public interface ICurrencyService
    {
        Task<decimal> GetUsdToZarRateAsync();
        decimal ConvertUsdToZar(decimal usdAmount, decimal rate);
    }

    public class CurrencyService : ICurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CurrencyService> _logger;
        private const string ApiUrl = "https://open.er-api.com/v6/latest/USD";

        public CurrencyService(HttpClient httpClient, ILogger<CurrencyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<decimal> GetUsdToZarRateAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(ApiUrl);
                using var doc = JsonDocument.Parse(response);
                return doc.RootElement.GetProperty("rates").GetProperty("ZAR").GetDecimal();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch USD-ZAR rate. Using fallback.");
                return 18.50m;
            }
        }

        public decimal ConvertUsdToZar(decimal usdAmount, decimal rate) =>
            Math.Round(usdAmount * rate, 2);
    }
}
