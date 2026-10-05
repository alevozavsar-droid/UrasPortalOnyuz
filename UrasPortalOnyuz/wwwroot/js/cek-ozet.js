// ============================================================
// VADE TARİHİNE GÖRE ÇEK ÖZETİ — ekran betiği (Views/CekOzet/Index.cshtml; veri /CekOzet/Veri; kaynak Data/CekOrnek.cs + Data/ButceOrnek.cs › Cekler)
// Sekmeler: Verilen Çekler (varsayılan) → Tahsile Gönderilen Çekler → Aylık Nakit Görünümü
// Ortak filtre: şirket (Tümü = konsolide), vade yılı, çek türü (verilen), durum (sekmeye göre verilen / alınan durumları), arama.
// Verilen matrisi: satır = cari (tür ara toplamlı, yıllık toplama göre azalan), sütun = Ocak…Aralık + 12 Aylık Toplam + Aylık Ortalama (toplam / 12) + % pay;
//   altta AYLIK TOPLAM TUTAR ve AYLIK ÇEK ADEDİ (şablondaki gibi). Hücre / satır → carinin çek listesi (ay vurgulu).
// Tahsil matrisi: satır = müşteri; yalnız bankaya ibraz edilmiş çekler (Tahsilde + Tahsil Edildi + Karşılıksız). Nakit: ay ay verilen ↔ tahsile gönderilen, net, kümülatif.
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Ödenecek", "Tahsil Edildi").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('co'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const tl = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const pct = v => v === null || v === undefined || !isFinite(v) ? '—' : (v * 100).toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%';
  const iso = d => d.toISOString().slice(0, 10);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const bos = '<span class="text-muted">—</span>';
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + iso(new Date()) + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 9000); }
  const TUR_SINIF = { 'Tedarikçi': 'tedarikci', 'Maaş': 'maas', 'Kira': 'kira', 'Sigorta ve Diğer': 'diger' };
  const turRozet = t => `<span class="co-tur ${TUR_SINIF[t] || 'diger'}">${esc(t)}</span>`;
  const vDurumRozet = d => rozet(d, d === 'Ödendi' ? 'ok' : d === 'İade Alındı' ? 'hata' : 'bekle');
  const aDurumRozet = d => rozet(d, d === 'Tahsil Edildi' ? 'ok' : d === 'Karşılıksız' || d === 'Riskli' ? 'hata' : d === 'Ciro Edildi' ? 'notr' : d === 'Tahsilde' ? 'bilgi' : 'bekle');
  const netHtml = v => `<span class="co-net ${v > 0.005 ? 'pozitif' : v < -0.005 ? 'negatif' : ''}">${v > 0 ? '+' : ''}${kisa(v)}</span>`;

  let V = null, F = { sirket: '', yil: +kok.dataset.yil, tur: '', durum: '', ara: '' }, acikCari = null, acikTip = 'verilen';
  const AY = () => V.aylar;
  const bugunAy = yil => yil === V.yil ? V.ay : 0;

  // ---- veri erişimi
  const aramaUyar = (c, alanlar) => { const a = F.ara.trim().toLocaleLowerCase('tr-TR'); return !a || alanlar.join(' ').toLocaleLowerCase('tr-TR').includes(a); };
  const verilenListe = () => V.verilen.filter(c => c.yil === F.yil && (!F.sirket || c.sirket === F.sirket) && (!F.tur || c.tur === F.tur) && (!F.durum || c.durum === F.durum) && aramaUyar(c, [c.cari, c.cariKod, c.no, c.banka, c.sirket]));
  const alinanListe = () => V.alinan.filter(c => c.yil === F.yil && (!F.sirket || c.sirket === F.sirket) && (!F.durum || c.durum === F.durum) && aramaUyar(c, [c.musteri, c.musteriKod, c.no, c.banka, c.sirket]));
  /// cari bazında 12 aylık matris: anahtar → { ad, kod, tur, aylar[12] (tutar), adet[12], toplam, adetToplam }
  function matris(liste, anahtar, ad, kod, tur) {
    const m = new Map();
    liste.forEach(c => { const k = anahtar(c); let r = m.get(k); if (!r) { r = { ad: ad(c), kod: kod(c), tur: tur ? tur(c) : '', sirketler: new Set(), aylar: new Array(12).fill(0), adet: new Array(12).fill(0), toplam: 0, adetToplam: 0 }; m.set(k, r); } r.aylar[c.ay - 1] += c.tutar; r.adet[c.ay - 1]++; r.toplam += c.tutar; r.adetToplam++; r.sirketler.add(c.sirket); });
    return [...m.values()].sort((a, b) => b.toplam - a.toplam);
  }

  // ---- sekmeler
  function sekme(p) {
    $$('#coSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('#co > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    durumSecenekleri(p);
    if (V) ciz(p);
    try { const u = new URL(location.href); if (p === 'verilen') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }
  $$('#coSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  const aktifSekme = () => ($('#coSekmeler button.aktif') || {}).dataset?.p || 'verilen';
  function ciz(p) { if (p === 'verilen') cizVerilen(); else if (p === 'tahsil') cizTahsil(); else cizNakit(); sayaclar(); }
  function hepsiniCiz() { ciz(aktifSekme()); if (acikCari && $('#coDrawer').classList.contains('acik')) drawerAc(acikCari, acikTip, null, true); }
  function durumSecenekleri(p) {
    const s = $('#coDurum'), mevcut = s.value, l = p === 'tahsil' ? V.alinanDurumlari : p === 'nakit' ? [] : V.durumlar;
    s.innerHTML = '<option value="">Tümü</option>' + l.map(d => `<option>${esc(d)}</option>`).join(''); s.value = l.includes(mevcut) ? mevcut : ''; F.durum = s.value; s.disabled = p === 'nakit';
    $('#coTur').disabled = p === 'tahsil';
  }

  // ---- veri
  async function yukle() {
    let r; try { r = await (await fetch('/CekOzet/Veri')).json(); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r;
    const ys = $('#coYil'); ys.innerHTML = V.yillar.map(y => `<option value="${y}">${y}</option>`).join(''); ys.value = String(F.yil);
    $('#coTur').innerHTML = '<option value="">Tümü</option>' + V.turler.map(t => `<option>${esc(t)}</option>`).join('');
    $('#coSirket').addEventListener('change', () => { F.sirket = $('#coSirket').value; hepsiniCiz(); });
    ys.addEventListener('change', () => { F.yil = +ys.value; hepsiniCiz(); });
    $('#coTur').addEventListener('change', () => { F.tur = $('#coTur').value; hepsiniCiz(); });
    $('#coDurum').addEventListener('change', () => { F.durum = $('#coDurum').value; hepsiniCiz(); });
    $('#coAra').addEventListener('input', () => { F.ara = $('#coAra').value; hepsiniCiz(); });
    $('#coTemizle').addEventListener('click', () => { F = { sirket: '', yil: V.yil, tur: '', durum: '', ara: '' }; $('#coSirket').value = ''; ys.value = String(F.yil); $('#coTur').value = ''; $('#coDurum').value = ''; $('#coAra').value = ''; hepsiniCiz(); });
    $('#coExcel').addEventListener('click', () => { const p = aktifSekme(); if (p === 'verilen') verilenCsv(); else if (p === 'tahsil') tahsilCsv(); else nakitCsv(); });
    $('#coVExcel').addEventListener('click', verilenCsv); $('#coTExcel').addEventListener('click', tahsilCsv); $('#coNExcel').addEventListener('click', nakitCsv);
    $('#coKpi').addEventListener('click', e => { const k = e.target.closest('[data-durum]'); if (!k) return; F.durum = F.durum === k.dataset.durum ? '' : k.dataset.durum; $('#coDurum').value = F.durum; hepsiniCiz(); });
    $('#coTKpi').addEventListener('click', e => { const k = e.target.closest('[data-durum]'); if (!k) return; F.durum = F.durum === k.dataset.durum ? '' : k.dataset.durum; $('#coDurum').value = F.durum; hepsiniCiz(); });
    $('#coDrawerKapat').addEventListener('click', drawerKapat); $('#coPerde').addEventListener('click', drawerKapat);
  }
  function sayaclar() {
    $('#coNVerilen').textContent = V.verilen.filter(c => c.yil === F.yil && (!F.sirket || c.sirket === F.sirket)).length;
    $('#coNTahsil').textContent = V.alinan.filter(c => c.yil === F.yil && c.tahsile && (!F.sirket || c.sirket === F.sirket)).length;
    const kapsam = (F.sirket || 'Tüm Şirketler (Konsolide)') + ' · ' + F.yil;
    $('#coVKapsam').textContent = kapsam; $('#coTKapsam').textContent = kapsam; $('#coNKapsam').textContent = kapsam;
    $('#coVBaslik').textContent = F.yil; $('#coTBaslik').textContent = F.yil; $('#coNBaslik').textContent = F.yil;
  }
  const basliklar = (ilk, ekstra) => `<tr><th class="ad">${ilk}</th>${AY().map((a, i) => `<th class="s ${i + 1 === bugunAy(F.yil) ? 'bugun-ay' : ''}">${a}</th>`).join('')}${ekstra}</tr>`;
  // Özet sütunları (sağa sabit): 12 Aylık Toplam = ayların toplamı · Aylık Ortalama = toplam / 12 (şablondaki gibi) · % = satır toplamı / genel toplam
  const OZET_BASLIK = '<th class="s sag sag3">12 Aylık Toplam</th><th class="s sag sag2">Aylık Ortalama</th><th class="s sag sag1">%</th>';
  const ozetHucreler = (toplam, genel) => `<td class="s yil sag sag3">${kisa(toplam)}</td><td class="s ort sag sag2">${kisa(toplam / 12)}</td><td class="s sag sag1">${pct(genel ? toplam / genel : 0)}</td>`;

  // ---- VERİLEN ÇEKLER
  function cizVerilen() {
    const L = verilenListe(), M = matris(L, c => c.tur + '|' + c.cari + '|' + c.cariKod, c => c.cari, c => c.cariKod, c => c.tur);
    const genel = M.reduce((s, r) => s + r.toplam, 0), ayTop = new Array(12).fill(0), ayAdet = new Array(12).fill(0);
    M.forEach(r => { for (let i = 0; i < 12; i++) { ayTop[i] += r.aylar[i]; ayAdet[i] += r.adet[i]; } });
    const bA = bugunAy(F.yil), yilL = V.verilen.filter(c => c.yil === F.yil && (!F.sirket || c.sirket === F.sirket));
    const odenecek = yilL.filter(c => c.durum === 'Ödenecek').reduce((s, c) => s + c.tutar, 0), odendi = yilL.filter(c => c.durum === 'Ödendi').reduce((s, c) => s + c.tutar, 0), iade = yilL.filter(c => c.durum === 'İade Alındı').reduce((s, c) => s + c.tutar, 0);
    const buAy = bA ? yilL.filter(c => c.ay === bA).reduce((s, c) => s + c.tutar, 0) : 0, enYuksek = ayTop.indexOf(Math.max(...ayTop));
    const turT = t => yilL.filter(c => c.tur === t).reduce((s, c) => s + c.tutar, 0);
    $('#coKpi').innerHTML = [
      { l: 'Yıllık Verilen Çek', v: kisa(yilL.reduce((s, c) => s + c.tutar, 0)), s: `${yilL.length} çek · ${new Set(yilL.map(c => c.cari)).size} cari`, k: '#0369a1' },
      { l: 'Ödendi', v: kisa(odendi), s: 'vadesi geçmiş, banka ödedi', k: '#15803d', d: 'Ödendi' }, { l: 'Ödenecek', v: kisa(odenecek), s: 'vadesi gelmemiş · tıkla: filtre', k: '#b45309', d: 'Ödenecek' }, { l: 'İade Alındı', v: kisa(iade), s: 'geri alınan / iptal', k: '#dc2626', d: 'İade Alındı' },
      { l: bA ? `Bu Ay Vadesi Gelen (${AY()[bA - 1]})` : 'Bu Ay Vadesi Gelen', v: bA ? kisa(buAy) : '—', s: bA ? `${yilL.filter(c => c.ay === bA).length} çek` : 'seçili yıl bu yıl değil', k: '#7c3aed' },
      { l: 'En Yüksek Ay', v: genel ? AY()[enYuksek] : '—', s: genel ? kisa(ayTop[enYuksek]) + ' TL' : '—', k: '#0f172a' },
      { l: 'Maaş Çekleri', v: kisa(turT('Maaş')), s: `kira ${kisa(turT('Kira'))}`, k: '#0f766e' }, { l: 'Tedarikçi Çekleri', v: kisa(turT('Tedarikçi')), s: `sigorta ve diğer ${kisa(turT('Sigorta ve Diğer'))}`, k: '#1d4ed8' }
    ].map(k => `<div style="--k:${k.k};${k.d && F.durum === k.d ? 'background:#f0f9ff' : ''}" ${k.d ? `data-durum="${k.d}" title="Durum filtresi"` : ''}><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
    const t = $('#coVerilenTablo');
    t.tHead.innerHTML = basliklar('Verilen · Cari', OZET_BASLIK);
    const satir = r => `<tr class="tik" data-cari="${esc(r.ad)}" data-tur="${esc(r.tur)}"><td class="ad"><b>${esc(r.ad)}</b>${r.kod ? `<small>${esc(r.kod)}${F.sirket ? '' : ' · ' + esc([...r.sirketler].join(', '))}</small>` : F.sirket ? '' : `<small>${esc([...r.sirketler].join(', '))}</small>`}</td>${r.aylar.map((v, i) => `<td class="s hucre ${i + 1 === bA ? 'bugun-ay' : ''}" data-ay="${i + 1}">${v ? kisa(v) + `<small>${r.adet[i]} çek</small>` : bos}</td>`).join('')}${ozetHucreler(r.toplam, genel)}</tr>`;
    const grup = (ad, l) => { const a = new Array(12).fill(0); let top = 0; l.forEach(r => { for (let i = 0; i < 12; i++) a[i] += r.aylar[i]; top += r.toplam; }); return `<tr class="grp"><td class="ad">${esc(ad)} <small style="display:inline;text-transform:none;font-weight:600">· ${l.length} cari</small></td>${a.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}">${v ? kisa(v) : ''}</td>`).join('')}${ozetHucreler(top, genel)}</tr>`; };
    const turler = V.turler.filter(x => M.some(r => r.tur === x));
    t.tBodies[0].innerHTML = M.length ? turler.map(x => { const l = M.filter(r => r.tur === x); return grup(x, l) + l.map(satir).join(''); }).join('') : '<tr><td colspan="16" class="na-bos">Filtreye uyan çek yok.</td></tr>';
    t.tFoot.innerHTML = M.length ? `<tr class="toplam"><td class="ad">AYLIK TOPLAM TUTAR</td>${ayTop.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}">${v ? kisa(v) : '—'}</td>`).join('')}${ozetHucreler(genel, genel)}</tr>
      <tr class="adet"><td class="ad">AYLIK ÇEK ADEDİ</td>${ayAdet.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}">${v || '—'}</td>`).join('')}<td class="s sag sag3">${ayAdet.reduce((s, v) => s + v, 0)}</td><td class="s sag sag2">${(ayAdet.reduce((s, v) => s + v, 0) / 12).toLocaleString('tr-TR', { maximumFractionDigits: 1 })}</td><td class="sag sag1"></td></tr>` : '';
    $$('tr.tik', t).forEach(r => r.addEventListener('click', e => { const h = e.target.closest('td.hucre'); drawerAc(r.dataset.cari, 'verilen', h ? +h.dataset.ay : null); }));
    $('#coVNot').textContent = `${F.sirket || 'konsolide'} · ${F.yil} · ${M.length} cari · ${L.length} çek · ${kisa(genel)} TL`;
  }
  function verilenCsv() {
    const M = matris(verilenListe(), c => c.tur + '|' + c.cari + '|' + c.cariKod, c => c.cari, c => c.cariKod, c => c.tur), genel = M.reduce((s, r) => s + r.toplam, 0);
    csvIndir('verilen_cek_vade_' + F.yil, ['Tür', 'Cari', 'Cari Kodu', ...AY(), '12 Aylık Toplam', 'Aylık Ortalama', '%', 'Çek Adedi'], M.map(r => [r.tur, r.ad, r.kod, ...r.aylar.map(v => v ? sayi(v) : ''), sayi(r.toplam), sayi(r.toplam / 12), genel ? (r.toplam / genel * 100).toFixed(2).replace('.', ',') : '', r.adetToplam]));
  }

  // ---- TAHSİLE GÖNDERİLEN ÇEKLER
  function cizTahsil() {
    const yilL = V.alinan.filter(c => c.yil === F.yil && (!F.sirket || c.sirket === F.sirket));
    const top = d => yilL.filter(c => c.durum === d).reduce((s, c) => s + c.tutar, 0), gonderilen = yilL.filter(c => c.tahsile);
    $('#coTKpi').innerHTML = [
      { l: 'Tahsile Gönderilen', v: kisa(gonderilen.reduce((s, c) => s + c.tutar, 0)), s: `${gonderilen.length} çek · bankaya ibraz`, k: '#0369a1' },
      { l: 'Tahsil Edildi', v: kisa(top('Tahsil Edildi')), s: 'hesaba geçti', k: '#15803d', d: 'Tahsil Edildi' }, { l: 'Tahsilde', v: kisa(top('Tahsilde')), s: 'bankada vade bekliyor', k: '#2563eb', d: 'Tahsilde' },
      { l: 'Karşılıksız', v: kisa(top('Karşılıksız')), s: 'gönderildi, ödenmedi', k: '#dc2626', d: 'Karşılıksız' }, { l: 'Portföyde', v: kisa(top('Portföyde')), s: 'henüz gönderilmedi', k: '#b45309', d: 'Portföyde' },
      { l: 'Ciro Edildi', v: kisa(top('Ciro Edildi')), s: 'tedarikçiye verildi', k: '#6b7280', d: 'Ciro Edildi' }, { l: 'Riskli', v: kisa(top('Riskli')), s: 'müşteri riski · tıkla: filtre', k: '#9d174d', d: 'Riskli' }
    ].map(k => `<div style="--k:${k.k};${k.d && F.durum === k.d ? 'background:#f0f9ff' : ''}" ${k.d ? `data-durum="${k.d}" title="Durum filtresi"` : ''}><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
    // durum filtresi yoksa yalnız tahsile gönderilenler; filtre varsa o durum
    const L = alinanListe().filter(c => F.durum ? true : c.tahsile), M = matris(L, c => c.musteri + '|' + c.musteriKod, c => c.musteri, c => c.musteriKod, null);
    const genel = M.reduce((s, r) => s + r.toplam, 0), ayTop = new Array(12).fill(0), ayAdet = new Array(12).fill(0), bA = bugunAy(F.yil);
    M.forEach(r => { for (let i = 0; i < 12; i++) { ayTop[i] += r.aylar[i]; ayAdet[i] += r.adet[i]; } });
    const t = $('#coTahsilTablo');
    t.tHead.innerHTML = basliklar('Müşteri', OZET_BASLIK);
    t.tBodies[0].innerHTML = M.length ? M.map(r => `<tr class="tik" data-cari="${esc(r.ad)}"><td class="ad"><b>${esc(r.ad)}</b><small>${esc(r.kod)}${F.sirket ? '' : ' · ' + esc([...r.sirketler].join(', '))}</small></td>${r.aylar.map((v, i) => `<td class="s hucre ${i + 1 === bA ? 'bugun-ay' : ''}" data-ay="${i + 1}">${v ? kisa(v) + `<small>${r.adet[i]} çek</small>` : bos}</td>`).join('')}${ozetHucreler(r.toplam, genel)}</tr>`).join('') : '<tr><td colspan="16" class="na-bos">Filtreye uyan çek yok.</td></tr>';
    t.tFoot.innerHTML = M.length ? `<tr class="toplam"><td class="ad">AYLIK TOPLAM TUTAR</td>${ayTop.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}">${v ? kisa(v) : '—'}</td>`).join('')}${ozetHucreler(genel, genel)}</tr>
      <tr class="adet"><td class="ad">AYLIK ÇEK ADEDİ</td>${ayAdet.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}">${v || '—'}</td>`).join('')}<td class="s sag sag3">${ayAdet.reduce((s, v) => s + v, 0)}</td><td class="s sag sag2">${(ayAdet.reduce((s, v) => s + v, 0) / 12).toLocaleString('tr-TR', { maximumFractionDigits: 1 })}</td><td class="sag sag1"></td></tr>` : '';
    $$('tr.tik', t).forEach(r => r.addEventListener('click', e => { const h = e.target.closest('td.hucre'); drawerAc(r.dataset.cari, 'alinan', h ? +h.dataset.ay : null); }));
    $('#coTNot').textContent = `${F.sirket || 'konsolide'} · ${F.yil} · ${F.durum || 'tahsile gönderilen (tahsil edildi + tahsilde + karşılıksız)'} · ${M.length} müşteri · ${L.length} çek · ${kisa(genel)} TL`;
  }
  function tahsilCsv() {
    const L = alinanListe().filter(c => F.durum ? true : c.tahsile), M = matris(L, c => c.musteri + '|' + c.musteriKod, c => c.musteri, c => c.musteriKod, null), genel = M.reduce((s, r) => s + r.toplam, 0);
    csvIndir('tahsile_gonderilen_cek_' + F.yil, ['Müşteri', 'Müşteri Kodu', ...AY(), '12 Aylık Toplam', 'Aylık Ortalama', '%', 'Çek Adedi'], M.map(r => [r.ad, r.kod, ...r.aylar.map(v => v ? sayi(v) : ''), sayi(r.toplam), sayi(r.toplam / 12), genel ? (r.toplam / genel * 100).toFixed(2).replace('.', ',') : '', r.adetToplam]));
  }

  // ---- AYLIK NAKİT GÖRÜNÜMÜ
  function nakitSatirlari() {
    const vL = V.verilen.filter(c => c.yil === F.yil && (!F.sirket || c.sirket === F.sirket) && c.durum !== 'İade Alındı'), aL = V.alinan.filter(c => c.yil === F.yil && (!F.sirket || c.sirket === F.sirket));
    const ay = (l, f) => { const a = new Array(12).fill(0); l.forEach(c => { if (!f || f(c)) a[c.ay - 1] += c.tutar; }); return a; };
    const adet = (l, f) => { const a = new Array(12).fill(0); l.forEach(c => { if (!f || f(c)) a[c.ay - 1]++; }); return a; };
    const r = [];
    V.turler.forEach(t => r.push({ ad: 'Verilen · ' + t, a: ay(vL, c => c.tur === t), cls: '' }));
    const verilen = ay(vL); r.push({ ad: 'VERİLEN ÇEK TOPLAMI', a: verilen, cls: 'toplam', adet: adet(vL) });
    const tahsil = ay(aL, c => c.tahsile);
    r.push({ ad: 'Tahsile Gönderilen · Tahsil Edildi', a: ay(aL, c => c.durum === 'Tahsil Edildi'), cls: '' }, { ad: 'Tahsile Gönderilen · Tahsilde', a: ay(aL, c => c.durum === 'Tahsilde'), cls: '' }, { ad: 'Tahsile Gönderilen · Karşılıksız', a: ay(aL, c => c.durum === 'Karşılıksız'), cls: '' });
    r.push({ ad: 'TAHSİLE GÖNDERİLEN TOPLAMI', a: tahsil, cls: 'toplam', adet: adet(aL, c => c.tahsile) });
    r.push({ ad: 'Portföyde Bekleyen (Gönderilmedi)', a: ay(aL, c => c.durum === 'Portföyde'), cls: 'adet' }, { ad: 'Ciro Edilen', a: ay(aL, c => c.durum === 'Ciro Edildi'), cls: 'adet' });
    const net = tahsil.map((v, i) => v - verilen[i]); let k = 0; const kum = net.map(v => k += v);
    r.push({ ad: 'NET (Tahsile Gönderilen − Verilen)', a: net, cls: 'toplam', net: true }, { ad: 'Kümülatif Net', a: kum, cls: '', net: true });
    return r;
  }
  function cizNakit() {
    const R = nakitSatirlari(), bA = bugunAy(F.yil), t = $('#coNakitTablo');
    t.tHead.innerHTML = basliklar('Kalem', '<th class="s sag sag1 genis">Yıl Toplamı</th>');
    t.tBodies[0].innerHTML = R.map(r => `<tr class="${r.cls}"><td class="ad">${esc(r.ad)}</td>${r.a.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}">${r.net ? netHtml(v) : v ? kisa(v) + (r.adet ? `<small>${r.adet[i]} çek</small>` : '') : '—'}</td>`).join('')}<td class="s yil sag sag1 genis">${r.net ? netHtml(r.ad.startsWith('Küm') ? r.a[11] : r.a.reduce((s, v) => s + v, 0)) : kisa(r.a.reduce((s, v) => s + v, 0))}</td></tr>`).join('');
  }
  function nakitCsv() { csvIndir('aylik_nakit_gorunumu_' + F.yil, ['Kalem', ...AY(), 'Yıl Toplamı'], nakitSatirlari().map(r => [r.ad, ...r.a.map(v => sayi(v)), sayi(r.ad.startsWith('Küm') ? r.a[11] : r.a.reduce((s, v) => s + v, 0))])); }

  // ---- DETAY (drawer): carinin çekleri (ay vurgulu)
  function drawerAc(cari, tip, ay, sessiz) {
    acikCari = cari; acikTip = tip;
    const verilen = tip === 'verilen';
    const L = (verilen ? V.verilen.filter(c => c.cari === cari) : V.alinan.filter(c => c.musteri === cari)).filter(c => c.yil === F.yil && (!F.sirket || c.sirket === F.sirket)).sort((a, b) => a.ay - b.ay || a.vade.localeCompare(b.vade));
    if (!L.length) return;
    const kod = verilen ? L[0].cariKod : L[0].musteriKod, tur = verilen ? L[0].tur : '', toplam = L.reduce((s, c) => s + c.tutar, 0);
    const aylik = new Array(12).fill(0); L.forEach(c => aylik[c.ay - 1] += c.tutar);
    const durumlar = [...new Set(L.map(c => c.durum))].map(d => `${d}: ${kisa(L.filter(c => c.durum === d).reduce((s, c) => s + c.tutar, 0))}`).join(' · ');
    $('#coDrawerBaslik').innerHTML = `${esc(cari)} ${verilen ? turRozet(tur) : rozet('Müşteri Çeki', 'bilgi')} ${kod ? `<span class="co-pill">${esc(kod)}</span>` : '<span class="co-pill" title="Cari kartı tanımlı değil">Kart Yok</span>'}`;
    $('#coDrawerAksiyon').innerHTML = `${kod ? `<span class="btn btn-light btn-sm" data-url="/CariKart/${verilen ? 'Tedarikci' : 'Musteri'}?kod=${esc(kod)}" data-baslik="${verilen ? 'Tedarikçi' : 'Müşteri'} Cari Kartları" style="font-size:.66rem"><i class="fas fa-address-card me-1"></i>Cari Kartı Aç</span>` : ''}<button type="button" class="btn btn-light btn-sm" id="coDExcel" style="font-size:.66rem"><i class="fas fa-file-excel me-1"></i>Excel</button>`;
    $('#coDrawerIc').innerHTML = `
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-circle-info me-1"></i>${verilen ? 'Verilen Çek Özeti' : 'Alınan Çek Özeti'}</span><span class="not">${esc(F.sirket || 'konsolide')} · ${F.yil}</span></div><div class="na-kart-g">
        <div class="co-ozet"><div><div class="l">12 Aylık Toplam</div><div class="v">${kisa(toplam)} TL</div></div><div><div class="l">Çek Adedi</div><div class="v">${L.length}</div></div><div><div class="l">Aylık Ortalama</div><div class="v">${kisa(toplam / 12)} TL</div></div><div><div class="l">Durum Dağılımı</div><div class="v" style="font-weight:500">${esc(durumlar)}</div></div></div>
        <div class="table-responsive mt-2"><table class="na-tablo co-excel" data-dinamik="kapali"><thead><tr>${AY().map((a, i) => `<th class="s ${i + 1 === ay ? 'bugun-ay' : ''}">${a.slice(0, 3)}</th>`).join('')}</tr></thead><tbody><tr>${aylik.map((v, i) => `<td class="s ${i + 1 === ay ? 'bugun-ay' : ''}">${v ? kisa(v) : '—'}</td>`).join('')}</tr></tbody></table></div>
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-list me-1"></i>Çek Listesi (${L.length})</span><span class="not">${ay ? AY()[ay - 1] + ' vurgulu · ' : ''}vade sırasıyla</span></div><div class="na-kart-g p-0"><div class="table-responsive" style="max-height:60vh"><table class="na-tablo" id="coDCekler" data-dinamik="kapali"><thead><tr><th>Çek No</th><th>Şirket</th><th>Banka</th>${verilen ? '<th>Keşide</th>' : ''}<th>Vade</th><th class="s">Tutar</th><th>Durum</th>${verilen ? '<th>Açıklama</th>' : ''}</tr></thead><tbody>
        ${L.map(c => `<tr class="${c.ay === ay ? 'vurgu' : ''}"><td><b>${esc(c.no)}</b></td><td>${esc(c.sirket)}</td><td>${esc(c.banka)}</td>${verilen ? `<td>${esc(c.keside)}</td>` : ''}<td>${esc(c.vade)}</td><td class="s">${kisa(c.tutar)}</td><td>${verilen ? vDurumRozet(c.durum) : aDurumRozet(c.durum)}</td>${verilen ? `<td class="ad"><small>${esc(c.aciklama || '')}</small></td>` : ''}</tr>`).join('')}
      </tbody><tfoot><tr><td colspan="${verilen ? 5 : 4}" class="ad" style="font-weight:800">Toplam</td><td class="s" style="font-weight:800">${kisa(toplam)}</td><td colspan="${verilen ? 2 : 1}"></td></tr></tfoot></table></div></div></div>`;
    $('#coDExcel').addEventListener('click', () => csvIndir('cek_' + cari.replace(/\s+/g, '_') + '_' + F.yil, verilen ? ['Çek No', 'Şirket', 'Banka', 'Keşide', 'Vade', 'Tutar', 'Durum', 'Açıklama'] : ['Çek No', 'Şirket', 'Banka', 'Vade', 'Tutar', 'Durum'], L.map(c => verilen ? [c.no, c.sirket, c.banka, c.keside, c.vade, sayi(c.tutar), c.durum, c.aciklama] : [c.no, c.sirket, c.banka, c.vade, sayi(c.tutar), c.durum])));
    $('#coDrawer').classList.add('acik'); $('#coPerde').classList.add('acik');
    if (!sessiz) { $('#coDrawer').scrollTop = 0; const v = $('#coDCekler tr.vurgu'); if (v) v.scrollIntoView({ block: 'center' }); }
  }
  function drawerKapat() { $('#coDrawer').classList.remove('acik'); $('#coPerde').classList.remove('acik'); acikCari = null; }

  // ---- başlangıç
  (async () => {
    await yukle();
    if (!V) return;
    sekme(kok.dataset.baslangic || 'verilen');
    if (kok.dataset.cari) drawerAc(kok.dataset.cari, 'verilen', null);
  })();
})();
