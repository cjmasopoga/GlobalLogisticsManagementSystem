using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Global_Logistics_Management_System.Models
{
    public enum ServiceRequestStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }

    public class ServiceRequest
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Contract")]
        public int ContractId { get; set; }

        public Contract? Contract { get; set; }

        [Required, StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Cost (USD)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostUsd { get; set; }

        [Display(Name = "Cost (ZAR)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostZar { get; set; }

        [Display(Name = "Exchange Rate Used")]
        [Column(TypeName = "decimal(18,4)")]
        public decimal ExchangeRateUsed { get; set; }

        [Required]
        public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
