// ============================================================
// TABLO → EXCEL (ortak bileşen) — ekrandaki tabloyu GÖRÜNDÜĞÜ GİBİ .xlsx olarak indirir. Kullanan ekranlar: Personel Maliyet Raporu, Personel Muhasebe İşlemleri.
//   Tablo hücre hücre okunur: başlık grupları (birleşik hücreler), zemin ve yazı renkleri, kalın / çizili yazı, hizalama, kenarlıklar, R üstte / X altta hücreler
//   (iki satırlı renkli metin), tutarlar gerçek sayı + ₺ biçimi, dip toplam, sekme başlığı, KPI kutuları, açıklama notu; sola sabit sütunlar ve başlık satırları
//   Excel'de dondurulmuş bölme olur. Gizli (0 ₺) sütunlar ve düğme sütunları (Kart, Fiş Önizleme) dışarıda kalır; düzenleme modunda giriş kutularının değeri yazılır.
//   Okunan yapı JSON olarak POST /TabloExcel/Indir ucuna gönderilir; dosya sunucuda ClosedXML ile üretilir (Controllers/TabloExcelController.cs, Data/TabloExcel.cs).
// Kullanım: TabloExcel.indir('dosya_adi', [{ tablo: '#id' | el, ad: 'Sayfa Adı', baslik, altBaslik, kpi: '#id' | el, not: '#id' | el }, { ad, baslik, basliklar: [...], satirlar: [[...]] }])
//   Tablo görünür (çizilmiş) olmalıdır; okuma hesaplanmış stillerden yapılır (getComputedStyle).
// ============================================================
window.TabloExcel = (() => {
  'use strict';
  const el = x => typeof x === 'string' ? document.querySelector(x) : (x || null);
  const BEYAZ = '#ffffff';
  /** rgb / rgba → #rrggbb (saydam → null; yarı saydam renk beyaz üstüne karıştırılır) */
  const hex = s => { const m = /rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*([\d.]+))?\)/.exec(s || ''); if (!m) return null; const a = m[4] === undefined ? 1 : parseFloat(m[4]); if (a <= 0) return null; const k = v => ('0' + Math.round(+v * a + 255 * (1 - a)).toString(16)).slice(-2); return '#' + k(m[1]) + k(m[2]) + k(m[3]); };
  const punto = px => Math.max(7, Math.round(parseFloat(px) * 0.75 + 1));   // ekran pikseli → Excel puntosu (.67rem ≈ 9 pt gövde, .58rem ≈ 8 pt başlık)
  const blokMu = d => /^(block|flex|grid|table|list-item|table-row|table-cell)$/.test(d);
  /** başlık hücresinin tek satıra sığan doğal genişliği (ekranda sarılan başlık Excel sütununu daraltmasın; +14: sıralama oku) */
  const dogalGenislik = el => { const r = document.createRange(); r.selectNodeContents(el); const cs = getComputedStyle(el); return r.getBoundingClientRect().width + 14 + (parseFloat(cs.paddingLeft) || 0) + (parseFloat(cs.paddingRight) || 0); };
  const kalin = w => w === 'bold' || w === 'bolder' || parseInt(w, 10) >= 600;
  const stil = (cs, ust) => ({ fg: hex(cs.color), b: kalin(cs.fontWeight), i: cs.fontStyle === 'italic', u: !!(ust && ust.u) || /line-through/.test(cs.textDecorationLine || cs.textDecoration || ''), pt: punto(cs.fontSize), buyuk: cs.textTransform === 'uppercase' });
  const dugmeMi = n => n.tagName === 'BUTTON' || n.classList.contains('btn') || n.classList.contains('ekyukle');
  const atla = n => dugmeMi(n) || n.tagName === 'SCRIPT' || n.tagName === 'STYLE' || n.tagName === 'TEMPLATE' || (n.tagName === 'I' && /(^|\s)fa/.test(n.className));

  /** hücre içeriğini biçimli parçalara ayırır: [{ t, fg, b, i, u, pt } | { br }] — blok öğeler (div, flex satırı, small.alt) satır sonu olur; düğme ve ikonlar atlanır; giriş kutularının değeri okunur */
  function parcalar(hucre, cs0) {
    const out = []; let dugme = false;
    const gez = (n, st) => {
      if (n.nodeType === 3) { let t = n.nodeValue.replace(/\s+/g, ' '); if (!t) return; if (st.buyuk) t = t.toLocaleUpperCase('tr-TR'); out.push({ t, fg: st.fg, b: st.b, i: st.i, u: st.u, pt: st.pt }); return; }
      if (n.nodeType !== 1) return;
      if (atla(n)) { if (dugmeMi(n)) dugme = true; return; }
      const cs = getComputedStyle(n); if (cs.display === 'none' || cs.visibility === 'hidden') return;
      const s = stil(cs, st), tag = n.tagName;
      if (tag === 'INPUT' || tag === 'SELECT' || tag === 'TEXTAREA') {
        const v = tag === 'SELECT' ? (n.selectedOptions[0] ? n.selectedOptions[0].textContent.trim() : '') : n.type === 'checkbox' || n.type === 'radio' ? (n.checked ? '✓' : '') : n.type === 'file' || n.type === 'hidden' ? '' : String(n.value ?? '').trim();
        if (v) out.push({ t: v, fg: s.fg, b: s.b, i: s.i, u: s.u, pt: s.pt }); return;
      }
      if (tag === 'BR') { out.push({ br: true }); return; }
      const blok = blokMu(cs.display), bosluk = () => out.push({ t: ' ', fg: s.fg, b: s.b, i: s.i, u: s.u, pt: s.pt });
      if (blok) out.push({ br: true }); else if (parseFloat(cs.marginLeft) > 0 || parseFloat(cs.paddingLeft) > 0) bosluk();
      for (const ch of n.childNodes) gez(ch, s);
      if (blok) out.push({ br: true }); else if (parseFloat(cs.marginRight) > 0 || parseFloat(cs.paddingRight) > 0) bosluk();
    };
    for (const ch of hucre.childNodes) gez(ch, stil(cs0, null));
    // satırlara böl: art arda satır sonları tek, satır başı / sonu boşlukları atılır, aynı biçimli komşu parçalar birleşir
    const satirlar = [[]];
    out.forEach(p => { if (p.br) { if (satirlar[satirlar.length - 1].length) satirlar.push([]); return; } const son = satirlar[satirlar.length - 1], o = son[son.length - 1]; if (o && o.fg === p.fg && o.b === p.b && o.i === p.i && o.u === p.u && o.pt === p.pt) o.t += p.t; else son.push({ ...p }); });
    const temiz = satirlar.map(s => { if (s.length) { s[0].t = s[0].t.replace(/^\s+/, ''); s[s.length - 1].t = s[s.length - 1].t.replace(/\s+$/, ''); } return s.filter(p => p.t !== ''); }).filter(s => s.length);
    const sonuc = []; temiz.forEach((s, i) => { if (i) sonuc.push({ br: true }); sonuc.push(...s); });
    return { parcalar: sonuc, dugme, satir: temiz.length };
  }

  /** "1.234 ₺" / "-12,5" / "% 12,00" / "7,5" → { n, f } (Excel sayı biçimi). Kodlar metin kalır: ayraçsız 6+ haneli sayı (sicil, TC), 0 ile başlayan. */
  function sayiCoz(t) {
    let s = (t || '').trim(), tur = null, m;
    if ((m = /^(-?[\d.]+(?:,\d+)?)\s*₺$/.exec(s))) { tur = 'tl'; s = m[1]; }
    else if ((m = /^%\s*(-?[\d.]+(?:,\d+)?)$/.exec(s))) { tur = 'yuzdeOn'; s = m[1]; }
    else if ((m = /^(-?[\d.]+(?:,\d+)?)\s*%$/.exec(s))) { tur = 'yuzdeSon'; s = m[1]; }
    if (!/^-?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$/.test(s)) return null;
    const [tam, ond] = s.split(','), hane = tam.replace(/[-.]/g, '');
    if (!tur && !ond && !tam.includes('.') && (hane.length >= 6 || /^0\d/.test(hane))) return null;
    const n = parseFloat(tam.replace(/\./g, '') + (ond ? '.' + ond : '')); if (!isFinite(n)) return null;
    const o = ond ? '.' + '0'.repeat(ond.length) : '';
    return { n, f: tur === 'tl' ? `#,##0${o} "₺"` : tur === 'yuzdeOn' ? `"% "#,##0${o}` : tur === 'yuzdeSon' ? `#,##0${o}"%"` : `#,##0${o}` };
  }
  /** kenarlık: 1px → ince (t), 2px+ → kalın (m) + renk */
  const kenar = (cs, yon) => { const w = parseFloat(cs['border' + yon + 'Width']) || 0; if (w < 1 || cs['border' + yon + 'Style'] === 'none') return null; const r = hex(cs['border' + yon + 'Color']); return r ? (w >= 2 ? 'm' : 't') + r : null; };

  /** tek hücre → sunucu hücresi (c / cs / rs yerleşim sonrası düzeltilir) */
  function hucreOku(h) {
    const cs = getComputedStyle(h.el), p = parcalar(h.el, cs), o = { c: h.c };
    if (h.cs > 1) o.cs = h.cs; if (h.rs > 1) o.rs = h.rs;
    const bg = hex(cs.backgroundColor); if (bg && bg !== BEYAZ) o.bg = bg;
    const hz = cs.textAlign; o.hz = hz === 'right' || hz === 'end' ? 'r' : hz === 'center' ? 'c' : 'l';
    const ic = h.el.firstElementChild; if (ic && h.el.children.length === 1) { const ics = getComputedStyle(ic); if (/flex/.test(ics.display) && ics.flexDirection === 'column' && ics.alignItems === 'flex-end') o.hz = 'r'; }   // R üstte / X altta hücre: içerik sağa yaslı
    const P = p.parcalar, metinler = P.filter(x => !x.br), ilk = metinler[0];
    const tekBicim = metinler.length > 0 && !P.some(x => x.br) && metinler.every(x => x.fg === ilk.fg && x.b === ilk.b && x.i === ilk.i && x.u === ilk.u && x.pt === ilk.pt);
    if (!metinler.length) o.v = '';
    else if (tekBicim) {
      const t = metinler.map(x => x.t).join(''), s = sayiCoz(t); if (s) { o.n = s.n; o.f = s.f; } else o.v = t;
      if (ilk.fg) o.fg = ilk.fg; if (ilk.b) o.b = true; if (ilk.i) o.i = true; if (ilk.u) o.u = true; if (ilk.pt !== 9) o.pt = ilk.pt;
    } else { o.z = P.map(x => x.br ? { br: true } : { t: x.t, fg: x.fg, b: x.b || undefined, i: x.i || undefined, u: x.u || undefined, pt: x.pt }); o.sar = true; }
    [['Left', 'sol'], ['Top', 'ust'], ['Right', 'sag'], ['Bottom', 'alt']].forEach(([y, k]) => { const v = kenar(cs, y); if (v) o[k] = v; });
    const g = /(rgba?\([^)]*\))\s+2px\s+0(?:px)?\s+0(?:px)?/.exec(cs.boxShadow || ''); if (g && !o.sag) { const r = hex(g[1]); if (r) o.sag = 'm' + r; }   // sabit sütun ayırıcı gölgesi → sağ kenarlık
    return { o, satir: p.satir, dugme: p.dugme, bos: !metinler.length };
  }

  /** ekrandaki <table> → sayfa tanımı */
  function tabloOku(tablo, o) {
    if (!tablo) return null;
    const gizli = e => getComputedStyle(e).display === 'none';
    const bolumler = []; if (tablo.tHead) bolumler.push(['bas', tablo.tHead]); for (const b of tablo.tBodies) bolumler.push(['govde', b]); if (tablo.tFoot) bolumler.push(['dip', tablo.tFoot]);
    const satirlar = []; bolumler.forEach(([tur, b]) => { for (const tr of b.rows) { if (gizli(tr) || tr.classList.contains('suzgec')) continue; if (tur === 'govde' && (tr.querySelector('.na-bos') || tr.classList.contains('detay'))) continue; satirlar.push({ tur, tr }); } });
    // ızgara: colspan / rowspan açılır, gizli hücreler (0 ₺ sütunları) atlanır
    const grid = [], hucreler = [];
    satirlar.forEach((s, r) => { grid[r] = grid[r] || []; let c = 0; for (const td of s.tr.children) { if (gizli(td)) continue; while (grid[r][c]) c++; const h = { el: td, r, c, cs: Math.max(1, td.colSpan || 1), rs: Math.max(1, td.rowSpan || 1), tur: s.tur }; hucreler.push(h); for (let i = 0; i < h.rs; i++) { grid[r + i] = grid[r + i] || []; for (let j = 0; j < h.cs; j++) grid[r + i][c + j] = h; } c += h.cs; } });
    const nSutun = Math.max(0, ...grid.map(g => g.length));
    hucreler.forEach(h => Object.assign(h, hucreOku(h)));
    // düğme sütunları (Kart, Fiş Önizleme…) ve başlıksız boş sütunlar atılır
    const basSatir = satirlar.filter(s => s.tur === 'bas').length, sonBas = basSatir - 1, at = new Set();
    for (let c = 0; c < nSutun; c++) {
      const th = sonBas >= 0 ? grid[sonBas][c] : null, thAd = th && th.cs === 1 ? (th.o.v || '').trim() : null;
      const govde = hucreler.filter(h => h.tur !== 'bas' && h.c === c && h.cs === 1), dolu = govde.some(h => !h.bos), dugme = govde.some(h => h.dugme);
      if ((th && th.el.dataset.k === 'kart') || (!dolu && (dugme || thAd === '' || /^(kart|personel kartı|işlem|işlemler)$/i.test(thAd || '')))) at.add(c);
    }
    const yeni = []; let k = 0; for (let c = 0; c < nSutun; c++) yeni[c] = at.has(c) ? -1 : k++;
    const sutunSayisi = k, cikti = satirlar.map(() => ({ h: [] }));
    hucreler.forEach(h => {
      let ilk = -1, n = 0; for (let j = 0; j < h.cs; j++) { const y = yeni[h.c + j]; if (y >= 0) { if (ilk < 0) ilk = y; n++; } }
      if (ilk < 0) return; h.o.c = ilk; if (n > 1) h.o.cs = n; else delete h.o.cs; cikti[h.r].h.push(h.o);
      if (h.rs === 1 && h.satir > 1) cikti[h.r].yk = Math.max(cikti[h.r].yk || 0, Math.round(h.satir * 12 + 4));   // çok satırlı hücre (R üstte / X altta) → satır yüksekliği
    });
    // sütun genişlikleri (ekran pikseli → Excel karakter birimi): son başlık satırından, yoksa gövde hücrelerinden
    const genislik = new Array(sutunSayisi).fill(0);
    const olc = h => { if (h.cs !== 1 || yeni[h.c] < 0 || genislik[yeni[h.c]]) return; const w = h.tur === 'bas' ? dogalGenislik(h.el) : h.el.getBoundingClientRect().width; if (w > 0) genislik[yeni[h.c]] = Math.round(Math.min(70, Math.max(3, w / 7 + 0.7)) * 10) / 10; };
    hucreler.filter(h => h.tur === 'bas' && h.r === sonBas).forEach(olc); hucreler.filter(h => h.tur !== 'bas').forEach(olc);
    for (let i = 0; i < sutunSayisi; i++) if (!genislik[i]) genislik[i] = 12;
    // sola sabit sütunlar (.sabit başlık hücreleri) → dondurulacak sütun sayısı
    let dondur = 0; if (sonBas >= 0) for (let c = 0; c < nSutun; c++) { const th = grid[sonBas][c]; if (th && th.cs === 1 && yeni[c] >= 0 && th.el.classList.contains('sabit')) dondur = yeni[c] + 1; }
    return { ad: o.ad, baslik: o.baslik, altBaslik: o.altBaslik, kpi: kpiOku(el(o.kpi)), not: notOku(el(o.not)), genislik, basSatir, dondurSutun: dondur, satirlar: cikti };
  }
  /** elle verilen satırlar (tablo olmayan bloklar, ör. kalem dağılımı çubukları): başlık satırı koyu, sayılar çözülür */
  function elleOku(o) {
    const bas = (o.basliklar || []).map((t, i) => ({ c: i, v: String(t).toLocaleUpperCase('tr-TR'), bg: '#1e293b', fg: '#ffffff', b: true, pt: 8, hz: i ? 'r' : 'l' }));
    const satirlar = (o.satirlar || []).map(r => ({ h: r.map((t, i) => { const s = sayiCoz(String(t ?? '')), h = { c: i, hz: s ? 'r' : 'l', alt: 't#e2e8f0' }; if (s) { h.n = s.n; h.f = s.f; } else h.v = String(t ?? ''); return h; }) }));
    return { ad: o.ad, baslik: o.baslik, altBaslik: o.altBaslik, kpi: kpiOku(el(o.kpi)), not: notOku(el(o.not)), genislik: (o.basliklar || []).map((t, i) => i ? 16 : 36), basSatir: bas.length ? 1 : 0, dondurSutun: 0, satirlar: (bas.length ? [{ h: bas }] : []).concat(satirlar) };
  }
  /** .na-kpi kutuları: etiket, değer, alt bilgi, renk çubuğu */
  const kpiOku = k => !k ? null : Array.from(k.children).map(d => { const t = s => { const e = d.querySelector(s); return e ? e.textContent.replace(/\s+/g, ' ').trim() : ''; }; return { l: t('.l'), v: t('.v'), s: t('.s'), renk: hex(getComputedStyle(d).borderLeftColor) }; }).filter(x => x.l || x.v);
  const notOku = n => n ? n.textContent.replace(/\s+/g, ' ').trim() : null;

  /** belgeyi kurar (indirmeden; test ve önizleme için) */
  function hazirla(dosya, sayfalar) {
    const belge = { dosya, sayfalar: (sayfalar || []).map(s => s.tablo ? tabloOku(el(s.tablo), s) : elleOku(s)).filter(Boolean) };
    if (!belge.sayfalar.length) throw new Error('Dışa aktarılacak tablo bulunamadı.');
    return belge;
  }
  /** belgeyi sunucuya gönderir, dosyayı indirir: yol /TabloExcel/Indir (.xlsx) ya da /TabloExcel/Pdf (.pdf) */
  async function gonder(yol, uzanti, etiket, dosya, sayfalar) {
    const belge = hazirla(dosya, sayfalar);
    const r = await fetch(yol, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(belge) });
    if (!r.ok) throw new Error((await r.text()) || (etiket + ' üretilemedi (' + r.status + ')'));
    const b = await r.blob(), a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = dosya + uzanti; document.body.appendChild(a); a.click(); a.remove(); setTimeout(() => URL.revokeObjectURL(a.href), 5000);
    return belge;
  }
  const indir = (dosya, sayfalar) => gonder('/TabloExcel/Indir', '.xlsx', 'Excel', dosya, sayfalar);
  const pdf = (dosya, sayfalar) => gonder('/TabloExcel/Pdf', '.pdf', 'PDF', dosya, sayfalar);
  /** Excel ve PDF düğmeleri: aynı sayfa tanımı fn('xlsx') ya da fn('pdf') ile kullanılır; pdfId boşsa yalnız Excel düğmesi bağlanır */
  function dugme(excelId, pdfId, fn) {
    const xl = el(excelId); if (xl) xl.addEventListener('click', () => fn('xlsx'));
    const pd = el(pdfId); if (pd) pd.addEventListener('click', () => fn('pdf'));
  }
  return { indir, pdf, dugme, hazirla, oku: tabloOku, sayiCoz };
})();
