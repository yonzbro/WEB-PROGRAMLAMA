using System.Diagnostics;
using GymProject1.Models;
using GymProject1.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymProject1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // İstatistikleri ViewBag'e ekle
            ViewBag.TrainerCount = await _context.Trainers.CountAsync();
            ViewBag.ServiceCount = await _context.Services.CountAsync();
            ViewBag.SalonCount = await _context.Salons.CountAsync();
            ViewBag.AppointmentCount = await _context.Appointments.CountAsync();
            
            // Son eklenen antrenörleri getir
            ViewBag.RecentTrainers = await _context.Trainers
                .Include(t => t.Salon)
                .OrderByDescending(t => t.TrainerId)
                .Take(3)
                .ToListAsync();
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}


