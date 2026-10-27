// ============================================================
// TAHSİLAT (Views/Rapor117/Index.cshtml) — bakiyeden (FIFO) / fatura bazlı tahsilat, otomatik vade farkı ve kur farkı, fark faturası kesme
//   Uçlar: /Tahsilat/Veri · /Tahsilat/Faturalar?kod= · /Tahsilat/Onizle · /Tahsilat/Kaydet · /Tahsilat/FaturaKes (Controllers/TahsilatController.cs; kurallar Data/TahsilatOrnek.cs)
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('th'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const n2 = v => Number(v || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const tl = v => n2(v) + ' ₺';
  const sayi = v => parseFloat(String(v ?? '').replace(/\./g, '').replace(',', '.')) || 0;
  const bugunIso = new Date().toISOString().slice(0, 10);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const riskRozet = d => rozet(d, d === 'Riskli' ? 'hata' : d === 'İzlemede' ? 'bekle' : 'ok');
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + bugunIso + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 12000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  async function post(url, veri) { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }); return r.json(); }
  let V = null, FAT = [], MOD = 'Bakiye', ON = null, SON = null, onZ = null;
  const cari = () => V.cariler.find(c => c.kod === $('#thCari').value);
  function sekme(p) { $$('#thSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('.na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); }

  // ---- cari listesi ve özet
  function cariDoldur() {
    const q = ($('#thCariAra').value || '').toLocaleLowerCase('tr-TR'), sel = $('#thCari'), eski = sel.value;
    const l = V.cariler.filter(c => !q || (c.kod + ' ' + c.kisaAd + ' ' + c.resmiUnvan).toLocaleLowerCase('tr-TR').includes(q));
    sel.innerHTML = '<option value="">Seçiniz…</option>' + l.map(c => `<option value="${esc(c.kod)}">${esc(c.kod)} · ${esc(c.kisaAd)}${c.acikAdet ? ` (${c.acikAdet} açık fatura)` : ''}</option>`).join('');
    sel.value = l.some(c => c.kod === eski) ? eski : (q && l.length === 1 ? l[0].kod : '');
  }
  function cariOzet() {
    const c = cari(); const o = $('#thCariOzet');
    if (!c) { o.innerHTML = '<span class="text-muted">Cari seçince bakiye, X / R, vadesi geçmiş ve açık faturalar burada görünür.</span>'; return; }
    o.innerHTML = [`<span class="k"><small>Bakiye (TL)</small><b>${esc(c.bakiyeTr)}</b></span>`, `<span class="k"><small>X / R</small><b>${esc(c.bakiyeXTr)} / ${esc(c.bakiyeRTr)}</b></span>`, `<span class="k ${c.vadesiGecmisAdet ? 'gec' : ''}"><small>Vadesi Geçmiş</small><b>${esc(c.vadesiGecmisTr)}${c.vadesiGecmisAdet ? ` · ${c.vadesiGecmisAdet} kalem · ${c.maxGecikme} gün` : ''}</b></span>`, `<span class="k"><small>Açık Faturalar</small><b>${c.acikPb.map(p => `${p.adet} × ${esc(p.kalanTr)}`).join(' · ') || '—'}</b></span>`, `<span class="k"><small>Vade</small><b>${esc(c.vadeSarti || '—')}</b></span>`, `<span class="k"><small>Temsilci</small><b>${esc(c.temsilci || '—')}</b></span>`, `<span class="k"><small>Risk</small><b>${riskRozet(c.risk)}</b></span>`, `<a class="btn btn-light btn-sm py-0" style="font-size:.62rem" href="/CariKart/Musteri?kod=${encodeURIComponent(c.kod)}"><i class="fas fa-address-card me-1"></i>Kart</a><a class="btn btn-light btn-sm py-0" style="font-size:.62rem" target="_blank" rel="noopener" href="/Rapor20?bpCode=${encodeURIComponent(c.kod)}"><i class="fas fa-file-invoice me-1"></i>Ekstre</a>`].join('');
    const pbler = c.pbler.length ? c.pbler : ['TL']; $('#thPb').innerHTML = pbler.map(p => `<option>${p}</option>`).join(''); kurGuncelle();
  }
  function kurGuncelle() { const pb = $('#thPb').value; $('#thKur').value = Number(V.kurlar[pb] ?? 1).toLocaleString('tr-TR', { maximumFractionDigits: 4 }); }
  async function faturaYukle() {
    const c = cari(); FAT = []; if (!c) { cizFaturalar(); return; }
    const j = await (await fetch('/Tahsilat/Faturalar?kod=' + encodeURIComponent(c.kod))).json(); FAT = j.faturalar || []; cizFaturalar(); onizle();
  }
  // ---- fatura tablosu (mod: Bakiye → dağıtım önizlemeden; Fatura → satır başına tutar girişi)
  function cizFaturalar() {
    const pb = $('#thPb').value, l = FAT.filter(f => f.pb === pb), tb = $('#thFaturalar tbody');
    const d = {}; (ON?.dagitim || []).forEach(x => d[x.belgeNo] = x);
    tb.innerHTML = l.map(f => { const x = d[f.belgeNo]; return `<tr data-no="${esc(f.belgeNo)}" class="${x ? 'secili' : ''}"><td><b>${esc(f.belgeNo)}</b><br><small class="text-muted">${esc(f.aciklama || '')}</small></td><td>${esc(f.tarih)}</td><td>${esc(f.vade)}</td><td class="s ${f.gecikme > 0 ? 'gec' : ''}">${f.gecikme > 0 ? f.gecikme + ' gün' : '—'}</td><td class="s">${esc(f.islemTipi)}</td><td class="e">${esc(f.kalanTr)} ${esc(f.pb)}</td><td class="e">${f.pb === 'TL' ? '—' : Number(f.faturaKuru).toLocaleString('tr-TR', { minimumFractionDigits: 4 })}</td><td class="e">${MOD === 'Fatura' ? `<input class="form-control form-control-sm dagit" inputmode="decimal" data-no="${esc(f.belgeNo)}" value="${x ? n2(x.tutar) : ''}" placeholder="0,00">` : (x ? `<b>${esc(x.tutarTr)}</b>` : '<span class="text-muted">—</span>')}</td><td class="e">${x && x.kurFarki ? (x.kurFarki > 0 ? '<span class="gec">' + esc(x.kurFarkiTr) + '</span>' : esc(x.kurFarkiTr)) : '—'}</td><td class="e">${x && x.vadeFarki > 0 ? '<span class="gec">' + esc(x.vadeFarkiTr) + '</span>' : '—'}</td></tr>`; }).join('') || `<tr><td colspan="10" class="na-bos">${cari() ? `Bu caride ${esc(pb)} açık fatura yok.` : 'Önce cari seçin.'}</td></tr>`;
    $('#thFaturalar tfoot').innerHTML = l.length ? `<tr><td colspan="5">${l.length} açık fatura · ${l.filter(f => f.gecikme > 0).length} gecikmiş</td><td class="e">${n2(l.reduce((s, f) => s + f.kalan, 0))} ${esc(pb)}</td><td></td><td class="e">${ON ? n2(ON.dagitilan) + ' ' + esc(pb) : '—'}</td><td class="e">${ON ? esc(tl(ON.kurFarki)) : '—'}</td><td class="e">${ON ? esc(tl(ON.vadeFarki)) : '—'}</td></tr>` : '';
    $$('#thFaturalar input.dagit').forEach(i => { i.addEventListener('input', () => onizleGecikmeli()); i.addEventListener('change', onizle); });
    $('#thHepsi').style.display = MOD === 'Fatura' ? '' : 'none';
  }
  const secim = () => { const s = {}; $$('#thFaturalar input.dagit').forEach(i => { const v = sayi(i.value); if (v > 0) s[i.dataset.no] = v; }); return s; };
  function onizleGecikmeli() { clearTimeout(onZ); onZ = setTimeout(onizle, 350); }
  async function onizle() {
    const c = cari(); if (!c) { ON = null; ozet(); return; }
    const g = { kod: c.kod, tarih: $('#thTarih').value, pb: $('#thPb').value, tutar: sayi($('#thTutar').value), kur: sayi($('#thKur').value) || 1, mod: MOD, secim: MOD === 'Fatura' ? secim() : null };
    if (MOD === 'Fatura') { const t = Object.values(g.secim).reduce((s, v) => s + v, 0); if (!g.tutar || g.tutar < t) { $('#thTutar').value = n2(t); g.tutar = t; } }
    const j = await post('/Tahsilat/Onizle', g); ON = j.success ? j : null; cizFaturalar(); ozet();
  }
  function ozet() {
    const c = cari(), pb = $('#thPb').value, kur = sayi($('#thKur').value) || 1, tutar = sayi($('#thTutar').value), o = $('#thOzet'), kdv = Number(V.kdvOran);
    if (!c) { o.innerHTML = '<div class="l">Cari</div><div class="v text-muted">seçilmedi</div>'; return; }
    const r = [['Cari', `${esc(c.kod)} · ${esc(c.kisaAd)}`], ['Tahsilat Tutarı', `<span class="buyuk">${n2(tutar)} ${esc(pb)}</span>`], ['TL Karşılığı', tl(tutar * kur) + (pb !== 'TL' ? ` <small class="text-muted">(kur ${kur.toLocaleString('tr-TR', { maximumFractionDigits: 4 })})</small>` : '')], ['Mod', MOD === 'Fatura' ? 'Fatura Bazlı' : 'Bakiyeden (FIFO)'], ['ayrac'],
      ['Kapanan Fatura', ON ? `${ON.dagitim.length} adet · ${n2(ON.dagitilan)} ${esc(pb)}` : '—'], ['Bakiyeye Avans', ON ? `<span class="${ON.avans > 0.005 ? 'yesil' : ''}">${n2(ON.avans)} ${esc(pb)}</span>` : '—'], ['ayrac'],
      ['Vade Farkı', ON ? `<span class="${ON.vadeFarki > 0 ? 'kirmizi' : ''}">${tl(ON.vadeFarki)}</span>` : '—'], ['Vade Farkı (KDV dahil)', ON && ON.vadeFarki > 0 ? tl(ON.vadeFarki * (1 + kdv / 100)) : '—'],
      ['Kur Farkı', ON ? `<span class="${ON.kurFarki > 0 ? 'kirmizi' : ''}">${tl(ON.kurFarki)}</span>` : (pb === 'TL' ? '<span class="text-muted">TL faturada yok</span>' : '—')], ['Kur Farkı (KDV dahil)', ON && ON.kurFarki > 0 ? tl(ON.kurFarki * (1 + kdv / 100)) : '—']];
    o.innerHTML = r.map(x => x[0] === 'ayrac' ? '<div class="ayrac"></div>' : `<div class="l">${x[0]}</div><div class="v">${x[1]}</div>`).join('');
    $('#thOzetNot').textContent = ON ? 'önizleme · kaydedince cari hesabına işlenir' : 'tutar girince hesaplanır';
    $('#thKff').disabled = !(SON && SON.kurFarki > 0 && !SON.kurFarkiFaturaNo); $('#thVff').disabled = !(SON && SON.vadeFarki > 0 && !SON.vadeFarkiFaturaNo);
  }
  function rehber() { $('#thRehber').innerHTML = [['Bakiyeden', 'tutar, en eski vadeli açık faturadan başlayarak sırayla kapatır; artan kısım bakiyeye avans olarak kalır.'], ['Fatura Bazlı', 'kapatılacak faturalara tutar yazılır; toplam, tahsilat tutarını aşamaz.'], ['Vade Farkı', `kalan × kur × %${Number(V.vadeFarkiAylikOran).toLocaleString('tr-TR')} / 30 × gecikme günü (tahsilat tarihi − vade).`], ['Kur Farkı', 'dövizli faturada tutar × (tahsilat kuru − fatura tarihindeki kur); pozitifse fatura kesilir.'], ['Fark Faturası', `KDV %${Number(V.kdvOran).toLocaleString('tr-TR')}, 30 gün vadeli; cari hesabına X / R tipiyle Fatura olarak işlenir.`]].map(([b, a]) => `<div><b>${b}:</b> ${esc(a)}</div>`).join(''); }
  async function kaydet() {
    const c = cari(); if (!c) { mesaj('danger', 'Cari seçin.'); return; } const tutar = sayi($('#thTutar').value); if (!(tutar > 0)) { mesaj('danger', 'Tutar girin.'); $('#thTutar').focus(); return; }
    const b = $('#thKaydet'); b.disabled = true;
    try {
      const j = await post('/Tahsilat/Kaydet', { kod: c.kod, tarih: $('#thTarih').value, yontem: $('#thYontem').value, islemTipi: $('#thTip').value, pb: $('#thPb').value, tutar, kur: sayi($('#thKur').value) || 1, mod: MOD, secim: MOD === 'Fatura' ? secim() : null, aciklama: $('#thAciklama').value });
      if (!j.success) { mesaj('danger', `<b>Kaydedilemedi:</b> ${esc(j.message)}`); return; }
      SON = j.tahsilat; V.tahsilatlar.unshift(j.tahsilat); mesaj('success', `<i class="fas fa-circle-check me-1"></i>${esc(j.message)}${(j.tahsilat.kurFarki > 0 || j.tahsilat.vadeFarki > 0) ? ' Sağdaki düğmelerle fark faturası kesebilirsiniz.' : ''}`);
      await veriYenile(); cizListe(); ozet();
    } finally { b.disabled = false; }
  }
  async function faturaKes(tur) {
    if (!SON) return; const j = await post('/Tahsilat/FaturaKes', { tahsilatNo: SON.no, tur });
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    SON = j.tahsilat; const i = V.tahsilatlar.findIndex(t => t.no === SON.no); if (i >= 0) V.tahsilatlar[i] = SON; V.faturalar.unshift(j.fatura);
    mesaj('success', `<i class="fas fa-file-invoice me-1"></i>${esc(j.message)}`); cizListe(); cizFaturaListe(); faturaOnizle(j.fatura.no); ozet(); await veriYenile();
  }
  async function veriYenile() { const j = await (await fetch('/Tahsilat/Veri')).json(); V.cariler = j.cariler; cariDoldur(); cariOzet(); await faturaYukle(); }
  function yeni() { SON = null; ON = null; $('#thTutar').value = ''; $('#thAciklama').value = ''; $('#thTarih').value = bugunIso; MOD = 'Bakiye'; $$('#thMod button').forEach(b => b.classList.toggle('aktif', b.dataset.mod === MOD)); cizFaturalar(); ozet(); }

  // ---- listeler
  function cizListe() {
    const q = ($('#thListeAra').value || '').toLocaleLowerCase('tr-TR'); const l = V.tahsilatlar.filter(t => !q || [t.no, t.cariKodu, t.cariAd, t.yontem, t.aciklama].join(' ').toLocaleLowerCase('tr-TR').includes(q));
    $('#thListe tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${esc(t.no)}"><td><b>${esc(t.no)}</b></td><td>${esc(t.tarih)}</td><td><b>${esc(t.cariKodu)}</b> ${esc(t.cariAd)}</td><td>${esc(t.yontem)}</td><td class="s">${esc(t.islemTipi)}</td><td class="e">${esc(t.tutarTr)}</td><td class="e">${esc(t.tutarTlTr)}</td><td>${t.mod === 'Fatura' ? 'Fatura Bazlı' : 'Bakiyeden'}</td><td class="s">${t.dagitim.length}</td><td class="e">${t.avans > 0.005 ? esc(t.avansTr) : '—'}</td><td class="e ${t.kurFarki > 0 ? 'gec' : ''}">${t.kurFarki ? esc(t.kurFarkiTr) : '—'}</td><td class="e ${t.vadeFarki > 0 ? 'gec' : ''}">${t.vadeFarki > 0 ? esc(t.vadeFarkiTr) : '—'}</td><td>${[t.kurFarkiFaturaNo, t.vadeFarkiFaturaNo].filter(Boolean).map(esc).join(', ') || (t.kurFarki > 0 || t.vadeFarki > 0 ? '<span class="text-warning">kesilmedi</span>' : '—')}</td><td><small>${esc(t.kullanici)}<br>${esc(t.zaman)}</small></td></tr>`).join('') || '<tr><td colspan="14" class="na-bos">Kayıt yok.</td></tr>';
    $('#thNListe').textContent = V.tahsilatlar.length; $('#thNFatura').textContent = V.faturalar.length;
    $$('#thListe tr.tik').forEach(r => r.addEventListener('click', () => detay(r.dataset.no)));
  }
  function detay(no) {
    const t = V.tahsilatlar.find(x => x.no === no); if (!t) return;
    $('#thDetay').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-receipt me-1"></i>${esc(t.no)} · ${esc(t.cariKodu)} ${esc(t.cariAd)} · ${esc(t.tutarTr)} (${esc(t.yontem)}, ${esc(t.islemTipi)})</span><span class="d-flex gap-1">${t.kurFarki > 0 && !t.kurFarkiFaturaNo ? `<button type="button" class="btn btn-outline-primary btn-sm py-0" data-kes="Kur Farkı" style="font-size:.64rem"><i class="fas fa-file-invoice me-1"></i>Kur Farkı Faturası Kes</button>` : ''}${t.vadeFarki > 0 && !t.vadeFarkiFaturaNo ? `<button type="button" class="btn btn-outline-primary btn-sm py-0" data-kes="Vade Farkı" style="font-size:.64rem"><i class="fas fa-file-invoice me-1"></i>Vade Farkı Faturası Kes</button>` : ''}${[t.kurFarkiFaturaNo, t.vadeFarkiFaturaNo].filter(Boolean).map(f => `<button type="button" class="btn btn-light btn-sm py-0" data-fat="${esc(f)}" style="font-size:.64rem"><i class="fas fa-print me-1"></i>${esc(f)}</button>`).join('')}<a class="btn btn-light btn-sm py-0" style="font-size:.64rem" target="_blank" rel="noopener" href="/Rapor123?sekme=${esc(t.panelSekme)}&sirket=${encodeURIComponent(t.sirketDb)}&bas=${esc(t.tarih.split('.').reverse().join('-'))}&bit=${esc(t.tarih.split('.').reverse().join('-'))}" title="Konsolide Finansal Takip Paneli'nde bu tahsilatın satırı"><i class="fas fa-clipboard-check me-1"></i>Takip Paneli</a><a class="btn btn-light btn-sm py-0" style="font-size:.64rem" target="_blank" rel="noopener" href="/Rapor20?bpCode=${encodeURIComponent(t.cariKodu)}"><i class="fas fa-file-invoice me-1"></i>Ekstre</a></span></div>
      <div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo th-liste" data-dinamik="kapali"><thead><tr><th>Belge No</th><th>Fatura Tarihi</th><th>Vade</th><th class="s">Gecikme</th><th class="s">Tip</th><th class="e">Kalan (Önce)</th><th class="e">Kapatılan</th><th class="e">TL</th><th class="e">Fatura Kuru</th><th class="e">Tahsilat Kuru</th><th class="e">Kur Farkı</th><th class="e">Vade Farkı</th></tr></thead><tbody>${t.dagitim.map(d => `<tr><td><b>${esc(d.belgeNo)}</b></td><td>${esc(d.faturaTarihi)}</td><td>${esc(d.vade)}</td><td class="s ${d.gecikmeGun > 0 ? 'gec' : ''}">${d.gecikmeGun > 0 ? d.gecikmeGun + ' gün' : '—'}</td><td class="s">${esc(d.islemTipi)}</td><td class="e">${esc(d.kalanTr)} ${esc(d.pb)}</td><td class="e"><b>${esc(d.tutarTr)} ${esc(d.pb)}</b></td><td class="e">${esc(d.tutarTlTr)}</td><td class="e">${d.pb === 'TL' ? '—' : Number(d.faturaKuru).toLocaleString('tr-TR', { minimumFractionDigits: 4 })}</td><td class="e">${d.pb === 'TL' ? '—' : Number(d.tahsilatKuru).toLocaleString('tr-TR', { minimumFractionDigits: 4 })}</td><td class="e ${d.kurFarki > 0 ? 'gec' : ''}">${d.kurFarki ? esc(d.kurFarkiTr) : '—'}</td><td class="e ${d.vadeFarki > 0 ? 'gec' : ''}">${d.vadeFarki > 0 ? esc(d.vadeFarkiTr) : '—'}</td></tr>`).join('') || '<tr><td colspan="12" class="na-bos">Fatura kapatılmadı; tutar bakiyeye avans olarak işlendi.</td></tr>'}</tbody><tfoot><tr><td colspan="6">Toplam · avans ${esc(t.avansTr)}${t.aciklama ? ' · ' + esc(t.aciklama) : ''}</td><td class="e">${n2(t.dagitilan)} ${esc(t.paraBirimi)}</td><td class="e">${esc(t.tutarTlTr)}</td><td colspan="2"></td><td class="e">${esc(t.kurFarkiTr)}</td><td class="e">${esc(t.vadeFarkiTr)}</td></tr></tfoot></table></div></div></div>`;
    $$('#thDetay [data-kes]').forEach(b => b.addEventListener('click', async () => { const j = await post('/Tahsilat/FaturaKes', { tahsilatNo: no, tur: b.dataset.kes }); if (!j.success) { mesaj('danger', esc(j.message)); return; } const i = V.tahsilatlar.findIndex(x => x.no === no); if (i >= 0) V.tahsilatlar[i] = j.tahsilat; V.faturalar.unshift(j.fatura); mesaj('success', esc(j.message)); cizListe(); detay(no); cizFaturaListe(); }));
    $$('#thDetay [data-fat]').forEach(b => b.addEventListener('click', () => { sekme('fatura'); faturaOnizle(b.dataset.fat); }));
  }
  function cizFaturaListe() {
    $('#thFaturaListe tbody').innerHTML = V.faturalar.map(f => `<tr class="tik" data-no="${esc(f.no)}"><td><b>${esc(f.no)}</b></td><td>${esc(f.tarih)}</td><td>${esc(f.vade)}</td><td><b>${esc(f.cariKodu)}</b> ${esc(f.cariAd)}</td><td>${rozet(f.tur, f.tur === 'Kur Farkı' ? 'bekle' : 'hata')}</td><td class="e">${esc(f.matrahTr)}</td><td class="e">${esc(f.kdvTr)}</td><td class="e"><b>${esc(f.toplamTr)}</b></td><td>${esc(f.tahsilatNo)}</td><td><small>${esc(f.kullanici)}</small></td></tr>`).join('') || '<tr><td colspan="10" class="na-bos">Henüz fark faturası kesilmedi.</td></tr>';
    $('#thNFatura').textContent = V.faturalar.length;
    $$('#thFaturaListe tr.tik').forEach(r => r.addEventListener('click', () => faturaOnizle(r.dataset.no)));
  }
  function faturaOnizle(no) {
    const f = V.faturalar.find(x => x.no === no); if (!f) return;
    $$('#thFaturaListe tr.tik').forEach(r => r.classList.toggle('secili', r.dataset.no === no));
    $('#thFaturaOnizle').innerHTML = `<div class="th-fatura"><div class="ust"><div><h5>${esc(f.tur.toLocaleUpperCase('tr-TR'))} FATURASI</h5><div class="no">${esc(f.no)} · İşlem tipi ${esc(f.islemTipi)} · Tahsilat ${esc(f.tahsilatNo)}</div></div><div class="text-end"><div><b>Tarih:</b> ${esc(f.tarih)}</div><div><b>Vade:</b> ${esc(f.vade)}</div><div><b>Düzenleyen:</b> ${esc(f.kullanici)}</div></div></div>
      <div><b>Sayın</b> ${esc(f.cariUnvan)}<br><small class="text-muted">${esc(f.cariKodu)} · VKN ${esc(f.vergiNo || '—')} · ${esc(f.vergiDairesi || '—')}</small></div>
      <table><thead><tr><th>#</th><th>Açıklama</th><th class="e">Tutar (₺)</th></tr></thead><tbody>${f.kalemler.map((k, i) => `<tr><td>${i + 1}</td><td>${esc(k.aciklama)}</td><td class="e">${esc(k.tutarTr)}</td></tr>`).join('')}</tbody><tfoot><tr><td colspan="2" class="e">Matrah</td><td class="e">${esc(f.matrahTr)}</td></tr><tr><td colspan="2" class="e">KDV %${Number(f.kdvOran).toLocaleString('tr-TR')}</td><td class="e">${esc(f.kdvTr)}</td></tr><tr class="toplam"><td colspan="2" class="e">GENEL TOPLAM</td><td class="e">${esc(f.toplamTr)}</td></tr></tfoot></table>
      <div class="text-muted mt-2" style="font-size:.6rem">Bu belge ön yüz örneğidir; cari hesabına ${esc(f.tarih)} tarihinde ${esc(f.toplamTr)} tutarında Fatura hareketi (${esc(f.islemTipi)}) olarak işlenmiştir. ${esc(f.zaman)}</div></div>`;
  }

  // ---- başlat
  function baslat() {
    $('#thKurlar').innerHTML = Object.keys(V.kurlar).filter(p => p !== 'TL').map(p => `<span>${p} ${Number(V.kurlar[p]).toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>`).join('') + `<span class="text-muted" style="background:none;border:none">kur ${esc(V.kurTarihi)} · vade farkı %${Number(V.vadeFarkiAylikOran).toLocaleString('tr-TR')} / ay · KDV %${Number(V.kdvOran)}</span>`;
    $('#thYontem').innerHTML = V.yontemler.map(y => `<option>${esc(y)}</option>`).join(''); $('#thTarih').value = bugunIso;
    cariDoldur(); rehber(); ozet(); cizListe(); cizFaturaListe();
    $('#thCariAra').addEventListener('input', () => { cariDoldur(); cariOzet(); faturaYukle(); });
    $('#thCari').addEventListener('change', () => { ON = null; SON = null; cariOzet(); faturaYukle(); ozet(); });
    $('#thPb').addEventListener('change', () => { kurGuncelle(); ON = null; $('#thTutar').value = ''; cizFaturalar(); onizle(); });
    ['#thTarih', '#thKur', '#thTutar'].forEach(s => { $(s).addEventListener('input', onizleGecikmeli); $(s).addEventListener('change', onizle); });
    $$('#thMod button').forEach(b => b.addEventListener('click', () => { MOD = b.dataset.mod; $$('#thMod button').forEach(x => x.classList.toggle('aktif', x === b)); ON = null; cizFaturalar(); onizle(); }));
    $('#thHepsi').addEventListener('click', () => { $$('#thFaturalar input.dagit').forEach(i => { const f = FAT.find(x => x.belgeNo === i.dataset.no); if (f) i.value = n2(f.kalan); }); onizle(); });
    $('#thKaydet').addEventListener('click', kaydet); $('#thKff').addEventListener('click', () => faturaKes('Kur Farkı')); $('#thVff').addEventListener('click', () => faturaKes('Vade Farkı')); $('#thYeni').addEventListener('click', yeni);
    $('#thListeAra').addEventListener('input', cizListe);
    $('#thListeExcel').addEventListener('click', () => csvIndir('tahsilatlar', ['Tahsilat No', 'Tarih', 'Cari Kodu', 'Cari', 'Yöntem', 'İşlem Tipi', 'Para Birimi', 'Tutar', 'Kur', 'TL Karşılığı', 'Mod', 'Kapanan Fatura', 'Avans', 'Kur Farkı (₺)', 'Vade Farkı (₺)', 'Kur Farkı Faturası', 'Vade Farkı Faturası', 'Açıklama', 'Kim', 'Zaman'], V.tahsilatlar.map(t => [t.no, t.tarih, t.cariKodu, t.cariAd, t.yontem, t.islemTipi, t.paraBirimi, t.tutar, t.kur, t.tutarTl, t.mod, t.dagitim.map(d => d.belgeNo).join(' | '), t.avans, t.kurFarki, t.vadeFarki, t.kurFarkiFaturaNo, t.vadeFarkiFaturaNo, t.aciklama, t.kullanici, t.zaman])));
    $('#thYazdir').addEventListener('click', () => { if ($('#thFaturaOnizle .th-fatura')) setTimeout(() => window.print(), 50); });
    $$('#thSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    const kod = kok.dataset.kod || new URLSearchParams(location.search).get('kod'); if (kod && V.cariler.some(c => c.kod === kod)) { $('#thCari').value = kod; cariOzet(); faturaYukle(); }
  }
  fetch('/Tahsilat/Veri').then(r => r.json()).then(j => { V = j; baslat(); }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
