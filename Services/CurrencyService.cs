using System.Text.Json;

namespace Global_Logistics_Management_System.Services
{
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
                var rates = doc.RootElement.GetProperty("rates");
                var zarRate = rates.GetProperty("ZAR").GetDecimal();
                return zarRate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve USD-to-ZAR exchange rate. Using fallback rate.");
                return 18.50m; // Fallback rate
            }
        }

        public decimal ConvertUsdToZar(decimal usdAmount, decimal rate)
        {
            return Math.Round(usdAmount * rate, 2);
        }
    }
}
