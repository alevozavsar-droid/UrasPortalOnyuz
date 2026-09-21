// ============================================================
// TALEP MOTORU EKRANI — Mesai / Belge / Seyahat / Araç (modül tanımı ve veriler /IkTalep/TmVeri?kod=… ile gelir)
// Bu dosya modülden bağımsızdır: form alanlarını, sistem hesabını, statüleri, olayları ve takip sütunlarını tanımdan çizer.
// ============================================================
(function () {
  'use strict';
  const kok = document.getElementById('tm'); if (!kok) return;
  const KOD = kok.dataset.kod;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const bugunIso = new Date().toISOString().slice(0, 10);
  const iso2tr = s => /^\d{4}-\d{2}-\d{2}$/.test(s || '') ? s.split('-').reverse().join('.') : (s ?? '');
  const PALET = ['#0ea5e9', '#6366f1', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6', '#0891b2', '#ec4899', '#64748b', '#334155'];
  let V = null, M = null, aktifNo = kok.dataset.no || '', onizlemeZ = null, takipOzel = null;

  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const talepRozet = d => rozet(d, d === 'Onaylandı' ? 'ok' : d === 'Reddedildi' || d === 'İptal Edildi' ? 'hata' : /Revizyon/.test(d) ? 'bilgi' : 'bekle');
  const durumRozet = d => d === '—' || !d ? '<span class="text-muted">—</span>' : rozet(d, /Kapandı|Tamamlandı|Gerçekleşti$|Aktarıldı|Eklendi|Teslim Alındı|İade Edildi|Hazır$|Elden Teslim|Gönderildi|Ödendi/.test(d) ? 'ok' : /İptal|gecikmiş|Gerçekleşmedi|Hasar|Ceza|SLA|Bekleniyor \(/.test(d) ? 'hata' : /Kullanımda|Seyahatte|Bugün|Kargoda|Planlandı|Hazırlanıyor/.test(d) ? 'bilgi' : 'bekle');
  const turRenk = kod => PALET[Math.max(0, M.turler.findIndex(t => t.kod === kod)) % PALET.length];
  const turRozet = t => `<span class="na-bs" style="background:${turRenk(t.turKod)}22;color:${turRenk(t.turKod)}">${esc(t.turAdi)}</span>`;
  const kilit = '<i class="fas fa-lock na-kilit" title="Kaynak sistemden gelir / sistem hesaplar — talep ekranından değiştirilemez"></i>';
  const kisi = pn => V.personel.find(p => p.personelNo === pn);
  const kisiAd = pn => (kisi(pn) || {}).adSoyad || (pn || '—');
  const alanGoster = (al, v) => { if (!v) return '—'; if (al.tip === 'checkbox') return v === 'true' ? 'Evet' : 'Hayır'; if (al.tip === 'date') return iso2tr(v); if (al.tip === 'personel') return kisiAd(v); if (al.tip === 'multiselect') return v.split('|').map(kisiAd).join(', '); if (al.tip === 'number') return Number(v).toLocaleString('tr-TR'); return v; };

  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 9000); }
  async function post(url, veri) { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }); return r.json(); }
  function guncelle(j) { if (j.talep) { const i = V.talepler.findIndex(x => x.no === j.talep.no); if (i >= 0) V.talepler[i] = j.talep; else V.talepler.unshift(j.talep); } if (j.kpiler) V.kpiler = j.kpiler; cizHepsi(); }
  const benimMi = t => t.olusturan.kullaniciId === V.ben.kullaniciId || t.kullanan.kullaniciId === V.ben.kullaniciId || (t.kisi2 && t.kisi2.kullaniciId === V.ben.kullaniciId);

  function sekme(p) { $$('#tmSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('.na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); try { history.replaceState(null, '', location.pathname + location.search + '#' + p + (p === 'detay' && aktifNo ? '/' + aktifNo : '')); } catch (e) { } }
  $$('#tmSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));

  // ---- başlık, kpi ----
  function cizBaslik() {
    document.documentElement.style.setProperty('--na-k', M.renk); kok.style.setProperty('--na-k', M.renk);
    $('#tmIkon').className = 'fas ' + M.ikon; $('#tmAd').textContent = M.ad; $('#tmAkisOzeti').innerHTML = '<i class="fas fa-route me-1"></i>' + esc(M.akisOzeti); $('#tmAciklama').textContent = M.aciklama;
    $('#tmListeD2').textContent = M.durum2Ad.replace(/\s*\(.*\)/, ''); $('#tmListeD3').textContent = M.durum3Ad.replace(/\s*\(.*\)/, '');
    $('#tmTakipBaslik').textContent = M.ad.replace(' Talebi', '') + ' Takip Raporu'; $('#tmKullananEtiket').textContent = M.kullananEtiket; $('#tmNoOnizleme').value = `Sistem üretir (${M.noOnEk}-${new Date().getFullYear()}-…)`;
    if (M.takvimSatir) { $('#tmTakvimSekme').style.display = ''; $('#tmTakvimAd').textContent = M.takvimAd; $('#tmTakvimBaslik').textContent = M.takvimAd; }
    $('#tmKuralListesi').innerHTML = M.kurallar.map(k => `<li>${esc(k)}</li>`).join('');
    $('#tmParametreler').innerHTML = Object.keys(M.parametreler).map(k => `<div class="na-param mb-1"><span>${esc(M.parametreEtiketler[k] || k)}:</span><input class="form-control" data-p="${k}" value="${esc(M.parametreler[k])}" style="width:90px"></div>`).join('') + '<button type="button" class="btn btn-light btn-sm py-0 mt-1" id="tmParamKaydet">Kaydet</button>';
    $('#tmParamKaydet').addEventListener('click', async () => { const d = { kod: KOD }; $$('#tmParametreler [data-p]').forEach(i => d['p_' + i.dataset.p] = i.value); const j = await post('/IkTalep/TmParametre', d); M.parametreler = j.parametreler; onizleme(); mesaj('success', 'Parametreler güncellendi.'); });
    if (M.master && M.master.filo) {
      $('#tmMasterKart').style.display = ''; $('#tmMasterBaslik').innerHTML = '<i class="fas fa-car me-1"></i>Araç Filosu';
      $('#tmMaster').innerHTML = `<table class="na-tablo tm-filo" data-dinamik="kapali"><thead><tr><th>Plaka</th><th>Araç</th><th>Tip</th><th class="s">Km</th><th>Durum</th></tr></thead><tbody>${M.master.filo.map(a => `<tr><td><b>${esc(a.plaka)}</b></td><td>${esc(a.marka)}<br><small class="text-muted">${a.kapasite} kişi · ${esc(a.yakit)}</small></td><td>${esc(a.tip)}</td><td class="s">${Number(a.km).toLocaleString('tr-TR')}</td><td>${rozet(a.durum, a.durum === 'Müsait' ? 'ok' : 'hata')}</td></tr>`).join('')}</tbody></table>`;
    }
  }
  function cizKpi() {
    $('#tmKpi').innerHTML = V.kpiler.map(k => `<div style="--k:${k.renk}" data-e="${k.etiket}"><div class="l">${esc(k.baslik)}</div><div class="v">${esc(k.deger)}</div><div class="s" title="${esc(k.alt)}">${esc(k.alt)}</div></div>`).join('');
    $$('#tmKpi > div').forEach(d => d.addEventListener('click', () => { const e = d.dataset.e; if (e === 'onay') { sekme('onay'); return; } if (e === 'musait') { sekme('takvim'); return; } $('#tmTakipAra').value = ''; takipOzel = t => t.etiketler.includes(e); cizTakip(); sekme('takip'); }));
    $('#tmNListe').textContent = V.talepler.filter(benimMi).length; $('#tmNOnay').textContent = V.talepler.filter(t => t.onaylar.some(o => o.durum === 'Onay Bekliyor')).length; $('#tmNTakip').textContent = V.talepler.length;
  }

  // ---- form ----
  const kisiOto = (k, baslik) => `<div class="h">${baslik}</div>` + (!k ? '<span class="text-muted">Kişi seçilmedi</span>' : `<span>Personel No <b>${esc(k.personelNo)}</b></span><span>Ad Soyad <b>${esc(k.adSoyad)}</b></span><span>Şirket <b>${esc(k.sirket)}</b></span><span>Departman <b>${esc(k.departman)}</b></span><span>Ünvan <b>${esc(k.unvan)}</b></span><span>Masraf Merkezi <b>${esc(k.masrafMerkezi)}</b></span><span>Yönetici <b>${esc((V.personel.find(p => p.rol === 'Yönetici' && p.departman === k.departman) || {}).adSoyad || '—')}</b></span><span>Aktif <b>${k.aktif ? 'Evet' : '<span class="text-danger">Hayır</span>'}</b></span>`);
  function alanHtml(al, deger, idOnEk) {
    const id = (idOnEk || 'a_') + al.ad, v = deger ?? al.varsayilan ?? '', z = al.zorunlu ? ' <span class="z">*</span>' : '';
    if (al.tip === 'checkbox') return `<div class="col-md-${al.col}"><div class="form-check ps-0 d-flex align-items-center gap-1" style="min-height:32px;margin-top:18px"><input class="form-check-input ms-0" type="checkbox" id="${id}" name="${al.ad}" ${v === 'true' ? 'checked' : ''}><label class="form-check-label" for="${id}">${esc(al.etiket)}</label></div></div>`;
    let ic;
    if (al.tip === 'select') ic = `<select class="form-select" id="${id}" name="${al.ad}" ${al.zorunlu ? 'required' : ''}><option value="">Seçiniz…</option>${(al.secenekler || []).map(s => `<option ${s === v ? 'selected' : ''}>${esc(s)}</option>`).join('')}</select>`;
    else if (al.tip === 'personel') ic = `<select class="form-select" id="${id}" name="${al.ad}" ${al.zorunlu ? 'required' : ''}><option value="">Seçiniz…</option>${V.personel.map(p => `<option value="${p.personelNo}" ${p.personelNo === v ? 'selected' : ''}>${esc(p.adSoyad)} — ${esc(p.departman)}${p.aktif ? '' : ' (pasif)'}</option>`).join('')}</select>`;
    else if (al.tip === 'multiselect') ic = `<select class="form-select" id="${id}" name="${al.ad}" multiple size="3">${V.personel.filter(p => p.aktif).map(p => `<option value="${p.personelNo}" ${v.split('|').includes(p.personelNo) ? 'selected' : ''}>${esc(p.adSoyad)} — ${esc(p.departman)}</option>`).join('')}</select>`;
    else if (al.tip === 'textarea') ic = `<textarea class="form-control" id="${id}" name="${al.ad}" rows="2" placeholder="${esc(al.ipucu || '')}" ${al.zorunlu ? 'required' : ''}>${esc(v)}</textarea>`;
    else ic = `<input class="form-control" type="${al.tip}" id="${id}" name="${al.ad}" value="${esc(v)}" placeholder="${esc(al.ipucu || '')}" ${al.zorunlu ? 'required' : ''} ${al.tip === 'number' ? 'min="0" step="any"' : ''}>`;
    return `<div class="col-md-${al.col}"><label class="form-label" for="${id}">${esc(al.etiket)}${z}</label>${ic}${al.ipucu && (al.tip === 'select' || al.tip === 'date' || al.tip === 'personel') ? `<div class="form-text">${esc(al.ipucu)}</div>` : ''}</div>`;
  }
  function formDegerleri(kok2, onEk) { const d = {}; M.alanlar.forEach(al => { const e = $('#' + (onEk || 'a_') + al.ad, kok2); if (!e) return; d[al.ad] = al.tip === 'checkbox' ? (e.checked ? 'true' : 'false') : al.tip === 'multiselect' ? Array.from(e.selectedOptions).map(o => o.value).join('|') : e.value; }); return d; }
  function formHazirla() {
    $('#tmKullanan').innerHTML = V.personel.map(p => `<option value="${p.personelNo}" ${p.personelNo === V.ben.personelNo ? 'selected' : ''}>${esc(p.adSoyad)} — ${esc(p.sirket)} / ${esc(p.departman)}${p.aktif ? '' : ' (pasif)'}</option>`).join('');
    $('#tmTur').innerHTML = M.turler.map(t => `<option value="${t.kod}">${esc(t.ad)}</option>`).join('');
    $('#tmAlanlar').innerHTML = M.alanlar.map(al => alanHtml(al)).join('');
    $('#tmOlusturan').innerHTML = kisiOto(V.ben, "Talebi oluşturan (İK'dan otomatik)");
    const n = new Date(); $('#tmTarih').value = n.toLocaleDateString('tr-TR') + ' ' + n.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    // varsayılan tarihler: bugün + 7 (hafta içi)
    const b = new Date(); b.setDate(b.getDate() + 7); while (b.getDay() === 0 || b.getDay() === 6) b.setDate(b.getDate() + 1);
    M.alanlar.filter(al => al.tip === 'date').forEach((al, i) => { const e = $('#a_' + al.ad); if (e && !e.value) { const d = new Date(b); if (i > 0) d.setDate(d.getDate() + 2); e.value = d.toISOString().slice(0, 10); } });
    $$('#tmAlanlar input, #tmAlanlar select, #tmAlanlar textarea').forEach(e => e.addEventListener('change', onizleme));
    kullananDegisti(); turDegisti();
  }
  function kullananDegisti() { if (!V) return; const k = kisi($('#tmKullanan').value); $('#tmKullananBilgi').innerHTML = kisiOto(k, "Seçilen personel (İK / ERP'den otomatik)"); const u = $('#tmKullananUyari'); u.className = 'na-uyari'; if (k && !k.aktif) { u.className = 'na-uyari kirmizi'; u.innerHTML = '<i class="fas fa-ban me-1"></i>Personel aktif değil: talep açılamaz.'; } onizleme(); }
  function turDegisti() { if (!V) return; const t = M.turler.find(x => x.kod === $('#tmTur').value); $('#tmTurBilgi').innerHTML = t ? `${esc(t.aciklama)} <span class="text-muted">· onay: ${t.akis.split(',').map(a => ({ Y: 'Yönetici', D: 'Direktör', I: 'İK', A: 'İdari İşler', F: 'Finans' }[a] || a)).join(' → ')}</span>` : ''; onizleme(); }
  function onizleme() {
    clearTimeout(onizlemeZ);
    onizlemeZ = setTimeout(async () => {
      if (!V) return;
      const d = Object.assign({ kod: KOD, kullanan: $('#tmKullanan').value, _tur: $('#tmTur').value }, formDegerleri($('#tmForm')));
      const j = await post('/IkTalep/TmOnizleme', d);
      $('#tmHesap').innerHTML = '<div class="h">Sistem hesabı (kilitli alanlar)</div>' + (j.hesap && Object.keys(j.hesap).some(k => M.hesapEtiketler[k]) ? Object.keys(M.hesapEtiketler).filter(k => j.hesap[k] != null).map(k => `<span>${esc(M.hesapEtiketler[k])} <b>${esc(j.hesap[k])}</b></span>`).join('') : '<span class="text-muted">Alanlar dolduruldukça hesaplanır.</span>');
      if (!j.success) { $('#tmKurallar').innerHTML = `<span class="text-danger"><i class="fas fa-circle-xmark me-1"></i>${esc(j.message)}</span>`; $('#tmAkis').innerHTML = ''; return; }
      $('#tmAkis').innerHTML = j.akis.map((a, i) => (i ? '<i class="fas fa-chevron-right"></i>' : '') + `<span title="${esc(a.kisi)}">${esc(a.adim.replace('Talep Oluşturma', 'Talep'))}</span>`).join('');
      $('#tmKurallar').innerHTML = j.uyarilar.length ? `<ul class="iz-uyari-liste">${j.uyarilar.map(u => `<li>${esc(u)}</li>`).join('')}</ul>` : '<span class="text-success"><i class="fas fa-circle-check me-1"></i>Kural ihlali yok</span>';
    }, 250);
  }
  $('#tmKullanan').addEventListener('change', kullananDegisti); $('#tmTur').addEventListener('change', turDegisti);
  $('#tmForm').addEventListener('reset', () => setTimeout(formHazirla, 0));
  $('#tmForm').addEventListener('submit', async e => {
    e.preventDefault(); if (!V) return;
    const f = e.target, d = Object.assign({ kod: KOD, kullanan: $('#tmKullanan').value, _tur: $('#tmTur').value }, formDegerleri(f));
    d.ekler = Array.from($('#tmEk').files || []).map(x => x.name).join('|');
    const eksik = $$('[required]', f).filter(x => !String(x.value).trim()); if (eksik.length) { eksik.forEach(x => x.classList.add('is-invalid')); mesaj('danger', 'Zorunlu alanları doldurun.'); eksik[0].focus(); return; }
    $('#tmGonder').disabled = true;
    try { const j = await post('/IkTalep/TmOlustur', d); if (!j.success) { mesaj('danger', esc(j.message)); return; } mesaj('success', '<i class="fas fa-circle-check me-1"></i>' + esc(j.message)); const r = await fetch('/IkTalep/TmVeri?kod=' + KOD); V = await r.json(); M = V.modul; f.reset(); detayAc(j.no); }
    finally { $('#tmGonder').disabled = false; }
  });
  $('#tmForm').addEventListener('input', e => e.target.classList.remove('is-invalid'));

  // ---- listeler ----
  function cizListe() {
    const l = V.talepler.filter(benimMi);
    $('#tmListe tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${t.no}"><td><b>${t.no}</b>${t.revizyon ? ' <small class="text-muted">rev.' + t.revizyon + '</small>' : ''}</td><td>${esc(t.kullanan.adSoyad)}</td><td>${turRozet(t)}</td><td>${esc(t.ozet)}</td><td>${talepRozet(t.talepDurumu)}</td><td>${durumRozet(t.durum2)}</td><td>${durumRozet(t.durum3)}</td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Talebiniz yok.</td></tr>';
    $$('#tmListe tr.tik').forEach(r => r.addEventListener('click', () => detayAc(r.dataset.no)));
  }
  const gunFarki = ddmmyyyy => { const [d, m, y] = ddmmyyyy.split('.'); return Math.max(0, Math.round((Date.now() - new Date(+y, m - 1, +d)) / 864e5)); };
  function cizOnay() {
    const l = V.talepler.filter(t => t.onaylar.some(o => o.durum === 'Onay Bekliyor'));
    $('#tmOnayListe tbody').innerHTML = l.map(t => { const a = t.onaylar.find(o => o.durum === 'Onay Bekliyor'); return `<tr class="tik" data-no="${t.no}"><td><b>${t.no}</b></td><td>${esc(t.kullanan.adSoyad)}</td><td>${esc(t.departman)}</td><td>${turRozet(t)}</td><td>${esc(t.ozet)}</td><td>${esc(a.adim)}</td><td>${esc(a.kisi)}</td><td class="s">${gunFarki(t.tarih)} g</td><td>${t.uyarilar.length ? `<span class="text-warning" title="${esc(t.uyarilar.join(' '))}"><i class="fas fa-triangle-exclamation"></i> ${t.uyarilar.length}</span>` : '<span class="text-success"><i class="fas fa-check"></i></span>'}</td><td><button type="button" class="btn btn-success btn-sm py-0" data-hizli="Onayla" data-no="${t.no}"><i class="fas fa-check"></i></button> <button type="button" class="btn btn-outline-danger btn-sm py-0" data-hizli="Reddet" data-no="${t.no}"><i class="fas fa-xmark"></i></button></td></tr>`; }).join('') || '<tr><td colspan="10" class="na-bos">Onay bekleyen talep yok.</td></tr>';
    $$('#tmOnayListe tr.tik').forEach(r => r.addEventListener('click', e => { if (!e.target.closest('button')) detayAc(r.dataset.no); }));
    $$('#tmOnayListe [data-hizli]').forEach(b => b.addEventListener('click', async () => { let not = ''; if (b.dataset.hizli === 'Reddet') { not = prompt('Red gerekçesi (zorunlu):') || ''; if (!not.trim()) return; } const j = await post('/IkTalep/TmOnay', { no: b.dataset.no, eylem: b.dataset.hizli, not }); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message || (b.dataset.no + ' ' + (b.dataset.hizli === 'Onayla' ? 'onaylandı.' : 'reddedildi.')))); }));
  }
  function sutunDeger(t, anahtar) {
    if (anahtar === 'no') return `<b>${t.no}</b>`; if (anahtar === 'kullanan') return esc(t.kullanan.adSoyad); if (anahtar === 'kisi2') return esc(t.kisi2 ? t.kisi2.adSoyad : (t.degerler[M.ikinciKisiAlan] ? kisiAd(t.degerler[M.ikinciKisiAlan]) : '—'));
    if (anahtar === 'departman') return esc(t.departman); if (anahtar === 'tur') return turRozet(t); if (anahtar === 'talepDurumu') return talepRozet(t.talepDurumu); if (anahtar === 'durum2') return durumRozet(t.durum2); if (anahtar === 'durum3') return durumRozet(t.durum3);
    if (anahtar.startsWith('d.')) { const ad = anahtar.slice(2), al = M.alanlar.find(x => x.ad === ad); return esc(al ? alanGoster(al, t.degerler[ad]) : (t.degerler[ad] || '')); }
    if (anahtar.startsWith('h.')) return esc(t.hesap[anahtar.slice(2)] ?? ''); if (anahtar.startsWith('e.')) return esc(t.ek[anahtar.slice(2)] ?? ''); if (anahtar.startsWith('onay.')) return esc(t.onay[anahtar.slice(5)] || '—');
    return '';
  }
  function cizTakip() {
    const ara = ($('#tmTakipAra').value || '').toLocaleLowerCase('tr-TR'), tf = $('#tmTakipTur').value, df = $('#tmTakipDurum').value, d2 = $('#tmTakipD2').value;
    const l = V.talepler.filter(t => (!ara || [t.no, t.kullanan.adSoyad, t.olusturan.adSoyad, t.turAdi, t.departman, t.ozet].join(' ').toLocaleLowerCase('tr-TR').includes(ara)) && (!tf || t.turKod === tf) && (!df || t.talepDurumu === df) && (!d2 || t.durum2 === d2) && (!takipOzel || takipOzel(t)));
    $('#tmTakip thead tr').innerHTML = M.takipSutunlar.map(s => `<th class="${/Km|Gün|sa\)|TL\)|Adet|\(sa\)/.test(s.baslik) ? 's' : ''}">${esc(s.baslik)}</th>`).join('');
    $('#tmTakip tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${t.no}">${M.takipSutunlar.map(s => `<td class="${/Km|Gün|sa\)|TL\)|Adet/.test(s.baslik) ? 's' : ''}">${sutunDeger(t, s.anahtar)}</td>`).join('')}</tr>`).join('') || `<tr><td colspan="${M.takipSutunlar.length}" class="na-bos">Kayıt yok.</td></tr>`;
    $('#tmTakipSayi').textContent = `${l.length} / ${V.talepler.length} kayıt`;
    $$('#tmTakip tr.tik').forEach(r => r.addEventListener('click', () => detayAc(r.dataset.no)));
  }
  ['#tmTakipAra', '#tmTakipTur', '#tmTakipDurum', '#tmTakipD2'].forEach(s => $(s).addEventListener('input', () => { takipOzel = null; cizTakip(); }));
  function takipFiltreleri() { const uniq = f => V.talepler.map(f).filter((v, i, a) => a.indexOf(v) === i); $('#tmTakipTur').innerHTML = '<option value="">Tür: tümü</option>' + M.turler.map(t => `<option value="${t.kod}">${esc(t.ad)}</option>`).join(''); $('#tmTakipDurum').innerHTML = '<option value="">Talep durumu: tümü</option>' + uniq(t => t.talepDurumu).map(x => `<option>${esc(x)}</option>`).join(''); $('#tmTakipD2').innerHTML = `<option value="">${esc(M.durum2Ad.replace(/\s*\(.*\)/, ''))}: tümü</option>` + uniq(t => t.durum2).map(x => `<option>${esc(x)}</option>`).join(''); }

  // ---- takvim ----
  let takvimAy = new Date(new Date().getFullYear(), new Date().getMonth(), 1);
  function cizTakvim() {
    if (!M.takvimSatir) return;
    const y = takvimAy.getFullYear(), m = takvimAy.getMonth(), gunSayisi = new Date(y, m + 1, 0).getDate(); $('#tmAyAd').textContent = takvimAy.toLocaleDateString('tr-TR', { month: 'long', year: 'numeric' });
    const gunler = Array.from({ length: gunSayisi }, (_, i) => new Date(y, m, i + 1)); const iso = d => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    const filtre = $('#tmTakvimFiltre').value; let satirlar;
    if (M.takvimSatir === 'arac') satirlar = M.master.filo.filter(a => !filtre || a.tip === filtre).map(a => ({ ad: a.plaka, alt: a.marka, ese: t => t.ek.plaka === a.plaka })).concat([{ ad: 'Tahsis bekleyen', alt: 'plaka atanmadı', ese: t => !t.ek.plaka }]);
    else satirlar = V.personel.filter(p => p.aktif && (!filtre || p.departman === filtre)).map(p => ({ ad: p.adSoyad, alt: p.departman, ese: t => t.kullanan.personelNo === p.personelNo }));
    const gecerli = V.talepler.filter(t => !t.iptal && t.talepDurumu !== 'Reddedildi');
    let h = '<thead><tr><th style="width:150px;text-align:left;padding-left:6px">' + (M.takvimSatir === 'arac' ? 'Araç' : 'Personel') + '</th>' + gunler.map(d => { const s = iso(d), hs = d.getDay() === 0 || d.getDay() === 6; return `<th class="${s === bugunIso ? 'bugun' : hs ? 'hs' : ''}">${d.getDate()}<br><small>${['Pz', 'Pt', 'Sa', 'Ça', 'Pe', 'Cu', 'Ct'][d.getDay()]}</small></th>`; }).join('') + '</tr></thead><tbody>';
    satirlar.forEach(r => { const ts = gecerli.filter(r.ese); h += `<tr><td class="ad">${esc(r.ad)}<br><small>${esc(r.alt)}</small></td>` + gunler.map(d => { const s = iso(d), hs = d.getDay() === 0 || d.getDay() === 6; const t = ts.find(x => (x.degerler[M.takvimBas] || '') <= s && (x.degerler[M.takvimBit] || x.degerler[M.takvimBas] || '') >= s); if (!t) return `<td class="${hs ? 'hs' : ''}"></td>`; return `<td class="${hs ? 'hs' : ''}"><span class="iz ${t.onaylandi ? '' : 'bekliyor'}" style="background:${turRenk(t.turKod)}" data-no="${t.no}" title="${esc(t.no + ' · ' + t.turAdi + ' · ' + t.ozet + ' · ' + t.talepDurumu)}"></span></td>`; }).join('') + '</tr>'; });
    $('#tmTakvim').innerHTML = h + '</tbody>';
    $('#tmLejant').innerHTML = M.turler.map(t => `<span style="--c:${turRenk(t.kod)}">${esc(t.ad)}</span>`).join('') + '<span style="--c:#f1f5f9">Hafta sonu</span><span style="--c:#cbd5e1">Çizgili: onay bekliyor</span>';
    $$('#tmTakvim .iz').forEach(s => s.addEventListener('click', () => detayAc(s.dataset.no)));
  }
  $('#tmAyOnce').addEventListener('click', () => { takvimAy = new Date(takvimAy.getFullYear(), takvimAy.getMonth() - 1, 1); cizTakvim(); });
  $('#tmAySonra').addEventListener('click', () => { takvimAy = new Date(takvimAy.getFullYear(), takvimAy.getMonth() + 1, 1); cizTakvim(); });
  $('#tmAyBugun').addEventListener('click', () => { takvimAy = new Date(new Date().getFullYear(), new Date().getMonth(), 1); cizTakvim(); });
  $('#tmTakvimFiltre').addEventListener('change', cizTakvim);

  // ---- detay ----
  function detayAc(no) { aktifNo = no; cizDetay(); sekme('detay'); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  // Dış betikler (grup-ici-sekmeler.js: Raporu / Muhasebe Kontrolü sekmeleri) satıra tıklanınca detayı burada açar
  window.TM_DETAY_AC = async no => { try { const r = await fetch('/IkTalep/TmVeri?kod=' + KOD); V = await r.json(); M = V.modul; cizHepsi(); } catch (e) { } detayAc(no); };
  function cizDetay() {
    const t = V.talepler.find(x => x.no === aktifNo), el = $('#tmDetay'); $('#tmDetayNo').textContent = t ? t.no : '—';
    if (!t) { el.innerHTML = '<div class="na-bos">Listeden bir talep seçin.</div>'; return; }
    const bekleyen = t.onaylar.find(o => o.durum === 'Onay Bekliyor');
    const adimSinif = d => d === 'Onaylandı' || d === 'Tamamlandı' ? 'ok' : d === 'Onay Bekliyor' || d === 'Revizyon' ? 'bekle' : d === 'Reddedildi' ? 'hata' : '';
    const kisiKart = (k, baslik, ek) => `<div class="na-kart h-100 mb-0"><div class="na-kart-b"><span>${baslik}</span><span class="not">İK</span></div><div class="na-kart-g na-kisi">${!k ? '<span class="na-bos">Belirtilmedi</span>' : `<span class="l">Personel No</span><span class="v">${esc(k.personelNo)}</span><span class="l">Ad Soyad</span><span class="v">${esc(k.adSoyad)}</span><span class="l">Şirket</span><span class="v">${esc(k.sirket)}</span><span class="l">Departman</span><span class="v">${esc(k.departman)}</span><span class="l">Ünvan</span><span class="v">${esc(k.unvan)}</span><span class="l">Masraf Merkezi</span><span class="v">${esc(k.masrafMerkezi)}</span>${ek || ''}`}</div></div>`;
    const basAlan = M.takvimBas || 'tarih'; const iptalEdilebilir = !t.iptal && t.talepDurumu !== 'Reddedildi' && Object.keys(t.ek).length === 0 && !(t.onaylandi && (t.degerler[basAlan] || '') <= bugunIso);
    const ehl = M.master && M.master.ehliyetler && t.kisi2 ? M.master.ehliyetler[t.kisi2.personelNo] : null;
    el.innerHTML = `
    <div class="na-kart">
      <div class="na-kart-b"><span><i class="fas fa-file-invoice me-1"></i>Talep Üst Bilgileri — <b>${t.no}</b>${t.revizyon ? ` <span class="not">revizyon ${t.revizyon}</span>` : ''}</span>
        <span class="d-flex gap-1 flex-wrap align-items-center">${turRozet(t)}${t.uyarilar.length ? `<span class="na-bs" style="background:#fef9c3;color:#713f12" title="${esc(t.uyarilar.join(' '))}"><i class="fas fa-triangle-exclamation me-1"></i>${t.uyarilar.length} kural uyarısı</span>` : ''}
        ${iptalEdilebilir ? '<button type="button" class="btn btn-outline-danger btn-sm py-0" id="tmIptalBtn"><i class="fas fa-ban me-1"></i>İptal Et</button>' : ''}<button type="button" class="btn btn-light btn-sm py-0" id="tmYazdir"><i class="fas fa-print me-1"></i>Yazdır</button></span></div>
      <div class="na-kart-g">
        <div class="na-ustbilgi"><div><div class="l">Talep No</div><div class="v">${t.no}</div></div><div><div class="l">Talep Tarihi / Saati</div><div class="v">${t.tarih} ${t.saat}</div></div><div><div class="l">Şirket</div><div class="v">${esc(t.sirket)}</div></div><div><div class="l">Departman</div><div class="v">${esc(t.departman)}</div></div><div><div class="l">Talebi Oluşturan</div><div class="v">${esc(t.olusturan.adSoyad)}</div></div><div><div class="l">Entegrasyon</div><div class="v">${rozet(t.entegrasyonDurumu, 'ok')} <small class="text-muted">son senkron ${t.sonSenkron || '—'}</small></div></div></div>
        <div class="na-durum3">
          <div><div class="l">Talep Durumu <small>(Workflow)</small></div><div class="v">${talepRozet(t.talepDurumu)}</div><div class="s">${bekleyen ? 'Sırada: ' + esc(bekleyen.kisi) : t.iptal ? esc(t.iptalNedeni) : t.onaylandi ? 'Onay tarihi ' + t.onayTarihi : ''}</div></div>
          <div><div class="l">${esc(M.durum2Ad)}</div><div class="v">${durumRozet(t.durum2)}</div><div class="s">${esc(t.ozet)}</div></div>
          <div><div class="l">${esc(M.durum3Ad)}</div><div class="v">${durumRozet(t.durum3)}</div><div class="s">${Object.keys(t.ek).filter(k => M.ekEtiketler[k]).slice(-2).map(k => esc(M.ekEtiketler[k]) + ' ' + esc(t.ek[k])).join(' · ') || 'kaynak sistem hareketi yok'}</div></div>
        </div>
        <div id="tmIptalForm" style="display:none;gap:8px;align-items:flex-end" class="mt-2 p-2 border rounded bg-light"><div class="flex-grow-1"><label class="form-label">İptal nedeni (zorunlu)</label><input class="form-control" id="tmIptalNeden"></div><button type="button" class="btn btn-danger btn-sm" id="tmIptalGonder">İptali Onayla</button><button type="button" class="btn btn-light btn-sm" id="tmIptalVazgec">Vazgeç</button></div>
      </div>
    </div>
    <div class="row g-2 mb-2"><div class="col-md-6">${kisiKart(t.kullanan, '<i class="fas fa-user-check me-1"></i>' + esc(M.kullananEtiket))}</div><div class="col-md-6">${M.ikinciKisiAlan ? kisiKart(t.kisi2, '<i class="fas fa-user-group me-1"></i>' + esc(M.ikinciKisiEtiket), ehl ? `<span class="l">Ehliyet</span><span class="v">${esc(ehl.sinif)} · ${esc(ehl.gecerlilik)} · ceza ${ehl.cezaPuani}</span>` : '') : kisiKart(t.olusturan, '<i class="fas fa-user-pen me-1"></i>Talebi Oluşturan')}</div></div>
    <div class="row g-2">
      <div class="col-lg-7">
        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-list-check me-1"></i>Talep Detayları</span>${t.talepDurumu === 'Revizyon Bekliyor' && benimMi(t) ? '<button type="button" class="btn btn-warning btn-sm py-0" id="tmRevizeAc"><i class="fas fa-pen me-1"></i>Revize Et</button>' : ''}</div>
          <div class="na-kart-g">
            <div class="na-ustbilgi" style="grid-template-columns:repeat(4,1fr)"><div><div class="l">Talep Türü</div><div class="v">${esc(t.turAdi)}</div></div>${M.alanlar.map(al => `<div ${al.tip === 'textarea' ? 'style="grid-column:span 3"' : ''}><div class="l">${esc(al.etiket)}</div><div class="${al.tip === 'textarea' ? '' : 'v'}">${esc(alanGoster(al, t.degerler[al.ad]))}</div></div>`).join('')}</div>
            ${Object.keys(M.hesapEtiketler).some(k => t.hesap[k] != null) ? `<div class="iz-hesap mt-2"><div class="h">Sistem hesabı ${kilit}</div>${Object.keys(M.hesapEtiketler).filter(k => t.hesap[k] != null).map(k => `<span>${esc(M.hesapEtiketler[k])} <b>${esc(t.hesap[k])}</b></span>`).join('')}</div>` : ''}
            ${t.uyarilar.length ? `<div class="na-uyari sari mt-2"><b>Kural uyarıları:</b><ul class="iz-uyari-liste">${t.uyarilar.map(u => `<li>${esc(u)}</li>`).join('')}</ul></div>` : ''}
            <div id="tmRevizeForm" style="display:none" class="mt-2 p-2 border rounded bg-light"><div class="row g-2"><div class="col-md-4"><label class="form-label">Talep Türü</label><select class="form-select" id="r__tur">${M.turler.map(x => `<option value="${x.kod}" ${x.kod === t.turKod ? 'selected' : ''}>${esc(x.ad)}</option>`).join('')}</select></div>${M.alanlar.map(al => alanHtml(al, t.degerler[al.ad], 'r_')).join('')}<div class="col-12 d-flex gap-1"><button type="button" class="btn btn-primary btn-sm" id="tmRevGonder">Revizyonu Gönder</button><button type="button" class="btn btn-light btn-sm" id="tmRevIptal">Vazgeç</button></div></div><div class="form-text">Değişiklikte sistem hesabı, kural kontrolleri ve onay akışı yeniden hesaplanır.</div></div>
          </div>
        </div>
        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-clipboard-check me-1"></i>Onay Süreci</span><span class="not">${t.onaylar.filter(o => o.sira > 1).map(o => o.adim.replace(' Onayı', '')).join(' + ')}</span></div>
          <div class="na-kart-g">
            ${t.onaylar.map(o => `<div class="na-adim ${adimSinif(o.durum)}"><span class="no">${o.sira}</span><div><div class="ad">${esc(o.adim)}</div><div class="k">${esc(o.kisi)}${o.not ? ' · <i>' + esc(o.not) + '</i>' : ''}</div></div><div class="sag">${o.durum === 'Onaylandı' || o.durum === 'Tamamlandı' ? rozet(o.durum, 'ok') : o.durum === 'Onay Bekliyor' ? rozet('Onay Bekliyor', 'bekle') : o.durum === 'Reddedildi' ? rozet('Reddedildi', 'hata') : o.durum === 'Revizyon' ? rozet('Revizyona Gönderildi', 'bilgi') : `<span class="text-muted">${esc(o.durum === 'Pasif' ? 'Önceki adım sonrası aktif' : o.durum)}</span>`}<div class="k">${o.tarih || ''}</div></div></div>`).join('')}
            ${bekleyen ? `<div class="d-flex gap-1 flex-wrap align-items-center mt-2 pt-2 border-top"><span class="na-mini me-1">Onaycı işlemi (${esc(bekleyen.kisi)} — örnek rol):</span><button type="button" class="btn btn-success btn-sm py-0" data-onay="Onayla"><i class="fas fa-check me-1"></i>Onayla</button><button type="button" class="btn btn-outline-danger btn-sm py-0" data-onay="Reddet"><i class="fas fa-xmark me-1"></i>Reddet</button><button type="button" class="btn btn-outline-warning btn-sm py-0" data-onay="Revizyon"><i class="fas fa-rotate-left me-1"></i>Revizyona Gönder</button><input class="form-control form-control-sm flex-grow-1" id="tmOnayNot" placeholder="Not / red gerekçesi (red ve revizyonda zorunlu)"></div>` : ''}
          </div>
        </div>
        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-database me-1"></i>Kaynak Sistem Verileri</span><span class="not">${esc(M.olaylar.map(o => o.kaynak).filter((v, i, a) => a.indexOf(v) === i).join(' · '))} ${kilit}</span></div>
          <div class="na-kart-g">${Object.keys(t.ek).filter(k => M.ekEtiketler[k]).length ? `<div class="na-ustbilgi" style="grid-template-columns:repeat(4,1fr)">${Object.keys(M.ekEtiketler).filter(k => t.ek[k] != null).map(k => `<div><div class="l">${esc(M.ekEtiketler[k])}</div><div class="v">${esc(t.ek[k])}</div></div>`).join('')}</div>` : '<div class="na-bos">Henüz kaynak sistem hareketi yok; onay sonrası ilgili birim ve sistemler bu alanı doldurur.</div>'}</div>
        </div>
      </div>
      <div class="col-lg-5">
        <div class="na-kart na-olay">
          <div class="na-kart-b"><span><i class="fas fa-satellite-dish me-1"></i>Kaynak Sistem Olayı (simülasyon)</span><span class="not">${esc(M.olaylar.map(o => o.kaynak).filter((v, i, a) => a.indexOf(v) === i).join(' · '))}</span></div>
          <div class="na-kart-g">
            <div class="na-mini mb-1">Gerçek kurulumda bu olaylar ilgili sistemlerden Talep No ile otomatik gelir; ekranda elle girilmez. Her olay benzersiz Transaction ID taşır; aynı ID ikinci kez yok sayılır.</div>
            <div class="row g-1 align-items-end">
              <div class="col-7"><label class="form-label">Olay</label><select class="form-select" id="tmOlayTur">${M.olaylar.map(o => `<option value="${o.kod}">${esc(o.ad)}</option>`).join('')}</select></div>
              <div class="col-5"><label class="form-label">Transaction ID</label><input class="form-control" id="tmOlayId" value="${'TX-' + Date.now().toString().slice(-6)}"></div>
              <div class="col-12" id="tmOlayGirdiler"><div class="row g-1"></div></div>
              <div class="col-12 na-mini" id="tmOlayAciklama"></div>
              <div class="col-12 d-flex gap-1 mt-1"><button type="button" class="btn btn-warning btn-sm" id="tmOlayGonder"><i class="fas fa-bolt me-1"></i>Olayı İşle</button><button type="button" class="btn btn-light btn-sm" id="tmOlayTekrar" title="Aynı Transaction ID ile tekrar gönderir; mükerrer koruma testi">Aynı ID ile tekrar gönder</button></div>
            </div>
          </div>
        </div>
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-timeline me-1"></i>Hareket Geçmişi / Timeline</span><span class="not">${t.zaman.length} hareket</span></div>
          <div class="na-kart-g"><ul class="na-zaman">${t.zaman.slice().reverse().map(z => `<li class="${/PDKS|SGK|Finans|Kasa|Filo|HGS|Acente/.test(z.kaynak) ? 'fin' : /Bordro|İK|İdari|ERP/.test(z.kaynak) ? 'muh' : z.kaynak === 'Sistem' ? 'sis' : 'wf'}"><span class="t">${z.tarih}</span>${esc(z.hareket)} <span class="k">· ${esc(z.kaynak)}</span></li>`).join('')}</ul></div></div>
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-paperclip me-1"></i>Belge ve Ekler</span><span class="not">${t.ekler.length} dosya</span></div>
          <div class="na-kart-g">${t.ekler.map(e => `<div class="na-ek"><i class="fas ${/pdf/.test(e.ad) ? 'fa-file-pdf' : /png|jpg/.test(e.ad) ? 'fa-file-image' : 'fa-file'}"></i><span>${esc(e.ad)}</span><span class="na-bs" style="background:#f1f5f9;color:#475569">${esc(e.tur)}</span><span class="m">${esc(e.boyut)} · ${e.tarih} · ${esc(e.yukleyen)}</span></div>`).join('') || '<div class="na-bos">Ek dosya yok.</div>'}</div></div>
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-shield-halved me-1"></i>Audit Log / Değişiklik Geçmişi</span><span class="not">${t.audit.length} kayıt</span></div>
          <div class="na-kart-g p-0"><div class="table-responsive" style="max-height:260px"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Saat</th><th>Tablo</th><th>Alan</th><th>Eski</th><th>Yeni</th><th>Kullanıcı</th><th>İşlem</th><th>Kaynak</th></tr></thead><tbody>${t.audit.map(x => `<tr><td>${x.tarih}</td><td>${x.saat}</td><td><small>${esc(x.tablo)}</small></td><td><small>${esc(x.alan)}</small></td><td>${esc(x.eski)}</td><td>${esc(x.yeni)}</td><td>${esc(x.kullanici)}</td><td>${esc(x.islemTipi)}</td><td>${esc(x.kaynakSistem)}</td></tr>`).join('')}</tbody></table></div></div></div>
      </div>
    </div>`;

    $('#tmYazdir').addEventListener('click', () => window.print());
    const ib = $('#tmIptalBtn'); if (ib) ib.addEventListener('click', () => { $('#tmIptalForm').style.display = 'flex'; $('#tmIptalNeden').focus(); });
    const iv = $('#tmIptalVazgec'); if (iv) iv.addEventListener('click', () => { $('#tmIptalForm').style.display = 'none'; });
    const ig = $('#tmIptalGonder'); if (ig) ig.addEventListener('click', async () => { const j = await post('/IkTalep/TmIptal', { no: t.no, neden: $('#tmIptalNeden').value }); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message || 'Talep iptal edildi.')); });
    $$('[data-onay]', el).forEach(bt => bt.addEventListener('click', async () => { const not = $('#tmOnayNot').value.trim(); if (bt.dataset.onay !== 'Onayla' && !not) { mesaj('danger', bt.dataset.onay === 'Reddet' ? 'Red gerekçesi zorunludur.' : 'Revizyon açıklaması zorunludur.'); $('#tmOnayNot').focus(); return; } const j = await post('/IkTalep/TmOnay', { no: t.no, eylem: bt.dataset.onay, not }); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message || (bt.dataset.onay + ' işlendi.'))); }));
    const ra = $('#tmRevizeAc'); if (ra) ra.addEventListener('click', () => { $('#tmRevizeForm').style.display = 'block'; });
    const ri = $('#tmRevIptal'); if (ri) ri.addEventListener('click', () => { $('#tmRevizeForm').style.display = 'none'; });
    const rg = $('#tmRevGonder'); if (rg) rg.addEventListener('click', async () => { const d = Object.assign({ no: t.no, _tur: $('#r__tur').value }, formDegerleri($('#tmRevizeForm'), 'r_')); const j = await post('/IkTalep/TmRevize', d); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message || 'Talep revize edildi; onay akışı yeniden başlatıldı.')); });
    // olay girdileri
    const olayTur = $('#tmOlayTur');
    function olayAlanlari() {
      const o = M.olaylar.find(x => x.kod === olayTur.value); $('#tmOlayAciklama').textContent = o ? o.aciklama : '';
      $('#tmOlayGirdiler .row').innerHTML = (o ? o.girdiler : []).map(g => {
        const [ad, etiket, tip] = g.split('|'); const id = 'g_' + ad;
        if (tip === 'filo') { const musait = M.master.filo.filter(a => a.durum === 'Müsait'); return `<div class="col-8"><label class="form-label">${esc(etiket)}</label><select class="form-select" id="${id}"><option value="">Otomatik (ilk müsait)</option>${musait.map(a => `<option>${esc(a.plaka)}</option>`).join('')}</select></div>`; }
        if (tip && tip.startsWith('select:')) return `<div class="col-6"><label class="form-label">${esc(etiket)}</label><select class="form-select" id="${id}">${tip.slice(7).split(';').map(s => `<option>${esc(s)}</option>`).join('')}</select></div>`;
        const vars = tip === 'date' ? bugunIso : tip === 'time' ? (ad === 'giris' ? (t.degerler.baslangicSaat || '08:30') : (t.degerler.bitisSaat || '18:00')) : '';
        return `<div class="col-6"><label class="form-label">${esc(etiket)}</label><input class="form-control" type="${tip === 'number' ? 'number' : tip === 'date' ? 'date' : tip === 'time' ? 'time' : 'text'}" id="${id}" value="${esc(vars)}" ${tip === 'number' ? 'step="any"' : ''}></div>`;
      }).join('');
    }
    olayTur.addEventListener('change', olayAlanlari); olayAlanlari();
    async function olayGonder(ayniId) {
      const o = olayTur.value, d = { no: t.no, olay: o, islemId: $('#tmOlayId').value.trim() };
      $$('#tmOlayGirdiler [id^=g_]').forEach(i => d[i.id] = i.value);
      const j = await post('/IkTalep/TmOlay', d); guncelle(j);
      mesaj(j.success ? 'success' : 'warning', j.success ? `<i class="fas fa-bolt me-1"></i>${o} işlendi; durumlar yeniden hesaplandı.` : esc(j.message));
      if (j.success && !ayniId) $('#tmOlayId').value = 'TX-' + Date.now().toString().slice(-6);
    }
    $('#tmOlayGonder').addEventListener('click', () => olayGonder(false)); $('#tmOlayTekrar').addEventListener('click', () => olayGonder(true));
  }

  function cizHepsi() { cizKpi(); cizListe(); cizOnay(); takipFiltreleri(); cizTakip(); cizTakvim(); cizDetay(); }

  fetch('/IkTalep/TmVeri?kod=' + KOD).then(r => r.json()).then(v => {
    V = v; M = v.modul; cizBaslik();
    if (M.takvimSatir === 'arac') $('#tmTakvimFiltre').innerHTML = '<option value="">Tüm araçlar</option>' + M.master.filo.map(a => a.tip).filter((x, i, a) => a.indexOf(x) === i).map(x => `<option>${esc(x)}</option>`).join('');
    else { const depler = V.personel.map(p => p.departman).filter((x, i, a) => a.indexOf(x) === i).sort((a, b) => a.localeCompare(b, 'tr')); $('#tmTakvimFiltre').innerHTML = '<option value="">Tüm departmanlar</option>' + depler.map(d => `<option ${d === V.ben.departman ? 'selected' : ''}>${esc(d)}</option>`).join(''); }
    formHazirla(); cizKpi(); cizListe(); cizOnay(); takipFiltreleri(); cizTakip(); cizTakvim();
    const h = decodeURIComponent(location.hash.slice(1)); let p = kok.dataset.sekme || 'yeni';
    if (h) { const [hp, hn] = h.split('/'); p = hp; if (hn) aktifNo = hn; }
    if (p === 'takvim' && !M.takvimSatir) p = 'takip';
    if (!aktifNo && p === 'detay') { const ilk = V.talepler.find(benimMi); if (ilk) aktifNo = ilk.no; }
    cizDetay(); sekme(p); window.NA_HAZIR = true;
  }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
