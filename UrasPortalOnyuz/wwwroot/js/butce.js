// ============================================================
// BÜTÇE RAPORU — ekran betiği (Views/Butce/Index.cshtml; veri /Butce/Veri; revizyon POST /Butce/Revize; kaynak Data/ButceOrnek.cs)
// Sekmeler: Bütçe Özeti (varsayılan) → Sabit Giderler → Tedarik Zinciri · Hammadde → Genel Giderler
// Ortak filtre: şirket (Tümü = konsolide toplam), bütçe yılı, seçili ay, sapma durumu (YTD), alt grup, arama.
// Hesap kuralları (istemcide):
//   Bütçe / Gerçekleşen : şirket × yıl × kalem satırlarının aylık toplamı; gerçekleşen yalnız kapanmış aylarda (içinde bulunulan ay geçici, kısmi)
//   YTD                 : yıl başından gerçekleşeni olan son aya kadar (kapanmış aylar + geçici ay)
//   Sapma               : Gerçekleşen − Bütçe (pozitif = bütçe aşımı, kırmızı; negatif = tasarruf, yeşil); Sapma % = Sapma / Bütçe
//   Durum               : YTD sapma > %5 → Aşım · < −%5 → Tasarruf · arası → Hedefte · gerçekleşen yoksa Veri Yok; Kullanım = YTD gerçekleşen / yıllık bütçe
//   Vadeli çek           : vade ayına göre müşteri çekleri; Kullanılabilir = Portföyde + Tahsilde + Tahsil Edildi (ciro edilen ve karşılıksız / riskli hariç);
//                          Bütçe Karşılama = kullanılabilir / o ayın toplam gider bütçesi; Gider Sonrası Kalan = kullanılabilir − gerçekleşen (açık ayda bütçe)
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Hedefte", "Aşım").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('bt'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const tl = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const pct = v => v === null || v === undefined || !isFinite(v) ? '—' : (v * 100).toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%';
  const iso = d => d.toISOString().slice(0, 10);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const bos = '<span class="text-muted">—</span>';
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + iso(new Date()) + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 9000); }
  const GRUP = { sabit: 'Sabit Giderler', tedarik: 'Tedarik Zinciri · Hammadde', genel: 'Genel Giderler' };
  const grupAnahtar = g => Object.keys(GRUP).find(k => GRUP[k] === g) || 'genel';
  const grupRozet = g => `<span class="bt-grup ${grupAnahtar(g)}">${esc(g)}</span>`;
  const sapmaHtml = (v, b) => v === null || v === undefined ? bos : `<span class="bt-sapma ${v > 0.005 ? 'pozitif' : v < -0.005 ? 'negatif' : 'notr'}">${v > 0 ? '+' : ''}${kisa(v)}</span>`;
  const kalanHtml = v => v === null || v === undefined ? bos : `<span class="bt-sapma ${v > 0.005 ? 'negatif' : v < -0.005 ? 'pozitif' : 'notr'}">${v > 0 ? '+' : ''}${kisa(v)}</span>`;   // kalan: artı yeşil, eksi kırmızı
  const sapmaPctHtml = (g, b) => g === null || g === undefined || !b ? bos : (() => { const o = (g - b) / b; return `<span class="bt-sapma ${o > 0.005 ? 'pozitif' : o < -0.005 ? 'negatif' : 'notr'}">${o > 0 ? '+' : ''}${pct(o)}</span>`; })();
  const barHtml = (oran, durum) => `<span class="bt-bar" title="${pct(oran)}"><i class="${durum === 'Aşım' ? 'asim' : oran >= 0.9 ? 'riskli' : ''}" style="width:${Math.min(100, Math.round((oran || 0) * 100))}%"></i></span>${pct(oran)}`;
  const sekmeli = kok.dataset.sekme === '1';

  let V = null, F = { sirket: '', yil: +kok.dataset.yil, ay: 0, durum: '', altGrup: '', ara: '' }, acikKalem = null;
  const AY = () => V.aylar;
  const kapali = yil => V.kapanmisAy[String(yil)] || 0;
  const gecici = yil => V.geciciAy[String(yil)] || 0;
  const veriAy = yil => kapali(yil) + (gecici(yil) ? 1 : 0);        // gerçekleşeni olan ay sayısı
  const kalem = kod => V.kalemler.find(k => k.kod === kod);
  const uygulanir = (k, sirket) => !k.sirketler || k.sirketler.includes(sirket);

  // ---- toplama: kalem × (şirket filtresi) × yıl → aylık bütçe / gerçekleşen / ilk bütçe
  function topla(kod, sirket, yil) {
    const l = V.satirlar.filter(s => s.kalemKod === kod && s.yil === yil && (!sirket || s.sirket === sirket));
    const t = { butce: new Array(12).fill(0), ilkButce: new Array(12).fill(0), gercek: new Array(12).fill(null), satirlar: l };
    l.forEach(s => { for (let i = 0; i < 12; i++) { t.butce[i] += s.butce[i]; t.ilkButce[i] += s.ilkButce[i]; if (s.gercek[i] !== null && s.gercek[i] !== undefined) t.gercek[i] = (t.gercek[i] || 0) + s.gercek[i]; } });
    return t;
  }
  function ozet(t, yil, ay) {
    const n = veriAy(yil), o = { yillik: t.butce.reduce((a, b) => a + b, 0), ytdB: 0, ytdG: 0, ytdVar: n > 0 };
    for (let i = 0; i < n; i++) { o.ytdB += t.butce[i]; o.ytdG += t.gercek[i] || 0; }
    o.ytdS = o.ytdVar ? o.ytdG - o.ytdB : null; o.ytdO = o.ytdVar && o.ytdB ? o.ytdS / o.ytdB : null;
    o.ayB = t.butce[ay - 1]; o.ayG = t.gercek[ay - 1]; o.ayS = o.ayG === null ? null : o.ayG - o.ayB; o.ayO = o.ayG === null || !o.ayB ? null : o.ayS / o.ayB;
    o.kullanim = o.yillik ? o.ytdG / o.yillik : 0;
    o.durum = !o.ytdVar ? 'Veri Yok' : o.ytdO > 0.05 ? 'Aşım' : o.ytdO < -0.05 ? 'Tasarruf' : 'Hedefte';
    return o;
  }
  const durumRozet = d => rozet(d, d === 'Aşım' ? 'hata' : d === 'Tasarruf' ? 'ok' : d === 'Hedefte' ? 'bilgi' : 'notr');
  // ---- vadeli müşteri çekleri (vade ayına göre): kullanılabilir = Portföyde + Tahsilde + Tahsil Edildi
  const cekRozet = d => rozet(d, d === 'Tahsil Edildi' ? 'ok' : d === 'Karşılıksız' || d === 'Riskli' ? 'hata' : d === 'Ciro Edildi' ? 'notr' : d === 'Tahsilde' ? 'bilgi' : 'bekle');
  const cekListe = (yil, ay) => V.cekler.filter(c => c.yil === yil && (!ay || c.ay === ay) && (!F.sirket || c.sirket === F.sirket));
  function cekAy(yil, ay) {
    const l = cekListe(yil, ay), t = { adet: l.length, vadeli: 0, ciro: 0, riskli: 0, kullanilabilir: 0, tahsil: 0, portfoy: 0 };
    l.forEach(c => { t.vadeli += c.tutar; if (c.durum === 'Ciro Edildi') t.ciro += c.tutar; else if (c.durum === 'Karşılıksız' || c.durum === 'Riskli') t.riskli += c.tutar; if (c.kullanilabilir) t.kullanilabilir += c.tutar; if (c.durum === 'Tahsil Edildi') t.tahsil += c.tutar; if (c.durum === 'Portföyde' || c.durum === 'Tahsilde') t.portfoy += c.tutar; });
    return t;
  }
  function kalemler(grup) {
    const ara = F.ara.trim().toLocaleLowerCase('tr-TR');
    return V.kalemler.filter(k => (!grup || k.grup === grup) && (!F.sirket || uygulanir(k, F.sirket)) && (!F.altGrup || k.altGrup === F.altGrup))
      .map(k => ({ k, t: topla(k.kod, F.sirket, F.yil) })).map(x => ({ ...x, o: ozet(x.t, F.yil, F.ay) }))
      .filter(x => (!F.durum || x.o.durum === F.durum) && (!ara || [x.k.ad, x.k.hesapKodu, x.k.sorumlu, x.k.altGrup].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  const ayEtiket = (yil, ay) => AY()[ay - 1] + (ay <= kapali(yil) ? '' : ay === gecici(yil) ? ' (geçici)' : ' (açık)');

  // ---- sekmeler
  function sekme(p) {
    $$('#btSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('#bt > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    if (V) ciz(p);
    try { const u = new URL(location.href); if (p === 'ozet') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }
  $$('#btSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  const aktifSekme = () => ($('#btSekmeler button.aktif') || {}).dataset?.p || 'ozet';
  function ciz(p) { if (p === 'ozet') cizOzet(); else cizGrup(p); sayaclar(); }
  function hepsiniCiz() { ciz(aktifSekme()); if (acikKalem && $('#btDrawer').classList.contains('acik')) drawerAc(acikKalem, true); }

  // ---- veri
  async function yukle() {
    let r; try { r = await (await fetch('/Butce/Veri')).json(); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r;
    if (!F.ay) F.ay = F.yil === V.yil ? (gecici(V.yil) || kapali(V.yil) || 12) : 12;
    filtreleriKur();
  }
  function filtreleriKur() {
    const ys = $('#btYil'); ys.innerHTML = V.yillar.map(y => `<option value="${y}">${y}</option>`).join(''); ys.value = String(F.yil);
    ayDoldur();
    $('#btSirket').addEventListener('change', () => { F.sirket = $('#btSirket').value; hepsiniCiz(); });
    ys.addEventListener('change', () => { F.yil = +ys.value; F.ay = F.yil === V.yil ? (gecici(V.yil) || kapali(V.yil) || 12) : 12; ayDoldur(); hepsiniCiz(); });
    $('#btAy').addEventListener('change', () => { F.ay = +$('#btAy').value; hepsiniCiz(); });
    $('#btDurum').addEventListener('change', () => { F.durum = $('#btDurum').value; hepsiniCiz(); });
    $('#btAltGrup').addEventListener('change', () => { F.altGrup = $('#btAltGrup').value; hepsiniCiz(); });
    $('#btAra').addEventListener('input', () => { F.ara = $('#btAra').value; hepsiniCiz(); });
    $('#btTemizle').addEventListener('click', () => { F = { sirket: '', yil: V.yil, ay: gecici(V.yil) || kapali(V.yil) || 12, durum: '', altGrup: '', ara: '' }; $('#btSirket').value = ''; ys.value = String(F.yil); ayDoldur(); $('#btDurum').value = ''; $('#btAltGrup').value = ''; $('#btAra').value = ''; hepsiniCiz(); });
    $('#btExcel').addEventListener('click', () => { const p = aktifSekme(); if (p === 'ozet') ozetCsv(); else grupCsv(p); });
    $('#btOExcel').addEventListener('click', ozetCsv);
    $('#btKpi').addEventListener('click', e => { const k = e.target.closest('[data-durum]'); if (!k) return; F.durum = F.durum === k.dataset.durum ? '' : k.dataset.durum; $('#btDurum').value = F.durum; hepsiniCiz(); });
    $('#btDrawerKapat').addEventListener('click', drawerKapat); $('#btPerde').addEventListener('click', drawerKapat);
    const cd = $('#btCekDurum'); cd.innerHTML = '<option value="">Tüm Durumlar</option>' + V.cekDurumlari.map(d => `<option>${esc(d)}</option>`).join('');
    cd.addEventListener('change', cizCek);
    $('#btCekExcel').addEventListener('click', () => { const l = cekListe(F.yil, F.ay).filter(c => !cd.value || c.durum === cd.value); csvIndir('vadeli_cek_' + F.yil + '_' + F.ay, ['Çek No', 'Şirket', 'Müşteri Kodu', 'Müşteri', 'Banka', 'Vade', 'Tutar', 'Durum', 'Kullanılabilir'], l.map(c => [c.no, c.sirket, c.musteriKod, c.musteri, c.banka, c.vade, sayi(c.tutar), c.durum, c.kullanilabilir ? 'Evet' : 'Hayır'])); });
    altGrupDoldur();
  }
  function ayDoldur() { const s = $('#btAy'); s.innerHTML = AY().map((a, i) => `<option value="${i + 1}">${ayEtiket(F.yil, i + 1)}</option>`).join(''); s.value = String(F.ay); }
  function altGrupDoldur(grup) {
    const s = $('#btAltGrup'), mevcut = s.value; const l = [...new Set(V.kalemler.filter(k => !grup || k.grup === grup).map(k => k.altGrup))];
    s.innerHTML = '<option value="">Tümü</option>' + l.map(x => `<option>${esc(x)}</option>`).join(''); s.value = l.includes(mevcut) ? mevcut : ''; F.altGrup = s.value;
  }
  function sayaclar() {
    Object.keys(GRUP).forEach(k => { $('#btN-' + k).textContent = kalemler(GRUP[k]).length; });
    const y = F.yil, kp = kapali(y), gc = gecici(y);
    $('#btKapanisNot').innerHTML = `<i class="fas fa-lock me-1"></i>${y}: ${kp ? 'Ocak – ' + AY()[kp - 1] + ' kapanmış' : 'kapanmış ay yok'}${gc ? ' · ' + AY()[gc - 1] + ' geçici (kısmi)' : ''} · seçili ay ${AY()[F.ay - 1]}`;
    $('#btOKapsam').textContent = (F.sirket || 'Tüm Şirketler (Konsolide)') + ' · ' + y;
    $('#btOBaslik').textContent = y;
  }

  // ---- ÖZET
  function grupToplam(grup) {
    const l = kalemler(grup); const t = { butce: new Array(12).fill(0), gercek: new Array(12).fill(null) };
    l.forEach(x => { for (let i = 0; i < 12; i++) { t.butce[i] += x.t.butce[i]; if (x.t.gercek[i] !== null) t.gercek[i] = (t.gercek[i] || 0) + x.t.gercek[i]; } });
    return { t, o: ozet(t, F.yil, F.ay), n: l.length, asim: l.filter(x => x.o.durum === 'Aşım').length, tasarruf: l.filter(x => x.o.durum === 'Tasarruf').length };
  }
  function cizOzet() {
    const y = F.yil, n = veriAy(y), gc = gecici(y);
    const G = Object.keys(GRUP).map(k => ({ k, ad: GRUP[k], ...grupToplam(GRUP[k]) }));
    const T = { butce: new Array(12).fill(0), gercek: new Array(12).fill(null) };
    G.forEach(g => { for (let i = 0; i < 12; i++) { T.butce[i] += g.t.butce[i]; if (g.t.gercek[i] !== null) T.gercek[i] = (T.gercek[i] || 0) + g.t.gercek[i]; } });
    const TO = ozet(T, y, F.ay), asim = G.reduce((s, g) => s + g.asim, 0), tasarruf = G.reduce((s, g) => s + g.tasarruf, 0);
    const CA = cekAy(y, F.ay), ayButce = T.butce[F.ay - 1];
    const kpi = [
      { l: 'Yıllık Bütçe', v: kisa(TO.yillik), s: `${y} · ${G.reduce((s, g) => s + g.n, 0)} kalem`, k: '#0f172a' },
      { l: `YTD Bütçe (${n} Ay)`, v: kisa(TO.ytdB), s: n ? 'Ocak – ' + AY()[n - 1] : 'gerçekleşen yok', k: '#475569' },
      { l: 'YTD Gerçekleşen', v: kisa(TO.ytdG), s: gc && y === V.yil ? AY()[gc - 1] + ' geçici dahil' : 'kapanmış aylar', k: '#0f766e' },
      { l: 'YTD Sapma', v: (TO.ytdS > 0 ? '+' : '') + kisa(TO.ytdS), s: 'Gerçekleşen − Bütçe · ' + pct(TO.ytdO), k: TO.ytdS > 0 ? '#dc2626' : '#15803d' },
      { l: 'Kullanım', v: pct(TO.kullanim), s: 'YTD gerçekleşen / yıllık bütçe', k: '#2563eb' },
      { l: 'Aşımdaki Kalem', v: asim, s: `${tasarruf} kalem tasarrufta · tıkla: filtre`, k: '#dc2626', d: 'Aşım' },
      ...G.map(g => ({ l: g.ad, v: kisa(g.o.ytdG), s: `bütçe ${kisa(g.o.ytdB)} · sapma ${pct(g.o.ytdO)} · ${g.asim} aşım`, k: g.k === 'sabit' ? '#1d4ed8' : g.k === 'tedarik' ? '#b45309' : '#0f766e', git: g.k })),
      { l: `Kullanılabilir Çek · ${AY()[F.ay - 1]}`, v: kisa(CA.kullanilabilir), s: `${CA.adet} çek · bütçe karşılama ${pct(ayButce ? CA.kullanilabilir / ayButce : 0)}`, k: '#7c3aed', git: 'ozet', odak: '#btCekTablo' }
    ];
    $('#btKpi').innerHTML = kpi.map(k => `<div style="--k:${k.k};${k.d && F.durum === k.d ? 'background:#ecfdf5' : ''}" ${k.d ? `data-durum="${k.d}" title="Sapma durumu filtresi"` : ''} ${k.git ? `data-git="${k.git}|${k.odak || '#btTablo-' + k.git}" title="${esc(k.l)}"` : ''}><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
    // aylık Excel tablosu
    $('#btAylik thead').innerHTML = `<tr><th class="ad">Açıklama</th>${AY().map((a, i) => `<th class="s">${a}${i + 1 === gc && y === V.yil ? '<span class="bt-ay-gecici">geçici</span>' : i + 1 > n ? '<span class="bt-ay-gecici" style="color:#94a3b8">açık</span>' : ''}</th>`).join('')}<th class="s">Yıllık / YTD</th><th class="s">Kullanım</th></tr>`;
    const blok = (ad, t, o, cls) => {
      const h = v => v === null || v === undefined ? bos : kisa(v);
      return `<tr class="grp ${cls || ''}"><td class="ad">${esc(ad)}</td><td colspan="12"></td><td class="s">${durumRozet(o.durum)}</td><td class="s">${pct(o.kullanim)}</td></tr>
        <tr class="r"><td class="ad">Bütçe</td>${t.butce.map(v => `<td class="s">${h(v)}</td>`).join('')}<td class="s yil">${kisa(o.yillik)}</td><td class="s"></td></tr>
        <tr><td class="ad">Gerçekleşen</td>${t.gercek.map(v => `<td class="s">${h(v)}</td>`).join('')}<td class="s yil">${o.ytdVar ? kisa(o.ytdG) : '—'}</td><td class="s">${pct(o.kullanim)}</td></tr>
        <tr><td class="ad">Sapma</td>${t.gercek.map((v, i) => `<td class="s">${v === null ? bos : sapmaHtml(v - t.butce[i])}</td>`).join('')}<td class="s yil">${o.ytdVar ? sapmaHtml(o.ytdS) : '—'}</td><td class="s"></td></tr>
        <tr class="oran"><td class="ad">Sapma %</td>${t.gercek.map((v, i) => `<td class="s">${v === null ? bos : sapmaPctHtml(v, t.butce[i])}</td>`).join('')}<td class="s yil">${o.ytdVar ? sapmaPctHtml(o.ytdG, o.ytdB) : '—'}</td><td class="s"></td></tr>
        <tr class="ara"><td colspan="15"></td></tr>`;
    };
    // vadeli çek bloğu: her ay vadesi gelen müşteri çekleri ↔ o ayın gider bütçesi
    const C = AY().map((a, i) => cekAy(y, i + 1)), CT = C.reduce((s, c) => ({ adet: s.adet + c.adet, vadeli: s.vadeli + c.vadeli, ciro: s.ciro + c.ciro, riskli: s.riskli + c.riskli, kullanilabilir: s.kullanilabilir + c.kullanilabilir }), { adet: 0, vadeli: 0, ciro: 0, riskli: 0, kullanilabilir: 0 });
    const kalan = i => C[i].kullanilabilir - (T.gercek[i] !== null ? T.gercek[i] : T.butce[i]);
    const ytdKalan = C.slice(0, n).reduce((s, c, i) => s + kalan(i), 0);
    const cekBlok = `<tr class="grp"><td class="ad">Vadeli Çek · Kullanılabilir</td><td colspan="12" style="text-transform:none;font-weight:600">vadesi o aya düşen müşteri çekleri · bütçeyle karşılaştırma</td><td class="s">${CT.adet} çek</td><td class="s"></td></tr>
      <tr class="r"><td class="ad">Vadesi Gelen Müşteri Çeki</td>${C.map(c => `<td class="s">${c.adet ? kisa(c.vadeli) : bos}<small>${c.adet ? c.adet + ' çek' : ''}</small></td>`).join('')}<td class="s yil">${kisa(CT.vadeli)}</td><td class="s"></td></tr>
      <tr class="r"><td class="ad">Ciro Edilen (Tedarikçiye)</td>${C.map(c => `<td class="s">${c.ciro ? '−' + kisa(c.ciro) : bos}</td>`).join('')}<td class="s yil">−${kisa(CT.ciro)}</td><td class="s"></td></tr>
      <tr class="r"><td class="ad">Karşılıksız / Riskli</td>${C.map(c => `<td class="s">${c.riskli ? '<span class="bt-sapma pozitif">−' + kisa(c.riskli) + '</span>' : bos}</td>`).join('')}<td class="s yil">−${kisa(CT.riskli)}</td><td class="s"></td></tr>
      <tr class="genel"><td class="ad">Kullanılabilir Çek</td>${C.map(c => `<td class="s">${c.adet ? kisa(c.kullanilabilir) : bos}</td>`).join('')}<td class="s yil">${kisa(CT.kullanilabilir)}</td><td class="s"></td></tr>
      <tr class="oran"><td class="ad">Bütçe Karşılama %</td>${C.map((c, i) => `<td class="s">${T.butce[i] ? pct(c.kullanilabilir / T.butce[i]) : bos}</td>`).join('')}<td class="s yil">${pct(TO.yillik ? CT.kullanilabilir / TO.yillik : 0)}</td><td class="s"></td></tr>
      <tr><td class="ad">Gider Sonrası Kalan</td>${C.map((c, i) => `<td class="s">${c.adet ? kalanHtml(kalan(i)) : bos}</td>`).join('')}<td class="s yil" title="YTD: kullanılabilir çek − gerçekleşen">${kalanHtml(ytdKalan)}</td><td class="s"></td></tr>`;
    $('#btAylik tbody').innerHTML = G.map(g => blok(g.ad, g.t, g.o)).join('') + blok('TOPLAM GİDER', T, TO, 'genel') + cekBlok;
    cizCek();
    // şirket bazlı
    const sirketler = F.sirket ? [F.sirket] : V.sirketler;
    const SR = sirketler.map(s => { const l = V.kalemler.filter(k => uygulanir(k, s)).map(k => ({ k, t: topla(k.kod, s, y) })).map(x => ({ ...x, o: ozet(x.t, y, F.ay) })); const t = { butce: new Array(12).fill(0), gercek: new Array(12).fill(null) }; l.forEach(x => { for (let i = 0; i < 12; i++) { t.butce[i] += x.t.butce[i]; if (x.t.gercek[i] !== null) t.gercek[i] = (t.gercek[i] || 0) + x.t.gercek[i]; } }); const enY = l.filter(x => x.o.ytdS !== null).sort((a, b) => b.o.ytdS - a.o.ytdS)[0]; return { s, o: ozet(t, y, F.ay), asim: l.filter(x => x.o.durum === 'Aşım').length, enY }; });
    $('#btSirketTablo tbody').innerHTML = SR.map(r => `<tr class="tik" data-sirket="${esc(r.s)}"><td class="ad"><b>${esc(r.s)}</b></td><td class="s">${kisa(r.o.yillik)}</td><td class="s">${kisa(r.o.ytdB)}</td><td class="s">${kisa(r.o.ytdG)}</td><td class="s">${sapmaHtml(r.o.ytdS)}</td><td class="s">${sapmaPctHtml(r.o.ytdG, r.o.ytdB)}</td><td>${barHtml(r.o.kullanim, r.o.durum)}</td><td class="s">${r.asim}</td><td class="ad">${r.enY && r.enY.o.ytdS > 0 ? `${esc(r.enY.k.ad)}<small>+${kisa(r.enY.o.ytdS)} TL · ${pct(r.enY.o.ytdO)}</small>` : bos}</td></tr>`).join('');
    const ST = SR.reduce((a, r) => ({ yillik: a.yillik + r.o.yillik, ytdB: a.ytdB + r.o.ytdB, ytdG: a.ytdG + r.o.ytdG, asim: a.asim + r.asim }), { yillik: 0, ytdB: 0, ytdG: 0, asim: 0 });
    $('#btSirketTablo tfoot').innerHTML = `<tr><td class="ad">Toplam</td><td class="s">${kisa(ST.yillik)}</td><td class="s">${kisa(ST.ytdB)}</td><td class="s">${kisa(ST.ytdG)}</td><td class="s">${sapmaHtml(ST.ytdG - ST.ytdB)}</td><td class="s">${sapmaPctHtml(ST.ytdG, ST.ytdB)}</td><td>${barHtml(ST.yillik ? ST.ytdG / ST.yillik : 0, '')}</td><td class="s">${ST.asim}</td><td></td></tr>`;
    $$('#btSirketTablo tbody tr.tik').forEach(r => r.addEventListener('click', () => { F.sirket = F.sirket === r.dataset.sirket ? '' : r.dataset.sirket; $('#btSirket').value = F.sirket; hepsiniCiz(); }));
    // en yüksek sapmalar
    const S = kalemler('').filter(x => x.o.ytdS !== null && x.o.ytdS > 0).sort((a, b) => b.o.ytdS - a.o.ytdS).slice(0, 10);
    $('#btSapmaTablo tbody').innerHTML = S.length ? S.map(x => `<tr class="tik" data-kod="${x.k.kod}"><td class="ad"><b>${esc(x.k.ad)}</b><small>${esc(x.k.altGrup)} · ${esc(x.k.hesapKodu)}${x.k.not ? ' · ' + esc(x.k.not) : ''}</small></td><td>${grupRozet(x.k.grup)}</td><td>${esc(x.k.sorumlu)}</td><td class="s">${kisa(x.o.ytdB)}</td><td class="s">${kisa(x.o.ytdG)}</td><td class="s">${sapmaHtml(x.o.ytdS)}</td><td class="s">${sapmaPctHtml(x.o.ytdG, x.o.ytdB)}</td><td>${durumRozet(x.o.durum)}</td></tr>`).join('') : '<tr><td colspan="8" class="na-bos">Bütçeyi aşan kalem yok.</td></tr>';
    $$('#btSapmaTablo tbody tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(r.dataset.kod)));
    // revizyon günlüğü
    const R = V.revizyonlar.filter(r => r.yil === y && (!F.sirket || r.sirket === F.sirket));
    $('#btRevizyonTablo tbody').innerHTML = R.length ? R.map(r => `<tr><td>${esc(r.zaman)}</td><td>${esc(r.kullanici)}</td><td>${esc(r.sirket)}</td><td class="ad"><b>${esc(r.kalemAd)}</b><small>${esc(r.kalemKod)}</small></td><td>${esc(r.ayAd)} ${r.yil}</td><td class="s">${kisa(r.onceki)}</td><td class="s">${kisa(r.yeni)}</td><td class="s">${sapmaHtml(r.fark)}</td><td class="ad"><small style="display:inline">${esc(r.gerekce)}</small></td></tr>`).join('') : '<tr><td colspan="9" class="na-bos">Revizyon yok.</td></tr>';
  }
  // seçili ayın çek listesi + özet kartları
  function cizCek() {
    const y = F.yil, ay = F.ay, d = $('#btCekDurum').value, CA = cekAy(y, ay);
    const T = Object.keys(GRUP).map(k => grupToplam(GRUP[k]).t).reduce((a, t) => { for (let i = 0; i < 12; i++) { a.butce[i] += t.butce[i]; if (t.gercek[i] !== null) a.gercek[i] = (a.gercek[i] || 0) + t.gercek[i]; } return a; }, { butce: new Array(12).fill(0), gercek: new Array(12).fill(null) });
    const ayButce = T.butce[ay - 1], ayGider = T.gercek[ay - 1] !== null ? T.gercek[ay - 1] : ayButce, kalan = CA.kullanilabilir - ayGider;
    $('#btCekBaslik').textContent = `${AY()[ay - 1]} ${y} · ${F.sirket || 'tüm şirketler'}`;
    $('#btCekKpi').innerHTML = [
      { l: 'Vadesi Gelen Çek', v: kisa(CA.vadeli), s: `${CA.adet} çek`, k: '#475569' }, { l: 'Ciro Edilen', v: '−' + kisa(CA.ciro), s: 'tedarikçiye verildi', k: '#94a3b8' }, { l: 'Karşılıksız / Riskli', v: '−' + kisa(CA.riskli), s: 'tahsil edilemez', k: '#dc2626' },
      { l: 'Kullanılabilir Çek', v: kisa(CA.kullanilabilir), s: `portföy ${kisa(CA.portfoy)} · tahsil ${kisa(CA.tahsil)}`, k: '#7c3aed' }, { l: 'Bütçe Karşılama', v: pct(ayButce ? CA.kullanilabilir / ayButce : 0), s: `ay gider bütçesi ${kisa(ayButce)}`, k: '#0f766e' },
      { l: 'Gider Sonrası Kalan', v: (kalan > 0 ? '+' : '') + kisa(kalan), s: T.gercek[ay - 1] !== null ? 'gerçekleşen gidere göre' : 'bütçeye göre (ay açık)', k: kalan >= 0 ? '#15803d' : '#dc2626' }
    ].map(k => `<div style="--k:${k.k}"><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
    const l = cekListe(y, ay).filter(c => !d || c.durum === d);
    $('#btCekTablo tbody').innerHTML = l.length ? l.map(c => `<tr><td><b>${esc(c.no)}</b></td><td>${esc(c.sirket)}</td><td class="ad"><b>${esc(c.musteri)}</b><small>${esc(c.musteriKod)}</small></td><td>${esc(c.banka)}</td><td>${esc(c.vade)}</td><td class="s">${kisa(c.tutar)}</td><td>${cekRozet(c.durum)}</td><td>${c.kullanilabilir ? rozet('Evet', 'ok') : rozet('Hayır', 'hata')}</td></tr>`).join('') : '<tr><td colspan="8" class="na-bos">Seçili ayda vadeli çek yok.</td></tr>';
    const kt = l.filter(c => c.kullanilabilir).reduce((s, c) => s + c.tutar, 0);
    $('#btCekTablo tfoot').innerHTML = l.length ? `<tr><td colspan="5" class="ad" style="font-weight:800">Toplam · ${l.length} çek</td><td class="s" style="font-weight:800">${kisa(l.reduce((s, c) => s + c.tutar, 0))}</td><td></td><td style="font-weight:800">${kisa(kt)} kullanılabilir</td></tr>` : '';
  }
  function ozetCsv() {
    const rows = $$('#btAylik tbody tr').filter(tr => !tr.classList.contains('ara')).map(tr => [...tr.cells].map(c => c.textContent.trim()));
    csvIndir('butce_ozeti_' + F.yil, [...$$('#btAylik thead th')].map(th => th.textContent.replace(/geçici|açık/g, '').trim()), rows);
  }

  // ---- GİDER GRUBU SEKMELERİ
  function cizGrup(p) {
    const grup = GRUP[p], L = kalemler(grup), y = F.yil, n = veriAy(y);
    altGrupDoldur(grup);
    const G = grupToplam(grup);
    $('#btKpi-' + p).innerHTML = [
      { l: 'Yıllık Bütçe', v: kisa(G.o.yillik), s: `${L.length} kalem · ${y}`, k: '#0f172a' }, { l: `YTD Bütçe (${n} Ay)`, v: kisa(G.o.ytdB), s: n ? 'Ocak – ' + AY()[n - 1] : '—', k: '#475569' },
      { l: 'YTD Gerçekleşen', v: kisa(G.o.ytdG), s: 'kapanmış aylar' + (gecici(y) && y === V.yil ? ' + geçici' : ''), k: '#0f766e' }, { l: 'YTD Sapma', v: (G.o.ytdS > 0 ? '+' : '') + kisa(G.o.ytdS), s: pct(G.o.ytdO), k: G.o.ytdS > 0 ? '#dc2626' : '#15803d' },
      { l: 'Kullanım', v: pct(G.o.kullanim), s: 'yıllık bütçenin', k: '#2563eb' }, { l: 'Aşım / Tasarruf', v: `${G.asim} / ${G.tasarruf}`, s: 'kalem sayısı', k: '#b45309' }
    ].map(k => `<div style="--k:${k.k}"><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
    const t = $('#btTablo-' + p), ayAd = AY()[F.ay - 1];
    t.tHead.innerHTML = `<tr><th class="ad">Kalem</th><th>Hesap Kodu</th><th>Sorumlu</th><th class="s">${esc(ayAd)} Bütçe</th><th class="s">${esc(ayAd)} Gerçekleşen</th><th class="s">${esc(ayAd)} Sapma</th><th class="s">Sapma %</th><th class="s">YTD Bütçe</th><th class="s">YTD Gerçekleşen</th><th class="s">YTD Sapma</th><th class="s">YTD Sapma %</th><th class="s">Yıllık Bütçe</th><th>Kullanım</th><th>Durum</th></tr>`;
    const alt = [...new Set(L.map(x => x.k.altGrup))];
    const satir = x => `<tr class="tik" data-kod="${x.k.kod}"><td class="ad"><b>${esc(x.k.ad)}</b>${x.k.not ? `<small>${esc(x.k.not)}</small>` : ''}</td><td>${esc(x.k.hesapKodu)}</td><td>${esc(x.k.sorumlu)}</td><td class="s">${kisa(x.o.ayB)}</td><td class="s">${x.o.ayG === null ? bos : kisa(x.o.ayG)}</td><td class="s">${sapmaHtml(x.o.ayS)}</td><td class="s">${sapmaPctHtml(x.o.ayG, x.o.ayB)}</td><td class="s">${kisa(x.o.ytdB)}</td><td class="s">${x.o.ytdVar ? kisa(x.o.ytdG) : bos}</td><td class="s">${sapmaHtml(x.o.ytdS)}</td><td class="s">${x.o.ytdVar ? sapmaPctHtml(x.o.ytdG, x.o.ytdB) : bos}</td><td class="s">${kisa(x.o.yillik)}</td><td>${barHtml(x.o.kullanim, x.o.durum)}</td><td>${durumRozet(x.o.durum)}</td></tr>`;
    const araToplam = (ad, l) => { const s = l.reduce((a, x) => ({ ayB: a.ayB + x.o.ayB, ayG: a.ayG + (x.o.ayG || 0), ayVar: a.ayVar || x.o.ayG !== null, ytdB: a.ytdB + x.o.ytdB, ytdG: a.ytdG + x.o.ytdG, yillik: a.yillik + x.o.yillik }), { ayB: 0, ayG: 0, ayVar: false, ytdB: 0, ytdG: 0, yillik: 0 }); const ytdVar = n > 0; return `<tr class="grp"><td class="ad">${esc(ad)} <small style="display:inline;text-transform:none;font-weight:600">· ${l.length} kalem</small></td><td></td><td></td><td class="s">${kisa(s.ayB)}</td><td class="s">${s.ayVar ? kisa(s.ayG) : bos}</td><td class="s">${s.ayVar ? sapmaHtml(s.ayG - s.ayB) : bos}</td><td class="s">${s.ayVar ? sapmaPctHtml(s.ayG, s.ayB) : bos}</td><td class="s">${kisa(s.ytdB)}</td><td class="s">${ytdVar ? kisa(s.ytdG) : bos}</td><td class="s">${ytdVar ? sapmaHtml(s.ytdG - s.ytdB) : bos}</td><td class="s">${ytdVar ? sapmaPctHtml(s.ytdG, s.ytdB) : bos}</td><td class="s">${kisa(s.yillik)}</td><td>${barHtml(s.yillik ? s.ytdG / s.yillik : 0, '')}</td><td></td></tr>`; };
    t.tBodies[0].innerHTML = L.length ? alt.map(a => { const l = L.filter(x => x.k.altGrup === a); return araToplam(a, l) + l.map(satir).join(''); }).join('') : '<tr><td colspan="14" class="na-bos">Filtreye uyan kalem yok.</td></tr>';
    t.tFoot.innerHTML = L.length ? araToplam('Toplam · ' + grup, L).replace('class="grp"', '') : '';
    $$('tr.tik', t).forEach(r => r.addEventListener('click', () => drawerAc(r.dataset.kod)));
    $('#btNot-' + p).textContent = `${F.sirket || 'konsolide'} · ${y} · seçili ay ${ayEtiket(y, F.ay)} · YTD ${n} ay · ${L.length} kalem`;
  }
  function grupCsv(p) {
    const L = kalemler(GRUP[p]), ayAd = AY()[F.ay - 1];
    csvIndir('butce_' + p + '_' + F.yil, ['Grup', 'Alt Grup', 'Kalem', 'Hesap Kodu', 'Sorumlu', ayAd + ' Bütçe', ayAd + ' Gerçekleşen', ayAd + ' Sapma', 'YTD Bütçe', 'YTD Gerçekleşen', 'YTD Sapma', 'YTD Sapma %', 'Yıllık Bütçe', 'Kullanım %', 'Durum', 'Not'],
      L.map(x => [x.k.grup, x.k.altGrup, x.k.ad, x.k.hesapKodu, x.k.sorumlu, sayi(x.o.ayB), x.o.ayG === null ? '' : sayi(x.o.ayG), x.o.ayS === null ? '' : sayi(x.o.ayS), sayi(x.o.ytdB), x.o.ytdVar ? sayi(x.o.ytdG) : '', x.o.ytdVar ? sayi(x.o.ytdS) : '', x.o.ytdO === null ? '' : (x.o.ytdO * 100).toFixed(1).replace('.', ','), sayi(x.o.yillik), (x.o.kullanim * 100).toFixed(1).replace('.', ','), x.o.durum, x.k.not]));
  }

  // ---- DETAY (drawer): aylık kırılım, şirket kırılımı, revizyon
  function drawerAc(kod, sessiz) {
    const k = kalem(kod); if (!k) return; acikKalem = kod;
    const y = F.yil, n = veriAy(y), kp = kapali(y), gc = gecici(y), t = topla(kod, F.sirket, y), o = ozet(t, y, F.ay);
    $('#btDrawerBaslik').innerHTML = `${esc(k.ad)} ${grupRozet(k.grup)} ${durumRozet(o.durum)}`;
    $('#btDrawerAksiyon').innerHTML = `<button type="button" class="btn btn-light btn-sm" id="btDAylikExcel" style="font-size:.66rem"><i class="fas fa-file-excel me-1"></i>Aylık Excel</button>`;
    let kum = { b: 0, g: 0 };
    const aylik = AY().map((a, i) => { const b = t.butce[i], g = t.gercek[i], ilk = t.ilkButce[i]; kum.b += b; if (g !== null) kum.g += g; const durum = i < kp ? rozet('Kapalı', 'notr') : i + 1 === gc && y === V.yil ? rozet('Geçici', 'bekle') : rozet('Açık', 'bilgi'); return `<tr><td class="ad">${a}</td><td class="s">${kisa(b)}${ilk !== b ? `<small title="ilk bütçe ${kisa(ilk)}">revize (ilk ${kisa(ilk)})</small>` : ''}</td><td class="s">${g === null ? bos : kisa(g)}</td><td class="s">${g === null ? bos : sapmaHtml(g - b)}</td><td class="s">${g === null ? bos : sapmaPctHtml(g, b)}</td><td class="s">${kisa(kum.b)}</td><td class="s">${g === null ? bos : kisa(kum.g)}</td><td class="s">${g === null ? bos : sapmaHtml(kum.g - kum.b)}</td><td>${durum}</td></tr>`; }).join('');
    const sirketler = (F.sirket ? [F.sirket] : V.sirketler).filter(s => uygulanir(k, s));
    const sirketSatir = sirketler.map(s => { const ts = topla(kod, s, y), os = ozet(ts, y, F.ay); return `<tr><td class="ad"><b>${esc(s)}</b></td><td class="s">${kisa(os.yillik)}</td><td class="s">${kisa(os.ytdB)}</td><td class="s">${os.ytdVar ? kisa(os.ytdG) : bos}</td><td class="s">${sapmaHtml(os.ytdS)}</td><td class="s">${os.ytdVar ? sapmaPctHtml(os.ytdG, os.ytdB) : bos}</td><td>${barHtml(os.kullanim, os.durum)}</td><td>${durumRozet(os.durum)}</td></tr>`; }).join('');
    const acikAylar = AY().map((a, i) => ({ a, i })).filter(x => x.i + 1 > kp);
    const R = V.revizyonlar.filter(r => r.kalemKod === kod && (!F.sirket || r.sirket === F.sirket));
    $('#btDrawerIc').innerHTML = `
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-circle-info me-1"></i>Kalem Bilgileri</span><span class="not">${esc(k.kod)} · ${esc(F.sirket || 'konsolide')} · ${y}</span></div><div class="na-kart-g">
        <div class="bt-ozet"><div><div class="l">Gider Grubu · Alt Grup</div><div class="v">${esc(k.grup)} · ${esc(k.altGrup)}</div></div><div><div class="l">Hesap Kodu</div><div class="v">${esc(k.hesapKodu)}</div></div><div><div class="l">Bütçe Sahibi</div><div class="v">${esc(k.sorumlu)}</div></div><div><div class="l">Uygulanan Şirketler</div><div class="v">${esc((k.sirketler || V.sirketler).join(', '))}</div></div>
        <div><div class="l">Yıllık Bütçe</div><div class="v">${kisa(o.yillik)} TL</div></div><div><div class="l">YTD Bütçe / Gerçekleşen (${n} ay)</div><div class="v">${kisa(o.ytdB)} / ${o.ytdVar ? kisa(o.ytdG) : '—'}</div></div><div><div class="l">YTD Sapma</div><div class="v">${sapmaHtml(o.ytdS)} ${o.ytdVar ? '(' + pct(o.ytdO) + ')' : ''}</div></div><div><div class="l">Kullanım</div><div class="v">${barHtml(o.kullanim, o.durum)}</div></div></div>
        ${k.not ? `<div class="bt-uyari mavi mt-2 mb-0"><i class="fas fa-lightbulb me-1"></i><b>Bütçe varsayımı / açıklama:</b> ${esc(k.not)}</div>` : ''}
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-calendar-days me-1"></i>Aylık Kırılım · ${y}</span><span class="not">kümülatif = yıl başından itibaren · revize edilen aylarda ilk bütçe gösterilir</span></div><div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo bt-tablo" id="btDAylik" data-dinamik="kapali"><thead><tr><th class="ad">Ay</th><th class="s">Bütçe</th><th class="s">Gerçekleşen</th><th class="s">Sapma</th><th class="s">Sapma %</th><th class="s">Küm. Bütçe</th><th class="s">Küm. Gerçekleşen</th><th class="s">Küm. Sapma</th><th>Ay Durumu</th></tr></thead><tbody>${aylik}</tbody><tfoot><tr><td class="ad">Toplam</td><td class="s">${kisa(o.yillik)}</td><td class="s">${o.ytdVar ? kisa(o.ytdG) : bos}</td><td class="s">${sapmaHtml(o.ytdS)}</td><td class="s">${o.ytdVar ? sapmaPctHtml(o.ytdG, o.ytdB) : bos}</td><td colspan="4"></td></tr></tfoot></table></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-building me-1"></i>Şirket Kırılımı</span><span class="not">${sirketler.length} şirket · YTD</span></div><div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo bt-tablo" data-dinamik="kapali"><thead><tr><th class="ad">Şirket</th><th class="s">Yıllık Bütçe</th><th class="s">YTD Bütçe</th><th class="s">YTD Gerçekleşen</th><th class="s">Sapma</th><th class="s">Sapma %</th><th>Kullanım</th><th>Durum</th></tr></thead><tbody>${sirketSatir}</tbody></table></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-pen-to-square me-1"></i>Bütçe Revizyonu</span><span class="not">yalnız açık aylar · gerekçe zorunlu · günlüğe yazılır</span></div><div class="na-kart-g">
        ${acikAylar.length ? `<div class="bt-rev-form"><div><label>Şirket</label><select class="form-select" id="btRSirket">${(F.sirket ? [F.sirket] : ['', ...sirketler]).map(s => `<option value="${esc(s)}">${s ? esc(s) : 'Seçiniz'}</option>`).join('')}</select></div><div><label>Ay</label><select class="form-select" id="btRAy">${acikAylar.map(x => `<option value="${x.i + 1}">${x.a}${x.i + 1 === gc && y === V.yil ? ' (geçici)' : ''}</option>`).join('')}</select></div><div><label>Yeni Bütçe (TL)</label><input class="form-control" id="btRTutar" type="number" min="0" step="1000" placeholder="mevcut: seçince dolar"></div><div><label>Gerekçe <span class="z">*</span></label><input class="form-control" id="btRGerekce" placeholder="Revizyon nedeni (fiyat artışı, yönetim kararı, hacim değişikliği…)"></div><div><button type="button" class="btn btn-primary btn-sm" id="btRKaydet" style="font-size:.7rem;height:28px"><i class="fas fa-floppy-disk me-1"></i>Revize Et</button></div></div><div class="na-mini" id="btRMevcut"></div>` : '<div class="bt-uyari sari mb-0">Bu yılın tüm ayları kapanmış; revizyon yapılamaz.</div>'}
        <div class="mt-2">${R.length ? `<div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih-Saat</th><th>Kullanıcı</th><th>Şirket</th><th>Ay</th><th class="s">Önceki</th><th class="s">Yeni</th><th class="s">Fark</th><th>Gerekçe</th></tr></thead><tbody>${R.map(r => `<tr><td>${esc(r.zaman)}</td><td>${esc(r.kullanici)}</td><td>${esc(r.sirket)}</td><td>${esc(r.ayAd)} ${r.yil}</td><td class="s">${kisa(r.onceki)}</td><td class="s">${kisa(r.yeni)}</td><td class="s">${sapmaHtml(r.fark)}</td><td class="ad">${esc(r.gerekce)}</td></tr>`).join('')}</tbody></table></div>` : '<div class="na-bos">Bu kalemde revizyon yok.</div>'}</div>
      </div></div>`;
    $('#btDAylikExcel').addEventListener('click', () => csvIndir('butce_' + kod + '_' + y, [...$$('#btDAylik thead th')].map(th => th.textContent.trim()), $$('#btDAylik tbody tr').map(tr => [...tr.cells].map(c => c.textContent.trim()))));
    const rs = $('#btRSirket'), ra = $('#btRAy'), rt = $('#btRTutar');
    if (rs) {
      const mevcut = () => { const s = V.satirlar.find(x => x.sirket === rs.value && x.yil === y && x.kalemKod === kod); const ay = +ra.value; if (!s) { $('#btRMevcut').textContent = 'Şirket seçince mevcut bütçe gösterilir.'; return; } rt.value = s.butce[ay - 1]; $('#btRMevcut').textContent = `${rs.value} · ${AY()[ay - 1]} ${y} mevcut bütçe ${kisa(s.butce[ay - 1])} TL (ilk bütçe ${kisa(s.ilkButce[ay - 1])} TL)`; };
      rs.addEventListener('change', mevcut); ra.addEventListener('change', mevcut); mevcut();
      $('#btRKaydet').addEventListener('click', async () => {
        const g = { sirket: rs.value, yil: y, kalemKod: kod, ay: +ra.value, tutar: rt.value === '' ? null : +rt.value, gerekce: $('#btRGerekce').value.trim() };
        if (!g.sirket) { mesaj('warning', 'Revizyon için şirket seçin.'); return; }
        let r; try { r = await (await fetch('/Butce/Revize', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(g) })).json(); } catch (e) { r = { success: false, message: 'Sunucuya ulaşılamadı.' }; }
        mesaj(r.success ? 'success' : 'warning', esc(r.message));
        if (r.success) { const i = V.satirlar.findIndex(x => x.sirket === r.satir.sirket && x.yil === r.satir.yil && x.kalemKod === r.satir.kalemKod); if (i >= 0) V.satirlar[i] = r.satir; V.revizyonlar = r.revizyonlar; hepsiniCiz(); }
      });
    }
    $('#btDrawer').classList.add('acik'); $('#btPerde').classList.add('acik'); if (!sessiz) $('#btDrawer').scrollTop = 0;
  }
  function drawerKapat() { $('#btDrawer').classList.remove('acik'); $('#btPerde').classList.remove('acik'); acikKalem = null; }

  // ---- başlangıç
  (async () => {
    await yukle();
    if (!V) return;
    sekme(kok.dataset.baslangic || 'ozet');
    if (kok.dataset.kalem) drawerAc(kok.dataset.kalem);
  })();
})();
