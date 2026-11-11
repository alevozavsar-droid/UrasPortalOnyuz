// ============================================================
// TABLO HİZALAMA (ortak) — Konsolide Finansal Takip Paneli, Kasa Nakit Akışı, Personel Maliyet Raporu, Bütçe Raporu.
//   Her hücre ortada, başlık ile gövde aynı (kullanıcı kararı: "tüm raporlarda ortaya hizalanacak her şey").
//   Gövde satır hizası: her hücrenin İLK satırı aynı taban çizgisinde; çok satırlı hücre ile tek satırlı komşusu kaymaz.
//   Başlıklar alt çizgide hizalanır: tek satırlı ile çok satırlı başlık aynı alt çizgiye oturur ("biri yukarıda biri aşağıda olmasın").
//   Çok satırlı başlık en çok 2 satır: üst satır kısa, alt satır üst satırdan uzun ya da eşit, genişlik en dar kalır
//     (Gelir / Vergisi Matrahı · Asgari Ücret / Vergi İstisnası · Malullük, Yaşlılık / ve Ölüm Primi (İşçi)).
//     Kırılım <br class="bsk"> ile yapılır; başlık metni değişmez.
//   Tutar sütunlarındaki düz sayılara ₺ eklenir; sayaç (Sıra, Gün, Adet, Kişi …) ve kimlik (Sicil, SGK Numarası …) sütunları işaretsiz kalır. Dövizli tutarlar kendi işaretini ekran betiğinde taşır.
//   Tablo her yeniden çizildiğinde (gövde ya da başlık değişince) hizalanır; ekran betiklerine dokunmaz.
//   data-dikey="orta" olan tabloda (Kredi Raporu, 11 Kas 2026) her hücre dikeyde de ortalanır (başlık dahil).
//   11 Kas 2026 (kullanıcı: "yıl sütununda ₺ yazıyor, bu gibi durumlara dikkat et"): Yıl / Dosya / Pay / Kalan Gün başlıkları sayaçtır; adet hücresine ekran betiği class="sayac" da verebilir.
// ============================================================
(() => {
  'use strict';
  const SECICI = 'table.na-tablo, table.table';
  const SAYI = /^[+-]?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$/;   // 1.565.000,00 · -120.495 · +902.715 · 3,50 (tarih 04.11.2026 değil)
  const PARA = /^-?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?\s*(₺|\$|€|£|%)?$/;   // işaretli tutar: 190.100,00 ₺ · 12 %
  const BASLAYAN = /^-?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?\s*(₺|\$|€|£)/;    // tutar + alt satır: 190.100,00 ₺ 16.000 $ ×40,19
  const TARIH = /\d{2}\.\d{2}\.\d{4}/;
  const SAYAC = /^(Sıra|No|#|Ver\.?|Dönem|Oran|Yıl|Dosya|Pay|Kalan Gün)$|\(Gün\)|Çalışılan Gün|Prim Gün|Saat$|\(Saat\)|\(sa\)|\bKur\b|Oran %|%\s*\(|^%|Çarpan|^Pay|\(Yıl\)|\(Ay\)|Ödemesiz Ay|Taksit Sayısı|Taksit Adedi|Geciken Taksit|Ödenen Taksit|Taksit No|Adet|Kişi|Personel|Sicil|Satır|Versiyon|Güncel|Numara|Kimlik|IBAN|\bNo\b/i;   // "(Saat)" (9 Kas 2026): Fazla Mesai (Saat) sayaçtır; "(Ay)", Ödemesiz Ay, Taksit Sayısı / Adedi / No, Geciken / Ödenen Taksit (Kredi Raporu) ₺ almaz
  const BOS = ['', '—', '-'];
  const PAY = 3;   // başlık sütununa eklenen pay (px): yazı tipi ölçüsündeki küçük farkları karşılar
  /** hücrenin tek dolu metin düğümü (renkli span içinde de olabilir); birden çok metin varsa null */
  const tekYazi = c => { const w = document.createTreeWalker(c, NodeFilter.SHOW_TEXT); let d = null, n; while ((n = w.nextNode())) { if (!n.nodeValue.trim()) continue; if (d) return null; d = n; } return d; };

  const olcu = document.createElement('canvas').getContext('2d');
  /** başlığın tarayıcı yazı tipiyle ölçümü: F tek satır genişliği; icerik en dar iki satırlı kırılımın içerik genişliği (alt satır üst satırdan uzun ya da eşit);
   *  i üst satırdaki sözcük sayısı; kutu kenar boşluğu (border-box için); sozcuk sözcük sayısı. Sıralama oku (::after) son sözcüğe bağlanır. */
  function baslikBilgi(th) {
    const cs = getComputedStyle(th);
    olcu.font = `${cs.fontStyle} ${cs.fontWeight} ${cs.fontSize} ${cs.fontFamily}`;
    if ('letterSpacing' in olcu) olcu.letterSpacing = cs.letterSpacing;
    const buyuk = cs.textTransform === 'uppercase';
    const olc = s => olcu.measureText(buyuk ? s.toLocaleUpperCase('tr-TR') : s).width;
    const kelime = th.textContent.trim().split(/\s+/);
    const ikon = getComputedStyle(th, '::after').content;
    if (ikon && ikon !== 'none' && ikon !== 'normal' && ikon !== '""') kelime[kelime.length - 1] += ikon.replace(/^"|"$/g, '');
    const dolgu = parseFloat(cs.paddingLeft) + parseFloat(cs.paddingRight) + parseFloat(cs.borderLeftWidth) + parseFloat(cs.borderRightWidth);
    let secim = null;
    for (let i = 1; i < kelime.length; i++) {
      const ust = olc(kelime.slice(0, i).join(' ')), alt = olc(kelime.slice(i).join(' '));
      const uygun = alt >= ust;   // alt satır üst satırdan uzun ya da eşit
      const sozcukUygun = kelime.length - i >= i;   // ve alt satırda en az o kadar sözcük var (3 sözcük: 1 + 2; 4 sözcük: 2 + 2)
      const oncelik = uygun && sozcukUygun ? 0 : uygun ? 1 : 2, deger = uygun ? alt : Math.max(ust, alt);
      if (!secim || oncelik < secim.oncelik || (oncelik === secim.oncelik && deger < secim.deger)) secim = { i, oncelik, deger };
    }
    const F = olc(kelime.join(' '));
    return { F, icerik: secim ? secim.deger : F, i: secim ? secim.i : 0, kutu: cs.boxSizing === 'border-box' ? dolgu : 0, sozcuk: kelime.length };
  }

  /** iki satırlı başlık: i. sözcükten başlayan alt satır için satır sonu; ayırıcı boşluk metinde kalır, başlık metni değişmez */
  function kirilimEkle(th, i) {
    const w = document.createTreeWalker(th, NodeFilter.SHOW_TEXT);
    let n, gorulen = false, sayac = 0;
    while ((n = w.nextNode())) {
      const re = /\S+|\s+/g;
      let m;
      while ((m = re.exec(n.nodeValue))) {
        if (m[0].trim()) { gorulen = true; continue; }
        if (!gorulen) continue;
        if (++sayac === i) {
          const br = document.createElement('br');
          br.className = 'bsk';
          const sonra = n.splitText(m.index);
          sonra.parentNode.insertBefore(br, sonra);
          return;
        }
      }
    }
  }
  function kirilimSil(th) {
    if (!th.querySelector('br.bsk')) return;
    th.querySelectorAll('br.bsk').forEach(b => b.remove());
    th.normalize();
  }

  /** başlık satırı: çok satırlı başlıklar kırılım kuralıyla iki satıra bölünür; sütun iki satıra yetmiyorsa genişletilir */
  function baslikDuzeni(satir) {
    const hucreler = Array.from(satir.cells).filter(th => th.colSpan <= 1 && th.textContent.trim() && th.offsetParent !== null);
    hucreler.forEach(kirilimSil);
    const bilgi = hucreler.map(th => {
      const cs = getComputedStyle(th);
      return { th, b: baslikBilgi(th), c: th.clientWidth - parseFloat(cs.paddingLeft) - parseFloat(cs.paddingRight) };
    });
    bilgi.forEach(({ th, b, c }) => {
      if (b.sozcuk < 2 || b.F <= c) return;   // tek sözcük ya da tek satıra sığıyor
      th.style.minWidth = Math.ceil(b.icerik + PAY + b.kutu) + 'px';   // kırılım eklenince sütun daralsa da alt satır sığsın
      if (getComputedStyle(th.closest('table')).tableLayout === 'fixed') th.closest('table').style.tableLayout = 'auto';   // sabit düzende min-width yok sayılır; tablo otomatik düzene geçer
      kirilimEkle(th, b.i);
    });
  }

  /** tablonun her hücresini ortalar; gövdede ilk satır taban çizgisine, başlıkta alt çizgiye hizalar; tutar sütunlarındaki düz sayılara ₺ ekler */
  function hizala(tablo) {
    const thead = tablo.tHead;
    const dikeyOrta = tablo.dataset.dikey === 'orta';   // 11 Kas 2026 (Kredi Raporu: "hem satır hem sütun ortala"): başlık ve gövde hücreleri dikeyde de ortada; öbür raporlarda ilk satır taban çizgisi
    for (const tr of tablo.rows) {
      const basta = tr.parentNode === thead;
      for (const c of tr.cells) {
        c.style.setProperty('text-align', 'center', 'important');
        c.style.setProperty('vertical-align', dikeyOrta ? 'middle' : basta ? 'bottom' : 'baseline', 'important');
      }
    }
    const bas = thead && thead.rows.length ? thead.rows[thead.rows.length - 1] : null;
    if (!bas) return;
    baslikDuzeni(bas);
    // mantıksal sütun: colspan'lı hücre kapladığı sütunların hepsinde yer alır (toplam satırı fiziksel değil, mantıksal sütuna bağlanır)
    const kolon = tr => { const m = []; let L = 0; for (const c of tr.cells) { m[L] = c; L += c.colSpan || 1; } return m; };
    const basKolon = kolon(bas);
    const govde = Array.from(tablo.tBodies).flatMap(b => Array.from(b.rows)).concat(tablo.tFoot ? Array.from(tablo.tFoot.rows) : [])
      .filter(r => r.cells.length > 1 && !r.querySelector('.na-bos'));
    const govdeKolon = govde.map(kolon);
    for (let i = 0; i < basKolon.length; i++) {
      const th = basKolon[i];
      if (!th || th.colSpan > 1) continue;
      const hucreler = govdeKolon.map(k => k[i]).filter(c => c && c.colSpan === 1);
      const dolu = hucreler.filter(c => !BOS.includes(c.textContent.trim()));
      const metin = c => c.textContent.trim();
      const sayisal = dolu.filter(c => PARA.test(metin(c)) || BASLAYAN.test(metin(c)) || ((c.classList.contains('s') || c.classList.contains('rt-sayi') || c.classList.contains('para')) && !TARIH.test(metin(c))));
      const baslik = th.textContent.trim().replace(/\s+/g, ' ');
      if (dolu.length && sayisal.length * 2 >= dolu.length && !SAYAC.test(baslik)) {
        dolu.filter(c => SAYI.test(metin(c)) && !c.classList.contains('sayac')).forEach(c => { const d = tekYazi(c); if (d) d.nodeValue = d.nodeValue.trim() + ' ₺'; });
      }
    }
  }

  // tablo her çizildiğinde (gövde ya da başlık değişince) bir kez hizalanır; kendi değişikliklerimizin kayıtları atılır
  const bekleyen = new Map();
  const planla = tablo => {
    if (bekleyen.has(tablo)) return;
    bekleyen.set(tablo, requestAnimationFrame(() => { bekleyen.delete(tablo); hizala(tablo); gozlem.takeRecords(); }));
  };
  const gozlem = new MutationObserver(kayitlar => {
    for (const k of kayitlar) {
      const e = k.target.nodeType === 1 ? k.target : k.target.parentElement;
      const t = e && e.closest(SECICI);
      if (t) planla(t);
    }
  });
  gozlem.observe(document.body, { childList: true, subtree: true });
  document.querySelectorAll(SECICI).forEach(planla);
  // pencere genişliği değişince sütunlar yeniden dağılır: başlık kırılımı yeniden hesaplanır
  let boyut = 0;
  window.addEventListener('resize', () => { clearTimeout(boyut); boyut = setTimeout(() => document.querySelectorAll(SECICI).forEach(planla), 150); });
  window.tabloBaslik = { bilgi: baslikBilgi };   // personel-maliyet.js sütun genişliğini bu ölçümle ayarlar
})();
