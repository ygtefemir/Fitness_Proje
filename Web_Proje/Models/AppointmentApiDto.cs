namespace Web_Proje.Models
{
    public class AppointmentApiDto
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string GymName { get; set; }
        public string TrainerName { get; set; }
        public string ServiceName { get; set; }
        public string Status { get; set; }
        public bool IsAdminView { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public bool CanCancel { get; set; }
    }
}
