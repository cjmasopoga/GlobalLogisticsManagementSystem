namespace GLMS.API.DTOs
{
    public record ClientDto(int Id, string Name, string ContactDetails, string Region, int ContractCount);
    public record CreateClientDto(string Name, string ContactDetails, string Region);

    public record ContractDto(
        int Id, int ClientId, string ClientName,
        DateTime StartDate, DateTime EndDate,
        string Status, string ServiceLevel,
        bool HasSignedAgreement);

    public record CreateContractDto(
        int ClientId, DateTime StartDate, DateTime EndDate,
        string Status, string ServiceLevel);

    public record PatchContractStatusDto(string Status);

    public record ServiceRequestDto(
        int Id, int ContractId, string ContractServiceLevel, string ClientName,
        string Description, decimal CostUsd, decimal CostZar,
        decimal ExchangeRateUsed, string Status, DateTime CreatedAt);

    public record CreateServiceRequestDto(
        int ContractId, string Description, decimal CostUsd, string Status);

    public record LoginDto(string Username, string Password);
    public record TokenDto(string Token, string Username);
}
