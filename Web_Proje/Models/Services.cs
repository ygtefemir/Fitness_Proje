using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Proje.Models
{
    public class Services
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hizmet adı zorunludur.")]
        [Display(Name = "Hizmet Adı")]
        public string Name { get; set; }

        [Display(Name = "Süre (Dakika)")]
        public int DurationMin { get; set; }

        [Display(Name = "Ücret")]
        [Column(TypeName = "decimal(18,2)")] // Para birimi formatı
        public decimal Price { get; set; }

        public ICollection<Appointment>? Appointments { get; set; }
    }
}
