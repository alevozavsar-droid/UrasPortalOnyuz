// ============================================================
// VADE TARİHİNE GÖRE ÇEK VE SENET ÖZETİ — ekran betiği (Views/CekOzet/Index.cshtml; veri /CekOzet/Veri; kaynak Data/CekOrnek.cs + Data/ButceOrnek.cs › Cekler / Senetler)
// Sekmeler: vc Verilen Çekler (varsayılan) · vs Verilen Senetler · tc Tahsile Gönderilen Çekler · ts Tahsile Gönderilen Senetler · nakit Aylık Nakit Görünümü (çek + senet ortak)
// Ortak filtre: şirket (Tümü = konsolide), vade yılı, ödeme türü (verilen sekmeleri), durum (sekmeye göre verilen / alınan durumları), tahsil bankası, arama.
// Verilen matrisi: satır = cari (ödeme türü ara toplamlı: Tedarikçi, Maaş, Kira; yıllık toplama göre azalan), sütun = Ocak…Aralık + 12 Aylık Toplam + Aylık Ortalama
//   (toplam / 12) + % pay; VERİLEN TOPLAMI ve ADEDİ; altında TAHSİLE GÖNDERİLEN bloğu (yalnız banka satırları: Ziraat Bankası / Halk Bankası; toplam, adet) ve NET.
// Tahsil matrisi: satır = tahsil bankası; yalnız bankaya ibraz edilmiş belgeler (Tahsilde + Tahsil Edildi + Karşılıksız); durum filtresiyle gönderilmemişler de.
// Nakit: çek ve senet birlikte — verilen (çek / senet, tür bazında) ↔ tahsile gönderilen (çek / senet, banka bazında), portföy, ciro, net, kümülatif.
// 12 ay tek ekranda: dar cari sütunu (üç nokta; tam ad ipucunda), sıkı hücreler, özet sütunları sağa sabit. Hücre / satır → belge listesi (ay vurgulu).
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Ödenecek", "Tahsil Edildi").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('co'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const tam = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });   // ipucu: kuruşlu tam tutar (matris kuruşsuz gösterir)
  const pct = v => v === null || v === undefined || !isFinite(v) ? '—' : (v * 100).toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%';
  const iso = d => d.toISOString().slice(0, 10);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const bos = '<span class="text-muted">—</span>';
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + iso(new Date()) + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 9000); }
  const TUR_SINIF = { 'Tedarikçi': 'tedarikci', 'Maaş': 'maas', 'Kira': 'kira' };
  const turRozet = t => `<span class="co-tur ${TUR_SINIF[t] || 'diger'}">${esc(t)}</span>`;
  const vDurumRozet = d => rozet(d, d === 'Ödendi' ? 'ok' : d === 'İade Alındı' ? 'hata' : 'bekle');
  const aDurumRozet = d => rozet(d, d === 'Tahsil Edildi' ? 'ok' : d === 'Karşılıksız' || d === 'Riskli' ? 'hata' : d === 'Ciro Edildi' ? 'notr' : d === 'Tahsilde' ? 'bilgi' : 'bekle');
  const netHtml = v => `<span class="co-net ${v > 0.005 ? 'pozitif' : v < -0.005 ? 'negatif' : ''}">${v > 0 ? '+' : ''}${kisa(v)}</span>`;

  // Sekme → belge türü: vc / tc çek, vs / ts senet, nakit ikisi birlikte. Etiketler belge türüne göre.
  const SEKME_BELGE = { vc: 'Çek', vs: 'Senet', tc: 'Çek', ts: 'Senet', nakit: 'Çek + Senet' };
  // Varsayılan şirket: kullanıcının şirketi (sunucudan; verilen çekler o şirketin bütçe analizi tablosuyla birebir) — Temizle de bu şirkete döner; "Tümü" konsolide
  const VARSAYILAN_SIRKET = kok.dataset.sirket || '';
  let V = null, F = { sirket: VARSAYILAN_SIRKET, yil: +kok.dataset.yil, belge: 'Çek', tur: '', durum: '', banka: '', ara: '' }, acikCari = null, acikTip = 'verilen';
  const bankaUyar = c => !F.banka || c.tahsilBankasi === F.banka;
  const belgeUyar = c => F.belge === 'Çek + Senet' || c.belge === F.belge;
  const BELGE = () => F.belge === 'Senet' ? 'Senet' : F.belge === 'Çek' ? 'Çek' : 'Çek / Senet';
  const BELGE_COGUL = () => F.belge === 'Senet' ? 'Senetler' : F.belge === 'Çek' ? 'Çekler' : 'Çek ve Senetler';
  const BELGE_IYELIK = () => F.belge === 'Senet' ? 'Senetleri' : F.belge === 'Çek' ? 'Çekleri' : 'Çek ve Senetleri';
  const BELGE_TEKIL_IYELIK = () => F.belge === 'Senet' ? 'Senedi' : F.belge === 'Çek' ? 'Çeki' : 'Çek / Senedi';
  const BELGE_BUYUK = () => BELGE().toLocaleUpperCase('tr-TR');
  const bk = () => F.belge === 'Senet' ? 'senet' : F.belge === 'Çek' ? 'çek' : 'belge';
  const AY = () => V.aylar;
  const bugunAy = yil => yil === V.yil ? V.ay : 0;

  // ---- veri erişimi
  const aramaUyar = (c, alanlar) => { const a = F.ara.trim().toLocaleLowerCase('tr-TR'); return !a || alanlar.join(' ').toLocaleLowerCase('tr-TR').includes(a); };
  const sirketUyar = c => !F.sirket || c.sirket === F.sirket;
  const verilenListe = belge => V.verilen.filter(c => c.yil === F.yil && c.belge === belge && sirketUyar(c) && (!F.tur || c.tur === F.tur) && (!F.durum || c.durum === F.durum) && aramaUyar(c, [c.cari, c.cariKod, c.no, c.banka, c.sirket]));
  const alinanListe = belge => V.alinan.filter(c => c.yil === F.yil && c.belge === belge && sirketUyar(c) && (!F.durum || c.durum === F.durum) && bankaUyar(c) && aramaUyar(c, [c.musteri, c.musteriKod, c.no, c.banka, c.tahsilBankasi, c.sirket]));
  // cari bazında 12 aylık matris: anahtar → { ad, kod, tur, aylar[12] (tutar), adet[12], toplam, adetToplam, sirketler }
  function matris(liste, anahtar, ad, kod, tur) {
    const m = new Map();
    liste.forEach(c => { const k = anahtar(c); let r = m.get(k); if (!r) { r = { ad: ad(c), kod: kod(c), tur: tur ? tur(c) : '', sirketler: new Set(), aylar: new Array(12).fill(0), adet: new Array(12).fill(0), toplam: 0, adetToplam: 0 }; m.set(k, r); } r.aylar[c.ay - 1] += c.tutar; r.adet[c.ay - 1]++; r.toplam += c.tutar; r.adetToplam++; r.sirketler.add(c.sirket); });
    return [...m.values()].sort((a, b) => b.toplam - a.toplam);
  }
  const aylik = (l, f) => { const a = new Array(12).fill(0); l.forEach(c => { if (!f || f(c)) a[c.ay - 1] += c.tutar; }); return a; };
  const aylikAdet = (l, f) => { const a = new Array(12).fill(0); l.forEach(c => { if (!f || f(c)) a[c.ay - 1]++; }); return a; };
  const toplaDizi = a => a.reduce((s, v) => s + v, 0);

  // ---- sekmeler
  function sekme(p) {
    if (!SEKME_BELGE[p]) p = 'vc';
    F.belge = SEKME_BELGE[p];
    $$('#coSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('#co > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    durumSecenekleri(p);
    if (V) ciz(p);
    try { const u = new URL(location.href); if (p === 'vc') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }
  $$('#coSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  const aktifSekme = () => ($('#coSekmeler button.aktif') || {}).dataset?.p || 'vc';
  function ciz(p) { if (p === 'vc' || p === 'vs') cizVerilen(p); else if (p === 'tc' || p === 'ts') cizTahsil(p); else cizNakit(); sayaclar(); }
  function hepsiniCiz() { ciz(aktifSekme()); if (acikCari && $('#coDrawer').classList.contains('acik')) drawerAc(acikCari, acikTip, null, true); }
  function durumSecenekleri(p) {
    const s = $('#coDurum'), mevcut = s.value, l = p === 'tc' || p === 'ts' ? V.alinanDurumlari : p === 'nakit' ? [] : V.durumlar;
    s.innerHTML = '<option value="">Tümü</option>' + l.map(d => `<option>${esc(d)}</option>`).join(''); s.value = l.includes(mevcut) ? mevcut : ''; F.durum = s.value; s.disabled = p === 'nakit';
    $('#coTur').disabled = p !== 'vc' && p !== 'vs';
  }

  // ---- veri
  async function yukle() {
    let r; try { r = await (await fetch('/CekOzet/Veri')).json(); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r;
    const ys = $('#coYil'); ys.innerHTML = V.yillar.map(y => `<option value="${y}">${y}</option>`).join(''); ys.value = String(F.yil);
    $('#coTur').innerHTML = '<option value="">Tümü</option>' + V.turler.map(t => `<option>${esc(t)}</option>`).join('');
    $('#coBanka').innerHTML = '<option value="">Tümü</option>' + V.tahsilBankalari.map(b => `<option>${esc(b)}</option>`).join('');
    $('#coSirket').addEventListener('change', () => { F.sirket = $('#coSirket').value; hepsiniCiz(); });
    ys.addEventListener('change', () => { F.yil = +ys.value; hepsiniCiz(); });
    $('#coTur').addEventListener('change', () => { F.tur = $('#coTur').value; hepsiniCiz(); });
    $('#coDurum').addEventListener('change', () => { F.durum = $('#coDurum').value; hepsiniCiz(); });
    $('#coBanka').addEventListener('change', () => { F.banka = $('#coBanka').value; hepsiniCiz(); });
    $('#coAra').addEventListener('input', () => { F.ara = $('#coAra').value; hepsiniCiz(); });
    $('#coSirket').value = F.sirket; if ($('#coSirket').value !== F.sirket) { F.sirket = ''; $('#coSirket').value = ''; }
    $('#coTemizle').addEventListener('click', () => { F = { sirket: VARSAYILAN_SIRKET, yil: V.yil, belge: F.belge, tur: '', durum: '', banka: '', ara: '' }; $('#coSirket').value = F.sirket; ys.value = String(F.yil); $('#coTur').value = ''; $('#coDurum').value = ''; $('#coBanka').value = ''; $('#coAra').value = ''; hepsiniCiz(); });
    $('#coExcel').addEventListener('click', () => { const p = aktifSekme(); if (p === 'vc' || p === 'vs') verilenCsv(); else if (p === 'tc' || p === 'ts') tahsilCsv(); else nakitCsv(); });
    ['vc', 'vs'].forEach(p => $('#coVExcel-' + p).addEventListener('click', verilenCsv));
    ['tc', 'ts'].forEach(p => $('#coTExcel-' + p).addEventListener('click', tahsilCsv));
    $('#coNExcel').addEventListener('click', nakitCsv);
    $$('.na-kpi').forEach(k => k.addEventListener('click', e => { const el = e.target.closest('[data-durum], [data-banka]'); if (!el) return; if (el.dataset.banka) { F.banka = F.banka === el.dataset.banka ? '' : el.dataset.banka; $('#coBanka').value = F.banka; } else { F.durum = F.durum === el.dataset.durum ? '' : el.dataset.durum; $('#coDurum').value = F.durum; } hepsiniCiz(); }));
    $('#coDrawerKapat').addEventListener('click', drawerKapat); $('#coPerde').addEventListener('click', drawerKapat);
  }
  function sayaclar() {
    const say = (belge, tahsile) => (tahsile ? V.alinan : V.verilen).filter(c => c.yil === F.yil && c.belge === belge && (!tahsile || c.tahsile) && sirketUyar(c)).length;
    $('#coN-vc').textContent = say('Çek', false); $('#coN-vs').textContent = say('Senet', false); $('#coN-tc').textContent = say('Çek', true); $('#coN-ts').textContent = say('Senet', true);
    const kapsam = (F.sirket || 'Tüm Şirketler (Konsolide)') + ' · ' + F.yil;
    $$('.co-kapsam').forEach(e => e.textContent = kapsam); $$('.co-yil').forEach(e => e.textContent = F.yil);
  }
  const basliklar = (ilk, ekstra) => `<tr><th class="ad">${ilk}</th>${AY().map((a, i) => `<th class="s ${i + 1 === bugunAy(F.yil) ? 'bugun-ay' : ''}">${a.slice(0, 3)}</th>`).join('')}${ekstra}</tr>`;
  // Özet sütunları (sağa sabit): 12 Aylık Toplam = ayların toplamı · Aylık Ortalama = toplam / 12 (şablondaki gibi) · % = satır toplamı / genel toplam
  const OZET_BASLIK = '<th class="s sag sag3">12 Aylık Toplam</th><th class="s sag sag2">Aylık Ort.</th><th class="s sag sag1">%</th>';
  const ozetHucreler = (toplam, genel) => `<td class="s yil sag sag3" title="12 aylık toplam ${tam(toplam)} TL">${kisa(toplam)}</td><td class="s ort sag sag2" title="aylık ortalama (toplam / 12) ${tam(toplam / 12)} TL">${kisa(toplam / 12)}</td><td class="s sag sag1">${pct(genel ? toplam / genel : 0)}</td>`;
  const ayHucre = (v, n, i, bA, tik) => `<td class="s ${tik ? 'hucre' : ''} ${i + 1 === bA ? 'bugun-ay' : ''}" data-ay="${i + 1}" ${v ? `title="${AY()[i]} · ${tam(v)} TL${n !== undefined ? ` · ${n} ${bk()}` : ''}"` : ''}>${v ? kisa(v) + (n !== undefined ? `<small>${n} ${bk()}</small>` : '') : bos}</td>`;
  const toplamSatir = (ad, a, g, bA, cls) => `<tr class="${cls || 'toplam'}"><td class="ad" title="${esc(ad)}">${esc(ad)}</td>${a.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}" ${v ? `title="${AY()[i]} · ${tam(v)} TL"` : ''}>${v ? kisa(v) : '—'}</td>`).join('')}${ozetHucreler(toplaDizi(a), g)}</tr>`;
  const adetSatir = (ad, a, bA) => `<tr class="adet"><td class="ad" title="${esc(ad)}">${esc(ad)}</td>${a.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}">${v || '—'}</td>`).join('')}<td class="s sag sag3">${toplaDizi(a)}</td><td class="s sag sag2">${(toplaDizi(a) / 12).toLocaleString('tr-TR', { maximumFractionDigits: 1 })}</td><td class="sag sag1"></td></tr>`;
  // tahsile gönderilen belgeler banka bazında (Ziraat Bankası / Halk Bankası); satır → o bankaya gönderilenlerin listesi; gönderilmemişler bilgi satırı
  const bankaSatirlari = (liste, genel, bA) => {
    const satir = (ad, l, tik) => { const a = aylik(l), n = aylikAdet(l); return `<tr class="banka ${tik ? 'tik' : ''}" ${tik ? `data-banka="${esc(ad)}"` : ''}><td class="ad" title="${esc(ad)} · ${l.length} ${bk()}"><b>${esc(ad)}</b><small>${l.length} ${bk()}</small></td>${a.map((v, i) => ayHucre(v, n[i], i, bA, tik)).join('')}${ozetHucreler(toplaDizi(a), genel)}</tr>`; };
    const gonderilmemis = liste.filter(c => !c.tahsilBankasi);
    return V.tahsilBankalari.map(b => satir(b, liste.filter(c => c.tahsilBankasi === b), true)).join('') + (gonderilmemis.length ? satir('Henüz Tahsile Gönderilmedi', gonderilmemis, false) : '');
  };

  // ---- VERİLEN ÇEKLER / SENETLER (p = vc | vs)
  function cizVerilen(p) {
    const belge = SEKME_BELGE[p], L = verilenListe(belge), M = matris(L, c => c.tur + '|' + c.cari + '|' + c.cariKod, c => c.cari, c => c.cariKod, c => c.tur);
    const genel = M.reduce((s, r) => s + r.toplam, 0), ayTop = aylik(L), ayAdet = aylikAdet(L), bA = bugunAy(F.yil);
    const yilL = V.verilen.filter(c => c.yil === F.yil && c.belge === belge && sirketUyar(c));
    const TG = F.tur ? [] : V.alinan.filter(c => c.yil === F.yil && c.belge === belge && c.tahsile && sirketUyar(c) && bankaUyar(c) && aramaUyar(c, [c.musteri, c.musteriKod, c.no, c.banka, c.tahsilBankasi, c.sirket]));
    const tAy = aylik(TG), tAdet = aylikAdet(TG), tGenel = toplaDizi(tAy);
    const yilTahsil = V.alinan.filter(c => c.yil === F.yil && c.belge === belge && c.tahsile && sirketUyar(c) && bankaUyar(c)), tahsilYil = yilTahsil.reduce((s, c) => s + c.tutar, 0), verilenYil = yilL.reduce((s, c) => s + c.tutar, 0);
    const odenecek = yilL.filter(c => c.durum === 'Ödenecek').reduce((s, c) => s + c.tutar, 0), odendi = yilL.filter(c => c.durum === 'Ödendi').reduce((s, c) => s + c.tutar, 0), iade = yilL.filter(c => c.durum === 'İade Alındı').reduce((s, c) => s + c.tutar, 0);
    const buAy = bA ? yilL.filter(c => c.ay === bA).reduce((s, c) => s + c.tutar, 0) : 0, enYuksek = ayTop.indexOf(Math.max(...ayTop));
    const turT = t => yilL.filter(c => c.tur === t).reduce((s, c) => s + c.tutar, 0);
    $('#coKpi-' + p).innerHTML = [
      { l: 'Yıllık Verilen ' + BELGE(), v: kisa(verilenYil), s: `${yilL.length} ${bk()} · ${new Set(yilL.map(c => c.cari)).size} cari`, k: '#0369a1' },
      { l: 'Ödendi', v: kisa(odendi), s: 'vadesi geçmiş, banka ödedi', k: '#15803d', d: 'Ödendi' }, { l: 'Ödenecek', v: kisa(odenecek), s: 'vadesi gelmemiş · tıkla: filtre', k: '#b45309', d: 'Ödenecek' }, { l: 'İade Alındı', v: kisa(iade), s: 'geri alınan / iptal', k: '#dc2626', d: 'İade Alındı' },
      { l: bA ? `Bu Ay Vadesi Gelen (${AY()[bA - 1]})` : 'Bu Ay Vadesi Gelen', v: bA ? kisa(buAy) : '—', s: bA ? `${yilL.filter(c => c.ay === bA).length} ${bk()}` : 'seçili yıl bu yıl değil', k: '#7c3aed' },
      { l: 'En Yüksek Ay', v: genel ? AY()[enYuksek] : '—', s: genel ? kisa(ayTop[enYuksek]) + ' TL' : '—', k: '#0f172a' },
      { l: 'Tedarikçi ' + BELGE_IYELIK(), v: kisa(turT('Tedarikçi')), s: `maaş ${kisa(turT('Maaş'))} · kira ${kisa(turT('Kira'))}`, k: '#1d4ed8' },
      { l: 'Tahsile Gönderilen', v: kisa(tahsilYil), s: `${yilTahsil.length} müşteri ${BELGE_TEKIL_IYELIK().toLocaleLowerCase('tr-TR')} · net ${tahsilYil - verilenYil >= 0 ? '+' : ''}${kisa(tahsilYil - verilenYil)}`, k: '#0f766e' }
    ].map(k => `<div style="--k:${k.k};${k.d && F.durum === k.d ? 'background:#f0f9ff' : ''}" ${k.d ? `data-durum="${k.d}" title="Durum filtresi"` : ''}><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
    const t = $('#coVerilenTablo-' + p);
    t.tHead.innerHTML = basliklar('Verilen · Cari', OZET_BASLIK);
    // % payı: cari satırında kendi ödeme türü (grup) toplamına göre (şablondaki gibi: tedarikçi payı tedarikçi toplamına), tür satırında genel toplama göre
    const grupTop = {}; M.forEach(r => grupTop[r.tur] = (grupTop[r.tur] || 0) + r.toplam);
    const satir = r => { const alt = r.kod ? r.kod + (F.sirket ? '' : ' · ' + [...r.sirketler].join(', ')) : F.sirket ? '' : [...r.sirketler].join(', '); return `<tr class="tik" data-cari="${esc(r.ad)}" data-tur="${esc(r.tur)}"><td class="ad" title="${esc(r.ad)}${alt ? ' · ' + esc(alt) : ''} · ${r.adetToplam} ${bk()} · ${tam(r.toplam)} TL"><b>${esc(r.ad)}</b>${alt ? `<small>${esc(alt)}</small>` : ''}</td>${r.aylar.map((v, i) => ayHucre(v, r.adet[i], i, bA, true)).join('')}${ozetHucreler(r.toplam, grupTop[r.tur])}</tr>`; };
    // tür satırı: aylık toplam + o ayki belge adedi (şablondaki AYLIK TOPLAM TUTAR / AYLIK ÇEK ADEDİ satırlarının tür karşılığı)
    const grup = (ad, l) => { const a = new Array(12).fill(0), n = new Array(12).fill(0); let top = 0, adet = 0; l.forEach(r => { for (let i = 0; i < 12; i++) { a[i] += r.aylar[i]; n[i] += r.adet[i]; } top += r.toplam; adet += r.adetToplam; }); return `<tr class="grp"><td class="ad" title="${esc(ad)} · ${l.length} cari · ${adet} ${bk()} · ${tam(top)} TL">${esc(ad)} <small style="display:inline;text-transform:none;font-weight:600">· ${l.length} cari · ${adet} ${bk()}</small></td>${a.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}" ${v ? `title="${AY()[i]} · ${tam(v)} TL · ${n[i]} ${bk()}"` : ''}>${v ? kisa(v) + `<small>${n[i]} ${bk()}</small>` : ''}</td>`).join('')}${ozetHucreler(top, genel)}</tr>`; };
    const turler = V.turler.filter(x => M.some(r => r.tur === x));
    const verilenBlok = M.length ? turler.map(x => { const l = M.filter(r => r.tur === x); return grup(x, l) + l.map(satir).join(''); }).join('') + toplamSatir(`VERİLEN ${BELGE_BUYUK()} TOPLAMI`, ayTop, genel, bA) + adetSatir(`VERİLEN ${BELGE_BUYUK()} ADEDİ`, ayAdet, bA) : `<tr><td colspan="16" class="na-bos">Filtreye uyan verilen ${bk()} yok.</td></tr>`;
    const net = tAy.map((v, i) => v - ayTop[i]);
    const tahsilBlok = F.tur ? '' : `<tr class="ara"><td colspan="16"></td></tr>
      <tr class="grp"><td class="ad" title="Tahsile gönderilen ${BELGE_COGUL()} · bankaya nakit dönüşü için ibraz edilen · ${TG.length} ${bk()}">Tahsile Gönderilen ${BELGE_COGUL()} <small style="display:inline;text-transform:none;font-weight:600">· ${TG.length} ${bk()}</small></td>${tAy.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}">${v ? kisa(v) : ''}</td>`).join('')}${ozetHucreler(tGenel, tGenel)}</tr>
      ${TG.length ? bankaSatirlari(TG, tGenel, bA) : `<tr><td colspan="16" class="na-bos">Seçili yılda tahsile gönderilen müşteri ${BELGE_TEKIL_IYELIK().toLocaleLowerCase('tr-TR')} yok.</td></tr>`}
      ${toplamSatir('TAHSİLE GÖNDERİLEN TOPLAMI', tAy, tGenel, bA)}${adetSatir(`TAHSİLE GÖNDERİLEN ${BELGE_BUYUK()} ADEDİ`, tAdet, bA)}
      <tr class="toplam"><td class="ad" title="NET = tahsile gönderilen − verilen">NET · Tahsil − Verilen</td>${net.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}">${netHtml(v)}</td>`).join('')}<td class="s yil sag sag3">${netHtml(toplaDizi(net))}</td><td class="s ort sag sag2">${netHtml(toplaDizi(net) / 12)}</td><td class="sag sag1"></td></tr>`;
    t.tBodies[0].innerHTML = verilenBlok + tahsilBlok; t.tFoot.innerHTML = '';
    $$('tr.tik', t).forEach(r => r.addEventListener('click', e => { const h = e.target.closest('td.hucre'); if (r.dataset.banka) drawerAc(r.dataset.banka, 'banka', h ? +h.dataset.ay : null); else drawerAc(r.dataset.cari, 'verilen', h ? +h.dataset.ay : null); }));
    $('#coVNot-' + p).textContent = `${F.sirket || 'konsolide'} · ${F.yil} · verilen ${M.length} cari / ${L.length} ${bk()} / ${kisa(genel)} TL${F.tur ? '' : ` · tahsile gönderilen ${TG.length} ${bk()} / ${kisa(tGenel)} TL`} · satıra tıklayınca liste`;
  }
  function verilenCsv() {
    const belge = F.belge, M = matris(verilenListe(belge), c => c.tur + '|' + c.cari + '|' + c.cariKod, c => c.cari, c => c.cariKod, c => c.tur), genel = M.reduce((s, r) => s + r.toplam, 0);
    const grupTop = {}; M.forEach(r => grupTop[r.tur] = (grupTop[r.tur] || 0) + r.toplam);
    const TG = F.tur ? [] : V.alinan.filter(c => c.yil === F.yil && c.belge === belge && c.tahsile && sirketUyar(c) && bankaUyar(c)), tGenel = TG.reduce((s, c) => s + c.tutar, 0);
    const satir = (tur, ad, kod, a, top, g, adet) => [tur, ad, kod, ...a.map(v => v ? sayi(v) : ''), sayi(top), sayi(top / 12), g ? (top / g * 100).toFixed(2).replace('.', ',') : '', adet];
    const turSatir = t => { const l = M.filter(r => r.tur === t), a = new Array(12).fill(0); l.forEach(r => r.aylar.forEach((v, i) => a[i] += v)); return satir(t + ' Toplamı', '', '', a, toplaDizi(a), genel, l.reduce((s, r) => s + r.adetToplam, 0)); };
    csvIndir(`verilen_${bk()}_vade_${F.yil}`, ['Tür', 'Cari', 'Cari Kodu', ...AY(), '12 Aylık Toplam', 'Aylık Ortalama', '% (Tür İçi Pay)', 'Adet'],
      [...V.turler.filter(t => M.some(r => r.tur === t)).flatMap(t => [...M.filter(r => r.tur === t).map(r => satir(r.tur, r.ad, r.kod, r.aylar, r.toplam, grupTop[t], r.adetToplam)), turSatir(t)]), ...V.tahsilBankalari.map(b => { const l = TG.filter(c => c.tahsilBankasi === b), a = aylik(l); return satir('Tahsile Gönderilen', b, '', a, toplaDizi(a), tGenel, l.length); })]);
  }

  // ---- TAHSİLE GÖNDERİLEN ÇEKLER / SENETLER (p = tc | ts)
  function cizTahsil(p) {
    const belge = SEKME_BELGE[p], yilL = V.alinan.filter(c => c.yil === F.yil && c.belge === belge && sirketUyar(c) && bankaUyar(c));
    const top = d => yilL.filter(c => c.durum === d).reduce((s, c) => s + c.tutar, 0), gonderilen = yilL.filter(c => c.tahsile), bankaT = b => gonderilen.filter(c => c.tahsilBankasi === b);
    $('#coTKpi-' + p).innerHTML = [
      { l: 'Tahsile Gönderilen', v: kisa(gonderilen.reduce((s, c) => s + c.tutar, 0)), s: `${gonderilen.length} ${bk()} · bankaya ibraz`, k: '#0369a1' },
      ...V.tahsilBankalari.map(b => ({ l: b, v: kisa(bankaT(b).reduce((s, c) => s + c.tutar, 0)), s: `${bankaT(b).length} ${bk()} · tıkla: filtre`, k: b === 'Ziraat Bankası' ? '#15803d' : '#1d4ed8', b })),
      { l: 'Tahsil Edildi', v: kisa(top('Tahsil Edildi')), s: 'hesaba geçti', k: '#15803d', d: 'Tahsil Edildi' }, { l: 'Tahsilde', v: kisa(top('Tahsilde')), s: 'bankada vade bekliyor', k: '#2563eb', d: 'Tahsilde' },
      { l: 'Karşılıksız', v: kisa(top('Karşılıksız')), s: 'gönderildi, ödenmedi', k: '#dc2626', d: 'Karşılıksız' }, { l: 'Portföyde', v: kisa(top('Portföyde')), s: 'henüz gönderilmedi', k: '#b45309', d: 'Portföyde' },
      { l: 'Ciro Edildi', v: kisa(top('Ciro Edildi')), s: 'tedarikçiye verildi', k: '#6b7280', d: 'Ciro Edildi' }, { l: 'Riskli', v: kisa(top('Riskli')), s: 'müşteri riski · tıkla: filtre', k: '#9d174d', d: 'Riskli' }
    ].map(k => `<div style="--k:${k.k};${(k.d && F.durum === k.d) || (k.b && F.banka === k.b) ? 'background:#f0f9ff' : ''}" ${k.d ? `data-durum="${k.d}" title="Durum filtresi"` : k.b ? `data-banka="${esc(k.b)}" title="Tahsil bankası filtresi"` : ''}><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
    // durum filtresi yoksa yalnız tahsile gönderilenler; filtre varsa o durum. Satırlar banka bazında; belge listesi satırdan açılır
    const L = alinanListe(belge).filter(c => F.durum ? true : c.tahsile), genel = L.reduce((s, c) => s + c.tutar, 0), ayTop = aylik(L), ayAdet = aylikAdet(L), bA = bugunAy(F.yil);
    const t = $('#coTahsilTablo-' + p);
    t.tHead.innerHTML = basliklar('Tahsil Bankası', OZET_BASLIK);
    t.tBodies[0].innerHTML = L.length ? bankaSatirlari(L, genel, bA) : `<tr><td colspan="16" class="na-bos">Filtreye uyan ${bk()} yok.</td></tr>`;
    t.tFoot.innerHTML = L.length ? toplamSatir('AYLIK TOPLAM TUTAR', ayTop, genel, bA) + adetSatir(`AYLIK ${BELGE_BUYUK()} ADEDİ`, ayAdet, bA) : '';
    $$('tr.tik', t).forEach(r => r.addEventListener('click', e => { const h = e.target.closest('td.hucre'); drawerAc(r.dataset.banka, 'banka', h ? +h.dataset.ay : null); }));
    $('#coTNot-' + p).textContent = `${F.sirket || 'konsolide'} · ${F.yil} · ${F.durum || 'tahsile gönderilen (tahsil edildi + tahsilde + karşılıksız)'} · ${L.length} ${bk()} · ${kisa(genel)} TL · satıra tıklayınca bankaya gönderilenlerin listesi`;
  }
  function tahsilCsv() {
    const L = alinanListe(F.belge).filter(c => F.durum ? true : c.tahsile), genel = L.reduce((s, c) => s + c.tutar, 0);
    const satir = (ad, l) => { const a = aylik(l), top = toplaDizi(a); return [ad, ...a.map(v => v ? sayi(v) : ''), sayi(top), sayi(top / 12), genel ? (top / genel * 100).toFixed(2).replace('.', ',') : '', l.length]; };
    csvIndir(`tahsile_gonderilen_${bk()}_${F.yil}`, ['Tahsil Bankası', ...AY(), '12 Aylık Toplam', 'Aylık Ortalama', '%', 'Adet'], [...V.tahsilBankalari.map(b => satir(b, L.filter(c => c.tahsilBankasi === b))), ...(L.some(c => !c.tahsilBankasi) ? [satir('Henüz Tahsile Gönderilmedi', L.filter(c => !c.tahsilBankasi))] : [])]);
  }

  // ---- AYLIK NAKİT GÖRÜNÜMÜ (çek + senet ortak)
  function nakitSatirlari() {
    const vL = V.verilen.filter(c => c.yil === F.yil && sirketUyar(c) && c.durum !== 'İade Alındı'), aL = V.alinan.filter(c => c.yil === F.yil && sirketUyar(c) && bankaUyar(c));
    const r = [];
    ['Çek', 'Senet'].forEach(b => {
      const l = vL.filter(c => c.belge === b);
      V.turler.forEach(t => { const lt = l.filter(c => c.tur === t); if (lt.length) r.push({ ad: `Verilen ${b} · ${t}`, a: aylik(lt), cls: '' }); });
      r.push({ ad: `VERİLEN ${b.toLocaleUpperCase('tr-TR')} TOPLAMI`, a: aylik(l), cls: 'toplam', adet: aylikAdet(l) });
    });
    const verilen = aylik(vL); r.push({ ad: 'VERİLEN TOPLAM · ÇEK + SENET', a: verilen, cls: 'genel', adet: aylikAdet(vL) });
    r.push({ ad: '', a: new Array(12).fill(0), cls: 'ara' });
    ['Çek', 'Senet'].forEach(b => {
      const l = aL.filter(c => c.belge === b && c.tahsile);
      V.tahsilBankalari.forEach(bk2 => r.push({ ad: `Tahsile Gönderilen ${b} · ${bk2}`, a: aylik(l, c => c.tahsilBankasi === bk2), cls: 'banka', adet: aylikAdet(l, c => c.tahsilBankasi === bk2) }));
      r.push({ ad: `TAHSİLE GÖNDERİLEN ${b.toLocaleUpperCase('tr-TR')} TOPLAMI`, a: aylik(l), cls: 'toplam', adet: aylikAdet(l) });
    });
    const tahsil = aylik(aL, c => c.tahsile); r.push({ ad: 'TAHSİLE GÖNDERİLEN TOPLAM · ÇEK + SENET', a: tahsil, cls: 'genel', adet: aylikAdet(aL, c => c.tahsile) });
    r.push({ ad: 'Tahsil Edildi', a: aylik(aL, c => c.durum === 'Tahsil Edildi'), cls: '' }, { ad: 'Tahsilde (Bankada Bekliyor)', a: aylik(aL, c => c.durum === 'Tahsilde'), cls: '' }, { ad: 'Karşılıksız', a: aylik(aL, c => c.durum === 'Karşılıksız'), cls: '' });
    r.push({ ad: 'Portföyde Bekleyen (Gönderilmedi)', a: aylik(aL, c => c.durum === 'Portföyde'), cls: 'adet' }, { ad: 'Ciro Edilen', a: aylik(aL, c => c.durum === 'Ciro Edildi'), cls: 'adet' });
    const net = tahsil.map((v, i) => v - verilen[i]); let k = 0; const kum = net.map(v => k += v);
    r.push({ ad: 'NET · Tahsile Gönderilen − Verilen', a: net, cls: 'genel', net: true }, { ad: 'Kümülatif Net', a: kum, cls: '', net: true, kum: true });
    return r;
  }
  function cizNakit() {
    const R = nakitSatirlari(), bA = bugunAy(F.yil), t = $('#coNakitTablo');
    t.tHead.innerHTML = basliklar('Kalem', '<th class="s sag sag1 genis">Yıl Toplamı</th>');
    t.tBodies[0].innerHTML = R.map(r => r.cls === 'ara' ? '<tr class="ara"><td colspan="14"></td></tr>' : `<tr class="${r.cls}"><td class="ad" title="${esc(r.ad)}">${esc(r.ad)}</td>${r.a.map((v, i) => `<td class="s ${i + 1 === bA ? 'bugun-ay' : ''}">${r.net ? netHtml(v) : v ? kisa(v) + (r.adet ? `<small>${r.adet[i]} belge</small>` : '') : '—'}</td>`).join('')}<td class="s yil sag sag1 genis">${r.net ? netHtml(r.kum ? r.a[11] : toplaDizi(r.a)) : kisa(toplaDizi(r.a))}</td></tr>`).join('');
  }
  function nakitCsv() { csvIndir('aylik_nakit_gorunumu_' + F.yil, ['Kalem', ...AY(), 'Yıl Toplamı'], nakitSatirlari().filter(r => r.cls !== 'ara').map(r => [r.ad, ...r.a.map(v => sayi(v)), sayi(r.kum ? r.a[11] : toplaDizi(r.a))])); }

  // ---- DETAY (drawer): carinin ya da bankanın belgeleri (ay vurgulu)
  function drawerAc(cari, tip, ay, sessiz) {
    acikCari = cari; acikTip = tip;
    const verilen = tip === 'verilen', banka = tip === 'banka';
    const L = (verilen ? V.verilen.filter(c => c.cari === cari) : banka ? V.alinan.filter(c => c.tahsile && c.tahsilBankasi === cari && (!F.durum || c.durum === F.durum)) : V.alinan.filter(c => c.musteri === cari && bankaUyar(c))).filter(c => c.yil === F.yil && belgeUyar(c) && sirketUyar(c)).sort((a, b) => a.ay - b.ay || a.vade.localeCompare(b.vade));
    if (!L.length) return;
    const kod = banka ? '' : verilen ? L[0].cariKod : L[0].musteriKod, tur = verilen ? L[0].tur : '', toplam = L.reduce((s, c) => s + c.tutar, 0);
    const aylikT = aylik(L);
    const durumlar = [...new Set(L.map(c => c.durum))].map(d => `${d}: ${kisa(L.filter(c => c.durum === d).reduce((s, c) => s + c.tutar, 0))}`).join(' · ');
    $('#coDrawerBaslik').innerHTML = `${esc(cari)} ${banka ? rozet('Tahsil Bankası · Tahsile Gönderilen ' + BELGE_COGUL(), 'bilgi') : verilen ? turRozet(tur) : rozet('Müşteri ' + BELGE_TEKIL_IYELIK(), 'bilgi')} ${banka ? '' : kod ? `<span class="co-pill">${esc(kod)}</span>` : '<span class="co-pill" title="Cari kartı tanımlı değil">Kart Yok</span>'}`;
    $('#coDrawerAksiyon').innerHTML = `${kod ? `<span class="btn btn-light btn-sm" data-url="/CariKart/${verilen ? 'Tedarikci' : 'Musteri'}?kod=${esc(kod)}" data-baslik="${verilen ? 'Tedarikçi' : 'Müşteri'} Cari Kartları" style="font-size:.66rem"><i class="fas fa-address-card me-1"></i>Cari Kartı Aç</span>` : ''}<button type="button" class="btn btn-light btn-sm" id="coDExcel" style="font-size:.66rem"><i class="fas fa-file-excel me-1"></i>Excel</button>`;
    const noBaslik = F.belge === 'Çek + Senet' ? 'Belge No' : BELGE() + ' No';
    $('#coDrawerIc').innerHTML = `
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-circle-info me-1"></i>${verilen ? 'Verilen ' : banka ? 'Tahsile Gönderilen ' : 'Alınan '}${BELGE()} Özeti</span><span class="not">${esc(F.sirket || 'konsolide')} · ${F.yil}</span></div><div class="na-kart-g">
        <div class="co-ozet"><div><div class="l">12 Aylık Toplam</div><div class="v">${tam(toplam)} TL</div></div><div><div class="l">${BELGE()} Adedi</div><div class="v">${L.length}</div></div><div><div class="l">Aylık Ortalama</div><div class="v">${tam(toplam / 12)} TL</div></div><div><div class="l">Durum Dağılımı</div><div class="v" style="font-weight:500">${esc(durumlar)}</div></div></div>
        <div class="table-responsive mt-2"><table class="na-tablo co-excel" data-dinamik="kapali"><thead><tr>${AY().map((a, i) => `<th class="s ${i + 1 === ay ? 'bugun-ay' : ''}">${a.slice(0, 3)}</th>`).join('')}</tr></thead><tbody><tr>${aylikT.map((v, i) => `<td class="s ${i + 1 === ay ? 'bugun-ay' : ''}">${v ? kisa(v) : '—'}</td>`).join('')}</tr></tbody></table></div>
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-list me-1"></i>${BELGE()} Listesi (${L.length})</span><span class="not">${ay ? AY()[ay - 1] + ' vurgulu · ' : ''}vade sırasıyla</span></div><div class="na-kart-g p-0"><div class="table-responsive" style="max-height:60vh"><table class="na-tablo" id="coDCekler" data-dinamik="kapali"><thead><tr><th>${noBaslik}</th><th>Şirket</th>${banka ? '<th>Müşteri</th>' : ''}<th>${verilen ? 'Banka' : 'Keşideci Banka'}</th>${verilen ? '<th>Keşide</th>' : ''}<th>Vade</th><th class="s">Tutar</th><th>Durum</th>${verilen ? '<th>Açıklama</th>' : '<th>Tahsil Bankası</th>'}</tr></thead><tbody>
        ${L.map(c => `<tr class="${c.ay === ay ? 'vurgu' : ''}"><td><b>${esc(c.no)}</b>${F.belge === 'Çek + Senet' ? `<small>${esc(c.belge)}</small>` : ''}</td><td>${esc(c.sirket)}</td>${banka ? `<td class="ad"><b>${esc(c.musteri)}</b><small>${esc(c.musteriKod)}</small></td>` : ''}<td>${esc(c.banka)}</td>${verilen ? `<td>${esc(c.keside)}</td>` : ''}<td>${esc(c.vade)}</td><td class="s">${tam(c.tutar)}</td><td>${verilen ? vDurumRozet(c.durum) : aDurumRozet(c.durum)}</td>${verilen ? `<td class="ad"><small>${esc(c.aciklama || '')}</small></td>` : `<td>${c.tahsilBankasi ? esc(c.tahsilBankasi) : bos}</td>`}</tr>`).join('')}
      </tbody><tfoot><tr><td colspan="${verilen || banka ? 5 : 4}" class="ad" style="font-weight:800">Toplam</td><td class="s" style="font-weight:800">${tam(toplam)}</td><td colspan="2"></td></tr></tfoot></table></div></div></div>`;
    $('#coDExcel').addEventListener('click', () => csvIndir(`${bk()}_${cari.replace(/\s+/g, '_')}_${F.yil}`, verilen ? [noBaslik, 'Şirket', 'Banka', 'Keşide', 'Vade', 'Tutar', 'Durum', 'Açıklama'] : [noBaslik, 'Şirket', 'Müşteri', 'Müşteri Kodu', 'Keşideci Banka', 'Vade', 'Tutar', 'Durum', 'Tahsil Bankası'], L.map(c => verilen ? [c.no, c.sirket, c.banka, c.keside, c.vade, sayi(c.tutar), c.durum, c.aciklama] : [c.no, c.sirket, c.musteri, c.musteriKod, c.banka, c.vade, sayi(c.tutar), c.durum, c.tahsilBankasi || ''])));
    $('#coDrawer').classList.add('acik'); $('#coPerde').classList.add('acik');
    if (!sessiz) { $('#coDrawer').scrollTop = 0; const v = $('#coDCekler tr.vurgu'); if (v) v.scrollIntoView({ block: 'center' }); }
  }
  function drawerKapat() { $('#coDrawer').classList.remove('acik'); $('#coPerde').classList.remove('acik'); acikCari = null; }

  // ---- başlangıç
  (async () => {
    await yukle();
    if (!V) return;
    sekme(kok.dataset.baslangic || 'vc');
    if (kok.dataset.cari) drawerAc(kok.dataset.cari, 'verilen', null);
  })();
})();
