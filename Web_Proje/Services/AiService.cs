using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace Web_Proje.Services
{
    public class AiService
    {
        private readonly string _apiKey;

        private readonly string _chatUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public AiService(IConfiguration configuration)
        {
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<string> ChatAsync(string prompt, string? imageBase64 = null)
        {
            if (string.IsNullOrEmpty(_apiKey)) return "API Key eksik.";

            using var client = new HttpClient();
            var partsList = new List<object> { new { text = prompt } };

            if (!string.IsNullOrEmpty(imageBase64))
            {
                partsList.Add(new { inline_data = new { mime_type = "image/jpeg", data = imageBase64 } });
            }

            var requestBody = new { contents = new[] { new { parts = partsList } } };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_chatUrl}?key={_apiKey}", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                try
                {
                    return doc.RootElement.GetProperty("candidates")[0]
                        .GetProperty("content").GetProperty("parts")[0]
                        .GetProperty("text").GetString() ?? "";
                }
                catch { return "Cevap okunamadı."; }
            }
            return "Sohbet servisi hatası.";
        }

        public async Task<string> GenerateImageAsync(string prompt)
        {
            using var client = new HttpClient();

            // Pollinations URL'i oluşturuyoruz
            // Prompt'u URL'e uygun hale getiriyoruz
            var encodedPrompt = System.Net.WebUtility.UrlEncode(prompt);
            string url = $"https://image.pollinations.ai/prompt/{encodedPrompt}?model=flux&width=1024&height=1024&nologo=true";

            try
            {
                // Resmi indiriyoruz (Server-Side Download)
                var imageBytes = await client.GetByteArrayAsync(url);

                // Base64'e çevirip geri döndürüyoruz 
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                return $"ERROR: Pollinations Hatası - {ex.Message}";
            }
        }
    }
}