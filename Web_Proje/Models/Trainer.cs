using System.ComponentModel.DataAnnotations;

namespace Web_Proje.Models
{
    public class Trainer
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Antrenör adı zorunludur.")]
        [Display(Name = "Ad Soyad")]

        public string Name { get; set; }

        [Display(Name = "Salon")]
        public int GymId { get; set; }
        public Gym? Gym { get; set; }

        [Required(ErrorMessage = "Uzmanlık alanı zorunludur.")]
        [Display(Name = "Uzmanlık Alanı")]
        public string Specialty { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Mesai Başlangıç")]
        public TimeSpan ShiftStart { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Mesai Bitiş")]
        public TimeSpan ShiftEnd { get; set; }

        public ICollection<Appointment> Appointments { get; set; }
    }
}
