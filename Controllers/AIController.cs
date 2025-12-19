using GymProject1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace GymProject1.Controllers
{
    public class AiController : Controller
    {
        private readonly IConfiguration _configuration;

        public AiController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new AiPlanViewModel());
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Index(AiPlanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. API Key Kontrolü
            string? apiKey = _configuration["GeminiSettings:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                model.AiResponse = "<div class='alert alert-danger'>Hata: API Key 'appsettings.json' içinde bulunamadı.</div>";
                return View(model);
            }

            // Varsayılan model
            string modelId = "gemini-1.5-flash";

            try
            {
                // Modelleri dinamik olarak seç (Flash öncelikli, yoksa Pro)
                var availableModels = await GetAvailableModelNamesAsync(apiKey);
                if (availableModels != null && availableModels.Count > 0)
                {
                    var bestModel = availableModels.FirstOrDefault(m => m.Equals("gemini-1.5-flash", StringComparison.OrdinalIgnoreCase))
                                 ?? availableModels.FirstOrDefault(m => m.Contains("flash", StringComparison.OrdinalIgnoreCase))
                                 ?? availableModels.FirstOrDefault(m => m.Contains("pro", StringComparison.OrdinalIgnoreCase))
                                 ?? availableModels.First();
                    modelId = bestModel.Replace("models/", "");
                }
            }
            catch
            {
                // Model listesi alınamazsa varsayılan ile devam et
            }

            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";

            // -----------------------------------------------------------------------------------------
            // 2. GELİŞMİŞ PROMPT (DARK MODE & OPTİMİZE GÖRSEL)
            // -----------------------------------------------------------------------------------------
            string userPrompt =
                $"Ben {model.Age} yaşında, {model.Weight} kilo, {model.Height} cm boyunda bir {model.Gender}. " +
                $"Hedefim: {model.Goal}. " +
                $"Bana haftalık profesyonel bir antrenman ve beslenme programı hazırla.\n\n" +

                $"### TASARIM VE FORMAT TALİMATLARI (ÇOK ÖNEMLİ):\n" +
                $"1. Cevabı **HTML formatında** ver. Bootstrap 'card', 'badge', 'list-group' yapılarını kullan.\n" +
                $"2. **DARK MODE UYUMU:** Site siyah temalıdır. Kartların arka planı için `bg-dark` veya `bg-secondary`, metinler için `text-white` veya `text-light` sınıflarını kullan. Asla beyaz arka plan kullanma.\n" +
                $"3. **GÖRSEL KURALI:** Yemekler için kesinlikle resim koyma. Her egzersiz için de resim koyma.\n" +
                $"4. Sadece o gün çalıştırılan **ANA KAS GRUBUNUN** (Örn: Göğüs, Sırt, Bacak) estetik ve gelişmiş bir fotoğrafını günün başlığına ekle.\n" +
                $"5. Yani 'Pazartesi: Göğüs' dediğinde, sadece gelişmiş bir göğüs kası (chest muscles) fotoğrafı koy.\n" +
                $"6. Görsel URL formatı şu olmalı: `<img src='https://image.pollinations.ai/prompt/aesthetic {model.Gender} fitness model with developed [KAS_GRUBU_INGILIZCE] muscles, gym lighting, hyper realistic?width=600&height=300&nologo=true' style='width:100%; border-radius:10px; margin-bottom:15px; box-shadow: 0 5px 15px rgba(0,0,0,0.5);' alt='Hedef Vücut' />`\n" +
                $"7. İçerik dili Türkçe, görsel promptları İngilizce olsun.\n" +
                $"8. Markdown etiketi (```html) kullanma, direkt saf HTML kodunu ver.";

            if (model.Photo != null && model.Photo.Length > 0)
            {
                userPrompt += " Ayrıca yüklediğim fotoğrafıma bakarak vücut tipime göre (ektomorf/endomorf vs.) özel tavsiyeler ekle.";
            }

            // JSON Gövdesi Hazırlığı
            var parts = new List<object> { new { text = userPrompt } };

            // Fotoğraf Yükleme İşlemi (Varsa)
            if (model.Photo != null && model.Photo.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await model.Photo.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    string base64Image = Convert.ToBase64String(imageBytes);

                    string ext = Path.GetExtension(model.Photo.FileName).ToLower();
                    string mimeType = ext == ".png" ? "image/png" : ext == ".webp" ? "image/webp" : "image/jpeg";

                    parts.Add(new { inline_data = new { mime_type = mimeType, data = base64Image } });
                }
            }

            var requestBody = new { contents = new[] { new { parts = parts.ToArray() } } };

            // İsteği Gönderme
            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                try
                {
                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        dynamic result = JsonConvert.DeserializeObject(responseString);

                        if (result?.candidates != null && result.candidates.Count > 0)
                        {
                            string aiText = result.candidates[0].content.parts[0].text;

                            // Markdown temizliği (Bazen ```html ile sarılı gelir, temizliyoruz)
                            aiText = aiText.Replace("```html", "").Replace("```", "");

                            model.AiResponse = aiText;
                        }
                        else
                        {
                            model.AiResponse = "<div class='alert alert-warning'>Yapay zeka boş cevap döndü. Lütfen tekrar deneyin.</div>";
                        }
                    }
                    else
                    {
                        model.AiResponse = $"<div class='alert alert-danger'>Hata oluştu. Kod: {response.StatusCode}</div>";
                    }
                }
                catch (Exception ex)
                {
                    model.AiResponse = $"<div class='alert alert-danger'>Bağlantı hatası: {ex.Message}</div>";
                }
            }

            return View(model);
        }

        // YARDIMCI METOT: Google'daki aktif modelleri listeler
        private async Task<List<string>> GetAvailableModelNamesAsync(string apiKey)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
            var list = new List<string>();

            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        dynamic result = JsonConvert.DeserializeObject(json);

                        if (result.models != null)
                        {
                            foreach (var m in result.models)
                            {
                                string name = (string)m.name;
                                string supportedMethods = m.supportedGenerationMethods?.ToString() ?? "";

                                if (supportedMethods.Contains("generateContent"))
                                {
                                    list.Add(name);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Hata olursa boş liste döner, kod varsayılanı kullanır.
                }
            }
            return list;
        }

        // DEBUG METODU (Opsiyonel)
        [HttpGet]
        public async Task<IActionResult> CheckModels()
        {
            try
            {
                string apiKey = _configuration["GeminiSettings:ApiKey"];
                string url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    var json = await response.Content.ReadAsStringAsync();
                    return Content(json, "application/json");
                }
            }
            catch (Exception ex)
            {
                return Content($"Hata: {ex.Message}");
            }
        }
    }
}
