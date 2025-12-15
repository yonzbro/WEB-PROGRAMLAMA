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
                model.AiResponse = "Hata: API Key 'appsettings.json' içinde bulunamadı.";
                return View(model);
            }

            // =========================================================================
            // AKILLI MODEL SEÇİMİ (429 Hatasını Çözmek İçin)
            // =========================================================================
            // Varsayılan olarak en güvenli modeli seçiyoruz.
            string modelId = "gemini-1.5-flash";

            try
            {
                // Google'a soruyoruz: "Hangi modellerin var?"
                var availableModels = await GetAvailableModelNamesAsync(apiKey);

                if (availableModels != null && availableModels.Count > 0)
                {
                    // 1. ÖNCELİK: İsmi tam olarak "gemini-1.5-flash" olanı bul. (En yüksek kota bunda)
                    var bestModel = availableModels.FirstOrDefault(m => m.Equals("gemini-1.5-flash", StringComparison.OrdinalIgnoreCase));

                    // 2. ÖNCELİK: Eğer o yoksa, isminde "flash" geçen en yeni modeli bul.
                    if (string.IsNullOrEmpty(bestModel))
                    {
                        bestModel = availableModels.FirstOrDefault(m => m.Contains("flash", StringComparison.OrdinalIgnoreCase));
                    }

                    // 3. ÖNCELİK: Flash yoksa, mecburen "pro" modeline bak (Kota riski var ama hiç yoktan iyidir)
                    if (string.IsNullOrEmpty(bestModel))
                    {
                        bestModel = availableModels.FirstOrDefault(m => m.Contains("pro", StringComparison.OrdinalIgnoreCase));
                    }

                    // 4. ÖNCELİK: Hiçbiri yoksa listedeki ilkini al.
                    if (string.IsNullOrEmpty(bestModel))
                    {
                        bestModel = availableModels.First();
                    }

                    // URL'de kullanmak için "models/" kısmını temizliyoruz.
                    modelId = bestModel.Replace("models/", "");
                }
            }
            catch
            {
                // Liste çekemezse kod patlamaz, en başta tanımladığımız "gemini-1.5-flash" ile devam eder.
            }

            // 2. Çalışan model ID'si ile URL oluşturuluyor
            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";

            // 3. Prompt Hazırlığı
            string userPrompt = $"Ben {model.Age} yaşında, {model.Weight} kilo, {model.Height} cm boyunda bir {model.Gender}. " +
                                $"Hedefim: {model.Goal}. " +
                                $"Bana maddeler halinde kısa ve etkili bir haftalık antrenman ve beslenme tavsiyesi verir misin? Türkçe olsun.";

            if (model.Photo != null && model.Photo.Length > 0)
            {
                userPrompt += " Ayrıca yüklediğim fotoğrafıma bakarak vücut tipime göre daha detaylı önerilerde bulunabilir misin?";
            }

            // JSON Gövdesi Hazırlama
            var parts = new List<object> { new { text = userPrompt } };

            // Fotoğraf Ekleme (Varsa)
            if (model.Photo != null && model.Photo.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await model.Photo.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    string base64Image = Convert.ToBase64String(imageBytes);

                    string mimeType = model.Photo.ContentType;
                    // MIME Type bulamazsa uzantıdan tahmin et
                    if (string.IsNullOrEmpty(mimeType))
                    {
                        string extension = Path.GetExtension(model.Photo.FileName).ToLower();
                        mimeType = extension switch
                        {
                            ".jpg" or ".jpeg" => "image/jpeg",
                            ".png" => "image/png",
                            ".webp" => "image/webp",
                            _ => "image/jpeg"
                        };
                    }

                    parts.Add(new
                    {
                        inline_data = new
                        {
                            mime_type = mimeType,
                            data = base64Image
                        }
                    });
                }
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = parts.ToArray() }
                }
            };

            // 5. İsteği Gönderme
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
                            model.AiResponse = aiText;
                        }
                        else
                        {
                            model.AiResponse = $"AI ({modelId}) boş cevap döndü.";
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        model.AiResponse = $"Hata oluştu.\nKullanılan Model: {modelId}\nDurum Kodu: {response.StatusCode}\nDetay: {errorContent}";
                    }
                }
                catch (Exception ex)
                {
                    model.AiResponse = "Bağlantı hatası: " + ex.Message;
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
                                // Sadece "generateContent" destekleyenleri alalım
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

        // DEBUG METODU: Tarayıcıdan modelleri manuel görmek istersen: /Ai/CheckModels
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
