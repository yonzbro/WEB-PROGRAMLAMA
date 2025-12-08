using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymProject1.Entities
{
    public class Service
    {
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Hizmet adı gereklidir")]
        [StringLength(200, ErrorMessage = "Hizmet adı en fazla 200 karakter olabilir")]
        [Display(Name = "Hizmet Adı")]
        public string? Name { get; set; } // Yoga, Pilates, BodyBuilding

        [Required(ErrorMessage = "Süre gereklidir")]
        [Range(15, 480, ErrorMessage = "Süre 15-480 dakika arasında olmalıdır")]
        [Display(Name = "Süre (Dakika)")]
        public int DurationMinutes { get; set; } // Süre (dk) 

        [Required(ErrorMessage = "Ücret gereklidir")]
        [Range(0, 10000, ErrorMessage = "Ücret 0-10000 arasında olmalıdır")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Ücret (TL)")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; } // Ücret 

        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; } = null;

        // Bu hizmeti hangi antrenörler veriyor?
        public ICollection<TrainerService> TrainerServices { get; set; } = [];
    }
}
