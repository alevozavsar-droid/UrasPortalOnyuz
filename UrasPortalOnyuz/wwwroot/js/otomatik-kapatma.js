// ============================================================
// OTOMATİK KAPATMA (Views/Rapor64/Index.cshtml) — açık fatura ↔ eşleştirilmemiş ödeme kapatma: cari listesi, cari eşleştirme (otomatik / elle), toplu otomatik, kapama kayıtları
//   Uçlar: /Kapama/Veri, /Kapama/Detay, /Kapama/Oner, /Kapama/Kapat, /Kapama/TumunuKapat (Controllers/KapamaController.cs)
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('ok'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const tl = v => Number(v || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 }) + ' ₺';
  const n2 = v => Number(v || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const durumRozet = d => rozet(d, d === 'Kapatılabilir' ? 'ok' : d === 'Ödeme Bekliyor' ? 'bekle' : d === 'Yalnız Açık Fatura' ? 'bilgi' : 'notr');
  const post = (u, b) => fetch(u, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(b) }).then(r => r.json());
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; }
  function sekme(p) { $$('#okSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('.na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); }
  let V = null, KUR = {}, D = null, CIFT = [], S = null;
  const kurTl = pb => pb === 'TL' || pb === 'TRY' || !pb ? 1 : (KUR[pb] || 1);
  const toplam = (l, f) => l.reduce((s, k) => s + (Number(f(k)) || 0), 0);

  // ---- liste
  function liste() {
    const v = id => $(id).value;
    return V.cariler.filter(k => (!k.suzgec || S.uyuyor(k.suzgec)) && (!v('#okDurum') || k.durum === v('#okDurum')) && (!v('#okPb') || k.pbler.includes(v('#okPb'))));
  }
  function sirala(l) { const s = $('#okSirala').value; const f = { kap: k => -k.kapatilabilir, fat: k => -k.acikFaturaTl, ode: k => -k.acikOdemeTl, bakiye: k => -Math.abs(k.bakiyeTl) }[s]; return l.slice().sort((a, b) => f ? (f(a) - f(b)) || a.kod.localeCompare(b.kod) : a.kod.localeCompare(b.kod)); }
  const riskRozet = d => rozet(d || '—', d === 'Riskli' ? 'hata' : d === 'İzlemede' ? 'bekle' : 'ok');
  function kpi() {
    const l = V.cariler; const kutu = (k, l1, v, s, sec) => `<div style="--k:${k}" ${sec ? `data-sec="${sec}"` : ''}><div class="l">${l1}</div><div class="v">${v}</div><div class="s">${s}</div></div>`;
    const bugun = V.bugun.split('-').reverse().join('.'); const bugunK = V.kayitlar.filter(k => k.zaman.startsWith(bugun));
    $('#okKpi').innerHTML = [kutu('#0369a1', 'Açık Fatura', tl(toplam(l, k => k.acikFaturaTl)), `${toplam(l, k => k.acikFatura)} fatura · ${l.filter(k => k.acikFatura > 0).length} cari`), kutu('#f59e0b', 'Eşleşmemiş Ödeme', tl(toplam(l, k => k.acikOdemeTl)), `${toplam(l, k => k.acikOdeme)} belge · ${l.filter(k => k.acikOdeme > 0).length} cari`, 'Ödeme Bekliyor'), kutu('#15803d', 'Kapatılabilir', tl(toplam(l, k => k.kapatilabilir)), `${l.filter(k => k.durum === 'Kapatılabilir').length} cari · ${toplam(l, k => k.oneri)} öneri (${toplam(l, k => k.birebir)} birebir)`, 'Kapatılabilir'), kutu('#7c3aed', 'Bugün Kapatılan', tl(toplam(bugunK, k => k.toplamTl)), `${bugunK.length} kayıt · ${toplam(bugunK, k => k.adet)} eşleşme`), kutu('#64748b', 'Toplam Kapama Kaydı', V.kayitlar.length, `${toplam(V.kayitlar, k => k.adet)} eşleşme · ${tl(toplam(V.kayitlar, k => k.toplamTl))}`), kutu('#dc2626', 'Yalnız Açık Fatura', l.filter(k => k.durum === 'Yalnız Açık Fatura').length + ' cari', 'ödeme bekleyen faturalar (kapatılacak ödeme yok)', 'Yalnız Açık Fatura')].join('');
    $$('#okKpi [data-sec]').forEach(d => d.addEventListener('click', () => { const e = $('#okDurum'); e.value = e.value === d.dataset.sec ? '' : d.dataset.sec; sekme('liste'); ciz(); }));
  }
  function ciz() {
    const l = sirala(liste());
    $('#okTablo tbody').innerHTML = l.map(k => `<tr data-kod="${esc(k.kod)}" class="tik"><td class="s">${k.durum === 'Kapatılabilir' ? `<input type="checkbox" class="okSec" value="${esc(k.kod)}">` : ''}</td><td><b>${esc(k.kod)}</b> ${esc(k.kisaAd)}<br><small class="text-muted">${esc(k.pbler.join(' · '))}${k.vergiNo ? ' · VKN ' + esc(k.vergiNo) : ''}</small></td><td><span class="ck-tip">${esc(k.tip)}</span><br><small class="text-muted">${esc(k.suzgec ? k.suzgec.sinif : '')} · ${esc(k.bolge)}</small></td><td><small>${esc(k.sirket)}<br>${esc(k.suzgec ? k.suzgec.temsilci : '')}</small></td><td class="e">${tl(k.bakiyeTl)}</td><td class="e">${k.acikFatura ? `${tl(k.acikFaturaTl)}<br><small class="text-muted">${k.acikFatura} fatura</small>` : '—'}</td><td class="e ${k.acikOdeme ? 'gec' : ''}">${k.acikOdeme ? `${tl(k.acikOdemeTl)}<br><small>${k.acikOdeme} belge</small>` : '—'}</td><td class="e ${k.kapatilabilir ? 'ok' : ''}"><b>${k.kapatilabilir ? tl(k.kapatilabilir) : '—'}</b></td><td class="s">${k.oneri ? `${k.oneri}<br><small class="text-muted">${k.birebir} birebir</small>` : '—'}</td><td>${durumRozet(k.durum)}</td><td>${k.suzgec ? `${riskRozet(k.suzgec.risk)}<br><small class="text-muted">${esc(k.suzgec.limitDurum)}</small>` : '—'}</td><td><small>${esc(k.sonKapama || '—')}</small></td><td class="ci-git"><button type="button" data-ac="${esc(k.kod)}" title="Eşleştirme ekranı"><i class="fas fa-arrows-left-right"></i></button>${k.oneri ? `<button type="button" data-oto="${esc(k.kod)}" title="Bu caride otomatik kapat"><i class="fas fa-bolt"></i></button>` : ''}<a href="/CariKart/${k.tip === 'Tedarikçi' ? 'Tedarikci' : 'Musteri'}?kod=${encodeURIComponent(k.kod)}" title="Cari kartı"><i class="fas fa-address-card"></i></a>${k.tip === 'Tedarikçi' ? `<a href="/Rapor52?kod=${encodeURIComponent(k.kod)}" title="Tedarikçi Fatura ve Ödeme Takip"><i class="fas fa-truck"></i></a>` : `<a href="/Rapor117?kod=${encodeURIComponent(k.kod)}" title="Tahsilat"><i class="fas fa-hand-holding-dollar"></i></a>`}</td></tr>`).join('') || '<tr><td colspan="13" class="na-bos">Süzgece uyan cari yok.</td></tr>';
    $('#okTablo tfoot').innerHTML = l.length ? `<tr><td colspan="4">Toplam · ${l.length} cari</td><td class="e">${tl(toplam(l, k => k.bakiyeTl))}</td><td class="e">${tl(toplam(l, k => k.acikFaturaTl))}</td><td class="e">${tl(toplam(l, k => k.acikOdemeTl))}</td><td class="e">${tl(toplam(l, k => k.kapatilabilir))}</td><td class="s">${toplam(l, k => k.oneri)}</td><td colspan="4">${l.filter(k => k.durum === 'Kapatılabilir').length} kapatılabilir</td></tr>` : '';
    $('#okSayi').textContent = `${l.length} / ${V.cariler.length} cari`; $('#okN').textContent = l.length; $('#okNKayit').textContent = V.kayitlar.length;
    $$('#okTablo [data-ac]').forEach(b => b.addEventListener('click', e => { e.stopPropagation(); cariAc(b.dataset.ac); }));
    $$('#okTablo [data-oto]').forEach(b => b.addEventListener('click', e => { e.stopPropagation(); topluKapat([b.dataset.oto], `${b.dataset.oto} carisinde otomatik kapatma yapılsın mı?`); }));
    $$('#okTablo tr.tik').forEach(r => r.addEventListener('dblclick', () => cariAc(r.dataset.kod)));
    $$('.okSec').forEach(c => c.addEventListener('change', seciliSay)); seciliSay(); kpi(); kayitlar();
  }
  function seciliSay() { const n = $$('.okSec:checked').length; $('#okSeciliKapat').disabled = n === 0; $('#okSeciliKapat').innerHTML = `<i class="fas fa-check-double me-1"></i>Seçili Carilerde Otomatik Kapat${n ? ` (${n})` : ''}`; }
  async function topluKapat(kodlar, soru) {
    if (!confirm(soru)) return; const j = await post('/Kapama/TumunuKapat', { kodlar });
    mesaj(j.success && j.cari > 0 ? 'success' : 'warning', esc(j.message) + (j.hatalar && j.hatalar.length ? '<br><small>' + j.hatalar.map(esc).join(' · ') + '</small>' : '')); await yenile(D ? D.cari.kod : null);
  }
  // ---- cari eşleştirme
  async function cariAc(kod) {
    sekme('detay'); $('#okCari').value = kod; const j = await fetch('/Kapama/Detay?kod=' + encodeURIComponent(kod)).then(r => r.json()); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    D = j; CIFT = []; $('#okNDetay').textContent = kod; $('#okEkstre').href = '/Rapor20?bpCode=' + encodeURIComponent(kod); $('#okCariOto').disabled = !(j.oneri && j.oneri.length);
    const c = j.cari; $('#okCariOzet').innerHTML = `<b>${esc(c.kod)} ${esc(c.kisaAd)}</b> · ${esc(c.resmiUnvan)} · <span class="ck-tip">${esc(c.tip)}</span> ${esc(c.sirket)} · bakiye <b>${tl(c.bakiyeTl)}</b> · açık fatura <b>${c.acikFatura}</b> (${tl(c.acikFaturaTl)}) · eşleşmemiş ödeme <b>${c.acikOdeme}</b> (${tl(c.acikOdemeTl)}) · kapatılabilir <b class="${c.kapatilabilir ? 'ok' : ''}">${tl(c.kapatilabilir)}</b> ${durumRozet(c.durum)}`;
    $('#okFaturalar tbody').innerHTML = j.faturalar.map(f => `<tr><td class="s"><input type="checkbox" class="okFat" value="${esc(f.belgeNo)}" data-pb="${esc(f.pb)}" data-kalan="${f.kalan}"></td><td><b>${esc(f.belgeNo)}</b></td><td>${esc(f.tarih)}</td><td>${esc(f.vade)}</td><td class="s ${f.gecikme > 0 ? 'gec' : ''}">${f.gecikme > 0 ? f.gecikme + ' gün' : '—'}</td><td class="s">${esc(f.islemTipi)}</td><td class="e">${n2(f.tutar)} ${esc(f.pb)}</td><td class="e"><b>${n2(f.kalan)} ${esc(f.pb)}</b></td></tr>`).join('') || '<tr><td colspan="8" class="na-bos">Açık fatura yok.</td></tr>';
    $('#okOdemeler tbody').innerHTML = j.odemeler.map(o => `<tr><td class="s"><input type="checkbox" class="okOde" value="${esc(o.belgeNo)}" data-pb="${esc(o.pb)}" data-kalan="${o.kalan}"></td><td><b>${esc(o.belgeNo)}</b><br><small class="text-muted">${esc(o.aciklama)}</small></td><td>${esc(o.tarih)}</td><td>${esc(o.belgeTipi)}</td><td class="s">${esc(o.islemTipi)}</td><td class="e">${n2(o.tutar)} ${esc(o.pb)}</td><td class="e"><b>${n2(o.kalan)} ${esc(o.pb)}</b></td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Eşleştirilmemiş ödeme yok.</td></tr>';
    $('#okNFat').textContent = j.faturalar.length; $('#okNOde').textContent = j.odemeler.length;
    $$('.okFat, .okOde').forEach(c => c.addEventListener('change', secSay)); secSay(); ciftCiz();
    $('#okCariLog').innerHTML = j.log.length ? `<div class="na-mini" style="font-size:.6rem"><b>Son kapamalar</b><br>${j.log.map(l => `${esc(l.zaman)} · ${esc(l.kullanici)} · ${esc(l.yeni)}`).join('<br>')}</div>` : '';
  }
  function secSay() { $('#okFatSec').textContent = 'seçili: ' + $$('.okFat:checked').length; $('#okOdeSec').textContent = 'seçili: ' + $$('.okOde:checked').length; }
  function ciftCiz() {
    const l = CIFT; $('#okNCift').textContent = l.length;
    $('#okCiftler tbody').innerHTML = l.map((c, i) => `<tr><td><b>${esc(c.faturaNo)}</b></td><td class="s"><i class="fas fa-arrow-left text-muted"></i></td><td><b>${esc(c.odemeNo)}</b></td><td class="s">${esc(c.pb)}</td><td class="e"><input class="form-control form-control-sm text-end" data-i="${i}" value="${n2(c.tutar)}" style="width:120px;font-size:.66rem;height:22px;display:inline-block"></td><td class="s">${rozet(c.yontem, c.yontem === 'Birebir' ? 'ok' : c.yontem === 'FIFO' ? 'bilgi' : 'bekle')}</td><td class="ci-git"><button type="button" data-sil="${i}" title="Listeden çıkar"><i class="fas fa-xmark"></i></button></td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Önce "Otomatik Eşleştir" ya da fatura + ödeme seçip "Seçileni Eşle".</td></tr>';
    $('#okCiftler tfoot').innerHTML = l.length ? `<tr><td colspan="4">Toplam · ${l.length} eşleşme</td><td class="e">${tl(toplam(l, c => c.tutar * kurTl(c.pb)))}</td><td colspan="2">${l.filter(c => c.yontem === 'Birebir').length} birebir · ${l.filter(c => c.yontem === 'FIFO').length} FIFO · ${l.filter(c => c.yontem === 'Elle').length} elle</td></tr>` : '';
    $$('#okCiftler [data-sil]').forEach(b => b.addEventListener('click', () => { CIFT.splice(+b.dataset.sil, 1); ciftCiz(); }));
    $$('#okCiftler input[data-i]').forEach(i => i.addEventListener('change', () => { const v = parseFloat(i.value.replace(/\./g, '').replace(',', '.')); if (v > 0) CIFT[+i.dataset.i].tutar = v; ciftCiz(); }));
    $('#okKapat').disabled = l.length === 0; ozet();
  }
  function ozet() {
    if (!D) { $('#okOzet').innerHTML = ''; return; } const c = D.cari; const pb = {}; CIFT.forEach(x => pb[x.pb] = (pb[x.pb] || 0) + x.tutar);
    const satir = (l, v, cls) => `<div class="l">${l}</div><div class="v ${cls || ''}">${v}</div>`;
    $('#okOzet').innerHTML = satir('Açık fatura', `${c.acikFatura} · ${tl(c.acikFaturaTl)}`) + satir('Eşleşmemiş ödeme', `${c.acikOdeme} · ${tl(c.acikOdemeTl)}`) + satir('Kapatılabilir', tl(c.kapatilabilir), 'ok') + '<div class="ayrac"></div>' + satir('Listedeki eşleşme', CIFT.length) + Object.keys(pb).map(p => satir('Kapatılacak ' + p, n2(pb[p]) + ' ' + p)).join('') + satir('TL karşılığı', tl(toplam(CIFT, x => x.tutar * kurTl(x.pb))), 'ok') + '<div class="ayrac"></div>' + satir('Kalacak açık fatura', tl(Math.max(0, c.acikFaturaTl - toplam(CIFT, x => x.tutar * kurTl(x.pb))))) + satir('Kalacak eşleşmemiş ödeme', tl(Math.max(0, c.acikOdemeTl - toplam(CIFT, x => x.tutar * kurTl(x.pb)))));
  }
  async function oner() {
    if (!D) { mesaj('warning', 'Önce cari seçin.'); return; } const f = $$('.okFat:checked').map(x => x.value), o = $$('.okOde:checked').map(x => x.value);
    const j = await post('/Kapama/Oner', { kod: D.cari.kod, faturalar: f, odemeler: o }); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    CIFT = j.ciftler.map(c => ({ ...c })); ciftCiz(); mesaj(CIFT.length ? 'info' : 'warning', esc(j.message));
  }
  function elle() {
    const f = $$('.okFat:checked'), o = $$('.okOde:checked'); if (f.length !== 1 || o.length !== 1) { mesaj('warning', 'Elle eşleştirme için tam bir fatura ve bir ödeme seçin.'); return; }
    if (f[0].dataset.pb !== o[0].dataset.pb) { mesaj('warning', 'Fatura ile ödemenin para birimi farklı; eşleştirilemez.'); return; }
    const kullanF = toplam(CIFT.filter(c => c.faturaNo === f[0].value), c => c.tutar), kullanO = toplam(CIFT.filter(c => c.odemeNo === o[0].value), c => c.tutar);
    const t = Math.min(+f[0].dataset.kalan - kullanF, +o[0].dataset.kalan - kullanO); if (t <= 0.005) { mesaj('warning', 'Seçilen belgelerin kalanı listede zaten kullanılmış.'); return; }
    CIFT.push({ faturaNo: f[0].value, odemeNo: o[0].value, tutar: Math.round(t * 100) / 100, pb: f[0].dataset.pb, yontem: 'Elle' }); f[0].checked = o[0].checked = false; secSay(); ciftCiz();
  }
  async function kapat() {
    if (!D || !CIFT.length) return; const j = await post('/Kapama/Kapat', { kod: D.cari.kod, mod: CIFT.every(c => c.yontem !== 'Elle') ? 'Otomatik' : 'Elle', ciftler: CIFT.map(c => ({ FaturaNo: c.faturaNo, OdemeNo: c.odemeNo, Tutar: c.tutar, Pb: c.pb, Yontem: c.yontem })) });
    mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) await yenile(D.cari.kod);
  }
  async function yenile(kod) { const j = await fetch('/Kapama/Veri').then(r => r.json()); V = j; KUR = j.kurlar || {}; doldurSec(); ciz(); if (kod) await cariAc(kod); }
  function doldurSec() {
    if (S) S.kartlar(V.cariler.map(k => k.suzgec).filter(Boolean));
    const ara = ($('#okCariAra').value || '').toLocaleLowerCase('tr-TR'); const cs = $('#okCari'), cv = cs.value;
    cs.innerHTML = '<option value="">Seçiniz…</option>' + V.cariler.filter(k => !ara || (k.kod + ' ' + k.kisaAd).toLocaleLowerCase('tr-TR').includes(ara)).map(k => `<option value="${esc(k.kod)}">${esc(k.kod)} · ${esc(k.kisaAd)}${k.oneri ? ` · ${k.oneri} öneri` : ''}</option>`).join(''); cs.value = cv;
  }
  function kayitlar() {
    const ara = ($('#okKayitAra').value || '').toLocaleLowerCase('tr-TR'); const l = V.kayitlar.filter(k => !ara || [k.cariKodu, k.cariAd, k.ciftler.map(c => c.faturaNo + ' ' + c.odemeNo).join(' ')].join(' ').toLocaleLowerCase('tr-TR').includes(ara));
    $('#okKayitlar tbody').innerHTML = l.map(k => `<tr><td>${esc(k.zaman)}</td><td><b>${esc(k.cariKodu)}</b> ${esc(k.cariAd)}</td><td class="s">${rozet(k.mod, k.mod === 'Otomatik' ? 'ok' : 'bekle')}</td><td class="s">${k.adet}</td><td class="e"><b>${tl(k.toplamTl)}</b></td><td>${esc(k.ozet)}</td><td><small>${k.ciftler.map(c => `${esc(c.faturaNo)} ← ${esc(c.odemeNo)} (${esc(c.tutarTr)}, ${esc(c.yontem)})`).join('<br>')}</small></td><td>${esc(k.kullanici)}</td></tr>`).join('') || '<tr><td colspan="8" class="na-bos">Kayıt yok.</td></tr>';
    kayitlar.son = l;
  }
  function baslat() {
    S = CariSuzgec.kur($('#okSuzgec'), V.secenekler, { tipler: ['Müşteri', 'Tedarikçi'], tipIlk: 'Müşteri + Tedarikçi', onChange: ciz }); S.kartlar(V.cariler.map(k => k.suzgec).filter(Boolean)); S.urlOn(new URLSearchParams(location.search));
    ['#okDurum', '#okPb', '#okSirala'].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    $('#okKayitAra').addEventListener('input', kayitlar);
    $('#okTemizle').addEventListener('click', () => { ['#okDurum', '#okPb'].forEach(s => $(s).value = ''); S.temizle(); });
    $$('#okSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    $('#okHepsi').addEventListener('change', e => { $$('.okSec').forEach(c => c.checked = e.target.checked); seciliSay(); });
    $('#okSeciliKapat').addEventListener('click', () => { const k = $$('.okSec:checked').map(c => c.value); topluKapat(k, `${k.length} caride otomatik kapatma yapılsın mı? (birebir + FIFO)`); });
    $('#okTumKapat').addEventListener('click', () => topluKapat([], `TÜM carilerde (${V.cariler.filter(k => k.durum === 'Kapatılabilir').length} kapatılabilir cari) otomatik kapatma yapılsın mı? (birebir + FIFO)`));
    $('#okCariAra').addEventListener('input', doldurSec); $('#okCari').addEventListener('change', () => { if ($('#okCari').value) cariAc($('#okCari').value); });
    $('#okFatHepsi').addEventListener('change', e => { $$('.okFat').forEach(c => c.checked = e.target.checked); secSay(); }); $('#okOdeHepsi').addEventListener('change', e => { $$('.okOde').forEach(c => c.checked = e.target.checked); secSay(); });
    $('#okOner').addEventListener('click', oner); $('#okElle').addEventListener('click', elle); $('#okCiftTemizle').addEventListener('click', () => { CIFT = []; ciftCiz(); }); $('#okKapat').addEventListener('click', kapat);
    $('#okCariOto').addEventListener('click', () => D && topluKapat([D.cari.kod], `${D.cari.kod} ${D.cari.kisaAd} carisinde otomatik kapatma yapılsın mı?`));
    $('#okKayitExcel').addEventListener('click', () => csvIndir('kapama_kayitlari', ['Zaman', 'Cari Kodu', 'Cari', 'Mod', 'Eşleşme', 'Toplam (₺)', 'Özet', 'Eşleşmeler', 'Kim'], (kayitlar.son || []).map(k => [k.zaman, k.cariKodu, k.cariAd, k.mod, k.adet, k.toplamTl, k.ozet, k.ciftler.map(c => `${c.faturaNo} <- ${c.odemeNo} ${c.tutarTr}`).join(' | '), k.kullanici])));
    doldurSec(); ciz(); const on = kok.dataset.kod; if (on) cariAc(on);
  }
  fetch('/Kapama/Veri').then(r => r.json()).then(j => { V = j; KUR = j.kurlar || {}; baslat(); }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
