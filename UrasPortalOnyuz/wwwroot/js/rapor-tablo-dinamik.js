// ============================================================
// DİNAMİK RAPOR TABLOSU — bütün raporlarda ortak: başlığa tıklayınca sıralama (artan / azalan),
// başlığın altında sütun başına filtre satırı (az değerli sütunda "Tümü" açılır listesi, çok değerlide arama kutusu).
//
// Kurallar:
//  * Kendi filtre satırı (tr.filter-row) olan tablolara filtre eklenmez; kendi sıralama simgesi (.sort-icon) olan
//    başlıklara sıralama eklenmez. Böylece elle yazılmış raporlarla çakışmaz.
//  * Gruplu (çok satırlı, colspan'lı) başlıklarda sütunlarla birebir eşleşen son başlık satırı kullanılır.
//  * Detay / açılır alt satırlar (colspan'lı ya da sınıfında detay/detail/child geçen) ana satırıyla birlikte taşınır;
//    "Toplam" satırları sıralamada en altta sabit kalır, filtreden etkilenmez.
//  * Sonradan (AJAX ile) dolan / yeniden çizilen tablolar MutationObserver ile yakalanır; seçenek listeleri tazelenir.
//  * Modal, modül sayfası, ana sayfa kartları, iç içe tablolar ve 3 sütundan az tablolar kapsam dışıdır.
//  * Kabuğun sütun yöneticisi / Excel aktarımı "filter-row" satırını tanır; aynı sınıf kullanılır.
//  * data-dinamik="kapali" verilen tablo atlanır.
// ============================================================
(function () {
  'use strict';
  const KAPSAM_DISI = '.modal, .km-modul, .gh-ana, .dropdown-menu, #kilavuz, .rozet-popover';
  const MAX_SECENEK = 40;
  const DETAY_SINIF = /detay|detail|child|alt-satir|expand|collapse|accordion|sub-row|subrow/i;
  const OZET_SINIF = /toplam|total|footer|ozet|summary|genel/i;
  const OZET_METIN = /^(genel\s+)?(toplam|total|ara\s*toplam|dip\s*toplam)\b/i;

  const tr = s => String(s == null ? '' : s).replace(/İ/g, 'i').replace(/I/g, 'ı').toLocaleLowerCase('tr-TR');
  const metin = td => (td ? td.textContent : '').replace(/\s+/g, ' ').trim();

  // "1.234,56" / "1234.56" / "-1.234,56 ₺" / "%12,5"
  function sayi(s) {
    const t = s.replace(/[₺$€%\s]/g, '');
    if (!/^[-+]?[\d.,]+$/.test(t) || !/\d/.test(t)) return null;
    let n;
    if (/,\d{1,2}$/.test(t)) n = parseFloat(t.replace(/\./g, '').replace(',', '.'));         // TR: binlik nokta, ondalık virgül
    else if (/\.\d{1,2}$/.test(t) && !/,/.test(t)) n = parseFloat(t.replace(/,/g, ''));     // EN: ondalık nokta
    else n = parseFloat(t.replace(/\./g, '').replace(/,/g, ''));
    return isNaN(n) ? null : n;
  }
  // "19.09.2026", "19.09.2026 14:30", "2026-09-19", "19/09/2026"
  function tarih(s) {
    let m = /^(\d{1,2})[./](\d{1,2})[./](\d{4})(?:\s+(\d{1,2}):(\d{2}))?$/.exec(s);
    if (m) return new Date(+m[3], +m[2] - 1, +m[1], +(m[4] || 0), +(m[5] || 0)).getTime();
    m = /^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2}))?/.exec(s);
    if (m) return new Date(+m[1], +m[2] - 1, +m[3], +(m[4] || 0), +(m[5] || 0)).getTime();
    return null;
  }
  function turBul(degerler) {
    const dolu = degerler.filter(v => v !== '');
    if (!dolu.length) return 'metin';
    if (dolu.every(v => tarih(v) !== null)) return 'tarih';
    if (dolu.every(v => sayi(v) !== null)) return 'sayi';
    return 'metin';
  }
  function anahtar(v, tur) {
    if (tur === 'sayi') { const n = sayi(v); return n === null ? -Infinity : n; }
    if (tur === 'tarih') { const t = tarih(v); return t === null ? -Infinity : t; }
    return tr(v);
  }

  // Başlık satırı: tek satırlı thead'de ilk satır; gruplu (colspan'lı) başlıkta sütunlarla birebir eşleşen son satır.
  function baslikSatiri(table) {
    if (table.dataset.dinamik === 'kapali' || table.dataset.dinamikHazir) return null;
    if (table.closest(KAPSAM_DISI)) return null;
    if (table.parentElement && table.parentElement.closest('table')) return null;   // iç içe tablo
    const thead = table.tHead; if (!thead || !thead.rows.length) return null;
    const satirlar = Array.from(thead.rows).filter(r => !r.classList.contains('filter-row'));
    const govdeSutun = table.tBodies[0] && table.tBodies[0].rows.length ? Math.max(...Array.from(table.tBodies[0].rows).map(r => r.cells.length)) : 0;
    for (let i = satirlar.length - 1; i >= 0; i--) {
      const r = satirlar[i], hucreler = Array.from(r.cells);
      if (hucreler.length < 3) continue;
      if (hucreler.some(c => c.colSpan > 1)) continue;
      if (govdeSutun && hucreler.length !== govdeSutun) continue;   // gövde sütunlarıyla eşleşmeyen satır (rowspan'lı grup başlığı)
      return r;
    }
    return null;
  }

  function hazirla(table) {
    const bas = baslikSatiri(table);
    if (!bas) return;
    table.dataset.dinamikHazir = '1';
    const thead = table.tHead, thler = Array.from(bas.cells);
    const sutunSayisi = thler.length;
    // Sayfanın kendi filtresi var mı? tr.filter-row ya da başlık satırlarından birinde giriş alanı / açılır liste / filtre düğmesi
    const kendiFiltresi = !!thead.querySelector('tr.filter-row') ||
      Array.from(thead.rows).some(r => r !== bas && r.querySelector('input, select, .dropdown-toggle, .btn')) ||
      !!bas.querySelector('input:not([type="checkbox"]), select, .dropdown-toggle, .btn');
    const durum = { kolon: -1, yon: 'none' };
    const govde = () => table.tBodies[0];

    // Satır sınıflandırma: ana satır / ana satıra bağlı detay satırı / en altta sabit özet satırı
    function ozetMi(r) {
      if (OZET_SINIF.test(r.className)) return true;
      const ilk = metin(r.cells[0]) || metin(r.cells[1]);
      return OZET_METIN.test(ilk) && (r.querySelector('b, strong, th') !== null || r.cells.length !== sutunSayisi || Array.from(r.cells).some(c => c.colSpan > 1));
    }
    function detayMi(r) {
      if (r.cells.length !== sutunSayisi) return true;
      if (Array.from(r.cells).some(c => c.colSpan > 1)) return true;
      return DETAY_SINIF.test(r.className);
    }
    function gruplar() {
      const tb = govde(); if (!tb) return { gruplar: [], ozetler: [] };
      const out = [], ozetler = []; let son = null;
      Array.from(tb.rows).forEach(r => {
        if (ozetMi(r)) { ozetler.push(r); return; }
        if (detayMi(r)) { if (son) son.ekler.push(r); else ozetler.push(r); return; }
        son = { ana: r, ekler: [] }; out.push(son);
      });
      return { gruplar: out, ozetler };
    }
    const anaSatirlar = () => gruplar().gruplar.map(g => g.ana);

    // ---- Sıralama ----
    thler.forEach((th, i) => {
      if (th.querySelector('.sort-icon, [class*="sort"], input, select, button, .dropdown-toggle')) return;   // sayfanın kendi sıralaması / filtresi
      if (th.hasAttribute('onclick') || th.dataset.sort !== undefined || th.dataset.sortable !== undefined || th.dataset.order !== undefined) return;
      if (!metin(th)) return;
      const ic = document.createElement('i');
      ic.className = 'fas fa-sort dinamik-sirala';
      th.appendChild(ic);
      th.classList.add('dinamik-baslik');
      th.addEventListener('click', e => {
        if (e.target.closest('.resizable-column-handle, .favorite-icon')) return;
        const yeniYon = durum.kolon === i && durum.yon === 'asc' ? 'desc' : 'asc';
        durum.kolon = i; durum.yon = yeniYon;
        thler.forEach(x => { const s = x.querySelector('.dinamik-sirala'); if (s) s.className = 'fas fa-sort dinamik-sirala'; });
        ic.className = 'fas ' + (yeniYon === 'asc' ? 'fa-sort-up' : 'fa-sort-down') + ' dinamik-sirala aktif';
        const g = gruplar(); if (!g.gruplar.length) return;
        if (g.gruplar.some(x => Array.from(x.ana.cells).some(c => c.rowSpan > 1))) return;   // rowspan'lı satırlar bozulmasın
        const tur = turBul(g.gruplar.map(x => metin(x.ana.cells[i])));
        const liste = g.gruplar.map((x, n) => ({ x, n, k: anahtar(metin(x.ana.cells[i]), tur) }));
        liste.sort((a, b) => {
          let c = tur === 'metin' ? a.k.localeCompare(b.k, 'tr') : (a.k < b.k ? -1 : a.k > b.k ? 1 : 0);
          if (c === 0) c = a.n - b.n;
          return yeniYon === 'asc' ? c : -c;
        });
        const tb = govde();
        liste.forEach(l => { tb.appendChild(l.x.ana); l.x.ekler.forEach(r => tb.appendChild(r)); });
        g.ozetler.forEach(r => tb.appendChild(r));
      });
    });

    // ---- Filtre satırı ----
    if (kendiFiltresi) return;
    const filtreler = new Array(sutunSayisi).fill('');
    const satir = document.createElement('tr');
    satir.className = 'filter-row dinamik-filtre';
    const hucreler = thler.map(th => {
      const td = document.createElement('th');
      td.className = 'dinamik-filtre-hucre';
      if (!metin(th) || th.querySelector('input[type="checkbox"]')) { satir.appendChild(td); return td; }
      td.innerHTML = '<input type="text" class="form-control form-control-sm dinamik-giris" placeholder="Ara…" aria-label="' + metin(th).replace(/"/g, '&quot;') + ' filtresi">';
      satir.appendChild(td);
      return td;
    });
    thead.appendChild(satir);

    function goster(r, acik) {
      // Kabuğun hızlı araması da display kullanır; yalnızca kendi gizlediğimizi geri açarız.
      if (!acik) { r.style.display = 'none'; r.dataset.dinamikGizli = '1'; }
      else if (r.dataset.dinamikGizli) { r.style.display = ''; delete r.dataset.dinamikGizli; }
    }
    function uygula() {
      const aktif = filtreler.map((f, i) => ({ i, f: tr(f), tam: hucreler[i].querySelector('select') !== null })).filter(x => x.f !== '');
      gruplar().gruplar.forEach(g => {
        let acik = true;
        for (const a of aktif) {
          const v = tr(metin(g.ana.cells[a.i]));
          if (a.tam ? v !== a.f : v.indexOf(a.f) < 0) { acik = false; break; }
        }
        goster(g.ana, acik);
        // Detay satırı: ana satır gizlenince gizlenir; ana satır açılınca sayfanın kendi aç/kapa durumuna dokunulmaz.
        g.ekler.forEach(r => { if (!acik) goster(r, false); else if (r.dataset.dinamikGizli) goster(r, true); });
      });
      table.dispatchEvent(new CustomEvent('dinamikFiltre', { bubbles: true }));
    }

    // Az değerli sütunlarda açılır liste, çok değerlide arama kutusu; tablo dolunca yeniden değerlendirilir.
    function secenekleriYenile() {
      const satirlar = anaSatirlar();
      if (!satirlar.length) return;
      hucreler.forEach((td, i) => {
        const giris = td.querySelector('.dinamik-giris'); if (!giris) return;
        const degerler = Array.from(new Set(satirlar.map(r => metin(r.cells[i])).filter(v => v !== '')));
        const listeUygun = degerler.length > 0 && degerler.length <= MAX_SECENEK && satirlar.length >= 2 && degerler.length < satirlar.length;
        if (listeUygun) {
          const tur = turBul(degerler);
          degerler.sort((a, b) => tur === 'metin' ? a.localeCompare(b, 'tr') : (anahtar(a, tur) - anahtar(b, tur)));
          if (giris.tagName !== 'SELECT') {
            const sel = document.createElement('select');
            sel.className = 'form-select form-select-sm dinamik-giris';
            sel.setAttribute('aria-label', giris.getAttribute('aria-label') || '');
            td.replaceChild(sel, giris);
            sel.addEventListener('change', () => { filtreler[i] = sel.value; uygula(); });
          }
          const sel = td.querySelector('select');
          const mevcut = sel.value;
          sel.innerHTML = '<option value="">Tümü</option>' + degerler.map(v => '<option>' + v.replace(/&/g, '&amp;').replace(/</g, '&lt;') + '</option>').join('');
          if (degerler.includes(mevcut)) sel.value = mevcut; else { sel.value = ''; if (filtreler[i]) { filtreler[i] = ''; uygula(); } }
        } else if (giris.tagName !== 'INPUT') {
          const inp = document.createElement('input');
          inp.type = 'text'; inp.className = 'form-control form-control-sm dinamik-giris'; inp.placeholder = 'Ara…';
          td.replaceChild(inp, giris);
          filtreler[i] = '';
          inp.addEventListener('input', () => { filtreler[i] = inp.value.trim(); uygula(); });
        }
      });
    }
    hucreler.forEach((td, i) => {
      const inp = td.querySelector('input.dinamik-giris');
      if (inp) inp.addEventListener('input', () => { filtreler[i] = inp.value.trim(); uygula(); });
    });
    secenekleriYenile();

    // Tablo sonradan dolarsa / yeniden çizilirse seçenekler tazelenir, filtre yeniden uygulanır.
    let zaman = null;
    const gozle = new MutationObserver(() => {
      clearTimeout(zaman);
      zaman = setTimeout(() => { secenekleriYenile(); if (filtreler.some(f => f)) uygula(); }, 150);
    });
    gozle.observe(table, { childList: true, subtree: true, characterData: true });
  }

  function tara(kok) {
    (kok || document).querySelectorAll('table').forEach(t => { try { hazirla(t); } catch (e) { /* tek tablo hatası diğerlerini durdurmasın */ } });
  }

  function basla() {
    tara();
    // Sonradan eklenen tablolar (AJAX ile çizilen raporlar)
    let z = null;
    new MutationObserver(m => {
      if (!m.some(x => x.addedNodes.length)) return;
      clearTimeout(z); z = setTimeout(() => tara(), 200);
    }).observe(document.body, { childList: true, subtree: true });
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => setTimeout(basla, 300));
  else setTimeout(basla, 300);
})();
