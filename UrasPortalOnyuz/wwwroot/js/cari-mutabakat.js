// ============================================================
// CARİ MUTABAKAT YÖNETİMİ — ekran betiği (Views/CariMutabakat/Index.cshtml; veri /CariMutabakat/Veri; kaynak Data/MutabakatOrnek.cs + Data/CariKartOrnek.cs)
// Sekmeler: Aktif Cariler | Pasif Cariler (Kontrol Merkezi §3) → Kontrol Bekleyen Carilerim (temsilci §5–6) → Kapanış Kontrolü (§8) → Gönderimler ve Yanıtlar (§10) → Günlük ve Parametreler (§11)
// Kural: Mutabakat Durumu elle seçilmez, sunucu hesaplar (DurumHesapla). Her işlem sonrası veri yeniden çekilir (yenile), açık drawer / kontrol paneli korunur.
// Rol ve temsilci demo amaçlı ekrandan seçilir; her istek rol + temsilci ile gider, yetki matrisi (V.yetkiler) hem ekranda hem sunucuda uygulanır.
// Cari kart bloğu (CARİ_MASTER) Müşteri Cari Kartları ekranıyla (wwwroot/js/cari-kart.js) aynı alan adları / sırası.
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Kontrol Bekliyor", "Gönderime Hazır").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('cm'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const tl = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const iso = d => d.toISOString().slice(0, 10);
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const trh = s => { if (!s) return null; const p = String(s).split(' ')[0].split('.'); return p.length === 3 ? new Date(+p[2], +p[1] - 1, +p[0]) : null; };
  const aySayisi = s => { const d = trh(s); return d ? (Date.now() - d.getTime()) / (1000 * 3600 * 24 * 30.4375) : null; };
  const DURUM_SINIF = { 'Henüz Başlanmadı': 'baslanmadi', 'İç Kontrolde': 'kontrolde', 'Kapanış Kontrolü Gerekli': 'kapanis', 'Gönderime Hazır': 'hazir', 'Gönderildi': 'gonderildi', 'Mutabık': 'mutabik', 'İtirazlı': 'itirazli', 'Yeniden Mutabakat Gerekli': 'yeniden' };
  const durumRozet = d => `<span class="cm-durum ${DURUM_SINIF[d] || 'baslanmadi'}">${esc(d)}</span>`;
  const iletisimRozet = d => rozet(d, d === 'Tamamlandı' ? 'ok' : d === 'Eksik' ? 'hata' : 'bekle');
  const sonucRozet = s => rozet(s, s === 'Mutabık' ? 'ok' : s === 'İtirazlı' ? 'hata' : 'bekle');
  const kartRozet = d => rozet(d, d === 'Aktif' ? 'ok' : 'notr');
  const tipRozet = t => `<span class="ck-tip ${t === 'Tedarikçi' ? 'tedarikci' : t === 'Müşteri' ? 'musteri' : ''}">${esc(t || '—')}</span>`;
  const iliskiRozet = i => `<span class="ck-tip ${i === 'Grup İçi' ? 'grup' : i === 'İlişkili Cari' ? 'iliskili' : ''}">${esc(i || '—')}</span>`;
  const rolRozet = r => `<span class="ck-tip ${r === 'Çift Rol' ? 'cift' : ''}">${esc(r || '—')}</span>`;
  const sinifRozet = s => `<span class="ck-tip ${s === 'Özel Takip' ? 'ozel' : ''}">${esc(s || '—')}</span>`;
  const riskHtml = c => (c.eskiBakiyeUyari ? '<span class="cm-risk eski" title="Eşikten eski açık bakiye">Eski Bakiye</span>' : '') + (c.degerlemeRiski ? '<span class="cm-risk deger" title="Yabancı para bakiyesinde değerleme riski">Değerleme Riski</span>' : '');
  const sekmeli = kok.dataset.sekme === '1';
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + iso(new Date()) + '.csv'; a.click(); };
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 9000); window.scrollTo({ top: 0, behavior: 'smooth' }); }

  let V = null, C = [], ROL = kok.dataset.rol || 'Muhasebe', TEMSILCI = '', mod = 'aktif', secili = new Set();
  let acikDrawer = null, acikKontrol = null, acikKapanis = null, acikYanit = null, acikItiraz = null, acikFark = null, farkFormAcik = false;
  const cari = kod => C.find(c => c.kod === kod);
  const yetki = e => ((V && V.yetkiler && V.yetkiler[ROL]) || []).includes(e);
  const temsilciRolu = () => ROL === 'Satış Temsilcisi' || ROL === 'Satın Alma Sorumlusu';
  const kontrolde = c => c.surecAcik && (c.durum === 'İç Kontrolde' || c.durum === 'Kapanış Kontrolü Gerekli');
  const benimCarim = c => !temsilciRolu() || c.temsilciler.includes(TEMSILCI);

  // ---- sunucu
  async function post(url, body) {
    try {
      const r = await fetch('/CariMutabakat/' + url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(Object.assign({ rol: ROL, temsilci: TEMSILCI }, body || {})) });
      return await r.json();
    } catch (e) { return { success: false, message: 'Sunucuya ulaşılamadı.' }; }
  }
  async function islem(url, body, sonra) {
    const r = await post(url, body);
    mesaj(r.success ? 'success' : 'warning', esc(r.message || (r.success ? 'İşlem tamamlandı.' : 'İşlem yapılamadı.')));
    if (r.success || r.cariler) { await yenile(); if (sonra) sonra(r); }
    return r;
  }

  // ---- sekmeler
  function sekme(p) {
    if (p === 'aktif' || p === 'pasif') { if (mod !== p) secili.clear(); mod = p; }
    $$('#cmSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    const panel = p === 'aktif' || p === 'pasif' ? 'merkez' : p;
    $$('#cm > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === panel));
    if (V) { if (panel === 'merkez') cizMerkez(); if (panel === 'temsilci') cizTemsilci(); if (panel === 'kapanis') cizKapanis(); if (panel === 'gonderim') cizGonderim(); if (panel === 'gunluk') cizGunluk(); }
    try { const u = new URL(location.href); if (p === 'aktif') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }
  $$('#cmSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  const aktifSekme = () => ($('#cmSekmeler button.aktif') || {}).dataset?.p || 'aktif';

  // ---- veri
  async function yukle(ilk) {
    let r; try { r = await (await fetch('/CariMutabakat/Veri')).json(); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r; C = r.cariler;
    if (ilk) { rolleriKur(); filtreleriKur(); }
    $('#cmAyRozet').textContent = V.parametreler.aktifPasifAy; $('#cmEskiEsik').textContent = V.parametreler.eskiBakiyeGun; $('#cmDegerEsik').textContent = V.parametreler.degerlemeRiskiGun;
    $('#cmAnlik').innerHTML = `<i class="fas fa-clock me-1"></i>${esc(V.simdi)} · kur ${esc(V.kurTarihi)}: USD ${tl(V.kurlar.USD)} · EUR ${tl(V.kurlar.EUR)}`;
    sayaclar();
  }
  async function yenile() {
    await yukle(false);
    const p = aktifSekme(); const panel = p === 'aktif' || p === 'pasif' ? 'merkez' : p;
    if (panel === 'merkez') cizMerkez(); if (panel === 'temsilci') cizTemsilci(); if (panel === 'kapanis') cizKapanis(); if (panel === 'gonderim') cizGonderim(); if (panel === 'gunluk') cizGunluk();
    if (acikDrawer && $('#cmDrawer').classList.contains('acik')) drawerAc(acikDrawer, true);
  }
  function sayaclar() {
    $('#cmNAktif').textContent = C.filter(c => c.aktif).length; $('#cmNPasif').textContent = C.filter(c => !c.aktif).length;
    $('#cmNTemsilci').textContent = C.filter(c => kontrolde(c) && benimCarim(c)).length;
    $('#cmNKapanis').textContent = C.filter(c => (c.kapanisGerekli && !c.kapanis.cozuldu) || (c.kontrol.farkVar && !c.kontrol.farkCozuldu)).length;
    $('#cmNGonderim').textContent = C.filter(c => c.durum === 'Gönderildi').length;
  }
  function rolleriKur() {
    const rs = $('#cmRol'); rs.innerHTML = V.roller.map(r => `<option>${esc(r)}</option>`).join(''); if (!V.roller.includes(ROL)) ROL = 'Muhasebe'; rs.value = ROL;
    temsilciSecenekleri(); rs.addEventListener('change', () => { ROL = rs.value; temsilciSecenekleri(); secili.clear(); sayaclar(); sekme(aktifSekme()); if (acikDrawer && $('#cmDrawer').classList.contains('acik')) drawerAc(acikDrawer, true); });
    $('#cmTemsilciSec').addEventListener('change', () => { TEMSILCI = $('#cmTemsilciSec').value; sayaclar(); sekme(aktifSekme()); });
  }
  function temsilciSecenekleri() {
    const ts = $('#cmTemsilciSec'); const liste = V.temsilciler.filter(t => !temsilciRolu() || t.rol === ROL);
    ts.innerHTML = liste.map(t => `<option value="${esc(t.ad)}">${esc(t.ad)} · ${esc(t.rol)}</option>`).join('');
    TEMSILCI = liste.some(t => t.ad === TEMSILCI) ? TEMSILCI : (liste.some(t => t.ad === V.ben.temsilci) ? V.ben.temsilci : (liste[0] || {}).ad || ''); ts.value = TEMSILCI;
  }
  function filtreleriKur() {
    const doldur = (id, l) => { const s = $(id); s.innerHTML = '<option value="">Tümü</option>' + l.map(x => `<option>${esc(x)}</option>`).join(''); };
    doldur('#cmSirket', V.sirketler); doldur('#cmTip', V.kartTipleri); doldur('#cmSinif', V.siniflar); doldur('#cmRolF', V.roller2); doldur('#cmIliski', V.iliskiDurumlari);
    doldur('#cmTemsilciF', V.temsilciler.map(t => t.ad)); doldur('#cmIletisimF', V.iletisimDurumlari); doldur('#cmDurumF', V.durumlar);
    $$('.cm-filtre select, .cm-filtre input').forEach(el => el.addEventListener(el.tagName === 'INPUT' ? 'input' : 'change', cizMerkez));
    $('#cmTemizle').addEventListener('click', () => { $$('.cm-filtre select, .cm-filtre input').forEach(el => el.value = ''); cizMerkez(); });
    $('#cmHepsi').addEventListener('change', () => { const l = filtreli(); if ($('#cmHepsi').checked) l.forEach(c => secili.add(c.kod)); else l.forEach(c => secili.delete(c.kod)); cizMerkez(); });
    $('#cmTopluTemsilci').addEventListener('click', () => toplu('TemsilciyeGonder'));
    $('#cmTopluMutabakat').addEventListener('click', () => toplu('MutabakatGonder'));
    $('#cmExcel').addEventListener('click', merkezCsv);
    $('#cmKapanisExcel').addEventListener('click', kapanisCsv);
    $('#cmGonderimExcel').addEventListener('click', gonderimCsv);
    $('#cmGonderimF').addEventListener('change', cizGonderim);
    $('#cmKpi').addEventListener('click', e => { const k = e.target.closest('[data-durum]'); if (!k) return; $('#cmDurumF').value = $('#cmDurumF').value === k.dataset.durum ? '' : k.dataset.durum; cizMerkez(); });
    $('#cmPKaydet').addEventListener('click', parametreKaydet);
    $('#cmKapanisKapat').addEventListener('click', () => { acikKapanis = null; $('#cmKapanisFormKart').style.display = 'none'; });
    $('#cmKapanisSonuclandir').addEventListener('click', kapanisSonuclandir);
    $('#cmYanitKapat').addEventListener('click', () => { acikYanit = null; $('#cmYanitKart').style.display = 'none'; });
    $('#cmYanitKaydet').addEventListener('click', yanitKaydet);
    $('#cmDrawerKapat').addEventListener('click', drawerKapat); $('#cmPerde').addEventListener('click', drawerKapat);
  }

  // ---- kontrol merkezi
  function filtreli() {
    const v = id => $(id).value, ara = v('#cmAra').trim().toLocaleLowerCase('tr-TR');
    const sirket = v('#cmSirket'), tip = v('#cmTip'), sinif = v('#cmSinif'), rol = v('#cmRolF'), iliski = v('#cmIliski'), tem = v('#cmTemsilciF'), ilt = v('#cmIletisimF'), dov = v('#cmDovizF'), dur = v('#cmDurumF'), sm = v('#cmSonMutF'), son = v('#cmSonrasiF'), yas = v('#cmYasF');
    return C.filter(c => c.aktif === (mod === 'aktif')).filter(c => {
      if (sirket && c.sirket !== sirket) return false; if (tip && c.kartTipi !== tip) return false; if (sinif && c.sinif !== sinif) return false; if (rol && c.rol !== rol) return false; if (iliski && c.iliski !== iliski) return false;
      if (tem && !c.temsilciler.includes(tem)) return false; if (ilt && c.iletisimDurumu !== ilt) return false; if (dov && c.cokDoviz !== (dov === 'var')) return false; if (dur && c.durum !== dur) return false;
      if (son && c.sonMutabakatSonrasiIslem !== (son === 'var')) return false;
      if (sm) { const a = aySayisi(c.sonMutabakatTr); if (sm === 'yok' ? a !== null : a === null || (sm === '3' ? a > 3 : sm === '6' ? a <= 3 || a > 6 : sm === '12' ? a <= 6 || a > 12 : a <= 12)) return false; }
      if (yas === 'eski' && !c.eskiBakiyeUyari) return false; if (yas === 'deger' && !c.degerlemeRiski) return false; if (yas === 'acik' && !c.acikFaturaAdet) return false; if (yas === 'yok' && c.acikFaturaAdet) return false;
      if (ara) { const m = [c.kod, c.kisaAd, c.resmiUnvan, c.sirket, c.temsilciler.join(' '), c.kisiler.map(k => k.adSoyad + ' ' + k.eposta).join(' ')].join(' ').toLocaleLowerCase('tr-TR'); if (!m.includes(ara)) return false; }
      return true;
    });
  }
  function islemHtml(c, yer) {
    const b = [];
    const d = (act, ad, cls, ikon, ipucu) => b.push(`<button type="button" class="btn ${cls} btn-sm" data-act="${act}" data-kod="${esc(c.kod)}" title="${esc(ipucu || ad)}"><i class="fas ${ikon} me-1"></i>${ad}</button>`);
    if (!c.surecAcik && yetki('temsilciyeGonder')) d('temsilci', 'Temsilciye Gönder', 'btn-primary', 'fa-user-check', 'Yeni mutabakat süreci açar; iç yetkili otomatik atanır: ' + c.temsilciler.join(' + '));
    if (kontrolde(c) && yetki('iletisim') && benimCarim(c)) d('kontrol', 'Kontrol Et', 'btn-warning', 'fa-list-check', 'İletişim ve bakiye kontrolü (temsilci ekranı)');
    if (c.kapanisGerekli && !c.kapanis.cozuldu && c.surecAcik && yetki('kapanisCoz')) d('kapanis', 'Kapanışı Çöz', 'btn-danger', 'fa-triangle-exclamation', 'Eski bakiye / değerleme riski kapanış kontrolü');
    if (c.kontrol.farkVar && !c.kontrol.farkCozuldu && yetki('farkCoz')) d('fark', 'Farkı Çöz', 'btn-danger', 'fa-flag', 'Temsilcinin bildirdiği fark / düzeltme');
    if (c.durum === 'Gönderime Hazır' && yetki('mutabakatGonder')) d('gonder', 'Mutabakat Gönder', 'btn-success', 'fa-paper-plane', 'Ana yetkilinin e-postasına PDF mutabakat gönderir');
    if (c.durum === 'Gönderildi' && yetki('yanitIsle')) d('yanit', 'Yanıt İşle', 'btn-info', 'fa-reply', 'Cari yanıtı: Mutabık / İtirazlı');
    if (c.durum === 'Gönderildi' && yetki('mutabakatGonder')) d('tekrar', 'Tekrar Gönder', 'btn-outline-secondary', 'fa-rotate-right', 'Bekleyen mutabakatı yeniden gönder');
    if (c.durum === 'İtirazlı' && yetki('itirazCoz')) d('itiraz', 'İtirazı Çöz', 'btn-danger', 'fa-gavel', 'İtiraz çözümü; cari yeniden iç kontrole gider');
    if (['Gönderime Hazır', 'Gönderildi', 'Mutabık', 'İtirazlı', 'Yeniden Mutabakat Gerekli'].includes(c.durum)) d('pdf', 'PDF', 'btn-outline-secondary', 'fa-file-pdf', 'Mutabakat mektubu önizleme');
    if (yer !== 'drawer') d('detay', 'Detay', 'btn-light', 'fa-circle-info', 'Mutabakat dosyası ve cari kart');
    return b.join('');
  }
  const dovizOzet = c => c.dovizler.filter(d => d.net !== 0).map(d => `${d.paraBirimi} ${tl(Math.abs(d.net))}`).join(' · ') || 'Sıfır';
  function cizMerkez() {
    $('#cmModBaslik').textContent = mod === 'aktif' ? 'Aktif Cariler' : 'Pasif Cariler';
    const tum = C.filter(c => c.aktif === (mod === 'aktif')), l = filtreli(), dur = $('#cmDurumF').value;
    // KPI: cari sayısı, bakiye ve 8 durum (tıklayınca Mutabakat Durumu filtresi)
    const kpi = [{ l: mod === 'aktif' ? 'Aktif Cari' : 'Pasif Cari', v: tum.length, s: `${tum.filter(c => c.kartTipi === 'Müşteri').length} müşteri · ${tum.filter(c => c.kartTipi === 'Tedarikçi').length} tedarikçi`, k: '#0e7490' }, { l: 'Bakiye (TL)', v: kisa(tum.reduce((s, c) => s + c.bakiyeTl, 0)), s: `${tum.filter(c => c.cokDoviz).length} cari birden fazla döviz`, k: '#0f172a' }];
    const renk = { 'Henüz Başlanmadı': '#94a3b8', 'İç Kontrolde': '#f59e0b', 'Kapanış Kontrolü Gerekli': '#dc2626', 'Gönderime Hazır': '#16a34a', 'Gönderildi': '#2563eb', 'Mutabık': '#059669', 'İtirazlı': '#db2777', 'Yeniden Mutabakat Gerekli': '#ea580c' };
    V.durumlar.forEach(d => { const n = tum.filter(c => c.durum === d); kpi.push({ l: d, v: n.length, s: n.length ? kisa(n.reduce((s, c) => s + c.bakiyeTl, 0)) + ' TL' : '—', k: renk[d], d }); });
    $('#cmKpi').innerHTML = kpi.map(k => `<div style="--k:${k.k};${k.d && dur === k.d ? 'background:#ecfeff' : ''}" ${k.d ? `data-durum="${esc(k.d)}" title="Mutabakat Durumu filtresi"` : ''}><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${k.s}</div></div>`).join('');
    const tb = $('#cmTablo tbody');
    if (!l.length) { tb.innerHTML = '<tr><td colspan="17" class="na-bos">Filtreye uyan cari yok.</td></tr>'; }
    else tb.innerHTML = l.map(c => `<tr class="tik ${secili.has(c.kod) ? 'secili' : ''}" data-kod="${esc(c.kod)}">
      <td><input type="checkbox" class="sec" data-kod="${esc(c.kod)}" ${secili.has(c.kod) ? 'checked' : ''}></td>
      <td>${esc(c.sirket)}</td><td><b>${esc(c.kod)}</b></td><td class="ad"><b>${esc(c.kisaAd)}</b><small>${esc(c.resmiUnvan)}</small></td>
      <td>${tipRozet(c.kartTipi)}</td><td>${sinifRozet(c.sinif)}</td><td>${rolRozet(c.rol)}</td><td>${iliskiRozet(c.iliski)}</td>
      <td>${c.temsilciRolleri.map(t => `<span class="cm-pill" title="${esc(t.rol)}">${esc(t.ad)}</span>`).join(' ')}</td>
      <td>${iletisimRozet(c.iletisimDurumu)}</td>
      <td class="s"><b>${tl(c.bakiyeTl)}</b><small>${esc(c.bakiyeYonu)} · ${esc(dovizOzet(c))}</small></td>
      <td>${c.cokDoviz ? rozet('Var', 'bilgi') : rozet('Yok', 'notr')}<small>${c.dovizler.map(d => d.paraBirimi).join(' / ')}</small></td>
      <td>${c.enEskiAcikFaturaTr ? `${esc(c.enEskiAcikFaturaTr)}${riskHtml(c)}<small>${c.bakiyeYasi} gün · ${c.acikFaturaAdet} açık fatura</small>` : '<span class="text-muted">Açık kalem yok</span>'}</td>
      <td>${bos(c.sonMutabakatTr)}</td><td>${c.sonMutabakatTr ? (c.sonMutabakatSonrasiIslem ? rozet('Var', 'hata') : rozet('Yok', 'ok')) : '<span class="text-muted">—</span>'}</td>
      <td>${durumRozet(c.durum)}${c.surecAcik ? `<small>süreç #${c.surecNo}${c.kontrol.onaylar.length ? ' · ' + c.kontrol.onaylar.length + '/' + c.temsilciler.length + ' onay' : ''}</small>` : ''}</td>
      <td class="cm-islem">${islemHtml(c, 'liste')}</td></tr>`).join('');
    $('#cmTopBakiye').textContent = tl(l.reduce((s, c) => s + c.bakiyeTl, 0));
    $('#cmTopOzet').textContent = `${l.length} cari · ${l.filter(c => c.durum === 'Gönderime Hazır').length} gönderime hazır · ${l.filter(c => c.kapanisGerekli && !c.kapanis.cozuldu).length} kapanış kontrolü · ${l.filter(c => c.iletisimDurumu === 'Eksik').length} iletişim eksik`;
    $('#cmSayi').textContent = `${l.length} cari listelendi · ${secili.size} seçili`;
    $('#cmHepsi').checked = l.length > 0 && l.every(c => secili.has(c.kod));
    $('#cmTopluTemsilci').style.display = yetki('temsilciyeGonder') ? '' : 'none'; $('#cmTopluMutabakat').style.display = yetki('mutabakatGonder') ? '' : 'none';
    $$('#cmTablo tbody tr.tik').forEach(r => r.addEventListener('click', e => { if (e.target.closest('button, input, a, [data-url]')) return; drawerAc(r.dataset.kod); }));
    $$('#cmTablo input.sec').forEach(i => i.addEventListener('change', () => { if (i.checked) secili.add(i.dataset.kod); else secili.delete(i.dataset.kod); i.closest('tr').classList.toggle('secili', i.checked); $('#cmSayi').textContent = `${l.length} cari listelendi · ${secili.size} seçili`; $('#cmHepsi').checked = l.length > 0 && l.every(c => secili.has(c.kod)); }));
  }
  async function toplu(url) {
    if (!secili.size) { mesaj('warning', 'Önce listeden cari seçin.'); return; }
    const r = await post(url, { kodlar: [...secili] });
    mesaj(r.success ? (r.hatalar && r.hatalar.length ? 'warning' : 'success') : 'warning', esc(r.message));
    if (r.success) secili.clear(); await yenile();
  }
  function merkezCsv() {
    csvIndir('cari_mutabakat_' + mod, ['Şirket', 'Cari Kodu', 'Cari Kısa Adı', 'Resmi Ünvan', 'Cari Kart Tipi', 'Cari Sınıfı', 'Cari Rolü', 'İlişki Durumu', 'Temsilcisi', 'İletişim Bilgileri Durumu', 'Güncel Bakiye (TL)', 'Bakiye Yönü', 'Döviz Bakiyeleri', 'Birden Fazla Döviz', 'En Eski Açık Fatura', 'Bakiye Yaşı (Gün)', 'Eski Bakiye Uyarısı', 'Değerleme Riski', 'Son Mutabakat Tarihi', 'Son Mutabakat Sonrası İşlem', 'Mutabakat Durumu'],
      filtreli().map(c => [c.sirket, c.kod, c.kisaAd, c.resmiUnvan, c.kartTipi, c.sinif, c.rol, c.iliski, c.temsilciler.join(' + '), c.iletisimDurumu, sayi(c.bakiyeTl), c.bakiyeYonu, dovizOzet(c), c.cokDoviz ? 'Var' : 'Yok', c.enEskiAcikFaturaTr, c.bakiyeYasi ?? '', c.eskiBakiyeUyari ? 'Evet' : 'Hayır', c.degerlemeRiski ? 'Evet' : 'Hayır', c.sonMutabakatTr, c.sonMutabakatTr ? (c.sonMutabakatSonrasiIslem ? 'Var' : 'Yok') : '', c.durum]));
  }

  // ---- ortak bloklar (döviz tablosu, kişiler, blokajlar, cari kart)
  function dovizTablo(c) {
    return `<div class="table-responsive"><table class="na-tablo cm-doviz" data-dinamik="kapali"><thead><tr><th>Para Birimi</th><th class="s">Borç Toplamı</th><th class="s">Alacak Toplamı</th><th class="s">Net Bakiye</th><th>Bakiye Yönü</th><th class="s">Kur</th><th class="s">TL Karşılığı</th><th class="s">Açık Fatura</th><th>En Eski Açık Fatura</th><th>Risk</th><th>Ekstre</th></tr></thead><tbody>
      ${c.dovizler.map(d => `<tr><td><b>${esc(d.paraBirimi)}</b></td><td class="s">${tl(d.borc)}</td><td class="s">${tl(d.alacak)}</td><td class="s"><b>${tl(Math.abs(d.net))}</b></td><td>${d.net > 0 ? rozet('Borç', 'bilgi') : d.net < 0 ? rozet('Alacak', 'bekle') : rozet('Sıfır', 'notr')}</td><td class="s">${d.paraBirimi === 'TL' ? '1,00' : tl(d.kur)}</td><td class="s">${tl(d.tlKarsiligi)}</td><td class="s">${d.acikFaturaAdet} adet<small>${tl(d.acikFaturaTutar)}</small></td><td>${d.enEskiAcikFaturaTr ? `${esc(d.enEskiAcikFaturaTr)}<small>${d.bakiyeYasi} gün</small>` : '<span class="text-muted">—</span>'}</td><td>${riskHtml(d) || '<span class="text-muted">—</span>'}</td>
        <td class="cm-islem"><button type="button" class="btn btn-light btn-sm" data-eks="${esc(c.kod)}|${esc(d.paraBirimi)}" title="Hareketleri bu ekranda göster"><i class="fas fa-list me-1"></i>Ekstreyi Gör</button><button type="button" class="btn btn-light btn-sm" data-pdf="${esc(c.kod)}|${esc(d.paraBirimi)}" title="Yazdırılabilir ekstre (PDF)"><i class="fas fa-file-pdf me-1"></i>PDF</button><button type="button" class="btn btn-light btn-sm" data-xls="${esc(c.kod)}|${esc(d.paraBirimi)}" title="Excel"><i class="fas fa-file-excel me-1"></i>Excel</button></td></tr>`).join('')}
      </tbody><tfoot><tr><td colspan="6" style="text-align:left;font-weight:800">Toplam TL Karşılığı · kur tarihi ${esc(V.kurTarihi)} · bakiye tarihi ${esc(c.bakiyeZamani)}</td><td class="s" style="font-weight:800">${tl(c.bakiyeTl)}</td><td class="s">${c.acikFaturaAdet} adet<small>${tl(c.acikFaturaTl)} TL</small></td><td colspan="3"></td></tr></tfoot></table></div><div class="cm-ekstre-ic" data-kod="${esc(c.kod)}"></div>`;
  }
  async function ekstreGoster(kod, pb, kap) {
    const ic = kap.querySelector('.cm-ekstre-ic'); if (!ic) return;
    if (ic.dataset.pb === pb && ic.innerHTML) { ic.innerHTML = ''; ic.dataset.pb = ''; return; }
    ic.innerHTML = '<div class="na-bos">Ekstre yükleniyor…</div>';
    let r; try { r = await (await fetch(`/CariMutabakat/Hareketler?kod=${encodeURIComponent(kod)}&pb=${encodeURIComponent(pb)}`)).json(); } catch (e) { r = null; }
    if (!r || !r.success) { ic.innerHTML = '<div class="na-bos">Ekstre alınamadı.</div>'; return; }
    ic.dataset.pb = pb;
    ic.innerHTML = `<div class="na-kart mt-2"><div class="na-kart-b"><span><i class="fas fa-list me-1"></i>${esc(pb)} Ekstresi · ${r.hareketler.length} hareket</span><span class="not">bakiye tarihi ${esc(r.bakiyeZamani)} · pozitif = cari borçlu</span></div><div class="na-kart-g p-0"><div class="table-responsive" style="max-height:40vh"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Belge Tipi</th><th>Belge No</th><th>Açıklama</th><th class="s">Borç</th><th class="s">Alacak</th><th class="s">Bakiye</th><th>Vade</th><th>Kapama Durumu</th></tr></thead><tbody>
      ${r.hareketler.map(h => `<tr><td>${esc(h.tarih)}</td><td>${esc(h.belgeTipi)}</td><td>${esc(h.belgeNo)}</td><td class="ad">${esc(h.aciklama)}</td><td class="s">${h.borc ? tl(h.borc) : ''}</td><td class="s">${h.alacak ? tl(h.alacak) : ''}</td><td class="s">${tl(h.bakiye)}</td><td>${bos(h.vade)}</td><td>${h.kapamaDurumu === 'Açık' || h.kapamaDurumu === 'Kısmi' ? rozet(h.kapamaDurumu + (h.kalan > 0.005 && h.belgeTipi === 'Fatura' ? ' · kalan ' + tl(h.kalan) : ''), 'hata') : h.kapamaDurumu === 'Vade Bekliyor' ? rozet(h.kapamaDurumu, 'bekle') : rozet(h.kapamaDurumu, 'ok')}</td></tr>`).join('')}
      </tbody></table></div></div></div>`;
  }
  async function ekstreExcel(kod, pb) {
    let r; try { r = await (await fetch(`/CariMutabakat/Hareketler?kod=${encodeURIComponent(kod)}&pb=${encodeURIComponent(pb)}`)).json(); } catch (e) { r = null; }
    if (!r || !r.success) { mesaj('warning', 'Ekstre alınamadı.'); return; }
    csvIndir(`ekstre_${kod}_${pb}`, ['Tarih', 'Para Birimi', 'Belge Tipi', 'Belge No', 'Açıklama', 'Borç', 'Alacak', 'Bakiye', 'Vade', 'Kapama Durumu', 'Kalan'], r.hareketler.map(h => [h.tarih, h.paraBirimi, h.belgeTipi, h.belgeNo, h.aciklama, sayi(h.borc), sayi(h.alacak), sayi(h.bakiye), h.vade, h.kapamaDurumu, sayi(h.kalan)]));
  }
  const blokajHtml = c => `<ul class="cm-blokaj">${c.blokajlar.map(b => `<li><i class="fas ${b.izin ? 'fa-circle-check text-success' : 'fa-circle-xmark text-danger'}"></i>${esc(b.kontrol)}</li>`).join('')}</ul>`;
  const kisiTablo = c => c.kisiler.length ? `<div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Ad Soyad</th><th>Ünvan</th><th>Departman</th><th>E-posta</th><th>Telefon</th><th>Cep</th><th>Mutabakat Yetkilisi</th><th>Ana Yetkili</th><th>E-posta Gönder</th><th>Bilgiler Doğru mu?</th><th>Not</th></tr></thead><tbody>${c.kisiler.map(k => `<tr class="${k.anaYetkili ? 'table-active' : ''}"><td class="ad"><b>${esc(k.adSoyad)}</b></td><td>${bos(k.unvan)}</td><td>${bos(k.departman)}</td><td>${bos(k.eposta)}</td><td>${bos(k.telefon)}</td><td>${bos(k.cepTelefonu)}</td><td>${k.mutabakatYetkilisi ? rozet('Evet', 'ok') : 'Hayır'}</td><td>${k.anaYetkili ? rozet('Ana Yetkili', 'bilgi') : '—'}</td><td>${k.epostaGonder ? rozet('Evet', 'ok') : 'Hayır'}</td><td>${k.dogru === true ? rozet('Evet', 'ok') : k.dogru === false ? rozet('Hayır', 'hata') : rozet('Kontrol Bekliyor', 'bekle')}</td><td><small>${esc(k.not || '')}</small></td></tr>`).join('')}</tbody></table></div>` : '<div class="na-bos">Kayıtlı mutabakat kişisi yok — temsilci iletişim kontrolünde ekler.</div>';
  const bekleyenTablo = c => c.bekleyenler.length ? `<div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tür</th><th>Tarih</th><th class="s">Tutar</th><th>Para Birimi</th><th>Açıklama</th></tr></thead><tbody>${c.bekleyenler.map(b => `<tr><td>${esc(b.tur)}</td><td>${esc(b.tarih)}</td><td class="s">${tl(b.tutar)}</td><td>${esc(b.paraBirimi)}</td><td class="ad">${esc(b.aciklama)}</td></tr>`).join('')}</tbody></table></div>` : '<div class="na-bos">Bekleyen ödeme / tahsilat yok.</div>';
  function cariKartHtml(k, c) {
    if (!k || !k.kod) return '<div class="na-kart"><div class="na-kart-g na-bos">Cari kart bulunamadı.</div></div>';
    // Alan adları ve sırası Müşteri Cari Kartları drawer'ındaki masterHtml() ile bire bir (20 alan)
    const a = [['1. Cari Kodu', k.kod], ['2. Cari Kısa Adı', k.kisaAd], ['3. Kısaltmalı Ünvan', k.kisaltmaliUnvan], ['4. Resmi Ünvan', k.resmiUnvan], ['5. Vergi No / TCKN', k.vergiNo], ['6. Vergi Dairesi', k.vergiDairesi], ['7. Cari Kart Tipi', k.kartTipi], ['8. Cari Bölge', k.bolge], ['9. Ülke', k.ulke], ['10. Şehir', k.sehir],
      ['11. Cari Sınıfı', k.sinif], ['12. İlişki Durumu', k.iliskiDurumu], ['13. Cari Rolü', k.rol], ['14. Eş Cari Kodu', k.esCariKodu ? k.esCariKodu + (k.esKartAd ? ' · ' + k.esKartAd : '') : ''], ['15. Yetkili Kişi', k.yetkili], ['16. Telefon', k.telefon], ['17. E-posta', k.eposta], ['18. Vade / Ödeme Şartı', k.vadeSarti], ['19. Durum', k.durum + (k.durumKaynagi ? ' · ' + k.durumKaynagi : '') + (c.kartSonHareketTr ? ' · son hareket ' + c.kartSonHareketTr + (c.kartSonHareketKaynak ? ' (' + c.kartSonHareketKaynak + ')' : '') : '')], ['Şirket (ERP)', k.sirket]];
    const f = k.faaliyetler || [];
    return `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-database me-1"></i>CARİ_MASTER</span><span class="not">${esc(k.olusturmaTarihi || '')}${k.olusturan ? ' · ' + esc(k.olusturan) : ''} · <span data-url="/CariKart/Musteri?kod=${esc(k.kod)}" data-baslik="Müşteri Cari Kartları" style="color:#0e7490;cursor:pointer">Cari Kartı Aç</span></span></div><div class="na-kart-g"><div class="ck-master">${a.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}<div style="grid-column:1/-1"><div class="l">20. Not</div><div class="v" style="font-weight:500">${bos(k.not)}</div></div></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-diagram-project me-1"></i>CARİ_FAALİYET_EŞLEŞTİRME (${f.length})</span><span class="not">faaliyet grubu bazında satış temsilcisi · ana faaliyet tek satırda Evet</span></div><div class="na-kart-g p-0">${f.length ? `<div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>#</th><th>Faaliyet Ana Grubu</th><th>Faaliyet Grubu</th><th>Satış Temsilcisi</th><th>Ana Faaliyet</th><th>Ürün Sınıfı › Grubu › Serisi › Adı</th><th>Durum</th><th>Başlangıç</th><th>Not</th></tr></thead><tbody>${f.map(x => `<tr class="${x.anaFaaliyet ? 'table-active' : ''}"><td>${x.sira}</td><td>${esc(x.anaGrup)}</td><td><b>${esc(x.grup)}</b></td><td>${bos(x.satisTemsilcisi)}</td><td>${x.anaFaaliyet ? rozet('Evet', 'ok') : 'Hayır'}</td><td><small>${esc([x.urunSinifi, x.urunGrubu, x.urunSerisi, x.urunAdi].filter(Boolean).join(' › ') || '—')}</small></td><td>${kartRozet(x.durum)}</td><td>${bos(x.baslangicTr)}</td><td><small>${esc(x.not || '')}</small></td></tr>`).join('')}</tbody></table></div>` : '<div class="na-bos">Faaliyet eşleştirmesi yok.</div>'}</div></div>`;
  }
  const logListe = (l, cls) => l.length ? `<ul class="na-zaman ${cls || ''}">${l.map(x => `<li class="${/Gönder|Yanıt/.test(x.islem) ? 'wf' : /Kapanış|Fark/.test(x.islem) ? 'muh' : /Onay|Tamamlandı/.test(x.islem) ? 'fin' : 'sis'}"><span class="t">${esc(x.zaman)}</span><b>${esc(x.islem)}</b> <span class="k">${esc(x.kullanici)}</span>${x.onceki || x.yeni ? `<br><small class="text-muted">${esc(x.onceki || '—')} → ${esc(x.yeni || '—')}</small>` : ''}${x.aciklama ? `<br><small>${esc(x.aciklama)}</small>` : ''}${x.durumGecisi ? `<br><small class="text-primary">Durum: ${esc(x.durumGecisi)}</small>` : ''}</li>`).join('')}</ul>` : '<div class="na-bos">Kayıt yok.</div>';

  // ---- drawer (mutabakat dosyası + cari kart)
  function drawerAc(kod, sessiz) {
    const c = cari(kod); if (!c) return; acikDrawer = kod;
    $('#cmDrawerBaslik').innerHTML = `${esc(c.kod)} · ${esc(c.kisaAd)} ${durumRozet(c.durum)} ${c.aktif ? rozet('Aktif', 'ok') : rozet('Pasif', 'notr')}`;
    $('#cmDrawerAksiyon').innerHTML = islemHtml(c, 'drawer') + `<button type="button" class="btn btn-light btn-sm" data-act="yenileBakiye" data-kod="${esc(c.kod)}" title="Bakiye ve kurları yeniden hesapla"><i class="fas fa-rotate me-1"></i>Bakiyeyi Yenile</button>`;
    const k = c.kontrol;
    $('#cmDrawerIc').innerHTML = `
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-route me-1"></i>Süreç Özeti</span><span class="not">${c.surecAcik ? `süreç #${c.surecNo} · ${esc(c.temsilciyeGonderen)} · ${esc(c.temsilciyeGonderim)}` : 'açık süreç yok'}</span></div><div class="na-kart-g">
        <div class="cm-ozet">
          <div><div class="l">Mutabakat Durumu</div><div class="v">${durumRozet(c.durum)}</div></div><div><div class="l">Aktif / Pasif (${V.parametreler.aktifPasifAy} Ay)</div><div class="v">${c.aktif ? 'Aktif' : 'Pasif'} · son hareket ${bos(c.sonHareketTr)}</div></div>
          <div><div class="l">İletişim Bilgileri</div><div class="v">${iletisimRozet(c.iletisimDurumu)}${c.iletisimOnay ? `<small class="text-muted"> ${esc(c.iletisimOnaylayan)} · ${esc(c.iletisimOnay)}</small>` : ''}</div></div><div><div class="l">İç Yetkili</div><div class="v">${c.temsilciRolleri.map(t => `${esc(t.ad)} <small class="text-muted">(${esc(t.rol)})</small>`).join(' + ')}</div></div>
          <div><div class="l">Temsilci Onayı</div><div class="v">${k.onaylar.length ? k.onaylar.map(o => `${esc(o.temsilci)} · ${esc(o.zaman)}`).join('<br>') : '—'}${c.temsilciler.length > 1 ? `<small class="text-muted"> ikili onay ${k.onaylar.length}/${c.temsilciler.length}</small>` : ''}</div></div><div><div class="l">Kapanış Kontrolü</div><div class="v">${c.kapanisGerekli ? (c.kapanis.cozuldu ? `${rozet('Çözüldü', 'ok')} ${esc(c.kapanis.secim)} · ${esc(c.kapanis.cozen)} ${esc(c.kapanis.zaman)}` : rozet('Bekliyor', 'hata')) : 'Gerekmiyor'}</div></div>
          <div><div class="l">Fark / Düzeltme</div><div class="v">${k.farkVar ? (k.farkCozuldu ? `${rozet('Çözüldü', 'ok')} ${esc(k.farkCozum)}` : `${rozet('Bekliyor', 'hata')} ${esc(k.farkAciklama)} <small class="text-muted">${esc(k.farkBildiren)} ${esc(k.farkZaman)}</small>`) : 'Yok'}</div></div><div><div class="l">Son Mutabakat</div><div class="v">${bos(c.sonMutabakatTr)}${c.sonMutabakatTr ? ' · sonrası işlem ' + (c.sonMutabakatSonrasiIslem ? 'Var' : 'Yok') : ''}</div></div>
        </div>
        <div class="mt-2"><div class="l" style="font-size:.58rem;font-weight:800;text-transform:uppercase;color:#94a3b8">Gönderim Öncesi Blokaj Kontrolleri</div>${blokajHtml(c)}</div>
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-coins me-1"></i>Bakiye ve Döviz Detayı</span><span class="not">bakiye tarihi ${esc(c.bakiyeZamani)} · açık çek / senet ${c.acikCekAdet} adet ${tl(c.acikCekTl)} TL · bekleyen ${c.bekleyenAdet} adet ${tl(c.bekleyenTl)} TL · mahsup / virman bekleyen ${c.mahsupVirmanBekleyen}</span></div><div class="na-kart-g p-0">${dovizTablo(c)}</div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-hourglass-half me-1"></i>Bekleyen Ödeme / Tahsilat (${c.bekleyenAdet})</span></div><div class="na-kart-g p-0">${bekleyenTablo(c)}</div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-address-book me-1"></i>Mutabakat Kişileri (${c.kisiler.length})</span><span class="not">ana yetkili tek satırda · e-posta gönderimi ana yetkiliye</span></div><div class="na-kart-g p-0">${kisiTablo(c)}</div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-paper-plane me-1"></i>Mutabakat Gönderimleri (${c.gonderimler.length})</span></div><div class="na-kart-g p-0">${c.gonderimler.length ? `<div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Belge No</th><th>Gönderim</th><th>Bakiye Tarihi</th><th>Kime</th><th>Kanal</th><th>Bakiye Özeti</th><th class="s">TL Karşılığı</th><th>Sonuç</th><th>Yanıt</th><th>Gönderen</th></tr></thead><tbody>${c.gonderimler.map(g => `<tr><td><b>${esc(g.belgeNo)}</b></td><td>${esc(g.tarih)}${g.tekrar ? `<small>${g.tekrar}. tekrar</small>` : ''}</td><td>${esc(g.bakiyeTarihi)}</td><td>${esc(g.kime)}</td><td>${esc(g.kanal)}</td><td>${esc(g.ozet)}</td><td class="s">${tl(g.tlKarsiligi)}</td><td>${sonucRozet(g.sonuc)}</td><td>${g.yanitTarihi ? `${esc(g.yanitTarihi)}<small>${esc(g.yanitAciklama)}</small>` : '<span class="text-muted">—</span>'}</td><td>${esc(g.gonderen)}</td></tr>`).join('')}</tbody></table></div>` : '<div class="na-bos">Gönderim yok.</div>'}</div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Cari Günlüğü</span><span class="not">kim · ne zaman · önceki → yeni · durum geçişi</span></div><div class="na-kart-g">${logListe(c.log)}</div></div>
      ${cariKartHtml(c.kart, c)}`;
    $('#cmDrawer').classList.add('acik'); $('#cmPerde').classList.add('acik'); if (!sessiz) $('#cmDrawer').scrollTop = 0;
  }
  function drawerKapat() { $('#cmDrawer').classList.remove('acik'); $('#cmPerde').classList.remove('acik'); acikDrawer = null; }

  // ---- temsilci ekranı (§5 A. iletişim, §6 B. bakiye)
  function cizTemsilci() {
    $('#cmTemsilciAd').textContent = temsilciRolu() ? TEMSILCI : ROL + (TEMSILCI ? ' · ' + TEMSILCI : '');
    const l = C.filter(c => kontrolde(c) && benimCarim(c));
    $('#cmTemsilciNot').textContent = `${l.length} cari · ${l.filter(c => c.iletisimDurumu !== 'Tamamlandı').length} iletişim · ${l.filter(c => c.iletisimDurumu === 'Tamamlandı').length} bakiye`;
    const adim = c => c.iletisimDurumu !== 'Tamamlandı' ? rozet('A · İletişim', 'bekle') : c.kontrol.onaylar.some(o => o.temsilci === TEMSILCI) ? rozet('Onaylandı', 'ok') : rozet('B · Bakiye', 'bilgi');
    $('#cmTemsilciListe tbody').innerHTML = l.length ? l.map(c => `<tr class="cm-liste-satir ${acikKontrol === c.kod ? 'secili' : ''}" data-kod="${esc(c.kod)}"><td class="ad"><b>${esc(c.kod)}</b> ${esc(c.kisaAd)}<small>${tipRozet(c.kartTipi)} ${c.temsilciler.join(' + ')}</small></td><td>${esc(c.sirket)}</td><td>${adim(c)}</td><td>${durumRozet(c.durum)}</td></tr>`).join('') : '<tr><td colspan="4" class="na-bos">Kontrol bekleyen cari yok. Muhasebe "Temsilciye Gönder" yaptığında burada listelenir.</td></tr>';
    $$('#cmTemsilciListe tr.cm-liste-satir').forEach(r => r.addEventListener('click', () => kontrolPanel(r.dataset.kod)));
    if (acikKontrol && cari(acikKontrol)) kontrolPanel(acikKontrol); else $('#cmKontrolPanel').innerHTML = '<div class="na-kart"><div class="na-kart-g na-bos">Soldaki listeden kontrol edilecek cariyi seçin.</div></div>';
  }
  function kisiSatir(k, i) {
    const sec = (ad, l, v) => `<select class="form-select" data-a="${ad}"><option value=""></option>${l.map(x => `<option ${x === v ? 'selected' : ''}>${esc(x)}</option>`).join('')}</select>`;
    return `<tr data-id="${k.id || 0}"><td><input class="form-control" data-a="adSoyad" value="${esc(k.adSoyad || '')}" placeholder="Ad Soyad"></td><td>${sec('unvan', V.unvanlar, k.unvan)}</td><td>${sec('departman', V.departmanlar, k.departman)}</td><td><input class="form-control" data-a="eposta" type="email" value="${esc(k.eposta || '')}" placeholder="e-posta"></td><td><input class="form-control" data-a="telefon" value="${esc(k.telefon || '')}"></td><td><input class="form-control" data-a="cepTelefonu" value="${esc(k.cepTelefonu || '')}"></td><td><input type="checkbox" data-a="mutabakatYetkilisi" ${k.mutabakatYetkilisi ? 'checked' : ''}></td><td><input type="radio" name="cmAna" data-a="anaYetkili" ${k.anaYetkili ? 'checked' : ''}></td><td><input type="checkbox" data-a="epostaGonder" ${k.epostaGonder ? 'checked' : ''}></td><td><select class="form-select" data-a="dogru"><option value="" ${k.dogru === null || k.dogru === undefined ? 'selected' : ''}>Kontrol Bekliyor</option><option value="true" ${k.dogru === true ? 'selected' : ''}>Evet</option><option value="false" ${k.dogru === false ? 'selected' : ''}>Hayır</option></select></td><td><input class="form-control" data-a="not" value="${esc(k.not || '')}" placeholder="not"></td><td><button type="button" class="btn btn-light btn-sm py-0" data-kisi-sil title="Satırı kaldır"><i class="fas fa-trash text-danger"></i></button></td></tr>`;
  }
  function kontrolPanel(kod) {
    const c = cari(kod); if (!c) return; acikKontrol = kod;
    $$('#cmTemsilciListe tr.cm-liste-satir').forEach(r => r.classList.toggle('secili', r.dataset.kod === kod));
    const duzenle = yetki('iletisim') && c.surecAcik, onayHakki = yetki('bakiyeOnay') && c.surecAcik, iletisimTamam = c.iletisimDurumu === 'Tamamlandı', k = c.kontrol;
    const benOnayladim = k.onaylar.some(o => o.temsilci === TEMSILCI);
    const soru = (ad, m) => `<label><input type="checkbox" data-s="${ad}" ${k[ad] ? 'checked' : ''} ${onayHakki && !benOnayladim ? '' : 'disabled'}>${m}</label>`;
    $('#cmKontrolPanel').innerHTML = `
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-building me-1"></i>${esc(c.kod)} · ${esc(c.kisaAd)} ${durumRozet(c.durum)}</span><span class="d-flex gap-1"><button type="button" class="btn btn-light btn-sm py-0" data-act="detay" data-kod="${esc(c.kod)}" style="font-size:.66rem"><i class="fas fa-circle-info me-1"></i>Dosya / Cari Kart</button></span></div><div class="na-kart-g">
        <div class="cm-ozet"><div><div class="l">Resmi Ünvan</div><div class="v">${esc(c.resmiUnvan)}</div></div><div><div class="l">Şirket · Kart Tipi · Rol</div><div class="v">${esc(c.sirket)} · ${esc(c.kartTipi)} · ${esc(c.rol)}</div></div><div><div class="l">İç Yetkili</div><div class="v">${c.temsilciRolleri.map(t => `${esc(t.ad)} <small class="text-muted">(${esc(t.rol)})</small>`).join(' + ')}</div></div><div><div class="l">Temsilciye Gönderim</div><div class="v">${esc(c.temsilciyeGonderen)} · ${esc(c.temsilciyeGonderim)}</div></div></div>
        <div class="cm-adim mt-2 mb-0"><span class="${iletisimTamam ? 'ok' : 'aktif'}">A · İletişim Bilgileri ${iletisimTamam ? '✓' : ''}</span><i class="fas fa-arrow-right"></i><span class="${benOnayladim ? 'ok' : iletisimTamam ? 'aktif' : ''}">B · Bakiye Kontrolü ${benOnayladim ? '✓' : ''}</span><i class="fas fa-arrow-right"></i><span class="${c.durum === 'Gönderime Hazır' ? 'ok' : ''}">Gönderime Hazır</span></div>
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-address-book me-1"></i>A. İletişim Bilgileri Kontrolü ${iletisimRozet(c.iletisimDurumu)}</span><span class="not">${c.iletisimOnay ? `tamamlandı: ${esc(c.iletisimOnaylayan)} · ${esc(c.iletisimOnay)}` : 'birden fazla kişi · tek ana yetkili · ana yetkiliye e-posta'}</span></div><div class="na-kart-g">
        ${c.iletisimDurumu === 'Eksik' ? '<div class="cm-uyari kirmizi">Ana mutabakat yetkilisi ve e-posta adresi eksik. Kişi ekleyin, mutabakat yetkilisini ve <b>tek</b> ana yetkiliyi işaretleyin, e-posta girin.</div>' : !iletisimTamam ? '<div class="cm-uyari sari">Her kişi için "Bilgiler Doğru mu?" alanını <b>Evet</b> yapıp <b>İletişim Bilgileri Tamamlandı</b> düğmesine basın. Yanlış bilgi varsa düzeltin ve kaydedin.</div>' : '<div class="cm-uyari yesil">İletişim bilgileri kontrol edildi ve tamamlandı. Değişiklik gerekirse düzenleyip yeniden tamamlayın.</div>'}
        <div class="table-responsive"><table class="na-tablo cm-kisi" id="cmKisiTablo" data-dinamik="kapali"><thead><tr><th>Ad Soyad</th><th>Ünvan</th><th>Departman</th><th>E-posta</th><th>Telefon</th><th>Cep Telefonu</th><th>Mutabakat Yetkilisi</th><th>Ana Yetkili</th><th>E-posta Gönder</th><th>Bilgiler Doğru mu?</th><th>Not</th><th></th></tr></thead><tbody>${c.kisiler.map(kisiSatir).join('')}</tbody></table></div>
        <div class="d-flex gap-1 mt-2 flex-wrap ${duzenle ? '' : 'cm-kilit'}"><button type="button" class="btn btn-light btn-sm" id="cmKisiEkle" style="font-size:.7rem"><i class="fas fa-plus me-1"></i>Kişi Ekle</button><button type="button" class="btn btn-primary btn-sm" id="cmKisiKaydet" style="font-size:.7rem"><i class="fas fa-floppy-disk me-1"></i>Kaydet</button><button type="button" class="btn btn-success btn-sm" id="cmIletisimTamamla" style="font-size:.7rem" ${iletisimTamam ? 'disabled' : ''}><i class="fas fa-check-double me-1"></i>İletişim Bilgileri Tamamlandı</button><span class="na-mini ms-auto">${duzenle ? 'Kaydet → ardından Tamamlandı' : 'Bu rolde düzenleme yetkisi yok'}</span></div>
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-scale-balanced me-1"></i>B. Bakiye Kontrolü ${benOnayladim ? rozet('Onaylandı', 'ok') : k.farkVar && !k.farkCozuldu ? rozet('Fark Bildirildi', 'hata') : iletisimTamam ? rozet('Kontrol Bekliyor', 'bekle') : rozet('Kilitli', 'notr')}</span><span class="not">bakiye tarihi ${esc(c.bakiyeZamani)} · kur tarihi ${esc(V.kurTarihi)}</span></div><div class="na-kart-g">
        ${!iletisimTamam ? '<div class="cm-uyari sari"><i class="fas fa-lock me-1"></i>Bakiye kontrolü iletişim bilgileri tamamlanmadan nihai olarak onaylanamaz. Önce A adımını bitirin.</div>' : ''}
        ${k.farkVar && !k.farkCozuldu ? `<div class="cm-uyari kirmizi"><i class="fas fa-flag me-1"></i>Fark / düzeltme bildirildi (${esc(k.farkBildiren)} · ${esc(k.farkZaman)}): ${esc(k.farkAciklama)} — muhasebe çözümü bekleniyor; çözülmeden cari Gönderime Hazır olamaz.</div>` : k.farkVar && k.farkCozuldu ? `<div class="cm-uyari yesil">Fark çözüldü (${esc(k.farkCozen)} · ${esc(k.farkCozumZaman)}): ${esc(k.farkCozum)}. Kontrolü yeniden yapıp onaylayın.</div>` : ''}
        ${c.kapanisGerekli ? `<div class="cm-uyari ${c.kapanis.cozuldu ? 'yesil' : 'kirmizi'}"><i class="fas fa-triangle-exclamation me-1"></i>Bakiye yaşlandırması: ${riskHtml(c)} en eski açık fatura ${esc(c.enEskiAcikFaturaTr)} (${c.bakiyeYasi} gün). ${c.kapanis.cozuldu ? `Muhasebe kapanış kontrolü: ${esc(c.kapanis.secim)} · ${esc(c.kapanis.aciklama)}` : 'Muhasebe kapanış kontrolü bekleniyor.'}</div>` : ''}
        <div>
          <div class="cm-ozet mb-2">
            <div><div class="l">Toplam Bakiye (TL)</div><div class="v">${tl(c.bakiyeTl)} · ${esc(c.bakiyeYonu)}</div></div><div><div class="l">Açık Fatura</div><div class="v">${c.acikFaturaAdet} adet · ${tl(c.acikFaturaTl)} TL</div></div><div><div class="l">Açık Çek / Senet</div><div class="v">${c.acikCekAdet} adet · ${tl(c.acikCekTl)} TL</div></div><div><div class="l">Bekleyen Ödeme / Tahsilat</div><div class="v">${c.bekleyenAdet} adet · ${tl(c.bekleyenTl)} TL</div></div>
            <div><div class="l">Mahsup / Virman Bekleyen</div><div class="v">${c.mahsupVirmanBekleyen}</div></div><div><div class="l">Son Hareket</div><div class="v">${bos(c.sonHareketTr)}</div></div><div><div class="l">Bakiye Yaşlandırması</div><div class="v">${c.enEskiAcikFaturaTr ? `${esc(c.enEskiAcikFaturaTr)} · ${c.bakiyeYasi} gün ${riskHtml(c)}` : 'Açık kalem yok'}</div></div><div><div class="l">Son Mutabakat</div><div class="v">${bos(c.sonMutabakatTr)}${c.sonMutabakatTr ? ' · sonrası işlem ' + (c.sonMutabakatSonrasiIslem ? 'Var' : 'Yok') : ''}</div></div>
          </div>
          ${dovizTablo(c)}
          <div class="mt-2"><div class="l" style="font-size:.58rem;font-weight:800;text-transform:uppercase;color:#94a3b8;margin-bottom:3px">Bekleyen Ödeme / Tahsilat</div>${bekleyenTablo(c)}</div>
          <div class="mt-2 ${iletisimTamam ? '' : 'cm-kilit'}"><div class="l" style="font-size:.58rem;font-weight:800;text-transform:uppercase;color:#94a3b8;margin-bottom:3px">Zorunlu Kontrol Soruları (Tümü Evet Olmalı)</div>
            <div class="cm-soru">${soru('bakiyeDogru', 'Bakiye doğru mu?')}${soru('acikFaturalar', 'Açık faturalar kontrol edildi mi?')}${soru('bekleyenOdeme', 'Bekleyen ödeme / tahsilat kontrol edildi mi?')}${soru('cekSenet', 'Çek / senet durumu kontrol edildi mi?')}${soru('dovizDogru', 'Döviz bakiyeleri doğru mu?')}${soru('gonderimeUygun', 'Mutabakat gönderimine uygun mu?')}</div></div>
          <div id="cmFarkForm" class="mt-2" style="display:${farkFormAcik ? 'block' : 'none'}"><label class="form-label">Fark / Düzeltme Açıklaması <span class="z">*</span></label><div class="d-flex gap-1"><input class="form-control" id="cmFarkAciklama" placeholder="Hangi belge / tutar farklı, ne düzeltilmeli?"><button type="button" class="btn btn-danger btn-sm" id="cmFarkGonder" style="font-size:.7rem;white-space:nowrap"><i class="fas fa-flag me-1"></i>Bildirimi Gönder</button></div></div>
          <div class="d-flex gap-1 mt-2 flex-wrap ${onayHakki && !benOnayladim ? '' : 'cm-kilit'}"><button type="button" class="btn btn-outline-danger btn-sm" id="cmFarkAc" style="font-size:.7rem"><i class="fas fa-flag me-1"></i>Fark / Düzeltme Bildir</button><button type="button" class="btn btn-success btn-sm" id="cmBakiyeOnayla" style="font-size:.7rem"><i class="fas fa-check-double me-1"></i>Kontrolü Tamamla ve Onayla</button><button type="button" class="btn btn-light btn-sm" data-act="yenileBakiye" data-kod="${esc(c.kod)}" style="font-size:.7rem"><i class="fas fa-rotate me-1"></i>Bakiyeyi Yenile</button><span class="na-mini ms-auto">${benOnayladim ? 'Onayınız alındı' + (k.onayTamam ? ' · tüm onaylar tamam' : ` · ikili onay ${k.onaylar.length}/${c.temsilciler.length}`) : onayHakki ? 'Tüm sorular Evet ise onaylayın; fark varsa bildirin' : 'Bu rolde onay yetkisi yok'}</span></div>
        </div>
      </div></div>`;
    const tb = $('#cmKisiTablo tbody');
    $('#cmKisiEkle').addEventListener('click', () => { tb.insertAdjacentHTML('beforeend', kisiSatir({ id: 0, mutabakatYetkilisi: true, epostaGonder: true, anaYetkili: !c.kisiler.length && !tb.children.length }, 0)); });
    tb.addEventListener('click', e => { const b = e.target.closest('[data-kisi-sil]'); if (b) b.closest('tr').remove(); });
    const kisileriOku = () => $$('#cmKisiTablo tbody tr').map(tr => { const o = { id: +tr.dataset.id || 0 }; $$('[data-a]', tr).forEach(el => { const a = el.dataset.a; o[a] = el.type === 'checkbox' || el.type === 'radio' ? el.checked : a === 'dogru' ? (el.value === '' ? null : el.value === 'true') : el.value; }); return o; });
    $('#cmKisiKaydet').addEventListener('click', () => islem('IletisimKaydet', { kod, kisiler: kisileriOku() }));
    $('#cmIletisimTamamla').addEventListener('click', async () => { const r = await post('IletisimKaydet', { kod, kisiler: kisileriOku() }); if (!r.success) { mesaj('warning', esc(r.message)); return; } islem('IletisimTamamla', { kod }); });
    $('#cmFarkAc').addEventListener('click', () => { farkFormAcik = !farkFormAcik; $('#cmFarkForm').style.display = farkFormAcik ? 'block' : 'none'; if (farkFormAcik) $('#cmFarkAciklama').focus(); });
    $('#cmFarkGonder').addEventListener('click', () => { const a = $('#cmFarkAciklama').value.trim(); if (!a) { mesaj('warning', 'Fark / düzeltme açıklaması zorunludur.'); return; } farkFormAcik = false; islem('BakiyeOnayla', Object.assign({ kod, farkVar: true, aciklama: a }, sorulariOku())); });
    $('#cmBakiyeOnayla').addEventListener('click', () => { const s = sorulariOku(); if (!Object.values(s).every(Boolean)) { mesaj('warning', 'Tüm zorunlu kontrol soruları Evet olmadan onay verilemez. Fark varsa "Fark / Düzeltme Bildir" kullanın.'); return; } islem('BakiyeOnayla', Object.assign({ kod, farkVar: false }, s)); });
  }
  const sorulariOku = () => { const o = {}; $$('#cmKontrolPanel [data-s]').forEach(el => o[el.dataset.s] = el.checked); return o; };

  // ---- kapanış kontrolü (§8) + fark bildirimleri
  function cizKapanis() {
    const l = C.filter(c => c.kapanisGerekli).sort((a, b) => (b.bakiyeYasi || 0) - (a.bakiyeYasi || 0));
    $('#cmKapanisTablo tbody').innerHTML = l.length ? l.map(c => `<tr class="${acikKapanis === c.kod ? 'table-active' : ''}"><td class="ad"><b>${esc(c.kod)}</b> ${esc(c.kisaAd)}<small>${esc(c.sirket)} · ${esc(c.kartTipi)}</small></td><td>${c.aktif ? rozet('Aktif', 'ok') : rozet('Pasif', 'notr')}</td><td>${c.dovizler.filter(d => d.eskiBakiyeUyari || d.degerlemeRiski).map(d => `<b>${esc(d.paraBirimi)}</b> ${tl(Math.abs(d.net))}`).join('<br>')}</td><td class="s">${tl(c.bakiyeTl)}<small>${esc(c.bakiyeYonu)} · TL karşılığı</small></td><td class="s">${c.acikFaturaAdet}<small>${tl(c.acikFaturaTl)} TL</small></td><td>${esc(c.enEskiAcikFaturaTr)}</td><td class="s">${c.bakiyeYasi}</td><td>${riskHtml(c)}</td><td>${durumRozet(c.durum)}</td><td>${c.kapanis.cozuldu ? `${rozet('Çözüldü', 'ok')}<small>${esc(c.kapanis.secim)} · ${esc(c.kapanis.cozen)} · ${esc(c.kapanis.zaman)}</small>` : c.surecAcik ? rozet('Bekliyor', 'hata') : `${rozet('Süreç Kapalı', 'notr')}<small>temsilciye gönderilince kontrol edilir</small>`}</td><td class="cm-islem">${!c.kapanis.cozuldu && c.surecAcik && yetki('kapanisCoz') ? `<button type="button" class="btn btn-danger btn-sm" data-act="kapanis" data-kod="${esc(c.kod)}"><i class="fas fa-pen-to-square me-1"></i>Sonuçlandır</button>` : ''}${!c.surecAcik && yetki('temsilciyeGonder') ? `<button type="button" class="btn btn-primary btn-sm" data-act="temsilci" data-kod="${esc(c.kod)}"><i class="fas fa-user-check me-1"></i>Temsilciye Gönder</button>` : ''}<button type="button" class="btn btn-light btn-sm" data-act="detay" data-kod="${esc(c.kod)}"><i class="fas fa-circle-info me-1"></i>Detay</button></td></tr>`).join('') : '<tr><td colspan="11" class="na-bos">Eşikten eski açık bakiye veya değerleme riski olan cari yok.</td></tr>';
    const f = C.filter(c => c.kontrol.farkVar).sort((a, b) => (a.kontrol.farkCozuldu ? 1 : 0) - (b.kontrol.farkCozuldu ? 1 : 0));
    $('#cmFarkTablo tbody').innerHTML = f.length ? f.map(c => `<tr><td class="ad"><b>${esc(c.kod)}</b> ${esc(c.kisaAd)}</td><td>${esc(c.kontrol.farkBildiren)}</td><td>${esc(c.kontrol.farkZaman)}</td><td class="ad">${esc(c.kontrol.farkAciklama)}${c.kontrol.farkCozuldu ? `<small>Çözüm: ${esc(c.kontrol.farkCozum)} · ${esc(c.kontrol.farkCozen)} · ${esc(c.kontrol.farkCozumZaman)}</small>` : ''}</td><td>${c.kontrol.farkCozuldu ? rozet('Çözüldü', 'ok') : rozet('Bekliyor', 'hata')}</td><td class="cm-islem">${!c.kontrol.farkCozuldu && yetki('farkCoz') ? `<button type="button" class="btn btn-danger btn-sm" data-act="fark" data-kod="${esc(c.kod)}"><i class="fas fa-flag me-1"></i>Farkı Çöz</button>` : ''}<button type="button" class="btn btn-light btn-sm" data-act="detay" data-kod="${esc(c.kod)}"><i class="fas fa-circle-info me-1"></i>Detay</button></td></tr>${acikFark === c.kod && !c.kontrol.farkCozuldu ? `<tr><td colspan="6"><div class="d-flex gap-1 align-items-center"><b style="font-size:.7rem;white-space:nowrap">Çözüm Açıklaması <span class="z">*</span></b><input class="form-control" id="cmFarkCozum" placeholder="Yapılan düzeltme / açıklama"><button type="button" class="btn btn-primary btn-sm" data-act="farkKaydet" data-kod="${esc(c.kod)}" style="font-size:.7rem;white-space:nowrap">Çözüldü Olarak Kaydet</button><button type="button" class="btn btn-light btn-sm" data-act="farkKapat" style="font-size:.7rem">Vazgeç</button></div></td></tr>` : ''}`).join('') : '<tr><td colspan="6" class="na-bos">Fark / düzeltme bildirimi yok.</td></tr>';
    if (acikKapanis && cari(acikKapanis)) kapanisFormAc(acikKapanis); else $('#cmKapanisFormKart').style.display = 'none';
  }
  function kapanisFormAc(kod) {
    const c = cari(kod); if (!c) return; acikKapanis = kod;
    $('#cmKapanisFormKart').style.display = ''; $('#cmKapanisFormBaslik').textContent = `${c.kod} · ${c.kisaAd}`;
    const s = $('#cmKapanisSecim'); if (!s.options.length) s.innerHTML = '<option value="">Seçiniz</option>' + V.kapanisSecenekleri.map(x => `<option>${esc(x)}</option>`).join('');
    $('#cmKapanisDetay').innerHTML = `<div class="cm-uyari mavi">Kapanış seçimi: <b>Mahsup</b> (karşılıklı açık kalemleri kapat), <b>Tahsilat / Ödeme</b> (eksik kaydı işle), <b>Virman</b> (hesaplar arası aktarım), <b>Ters Kayıt</b> (hatalı kaydı iptal), <b>İnceleme</b> (bakiyenin gerçekten açık olduğu teyit edildi, mutabakata girsin).</div>${dovizTablo(c)}`;
    $('#cmKapanisFormKart').scrollIntoView({ block: 'nearest', behavior: 'smooth' });
  }
  function kapanisSonuclandir() {
    if (!acikKapanis) return; const secim = $('#cmKapanisSecim').value, aciklama = $('#cmKapanisAciklama').value.trim();
    if (!secim) { mesaj('warning', 'Kapanış seçimi zorunludur.'); return; } if (!aciklama) { mesaj('warning', 'Kapanış açıklaması zorunludur.'); return; }
    const kod = acikKapanis; islem('KapanisCoz', { kod, secim, aciklama }, () => { acikKapanis = null; $('#cmKapanisAciklama').value = ''; $('#cmKapanisSecim').value = ''; $('#cmKapanisFormKart').style.display = 'none'; });
  }
  function kapanisCsv() { csvIndir('kapanis_kontrolu', ['Cari Kodu', 'Cari', 'Şirket', 'Aktif / Pasif', 'Bakiye (TL)', 'Açık Fatura', 'En Eski Açık Fatura', 'Bakiye Yaşı (Gün)', 'Eski Bakiye', 'Değerleme Riski', 'Mutabakat Durumu', 'Kapanış Kontrolü'], C.filter(c => c.kapanisGerekli).map(c => [c.kod, c.kisaAd, c.sirket, c.aktif ? 'Aktif' : 'Pasif', sayi(c.bakiyeTl), c.acikFaturaAdet, c.enEskiAcikFaturaTr, c.bakiyeYasi ?? '', c.eskiBakiyeUyari ? 'Evet' : 'Hayır', c.degerlemeRiski ? 'Evet' : 'Hayır', c.durum, c.kapanis.cozuldu ? 'Çözüldü · ' + c.kapanis.secim : 'Bekliyor'])); }

  // ---- gönderimler ve yanıtlar (§10)
  const tumGonderimler = () => C.flatMap(c => c.gonderimler.map(g => Object.assign({ kod: c.kod, kisaAd: c.kisaAd, durum: c.durum, sonMu: g.id === Math.max(...c.gonderimler.map(x => x.id)) }, g))).sort((a, b) => b.id - a.id);
  function cizGonderim() {
    const f = $('#cmGonderimF').value, l = tumGonderimler().filter(g => !f || g.sonuc === f);
    $('#cmGonderimTablo tbody').innerHTML = l.length ? l.map(g => `<tr><td><b>${esc(g.belgeNo)}</b><small>${g.surecNo ? 'süreç #' + g.surecNo : 'önceki dönem'}</small></td><td>${esc(g.tarih)}${g.tekrar ? `<small>${g.tekrar}. tekrar</small>` : ''}</td><td class="ad"><b>${esc(g.kod)}</b> ${esc(g.kisaAd)}<small>${durumRozet(g.durum)}</small></td><td>${esc(g.kime)}</td><td>${esc(g.kanal)}</td><td>${esc(g.ozet)}<small>bakiye tarihi ${esc(g.bakiyeTarihi)}</small></td><td class="s">${tl(g.tlKarsiligi)}</td><td>${sonucRozet(g.sonuc)}</td><td>${g.yanitTarihi ? `${esc(g.yanitTarihi)}<small>${esc(g.yanitAciklama)}</small>` : '<span class="text-muted">—</span>'}</td><td>${esc(g.gonderen)}</td>
      <td class="cm-islem"><button type="button" class="btn btn-outline-secondary btn-sm" data-act="pdf" data-kod="${esc(g.kod)}"><i class="fas fa-file-pdf me-1"></i>PDF</button>${g.sonMu && g.sonuc === 'Bekliyor' && yetki('yanitIsle') ? `<button type="button" class="btn btn-info btn-sm" data-act="yanit" data-kod="${esc(g.kod)}"><i class="fas fa-reply me-1"></i>Yanıt İşle</button>` : ''}${g.sonMu && g.sonuc === 'Bekliyor' && yetki('mutabakatGonder') ? `<button type="button" class="btn btn-outline-secondary btn-sm" data-act="tekrar" data-kod="${esc(g.kod)}"><i class="fas fa-rotate-right me-1"></i>Tekrar Gönder</button>` : ''}${g.sonMu && g.sonuc === 'İtirazlı' && g.durum === 'İtirazlı' && yetki('itirazCoz') ? `<button type="button" class="btn btn-danger btn-sm" data-act="itiraz" data-kod="${esc(g.kod)}"><i class="fas fa-gavel me-1"></i>İtirazı Çöz</button>` : ''}<button type="button" class="btn btn-light btn-sm" data-act="detay" data-kod="${esc(g.kod)}"><i class="fas fa-circle-info me-1"></i>Detay</button></td></tr>
      ${acikItiraz === g.kod && g.sonMu && g.sonuc === 'İtirazlı' ? `<tr><td colspan="11"><div class="d-flex gap-1 align-items-center"><b style="font-size:.7rem;white-space:nowrap">İtiraz Çözümü <span class="z">*</span></b><input class="form-control" id="cmItirazAciklama" placeholder="Yapılan düzeltme / mutabık kalınan fark; cari yeniden iç kontrole gider"><button type="button" class="btn btn-primary btn-sm" data-act="itirazKaydet" data-kod="${esc(g.kod)}" style="font-size:.7rem;white-space:nowrap">Çöz ve Yeniden Kontrole Gönder</button><button type="button" class="btn btn-light btn-sm" data-act="itirazKapat" style="font-size:.7rem">Vazgeç</button></div></td></tr>` : ''}`).join('') : '<tr><td colspan="11" class="na-bos">Gönderim yok.</td></tr>';
    if (acikYanit && cari(acikYanit)) yanitAc(acikYanit); else $('#cmYanitKart').style.display = 'none';
  }
  function yanitAc(kod) { const c = cari(kod); if (!c) return; acikYanit = kod; $('#cmYanitKart').style.display = ''; $('#cmYanitBaslik').textContent = `${c.kod} · ${c.kisaAd} · ${(c.gonderimler[0] || {}).belgeNo || ''}`; $('#cmYanitKart').scrollIntoView({ block: 'nearest', behavior: 'smooth' }); }
  function yanitKaydet() {
    if (!acikYanit) return; const sonuc = $('#cmYanitSonuc').value, aciklama = $('#cmYanitAciklama').value.trim();
    if (sonuc === 'İtirazlı' && !aciklama) { mesaj('warning', 'İtirazda fark detayı / açıklama zorunludur.'); return; }
    const kod = acikYanit; islem('YanitIsle', { kod, sonuc, aciklama }, () => { acikYanit = null; $('#cmYanitAciklama').value = ''; $('#cmYanitKart').style.display = 'none'; });
  }
  function gonderimCsv() { csvIndir('mutabakat_gonderimleri', ['Belge No', 'Süreç', 'Gönderim', 'Bakiye Tarihi', 'Cari Kodu', 'Cari', 'Kime', 'Kanal', 'Bakiye Özeti', 'TL Karşılığı', 'Sonuç', 'Yanıt Tarihi', 'Yanıt Açıklaması', 'Gönderen', 'Tekrar'], tumGonderimler().map(g => [g.belgeNo, g.surecNo, g.tarih, g.bakiyeTarihi, g.kod, g.kisaAd, g.kime, g.kanal, g.ozet, sayi(g.tlKarsiligi), g.sonuc, g.yanitTarihi, g.yanitAciklama, g.gonderen, g.tekrar])); }

  // ---- günlük ve parametreler (§11)
  function cizGunluk() {
    $('#cmAudit tbody').innerHTML = V.genelLog.length ? V.genelLog.map(x => `<tr><td>${esc(x.zaman)}</td><td>${esc(x.kullanici)}</td><td class="ad"><b>${esc(x.islem)}</b></td><td class="ad"><small>${esc(x.onceki || '—')}</small></td><td class="ad"><small>${esc(x.yeni || '—')}</small></td><td class="ad"><small>${esc(x.aciklama || '')}</small></td><td>${x.durumGecisi ? `<small class="text-primary">${esc(x.durumGecisi)}</small>` : '<span class="text-muted">—</span>'}</td></tr>`).join('') : '<tr><td colspan="7" class="na-bos">Kayıt yok.</td></tr>';
    if (document.activeElement !== $('#cmPAy') && document.activeElement !== $('#cmPEski') && document.activeElement !== $('#cmPDeger')) { $('#cmPAy').value = V.parametreler.aktifPasifAy; $('#cmPEski').value = V.parametreler.eskiBakiyeGun; $('#cmPDeger').value = V.parametreler.degerlemeRiskiGun; }
    const izin = yetki('parametre'); $('#cmPKaydet').disabled = !izin; $$('#cmPAy, #cmPEski, #cmPDeger').forEach(i => i.disabled = !izin);
  }
  function parametreKaydet() { islem('ParametreKaydet', { aktifPasifAy: +$('#cmPAy').value || null, eskiBakiyeGun: +$('#cmPEski').value || null, degerlemeRiskiGun: +$('#cmPDeger').value || null }); }

  // ---- işlem düğmeleri (tüm sekmeler + drawer)
  kok.addEventListener('click', e => {
    const b = e.target.closest('[data-act], [data-eks], [data-pdf], [data-xls]'); if (!b) return;
    e.stopPropagation();
    if (b.dataset.eks) { const [kod, pb] = b.dataset.eks.split('|'); ekstreGoster(kod, pb, b.closest('.na-kart-g') || b.closest('.cm-drawer') || kok); return; }
    if (b.dataset.pdf) { const [kod, pb] = b.dataset.pdf.split('|'); window.open(`/CariMutabakat/Ekstre?kod=${encodeURIComponent(kod)}&pb=${encodeURIComponent(pb)}`, '_blank'); return; }
    if (b.dataset.xls) { const [kod, pb] = b.dataset.xls.split('|'); ekstreExcel(kod, pb); return; }
    const act = b.dataset.act, kod = b.dataset.kod;
    if (act === 'detay') drawerAc(kod);
    else if (act === 'temsilci') islem('TemsilciyeGonder', { kod });
    else if (act === 'gonder') islem('MutabakatGonder', { kod });
    else if (act === 'tekrar') islem('TekrarGonder', { kod });
    else if (act === 'pdf') window.open(`/CariMutabakat/Mektup?kod=${encodeURIComponent(kod)}`, '_blank');
    else if (act === 'yenileBakiye') islem('Yenile', { kod });
    else if (act === 'kontrol') { drawerKapat(); farkFormAcik = false; acikKontrol = kod; sekme('temsilci'); }
    else if (act === 'kapanis') { drawerKapat(); acikKapanis = kod; sekme('kapanis'); }
    else if (act === 'fark') { drawerKapat(); acikFark = kod; sekme('kapanis'); setTimeout(() => { const i = $('#cmFarkCozum'); if (i) i.focus(); }, 50); }
    else if (act === 'farkKapat') { acikFark = null; cizKapanis(); }
    else if (act === 'farkKaydet') { const a = ($('#cmFarkCozum') || {}).value?.trim(); if (!a) { mesaj('warning', 'Çözüm açıklaması zorunludur.'); return; } islem('FarkCoz', { kod, aciklama: a }, () => { acikFark = null; }); }
    else if (act === 'yanit') { drawerKapat(); acikYanit = kod; sekme('gonderim'); }
    else if (act === 'itiraz') { drawerKapat(); acikItiraz = kod; sekme('gonderim'); setTimeout(() => { const i = $('#cmItirazAciklama'); if (i) i.focus(); }, 50); }
    else if (act === 'itirazKapat') { acikItiraz = null; cizGonderim(); }
    else if (act === 'itirazKaydet') { const a = ($('#cmItirazAciklama') || {}).value?.trim(); if (!a) { mesaj('warning', 'Çözüm açıklaması zorunludur.'); return; } islem('ItirazCoz', { kod, aciklama: a }, () => { acikItiraz = null; }); }
  });
  // ---- başlangıç
  (async () => {
    await yukle(true);
    if (!V) return;
    const s = kok.dataset.baslangic || 'aktif';
    sekme(s);
    if (kok.dataset.kod) drawerAc(kok.dataset.kod);
  })();
})();
