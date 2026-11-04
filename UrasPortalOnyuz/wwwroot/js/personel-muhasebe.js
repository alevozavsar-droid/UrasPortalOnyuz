// ============================================================
// PERSONEL MUHASEBE İŞLEMLERİ — ekran betiği (Views/PersonelMuhasebe/Index.cshtml; veri /PersonelMuhasebe/Veri?donem=)
// Personel Maliyet Raporu › Muhasebe Kayıtları sekmesiyle AYNI simülasyon verisini (Data/PersonelMuhasebeOrnek.cs) kullanır: burada atılan onay /
// muhasebeleştirme / ters kayıt / hesap eşleştirmesi orada da görünür (ve tersi). Yansıtma kuralları Data/PersonelYansitmaOrnek.cs (GDR.13 ↔ YNS.13).
// Sekmeler: Muhasebe Kayıtları (ayrıntılı: kontroller, fişler, personel bazlı satırlar, özet, audit, aksiyonlar) · Ödemeler (banka R / kasa X) ·
//   Yansıtma Hazırlığı · Yansıtma Kayıtları (+ simülasyon B grubu mutabakatı) · Kalem Eşleme · Hesap Eşleştirme · Fiş Arşivi (tüm dönemler)
// Ortak sütun / filtre: SGK İşvereni · Fiili Görev Yeri · Ödeme Şirketi her personel tablosunda sütundur; filtre çubuğundaki aynı üç seçim (kisiUyar)
//   tüm sekmelerdeki personel satırlarını süzer (Kalem Eşleme / Hesap Eşleştirme / Fiş Arşivi'nde personel satırı yok → seçimler pasif).
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Kaydedildi", "Faturalandı", "İptal").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('ph'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const kurus = v => { const n = Math.round((Number(v) || 0) * 100) / 100; return Math.abs(n) < 0.005 ? 0 : n; };
  const tam = v => kurus(v).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const kisa = v => tam(v) + ' ₺';
  const hucre = v => kurus(v) ? kisa(v) : '<span class="text-muted">—</span>';
  const tl = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const pct = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 2 });
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const kucukTr = s => String(s ?? '').toLocaleLowerCase('tr-TR');
  const rozet = (d, sinif) => `<span class="pm-rz ${sinif || (/(Onaylandı|Muhasebeleştirildi|Kaydedildi|Faturalandı|Geçti|Dengeli|Güncel|Aktif|Uyumlu|^Hazır$)/.test(d) ? 'ok' : /(Bekliyor|Onaya Gönderildi|Kontrole Hazır|Simülasyon Oluşturuldu|Kısmen|Uyarı|Taslak|Kayıtsız)/.test(d) ? 'bekle' : /(İptal|Hata|Dengesiz|Değişti|Kontrol Gerekiyor|Güncelliğini Yitirdi|Pasif|Kalem Yok|Fark)/.test(d) ? 'hata' : /(Ters Kayıt)/.test(d) ? 'mavi' : 'gri')}">${esc(d || '—')}</span>`;
  const yon = (k, h, pay) => `<span class="ph-yon"><b>${esc(k)}</b><span class="ok">→</span><b>${esc(h)}</b>${pay !== undefined ? `<span class="ph-pay">%${pct(pay)}</span>` : ''}</span>`;
  /** ekrandaki tabloları göründüğü gibi Excel'e (ortak bileşen wwwroot/js/tablo-excel.js: renkler, rozetler, birleşik başlıklar, dip toplam, dondurulmuş başlık; her tablo ayrı sayfa, ilk sayfada sekmenin KPI kutuları ve açıklaması) */
  function domExcel(ad, tablolar, bicim = 'xlsx') {
    if (!window.TabloExcel) { mesaj('danger', 'Excel bileşeni yüklenemedi; sayfayı yenileyip tekrar deneyin.'); return; }
    const panel = $('.na-panel.aktif'), h5 = panel ? panel.querySelector('.pm-rapor-ust h5') : null, bas = h5 ? h5.textContent.trim() : '', sayac = panel ? panel.querySelector('.pm-sayac') : null;
    const sayfalar = tablolar.filter(([id]) => $('#' + id)).map(([id, sayfa], i) => ({ tablo: '#' + id, ad: sayfa, baslik: `Personel Muhasebe İşlemleri · ${bas || sayfa}${bas ? ' · ' + sayfa : ''}`, altBaslik: i === 0 && sayac ? sayac.textContent.trim() : '', kpi: i === 0 ? '#phKpi' : null, not: i === 0 && panel ? panel.querySelector(':scope > .pm-not') : null }));
    const pdf = bicim === 'pdf';
    (pdf ? window.TabloExcel.pdf : window.TabloExcel.indir)(ad, sayfalar).catch(e => mesaj('danger', (pdf ? 'PDF' : 'Excel') + ' indirilemedi: ' + esc(e.message)));
  }
  /** Excel ve PDF düğmelerini bağlar (ortak TabloExcel.dugme): fn('xlsx') ya da fn('pdf') çağrılır */
  const dugmeBagla = (xl, pd, fn) => { if (window.TabloExcel) window.TabloExcel.dugme(xl, pd, fn); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 9000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  async function post(url, veri) { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }); return r.json(); }

  let V = null, aktifSekme = kok.dataset.baslangic || 'kayitlar', seciliSim = null, hata = false, fisF = '', detay = true;
  let secili = new Set(), acikDetay = new Set();   // yansıtma hazırlığı: seçili personel, açık satır detayı
  const M = () => (V && V.muhasebe) || { simulasyonlar: [], eslemeler: [], hesaplar: [], sirketler: [], islemTurleri: [], kaynakOzet: {} };
  const Y = () => (V && V.yansitma) || { adaylar: [], kayitlar: [], esleme: [], katalog: [], sirketler: [], donemler: [] };
  const sim = () => { const m = M(); return m.simulasyonlar.find(s => s.id === seciliSim) || m.simulasyonlar.find(s => s.id === m.seciliId) || m.simulasyonlar[0] || null; };
  const donem = () => (V && V.donem) || kok.dataset.donem || '';
  const ara = () => kucukTr($('#phAra').value || '');
  const kartAdresi = kod => (V && V.kartAdresi || '/PersonelMaliyet?kod=') + encodeURIComponent(kod || '');
  /** ortak üç filtre (SGK İşvereni / Fiili Görev Yeri / Ödeme Şirketi): tüm sekmelerdeki personel satırlarına uygulanır */
  const secim = id => ($(id) || {}).value || '';
  const kisiUyar = x => { const s = secim('#phSgk'), g = secim('#phGorevYeri'), o = secim('#phOdemeSirketi'); return (!s || x.sgkIsvereni === s) && (!g || x.fiiliGorevYeri === g) && (!o || x.odemeSirketi === o || x.odemeSirketiX === o); };
  const KISI_SEKME = ['kayitlar', 'odemeler', 'hazirlik', 'yansitma'];   // personel satırı olan sekmeler; diğerlerinde ortak filtre pasif

  // ---- yerleşim: tek tablolu sekmelerde (data-tam) kaydırma kabı ekranın kalanını doldurur
  function kapBoyut() { $$('.na-panel.aktif .pm-kaydir[data-tam]').forEach(k => { const r = k.getBoundingClientRect(); k.style.maxHeight = Math.max(300, window.innerHeight - Math.max(r.top, 0) - 28) + 'px'; }); }

  // ---- sekmeler (muhasebe / ödeme / arşiv sekmelerine girişte veri tazelenir: Personel Maliyet Raporu'nda atılan adımlar burada görünsün)
  function sekme(p, tazele) {
    aktifSekme = p; $$('#phSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#ph > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    $$('#phSgk, #phGorevYeri, #phOdemeSirketi').forEach(e => { e.disabled = !KISI_SEKME.includes(p); e.title = e.disabled ? 'Bu sekmede personel satırı yok (tanım / arşiv tablosu); seçim diğer sekmelerde geçerli kalır' : 'Tüm sekmelerdeki personel satırlarını süzer'; });
    try { const u = new URL(location.href); if (p === 'kayitlar') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
    if (!V) return;
    if (tazele && ['kayitlar', 'odemeler', 'arsiv', 'hesap'].includes(p)) yukle().catch(() => { /* yoksay */ });
    kpiCiz(); cizAktif();
  }
  function cizAktif() {
    const p = aktifSekme;
    if (p === 'kayitlar') kayitlarCiz(); else if (p === 'odemeler') odemeCiz(); else if (p === 'hazirlik') hazirlikCiz(); else if (p === 'yansitma') yansitmaCiz(); else if (p === 'esleme') kalemEslemeCiz(); else if (p === 'hesap') hesapCiz(); else if (p === 'arsiv') arsivCiz();
    requestAnimationFrame(kapBoyut);
  }

  // ---- veri
  async function yukle(d) { const r = await fetch('/PersonelMuhasebe/Veri?donem=' + encodeURIComponent(d || donem())); const j = await r.json(); veriUygula(j); }
  function veriUygula(j) {
    V = j; kok.dataset.donem = V.donem;
    const sel = $('#phDonem'); sel.innerHTML = (V.donemler || []).map(d => `<option value="${esc(d.kod)}">${esc(d.ad)}${d.guncel ? ' (Güncel)' : ''}</option>`).join(''); sel.value = V.donem;
    $('#phDonemPill').textContent = V.donemAd; $('#phRapor').href = V.raporAdresi || '/PersonelMaliyet?sekme=muhasebe';
    const y = Y(), m = M();
    const doldur = (id, l, ilk) => { const e = $(id); if (!e) return; const v = e.value, ilkMetin = ilk || e.options[0]?.textContent || 'Tümü'; e.innerHTML = `<option value="">${esc(ilkMetin)}</option>` + l.map(x => `<option value="${esc(x.kod !== undefined ? x.kod : x)}">${esc(x.ad !== undefined ? x.ad : x)}</option>`).join(''); e.value = [...e.options].some(o => o.value === v) ? v : ''; };
    const F = V.filtreler || {}; doldur('#phSgk', F.sgkIsverenleri || []); doldur('#phGorevYeri', F.fiiliGorevYerleri || []); doldur('#phOdemeSirketi', F.odemeSirketleri || []);   // ortak üç filtre (tüm sekmeler)
    doldur('#phHHedef', y.sirketler || []); doldur('#phHCati', [...new Set((y.adaylar || []).map(a => a.catiDepartman))].sort((a, b) => String(a).localeCompare(String(b), 'tr')));
    doldur('#phKHedef', y.sirketler || []); doldur('#phKDonem', y.donemler || []);
    $('#phKdv').value = y.kdvOrani ?? 0;
    $('#phNSim').textContent = m.simulasyonlar.length; $('#phNOdeme').textContent = (V.odemeler && V.odemeler.satirlar || []).length; $('#phNAday').textContent = (y.adaylar || []).length; $('#phNKayit').textContent = (y.kayitlar || []).length; $('#phNEsleme').textContent = (m.eslemeler || []).length; $('#phNArsiv').textContent = (V.arsiv || []).length;
    $('#phSayi').textContent = `${V.donemAd} · ${m.kullanici || V.ben} (${m.yetkili ? 'onay yetkili' : 'onay yetkisi yok'}) · Personel Maliyet Raporu › Muhasebe Kayıtları ile aynı veri`;
    hesapFormKur(m);
    kpiCiz(); cizAktif();
  }
  /** üst KPI kartları: muhasebe sekmelerinde seçili simülasyon, yansıtma sekmelerinde yansıtma özetleri (ortak na-kpi ölçüsü) */
  function kpiCiz() {
    const s = sim(), m = M(), K = m.kaynakOzet || {}, y = Y(); let kpi;
    if (['hazirlik', 'yansitma', 'esleme'].includes(aktifSekme)) {
      const A = y.adaylar || [], kay = (y.kayitlar || []).filter(k => k.donem === donem() && k.durum !== 'İptal'), kalan = A.filter(a => a.durum !== 'Kaydedildi'), ciftler = new Set(A.flatMap(a => a.hedefler.map(h => a.kaynakSirket + '→' + h.sirket)));
      kpi = [['#b45309', 'Yansıtma Gereken Personel', A.length, `${V.donemAd} · ${kalan.length} kayıtsız`], ['#0f172a', 'Yansıtılacak Toplam', kisa(A.reduce((t, a) => t + a.yansitilacak, 0)), `aylık maliyet ${kisa(A.reduce((t, a) => t + a.maliyet, 0))}`], ['#7c3aed', 'Simülasyon B · Yansıtma', kisa(s ? s.yansitmaGereken : K.yansitma), 'genel maliyet bazında (Muhasebe Kayıtları)'],
        ['#1d4ed8', 'Şirket Çifti', ciftler.size, 'kaynak → hedef'], ['#16a34a', 'Dönem Kayıtları', kay.length, `${kisa(kay.reduce((t, k) => t + k.toplam, 0))} · ${kay.filter(k => k.durum === 'Faturalandı').length} faturalı`], ['#64748b', 'Tüm Yansıtma Kayıtları', (y.kayitlar || []).length, `${(y.kayitlar || []).filter(k => k.durum === 'İptal').length} iptal`]];
    } else if (s) {
      kpi = [['#0f172a', 'Personel Sayısı', s.personelSayisi, `${s.no} v${s.versiyon} · ${s.kapsamAd} · ${s.durum}`], ['#dc2626', 'Genel Maliyet', kisa(s.genelMaliyet), 'toplam personel maliyeti (kontrol toplamı)'], ['#0f766e', 'Ara Toplam', kisa(s.araToplam), 'fiili net ödeme (Net-X + Net-R)'],
        ['#7c3aed', 'Yansıtma Gereken', kisa(s.yansitmaGereken), 'SGK işvereni ≠ fiili görev yeri'], ['#1d4ed8', 'Başka Şirket Tarafından Ödenen', kisa(s.baskaSirketOdeme), 'SGK işvereni ≠ ödeme şirketi'], [s.dengesizFis || s.hataliSatir ? '#dc2626' : '#b45309', 'Toplam Borç / Alacak', kisa(s.toplamBorc), `alacak ${kisa(s.toplamAlacak)} · ${s.fisSayisi} fiş · ${s.satirSayisi} satır · ${s.dengesizFis} dengesiz · ${s.hataliSatir} hatalı`]];
    } else {
      kpi = [['#0f172a', 'Personel Sayısı', K.personel || 0, 'kaynak: Personel Maliyet Tablosu (aktif)'], ['#dc2626', 'Genel Maliyet', kisa(K.genelMaliyet), 'toplam personel maliyeti'], ['#0f766e', 'Ara Toplam', kisa(K.araToplam), 'fiili net ödeme'], ['#7c3aed', 'Yansıtma Gereken', kisa(K.yansitma), 'SGK işvereni ≠ fiili görev yeri'], ['#1d4ed8', 'Başka Şirket Tarafından Ödenen', kisa(K.baskaSirket), 'SGK işvereni ≠ ödeme şirketi'], ['#b45309', 'Ödeme Yapılmıyor Notu', K.odemeYok || 0, 'ödeme kaydı üretilmeyecek personel']];
    }
    $('#phKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${esc(k[1])}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
  }

  // ============================================================
  // MUHASEBE KAYITLARI — ayrıntılı (Personel Maliyet Raporu'ndaki mhSeciliCiz ile aynı veri; burada personel bazlı satırlar açık)
  // ============================================================
  const AKIS = ['Taslak', 'Simülasyon Oluşturuldu', 'Kontrol / Analiz', 'Onaya Gönderildi', 'Onaylandı', 'Muhasebeleştirildi', 'Ters Kayıt'];
  const xrRozet = x => rozet(x === '*' ? 'X+R' : x, x === 'X' ? 'bekle' : x === 'R' ? 'ok' : 'gri');
  function kayitlarCiz() {
    const m = M(), s = sim(); seciliSim = s ? s.id : null; detay = $('#phDetay').checked;
    const adim = !s ? 0 : s.durum === 'Simülasyon Oluşturuldu' ? 1 : (s.durum === 'Kontrol Gerekiyor' || s.durum === 'Kontrole Hazır' || s.durum === 'Güncelliğini Yitirdi') ? 2 : s.durum === 'Onaya Gönderildi' ? 3 : s.durum === 'Onaylandı' ? 4 : s.durum === 'Muhasebeleştirildi' ? 5 : s.durum === 'Ters Kayıt Yapıldı' ? 6 : 0;
    $('#phAkis').innerHTML = AKIS.map((a, i) => `<span class="${i < adim ? 'oldu' : i === adim ? (s && (s.durum === 'Kontrol Gerekiyor' || s.durum === 'Güncelliğini Yitirdi') && i === 2 ? 'hata' : 'simdi') : ''}">${i + 1}. ${a}${s && i === adim ? ' · ' + esc(s.durum) : ''}</span>`).join('') + (s && s.durum === 'İptal' ? '<span class="hata">İptal</span>' : '');
    $('#phSimTablo tbody').innerHTML = m.simulasyonlar.map(x => `<tr data-id="${x.id}" class="${s && x.id === s.id ? 'secili' : ''}"><td><b>${esc(x.no)}</b></td><td>v${x.versiyon}</td><td>${esc(x.donemAd)}</td><td>${esc(x.kapsamAd)}</td><td class="s">${x.personelSayisi}</td><td class="s">${x.fisSayisi}</td><td class="s">${x.satirSayisi}</td><td class="s">${kisa(x.toplamBorc)}</td><td class="s">${kisa(x.toplamAlacak)}</td><td>${rozet(x.durum)}</td><td>${esc(x.olusturan)}<small class="alt">${esc(x.olusturma)}</small></td><td>${x.onayaGonderen ? esc(x.onayaGonderen) + `<small class="alt">${esc(x.onayaGonderme)}</small>` : '—'}</td><td>${x.onaylayan ? esc(x.onaylayan) + `<small class="alt">${esc(x.onayTarihi)}</small>` : '—'}</td><td>${x.muhasebelestiren ? esc(x.muhasebelestiren) + `<small class="alt">${esc(x.muhasebeTarihi)}</small>` : '—'}</td><td>${x.guncel ? rozet('Güncel') : rozet('Veri Değişti', 'hata')}</td></tr>`).join('') || '<tr><td colspan="15" class="na-bos">Bu dönem için simülasyon yok (kapsamda aktif personel bulunamadı).</td></tr>';
    seciliCiz(s);
    $('#phKayitSayi').textContent = s ? `${s.no} v${s.versiyon} · ${s.durum} · ${s.fisSayisi} fiş · ${s.satirSayisi} satır · ${m.simulasyonlar.length} simülasyon · ${V.donemAd}` : `${V.donemAd} · simülasyon yok`;
  }
  function seciliCiz(s) {
    const el = $('#phSecili'); if (!s) { el.innerHTML = ''; return; }
    const m = M(), yet = m.yetkili, acik = s.durum !== 'İptal', a = ara();
    const btn = (id, ikon, yazi, aktif, sinif) => `<button type="button" class="btn ${sinif || 'btn-light'} btn-sm py-0" id="${id}" ${aktif ? '' : 'disabled'}><i class="fas ${ikon} me-1"></i>${yazi}</button>`;
    const aksiyon = `<div class="pm-aksiyon">${btn('phHata', 'fa-triangle-exclamation', hata ? 'Tüm Satırlar' : 'Hataları Göster', true)}${btn('phOnaya', 'fa-paper-plane', 'Onaya Gönder', acik && s.durum === 'Kontrole Hazır' && s.guncel)}${btn('phOnayla', 'fa-check-double', 'Onayla', acik && s.durum === 'Onaya Gönderildi' && !s.kritikHata && s.guncel && yet, 'btn-warning')}${btn('phKaydet', 'fa-book', 'Muhasebeleştir', acik && s.durum === 'Onaylandı' && s.guncel && yet, 'btn-warning')}${btn('phTers', 'fa-rotate-left', 'Ters Kayıt Oluştur', s.durum === 'Muhasebeleştirildi' && yet)}${btn('phIptal', 'fa-ban', 'İptal', acik && s.durum !== 'Muhasebeleştirildi' && s.durum !== 'Ters Kayıt Yapıldı')}<select class="form-select" id="phFisF" style="font-size:.66rem;height:24px;min-height:24px;width:auto;padding:1px 22px 1px 6px"><option value="">Tüm Fişler</option>${s.fisler.map(f => `<option value="${esc(f.kod)}" ${f.kod === fisF ? 'selected' : ''}>${esc(f.kod)} · ${esc(f.sirket)} · ${esc(f.islemGrubu)}</option>`).join('')}</select><span class="text-muted" style="font-size:.64rem">${s.kritikHata ? 'Kritik hata var: Onayla kapalı.' : !s.guncel && acik && s.durum !== 'Muhasebeleştirildi' && s.durum !== 'Ters Kayıt Yapıldı' ? 'Kaynak veri / eşleştirme değişti: Yeniden Simüle gerekir.' : s.durum === 'Onaylandı' ? 'İçerik kilitli; Muhasebeleştir gerçek fişleri oluşturur.' : s.durum === 'Muhasebeleştirildi' ? 'Fişler muhasebeleşti; düzeltme için Ters Kayıt.' : ''}</span></div>`;
    const kontrol = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-list-check me-1"></i>Simülasyon Kontrolleri — Onay Öncesi Zorunlu Testler</span><span class="not">kritik hata varsa onay kapalı · hatalı satıra tıklayınca hatalı satırlar listelenir</span></div><div class="na-kart-g p-0"><table class="pm-kontrol" data-dinamik="kapali"><tbody>${s.kontroller.map(k => `<tr class="${k.gecti ? '' : k.seviye === 'Kritik' ? 'hata' : 'uyari'}"><td class="n">${k.no}</td><td><b>${esc(k.ad)}</b></td><td>${rozet(k.seviye, k.seviye === 'Kritik' ? 'hata' : 'bekle')}</td><td>${k.gecti ? rozet('Geçti') : rozet(k.seviye === 'Kritik' ? 'Hata' : 'Uyarı', k.seviye === 'Kritik' ? 'hata' : 'bekle')}${k.adet ? ` <small>(${k.adet})</small>` : ''}</td><td>${esc(k.mesaj)}</td></tr>`).join('')}</tbody></table></div></div>`;
    const fisler = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-file-invoice me-1"></i>Fişler (Şirket Bazında)</span><span class="not">her tüzel şirket için ayrı fiş · satıra tıklayınca fiş önizleme</span></div><div class="na-kart-g p-0"><section class="pm-kaydir" style="max-height:36vh"><table class="na-tablo pm-excel pm-tik" id="phFisTablo" data-dinamik="kapali"><thead><tr class="tek"><th>Fiş</th><th>Şirket</th><th>İşlem Grubu</th><th class="s">Personel</th><th class="s">Satır</th><th class="s">Borç</th><th class="s">Alacak</th><th class="s">Fark</th><th>Denge</th><th class="s">Hatalı Satır</th><th>Muhasebe Fiş No</th><th>Kayıt Tarihi</th><th>Ters Fiş</th><th>Veritabanı</th></tr></thead><tbody>${s.fisler.map(f => `<tr class="${f.dengeli && !f.hataliSatir ? '' : 'hatali'}" data-fis="${esc(f.kod)}"><td><b>${esc(f.kod)}</b></td><td>${esc(f.sirket)}</td><td>${esc(f.islemGrubu)} · ${esc(f.baslik)}</td><td class="s">${f.personel}</td><td class="s">${f.satir}</td><td class="s">${kisa(f.toplamBorc)}</td><td class="s">${kisa(f.toplamAlacak)}</td><td class="s">${f.fark ? `<span class="text-danger">${kisa(f.fark)}</span>` : '—'}</td><td>${f.dengeli ? rozet('Dengeli') : rozet('Dengesiz', 'hata')}</td><td class="s">${f.hataliSatir || '—'}</td><td>${f.muhasebeFisNo ? `<b>${esc(f.muhasebeFisNo)}</b>` : '<span class="text-muted">— (onay öncesi fiş no yok)</span>'}</td><td class="kucuk">${esc(f.kayitTarihi || '—')}</td><td>${f.tersFisNo ? `<b>${esc(f.tersFisNo)}</b><small class="alt">${esc(f.tersTarihi)}</small>` : '—'}</td><td class="kucuk">${esc(f.db || '')}</td></tr>`).join('')}</tbody><tfoot><tr><td>Toplam</td><td>${s.fisler.length} Fiş</td><td></td><td></td><td class="s">${s.satirSayisi}</td><td class="s">${kisa(s.toplamBorc)}</td><td class="s">${kisa(s.toplamAlacak)}</td><td class="s">${kisa(s.toplamBorc - s.toplamAlacak)}</td><td></td><td class="s">${s.hataliSatir}</td><td></td><td></td><td></td><td></td></tr></tfoot></table></section></div></div>`;
    const L2 = !detay ? [] : (s.satirlar || []).filter(x => kisiUyar(x) && (!hata || x.hatalar.length) && (!fisF || x.fisKodu === fisF) && (!a || kucukTr([x.adSoyad, x.hesapKodu, x.hesapAdi, x.kaynakKalem, x.islemTuru, x.sirket, x.karsiSirket, x.fisKodu, x.sgkIsvereni, x.fiiliGorevYeri, x.odemeSirketi].join(' ')).includes(a)));
    const satirlar = !detay ? '<div class="pm-not">Personel bazlı satırlar gizli; görmek için şeritteki "Personel Bazlı Satırlar" anahtarını açın.</div>' : `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-table-list me-1"></i>Personel Bazlı Satırlar — Ayrıntı</span><span class="not">${L2.length} / ${(s.satirlar || []).length} satır · üstteki Ara, SGK İşvereni / Fiili Görev Yeri / Ödeme Şirketi ve fiş filtreleri geçerli · personel adına tıklayınca cari kartı · muhasebeye bu satırlar değil, özet kayıtlar atılır</span></div><div class="na-kart-g p-0"><section class="pm-kaydir" style="max-height:56vh"><table class="na-tablo pm-excel pm-tik" id="phSatirTablo" data-dinamik="kapali"><thead><tr class="tek"><th>Sim. No</th><th>Fiş</th><th>Şirket</th><th>Adı-Soyadı</th><th>SGK İşvereni</th><th>Fiili Görev Yeri</th><th>Ödeme Şirketi</th><th>İşlem Türü</th><th>X/R</th><th>Kaynak Kalem</th><th>Hesap Kodu</th><th>Hesap Adı</th><th class="s">Borç</th><th class="s">Alacak</th><th>Karşı Şirket</th><th>Kontrol</th><th>Açıklama</th></tr></thead><tbody>${L2.slice(0, 1500).map(x => `<tr class="${x.hatalar.length ? 'hatali' : ''}" data-kod="${esc(x.cariKodu || '')}" title="${esc(x.hatalar.join(' · '))}"><td class="kucuk">${esc(x.simNo)}</td><td><b>${esc(x.fisKodu)}</b></td><td>${esc(x.sirket)}</td><td class="ad">${esc(x.adSoyad)}<small class="alt">${esc(x.cariKodu || '')}</small></td><td>${bos(x.sgkIsvereni)}</td><td>${bos(x.fiiliGorevYeri)}</td><td>${bos(x.odemeSirketi)}</td><td>${esc(x.islemTuru)}</td><td>${xrRozet(x.xr)}</td><td>${esc(x.kaynakKalem)}</td><td><b>${esc(x.hesapKodu)}</b></td><td><small>${esc(x.hesapAdi)}</small></td><td class="s">${hucre(x.borc)}</td><td class="s">${hucre(x.alacak)}</td><td>${bos(x.karsiSirket)}</td><td>${x.hatalar.length ? rozet('✗ ' + x.hatalar[0], 'hata') : rozet('✓')}</td><td><small>${esc(x.aciklama)}</small></td></tr>`).join('') || '<tr><td colspan="17" class="na-bos">Filtreye uyan satır yok.</td></tr>'}</tbody><tfoot><tr><td>Toplam</td><td colspan="11">${L2.length} satır${L2.length > 1500 ? ' (ilk 1.500 gösteriliyor)' : ''}</td><td class="s">${kisa(L2.reduce((t, x) => t + (Number(x.borc) || 0), 0))}</td><td class="s">${kisa(L2.reduce((t, x) => t + (Number(x.alacak) || 0), 0))}</td><td></td><td></td><td></td></tr></tfoot></table></section></div></div>`;
    const O = s.fisler.flatMap(f => f.hesapOzeti.map(h => ({ ...h, fisKodu: f.kod, sirket: f.sirket, islemGrubu: f.islemGrubu, baslik: f.baslik }))).filter(h => (!hata || h.hatali) && (!fisF || h.fisKodu === fisF) && (!a || kucukTr([h.hesapKodu, h.hesapAdi, h.sirket, h.karsiSirket, h.baslik, h.fisKodu].join(' ')).includes(a)));
    const ozet = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-shield-halved me-1"></i>Özet Kayıtlar — Muhasebeye Atılacak Hâl (Hesap + X/R + Karşı Şirket)</span><span class="not">${O.length} kayıt · personel adı içermez · raporda varsayılan görünüm budur · satıra tıklayınca fiş önizleme</span></div><div class="na-kart-g p-0"><section class="pm-kaydir" style="max-height:44vh"><table class="na-tablo pm-excel pm-tik" id="phOzetTablo" data-dinamik="kapali"><thead><tr class="tek"><th>Fiş</th><th>Şirket</th><th>İşlem Grubu</th><th>X/R</th><th>Hesap Kodu</th><th>Hesap Adı</th><th class="s">Borç</th><th class="s">Alacak</th><th>Karşı Şirket</th><th class="s">Personel</th><th>Kontrol</th></tr></thead><tbody>${O.map(h => `<tr class="${h.hatali ? 'hatali' : ''}" data-fis="${esc(h.fisKodu)}"><td><b>${esc(h.fisKodu)}</b></td><td>${esc(h.sirket)}</td><td>${esc(h.islemGrubu)} · ${esc(h.baslik)}</td><td>${xrRozet(h.xr)}</td><td><b>${esc(h.hesapKodu)}</b></td><td>${esc(h.hesapAdi)}</td><td class="s">${hucre(h.borc)}</td><td class="s">${hucre(h.alacak)}</td><td>${bos(h.karsiSirket)}</td><td class="s">${h.personel}</td><td>${h.hatali ? rozet(`✗ ${h.hatali} satır`, 'hata') : rozet('✓')}</td></tr>`).join('') || '<tr><td colspan="11" class="na-bos">Filtreye uyan kayıt yok.</td></tr>'}</tbody><tfoot><tr><td>Toplam</td><td colspan="5">${O.length} kayıt</td><td class="s">${kisa(O.reduce((t, x) => t + (Number(x.borc) || 0), 0))}</td><td class="s">${kisa(O.reduce((t, x) => t + (Number(x.alacak) || 0), 0))}</td><td></td><td></td><td></td></tr></tfoot></table></section></div></div>`;
    const audit = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clipboard-list me-1"></i>İzlenebilirlik / Audit Log</span><span class="not">${esc(s.no)} v${s.versiyon} · eşleştirme v${s.eslemeVersiyonu} · raporla ortak</span></div><div class="na-kart-g"><ul class="pm-zaman">${s.audit.map(x => `<li><b>${esc(x.islem)}</b> — ${esc(x.detay)}<small>${esc(x.tarih)} · ${esc(x.kullanici)}</small></li>`).join('')}</ul></div></div>`;
    el.innerHTML = aksiyon + kontrol + fisler + satirlar + ozet + audit;
    $('#phHata').addEventListener('click', () => { hata = !hata; seciliCiz(s); });
    $('#phFisF').addEventListener('change', e => { fisF = e.target.value; seciliCiz(s); });
    $('#phOnaya').addEventListener('click', () => islem(s.id, 'onayaGonder')); $('#phOnayla').addEventListener('click', () => islem(s.id, 'onayla'));
    $('#phKaydet').addEventListener('click', () => { if (window.confirm(`${s.no} v${s.versiyon}: ${s.fisler.length} fiş gerçek muhasebe kaydına dönüştürülecek. Devam edilsin mi?`)) islem(s.id, 'muhasebelestir'); });
    $('#phTers').addEventListener('click', () => { const n = window.prompt('Ters kayıt nedeni (zorunlu):'); if (n && n.trim()) islem(s.id, 'tersKayit', n.trim()); });
    $('#phIptal').addEventListener('click', () => { const n = window.prompt('İptal nedeni:'); if (n !== null) islem(s.id, 'iptal', n); });
    $$('.pm-kontrol tr.hata, .pm-kontrol tr.uyari', el).forEach(tr => { tr.style.cursor = 'pointer'; tr.addEventListener('click', () => { if (!hata) { hata = true; seciliCiz(s); } }); });
  }
  async function islem(id, isl, not) {
    const j = await post('/PersonelMuhasebe/Islem', { id, islem: isl, not: not || '', donem: donem() }); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    seciliSim = id; veriUygula(j); mesaj('success', esc(j.message));
  }
  /** fiş önizleme (çekmece): hesap bazında toplanmış borç / alacak + personel bazlı satırlar */
  function fisOnizle(s, kod) {
    const f = s.fisler.find(x => x.kod === kod); if (!f) return;
    const satirlar = (s.satirlar || []).filter(x => x.fisKodu === kod);
    drawerAc(`<i class="fas fa-file-invoice me-1"></i>${esc(s.no)} v${s.versiyon} · ${esc(f.kod)} · ${esc(f.sirket)} ${f.dengeli ? rozet('Dengeli') : rozet('Dengesiz', 'hata')}${f.muhasebeFisNo ? ' ' + rozet('Fiş ' + f.muhasebeFisNo) : ''}${f.tersFisNo ? ' ' + rozet('Ters ' + f.tersFisNo, 'mavi') : ''}`,
      f.muhasebeFisNo ? `<a class="btn btn-light btn-sm py-0" href="${esc(M().muhasebeEkrani || '/Muhasebe')}" target="_blank"><i class="fas fa-up-right-from-square me-1"></i>Muhasebe Fişleri</a>` : '',
      `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-scale-balanced me-1"></i>${esc(f.islemGrubu)} · ${esc(f.baslik)}</span><span class="not">${esc(V.donemAd)} · ${f.personel} personel · ${f.satir} satır · veritabanı ${esc(f.db)} · muhasebeye bu özet hâliyle atılır</span></div><div class="na-kart-g p-0"><table class="pm-tfis" data-dinamik="kapali"><thead><tr><th>Hesap Kodu</th><th>Hesap Adı</th><th>X/R</th><th>Karşı Şirket</th><th class="s">Borç</th><th class="s">Alacak</th><th class="s">Personel</th></tr></thead><tbody>${f.hesapOzeti.map(h => `<tr><td><b>${esc(h.hesapKodu)}</b></td><td>${esc(h.hesapAdi)}</td><td>${esc(h.xr)}</td><td>${bos(h.karsiSirket)}</td><td class="s">${hucre(h.borc)}</td><td class="s">${hucre(h.alacak)}</td><td class="s">${h.personel}</td></tr>`).join('')}<tr class="toplam"><td colspan="4">Toplam</td><td class="s">${kisa(f.toplamBorc)}</td><td class="s">${kisa(f.toplamAlacak)}</td><td class="s">${f.personel}</td></tr></tbody></table></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-users me-1"></i>Personel Bazında Satırlar</span><span class="not">${satirlar.length} satır · SGK işvereni / fiili görev yeri / ödeme şirketi · kaynak kalem · X/R · karşı şirket</span></div><div class="na-kart-g p-0"><section class="pm-kaydir" style="max-height:52vh"><table class="pm-tfis" data-dinamik="kapali"><thead><tr><th>Adı-Soyadı</th><th>SGK İşvereni</th><th>Fiili Görev Yeri</th><th>Ödeme Şirketi</th><th>Kalem</th><th>X/R</th><th>Hesap</th><th class="s">Borç</th><th class="s">Alacak</th><th>Karşı</th><th>K.</th></tr></thead><tbody>${satirlar.map(x => `<tr><td>${esc(x.adSoyad)}</td><td>${bos(x.sgkIsvereni)}</td><td>${bos(x.fiiliGorevYeri)}</td><td>${bos(x.odemeSirketi)}</td><td>${esc(x.kaynakKalem)}</td><td>${esc(x.xr)}</td><td>${esc(x.hesapKodu)}</td><td class="s">${hucre(x.borc)}</td><td class="s">${hucre(x.alacak)}</td><td>${bos(x.karsiSirket)}</td><td>${x.hatalar.length ? '<span class="text-danger" title="' + esc(x.hatalar.join(' · ')) + '">✗</span>' : '✓'}</td></tr>`).join('') || '<tr><td colspan="11" class="na-bos">Satır yok.</td></tr>'}</tbody></table></section></div></div>`);
  }

  // ============================================================
  // ÖDEMELER — net ödeme listesi (V.odemeler: simülasyon C / D grubu banka ve kasa satırları)
  // ============================================================
  function odemeCiz() {
    const O = V.odemeler || { satirlar: [], odemeYok: [] }, a = ara(), sirket = $('#phOdemeSirket').value, kanal = $('#phOdemeKanal').value;
    const sel = $('#phOdemeSirket'), v = sel.value; sel.innerHTML = '<option value="">Tüm Ödeyen Şirketler</option>' + [...new Set(O.satirlar.map(x => x.odeyen))].sort((p, q) => p.localeCompare(q, 'tr')).map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); sel.value = [...sel.options].some(o => o.value === v) ? v : '';
    const L = O.satirlar.filter(x => kisiUyar(x) && (!sirket || x.odeyen === sirket) && (!kanal || x.kanal === kanal) && (!a || kucukTr([x.adSoyad, x.odeyen, x.sgkIsvereni, x.fiiliGorevYeri, x.odemeSirketi, x.hesapKodu, x.hesapAdi, x.fisKodu, x.muhasebeFisNo].join(' ')).includes(a)));
    const durumAd = O.odenebilir ? 'Muhasebeleşti · Ödenebilir' : O.durum || '—';
    $('#phOdemeTablo tbody').innerHTML = L.map((x, i) => `<tr class="${x.hatali ? 'hatali' : ''}" data-sira="${x.personelSira}"><td class="kucuk">${i + 1}</td><td><b>${esc(x.odeyen)}</b></td><td>${rozet(x.kanal, x.xr === 'X' ? 'bekle' : 'ok')}</td><td class="ad">${esc(x.adSoyad)}</td><td>${esc(x.sgkIsvereni)}${x.islemGrubu === 'D' ? '<small class="alt">başka şirket ödüyor (D)</small>' : ''}</td><td>${bos(x.fiiliGorevYeri)}</td><td>${bos(x.odemeSirketi)}</td><td><b>${esc(x.hesapKodu)}</b><small class="alt">${esc(x.hesapAdi)}</small></td><td class="s hesap">${kisa(x.tutar)}</td><td>${esc(x.fisKodu)}</td><td>${x.muhasebeFisNo ? `<b>${esc(x.muhasebeFisNo)}</b>${x.tersFisNo ? `<small class="alt">ters ${esc(x.tersFisNo)}</small>` : ''}` : '<span class="text-muted">—</span>'}</td><td>${rozet(x.tersFisNo ? 'Ters Kayıt Yapıldı' : durumAd, x.tersFisNo ? 'mavi' : O.odenebilir ? 'ok' : 'bekle')}</td></tr>`).join('') || '<tr><td colspan="12" class="na-bos">Filtreye uyan ödeme satırı yok.</td></tr>';
    $('#phOdemeTablo tfoot tr').innerHTML = `<td>Toplam</td><td>${new Set(L.map(x => x.odeyen)).size} şirket</td><td></td><td>${new Set(L.map(x => x.personelSira)).size} kişi</td><td></td><td></td><td></td><td></td><td class="s">${kisa(L.reduce((t, x) => t + (Number(x.tutar) || 0), 0))}</td><td></td><td></td><td></td>`;
    const grp = {}; L.forEach(x => { const k = x.odeyen + '|' + x.kanal; (grp[k] = grp[k] || { odeyen: x.odeyen, kanal: x.kanal, kisi: new Set(), tutar: 0 }); grp[k].kisi.add(x.personelSira); grp[k].tutar += Number(x.tutar) || 0; });
    const G = Object.values(grp).sort((p, q) => p.odeyen.localeCompare(q.odeyen, 'tr') || p.kanal.localeCompare(q.kanal, 'tr'));
    $('#phOdemeOzet tbody').innerHTML = G.map(g => `<tr><td><b>${esc(g.odeyen)}</b></td><td>${rozet(g.kanal, g.kanal.startsWith('Kasa') ? 'bekle' : 'ok')}</td><td class="s">${g.kisi.size}</td><td class="s hesap">${kisa(g.tutar)}</td></tr>`).join('') + (G.length ? `<tr class="toplam"><td>Toplam</td><td></td><td class="s">${new Set(L.map(x => x.personelSira)).size}</td><td class="s">${kisa(L.reduce((t, x) => t + (Number(x.tutar) || 0), 0))}</td></tr>` : '<tr><td colspan="4" class="na-bos">Kayıt yok.</td></tr>');
    const Yk = (O.odemeYok || []).filter(x => kisiUyar(x) && (!a || kucukTr([x.adSoyad, x.sgkIsvereni, x.fiiliGorevYeri, x.odemeSirketi, x.odemeSirketiX, x.not].join(' ')).includes(a)));
    $('#phOdemeYok tbody').innerHTML = Yk.map(x => `<tr data-kod="${esc(x.cariKodu || '')}"><td class="ad">${esc(x.adSoyad)}</td><td>${esc(x.sgkIsvereni)}</td><td>${bos(x.fiiliGorevYeri)}</td><td><small>R: ${bos(x.odemeSirketi)}<br>X: ${bos(x.odemeSirketiX)}</small></td><td class="s">${hucre(x.netR)}</td><td class="s">${hucre(x.netX)}</td><td><small>${esc(x.not || '')}</small></td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Ödeme engelleyen notu olan personel yok.</td></tr>';
    $('#phOdemeYokSayi').textContent = `${(O.odemeYok || []).length} personel · tahakkuk var, ödeme kaydı yok`;
    $('#phOdemeSayi').textContent = `${L.length} / ${O.satirlar.length} ödeme satırı · ${O.simNo ? `${O.simNo} v${O.versiyon} · ${O.durum}` : 'simülasyon yok'} · ${V.donemAd}`;
  }

  // ============================================================
  // YANSITMA HAZIRLIĞI (eski Personel Maliyet Yansıtması › Hazırlık; V.yansitma)
  // ============================================================
  const secilebilir = a => a.durum !== 'Kaydedildi' && a.durum !== 'Kalem Yok';
  let HL = [];
  function adayListesi() {
    const a = ara(), v = id => $(id).value;
    return (Y().adaylar || []).filter(x => kisiUyar(x) && (!v('#phHHedef') || x.hedefler.some(h => h.sirket === v('#phHHedef'))) && (!v('#phHCati') || x.catiDepartman === v('#phHCati')) && (!v('#phHDurum') || x.durum === v('#phHDurum'))
      && (!a || kucukTr([x.adSoyad, x.gorev, x.cariKodu, x.unvan, x.sgkIsvereni, x.fiiliGorevYeri, x.odemeSirketi].join(' ')).includes(a)));
  }
  function detayHtml(a) {
    const y = Y();
    const kal = a.kalemler.length ? `<table class="na-tablo ph-kalem" data-dinamik="kapali"><thead><tr><th>Maliyet Alanı</th><th>X/R</th><th>Gider Kodu</th><th>Gider Kalemi</th><th>Yansıtma Kodu</th><th>Yansıtma Gelir Kalemi</th><th class="s">Tutar (%100)</th>${a.hedefler.map(h => `<th class="s">${esc(h.sirket)} %${pct(h.pay)}</th>`).join('')}</tr></thead><tbody>${a.kalemler.map(k => `<tr><td>${esc(k.alanAdi)}</td><td>${esc(k.aktarimTipi || '—')}</td><td class="kod">${esc(k.giderKodu)}</td><td>${esc(k.giderKalem)}</td><td class="kod">${esc(k.yansitmaKodu)}</td><td>${esc(k.yansitmaKalem)}</td><td class="s">${tl(k.tutar)}</td>${a.hedefler.map(h => `<td class="s">${tl(Math.round(k.tutar * h.pay) / 100)}</td>`).join('')}</tr>`).join('')}<tr class="top"><td colspan="6"><b>Toplam</b></td><td class="s"><b>${tl(a.maliyet)}</b></td>${a.hedefler.map(h => `<td class="s"><b>${tl(h.tutar)}</b></td>`).join('')}</tr></tbody></table>` : '<div class="na-bos">Maliyet kalemi yok (tutarlar 0).</div>';
    const dag = `<div class="ph-dagilim mt-2"><b>Görev Yeri Dağılımı${a.dagilimOzel ? ' (Özel)' : ''}:</b>${a.dagilim.map((d, i) => `<span>${esc(d.sirket)} <input type="number" min="0" max="100" step="0.5" data-sira="${a.sira}" data-i="${i}" data-sirket="${esc(d.sirket)}" value="${d.pay}"> %</span>`).join('')}<select class="form-select" data-ek="${a.sira}"><option value="">+ Şirket Ekle</option>${(y.sirketler || []).filter(s => !a.dagilim.some(d => d.sirket === s)).map(s => `<option value="${esc(s)}">${esc(s)}</option>`).join('')}</select><button type="button" class="btn btn-light btn-sm py-0 dagKaydet" data-sira="${a.sira}" style="font-size:.64rem"><i class="fas fa-floppy-disk me-1"></i>Dağılımı Kaydet</button>${a.dagilimOzel ? `<button type="button" class="btn btn-light btn-sm py-0 dagSifirla" data-sira="${a.sira}" style="font-size:.64rem">Varsayılana Dön</button>` : ''}<a class="btn btn-light btn-sm py-0" href="${kartAdresi(a.cariKodu)}" target="_blank" style="font-size:.64rem"><i class="fas fa-id-card me-1"></i>Personel Kartı</a><span class="text-muted">SGK işvereni (${esc(a.kaynakSirket)}) payı yansıtılmaz; toplam %100 olmalı.</span></div>`;
    const kayit = a.hedefler.filter(h => h.kayit).map(h => `<span class="pm-pill">${esc(h.sirket)}: ${esc(h.kayit)}</span>`).join(' ');
    return `<div style="overflow:auto">${kal}</div>${dag}${kayit ? `<div class="mt-2" style="font-size:.66rem">Bu dönem kayıtları: ${kayit} <button type="button" class="btn btn-light btn-sm py-0 kayitAc" data-no="${esc(a.hedefler.find(h => h.kayit).kayit)}" style="font-size:.62rem">Kaydı Aç</button></div>` : ''}`;
  }
  function hepsiGuncelle() { const s = HL.filter(secilebilir), h = $('#phHepsi'); h.disabled = !s.length; h.checked = s.length > 0 && s.every(a => secili.has(a.sira)); }
  function hazirlikCiz() {
    const y = Y();
    secili = new Set([...secili].filter(s => (y.adaylar || []).some(a => a.sira === s && secilebilir(a))));
    HL = adayListesi();
    $('#phAdayTablo tbody').innerHTML = HL.map(a => `<tr class="ana ${a.durum === 'Kaydedildi' ? 'kaydedildi' : ''}" data-sira="${a.sira}"><td><input type="checkbox" class="sec" data-sira="${a.sira}" ${secili.has(a.sira) ? 'checked' : ''} ${a.durum === 'Kaydedildi' ? 'disabled title="Bu dönemde kaydedildi"' : a.durum === 'Kalem Yok' ? 'disabled title="Maliyet kalemi yok (tutarlar 0)"' : ''}></td><td class="ad">${esc(a.adSoyad)}<small class="alt">${esc(a.cariKodu)}</small></td><td>${esc(a.unvan)}<small class="alt">${esc(a.gorev)}</small></td><td>${esc(a.catiDepartman)}</td><td>${esc(a.sgkIsvereni)}</td><td>${esc(a.fiiliGorevYeri)}</td><td>${bos(a.odemeSirketi)}</td><td>${a.hedefler.map(h => yon(a.kaynakSirket, h.sirket, h.pay)).join('<br>')}</td><td class="s">${kisa(a.maliyet)}</td><td class="s">%${pct(a.yansitilacakPay)}</td><td class="s hesap">${kisa(a.yansitilacak)}</td><td class="s">${a.kalemler.length}</td><td>${rozet(a.durum)}</td></tr>${acikDetay.has(a.sira) ? `<tr class="detay" data-sira="${a.sira}"><td colspan="13">${detayHtml(a)}</td></tr>` : ''}`).join('') || '<tr><td colspan="13" class="na-bos">Bu dönemde yansıtma gerektiren personel yok (SGK işvereni ile fiili görev yeri aynı).</td></tr>';
    $('#phTopMaliyet').textContent = kisa(HL.reduce((s, a) => s + a.maliyet, 0)); $('#phTopYansitma').textContent = kisa(HL.reduce((s, a) => s + a.yansitilacak, 0)); $('#phTopKisi').textContent = `${HL.length} Personel`;
    $('#phHazirlikSayi').textContent = `${HL.length} / ${(y.adaylar || []).length} personel · ${secili.size} seçili · ${V.donemAd}`;
    hepsiGuncelle(); kpiCiz();
  }
  async function dagilimKaydet(sira, dagilim) {
    const j = await post('/PersonelMuhasebe/Dagilim', { sira, donem: donem(), dagilim }); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    veriUygula(j); mesaj('success', esc(j.message));
  }
  $('#phAdayTablo').addEventListener('click', async e => {
    const cb = e.target.closest('input.sec'); if (cb) { if (cb.checked) secili.add(+cb.dataset.sira); else secili.delete(+cb.dataset.sira); $('#phHazirlikSayi').textContent = `${HL.length} / ${(Y().adaylar || []).length} personel · ${secili.size} seçili · ${V.donemAd}`; hepsiGuncelle(); return; }
    if (e.target.closest('input, select, button, a')) {
      const ks = e.target.closest('button.dagKaydet'); if (ks) { const sira = +ks.dataset.sira; await dagilimKaydet(sira, $$(`#phAdayTablo tr.detay[data-sira="${sira}"] .ph-dagilim input[data-sira]`).map(i => ({ sirket: i.dataset.sirket, pay: Number(i.value) || 0 }))); return; }
      const sf = e.target.closest('button.dagSifirla'); if (sf) { await dagilimKaydet(+sf.dataset.sira, []); return; }
      const ka = e.target.closest('button.kayitAc'); if (ka) { sekme('yansitma'); kayitDrawer(ka.dataset.no); return; }
      return;
    }
    const tr = e.target.closest('tr.ana'); if (!tr) return;
    const s = +tr.dataset.sira; if (acikDetay.has(s)) acikDetay.delete(s); else acikDetay.add(s); hazirlikCiz();
  });
  $('#phAdayTablo').addEventListener('change', e => {
    const sel = e.target.closest('select[data-ek]'); if (!sel || !sel.value) return;
    const sira = +sel.dataset.ek, a = (Y().adaylar || []).find(x => x.sira === sira); if (!a) return;
    a.dagilim = $$(`#phAdayTablo tr.detay[data-sira="${sira}"] .ph-dagilim input[data-sira]`).map(i => ({ sirket: i.dataset.sirket, pay: Number(i.value) || 0 }));
    a.dagilim.push({ sirket: sel.value, pay: 0 }); hazirlikCiz();
  });
  $('#phHepsi').addEventListener('change', () => { HL.filter(secilebilir).forEach(a => { if ($('#phHepsi').checked) secili.add(a.sira); else secili.delete(a.sira); }); hazirlikCiz(); });
  $('#phTumunuSec').addEventListener('click', () => { HL.filter(secilebilir).forEach(a => secili.add(a.sira)); hazirlikCiz(); });
  $('#phSecimTemizle').addEventListener('click', () => { secili.clear(); hazirlikCiz(); });
  $('#phYansit').addEventListener('click', async () => {
    const y = Y(), siralar = [...secili].filter(s => (y.adaylar || []).some(a => a.sira === s && secilebilir(a)));
    if (!siralar.length) { mesaj('warning', 'Yansıtılacak personel seçin (kayıtsız satırlar).'); return; }
    const secilenler = y.adaylar.filter(a => siralar.includes(a.sira));
    if (!window.confirm(`${V.donemAd} dönemi için ${secilenler.length} personel, ${secilenler.reduce((s, a) => s + a.hedefler.length, 0)} yansıtma kaydı (${tl(secilenler.reduce((s, a) => s + a.yansitilacak, 0))} TL) oluşturulacak.\nHer kayıt SGK işvereni ve fiili görev yeri şirketine fiş olarak atılır. Devam edilsin mi?`)) return;
    const j = await post('/PersonelMuhasebe/YansitOlustur', { donem: donem(), siralar }); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    secili.clear(); veriUygula(j); mesaj('success', esc(j.message) + (j.yeniler && j.yeniler.length ? ` <button type="button" class="btn btn-light btn-sm py-0 ms-2" id="phKayitlaraGit" style="font-size:.64rem">Kayıtları Gör</button>` : ''));
    const g = $('#phKayitlaraGit'); if (g) g.addEventListener('click', () => { $('#phKDonem').value = donem(); sekme('yansitma'); });
  });
  dugmeBagla('#phHazirlikExcel', '#phHazirlikPdf', bicim => domExcel('personel_yansitma_hazirlik_' + donem(), [['phAdayTablo', 'Yansıtma Hazırlığı']], bicim));
  ['#phHHedef', '#phHCati', '#phHDurum'].forEach(s => $(s).addEventListener('change', hazirlikCiz));

  // ============================================================
  // YANSITMA KAYITLARI (+ şirket çifti matrisi + simülasyon B grubu mutabakatı)
  // ============================================================
  let KL = [];
  function kayitListesi() {
    const a = ara(), v = id => $(id).value;
    return (Y().kayitlar || []).filter(k => kisiUyar(k) && (!v('#phKDonem') || k.donem === v('#phKDonem')) && (!v('#phKHedef') || k.hedefSirket === v('#phKHedef')) && (!v('#phKDurum') || k.durum === v('#phKDurum'))
      && (!a || kucukTr([k.no, k.adSoyad, k.cariKodu, k.kaynakFisNo, k.hedefFisNo, k.faturaNo, k.kaynakSirket, k.hedefSirket, k.fiiliGorevYeri, k.odemeSirketi, k.odemeSirketiX].join(' ')).includes(a)));
  }
  function yansitmaCiz() {
    const y = Y(); KL = kayitListesi();
    $('#phYKayitTablo tbody').innerHTML = KL.map(k => `<tr data-no="${esc(k.no)}"><td><b>${esc(k.no)}</b></td><td>${esc(k.donemAdi)}</td><td class="ad">${esc(k.adSoyad)}<small class="alt">${esc(k.cariKodu)} · ${esc(k.gorev)}</small></td><td>${bos(k.sgkIsvereni)}</td><td>${bos(k.fiiliGorevYeri)}</td><td>${bos(k.odemeSirketi)}</td><td>${yon(k.kaynakSirket, k.hedefSirket)}</td><td class="s">%${pct(k.pay)}</td><td class="s">${kisa(k.net)}</td><td class="s">${k.kdv ? kisa(k.kdv) : '—'}</td><td class="s hesap">${kisa(k.toplam)}</td><td><small>${esc(k.kaynakFisNo)}</small></td><td><small>${esc(k.hedefFisNo)}</small></td><td>${bos(k.faturaNo)}</td><td>${rozet(k.durum)}</td><td class="kucuk">${esc(k.olusturan)}<small class="alt">${esc(k.zaman)}</small></td></tr>`).join('') || '<tr><td colspan="16" class="na-bos">Kayıt yok. Yansıtma Hazırlığı sekmesinden "Seçilenleri Yansıt" ile oluşturun.</td></tr>';
    const g = KL.filter(k => k.durum !== 'İptal');
    $('#phYKayitTablo tfoot tr').innerHTML = `<td>Toplam</td><td></td><td>${KL.length} kayıt</td><td></td><td></td><td></td><td>${g.length} aktif</td><td></td><td class="s">${kisa(g.reduce((s, k) => s + k.net, 0))}</td><td class="s">${kisa(g.reduce((s, k) => s + k.kdv, 0))}</td><td class="s">${kisa(g.reduce((s, k) => s + k.toplam, 0))}</td><td></td><td></td><td>${g.filter(k => k.durum === 'Faturalandı').length} faturalı</td><td></td><td></td>`;
    $('#phYKayitSayi').textContent = `${KL.length} / ${(y.kayitlar || []).length} kayıt · iptal hariç ${kisa(g.reduce((s, k) => s + k.toplam, 0))} · ${g.filter(k => k.durum === 'Faturalandı').length} faturalı`;
    // kaynak → hedef matrisi
    const kaynaklar = [...new Set(g.map(k => k.kaynakSirket))].sort(), hedefler = [...new Set(g.map(k => k.hedefSirket))].sort();
    const h = l => l.length ? `<b>${tam(l.reduce((s, k) => s + k.toplam, 0))}</b><small class="alt">${l.length} kayıt · ${new Set(l.map(k => k.personelSira)).size} kişi</small>` : '<span class="text-muted">—</span>';
    $('#phMatris thead').innerHTML = `<tr class="tek"><th>Kaynak ↓ / Hedef →</th>${hedefler.map(x => `<th class="s">${esc(x)}</th>`).join('')}<th class="s">Toplam</th></tr>`;
    $('#phMatris tbody').innerHTML = kaynaklar.length ? kaynaklar.map(ka => `<tr><td><b>${esc(ka)}</b></td>${hedefler.map(he => `<td class="s">${h(g.filter(k => k.kaynakSirket === ka && k.hedefSirket === he))}</td>`).join('')}<td class="s">${h(g.filter(k => k.kaynakSirket === ka))}</td></tr>`).join('') + `<tr class="toplam"><td>Toplam</td>${hedefler.map(he => `<td class="s">${h(g.filter(k => k.hedefSirket === he))}</td>`).join('')}<td class="s">${h(g)}</td></tr>` : '<tr><td class="na-bos">Kayıt yok.</td></tr>';
    mutabakatCiz();
    kpiCiz();
  }
  /** simülasyonun B grubu ("Yansıtma Geliri" satırları: yansıtan şirket → karşı şirket, genel maliyet) ↔ seçili dönemin yansıtma kayıtları (katalog kalemleri, net) */
  function mutabakatCiz() {
    const s = sim(), d = donem(), M2 = {};
    if (s) (s.satirlar || []).filter(x => x.islemGrubu === 'B' && x.kaynakKalem === 'Yansıtma Geliri').forEach(x => { const k = x.sirket + '→' + (x.karsiSirket || '?'); (M2[k] = M2[k] || { kaynak: x.sirket, hedef: x.karsiSirket || '?', sim: 0, kayit: 0, adet: 0, kisi: 0 }); M2[k].sim += Number(x.alacak) || 0; M2[k].kisi++; });
    (Y().kayitlar || []).filter(k => k.donem === d && k.durum !== 'İptal').forEach(k => { const key = k.kaynakSirket + '→' + k.hedefSirket; (M2[key] = M2[key] || { kaynak: k.kaynakSirket, hedef: k.hedefSirket, sim: 0, kayit: 0, adet: 0, kisi: 0 }); M2[key].kayit += Number(k.net) || 0; M2[key].adet++; });
    const R = Object.values(M2).sort((p, q) => (p.kaynak + p.hedef).localeCompare(q.kaynak + q.hedef, 'tr'));
    const durum = r => r.adet === 0 ? rozet('Kayıt Yok', 'bekle') : r.sim === 0 ? rozet('Simülasyonda Yok', 'hata') : Math.abs(r.sim - r.kayit) < 1 ? rozet('Uyumlu') : rozet('Fark Var', 'bekle');
    $('#phMutabakat tbody').innerHTML = R.map(r => `<tr title="simülasyon ${r.kisi} personel · ${r.adet} yansıtma kaydı"><td>${yon(r.kaynak, r.hedef)}</td><td class="s">${hucre(r.sim)}</td><td class="s">${hucre(r.kayit)}</td><td class="s ${Math.abs(r.sim - r.kayit) >= 1 ? 'text-danger' : ''}">${kisa(r.sim - r.kayit)}</td><td class="s">${r.adet}</td><td>${durum(r)}</td></tr>`).join('') + (R.length ? `<tr class="toplam"><td>Toplam · ${V.donemAd}${s ? ` · ${esc(s.no)} v${s.versiyon}` : ''}</td><td class="s">${kisa(R.reduce((t, r) => t + r.sim, 0))}</td><td class="s">${kisa(R.reduce((t, r) => t + r.kayit, 0))}</td><td class="s">${kisa(R.reduce((t, r) => t + r.sim - r.kayit, 0))}</td><td class="s">${R.reduce((t, r) => t + r.adet, 0)}</td><td></td></tr>` : '<tr><td colspan="6" class="na-bos">Bu dönemde yansıtma yok.</td></tr>');
  }
  $('#phYKayitTablo tbody').addEventListener('click', e => { const tr = e.target.closest('tr[data-no]'); if (tr) kayitDrawer(tr.dataset.no); });
  ['#phKDonem', '#phKHedef', '#phKDurum'].forEach(s => $(s).addEventListener('change', yansitmaCiz));
  dugmeBagla('#phKayitlarExcel', '#phKayitlarPdf', bicim => domExcel('personel_yansitma_kayitlari_' + donem(), [['phYKayitTablo', 'Yansıtma Kayıtları']], bicim));
  // fiş satırları ekranda tablo değil (kayıt detayında): listeden kurulur, aynı Excel bileşeniyle (koyu başlık, sayılar ₺ biçimli)
  dugmeBagla('#phFisExcel', '#phFisPdf', bicim => { if (!window.TabloExcel) { mesaj('danger', 'Excel bileşeni yüklenemedi; sayfayı yenileyip tekrar deneyin.'); return; }
    const satirlar = KL.filter(k => k.durum !== 'İptal').flatMap(k => [...k.kaynakFis.map(f => [k.no, k.donemAdi, k.kaynakSirket, 'Kaynak (SGK İşvereni)', k.kaynakFisNo, f.hesap, f.kod, f.ad, kisa(f.borc), kisa(f.alacak)]), ...k.hedefFis.map(f => [k.no, k.donemAdi, k.hedefSirket, 'Hedef (Fiili Görev Yeri)', k.hedefFisNo, f.hesap, f.kod, f.ad, kisa(f.borc), kisa(f.alacak)])]);
    const pdf = bicim === 'pdf';
    (pdf ? window.TabloExcel.pdf : window.TabloExcel.indir)('personel_yansitma_fis_satirlari_' + donem(), [{ ad: 'Fiş Satırları', baslik: 'Personel Muhasebe İşlemleri · Yansıtma Kayıtları · Fiş Satırları', altBaslik: `${satirlar.length} fiş satırı · ${KL.filter(k => k.durum !== 'İptal').length} yansıtma kaydı`, basliklar: ['Yansıtma No', 'Dönem', 'Şirket', 'Taraf', 'Fiş No', 'Hesap', 'Kod', 'Hesap Adı', 'Borç', 'Alacak'], satirlar }]).catch(e => mesaj('danger', (pdf ? 'PDF' : 'Excel') + ' indirilemedi: ' + esc(e.message))); });
  function fisTablo(satirlar) { const tb = satirlar.reduce((s, f) => s + f.borc, 0), ta = satirlar.reduce((s, f) => s + f.alacak, 0); return `<table class="na-tablo ph-fis" data-dinamik="kapali"><thead><tr><th>Hesap</th><th>Kod</th><th>Hesap Adı</th><th class="s">Borç</th><th class="s">Alacak</th></tr></thead><tbody>${satirlar.map(f => `<tr><td class="kod">${esc(f.hesap)}</td><td class="kod">${esc(f.kod || '')}</td><td>${esc(f.ad)}</td><td class="s">${f.borc ? tl(f.borc) : ''}</td><td class="s">${f.alacak ? tl(f.alacak) : ''}</td></tr>`).join('')}<tr class="top"><td colspan="3">Toplam ${Math.abs(tb - ta) < 0.01 ? rozet('Borç = Alacak') : rozet('Dengesiz', 'hata')}</td><td class="s">${tl(tb)}</td><td class="s">${tl(ta)}</td></tr></tbody></table>`; }
  function kayitDrawer(no) {
    const k = (Y().kayitlar || []).find(x => x.no === no); if (!k) return;
    const m = [['Yansıtma No', k.no], ['Dönem', k.donemAdi], ['Personel', `${k.adSoyad} (${k.cariKodu})`], ['Görev', k.gorev], ['SGK İşvereni (Kaynak)', k.kaynakSirket], ['Fiili Görev Yeri', k.fiiliGorevYeri || '—'], ['Ödeme Şirketi', k.odemeSirketi || '—'], ['Hedef Şirket', k.hedefSirket], ['Pay', '%' + pct(k.pay)], ['Net / KDV / Toplam', `${tl(k.net)} / ${tl(k.kdv)} / ${tl(k.toplam)} ₺`], ['Kaynak Fişi', k.kaynakFisNo], ['Hedef Fişi', k.hedefFisNo], ['Fatura', k.faturaNo || '—'], ['Oluşturan', `${k.olusturan} · ${k.zaman}`]];
    drawerAc(`<i class="fas fa-file-invoice-dollar me-1"></i>${esc(k.no)} · ${esc(k.adSoyad)} ${rozet(k.durum)}`,
      `${k.durum === 'Kaydedildi' ? '<button type="button" class="btn btn-warning btn-sm py-0" data-act="fatura"><i class="fas fa-file-invoice me-1"></i>Fatura Oluştur</button>' : ''}${k.durum !== 'İptal' ? '<button type="button" class="btn btn-light btn-sm py-0" data-act="iptal" style="color:#b91c1c"><i class="fas fa-ban me-1"></i>İptal</button>' : ''}<a class="btn btn-light btn-sm py-0" href="${kartAdresi(k.cariKodu)}" target="_blank"><i class="fas fa-id-card me-1"></i>Personel Kartı</a>`,
      `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-receipt me-1"></i>Yansıtma Bilgileri</span><span class="not">${k.durum === 'İptal' ? 'İptal: ' + esc(k.iptalNedeni) : 'her iki şirkete de kayıt atıldı'}</span></div><div class="na-kart-g"><div class="ph-master">${m.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${esc(v)}</div></div>`).join('')}</div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-list me-1"></i>Kalemler (GDR ↔ YNS)</span><span class="not">%${pct(k.pay)} pay uygulanmış tutarlar</span></div><div class="na-kart-g p-0" style="overflow:auto"><table class="na-tablo ph-kalem" data-dinamik="kapali"><thead><tr><th>Maliyet Alanı</th><th>X/R</th><th>Gider Kodu</th><th>Gider Kalemi</th><th>Yansıtma Kodu</th><th>Yansıtma Gelir Kalemi</th><th class="s">Tutar</th></tr></thead><tbody>${k.kalemler.map(x => `<tr><td>${esc(x.alanAdi)}</td><td>${esc(x.aktarimTipi || '—')}</td><td class="kod">${esc(x.giderKodu)}</td><td>${esc(x.giderKalem)}</td><td class="kod">${esc(x.yansitmaKodu)}</td><td>${esc(x.yansitmaKalem)}</td><td class="s">${tl(x.tutar)}</td></tr>`).join('')}</tbody></table></div></div>
      <div class="row g-2"><div class="col-lg-6"><div class="ph-fis-bas k"><i class="fas fa-building"></i>${esc(k.kaynakSirket)} · Kaynak Fişi<small>${esc(k.kaynakFisNo)} · Yansıtma Geliri</small></div>${fisTablo(k.kaynakFis)}</div><div class="col-lg-6"><div class="ph-fis-bas h"><i class="fas fa-building-user"></i>${esc(k.hedefSirket)} · Hedef Fişi<small>${esc(k.hedefFisNo)} · Personel Gideri</small></div>${fisTablo(k.hedefFis)}</div></div>
      <div class="na-kart mt-2"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Geçmiş</span></div><div class="na-kart-g"><ul class="ph-gecmis mb-0 ps-3">${k.gecmis.map(g => `<li>${esc(g)}</li>`).join('')}</ul></div></div>`);
    $$('#phDrawerAksiyon [data-act]').forEach(b => b.addEventListener('click', async () => {
      if (b.dataset.act === 'fatura') { const j = await post('/PersonelMuhasebe/YansitFatura', { no: k.no, donem: donem() }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veriUygula(j); mesaj('success', esc(j.message)); kayitDrawer(k.no); }
      if (b.dataset.act === 'iptal') { const neden = window.prompt('İptal nedeni (zorunlu):', ''); if (neden === null) return; const j = await post('/PersonelMuhasebe/YansitIptal', { no: k.no, neden, donem: donem() }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veriUygula(j); mesaj('success', esc(j.message)); drawerKapat(); }
    }));
  }

  // ============================================================
  // KALEM EŞLEME (maliyet alanı → GDR.13 ↔ YNS.13; KDV; katalog)
  // ============================================================
  function kalemEslemeCiz() {
    const y = Y();
    const secenek = kod => `<select class="form-select" data-alan-kod>${(y.katalog || []).map(k => `<option value="${esc(k.giderKodu)}" ${k.giderKodu === kod ? 'selected' : ''}>${esc(k.giderKodu)} · ${esc(k.kalem)}</option>`).join('')}</select>`;
    $('#phKalemEslemeTablo tbody').innerHTML = (y.esleme || []).map(e => `<tr data-alan="${esc(e.alan)}"><td class="ad">${esc(e.alanAdi)}</td><td>${e.aktarimTipi ? xrRozet(e.aktarimTipi) : '—'}</td><td>${secenek(e.giderKodu)}</td><td>${esc(e.giderAltGrup)} › ${esc(e.giderKalem)}</td><td class="kod">${esc(e.yansitmaKodu)}</td><td>${esc(e.yansitmaKalem)}</td></tr>`).join('');
    $$('#phKalemEslemeTablo select[data-alan-kod]').forEach(s => s.addEventListener('change', async () => { const j = await post('/PersonelMuhasebe/KalemEsleme', { alan: s.closest('tr').dataset.alan, giderKodu: s.value, donem: donem() }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veriUygula(j); mesaj('success', esc(j.message)); }));
    $('#phKatalog tbody').innerHTML = (y.katalog || []).map(k => `<tr><td class="kod">${esc(k.giderKodu)}</td><td>${esc(k.altGrup)}</td><td>${esc(k.kalem)}</td><td class="kod">${esc(k.yansitmaKodu)}</td><td>${esc(k.yansitmaKalem)}</td></tr>`).join('');
    $('#phKatalogSayi').textContent = `${(y.esleme || []).length} alan eşlemesi · katalog ${(y.katalog || []).length} kalem · KDV %${pct(y.kdvOrani || 0)}`;
    kpiCiz();
  }
  $('#phKdvKaydet').addEventListener('click', async () => { const j = await post('/PersonelMuhasebe/Kdv', { kdv: Number($('#phKdv').value) || 0, donem: donem() }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veriUygula(j); mesaj('success', esc(j.message)); });

  // ============================================================
  // HESAP EŞLEŞTİRME (simülasyon hesapları; Personel Maliyet Raporu ile aynı tablo)
  // ============================================================
  function hesapFormKur(m) {
    const sirketler = m.sirketler || []; ['#phe_sirket', '#phe_karsi'].forEach((id, i) => { const e = $(id), v = e.value; e.innerHTML = `<option value="*">${i ? 'Tümü' : 'Tüm Şirketler'}</option>` + sirketler.map(s => `<option value="${esc(s)}">${esc(s)}</option>`).join(''); e.value = v || '*'; });
    const isl = $('#phe_islem'); if (!isl.options.length) isl.innerHTML = (m.islemTurleri || []).map(t => `<option value="${esc(t)}">${esc(t)}</option>`).join('');
    $('#dlPhKalem').innerHTML = [...new Set((m.eslemeler || []).map(e => e.kaynakKalem))].map(k => `<option value="${esc(k)}">`).join('');
    $('#dlPhHesap').innerHTML = (m.hesaplar || []).map(h => `<option value="${esc(h.kod)}">${esc(h.ad)}</option>`).join('');
  }
  function hesapCiz() {
    const m = M(), a = ara(), L = (m.eslemeler || []).filter(e => !a || kucukTr([e.sirket, e.karsiSirket, e.islemTuru, e.kaynakKalem, e.hesapKodu, e.hesapAdi].join(' ')).includes(a));
    $('#phEslemeTablo tbody').innerHTML = L.map(e => `<tr data-id="${e.id}" class="${e.aktif ? '' : 'ayrildi'}"><td class="kucuk">${e.id}</td><td>${e.sirket === '*' ? '<span class="text-muted">Tüm Şirketler</span>' : esc(e.sirket)}</td><td>${e.karsiSirket === '*' ? '<span class="text-muted">—</span>' : esc(e.karsiSirket)}</td><td>${esc(e.islemTuru)}</td><td>${esc(e.kaynakKalem)}</td><td>${xrRozet(e.xr)}</td><td>${esc(e.yon)}</td><td><b>${esc(e.hesapKodu)}</b>${e.posting ? '' : ' <span class="pm-rz hata">üst hesap</span>'}</td><td>${esc(e.hesapAdi)}</td><td>${e.varsayilan ? '✓' : ''}</td><td class="kucuk">${esc(e.gecerlilikBas || '—')} – ${esc(e.gecerlilikBit || '∞')}</td><td>${rozet(e.aktif ? 'Aktif' : 'Pasif', e.aktif ? 'ok' : 'gri')}</td><td class="s">v${e.versiyon}</td><td class="kucuk">${esc(e.degistiren || '')}<small class="alt">${esc(e.degisme || '')}</small></td></tr>`).join('') || '<tr><td colspan="14" class="na-bos">Eşleştirme yok.</td></tr>';
    $('#phHesapSayi').textContent = `${L.length} / ${(m.eslemeler || []).length} eşleştirme · v${m.eslemeVersiyonu} · raporla ortak tablo`;
  }
  function hesapSec(id) {
    const e = (M().eslemeler || []).find(x => x.id === id); if (!e) return;
    $('#phe_id').value = e.id; $('#phe_sirket').value = e.sirket; $('#phe_karsi').value = e.karsiSirket; $('#phe_islem').value = e.islemTuru; $('#phe_kalem').value = e.kaynakKalem; $('#phe_xr').value = e.xr; $('#phe_yon').value = e.yon; $('#phe_hesap').value = e.hesapKodu; $('#phe_hesapAd').value = e.hesapAdi; $('#phe_bas').value = e.gecerlilikBas || ''; $('#phe_bit').value = e.gecerlilikBit || ''; $('#phe_varsayilan').checked = e.varsayilan; $('#phe_aktif').checked = e.aktif; $('#phe_kaydetYazi').textContent = `#${e.id} Güncelle`;
    $('#phe_kalem').scrollIntoView({ block: 'center' });
  }
  function hesapTemizle() { $('#phEslemeForm').reset(); $('#phe_id').value = ''; $('#phe_hesapAd').value = ''; $('#phe_kaydetYazi').textContent = 'Eşleştirme Ekle'; }
  $('#phEslemeTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr) hesapSec(+tr.dataset.id); });
  $('#phEslemeForm').addEventListener('submit', async ev => {
    ev.preventDefault(); if (!$('#phEslemeForm').reportValidity()) return;
    const d = { id: $('#phe_id').value ? +$('#phe_id').value : null, sirket: $('#phe_sirket').value, karsiSirket: $('#phe_karsi').value, islemTuru: $('#phe_islem').value, kaynakKalem: $('#phe_kalem').value.trim(), xr: $('#phe_xr').value, yon: $('#phe_yon').value, hesapKodu: $('#phe_hesap').value.trim(), varsayilan: $('#phe_varsayilan').checked, gecerlilikBas: $('#phe_bas').value, gecerlilikBit: $('#phe_bit').value, aktif: $('#phe_aktif').checked, donem: donem() };
    const j = await post('/PersonelMuhasebe/Esleme', d); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    hesapTemizle(); veriUygula(j); mesaj('success', esc(j.message));
  });
  $('#phe_temizle').addEventListener('click', hesapTemizle);
  $('#phe_hesap').addEventListener('input', () => { const h = (M().hesaplar || []).find(x => x.kod === $('#phe_hesap').value.trim()); $('#phe_hesapAd').value = h ? h.ad : ''; });

  // ============================================================
  // FİŞ ARŞİVİ (tüm dönemler)
  // ============================================================
  function arsivCiz() {
    const a = ara(), d = $('#phArsivDurum').value, L = (V.arsiv || []).filter(x => (!d || x.durum === d) && (!a || kucukTr([x.no, x.donemAd, x.kapsamAd, x.durum, x.olusturan, x.onaylayan, x.muhasebelestiren, (x.fisler || []).map(f => f.muhasebeFisNo).join(' ')].join(' ')).includes(a)));
    $('#phArsivTablo tbody').innerHTML = L.map(x => { const son = (x.audit || []).slice(-1)[0]; return `<tr data-id="${x.id}" data-donem="${esc(x.donem)}"><td><b>${esc(x.no)}</b></td><td>v${x.versiyon}</td><td>${esc(x.donemAd)}</td><td>${esc(x.kapsamAd)}</td><td class="s">${x.personelSayisi}</td><td class="s">${x.fisSayisi}</td><td class="s">${kisa(x.toplamBorc)}</td><td class="s">${kisa(x.toplamAlacak)}</td><td>${rozet(x.durum)}</td><td><small>${(x.fisler || []).filter(f => f.muhasebeFisNo).map(f => esc(f.muhasebeFisNo)).join(', ') || '—'}</small></td><td><small>${(x.fisler || []).filter(f => f.tersFisNo).map(f => esc(f.tersFisNo)).join(', ') || '—'}</small></td><td>${esc(x.olusturan)}<small class="alt">${esc(x.olusturma)}</small></td><td>${x.onaylayan ? esc(x.onaylayan) + `<small class="alt">${esc(x.onayTarihi)}</small>` : '—'}</td><td>${x.muhasebelestiren ? esc(x.muhasebelestiren) + `<small class="alt">${esc(x.muhasebeTarihi)}</small>` : '—'}</td><td>${son ? `<b>${esc(son.islem)}</b><small class="alt">${esc(son.tarih)} · ${esc(son.kullanici)}</small>` : '—'}</td></tr>`; }).join('') || '<tr><td colspan="15" class="na-bos">Kayıt yok.</td></tr>';
    const m = (V.arsiv || []).filter(x => x.durum === 'Muhasebeleştirildi').length, t = (V.arsiv || []).filter(x => x.durum === 'Ters Kayıt Yapıldı').length;
    $('#phArsivSayi').textContent = `${L.length} / ${(V.arsiv || []).length} simülasyon · ${m} muhasebeleştirilmiş · ${t} ters kayıt · tüm dönemler`;
  }
  $('#phArsivDurum').addEventListener('change', arsivCiz);
  $('#phArsivTablo tbody').addEventListener('click', async ev => { const tr = ev.target.closest('tr[data-id]'); if (!tr) return; seciliSim = +tr.dataset.id; await yukle(tr.dataset.donem); sekme('kayitlar'); });

  // ---- çekmece
  function drawerAc(baslik, aksiyon, ic) { $('#phDrawerBaslik').innerHTML = baslik; $('#phDrawerAksiyon').innerHTML = aksiyon || ''; $('#phDrawerIc').innerHTML = ic; $('#phDrawer').classList.add('acik'); $('#phPerde').classList.add('acik'); $('#phDrawer').scrollTop = 0; }
  function drawerKapat() { $('#phDrawer').classList.remove('acik'); $('#phPerde').classList.remove('acik'); }
  $('#phDrawerKapat').addEventListener('click', drawerKapat); $('#phPerde').addEventListener('click', drawerKapat);

  // ---- ortak olaylar
  $$('#phSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p, true)));
  $('#phDonem').addEventListener('change', () => { seciliSim = null; secili.clear(); acikDetay.clear(); yukle($('#phDonem').value).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message))); });
  $('#phAra').addEventListener('input', () => { if (V) cizAktif(); });
  ['#phSgk', '#phGorevYeri', '#phOdemeSirketi'].forEach(s => $(s).addEventListener('change', () => { if (V) cizAktif(); }));   // ortak üç filtre: aktif sekmenin personel satırları yeniden süzülür
  $('#phDetay').addEventListener('change', kayitlarCiz);
  $('#phSimule').addEventListener('click', async () => { const j = await post('/PersonelMuhasebe/Simule', { donem: donem(), kapsam: '*' }); if (!j.success) { mesaj('danger', esc(j.message)); return; } seciliSim = null; veriUygula(j); mesaj('success', esc(j.message)); });
  $('#phSimTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr) { seciliSim = +tr.dataset.id; fisF = ''; kayitlarCiz(); kpiCiz(); } });
  $('#phSecili').addEventListener('click', ev => {   // fiş / özet satırı → fiş önizleme; personel satırı → cari kartı
    const s = sim(); if (!s || ev.target.closest('button, input, select, a')) return;
    const fs = ev.target.closest('#phFisTablo tbody tr[data-fis], #phOzetTablo tbody tr[data-fis]'); if (fs) { fisOnizle(s, fs.dataset.fis); return; }
    const st = ev.target.closest('#phSatirTablo tbody tr[data-kod]'); if (st && st.dataset.kod) window.open(kartAdresi(st.dataset.kod), '_blank');
  });
  $('#phOdemeTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-sira]'); if (!tr) return; const o = (V.odemeler.satirlar || []).find(x => x.personelSira === +tr.dataset.sira); const k = (V.odemeler.odemeYok || []).find(x => x.sira === +tr.dataset.sira); const y = (Y().adaylar || []).find(x => x.sira === +tr.dataset.sira); const kod = (k && k.cariKodu) || (y && y.cariKodu); if (kod) window.open(kartAdresi(kod), '_blank'); else if (o) window.open('/PersonelMaliyet?sekme=tablo&donem=' + encodeURIComponent(donem()), '_blank'); });
  $('#phOdemeYok tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-kod]'); if (tr && tr.dataset.kod) window.open(kartAdresi(tr.dataset.kod), '_blank'); });
  ['#phOdemeSirket', '#phOdemeKanal'].forEach(s => $(s).addEventListener('change', odemeCiz));
  dugmeBagla('#phKayitExcel', '#phKayitPdf', bicim => domExcel('personel_muhasebe_kayitlari_' + donem(), [['phSimTablo', 'Simülasyonlar'], ['phFisTablo', 'Fişler'], ['phSatirTablo', 'Personel Bazlı Satırlar'], ['phOzetTablo', 'Özet Kayıtlar — Muhasebeye Atılacak']], bicim));
  dugmeBagla('#phOdemeExcel', '#phOdemePdf', bicim => domExcel('personel_odemeleri_' + donem(), [['phOdemeTablo', 'Ödemeler'], ['phOdemeOzet', 'Şirket × Kanal'], ['phOdemeYok', 'Ödeme Yapılmıyor']], bicim));
  dugmeBagla('#phHesapExcel', '#phHesapPdf', bicim => domExcel('hesap_eslestirme', [['phEslemeTablo', 'Hesap Eşleştirme']], bicim));
  dugmeBagla('#phArsivExcel', '#phArsivPdf', bicim => domExcel('personel_muhasebe_fis_arsivi', [['phArsivTablo', 'Fiş Arşivi']], bicim));
  $('#phBilgi').addEventListener('click', () => { kok.classList.toggle('aciklama'); kapBoyut(); });
  window.addEventListener('resize', () => { clearTimeout(kapBoyut.z); kapBoyut.z = setTimeout(kapBoyut, 120); });
  document.addEventListener('visibilitychange', () => { if (!document.hidden && V && ['kayitlar', 'odemeler', 'arsiv'].includes(aktifSekme)) yukle().catch(() => { /* yoksay */ }); });   // başka sekmede (raporda) adım atıldıysa geri dönünce tazele

  // ---- başlat
  sekme(aktifSekme);
  yukle(kok.dataset.donem).then(() => { window.NA_HAZIR = true; }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
