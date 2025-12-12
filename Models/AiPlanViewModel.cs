using System.ComponentModel.DataAnnotations;

namespace GymProject1.Models
{
    public class AiPlanViewModel
    {
        [Required(ErrorMessage = "Yaş gereklidir")]
        [Range(1, 120, ErrorMessage = "Yaş 1-120 arasında olmalıdır")]
        public int Age { get; set; }        // Yaş

        [Required(ErrorMessage = "Kilo gereklidir")]
        [Range(1, 500, ErrorMessage = "Kilo 1-500 kg arasında olmalıdır")]
        public double Weight { get; set; }  // Kilo

        [Required(ErrorMessage = "Boy gereklidir")]
        [Range(50, 300, ErrorMessage = "Boy 50-300 cm arasında olmalıdır")]
        public double Height { get; set; }  // Boy

        [Required(ErrorMessage = "Cinsiyet seçiniz")]
        public string? Gender { get; set; } // Cinsiyet

        [Required(ErrorMessage = "Hedef belirtiniz")]
        [StringLength(500, ErrorMessage = "Hedef en fazla 500 karakter olabilir")]
        public string? Goal { get; set; }   // Hedef (Kilo verme vb.)

        // Fotoğraf yükleme için (Opsiyonel)
        public IFormFile? Photo { get; set; }

        // Gemini'den gelen cevabı burada tutacağız
        public string? AiResponse { get; set; }
    }
}
