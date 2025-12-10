using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymProject1.Data; // Veritabanı bağlantısı için
using Microsoft.EntityFrameworkCore;

namespace GymProject1.Controllers
{
    [Authorize(Roles = "Admin")] // Sadece Admin girebilir!
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // İstatistikleri çekip ekrana basmak için (Opsiyonel ama havalı durur)
            ViewBag.TrainerCount = await _context.Trainers.CountAsync();
            ViewBag.ServiceCount = await _context.Services.CountAsync();
            ViewBag.AppointmentCount = await _context.Appointments.CountAsync();

            return View();
        }
    }
}
