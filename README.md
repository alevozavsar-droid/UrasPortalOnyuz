# UrasPortalOnyuz — Rapor Portalı (YALNIZCA ÖN YÜZ)

Ana projenin (`WebApplication3`) ekranlarını **veritabanı, SAP Service Layer, e-posta ya da başka bir dış bağlantı olmadan**
gösteren örnek projedir. Bütün ekranlar `Data/OrnekVeri.cs` içindeki örnek verilerle çalışır.

## Çalıştırma

Gereken: **.NET 10 SDK** (hedef çerçeve `net10.0`; ana proje net5.0 olsa da görünümler ve tip imzaları değişmeden derlenir).

```
cd UrasPortalOnyuz
dotnet run
```

Tarayıcı: http://localhost:5300  (Visual Studio'da da açılabilir: `UrasPortalOnyuz.sln`)

Giriş: **herhangi bir kullanıcı kodu + herhangi bir şifre** (ör. `it02` / `x`). Şifre denetimi yoktur.
Örnek kullanıcı kodları ve adları `OrnekVeri.Kullanicilar` listesinde.

## Menü ve tasarım (Grup ERP Portalı uyarlaması)

Sol menü ve üst çubuk, **Grup ERP Portalı** (`source/repos/WebApplication1`, `wwwroot/menu-config.js` + `index.html` + `modul.html`)
tasarımından uyarlandı:

* **Sol menü** düz modül listesidir: Ana Sayfa, Talep, Onaylarım, Mali İşler, Satış ve Operasyon, Marka ve İletişim, Tedarik Zinciri,
  Üretim ve Kalite, İK ve İdari İşler, Bilgi Teknolojileri, Hukuk, Yönetim, Takvim, Ayarlar. Favoriler, IT Destek, Sistem Yetkileri
  ve Uras Üretim Portalı bağlantıları korunur. Menü sabittir (daralıp genişlemez).
* **Modül sayfası** `/Modul/{key}` (`Controllers/ModulController.cs`, `Views/Modul/Index.cshtml`, `wwwroot/js/kurumsal-modul.js`):
  solda 1. alt menü → 2. alt menü → rapor / ekran akordeonu; sağda **organizasyon şeması** (modül → 1. alt menü kolonları →
  2. alt menü kutuları → rapor satırları; hazır / kısayol / planlanıyor renkli nokta). Gösterge panelleri (KPI, trend, bekleyen işler)
  kaldırıldı. 1. alt menü seçilince o kırılımın şeması, süreç grubu seçilince şema + filtrelenebilir rapor listesi gelir.
  Hazır ekranlar sayfa içinde çerçevede açılır; "Sekmede aç" kabuğun sekmesine taşır.
  Derin bağlantı: `/Modul/mali-isler#finans/finansal-araclar/r0` ya da `/Modul/mali-isler?yol=finans/finansal-araclar/r0`.
* **İK ve İdari İşler talepleri** (Talep › İK ve İdari İşler): İzin, Mesai, Avans, Belge, Seyahat ve Araç talep formları
  `Controllers/IkTalepController.cs` + `Views/IkTalep/Form.cshtml` (tek görünüm, alan tanımına göre çizilir). Sol menüde kırılım yoktur
  (`KmGrup.Dogrudan`): başlığa tıklayınca İzin Talebi açılır, diğer türler ekranın üstündeki sekmelerden seçilir. Gönderilen talepler
  bellek içinde tutulur (`/IkTalep/Gonder` JSON, `/IkTalep/Liste`), sağdaki "Son Taleplerim" listesine düşer; onay akışı örnek metindir.
* **Nakit Avans modülü** (Avans Talebi; `Views/IkTalep/Avans.cshtml`, `wwwroot/js/nakit-avans.js`, kurallar ve örnek veri
  `Data/NakitAvansOrnek.cs`): "Nakit Avans Talep, Ödeme, Mahsup ve Kapatma Modülü — IT Analiz ve Geliştirme Gereksinimleri v1.0"
  şartnamesine göre yazıldı. Tek ekranda §25.2 gösterge kutucukları ve sekmeler: Yeni Talep (talep üst bilgileri, oluşturan / kullanacak
  kişi İK'dan otomatik, masraf merkezi kullanacak kişiden, açık avans kontrolü, grup içi uyarı, parametrik onay akışı önizlemesi),
  Taleplerim, Talep Detayı (üç ayrı statü: Talep / Ödeme / Kapatma; onay süreci ve Onayla / Reddet / Revizyona Gönder; muhasebe yansıması;
  finans teslim bilgileri + teslim teyidi; Açık = Teslim − Mahsup − İade özeti; harcama belge satırları; iadeler; timeline; ekler; audit log),
  Finans › Bekleyen Ödemeler (`/IkTalep/AvansBekleyenOdemeler`, menüde Mali İşler › Finans › Ödeme Yönetimi) ve Takip Raporu
  (`/IkTalep/AvansTakip`). Finansal tutarlar ekrandan girilmez: detaydaki "Kaynak Sistem Olayı" paneli §30 olaylarını
  (CashPaymentCreated, ExpensePosted, JournalPosted, CashReturned, ters kayıtlar, IntercompanyPosted) Transaction ID ile üretir;
  aynı ID ikinci kez yok sayılır, Teslim = Mahsup + İade olunca sistem kapatır, tutar revizesinde onay matrisi yeniden hesaplanır.
  Onay limitleri ve açık avans politikası ekrandaki Parametreler kartından değiştirilir. Kayıtlar bellek içindedir.
* **İzin modülü** (İzin Talebi; `Views/IkTalep/Izin.cshtml`, `wwwroot/js/izin-talep.js`, kurallar ve örnek veri `Data/IzinOrnek.cs`;
  ortak stiller `wwwroot/css/nakit-avans.css`): Nakit Avans ile aynı mimari. İzin türü master'ı (yıllık, mazeret, raporlu, ücretsiz,
  doğum, babalık, evlilik, vefat, süt, idari; ücret / bakiye / belge / onay akışı / yasal dayanak), kıdeme göre yıllık izin hak edişi
  (14 / 20 / 26 gün) ve devir, bakiye hareketleri (hak ediş, devir, kullanım, planlanan, iptal iadesi), iş günü hesabı (hafta sonu ve
  2026 resmi tatilleri hariç, arife yarım gün), çakışma / vekil / max gün / bildirim süresi kontrolleri, parametrik onay akışı (uzun izin
  ve departman doluluğunda Direktör, ücretsizde İK, yetersiz bakiyede avans izin politikası), üç ayrı statü (Talep / Kullanım / Puantaj),
  ekip izin takvimi, onay bekleyenler kuyruğu (hızlı onay / red), takip raporu. Kullanım ve puantaj bilgileri PDKS / SGK / Bordro
  olaylarıyla (IzinBasladi, IzinBitti erken dönüş, RaporBildirimi, BelgeAlindi, PuantajAktarildi, ters kayıt) Talep No üzerinden işlenir;
  başlamış izin iptal edilemez, revizyonda onay akışı yeniden hesaplanır. Rotalar: `/IkTalep/Izin`, `IzinTakvim`, `IzinOnay`, `IzinTakip`
  (menüde İK ve İdari İşler › İzin ve Devamlılık ile Onaylarım › İK Onayları).
