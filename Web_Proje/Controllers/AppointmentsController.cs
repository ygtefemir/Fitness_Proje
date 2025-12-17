using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Policy;
using System.Text.Json;
using Web_Proje.Models;

namespace Web_Proje.Controllers
{
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly GymContext _context;
        private readonly UserManager<User> _userManager;

        public AppointmentsController(GymContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(bool isPast = false, string search = "", int? trainerId=null)
        {
            List<AppointmentApiDto> randevular = new List<AppointmentApiDto>();

            using (var client = new HttpClient())
            {
                // API'nin tam adresi (Localhost portunu dinamik alır)
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                client.BaseAddress = new Uri(baseUrl);

                // cookie iletiyoruz ki login kalsın.
                foreach (var cookie in Request.Cookies)
                {
                    client.DefaultRequestHeaders.Add("Cookie", $"{cookie.Key}={cookie.Value}");
                }

                string apiUrl = $"/api/AppointmentsApi?isPast={isPast}&search={search}";

                if (trainerId.HasValue)
                {
                    apiUrl += $"&trainerId={trainerId}";
                    ViewBag.CurrentTrainerId = trainerId; // View'da kullanmak için
                }

                // API'ye İstek At (JSON İste)
                
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // SON String'i oku
                    var jsonString = await response.Content.ReadAsStringAsync();

                    //JSON'u C# Listesine Çevir (Deserialize)
                    // PropertyNameCaseInsensitive: 'id' ile 'Id' eşleşsin diye.
                    randevular = JsonSerializer.Deserialize<List<AppointmentApiDto>>(jsonString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    // Hata olursa boş liste veya hata mesajı
                    ModelState.AddModelError("", "API verisi çekilemedi.");
                }
            }

            // View'a durumu bildirmek için
            ViewBag.IsPast = isPast;
            ViewBag.CurrentSearch = search;

            //Listeyi View'a gönder
            return View(randevular);
        }
        public IActionResult Create()
        {
            //Eğitmenler, Gymler, Hizmetler verisini çek

            // Eğitmenler
            var trainers = _context.Trainers
                .Include(t => t.TrainerServices)
                .Select(t => new
                {
                    Id = t.Id,
                    Name = t.Name,
                    GymId = t.GymId,
                    ServiceIds = t.TrainerServices.Select(ts => ts.ServiceId).ToList()
                }).ToList();

            // Gymler
            var gyms = _context.Gyms
                .Select(g => new { Id = g.Id, Name = g.Name })
                .ToList();

            // Hizmetler
            var services = _context.Services
                .Select(s => new { Id = s.Id, Name = s.Name, Price = s.Price })
                .ToList();

            //Bu veriyi JSON olarak View'a taşı
            ViewBag.MasterData = System.Text.Json.JsonSerializer.Serialize(new
            {
                Trainers = trainers,
                Gyms = gyms,
                Services = services
            });

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            if (!User.IsInRole("Admin") && appointment.UserId != user.Id)
            {
                return Unauthorized();
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            var user = await _userManager.GetUserAsync(User);
            appointment.UserId = user.Id;
            appointment.Status = Status.Pending;

            // Servis süresini çek
            var service = await _context.Services.FindAsync(appointment.ServiceId);
            if (service == null)
            {
                ModelState.AddModelError("", "Hizmet bulunamadı.");
                return View(appointment);
            }

            // Seçilen Eğitmeni ve Gym bilgisini al
            var chosenTrainee = await _context.Trainers.FindAsync(appointment.TrainerId);
            if (chosenTrainee != null)
            {
                appointment.GymId = chosenTrainee.GymId;
            }

          
            int bufferMinutes = 15; 

            var newStart = appointment.AppointmentDate;
            var newEnd = newStart.AddMinutes(service.DurationMin + bufferMinutes);

            bool is_conflict = await _context.Appointments
                .Include(a => a.Service) // Eski randevunun süresini bilmek için Include şart!
                .Where(a => a.TrainerId == appointment.TrainerId && a.Status != Status.Rejected)
                .AnyAsync(a =>
                    // Veritabanındaki randevunun başlangıcı < Yeni Bitiş
                    a.AppointmentDate < newEnd &&
                    // Veritabanındaki randevunun bitişi > Yeni Başlangıç
                    a.AppointmentDate.AddMinutes(a.Service.DurationMin) > newStart
                );

            if (is_conflict)
            {
                ModelState.AddModelError("", "Seçilen eğitmen, bu saat aralığında başka bir randevu ile dolu.");

                // Hata durumunda dropdownları tekrar doldur
                ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Name", appointment.GymId);
                ViewData["TrainerId"] = new SelectList(_context.Trainers, "Id", "Name", appointment.TrainerId);
                ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", appointment.ServiceId);
                return View(appointment);
            }

            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Model valid değilse dropdownları doldur
            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Name", appointment.GymId);
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "Id", "Name", appointment.TrainerId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name", appointment.ServiceId);

            return View(appointment);
        }



        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if(appointment != null)
            {
                appointment.Status = Status.Confirmed;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deny(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = Status.Rejected;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task _SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
