# Spor Salonu (Fitness Center) Yönetim ve Randevu Sistemi

Bu proje, 2025-2026 Güz Dönemi Web Programlama dersi kapsamında geliştirilmiş bir ASP.NET Core MVC uygulamasıdır. Spor salonlarının yönetimi, üye takibi, antrenör randevu sistemi ve yapay zeka destekli egzersiz önerileri sunmayı amaçlamaktadır.

## 🎯 Projenin Amacı
Spor salonu üyelerinin antrenörlerden kolayca randevu alabilmesini sağlamak, salon yönetimini dijitalleştirmek ve üyelere kişiselleştirilmiş spor deneyimi sunmaktır.

## 🚀 Özellikler

### 1. Yönetim Paneli (Admin)
* **Spor Salonu Yönetimi:** Salon bilgileri, çalışma saatleri ve adres tanımlamaları.
* **Antrenör Yönetimi:** Antrenörlerin uzmanlık alanları (Fitness, Yoga, Pilates vb.) ve müsaitlik saatlerinin ayarlanması.
* **Hizmet Yönetimi:** Sunulan hizmetlerin süre ve ücret bilgileriyle tanımlanması.
* **Raporlama:** Üye ve randevu istatistiklerinin görüntülenmesi.

### 2. Üye İşlemleri
* **Randevu Sistemi:** Üyeler, uygun antrenör ve hizmete göre randevu alabilir. Sistem, çakışan saatlerde uyarı verir (Conflict Detection).
* **Yapay Zeka Desteği (AI):** Kullanıcılar vücut tipi ve hedeflerini girerek yapay zeka destekli (OpenAI/External API) egzersiz ve diyet önerisi alabilir.
* **Profil Yönetimi:** Geçmiş ve gelecek randevuların takibi.

### 3. Teknik Özellikler
* **REST API:** Verilere dışarıdan erişim için LINQ filtrelemeli API uçları.
* **Rol Bazlı Yetkilendirme:** Admin ve Üye rolleri ile güvenli erişim (ASP.NET Core Identity).
* **Veri Doğrulama:** Hem istemci (Client-side) hem sunucu (Server-side) doğrulamaları.

## 🛠️ Kullanılan Teknolojiler
* **Framework:** ASP.NET Core MVC (LTS)
* **Dil:** C#
* **Veritabanı:** SQL Server / Entity Framework Core (Code First)
* **Front-End:** HTML5, CSS3, JavaScript, Bootstrap 5
* **AI Entegrasyonu:** OpenAI API (veya muadili)

## ⚙️ Kurulum ve Çalıştırma

1.  Projeyi klonlayın:
    ```bash
    git clone [https://github.com/YONZBRO/WEB-PROGRAMLAMA.git](https://github.com/yonzbro/WEB-PROGRAMLAMA.git)
    ```
2.  `appsettings.json` dosyasındaki Connection String'i kendi veritabanı sunucunuza göre düzenleyin.
3.  Migration işlemlerini uygulayın (Package Manager Console):
    ```powershell
    Update-Database
    ```
4.  Projeyi çalıştırın.

## 🔑 Varsayılan Giriş Bilgileri (Seed Data)

Proje ilk kez çalıştırıldığında veritabanına otomatik olarak aşağıdaki Admin kullanıcısı eklenir:

* **Email:** `ogrencinumarasi@sakarya.edu.tr`
* **Şifre:** `sau`



