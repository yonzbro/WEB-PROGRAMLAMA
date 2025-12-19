using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymProject1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // UserManager için gerekli
using GymProject1.Entities; // AppUser için gerekli

namespace GymProject1.Controllers
{
    [Authorize(Roles = "Admin")] // Sadece Admin girebilir!
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // İstatistikleri çekip ekrana basmak için
            ViewBag.TrainerCount = await _context.Trainers.CountAsync();
            ViewBag.ServiceCount = await _context.Services.CountAsync();
            ViewBag.AppointmentCount = await _context.Appointments.CountAsync();
            ViewBag.SalonCount = await _context.Salons.CountAsync();

            // Ekstra: Toplam üye sayısını da gösterelim
            ViewBag.MemberCount = await _userManager.Users.CountAsync();

            return View();
        }

        // --- ÜYE LİSTELEME VE YÖNETME ---

        // 3. Üyeleri Listeleyen Sayfa
        public async Task<IActionResult> Members()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                // Her kullanıcının rolünü (Admin, Member vs.) çekiyoruz
                var roles = await _userManager.GetRolesAsync(user);

                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}", // Ad Soyad Birleştir
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "Üye" // Rol yoksa varsayılan 'Üye' yazsın
                });
            }

            return View(userViewModels);
        }

        // 4. Üye Silme İşlemi (GÜNCELLENDİ: ARTIK HATA VERMEZ)
        [HttpPost]
        public async Task<IActionResult> DeleteMember(string id)
        {
            // 1. Kullanıcıyı bul
            var user = await _userManager.FindByIdAsync(id);

            if (user != null)
            {
                // 2. KRİTİK ADIM: Önce bu kullanıcının randevularını temizle
                // Eğer bunu yapmazsak SQL "Foreign Key" hatası verir.
                var userAppointments = _context.Appointments
                                               .Where(a => a.AppUserId == id)
                                               .ToList();

                if (userAppointments.Any())
                {
                    _context.Appointments.RemoveRange(userAppointments);
                    await _context.SaveChangesAsync(); // Randevuları silmeyi onayla
                }

                // 3. Randevular temizlendi, şimdi üyeyi silebiliriz
                await _userManager.DeleteAsync(user);
            }

            return RedirectToAction(nameof(Members));
        }

        // Listeleme için yardımcı model
        public class UserViewModel
        {
            public string Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
        }
    }
}