* **Mesai, Belge, Seyahat ve Araç modülleri** (ortak "talep motoru": `Data/TalepMotoru.cs` çekirdek, `Data/TalepModulleri.cs` modül
  tanımları; ekran `Views/IkTalep/Modul.cshtml` + `wwwroot/js/talep-modulu.js`, modül tanımı `/IkTalep/TmVeri?kod=` ile gelir): Avans ve
  İzin ile aynı kalıp — gösterge kutucukları, parametrik onay akışı (Y/D/A/I/F adımları), üç statü (Talep + modüle özel iki kaynak sistem
  statüsü), kilitli sistem hesabı, Transaction ID'li kaynak sistem olayları, timeline, audit, revizyon, iptal, takvim, onay kuyruğu, takip
  raporu. Modül kuralları: **Mesai** (planlanan saat / çarpan / serbest zaman, 270 saat yıllık ve günlük sınır, izin çakışması, PDKS
  gerçekleşme, bordro puantajı), **Belge** (SLA hedefi iş günü, KVKK rızası, kargo adresi, hazırlık → imza → belge no → teslim → teyit),
  **Seyahat** (gün / gece, harcırah parametrik TL / EUR, bütçe eşiğinde Direktör, pasaport / vize, izin ve seyahat çakışması, bilet / otel /
  avans / masraf formu / kapanış), **Araç** (filo master, ehliyet ve ceza puanı, müsaitlik, hafta sonu ve uzun tahsis onayları, tahsis →
  anahtar teslim → iade km-yakıt-hasar → HGS/ceza → kapanış). Ek rotalar: `MesaiTakip`, `SeyahatTakvim`, `AracTakvim`, `TalepOnay?kod=`.
* **Grup İçi Özel Finansal İşlemler** (Mali İşler › Finans → Muhasebe → Cari İşler → Grup İçi Özel Finansal İşlemler; `Controllers/GrupIciController.cs`,
  tanımlar `Data/TalepModulleri.cs` YANSITMA / MAHSUP / TEMLIK, ortak ekran `Views/IkTalep/Modul.cshtml`, rapor `Views/GrupIci/Rapor.cshtml`):
  **Yansıtma İşlemleri** (şirketler arası masraf / hizmet / personel / kira yansıtması; KDV ve toplam sistem hesabı, mükerrer dönem engeli,
  eşikte Direktör; e-fatura → karşı kabul/red → iki tarafta fiş → mutabakat → kapanış), **Mahsup İşlemleri** (ERP cari bakiyeleriyle sınırlı
  karşılıklı / üçlü / avans / fatura mahsubu; A-B fişleri, mutabakat farkı, kapanış), **Temlik İşlemleri** (alacağın grup şirketine devri;
  Muhasebe → Hukuk → (Direktör) → Finans, sözleşme, borçluya bildirim (TBK m.186), teyit, tahsilat, kapanış) ve **Grup İçi Özel Finansal
  İşlemler Raporu** (üç modülün birleşik listesi, şirket bazında açık tutarlar, ERP grup içi bakiye tablosu `TalepModulleri.GrupBakiyeleri`).
  **Sekme yapısı** (GRUP_ICI_OZEL_FINANSAL_ISLEMLER_SEKME_YAPISI şartnamesi): Yansıtma / Mahsup / Temlik ekranlarının üstünde aynı
  sayfada üç sekme — *İşlem Ekranı* (varsayılan; talep motoru), *Raporu* (modülün gösterge kutucukları, şirket bazında açık işlemler, tür
  dağılımı, takip sütunlarıyla liste) ve *Muhasebe Kontrolü* (fiş / fatura / sözleşme / mutabakat / kapanış kontrolü, işlem başına kontrol
  sonucu, kaynak sistem olay kaydı, kontrol adımları). Rapor ve kontrol ayrı menü değildir; `?ust=rapor|kontrol` ile doğrudan açılır.
  Betik `wwwroot/js/grup-ici-sekmeler.js`; kontrol kuralları modül başına `KONTROL` tablosundadır. Ekranlar arası geçiş sol menüden yapılır.
