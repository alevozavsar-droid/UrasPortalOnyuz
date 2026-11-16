// ============================================================
// KONSOLİDE SATIŞ RAPORU (/SatisRaporu, 13 Kas 2026) — Views/SatisRaporu/Index.cshtml · veri /SatisRaporu/Veri (Data/SatisRaporOrnek.cs)
// Yedi satış raporunun süzgeç, sütun ve bilgileri tek ekranda. Sunucu ham paketi verir (belgeler + kalemleri, tahsilatlar, müşteri bakiyeleri);
// süzme, sıralama, özetleme ve grafikler burada hesaplanır — süzgeç değişince sunucuya gidilmez.
//   Ana sekme pazar (Yurtiçi · Yurtdışı · Tümü); alt sekmeler: Faturalar · Kalemler · Siparişler · Tahsilatlar | Müşteri · Ürün · Temsilci · Şirket · Kırılım | Özet
//   (13 Kas 2026: Prim sekmesi kullanıcı isteğiyle bu rapordan kaldırıldı; prim Konsolide Prim Raporu'nda kalır.)
//   Kurallar: genel süzgeçler çoklu seçim; özetler KALEM satırlarından toplanır (ürün süzgecinde yalnız o ürünün tutarı); para birimleri karıştırılmaz
//   (PB sütunlarının dip toplamı para birimi bazında, TL sütunları fatura günü kuruyla); tablo ekrana sığar (sütun ağırlıkları yüzdeye çevrilir).
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('sr'); if (!kok) return;
  const $ = s => kok.querySelector(s);
  const TR = 'tr-TR', SEM = { TL: '₺', USD: '$', EUR: '€', GBP: '£' };
  const AY = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];
  const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const say = (v, h = 2) => (+v || 0).toLocaleString(TR, { minimumFractionDigits: h, maximumFractionDigits: h });
  const para = (v, pb = 'TL', h = 2) => say(v, h) + ' ' + (SEM[pb] || pb);
  const yuzde = (v, h = 1) => '% ' + say(v, h);
  const tarih = s => s ? s.slice(8, 10) + '.' + s.slice(5, 7) + '.' + s.slice(0, 4) : '';
  const ayAd = s => s ? AY[+s.slice(5, 7) - 1] + ' ' + s.slice(0, 4) : '';
  const kucuk = s => String(s ?? '').toLocaleLowerCase(TR);
  const gun = d => d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');   // yerel tarih (UTC kayması olmasın)
  const tarihYap = s => new Date(+s.slice(0, 4), +s.slice(5, 7) - 1, +s.slice(8, 10));
  const gunFark = (a, b) => Math.round((tarihYap(b) - tarihYap(a)) / 864e5);
  const gunEkle = (s, n) => { const d = tarihYap(s); d.setDate(d.getDate() + n); return gun(d); };
  const topla = (l, f) => l.reduce((t, x) => t + (+f(x) || 0), 0);
  const sirala = (a, b) => typeof a === 'number' && typeof b === 'number' ? a - b : String(a ?? '').localeCompare(String(b ?? ''), TR, { numeric: true });
  const NOKTA = { 'Açık': '#b45309', 'Kısmi Kapalı': '#2563eb', 'Kapalı': '#15803d', 'Açık Sipariş': '#b45309', 'Teslimat': '#2563eb', 'Fatura': '#15803d', 'İade Faturası': '#b91c1c', 'Sipariş': '#64748b', 'Hazırlanıyor': '#b45309', 'Yüklemeye Hazır': '#0e7490', 'Yüklendi': '#2563eb', 'Faturalandı': '#15803d', 'Vade Bekliyor': '#b45309', 'Tahsil Edildi': '#15803d', 'İşlendi': '#15803d', 'Kapama Bekliyor': '#b45309' };
  const rozet = d => d ? `<span class="rz" style="--n:${NOKTA[d] || '#94a3b8'}">${esc(d)}</span>` : '';
  const cift = (ust, alt) => `<b>${ust}</b>${alt ? `<small>${alt}</small>` : ''}`;

  // ---------- durum ----------
  let V = null, B = [], K = [], T = [], C = {}, sekme = kok.dataset.sekme || 'belge';
  // ana sekme: pazar (Yurtiçi · Yurtdışı · Tümü). Adres ?pazar=yurtdisi | tumu; varsayılan Yurtiçi (rapor Yurtiçi Satış menüsünde)
  const PZ_KOD = { 'Yurtiçi': 'yurtici', 'Yurtdışı': 'yurtdisi', '': 'tumu' }, pzParam = new URLSearchParams(location.search).get('pazar');
  let PZ = pzParam === 'yurtdisi' ? 'Yurtdışı' : pzParam === 'tumu' ? '' : 'Yurtiçi';
  const pzHarf = () => PZ === 'Yurtiçi' ? 'i' : PZ === 'Yurtdışı' ? 'd' : 't';
  const BOS_F = () => ({ sirket: [], kartTipi: [], ulke: [], sehir: [], grup: [], iliski: [], rol: [], cariDurum: [], temsilci: [], musteri: [], anaGrup: [], fGrup: [], urunSinifi: [], urunGrubu: [], urunSerisi: [], kalem: [], tur: ['Fatura', 'İade Faturası'], pb: [], bas: '', bit: '', ara: '' });
  let F = BOS_F();
  const H = { marj20: false, ciro10: false };           // özet sekmelerinde hızlı süzgeç (Satış Analiz Dashboard'daki "Kâr < %20", "Ciro > 10.000 ₺")
  const DURUM = {};                                      // sekme → { sira: {k, y}, suz: {k: metin | dizi}, limit, acik: Set }
  const durum = p => DURUM[p] || (DURUM[p] = { sira: null, suz: {}, limit: 250, acik: new Set() });
  const SATIS_TUR = ['Fatura', 'İade Faturası'];

  // ---------- veri ----------
  function hazirla() {
    const bugun = V.bugun; C = {}; V.cariler.forEach(c => C[c.kod] = c);
    B = V.belgeler.map(b => {
      const tl = b.tutar * b.kur, netTl = b.net * b.kur, kalanTl = b.kalan * b.kur, kg = topla(b.kalemler.filter(k => k.birim === 'kg'), k => k.miktar), maliyet = topla(b.kalemler, k => k.maliyetTl);
      const acik = b.tur === 'Fatura' && b.kalan > 0.005, gecGun = acik && b.vade < bugun ? gunFark(b.vade, bugun) : 0;
      const ck = C[b.cariKod] || {};
      const o = Object.assign({}, b, { kartTipi: ck.kartTipi || 'Müşteri', rol: ck.rol || '', cariDurum: ck.durum || '', _k: [],
        tl, netTl, kdvTl: tl - netTl, kalanTl, odenen: b.tutar - b.kalan, odenenTl: tl - kalanTl, netUsd: b.kurUsd ? netTl / b.kurUsd : 0, netEur: b.kurEur ? netTl / b.kurEur : 0, kg, maliyet, kar: netTl - maliyet, marj: netTl ? (netTl - maliyet) / netTl * 100 : 0,
        gecGun, vgTl: gecGun > 0 ? kalanTl : 0, ay: b.tarih.slice(0, 7), vadeAy: (b.vade || '').slice(0, 7), vadeGun: gunFark(b.tarih, b.vade || b.tarih), bakiyeTl: C[b.cariKod] ? C[b.cariKod].bakiyeTl : 0,
        kalemKodlari: b.kalemler.map(k => k.kod)
      });
      o._ust = kucuk([b.no, b.cari, b.cariKod, b.temsilci, b.sirket, b.ulke, b.grup, b.sip ? b.sip.no + ' ' + b.sip.talepNo : ''].join(' '));
      o._ara = o._ust + ' ' + kucuk(b.kalemler.map(k => k.kod + ' ' + k.ad).join(' '));
      return o;
    });
    K = []; B.forEach(b => b.kalemler.forEach((k, i) => {
      const netTl = k.tutar * b.kur;
      const ks = Object.assign({}, k, { b, sira: i + 1, urunGrup: k.anaGrup + ' / ' + k.grup, kg: k.birim === 'kg' ? k.miktar : 0, ton: k.birim === 'kg' ? k.miktar / 1000 : 0, netTl, usd: b.kurUsd ? netTl / b.kurUsd : 0, eur: b.kurEur ? netTl / b.kurEur : 0, kar: netTl - k.maliyetTl, marj: netTl ? (netTl - k.maliyetTl) / netTl * 100 : 0, _ara: kucuk(k.kod + ' ' + k.ad) }); K.push(ks); b._k.push(ks);
    }));
    T = V.tahsilatlar.map(t => Object.assign({}, t, { kartTipi: (C[t.cariKod] || {}).kartTipi || 'Müşteri', rol: (C[t.cariKod] || {}).rol || '', cariDurum: (C[t.cariKod] || {}).durum || '', tl: t.tutar * t.kur, _ara: kucuk([t.no, t.cari, t.cariKod, t.temsilci, t.sirket, t.banka, t.tur, t.cekNo].join(' ')) }));
  }

  // ---------- genel süzgeç ----------
  const icinde = (l, v) => !l.length || l.includes(v);
  // SÜZGEÇ ALANLARI — cari kart sınıflandırması (Cari Kart Tipi, Cari Bölge = ana sekme, Ülke, Cari Sınıfı, İlişki Durumu, Cari Rolü, Durum, Satış Temsilcisi)
  // ve ürün ağacı (Faaliyet Ana Grubu, Faaliyet Grubu, Ürün Sınıfı, Ürün Grubu, Ürün Serisi, Ürün Adı). Cari alanları belge ve tahsilatta da bulunur; ürün alanları kalem satırındadır.
  const CARI_AL = { sirket: x => x.sirket, kartTipi: x => x.kartTipi, ulke: x => x.ulke, sehir: x => x.sehir, grup: x => x.grup, iliski: x => x.iliski, rol: x => x.rol, cariDurum: x => x.cariDurum, temsilci: x => x.temsilci, musteri: x => x.cariKod, pb: x => x.pb };
  const URUN_AL = { anaGrup: k => k.anaGrup, fGrup: k => k.grup, urunSinifi: k => k.sinif, urunGrubu: k => k.urunGrubu, urunSerisi: k => k.seri, kalem: k => k.kod };
  const CARI_K = Object.keys(CARI_AL), URUN_K = Object.keys(URUN_AL);
  const genelUyar = (x, haric) => (!PZ || x.pazar === PZ) && CARI_K.every(a => a === haric || icinde(F[a], CARI_AL[a](x)));
  const urunSatirUyar = (k, haric) => URUN_K.every(a => a === haric || icinde(F[a], URUN_AL[a](k)));
  const urunSuzVar = haric => URUN_K.some(a => a !== haric && F[a].length);
  const tarihUyar = x => (!F.bas || x.tarih >= F.bas) && (!F.bit || x.tarih <= F.bit);
  const urunUyar = (b, haric) => !urunSuzVar(haric) || b._k.some(k => urunSatirUyar(k, haric));
  const belgeUyar = (b, o = {}) => genelUyar(b) && (o.turYok || icinde(F.tur, b.tur)) && (o.tarihYok || tarihUyar(b)) && urunUyar(b) && (!F.ara || b._ara.includes(F.ara));
  const kalemUyar = (k, o = {}) => genelUyar(k.b) && (o.satis ? SATIS_TUR.includes(k.b.tur) : icinde(F.tur, k.b.tur)) && (o.tarihYok || tarihUyar(k.b)) && urunSatirUyar(k) && (!F.ara || k.b._ust.includes(F.ara) || k._ara.includes(F.ara));
  const tahsilatUyar = t => genelUyar(t) && tarihUyar(t) && (!F.ara || t._ara.includes(F.ara));

  // ---------- özetleme (kalem satırlarından) ----------
  function ozetle(anahtar, bilgi, o = {}) {
    const m = new Map(), kalemler = K.filter(k => kalemUyar(k, o));
    kalemler.forEach(k => {
      const a = anahtar(k); let x = m.get(a);
      if (!x) m.set(a, x = Object.assign({ a, belgeler: new Set(), musteriler: new Set(), kg: 0, netTl: 0, netUsd: 0, maliyet: 0, yurtici: 0, yurtdisi: 0, grupIci: 0, vadeAg: 0, son: '' }, bilgi ? bilgi(k) : {}));
      x.belgeler.add(k.b); x.musteriler.add(k.b.cariKod); x.kg += k.kg; x.netTl += k.netTl; x.netUsd += k.usd; x.maliyet += k.maliyetTl; x.vadeAg += k.b.vadeGun * k.netTl;
      if (k.b.pazar === 'Yurtiçi') x.yurtici += k.netTl; else x.yurtdisi += k.netTl; if (k.b.iliski === 'Grup İçi') x.grupIci += k.netTl; if (k.b.tarih > x.son) x.son = k.b.tarih;
    });
    const toplam = topla([...m.values()], x => x.netTl);
    return [...m.values()].map(x => {
      const bl = [...x.belgeler];
      return Object.assign(x, { belge: bl.length, musteri: x.musteriler.size, ton: x.kg / 1000, kar: x.netTl - x.maliyet, marj: x.netTl ? (x.netTl - x.maliyet) / x.netTl * 100 : 0, pay: toplam ? x.netTl / toplam * 100 : 0, ortVade: x.netTl ? x.vadeAg / x.netTl : 0, ortFiyat: x.kg ? x.netUsd / x.kg : 0, ortFiyatTl: x.kg ? x.netTl / x.kg : 0,
        kdvTl: topla(bl, b => b.kdvTl), brutTl: topla(bl, b => b.tl), kalanTl: topla(bl, b => b.kalanTl), vgTl: topla(bl, b => b.vgTl) });
    }).filter(x => (!H.marj20 || x.marj < 20) && (!H.ciro10 || x.netTl > 10000));
  }
  const tahsilatTopla = alan => { const m = {}; T.filter(tahsilatUyar).forEach(t => m[t[alan]] = (m[t[alan]] || 0) + t.tl); return m; };

  // ---------- sütun tanımları ----------
  // t: m metin · l liste (çoklu seçim süzgeci) · d tarih · p tutar · s sayı · y yüzde ; top: 1 dip toplam, 'pb' para birimi bazında dip toplam ; tf: özel dip hesabı
  const M = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'm', v }, o);
  const L = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'l', v }, o);
  const D = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'd', v, h: r => tarih(v(r)) }, o);
  const TL = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'p', v, h: r => para(v(r)), top: 1 }, o);
  const TL0 = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'p', v, h: r => para(v(r), 'TL', 0), top: 1, hane: 0 }, o);
  const S = (k, ad, w, v, hane = 0, o) => Object.assign({ k, ad, w, t: 's', v, h: r => say(v(r), hane), top: 1, hane }, o);
  const Y = (k, ad, w, v, tf) => ({ k, ad, w, t: 'y', v, h: r => yuzde(v(r)), tf });
  const PB = (k, ad, w, v, o) => Object.assign({ k, ad, w, t: 'p', v, h: r => para(v(r), r.pb || (r.b && r.b.pb)), top: 'pb' }, o);
  const marjDip = rows => { const n = topla(rows, r => r.netTl); return yuzde(n ? topla(rows, r => r.kar) / n * 100 : 0); };
  // özet sekmelerinde adet dip toplamı: satırların toplamı değil, tekil belge / müşteri sayısı (aynı belge birden çok satırda sayılmasın)
  const tekilBelge = rows => say(new Set(rows.flatMap(r => [...r.belgeler])).size, 0), tekilMusteri = rows => say(new Set(rows.flatMap(r => [...r.musteriler])).size, 0);
  const musteriHucre = r => cift(esc(r.cari), esc(r.cariKod));
  const B_ = f => r => f(r.b);

  // gz: sütunun varsayılan GİZLİ olduğu pazar sekmeleri — i Yurtiçi · d Yurtdışı · t Tümü (gizli: 1 = hepsinde gizli; Sütunlar'dan açılır).
  // Yurtiçinde döviz / kur / ülke / USD karşılığı, yurtdışında KDV sütunları görünmez: her pazar yalnız kendi bilgisini taşır, satırlar tek satırdır.
  const noHucre = b => b.tur === 'Fatura' ? `<b>${esc(b.no)}</b>` : cift(esc(b.no), esc(b.tur));
  const adHucre = ad => `<b>${esc(ad || '—')}</b>`;
  // Cari Ekstre'ye git: satırın müşterisiyle (ve seçili dönemle) Cari Ekstre Raporu yeni sekmede açılır; rapordaki süzgeçler kaybolmaz
  const ekLink = x => `<a class="sr-ek" href="/Rapor20?bpCode=${encodeURIComponent(x.cariKod)}${F.bas ? '&startDate=' + F.bas : ''}${F.bit ? '&endDate=' + F.bit : ''}" target="_blank" rel="noopener" title="Cari Ekstre'ye Git: ${esc(x.cari)} (${esc(x.cariKod)})"><i class="fas fa-file-invoice"></i></a>`;
  const EK = al => ({ k: 'ekstre', ad: 'Cari Ekstre', w: 34, t: 'ik', v: r => al(r).cariKod, h: r => ekLink(al(r)) });
  const dovizH = (tutar, pb) => pb === 'TL' ? '—' : para(tutar, pb);
  const kurH = (kur, pb) => pb === 'TL' ? '—' : say(kur, 4);
  const ikiPara = (tl, doviz, pb) => Math.abs(tl) < 0.005 ? '—' : pb === 'TL' ? para(tl) : cift(para(tl), para(doviz, pb));
  const vgH = (v, h = 2) => v ? `<span class="kirmizi">${para(v, 'TL', h)}</span>` : '—';
  const usd0 = v => para(v, 'USD', 0);

  // Kırılım sekmesi: cari kart ve ürün ağacı alanlarından birine göre satış özeti (alan sekme şeridinin sağındaki kutudan seçilir)
  const KIRILIM = { fGrup: ['Faaliyet Grubu', k => k.grup], anaGrup: ['Faaliyet Ana Grubu', k => k.anaGrup], urunSinifi: ['Ürün Sınıfı', k => k.sinif], urunGrubu: ['Ürün Grubu', k => k.urunGrubu], urunSerisi: ['Ürün Serisi', k => k.seri],
    pazar: ['Cari Bölge', k => k.b.pazar], ulke: ['Ülke', k => k.b.ulke], grup: ['Cari Sınıfı', k => k.b.grup], iliski: ['İlişki Durumu', k => k.b.iliski], rol: ['Cari Rolü', k => k.b.rol], cariDurum: ['Cari Durumu', k => k.b.cariDurum], kartTipi: ['Cari Kart Tipi', k => k.b.kartTipi], ay: ['Ay', k => k.b.ay] };
  let kirilim = 'fGrup';

  const SEK = {
    belge: {
      ad: 'Faturalar', sira: { k: 'tarih', y: -1 }, satir: () => B.filter(b => belgeUyar(b)), tik: 'detay',
      kol: [
        M('no', 'Belge No', 118, r => r.no, { tek: 1, h: noHucre }), L('tur', 'Belge Türü', 84, r => r.tur, { gizli: 1 }),
        D('tarih', 'Belge Tarihi', 82, r => r.tarih), L('ay', 'Belge Ayı', 70, r => r.ay, { h: r => ayAd(r.ay), et: ayAd, gizli: 1 }),
        D('vade', 'Vade Tarihi', 82, r => r.vade, { h: r => tarih(r.vade) + (r.gecGun > 0 ? `<small class="kirmizi">${r.gecGun} Gün Geçti</small>` : '') }), L('vadeAy', 'Vade Ayı', 70, r => r.vadeAy, { h: r => ayAd(r.vadeAy), et: ayAd, gizli: 1 }),
        L('sirket', 'Şirket', 92, r => r.sirket), M('cari', 'Müşteri', 150, r => r.cari + ' ' + r.cariKod, { h: r => adHucre(r.cari) }), EK(r => r), M('cariKod', 'Cari Kodu', 70, r => r.cariKod, { gizli: 1, tek: 1 }),
        L('grup', 'Cari Sınıfı', 104, r => r.grup, { gizli: 1 }), L('pazar', 'Pazar', 70, r => r.pazar, { gz: 'id' }), L('ulke', 'Ülke', 82, r => r.ulke, { gz: 'i' }), L('sehir', 'Şehir', 82, r => r.sehir, { gz: 'dt' }),
        L('temsilci', 'Satış Temsilcisi', 104, r => r.temsilci), L('kosul', 'Ödeme Koşulu', 104, r => r.kosul, { gz: 'dt' }),
        S('kg', 'Miktar (kg)', 68, r => r.kg, 0, { gz: 'dt' }),
        PB('doviz', 'Döviz Tutarı', 106, r => r.pb === 'TL' ? 0 : r.tutar, { gz: 'i', h: r => dovizH(r.tutar, r.pb) }), S('kur', 'Kur', 62, r => r.kur, 4, { top: 0, gz: 'it', h: r => kurH(r.kur, r.pb) }),
        TL('tl', 'Tutar (₺)', 112, r => r.tl), TL('netTl', "KDV'siz (₺)", 112, r => r.netTl, { gz: 'dt' }), TL('kdvTl', 'KDV (₺)', 96, r => r.kdvTl, { gizli: 1 }),
        PB('tutar', 'Tutar (PB)', 104, r => r.tutar, { gizli: 1 }), PB('net', "KDV'siz (PB)", 104, r => r.net, { gizli: 1 }), L('pb', 'PB', 44, r => r.pb, { gizli: 1 }),
        TL('netUsd', 'Net ($)', 96, r => r.netUsd, { gz: 'i', h: r => para(r.netUsd, 'USD') }), TL('netEur', 'Net (€)', 96, r => r.netEur, { gizli: 1, h: r => para(r.netEur, 'EUR') }),
        TL('odenen', 'Ödenen (₺)', 112, r => r.odenenTl, { h: r => ikiPara(r.odenenTl, r.odenen, r.pb) }), TL('kalan', 'Kalan (₺)', 112, r => r.kalanTl, { h: r => ikiPara(r.kalanTl, r.kalan, r.pb) }),
        TL('vg', 'Vadesi Geçmiş (₺)', 108, r => r.vgTl, { h: r => vgH(r.vgTl) }), TL('bakiye', 'Güncel Bakiye (₺)', 114, r => r.bakiyeTl, { top: 0 }),
        TL('maliyet', 'Maliyet (₺)', 104, r => r.maliyet, { gizli: 1 }), TL('kar', 'Kâr (₺)', 104, r => r.kar, { gizli: 1 }), Y('marj', 'Marj', 58, r => r.marj, marjDip),
        L('durum', 'Durum', 92, r => r.durum, { tek: 1, h: r => rozet(r.durum) }), L('islem', 'İşlem Tipi', 50, r => r.islem, { gizli: 1 }),
        L('depo', 'Depo', 104, r => r.depo, { gizli: 1 }), L('iliski', 'İlişki Durumu', 96, r => r.iliski, { gizli: 1 }), L('rol', 'Cari Rolü', 96, r => r.rol, { gizli: 1 }), L('cariDurum', 'Cari Durumu', 80, r => r.cariDurum, { gizli: 1 }), L('kartTipi', 'Cari Kart Tipi', 90, r => r.kartTipi, { gizli: 1 }), L('kaynak', 'Kaynak', 96, r => r.kaynak, { gizli: 1 })
      ]
    },
    kalem: {
      ad: 'Kalemler', sira: { k: 'tarih', y: -1 }, satir: () => K.filter(k => kalemUyar(k)),
      kol: [
        D('tarih', 'Tarih', 84, B_(b => b.tarih)), M('no', 'Belge No', 120, B_(b => b.no), { tek: 1, h: r => noHucre(r.b) }), L('tur', 'Belge Türü', 84, B_(b => b.tur), { gizli: 1 }),
        L('sirket', 'Şirket', 92, B_(b => b.sirket)), M('cari', 'Müşteri', 146, B_(b => b.cari + ' ' + b.cariKod), { h: r => adHucre(r.b.cari) }), EK(r => r.b), L('ulke', 'Ülke', 80, B_(b => b.ulke), { gz: 'i' }), L('temsilci', 'Satış Temsilcisi', 104, B_(b => b.temsilci)),
        M('ad', 'Ürün Adı', 160, r => r.ad + ' ' + r.kod, { h: r => adHucre(r.ad) }), M('kod', 'Ürün Kodu', 96, r => r.kod, { gizli: 1, tek: 1 }),
        L('anaGrup', 'Faaliyet Ana Grubu', 130, r => r.anaGrup, { gizli: 1 }), L('fGrup', 'Faaliyet Grubu', 150, r => r.grup), L('urunSinifi', 'Ürün Sınıfı', 110, r => r.sinif, { gizli: 1 }), L('urunGrubu', 'Ürün Grubu', 116, r => r.urunGrubu, { gz: 'dt' }), L('urunSerisi', 'Ürün Serisi', 110, r => r.seri, { gizli: 1 }), L('depo', 'Depo', 108, B_(b => b.depo), { gizli: 1 }),
        S('miktar', 'Miktar', 78, r => r.miktar, 0, { h: r => say(r.miktar, 0) + ' ' + esc(r.birim) }), L('birim', 'Birim', 48, r => r.birim, { gizli: 1 }), S('ton', 'Ton', 58, r => r.ton, 2, { gizli: 1 }),
        PB('fiyat', 'Birim Fiyat', 90, r => r.fiyat, { top: 0, h: r => para(r.fiyat, r.b.pb, 4) }),
        PB('doviz', 'Döviz Tutarı', 104, r => r.b.pb === 'TL' ? 0 : r.tutar, { gz: 'i', h: r => dovizH(r.tutar, r.b.pb) }), S('kur', 'Kur', 62, B_(b => b.kur), 4, { top: 0, gizli: 1, h: r => kurH(r.b.kur, r.b.pb) }),
        PB('tutar', 'Tutar (PB)', 104, r => r.tutar, { gizli: 1 }), L('pb', 'PB', 44, B_(b => b.pb), { gizli: 1 }),
        TL('netTl', 'Tutar (₺)', 110, r => r.netTl), TL('usd', 'USD Karşılığı', 98, r => r.usd, { gz: 'i', h: r => para(r.usd, 'USD') }), TL('eur', 'EUR Karşılığı', 98, r => r.eur, { gizli: 1, h: r => para(r.eur, 'EUR') }),
        TL('maliyet', 'Maliyet (₺)', 108, r => r.maliyetTl), TL('kar', 'Kâr (₺)', 104, r => r.kar, { h: r => `<span class="${r.kar < 0 ? 'kirmizi' : ''}">${para(r.kar)}</span>` }), Y('marj', 'Marj', 58, r => r.marj, marjDip),
        L('grup', 'Cari Sınıfı', 104, B_(b => b.grup), { gizli: 1 }), L('pazar', 'Pazar', 70, B_(b => b.pazar), { gizli: 1 }), L('iliski', 'İlişki Durumu', 96, B_(b => b.iliski), { gizli: 1 }), L('islem', 'İşlem Tipi', 50, B_(b => b.islem), { gizli: 1 })
      ]
    },
    siparis: {
      ad: 'Siparişler', sira: { k: 'tarih', y: -1 }, satir: () => B.filter(b => b.sip && belgeUyar(b, { turYok: 1 })), tik: 'detay',
      kol: [
        M('no', 'Sipariş No', 118, r => r.sip.no + ' ' + r.sip.talepNo, { tek: 1, h: r => adHucre(r.sip.no) }), M('talepNo', 'Talep No', 104, r => r.sip.talepNo, { gizli: 1, tek: 1 }), D('tarih', 'Sipariş Tarihi', 88, r => r.sip.siparisTarihi),
        L('asama', 'Aşama', 118, r => r.sip.asama, { tek: 1, h: r => rozet(r.sip.asama) }), L('tur', 'Rapor Tipi', 92, r => r.tur, { gizli: 1 }),
        L('sirket', 'Fatura Kesen', 94, r => r.sirket), L('gonderici', 'Gönderici', 94, r => r.sip.gonderici, { gizli: 1 }), M('cari', 'Müşteri', 146, r => r.cari + ' ' + r.cariKod, { h: r => adHucre(r.cari) }), EK(r => r),
        L('ulke', 'Ülke', 80, r => r.ulke, { gz: 'i' }), L('temsilci', 'Satış Temsilcisi', 104, r => r.temsilci), L('marka', 'Marka', 92, r => r.sip.marka, { tek: 1, gz: 'dt' }),
        D('yukleme', 'Yükleme', 92, r => r.sip.yukleme, { h: r => cift(tarih(r.sip.yukleme) || '—', r.sip.gecikti ? '<span class="kirmizi">Gecikti</span>' : esc(r.sip.saat)) }),
        L('yer', 'Yükleme Yeri', 118, r => r.sip.yer), L('nakliye', 'Nakliye', 130, r => [r.sip.nakliye, r.sip.firma].filter(Boolean).join(' · '), { gizli: 1 }),
        L('teslim', 'Teslim Şekli', 112, r => r.sip.teslim, { gz: 'i' }), L('kosul', 'Ödeme Koşulu', 112, r => r.kosul),
        S('kg', 'Miktar (kg)', 72, r => r.sip.kg), S('koli', 'Koli', 52, r => r.sip.koli, 0, { gizli: 1 }),
        PB('doviz', 'Döviz Tutarı', 104, r => r.pb === 'TL' ? 0 : r.net, { gz: 'i', h: r => dovizH(r.net, r.pb) }), S('kur', 'Kur', 66, r => r.kur, 4, { top: 0, gizli: 1, h: r => kurH(r.kur, r.pb) }),
        TL('netTl', 'Sipariş Tutarı (₺)', 112, r => r.netTl),
        TL('faturalanan', 'Faturalanan (₺)', 108, r => r.tur === 'Fatura' ? r.netTl : 0, { h: r => r.tur === 'Fatura' ? para(r.netTl) : '—' }), TL('acik', 'Açık Sipariş (₺)', 108, r => r.tur === 'Fatura' ? 0 : r.netTl, { h: r => r.tur === 'Fatura' ? '—' : para(r.netTl) }),
        M('faturaNo', 'Fatura No', 112, r => r.sip.faturaNo, { tek: 1 }), L('siparisKaynak', 'Kaynak', 96, r => r.sip.siparisKaynak, { gizli: 1 }), M('teklifNo', 'Teklif No', 96, r => r.sip.teklifNo, { gizli: 1 })
      ]
    },
    tahsilat: {
      ad: 'Tahsilatlar', sira: { k: 'tarih', y: -1 }, satir: () => T.filter(tahsilatUyar),
      kol: [
        D('tarih', 'Tarih', 80, r => r.tarih), M('no', 'Belge No', 124, r => r.no, { tek: 1 }), M('cari', 'Müşteri', 156, r => r.cari + ' ' + r.cariKod, { h: r => adHucre(r.cari) }), EK(r => r), L('ulke', 'Ülke', 82, r => r.ulke, { gz: 'i' }),
        L('temsilci', 'Satış Temsilcisi', 108, r => r.temsilci), L('sirket', 'Şirket', 94, r => r.sirket), L('tur', 'Ödeme Türü', 100, r => r.tur),
        PB('doviz', 'Döviz Tutarı', 108, r => r.pb === 'TL' ? 0 : r.tutar, { gz: 'i', h: r => dovizH(r.tutar, r.pb) }), S('kur', 'Kur', 64, r => r.kur, 4, { top: 0, gz: 'i', h: r => kurH(r.kur, r.pb) }),
        TL('tl', 'Tutar (₺)', 116, r => r.tl), L('hesap', 'Hesap', 136, r => r.hesap), M('cekNo', 'Çek / Senet No', 112, r => r.cekNo, { tek: 1 }), D('cekVade', 'Çek Vadesi', 86, r => r.cekVade), L('banka', 'Banka', 114, r => r.banka),
        L('durum', 'Durum', 118, r => r.durum, { tek: 1, h: r => rozet(r.durum) }), L('islem', 'İşlem Tipi', 50, r => r.islem, { gizli: 1 }),
        PB('tutar', 'Tutar (PB)', 104, r => r.tutar, { gizli: 1 }), L('grup', 'Cari Sınıfı', 104, r => r.grup, { gizli: 1 }), L('pb', 'PB', 44, r => r.pb, { gizli: 1 }), M('aciklama', 'Açıklama', 190, r => r.aciklama, { gizli: 1 })
      ]
    },
    musteri: {
      ad: 'Müşteri', sira: { k: 'netTl', y: -1 }, tik: 'musteri', hizli: 1,
      satir: () => { const th = tahsilatTopla('cariKod'); return ozetle(k => k.b.cariKod, k => ({ cariKod: k.b.cariKod, cari: k.b.cari, grup: k.b.grup, ulke: k.b.ulke, sehir: k.b.sehir, sirket: k.b.sirket, iliski: k.b.iliski, rol: k.b.rol, cariDurum: k.b.cariDurum, kartTipi: k.b.kartTipi })).map(x => Object.assign(x, { temsilci: (C[x.cariKod] || {}).temsilci || '', tahsilat: th[x.cariKod] || 0, bakiyeTl: (C[x.cariKod] || {}).bakiyeTl || 0 })); },
      kol: [
        M('cari', 'Müşteri', 164, r => r.cari + ' ' + r.cariKod, { h: r => adHucre(r.cari) }), EK(r => r), M('cariKod', 'Cari Kodu', 72, r => r.cariKod, { gizli: 1, tek: 1 }), L('grup', 'Cari Sınıfı', 116, r => r.grup), L('iliski', 'İlişki Durumu', 96, r => r.iliski), L('rol', 'Cari Rolü', 100, r => r.rol, { gizli: 1 }), L('cariDurum', 'Cari Durumu', 80, r => r.cariDurum, { gizli: 1 }), L('kartTipi', 'Cari Kart Tipi', 90, r => r.kartTipi, { gizli: 1 }),
        L('ulke', 'Ülke', 84, r => r.ulke, { gz: 'i' }), L('sehir', 'Şehir', 84, r => r.sehir, { gz: 'dt' }), L('temsilci', 'Satış Temsilcisi', 106, r => r.temsilci), L('sirket', 'Şirket', 94, r => r.sirket),
        S('belge', 'Belge', 50, r => r.belge, 0, { tf: tekilBelge }), S('kg', 'Miktar (kg)', 76, r => r.kg), TL0('netTl', 'Net Satış (₺)', 110, r => r.netTl), Y('pay', 'Pay', 54, r => r.pay, () => yuzde(100)),
        TL0('netUsd', 'Net ($)', 96, r => r.netUsd, { gz: 'i', h: r => usd0(r.netUsd) }), TL0('maliyet', 'Maliyet (₺)', 106, r => r.maliyet, { gizli: 1 }), TL0('kar', 'Kâr (₺)', 102, r => r.kar), Y('marj', 'Marj', 56, r => r.marj, marjDip),
        TL0('tahsilat', 'Tahsilat (₺)', 106, r => r.tahsilat), TL0('kalanTl', 'Açık Alacak (₺)', 106, r => r.kalanTl), TL0('vgTl', 'Vadesi Geçmiş (₺)', 106, r => r.vgTl, { h: r => vgH(r.vgTl, 0) }),
        TL0('bakiyeTl', 'Güncel Bakiye (₺)', 110, r => r.bakiyeTl), S('ortVade', 'Ort. Vade (Gün)', 60, r => r.ortVade, 0, { top: 0 }), D('son', 'Son Satış', 80, r => r.son)
      ]
    },
    urun: {
      ad: 'Ürün', sira: { k: 'netTl', y: -1 }, tik: 'urun', hizli: 1,
      satir: () => ozetle(k => k.kod, k => ({ kod: k.kod, ad: k.ad, urunGrup: k.urunGrup, grupAd: k.grup, anaGrup: k.anaGrup, sinif: k.sinif, urunGrubu: k.urunGrubu, seri: k.seri, birim: k.birim })),
      kol: [
        M('ad', 'Ürün Adı', 180, r => r.ad + ' ' + r.kod, { h: r => adHucre(r.ad) }), M('kod', 'Ürün Kodu', 100, r => r.kod, { gizli: 1, tek: 1 }), L('anaGrup', 'Faaliyet Ana Grubu', 130, r => r.anaGrup, { gizli: 1 }), L('fGrup', 'Faaliyet Grubu', 150, r => r.grupAd), L('urunSinifi', 'Ürün Sınıfı', 110, r => r.sinif), L('urunGrubu', 'Ürün Grubu', 116, r => r.urunGrubu), L('urunSerisi', 'Ürün Serisi', 110, r => r.seri),
        S('belge', 'Belge', 54, r => r.belge, 0, { tf: tekilBelge }), S('musteri', 'Müşteri', 58, r => r.musteri, 0, { tf: tekilMusteri }), S('kg', 'Miktar (kg)', 82, r => r.kg), S('ton', 'Ton', 60, r => r.ton, 2, { gizli: 1 }),
        S('ortFiyatTl', 'Ort. Fiyat (₺ / kg)', 84, r => r.ortFiyatTl, 2, { top: 0, gz: 'd' }), S('ortFiyat', 'Ort. Fiyat ($ / kg)', 84, r => r.ortFiyat, 2, { top: 0, gz: 'i' }),
        TL0('netTl', 'Net Satış (₺)', 116, r => r.netTl), Y('pay', 'Pay', 58, r => r.pay, () => yuzde(100)), TL0('netUsd', 'Net ($)', 100, r => r.netUsd, { gz: 'i', h: r => usd0(r.netUsd) }),
        TL0('maliyet', 'Maliyet (₺)', 112, r => r.maliyet), TL0('kar', 'Kâr (₺)', 110, r => r.kar), Y('marj', 'Marj', 60, r => r.marj, marjDip), D('son', 'Son Satış', 82, r => r.son)
      ]
    },
    temsilci: {
      ad: 'Temsilci', sira: { k: 'netTl', y: -1 }, tik: 'temsilci', hizli: 1,
      satir: () => { const th = tahsilatTopla('temsilci'); return ozetle(k => k.b.temsilci, k => ({ temsilci: k.b.temsilci })).map(x => Object.assign(x, { tahsilat: th[x.temsilci] || 0 })); },
      kol: [
        M('temsilci', 'Satış Temsilcisi', 150, r => r.temsilci, { h: r => adHucre(r.temsilci) }), S('musteri', 'Müşteri', 60, r => r.musteri, 0, { tf: tekilMusteri }), S('belge', 'Belge', 56, r => r.belge, 0, { tf: tekilBelge }), S('kg', 'Miktar (kg)', 86, r => r.kg),
        TL0('yurtici', 'Yurtiçi (₺)', 110, r => r.yurtici, { gz: 'id' }), TL0('yurtdisi', 'Yurtdışı (₺)', 110, r => r.yurtdisi, { gz: 'id' }), TL0('netTl', 'Net Satış (₺)', 118, r => r.netTl), Y('pay', 'Pay', 58, r => r.pay, () => yuzde(100)),
        TL0('netUsd', 'Net ($)', 100, r => r.netUsd, { gz: 'i', h: r => usd0(r.netUsd) }), TL0('maliyet', 'Maliyet (₺)', 112, r => r.maliyet, { gizli: 1 }), TL0('kar', 'Kâr (₺)', 110, r => r.kar), Y('marj', 'Marj', 60, r => r.marj, marjDip),
        TL0('tahsilat', 'Tahsilat (₺)', 112, r => r.tahsilat), TL0('kalanTl', 'Açık Alacak (₺)', 112, r => r.kalanTl), TL0('vgTl', 'Vadesi Geçmiş (₺)', 112, r => r.vgTl, { h: r => vgH(r.vgTl, 0) }), D('son', 'Son Satış', 82, r => r.son)
      ]
    },
    sirket: {
      ad: 'Şirket', sira: { k: 'netTl', y: -1 }, tik: 'sirket',
      satir: () => { const th = tahsilatTopla('sirket'); return ozetle(k => k.b.sirket, k => ({ sirket: k.b.sirket })).map(x => Object.assign(x, { tahsilat: th[x.sirket] || 0 })); },
      kol: [
        M('sirket', 'Şirket', 150, r => r.sirket, { h: r => adHucre(r.sirket) }), S('musteri', 'Müşteri', 60, r => r.musteri, 0, { tf: tekilMusteri }), S('belge', 'Belge', 56, r => r.belge, 0, { tf: tekilBelge }), S('kg', 'Miktar (kg)', 86, r => r.kg),
        TL0('yurtici', 'Yurtiçi (₺)', 110, r => r.yurtici, { gz: 'id' }), TL0('yurtdisi', 'Yurtdışı (₺)', 110, r => r.yurtdisi, { gz: 'id' }), TL0('grupIci', 'Grup İçi (₺)', 106, r => r.grupIci), TL0('netTl', 'Net Satış (₺)', 118, r => r.netTl), Y('pay', 'Pay', 58, r => r.pay, () => yuzde(100)),
        TL0('kdvTl', 'KDV (₺)', 102, r => r.kdvTl, { gz: 'd' }), TL0('brutTl', 'KDV Dahil (₺)', 116, r => r.brutTl, { gz: 'd' }), TL0('netUsd', 'Net ($)', 100, r => r.netUsd, { gz: 'i', h: r => usd0(r.netUsd) }),
        TL0('kar', 'Kâr (₺)', 110, r => r.kar), Y('marj', 'Marj', 60, r => r.marj, marjDip),
        TL0('tahsilat', 'Tahsilat (₺)', 112, r => r.tahsilat), TL0('kalanTl', 'Açık Alacak (₺)', 112, r => r.kalanTl), TL0('vgTl', 'Vadesi Geçmiş (₺)', 112, r => r.vgTl, { h: r => vgH(r.vgTl, 0) })
      ]
    },
    kirilim: {
      ad: 'Kırılım', sira: { k: 'netTl', y: -1 }, tik: 'kirilim', hizli: 1,
      satir: () => ozetle(k => KIRILIM[kirilim][1](k), k => ({ deger: KIRILIM[kirilim][1](k) })),
      kol: [
        { k: 'deger', get ad() { return KIRILIM[kirilim][0]; }, w: 210, t: 'm', v: r => kirilim === 'ay' ? ayAd(r.deger) : r.deger, h: r => adHucre(kirilim === 'ay' ? ayAd(r.deger) : r.deger) },
        S('musteri', 'Müşteri', 60, r => r.musteri, 0, { tf: tekilMusteri }), S('belge', 'Belge', 56, r => r.belge, 0, { tf: tekilBelge }), S('kg', 'Miktar (kg)', 86, r => r.kg),
        TL0('yurtici', 'Yurtiçi (₺)', 110, r => r.yurtici, { gz: 'id' }), TL0('yurtdisi', 'Yurtdışı (₺)', 110, r => r.yurtdisi, { gz: 'id' }), TL0('netTl', 'Net Satış (₺)', 118, r => r.netTl), Y('pay', 'Pay', 58, r => r.pay, () => yuzde(100)),
        TL0('netUsd', 'Net ($)', 100, r => r.netUsd, { gz: 'i', h: r => usd0(r.netUsd) }), TL0('maliyet', 'Maliyet (₺)', 112, r => r.maliyet), TL0('kar', 'Kâr (₺)', 110, r => r.kar), Y('marj', 'Marj', 60, r => r.marj, marjDip), D('son', 'Son Satış', 82, r => r.son)
      ]
    },
    ozet: { ad: 'Özet' }
  };

  // ---------- sütun görünürlüğü (tarayıcıda saklanır) ----------
  const kolAnahtar = p => 'sr-kol2-' + p + '-' + pzHarf();
  const kolAyar = p => { try { return JSON.parse(localStorage.getItem(kolAnahtar(p)) || '{}'); } catch (e) { return {}; } };
  const gorunenKol = p => { const a = kolAyar(p); return SEK[p].kol.filter(c => c.k in a ? a[c.k] : !(c.gizli || (c.gz || '').includes(pzHarf()))); };

  // ---------- çoklu seçim açılır listesi ----------
  let pop = null;
  const popKapat = () => { if (pop) { pop.el.remove(); pop.dugme.classList.remove('acik'); pop = null; } };
  /** secenekler: [{ v, ad, n }] ; secili: dizi (boş = tümü) ; uygula(dizi) her değişimde çağrılır */
  function popAc(dugme, secenekler, secili, uygula, o = {}) {
    if (pop && pop.dugme === dugme) { popKapat(); return; } popKapat();
    const el = document.createElement('div'); el.className = 'sr-pop'; let sec = new Set(secili);
    const liste = q => secenekler.filter(s => !q || kucuk(s.ad).includes(q)).map(s => `<label><input type="checkbox" value="${esc(s.v)}"${sec.has(s.v) ? ' checked' : ''}><span>${esc(s.ad || '—')}</span>${s.n !== undefined ? `<small>${s.n}</small>` : ''}</label>`).join('') || '<div style="padding:8px 10px;color:#94a3b8">Kayıt Yok</div>';
    el.innerHTML = `<div class="ust"><a data-i="tumu">${o.tumuAd || 'Tümü'}</a><a data-i="temizle">Temizle</a><span class="say"></span></div>${secenekler.length > 8 ? '<div class="ara"><input type="search" placeholder="Ara…"></div>' : ''}<div class="liste">${liste('')}</div>`;
    document.body.appendChild(el);
    const r = dugme.getBoundingClientRect(); el.style.minWidth = Math.max(180, r.width) + 'px'; el.style.top = Math.min(r.bottom + 3, window.innerHeight - el.offsetHeight - 8) + 'px'; el.style.left = Math.max(6, Math.min(r.left, window.innerWidth - el.offsetWidth - 8)) + 'px';
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

  // ---------- genel süzgeç düğmeleri ----------
  const say_ = (l, f, ad) => { const m = new Map(); l.forEach(x => { const v = f(x); if (v === '' || v == null) return; const o = m.get(v) || { v, ad: ad ? ad(x) : v, n: 0 }; o.n++; m.set(v, o); }); return [...m.values()].sort((a, b) => String(a.ad).localeCompare(String(b.ad), TR)); };
  const ETIKET = { musteri: b => b.cari + ' · ' + b.cariKod, kalem: k => k.ad + ' · ' + k.kod };
  /** bir süzgecin seçenekleri: ÖBÜR süzgeçlere (ve pazara) uyan kayıtlardaki değerler — üst seçim alt listeleri daraltır (Ürün Sınıfı seçilince Ürün Grubu listesi kısalır) */
  function secenekler(a) {
    let l;
    if (a === 'tur') l = ['Fatura', 'İade Faturası', 'Açık Sipariş', 'Teslimat'].map(v => ({ v, ad: v, n: B.filter(b => b.tur === v && genelUyar(b) && urunUyar(b)).length }));
    else if (CARI_AL[a]) l = say_(B.filter(b => genelUyar(b, a) && urunUyar(b)), CARI_AL[a], ETIKET[a]);
    else l = say_(K.filter(k => genelUyar(k.b) && urunSatirUyar(k, a)), URUN_AL[a], ETIKET[a]);
    F[a].forEach(v => { if (!l.some(s => s.v === v)) l.push({ v, ad: v, n: 0 }); });
    return l;
  }
  const SECENEK = new Proxy({}, { get: (_, a) => () => secenekler(a) });
  function suzDugmeYaz() {
    kok.querySelectorAll('#srSuz .sr-cs').forEach(d => {
      // Yurtiçinde ülke hep Türkiye: aynı kutu Şehir süzgecine döner (ızgara bozulmaz)
      if (d.dataset.yer) { d.dataset.f = PZ === 'Yurtiçi' ? 'sehir' : 'ulke'; d.dataset.e = PZ === 'Yurtiçi' ? 'Şehir' : 'Ülke'; }
      const f = d.dataset.f, l = F[f], sn = SECENEK[f](); let yazi = d.dataset.bos || 'Tümü';
      if (l.length === 1) yazi = (sn.find(s => s.v === l[0]) || {}).ad || l[0]; else if (l.length > 1) yazi = l.length + ' Seçili';
      if (f === 'tur') yazi = l.length === 0 ? 'Tümü' : l.length === 2 && SATIS_TUR.every(x => l.includes(x)) ? 'Fatura + İade' : yazi;
      d.innerHTML = `<span class="e">${esc(d.dataset.e)}</span><span class="d">${esc(yazi)}</span>`; d.title = d.dataset.e + ': ' + yazi; d.classList.toggle('dolu', f === 'tur' ? !(l.length === 2 && SATIS_TUR.every(x => l.includes(x))) : l.length > 0);
    });
    const d = F.bas || F.bit ? (F.bas ? tarih(F.bas) : '…') + ' – ' + (F.bit ? tarih(F.bit) : '…') : 'Tüm Dönemler';
    $('#srDonemYazi').textContent = (PZ || 'Yurtiçi + Yurtdışı') + ' · ' + (F.sirket.length ? F.sirket.join(' + ') : 'Tüm Şirketler') + ' · ' + d;
    kok.querySelectorAll('#srPazar button').forEach(b => b.classList.toggle('aktif', b.dataset.pz === PZ)); const ih = $('#srIhracat'); if (ih) ih.style.display = PZ === 'Yurtiçi' ? 'none' : '';
  }
  kok.querySelector('#srSuz').addEventListener('click', e => {
    const d = e.target.closest('.sr-cs'); if (!d) return; const f = d.dataset.f;
    popAc(d, SECENEK[f](), F[f], l => { F[f] = l; suzDugmeYaz(); ciz(); }, { bosAd: d.dataset.bos || 'Tümü' });
  });
  function donemAyarla(v) {
    const b = tarihYap(V.bugun), y = b.getFullYear(), a = b.getMonth(); let bas = '', bit = V.bugun;
    if (v === 'yil') bas = y + '-01-01'; else if (v === 'ay') bas = gun(new Date(y, a, 1)); else if (v === 'gecenay') { bas = gun(new Date(y, a - 1, 1)); bit = gun(new Date(y, a, 0)); }
    else if (v === 'ceyrek') bas = gun(new Date(y, Math.floor(a / 3) * 3, 1)); else if (v === 'son12') bas = gun(new Date(y, a - 11, 1)); else if (v === 'gecenyil') { bas = (y - 1) + '-01-01'; bit = (y - 1) + '-12-31'; }
    else if (v === 'tumu') { bas = ''; bit = ''; } else return;
    F.bas = bas; F.bit = bit; $('#srBas').value = bas; $('#srBit').value = bit;
  }
  $('#srDonem').addEventListener('change', e => { donemAyarla(e.target.value); suzDugmeYaz(); ciz(); });
  ['#srBas', '#srBit'].forEach(s => $(s).addEventListener('change', () => { F.bas = $('#srBas').value; F.bit = $('#srBit').value; $('#srDonem').value = 'ozel'; suzDugmeYaz(); ciz(); }));
  let araZ; $('#srAra').addEventListener('input', e => { clearTimeout(araZ); araZ = setTimeout(() => { F.ara = kucuk(e.target.value.trim()); ciz(); }, 200); });
  $('#srTemizle').addEventListener('click', () => { F = BOS_F(); H.marj20 = H.ciro10 = false; Object.values(DURUM).forEach(d => d.suz = {}); $('#srAra').value = ''; $('#srDonem').value = 'yil'; donemAyarla('yil'); suzDugmeYaz(); ciz(true); });

  // ---------- tablo ----------
  const tablo = $('#srTablo'), kap = $('#srKap');
  function yukseklik() { if ($('#srTabloKart').hidden) return; kap.style.maxHeight = Math.max(220, window.innerHeight - kap.getBoundingClientRect().top - 12) + 'px'; }
  const sayiCoz = s => parseFloat(String(s).replace(/\./g, '').replace(',', '.'));
  /** sayı süzgeci: ">1000", "<=500", "100-200", "=0" ya da metin içerir */
  function sayiSuz(q, v, metin) {
    let m; q = q.replace(/\s/g, '');
    if ((m = /^(>=|<=|>|<|=)(-?[\d.,]+)$/.exec(q))) { const n = sayiCoz(m[2]); return m[1] === '>' ? v > n : m[1] === '<' ? v < n : m[1] === '>=' ? v >= n : m[1] === '<=' ? v <= n : Math.abs(v - n) < 0.005; }
    if ((m = /^(-?[\d.,]+)(?:-|\.\.)(-?[\d.,]+)$/.exec(q))) return v >= sayiCoz(m[1]) && v <= sayiCoz(m[2]);
    return metin.includes(q);
  }
  function kolonUyar(r, kol, suz) {
    for (const c of kol) {
      if (c.t === 'ik') continue;
      const q = suz[c.k]; if (q == null || q === '' || (Array.isArray(q) && !q.length)) continue; const v = c.v(r);
      if (c.t === 'l') { if (!q.includes(String(v ?? ''))) return false; }
      else if (c.t === 'd') { if (!tarih(v).includes(q)) return false; }
      else if (c.t === 'm') { if (!kucuk(v).includes(kucuk(q))) return false; }
      else if (!sayiSuz(q, +v || 0, say(v, c.hane ?? 2))) return false;
    }
    return true;
  }
  function cizBaslik() {
    const tn = SEK[sekme], Dr = durum(sekme), kol = gorunenKol(sekme), W = 34 + topla(kol, c => c.w);
    tablo.style.minWidth = Math.round(W * 0.74) + 'px';
    tablo.querySelector('colgroup').innerHTML = `<col style="width:${(34 / W * 100).toFixed(3)}%">` + kol.map(c => `<col style="width:${(c.w / W * 100).toFixed(3)}%">`).join('');
    const sr = Dr.sira || tn.sira;
    tablo.tHead.innerHTML = `<tr><th data-k="">#</th>${kol.map(c => c.t === 'ik' ? `<th data-k="" class="ik" title="Cari Ekstre'ye Git"><i class="fas fa-file-invoice"></i></th>` : `<th data-k="${c.k}" class="${sr && sr.k === c.k ? (sr.y > 0 ? 'art' : 'azal') : ''}" title="${esc(c.ad)} · sıralamak için tıklayın">${esc(c.ad)}</th>`).join('')}</tr>`
      + `<tr class="suzgec"><th></th>${kol.map(c => { if (c.t === 'ik') return '<th></th>'; const q = Dr.suz[c.k]; return c.t === 'l' ? `<th><button type="button" data-k="${c.k}" class="${q && q.length ? 'dolu' : ''}">${q && q.length ? (q.length === 1 ? esc(c.et ? c.et(q[0]) : q[0]) : q.length + ' Seçili') : 'Tümü'}</button></th>` : `<th><input data-k="${c.k}" value="${esc(q || '')}" class="${q ? 'dolu' : ''}" placeholder="${c.t === 'p' || c.t === 's' || c.t === 'y' ? '> <' : 'Ara'}" title="${c.t === 'm' ? 'İçerir' : c.t === 'd' ? 'Tarih içerir (örn. 10.2026)' : 'Sayı süzgeci: >1000 · <=500 · 100-200 · =0'}"></th>`; }).join('')}</tr>`;
    tablo.style.setProperty('--sr-bas', tablo.tHead.rows[0].offsetHeight + 'px');
  }
  function cizGovde() {
    const tn = SEK[sekme], Dr = durum(sekme), kol = gorunenKol(sekme), sr = Dr.sira || tn.sira;
    const tum = Dr.tum = tn.satir(); let rows = tum.filter(r => kolonUyar(r, kol, Dr.suz));
    if (sr) { const c = SEK[sekme].kol.find(x => x.k === sr.k); if (c) rows = rows.slice().sort((a, b) => sirala(c.v(a), c.v(b)) * sr.y); }
    Dr.gorunen = rows; const n = kol.length + 1, goster = rows.slice(0, Dr.limit);
    const hucre = (c, r) => { if (c.t === 'ik') return `<td class="ik">${c.h(r)}</td>`; const v = c.v(r), sayisal = c.t === 'p' || c.t === 's' || c.t === 'y'; return `<td class="${sayisal ? 'p' : c.t === 'd' || c.tek ? '' : 'yz'}${sayisal && v < 0 ? ' eksi' : ''}"${c.t === 'm' || c.t === 'l' ? ` title="${esc(v)}"` : ''}>${c.h ? c.h(r) : esc(v)}</td>`; };
    tablo.tBodies[0].innerHTML = !rows.length ? `<tr><td class="bos" colspan="${n}">Süzgeçlere uyan kayıt yok.</td></tr>`
      : goster.map((r, i) => { const id = r.id ?? r.a ?? i, acik = tn.tik === 'detay' && Dr.acik.has(id); return `<tr class="${tn.tik ? 'tik' : ''}${acik ? ' acik' : ''}" data-i="${i}"><td class="sayac">${i + 1}</td>${kol.map(c => hucre(c, r)).join('')}</tr>${acik ? `<tr class="detay"><td colspan="${n}">${detayHtml(r)}</td></tr>` : ''}`; }).join('')
        + (rows.length > goster.length ? `<tr class="detay"><td class="dahafazla" colspan="${n}"><button type="button" class="sr-dgm" data-daha="1">Sonraki 500 Satır</button> <button type="button" class="sr-dgm" data-daha="0">Tümünü Göster (${say(rows.length, 0)})</button></td></tr>` : '');
    tablo.tFoot.innerHTML = !rows.length ? '' : `<tr><td class="sayac"></td>${kol.map((c, i) => {
      if (c.tf) return `<td class="p">${c.tf(rows)}</td>`;
      if (c.top === 'pb') { const m = {}; rows.forEach(r => { const pb = r.pb || (r.b && r.b.pb) || 'TL'; m[pb] = (m[pb] || 0) + (+c.v(r) || 0); }); return `<td class="p">${Object.keys(m).filter(pb => Math.abs(m[pb]) > 0.005).sort().map(pb => para(m[pb], pb)).join('<br>') || '—'}</td>`; }
      if (c.top === 1) { const t = topla(rows, c.v); return `<td class="p">${c.t === 's' ? say(t, c.hane ?? 0) : /\$/.test(c.ad) || /USD/.test(c.ad) ? para(t, 'USD', c.hane ?? 2) : /€/.test(c.ad) || /EUR/.test(c.ad) ? para(t, 'EUR', c.hane ?? 2) : para(t, 'TL', c.hane ?? 2)}</td>`; }
      return `<td>${i === 0 ? 'Toplam · ' + say(rows.length, 0) : ''}</td>`;
    }).join('')}</tr>`;
    $('#srKapsam').textContent = say(rows.length, 0) + (rows.length !== tum.length ? ' / ' + say(tum.length, 0) : '') + ' Kayıt';
  }
  /** belge satırının kalem kırılımı (Satışçı Analiz Raporu'ndaki fatura → kalem açılımı) */
  function detayHtml(b) {
    const s = b.sip, bilgi = [['Belge', b.no + ' · ' + b.tur], ['Müşteri', b.cari + ' (' + b.cariKod + ')'], ['Temsilci', b.temsilci], ['Ödeme Koşulu', b.kosul], ['Depo', b.depo], ['Kaynak', b.kaynak]];
    if (s) bilgi.push(['Sipariş', s.no + (s.talepNo ? ' · ' + s.talepNo : '')], ['Yükleme', (tarih(s.yukleme) || '—') + ' ' + s.saat + ' · ' + s.yer], ['Nakliye', [s.nakliye, s.firma].filter(Boolean).join(' · ')], ['Teslim', s.teslim]);
    const kl = K.filter(k => k.b === b);
    return `<div class="sr-detay-ust">${bilgi.filter(x => x[1] && x[1].trim()).map(x => `<span>${x[0]}: <b>${esc(x[1])}</b></span>`).join('')}</div>`
      + `<table class="sr-mini"><thead><tr><th>#</th><th>Ürün Kodu</th><th>Ürün Adı</th><th>Faaliyet Grubu</th><th>Ürün Grubu</th><th>Miktar</th><th>Birim</th><th>Fiyat</th><th>Tutar</th><th>Tutar (₺)</th><th>Maliyet (₺)</th><th>Kâr (₺)</th><th>Marj</th></tr></thead><tbody>`
      + kl.map(k => `<tr><td>${k.sira}</td><td>${esc(k.kod)}</td><td>${esc(k.ad)}</td><td>${esc(k.grup)}</td><td>${esc(k.urunGrubu)}</td><td>${say(k.miktar, 0)}</td><td>${esc(k.birim)}</td><td>${para(k.fiyat, b.pb, 4)}</td><td>${para(k.tutar, b.pb)}</td><td>${para(k.netTl)}</td><td>${para(k.maliyetTl)}</td><td class="${k.kar < 0 ? 'kirmizi' : ''}">${para(k.kar)}</td><td>${yuzde(k.marj)}</td></tr>`).join('')
      + `<tr class="top"><td colspan="5">KDV'siz Toplam${b.kdvTl ? ' · KDV ' + para(b.tutar - b.net, b.pb) + ' · Genel Toplam ' + para(b.tutar, b.pb) : ''}</td><td>${say(topla(kl, k => k.miktar), 0)}</td><td></td><td></td><td>${para(b.net, b.pb)}</td><td>${para(b.netTl)}</td><td>${para(b.maliyet)}</td><td>${para(b.kar)}</td><td>${yuzde(b.marj)}</td></tr></tbody></table>`;
  }
  tablo.tHead.addEventListener('click', e => {
    const dg = e.target.closest('tr.suzgec button'); if (dg) { const c = SEK[sekme].kol.find(x => x.k === dg.dataset.k), Dr = durum(sekme); popAc(dg, say_(Dr.tum || [], r => String(c.v(r) ?? ''), c.et ? r => c.et(String(c.v(r) ?? '')) : null), Dr.suz[c.k] || [], l => { Dr.suz[c.k] = l; dg.classList.toggle('dolu', l.length > 0); dg.textContent = l.length ? (l.length === 1 ? (c.et ? c.et(l[0]) : l[0]) : l.length + ' Seçili') : 'Tümü'; Dr.limit = 250; cizGovde(); }); return; }
    if (e.target.closest('tr.suzgec')) return;
    const th = e.target.closest('th'); if (!th || !th.dataset.k) return; const Dr = durum(sekme), sr = Dr.sira || SEK[sekme].sira;
    Dr.sira = { k: th.dataset.k, y: sr && sr.k === th.dataset.k ? -sr.y : (/^(p|s|y|d)$/.test(SEK[sekme].kol.find(x => x.k === th.dataset.k).t) ? -1 : 1) }; cizBaslik(); cizGovde();
  });
  let suzZ; tablo.tHead.addEventListener('input', e => { const i = e.target.closest('tr.suzgec input'); if (!i) return; const Dr = durum(sekme); Dr.suz[i.dataset.k] = i.value.trim(); i.classList.toggle('dolu', !!i.value.trim()); Dr.limit = 250; clearTimeout(suzZ); suzZ = setTimeout(cizGovde, 180); });
  tablo.tBodies[0].addEventListener('click', e => {
    const daha = e.target.closest('[data-daha]'); if (daha) { const Dr = durum(sekme); Dr.limit = daha.dataset.daha === '1' ? Dr.limit + 500 : Infinity; cizGovde(); return; }
    const tr = e.target.closest('tr.tik'); if (!tr || e.target.closest('a,button,input')) return; const Dr = durum(sekme), r = Dr.gorunen[+tr.dataset.i], tik = SEK[sekme].tik; if (!r) return;
    if (tik === 'detay') { const id = r.id; if (Dr.acik.has(id)) Dr.acik.delete(id); else Dr.acik.add(id); cizGovde(); return; }
    // özet satırı → o kayda süzülmüş Belgeler / Kalemler
    if (tik === 'musteri') F.musteri = [r.cariKod]; else if (tik === 'urun') F.kalem = [r.kod]; else if (tik === 'temsilci') F.temsilci = [r.temsilci]; else if (tik === 'sirket') F.sirket = [r.sirket];
    else if (tik === 'kirilim') { if (kirilim === 'ay') { const d = tarihYap(r.deger + '-01'); F.bas = r.deger + '-01'; F.bit = gun(new Date(d.getFullYear(), d.getMonth() + 1, 0)); $('#srBas').value = F.bas; $('#srBit').value = F.bit; $('#srDonem').value = 'ozel'; } else if (kirilim === 'pazar') PZ = r.deger; else F[kirilim] = [r.deger]; }
    suzDugmeYaz(); sekmeAc(tik === 'urun' || (tik === 'kirilim' && URUN_AL[kirilim]) ? 'kalem' : 'belge');
  });

  // ---------- Özet (Satış Analiz Dashboard) ----------
  function cizOzet() {
    const satis = K.filter(k => kalemUyar(k, { satis: 1 })), belgeler = [...new Set(satis.map(k => k.b))], net = topla(satis, k => k.netTl), maliyet = topla(satis, k => k.maliyetTl), kar = net - maliyet;
    const tarihsiz = K.filter(k => kalemUyar(k, { satis: 1, tarihYok: 1 })), bugun = V.bugun, y = +bugun.slice(0, 4), buAy = bugun.slice(0, 7), ga = tarihYap(buAy + '-01'); ga.setMonth(ga.getMonth() - 1); const gecenAy = gun(ga).slice(0, 7);
    const ayTop = a => topla(tarihsiz.filter(k => k.b.ay === a), k => k.netTl), aralik = (a, b) => topla(tarihsiz.filter(k => k.b.tarih >= a && k.b.tarih <= b), k => k.netTl);
    const cBuAy = ayTop(buAy), cGecenAy = ayTop(gecenAy), ytd = aralik(y + '-01-01', bugun), gYtd = aralik((y - 1) + '-01-01', (y - 1) + bugun.slice(4));
    const degisim = (a, b) => b ? `<b class="${a >= b ? 'yesil' : 'kirmizi'}">${a >= b ? '▲' : '▼'} ${yuzde(Math.abs((a - b) / b * 100))}</b>` : '—';
    const tah = topla(T.filter(tahsilatUyar), t => t.tl), sip = B.filter(b => b.sip && b.tur !== 'Fatura' && belgeUyar(b, { turYok: 1, tarihYok: 1 }));
    const grupIci = topla(satis.filter(k => k.b.iliski === 'Grup İçi'), k => k.netTl), iade = topla(satis.filter(k => k.b.tur === 'İade Faturası'), k => k.netTl);
    const kpi = [
      ['Net Satış (Dönem)', para(net, 'TL', 0), `${belgeler.length} Belge · ${new Set(belgeler.map(b => b.cariKod)).size} Müşteri`, 'belge'],
      ['Bu Ay Ciro', para(cBuAy, 'TL', 0), `${degisim(cBuAy, cGecenAy)} Geçen Ay ${para(cGecenAy, 'TL', 0)}`, 'belge'],
      ['Yıllık Ciro', para(ytd, 'TL', 0), `${degisim(ytd, gYtd)} Geçen Yıl ${para(gYtd, 'TL', 0)}`, 'belge'],
      ['Brüt Kâr', para(kar, 'TL', 0), `Maliyet ${para(maliyet, 'TL', 0)}`, 'urun'],
      ['Ortalama Marj', yuzde(net ? kar / net * 100 : 0), `Net Satış ${para(topla(satis, k => k.usd), 'USD', 0)}`, 'urun'],
      ['Miktar', say(topla(satis, k => k.kg) / 1000, 1) + ' Ton', `${new Set(satis.map(k => k.kod)).size} Kalem`, 'kalem'],
      ['Tahsilat (Dönem)', para(tah, 'TL', 0), net ? `Net Satışın ${yuzde(tah / net * 100, 0)}` : '', 'tahsilat'],
      ['Açık Alacak', para(topla(belgeler, b => b.kalanTl), 'TL', 0), `${belgeler.filter(b => b.kalanTl > 0.005).length} Açık Belge`, 'musteri'],
      ['Vadesi Geçmiş', para(topla(belgeler, b => b.vgTl), 'TL', 0), `${belgeler.filter(b => b.vgTl > 0).length} Belge`, 'musteri'],
      ['Açık Sipariş · Teslimat', para(topla(sip, b => b.netTl), 'TL', 0), `${sip.filter(b => b.tur === 'Açık Sipariş').length} Sipariş · ${sip.filter(b => b.tur === 'Teslimat').length} Teslimat`, 'siparis'],
      ['Grup İçi Satış', para(grupIci, 'TL', 0), net ? `Net Satışın ${yuzde(grupIci / net * 100)}` : '', 'sirket'],
      ['İade', para(Math.abs(iade), 'TL', 0), `${belgeler.filter(b => b.tur === 'İade Faturası').length} İade Faturası`, 'belge']
    ];
    $('#srKpi').innerHTML = kpi.map(k => `<div data-git="${k[3]}" title="${esc(SEK[k[3]].ad)} sekmesine git"><div class="l">${k[0]}</div><div class="v">${k[1]}</div><div class="s">${k[2]}</div></div>`).join('');
    // aylık: bu yıl / geçen yıl — tek eksen, iki seri (lacivert = bu yıl, gri = geçen yıl)
    const seri = yil => AY.map((_, i) => ayTop(yil + '-' + String(i + 1).padStart(2, '0'))), bu = seri(y), gecen = seri(y - 1), enB = Math.max(1, ...bu, ...gecen);
    const adim = (() => { const ham = enB / 4, us = Math.pow(10, Math.floor(Math.log10(ham))), n = ham / us; return (n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10) * us; })(), ust = Math.ceil(enB / adim) * adim;
    const Wg = 660, Hg = 210, sol = 46, alt = 22, ustB = 8, gw = (Wg - sol - 6) / 12, yk = v => ustB + (Hg - alt - ustB) * (1 - v / ust), kisa = v => v >= 1e6 ? say(v / 1e6, v % 1e6 ? 1 : 0) + ' Mn' : say(v / 1e3, 0) + ' B';
    let svg = `<svg viewBox="0 0 ${Wg} ${Hg}" width="100%" role="img" aria-label="Aylık net satış: ${y} ve ${y - 1}">`;
    for (let v = 0; v <= ust + 1; v += adim) svg += `<line x1="${sol}" x2="${Wg - 4}" y1="${yk(v)}" y2="${yk(v)}" stroke="#eef2f7"/><text x="${sol - 6}" y="${yk(v) + 3}" text-anchor="end" font-size="9" fill="#94a3b8">${kisa(v)}</text>`;
    AY.forEach((a, i) => {
      const x = sol + i * gw, bw = Math.min(16, gw / 2 - 4), c = x + gw / 2, cubuk = (v, renk, dx) => v > 0 ? `<rect x="${c + dx}" y="${yk(v)}" width="${bw}" height="${Math.max(1, Hg - alt - yk(v))}" rx="3" fill="${renk}"/>` : '';
      svg += cubuk(gecen[i], '#cbd5e1', -bw - 1) + cubuk(bu[i], '#1e3a8a', 1) + `<text x="${c}" y="${Hg - 7}" text-anchor="middle" font-size="9.5" fill="#64748b">${a}</text><rect class="alan" data-i="${i}" x="${x}" y="${ustB}" width="${gw}" height="${Hg - alt - ustB}" fill="transparent"/>`;
    });
    const aylik = $('#srAylik'); aylik.innerHTML = svg + `<line x1="${sol}" x2="${Wg - 4}" y1="${Hg - alt}" y2="${Hg - alt}" stroke="#cbd5e1"/></svg><div class="sr-ipucu"></div>`;
    $('#srAylikGosterge').innerHTML = `<span><i style="background:#1e3a8a"></i>${y}</span><span><i style="background:#cbd5e1"></i>${y - 1}</span>`;
    const ipucu = aylik.querySelector('.sr-ipucu');
    aylik.onmousemove = e => { const a = e.target.closest('.alan'); if (!a) { ipucu.style.display = 'none'; return; } const i = +a.dataset.i, r = aylik.getBoundingClientRect(); ipucu.innerHTML = `<b>${AY[i]}</b><br>${y}: <b>${para(bu[i], 'TL', 0)}</b><br>${y - 1}: <b>${para(gecen[i], 'TL', 0)}</b>${gecen[i] && bu[i] ? `<br>Değişim: <b>${bu[i] >= gecen[i] ? '+' : '−'}${yuzde(Math.abs((bu[i] - gecen[i]) / gecen[i] * 100))}</b>` : ''}`; ipucu.style.display = 'block'; ipucu.style.left = Math.min(e.clientX - r.left + 12, r.width - ipucu.offsetWidth - 4) + 'px'; ipucu.style.top = Math.max(4, e.clientY - r.top - ipucu.offsetHeight - 8) + 'px'; };
    aylik.onmouseleave = () => ipucu.style.display = 'none';
    // dağılımlar: tek renk (büyüklük), ilk 8 + Diğer; ada tıklayınca o değere süzülür
    const dagilim = (el, anahtar, f) => {
      const m = {}; satis.forEach(k => m[anahtar(k)] = (m[anahtar(k)] || 0) + k.netTl); let l = Object.keys(m).map(a => ({ a, v: m[a] })).sort((a, b) => b.v - a.v);
      if (l.length > 9) l = l.slice(0, 8).concat({ a: 'Diğer', v: topla(l.slice(8), x => x.v), diger: 1 }); const en = Math.max(1, ...l.map(x => x.v));
      $(el).innerHTML = l.length ? `<div class="sr-cubuk">${l.map(x => `<span class="ad" ${x.diger || !f ? 'style="cursor:default;text-decoration:none"' : `data-f="${f}" data-v="${esc(x.a)}"`} title="${esc(x.a)}">${esc(x.a || '—')}</span><span class="yol" title="${esc(x.a)}: ${para(x.v, 'TL', 0)}"><i style="width:${Math.max(0, x.v / en * 100).toFixed(1)}%"></i></span><span class="dg">${para(x.v, 'TL', 0)}<small>${yuzde(net ? x.v / net * 100 : 0)}</small></span>`).join('')}</div>` : '<div class="text-muted" style="font-size:.7rem">Kayıt yok.</div>';
    };
    dagilim('#srGrupDagilim', k => k.grup, 'fGrup'); $('#srUlkeBaslik').textContent = PZ === 'Yurtiçi' ? 'Şehre Göre Net Satış' : 'Ülkeye Göre Net Satış'; if (PZ === 'Yurtiçi') dagilim('#srUlkeDagilim', k => k.b.sehir, ''); else dagilim('#srUlkeDagilim', k => k.b.ulke, 'ulke');
    // ilk 10 tablolar (hızlı süzgeçler uygulanır)
    const mini = (el, baslik, rows, ad, f, deger) => {
      $(el).innerHTML = `<thead><tr><th style="width:38%">${baslik}</th><th>Net Satış (₺)</th><th>Pay</th><th>Kâr (₺)</th><th>Marj</th></tr></thead><tbody>${rows.length ? rows.map(r => `<tr class="tik" data-f="${f}" data-v="${esc(deger(r))}"><td title="${esc(ad(r))}"><b>${esc(ad(r) || '—')}</b></td><td class="p">${para(r.netTl, 'TL', 0)}</td><td class="p">${yuzde(r.pay)}</td><td class="p${r.kar < 0 ? ' eksi' : ''}">${para(r.kar, 'TL', 0)}</td><td class="p">${yuzde(r.marj)}</td></tr>`).join('') : '<tr><td class="bos" colspan="5">Kayıt yok.</td></tr>'}</tbody>`;
    };
    const ilk = l => l.sort((a, b) => b.netTl - a.netTl).slice(0, 10), o = { satis: 1 };
    mini('#srOzetMusteri', 'Müşteri', ilk(ozetle(k => k.b.cariKod, k => ({ cariKod: k.b.cariKod, cari: k.b.cari }), o)), r => r.cari, 'musteri', r => r.cariKod);
    mini('#srOzetUrun', 'Ürün Adı', ilk(ozetle(k => k.kod, k => ({ kod: k.kod, ad: k.ad }), o)), r => r.ad, 'kalem', r => r.kod);
    mini('#srOzetTemsilci', 'Satış Temsilcisi', ilk(ozetle(k => k.b.temsilci, k => ({ temsilci: k.b.temsilci }), o)), r => r.temsilci, 'temsilci', r => r.temsilci);
    $('#srKapsam').textContent = belgeler.length + ' Belge';
  }
  $('#srOzet').addEventListener('click', e => {
    const k = e.target.closest('#srKpi > div'); if (k) { sekmeAc(k.dataset.git); return; }
    const d = e.target.closest('[data-f]'); if (!d) return; F[d.dataset.f] = [d.dataset.v]; suzDugmeYaz(); ciz();
  });

  // ---------- sekme şeridi, sağ işlem alanı ----------
  function islemYaz() {
    const el = $('#srIslem'), tn = SEK[sekme]; let h = '';
    if (tn.hizli || sekme === 'ozet') h += `<button type="button" class="sr-dgm${H.marj20 ? ' acik' : ''}" data-h="marj20" title="Yalnız marjı % 20'nin altında kalan satırlar">Marj &lt; % 20</button><button type="button" class="sr-dgm${H.ciro10 ? ' acik' : ''}" data-h="ciro10" title="Yalnız net satışı 10.000 ₺'yi aşan satırlar">Ciro &gt; 10.000 ₺</button>`;
    if (sekme === 'kirilim') h += `<span>Kırılım</span><select id="srKirilim" title="Satışın hangi alana göre özetleneceği">${Object.keys(KIRILIM).map(a => `<option value="${a}"${a === kirilim ? ' selected' : ''}>${KIRILIM[a][0]}</option>`).join('')}</select><span>Satıra tıklayın: o değere süzülür</span>`;
    if (sekme === 'siparis') h += `<a class="sr-dgm" href="/Rapor94" title="Satış Siparişi ekranı: siparişler buradan okunur"><i class="fas fa-cart-flatbed"></i>Satış Siparişi</a>`;
    if (sekme === 'belge' || sekme === 'siparis') h += `<span>Satıra tıklayın: kalem kırılımı</span>`;
    if (/^(musteri|urun|temsilci|sirket)$/.test(sekme)) h += `<span>Satıra tıklayın: ${sekme === 'urun' ? 'kalemleri' : 'belgeleri'}</span>`;
    el.innerHTML = h;
  }
  $('#srIslem').addEventListener('click', e => { const d = e.target.closest('[data-h]'); if (!d) return; H[d.dataset.h] = !H[d.dataset.h]; ciz(); });
  $('#srIslem').addEventListener('change', e => { if (e.target.id === 'srKirilim') { kirilim = e.target.value; durum('kirilim').suz = {}; ciz(); return; } });
  function ciz(baslik) {
    if (!V) return;   // açık seçim listesi kapatılmaz: çoklu seçimde liste açık kalır (dışarı tıklanınca / Esc ile kapanır)
    kok.querySelectorAll('#srSekmeler button[data-p]').forEach(b => b.classList.toggle('aktif', b.dataset.p === sekme));
    const ozet = sekme === 'ozet'; $('#srTabloKart').hidden = ozet; $('#srOzet').classList.toggle('aktif', ozet); $('#srSutunlar').style.display = SEK[sekme].kol ? '' : 'none';
    islemYaz();
    if (ozet) { cizOzet(); return; }
    if (baslik !== false) cizBaslik(); cizGovde(); yukseklik();
  }
  function sekmeAc(p) { if (!SEK[p]) p = 'belge'; sekme = p; kap.scrollTop = 0; ciz(); adresYaz(); }
  function adresYaz() { const q = []; if (PZ !== 'Yurtiçi') q.push('pazar=' + PZ_KOD[PZ]); if (sekme !== 'belge') q.push('sekme=' + sekme); history.replaceState(null, '', location.pathname + (q.length ? '?' + q.join('&') : '')); }
  // pazar değişince o pazarda karşılığı olmayan seçimler ve başlık süzgeçleri temizlenir (görünmeyen sütunda süzgeç kalmasın)
  $('#srPazar').addEventListener('click', e => { const b = e.target.closest('button[data-pz]'); if (!b || b.dataset.pz === PZ) return; PZ = b.dataset.pz; ['musteri', 'grup', 'ulke', 'sehir', 'pb', 'kalem', 'rol', 'urunSinifi', 'urunGrubu', 'urunSerisi'].forEach(a => F[a] = []); Object.values(DURUM).forEach(d => { d.suz = {}; d.sira = null; d.acik = new Set(); d.limit = 250; }); kap.scrollTop = 0; suzDugmeYaz(); ciz(); adresYaz(); });
  $('#srSekmeler').addEventListener('click', e => { const b = e.target.closest('button[data-p]'); if (b) sekmeAc(b.dataset.p); });

  // ---------- Sütunlar, Excel, Getir ----------
  $('#srSutunlar').addEventListener('click', e => {
    const tn = SEK[sekme]; if (!tn.kol) return; const gor = gorunenKol(sekme).map(c => c.k);
    popAc(e.currentTarget, tn.kol.map(c => ({ v: c.k, ad: c.ad })), gor, l => { const a = {}; tn.kol.forEach(c => a[c.k] = l.includes(c.k)); try { localStorage.setItem(kolAnahtar(sekme), JSON.stringify(a)); } catch (x) { } cizBaslik(); cizGovde(); }, { tumuDolu: 1, tumuAd: 'Tümünü Göster', bosAd: 'Sütun Seçin' });
  });
  $('#srExcel').addEventListener('click', async () => {
    const dg = $('#srExcel'), ad = SEK[sekme].ad, alt = $('#srDonemYazi').textContent + ' · ' + $('#srKapsam').textContent; dg.disabled = true;
    try {
      if (sekme === 'ozet') await TabloExcel.indir('Satis_Raporu_Ozet', [{ tablo: '#srOzetMusteri', ad: 'İlk 10 Müşteri', baslik: 'Konsolide Satış Raporu · Özet', altBaslik: alt, kpi: '#srKpi' }, { tablo: '#srOzetUrun', ad: 'İlk 10 Ürün', baslik: 'İlk 10 Ürün', altBaslik: alt }, { tablo: '#srOzetTemsilci', ad: 'Temsilciler', baslik: 'Temsilciler', altBaslik: alt }]);
      else {
        const Dr = durum(sekme), eski = Dr.limit, acik = Dr.acik; Dr.limit = Infinity; Dr.acik = new Set(); cizGovde();
        try { await TabloExcel.indir('Satis_Raporu_' + ad.replace(/[^\wığüşöçİĞÜŞÖÇ]+/g, '_'), [{ tablo: '#srTablo', ad, baslik: 'Konsolide Satış Raporu · ' + ad, altBaslik: alt }]); }
        finally { Dr.limit = eski; Dr.acik = acik; cizGovde(); }
      }
    } catch (x) { alert('Excel üretilemedi: ' + x.message); }
    dg.disabled = false;
  });
  async function getir() {
    const dg = $('#srGetir'); dg.disabled = true;
    try {
      const r = await fetch('/SatisRaporu/Veri', { headers: { 'X-Requested-With': 'XMLHttpRequest' } }); if (!r.ok) throw new Error('Sunucu yanıtı ' + r.status);
      const ilk = !V; V = await r.json(); hazirla();
      if (ilk) { $('#srDonem').value = 'yil'; donemAyarla('yil'); }
      suzDugmeYaz(); ciz();
    } catch (x) { tablo.tBodies[0].innerHTML = `<tr><td class="bos">Veri alınamadı: ${esc(x.message)}</td></tr>`; }
    dg.disabled = false;
  }
  $('#srGetir').addEventListener('click', getir);
  if (!SEK[sekme]) sekme = 'belge';
  getir();
})();
