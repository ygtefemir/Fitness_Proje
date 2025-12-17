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

        // EĞİTMENİN HİZMETLERİNİ GETİR
        // Frontend'deki dropdown için "text" formatında veri döner.
        [HttpGet]
        public async Task<IActionResult> GetTrainers()
        {
            var trainers = await _context.Trainers
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.service)
                .Select(t => new TrainerApiDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Specialty = t.Specialty,
                    GymId = t.GymId,
                    Services = t.TrainerServices.Select(ts => ts.service.Name).ToList(),
                    ShiftStart = t.ShiftStart,
                    ShiftEnd = t.ShiftEnd
                })
                .ToListAsync();

            return Ok(trainers);
        }

        // EĞİTMEN DETAY GETİR
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrainer(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.TrainerServices)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (trainer == null) return NotFound();

            var dto = new TrainerApiDto
            {
                Id = trainer.Id,
                Name = trainer.Name,
                Specialty = trainer.Specialty,
                ServiceIds = trainer.TrainerServices.Select(ts => ts.ServiceId).ToList(),
                ShiftStart = trainer.ShiftStart,
                ShiftEnd = trainer.ShiftEnd
            };

            return Ok(dto);
        }
        //YENİ EĞİTMEN OLUŞTUR
        [HttpPost]
        public async Task<IActionResult> CreateTrainer([FromBody] TrainerApiDto dto)
        {
            var trainer = new Trainer
            {
                Name = dto.Name,
                Specialty = dto.Specialty,
                GymId = dto.GymId,
                ShiftStart = dto.ShiftStart, 
                ShiftEnd = dto.ShiftEnd

            };

            _context.Trainers.Add(trainer);
            await _context.SaveChangesAsync();

            if (dto.ServiceIds != null && dto.ServiceIds.Any())
            {
                foreach (var serviceId in dto.ServiceIds)
                {
                    _context.TrainerService.Add(new TrainerService
                    {
                        TrainerId = trainer.Id,
                        ServiceId = serviceId
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Eklendi" });
        }

        // EĞİTMEN GÜNCELLE
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTrainer(int id, [FromBody] TrainerApiDto dto)
        {
            if (id != dto.Id) return BadRequest();

            var trainer = await _context.Trainers
                .Include(t => t.TrainerServices)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (trainer == null) return NotFound();

            trainer.Name = dto.Name;
            trainer.Specialty = dto.Specialty;
            trainer.GymId = dto.GymId;
            trainer.ShiftStart = dto.ShiftStart;
            trainer.ShiftEnd = dto.ShiftEnd;

            _context.TrainerService.RemoveRange(trainer.TrainerServices);

            if (dto.ServiceIds != null)
            {
                foreach (var serviceId in dto.ServiceIds)
                {
                    _context.TrainerService.Add(new TrainerService
                    {
                        TrainerId = trainer.Id,
                        ServiceId = serviceId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Güncellendi" });
        }

        // EĞİTMEN SİL
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer == null) return NotFound();

            _context.Trainers.Remove(trainer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Silindi." });
        }

        // MÜSAİT SAATLERİ (SLOTLARI) GETİR
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

        //TÜM HİZMETLERİ GETİR (Filtre Kaldırılınca Kullanılacak)
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

        //TÜM EĞİTMENLERİ GETİR
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