// ============================================================
// KONSOLİDE TAHSİLAT TAKİP RAPORU (Views/Rapor17/Index.cshtml) — tüm şirketlerin müşteri carileri: vade kırılımı, çek / senet, son 30 gün tahsilat, risk; haftalık beklenen plan; şirket özeti
//   Veri: /Tahsilat/KonsolideTakip (Controllers/TahsilatController.cs)
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('kt'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const tl = v => Number(v || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 }) + ' ₺';
  const n2 = v => Number(v || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const riskRozet = d => rozet(d || '—', d === 'Riskli' ? 'hata' : d === 'İzlemede' ? 'bekle' : 'ok');
  const bugunIso = new Date().toISOString().slice(0, 10);
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + bugunIso + '.csv'; a.click(); };
  const trh = iso => { const [y, m, d] = iso.split('-'); return `${d}.${m}.${y}`; };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; }
  function sekme(p) { $$('#ktSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('.na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); }
  let V = null;
  const toplam = (l, f) => l.reduce((s, k) => s + (Number(f(k)) || 0), 0);
  const secDoldur = (el, l, ilk) => { const v = el.value; el.innerHTML = `<option value="">${ilk}</option>` + l.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); el.value = l.includes(v) ? v : ''; };

  function liste() {
    const ara = ($('#ktAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value, d = v('#ktDurum');
    return V.satirlar.filter(k => (!v('#ktSirket') || k.sirket === v('#ktSirket')) && (!v('#ktTemsilci') || (k.temsilci || '').split(', ').includes(v('#ktTemsilci'))) && (!v('#ktBolge') || k.bolge === v('#ktBolge')) && (!v('#ktRisk') || k.risk === v('#ktRisk'))
      && (!d || (d === 'gecmis' ? k.gecmis > 0 : d === 'buhafta' ? k.buHafta > 0 : d === 'cek' ? k.cek > 0 : d === 'tahsilatsiz' ? (k.son30Adet === 0 && k.acikToplam > 0) : k.bakiye > 0))
      && (!ara || [k.kod, k.kisaAd, k.resmiUnvan, k.temsilci, k.sirket].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  function sirala(l) { const s = $('#ktSirala').value; const f = { gecmis: k => -k.gecmis, acik: k => -k.acikToplam, buhafta: k => -k.buHafta, bakiye: k => -k.bakiye, son30: k => -k.son30Tahsilat }[s]; return l.slice().sort((a, b) => f ? (f(a) - f(b)) || a.kod.localeCompare(b.kod) : a.kod.localeCompare(b.kod)); }
  const RENK = { gecmis: '#dc2626', buHafta: '#f59e0b', g8_30: '#0ea5e9', g31_60: '#2563eb', g60: '#6366f1' };
  function cubuk(k) { const t = k.acikToplam || 0; if (!t) return '<span class="text-muted">—</span>'; return `<div class="ci-cubuk" title="gecikmiş ${tl(k.gecmis)} · bu hafta ${tl(k.buHafta)} · 8–30 ${tl(k.g8_30)} · 31–60 ${tl(k.g31_60)} · 60+ ${tl(k.g60)}">${['gecmis', 'buHafta', 'g8_30', 'g31_60', 'g60'].map(a => `<span style="width:${(k[a] / t * 100).toFixed(1)}%;background:${RENK[a]}"></span>`).join('')}</div>`; }
  function kpi(l) {
    const kutu = (k, l1, v, s, sec) => `<div style="--k:${k}" ${sec ? `data-sec="${sec}" title="Süzgeçle"` : ''}><div class="l">${l1}</div><div class="v">${v}</div><div class="s">${s}</div></div>`;
    $('#ktKpi').innerHTML = [kutu('#0369a1', 'Açık Alacak', tl(toplam(l, k => k.acikToplam)), `${l.length} cari · ${toplam(l, k => k.acikAdet)} fatura`, 'acik'), kutu('#dc2626', 'Gecikmiş', tl(toplam(l, k => k.gecmis)), `${l.filter(k => k.gecmis > 0).length} cari · ${toplam(l, k => k.gecmisAdet)} fatura`, 'gecmis'), kutu('#f59e0b', 'Bu Hafta Vadesi Gelen', tl(toplam(l, k => k.buHafta)), `${l.filter(k => k.buHafta > 0).length} cari`, 'buhafta'), kutu('#2563eb', '8–60 Gün', tl(toplam(l, k => k.g8_30 + k.g31_60)), `60+ gün ${tl(toplam(l, k => k.g60))}`), kutu('#7c3aed', 'İçerideki Çek / Senet', tl(toplam(l, k => k.cek)), `${toplam(l, k => k.cekAdet)} belge`, 'cek'), kutu('#15803d', 'Son 30 Gün Tahsilat', tl(toplam(l, k => k.son30Tahsilat)), `${toplam(l, k => k.son30Adet)} tahsilat · ${l.filter(k => k.son30Adet === 0 && k.acikToplam > 0).length} caride tahsilat yok`, 'tahsilatsiz')].join('');
    $$('#ktKpi [data-sec]').forEach(d => d.addEventListener('click', () => { const e = $('#ktDurum'); e.value = e.value === d.dataset.sec ? '' : d.dataset.sec; sekme('liste'); ciz(); }));
  }
  function satir(k) {
    return `<tr data-kod="${esc(k.kod)}" title="${esc(k.riskNeden)}"><td><small><b>${esc(k.sirket)}</b></small></td><td><b>${esc(k.kod)}</b> ${esc(k.kisaAd)}<br><small class="text-muted">${esc(k.temsilci || '—')} · ${esc(k.sinif)} · ${esc(k.vadeSarti || '')}</small></td><td class="e"><b>${tl(k.bakiye)}</b><br><small class="text-muted">X ${tl(k.bakiyeX)} · R ${tl(k.bakiyeR)}</small></td><td class="e ${k.gecmis > 0 ? 'gec' : ''}">${k.gecmis > 0 ? `${tl(k.gecmis)}<br><small>${k.gecmisAdet} fatura · ${k.maxGecikme} gün</small>` : '—'}</td><td class="e">${k.buHafta ? tl(k.buHafta) : '—'}</td><td class="e">${k.g8_30 ? tl(k.g8_30) : '—'}</td><td class="e">${k.g31_60 ? tl(k.g31_60) : '—'}</td><td class="e">${k.g60 ? tl(k.g60) : '—'}</td><td class="e"><b>${tl(k.acikToplam)}</b><br><small class="text-muted">${k.acikAdet} fatura</small></td><td class="e">${k.cek ? `${tl(k.cek)}<br><small class="text-muted">${k.cekAdet} belge</small>` : '—'}</td><td class="e ${k.son30Tahsilat ? 'ok' : ''}">${k.son30Tahsilat ? `${tl(k.son30Tahsilat)} <small class="text-muted">(${k.son30Adet})</small>` : '<span class="text-muted">yok</span>'}<br><small class="text-muted">${k.sonTahsilat ? `son ${esc(k.sonTahsilat)} · ${tl(k.sonTahsilatTl)}` : 'tahsilat yok'}</small></td><td>${cubuk(k)}</td><td>${riskRozet(k.risk)}<br><small class="text-muted">${esc(k.limitDurum)}</small></td><td class="ci-git"><a href="/Rapor117?kod=${encodeURIComponent(k.kod)}" title="Tahsilat ekranı (bu cari)"><i class="fas fa-hand-holding-dollar"></i></a><a href="/CariKart/Musteri?kod=${encodeURIComponent(k.kod)}" title="Cari kartı"><i class="fas fa-address-card"></i></a><a href="/Rapor20?bpCode=${encodeURIComponent(k.kod)}" target="_blank" rel="noopener" title="Cari Ekstre"><i class="fas fa-file-invoice"></i></a></td></tr>`;
  }
  function ciz() {
    const l = sirala(liste());
    $('#ktTablo tbody').innerHTML = l.map(satir).join('') || '<tr><td colspan="14" class="na-bos">Süzgece uyan cari yok.</td></tr>';
    $('#ktTablo tfoot').innerHTML = l.length ? `<tr><td colspan="2">Toplam · ${l.length} cari</td><td class="e">${tl(toplam(l, k => k.bakiye))}</td><td class="e gec">${tl(toplam(l, k => k.gecmis))}</td><td class="e">${tl(toplam(l, k => k.buHafta))}</td><td class="e">${tl(toplam(l, k => k.g8_30))}</td><td class="e">${tl(toplam(l, k => k.g31_60))}</td><td class="e">${tl(toplam(l, k => k.g60))}</td><td class="e">${tl(toplam(l, k => k.acikToplam))}</td><td class="e">${tl(toplam(l, k => k.cek))}</td><td class="e">${tl(toplam(l, k => k.son30Tahsilat))}</td><td colspan="3">${l.filter(k => k.risk === 'Riskli').length} riskli</td></tr>` : '';
    $('#ktSayi').textContent = `${l.length} / ${V.satirlar.length} cari`; $('#ktN').textContent = l.length;
    kpi(l); hafta(l); sirketOzet(l); ciz.son = l;
  }
  // ---- haftalık beklenen (fatura vadeleri → hafta kovaları)
  let HAFTA = [];
  function hafta(l) {
    const bugun = new Date(V.bugun + 'T00:00:00'); const kal = []; l.forEach(k => k.kalemler.forEach(x => kal.push({ ...x, kod: k.kod, ad: k.kisaAd, sirket: k.sirket })));
    const kovalar = [{ ad: 'Gecikmiş', bas: null, bit: -1, l: kal.filter(x => x.gun < 0) }];
    for (let h = 0; h < 8; h++) { const b1 = h * 7, b2 = h * 7 + 6; kovalar.push({ ad: h === 0 ? 'Bu Hafta' : `${h + 1}. Hafta`, bas: b1, bit: b2, l: kal.filter(x => x.gun >= b1 && x.gun <= b2) }); }
    kovalar.push({ ad: '8 Haftadan Sonra', bas: 56, bit: null, l: kal.filter(x => x.gun > 55) });
    HAFTA = kovalar; const maks = Math.max(1, ...kovalar.map(k => k.l.reduce((s, x) => s + x.tl, 0)));
    const t = d => { const x = new Date(bugun); x.setDate(x.getDate() + d); return x.toLocaleDateString('tr-TR'); };
    $('#ktHafta tbody').innerHTML = kovalar.map((k, i) => { const tut = k.l.reduce((s, x) => s + x.tl, 0); return `<tr class="tik" data-i="${i}"><td><b>${k.ad}</b></td><td><small>${k.bas === null ? `${V.bugun.split('-').reverse().join('.')} öncesi` : k.bit === null ? `${t(k.bas)} sonrası` : `${t(k.bas)} – ${t(k.bit)}`}</small></td><td class="s">${k.l.length}</td><td class="s">${new Set(k.l.map(x => x.kod)).size}</td><td class="e ${i === 0 ? 'gec' : ''}"><b>${tl(tut)}</b></td><td><div class="ci-cubuk" style="min-width:160px"><span style="width:${(tut / maks * 100).toFixed(1)}%;background:${i === 0 ? '#dc2626' : i === 1 ? '#f59e0b' : '#0369a1'}"></span></div></td></tr>`; }).join('');
    $('#ktHafta tfoot').innerHTML = `<tr><td colspan="2">Toplam</td><td class="s">${kal.length}</td><td class="s">${new Set(kal.map(x => x.kod)).size}</td><td class="e">${tl(kal.reduce((s, x) => s + x.tl, 0))}</td><td></td></tr>`;
    $$('#ktHafta tr.tik').forEach(r => r.addEventListener('click', () => haftaDetay(+r.dataset.i)));
  }
  function haftaDetay(i) {
    const k = HAFTA[i]; if (!k) return; $$('#ktHafta tr.tik').forEach(r => r.classList.toggle('secili', +r.dataset.i === i));
    $('#ktHaftaNot').textContent = `${k.ad} · ${k.l.length} fatura · ${tl(k.l.reduce((s, x) => s + x.tl, 0))}`;
    $('#ktHaftaDetay tbody').innerHTML = k.l.slice().sort((a, b) => a.vade.localeCompare(b.vade)).map(x => `<tr><td class="${x.gun < 0 ? 'gec' : ''}">${trh(x.vade)}${x.gun < 0 ? `<br><small>${-x.gun} gün gecikme</small>` : ''}</td><td><b>${esc(x.kod)}</b> ${esc(x.ad)}<br><small class="text-muted">${esc(x.sirket)}</small></td><td>${esc(x.belge)}</td><td class="s">${esc(x.islemTipi)}</td><td class="e">${n2(x.kalan)} ${esc(x.pb)}</td><td class="e">${tl(x.tl)}</td></tr>`).join('') || '<tr><td colspan="6" class="na-bos">Bu haftada fatura yok.</td></tr>';
  }
  function sirketOzet(l) {
    const g = {}; l.forEach(k => { const p = g[k.sirket] || (g[k.sirket] = { n: 0, bakiye: 0, gecmis: 0, gecikmeli: 0, buHafta: 0, g8_30: 0, g31_60: 0, g60: 0, cek: 0, son30: 0, riskli: 0, acik: 0 }); p.n++; p.bakiye += k.bakiye; p.gecmis += k.gecmis; if (k.gecmis > 0) p.gecikmeli++; p.buHafta += k.buHafta; p.g8_30 += k.g8_30; p.g31_60 += k.g31_60; p.g60 += k.g60; p.cek += k.cek; p.son30 += k.son30Tahsilat; if (k.risk === 'Riskli') p.riskli++; p.acik += k.acikToplam; });
    const ks = Object.keys(g).sort((a, b) => g[b].acik - g[a].acik);
    $('#ktSirketTablo tbody').innerHTML = ks.map(s => { const p = g[s]; const oran = p.acik + p.son30 > 0 ? p.son30 / (p.acik + p.son30) * 100 : 0; return `<tr class="tik" data-sirket="${esc(s)}"><td><b>${esc(s)}</b></td><td class="s">${p.n}</td><td class="e">${tl(p.bakiye)}</td><td class="e gec">${p.gecmis ? tl(p.gecmis) : '—'}</td><td class="s">${p.gecikmeli || '—'}</td><td class="e">${tl(p.buHafta)}</td><td class="e">${tl(p.g8_30)}</td><td class="e">${tl(p.g31_60)}</td><td class="e">${tl(p.g60)}</td><td class="e">${tl(p.cek)}</td><td class="e ok">${tl(p.son30)}</td><td class="s">${p.riskli || '—'}</td><td><div class="d-flex align-items-center gap-1"><div class="ci-cubuk" style="min-width:90px"><span style="width:${oran.toFixed(0)}%;background:#15803d"></span></div><small>%${oran.toFixed(0)}</small></div></td></tr>`; }).join('') || '<tr><td colspan="13" class="na-bos">Kayıt yok.</td></tr>';
    $('#ktSirketTablo tfoot').innerHTML = l.length ? `<tr><td>Toplam</td><td class="s">${l.length}</td><td class="e">${tl(toplam(l, k => k.bakiye))}</td><td class="e gec">${tl(toplam(l, k => k.gecmis))}</td><td class="s">${l.filter(k => k.gecmis > 0).length}</td><td class="e">${tl(toplam(l, k => k.buHafta))}</td><td class="e">${tl(toplam(l, k => k.g8_30))}</td><td class="e">${tl(toplam(l, k => k.g31_60))}</td><td class="e">${tl(toplam(l, k => k.g60))}</td><td class="e">${tl(toplam(l, k => k.cek))}</td><td class="e">${tl(toplam(l, k => k.son30Tahsilat))}</td><td class="s">${l.filter(k => k.risk === 'Riskli').length}</td><td></td></tr>` : '';
    $$('#ktSirketTablo tr.tik').forEach(r => r.addEventListener('click', () => { const e = $('#ktSirket'); e.value = e.value === r.dataset.sirket ? '' : r.dataset.sirket; sekme('liste'); ciz(); }));
  }
  function tahsilatlar() {
    const l = V.tahsilatlar || [];
    $('#ktTahsilat tbody').innerHTML = l.map(t => `<tr><td><b>${esc(t.no)}</b></td><td>${esc(t.tarih)}</td><td><small>${esc(t.sirket)}</small></td><td><b>${esc(t.cariKodu)}</b> ${esc(t.cariAd)}</td><td>${esc(t.yontem)}</td><td class="s">${esc(t.islemTipi)}</td><td class="e">${esc(t.tutarTr)}</td><td class="e">${esc(t.tutarTlTr)}</td><td>${t.mod === 'Fatura' ? 'Fatura Bazlı' : 'Bakiyeden'}</td><td class="s">${t.dagitim.length}</td><td class="e ${t.vadeFarki > 0 ? 'gec' : ''}">${t.vadeFarki > 0 ? esc(t.vadeFarkiTr) : '—'}</td><td class="e">${t.kurFarki ? esc(t.kurFarkiTr) : '—'}</td><td>${[t.kurFarkiFaturaNo, t.vadeFarkiFaturaNo].filter(Boolean).map(esc).join(', ') || '—'}</td><td class="ci-git"><a href="/Rapor117?kod=${encodeURIComponent(t.cariKodu)}" title="Tahsilat ekranı"><i class="fas fa-hand-holding-dollar"></i></a><a href="/Rapor123?sekme=${esc(t.panelSekme)}&sirket=${encodeURIComponent(t.sirketDb)}" target="_blank" rel="noopener" title="Konsolide Finansal Takip Paneli"><i class="fas fa-clipboard-check"></i></a></td></tr>`).join('') || '<tr><td colspan="14" class="na-bos">Kayıt yok.</td></tr>';
    $('#ktNT').textContent = l.length;
  }
  function baslat() {
    secDoldur($('#ktSirket'), [...new Set(V.satirlar.map(k => k.sirket))].sort(), 'Tümü'); secDoldur($('#ktTemsilci'), [...new Set(V.satirlar.flatMap(k => (k.temsilci || '').split(', ').filter(Boolean)))].sort(), 'Tümü');
    ['#ktAra', '#ktSirket', '#ktTemsilci', '#ktBolge', '#ktRisk', '#ktDurum', '#ktSirala'].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    $('#ktTemizle').addEventListener('click', () => { $('#ktAra').value = ''; ['#ktSirket', '#ktTemsilci', '#ktBolge', '#ktRisk', '#ktDurum'].forEach(s => $(s).value = ''); ciz(); });
    $$('#ktSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    $('#ktExcel').addEventListener('click', () => csvIndir('konsolide_tahsilat_takip', ['Şirket', 'Cari Kodu', 'Kısa Ad', 'Temsilci', 'Bölge', 'Sınıf', 'Vade Şartı', 'Bakiye (₺)', 'Bakiye X', 'Bakiye R', 'Gecikmiş (₺)', 'Gecikmiş Fatura', 'En Uzun Gecikme', 'Bu Hafta', '8–30 Gün', '31–60 Gün', '60+ Gün', 'Açık Toplam', 'Açık Fatura', 'Çek / Senet', 'Son 30 Gün Tahsilat', 'Son Tahsilat', 'Risk', 'Limit Durumu'], (ciz.son || []).map(k => [k.sirket, k.kod, k.kisaAd, k.temsilci, k.bolge, k.sinif, k.vadeSarti, k.bakiye, k.bakiyeX, k.bakiyeR, k.gecmis, k.gecmisAdet, k.maxGecikme, k.buHafta, k.g8_30, k.g31_60, k.g60, k.acikToplam, k.acikAdet, k.cek, k.son30Tahsilat, k.sonTahsilat, k.risk, k.limitDurum])));
    tahsilatlar(); ciz();
  }
  fetch('/Tahsilat/KonsolideTakip').then(r => r.json()).then(j => { V = j; baslat(); }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
