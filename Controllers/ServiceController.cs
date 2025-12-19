using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GymProject1.Data;
using GymProject1.Entities;
using Microsoft.AspNetCore.Authorization;

namespace GymProject1.Controllers
{
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            // Include ile Salon bilgisini de çekiyoruz ki listede "Hangi Salon?" görünsün
            return View(await _context.Services.Include(s => s.Salon).ToListAsync());
        }

        // 2. DETAYLAR
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.Services
                .Include(s => s.Salon) // Detayda salon adı görünsün
                .FirstOrDefaultAsync(m => m.ServiceId == id);

            if (service == null) return NotFound();

            return View(service);
        }

        // --- ADMIN İŞLEMLERİ ---

        // 3. EKLEME (GET)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["SalonId"] = new SelectList(_context.Salons, "SalonId", "Name");
            return View();
        }

        // 4. EKLEME (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("ServiceId,Name,DurationMinutes,Price,Description,SalonId")] Service service)
        {
            if (ModelState.IsValid)
            {
                _context.Add(service);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["SalonId"] = new SelectList(_context.Salons, "SalonId", "Name", service.SalonId);
            return View(service);
        }

        // 5. DÜZENLEME (GET)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            // DÜZELTME: Düzenleme sayfasında da Salon dropdown'ı dolu gelmeli
            ViewData["SalonId"] = new SelectList(_context.Salons, "SalonId", "Name", service.SalonId);

            return View(service);
        }

        // 6. DÜZENLEME (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        // DÜZELTME: Bind içine "SalonId" eklendi.
        public async Task<IActionResult> Edit(int id, [Bind("ServiceId,Name,DurationMinutes,Price,Description,SalonId")] Service service)
        {
            if (id != service.ServiceId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceExists(service.ServiceId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            // Hata olursa dropdown tekrar dolsun
            ViewData["SalonId"] = new SelectList(_context.Salons, "SalonId", "Name", service.SalonId);
            return View(service);
        }

        // 7. SİLME (GET)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var service = await _context.Services
                .Include(s => s.Salon)
                .FirstOrDefaultAsync(m => m.ServiceId == id);

            if (service == null) return NotFound();

            return View(service);
        }

        // 8. SİLME (POST) - KRİTİK DÜZELTME BURADA!
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service != null)
            {
                // --- İŞTE BURASI HATAYI ÇÖZER ---
                // Önce bu hizmete ait tüm randevuları bul ve sil
                var relatedAppointments = _context.Appointments
                                                  .Where(a => a.ServiceId == id)
                                                  .ToList();

                if (relatedAppointments.Any())
                {
                    _context.Appointments.RemoveRange(relatedAppointments);
                    // Değişikliği hemen kaydet ki Service silinirken engel kalmasın
                    await _context.SaveChangesAsync();
                }
                // --------------------------------

                _context.Services.Remove(service);
                await _context.SaveChangesAsync(); // Hizmeti sil
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ServiceExists(int id)
        {
            return _context.Services.Any(e => e.ServiceId == id);
        }
    }
}