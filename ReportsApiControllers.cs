using GymProject1.Data;
using GymProject1.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymProject1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsApiControllers : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsApiControllers(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. TÜM ANTRENÖRLERİ GETİREN API (PDF Örneği)
        // Adres: https://localhost:port/api/reportsapi/trainers
        [HttpGet("trainers")]
        public async Task<ActionResult<IEnumerable<object>>> GetTrainers()
        {
            // LINQ Sorgusu: Sadece gerekli alanları (Ad, Uzmanlık, Şube) seçip getirir.
            var trainers = await _context.Trainers
                .Include(t => t.Salon)
                .Select(t => new {
                    AdSoyad = t.Name,
                    Uzmanlik = t.Specialization,
                    Sube = t.Salon.Name
                })
                .ToListAsync();

            return Ok(trainers);
        }

        // 2. BELİRLİ BİR TARİHTEKİ RANDEVULARI GETİREN FİLTRELEME API'Sİ (LINQ ile Filtreleme)
        // Adres: https://localhost:port/api/reportsapi/appointments?date=2025-12-04
        [HttpGet("appointments")]
        public async Task<ActionResult<IEnumerable<object>>> GetAppointmentsByDate(DateTime date)
        {
            // LINQ Sorgusu: Parametre olarak gelen tarihteki randevuları filtreler
            var appointments = await _context.Appointments
                .Where(a => a.AppointmentDate.Date == date.Date) // Filtreleme Kısmı
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                .Select(a => new {
                    Tarih = a.AppointmentDate.ToString("dd.MM.yyyy HH:mm"),
                    Egitmen = a.Trainer.Name,
                    Hizmet = a.Service.Name,
                    Durum = a.Status
                })
                .ToListAsync();

            if (appointments.Count == 0)
            {
                return NotFound(new { Mesaj = "Bu tarihte kayıtlı randevu bulunamadı." });
            }

            return Ok(appointments);
        }

        // 3. BELİRLİ BİR TARİHTE UYGUN ANTRENÖRLERİ GETİREN API (LINQ ile Filtreleme)
        // Adres: https://localhost:port/api/reportsapi/availabletrainers?date=2025-12-04T14:00
        [HttpGet("availabletrainers")]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailableTrainers(DateTime date)
        {
            // LINQ Sorgusu: Belirli tarihte randevusu olmayan antrenörleri filtreler
            var busyTrainerIds = await _context.Appointments
                .Include(a => a.Service) // Service bilgisini yükle
                .Where(a => 
                    a.AppointmentDate.Date == date.Date && // Aynı gün
                    a.Status != "İptal Edildi" && // İptal edilenler hariç
                    // Çakışma kontrolü: Randevu saatleri arasında olan antrenörler
                    a.AppointmentDate <= date &&
                    a.AppointmentDate.AddMinutes(a.Service != null ? a.Service.DurationMinutes : 60) > date
                )
                .Select(a => a.TrainerId)
                .Distinct()
                .ToListAsync();

            // Meşgul olmayan antrenörleri getir
            var availableTrainers = await _context.Trainers
                .Include(t => t.Salon)
                .Where(t => !busyTrainerIds.Contains(t.TrainerId)) // Meşgul olmayanlar
                .Select(t => new {
                    TrainerId = t.TrainerId,
                    AdSoyad = t.Name,
                    Uzmanlik = t.Specialization,
                    MüsaitlikSaatleri = t.AvailabilityHours,
                    Sube = t.Salon != null ? t.Salon.Name : "Belirtilmemiş"
                })
                .ToListAsync();

            if (availableTrainers.Count == 0)
            {
                return NotFound(new { Mesaj = "Bu tarihte uygun antrenör bulunamadı." });
            }

            return Ok(availableTrainers);
        }
    }
}
