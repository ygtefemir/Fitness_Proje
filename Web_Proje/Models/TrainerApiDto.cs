using System.Text.Json.Serialization;

namespace Web_Proje.Models
{
    public class TrainerApiDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }


        [JsonPropertyName("specialty")] // Uzmanlık alanı
        public string Specialty { get; set; }

        [JsonPropertyName("gymId")] // Uzmanlık alanı
        public int GymId { get; set; }

        [JsonPropertyName("shiftStart")]
        public TimeSpan ShiftStart { get; set; }

        [JsonPropertyName("shiftEnd")]
        public TimeSpan ShiftEnd { get; set; }

        //Kayıt
        [JsonPropertyName("serviceIds")]
        public List<int> ServiceIds { get; set; } = new List<int>();

        // okuma
        [JsonPropertyName("services")]
        public List<string>? Services { get; set; } = new List<string>();
    }
}
