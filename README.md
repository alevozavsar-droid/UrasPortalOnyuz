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
  Üst çubuktaki `/Home/SonGuncellemeler` ucu da aynı listeden beslenir.
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
