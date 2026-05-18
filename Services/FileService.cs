namespace Global_Logistics_Management_System.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;
        private const string UploadFolder = "uploads";

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public void ValidatePdfFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was provided.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf")
                throw new InvalidOperationException($"Invalid file type '{extension}'. Only .pdf files are allowed.");
        }

        public async Task<string> SaveSignedAgreementAsync(IFormFile file)
        {
            ValidatePdfFile(file);

            var uploadsPath = Path.Combine(_env.WebRootPath, UploadFolder);
            Directory.CreateDirectory(uploadsPath);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Path.Combine(UploadFolder, uniqueFileName);
        }
    }
}
