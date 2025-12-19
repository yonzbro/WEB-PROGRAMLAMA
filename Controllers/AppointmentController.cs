using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GymProject1.Data;
using GymProject1.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json; // JSON İşlemleri için gerekli

namespace GymProject1.Controllers
{
    [Authorize] // KİLİT: Sadece giriş yapanlar bu sayfaları açabilir
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AppointmentController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. RANDEVULARI LİSTELEME
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Admin"))
            {
                var allAppointments = await _context.Appointments
                    .Include(a => a.Trainer)
                    .Include(a => a.Service)
                    .Include(a => a.AppUser)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
                return View(allAppointments);
            }
            else
            {
                var myAppointments = await _context.Appointments
                    .Include(a => a.Trainer)
                    .Include(a => a.Service)
                    .Where(a => a.AppUserId == userId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
                return View(myAppointments);
            }
        }

        // 2. YENİ RANDEVU ALMA SAYFASI (GÜNCELLENDİ: Cascading Dropdown İçin Veri Hazırlığı)
        [HttpGet]
        public IActionResult Create()
        {
            // 1. Eğitmenleri Çek (Ad Soyad Birleştir ve Uzmanlık Alanını Al)
            // Not: Eğitmen tablosunda 'Specialty' yoksa kod patlamasın diye kontrol ediyoruz.
            // Eğer veritabanında 'Specialty' yoksa buradaki ".Select" kısmını güncellemen gerekir.
            var trainers = _context.Trainers.Select(t => new
            {
                Id = t.TrainerId,
                FullName = (t.Name ?? "") ,
                Specialty = t.Specialization ?? "" // Uzmanlık alanı (Örn: Fitness, Pilates)
            }).ToList();

            // 2. Hizmetleri Çek
            var services = _context.Services.Select(s => new
            {
                Id = s.ServiceId,
                Name = s.Name
            }).ToList();

            // Dropdown'ı doldur
            ViewData["TrainerId"] = new SelectList(trainers, "Id", "FullName");

            // Hizmet dropdown'ını BOŞ gönderiyoruz (JavaScript dolduracak)
            ViewData["ServiceId"] = new SelectList(Enumerable.Empty<SelectListItem>());

            // *** KRİTİK KISIM ***
            // JavaScript'in okuyabilmesi için verileri JSON formatında View'a taşıyoruz.
            // Bu sayede "Eğitmen seçilince Hizmetleri filtrele" işlemini yapabileceğiz.
            ViewBag.TrainersJson = JsonSerializer.Serialize(trainers);
            ViewBag.ServicesJson = JsonSerializer.Serialize(services);

            return View();
        }

        // 3. RANDEVU KAYDETME (POST) - GÜNCELLENDİ: Güvenlik Kontrolü Eklendi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            appointment.AppUserId = currentUserId;
            appointment.Status = "Onay Bekliyor";

            var selectedService = await _context.Services.FindAsync(appointment.ServiceId);

            // --- GÜVENLİK KONTROLÜ (Back-End Validation) ---
            // Kullanıcı JavaScript'i atlatıp yanlış hizmet seçerse burada yakalayalım.
            var selectedTrainer = await _context.Trainers
                .Include(t => t.Salon)
                .FirstOrDefaultAsync(t => t.TrainerId == appointment.TrainerId);

            if (selectedTrainer != null && selectedService != null)
            {
                // Eğer eğitmenin uzmanlığı varsa ve seçilen hizmet bu uzmanlığı İÇERMİYORSA hata ver.
                // Örn: Eğitmen "Fitness", Hizmet "Yoga" -> Hata!
                if (!string.IsNullOrEmpty(selectedTrainer.Specialization) &&
                    !selectedService.Name.Contains(selectedTrainer.Specialization, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("", $"Seçilen eğitmen ({selectedTrainer.Name}) bu hizmeti ({selectedService.Name}) vermemektedir.");
                }

                // --- MEVCUT ÇAKIŞMA KONTROLLERİN (Aynen Korumalı) ---
                var appointmentTime = appointment.AppointmentDate.TimeOfDay;
                var appointmentEndTime = appointment.AppointmentDate.AddMinutes(selectedService.DurationMinutes).TimeOfDay;

                // 1. Müsaitlik Saati Kontrolü
                if (!string.IsNullOrEmpty(selectedTrainer.AvailabilityHours))
                {
                    var parts = selectedTrainer.AvailabilityHours.Split(new[] { '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 && TimeSpan.TryParse(parts[0], out var start) && TimeSpan.TryParse(parts[1], out var end))
                    {
                        if (appointmentTime < start || appointmentEndTime > end)
                        {
                            ModelState.AddModelError("AppointmentDate", $"Eğitmen sadece {selectedTrainer.AvailabilityHours} saatleri arasında müsaittir.");
                        }
                    }
                }

                // 2. Çakışma Kontrolü
                var newEndTime = appointment.AppointmentDate.AddMinutes(selectedService.DurationMinutes);
                bool isConflict = await _context.Appointments
                    .Include(a => a.Service)
                    .Where(e => e.TrainerId == appointment.TrainerId && e.Status != "İptal Edildi" && e.AppointmentDate >= DateTime.Now)
                    .AnyAsync(e => e.AppointmentDate < newEndTime && e.AppointmentDate.AddMinutes(e.Service.DurationMinutes) > appointment.AppointmentDate);

                if (isConflict)
                {
                    ModelState.AddModelError("AppointmentDate", "Seçtiğiniz saatte eğitmen dolu.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Hata durumunda listeleri tekrar doldur
            // (Burada da aynı mantık: Trainer listesi dolu, Service listesi JS ile dolacak)
            var trainersList = _context.Trainers.Select(t => new { Id = t.TrainerId, FullName = (t.Name ?? "") }).ToList();
            var servicesList = _context.Services.Select(s => new { Id = s.ServiceId, Name = s.Name }).ToList();

            ViewData["TrainerId"] = new SelectList(trainersList, "Id", "FullName", appointment.TrainerId);
            ViewData["ServiceId"] = new SelectList(servicesList, "Id", "Name", appointment.ServiceId); // Hata durumunda seçili kalsın diye dolu gönderiyoruz

            // JSON verilerini tekrar gönder (JS çalışmaya devam etsin)
            ViewBag.TrainersJson = JsonSerializer.Serialize(trainersList);
            ViewBag.ServicesJson = JsonSerializer.Serialize(servicesList);

            return View(appointment);
        }

        // 4. ONAYLAMA
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = "Onaylandı";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 5. İPTAL
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (appointment.AppUserId == userId || User.IsInRole("Admin"))
            {
                appointment.Status = "İptal Edildi";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. PROFİL SAYFASI (HAKKINDA)
        public async Task<IActionResult> Hakkinda()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var myAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Where(a => a.AppUserId == user.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var model = new MemberProfileViewModel
            {
                User = user,
                Appointments = myAppointments
            };

            return View(model);
        }
    }

    public class MemberProfileViewModel
    {
        public AppUser User { get; set; }
        public List<Appointment> Appointments { get; set; }
    }
}