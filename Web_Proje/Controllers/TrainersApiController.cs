using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_Proje.Models; // Modellerin olduğu namespace

namespace Web_Proje.Controllers
{
    [Route("api/[controller]")] // URL: /api/trainersapi
    [ApiController]
    public class TrainersApiController : ControllerBase
    {
        private readonly GymContext _context; // Eğer adını GymContext yaptıysan burayı değiştir

        public TrainersApiController(GymContext context)
        {
            _context = context;
        }

        // 1. EĞİTMENİN HİZMETLERİNİ GETİR
        // Frontend'deki dropdown için "text" formatında veri döner.
        [HttpGet("{id}/services")]
        public async Task<IActionResult> GetTrainerServices(int id)
        {
            var services = await _context.Services
                .Where(s => s.TrainerServices.Any(ts => ts.TrainerId == id))
                .Select(s => new
                {
                    id = s.Id,
                    // ÖNEMLİ: Frontend (jQuery) 'text' alanı bekliyor
                    text = $"{s.Name} ({s.DurationMin} dk - {s.Price} TL)",
                    price = s.Price,
                    duration = s.DurationMin
                })
                .ToListAsync();

            return Ok(services);
        }

        // 2. MÜSAİT SAATLERİ (SLOTLARI) GETİR
        // TimeSpan/TimeOnly dönüşümünü ve Çakışma kontrolünü yapar.
        [HttpGet("{id}/slots")]
        public async Task<IActionResult> GetDailySlots(int id, [FromQuery] string date, [FromQuery] int serviceId)
        {
            if (!DateTime.TryParse(date, out DateTime selectedDate))
                return BadRequest("Geçersiz tarih formatı.");

            var trainer = await _context.Trainers
                .Include(t => t.Appointments)
                .ThenInclude(a => a.Service)
                .FirstOrDefaultAsync(t => t.Id == id);

            var requestedService = await _context.Services.FindAsync(serviceId);

            if (trainer == null || requestedService == null) return NotFound("Eğitmen veya Hizmet bulunamadı.");

            TimeOnly shiftStart = trainer.ShiftStart != TimeSpan.Zero ? TimeOnly.FromTimeSpan(trainer.ShiftStart) : new TimeOnly(9, 0);
            TimeOnly shiftEnd = trainer.ShiftEnd != TimeSpan.Zero ? TimeOnly.FromTimeSpan(trainer.ShiftEnd) : new TimeOnly(17, 0);

            var existingAppointments = trainer.Appointments
                .Where(a => a.AppointmentDate.Date == selectedDate.Date && a.Status != Status.Rejected)
                .ToList();

            var availableSlots = new List<object>();

            // --- MOLA SÜRESİ TANIMI ---
            int bufferMinutes = 15;

            var currentTime = shiftStart;

            // Döngü: "Ders Süresi + 15 Dk" ekleyerek ilerler.
            // Böylece 09:00'da 45 dklık ders varsa, bir sonraki slot 10:00'da açılır (09:45 + 15dk).
            while (currentTime.AddMinutes(requestedService.DurationMin) <= shiftEnd)
            {
                var slotStart = currentTime;
                var slotEnd = currentTime.AddMinutes(requestedService.DurationMin);

                // ÇAKIŞMA KONTROLÜ (BUFFER DAHİL)
                bool isTaken = existingAppointments.Any(a =>
                {
                    var appStart = TimeOnly.FromDateTime(a.AppointmentDate);
                    var appDuration = a.Service?.DurationMin ?? 60;

                    // Mevcut randevunun bittiği an değil, mola sonrası anı "meşgul" sayıyoruz.
                    var appEndWithBuffer = appStart.AddMinutes(appDuration + bufferMinutes);

                    // Mantık: (Bizim Başlangıç < Onların Bitiş+Mola) VE (Bizim Bitiş+Mola > Onların Başlangıç)
                    return slotStart < appEndWithBuffer && (slotEnd.AddMinutes(bufferMinutes)) > appStart;
                });

                bool isPast = (selectedDate.Date == DateTime.Today && slotStart < TimeOnly.FromDateTime(DateTime.Now));

                availableSlots.Add(new
                {
                    time = slotStart.ToString("HH:mm"),
                    isAvailable = !isTaken && !isPast
                });

                // Bir sonraki seansa geçerken MOLAYI EKLE
                currentTime = currentTime.AddMinutes(requestedService.DurationMin + bufferMinutes);
            }

            return Ok(availableSlots);
        }

        [HttpGet("by-service/{serviceId}")]
        public async Task<IActionResult> GetTrainersByService(int serviceId)
        {
            var trainers = await _context.Trainers
                .Where(t => t.TrainerServices.Any(ts => ts.ServiceId == serviceId))
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name // Dropdown'da görünecek isim
                })
                .ToListAsync();

            return Ok(trainers);
        }

        // 3. TÜM HİZMETLERİ GETİR (Filtre Kaldırılınca Kullanılacak)
        [HttpGet("all-services")]
        public async Task<IActionResult> GetAllServices()
        {
            var services = await _context.Services
                .Select(s => new
                {
                    id = s.Id,
                    text = $"{s.Name} ({s.DurationMin} dk - {s.Price} TL)" // Formatlı text
                })
                .ToListAsync();

            return Ok(services);
        }

        // 4. TÜM EĞİTMENLERİ GETİR
        [HttpGet("all-trainers")]
        public async Task<IActionResult> GetAllTrainers()
        {
            var trainers = await _context.Trainers
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name
                })
                .ToListAsync();

            return Ok(trainers);
        }


    }
}