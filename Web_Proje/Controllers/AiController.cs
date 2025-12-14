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
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Bir şeyler yazın. ");
            }
            //Veritabanından hizmetleri ve antretörleri al
            var services = await _context.Services.ToListAsync();
            var trainers = await _context.Trainers
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.service)
                .ToListAsync();

            //Listeleri biçimlendir
            string serviceList = string.Join("\n", services.Select(s => $"- {s.Name} (Fiyat: {s.Price} TL)"));
            string trainerList = string.Join("\n", trainers.Select(t =>
            {
                //antrenörün derslerini birleştir
                var trainerServices = string.Join(", ", t.TrainerServices.Select(ts => ts.service.Name));
                return $"- {t.Name} (Hizmetler: {trainerServices})";
            }));

            string prompt = $@"
                Sen 'FitLife Spor Salonu' asistanısın. Samimi ve emojili konuş.
        
                SALON VERİLERİMİZ:
        
                MEVCUT DERSLERİMİZ: 
                {serviceList}

                HOCALARIMIZ VE VERDİKLERİ DERSLER (Buraya dikkat et, hangi hoca hangi dersi veriyor):
                {trainerList}

                KULLANICI MESAJI: ""{request.Message}""

                GÖREVİN:
                Bu verilere dayanarak cevap ver. 
                Örneğin kullanıcı 'Pilates istiyorum' derse, yukarıdaki listeden Pilates veren hocayı bul ve onu öner.
                Asla listede olmayan bir hoca veya ders uydurma.
            ";

            string answer = await _aiService.GetAnswerAsync(prompt);
            return Json(new {reply = answer});
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
