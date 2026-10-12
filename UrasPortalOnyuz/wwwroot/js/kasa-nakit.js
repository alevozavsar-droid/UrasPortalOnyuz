// ============================================================
// KASA NAKİT AKIŞI — modern ekran betiği (Views/Rapor79/Index.cshtml; veri /Rapor79/Veri; kaynak Data/NakitAkisOrnek.cs + CekAkibetOrnek.cs + Mahsup / Temlik / Yansıtma modülleri)
// Sekmeler ödeme yöntemine göre; her sekmede yalnız o yöntemin sütunları (KOL). Satır içi düzenleme (.kn-alan → /Rapor79/AlanKaydet), çekmece (tüm alanlar → SatirKaydet / SatirEkle / SilJson,
// dekont → DekontEkle), Çek / Senet belge seçimi (SecilebilirBelgeler → BelgePlanla → Çek Akıbeti'nde Planlandı; BelgePlanKaldir), Mahsup / Temlik / Yansıtma kayıt bağlama
// (GrupIciKayitlar → GrupIciBagla; GrupIciKaldir; modül kaydı bağlantıyla açılır). KPI, bakiye / projeksiyon, günlük, CSV. Yazım: liste değerleri ve etiketler Title Case.
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
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 8000); }
  const csrf = ($('input[name="__RequestVerificationToken"]') || {}).value || '';
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrf }, body: JSON.stringify(body) }).then(r => r.json());
  // Bootstrap modal: açılış geçişi sürerken hide() yok sayılır → geçiş bitince kapat
  const modalKapat = el => { const m = bootstrap.Modal.getOrCreateInstance(el); if (el.classList.contains('show') && !m._isTransitioning) m.hide(); else el.addEventListener('shown.bs.modal', () => m.hide(), { once: true }); };
  const SEKME = { tumu: '', nakit: 'Nakit', cek: 'Çek', senet: 'Senet', havale: 'Havale / EFT', kredi: 'Kredi Kartı / Banka Kartı', mahsup: 'Mahsup', temlik: 'Temlik', yansitma: 'Yansıtma', diger: 'Diğer' };
  const YKEY = y => Object.keys(SEKME).find(k => SEKME[k] === y) || 'diger';
  const yontemRozet = y => `<span class="kn-yontem ${YKEY(y)}">${esc(y === 'Kredi Kartı / Banka Kartı' ? 'Kredi / Banka Kartı' : y || '—')}</span>`;
  const DURUM_SINIF = { 'ÖDENDİ': 'odendi', 'Gecikti': 'gecikti', 'Özel Onay': 'ozel', 'Planlandı': 'planli', 'Bekliyor': '' };
  const durumRozet = d => rozet(d, d === 'ÖDENDİ' ? 'ok' : d === 'Gecikti' ? 'hata' : d === 'Planlandı' ? 'bilgi' : d === 'Özel Onay' ? 'notr' : 'bekle');
  const kontrolRozet = (k, x) => k === 'Uyumlu' ? '<span class="na-bs kn-kontrol-uyumlu" title="Planlanan tedarikçiye çıktı">Uyumlu</span>' : k === 'Farklı' ? `<span class="na-bs kn-kontrol-farkli" title="DİKKAT: plan ${esc(x?.planlananTedarikci)}, çıkış ${esc(x?.ibrazYeri)}">FARKLI → ${esc(x?.ibrazYeri)}</span>` : k === 'İbraz Bekliyor' ? '<span class="na-bs kn-kontrol-bekliyor">İbraz Bekliyor</span>' : k === 'Plansız İbraz' ? '<span class="na-bs kn-kontrol-plansiz">Plansız İbraz</span>' : '';
  const akibetRozet = a => rozet(a || '—', a === 'Planlandı' ? 'bilgi' : a === 'Ciro Edildi' ? 'notr' : (a || '').includes('Tahsil') ? 'ok' : a === 'Karşılıksız' ? 'hata' : 'bekle');
  const modulRozet = d => rozet(d || '—', /Muhasebeleşti|Tamamlandı|Uyumlu|Mutabakat Oluştu|Fatura Oluştu/.test(d || '') ? 'ok' : /Reddedildi|İptal|Hata|Ters/.test(d || '') ? 'hata' : /Onay Bekliyor|İmza|Taslak/.test(d || '') ? 'bekle' : 'bilgi');

  let V = null, F = { sirket: kok.dataset.sirket || '', bas: kok.dataset.bas, bit: kok.dataset.bit, durum: '', satinalmaci: '', ara: '' }, acik = null;
  const aktifSekme = () => ($('#knSekmeler button.aktif') || {}).dataset?.p || 'tumu';
  const satirlar = () => V.odemeler.filter(r => (!F.durum || r.durum === F.durum) && (!F.satinalmaci || r.satinalmaci === F.satinalmaci) && aramaUyar(r));
  const aramaUyar = r => { const a = F.ara.trim().toLocaleLowerCase('tr-TR'); return !a || [r.kurum, r.faturaNo, r.urun, r.notlar, r.cekNolar, r.sirket, r.grupIci?.no, r.grupIci?.karsi, r.alanlar?.dekontNo, r.alanlar?.aliciIban].join(' ').toLocaleLowerCase('tr-TR').includes(a); };
  const satirBul = d => V.odemeler.find(r => r.docEntry === d);
  const satirYerlestir = row => { if (!row) return; const i = V.odemeler.findIndex(r => r.docEntry === row.docEntry); if (i >= 0) V.odemeler[i] = row; else V.odemeler.push(row); };

  // ---------- yükleme
  async function yukle() {
    $('#knKapsam').textContent = 'yükleniyor…';
    let r; try { r = await (await fetch(`/Rapor79/Veri?sirket=${encodeURIComponent(F.sirket)}&bas=${F.bas}&bit=${F.bit}`)).json(); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r;
    const d = $('#knDurum'), mevcutD = d.value; d.innerHTML = '<option value="">Tümü</option>' + V.durumlar.map(x => `<option>${esc(x)}</option>`).join(''); d.value = mevcutD;
    const s = $('#knSatinalmaci'), mevcutS = s.value; s.innerHTML = '<option value="">Tümü</option>' + V.satinalmacilar.map(x => `<option>${esc(x)}</option>`).join(''); s.value = mevcutS;
    ciz();
  }
  function ciz() {
    if (!V) return;
    const L = satirlar();
    $('#knKapsam').textContent = `${F.sirket ? V.sirketler.find(x => x.dbName === F.sirket)?.ad : 'Tüm Şirketler (Konsolide)'} · ${tarih(F.bas)} – ${tarih(F.bit)} · ${L.length} kayıt`;
    kpi(L); sayaclar(L);
    const p = aktifSekme();
    if (p === 'bakiye') cizBakiye(); else if (p === 'gunluk') cizGunluk(); else cizTablo(p, L);
    if (p === 'mahsup' || p === 'temlik' || p === 'yansitma') cizGrupOzet(p);
  }
  function sayaclar(L) { Object.keys(SEKME).forEach(k => { const n = $('#knN-' + k); if (n) n.textContent = k === 'tumu' ? L.length : L.filter(r => r.yontem === SEKME[k]).length; }); $('#knN-bakiye').textContent = V.bakiyeler.length; $('#knN-gunluk').textContent = V.gunluk.length; }
  function kpi(L) {
    const bugun = V.bugun, hafta = new Date(bugun); hafta.setDate(hafta.getDate() + 7); const haftaS = hafta.toISOString().slice(0, 10);
    const odenmemis = L.filter(r => r.durum !== 'ÖDENDİ'), odenen = L.filter(r => r.durum === 'ÖDENDİ');
    const toplam = L.reduce((s, r) => s + r.tutarTL, 0), odenenT = odenen.reduce((s, r) => s + (r.odenen ? r.odenen * (r.kur || 1) : r.tutarTL), 0), kalan = odenmemis.reduce((s, r) => s + r.tutarTL, 0);
    const buHafta = odenmemis.filter(r => r.planlanan && r.planlanan >= bugun && r.planlanan <= haftaS), geciken = odenmemis.filter(r => r.durum === 'Gecikti' || (r.planlanan && r.planlanan < bugun));
    const B = V.bakiyeler, nakit = B.filter(b => b.bankaKasa === 'BANKA' || b.bankaKasa === 'KASA').reduce((s, b) => s + b.tutarTL, 0), portfoy = B.filter(b => b.bankaKasa.includes('PORTFÖY')).reduce((s, b) => s + b.tutarTL, 0);
    const cekBekleyen = odenmemis.filter(r => r.yontem === 'Çek' || r.yontem === 'Senet').reduce((s, r) => s + r.tutarTL, 0), nakitBekleyen = kalan - cekBekleyen - odenmemis.filter(r => ['Mahsup', 'Temlik', 'Yansıtma'].includes(r.yontem)).reduce((s, r) => s + r.tutarTL, 0);
    const K = [
      { l: 'Dönem Toplamı', v: kisa(toplam), s: `${L.length} kayıt · ${odenmemis.length} açık`, k: '#0f766e' },
      { l: 'Ödenen', v: kisa(odenenT), s: `${odenen.length} kayıt`, k: '#15803d', d: 'ÖDENDİ' },
      { l: 'Kalan Ödenecek', v: kisa(kalan), s: `nakit ${kisa(nakitBekleyen)} · çek/senet ${kisa(cekBekleyen)}`, k: '#b45309' },
      { l: 'Bu Hafta Vadesi Gelen', v: kisa(buHafta.reduce((s, r) => s + r.tutarTL, 0)), s: `${buHafta.length} ödeme · ${tarih(bugun)} – ${tarih(haftaS)}`, k: '#7c3aed' },
      { l: 'Geciken', v: kisa(geciken.reduce((s, r) => s + r.tutarTL, 0)), s: `${geciken.length} ödeme · tıkla: filtre`, k: '#dc2626', d: 'Gecikti' },
      { l: 'Banka + Kasa Mevcudu', v: kisa(nakit), s: `${B.filter(b => b.bankaKasa === 'BANKA').length} banka hesabı`, k: '#1d4ed8', p: 'bakiye' },
      { l: 'Çek / Senet Portföyü', v: kisa(portfoy), s: 'portföyde + planlandı', k: '#0369a1', p: 'bakiye' },
      { l: 'Net Nakit Durumu', v: (nakit - nakitBekleyen >= 0 ? '+' : '') + kisa(nakit - nakitBekleyen), s: 'banka + kasa − bekleyen nakit ödeme', k: nakit - nakitBekleyen >= 0 ? '#15803d' : '#dc2626', p: 'bakiye' }
    ];
    $('#knKpi').innerHTML = K.map(k => `<div style="--k:${k.k};${k.d && F.durum === k.d ? 'background:#f0fdfa' : ''}" ${k.d ? `data-durum="${k.d}" title="Durum filtresi"` : k.p ? `data-p="${k.p}" title="Bakiye sekmesi"` : ''}><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
  }

  // ---------- sütun tanımları (sekmeye göre)
  // Sütunlar sekmeye göre: [başlık, genişlik ağırlığı]. Tablo sabit düzen (table-layout: fixed, %100): sütunlar ağırlıkla paylaşılır, hücreler taşmaz (üç nokta; tam metin ipucunda) —
  // 12 sütun bile 1300px'te tek ekrana sığar. Ürün / hizmet kurumun altında, para birimi tutarın altında küçük satır; not çekmecede (Tümü ve Diğer'de kısa sütun).
  const K = (ad, w) => ({ ad, w });
  const C_SIRA = K('#', 28), C_PLAN = K('Planlanan', 104), C_KURUM = K('Kurum · Ürün / Hizmet', 200), C_BELGE = K('Belge No', 92), C_TL = K('Tutar ₺', 104), C_DURUM = K('Durum', 96), C_ODENME = K('Ödenme', 78), C_ODENEN = K('Ödenen', 96), C_NOT = K('Not', 150), C_ISLEM = K('', 30);
  const KOL = {
    tumu: [C_SIRA, C_PLAN, C_KURUM, C_BELGE, K('Yöntem', 92), C_TL, C_DURUM, C_ODENME, C_ODENEN, K('Mutabakat', 80), K('Satınalmacı', 90), C_NOT, C_ISLEM],
    nakit: [C_SIRA, C_PLAN, C_KURUM, C_BELGE, C_TL, K('Kasa', 118), K('Makbuz No', 96), K('Teslim Alan', 110), C_DURUM, C_ODENME, C_ODENEN, C_NOT, C_ISLEM],
    havale: [C_SIRA, C_PLAN, C_KURUM, C_BELGE, C_TL, K('Banka Hesabı (Bizim)', 160), K('Alıcı IBAN', 160), K('Dekont No', 92), K('Dekont', 48), C_DURUM, C_ODENME, C_ODENEN, C_ISLEM],
    kredi: [C_SIRA, C_PLAN, C_KURUM, C_BELGE, C_TL, K('Kart', 150), K('Taksit', 88), K('Ekstre Dönemi', 100), C_DURUM, C_ODENME, C_ODENEN, C_NOT, C_ISLEM],
    cek: [C_SIRA, C_PLAN, K('Kurum · Ürün / Hizmet', 170), C_TL, C_DURUM, K('Çek No · Bankası', 180), K('Keşideci · Kimden Geldi', 170), K('Vade', 76), K('Belge Tutarı', 96), K('Akıbet · Plan Kontrolü', 180), K('Belge İşlemi', 122), C_ISLEM],
    senet: [C_SIRA, C_PLAN, K('Kurum · Ürün / Hizmet', 170), C_TL, C_DURUM, K('Senet No · Bankası', 180), K('Keşideci · Kimden Geldi', 170), K('Vade', 76), K('Belge Tutarı', 96), K('Akıbet · Plan Kontrolü', 180), K('Belge İşlemi', 122), C_ISLEM],
    mahsup: [C_SIRA, C_PLAN, C_KURUM, C_TL, C_DURUM, K('Mahsup No', 108), K('Mahsup Türü · Taraflar', 230), K('Mahsup Tutarı', 104), K('Mahsup Durumu · Muhasebe', 170), K('Kontrol', 110), K('Kayıt', 96), C_ISLEM],
    temlik: [C_SIRA, C_PLAN, C_KURUM, C_TL, C_DURUM, K('Temlik No', 108), K('Temlik Türü · İlk Eden → Nihai Alan', 230), K('Temlik Tutarı', 104), K('Temlik Durumu · İmza · Muhasebe', 170), K('Kontrol', 110), K('Kayıt', 96), C_ISLEM],
    yansitma: [C_SIRA, C_PLAN, C_KURUM, C_TL, C_DURUM, K('Yansıtma No', 108), K('Tür · Yansıtan → Yansıtılan', 230), K('Genel Toplam', 104), K('Durum · Fatura / Entegrasyon', 170), K('Kontrol', 110), K('Kayıt', 96), C_ISLEM],
    diger: [C_SIRA, C_PLAN, C_KURUM, C_BELGE, C_TL, K('İşlem Tipi', 70), K('Belge Tipi', 84), C_DURUM, C_ODENME, C_ODENEN, C_NOT, C_ISLEM]
  };
  const alan = (r, ad, deger, tip, secenekler, kisaGoster) => tip === 'select' ? `<select class="kn-alan" data-alan="${ad}" title="${esc(deger || '')}"><option value="">—</option>${secenekler.map(o => `<option value="${esc(o)}" ${o === deger ? 'selected' : ''}>${esc(kisaGoster ? kisaGoster(o) : o)}</option>`).join('')}</select>` : `<input type="text" class="kn-alan" data-alan="${ad}" value="${esc(deger)}" title="${esc(deger || '')}" placeholder="…">`;
  const yigin = (liste, fn) => liste.length ? liste.map(x => `<span class="yb">${fn(x)}</span>`).join('') : bos;
  const paraTL = r => `<td class="para" title="${esc(para(r.tutar) + ' ' + (r.pb || ''))}">${para(r.tutarTL)}${r.pb && r.pb !== 'TRY' ? `<small>${para(r.tutar)} ${esc(r.pb)} × ${esc(String(r.kur))}</small>` : ''}</td>`;
  function hucreler(r, p) {
    const a = r.alanlar || {}, g = r.grupIci, b = r.belgeler || [];
    const sira = `<td>${r.sira ?? ''}</td>`, plan = `<td><input type="date" class="kn-alan kn-tarih" data-alan="U_PlanlananTarih" value="${r.planlanan || ''}"></td>`;
    const kurum = `<td class="sol kurum" title="${esc(r.kurum)}${F.sirket ? '' : ' · ' + esc(r.sirket)}${r.urun ? ' · ' + esc(r.urun) : ''}">${esc(r.kurum)}<small>${F.sirket ? '' : esc(r.sirket) + ' · '}${esc(r.urun || '')}</small></td>`;
    const belgeNo = `<td class="sol" title="${esc(r.faturaNo || '')} · ${esc(r.belgeTipi || '')}">${esc(r.faturaNo || '') || bos}<small>${esc(r.belgeTipi || '')}</small></td>`;
    const tl = paraTL(r);
    const durum = `<td><select class="kn-alan kn-durum" data-alan="U_Durum">${V.durumlar.map(d => `<option ${d === r.durum ? 'selected' : ''}>${esc(d)}</option>`).join('')}</select></td>`;
    const odenme = `<td>${tarih(r.odenme)}</td>`, odenen = `<td class="para">${r.odenen ? para(r.odenen) + (r.odenenPB && r.odenenPB !== 'TRY' ? `<small>${esc(r.odenenPB)}</small>` : '') : '—'}</td>`;
    const not = `<td class="sol not" title="${esc(r.notlar || '')}">${esc(r.notlar || '')}</td>`, islem = `<td><button type="button" class="btn btn-light btn-sm py-0 px-1 border kn-ac" title="Detay / düzenle"><i class="fas fa-pen"></i></button></td>`;
    if (p === 'tumu') return [sira, plan, kurum, belgeNo, `<td>${yontemRozet(r.yontem)}</td>`, tl, durum, odenme, odenen, `<td>${r.mutabakat ? rozet(r.mutabakat, r.mutabakat === 'Mutabık' ? 'ok' : 'hata') : bos}</td>`, `<td title="${esc(r.satinalmaci || '')}">${esc(r.satinalmaci || '')}</td>`, not, islem];
    if (p === 'nakit') return [sira, plan, kurum, belgeNo, tl, `<td>${alan(r, 'U_Kasa', a.kasa, 'select', V.kasalar)}</td>`, `<td>${alan(r, 'U_MakbuzNo', a.makbuzNo)}</td>`, `<td>${alan(r, 'U_TeslimAlan', a.teslimAlan)}</td>`, durum, odenme, odenen, not, islem];
    if (p === 'havale') return [sira, plan, kurum, belgeNo, tl, `<td>${alan(r, 'U_BankaHesabi', a.bankaHesabi, 'select', V.bankaHesaplari, o => o.split(' · ')[0] + ' · ' + (o.split(' · ')[1] || '').slice(-7))}</td>`, `<td>${alan(r, 'U_AliciIban', a.aliciIban)}</td>`, `<td>${alan(r, 'U_DekontNo', a.dekontNo)}</td>`, `<td><button type="button" class="btn btn-light btn-sm py-0 px-1 border kn-ac" title="Dekontlar"><i class="fas fa-paperclip"></i> ${(r.dekontlar || []).length}</button></td>`, durum, odenme, odenen, islem];
    if (p === 'kredi') return [sira, plan, kurum, belgeNo, tl, `<td>${alan(r, 'U_Kart', a.kart, 'select', V.kartlar)}</td>`, `<td>${alan(r, 'U_Taksit', a.taksit, 'select', V.taksitler)}</td>`, `<td>${alan(r, 'U_EkstreDonemi', a.ekstreDonemi)}</td>`, durum, odenme, odenen, not, islem];
    if (p === 'cek' || p === 'senet') return [sira, plan, kurum, tl, durum,
      `<td class="sol">${yigin(b, x => `<b>${esc(x.no)}</b> <span class="text-muted">· ${esc(x.banka)}</span>`)}</td>`,
      `<td class="sol">${yigin(b, x => esc(x.asilBorclu) + (x.kimdenGeldi && x.kimdenGeldi !== x.asilBorclu ? ` <span class="text-muted">· ${esc(x.kimdenGeldi)}</span>` : ''))}</td>`,
      `<td>${yigin(b, x => tarih(x.vade))}</td>`, `<td class="para">${yigin(b, x => para(x.tutar))}</td>`,
      `<td class="sol">${yigin(b, x => akibetRozet(x.akibet) + ' ' + kontrolRozet(x.kontrol, x))}</td>`,
      `<td class="sol">${b.map(x => `<span class="yb"><button type="button" class="btn btn-link btn-sm p-0 text-danger kn-belge-kaldir" data-cek="${x.cekDocEntry}" ${x.akibet !== 'Planlandı' ? 'disabled' : ''} title="Planı kaldır: belge portföye döner"><i class="fas fa-times"></i> Kaldır</button></span>`).join('')}<span class="yb"><button type="button" class="btn btn-primary btn-sm py-0 px-1 kn-belge-sec" style="font-size:.62rem"><i class="fas fa-plus"></i> ${b.length ? 'Ekle' : 'Belge Seç'}</button></span></td>`, islem];
    if (p === 'mahsup' || p === 'temlik' || p === 'yansitma') {
      const kayit = g ? `<td class="sol"><a href="${esc(g.link)}" target="_blank" class="fw-bold" title="${esc(g.tur)} kaydını modülde aç">${esc(g.no)}</a><small>${g.bulundu === false ? '<span class="text-danger">kayıt bulunamadı</span>' : tarih(g.tarih)}</small></td>` : `<td class="sol">${bos}</td>`;
      const tur = g ? `<td class="sol" title="${esc(g.baslik || '')} · ${esc(g.taraflar || '')}">${esc(g.baslik || '')}<small>${esc(g.taraflar || '')}</small></td>` : `<td class="sol">${bos}</td>`;
      const mtutar = `<td class="para">${g ? para(g.tutar) + (g.pb && g.pb !== 'TRY' ? `<small>${esc(g.pb)}</small>` : '') : '—'}</td>`;
      const mdurum = g ? `<td class="sol" title="${esc(g.durum || '')} · ${esc((p === 'temlik' && g.imza ? 'imza ' + g.imza + ' · ' : '') + (g.muhasebe || ''))}">${modulRozet(g.durum)}<small>${esc((p === 'temlik' && g.imza ? 'imza ' + g.imza + ' · ' : '') + (g.muhasebe || ''))}</small></td>` : `<td>${bos}</td>`;
      const kontrol = `<td>${g && g.kontrol ? rozet(g.kontrol, /Uyumlu|UYUMLU/.test(g.kontrol) ? 'ok' : 'hata') : bos}</td>`;
      const aksiyon = `<td class="sol"><button type="button" class="btn btn-primary btn-sm py-0 px-1 kn-grup-sec" data-tur="${SEKME[p]}" title="${g ? 'Kaydı değiştir' : 'Modülden kayıt bağla'}"><i class="fas fa-link"></i> ${g ? 'Değiştir' : 'Kayıt Seç'}</button>${g ? ` <button type="button" class="btn btn-link btn-sm p-0 text-danger kn-grup-kaldir" title="Kayıt bağını kaldır"><i class="fas fa-times"></i></button>` : ''}</td>`;
      return [sira, plan, kurum, tl, durum, kayit, tur, mtutar, mdurum, kontrol, aksiyon, islem];
    }
    return [sira, plan, kurum, belgeNo, tl, `<td>${esc(r.islemTipi || '')}</td>`, `<td>${esc(r.belgeTipi || '')}</td>`, durum, odenme, odenen, not, islem];
  }
  function cizTablo(p, L) {
    const t = $('#knTablo-' + p); if (!t) return;
    const R = p === 'tumu' ? L : L.filter(r => r.yontem === SEKME[p]);
    const kol = KOL[p], topW = kol.reduce((s, k) => s + k.w, 0);
    let cg = t.querySelector('colgroup'); if (!cg) { cg = document.createElement('colgroup'); t.insertBefore(cg, t.tHead); }
    cg.innerHTML = kol.map(k => `<col style="width:${(k.w / topW * 100).toFixed(2)}%">`).join('');
    t.tHead.innerHTML = '<tr>' + kol.map(k => `<th class="${/Tutar|Toplam|Ödenen/.test(k.ad) ? 's' : ''}" title="${esc(k.ad)}">${esc(k.ad)}</th>`).join('') + '</tr>';
    t.tBodies[0].innerHTML = R.length ? R.map(r => `<tr class="tik ${DURUM_SINIF[r.durum] || ''}" data-doc="${r.docEntry}" data-db="${esc(r.dbName)}">${hucreler(r, p).join('')}</tr>`).join('') : `<tr><td colspan="${kol.length}" class="na-bos text-center">Bu sekmede filtreye uyan kayıt yok.${p !== 'tumu' ? ' Yeni Ödeme ile "' + esc(SEKME[p]) + '" yöntemli kayıt açabilirsiniz.' : ''}</td></tr>`;
    const toplam = R.reduce((s, r) => s + r.tutarTL, 0), odenen = R.filter(r => r.durum === 'ÖDENDİ').reduce((s, r) => s + r.tutarTL, 0);
    const tlIdx = kol.findIndex(k => k.ad === 'Tutar ₺');
    t.tFoot.innerHTML = R.length ? `<tr class="toplam"><td colspan="${tlIdx}" class="text-end">TOPLAM (₺)</td><td class="para">${para(toplam)}</td><td colspan="${kol.length - tlIdx - 1}" class="sol">ödenen ${para(odenen)} · kalan ${para(toplam - odenen)} · ${R.length} kayıt</td></tr>` : '';
    $('#knNot-' + p).textContent = `${R.length} kayıt · ${para(toplam)} ₺ · satır içi alanlar anında kaydedilir · satıra tıklayınca detay`;
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
    // projeksiyon: PB bazında mevcut − bekleyen (ÖDENDİ hariç); nakit yöntemler ↔ banka + kasa, çek / senet ↔ portföy
    const L = satirlar().filter(r => r.durum !== 'ÖDENDİ'), pbler = [...new Set([...B.map(b => b.pb), ...L.map(r => r.pb)])].sort((a, b) => a === 'TRY' ? -1 : b === 'TRY' ? 1 : a.localeCompare(b));
    const nakitY = r => !['Çek', 'Senet', 'Mahsup', 'Temlik', 'Yansıtma'].includes(r.yontem);
    $('#knProjeksiyon tbody').innerHTML = pbler.map(pb => { const mb = B.filter(b => (b.bankaKasa === 'BANKA' || b.bankaKasa === 'KASA') && b.pb === pb).reduce((s, b) => s + b.tutar, 0), bn = L.filter(r => r.pb === pb && nakitY(r)).reduce((s, r) => s + r.tutar, 0), mc = B.filter(b => b.bankaKasa.includes('PORTFÖY') && b.pb === pb).reduce((s, b) => s + b.tutar, 0), bc = L.filter(r => r.pb === pb && (r.yontem === 'Çek' || r.yontem === 'Senet')).reduce((s, r) => s + r.tutar, 0); const k = v => `<td class="para ${v < 0 ? 'text-danger' : 'text-primary'}">${para(v)}</td>`; return `<tr><td rowspan="2" class="fw-bold">${esc(pb === 'TRY' ? 'TL' : pb)}</td><td class="sol">Nakit / Banka</td><td class="para">${para(mb)}</td><td class="para text-danger">${para(bn)}</td>${k(mb - bn)}</tr><tr><td class="sol">Müşteri Çekleri / Senetleri</td><td class="para">${para(mc)}</td><td class="para text-danger">${para(bc)}</td>${k(mc - bc)}</tr>`; }).join('');
  }
  function cizGunluk() {
    const a = F.ara.trim().toLocaleLowerCase('tr-TR'); const G = V.gunluk.filter(g => !a || [g.kullanici, g.tip, g.aciklama, g.sirket, String(g.docEntry)].join(' ').toLocaleLowerCase('tr-TR').includes(a));
    $('#knGunluk tbody').innerHTML = G.length ? G.map(g => `<tr class="tik" data-doc="${g.docEntry}" data-db="${esc(g.dbName)}"><td>${esc(g.tarih)}</td><td>${esc(g.kullanici)}</td><td>${esc(g.sirket)}</td><td>#${g.docEntry}</td><td>${rozet(g.tip, /Sil|Kaldır/.test(g.tip) ? 'hata' : /Ödeme|Planlandı|Bağlandı/.test(g.tip) ? 'ok' : 'bilgi')}</td><td class="sol">${esc(g.aciklama)}</td></tr>`).join('') : '<tr><td colspan="6" class="na-bos">Günlük kaydı yok.</td></tr>';
    $('#knGunlukNot').textContent = `${G.length} kayıt · son 300 · satıra tıklayınca ödeme kaydı`;
  }

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
  $('#knExcel').addEventListener('click', () => { const p = aktifSekme(), t = $('#knTablo-' + p) || $('#knBakiye'); if (!t) return; const bas = $$('thead th', t).map(x => x.textContent.trim()); const rows = $$('tbody tr', t).filter(r => !r.querySelector('.na-bos')).map(r => $$('td', r).map(td => { const i = td.querySelector('input,select'); return i ? i.value : td.textContent.trim().replace(/\s+/g, ' '); })); csvIndir('kasa_nakit_akisi_' + p, bas, rows); });

  // ---------- satır içi kayıt
  kok.addEventListener('change', async e => {
    const inp = e.target.closest('.kn-alan'); if (!inp) return; const tr = inp.closest('tr'); if (!tr) return;
    const d = await post('/Rapor79/AlanKaydet', { DbName: tr.dataset.db, DocEntry: +tr.dataset.doc, Alan: inp.dataset.alan, Deger: inp.value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message || 'Kaydedilemedi.'); return; }
    satirYerlestir(d.row); inp.classList.add('ok'); setTimeout(() => inp.classList.remove('ok'), 1200);
    if (inp.dataset.alan === 'U_Durum' || inp.dataset.alan === 'U_PlanlananTarih') ciz(); else { const L = satirlar(); kpi(L); }
  });
  kok.addEventListener('click', e => {
    const tr = e.target.closest('tr.tik'); if (!tr || !kok.contains(tr)) return;
    if (e.target.closest('input,select,a,button.kn-belge-sec,button.kn-belge-kaldir,button.kn-grup-sec,button.kn-grup-kaldir')) return;
    const r = satirBul(+tr.dataset.doc); if (r) drawerAc(r);
  });

  // ---------- belge seçimi (Çek / Senet)
  let secim = { r: null, belge: 'Çek', liste: [] };
  const kbSayac = () => { const c = $$('.kb-chk:checked'); let t = 0; c.forEach(x => t += +x.dataset.tutar || 0); $('#kbSayi').textContent = c.length; $('#kbToplam').textContent = para(t); const st = secim.r ? secim.r.tutar : 0; $('#kbFark').textContent = c.length && st ? `(satır ${para(st)} · fark ${t - st >= 0 ? '+' : ''}${para(t - st)}; satır tutarı belge toplamı olur)` : ''; };
  const vadeRozet = v => rozet(v || '', v === 'Vadesi Geçti' ? 'hata' : v === 'Bugün' || v === 'Bu Hafta' ? 'bekle' : v === 'Bu Ay' ? 'bilgi' : 'notr');
  function kbCiz() {
    const q = ($('#kbAra').value || '').toLocaleLowerCase('tr-TR'); const l = secim.liste.filter(x => !q || [x.no, x.asilBorclu, x.kimdenGeldi, x.banka, x.musteriKod].join(' ').toLocaleLowerCase('tr-TR').includes(q));
    $('#kbGovde').innerHTML = l.length ? l.map(x => `<tr><td><input type="checkbox" class="form-check-input kb-chk" value="${x.docEntry}" data-tutar="${x.tutar}"></td><td class="fw-bold">${esc(x.no)}</td><td>${esc(x.banka)} / ${esc(x.sube)}</td><td>${esc(x.asilBorclu)}</td><td>${esc(x.kimdenGeldi)}<br><small class="text-muted">${esc(x.musteriKod)}</small></td><td>${tarih(x.vade)}</td><td>${vadeRozet(x.vadeDurumu)}</td><td class="text-end fw-bold">${para(x.tutar)} ₺</td><td>${tarih(x.portfoyGiris)}</td></tr>`).join('') : `<tr><td colspan="9" class="text-center text-muted py-3">Portföyde seçilebilir ${esc(secim.belge.toLocaleLowerCase('tr-TR'))} yok (planlanmamış ve kullanılabilir olmalı).</td></tr>`;
    $('#kbHepsi').checked = false; kbSayac();
  }
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.kn-belge-sec'); if (!b) return; const tr = b.closest('tr'); const r = satirBul(+tr.dataset.doc); if (!r) return;
    secim = { r, belge: r.yontem === 'Senet' ? 'Senet' : 'Çek', liste: [] };
    $('#kbBaslik').textContent = secim.belge + ' Seç'; $('#kbKurum').textContent = r.kurum; $('#kbBilgi').textContent = `Satır tutarı ${para(r.tutar)} ${r.pb} · seçilen belgeler Çek Akıbeti Raporu'nda "Planlandı" olur, planlanan tedarikçi bu kurum`;
    $('#kbAra').value = ''; $('#kbGovde').innerHTML = '<tr><td colspan="9" class="text-center text-muted py-3"><i class="fas fa-spinner fa-spin"></i> Portföy getiriliyor…</td></tr>'; kbSayac();
    bootstrap.Modal.getOrCreateInstance($('#knBelgeModal')).show();
    try { const d = await (await fetch(`/Rapor79/SecilebilirBelgeler?dbName=${encodeURIComponent(r.dbName)}&belge=${encodeURIComponent(secim.belge)}`)).json(); secim.liste = d.data || []; kbCiz(); } catch (x) { $('#kbGovde').innerHTML = '<tr><td colspan="9" class="text-center text-danger py-3">Ağ hatası.</td></tr>'; }
  });
  $('#kbAra').addEventListener('input', kbCiz); $('#kbGovde').addEventListener('change', kbSayac); $('#kbHepsi').addEventListener('change', () => { $$('.kb-chk').forEach(c => c.checked = $('#kbHepsi').checked); kbSayac(); });
  $('#kbOnayla').addEventListener('click', async () => {
    const ids = $$('.kb-chk:checked').map(c => +c.value); if (!ids.length) { mesaj('warning', 'En az bir belge seçin.'); return; }
    const d = await post('/Rapor79/BelgePlanla', { DbName: secim.r.dbName, DocEntry: secim.r.docEntry, CekDocEntryler: ids }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message || 'Planlanamadı.'); return; }
    modalKapat($('#knBelgeModal')); satirYerlestir(d.row); mesaj('success', '<i class="fas fa-check me-1"></i>' + esc(d.message)); ciz();
  });
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.kn-belge-kaldir'); if (!b || b.disabled) return; const tr = b.closest('tr');
    if (!confirm('Belgenin planı kaldırılsın mı? Belge portföye döner (Çek Akıbeti: Portföyde).')) return;
    const d = await post('/Rapor79/BelgePlanKaldir', { DbName: tr.dataset.db, DocEntry: +tr.dataset.doc, CekDocEntry: +b.dataset.cek }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', d.message); return; } satirYerlestir(d.row); mesaj('info', esc(d.message)); ciz();
  });

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
    bootstrap.Modal.getOrCreateInstance($('#knGrupModal')).show();
    try { const d = await (await fetch(`/Rapor79/GrupIciKayitlar?dbName=${encodeURIComponent(r.dbName)}&tur=${encodeURIComponent(grup.tur)}`)).json(); grup.liste = d.data || []; kgCiz(); } catch (x) { $('#kgGovde').innerHTML = '<tr><td colspan="9" class="text-center text-danger py-3">Ağ hatası.</td></tr>'; }
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
    acik = r; const yeni = !r.docEntry; const a = r.alanlar || {}, g = r.grupIci;
    $('#knDBaslik').innerHTML = yeni ? '<i class="fas fa-plus me-1"></i>Yeni Ödeme Kaydı' : `${esc(r.kurum)} ${yontemRozet(r.yontem)} ${durumRozet(r.durum)} <span class="kn-pill">#${r.docEntry} · ${esc(r.sirket)}</span>`;
    $('#knDAksiyon').innerHTML = yeni ? '' : `<button type="button" class="btn btn-light btn-sm border" id="knDSil" style="font-size:.66rem"><i class="fas fa-trash me-1 text-danger"></i>Sil</button>`;
    const dbSec = yeni ? F_ALAN('knD_db', 'Şirket', sel('knD_db', F.sirket || V.sirketler[0].dbName, V.sirketler.map(s => s.dbName)).replace(/<option( selected)?>([^<]+)<\/option>/g, (m, s, v) => `<option value="${v}"${s || ''}>${esc(V.sirketler.find(x => x.dbName === v)?.ad || v)}</option>`)) : '';
    $('#knDIc').innerHTML = `
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-pen me-1"></i>Ödeme Bilgileri</span><span class="not">${yeni ? 'kaydet: bellek içi' : 'satır içi alanlar da tabloda düzenlenebilir'}</span></div><div class="na-kart-g">
        <div class="kn-form">
          ${dbSec}
          ${F_ALAN('knD_kurum', 'Kurum <span class="z">*</span>', `<input class="form-control" id="knD_kurum" list="knTedarikciler" value="${esc(r.kurum)}"><datalist id="knTedarikciler">${(V.tedarikciler || []).map(t => `<option value="${esc(t)}">`).join('')}</datalist>`, yeni ? 'g2' : 'g2')}
          ${F_ALAN('knD_yontem', 'Ödeme Yöntemi', sel('knD_yontem', r.yontem || 'Havale / EFT', V.yontemler))}
          ${F_ALAN('knD_durum', 'Durum', sel('knD_durum', r.durum || 'Bekliyor', V.durumlar))}
          ${F_ALAN('knD_plan', 'Planlanan Ödeme', inp('knD_plan', r.planlanan, 'date'))}
          ${F_ALAN('knD_belgeTarihi', 'Belge Tarihi', inp('knD_belgeTarihi', r.belgeTarihi, 'date'))}
          ${F_ALAN('knD_belgeTipi', 'Belge Tipi', sel('knD_belgeTipi', r.belgeTipi || 'Fatura', ['Fatura', 'Sipariş', 'Manuel', 'Mahsup', 'Temlik', 'Yansıtma']))}
          ${F_ALAN('knD_faturaNo', 'Belge No', inp('knD_faturaNo', r.faturaNo))}
          ${F_ALAN('knD_urun', 'Ürün / Hizmet', inp('knD_urun', r.urun), 'g2')}
          ${F_ALAN('knD_satinalmaci', 'Satınalmacı', sel('knD_satinalmaci', r.satinalmaci, V.satinalmacilar, true))}
          ${F_ALAN('knD_islemTipi', 'İşlem Tipi', sel('knD_islemTipi', r.islemTipi || 'X', ['X', 'R', 'Y']))}
          ${F_ALAN('knD_tutar', 'Tutar', inp('knD_tutar', r.tutar ?? '', 'number'))}
          ${F_ALAN('knD_pb', 'PB', sel('knD_pb', r.pb || 'TRY', Object.keys(V.kurlar)))}
          ${F_ALAN('knD_kur', 'Kur', inp('knD_kur', r.kur ?? 1, 'number'))}
          ${F_ALAN('knD_tutarTL', 'Tutar (₺)', inp('knD_tutarTL', r.tutarTL ?? '', 'number'))}
          ${F_ALAN('knD_odenme', 'Ödenme Tarihi', inp('knD_odenme', r.odenme, 'date'))}
          ${F_ALAN('knD_odenen', 'Ödenen Tutar', inp('knD_odenen', r.odenen ?? '', 'number'))}
          ${F_ALAN('knD_mutabakat', 'Mutabakat', sel('knD_mutabakat', r.mutabakat, ['Mutabık', 'Mutabık Değil'], true))}
          ${F_ALAN('knD_muafiyet', 'Muafiyet', sel('knD_muafiyet', r.muafiyet, ['Vergi ödemesi', 'Kontör / Yakıt / Sertifika', 'Fatura (Elektrik/Su/Doğalgaz)', 'Diğer'], true))}
          ${F_ALAN('knD_not', 'Not', `<textarea class="form-control" id="knD_not" rows="2">${esc(r.notlar)}</textarea>`, 'g4')}
        </div>
        <div class="d-flex gap-2 mt-2 align-items-center"><button type="button" class="btn btn-primary btn-sm" id="knDKaydet" style="font-size:.7rem"><i class="fas fa-save me-1"></i>${yeni ? 'Kaydı Oluştur' : 'Kaydet'}</button><span class="na-mini" id="knDMesaj"></span></div>
      </div></div>
      ${yeni ? '' : `
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-sliders me-1"></i>Yönteme Özel Bilgiler · ${esc(r.yontem)}</span><span class="not">tablodaki sekmede satır içi düzenlenir</span></div><div class="na-kart-g">
        ${r.yontem === 'Çek' || r.yontem === 'Senet' ? `<div class="kn-liste">${(r.belgeler || []).length ? r.belgeler.map(x => `<div class="satir"><b>${esc(x.no)}</b><span>${esc(x.banka)}</span><span>${esc(x.asilBorclu)}</span><span>${tarih(x.vade)}</span><span class="fw-bold">${para(x.tutar)} ₺</span>${akibetRozet(x.akibet)} ${kontrolRozet(x.kontrol, x)}</div>`).join('') : '<div class="na-bos">Belge seçilmedi · Çek / Senet sekmesinde "Belge Seç"</div>'}</div>`
        : g ? `<div class="na-ustbilgi"><div><div class="l">${esc(g.tur)} No</div><div class="v"><a href="${esc(g.link)}" target="_blank">${esc(g.no)} <i class="fas fa-arrow-up-right-from-square"></i></a></div></div><div><div class="l">Tür</div><div class="v">${esc(g.baslik || '')}</div></div><div><div class="l">Taraflar</div><div class="v">${esc(g.taraflar || '')}</div></div><div><div class="l">Tutar</div><div class="v">${para(g.tutar)} ${esc(g.pb || '')}</div></div><div><div class="l">Durum</div><div class="v">${modulRozet(g.durum)}</div></div><div><div class="l">Muhasebe · Kontrol</div><div class="v">${esc(g.muhasebe || '—')} ${g.kontrol ? '· ' + esc(g.kontrol) : ''}</div></div><div class="g4" style="grid-column:1/-1"><div class="l">Kayıt</div><div class="v" style="font-weight:500">${esc(g.ek || '')}</div></div></div>`
        : `<div class="na-ustbilgi">${Object.entries({ 'Banka Hesabı': a.bankaHesabi, 'Alıcı IBAN': a.aliciIban, 'Dekont No': a.dekontNo, 'Kasa': a.kasa, 'Makbuz No': a.makbuzNo, 'Teslim Alan': a.teslimAlan, 'Kart': a.kart, 'Taksit': a.taksit, 'Ekstre Dönemi': a.ekstreDonemi }).filter(([k, v]) => v).map(([k, v]) => `<div><div class="l">${esc(k)}</div><div class="v">${esc(v)}</div></div>`).join('') || '<div class="na-bos">Yönteme özel alan girilmemiş.</div>'}</div>`}
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-paperclip me-1"></i>Dekontlar (${(r.dekontlar || []).length})</span><span class="not">ön yüz örneği: dosya saklanmaz</span></div><div class="na-kart-g">
        <div class="kn-liste">${(r.dekontlar || []).map(d => `<div class="satir"><i class="fas fa-file-pdf text-danger"></i><b>${esc(d.ad)}</b><span class="text-muted">${esc(d.aciklama || '')}</span><span class="ms-auto text-muted" style="font-size:.64rem">${esc(d.tarih)} · ${esc(d.yukleyen)}</span></div>`).join('') || '<div class="na-bos">Dekont yok.</div>'}</div>
        <div class="d-flex gap-2 mt-2"><input class="form-control form-control-sm" id="knDDekontAd" placeholder="dekont dosya adı (ör. dekont_${r.docEntry}.pdf)" style="font-size:.7rem"><input class="form-control form-control-sm" id="knDDekontAciklama" placeholder="açıklama" style="font-size:.7rem"><button type="button" class="btn btn-light btn-sm border" id="knDDekontEkle" style="font-size:.66rem;white-space:nowrap"><i class="fas fa-upload me-1"></i>Dekont Ekle</button></div>
      </div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Kayıt Günlüğü</span></div><div class="na-kart-g"><ul class="na-zaman">${V.gunluk.filter(x => x.docEntry === r.docEntry && x.dbName === r.dbName).map(x => `<li class="${/Ödeme|Planlandı/.test(x.tip) ? 'fin' : /Sil/.test(x.tip) ? 'wf' : 'sis'}"><span class="t">${esc(x.tarih)}</span><b>${esc(x.tip)}</b> · ${esc(x.aciklama)} <span class="k">${esc(x.kullanici)}</span></li>`).join('') || '<li>Kayıt yok.</li>'}</ul></div></div>`}`;
    $('#knDrawer').classList.add('acik'); $('#knPerde').classList.add('acik'); $('#knDrawer').scrollTop = 0;
    const pbKur = () => { const pb = $('#knD_pb').value; $('#knD_kur').value = V.kurlar[pb] ?? 1; hesapla(); }, hesapla = () => { $('#knD_tutarTL').value = (Math.round((+$('#knD_tutar').value || 0) * (+$('#knD_kur').value || 1) * 100) / 100).toFixed(2); };
    $('#knD_pb').addEventListener('change', pbKur); $('#knD_tutar').addEventListener('input', hesapla); $('#knD_kur').addEventListener('input', hesapla);
    $('#knDKaydet').addEventListener('click', drawerKaydet);
    if (!yeni) { $('#knDSil').addEventListener('click', drawerSil); $('#knDDekontEkle').addEventListener('click', dekontEkle); }
  }
  async function drawerKaydet() {
    const r = acik, yeni = !r.docEntry; const v = id => ($('#' + id) || {}).value ?? '';
    const gov = { DocEntry: r.docEntry || 0, U_Kurum: v('knD_kurum').trim(), U_OdemeYontemi: v('knD_yontem'), U_Durum: v('knD_durum'), U_PlanlananTarih: v('knD_plan') || null, BelgeTarihi: v('knD_belgeTarihi') || null, U_BagliBelgeTipi: v('knD_belgeTipi'), U_FaturaSiparisNo: v('knD_faturaNo'), U_UrunHizmet: v('knD_urun'), U_Satinalmaci: v('knD_satinalmaci'), U_IslemTipi: v('knD_islemTipi'), U_Tutar: +v('knD_tutar') || 0, U_ParaBirimi: v('knD_pb'), U_Kur: +v('knD_kur') || 1, U_TutarTL: +v('knD_tutarTL') || 0, U_OdenmeTarihi: v('knD_odenme') || null, U_OdenenTutar: +v('knD_odenen') || 0, U_OdenenTutarPB: v('knD_pb'), U_MutabakatDurumu: v('knD_mutabakat'), MuafiyetSebebi: v('knD_muafiyet'), U_Notlar: v('knD_not'), U_KayitTipi: v('knD_belgeTipi') === 'Manuel' ? 'Bakiye' : v('knD_belgeTipi') };
    if (!gov.U_Kurum) { $('#knDMesaj').textContent = 'Kurum zorunlu.'; return; }
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
  $('#knYeni').addEventListener('click', () => drawerAc({ docEntry: 0, dbName: F.sirket, sirket: '', kurum: '', yontem: aktifSekme() in SEKME && SEKME[aktifSekme()] ? SEKME[aktifSekme()] : 'Havale / EFT', durum: 'Bekliyor', planlanan: V ? V.bugun : '', pb: 'TRY', kur: 1, tutar: '', tutarTL: '', notlar: '', alanlar: {}, belgeler: [], dekontlar: [] }));
  kok.addEventListener('click', e => { const b = e.target.closest('.kn-ac'); if (!b) return; e.stopPropagation(); const tr = b.closest('tr'); const r = satirBul(+tr.dataset.doc); if (r) drawerAc(r); });

  // ---------- başlangıç
  (async () => { await yukle(); if (V) sekme(kok.dataset.sekme || 'tumu'); })();
})();
