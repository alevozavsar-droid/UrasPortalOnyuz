// ============================================================
// CARİ EKSTRE / HESAP EKSTRESİ MUTABAKATI (Views/Rapor141/Index.cshtml) — cari ekstre ↔ muhasebe hesap ekstresi: cari listesi, satır satır karşılaştırma, düzeltme fişi
//   Uçlar: /EkstreKarsilastirma/Veri, Detay, DuzeltmeFisi (Controllers/EkstreKarsilastirmaController.cs)
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('ek'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const tl = v => Number(v || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 }) + ' ₺';
  const n2 = v => Number(v || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const durumRozet = d => rozet(d, d === 'Uyuşuyor' || d === 'Eşleşti' ? 'ok' : d === 'Hesap Tanımsız' ? 'notr' : d === 'Tutar Farkı' ? 'bekle' : 'hata');
  const post = (u, b) => fetch(u, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(b || {}) }).then(r => r.json());
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; }
  function sekme(p) { $$('#ekSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('.na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); }
  let V = null, D = null;
  const toplam = (l, f) => l.reduce((s, k) => s + (Number(f(k)) || 0), 0);
  const aralik = () => `bas=${$('#ekBas').value}&bit=${$('#ekBit').value}`;

  function liste() {
    const ara = ($('#ekAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value, ft = v('#ekFarkTur');
    return V.cariler.filter(k => (!v('#ekTip') || k.tip === v('#ekTip')) && (!v('#ekSirket') || k.sirket === v('#ekSirket')) && (!v('#ekDurum') || k.durum === v('#ekDurum')) && (!ft || k.tipler.some(t => t.durum === ft)) && (!ara || [k.kod, k.kisaAd, k.resmiUnvan, k.hesapKodu].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  function kpi() {
    const l = V.cariler; const kutu = (k, l1, v, s, sec) => `<div style="--k:${k}" ${sec ? `data-sec="${sec}"` : ''}><div class="l">${l1}</div><div class="v">${v}</div><div class="s">${s}</div></div>`; const fark = l.filter(k => k.durum === 'Fark Var');
    $('#ekKpi').innerHTML = [kutu('#0369a1', 'Karşılaştırılan Cari', l.length, `${V.bas.split('-').reverse().join('.')} – ${V.bit.split('-').reverse().join('.')}`), kutu('#15803d', 'Uyuşan', l.filter(k => k.durum === 'Uyuşuyor').length, 'cari ekstre = hesap ekstresi', 'Uyuşuyor'), kutu('#dc2626', 'Fark Var', fark.length, `${toplam(fark, k => k.farkAdet)} farklı satır · net ${tl(toplam(fark, k => k.fark))}`, 'Fark Var'), kutu('#f59e0b', 'Mutlak Fark', tl(toplam(fark, k => Math.abs(k.fark))), 'cari bakiye − hesap bakiye'), kutu('#64748b', 'Hesap Tanımsız', l.filter(k => k.durum === 'Hesap Tanımsız').length, 'kebir hesabı bağlı değil', 'Hesap Tanımsız'), kutu('#7c3aed', 'Düzeltme Fişi', V.duzeltmeler.length, `${tl(toplam(V.duzeltmeler, d => d.tutar))} · bu oturum`)].join('');
    $$('#ekKpi [data-sec]').forEach(x => x.addEventListener('click', () => { const e = $('#ekDurum'); e.value = e.value === x.dataset.sec ? '' : x.dataset.sec; sekme('liste'); ciz(); }));
  }
  function ciz() {
    const l = liste().slice().sort((a, b) => (Math.abs(b.fark) - Math.abs(a.fark)) || a.kod.localeCompare(b.kod));
    $('#ekTablo tbody').innerHTML = l.map(k => `<tr class="tik" data-kod="${esc(k.kod)}"><td><b>${esc(k.kod)}</b> ${esc(k.kisaAd)}<br><small class="text-muted"><span class="ck-tip">${esc(k.tip)}</span></small></td><td><small>${esc(k.sirket)}</small></td><td>${k.hesapKodu ? `<b>${esc(k.hesapKodu)}</b>` : '<span class="text-muted">—</span>'}</td><td class="e">${tl(k.cariBorc)}</td><td class="e">${tl(k.cariAlacak)}</td><td class="e"><b>${tl(k.cariBakiye)}</b></td><td class="e">${k.hesapKodu ? tl(k.hesapBorc) : '—'}</td><td class="e">${k.hesapKodu ? tl(k.hesapAlacak) : '—'}</td><td class="e"><b>${k.hesapKodu ? tl(k.hesapBakiye) : '—'}</b></td><td class="e ${Math.abs(k.fark) > 0.5 ? 'gec' : 'ok'}"><b>${k.hesapKodu ? tl(k.fark) : '—'}</b></td><td class="s">${k.satir} / ${k.hesapSatir}</td><td>${durumRozet(k.durum)}${k.farkAdet ? `<br><small>${k.farkAdet} satır</small>` : ''}</td><td><small>${k.nedenler.map(esc).join('<br>') || '—'}</small></td><td class="ci-git"><button type="button" data-ac="${esc(k.kod)}" title="Satır karşılaştırma"><i class="fas fa-table-columns"></i></button><a href="/Rapor20?bpCode=${encodeURIComponent(k.kod)}" target="_blank" rel="noopener" title="Cari Ekstre"><i class="fas fa-file-invoice"></i></a></td></tr>`).join('') || '<tr><td colspan="14" class="na-bos">Süzgece uyan cari yok.</td></tr>';
    $('#ekTablo tfoot').innerHTML = l.length ? `<tr><td colspan="3">Toplam · ${l.length} cari</td><td class="e">${tl(toplam(l, k => k.cariBorc))}</td><td class="e">${tl(toplam(l, k => k.cariAlacak))}</td><td class="e">${tl(toplam(l, k => k.cariBakiye))}</td><td class="e">${tl(toplam(l, k => k.hesapBorc))}</td><td class="e">${tl(toplam(l, k => k.hesapAlacak))}</td><td class="e">${tl(toplam(l, k => k.hesapBakiye))}</td><td class="e gec">${tl(toplam(l, k => k.fark))}</td><td colspan="4">${l.filter(k => k.durum === 'Fark Var').length} farklı · ${l.filter(k => k.durum === 'Uyuşuyor').length} uyuşan · ${l.filter(k => k.durum === 'Hesap Tanımsız').length} tanımsız</td></tr>` : '';
    $('#ekSayi').textContent = `${l.length} / ${V.cariler.length} cari`; $('#ekN').textContent = l.length; $('#ekNFis').textContent = V.duzeltmeler.length;
    $$('#ekTablo [data-ac]').forEach(b => b.addEventListener('click', e => { e.stopPropagation(); cariAc(b.dataset.ac); })); $$('#ekTablo tr.tik').forEach(r => r.addEventListener('dblclick', () => cariAc(r.dataset.kod)));
    kpi(); fisler(); ciz.son = l;
  }
  async function cariAc(kod) {
    sekme('detay'); $('#ekCari').value = kod; const j = await fetch(`/EkstreKarsilastirma/Detay?kod=${encodeURIComponent(kod)}&${aralik()}`).then(r => r.json()); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    D = j; $('#ekNDetay').textContent = kod; $('#ekEkstre').href = '/Rapor20?bpCode=' + encodeURIComponent(kod);
    const fark = j.satirlar.filter(s => s.durum !== 'Eşleşti');
    $('#ekCariOzet').innerHTML = `<b>${esc(j.kod)} ${esc(j.kisaAd)}</b> · hesap <b>${esc(j.hesapKodu || 'tanımsız')}</b> · cari bakiye <b>${tl(j.cariBakiye)}</b> · hesap bakiye <b>${j.tanimsiz ? '—' : tl(j.hesapBakiye)}</b> · fark <b class="${Math.abs(j.cariBakiye - j.hesapBakiye) > 0.5 ? 'gec' : 'ok'}">${j.tanimsiz ? '—' : tl(j.cariBakiye - j.hesapBakiye)}</b> · ${j.satirlar.length} satır · ${fark.length} fark ${j.tanimsiz ? rozet('Hesap Tanımsız', 'notr') : fark.length ? rozet('Fark Var', 'hata') : rozet('Uyuşuyor', 'ok')}`;
    satirlar(); ozet();
  }
  function satirlar() {
    if (!D) return; const f = $('#ekSatirDurum').value; const l = D.satirlar.filter(s => !f || (f === 'fark' ? s.durum !== 'Eşleşti' : s.durum === f));
    $('#ekSatirlar tbody').innerHTML = l.map((s, i) => { const c = s.cari, h = s.hesap, x = c || h; const kaynak = c ? c : h; return `<tr class="${s.durum !== 'Eşleşti' ? 'secili' : ''}"><td>${esc(x.tarih)}</td><td><b>${esc(x.belge)}</b>${c && c.pb !== 'TL' ? `<br><small class="text-muted">${esc(c.pb)}</small>` : ''}</td><td><small>${esc(x.tip)}${x.islemTipi ? ' · ' + esc(x.islemTipi) : ''}</small></td><td class="e">${c ? (c.borc ? n2(c.borc) : '—') : '<span class="text-muted">yok</span>'}</td><td class="e">${c ? (c.alacak ? n2(c.alacak) : '—') : '<span class="text-muted">yok</span>'}</td><td class="s"><i class="fas ${s.durum === 'Eşleşti' ? 'fa-equals text-success' : s.durum === 'Tutar Farkı' ? 'fa-not-equal text-warning' : s.durum === 'Yalnız Cari' ? 'fa-arrow-right text-danger' : 'fa-arrow-left text-danger'}"></i></td><td><small>${h ? esc(h.fis) : '—'}</small></td><td class="e">${h ? (h.borc ? n2(h.borc) : '—') : '<span class="text-muted">yok</span>'}</td><td class="e">${h ? (h.alacak ? n2(h.alacak) : '—') : '<span class="text-muted">yok</span>'}</td><td class="e ${Math.abs(s.fark) > 0.005 ? 'gec' : ''}">${Math.abs(s.fark) > 0.005 ? n2(s.fark) : '—'}</td><td>${durumRozet(s.durum)}</td><td><small>${s.neden ? `<b>${esc(s.neden)}</b><br>${esc(s.oneri)}` : '—'}</small></td><td class="ci-git">${s.durum !== 'Eşleşti' ? `<button type="button" data-fis="${i}" data-belge="${esc(kaynak.belge)}" data-tur="${esc(s.oneri)}" data-tutar="${s.fark}" data-neden="${esc(s.neden)}" title="Düzeltme fişi oluştur"><i class="fas fa-file-pen"></i></button>` : ''}</td></tr>`; }).join('') || '<tr><td colspan="13" class="na-bos">Satır yok.</td></tr>';
    $('#ekSatirlar tfoot').innerHTML = `<tr><td colspan="3">Toplam · ${l.length} satır</td><td class="e">${n2(toplam(l, s => s.cari ? s.cari.borc : 0))}</td><td class="e">${n2(toplam(l, s => s.cari ? s.cari.alacak : 0))}</td><td></td><td></td><td class="e">${n2(toplam(l, s => s.hesap ? s.hesap.borc : 0))}</td><td class="e">${n2(toplam(l, s => s.hesap ? s.hesap.alacak : 0))}</td><td class="e gec">${n2(toplam(l, s => s.fark))}</td><td colspan="3">${l.filter(s => s.durum !== 'Eşleşti').length} fark</td></tr>`;
    $$('#ekSatirlar [data-fis]').forEach(b => b.addEventListener('click', () => fis(b.dataset.belge, b.dataset.tur, +b.dataset.tutar, b.dataset.neden)));
  }
  function ozet() {
    if (!D) return; const satir = (l, v, cls) => `<div class="l">${l}</div><div class="v ${cls || ''}">${v}</div>`; const fark = D.satirlar.filter(s => s.durum !== 'Eşleşti');
    $('#ekOzet').innerHTML = satir('Cari bakiye', tl(D.cariBakiye)) + satir('Hesap bakiye', D.tanimsiz ? '—' : tl(D.hesapBakiye)) + satir('Fark', D.tanimsiz ? '—' : tl(D.cariBakiye - D.hesapBakiye), Math.abs(D.cariBakiye - D.hesapBakiye) > 0.5 ? 'gec' : 'ok') + '<div class="ayrac"></div>' + D.ozet.map(o => satir(o.durum, `${o.adet} satır${Math.abs(o.fark) > 0.005 ? ' · ' + tl(o.fark) : ''}`, o.durum === 'Eşleşti' ? '' : 'gec')).join('') + '<div class="ayrac"></div>' + satir('Bu caride düzeltme fişi', D.duzeltmeler.length);
    $('#ekTumFis').disabled = fark.length === 0;
  }
  async function fis(belge, tur, tutar, neden) {
    if (!D) return; if (!confirm(`${D.kod} · ${belge}: "${tur}" düzeltme fişi oluşturulsun mu? (${n2(Math.abs(tutar))} ₺)`)) return;
    const j = await post('/EkstreKarsilastirma/DuzeltmeFisi', { kod: D.kod, belge, tur, tutar, aciklama: neden }); mesaj(j.success ? 'success' : 'warning', esc(j.message)); if (j.success) await yenile(D.kod);
  }
  async function tumFis() {
    if (!D) return; const fark = D.satirlar.filter(s => s.durum !== 'Eşleşti'); if (!confirm(`${D.kod} için ${fark.length} fark satırına düzeltme fişi oluşturulsun mu?`)) return; let n = 0;
    for (const s of fark) { const x = s.cari || s.hesap; const j = await post('/EkstreKarsilastirma/DuzeltmeFisi', { kod: D.kod, belge: x.belge, tur: s.oneri, tutar: s.fark, aciklama: s.neden }); if (j.success) n++; }
    mesaj('success', `${n} düzeltme fişi oluşturuldu.`); await yenile(D.kod);
  }
  function fisler() { $('#ekFisler tbody').innerHTML = V.duzeltmeler.map(d => `<tr><td>${esc(d.zaman)}</td><td><b>${esc(d.fisNo)}</b></td><td><b>${esc(d.cariKodu)}</b> ${esc(d.cariAd)}</td><td>${esc(d.tur)}</td><td><small>${esc(d.aciklama)}</small></td><td class="e">${esc(d.tutarTr)}</td><td>${esc(d.kullanici)}</td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Henüz düzeltme fişi yok.</td></tr>'; }
  async function yenile(kod) { V = await fetch('/EkstreKarsilastirma/Veri?' + aralik()).then(r => r.json()); doldur(); ciz(); if (kod) await cariAc(kod); }
  function doldur() { const s = $('#ekSirket'), v = s.value; s.innerHTML = '<option value="">Tümü</option>' + [...new Set(V.cariler.map(k => k.sirket))].sort().map(x => `<option>${esc(x)}</option>`).join(''); s.value = v; const c = $('#ekCari'), cv = c.value; c.innerHTML = '<option value="">Seçiniz…</option>' + V.cariler.map(k => `<option value="${esc(k.kod)}">${esc(k.kod)} · ${esc(k.kisaAd)}${k.farkAdet ? ` · ${k.farkAdet} fark` : ''}</option>`).join(''); c.value = cv; }
  function baslat() {
    $('#ekBas').value = V.bas; $('#ekBit').value = V.bit;
    ['#ekAra', '#ekTip', '#ekSirket', '#ekDurum', '#ekFarkTur'].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    $('#ekRaporla').addEventListener('click', () => yenile(D ? D.kod : null)); $('#ekSatirDurum').addEventListener('change', satirlar);
    $('#ekTemizle').addEventListener('click', () => { $('#ekAra').value = ''; ['#ekTip', '#ekSirket', '#ekDurum', '#ekFarkTur'].forEach(s => $(s).value = ''); ciz(); });
    $$('#ekSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    $('#ekCari').addEventListener('change', () => { if ($('#ekCari').value) cariAc($('#ekCari').value); }); $('#ekTumFis').addEventListener('click', tumFis);
    $('#ekExcel').addEventListener('click', () => csvIndir('cari_hesap_ekstre_mutabakati', ['Cari Kodu', 'Cari', 'Tip', 'Şirket', 'Hesap Kodu', 'Cari Borç', 'Cari Alacak', 'Cari Bakiye', 'Hesap Borç', 'Hesap Alacak', 'Hesap Bakiye', 'Fark', 'Durum', 'Fark Satırı', 'Nedenler'], (ciz.son || []).map(k => [k.kod, k.kisaAd, k.tip, k.sirket, k.hesapKodu, k.cariBorc, k.cariAlacak, k.cariBakiye, k.hesapBorc, k.hesapAlacak, k.hesapBakiye, k.fark, k.durum, k.farkAdet, k.nedenler.join(' | ')])));
    doldur(); ciz(); const on = kok.dataset.kod; if (on) cariAc(on);
  }
  fetch('/EkstreKarsilastirma/Veri').then(r => r.json()).then(j => { V = j; baslat(); }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
