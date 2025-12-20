using System.ComponentModel.DataAnnotations;

namespace Web_Proje.Models
{
    public class Trainer
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Eğitmen adı zorunludur.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "İsim en az 2 karakter olmalıdır.")]
        [Display(Name = "Ad Soyad")]

        public string Name { get; set; }

        [Display(Name = "Çalıştığı Salon")]
        public int GymId { get; set; }
        public Gym? Gym { get; set; }

        [Required(ErrorMessage = "Uzmanlık alanı belirtilmelidir.")]
        [StringLength(50, ErrorMessage = "Uzmanlık alanı çok uzun.")]
        [Display(Name = "Uzmanlık")]
        public string Specialty { get; set; }

        public ICollection<TrainerService> TrainerServices { get; set; }

        [Required(ErrorMessage = "Mesai başlangıç saati gereklidir.")]
        [DataType(DataType.Time)]
        [Display(Name = "Mesai Başlangıç")]
        public TimeSpan ShiftStart { get; set; }

        [Required(ErrorMessage = "Mesai bitiş saati gereklidir.")]
        [DataType(DataType.Time)]
        [Display(Name = "Mesai Bitiş")]
        public TimeSpan ShiftEnd { get; set; }

        public ICollection<Appointment> Appointments { get; set; }
    }
}
