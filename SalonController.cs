using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GymProject1.Data;
using GymProject1.Entities;
using Microsoft.AspNetCore.Authorization; // <-- BU KÜTÜPHANE EKLENDİ

namespace GymProject1.Controllers
{
    public class SalonController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalonController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Salon (Herkes şubeleri görebilir, sorun yok)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Salons.ToListAsync());
        }

        // GET: Salon/Details/5 (Detayları herkes görebilir)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var salon = await _context.Salons.FirstOrDefaultAsync(m => m.SalonId == id);
            if (salon == null) return NotFound();

            return View(salon);
        }

        // ---------------------------------------------------------
        // AŞAĞIDAKİ İŞLEMLERİ SADECE ADMIN YAPABİLİR
        // ---------------------------------------------------------

        // GET: Salon/Create
        [Authorize(Roles = "Admin")] // <-- KİLİT
        public IActionResult Create()
        {
            return View();
        }

        // POST: Salon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <-- KİLİT
        public async Task<IActionResult> Create([Bind("SalonId,Name,Address,WorkingHours")] Salon salon)
        {
            if (ModelState.IsValid)
            {
                _context.Add(salon);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(salon);
        }

        // GET: Salon/Edit/5
        [Authorize(Roles = "Admin")] // <-- KİLİT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var salon = await _context.Salons.FindAsync(id);
            if (salon == null) return NotFound();
            return View(salon);
        }

        // POST: Salon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <-- KİLİT
        public async Task<IActionResult> Edit(int id, [Bind("SalonId,Name,Address,WorkingHours")] Salon salon)
        {
            if (id != salon.SalonId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(salon);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SalonExists(salon.SalonId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(salon);
        }

        // GET: Salon/Delete/5
        [Authorize(Roles = "Admin")] // <-- KİLİT
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var salon = await _context.Salons.FirstOrDefaultAsync(m => m.SalonId == id);
            if (salon == null) return NotFound();

            return View(salon);
        }

        // POST: Salon/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <-- KİLİT
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var salon = await _context.Salons.FindAsync(id);
            if (salon != null) _context.Salons.Remove(salon);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SalonExists(int id)
        {
            return _context.Salons.Any(e => e.SalonId == id);
        }
    }
}
