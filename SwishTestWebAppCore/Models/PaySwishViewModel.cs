using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SwishTestWebAppCore.Models
{
    public class EPaySwishViewModel : MPaySwishViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }
    }

    public class MPaySwishViewModel
    {
        [Required]
        [Range(0.0, 999999999999.99)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Message { get; set; }

    }
}
