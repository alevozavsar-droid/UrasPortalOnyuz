// ============================================================
// KASA NAKİT AKIŞI — modern ekran betiği (Views/Rapor79/Index.cshtml; veri /Rapor79/Veri; kaynak Data/NakitAkisOrnek.cs + CekAkibetOrnek.cs + TalimatOrnek.cs + MakbuzOrnek.cs + MuhasebeOrnek.cs + Mahsup / Temlik / Yansıtma / Nakit Avans modülleri)
// Sekmeler ödeme yöntemine göre; her sekmede yalnız o yöntemin sütunları (KOL). Her sekmede: planlanan ödeme tarihi, son ödeme tarihi, belge no (tıklanınca belge detayı), fatura sahibi ve ödeyen şirket ayrı sütun.
// Üst kartlar tıklanınca filtre (ödenen, kalan, bu hafta, geciken, onay bekleyen, mükerrer, avans). Satır içi düzenleme (.kn-alan → AlanKaydet), çekmece (tüm alanlar + işlem çubuğu: tablodaki her işlem çekmeceden de yapılır).
// Satır seçimi (.kn-sec) → "Seçilenlerden Talimat Oluştur" (TalimatOlustur; Talimatlar sekmesi). Çek / Senet: portföy görünümü (SecilebilirBelgeler) → BelgePlanla, "Planla ve Dolaşımı Planla" ile dolaşım modalı sırayla açılır;
// dolaşım modalı (DolasimOner / DolasimSimule / DolasimKaydet / DolasimAdim / DolasimIptal). Plansız çek çıkışı (Çek Akıbeti'nden) GMY onayı satırı olarak düşer (CekCikisOnay).
// Nakit: talep → onay (NakitOnay; Alev Özavşar / Neslihan Uludağ / Özge İsen) → makbuz (MakbuzOlustur; satır ÖDENDİ, muhasebe fişi otomatik). Kısmi ödeme / taksit (Taksitlendir), mükerrer uyarısı (kayıt öncesi + satırda rozet), belge seç (CariBelgeler: fatura / sipariş / cari bakiye).
// Ödeme: çek / senet satırında "Çeki Ver / İbraz Et" (CekIbraz: tedarikçiye ciro / bankaya tahsil, satır ÖDENDİ, fişler); Havale: alıcı banka · IBAN · hesap adı (CariBilgi → cari karttan) → banka talimatı; ekler (EkYukle: dekont, imzalı makbuz / talimat, ciro bordrosu; Ek / TalimatEk ile açılır).
// Dövizli ödeme: kur kaynağı (TCMB alış / satış / efektif → /Rapor79/Kur ile o günün kuru otomatik; anlaşmalı / fatura kuru elle; çekte tahsil günü kuru), kur günü, kur farkı faturası. Fatura sahibi ≠ ödeyen: otomatik grup içi Mahsup / Temlik belgesi (GrupIciTur; Mahsup / Temlik sekmelerinde liste, yazdırılabilir belge, iki şirkette fiş).
// Yazım: liste değerleri ve etiketler Title Case.
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('kn'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const para = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const tarih = s => { if (!s) return '—'; const p = String(s).slice(0, 10).split('-'); return p.length === 3 ? `${p[2]}.${p[1]}.${p[0]}` : s; };
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const bos = '<span class="text-muted">—</span>';
  const pbAd = pb => !pb || pb === 'TRY' ? '₺' : pb;
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + new Date().toISOString().slice(0, 10) + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 10000); }
  const csrf = ($('input[name="__RequestVerificationToken"]') || {}).value || '';
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrf }, body: JSON.stringify(body) }).then(r => r.json());
  const getJ = url => fetch(url).then(r => r.json());
  const modalKapat = el => { const m = bootstrap.Modal.getOrCreateInstance(el); if (el.classList.contains('show') && !m._isTransitioning) m.hide(); else el.addEventListener('shown.bs.modal', () => m.hide(), { once: true }); };
  const modalAc = el => bootstrap.Modal.getOrCreateInstance(el).show();
  const SEKME = { tumu: '', nakit: 'Nakit', cek: 'Çek', senet: 'Senet', havale: 'Havale / EFT', kredi: 'Kredi Kartı / Banka Kartı', mahsup: 'Mahsup', temlik: 'Temlik', yansitma: 'Yansıtma', diger: 'Diğer' };
  const YKEY = y => Object.keys(SEKME).find(k => SEKME[k] === y) || 'diger';
  const yontemRozet = y => `<span class="kn-yontem ${YKEY(y)}">${esc(y === 'Kredi Kartı / Banka Kartı' ? 'Kredi / Banka Kartı' : y || '—')}</span>`;
  const DURUM_SINIF = { 'ÖDENDİ': 'odendi', 'Gecikti': 'gecikti', 'Özel Onay': 'ozel', 'Planlandı': 'planli', 'Bekliyor': '' };
  const durumRozet = d => rozet(d, d === 'ÖDENDİ' ? 'ok' : d === 'Gecikti' ? 'hata' : d === 'Planlandı' ? 'bilgi' : d === 'Özel Onay' ? 'notr' : 'bekle');
  const kontrolRozet = (k, x) => k === 'Uyumlu' ? '<span class="na-bs kn-kontrol-uyumlu" title="Planlanan tedarikçiye çıktı">Uyumlu</span>' : k === 'Farklı' ? `<span class="na-bs kn-kontrol-farkli" title="DİKKAT: plan ${esc(x?.planlananTedarikci)}, çıkış ${esc(x?.ibrazYeri)}">FARKLI → ${esc(x?.ibrazYeri)}</span>` : k === 'İbraz Bekliyor' ? '<span class="na-bs kn-kontrol-bekliyor">İbraz Bekliyor</span>' : k === 'Plansız İbraz' ? '<span class="na-bs kn-kontrol-plansiz">Plansız İbraz</span>' : '';
  const akibetRozet = a => rozet(a || '—', a === 'Planlandı' ? 'bilgi' : a === 'Ciro Edildi' ? 'notr' : (a || '').includes('Tahsil') ? 'ok' : a === 'Karşılıksız' ? 'hata' : 'bekle');
  const modulRozet = d => rozet(d || '—', /Muhasebeleşti|Tamamlandı|Uyumlu|Mutabakat Oluştu|Fatura Oluştu|Teslim Edildi|Onaylandı$/.test(d || '') ? 'ok' : /Reddedildi|İptal|Hata|Ters/.test(d || '') ? 'hata' : /Onay Bekliyor|İmza|Taslak|Bekliyor/.test(d || '') ? 'bekle' : 'bilgi');
  const talimatRozet = (t, tekSatir) => t ? `<span class="kn-talimat-rozet kn-talimat-ac" data-no="${esc(t.no)}" title="${esc(t.baslik)} · ${esc(t.durum)} · Talimatlar sekmesinde aç">${esc(t.no)}${tekSatir ? ' · ' + esc(t.durum) : ''}</span>${tekSatir ? '' : `<small>${esc(t.durum)}</small>`}` : '';
  const onayRozet = o => !o ? '' : o.tip === 'cekCikis' ? `<span class="kn-onay ${o.durum === 'Onaylandı' ? 'onayli' : o.durum === 'Reddedildi' ? 'red' : 'gmy'}" title="${esc(o.durum)}${o.onaylayan ? ' · ' + esc(o.onaylayan) : ''}${o.tarih ? ' · ' + esc(o.tarih) : ''}${o.not ? ' · ' + esc(o.not) : ''}">${o.durum === 'Onay Bekliyor' ? 'GMY Onayı Bekliyor' : esc(o.durum)}</span>` : `<span class="kn-onay ${o.durum === 'Onaylandı' ? 'onayli' : o.durum === 'Reddedildi' ? 'red' : 'bekliyor'}" title="${esc(o.durum)}${o.onaylayan ? ' · ' + esc(o.onaylayan) : ''}${o.tarih ? ' · ' + esc(o.tarih) : ''}${o.not ? ' · ' + esc(o.not) : ''}">${esc(o.durum)}</span>`;
  const belgeCikmis = x => x.akibet && x.akibet !== 'Planlandı' && x.akibet !== 'Portföyde';
  const mukerrerVar = r => !!(r.plan && r.plan.uyarilar && r.plan.uyarilar.length);
  const mukerrerRozet = r => mukerrerVar(r) ? `<span class="kn-mukerrer kn-belge-detay" title="${esc(r.plan.uyarilar.map(u => u.tip + ': ' + u.aciklama).join(' · '))}">MÜKERRER?</span>` : '';
  const onayBekliyor = r => r.onay && r.onay.durum === 'Onay Bekliyor';

  let V = null, F = { sirket: kok.dataset.sirket || '', bas: kok.dataset.bas, bit: kok.dataset.bit, durum: '', satinalmaci: '', ara: '', kpi: '' }, acik = null;
  const aktifSekme = () => ($('#knSekmeler button.aktif') || {}).dataset?.p || 'tumu';
  const bugun = () => V ? V.bugun : new Date().toISOString().slice(0, 10);
  const haftaSonu = () => { const h = new Date(bugun()); h.setDate(h.getDate() + 7); return h.toISOString().slice(0, 10); };
  const gecikmis = r => r.durum !== 'ÖDENDİ' && (r.durum === 'Gecikti' || (r.planlanan && r.planlanan < bugun()) || (r.sonOdeme && r.sonOdeme < bugun()));
  const KPI_F = {
    odenen: r => r.durum === 'ÖDENDİ', kalan: r => r.durum !== 'ÖDENDİ', hafta: r => r.durum !== 'ÖDENDİ' && r.planlanan && r.planlanan >= bugun() && r.planlanan <= haftaSonu(), geciken: gecikmis,
    onay: onayBekliyor, mukerrer: mukerrerVar, avans: r => !!r.avansNo, talimatli: r => !!r.talimatNo && r.durum !== 'ÖDENDİ'
  };
  const kisiUyar = r => !F.satinalmaci || r.satinalmaci === F.satinalmaci || r.talepEden === F.satinalmaci;
  const satirlar = () => V.odemeler.filter(r => (!F.durum || r.durum === F.durum) && kisiUyar(r) && (!F.kpi || !KPI_F[F.kpi] || KPI_F[F.kpi](r)) && aramaUyar(r));
  const aramaUyar = r => { const a = F.ara.trim().toLocaleLowerCase('tr-TR'); return !a || [r.kurum, r.faturaNo, r.urun, r.notlar, r.cekNolar, r.sirket, r.faturaSahibi, r.odeyenSirket, r.talimatNo, r.avansNo, r.belgeTipi, r.yontem, r.durum, r.onay?.durum, r.onay?.onaylayan, r.makbuz?.no, r.yevmiyeNo, r.grupIci?.no, r.grupIci?.karsi, r.alanlar?.dekontNo, r.alanlar?.aliciIban, r.alanlar?.aliciBanka, r.alanlar?.kasa, r.grupIciBelge?.no, r.ibraz?.belgeNo, r.kurBilgi?.kaynak, String(r.docEntry)].join(' ').toLocaleLowerCase('tr-TR').includes(a); };
  const satirBul = d => V.odemeler.find(r => r.docEntry === d);
  const satirYerlestir = row => { if (!row) return; const i = V.odemeler.findIndex(r => r.docEntry === row.docEntry); if (i >= 0) V.odemeler[i] = row; else V.odemeler.push(row); };
  const satirKaldir = d => { V.odemeler = V.odemeler.filter(r => r.docEntry !== d); };
  const sirketAdi = db => (V.sirketler.find(x => x.dbName === db) || {}).ad || db;
  const dSirketler = () => (V.grupSirketleri || V.sirketler.map(s => s.ad));

  // ---------- yükleme
  async function yukle(sessiz) {
    if (!sessiz) $('#knKapsam').textContent = 'yükleniyor…';
    let r; try { r = await getJ(`/Rapor79/Veri?sirket=${encodeURIComponent(F.sirket)}&bas=${F.bas}&bit=${F.bit}`); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r; A.veri = null;
    const d = $('#knDurum'), mevcutD = d.value; d.innerHTML = '<option value="">Tümü</option>' + V.durumlar.map(x => `<option>${esc(x)}</option>`).join(''); d.value = mevcutD;
    // Temsilci / Talep Eden: satınalma temsilcileri + satırlardaki talep edenler
    const kisiler = [...new Set([...V.satinalmacilar, ...V.odemeler.map(r => r.talepEden)])].filter(Boolean).sort((a, b) => a.localeCompare(b, 'tr'));
    const s = $('#knSatinalmaci'), mevcutS = s.value; s.innerHTML = '<option value="">Tümü</option>' + kisiler.map(x => `<option>${esc(x)}</option>`).join(''); s.value = mevcutS;
    ciz();
  }
  // sabit başlıklar için üst boşluk: sayfanın üstünde sabit duran çubukların (top-header, sabit araç çubuğu) alt kenarı + kart başlığı yüksekliği
  function sabitUstOlc() {
    // kabuğun rapor kutusu (.table-container, overflow:auto) yapışkan başlığı kendi içinde tutar; pencereye göre çalışsın diye açılır
    const tk = kok.closest('.table-container'); if (tk) tk.style.setProperty('overflow', 'visible', 'important');
    let ust = 0; const band = 220;
    for (const e of document.querySelectorAll('.top-header, .toolbar-container, .rt-cubuk, [class*="sticky"]')) { if (kok.contains(e)) continue; const cs = getComputedStyle(e); if (cs.position !== 'sticky' && cs.position !== 'fixed') continue; const r = e.getBoundingClientRect(); if (r.width < 300 || r.top < -5 || r.top > band || r.height === 0) continue; ust = Math.max(ust, r.bottom); }
    kok.style.setProperty('--kn-sabit-ust', Math.round(ust) + 'px');
    const kb = $('.na-panel.aktif > .na-kart > .na-kart-b'); if (kb) kok.style.setProperty('--kn-kartb', kb.offsetHeight + 'px');
  }
  window.addEventListener('resize', sabitUstOlc);
  function ciz() {
    if (!V) return; if (typeof talepRozet === 'function') talepRozet();
    const L = satirlar();
    $('#knKapsam').textContent = `${F.sirket ? sirketAdi(F.sirket) : 'Tüm Şirketler (Konsolide)'} · ${tarih(F.bas)} – ${tarih(F.bit)} · ${L.length} kayıt${F.kpi ? ' · kart filtresi açık' : ''}`;
    kpi(); sayaclar(L);
    const p = aktifSekme();
    if (p === 'bakiye') cizBakiye(); else if (p === 'gunluk') cizGunluk(); else if (p === 'talimat') cizTalimatlar(); else if (p === 'aylik') cizAylik(); else cizTablo(p, L);
    if (p === 'mahsup' || p === 'temlik' || p === 'yansitma') cizGrupOzet(p);
    if (p === 'mahsup' || p === 'temlik') cizGib(p);
    if (window.scrollY < 5) sabitUstOlc(); else { const kb = $('.na-panel.aktif > .na-kart > .na-kart-b'); if (kb) kok.style.setProperty('--kn-kartb', kb.offsetHeight + 'px'); }
  }
  function sayaclar(L) { Object.keys(SEKME).forEach(k => { const n = $('#knN-' + k); if (n) n.textContent = k === 'tumu' ? L.length : L.filter(r => r.yontem === SEKME[k]).length; }); $('#knN-bakiye').textContent = V.bakiyeler.length; $('#knN-gunluk').textContent = V.gunluk.length; $('#knN-talimat').textContent = (V.talimatlar || []).filter(t => !t.kapandi).length + '/' + (V.talimatlar || []).length; const na = $('#knN-aylik'); if (na) na.textContent = A.yil; }
  // üst kartlar: temel filtreler (durum / satınalmacı / arama) uygulanmış küme üzerinden; tıklanan kart ek filtre olur
  function kpi() {
    const T = V.odemeler.filter(r => (!F.durum || r.durum === F.durum) && kisiUyar(r) && aramaUyar(r));
    const odenmemis = T.filter(r => r.durum !== 'ÖDENDİ'), odenen = T.filter(KPI_F.odenen), hafta = T.filter(KPI_F.hafta), geciken = T.filter(KPI_F.geciken), onay = T.filter(KPI_F.onay), muk = T.filter(KPI_F.mukerrer), av = T.filter(KPI_F.avans);
    const tl = l => l.reduce((s, r) => s + r.tutarTL, 0), B = V.bakiyeler, nakit = B.filter(b => b.bankaKasa === 'BANKA' || b.bankaKasa === 'KASA').reduce((s, b) => s + b.tutarTL, 0), portfoy = B.filter(b => b.bankaKasa.includes('PORTFÖY')).reduce((s, b) => s + b.tutarTL, 0);
    const portX = B.filter(b => b.bankaKasa.includes('PORTFÖY') && b.bankaKasa.includes('(X)')).reduce((s, b) => s + b.tutarTL, 0), portR = B.filter(b => b.bankaKasa.includes('PORTFÖY') && b.bankaKasa.includes('(R)')).reduce((s, b) => s + b.tutarTL, 0);
    const cekBekleyen = odenmemis.filter(r => r.yontem === 'Çek' || r.yontem === 'Senet');
    const K = [
      { k: '#0f766e', l: 'Dönem Toplamı', v: kisa(tl(T)), s: `${T.length} kayıt · ${odenmemis.length} açık · tıkla: filtreyi kaldır`, f: '' },
      { k: '#15803d', l: 'Ödenen', v: kisa(tl(odenen)), s: `${odenen.length} kayıt`, f: 'odenen' },
      { k: '#b45309', l: 'Kalan Ödenecek', v: kisa(tl(odenmemis)), s: `${odenmemis.length} kayıt · çek/senet ${kisa(tl(cekBekleyen))}`, f: 'kalan' },
      { k: '#7c3aed', l: 'Bu Hafta Vadesi Gelen', v: kisa(tl(hafta)), s: `${hafta.length} ödeme · ${tarih(bugun())} – ${tarih(haftaSonu())}`, f: 'hafta' },
      { k: '#dc2626', l: 'Geciken', v: kisa(tl(geciken)), s: `${geciken.length} ödeme · plan ya da son ödeme tarihi geçmiş`, f: 'geciken' },
      { k: '#9d174d', l: 'Onay Bekleyen', v: kisa(tl(onay)), s: `${onay.filter(r => r.onay.tip === 'nakit').length} nakit talebi · ${onay.filter(r => r.onay.tip === 'cekCikis').length} GMY çek çıkışı`, f: 'onay' },
      { k: '#991b1b', l: 'Mükerrer Uyarısı', v: String(muk.length), s: muk.length ? `${kisa(tl(muk))} ₺ · aynı belge / fazla ödeme` : 'uyarı yok', f: 'mukerrer' },
      { k: '#1d4ed8', l: 'Banka + Kasa Mevcudu', v: kisa(nakit), s: `${B.filter(b => b.bankaKasa === 'BANKA').length} banka hesabı · tıkla: bakiye`, p: 'bakiye' },
      { k: '#0369a1', l: 'Çek / Senet Portföyü', v: kisa(portfoy), s: `X ${kisa(portX)} · R ${kisa(portR)} · tıkla: bakiye`, p: 'bakiye' },
      { k: '#b45309', l: 'Avans Satırları', v: kisa(tl(av.filter(r => r.durum !== 'ÖDENDİ'))), s: `${(V.avans || {}).bekleyen || 0} ödeme bekleyen avans · ${av.length} satır`, f: 'avans' }
    ];
    $('#knKpi').innerHTML = K.map(k => `<div class="${k.f !== undefined && k.f === F.kpi && k.f ? 'aktif' : ''}" style="--k:${k.k}" ${k.f !== undefined ? `data-kpi="${k.f}"` : `data-p="${k.p}"`} title="${k.f ? 'Tıkla: bu karta göre filtrele / kaldır' : k.p ? 'Sekmeye git' : 'Kart filtresini kaldır'}"><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
  }

  // ---------- sütunlar (sekmeye göre): [başlık, genişlik ağırlığı]; sabit tablo düzeni → tek ekrana sığar
  const K = (ad, w) => ({ ad, w });
  const C_SEC = K('', 22), C_SIRA = K('Sıra', 26), C_PLAN = K('Planlanan Ödeme Tarihi', 90), C_SON = K('Son Ödeme Tarihi', 80), C_BELGE = K('Belge No · Belge Tipi', 96), C_KURUM = K('Ödenecek Cari · Ürün / Hizmet', 136), C_FS = K('Fatura Sahibi', 84), C_OD = K('Ödeyen Şirket', 84), C_TL = K('Tutar (₺) · Belge Oranı', 114), C_DURUM = K('Ödeme Durumu', 94), C_ODENME = K('Ödenme Tarihi', 72), C_ODENEN = K('Ödenen Tutar', 84), C_NOT = K('Not', 90), C_TALIMAT = K('Talimat', 76), C_ISLEM = K('', 26);
  const KOL = {
    tumu: [C_SEC, C_SIRA, C_PLAN, C_SON, C_BELGE, C_KURUM, C_FS, C_OD, K('Ödeme Yöntemi', 78), C_TL, C_DURUM, C_ODENME, C_ODENEN, C_TALIMAT, C_NOT, C_ISLEM],
    nakit: [C_SEC, C_SIRA, C_PLAN, C_SON, C_BELGE, C_KURUM, C_FS, C_OD, C_TL, K('Kasa', 92), K('Makbuz No · Muhasebe Fişi', 98), K('Nakit Onayı', 104), C_DURUM, C_ODENME, C_TALIMAT, C_ISLEM],
    havale: [C_SEC, C_SIRA, C_PLAN, C_SON, C_BELGE, C_KURUM, C_FS, C_OD, C_TL, K('Banka Hesabımız', 104), K('Alıcı IBAN · Banka · Hesap Adı', 144), K('Dekont No', 72), C_DURUM, C_ODENME, C_TALIMAT, C_ISLEM],
    kredi: [C_SEC, C_SIRA, C_PLAN, C_SON, C_BELGE, C_KURUM, C_FS, C_OD, C_TL, K('Kart', 118), K('Taksit', 70), K('Ekstre Dönemi', 86), C_DURUM, C_ODENME, C_TALIMAT, C_ISLEM],
    cek: [C_SEC, C_SIRA, C_PLAN, C_SON, C_BELGE, K('Ödenecek Cari · Ürün / Hizmet', 126), C_FS, C_OD, K('Tutar (₺)', 94), C_DURUM, K('Çek No · Keşideci', 118), K('Çek Vadesi · Çek Tutarı', 88), K('Akıbet · Plan Kontrolü · Dolaşım', 140), K('Belge İşlemi · İbraz · GMY Onayı', 120), C_TALIMAT, C_ISLEM],
    senet: [C_SEC, C_SIRA, C_PLAN, C_SON, C_BELGE, K('Ödenecek Cari · Ürün / Hizmet', 126), C_FS, C_OD, K('Tutar (₺)', 94), C_DURUM, K('Senet No · Keşideci', 118), K('Senet Vadesi · Senet Tutarı', 88), K('Akıbet · Plan Kontrolü · Dolaşım', 140), K('Belge İşlemi · İbraz · GMY Onayı', 120), C_TALIMAT, C_ISLEM],
    mahsup: [C_SEC, C_SIRA, C_PLAN, C_SON, K('Mahsup No (Modül Kaydı)', 90), K('Ödenecek Cari · Ürün / Hizmet', 136), C_FS, C_OD, C_TL, C_DURUM, K('Mahsup Türü · Taraflar', 150), K('Mahsup Tutarı', 90), K('Mahsup Durumu · Muhasebe', 120), K('Kontrol', 80), K('Kayıt Bağı', 86), C_TALIMAT, C_ISLEM],
    temlik: [C_SEC, C_SIRA, C_PLAN, C_SON, K('Temlik No (Modül Kaydı)', 90), K('Ödenecek Cari · Ürün / Hizmet', 136), C_FS, C_OD, C_TL, C_DURUM, K('Temlik Türü · İlk Eden → Nihai Alan', 150), K('Temlik Tutarı', 90), K('Temlik Durumu · İmza · Muhasebe', 120), K('Kontrol', 80), K('Kayıt Bağı', 86), C_TALIMAT, C_ISLEM],
    yansitma: [C_SEC, C_SIRA, C_PLAN, C_SON, K('Yansıtma No (Modül Kaydı)', 90), K('Ödenecek Cari · Ürün / Hizmet', 136), C_FS, C_OD, C_TL, C_DURUM, K('Tür · Yansıtan → Yansıtılan', 150), K('Genel Toplam', 90), K('Durum · Fatura / Entegrasyon', 120), K('Kontrol', 80), K('Kayıt Bağı', 86), C_TALIMAT, C_ISLEM],
    diger: [C_SEC, C_SIRA, C_PLAN, C_SON, C_BELGE, C_KURUM, C_FS, C_OD, C_TL, K('İşlem Tipi', 56), C_DURUM, C_ODENME, C_ODENEN, C_TALIMAT, C_NOT, C_ISLEM]
  };
  const alan = (r, ad, deger, tip, secenekler, kisaGoster) => tip === 'select' ? `<select class="kn-alan" data-alan="${ad}" title="${esc(deger || '')}"><option value="">—</option>${secenekler.map(o => `<option value="${esc(o)}" ${o === deger ? 'selected' : ''}>${esc(kisaGoster ? kisaGoster(o) : o)}</option>`).join('')}</select>` : `<input type="text" class="kn-alan" data-alan="${ad}" value="${esc(deger)}" title="${esc(deger || '')}" placeholder="…">`;
  const yigin = (liste, fn) => liste.length ? liste.map(x => `<span class="yb">${fn(x)}</span>`).join('') : bos;
  const belgeEtiket = r => r.belgeTipi === 'Cari Bakiye' || (!r.faturaNo && r.belgeTipi !== 'Avans') ? 'Cari Bakiye' : (r.faturaNo || r.avansNo || '—');
  // çek / senet satırında ekrandan ibraz: ödenmemiş, çıkış talebi olmayan, planlı (ya da dolaşımı bitmemiş) belgesi olan satır
  const ibrazUygun = r => (r.yontem === 'Çek' || r.yontem === 'Senet') && r.durum !== 'ÖDENDİ' && !r.cikis && (r.belgeler || []).some(x => !belgeCikmis(x) || (x.adimToplam > 0 && x.gerceklesen < x.adimToplam));
  function hucreler(r, p, no) {
    const a = r.alanlar || {}, g = r.grupIci, b = r.belgeler || [], av = r.avans, pl = r.plan || {}, o = r.onay;
    const cikmis = b.filter(belgeCikmis), belgesiz = (r.yontem === 'Çek' || r.yontem === 'Senet') && !b.length;
    const secTitle = r.durum === 'ÖDENDİ' ? 'disabled title="Ödendi"' : r.talimat && r.talimat.durum !== 'İptal' && r.talimat.durum !== 'Gerçekleşti' ? `disabled title="${esc(r.talimat.no)} talimatında (${esc(r.talimat.durum)})"` : r.cikis ? 'disabled title="Çek çıkış talebi: GMY onayı ile işlenir, talimata girmez"' : cikmis.length ? `disabled title="Belge plan dışı çıkmış: ${esc(cikmis.map(x => x.no + ' ' + x.akibet + (x.ibrazYeri ? ' → ' + x.ibrazYeri : '')).join(', '))} · Takip Paneli'nde kontrol edin"` : belgesiz ? `disabled title="Talimat için önce portföyden ${esc(r.yontem.toLocaleLowerCase('tr-TR'))} seçin"` : r.yontem === 'Nakit' && (!o || o.durum !== 'Onaylandı') ? 'disabled title="Nakit ödeme onaylanmadan talimata alınmaz"' : 'title="Talimat için seç"';
    const sec = `<td><input type="checkbox" class="kn-sec form-check-input" ${secTitle}></td>`;
    const sira = `<td title="Sekme içi sıra · kayıt #${r.docEntry}">${no ?? r.sira ?? ''}</td>`, plan = `<td><input type="date" class="kn-alan kn-tarih" data-alan="U_PlanlananTarih" value="${r.planlanan || ''}" title="Planlanan ödeme tarihi"></td>`;
    const son = `<td class="${r.sonOdeme && r.sonOdeme < bugun() && r.durum !== 'ÖDENDİ' ? 'gec' : ''}" title="${r.sonOdeme ? 'Son ödeme tarihi ' + tarih(r.sonOdeme) + (r.sonOdeme < bugun() && r.durum !== 'ÖDENDİ' ? ' · vadesi geçti' : '') : 'Son ödeme tarihi girilmemiş'}">${tarih(r.sonOdeme)}</td>`;
    const belge = `<td class="sol" title="${esc(r.belgeTipi || '')} ${esc(r.faturaNo || '')} · tıkla: belge detayı">${r.belgeTipi === 'Cari Bakiye' || (!r.faturaNo && !r.avansNo) ? `<a class="kn-belge-link kn-belge-detay">Cari Bakiye</a>` : `<a class="kn-belge-link kn-belge-detay">${esc(r.faturaNo || r.avansNo)}</a>`}${mukerrerRozet(r) ? ' ' + mukerrerRozet(r) : ''}<small>${esc(r.belgeTipi || '')}${pl.taksit ? ' · taksit ' + esc(pl.taksit) : ''}</small></td>`;
    // avans satırında konu yazılmaz (çekmecede görünür): alt satır yalnız avans no
    // avans satırı sığacak şekilde: üst satır kısa ad ("Nakit Avans" / "Şahsi Avans") + rozet, alt satır departman · avans no; konu yalnız çekmecede
    const avDep = av ? ((r.kurum || '').split(' · ')[1] || av.departman || '') : '';
    const kurum = av
      ? `<td class="sol kurum" title="${esc(r.kurum)} · avans ${esc(av.no)} · ayrıntı çekmecede">${av.sahsi ? 'Şahsi Avans' : 'Nakit Avans'} <span class="kn-avans" title="Nakit avans ${esc(av.no)} · ${esc(av.talepDurumu)} · ${esc(av.odemeDurumu)}">AVANS</span><small>${esc(avDep)}${avDep ? ' · ' : ''}${av.sahsi ? esc(av.no) : `<a href="${esc(av.link)}" target="_blank">${esc(av.no)}</a>`}</small></td>`
      : `<td class="sol kurum" title="${esc(r.kurum)}${r.urun ? ' · ' + esc(r.urun) : ''}">${esc(r.kurum)}<small>${esc(r.urun || '')}</small></td>`;
    const fs = `<td class="sol" title="Fatura sahibi: ${esc(r.faturaSahibi)}${r.talepEden ? ' · talep eden: ' + esc(r.talepEden) : ''}"><b>${esc(r.faturaSahibi)}</b><small>${r.talepEden ? 'talep: ' + esc(r.talepEden) : ''}</small></td>`;
    const od = `<td class="sol ${r.odeyenSirket !== r.faturaSahibi ? 'text-primary fw-bold' : ''}" title="Ödeyen şirket: ${esc(r.odeyenSirket)}${r.odeyenSirket !== r.faturaSahibi ? ' (fatura sahibinden farklı: grup içi ödeme)' : ''}">${esc(r.odeyenSirket)}</td>`;
    // üst satır: TL tutarı (₺ ile); alt satır: yalnız ödenecek dövizli tutar ve belgeye göre oran
    const oranYazi = pl.faturaTutari ? '%' + (pl.oran ?? 100).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) : '';
    const kf = r.kurBilgi && r.kurBilgi.kurFarki === 'Kesilecek' ? ' <span class="kn-kf" title="Bu cariye kur farkı faturası kesilecek">KF</span>' : '';
    const oranKucuk = r.pb && r.pb !== 'TRY' ? `<small>${para(r.tutar)} ${esc(r.pb)}${oranYazi ? ' · ' + oranYazi : ''}${kf}</small>` : (oranYazi && (pl.oran ?? 100) !== 100 ? `<small>${oranYazi}</small>` : '');
    const tl = `<td class="para" title="${esc(para(r.tutar) + ' ' + (r.pb || ''))}${r.pb !== 'TRY' ? ' · kur ' + esc(String(r.kur)) + (r.kurBilgi ? ' (' + esc(r.kurBilgi.kaynak || '') + (r.kurBilgi.tarih ? ' ' + tarih(r.kurBilgi.tarih) : '') + ')' + (r.kurBilgi.kurFarki ? ' · kur farkı faturası: ' + esc(r.kurBilgi.kurFarki) : '') + (r.kurBilgi.not ? ' · ' + esc(r.kurBilgi.not) : '') : '') : ''}${pl.faturaTutari ? ' · belge ' + para(pl.faturaTutari) + ' ' + esc(r.pb) + ' · bu satır %' + (pl.oran ?? 100) + (pl.kalan != null ? ' · kalan ' + para(pl.kalan) : '') : ''}">${para(r.tutarTL)} ₺${oranKucuk}</td>`;
    const durum = `<td><select class="kn-alan kn-durum" data-alan="U_Durum">${V.durumlar.map(d => `<option ${d === r.durum ? 'selected' : ''}>${esc(d)}</option>`).join('')}</select><span class="kn-mutabakat ${r.mutabakat === 'Mutabık' ? 'ok' : r.mutabakat ? 'hata' : 'yok'}" title="Mutabakat durumu${r.mutabakatTarihi ? ' · ' + tarih(r.mutabakatTarihi) : ''}">${esc(r.mutabakat || 'Mutabakat Yok')}</span></td>`;
    // ödenen tutar: TL karşılığı üstte (ödemede baz alınan kurla: TCMB ya da anlaşılan kur), dövizli ödemede altta döviz tutarı + kur; TL ödemede yalnız TL
    const odenenDoviz = r.odenen && r.odenenPB && r.odenenPB !== 'TRY';
    const odenme = `<td>${tarih(r.odenme)}</td>`, odenen = `<td class="para" title="${odenenDoviz ? esc(`${para(r.odenen)} ${r.odenenPB} × ${r.kur} (${r.kurBilgi?.kaynak || 'kur'}${r.kurBilgi?.tarih ? ' ' + tarih(r.kurBilgi.tarih) : ''}) = ${para(r.odenenTL ?? r.odenen * r.kur)} ₺`) : ''}">${r.odenen ? `${para(r.odenenTL ?? r.odenen)} ₺${odenenDoviz ? `<small>${para(r.odenen)} ${esc(r.odenenPB)} · ${esc(String(r.kur))}${r.kurBilgi?.kaynak ? ' ' + esc(r.kurBilgi.kaynak.replace('TCMB ', 'TCMB ').replace('Döviz ', '')) : ''}</small>` : ''}` : '—'}</td>`;
    const talimat = `<td>${r.talimat ? talimatRozet(r.talimat) : bos}</td>`;
    const not = `<td class="sol not" title="${esc(r.notlar || '')}">${esc(r.notlar || '')}</td>`, islem = `<td><button type="button" class="btn btn-light btn-sm py-0 px-1 border kn-ac" title="Detay / düzenle / işlemler"><i class="fas fa-pen"></i></button></td>`;
    if (p === 'tumu') return [sec, sira, plan, son, belge, kurum, fs, od, `<td>${yontemRozet(r.yontem)}${o ? '<small>' + onayRozet(o) + '</small>' : ''}</td>`, tl, durum, odenme, odenen, talimat, not, islem];
    if (p === 'nakit') {
      const onayHucre = `<td class="sol wrap">${o ? onayRozet(o) : '<span class="kn-onay bekliyor">Onay Bekliyor</span>'}${o && o.onaylayan ? `<small>${esc(o.onaylayan)} · ${esc((o.tarih || '').slice(0, 10))}</small>` : ''}${r.durum !== 'ÖDENDİ' && (!o || o.durum !== 'Onaylandı') ? ` <button type="button" class="btn btn-primary btn-sm py-0 px-1 kn-onay-ac" style="font-size:.6rem" title="Onaycı: ${esc((V.onaycilar?.nakit || []).join(' / '))}"><i class="fas fa-user-check"></i> Onayla / Reddet</button>` : ''}${r.durum !== 'ÖDENDİ' && o && o.durum === 'Onaylandı' ? ` <button type="button" class="btn btn-success btn-sm py-0 px-1 kn-makbuz-ac" style="font-size:.6rem" title="Kasa tediye makbuzu kes: satır ödenir, muhasebe fişi atılır"><i class="fas fa-receipt"></i> Makbuz Kes</button>` : ''}</td>`;
      const imzali = (r.dekontlar || []).some(d => d.tur === 'İmzalı Makbuz') ? ' <span class="kn-ek-tur imzali" title="İmzalı makbuz kopyası rapor sayfasına yüklendi (çekmece › Ekler)">imzalı</span>' : '';
      const makbuz = `<td class="sol" title="${r.makbuz ? 'Makbuz ' + esc(r.makbuz.no) : ''}${r.yevmiyeNo ? ' · muhasebe fişi ' + esc(r.yevmiyeNo) : ''}${a.teslimAlan ? ' · teslim alan ' + esc(a.teslimAlan) : ''}">${r.makbuz ? (r.makbuz.link ? `<a href="${esc(r.makbuz.link)}" target="_blank" class="fw-bold" title="Makbuzu yazdır">${esc(r.makbuz.no)}</a>` : `<b>${esc(r.makbuz.no)}</b>`) : bos}${imzali}<small>${r.yevmiyeNo ? esc(r.yevmiyeNo) : (a.teslimAlan ? 'teslim: ' + esc(a.teslimAlan) : '')}</small></td>`;
      return [sec, sira, plan, son, belge, kurum, fs, od, tl, `<td>${alan(r, 'U_Kasa', a.kasa, 'select', V.kasalar)}</td>`, makbuz, onayHucre, durum, odenme, talimat, islem];
    }
    if (p === 'havale') return [sec, sira, plan, son, belge, kurum, fs, od, tl, `<td>${alan(r, 'U_BankaHesabi', a.bankaHesabi, 'select', V.bankaHesaplari, x => x.split(' · ')[0] + ' · ' + (x.split(' · ')[1] || '').slice(-7))}</td>`, `<td class="sol" title="${esc([a.aliciBanka, a.aliciIban, a.aliciHesapAdi].filter(Boolean).join(' · '))} · çekmecede Cari Karttan Getir">${alan(r, 'U_AliciIban', a.aliciIban)}<small>${esc([a.aliciBanka, a.aliciHesapAdi].filter(Boolean).join(' · ') || 'banka bilgisi yok')}</small></td>`, `<td class="sol">${alan(r, 'U_DekontNo', a.dekontNo)}${(r.dekontlar || []).length ? `<small><i class="fas fa-paperclip"></i> ${r.dekontlar.length} ek</small>` : ''}</td>`, durum, odenme, talimat, islem];
    if (p === 'kredi') return [sec, sira, plan, son, belge, kurum, fs, od, tl, `<td>${alan(r, 'U_Kart', a.kart, 'select', V.kartlar)}</td>`, `<td>${alan(r, 'U_Taksit', a.taksit, 'select', V.taksitler)}</td>`, `<td>${alan(r, 'U_EkstreDonemi', a.ekstreDonemi)}</td>`, durum, odenme, talimat, islem];
    if (p === 'cek' || p === 'senet') {
      const cikisHucre = r.cikis ? `<span class="yb">${onayRozet(o)}<small>${esc(r.cikis.islem)}${r.cikis.ibrazYeri ? ' → ' + esc(r.cikis.ibrazYeri) : ''} · talep ${esc(r.cikis.talepEden)}</small>${o && o.durum === 'Onay Bekliyor' ? `<button type="button" class="btn btn-danger btn-sm py-0 px-1 kn-cikis-onay" style="font-size:.6rem" title="GMY (${esc((V.onaycilar?.cekCikis || []).join(' / '))}) onaylarsa çıkış işlenir, reddederse belge portföyde kalır"><i class="fas fa-shield-halved"></i> GMY Onayı</button>` : ''}</span>` : '';
      return [sec, sira, plan, son, belge, kurum, fs, od, `<td class="para" title="${esc(para(r.tutar) + ' ' + (r.pb || ''))}">${para(r.tutarTL)}</td>`, durum,
        `<td class="sol">${yigin(b, x => `<span title="${esc(x.banka)}${x.sube ? ' / ' + esc(x.sube) : ''} · keşideci ${esc(x.asilBorclu)}"><b>${esc(x.no)}</b>${x.tip ? ` <span class="kn-oran" title="İşlem tipi ${esc(x.tip)}">${esc(x.tip)}</span>` : ''}${x.sahip && x.sahip !== r.odeyenSirket ? ` <small class="d-inline text-primary">(${esc(x.sahip)} portföyü)</small>` : ''}<small>${esc(x.asilBorclu)}${x.kimdenGeldi && x.kimdenGeldi !== x.asilBorclu ? ' · ' + esc(x.kimdenGeldi) : ''}</small></span>`)}</td>`,
        `<td class="para">${yigin(b, x => `${tarih(x.vade)}<small class="fw-bold text-dark">${para(x.tutar)}</small>`)}</td>`,
        `<td class="sol wrap">${yigin(b, x => akibetRozet(x.akibet) + ' ' + kontrolRozet(x.kontrol, x) + (x.dolasim ? `<span class="kn-dolasim d-block" title="Grup içi dolaşım planı: ${esc(x.dolasim)} · ${x.gerceklesen}/${x.adimToplam} adım gerçekleşti"><i class="fas fa-route"></i> ${esc(x.dolasim)} (${x.gerceklesen}/${x.adimToplam})</span>` : ''))}</td>`,
        `<td class="sol wrap">${cikisHucre}${r.cikis ? '' : b.map(x => `<span class="yb">${belgeCikmis(x) && !x.adimToplam ? `<span class="text-muted" title="Belge çıkmış (${esc(x.akibet)}${x.ibrazYeri ? ' → ' + esc(x.ibrazYeri) : ''}); işlem yapılamaz">${esc(x.akibet)}${r.ibraz ? ' · ' + esc(r.ibraz.belgeNo) : ''}</span>` : `<button type="button" class="btn btn-link btn-sm p-0 kn-dolasim-ac" data-cek="${x.cekDocEntry}" title="${x.adimToplam ? 'Dolaşım planı: ' + esc(x.dolasim) + ' (' + x.gerceklesen + '/' + x.adimToplam + ' adım) · adım gerçekleştir' : 'Grup içi dolaşım planı kur ve uygunluk simülasyonunu gör'}"><i class="fas fa-route"></i> Dolaşım</button> <button type="button" class="btn btn-link btn-sm p-0 text-danger kn-belge-kaldir" data-cek="${x.cekDocEntry}" ${x.akibet !== 'Planlandı' || x.gerceklesen > 0 ? 'disabled' : ''} title="Planı kaldır: belge portföye döner"><i class="fas fa-times"></i></button>`}</span>`).join('')}${r.durum === 'ÖDENDİ' || r.cikis ? '' : `<span class="yb"><button type="button" class="btn btn-primary btn-sm py-0 px-1 kn-belge-sec" style="font-size:.62rem" title="Portföydeki belgeleri gör ve seç (vade, tutar, banka süzgeçleri; başka grup şirketinin portföyü)"><i class="fas fa-plus"></i> ${b.length ? 'Ekle' : 'Belge Seç'}</button>${ibrazUygun(r) ? ` <button type="button" class="btn btn-success btn-sm py-0 px-1 kn-ibraz-ac" style="font-size:.62rem" title="Çeki tedarikçiye ver (ciro) ya da bankaya tahsile ver: belge çıkar, satır ödenir, muhasebe fişleri atılır"><i class="fas fa-money-check-dollar"></i> ${r.yontem === 'Senet' ? 'Senedi Ver' : 'Çeki Ver / İbraz'}</button>` : ''}</span>`}</td>`, talimat, islem];
    }
    if (p === 'mahsup' || p === 'temlik' || p === 'yansitma') {
      const kayit = g ? `<td class="sol"><a href="${esc(g.link)}" target="_blank" class="fw-bold" title="${esc(g.tur)} kaydını modülde aç">${esc(g.no)}</a><small>${g.bulundu === false ? '<span class="text-danger">kayıt bulunamadı</span>' : tarih(g.tarih)}</small></td>` : `<td class="sol">${bos}<small>kayıt bağlanmadı</small></td>`;
      const tur = g ? `<td class="sol" title="${esc(g.baslik || '')} · ${esc(g.taraflar || '')}">${esc(g.baslik || '')}<small>${esc(g.taraflar || '')}</small></td>` : `<td class="sol">${bos}</td>`;
      const mtutar = `<td class="para">${g ? para(g.tutar) + (g.pb && g.pb !== 'TRY' ? `<small>${esc(g.pb)}</small>` : '') : '—'}</td>`;
      const mdurum = g ? `<td class="sol" title="${esc(g.durum || '')} · ${esc((p === 'temlik' && g.imza ? 'imza ' + g.imza + ' · ' : '') + (g.muhasebe || ''))}">${modulRozet(g.durum)}<small>${esc((p === 'temlik' && g.imza ? 'imza ' + g.imza + ' · ' : '') + (g.muhasebe || ''))}</small></td>` : `<td>${bos}</td>`;
      const kontrol = `<td>${g && g.kontrol ? rozet(g.kontrol, /Uyumlu|UYUMLU/.test(g.kontrol) ? 'ok' : 'hata') : bos}</td>`;
      const aksiyon = `<td class="sol"><button type="button" class="btn btn-primary btn-sm py-0 px-1 kn-grup-sec" data-tur="${SEKME[p]}" style="font-size:.62rem" title="${g ? 'Kaydı değiştir' : 'Modülden kayıt bağla'}"><i class="fas fa-link"></i> ${g ? 'Değiştir' : 'Kayıt Seç'}</button>${g ? ` <button type="button" class="btn btn-link btn-sm p-0 text-danger kn-grup-kaldir" title="Kayıt bağını kaldır"><i class="fas fa-times"></i></button>` : ''}</td>`;
      return [sec, sira, plan, son, kayit, kurum, fs, od, tl, durum, tur, mtutar, mdurum, kontrol, aksiyon, talimat, islem];
    }
    return [sec, sira, plan, son, belge, kurum, fs, od, tl, `<td>${esc(r.islemTipi || '')}</td>`, durum, odenme, odenen, talimat, not, islem];
  }
  function cizTablo(p, L) {
    const t = $('#knTablo-' + p); if (!t) return;
    // ödenmemişler planlanan tarihe göre üstte; ödenenler ödenme tarihine göre en altta
    const R0 = p === 'tumu' ? L : L.filter(r => r.yontem === SEKME[p]);
    const R = [...R0.filter(r => r.durum !== 'ÖDENDİ'), ...R0.filter(r => r.durum === 'ÖDENDİ').sort((a, b) => (a.odenme || '').localeCompare(b.odenme || '') || a.docEntry - b.docEntry)];
    const kol = KOL[p], topW = kol.reduce((s, k) => s + k.w, 0);
    let cg = t.querySelector('colgroup'); if (!cg) { cg = document.createElement('colgroup'); t.insertBefore(cg, t.tHead); }
    cg.innerHTML = kol.map(k => `<col style="width:${(k.w / topW * 100).toFixed(2)}%">`).join('');
    t.tHead.innerHTML = '<tr>' + kol.map((k, i) => i === 0 ? `<th><input type="checkbox" class="form-check-input kn-sec-hepsi" title="Tümünü seç"></th>` : `<th class="${/Tutar|Toplam|Ödenen/.test(k.ad) && !/Çek No|Senet No/.test(k.ad) ? 's' : ''}" title="${esc(k.ad)}">${esc(k.ad)}</th>`).join('') + '</tr>';
    t.tBodies[0].innerHTML = R.length ? R.map((r, i) => `<tr class="tik ${DURUM_SINIF[r.durum] || ''}" data-doc="${r.docEntry}" data-db="${esc(r.dbName)}">${hucreler(r, p, i + 1).join('')}</tr>`).join('') : `<tr><td colspan="${kol.length}" class="na-bos text-center">Bu sekmede filtreye uyan kayıt yok.${p !== 'tumu' ? ' Yeni Ödeme ile "' + esc(SEKME[p]) + '" yöntemli kayıt açabilirsiniz.' : ''}${F.kpi ? ' Üst karttaki filtre açık.' : ''}</td></tr>`;
    const toplam = R.reduce((s, r) => s + r.tutarTL, 0), odenen = R.filter(r => r.durum === 'ÖDENDİ').reduce((s, r) => s + r.tutarTL, 0);
    const tlIdx = kol.findIndex(k => k.ad.startsWith('Tutar (₺)'));
    t.tFoot.innerHTML = R.length ? `<tr class="toplam"><td colspan="${tlIdx}" class="text-end">TOPLAM (₺)</td><td class="para">${para(toplam)}</td><td colspan="${kol.length - tlIdx - 1}" class="sol">ödenen ${para(odenen)} · kalan ${para(toplam - odenen)} · ${R.length} kayıt</td></tr>` : '';
    $('#knNot-' + p).textContent = '';   // kart başlığındaki bilgi satırı istenmedi (toplamlar tablo altında)
    secimSayaci(t);
  }
  function cizGrupOzet(p) {
    const g = V.grupIci?.[p]; const kap = $('#knGrup-' + p); if (!g || !kap) return;
    const ad = SEKME[p];
    kap.innerHTML = [
      { l: `Toplam ${ad} Kaydı`, v: g.toplam, s: F.sirket ? 'bu şirketin taraf olduğu' : 'tüm şirketler', k: '#0f766e' },
      { l: 'Açık İşlem', v: g.acik, s: kisa(g.acikTutar) + ' ₺ muhasebeleşmemiş', k: '#b45309' },
      { l: 'Onay Bekleyen', v: g.onayBekleyen, s: p === 'temlik' ? `${g.imzaBekleyen} imza bekleyen` : p === 'yansitma' ? `${g.faturaBekleyen} fatura bekleyen` : `${g.kontrol} kontrol gerekli`, k: '#7c3aed' },
      { l: 'Modül', v: `<a href="${esc(g.link)}" target="_blank" style="font-size:.8rem">${esc(ad)} İşlemleri <i class="fas fa-arrow-up-right-from-square"></i></a>`, s: `<a href="${esc(V.grupIci.rapor)}" target="_blank">Grup İçi Özel Finansal İşlemler Raporu</a>`, k: '#1d4ed8', h: true }
    ].map(k => `<div style="--k:${k.k}"><div class="l">${esc(k.l)}</div><div class="v">${k.h ? k.v : esc(String(k.v))}</div><div class="s">${k.h ? k.s : esc(k.s)}</div></div>`).join('');
    $('#knGrupNot-' + p).textContent = 'kayıt bağlanan satırlarda kurum, tutar, durum ve kontrol modülden canlı okunur';
  }
  function cizBakiye() {
    const B = V.bakiyeler, t = $('#knBakiye');
    t.tBodies[0].innerHTML = B.length ? B.map(b => `<tr><td>${esc(b.sirket)}</td><td class="sol fw-bold">${esc(b.bankaKasa)}</td><td class="sol">${esc(b.detay)}</td><td>${esc(b.pb === 'TRY' ? 'TL' : b.pb)}</td><td class="para">${para(b.tutar)}</td><td class="para">${para(b.tutarTL)}</td><td>${tarih(b.tarih)}</td></tr>`).join('') : '<tr><td colspan="7" class="na-bos">Bakiye yok.</td></tr>';
    const nakit = B.filter(b => b.bankaKasa === 'BANKA' || b.bankaKasa === 'KASA').reduce((s, b) => s + b.tutarTL, 0), portfoy = B.filter(b => b.bankaKasa.includes('PORTFÖY')).reduce((s, b) => s + b.tutarTL, 0);
    t.tFoot.innerHTML = `<tr class="toplam"><td colspan="5" class="text-end">BANKA + KASA</td><td class="para">${para(nakit)}</td><td></td></tr><tr class="toplam"><td colspan="5" class="text-end">ÇEK + SENET PORTFÖYÜ</td><td class="para">${para(portfoy)}</td><td></td></tr>`;
    $('#knBakiyeNot').textContent = `${B.length} satır · toplam ${para(nakit + portfoy)} ₺`;
    const L = satirlar().filter(r => r.durum !== 'ÖDENDİ'), pbler = [...new Set([...B.map(b => b.pb), ...L.map(r => r.pb)])].sort((a, b) => a === 'TRY' ? -1 : b === 'TRY' ? 1 : a.localeCompare(b));
    const nakitY = r => !['Çek', 'Senet', 'Mahsup', 'Temlik', 'Yansıtma'].includes(r.yontem);
    $('#knProjeksiyon tbody').innerHTML = pbler.map(pb => { const mb = B.filter(b => (b.bankaKasa === 'BANKA' || b.bankaKasa === 'KASA') && b.pb === pb).reduce((s, b) => s + b.tutar, 0), bn = L.filter(r => r.pb === pb && nakitY(r)).reduce((s, r) => s + r.tutar, 0), mc = B.filter(b => b.bankaKasa.includes('PORTFÖY') && b.pb === pb).reduce((s, b) => s + b.tutar, 0), bc = L.filter(r => r.pb === pb && (r.yontem === 'Çek' || r.yontem === 'Senet')).reduce((s, r) => s + r.tutar, 0); const k = v => `<td class="para ${v < 0 ? 'text-danger' : 'text-primary'}">${para(v)}</td>`; return `<tr><td rowspan="2" class="fw-bold">${esc(pb === 'TRY' ? 'TL' : pb)}</td><td class="sol">Nakit / Banka</td><td class="para">${para(mb)}</td><td class="para text-danger">${para(bn)}</td>${k(mb - bn)}</tr><tr><td class="sol">Müşteri Çekleri / Senetleri</td><td class="para">${para(mc)}</td><td class="para text-danger">${para(bc)}</td>${k(mc - bc)}</tr>`; }).join('');
  }
  function cizGunluk() {
    const a = F.ara.trim().toLocaleLowerCase('tr-TR'); const G = V.gunluk.filter(g => !a || [g.kullanici, g.tip, g.aciklama, g.sirket, String(g.docEntry)].join(' ').toLocaleLowerCase('tr-TR').includes(a));
    $('#knGunluk tbody').innerHTML = G.length ? G.map(g => `<tr class="tik" data-doc="${g.docEntry}" data-db="${esc(g.dbName)}"><td>${esc(g.tarih)}</td><td>${esc(g.kullanici)}</td><td>${esc(g.sirket)}</td><td>#${g.docEntry}</td><td>${rozet(g.tip, /Sil|Kaldır|Hata|Red/.test(g.tip) ? 'hata' : /Ödeme|Planlandı|Bağlandı|Talimat|Avans|Onay|Makbuz|Fiş/.test(g.tip) ? 'ok' : 'bilgi')}</td><td class="sol">${esc(g.aciklama)}</td></tr>`).join('') : '<tr><td colspan="6" class="na-bos">Günlük kaydı yok.</td></tr>';
    $('#knGunlukNot').textContent = `${G.length} kayıt · son 300`;
  }

  // ---------- TALİMATLAR
  const talimatDurumRozet = d => rozet(d, d === 'Gerçekleşti' ? 'ok' : d === 'İptal' ? 'hata' : d === 'Hazırlandı' ? 'bekle' : 'bilgi');
  function cizTalimatlar() {
    const T = V.talimatlar || [], tb = $('#knTalimatlar tbody');
    tb.innerHTML = T.length ? T.map(t => `<tr class="tik tl-satir" data-no="${esc(t.no)}"><td class="sol"><b>${esc(t.no)}</b></td><td>${esc(t.tarih)}</td><td class="sol">${esc(t.baslik)}<small>${yontemRozet(t.yontem)}</small></td><td class="sol fw-bold">${esc(t.sirket)}</td><td class="sol" title="${esc(t.bankaHesabi || t.kasa || '')}">${esc(t.bankaHesabi || t.kasa || '—')}</td><td class="para">${t.satirSayisi}</td><td class="para">${para(t.toplamTL)}</td><td>${talimatDurumRozet(t.durum)}${t.imzali ? '<small><span class="kn-ek-tur imzali" title="İmzalı talimat rapor sayfasına yüklendi">İmzalı Yüklendi</span></small>' : ''}</td><td class="sol">${esc(t.hazirlayan)}${t.onaylayan ? ' · ' + esc(t.onaylayan) : ''}<small>${t.gerceklesmeTarihi ? 'gerçekleşme ' + esc(t.gerceklesmeTarihi) : t.iletimTarihi ? 'iletim ' + esc(t.iletimTarihi) : t.onayTarihi ? 'onay ' + esc(t.onayTarihi) : ''}</small></td>
      <td class="sol" style="white-space:normal"><a class="btn btn-light btn-sm py-0 px-1 border" href="${esc(t.link)}" target="_blank" title="Yazdırılabilir talimat" style="font-size:.62rem"><i class="fas fa-print"></i> Yazdır</a> <button type="button" class="btn btn-light btn-sm py-0 px-1 border kn-tl-ek" data-no="${esc(t.no)}" title="İmzalı talimatı (tarama / PDF) bu rapor sayfasına yükle" style="font-size:.62rem"><i class="fas fa-file-signature"></i> İmzalı Yükle</button> ${t.sonrakiDurum ? `<button type="button" class="btn btn-primary btn-sm py-0 px-1 kn-tl-durum" data-no="${esc(t.no)}" data-durum="${esc(t.sonrakiDurum)}" style="font-size:.62rem"><i class="fas fa-arrow-right"></i> ${esc(t.sonrakiDurum)}</button>` : ''} ${!t.kapandi ? `<button type="button" class="btn btn-link btn-sm p-0 text-danger kn-tl-durum" data-no="${esc(t.no)}" data-durum="İptal" title="Talimatı iptal et"><i class="fas fa-times"></i></button>` : ''}</td></tr>
      <tr class="tl-detay" data-no="${esc(t.no)}" style="display:none"><td colspan="10" class="sol"><div class="kn-liste">${t.satirlar.map(s => `<div class="satir"><span class="text-muted">#${s.DocEntry ?? s.docEntry}</span><b>${esc(s.kurum)}</b><span>${esc(s.faturaSahibi)}${s.odeyenSirket !== s.faturaSahibi ? ' · ödeyen ' + esc(s.odeyenSirket) : ''}</span><span>${esc(s.faturaNo || '')}</span><span class="fw-bold">${para(s.tutar)} ${esc(pbAd(s.pb))}</span>${s.aliciIban ? `<span class="text-muted" title="Alıcı banka bilgisi">${esc(s.aliciBanka || '')} ${esc(s.aliciIban)}${s.aliciHesapAdi ? ' · ' + esc(s.aliciHesapAdi) : ''}</span>` : ''}${s.kurBilgisi ? `<span class="text-muted">kur ${esc(s.kurBilgisi)}</span>` : ''}<span class="text-muted">${esc(s.aciklama)}</span>${s.belgeler.length ? `<span class="text-primary">${s.belgeler.map(b => esc(b.No || b.no) + (b.Dolasim || b.dolasim ? ' (' + esc(b.Dolasim || b.dolasim) + ')' : '')).join(', ')}</span>` : ''}${s.avansNo ? `<span class="kn-avans">AVANS ${esc(s.avansNo)}</span>` : ''}${s.makbuzNo ? `<a href="/Rapor79/Makbuz?no=${encodeURIComponent(s.makbuzNo)}" target="_blank" class="fw-bold"><i class="fas fa-receipt"></i> ${esc(s.makbuzNo)}</a>` : ''}${s.yevmiyeNo ? `<span class="text-muted">fiş ${esc(s.yevmiyeNo)}</span>` : ''}</div>`).join('')}${(t.ekler || []).length ? `<div class="na-mini"><i class="fas fa-paperclip me-1"></i>Ekler: ${t.ekler.map(e => e.dosyaVar ? `<a href="${esc(e.yol)}" target="_blank">${esc(e.tur)} · ${esc(e.ad)}</a>` : esc(e.tur + ' · ' + e.ad)).join(' · ')}</div>` : ''}<div class="na-mini">${t.gunluk.map(g => `${esc(g.tarih)} ${esc(g.Islem || g.islem)} ${esc(g.Kullanici || g.kullanici)} ${esc(g.Aciklama || g.aciklama || '')}`).join(' · ')}</div></div></td></tr>`).join('') : '<tr><td colspan="10" class="na-bos text-center">Henüz talimat yok. Yöntem sekmelerinde satırları seçip "Seçilenlerden Talimat Oluştur" düğmesine basın.</td></tr>';
    $('#knTalimatNot').textContent = `${T.length} talimat · ${T.filter(t => !t.kapandi).length} açık · toplam ${para(T.filter(t => t.durum !== 'İptal').reduce((s, t) => s + t.toplamTL, 0))} ₺`;
  }
  kok.addEventListener('click', e => { const tr = e.target.closest('tr.tl-satir'); if (!tr || e.target.closest('a,button')) return; const d = $(`tr.tl-detay[data-no="${tr.dataset.no}"]`); if (d) d.style.display = d.style.display === 'none' ? '' : 'none'; });
  // imzalı talimat yükleme (Talimatlar sekmesi): gizli dosya seçici → EkYukle (talimatNo)
  let tlEkNo = null;
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-tl-ek'); if (!b) return; e.stopPropagation(); tlEkNo = b.dataset.no; const f = $('#knTlDosya'); f.value = ''; f.click(); });
  $('#knTlDosya').addEventListener('change', async () => {
    const f = $('#knTlDosya').files[0]; if (!f || !tlEkNo) return;
    const fd = new FormData(); fd.append('dosya', f); fd.append('talimatNo', tlEkNo); fd.append('tur', 'İmzalı Talimat'); fd.append('aciklama', 'İmzalı talimat (tarama)');
    const d = await fetch('/Rapor79/EkYukle', { method: 'POST', headers: { 'RequestVerificationToken': csrf }, body: fd }).then(x => x.json()).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; } mesaj('success', '<i class="fas fa-file-signature me-1"></i>' + esc(d.message)); await yukle(true); sekme('talimat');
  });
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.kn-tl-durum'); if (!b) return; const no = b.dataset.no, durum = b.dataset.durum; let not = '';
    if (durum === 'İptal') { not = prompt(`${no} iptal nedeni:`, ''); if (not === null) return; } else if (!confirm(`${no} → ${durum}?` + (durum === 'Gerçekleşti' ? ' Satırlar ÖDENDİ olur; çekler dolaşım planına göre ciro / ibraz edilir, kasa talimatında makbuzlar kesilir, avanslar teslim edilmiş sayılır, muhasebe fişleri atılır.' : ''))) return;
    const d = await post('/Rapor79/TalimatDurum', { No: no, Durum: durum, Not: not }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } mesaj(durum === 'Gerçekleşti' ? 'success' : 'info', esc(d.message)); await yukle(true); sekme('talimat');
  });
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-talimat-ac'); if (!b) return; e.stopPropagation(); sekme('talimat'); const tr = $(`tr.tl-satir[data-no="${b.dataset.no}"]`); if (tr) { tr.scrollIntoView({ block: 'center' }); const d = $(`tr.tl-detay[data-no="${b.dataset.no}"]`); if (d) d.style.display = ''; } });
  // talimat seçimi: kutucuk ya da ilk hücre tıklanır; "Seçilenlerden Talimat Oluştur" düğmesi seçili sayısını gösterir
  const secimSayaci = t => { if (!t) return; const n = $$('.kn-sec:checked', t).length, p = t.id.replace('knTablo-', ''), b = $(`.kn-talimat-olustur[data-p="${p}"]`); if (b) { b.innerHTML = `<i class="fas fa-file-contract me-1"></i>Seçilenlerden Talimat Oluştur${n ? ` (${n})` : ''}`; b.classList.toggle('btn-success', n > 0); b.classList.toggle('btn-primary', n === 0); } };
  kok.addEventListener('change', e => { const h = e.target.closest('.kn-sec-hepsi'); if (h) { $$('.kn-sec:not(:disabled)', h.closest('table')).forEach(c => { c.checked = h.checked; c.closest('tr').classList.toggle('secili', h.checked); }); secimSayaci(h.closest('table')); return; } const c = e.target.closest('.kn-sec'); if (c) { c.closest('tr').classList.toggle('secili', c.checked); secimSayaci(c.closest('table')); } });
  kok.addEventListener('click', e => { const td = e.target.closest('td'); if (!td || e.target.closest('input') || td !== td.parentElement.firstElementChild) return; const c = td.querySelector('.kn-sec'); if (!c) return; e.stopPropagation(); if (c.disabled) { mesaj('info', 'Bu satır talimata alınamaz: ' + esc(c.title || 'kapalı')); return; } c.checked = !c.checked; c.dispatchEvent(new Event('change', { bubbles: true })); }, true);
  // ---------- TALİMAT HAZIRLAMA PENCERESİ: seçili satırlar, dövizli satırlarda baz kur (TCMB / fatura üstü / anlaşmalı), banka hesabı / kasa
  let TLM = { satirlar: [], db: '' };
  function talimatOlustur(ids) {
    const secilen = ids.map(satirBul).filter(Boolean); if (!secilen.length) { mesaj('warning', 'Önce tablodan satır seçin (ilk sütundaki kutular ya da ilk hücre).'); return; }
    TLM = { satirlar: secilen, db: F.sirket || secilen[0].dbName };
    const ozet = {}; secilen.forEach(r => { const k = (r.odeyenSirket || r.sirket) + ' · ' + r.yontem; ozet[k] = (ozet[k] || 0) + 1; });
    $('#tlOzet').innerHTML = Object.entries(ozet).map(([k, n]) => `<span class="kn-pill">${esc(k)}: ${n} satır</span>`).join(' ');
    const dovizli = secilen.filter(r => r.pb && r.pb !== 'TRY');
    $('#tlGovde').innerHTML = secilen.map((r, i) => `<tr data-i="${i}"><td class="sol"><b>${esc(r.kurum)}</b><br><small class="text-muted">${esc(r.belgeTipi || '')} ${esc(r.faturaNo || '')} · ${esc(r.faturaSahibi)} → ${esc(r.odeyenSirket)}</small></td><td>${yontemRozet(r.yontem)}</td><td class="text-end fw-bold">${para(r.tutar)} ${esc(pbAd(r.pb))}</td>${r.pb && r.pb !== 'TRY'
      ? `<td><select class="form-select form-select-sm tl-kaynak" style="font-size:.68rem">${(V.kurKaynaklari || []).map(k => `<option ${k === (r.kurBilgi?.kaynak || 'TCMB Döviz Alış') ? 'selected' : ''}>${esc(k)}</option>`).join('')}</select></td><td><input type="date" class="form-control form-control-sm tl-tarih" value="${r.kurBilgi?.tarih || bugun()}" style="font-size:.68rem"></td><td><input type="number" step="0.0001" class="form-control form-control-sm tl-kur" value="${r.kur}" style="font-size:.68rem;width:96px"></td><td><input class="form-control form-control-sm tl-not" value="${esc(r.kurBilgi?.not || '')}" placeholder="fatura üstü / anlaşılan kur notu" style="font-size:.68rem"></td><td class="text-end tl-tl">${para(r.tutarTL)}</td>`
      : `<td colspan="4" class="text-muted">TL</td><td class="text-end">${para(r.tutarTL)}</td>`}</tr>`).join('');
    $('#tlDovizNot').innerHTML = dovizli.length ? `<i class="fas fa-landmark me-1"></i>${dovizli.length} dövizli satır: baz alınan kur satıra ve talimata yazılır (TCMB kurları otomatik, fatura üstü / anlaşılan kur elle)` : '';
    const hv = secilen.some(r => r.yontem === 'Havale / EFT' || r.yontem === 'Kredi Kartı / Banka Kartı'), nk = secilen.some(r => r.yontem === 'Nakit');
    $('#tlBankaKap').style.display = hv ? '' : 'none'; $('#tlKasaKap').style.display = nk ? '' : 'none';
    $('#tlBanka').innerHTML = (V.bankaHesaplari || []).map(h => `<option ${h === (secilen.find(r => r.alanlar?.bankaHesabi)?.alanlar.bankaHesabi) ? 'selected' : ''}>${esc(h)}</option>`).join(''); $('#tlKasa').innerHTML = (V.kasalar || []).map(k => `<option>${esc(k)}</option>`).join('');
    modalAc($('#knTalimatModal')); tlKurlariGetir();
  }
  const tlTl = (tr, r) => { const k = +tr.querySelector('.tl-kur').value || 1; tr.querySelector('.tl-tl').textContent = para(r.tutar * k); };
  async function tlKurlariGetir() {
    for (const tr of $$('#tlGovde tr')) {
      const r = TLM.satirlar[+tr.dataset.i]; if (!r || !r.pb || r.pb === 'TRY') continue;
      const kay = tr.querySelector('.tl-kaynak').value, t = tr.querySelector('.tl-tarih').value || bugun(), tcmb = /TCMB|Çek Tahsil/.test(kay), inp = tr.querySelector('.tl-kur'); inp.readOnly = tcmb;
      if (!tcmb) { inp.title = 'fatura üstündeki / anlaşılan kur elle yazılır'; tlTl(tr, r); continue; }
      const d = await getJ(`/Rapor79/Kur?pb=${encodeURIComponent(r.pb)}&tarih=${t}`).catch(() => null);
      if (d && d.success) { const k = d.kur; inp.value = kay === 'TCMB Döviz Satış' ? k.satis : kay === 'TCMB Efektif Alış' ? k.efektifAlis : k.alis; inp.title = k.aciklama; }
      tlTl(tr, r);
    }
  }
  $('#tlGovde').addEventListener('change', e => { const tr = e.target.closest('tr'); if (!tr) return; if (e.target.classList.contains('tl-kaynak') || e.target.classList.contains('tl-tarih')) tlKurlariGetir(); });
  $('#tlGovde').addEventListener('input', e => { const tr = e.target.closest('tr'); if (tr && e.target.classList.contains('tl-kur')) tlTl(tr, TLM.satirlar[+tr.dataset.i]); });
  $('#tlHazirla').addEventListener('click', async () => {
    const kurlar = $$('#tlGovde tr').map(tr => { const r = TLM.satirlar[+tr.dataset.i]; if (!r || !r.pb || r.pb === 'TRY') return null; return { DocEntry: r.docEntry, DbName: r.dbName, Kaynak: tr.querySelector('.tl-kaynak').value, Kur: +tr.querySelector('.tl-kur').value || null, Tarih: tr.querySelector('.tl-tarih').value, Not: tr.querySelector('.tl-not').value }; }).filter(Boolean);
    const d = await post('/Rapor79/TalimatOlustur', { DbName: TLM.db, DocEntryler: TLM.satirlar.map(r => r.docEntry), BankaHesabi: $('#tlBankaKap').style.display === 'none' ? null : $('#tlBanka').value, Kasa: $('#tlKasaKap').style.display === 'none' ? null : $('#tlKasa').value, Kurlar: kurlar }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    modalKapat($('#knTalimatModal')); mesaj('success', '<i class="fas fa-file-contract me-1"></i>' + esc(d.message) + (d.talimatlar || []).map(t => ` <a href="${esc(t.link)}" target="_blank" class="ms-1"><i class="fas fa-print"></i> ${esc(t.no)} yazdır</a>`).join(''));
    await yukle(true); sekme('talimat');
  });
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-talimat-olustur'); if (!b) return; const t = $('#knTablo-' + b.dataset.p); talimatOlustur($$('.kn-sec:checked', t).map(c => +c.closest('tr').dataset.doc)); });

  // ---------- sekmeler / filtreler / üst kartlar
  function sekme(p) {
    if (!$(`#knSekmeler button[data-p="${p}"]`)) p = 'tumu';
    $$('#knSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('#kn > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    ciz();
    try { const u = new URL(location.href); if (p === 'tumu') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); u.searchParams.set('sirket', F.sirket || 'tumu'); history.replaceState(null, '', u); } catch (e) { }   // sirket=tumu: yeniden yüklenince konsolide kalır
  }
  $$('#knSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  $('#knSirket').addEventListener('change', () => { F.sirket = $('#knSirket').value; yukle(); });
  $('#knBas').addEventListener('change', () => { F.bas = $('#knBas').value; yukle(); }); $('#knBit').addEventListener('change', () => { F.bit = $('#knBit').value; yukle(); });
  $$('.kn-hizli button').forEach(b => b.addEventListener('click', () => { const d = new Date(); const bit = new Date(); bit.setDate(bit.getDate() + +b.dataset.gun); F.bas = d.toISOString().slice(0, 10); F.bit = bit.toISOString().slice(0, 10); $('#knBas').value = F.bas; $('#knBit').value = F.bit; yukle(); }));
  $('#knDurum').addEventListener('change', () => { F.durum = $('#knDurum').value; ciz(); }); $('#knSatinalmaci').addEventListener('change', () => { F.satinalmaci = $('#knSatinalmaci').value; ciz(); });
  $('#knAra').addEventListener('input', () => { F.ara = $('#knAra').value; ciz(); });
  $('#knTemizle').addEventListener('click', () => { F.durum = ''; F.satinalmaci = ''; F.ara = ''; F.kpi = ''; $('#knDurum').value = ''; $('#knSatinalmaci').value = ''; $('#knAra').value = ''; ciz(); });
  $('#knKpi').addEventListener('click', e => { const el = e.target.closest('[data-kpi],[data-p]'); if (!el) return; if (el.dataset.p) { sekme(el.dataset.p); return; } const f = el.dataset.kpi || ''; F.kpi = F.kpi === f ? '' : f; if (F.kpi === 'avans' && aktifSekme() !== 'nakit' && aktifSekme() !== 'tumu') sekme('nakit'); else if (F.kpi === 'onay' && !['tumu', 'nakit', 'cek', 'senet'].includes(aktifSekme())) sekme('tumu'); else ciz(); });
  $('#knExcel').addEventListener('click', () => { const p = aktifSekme(), t = $('#knTablo-' + p) || (p === 'talimat' ? $('#knTalimatlar') : $('#knBakiye')); if (!t) return; const bas = $$('thead th', t).map(x => x.textContent.trim()); const rows = $$('tbody tr', t).filter(r => !r.querySelector('.na-bos') && !r.classList.contains('tl-detay')).map(r => $$('td', r).map(td => { const i = td.querySelector('input:not([type=checkbox]),select'); return i ? i.value : td.textContent.trim().replace(/\s+/g, ' '); })); csvIndir('kasa_nakit_akisi_' + p, bas, rows); });

  // ---------- satır içi kayıt
  kok.addEventListener('change', async e => {
    const inp = e.target.closest('.kn-alan'); if (!inp) return; const tr = inp.closest('tr'); if (!tr) return;
    const d = await post('/Rapor79/AlanKaydet', { DbName: tr.dataset.db, DocEntry: +tr.dataset.doc, Alan: inp.dataset.alan, Deger: inp.value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message || 'Kaydedilemedi.'); if (d.row) { satirYerlestir(d.row); ciz(); } return; }
    satirYerlestir(d.row); inp.classList.add('ok'); setTimeout(() => inp.classList.remove('ok'), 1200);
    if (d.message && d.message.length > 12) mesaj('info', esc(d.message));
    if (inp.dataset.alan === 'U_Durum' || inp.dataset.alan === 'U_PlanlananTarih') ciz(); else kpi();
  });
  kok.addEventListener('click', e => {
    const tr = e.target.closest('tr.tik'); if (!tr || !kok.contains(tr) || tr.classList.contains('tl-satir')) return;
    if (e.target.closest('input,select,a,button,.kn-belge-detay,.kn-mukerrer')) return;
    const r = satirBul(+tr.dataset.doc); if (r) drawerAc(r);
  });
  const satirdan = el => { const tr = el.closest('tr'); return tr ? satirBul(+tr.dataset.doc) : null; };

  // ---------- BELGE DETAYI (belge no tıklanınca): başlık, kalemler, ödeme planı, mükerrer, cari açık kalemler
  async function belgeDetayAc(r) {
    $('#kdtBaslik').textContent = 'Belge Detayı'; $('#kdtKurum').textContent = r.kurum; $('#kdtBilgi').innerHTML = '<div class="na-bos">Yükleniyor…</div>'; $('#kdtUyari').innerHTML = ''; $('#kdtKalemler tbody').innerHTML = ''; $('#kdtPlan tbody').innerHTML = ''; $('#kdtCariKart').style.display = 'none'; $('#kdtModul').style.display = 'none';
    modalAc($('#knBelgeDetayModal'));
    const d = await getJ(`/Rapor79/BelgeDetay?dbName=${encodeURIComponent(r.dbName)}&docEntry=${r.docEntry}`).catch(() => null); if (!d || !d.success) { $('#kdtBilgi').innerHTML = '<div class="na-bos text-danger">Belge detayı alınamadı.</div>'; return; }
    const b = d.belge, oz = b.ozet || {}, pb = pbAd(b.pb);
    $('#kdtBaslik').textContent = `${b.tip}${b.no ? ' ' + b.no : ''}`;
    $('#kdtBilgi').innerHTML = [['Belge Tipi · No', `${b.tip}${b.no ? ' · ' + b.no : ''}${b.bagliDocEntry ? ' · kayıt ' + b.bagliDocEntry : ''}`], ['Belge Tarihi', tarih(b.tarih)], ['Son Ödeme Tarihi', tarih(b.sonOdeme)], ['Cari', b.kurum], ['Fatura Sahibi · Ödeyen', `${b.faturaSahibi} · ${b.odeyenSirket}`], ['Satınalma Temsilcisi', b.satinalmaci || '—'],
      ['Belge Tutarı', `${para(b.belgeTutari)} ${pb}${b.pb !== 'TRY' ? ' · ' + para(b.belgeTutariTL) + ' ₺ (kur ' + b.kur + ')' : ''}`], ['KDV Hariç · KDV', `${para(b.net)} · ${para(b.kdv)} ${pb}`], ['Bu Satır', `${para(r.tutar)} ${pb}${oz.oran != null ? ' · %' + oz.oran : ''}${oz.taksit ? ' · taksit ' + oz.taksit : ''}`], ['Planlanan Toplam', `${para(oz.planlanan)} ${pb}`], ['Ödenen', `${para(oz.odenen)} ${pb}`], ['Kalan (Belgeden)', oz.kalan != null ? `${para(oz.kalan)} ${pb}` : '—']].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    $('#kdtUyari').innerHTML = (oz.uyarilar || []).map(u => `<div class="na-uyari kirmizi"><b>${esc(u.tip)}</b> · ${esc(u.aciklama)}${u.docEntryler?.length ? ' · diğer kayıtlar: ' + u.docEntryler.map(x => '#' + x).join(', ') : ''}</div>`).join('') || (b.tip === 'Fatura' || b.tip === 'Sipariş' ? '<div class="na-uyari mavi"><b>Mükerrer kontrolü</b> · bu belge için başka aynı tutarlı kayıt yok; planlanan toplam belge tutarını aşmıyor.</div>' : '');
    const kal = b.kalemler || []; $('#kdtKalemler tbody').innerHTML = kal.length ? kal.map(k => `<tr><td>${esc(k.kod)}</td><td class="sol">${esc(k.ad)}</td><td class="para">${(k.miktar || 0).toLocaleString('tr-TR')}</td><td>${esc(k.birim)}</td><td class="para">${para(k.birimFiyat)}</td><td class="para">${para(k.tutar)}</td><td class="para">${para(k.kdv)}</td></tr>`).join('') : '<tr><td colspan="7" class="na-bos">Kalem yok (cari bakiye).</td></tr>';
    $('#kdtKalemler tfoot').innerHTML = kal.length ? `<tr class="toplam"><td colspan="5" class="text-end">TOPLAM (${esc(pb)})</td><td class="para">${para(kal.reduce((s, k) => s + k.tutar, 0))}</td><td class="para">${para(kal.reduce((s, k) => s + k.kdv, 0))}</td></tr>` : ''; $('#kdtKalemNot').textContent = kal.length ? `${kal.length} kalem · ön yüz örneği (deterministik)` : '';
    const planSat = [{ docEntry: r.docEntry, sirket: r.sirket, planlanan: r.planlanan, tutar: r.tutar, pb: r.pb, oran: oz.oran, durum: r.durum, yontem: r.yontem, odenme: r.odenme, bu: true }, ...(oz.kardesler || [])].sort((a, c) => (a.planlanan || '').localeCompare(c.planlanan || ''));
    $('#kdtPlan tbody').innerHTML = planSat.map(s => `<tr class="${s.bu ? 'fw-bold' : ''}"><td>#${s.docEntry}${s.bu ? ' (bu)' : ''}</td><td class="sol">${esc(s.sirket)}</td><td>${tarih(s.planlanan)}</td><td class="para">${para(s.tutar)} ${esc(pbAd(s.pb))}</td><td class="para">${s.oran != null ? '%' + s.oran : '—'}</td><td>${yontemRozet(s.yontem)}</td><td>${durumRozet(s.durum)}</td><td>${tarih(s.odenme)}</td></tr>`).join('');
    $('#kdtPlan tfoot').innerHTML = `<tr class="toplam"><td colspan="3" class="text-end">PLANLANAN TOPLAM</td><td class="para">${para(oz.planlanan)} ${esc(pb)}</td><td class="para">${oz.faturaTutari ? '%' + Math.round(oz.planlanan / oz.faturaTutari * 1000) / 10 : ''}</td><td colspan="3" class="sol">belge ${para(oz.faturaTutari)} · ödenen ${para(oz.odenen)} · kalan ${oz.kalan != null ? para(oz.kalan) : '—'}</td></tr>`;
    if (b.acikKalemler) { $('#kdtCariKart').style.display = ''; $('#kdtCari tbody').innerHTML = b.acikKalemler.map(k => `<tr><td>${esc(k.belge)}</td><td>${esc(k.no)}</td><td>${tarih(k.tarih)}</td><td class="${k.vade < bugun() ? 'gec' : ''}">${tarih(k.vade)}</td><td class="para">${para(k.tutar)}</td><td class="para">${para(k.odenen)}</td><td class="para">${para(k.kalan)}</td></tr>`).join(''); $('#kdtCari tfoot').innerHTML = `<tr class="toplam"><td colspan="6" class="text-end">CARİ BAKİYE (TL)</td><td class="para">${para(b.cariBakiye)}</td></tr>`; $('#kdtCariNot').textContent = `${b.acikKalemler.length} açık kalem · satır tutarı ${para(r.tutar)} ${pb}`; }
    if (b.link) { $('#kdtModul').style.display = ''; $('#kdtModul').href = b.link; }
    $('#kdtOzet').textContent = `#${r.docEntry} · ${r.sirket} · ${r.yontem} · ${r.durum}`;
  }
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-belge-detay'); if (!b) return; e.stopPropagation(); const r = satirdan(b) || acik; if (r) belgeDetayAc(r); });

  // ---------- ONAY: nakit talebi / GMY çek çıkışı
  let O = { r: null, tip: 'nakit' };
  function onayAc(r, tip) {
    O = { r, tip }; const liste = tip === 'cekCikis' ? (V.onaycilar?.cekCikis || []) : (V.onaycilar?.nakit || []);
    $('#koBaslik').textContent = tip === 'cekCikis' ? 'GMY Çek Çıkış Onayı' : 'Nakit Ödeme Talebi Onayı';
    const b = (r.belgeler || [])[0];
    $('#koBilgi').innerHTML = (tip === 'cekCikis' ? [['Belge', b ? `${b.belge} ${b.no} · ${b.banka}` : r.cekNolar], ['Tutar', `${para(r.tutar)} ${pbAd(r.pb)}`], ['Portföy Şirketi', r.odeyenSirket], ['Talep Edilen Çıkış', `${r.cikis.islem}${r.cikis.ibrazYeri ? ' → ' + r.cikis.ibrazYeri : ''}`], ['Talep Eden', r.cikis.talepEden], ['Durum', r.onay?.durum || '']] : [['Ödenecek', r.kurum], ['Tutar', `${para(r.tutar)} ${pbAd(r.pb)}`], ['Fatura Sahibi · Ödeyen', `${r.faturaSahibi} · ${r.odeyenSirket}`], ['Belge', `${r.belgeTipi || ''} ${r.faturaNo || ''}${r.avansNo ? ' · avans ' + r.avansNo : ''}`], ['Planlanan Ödeme', tarih(r.planlanan)], ['Kasa', r.alanlar?.kasa || '—']]).map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    $('#koNot').innerHTML = tip === 'cekCikis' ? `<b>Kasa Nakit Akışı'nda planlanmamış çek çıkışı.</b> Onaylanırsa belge ${esc(r.cikis.islem.toLocaleLowerCase('tr-TR'))} olarak çıkar ve satır ÖDENDİ olur; reddedilirse belge portföyde kalır, satır kaldırılır. Çek Akıbeti ve Takip Paneli güncellenir.` : `<b>Nakit yalnız onaylı talep için verilir.</b> Onaylanınca satır Planlandı olur; "Makbuz Kes" ile kasa tediye makbuzu kesilir (satır ÖDENDİ, muhasebe fişi otomatik) ya da kasa talimatına alınır.`;
    $('#koOnaylayan').innerHTML = liste.map(x => `<option ${x === V.kullanici ? 'selected' : ''}>${esc(x)}</option>`).join(''); $('#koNotGiris').value = '';
    modalAc($('#knOnayModal'));
  }
  async function onayGonder(karar) {
    const r = O.r; const d = await post(O.tip === 'cekCikis' ? '/Rapor79/CekCikisOnay' : '/Rapor79/NakitOnay', { DbName: r.dbName, DocEntry: r.docEntry, Karar: karar, Onaylayan: $('#koOnaylayan').value, Not: $('#koNotGiris').value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    modalKapat($('#knOnayModal')); if (d.kaldirildi) satirKaldir(r.docEntry); else if (d.row) satirYerlestir(d.row);
    mesaj(karar === 'Reddet' ? 'warning' : 'success', '<i class="fas fa-user-check me-1"></i>' + esc(d.message)); ciz(); if (acik && acik.docEntry === r.docEntry) { if (d.kaldirildi) drawerKapat(); else drawerAc(d.row); }
  }
  $('#koOnayla').addEventListener('click', () => onayGonder('Onayla')); $('#koReddet').addEventListener('click', () => { if (confirm('Talep reddedilsin mi?')) onayGonder('Reddet'); });
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-onay-ac'); if (!b) return; e.stopPropagation(); const r = satirdan(b) || acik; if (r) onayAc(r, 'nakit'); });
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-cikis-onay'); if (!b) return; e.stopPropagation(); const r = satirdan(b) || acik; if (r) onayAc(r, 'cekCikis'); });

  // ---------- MAKBUZ (kasa tediye)
  let M = { r: null };
  function makbuzAc(r) {
    M = { r }; $('#kmKurum').textContent = r.kurum;
    $('#kmBilgi').innerHTML = [['Ödenecek', r.kurum], ['Tutar', `${para(r.tutar)} ${pbAd(r.pb)}${r.pb !== 'TRY' ? ' · ' + para(r.tutarTL) + ' ₺' : ''}`], ['Fatura Sahibi · Ödeyen', `${r.faturaSahibi} · ${r.odeyenSirket}`], ['Belge', `${r.belgeTipi || ''} ${r.faturaNo || ''}${r.avansNo ? ' · avans ' + r.avansNo : ''}`], ['Onay', r.onay ? `${r.onay.durum} · ${r.onay.onaylayan}` : '—'], ['Planlanan Ödeme', tarih(r.planlanan)]].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    $('#kmKasa').innerHTML = V.kasalar.map(k => `<option ${k === (r.alanlar?.kasa || V.kasalar[0]) ? 'selected' : ''}>${esc(k)}</option>`).join(''); $('#kmTeslimAlan').value = r.alanlar?.teslimAlan || (r.avans ? r.avans.kullanan : r.kurum);
    modalAc($('#knMakbuzModal'));
  }
  $('#kmKes').addEventListener('click', async () => {
    const r = M.r; const d = await post('/Rapor79/MakbuzOlustur', { DbName: r.dbName, DocEntry: r.docEntry, TeslimAlan: $('#kmTeslimAlan').value, Kasa: $('#kmKasa').value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    modalKapat($('#knMakbuzModal')); if (d.row) satirYerlestir(d.row); mesaj('success', '<i class="fas fa-receipt me-1"></i>' + esc(d.message) + (d.makbuz ? ` <a href="${esc(d.makbuz.link)}" target="_blank" class="ms-1"><i class="fas fa-print"></i> ${esc(d.makbuz.no)} yazdır</a>` : '')); await yukle(true); if (acik && acik.docEntry === r.docEntry) drawerAc(satirBul(r.docEntry));
  });
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-makbuz-ac'); if (!b) return; e.stopPropagation(); const r = satirdan(b) || acik; if (r) makbuzAc(r); });

  // ---------- KISMİ ÖDEME / TAKSİT
  let T = { r: null };
  function taksitAc(r) {
    T = { r }; $('#ktKurum').textContent = r.kurum; const fat = r.plan?.faturaTutari || r.tutar;
    $('#ktBilgi').innerHTML = [['Belge', `${r.belgeTipi || ''} ${r.faturaNo || ''}`], ['Belge Tutarı', `${para(fat)} ${pbAd(r.pb)}`], ['Bu Satır', `${para(r.tutar)} ${pbAd(r.pb)}${r.plan?.oran != null ? ' · %' + r.plan.oran + ' (belgeye göre)' : ''}`], ['Planlanan Ödeme', tarih(r.planlanan)], ['Son Ödeme', tarih(r.sonOdeme)], ['Yöntem', r.yontem]].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    taksitParcalar([[70, r.planlanan || bugun()], [30, gunEkle(r.planlanan || bugun(), 30)]]); modalAc($('#knTaksitModal'));
  }
  const gunEkle = (s, n) => { const d = new Date(s); d.setDate(d.getDate() + n); return d.toISOString().slice(0, 10); };
  function taksitParcalar(l) { $('#ktParcalar').innerHTML = l.map((p, i) => `<div class="kn-taksit-satir"><span>${i + 1}</span><input type="number" class="form-control form-control-sm kt-oran" value="${p[0]}" min="0" max="100" step="0.1" style="font-size:.72rem"><input type="date" class="form-control form-control-sm kt-plan" value="${p[1]}" style="font-size:.72rem"><span class="kt-tutar para"></span><button type="button" class="btn btn-link btn-sm p-0 text-danger kt-sil"><i class="fas fa-times"></i></button></div>`).join(''); taksitHesap(); }
  function taksitHesap() { const r = T.r; let top = 0; $$('#ktParcalar .kn-taksit-satir').forEach(s => { const o = +s.querySelector('.kt-oran').value || 0; top += o; s.querySelector('.kt-tutar').textContent = para(r.tutar * o / 100) + ' ' + pbAd(r.pb); }); $('#ktToplam').innerHTML = `Toplam <b class="${Math.abs(top - 100) > 0.5 ? 'text-danger' : 'text-success'}">%${top.toLocaleString('tr-TR', { maximumFractionDigits: 1 })}</b> · ${para(r.tutar)} ${pbAd(r.pb)}`; }
  $('#ktParcalar').addEventListener('input', taksitHesap); $('#ktParcalar').addEventListener('click', e => { const b = e.target.closest('.kt-sil'); if (b) { b.closest('.kn-taksit-satir').remove(); taksitHesap(); } });
  $('#ktEkle').addEventListener('click', () => { const l = $$('#ktParcalar .kn-taksit-satir').map(s => [+s.querySelector('.kt-oran').value || 0, s.querySelector('.kt-plan').value]); const son = l[l.length - 1] || [0, T.r.planlanan || bugun()]; l.push([0, gunEkle(son[1], 30)]); taksitParcalar(l); });
  $('#kt7030').addEventListener('click', () => taksitParcalar([[70, T.r.planlanan || bugun()], [30, gunEkle(T.r.planlanan || bugun(), 30)]]));
  $('#kt3x').addEventListener('click', () => { const b = T.r.planlanan || bugun(); taksitParcalar([[33.4, b], [33.3, gunEkle(b, 30)], [33.3, gunEkle(b, 60)]]); });
  $('#ktKaydet').addEventListener('click', async () => {
    const r = T.r; const parcalar = $$('#ktParcalar .kn-taksit-satir').map(s => ({ Oran: +s.querySelector('.kt-oran').value || 0, Planlanan: s.querySelector('.kt-plan').value })).filter(p => p.Oran > 0);
    const d = await post('/Rapor79/Taksitlendir', { DbName: r.dbName, DocEntry: r.docEntry, Parcalar: parcalar }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    modalKapat($('#knTaksitModal')); (d.rows || []).forEach(satirYerlestir); mesaj('success', '<i class="fas fa-percent me-1"></i>' + esc(d.message)); await yukle(true); if (acik && acik.docEntry === r.docEntry) drawerAc(satirBul(r.docEntry));
  });

  // ---------- PORTFÖYDEN BELGE SEÇİMİ (Çek / Senet)
  let secim = { r: null, belge: 'Çek', liste: [], sirket: '' };
  const kbSayac = () => { const c = $$('.kb-chk:checked'); let t = 0; c.forEach(x => t += +x.dataset.tutar || 0); $('#kbSayi').textContent = c.length; $('#kbToplam').textContent = para(t); const st = secim.r ? secim.r.tutar : 0; $('#kbFark').textContent = c.length && st ? `(satır ${para(st)} · fark ${t - st >= 0 ? '+' : ''}${para(t - st)}; satır tutarı belge toplamı olur)` : ''; };
  const vadeRozet = v => rozet(v || '', v === 'Vadesi Geçti' ? 'hata' : v === 'Bugün' || v === 'Bu Hafta' ? 'bekle' : v === 'Bu Ay' ? 'bilgi' : 'notr');
  function kbSuz() {
    const q = ($('#kbAra').value || '').toLocaleLowerCase('tr-TR'), vade = $('#kbVade').value, min = +$('#kbMin').value || 0, max = +$('#kbMax').value || 0, banka = $('#kbBanka').value, planli = $('#kbPlanli').checked, sira = $('#kbSira').value;
    const gun = s => Math.round((new Date(s) - new Date(bugun())) / 86400000);
    let l = secim.liste.filter(x => (planli || x.secilebilir) && (!q || [x.no, x.asilBorclu, x.kimdenGeldi, x.banka, x.musteriKod].join(' ').toLocaleLowerCase('tr-TR').includes(q)) && (!min || x.tutar >= min) && (!max || x.tutar <= max) && (!banka || x.banka === banka)
      && (!vade || (vade === 'gecmis' ? gun(x.vade) < 0 : vade === 'uzak' ? gun(x.vade) > 60 : gun(x.vade) >= 0 && gun(x.vade) <= +vade)));
    const hedef = secim.r ? secim.r.tutarTL || secim.r.tutar : 0, hedefGun = secim.r && secim.r.sonOdeme ? gun(secim.r.sonOdeme) : 0;
    if (sira === 'tutarAzalan') l.sort((a, b) => b.tutar - a.tutar); else if (sira === 'tutarArtan') l.sort((a, b) => a.tutar - b.tutar); else if (sira === 'yakin') l.sort((a, b) => Math.abs(a.tutar - hedef) - Math.abs(b.tutar - hedef)); else if (sira === 'vadeYakin') l.sort((a, b) => Math.abs(gun(a.vade) - hedefGun) - Math.abs(gun(b.vade) - hedefGun) || a.vade.localeCompare(b.vade)); else l.sort((a, b) => a.vade.localeCompare(b.vade));
    return l;
  }
  function kbCiz() {
    const l = kbSuz(), secili = new Set($$('.kb-chk:checked').map(c => +c.value));
    $('#kbGovde').innerHTML = l.length ? l.map(x => `<tr class="${x.secilebilir ? '' : 'text-muted'}"><td><input type="checkbox" class="form-check-input kb-chk" value="${x.docEntry}" data-tutar="${x.tutar}" ${x.secilebilir ? '' : 'disabled'} ${secili.has(x.docEntry) ? 'checked' : ''}></td><td class="fw-bold">${esc(x.no)}</td><td><span class="kn-oran">${esc(x.tip || 'X')}</span></td><td>${esc(x.banka)} / ${esc(x.sube)}</td><td>${esc(x.asilBorclu)}</td><td>${esc(x.kimdenGeldi)}<br><small class="text-muted">${esc(x.musteriKod)}</small></td><td>${tarih(x.vade)}</td><td>${vadeRozet(x.vadeDurumu)}</td><td class="text-end fw-bold">${para(x.tutar)} ₺</td><td>${tarih(x.portfoyGiris)}</td><td>${x.secilebilir ? '<span class="rt-durum rt-ok">Portföyde</span>' : akibetRozet(x.akibet) + (x.planlananTedarikci ? `<br><small>${esc(x.planlananTedarikci)}${x.planDocEntry ? ' · #' + x.planDocEntry : ''}</small>` : x.kullanim !== 'Kullanılabilir' ? `<br><small>${esc(x.kullanim)}</small>` : '')}</td></tr>`).join('') : `<tr><td colspan="11" class="text-center text-muted py-3">Süzgece uyan belge yok.</td></tr>`;
    const hepsi = secim.liste, uygun = hepsi.filter(x => x.secilebilir); $('#kbOzet').textContent = `${secim.sirket} portföyü: ${uygun.length} seçilebilir ${secim.belge.toLocaleLowerCase('tr-TR')} · ${para(uygun.reduce((s, x) => s + x.tutar, 0))} ₺ (X ${kisa(uygun.filter(x => (x.tip || 'X') === 'X').reduce((s, x) => s + x.tutar, 0))} · R ${kisa(uygun.filter(x => x.tip === 'R').reduce((s, x) => s + x.tutar, 0))}) · toplam ${hepsi.length} belge (planlı ${hepsi.length - uygun.length}) · listede ${l.length}`;
    $('#kbHepsi').checked = false; kbSayac();
  }
  async function kbYukle() {
    $('#kbGovde').innerHTML = '<tr><td colspan="11" class="text-center text-muted py-3"><i class="fas fa-spinner fa-spin"></i> Portföy getiriliyor…</td></tr>';
    try { const d = await getJ(`/Rapor79/SecilebilirBelgeler?dbName=${encodeURIComponent(secim.r.dbName)}&belge=${encodeURIComponent(secim.belge)}&sirket=${encodeURIComponent($('#kbSirket').value)}&hepsi=true`); secim.liste = d.data || []; secim.sirket = d.sirket || ''; $('#kbBanka').innerHTML = '<option value="">Tümü</option>' + [...new Set(secim.liste.map(x => x.banka))].sort().map(b => `<option>${esc(b)}</option>`).join(''); kbCiz(); } catch (x) { $('#kbGovde').innerHTML = '<tr><td colspan="11" class="text-center text-danger py-3">Ağ hatası.</td></tr>'; }
  }
  function belgeSecAc(r) {
    secim = { r, belge: r.yontem === 'Senet' ? 'Senet' : 'Çek', liste: [], sirket: '' };
    $('#kbBaslik').textContent = secim.belge + ' Seç · Portföy'; $('#kbKurum').textContent = r.kurum; $('#kbBilgi').innerHTML = `Fatura sahibi <b>${esc(r.faturaSahibi)}</b> · ödeyen <b>${esc(r.odeyenSirket)}</b> · satır tutarı <b>${para(r.tutarTL || r.tutar)} ₺</b>${r.pb && r.pb !== 'TRY' ? ` (${para(r.tutar)} ${esc(r.pb)} · dövizli fatura TL çekle: çekin tahsil günü kuru baz alınır, kur farkı faturası)` : ''}${r.sonOdeme ? ` · <b>son ödeme ${tarih(r.sonOdeme)}</b> → vadesi buna yakın belgeler önce` : ''}. Seçilen belgeler Çek Akıbeti Raporu'nda "Planlandı" olur; başka grup şirketinin portföyünden seçilirse ödeyen şirket o şirket olur. "Planla ve Dolaşımı Planla" ile her belge için dolaşım planı (uygunluk simülasyonuyla) hemen kurulur.`;
    const sel = $('#kbSirket'); sel.innerHTML = V.sirketler.map(s => `<option value="${esc(s.dbName)}" ${s.ad === r.odeyenSirket || (!V.sirketler.some(x => x.ad === r.odeyenSirket) && s.dbName === r.dbName) ? 'selected' : ''}>${esc(s.ad)}</option>`).join('');
    $('#kbAra').value = ''; $('#kbVade').value = ''; $('#kbMin').value = ''; $('#kbMax').value = ''; $('#kbPlanli').checked = false; $('#kbSira').value = r.sonOdeme ? 'vadeYakin' : 'vade'; kbSayac();
    modalAc($('#knBelgeModal')); kbYukle();
  }
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-belge-sec'); if (!b) return; e.stopPropagation(); const r = satirdan(b) || acik; if (r) belgeSecAc(r); });
  ['#kbAra', '#kbVade', '#kbMin', '#kbMax', '#kbBanka', '#kbPlanli', '#kbSira'].forEach(s => $(s).addEventListener(s === '#kbAra' || s === '#kbMin' || s === '#kbMax' ? 'input' : 'change', kbCiz));
  $('#kbSirket').addEventListener('change', kbYukle);
  $('#kbGovde').addEventListener('change', kbSayac); $('#kbHepsi').addEventListener('change', () => { $$('.kb-chk:not(:disabled)').forEach(c => c.checked = $('#kbHepsi').checked); kbSayac(); });
  $('#kbYakin').addEventListener('click', () => {
    const hedef = secim.r ? secim.r.tutar : 0; if (!hedef) return; const l = kbSuz().filter(x => x.secilebilir).sort((a, b) => b.tutar - a.tutar); let kalan = hedef, sec = [];
    l.forEach(x => { if (x.tutar <= kalan * 1.05 && kalan > 0) { sec.push(x.docEntry); kalan -= x.tutar; } });
    if (!sec.length && l.length) { const en = l.reduce((a, b) => Math.abs(a.tutar - hedef) < Math.abs(b.tutar - hedef) ? a : b); sec.push(en.docEntry); }
    $$('.kb-chk').forEach(c => c.checked = sec.includes(+c.value)); kbSayac(); mesaj('info', `${sec.length} belge seçildi (satır tutarına en yakın toplam).`);
  });
  async function belgePlanla(dolasimla) {
    const ids = $$('.kb-chk:checked').map(c => +c.value); if (!ids.length) { mesaj('warning', 'En az bir belge seçin.'); return; }
    const d = await post('/Rapor79/BelgePlanla', { DbName: secim.r.dbName, DocEntry: secim.r.docEntry, CekDocEntryler: ids }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message || 'Planlanamadı.'); return; }
    modalKapat($('#knBelgeModal')); satirYerlestir(d.row); mesaj('success', '<i class="fas fa-check me-1"></i>' + esc(d.message) + ' <a href="/CekAkibeti" target="_blank" class="ms-1"><i class="fas fa-arrow-up-right-from-square"></i> Çek Akıbeti Raporu\'nda gör</a>'); ciz(); if (acik && acik.docEntry === d.row.docEntry) drawerAc(d.row);
    const r = d.row, secilenler = (r.belgeler || []).filter(x => ids.includes(x.cekDocEntry));
    // dolaşım: istenirse tüm seçilen belgeler sırayla; istenmezse yalnız başka şirket portföyünden gelen (fatura sahibine ulaşması gereken) belge
    const kuyruk = dolasimla ? secilenler : secilenler.filter(x => x.sahip && x.sahip !== r.faturaSahibi && !x.adimToplam);
    if (kuyruk.length) setTimeout(() => { D.kuyruk = kuyruk.slice(1); dolasimAc(r, kuyruk[0]); }, 400);
  }
  $('#kbOnayla').addEventListener('click', () => belgePlanla(false)); $('#kbOnaylaDolasim').addEventListener('click', () => belgePlanla(true));
  async function belgeKaldir(r, cekDocEntry) {
    if (!confirm('Belgenin planı kaldırılsın mı? Belge portföye döner (Çek Akıbeti: Portföyde).')) return;
    const d = await post('/Rapor79/BelgePlanKaldir', { DbName: r.dbName, DocEntry: r.docEntry, CekDocEntry: cekDocEntry }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } satirYerlestir(d.row); mesaj('info', esc(d.message)); ciz(); if (acik && acik.docEntry === r.docEntry) drawerAc(d.row);
  }
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-belge-kaldir'); if (!b || b.disabled) return; e.stopPropagation(); const r = satirdan(b) || acik; if (r) belgeKaldir(r, +b.dataset.cek); });
  // değiştir: mevcut belgenin planı kaldırılır, portföy penceresi açılır
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.kn-belge-degistir'); if (!b) return; e.stopPropagation(); const r = satirdan(b) || acik; if (!r) return;
    if (!confirm('Belge kaldırılıp portföyden yenisi seçilsin mi? (mevcut belge portföye döner)')) return;
    const d = await post('/Rapor79/BelgePlanKaldir', { DbName: r.dbName, DocEntry: r.docEntry, CekDocEntry: +b.dataset.cek }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } satirYerlestir(d.row); ciz(); if (acik && acik.docEntry === r.docEntry) drawerAc(d.row); belgeSecAc(d.row);
  });

  // ---------- GRUP İÇİ DOLAŞIM PLANI (zincir + uygunluk simülasyonu)
  let D = { r: null, b: null, zincir: [], sim: null, kuyruk: [] };
  const adimAlan = (a, k) => a[k] ?? a[k.charAt(0).toLowerCase() + k.slice(1)];
  const kalanZincir = b => (b.adimlar || []).filter(a => adimAlan(a, 'Tur') === 'Grup İçi Ciro' && adimAlan(a, 'Durum') !== 'Gerçekleşti').map(a => adimAlan(a, 'Alan'));
  function dolasimBaslikCiz() {
    const r = D.r, b = D.b;
    $('#kdBelge').textContent = `${b.no} · ${para(b.tutar)} ₺ · şu an ${b.sahip} portföyünde${D.kuyruk?.length ? ' · sırada ' + D.kuyruk.length + ' belge daha' : ''}`;
    $('#kdBilgi').innerHTML = [['Belge', `${b.belge} ${b.no} · ${b.banka}`], ['Keşideci / Kimden', `${b.asilBorclu} / ${b.kimdenGeldi}`], ['Vade · Tutar', `${tarih(b.vade)} · ${para(b.tutar)} ₺`], ['Şu Anki Sahip', (b.sahip || '—') + (b.kaynakSirket && b.kaynakSirket !== b.sahip ? ` (ilk portföy ${b.kaynakSirket})` : '')], ['Fatura Sahibi · Ödeyen', `${r.faturaSahibi} · ${r.odeyenSirket}`], ['Tedarikçi (Ödenecek Cari)', r.kurum]].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    $('#kdBakiye tbody').innerHTML = (V.grupBakiyeleri || []).map(x => `<tr><td class="sol">${esc(x.borclu)}</td><td class="sol">${esc(x.alacakli)}</td><td class="para">${para(x.tutar)}</td></tr>`).join('');
  }
  function dolasimAc(r, b) {
    D = { r, b, zincir: b.adimToplam ? kalanZincir(b) : [], sim: null, kuyruk: D.kuyruk || [] };
    dolasimBaslikCiz();
    $('#kdTedarikci').value = b.planlananTedarikci || r.kurum; $('#kdSonAdim').value = 'Tedarikçiye Ciro';
    const sonAdim = (b.adimlar || []).find(a => adimAlan(a, 'Tur') !== 'Grup İçi Ciro'); if (sonAdim) $('#kdSonAdim').value = adimAlan(sonAdim, 'Tur');
    dolasimZincirCiz(); dolasimPlanCiz(); modalAc($('#knDolasimModal'));
    if (!b.adimToplam && !D.zincir.length && b.sahip !== r.faturaSahibi) dolasimOner(); else dolasimSimule();
  }
  function dolasimZincirCiz() {
    const sahip = D.b.sahip, halkalar = [`<span class="halka"><i class="fas fa-building text-muted"></i> ${esc(sahip)}</span>`];
    D.zincir.forEach((c, i) => halkalar.push(`<span class="ok"><i class="fas fa-arrow-right"></i></span><span class="halka"><select data-i="${i}" class="kd-halka">${dSirketler().map(s => `<option ${s === c ? 'selected' : ''}>${esc(s)}</option>`).join('')}</select><button type="button" class="btn btn-link btn-sm p-0 text-danger kd-sil" data-i="${i}" title="Halkayı kaldır"><i class="fas fa-times"></i></button></span>`));
    halkalar.push(`<span class="ok"><i class="fas fa-arrow-right"></i></span><button type="button" class="btn btn-light btn-sm border" id="kdEkle" style="font-size:.66rem"><i class="fas fa-plus"></i> Şirket Ekle</button><span class="ok"><i class="fas fa-arrow-right"></i></span><span class="halka"><i class="fas ${$('#kdSonAdim').value === 'Bankaya İbraz' ? 'fa-building-columns' : 'fa-truck'} text-muted"></i> ${esc($('#kdSonAdim').value === 'Bankaya İbraz' ? 'Banka (Tahsil)' : $('#kdTedarikci').value || 'Tedarikçi')}</span>`);
    $('#kdZincir').innerHTML = halkalar.join('');
    const kayitli = D.b.adimToplam > 0, basladi = kayitli && D.b.gerceklesen > 0; $('#kdIptal').style.display = kayitli && !basladi ? '' : 'none'; $('#kdKaydet').innerHTML = `<i class="fas fa-save me-1"></i>${kayitli ? 'Planı Güncelle' : 'Planı Kaydet'}`; $('#kdKaydet').disabled = basladi; $('#kdKaydet').title = basladi ? 'Gerçekleşmiş adımı olan plan değiştirilemez; kalan adımlar plan kartından gerçekleştirilir' : '';
    $$('#kdZincir select, #kdZincir button').forEach(x => x.disabled = basladi); $('#kdSonAdim').disabled = basladi; $('#kdTedarikci').disabled = basladi; $('#kdOner').disabled = basladi;
  }
  function dolasimPlanCiz() {
    const b = D.b, kart = $('#kdPlanKart'); if (!b.adimToplam) { kart.style.display = 'none'; return; } kart.style.display = '';
    const sonraki = (b.adimlar || []).find(a => adimAlan(a, 'Durum') === 'Planlandı');
    $('#kdAdimlar').innerHTML = (b.adimlar || []).map(a => { const d = adimAlan(a, 'Durum'), s = adimAlan(a, 'Sira'); return `<span class="adim ${d === 'Gerçekleşti' ? 'ok' : 'plan'}"><b>${s}.</b> ${esc(adimAlan(a, 'Eden'))} → ${esc(adimAlan(a, 'Alan'))} <small class="text-muted">${esc(adimAlan(a, 'Tur'))} · ${d === 'Gerçekleşti' ? 'gerçekleşti ' + tarih(adimAlan(a, 'GercekTarih')) + ' · ' + esc(adimAlan(a, 'BelgeNo')) : 'plan ' + tarih(adimAlan(a, 'PlanTarih'))}</small>${sonraki && adimAlan(sonraki, 'Sira') === s ? ` <button type="button" class="btn btn-primary btn-sm py-0 px-1 kd-gerceklestir" data-sira="${s}" style="font-size:.62rem" title="Ciro / ibraz kaydı atılır, grup içi bakiye güncellenir, muhasebe fişleri atılır, Takip Paneli'ne düşer"><i class="fas fa-check"></i> Gerçekleştir</button>` : ''}</span>`; }).join('');
    $('#kdPlanNot').textContent = `${b.gerceklesen}/${b.adimToplam} adım gerçekleşti · ${b.dolasim}${b.gerceklesen < b.adimToplam ? ' · simülasyon kalan adımlar için, belgenin şu anki sahibinden başlar' : ''}`;
  }
  async function dolasimSimule() {
    const gov = { CekDocEntry: D.b.cekDocEntry, Zincir: D.zincir, SonAdim: $('#kdSonAdim').value, Tedarikci: $('#kdTedarikci').value, FaturaSahibi: D.r.faturaSahibi };
    const d = await post('/Rapor79/DolasimSimule', gov).catch(() => null); if (!d || !d.success) return; D.sim = d.sonuc; const s = d.sonuc;
    $('#kdSim tbody').innerHTML = s.adimlar.length ? s.adimlar.map(a => `<tr><td>${a.sira}</td><td class="sol">${esc(a.eden)}</td><td class="sol">${esc(a.alan)}</td><td>${esc(a.tur)}</td><td class="para">${a.tur === 'Grup İçi Ciro' ? para(a.bakiye) : '—'}</td><td class="para">${a.tur === 'Grup İçi Ciro' ? para(a.kapanan) : '—'}</td><td class="para ${a.yeniBorc > 0 ? 'text-danger' : ''}">${a.tur === 'Grup İçi Ciro' ? para(a.yeniBorc) : '—'}</td><td class="${a.sonuc === 'Uygun' ? 'uygun' : a.sonuc === 'Kısmi' ? 'kismi' : 'uyari'}">${esc(a.sonuc)}</td><td class="sol" style="white-space:normal">${esc(a.not)}</td></tr>`).join('') : '<tr><td colspan="9" class="na-bos text-center">Zincire şirket ekleyin.</td></tr>';
    const derece = s.derece || (s.uygun ? 'Uygun' : 'Uyarı');
    const u = $('#kdUyari'); u.className = 'na-uyari ' + (derece === 'Uygun' ? 'mavi' : derece === 'Kısmi' ? 'sari' : 'kirmizi'); u.innerHTML = `<b>${derece === 'Uygun' ? 'UYGUN · zincir boyunca grup içi borçlar kapanıyor' : derece === 'Kısmi' ? 'KISMİ UYGUN · borçlar kapanıyor ama belge tutarı borçlardan büyük' : 'UYARI · borç ilişkisi olmayan adım var, grup içi bakiye şişer'}</b> · kapanan grup içi borç ${para(s.toplamKapanan)} ₺ · oluşan yeni bakiye ${para(s.toplamYeniBorc)} ₺ · zincir sonu ${esc(s.sonSahip)}${s.uyarilar.length ? '<br>' + s.uyarilar.map(esc).join('<br>') : ''}`;
    $('#kdOzet').textContent = s.ozet;
  }
  async function dolasimOner() {
    const d = await getJ(`/Rapor79/DolasimOner?cekDocEntry=${D.b.cekDocEntry}&hedef=${encodeURIComponent(D.r.faturaSahibi)}`).catch(() => null);
    if (d && d.success && d.zincir.length) { D.zincir = d.zincir; mesaj('info', `Öneri: ${D.b.sahip} → ${d.zincir.join(' → ')} (mevcut grup içi borçlar üzerinden, en az yeni bakiye).`); } else mesaj('warning', 'Fatura sahibine ulaşan borç zinciri bulunamadı; zinciri elle kurun.');
    dolasimZincirCiz(); dolasimSimule();
  }
  $('#kdZincir').addEventListener('change', e => { const s = e.target.closest('.kd-halka'); if (s) { D.zincir[+s.dataset.i] = s.value; dolasimSimule(); } });
  $('#kdZincir').addEventListener('click', e => { const sil = e.target.closest('.kd-sil'); if (sil) { D.zincir.splice(+sil.dataset.i, 1); dolasimZincirCiz(); dolasimSimule(); return; } if (e.target.closest('#kdEkle')) { const aday = dSirketler().find(s => s !== D.b.sahip && !D.zincir.includes(s)) || dSirketler()[0]; D.zincir.push(D.zincir.length ? aday : (D.r.faturaSahibi !== D.b.sahip ? D.r.faturaSahibi : aday)); dolasimZincirCiz(); dolasimSimule(); } });
  $('#kdSonAdim').addEventListener('change', () => { dolasimZincirCiz(); dolasimSimule(); }); $('#kdTedarikci').addEventListener('change', () => { dolasimZincirCiz(); dolasimSimule(); });
  $('#kdOner').addEventListener('click', dolasimOner); $('#kdSimule').addEventListener('click', dolasimSimule);
  function dolasimSiradaki() { if (D.kuyruk && D.kuyruk.length) { const r = satirBul(D.r.docEntry) || D.r; const sonraki = D.kuyruk[0]; const taze = (r.belgeler || []).find(x => x.cekDocEntry === sonraki.cekDocEntry) || sonraki; const kuyruk = D.kuyruk.slice(1); setTimeout(() => { D.kuyruk = kuyruk; dolasimAc(r, taze); }, 300); return true; } return false; }
  $('#kdKaydet').addEventListener('click', async () => {
    const d = await post('/Rapor79/DolasimKaydet', { CekDocEntry: D.b.cekDocEntry, Zincir: D.zincir, SonAdim: $('#kdSonAdim').value, Tedarikci: $('#kdTedarikci').value, PlanDocEntry: D.r.docEntry }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } mesaj('success', '<i class="fas fa-route me-1"></i>' + esc(d.message)); if (d.row) satirYerlestir(d.row); D.b = d.belge; ciz(); if (acik && acik.docEntry === D.r.docEntry && d.row) drawerAc(d.row);
    if (dolasimSiradaki()) return; dolasimBaslikCiz(); dolasimZincirCiz(); dolasimPlanCiz();
  });
  $('#kdAdimlar').addEventListener('click', async e => {
    const b = e.target.closest('.kd-gerceklestir'); if (!b) return; if (!confirm(`${b.dataset.sira}. adım gerçekleştirilsin mi? (ciro / ibraz kaydı atılır, grup içi bakiye güncellenir, muhasebe fişleri atılır, Takip Paneli'ne düşer)`)) return;
    const d = await post('/Rapor79/DolasimAdim', { CekDocEntry: D.b.cekDocEntry, Sira: +b.dataset.sira }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } mesaj('success', esc(d.message)); D.b = d.belge; await yukle(true); const r = satirBul(D.r.docEntry); if (r) D.r = r; D.zincir = kalanZincir(D.b); dolasimBaslikCiz(); dolasimPlanCiz(); dolasimZincirCiz(); dolasimSimule();
    if (D.b.gerceklesen >= D.b.adimToplam) { mesaj('success', esc(d.message) + ' Dolaşım tamamlandı; satır ÖDENDİ.'); setTimeout(() => modalKapat($('#knDolasimModal')), 1200); }
  });
  $('#kdIptal').addEventListener('click', async () => { if (!confirm('Dolaşım planı iptal edilsin mi?')) return; const d = await post('/Rapor79/DolasimIptal', { CekDocEntry: D.b.cekDocEntry }).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (!d.success) { mesaj('danger', d.message); return; } mesaj('info', esc(d.message)); if (d.row) satirYerlestir(d.row); D.b = d.belge; D.zincir = []; ciz(); dolasimBaslikCiz(); dolasimZincirCiz(); dolasimPlanCiz(); dolasimSimule(); });
  document.addEventListener('click', e => { const b = e.target.closest('.kn-dolasim-ac'); if (!b) return; e.stopPropagation(); const r = satirdan(b) || acik; const belge = r && (r.belgeler || []).find(x => x.cekDocEntry === +b.dataset.cek); if (r && belge) { D.kuyruk = []; dolasimAc(r, belge); } });

  // ---------- ÇEK / SENET İBRAZI: ekrandan çeki ver (tedarikçiye ciro) ya da bankaya tahsile ver → belge çıkar, satır ÖDENDİ, fişler
  let IB = { r: null };
  function ibrazAc(r) {
    IB = { r }; const b = (r.belgeler || []).filter(x => !belgeCikmis(x) || (x.adimToplam > 0 && x.gerceklesen < x.adimToplam)); const senet = r.yontem === 'Senet';
    $('#kiBaslik').textContent = senet ? 'Senedi Ver / İbraz Et' : 'Çeki Ver / İbraz Et'; $('#kiKurum').textContent = r.kurum;
    $('#kiBilgi').innerHTML = [['Ödenecek Cari', r.kurum], ['Fatura Sahibi · Ödeyen', `${r.faturaSahibi} · ${r.odeyenSirket}`], ['Belge', `${r.belgeTipi || ''} ${r.faturaNo || ''}`], ['Tutar', `${para(r.tutarTL)} ₺${r.pb !== 'TRY' ? ' (' + para(r.tutar) + ' ' + r.pb + (r.kurBilgi ? ' · ' + r.kurBilgi.ozet : '') + ')' : ''}`], ['Son Ödeme · Planlanan', `${tarih(r.sonOdeme)} · ${tarih(r.planlanan)}`], ['Kur Farkı Faturası', r.kurBilgi ? (r.kurBilgi.kurFarki || '—') : 'TL ödeme (yok)']].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    $('#kiBelgeler').innerHTML = b.length ? b.map(x => `<div class="satir"><b>${esc(x.no)}</b><span>${esc(x.asilBorclu)}</span><span>vade ${tarih(x.vade)}</span><span class="fw-bold">${para(x.tutar)} ₺</span>${akibetRozet(x.akibet)}${x.dolasim ? `<span class="kn-dolasim">${esc(x.dolasim)} (${x.gerceklesen}/${x.adimToplam}) · kalan adımlar gerçekleştirilir</span>` : `<span class="text-muted">${esc(x.sahip || r.odeyenSirket)} portföyünden çıkar</span>`}</div>`).join('') : '<div class="na-bos">Çıkacak belge yok.</div>';
    const bankaya = /\(Tahsil\)/.test(r.kurum || ''); $('#kiTur').value = bankaya ? 'Bankaya Tahsile Ver' : 'Tedarikçiye Ciro';
    $('#kiBanka').innerHTML = (V.tahsilBankalari || ['Ziraat Bankası', 'Halk Bankası']).map(x => `<option>${esc(x)}</option>`).join(''); $('#kiYer').value = (r.kurum || '').replace(/ \(Tahsil\)$/, ''); $('#kiTarih').value = bugun(); $('#kiTeslimAlan').value = r.alanlar?.teslimAlan || ''; $('#kiBelgeNo').value = ''; $('#kiNot').value = '';
    IB.sim = null; $('#kiSimOnay').checked = false; kiTurGoster(); modalAc($('#knIbrazModal')); kiSimule();
  }
  function kiTurGoster() { const bankaya = $('#kiTur').value === 'Bankaya Tahsile Ver'; $('#kiBankaKap').style.display = bankaya ? '' : 'none'; $('#kiYerKap').style.display = bankaya ? 'none' : ''; $('#kiTeslimKap').style.display = bankaya ? 'none' : ''; $('#kiBelgeNo').placeholder = bankaya ? 'ibraz bordro no (boşsa otomatik İBZ-…)' : 'ciro bordro no (boşsa otomatik CRO-…)'; }
  // muhasebe simülasyonu: çıkış adımları, fişler (kaydedilmez), grup içi bakiye etkisi; grup içi işlemde onay kutusu işaretlenmeden kayıt atılmaz
  const kiOnayDurumu = () => { const s = IB.sim; const b = $('#kiGonder'); if (!s) { b.disabled = true; return; } b.disabled = s.grupIci && !$('#kiSimOnay').checked; b.innerHTML = `<i class="fas fa-check me-1"></i>${s.grupIci ? 'Onayla, Çıkışı İşle ve Kaydet' : 'Çıkışı İşle ve Kaydet'}`; };
  async function kiSimule() {
    const r = IB.r; if (!r) return; const bankaya = $('#kiTur').value === 'Bankaya Tahsile Ver'; $('#kiSimNot').textContent = 'hesaplanıyor…'; IB.sim = null; kiOnayDurumu();
    const d = await post('/Rapor79/CekIbrazSimule', { DbName: r.dbName, DocEntry: r.docEntry, Tur: $('#kiTur').value, IbrazYeri: bankaya ? $('#kiBanka').value : $('#kiYer').value }).catch(() => null);
    if (!d || !d.success) { $('#kiSimNot').textContent = 'simülasyon alınamadı'; return; } const s = d.sim; IB.sim = s;
    $('#kiSimNot').textContent = s.ozet;
    $('#kiSimAdimlar').innerHTML = (s.adimlar || []).map(a => `<div class="satir"><span class="text-muted">${a.sira}.</span><b>${esc(a.belge)}</b><span>${esc(a.eden)} → ${esc(a.alan)}</span><span class="kn-oran">${esc(a.tur)}</span><span class="fw-bold">${para(a.tutar)} ₺</span>${a.tur === 'Grup İçi Ciro' ? `<span class="text-muted">mevcut borç ${para(a.mevcutBorc)} · kapanan ${para(a.kapanan)}${a.yeniBorc > 0 ? ` · <b class="text-danger">yeni bakiye ${para(a.yeniBorc)}</b>` : ''}</span>` : ''}<span class="ms-auto text-muted">${esc(a.belgeNo)}</span></div>`).join('') || '<div class="na-bos">Çıkış adımı yok.</div>';
    $('#kiSimFisler').innerHTML = (s.fisler || []).map(f => `<table class="na-tablo kn-kalem mb-1"><thead><tr><th colspan="2">${esc(f.sirket)} · ${esc(f.kaynak)} <small class="text-muted">${esc(f.aciklama)}</small></th><th class="s">Borç (₺)</th><th class="s">Alacak (₺)</th></tr></thead><tbody>${(f.satirlar || []).map(x => `<tr><td style="width:48px">${esc(x.hesapKodu)}</td><td class="sol">${esc(x.hesapAdi)}<small>${esc(x.aciklama)}</small></td><td class="para">${x.borc ? para(x.borc) : ''}</td><td class="para">${x.alacak ? para(x.alacak) : ''}</td></tr>`).join('')}</tbody></table>`).join('') || '<div class="na-mini">Yeni fiş yok (fişler daha önce atılmış).</div>';
    $('#kiSimBakiye').innerHTML = [(s.grupIciBelge ? `<div><i class="fas fa-file-signature me-1"></i><b>${esc(s.grupIciBelge.tur)} belgesi ${esc(s.grupIciBelge.no)}</b>: ${esc(s.grupIciBelge.faturaSahibi)} adına ${esc(s.grupIciBelge.odeyen)} ödüyor · ${esc(s.grupIciBelge.sekil)} · ${esc(s.grupIciBelge.etki)}</div>` : ''), ...(s.bakiyeler || []).map(b => `<div><i class="fas fa-scale-balanced me-1"></i>${esc(b.borclu)} → ${esc(b.alacakli)} grup içi borcu: ${para(b.mevcut)} → <b>${para(b.sonra)}</b> ₺ (${b.degisim > 0 ? '+' : ''}${para(b.degisim)})</div>`), ...(s.uyarilar || []).map(u => `<div class="text-danger"><i class="fas fa-triangle-exclamation me-1"></i>${esc(u)}</div>`)].join('');
    $('#kiSimOnayKap').style.display = s.grupIci ? '' : 'none'; kiOnayDurumu();
  }
  $('#kiTur').addEventListener('change', () => { kiTurGoster(); kiSimule(); }); $('#kiYer').addEventListener('change', kiSimule); $('#kiBanka').addEventListener('change', kiSimule); $('#kiSimule').addEventListener('click', kiSimule); $('#kiSimOnay').addEventListener('change', kiOnayDurumu);
  $('#kiGonder').addEventListener('click', async () => {
    const r = IB.r; if (!r || !IB.sim) return; const bankaya = $('#kiTur').value === 'Bankaya Tahsile Ver';
    if (IB.sim.grupIci && !$('#kiSimOnay').checked) { mesaj('warning', 'Grup içi işlem: simülasyondaki muhasebe kayıtlarını onaylayın.'); return; }
    if (!confirm(`${r.cekNolar} ${bankaya ? $('#kiBanka').value + ' bankasına tahsile verilsin' : ($('#kiYer').value || r.kurum) + ' tedarikçisine ciro edilsin'} mi? Belge çıkar, satır ÖDENDİ olur, simülasyondaki ${(IB.sim.fisler || []).length} fiş atılır.`)) return;
    const d = await post('/Rapor79/CekIbraz', { DbName: r.dbName, DocEntry: r.docEntry, Tur: $('#kiTur').value, IbrazYeri: bankaya ? $('#kiBanka').value : $('#kiYer').value, Tarih: $('#kiTarih').value, TeslimAlan: $('#kiTeslimAlan').value, BelgeNo: $('#kiBelgeNo').value, Not: $('#kiNot').value, SimOnay: true }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    modalKapat($('#knIbrazModal')); if (d.row) satirYerlestir(d.row); mesaj('success', '<i class="fas fa-money-check-dollar me-1"></i>' + esc(d.message) + ' <a href="/CekAkibeti" target="_blank" class="ms-1"><i class="fas fa-arrow-up-right-from-square"></i> Çek Akıbeti Raporu</a>'); await yukle(true); if (acik && acik.docEntry === r.docEntry) drawerAc(satirBul(r.docEntry) || d.row);
  });
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-ibraz-ac'); if (!b) return; e.stopPropagation(); const r = satirdan(b) || acik; if (r) ibrazAc(r); });

  // ---------- otomatik grup içi Mahsup / Temlik belgeleri (fatura sahibi ≠ ödeyen): Mahsup / Temlik sekmelerinde liste
  function cizGib(p) {
    const t = $('#knGib-' + p); if (!t) return; const tur = SEKME[p];
    const L = (V.grupIciBelgeler || []).filter(b => b.tur === tur && (!F.sirket || b.faturaSahibiDb === F.sirket || b.odeyenDb === F.sirket || b.kaynakDb === F.sirket));
    t.tBodies[0].innerHTML = L.length ? L.map(b => `<tr class="${b.durum === 'İptal' ? 'text-muted' : ''}"><td class="sol"><a href="${esc(b.link)}" target="_blank" class="fw-bold" title="Yazdırılabilir belge">${esc(b.no)}</a></td><td>${tarih(b.tarih)}</td><td class="sol fw-bold">${esc(b.faturaSahibi)}</td><td class="sol text-primary fw-bold">${esc(b.odeyen)}</td><td class="sol">${esc(b.kurum)}<small>${esc(b.belgeTipi)} ${esc(b.belgeNo)}</small></td><td>${yontemRozet(b.yontem)}</td><td class="para">${para(b.tutarTL)}${b.pb !== 'TRY' ? `<small>${para(b.tutar)} ${esc(b.pb)}</small>` : ''}</td><td class="sol" style="white-space:normal">${esc(b.sekil)}</td><td>${modulRozet(b.durum)}${b.muhasebeTarihi ? `<small>${esc(b.muhasebeTarihi.slice(0, 10))}</small>` : ''}</td><td class="sol" style="white-space:normal">${b.faturaSahibiFisNo ? `<b>${esc(b.faturaSahibiFisNo)}</b>` : '—'}<small style="white-space:normal">${esc(b.odeyenFisNo || (b.durum === 'Muhasebeleşti' ? 'dolaşım fişleri' : 'ödeme gerçekleşince'))}</small></td><td><a class="kn-belge-link kn-gib-satir" data-doc="${b.docEntry}" data-db="${esc(b.kaynakDb)}" title="Kaynak ödeme satırını aç">#${b.docEntry}</a></td><td><a class="btn btn-light btn-sm py-0 px-1 border" href="${esc(b.link)}" target="_blank" style="font-size:.62rem" title="Yazdır"><i class="fas fa-print"></i></a></td></tr>`).join('') : `<tr><td colspan="12" class="na-bos text-center">Fatura sahibi ≠ ödeyen şirket olan ${esc(tur.toLocaleLowerCase('tr-TR'))} belgesi yok. Bir ödemede fatura sahibi ile ödeyen şirket farklı seçilince otomatik oluşur.</td></tr>`;
    const aktif = L.filter(b => b.durum !== 'İptal'); t.tFoot.innerHTML = aktif.length ? `<tr class="toplam"><td colspan="6" class="text-end">${aktif.length} belge · muhasebeleşen ${aktif.filter(b => b.durum === 'Muhasebeleşti').length} · hazır ${aktif.filter(b => b.durum === 'Hazırlandı').length}</td><td class="para">${para(aktif.reduce((s, b) => s + b.tutarTL, 0))}</td><td colspan="5"></td></tr>` : '';
    $('#knGibNot-' + p).textContent = '';
  }
  kok.addEventListener('click', e => { const a = e.target.closest('.kn-gib-satir'); if (!a) return; e.stopPropagation(); satirGetirAc(+a.dataset.doc, a.dataset.db); });

  // ---------- grup içi kayıt bağlama (Mahsup / Temlik / Yansıtma)
  let grup = { r: null, tur: 'Mahsup', liste: [] };
  function kgCiz() {
    const q = ($('#kgAra').value || '').toLocaleLowerCase('tr-TR'); const l = grup.liste.filter(x => !q || [x.no, x.baslik, x.taraflar, x.durum].join(' ').toLocaleLowerCase('tr-TR').includes(q));
    $('#kgGovde').innerHTML = l.length ? l.map(x => `<tr><td><input type="radio" name="kgSec" class="form-check-input" value="${esc(x.no)}" ${x.bagli ? 'disabled title="başka satıra bağlı"' : ''}></td><td class="fw-bold">${esc(x.no)}${x.bagli ? '<br><small class="text-muted">bağlı</small>' : ''}</td><td>${tarih(x.tarih)}</td><td>${esc(x.baslik)}</td><td>${esc(x.taraflar)}</td><td class="text-end fw-bold">${para(x.tutar)} ${esc(pbAd(x.pb))}</td><td>${modulRozet(x.durum)}</td><td>${esc(x.muhasebe || '')}</td><td><a href="${esc(x.link)}" target="_blank" title="Modülde aç"><i class="fas fa-arrow-up-right-from-square"></i></a></td></tr>`).join('') : `<tr><td colspan="9" class="text-center text-muted py-3">Modülde ${esc(grup.tur.toLocaleLowerCase('tr-TR'))} kaydı yok.</td></tr>`;
  }
  async function grupSecAc(r, tur) {
    grup = { r, tur: tur || r.yontem, liste: [] };
    $('#kgBaslik').textContent = grup.tur + ' Kaydı Seç'; $('#kgKurum').textContent = r.kurum; $('#kgBilgi').textContent = `${grup.tur} İşlemleri modülündeki kayıtlar (şirketin taraf olduğu, aktif) · seçilen kayıt satıra bağlanır`;
    $('#kgAra').value = ''; $('#kgGovde').innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3"><i class="fas fa-spinner fa-spin"></i> Kayıtlar getiriliyor…</td></tr>';
    modalAc($('#knGrupModal'));
    try { const d = await getJ(`/Rapor79/GrupIciKayitlar?dbName=${encodeURIComponent(r.dbName)}&tur=${encodeURIComponent(grup.tur)}`); grup.liste = d.data || []; kgCiz(); } catch (x) { $('#kgGovde').innerHTML = '<tr><td colspan="9" class="text-center text-danger py-3">Ağ hatası.</td></tr>'; }
  }
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-grup-sec'); if (!b) return; e.stopPropagation(); const r = satirdan(b) || acik; if (r) grupSecAc(r, b.dataset.tur); });
  $('#kgAra').addEventListener('input', kgCiz);
  $('#kgOnayla').addEventListener('click', async () => {
    const s = $('input[name="kgSec"]:checked'); if (!s) { mesaj('warning', 'Bir kayıt seçin.'); return; }
    const d = await post('/Rapor79/GrupIciBagla', { DbName: grup.r.dbName, DocEntry: grup.r.docEntry, Tur: grup.tur, No: s.value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message || 'Bağlanamadı.'); return; }
    modalKapat($('#knGrupModal')); satirYerlestir(d.row); mesaj('success', '<i class="fas fa-link me-1"></i>' + esc(d.message)); ciz(); if (acik && acik.docEntry === d.row.docEntry) drawerAc(d.row);
  });
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.kn-grup-kaldir'); if (!b) return; e.stopPropagation(); const r = satirdan(b) || acik; if (!r || !confirm('Kayıt bağı kaldırılsın mı? (Modül kaydı silinmez.)')) return;
    const d = await post('/Rapor79/GrupIciKaldir', { DbName: r.dbName, DocEntry: r.docEntry }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } satirYerlestir(d.row); mesaj('info', esc(d.message)); ciz(); if (acik && acik.docEntry === r.docEntry) drawerAc(d.row);
  });

  // ---------- ÇEKMECE: BELGE SEÇ (carinin açık faturaları / siparişleri ya da cari bakiyesi) → alanları doldurur
  let KS = { tip: '', veri: null, secili: null };
  function ksCiz() {
    const v = KS.veri; if (!v) return; const tip = KS.tip;
    $$('#ksTipler button').forEach(b => b.classList.toggle('aktif', b.dataset.tip === tip)); $$('#ksTipler button').forEach(b => b.classList.toggle('btn-primary', b.dataset.tip === tip)); $$('#ksTipler button').forEach(b => b.classList.toggle('btn-outline-primary', b.dataset.tip !== tip));
    if (tip === 'Cari Bakiye') {
      $('#ksBelgeKap').style.display = 'none'; $('#ksCari').style.display = ''; const c = v.cariBakiye;
      $('#ksCariBilgi').innerHTML = [['Cari Bakiye (TL)', para(c.toplam)], ['Vadesi Geçmiş', para(c.vadesiGecmis)], ['Nakit Akışta Planlanan', para(c.planlanan)], ['Planlanabilir Kalan', para(c.kalan)], ['Açık Kalem', String(c.acikKalemler.length)]].map(([l, x]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(x)}</div></div>`).join('');
      $('#ksCariGovde').innerHTML = c.acikKalemler.map(k => `<tr><td>${esc(k.belge)}</td><td>${esc(k.no)}</td><td>${tarih(k.tarih)}</td><td class="${k.vade < bugun() ? 'text-danger fw-bold' : ''}">${tarih(k.vade)}</td><td class="text-end">${para(k.tutar)}</td><td class="text-end">${para(k.odenen)}</td><td class="text-end fw-bold">${para(k.kalan)}</td></tr>`).join('');
      KS.secili = { tip: 'Cari Bakiye', no: '', tutar: c.kalan > 0 ? c.kalan : c.toplam, pb: 'TRY', kur: 1, tarih: bugun(), sonOdeme: bugun(), faturaTutari: null, bagliDocEntry: null }; $('#ksOzet').textContent = `Seçilince satır tutarı planlanabilir kalan bakiye (${para(KS.secili.tutar)} ₺) olur; belge tipi Cari Bakiye.`; return;
    }
    $('#ksBelgeKap').style.display = ''; $('#ksCari').style.display = 'none';
    const l = v.belgeler.filter(b => !tip || b.tip === tip);
    $('#ksGovde').innerHTML = l.length ? l.map((b, i) => `<tr><td><input type="radio" name="ksSec" class="form-check-input" value="${i}" ${b.kalan <= 0 ? 'disabled title="belge tamamen planlanmış"' : ''}></td><td>${esc(b.tip)}</td><td class="fw-bold">${esc(b.no)}</td><td>${tarih(b.tarih)}</td><td class="${b.sonOdeme < bugun() ? 'text-danger fw-bold' : ''}">${tarih(b.sonOdeme)}</td><td class="text-end fw-bold">${para(b.tutar)} ${esc(pbAd(b.pb))}</td><td class="text-end">${para(b.planlanan)}</td><td class="text-end fw-bold ${b.kalan <= 0 ? 'text-muted' : 'text-success'}">${para(b.kalan)}</td><td>${esc(b.satinalmaci)}</td><td>${esc(b.urun)}</td><td><small>${esc(b.kaynak)}${b.satirSayisi ? ' · ' + b.satirSayisi + ' satır' : ''}</small></td></tr>`).join('') : '<tr><td colspan="11" class="text-center text-muted py-3">Belge yok.</td></tr>';
    KS.liste = l; KS.secili = null; $('#ksOzet').textContent = `${l.length} belge · seçilince belge no, tarih, son ödeme, belge tutarı ve kalan tutar satıra yazılır`;
  }
  async function belgeSecDrawer() {
    const kurum = ($('#knD_kurum') || {}).value?.trim(); if (!kurum) { mesaj('warning', 'Önce ödenecek cariyi girin.'); return; }
    $('#ksKurum').textContent = kurum; KS = { tip: ($('#knD_belgeTipi') || {}).value === 'Cari Bakiye' ? 'Cari Bakiye' : (['Fatura', 'Sipariş'].includes(($('#knD_belgeTipi') || {}).value) ? $('#knD_belgeTipi').value : ''), veri: null, secili: null };
    $('#ksGovde').innerHTML = '<tr><td colspan="11" class="text-center text-muted py-3"><i class="fas fa-spinner fa-spin"></i> Belgeler getiriliyor…</td></tr>'; modalAc($('#knBelgeSecModal'));
    const db = acik && acik.dbName ? acik.dbName : (($('#knD_db') || {}).value || F.sirket || V.sirketler[0].dbName);
    const d = await getJ(`/Rapor79/CariBelgeler?dbName=${encodeURIComponent(db)}&kurum=${encodeURIComponent(kurum)}&tip=`).catch(() => null); if (!d || !d.success) { $('#ksGovde').innerHTML = '<tr><td colspan="11" class="text-center text-danger py-3">Ağ hatası.</td></tr>'; return; }
    KS.veri = d.veri; ksCiz();
  }
  $('#ksTipler').addEventListener('click', e => { const b = e.target.closest('button'); if (!b) return; KS.tip = b.dataset.tip; ksCiz(); });
  $('#ksGovde').addEventListener('change', e => { const r = e.target.closest('input[name="ksSec"]'); if (!r) return; const b = KS.liste[+r.value]; KS.secili = { tip: b.tip, no: b.no, tutar: b.kalan, pb: b.pb, kur: b.kur, tarih: b.tarih, sonOdeme: b.sonOdeme, faturaTutari: b.tutar, bagliDocEntry: b.bagliDocEntry, urun: b.urun, satinalmaci: b.satinalmaci }; $('#ksOzet').textContent = `${b.tip} ${b.no}: belge ${para(b.tutar)} ${pbAd(b.pb)} · kalan ${para(b.kalan)} satıra yazılır (${Math.round(b.kalan / b.tutar * 1000) / 10}%)`; });
  $('#ksOnayla').addEventListener('click', () => {
    const s = KS.secili; if (!s) { mesaj('warning', 'Bir belge seçin.'); return; }
    const set = (id, v) => { const el = $('#' + id); if (el && v != null) el.value = v; };
    set('knD_belgeTipi', s.tip); set('knD_faturaNo', s.no); set('knD_belgeTarihi', s.tarih); set('knD_sonOdeme', s.sonOdeme); set('knD_faturaTutari', s.faturaTutari ?? ''); set('knD_pb', s.pb); set('knD_kur', s.kur); set('knD_tutar', s.tutar); if (s.urun && !$('#knD_urun').value) set('knD_urun', s.urun); if (s.satinalmaci) set('knD_satinalmaci', s.satinalmaci);
    $('#knD_tutar').dispatchEvent(new Event('input')); $('#knD_belgeTipi').dispatchEvent(new Event('change')); modalKapat($('#knBelgeSecModal')); $('#knDMesaj').textContent = `${s.tip}${s.no ? ' ' + s.no : ''} seçildi; Kaydet ile yazılır.`;
  });

  // ---------- çekmece: detay / düzenleme / yeni / işlem çubuğu
  const F_ALAN = (id, etiket, html, g) => `<div class="${g || ''}"><label class="form-label" for="${id}">${etiket}</label>${html}</div>`;
  const sel = (id, deger, secenekler, bosMu) => `<select class="form-select" id="${id}">${bosMu ? '<option value="">—</option>' : ''}${secenekler.map(o => `<option ${o === deger ? 'selected' : ''}>${esc(o)}</option>`).join('')}</select>`;
  const inp = (id, deger, tip) => `<input class="form-control" id="${id}" type="${tip || 'text'}" value="${esc(deger ?? '')}" ${tip === 'number' ? 'step="0.01"' : ''}>`;
  function aksiyonlar(r) {
    const b = r.belgeler || [], o = r.onay, odendi = r.durum === 'ÖDENDİ', btn = (cls, ikon, ad, title, renk) => `<button type="button" class="btn ${renk || 'btn-light border'} btn-sm ${cls}" title="${esc(title || ad)}"><i class="fas ${ikon} me-1"></i>${ad}</button>`;
    const l = [];
    l.push(btn('kn-belge-detay', 'fa-file-invoice', 'Belge Detayı', 'Fatura / sipariş / cari bakiye detayı, ödeme planı, mükerrer kontrolü'));
    if (mukerrerVar(r)) l.push(btn('kn-belge-detay', 'fa-triangle-exclamation', 'Mükerrer Kontrolü', r.plan.uyarilar.map(u => u.aciklama).join(' · '), 'btn-outline-danger'));
    if (r.cikis) { if (o && o.durum === 'Onay Bekliyor') l.push(btn('kn-cikis-onay', 'fa-shield-halved', 'GMY Onayı / Reddi', 'Plansız çek çıkışı: onaylanırsa çıkış işlenir, reddedilirse belge portföyde kalır', 'btn-danger')); }
    else if (r.yontem === 'Çek' || r.yontem === 'Senet') {
      if (!odendi) l.push(btn('kn-belge-sec', 'fa-money-check-alt', b.length ? 'Belge Ekle (Portföy)' : 'Portföyden Belge Seç', 'Portföy görünümü: süzgeçler, başka şirket portföyü, planla ve dolaşımı planla', 'btn-primary'));
      if (ibrazUygun(r)) l.push(btn('kn-ibraz-ac', 'fa-money-check-dollar', r.yontem === 'Senet' ? 'Senedi Ver / İbraz Et' : 'Çeki Ver / İbraz Et', 'Belgeyi tedarikçiye ciro et ya da bankaya tahsile ver: Çek Akıbeti çıkış, satır ÖDENDİ, muhasebe fişleri', 'btn-success'));
      b.filter(x => !belgeCikmis(x) || x.adimToplam).forEach(x => l.push(`<button type="button" class="btn btn-light border btn-sm kn-dolasim-ac" data-cek="${x.cekDocEntry}" title="${esc(x.no)} dolaşım planı ve uygunluk simülasyonu"><i class="fas fa-route me-1"></i>Dolaşım · ${esc(x.no)}${x.adimToplam ? ' (' + x.gerceklesen + '/' + x.adimToplam + ')' : ''}</button>`));
      b.filter(x => x.akibet === 'Planlandı' && !x.gerceklesen).forEach(x => l.push(`<button type="button" class="btn btn-light border btn-sm kn-belge-kaldir" data-cek="${x.cekDocEntry}" title="Planı kaldır: belge portföye döner"><i class="fas fa-times me-1 text-danger"></i>Planı Kaldır · ${esc(x.no)}</button>`));
    }
    if (r.yontem === 'Nakit' && !odendi) { if (!o || o.durum !== 'Onaylandı') l.push(btn('kn-onay-ac', 'fa-user-check', 'Nakit Onayı / Reddi', 'Onaycılar: ' + (V.onaycilar?.nakit || []).join(' / '), 'btn-primary')); else l.push(btn('kn-makbuz-ac', 'fa-receipt', 'Makbuz Kes', 'Kasa tediye makbuzu: satır ödenir, muhasebe fişi atılır', 'btn-success')); }
    if (r.makbuz && r.makbuz.link) l.push(`<a class="btn btn-light border btn-sm" href="${esc(r.makbuz.link)}" target="_blank"><i class="fas fa-print me-1"></i>Makbuz ${esc(r.makbuz.no)}</a>`);
    if (r.grupIciBelge) l.push(`<a class="btn btn-light border btn-sm" href="${esc(r.grupIciBelge.link)}" target="_blank" title="${esc(r.grupIciBelge.baslik)} · ${esc(r.grupIciBelge.durum)}"><i class="fas fa-file-signature me-1"></i>${esc(r.grupIciBelge.tur)} Belgesi ${esc(r.grupIciBelge.no)}</a>`);
    if (['Mahsup', 'Temlik', 'Yansıtma'].includes(r.yontem)) l.push(`<button type="button" class="btn btn-primary btn-sm kn-grup-sec" data-tur="${esc(r.yontem)}"><i class="fas fa-link me-1"></i>${r.grupIci ? 'Modül Kaydını Değiştir' : 'Modül Kaydı Bağla'}</button>`);
    if (!odendi && !r.cikis) { const uygun = !(r.talimat && r.talimat.durum !== 'İptal' && r.talimat.durum !== 'Gerçekleşti') && !((r.yontem === 'Çek' || r.yontem === 'Senet') && !b.length) && !(r.yontem === 'Nakit' && (!o || o.durum !== 'Onaylandı')); const tAd = r.yontem === 'Havale / EFT' ? 'Banka Talimatı Oluştur' : r.yontem === 'Nakit' ? 'Kasa Talimatı Oluştur' : 'Talimat Oluştur'; l.push(`<button type="button" class="btn ${uygun ? 'btn-outline-primary' : 'btn-light border'} btn-sm" id="knDTalimat" ${uygun ? '' : 'disabled'} title="${uygun ? (r.yontem === 'Havale / EFT' ? 'Alıcı banka · IBAN · hesap adı ile banka ödeme talimatı hazırla' : 'Bu satırdan talimat hazırla') : 'Talimat için: açık talimatta olmamalı, çek satırında belge seçili ve nakit onaylı olmalı'}"><i class="fas fa-file-contract me-1"></i>${tAd}</button>`); }
    if (!odendi && !r.cikis) l.push(`<button type="button" class="btn btn-light border btn-sm" id="knDTaksit" title="Satırı oranlara böl (ör. %70 + %30)"><i class="fas fa-percent me-1"></i>Kısmi Ödeme / Taksit</button>`);
    if (r.talimat) l.push(talimatRozet(r.talimat, true));
    l.push(`<button type="button" class="btn btn-light btn-sm border" id="knDSil" title="Satırı sil"><i class="fas fa-trash me-1 text-danger"></i>Sil</button>`);
    return l.join('');
  }
  function drawerAc(r) {
    if (!r) return; acik = r; const yeni = !r.docEntry; const a = r.alanlar || {}, g = r.grupIci, av = r.avans, pl = r.plan || {}, sirketler = dSirketler(), kb = r.kurBilgi || {}, gb = r.grupIciBelge;
    const ekVarsayilan = r.yontem === 'Nakit' ? 'İmzalı Makbuz' : (r.yontem === 'Çek' || r.yontem === 'Senet') ? 'Ciro Bordrosu' : 'Dekont';
    $('#knDBaslik').innerHTML = yeni ? '<i class="fas fa-plus me-1"></i>Yeni Ödeme Kaydı' : `${esc(r.kurum)} ${yontemRozet(r.yontem)} ${durumRozet(r.durum)} ${r.onay ? onayRozet(r.onay) : ''} <span class="kn-pill">#${r.docEntry} · ${esc(r.sirket)}</span>`;
    $('#knDAksiyon').innerHTML = '';
    const dbSec = yeni ? F_ALAN('knD_db', 'Kayıt Şirketi (Liste)', `<select class="form-select" id="knD_db">${V.sirketler.map(s => `<option value="${esc(s.dbName)}" ${s.dbName === (F.sirket || V.sirketler[0].dbName) ? 'selected' : ''}>${esc(s.ad)}</option>`).join('')}</select>`) : '';
    $('#knDIc').innerHTML = `
      ${yeni ? yeniSekmeHtml() : `<div class="kn-aksiyon">${aksiyonlar(r)}</div>`}
      ${yeni && (r.yontem === 'Nakit') ? '<div class="na-uyari sari"><b>Nakit ödeme talebi:</b> kaydedilince onaya düşer; Alev Özavşar / Neslihan Uludağ / Özge İsen onaylamadan ödenmez.</div>' : ''}
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-pen me-1"></i>Ödeme Bilgileri</span><span class="not">${yeni ? 'kaydet: bellek içi · kayıt öncesi mükerrer kontrolü' : 'satır içi alanlar da tabloda düzenlenebilir'}</span></div><div class="na-kart-g">
        <div class="kn-form">
          ${dbSec}
          ${F_ALAN('knD_kurum', 'Ödenecek Cari (Tedarikçi) <span class="z">*</span>', `<input class="form-control" id="knD_kurum" list="knTedarikciler" value="${esc(r.kurum)}"><datalist id="knTedarikciler">${(V.tedarikciler || []).map(t => `<option value="${esc(t)}">`).join('')}</datalist><div class="na-mini" id="knDCariNot" style="min-height:14px"></div>`, 'g2')}
          ${F_ALAN('knD_faturaSahibi', 'Fatura Sahibi Şirket', sel('knD_faturaSahibi', r.faturaSahibi || sirketAdi(F.sirket || V.sirketler[0].dbName), sirketler))}
          ${F_ALAN('knD_odeyen', 'Ödeyen Şirket', sel('knD_odeyen', r.odeyenSirket || sirketAdi(F.sirket || V.sirketler[0].dbName), sirketler))}
          ${F_ALAN('knD_belgeTipi', 'Belge Tipi', sel('knD_belgeTipi', r.belgeTipi || 'Fatura', V.belgeTipleri || ['Fatura', 'Sipariş', 'Cari Bakiye', 'Avans', 'Mahsup', 'Temlik', 'Yansıtma']))}
          ${F_ALAN('knD_faturaNo', 'Fatura / Sipariş No', `<div class="input-group input-group-sm"><input class="form-control" id="knD_faturaNo" value="${esc(r.faturaNo)}" placeholder="belge no (cari bakiyede boş)"><button type="button" class="btn btn-outline-primary" id="knDBelgeSec" title="Carinin açık faturaları / siparişleri ya da cari bakiyesinden seç"><i class="fas fa-magnifying-glass"></i> Belge Seç</button></div>`, 'g2')}
          ${F_ALAN('knD_belgeTarihi', 'Belge Tarihi', inp('knD_belgeTarihi', r.belgeTarihi, 'date'))}
          ${F_ALAN('knD_sonOdeme', 'Son Ödeme Tarihi', inp('knD_sonOdeme', r.sonOdeme, 'date'))}
          ${F_ALAN('knD_plan', 'Planlanan Ödeme Tarihi', inp('knD_plan', r.planlanan, 'date'))}
          ${F_ALAN('knD_yontem', 'Ödeme Yöntemi', sel('knD_yontem', r.yontem || 'Havale / EFT', V.yontemler))}
          ${F_ALAN('knD_faturaTutari', 'Belge Tutarı (Belge PB)', inp('knD_faturaTutari', r.faturaTutari ?? '', 'number'))}
          ${F_ALAN('knD_tutar', 'Bu Ödeme Tutarı (Belge PB)', inp('knD_tutar', r.tutar ?? '', 'number'))}
          ${F_ALAN('knD_pb', 'Para Birimi', sel('knD_pb', r.pb || 'TRY', Object.keys(V.kurlar)))}
          ${F_ALAN('knD_kur', 'Kur', inp('knD_kur', r.kur ?? 1, 'number'))}
          ${F_ALAN('knD_kurKaynagi', 'Kur Kaynağı', sel('knD_kurKaynagi', kb.kaynak || ((r.yontem === 'Çek' || r.yontem === 'Senet') ? 'Çek Tahsil Günü Kuru' : 'TCMB Döviz Alış'), V.kurKaynaklari || ['TCMB Döviz Alış', 'TCMB Döviz Satış', 'TCMB Efektif Alış', 'Anlaşmalı Kur', 'Fatura Kuru', 'Çek Tahsil Günü Kuru']), 'kn-kur-alan')}
          ${F_ALAN('knD_kurTarihi', 'Kur Günü', inp('knD_kurTarihi', kb.tarih || r.planlanan || bugun(), 'date'), 'kn-kur-alan')}
          ${F_ALAN('knD_kurFarki', 'Kur Farkı Faturası (Bu Cariye)', sel('knD_kurFarki', kb.kurFarki || 'Belirsiz', V.kurFarkiSecenekleri || ['Kesilmeyecek', 'Kesilecek', 'Belirsiz']), 'kn-kur-alan')}
          ${F_ALAN('knD_kurNotu', 'Kur Anlaşması / Not', inp('knD_kurNotu', kb.not), 'kn-kur-alan')}
          <div class="g4 kn-kur-alan na-mini" id="knDKurBilgi" style="min-height:14px"></div>
          ${F_ALAN('knD_tutarTL', 'Tutar (₺)', inp('knD_tutarTL', r.tutarTL ?? '', 'number'))}
          ${F_ALAN('knD_oran', 'Ödeme Oranı (Belgeye Göre)', `<input class="form-control" id="knD_oran" readonly value="">`)}
          ${F_ALAN('knD_durum', 'Ödeme Durumu', sel('knD_durum', r.durum || 'Bekliyor', V.durumlar))}
          ${F_ALAN('knD_urun', 'Ürün / Hizmet', inp('knD_urun', r.urun), 'g2')}
          ${F_ALAN('knD_satinalmaci', 'Satınalma Temsilcisi', sel('knD_satinalmaci', r.satinalmaci, V.satinalmacilar, true))}
          ${F_ALAN('knD_talepEden', 'Talep Eden', inp('knD_talepEden', r.talepEden))}
          ${F_ALAN('knD_islemTipi', 'İşlem Tipi', sel('knD_islemTipi', r.islemTipi || 'X', ['X', 'R', 'Y']))}
          ${F_ALAN('knD_odenme', 'Ödenme Tarihi', inp('knD_odenme', r.odenme, 'date'))}
          ${F_ALAN('knD_odenen', 'Ödenen Tutar', inp('knD_odenen', r.odenen ?? '', 'number'))}
          ${F_ALAN('knD_mutabakat', 'Mutabakat Durumu', sel('knD_mutabakat', r.mutabakat, ['Mutabık', 'Mutabık Değil'], true))}
          ${F_ALAN('knD_muafiyet', 'Muafiyet', sel('knD_muafiyet', r.muafiyet, ['Vergi ödemesi', 'Kontör / Yakıt / Sertifika', 'Fatura (Elektrik/Su/Doğalgaz)', 'Diğer'], true))}
          ${F_ALAN('knD_kasa', 'Kasa (Nakit)', sel('knD_kasa', a.kasa, V.kasalar, true))}
          ${F_ALAN('knD_teslimAlan', 'Teslim Alan (Nakit)', inp('knD_teslimAlan', a.teslimAlan))}
          ${F_ALAN('knD_aliciBanka', 'Alıcı Banka (Havale)', sel('knD_aliciBanka', a.aliciBanka, V.aliciBankalar || [], true), 'kn-banka-alan')}
          ${F_ALAN('knD_aliciIban', 'Alıcı IBAN', inp('knD_aliciIban', a.aliciIban), 'kn-banka-alan g2')}
          ${F_ALAN('knD_aliciHesapAdi', 'Hesap Adı (Alıcı)', inp('knD_aliciHesapAdi', a.aliciHesapAdi), 'kn-banka-alan')}
          <div class="g4 kn-banka-alan d-flex align-items-center gap-2 flex-wrap"><button type="button" class="btn btn-light border btn-sm" id="knDBankaGetir" style="font-size:.66rem"><i class="fas fa-building-columns me-1"></i>Cari Karttan Banka Bilgisi Getir</button><span class="na-mini" id="knDBankaNot">alıcı banka · IBAN · hesap adı banka talimatına yazılır</span></div>
          ${F_ALAN('knD_grupIciTur', 'Grup İçi Belge (Fatura Sahibi ≠ Ödeyen)', sel('knD_grupIciTur', r.grupIciTur || 'Mahsup', ['Mahsup', 'Temlik']), 'kn-gib-alan')}
          <div class="g3 kn-gib-alan na-mini" style="grid-column:span 3;align-self:end">fatura sahibi ile ödeyen şirket farklı: kaydedilince otomatik mahsup / temlik belgesi açılır; ödeme gerçekleşince iki şirkette fiş atılır, grup içi bakiye güncellenir</div>
          ${F_ALAN('knD_not', 'Not', `<textarea class="form-control" id="knD_not" rows="2">${esc(r.notlar)}</textarea>`, 'g4')}
        </div>
        <div class="d-flex gap-2 mt-2 align-items-center"><button type="button" class="btn btn-primary btn-sm" id="knDKaydet" style="font-size:.7rem"><i class="fas fa-save me-1"></i>${yeni ? 'Kaydı Oluştur' : 'Kaydet'}</button><span class="na-mini" id="knDMesaj"></span></div>
      </div></div>
      ${yeni ? '' : `
      ${r.onay || r.cikis ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-user-check me-1"></i>${r.cikis ? 'Çek Çıkış Talebi · GMY Onayı' : 'Nakit Ödeme Onayı'}</span><span class="not">${r.cikis ? 'onaycı: ' + esc((V.onaycilar?.cekCikis || []).join(' / ')) : 'onaycılar: ' + esc((V.onaycilar?.nakit || []).join(' / '))}</span></div><div class="na-kart-g"><div class="na-ustbilgi">${[['Durum', r.onay ? r.onay.durum : '—'], ['Onaylayan', r.onay?.onaylayan || '—'], ['Tarih', r.onay?.tarih || '—'], ['Not', r.onay?.not || '—'], ...(r.cikis ? [['Talep Edilen Çıkış', `${r.cikis.islem}${r.cikis.ibrazYeri ? ' → ' + r.cikis.ibrazYeri : ''}`], ['Talep Eden', r.cikis.talepEden]] : [])].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('')}</div></div></div>` : ''}
      ${pl.faturaTutari || (pl.kardesler || []).length ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-percent me-1"></i>Belge Ödeme Planı</span><span class="not">belge tutarı sabit · satırlar oran / tutar olarak izlenir</span></div><div class="na-kart-g"><div class="na-ustbilgi">${[['Belge Tutarı', pl.faturaTutari ? para(pl.faturaTutari) + ' ' + pbAd(r.pb) : '—'], ['Bu Satır', para(r.tutar) + ' ' + pbAd(r.pb) + (pl.oran != null ? ' · %' + pl.oran : '')], ['Planlanan Toplam', para(pl.planlanan) + ' ' + pbAd(r.pb)], ['Ödenen', para(pl.odenen) + ' ' + pbAd(r.pb)], ['Kalan (Belgeden)', pl.kalan != null ? para(pl.kalan) + ' ' + pbAd(r.pb) : '—'], ['Taksit', pl.taksit || 'tek ödeme']].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('')}</div>${(pl.kardesler || []).length ? `<div class="kn-liste mt-2">${pl.kardesler.map(k => `<div class="satir"><span class="text-muted">#${k.docEntry}</span><span>${esc(k.sirket)}</span><span>${tarih(k.planlanan)}</span><b>${para(k.tutar)} ${esc(pbAd(k.pb))}</b><span class="kn-oran">%${k.oran ?? '—'}</span>${yontemRozet(k.yontem)}${durumRozet(k.durum)}<span class="text-muted">${tarih(k.odenme)}</span></div>`).join('')}</div>` : ''}${(pl.uyarilar || []).map(u => `<div class="na-uyari kirmizi"><b>${esc(u.tip)}</b> · ${esc(u.aciklama)}</div>`).join('')}</div></div>` : ''}
      ${av ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-hand-holding-dollar me-1"></i>Nakit Avans · ${esc(av.no)} (Modülle Canlı)${av.sahsi ? ' <span class="kn-onay gmy" title="Şahsi avans: gizlilik gereği personel adı ve konu gösterilmez">Şahsi · Gizli</span>' : ''}</span><span class="not">${av.sahsi ? 'ayrıntı yalnız İK / Nakit Avans modülünde' : `<a href="${esc(av.link)}" target="_blank">modülde aç</a>`}</span></div><div class="na-kart-g"><div class="na-ustbilgi">${(av.sahsi
        ? [['Avans Türü', av.tur || 'Şahsi Avans'], ['Kullanan', 'Personel (gizli)'], ['Departman', av.departman || '—'], ['Ödeyen Şirket', av.odeyenSirket], ['Talep Durumu', av.talepDurumu], ['Ödeme Durumu', av.odemeDurumu], ['Onaylanan / Bekleyen', `${para(av.onaylanan)} / ${para(av.odemeBekleyen)} ${av.pb}`], ['Kapatma', av.kapatmaDurumu]]
        : [['Avans Türü', av.tur || 'Nakit Avans'], ['Kullanan', `${av.kullanan} · ${av.kullananSirket}`], ['Departman', av.departman || '—'], ['Konu', av.konu || '—'], ['Harcama Kategorisi', av.kategori || '—'], ['Ödeyen Şirket', av.odeyenSirket + (av.grupIci ? ' (grup içi · masraf ' + av.kullananSirket + ')' : '')], ['Talep Durumu', av.talepDurumu], ['Ödeme Durumu', av.odemeDurumu], ['Onaylanan / Bekleyen', `${para(av.onaylanan)} / ${para(av.odemeBekleyen)} ${av.pb}`], ['Kullanım Tarihi', tarih(av.kullanimTarihi)], ['Kapatma', av.kapatmaDurumu], ['Öncelik', av.oncelik || '—']]).map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('')}</div><div class="na-mini">${av.sahsi ? 'Şahsi (maaş) avansı şirket masrafı değildir; gizlilik için ad ve konu gösterilmez, makbuzda teslim alan yazılır. ' : 'Şirket masrafı avansı: ayrıntılar görünür. '}Makbuz kesilince avans modülünde "teslim edildi" olayı işlenir ve Bekleyen Ödemeler listesinden düşer.</div></div></div>` : ''}
      ${gb || r.grupIciGerekli ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-file-signature me-1"></i>Grup İçi Belge · Fatura Sahibi ≠ Ödeyen Şirket${gb ? ' · ' + esc(gb.no) : ''}</span><span class="not">${gb ? modulRozet(gb.durum) : 'kaydedilince otomatik oluşur'}</span></div><div class="na-kart-g"><div class="d-flex gap-2 align-items-end flex-wrap" style="font-size:.72rem"><div><label class="form-label mb-0">Belge Türü</label><select class="form-select form-select-sm" id="knDGibTur" style="font-size:.72rem" ${gb && gb.durum === 'Muhasebeleşti' ? 'disabled title="Muhasebeleşmiş belge değiştirilemez"' : ''}>${['Mahsup', 'Temlik'].map(t => `<option ${t === (gb ? gb.tur : (r.grupIciTur || 'Mahsup')) ? 'selected' : ''}>${t}</option>`).join('')}</select></div>${gb ? `<a class="btn btn-light border btn-sm" href="${esc(gb.link)}" target="_blank" style="font-size:.66rem"><i class="fas fa-print me-1"></i>${esc(gb.baslik)} Yazdır</a>` : ''}<span class="na-mini">Mahsup: karşılıklı grup içi bakiyeden düşülür (132 / 332) · Temlik: tedarikçi borcu ödeyen şirkete devredilir (332)</span></div>${gb ? `<div class="na-ustbilgi mt-2">${[['Fatura Sahibi → Ödeyen', `${gb.faturaSahibi} → ${gb.odeyen}`], ['İşlem Şekli', gb.sekil], ['Tutar', `${para(gb.tutarTL)} ₺`], ['Durum', gb.durum + (gb.muhasebeTarihi ? ' · ' + gb.muhasebeTarihi : '')], ['Fatura Sahibi Fişi', gb.faturaSahibiFisNo || '— (ödeme gerçekleşince)'], ['Ödeyen Fişi', gb.odeyenFisNo || (gb.durum === 'Muhasebeleşti' ? 'dolaşım fişleri' : '— (ödeme gerçekleşince)')]].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('')}</div>` : ''}</div></div>` : ''}
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-sliders me-1"></i>Yönteme Özel Bilgiler · ${esc(r.yontem)}</span><span class="not d-flex gap-1 align-items-center">${(r.yontem === 'Çek' || r.yontem === 'Senet') && r.durum !== 'ÖDENDİ' && !r.cikis ? `<button type="button" class="btn btn-primary btn-sm py-0 px-2 kn-belge-sec" style="font-size:.62rem" title="Portföyden belge ekle (başka grup şirketinin portföyü de seçilebilir)"><i class="fas fa-plus me-1"></i>Belge Ekle</button>` : 'tablodaki sekmede satır içi düzenlenir'}</span></div><div class="na-kart-g">
        ${r.yontem === 'Çek' || r.yontem === 'Senet' ? `<div class="kn-liste">${(r.belgeler || []).length ? r.belgeler.map(x => `<div class="satir" style="flex-wrap:wrap"><b>${esc(x.no)}</b><span>${esc(x.banka)}</span><span>${esc(x.asilBorclu)}</span><span>vade ${tarih(x.vade)}</span><span class="fw-bold">${para(x.tutar)} ₺</span>${akibetRozet(x.akibet)} ${kontrolRozet(x.kontrol, x)}<span class="ms-auto d-flex gap-1">${!belgeCikmis(x) || x.adimToplam ? `<button type="button" class="btn btn-light border btn-sm py-0 px-1 kn-dolasim-ac" data-cek="${x.cekDocEntry}" style="font-size:.6rem" title="Hangi şirketten nasıl çıkacak: grup içi zincir, son adım (tedarikçiye ciro / bankaya ibraz), uygunluk simülasyonu"><i class="fas fa-route"></i> Çıkış Planı</button>` : ''}${x.akibet === 'Planlandı' && !x.gerceklesen && r.durum !== 'ÖDENDİ' ? `<button type="button" class="btn btn-light border btn-sm py-0 px-1 kn-belge-degistir" data-cek="${x.cekDocEntry}" style="font-size:.6rem" title="Bu belgeyi kaldırıp portföyden başka belge seç"><i class="fas fa-right-left"></i> Değiştir</button><button type="button" class="btn btn-light border btn-sm py-0 px-1 kn-belge-kaldir" data-cek="${x.cekDocEntry}" style="font-size:.6rem" title="Planı kaldır: belge portföye döner"><i class="fas fa-times text-danger"></i> Kaldır</button>` : ''}</span><div class="w-100 kn-dolasim" title="Belgenin çıkış yolu"><i class="fas fa-route"></i> Çıkış: ${esc(x.dolasim ? x.dolasim + ' (' + x.gerceklesen + '/' + x.adimToplam + ' adım)' : (x.sahip || r.odeyenSirket) + ' → ' + (x.ibrazYeri || r.kurum) + (x.akibet === 'Planlandı' || x.akibet === 'Portföyde' ? ' · tedarikçiye ciro (planlı)' : ' · ' + x.akibet))}${x.sahip && x.sahip !== r.faturaSahibi ? ' · belge ' + esc(x.sahip) + ' portföyünde, fatura sahibi ' + esc(r.faturaSahibi) : ''}</div></div>`).join('') : '<div class="na-bos">Belge seçilmedi · "Belge Ekle" ile portföyden seçin (başka grup şirketinin portföyü de olabilir); çıkış yolu "Çıkış Planı" ile belirlenir</div>'}</div>`
        : g ? `<div class="na-ustbilgi"><div><div class="l">${esc(g.tur)} No</div><div class="v"><a href="${esc(g.link)}" target="_blank">${esc(g.no)} <i class="fas fa-arrow-up-right-from-square"></i></a></div></div><div><div class="l">Tür</div><div class="v">${esc(g.baslik || '')}</div></div><div><div class="l">Taraflar</div><div class="v">${esc(g.taraflar || '')}</div></div><div><div class="l">Tutar</div><div class="v">${para(g.tutar)} ${esc(g.pb || '')}</div></div><div><div class="l">Durum</div><div class="v">${modulRozet(g.durum)}</div></div><div><div class="l">Muhasebe · Kontrol</div><div class="v">${esc(g.muhasebe || '—')} ${g.kontrol ? '· ' + esc(g.kontrol) : ''}</div></div><div style="grid-column:1/-1"><div class="l">Kayıt</div><div class="v" style="font-weight:500">${esc(g.ek || '')}</div></div></div>`
        : `<div class="na-ustbilgi">${Object.entries({ 'Banka Hesabımız': a.bankaHesabi, 'Alıcı Banka': a.aliciBanka, 'Alıcı IBAN': a.aliciIban, 'Hesap Adı': a.aliciHesapAdi, 'Dekont No': a.dekontNo, 'Kasa': a.kasa, 'Makbuz No': a.makbuzNo || r.makbuz?.no, 'Teslim Alan': a.teslimAlan, 'Kart': a.kart, 'Taksit': a.taksit, 'Ekstre Dönemi': a.ekstreDonemi, 'Kur': r.kurBilgi ? r.kurBilgi.ozet + (r.kurBilgi.kurFarki ? ' · kur farkı faturası ' + r.kurBilgi.kurFarki : '') : '' }).filter(([k, v]) => v).map(([k, v]) => `<div><div class="l">${esc(k)}</div><div class="v">${esc(v)}</div></div>`).join('') || '<div class="na-bos">Yönteme özel alan girilmemiş.</div>'}</div>`}
      </div></div>
      ${r.yevmiye ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-book me-1"></i>Muhasebe Fişi · ${esc(r.yevmiye.no)}</span><span class="not">${esc(r.yevmiye.tarih)} · ${esc(r.yevmiye.kaynak)} · ${esc(r.yevmiye.kullanici)}</span></div><div class="na-kart-g p-0"><table class="na-tablo kn-kalem"><thead><tr><th>Hesap</th><th>Hesap Adı · Açıklama</th><th class="s">Borç (₺)</th><th class="s">Alacak (₺)</th></tr></thead><tbody>${r.yevmiye.satirlar.map(s => `<tr><td>${esc(s.hesapKodu)}</td><td class="sol">${esc(s.hesapAdi)}<small>${esc(s.aciklama)}</small></td><td class="para">${s.borc ? para(s.borc) : ''}</td><td class="para">${s.alacak ? para(s.alacak) : ''}</td></tr>`).join('')}</tbody><tfoot><tr class="toplam"><td colspan="2" class="text-end">TOPLAM</td><td class="para">${para(r.yevmiye.toplamBorc)}</td><td class="para">${para(r.yevmiye.toplamAlacak)}</td></tr></tfoot></table></div></div>` : (r.durum === 'ÖDENDİ' ? '' : '<div class="na-mini rt-aciklama">Muhasebe fişi ödeme gerçekleşince (makbuz, talimat ya da satır ÖDENDİ) otomatik atılır.</div>')}
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-paperclip me-1"></i>Ekler · Dekont / İmzalı Makbuz / İmzalı Talimat / Ciro Bordrosu (${(r.dekontlar || []).length})</span><span class="not">imzalı belgeler bu rapor sayfasına yüklenir, buradan açılır</span></div><div class="na-kart-g">
        <div class="kn-liste">${(r.dekontlar || []).map(d => `<div class="satir"><span class="kn-ek-tur ${/İmzalı/.test(d.tur || '') ? 'imzali' : ''}">${esc(d.tur || 'Dekont')}</span>${d.dosyaVar ? `<a href="${esc(d.yol)}" target="_blank" class="fw-bold" title="Dosyayı aç"><i class="fas fa-file-arrow-down me-1"></i>${esc(d.ad)}</a>` : `<b title="Ön yüz örneği: dosya içeriği yok (tohum kaydı)">${esc(d.ad)}</b>`}<span class="text-muted">${esc(d.aciklama || '')}</span><span class="ms-auto text-muted" style="font-size:.64rem">${esc(d.tarih)} · ${esc(d.yukleyen)}${d.boyut ? ' · ' + Math.max(1, Math.round(d.boyut / 1024)) + ' KB' : ''}</span></div>`).join('') || '<div class="na-bos">Ek yok.</div>'}</div>
        <div class="d-flex gap-2 mt-2 flex-wrap align-items-center"><select class="form-select form-select-sm" id="knDEkTur" style="font-size:.7rem;width:auto">${(V.ekTurleri || ['Dekont']).map(t => `<option ${t === ekVarsayilan ? 'selected' : ''}>${esc(t)}</option>`).join('')}</select><input type="file" class="form-control form-control-sm" id="knDEkDosya" style="font-size:.7rem;max-width:260px" accept=".pdf,.jpg,.jpeg,.png"><input class="form-control form-control-sm" id="knDEkAciklama" placeholder="açıklama" style="font-size:.7rem;max-width:200px"><button type="button" class="btn btn-primary btn-sm" id="knDEkYukle" style="font-size:.66rem;white-space:nowrap"><i class="fas fa-upload me-1"></i>Yükle</button></div>
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Kayıt Günlüğü</span></div><div class="na-kart-g"><ul class="na-zaman">${V.gunluk.filter(x => x.docEntry === r.docEntry && x.dbName === r.dbName).map(x => `<li class="${/Ödeme|Planlandı|Talimat|Avans|Onay|Makbuz|Fiş/.test(x.tip) ? 'fin' : /Sil|Red/.test(x.tip) ? 'wf' : 'sis'}"><span class="t">${esc(x.tarih)}</span><b>${esc(x.tip)}</b> · ${esc(x.aciklama)} <span class="k">${esc(x.kullanici)}</span></li>`).join('') || '<li>Kayıt yok.</li>'}</ul></div></div>`}`;
    $('#knDrawer').classList.add('acik'); $('#knPerde').classList.add('acik'); $('#knDrawer').scrollTop = 0;
    const hesapla = () => { const t = +$('#knD_tutar').value || 0, k = +$('#knD_kur').value || 1, f = +$('#knD_faturaTutari').value || 0; $('#knD_tutarTL').value = (Math.round(t * k * 100) / 100).toFixed(2); $('#knD_oran').value = f > 0 ? '%' + (Math.round(t / f * 1000) / 10).toLocaleString('tr-TR') + (t > f ? ' · belge tutarını aşıyor!' : '') : 'belge tutarı girilmedi'; };
    // kur: dövizde kaynak TCMB türüyse o günün kuru /Rapor79/Kur ile otomatik (kur alanı kilitli); anlaşmalı / fatura kurunda elle; çekte tahsil (vade) günü
    const kurAlanlariGoster = () => { const tl = $('#knD_pb').value === 'TRY'; $$('#knDIc .kn-kur-alan').forEach(x => x.style.display = tl ? 'none' : ''); if (tl) $('#knD_kur').readOnly = false; };
    const kurGetir = async () => {
      const pb = $('#knD_pb').value; if (pb === 'TRY') { $('#knD_kur').value = 1; hesapla(); return; }
      const kay = $('#knD_kurKaynagi').value, t = $('#knD_kurTarihi').value || bugun(), tcmb = /TCMB|Çek Tahsil/.test(kay);
      const d = await getJ(`/Rapor79/Kur?pb=${encodeURIComponent(pb)}&tarih=${t}`).catch(() => null); if (!d || !d.success || !$('#knD_kur')) return;
      const k = d.kur, v = kay === 'TCMB Döviz Satış' ? k.satis : kay === 'TCMB Efektif Alış' ? k.efektifAlis : k.alis;
      if (tcmb) $('#knD_kur').value = v; $('#knD_kur').readOnly = tcmb; hesapla();
      $('#knDKurBilgi').innerHTML = `<i class="fas fa-landmark me-1"></i>TCMB ${tarih(k.tarih)}: alış <b>${k.alis}</b> · satış <b>${k.satis}</b> · efektif alış ${k.efektifAlis}${k.tahmini ? ' · <b class="text-danger">tahmini (ileri tarih; gün gelince kesinleşir)</b>' : ''}${tcmb ? ' · kur otomatik yazıldı' : ' · anlaşmalı / fatura kuru: kuru elle yazın, anlaşmayı nota yazın'}${kay === 'Çek Tahsil Günü Kuru' ? ' · çekin tahsil (vade) günü kuru baz; fark için bu cariye kur farkı faturası' : ''}`;
    };
    const pbKur = () => { kurAlanlariGoster(); if ($('#knD_pb').value === 'TRY') { $('#knD_kur').value = 1; hesapla(); } else { if (!kb.tarih) $('#knD_kurTarihi').value = $('#knD_plan').value || bugun(); kurGetir(); } };
    $('#knD_pb').addEventListener('change', pbKur); ['knD_tutar', 'knD_kur', 'knD_faturaTutari'].forEach(id => $('#' + id).addEventListener('input', hesapla)); hesapla(); kurAlanlariGoster(); if ($('#knD_pb').value !== 'TRY') kurGetir();
    $('#knD_kurKaynagi').addEventListener('change', () => { if ($('#knD_kurKaynagi').value === 'Çek Tahsil Günü Kuru' && (r.belgeler || []).length) $('#knD_kurTarihi').value = r.belgeler.map(x => x.vade).sort()[0]; kurGetir(); });
    $('#knD_kurTarihi').addEventListener('change', kurGetir);
    $('#knD_plan').addEventListener('change', () => { if ($('#knD_pb').value !== 'TRY' && $('#knD_kurKaynagi').value !== 'Çek Tahsil Günü Kuru' && $('#knD_durum').value !== 'ÖDENDİ') { $('#knD_kurTarihi').value = $('#knD_plan').value; kurGetir(); } });
    // banka bilgisi (havale) ve grup içi belge (fatura sahibi ≠ ödeyen) alanları yönteme / şirketlere göre görünür
    const bankaGoster = () => { const h = $('#knD_yontem').value === 'Havale / EFT'; $$('#knDIc .kn-banka-alan').forEach(x => x.style.display = h ? '' : 'none'); };
    const gibGoster = () => { const farkli = $('#knD_faturaSahibi').value !== $('#knD_odeyen').value && !r.avansNo && !['Mahsup', 'Temlik', 'Yansıtma'].includes($('#knD_yontem').value); $$('#knDIc .kn-gib-alan').forEach(x => x.style.display = farkli ? '' : 'none'); };
    bankaGoster(); gibGoster(); ['knD_faturaSahibi', 'knD_odeyen'].forEach(id => $('#' + id).addEventListener('change', gibGoster));
    $('#knD_belgeTipi').addEventListener('change', () => { const cb = $('#knD_belgeTipi').value === 'Cari Bakiye'; $('#knD_faturaNo').disabled = cb; if (cb) $('#knD_faturaNo').value = ''; }); $('#knD_belgeTipi').dispatchEvent(new Event('change'));
    $('#knD_yontem').addEventListener('change', () => { if (yeni) { drawerAc({ ...acik, yontem: $('#knD_yontem').value, kurum: $('#knD_kurum').value, tutar: $('#knD_tutar').value, notlar: $('#knD_not').value }); return; } bankaGoster(); gibGoster(); if ($('#knD_pb').value !== 'TRY' && /Çek|Senet/.test($('#knD_yontem').value)) { $('#knD_kurKaynagi').value = 'Çek Tahsil Günü Kuru'; kurGetir(); } });
    // cari girilince: vade şartı → son ödeme tarihi (yeni kayıtta), banka bilgisi (cari kart / son ödeme); "Cari Karttan Getir" düğmesi banka alanlarını doldurur
    const cariBilgiGetir = async (uygula) => {
      const kurum = $('#knD_kurum').value.trim(); if (!kurum) return null; const db = yeni ? (($('#knD_db') || {}).value || F.sirket || V.sirketler[0].dbName) : r.dbName;
      const d = await getJ(`/Rapor79/CariBilgi?dbName=${encodeURIComponent(db)}&kurum=${encodeURIComponent(kurum)}`).catch(() => null); if (!d || !d.success || !$('#knDCariNot')) return null; const c = d.cari;
      $('#knDCariNot').innerHTML = `<i class="fas fa-circle-info me-1"></i>${esc(c.vadeSarti)} → son ödeme ${tarih(c.sonOdeme)} · ${esc(c.banka)} ${esc(c.iban)} (${esc(c.bankaKaynagi)})${c.acikSatir ? ' · nakit akışta ' + c.acikSatir + ' açık satır' : ''}`;
      if (uygula && yeni) { if (!$('#knD_sonOdeme').value) { $('#knD_sonOdeme').value = c.sonOdeme; if (!$('#knD_plan').value || $('#knD_plan').value === bugun()) { $('#knD_plan').value = c.sonOdeme; $('#knD_plan').dispatchEvent(new Event('change')); } } if (!$('#knD_aliciIban').value) { $('#knD_aliciBanka').value = c.banka; $('#knD_aliciIban').value = c.iban; $('#knD_aliciHesapAdi').value = c.hesapAdi; } }
      return c;
    };
    $('#knD_kurum').addEventListener('change', () => cariBilgiGetir(true)); $('#knD_kurum').addEventListener('cari-not', () => cariBilgiGetir(false)); if (r.kurum) cariBilgiGetir(false);
    $('#knDBankaGetir').addEventListener('click', async () => { const c = await cariBilgiGetir(false); if (!c) { mesaj('warning', 'Önce ödenecek cariyi girin.'); return; } $('#knD_aliciBanka').value = c.banka; $('#knD_aliciIban').value = c.iban; $('#knD_aliciHesapAdi').value = c.hesapAdi; $('#knDBankaNot').textContent = `${c.banka} · ${c.iban} · ${c.hesapAdi} (${c.bankaKaynagi}); Kaydet ile yazılır`; });
    const gt = $('#knDGibTur'); if (gt) gt.addEventListener('change', async () => { const d = await post('/Rapor79/GrupIciTur', { DbName: r.dbName, DocEntry: r.docEntry, Tur: gt.value }).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (!d.success) { mesaj('danger', esc(d.message)); return; } if (d.belgeler) V.grupIciBelgeler = d.belgeler; satirYerlestir(d.row); mesaj('success', '<i class="fas fa-file-signature me-1"></i>' + esc(d.message)); drawerAc(d.row); ciz(); });
    $('#knDBelgeSec').addEventListener('click', belgeSecDrawer);
    $('#knDKaydet').addEventListener('click', () => drawerKaydet(false));
    if (yeni) { yeniBagla(); if (Y.g !== 'form') yeniCiz(); else $('#knDrawer').classList.remove('genis'); }
    if (!yeni) { $('#knDSil').addEventListener('click', drawerSil); $('#knDEkYukle').addEventListener('click', ekYukle); const t = $('#knDTalimat'); if (t) t.addEventListener('click', () => talimatOlustur([r.docEntry])); const tk = $('#knDTaksit'); if (tk) tk.addEventListener('click', () => taksitAc(acik)); }
  }
  async function drawerKaydet(zorla) {
    const r = acik, yeni = !r.docEntry; const v = id => ($('#' + id) || {}).value ?? '';
    const gov = { DocEntry: r.docEntry || 0, U_Kurum: v('knD_kurum').trim(), U_FaturaSahibi: v('knD_faturaSahibi'), U_OdeyenSirket: v('knD_odeyen'), U_OdemeYontemi: v('knD_yontem'), U_Durum: v('knD_durum'), U_PlanlananTarih: v('knD_plan') || null, U_SonOdemeTarihi: v('knD_sonOdeme') || null, BelgeTarihi: v('knD_belgeTarihi') || null, U_BagliBelgeTipi: v('knD_belgeTipi'), U_FaturaSiparisNo: v('knD_belgeTipi') === 'Cari Bakiye' ? '' : v('knD_faturaNo'), U_FaturaTutari: +v('knD_faturaTutari') || null, U_UrunHizmet: v('knD_urun'), U_Satinalmaci: v('knD_satinalmaci'), U_IslemTipi: v('knD_islemTipi'), U_Tutar: +v('knD_tutar') || 0, U_ParaBirimi: v('knD_pb'), U_Kur: +v('knD_kur') || 1, U_TutarTL: +v('knD_tutarTL') || 0, U_OdenmeTarihi: v('knD_odenme') || null, U_OdenenTutar: +v('knD_odenen') || 0, U_OdenenTutarPB: v('knD_pb'), U_MutabakatDurumu: v('knD_mutabakat'), MuafiyetSebebi: v('knD_muafiyet'), U_Notlar: v('knD_not'), U_KayitTipi: v('knD_belgeTipi') === 'Cari Bakiye' ? 'Bakiye' : v('knD_belgeTipi'), U_Kasa: v('knD_kasa'), U_TeslimAlan: v('knD_teslimAlan'), U_TalepEden: v('knD_talepEden'), MukerrerKontrolEdildi: zorla ? 1 : 0,
      U_KurKaynagi: v('knD_pb') === 'TRY' ? '' : v('knD_kurKaynagi'), U_KurTarihi: v('knD_pb') === 'TRY' ? null : (v('knD_kurTarihi') || null), U_KurNotu: v('knD_kurNotu'), U_KurFarkiFaturasi: v('knD_pb') === 'TRY' ? '' : v('knD_kurFarki'), U_GrupIciTur: v('knD_faturaSahibi') !== v('knD_odeyen') ? v('knD_grupIciTur') : '',
      U_AliciBanka: v('knD_aliciBanka'), U_AliciIban: v('knD_aliciIban'), U_AliciHesapAdi: v('knD_aliciHesapAdi') };
    if (!gov.U_Kurum) { $('#knDMesaj').textContent = 'Ödenecek cari zorunlu.'; return; }
    const db = yeni ? v('knD_db') : r.dbName;
    const d = await post(`/Rapor79/${yeni ? 'SatirEkle' : 'SatirKaydet'}?dbName=${encodeURIComponent(db)}`, gov).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success && d.mukerrer) { mesaj('warning', '<i class="fas fa-triangle-exclamation me-1"></i>' + esc(d.message)); if (confirm(d.message + '\n\nYine de kaydedilsin mi?')) return drawerKaydet(true); $('#knDMesaj').textContent = 'Mükerrer uyarısı: kayıt yapılmadı.'; return; }
    if (!d.success) { $('#knDMesaj').textContent = d.message || 'Kaydedilemedi.'; return; }
    if (yeni) { if ((!F.sirket || F.sirket === db) && (!d.row.planlanan || (d.row.planlanan >= F.bas && d.row.planlanan <= F.bit))) V.odemeler.push(d.row); mesaj(d.row.yontem === 'Nakit' ? 'warning' : 'success', esc(d.message) + (d.row.grupIciBelge ? ` <b>${esc(d.row.grupIciBelge.no)}</b> (${esc(d.row.grupIciBelge.baslik)}) otomatik açıldı.` : '')); drawerKapat(); ciz(); if (d.row.yontem) sekme(YKEY(d.row.yontem)); }
    else { satirYerlestir(d.row); ciz(); drawerAc(d.row); $('#knDMesaj').textContent = 'Kaydedildi.' + (d.row.grupIciBelge ? ` ${d.row.grupIciBelge.no} (${d.row.grupIciBelge.tur}) güncel.` : ''); }
    if (d.row.grupIciBelge || d.row.grupIciGerekli) yukle(true);   // Mahsup / Temlik sekmelerindeki belge listesi tazelensin
  }
  async function drawerSil() {
    const r = acik; if (!confirm(`#${r.docEntry} ${r.kurum} kaydı silinsin mi? Bağlı çek / senet planları kaldırılır.`)) return;
    const d = await post('/Rapor79/SilJson', { DbName: r.dbName, DocEntry: r.docEntry }).catch(() => ({ success: false }));
    if (!d.success) { mesaj('danger', 'Silinemedi.'); return; } satirKaldir(r.docEntry); drawerKapat(); mesaj('info', 'Kayıt silindi.'); ciz();
  }
  // ek yükleme: dosya seçildiyse çok parçalı gönderim (bellek içinde tutulur, Ek ile açılır); dosyasızsa yalnız kayıt
  async function ekYukle() {
    const r = acik, f = ($('#knDEkDosya').files || [])[0], tur = $('#knDEkTur').value, aciklama = $('#knDEkAciklama').value; let d;
    if (f) { const fd = new FormData(); fd.append('dosya', f); fd.append('dbName', r.dbName); fd.append('docEntry', r.docEntry); fd.append('tur', tur); fd.append('aciklama', aciklama); d = await fetch('/Rapor79/EkYukle', { method: 'POST', headers: { 'RequestVerificationToken': csrf }, body: fd }).then(x => x.json()).catch(() => ({ success: false, message: 'Ağ hatası.' })); }
    else d = await post('/Rapor79/DekontEkle', { DbName: r.dbName, DocEntry: r.docEntry, Ad: '', Tur: tur, Aciklama: aciklama }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; } satirYerlestir(d.row); mesaj('success', '<i class="fas fa-paperclip me-1"></i>' + esc(d.message)); drawerAc(d.row); ciz();
  }
  function drawerKapat() { $('#knDrawer').classList.remove('acik'); $('#knDrawer').classList.remove('genis'); $('#knPerde').classList.remove('acik'); acik = null; }
  $('#knDKapat').addEventListener('click', drawerKapat); $('#knPerde').addEventListener('click', drawerKapat);
  // ---------- YENİ ÖDEME: elle giriş · ödenmemiş faturalar · cari bakiyeleri (Cari Mutabakat ekstresinden); cari adı → Cari Ekstre Raporu (/Rapor20?bpCode=)
  let Y = { g: 'form', db: '', veri: null, ara: '', temsilci: '', liste: [] };
  const yeniSekmeHtml = () => `<div class="kn-yeni"><div class="btn-group btn-group-sm mb-2" id="knYeniSekme"><button type="button" class="btn btn-primary" data-g="form"><i class="fas fa-pen me-1"></i>Elle Giriş</button><button type="button" class="btn btn-outline-primary" data-g="fatura"><i class="fas fa-file-invoice me-1"></i>Ödenmemiş Faturalar</button><button type="button" class="btn btn-outline-primary" data-g="cari"><i class="fas fa-building me-1"></i>Cari Bakiyeleri</button><button type="button" class="btn btn-outline-primary" data-g="talep"><i class="fas fa-inbox me-1"></i>Departman Talepleri</button></div>
    <div id="knYeniListe" style="display:none"><div class="d-flex gap-2 align-items-end mb-2 flex-wrap" style="font-size:.7rem"><div><label class="form-label mb-0" style="font-size:.58rem;font-weight:700;color:#64748b;text-transform:uppercase">Şirket</label><select id="knYeniSirket" class="form-select form-select-sm" style="min-width:150px">${V.sirketler.map(s => `<option value="${esc(s.dbName)}">${esc(s.ad)}</option>`).join('')}</select></div><div><label class="form-label mb-0" style="font-size:.58rem;font-weight:700;color:#64748b;text-transform:uppercase">Temsilci / Talep Eden</label><select id="knYeniTemsilci" class="form-select form-select-sm" style="min-width:160px"><option value="">Tümü</option></select></div><div><label class="form-label mb-0" style="font-size:.58rem;font-weight:700;color:#64748b;text-transform:uppercase">Ara</label><input id="knYeniAra" class="form-control form-control-sm" style="width:220px;font-size:.7rem;height:26px" placeholder="firma / belge no / açıklama…"></div><span class="text-muted ms-auto" id="knYeniNot"></span></div><div class="table-responsive kn-liste-kap"><table class="na-tablo kn-tablo" id="knYeniTablo" data-dinamik="kapali"><thead></thead><tbody></tbody><tfoot></tfoot></table></div></div></div>`;
  async function yeniYukle() {
    const db = $('#knYeniSirket').value; if (Y.veri && Y.db === db) return Y.veri;
    $('#knYeniNot').textContent = 'yükleniyor…'; const d = await getJ(`/Rapor79/CariAcikFaturalar?dbName=${encodeURIComponent(db)}`).catch(() => null);
    if (!d || !d.success) { $('#knYeniNot').textContent = 'veri alınamadı'; return null; } Y.veri = d.veri; Y.db = db;
    const ts = $('#knYeniTemsilci'); ts.innerHTML = '<option value="">Tümü</option>' + (d.veri.temsilciler || []).map(x => `<option ${x === Y.temsilci ? 'selected' : ''}>${esc(x)}</option>`).join(''); if (!(d.veri.temsilciler || []).includes(Y.temsilci)) Y.temsilci = '';
    return Y.veri;
  }
  async function yeniCiz() {
    $$('#knYeniSekme button').forEach(b => { b.classList.toggle('btn-primary', b.dataset.g === Y.g); b.classList.toggle('btn-outline-primary', b.dataset.g !== Y.g); });
    const liste = $('#knYeniListe'); if (!liste) return; liste.style.display = Y.g === 'form' ? 'none' : ''; $$('#knDIc .kn-form').forEach(f => f.closest('.na-kart').style.display = Y.g === 'form' ? '' : 'none'); $('#knDrawer').classList.toggle('genis', Y.g !== 'form');
    if (Y.g === 'form') return; const v = await yeniYukle(); if (!v) return; const q = Y.ara.trim().toLocaleLowerCase('tr-TR'), tf = Y.temsilci, t = $('#knYeniTablo');
    const ekstre = c => `<a href="${esc(c.ekstre)}" target="_blank" class="kn-belge-link" title="Cari Ekstre Raporu'nda aç · temsilci: ${esc((c.temsilciler || []).join(', '))}">${esc(c.ad)}</a> <a href="${esc(c.ekstreYazdir)}" target="_blank" class="text-muted" title="Yazdırılabilir ekstre" style="font-size:.6rem"><i class="fas fa-print"></i></a><small title="Carinin kayıtlı olduğu grup şirketi">${esc(c.sirket || '')}${c.kendi ? '' : ' · grup tedarikçisi'}</small>`;
    const temsilciUyar = c => !tf || (c.temsilciler || []).includes(tf);
    if (Y.g === 'talep') {
      const L = (v.talepler || []).filter(x => (!tf || x.talepEden === tf) && (!q || [x.kurum, x.belgeNo, x.aciklama, x.departman, x.talepEden, x.no, x.belgeTipi, x.konu].join(' ').toLocaleLowerCase('tr-TR').includes(q)));
      const mutRozet = m => rozet(m || 'Mutabakat Yok', m === 'Mutabık' ? 'ok' : m === 'Mutabık Değil' ? 'hata' : m === 'Bekliyor' ? 'bekle' : m === 'Gerekmez' ? 'notr' : 'bilgi');
      t.tHead.innerHTML = '<tr><th>Talep No · Tarih</th><th>Şirket</th><th>Departman · Talep Eden</th><th>Ödenecek Kurum</th><th>Belge Tipi · No</th><th>Açıklama</th><th>Son Ödeme</th><th class="s">Talep Tutarı</th><th class="s">Tutar (₺)</th><th title="Ödeme öncesi cari mutabakat durumu">Mutabakat</th><th>Yöntem</th><th>Öncelik</th><th>Durum</th><th></th></tr>';
      t.tBodies[0].innerHTML = L.length ? L.map((x, i) => `<tr class="tik ${x.durum === 'Onaylandı' ? '' : 'kn-bekleyen'}" data-i="${i}"><td class="sol"><b>${esc(x.no)}</b>${x.talepLink ? ` <a href="${esc(x.talepLink)}" target="_blank" title="Ödeme Talebi modülünde aç (Talep › Yeni Talep › Ödeme Talebi)" style="font-size:.6rem"><i class="fas fa-arrow-up-right-from-square"></i></a>` : ''}<small>${tarih(x.tarih)}${x.konu ? ' · ' + esc(x.konu) : ''}</small></td><td class="sol"><b>${esc(x.sirket)}</b><small>${esc(x.kaynak || '')}</small></td><td class="sol"><b>${esc(x.departman)}</b><small>${esc(x.talepEden)}</small></td><td class="sol fw-bold">${esc(x.kurum)}</td><td class="sol">${rozet(x.belgeTipi, x.belgeTipi === 'Vergi' || x.belgeTipi === 'SGK' || x.belgeTipi === 'Gümrük' ? 'notr' : x.belgeTipi === 'Sipariş' ? 'bilgi' : 'bekle')}<small>${esc(x.belgeNo)}</small></td><td class="sol" style="white-space:normal;max-width:220px">${esc(x.aciklama)}</td><td class="${x.gecikme > 0 ? 'gec' : ''}">${tarih(x.sonOdeme)}${x.gecikme > 0 ? `<small class="text-danger">${x.gecikme} gün geçti</small>` : `<small>${-x.gecikme} gün kaldı</small>`}</td><td class="para fw-bold">${para(x.tutar)} ${esc(pbAd(x.pb))}</td><td class="para">${para(x.tutarTL)}</td><td>${mutRozet(x.mutabakat)}${x.mutabakatTarihi ? `<small>${tarih(x.mutabakatTarihi)}</small>` : ''}</td><td>${yontemRozet(x.yontem)}</td><td>${rozet(x.oncelik, x.oncelik === 'Çok Acil' ? 'hata' : x.oncelik === 'Acil' ? 'bekle' : 'notr')}</td><td>${rozet(x.durum, x.durum === 'Onaylandı' ? 'ok' : 'bekle')}${x.onaylayan ? `<small>${esc(x.onaylayan)}${x.onayTarihi ? ' · ' + tarih(x.onayTarihi) : ''}</small>` : x.durum !== 'Onaylandı' ? '<small>onay akışında</small>' : ''}</td><td style="white-space:nowrap">${x.durum === 'Onaylandı' ? `<button type="button" class="btn btn-success btn-sm py-0 px-2 yeni-talep-ekle" data-no="${esc(x.no)}" style="font-size:.62rem" title="Onaylı talebi ${esc(x.sirket)} nakit akışına satır olarak ekle"><i class="fas fa-plus"></i> Nakit Akışa Ekle</button> <button type="button" class="btn btn-light border btn-sm py-0 px-2 yeni-sec" data-i="${i}" style="font-size:.62rem" title="Forma al, düzenleyip kaydet"><i class="fas fa-pen"></i></button>` : `<span class="rt-durum rt-bekle" title="Onaylanmadan nakit akışa alınamaz">Onay Bekliyor</span>${x.talepLink ? ` <a class="btn btn-light border btn-sm py-0 px-2" href="${esc(x.talepLink)}" target="_blank" style="font-size:.62rem" title="Onay akışını Ödeme Talebi modülünde aç"><i class="fas fa-arrow-up-right-from-square"></i></a>` : ''}`}</td></tr>`).join('') : '<tr><td colspan="14" class="na-bos">Açık ödeme talebi yok.</td></tr>';
      t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="8" class="text-end">${L.length} talep · onaylı (eklenebilir) ${L.filter(x => x.durum === 'Onaylandı').length} · onay bekleyen ${L.filter(x => x.durum !== 'Onaylandı').length} · mutabık ${L.filter(x => x.mutabakat === 'Mutabık').length}</td><td class="para">${para(L.reduce((s, x) => s + x.tutarTL, 0))}</td><td colspan="5"></td></tr>` : '';
      Y.liste = L; $('#knYeniNot').textContent = `Tüm şirketler · ${(v.talepler || []).length} açık talep (onaylı ${v.talepOnayli || 0} · onay bekleyen ${v.talepBekleyen || 0}) · Ödeme Talebi modülünden gelenler onaylanınca "Nakit Akışa Ekle" ile satır olur · kalem: forma al`;
      return;
    }
    if (Y.g === 'fatura') {
      const L = v.cariler.filter(temsilciUyar).flatMap(c => c.faturalar.map(f => ({ c, f }))).filter(x => !q || (x.c.ad + ' ' + x.f.belgeNo + ' ' + (x.f.aciklama || '')).toLocaleLowerCase('tr-TR').includes(q)).sort((a, b) => a.f.vade.localeCompare(b.f.vade));
      t.tHead.innerHTML = '<tr><th>Cari (Ekstre)</th><th>Temsilci</th><th>Belge No</th><th>Belge Tarihi</th><th>Son Ödeme</th><th>Gecikme</th><th class="s">Belge Tutarı</th><th class="s">Kalan</th><th class="s">Kalan (₺)</th><th>Durum</th><th>Nakit Akış</th><th></th></tr>';
      t.tBodies[0].innerHTML = L.length ? L.map((x, i) => `<tr class="tik" data-i="${i}"><td class="sol">${ekstre(x.c)}</td><td class="sol"><small style="display:inline">${esc((x.c.temsilciler || []).join(', '))}</small></td><td class="fw-bold">${esc(x.f.belgeNo)}</td><td>${tarih(x.f.tarih)}</td><td class="${x.f.gecikme > 0 ? 'gec' : ''}">${tarih(x.f.vade)}</td><td>${x.f.gecikme > 0 ? `<span class="rt-durum rt-hata">${x.f.gecikme} gün geçti</span>` : `<span class="rt-durum rt-bilgi">${-x.f.gecikme} gün kaldı</span>`}</td><td class="para">${para(x.f.tutar)} ${esc(pbAd(x.f.pb))}</td><td class="para">${para(x.f.kalan)} ${esc(pbAd(x.f.pb))}</td><td class="para">${para(x.f.kalanTL)}</td><td>${rozet(x.f.kapama, x.f.kapama === 'Açık' ? 'bekle' : 'bilgi')}</td><td>${x.f.planlandi ? '<span class="kn-onay onayli" title="Bu belge için nakit akışta satır var">Planlandı</span>' : bos}</td><td><button type="button" class="btn btn-primary btn-sm py-0 px-2 yeni-sec" data-i="${i}" style="font-size:.62rem"><i class="fas fa-check"></i> Seç</button></td></tr>`).join('') : '<tr><td colspan="12" class="na-bos">Açık fatura yok.</td></tr>';
      t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="8" class="text-end">${L.length} fatura · vadesi geçmiş ${L.filter(x => x.f.gecikme > 0).length}</td><td class="para">${para(L.reduce((s, x) => s + x.f.kalanTL, 0))}</td><td colspan="3"></td></tr>` : '';
      Y.liste = L; $('#knYeniNot').textContent = `${sirketAdi(Y.db)} · ${v.kendiAdet || 0} kendi carisi + grup tedarikçileri (${v.cariler.length} cari) · ${v.adet} açık fatura · ${para(v.toplam)} ₺ (vadesi geçmiş ${para(v.gecmis)})${tf ? ' · temsilci ' + tf : ''}`;
    } else {
      const L = v.cariler.filter(temsilciUyar).filter(c => !q || (c.ad + ' ' + c.kod).toLocaleLowerCase('tr-TR').includes(q));
      t.tHead.innerHTML = '<tr><th>Cari (Ekstre)</th><th>Kod · Tip</th><th>Temsilci</th><th title="Cari kart vade şartı → seçilince son ödeme / planlanan tarih">Vade Şartı</th><th class="s">Açık Fatura</th><th class="s">Toplam (₺)</th><th class="s">Vadesi Geçmiş (₺)</th><th class="s">Gelecek (₺)</th><th>En Eski Vade</th><th>İlk Gelecek Vade</th><th>Döviz</th><th class="s">Nakit Akışta Planlanan</th><th></th></tr>';
      t.tBodies[0].innerHTML = L.length ? L.map((c, i) => `<tr class="tik" data-i="${i}"><td class="sol">${ekstre(c)}</td><td class="sol">${esc(c.kod)}<small>${esc(c.kartTipi)}${c.sinif ? ' · ' + esc(c.sinif) : ''}</small></td><td class="sol"><small style="display:inline">${esc((c.temsilciler || []).join(', '))}</small></td><td class="sol" title="Seçilince son ödeme tarihi bugün + vade günü olur; çek talebinde bu vadeye yakın çek seçilir">${esc(c.vadeSarti || '')}<small>son ödeme ${tarih(c.sonOdemeOnerisi)}</small></td><td class="para">${c.adet}${c.gecmisAdet ? `<small class="text-danger">${c.gecmisAdet} gecikmiş</small>` : ''}</td><td class="para">${para(c.toplam)}</td><td class="para ${c.gecmis ? 'text-danger' : ''}">${para(c.gecmis)}${c.enUzunGecikme > 0 ? `<small>en uzun ${c.enUzunGecikme} gün</small>` : ''}</td><td class="para">${para(c.gelecek)}</td><td class="${c.enEskiVade ? 'gec' : ''}">${c.enEskiVade ? tarih(c.enEskiVade) : '—'}</td><td>${c.ilkVade ? tarih(c.ilkVade) : '—'}</td><td class="sol"><small>${c.dovizler.map(d => `${esc(d.pb)} ${kisa(d.acikTutar)} (${d.acikAdet})`).join(' · ') || '—'}</small></td><td class="para">${c.planlanan ? para(c.planlanan) : '—'}</td><td><button type="button" class="btn btn-primary btn-sm py-0 px-2 yeni-sec" data-i="${i}" style="font-size:.62rem" title="Cari bakiyesinden ödeme satırı aç (vade şartı ve banka bilgisi forma gelir)"><i class="fas fa-check"></i> Seç</button></td></tr>`).join('') : '<tr><td colspan="13" class="na-bos">Cari yok.</td></tr>';
      t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="4" class="text-end">${L.length} cari</td><td class="para">${L.reduce((s, c) => s + c.adet, 0)}</td><td class="para">${para(L.reduce((s, c) => s + c.toplam, 0))}</td><td class="para">${para(L.reduce((s, c) => s + c.gecmis, 0))}</td><td class="para">${para(L.reduce((s, c) => s + c.gelecek, 0))}</td><td colspan="5"></td></tr>` : '';
      Y.liste = L; $('#knYeniNot').textContent = `${sirketAdi(Y.db)} · ${v.kendiAdet || 0} kendi carisi + grup tedarikçileri (${v.cariler.length} cari) · ${para(v.toplam)} ₺ açık · vadesi geçmiş ${para(v.gecmis)} · gelecek ${para(v.gelecek)}${tf ? ' · temsilci ' + tf : ''}`;
    }
  }
  function yeniSec(i) {
    const set = (id, v) => { const el = $('#' + id); if (el && v != null) el.value = v; };
    const x = Y.liste[i]; if (!x) return;
    const banka = c => { if (c && c.iban) { set('knD_aliciBanka', c.banka); set('knD_aliciIban', c.iban); set('knD_aliciHesapAdi', c.hesapAdi); } };
    if (Y.g === 'talep') { set('knD_kurum', x.kurum); set('knD_belgeTipi', x.nakitAkisBelgeTipi); set('knD_faturaNo', x.belgeNo); set('knD_belgeTarihi', x.tarih); set('knD_sonOdeme', x.sonOdeme); set('knD_plan', x.sonOdeme < bugun() ? bugun() : x.sonOdeme); set('knD_faturaTutari', x.tutar); set('knD_tutar', x.tutar); set('knD_pb', x.pb); set('knD_kur', V.kurlar[x.pb] ?? 1); set('knD_yontem', x.yontem); set('knD_urun', x.aciklama); set('knD_talepEden', x.talepEden); if (x.mutabakat === 'Mutabık' || x.mutabakat === 'Mutabık Değil') set('knD_mutabakat', x.mutabakat); set('knD_not', `Departman talebi ${x.no} · ${x.departman} · ${x.belgeTipi}${x.onaylayan ? ' · onay ' + x.onaylayan : ''}${x.mutabakat && x.mutabakat !== 'Mutabakat Yok' ? ' · mutabakat: ' + x.mutabakat : ''}`); $('#knDMesaj').textContent = `${x.no} forma alındı (${x.departman} · ${x.kurum} · ${para(x.tutar)} ${pbAd(x.pb)} · son ödeme ${tarih(x.sonOdeme)}); Kaydı Oluştur ile yazılır.`; }
    else if (Y.g === 'fatura') { set('knD_kurum', x.c.ad); set('knD_belgeTipi', 'Fatura'); set('knD_faturaNo', x.f.belgeNo); set('knD_belgeTarihi', x.f.tarih); set('knD_sonOdeme', x.f.vade); set('knD_plan', x.f.vade < bugun() ? bugun() : x.f.vade); set('knD_faturaTutari', x.f.tutar); set('knD_tutar', x.f.kalan); set('knD_pb', x.f.pb); set('knD_kur', V.kurlar[x.f.pb] ?? 1); if (x.f.aciklama) set('knD_urun', x.f.aciklama); banka(x.c); $('#knDMesaj').textContent = `${x.c.ad} · ${x.f.belgeNo} seçildi (kalan ${para(x.f.kalan)} ${pbAd(x.f.pb)} · fatura vadesi ${tarih(x.f.vade)} son ödeme / planlanan tarih oldu${x.c.vadeSarti ? ' · ' + x.c.vadeSarti : ''}); Kaydı Oluştur ile yazılır.`; }
    else { const vade = x.sonOdemeOnerisi || x.ilkVade || bugun(); set('knD_kurum', x.ad); set('knD_belgeTipi', 'Cari Bakiye'); set('knD_faturaNo', ''); set('knD_belgeTarihi', bugun()); set('knD_sonOdeme', vade); set('knD_plan', vade); set('knD_faturaTutari', ''); set('knD_tutar', x.toplam); set('knD_pb', 'TRY'); set('knD_kur', 1); set('knD_urun', `Cari bakiye ödemesi · ${x.adet} açık fatura${x.gecmis ? ' · vadesi geçmiş ' + para(x.gecmis) + ' ₺' : ''}`); banka(x); $('#knDMesaj').textContent = `${x.ad} cari bakiyesi seçildi (${para(x.toplam)} ₺ · ${x.vadeSarti || 'vade şartı'} → son ödeme / planlanan ${tarih(vade)}); tutarı düzenleyip Kaydı Oluştur.`; }
    $('#knD_tutar').dispatchEvent(new Event('input')); $('#knD_belgeTipi').dispatchEvent(new Event('change')); $('#knD_pb').dispatchEvent(new Event('change')); $('#knD_plan').dispatchEvent(new Event('change')); $('#knD_kurum').dispatchEvent(new CustomEvent('cari-not')); Y.g = 'form'; yeniCiz(); $('#knDrawer').scrollTop = 0;
  }
  function yeniBagla() {
    $('#knYeniSekme').addEventListener('click', e => { const b = e.target.closest('button'); if (!b) return; Y.g = b.dataset.g; yeniCiz(); });
    const dbSel = $('#knD_db'), ySel = $('#knYeniSirket'); if (dbSel) { ySel.value = dbSel.value; dbSel.addEventListener('change', () => { ySel.value = dbSel.value; Y.veri = null; if (Y.g !== 'form') yeniCiz(); }); }
    ySel.addEventListener('change', () => { if (dbSel) dbSel.value = ySel.value; Y.veri = null; yeniCiz(); });
    $('#knYeniAra').addEventListener('input', () => { Y.ara = $('#knYeniAra').value; yeniCiz(); });
    $('#knYeniTemsilci').addEventListener('change', () => { Y.temsilci = $('#knYeniTemsilci').value; yeniCiz(); });
    $('#knYeniTablo').addEventListener('click', async e => {
      const ek = e.target.closest('.yeni-talep-ekle'); if (ek) { e.stopPropagation(); await talepEkle(ek.dataset.no); return; }
      const b = e.target.closest('.yeni-sec'); if (b) { e.stopPropagation(); yeniSec(+b.dataset.i); return; } if (e.target.closest('a')) return; const tr = e.target.closest('tr.tik'); if (tr) yeniSec(+tr.dataset.i);
    });
  }
  // departman talebi → doğrudan nakit akış satırı (talep "Nakit Akışa Alındı" olur, satır ilgili sekmede görünür)
  async function talepEkle(no) {
    const d = await post('/Rapor79/OdemeTalebiEkle', { No: no }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    const row = d.row; if (row && (!F.sirket || F.sirket === row.dbName) && (!row.planlanan || (row.planlanan >= F.bas && row.planlanan <= F.bit))) satirYerlestir(row);
    if (Y.veri) { Y.veri.talepler = (Y.veri.talepler || []).filter(t => t.no !== no); Y.veri.talepOnayli = Math.max(0, (Y.veri.talepOnayli || 1) - 1); } if (V && V.talepOzet) { V.talepOzet.acik = Math.max(0, V.talepOzet.acik - 1); V.talepOzet.onayli = Math.max(0, V.talepOzet.onayli - 1); }
    mesaj(row && row.yontem === 'Nakit' ? 'warning' : 'success', '<i class="fas fa-inbox me-1"></i>' + esc(d.message)); ciz(); yeniCiz();
  }
  function yeniAc(g) { Y = { g: g || 'form', db: '', veri: null, ara: '', temsilci: '', liste: [] }; const ad = sirketAdi(F.sirket || V.sirketler[0].dbName); const p = aktifSekme(); drawerAc({ docEntry: 0, dbName: F.sirket, sirket: '', kurum: '', faturaSahibi: ad, odeyenSirket: ad, yontem: p in SEKME && SEKME[p] ? SEKME[p] : 'Havale / EFT', durum: 'Bekliyor', planlanan: bugun(), sonOdeme: '', belgeTipi: 'Fatura', pb: 'TRY', kur: 1, tutar: '', tutarTL: '', notlar: '', alanlar: {}, belgeler: [], dekontlar: [] }); }
  $('#knYeni').addEventListener('click', () => yeniAc('form'));
  // "Talepler": Ödeme Talebi modülünden gelen departman talepleri (tüm şirketler) — onaylananlar "Nakit Akışa Ekle" ile satır olur; rozet = açık talep sayısı (onaylı / bekleyen)
  $('#knTalepler').addEventListener('click', () => yeniAc('talep'));
  function talepRozet() { const o = V && V.talepOzet; const b = $('#knTalepN'); if (!b || !o) return; b.textContent = o.acik; b.className = 'badge ms-1 ' + (o.onayli ? 'bg-success' : 'bg-dark'); $('#knTalepler').title = `${o.acik} açık ödeme talebi · onaylı (nakit akışa eklenebilir) ${o.onayli} · onay bekleyen ${o.bekleyen} · onaylı toplam ${para(o.onayliTutar || 0)} ₺`; }
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-ac'); if (!b) return; e.stopPropagation(); const r = satirdan(b); if (r) drawerAc(r); });

  // ---------- AYLIK ÖDEME · ÇEK VADE: ay ay yönteme göre ödemeler (o ay çıkan çekler vadesi ne olursa olsun) → o ayın çeklerinin vade dağılımı; çıkış ayı × vade ayı matrisi
  const AYLAR = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
  const YONTEMLER = [['Çek', 'Çek'], ['Senet', 'Senet'], ['Havale / EFT', 'Banka (Havale / EFT)'], ['Nakit', 'Nakit'], ['Kredi Kartı / Banka Kartı', 'Kart'], ['Mahsup', 'Mahsup'], ['Temlik', 'Temlik'], ['Yansıtma', 'Yansıtma'], ['Diğer', 'Diğer']];
  let A = { g: 'aylik', yil: new Date().getFullYear(), mod: 'gercek', veri: null, anahtar: '', secAy: null, secYontem: null, cikisAy: '', cekDurum: '', vadeSec: null };
  const ayNo = s => s ? +String(s).slice(5, 7) : 0, yilNo = s => s ? +String(s).slice(0, 4) : 0;
  async function aylikYukle() {
    const key = (F.sirket || '') + '|' + A.yil; if (A.veri && A.anahtar === key) return A.veri;
    $('#kaKapsam').textContent = 'yükleniyor…';
    const d = await getJ(`/Rapor79/AylikOzet?sirket=${encodeURIComponent(F.sirket)}&yil=${A.yil}`).catch(() => null); if (!d || !d.success) { $('#kaKapsam').textContent = 'veri alınamadı'; return null; }
    A.veri = d; A.anahtar = key; A.secAy = null; A.secYontem = null; A.vadeSec = null;
    $('#kaYil').innerHTML = d.yillar.map(y => `<option ${y === A.yil ? 'selected' : ''}>${y}</option>`).join('');
    return d;
  }
  const aylikSatirlar = () => (A.veri?.odemeler || []).filter(r => yilNo(r.tarih) === A.yil && (A.mod === 'tumu' || (A.mod === 'gercek' ? r.gerceklesen : !r.gerceklesen)));
  async function cizAylik() {
    const d = await aylikYukle(); if (!d) return;
    $$('#kaSekme button').forEach(b => { b.classList.toggle('btn-primary', b.dataset.g === A.g); b.classList.toggle('btn-outline-primary', b.dataset.g !== A.g); });
    $$('#kaMod button').forEach(b => { b.classList.toggle('btn-primary', b.dataset.m === A.mod); b.classList.toggle('btn-outline-primary', b.dataset.m !== A.mod); });
    $('#kaAylik').style.display = A.g === 'aylik' ? '' : 'none'; $('#kaVade').style.display = A.g === 'vade' ? '' : 'none'; $('#kaModKap').style.display = A.g === 'aylik' ? '' : 'none'; $('#kaVadeFiltreKap').classList.toggle('d-none', A.g !== 'vade'); $('#kaVadeFiltreKap').style.display = '';
    if (A.g === 'aylik') cizAylikTablo(); else cizVade();
  }
  function cizAylikTablo() {
    const L = aylikSatirlar(), t = $('#kaTablo'), buAy = yilNo(bugun()) === A.yil ? ayNo(bugun()) : 0;
    const hucre = (ay, y) => L.filter(r => ayNo(r.tarih) === ay && (!y || r.yontem === y));
    const toplamlar = {}; let enBuyuk = 0; for (let ay = 1; ay <= 12; ay++) { const tl = hucre(ay).reduce((s, r) => s + r.tutarTL, 0); toplamlar[ay] = tl; enBuyuk = Math.max(enBuyuk, tl); }
    t.tHead.innerHTML = '<tr><th>Ay</th><th class="s">Adet</th>' + YONTEMLER.map(([y, ad]) => `<th class="s" title="${esc(y)}">${esc(ad)}</th>`).join('') + '<th class="s">Toplam (₺)</th><th style="min-width:90px">Pay</th></tr>';
    t.tBodies[0].innerHTML = Array.from({ length: 12 }, (_, i) => i + 1).map(ay => { const satir = hucre(ay); const tl = toplamlar[ay]; return `<tr class="${ay === buAy ? 'buAy' : ''} ${buAy && ay > buAy && A.mod === 'gercek' ? 'gelecek' : ''}"><td class="sol fw-bold">${AYLAR[ay - 1]} ${A.yil}</td><td class="para">${satir.length || '—'}</td>${YONTEMLER.map(([y]) => { const h = hucre(ay, y), v = h.reduce((s, r) => s + r.tutarTL, 0); return `<td class="para hucre ${v ? '' : 'sifir'} ${A.secAy === ay && A.secYontem === y ? 'secili' : ''}" data-ay="${ay}" data-y="${esc(y)}" title="${AYLAR[ay - 1]} · ${esc(y)} · ${h.length} ödeme · tıkla: detay">${v ? para(v) : '—'}${y === 'Çek' || y === 'Senet' ? `<small>${h.length ? h.length + ' satır · ' + (A.veri.cekler.filter(c => h.some(r => r.docEntry === c.docEntry) && c.belge === y).length) + ' belge' : ''}</small>` : ''}</td>`; }).join('')}<td class="para hucre fw-bold ${A.secAy === ay && !A.secYontem ? 'secili' : ''}" data-ay="${ay}" data-y="" title="${AYLAR[ay - 1]} toplam · tıkla: o ayın tüm ödemeleri">${tl ? para(tl) : '—'}</td><td><span class="ka-bar" style="width:${enBuyuk ? Math.round(tl / enBuyuk * 100) : 0}%"></span><small>${enBuyuk ? Math.round(tl / enBuyuk * 100) + '%' : ''}</small></td></tr>`; }).join('');
    const yilToplam = L.reduce((s, r) => s + r.tutarTL, 0);
    t.tFoot.innerHTML = `<tr class="toplam"><td class="text-end">${A.yil} TOPLAM</td><td class="para">${L.length}</td>${YONTEMLER.map(([y]) => `<td class="para">${para(L.filter(r => r.yontem === y).reduce((s, r) => s + r.tutarTL, 0))}</td>`).join('')}<td class="para">${para(yilToplam)}</td><td></td></tr>`;
    const modAd = A.mod === 'gercek' ? 'gerçekleşen (ödenme tarihi)' : A.mod === 'plan' ? 'planlanan (planlanan ödeme tarihi)' : 'gerçekleşen + planlanan';
    $('#kaKapsam').textContent = `${F.sirket ? sirketAdi(F.sirket) : 'Tüm Şirketler (Konsolide)'} · ${A.yil} · ${modAd} · ${L.length} ödeme · ${para(yilToplam)} ₺`;
    $('#kaNot').textContent = '';
    if (A.secAy) aylikDetay(A.secAy, A.secYontem, A.secKurum); else { $('#kaDetayKart').style.display = 'none'; }
    cizCari();
  }
  function aylikDetay(ay, y, kurum) {
    A.secAy = ay; A.secYontem = y || null; A.secKurum = kurum || null; $$('#kaTablo td.hucre').forEach(td => td.classList.toggle('secili', !kurum && +td.dataset.ay === ay && (td.dataset.y || null) === (y || null)));
    $$('#kaCari td.hucre').forEach(td => td.classList.toggle('secili', !!kurum && +td.dataset.ay === ay && td.closest('tr').dataset.kurum === kurum));
    const L = aylikSatirlar().filter(r => ayNo(r.tarih) === ay && (!y || r.yontem === y) && (!kurum || r.kurum === kurum)).sort((a, b) => (a.tarih || '').localeCompare(b.tarih || '')); const kart = $('#kaDetayKart'); kart.style.display = ''; const t = $('#kaDetay');
    const cekMi = y === 'Çek' || y === 'Senet';
    $('#kaDetayBaslik').textContent = `${AYLAR[ay - 1]} ${A.yil} · ${y ? (YONTEMLER.find(x => x[0] === y) || [y, y])[1] : 'Tüm Yöntemler'}${kurum ? ' · ' + kurum : ''}`;
    if (cekMi) {
      const belgeler = A.veri.cekler.filter(c => c.belge === y && L.some(r => r.docEntry === c.docEntry)).sort((a, b) => a.vade.localeCompare(b.vade));
      t.tHead.innerHTML = '<tr><th>Çıkış / Plan Tarihi</th><th>Belge No</th><th>Banka</th><th>Keşideci</th><th>Kimden Geldi</th><th>Vade</th><th>Vade Ayı</th><th>Şirket</th><th>Verilen Cari</th><th class="s">Tutar (₺)</th><th>Akıbet</th><th>Kontrol</th></tr>';
      t.tBodies[0].innerHTML = belgeler.length ? belgeler.map(c => `<tr class="tik" data-doc="${c.docEntry}" data-db="${esc(c.dbName)}"><td>${tarih(c.cikisTarihi)}${c.cikti ? '' : '<small>planlı</small>'}</td><td class="fw-bold">${esc(c.no)}</td><td class="sol">${esc(c.banka)}</td><td class="sol">${esc(c.kesideci)}</td><td class="sol">${esc(c.kimden)}</td><td class="${c.vade < bugun() && !c.cikti ? 'gec' : ''}">${tarih(c.vade)}</td><td>${AYLAR[ayNo(c.vade) - 1]} ${yilNo(c.vade)}</td><td>${esc(c.sirket)}</td><td class="sol fw-bold">${esc(c.kurum)}</td><td class="para">${para(c.tutar)}</td><td>${akibetRozet(c.akibet)}</td><td>${kontrolRozet(c.kontrol, c)}</td></tr>`).join('') : '<tr><td colspan="12" class="na-bos">Bu ayda belge yok.</td></tr>';
      const toplam = belgeler.reduce((s, c) => s + c.tutar, 0); t.tFoot.innerHTML = belgeler.length ? `<tr class="toplam"><td colspan="9" class="text-end">${belgeler.length} belge · vade dağılımı: ${Object.entries(belgeler.reduce((o, c) => { const k = AYLAR[ayNo(c.vade) - 1] + ' ' + yilNo(c.vade); o[k] = (o[k] || 0) + c.tutar; return o; }, {})).map(([k, v]) => k + ' ' + kisa(v)).join(' · ')}</td><td class="para">${para(toplam)}</td><td colspan="2"></td></tr>` : '';
      $('#kaDetayNot').textContent = `${L.length} ödeme satırı · ${belgeler.length} belge · o ay çıkan (ya da planlanan) belgeler, vadesi ne olursa olsun`;
      const g = $('#kaVadeyeGit'); g.style.display = ''; g.dataset.ay = ay; g.dataset.belge = y;
    } else {
      t.tHead.innerHTML = '<tr><th>Tarih</th><th>Şirket</th><th>Ödenecek Cari</th><th>Belge No</th><th>Fatura Sahibi</th><th>Ödeyen</th><th>Yöntem</th><th class="s">Tutar (₺)</th><th>Durum</th><th>Hesap / Kasa · Belge</th></tr>';
      t.tBodies[0].innerHTML = L.length ? L.map(r => `<tr class="tik" data-doc="${r.docEntry}" data-db="${esc(r.dbName)}"><td>${tarih(r.tarih)}${r.gerceklesen ? '' : '<small>planlanan</small>'}</td><td>${esc(r.sirket)}</td><td class="sol fw-bold">${esc(r.kurum)}</td><td class="sol">${esc(r.faturaNo || r.belgeTipi || '')}</td><td>${esc(r.faturaSahibi)}</td><td>${esc(r.odeyenSirket)}</td><td>${yontemRozet(r.yontem)}</td><td class="para">${para(r.tutarTL)}${r.pb !== 'TRY' ? `<small>${para(r.tutar)} ${esc(r.pb)}</small>` : ''}</td><td>${durumRozet(r.durum)}</td><td class="sol">${esc(r.yontem === 'Nakit' ? [r.kasa, r.makbuzNo].filter(Boolean).join(' · ') : r.yontem === 'Havale / EFT' ? (r.bankaHesabi || '').split(' · ')[0] : r.cekNolar || '')}${r.talimatNo ? `<small>${esc(r.talimatNo)}</small>` : ''}</td></tr>`).join('') : '<tr><td colspan="10" class="na-bos">Bu ayda ödeme yok.</td></tr>';
      t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="7" class="text-end">${L.length} ödeme</td><td class="para">${para(L.reduce((s, r) => s + r.tutarTL, 0))}</td><td colspan="2"></td></tr>` : '';
      $('#kaDetayNot').textContent = ''; $('#kaVadeyeGit').style.display = 'none';
    }
  }
  function vadeBelgeleri() { return (A.veri?.cekler || []).filter(c => (yilNo(c.vade) === A.yil || yilNo(c.cikisTarihi) === A.yil) && (!A.cikisAy || (ayNo(c.cikisTarihi) === +A.cikisAy && yilNo(c.cikisTarihi) === A.yil)) && (!A.cekDurum || (A.cekDurum === 'cikti' ? c.cikti : !c.cikti))); }
  function cizVade() {
    const sel = $('#kaCikisAy'); if (!sel.options.length) sel.innerHTML = '<option value="">Tümü</option>' + AYLAR.map((a, i) => `<option value="${i + 1}">${a}</option>`).join(''); sel.value = A.cikisAy; $('#kaCekDurum').value = A.cekDurum;
    const B = vadeBelgeleri(), t = $('#kaVadeTablo');
    const grup = ay => B.filter(c => yilNo(c.vade) === A.yil && ayNo(c.vade) === ay), sonraki = B.filter(c => yilNo(c.vade) > A.yil), onceki = B.filter(c => yilNo(c.vade) < A.yil);
    const satir = (ad, l, key) => { const cek = l.filter(c => c.belge === 'Çek'), sen = l.filter(c => c.belge === 'Senet'); const top = l.reduce((s, c) => s + c.tutar, 0); return `<tr class="${l.length ? 'tik' : ''} ${A.vadeSec === key ? 'secili' : ''}" data-vade="${key}" title="${l.length ? 'tıkla: belgeler' : ''}"><td class="sol fw-bold">${esc(ad)}</td><td class="para">${cek.length || '—'}</td><td class="para">${cek.length ? para(cek.reduce((s, c) => s + c.tutar, 0)) : '—'}</td><td class="para">${sen.length || '—'}</td><td class="para">${sen.length ? para(sen.reduce((s, c) => s + c.tutar, 0)) : '—'}</td><td class="para fw-bold">${top ? para(top) : '—'}</td><td class="para">${para(l.filter(c => c.cikti).reduce((s, c) => s + c.tutar, 0))}</td><td class="para">${para(l.filter(c => !c.cikti).reduce((s, c) => s + c.tutar, 0))}</td></tr>`; };
    t.tBodies[0].innerHTML = (onceki.length ? satir('Önceki Yıllar', onceki, 'onceki') : '') + AYLAR.map((a, i) => satir(`${a} ${A.yil}`, grup(i + 1), String(i + 1))).join('') + (sonraki.length ? satir('Sonraki Yıllar', sonraki, 'sonraki') : '');
    t.tFoot.innerHTML = `<tr class="toplam"><td class="text-end">TOPLAM</td><td class="para">${B.filter(c => c.belge === 'Çek').length}</td><td class="para">${para(B.filter(c => c.belge === 'Çek').reduce((s, c) => s + c.tutar, 0))}</td><td class="para">${B.filter(c => c.belge === 'Senet').length}</td><td class="para">${para(B.filter(c => c.belge === 'Senet').reduce((s, c) => s + c.tutar, 0))}</td><td class="para">${para(B.reduce((s, c) => s + c.tutar, 0))}</td><td class="para">${para(B.filter(c => c.cikti).reduce((s, c) => s + c.tutar, 0))}</td><td class="para">${para(B.filter(c => !c.cikti).reduce((s, c) => s + c.tutar, 0))}</td></tr>`;
    $('#kaVadeNot').textContent = `${B.length} belge · ${A.cikisAy ? AYLAR[A.cikisAy - 1] + ' ayında çıkan / planlanan' : 'tüm çıkışlar'} · ${A.cekDurum === 'cikti' ? 'çıkmış' : A.cekDurum === 'planli' ? 'planlı' : 'çıkmış + planlı'}`;
    // çıkış ayı × vade ayı matrisi (çıkış ayı filtresinden bağımsız: tüm yıl; seçili çıkış ayı vurgulu)
    const M = (A.veri?.cekler || []).filter(c => yilNo(c.cikisTarihi) === A.yil && (!A.cekDurum || (A.cekDurum === 'cikti' ? c.cikti : !c.cikti))), m = $('#kaMatris');
    const vadeKol = vadeKolNo;
    m.tHead.innerHTML = '<tr><th>Çıkış Ayı ↓ · Vade Ayı →</th>' + AYLAR.map(a => `<th class="s">${a.slice(0, 3)}</th>`).join('') + '<th class="s">Sonraki Yıl</th><th class="s">Toplam</th></tr>';
    m.tBodies[0].innerHTML = AYLAR.map((a, i) => { const ay = i + 1, l = M.filter(c => ayNo(c.cikisTarihi) === ay); return `<tr class="${+A.cikisAy === ay ? 'vurgu' : ''}"><td class="sol fw-bold">${a} ${A.yil}</td>${Array.from({ length: 13 }, (_, j) => j + 1).map(k => { const h = l.filter(c => vadeKol(c) === k), v = h.reduce((s, c) => s + c.tutar, 0); return `<td class="para hucre ${v ? '' : 'sifir'} ${A.vadeSec === 'm' + ay + '-' + k ? 'secili' : ''}" data-cikis="${ay}" data-vade="${k}" title="çıkış ${a} · vade ${k === 13 ? 'sonraki yıl' : AYLAR[k - 1]} · ${h.length} belge">${v ? kisa(v) : '·'}</td>`; }).join('')}<td class="para fw-bold">${l.length ? kisa(l.reduce((s, c) => s + c.tutar, 0)) : '—'}</td></tr>`; }).join('');
    m.tFoot.innerHTML = '<tr class="toplam"><td class="text-end">TOPLAM</td>' + Array.from({ length: 13 }, (_, j) => j + 1).map(k => `<td class="para">${kisa(M.filter(c => vadeKol(c) === k).reduce((s, c) => s + c.tutar, 0)) || '·'}</td>`).join('') + `<td class="para">${kisa(M.reduce((s, c) => s + c.tutar, 0))}</td></tr>`;
    $('#kaKapsam').textContent = `${F.sirket ? sirketAdi(F.sirket) : 'Tüm Şirketler (Konsolide)'} · ${A.yil} · çek / senet vade dağılımı · ${B.length} belge · ${para(B.reduce((s, c) => s + c.tutar, 0))} ₺`;
    if (A.vadeSec) { const [tip, a, b] = A.vadeSec.startsWith('m') ? ['m', ...A.vadeSec.slice(1).split('-').map(Number)] : ['v', A.vadeSec]; if (tip === 'm') vadeDetay(M.filter(c => ayNo(c.cikisTarihi) === a && vadeKol(c) === b), `Çıkış ${AYLAR[a - 1]} → vade ${b === 13 ? 'sonraki yıl' : AYLAR[b - 1]}`); else vadeDetay(a === 'onceki' ? onceki : a === 'sonraki' ? sonraki : grup(+a), a === 'onceki' ? 'Önceki yıllar vadeli' : a === 'sonraki' ? 'Sonraki yıllar vadeli' : `Vade ${AYLAR[a - 1]} ${A.yil}`); }
    else $('#kaVadeDetayKart').style.display = 'none';
    cizVadeCari(B);
  }
  // cari bazında: aylık ödemeler (ay ay çıkışlar, yıl toplamı, bu aya kadar, aylık ortalama ÷ ay sayısı) ve vade ayına göre verilen çek / senet (tahsile gönderilenler banka bazında)
  const kompakt = v => Math.abs(v) >= 1e6 ? (v / 1e6).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + ' Mn' : Math.abs(v) >= 1e3 ? Math.round(v / 1e3).toLocaleString('tr-TR') + ' B' : kisa(v);
  const buAyNo = () => yilNo(bugun()) === A.yil ? ayNo(bugun()) : yilNo(bugun()) > A.yil ? 12 : 0;
  const vadeKolNo = c => yilNo(c.vade) > A.yil ? 13 : yilNo(c.vade) < A.yil ? 0 : ayNo(c.vade);
  const vadeAnahtar = c => /Tahsil|Bankaya/.test(c.akibet || '') || /\(Tahsil\)/.test(c.kurum || '') ? (c.ibrazYeri || c.kurum).replace(/ \(Tahsil\)$/, '') + ' (Tahsil)' : c.kurum;
  function cizCari() {
    const sel = $('#kaCariYontem'); if (sel.options.length < 2) sel.innerHTML = '<option value="">Tüm Yöntemler</option>' + YONTEMLER.map(([y, ad]) => `<option value="${esc(y)}">${esc(ad)}</option>`).join('');
    const yf = sel.value, L = aylikSatirlar().filter(r => !yf || r.yontem === yf), n = buAyNo(), t = $('#kaCari');
    const grup = {}; L.forEach(r => { const g = grup[r.kurum] || (grup[r.kurum] = { kurum: r.kurum, aylar: Array(13).fill(0), adet: 0, yontem: {} }); g.aylar[ayNo(r.tarih)] += r.tutarTL; g.adet++; g.yontem[r.yontem] = (g.yontem[r.yontem] || 0) + r.tutarTL; });
    const S = Object.values(grup).map(g => { g.toplam = g.aylar.reduce((s, v) => s + v, 0); g.buAya = g.aylar.slice(1, n + 1).reduce((s, v) => s + v, 0); g.ort = n ? g.buAya / n : 0; return g; }).sort((a, b) => b.toplam - a.toplam);
    const yAd = y => ((YONTEMLER.find(x => x[0] === y) || [y, y])[1]).replace('Banka (Havale / EFT)', 'Banka');
    t.tHead.innerHTML = `<tr><th>Cari</th><th class="s">Adet</th><th>Yönteme Göre</th>${AYLAR.map((a, i) => `<th class="s ${i + 1 === n ? 'buAy' : ''}">${a.slice(0, 3)}</th>`).join('')}<th class="s">Yıl Toplamı</th><th class="s" title="${n ? AYLAR[0] + ' – ' + AYLAR[n - 1] + ' (' + n + ' ay)' : 'bu yıl henüz başlamadı'}">Bu Aya Kadar</th><th class="s" title="bu aya kadar toplam ÷ ${n || '—'} ay">Aylık Ort. (÷${n || '—'})</th></tr>`;
    t.tBodies[0].innerHTML = S.length ? S.map(g => `<tr class="tik" data-kurum="${esc(g.kurum)}"><td class="sol cari" title="${esc(g.kurum)}">${esc(g.kurum)}</td><td class="para">${g.adet}</td><td class="sol" style="font-size:.58rem;color:#475569;max-width:170px;overflow:hidden;text-overflow:ellipsis" title="${esc(Object.entries(g.yontem).map(([y, v]) => y + ' ' + para(v)).join(' · '))}">${Object.entries(g.yontem).sort((a, b) => b[1] - a[1]).map(([y, v]) => `${esc(yAd(y))} ${kompakt(v)}`).join(' · ')}</td>${AYLAR.map((a, i) => `<td class="para hucre ${g.aylar[i + 1] ? '' : 'sifir'} ${i + 1 === n ? 'buAy' : ''}" data-ay="${i + 1}" title="${esc(g.kurum)} · ${a} · tıkla: ödemeler">${g.aylar[i + 1] ? kisa(g.aylar[i + 1]) : '·'}</td>`).join('')}<td class="para ozet">${kisa(g.toplam)}</td><td class="para ozet">${n ? kisa(g.buAya) : '—'}</td><td class="para ozet">${n ? kisa(g.ort) : '—'}</td></tr>`).join('') : '<tr><td colspan="18" class="na-bos">Kayıt yok.</td></tr>';
    const kol = i => S.reduce((s, g) => s + g.aylar[i], 0), top = S.reduce((s, g) => s + g.toplam, 0), ba = S.reduce((s, g) => s + g.buAya, 0);
    t.tFoot.innerHTML = S.length ? `<tr class="toplam"><td class="text-end">TOPLAM</td><td class="para">${S.reduce((s, g) => s + g.adet, 0)}</td><td></td>${AYLAR.map((a, i) => `<td class="para">${kol(i + 1) ? kisa(kol(i + 1)) : '·'}</td>`).join('')}<td class="para">${kisa(top)}</td><td class="para">${n ? kisa(ba) : '—'}</td><td class="para">${n ? kisa(ba / n) : '—'}</td></tr>` : '';
    $('#kaCariNot').textContent = `${S.length} cari · ${yf ? yAd(yf) : 'tüm yöntemler'} · ${A.mod === 'gercek' ? 'gerçekleşen' : A.mod === 'plan' ? 'planlanan' : 'gerçekleşen + planlanan'} · bu aya kadar = ${n ? AYLAR[0] + '–' + AYLAR[n - 1] : '—'} · aylık ortalama ÷ ${n || '—'}`;
  }
  function cizVadeCari(B) {
    const n = buAyNo(), t = $('#kaVadeCari'); const grup = {};
    B.forEach(c => { const k = vadeAnahtar(c); const g = grup[k] || (grup[k] = { kurum: k, aylar: Array(14).fill(0), adet: 0, cek: 0, senet: 0 }); g.aylar[vadeKolNo(c)] += c.tutar; g.adet++; if (c.belge === 'Çek') g.cek += c.tutar; else g.senet += c.tutar; });
    const S = Object.values(grup).map(g => { g.toplam = g.aylar.reduce((s, v) => s + v, 0); g.buAya = g.aylar.slice(1, n + 1).reduce((s, v) => s + v, 0); g.ort = n ? g.buAya / n : 0; return g; }).sort((a, b) => b.toplam - a.toplam);
    const K13 = Array.from({ length: 13 }, (_, j) => j + 1);
    t.tHead.innerHTML = `<tr><th>Cari</th><th class="s">Adet</th><th class="s">Çek</th><th class="s">Senet</th>${AYLAR.map((a, i) => `<th class="s ${i + 1 === n ? 'buAy' : ''}">${a.slice(0, 3)}</th>`).join('')}<th class="s">Sonraki Yıl</th><th class="s">Toplam</th><th class="s" title="vade ${n ? AYLAR[0] + ' – ' + AYLAR[n - 1] : '—'}">Bu Aya Kadar</th><th class="s">Aylık Ort. (÷${n || '—'})</th></tr>`;
    t.tBodies[0].innerHTML = S.length ? S.map(g => `<tr class="tik" data-kurum="${esc(g.kurum)}"><td class="sol cari" title="${esc(g.kurum)}">${esc(g.kurum)}</td><td class="para">${g.adet}</td><td class="para">${g.cek ? kisa(g.cek) : '·'}</td><td class="para">${g.senet ? kisa(g.senet) : '·'}</td>${K13.map(k => `<td class="para hucre ${g.aylar[k] ? '' : 'sifir'} ${k === n ? 'buAy' : ''}" data-vade="${k}" title="${esc(g.kurum)} · vade ${k === 13 ? 'sonraki yıl' : AYLAR[k - 1]} · tıkla: belgeler">${g.aylar[k] ? kisa(g.aylar[k]) : '·'}</td>`).join('')}<td class="para ozet">${kisa(g.toplam)}</td><td class="para ozet">${n ? kisa(g.buAya) : '—'}</td><td class="para ozet">${n ? kisa(g.ort) : '—'}</td></tr>`).join('') : '<tr><td colspan="21" class="na-bos">Belge yok.</td></tr>';
    const kol = k => S.reduce((s, g) => s + g.aylar[k], 0), top = S.reduce((s, g) => s + g.toplam, 0), ba = S.reduce((s, g) => s + g.buAya, 0);
    t.tFoot.innerHTML = S.length ? `<tr class="toplam"><td class="text-end">TOPLAM</td><td class="para">${S.reduce((s, g) => s + g.adet, 0)}</td><td class="para">${kisa(S.reduce((s, g) => s + g.cek, 0))}</td><td class="para">${kisa(S.reduce((s, g) => s + g.senet, 0))}</td>${K13.map(k => `<td class="para">${kol(k) ? kisa(kol(k)) : '·'}</td>`).join('')}<td class="para">${kisa(top)}</td><td class="para">${n ? kisa(ba) : '—'}</td><td class="para">${n ? kisa(ba / n) : '—'}</td></tr>` : '';
    $('#kaVadeCariNot').textContent = `${S.length} cari · vade ayına göre · "Banka (Tahsil)" = bankaya tahsile gönderilen belgeler · bu aya kadar = vade ${n ? AYLAR[0] + '–' + AYLAR[n - 1] : '—'} · aylık ortalama ÷ ${n || '—'}`;
  }
  $('#kaCariYontem').addEventListener('change', cizCari);
  $('#kaDetayKapat').addEventListener('click', () => { A.secAy = null; A.secYontem = null; A.secKurum = null; $('#kaDetayKart').style.display = 'none'; $$('#kaTablo td.secili, #kaCari td.secili').forEach(x => x.classList.remove('secili')); });
  $('#kaVadeDetayKapat').addEventListener('click', () => { A.vadeSec = null; $('#kaVadeDetayKart').style.display = 'none'; $$('#kaMatris td.secili, #kaVadeTablo tr.secili').forEach(x => x.classList.remove('secili')); });
  $('#kaCari').addEventListener('click', e => { const td = e.target.closest('td.hucre'); if (!td) return; aylikDetay(+td.dataset.ay, $('#kaCariYontem').value || null, td.closest('tr').dataset.kurum); $('#kaDetayKart').scrollIntoView({ block: 'nearest' }); });
  $('#kaVadeCari').addEventListener('click', e => { const td = e.target.closest('td.hucre'); if (!td) return; const k = +td.dataset.vade, kurum = td.closest('tr').dataset.kurum; A.vadeSec = null; $$('#kaMatris td.secili, #kaVadeTablo tr.secili').forEach(x => x.classList.remove('secili')); vadeDetay(vadeBelgeleri().filter(c => vadeAnahtar(c) === kurum && vadeKolNo(c) === k), `${kurum} · vade ${k === 13 ? 'sonraki yıl' : AYLAR[k - 1] + ' ' + A.yil}`); $('#kaVadeDetayKart').scrollIntoView({ block: 'nearest' }); });
  function vadeDetay(l, baslik) {
    const t = $('#kaVadeDetay'); $('#kaVadeDetayKart').style.display = ''; l = l.slice().sort((a, b) => a.vade.localeCompare(b.vade));
    $('#kaVadeDetayBaslik').textContent = baslik; $('#kaVadeDetayNot').textContent = `${l.length} belge · ${para(l.reduce((s, c) => s + c.tutar, 0))} ₺`;
    t.tBodies[0].innerHTML = l.length ? l.map(c => `<tr class="tik" data-doc="${c.docEntry}" data-db="${esc(c.dbName)}"><td class="fw-bold">${esc(c.no)}</td><td>${esc(c.belge)}</td><td class="sol">${esc(c.banka)}</td><td class="sol">${esc(c.kesideci)}</td><td class="sol">${esc(c.kimden)}</td><td class="${c.vade < bugun() && !c.cikti ? 'gec' : ''}">${tarih(c.vade)}</td><td>${tarih(c.cikisTarihi)}${c.cikti ? '' : '<small>planlı</small>'}</td><td>${esc(c.sirket)}</td><td class="sol fw-bold">${esc(c.kurum)}</td><td class="para">${para(c.tutar)}</td><td>${akibetRozet(c.akibet)}</td><td>${kontrolRozet(c.kontrol, c)}</td></tr>`).join('') : '<tr><td colspan="12" class="na-bos">Belge yok.</td></tr>';
    t.tFoot.innerHTML = l.length ? `<tr class="toplam"><td colspan="9" class="text-end">${l.length} belge</td><td class="para">${para(l.reduce((s, c) => s + c.tutar, 0))}</td><td colspan="2"></td></tr>` : '';
  }
  $('#kaSekme').addEventListener('click', e => { const b = e.target.closest('button'); if (!b) return; A.g = b.dataset.g; cizAylik(); });
  $('#kaMod').addEventListener('click', e => { const b = e.target.closest('button'); if (!b) return; A.mod = b.dataset.m; A.secAy = null; A.secYontem = null; cizAylik(); });
  $('#kaYil').addEventListener('change', () => { A.yil = +$('#kaYil').value; A.veri = null; A.cikisAy = ''; A.vadeSec = null; cizAylik(); });
  $('#kaCikisAy').addEventListener('change', () => { A.cikisAy = $('#kaCikisAy').value; A.vadeSec = null; cizVade(); }); $('#kaCekDurum').addEventListener('change', () => { A.cekDurum = $('#kaCekDurum').value; A.vadeSec = null; cizVade(); });
  $('#kaTablo').addEventListener('click', e => { const td = e.target.closest('td.hucre'); if (!td) return; aylikDetay(+td.dataset.ay, td.dataset.y || null, null); });
  $('#kaVadeyeGit').addEventListener('click', () => { const g = $('#kaVadeyeGit'); A.g = 'vade'; A.cikisAy = g.dataset.ay; A.cekDurum = ''; A.vadeSec = null; cizAylik(); });
  $('#kaVadeTablo').addEventListener('click', e => { const tr = e.target.closest('tr.tik'); if (!tr) return; A.vadeSec = A.vadeSec === tr.dataset.vade ? null : tr.dataset.vade; cizVade(); });
  $('#kaMatris').addEventListener('click', e => { const td = e.target.closest('td.hucre'); if (!td) return; const key = 'm' + td.dataset.cikis + '-' + td.dataset.vade; A.vadeSec = A.vadeSec === key ? null : key; cizVade(); });
  // detay satırı → çekmece (satır dönem dışındaysa sunucudan getirilir)
  async function satirGetirAc(doc, db) { let r = satirBul(doc); if (!r) { const d = await getJ(`/Rapor79/SatirBelgeleri?dbName=${encodeURIComponent(db)}&docEntry=${doc}`).catch(() => null); if (d && d.success) r = d.row; } if (r) drawerAc(r); }
  ['#kaDetay', '#kaVadeDetay'].forEach(s => $(s).addEventListener('click', e => { const tr = e.target.closest('tr.tik'); if (!tr) return; e.stopPropagation(); satirGetirAc(+tr.dataset.doc, tr.dataset.db); }));

  // ---------- başlangıç
  // ?doc=1234: Takip Paneli / Çek Akıbeti'nden gelen derin bağlantı — satır çekmecesi açılır (satır dönem dışındaysa sunucudan getirilir)
  (async () => { await yukle(); if (!V) return; sekme(kok.dataset.sekme || 'tumu'); try { const u = new URL(location.href), doc = +(u.searchParams.get('doc') || 0); if (doc) { const db = u.searchParams.get('sirket'); await satirGetirAc(doc, db && db !== 'tumu' ? db : (F.sirket || 'URASKIMYA')); u.searchParams.delete('doc'); history.replaceState(null, '', u); } } catch (e) { } })();
})();
