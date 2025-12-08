using Microsoft.AspNetCore.Identity;
namespace GymProject1.Entities
{
    public class AppUser : IdentityUser
    {
        // Ad ve Soyad, raporlama ve profil ekranı için gerekli[cite: 73].
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // Profil fotoğrafı veya AI analizi için yüklenecek fotoğraflar için
        public string? ImageUrl { get; set; }
    }
}