* **Yansıtma Modülü** (Yansıtma İşlemleri; `Controllers/GrupIciController.Yansitma` → `Views/GrupIci/Yansitma.cshtml`, betik
  `wwwroot/js/yansitma.js`, veri uçları `Controllers/YansitmaController.cs`, kurallar ve örnek kayıtlar `Data/YansitmaOrnek.cs`):
  "URAS HOLDING Yansıtma Modülü — IT Teknik ve İşlevsel Gereksinim Dokümanı v1.0 (21.09.2026)" uyarlaması; talep motorundan ayrıldı
  (Mahsup ve Temlik talep motorunda kaldı; birleşik Grup İçi raporu eski YANSITMA tanımını kullanmaya devam eder). Üç sekme:
  *İşlem Ekranı* — Kaynak Fatura Seç penceresi (yansıtan şirkete göre filtreli, sunucu tarafı arama / sayfalama; kaynak faturalar Aylık
  Alım Onay ekranındaki tedarikçi faturaları), **fatura kalemleri tek tek işaretlenir**, satır bazında kalan miktar (aynı belge id + satır
  no'dan daha önce yansıtılan düşülür, mükerrer engeli V04/V15), kısmi miktar, toplu ve satır bazında kâr %, yetkiliyse manuel birim
  yansıtma (kâr % geriye hesaplanır), KDV, masraf merkezi, satırı birden fazla hedef şirkete dağıtma; sağ sabit panelde özet (ağırlıklı
  kâr %), validasyon (V01–V15) ve parametrik onay akışı (Muhasebe → (Direktör: tutar eşiği / kâr üst sınırı / negatif kâr / hizmet) →
  Finans); alt panelde iki şirketin beklenen yevmiye kaydı (hesap kodları `YansitmaOrnek.Esleme` eşleme tablosundan, 120/320 cari, 191/391
  KDV; onaya gönderme anında snapshot). Taslak kaydet, onaya gönder, onayla / reddet (ret nedeni zorunlu), geri çek, idempotent ERP fatura
  oluşturma, iptal (ERP belgesi varsa ters kayıt), audit log (eski / yeni değer), Excel (CSV) / PDF (yazdır). Rol demo amaçlı ekrandan
  seçilir (§10 yetki matrisi). *Raporu* — §6.1 filtreleri (çoklu seçim), §6.4 KPI'lar, §6.2 grid, satırda detay drawer, Excel.
  *Muhasebe Kontrolü* — "Gerçek fişi ERP'den oku" olayı (demo senaryolar: uyumlu, hesap, KDV, tutar, cari, masraf merkezi, referans,
  mükerrer, kur, fiş yok) beklenen snapshot ile gerçek satırları alan alan karşılaştırır (borç = alacak, net, KDV, hesap, cari, masraf
  merkezi, proje, para birimi, kur, belge referansı, mükerrer, şirketler arası karşı kayıt); §7.4 durumları öncelik sırasıyla verilir,
  UYUMLU olunca işlem "Uyumlu" olur. Onaylı yansıtma ERP belgesi alınca kaynak alım faturasının "Yansıtıldı" rozeti bu no'yu gösterir.
  `?ust=rapor|kontrol` ve `?no=YNS-000125` ile doğrudan açılır.
* **Temlik Modülü** (Temlik İşlemleri; `Controllers/GrupIciController.Temlik` → `Views/GrupIci/Temlik.cshtml`, betik `wwwroot/js/temlik.js`,
  veri uçları `Controllers/TemlikController.cs`, kurallar ve örnek kayıtlar `Data/TemlikOrnek.cs`): "URAS HOLDING Temlik Modülü — IT Teknik ve
  İşlevsel Gereksinim Dokümanı v1.0 (21.09.2026)" (çok taraflı temlik zinciri + 2–10 taraf + N'li virman + Temlik Belgesi) uyarlaması; talep
  motorundan ayrıldı (Mahsup talep motorunda kaldı). *İşlem Ekranı* — Kaynak Alacak Seç penceresi (satış faturası / cari açık kalem / sözleşme;
  `TemlikOrnek.KaynakAlacaklar`, kaynak şirkete göre filtre, sunucu tarafı arama / sayfalama, birden fazla belge aynı zincire), **belge satırları
  tek tek işaretlenir**, satır bazında temlik edilebilir bakiye (nominal − kapanan − aktif / rezerve temlik; taslak ve onay süreci de rezervasyon)
  ve kısmi "bu işlem tutarı"; Taraf Sayısı (2–10) seçimi, + Taraf Ekle / Tarafı Çıkar / sıra değiştirme, taraf tipi (grup şirketi / tedarikçi /
  müşteri / diğer, master `TemlikOrnek.Taraflar`), zincir rolü, aldığı / devrettiği / kalan hak; N−1 transfer step (step tutarı önceki adımı
  aşamaz, ara taraf kısmi devrederse kalan hak açık gösterilir), Temlik Dağılımını Oluştur; sağ sabit özet, V01–V23 validasyonu, parametrik onay
  akışı (Muhasebe → Hukuk → (Direktör: tutar / taraf sayısı / teminat) → Finans); step bazlı muhasebe simülasyonu (ASSIGN_OUT / ASSIGN_IN,
  hesaplar `TemlikOrnek.Esleme` TEMLIK_HESAP_ESLEME tablosundan, ERP dışı taraflar bilgi satırı, konsolide giren / çıkan / net görünüm, versiyon +
  hash snapshot). **Temlik Belgesi Oluştur**: hukuk şablonu sürümüyle belge no / versiyon / hash, zincirdeki her taraf için dinamik imza bloğu;
  belge "TEMLİK SÖZLEŞMESİ (kurumsal, final)" şablonu formatında ekrandaki verilerden otomatik doldurulur (başlık tablosu: Temlik No / Tarih /
  Tür (fatura-cari-sözleşme bazlı) / PB-Tutar; TARAFLAR: rol, büyük harf unvan, VKN, yetkili; TEMLİK DETAYI: kaynak belge / cari no, tarih,
  açıklama, açık tutar, temlik tutarı, toplam, 3+ tarafta adım tablosu; TEMLİK HÜKMÜ zincire göre yazılır; KAŞE VE İMZA taraf sayısı kadar sütun,
  imzalananlar damgalı) ve yazdırılır;
  İmzaya Gönder, taraf bazında imzala / reddet, İmzalı Belge Yükle (dosya hash), Temlik Bildirimi, Belgeyi Geçersiz Kıl, yeni versiyon. Tam
  imzalı işlemde Temliği Muhasebeleştir: step bazlı idempotent ERP kaydı (demo senaryolar: uyumlu, step 2 hatası → Kısmi Muhasebeleşti ve
  sonraki step bloke, yalnız başarısız stepi tekrar dene, hesap / tutar / ara taraf / nihai hak / mükerrer / referans / kur sapmaları), iptal
  (rezervasyon serbest) ve ters kayıt (son stepten ilk stepe), audit log. Kuyruklar: onay bekleyenler, imza takibi. *Raporu* — §10.1 filtre
  grupları (tarih türü, taraf, nihai alan, borçlu, şirket, belge no'ları, tür / PB / kısmi-tam / taraf sayısı, durum / belge / muhasebe / kontrol,
  kullanıcı), §10.4 KPI'lar, §10.2 grid, zincir detay drawer, rapor ve zincir detay Excel'i. *Muhasebe Kontrolü* — step bazlı kontrol listesi,
  beklenen–gerçekleşen karşılaştırması (CompanyCode, step, hesap rolü / kodu, borç / alacak, karşı taraf, kaynak ref, PB / kur, Temlik / Zincir /
  Step ID), zincir bütünlüğü (step sayısı, step k çıkan = k+1 giren, ara taraf net hakkı, nihai temlik alan ve tutarı, kaynak aktif temlik
  sınırı, imzalar, ERP tamamlanma, grup içi mirror cari), §11.5 durumları öncelik sırasıyla, fark kapatma, kurallar ve eşleme tablosu.
  Örnek kayıtlar §23 senaryolarını içerir (TML-00045 A Tedarikçisi → ALV → URAS 1.000.000 uyumlu; TML-00046 ALV → URAS 700.000 ile ara tarafta
  300.000 kalan ve step 2 ERP hatası; TML-00047 4 taraflı; 5 taraflı zincir; ters kayıt; EUR temlik). Rol demo amaçlı ekrandan seçilir (§14).
* **Mahsup Modülü** (Mahsup İşlemleri; `Controllers/GrupIciController.Mahsup` → `Views/GrupIci/Mahsup.cshtml`, betik `wwwroot/js/mahsup.js`,
  veri uçları `Controllers/MahsupController.cs`, kurallar ve örnek kayıtlar `Data/MahsupOrnek.cs`): "URAS HOLDING Mahsup Modülü — IT Teknik ve
  İşlevsel Gereksinim Dokümanı v1.0 (21.09.2026)" uyarlaması; talep motorundan ayrıldı (Grup İçi birleşik raporu eski tanımları kullanmaya devam
  eder). *İşlem Ekranı* — Açık Kalemleri Getir (şirket, kapsam / karşı şirket, cari ve mahsup türüne göre ERP açık kalemleri
  `MahsupOrnek.AcikKalemler`; kapalı belgeler opsiyonla; sunucu tarafı filtre), yan yana **Borç ve Alacak açık kalem panelleri** (kalemler tek tek
  işaretlenir; orijinal, kapanan, rezerve, açık tutar, mahsup tutarı ≤ açık, sonrası kalan, PB / kur, TL, proje / MM, referans; panel altında
  seçilen tutar / belge sayısı / kalan), belge → belge dağılım tablosu (otomatik FIFO ya da N-N manuel; Otomatik Eşleştir: en eski vade / en eski
  belge / aynı tutar / referans bazlı ön izleme, Seçili Kalemleri Mahsup Et, Eşleştirmeyi Temizle), borç = alacak eşitlik rozeti, sağ sabit
  özet (borç / alacak toplam, fark, mahsup tutarı, kur ve yuvarlama farkı, tam / kısmi kapanan, sonrası açık), V01–V15 validasyonu, parametrik
  onay (Muhasebe → (Direktör: tutar / M06 / M07 / kur farkı üst sınırı) → Finans); simülasyon "Yeni fiş oluşacak mı? EVET / HAYIR": aynı cari
  ve aynı hesapta yalnız iç mutabakat, aksi hâlde hesap eşleme tablosundan (`MahsupOrnek.Esleme`, MAHSUP_HESAP_ESLEME) yevmiye + mutabakat
  (M03 320 Borç / 159 Alacak, M04 340 / 120, M02 320 / 120), belge kuru farklarından kur farkı (646 / 656) ya da tolerans içi yuvarlama
  satırı, M06'da karşı şirkette Grup Mahsup ID'li mirror kayıt; KDV satırı üretilmez. ERP: şirket bazlı idempotent muhasebeleştirme / iç
  mutabakat (demo senaryolar: uyumlu, karşı şirket hatası → Kısmi Entegrasyon ve yalnız başarısız tarafı tekrar, hesap, tutar, kur, açık kalem
  kalanı, mutabakat yok, mükerrer, referans, proje / MM), iptal (rezervasyon serbest), ters kayıt / unreconcile (kayıt silinmez), audit log.
  *Raporu* — §6.1 filtre grupları, §6.4 KPI'lar, §6.2 kolonları, detay drawer, rapor ve detay Excel'i. *Muhasebe Kontrolü* — §7.1 liste
  (simülasyon / ERP kaydı / iç mutabakat / kontrol), §7.2 beklenen–gerçekleşen (hesap / BP rolü, borç, alacak, kur, proje, MM, referans),
  §7.3 açık kalem kapanma (önceki açık, beklenen mahsup, beklenen kalan, ERP kalan), §7.6 mirror kontrolü, §7.5 durumları öncelik sırasıyla,
  fark kapatma, kurallar ve eşleme tablosu. Örnek kayıtlar §19 senaryolarını içerir (MHS-000125 aynı cari 1-N iç mutabakat 75.000, fiş yok;
  MHS-000126 tedarikçi avans-fatura 60.000, faturada 40.000 açık; MHS-000127 URAS ⇄ ALV 50.000 kısmi entegrasyon; M04, M02, EUR kur farkı,
  ters kayıt, hesap hatası, açık kalem farkı örnekleri).
