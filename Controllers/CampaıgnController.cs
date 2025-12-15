using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Mail;
using GymProject1.Entities;

namespace GymProject1.Controllers
{
    [Authorize(Roles = "Admin")] // Sadece Admin erişebilir
    public class CampaignController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public CampaignController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        // 1. Mail Yazma Ekranı (GET)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // 2. Mail Gönderme İşlemi (POST)
        [HttpPost]
        [ValidateAntiForgeryToken] // Güvenlik önlemi
        public async Task<IActionResult> SendEmail(string subject, string messageBody)
        {
            // 1. O anki oturum açmış Admin bilgisini al (İsmi ve Maili için)
            var currentUser = await _userManager.GetUserAsync(User);

            // Eğer admin bulunamazsa varsayılan değerler ata
            string adminName = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "GYM-A Yönetim";
            string adminEmail = currentUser?.Email;

            // 2. Tüm üyelerin e-posta adreslerini çek
            var userEmails = _userManager.Users
                .Where(u => !string.IsNullOrEmpty(u.Email)) // Boş mailleri filtrele
                .Select(u => u.Email)
                .ToList();

            // 3. SMTP Ayarları (Sabit Şirket Hesabı)
            // BURAYA DİKKAT: Google Hesabından aldığın 16 haneli "Uygulama Şifresi"ni girmen şart.
            string senderEmail = "gyma1907@gmail.com";
            string senderPassword = "iutviygtlzrcmqiv";

            try
            {
                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.EnableSsl = true;
                    // Sabit mail ile sunucuya bağlan
                    smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;

                    foreach (var email in userEmails)
                    {
                        var mail = new MailMessage();

                        // GÖNDEREN: Teknik olarak gyma1907@gmail.com ama görünen isim "GYM-A (Admin Adı)"
                        mail.From = new MailAddress(senderEmail, $"GYM-A ({adminName})");

                        // ALICI
                        mail.To.Add(email);

                        // YANIT ADRESİ: Üye cevaplarsa adminin kendi mailine gitsin
                        if (!string.IsNullOrEmpty(adminEmail))
                        {
                            mail.ReplyToList.Add(adminEmail);
                        }

                        mail.Subject = subject;

                        // HTML İçerik (Daha şık bir görünüm için)
                        mail.Body = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #ddd; background-color: #f9f9f9;'>
                                <h2 style='color: #d35400;'>{subject}</h2>
                                <hr style='border: 0; border-top: 1px solid #ccc;' />
                                <p style='font-size: 16px; color: #333;'>{messageBody}</p>
                                <br/><br/>
                                <small style='color: #888;'>Bu mesaj <strong>{adminName}</strong> tarafından gönderilmiştir.</small>
                            </div>";

                        mail.IsBodyHtml = true;

                        await smtp.SendMailAsync(mail);
                    }
                }

                TempData["Success"] = $"{userEmails.Count} üyeye kampanya maili başarıyla gönderildi!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Mail gönderilirken hata oluştu: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
