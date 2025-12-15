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
            public string? Age { get; set; }
            public string? Height { get; set; }
            public string? Weight { get; set; }
            public string? Goal { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Mesaj boş olamaz.");
            }

            //  Salon Verilerini Çek
            var services = await _context.Services.ToListAsync();
            var trainers = await _context.Trainers
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.service)
                .ToListAsync();

            string serviceList = string.Join("\n", services.Select(s => $"- {s.Name} ({s.DurationMin} dk - {s.Price} TL)"));
            string trainerList = string.Join("\n", trainers.Select(t =>
            {
                var tServices = string.Join(", ", t.TrainerServices.Select(ts => ts.service.Name));
                return $"- {t.Name} (Uzmanlık: {t.Specialty}, Verdiği Dersler: {tServices})";
            }));

            // kullanıcı profili oluştur
            string userContext = "";
            if (!string.IsNullOrEmpty(request.Weight) && !string.IsNullOrEmpty(request.Height))
            {
                userContext = $@"
                    [KULLANICI PROFİLİ]
                    - Yaş: {request.Age}
                    - Boy: {request.Height} cm
                    - Kilo: {request.Weight} kg
                    - Hedef: {request.Goal ?? "Belirtilmedi"}
                    * Lütfen cevaplarını bu profile göre özelleştir. (Örn: Kilo vermek istiyorsa kardiyo ağırlıklı konuş).
                ";
            }
            else
            {
                userContext = "[KULLANICI PROFİLİ] Bilinmiyor. Genel cevaplar ver.";
            }

            // prompt
            string prompt = $@"
                Sen 'FitLife Spor Salonu'nun profesyonel ve samimi yapay zeka koçusun.
                
                GÖREVLERİN:
                1. Salonumuzdaki hizmetleri ve hocaları pazarlamak.
                2. Kullanıcıya diyeti ve antrenmanı konusunda tavsiye vermek.
                3. Görsel Üretim Talebi Gelirse: Resim çizemezsin ama BETİMLEME yapabilirsin. 
                   Kullanıcı 'Zayıflayınca nasıl görünürüm?' derse, onu motive edecek şekilde zihinsel bir resim çiz (Örn: '3 ay sonra bel çevren incelmiş, duruşun dikleşmiş olacak...').

                SALON VERİLERİ:
                Hizmetler:
                {serviceList}

                Eğitmenler:
                {trainerList}

                {userContext}

                KULLANICI MESAJI: ""{request.Message}""
                
                Cevabın kısa, net ve motive edici olsun. Emoji kullan.
            ";

            try
            {
                string answer = await _aiService.GetAnswerAsync(prompt);
                return Json(new { reply = answer });
            }
            catch
            {
                return Json(new { reply = "Şu an bağlantıda bir sorun var, ama pes etmek yok! Tekrar dene. 🤖" });
            }
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