* **Aylık Alım Onay Ekranı** (Tedarik Zinciri › Satın Alma › Fiyat ve Alım Analizi ve Mali İşler › Muhasebe › Alış ve Satış Muhasebesi;
  Onaylarım'da kısayol; `Controllers/AlimOnayController.cs`, `Views/AlimOnay/Index.cshtml`, veri ve kurallar `Data/AlimOnayOrnek.cs`):
  Muhasebe'nin yönetim için hazırladığı aylık alım raporu. Gider Onay Ekranı kalıbında: solda dönemin tedarikçi faturaları
  (Fatura Şirketi, Kullanım Şirketi, Tarih, Fatura No, Tedarikçi + VKN, Talep Eden + birim, Satınalmacı, Kategori, Sipariş / İrsaliye No,
  Net Alım, KDV, Brüt, Vade / Ödeme, Onay, Uyarı; döviz faturada TL karşılığı + döviz tutarı), sağda seçili faturanın kalem detayı
  (önceki birim fiyat ve fark yüzdesiyle), e-fatura görüntüsü (`/AlimOnay/Gorsel?id=`, çerçevede; yeni sekme / yazdır) ve onay geçmişi.
  Akış: muhasebe kaydı → satınalmacı onayı (yalnız kendi faturaları) → Tedarik Zinciri Direktörü onayı → tüm faturalar sonuçlanınca
  "Ayı kapat ve yönetime sun". Sistem uyarıları: siparişsiz fatura, irsaliyesiz mal faturası, ≥%10 fiyat artışı, kategori bütçe aşımı,
  mükerrer fatura şüphesi, vadesi geçmiş ödenmemiş, grup içi kullanım (fatura şirketi ≠ kullanım şirketi), eşik üstü tutar.
  Üstte dönem özeti (net / KDV / brüt, onay ilerlemesi) ve kategori bazında bütçe gerçekleşmesi; dönem, şirket, satınalmacı, kategori,
  uyarı filtreleri; CSV dışa aktarma. Rol (satınalmacı / direktör / muhasebe / izleyici) demo amaçlı ekrandan seçilir.
* **Genel Analiz Raporu** (Yönetim › Yönetim Özeti › Özel Analizler › Özel Raporlar; `Controllers/GenelAnalizController.cs`,
  `Views/GenelAnaliz/Index.cshtml`, yapı ve örnek veri `Data/GenelAnalizOrnek.cs`): "2026 Aylık Analiz Raporu" Excel şablonunun
  portal karşılığı. Satırlar şablondan: CİRO (URAS / ALV / Avrupa Paper / DAF / URS alt toplamları, GES, TOPLAM CİRO), ALIMLAR,
  GİDERLER, ARA SONUÇ (Alım + Gider, Faaliyet Kâr / Zararı), AMORTİSMAN, ARA SONUÇ (Dönem Net), KUR FARKI, SONUÇ (Kur Farkı Sonrası).
  Sütunlar Ocak–Aralık, TOPLAM, AYLIK ORT. (Excel'deki `IF(COUNT()=0,"",SUM/AVERAGE)` mantığı: boş aylar sayılmaz). Yaprak hücreler
  yazılabilir, alt toplam / sonuç satırları terim listesinden (işaretli toplam) anında hesaplanır; Kaydet bellek içine yazar,
  `/GenelAnaliz/Csv?yil=` şablonla aynı düzende CSV verir. Kapanmış aylar örnek veriyle dolu gelir.
* **Cari Kart ve Faaliyet Eşleştirme** (Müşteri / Tedarikçi Cari Kartları ve Müşteri / Tedarikçi Talep Formu; `Controllers/CariKartController.cs`,
  `Views/CariKart/Kartlar.cshtml` + `Talep.cshtml`, betik `wwwroot/js/cari-kart.js`, stil `wwwroot/css/cari-kart.css`, kurallar ve örnek veri
  `Data/CariKartOrnek.cs`): "Cari Kart ve Faaliyet Eşleştirme Yapısı — IT Uygulama Raporu v6 (13.08.2026)" uyarlaması. **CARİ_MASTER** (§3'teki 20
  alan: kod, kısa ad, kısaltmalı / resmi ünvan, VKN, vergi dairesi, kart tipi, bölge, ülke, şehir, sınıf, ilişki durumu, rol, eş cari kodu, yetkili,
  telefon, e-posta, vade, durum, not) ve **CARİ_FAALİYET_EŞLEŞTİRME** (§9: faaliyet ana grubu → faaliyet grubu, faaliyet grubu bazında satış
  temsilcisi, ana faaliyet mi, ürün sınıfı › grubu › serisi › adı, durum, başlangıç, not) ayrı yapıdadır. Tedarikçi ekranlarında aynı alan
  "Satınalma Temsilcisi" etiketiyle gösterilir (kart tipine göre `temsilci` değişkeni / `temsilciAdi(tip)`). Seçim listeleri §14'ten gelir
  (`CariKartOrnek.SinifMatrisi`, `FaaliyetHiyerarsisi`, `UrunHiyerarsisi`, `SatisTemsilcileri`). *Kartlar* ekranı (`/CariKart/Musteri`,
  `/CariKart/Tedarikci`): filtreler (tip, bölge, ülke, sınıf, ilişki, rol, durum, faaliyet ana grubu / grubu, temsilci, şirket), KPI, liste,
  detay drawer'ında master + faaliyet satırları, eş kart bağlantısı, aktif / pasif, güncelleme talebi; temsilci × faaliyet grubu dağılımı ve
  §5 matrisi; Excel (kart + faaliyet). *Talep formu* (`/CariKart/MusteriTalep`, `/CariKart/TedarikciTalep`): bağımlı seçimler — kart tipi →
  bölge (yurtiçi = Türkiye kilitli, yurtdışı = ülke zorunlu) → şehir (il listesi / ülke bazlı), kart tipi + bölge → cari sınıfı (finansal ve
  resmi kurumlarda Özel Takip yok), ilişki durumu açıklamalı, rol → çift rolde eş cari kodu (karşı tip kartlardan) zorunlu; faaliyet satırları
  (ana grup seçilmeden grup, grup seçilmeden temsilci seçilemez; aynı grup mükerrer olamaz; ana faaliyet tek satırda Evet; ürün hiyerarşisi
  bağımlı); canlı validasyon (§13), olası cari kodu, onay akışı (Muhasebe cari açılış onayı; grup içi / ilişkili / özel takipte Direktör);
  taslak, onaya gönder, onayla / reddet, iptal; onaylanınca kart CARİ_MASTER'a otomatik açılır (120.xxx / 320.xxx …) ve çift rolde eş kart
  karşılıklı bağlanır. Örnek veri §8 senaryolarını (ABC Tekstil'in 5 faaliyet satırı, çift rol eş kartlar, grup içi, ilişkili, yurtdışı şube /
  bayi / distribütör, özel takip, finansal / resmi / personel carileri) ve her durumdan talepleri içerir.
* **Yapılan Ödemeler** (Yönetim › Özel Analizler › Özel Raporlar; `Controllers/YapilanOdemelerController.cs`, `Views/YapilanOdemeler/Index.cshtml`,
  betik `wwwroot/js/yapilan-odemeler.js`, veri `Data/YapilanOdemelerOrnek.cs`): tüm grup şirketlerinin yaptığı ödemelerin konsolide listesi —
  **Banka (Havale / EFT)**, **Çek** (verilen vadeli çekler: vadesi gelen "Ödendi", gelmeyen "Vade Bekliyor"), **Nakit (Kasa)** çıkış fişleri
  ("İptal" olanlar listelenir, toplama girmez) ve **Nakit Avans** teslimleri. Kaynaklar: Nakit Avans modülünün canlı verisi
  (`NakitAvansOrnek.Talepler › Odemeler`), Aylık Alım Onay'daki "Ödendi" tedarikçi faturaları (`AlimOnayOrnek.Faturalar`; beşte ikisi çek, kalanı
  banka) ve deterministik ERP kasa / banka / çek bordrosu örnekleri (maaş, vergi / SGK, kira, kredi taksiti, enerji ve hizmet, küçük kasa
  giderleri, grup içi ödemeler). Döviz ödemeleri işlem kuruyla TL'ye çevrilir. Ekran: dönem (Bu Ay / Geçen Ay / Son 30–90 Gün / Bu Yıl / Özel),
  şirket, ödeme türü, kategori, alıcı tipi, para birimi, durum ve kaynak filtreleri; KPI kutuları (tıklayınca filtre); Şirket × Ödeme Türü ve
  Ay × Ödeme Türü matrisleri; kategori dağılımı; en büyük 10 alıcı; satır detay drawer'ı (aynı alıcıya yapılan ödemeler, avans talebine /
  Aylık Alım Onay ekranına geçiş); Excel (liste + şirket özeti). Finans › Ödeme Yönetimi altındaki "Yapılan Ödemeler" (Rapor118) tek şirketlik
  ödeme giriş ekranıdır; bu rapor ondan bağımsızdır. Ekran üç sekmelidir: **Ödeme Listesi** (yukarıdaki), **Şirket Bazlı Ödeme Raporu**
  ("2026 Şirket Bazlı Ödeme Raporu" Excel şablonu: her şirket için Banka / Çek / Nakit ödemeleri X ve R satırlarıyla + Toplam Ödeme; 12 ay,
  Toplam, Aylık Ort. = Toplam / veri olan ay sayısı; banka satırları yalnız tanımlı şirketlerde — `YapilanOdemelerOrnek.BankaSirketleri`) ve
  **Konsolide Ödeme Raporu** ("2026 Konsolide Ödeme Raporu" şablonu: Banka / Çek / Nakit × X / R, Toplam X, Toplam R, Genel Toplam; şirket
  konsolidesi). Şirket listesi ve sırası şablondan gelir (10 şirket; diğer modüllerden gelen şablon dışı şirketler ek satır olarak görünür,
  `SirketAdi` eşlemesi ile "URS Makina" → "URS Makine" gibi adlar birleştirilir). Nakit = kasa çıkışları + nakit avans teslimleri; X / R = ERP
  aktarım tipi (`AktarimTipi`). Sekme sırası: Şirket Bazlı Ödeme Raporu (açılışta bu gelir) → Konsolide Ödeme Raporu → Ödeme Listesi; derin
  bağlantı `/YapilanOdemeler?sekme=konsolide | liste`; yıl seçimi ve Excel (CSV) her iki şablon sekmesinde.
