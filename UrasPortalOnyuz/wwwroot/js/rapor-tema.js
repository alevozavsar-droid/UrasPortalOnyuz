// ============================================================
// RAPOR TEMASI (davranış) — bütün rapor sayfalarında (body.rapor-sayfasi):
//  * Sayfa başlığına konuya uygun renkli ikon karosu (başlıkta zaten ikon varsa dokunmaz).
//  * Tablo hücrelerindeki durum sözcükleri (Onaylandı, Hatalı, Beklemede, Aktif, Kapalı…) renkli rozete çevrilir.
//  * Sayısal hücreler sağa yaslanır; eksi tutarlar kırmızı vurgulanır. Sayısal sütunların başlığı ve toplam hücresi de
//    sağa yaslanır (başlık solda, rakam sağda kalınca sütunlar kaymış görünüyordu).
//  Sonradan (AJAX ile) dolan tablolar için gözlemci ile tekrar uygulanır; hücreler bir kez işaretlenir.
// ============================================================
(function () {
  'use strict';
  if (!document.body.classList.contains('rapor-sayfasi')) return;
  const KAPSAM_DISI = '.modal, .dropdown-menu, #kilavuz, .rozet-popover, .select2-container, .bootstrap-select';

  // ---- Durum sözlüğü ----
  const DURUM = [
    { s: 'ok', i: 'fa-circle-check', k: /^(onaylandı|onaylı|başarılı|tamamlandı|tamamlanan|aktif|etkin|açık|ödendi|tahsil edildi|kapatıldı|gönderildi|kabul|kabul edildi|uygun|evet|var|olumlu|geçti|yeşil|alacaklıyız|onay verildi)$/i },
    { s: 'hata', i: 'fa-circle-xmark', k: /^(reddedildi|red|hatalı|hata|iptal|iptal edildi|pasif|etkin değil|başarısız|gecikmiş|gecikti|vadesi geçti|vadesi geçmiş|uygunsuz|hayır|yok|olumsuz|kaldı|kırmızı|borçluyuz|silindi)$/i },
    { s: 'bekle', i: 'fa-hourglass-half', k: /^(beklemede|bekliyor|bekleyen|onay bekliyor|onay bekleyen|taslak|işlemde|devam ediyor|kısmi|kısmen|hazırlanıyor|sarı|planlandı|planlanıyor)$/i },
    { s: 'bilgi', i: 'fa-circle-info', k: /^(kapalı|sonuçlandı|arşiv|arşivlendi|bilgi|yeni|mavi|kısayol)$/i }
  ];
  const durumBul = t => DURUM.find(d => d.k.test(t));

  // ---- Başlık ikonu (anahtar sözcüğe göre) ----
  const IKONLAR = [
    [/fatura|e-belge|irsaliye|belge/i, 'fa-file-invoice'], [/çek|senet|ibraz|bordro/i, 'fa-money-check-dollar'], [/kasa|banka|nakit|ödeme|tahsilat|finekra/i, 'fa-vault'],
    [/mizan|muhasebe|yevmiye|mahsup|virman|hesap/i, 'fa-book-open'], [/kdv|vergi|beyanname/i, 'fa-percent'], [/cari|muhatap|müşteri|tedarikçi/i, 'fa-address-book'],
    [/stok|envanter|depo|malzeme|kalem|parti|sayım/i, 'fa-boxes-stacked'], [/üretim|reçete|ürün|silo|planlama|simülasyon/i, 'fa-industry'],
    [/kalite|kk|tutanak|numune/i, 'fa-vial-circle-check'], [/satış|sipariş|teslimat|sevk|prim/i, 'fa-cart-shopping'], [/satın ?alma|alış|alım|ithalat/i, 'fa-truck-ramp-box'],
    [/ihracat|dosya|gümrük/i, 'fa-ship'], [/aktarım|robot|servis|entegrasyon|denetim/i, 'fa-robot'], [/personel|maaş|bordro|zimmet|danışma|giriş-çıkış/i, 'fa-id-badge'],
    [/onay/i, 'fa-clipboard-check'], [/analiz|dashboard|performans|ciro|kar|gider|gelir/i, 'fa-chart-line'], [/ayar|yetki|tanım|profil|sap/i, 'fa-sliders'], [/mesaj/i, 'fa-comment-dots'],
    [/ai|asistan|gemini/i, 'fa-brain'], [/./, 'fa-chart-column']
  ];
  function baslikIkonu() {
    const kap = document.querySelector('.main-content') || document.body;
    const h = Array.from(kap.querySelectorAll('h1, h2, h3, h4, h5')).find(x => x.textContent.trim().length > 2 && !x.closest(KAPSAM_DISI) && x.offsetParent !== null);
    if (!h || h.dataset.rtIkon) return;
    h.dataset.rtIkon = '1';
    if (h.querySelector('i, svg, img, .rt-ikon')) return;          // zaten ikonlu
    const metin = h.textContent + ' ' + document.title;
    const ic = (IKONLAR.find(p => p[0].test(metin)) || IKONLAR[IKONLAR.length - 1])[1];
    const sp = document.createElement('span'); sp.className = 'rt-ikon'; sp.innerHTML = '<i class="fas ' + ic + '"></i>';
    h.insertBefore(sp, h.firstChild);
  }

  // ---- Hücreler ----
  const SAYI = /^[-+]?[\d.]+(,\d+)?\s?(₺|TL|\$|€|%)?$|^[-+]?[\d,]+(\.\d+)?\s?(₺|TL|\$|€|%)?$/;
  let islenen = 0;
  function hucreler(kok) {
    const tablolar = Array.from((kok || document).querySelectorAll('table')).filter(t => !t.closest(KAPSAM_DISI) && t.tBodies.length && t.tHead && t.tHead.rows.length && t.tHead.rows[0].cells.length >= 3);
    let sayac = 0;
    for (const t of tablolar) {
      const satirlar = Array.from(t.tBodies[0].rows).concat(t.tFoot ? Array.from(t.tFoot.rows) : []);   // toplam satırındaki rakamlar da sağa yaslanır
      for (const r of satirlar) {
        for (const c of Array.from(r.cells)) {
          if (c.dataset.rt) continue;
          if (++sayac > 4000) return;   // çok büyük tablolarda kalanı sonraki tura bırak
          c.dataset.rt = '1';
          if (c.children.length) continue;          // içinde düğme/rozet/bağlantı olan hücreye dokunma
          const m = c.textContent.replace(/\s+/g, ' ').trim();
          if (!m) continue;
          const d = durumBul(m);
          if (d) { c.innerHTML = '<span class="rt-durum rt-' + d.s + '"><i class="fas ' + d.i + '"></i>' + m.replace(/&/g, '&amp;').replace(/</g, '&lt;') + '</span>'; continue; }
          if (SAYI.test(m) && /\d/.test(m)) {
            c.classList.add('rt-sayi');
            if (/^-|^\(.*\)$/.test(m) || /^-/.test(m)) c.classList.add('rt-negatif');
          }
        }
      }
    }
    islenen += sayac;
    for (const t of tablolar) basliklariHizala(t);
  }

  // ---- Başlık hizası: gövdesi ağırlıklı sayısal olan sütunun başlığı (ve varsa toplam satırı hücresi) da sağa yaslanır ----
  // Aksi halde başlık solda, rakamlar sağda kalıyor ve sıralama ikonuyla birlikte sütunlar kaymış görünüyordu.
  function basliklariHizala(t) {
    const govde = t.tBodies[0]; if (!govde || !govde.rows.length) return;
    const n = t.tHead.rows[0].cells.length; if (!n) return;
    const sayi = new Array(n).fill(0), toplam = new Array(n).fill(0);
    for (const r of Array.from(govde.rows)) {
      if (r.cells.length !== n) continue;                      // colspan'lı / detay satırları sayılmaz
      for (let i = 0; i < n; i++) {
        const c = r.cells[i];
        if (!c.dataset.rt || c.children.length || !c.textContent.trim()) continue;
        toplam[i]++; if (c.classList.contains('rt-sayi')) sayi[i]++;
      }
    }
    const sayisal = i => toplam[i] >= 2 && sayi[i] / toplam[i] >= 0.6;
    const uygula = satirlar => {
      for (const r of satirlar) {
        if (r.classList.contains('filter-row')) continue;
        let kol = 0;   // colspan'lı başlık / toplam satırlarında mantıksal sütun konumu
        for (const h of Array.from(r.cells)) {
          const genis = h.colSpan || 1;
          if (genis === 1 && kol < n) {
            if (sayisal(kol)) { h.classList.add('rt-sayi'); h.dataset.rtSayi = '1'; }
            else if (h.dataset.rtSayi) { h.classList.remove('rt-sayi'); delete h.dataset.rtSayi; }   // veri değişince geri al
          }
          kol += genis;
        }
      }
    };
    uygula(Array.from(t.tHead.rows));
    if (t.tFoot) uygula(Array.from(t.tFoot.rows));
  }

  // ---- Özet (KPI) kartlarını sezgisel işaretle: kısa metinli, büyük puntolu değer içeren, yüksek kutular ----
  const KPI_SECICI = '.card, [class*="card"], [class*="kpi"], [class*="stat"], [class*="ozet"], [class*="summary"], [class*="widget"]';
  const KPI_IC_PARCA = /(^|\s)[\w-]*(-body|-title|-value|-val|-label|-lbl|-icon|-desc|-header|-footer|-text|-count|-amount|-grid|-wrapper|-row|-bar|-group|-head|-metric)(\s|$)/;
  const kpiAdayi = el => el.matches(KPI_SECICI) && !KPI_IC_PARCA.test(el.getAttribute('class') || '');
  function kpiKartlari() {
    const kap = document.querySelector('.main-content') || document.body;
    kap.querySelectorAll(KPI_SECICI).forEach(el => {
      if (el.dataset.rtKpi || !kpiAdayi(el) || el.closest(KAPSAM_DISI) || el.querySelector('table, form, input, select, textarea, .btn, button, a.btn')) return;
      if (Array.from(el.querySelectorAll(KPI_SECICI)).some(kpiAdayi)) return;   // iç içe kart: yalnızca en içteki değerlendirilir
      const metin = (el.textContent || '').replace(/\s+/g, ' ').trim();
      if (!metin || metin.length > 160 || el.offsetHeight < 84 || el.offsetWidth > 700) return;
      let deger = null, enBuyuk = 0;
      el.querySelectorAll('*').forEach(x => {
        if (!x.textContent.trim() || x.children.length > 1) return;
        const fs = parseFloat(getComputedStyle(x).fontSize);
        if (fs > enBuyuk) { enBuyuk = fs; deger = x; }
      });
      if (!deger || enBuyuk < 17) return;
      el.dataset.rtKpi = '1';
      el.classList.add('rt-kpi');
      deger.classList.add('rt-kpi-deger');
      el.querySelectorAll('*').forEach(x => {
        if (x === deger) return;
        const cs = getComputedStyle(x);
        if ((x.tagName === 'I' || x.tagName === 'SVG' || x.tagName === 'IMG') && cs.position === 'absolute') { x.classList.add('rt-kpi-dekor'); return; }
        if (x.children.length === 0 && x.textContent.trim()) {
          if (cs.textTransform === 'uppercase' || parseFloat(cs.fontSize) <= 12) x.classList.add(x.compareDocumentPosition(deger) & Node.DOCUMENT_POSITION_FOLLOWING ? 'rt-kpi-baslik' : 'rt-kpi-aciklama');
        }
      });
    });
  }

  function uygula() { try { baslikIkonu(); hucreler(); kpiKartlari(); } catch (e) { /* sessiz */ } }
  let z = null;
  function basla() {
    uygula();
    new MutationObserver(m => {
      if (!m.some(x => x.addedNodes.length)) return;
      clearTimeout(z); z = setTimeout(uygula, 250);
    }).observe(document.body, { childList: true, subtree: true });
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => setTimeout(basla, 350));
  else setTimeout(basla, 350);
})();
