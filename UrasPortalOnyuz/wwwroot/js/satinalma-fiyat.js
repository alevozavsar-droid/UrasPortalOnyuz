// ============================================================
// SATINALMA FİYAT ANALİZ RAPORU (Views/Rapor6/Index.cshtml, 23 Kas 2026) — veri: /UrunFiyatKarsilastirma/Veri (Data/SatinalmaFiyatOrnek.cs)
// Kullanıcı: "Satınalma Fiyat Analizi raporunu geliştirelim, temsilci seçelim, TL fiyat, dövizli fiyat, detaylı rapor hazırla."
// Her satır bir alış faturası kalemidir (malzeme alımı): belge para birimiyle birim fiyat + fatura günü kuruyla ₺ / $ / € birim fiyat ve tutar.
// Sekmeler: Malzeme Fiyat Analizi (son alım – önceki alım, ₺ ve döviz değişimi) · Tedarikçi Karşılaştırması (aynı malzemenin tedarikçileri, en ucuza göre fark)
//   · Aylık Fiyat Seyri (malzeme × ay ağırlıklı ortalama fiyat) · Alım Detayı (kalem kalem) | Temsilci · Tedarikçi · Kategori özetleri.
// Kurallar: süzgeçler ortak SuzgecCubuk (hepsi çoklu seçim; ilk süzgeç Satınalma Temsilcisi); her tabloda seçim kutusu + dipte Seçili ve Toplam; başlıkla sıralama;
//   tutar + para birimi bölünmez; sayılar tıklanır; Excel = ekrandaki tablo (.xlsx, TabloExcel).
// Tıklama (23 Kas 2026, kullanıcı: "tüm fiyatlara tıkladığımda o ürünün fatura incelemesini aç", "tıkladığımda o faturaya gidebileyim"):
//   malzeme kodu / adı / HER fiyat → Fatura İncelemesi penceresi (malzemenin bütün alış faturaları; tıklanan fiyatın faturası işaretli) — kodBag;
//   fatura tarihi / belge no → Fatura penceresi (o faturanın başlığı ve bütün kalemleri, Cari Ekstre ve Fatura ve Ödeme Takip bağlantıları) — fatBag.
// En Düşük / En Yüksek Fiyat (aynı istek): malzemenin DÖNEMDEKİ en düşük ve en yüksek ₺ birim fiyatlı alımı (eşitlikte en yeni fatura) — fatura tarihi, ₺ ve döviz fiyatı,
//   aradaki fark % = en yüksek ÷ en düşük − 1 (₺). Dövizde en düşük başka bir fatura olabilir; ölçü ₺ fiyatıdır.
// Geniş tablo (Malzeme Fiyat Analizi 30 sütun) dar ekranda yana kayar; seçim kutusu + kod + ad solda sabit kalır (T.yap, yapYaz).
// "Önceki alım", süzgeçlere uyan alımlar içinde son alımdan bir önceki alımdır (dönem dışında da olabilir); değişim ₺ ve seçili dövizde ayrı hesaplanır.
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('sr'); if (!kok || kok.dataset.rapor !== 'satinalma-fiyat') return;
  const $ = (s, k) => (k || kok).querySelector(s), $$ = (s, k) => Array.from((k || kok).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const TR = 'tr-TR', NB = ' ', kucuk = s => String(s ?? '').toLocaleLowerCase(TR);
  const say = (v, d = 0) => Number(v || 0).toLocaleString(TR, { minimumFractionDigits: d, maximumFractionDigits: d });
  const SIM = { TL: '₺', USD: '$', EUR: '€', GBP: '£' }, sim = pb => SIM[pb] || pb;
  const para = (v, pb) => say(v, 0) + NB + sim(pb);
  const fiyat = (v, pb) => v == null || !isFinite(v) ? '<span class="soluk">—</span>' : say(v, Math.abs(v) < 10 ? 4 : Math.abs(v) < 1000 ? 2 : 0) + NB + sim(pb);
  const mikt = (v, birim) => say(v, v % 1 ? 2 : 0) + NB + esc(birim);
  const mikS = v => say(v, v % 1 ? 2 : 0);
  const trh = iso => iso ? iso.slice(8, 10) + '.' + iso.slice(5, 7) + '.' + iso.slice(0, 4) : '';
  const topla = (l, f) => l.reduce((s, x) => s + (Number(f(x)) || 0), 0);
  const tekil = l => [...new Set(l)];
  const AY_AD = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];
  const sirali = (a, b) => a.tarih < b.tarih ? -1 : a.tarih > b.tarih ? 1 : a.id - b.id;
  const ESIK = 0.5;   // % — bunun altındaki fark "Değişmedi"
  const YON_RENK = { 'Arttı': 'art', 'Düştü': 'dus' };   // satır rengi (₺ fiyat yönü)
  const yonAd = d => d == null ? 'Tek Alım' : d > ESIK ? 'Arttı' : d < -ESIK ? 'Düştü' : 'Değişmedi';
  /** değişim göstergesi: ▲ % 12,4 (artış kırmızı) · ▼ % 3,1 (düşüş yeşil) · % 0,0 */
  const dg = d => d == null || !isFinite(d) ? '<span class="soluk">—</span>' : `<span class="sf-dg ${d > ESIK ? 'art' : d < -ESIK ? 'dus' : 'sabit'}">${d > ESIK ? '<i>▲</i>' : d < -ESIK ? '<i>▼</i>' : ''}%${NB}${say(Math.abs(d), 1)}</span>`;
  const agOrt = (l, deg, ag) => { let t = 0, a = 0; l.forEach(x => { const d = deg(x), w = ag(x); if (d != null && isFinite(d) && w > 0) { t += d * w; a += w; } }); return a ? t / a : null; };

  let V = null, SZ = null, DOVIZ = 'USD', SEKME = kok.dataset.sekme || 'malzeme', BAS = '', BIT = '', AYB = 'TL', COK = true;
  let TUM = [], DON = [], MLZ = new Map(), YON_SAY = {};
  const F = {}, DURUM = {};   // DURUM[sekme] = { sira: { k, yon }, secili: Set, limit }
  const fD = r => DOVIZ === 'USD' ? r.fUsd : r.fEur, tD = r => DOVIZ === 'USD' ? r.tUsd : r.tEur;
  const donemde = r => r.tarih >= BAS && r.tarih <= BIT;

  // ---------- süzgeçler ----------
  const AL = { temsilci: r => r.temsilci, sirket: r => r.sirket, cari: r => r.cari, kategori: r => r.kategori, pb: r => r.pb, bolge: r => r.bolge, ulke: r => r.ulke, sinif: r => r.sinif, iliski: r => r.iliski, kosul: r => r.kosul, malzeme: r => r.ad, birim: r => r.birim, islem: r => r.islem };
  const ALANLAR = [{ k: 'temsilci', ad: 'Satınalma Temsilcisi' }, { k: 'sirket', ad: 'Şirket', bos: 'Tümü (Konsolide)' }, { k: 'cari', ad: 'Tedarikçi' }, { k: 'kategori', ad: 'Kategori' }, { k: 'pb', ad: 'Para Birimi' },
    { k: 'bolge', ad: 'Bölge' }, { k: 'ulke', ad: 'Ülke' }, { k: 'sinif', ad: 'Cari Sınıfı' }, { k: 'iliski', ad: 'İlişki Durumu' }, { k: 'kosul', ad: 'Vade Şartı' }, { k: 'malzeme', ad: 'Malzeme' }, { k: 'birim', ad: 'Birim' }, { k: 'islem', ad: 'İşlem Tipi' }, { k: 'yon', ad: 'Fiyat Yönü (₺)' }];
  const araMetni = r => r._ara || (r._ara = kucuk([r.kod, r.ad, r.cari, r.unvan, r.cariKod, r.no, r.temsilci, r.kategori, r.sirket].join(' ')));
  /** satır süzgeçlere uyuyor mu (dönem ve Fiyat Yönü hariç; haric = atlanacak süzgeç — seçenek listesi için) */
  const uy = (r, haric) => { for (const k in AL) { if (k === haric) continue; const l = F[k]; if (l && l.length && !l.includes(AL[k](r))) return false; } return !F.ara || araMetni(r).includes(kucuk(F.ara)); };
  function secenekler(k) {
    if (k === 'yon') return ['Arttı', 'Düştü', 'Değişmedi', 'Tek Alım'].map(v => ({ v, n: YON_SAY[v] || 0 }));
    const say = new Map(); V.satirlar.forEach(r => { if (donemde(r) && uy(r, k)) { const v = AL[k](r); say.set(v, (say.get(v) || 0) + 1); } });
    return [...say].sort((a, b) => String(a[0]).localeCompare(String(b[0]), TR)).map(([v, n]) => ({ v, n }));
  }

  // ---------- dönem ----------
  const isoGun = d => d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
  function donemKur(v) {
    const b = new Date(V.bugun + 'T00:00:00'), y = b.getFullYear();
    if (v === 'tum') { BAS = V.satirlar.reduce((m, r) => r.tarih < m ? r.tarih : m, V.bugun); BIT = V.bugun; }
    else if (v === 'ay') { BAS = V.bugun.slice(0, 8) + '01'; BIT = V.bugun; }
    else if (v === 'yil') { BAS = y + '-01-01'; BIT = V.bugun; }
    else if (v === 'gecen') { BAS = (y - 1) + '-01-01'; BIT = (y - 1) + '-12-31'; }
    else if (v !== 'ozel') { const d = new Date(b); d.setMonth(d.getMonth() - (+v || 12)); d.setDate(d.getDate() + 1); BAS = isoGun(d); BIT = V.bugun; }
    $('#sfBas').value = BAS; $('#sfBit').value = BIT;
  }
  const donemYazi = () => trh(BAS) + ' – ' + trh(BIT);

  // ---------- özetleme: anahtara göre (malzeme · malzeme + tedarikçi) son / önceki alım ve dönem toplamları ----------
  function ozetle(satirlar, anahtar) {
    const g = new Map(); satirlar.forEach(r => { const k = anahtar(r); let o = g.get(k); if (!o) g.set(k, o = { a: k, l: [] }); o.l.push(r); });
    const out = new Map();
    g.forEach(o => {
      o.l.sort(sirali); const d = o.l.filter(donemde); if (!d.length) return;
      const son = d[d.length - 1], i = o.l.indexOf(son), onc = i > 0 ? o.l[i - 1] : null, mik = topla(d, r => r.miktar), tTl = topla(d, r => r.tTl), tDv = topla(d, tD);
      const dTl = onc && onc.fTl ? (son.fTl / onc.fTl - 1) * 100 : null, dDv = onc && fD(onc) ? (fD(son) / fD(onc) - 1) * 100 : null;
      // dönemin en düşük / en yüksek ₺ birim fiyatlı alımı (eşitlikte en yeni fatura) ve aralarındaki fark
      let enD = d[0], enY = d[0]; d.forEach(r => { if (r.fTl <= enD.fTl) enD = r; if (r.fTl >= enY.fTl) enY = r; });
      o.enD = enD; o.enY = enY; o.aralik = d.length > 1 && enD.fTl > 0 ? (enY.fTl / enD.fTl - 1) * 100 : null;
      Object.assign(o, { d, son, onc, kod: son.kod, ad: son.ad, kategori: son.kategori, birim: son.birim, n: d.length, miktar: mik, tTl, tDv, ortTl: mik ? tTl / mik : null, ortDv: mik ? tDv / mik : null,
        minDv: Math.min(...d.map(fD)), maxDv: Math.max(...d.map(fD)), dTl, dDv, yon: yonAd(dTl), yonDv: yonAd(dDv), temsilciler: tekil(d.map(r => r.temsilci)), tedarikciler: tekil(d.map(r => r.cariKod)), fat: new Set(d.map(r => r.cariKod + '|' + r.no)).size });
      out.set(o.a, o);
    });
    return out;
  }
  function hesapla() {
    const t0 = V.satirlar.filter(r => uy(r)); let st = ozetle(t0, r => r.kod);
    YON_SAY = {}; st.forEach(s => { YON_SAY[s.yon] = (YON_SAY[s.yon] || 0) + 1; });
    if (F.yon.length) { const izin = new Set([...st.values()].filter(s => F.yon.includes(s.yon)).map(s => s.kod)); TUM = t0.filter(r => izin.has(r.kod)); st = new Map([...st].filter(([k]) => izin.has(k))); } else TUM = t0;
    MLZ = st; DON = TUM.filter(donemde);
  }

  // ---------- ortak tablo çizici ----------
  // T = { kol: [{ k, ad, g (küme adı), gc (küme sınıfı), w (ağırlık), s: r => sıralama değeri, h: r => html, c: sınıf | r => sınıf, top: l => html, tip, f: genel süzgeç anahtarı, m: r => arama metni }],
  //       satir: [], anahtar: r => anahtar, dipAd: l => 'Toplam · …', sira: { k, yon }, renk: r => 'art' | 'dus' | '', bos }
  // Başlık (Nakit Akış Planlama düzeni): [küme satırı] + büyük harf başlık satırı (tıklayınca sıralama) + süzgeç satırı — seçenekli sütunda (kol.f) çoklu seçim düğmesi
  // genel süzgeci sürer (üstteki süzgeç çubuğuyla aynı durum), öbür sütunlarda "Ara…" kutusu yalnız o sekmenin tablosunu süzer (D.ara). İlk hücre: seçim kutusu + sıra no.
  const hucreMetni = (c, r) => kucuk(c.m ? c.m(r) : String(c.h(r)).replace(/<[^>]*>/g, ' ')).replace(/ /g, ' ').replace(/\s+/g, ' ').trim();
  const suzYazi = k => F[k].length === 0 ? 'Tümü' : F[k].length === 1 ? F[k][0] : F[k].length + ' Seçili';
  function tabloYaz(tablo, T, D, opt) {
    opt = opt || {}; const sec = opt.sec !== false, suz = opt.suz !== false, kol = T.kol; tablo.classList.add('sf-olc');
    if (!D.sira || !kol.some(c => c.k === D.sira.k)) D.sira = { ...T.sira };
    if (!D.ara) D.ara = {}; if (!D.secili) D.secili = new Set();
    // T.yap: yana kaydırırken solda sabit kalan ilk sütun sayısı (seçim kutusu + kod + ad); konumlar yapYaz'da ölçülür
    const yp0 = T.yap ? ' yp yp0' : '';
    kol.forEach((c, i) => { c.gb = !!c.g && (i === 0 || kol[i - 1].g !== c.g); c.yp = T.yap && i < T.yap ? ` yp yp${i + 1}${i === T.yap - 1 ? ' yp-son' : ''}` : ''; });
    const wt = topla(kol, c => c.w) + (sec ? 1.9 : 0);
    tablo.querySelector('colgroup').innerHTML = (sec ? `<col style="width:${(1.9 / wt * 100).toFixed(3)}%">` : '') + kol.map(c => `<col style="width:${(c.w / wt * 100).toFixed(3)}%">`).join('');
    let ust = '';
    if (kol.some(c => c.g)) { ust = '<tr class="ustgrup">' + (sec ? `<th class="bos${yp0}"></th>` : ''); for (let i = 0; i < kol.length;) { if (kol[i].yp && !kol[i].g) { ust += `<th class="bos${kol[i].yp}"></th>`; i++; continue; } let j = i; while (j < kol.length && (kol[j].g || '') === (kol[i].g || '')) j++; ust += kol[i].g ? `<th class="ug gb ${kol[i].gc || 'ug-d'}" colspan="${j - i}">${esc(kol[i].g)}</th>` : `<th class="bos" colspan="${j - i}"></th>`; i = j; } ust += '</tr>'; }
    const suzSatir = !suz ? '' : '<tr class="suzgec">' + (sec ? `<th class="sec${yp0}"></th>` : '') + kol.map(c => `<th class="${c.gb ? 'gb' : ''}${c.yp}">${c.f ? `<button type="button" class="sf-cs${F[c.f].length ? ' dolu' : ''}" data-f="${c.f}" title="${esc(c.adDuz || c.ad)}: ${esc(F[c.f].length ? F[c.f].join(', ') : 'Tümü')}">${esc(suzYazi(c.f))}</button>` : c.s ? `<input type="search" data-ara="${c.k}" placeholder="Ara…" autocomplete="off" value="${esc(D.ara[c.k] || '')}"${D.ara[c.k] ? ' class="dolu"' : ''}>` : ''}</th>`).join('') + '</tr>';
    tablo.tHead.innerHTML = ust + '<tr class="bas">' + (sec ? `<th class="sec ik${yp0}"><input type="checkbox" title="Görünen satırların hepsini seç / bırak"></th>` : '')
      + kol.map(c => `<th data-k="${c.k}" class="${c.s ? 'sira ' : 'ik '}${c.gb ? 'gb ' : ''}${D.sira.k === c.k ? (D.sira.yon > 0 ? 'art' : 'azal') : ''}${c.yp}"${c.tip ? ` title="${esc(c.tip)}"` : ''}>${c.ad}</th>`).join('') + '</tr>' + suzSatir;
    tablo._sf = { T, D, opt };
    govdeYaz(tablo);
    if (tablo._sfBagli) return; tablo._sfBagli = true;
    tablo.addEventListener('click', e => {
      const s = tablo._sf, cs = e.target.closest('.sf-cs[data-f]');
      if (cs) { const k = cs.dataset.f, secn = secenekler(k); F[k].forEach(v => { if (!secn.some(x => x.v === v)) secn.push({ v, n: 0 }); }); SuzgecCubuk.popAc(cs, secn, F[k], l => { F[k] = l; ciz(); }, { bosAd: 'Tümü' }); return; }
      const th = e.target.closest('thead tr.bas th[data-k]');
      if (th && !e.target.closest('input')) { const c = s.T.kol.find(x => x.k === th.dataset.k); if (!c || !c.s) return; s.D.sira = s.D.sira.k === c.k ? { k: c.k, yon: -s.D.sira.yon } : { k: c.k, yon: c.ilkYon || 1 }; tabloYaz(tablo, s.T, s.D, s.opt); return; }
      if (e.target.closest('[data-tum]')) { s.D.limit = 1e9; govdeYaz(tablo); return; }
      const fat = e.target.closest('[data-fat]'); if (fat) { e.preventDefault(); faturaAc(fat.dataset.fat); return; }
      const kod = e.target.closest('[data-kod]'); if (kod) { e.preventDefault(); gecmisAc(kod.dataset.kod, kod.dataset.vurgu); return; }
      const sz = e.target.closest('[data-suz]'); if (sz) { e.preventDefault(); SZ.ayarla(sz.dataset.suz, [sz.dataset.deger]); sekmeAc('malzeme'); }
    });
    let bekle; tablo.addEventListener('input', e => { const i = e.target.closest('input[data-ara]'); if (!i) return; const s = tablo._sf; s.D.ara[i.dataset.ara] = i.value.trim(); i.classList.toggle('dolu', !!i.value.trim()); clearTimeout(bekle); bekle = setTimeout(() => govdeYaz(tablo), 160); });
    tablo.addEventListener('change', e => {
      if (e.target.type !== 'checkbox') return; const s = tablo._sf, secili = s.D.secili;
      if (e.target.closest('thead')) s.gor.forEach(r => { const a = String(s.T.anahtar(r)); if (e.target.checked) secili.add(a); else secili.delete(a); });
      else { const tr = e.target.closest('tr[data-a]'); if (!tr) return; if (e.target.checked) secili.add(tr.dataset.a); else secili.delete(tr.dataset.a); }
      govdeYaz(tablo);
    });
  }
  /** gövde + dip: sütun aramaları, sıralama, seçim; başlık yeniden çizilmez (arama kutusunda yazarken odak kaybolmaz) */
  function govdeYaz(tablo) {
    const s = tablo._sf, T = s.T, D = s.D, opt = s.opt, sec = opt.sec !== false, kol = T.kol, secili = D.secili;
    const aranan = Object.keys(D.ara).filter(k => D.ara[k]).map(k => [kol.find(c => c.k === k), kucuk(D.ara[k])]).filter(x => x[0]);
    const l = aranan.length ? T.satir.filter(r => aranan.every(([c, q]) => hucreMetni(c, r).includes(q))) : T.satir.slice();
    const sk = kol.find(c => c.k === D.sira.k), yon = D.sira.yon;
    if (sk && sk.s) l.sort((a, b) => { const x = sk.s(a), y = sk.s(b); if (x == null || x === '') return y == null || y === '' ? 0 : 1; if (y == null || y === '') return -1; return (typeof x === 'number' && typeof y === 'number' ? x - y : String(x).localeCompare(String(y), TR)) * yon; });
    const limit = D.limit || 500, gor = l.slice(0, limit), n = kol.length + (sec ? 1 : 0);
    const hepsi = new Set(T.satir.map(r => String(T.anahtar(r)))); [...secili].forEach(a => { if (!hepsi.has(a)) secili.delete(a); });
    const sinif = (c, r) => (typeof c.c === 'function' ? c.c(r) : c.c || '') + (c.gb ? ' gb' : '') + (c.yp || '');
    tablo.tBodies[0].innerHTML = gor.map((r, i) => { const a = String(T.anahtar(r)), sv = sec && secili.has(a), rn = T.renk ? T.renk(r) : ''; return `<tr data-a="${esc(a)}" class="${rn}${sv ? ' secili' : ''}">${sec ? `<td class="sec${T.yap ? ' yp yp0' : ''}"><input type="checkbox"${sv ? ' checked' : ''}><small>${i + 1}</small></td>` : ''}${kol.map(c => `<td class="${sinif(c, r)}">${c.h(r)}</td>`).join('')}</tr>`; }).join('')
      + (l.length > gor.length ? `<tr><td class="dahafazla" colspan="${n}"><a data-tum>Tümünü Göster · ${say(l.length - gor.length)} Satır Daha</a></td></tr>` : '')
      || `<tr><td class="bos" colspan="${n}">${esc(T.bos || 'Süzgeçlere Uyan Kayıt Yok')}</td></tr>`;
    const ilk = kol.findIndex(c => c.top), bas = ilk < 0 ? kol.length : ilk;
    const satir = (liste, ad, cls) => `<tr class="${cls}"><td colspan="${bas + (sec ? 1 : 0)}"><span class="dip-ad">${ad}</span></td>${kol.slice(bas).map(c => `<td class="sy${c.gb ? ' gb' : ''}">${c.top ? c.top(liste) : ''}</td>`).join('')}</tr>`;
    const sl = sec ? l.filter(r => secili.has(String(T.anahtar(r)))) : [];
    tablo.tFoot.innerHTML = l.length ? (sl.length ? satir(sl, `Seçili · ${say(sl.length)}`, 'sf-secili-dip') : '') + satir(l, T.dipAd ? T.dipAd(l) : `Toplam · ${say(l.length)}`, 'sf-toplam') : '';
    const tum = tablo.tHead.querySelector('th.sec input'); if (tum) { tum.checked = gor.length > 0 && gor.every(r => secili.has(String(T.anahtar(r)))); tum.indeterminate = !tum.checked && sl.length > 0; }
    s.gor = gor; s.liste = l; s.seciliSatir = sl;
    if (opt.sonra) opt.sonra(l, sl);
  }
  /** dip ve üst başlık yükseklikleri (yapışkan satırların konumu); yazı oranı değişince yeniden ölçülür */
  function olcuYaz(tablo) { const tr = tablo.tFoot && tablo.tFoot.querySelector('tr.sf-toplam'), ug = tablo.tHead && tablo.tHead.querySelector('tr.ustgrup'); tablo.style.setProperty('--sf-dip', (tr ? tr.offsetHeight : 0) + 'px'); tablo.style.setProperty('--sr-ust', (ug ? ug.offsetHeight : 0) + 'px'); }
  /** GENİŞ EKRANDA BÜYÜYEN YAZI (kullanıcı: "sayfanın yarısı kullanılmıyor, verimli kullan"): tablo, sığdığı en dar genişlikten (minGen) ne kadar genişse yazı o oranda büyür (1 – ust); hiçbir durumda küçülmez */
  function olcekle(tablo, kap, minGen, ust) { const g = kap.getBoundingClientRect().width - 18, o = Math.max(1, Math.min(ust || 1.4, g / (minGen * 1.02))); tablo.style.setProperty('--sf-o', o.toFixed(3)); return o; }
  const tekBirim = (l, birim, mik) => { const b = tekil(l.map(birim)); return b.length === 1 ? mikt(topla(l, mik), b[0]) : '<span class="soluk">—</span>'; };
  /** solda sabit kalan sütunların konumu (yana kayan geniş tabloda seçim kutusu + kod + ad yerinde kalır) */
  function yapYaz(tablo) { const s = tablo._sf; if (!s || !s.T.yap) return; const th = tablo.tHead.querySelectorAll('tr.bas th'); let x = 0; for (let i = 0; i <= s.T.yap && i < th.length; i++) { tablo.style.setProperty('--yp' + i, x.toFixed(1) + 'px'); x += th[i].getBoundingClientRect().width; } }
  /** malzemenin Fatura İncelemesi penceresini açan bağlantı (kod, ad, her fiyat); id verilirse o fatura satırı pencerede işaretlenir */
  const kodBag = (kod, ic, id) => `<a class="sf-tik" data-kod="${esc(kod)}"${id ? ` data-vurgu="${id}"` : ''} title="Fatura İncelemesi">${ic}</a>`;
  /** faturanın kendisini açan bağlantı (fatura tarihi, belge no) */
  const fatBag = (r, ic) => `<a class="sf-tik" data-fat="${r.id}" title="Faturayı Aç · ${esc(r.no)}">${ic}</a>`;
  const YOK = '<span class="soluk">—</span>';
  const dvAd = () => sim(DOVIZ);

  // ---------- sekmeler ----------
  const SEKMELER = {
    // Malzeme Fiyat Analizi: malzeme başına son alım – önceki alım (₺ ve döviz), dönem ortalaması ve tutarı
    malzeme() {
      const dv = dvAd(), l = [...MLZ.values()];
      const enKol = (alan, g, gc, ne) => [
        { k: alan + 'Tarih', g, gc, ad: 'Fatura Tarihi', w: 6.2, tip: `Dönemde ${ne} ₺ birim fiyatlı alımın faturası — tıklayınca fatura açılır`, s: s => s[alan].tarih, ilkYon: -1, h: s => fatBag(s[alan], trh(s[alan].tarih)) },
        { k: alan + 'Tl', g, gc, ad: 'Fiyat (₺)', w: 5.8, c: 'sy', tip: `Dönemde ${ne} ₺ birim fiyat`, s: s => s[alan].fTl, ilkYon: -1, h: s => kodBag(s.kod, fiyat(s[alan].fTl, 'TL'), s[alan].id) },
        { k: alan + 'Dv', g, gc, ad: `Fiyat (${dv})`, w: 5.4, c: 'sy', tip: 'Aynı faturanın döviz birim fiyatı (fatura günü TCMB kuruyla)', s: s => fD(s[alan]), ilkYon: -1, h: s => kodBag(s.kod, fiyat(fD(s[alan]), DOVIZ), s[alan].id) }];
      const T = { minGen: 2230, yap: 2, renk: s => YON_RENK[s.yon] || '', tl: s => s.tTl, dv: s => s.tDv, satir: l, anahtar: s => s.kod, sira: { k: 'tTl', yon: -1 }, dipAd: x => `Toplam · ${say(x.length)} Malzeme`, birim: 'Malzeme', kol: [
        { k: 'kod', ad: 'Malzeme Kodu', w: 7.4, s: s => s.kod, h: s => kodBag(s.kod, `<b>${esc(s.kod)}</b>`) },
        { k: 'ad', ad: 'Malzeme', w: 9.6, c: 'yz', s: s => s.ad, h: s => kodBag(s.kod, esc(s.ad)) },
        { k: 'kategori', ad: 'Kategori', w: 5.6, c: 'yz', f: 'kategori', s: s => s.kategori, h: s => esc(s.kategori) },
        { k: 'birim', ad: 'Birim', w: 4.2, c: 'yz', f: 'birim', s: s => s.birim, h: s => esc(s.birim) },
        { k: 'temsilci', ad: 'Satınalma Temsilcisi', w: 5.8, c: 'yz', s: s => s.temsilciler.join(', '), h: s => esc(s.temsilciler.join(', ')) },
        { k: 'sonTarih', g: 'Son Alım', gc: 'ug-f', ad: 'Tarih', w: 6.2, s: s => s.son.tarih, ilkYon: -1, h: s => fatBag(s.son, trh(s.son.tarih)) },
        { k: 'sonTed', g: 'Son Alım', gc: 'ug-f', ad: 'Tedarikçi', w: 7, c: 'yz', s: s => s.son.cari, h: s => `<span title="${esc(s.son.unvan)}">${esc(s.son.cari)}</span>` },
        { k: 'sonBelge', g: 'Son Alım', gc: 'ug-f', ad: 'Belge Fiyatı', w: 6, c: 'sy', tip: 'Faturadaki birim fiyat, faturanın para birimiyle', s: s => s.son.fTl, ilkYon: -1, h: s => kodBag(s.kod, fiyat(s.son.fiyat, s.son.pb), s.son.id) },
        { k: 'sonTl', g: 'Son Alım', gc: 'ug-f', ad: 'Fiyat (₺)', w: 5.8, c: 'sy kalin', s: s => s.son.fTl, ilkYon: -1, h: s => kodBag(s.kod, fiyat(s.son.fTl, 'TL'), s.son.id) },
        { k: 'sonDv', g: 'Son Alım', gc: 'ug-f', ad: `Fiyat (${dv})`, w: 5.4, c: 'sy kalin', tip: 'Fatura günü TCMB kuruyla', s: s => fD(s.son), ilkYon: -1, h: s => kodBag(s.kod, fiyat(fD(s.son), DOVIZ), s.son.id) },
        { k: 'oncTarih', g: 'Önceki Alım', gc: 'ug-d', ad: 'Tarih', w: 6.2, s: s => s.onc && s.onc.tarih, ilkYon: -1, h: s => s.onc ? fatBag(s.onc, trh(s.onc.tarih)) : '<span class="soluk">—</span>' },
        { k: 'oncTed', g: 'Önceki Alım', gc: 'ug-d', ad: 'Tedarikçi', w: 7, c: 'yz', s: s => s.onc && s.onc.cari, h: s => s.onc ? esc(s.onc.cari) : '<span class="soluk">—</span>' },
        { k: 'oncTl', g: 'Önceki Alım', gc: 'ug-d', ad: 'Fiyat (₺)', w: 5.6, c: 'sy', s: s => s.onc && s.onc.fTl, ilkYon: -1, h: s => s.onc ? kodBag(s.kod, fiyat(s.onc.fTl, 'TL'), s.onc.id) : '<span class="soluk">—</span>' },
        { k: 'oncDv', g: 'Önceki Alım', gc: 'ug-d', ad: `Fiyat (${dv})`, w: 5.4, c: 'sy', s: s => s.onc && fD(s.onc), ilkYon: -1, h: s => s.onc ? kodBag(s.kod, fiyat(fD(s.onc), DOVIZ), s.onc.id) : '<span class="soluk">—</span>' },
        { k: 'dTl', g: 'Değişim', gc: 'ug-x', ad: '₺', w: 5, tip: 'Son alım – önceki alım, ₺ birim fiyat', s: s => s.dTl, ilkYon: -1, h: s => dg(s.dTl), top: x => dg(agOrt(x, s => s.dTl, s => s.tTl)) },
        { k: 'dDv', g: 'Değişim', gc: 'ug-x', ad: dv, w: 5, tip: 'Son alım – önceki alım, döviz birim fiyat', s: s => s.dDv, ilkYon: -1, h: s => dg(s.dDv), top: x => dg(agOrt(x, s => s.dDv, s => s.tTl)) },
        ...enKol('enD', 'En Düşük Fiyat', 'ug-k', 'en düşük'),
        ...enKol('enY', 'En Yüksek Fiyat', 'ug-y', 'en yüksek'),
        { k: 'aralik', g: 'Fark', gc: 'ug-x', ad: 'Düşük – Yüksek', w: 6.4, c: 'sy', tip: 'Dönemin en yüksek ₺ birim fiyatı, en düşük ₺ birim fiyatından yüzde kaç fazla', s: s => s.aralik, ilkYon: -1, h: s => s.aralik == null ? YOK : kodBag(s.kod, `%${NB}${say(s.aralik, 1)}`) },
        { k: 'n', g: 'Dönem', gc: 'ug-m', ad: 'Alım', w: 3.2, c: 'sy', s: s => s.n, ilkYon: -1, h: s => kodBag(s.kod, say(s.n)), top: x => say(topla(x, s => s.n)) },
        { k: 'miktar', g: 'Dönem', gc: 'ug-m', ad: 'Miktar', w: 6, c: 'sy', s: s => s.miktar, ilkYon: -1, h: s => mikS(s.miktar), top: x => tekBirim(x, s => s.birim, s => s.miktar) },
        { k: 'ortTl', g: 'Dönem', gc: 'ug-m', ad: 'Ortalama (₺)', w: 5.6, c: 'sy', tip: 'Dönem alımlarının miktar ağırlıklı ortalama birim fiyatı', s: s => s.ortTl, ilkYon: -1, h: s => kodBag(s.kod, fiyat(s.ortTl, 'TL')), top: () => '' },
        { k: 'ortDv', g: 'Dönem', gc: 'ug-m', ad: `Ortalama (${dv})`, w: 5.4, c: 'sy', s: s => s.ortDv, ilkYon: -1, h: s => kodBag(s.kod, fiyat(s.ortDv, DOVIZ)), top: () => '' },
        { k: 'tTl', g: 'Dönem', gc: 'ug-m', ad: 'Tutar (₺)', w: 7.4, c: 'sy kalin', s: s => s.tTl, ilkYon: -1, h: s => para(s.tTl, 'TL'), top: x => para(topla(x, s => s.tTl), 'TL') },
        { k: 'tDv', g: 'Dönem', gc: 'ug-m', ad: `Tutar (${dv})`, w: 6.4, c: 'sy', s: s => s.tDv, ilkYon: -1, h: s => para(s.tDv, DOVIZ), top: x => para(topla(x, s => s.tDv), DOVIZ) }] };
      // genişlik bütçesi tek yerde (ölçüm: 1 oranında gereken px ÷ 13): 30 sütun, kesilen yazı olmadan 2200 px
      const W = { kod: 7.4, ad: 9.2, kategori: 6.2, birim: 4.2, temsilci: 6.9, sonTarih: 6.2, sonTed: 6.9, sonBelge: 5.2, sonTl: 5.1, sonDv: 5.1, oncTarih: 6.2, oncTed: 6.9, oncTl: 5.1, oncDv: 5.1, dTl: 5.6, dDv: 5.6,
        enDTarih: 6.2, enDTl: 5.1, enDDv: 5.1, enYTarih: 6.2, enYTl: 5.1, enYDv: 5.1, aralik: 5.5, n: 3.9, miktar: 5.2, ortTl: 6.6, ortDv: 6.6, tTl: 7.1, tDv: 5.8 };
      T.kol.forEach(c => { if (W[c.k]) c.w = W[c.k]; });
      return T;
    },
    // Tedarikçi Karşılaştırması: malzeme + tedarikçi; son döviz fiyatı en düşük tedarikçiye göre fark
    tedarikci() {
      const dv = dvAd(), st = [...ozetle(TUM, r => r.kod + '|' + r.cariKod).values()], g = new Map();
      st.forEach(s => { let o = g.get(s.kod); if (!o) g.set(s.kod, o = { n: 0, min: Infinity, t: 0 }); o.n++; o.t += s.tTl; o.min = Math.min(o.min, fD(s.son)); });
      st.forEach(s => { const o = g.get(s.kod); s.tedSay = o.n; s.fark = o.n > 1 && o.min > 0 ? (fD(s.son) / o.min - 1) * 100 : null; s.ucuz = o.n > 1 && fD(s.son) <= o.min; s.pay = o.t ? s.tTl / o.t * 100 : 0; });
      const l = (COK ? st.filter(s => s.tedSay > 1) : st).sort((a, b) => a.kod.localeCompare(b.kod) || fD(a.son) - fD(b.son));
      return { minGen: 1620, renk: s => YON_RENK[s.yon] || '', tl: s => s.tTl, dv: s => s.tDv, satir: l, anahtar: s => s.a, sira: { k: 'sira', yon: 1 }, dipAd: x => `Toplam · ${say(tekil(x.map(s => s.kod)).length)} Malzeme · ${say(tekil(x.map(s => s.son.cariKod)).length)} Tedarikçi`, birim: 'Satır', bos: COK ? 'Birden Çok Tedarikçiden Alınan Malzeme Yok' : '',
        islem: `<button type="button" class="sr-dgm${COK ? ' acik' : ''}" data-i="cok" title="Yalnız birden çok tedarikçiden alınan malzemeler"><i class="fas fa-code-compare"></i>Birden Çok Tedarikçili</button>`, kol: [
        { k: 'sira', ad: 'Malzeme Kodu', w: 6.4, s: s => s.kod + '|' + String(Math.round(fD(s.son) * 1e4)).padStart(14, '0'), h: s => kodBag(s.kod, `<b>${esc(s.kod)}</b>`) },
        { k: 'ad', ad: 'Malzeme', w: 10.3, c: 'yz', s: s => s.ad, h: s => kodBag(s.kod, esc(s.ad)) },
        { k: 'birim', ad: 'Birim', w: 4, c: 'yz', f: 'birim', s: s => s.birim, h: s => esc(s.birim) },
        { k: 'cari', ad: 'Tedarikçi', w: 9.5, c: 'yz', f: 'cari', s: s => s.son.cari, h: s => `<b title="${esc(s.son.unvan)}">${esc(s.son.cari)}</b>` },
        { k: 'temsilci', ad: 'Satınalma Temsilcisi', w: 6.8, c: 'yz', f: 'temsilci', s: s => s.son.temsilci, h: s => esc(s.son.temsilci) },
        { k: 'bolge', ad: 'Bölge', w: 4.6, f: 'bolge', s: s => s.son.bolge, h: s => esc(s.son.bolge) },
        { k: 'sonTarih', g: 'Son Alım', gc: 'ug-f', ad: 'Tarih', w: 5.6, s: s => s.son.tarih, ilkYon: -1, h: s => fatBag(s.son, trh(s.son.tarih)) },
        { k: 'sonBelge', g: 'Son Alım', gc: 'ug-f', ad: 'Belge Fiyatı', w: 5.8, c: 'sy', s: s => s.son.fTl, ilkYon: -1, h: s => kodBag(s.kod, fiyat(s.son.fiyat, s.son.pb), s.son.id) },
        { k: 'sonTl', g: 'Son Alım', gc: 'ug-f', ad: 'Fiyat (₺)', w: 5.6, c: 'sy kalin', s: s => s.son.fTl, ilkYon: -1, h: s => kodBag(s.kod, fiyat(s.son.fTl, 'TL'), s.son.id) },
        { k: 'sonDv', g: 'Son Alım', gc: 'ug-f', ad: `Fiyat (${dv})`, w: 5.6, c: 'sy kalin', s: s => fD(s.son), ilkYon: -1, h: s => kodBag(s.kod, fiyat(fD(s.son), DOVIZ), s.son.id) },
        { k: 'fark', g: 'Karşılaştırma', gc: 'ug-x', ad: `En Ucuza Göre (${dv})`, w: 6.6, tip: 'Son döviz fiyatı, aynı malzemenin en ucuz tedarikçisine göre', s: s => s.fark, ilkYon: -1, h: s => s.tedSay < 2 ? '<span class="soluk">Tek Tedarikçi</span>' : s.ucuz ? '<span class="rz ucuz">En Ucuz</span>' : dg(s.fark) },
        { k: 'pay', g: 'Karşılaştırma', gc: 'ug-x', ad: 'Alım Payı', w: 7.9, tip: 'Malzemenin dönem alım tutarı içinde bu tedarikçinin payı', s: s => s.pay, ilkYon: -1, h: s => `<span class="sf-pay"><span class="yol"><i style="width:${s.pay.toFixed(1)}%"></i></span><b>%${NB}${say(s.pay, 1)}</b></span>` },
        { k: 'n', g: 'Dönem', gc: 'ug-m', ad: 'Alım', w: 3.6, c: 'sy', s: s => s.n, ilkYon: -1, h: s => say(s.n), top: x => say(topla(x, s => s.n)) },
        { k: 'miktar', g: 'Dönem', gc: 'ug-m', ad: 'Miktar', w: 5.8, c: 'sy', s: s => s.miktar, ilkYon: -1, h: s => mikS(s.miktar), top: x => tekBirim(x, s => s.birim, s => s.miktar) },
        { k: 'ortTl', g: 'Dönem', gc: 'ug-m', ad: 'Ortalama (₺)', w: 6.2, c: 'sy', s: s => s.ortTl, ilkYon: -1, h: s => kodBag(s.kod, fiyat(s.ortTl, 'TL')), top: () => '' },
        { k: 'ortDv', g: 'Dönem', gc: 'ug-m', ad: `Ortalama (${dv})`, w: 6.2, c: 'sy', s: s => s.ortDv, ilkYon: -1, h: s => kodBag(s.kod, fiyat(s.ortDv, DOVIZ)), top: () => '' },
        { k: 'tTl', g: 'Dönem', gc: 'ug-m', ad: 'Tutar (₺)', w: 6.8, c: 'sy kalin', s: s => s.tTl, ilkYon: -1, h: s => para(s.tTl, 'TL'), top: x => para(topla(x, s => s.tTl), 'TL') },
        { k: 'tDv', g: 'Dönem', gc: 'ug-m', ad: `Tutar (${dv})`, w: 6.2, c: 'sy', s: s => s.tDv, ilkYon: -1, h: s => para(s.tDv, DOVIZ), top: x => para(topla(x, s => s.tDv), DOVIZ) }] };
    },
    // Aylık Fiyat Seyri: malzeme × ay, miktar ağırlıklı ortalama birim fiyat (₺ / $ / €)
    aylik() {
      const ay = []; { const d = new Date(BAS.slice(0, 7) + '-01T00:00:00'), son = BIT.slice(0, 7); for (let i = 0; i < 240; i++) { const ym = isoGun(d).slice(0, 7); ay.push(ym); if (ym >= son) break; d.setMonth(d.getMonth() + 1); } }
      const aylar = ay.slice(-13), aySet = new Set(aylar), tA = r => AYB === 'TL' ? r.tTl : AYB === 'USD' ? r.tUsd : r.tEur, g = new Map();
      DON.forEach(r => { let o = g.get(r.kod); if (!o) g.set(r.kod, o = { kod: r.kod, ad: r.ad, birim: r.birim, kategori: r.kategori, ay: {}, tTl: 0, tDv: 0 }); const ym = r.tarih.slice(0, 7), h = o.ay[ym] || (o.ay[ym] = { t: 0, m: 0 }); h.t += tA(r); h.m += r.miktar; if (aySet.has(ym)) { o.tTl += r.tTl; o.tDv += tD(r); } });
      const l = [...g.values()]; l.forEach(o => { let onc = null; o.dolu = []; aylar.forEach(ym => { const h = o.ay[ym]; if (!h || !h.m) return; h.f = h.t / h.m; h.d = onc ? (h.f / onc - 1) * 100 : null; onc = h.f; o.dolu.push(h); }); o.ilk = o.dolu.length ? o.dolu[0].f : null; o.son = o.dolu.length ? o.dolu[o.dolu.length - 1].f : null; o.deg = o.dolu.length > 1 && o.ilk ? (o.son / o.ilk - 1) * 100 : null; o.tutar = topla(o.dolu, h => h.t); });
      const gor = l.filter(o => o.dolu.length);
      const hucre = (o, ym) => { const h = o.ay[ym]; return !h || !h.m ? '<span class="soluk">—</span>' : `<span class="sf-ay">${kodBag(o.kod, fiyat(h.f, AYB))}${h.d != null && Math.abs(h.d) > ESIK ? `<i class="${h.d > 0 ? 'art' : 'dus'}" title="Önceki alım ayına göre %${NB}${say(Math.abs(h.d), 1)} ${h.d > 0 ? 'artış' : 'düşüş'}">${h.d > 0 ? '▲' : '▼'}</i>` : ''}</span>`; };
      return { minGen: 1640, renk: o => o.deg == null ? '' : o.deg > ESIK ? 'art' : o.deg < -ESIK ? 'dus' : '', tl: o => o.tTl, dv: o => o.tDv, satir: gor, anahtar: o => o.kod, sira: { k: 'tutar', yon: -1 }, dipAd: x => `Toplam Alım Tutarı · ${say(x.length)} Malzeme`, birim: 'Malzeme', kapsam: ay.length > aylar.length ? `Son ${aylar.length} Ay · ${AY_AD[+aylar[0].slice(5) - 1]} ${aylar[0].slice(0, 4)} – ${AY_AD[+aylar[aylar.length - 1].slice(5) - 1]} ${aylar[aylar.length - 1].slice(0, 4)}` : '',
        islem: `<span class="sf-secim" data-i="ayb" title="Aylık ortalama fiyatın para birimi"><span class="e">Fiyat</span>${[['TL', '₺ TL'], ['USD', '$ USD'], ['EUR', '€ EUR']].map(([v, a]) => `<button type="button" data-v="${v}"${AYB === v ? ' class="aktif"' : ''}>${a}</button>`).join('')}</span>`, kol: [
        { k: 'kod', ad: 'Malzeme Kodu', w: 6.8, s: o => o.kod, h: o => kodBag(o.kod, `<b>${esc(o.kod)}</b>`) },
        { k: 'ad', ad: 'Malzeme', w: 10, c: 'yz', s: o => o.ad, h: o => kodBag(o.kod, esc(o.ad)) },
        { k: 'birim', ad: 'Birim', w: 4.8, c: 'yz', f: 'birim', s: o => o.birim, h: o => esc(o.birim) },
        ...aylar.map(ym => ({ k: 'a' + ym, g: ym.slice(0, 4), gc: 'ug-d', ad: AY_AD[+ym.slice(5) - 1], w: 5.3, c: 'sy', s: o => o.ay[ym] && o.ay[ym].f, ilkYon: -1, h: o => hucre(o, ym), top: x => { const t = topla(x, o => o.ay[ym] ? o.ay[ym].t : 0); return t ? para(t, AYB) : '<span class="soluk">—</span>'; } })),
        { k: 'ilk', g: 'Dönem', gc: 'ug-m', ad: 'İlk Ay', w: 5.4, c: 'sy', s: o => o.ilk, ilkYon: -1, h: o => kodBag(o.kod, fiyat(o.ilk, AYB)), top: () => '' },
        { k: 'son', g: 'Dönem', gc: 'ug-m', ad: 'Son Ay', w: 5.4, c: 'sy kalin', s: o => o.son, ilkYon: -1, h: o => kodBag(o.kod, fiyat(o.son, AYB)), top: () => '' },
        { k: 'deg', g: 'Dönem', gc: 'ug-m', ad: 'Değişim', w: 5.2, tip: 'İlk alım ayı – son alım ayı ortalama fiyatı', s: o => o.deg, ilkYon: -1, h: o => dg(o.deg), top: x => dg(agOrt(x, o => o.deg, o => o.tutar)) },
        { k: 'tutar', g: 'Dönem', gc: 'ug-m', ad: `Tutar (${sim(AYB)})`, w: 6.8, c: 'sy kalin', s: o => o.tutar, ilkYon: -1, h: o => para(o.tutar, AYB), top: x => para(topla(x, o => o.tutar), AYB) }] };
    },
    // Alım Detayı: fatura kalemleri
    detay() {
      const dv = dvAd(), onc = new Map(), g = new Map();
      TUM.forEach(r => { let l = g.get(r.kod); if (!l) g.set(r.kod, l = []); l.push(r); });
      g.forEach(l => { l.sort(sirali); l.forEach((r, i) => { const p = i ? l[i - 1] : null; onc.set(r.id, p ? { tl: p.fTl ? (r.fTl / p.fTl - 1) * 100 : null, dv: fD(p) ? (fD(r) / fD(p) - 1) * 100 : null } : { tl: null, dv: null }); }); });
      const P = r => onc.get(r.id) || {};
      return { minGen: 1640, renk: r => { const d = P(r).tl; return d == null ? '' : d > ESIK ? 'art' : d < -ESIK ? 'dus' : ''; }, tl: r => r.tTl, dv: tD, satir: DON, anahtar: r => r.id, sira: { k: 'tarih', yon: -1 }, dipAd: x => `Toplam · ${say(x.length)} Kalem · ${say(new Set(x.map(r => r.cariKod + '|' + r.no)).size)} Fatura`, birim: 'Kalem', kol: [
        { k: 'tarih', ad: 'Tarih', w: 5.2, s: r => r.tarih + String(r.id).padStart(6, '0'), ilkYon: -1, h: r => fatBag(r, trh(r.tarih)) },
        { k: 'no', ad: 'Belge No', w: 7.6, s: r => r.no, h: r => fatBag(r, esc(r.no)) },
        { k: 'sirket', ad: 'Şirket', w: 5.6, f: 'sirket', s: r => r.sirket, h: r => esc(r.sirket) },
        { k: 'cari', ad: 'Tedarikçi', w: 7.6, c: 'yz', f: 'cari', s: r => r.cari, h: r => `<span title="${esc(r.cariKod + ' · ' + r.unvan)}">${esc(r.cari)}</span>` },
        { k: 'temsilci', ad: 'Satınalma Temsilcisi', w: 6.4, c: 'yz', f: 'temsilci', s: r => r.temsilci, h: r => esc(r.temsilci) },
        { k: 'kod', ad: 'Malzeme Kodu', w: 6.2, s: r => r.kod, h: r => kodBag(r.kod, `<b>${esc(r.kod)}</b>`) },
        { k: 'ad', ad: 'Malzeme', w: 8.6, c: 'yz', s: r => r.ad, h: r => kodBag(r.kod, esc(r.ad)) },
        { k: 'kategori', ad: 'Kategori', w: 5.6, c: 'yz', f: 'kategori', s: r => r.kategori, h: r => esc(r.kategori) },
        { k: 'miktar', ad: 'Miktar', w: 7.6, c: 'sy', s: r => r.miktar, ilkYon: -1, h: r => mikt(r.miktar, r.birim), top: x => tekBirim(x, r => r.birim, r => r.miktar) },
        { k: 'fiyat', g: 'Birim Fiyat', gc: 'ug-f', ad: 'Belge', w: 5.6, c: 'sy', tip: 'Faturadaki birim fiyat, faturanın para birimiyle', s: r => r.fTl, ilkYon: -1, h: r => kodBag(r.kod, fiyat(r.fiyat, r.pb), r.id), top: () => '' },
        { k: 'kur', g: 'Birim Fiyat', gc: 'ug-f', ad: 'Kur', w: 4.4, c: 'sy', tip: 'Fatura günü TCMB kuru', s: r => r.pb === 'TL' ? null : r.kur, ilkYon: -1, h: r => r.pb === 'TL' ? '<span class="soluk">—</span>' : say(r.kur, 4), top: () => '' },
        { k: 'fTl', g: 'Birim Fiyat', gc: 'ug-f', ad: '₺', w: 5.4, c: 'sy kalin', s: r => r.fTl, ilkYon: -1, h: r => kodBag(r.kod, fiyat(r.fTl, 'TL'), r.id), top: () => '' },
        { k: 'fDv', g: 'Birim Fiyat', gc: 'ug-f', ad: dv, w: 5.4, c: 'sy kalin', s: r => fD(r), ilkYon: -1, h: r => kodBag(r.kod, fiyat(fD(r), DOVIZ), r.id), top: () => '' },
        { k: 'pTl', g: 'Önceki Alıma Göre', gc: 'ug-x', ad: '₺', w: 5, tip: 'Aynı malzemenin bir önceki alımına göre ₺ birim fiyat değişimi', s: r => P(r).tl, ilkYon: -1, h: r => dg(P(r).tl), top: x => dg(agOrt(x, r => P(r).tl, r => r.tTl)) },
        { k: 'pDv', g: 'Önceki Alıma Göre', gc: 'ug-x', ad: dv, w: 5, s: r => P(r).dv, ilkYon: -1, h: r => dg(P(r).dv), top: x => dg(agOrt(x, r => P(r).dv, r => r.tTl)) },
        { k: 'tTl', g: 'Tutar', gc: 'ug-m', ad: '₺', w: 6.6, c: 'sy kalin', s: r => r.tTl, ilkYon: -1, h: r => para(r.tTl, 'TL'), top: x => para(topla(x, r => r.tTl), 'TL') },
        { k: 'tDv', g: 'Tutar', gc: 'ug-m', ad: dv, w: 6, c: 'sy', s: r => tD(r), ilkYon: -1, h: r => para(tD(r), DOVIZ), top: x => para(topla(x, tD), DOVIZ) },
        { k: 'ek', ad: '', w: 2.2, c: 'ik', h: r => `<a class="sr-ek" href="/Rapor20?bpCode=${encodeURIComponent(r.cariKod)}" target="_blank" rel="noopener" title="Cari Ekstre · ${esc(r.cari)}"><i class="fas fa-file-invoice"></i></a>`, top: () => '' }] };
    },
    temsilci: () => grupSekme('temsilci', 'Satınalma Temsilcisi', 'Temsilci'),
    tedOzet: () => grupSekme('cari', 'Tedarikçi', 'Tedarikçi'),
    kategori: () => grupSekme('kategori', 'Kategori', 'Kategori')
  };
  /** Temsilci · Tedarikçi · Kategori özeti: alım tutarı ve payı, malzeme fiyat yönleri, tutar ağırlıklı ortalama değişim; ada tıklayınca o değere süzülüp Malzeme Fiyat Analizi açılır */
  function grupSekme(alan, baslik, tekAd) {
    const dv = dvAd(), g = new Map(), deger = AL[alan];
    DON.forEach(r => { const k = deger(r); let o = g.get(k); if (!o) g.set(k, o = { ad: k, ted: new Set(), mlz: new Set(), fat: new Set(), tems: new Set(), pbler: new Set(), n: 0, tTl: 0, tDv: 0, son: '', bolge: r.bolge }); o.n++; o.ted.add(r.cariKod); o.mlz.add(r.kod); o.fat.add(r.cariKod + '|' + r.no); o.tems.add(r.temsilci); o.pbler.add(r.pb); o.tTl += r.tTl; o.tDv += tD(r); if (r.tarih > o.son) o.son = r.tarih; });
    const top = topla([...g.values()], o => o.tTl), l = [...g.values()];
    l.forEach(o => { const st = [...ozetle(TUM.filter(r => deger(r) === o.ad), r => r.kod).values()]; ['Arttı', 'Düştü', 'Değişmedi', 'Tek Alım'].forEach((y, i) => { o['y' + i] = st.filter(s => s.yon === y).length; }); o.dTl = agOrt(st, s => s.dTl, s => s.tTl); o.dDv = agOrt(st, s => s.dDv, s => s.tTl); o.pay = top ? o.tTl / top * 100 : 0; });
    const ted = alan === 'cari', sayi = v => v ? say(v) : '<span class="soluk">—</span>';
    return { minGen: ted ? 1500 : 1260, tl: o => o.tTl, dv: o => o.tDv, satir: l, anahtar: o => o.ad, sira: { k: 'tTl', yon: -1 }, dipAd: x => `Toplam · ${say(x.length)} ${tekAd}`, birim: tekAd, kol: [
      { k: 'ad', ad: baslik, w: 10, c: 'yz', s: o => o.ad, h: o => `<a class="sf-tik" data-suz="${alan}" data-deger="${esc(o.ad)}" title="Bu değere süz ve Malzeme Fiyat Analizi'ni aç"><b>${esc(o.ad)}</b></a>` },
      ...(ted ? [{ k: 'tems', ad: 'Satınalma Temsilcisi', w: 8, c: 'yz', s: o => [...o.tems].join(', '), h: o => esc([...o.tems].join(', ')) }, { k: 'bolge', ad: 'Bölge', w: 4.8, s: o => o.bolge, h: o => esc(o.bolge) }, { k: 'pb', ad: 'Para Birimi', w: 5, s: o => [...o.pbler].sort().join(' · '), h: o => esc([...o.pbler].sort().join(' · ')) }, { k: 'son', ad: 'Son Alım', w: 5.8, s: o => o.son, ilkYon: -1, h: o => trh(o.son) }]
        : [{ k: 'ted', g: 'Alım', gc: 'ug-m', ad: 'Tedarikçi', w: 5.9, c: 'sy', s: o => o.ted.size, ilkYon: -1, h: o => say(o.ted.size), top: x => say(new Set(x.flatMap(o => [...o.ted])).size) }]),
      { k: 'mlz', g: 'Alım', gc: 'ug-m', ad: 'Malzeme', w: 5.7, c: 'sy', s: o => o.mlz.size, ilkYon: -1, h: o => say(o.mlz.size), top: x => say(new Set(x.flatMap(o => [...o.mlz])).size) },
      { k: 'fat', g: 'Alım', gc: 'ug-m', ad: 'Fatura', w: 4.8, c: 'sy', s: o => o.fat.size, ilkYon: -1, h: o => say(o.fat.size), top: x => say(topla(x, o => o.fat.size)) },
      { k: 'n', g: 'Alım', gc: 'ug-m', ad: 'Kalem', w: 4.8, c: 'sy', s: o => o.n, ilkYon: -1, h: o => say(o.n), top: x => say(topla(x, o => o.n)) },
      { k: 'tTl', g: 'Alım', gc: 'ug-m', ad: 'Tutar (₺)', w: 8, c: 'sy kalin', s: o => o.tTl, ilkYon: -1, h: o => para(o.tTl, 'TL'), top: x => para(topla(x, o => o.tTl), 'TL') },
      { k: 'tDv', g: 'Alım', gc: 'ug-m', ad: `Tutar (${dv})`, w: 7, c: 'sy', s: o => o.tDv, ilkYon: -1, h: o => para(o.tDv, DOVIZ), top: x => para(topla(x, o => o.tDv), DOVIZ) },
      { k: 'pay', g: 'Alım', gc: 'ug-m', ad: 'Pay', w: 11.2, s: o => o.pay, ilkYon: -1, h: o => `<span class="sf-pay"><span class="yol"><i style="width:${o.pay.toFixed(1)}%"></i></span><b>%${NB}${say(o.pay, 1)}</b></span>`, top: x => `%${NB}${say(topla(x, o => o.pay), 1)}` },
      { k: 'y0', g: 'Malzeme Fiyat Yönü (₺)', gc: 'ug-x', ad: 'Artan', w: 4.6, c: 'sy', s: o => o.y0, ilkYon: -1, h: o => o.y0 ? `<span class="sf-dg art"><i>▲</i>${say(o.y0)}</span>` : sayi(0), top: x => say(topla(x, o => o.y0)) },
      { k: 'y1', g: 'Malzeme Fiyat Yönü (₺)', gc: 'ug-x', ad: 'Düşen', w: 4.6, c: 'sy', s: o => o.y1, ilkYon: -1, h: o => o.y1 ? `<span class="sf-dg dus"><i>▼</i>${say(o.y1)}</span>` : sayi(0), top: x => say(topla(x, o => o.y1)) },
      { k: 'y2', g: 'Malzeme Fiyat Yönü (₺)', gc: 'ug-x', ad: 'Değişmeyen', w: 7, c: 'sy', s: o => o.y2, ilkYon: -1, h: o => sayi(o.y2), top: x => say(topla(x, o => o.y2)) },
      { k: 'y3', g: 'Malzeme Fiyat Yönü (₺)', gc: 'ug-x', ad: 'Tek Alım', w: 4.8, c: 'sy', s: o => o.y3, ilkYon: -1, h: o => sayi(o.y3), top: x => say(topla(x, o => o.y3)) },
      { k: 'dTl', g: 'Ortalama Değişim', gc: 'ug-d', ad: '₺', w: 5.4, tip: 'Malzemelerin son alım – önceki alım değişimi, dönem tutarıyla ağırlıklı', s: o => o.dTl, ilkYon: -1, h: o => dg(o.dTl), top: x => dg(agOrt(x, o => o.dTl, o => o.tTl)) },
      { k: 'dDv', g: 'Ortalama Değişim', gc: 'ug-d', ad: dv, w: 5.4, s: o => o.dDv, ilkYon: -1, h: o => dg(o.dDv), top: x => dg(agOrt(x, o => o.dDv, o => o.tTl)) }] };
  }
  const SEKME_AD = () => { const b = $(`#sfSekmeler button[data-p="${SEKME}"]`); if (!b) return ''; const k = b.cloneNode(true); k.querySelectorAll('.n').forEach(x => x.remove()); return k.textContent.trim(); };

  // ---------- Özet sekmesi: kutucuklar + fiyatı en çok artan / düşen malzemeler ----------
  function kpiCiz() {
    const ml = [...MLZ.values()], tTl = topla(DON, r => r.tTl), tDv = topla(DON, tD), dv = dvAd();
    const kutu = (k, l, v, s, yon) => `<div style="--k:${k}"${yon ? ` data-yon="${yon}" class="${F.yon.length === 1 && F.yon[0] === yon ? 'secili' : ''}" title="Fiyatı ${yon === 'Arttı' ? 'artan' : 'düşen'} malzemelere süz ve Malzeme Fiyat Analizi'ni aç"` : ''}><div class="l">${l}</div><div class="v">${v}</div><div class="s">${s}</div></div>`;
    $('#sfKpi').innerHTML = [
      kutu('#1e3a8a', 'Alım Tutarı (₺)', para(tTl, 'TL'), `${say(DON.length)} kalem · ${say(new Set(DON.map(r => r.cariKod + '|' + r.no)).size)} fatura`),
      kutu('#1e3a8a', `Alım Tutarı (${dv})`, para(tDv, DOVIZ), tDv ? `ortalama kur <b>${say(tTl / tDv, 4)}${NB}₺</b>` : '&nbsp;'),
      kutu('#1e3a8a', 'Malzeme · Tedarikçi', `${say(ml.length)} · ${say(new Set(DON.map(r => r.cariKod)).size)}`, `${say(new Set(DON.map(r => r.temsilci)).size)} satınalma temsilcisi`),
      kutu('#b91c1c', 'Fiyatı Artan Malzeme (₺)', say(ml.filter(s => s.yon === 'Arttı').length), `${dv} bazında <b>${say(ml.filter(s => s.yonDv === 'Arttı').length)}</b> malzeme`, 'Arttı'),
      kutu('#15803d', 'Fiyatı Düşen Malzeme (₺)', say(ml.filter(s => s.yon === 'Düştü').length), `${dv} bazında <b>${say(ml.filter(s => s.yonDv === 'Düştü').length)}</b> malzeme`, 'Düştü'),
      kutu('#b45309', 'Ortalama Fiyat Değişimi (₺)', dg(agOrt(ml, s => s.dTl, s => s.tTl)), `${dv} bazında ${dg(agOrt(ml, s => s.dDv, s => s.tTl))}`)].join('');
  }
  function ozetCiz() {
    const g = $('#sfOzet').getBoundingClientRect().width, k = 1 + (Math.max(1, Math.min(1.6, g / 1650)) - 1) * .5; kok.style.setProperty('--sf-k', k.toFixed(3));
    kpiCiz();
    const dv = dvAd(), ml = [...MLZ.values()];
    const tanim = (liste, yon) => ({ satir: liste, anahtar: s => s.kod, sira: { k: 'dTl', yon }, renk: s => YON_RENK[s.yon] || '', dipAd: x => `Toplam · ${say(x.length)} Malzeme`, bos: 'Bu Yönde Fiyatı Değişen Malzeme Yok', kol: [
      { k: 'kod', ad: 'Malzeme Kodu', w: 9.5, s: s => s.kod, h: s => kodBag(s.kod, `<b>${esc(s.kod)}</b>`) },
      { k: 'ad', ad: 'Malzeme', w: 13, c: 'yz', s: s => s.ad, h: s => kodBag(s.kod, esc(s.ad)) },
      { k: 'sonTed', ad: 'Son Tedarikçi', w: 10, c: 'yz', s: s => s.son.cari, h: s => esc(s.son.cari) },
      { k: 'oncTl', ad: 'Önceki Fiyat (₺)', w: 8, c: 'sy', s: s => s.onc && s.onc.fTl, ilkYon: -1, h: s => s.onc ? kodBag(s.kod, fiyat(s.onc.fTl, 'TL'), s.onc.id) : '<span class="soluk">—</span>' },
      { k: 'sonTl', ad: 'Son Fiyat (₺)', w: 8, c: 'sy kalin', s: s => s.son.fTl, ilkYon: -1, h: s => kodBag(s.kod, fiyat(s.son.fTl, 'TL'), s.son.id) },
      { k: 'sonDv', ad: `Son Fiyat (${dv})`, w: 8, c: 'sy kalin', s: s => fD(s.son), ilkYon: -1, h: s => kodBag(s.kod, fiyat(fD(s.son), DOVIZ), s.son.id) },
      { k: 'dTl', ad: 'Değişim (₺)', w: 7.5, s: s => s.dTl, ilkYon: -1, h: s => dg(s.dTl), top: x => dg(agOrt(x, s => s.dTl, s => s.tTl)) },
      { k: 'dDv', ad: `Değişim (${dv})`, w: 7.5, s: s => s.dDv, ilkYon: -1, h: s => dg(s.dDv), top: x => dg(agOrt(x, s => s.dDv, s => s.tTl)) },
      { k: 'tTl', ad: 'Dönem Tutarı (₺)', w: 10.5, c: 'sy kalin', s: s => s.tTl, ilkYon: -1, h: s => para(s.tTl, 'TL'), top: x => para(topla(x, s => s.tTl), 'TL') }] });
    [['#sfOzetArt', ml.filter(s => s.yon === 'Arttı').sort((a, b) => b.dTl - a.dTl).slice(0, 12), -1], ['#sfOzetDus', ml.filter(s => s.yon === 'Düştü').sort((a, b) => a.dTl - b.dTl).slice(0, 12), 1]].forEach(([id, liste, yon]) => {
      const t = $(id); olcekle(t, t.parentElement, 790, 1.3); tabloYaz(t, tanim(liste, yon), DURUM[id] = {}, { sec: false, suz: false }); });
  }

  // ---------- çizim ----------
  let MIN_GEN = 1600;
  /** Yazı oranı: genişliğin izin verdiği en büyük oran; satırların hepsi ekrana sığabiliyorsa sığdıran en büyük oran (az satırlı tabloda kaydırma çıkmasın). Uzun listede (oran 1'de de sığmıyorsa) genişlik oranı kalır, tablo içeride kayar. */
  function oranUygula() {
    const t = $('#sfTablo'), k = $('#sfKap'), g = k.getBoundingClientRect().width - 18;
    kok.style.setProperty('--sf-k', (1 + (Math.max(1, Math.min(1.6, g / 1650)) - 1) * .5).toFixed(3));
    const ust = Math.max(1, Math.min(1.4, g / (MIN_GEN * 1.02))), yer = window.innerHeight - k.getBoundingClientRect().top - 14;
    const dene = o => { t.style.setProperty('--sf-o', o.toFixed(3)); return t.offsetHeight <= yer; };
    if (!dene(ust) && ust > 1) { if (dene(1)) { let a = 1, b = ust; for (let i = 0; i < 7; i++) { const m = (a + b) / 2; if (dene(m)) a = m; else b = m; } dene(a); } else dene(ust); }
    kapBoyut(); yapYaz(t);
  }
  function yenidenOlc() { if (SEKME === 'ozet') ozetCiz(); else oranUygula(); if (!$('#sfPerde').hidden) olcekle($('#sfGcTablo'), $('#sfPerde .gc-kap'), GC_GEN, 1.45); if (!$('#sfFatPerde').hidden) olcekle($('#sfFtTablo'), $('#sfFatPerde .gc-kap'), FT_GEN, 1.45); }
  function kapBoyut() { const k = $('#sfKap'); k.style.maxHeight = Math.max(260, window.innerHeight - k.getBoundingClientRect().top - 12) + 'px'; }
  /** sekme adlarındaki kayıt sayıları (genel süzgeçlere ve döneme göre) */
  function sayacYaz() {
    const ted = new Map(); DON.forEach(r => { let x = ted.get(r.kod); if (!x) ted.set(r.kod, x = new Set()); x.add(r.cariKod); });
    let tedSatir = 0; ted.forEach(x => { if (!COK || x.size > 1) tedSatir += x.size; });
    const n = { malzeme: MLZ.size, tedarikci: tedSatir, aylik: MLZ.size, detay: DON.length, temsilci: new Set(DON.map(r => r.temsilci)).size, tedOzet: new Set(DON.map(r => r.cari)).size, kategori: new Set(DON.map(r => r.kategori)).size };
    $$('#sfSekmeler button[data-p]').forEach(b => { const v = n[b.dataset.p]; if (v === undefined) return; b.querySelector('.n').textContent = say(v); b.classList.toggle('sifir', !v); });
  }
  /** üst şeritteki Seçili Tutar kutuları: etkin sekmede işaretli satırların ₺ ve döviz alım tutarı */
  function seciliYaz(sl, T) {
    $('#sfSeciliTl').textContent = para(sl && T && T.tl ? topla(sl, T.tl) : 0, 'TL'); $('#sfSeciliDv').textContent = para(sl && T && T.dv ? topla(sl, T.dv) : 0, DOVIZ); $('#sfSeciliDvAd').textContent = dvAd();
  }
  function ciz() {
    hesapla(); SZ.yaz(); sayacYaz();
    $('#sfDonemYazi').textContent = 'Fiyat Analizi · ' + donemYazi();
    const ozet = SEKME === 'ozet'; $('#sfTabloKart').hidden = ozet; $('#sfOzet').hidden = !ozet;
    if (ozet) { $('#sfIslem').innerHTML = ''; $('#sfKapsam').textContent = ''; seciliYaz(); ozetCiz(); return; }
    const T = SEKMELER[SEKME](), D = DURUM[SEKME] || (DURUM[SEKME] = {});
    $('#sfIslem').innerHTML = T.islem || ''; $('#sfTablo').style.minWidth = (T.minGen || 0) + 'px'; MIN_GEN = T.minGen || 1600;
    olcekle($('#sfTablo'), $('#sfKap'), MIN_GEN);
    tabloYaz($('#sfTablo'), T, D, { sonra: (l, sl) => { $('#sfKapsam').textContent = l.length !== T.satir.length ? `${say(l.length)} / ${say(T.satir.length)} ${T.birim}` : (T.kapsam || ''); seciliYaz(sl, T); } });
    oranUygula();
  }
  function sekmeAc(p) { if (p !== 'ozet' && !SEKMELER[p]) p = 'malzeme'; SEKME = p; $$('#sfSekmeler button[data-p]').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $('#sfKap').scrollTop = 0; ciz(); }

  // ---------- Fatura İncelemesi penceresi: malzemenin bütün alış faturaları (kod, ad ya da herhangi bir fiyata tıklayınca) ----------
  // vurgu = tıklanan fiyatın fatura satırı (id): tabloda işaretlenir ve görünür yere kaydırılır
  let GC = null;
  function vurguyaGit(tablo, kap) { const v = tablo.querySelector('tbody tr.vurgu'); kap.scrollTop = v ? Math.max(0, v.offsetTop - kap.clientHeight / 2) : 0; }
  function gecmisAc(kod, vurgu) {
    const l = TUM.filter(r => r.kod === kod).sort(sirali); if (!l.length) return;
    faturaKapat(); vurgu = +vurgu || 0;
    const son = l[l.length - 1], ilk = l[0], dv = dvAd(), mik = topla(l, r => r.miktar), tTl = topla(l, r => r.tTl);
    // en düşük / en yüksek: Malzeme Fiyat Analizi'ndeki sütunlarla aynı ölçü (dönem alımları, ₺ birim fiyat, eşitlikte en yeni fatura)
    const dn = l.filter(donemde), dl = dn.length ? dn : l; let enD = dl[0], enY = dl[0]; dl.forEach(r => { if (r.fTl <= enD.fTl) enD = r; if (r.fTl >= enY.fTl) enY = r; });
    GC = { kod, ad: son.ad };
    $('#sfGcAd').innerHTML = `<small>Fatura İncelemesi</small>${esc(kod)}<span>·</span>${esc(son.ad)}`;
    $('#sfGcAlt').textContent = `${son.kategori} · Birim ${son.birim} · ${tekil(l.map(r => r.cari)).join(', ')} · ${trh(ilk.tarih)} – ${trh(son.tarih)}`;
    const kutu = (k, a, v, s, fat) => `<div style="--k:${k}"${fat ? ` data-fat="${fat.id}" title="Faturayı Aç · ${esc(fat.no)}"` : ''}><div class="l">${a}</div><div class="v">${v}</div><div class="s">${s}</div></div>`;
    const deg = (a, b) => a ? (b / a - 1) * 100 : null;
    $('#sfGcKpi').innerHTML = [
      kutu('#2a78d6', 'Son Fiyat (₺)', fiyat(son.fTl, 'TL'), `${trh(son.tarih)} · ${esc(son.cari)}`, son),
      kutu('#eb6834', `Son Fiyat (${dv})`, fiyat(fD(son), DOVIZ), son.pb === 'TL' ? `kur ${say(son.fTl / fD(son), 4)}${NB}₺` : `belge ${fiyat(son.fiyat, son.pb)}`, son),
      kutu('#2a78d6', 'İlk Alım – Son Alım (₺)', dg(l.length > 1 ? deg(ilk.fTl, son.fTl) : null), `${fiyat(ilk.fTl, 'TL')} → ${fiyat(son.fTl, 'TL')}`),
      kutu('#eb6834', `İlk Alım – Son Alım (${dv})`, dg(l.length > 1 ? deg(fD(ilk), fD(son)) : null), `${fiyat(fD(ilk), DOVIZ)} → ${fiyat(fD(son), DOVIZ)}`),
      kutu('#15803d', 'Dönem En Düşük Fiyat (₺)', fiyat(enD.fTl, 'TL'), `${trh(enD.tarih)} · ${fiyat(fD(enD), DOVIZ)} · ${esc(enD.cari)}`, enD),
      kutu('#b91c1c', 'Dönem En Yüksek Fiyat (₺)', fiyat(enY.fTl, 'TL'), `${trh(enY.tarih)} · ${fiyat(fD(enY), DOVIZ)} · ${esc(enY.cari)}`, enY),
      kutu('#b45309', 'En Düşük – En Yüksek Fark', dl.length > 1 && enD.fTl > 0 ? `%${NB}${say((enY.fTl / enD.fTl - 1) * 100, 1)}` : YOK, `${fiyat(enD.fTl, 'TL')} → ${fiyat(enY.fTl, 'TL')}`),
      kutu('#475569', 'Toplam Alım', para(tTl, 'TL'), `${say(l.length)} alım · ${mikt(mik, son.birim)}`)].join('');
    const onc = new Map(); l.forEach((r, i) => { const p = i ? l[i - 1] : null; onc.set(r.id, p ? { tl: deg(p.fTl, r.fTl), dv: deg(fD(p), fD(r)) } : {}); });
    const T = { satir: l, anahtar: r => r.id, sira: { k: 'tarih', yon: -1 }, renk: r => r.id === vurgu ? 'vurgu' : '', dipAd: x => `Toplam · ${say(x.length)} Alım`, kol: [
      { k: 'tarih', ad: 'Fatura Tarihi', w: 5.8, s: r => r.tarih + String(r.id).padStart(6, '0'), ilkYon: -1, h: r => fatBag(r, trh(r.tarih)) },
      { k: 'no', ad: 'Belge No', w: 8, s: r => r.no, h: r => fatBag(r, esc(r.no)) },
      { k: 'sirket', ad: 'Şirket', w: 6, s: r => r.sirket, h: r => esc(r.sirket) },
      { k: 'cari', ad: 'Tedarikçi', w: 9, c: 'yz', s: r => r.cari, h: r => `<span title="${esc(r.unvan)}">${esc(r.cari)}</span>` },
      { k: 'temsilci', ad: 'Satınalma Temsilcisi', w: 7.5, c: 'yz', s: r => r.temsilci, h: r => esc(r.temsilci) },
      { k: 'miktar', ad: 'Miktar', w: 6.4, c: 'sy', s: r => r.miktar, ilkYon: -1, h: r => mikt(r.miktar, r.birim), top: x => mikt(topla(x, r => r.miktar), son.birim) },
      { k: 'fiyat', g: 'Birim Fiyat', gc: 'ug-f', ad: 'Belge', w: 6, c: 'sy', s: r => r.fTl, ilkYon: -1, h: r => fatBag(r, fiyat(r.fiyat, r.pb)), top: () => '' },
      { k: 'kur', g: 'Birim Fiyat', gc: 'ug-f', ad: 'Kur', w: 5, c: 'sy', s: r => r.pb === 'TL' ? null : r.kur, ilkYon: -1, h: r => r.pb === 'TL' ? YOK : say(r.kur, 4), top: () => '<span class="soluk">Ortalama</span>' },
      { k: 'fTl', g: 'Birim Fiyat', gc: 'ug-f', ad: '₺', w: 6, c: 'sy', s: r => r.fTl, ilkYon: -1, h: r => fatBag(r, fiyat(r.fTl, 'TL')), top: x => fiyat(topla(x, r => r.tTl) / (topla(x, r => r.miktar) || 1), 'TL') },
      { k: 'fUsd', g: 'Birim Fiyat', gc: 'ug-f', ad: '$', w: 6, c: 'sy', s: r => r.fUsd, ilkYon: -1, h: r => fatBag(r, fiyat(r.fUsd, 'USD')), top: x => fiyat(topla(x, r => r.tUsd) / (topla(x, r => r.miktar) || 1), 'USD') },
      { k: 'fEur', g: 'Birim Fiyat', gc: 'ug-f', ad: '€', w: 6, c: 'sy', s: r => r.fEur, ilkYon: -1, h: r => fatBag(r, fiyat(r.fEur, 'EUR')), top: x => fiyat(topla(x, r => r.tEur) / (topla(x, r => r.miktar) || 1), 'EUR') },
      { k: 'pTl', g: 'Önceki Alıma Göre', gc: 'ug-x', ad: '₺', w: 5.6, s: r => onc.get(r.id).tl, ilkYon: -1, h: r => dg(onc.get(r.id).tl), top: () => '' },
      { k: 'pDv', g: 'Önceki Alıma Göre', gc: 'ug-x', ad: dv, w: 5.6, s: r => onc.get(r.id).dv, ilkYon: -1, h: r => dg(onc.get(r.id).dv), top: () => '' },
      { k: 'tTl', g: 'Tutar', gc: 'ug-m', ad: '₺', w: 7, c: 'sy', s: r => r.tTl, ilkYon: -1, h: r => para(r.tTl, 'TL'), top: x => para(topla(x, r => r.tTl), 'TL') },
      { k: 'tDv', g: 'Tutar', gc: 'ug-m', ad: dv, w: 6.4, c: 'sy', s: r => tD(r), ilkYon: -1, h: r => para(tD(r), DOVIZ), top: x => para(topla(x, tD), DOVIZ) }] };
    $('#sfPerde').hidden = false;
    olcekle($('#sfGcTablo'), $('#sfPerde .gc-kap'), GC_GEN, 1.45);
    tabloYaz($('#sfGcTablo'), T, { limit: 1e9 }, { sec: false, suz: false });
    grafikCiz(l);
    vurguyaGit($('#sfGcTablo'), $('#sfPerde .gc-kap'));
  }
  const gecmisKapat = () => { $('#sfPerde').hidden = true; GC = null; };

  // ---------- Fatura penceresi: tek alış faturası — başlık bilgileri ve bütün kalemleri (fatura tarihine / belge numarasına tıklayınca) ----------
  // Fatura süzgeçlerden bağımsız bütün kalemleriyle gelir; tıklanan kalem işaretlenir. Malzeme koduna / fiyata tıklayınca o malzemenin Fatura İncelemesi açılır.
  let FT = null; const GC_GEN = 1500, FT_GEN = 1320;
  function faturaAc(id) {
    const r0 = V.satirlar.find(r => r.id === +id); if (!r0) return;
    const l = V.satirlar.filter(r => r.cariKod === r0.cariKod && r.no === r0.no).sort((a, b) => a.id - b.id), dv = dvAd();
    const tPb = topla(l, r => r.tutar), tTl = topla(l, r => r.tTl), tDv = topla(l, tD);
    FT = { no: r0.no, cari: r0.cari };
    $('#sfFtAd').innerHTML = `<small>Alış Faturası</small>${esc(r0.no)}<span>·</span>${esc(r0.cari)}`;
    $('#sfFtAlt').textContent = [r0.cariKod, r0.unvan, r0.sirket, 'Satınalma Temsilcisi ' + r0.temsilci, r0.kosul ? 'Vade Şartı ' + r0.kosul : '', r0.islem ? 'İşlem Tipi ' + r0.islem : ''].filter(Boolean).join(' · ');
    $('#sfFtEkstre').href = '/Rapor20?bpCode=' + encodeURIComponent(r0.cariKod); $('#sfFtTakip').href = '/Rapor52?kod=' + encodeURIComponent(r0.cariKod); $('#sfFtOrnek').hidden = !V.ornek;
    const kutu = (k, a, v, s) => `<div style="--k:${k}"><div class="l">${a}</div><div class="v">${v}</div><div class="s">${s}</div></div>`;
    $('#sfFtKpi').innerHTML = [
      kutu('#1e3a8a', 'Fatura Tarihi', trh(r0.tarih), esc(r0.no)),
      kutu('#1e3a8a', `Fatura Tutarı (${sim(r0.pb)})`, para(tPb, r0.pb), 'KDV hariç · belge para birimi'),
      kutu('#2a78d6', 'Tutar (₺)', para(tTl, 'TL'), r0.pb === 'TL' ? 'belge para birimi ₺' : `fatura günü ${sim(r0.pb)} kuru ${say(r0.kur, 4)}${NB}₺`),
      kutu('#eb6834', `Tutar (${dv})`, para(tDv, DOVIZ), tDv ? `fatura günü ${dv} kuru ${say(tTl / tDv, 4)}${NB}₺` : '&nbsp;'),
      kutu('#475569', 'Kalem', say(l.length), `${say(tekil(l.map(r => r.kategori)).length)} kategori`)].join('');
    const T = { satir: l, anahtar: r => r.id, sira: { k: 'sira', yon: 1 }, renk: r => r.id === r0.id ? 'vurgu' : '', dipAd: x => `Toplam · ${say(x.length)} Kalem`, kol: [
      { k: 'kod', ad: 'Malzeme Kodu', w: 7.4, s: r => r.kod, h: r => kodBag(r.kod, esc(r.kod), r.id) },
      { k: 'ad', ad: 'Malzeme', w: 12, c: 'yz', s: r => r.ad, h: r => kodBag(r.kod, esc(r.ad), r.id) },
      { k: 'kategori', ad: 'Kategori', w: 6.4, c: 'yz', s: r => r.kategori, h: r => esc(r.kategori) },
      { k: 'miktar', ad: 'Miktar', w: 6, c: 'sy', s: r => r.miktar, ilkYon: -1, h: r => mikS(r.miktar) },
      { k: 'birim', ad: 'Birim', w: 4.6, c: 'yz', s: r => r.birim, h: r => esc(r.birim) },
      { k: 'fiyat', g: 'Birim Fiyat', gc: 'ug-f', ad: 'Belge', w: 6.4, c: 'sy', s: r => r.fTl, ilkYon: -1, h: r => kodBag(r.kod, fiyat(r.fiyat, r.pb), r.id), top: () => '' },
      { k: 'kur', g: 'Birim Fiyat', gc: 'ug-f', ad: 'Kur', w: 5.2, c: 'sy', tip: 'Fatura günü TCMB kuru', s: r => r.pb === 'TL' ? null : r.kur, ilkYon: -1, h: r => r.pb === 'TL' ? YOK : say(r.kur, 4), top: () => '' },
      { k: 'fTl', g: 'Birim Fiyat', gc: 'ug-f', ad: '₺', w: 6.2, c: 'sy', s: r => r.fTl, ilkYon: -1, h: r => kodBag(r.kod, fiyat(r.fTl, 'TL'), r.id), top: () => '' },
      { k: 'fDv', g: 'Birim Fiyat', gc: 'ug-f', ad: dv, w: 6.2, c: 'sy', s: r => fD(r), ilkYon: -1, h: r => kodBag(r.kod, fiyat(fD(r), DOVIZ), r.id), top: () => '' },
      { k: 'tPb', g: 'Tutar', gc: 'ug-m', ad: 'Belge', w: 7.6, c: 'sy', s: r => r.tTl, ilkYon: -1, h: r => para(r.tutar, r.pb), top: x => para(topla(x, r => r.tutar), r0.pb) },
      { k: 'tTl', g: 'Tutar', gc: 'ug-m', ad: '₺', w: 7.6, c: 'sy', s: r => r.tTl, ilkYon: -1, h: r => para(r.tTl, 'TL'), top: x => para(topla(x, r => r.tTl), 'TL') },
      { k: 'tDv', g: 'Tutar', gc: 'ug-m', ad: dv, w: 7, c: 'sy', s: r => tD(r), ilkYon: -1, h: r => para(tD(r), DOVIZ), top: x => para(topla(x, tD), DOVIZ) }] };
    $('#sfFatPerde').hidden = false;
    olcekle($('#sfFtTablo'), $('#sfFatPerde .gc-kap'), FT_GEN, 1.45);
    tabloYaz($('#sfFtTablo'), T, { limit: 1e9 }, { sec: false, suz: false });
  }
  function faturaKapat() { $('#sfFatPerde').hidden = true; FT = null; }

  // Fiyat endeksi grafiği: ₺ ve döviz birim fiyatı ilk alım = 100 olacak şekilde tek eksende (iki ayrı eksen yok); aynı günün alımları miktar ağırlıklı tek nokta
  function grafikCiz(l) {
    const kap = $('#sfGcGrafik'), dv = dvAd(), g = new Map();
    l.forEach(r => { let o = g.get(r.tarih); if (!o) g.set(r.tarih, o = { tarih: r.tarih, m: 0, tl: 0, dv: 0, ted: new Set() }); o.m += r.miktar; o.tl += r.tTl; o.dv += tD(r); o.ted.add(r.cari); });
    const n = [...g.values()].filter(o => o.m > 0).map(o => ({ tarih: o.tarih, t: Date.parse(o.tarih + 'T00:00:00'), tl: o.tl / o.m, dv: o.dv / o.m, ted: [...o.ted].join(', ') }));
    if (n.length < 2) { kap.innerHTML = '<div class="bos">Fiyat endeksi için en az iki ayrı günde alım gerekir.</div>'; return; }
    n.forEach(o => { o.eTl = o.tl / n[0].tl * 100; o.eDv = o.dv / n[0].dv * 100; });
    const RT = '#2a78d6', RD = '#eb6834';
    kap.innerHTML = `<div class="ust"><b>Fiyat Endeksi</b><span>İlk Alım = 100</span><span class="gosterge"><span><i style="background:${RT}"></i>₺ Birim Fiyat</span><span><i style="background:${RD}"></i>${dv} Birim Fiyat</span></span></div><svg></svg><div class="sf-ipucu"></div>`;
    const svg = kap.querySelector('svg'), ip = kap.querySelector('.sf-ipucu'), W = Math.max(520, svg.clientWidth || kap.clientWidth - 24), H = 190, ms = 36, mu = 10, mg = 44, ma = 22;
    svg.setAttribute('viewBox', `0 0 ${W} ${H}`);
    const tum = n.flatMap(o => [o.eTl, o.eDv]).concat(100), mn = Math.min(...tum), mx = Math.max(...tum), adim = [1, 2, 5, 10, 20, 25, 50, 100].find(a => (mx - mn) / a <= 5) || 200;
    const lo = Math.floor((mn - adim * .25) / adim) * adim, hi = Math.ceil((mx + adim * .25) / adim) * adim, t0 = n[0].t, t1 = n[n.length - 1].t;
    const x = t => ms + (t - t0) / (t1 - t0 || 1) * (W - ms - mg), y = v => mu + (hi - v) / (hi - lo || 1) * (H - mu - ma);
    let s = '';
    for (let v = lo; v <= hi + 1e-9; v += adim) s += `<line x1="${ms}" x2="${W - mg}" y1="${y(v)}" y2="${y(v)}" stroke="${v === 100 ? '#94a3b8' : '#eef1f4'}" stroke-width="1"${v === 100 ? ' stroke-dasharray="3 3"' : ''}/><text x="${ms - 6}" y="${y(v) + 4}" text-anchor="end">${say(v)}</text>`;
    // ay çizgileri: en çok ~10 etiket
    const d = new Date(t0); d.setDate(1); d.setMonth(d.getMonth() + 1); const ayl = []; while (d.getTime() <= t1) { ayl.push(new Date(d)); d.setMonth(d.getMonth() + 1); }
    const atla = Math.max(1, Math.ceil(ayl.length / 10));
    ayl.forEach((a, i) => { if (i % atla) return; const px = x(a.getTime()); s += `<line x1="${px}" x2="${px}" y1="${H - ma}" y2="${H - ma + 4}" stroke="#cbd5e1"/><text x="${px}" y="${H - 5}" text-anchor="middle">${AY_AD[a.getMonth()]} ${String(a.getFullYear()).slice(2)}</text>`; });
    const yol = (alan, renk) => `<polyline fill="none" stroke="${renk}" stroke-width="2" stroke-linejoin="round" stroke-linecap="round" points="${n.map(o => x(o.t).toFixed(1) + ',' + y(o[alan]).toFixed(1)).join(' ')}"/>`;
    s += yol('eDv', RD) + yol('eTl', RT);
    // uç etiketleri (çakışırsa ayrılır); yazı mürekkep renginde, renk yalnız uç noktasında
    const son = n[n.length - 1]; let yT = y(son.eTl), yD = y(son.eDv); if (Math.abs(yT - yD) < 13) { const o = (yT + yD) / 2, ust = yT <= yD; yT = o + (ust ? -6.5 : 6.5); yD = o + (ust ? 6.5 : -6.5); }
    s += `<circle cx="${x(son.t)}" cy="${y(son.eTl)}" r="4" fill="${RT}" stroke="#fff" stroke-width="2"/><circle cx="${x(son.t)}" cy="${y(son.eDv)}" r="4" fill="${RD}" stroke="#fff" stroke-width="2"/><text class="uc" x="${x(son.t) + 9}" y="${yT + 4}">${say(son.eTl, 1)}</text><text class="uc" x="${x(son.t) + 9}" y="${yD + 4}">${say(son.eDv, 1)}</text>`;
    s += `<g class="iz" style="display:none"><line y1="${mu}" y2="${H - ma}" stroke="#94a3b8" stroke-width="1"/><circle r="4.5" fill="${RT}" stroke="#fff" stroke-width="2"/><circle r="4.5" fill="${RD}" stroke="#fff" stroke-width="2"/></g><rect x="${ms}" y="${mu}" width="${W - ms - mg}" height="${H - mu - ma}" fill="transparent"/>`;
    svg.innerHTML = s;
    const iz = svg.querySelector('g.iz'), [cizgi, c1, c2] = iz.children, alan = svg.querySelector('rect');
    alan.addEventListener('mousemove', e => {
      const r = svg.getBoundingClientRect(), px = (e.clientX - r.left) * (W / r.width); let o = n[0]; n.forEach(k => { if (Math.abs(x(k.t) - px) < Math.abs(x(o.t) - px)) o = k; });
      const ox = x(o.t); cizgi.setAttribute('x1', ox); cizgi.setAttribute('x2', ox); c1.setAttribute('cx', ox); c1.setAttribute('cy', y(o.eTl)); c2.setAttribute('cx', ox); c2.setAttribute('cy', y(o.eDv)); iz.style.display = '';
      ip.innerHTML = `<b>${trh(o.tarih)}</b><div class="alt">${esc(o.ted)}</div><div class="satir"><i style="background:${RT}"></i>₺ ${fiyat(o.tl, 'TL')}<span>${say(o.eTl, 1)}</span></div><div class="satir"><i style="background:${RD}"></i>${dv} ${fiyat(o.dv, DOVIZ)}<span>${say(o.eDv, 1)}</span></div>`;
      ip.style.display = 'block'; const sol = svg.offsetLeft + ox / (W / r.width); ip.style.left = (sol + ip.offsetWidth + 20 > kap.clientWidth ? sol - ip.offsetWidth - 12 : sol + 12) + 'px'; ip.style.top = (svg.offsetTop + 8) + 'px';
    });
    alan.addEventListener('mouseleave', () => { iz.style.display = 'none'; ip.style.display = 'none'; });
  }

  // ---------- Excel ----------
  function excel() {
    const ad = SEKME_AD(), suz = ALANLAR.filter(a => F[a.k].length).map(a => `${a.ad}: ${F[a.k].join(', ')}`).join(' · '), alt = `Dönem ${donemYazi()} · Döviz ${DOVIZ}${suz ? ' · ' + suz : ''}`, dosya = 'Satinalma_Fiyat_Analizi_' + ad.replace(/[^\wığüşöçİĞÜŞÖÇ]+/g, '_');
    if (SEKME === 'ozet') { TabloExcel.indir(dosya, [{ tablo: '#sfOzetArt', ad: 'En Çok Artan', baslik: 'Satınalma Fiyat Analiz Raporu · Fiyatı En Çok Artan Malzemeler (₺)', altBaslik: alt, kpi: '#sfKpi' }, { tablo: '#sfOzetDus', ad: 'En Çok Düşen', baslik: 'Fiyatı En Çok Düşen Malzemeler (₺)', altBaslik: alt }]); return; }
    const t = $('#sfTablo'); if (t.querySelector('tbody td.dahafazla')) { t._sf.D.limit = 1e9; govdeYaz(t); }
    TabloExcel.indir(dosya, [{ tablo: '#sfTablo', ad: ad.slice(0, 31), baslik: 'Satınalma Fiyat Analiz Raporu · ' + ad, altBaslik: alt }]);
  }

  // ---------- başlat ----------
  function veriAl(j) { V = j.paket; $('#sfOrnek').hidden = !V.ornek; $('#sfKur').innerHTML = `Kur · ${trh(V.bugun)} <b>$${NB}${say(V.kur.usd, 4)}</b> · <b>€${NB}${say(V.kur.eur, 4)}</b>`; }
  function donemSec(v) { donemKur(v); $$('#sfHizli button').forEach(b => b.classList.toggle('aktif', b.dataset.donem === v)); }
  function baslat() {
    SZ = SuzgecCubuk.kur({ kap: $('#sfSuz'), F, alanlar: ALANLAR, hizli: ['temsilci', 'sirket', 'cari', 'kategori'], ek: $('#sfEk'), alt: $('#sfHizli'),
      gruplar: [{ ad: 'Tedarikçi', alanlar: ['bolge', 'ulke', 'sinif', 'iliski', 'kosul'] }, { ad: 'Malzeme', alanlar: ['malzeme', 'birim'] }, { ad: 'Alım', alanlar: ['pb', 'islem', 'yon'] }],
      secenekler, degisti: k => { if (k === 'temizle') { Object.values(DURUM).forEach(d => { d.ara = {}; }); } ciz(); }, araYer: 'Malzeme kodu, adı, tedarikçi, belge no…' });
    donemSec('12');
    const u = new URLSearchParams(location.search); ['temsilci', 'sirket', 'cari', 'kategori'].forEach(k => { const v = u.get(k); if (v) F[k] = [v]; });
    $('#sfHizli').addEventListener('click', e => { const b = e.target.closest('button[data-donem]'); if (!b) return; donemSec(b.dataset.donem); ciz(); });
    ['#sfBas', '#sfBit'].forEach(s => $(s).addEventListener('change', () => { const b = $('#sfBas').value, t = $('#sfBit').value; if (!b || !t) return; BAS = b <= t ? b : t; BIT = b <= t ? t : b; $$('#sfHizli button').forEach(x => x.classList.remove('aktif')); ciz(); }));
    $('#sfDoviz').addEventListener('click', e => { const b = e.target.closest('button[data-d]'); if (!b || b.dataset.d === DOVIZ) return; DOVIZ = b.dataset.d; $$('#sfDoviz button').forEach(x => x.classList.toggle('aktif', x === b)); ciz(); });
    $('#sfSekmeler').addEventListener('click', e => {
      const b = e.target.closest('button[data-p]'); if (b) { sekmeAc(b.dataset.p); return; }
      if (e.target.closest('[data-i="cok"]')) { COK = !COK; ciz(); return; }
      const v = e.target.closest('[data-i="ayb"] button[data-v]'); if (v) { AYB = v.dataset.v; ciz(); }
    });
    $('#sfKpi').addEventListener('click', e => { const d = e.target.closest('[data-yon]'); if (!d) return; F.yon = F.yon.length === 1 && F.yon[0] === d.dataset.yon ? [] : [d.dataset.yon]; if (F.yon.length) sekmeAc('malzeme'); else ciz(); });
    $('#sfGetir').addEventListener('click', () => { const d = $('#sfGetir'); d.disabled = true; fetch('/UrunFiyatKarsilastirma/Veri').then(r => r.json()).then(j => { veriAl(j); ciz(); }).finally(() => { d.disabled = false; }); });
    $('#sfExcel').addEventListener('click', excel);
    $('#sfGcKapat').addEventListener('click', gecmisKapat);
    $('#sfPerde').addEventListener('mousedown', e => { if (e.target.id === 'sfPerde') gecmisKapat(); });
    $('#sfGcKpi').addEventListener('click', e => { const d = e.target.closest('[data-fat]'); if (d) faturaAc(d.dataset.fat); });
    $('#sfFtKapat').addEventListener('click', faturaKapat);
    $('#sfFatPerde').addEventListener('mousedown', e => { if (e.target.id === 'sfFatPerde') faturaKapat(); });
    // Esc en üstteki pencereyi kapatır (Fatura, sonra Fatura İncelemesi)
    document.addEventListener('keydown', e => { if (e.key !== 'Escape') return; if (!$('#sfFatPerde').hidden) faturaKapat(); else if (!$('#sfPerde').hidden) gecmisKapat(); });
    $('#sfGcExcel').addEventListener('click', () => { if (GC) TabloExcel.indir('Fatura_Incelemesi_' + GC.kod.replace(/[^\w-]+/g, '_'), [{ tablo: '#sfGcTablo', ad: 'Fatura İncelemesi', baslik: 'Fatura İncelemesi · ' + GC.kod + ' · ' + GC.ad, altBaslik: $('#sfGcAlt').textContent, kpi: '#sfGcKpi' }]); });
    $('#sfFtExcel').addEventListener('click', () => { if (FT) TabloExcel.indir('Fatura_' + FT.no.replace(/[^\w-]+/g, '_'), [{ tablo: '#sfFtTablo', ad: 'Fatura', baslik: 'Fatura · ' + FT.no + ' · ' + FT.cari, altBaslik: $('#sfFtAlt').textContent, kpi: '#sfFtKpi' }]); });
    let z; window.addEventListener('resize', () => { clearTimeout(z); z = setTimeout(yenidenOlc, 120); });
    sekmeAc(SEKME);
  }
  fetch('/UrunFiyatKarsilastirma/Veri').then(r => r.json()).then(j => { veriAl(j); baslat(); })
    .catch(e => { $('#sfTablo tbody').innerHTML = `<tr><td class="bos">Veri Yüklenemedi: ${esc(e.message)}</td></tr>`; });
})();