* **Müşteri Teklifleri** (Satış ve Operasyon › Müşteri ve CRM Yönetimi › Teklif Yönetimi ve Yönetim › Özel Analizler › Özel Raporlar — aynı ekran;
  `Controllers/MusteriTeklifController.cs`, `Views/MusteriTeklif/Index.cshtml`, betik `wwwroot/js/musteri-teklif.js`, veri `Data/TeklifOrnek.cs`):
  tüm grup şirketlerinin (Uras Kimya, Selvi, Avrupa Paper, Alv Kimya, Daf Kimya, URS Makine) müşterilere verdiği tekliflerin konsolide listesi.
  Her teklif bir CARİ_MASTER müşteri kartına bağlıdır (`CariKartOrnek.Kartlar`); teklifi veren şirket kartın faaliyet ana grubuna göre bulunur
  (`TeklifOrnek.GrupSirketleri`), kalemler faaliyet satırındaki ürün hiyerarşisinden seçilir, temsilci faaliyet grubunun satış temsilcisidir, yurtdışı
  müşterilere EUR / USD teklif verilir (TL karşılığı teklif günü kuru). Durumlar: Taslak, Gönderildi, Revize Edildi, Satışa Döndü (sipariş no / tarihi,
  satış tutarı — kısmi olabilir —, fatura no), Reddedildi (kaybetme nedeni), Süresi Doldu, İptal (listelenir, toplama girmez). Üç sekme:
  **Aylık Teklif Özeti** (açılış): yıl / şirket / temsilci seçimi, KPI kutuları; "Aylık Teklif ve Satışa Dönüşüm" tablosu — verilen teklif adedi ve
  tutarı, satışa dönen adet ve tutar, dönüşüm oranı (tutar ve adet), açık / reddedilen / süresi dolan / taslak tutarı, ortalama teklif; 12 ay + Toplam +
  Aylık Ort. (= Toplam / veri olan ay sayısı); şirket bazlı aynı düzen; temsilci ve faaliyet ana grubu tabloları; Excel (CSV). Satışa dönen teklif
  **teklifin verildiği aya** yazılır (kohort), satış tutarı sipariş tutarıdır. **Müşteri Bazlı**: CARİ_MASTER sütunları (kod, ünvan, VKN, sınıf,
  bölge / ülke / şehir, ilişki, temsilci, vade şartı, kart durumu) + yıl / şirket bazında teklif adedi ve tutarı, satışa dönen, dönüşüm, açık teklif,
  son teklif; satırdan cari kart + faaliyet satırları + teklif geçmişi drawer'ı; Excel. **Teklif Listesi**: dönem, şirket, durum, temsilci, faaliyet ana
  grubu, cari sınıfı, bölge, ilişki durumu, para birimi filtreleri; KPI (tıklayınca filtre); Şirket × Durum ve Ay × Dönüşüm matrisleri; kaybetme
  nedenleri; en büyük 10 müşteri; satır detayında teklif bilgileri, kalemler, cari kart bilgileri (Müşteri Cari Kartları ekranıyla aynı 20 alan) ve
  "Cari Kartı Aç" bağlantısı; Excel. Derin bağlantı: `/MusteriTeklif?sekme=musteri | liste`, `?no=TKL-2026-00042`, `?kod=120.001`.
* **Numune Gönderim Takip Raporu** (Satış ve Operasyon › Müşteri ve CRM Yönetimi › Numune Yönetimi ve Yönetim › Özel Raporlar — aynı ekran;
  `Controllers/NumuneGonderimController.cs`, `Views/NumuneGonderim/Index.cshtml`, betik `wwwroot/js/numune-gonderim.js`, veri `Data/NumuneOrnek.cs`):
  grup şirketlerinin müşterilere gönderdiği yurtiçi ve yurtdışı numunelerin konsolide takibi. Her gönderim bir CARİ_MASTER müşteri kartına bağlıdır;
  gönderen şirket, kalemler ve temsilci Müşteri Teklifleri ile aynı kuralla bulunur. Kayıt alanları: alıcı kişi / adres / telefon, gönderen satış
  temsilcisi, amaç (yeni ürün denemesi, teklif öncesi, sipariş öncesi onay, kalite şikayeti, fuar, Ar-Ge, rakip karşılaştırması), kalemler (ürün, parti /
  lot, miktar, birim, birim bedel) ve **numunenin gerçek bedeli** (yurtdışında proforma döviz + kur), **kargo firması** (`NumuneOrnek.KargoFirmalari`:
  DHL Express, UPS, FedEx, TNT, Turkish Cargo yurtdışı; Yurtiçi / Aras / MNG / Sürat Kargo, DHL ve Şirket Aracı yurtiçi), gönderim türü, **gönderim
  (takip) numarası** ve firmanın takip sayfası bağlantısı, ağırlık / desi, **masraflar** (kargo faturası döviz / TL, gümrük müşavirliği / ihracat beyannamesi,
  sigorta, toplam; masraf kime: gönderen / alıcı), tahmini ve gerçek teslim, teslim alan, kargo hareketleri (talep → paketleme → kargo → gümrük → dağıtım →
  teslim), numune irsaliyesi / proforma / ihracat beyannamesi, durum (Hazırlanıyor, Kargoya Verildi, Yolda, Gümrükte, Teslim Edildi, Teslim Edilemedi,
  İptal) ve **sonuç** (Geri Bildirim Bekleniyor, Değerlendiriliyor, Onaylandı, Reddedildi, Teklif Verildi, Siparişe Döndü, Sonuçsuz): teslimden sonraki 45
  gün içinde aynı şirketin aynı müşteriye aynı faaliyet grubunda verdiği ilk teklif `TeklifOrnek.Liste` içinden bağlanır, teklif satışa döndüyse numune "Siparişe Döndü" olur. Üç sekme:
  **Aylık Numune Özeti** (açılış; yıl / şirket / bölge / temsilci; sade tutulur, şirket bazlı kırılım yoktur): gönderim adedi (yurtiçi / yurtdışı),
  numune bedeli, gönderim masrafı, toplam maliyet, teslim edilen, teklife ve siparişe dönen, dönüşüm oranı, sipariş tutarı, ortalama teslim süresi; 12 ay +
  Toplam + Aylık Ort.; satış temsilcisi ve kargo firması tabloları, sonuç dağılımı, Excel.
  **Müşteri Bazlı**: CARİ_MASTER sütunları + gönderim, bedel, masraf, teslim, teklife / siparişe dönen, dönüşüm, sipariş tutarı, son gönderim; satırdan cari
  kart + numune geçmişi. **Numune Listesi**: dönem, şirket, durum, sonuç, kargo firması, gönderim türü, amaç, masraf kime ve cari kart filtreleri; KPI;
  Şirket × Bölge ve Ay matrisleri; kargo dağılımı; en çok numune gönderilen 10 müşteri; detayda gönderim bilgileri, kalemler ve bedel, kargo ve masraf
  kutuları, hareket zaman çizelgesi, sonuç, cari kart, müşterinin numune geçmişi; "Kargo Takip", "Teklif …" ve "Cari Kartı Aç" bağlantıları; Excel (liste,
  kalem, şirket özeti). Derin bağlantı: `/NumuneGonderim?sekme=musteri | liste`, `?no=NMN-2026-00042`, `?kod=120.001`.
