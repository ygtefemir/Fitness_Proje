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
        public async Task<IActionResult> Create(TrainerViewModel ViewModel, int[] selectedServices)
        {
            if (ModelState.IsValid)
            {
                // Trainer Nesnesini Oluştur
                var trainer = new Trainer
                {
                    Name = ViewModel.Name,
                    Specialty = ViewModel.Specialty,
                    ShiftStart = ViewModel.ShiftStart,
                    ShiftEnd = ViewModel.ShiftEnd,
                    GymId = ViewModel.GymId,
                    // Listeyi başlatıyoruz
                    TrainerServices = new List<TrainerService>()
                };

                //Seçilen Checkbox'ları (ID'leri) Dönüp Ekliyoruz
                if (selectedServices != null)
                {
                    foreach (var serviceId in selectedServices)
                    {
                        trainer.TrainerServices.Add(new TrainerService
                        {
                            ServiceId = serviceId
                        });
                    }
                }

                //aydet
                _context.Add(trainer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Hata varsa sayfayı tekrar doldurup gönder
            
            ViewModel.Services = _context.Services.Select(s => new TrainerViewModel.AssignedServiceData
            {
                ServiceId = s.Id,
                ServiceName = s.Name,
                Assigned = false
            }).ToList();

            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Name", ViewModel.GymId);
            return View(ViewModel);
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Antrenörü ve ilişkili derslerini çek
            var trainer = await _context.Trainers
                .Include(t => t.TrainerServices)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainer == null) return NotFound();

            // derslerinin ID'lerini bir listeye al (Hızlı kontrol için HashSet)
            var currentServiceIds = trainer.TrainerServices.Select(ts => ts.ServiceId).ToHashSet();

            //Entity'i ViewModel'e çevir
            var viewModel = new TrainerViewModel
            {
                Id = trainer.Id,
                Name = trainer.Name,
                Specialty = trainer.Specialty,
                ShiftStart = trainer.ShiftStart,
                ShiftEnd = trainer.ShiftEnd,
                GymId = trainer.GymId,
                // Checkbox listesini hazırlıyoruz
                Services = _context.Services.Select(s => new TrainerViewModel.AssignedServiceData
                {
                    ServiceId = s.Id,
                    ServiceName = s.Name,
                    // sahipse 'Assigned' true olsun (Kutu işaretli gelsin)
                    Assigned = currentServiceIds.Contains(s.Id)
                }).ToList()
            };

            ViewData["GymId"] = new SelectList(_context.Gyms, "Id", "Name", trainer.GymId);
            return View(viewModel);
        }


        // POST: Trainers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TrainerViewModel viewModel, int[] selectedServices)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    //hocayı çek
                    var trainerToUpdate = await _context.Trainers
                        .Include(t => t.TrainerServices)
                        .FirstOrDefaultAsync(t => t.Id == id);

                    if (trainerToUpdate == null) return NotFound();

                    //normal bilgileri güncelle
                    trainerToUpdate.Name = viewModel.Name;
                    trainerToUpdate.Specialty = viewModel.Specialty;
                    trainerToUpdate.ShiftStart = viewModel.ShiftStart;
                    trainerToUpdate.ShiftEnd = viewModel.ShiftEnd;
                    trainerToUpdate.GymId = viewModel.GymId;

                    // (Veritabanındaki mevcut listeyi siliyoruz)
                    trainerToUpdate.TrainerServices.Clear();

                    //yeni seçilenleri ekle
                    if (selectedServices != null)
                    {
                        foreach (var serviceId in selectedServices)
                        {
                            trainerToUpdate.TrainerServices.Add(new TrainerService
                            {
                                ServiceId = serviceId,
                                TrainerId = id
                            });
                        }
                    }

                    // Kaydet
                    _context.Update(trainerToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrainerExists(viewModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(viewModel);
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
