// ============================================================
// MODÜL SAYFASI — Grup ERP Portalı › modul.html mantığının uyarlaması.
// Adres: /Modul/{key}#alt1/alt2/rN   (kisayol: "#modul/alt1/alt2" → ilgili modül sayfasına gider)
// Veri: window.MENU_YAPISI (Modul/Yapi.js). Hazır ekranlar çerçeve (iframe) içinde açılır;
// kabuk (_Layout) çerçeve içindeyken kendini gizlediği için yalnızca rapor gövdesi görünür.
// ============================================================
(() => {
  const root = document.getElementById('kmModul');
  if (!root || !window.MENU_YAPISI) return;
  const $ = s => document.querySelector(s);
  const esc = s => String(s == null ? '' : s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const MENU = window.MENU_YAPISI, I = window.MENU_ICONS || {};
  const moduller = MENU.filter(m => m.alt);
  const modul = moduller.find(m => m.key === root.dataset.key) || moduller[0];
  const acik = new Set();
  const chev = I.chev || '';

  const raporSayisi = n => n.raporlar ? n.raporlar.length : (n.alt || []).reduce((t, a) => t + raporSayisi(a), 0);
  const hazirSayisi = n => n.raporlar ? n.raporlar.filter(r => r.url).length : (n.alt || []).reduce((t, a) => t + hazirSayisi(a), 0);
  const tur = ad => /rapor|liste|tablo|analiz|dashboard|ekstre|mizan|karşılaştırma|denetim|defter/i.test(ad) ? 'Rapor' : 'Ekran';
  const durum = r => r.url ? 'hazir' : r.kisayol ? 'kisayol' : 'plan';
  const sekmeli = url => url + (url.indexOf('?') >= 0 ? '&' : '?') + '_sekme=1';
  // Son eklenen raporlar (Yapi.js › yeni: true; kaynak Data/PortalDegisiklikler.cs): şemada, sol menüde ve listede "(yeni)" rozeti
  // "(Yeni)" son 30 günde eklenen, "(Düzenlendi)" son 30 günde güncellenen / taşınan rapor (Yapi.js › yeni / duzenlendi)
  const yeniRozet = r => r.yeni ? '<span class="yeni" title="Son 30 günde eklendi">(Yeni)</span>' : r.duzenlendi ? '<span class="yeni duzenlendi" title="Son 30 günde düzenlendi">(Düzenlendi)</span>' : '';
  const yeniSayisi = n => n.raporlar ? n.raporlar.filter(r => r.yeni).length : (n.alt || []).reduce((t, a) => t + yeniSayisi(a), 0);
  const duzenlenenSayisi = n => n.raporlar ? n.raporlar.filter(r => r.duzenlendi).length : (n.alt || []).reduce((t, a) => t + duzenlenenSayisi(a), 0);

  // ?yol=… ile gelen derin bağlantı (kabuk sekmesi "#" taşıyamadığı için sorgu olarak gelir)
  if (root.dataset.yol && !location.hash.slice(1)) history.replaceState(null, '', '#' + root.dataset.yol);

  // Kabukta <base href="/"> olduğu için href="#..." bağlantıları köke (ana sayfaya) gidiyordu;
  // sayfa içi bağlantılar yalnızca adres çubuğundaki hash'i değiştirir.
  root.addEventListener('click', e => {
    const a = e.target.closest('a[href^="#"]');
    if (!a || e.ctrlKey || e.metaKey || e.shiftKey) return;
    e.preventDefault();
    const h = a.getAttribute('href').slice(1);
    if (decodeURIComponent(location.hash.slice(1)) === h) git(); else location.hash = h;
  });

  // Adres: #alt1/alt2/rN  (alt1 doğrudan rapor taşıyorsa #alt1/rN). Başka modülün yolu gelirse o sayfaya gidilir.
  function coz() {
    let p = decodeURIComponent(location.hash.slice(1)).split('/').filter(Boolean);
    if (p.length && moduller.some(m => m.key === p[0])) {
      if (p[0] !== modul.key) { location.href = '/Modul/' + p[0] + '#' + p.slice(1).join('/'); return null; }
      p = p.slice(1);
    }
    const l1 = modul.alt.find(a => a.key === p[0]) || null;
    let l2 = null, grup = null, rIdx = null;
    if (l1 && l1.alt) { l2 = l1.alt.find(a => a.key === p[1]) || null; grup = l2; rIdx = l2 && p[2] && p[2][0] === 'r' ? parseInt(p[2].slice(1), 10) : null; }
    else if (l1) { grup = l1; rIdx = p[1] && p[1][0] === 'r' ? parseInt(p[1].slice(1), 10) : null; }
    const rapor = grup && rIdx !== null && grup.raporlar[rIdx] ? grup.raporlar[rIdx] : null;
    return { l1, l2, grup, rIdx, rapor };
  }
  const grupYolu = (l1, l2) => [l1 && l1.key, l2 && l2.key].filter(Boolean).join('/');

  function menuCiz({ l1, l2, rIdx }) {
    if (l1) acik.add(l1.key);
    if (l2) acik.add(l1.key + '/' + l2.key);
    const raporListesi = (grup, yol) => grup.raporlar.length
      ? grup.raporlar.map((r, i) => `<a class="rap ${durum(r)} ${grup === (l2 || l1) && i === rIdx ? 'active' : ''}" href="#${yol}/r${i}" title="${esc(r.ad)}"><span class="dot"></span><span class="ad">${esc(r.ad)}</span>${yeniRozet(r)}</a>`).join('')
      : '<div class="rap-bos">Rapor kaydı yok, ekran planlanıyor</div>';
    // Başlık sağındaki sol ok menüyü daraltır (km-dar: 52px dikey şerit, içerik genişler); şeritteki sağ ok geri açar
    $('#kmSide').innerHTML = `<h4><span class="ad">${esc(modul.ad)}</span><button type="button" class="km-daralt" title="Modül menüsünü daralt — içerik genişler"><i class="fas fa-chevron-left"></i></button></h4>` +
      `<div class="km-serit" title="Modül menüsünü genişlet"><i class="fas fa-chevron-right"></i><span class="dik">${esc(modul.ad)}</span></div>` +
      modul.alt.map(a => {
        const yol1 = a.key;
        // Doğrudan açılan grup: kırılım yok, başlık ilk ekrana bağlantı (tür seçimi ekranın içinde)
        if (a.dogrudan && !a.alt) return `<div class="grp l1 tek ${a === l1 ? 'current' : ''}"><a class="grp-h grp-tek" href="#${yol1}/r0" title="${esc(a.ad)}"><span class="ad">${esc(a.ad)}</span><span class="n">${raporSayisi(a) || ''}</span></a></div>`;
        const govde = a.alt
          ? a.alt.map(b => { const yol2 = yol1 + '/' + b.key; return `<div class="grp l2 ${acik.has(yol2) ? 'open' : ''} ${b === l2 ? 'current' : ''}"><button type="button" class="grp-h" data-yol="${yol2}"><span class="ad">${esc(b.ad)}</span><span class="n">${b.raporlar.length || ''}</span>${chev}</button><div class="grp-b">${raporListesi(b, yol2)}</div></div>`; }).join('')
          : raporListesi(a, yol1);
        return `<div class="grp l1 ${acik.has(yol1) ? 'open' : ''} ${a === l1 ? 'current' : ''}"><button type="button" class="grp-h" data-yol="${yol1}"><span class="ad">${esc(a.ad)}</span><span class="n">${raporSayisi(a) || ''}</span>${chev}</button><div class="grp-b">${govde}</div></div>`;
      }).join('') +
      `<div class="legend"><span class="h">Hazır ekran</span><span class="k">Kısayol</span><span>Planlanıyor</span></div>`;
    document.querySelectorAll('#kmSide button.grp-h').forEach(b => b.addEventListener('click', () => {
      const yol = b.dataset.yol, g = b.closest('.grp'), suanki = decodeURIComponent(location.hash.slice(1));
      if (acik.has(yol) && suanki === yol) { acik.delete(yol); g.classList.remove('open'); return; }
      acik.add(yol); g.classList.add('open');
      if (suanki !== yol) location.hash = yol;
    }));
    $('#kmSide .km-daralt').addEventListener('click', () => darUygula(true));
    $('#kmSide .km-serit').addEventListener('click', () => darUygula(false));
    root.classList.toggle('km-dar', darMi());
  }

  // ---- Modül menüsünü daraltma: başlıktaki sol ok menüyü dikey şeride indirir (içerik tam genişlik), şeritteki sağ ok geri açar.
  //      Tercih tüm modüllerde ortaktır (localStorage kmMenuDar); kabuğun "Menüleri gizle" (km-genis) görünümünden bağımsızdır. ----
  const DAR_ANAHTAR = 'kmMenuDar';
  const darMi = () => { try { return localStorage.getItem(DAR_ANAHTAR) === '1'; } catch (e) { return false; } };
  function darUygula(dar) {
    root.classList.toggle('km-dar', dar);
    try { localStorage.setItem(DAR_ANAHTAR, dar ? '1' : '0'); } catch (e) { }
    try { window.dispatchEvent(new Event('resize')); } catch (e) { }
  }

  function sahip(kisayol) {
    const p = kisayol.replace(/^#/, '').split('/');
    const m = moduller.find(x => x.key === p[0]); const a = m && m.alt.find(x => x.key === p[1]); const b = a && a.alt && a.alt.find(x => x.key === p[2]);
    return m ? [m.ad, a && a.ad, b && b.ad].filter(Boolean).join(' › ') : kisayol;
  }

  // ---- Organizasyon şeması: modül → 1. alt menü → 2. alt menü (süreç grubu) → rapor / ekran ----
  // Gösterge paneli (KPI, trend, bekleyen işler) kaldırıldı; modüle tıklayınca kırılım şeması çıkar.
  // Kutulara tıklayınca ilgili bölüm / rapor açılır. Seçili düğüm vurgulanır.
  const ozet = n => `${raporSayisi(n)} rapor / ekran${hazirSayisi(n) ? ` · ${hazirSayisi(n)} hazır` : ''}${yeniSayisi(n) ? ` · ${yeniSayisi(n)} yeni` : ''}${duzenlenenSayisi(n) ? ` · ${duzenlenenSayisi(n)} düzenlendi` : ''}`;
  const dugum = (n, yol, cls, alt) => `<a class="org-node ${cls}" href="#${yol}" title="${esc(n.ad)}"><span class="ad">${esc(n.ad)}</span><span class="s">${esc(alt)}</span></a>`;
  const raporKutu = (grup, yol) => `<div class="org-box">${grup.raporlar.length
    ? grup.raporlar.map((r, i) => `<a class="org-rap ${durum(r)}" href="#${yol}/r${i}" title="${esc(r.ad)}${r.kisayol ? ' · kısayol: ' + esc(sahip(r.kisayol)) : r.url ? ' · hazır ekran' : ' · planlanıyor'}${r.yeni ? ' · yeni eklendi' : r.duzenlendi ? ' · düzenlendi' : ''}"><span class="dot"></span><span class="ad">${esc(r.ad)}</span>${yeniRozet(r)}</a>`).join('')
    : '<div class="org-bos">Rapor kaydı yok, ekran planlanıyor</div>'}</div>`;
  // Kolon: başlık düğümü + altında 2. alt menü kutuları ya da (kırılımsızsa) doğrudan rapor kutusu
  function kolon(n, yol, secili) {
    const govde = n.alt
      ? `<div class="org-grps">${n.alt.map(b => `<div class="org-grp ${b === secili ? 'current' : ''}"><a class="org-grp-h" href="#${yol}/${b.key}" title="${esc(b.ad)}"><span class="ad">${esc(b.ad)}</span><span class="n">${b.raporlar.length || ''}</span></a>${raporKutu(b, yol + '/' + b.key)}</div>`).join('')}</div>`
      : `<div class="org-grps tek">${raporKutu(n, yol)}</div>`;
    return `<div class="org-col">${dugum(n, yol, 'l1' + (n === secili ? ' current' : ''), n.alt ? `${n.alt.length} süreç grubu · ${ozet(n)}` : ozet(n))}${govde}</div>`;
  }
  function semaHtml(kok, kokYol, kokAlt, kolonlar, secili) {
    return `<div class="org-wrap"><div class="org"><div class="org-root">${dugum(kok, kokYol, 'root', kokAlt)}</div><div class="org-l1s">${kolonlar.map(k => kolon(k.n, k.yol, secili)).join('')}</div></div></div>
      <div class="org-legend"><span class="h">Hazır ekran</span><span class="k">Kısayol (ana sahibi başka bölümde)</span><span>Planlanıyor</span><span class="y"><em class="yeni">(Yeni)</em> son 30 günde eklendi</span><span class="y"><em class="yeni duzenlendi">(Düzenlendi)</em> son 30 günde güncellendi</span><span class="i">Kutulara tıklayınca ilgili bölüm ya da rapor açılır.</span></div>`;
  }
  const modulSemasi = secili => semaHtml(modul, '', `${modul.alt.length} alt menü · ${ozet(modul)}`, modul.alt.map(a => ({ n: a, yol: a.key })), secili);
  const l1Semasi = (l1, secili) => l1.alt
    ? semaHtml(l1, l1.key, `${modul.ad} · ${l1.alt.length} süreç grubu · ${ozet(l1)}`, l1.alt.map(b => ({ n: b, yol: l1.key + '/' + b.key })), secili)
    : modulSemasi(l1);

  // ---- Geniş görünüm: kabuktaki (sekme çubuğu sağındaki) "Menüleri gizle" düğmesi kabuğun body sınıfını değiştirir;
  //      burada o sınıf izlenir ve rapor açıkken modülün sol menüsü de gizlenir. Kabuk yoksa (sekmeli görünüm kapalı)
  //      düğme rapor çubuğunda gösterilir. ----
  const GENIS_ANAHTAR = 'kmGenisGorunum';
  const kabukBody = (() => { try { return window.top.document.body; } catch (e) { return document.body; } })();
  const kabukDugmesiVar = (() => { try { return !!window.top.document.getElementById('kmGenisKabuk'); } catch (e) { return false; } })();
  const genisMi = () => kabukDugmesiVar ? kabukBody.classList.contains('km-genis') : (() => { try { return localStorage.getItem(GENIS_ANAHTAR) === '1'; } catch (e) { return false; } })();
  let raporAcik = false;
  function genisUygula() {
    const acik = raporAcik && genisMi();
    root.classList.toggle('km-genis', acik);
    if (!kabukDugmesiVar) {   // kabuk düğmesi yoksa kenar menüyü buradan yönet
      try { kabukBody.classList.toggle('km-genis', acik); } catch (e) { }
      const b = document.getElementById('kmGenisBtn');
      if (b) b.innerHTML = acik ? '<span aria-hidden="true">⇥</span> Menüleri göster' : '<span aria-hidden="true">⇤</span> Menüleri gizle';
    }
    try { window.dispatchEvent(new Event('resize')); } catch (e) { }
  }
  function genisDegistir() {
    const yeni = !genisMi();
    try { localStorage.setItem(GENIS_ANAHTAR, yeni ? '1' : '0'); } catch (e) { }
    genisUygula();
  }
  if (kabukDugmesiVar) { try { new MutationObserver(genisUygula).observe(kabukBody, { attributes: true, attributeFilter: ['class'] }); } catch (e) { } }

  // Hazır ekranı kabuğun sekmesinde aç (kabuk yoksa yeni pencere).
  function yeniSekmedeAc(ad, url) {
    try { if (window.top !== window && window.top.portalSekmeAc) { window.top.portalSekmeAc(ad, url); return; } } catch (e) { }
    if (window.portalSekmeAc) { window.portalSekmeAc(ad, url); return; }
    window.open(url, '_blank');
  }

  function git() {
    const c = coz(); if (!c) return;
    const { l1, l2, grup, rapor } = c;
    document.title = `${(rapor || grup || l1 || modul).ad} · ${modul.ad}`;
    const yol = grupYolu(l1, l2);
    menuCiz(c);
    // Üst çubuk ekmek kırıntısı: modül › 1. alt menü › 2. alt menü › rapor. Sekme çerçevesindeysek kabuğa iletilir,
    // kabuk sayfasının kendisiysek (sekme 0) aynı pencereye olay olarak bildirilir (_Layout › kirintiCiz).
    try {
      const y = [{ ad: modul.ad, url: '/Modul/' + modul.key, hash: '' }];
      if (l1) y.push({ ad: l1.ad, url: '/Modul/' + modul.key + '?yol=' + l1.key, hash: l1.key });
      if (l2) y.push({ ad: l2.ad, url: '/Modul/' + modul.key + '?yol=' + yol, hash: yol });
      if (rapor) y.push({ ad: rapor.ad, url: rapor.url || '', hash: yol + '/r' + c.rIdx });
      if (window.parent && window.parent !== window) window.parent.postMessage({ kaynak: 'portal', tip: 'kirinti', yol: y }, location.origin);
      else window.dispatchEvent(new CustomEvent('portalKirinti', { detail: y }));
    } catch (e) { /* kabuk yoksa yoksay */ }
    const el = $('#kmIcerik');
    const sahipYolu = [modul.ad, l1 && l1.ad, l2 && l2.ad].filter(Boolean).join(' › ');
    const ic = I[modul.ic] || '';

    // Rapor / ekran seçili
    if (rapor) {
      if (rapor.kisayol) { location.hash = rapor.kisayol.replace(/^#/, ''); return; }
      const geri = `<a class="km-btn sm" href="#${yol}">← ${esc(grup.ad)}</a>`;
      if (rapor.url) {
        el.innerHTML = `<div class="ekran-wrap"><div class="ekran-bar">${geri}<span>${esc(rapor.ad)}</span><span class="tag ok">Hazır ekran</span><span class="spacer"></span>${kabukDugmesiVar ? '' : '<button type="button" class="km-btn sm" id="kmGenisBtn" title="Sol menüleri gizleyip raporu tam genişlikte göster"></button>'}<button type="button" class="km-btn sm" id="kmYeniSekme">Sekmede aç ↗</button></div><iframe class="ekran" src="${esc(sekmeli(rapor.url))}" title="${esc(rapor.ad)}"></iframe></div>`;
        $('#kmYeniSekme').addEventListener('click', () => yeniSekmedeAc(rapor.ad, rapor.url));
        const gb = $('#kmGenisBtn'); if (gb) gb.addEventListener('click', genisDegistir);
        raporAcik = true; genisUygula();
        return;
      }
      raporAcik = false; genisUygula();
      el.innerHTML = `<div class="head"><span class="ic">${ic}</span><div><h1>${esc(rapor.ad)}</h1><div class="sub">${esc(sahipYolu)}</div></div><div class="act">${geri}</div></div>
        <div class="pane"><div class="km-placeholder"><div class="big">${ic}</div><span class="tag plan" style="margin-bottom:12px">${esc(tur(rapor.ad))} · planlanıyor</span><h2>${esc(rapor.ad)}</h2><p>Bu ${tur(rapor.ad).toLowerCase()} henüz bu portalda yok. Ana sahibi: ${esc(sahipYolu)}. Ekran geliştirildiğinde burada açılacak.</p><a class="km-btn" href="#${yol}">← Rapor listesine dön</a></div></div>`;
      return;
    }

    raporAcik = false; genisUygula();   // rapor açık değilken modülün sol menüsü her zaman görünür

    // Rapor grubu seçili (2. alt menü veya rapor taşıyan 1. alt menü): üst kırılımın şeması (grup vurgulu) + liste
    if (grup) {
      const liste = grup.raporlar;
      el.innerHTML = `<div class="head"><span class="ic">${ic}</span><div><h1>${esc(grup.ad)}</h1><div class="sub">${esc([modul.ad, l2 && l1.ad].filter(Boolean).join(' › '))} · ${esc(ozet(grup))}${l1.not ? ' · ' + esc(l1.not) : ''}</div></div></div>
        ${l1Semasi(l1, grup)}
        <div class="filters"><input id="kmAra" placeholder="Rapor veya ekran ara…" autocomplete="off"><select id="kmTur"><option value="">Tüm türler</option><option>Rapor</option><option>Ekran</option></select><select id="kmDurum"><option value="">Tüm durumlar</option><option value="hazir">Hazır</option><option value="kisayol">Kısayol</option><option value="plan">Planlanıyor</option></select><span class="cnt" id="kmSayac"></span></div>
        <div class="pane"><h3>${esc(grup.ad)} · rapor / ekran listesi</h3><div id="kmTablo"></div></div>`;
      const ciz = () => {
        const q = ($('#kmAra').value || '').toLocaleLowerCase('tr-TR'), t = $('#kmTur').value, d = $('#kmDurum').value;
        const g = liste.map((r, i) => ({ r, i })).filter(({ r }) => (!q || r.ad.toLocaleLowerCase('tr-TR').includes(q)) && (!t || tur(r.ad) === t) && (!d || durum(r) === d));
        $('#kmSayac').textContent = `${g.length} / ${liste.length} kayıt`;
        $('#kmTablo').innerHTML = g.length ? `<div class="table-wrap"><table class="km-tablo"><thead><tr><th class="num">#</th><th>Rapor / Ekran</th><th>Tür</th><th>Durum</th><th>Ana Sahibi</th><th></th></tr></thead><tbody>${g.map(({ r, i }) => `<tr class="km-row" data-r="${i}"><td class="num">${i + 1}</td><td><b>${esc(r.ad)}</b> ${yeniRozet(r)}</td><td>${esc(tur(r.ad))}</td><td>${r.url ? '<span class="tag ok">Hazır</span>' : r.kisayol ? '<span class="tag ks">Kısayol</span>' : '<span class="tag plan">Planlanıyor</span>'}</td><td class="muted">${r.kisayol ? esc(sahip(r.kisayol)) : esc(sahipYolu)}</td><td style="text-align:right"><a class="km-btn sm" href="#${yol}/r${i}">Aç →</a></td></tr>`).join('')}</tbody></table></div>`
          : `<div class="km-empty">${liste.length ? 'Filtreye uyan kayıt yok.' : 'Bu süreç grubuna henüz rapor / ekran atanmadı. Yeni ekranlar bu grup altında geliştirilecek.'}</div>`;
        document.querySelectorAll('#kmTablo tr.km-row').forEach(tr => tr.addEventListener('click', e => { if (!e.target.closest('a')) location.hash = `${yol}/r${tr.dataset.r}`; }));
      };
      ['#kmAra', '#kmTur', '#kmDurum'].forEach(s => $(s).addEventListener('input', ciz));
      ciz();
      return;
    }

    // 1. alt menü seçili (2. alt menüleri var): o kırılımın şeması
    if (l1) {
      el.innerHTML = `<div class="head"><span class="ic">${ic}</span><div><h1>${esc(l1.ad)}</h1><div class="sub">${esc(modul.ad)} · ${l1.alt.length} süreç grubu · ${esc(ozet(l1))}${l1.not ? ' · ' + esc(l1.not) : ''}</div></div><div class="act"><a class="km-btn sm" href="#">← ${esc(modul.ad)} şeması</a></div></div>
        ${l1Semasi(l1, l1)}`;
      return;
    }

    // Modül kökü: modülün tam kırılım şeması (kaça ayrılıyor, hangi rapor nerede)
    el.innerHTML = `<div class="head"><span class="ic">${ic}</span><div><h1>${esc(modul.ad)}</h1><div class="sub">${esc(modul.amac || '')}</div></div></div>
      ${modulSemasi(null)}
      <div class="muted">${modul.alt.length} alt menü · ${raporSayisi(modul)} rapor / ekran · ${hazirSayisi(modul)} hazır ekran. Aynı rapor birden fazla süreçte gerekliyse kısayol olarak gösterilir, veri kaynağı tek kalır.</div>`;
  }

  window.addEventListener('hashchange', git);
  git();
})();