* **Müşteri Fiyat ve Kâr Oranları** (YALNIZ Yönetim › Yönetim Özeti › Fiyat ve Kâr Yönetimi; `Controllers/FiyatYonetimiController.cs`,
  `Views/FiyatYonetimi/Index.cshtml`, betik `wwwroot/js/fiyat-yonetimi.js`, veri ve kurallar `Data/FiyatOrnek.cs`): iki katmanlı fiyatlandırma.
  **1. katman (Üretici → Yetkili Satıcı):** Uras Kimya ürünleri (`FiyatOrnek.Urunler`: Boya Emprime / Dijital, Tekstil Yardımcı Kimyasalları, Tekstil
  Binderleri; kod, hiyerarşi, üretim maliyeti, liste fiyatı) yetkili satıcılara (`FiyatOrnek.Saticilar`: Selvi Kimya 120.006, Alv Kimya 120.005, Uras Kimya
  GmbH 120.009) kaç % kârla satılır — Genel (toplu) / Ürün Grubu / Kalem (ürün) oranı; en özel tanım kazanır (`OranCoz`: Kalem > Grup > Genel); satıcı alış
  fiyatı = maliyet × (1 + oran). **2. katman (Satıcı → Müşteri):** yetkili satıcı her müşteri carisine (CARİ_MASTER müşteri kartları) ürün ürün kaç % kârla
  satar; müşteri satış fiyatı = satıcı alış fiyatı × (1 + oran); liste fiyatına göre fark ve uçtan uca marj. Ekran: satıcı / müşteri seçimi, cari kart özeti,
  genel oran ve grup oranı kutuları, ürün tablosunda kalem oranı girişi (değişenler sarı işaretlenir), anlık hesaplanan fiyatlar, Kaydet (yalnız değişen
  tanımlar gönderilir; boş oran = tanım silme), Vazgeç, Excel fiyat listeleri; Özet sekmesinde satıcı × grup ve müşteri × grup matrisleri ve değişiklik
  günlüğü (kim, ne zaman, eski → yeni). Kaydetme yalnız Yönetici yetkisiyle (`OrnekVeri.Kullanicilar` Yetki = "A", ör. `it02`); diğer roller salt okunur.
  Derin bağlantı: `/FiyatYonetimi?sekme=musteri | ozet`, `?satici=Selvi Kimya`, `?kod=120.001`.
* **İhracat Belge Gönderim Takibi** (Satış ve Operasyon › Yurtdışı Satış › İhracat Operasyon Takibi ve Yönetim › Özel Raporlar — aynı ekran;
  `Controllers/IhracatBelgeController.cs`, `Views/IhracatBelge/Index.cshtml`, betik `wwwroot/js/ihracat-belge.js`, veri `Data/IhracatBelgeOrnek.cs`):
  ihracat operasyonunun kargo firmalarıyla gönderdiği belgelerin konsolide takibi. Her gönderim yurtdışı CARİ_MASTER müşteri kartına bağlıdır; ihracat
  dosyası (IHR-…), fatura, ödeme şekli (kartın vade şartından: Akreditif (L/C), Vesaik Mukabili, Mal Mukabili, Peşin), teslim şekli, konşimento tarihi.
  Gönderim türleri: **Akreditif İbrazı** (amir bankaya; `IhracatBelgeOrnek.AmirBankalar`; akreditif no, ihbar bankası, **ibraz son tarihi** = konşimento +
  21 gün, akreditif vadesini aşamaz; banka sonucu İnceleniyor / Uygun Bulundu / Rezerv Bildirildi / Rezerv Kaldırıldı / Ödendi, rezerv notu, ödeme tarihi),
  **Vesaik Tahsili** (tahsil bankasına; ödeme ile serbest bırakma), **Müşteriye Orijinal Evrak / Forwarder'a Evrak** (orijinal konşimento, A.TR / menşe),
  **Konsolosluk Tasdiki / Ticaret Odası Onayı** (yurtiçi adrese motorlu kurye / kargo). Belge seti satırları (`IbBelge`: tür, no, adet, orijinal / kopya /
  tasdikli). Kargo: DHL Express ağırlıklı, UPS / FedEx / TNT; yurtiçi Yurtiçi / MNG Kargo ve Motorlu Kurye; gönderim (takip) no ve takip sayfası, zarf,
  ağırlık, masraf (döviz + TL), gönderen operasyon uzmanı, hareketler, tahmini / gerçek teslim. **İbraz riski** (`IbGonderim.IbrazRiski`): Süresinde / Kritik
  (≤ 3 gün kaldı) / Geç İbraz. Üç sekme: **Aylık Özet** (sade: gönderim adedi, akreditif ibrazı, vesaik, müşteri / forwarder, belge adedi, masraf, teslim,
  süresinde / geç ibraz, rezerv, ödendi ve tahsil tutarı; 12 ay + Toplam + Aylık Ort.; operasyon uzmanı ve kargo firması tabloları, alıcı tipi dağılımı),
  **Müşteri Bazlı** (CARİ_MASTER sütunları + gönderim, akreditif ibrazı, belge, masraf, fatura tutarı, geç ibraz, rezerv, ödendi), **Gönderim Listesi**
  (filtreler, KPI, ibraz riski listesi, Şirket × Amaç ve Ay matrisleri, kargo ve belge türü dağılımı, detayda dosya / fatura, akreditif ve ibraz kutuları, belge
  seti, kargo ve masraf, hareketler, cari kart, müşterinin gönderim geçmişi; "Kargo Takip" ve "Cari Kartı Aç"; Excel liste + belge). Derin bağlantı:
  `/IhracatBelge?sekme=musteri | liste`, `?no=İBG-2026-00042`, `?kod=120.012`. Yurtdışı müşteri kartları (Mısır, Pakistan, ABD, Polonya, Bangladeş, Fas)
  `CariKartOrnek` tohumuna eklendi (120.012–120.017).
