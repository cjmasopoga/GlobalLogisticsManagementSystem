namespace Global_Logistics_Management_System.Services
{
    public interface IFileService
    {
        Task<string> SaveSignedAgreementAsync(IFormFile file);
        void ValidatePdfFile(IFormFile file);
    }
}
