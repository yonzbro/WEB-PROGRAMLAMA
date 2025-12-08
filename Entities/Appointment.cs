using System.ComponentModel.DataAnnotations;

namespace GymProject1.Entities
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        // Randevu Tarihi ve Saati 
        [Required(ErrorMessage = "Randevu tarihi ve saati gereklidir")]
        [Display(Name = "Randevu Tarihi ve Saati")]
        public DateTime AppointmentDate { get; set; }

        // Durum: Onay Bekliyor, Onaylandı, İptal 
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Onay Bekliyor";

        // İlişkiler: Kim, Hangi Antrenörden, Hangi Dersi alıyor?
        public string? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        [Required(ErrorMessage = "Antrenör seçimi gereklidir")]
        [Display(Name = "Antrenör")]
        public int TrainerId { get; set; }
        public Trainer? Trainer { get; set; }

        [Required(ErrorMessage = "Hizmet seçimi gereklidir")]
        [Display(Name = "Hizmet")]
        public int ServiceId { get; set; }
        public Service? Service { get; set; }
    }
}