* **Personel Maliyet Raporu** (Yönetim › Özel Analizler › Özel Raporlar ve İK ve İdari İşler › İnsan Kaynakları › Personel Maliyet ve Bordro — aynı
  ekran; `Controllers/PersonelMaliyetController.cs`, `Views/PersonelMaliyet/Index.cshtml`,
  betik `wwwroot/js/personel-maliyet.js`, veri `Data/PersonelMaliyetOrnek.cs`): "PERSONEL 19.06 GÜNCEL" Google E-Tablosunun portal karşılığı, sütun
  düzeni tabloyla aynı — **Personel Kimlik Bilgileri** (sıra, SGK işvereni, fiili görev yeri, ad soyad, ünvan, bölüm, görev, işe giriş, kıdem, çatı
  departman, özel durumu), **Brüt Ücret** (X-Net, R-Net, Top. Net, brüt ücret masrafları, toplam brüt ücret), **İşveren Payları**, **Ek Çalışmalar**,
  **Sosyal Yardımlar** (yemek, yol, servis, kira desteği, eğitim, toplam), **Prim** (satış primi, özel ödeme, toplam), **Yan Haklar**, **Yasal
  Ödemeler**, **Maliyet** (genel maliyet) ve **Not**. Toplam sütunları formülle hesaplanır (Genel Maliyet = Toplam Brüt Ücret + İşveren Payı + Ek
  Çalışma + Sosyal Yrd. + Prim + Yan Haklar + Yasal Öd.); tablodaki boş hücreler 0 alınır. Filtreler (SGK işvereni, görev yeri, çatı departman,
  bölüm, ünvan, özel durum, durum Aktif / İşten Ayrıldı / Tümü, arama) ve KPI kutuları iki sekme için ortaktır. **Personel Maliyet Tablosu**
  sekmesi: tam tablo, dip toplam; **Düzenle → Kaydet** ile kimlik bilgileri (SGK işvereni, görev yeri, ad soyad, ünvan, bölüm, görev, işe giriş,
  çatı departman, özel durum — mevcut değerler `datalist` ile listeden seçilir ya da yeni değer yazılır) ve tüm tutarlar satır bazında toplu
  değiştirilir (bellek içi, son kayıt bilgisi gösterilir); her satırdaki **Kart** düğmesi **Personel Cari Kartı** çekmecesini açar (335.xxx cari
  kodu, durum ve ayrılış tarihi, TC kimlik, telefon, e-posta, tüm ücret ve maliyet kalemleri canlı toplamla, not; Kaydet / Sil / Personel Carileri
  bağlantısı; `?kod=335.012` derin bağlantısı kartı açar); **Personel Ekle** yeni kart açar (`/PersonelMaliyet/Ekle`), hatalı satır **Sil** ile
  kaldırılır (`/PersonelMaliyet/Sil`), işten ayrılan personel silinmez, kartında Durum = İşten Ayrıldı yapılır. Sunucu doğrulaması
  (`PersonelMaliyetOrnek.Dogrula`): zorunlu kimlik alanları, tarih biçimi, TC kimlik 11 hane, e-posta, negatif tutar, ayrılış tarihi; Excel (CSV).
  **Özet Raporu** sekmesi (`/PersonelMaliyet?sekme=ozet`): seçilebilir kırılım (çatı departman, SGK işvereni, fiili görev yeri, bölüm, ünvan,
  görev, özel durum, kıdem aralığı) × tüm maliyet kalemleri + ortalama + pay %, SGK işvereni × çatı departman matrisi (personel sayısı ve
  genel maliyet), maliyet kalemi dağılımı (genel maliyet içindeki pay), ünvan / özel durum / kıdem dağılımları, en yüksek maliyetli 10 personel
  ve tüm bölümleri tek dosyada veren Özet Excel. Özet, filtre kapsamındaki personel üzerinden ve düzenleme modundaki kaydedilmemiş
  değişiklikler dahil hesaplanır.
* **Personel Maliyet Yansıtması** (İK ve İdari İşler › İnsan Kaynakları › Personel Maliyet ve Bordro; `Controllers/PersonelYansitmaController.cs`,
  `Views/PersonelYansitma/Index.cshtml`, betik `wwwroot/js/personel-yansitma.js`, kurallar ve katalog `Data/PersonelYansitmaOrnek.cs`): personel
  cari kartında **SGK İşvereni ≠ Fiili Görev Yeri** olan çalışanların aylık maliyeti (Personel Maliyet Raporu verisi) SGK işvereni şirketten
  fiili görev yeri şirkete yansıtılır. Kalemler Gider / Gelir Kataloğu **13 Personel ve İşçilik Giderleri** (GDR.13.AA.KK, 62 kalem) ile bire bir
  yansıtma gelir kalemine (YNS.13.AA.KK — "… Yansıtma Geliri") eşlenir; maliyet alanı → gider kodu eşlemesi Kalem Eşleme sekmesinden
  değiştirilir. **Yansıtma Hazırlığı:** bordro dönemi seçilince adaylar otomatik listelenir (görev yeri adı → şirket eşlemesi, "+" ile çok
  şirketli görev yerinde eşit pay; pay yüzdeleri satırdan düzenlenir), satır detayında kalemler ve hedef bazında tutarlar; **Seçilenleri Yansıt**
  personel + hedef şirket + dönem bazında tekil kayıt açar ve iki şirkete birden fiş atar (kaynak: 120 hedef carisi borç / 649 YNS alacak /
  391 KDV; hedef: 770 GDR borç / 191 KDV / 320 kaynak carisi alacak). **Yansıtma Kayıtları:** liste, kaynak → hedef matrisi, iki taraflı fiş
  detayı ve geçmiş, Fatura Oluştur (idempotent) / İptal (ters kayıt), Excel (kayıtlar + fiş satırları). KDV oranı parametrik (varsayılan %0).
* **Üst çubuk ekmek kırıntısı** (`Views/Shared/_Layout.cshtml` › `#kmKirinti`): "Ana Sayfa › Modül › Alt Menü › Süreç Grubu › Rapor" yolu
  tıklanabilir. Kabuk sayfası için yol sunucuda `KurumsalMenu.Kirinti(controller, action)` ile menü yapısından bulunur; açık sekmeler için sekme
  kabuğu betiği aktif sekmeye göre çizer — çerçevedeki modül sayfası (`wwwroot/js/kurumsal-modul.js`) bulunduğu kırılımı `postMessage`
  (`tip: "kirinti"`) ile bildirir, bildirim yoksa sekme adresi `MENU_YAPISI` içinde aranır, o da yoksa "Ana Sayfa › sekme adı" gösterilir.
  "Ana Sayfa" kabuğu ana sayfaya döndürür (önceki aktif sekme değil ana sayfa gösterilir); modül / kırılım adımları aynı modülün sekmesi
  açıksa yeni sekme açmadan çerçevenin hash'ini değiştirir, değilse ilgili modül sayfasını sekmede açar.
