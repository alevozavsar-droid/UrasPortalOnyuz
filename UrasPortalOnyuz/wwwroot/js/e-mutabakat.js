// ============================================================
// E-MUTABAKAT (Views/Rapor59/Index.cshtml) — e-posta / WhatsApp ile otomatik mutabakat: cari listesi, önizleme, tekil / toplu gönderim, hatırlatma, yanıtlar, iletişim bilgileri, şablonlar, günlük
//   Uçlar: /EMutabakat/Veri, Onizle, Gonder, Hatirlat, Yanit, YanitlariCek, KisiGuncelle, SablonKaydet (Controllers/EMutabakatController.cs)
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('em'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const tl = v => Number(v || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 }) + ' ₺';
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const durumRozet = (d, hat) => rozet(hat ? 'Hatırlatma Zamanı' : d, d === 'Mutabık' ? 'ok' : d === 'İtirazlı' ? 'hata' : d === 'Yanıt Bekliyor' ? (hat ? 'hata' : 'bekle') : 'notr');
  const kanalRozet = k => k === 'WhatsApp' ? '<span class="ci-kanal wa"><i class="fab fa-whatsapp"></i> WhatsApp</span>' : '<span class="ci-kanal mail"><i class="fas fa-envelope"></i> E-posta</span>';
  const post = (u, b) => fetch(u, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(b || {}) }).then(r => r.json());
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; }
  function sekme(p) { $$('#emSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('.na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); }
  let V = null, SECILI = null;
  const toplam = (l, f) => l.reduce((s, k) => s + (Number(f(k)) || 0), 0);

  function liste() {
    const ara = ($('#emAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value, u = v('#emUygun'), d = v('#emDurum'), b = v('#emBakiye');
    return V.cariler.filter(k => (!v('#emTip') || k.tip === v('#emTip')) && (!v('#emSirket') || k.sirket === v('#emSirket')) && (!u || (u === 'mail' ? k.mail : u === 'wa' ? k.wa : u === 'ikisi' ? (k.mail && k.wa) : (!k.mail && !k.wa))) && (!d || (d === 'hatirlat' ? k.hatirlat : k.durum === d)) && (!b || (b === 'var' ? Math.abs(k.bakiyeTl) > 0.5 : Math.abs(k.bakiyeTl) <= 0.5))
      && (!ara || [k.kod, k.kisaAd, k.resmiUnvan, k.yetkili?.adSoyad, k.yetkili?.eposta, k.yetkili?.cep, k.temsilci].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  function kpi() {
    const l = V.cariler; const kutu = (k, l1, v, s, sec) => `<div style="--k:${k}" ${sec ? `data-sec="${sec}"` : ''}><div class="l">${l1}</div><div class="v">${v}</div><div class="s">${s}</div></div>`;
    $('#emKpi').innerHTML = [kutu('#0369a1', 'Cari', l.length, `${l.filter(k => k.tip === 'Müşteri').length} müşteri · ${l.filter(k => k.tip === 'Tedarikçi').length} tedarikçi`), kutu('#15803d', 'Gönderilebilir', l.filter(k => k.mail || k.wa).length, `${l.filter(k => k.mail).length} e-posta · ${l.filter(k => k.wa).length} WhatsApp · ${l.filter(k => k.mail && k.wa).length} ikisi`), kutu('#dc2626', 'İletişim Eksik', l.filter(k => !k.mail && !k.wa).length, 'e-posta ve cep telefonu yok', 'yok'), kutu('#f59e0b', 'Yanıt Bekleyen', l.filter(k => k.durum === 'Yanıt Bekliyor').length, `${l.filter(k => k.hatirlat).length} hatırlatma zamanı gelmiş`, 'Yanıt Bekliyor'), kutu('#15803d', 'Mutabık', l.filter(k => k.durum === 'Mutabık').length, `${tl(toplam(l.filter(k => k.durum === 'Mutabık'), k => Math.abs(k.bakiyeTl)))} bakiye`, 'Mutabık'), kutu('#dc2626', 'İtirazlı', l.filter(k => k.durum === 'İtirazlı').length, `${tl(toplam(l.filter(k => k.durum === 'İtirazlı'), k => Math.abs(k.bakiyeTl)))} bakiye`, 'İtirazlı')].join('');
    $$('#emKpi [data-sec]').forEach(x => x.addEventListener('click', () => { const s = x.dataset.sec; if (s === 'yok') { const e = $('#emUygun'); e.value = e.value === 'yok' ? '' : 'yok'; } else { const e = $('#emDurum'); e.value = e.value === s ? '' : s; } sekme('liste'); ciz(); }));
  }
  function satir(k) {
    const y = k.yetkili; const son = k.son;
    return `<tr data-kod="${esc(k.kod)}" class="tik ${SECILI === k.kod ? 'secili' : ''}"><td class="s">${k.mail || k.wa ? `<input type="checkbox" class="emSec" value="${esc(k.kod)}">` : ''}</td><td><b>${esc(k.kod)}</b> ${esc(k.kisaAd)}<br><small class="text-muted"><span class="ck-tip">${esc(k.tip)}</span> ${esc(k.temsilci)}</small></td><td><small>${esc(k.sirket)}</small></td><td class="e"><b>${tl(k.bakiyeTl)}</b><br><small class="text-muted">${k.dovizler.map(d => esc(d.netTr)).join(' · ') || '—'}${k.acikFatura ? ` · ${k.acikFatura} açık fatura` : ''}</small></td><td>${y ? `<b>${esc(y.adSoyad || '—')}</b><br><small class="text-muted">${esc(y.eposta || 'e-posta yok')} · ${esc(y.cep || 'cep yok')}</small>` : '<span class="text-muted">yetkili yok</span>'}</td><td>${k.mail ? '<span class="ci-kanal mail"><i class="fas fa-envelope"></i></span>' : ''} ${k.wa ? '<span class="ci-kanal wa"><i class="fab fa-whatsapp"></i></span>' : ''}${!k.mail && !k.wa ? '<span class="ci-kanal yok">eksik</span>' : ''}</td><td><small>${son ? `${esc(son.tarih)}<br>${kanalRozet(son.kanal)}${son.tekrar ? ` · ${son.tekrar} hatırlatma` : ''}` : '—'}</small></td><td>${durumRozet(k.durum, k.hatirlat)}${k.sonMutabik ? `<br><small class="text-muted">son mutabık ${esc(k.sonMutabik)}</small>` : ''}</td><td class="ci-git">${k.mail ? `<button type="button" data-g="${esc(k.kod)}" data-k="E-posta" title="E-posta gönder"><i class="fas fa-envelope"></i></button>` : ''}${k.wa ? `<button type="button" data-g="${esc(k.kod)}" data-k="WhatsApp" title="WhatsApp gönder"><i class="fab fa-whatsapp"></i></button>` : ''}${k.durum === 'Yanıt Bekliyor' ? `<button type="button" data-h="${esc(k.kod)}" title="Hatırlat"><i class="fas fa-bell"></i></button>` : ''}<a href="/CariMutabakat?kod=${encodeURIComponent(k.kod)}" target="_blank" rel="noopener" title="Cari Mutabakat Yönetimi"><i class="fas fa-handshake"></i></a></td></tr>`;
  }
  function ciz() {
    const l = liste().slice().sort((a, b) => (b.hatirlat - a.hatirlat) || a.kod.localeCompare(b.kod));
    $('#emTablo tbody').innerHTML = l.map(satir).join('') || '<tr><td colspan="9" class="na-bos">Süzgece uyan cari yok.</td></tr>';
    $('#emTablo tfoot').innerHTML = l.length ? `<tr><td colspan="3">Toplam · ${l.length} cari</td><td class="e">${tl(toplam(l, k => k.bakiyeTl))}</td><td colspan="5">${l.filter(k => k.mail || k.wa).length} gönderilebilir · ${l.filter(k => k.durum === 'Yanıt Bekliyor').length} bekliyor · ${l.filter(k => k.durum === 'Mutabık').length} mutabık · ${l.filter(k => k.durum === 'İtirazlı').length} itirazlı</td></tr>` : '';
    $('#emSayi').textContent = `${l.length} / ${V.cariler.length} cari`; $('#emN').textContent = l.length;
    $$('#emTablo tr.tik').forEach(r => r.addEventListener('click', e => { if (e.target.closest('input,button,a')) return; onizle(r.dataset.kod); }));
    $$('#emTablo [data-g]').forEach(b => b.addEventListener('click', () => gonder([b.dataset.g], b.dataset.k)));
    $$('#emTablo [data-h]').forEach(b => b.addEventListener('click', () => hatirlat(b.dataset.h)));
    $$('.emSec').forEach(c => c.addEventListener('change', seciliSay)); seciliSay(); kpi();
  }
  function seciliSay() { const n = $$('.emSec:checked').length; $('#emSeciliGonder').disabled = n === 0; $('#emSeciliGonder').innerHTML = `<i class="fas fa-paper-plane me-1"></i>Seçilenlere Gönder${n ? ` (${n})` : ''}`; }
  async function onizle(kod, hatirlatma) {
    SECILI = kod; $$('#emTablo tr.tik').forEach(r => r.classList.toggle('secili', r.dataset.kod === kod));
    const j = await fetch(`/EMutabakat/Onizle?kod=${encodeURIComponent(kod)}&hatirlatma=${!!hatirlatma}`).then(r => r.json()); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    const c = j.cari; $('#emOnNot').textContent = `${c.kod} · ${c.kisaAd}`;
    $('#emOnizle').innerHTML = `<div class="na-mini mb-1"><b>${esc(c.kod)} ${esc(c.kisaAd)}</b> · ${esc(c.sirket)} · bakiye <b>${tl(c.bakiyeTl)}</b> · ${durumRozet(c.durum, c.hatirlat)}</div>
      <div class="d-flex justify-content-between align-items-center mb-1"><span class="ci-kanal mail"><i class="fas fa-envelope"></i> E-posta → ${esc(j.mail.kime || 'adres yok')}</span><span class="d-flex gap-1">${j.mail.uygun ? `<button type="button" class="btn btn-primary btn-sm py-0" id="emOnMail" style="font-size:.62rem"><i class="fas fa-paper-plane me-1"></i>Gönder</button><a class="btn btn-light btn-sm py-0" style="font-size:.62rem" href="${esc(j.mail.mailto)}" title="Cihazdaki posta uygulamasında aç"><i class="fas fa-arrow-up-right-from-square"></i></a>` : '<span class="ci-kanal yok">e-posta yok</span>'}</span></div>
      <div class="ci-mesaj mail mb-2"><b>${esc(j.mail.konu)}</b>\n<small class="text-muted">Ek: ${esc(j.mail.ek)}</small>\n\n${esc(j.mail.govde)}</div>
      <div class="d-flex justify-content-between align-items-center mb-1"><span class="ci-kanal wa"><i class="fab fa-whatsapp"></i> WhatsApp → ${esc(j.wa.kime || 'numara yok')}</span><span class="d-flex gap-1">${j.wa.uygun ? `<button type="button" class="btn btn-success btn-sm py-0" id="emOnWa" style="font-size:.62rem"><i class="fas fa-paper-plane me-1"></i>Gönder</button><a class="btn btn-light btn-sm py-0" style="font-size:.62rem" href="${esc(j.wa.link)}" target="_blank" rel="noopener" title="WhatsApp'ta aç (wa.me)"><i class="fas fa-arrow-up-right-from-square"></i></a>` : '<span class="ci-kanal yok">cep yok</span>'}</span></div>
      <div class="ci-mesaj wa">${esc(j.wa.govde)}</div>
      <div class="d-flex gap-1 mt-2 flex-wrap">${j.mail.uygun && j.wa.uygun ? `<button type="button" class="btn btn-outline-primary btn-sm py-0" id="emOnIkisi" style="font-size:.62rem"><i class="fas fa-paper-plane me-1"></i>İkisini De Gönder</button>` : ''}${c.durum === 'Yanıt Bekliyor' ? `<button type="button" class="btn btn-outline-secondary btn-sm py-0" id="emOnHat" style="font-size:.62rem"><i class="fas fa-bell me-1"></i>Hatırlat</button><button type="button" class="btn btn-outline-success btn-sm py-0" data-y="Mutabık" style="font-size:.62rem"><i class="fas fa-check me-1"></i>Yanıt: Mutabık</button><button type="button" class="btn btn-outline-danger btn-sm py-0" data-y="İtirazlı" style="font-size:.62rem"><i class="fas fa-xmark me-1"></i>Yanıt: İtiraz</button>` : ''}<button type="button" class="btn btn-light btn-sm py-0" id="emOnHatOn" style="font-size:.62rem" title="Hatırlatma ekiyle önizle">${hatirlatma ? 'Normal Metin' : 'Hatırlatma Metni'}</button></div>`;
    const b = (id, f) => { const e = $(id); if (e) e.addEventListener('click', f); };
    b('#emOnMail', () => gonder([kod], 'E-posta')); b('#emOnWa', () => gonder([kod], 'WhatsApp')); b('#emOnIkisi', () => gonder([kod], 'İkisi')); b('#emOnHat', () => hatirlat(kod)); b('#emOnHatOn', () => onizle(kod, !hatirlatma));
    $$('#emOnizle [data-y]').forEach(x => x.addEventListener('click', () => yanit(kod, c.son ? c.son.id : 0, x.dataset.y)));
  }
  async function gonder(kodlar, kanal) {
    const j = await post('/EMutabakat/Gonder', { kodlar, kanal }); mesaj(j.success ? 'success' : 'warning', esc(j.message)); await yenile();
  }
  async function hatirlat(kod) { const j = await post('/EMutabakat/Hatirlat', { kod }); mesaj(j.success ? 'success' : 'warning', esc(j.message)); await yenile(); }
  async function yanit(kod, id, sonuc) {
    let aciklama = ''; if (sonuc === 'İtirazlı') { aciklama = prompt('İtiraz / fark açıklaması:'); if (!aciklama) return; } else if (!confirm(`${kod} için yanıt "Mutabık" olarak işlensin mi?`)) return;
    const j = await post('/EMutabakat/Yanit', { kod, gonderimId: id, sonuc, aciklama }); mesaj(j.success ? 'success' : 'warning', esc(j.message)); await yenile();
  }
  async function yenile() { V = await fetch('/EMutabakat/Veri').then(r => r.json()); doldur(); ciz(); gonderimler(); kisiler(); gunluk(); if (SECILI) onizle(SECILI); }
  function doldur() { const s = $('#emSirket'), v = s.value; s.innerHTML = '<option value="">Tümü</option>' + [...new Set(V.cariler.map(k => k.sirket))].sort().map(x => `<option>${esc(x)}</option>`).join(''); s.value = v; $('#emHatGun').textContent = V.hatirlatmaGunu; $('#emNI').textContent = V.cariler.reduce((s, k) => s + k.kisiler.length, 0); sablonDoldur(); }
  function gonderimler() {
    const ara = ($('#emGAra').value || '').toLocaleLowerCase('tr-TR'), so = $('#emGSonuc').value, ka = $('#emGKanal').value; const ad = {}; V.cariler.forEach(k => ad[k.kod] = k.kisaAd);
    const l = V.gonderimler.filter(g => (!so || g.sonuc === so) && (!ka || g.kanal === ka) && (!ara || [g.belgeNo, g.kod, ad[g.kod], g.kime, g.yanitAciklama].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    $('#emGonderimler tbody').innerHTML = l.map(g => `<tr><td><b>${esc(g.belgeNo)}</b></td><td>${esc(g.tarih)}<br><small class="text-muted">${g.gun} gün önce</small></td><td><b>${esc(g.kod)}</b> ${esc(ad[g.kod] || '')}</td><td class="s">${kanalRozet(g.kanal)}</td><td><small>${esc(g.kime)}</small></td><td><small>${esc(g.ozet)}</small></td><td class="e">${esc(g.tlTr)}</td><td class="s">${g.tekrar || '—'}</td><td>${rozet(g.sonuc, g.sonuc === 'Mutabık' ? 'ok' : g.sonuc === 'İtirazlı' ? 'hata' : 'bekle')}</td><td><small>${g.yanitTarihi ? esc(g.yanitTarihi) + '<br>' : ''}${esc(g.yanitAciklama)}</small></td><td><small>${esc(g.gonderen)}</small></td><td class="ci-git">${g.sonuc === 'Bekliyor' ? `<button type="button" data-h="${esc(g.kod)}" title="Hatırlat"><i class="fas fa-bell"></i></button><button type="button" data-y="Mutabık" data-kod="${esc(g.kod)}" data-id="${g.id}" title="Yanıt: Mutabık"><i class="fas fa-check"></i></button><button type="button" data-y="İtirazlı" data-kod="${esc(g.kod)}" data-id="${g.id}" title="Yanıt: İtiraz"><i class="fas fa-xmark"></i></button>` : ''}</td></tr>`).join('') || '<tr><td colspan="12" class="na-bos">Gönderim yok.</td></tr>';
    $('#emNG').textContent = V.gonderimler.length; gonderimler.son = l;
    $$('#emGonderimler [data-h]').forEach(b => b.addEventListener('click', () => hatirlat(b.dataset.h))); $$('#emGonderimler [data-y]').forEach(b => b.addEventListener('click', () => yanit(b.dataset.kod, +b.dataset.id, b.dataset.y)));
  }
  function kisiler() {
    const ara = ($('#emIAra').value || '').toLocaleLowerCase('tr-TR'), ek = $('#emIEksik').value; const l = [];
    V.cariler.forEach(k => k.kisiler.forEach(p => l.push({ ...p, kod: k.kod, kisaAd: k.kisaAd, anaYok: !k.kisiler.some(x => x.anaYetkili) })));
    const f = l.filter(p => (!ek || (ek === 'mail' ? !p.eposta : ek === 'cep' ? !p.cep : p.anaYok)) && (!ara || [p.kod, p.kisaAd, p.adSoyad, p.eposta, p.cep].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    $('#emKisiler tbody').innerHTML = f.map(p => `<tr data-kod="${esc(p.kod)}" data-id="${p.id}"><td><b>${esc(p.kod)}</b> ${esc(p.kisaAd)}</td><td><b>${esc(p.adSoyad || '—')}</b></td><td><small>${esc(p.unvan)} · ${esc(p.departman)}</small></td><td><input class="form-control form-control-sm emEp" value="${esc(p.eposta)}" placeholder="e-posta" style="font-size:.66rem;height:22px;width:220px"></td><td><input class="form-control form-control-sm emCep" value="${esc(p.cep)}" placeholder="05xx xxx xx xx" style="font-size:.66rem;height:22px;width:150px"></td><td class="s"><input type="checkbox" class="emAna" ${p.anaYetkili ? 'checked' : ''}></td><td class="ci-git"><button type="button" data-kaydet title="Kaydet"><i class="fas fa-floppy-disk"></i></button></td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Kayıt yok.</td></tr>';
    $$('#emKisiler [data-kaydet]').forEach(b => b.addEventListener('click', async () => { const r = b.closest('tr'); const j = await post('/EMutabakat/KisiGuncelle', { kod: r.dataset.kod, kisiId: +r.dataset.id, eposta: $('.emEp', r).value, cep: $('.emCep', r).value, anaYetkili: $('.emAna', r).checked }); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) await yenile(); }));
  }
  function sablonDoldur() { const s = V.sablon; $('#emSMailKonu').value = s.mailKonu; $('#emSMailGovde').value = s.mailGovde; $('#emSWaGovde').value = s.waGovde; $('#emSHatirlatma').value = s.hatirlatmaEk; $('#emAlanlar').innerHTML = V.alanlar.map(a => `<code>${esc(a)}</code>`).join(' · ') + '<br>Yanıt bağlantısı: <code>' + esc(V.yanitAdresi) + '…</code>'; }
  function gunluk() { $('#emLog tbody').innerHTML = (V.log || []).map(l => `<tr><td>${esc(l.zaman)}</td><td>${esc(l.kullanici)}</td><td>${esc(l.islem)}</td><td>${esc(l.yeni)}</td><td><small>${esc(l.aciklama)}</small></td></tr>`).join('') || '<tr><td colspan="5" class="na-bos">Kayıt yok.</td></tr>'; }
  function baslat() {
    ['#emAra', '#emTip', '#emSirket', '#emUygun', '#emDurum', '#emBakiye'].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    ['#emGAra', '#emGSonuc', '#emGKanal'].forEach(s => { $(s).addEventListener('input', gonderimler); $(s).addEventListener('change', gonderimler); }); ['#emIAra', '#emIEksik'].forEach(s => { $(s).addEventListener('input', kisiler); $(s).addEventListener('change', kisiler); });
    $('#emTemizle').addEventListener('click', () => { $('#emAra').value = ''; ['#emTip', '#emSirket', '#emUygun', '#emDurum', '#emBakiye'].forEach(s => $(s).value = ''); ciz(); });
    $$('#emSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    $('#emHepsi').addEventListener('change', e => { $$('.emSec').forEach(c => c.checked = e.target.checked); seciliSay(); });
    $('#emSeciliGonder').addEventListener('click', () => { const k = $$('.emSec:checked').map(c => c.value); if (confirm(`${k.length} cariye ${$('#emKanal').value} ile mutabakat gönderilsin mi?`)) gonder(k, $('#emKanal').value); });
    $('#emHatirlatHepsi').addEventListener('click', async () => { const k = V.cariler.filter(c => c.hatirlat); if (!k.length) { mesaj('info', 'Hatırlatma zamanı gelmiş bekleyen gönderim yok.'); return; } if (!confirm(`${k.length} cariye hatırlatma gönderilsin mi?`)) return; for (const c of k) await post('/EMutabakat/Hatirlat', { kod: c.kod }); mesaj('success', `${k.length} cariye hatırlatma gönderildi.`); await yenile(); });
    $('#emSenkron').addEventListener('click', async () => { const j = await post('/EMutabakat/YanitlariCek'); mesaj('info', esc(j.message)); await yenile(); });
    $('#emSablonKaydet').addEventListener('click', async () => { const j = await post('/EMutabakat/SablonKaydet', { mailKonu: $('#emSMailKonu').value, mailGovde: $('#emSMailGovde').value, waGovde: $('#emSWaGovde').value, hatirlatmaEk: $('#emSHatirlatma').value }); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) await yenile(); });
    $('#emGExcel').addEventListener('click', () => csvIndir('e_mutabakat_gonderimler', ['Belge No', 'Gönderim', 'Cari', 'Kanal', 'Kime', 'Özet', 'TL', 'Tekrar', 'Sonuç', 'Yanıt Tarihi', 'Yanıt', 'Gönderen'], (gonderimler.son || []).map(g => [g.belgeNo, g.tarih, g.kod, g.kanal, g.kime, g.ozet, g.tlKarsiligi, g.tekrar, g.sonuc, g.yanitTarihi, g.yanitAciklama, g.gonderen])));
    doldur(); ciz(); gonderimler(); kisiler(); gunluk(); const on = kok.dataset.kod; if (on) onizle(on);
  }
  fetch('/EMutabakat/Veri').then(r => r.json()).then(j => { V = j; baslat(); }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
