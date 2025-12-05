using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Proje.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; } //ORM için otomatik ilişki sağlandı ve LINQ işlemleri kolaylaştı

        [Display(Name = "Salon")]
        public int GymId { get; set; }
        public Gym? Gym { get; set; }

        [Required]
        public int TrainerId { get; set; }
        public Trainer? Trainer { get; set; }

        [Required]
        public int ServiceId { get; set; }  
        public Services? Service { get; set; }

        [Required]
        [Display(Name = "Randevu Tarihi ve Saati")]
        public DateTime AppointmentDate { get; set; }

        [Display(Name ="Durum")]
        public Status Status { get; set; } = Status.Pending; //Varsayılan olarak gelen bir randevu durumu beklemedir
    }
}