* **Menü tanımı** `Data/KurumsalMenu.cs` içindedir (menu-config.js'in C# karşılığı). Rapor adları buradaki ekranlara ada göre
  otomatik eşlenir; yazımı farklı olanlar `TakmaAdlar` tablosundadır. Eşleşmeyen kayıtlar "planlanıyor", ana sahibi başka
  modülde olanlar "kısayol" olarak görünür. Menüde yeri belirlenmemiş mevcut ekranlar
  **Ayarlar › Sistem › Menüde Yeri Belirlenmemiş Ekranlar** altında otomatik listelenir.
* **Ana sayfa** (`Views/Home/Index.cshtml`, `wwwroot/css/grup-erp-ana.css`, `wwwroot/js/grup-erp-ana.js`): Grup ERP'deki index.html
  kart düzeni — profil, izin bilgileri, yaklaşan izinler, resmi tatiller, doğum günleri, izin / mesai talepleri, şirket içi mesajlar
  (sohbet + AI asistan sekmesi, veri `wwwroot/js/grup-mesaj-data.js`), takipteki görevler, performans. Veriler örnektir; kart bağlantıları
  ilgili modül sayfasını sekmede açar. Eski rapor kartları sayfası **`/Home/RaporPaneli`** adresinde durur (Ayarlar › Sistem › Rapor Paneli);
  oradaki raporlar artık Grup ERP modüllerine göre gruplanır.
* **Yeni Eklenenler / Son Değişiklikler kartları** (ana sayfa sağ sütun; veri `Data/PortalDegisiklikler.cs`, tam liste
  `/Home/Degisiklikler`, menüde Ayarlar › Sistem › Değişiklik Günlüğü): portala yeni rapor / ekran eklendiğinde `Yeni(...)`,
  ekran taşındığında `Tasima(...)`, güncellendiğinde `Guncelleme(...)`, kaldırıldığında `Kaldirma(...)` satırı eklenir. Ekran adı
  `KurumsalMenu`'daki rapor adıyla aynıysa bağlantı ve menü yolu otomatik bulunur; menüde olmayanlar için `href` / yol elle verilir.
  Üst çubuktaki `/Home/SonGuncellemeler` ucu da aynı listeden beslenir. **Rozetler:** son 30 gün içindeki `Yeni(...)` kayıtları
  modül şemasında, sol menüde ve listede **(Yeni)**, `Guncelleme(...)` / `Tasima(...)` kayıtları **(Düzenlendi)** rozeti üretir
  (`ModulController.Yapi` → `yeni` / `duzenlendi`, `wwwroot/js/kurumsal-modul.js`). Eşleme kayıttaki adrese göredir: modül derin bağlantısı
  (`/Modul/x?yol=.../rN`) ya da ekranın kendi adresi (`/CariKart/Tedarikci`); ana sayfaya giden kısayollar eşlenmez. Yeni rapor eklerken
  ya da mevcut raporu düzenlerken bu günlüğe satır eklemeyi unutmayın.
* **Rapor tabloları** (`wwwroot/js/rapor-tablo-dinamik.js`, kabuktan tüm sayfalara yüklenir): her tabloda başlığa tıklayınca
  sıralama (sayı / tarih / metin otomatik), başlık altında sütun filtreleri (≤40 farklı değerde "Tümü" listesi, aksi halde arama).
  Kendi filtre satırı (`tr.filter-row`) ya da sıralama simgesi (`.sort-icon`) olan raporlara dokunmaz; gruplu başlık, detay satırı,
  "Toplam" satırı ve sonradan AJAX ile dolan tablolar desteklenir. Bir tabloyu dışlamak için `data-dinamik="kapali"`.
* **Rapor teması** (`wwwroot/css/rapor-tema.css`, `wwwroot/js/rapor-tema.js`; kabuk `body.rapor-sayfasi` sınıfını Home/Modul/Account
  dışındaki her sayfaya verir): Inter yazı tipi zorunlu (Poppins vb. ezilir), tek marka paleti, düz düğmeler, 8px köşe, küçük büyük
  harfli tablo başlıkları, zebra satırlar; sayfa başlığına konuya göre ikon karosu; tablo hücrelerindeki durum sözcükleri
  (Onaylandı / Hatalı / Beklemede / Aktif / Kapalı …) renkli rozete dönüşür, sayısal hücreler sağa yaslanır, eksi tutarlar kırmızı.
* **Kompakt rapor görünümü** (`grup-erp-tema.css`): çerçeve içinde açılan raporlarda yazı, kart, düğme, alan ve tablo ölçüleri daraltılır;
  modül sayfasındaki raporda "Menüleri gizle" ile sol menüler kapanıp rapor tam genişlik alır (tercih hatırlanır).
  Raporların üstündeki özet (KPI) kartları ve renkli başlık bantları da `rapor-tema.css` ile alçaltılır: `.dashboard-card`/`.kpi-*`,
  `.stat-card`, `.summary-card`, `.kpi`, `.kpi-card`, `*-ozet-kutu`, `.summary-box`, `.quick-filter-card`, `*-hero`, `.page-header`
  kalıpları ~60 px yüksekliğe iner; bilinmeyen kalıplardaki kısa metinli, büyük puntolu kartları `rapor-tema.js` sezgisel olarak
  `rt-kpi` işaretler. Uras Üretim ekranlarında aynı sıkıştırma `_UretimLayout.cshtml` içinde (`html.cerceve-icinde`) yapılır.
* **Üst çubuk**: selam + bugünün tarihi, menü yapısında arama (`/` tuşu odaklar; sonuçlar sekmede açılır), mevcut ikon düğmeleri.
* **Tema**: `wwwroot/css/grup-erp-tema.css` (portal-tema.css'ten sonra yüklenir; beyaz 216px menü, 60px beyaz üst çubuk, #f4f6fa zemin).
  Menü yapısı istemciye `/Modul/Yapi.js` ile verilir (`window.MENU_YAPISI`, `window.MENU_ICONS`).

## İçerik

Ana projedeki **tüm ekranlar** (218 ekran, 269 görünüm) bu projede açılır. Ekran kataloğu `Data/MenuKatalogu.cs` içinden gelir;
menü yerleşimi ise yukarıdaki `Data/KurumsalMenu.cs` ile belirlenir.

| Tür | Ekranlar | Veri |
|---|---|---|
| Elle yazılmış, zengin örnek verili | Dashboard, Cari Ekstre (Rapor20), Mizan (Rapor28), Satış Faturası (Rapor109), Aktarım Denetimi (Rapor145), Toplu Kalem Güncelleme (Rapor173), Mesajlar, Sistem Yetkileri | `Data/OrnekVeri.cs` |
| Otomatik üretilmiş | Diğer bütün ekranlar (`Uretilen/` klasörü) | Tablolar ve AJAX uçları ÖRNEK VERİYLE dolu gelir: `Data/OrnekDoldurucu.cs` alan adına göre (cari adı, tutar, tarih, vade, belge no, para birimi …) değer üretir. SQL / SAP bağlantısı yoktur. |

Kaydetme / SAP'ye yazma / Excel-PDF üretme uçları sahte cevap döner. Bellek içi değişiklikler (mesaj, favori, tercih) uygulama yeniden başlayınca sıfırlanır.

## `Uretilen/` klasörü nasıl oluşuyor

**Örnek veri:** Araç ana projeyi tip bilgisiyle analiz eder (yalnızca kaynak kodu okur, hiçbir şey çalıştırmaz). Her `return Json(...)` ifadesinin döndürdüğü yapıyı aynı alan adlarıyla yeniden kurar; değerleri çalışma anında `OrnekDoldurucu` üretir. Dapper ile dönen dinamik satırlarda kolon adları SQL metnindeki `SELECT` listesinden okunur. Görünüm modelleri ve ViewBag listeleri de dolu gelir. Örnek değer kurallarını değiştirmek için yalnızca `Data/OrnekDoldurucu.cs` düzenlenir.

`Tools/Soyucu` (Roslyn aracı) ana projedeki Controllers, Services, Helpers, Repositories, Data ve Models dosyalarını okur, **bütün metot gövdelerini söker** (SQL, Service Layer, HTTP, dosya işlemi kalmaz) ve her eylemin yerine görünümü boş ama geçerli model / ViewBag ile açan ya da boş JSON dönen bir gövde yazar. Tip tanımları aynen kaldığı için Razor görünümleri değiştirilmeden derlenir. `Tools/Soyucu/yamalar.txt` belirli metotlar için özel gövde tanımlar.

Ana projede ekran eklenince yeniden üretmek için:

```
dotnet build Tools/Soyucu -c Release
dotnet Tools/Soyucu/bin/Release/net10.0/Soyucu.dll <ana proje klasörü> UrasPortalOnyuz/Uretilen "AccountController,HomeController,MesajController,Rapor20Controller,Rapor28Controller,Rapor109Controller,Rapor145Controller,Rapor173Controller,PermissionController,WhatsAppController,ApplicationDbContext" Tools/Soyucu/yamalar.txt <ana projenin derleme çıktısı klasörü (binDebug
et5.0)>
```

Ardından ana projenin `Views` klasörünü kopyala (`Views/Shared/_Layout.cshtml` ve `Views/Home/Index.cshtml` hariç; bu ikisinde SQL blokları elle temizlendi). csproj içindeki SqlClient, Dapper, EF gibi paketler yalnızca tip imzaları derlensin diye vardır; hiçbir bağlantı açılmaz, `appsettings.json` içinde bağlantı dizesi yoktur.

## Yapı notları

* `RootNamespace` bilerek `WebApplication3` bırakıldı; böylece Razor görünümleri ana projeden **değiştirilmeden**
  kopyalanabiliyor. Yeni bir ekran eklemek için: görünümü `Views/<Ekran>/Index.cshtml` olarak kopyala,
  `Controllers/` altına aynı adla örnek veri döndüren bir denetleyici yaz.
* `Data/ApplicationDbContext.cs`, `Services/Taklitler.cs` → ana projedeki EF/yetki/SAP kimlik sınıflarının bellek içi taklitleri.
* Tanımsız bir AJAX ucu çağrılırsa `Startup.cs` içindeki ara yazılım 404 yerine `{success:false, message:"…"}` döner;
  böylece sayfalar bozulmaz.
