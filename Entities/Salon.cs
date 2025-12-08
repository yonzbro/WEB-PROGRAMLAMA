using System.ComponentModel.DataAnnotations;

namespace GymProject1.Entities
{
    public class Salon
    {
        public int SalonId { get; set; }

        [Required(ErrorMessage = "Salon adı gereklidir")]
        [StringLength(200, ErrorMessage = "Salon adı en fazla 200 karakter olabilir")]
        [Display(Name = "Salon Adı")]
        public string? Name { get; set; } // Şube Adı

        [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir")]
        [Display(Name = "Adres")]
        public string? Address { get; set; }

        [Range(1, 10000, ErrorMessage = "Kapasite 1-10000 arasında olmalıdır")]
        [Display(Name = "Kapasite")]
        public int? Capacity { get; set; } // Maksimum üye kapasitesi

        // Çalışma saatleri tanımlanmalı 
        [StringLength(100, ErrorMessage = "Çalışma saatleri en fazla 100 karakter olabilir")]
        [Display(Name = "Çalışma Saatleri")]
        public string? WorkingHours { get; set; } // Örn: "09:00 - 22:00"

        // İlişkiler
        public ICollection<Trainer> Trainers { get; set; } = [];

    }
}
