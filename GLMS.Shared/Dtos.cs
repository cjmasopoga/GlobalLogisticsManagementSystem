using System.ComponentModel.DataAnnotations;
using Global_Logistics_Management_System.Models;

namespace Global_Logistics_Management_System.Dtos
{
    // ── Auth ───────────────────────────────────────────────────────────────────

    public class LoginRequest
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
    }

    // ── Clients ────────────────────────────────────────────────────────────────

    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactDetails { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public int ContractCount { get; set; }
    }

    public class CreateClientRequest
    {
        [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
        [Required, StringLength(300)] public string ContactDetails { get; set; } = string.Empty;
        [Required, StringLength(100)] public string Region { get; set; } = string.Empty;
    }

    // ── Contracts ──────────────────────────────────────────────────────────────

    public class ContractDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ContractStatus Status { get; set; }
        public string ServiceLevel { get; set; } = string.Empty;
        public string? SignedAgreementPath { get; set; }
    }

    public class CreateContractRequest
    {
        [Required] public int ClientId { get; set; }
        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }
        public ContractStatus Status { get; set; } = ContractStatus.Draft;
        [Required, StringLength(200)] public string ServiceLevel { get; set; } = string.Empty;
    }

    public class PatchContractStatusRequest
    {
        [Required] public ContractStatus Status { get; set; }
    }

    // ── ServiceRequests ────────────────────────────────────────────────────────

    public class ServiceRequestDto
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public string ContractServiceLevel { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CostUsd { get; set; }
        public decimal CostZar { get; set; }
        public decimal ExchangeRateUsed { get; set; }
        public ServiceRequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateServiceRequestRequest
    {
        [Required] public int ContractId { get; set; }
        [Required, StringLength(1000)] public string Description { get; set; } = string.Empty;
        [Required] public decimal CostUsd { get; set; }
        public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;
    }

    // ── Dashboard ──────────────────────────────────────────────────────────────

    public class DashboardDto
    {
        public int TotalClients { get; set; }
        public int TotalContracts { get; set; }
        public int ActiveContracts { get; set; }
        public int TotalServiceRequests { get; set; }
    }

    // ── Currency ───────────────────────────────────────────────────────────────

    public class ExchangeRateDto
    {
        public decimal Rate { get; set; }
    }
}
