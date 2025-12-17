using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_Proje.Models;

namespace Web_Proje.Controllers
{
    [Route("api/[controller]")] // /api/appointmentsapi
    [ApiController]
    [Authorize] // Sadece giriş yapmış kullanıcılar API'yi kullanabilir
    public class AppointmentsApiController : ControllerBase
    {
        private readonly GymContext _context;
        private readonly UserManager<User> _userManager;

        public AppointmentsApiController(GymContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments(bool isPast = false, string search = "", int? trainerId = null)
        {
      
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            // Sorgu ifadeleri
            var query = _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Include(a => a.Gym)
                .Include(a => a.User)
                .AsQueryable();

            // Admin değilse sadece kendi randevularını görmeli
            if (trainerId.HasValue)
            {
                query = query.Where(a => a.TrainerId == trainerId.Value);
            }
            if (!isAdmin)
            {
                query = query.Where(a => a.UserId == user.Id);
            }
            else if (!string.IsNullOrEmpty(search)) // Admin Arama Yapıyorsa
            {
                query = query.Where(a =>
                    a.User.FirstName.Contains(search) ||
                    a.User.LastName.Contains(search) ||
                    a.UserId.ToString() == search
                );
            }

            // (Gelecek / Geçmiş)
            if (isPast)
            {
                query = query.Where(a => a.AppointmentDate < DateTime.Now ||
                                         a.Status == Status.Completed ||
                                         a.Status == Status.NotCompleted)
                             .OrderByDescending(a => a.AppointmentDate);
            }
            else // "upcoming"
            {
                query = query.Where(a => a.AppointmentDate >= DateTime.Now &&
                                         (a.Status == Status.Pending || a.Status == Status.Confirmed))
                             .OrderBy(a => a.AppointmentDate);
            }

            // JSON döndür
            var result = await query.Select(a => new {
                id = a.Id,
                AppointmentDate = a.AppointmentDate,
                gymName = a.Gym.Name,
                trainerName = a.Trainer.Name,
                serviceName = a.Service.Name,
                status = a.Status.ToString(),
                // Admin için ekstra bilgiler
                userName = isAdmin ? a.User.FirstName + " " + a.User.LastName : null,
                userId = isAdmin ? a.UserId : (int?)null,
                // Buton kontrolleri için (Admin veya Sahibi)
                canCancel = isAdmin || (a.UserId == user.Id && a.Status == Status.Pending)
            }).ToListAsync();

            return Ok(result);
        }
    }
}