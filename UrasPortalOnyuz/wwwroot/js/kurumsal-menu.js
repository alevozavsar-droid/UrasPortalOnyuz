// ============================================================
// KABUK: Grup ERP Portalı'ndan taşınan üst çubuk davranışları
//  - Selam + bugünün tarihi
//  - Menü yapısında arama (rapor / ekran / grup / modül), "/" kısayolu, ok tuşları, Enter
// Veri: window.MENU_YAPISI (Modul/Yapi.js). Sekme kabuğu varsa sonuçlar sekmede açılır.
// ============================================================
(() => {
  const $ = s => document.querySelector(s);
  const esc = s => String(s == null ? '' : s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const TR_LOWER = s => String(s).replace(/İ/g, 'i').replace(/I/g, 'ı').toLocaleLowerCase('tr-TR');

  // ---- Tarih ----
  const tarih = $('#kmTarih');
  if (tarih) tarih.textContent = new Date().toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric', weekday: 'long' });

  // ---- Geniş görünüm: sekme çubuğunun sağındaki düğme kabuğun kenar menüsünü gizler/gösterir; tercih hatırlanır.
  //      Modül sayfaları (çerçeve içinde) kabuğun body sınıfını izleyip kendi sol menülerini de kapatır. ----
  const genisDugme = $('#kmGenisKabuk');
  if (genisDugme) {
    const ANAHTAR = 'kmGenisGorunum';
    const uygula = acik => {
      document.body.classList.toggle('km-genis', acik);
      genisDugme.innerHTML = acik ? '<span aria-hidden="true">⇥</span> Menüleri göster' : '<span aria-hidden="true">⇤</span> Menüleri gizle';
      try { window.dispatchEvent(new Event('resize')); } catch (e) { }
    };
    let acik = false; try { acik = localStorage.getItem(ANAHTAR) === '1'; } catch (e) { }
    uygula(acik);
    genisDugme.addEventListener('click', () => {
      acik = !document.body.classList.contains('km-genis');
      try { localStorage.setItem(ANAHTAR, acik ? '1' : '0'); } catch (e) { }
      uygula(acik);
    });
  }

  // ---- Arama ----
  const form = $('#kmAramaForm'), giris = $('#kmArama'), dd = $('#kmAramaDd');
  if (!form || !giris || !dd || !window.MENU_YAPISI) return;
  const RAPOR_MU = ad => /rapor|liste|tablo|analiz|dashboard|ekstre|mizan|karşılaştırma|denetim|defter/i.test(ad);
  const ARAMA = [];
  window.MENU_YAPISI.forEach(m => {
    if (!m.alt) { if (m.key !== 'ana') ARAMA.push({ ad: m.ad, yol: 'Modül', href: m.href, tip: 'Modül', c: 'g' }); return; }
    const kok = '/Modul/' + m.key;
    ARAMA.push({ ad: m.ad, yol: 'Modül', href: kok, tip: 'Modül', c: 'g' });
    m.alt.forEach(a => {
      ARAMA.push({ ad: a.ad, yol: m.ad, href: `${kok}?yol=${a.key}`, tip: 'Grup', c: 'g' });
      const gruplar = a.alt ? a.alt.map(b => ({ b, yol: `${m.ad} › ${a.ad}`, yolKey: `${a.key}/${b.key}` })) : [{ b: a, yol: m.ad, yolKey: a.key }];
      gruplar.forEach(({ b, yol, yolKey }) => {
        if (b !== a) ARAMA.push({ ad: b.ad, yol, href: `${kok}?yol=${yolKey}`, tip: 'Grup', c: 'g' });
        (b.raporlar || []).forEach((r, i) => {
          const href = r.kisayol ? '/Modul/' + r.kisayol.replace(/^#/, '').replace(/\//, '?yol=') : `${kok}?yol=${yolKey}/r${i}`;
          ARAMA.push({ ad: r.ad, yol: `${yol} › ${b.ad}`, href, tip: RAPOR_MU(r.ad) ? 'Rapor' : 'Ekran', c: RAPOR_MU(r.ad) ? 'r' : 'e', hazir: !!r.url });
        });
      });
    });
  });

  let sonuclar = [], secIdx = -1;
  function ac(x) {
    dd.hidden = true; giris.value = '';
    if (window.portalSekmeAc && x.href.indexOf('/Modul') === 0) { window.portalSekmeAc(x.ad, x.href); return; }
    location.href = x.href;
  }
  function aramaCiz() {
    const q = TR_LOWER(giris.value.trim());
    if (q.length < 2) { dd.hidden = true; sonuclar = []; return; }
    const kelimeler = q.split(/\s+/);
    sonuclar = ARAMA.map(x => { const ad = TR_LOWER(x.ad), yol = TR_LOWER(x.yol); let p = 0; if (ad === q) p = 100; else if (ad.startsWith(q)) p = 80; else if (kelimeler.every(k => ad.includes(k))) p = 60; else if (kelimeler.every(k => (ad + ' ' + yol).includes(k))) p = 30; if (p && x.hazir) p += 5; return { x, p }; })
      .filter(s => s.p > 0).sort((a, b) => b.p - a.p || a.x.ad.localeCompare(b.x.ad, 'tr')).slice(0, 8).map(s => s.x);
    secIdx = sonuclar.length ? 0 : -1;
    const vurgula = ad => { const i = TR_LOWER(ad).indexOf(kelimeler[0]); return i < 0 ? esc(ad) : esc(ad.slice(0, i)) + '<b>' + esc(ad.slice(i, i + kelimeler[0].length)) + '</b>' + esc(ad.slice(i + kelimeler[0].length)); };
    dd.innerHTML = `<div class="hd">${sonuclar.length ? `${sonuclar.length} sonuç · Enter ile aç` : 'Sonuç yok'}</div>` +
      (sonuclar.length ? sonuclar.map((x, i) => `<a href="${esc(x.href)}" class="${i === secIdx ? 'sel' : ''}" data-i="${i}"><span class="tip ${x.c}">${esc(x.tip)}</span><div style="min-width:0"><div class="ad">${vurgula(x.ad)}</div><div class="yol">${esc(x.yol)}${x.tip !== 'Modül' && x.tip !== 'Grup' ? (x.hazir ? ' · hazır' : ' · planlanıyor') : ''}</div></div></a>`).join('')
        : `<div class="bos">Menüde eşleşen rapor veya ekran yok.</div>`);
    dd.hidden = false;
    dd.querySelectorAll('a').forEach(a => a.addEventListener('click', e => { e.preventDefault(); ac(sonuclar[+a.dataset.i]); }));
  }
  giris.addEventListener('input', aramaCiz);
  giris.addEventListener('focus', () => { if (giris.value.trim().length >= 2) aramaCiz(); });
  giris.addEventListener('keydown', e => {
    if (!sonuclar.length) return;
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') { e.preventDefault(); secIdx = (secIdx + (e.key === 'ArrowDown' ? 1 : -1) + sonuclar.length) % sonuclar.length; dd.querySelectorAll('a').forEach((a, i) => a.classList.toggle('sel', i === secIdx)); }
  });
  form.addEventListener('submit', e => { e.preventDefault(); if (sonuclar.length && secIdx >= 0) ac(sonuclar[secIdx]); });
  document.addEventListener('click', e => { if (!e.target.closest('#kmAramaForm')) dd.hidden = true; });
  document.addEventListener('keydown', e => {
    const t = document.activeElement, yaziyor = t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT' || t.isContentEditable);
    if (e.key === '/' && !yaziyor) { e.preventDefault(); giris.focus(); }
    if (e.key === 'Escape') { dd.hidden = true; giris.blur(); }
  });
})();
