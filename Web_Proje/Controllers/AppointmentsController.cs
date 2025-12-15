using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Policy;
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

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var allAppointments = _context.Appointments
                    .Include(a => a.Trainer) 
                    .Include(a => a.Service) 
                    .Include(a => a.User) 
                    .Include(a => a.Gym); 

                return View(await allAppointments.ToListAsync());
            }
            else
            { 
                var myAppointments = _context.Appointments
                    .Include(a => a.Trainer)
                    .Include(a => a.Service)
                    .Include(a => a.Gym)
                    .Where(a => a.UserId == user.Id); 

                return View(await myAppointments.ToListAsync());
            }


        }
        public IActionResult Create()
        {
            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Name");
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "Id", "Name");
            ViewData["ServiceId"] = new SelectList(_context.Services, "Id", "Name");
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

            // 1. Önce Hizmetin Süresini Çekmeliyiz (Bitiş zamanını hesaplamak için)
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

            // 2. YENİ ÇAKIŞMA KONTROLÜ (Overlap Check)
            // Mantık: (YeniBaşla < EskiBit) VE (YeniBit > EskiBaşla)
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
