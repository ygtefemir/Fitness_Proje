using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_Proje.Models;

namespace Web_Proje.Controllers
{
    [Route("api/trainers")]
    [ApiController]
    public class TrainersApiController : ControllerBase
    {
        private readonly GymContext _context;

        public TrainersApiController(GymContext context)
        {
            _context = context;
        }

        [HttpGet("{id}/slots")]
        public async Task<IActionResult> GetDailySlots(int id, string date, int serviceId)
        {
            
            if (!DateTime.TryParse(date, out DateTime selectedDate))
                return BadRequest("Geçersiz tarih.");

            var trainer = await _context.Trainers.FindAsync(id);
            var requestedService = await _context.Services.FindAsync(serviceId);

            if (trainer == null || requestedService == null) return NotFound();

            int breakTime = 15;
            
            var existingAppointments = await _context.Appointments
                .Include(a => a.Service) 
                .Where(a => a.TrainerId == id &&
                            a.AppointmentDate.Date == selectedDate.Date &&
                            a.Status != Models.Status.Rejected)
                .ToListAsync();

            var availableSlots = new List<object>();

            
            TimeSpan currentSlot = trainer.ShiftStart;

            
            int durationNeeded = requestedService.DurationMin + breakTime;

            
            while (currentSlot.Add(TimeSpan.FromMinutes(durationNeeded)) <= trainer.ShiftEnd)
            {
                TimeSpan startCandidate = currentSlot;
                TimeSpan endCandidate = currentSlot.Add(TimeSpan.FromMinutes(durationNeeded));

                bool isTaken = existingAppointments.Any(existingApp =>
                {
                    var existingStart = existingApp.AppointmentDate.TimeOfDay;
                    var existingEnd = existingStart.Add(TimeSpan.FromMinutes(existingApp.Service.DurationMin + breakTime));

                    return (startCandidate < existingEnd) && (endCandidate > existingStart);
                });

                availableSlots.Add(new
                {
                    time = currentSlot.ToString(@"hh\:mm"),
                    isAvailable = !isTaken
                });

                currentSlot = currentSlot.Add(TimeSpan.FromMinutes(15));
            }

            return Ok(availableSlots);
        }

        [HttpGet("{id}/services")]
        public async Task<IActionResult> GetTrainerServices(int id)
        {
            var services = await _context.TrainerService
                .Where(ts => ts.TrainerId == id)
                .Include(ts => ts.service)
                .Select(ts => new
                {
                    id = ts.service.Id,
                    name = ts.service.Name,
                    price = ts.service.Price
                })
                .ToListAsync();

            return Ok(services);
        }
    }
}