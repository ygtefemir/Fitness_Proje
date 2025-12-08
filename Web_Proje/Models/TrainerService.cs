namespace Web_Proje.Models
{
    public class TrainerService
    {
        public int TrainerId { get; set; }
        public Trainer trainer { get; set; }

        public int ServiceId { get; set; }  
        public Services service { get; set; }    
    }
}
