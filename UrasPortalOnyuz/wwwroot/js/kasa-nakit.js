// ============================================================
// KASA NAKİT AKIŞI — modern ekran betiği (Views/Rapor79/Index.cshtml; veri /Rapor79/Veri; kaynak Data/NakitAkisOrnek.cs + CekAkibetOrnek.cs + TalimatOrnek.cs + Mahsup / Temlik / Yansıtma / Nakit Avans modülleri)
// Sekmeler ödeme yöntemine göre; her sekmede yalnız o yöntemin sütunları (KOL; başlıklar tam ad). Her satırda fatura sahibi ve ödeyen şirket.
// Satır içi düzenleme (.kn-alan → AlanKaydet), çekmece (SatirKaydet / SatirEkle / SilJson / DekontEkle), satır seçimi (.kn-sec) → "Seçilenlerden Talimat Oluştur" (TalimatOlustur; Talimatlar sekmesi: durum akışı, yazdır).
// Çek / Senet: portföy görünümü (SecilebilirBelgeler: şirket, vade, tutar, banka süzgeçleri, tutara yakın seçim) → BelgePlanla (Çek Akıbeti'nde Planlandı); belge başına Dolaşım Planı
// (DolasimOner / DolasimSimule / DolasimKaydet / DolasimAdim / DolasimIptal): grup içi zincir + uygunluk simülasyonu + adım gerçekleştirme. Mahsup / Temlik / Yansıtma kayıt bağlama; Nakit avans satırları.
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
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + new Date().toISOString().slice(0, 10) + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 9000); }
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
  const belgeCikmis = x => x.akibet && x.akibet !== 'Planlandı' && x.akibet !== 'Portföyde';

  let V = null, F = { sirket: kok.dataset.sirket || '', bas: kok.dataset.bas, bit: kok.dataset.bit, durum: '', satinalmaci: '', ara: '' }, acik = null;
  const aktifSekme = () => ($('#knSekmeler button.aktif') || {}).dataset?.p || 'tumu';
  const satirlar = () => V.odemeler.filter(r => (!F.durum || r.durum === F.durum) && (!F.satinalmaci || r.satinalmaci === F.satinalmaci) && aramaUyar(r));
  const aramaUyar = r => { const a = F.ara.trim().toLocaleLowerCase('tr-TR'); return !a || [r.kurum, r.faturaNo, r.urun, r.notlar, r.cekNolar, r.sirket, r.faturaSahibi, r.odeyenSirket, r.talimatNo, r.avansNo, r.grupIci?.no, r.grupIci?.karsi, r.alanlar?.dekontNo, r.alanlar?.aliciIban].join(' ').toLocaleLowerCase('tr-TR').includes(a); };
  const satirBul = d => V.odemeler.find(r => r.docEntry === d);
  const satirYerlestir = row => { if (!row) return; const i = V.odemeler.findIndex(r => r.docEntry === row.docEntry); if (i >= 0) V.odemeler[i] = row; else V.odemeler.push(row); };
  const sirketAdi = db => (V.sirketler.find(x => x.dbName === db) || {}).ad || db;

  // ---------- yükleme
  async function yukle(sessiz) {
    if (!sessiz) $('#knKapsam').textContent = 'yükleniyor…';
    let r; try { r = await getJ(`/Rapor79/Veri?sirket=${encodeURIComponent(F.sirket)}&bas=${F.bas}&bit=${F.bit}`); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r;
    const d = $('#knDurum'), mevcutD = d.value; d.innerHTML = '<option value="">Tümü</option>' + V.durumlar.map(x => `<option>${esc(x)}</option>`).join(''); d.value = mevcutD;
    const s = $('#knSatinalmaci'), mevcutS = s.value; s.innerHTML = '<option value="">Tümü</option>' + V.satinalmacilar.map(x => `<option>${esc(x)}</option>`).join(''); s.value = mevcutS;
    ciz();
  }
  function ciz() {
    if (!V) return;
    const L = satirlar();
    $('#knKapsam').textContent = `${F.sirket ? sirketAdi(F.sirket) : 'Tüm Şirketler (Konsolide)'} · ${tarih(F.bas)} – ${tarih(F.bit)} · ${L.length} kayıt`;
    kpi(L); sayaclar(L);
    const p = aktifSekme();
    if (p === 'bakiye') cizBakiye(); else if (p === 'gunluk') cizGunluk(); else if (p === 'talimat') cizTalimatlar(); else cizTablo(p, L);
    if (p === 'mahsup' || p === 'temlik' || p === 'yansitma') cizGrupOzet(p);
  }
  function sayaclar(L) { Object.keys(SEKME).forEach(k => { const n = $('#knN-' + k); if (n) n.textContent = k === 'tumu' ? L.length : L.filter(r => r.yontem === SEKME[k]).length; }); $('#knN-bakiye').textContent = V.bakiyeler.length; $('#knN-gunluk').textContent = V.gunluk.length; $('#knN-talimat').textContent = (V.talimatlar || []).filter(t => !t.kapandi).length + '/' + (V.talimatlar || []).length; }
  function kpi(L) {
    const bugun = V.bugun, hafta = new Date(bugun); hafta.setDate(hafta.getDate() + 7); const haftaS = hafta.toISOString().slice(0, 10);
    const odenmemis = L.filter(r => r.durum !== 'ÖDENDİ'), odenen = L.filter(r => r.durum === 'ÖDENDİ');
    const toplam = L.reduce((s, r) => s + r.tutarTL, 0), odenenT = odenen.reduce((s, r) => s + (r.odenen ? r.odenen * (r.kur || 1) : r.tutarTL), 0), kalan = odenmemis.reduce((s, r) => s + r.tutarTL, 0);
    const buHafta = odenmemis.filter(r => r.planlanan && r.planlanan >= bugun && r.planlanan <= haftaS), geciken = odenmemis.filter(r => r.durum === 'Gecikti' || (r.planlanan && r.planlanan < bugun));
    const B = V.bakiyeler, nakit = B.filter(b => b.bankaKasa === 'BANKA' || b.bankaKasa === 'KASA').reduce((s, b) => s + b.tutarTL, 0), portfoy = B.filter(b => b.bankaKasa.includes('PORTFÖY')).reduce((s, b) => s + b.tutarTL, 0);
    const cekBekleyen = odenmemis.filter(r => r.yontem === 'Çek' || r.yontem === 'Senet').reduce((s, r) => s + r.tutarTL, 0), nakitBekleyen = kalan - cekBekleyen - odenmemis.filter(r => ['Mahsup', 'Temlik', 'Yansıtma'].includes(r.yontem)).reduce((s, r) => s + r.tutarTL, 0);
    const talimatli = odenmemis.filter(r => r.talimatNo).length, avans = V.avans || { bekleyen: 0, bekleyenTutar: 0 };
    const K = [
      { l: 'Dönem Toplamı', v: kisa(toplam), s: `${L.length} kayıt · ${odenmemis.length} açık · ${talimatli} talimatlı`, k: '#0f766e' },
      { l: 'Ödenen', v: kisa(odenenT), s: `${odenen.length} kayıt`, k: '#15803d', d: 'ÖDENDİ' },
      { l: 'Kalan Ödenecek', v: kisa(kalan), s: `nakit ${kisa(nakitBekleyen)} · çek/senet ${kisa(cekBekleyen)}`, k: '#b45309' },
      { l: 'Bu Hafta Vadesi Gelen', v: kisa(buHafta.reduce((s, r) => s + r.tutarTL, 0)), s: `${buHafta.length} ödeme · ${tarih(bugun)} – ${tarih(haftaS)}`, k: '#7c3aed' },
      { l: 'Geciken', v: kisa(geciken.reduce((s, r) => s + r.tutarTL, 0)), s: `${geciken.length} ödeme · tıkla: filtre`, k: '#dc2626', d: 'Gecikti' },
      { l: 'Banka + Kasa Mevcudu', v: kisa(nakit), s: `${B.filter(b => b.bankaKasa === 'BANKA').length} banka hesabı`, k: '#1d4ed8', p: 'bakiye' },
      { l: 'Çek / Senet Portföyü', v: kisa(portfoy), s: 'portföyde + planlandı', k: '#0369a1', p: 'bakiye' },
      { l: 'Ödeme Bekleyen Avans', v: kisa(avans.bekleyenTutar), s: `${avans.bekleyen} onaylı avans · Nakit sekmesinde`, k: '#b45309', p: 'nakit' }
    ];
    $('#knKpi').innerHTML = K.map(k => `<div style="--k:${k.k};${k.d && F.durum === k.d ? 'background:#f0fdfa' : ''}" ${k.d ? `data-durum="${k.d}" title="Durum filtresi"` : k.p ? `data-p="${k.p}" title="Sekmeye git"` : ''}><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
  }

  // ---------- sütunlar (sekmeye göre): [başlık, genişlik ağırlığı]; sabit tablo düzeni → tek ekrana sığar
  const K = (ad, w) => ({ ad, w });
  const C_SEC = K('', 24), C_SIRA = K('Sıra', 30), C_PLAN = K('Planlanan Ödeme Tarihi', 104), C_KURUM = K('Ödenecek Cari · Ürün / Hizmet', 190), C_FIRMA = K('Fatura Sahibi / Ödeyen Şirket', 128), C_BELGE = K('Fatura / Sipariş No', 92), C_TL = K('Tutar (₺)', 100), C_DURUM = K('Ödeme Durumu', 112), C_ODENME = K('Ödenme Tarihi', 80), C_ODENEN = K('Ödenen Tutar', 92), C_NOT = K('Not', 116), C_TALIMAT = K('Talimat', 96), C_ISLEM = K('', 30);
  const KOL = {
    tumu: [C_SEC, C_SIRA, C_PLAN, C_KURUM, C_FIRMA, C_BELGE, K('Ödeme Yöntemi', 90), C_TL, C_DURUM, C_ODENME, C_ODENEN, K('Mutabakat Durumu', 80), C_TALIMAT, C_NOT, C_ISLEM],
    nakit: [C_SEC, C_SIRA, C_PLAN, C_KURUM, C_FIRMA, C_BELGE, C_TL, K('Kasa', 110), K('Makbuz No', 90), K('Teslim Alan', 100), C_DURUM, C_ODENME, C_ODENEN, C_TALIMAT, C_ISLEM],
    havale: [C_SEC, C_SIRA, C_PLAN, C_KURUM, C_FIRMA, C_BELGE, C_TL, K('Banka Hesabımız', 140), K('Alıcı IBAN', 150), K('Dekont No', 86), K('Dekont', 44), C_DURUM, C_ODENME, C_TALIMAT, C_ISLEM],
    kredi: [C_SEC, C_SIRA, C_PLAN, C_KURUM, C_FIRMA, C_BELGE, C_TL, K('Kart', 140), K('Taksit', 84), K('Ekstre Dönemi', 96), C_DURUM, C_ODENME, C_ODENEN, C_TALIMAT, C_ISLEM],
    cek: [C_SEC, C_SIRA, C_PLAN, K('Ödenecek Cari · Ürün / Hizmet', 150), K('Fatura Sahibi / Ödeyen Şirket', 124), C_TL, K('Ödeme Durumu', 108), K('Çek No · Çekin Bankası', 156), K('Keşideci · Kimden Geldi', 130), K('Çek Vadesi', 86), K('Çek Tutarı', 100), K('Akıbet · Plan Kontrolü · Dolaşım', 170), K('Belge İşlemi', 104), C_TALIMAT, C_ISLEM],
    senet: [C_SEC, C_SIRA, C_PLAN, K('Ödenecek Cari · Ürün / Hizmet', 150), K('Fatura Sahibi / Ödeyen Şirket', 124), C_TL, K('Ödeme Durumu', 108), K('Senet No · Senet Bankası', 156), K('Keşideci · Kimden Geldi', 130), K('Senet Vadesi', 86), K('Senet Tutarı', 100), K('Akıbet · Plan Kontrolü · Dolaşım', 170), K('Belge İşlemi', 104), C_TALIMAT, C_ISLEM],
    mahsup: [C_SEC, C_SIRA, C_PLAN, C_KURUM, C_FIRMA, C_TL, C_DURUM, K('Mahsup No', 104), K('Mahsup Türü · Taraflar', 210), K('Mahsup Tutarı', 100), K('Mahsup Durumu · Muhasebe', 160), K('Kontrol', 100), K('Kayıt Bağı', 96), C_TALIMAT, C_ISLEM],
    temlik: [C_SEC, C_SIRA, C_PLAN, C_KURUM, C_FIRMA, C_TL, C_DURUM, K('Temlik No', 104), K('Temlik Türü · İlk Eden → Nihai Alan', 210), K('Temlik Tutarı', 100), K('Temlik Durumu · İmza · Muhasebe', 160), K('Kontrol', 100), K('Kayıt Bağı', 96), C_TALIMAT, C_ISLEM],
    yansitma: [C_SEC, C_SIRA, C_PLAN, C_KURUM, C_FIRMA, C_TL, C_DURUM, K('Yansıtma No', 104), K('Tür · Yansıtan → Yansıtılan', 210), K('Genel Toplam', 100), K('Durum · Fatura / Entegrasyon', 160), K('Kontrol', 100), K('Kayıt Bağı', 96), C_TALIMAT, C_ISLEM],
    diger: [C_SEC, C_SIRA, C_PLAN, C_KURUM, C_FIRMA, C_BELGE, C_TL, K('İşlem Tipi', 64), K('Belge Tipi', 80), C_DURUM, C_ODENME, C_ODENEN, C_TALIMAT, C_NOT, C_ISLEM]
  };
  const alan = (r, ad, deger, tip, secenekler, kisaGoster) => tip === 'select' ? `<select class="kn-alan" data-alan="${ad}" title="${esc(deger || '')}"><option value="">—</option>${secenekler.map(o => `<option value="${esc(o)}" ${o === deger ? 'selected' : ''}>${esc(kisaGoster ? kisaGoster(o) : o)}</option>`).join('')}</select>` : `<input type="text" class="kn-alan" data-alan="${ad}" value="${esc(deger)}" title="${esc(deger || '')}" placeholder="…">`;
  const yigin = (liste, fn) => liste.length ? liste.map(x => `<span class="yb">${fn(x)}</span>`).join('') : bos;
  const paraTL = r => `<td class="para" title="${esc(para(r.tutar) + ' ' + (r.pb || ''))}">${para(r.tutarTL)}${r.pb && r.pb !== 'TRY' ? `<small>${para(r.tutar)} ${esc(r.pb)} × ${esc(String(r.kur))}</small>` : ''}</td>`;
  function hucreler(r, p) {
    const a = r.alanlar || {}, g = r.grupIci, b = r.belgeler || [], av = r.avans;
    const cikmis = b.filter(belgeCikmis);
    const belgesiz = (r.yontem === 'Çek' || r.yontem === 'Senet') && !b.length;
    const sec = `<td><input type="checkbox" class="kn-sec form-check-input" ${r.durum === 'ÖDENDİ' ? 'disabled title="Ödendi"' : r.talimat && r.talimat.durum !== 'İptal' && r.talimat.durum !== 'Gerçekleşti' ? `disabled title="${esc(r.talimat.no)} talimatında (${esc(r.talimat.durum)})"` : cikmis.length ? `disabled title="Belge plan dışı çıkmış: ${esc(cikmis.map(x => x.no + ' ' + x.akibet + (x.ibrazYeri ? ' → ' + x.ibrazYeri : '')).join(', '))} · Takip Paneli'nde kontrol edin"` : belgesiz ? `disabled title="Talimat için önce portföyden ${esc(r.yontem.toLocaleLowerCase('tr-TR'))} seçin"` : 'title="Talimat için seç"'}></td>`;
    const sira = `<td>${r.sira ?? ''}</td>`, plan = `<td><input type="date" class="kn-alan kn-tarih" data-alan="U_PlanlananTarih" value="${r.planlanan || ''}"></td>`;
    const kurum = `<td class="sol kurum" title="${esc(r.kurum)}${r.urun ? ' · ' + esc(r.urun) : ''}">${esc(r.kurum)}${av ? ` <span class="kn-avans" title="Nakit avans ${esc(av.no)} · ${esc(av.talepDurumu)} · ${esc(av.odemeDurumu)}">AVANS</span>` : ''}<small>${esc(r.urun || '')}${av ? ` · <a href="${esc(av.link)}" target="_blank">${esc(av.no)}</a> ${esc(av.odemeDurumu || '')}` : ''}</small></td>`;
    const firma = `<td class="sol firma" title="Fatura sahibi: ${esc(r.faturaSahibi)} · Ödeyen: ${esc(r.odeyenSirket)}"><b>${esc(r.faturaSahibi)}</b><small>${r.odeyenSirket && r.odeyenSirket !== r.faturaSahibi ? 'ödeyen: ' + esc(r.odeyenSirket) : 'ödeyen: aynı şirket'}</small></td>`;
    const belgeNo = `<td class="sol" title="${esc(r.faturaNo || '')} · ${esc(r.belgeTipi || '')}">${esc(r.faturaNo || '') || bos}<small>${esc(r.belgeTipi || '')}</small></td>`;
    const tl = paraTL(r);
    const durum = `<td><select class="kn-alan kn-durum" data-alan="U_Durum">${V.durumlar.map(d => `<option ${d === r.durum ? 'selected' : ''}>${esc(d)}</option>`).join('')}</select></td>`;
    const odenme = `<td>${tarih(r.odenme)}</td>`, odenen = `<td class="para">${r.odenen ? para(r.odenen) + (r.odenenPB && r.odenenPB !== 'TRY' ? `<small>${esc(r.odenenPB)}</small>` : '') : '—'}</td>`;
    const talimat = `<td>${r.talimat ? talimatRozet(r.talimat) : bos}</td>`;
    const not = `<td class="sol not" title="${esc(r.notlar || '')}">${esc(r.notlar || '')}</td>`, islem = `<td><button type="button" class="btn btn-light btn-sm py-0 px-1 border kn-ac" title="Detay / düzenle"><i class="fas fa-pen"></i></button></td>`;
    if (p === 'tumu') return [sec, sira, plan, kurum, firma, belgeNo, `<td>${yontemRozet(r.yontem)}</td>`, tl, durum, odenme, odenen, `<td>${r.mutabakat ? rozet(r.mutabakat, r.mutabakat === 'Mutabık' ? 'ok' : 'hata') : bos}</td>`, talimat, not, islem];
    if (p === 'nakit') return [sec, sira, plan, kurum, firma, belgeNo, tl, `<td>${alan(r, 'U_Kasa', a.kasa, 'select', V.kasalar)}</td>`, `<td>${alan(r, 'U_MakbuzNo', a.makbuzNo)}</td>`, `<td>${alan(r, 'U_TeslimAlan', a.teslimAlan)}</td>`, durum, odenme, odenen, talimat, islem];
    if (p === 'havale') return [sec, sira, plan, kurum, firma, belgeNo, tl, `<td>${alan(r, 'U_BankaHesabi', a.bankaHesabi, 'select', V.bankaHesaplari, o => o.split(' · ')[0] + ' · ' + (o.split(' · ')[1] || '').slice(-7))}</td>`, `<td>${alan(r, 'U_AliciIban', a.aliciIban)}</td>`, `<td>${alan(r, 'U_DekontNo', a.dekontNo)}</td>`, `<td><button type="button" class="btn btn-light btn-sm py-0 px-1 border kn-ac" title="Dekontlar"><i class="fas fa-paperclip"></i> ${(r.dekontlar || []).length}</button></td>`, durum, odenme, talimat, islem];
    if (p === 'kredi') return [sec, sira, plan, kurum, firma, belgeNo, tl, `<td>${alan(r, 'U_Kart', a.kart, 'select', V.kartlar)}</td>`, `<td>${alan(r, 'U_Taksit', a.taksit, 'select', V.taksitler)}</td>`, `<td>${alan(r, 'U_EkstreDonemi', a.ekstreDonemi)}</td>`, durum, odenme, odenen, talimat, islem];
    if (p === 'cek' || p === 'senet') return [sec, sira, plan, kurum, firma, tl, durum,
      `<td class="sol">${yigin(b, x => `<b>${esc(x.no)}</b> <span class="text-muted">· ${esc(x.banka)}</span>${x.sahip && x.sahip !== r.odeyenSirket ? ` <small class="d-inline text-primary">(${esc(x.sahip)} portföyü)</small>` : ''}`)}</td>`,
      `<td class="sol">${yigin(b, x => esc(x.asilBorclu) + (x.kimdenGeldi && x.kimdenGeldi !== x.asilBorclu ? ` <span class="text-muted">· ${esc(x.kimdenGeldi)}</span>` : ''))}</td>`,
      `<td>${yigin(b, x => tarih(x.vade))}</td>`, `<td class="para">${yigin(b, x => para(x.tutar))}</td>`,
      `<td class="sol wrap">${yigin(b, x => akibetRozet(x.akibet) + ' ' + kontrolRozet(x.kontrol, x) + (x.dolasim ? `<span class="kn-dolasim d-block" title="Grup içi dolaşım planı: ${esc(x.dolasim)} · ${x.gerceklesen}/${x.adimToplam} adım gerçekleşti"><i class="fas fa-route"></i> ${esc(x.dolasim)} (${x.gerceklesen}/${x.adimToplam})</span>` : ''))}</td>`,
      `<td class="sol wrap">${b.map(x => `<span class="yb">${belgeCikmis(x) && !x.adimToplam ? `<span class="text-muted" title="Belge çıkmış (${esc(x.akibet)}${x.ibrazYeri ? ' → ' + esc(x.ibrazYeri) : ''}); işlem yapılamaz">${esc(x.akibet)}</span>` : `<button type="button" class="btn btn-link btn-sm p-0 kn-dolasim-ac" data-cek="${x.cekDocEntry}" title="${x.adimToplam ? 'Dolaşım planı: ' + esc(x.dolasim) + ' (' + x.gerceklesen + '/' + x.adimToplam + ' adım) · adım gerçekleştir' : 'Grup içi dolaşım planı kur ve uygunluk simülasyonunu gör'}"><i class="fas fa-route"></i> Dolaşım</button> <button type="button" class="btn btn-link btn-sm p-0 text-danger kn-belge-kaldir" data-cek="${x.cekDocEntry}" ${x.akibet !== 'Planlandı' || x.gerceklesen > 0 ? 'disabled' : ''} title="Planı kaldır: belge portföye döner"><i class="fas fa-times"></i></button>`}</span>`).join('')}${r.durum === 'ÖDENDİ' ? '' : `<span class="yb"><button type="button" class="btn btn-primary btn-sm py-0 px-1 kn-belge-sec" style="font-size:.62rem" title="Portföydeki belgeleri gör ve seç (vade, tutar, banka süzgeçleri; başka grup şirketinin portföyü)"><i class="fas fa-plus"></i> ${b.length ? 'Ekle' : 'Belge Seç'}</button></span>`}</td>`, talimat, islem];
    if (p === 'mahsup' || p === 'temlik' || p === 'yansitma') {
      const kayit = g ? `<td class="sol"><a href="${esc(g.link)}" target="_blank" class="fw-bold" title="${esc(g.tur)} kaydını modülde aç">${esc(g.no)}</a><small>${g.bulundu === false ? '<span class="text-danger">kayıt bulunamadı</span>' : tarih(g.tarih)}</small></td>` : `<td class="sol">${bos}</td>`;
      const tur = g ? `<td class="sol" title="${esc(g.baslik || '')} · ${esc(g.taraflar || '')}">${esc(g.baslik || '')}<small>${esc(g.taraflar || '')}</small></td>` : `<td class="sol">${bos}</td>`;
      const mtutar = `<td class="para">${g ? para(g.tutar) + (g.pb && g.pb !== 'TRY' ? `<small>${esc(g.pb)}</small>` : '') : '—'}</td>`;
      const mdurum = g ? `<td class="sol" title="${esc(g.durum || '')} · ${esc((p === 'temlik' && g.imza ? 'imza ' + g.imza + ' · ' : '') + (g.muhasebe || ''))}">${modulRozet(g.durum)}<small>${esc((p === 'temlik' && g.imza ? 'imza ' + g.imza + ' · ' : '') + (g.muhasebe || ''))}</small></td>` : `<td>${bos}</td>`;
      const kontrol = `<td>${g && g.kontrol ? rozet(g.kontrol, /Uyumlu|UYUMLU/.test(g.kontrol) ? 'ok' : 'hata') : bos}</td>`;
      const aksiyon = `<td class="sol"><button type="button" class="btn btn-primary btn-sm py-0 px-1 kn-grup-sec" data-tur="${SEKME[p]}" style="font-size:.62rem" title="${g ? 'Kaydı değiştir' : 'Modülden kayıt bağla'}"><i class="fas fa-link"></i> ${g ? 'Değiştir' : 'Kayıt Seç'}</button>${g ? ` <button type="button" class="btn btn-link btn-sm p-0 text-danger kn-grup-kaldir" title="Kayıt bağını kaldır"><i class="fas fa-times"></i></button>` : ''}</td>`;
      return [sec, sira, plan, kurum, firma, tl, durum, kayit, tur, mtutar, mdurum, kontrol, aksiyon, talimat, islem];
    }
    return [sec, sira, plan, kurum, firma, belgeNo, tl, `<td>${esc(r.islemTipi || '')}</td>`, `<td>${esc(r.belgeTipi || '')}</td>`, durum, odenme, odenen, talimat, not, islem];
  }
  function cizTablo(p, L) {
    const t = $('#knTablo-' + p); if (!t) return;
    const R = p === 'tumu' ? L : L.filter(r => r.yontem === SEKME[p]);
    const kol = KOL[p], topW = kol.reduce((s, k) => s + k.w, 0);
    let cg = t.querySelector('colgroup'); if (!cg) { cg = document.createElement('colgroup'); t.insertBefore(cg, t.tHead); }
    cg.innerHTML = kol.map(k => `<col style="width:${(k.w / topW * 100).toFixed(2)}%">`).join('');
    t.tHead.innerHTML = '<tr>' + kol.map((k, i) => i === 0 ? `<th><input type="checkbox" class="form-check-input kn-sec-hepsi" title="Tümünü seç"></th>` : `<th class="${/Tutar|Toplam|Ödenen/.test(k.ad) ? 's' : ''}" title="${esc(k.ad)}">${esc(k.ad)}</th>`).join('') + '</tr>';
    t.tBodies[0].innerHTML = R.length ? R.map(r => `<tr class="tik ${DURUM_SINIF[r.durum] || ''}" data-doc="${r.docEntry}" data-db="${esc(r.dbName)}">${hucreler(r, p).join('')}</tr>`).join('') : `<tr><td colspan="${kol.length}" class="na-bos text-center">Bu sekmede filtreye uyan kayıt yok.${p !== 'tumu' ? ' Yeni Ödeme ile "' + esc(SEKME[p]) + '" yöntemli kayıt açabilirsiniz.' : ''}</td></tr>`;
    const toplam = R.reduce((s, r) => s + r.tutarTL, 0), odenen = R.filter(r => r.durum === 'ÖDENDİ').reduce((s, r) => s + r.tutarTL, 0);
    const tlIdx = kol.findIndex(k => k.ad === 'Tutar (₺)');
    t.tFoot.innerHTML = R.length ? `<tr class="toplam"><td colspan="${tlIdx}" class="text-end">TOPLAM (₺)</td><td class="para">${para(toplam)}</td><td colspan="${kol.length - tlIdx - 1}" class="sol">ödenen ${para(odenen)} · kalan ${para(toplam - odenen)} · ${R.length} kayıt</td></tr>` : '';
    $('#knNot-' + p).textContent = `${R.length} kayıt · ${para(toplam)} ₺ · satır içi alanlar anında kaydedilir · satır seçip talimat oluşturun`;
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
    $('#knGunluk tbody').innerHTML = G.length ? G.map(g => `<tr class="tik" data-doc="${g.docEntry}" data-db="${esc(g.dbName)}"><td>${esc(g.tarih)}</td><td>${esc(g.kullanici)}</td><td>${esc(g.sirket)}</td><td>#${g.docEntry}</td><td>${rozet(g.tip, /Sil|Kaldır|Hata/.test(g.tip) ? 'hata' : /Ödeme|Planlandı|Bağlandı|Talimat|Avans/.test(g.tip) ? 'ok' : 'bilgi')}</td><td class="sol">${esc(g.aciklama)}</td></tr>`).join('') : '<tr><td colspan="6" class="na-bos">Günlük kaydı yok.</td></tr>';
    $('#knGunlukNot').textContent = `${G.length} kayıt · son 300 · satıra tıklayınca ödeme kaydı`;
  }

  // ---------- TALİMATLAR
  const talimatDurumRozet = d => rozet(d, d === 'Gerçekleşti' ? 'ok' : d === 'İptal' ? 'hata' : d === 'Hazırlandı' ? 'bekle' : 'bilgi');
  function cizTalimatlar() {
    const T = V.talimatlar || [], tb = $('#knTalimatlar tbody');
    tb.innerHTML = T.length ? T.map(t => `<tr class="tik tl-satir" data-no="${esc(t.no)}"><td class="sol"><b>${esc(t.no)}</b></td><td>${esc(t.tarih)}</td><td class="sol">${esc(t.baslik)}<small>${yontemRozet(t.yontem)}</small></td><td class="sol fw-bold">${esc(t.sirket)}</td><td class="sol" title="${esc(t.bankaHesabi || t.kasa || '')}">${esc(t.bankaHesabi || t.kasa || '—')}</td><td class="para">${t.satirSayisi}</td><td class="para">${para(t.toplamTL)}</td><td>${talimatDurumRozet(t.durum)}</td><td class="sol">${esc(t.hazirlayan)}${t.onaylayan ? ' · ' + esc(t.onaylayan) : ''}<small>${t.gerceklesmeTarihi ? 'gerçekleşme ' + esc(t.gerceklesmeTarihi) : t.iletimTarihi ? 'iletim ' + esc(t.iletimTarihi) : t.onayTarihi ? 'onay ' + esc(t.onayTarihi) : ''}</small></td>
      <td class="sol" style="white-space:normal"><a class="btn btn-light btn-sm py-0 px-1 border" href="${esc(t.link)}" target="_blank" title="Yazdırılabilir talimat" style="font-size:.62rem"><i class="fas fa-print"></i> Yazdır</a> ${t.sonrakiDurum ? `<button type="button" class="btn btn-primary btn-sm py-0 px-1 kn-tl-durum" data-no="${esc(t.no)}" data-durum="${esc(t.sonrakiDurum)}" style="font-size:.62rem"><i class="fas fa-arrow-right"></i> ${esc(t.sonrakiDurum)}</button>` : ''} ${!t.kapandi ? `<button type="button" class="btn btn-link btn-sm p-0 text-danger kn-tl-durum" data-no="${esc(t.no)}" data-durum="İptal" title="Talimatı iptal et"><i class="fas fa-times"></i></button>` : ''}</td></tr>
      <tr class="tl-detay" data-no="${esc(t.no)}" style="display:none"><td colspan="10" class="sol"><div class="kn-liste">${t.satirlar.map(s => `<div class="satir"><span class="text-muted">#${s.DocEntry ?? s.docEntry}</span><b>${esc(s.kurum)}</b><span>${esc(s.faturaSahibi)}${s.odeyenSirket !== s.faturaSahibi ? ' · ödeyen ' + esc(s.odeyenSirket) : ''}</span><span>${esc(s.faturaNo || '')}</span><span class="fw-bold">${para(s.tutar)} ${esc(s.pb === 'TRY' ? '₺' : s.pb)}</span><span class="text-muted">${esc(s.aciklama)}</span>${s.belgeler.length ? `<span class="text-primary">${s.belgeler.map(b => esc(b.No || b.no) + (b.Dolasim || b.dolasim ? ' (' + esc(b.Dolasim || b.dolasim) + ')' : '')).join(', ')}</span>` : ''}${s.avansNo ? `<span class="kn-avans">AVANS ${esc(s.avansNo)}</span>` : ''}</div>`).join('')}<div class="na-mini">${t.gunluk.map(g => `${esc(g.tarih)} ${esc(g.Islem || g.islem)} ${esc(g.Kullanici || g.kullanici)} ${esc(g.Aciklama || g.aciklama || '')}`).join(' · ')}</div></div></td></tr>`).join('') : '<tr><td colspan="10" class="na-bos text-center">Henüz talimat yok. Yöntem sekmelerinde satırları seçip "Seçilenlerden Talimat Oluştur" düğmesine basın.</td></tr>';
    $('#knTalimatNot').textContent = `${T.length} talimat · ${T.filter(t => !t.kapandi).length} açık · toplam ${para(T.filter(t => t.durum !== 'İptal').reduce((s, t) => s + t.toplamTL, 0))} ₺ · satıra tıklayınca satırlar`;
  }
  kok.addEventListener('click', e => { const tr = e.target.closest('tr.tl-satir'); if (!tr || e.target.closest('a,button')) return; const d = $(`tr.tl-detay[data-no="${tr.dataset.no}"]`); if (d) d.style.display = d.style.display === 'none' ? '' : 'none'; });
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.kn-tl-durum'); if (!b) return; const no = b.dataset.no, durum = b.dataset.durum; let not = '';
    if (durum === 'İptal') { not = prompt(`${no} iptal nedeni:`, ''); if (not === null) return; } else if (!confirm(`${no} → ${durum}?` + (durum === 'Gerçekleşti' ? ' Satırlar ÖDENDİ olur; çekler dolaşım planına göre ciro / ibraz edilir, avanslar teslim edilmiş sayılır.' : ''))) return;
    const d = await post('/Rapor79/TalimatDurum', { No: no, Durum: durum, Not: not }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } mesaj(durum === 'Gerçekleşti' ? 'success' : 'info', esc(d.message)); await yukle(true); sekme('talimat');
  });
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-talimat-ac'); if (!b) return; e.stopPropagation(); sekme('talimat'); const tr = $(`tr.tl-satir[data-no="${b.dataset.no}"]`); if (tr) { tr.scrollIntoView({ block: 'center' }); const d = $(`tr.tl-detay[data-no="${b.dataset.no}"]`); if (d) d.style.display = ''; } });
  kok.addEventListener('change', e => { const h = e.target.closest('.kn-sec-hepsi'); if (h) { $$('.kn-sec:not(:disabled)', h.closest('table')).forEach(c => { c.checked = h.checked; c.closest('tr').classList.toggle('secili', h.checked); }); return; } const c = e.target.closest('.kn-sec'); if (c) c.closest('tr').classList.toggle('secili', c.checked); });
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.kn-talimat-olustur'); if (!b) return; const t = $('#knTablo-' + b.dataset.p); const ids = $$('.kn-sec:checked', t).map(c => +c.closest('tr').dataset.doc);
    if (!ids.length) { mesaj('warning', 'Önce tablodan satır seçin (ilk sütundaki kutular).'); return; }
    const secilen = ids.map(satirBul).filter(Boolean); const ozet = {}; secilen.forEach(r => { const k = (r.odeyenSirket || r.sirket) + ' · ' + r.yontem; ozet[k] = (ozet[k] || 0) + 1; });
    if (!confirm(`${ids.length} satırdan talimat hazırlansın mı?\n` + Object.entries(ozet).map(([k, n]) => `• ${k}: ${n} satır`).join('\n') + '\n(ödeyen şirket + yöntem bazında ayrı talimat; çek satırlarında seçili belgeler ve dolaşım planı talimata girer)')) return;
    const d = await post('/Rapor79/TalimatOlustur', { DbName: F.sirket || secilen[0].dbName, DocEntryler: ids }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    mesaj('success', '<i class="fas fa-file-contract me-1"></i>' + esc(d.message) + (d.talimatlar || []).map(t => ` <a href="${esc(t.link)}" target="_blank" class="ms-1"><i class="fas fa-print"></i> ${esc(t.no)} yazdır</a>`).join(''));
    await yukle(true); sekme('talimat');
  });

  // ---------- sekmeler / filtreler
  function sekme(p) {
    if (!$(`#knSekmeler button[data-p="${p}"]`)) p = 'tumu';
    $$('#knSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('#kn > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    ciz();
    try { const u = new URL(location.href); if (p === 'tumu') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); if (F.sirket) u.searchParams.set('sirket', F.sirket); else u.searchParams.delete('sirket'); history.replaceState(null, '', u); } catch (e) { }
  }
  $$('#knSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  $('#knSirket').addEventListener('change', () => { F.sirket = $('#knSirket').value; yukle(); });
  $('#knBas').addEventListener('change', () => { F.bas = $('#knBas').value; yukle(); }); $('#knBit').addEventListener('change', () => { F.bit = $('#knBit').value; yukle(); });
  $$('.kn-hizli button').forEach(b => b.addEventListener('click', () => { const d = new Date(); const bit = new Date(); bit.setDate(bit.getDate() + +b.dataset.gun); F.bas = d.toISOString().slice(0, 10); F.bit = bit.toISOString().slice(0, 10); $('#knBas').value = F.bas; $('#knBit').value = F.bit; yukle(); }));
  $('#knDurum').addEventListener('change', () => { F.durum = $('#knDurum').value; ciz(); }); $('#knSatinalmaci').addEventListener('change', () => { F.satinalmaci = $('#knSatinalmaci').value; ciz(); });
  $('#knAra').addEventListener('input', () => { F.ara = $('#knAra').value; ciz(); });
  $('#knTemizle').addEventListener('click', () => { F.durum = ''; F.satinalmaci = ''; F.ara = ''; $('#knDurum').value = ''; $('#knSatinalmaci').value = ''; $('#knAra').value = ''; ciz(); });
  $('#knKpi').addEventListener('click', e => { const el = e.target.closest('[data-durum],[data-p]'); if (!el) return; if (el.dataset.p) { sekme(el.dataset.p); return; } F.durum = F.durum === el.dataset.durum ? '' : el.dataset.durum; $('#knDurum').value = F.durum; ciz(); });
  $('#knExcel').addEventListener('click', () => { const p = aktifSekme(), t = $('#knTablo-' + p) || (p === 'talimat' ? $('#knTalimatlar') : $('#knBakiye')); if (!t) return; const bas = $$('thead th', t).map(x => x.textContent.trim()); const rows = $$('tbody tr', t).filter(r => !r.querySelector('.na-bos') && !r.classList.contains('tl-detay')).map(r => $$('td', r).map(td => { const i = td.querySelector('input:not([type=checkbox]),select'); return i ? i.value : td.textContent.trim().replace(/\s+/g, ' '); })); csvIndir('kasa_nakit_akisi_' + p, bas, rows); });

  // ---------- satır içi kayıt
  kok.addEventListener('change', async e => {
    const inp = e.target.closest('.kn-alan'); if (!inp) return; const tr = inp.closest('tr'); if (!tr) return;
    const d = await post('/Rapor79/AlanKaydet', { DbName: tr.dataset.db, DocEntry: +tr.dataset.doc, Alan: inp.dataset.alan, Deger: inp.value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message || 'Kaydedilemedi.'); return; }
    satirYerlestir(d.row); inp.classList.add('ok'); setTimeout(() => inp.classList.remove('ok'), 1200);
    if (d.message && d.message.length > 12) mesaj('info', esc(d.message));
    if (inp.dataset.alan === 'U_Durum' || inp.dataset.alan === 'U_PlanlananTarih') ciz(); else { const L = satirlar(); kpi(L); }
  });
  kok.addEventListener('click', e => {
    const tr = e.target.closest('tr.tik'); if (!tr || !kok.contains(tr) || tr.classList.contains('tl-satir')) return;
    if (e.target.closest('input,select,a,button')) return;
    const r = satirBul(+tr.dataset.doc); if (r) drawerAc(r);
  });

  // ---------- PORTFÖYDEN BELGE SEÇİMİ (Çek / Senet)
  let secim = { r: null, belge: 'Çek', liste: [], sirket: '' };
  const kbSayac = () => { const c = $$('.kb-chk:checked'); let t = 0; c.forEach(x => t += +x.dataset.tutar || 0); $('#kbSayi').textContent = c.length; $('#kbToplam').textContent = para(t); const st = secim.r ? secim.r.tutar : 0; $('#kbFark').textContent = c.length && st ? `(satır ${para(st)} · fark ${t - st >= 0 ? '+' : ''}${para(t - st)}; satır tutarı belge toplamı olur)` : ''; };
  const vadeRozet = v => rozet(v || '', v === 'Vadesi Geçti' ? 'hata' : v === 'Bugün' || v === 'Bu Hafta' ? 'bekle' : v === 'Bu Ay' ? 'bilgi' : 'notr');
  function kbSuz() {
    const q = ($('#kbAra').value || '').toLocaleLowerCase('tr-TR'), vade = $('#kbVade').value, min = +$('#kbMin').value || 0, max = +$('#kbMax').value || 0, banka = $('#kbBanka').value, planli = $('#kbPlanli').checked, sira = $('#kbSira').value, bugun = V.bugun;
    const gun = s => Math.round((new Date(s) - new Date(bugun)) / 86400000);
    let l = secim.liste.filter(x => (planli || x.secilebilir) && (!q || [x.no, x.asilBorclu, x.kimdenGeldi, x.banka, x.musteriKod].join(' ').toLocaleLowerCase('tr-TR').includes(q)) && (!min || x.tutar >= min) && (!max || x.tutar <= max) && (!banka || x.banka === banka)
      && (!vade || (vade === 'gecmis' ? gun(x.vade) < 0 : vade === 'uzak' ? gun(x.vade) > 60 : gun(x.vade) >= 0 && gun(x.vade) <= +vade)));
    const hedef = secim.r ? secim.r.tutar : 0;
    if (sira === 'tutarAzalan') l.sort((a, b) => b.tutar - a.tutar); else if (sira === 'tutarArtan') l.sort((a, b) => a.tutar - b.tutar); else if (sira === 'yakin') l.sort((a, b) => Math.abs(a.tutar - hedef) - Math.abs(b.tutar - hedef)); else l.sort((a, b) => a.vade.localeCompare(b.vade));
    return l;
  }
  function kbCiz() {
    const l = kbSuz(), secili = new Set($$('.kb-chk:checked').map(c => +c.value));
    $('#kbGovde').innerHTML = l.length ? l.map(x => `<tr class="${x.secilebilir ? '' : 'text-muted'}"><td><input type="checkbox" class="form-check-input kb-chk" value="${x.docEntry}" data-tutar="${x.tutar}" ${x.secilebilir ? '' : 'disabled'} ${secili.has(x.docEntry) ? 'checked' : ''}></td><td class="fw-bold">${esc(x.no)}</td><td>${esc(x.banka)} / ${esc(x.sube)}</td><td>${esc(x.asilBorclu)}</td><td>${esc(x.kimdenGeldi)}<br><small class="text-muted">${esc(x.musteriKod)}</small></td><td>${tarih(x.vade)}</td><td>${vadeRozet(x.vadeDurumu)}</td><td class="text-end fw-bold">${para(x.tutar)} ₺</td><td>${tarih(x.portfoyGiris)}</td><td>${x.secilebilir ? '<span class="rt-durum rt-ok">Portföyde</span>' : akibetRozet(x.akibet) + (x.planlananTedarikci ? `<br><small>${esc(x.planlananTedarikci)}${x.planDocEntry ? ' · #' + x.planDocEntry : ''}</small>` : x.kullanim !== 'Kullanılabilir' ? `<br><small>${esc(x.kullanim)}</small>` : '')}</td></tr>`).join('') : `<tr><td colspan="10" class="text-center text-muted py-3">Süzgece uyan belge yok.</td></tr>`;
    const hepsi = secim.liste, uygun = hepsi.filter(x => x.secilebilir); $('#kbOzet').textContent = `${esc(secim.sirket)} portföyü: ${uygun.length} seçilebilir ${secim.belge.toLocaleLowerCase('tr-TR')} · ${para(uygun.reduce((s, x) => s + x.tutar, 0))} ₺ · toplam ${hepsi.length} belge (planlı ${hepsi.length - uygun.length}) · listede ${l.length}`;
    $('#kbHepsi').checked = false; kbSayac();
  }
  async function kbYukle() {
    $('#kbGovde').innerHTML = '<tr><td colspan="10" class="text-center text-muted py-3"><i class="fas fa-spinner fa-spin"></i> Portföy getiriliyor…</td></tr>';
    try { const d = await getJ(`/Rapor79/SecilebilirBelgeler?dbName=${encodeURIComponent(secim.r.dbName)}&belge=${encodeURIComponent(secim.belge)}&sirket=${encodeURIComponent($('#kbSirket').value)}&hepsi=true`); secim.liste = d.data || []; secim.sirket = d.sirket || ''; $('#kbBanka').innerHTML = '<option value="">Tümü</option>' + [...new Set(secim.liste.map(x => x.banka))].sort().map(b => `<option>${esc(b)}</option>`).join(''); kbCiz(); } catch (x) { $('#kbGovde').innerHTML = '<tr><td colspan="10" class="text-center text-danger py-3">Ağ hatası.</td></tr>'; }
  }
  kok.addEventListener('click', e => {
    const b = e.target.closest('.kn-belge-sec'); if (!b) return; const tr = b.closest('tr'); const r = satirBul(+tr.dataset.doc); if (!r) return;
    secim = { r, belge: r.yontem === 'Senet' ? 'Senet' : 'Çek', liste: [], sirket: '' };
    $('#kbBaslik').textContent = secim.belge + ' Seç · Portföy'; $('#kbKurum').textContent = r.kurum; $('#kbBilgi').innerHTML = `Fatura sahibi <b>${esc(r.faturaSahibi)}</b> · ödeyen <b>${esc(r.odeyenSirket)}</b> · satır tutarı <b>${para(r.tutar)} ${esc(r.pb)}</b>. Seçilen belgeler Çek Akıbeti Raporu'nda "Planlandı" olur; başka grup şirketinin portföyünden seçilirse ödeyen şirket o şirket olur ve dolaşım planı önerilir.`;
    const sel = $('#kbSirket'); sel.innerHTML = V.sirketler.map(s => `<option value="${esc(s.dbName)}" ${s.ad === r.odeyenSirket || (!V.sirketler.some(x => x.ad === r.odeyenSirket) && s.dbName === r.dbName) ? 'selected' : ''}>${esc(s.ad)}</option>`).join('');
    $('#kbAra').value = ''; $('#kbVade').value = ''; $('#kbMin').value = ''; $('#kbMax').value = ''; $('#kbPlanli').checked = false; $('#kbSira').value = 'vade'; kbSayac();
    modalAc($('#knBelgeModal')); kbYukle();
  });
  ['#kbAra', '#kbVade', '#kbMin', '#kbMax', '#kbBanka', '#kbPlanli', '#kbSira'].forEach(s => $(s).addEventListener(s === '#kbAra' || s === '#kbMin' || s === '#kbMax' ? 'input' : 'change', kbCiz));
  $('#kbSirket').addEventListener('change', kbYukle);
  $('#kbGovde').addEventListener('change', kbSayac); $('#kbHepsi').addEventListener('change', () => { $$('.kb-chk:not(:disabled)').forEach(c => c.checked = $('#kbHepsi').checked); kbSayac(); });
  $('#kbYakin').addEventListener('click', () => {
    const hedef = secim.r ? secim.r.tutar : 0; if (!hedef) return; const l = kbSuz().filter(x => x.secilebilir).sort((a, b) => b.tutar - a.tutar); let kalan = hedef, sec = [];
    l.forEach(x => { if (x.tutar <= kalan * 1.05 && kalan > 0) { sec.push(x.docEntry); kalan -= x.tutar; } });
    if (!sec.length && l.length) { const en = l.reduce((a, b) => Math.abs(a.tutar - hedef) < Math.abs(b.tutar - hedef) ? a : b); sec.push(en.docEntry); }
    $$('.kb-chk').forEach(c => c.checked = sec.includes(+c.value)); kbSayac(); mesaj('info', `${sec.length} belge seçildi (satır tutarına en yakın toplam).`);
  });
  $('#kbOnayla').addEventListener('click', async () => {
    const ids = $$('.kb-chk:checked').map(c => +c.value); if (!ids.length) { mesaj('warning', 'En az bir belge seçin.'); return; }
    const d = await post('/Rapor79/BelgePlanla', { DbName: secim.r.dbName, DocEntry: secim.r.docEntry, CekDocEntryler: ids }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message || 'Planlanamadı.'); return; }
    modalKapat($('#knBelgeModal')); satirYerlestir(d.row); mesaj('success', '<i class="fas fa-check me-1"></i>' + esc(d.message)); ciz();
    const r = d.row, b = (r.belgeler || []).find(x => ids.includes(x.cekDocEntry)); if (b && b.sahip && b.sahip !== r.faturaSahibi && !b.adimToplam) setTimeout(() => dolasimAc(r, b), 400);
  });
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.kn-belge-kaldir'); if (!b || b.disabled) return; const tr = b.closest('tr');
    if (!confirm('Belgenin planı kaldırılsın mı? Belge portföye döner (Çek Akıbeti: Portföyde).')) return;
    const d = await post('/Rapor79/BelgePlanKaldir', { DbName: tr.dataset.db, DocEntry: +tr.dataset.doc, CekDocEntry: +b.dataset.cek }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } satirYerlestir(d.row); mesaj('info', esc(d.message)); ciz();
  });

  // ---------- GRUP İÇİ DOLAŞIM PLANI (zincir + uygunluk simülasyonu)
  let D = { r: null, b: null, zincir: [], sim: null };
  const dSirketler = () => (V.grupSirketleri || V.sirketler.map(s => s.ad));
  const adimAlan = (a, k) => a[k] ?? a[k.charAt(0).toLowerCase() + k.slice(1)];
  // kayıtlı planda zincir = henüz gerçekleşmemiş grup içi adımların alan şirketleri (simülasyon belgenin şu anki sahibinden başlar); gerçekleşenler plan kartında
  const kalanZincir = b => (b.adimlar || []).filter(a => adimAlan(a, 'Tur') === 'Grup İçi Ciro' && adimAlan(a, 'Durum') !== 'Gerçekleşti').map(a => adimAlan(a, 'Alan'));
  function dolasimBaslikCiz() {
    const r = D.r, b = D.b;
    $('#kdBelge').textContent = `${b.no} · ${para(b.tutar)} ₺ · şu an ${b.sahip} portföyünde`;
    $('#kdBilgi').innerHTML = [['Belge', `${b.belge} ${b.no} · ${b.banka}`], ['Keşideci / Kimden', `${b.asilBorclu} / ${b.kimdenGeldi}`], ['Vade · Tutar', `${tarih(b.vade)} · ${para(b.tutar)} ₺`], ['Şu Anki Sahip', (b.sahip || '—') + (b.kaynakSirket && b.kaynakSirket !== b.sahip ? ` (ilk portföy ${b.kaynakSirket})` : '')], ['Fatura Sahibi · Ödeyen', `${r.faturaSahibi} · ${r.odeyenSirket}`], ['Tedarikçi (Ödenecek Cari)', r.kurum]].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    $('#kdBakiye tbody').innerHTML = (V.grupBakiyeleri || []).map(x => `<tr><td class="sol">${esc(x.borclu)}</td><td class="sol">${esc(x.alacakli)}</td><td class="para">${para(x.tutar)}</td></tr>`).join('');
  }
  function dolasimAc(r, b) {
    D = { r, b, zincir: b.adimToplam ? kalanZincir(b) : [], sim: null };
    dolasimBaslikCiz();
    $('#kdTedarikci').value = b.planlananTedarikci || r.kurum; $('#kdSonAdim').value = 'Tedarikçiye Ciro';
    const sonAdim = (b.adimlar || []).find(a => adimAlan(a, 'Tur') !== 'Grup İçi Ciro'); if (sonAdim) $('#kdSonAdim').value = adimAlan(sonAdim, 'Tur');
    dolasimZincirCiz(); dolasimPlanCiz(); modalAc($('#knDolasimModal'));
    if (!b.adimToplam && !D.zincir.length && b.sahip !== r.faturaSahibi) dolasimOner(); else dolasimSimule();
  }
  function dolasimZincirCiz() {
    const sahip = D.b.sahip, halkalar = [`<span class="halka"><i class="fas fa-building text-muted"></i> ${esc(sahip)}</span>`];
    D.zincir.forEach((c, i) => halkalar.push(`<span class="ok"><i class="fas fa-arrow-right"></i></span><span class="halka"><select data-i="${i}" class="kd-halka">${dSirketler().map(s => `<option ${s === c ? 'selected' : ''}>${esc(s)}</option>`).join('')}</select><button type="button" class="btn btn-link btn-sm p-0 text-danger kd-sil" data-i="${i}" title="Halkayı kaldır"><i class="fas fa-times"></i></button></span>`));
    halkalar.push(`<span class="ok"><i class="fas fa-arrow-right"></i></span><button type="button" class="btn btn-light btn-sm border" id="kdEkle" style="font-size:.66rem"><i class="fas fa-plus"></i> Şirket Ekle</button><span class="ok"><i class="fas fa-arrow-right"></i></span><span class="halka"><i class="fas ${$('#kdSonAdim').value === 'Bankaya İbraz' ? 'fa-building-columns' : 'fa-truck'} text-muted"></i> ${esc($('#kdSonAdim').value === 'Bankaya İbraz' ? 'Banka (tahsil)' : $('#kdTedarikci').value || 'Tedarikçi')}</span>`);
    $('#kdZincir').innerHTML = halkalar.join('');
    const kayitli = D.b.adimToplam > 0, basladi = kayitli && D.b.gerceklesen > 0; $('#kdIptal').style.display = kayitli && !basladi ? '' : 'none'; $('#kdKaydet').innerHTML = `<i class="fas fa-save me-1"></i>${kayitli ? 'Planı Güncelle' : 'Planı Kaydet'}`; $('#kdKaydet').disabled = basladi; $('#kdKaydet').title = basladi ? 'Gerçekleşmiş adımı olan plan değiştirilemez; kalan adımlar plan kartından gerçekleştirilir' : '';
    $$('#kdZincir select, #kdZincir button').forEach(x => x.disabled = basladi); $('#kdSonAdim').disabled = basladi; $('#kdTedarikci').disabled = basladi; $('#kdOner').disabled = basladi;
  }
  function dolasimPlanCiz() {
    const b = D.b, kart = $('#kdPlanKart'); if (!b.adimToplam) { kart.style.display = 'none'; return; } kart.style.display = '';
    const sonraki = (b.adimlar || []).find(a => adimAlan(a, 'Durum') === 'Planlandı');
    $('#kdAdimlar').innerHTML = (b.adimlar || []).map(a => { const d = adimAlan(a, 'Durum'), s = adimAlan(a, 'Sira'); return `<span class="adim ${d === 'Gerçekleşti' ? 'ok' : 'plan'}"><b>${s}.</b> ${esc(adimAlan(a, 'Eden'))} → ${esc(adimAlan(a, 'Alan'))} <small class="text-muted">${esc(adimAlan(a, 'Tur'))} · ${d === 'Gerçekleşti' ? 'gerçekleşti ' + tarih(adimAlan(a, 'GercekTarih')) + ' · ' + esc(adimAlan(a, 'BelgeNo')) : 'plan ' + tarih(adimAlan(a, 'PlanTarih'))}</small>${sonraki && adimAlan(sonraki, 'Sira') === s ? ` <button type="button" class="btn btn-primary btn-sm py-0 px-1 kd-gerceklestir" data-sira="${s}" style="font-size:.62rem" title="Ciro / ibraz kaydı atılır, grup içi bakiye güncellenir, Takip Paneli'ne düşer"><i class="fas fa-check"></i> Gerçekleştir</button>` : ''}</span>`; }).join('');
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
    if (d && d.success && d.zincir.length) { D.zincir = d.zincir; mesaj('info', `Öneri: ${D.b.sahip} → ${d.zincir.join(' → ')} (mevcut grup içi borçlar üzerinden).`); } else mesaj('warning', 'Fatura sahibine ulaşan borç zinciri bulunamadı; zinciri elle kurun.');
    dolasimZincirCiz(); dolasimSimule();
  }
  $('#kdZincir').addEventListener('change', e => { const s = e.target.closest('.kd-halka'); if (s) { D.zincir[+s.dataset.i] = s.value; dolasimSimule(); } });
  $('#kdZincir').addEventListener('click', e => { const sil = e.target.closest('.kd-sil'); if (sil) { D.zincir.splice(+sil.dataset.i, 1); dolasimZincirCiz(); dolasimSimule(); return; } if (e.target.closest('#kdEkle')) { const aday = dSirketler().find(s => s !== D.b.sahip && !D.zincir.includes(s)) || dSirketler()[0]; D.zincir.push(D.zincir.length ? aday : (D.r.faturaSahibi !== D.b.sahip ? D.r.faturaSahibi : aday)); dolasimZincirCiz(); dolasimSimule(); } });
  $('#kdSonAdim').addEventListener('change', () => { dolasimZincirCiz(); dolasimSimule(); }); $('#kdTedarikci').addEventListener('change', () => { dolasimZincirCiz(); dolasimSimule(); });
  $('#kdOner').addEventListener('click', dolasimOner); $('#kdSimule').addEventListener('click', dolasimSimule);
  $('#kdKaydet').addEventListener('click', async () => {
    const d = await post('/Rapor79/DolasimKaydet', { CekDocEntry: D.b.cekDocEntry, Zincir: D.zincir, SonAdim: $('#kdSonAdim').value, Tedarikci: $('#kdTedarikci').value, PlanDocEntry: D.r.docEntry }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } mesaj('success', '<i class="fas fa-route me-1"></i>' + esc(d.message)); if (d.row) satirYerlestir(d.row); D.b = d.belge; ciz(); dolasimBaslikCiz(); dolasimZincirCiz(); dolasimPlanCiz();
  });
  $('#kdAdimlar').addEventListener('click', async e => {
    const b = e.target.closest('.kd-gerceklestir'); if (!b) return; if (!confirm(`${b.dataset.sira}. adım gerçekleştirilsin mi? (ciro / ibraz kaydı atılır, grup içi bakiye güncellenir, Takip Paneli'ne düşer)`)) return;
    const d = await post('/Rapor79/DolasimAdim', { CekDocEntry: D.b.cekDocEntry, Sira: +b.dataset.sira }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } mesaj('success', esc(d.message)); D.b = d.belge; await yukle(true); const r = satirBul(D.r.docEntry); if (r) D.r = r; D.zincir = kalanZincir(D.b); dolasimBaslikCiz(); dolasimPlanCiz(); dolasimZincirCiz(); dolasimSimule();
    if (D.b.gerceklesen >= D.b.adimToplam) { mesaj('success', esc(d.message) + ' Dolaşım tamamlandı; satır ÖDENDİ.'); setTimeout(() => modalKapat($('#knDolasimModal')), 1200); }
  });
  $('#kdIptal').addEventListener('click', async () => { if (!confirm('Dolaşım planı iptal edilsin mi?')) return; const d = await post('/Rapor79/DolasimIptal', { CekDocEntry: D.b.cekDocEntry }).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (!d.success) { mesaj('danger', d.message); return; } mesaj('info', esc(d.message)); if (d.row) satirYerlestir(d.row); D.b = d.belge; D.zincir = []; ciz(); dolasimBaslikCiz(); dolasimZincirCiz(); dolasimPlanCiz(); dolasimSimule(); });
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-dolasim-ac'); if (!b) return; e.stopPropagation(); const tr = b.closest('tr'); const r = satirBul(+tr.dataset.doc); const belge = r && (r.belgeler || []).find(x => x.cekDocEntry === +b.dataset.cek); if (r && belge) dolasimAc(r, belge); });

  // ---------- grup içi kayıt bağlama (Mahsup / Temlik / Yansıtma)
  let grup = { r: null, tur: 'Mahsup', liste: [] };
  function kgCiz() {
    const q = ($('#kgAra').value || '').toLocaleLowerCase('tr-TR'); const l = grup.liste.filter(x => !q || [x.no, x.baslik, x.taraflar, x.durum].join(' ').toLocaleLowerCase('tr-TR').includes(q));
    $('#kgGovde').innerHTML = l.length ? l.map(x => `<tr><td><input type="radio" name="kgSec" class="form-check-input" value="${esc(x.no)}" ${x.bagli ? 'disabled title="başka satıra bağlı"' : ''}></td><td class="fw-bold">${esc(x.no)}${x.bagli ? '<br><small class="text-muted">bağlı</small>' : ''}</td><td>${tarih(x.tarih)}</td><td>${esc(x.baslik)}</td><td>${esc(x.taraflar)}</td><td class="text-end fw-bold">${para(x.tutar)} ${esc(x.pb === 'TRY' ? '₺' : x.pb)}</td><td>${modulRozet(x.durum)}</td><td>${esc(x.muhasebe || '')}</td><td><a href="${esc(x.link)}" target="_blank" title="Modülde aç"><i class="fas fa-arrow-up-right-from-square"></i></a></td></tr>`).join('') : `<tr><td colspan="9" class="text-center text-muted py-3">Modülde ${esc(grup.tur.toLocaleLowerCase('tr-TR'))} kaydı yok.</td></tr>`;
  }
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.kn-grup-sec'); if (!b) return; const tr = b.closest('tr'); const r = satirBul(+tr.dataset.doc); if (!r) return;
    grup = { r, tur: b.dataset.tur || r.yontem, liste: [] };
    $('#kgBaslik').textContent = grup.tur + ' Kaydı Seç'; $('#kgKurum').textContent = r.kurum; $('#kgBilgi').textContent = `${grup.tur} İşlemleri modülündeki kayıtlar (şirketin taraf olduğu, aktif) · seçilen kayıt satıra bağlanır`;
    $('#kgAra').value = ''; $('#kgGovde').innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3"><i class="fas fa-spinner fa-spin"></i> Kayıtlar getiriliyor…</td></tr>';
    modalAc($('#knGrupModal'));
    try { const d = await getJ(`/Rapor79/GrupIciKayitlar?dbName=${encodeURIComponent(r.dbName)}&tur=${encodeURIComponent(grup.tur)}`); grup.liste = d.data || []; kgCiz(); } catch (x) { $('#kgGovde').innerHTML = '<tr><td colspan="9" class="text-center text-danger py-3">Ağ hatası.</td></tr>'; }
  });
  $('#kgAra').addEventListener('input', kgCiz);
  $('#kgOnayla').addEventListener('click', async () => {
    const s = $('input[name="kgSec"]:checked'); if (!s) { mesaj('warning', 'Bir kayıt seçin.'); return; }
    const d = await post('/Rapor79/GrupIciBagla', { DbName: grup.r.dbName, DocEntry: grup.r.docEntry, Tur: grup.tur, No: s.value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message || 'Bağlanamadı.'); return; }
    modalKapat($('#knGrupModal')); satirYerlestir(d.row); mesaj('success', '<i class="fas fa-link me-1"></i>' + esc(d.message)); ciz();
  });
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.kn-grup-kaldir'); if (!b) return; const tr = b.closest('tr'); if (!confirm('Kayıt bağı kaldırılsın mı? (Modül kaydı silinmez.)')) return;
    const d = await post('/Rapor79/GrupIciKaldir', { DbName: tr.dataset.db, DocEntry: +tr.dataset.doc }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } satirYerlestir(d.row); mesaj('info', esc(d.message)); ciz();
  });

  // ---------- çekmece: detay / düzenleme / yeni
  const F_ALAN = (id, etiket, html, g) => `<div class="${g || ''}"><label class="form-label" for="${id}">${etiket}</label>${html}</div>`;
  const sel = (id, deger, secenekler, bosMu) => `<select class="form-select" id="${id}">${bosMu ? '<option value="">—</option>' : ''}${secenekler.map(o => `<option ${o === deger ? 'selected' : ''}>${esc(o)}</option>`).join('')}</select>`;
  const inp = (id, deger, tip) => `<input class="form-control" id="${id}" type="${tip || 'text'}" value="${esc(deger ?? '')}" ${tip === 'number' ? 'step="0.01"' : ''}>`;
  function drawerAc(r) {
    acik = r; const yeni = !r.docEntry; const a = r.alanlar || {}, g = r.grupIci, av = r.avans, sirketler = dSirketler();
    $('#knDBaslik').innerHTML = yeni ? '<i class="fas fa-plus me-1"></i>Yeni Ödeme Kaydı' : `${esc(r.kurum)} ${yontemRozet(r.yontem)} ${durumRozet(r.durum)} <span class="kn-pill">#${r.docEntry} · ${esc(r.sirket)}</span> ${r.talimat ? talimatRozet(r.talimat, true) : ''}`;
    $('#knDAksiyon').innerHTML = yeni ? '' : `<button type="button" class="btn btn-light btn-sm border" id="knDSil" style="font-size:.66rem"><i class="fas fa-trash me-1 text-danger"></i>Sil</button>`;
    const dbSec = yeni ? F_ALAN('knD_db', 'Kayıt Şirketi (Liste)', `<select class="form-select" id="knD_db">${V.sirketler.map(s => `<option value="${esc(s.dbName)}" ${s.dbName === (F.sirket || V.sirketler[0].dbName) ? 'selected' : ''}>${esc(s.ad)}</option>`).join('')}</select>`) : '';
    $('#knDIc').innerHTML = `
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-pen me-1"></i>Ödeme Bilgileri</span><span class="not">${yeni ? 'kaydet: bellek içi' : 'satır içi alanlar da tabloda düzenlenebilir'}</span></div><div class="na-kart-g">
        <div class="kn-form">
          ${dbSec}
          ${F_ALAN('knD_kurum', 'Ödenecek Cari (Tedarikçi) <span class="z">*</span>', `<input class="form-control" id="knD_kurum" list="knTedarikciler" value="${esc(r.kurum)}"><datalist id="knTedarikciler">${(V.tedarikciler || []).map(t => `<option value="${esc(t)}">`).join('')}</datalist>`, 'g2')}
          ${F_ALAN('knD_faturaSahibi', 'Fatura Sahibi Şirket', sel('knD_faturaSahibi', r.faturaSahibi || sirketAdi(F.sirket || V.sirketler[0].dbName), sirketler))}
          ${F_ALAN('knD_odeyen', 'Ödeyen Şirket', sel('knD_odeyen', r.odeyenSirket || sirketAdi(F.sirket || V.sirketler[0].dbName), sirketler))}
          ${F_ALAN('knD_yontem', 'Ödeme Yöntemi', sel('knD_yontem', r.yontem || 'Havale / EFT', V.yontemler))}
          ${F_ALAN('knD_durum', 'Ödeme Durumu', sel('knD_durum', r.durum || 'Bekliyor', V.durumlar))}
          ${F_ALAN('knD_plan', 'Planlanan Ödeme Tarihi', inp('knD_plan', r.planlanan, 'date'))}
          ${F_ALAN('knD_belgeTarihi', 'Belge Tarihi', inp('knD_belgeTarihi', r.belgeTarihi, 'date'))}
          ${F_ALAN('knD_belgeTipi', 'Belge Tipi', sel('knD_belgeTipi', r.belgeTipi || 'Fatura', ['Fatura', 'Sipariş', 'Manuel', 'Avans', 'Mahsup', 'Temlik', 'Yansıtma']))}
          ${F_ALAN('knD_faturaNo', 'Fatura / Sipariş No', inp('knD_faturaNo', r.faturaNo))}
          ${F_ALAN('knD_urun', 'Ürün / Hizmet', inp('knD_urun', r.urun), 'g2')}
          ${F_ALAN('knD_satinalmaci', 'Satınalmacı', sel('knD_satinalmaci', r.satinalmaci, V.satinalmacilar, true))}
          ${F_ALAN('knD_islemTipi', 'İşlem Tipi', sel('knD_islemTipi', r.islemTipi || 'X', ['X', 'R', 'Y']))}
          ${F_ALAN('knD_tutar', 'Tutar', inp('knD_tutar', r.tutar ?? '', 'number'))}
          ${F_ALAN('knD_pb', 'Para Birimi', sel('knD_pb', r.pb || 'TRY', Object.keys(V.kurlar)))}
          ${F_ALAN('knD_kur', 'Kur', inp('knD_kur', r.kur ?? 1, 'number'))}
          ${F_ALAN('knD_tutarTL', 'Tutar (₺)', inp('knD_tutarTL', r.tutarTL ?? '', 'number'))}
          ${F_ALAN('knD_odenme', 'Ödenme Tarihi', inp('knD_odenme', r.odenme, 'date'))}
          ${F_ALAN('knD_odenen', 'Ödenen Tutar', inp('knD_odenen', r.odenen ?? '', 'number'))}
          ${F_ALAN('knD_mutabakat', 'Mutabakat Durumu', sel('knD_mutabakat', r.mutabakat, ['Mutabık', 'Mutabık Değil'], true))}
          ${F_ALAN('knD_muafiyet', 'Muafiyet', sel('knD_muafiyet', r.muafiyet, ['Vergi ödemesi', 'Kontör / Yakıt / Sertifika', 'Fatura (Elektrik/Su/Doğalgaz)', 'Diğer'], true))}
          ${F_ALAN('knD_not', 'Not', `<textarea class="form-control" id="knD_not" rows="2">${esc(r.notlar)}</textarea>`, 'g4')}
        </div>
        <div class="d-flex gap-2 mt-2 align-items-center"><button type="button" class="btn btn-primary btn-sm" id="knDKaydet" style="font-size:.7rem"><i class="fas fa-save me-1"></i>${yeni ? 'Kaydı Oluştur' : 'Kaydet'}</button><span class="na-mini" id="knDMesaj"></span></div>
      </div></div>
      ${yeni ? '' : `
      ${av ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-hand-holding-dollar me-1"></i>Nakit Avans · ${esc(av.no)} (Modülle Canlı)</span><span class="not"><a href="${esc(av.link)}" target="_blank">modülde aç</a></span></div><div class="na-kart-g"><div class="na-ustbilgi">${[['Kullanan', `${av.kullanan} · ${av.kullananSirket}`], ['Ödeyen Şirket', av.odeyenSirket + (av.grupIci ? ' (grup içi)' : '')], ['Talep Durumu', av.talepDurumu], ['Ödeme Durumu', av.odemeDurumu], ['Onaylanan / Bekleyen', `${para(av.onaylanan)} / ${para(av.odemeBekleyen)} ${av.pb}`], ['Kapatma', av.kapatmaDurumu]].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('')}</div><div class="na-mini">Satır ÖDENDİ olunca avans modülünde "teslim edildi" olayı işlenir ve Bekleyen Ödemeler listesinden düşer.</div></div></div>` : ''}
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-sliders me-1"></i>Yönteme Özel Bilgiler · ${esc(r.yontem)}</span><span class="not">tablodaki sekmede satır içi düzenlenir</span></div><div class="na-kart-g">
        ${r.yontem === 'Çek' || r.yontem === 'Senet' ? `<div class="kn-liste">${(r.belgeler || []).length ? r.belgeler.map(x => `<div class="satir"><b>${esc(x.no)}</b><span>${esc(x.banka)}</span><span>${esc(x.asilBorclu)}</span><span>${tarih(x.vade)}</span><span class="fw-bold">${para(x.tutar)} ₺</span>${akibetRozet(x.akibet)} ${kontrolRozet(x.kontrol, x)}${x.dolasim ? `<span class="kn-dolasim"><i class="fas fa-route"></i> ${esc(x.dolasim)} (${x.gerceklesen}/${x.adimToplam})</span>` : ''}<button type="button" class="btn btn-link btn-sm p-0 kn-dolasim-ac ms-auto" data-cek="${x.cekDocEntry}"><i class="fas fa-route"></i> Dolaşım</button></div>`).join('') : '<div class="na-bos">Belge seçilmedi · Çek / Senet sekmesinde "Portföyden Seç"</div>'}</div>`
        : g ? `<div class="na-ustbilgi"><div><div class="l">${esc(g.tur)} No</div><div class="v"><a href="${esc(g.link)}" target="_blank">${esc(g.no)} <i class="fas fa-arrow-up-right-from-square"></i></a></div></div><div><div class="l">Tür</div><div class="v">${esc(g.baslik || '')}</div></div><div><div class="l">Taraflar</div><div class="v">${esc(g.taraflar || '')}</div></div><div><div class="l">Tutar</div><div class="v">${para(g.tutar)} ${esc(g.pb || '')}</div></div><div><div class="l">Durum</div><div class="v">${modulRozet(g.durum)}</div></div><div><div class="l">Muhasebe · Kontrol</div><div class="v">${esc(g.muhasebe || '—')} ${g.kontrol ? '· ' + esc(g.kontrol) : ''}</div></div><div style="grid-column:1/-1"><div class="l">Kayıt</div><div class="v" style="font-weight:500">${esc(g.ek || '')}</div></div></div>`
        : `<div class="na-ustbilgi">${Object.entries({ 'Banka Hesabı': a.bankaHesabi, 'Alıcı IBAN': a.aliciIban, 'Dekont No': a.dekontNo, 'Kasa': a.kasa, 'Makbuz No': a.makbuzNo, 'Teslim Alan': a.teslimAlan, 'Kart': a.kart, 'Taksit': a.taksit, 'Ekstre Dönemi': a.ekstreDonemi }).filter(([k, v]) => v).map(([k, v]) => `<div><div class="l">${esc(k)}</div><div class="v">${esc(v)}</div></div>`).join('') || '<div class="na-bos">Yönteme özel alan girilmemiş.</div>'}</div>`}
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-paperclip me-1"></i>Dekontlar (${(r.dekontlar || []).length})</span><span class="not">ön yüz örneği: dosya saklanmaz</span></div><div class="na-kart-g">
        <div class="kn-liste">${(r.dekontlar || []).map(d => `<div class="satir"><i class="fas fa-file-pdf text-danger"></i><b>${esc(d.ad)}</b><span class="text-muted">${esc(d.aciklama || '')}</span><span class="ms-auto text-muted" style="font-size:.64rem">${esc(d.tarih)} · ${esc(d.yukleyen)}</span></div>`).join('') || '<div class="na-bos">Dekont yok.</div>'}</div>
        <div class="d-flex gap-2 mt-2"><input class="form-control form-control-sm" id="knDDekontAd" placeholder="dekont dosya adı (ör. dekont_${r.docEntry}.pdf)" style="font-size:.7rem"><input class="form-control form-control-sm" id="knDDekontAciklama" placeholder="açıklama" style="font-size:.7rem"><button type="button" class="btn btn-light btn-sm border" id="knDDekontEkle" style="font-size:.66rem;white-space:nowrap"><i class="fas fa-upload me-1"></i>Dekont Ekle</button></div>
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Kayıt Günlüğü</span></div><div class="na-kart-g"><ul class="na-zaman">${V.gunluk.filter(x => x.docEntry === r.docEntry && x.dbName === r.dbName).map(x => `<li class="${/Ödeme|Planlandı|Talimat|Avans/.test(x.tip) ? 'fin' : /Sil/.test(x.tip) ? 'wf' : 'sis'}"><span class="t">${esc(x.tarih)}</span><b>${esc(x.tip)}</b> · ${esc(x.aciklama)} <span class="k">${esc(x.kullanici)}</span></li>`).join('') || '<li>Kayıt yok.</li>'}</ul></div></div>`}`;
    $('#knDrawer').classList.add('acik'); $('#knPerde').classList.add('acik'); $('#knDrawer').scrollTop = 0;
    const pbKur = () => { const pb = $('#knD_pb').value; $('#knD_kur').value = V.kurlar[pb] ?? 1; hesapla(); }, hesapla = () => { $('#knD_tutarTL').value = (Math.round((+$('#knD_tutar').value || 0) * (+$('#knD_kur').value || 1) * 100) / 100).toFixed(2); };
    $('#knD_pb').addEventListener('change', pbKur); $('#knD_tutar').addEventListener('input', hesapla); $('#knD_kur').addEventListener('input', hesapla);
    $('#knDKaydet').addEventListener('click', drawerKaydet);
    if (!yeni) { $('#knDSil').addEventListener('click', drawerSil); $('#knDDekontEkle').addEventListener('click', dekontEkle); }
  }
  async function drawerKaydet() {
    const r = acik, yeni = !r.docEntry; const v = id => ($('#' + id) || {}).value ?? '';
    const gov = { DocEntry: r.docEntry || 0, U_Kurum: v('knD_kurum').trim(), U_FaturaSahibi: v('knD_faturaSahibi'), U_OdeyenSirket: v('knD_odeyen'), U_OdemeYontemi: v('knD_yontem'), U_Durum: v('knD_durum'), U_PlanlananTarih: v('knD_plan') || null, BelgeTarihi: v('knD_belgeTarihi') || null, U_BagliBelgeTipi: v('knD_belgeTipi'), U_FaturaSiparisNo: v('knD_faturaNo'), U_UrunHizmet: v('knD_urun'), U_Satinalmaci: v('knD_satinalmaci'), U_IslemTipi: v('knD_islemTipi'), U_Tutar: +v('knD_tutar') || 0, U_ParaBirimi: v('knD_pb'), U_Kur: +v('knD_kur') || 1, U_TutarTL: +v('knD_tutarTL') || 0, U_OdenmeTarihi: v('knD_odenme') || null, U_OdenenTutar: +v('knD_odenen') || 0, U_OdenenTutarPB: v('knD_pb'), U_MutabakatDurumu: v('knD_mutabakat'), MuafiyetSebebi: v('knD_muafiyet'), U_Notlar: v('knD_not'), U_KayitTipi: v('knD_belgeTipi') === 'Manuel' ? 'Bakiye' : v('knD_belgeTipi') };
    if (!gov.U_Kurum) { $('#knDMesaj').textContent = 'Ödenecek cari zorunlu.'; return; }
    const db = yeni ? v('knD_db') : r.dbName;
    const d = await post(`/Rapor79/${yeni ? 'SatirEkle' : 'SatirKaydet'}?dbName=${encodeURIComponent(db)}`, gov).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { $('#knDMesaj').textContent = d.message || 'Kaydedilemedi.'; return; }
    if (yeni) { if ((!F.sirket || F.sirket === db) && (!d.row.planlanan || (d.row.planlanan >= F.bas && d.row.planlanan <= F.bit))) V.odemeler.push(d.row); mesaj('success', esc(d.message)); drawerKapat(); ciz(); if (d.row.yontem) sekme(YKEY(d.row.yontem)); }
    else { satirYerlestir(d.row); ciz(); drawerAc(d.row); $('#knDMesaj').textContent = 'Kaydedildi.'; }
  }
  async function drawerSil() {
    const r = acik; if (!confirm(`#${r.docEntry} ${r.kurum} kaydı silinsin mi? Bağlı çek / senet planları kaldırılır.`)) return;
    const d = await post('/Rapor79/SilJson', { DbName: r.dbName, DocEntry: r.docEntry }).catch(() => ({ success: false }));
    if (!d.success) { mesaj('danger', 'Silinemedi.'); return; } V.odemeler = V.odemeler.filter(x => x.docEntry !== r.docEntry); drawerKapat(); mesaj('info', 'Kayıt silindi.'); ciz();
  }
  async function dekontEkle() {
    const r = acik; const d = await post('/Rapor79/DekontEkle', { DbName: r.dbName, DocEntry: r.docEntry, Ad: $('#knDDekontAd').value, Aciklama: $('#knDDekontAciklama').value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } satirYerlestir(d.row); mesaj('success', esc(d.message)); drawerAc(d.row); ciz();
  }
  function drawerKapat() { $('#knDrawer').classList.remove('acik'); $('#knPerde').classList.remove('acik'); acik = null; }
  $('#knDKapat').addEventListener('click', drawerKapat); $('#knPerde').addEventListener('click', drawerKapat);
  $('#knYeni').addEventListener('click', () => { const ad = sirketAdi(F.sirket || V.sirketler[0].dbName); drawerAc({ docEntry: 0, dbName: F.sirket, sirket: '', kurum: '', faturaSahibi: ad, odeyenSirket: ad, yontem: aktifSekme() in SEKME && SEKME[aktifSekme()] ? SEKME[aktifSekme()] : 'Havale / EFT', durum: 'Bekliyor', planlanan: V ? V.bugun : '', pb: 'TRY', kur: 1, tutar: '', tutarTL: '', notlar: '', alanlar: {}, belgeler: [], dekontlar: [] }); });
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-ac'); if (!b) return; e.stopPropagation(); const tr = b.closest('tr'); const r = satirBul(+tr.dataset.doc); if (r) drawerAc(r); });
  document.addEventListener('click', e => { const b = e.target.closest('#knDIc .kn-dolasim-ac'); if (!b || !acik) return; const belge = (acik.belgeler || []).find(x => x.cekDocEntry === +b.dataset.cek); if (belge) dolasimAc(acik, belge); });

  // ---------- başlangıç
  (async () => { await yukle(); if (V) sekme(kok.dataset.sekme || 'tumu'); })();
})();
