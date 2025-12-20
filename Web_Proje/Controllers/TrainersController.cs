using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Web_Proje.Models;
using Web_Proje.Models.ViewModels; 
namespace Web_Proje.Controllers
{
    public class TrainersController : Controller
    {
        private readonly GymContext _context;

        public TrainersController(GymContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            // Trainer ve TrainerApiDto
            var trainers = await _context.Trainers
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.service)
                .Select(t => new TrainerApiDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Specialty = t.Specialty,
                    GymId = t.GymId,
                    GymName = t.Gym.Name,
                    ShiftStart = t.ShiftStart,
                    ShiftEnd = t.ShiftEnd,
                    Services = t.TrainerServices.Select(ts => ts.service.Name).ToList()
                })
                .ToListAsync();

            return View(trainers);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.Gym)
                .Include(t => t.TrainerServices)
                .ThenInclude(ts => ts.service)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null) return NotFound();

            return View(trainer);
        }
        public IActionResult Create()
        {
            //Frontend listeleme için
            ViewBag.GymList = new SelectList(_context.Gyms, "Id", "Name");
            ViewBag.AllServices = _context.Services.ToList();

            return View(new TrainerApiDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TrainerApiDto model, int[] ServiceIds)
        {
            ModelState.Remove("GymName");
            //Saat doğrulama
            var gym = await _context.Gyms.FindAsync(model.GymId);
            if (gym != null)
            {
                if (model.ShiftStart < gym.OpeningHour)
                {
                    ModelState.AddModelError("ShiftStart", $"Hata: Mesai ({model.ShiftStart}), salon açılışından ({gym.OpeningHour}) önce başlayamaz.");
                }
                if (model.ShiftEnd > gym.ClosingTime)
                {
                    ModelState.AddModelError("ShiftEnd", $"Hata: Mesai ({model.ShiftEnd}), salon kapanışından ({gym.ClosingTime}) sonra bitemez.");
                }
            }

            if (ModelState.IsValid)
            {
                // Trainer ve Dto eşlemesi
                var trainer = new Trainer
                {
                    Name = model.Name,
                    Specialty = model.Specialty,
                    ShiftStart = model.ShiftStart,
                    ShiftEnd = model.ShiftEnd,
                    GymId = model.GymId,
                    TrainerServices = new List<TrainerService>() // Listeyi başlat
                };

                
                if (ServiceIds != null)
                {
                    foreach (var serviceId in ServiceIds)
                    {
                        trainer.TrainerServices.Add(new TrainerService
                        {
                            ServiceId = serviceId
                        });
                    }
                }

                _context.Add(trainer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            //Modelde Hata var

            // Listeleri tekrar doldur
            ViewBag.GymList = new SelectList(_context.Gyms, "Id", "Name", model.GymId);
            ViewBag.AllServices = _context.Services.ToList();
            //tekrar model gönder (yazılar silinmesin)
            return View(model);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Eğitmeni ve Hizmet İlişkilerini Çek
            var trainer = await _context.Trainers
                .Include(t => t.TrainerServices) // İlişkiyi dahil et
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null) return NotFound();

            var trainerDto = new TrainerApiDto
            {
                Id = trainer.Id,
                Name = trainer.Name,
                Specialty = trainer.Specialty,
                ShiftStart = trainer.ShiftStart,
                ShiftEnd = trainer.ShiftEnd,
                GymId = trainer.GymId,
                ServiceIds = trainer.TrainerServices.Select(ts => ts.ServiceId).ToList()
            };

            ViewBag.GymList = new SelectList(_context.Gyms, "Id", "Name", trainer.GymId);
            ViewBag.AllServices = _context.Services.ToList(); // (List<Web_Proje.Models.Services>) cast için

            return View(trainerDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TrainerApiDto model, int[] ServiceIds)
        {
            if (id != model.Id) return NotFound();

            ModelState.Remove("GymName");

            var gym = await _context.Gyms.FindAsync(model.GymId);
            if (gym != null)
            {
                if (model.ShiftStart < gym.OpeningHour)
                {
                    ModelState.AddModelError("ShiftStart", $"Hata: Mesai ({model.ShiftStart}), salon açılışından ({gym.OpeningHour}) önce başlayamaz.");
                }
                if (model.ShiftEnd > gym.ClosingTime)
                {
                    ModelState.AddModelError("ShiftEnd", $"Hata: Mesai ({model.ShiftEnd}), salon kapanışından ({gym.ClosingTime}) sonra bitemez.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var trainerToUpdate = await _context.Trainers
                        .Include(t => t.TrainerServices) 
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (trainerToUpdate == null) return NotFound();

                    trainerToUpdate.Name = model.Name;
                    trainerToUpdate.Specialty = model.Specialty;
                    trainerToUpdate.ShiftStart = model.ShiftStart;
                    trainerToUpdate.ShiftEnd = model.ShiftEnd;
                    trainerToUpdate.GymId = model.GymId;

                    trainerToUpdate.TrainerServices.Clear(); // Eskileri sil
                    if (ServiceIds != null)
                    {
                        foreach (var serviceId in ServiceIds)
                        {
                            trainerToUpdate.TrainerServices.Add(new TrainerService
                            {
                                ServiceId = serviceId,
                                TrainerId = id
                            });
                        }
                    }

                    _context.Update(trainerToUpdate);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrainerExists(model.Id)) return NotFound();
                    else throw;
                }
            }

            // Hata

            // Checkbox'ların seçili kalması için model.ServiceIds'i güncelle
            model.ServiceIds = ServiceIds?.ToList() ?? new List<int>();

            ViewBag.GymList = new SelectList(_context.Gyms, "Id", "Name", model.GymId);
            ViewBag.AllServices = _context.Services.ToList();

            return View(model);
        }

        private bool TrainerExists(int id)
        {
            return _context.Trainers.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.Gym)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null) return NotFound();

            return View(trainer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null) _context.Trainers.Remove(trainer);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}