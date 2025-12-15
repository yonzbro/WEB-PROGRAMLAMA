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
        private readonly UserManager<AppUser> _userManager; // 1. Üye yönetimi için bunu ekledik

        // 2. Constructor'ı güncelledik (UserManager'ı içeri aldık)
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

            // Ekstra: Toplam üye sayısını da gösterelim
            ViewBag.MemberCount = await _userManager.Users.CountAsync();

            return View();
        }

        // --- YENİ EKLENEN: ÜYE LİSTELEME VE YÖNETME ---

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

        // 4. Üye Silme İşlemi
        [HttpPost]
        public async Task<IActionResult> DeleteMember(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Members));
        }

        // Listeleme için yardımcı model (Controller içinde kalabilir)
        public class UserViewModel
        {
            public string Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
        }
    }
}
