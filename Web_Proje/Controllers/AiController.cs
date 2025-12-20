using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_Proje.Models;
using Web_Proje.Services;

namespace Web_Proje.Controllers
{
    public class AiController : Controller
    {
        private readonly GymContext _context;
        private readonly AiService _aiService;

        public AiController(GymContext context, AiService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        public class ChatRequest
        {
            public string Message { get; set; }
            public string? ImageBase64 { get; set; }
            public string? Age { get; set; }
            public string? Height { get; set; }
            public string? Weight { get; set; }
            public string? Goal { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (request == null || (string.IsNullOrWhiteSpace(request.Message) && string.IsNullOrEmpty(request.ImageBase64)))
                return BadRequest();

            string finalReplyText = "";
            string? generatedImage = null;
            string imageStatusNote = "";

            string msgLower = request.Message?.ToLower() ?? "";

            
            bool wantsImage = msgLower.Contains("çiz") || msgLower.Contains("oluştur") || msgLower.Contains("foto") || msgLower.Contains("hayal et");

            if (wantsImage)
            {
                // Prompt Hazırla
                string userDetails = $"Male, {request.Age} years old, {request.Height}cm height, {request.Weight}kg weight.";
                string promptInstruction = $"Create a detailed English image prompt for: '{request.Message}'. Include these physical traits: {userDetails}. Start with 'A photorealistic shot of...'. Output ONLY the prompt.";

                string englishPrompt = await _aiService.ChatAsync(promptInstruction, request.ImageBase64);

                // Resmi Çiz 
                string result = await _aiService.GenerateImageAsync(englishPrompt);

                if (!string.IsNullOrEmpty(result) && !result.StartsWith("ERROR"))
                {
                    generatedImage = $"data:image/jpeg;base64,{result}";
                    imageStatusNote = "(SİSTEM NOTU: İstenen görsel oluşturuldu. Cevabında görselden bahset.)";
                }
                else
                {
                    imageStatusNote = $"(SİSTEM NOTU: Görsel oluşturulamadı. Hata: {result})";
                }
            }

            

            // Hizmetleri Çek
            string serviceList = "Genel Fitness";
            if (_context.Services != null)
            {
                var services = await _context.Services.ToListAsync();
                serviceList = string.Join(", ", services.Select(s => s.Name));
            }

            
            //Eğitmenleri Çek 
            string trainerList = "Bilgi yok";

            if (_context.Trainers != null)
            {
                
                var trainers = await _context.Trainers
                                             .Include(t => t.TrainerServices)       
                                             .ThenInclude(ts => ts.service)        
                                             .ToListAsync();

               
                
                trainerList = string.Join("; ", trainers.Select(t =>
                {
                    // Hocanın verdiği tüm hizmetlerin isimlerini virgülle birleştir
                    var skills = t.TrainerServices != null && t.TrainerServices.Any()
                                 ? string.Join(", ", t.TrainerServices.Select(ts => ts.service?.Name))
                                 : "Genel";

                    return $"{t.Name} (Uzmanlıklar: {skills})";
                }));
            }

            // SOHBET PROMPTU
            string systemPrompt = $@"
                Sen FitLife Spor Salonu'nun yapay zeka koçusun.
                
                SALON BİLGİLERİ:
                - Hizmetler: {serviceList}
                - Eğitmenler (Hocalar): {trainerList}
                
                KULLANICI: {request.Age} yaş, {request.Height}cm, {request.Weight}kg, Hedef: {request.Goal}.
                MESAJ: ""{request.Message}""
                
                {imageStatusNote}
                
                GÖREVLERİN:
                1. Kullanıcıya detaylı bir antrenman/beslenme planı veya cevabı ver.
                2. Eğer kullanıcı belirli bir hizmetle (örn: Pilates, Kick Boks) ilgileniyorsa, EĞİTMENLER listesinden o işin uzmanı olan hocayı mutlaka öner (Örn: 'Bu konuda Ahmet Hoca ile çalışabilirsin').
                3. Motive edici ol.
            ";

            finalReplyText = await _aiService.ChatAsync(systemPrompt, request.ImageBase64);

            return Json(new { reply = finalReplyText, generatedImageUrl = generatedImage });
        }

        public IActionResult Index() => View();
    }
}