using System.ComponentModel.DataAnnotations;

namespace Web_Proje.Models
{
    public class Gym
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Salon adı zorunludur.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Salon adı 3 ile 100 karakter arasında olmalıdır.")]
        [Display(Name = "Salon Adı")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Adres alanı zorunludur.")]
        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir.")]
        [Display(Name = "Açık Adres")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Açılış saati zorunludur.")]
        [Display(Name = "Açılış saati")]
        [DataType(DataType.Time)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:hh\\:mm}")]
        public TimeSpan OpeningHour { get; set; }

        [Required(ErrorMessage = "Kapanış saati zorunludur.")]
        [DataType(DataType.Time)]
        [Display(Name = "Kapanış Saati")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:hh\\:mm}")]
        public TimeSpan ClosingTime { get; set; } 

        public ICollection<Trainer>? Trainers { get; set; }
        public ICollection<Services>? Services { get; set; }
    }
}
