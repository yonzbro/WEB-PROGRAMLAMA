using GymProject1.Data;
using GymProject1.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GymProject1.Controllers
{
    public class TrainerController : Controller
    {
        
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; // 1. EKLENDİ: Dosya yolu bulucu

        // Constructor güncellendi
        public TrainerController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Trainer
        public async Task<IActionResult> Index()
        {
            var trainers = await _context.Trainers.Include(t => t.Salon).ToListAsync();
            return View(trainers);
        }

        // GET: Trainer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.Salon)
                .FirstOrDefaultAsync(m => m.TrainerId == id);

            if (trainer == null) return NotFound();

            return View(trainer);
        }

        // GET: Trainer/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            // Salonları dropdown için gönderiyoruz
            ViewData["SalonId"] = new SelectList(_context.Salons, "SalonId", "Name"); // "SalonId" yerine "Name" görünmesi daha şık olur
            return View();
        }

        // POST: Trainer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Trainer trainer, IFormFile? file)
        {
            // 2. DÜZELTİLDİ: WebRootPath kullanımı
            string wwwRootPath = _webHostEnvironment.WebRootPath;

            if (file != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string trainerPath = Path.Combine(wwwRootPath, @"img\trainers");

                if (!Directory.Exists(trainerPath)) Directory.CreateDirectory(trainerPath);

                using (var fileStream = new FileStream(Path.Combine(trainerPath, fileName), FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                trainer.ImageUrl = @"/img/trainers/" + fileName;
            }
            else
            {
                trainer.ImageUrl = @"/img/default-user.png";
            }

            if (trainer.SalonId == 0) trainer.SalonId = 1; // Geçici çözüm

            _context.Add(trainer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Trainer/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer == null) return NotFound();

            // Dropdown'da Salon İsmi (Name) görünsün diye 3. parametreyi "Name" yaptım (Modelde varsa)
            // Modelde Name yoksa "SalonId" olarak bırakabilirsin.
            ViewData["SalonId"] = new SelectList(_context.Salons, "SalonId", "Name", trainer.SalonId);
            return View(trainer);
        }

        // POST: Trainer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Trainer trainer, IFormFile? file) // <-- file eklendi
        {
            if (id != trainer.TrainerId) return NotFound();

            

            try
            {
                // Resim yükleme işlemi (Create ile aynı mantık)
                if (file != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string trainerPath = Path.Combine(wwwRootPath, @"img\trainers");

                    if (!Directory.Exists(trainerPath)) Directory.CreateDirectory(trainerPath);

                    using (var fileStream = new FileStream(Path.Combine(trainerPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    trainer.ImageUrl = @"/img/trainers/" + fileName;
                }
                else
                {
                    // Eğer yeni resim yüklenmediyse, eski resim silinmesin diye veritabanından eskiyi bulup geri yazıyoruz.
                    // AsNoTracking kullanıyoruz ki EF Core karışıklık çıkarmasın.
                    var oldTrainer = await _context.Trainers.AsNoTracking().FirstOrDefaultAsync(x => x.TrainerId == id);
                    if (oldTrainer != null)
                    {
                        trainer.ImageUrl = oldTrainer.ImageUrl;
                    }
                }

                _context.Update(trainer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TrainerExists(trainer.TrainerId)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Trainer/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var trainer = await _context.Trainers
                .Include(t => t.Salon)
                .FirstOrDefaultAsync(m => m.TrainerId == id);

            if (trainer == null) return NotFound();

            return View(trainer);
        }

        // POST: Trainer/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                // İstersen burada wwwroot klasöründen resmi de silebilirsin (Opsiyonel)
                _context.Trainers.Remove(trainer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TrainerExists(int id)
        {
            return _context.Trainers.Any(e => e.TrainerId == id);
        }
    }
}