# MegaChaos Mod - Yol Haritası (Roadmap)

MegaChaos projesinin gelecekteki güncellemeleri için planlanan özellikler ve hedefler sürüm sürüm aşağıda listelenmiştir.

## v1.2.0 - Arayüz ve Yaşam Kalitesi (QoL) Güncellemesi
Bu sürüm, oyuncuların modla etkileşimini kolaylaştırmaya ve arayüzü daha kullanıcı dostu hale getirmeye odaklanır.
- **Profil ve Preset Sistemi:** Kural setlerini kaydetme ve "Zor Mod", "Kaos Modu" gibi profiller arası oyun içi tek tuşla geçiş yapabilme.
- **Sürükle-Bırak Sıralama:** Kural menüsünde kuralları sürükle-bırak ile yeniden sıralayabilme.
- **Oyun Sonu İstatistikleri:** Hangi kuralın kaç kez çalıştığını ve toplamda neler kazanıldığını gösteren detaylı log/istatistik ekranı.
- **Gamepad Desteği:** F8 menüsü ve tüm arayüz bileşenlerinin oyun kumandası (gamepad) ile tam uyumlu çalışması.
- **UI Performans İyileştirmeleri:** Çok fazla kural olduğunda menüde yaşanan kasmaları önlemek için Object Pooling vb. arayüz optimizasyonları.

## v1.3.0 - Gerçek Kaos (The Chaos Update)
Bu sürüm, modun adının hakkını vererek oyuna eşya vermek dışında gerçek "kaos" unsurlarını ve yeni tetikleyicileri dahil eder.
- **Yeni Aksiyonlar (Eşya Dışında):**
  - **Stat Değişimleri (Buff/Debuff):** 30 saniyeliğine +%50 hızlanma, hasar artışı veya can düşüşü gibi geçici etkiler.
  - **Tehlike Doğurma (Spawn):** Ödül olarak aniden etrafta düşmanların, elit yaratıkların veya tuzakların belirmesi.
  - **Altın/XP Ödülleri:** Doğrudan oyun içi altın veya deneyim puanı verebilme.
- **Yeni Tetikleyiciler (Triggers):**
  - **Alınan Hasar:** Toplamda belirli bir hasar alındığında tetiklenme.
  - **Hasarsız Geçiş:** Bölümü veya boss savaşını hiç hasar almadan tamamlama.
  - **Belirli Düşman/Elit Kesimi:** Sadece belirli tip bir düşman kesildiğinde çalışan kurallar.

## v1.4.0 - Gelişmiş Mekanikler (Advanced Mechanics)
Kuralların derinliğini artırarak oyunculara daha kompleks senaryolar yaratma imkanı sunan sürüm.
- **Bileşik Koşullar (AND/OR Logic):** Kuralları birden fazla şarta bağlama. (Örn: Can <%30 VE Boss Kesilirse).
- **Kademeli Şans (Escalating Odds / Pity):** Şanslı kurallarda "None" (Şanssızlık) geldiğinde, bir sonraki seferde başarı şansının otomatik artması.
- **Eşya Kısıtlamaları:** Oyuncunun envanterinde zaten bulunan bir eşyanın tekrar verilmesini engelleme ve yerine alternatif eşya atama.

## v2.0.0 - Ekosistem ve Entegrasyon
Modu sadece yerel bir deneyim olmaktan çıkarıp daha geniş bir topluluk ve yayıncı aracına dönüştürme adımı.
- **Twitch / Yayıncı Entegrasyonu:** Twitch izleyicilerinin chat üzerinden komut yazarak (Örn: `!drop`, `!spawn`) oyundaki kuralları dışarıdan tetikleyebilmesi.
- **Modüler Tetikleyici Sistemi (Mod API):** Diğer mod geliştiricilerinin kendi modlarındaki eşyaları ve olayları `MegaChaos` içerisine kolayca ekleyebileceği açık bir API altyapısı.
- **Dış Mod Uyumluluğu:** Oyuna sonradan eklenmiş özel (custom) mod eşyalarının MegaChaos tarafından otomatik algılanıp ödül listesine dahil edilmesi.
