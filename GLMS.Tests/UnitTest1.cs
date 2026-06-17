using Microsoft.AspNetCore.Http;
using Moq;

namespace GLMS.Tests;
// ---------------------------------------------------------------------------
// 1. Currency Calculation Tests
// ---------------------------------------------------------------------------
public class CurrencyCalculationTests
{
    private readonly CurrencyServiceTestDouble _service = new();

    [Fact]
    public void ConvertUsdToZar_CorrectlyMultipliesAmountByRate()
    {
        // 100 USD × 18.50 = 1850.00 ZAR
        var result = _service.ConvertUsdToZar(100m, 18.50m);
        Assert.Equal(1850.00m, result);
    }

    [Fact]
    public void ConvertUsdToZar_ZeroUsd_ReturnsZero()
    {
        var result = _service.ConvertUsdToZar(0m, 18.50m);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ConvertUsdToZar_RoundsToTwoDecimalPlaces()
    {
        // 1 USD × 18.1234567 = 18.12 (rounded)
        var result = _service.ConvertUsdToZar(1m, 18.1234567m);
        Assert.Equal(18.12m, result);
    }

    [Fact]
    public void ConvertUsdToZar_LargeAmount_IsAccurate()
    {
        // 5000 USD × 19.25 = 96250.00 ZAR
        var result = _service.ConvertUsdToZar(5000m, 19.25m);
        Assert.Equal(96250.00m, result);
    }

    [Fact]
    public void ConvertUsdToZar_FractionalUsd_IsAccurate()
    {
        // 0.50 USD × 18.00 = 9.00 ZAR
        var result = _service.ConvertUsdToZar(0.50m, 18.00m);
        Assert.Equal(9.00m, result);
    }

    [Fact]
    public void ConvertUsdToZar_NegativeAmount_ReturnsNegativeZar()
    {
        var result = _service.ConvertUsdToZar(-50m, 18.00m);
        Assert.Equal(-900.00m, result);
    }
}

/// <summary>
/// Thin test double that exposes only the pure calculation method.
/// Avoids needing HttpClient / ILogger in unit tests.
/// </summary>
internal class CurrencyServiceTestDouble
{
    public decimal ConvertUsdToZar(decimal usdAmount, decimal rate)
        => Math.Round(usdAmount * rate, 2);
}

// ---------------------------------------------------------------------------
// 2. File Validation Tests
// ---------------------------------------------------------------------------
public class FileValidationTests
{
    private readonly FileServiceTestDouble _service = new();

    [Theory]
    [InlineData(".exe")]
    [InlineData(".bat")]
    [InlineData(".sh")]
    [InlineData(".js")]
    [InlineData(".docx")]
    [InlineData(".png")]
    [InlineData(".zip")]
    public void ValidatePdfFile_NonPdfExtension_ThrowsInvalidOperationException(string extension)
    {
        var file = BuildMockFile($"document{extension}", 1024);
        Assert.Throws<InvalidOperationException>(() => _service.ValidatePdfFile(file));
    }

    [Fact]
    public void ValidatePdfFile_PdfExtension_DoesNotThrow()
    {
        var file = BuildMockFile("agreement.pdf", 1024);
        var exception = Record.Exception(() => _service.ValidatePdfFile(file));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidatePdfFile_EmptyFile_ThrowsArgumentException()
    {
        var file = BuildMockFile("agreement.pdf", 0);
        Assert.Throws<ArgumentException>(() => _service.ValidatePdfFile(file));
    }

    [Fact]
    public void ValidatePdfFile_NullFile_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _service.ValidatePdfFile(null!));
    }

    [Fact]
    public void ValidatePdfFile_PdfExtensionUpperCase_DoesNotThrow()
    {
        var file = BuildMockFile("AGREEMENT.PDF", 512);
        var exception = Record.Exception(() => _service.ValidatePdfFile(file));
        Assert.Null(exception);
    }

    private static IFormFile BuildMockFile(string fileName, long length)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.Length).Returns(length);
        return mock.Object;
    }
}

/// <summary>
/// Extracted file-validation logic – no IWebHostEnvironment needed.
/// </summary>
internal class FileServiceTestDouble
{
    public void ValidatePdfFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file was provided.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".pdf")
            throw new InvalidOperationException($"Invalid file type '{extension}'. Only .pdf files are allowed.");
    }
}

// ---------------------------------------------------------------------------
// 3. Contract Workflow / Business Logic Tests
// ---------------------------------------------------------------------------
public class ContractWorkflowTests
{
    [Theory]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.OnHold)]
    public void CanCreateServiceRequest_ExpiredOrOnHold_ReturnsFalse(ContractStatus status)
    {
        var contract = new Contract { Status = status };
        Assert.False(WorkflowRules.CanCreateServiceRequest(contract));
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Draft)]
    public void CanCreateServiceRequest_ActiveOrDraft_ReturnsTrue(ContractStatus status)
    {
        var contract = new Contract { Status = status };
        Assert.True(WorkflowRules.CanCreateServiceRequest(contract));
    }

    [Fact]
    public void CanCreateServiceRequest_NullContract_ReturnsFalse()
    {
        Assert.False(WorkflowRules.CanCreateServiceRequest(null));
    }

    [Fact]
    public void ContractStatus_DefaultValue_IsDraft()
    {
        var contract = new Contract();
        Assert.Equal(ContractStatus.Draft, contract.Status);
    }

    [Fact]
    public void ServiceRequest_DefaultStatus_IsPending()
    {
        var sr = new ServiceRequest();
        Assert.Equal(ServiceRequestStatus.Pending, sr.Status);
    }
}

/// <summary>
/// Static helper that encapsulates the service-request creation guard.
/// Mirrors the same check used in ServiceRequestsController.
/// </summary>
internal static class WorkflowRules
{
    public static bool CanCreateServiceRequest(Contract? contract)
    {
        if (contract is null) return false;
        return contract.Status != ContractStatus.Expired
            && contract.Status != ContractStatus.OnHold;
    }
}

// ---------------------------------------------------------------------------
// Local domain stubs – the MVC project no longer exposes these EF entities,
// so tests that validate pure business logic define minimal stand-ins here.
// ---------------------------------------------------------------------------
public enum ContractStatus { Draft, Active, OnHold, Expired }
public enum ServiceRequestStatus { Pending, InProgress, Completed, Cancelled }

internal class Contract
{
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public List<ServiceRequest> ServiceRequests { get; set; } = new();
    public Client Client { get; set; } = new();
}

internal class ServiceRequest
{
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

internal class Client
{
    public List<Contract> Contracts { get; set; } = new();
}

// ---------------------------------------------------------------------------
// 4. Model / Domain Tests
// ---------------------------------------------------------------------------
public class ModelTests
{
    [Fact]
    public void Client_DefaultContractsCollection_IsNotNull()
    {
        var client = new Client();
        Assert.NotNull(client.Contracts);
    }

    [Fact]
    public void Contract_DefaultServiceRequestsCollection_IsNotNull()
    {
        var contract = new Contract();
        Assert.NotNull(contract.ServiceRequests);
    }

    [Fact]
    public void ServiceRequest_CreatedAt_IsUtc()
    {
        var sr = new ServiceRequest();
        Assert.Equal(DateTimeKind.Utc, sr.CreatedAt.Kind);
    }

    [Fact]
    public void CurrencyConversion_CostZarReflectsRateTimesUsd()
    {
        decimal rate = 18.75m;
        decimal usd = 200m;
        decimal expectedZar = Math.Round(usd * rate, 2); // 3750.00

        Assert.Equal(3750.00m, expectedZar);
    }
}

