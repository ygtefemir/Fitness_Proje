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
    [Authorize(Roles="Admin")]
    public class TrainersController : Controller
    {
        private readonly GymContext _context;

        public TrainersController(GymContext context)
        {
            _context = context;
        }

        // GET: Trainers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Trainers.ToListAsync());
        }

        // GET: Trainers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        // GET: Trainers/Create
        public IActionResult Create()
        {
            var allServices = _context.Services.ToList();
            var ViewModel = new TrainerViewModel
            {
                Services = allServices.Select(s => new TrainerViewModel.AssignedServiceData
                {
                    ServiceId = s.Id,
                    ServiceName = s.Name,
                    Assigned = false
                }).ToList()
            };

            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Name");
            return View(ViewModel);
        }

        // POST: Trainers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TrainerViewModel ViewModel)
        {
            var trainer = new Trainer
            {
                Name = ViewModel.Name,
                Specialty = ViewModel.Specialty,
                ShiftStart = ViewModel.ShiftStart,
                ShiftEnd = ViewModel.ShiftEnd,
                GymId = ViewModel.GymId,
                TrainerServices = new List<TrainerService>()
            };  

            foreach (var service in ViewModel.Services)
            {
                if (service.Assigned)
                {
                    trainer.TrainerServices.Add(new TrainerService
                    {
                        ServiceId = service.ServiceId
                    });
                }
            }


            if (ModelState.IsValid)
            {
                _context.Add(trainer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ViewModel);
        }

        // GET: Trainers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .Include(t => t.TrainerServices)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (trainer == null)
            {
                return NotFound();
            }

            var allServices = await _context.Services.ToListAsync(); //tüm hizmetleri al
            var trainerServiceIds = trainer.TrainerServices.Select(ts => ts.ServiceId).ToHashSet();
            var ViewModel = new TrainerViewModel
            {
                Id = trainer.Id,
                Name = trainer.Name,
                Specialty = trainer.Specialty,
                ShiftStart = trainer.ShiftStart,
                ShiftEnd = trainer.ShiftEnd,
                Services = new List<TrainerViewModel.AssignedServiceData>()
            };

            foreach (var service in allServices)
            {
                ViewModel.Services.Add(new TrainerViewModel.AssignedServiceData
                {
                    ServiceId = service.Id,
                    ServiceName = service.Name,
                    Assigned = trainerServiceIds.Contains(service.Id)
                });
            }
            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Name", trainer.GymId);

            return View(ViewModel);
        }
        

        // POST: Trainers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TrainerViewModel ViewModel)
        {
            if (id != ViewModel.Id)
            {
                return NotFound();
            }

            var TrainerToUpdate = await _context.Trainers
                .Include(t => t.TrainerServices)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (TrainerToUpdate == null) return NotFound();

            TrainerToUpdate.Name = ViewModel.Name;
            TrainerToUpdate.Specialty = ViewModel.Specialty;
            TrainerToUpdate.ShiftStart = ViewModel.ShiftStart;    
            TrainerToUpdate.ShiftEnd = ViewModel.ShiftEnd;
            TrainerToUpdate.GymId = ViewModel.GymId;
            
            var currentServices = TrainerToUpdate.TrainerServices.ToList();

            _context.TrainerService.RemoveRange(currentServices);

            foreach(var service in ViewModel.Services)
            {
                if (service.Assigned)
                {
                    _context.TrainerService.Add(new TrainerService
                    {
                        TrainerId = TrainerToUpdate.Id,
                        ServiceId = service.ServiceId
                    });
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TrainerExists(ViewModel.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return View(ViewModel);


        }

        // GET: Trainers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trainer == null)
            {
                return NotFound();
            }

            return View(trainer);
        }

        // POST: Trainers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                _context.Trainers.Remove(trainer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TrainerExists(int id)
        {
            return _context.Trainers.Any(e => e.Id == id);
        }
    }
}
