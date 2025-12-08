using System.ComponentModel.DataAnnotations;

namespace GymProject1.Entities
{
    public class Trainer
    {
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Ad Soyad gereklidir")]
        [StringLength(200, ErrorMessage = "Ad Soyad en fazla 200 karakter olabilir")]
        [Display(Name = "Ad Soyad")]
        public string? Name { get; set; }

        // Uzmanlık alanı (Örn: Kilo Verme, Kas Kazanma) [cite: 16]
        [StringLength(200, ErrorMessage = "Uzmanlık alanı en fazla 200 karakter olabilir")]
        [Display(Name = "Uzmanlık Alanı")]
        public string? Specialization { get; set; }

        // Müsaitlik saatleri (Örn: "09:00-18:00" veya "Pazartesi-Cuma: 09:00-18:00")
        [StringLength(100, ErrorMessage = "Müsaitlik saatleri en fazla 100 karakter olabilir")]
        [Display(Name = "Müsaitlik Saatleri")]
        public string? AvailabilityHours { get; set; }

        [Display(Name = "Fotoğraf")]
        public string? ImageUrl { get; set; } // Arayüzde kartlarda göstermek için

        [Required(ErrorMessage = "Salon seçimi gereklidir")]
        [Display(Name = "Salon")]
        public int SalonId { get; set; }
        public Salon? Salon { get; set; }

        public ICollection<TrainerService> TrainerServices { get; set; } = [];
        public ICollection<Appointment> Appointments { get; set; } = [];
    }
}
