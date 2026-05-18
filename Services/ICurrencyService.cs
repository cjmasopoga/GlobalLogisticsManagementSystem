namespace Global_Logistics_Management_System.Services
{
    public interface ICurrencyService
    {
        Task<decimal> GetUsdToZarRateAsync();
        decimal ConvertUsdToZar(decimal usdAmount, decimal rate);
    }
}
