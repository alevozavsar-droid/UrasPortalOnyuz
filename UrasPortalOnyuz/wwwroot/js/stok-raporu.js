// ============================================================
// KONSOLİDE STOK RAPORU (/StokRaporu, 13 Kas 2026) — Views/StokRaporu/Index.cshtml · veri /StokRaporu/Veri (Data/StokRaporOrnek.cs)
// "URAS Fiili ve Resmî Stok Raporu" şablonunun portal karşılığı. Sunucu ham kayıtları verir (stok satırları + tüketim planları); türetilen her şey burada hesaplanır.
//   Ana sekme stok grubu: Hammadde (Hammadde + Yarı Mamul) · Bitmiş Ürün (Mamul + Ticari Mal) · Ambalaj · Diğer Stoklar · Tümü.
//   16 Kas 2026: SAYIM ve SAYIM ONAYI sekmeleri kullanıcı isteğiyle KALDIRILDI (15 Kas'ta eklenmişti: sayan kişi sayımı girer, onaylanan sayım fiili stok olurdu). Ekrandan sayım girilmez / onaylanmaz;
//   sunucudaki kayıt kodu (Data/StokSayimKayit.cs, /StokRaporu/SayimKaydet · SayimKarar) duruyor, önceden onaylanmış kayıt varsa fiili stok olarak geçerli kalır (hazirla › ONAYLI).
//   Alt sekmeler (8): Stok Listesi · Tüketim ve Yeterlilik · Sayım ve Risk | Aylık Stok Hareketleri (iki görünüm: Aylık Hareket · Stok ve Uyum) · Tüketim Verisi | Fiili Stok Takibi · Muhasebe Stok Takibi | Özet. (15 Kas 2026: Yönetim Özeti sekmesi kullanıcı isteğiyle kaldırıldı.)
//   16 Kas 2026 — KULLANICI GİRİŞLERİ (Data/StokTanimKayit.cs): (1) Tüketim ve Yeterlilik'te STOK EŞİKLERİ girilir (kritik stok, minimum stok, tedarik süresi, hedef stok);
//   (2) Stok Listesi'nde LİSTEDE OLMAYAN ÜRÜN eklenir (girilen miktar fiili stoktur); (3) DEMİRBAŞ ana grubu ve Demirbaş Listesi sekmesi: üretim makineleri / ekipman ve küçük demirbaşlar
//   bilgileriyle girilir. Demirbaş grubunda tüketim / reçete / aylık hareket sekmeleri gösterilmez.
//   ÜRÜN GEÇMİŞİ (15 Kas 2026): her sekmede stok koduna tıklanınca ürünün giriş (tarih, tedarikçi, belge, fatura, parti, fiyat), çıkış ve sayım kayıtları ayrı pencerede açılır (gecmisAc; veri V.girisler).
//   15 Kas 2026: Depo · Şirket · Kırılım özet sekmeleri kullanıcı isteğiyle kaldırıldı (depo ve şirket dağılımı Özet sekmesinde, süzgeçlerde ve her tablonun başlık süzgecinde duruyor).
//   TEMEL KURAL (kullanıcı, 14 Kas 2026): MUHASEBE STOKU (R) ve FİİLİ STOK tamamen ayrı şeylerdir; muhasebe formülü (reçetesi) ile gerçek formül de ayrıdır, dolayısıyla
//   muhasebe (R) tüketimi ile fiili tüketim de farklıdır. Her sekme iki tarafı ayrı ayrı gösterir; fark (X = fiili − muhasebe) BİLGİDİR, hata / uyumsuzluk sayılmaz.
//   Kontroller her tarafı kendi içinde yapar: fiili tüketim ↔ gerçek reçete, muhasebe çıkışı ↔ muhasebe reçetesi; aylık stok değişimi fiili ve muhasebe için ayrı ayrı.
//   14 Kas 2026: Stok ve Tüketim Uyum Raporu bu rapora katıldı (kullanıcı: "birleştir, toplam 12 sekme"); uyum sekmeleri aynı stok verisinden, stok kartı (şirket + depo + ürün) bazında hesaplanır.
//   R = resmî (kayıtlı) miktar · X = fark (fiili − resmî) · Fiili = sayılan + sayım sonrası giriş − çıkış. Değer farkı = miktar etkisi + maliyet etkisi.
//   Kullanılabilir = fiili − bloke − rezerve (yalnız Depoda / Dış Depoda ve Gruba Ait stok). Tüketim: yeterlilik = kullanılabilir ÷ günlük tüketim;
//   hedef = günlük × hedef gün + güvenlik; önerilen alım = en yüksek(0; hedef − (kullanılabilir + uygun açık alım + uygun yoldaki alım)).
//   Miktarlar farklı birimlerde olduğundan miktar sütunlarının dip toplamı yalnız tek birim süzülüyken yazılır; değerler ₺.
// Tablo motoru Konsolide Satış Raporu ile aynı düzendedir (sütun ağırlıkları yüzdeye çevrilir, başlık süzgeci, sıralama, Sütunlar, Excel).
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('sr'); if (!kok || kok.dataset.rapor !== 'stok') return;
  const $ = s => kok.querySelector(s);
  const TR = 'tr-TR';
  const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const say = (v, h = 2) => (+v || 0).toLocaleString(TR, { minimumFractionDigits: h, maximumFractionDigits: h });
  const tl = (v, h = 0) => say(v, h) + ' ₺';
  const yuzde = (v, h = 1) => '% ' + say(v, h);
  const tarih = s => s ? s.slice(8, 10) + '.' + s.slice(5, 7) + '.' + s.slice(0, 4) : '';
  const kucuk = s => String(s ?? '').toLocaleLowerCase(TR);
  const tarihYap = s => new Date(+s.slice(0, 4), +s.slice(5, 7) - 1, +s.slice(8, 10));
  const gunFark = (a, b) => Math.round((tarihYap(b) - tarihYap(a)) / 864e5);
  const gunEkle = (s, n) => { const d = tarihYap(s); d.setDate(d.getDate() + n); return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0'); };
  const topla = (l, f) => l.reduce((t, x) => t + (+f(x) || 0), 0);
  const sirala = (a, b) => typeof a === 'number' && typeof b === 'number' ? a - b : String(a ?? '').localeCompare(String(b ?? ''), TR, { numeric: true });
  const TAM = /^(adet|çift|koli|kutu|rulo|takım)$/;
  const mik = (v, birim) => say(v, TAM.test(birim) ? 0 : 1) + ' ' + birim;
  const sayE = v => (+v || 0).toLocaleString(TR, { minimumFractionDigits: 0, maximumFractionDigits: 2 });
  const mikE = (v, birim) => sayE(v) + ' ' + birim;
  const mikS = (v, birim) => say(v, TAM.test(birim) ? 0 : 1);   // birimsiz miktar, birimin ondalık hassasiyetiyle (Tüketim ve Yeterlilik)
  const AY_AD = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
  const ayAd = a => a ? AY_AD[+a.slice(5, 7) - 1] + ' ' + a.slice(0, 4) : '';
  const NOKTA = { 'Uyumlu': '#15803d', 'Eşit': '#94a3b8', 'Minimum Altı': '#b45309', 'Fiili Fazla': '#0d9488', 'Fiili Eksik': '#b45309', 'Negatif Stok': '#b91c1c', 'Uygun': '#15803d', 'SKT Yaklaşıyor': '#b45309', 'SKT Geçmiş': '#b91c1c', 'Uygulanmaz': '#94a3b8', 'Aktif': '#15803d', 'Yavaş': '#b45309', 'Atıl': '#b91c1c', 'Bakımda': '#b45309', 'Arızalı': '#b91c1c', 'Hurda': '#94a3b8',
    'Acil Alım': '#b91c1c', 'Kritik': '#c2410c', 'Yakın Takip': '#b45309', 'Yeterli': '#15803d', 'Tüketim Yok': '#94a3b8', 'Plan Yok': '#cbd5e1', 'Onaylandı': '#15803d', 'İncelemede': '#0d9488', 'Bekliyor': '#b45309', 'Onay Bekliyor': '#b45309', 'Reddedildi': '#b91c1c', 'Sayım Bekliyor': '#94a3b8', 'Hızlı Azalıyor': '#b91c1c', 'Azalıyor': '#b45309', 'Sabit': '#94a3b8', 'Artıyor': '#0d9488',
    'Uyumsuz': '#b91c1c', 'Reçete Yok': '#94a3b8', 'Kayıt Yok': '#cbd5e1', 'Stok Kaydı Yok': '#c2410c', 'Dengeli': '#15803d', 'Eksik': '#b45309', 'Fazla': '#0d9488', 'Düşük': '#15803d', 'Orta': '#b45309', 'Yüksek': '#b91c1c', 'İncele': '#b91c1c', 'Normal': '#15803d' };
  const rozet = (d, ac) => d ? `<span class="rz" style="--n:${NOKTA[d] || '#94a3b8'}"${ac ? ` title="${esc(ac)}"` : ''}>${esc(d)}</span>` : '';
  const cift = (ust, alt) => `<b>${ust}</b>${alt ? `<small>${alt}</small>` : ''}`;
  const adHucre = ad => `<b>${esc(ad || '—')}</b>`;
  /** stok kodu hücresi: tıklanınca Ürün Geçmişi penceresi açılır (gecmisAc) */
  const kodHucre = r => `<a href="#" class="st-kod" data-gecmis="${esc(r.kod)}" data-sirket="${esc(r.sirket || '')}" title="Ürün geçmişi: ne zaman geldi, hangi tedarikçi, çıkışlar ve sayımlar"><b>${esc(r.kod || '—')}</b></a>`;
  const yon = (v, yazi) => Math.abs(v) < 0.005 ? '—' : `<span class="${v < 0 ? 'kirmizi' : ''}">${v > 0 ? '+' : ''}${yazi}</span>`;

  // ---------- durum ----------
  let V = null, S = [], P = [], sekme = kok.dataset.sekme || 'stok';
  // ana sekme: stok grubu (adres ?grup=b | a | d | t; varsayılan Hammadde)
  const GRUP = { h: ['Hammadde', 'Yarı Mamul'], b: ['Mamul', 'Ticari Mal'], a: ['Ambalaj Malzemesi'], d: ['Sarf Malzeme', 'Kırtasiye / Ofis', 'Yedek Parça', 'Numune', 'Hurda / Fire'], m: ['Makine / Ekipman', 'Küçük Demirbaş'], t: null };
  const DEMIRBAS_TIP = GRUP.m;   // Demirbaş grubu (16 Kas 2026): üretim makineleri / ekipman ve küçük demirbaşlar; kayıtları Demirbaş Listesi sekmesinden girilir
  const GRUP_AD = { h: 'Hammadde', b: 'Bitmiş Ürün', a: 'Ambalaj', d: 'Diğer Stoklar', m: 'Demirbaş', t: 'Tüm Stoklar' };
  let GR = new URLSearchParams(location.search).get('grup'); if (!(GR in GRUP)) GR = 'h';
  const AYAR = { hedefGun: 90, sktGun: 90, yavasGun: 90, atilGun: 180, donemGun: 90 };
  const BOS_F = () => ({ sirket: [], depo: [], tip: [], konum: [], mulkiyet: [], mDurum: [], sktDurum: [], hDurum: [], oncelik: [], anaGrup: [], fGrup: [], sinif: [], urunGrubu: [], seri: [], urun: [], onay: [], sorumlu: [], ara: '' });
  let F = BOS_F();
  let KART = [], TB = [], AYLAR = [], AY = new URLSearchParams(location.search).get('ay') || '', TOL = null;   // uyum kontrolü: stok kartları, tüketim belgeleri, Kontrol Ayı, Tolerans
  let HG = new URLSearchParams(location.search).get('gorunum') === 'stok' ? 's' : 'a';   // Aylık Stok Hareketleri görünümü: a = Aylık Hareket · s = Stok ve Uyum
  let ESIK = new Map(), TASLAK_E = {};   // stok eşikleri: kart anahtarı → girilen eşik · kaydedilmemiş girişler
  let ONAYLI = new Map(), KULLANICI = '';   // satır anahtarı → kayıtlı onaylı sayım (16 Kas 2026: Sayım ve Sayım Onayı sekmeleri kaldırıldı; önceden onaylanmış kayıt varsa geçerli kalır)
  const H = { farkli: false, riskli: false, incele: false, ay: false, azalan: false, azalanM: false, uyF: false, uyM: false, uyS: false };            // Sayım ve Risk sekmesinde hızlı süzgeç
  const DURUM = {};
  const durum = p => DURUM[p] || (DURUM[p] = { sira: null, suz: {}, limit: 300, acik: new Set() });

  // ---------- veri ----------
  /** kullanıcının eklediği ürün ve demirbaşlar (V.urunler, Data/StokTanimKayit.cs) → stok satırı. Muhasebe miktarı (R) yalnız demirbaş formundaki alandan gelir;
   *  fiili miktar formda girilen miktardır (demirbaşta adet, eklenen üründe Fiili Miktar). */
  const ekSatir = () => (V.urunler || []).map(u => ({ no: 100000 + u.id, ek: true, urunId: u.id, dm: u, sirket: u.sirket, depoKod: u.depoKod, depo: u.depo, mulkiyet: 'Gruba Ait', konum: 'Depoda', kod: u.kod, ad: u.ad, tip: u.tip,
    anaGrup: '', grup: '', sinif: '', urunGrubu: '', seri: '', birim: u.birim, lot: u.lot || '', sktTakibi: false, skt: '', sonHareket: String(u.guncellemeZamani || u.girisZamani || '').slice(0, 10),
    rMiktar: u.muhMiktar || 0, rMaliyet: u.maliyet || 0, sayimTarihi: '', sayilan: u.miktar || 0, giris: 0, cikis: 0, fMaliyet: u.maliyet || 0, bloke: 0, rezerve: 0, sayimBelge: '', farkNedeni: '', sorumlu: u.zimmet || '',
    aksiyon: '', hedefTarih: '', onay: '', duzeltmeBelge: '', tedarikci: u.tedarikci || '' }));
  function hazirla() {
    Object.assign(AYAR, V.ayar || {});
    // kayıtlı sayımlar (sekmeler kaldırılmadan önce girilmiş olabilir): satır başına en son ONAYLANAN sayım geçerlidir; ekrandan yeni sayım girilmez
    ONAYLI = new Map();
    (V.sayimlar || []).forEach(k => { if (k.durum === 'Onaylandı') { const o = ONAYLI.get(k.anahtar); if (!o || String(k.onayZamani) > String(o.onayZamani)) ONAYLI.set(k.anahtar, k); } });
    S = V.satirlar.concat(ekSatir()).map(s0 => {
      const sKey = s0.sirket + '|' + s0.depoKod + '|' + s0.kod + '|' + (s0.lot || ''), o = ONAYLI.get(sKey);
      // onaylı sayım varsa fiili stok odur: sayılan = onaylanan miktar, sayım sonrası giriş / çıkış sıfır; sayımda girilen bloke / karantina, konum ve SKT de satıra işlenir. Muhasebe (R) değerleri değişmez.
      const s = o ? Object.assign({}, s0, { sayilan: o.sayilan, giris: 0, cikis: 0, sayimTarihi: o.tarih, sayimBelge: o.belge }, o.bloke != null ? { bloke: o.bloke } : {}, o.konum ? { konum: o.konum } : {}, o.skt ? { skt: o.skt } : {}) : s0;
      const fiili = s.sayilan + s.giris - s.cikis, x = fiili - s.rMiktar, rDeger = s.rMiktar * s.rMaliyet, fDeger = fiili * s.fMaliyet;
      const kullan = (s.konum === 'Depoda' || s.konum === 'Dış Depoda') && s.mulkiyet === 'Gruba Ait', kMiktar = kullan ? Math.max(0, fiili - s.bloke - s.rezerve) : 0;
      return Object.assign({}, s, { fiili, x, xYuzde: s.rMiktar ? x / Math.abs(s.rMiktar) * 100 : 0, rDeger, fDeger, xDeger: fDeger - rDeger, mEtki: x * s.rMaliyet, malEtki: fiili * (s.fMaliyet - s.rMaliyet), kMiktar, kDeger: kMiktar * s.fMaliyet,
        sKey, depoTam: s.depoKod + ' · ' + s.depo, anahtar: s.sirket + '|' + s.depoKod + '|' + s.kod, _ara: kucuk([s.kod, s.ad, s.lot, s.sirket, s.depo, s.tip, s.sayimBelge, s.tedarikci, s.dm ? [s.dm.marka, s.dm.model, s.dm.seriNo, s.dm.zimmet, s.dm.yer].join(' ') : ''].join(' ')) });
    });
    // STOK EŞİKLERİ (Tüketim ve Yeterlilik'te girilir): girilen kritik stok ve tedarik süresi planın örnek değerinin yerine geçer; minimum ve hedef stok girilmişse hesaplananın yerine kullanılır (hesapla)
    ESIK = new Map((V.esikler || []).map(e => [e.anahtar, e]));
    // kullanıcının eklediği ürünler (demirbaş dışı) için tüketimi sıfır plan satırı açılır: eşikleri girilebilsin
    const planVar = new Set(V.planlar.map(p => p.sirket + '|' + p.depoKod + '|' + p.kod)), ekPlan = [];
    (V.urunler || []).forEach(u => {
      const a = u.sirket + '|' + u.depoKod + '|' + u.kod; if (DEMIRBAS_TIP.includes(u.tip) || planVar.has(a)) return; planVar.add(a);
      ekPlan.push({ no: 100000 + u.id, ek: true, sirket: u.sirket, depoKod: u.depoKod, depo: u.depo, kod: u.kod, ad: u.ad, tip: u.tip, anaGrup: '', grup: '', sinif: '', urunGrubu: '', seri: '', birim: u.birim, tuketimR: 0, tuketimX: 0, recete: null, receteR: null,
        guvenlik: 0, tedarikGun: 0, acikAlim: 0, acikTarih: '', yoldaki: 0, yoldakiTarih: '', tedarikci: u.tedarikci || '', sorumlu: '', maliyet: u.maliyet || 0 });
    });
    P = V.planlar.concat(ekPlan).map(p => {
      const anahtar = p.sirket + '|' + p.depoKod + '|' + p.kod, e = ESIK.get(anahtar) || null;
      return Object.assign({}, p, { tuketim: p.tuketimR + p.tuketimX, depoTam: p.depoKod + ' · ' + p.depo, anahtar, esik: e, guvenlik0: p.guvenlik, tedarikGun0: p.tedarikGun, guvenlik: e && e.kritik != null ? e.kritik : p.guvenlik, tedarikGun: e && e.tedarikGun != null ? e.tedarikGun : p.tedarikGun,
        _ara: kucuk([p.kod, p.ad, p.sirket, p.depo, p.tip, p.tedarikci].join(' ')) });
    });
    uyumHazirla(); hesapla();
  }
  /** eşiklere bağlı durumlar ve tüketim / yeterlilik hesabı (eşik değişince yeniden çalışır) */
  function hesapla() {
    const bugun = V.bugun, kul = {}, muhS = {}, fiS = {};
    S.forEach(s => {
      s.mDurum = s.rMiktar < 0 || s.fiili < 0 ? 'Negatif Stok' : Math.abs(s.x) <= 0.001 ? 'Eşit' : s.x > 0 ? 'Fiili Fazla' : 'Fiili Eksik';   // fark yönü (X); fark bir sorun sayılmaz
      s.sktKalan = s.sktTakibi && s.skt ? gunFark(bugun, s.skt) : null;
      s.sktDurum = !s.sktTakibi ? 'Uygulanmaz' : s.sktKalan === null ? 'SKT Eksik' : s.sktKalan < 0 ? 'SKT Geçmiş' : s.sktKalan <= AYAR.sktGun ? 'SKT Yaklaşıyor' : 'Uygun';
      s.hGun = s.sonHareket ? gunFark(s.sonHareket, bugun) : null; s.hDurum = s.hGun === null ? 'Hareket Yok' : s.hGun >= AYAR.atilGun ? 'Atıl' : s.hGun >= AYAR.yavasGun ? 'Yavaş' : 'Aktif';
      s.riskli = s.mDurum === 'Negatif Stok' || s.sktDurum === 'SKT Geçmiş' || s.sktDurum === 'SKT Yaklaşıyor' || s.hDurum !== 'Aktif' || s.bloke > 0;
      kul[s.anahtar] = (kul[s.anahtar] || 0) + s.kMiktar; muhS[s.anahtar] = (muhS[s.anahtar] || 0) + s.rMiktar; fiS[s.anahtar] = (fiS[s.anahtar] || 0) + s.fiili;
    });
    const ufuk = gunEkle(bugun, AYAR.hedefGun), oncelik = {};
    P.forEach(p => {
      p.kullan = kul[p.anahtar] || 0; p.gunluk = AYAR.donemGun ? p.tuketim / AYAR.donemGun : 0; p.ay30 = p.gunluk * 30; p.yeterlilik = p.gunluk > 0 ? p.kullan / p.gunluk : null;
      // otomatik eşikler (giriş kutularında soluk yazılır) ve yürürlükteki eşikler: girilen değer varsa o, yoksa otomatik. Kritik stok = güvenlik stoku.
      p.oto = { kritik: p.guvenlik0, tedarikGun: p.tedarikGun0, minimum: p.guvenlik + p.gunluk * p.tedarikGun, hedef: p.gunluk * AYAR.hedefGun + p.guvenlik };
      p.minStok = p.esik && p.esik.minimum != null ? p.esik.minimum : p.oto.minimum; p.hedef = p.esik && p.esik.hedef != null ? p.esik.hedef : p.oto.hedef;
      p.fiiliStok = fiS[p.anahtar] || 0; p.durum = p.fiiliStok < p.guvenlik ? 'Kritik' : p.fiiliStok < p.minStok ? 'Minimum Altı' : 'Yeterli';   // stok durumu (uyum sekmelerindeki kart durumuyla aynı kural)
      // gecikmiş ya da hedef dönem sonrasına kalan alım net stok pozisyonuna eklenmez
      p.uygunAcik = p.acikAlim && p.acikTarih >= bugun && p.acikTarih <= ufuk ? p.acikAlim : 0; p.uygunYol = p.yoldaki && p.yoldakiTarih >= bugun && p.yoldakiTarih <= ufuk ? p.yoldaki : 0;
      p.net = p.kullan + p.uygunAcik + p.uygunYol; p.oneri = Math.max(0, p.hedef - p.net); p.oneriDeger = p.oneri * p.maliyet;
      p.oncelik = p.tuketim <= 0 ? 'Tüketim Yok' : p.kullan <= 0 ? 'Acil Alım' : p.yeterlilik < p.tedarikGun ? 'Kritik' : p.oneri > 0 ? 'Yakın Takip' : 'Yeterli';
      p.receteFark = p.recete == null ? null : p.recete - p.tuketim; p.receteFarkR = p.receteR == null ? null : p.receteR - p.tuketimR;   // gerçek reçete ↔ fiili tüketim · muhasebe reçetesi ↔ muhasebe çıkışı
      p.muhStok = muhS[p.anahtar] || 0; p.gunlukR = AYAR.donemGun ? p.tuketimR / AYAR.donemGun : 0; p.yeterlilikR = p.gunlukR > 0 ? p.muhStok / p.gunlukR : null;   // muhasebe stoku, muhasebe çıkış hızıyla kaç gün yeter
      oncelik[p.anahtar] = p.oncelik;
    });
    S.forEach(s => { s.oncelik = oncelik[s.anahtar] || 'Plan Yok'; });
    uyumHesapla();
  }
  // ---------- UYUM KONTROLÜ: veri (14 Kas 2026 — Stok ve Tüketim Uyum Raporu bu rapora katıldı) ----------
  // Stok kartı = şirket + depo + ürün (stok satırlarının toplamı). Kontrol Ayı ve Tolerans sekme şeridinden değişir.
  //   Stok durumu (fiili stokun yeterliliği): fiili < kritik stok → Kritik; fiili < minimum stok → Minimum Altı; değilse Yeterli. Kritik stok = güvenlik stoku; minimum stok = güvenlik + tedarik süresi tüketimi.
  //   Risk: Kritik / negatif stok / aylık uyumsuz → Yüksek; reçete kontrolü uyumsuz ya da minimum altı → Orta; değilse Düşük. Muhasebe − fiili farkı (X) riske girmez.
  //   Aylık uyum: beklenen ay sonu = önceki ay sonu + ay içi giriş − tüketim; |ay sonu − beklenen| ≤ tolerans (fiili VE muhasebe) → Uyumlu.
  //   Reçete kontrolü (iki ayrı formül): gerçek reçete ↔ fiili tüketim ve muhasebe reçetesi ↔ muhasebe çıkışı, her biri tolerans içinde ve aylık uyumsuz değilse Uyumlu.
  //   Tüketim belgesi: fire % = (fiili − gerçek reçete) ÷ gerçek reçete; muhasebe sapma % = (çıkış − muhasebe reçetesi) ÷ muhasebe reçetesi; X = fiili − muhasebe (bilgi).
  function uyumHazirla() {
    const km = new Map(), pm = new Map(P.map(p => [p.anahtar, p]));
    S.forEach(s => {
      let k = km.get(s.anahtar);
      if (!k) km.set(s.anahtar, k = { no: km.size + 1, anahtar: s.anahtar, sirket: s.sirket, depoKod: s.depoKod, depo: s.depo, depoTam: s.depoTam, kod: s.kod, ad: s.ad, tip: s.tip, anaGrup: s.anaGrup, grup: s.grup, sinif: s.sinif, urunGrubu: s.urunGrubu, seri: s.seri, birim: s.birim,
        satirlar: [], muhMiktar: 0, muhBedel: 0, fiiliMiktar: 0, fiiliBedel: 0, plan: pm.get(s.anahtar) || null });
      k.satirlar.push(s); k.muhMiktar += s.rMiktar; k.muhBedel += s.rDeger; k.fiiliMiktar += s.fiili; k.fiiliBedel += s.fDeger;
    });
    KART = [...km.values()];
    KART.forEach(k => { k.sayimli = k.satirlar.some(s => s.sayimOnayli); k.ayTuk = {}; k.ayTukM = {}; k.maliyet = Math.abs(k.fiiliMiktar) > 1e-9 ? k.fiiliBedel / k.fiiliMiktar : k.satirlar[0].fMaliyet; k.maliyetR = Math.abs(k.muhMiktar) > 1e-9 ? k.muhBedel / k.muhMiktar : k.satirlar[0].rMaliyet; k.fark = k.fiiliMiktar - k.muhMiktar; k.farkBedel = k.fiiliBedel - k.muhBedel; k.farkYuzde = k.muhBedel ? k.farkBedel / Math.abs(k.muhBedel) * 100 : 0; k.lot = k.satirlar.length === 1 ? k.satirlar[0].lot : k.satirlar.length + ' Parti'; });
    TB = (V.belgeler || []).map(t => {
      const k = km.get(t.sirket + '|' + t.depoKod + '|' + t.kod) || null, m = k ? k.maliyet : 0, mR = k ? k.maliyetR : 0;
      return Object.assign({}, t, { kart: k, ay: t.tarih.slice(0, 7), ad: k ? k.ad : '', birim: k ? k.birim : '', tip: k ? k.tip : '', depoTam: k ? k.depoTam : '', anaGrup: k ? k.anaGrup : '', grup: k ? k.grup : '', sinif: k ? k.sinif : '', urunGrubu: k ? k.urunGrubu : '', seri: k ? k.seri : '', maliyet: m,
        fiiliBedel: t.fiili * m, muhBedel: t.muhasebe * mR, sapma: t.fiili - t.muhasebe, sapmaBedel: t.fiili * m - t.muhasebe * mR, sapmaYuzde: t.muhasebe ? (t.fiili - t.muhasebe) / t.muhasebe * 100 : 0,
        fire: t.recete ? (t.fiili - t.recete) / t.recete * 100 : 0, fireR: t.receteR ? (t.muhasebe - t.receteR) / t.receteR * 100 : 0,   // fiili ↔ gerçek reçete · muhasebe çıkışı ↔ muhasebe reçetesi
        muhStok: k ? k.muhMiktar : null, fiiliStok: k ? k.fiiliMiktar : null,
        stokUyum: !k ? 'Stok Kaydı Yok' : t.fiili <= k.fiiliMiktar && t.muhasebe <= k.muhMiktar ? 'Uyumlu' : 'Uyumsuz',
        receteUyum: !t.recete && !t.receteR ? 'Reçete Yok' : !k ? 'Stok Kaydı Yok' : (!t.recete || t.recete <= k.fiiliMiktar) && (!t.receteR || t.receteR <= k.muhMiktar) ? 'Uyumlu' : 'Uyumsuz', _ara: kucuk(t.belge + ' ' + tarih(t.tarih)) });
    });
    TB.forEach(t => { if (!t.kart) return; t.kart.ayTuk[t.ay] = (t.kart.ayTuk[t.ay] || 0) + t.fiili; t.kart.ayTukM[t.ay] = (t.kart.ayTukM[t.ay] || 0) + t.muhasebe; });   // kart bazında aylık fiili tüketim ve muhasebe çıkışı (stok takibi sekmeleri)
    AYLAR = [...new Set((V.hareketler || []).map(h => h.ay))].sort().reverse();
    SEK.hiyerarsi.kol = takipKol('f'); SEK.muhtakip.kol = takipKol('m');
    if (!AYLAR.includes(AY)) AY = AYLAR.includes(AYAR.kontrolAyi) ? AYAR.kontrolAyi : (AYLAR[0] || '');
    if (TOL === null) TOL = +AYAR.tolerans >= 0 ? +AYAR.tolerans : 2;
  }
  const tolIci = v => Math.abs(v) <= TOL + 1e-9;
  /** Kontrol Ayı ve Tolerans'a bağlı her şey (hesapla() sonunda ve parametre değişince çalışır) */
  function uyumHesapla() {
    const tk = {}, hr = {};
    TB.forEach(t => { if (t.ay !== AY || !t.kart) return; const o = tk[t.kart.anahtar] || (tk[t.kart.anahtar] = { r: 0, rr: 0, f: 0, m: 0, n: 0 }); o.r += t.recete; o.rr += t.receteR; o.f += t.fiili; o.m += t.muhasebe; o.n++; });
    (V.hareketler || []).forEach(h => { if (h.ay === AY) hr[h.sirket + '|' + h.depoKod + '|' + h.kod] = h; });
    KART.forEach(s => {
      const p = s.plan; s.kritikStok = p ? p.guvenlik : 0; s.minStok = p ? p.minStok : 0; s.oncelik = p ? p.oncelik : 'Plan Yok';
      // Stok durumu FİİLİ stokun yeterliliğidir; muhasebe − fiili farkı (X) sorun sayılmaz (iki stok ayrı formülle oluşur)
      s.durum = !p ? 'Plan Yok' : s.fiiliMiktar < s.kritikStok ? 'Kritik' : s.fiiliMiktar < s.minStok ? 'Minimum Altı' : 'Yeterli';
      const t = tk[s.anahtar] || { r: 0, rr: 0, f: 0, m: 0, n: 0 }, h = hr[s.anahtar];
      s.recete = t.r; s.receteR = t.rr; s.tukF = t.f; s.tukM = t.m; s.belgeSay = t.n;
      if (h) {
        s.oncF = h.oncekiFiili; s.girF = h.girisFiili; s.sonF = h.ay === AYLAR[0] && s.sayimli ? s.fiiliMiktar : h.sonFiili; s.bekF = s.oncF + s.girF - s.tukF; s.farkF = s.sonF - s.bekF;   // son ayın kapanışı güncel stoktur; onaylı sayım varsa sayılan miktar
        s.oncM = h.oncekiMuh; s.girM = h.girisMuh; s.sonM = h.sonMuh; s.bekM = s.oncM + s.girM - s.tukM; s.farkM = s.sonM - s.bekM;
        s.aylik = tolIci(s.farkF) && tolIci(s.farkM) ? 'Uyumlu' : 'Uyumsuz';
      } else { s.oncF = s.girF = s.sonF = s.bekF = s.farkF = s.oncM = s.girM = s.sonM = s.bekM = s.farkM = null; s.aylik = 'Kayıt Yok'; }
      // Reçete kontrolü iki ayrı formülle: gerçek reçete ↔ fiili tüketim, muhasebe reçetesi ↔ muhasebe çıkışı (fiili ile muhasebe birbiriyle karşılaştırılmaz)
      s.rf = s.recete - s.tukF; s.rm = s.receteR - s.tukM;
      // SATIŞ UYUMU (muhasebe tarafı): muhasebe stoku satışla uyumlu düşüyor mu? Beklenen çıkış bitmiş üründe faturalı satış miktarı,
      // hammadde / yarı mamul / ambalajda muhasebe reçetesine (satılan ürünün R formülüne) göre tüketimdir.
      s.satis = h && h.satis != null ? h.satis : null;
      s.satisBek = s.satis != null ? s.satis : s.receteR ? s.receteR : null;
      s.satisDayanak = s.satis != null ? 'Faturalı satış miktarı' : s.receteR ? 'Muhasebe reçetesi (satılan ürünün R formülü)' : '';
      s.satisFark = s.satisBek == null ? null : s.tukM - s.satisBek;
      s.satisUyum = s.satisBek == null ? 'Uygulanmaz' : tolIci(s.satisFark) ? 'Uyumlu' : 'Uyumsuz';
      s.uclu = !s.recete && !s.receteR ? 'Reçete Yok' : s.aylik !== 'Uyumsuz' && (!s.recete || tolIci(s.rf)) && (!s.receteR || tolIci(s.rm)) ? 'Uyumlu' : 'Uyumsuz';
      s.sonuc = s.aylik === 'Uyumsuz' || s.uclu === 'Uyumsuz' ? 'İncele' : 'Normal';
      s.risk = s.durum === 'Kritik' || s.fiiliMiktar < 0 || s.muhMiktar < 0 || s.aylik === 'Uyumsuz' ? 'Yüksek' : s.uclu === 'Uyumsuz' || s.durum === 'Minimum Altı' ? 'Orta' : 'Düşük';
      s.riskAc = [s.durum === 'Kritik' ? 'Fiili stok kritik stokun altında' : '', s.fiiliMiktar < 0 ? 'Fiili stok negatif' : '', s.muhMiktar < 0 ? 'Muhasebe stoku negatif' : '', s.aylik === 'Uyumsuz' ? 'Aylık stok değişimi uyumsuz' : '', s.uclu === 'Uyumsuz' ? 'Reçete kontrolü uyumsuz' : '', s.durum === 'Minimum Altı' ? 'Fiili stok minimum stokun altında' : ''].filter(Boolean).join(' · ');
      const isaret = v => (v > 0 ? '+' : '') + mikE(v, s.birim);
      s.aylikDetay = s.aylik === 'Kayıt Yok' ? 'Seçili ay için ay sonu stok kaydı yok.' : s.aylik === 'Uyumlu' ? 'Fiili ve muhasebe stokunun aylık değişimi kendi tüketimleriyle uyumlu.'
        : [!tolIci(s.farkF) ? `Fiili: beklenen ${mikE(s.bekF, s.birim)} / ay sonu ${mikE(s.sonF, s.birim)} / fark ${isaret(s.farkF)}` : '', !tolIci(s.farkM) ? `Muhasebe: beklenen ${mikE(s.bekM, s.birim)} / ay sonu ${mikE(s.sonM, s.birim)} / fark ${isaret(s.farkM)}` : ''].filter(Boolean).join(' · ');
      s.ucluDetay = s.uclu === 'Uyumlu' ? 'Fiili tüketim gerçek reçeteyle, muhasebe çıkışı muhasebe reçetesiyle uyumlu.' : s.uclu === 'Reçete Yok' ? 'Seçili ay için reçete tüketimi yok.'
        : `Aylık ${s.aylik}; gerçek reçete − fiili tüketim ${isaret(s.rf)}; muhasebe reçetesi − muhasebe çıkışı ${isaret(s.rm)}`;
      s.aksiyon = s.sonuc !== 'İncele' ? '' : s.aylik === 'Uyumsuz' && s.uclu === 'Uyumsuz' ? 'Stok hareketi, tüketim fişi ve reçete miktarı karşılaştırılmalı.' : s.aylik === 'Uyumsuz' ? 'Ay içi stok hareketleri ve sayım kontrol edilmeli.' : 'Tüketim fişi ve reçete miktarı karşılaştırılmalı.';
    });
  }

  // ---------- süzgeç ----------
  const icinde = (l, v) => !l.length || l.includes(v);
  const AL = { sirket: x => x.sirket, depo: x => x.depoTam, tip: x => x.tip, konum: x => x.konum, mulkiyet: x => x.mulkiyet, mDurum: x => x.mDurum, sktDurum: x => x.sktDurum, hDurum: x => x.hDurum, oncelik: x => x.oncelik,
    anaGrup: x => x.anaGrup, fGrup: x => x.grup, sinif: x => x.sinif, urunGrubu: x => x.urunGrubu, seri: x => x.seri, urun: x => x.kod, onay: x => x.onay, sorumlu: x => x.sorumlu };
  const AL_K = Object.keys(AL), PLAN_K = ['sirket', 'depo', 'tip', 'oncelik', 'anaGrup', 'fGrup', 'sinif', 'urunGrubu', 'seri', 'urun', 'sorumlu'];
  const grupUyar = x => !GRUP[GR] || GRUP[GR].includes(x.tip);
  const stokUyar = (s, haric) => grupUyar(s) && AL_K.every(a => a === haric || icinde(F[a], AL[a](s))) && (!F.ara || s._ara.includes(F.ara));
  const planUyar = p => grupUyar(p) && PLAN_K.every(a => icinde(F[a], AL[a](p))) && (!F.ara || p._ara.includes(F.ara));
  /** stok kartı (uyum sekmeleri): satırlarından en az biri süzgeçlere uyuyorsa görünür; ek = aramada ayrıca bakılacak metin (belge no) */
  const kartUyar = (k, ek) => k.satirlar.some(s => grupUyar(s) && AL_K.every(a => icinde(F[a], AL[a](s))) && (!F.ara || s._ara.includes(F.ara) || (!!ek && ek.includes(F.ara))));
  const kartSatir = () => KART.filter(k => kartUyar(k));


  // ---------- sütun tanımları ----------
  // t: m metin · l liste · d tarih · p tutar / sayı · y yüzde · ik işaret ; top: 1 dip toplam, 'birim' tek birim süzülüyken dip toplam ; gz: sütunun varsayılan gizli olduğu stok grupları (h b a d t)
  const M = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'm', v }, o);
  const L = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'l', v }, o);
  const D = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'd', v, h: r => tarih(v(r)) || '—' }, o);
  const TL = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'p', v, h: r => tl(v(r)), top: 1, hane: 0 }, o);
  const N = (k, ad, w, v, hane = 0, o) => Object.assign({ k, ad, w, t: 'p', v, h: r => say(v(r), hane), top: 1, hane, sayi: 1 }, o);
  const Q = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'p', v, h: r => mik(v(r), r.birim), top: 'birim' }, o);   // miktar (birimiyle)
  const Y = (k, ad, w, v, tf, o) => Object.assign({ k, ad, w, t: 'y', v, h: r => yuzde(v(r)), tf }, o);
  // ÜST BAŞLIK KÜMELERİ (14 Kas 2026, kullanıcı: "fiili stok bilgileri ve muhasebe stok bilgileri sütunlarla, üst başlıkla ayrılsın"): sütunda ust = küme başlığı, ur = küme rengi
  // (m muhasebe · f fiili · x fark · d nötr). Küme içindeki sütun adları kısadır (Miktar, Birim Maliyet, Değer); Sütunlar listesinde küme adıyla birlikte yazılır.
  const MUH = { ust: 'Muhasebe Stoku (R)', ur: 'm' }, FII = { ust: 'Fiili Stok', ur: 'f' }, FRK = { ust: 'Fark (X)', ur: 'x', ustAc: 'Fark (X) = Fiili Stok − Muhasebe Stoku' }, UR_AD = { m: 'Muhasebe', f: 'Fiili', x: 'Fark', d: '' };
  const kume = (g, o) => Object.assign({}, g, o || {});
  const tamAd = c => (c.ust ? (c.tamUst || !UR_AD[c.ur] ? c.ust : UR_AD[c.ur]) + ' · ' : '') + c.ad;
  const AGAC = () => [L('anaGrup', 'Faaliyet Ana Grubu', 120, r => r.anaGrup, { gizli: 1 }), L('fGrup', 'Faaliyet Grubu', 130, r => r.grup, { gz: 'hadmt' }), L('sinif', 'Ürün Sınıfı', 104, r => r.sinif, { gizli: 1 }), L('urunGrubu', 'Ürün Grubu', 110, r => r.urunGrubu, { gizli: 1 }), L('seri', 'Ürün Serisi', 110, r => r.seri, { gizli: 1 })];
  const sktHucre = r => !r.sktTakibi ? '—' : cift(tarih(r.skt) || '—', r.sktKalan === null ? '' : r.sktKalan < 0 ? `<span class="kirmizi">${-r.sktKalan} Gün Geçti</span>` : r.sktKalan + ' Gün');
  const hareketHucre = r => r.sonHareket ? cift(tarih(r.sonHareket), r.hGun + ' Gün') : '—';

  // ---------- UYUM KONTROLÜ: sütunlar ----------
  // Bu sekmelerde miktarlar birimsiz yazılır (çok sayıda miktar sütunu sığsın diye), birim ayrı sütundadır; dip toplam tek birim süzülüyken birimiyle yazılır.
  const DRM = { ust: 'Stok Durumu', ur: 'd', ustAc: 'Fiili stokun yeterliliği: kritik stokun altı Kritik, minimum stokun altı Minimum Altı, değilse Yeterli. Muhasebe − fiili farkı (X) durum ve riske girmez.' },
    AYL = { ust: 'Aylık Stok Değişimi ve Tüketim Uyumu', ur: 'a', ustAc: 'Beklenen ay sonu = önceki ay sonu + ay içi giriş − tüketim. Ay sonu stokla farkı toleransı aşmıyorsa (fiili ve muhasebe) uyumlu.' },
    UCL = { ust: 'Reçete Kontrolü', ur: 'k', ustAc: 'İki ayrı formül: fiili tüketim gerçek reçeteyle, muhasebe çıkışı muhasebe reçetesiyle karşılaştırılır. İkisi de tolerans içinde ve aylık stok değişimi uyumluysa uyumlu.' },
    MUHF = { ust: 'Muhasebe (R Formülü)', ur: 'm', ustAc: 'Muhasebe kayıtlarındaki formüle (reçeteye) göre oluşan tüketim ve stok' }, FIIF = { ust: 'Fiili (Gerçek Formül)', ur: 'f', ustAc: 'Üretimde gerçekten kullanılan formüle göre oluşan tüketim ve stok' };
  // STOK EŞİKLERİ kümesi (Tüketim ve Yeterlilik): giriş kutulu sütunlar. v = yürürlükteki değer (sıralama / süzgeç), h = giriş kutusu (esKutu).
  const ESK = { ust: 'Stok Eşikleri (Girilir)', ur: 'a', tamUst: 1, ustAc: 'Kritik stok, minimum stok, tedarik süresi ve hedef stok buradan girilir. Kutu boşsa soluk yazılan otomatik değer geçerlidir; Kaydet ile bütün sekmelere yansır.' };
  const ESG = (k, ad, w, alan, f) => Object.assign({ k, ad, w, t: 'p', top: 0, v: r => f(r) || 0, h: r => esKutu(r, alan) }, ESK);
  const DMB = { ust: 'Makine Bilgileri', ur: 'd', tamUst: 1 };   // Demirbaş Listesi
  const yonY = v => Math.abs(v) < 0.005 ? '—' : `<span class="${v < 0 ? 'kirmizi' : ''}">% ${v > 0 ? '+' : '−'}${say(Math.abs(v), 1)}</span>`;
  /** birimsiz miktar; boş (null) değerde "—"; yonlu = işaretli fark gösterimi */
  const QS = (k, ad, w, f, o, yonlu) => Object.assign({ k, ad, w, t: 'p', v: r => f(r) ?? 0, h: r => f(r) == null ? '—' : yonlu ? yon(f(r), sayE(f(r))) : sayE(f(r)), top: 'birim', bs: 1 }, o);
  /** birimsiz miktar, birimin ondalık hassasiyetiyle (adet vb. tam, kg / lt 1 ondalık); boş değerde "—"; yonlu = işaretli */
  const QK = (k, ad, w, f, o, yonlu) => Object.assign({ k, ad, w, t: 'p', v: r => f(r) ?? 0, h: r => f(r) == null ? '—' : yonlu ? yon(f(r), mikS(f(r), r.birim)) : mikS(f(r), r.birim), top: 'birim', bs: 1 }, o);
  const BIRIM = () => L('birim', 'Birim', 50, r => r.birim, { tek: 1 });
  const KIMLIK = () => [M('kod', 'Stok Kodu', 118, r => r.kod, { tek: 1, h: kodHucre }), M('ad', 'Ürün Adı', 110, r => r.ad, { h: r => adHucre(r.ad) }), L('tip', 'Stok Tipi', 84, r => r.tip, { gz: 'hbad' }), L('sirket', 'Şirket', 82, r => r.sirket)];
  const UCLU_KOL = () => [
    QS('recete', 'Gerçek Reçete', 82, r => r.recete, kume(UCL, { gizli: 1, h: r => r.recete ? sayE(r.recete) : '—' })), QS('rf', 'Gerçek Reçete − Fiili', 82, r => r.rf, kume(UCL, { h: r => r.recete ? yon(r.rf, sayE(r.rf)) : '—' })),
    QS('receteR', 'Muhasebe Reçetesi', 86, r => r.receteR, kume(UCL, { gizli: 1, h: r => r.receteR ? sayE(r.receteR) : '—' })), QS('rm', 'Muhasebe Reçetesi − Çıkış', 90, r => r.rm, kume(UCL, { h: r => r.receteR ? yon(r.rm, sayE(r.rm)) : '—' })),
    L('uclu', 'Reçete Uyumu', 118, r => r.uclu, kume(UCL, { tek: 1, h: r => rozet(r.uclu, r.ucluDetay) }))
  ];
  const BELGE_AC = { 'Uyumlu': 'Stok tüketimi karşılıyor.', 'Stok Kaydı Yok': 'Ürün kodu stok kartlarında yok.', 'Uyumsuz': 'Tüketim kendi stokunu aşıyor (fiili tüketim fiili stoku ya da muhasebe çıkışı muhasebe stokunu).', 'Reçete Yok': 'Bu satırda reçete miktarı yok.' };
  /** STOK TAKİBİ (15 Kas 2026) — iki sekme, aynı düzen, iki ayrı taraf (muhasebe ve fiili birbirinden bağımsızdır):
   *    Fiili Stok Takibi (eski adı Ürün Hiyerarşisi): aylık FİİLİ tüketime göre stoku azalan ürünler.
   *    Muhasebe Stok Takibi: aylık MUHASEBE çıkışına (R formülü) göre muhasebe stoku azalan ürünler.
   *  Görünen stok kartları ürün (kod + stok tipi) bazında toplanır. Her ürün için:
   *    Aylık tüketim (ay ay) ve aylık ortalama; yeterlilik = güncel stok ÷ aylık ortalama; stok eğilimi.
   *    STOK KONTROLÜ (kontrol ayı): önceki ay sonu + ay içi giriş − tüketim = beklenen ay sonu; ay sonu sayımla (fiili) / ay sonu stokla (muhasebe) farkı.
   *      Üründeki stok kartlarından birinin farkı toleransı aşıyorsa Uyumsuz (kartlar arası artı / eksi farklar birbirini gizlemesin diye kart bazında bakılır).
   *    SATIŞ UYUMU (yalnız muhasebe): muhasebe stoku satışla uyumlu düşüyor mu? Beklenen çıkış bitmiş üründe faturalı satış miktarı, hammadde / yarı mamul /
   *      ambalajda muhasebe reçetesine (satılan ürünün R formülüne) göre tüketimdir; muhasebe çıkışıyla farkı toleransı aşıyorsa Uyumsuz.
   *  Eğilim: değişim < −tolerans → Azalıyor (yeterlilik 1 aydan azsa Hızlı Azalıyor); değişim > tolerans → Artıyor; değilse Sabit; hiç tüketimi yoksa Tüketim Yok.
   *  taraf: 'f' fiili · 'm' muhasebe */
  function takipSatir(taraf) {
    const muh = taraf === 'm', m = new Map();
    kartSatir().forEach(k => {
      const a = k.kod + '|' + k.tip; let x = m.get(a);
      if (!x) m.set(a, x = { a, kod: k.kod, ad: k.ad, tip: k.tip, anaGrup: k.anaGrup, grup: k.grup, sinif: k.sinif, urunGrubu: k.urunGrubu, seri: k.seri, birim: k.birim, sirketler: new Set(), kart: 0,
        stok: 0, oteki: 0, deger: 0, minStok: 0, kritikStok: 0, ortAy: 0, onc: 0, gir: 0, tuk: 0, son: 0, kayit: 0, uyumsuzKart: 0, satisBek: 0, satisCikis: 0, satisKart: 0, satisUyumsuz: 0, dayanak: new Set(), ay: {} });
      x.sirketler.add(k.sirket); x.kart++; x.minStok += k.minStok; x.kritikStok += k.kritikStok;
      x.stok += muh ? k.muhMiktar : k.fiiliMiktar; x.oteki += muh ? k.fiiliMiktar : k.muhMiktar; x.deger += muh ? k.muhBedel : k.fiiliBedel;
      x.ortAy += k.plan ? ((muh ? k.plan.gunlukR : k.plan.gunluk) || 0) * 30 : 0; x.tuk += (muh ? k.tukM : k.tukF) || 0;
      const onc = muh ? k.oncM : k.oncF;
      if (onc != null) { x.onc += onc; x.gir += muh ? k.girM : k.girF; x.son += muh ? k.sonM : k.sonF; x.kayit++; if (!tolIci(muh ? k.farkM : k.farkF)) x.uyumsuzKart++; }
      if (muh && k.satisBek != null) { x.satisBek += k.satisBek; x.satisCikis += k.tukM || 0; x.satisKart++; x.dayanak.add(k.satisDayanak); if (k.satisUyum === 'Uyumsuz') x.satisUyumsuz++; }
      const ayTuk = (muh ? k.ayTukM : k.ayTuk) || {};
      AYLAR.forEach(ay => { x.ay[ay] = (x.ay[ay] || 0) + (ayTuk[ay] || 0); });
    });
    const azalan = H[muh ? 'azalanM' : 'azalan'], stokUy = H[muh ? 'uyM' : 'uyF'], satisUy = muh && H.uyS;
    return [...m.values()].map(x => {
      const degisim = x.kayit ? x.son - x.onc : null, yet = x.ortAy > 0 ? x.stok / x.ortAy : null, bek = x.kayit ? x.onc + x.gir - x.tuk : null;
      const egilim = x.ortAy <= 0 && x.tuk <= 0 ? 'Tüketim Yok' : degisim === null ? 'Kayıt Yok' : degisim < -TOL ? (yet !== null && yet < 1 ? 'Hızlı Azalıyor' : 'Azalıyor') : degisim > TOL ? 'Artıyor' : 'Sabit';
      const stokUyum = !x.kayit ? 'Kayıt Yok' : x.uyumsuzKart ? 'Uyumsuz' : 'Uyumlu', satisUyum = !x.satisKart ? 'Uygulanmaz' : x.satisUyumsuz ? 'Uyumsuz' : 'Uyumlu';
      return Object.assign(x, { sirketYazi: [...x.sirketler].join(', '), degisim, degisimY: x.kayit && x.onc ? degisim / Math.abs(x.onc) * 100 : 0, yeterlilikAy: yet,
        tukenme: yet === null || yet > 120 ? '' : gunEkle(V.bugun, Math.round(Math.max(0, yet) * 30)), egilim, bek, stokFark: x.kayit ? x.son - bek : null, stokUyum,
        stokAc: !x.kayit ? 'Seçili ay için ay sonu stok kaydı yok.' : x.uyumsuzKart ? `${x.uyumsuzKart} / ${x.kayit} stok kartında ${muh ? 'ay sonu muhasebe stoku' : 'ay sonu sayımı'} beklenenden farklı (tolerans ± ${sayE(TOL)}).` : `Önceki ay sonu + giriş − ${muh ? 'çıkış' : 'tüketim'}, ${muh ? 'ay sonu muhasebe stokuyla' : 'ay sonu sayımıyla'} uyumlu.`,
        satisBekT: x.satisKart ? x.satisBek : null, satisFark: x.satisKart ? x.satisCikis - x.satisBek : null, satisUyum,
        satisAc: !x.satisKart ? 'Bu üründe satışa bağlı beklenen çıkış yok (satış ya da muhasebe reçetesi kaydı bulunmuyor).' : 'Dayanak: ' + [...x.dayanak].join(' · ') + (x.satisUyumsuz ? `. ${x.satisUyumsuz} / ${x.satisKart} stok kartında muhasebe çıkışı beklenenden farklı.` : '. Muhasebe çıkışı beklenenle uyumlu.') });
    }).filter(x => (!azalan || x.egilim === 'Azalıyor' || x.egilim === 'Hızlı Azalıyor') && (!stokUy || x.stokUyum === 'Uyumsuz') && (!satisUy || x.satisUyum === 'Uyumsuz'));
  }
  /** Stok takibi sütunları: ay sütunları veriye göre değiştiği için veri gelince kurulur (uyumHazirla) */
  function takipKol(taraf) {
    const muh = taraf === 'm', aylar = AYLAR.slice().reverse();
    const AYT = muh ? { ust: 'Aylık Muhasebe Çıkışı (R)', ur: 'a', ustAc: 'Tüketim belgelerindeki muhasebe çıkışı (R formülü). Dönemin ilk ve son ayı kısmi olabilir.' }
      : { ust: 'Aylık Fiili Tüketim', ur: 'a', ustAc: 'Tüketim belgelerindeki fiili tüketim (gerçek formül). Dönemin ilk ve son ayı kısmi olabilir.' };
    const STK = muh ? { ust: 'Muhasebe Stok Kontrolü (R) · Kontrol Ayı', ur: 'm', ustAc: 'Önceki ay sonu muhasebe stoku + ay içi giriş − muhasebe çıkışı = beklenen ay sonu. Ay sonu muhasebe stokuyla farkı toleransı aşıyorsa uyumsuz.' }
      : { ust: 'Fiili Stok Kontrolü · Kontrol Ayı', ur: 'f', ustAc: 'Önceki ay sonu fiili stok + ay içi giriş − fiili tüketim = beklenen ay sonu. Ay sonu sayımıyla farkı toleransı aşıyorsa uyumsuz.' };
    const SAT = { ust: 'Satış Uyumu', ur: 'k', ustAc: 'Muhasebe stoku satışla uyumlu düşüyor mu? Beklenen çıkış: bitmiş üründe faturalı satış miktarı; hammadde, yarı mamul ve ambalajda muhasebe reçetesine (satılan ürünün R formülüne) göre tüketim.' };
    const GNC = { ust: 'Güncel Durum', ur: 'd', ustAc: muh ? 'Yeterlilik = güncel muhasebe stoku ÷ aylık ortalama muhasebe çıkışı' : 'Yeterlilik = güncel fiili stok ÷ aylık ortalama fiili tüketim' };
    const ayAc = muh ? ' muhasebe çıkışı' : ' fiili tüketimi';
    const satis = !muh ? [] : [
      QS('satisBekT', 'Satışa Göre Beklenen Çıkış', 92, r => r.satisBekT, kume(SAT)), QS('satisFark', 'Fark', 72, r => r.satisFark, kume(SAT, { ac: 'Fark = muhasebe çıkışı − satışa göre beklenen çıkış' }), 1),
      L('satisUyum', 'Satış Uyumu', 122, r => r.satisUyum, kume(SAT, { tek: 1, h: r => rozet(r.satisUyum, r.satisAc) }))
    ];
    return [
      L('anaGrup', 'Faaliyet Ana Grubu', 112, r => r.anaGrup, { gizli: 1 }), L('fGrup', 'Faaliyet Grubu', 118, r => r.grup, { gizli: 1 }), L('sinif', 'Ürün Sınıfı', 108, r => r.sinif, { gizli: 1 }),
      L('urunGrubu', 'Ürün Grubu', 108, r => r.urunGrubu, { gizli: 1 }), L('seri', 'Ürün Serisi', 112, r => r.seri, { gizli: 1 }),
      M('kod', 'Stok Kodu', 116, r => r.kod, { tek: 1, h: kodHucre }), M('ad', 'Ürün Adı', 132, r => r.ad, { h: r => adHucre(r.ad) }), L('tip', 'Stok Tipi', 96, r => r.tip, { gz: 'a' }), BIRIM(),
      N('kart', 'Stok Kartı', 58, r => r.kart, 0, { gizli: 1 }), M('sirketYazi', 'Şirketler', 150, r => r.sirketYazi, { gizli: 1 }),
      ...aylar.map(a => QS('ay' + a, AY_AD[+a.slice(5, 7) - 1].slice(0, 3) + ' ' + a.slice(2, 4), 72, r => r.ay[a] || 0, kume(AYT, { ac: ayAd(a) + ayAc }))),
      QS('ortAy', 'Aylık Ortalama', 80, r => r.ortAy, kume(AYT, { ac: 'Dönem toplamının 30 gün eşdeğeri', h: r => `<b>${sayE(r.ortAy)}</b>` })),
      QS('onc', 'Önceki Ay Sonu', 84, r => r.kayit ? r.onc : null, kume(STK)), QS('gir', 'Ay İçi Giriş', 74, r => r.kayit ? r.gir : null, kume(STK)), QS('tuk', muh ? 'Çıkış' : 'Tüketim', 78, r => r.kayit ? r.tuk : null, kume(STK)),
      QS('bek', 'Beklenen Ay Sonu', 84, r => r.bek, kume(STK)), QS('son', muh ? 'Ay Sonu Stok' : 'Ay Sonu Sayım', 84, r => r.kayit ? r.son : null, kume(STK, { h: r => r.kayit ? `<b>${sayE(r.son)}</b>` : '—' })),
      QS('stokFark', 'Fark', 74, r => r.stokFark, kume(STK, { ac: muh ? 'Fark = ay sonu muhasebe stoku − beklenen ay sonu' : 'Fark = ay sonu sayım − beklenen ay sonu' }), 1),
      L('stokUyum', muh ? 'Stok Uyumu' : 'Sayım Uyumu', 106, r => r.stokUyum, kume(STK, { tek: 1, h: r => rozet(r.stokUyum, r.stokAc) })),
      QS('degisim', 'Değişim', 80, r => r.degisim, kume(STK, { gizli: 1, ac: 'Değişim = ay sonu − önceki ay sonu' }), 1), Y('degisimY', 'Değişim %', 68, r => r.degisimY, () => '', kume(STK, { gizli: 1, h: r => r.degisim === null ? '—' : yonY(r.degisimY) })),
      ...satis,
      QS('stok', muh ? 'Muhasebe Stoku (R)' : 'Fiili Stok', 88, r => r.stok, kume(GNC, { h: r => `<b>${sayE(r.stok)}</b>` })), QS('oteki', muh ? 'Fiili Stok' : 'Muhasebe Stoku (R)', 88, r => r.oteki, kume(GNC, { gizli: muh ? 1 : 0 })),
      N('yeterlilikAy', 'Yeterlilik (Ay)', 80, r => r.yeterlilikAy === null ? 99999 : r.yeterlilikAy, 1, kume(GNC, { top: 0, h: r => r.yeterlilikAy === null ? '—' : `<b class="${r.yeterlilikAy < 1 ? 'kirmizi' : ''}">${say(r.yeterlilikAy, 1)}</b>` })),
      D('tukenme', 'Tahmini Tükenme', 92, r => r.tukenme, kume(GNC, { gizli: muh ? 1 : 0 })), L('egilim', 'Stok Eğilimi', 130, r => r.egilim, kume(GNC, { tek: 1, h: r => rozet(r.egilim) })),
      QS('minStok', 'Minimum Stok', 86, r => r.minStok, { top: 0, gizli: 1 }), QS('kritikStok', 'Kritik Stok', 86, r => r.kritikStok, { top: 0, gizli: 1 }), TL('deger', muh ? 'Muhasebe Değeri (₺)' : 'Fiili Değer (₺)', 104, r => r.deger, { gizli: 1 })
    ];
  }
  const GMUH = { ust: 'Güncel Muhasebe Stoku (R)', ur: 'm', tamUst: 1 }, GFII = { ust: 'Güncel Fiili Stok', ur: 'f', tamUst: 1 },
    AYF = { ust: 'Aylık · Fiili Stok', ur: 'f', tamUst: 1, ustAc: 'Kontrol ayı: önceki ay sonu fiili stok + ay içi giriş − fiili tüketim = beklenen ay sonu; ay sonu stokla farkı' },
    AYM = { ust: 'Aylık · Muhasebe Stoku (R)', ur: 'm', tamUst: 1, ustAc: 'Kontrol ayı: önceki ay sonu muhasebe stoku + ay içi giriş − muhasebe çıkışı = beklenen ay sonu; ay sonu stokla farkı' };
  const UYUM_SEK = {
    belge: {
      ad: 'Tüketim Verisi', sira: { k: 'tarih', y: -1 }, satir: () => TB.filter(t => t.kart && kartUyar(t.kart, t._ara) && (!H.ay || t.ay === AY)),
      kol: [
        D('tarih', 'Tarih', 92, r => r.tarih), L('donemTipi', 'Dönem Tipi', 68, r => r.donemTipi, { gizli: 1 }), L('ay', 'Ay', 98, r => r.ay, { tek: 1, h: r => ayAd(r.ay) }), M('belge', 'Tüketim Belgesi', 126, r => r.belge, { tek: 1 }),
        M('kod', 'Stok Kodu', 122, r => r.kod, { tek: 1, h: kodHucre }), M('ad', 'Ürün Adı', 124, r => r.ad, { h: r => adHucre(r.ad) }), L('tip', 'Stok Tipi', 92, r => r.tip, { gz: 'hbad' }), L('sirket', 'Şirket', 84, r => r.sirket), L('depo', 'Depo', 124, r => r.depoTam, { gizli: 1 }),
        ...AGAC().map(c => Object.assign(c, { gizli: 1, gz: '' })), BIRIM(),
        QS('recete', 'Gerçek Reçete', 82, r => r.recete, kume(FIIF, { h: r => r.recete ? sayE(r.recete) : '—' })), QS('fiili', 'Tüketim', 78, r => r.fiili, kume(FIIF, { h: r => `<b>${sayE(r.fiili)}</b>` })), Y('fire', 'Fire %', 66, r => r.fire, () => '', kume(FIIF, { h: r => r.recete ? yonY(r.fire) : '—', ac: 'Fire % = (Fiili Tüketim − Gerçek Reçete) ÷ Gerçek Reçete' })),
        QS('receteR', 'Muhasebe Reçetesi', 86, r => r.receteR, kume(MUHF, { h: r => r.receteR ? sayE(r.receteR) : '—' })), QS('muhasebe', 'Çıkış', 78, r => r.muhasebe, kume(MUHF)), Y('fireR', 'Sapma %', 66, r => r.fireR, () => '', kume(MUHF, { h: r => r.receteR ? yonY(r.fireR) : '—', ac: 'Sapma % = (Muhasebe Çıkışı − Muhasebe Reçetesi) ÷ Muhasebe Reçetesi' })),
        N('maliyet', 'Birim Maliyet (₺)', 80, r => r.maliyet, 2, { top: 0, ust: 'Tüketim Değeri', ur: 'd', gizli: 1 }), TL('fiiliBedel', 'Fiili (₺)', 94, r => r.fiiliBedel, { ust: 'Tüketim Değeri', ur: 'd' }), TL('muhBedel', 'Muhasebe (₺)', 94, r => r.muhBedel, { ust: 'Tüketim Değeri', ur: 'd' }),
        QS('sapma', 'Miktar', 82, r => r.sapma, { ust: 'Fark (X)', ur: 'x', ustAc: 'Fark (X) = Fiili Tüketim − Muhasebe Çıkışı; iki formül ayrı olduğu için beklenen bir farktır' }, 1), TL('sapmaBedel', 'Değer (₺)', 94, r => r.sapmaBedel, { ust: 'Fark (X)', ur: 'x', h: r => yon(r.sapmaBedel, tl(r.sapmaBedel)) }), Y('sapmaYuzde', 'Fark %', 68, r => r.sapmaYuzde, () => '', { ust: 'Fark (X)', ur: 'x', gizli: 1, h: r => yonY(r.sapmaYuzde) }),
        QS('muhStok', 'Muhasebe Stoku', 80, r => r.muhStok, { ust: 'Stok Karşılaştırması', ur: 'k', top: 0 }), QS('fiiliStok', 'Fiili Stok', 76, r => r.fiiliStok, { ust: 'Stok Karşılaştırması', ur: 'k', top: 0 }),
        L('stokUyum', 'Stok Uyumu', 106, r => r.stokUyum, { ust: 'Stok Karşılaştırması', ur: 'k', tek: 1, h: r => rozet(r.stokUyum, BELGE_AC[r.stokUyum]) }), L('receteUyum', 'Reçete Stok Uyumu', 116, r => r.receteUyum, { ust: 'Stok Karşılaştırması', ur: 'k', tek: 1, h: r => rozet(r.receteUyum, BELGE_AC[r.receteUyum]) })
      ]
    },
    hareket: {
      // AYLIK STOK HAREKETLERİ (15 Kas 2026: "Stok ve Uyum" sekmesi buraya katıldı — kullanıcı: "hiçbir bilgiyi azaltmamalısın, sadece taşıyabilirsin").
      // İki sekmenin BÜTÜN sütunları bu tablodadır. Hepsi tek ekrana sığmadığı için şeritte iki görünüm düğmesi var (HG):
      //   'a' Aylık Hareket  = eski Aylık Stok Hareketleri'nin varsayılan sütunları (+ Stok Durumu)
      //   's' Stok ve Uyum   = eski Stok ve Uyum'un varsayılan sütunları
      // Sütundaki gr alanı o sütunun hangi görünümde varsayılan açık olduğunu söyler (boş = ikisinde de). Gizli kalan her sütun Sütunlar'dan açılabilir.
      ad: 'Aylık Stok Hareketleri', sira: { k: 'fiiliBedel', y: -1 }, satir: () => kartSatir().filter(k => !H.incele || k.sonuc === 'İncele'), tik: 'detay', detayHtml: uyumDetay,
      kol: [
        ...KIMLIK(), L('depo', 'Depo', 124, r => r.depoTam, { h: r => esc(r.depo), gizli: 1 }), M('lot', 'Parti / Lot', 88, r => r.lot, { tek: 1, gizli: 1 }), ...AGAC().map(c => Object.assign(c, { gizli: 1, gz: '' })), BIRIM(),
        N('maliyet', 'Birim Maliyet (₺)', 86, r => r.maliyet, 2, { top: 0, gizli: 1 }),
        QS('muhMiktar', 'Miktar', 82, r => r.muhMiktar, kume(GMUH, { gr: 's' })), TL('muhBedel', 'Değer (₺)', 98, r => r.muhBedel, kume(GMUH, { gr: 's' })),
        QS('fiiliMiktar', 'Miktar', 82, r => r.fiiliMiktar, kume(GFII, { gr: 's', h: r => `<b>${sayE(r.fiiliMiktar)}</b>` })), TL('fiiliBedel', 'Değer (₺)', 98, r => r.fiiliBedel, kume(GFII, { gr: 's', h: r => `<b>${tl(r.fiiliBedel)}</b>` })),
        QS('fark', 'Miktar', 84, r => r.fark, kume(FRK, { gr: 's' }), 1), TL('farkBedel', 'Değer (₺)', 104, r => r.farkBedel, kume(FRK, { gr: 's', h: r => yon(r.farkBedel, tl(r.farkBedel)) })),
        Y('farkYuzde', 'Fark %', 58, r => r.farkYuzde, () => '', kume(FRK, { gizli: 1, h: r => yonY(r.farkYuzde) })),
        QS('minStok', 'Minimum Stok', 80, r => r.minStok, kume(DRM, { gizli: 1 })), QS('kritikStok', 'Kritik Stok', 78, r => r.kritikStok, kume(DRM, { gizli: 1 })),
        L('durum', 'Durum', 128, r => r.durum, kume(DRM, { tek: 1, h: r => rozet(r.durum, 'Kritik stok ' + mikE(r.kritikStok, r.birim) + ' · minimum stok ' + mikE(r.minStok, r.birim)) })),
        L('risk', 'Risk Seviyesi', 92, r => r.risk, kume(DRM, { tek: 1, gizli: 1, h: r => rozet(r.risk, r.riskAc) })),
        QS('oncF', 'Önceki Ay Sonu', 84, r => r.oncF, kume(AYF)), QS('girF', 'Ay İçi Giriş', 72, r => r.girF, kume(AYF)), QS('tukF', 'Fiili Tüketim', 74, r => r.tukF, kume(AYF)), QS('bekF', 'Beklenen Ay Sonu', 82, r => r.bekF, kume(AYF)),
        QS('sonF', 'Ay Sonu Stok', 80, r => r.sonF, kume(AYF, { gr: 'a', h: r => r.sonF == null ? '—' : `<b>${sayE(r.sonF)}</b>` })), QS('farkF', 'Fark', 72, r => r.farkF, kume(AYF), 1),
        QS('oncM', 'Önceki Ay Sonu', 84, r => r.oncM, kume(AYM, { gr: 'a' })), QS('girM', 'Ay İçi Giriş', 72, r => r.girM, kume(AYM, { gr: 'a' })), QS('tukM', 'Muhasebe Çıkışı', 80, r => r.tukM, kume(AYM)),
        QS('bekM', 'Beklenen Ay Sonu', 82, r => r.bekM, kume(AYM, { gr: 'a' })), QS('sonM', 'Ay Sonu Stok', 80, r => r.sonM, kume(AYM, { gr: 'a', h: r => r.sonM == null ? '—' : `<b>${sayE(r.sonM)}</b>` })), QS('farkM', 'Fark', 72, r => r.farkM, kume(AYM, { gr: 'a' }), 1),
        L('aylik', 'Aylık Uyum', 106, r => r.aylik, kume(AYL, { ust: 'Aylık Kontrol', tek: 1, h: r => rozet(r.aylik, r.aylikDetay) })),
        ...UCLU_KOL(), L('sonuc', 'Sonuç', 94, r => r.sonuc, { tek: 1, gr: 'a', h: r => rozet(r.sonuc, r.aksiyon) })
      ]
    },
    hiyerarsi: { ad: 'Fiili Stok Takibi', sira: { k: 'yeterlilikAy', y: 1 }, satir: () => takipSatir('f'), tik: 'urun', kol: [] },
    muhtakip: { ad: 'Muhasebe Stok Takibi', sira: { k: 'yeterlilikAy', y: 1 }, satir: () => takipSatir('m'), tik: 'urun', kol: [] },   // sütunlar veri gelince kurulur (takipKol)
  };

  const SEK = {
    demirbas: {
      // DEMİRBAŞ LİSTESİ (16 Kas 2026, "demirbaş (üretim makineleri bilgileri) ve küçük demirbaşlar da girilebilsin"): yalnız Demirbaş grubunda görünür.
      // Satırlar Makine / Ekipman ve Küçük Demirbaş tipindeki stok satırlarıdır; kullanıcının girdiği kayıtlarda (r.dm) makine bilgileri dolar, örnek kayıtlarda boştur.
      // "Yeni Demirbaş" ile kayıt açılır; İşlem sütunundan düzenlenir / silinir. Muhasebe (R) ve fiili adet / değer ayrı kümelerdedir.
      ad: 'Demirbaş Listesi', sira: { k: 'fDeger', y: -1 }, satir: () => S.filter(s => stokUyar(s) && DEMIRBAS_TIP.includes(s.tip)), tik: 'detay', bosYazi: 'Demirbaş kaydı yok. "Yeni Demirbaş" ile ekleyin.',
      kol: [
        M('kod', 'Demirbaş Kodu', 108, r => r.kod, { tek: 1, h: kodHucre }), M('ad', 'Demirbaş Adı', 140, r => r.ad, { h: r => adHucre(r.ad) }), L('tip', 'Tür', 116, r => r.tip, { tek: 1 }), L('sirket', 'Şirket', 88, r => r.sirket),
        L('depo', 'Bulunduğu Yer', 118, r => r.depoTam, { h: r => esc(r.depo) }), M('yer', 'Bölüm / Hat', 100, r => (r.dm && r.dm.yer) || '', { gizli: 1 }),
        M('marka', 'Marka', 88, r => (r.dm && r.dm.marka) || '', kume(DMB)), M('model', 'Model', 92, r => (r.dm && r.dm.model) || '', kume(DMB)), M('seriNo', 'Seri No', 100, r => (r.dm && r.dm.seriNo) || '', kume(DMB, { tek: 1 })),
        N('uretimYili', 'Üretim Yılı', 62, r => (r.dm && r.dm.uretimYili) || 0, 0, kume(DMB, { top: 0, gizli: 1, h: r => r.dm && r.dm.uretimYili ? String(r.dm.uretimYili) : '—' })), M('kapasite', 'Kapasite', 96, r => (r.dm && r.dm.kapasite) || '', kume(DMB)),
        D('alimTarihi', 'Alım Tarihi', 84, r => (r.dm && r.dm.alimTarihi) || ''), L('tedarikci', 'Tedarikçi', 110, r => r.tedarikci, { gizli: 1 }),
        QS('rMiktar', 'Adet', 56, r => r.rMiktar, kume(MUH)), TL('rDeger', 'Değer (₺)', 96, r => r.rDeger, kume(MUH)),
        QS('fiili', 'Adet', 56, r => r.fiili, kume(FII, { h: r => `<b>${sayE(r.fiili)}</b>` })), N('fMaliyet', 'Birim Bedel (₺)', 92, r => r.fMaliyet, 2, kume(FII, { top: 0 })), TL('fDeger', 'Değer (₺)', 100, r => r.fDeger, kume(FII, { h: r => `<b>${tl(r.fDeger)}</b>` })),
        L('dDurum', 'Durum', 96, r => (r.dm && r.dm.durum) || '', { tek: 1, h: r => r.dm && r.dm.durum ? rozet(r.dm.durum) : '—' }), M('zimmet', 'Zimmet / Sorumlu', 108, r => (r.dm && r.dm.zimmet) || ''),
        D('garantiBitis', 'Garanti Bitiş', 86, r => (r.dm && r.dm.garantiBitis) || '', { gizli: 1 }), D('sonBakim', 'Son Bakım', 84, r => (r.dm && r.dm.sonBakim) || '', { gizli: 1 }),
        D('sonrakiBakim', 'Sonraki Bakım', 88, r => (r.dm && r.dm.sonrakiBakim) || '', { h: r => r.dm && r.dm.sonrakiBakim ? `<span class="${r.dm.sonrakiBakim < V.bugun ? 'kirmizi' : ''}" title="${r.dm.sonrakiBakim < V.bugun ? 'Bakım tarihi geçti' : ''}">${tarih(r.dm.sonrakiBakim)}</span>` : '—' }),
        M('aciklama', 'Açıklama', 150, r => (r.dm && r.dm.aciklama) || '', { gizli: 1 }), M('giren', 'Kaydı Giren', 96, r => (r.dm && r.dm.giren) || '', { gizli: 1 }),
        { k: 'dIslem', ad: 'İşlem', w: 68, t: 'm', tek: 1, v: () => '', h: r => r.ek ? urunDugme(r, 1) : '<span class="text-muted" title="Örnek veri kaydı; düzenlenemez">Örnek</span>' }
      ]
    },
    stok: {
      ad: 'Stok Listesi', sira: { k: 'fDeger', y: -1 }, satir: () => S.filter(s => stokUyar(s)), tik: 'detay',
      kol: [
        M('kod', 'Stok Kodu', 114, r => r.kod, { tek: 1, h: kodHucre }), M('ad', 'Ürün Adı', 146, r => r.ad, { h: r => r.ek && r.dm.kaynak !== 'Demirbaş' ? cift(esc(r.ad), urunDugme(r)) : adHucre(r.ad) }), L('tip', 'Stok Tipi', 100, r => r.tip, { gz: 'a' }),
        L('sirket', 'Şirket', 96, r => r.sirket, { tek: 1 }), L('depo', 'Depo', 134, r => r.depoTam, { tek: 1, h: r => esc(r.depo) }), L('konum', 'Stok Konumu', 84, r => r.konum, { gizli: 1 }), L('mulkiyet', 'Mülkiyet', 92, r => r.mulkiyet, { gizli: 1 }),
        M('lot', 'Parti / Lot', 88, r => r.lot, { tek: 1, gz: 'adm' }), ...AGAC(),
        // --- Muhasebe Stoku (R): kayıtlı miktar, birim maliyet, değer
        Q('rMiktar', 'Miktar', 96, r => r.rMiktar, kume(MUH)), N('rMaliyet', 'Birim Maliyet (₺)', 92, r => r.rMaliyet, 2, kume(MUH, { top: 0 })), TL('rDeger', 'Değer (₺)', 106, r => r.rDeger, kume(MUH)),
        // --- Fiili Stok: sayım sonucu miktar, fiili birim maliyet, değer, kullanılabilir
        Q('sayilan', 'Sayılan', 92, r => r.sayilan, kume(FII, { gizli: 1 })), Q('fiili', 'Miktar', 96, r => r.fiili, kume(FII, { h: r => `<b>${mik(r.fiili, r.birim)}</b>` })), N('fMaliyet', 'Birim Maliyet (₺)', 92, r => r.fMaliyet, 2, kume(FII, { top: 0 })),
        TL('fDeger', 'Değer (₺)', 106, r => r.fDeger, kume(FII, { h: r => `<b>${tl(r.fDeger)}</b>` })), Q('kMiktar', 'Kullanılabilir', 106, r => r.kMiktar, kume(FII)), TL('kDeger', 'Kullanılabilir (₺)', 108, r => r.kDeger, kume(FII, { gizli: 1 })),
        // --- Fark (X = Fiili − Muhasebe)
        Q('x', 'Miktar', 100, r => r.x, kume(FRK, { h: r => yon(r.x, mik(r.x, r.birim)) })), Y('xYuzde', 'Fark %', 56, r => r.xYuzde, () => '', kume(FRK, { gizli: 1 })), TL('xDeger', 'Değer (₺)', 102, r => r.xDeger, kume(FRK, { h: r => yon(r.xDeger, tl(r.xDeger)) })),
        TL('mEtki', 'Miktar Etkisi (₺)', 104, r => r.mEtki, kume(FRK, { gizli: 1 })), TL('malEtki', 'Maliyet Etkisi (₺)', 104, r => r.malEtki, kume(FRK, { gizli: 1 })),
        D('sonHareket', 'Son Hareket', 86, r => r.sonHareket, { h: hareketHucre, gz: 'b' }), D('skt', 'Son Kullanma', 90, r => r.skt, { h: sktHucre, gz: 'admt' }),
        L('mDurum', 'Fark Yönü (X)', 118, r => r.mDurum, { tek: 1, h: r => rozet(r.mDurum) }), L('sktDurum', 'SKT Durumu', 104, r => r.sktDurum, { tek: 1, gizli: 1, h: r => rozet(r.sktDurum) }), L('hDurum', 'Hareket Durumu', 92, r => r.hDurum, { tek: 1, gizli: 1, h: r => rozet(r.hDurum) }),
        L('oncelik', 'Alım Önceliği', 100, r => r.oncelik, { tek: 1, gizli: 1, h: r => rozet(r.oncelik) }), L('birim', 'Birim', 50, r => r.birim, { gizli: 1 }), L('tedarikci', 'Tedarikçi', 130, r => r.tedarikci, { gizli: 1 })
      ]
    },
    tuketim: {
      // 16 Kas 2026: stok eşikleri giriş kutuları eklendi; sığması için miktarlar birimsiz yazılır (QK, eski ondalık hassasiyetiyle), birim Kullanılabilir Stok sütununda görünür
      // (ayrı Birim sütunu Sütunlar'dan açılır). Dip toplam tek birim süzülüyken yazılır.
      ad: 'Tüketim ve Yeterlilik', sira: { k: 'yeterlilik', y: 1 }, satir: () => P.filter(planUyar), sayacW: 46,   // sıra no sütunu: bu sekme sıkışık, üç haneli sıra no kesilmesin
      kol: [
        M('kod', 'Stok Kodu', 122, r => r.kod, { tek: 1, h: kodHucre }), M('ad', 'Ürün Adı', 92, r => r.ad, { h: r => adHucre(r.ad) }), L('tip', 'Stok Tipi', 98, r => r.tip, { gz: 'a' }), L('sirket', 'Şirket', 66, r => r.sirket), L('depo', 'Depo', 120, r => r.depoTam, { h: r => esc(r.depo), gizli: 1 }),
        ...AGAC().map(c => c.k === 'fGrup' ? Object.assign(c, { w: 88 }) : c), L('birim', 'Birim', 50, r => r.birim, { tek: 1, gizli: 1 }),
        // muhasebe (R formülü) ve fiili (gerçek formül) tüketimi ayrı ayrı; X bilgi amaçlıdır. Alım önerisi fiili tarafa göre hesaplanır; muhasebe yeterliliği kayıtlı stokun ne zaman tükeneceğini gösterir.
        QK('tuketimR', 'Dönem Tüketimi', 92, r => r.tuketimR, kume(MUHF)), QK('muhStok', 'Stok', 78, r => r.muhStok, kume(MUHF)), N('yeterlilikR', 'Yeterlilik (Gün)', 90, r => r.yeterlilikR === null ? 99999 : r.yeterlilikR, 0, kume(MUHF, { top: 0, h: r => r.yeterlilikR === null ? '—' : `<span class="${r.yeterlilikR < r.tedarikGun ? 'kirmizi' : ''}">${say(r.yeterlilikR, 0)}</span>` })),
        QK('tuketim', 'Dönem Tüketimi', 92, r => r.tuketim, kume(FIIF, { h: r => `<b>${mikS(r.tuketim, r.birim)}</b>` })), QK('gunluk', 'Günlük Ortalama', 84, r => r.gunluk, kume(FIIF)), QK('ay30', '30 Gün Eşdeğeri', 84, r => r.ay30, kume(FIIF, { gizli: 1 })), Q('kullan', 'Kullanılabilir Stok', 114, r => r.kullan, kume(FIIF)),
        N('yeterlilik', 'Yeterlilik (Gün)', 90, r => r.yeterlilik === null ? 99999 : r.yeterlilik, 0, kume(FIIF, { top: 0, h: r => r.yeterlilik === null ? '—' : `<b class="${r.yeterlilik < r.tedarikGun ? 'kirmizi' : ''}">${say(r.yeterlilik, 0)}</b>` })),
        QK('tuketimX', 'Tüketim', 92, r => r.tuketimX, kume(FRK), 1),
        // STOK EŞİKLERİ (16 Kas 2026, "kritik stok vs girelim"): giriş kutuları; kutu boşsa otomatik değer geçerli. Eski salt okunur sütunlar (yürürlükteki değerler) Sütunlar'dan açılır.
        ESG('eKritik', 'Kritik Stok', 78, 'kritik', r => r.guvenlik), ESG('eMin', 'Minimum Stok', 78, 'minimum', r => r.minStok), ESG('eTedarik', 'Tedarik (Gün)', 66, 'tedarikGun', r => r.tedarikGun), ESG('eHedef', 'Hedef Stok', 86, 'hedef', r => r.hedef),
        L('durum', 'Stok Durumu', 136, r => r.durum, kume(ESK, { tek: 1, h: r => rozet(r.durum, 'Fiili stok ' + mikE(r.fiiliStok, r.birim) + ' · kritik stok ' + mikE(r.guvenlik, r.birim) + ' · minimum stok ' + mikE(r.minStok, r.birim)) })),
        N('tedarikGun', 'Tedarik Süresi (Gün)', 72, r => r.tedarikGun, 0, { top: 0, gizli: 1 }), QK('guvenlik', 'Güvenlik Stoku', 84, r => r.guvenlik, { gizli: 1 }), QK('hedef', 'Hedef Stok', 84, r => r.hedef, { gizli: 1 }),
        QK('acikAlim', 'Açık Alım', 92, r => r.acikAlim, { h: r => r.acikAlim ? cift(mikS(r.acikAlim, r.birim), `<span class="${r.uygunAcik ? '' : 'kirmizi'}">${tarih(r.acikTarih)}</span>`) + (r.uygunAcik ? '' : '<small class="kirmizi" title="Gecikmiş ya da hedef dönem sonrasına kalan alım net stok pozisyonuna eklenmez">Sayılmadı</small>') : '—' }),
        QK('yoldaki', 'Yoldaki Alım', 84, r => r.yoldaki, { h: r => r.yoldaki ? cift(mikS(r.yoldaki, r.birim), tarih(r.yoldakiTarih)) : '—' }), QK('net', 'Net Stok Pozisyonu', 88, r => r.net, { gizli: 1 }),
        QK('oneri', 'Önerilen Alım', 92, r => r.oneri, { h: r => r.oneri > 0 ? `<b>${mikS(r.oneri, r.birim)}</b>` : '—' }), TL('oneriDeger', 'Önerilen Alım (₺)', 110, r => r.oneriDeger, { h: r => r.oneriDeger > 0 ? tl(r.oneriDeger) : '—' }),
        L('oncelik', 'Öncelik', 130, r => r.oncelik, { tek: 1, h: r => rozet(r.oncelik) }), L('tedarikci', 'Tedarikçi', 104, r => r.tedarikci, { gizli: 1 }), L('sorumlu', 'Sorumlu', 100, r => r.sorumlu, { gizli: 1 }),
        QK('recete', 'Gerçek Reçete', 88, r => r.recete, kume(FIIF, { gizli: 1 })), QK('receteFark', 'Gerçek Reçete − Tüketim', 92, r => r.receteFark, kume(FIIF, { gizli: 1 }), 1),
        QK('receteR', 'Muhasebe Reçetesi', 88, r => r.receteR, kume(MUHF, { gizli: 1 })), QK('receteFarkR', 'Muhasebe Reçetesi − Çıkış', 92, r => r.receteFarkR, kume(MUHF, { gizli: 1 }), 1)
      ]
    },
    sayim: {
      ad: 'Sayım ve Risk', sira: { k: 'xDeger', y: 1 }, satir: () => S.filter(s => stokUyar(s) && (!H.farkli || s.mDurum !== 'Eşit') && (!H.riskli || s.riskli)), tik: 'detay',
      kol: [
        M('kod', 'Stok Kodu', 116, r => r.kod, { tek: 1, h: kodHucre }), M('ad', 'Ürün Adı', 116, r => r.ad, { h: r => adHucre(r.ad) }), L('sirket', 'Şirket', 86, r => r.sirket), L('depo', 'Depo', 116, r => r.depoTam, { h: r => esc(r.depo), gizli: 1 }), M('lot', 'Parti / Lot', 86, r => r.lot, { tek: 1, gz: 'adm' }),
        // --- Fiili Stok (sayım) · Muhasebe Stoku (R) · Fark (X)
        D('sayimTarihi', 'Sayım Tarihi', 90, r => r.sayimTarihi, kume(FII)), Q('sayilan', 'Sayılan', 92, r => r.sayilan, kume(FII)), Q('giris', 'Sayım Sonrası Giriş', 84, r => r.giris, kume(FII, { gizli: 1 })), Q('cikis', 'Sayım Sonrası Çıkış', 84, r => r.cikis, kume(FII, { gizli: 1 })),
        Q('fiili', 'Miktar', 92, r => r.fiili, kume(FII, { h: r => `<b>${mik(r.fiili, r.birim)}</b>` })), Q('rMiktar', 'Miktar', 92, r => r.rMiktar, kume(MUH)),
        Q('x', 'Miktar', 100, r => r.x, kume(FRK, { h: r => yon(r.x, mik(r.x, r.birim)) })), Y('xYuzde', 'Fark %', 68, r => r.xYuzde, () => '', kume(FRK)), TL('xDeger', 'Değer (₺)', 100, r => r.xDeger, kume(FRK, { h: r => yon(r.xDeger, tl(r.xDeger)) })),
        D('skt', 'Son Kullanma', 90, r => r.skt, { h: sktHucre, gz: 'adm' }), L('sktDurum', 'SKT Durumu', 132, r => r.sktDurum, { tek: 1, gz: 'adm', h: r => rozet(r.sktDurum) }),
        D('sonHareket', 'Son Hareket', 86, r => r.sonHareket, { h: hareketHucre }), L('hDurum', 'Hareket', 86, r => r.hDurum, { tek: 1, h: r => rozet(r.hDurum) }),
        Q('bloke', 'Bloke / Karantina', 94, r => r.bloke, { h: r => r.bloke ? mik(r.bloke, r.birim) : '—' }), Q('rezerve', 'Rezerve', 80, r => r.rezerve, { gizli: 1, h: r => r.rezerve ? mik(r.rezerve, r.birim) : '—' }), Q('kMiktar', 'Kullanılabilir', 92, r => r.kMiktar, { gizli: 1 }),
        L('farkNedeni', 'Fark Nedeni', 96, r => r.farkNedeni), L('sorumlu', 'Sorumlu', 96, r => r.sorumlu), L('aksiyon', 'Düzeltici Aksiyon', 150, r => r.aksiyon, { gizli: 1 }),
        D('hedefTarih', 'Hedef Tarih', 92, r => r.hedefTarih, { h: r => r.hedefTarih ? `<span class="${r.hedefTarih < V.bugun ? 'kirmizi' : ''}">${tarih(r.hedefTarih)}</span>` : '—' }), L('onay', 'Onay Durumu', 114, r => r.onay, { tek: 1, h: r => rozet(r.onay) || '—' }),
        M('sayimBelge', 'Sayım Belge No', 110, r => r.sayimBelge, { gizli: 1, tek: 1 }), M('duzeltmeBelge', 'Düzeltme Belge No', 110, r => r.duzeltmeBelge, { gizli: 1, tek: 1 })
      ]
    },
    ozet: { ad: 'Özet' }
  };
  Object.assign(SEK, UYUM_SEK);

  // ---------- sütun görünürlüğü ----------
  const kolAnahtar = p => 'stk-kol-' + p + '-' + GR + (p === 'hareket' ? '-' + HG : '');
  const kolAyar = p => { try { return JSON.parse(localStorage.getItem(kolAnahtar(p)) || '{}'); } catch (e) { return {}; } };
  const gorunenKol = p => { const a = kolAyar(p); return SEK[p].kol.filter(c => c.k in a ? a[c.k] : !(c.gizli || (c.gz || '').includes(GR) || (c.gr && c.gr !== HG))); };

  // ---------- çoklu seçim açılır listesi ----------
  let pop = null, popKapandi = 0;
  const popKapat = () => { if (pop) { pop.el.remove(); pop.dugme.classList.remove('acik'); pop = null; popKapandi = Date.now(); } };
  function popAc(dugme, secenekler, secili, uygula, o = {}) {
    if (pop && pop.dugme === dugme) { popKapat(); return; } popKapat();
    const el = document.createElement('div'); el.className = 'sr-pop sade'; let sec = new Set(secili);
    const liste = q => secenekler.filter(s => !q || kucuk(s.ad).includes(q)).map(s => `<label><input type="checkbox" value="${esc(s.v)}"${sec.has(s.v) ? ' checked' : ''}><span>${esc(s.ad || '—')}</span>${s.n !== undefined ? `<small>${s.n}</small>` : ''}</label>`).join('') || '<div style="padding:8px 10px;color:#94a3b8">Kayıt Yok</div>';
    el.innerHTML = `<div class="ust"><a data-i="tumu">${o.tumuAd || 'Tümü'}</a><a data-i="temizle">Temizle</a><span class="say"></span></div>${secenekler.length > 8 ? '<div class="ara"><input type="search" placeholder="Ara…"></div>' : ''}<div class="liste">${liste('')}</div>`;
    document.body.appendChild(el);
    const r = dugme.getBoundingClientRect(); el.style.minWidth = Math.max(190, r.width) + 'px'; el.style.top = Math.min(r.bottom + 3, window.innerHeight - el.offsetHeight - 8) + 'px'; el.style.left = Math.max(6, Math.min(r.left, window.innerWidth - el.offsetWidth - 8)) + 'px';
    const sayYaz = () => el.querySelector('.say').textContent = sec.size ? sec.size + ' Seçili' : (o.bosAd || 'Tümü');
    const bildir = () => { sayYaz(); uygula(o.tumuDolu ? [...sec] : (sec.size === secenekler.length ? [] : [...sec])); };
    el.addEventListener('change', e => { if (e.target.type !== 'checkbox') return; if (e.target.checked) sec.add(e.target.value); else sec.delete(e.target.value); bildir(); });
    el.addEventListener('click', e => { const i = e.target.dataset && e.target.dataset.i; if (!i) return; sec = new Set(i === 'tumu' && o.tumuDolu ? secenekler.map(s => s.v) : []); el.querySelector('.liste').innerHTML = liste(kucuk((el.querySelector('.ara input') || {}).value || '')); bildir(); });
    const ara = el.querySelector('.ara input'); if (ara) { ara.addEventListener('input', () => el.querySelector('.liste').innerHTML = liste(kucuk(ara.value))); ara.focus(); }
    sayYaz(); dugme.classList.add('acik'); pop = { el, dugme };
  }
  document.addEventListener('mousedown', e => { if (pop && !pop.el.contains(e.target) && !pop.dugme.contains(e.target)) popKapat(); });
  document.addEventListener('keydown', e => { if (e.key === 'Escape') popKapat(); });
  window.addEventListener('resize', () => { popKapat(); yukseklik(); });

  // ---------- genel süzgeç düğmeleri (kademeli seçenekler) ----------
  const say_ = (l, f, ad) => { const m = new Map(); l.forEach(x => { const v = f(x); if (v === '' || v == null) return; const o = m.get(v) || { v, ad: ad ? ad(x) : v, n: 0 }; o.n++; m.set(v, o); }); return [...m.values()].sort((a, b) => String(a.ad).localeCompare(String(b.ad), TR)); };
  function secenekler(a) { const l = say_(S.filter(s => stokUyar(s, a)), AL[a], a === 'urun' ? s => s.ad + ' · ' + s.kod : null); F[a].forEach(v => { if (!l.some(s => s.v === v)) l.push({ v, ad: v, n: 0 }); }); return l; }
  function suzDugmeYaz() {
    kok.querySelectorAll('#srSuz .sr-cs').forEach(d => {
      const f = d.dataset.f, l = F[f]; let yazi = d.dataset.bos || 'Tümü';
      if (l.length === 1) yazi = f === 'urun' ? ((S.find(s => s.kod === l[0]) || {}).ad || l[0]) : l[0]; else if (l.length > 1) yazi = l.length + ' Seçili';
      d.innerHTML = `<span class="e">${esc(d.dataset.e)}</span><span class="d">${esc(yazi)}</span>`; d.title = d.dataset.e + ': ' + yazi; d.classList.toggle('dolu', l.length > 0);
    });
    kok.querySelectorAll('#srGrup button').forEach(b => b.classList.toggle('aktif', b.dataset.gr === GR));
    cipYaz();
    $('#srDonemYazi').textContent = GRUP_AD[GR] + ' · ' + (F.sirket.length ? F.sirket.join(' + ') : 'Tüm Şirketler') + ' · rapor tarihi ' + tarih(V.bugun) + (/^(belge|hareket|hiyerarsi|muhtakip)$/.test(sekme) ? ' · Kontrol Ayı ' + ayAd(AY) + ' · Tolerans ± ' + sayE(TOL) : '');
  }
  $('#srSuz').addEventListener('click', e => { const d = e.target.closest('.sr-cs'); if (!d) return; const f = d.dataset.f; popAc(d, secenekler(f), F[f], l => { F[f] = l; suzDugmeYaz(); ciz(); }, { bosAd: d.dataset.bos || 'Tümü' }); });
  let araZ; $('#srAra').addEventListener('input', e => { clearTimeout(araZ); araZ = setTimeout(() => { F.ara = kucuk(e.target.value.trim()); ciz(); }, 200); });
  $('#srTemizle').addEventListener('click', () => { F = BOS_F(); Object.keys(H).forEach(a => H[a] = false); Object.values(DURUM).forEach(d => d.suz = {}); $('#srAra').value = ''; suzDugmeYaz(); ciz(); });
  $('#srGrup').addEventListener('click', e => { const b = e.target.closest('button[data-gr]'); if (!b || b.dataset.gr === GR) return; GR = b.dataset.gr; if (GR === 'm') sekme = 'demirbas'; else if (sekme === 'demirbas') sekme = 'stok'; ['tip', 'urun', 'anaGrup', 'fGrup', 'sinif', 'urunGrubu', 'seri', 'depo'].forEach(a => F[a] = []); Object.values(DURUM).forEach(d => { d.suz = {}; d.sira = null; d.acik = new Set(); d.limit = 300; }); kap.scrollTop = 0; suzDugmeYaz(); ciz(); adresYaz(); });

  // ---------- üst bölüm (14 Kas 2026 tasarımı): grup kartları, süzgeç etiketleri, "Tüm Süzgeçler" paneli ----------
  const HIZLI = ['sirket', 'depo', 'tip', 'mDurum'];   // çubukta her zaman görünen süzgeçler; kalanı panelde, seçilince çubukta etiket olur
  const ETK = {}; kok.querySelectorAll('#srSuz .sr-cs').forEach(d => ETK[d.dataset.f] = d.dataset.e);
  const kisaTl = v => Math.abs(v) >= 1e6 ? say(v / 1e6, 1) + ' Mn ₺' : Math.abs(v) >= 1e3 ? say(v / 1e3, 0) + ' Bin ₺' : tl(v);
  /** stok grubu kartları: her grubun (öbür süzgeçler uygulanmış) fiili stok değeri, satır sayısı ve X farkı */
  function grupYaz() {
    const secili = GR;
    kok.querySelectorAll('#srGrup button').forEach(b => {
      GR = b.dataset.gr; const l = S.filter(s => stokUyar(s)), x = topla(l, s => s.xDeger);
      b.querySelector('.deger').textContent = kisaTl(topla(l, s => s.fDeger));
      b.querySelector('.alt').innerHTML = `${say(l.length, 0)} Satır · X <b class="${x < 0 ? 'kirmizi' : ''}">${x > 0 ? '+' : ''}${kisaTl(x)}</b>`;
    });
    GR = secili;
  }
  function cipYaz() {
    const l = AL_K.filter(a => !HIZLI.includes(a) && F[a].length);
    $('#srCipler').innerHTML = l.map(a => { const v = F[a].length === 1 ? (a === 'urun' ? ((S.find(s => s.kod === F[a][0]) || {}).ad || F[a][0]) : F[a][0]) : F[a].length + ' Seçili'; return `<span class="st-cip" title="${esc(ETK[a] + ': ' + F[a].join(', '))}">${esc(ETK[a])}: <b>${esc(v)}</b><i class="fas fa-xmark" data-sil="${a}" title="Süzgeci kaldır"></i></span>`; }).join('');
    $('#srSuzSay').textContent = l.length || '';
  }
  const panelKapat = () => { $('#srSuzPanel').hidden = true; $('#srSuzAc').classList.remove('acik'); };
  $('#srSuzAc').addEventListener('click', () => { const p = $('#srSuzPanel'); p.hidden = !p.hidden; $('#srSuzAc').classList.toggle('acik', !p.hidden); });
  $('#srCipler').addEventListener('click', e => { const i = e.target.closest('[data-sil]'); if (!i) return; F[i.dataset.sil] = []; suzDugmeYaz(); ciz(); });
  document.addEventListener('mousedown', e => { if ($('#srSuzPanel').hidden) return; if (e.target.closest('#srSuzPanel, #srSuzAc, .sr-pop')) return; panelKapat(); });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !pop && Date.now() - popKapandi > 80) panelKapat(); });   // Esc önce açık seçim listesini kapatır; panel ikinci Esc'de kapanır

  // ---------- tablo ----------
  const tablo = $('#srTablo'), kap = $('#srKap');
  function yukseklik() { if ($('#srTabloKart').hidden) return; kap.style.maxHeight = Math.max(220, window.innerHeight - kap.getBoundingClientRect().top - 14) + 'px'; }
  const sayiCoz = s => parseFloat(String(s).replace(/\./g, '').replace(',', '.'));
  function sayiSuz(q, v, metin) {
    let m; q = q.replace(/\s/g, '');
    if ((m = /^(>=|<=|>|<|=)(-?[\d.,]+)$/.exec(q))) { const n = sayiCoz(m[2]); return m[1] === '>' ? v > n : m[1] === '<' ? v < n : m[1] === '>=' ? v >= n : m[1] === '<=' ? v <= n : Math.abs(v - n) < 0.005; }
    if ((m = /^(-?[\d.,]+)(?:-|\.\.)(-?[\d.,]+)$/.exec(q))) return v >= sayiCoz(m[1]) && v <= sayiCoz(m[2]);
    return metin.includes(q);
  }
  function kolonUyar(r, kol, suz) {
    for (const c of kol) {
      const q = suz[c.k]; if (q == null || q === '' || (Array.isArray(q) && !q.length)) continue; const v = c.v(r);
      if (c.t === 'l') { if (!q.includes(String(v ?? ''))) return false; }
      else if (c.t === 'd') { if (!tarih(v).includes(q)) return false; }
      else if (c.t === 'm') { if (!kucuk(v).includes(kucuk(q))) return false; }
      else if (!sayiSuz(q, +v || 0, say(v, c.hane ?? 2))) return false;
    }
    return true;
  }
  function cizBaslik() {
    const tn = SEK[sekme], Dr = durum(sekme), kol = gorunenKol(sekme), SW = tn.sayacW || 38, W = SW + topla(kol, c => c.w);
    tablo.style.minWidth = Math.round(W * 0.74) + 'px';
    tablo.querySelector('colgroup').innerHTML = `<col style="width:${(SW / W * 100).toFixed(3)}%">` + kol.map(c => `<col style="width:${(c.w / W * 100).toFixed(3)}%">`).join('');
    const sr = Dr.sira || tn.sira;
    // üst başlık satırı: yan yana aynı kümedeki sütunlar tek birleşik hücrede; küme sınırları (gb) gövdede ve dipte kalın çizgiyle ayrılır
    let ustHtml = ''; Dr.gb = [];
    kol.forEach((c, i) => { if (i > 0 && (c.ust || '') !== (kol[i - 1].ust || '')) Dr.gb.push(i); });
    if (kol.some(c => c.ust)) { ustHtml = '<tr class="ustgrup"><th></th>'; for (let i = 0; i < kol.length;) { let j = i; while (j < kol.length && (kol[j].ust || '') === (kol[i].ust || '')) j++; ustHtml += `<th colspan="${j - i}" class="${kol[i].ust ? 'ug ug-' + (kol[i].ur || 'd') : 'bos'}"${kol[i].ustAc ? ` title="${esc(kol[i].ustAc)}"` : ''}>${esc(kol[i].ust || '')}</th>`; i = j; } ustHtml += '</tr>'; }
    tablo.tHead.innerHTML = ustHtml + `<tr class="bas"><th data-k="">#</th>${kol.map(c => `<th data-k="${c.k}" class="${c.ust ? 'ug-' + (c.ur || 'd') + ' ' : ''}${sr && sr.k === c.k ? (sr.y > 0 ? 'art' : 'azal') : ''}" title="${esc(tamAd(c))} · sıralamak için tıklayın">${esc(c.ad)}</th>`).join('')}</tr>`
      + `<tr class="suzgec"><th></th>${kol.map(c => { const q = Dr.suz[c.k]; return c.t === 'l' ? `<th><button type="button" data-k="${c.k}" class="${q && q.length ? 'dolu' : ''}">${q && q.length ? (q.length === 1 ? esc(q[0]) : q.length + ' Seçili') : 'Tümü'}</button></th>` : `<th><input data-k="${c.k}" value="${esc(q || '')}" class="${q ? 'dolu' : ''}" placeholder="${c.t === 'p' || c.t === 'y' ? '> <' : 'Ara'}" title="${c.t === 'm' ? 'İçerir' : c.t === 'd' ? 'Tarih içerir (örn. 10.2026)' : 'Sayı süzgeci: >1000 · <=500 · 100-200 · =0'}"></th>`; }).join('')}</tr>`;
    const ustYuk = ustHtml ? tablo.tHead.rows[0].offsetHeight : 0; tablo.style.setProperty('--sr-ust', ustYuk + 'px'); tablo.style.setProperty('--sr-bas', (ustYuk + tablo.tHead.querySelector('tr.bas').offsetHeight) + 'px');
    tablo.tHead.querySelectorAll('tr.bas, tr.suzgec').forEach(tr => Dr.gb.forEach(i => tr.cells[i + 1] && tr.cells[i + 1].classList.add('gb')));
  }
  function cizGovde() {
    const tn = SEK[sekme], Dr = durum(sekme), kol = gorunenKol(sekme), sr = Dr.sira || tn.sira;
    const tum = Dr.tum = tn.satir(); let rows = tum.filter(r => kolonUyar(r, kol, Dr.suz));
    if (sr) { const c = SEK[sekme].kol.find(x => x.k === sr.k); if (c) rows = rows.slice().sort((a, b) => sirala(c.v(a), c.v(b)) * sr.y); }
    Dr.gorunen = rows; const n = kol.length + 1, goster = rows.slice(0, Dr.limit);
    const hucre = (c, r) => { const v = c.v(r), sayisal = c.t === 'p' || c.t === 'y'; return `<td class="${sayisal ? 'p' : c.t === 'd' || c.tek ? '' : 'yz'}${sayisal && v < 0 ? ' eksi' : ''}"${c.t === 'm' || c.t === 'l' ? ` title="${esc(v)}"` : ''}>${c.h ? c.h(r) : esc(v)}</td>`; };
    tablo.tBodies[0].innerHTML = !rows.length ? `<tr><td class="bos" colspan="${n}">${esc(tn.bosYazi || 'Süzgeçlere uyan kayıt yok.')}</td></tr>`
      : goster.map((r, i) => { const id = r.no ?? r.a ?? i, acik = tn.tik === 'detay' && Dr.acik.has(id); return `<tr class="${tn.tik ? 'tik' : ''}${acik ? ' acik' : ''}" data-i="${i}"><td class="sayac">${i + 1}</td>${kol.map(c => hucre(c, r)).join('')}</tr>${acik ? `<tr class="detay"><td colspan="${n}">${(tn.detayHtml || detayHtml)(r)}</td></tr>` : ''}`; }).join('')
        + (rows.length > goster.length ? `<tr class="detay"><td class="dahafazla" colspan="${n}"><button type="button" class="sr-dgm" data-daha="1">Sonraki 500 Satır</button> <button type="button" class="sr-dgm" data-daha="0">Tümünü Göster (${say(rows.length, 0)})</button></td></tr>` : '');
    const tekBirim = new Set(rows.map(r => r.birim)).size === 1 ? rows[0] && rows[0].birim : null;
    tablo.tFoot.innerHTML = !rows.length ? '' : `<tr><td class="sayac"></td>${kol.map((c, i) => {
      if (c.tf) return `<td class="p">${c.tf(rows)}</td>`;
      if (c.top === 'birim') return `<td class="p">${tekBirim ? (c.bs ? sayE(topla(rows, c.v)) : mik(topla(rows, c.v), tekBirim)) : ''}</td>`;   // bs: birimsiz sütun (birim ayrı sütunda)
      if (c.top === 1) { const t = topla(rows, c.v); return `<td class="p">${c.sayi ? say(t, c.hane ?? 0) : tl(t)}</td>`; }
      return `<td>${i === 0 ? 'Toplam · ' + say(rows.length, 0) : ''}</td>`;
    }).join('')}</tr>`;
    if (Dr.gb && Dr.gb.length) tablo.querySelectorAll('tbody tr:not(.detay), tfoot tr').forEach(tr => Dr.gb.forEach(i => tr.cells[i + 1] && tr.cells[i + 1].classList.add('gb')));
    $('#srKapsam').textContent = say(rows.length, 0) + (rows.length !== tum.length ? ' / ' + say(tum.length, 0) : '') + ' Kayıt';
  }
  /** stok satırının ayrıntısı: şablondaki Stok_Verisi alan kümeleri (kimlik, resmî stok, fiili sayım, kullanılabilirlik, mutabakat) */
  function detayHtml(s) {
    const kutu = (b, l) => `<div><h6>${b}</h6><dl>${l.filter(x => x[1] !== '' && x[1] != null).map(x => `<dt>${x[0]}</dt><dd title="${esc(String(x[1]).replace(/<[^>]+>/g, ''))}">${x[1]}</dd>`).join('')}</dl></div>`;
    return `<div class="sr-detay-izgara">`
      + kutu('Kimlik ve Yer', [['Stok Kodu', esc(s.kod)], ['Stok Tipi', esc(s.tip)], ['Şirket', esc(s.sirket)], ['Depo', esc(s.depoTam)], ['Konum', esc(s.konum)], ['Mülkiyet', esc(s.mulkiyet)], ['Parti / Lot', esc(s.lot)], ['Faaliyet Grubu', esc(s.grup)], ['Ürün Sınıfı', esc(s.sinif)], ['Tedarikçi', esc(s.tedarikci)]])
      + kutu('Muhasebe Stoku (R)', [['Miktar', mik(s.rMiktar, s.birim)], ['Birim Maliyet', say(s.rMaliyet, 2) + ' ₺'], ['Değer', tl(s.rDeger, 2)], ['Son Hareket', tarih(s.sonHareket)], ['Hareketsiz', s.hGun === null ? '' : s.hGun + ' Gün · ' + s.hDurum]])
      + kutu('Fiili Stok', [['Sayım Tarihi', tarih(s.sayimTarihi)], ['Sayım Belge No', esc(s.sayimBelge)], ['Sayılan', mik(s.sayilan, s.birim)], ['Sayım Sonrası Giriş', s.giris ? '+' + mik(s.giris, s.birim) : ''], ['Sayım Sonrası Çıkış', s.cikis ? '−' + mik(s.cikis, s.birim) : ''], ['Fiili Miktar', mik(s.fiili, s.birim)], ['Fiili Birim Maliyet', say(s.fMaliyet, 2) + ' ₺'], ['Fiili Değer', tl(s.fDeger, 2)]])
      + kutu('Fark (X = Fiili − Muhasebe)', [['X Miktar', yon(s.x, mik(s.x, s.birim))], ['Fark %', s.x ? yuzde(s.xYuzde) : ''], ['Miktar Etkisi', yon(s.mEtki, tl(s.mEtki, 2))], ['Maliyet Etkisi', yon(s.malEtki, tl(s.malEtki, 2))], ['X Değer', yon(s.xDeger, tl(s.xDeger, 2))], ['Bloke / Karantina', s.bloke ? mik(s.bloke, s.birim) : ''], ['Rezerve', s.rezerve ? mik(s.rezerve, s.birim) : ''], ['Kullanılabilir', mik(s.kMiktar, s.birim)], ['Son Kullanma', s.sktTakibi ? tarih(s.skt) + (s.sktKalan === null ? '' : ' · ' + s.sktKalan + ' Gün') : '']])
      + kutu('Mutabakat', [['Fark Yönü (X)', esc(s.mDurum)], ['Fark Nedeni', esc(s.farkNedeni)], ['Sorumlu', esc(s.sorumlu)], ['Düzeltici Aksiyon', esc(s.aksiyon)], ['Hedef Tarih', tarih(s.hedefTarih)], ['Onay Durumu', esc(s.onay)], ['Düzeltme Belge No', esc(s.duzeltmeBelge)], ['Alım Önceliği', esc(s.oncelik)]])
      + `</div>`;
  }
  tablo.tHead.addEventListener('click', e => {
    const dg = e.target.closest('tr.suzgec button'); if (dg) { const c = SEK[sekme].kol.find(x => x.k === dg.dataset.k), Dr = durum(sekme); popAc(dg, say_(Dr.tum || [], r => String(c.v(r) ?? '')), Dr.suz[c.k] || [], l => { Dr.suz[c.k] = l; dg.classList.toggle('dolu', l.length > 0); dg.textContent = l.length ? (l.length === 1 ? l[0] : l.length + ' Seçili') : 'Tümü'; Dr.limit = 300; cizGovde(); }); return; }
    if (e.target.closest('tr.suzgec')) return;
    const th = e.target.closest('th'); if (!th || !th.dataset.k) return; const Dr = durum(sekme), sr = Dr.sira || SEK[sekme].sira;
    Dr.sira = { k: th.dataset.k, y: sr && sr.k === th.dataset.k ? -sr.y : (/^(p|y|d)$/.test(SEK[sekme].kol.find(x => x.k === th.dataset.k).t) ? -1 : 1) }; cizBaslik(); cizGovde();
  });
  let suzZ; tablo.tHead.addEventListener('input', e => { const i = e.target.closest('tr.suzgec input'); if (!i) return; const Dr = durum(sekme); Dr.suz[i.dataset.k] = i.value.trim(); i.classList.toggle('dolu', !!i.value.trim()); Dr.limit = 300; clearTimeout(suzZ); suzZ = setTimeout(cizGovde, 180); });
  tablo.tBodies[0].addEventListener('click', e => {
    const gc = e.target.closest('[data-gecmis]'); if (gc) { e.preventDefault(); gecmisAc(gc.dataset.gecmis, gc.dataset.sirket); return; }
    const ur = e.target.closest('[data-urun]'); if (ur) { const u = (V.urunler || []).find(x => x.id === +ur.dataset.id); if (u) { if (ur.dataset.urun === 'sil') urunSil(u.id); else formAc(u.kaynak === 'Demirbaş' ? 'demirbas' : 'urun', u.id); } return; }
    const daha = e.target.closest('[data-daha]'); if (daha) { const Dr = durum(sekme); Dr.limit = daha.dataset.daha === '1' ? Dr.limit + 500 : Infinity; cizGovde(); return; }
    const tr = e.target.closest('tr.tik'); if (!tr || e.target.closest('a,button,input')) return; const Dr = durum(sekme), r = Dr.gorunen[+tr.dataset.i], tik = SEK[sekme].tik; if (!r) return;
    if (tik === 'detay') { if (Dr.acik.has(r.no)) Dr.acik.delete(r.no); else Dr.acik.add(r.no); cizGovde(); return; }
    // stok takibi satırı (fiili ya da muhasebe) → o ürüne süzülmüş Stok Listesi
    if (tik === 'urun') F.urun = [r.kod]; else return;
    suzDugmeYaz(); sekmeAc('stok');
  });

  // ---------- ÜRÜN GEÇMİŞİ (15 Kas 2026, "stok koduna tıkladığımızda o ürünün geçmişini görelim: ne zaman gelmiş, hangi tedarikçi") ----------
  // Her sekmede stok koduna (Özet'te ürün adına) tıklanınca açılır. Kayıtlar:
  //   Girişler  = giriş belgeleri (V.girisler): tarih, giriş türü, tedarikçi, irsaliye / üretim fişi no, fatura no, parti / lot, fiili miktar, muhasebe (faturalı) miktar, birim fiyat.
  //   Çıkışlar  = tüketim / çıkış belgeleri (Tüketim Verisi sekmesindeki kayıtlar): fiili tüketim ve muhasebe çıkışı ayrı sütunlarda.
  //   Sayımlar  = stok satırının yürürlükteki sayımı + (varsa) önceden kaydedilmiş sayım kayıtları (durumuyla).
  // Fiili ve muhasebe miktarı ayrı sütunlardır (temel kural). Pencere satırın şirketiyle açılır; şirket ve işlem türü düğmeleriyle süzülür; başlığa tıklanınca sıralanır; Excel'e aktarılır.
  kok.insertAdjacentHTML('beforeend', `<div class="gc-perde" id="srGecmis" hidden><div class="gc-pencere" role="dialog" aria-modal="true">
    <div class="gc-ust"><span class="st-ikon"><i class="fas fa-clock-rotate-left"></i></span><div class="gc-baslik"><div class="ad" id="gcAd"></div><div class="alt" id="gcAlt"></div></div>
      <div class="gc-sag"><button type="button" class="sr-dgm" id="gcExcel" title="Ürün geçmişini göründüğü gibi Excel'e aktar (.xlsx)"><i class="fas fa-file-excel"></i>Excel</button><button type="button" class="gc-kapat" id="gcKapat" title="Kapat (Esc)"><i class="fas fa-xmark"></i></button></div></div>
    <div class="sr-kpi gc-kpi" id="gcKpi" data-dinamik="kapali"></div>
    <div class="gc-serit"><span class="e">Şirket</span><span id="gcSirket"></span><span class="ayr"></span><span class="e">İşlem</span><span id="gcTur"></span><span class="sr-kapsam" id="gcKapsam"></span></div>
    <section class="gc-kap"><table class="sr-tablo" id="gcTablo" data-dinamik="kapali"><colgroup></colgroup><thead></thead><tbody></tbody><tfoot></tfoot></table></section>
  </div></div>`);
  const gcEl = $('#srGecmis'); let GC = null;
  const GC_TUR = [['', 'Tümü'], ['g', 'Girişler'], ['c', 'Çıkışlar'], ['s', 'Sayımlar']], GC_RENK = { g: '#15803d', c: '#b45309', s: '#64748b' };
  // sütun: anahtar, başlık, genişlik %, sıralama değeri
  const GC_KOL = [
    ['tarih', 'Tarih', 7, o => o.tarih], ['islem', 'İşlem', 10, o => o.islem], ['belge', 'Belge No', 8, o => o.belge], ['kaynak', 'Tedarikçi / Kaynak', 11, o => o.kaynak], ['sirket', 'Şirket', 8, o => o.sirket], ['depo', 'Depo', 10, o => o.depo],
    ['lot', 'Parti / Lot', 6, o => o.lot], ['fiili', 'Fiili Miktar', 8, o => o.sinif === 's' ? o.sayilan : o.fiili], ['muh', 'Muhasebe Miktarı', 8, o => o.sinif === 's' ? 0 : o.muh], ['fiyat', 'Birim Fiyat (₺)', 7, o => o.fiyat || 0], ['tutar', 'Tutar (₺)', 7, o => o.tutar || 0], ['fatura', 'Fatura No', 7, o => o.fatura]
  ];
  /** ürünün bütün kayıtları (bütün şirketler): girişler + çıkışlar + sayımlar */
  function gecmisKayit(kod) {
    const depoAd = {}; S.forEach(s => { if (s.kod === kod) depoAd[s.sirket + '|' + s.depoKod] = s.depo; });
    const dp = (sirket, depoKod) => depoAd[sirket + '|' + depoKod] || depoKod, l = [];
    (V.girisler || []).forEach(g => { if (g.kod !== kod) return; l.push({ sinif: 'g', tarih: g.tarih, islem: g.tur, belge: g.belge, kaynak: g.tedarikci || 'Kendi Üretimi', sirket: g.sirket, depo: dp(g.sirket, g.depoKod), lot: g.lot, fiili: g.fiili, muh: g.muhasebe, fiyat: g.fiyat, tutar: g.fiili * g.fiyat, fatura: g.fatura }); });
    (V.urunler || []).forEach(u => { if (u.kod !== kod || u.kaynak !== 'Demirbaş' || !u.alimTarihi) return; l.push({ sinif: 'g', tarih: u.alimTarihi, islem: 'Satınalma', belge: '', kaynak: u.tedarikci || 'Belirtilmedi', sirket: u.sirket, depo: u.depo, lot: u.lot || '', fiili: u.miktar, muh: u.muhMiktar, fiyat: u.maliyet, tutar: u.miktar * u.maliyet, fatura: '' }); });
    // listeye elle eklenen ürün (Stok Listesi › Ürün Ekle): kaydın açıldığı gün, girilen fiili miktarla tek giriş satırı
    (V.urunler || []).forEach(u => { if (u.kod !== kod || u.kaynak === 'Demirbaş') return; l.push({ sinif: 'g', tarih: String(u.girisZamani || '').slice(0, 10), islem: 'Listeye Eklendi', belge: '', kaynak: u.tedarikci || (u.giren ? 'Kaydı Giren: ' + u.giren : ''), sirket: u.sirket, depo: u.depo, lot: u.lot || '', fiili: u.miktar, muh: u.muhMiktar || 0, fiyat: u.maliyet, tutar: u.miktar * u.maliyet, fatura: '' }); });
    TB.forEach(t => { if (t.kod !== kod || (!t.fiili && !t.muhasebe)) return; l.push({ sinif: 'c', tarih: t.tarih, islem: t.tip === 'Mamul' || t.tip === 'Ticari Mal' ? 'Çıkış' : 'Tüketim', belge: t.belge, kaynak: '', sirket: t.sirket, depo: dp(t.sirket, t.depoKod), lot: '', fiili: -t.fiili, muh: -t.muhasebe, fiyat: t.maliyet, tutar: -t.fiiliBedel, fatura: '' }); });
    V.satirlar.forEach(s => { if (s.kod !== kod || !s.sayimTarihi) return; l.push({ sinif: 's', tarih: s.sayimTarihi, islem: 'Sayım', belge: s.sayimBelge, kaynak: '', sirket: s.sirket, depo: s.depo, lot: s.lot, sayilan: s.sayilan, fatura: '' }); });
    (V.sayimlar || []).forEach(k => { const a = String(k.anahtar).split('|'); if (a[2] !== kod) return; l.push({ sinif: 's', tarih: k.tarih, islem: 'Sayım', belge: k.belge, kaynak: [k.giren ? 'Sayan: ' + k.giren : '', k.durum].filter(Boolean).join(' · '), sirket: a[0], depo: dp(a[0], a[1]), lot: a[3] || '', sayilan: k.sayilan, fatura: '' }); });
    return l;
  }
  /** Özet tablolarındaki ürün adı: tıklanınca ürün geçmişi */
  const urunBag = r => `<a href="#" class="st-kod" data-gecmis="${esc(r.kod)}" data-sirket="${esc(r.sirket || '')}" title="${esc(r.kod)} · ürün geçmişini aç"><b>${esc(r.ad)}</b></a>`;
  function gecmisAc(kod, sirket) {
    if (!V) return; const sirketler = [...new Set(S.filter(s => s.kod === kod).map(s => s.sirket))].sort((a, b) => a.localeCompare(b, TR));
    GC = { kod, tum: gecmisKayit(kod), sirketler, sirket: sirketler.length > 1 && sirketler.includes(sirket) ? sirket : '', tur: '', sira: { k: 'tarih', y: -1 } };
    gcEl.hidden = false; gecmisCiz(); gcEl.querySelector('.gc-kap').scrollTop = 0;
  }
  function gecmisKapat() { gcEl.hidden = true; GC = null; }
  function gecmisCiz() {
    const st = S.filter(s => s.kod === GC.kod && (!GC.sirket || s.sirket === GC.sirket)), ilk = S.find(s => s.kod === GC.kod) || {}, birim = ilk.birim || '';
    const kap = GC.tum.filter(o => !GC.sirket || o.sirket === GC.sirket), gir = kap.filter(o => o.sinif === 'g').sort((a, b) => a.tarih.localeCompare(b.tarih));
    const kaynaklar = [...new Set(gir.map(o => o.kaynak))], son = gir[gir.length - 1], gF = topla(gir, o => o.fiili), gM = topla(gir, o => o.muh), gT = topla(gir, o => o.tutar);
    $('#gcAd').innerHTML = `${esc(GC.kod)} <span>·</span> ${esc(ilk.ad || '')}`;
    $('#gcAlt').textContent = ['Ürün Geçmişi', [...new Set(st.map(s => s.tip))].join(' · '), birim ? 'Birim: ' + birim : '', kaynaklar.length ? (kaynaklar.every(a => a === 'Kendi Üretimi') ? 'Kaynak: ' : 'Tedarikçi: ') + kaynaklar.join(', ') : ''].filter(Boolean).join('  •  ');
    const kpi = [
      ['İlk Giriş', gir.length ? tarih(gir[0].tarih) : '—', gir.length ? gir[0].kaynak : 'Giriş Kaydı Yok'],
      ['Son Giriş', son ? tarih(son.tarih) : '—', son ? son.kaynak + ' · ' + mikE(son.fiili, birim) : ''],
      ['Giriş Sayısı', gir.length + ' Belge', kaynaklar.length > 1 ? kaynaklar.length + ' Kaynak' : kaynaklar[0] || ''],
      ['Toplam Giriş (Fiili)', mikE(gF, birim), 'Muhasebe Girişi ' + mikE(gM, birim)],
      ['Ortalama Giriş Fiyatı', gF ? say(gT / gF, 2) + ' ₺' : '—', son ? 'Son Giriş ' + say(son.fiyat, 2) + ' ₺' : ''],
      ['Güncel Fiili Stok', mikE(topla(st, s => s.fiili), birim), 'Muhasebe Stoku (R) ' + mikE(topla(st, s => s.rMiktar), birim)]
    ];
    $('#gcKpi').innerHTML = kpi.map(k => `<div><div class="l">${k[0]}</div><div class="v" title="${esc(k[1])}">${esc(k[1])}</div><div class="s" title="${esc(k[2])}">${esc(k[2])}</div></div>`).join('');
    $('#gcSirket').innerHTML = (GC.sirketler.length > 1 ? [''].concat(GC.sirketler) : GC.sirketler).map(a => `<button type="button" class="sr-dgm${GC.sirket === a || GC.sirketler.length === 1 ? ' acik' : ''}" data-gs="${esc(a)}">${esc(a || 'Tümü (Konsolide)')}</button>`).join('');
    $('#gcTur').innerHTML = GC_TUR.map(a => `<button type="button" class="sr-dgm${GC.tur === a[0] ? ' acik' : ''}" data-gt="${a[0]}">${a[1]}${a[0] ? ' (' + kap.filter(o => o.sinif === a[0]).length + ')' : ''}</button>`).join('');
    const c = GC_KOL.find(x => x[0] === GC.sira.k) || GC_KOL[0], rows = kap.filter(o => !GC.tur || o.sinif === GC.tur).sort((a, b) => sirala(c[3](a), c[3](b)) * GC.sira.y || b.tarih.localeCompare(a.tarih));
    const t = $('#gcTablo'), isa = v => (v > 0 ? '+' : '') + sayE(v);
    t.querySelector('colgroup').innerHTML = '<col style="width:3%">' + GC_KOL.map(x => `<col style="width:${x[2]}%">`).join('');
    t.tHead.innerHTML = `<tr class="bas"><th class="ik">#</th>${GC_KOL.map(x => `<th data-k="${x[0]}" class="${GC.sira.k === x[0] ? (GC.sira.y > 0 ? 'art' : 'azal') : ''}" title="${esc(x[1])} · sıralamak için tıklayın">${esc(x[1])}${(x[0] === 'fiili' || x[0] === 'muh') && birim ? ' (' + esc(birim) + ')' : ''}</th>`).join('')}</tr>`;
    t.tBodies[0].innerHTML = !rows.length ? `<tr><td class="bos" colspan="${GC_KOL.length + 1}">Bu seçimde kayıt yok.</td></tr>` : rows.map((o, i) => {
      const s = o.sinif === 's', bos = '<span class="text-muted">—</span>';
      return `<tr><td class="sayac">${i + 1}</td><td>${tarih(o.tarih)}</td><td><span class="rz" style="--n:${GC_RENK[o.sinif]}">${esc(o.islem)}</span></td><td title="${esc(o.belge)}">${esc(o.belge) || bos}</td><td class="yz" title="${esc(o.kaynak)}">${o.kaynak ? (o.sinif === 'g' ? `<b>${esc(o.kaynak)}</b>` : esc(o.kaynak)) : bos}</td>`
        + `<td title="${esc(o.sirket)}">${esc(o.sirket)}</td><td title="${esc(o.depo)}">${esc(o.depo)}</td><td title="${esc(o.lot)}">${esc(o.lot) || bos}</td>`
        + (s ? `<td class="p" title="Sayılan miktar"><b>= ${sayE(o.sayilan)}</b></td><td class="p">${bos}</td><td class="p">${bos}</td><td class="p">${bos}</td>`
          : `<td class="p${o.fiili < 0 ? ' eksi' : ''}"><b>${isa(o.fiili)}</b></td><td class="p${o.muh < 0 ? ' eksi' : ''}">${o.muh ? isa(o.muh) : bos}</td><td class="p">${say(o.fiyat, 2)}</td><td class="p${o.tutar < 0 ? ' eksi' : ''}">${tl(o.tutar)}</td>`)
        + `<td title="${esc(o.fatura)}">${esc(o.fatura) || bos}</td></tr>`;
    }).join('');
    const har = rows.filter(o => o.sinif !== 's'), tF = topla(har, o => o.fiili), tM = topla(har, o => o.muh), tT = topla(har, o => o.tutar);
    t.tFoot.innerHTML = !rows.length ? '' : `<tr><td class="sayac"></td><td>Toplam · ${rows.length}</td><td></td><td></td><td></td><td></td><td></td><td></td><td class="p${tF < 0 ? ' eksi' : ''}" title="Girişler − çıkışlar (sayımlar hariç)">${har.length ? isa(tF) : ''}</td><td class="p${tM < 0 ? ' eksi' : ''}">${har.length ? isa(tM) : ''}</td><td></td><td class="p${tT < 0 ? ' eksi' : ''}">${har.length ? tl(tT) : ''}</td><td></td></tr>`;
    $('#gcKapsam').textContent = say(rows.length, 0) + (rows.length !== kap.length ? ' / ' + say(kap.length, 0) : '') + ' Kayıt';
  }
  gcEl.addEventListener('click', e => {
    if (!GC) return;
    if (e.target === gcEl || e.target.closest('#gcKapat')) { gecmisKapat(); return; }
    const gs = e.target.closest('[data-gs]'); if (gs) { GC.sirket = gs.dataset.gs; gecmisCiz(); return; }
    const gt = e.target.closest('[data-gt]'); if (gt) { GC.tur = gt.dataset.gt; gecmisCiz(); return; }
    const th = e.target.closest('th[data-k]'); if (th) { GC.sira = { k: th.dataset.k, y: GC.sira.k === th.dataset.k ? -GC.sira.y : (/^(tarih|fiili|muh|fiyat|tutar)$/.test(th.dataset.k) ? -1 : 1) }; gecmisCiz(); return; }
    if (e.target.closest('#gcExcel')) gecmisExcel();
  });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && GC) gecmisKapat(); });
  async function gecmisExcel() {
    const dg = $('#gcExcel'); dg.disabled = true;
    try { await TabloExcel.indir('Urun_Gecmisi_' + GC.kod.replace(/[^\w-]+/g, '_'), [{ tablo: '#gcTablo', ad: 'Ürün Geçmişi', baslik: 'Ürün Geçmişi · ' + $('#gcAd').textContent.replace(/\s+/g, ' ').trim(), altBaslik: (GC.sirket || 'Tümü (Konsolide)') + ' · ' + GC_TUR.find(a => a[0] === GC.tur)[1] + ' · ' + $('#gcKapsam').textContent, kpi: '#gcKpi' }]); }
    catch (x) { alert('Excel üretilemedi: ' + x.message); }
    dg.disabled = false;
  }

  // ---------- Özet ----------
  function cizOzet() {
    const l = S.filter(s => stokUyar(s)), pl = P.filter(planUyar), rD = topla(l, s => s.rDeger), fD = topla(l, s => s.fDeger), n = (f) => l.filter(f).length;
    const acil = pl.filter(p => p.oncelik === 'Acil Alım'), kritik = pl.filter(p => p.oncelik === 'Kritik'), yakin = pl.filter(p => p.oncelik === 'Yakın Takip');
    const kpi = [
      ['Muhasebe Stok Değeri (R)', tl(rD), `${l.length} Stok Satırı · ${new Set(l.map(s => s.kod)).size} Ürün`, 'stok'],
      ['Fiili Stok Değeri', tl(fD), `Kullanılabilir ${tl(topla(l, s => s.kDeger))}`, 'stok'],
      ['Fark (X = Fiili − Muhasebe)', (fD - rD > 0 ? '+' : '') + tl(fD - rD), `Miktar Etkisi ${tl(topla(l, s => s.mEtki))} · Maliyet Etkisi ${tl(topla(l, s => s.malEtki))}`, 'sayim'],
      ['Fiili Fazla', n(s => s.mDurum === 'Fiili Fazla') + ' Satır', `+${tl(topla(l.filter(s => s.mDurum === 'Fiili Fazla'), s => s.mEtki))} Miktar Etkisi`, 'sayim'],
      ['Fiili Eksik', n(s => s.mDurum === 'Fiili Eksik') + ' Satır', `${tl(topla(l.filter(s => s.mDurum === 'Fiili Eksik'), s => s.mEtki))} Miktar Etkisi`, 'sayim'],
      ['Negatif Stok', n(s => s.mDurum === 'Negatif Stok') + ' Satır', 'Kayıt Hatası Olasılığı', 'sayim'],
      ['SKT Geçmiş', n(s => s.sktDurum === 'SKT Geçmiş') + ' Satır', tl(topla(l.filter(s => s.sktDurum === 'SKT Geçmiş'), s => s.fDeger)), 'sayim'],
      ['SKT Yaklaşan', n(s => s.sktDurum === 'SKT Yaklaşıyor') + ' Satır', `${AYAR.sktGun} Gün İçinde · ${tl(topla(l.filter(s => s.sktDurum === 'SKT Yaklaşıyor'), s => s.fDeger))}`, 'sayim'],
      ['Yavaş · Atıl Stok', n(s => s.hDurum === 'Yavaş') + ' · ' + n(s => s.hDurum === 'Atıl') + ' Satır', `Atıl ${tl(topla(l.filter(s => s.hDurum === 'Atıl'), s => s.fDeger))}`, 'sayim'],
      ['Bloke / Karantina', tl(topla(l, s => s.bloke * s.fMaliyet)), n(s => s.bloke > 0) + ' Satır', 'sayim'],
      ['Acil Alım · Kritik', acil.length + ' · ' + kritik.length + ' Ürün', `Yakın Takip ${yakin.length} Ürün`, 'tuketim'],
      ['Önerilen Alım Tutarı', tl(topla(pl, p => p.oneriDeger)), `Hedef Stok ${AYAR.hedefGun} Gün`, 'tuketim']
    ];
    $('#srKpi').innerHTML = kpi.map(k => `<div data-git="${k[3]}" title="${esc(SEK[k[3]].ad)} sekmesine git"><div class="l">${k[0]}</div><div class="v">${k[1]}</div><div class="s">${k[2]}</div></div>`).join('');
    const dagilim = (el, anahtar, f) => {
      const m = {}; l.forEach(s => { const a = anahtar(s); (m[a] = m[a] || { a, v: 0, x: 0 }); m[a].v += s.fDeger; m[a].x += s.xDeger; }); let d = Object.values(m).sort((a, b) => b.v - a.v);
      if (d.length > 10) d = d.slice(0, 9).concat({ a: 'Diğer', v: topla(d.slice(9), x => x.v), x: topla(d.slice(9), x => x.x), diger: 1 }); const en = Math.max(1, ...d.map(x => x.v));
      $(el).innerHTML = d.length ? `<div class="sr-cubuk">${d.map(x => `<span class="ad" ${x.diger ? 'style="cursor:default;text-decoration:none"' : `data-f="${f}" data-v="${esc(x.a)}"`} title="${esc(x.a)}">${esc(x.a || '—')}</span><span class="yol" title="${esc(x.a)}: fiili ${tl(x.v)} · X ${tl(x.x)}"><i style="width:${Math.max(0, x.v / en * 100).toFixed(1)}%"></i></span><span class="dg">${tl(x.v)}<small>${yuzde(fD ? x.v / fD * 100 : 0)}</small></span>`).join('')}</div>` : '<div class="text-muted" style="font-size:.7rem">Kayıt yok.</div>';
    };
    dagilim('#srTipDagilim', s => s.tip, 'tip'); dagilim('#srSirketDagilim', s => s.sirket, 'sirket'); dagilim('#srDepoDagilim', s => s.depoTam, 'depo');
    const mini = (el, basliklar, rows, hucreler, bos) => { $(el).innerHTML = `<thead><tr>${basliklar.map((b, i) => `<th${i ? '' : ' style="width:36%"'}>${b}</th>`).join('')}</tr></thead><tbody>${rows.length ? rows.map(r => `<tr>${hucreler(r).map((h, i) => `<td class="${i ? 'p' : ''}">${h}</td>`).join('')}</tr>`).join('') : `<tr><td class="bos" colspan="${basliklar.length}">${bos}</td></tr>`}</tbody>`; };
    mini('#srOzetFark', ['Ürün', 'Şirket', 'X Miktar', 'X Değer (₺)'], l.filter(s => Math.abs(s.xDeger) > 0.5).sort((a, b) => Math.abs(b.xDeger) - Math.abs(a.xDeger)).slice(0, 10), s => [urunBag(s), esc(s.sirket), yon(s.x, mik(s.x, s.birim)), yon(s.xDeger, tl(s.xDeger))], 'Fark yok.');
    mini('#srOzetAlim', ['Ürün', 'Şirket', 'Yeterlilik (Gün)', 'Önerilen Alım', 'Öncelik'], acil.concat(kritik).sort((a, b) => (a.yeterlilik ?? 0) - (b.yeterlilik ?? 0)).slice(0, 10), p => [urunBag(p), esc(p.sirket), p.yeterlilik === null ? '—' : say(p.yeterlilik, 0), mik(p.oneri, p.birim), rozet(p.oncelik)], 'Acil ya da kritik alım yok.');
    mini('#srOzetRisk', ['Ürün', 'Şirket', 'Risk', 'Fiili Değer (₺)'], l.filter(s => s.sktDurum === 'SKT Geçmiş' || s.sktDurum === 'SKT Yaklaşıyor' || s.hDurum === 'Atıl').sort((a, b) => b.fDeger - a.fDeger).slice(0, 10), s => [urunBag(s), esc(s.sirket), rozet(s.sktDurum === 'SKT Geçmiş' || s.sktDurum === 'SKT Yaklaşıyor' ? s.sktDurum : s.hDurum), tl(s.fDeger)], 'Riskli stok yok.');
    $('#srKapsam').textContent = l.length + ' Stok Satırı';
  }
  $('#srOzet').addEventListener('click', e => {
    const gc = e.target.closest('[data-gecmis]'); if (gc) { e.preventDefault(); gecmisAc(gc.dataset.gecmis, gc.dataset.sirket); return; }
    const k = e.target.closest('#srKpi > div'); if (k) { sekmeAc(k.dataset.git); return; }
    const d = e.target.closest('[data-f]'); if (!d) return; F[d.dataset.f] = [d.dataset.v]; suzDugmeYaz(); ciz();
  });

  // ---------- UYUM KONTROLÜ: satır ayrıntısı, Kontrol Ayı ve Tolerans ----------
  function uyumDetay(s) {
    const kutu = (b, l) => `<div><h6>${b}</h6><dl>${l.filter(x => x[1] !== '' && x[1] != null).map(x => `<dt>${x[0]}</dt><dd title="${esc(String(x[1]).replace(/<[^>]+>/g, ''))}">${x[1]}</dd>`).join('')}</dl></div>`;
    const q = v => v == null ? '' : mikE(v, s.birim), f = v => v == null ? '' : yon(v, mikE(v, s.birim));
    const belgeler = TB.filter(t => t.kart === s && t.ay === AY).map(t => t.belge + ' (' + tarih(t.tarih) + ')').join(', ');
    return `<div class="sr-detay-izgara">`
      + kutu('Kimlik ve Ürün Ağacı', [['Stok Kodu', esc(s.kod)], ['Stok Tipi', esc(s.tip)], ['Şirket', esc(s.sirket)], ['Depo', esc(s.depoTam)], ['Parti / Lot', esc(s.lot)], ['Stok Satırı', s.satirlar.length], ['Faaliyet Ana Grubu', esc(s.anaGrup)], ['Faaliyet Grubu', esc(s.grup)], ['Ürün Sınıfı', esc(s.sinif)], ['Ürün Grubu', esc(s.urunGrubu)], ['Ürün Serisi', esc(s.seri)], ['Alım Önceliği', esc(s.oncelik)]])
      + kutu('Stok ve Fark', [['Birim Maliyet', say(s.maliyet, 2) + ' ₺'], ['Muhasebe Miktarı (R)', q(s.muhMiktar)], ['Muhasebe Değeri', tl(s.muhBedel, 2)], ['Fiili Miktar', q(s.fiiliMiktar)], ['Fiili Değer', tl(s.fiiliBedel, 2)], ['Fark Miktarı (X)', f(s.fark)], ['Fark Değeri', yon(s.farkBedel, tl(s.farkBedel, 2))],
        ['Fark %', yonY(s.farkYuzde)], ['Minimum Stok', q(s.minStok)], ['Kritik Stok', q(s.kritikStok)], ['Durum', esc(s.durum)], ['Risk Seviyesi', esc(s.risk)]])
      + kutu(ayAd(AY) + ' · Fiili Stok', [['Önceki Ay Sonu', q(s.oncF)], ['Ay İçi Giriş', q(s.girF)], ['Aylık Fiili Tüketim', q(s.tukF)], ['Beklenen Ay Sonu', q(s.bekF)], ['Ay Sonu Stok', q(s.sonF)], ['Fark', f(s.farkF)]])
      + kutu(ayAd(AY) + ' · Muhasebe Stoku', [['Önceki Ay Sonu', q(s.oncM)], ['Ay İçi Giriş', q(s.girM)], ['Aylık Muhasebe Çıkışı', q(s.tukM)], ['Beklenen Ay Sonu', q(s.bekM)], ['Ay Sonu Stok', q(s.sonM)], ['Fark', f(s.farkM)]])
      + kutu('Reçete Kontrolü', [['Gerçek Reçete', s.recete ? q(s.recete) : '—'], ['Fiili Tüketim', q(s.tukF)], ['Gerçek Reçete − Fiili', s.recete ? f(s.rf) : ''], ['Muhasebe Reçetesi', s.receteR ? q(s.receteR) : '—'], ['Muhasebe Çıkışı', q(s.tukM)], ['Muhasebe Reçetesi − Çıkış', s.receteR ? f(s.rm) : ''], ['Tüketim Belgesi', esc(belgeler)], ['Aylık Uyum', esc(s.aylik)], ['Reçete Uyumu', esc(s.uclu)], ['Sonuç', esc(s.sonuc)]])
      + `</div><div class="uy-not"><div style="--n:${NOKTA[s.aylik]}"><b>Aylık Uyum Detayı</b>${esc(s.aylikDetay)}</div><div style="--n:${NOKTA[s.uclu]}"><b>Reçete Uyumu Detayı</b>${esc(s.ucluDetay)}${s.aksiyon ? ' ' + esc(s.aksiyon) : ''}</div></div>`;
  }
  // parametreler başlıkta: Kontrol Ayı ve Tolerans (uyum ve stok takibi sekmelerini etkiler)
  function parametreYaz() { $('#srAy').innerHTML = AYLAR.map(a => `<option value="${a}"${a === AY ? ' selected' : ''}>${ayAd(a)}</option>`).join(''); $('#srTol').value = sayE(TOL); }
  $('#srAy').addEventListener('change', e => { AY = e.target.value; uyumHesapla(); ciz(); adresYaz(); });
  $('#srTol').addEventListener('change', e => { const v = parseFloat(String(e.target.value).replace(/\s/g, '').replace(/\./g, '').replace(',', '.')); if (isFinite(v) && v >= 0 && v <= 1e7) { TOL = v; uyumHesapla(); } e.target.value = sayE(TOL); ciz(); });

  // ---------- VERİ GİRİŞİ ORTAKLARI: sayı çözümü, kaydet isteği ----------
  // 16 Kas 2026: Sayım ve Sayım Onayı sekmeleri kullanıcı isteğiyle kaldırıldı ("Sayım ve Sayım Onayı kaldır"); sayım girişi, onay / red / geri alma ekranı ve taslakları buradan silindi.
  /** sayı çözümü (veri girişi): "1.234,5" · "1234,5" · "1234.5" · "1.234" (binlik) */
  function sgSayi(s) { s = String(s ?? '').trim().replace(/\s/g, ''); if (s === '') return NaN; if (s.includes(',')) s = s.replace(/\./g, '').replace(',', '.'); else if (/^\d{1,3}(\.\d{3})+$/.test(s)) s = s.replace(/\./g, ''); return /^\d+(\.\d+)?$/.test(s) ? parseFloat(s) : NaN; }
  const sgYazi = n => n == null ? '' : String(n).replace('.', ',');
  const zaman = z => z ? tarih(String(z).slice(0, 10)) + ' ' + String(z).slice(11, 16) : '';
  function yenile() { hazirla(); suzDugmeYaz(); ciz(); }
  async function sgGonder(adres, govde) {
    const r = await fetch(adres, { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' }, body: JSON.stringify(govde) }); if (!r.ok) throw new Error('Sunucu yanıtı ' + r.status);
    const j = await r.json(); if (j.sayimlar) V.sayimlar = j.sayimlar; if (j.urunler) V.urunler = j.urunler; if (j.esikler) V.esikler = j.esikler; return j;
  }
  tablo.tBodies[0].addEventListener('keydown', e => {
    if (e.key !== 'Enter' || !e.target.closest('input.sg')) return; e.preventDefault();
    const sc = `input.sg[data-e="${e.target.dataset.e}"]`, l =[...tablo.tBodies[0].querySelectorAll(sc)], n = l[l.indexOf(e.target) + 1]; if (n) { n.focus(); if (n.select) n.select(); }
  });
  window.addEventListener('beforeunload', e => { if (esTaslakSay()) { e.preventDefault(); e.returnValue = ''; } });

  // ---------- STOK EŞİKLERİ (16 Kas 2026, "Tüketim ve Yeterlilik sekmesine kritik stok vs girelim") ----------
  // Tüketim ve Yeterlilik sekmesinde stok kartı başına Kritik Stok, Minimum Stok, Tedarik Süresi (gün) ve Hedef Stok girilir (Data/StokTanimKayit.cs › Esik).
  // Kutudaki soluk değer otomatik (hesaplanan / örnek) değerdir; değer girilirse onun yerine geçer, kutu boşaltılıp kaydedilirse otomatiğe döner.
  // Girilen eşikler bütün sekmelere yansır: stok durumu (Kritik · Minimum Altı · Yeterli), risk, hedef stok, önerilen alım, alım önceliği.
  // TASLAK_E: kaydedilmemiş girişler (kart anahtarı → { kritik, minimum, tedarikGun, hedef }).
  const ES_ALAN = [['kritik', 'Kritik Stok'], ['minimum', 'Minimum Stok'], ['tedarikGun', 'Tedarik Süresi'], ['hedef', 'Hedef Stok']];
  const esKirli = (r, alan) => !!TASLAK_E[r.anahtar] && TASLAK_E[r.anahtar][alan] !== undefined;
  /** kutunun değeri: taslak → kayıtlı eşik → boş (otomatik) */
  const esDeger = (r, alan) => esKirli(r, alan) ? TASLAK_E[r.anahtar][alan] : r.esik && r.esik[alan] != null ? sgYazi(r.esik[alan]) : '';
  const esKutu = (r, alan) => `<input class="sg es${esKirli(r, alan) ? ' dolu' : r.esik && r.esik[alan] != null ? ' giril' : ''}" data-e="${alan}" data-key="${esc(r.anahtar)}" value="${esc(esDeger(r, alan))}" placeholder="${r.oto ? esOto(r.oto[alan]) : ''}" inputmode="decimal" autocomplete="off" title="${r.esik && r.esik[alan] != null ? 'Girilen değer' + (r.esik.giren ? ' · ' + esc(r.esik.giren) : '') + ' · otomatik değer ' + (r.oto ? sayE(r.oto[alan]) : '—') : 'Boş: otomatik değer kullanılır'}">`;
  const esOto = v => Math.abs(v) >= 100 ? say(v, 0) : sayE(v);   // kutudaki soluk otomatik değer: sığması için 100 ve üstü ondalıksız (tam değer ipucunda)
  const esTaslakSay = () => Object.keys(TASLAK_E).length;
  function esSayacYaz() { const d = $('#esKaydet'); if (!d) return; const n = esTaslakSay(); d.innerHTML = `<i class="fas fa-floppy-disk"></i>Kaydet${n ? ' (' + n + ')' : ''}`; d.disabled = !n; }
  async function esikKaydet() {
    const l = [];
    for (const key of Object.keys(TASLAK_E)) {
      const p = P.find(x => x.anahtar === key); if (!p) continue; const ad = p.kod + ' · ' + p.ad, o = { anahtar: key };
      for (const [alan, etiket] of ES_ALAN) {
        const ham = String(esDeger(p, alan)).trim(); if (ham === '') { o[alan] = null; continue; }
        const n = sgSayi(ham); if (!isFinite(n)) { alert(`${ad}: ${etiket} geçersiz (${ham}). Örnek: 1250 ya da 1.250,5`); return; } o[alan] = alan === 'tedarikGun' ? Math.round(n) : n;
      }
      if (o.kritik != null && o.minimum != null && o.minimum < o.kritik) { alert(`${ad}: Minimum Stok, Kritik Stoktan küçük olamaz.`); return; }
      if (o.minimum != null && o.hedef != null && o.hedef < o.minimum) { alert(`${ad}: Hedef Stok, Minimum Stoktan küçük olamaz.`); return; }
      l.push(o);
    }
    if (!l.length) return;
    try { const j = await sgGonder('/StokRaporu/EsikKaydet', l); if (!j.ok) { alert(j.mesaj); return; } TASLAK_E = {}; yenile(); } catch (x) { alert('Stok eşikleri kaydedilemedi: ' + x.message); }
  }
  tablo.tBodies[0].addEventListener('input', e => {
    const i = e.target.closest('.sg[data-e]'); if (!i) return; const t = TASLAK_E[i.dataset.key] || (TASLAK_E[i.dataset.key] = {}); t[i.dataset.e] = i.value; i.classList.add('dolu'); esSayacYaz();
  });

  // ---------- KAYIT FORMU: LİSTEDE OLMAYAN ÜRÜN ve DEMİRBAŞ (16 Kas 2026) ----------
  // "Listede olmayan ürünü girebilelim": Stok Listesi'nde "Ürün Ekle" → ürün tanımı + fiili miktar. Ürün stok satırı olarak listeye eklenir; girilen miktar doğrudan fiili stoktur,
  //   muhasebe miktarı 0'dır. (16 Kas 2026: düğme Sayım sekmesindeydi; Sayım ve Sayım Onayı sekmeleri kaldırılınca Stok Listesi'ne taşındı, sayım / onay adımı kalktı.)
  // "Demirbaş (üretim makineleri bilgileri) ve küçük demirbaşlar da girilebilsin": Demirbaş grubunda "Yeni Demirbaş" → tür (Makine / Ekipman · Küçük Demirbaş), marka, model, seri no,
  //   üretim yılı, kapasite, alım tarihi / bedeli, adet, durum, zimmet, garanti ve bakım tarihleri. Adet doğrudan fiili miktardır.
  // Kayıtlar Data/StokTanimKayit.cs (App_Data/stok-tanim.json). Anahtar alanları (şirket, depo / yer, kod, parti) kayıt açıldıktan sonra değişmez.
  const BIRIMLER = ['kg', 'lt', 'adet', 'koli', 'kutu', 'rulo', 'takım', 'çift', 'metre'], DURUMLAR = ['Aktif', 'Bakımda', 'Arızalı', 'Atıl', 'Hurda'];
  const DEPO_SABIT = [['01', 'Hammadde Deposu'], ['02', 'Mamul Deposu'], ['03', 'Ambalaj Deposu'], ['04', 'Sarf ve Genel Depo'], ['05', 'Hurda Sahası']], YER_SABIT = [['06', 'Üretim Sahası'], ['07', 'İdari Ofis']];
  kok.insertAdjacentHTML('beforeend', `<div class="gc-perde" id="srForm" hidden><div class="gc-pencere fm" role="dialog" aria-modal="true">
    <div class="gc-ust"><span class="st-ikon"><i class="fas fa-pen-to-square"></i></span><div class="gc-baslik"><div class="ad" id="fmAd"></div><div class="alt" id="fmAlt"></div></div><div class="gc-sag"><button type="button" class="gc-kapat" data-fm="kapat" title="Kapat (Esc)"><i class="fas fa-xmark"></i></button></div></div>
    <form class="fm-govde" id="fmGovde" autocomplete="off"></form>
    <div class="fm-alt"><button type="button" class="sr-dgm fm-sil" data-fm="sil" id="fmSil"><i class="fas fa-trash-can"></i>Kaydı Sil</button><span class="fm-hata" id="fmHata"></span><button type="button" class="sr-dgm" data-fm="kapat">Vazgeç</button><button type="button" class="sr-dgm ana" data-fm="kaydet" id="fmKaydet"><i class="fas fa-floppy-disk"></i>Kaydet</button></div>
  </div></div>`);
  const fmEl = $('#srForm'), fmGovde = $('#fmGovde'); let FM = null;
  const fmSirketler = () => [...new Set(S.map(s => s.sirket).concat(['Uras Holding', 'Uras Power']))].sort((a, b) => a.localeCompare(b, TR));
  /** şirketin depoları: stok listesindeki adlarıyla; eksik kodlar genel listeden; demirbaşta ayrıca Üretim Sahası ve İdari Ofis */
  function fmDepolar(sirket, demirbas) {
    const m = new Map(); S.forEach(s => { if (s.sirket === sirket && !m.has(s.depoKod)) m.set(s.depoKod, s.depo); });
    DEPO_SABIT.concat(demirbas ? YER_SABIT : []).forEach(d => { if (!m.has(d[0])) m.set(d[0], d[1]); });
    return [...m.entries()].sort((a, b) => a[0].localeCompare(b[0])).map(d => ({ v: d[0], ad: d[0] + ' · ' + d[1], depo: d[1] }));
  }
  /** form alanları: [bölüm başlığı, [alan…]]; alan = { k, ad, tur: metin | sayi | tarih | sec | uzun, sec, z (zorunlu), g (ızgara genişliği), kilit (düzenlemede değişmez), ek (input öznitelikleri) } */
  function fmAlanlar(tur, yeni) {
    const sirket = { k: 'sirket', ad: 'Şirket', tur: 'sec', sec: fmSirketler(), z: 1, kilit: 1 };
    if (tur === 'demirbas') return [
      ['Demirbaş', [{ k: 'tip', ad: 'Tür', tur: 'sec', sec: DEMIRBAS_TIP, z: 1 }, sirket, { k: 'depoKod', ad: 'Bulunduğu Yer', tur: 'sec', sec: [], z: 1, kilit: 1 }, { k: 'yer', ad: 'Bölüm / Hat', ek: 'maxlength="60"' },
        { k: 'kod', ad: 'Demirbaş Kodu', z: 1, kilit: 1, ek: 'maxlength="40"' }, { k: 'ad', ad: 'Demirbaş Adı', z: 1, g: 2, ek: 'maxlength="120"' }, { k: 'durum', ad: 'Durum', tur: 'sec', sec: DURUMLAR }]],
      ['Makine Bilgileri', [{ k: 'marka', ad: 'Marka', ek: 'maxlength="60"' }, { k: 'model', ad: 'Model', ek: 'maxlength="60"' }, { k: 'seriNo', ad: 'Seri No', ek: 'maxlength="60"' }, { k: 'uretimYili', ad: 'Üretim Yılı', tur: 'sayi', ek: 'maxlength="4" placeholder="2021"' },
        { k: 'kapasite', ad: 'Kapasite', g: 2, ek: 'maxlength="60" placeholder="Örnek: 2.000 kg / saat"' }, { k: 'zimmet', ad: 'Zimmet / Sorumlu', g: 2, ek: 'maxlength="60"' }]],
      ['Alım ve Değer', [{ k: 'alimTarihi', ad: 'Alım Tarihi', tur: 'tarih' }, { k: 'tedarikci', ad: 'Tedarikçi', ek: 'maxlength="80"' }, { k: 'miktar', ad: 'Adet (Fiili)', tur: 'sayi', z: 1 }, { k: 'birim', ad: 'Birim', tur: 'sec', sec: BIRIMLER, z: 1 },
        { k: 'maliyet', ad: 'Birim Alım Bedeli (₺)', tur: 'sayi' }, { k: 'muhMiktar', ad: 'Muhasebe Adedi (R)', tur: 'sayi', ek: 'placeholder="0" title="Muhasebede kayıtlı adet; muhasebe kaynağı bağlanınca oradan gelir"' }]],
      ['Garanti ve Bakım', [{ k: 'garantiBitis', ad: 'Garanti Bitiş', tur: 'tarih' }, { k: 'sonBakim', ad: 'Son Bakım', tur: 'tarih' }, { k: 'sonrakiBakim', ad: 'Sonraki Bakım', tur: 'tarih' }, { k: 'aciklama', ad: 'Açıklama', g: 4, ek: 'maxlength="300"' }]]
    ];
    const tipler = (V.tipler || []).filter(t => !DEMIRBAS_TIP.includes(t) && (!GRUP[GR] || GRUP[GR].includes(t)));
    const l = [['Ürün', [sirket, { k: 'depoKod', ad: 'Depo', tur: 'sec', sec: [], z: 1, kilit: 1 }, { k: 'tip', ad: 'Stok Tipi', tur: 'sec', sec: tipler.length ? tipler : (V.tipler || []).filter(t => !DEMIRBAS_TIP.includes(t)), z: 1 }, { k: 'birim', ad: 'Birim', tur: 'sec', sec: BIRIMLER, z: 1 },
      { k: 'kod', ad: 'Stok Kodu', z: 1, kilit: 1, ek: 'maxlength="40"' }, { k: 'ad', ad: 'Ürün Adı', z: 1, g: 2, ek: 'maxlength="120"' }, { k: 'lot', ad: 'Parti / Lot', kilit: 1, ek: 'maxlength="40"' },
      { k: 'miktar', ad: 'Fiili Miktar', tur: 'sayi', ek: 'title="Depoda bulunan miktar; fiili stok olarak bütün sekmelere yansır"' }, { k: 'maliyet', ad: 'Birim Maliyet (₺)', tur: 'sayi' }, { k: 'tedarikci', ad: 'Tedarikçi', g: 2, ek: 'maxlength="80"' }]]];
    return l;
  }
  const fmAl = k => { const e = fmGovde.querySelector(`[name="${k}"]`); return e ? e.value.trim() : ''; };
  // 16 Kas 2026 ("Tür alanına ikisine örnek ver"): Tür'ün altında iki türün örnekleri yazılır, seçili tür koyu görünür; Demirbaş Kodu / Adı kutularının soluk örneği de türe göre değişir.
  const TIP_ORNEK = {
    'Makine / Ekipman': { l: 'Reaktör, Mikser, Dolum Makinesi, Kompresör, Forklift, Transpalet, Platform Terazi', ad: 'Dolum Makinesi 5 Lt', kod: 'MK-DLM-01' },
    'Küçük Demirbaş': { l: 'Dizüstü Bilgisayar, Yazıcı, Masa, Sandalye, Dolap, Matkap, El Terminali', ad: 'Dizüstü Bilgisayar', kod: 'KD-BLG-01' }
  };
  function fmTipYaz() {
    const k = fmGovde.querySelector('.fm-ornek'); if (!k) return; const t = fmAl('tip');
    k.innerHTML = DEMIRBAS_TIP.map(x => `<span class="${x === t ? 'secili' : ''}"><b>${esc(x)}</b>${esc(TIP_ORNEK[x].l)}</span>`).join('');
    ['ad', 'kod'].forEach(a => { const e = fmGovde.querySelector(`[name="${a}"]`); if (e && TIP_ORNEK[t]) e.placeholder = 'Örnek: ' + TIP_ORNEK[t][a]; });
  }
  function fmDepoYaz(secili) {
    const e = fmGovde.querySelector('[name="depoKod"]'); if (!e) return; const l = fmDepolar(fmAl('sirket'), FM.tur === 'demirbas');
    e.innerHTML = l.map(d => `<option value="${esc(d.v)}"${d.v === secili ? ' selected' : ''}>${esc(d.ad)}</option>`).join('');
  }
  function formAc(tur, id) {
    if (!V) return; const u = id ? (V.urunler || []).find(x => x.id === id) : null; if (id && !u) return; const dm = tur === 'demirbas';
    FM = { tur, id: id || 0, u };
    $('#fmAd').textContent = dm ? (u ? 'Demirbaş Düzenle' : 'Yeni Demirbaş') : (u ? 'Ürün Kaydını Düzenle' : 'Listede Olmayan Ürün Ekle');
    $('#fmAlt').textContent = u ? [u.kod, u.sirket, 'Kaydı Giren: ' + (u.giren || '—') + ' (' + zaman(u.girisZamani) + ')', u.guncelleyen ? 'Son Düzenleyen: ' + u.guncelleyen + ' (' + zaman(u.guncellemeZamani) + ')' : ''].filter(Boolean).join('  •  ')
      : dm ? 'Üretim Makinesi · Ekipman · Küçük Demirbaş  •  Kaydı Giren: ' + (KULLANICI || '—') : 'Ürün stok listesine eklenir; girilen miktar fiili stok olur  •  Kaydı Giren: ' + (KULLANICI || '—');
    // başlangıç değerleri: düzenlemede kayıttan; yeni kayıtta tek şirket süzülüyse o şirket
    const d0 = u ? Object.assign({}, u) : { sirket: F.sirket.length === 1 ? F.sirket[0] : (S[0] || {}).sirket || '', depoKod: dm ? '06' : (GRUP[GR] && S.find(s => GRUP[GR].includes(s.tip)) || {}).depoKod || '01', birim: dm ? 'adet' : 'kg', miktar: dm ? 1 : 0, durum: 'Aktif', tip: dm ? DEMIRBAS_TIP[0] : '' };
    const deger = a => { const v = d0[a.k]; return v == null ? '' : a.tur === 'sayi' ? (a.k === 'uretimYili' ? String(v) : (+v || a.k === 'miktar' ? sgYazi(v) : '')) : String(v); };
    fmGovde.innerHTML = fmAlanlar(tur, !u).map(b => `<h6>${b[0]}</h6><div class="fm-izgara">${b[1].map(a => {
      const oz = `name="${a.k}"${a.kilit && u ? ' disabled' : ''} ${a.ek || ''}`, v = deger(a);
      const kutu = a.tur === 'sec' ? `<select ${oz}>${(a.sec.includes(v) || !v || a.k === 'depoKod' ? a.sec : [v].concat(a.sec)).map(s => `<option${s === v ? ' selected' : ''}>${esc(s)}</option>`).join('')}</select>`
        : `<input ${oz} type="${a.tur === 'tarih' ? 'date' : 'text'}"${a.tur === 'sayi' ? ' inputmode="decimal"' : ''} value="${esc(v)}">`;
      return `<label class="g${a.g || 1}"><span>${a.ad}${a.z ? ' <i>*</i>' : ''}</span>${kutu}</label>`;
    }).join('')}</div>`).join('');
    if (dm) { fmGovde.querySelector('[name="yer"]').closest('label').insertAdjacentHTML('afterend', '<div class="fm-ornek"></div>'); fmTipYaz(); }
    fmDepoYaz(d0.depoKod); $('#fmHata').textContent = ''; $('#fmSil').hidden = !u; fmEl.hidden = false;
    const ilk = fmGovde.querySelector('input:not([disabled]), select:not([disabled])'); if (ilk) ilk.focus();
  }
  function formKapat() { fmEl.hidden = true; FM = null; }
  async function formKaydet() {
    const hata = m => { $('#fmHata').textContent = m; }, dm = FM.tur === 'demirbas', u = FM.u, yeni = !u, o = { id: FM.id, kaynak: dm ? 'Demirbaş' : 'Sayım' };
    for (const b of fmAlanlar(FM.tur, yeni)) for (const a of b[1]) {
      const ham = fmAl(a.k); if (a.z && ham === '' && !(a.kilit && u)) { hata(a.ad + ' girilmeli.'); return; }
      if (a.tur === 'sayi') { if (ham === '') { o[a.k] = a.k === 'uretimYili' ? null : 0; continue; } const n = sgSayi(ham); if (!isFinite(n)) { hata(a.ad + ' geçersiz: ' + ham); return; } o[a.k] = a.k === 'uretimYili' ? Math.round(n) : n; }
      else o[a.k] = ham;
    }
    if (u) { o.sirket = u.sirket; o.depoKod = u.depoKod; o.depo = u.depo; o.kod = u.kod; o.lot = u.lot || ''; if (!dm) o.muhMiktar = u.muhMiktar || 0; }
    else {
      o.depo = (fmDepolar(o.sirket, dm).find(d => d.v === o.depoKod) || {}).depo || ''; o.lot = o.lot || '';
      const key = o.sirket + '|' + o.depoKod + '|' + o.kod + '|' + o.lot;
      if (S.some(s => s.sKey === key)) { hata((dm ? 'Bu demirbaş kodu' : 'Bu stok kodu' + (o.lot ? ' ve parti' : '')) + ' seçilen şirket ve ' + (dm ? 'yerde' : 'depoda') + ' listede zaten var.'); return; }
    }
    const dg = $('#fmKaydet'); dg.disabled = true;
    try { const j = await sgGonder('/StokRaporu/UrunKaydet', o); if (!j.ok) { hata(j.mesaj); if (j.urunler) yenile(); } else { formKapat(); yenile(); } } catch (x) { hata('Kaydedilemedi: ' + x.message); }
    dg.disabled = false;
  }
  async function urunSil(id) {
    const u = (V.urunler || []).find(x => x.id === id); if (!u) return;
    if (!confirm(`${u.kod} · ${u.ad}\n\nKayıt silinsin mi?`)) return;
    try { const j = await sgGonder('/StokRaporu/UrunSil', { id }); if (!j.ok) alert(j.mesaj); if (FM) formKapat(); yenile(); } catch (x) { alert('Silinemedi: ' + x.message); }
  }
  /** kullanıcının eklediği kayıt için satır düğmeleri: Demirbaş Listesi'nde (sil = 1) düzenle + sil simgeleri; Stok Listesi'nde ürün adının altında "Listeye Eklendi" bağlantısı (düzenleme penceresini açar; silme pencerenin içinde) */
  const urunDugme = (r, sil) => !r.ek ? '' : sil ? `<button type="button" class="sg-b" data-urun="duzenle" data-id="${r.urunId}" title="Kaydı düzenle"><i class="fas fa-pen"></i></button><button type="button" class="sg-b" data-urun="sil" data-id="${r.urunId}" title="Kaydı sil"><i class="fas fa-trash-can"></i></button>`
    : `<button type="button" class="st-duz" data-urun="duzenle" data-id="${r.urunId}" title="Kaydı düzenle ya da sil"><i class="fas fa-pen"></i>Listeye Eklendi</button>`;
  fmEl.addEventListener('click', e => {
    if (!FM) return; const d = e.target.closest('[data-fm]'); if (!d) return;
    if (d.dataset.fm === 'kapat') formKapat(); else if (d.dataset.fm === 'kaydet') formKaydet(); else if (d.dataset.fm === 'sil') urunSil(FM.id);
  });
  fmGovde.addEventListener('change', e => { if (e.target.name === 'sirket') fmDepoYaz(fmAl('depoKod')); if (e.target.name === 'tip') fmTipYaz(); });
  fmGovde.addEventListener('submit', e => { e.preventDefault(); formKaydet(); });
  fmGovde.addEventListener('keydown', e => { if (e.key === 'Enter' && e.target.tagName === 'INPUT') { e.preventDefault(); formKaydet(); } });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && FM) formKapat(); });

  // ---------- sekme şeridi ----------
  function islemYaz() {
    const el = $('#srIslem'); let h = '';
    const ayar = (k, ad, ac) => `<label title="${ac}">${ad} <input class="ayar" data-ayar="${k}" value="${AYAR[k]}"></label>`;
    if (sekme === 'tuketim' || sekme === 'ozet') h += ayar('hedefGun', 'Hedef Stok', 'Hedef stok süresi, gün olarak. Satınalma hesabında kullanılan hedef süre: hedef stok = günlük tüketim × hedef gün + güvenlik stoku');
    if (sekme === 'tuketim') h += `<span title="Tüketim dönemi: ${tarih(AYAR.donemBas)} – ${tarih(AYAR.donemBit)} (${AYAR.donemGun} gün)">Dönem <b>${tarih(AYAR.donemBas).slice(0, 5)} – ${tarih(AYAR.donemBit)}</b></span>`;
    if (sekme === 'tuketim') h += `<button type="button" class="sr-dgm ana" data-is="esKaydet" id="esKaydet" title="Girilen stok eşiklerini (kritik stok, minimum stok, tedarik süresi, hedef stok) kaydet"><i class="fas fa-floppy-disk"></i>Kaydet</button>`;
    if (sekme === 'demirbas') h += `<button type="button" class="sr-dgm ana" data-is="yeniDemirbas" title="Üretim makinesi, ekipman ya da küçük demirbaş kaydı ekle"><i class="fas fa-plus"></i>Yeni Demirbaş</button>`;
    // 16 Kas 2026: Sayım sekmesi kaldırıldı; "Ürün Ekle" (listede olmayan ürün) Stok Listesi'ne taşındı. Demirbaş grubunda kayıt Demirbaş Listesi'nden açılır.
    if (sekme === 'stok' && GR !== 'm') h += `<button type="button" class="sr-dgm ana" data-is="yeniUrun" title="Listede olmayan ürünü stok listesine ekle"><i class="fas fa-plus"></i>Ürün Ekle</button>`;
    if (sekme === 'sayim' || sekme === 'ozet') h += ayar('sktGun', 'SKT', 'SKT uyarı süresi, gün olarak. Son kullanma tarihine kalan gün bu sınırın altındaysa "SKT Yaklaşıyor"') + ayar('yavasGun', 'Yavaş', 'Gün olarak. Son hareketten bu yana geçen gün: yavaş stok eşiği') + ayar('atilGun', 'Atıl', 'Gün olarak. Son hareketten bu yana geçen gün: atıl stok eşiği');
    if (sekme === 'sayim') h += `<button type="button" class="sr-dgm${H.farkli ? ' acik' : ''}" data-h="farkli" title="Yalnız fiili ile muhasebe miktarı tutmayan satırlar">Farklı</button><button type="button" class="sr-dgm${H.riskli ? ' acik' : ''}" data-h="riskli" title="Yalnız negatif stok, SKT, yavaş / atıl ya da blokesi olan satırlar">Riskli</button>`;
    if (sekme === 'hiyerarsi') h += `<button type="button" class="sr-dgm${H.azalan ? ' acik' : ''}" data-h="azalan" title="Yalnız kontrol ayında fiili stoku azalan ürünler">Azalanlar</button><button type="button" class="sr-dgm${H.uyF ? ' acik' : ''}" data-h="uyF" title="Yalnız ay sonu sayımı beklenenden (önceki ay sonu + giriş − tüketim) farklı ürünler">Sayımla Uyumsuz</button>`;
    if (sekme === 'muhtakip') h += `<button type="button" class="sr-dgm${H.azalanM ? ' acik' : ''}" data-h="azalanM" title="Yalnız kontrol ayında muhasebe stoku azalan ürünler">Azalanlar</button><button type="button" class="sr-dgm${H.uyM ? ' acik' : ''}" data-h="uyM" title="Yalnız ay sonu muhasebe stoku beklenenden (önceki ay sonu + giriş − çıkış) farklı ürünler">Stokla Uyumsuz</button><button type="button" class="sr-dgm${H.uyS ? ' acik' : ''}" data-h="uyS" title="Yalnız muhasebe çıkışı satışa göre beklenenden farklı ürünler">Satışla Uyumsuz</button>`;
    if (sekme === 'hareket') h += `<button type="button" class="sr-dgm${HG === 'a' ? ' acik' : ''}" data-gor="a" title="Aylık zincirin fiili ve muhasebe tarafı: önceki ay sonu, giriş, tüketim, beklenen, ay sonu, fark">Aylık Hareket</button><button type="button" class="sr-dgm${HG === 's' ? ' acik' : ''}" data-gor="s" title="Güncel muhasebe ve fiili stok, fark, stok durumu, aylık uyum ve reçete kontrolü">Stok ve Uyum</button>`
      + `<button type="button" class="sr-dgm${H.incele ? ' acik' : ''}" data-h="incele" title="Yalnız aylık stok değişimi ya da reçete kontrolü uyumsuz kartlar">Yalnız İncelenecekler</button>`;
    if (sekme === 'belge') h += `<button type="button" class="sr-dgm${H.ay ? ' acik' : ''}" data-h="ay" title="Yalnız kontrol ayının tüketim belgeleri">Yalnız ${ayAd(AY)}</button>`;
    el.innerHTML = h; esSayacYaz();
  }
  $('#srIslem').addEventListener('click', e => {
    const gor = e.target.closest('[data-gor]'); if (gor) { HG = gor.dataset.gor; ciz(); adresYaz(); return; }
    const is = e.target.closest('[data-is]');
    if (is && is.dataset.is === 'esKaydet') { esikKaydet(); return; }
    if (is && (is.dataset.is === 'yeniUrun' || is.dataset.is === 'yeniDemirbas')) { formAc(GR === 'm' ? 'demirbas' : 'urun'); return; }
    if (is) return;
    const d = e.target.closest('[data-h]'); if (!d) return; H[d.dataset.h] = !H[d.dataset.h]; ciz();
  });
  $('#srIslem').addEventListener('change', e => {
    const k = e.target.dataset.ayar; if (!k) return; const v = parseInt(e.target.value, 10); if (isFinite(v) && v > 0 && v <= 3650) { AYAR[k] = v; if (k === 'yavasGun' && AYAR.atilGun < v) AYAR.atilGun = v; hesapla(); } ciz();
  });
  const SEKME_M_YOK = ['tuketim', 'hareket', 'belge', 'hiyerarsi', 'muhtakip'], sekmeVar = p => p === 'demirbas' ? GR === 'm' : GR !== 'm' || !SEKME_M_YOK.includes(p);
  function ciz() {
    if (!V) return;   // açık seçim listesi kapatılmaz: çoklu seçimde liste açık kalır (dışarı tıklanınca / Esc ile kapanır)
    // Demirbaş grubunda tüketim / reçete / aylık hareket sekmeleri yoktur; Demirbaş Listesi yalnız o grupta görünür
    if (!sekmeVar(sekme)) sekme = GR === 'm' ? 'demirbas' : 'stok';
    kok.querySelectorAll('#srSekmeler button[data-p]').forEach(b => { b.classList.toggle('aktif', b.dataset.p === sekme); b.style.display = sekmeVar(b.dataset.p) ? '' : 'none'; });
    kok.querySelectorAll('#srSekmeler .cizgi[data-a]').forEach(c => { c.style.display = sekmeVar(c.dataset.a) ? '' : 'none'; });
    const ozet = sekme === 'ozet'; $('#srTabloKart').hidden = ozet; $('#srOzet').classList.toggle('aktif', ozet); $('#srSutunlar').style.display = SEK[sekme].kol ? '' : 'none';
    islemYaz(); grupYaz();
    if (ozet) { cizOzet(); return; }
    cizBaslik(); cizGovde(); yukseklik();
  }
  function adresYaz() { const q = []; if (GR !== 'h') q.push('grup=' + GR); if (sekme !== 'stok') q.push('sekme=' + sekme); if (AY && AY !== AYAR.kontrolAyi) q.push('ay=' + AY); if (sekme === 'hareket' && HG === 's') q.push('gorunum=stok'); history.replaceState(null, '', location.pathname + (q.length ? '?' + q.join('&') : '')); }
  function sekmeAc(p) { if (!SEK[p]) p = 'stok'; sekme = p; kap.scrollTop = 0; ciz(); adresYaz(); }
  $('#srSekmeler').addEventListener('click', e => { const b = e.target.closest('button[data-p]'); if (b) sekmeAc(b.dataset.p); });

  // ---------- Sütunlar, Excel, Getir ----------
  $('#srSutunlar').addEventListener('click', e => {
    const tn = SEK[sekme]; if (!tn.kol) return; const gor = gorunenKol(sekme).map(c => c.k);
    popAc(e.currentTarget, tn.kol.map(c => ({ v: c.k, ad: tamAd(c) })), gor, l => { const a = {}; tn.kol.forEach(c => a[c.k] = l.includes(c.k)); try { localStorage.setItem(kolAnahtar(sekme), JSON.stringify(a)); } catch (x) { } cizBaslik(); cizGovde(); }, { tumuDolu: 1, tumuAd: 'Tümünü Göster', bosAd: 'Sütun Seçin' });
  });
  $('#srExcel').addEventListener('click', async () => {
    suzDugmeYaz(); const dg = $('#srExcel'), ad = SEK[sekme].ad, alt = $('#srDonemYazi').textContent + ' · ' + $('#srKapsam').textContent; dg.disabled = true;
    try {
      if (sekme === 'ozet') await TabloExcel.indir('Stok_Raporu_Ozet', [{ tablo: '#srOzetFark', ad: 'En Büyük Farklar', baslik: 'Konsolide Stok Raporu · Özet', altBaslik: alt, kpi: '#srKpi' }, { tablo: '#srOzetAlim', ad: 'Alım Önceliği', baslik: 'Alım Önceliği · Acil ve Kritik', altBaslik: alt }, { tablo: '#srOzetRisk', ad: 'Raf Ömrü ve Atıl', baslik: 'Raf Ömrü ve Atıl Stok Riski', altBaslik: alt }]);
      else {
        const Dr = durum(sekme), eski = Dr.limit, acik = Dr.acik; Dr.limit = Infinity; Dr.acik = new Set(); cizGovde();
        tablo.querySelectorAll('input.es').forEach(i => { if (!i.value) i.value = i.placeholder; });   // Excel'de boş eşik kutusu yürürlükteki otomatik değerle yazılır (tablo aktarımdan sonra yeniden çizilir)
        try { await TabloExcel.indir('Stok_Raporu_' + GRUP_AD[GR].replace(/\s+/g, '_') + '_' + ad.replace(/[^\wığüşöçİĞÜŞÖÇ]+/g, '_'), [{ tablo: '#srTablo', ad, baslik: 'Konsolide Stok Raporu · ' + GRUP_AD[GR] + ' · ' + ad, altBaslik: alt }]); }
        finally { Dr.limit = eski; Dr.acik = acik; cizGovde(); }
      }
    } catch (x) { alert('Excel üretilemedi: ' + x.message); }
    dg.disabled = false;
  });
  async function getir() {
    const dg = $('#srGetir'); dg.disabled = true;
    try {
      const r = await fetch('/StokRaporu/Veri', { headers: { 'X-Requested-With': 'XMLHttpRequest' } }); if (!r.ok) throw new Error('Sunucu yanıtı ' + r.status);
      const j = await r.json(); V = j.paket; V.sayimlar = j.sayimlar || []; V.urunler = j.urunler || []; V.esikler = j.esikler || []; KULLANICI = j.kullanici || ''; hazirla(); parametreYaz(); $('#srTarih').textContent = tarih(V.bugun); $('#srOrnek').hidden = !V.ornek;
      suzDugmeYaz(); ciz();
    } catch (x) { tablo.tBodies[0].innerHTML = `<tr><td class="bos">Veri alınamadı: ${esc(x.message)}</td></tr>`; }
    dg.disabled = false;
  }
  $('#srGetir').addEventListener('click', getir);
  if (sekme === 'uyum') { sekme = 'hareket'; HG = 's'; }   // eski Stok ve Uyum bağlantısı → birleşik sekmenin aynı adlı görünümü
  if (!SEK[sekme]) sekme = 'stok';
  getir();
})();
