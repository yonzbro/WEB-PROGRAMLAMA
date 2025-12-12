using Microsoft.AspNetCore.Identity;
using GymProject1.Entities; 
using System;
using System.Threading.Tasks;

namespace GymProject1.Data 
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider service)
        {
            // Kullanıcı ve Rol Yönetimi servislerini çağırıyoruz
            var userManager = service.GetService<UserManager<AppUser>>();
            var roleManager = service.GetService<RoleManager<IdentityRole>>();

            // 1. ROLLERİ OLUŞTUR (Admin ve Member)
            
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            await roleManager.CreateAsync(new IdentityRole("Member"));

            // 2. ADMIN KULLANCISINI OLUŞTUR
            // Gereksinimler: ogrencinumarasi@sakarya.edu.tr formatında olmalı
            var adminEmail = "G231210039@sakarya.edu.tr"; // <-- Kendi öğrenci numaranızı buraya yazın (örn: G231210039@sakarya.edu.tr)
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Sistem",
                    LastName = "Yöneticisi",
                    EmailConfirmed = true,
                    ImageUrl = "/img/default-admin.jpg"
                };

                // Kullanıcıyı oluştur (Şifre: sau)
                var result = await userManager.CreateAsync(adminUser, "sau");

                if (result.Succeeded)
                {
                    // Kullanıcıya Admin rolünü ata
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
