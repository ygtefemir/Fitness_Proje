using System.ComponentModel.DataAnnotations;

namespace Web_Proje.Models
{
    public class Gym
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Salon adı")]
        public string Name { get; set; }

        [Display(Name = "Salon adresi")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Açılış saati")]
        [DataType(DataType.Time)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:hh\\:mm}")]
        public TimeSpan OpeningHour { get; set; }

        [Required]
        [DataType(DataType.Time)]
        [Display(Name = "Kapanış Saati")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:hh\\:mm}")]
        public int ClosingHour { get; set; } 

        public ICollection<Trainer>? Trainers { get; set; }
        public ICollection<Services>? Services { get; set; }
    }
}
