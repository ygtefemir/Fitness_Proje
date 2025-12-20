using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Proje.Models
{
    public class Services
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Hizmet adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Hizmet adı en fazla 50 karakter olabilir.")]
        [Display(Name = "Hizmet Adı")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Süre alanı zorunludur.")]
        [Range(10, 240, ErrorMessage = "Süre 10 ile 240 dakika arasında olmalıdır.")]
        [Display(Name = "Süre (Dk)")]
        public int DurationMin { get; set; }

        [Required(ErrorMessage = "Fiyat alanı zorunludur.")]
        [Range(0, 50000, ErrorMessage = "Fiyat 0'dan küçük olamaz.")]
        [Display(Name = "Ücret (₺)")] // Para birimi formatı
        public decimal Price { get; set; }

        public ICollection<TrainerService>? TrainerServices { get; set; }

        public ICollection<Appointment>? Appointments { get; set; }
    }
}
