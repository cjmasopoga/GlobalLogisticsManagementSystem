using System.ComponentModel.DataAnnotations;

namespace Global_Logistics_Management_System.Models
{
    public class ContractSearchViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "Start Date From")]
        public DateTime? StartDateFrom { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date To")]
        public DateTime? StartDateTo { get; set; }

        public ContractStatus? Status { get; set; }

        public List<Contract> Results { get; set; } = new List<Contract>();
    }
}
