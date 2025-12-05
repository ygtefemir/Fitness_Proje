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
        public async Task<IActionResult> Create(Appointment appointment) {
            var user = await _userManager.GetUserAsync(User);
            appointment.UserId = user.Id;

            appointment.Status = Status.Pending;

            var chosenTrainee = await _context.Trainers.FindAsync(appointment.TrainerId);

            if (chosenTrainee != null)
            {
                appointment.GymId = chosenTrainee.GymId;
            }

            bool is_conflict = _context.Appointments.Any(a =>
                a.TrainerId == appointment.TrainerId &&
                a.AppointmentDate == appointment.AppointmentDate &&
                a.Status != Status.Rejected
            );

            if (is_conflict)
            {
                ModelState.AddModelError("", "Seçilen eğitmen, seçilen saatte müsait değildir.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

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
