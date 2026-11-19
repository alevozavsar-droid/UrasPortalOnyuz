// ============================================================
// ORTAK SÜZGEÇ ÇUBUĞU (19 Kas 2026) — wwwroot/css/suzgec-cubuk.css
// Kullanıcı: "Tahsilat Raporunu tüm sekmelerde de bu tarzda [Konsolide Stok Raporu] filtreleme yöntemi getir; 4 – 5 sabit filtre olsun, sonra Tüm Süzgeçler yazsın;
//             filtrelerde birden fazla seçime onay versin (ilişki durumunda hem Grup Dışı hem İlişkili); tüm raporlarda bu şekilde düzenle."
// Tek satır: arama + sabit (hızlı) süzgeç düğmeleri + "Tüm Süzgeçler" paneli (gruplu öbür süzgeçler) + seçili panel süzgeçlerinin etiketleri (✕ ile kalkar) + Temizle.
// Her süzgeç ÇOKLU SEÇİMDİR: düğmeye tıklayınca onay kutulu açılır liste (Tümü / Temizle, 8'den çok seçenekte arama, seçenek yanında kayıt sayısı).
// Değer: F[k] = [] (süzgeç yok) ya da seçili değerlerin dizisi; hepsi seçilirse [] (süzgeç yok).
//
// Kullanım:
//   const sz = SuzgecCubuk.kur({
//     kap: document.getElementById('…'),                     // çubuğun çizileceği öğe
//     alanlar: [{ k: 'sirket', ad: 'Şirket', bos: 'Tümü (Konsolide)' }, …],   // bütün süzgeçler (bos: boşken yazılan; varsayılan "Tümü")
//     hizli: ['sirket', 'bolge', …],                         // çubukta her zaman görünenler (4 – 5)
//     gruplar: [{ ad: 'Cari Kart', alanlar: ['ulke', …] }, …],  // panel grupları (hızlı olmayanlar)
//     secenekler: k => [{ v, ad?, n? }],                     // o süzgecin seçenekleri (kademeli: çağıran, diğer seçimlere uyan kayıtlardan üretir)
//     degisti: () => ciz(),                                  // süzgeç ya da arama değişince
//     araYer: 'Müşteri kodu, adı …', F: {},                  // isteğe bağlı: arama yer tutucusu, durum nesnesi
//     ek: element,                                           // isteğe bağlı: çubuğa (etiketlerle Temizle arasına) taşınacak ek içerik
//     tumIpucu: 'Öbür süzgeçler: …' });
//   sz.F.sirket → ['Alv Kimya', 'Daf Kimya'] · sz.F.ara → 'metin' · sz.yaz() (etiketleri tazele) · sz.temizle() · sz.ayarla('sirket', ['Alv Kimya']) · sz.uyar(k, degerler) (değer dizisi süzgece uyuyor mu)
// ============================================================
(() => {
  'use strict';
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const TR = 'tr-TR', kucuk = s => String(s ?? '').toLocaleLowerCase(TR);
  let pop = null, popKapandi = 0;
  const popKapat = () => { if (pop) { pop.el.remove(); pop.dugme.classList.remove('acik'); pop = null; popKapandi = Date.now(); } };
  /** onay kutulu açılır liste: secenekler [{v, ad, n}], secili [], uygula(dizi) */
  function popAc(dugme, secenekler, secili, uygula, o) {
    o = o || {}; if (pop && pop.dugme === dugme) { popKapat(); return; } popKapat();
    const el = document.createElement('div'); el.className = 'sz-pop'; let sec = new Set(secili);
    const liste = q => secenekler.filter(s => !q || kucuk(s.ad ?? s.v).includes(q)).map(s => `<label><input type="checkbox" value="${esc(s.v)}"${sec.has(s.v) ? ' checked' : ''}><span>${esc(s.ad ?? s.v) || '—'}</span>${s.n !== undefined ? `<small>${s.n}</small>` : ''}</label>`).join('') || '<div class="bos">Kayıt Yok</div>';
    el.innerHTML = `<div class="ust"><a data-i="tumu">Tümü</a><a data-i="temizle">Temizle</a><span class="say"></span></div>${secenekler.length > 8 ? '<div class="ara"><input type="search" placeholder="Ara…"></div>' : ''}<div class="liste">${liste('')}</div>`;
    document.body.appendChild(el);
    const r = dugme.getBoundingClientRect(); el.style.minWidth = Math.max(200, r.width) + 'px'; el.style.top = Math.min(r.bottom + 3, window.innerHeight - el.offsetHeight - 8) + 'px'; el.style.left = Math.max(6, Math.min(r.left, window.innerWidth - el.offsetWidth - 8)) + 'px';
    const sayYaz = () => { el.querySelector('.say').textContent = sec.size ? sec.size + ' Seçili' : (o.bosAd || 'Tümü'); };
    const bildir = () => { sayYaz(); uygula(sec.size === secenekler.length ? [] : [...sec]); };
    el.addEventListener('change', e => { if (e.target.type !== 'checkbox') return; if (e.target.checked) sec.add(e.target.value); else sec.delete(e.target.value); bildir(); });
    el.addEventListener('click', e => { const i = e.target.dataset && e.target.dataset.i; if (!i) return; sec = new Set(i === 'tumu' ? secenekler.map(s => s.v) : []); el.querySelector('.liste').innerHTML = liste(kucuk((el.querySelector('.ara input') || {}).value || '')); bildir(); });
    const ara = el.querySelector('.ara input'); if (ara) { ara.addEventListener('input', () => { el.querySelector('.liste').innerHTML = liste(kucuk(ara.value)); }); ara.focus(); }
    sayYaz(); dugme.classList.add('acik'); pop = { el, dugme };
  }
  document.addEventListener('mousedown', e => { if (pop && !pop.el.contains(e.target) && !pop.dugme.contains(e.target)) popKapat(); });
  document.addEventListener('keydown', e => { if (e.key === 'Escape') popKapat(); });
  window.addEventListener('resize', popKapat);

  function kur(o) {
    const kap = o.kap, F = o.F || {}, AL = o.alanlar, AD = {}, BOS = {}; AL.forEach(a => { AD[a.k] = a.ad; BOS[a.k] = a.bos || 'Tümü'; if (!Array.isArray(F[a.k])) F[a.k] = []; }); if (typeof F.ara !== 'string') F.ara = '';
    const hizli = o.hizli || AL.slice(0, 5).map(a => a.k), gruplar = o.gruplar || [{ ad: 'Süzgeçler', alanlar: AL.map(a => a.k).filter(k => !hizli.includes(k)) }];
    const dugme = k => `<button type="button" class="sz-cs" data-f="${esc(k)}"><span class="e">${esc(AD[k])}</span><span class="d"></span></button>`;
    const panelVar = gruplar.some(g => g.alanlar.length);
    kap.classList.add('sz-cubuk');
    kap.innerHTML = `<div class="sz-ara"><i class="fas fa-magnifying-glass"></i><input type="search" placeholder="${esc(o.araYer || 'Ara…')}" autocomplete="off"></div>${hizli.map(dugme).join('')}`
      + (panelVar ? `<button type="button" class="sz-tum" title="${esc(o.tumIpucu || 'Öbür süzgeçler: ' + gruplar.flatMap(g => g.alanlar).map(k => AD[k]).join(', '))}"><i class="fas fa-sliders"></i>Tüm Süzgeçler<span class="say"></span></button>` : '')
      + `<span class="sz-cipler"></span><span class="sz-ek"></span><span class="sz-sag"><button type="button" class="sz-temizle" title="Bütün süzgeçleri ve aramayı temizle"><i class="fas fa-filter-circle-xmark"></i>Temizle</button><span class="sz-alt"></span></span>`
      + (panelVar ? `<div class="sz-panel" hidden>${gruplar.filter(g => g.alanlar.length).map(g => `<div class="sz-panel-b">${esc(g.ad)}</div><div class="sz-panel-g">${g.alanlar.map(dugme).join('')}</div>`).join('')}</div>` : '');
    if (o.ek) { kap.querySelector('.sz-ek').appendChild(o.ek); o.ek.hidden = false; }
    if (o.alt) { kap.querySelector('.sz-alt').appendChild(o.alt); o.alt.hidden = false; }   // Temizle'nin altındaki küçük yazı (ör. kapsam sayacı "19 / 19 müşteri")
    const araEl = kap.querySelector('.sz-ara input'), tum = kap.querySelector('.sz-tum'), panel = kap.querySelector('.sz-panel'), cipler = kap.querySelector('.sz-cipler');
    const yazi = k => { const l = F[k]; return l.length === 0 ? BOS[k] : l.length === 1 ? ((o.deger ? o.deger(k, l[0]) : null) ?? l[0]) : l.length + ' Seçili'; };
    function yaz() {
      kap.querySelectorAll('.sz-cs').forEach(d => { const k = d.dataset.f; d.querySelector('.d').textContent = yazi(k); d.title = AD[k] + ': ' + (F[k].length ? F[k].join(', ') : BOS[k]); d.classList.toggle('dolu', F[k].length > 0); });
      const l = AL.map(a => a.k).filter(k => !hizli.includes(k) && F[k].length);
      cipler.innerHTML = l.map(k => `<span class="sz-cip" title="${esc(AD[k] + ': ' + F[k].join(', '))}">${esc(AD[k])}: <b>${esc(yazi(k))}</b><i class="fas fa-xmark" data-sil="${esc(k)}" title="Süzgeci kaldır"></i></span>`).join('');
      if (tum) tum.querySelector('.say').textContent = l.length || '';
      if (araEl.value !== F.ara) araEl.value = F.ara;
    }
    const degisti = k => { yaz(); if (o.degisti) o.degisti(k); };   // k: değişen süzgecin anahtarı · 'ara' · 'temizle'
    const panelKapat = () => { if (panel) { panel.hidden = true; tum.classList.remove('acik'); } };
    kap.addEventListener('click', e => {
      const d = e.target.closest('.sz-cs'); if (d) { const k = d.dataset.f; const sec = (o.secenekler ? o.secenekler(k) : []).map(s => typeof s === 'object' ? s : { v: String(s) }); F[k].forEach(v => { if (!sec.some(s => s.v === v)) sec.push({ v, n: 0 }); }); popAc(d, sec, F[k], l => { F[k] = l; degisti(k); }, { bosAd: BOS[k] }); return; }
      const sil = e.target.closest('[data-sil]'); if (sil) { F[sil.dataset.sil] = []; degisti(sil.dataset.sil); return; }
      if (tum && e.target.closest('.sz-tum')) { panel.hidden = !panel.hidden; tum.classList.toggle('acik', !panel.hidden); return; }
      if (e.target.closest('.sz-temizle')) { temizle(); }
    });
    let z; araEl.addEventListener('input', () => { clearTimeout(z); z = setTimeout(() => { F.ara = araEl.value.trim(); if (o.degisti) o.degisti('ara'); }, 180); });
    function temizle() { AL.forEach(a => { F[a.k] = []; }); F.ara = ''; araEl.value = ''; degisti('temizle'); }
    if (panel) {
      document.addEventListener('mousedown', e => { if (panel.hidden) return; if (e.target.closest && e.target.closest('.sz-panel, .sz-tum, .sz-pop')) return; panelKapat(); });
      document.addEventListener('keydown', e => { if (e.key === 'Escape' && !pop && Date.now() - popKapandi > 80) panelKapat(); });   // Esc önce açık listeyi, ikinci Esc paneli kapatır
    }
    const uyar = (k, degerler) => !F[k].length || (Array.isArray(degerler) ? degerler : [degerler]).some(v => F[k].includes(v));
    yaz();
    return { F, yaz, temizle, uyar, ayarla: (k, l) => { F[k] = Array.isArray(l) ? l.slice() : l ? [l] : []; degisti(k); }, aktif: haric => AL.some(a => a.k !== haric && F[a.k].length), kapat: () => { popKapat(); panelKapat(); },
      etiket: (k, ad) => { AD[k] = ad; kap.querySelectorAll(`.sz-cs[data-f="${k}"] .e`).forEach(e => { e.textContent = ad; }); },   // sekmeye göre değişen süzgeç adı (ör. Durum → Belge Türü)
      buda: (k, gecerli) => { const g = gecerli.map(String); F[k] = F[k].filter(v => g.includes(String(v))); } };   // seçenek listesi değişince geçersiz seçimleri at
  }
  window.SuzgecCubuk = { kur, popAc, popKapat };
})();
