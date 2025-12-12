using System.ComponentModel.DataAnnotations;

namespace Web_Proje.Models.ViewModels
{
    public class TrainerViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Specialty { get; set; } //kaldırılabilir

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan ShiftStart { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan ShiftEnd { get; set; }

        public int GymId { get; set; }  

        public List<AssignedServiceData> Services = new List<AssignedServiceData>();

        public class AssignedServiceData
        {
            public int ServiceId { get; set; }
            public string ServiceName { get; set; }
            public bool Assigned { get; set; }
        }
    }
}
