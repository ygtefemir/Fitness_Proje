# FitLife - Spor Salonu Yönetim ve Randevu Sistemi

**FitLife**, spor salonu yöneticileri (Admin) ile üyeler arasındaki etkileşimi dijitalleştiren, ASP.NET Core 8.0 MVC mimarisi ile geliştirilmiş kapsamlı bir yönetim sistemidir.

Bu proje, **Sakarya Üniversitesi Bilgisayar Mühendisliği** Web Programlama dersi projesi olarak geliştirilmiştir.

## Öne Çıkan Özellikler

* [cite_start]**Dinamik Randevu Sistemi:** Üyeler; salon, eğitmen ve hizmet seçimi yaparak, eğitmenin mesai saatleri ve anlık doluluk durumuna göre çakışma olmadan randevu alabilirler[cite: 13, 198].
* [cite_start]**Akıllı Çakışma Kontrolü:** Randevular arasında 15 dakikalık "tampon zaman" (mola) bırakılarak profesyonel bir planlama sağlanır [cite: 220, 305-307].
* [cite_start]**Yapay Zeka (AI) Entegrasyonu:** Entegre AI asistanı (Gemini API); kullanıcıların boy, kilo ve yaş bilgilerine göre kişisel diyet/antrenman planı sunar ve hedef görselleştirme yapar [cite: 14, 338-341, 371].
* [cite_start]**RESTful API Mimarisi:** Randevu listeleme ve eğitmen müsaitlik saatleri (slotlar), LINQ sorguları ile filtrelenen API endpoint'leri üzerinden asenkron olarak sunulur [cite: 279-281, 327-336, 431].
* **Rol Bazlı Yetkilendirme:** Sistemde 'Admin' ve 'Member' rolleri bulunur. [cite_start]Adminler tam CRUD yetkisine sahipken, üyeler randevularını yönetebilir [cite: 75, 111, 404-407].
