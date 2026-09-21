// Şirket içi mesajlar (örnek veri). Ana sayfa (index.html) ve mesaj ekranı (mesajlar.html) ortak kullanır.
// tip: duyuru | grup | dm. gecmis: [{ kim, txt, z }]  ("Ben" = oturum kullanıcısı)
window.MESAJLAR = [
  { tip: 'duyuru', av: '📣', cls: 'p5', ad: 'Genel Duyurular', yeni: 3, z: '10:24', y: true, gecmis: [
    { kim: 'İK Departmanı', txt: 'Eylül ayı doğum günü kutlaması cuma 16:00\'da yemekhanede.', z: 'Dün 15:10' },
    { kim: 'İK Departmanı', txt: 'Servis güzergâhları güncellendi, intranetten inceleyebilirsiniz.', z: '09:55' },
    { kim: 'İK Departmanı', txt: 'Yeni ofise taşınma takvimi paylaşıldı. Departman bazlı taşınma günleri için ekteki planı inceleyin.', z: '10:24' }] },
  { tip: 'duyuru', av: 'BT', cls: 'p4', ad: 'Bilgi Teknolojileri Duyurusu', yeni: 1, z: '09:40', y: true, gecmis: [
    { kim: 'BT', txt: 'Cumartesi 22:00 - 02:00 arasında sistem bakımı yapılacaktır; ERP ve e-posta erişimi kesintili olacaktır.', z: '09:40' }] },
  { tip: 'duyuru', av: 'İK', cls: 'p2', ad: 'İK Duyurusu', yeni: 0, z: 'Dün', gecmis: [
    { kim: 'İK', txt: 'Cumhuriyet Bayramı çalışma takvimi: 28 Ekim yarım gün, 29 Ekim tam gün tatil.', z: 'Dün 11:20' }] },
  { tip: 'grup', av: 'UH', cls: 'p4', ad: 'Uras Holding', yeni: 1, z: '09:18', y: true, gecmis: [
    { kim: 'Sedef Yılmaz', txt: 'Günaydın, yönetim toplantısı 10:00\'da başlıyor.', z: '08:45' },
    { kim: 'Ben', txt: 'Teşekkürler, katılıyorum.', z: '08:47' },
    { kim: 'Sedef Yılmaz', txt: 'Yönetim toplantısı notlarını ekledim, aksiyon maddeleri için sorumlular atandı.', z: '09:18' }] },
  { tip: 'dm', av: 'MÖ', cls: 'p6', ad: 'Murat Özavşar', yeni: 1, z: '08:52', y: true, gecmis: [
    { kim: 'Murat Özavşar', txt: 'ABC Tekstil dosyasında arabulucudan yeni teklif geldi.', z: '08:40' },
    { kim: 'Ben', txt: 'Gördüm, 850.000 ₺ / 6 taksit önerisi mi?', z: '08:44' },
    { kim: 'Murat Özavşar', txt: 'Öğleden sonra ABC Tekstil sulh teklifini birlikte değerlendirelim mi?', z: '08:52' }] },
  { tip: 'grup', av: '₺', cls: 'p2', ad: 'Finans Grubu', yeni: 0, z: 'Dün', gecmis: [
    { kim: 'Hakan Ayyürek', txt: 'Eylül tahsilat planı güncellendi; vadesi geçen bakiyeler için hukuk ile görüşülecek.', z: 'Dün 16:30' }] },
  { tip: 'grup', av: '⚙', cls: 'p3', ad: 'Operasyon', yeni: 0, z: 'Dün', gecmis: [
    { kim: 'Sedef Yılmaz', txt: 'Sevkiyat planı güncellendi, Almanya sevkiyatı perşembeye alındı.', z: 'Dün 14:05' }] },
  { tip: 'dm', av: 'SK', cls: 'p1', ad: 'Sevgi Kaya', yeni: 0, z: 'Dün', gecmis: [
    { kim: 'Sevgi Kaya', txt: 'Ankara bayilik sözleşmesi karşı taraf imzasına gönderildi, teşekkürler.', z: 'Dün 17:40' }] },
  { tip: 'grup', av: '♥', cls: 'p1', ad: 'Sosyal Kulüp', yeni: 0, z: '14 Eyl', gecmis: [
    { kim: 'Ayşegül Ula', txt: 'Hafta sonu etkinliği için önerilerinizi cuma gününe kadar bekliyoruz.', z: '14 Eyl' }] }
];
window.MESAJLAR.forEach(m => { const s = m.gecmis[m.gecmis.length - 1]; m.son = (s.kim === 'Ben' ? 'Ben: ' : (m.tip === 'dm' ? '' : s.kim + ': ')) + s.txt; });
window.MESAJ_YANITLARI = ['Teşekkürler, inceleyip döneceğim.', 'Tamam, not aldım. Bugün içinde ilerletirim.', 'Anlaşıldı, ilgili ekiple paylaşıyorum.', 'Uygun, öğleden sonra kısa bir görüşme ayarlayalım.', 'Detayları e-posta ile de iletiyorum.'];
