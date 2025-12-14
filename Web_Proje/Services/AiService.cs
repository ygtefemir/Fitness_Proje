using System.Text;
using System.Text.Json;

namespace Web_Proje.Services
{
    public class AiService
    {
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
        public AiService(IConfiguration configuration) //UserSecrets'dan kullanım için API key alınıyor.
        {
            _apiKey = configuration["Gemini:ApiKey"];
        }

        //prompt için cevap döndüren metot
        public async Task<string> GetAnswerAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return ("Hata: API anahtarı yapılandırılmamış.");
            }

            using var client = new HttpClient();

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        },
                    }
                },
            };
            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_apiUrl}?key={_apiKey}", content);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(responseContent);
                var answer = document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                return answer ?? "Cevap alınamadı.";
            }
            else
            {
                return $"Hata: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
            }
        }
    }
}
