using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GymProject1.Data;
using GymProject1.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace GymProject1.Controllers
{
    [Authorize] // KİLİT: Sadece giriş yapanlar bu sayfaları açabilir
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private TimeSpan appointmentTime;

        public AppointmentController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. RANDEVULARI LİSTELEME
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Eğer Admin ise TÜM randevuları görsün
            if (User.IsInRole("Admin"))
            {
                var allAppointments = await _context.Appointments
                    .Include(a => a.Trainer)
                    .Include(a => a.Service)
                    .Include(a => a.AppUser) // Üye bilgisini de getir
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
                return View(allAppointments);
            }
            else
            {
                // Eğer Üye ise SADECE KENDİ randevularını görsün
                var myAppointments = await _context.Appointments
                    .Include(a => a.Trainer)
                    .Include(a => a.Service)
                    .Where(a => a.AppUserId == userId) // Filtreleme
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
                return View(myAppointments);
            }
        }

        // 2. YENİ RANDEVU ALMA SAYFASI
        public IActionResult Create()
        {
            // Dropdownlar için verileri gönderiyoruz (Eğitmenler ve Hizmetler)
            // Not: İsim alanlarını (FullName, Name) kendi modeline göre kontrol et.
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "Name");
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name");
            return View();
        }

        // 3. RANDEVU KAYDETME (POST) - GÜNCELLENEN KISIM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            // Giriş yapan kullanıcının ID'sini alıyoruz
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            appointment.AppUserId = currentUserId;
            appointment.Status = "Onay Bekliyor";

            // -- DETAYLI ÇAKIŞMA KONTROLÜ BAŞLANGICI --

            // Önce hizmeti bul (randevu bitiş saatini hesaplamak için)
            var selectedService = await _context.Services.FindAsync(appointment.ServiceId);
            
            // 0. Adım: Seçilen antrenörü bul (Müsaitlik saatlerini kontrol etmek için)
            var selectedTrainer = await _context.Trainers
                .Include(t => t.Salon)
                .FirstOrDefaultAsync(t => t.TrainerId == appointment.TrainerId);

            if (selectedTrainer != null)
            {
                var appointmentTime = appointment.AppointmentDate.TimeOfDay;
                // Randevu bitiş saatini hesapla
                var appointmentEndTime = selectedService != null 
                    ? appointment.AppointmentDate.AddMinutes(selectedService.DurationMinutes).TimeOfDay 
                    : appointmentTime.Add(TimeSpan.FromHours(1)); // Varsayılan 1 saat

                // Antrenörün müsaitlik saatlerini kontrol et
                if (!string.IsNullOrEmpty(selectedTrainer.AvailabilityHours))
                {
                    var availabilityHours = selectedTrainer.AvailabilityHours;
                    var timeSeparator = availabilityHours.Contains(" - ") ? " - " : "-";
                    var parts = availabilityHours.Split(new[] { timeSeparator }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 2)
                    {
                        if (TimeSpan.TryParse(parts[0].Trim(), out TimeSpan startTime) &&
                            TimeSpan.TryParse(parts[1].Trim(), out TimeSpan endTime))
                        {
                            // Randevu başlangıç ve bitiş saati kontrolü
                            if (appointmentTime < startTime || appointmentEndTime > endTime)
                            {
                                ModelState.AddModelError("AppointmentDate", 
                                    $"Bu antrenörün müsaitlik saatleri: {availabilityHours}. Lütfen bu saatler arasında randevu alınız.");
                            }
                        }
                    }
                }

                // Salon çalışma saatlerini de kontrol et
                if (selectedTrainer.Salon != null && !string.IsNullOrEmpty(selectedTrainer.Salon.WorkingHours))
                {
                    var salonHours = selectedTrainer.Salon.WorkingHours;
                    // Saat formatını parse et: "09:00 - 23:00" veya "09:00-23:00"
                    var timeSeparator = salonHours.Contains(" - ") ? " - " : "-";
                    var parts = salonHours.Split(new[] { timeSeparator }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (parts.Length == 2)
                    {
                        if (TimeSpan.TryParse(parts[0].Trim(), out TimeSpan startTime) &&
                            TimeSpan.TryParse(parts[1].Trim(), out TimeSpan endTime))
                        {
                            // Randevu başlangıç ve bitiş saati kontrolü
                            if (appointmentTime < startTime || appointmentEndTime > endTime)
                            {
                                ModelState.AddModelError("AppointmentDate", 
                                    $"Salon çalışma saatleri: {salonHours}. Lütfen bu saatler arasında randevu alınız.");
                            }
                        }
                    }
                }
            }

            if (selectedService != null)
            {
                // Yeni randevunun başlangıç ve tahmini bitiş saati
                var newStartTime = appointment.AppointmentDate;

                // DÜZELTME: Senin modelindeki 'DurationMinutes' alanını kullandım
                var newEndTime = newStartTime.AddMinutes(selectedService.DurationMinutes);

                // 2. Adım: Veritabanında çakışan randevu var mı?
                // Sadece gelecekteki randevuları kontrol et (geçmiş randevuları hariç tut)
                bool isConflict = await _context.Appointments
                    .Include(a => a.Service) // Mevcut randevuların sürelerini de hesaba katmak için
                    .Where(existing =>
                        existing.TrainerId == appointment.TrainerId && // Aynı antrenör
                        existing.Status != "İptal Edildi" && // İptal edilenler hariç
                        existing.AppointmentDate >= DateTime.Now // Sadece gelecekteki randevular
                    )
                    .AnyAsync(existing =>
                        // Çakışma Mantığı: (MevcutBaşlangıç < YeniBitiş) VE (MevcutBitiş > YeniBaşlangıç)
                        existing.AppointmentDate < newEndTime &&
                        existing.Service != null && // Service null kontrolü
                        existing.AppointmentDate.AddMinutes(existing.Service.DurationMinutes) > newStartTime
                    );

                if (isConflict)
                {
                    ModelState.AddModelError("AppointmentDate", "Seçtiğiniz saat aralığında bu eğitmen başka bir randevuda. Lütfen başka bir saat seçiniz.");
                }
            }
            // -- DETAYLI ÇAKIŞMA KONTROLÜ BİTİŞİ --

            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Hata varsa dropdownları tekrar doldur
            ViewData["TrainerId"] = new SelectList(_context.Trainers, "TrainerId", "Name", appointment.TrainerId);
            ViewData["ServiceId"] = new SelectList(_context.Services, "ServiceId", "Name", appointment.ServiceId);
            return View(appointment);
        }

        // 4. RANDEVU ONAYLAMA (Sadece Admin)
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

        // 5. RANDEVU İPTAL ETME (Admin veya Randevu Sahibi)
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Sadece kendi randevusunu veya Admin ise iptal edebilir
            if (appointment.AppUserId == userId || User.IsInRole("Admin"))
            {
                appointment.Status = "İptal Edildi";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Hakkinda()
        {
            // 1. Giriş yapan kullanıcıyı bul
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // 2. Kullanıcının randevularını çek
            var myAppointments = await _context.Appointments
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Where(a => a.AppUserId == user.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            // 3. Verileri ViewModel'e paketle
            var model = new MemberProfileViewModel
            {
                User = user,
                Appointments = myAppointments
            };

            return View(model);
        }
    }

    // --- BU KISMI CONTROLLER CLASS'ININ DIŞINA (EN ALTA) EKLEYEBİLİRSİN ---
    public class MemberProfileViewModel
    {
        public AppUser User { get; set; }
        public List<Appointment> Appointments { get; set; }
    }
}

