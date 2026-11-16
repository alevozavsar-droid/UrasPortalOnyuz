// ============================================================
// MÜŞTERİ FİYAT VE KÂR ORANLARI — ekran betiği (Views/FiyatYonetimi/Index.cshtml; veri /FiyatYonetimi/Veri; kayıt SaticiKaydet / MusteriKaydet; Data/FiyatOrnek.cs)
// Sekme 1 Yetkili Satıcı Fiyatları : satıcı seç → genel (toplu) oran, ürün grubu oranları, ürün tablosunda kalem oranı; uygulanan oran (Kalem > Grup > Genel), satıcı alış fiyatı,
//                                    satıcı alış fiyatı ($ ve ₺ — ₺ fiyatlandırma kuruyla); Kaydet (yalnız değişenler gönderilir), Vazgeç, Excel
// Sütun adları (anlam karışmasın diye): "Satıcı Alış Fiyatı" = Uras Kimya → satıcı ($ / ₺); "Müşteriye Satış Fiyatı" = satıcı → müşteri ($ / € / ₺). Liste fiyatı kavramı yoktur (kaldırıldı).
// Tablolarda üst başlık satırı (tr.fy-grp) sütun gruplarını "kimden kime" ile yazar; grup etiketleri seçili satıcı / müşteri adıyla betikte doldurulur.
// Sekme 2 Müşteri Fiyatları        : kur kartı (USD/TL, EUR/TL piyasa + döviz kâr marjı → fiyatlandırma kuru / paritesi; yalnız yönetici kaydeder); satıcı + müşteri seç (sol liste:
//                                    cari kodu, ad, sınıf, genel %, grup / kalem tanım sayısı) → cari kart özeti, genel oran, grup oranları, ürün tablosunda kalem oranı; satıcı alış
//                                    fiyatı ($, 1. katman) × (1 + oran) = müşteri satış fiyatı ($) + EUR / TL karşılığı; uçtan uca marj; Kaydet, Vazgeç, Excel
// Para birimi: maliyet ve fiyatlar USD (V.paraBirimi); TL / EUR karşılığı yalnız 2. sekmede, kurdaki kaydedilmemiş değerlerle anlık hesaplanır.
// Sekme 3 Özet ve Günlük           : satıcı × grup ve müşteri × grup oran matrisleri; değişiklik günlüğü
// 13 Kas 2026 — dört salt okunur sekme daha (ayrıntı aşağıda "YENİ SEKMELER"): Müşteri Fiyat Ortalamaları · Kâr Oranı Sorgusu · Ürün Fiyat Karşılaştırması · Satış Uyarıları
// Yetki: V.ben.yonetici değilse tüm girişler kilitli (salt okunur). Boş oran = tanım yok (silme). Kaydedince veri yeniden çekilir.
// Yazım kuralı: etiketler Title Case ("Genel Oran", "Kalem", "Tanımsız").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('fy'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const tl = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const pct = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 2 }) + '%';
  const iso = d => d.toISOString().slice(0, 10);
  const sekmeli = kok.dataset.sekme === '1';
  let V = null, YONETICI = false, URETICI = '';
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 8000); }
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + iso(new Date()) + '.csv'; a.click(); };
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');
  const post = async (url, g) => { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(g) }); return r.json(); };
  const kaynakRozet = k => `<span class="fy-kaynak ${esc(k)}">${esc(k)}</span>`;
  const sinifRozet = s => `<span class="ck-tip ${s === 'Özel Takip' ? 'ozel' : ''}">${esc(s || '—')}</span>`;
  const iliskiRozet = i => `<span class="ck-tip ${i === 'Grup İçi' ? 'grup' : i === 'İlişkili Cari' ? 'iliskili' : ''}">${esc(i || '—')}</span>`;
  const oranGiris = (id, deger, k, kapsam) => `<input type="number" step="0.01" class="form-control fy-oran" data-k="${esc(k)}" data-kapsam="${kapsam}" data-eski="${deger ?? ''}" value="${deger ?? ''}" placeholder="—" ${YONETICI ? '' : 'disabled'}>`;
  const sayiMi = v => v !== '' && v !== null && v !== undefined && !isNaN(Number(v));

  // ---- oran çözümleme (sunucudaki OranCoz ile aynı kural): Kalem > Grup > Genel
  function oranCoz(oranlar, u) {
    let o = oranlar.find(x => x.kapsam === 'Urun' && x.anahtar.toLowerCase() === u.kod.toLowerCase()); if (o) return { oran: o.oran, kaynak: 'Kalem' };
    o = oranlar.find(x => x.kapsam === 'Grup' && x.anahtar === u.grupAnahtari); if (o) return { oran: o.oran, kaynak: 'Grup' };
    o = oranlar.find(x => x.kapsam === 'Genel'); if (o) return { oran: o.oran, kaynak: 'Genel' };
    return { oran: 0, kaynak: 'Tanımsız' };
  }
  /// Ekrandaki girişlerden (kaydedilmemiş) oran listesi kurar
  function ekranOranlari(genelId, grupKok, tabloId) {
    const l = []; const g = $(genelId).value; if (sayiMi(g)) l.push({ kapsam: 'Genel', anahtar: '', oran: Number(g) });
    $$(grupKok + ' input').forEach(i => { if (sayiMi(i.value)) l.push({ kapsam: 'Grup', anahtar: i.dataset.k, oran: Number(i.value) }); });
    $$(tabloId + ' input.fy-oran').forEach(i => { if (sayiMi(i.value)) l.push({ kapsam: 'Urun', anahtar: i.dataset.k, oran: Number(i.value) }); });
    return l;
  }
  // 13 Kas 2026 — ZİNCİR DİNAMİK: üretici de seçilir (URETICI). Oranlar üretici → satıcı çiftine aittir: varsayılan üreticide st.oranlar, öbürlerinde st.zincir[].
  const saticiOranlari = ad => { const st = V.saticilar.find(x => x.ad === ad); if (!st) return []; if (URETICI === V.uretici) return st.oranlar; const z = (st.zincir || []).find(x => x.uretici === URETICI); return z ? z.oranlar : []; };
  const sOr = s => saticiOranlari(s.ad);
  // müşteri katmanında risk payı %: satıcı alış fiyatının üstüne, kâr oranından ÖNCE eklenir; kalem değeri müşterinin toplu değerini ezer
  const mRisk = (t, u) => { if (!t) return 0; const k = (t.urunRisk || []).find(x => x.kod === u.kod); return k ? Number(k.oran) : Number(t.risk ?? 0); };
  /** zincirde yetkili satıcı olabilen şirketler: üreticinin kendisi hariç */
  const zincirSaticilar = () => V.saticilar.filter(s => s.ad !== URETICI);
  // 13 Kas 2026 — MALİYET BİLEŞENLERİ: Toplam Maliyet = (Üretim Maliyeti + Ambalaj Maliyeti) × (1 + Risk Payı %). Ambalaj ve risk payı toplu (tüm ürünler) ya da kalem kalem
  // girilir; kalem değeri toplu değeri ezer. u.maliyet her zaman GEÇERLİ TOPLAM MALİYETTİR (bütün sekmeler bunu kullanır); u.uretim ham üretim maliyetidir.
  const malEkran = () => { const m = {}; $$('#fyUrunTablo input.fy-mal').forEach(i => { (m[i.dataset.k] = m[i.dataset.k] || {})[i.dataset.alan] = i.value; }); return m; };
  function maliyetUygula(ekran) {
    const ga = sayiMi($('#fyGenelAmbalaj').value) ? Number($('#fyGenelAmbalaj').value) : ambalajOto().usdKg, gr = sayiMi($('#fyGenelRisk').value) ? Number($('#fyGenelRisk').value) : 0;
    V.urunler.forEach(u => {
      const e = ekran && ekran[u.kod], a = e && 'ambalaj' in e ? e.ambalaj : (u.ambalajMaliyeti ?? ''), r = e && 'risk' in e ? e.risk : (u.riskPayi ?? '');
      u.ambalajGiris = a; u.riskGiris = r; u.ambalaj = sayiMi(a) ? Number(a) : ga; u.risk = sayiMi(r) ? Number(r) : gr;
      u.maliyet = Math.round((u.uretim + u.ambalaj) * (1 + u.risk / 100) * 10000) / 10000;
    });
  }
  // toplu ambalaj maliyeti OTOMATİK: seçili ambalaj tipinin son alış fiyatı (KDV DAHİL) ÷ kap kapasitesi ÷ USD/TL; kutuya değer yazılırsa o geçerli olur
  const ambalajOto = () => (V.maliyet.ambalajTipleri || []).find(x => x.kod === $('#fyAmbalajTipi').value) || { usdKg: 0 };
  function ambalajBilgi() { const a = ambalajOto(), elle = sayiMi($('#fyGenelAmbalaj').value); $('#fyGenelAmbalaj').placeholder = sayi(a.usdKg || 0); $('#fyAmbalajBilgi').innerHTML = a.kod ? `<b>${elle ? 'Elle girildi' : 'Otomatik'}</b> · ${esc(a.ad)}: ${tl(a.birimFiyat)} ₺ + KDV % ${Number(a.kdv)} = <b>${tl(a.kdvDahil)} ₺</b> ÷ ${Number(a.kapasiteKg)} kg ÷ USD/TL ${k4(a.kur)} = <b>${k4(a.usdKg)} $ / kg</b>${a.tedarikci ? ' · ' + esc(a.tedarikci) : ''}${a.tarih ? ' · son alış ' + esc(a.tarih) : ' · fiyat listesi'}` : 'Ambalaj alış fiyatı bulunamadı.'; }
  const malDegisen = () => $$('[data-p=satici] .fy-mal').filter(i => (i.value || '') !== (i.dataset.eski || ''));
  /** tablo kaplarını ekranın altına kadar uzatır (yarım ekran boş kalmasın): etkin paneldeki her .fy-kap kendi üst konumundan pencere altına kadar */
  function kapBoyut() { $$('#fy > .na-panel.aktif .fy-kap').forEach(k => { const alt = k.dataset.alt ? Number(k.dataset.alt) : 26; k.style.maxHeight = Math.max(200, window.innerHeight - k.getBoundingClientRect().top - alt) + 'px'; }); }
  const musteriTanimi = (satici, kod) => V.musteriTanimlari.find(m => m.satici === satici && m.cariKodu === kod);
  const saticiFiyati = (oranlar, u) => Math.round(u.maliyet * (1 + oranCoz(oranlar, u).oran / 100) * 100) / 100;
  // Kur: ekrandaki (kaydedilmemiş) değerlerle fiyatlandırma kuru — TL = $ × USD/TL × (1 + marj); EUR = $ ÷ (parite ÷ (1 + marj))
  const r4 = v => Math.round(v * 10000) / 10000;   // sunucudaki FyKur ile aynı 4 hane yuvarlama (parite önce yuvarlanır, sonra marj uygulanır)
  const kurDegerleri = () => { const usd = Number($('#fyKurUsd').value) || V.kur.usdTl, eur = Number($('#fyKurEur').value) || V.kur.eurTl, marj = sayiMi($('#fyKurMarj').value) ? Number($('#fyKurMarj').value) : V.kur.marj; const parite = usd ? r4(eur / usd) : 0; return { usd, eur, marj, fiyatUsdTl: r4(usd * (1 + marj / 100)), parite, fiyatParite: r4(parite / (1 + marj / 100)) }; };
  const tlKarsiligi = usd => { const k = kurDegerleri(); return Math.round(usd * k.fiyatUsdTl * 100) / 100; };
  const eurKarsiligi = usd => { const k = kurDegerleri(); return k.fiyatParite ? Math.round(usd / k.fiyatParite * 100) / 100 : 0; };
  // Kur kutusu "kirli" sayımı sayısal karşılaştırmayla yapılır (41.350 = 41.35); boş kutu kayıtlı değere döner, değişiklik sayılmaz
  const kurKirli = () => $$('.fy-kur-giris').filter(i => sayiMi(i.value) && Number(i.value) !== Number(i.dataset.eski));
  const kurIsaretle = i => i.classList.toggle('degisti', sayiMi(i.value) && Number(i.value) !== Number(i.dataset.eski));
  const k4 = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 4, maximumFractionDigits: 4 });
  function kurYukle() {
    const k = V.kur; $('#fyKurUsd').value = k.usdTl; $('#fyKurEur').value = k.eurTl; $('#fyKurMarj').value = k.marj;
    $$('.fy-kur-giris').forEach(i => { i.disabled = !YONETICI; i.dataset.eski = i.value; i.classList.remove('degisti'); }); $('#fyKurKaydet').disabled = true; $('#fyKurVazgec').disabled = true;
    kurHesapla();
  }
  function kurHesapla() {
    const k = kurDegerleri(); $('#fyKurFiyatUsd').textContent = k4(k.fiyatUsdTl); $('#fyKurParite').textContent = k4(k.parite); $('#fyKurFiyatParite').textContent = k4(k.fiyatParite);
    const n = kurKirli().length; $('#fyKurKaydet').disabled = !YONETICI || n === 0; $('#fyKurVazgec').disabled = n === 0;
    $('#fyKurNot').textContent = `${V.kur.kaynak} · ${V.kur.tarih} · ${V.kur.guncelleyen || ''}${n ? ' · kaydedilmemiş kur değişikliği: fiyatlar geçici hesaplanıyor' : ''}`;
  }
  function kurVazgec() { const k = V.kur; $('#fyKurUsd').value = k.usdTl; $('#fyKurEur').value = k.eurTl; $('#fyKurMarj').value = k.marj; $$('.fy-kur-giris').forEach(i => i.classList.remove('degisti')); kurHesapla(); cizUrunler(); if (SECILI) cizMUrunler(); }
  async function kurKaydet() {
    const g = { usdTl: Number($('#fyKurUsd').value) || null, eurTl: Number($('#fyKurEur').value) || null, marj: sayiMi($('#fyKurMarj').value) ? Number($('#fyKurMarj').value) : null };
    const j = await post('/FiyatYonetimi/KurKaydet', g); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) await yenile(false);   // kaydedildi: sunucu değerleri alınır, taslak korunmaz
  }
  const cariHtml = c => c ? [['Cari Kodu', c.kod], ['Kısa Ad', c.kisaAd], ['Resmi Ünvan', c.resmiUnvan], ['Vergi No', c.vergiNo], ['Cari Sınıfı', c.sinif], ['İlişki Durumu', c.iliskiDurumu], ['Bölge · Ülke · Şehir', [c.bolge, c.ulke, c.sehir].filter(Boolean).join(' · ')], ['Vade / Ödeme Şartı', c.vadeSarti], ['Satış Temsilcisi', c.temsilciler], ['Ana Faaliyet', c.anaFaaliyet], ['Yetkili', c.yetkili], ['Durum', c.durum + (c.durumKaynagi ? ' · ' + c.durumKaynagi : '')]].map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${esc(v || '—')}</div></div>`).join('') : '<div class="na-bos">Cari kart bulunamadı.</div>';
  const degistiIsaretle = i => i.classList.toggle('degisti', (i.value || '') !== (i.dataset.eski || ''));
  const degisenSayisi = kokSec => $$(kokSec + ' input.fy-oran:not(.fy-kur-giris)').filter(i => (i.value || '') !== (i.dataset.eski || '')).length;   // kur kutuları oran sayımına girmez (kendi Kaydet'i var)

  // ---- sekmeler
  function sekme(p) {
    $$('#fySekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('#fy > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    if (V && p === 'ozet') cizOzet();
    if (V && YENI[p]) YENI[p]();
    kapBoyut();
    try { const u = new URL(location.href); if (p === 'satici') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }

  // ================= SEKME 1: YETKİLİ SATICI =================
  function saticiYukle() {
    const s = V.saticilar.find(x => x.ad === $('#fySatici').value); if (!s) return;
    malGenelYukle();
    const sk = $('#fySaticiKutu'); sk.textContent = [s.sirket !== s.ad ? s.sirket : '', s.cariKodu, s.bolge].filter(Boolean).join(' · ') || 'Cari kartı tanımlı değil';
    if (s.cariKodu) { sk.dataset.url = '/CariKart/Musteri?kod=' + encodeURIComponent(s.cariKodu); sk.dataset.baslik = 'Müşteri Cari Kartları'; sk.title = `${s.ad} cari kartını aç (${s.cariKodu})`; } else { delete sk.dataset.url; sk.title = ''; }
    $('#fyGrpAlis').textContent = `Satıcı Alış Fiyatı · ${URETICI} → ${s.ad}`;
    $('#fySaticiNot').textContent = `${s.not || ''} · ${sOr(s).length} oran tanımı · ${s.musteriSayisi} müşteri tanımı`;
    $('#fySaticiCari').innerHTML = cariHtml(s.cari);
    const genel = sOr(s).find(o => o.kapsam === 'Genel');
    $('#fyGenel').value = genel ? genel.oran : ''; $('#fyGenel').dataset.eski = genel ? genel.oran : ''; $('#fyGenel').disabled = !YONETICI; $('#fyGenel').classList.remove('degisti');
    $('#fyGruplar').innerHTML = V.gruplar.map(g => { const o = sOr(s).find(x => x.kapsam === 'Grup' && x.anahtar === g.anahtar); return `<div><b>${esc(g.ad)}<small>${V.urunler.filter(u => u.grupAnahtari === g.anahtar).length} ürün · grup oranı</small></b><span>%</span>${oranGiris('', o ? o.oran : '', g.anahtar, 'Grup')}</div>`; }).join('');
    $$('#fyGruplar input, #fyGenel').forEach(i => i.addEventListener('input', () => { degistiIsaretle(i); cizUrunler(); }));
    cizUrunler();
  }
  function cizUrunler() {
    const s = V.saticilar.find(x => x.ad === $('#fySatici').value); if (!s) return;
    maliyetUygula(malEkran());   // toplam maliyet: ekrandaki (kaydedilmemiş) ambalaj / risk payı değerleriyle
    // kaydedilmemiş ekran değerleriyle hesapla (kalem girişleri tabloda; ilk çizimde sunucu değerleri)
    const kalemler = {}; $$('#fyUrunTablo input.fy-oran').forEach(i => kalemler[i.dataset.k] = i.value);
    const oranlar = [];
    const g = $('#fyGenel').value; if (sayiMi(g)) oranlar.push({ kapsam: 'Genel', anahtar: '', oran: Number(g) });
    $$('#fyGruplar input').forEach(i => { if (sayiMi(i.value)) oranlar.push({ kapsam: 'Grup', anahtar: i.dataset.k, oran: Number(i.value) }); });
    V.urunler.forEach(u => { const v = u.kod in kalemler ? kalemler[u.kod] : (sOr(s).find(x => x.kapsam === 'Urun' && x.anahtar === u.kod) || {}).oran; if (sayiMi(v)) oranlar.push({ kapsam: 'Urun', anahtar: u.kod, oran: Number(v) }); });
    const ara = ($('#fyAra').value || '').toLocaleLowerCase('tr-TR'), gf = $('#fyGrupF').value, kf = $('#fyKaynakF').value;
    const l = V.urunler.map(u => ({ u, c: oranCoz(oranlar, u) })).map(x => ({ ...x, fiyat: Math.round(x.u.maliyet * (1 + x.c.oran / 100) * 100) / 100 })).filter(x => (!gf || x.u.grupAnahtari === gf) && (!kf || x.c.kaynak === kf) && (!ara || [x.u.kod, x.u.ad, x.u.seri, x.u.urunGrubu].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    $('#fyUrunTablo tbody').innerHTML = l.map(x => { const u = x.u, eski = (sOr(s).find(o => o.kapsam === 'Urun' && o.anahtar === u.kod) || {}).oran ?? '', kalem = u.kod in kalemler ? kalemler[u.kod] : eski, ae = u.ambalajMaliyeti ?? '', re = u.riskPayi ?? ''; const mal = (alan, deger, eskiD, yer, adim) => `<input type="number" step="${adim}" min="0" class="form-control fy-mal ${String(deger) !== String(eskiD) ? 'degisti' : ''}" data-k="${esc(u.kod)}" data-alan="${alan}" data-eski="${eskiD}" value="${deger}" placeholder="${yer}" title="Boş = toplu değer" ${YONETICI ? '' : 'disabled'}>`; return `<tr data-kod="${esc(u.kod)}"><td><b>${esc(u.kod)}</b></td><td><b>${esc(u.ad)}</b><small>${esc(u.hiyerarsi)}</small></td><td>${esc(u.anaGrup)}<small>${esc(u.grup)}</small></td><td>${esc(u.birim)}</td><td class="s">${tl(u.uretim)}</td><td class="s">${mal('ambalaj', u.ambalajGiris, ae, sayi(u.ambalaj), '0.01')}</td><td class="s">${mal('risk', u.riskGiris, re, sayi(u.risk), '0.5')}</td><td class="s"><b>${tl(u.maliyet)}</b></td><td class="s"><input type="number" step="0.01" class="form-control fy-oran ${String(kalem) !== String(eski) ? 'degisti' : ''}" data-k="${esc(u.kod)}" data-alan="oran" data-kapsam="Urun" data-eski="${eski}" value="${kalem}" placeholder="—" ${YONETICI ? '' : 'disabled'}></td><td class="s"><b>${pct(x.c.oran)}</b></td><td>${kaynakRozet(x.c.kaynak)}</td><td class="s"><b>${tl(x.fiyat)}</b></td><td class="s">${tl(tlKarsiligi(x.fiyat))}</td></tr>`; }).join('') || '<tr><td colspan="13" class="na-bos">Filtreye uyan ürün yok.</td></tr>';
    $$('#fyUrunTablo input.fy-oran, #fyUrunTablo input.fy-mal').forEach(i => i.addEventListener('input', () => { degistiIsaretle(i); cizUrunler.ertele(); }));
    const tm = l.reduce((a, x) => a + x.u.maliyet, 0), tf = l.reduce((a, x) => a + x.fiyat, 0);
    $('#fyTopUretim').textContent = tl(l.reduce((a, x) => a + x.u.uretim, 0)) + ' $'; $('#fyTopAmbalaj').textContent = tl(l.reduce((a, x) => a + x.u.ambalaj, 0)) + ' $';
    $('#fyTopMaliyet').textContent = tl(tm) + ' $'; $('#fyTopFiyat').textContent = tl(tf) + ' $'; $('#fyTopFiyatTl').textContent = tl(tlKarsiligi(tf)) + ' ₺'; $('#fyOrtOran').textContent = tm ? pct((tf / tm - 1) * 100) + ' ort.' : '—';
    $('#fyKurBilgi').textContent = k4(kurDegerleri().fiyatUsdTl) + (kurKirli().length ? ' (kaydedilmemiş)' : '');
    const tanimsiz = l.filter(x => x.c.kaynak === 'Tanımsız').length; const n = degisenSayisi('[data-p=satici]') + malDegisen().length;
    $('#fyDegisiklik').innerHTML = `${l.length} ürün · ${tanimsiz ? `<span class="text-danger">${tanimsiz} ürün tanımsız (kârsız fiyatlanır)</span> · ` : ''}${n ? `<b class="text-warning">${n} kaydedilmemiş değişiklik</b>` : 'Kaydedilmemiş değişiklik yok'}${YONETICI ? '' : ' · salt okunur'}`;
    $('#fyKaydet').disabled = !YONETICI || n === 0;
  }
  cizUrunler.ertele = () => { clearTimeout(cizUrunler.z); cizUrunler.z = setTimeout(() => { const a = document.activeElement; const kod = a && a.dataset ? a.dataset.k : null, alan = a && a.dataset ? a.dataset.alan : null; cizUrunler(); if (kod && alan) { const i = $(`#fyUrunTablo input[data-k="${kod}"][data-alan="${alan}"]`); if (i) { i.focus(); } } }, 600); };
  async function saticiKaydet() {
    const sayiYa = v => sayiMi(v) ? Number(v) : null, mesajlar = []; let hata = false;
    // 1) maliyet bileşenleri (toplu ambalaj, toplu risk payı, değişen kalemler)
    const md = malDegisen();
    if (md.length) {
      const kodlar = [...new Set(md.filter(i => i.dataset.k).map(i => i.dataset.k))], al = (k, alan) => { const i = $(`#fyUrunTablo input.fy-mal[data-k="${k}"][data-alan="${alan}"]`); return i ? sayiYa(i.value) : null; };
      const j = await post('/FiyatYonetimi/MaliyetKaydet', { ambalajTipi: $('#fyAmbalajTipi').value, genelAmbalaj: sayiYa($('#fyGenelAmbalaj').value), genelRisk: sayiYa($('#fyGenelRisk').value), urunler: kodlar.map(k => ({ kod: k, ambalaj: al(k, 'ambalaj'), risk: al(k, 'risk') })) });
      mesajlar.push(esc(j.message)); if (!j.success) hata = true;
    }
    // 2) kâr oranları (seçili üretici → satıcı zinciri)
    if (!hata && degisenSayisi('[data-p=satici]')) {
      const g = { uretici: URETICI, satici: $('#fySatici').value, genel: sayiYa($('#fyGenel').value), gruplar: $$('#fyGruplar input').map(i => ({ anahtar: i.dataset.k, oran: sayiYa(i.value) })), urunler: $$('#fyUrunTablo input.fy-oran').filter(i => (i.value || '') !== (i.dataset.eski || '')).map(i => ({ anahtar: i.dataset.k, oran: sayiYa(i.value) })) };
      const j = await post('/FiyatYonetimi/SaticiKaydet', g); mesajlar.push(esc(j.message)); if (!j.success) hata = true;
    }
    mesaj(hata ? 'danger' : 'success', mesajlar.join(' · ') || 'Değişiklik yok.'); if (!hata) await yenile();
  }
  async function saticiExcel() {
    if (!window.TabloExcel) { mesaj('danger', 'Excel bileşeni yüklenemedi.'); return; }
    const k = kurDegerleri();
    try { await TabloExcel.indir('yetkili_satici_fiyat_listesi_' + $('#fySatici').value.replace(/\s+/g, '_'), [{ tablo: '#fyUrunTablo', ad: 'Satıcı Fiyat Listesi', baslik: `Yetkili Satıcı Fiyat Listesi · ${URETICI} → ${$('#fySatici').value}`, altBaslik: `Toplam maliyet = (üretim + ambalaj) × (1 + risk payı) · fiyatlandırma kuru USD/TL ${k4(k.fiyatUsdTl)}${kurKirli().length ? ' (kaydedilmemiş ekran değeri)' : ''}` }]); } catch (x) { mesaj('danger', 'Excel üretilemedi: ' + esc(x.message)); }
  }

  // ================= SEKME 2: MÜŞTERİ =================
  let SECILI = null;
  function musteriListesi() {
    const satici = $('#fyMSatici').value, ara = ($('#fyMAra').value || '').toLocaleLowerCase('tr-TR'), sf = $('#fyMSinif').value, tf = $('#fyMTanim').value;
    const l = V.musteriler.map(c => ({ c, t: musteriTanimi(satici, c.kod) })).filter(x => (!sf || x.c.sinif === sf) && (!tf || (tf === 'var') === !!x.t) && (!ara || [x.c.kod, x.c.kisaAd, x.c.resmiUnvan, x.c.sinif].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    $('#fyMusteriListe tbody').innerHTML = l.map(x => { const genel = x.t ? x.t.oranlar.find(o => o.kapsam === 'Genel') : null; return `<tr class="tik ${SECILI === x.c.kod ? 'secili' : ''}" data-kod="${esc(x.c.kod)}"><td><b>${esc(x.c.kod)}</b></td><td>${esc(x.c.kisaAd)}<br><small class="text-muted">${esc(x.c.bolge)} · ${esc(x.c.ulke)}</small></td><td>${sinifRozet(x.c.sinif)}</td><td class="s">${genel ? pct(genel.oran) : '<span class="text-muted">—</span>'}</td><td class="s">${x.t ? `${x.t.oranlar.filter(o => o.kapsam === 'Grup').length} / ${x.t.oranlar.filter(o => o.kapsam === 'Urun').length}` : '<span class="text-muted">Tanımsız</span>'}</td></tr>`; }).join('') || '<tr><td colspan="5" class="na-bos">Müşteri yok.</td></tr>';
    $$('#fyMusteriListe tr.tik').forEach(r => r.addEventListener('click', () => { SECILI = r.dataset.kod; musteriListesi(); musteriYukle(); }));
    $('#fyMListeNot').textContent = `${l.length} müşteri · ${V.musteriTanimlari.filter(m => m.satici === satici).length} tanımlı`;
    $('#fyNMusteri').textContent = V.musteriTanimlari.length;
    // (satıcı kutusu üst zincir şeridine taşındı)
    $('#fyMGrpAlis').textContent = `Satıcı Alış Fiyatı · ${URETICI} → ${satici}`; if (!SECILI) $('#fyMGrpSatis').textContent = `Müşteriye Satış Fiyatı · ${satici} → Müşteri`;
  }
  function musteriYukle() {
    const satici = $('#fyMSatici').value, c = V.musteriler.find(x => x.kod === SECILI);
    const mk = $('#fyMMusteriKutu');
    if (!c) { $('#fyMCari').innerHTML = '<div class="na-bos" style="grid-column:1/-1">Soldaki listeden müşteri seçin.</div>'; mk.innerHTML = '<i class="fas fa-user me-1"></i>Müşteri<small>Seçilmedi · soldaki listeden seçin</small>'; delete mk.dataset.url; mk.dataset.git = 'musteri|#fyMAra'; mk.title = 'Müşteri seçmek için listeye git'; $('#fyMGruplar').innerHTML = ''; $('#fyMUrunTablo tbody').innerHTML = '<tr><td colspan="13" class="na-bos">Müşteri seçilmedi.</td></tr>'; $('#fyMGenel').value = ''; $('#fyMGenel').disabled = true; $('#fyMGenelRisk').value = ''; $('#fyMGenelRisk').disabled = true; $('#fyMKaydet').disabled = true; $('#fyMDegisiklik').textContent = ''; return; }
    const t = musteriTanimi(satici, c.kod) || { oranlar: [] };
    mk.innerHTML = `<i class="fas fa-user me-1"></i>${esc(c.kod)} · ${esc(c.kisaAd)}<small>${esc(c.sinif)} · ${esc(c.iliskiDurumu)}${t.zaman ? ' · son kayıt ' + esc(t.zaman) + ' ' + esc(t.kullanici) : ' · tanım yok'}</small>`;
    delete mk.dataset.git; mk.dataset.url = '/CariKart/Musteri?kod=' + encodeURIComponent(c.kod); mk.dataset.baslik = 'Müşteri Cari Kartları'; mk.title = `${c.kisaAd} cari kartını aç (${c.kod})`;
    $('#fyMGrpAlis').textContent = `Satıcı Alış Fiyatı · ${URETICI} → ${satici}`; $('#fyMGrpSatis').textContent = `Müşteriye Satış Fiyatı · ${satici} → ${c.kisaAd}`;
    $('#fyMCari').innerHTML = cariHtml(c);
    const genel = t.oranlar.find(o => o.kapsam === 'Genel');
    $('#fyMGenel').value = genel ? genel.oran : ''; $('#fyMGenel').dataset.eski = genel ? genel.oran : ''; $('#fyMGenel').disabled = !YONETICI; $('#fyMGenel').classList.remove('degisti');
    { const gr = $('#fyMGenelRisk'); gr.value = t.risk ?? ''; gr.dataset.eski = gr.value; gr.disabled = !YONETICI; gr.classList.remove('degisti'); }
    $('#fyMGruplar').innerHTML = V.gruplar.map(g => { const o = t.oranlar.find(x => x.kapsam === 'Grup' && x.anahtar === g.anahtar); const faal = (c.faaliyetGruplari || []).includes(g.anahtar); return `<div style="${faal ? 'border-color:#fdba74' : ''}"><b>${esc(g.ad)}<small>${faal ? 'Müşterinin faaliyet grubu' : 'Grup oranı'}</small></b><span>%</span>${oranGiris('', o ? o.oran : '', g.anahtar, 'Grup')}</div>`; }).join('');
    $$('#fyMGruplar input, #fyMGenel').forEach(i => i.addEventListener('input', () => { degistiIsaretle(i); cizMUrunler(); }));
    cizMUrunler();
  }
  function cizMUrunler() {
    const satici = $('#fyMSatici').value, c = V.musteriler.find(x => x.kod === SECILI); if (!c) return;
    const t = musteriTanimi(satici, c.kod) || { oranlar: [] }, sOran = saticiOranlari(satici);
    const kalemler = {}; $$('#fyMUrunTablo input.fy-oran').forEach(i => kalemler[i.dataset.k] = i.value);
    const oranlar = []; const g = $('#fyMGenel').value; if (sayiMi(g)) oranlar.push({ kapsam: 'Genel', anahtar: '', oran: Number(g) });
    $$('#fyMGruplar input').forEach(i => { if (sayiMi(i.value)) oranlar.push({ kapsam: 'Grup', anahtar: i.dataset.k, oran: Number(i.value) }); });
    V.urunler.forEach(u => { const v = u.kod in kalemler ? kalemler[u.kod] : (t.oranlar.find(x => x.kapsam === 'Urun' && x.anahtar === u.kod) || {}).oran; if (sayiMi(v)) oranlar.push({ kapsam: 'Urun', anahtar: u.kod, oran: Number(v) }); });
    const ara = ($('#fyMUrunAra').value || '').toLocaleLowerCase('tr-TR'), gf = $('#fyMGrupF').value;
    const riskler = {}; $$('#fyMUrunTablo input.fy-mrisk').forEach(i => riskler[i.dataset.k] = i.value); const gRisk = sayiMi($('#fyMGenelRisk').value) ? Number($('#fyMGenelRisk').value) : 0;
    const riskCoz = u => { const eski = ((t.urunRisk || []).find(x => x.kod === u.kod) || {}).oran ?? '', giris = u.kod in riskler ? riskler[u.kod] : eski; return { eski, giris, deger: sayiMi(giris) ? Number(giris) : gRisk }; };
    const l = V.urunler.map(u => { const alis = saticiFiyati(sOran, u), mc = oranCoz(oranlar, u), rk = riskCoz(u), satis = Math.round(alis * (1 + rk.deger / 100) * (1 + mc.oran / 100) * 100) / 100; return { u, alis, mc, rk, satis }; }).filter(x => (!gf || x.u.grupAnahtari === gf) && (!ara || [x.u.kod, x.u.ad, x.u.seri].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    // EUR / TL karşılığı: fiyatlandırma kuru (piyasa + döviz kâr marjı) — sunucudaki FyKur.TlKarsiligi / EurKarsiligi ile aynı formül
    $('#fyMUrunTablo tbody').innerHTML = l.map(x => { const u = x.u, eski = (t.oranlar.find(o => o.kapsam === 'Urun' && o.anahtar === u.kod) || {}).oran ?? '', kalem = u.kod in kalemler ? kalemler[u.kod] : eski; return `<tr data-kod="${esc(u.kod)}"><td><b>${esc(u.kod)}</b></td><td><b>${esc(u.ad)}</b><small>${esc(u.hiyerarsi)}</small></td><td>${esc(u.anaGrup)}<small>${esc(u.grup)}</small></td><td class="s">${tl(x.alis)}<small>${pct(oranCoz(sOran, u).oran)} ${esc(oranCoz(sOran, u).kaynak)}</small></td><td class="s">${tl(tlKarsiligi(x.alis))}</td><td class="s"><input type="number" step="0.5" min="0" class="form-control fy-mrisk ${String(x.rk.giris) !== String(x.rk.eski) ? 'degisti' : ''}" data-k="${esc(u.kod)}" data-alan="risk" data-eski="${x.rk.eski}" value="${x.rk.giris}" placeholder="${sayi(gRisk)}" title="Boş = müşterinin toplu risk payı" ${YONETICI ? '' : 'disabled'}></td><td class="s"><input type="number" step="0.01" class="form-control fy-oran ${String(kalem) !== String(eski) ? 'degisti' : ''}" data-k="${esc(u.kod)}" data-alan="oran" data-kapsam="Urun" data-eski="${eski}" value="${kalem}" placeholder="—" ${YONETICI ? '' : 'disabled'}></td><td class="s"><b>${pct(x.mc.oran)}</b></td><td>${kaynakRozet(x.mc.kaynak)}</td><td class="s"><b>${tl(x.satis)}</b></td><td class="s">${tl(eurKarsiligi(x.satis))}</td><td class="s">${tl(tlKarsiligi(x.satis))}</td><td class="s">${pct((x.satis / u.maliyet - 1) * 100)}</td></tr>`; }).join('') || '<tr><td colspan="13" class="na-bos">Filtreye uyan ürün yok.</td></tr>';
    $$('#fyMUrunTablo input.fy-oran, #fyMUrunTablo input.fy-mrisk').forEach(i => i.addEventListener('input', () => { degistiIsaretle(i); cizMUrunler.ertele(); }));
    const ta = l.reduce((a, x) => a + x.alis, 0), ts = l.reduce((a, x) => a + x.satis, 0), tm = l.reduce((a, x) => a + x.u.maliyet, 0);
    $('#fyMTopAlis').textContent = tl(ta) + ' $'; $('#fyMTopAlisTl').textContent = tl(tlKarsiligi(ta)) + ' ₺'; $('#fyMTopSatis').textContent = tl(ts) + ' $'; $('#fyMTopEur').textContent = tl(eurKarsiligi(ts)) + ' €'; $('#fyMTopTl').textContent = tl(tlKarsiligi(ts)) + ' ₺'; $('#fyMOrtOran').textContent = ta ? pct((ts / ta - 1) * 100) + ' ort.' : '—'; $('#fyMOrtMarj').textContent = tm ? pct((ts / tm - 1) * 100) : '—';
    const tanimsiz = l.filter(x => x.mc.kaynak === 'Tanımsız').length, n = degisenSayisi('[data-p=musteri]') + mRiskDegisen().length;
    $('#fyMDegisiklik').innerHTML = `${l.length} ürün · ${tanimsiz ? `<span class="text-danger">${tanimsiz} ürün tanımsız (satıcı alış fiyatına, kârsız)</span> · ` : ''}${n ? `<b class="text-warning">${n} kaydedilmemiş değişiklik</b>` : 'Kaydedilmemiş değişiklik yok'}${YONETICI ? '' : ' · salt okunur'}`;
    $('#fyMKaydet').disabled = !YONETICI || n === 0;
  }
  cizMUrunler.ertele = () => { clearTimeout(cizMUrunler.z); cizMUrunler.z = setTimeout(() => { const a = document.activeElement; const kod = a && a.dataset ? a.dataset.k : null, alan = a && a.dataset ? a.dataset.alan : null; cizMUrunler(); if (kod && alan) { const i = $(`#fyMUrunTablo input[data-k="${kod}"][data-alan="${alan}"]`); if (i) i.focus(); } }, 600); };
  const mRiskDegisen = () => $$('[data-p=musteri] .fy-mrisk').filter(i => (i.value || '') !== (i.dataset.eski || ''));
  async function musteriKaydet() {
    if (!SECILI) return;
    const g = { satici: $('#fyMSatici').value, cariKodu: SECILI, genel: sayiMi($('#fyMGenel').value) ? Number($('#fyMGenel').value) : null, gruplar: $$('#fyMGruplar input').map(i => ({ anahtar: i.dataset.k, oran: sayiMi(i.value) ? Number(i.value) : null })), urunler: $$('#fyMUrunTablo input.fy-oran').filter(i => (i.value || '') !== (i.dataset.eski || '')).map(i => ({ anahtar: i.dataset.k, oran: sayiMi(i.value) ? Number(i.value) : null })),
      riskGonderildi: true, risk: sayiMi($('#fyMGenelRisk').value) ? Number($('#fyMGenelRisk').value) : null, urunRiskler: $$('#fyMUrunTablo input.fy-mrisk').filter(i => (i.value || '') !== (i.dataset.eski || '')).map(i => ({ anahtar: i.dataset.k, oran: sayiMi(i.value) ? Number(i.value) : null })) };
    const j = await post('/FiyatYonetimi/MusteriKaydet', g); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) await yenile();
  }
  async function musteriExcel() {
    const c = V.musteriler.find(x => x.kod === SECILI); if (!c) { mesaj('warning', 'Önce müşteri seçin.'); return; }
    if (!window.TabloExcel) { mesaj('danger', 'Excel bileşeni yüklenemedi.'); return; }
    const k = kurDegerleri();
    try { await TabloExcel.indir('musteri_fiyat_listesi_' + c.kod, [{ tablo: '#fyMUrunTablo', ad: 'Müşteri Fiyat Listesi', baslik: `Müşteri Fiyat Listesi · ${c.kod} ${c.kisaAd} · ${URETICI} → ${$('#fyMSatici').value}`, altBaslik: `Satış fiyatı = satıcı alış fiyatı × (1 + risk payı) × (1 + kâr oranı) · fiyatlandırma kuru USD/TL ${k4(k.fiyatUsdTl)} · EUR paritesi ${k4(k.fiyatParite)}${kurKirli().length ? ' (kaydedilmemiş ekran değeri)' : ''}` }]); } catch (x) { mesaj('danger', 'Excel üretilemedi: ' + esc(x.message)); }
  }
  function ac(url, baslik) { if (window.top && window.top !== window && typeof window.top.portalSekmeAc === 'function') window.top.portalSekmeAc(baslik, url); else window.open(url + (sekmeli ? (url.includes('?') ? '&' : '?') + '_sekme=1' : ''), '_blank'); }

  // ================= SEKME 3: ÖZET VE GÜNLÜK =================
  function cizOzet() {
    const bas = `<tr><th>Yetkili Satıcı</th><th class="s">Genel</th>${V.gruplar.map(g => `<th class="s">${esc(g.ad)}</th>`).join('')}<th class="s">Kalem Tanımı</th><th class="s">Ort. Oran</th><th class="s">Müşteri</th></tr>`;
    $('#fySaticiMatris thead').innerHTML = bas;
    $('#fySaticiMatris tbody').innerHTML = zincirSaticilar().map(s => { const genel = sOr(s).find(o => o.kapsam === 'Genel'); const tm = V.urunler.reduce((a, u) => a + u.maliyet, 0), tf = V.urunler.reduce((a, u) => a + saticiFiyati(sOr(s), u), 0); return `<tr><td><b>${esc(s.ad)}</b><br><small class="text-muted">${esc(s.cariKodu)} · ${esc(s.bolge)}</small></td><td class="s">${genel ? pct(genel.oran) : '—'}</td>${V.gruplar.map(g => { const o = sOr(s).find(x => x.kapsam === 'Grup' && x.anahtar === g.anahtar); return `<td class="s">${o ? `<b>${pct(o.oran)}</b>` : genel ? `<span class="text-muted">${pct(genel.oran)}</span>` : '<span class="text-danger">—</span>'}</td>`; }).join('')}<td class="s">${sOr(s).filter(o => o.kapsam === 'Urun').length}</td><td class="s">${pct((tf / tm - 1) * 100)}</td><td class="s">${s.musteriSayisi}</td></tr>`; }).join('');
    const satici = $('#fyOSatici').value, sOran = saticiOranlari(satici);
    $('#fyMusteriMatris thead').innerHTML = `<tr><th>Müşteri</th><th>Cari Sınıfı</th><th class="s">Genel</th>${V.gruplar.map(g => `<th class="s">${esc(g.ad)}</th>`).join('')}<th class="s">Kalem</th><th class="s">Ort. Uçtan Uca Marj</th><th>Son Kayıt</th></tr>`;
    const ml = V.musteriTanimlari.filter(m => m.satici === satici);
    // (boş satır colspan: 6 sabit sütun + grup sütunları)
    $('#fyMusteriMatris tbody').innerHTML = ml.map(m => { const c = V.musteriler.find(x => x.kod === m.cariKodu) || {}; const genel = m.oranlar.find(o => o.kapsam === 'Genel'); const tm = V.urunler.reduce((a, u) => a + u.maliyet, 0), ts = V.urunler.reduce((a, u) => a + saticiFiyati(sOran, u) * (1 + mRisk(m, u) / 100) * (1 + oranCoz(m.oranlar, u).oran / 100), 0); return `<tr><td><b>${esc(m.cariKodu)}</b> · ${esc(c.kisaAd || '')}</td><td>${sinifRozet(c.sinif)} ${iliskiRozet(c.iliskiDurumu)}</td><td class="s">${genel ? pct(genel.oran) : '—'}</td>${V.gruplar.map(g => { const o = m.oranlar.find(x => x.kapsam === 'Grup' && x.anahtar === g.anahtar); return `<td class="s">${o ? `<b>${pct(o.oran)}</b>` : genel ? `<span class="text-muted">${pct(genel.oran)}</span>` : '<span class="text-danger">—</span>'}</td>`; }).join('')}<td class="s">${m.oranlar.filter(o => o.kapsam === 'Urun').length}</td><td class="s">${pct((ts / tm - 1) * 100)}</td><td><small>${esc(m.zaman)} · ${esc(m.kullanici)}</small></td></tr>`; }).join('') || `<tr><td colspan="${6 + V.gruplar.length}" class="na-bos">${esc(satici)} için müşteri tanımı yok.</td></tr>`;
    $('#fyAudit tbody').innerHTML = V.audit.map(a => `<tr><td>${esc(a.zaman)}</td><td>${esc(a.kullanici)}</td><td>${esc(a.katman)}</td><td>${esc(a.hedef)}</td><td>${esc(a.alan)}</td><td class="s">${esc(a.eski || '—')}</td><td class="s"><b>${esc(a.yeni || '(silindi)')}</b></td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Kayıt yok.</td></tr>';
  }

  // ================= YENİ SEKMELER (13 Kas 2026) =================
  // Müşteri Fiyat Ortalamaları : seçili yetkili satıcının TÜM müşterilerine fiyat listesi ortalamaları tek tabloda (grup grup ortalama fiyat + marj, genel ortalama, en düşük / en yüksek marj)
  // Kâr Oranı Sorgusu          : % eşiği girilir → kâr oranı eşiğin altında (ya da üstünde) kalan ÜRÜN × MÜŞTERİ fiyatları; ürün / grup / müşteri araması; istenirse yalnız satışı olanlar
  // Ürün Fiyat Karşılaştırması : aynı ürün müşteriler arasında kaça fiyatlanmış — en düşük / en yüksek fiyat ve müşterisi, fark %
  // Satış Uyarıları            : şirket + minimum kâr oranı → kesilen satış faturalarında (Konsolide Satış Raporu verisi) kâr oranı minimumun altında kalan kalemler; sekmede uyarı sayısı
  // Kâr oranı bu ekranın tanımıyla MALİYET ÜZERİNDENDİR: (fiyat ÷ maliyet − 1). Yeni sekmeler yalnız okur; oranlar ilk iki sekmeden kaydedilir.
  const TRL = 'tr-TR', kck = s => String(s ?? '').toLocaleLowerCase(TRL), topl = a => a.reduce((x, y) => x + (Number(y) || 0), 0);
  const tam = v => (Number(v) || 0).toLocaleString(TRL, { maximumFractionDigits: 0 });
  const trh = s => s ? s.slice(8, 10) + '.' + s.slice(5, 7) + '.' + s.slice(0, 4) : '';
  const fiyatSatiri = (satici, c, u, sOran, t) => { const alis = saticiFiyati(sOran, u), mc = oranCoz(t ? t.oranlar : [], u), risk = mRisk(t, u), satis = Math.round(alis * (1 + risk / 100) * (1 + mc.oran / 100) * 100) / 100; return { satici, c, u, t, alis, mc, risk, satis, marj: u.maliyet ? (satis / u.maliyet - 1) * 100 : 0 }; };
  const marjRenk = v => v < 0 ? 'fy-fark negatif' : '';

  // ---- ortak liste çizici: başlığa tıklayınca sıralama, dip satırı, satır sınırı (Excel'de sınır kalkar)
  const LD = {};
  function listeCiz(id, kol, rows, o = {}) {
    const d = LD[id] || (LD[id] = { k: o.sira || null, y: o.yon || 1 }), tablo = $(id);
    if (d.k) { const c = kol.find(x => x.k === d.k); if (c) rows = rows.slice().sort((a, b) => { const x = c.v(a), y = c.v(b); return (typeof x === 'number' && typeof y === 'number' ? x - y : String(x ?? '').localeCompare(String(y ?? ''), TRL, { numeric: true })) * d.y; }); }
    const limit = d.tum ? Infinity : (o.limit || 500), goster = rows.slice(0, limit);
    $('thead', tablo).innerHTML = (o.grup || '') + `<tr>${kol.map(c => `<th data-k="${c.k}" class="${c.s ? 's ' : ''}${d.k === c.k ? (d.y > 0 ? 'art' : 'azal') : ''}" title="${esc(c.ac || c.ad)}${c.ik ? '' : ' · sıralamak için tıklayın'}">${c.ik ? c.adHtml : esc(c.ad)}</th>`).join('')}</tr>`;
    const govde = goster.map((r, i) => `<tr class="${o.tik ? 'tik' : ''}" data-i="${i}">${kol.map(c => `<td class="${c.s ? 's' : ''}">${c.h ? c.h(r) : esc(c.v(r))}</td>`).join('')}</tr>`).join('');
    $('tbody', tablo).innerHTML = (govde + (rows.length > goster.length ? `<tr><td colspan="${kol.length}" class="na-bos">İlk ${tam(limit)} satır gösteriliyor (toplam ${tam(rows.length)}). Süzgeçle daraltın; Excel tümünü içerir.</td></tr>` : '')) || `<tr><td colspan="${kol.length}" class="na-bos">${esc(o.bos || 'Kayıt yok.')}</td></tr>`;
    $('tfoot', tablo).innerHTML = o.dip && rows.length ? `<tr>${o.dip(rows)}</tr>` : '';
    tablo._rows = goster; tablo._ciz = o.yeniden; tablo._tik = o.tik;
  }
  kok.addEventListener('click', e => {
    const th = e.target.closest('table.fy-liste thead tr:not(.fy-grp) th[data-k]');
    if (th) { const t = th.closest('table'), d = LD['#' + t.id]; if (!d || !th.dataset.k) return; d.y = d.k === th.dataset.k ? -d.y : (th.classList.contains('s') ? -1 : 1); d.k = th.dataset.k; if (t._ciz) t._ciz(); return; }
    const tr = e.target.closest('table.fy-liste tbody tr.tik'); if (!tr || e.target.closest('a,button,input')) return; const t = tr.closest('table'), r = t._rows && t._rows[+tr.dataset.i]; if (r && t._tik) t._tik(r);
  });
  async function listeExcel(id, dosya, ad, alt) {
    if (!window.TabloExcel) { mesaj('danger', 'Excel bileşeni yüklenemedi.'); return; }
    const d = LD[id], t = $(id); if (!d) return; d.tum = true; t._ciz();
    try { await TabloExcel.indir(dosya, [{ tablo: id, ad, baslik: 'Müşteri Fiyat ve Kâr Oranları · ' + ad, altBaslik: alt }]); } catch (x) { mesaj('danger', 'Excel üretilemedi: ' + esc(x.message)); }
    d.tum = false; t._ciz();
  }
  /** müşteri fiyat listesine git (Müşteri Fiyatları sekmesi): satıcı + müşteri seçilir, isteğe bağlı ürün araması doldurulur */
  function musteriyeGit(satici, kod, urun) { if ($('#fySatici').value !== satici) { $('#fySatici').value = satici; saticiYay(); $('#fyUrunTablo tbody').innerHTML = ''; saticiYukle(); } SECILI = kod; $('#fyMUrunAra').value = urun || ''; musteriListesi(); $('#fyMUrunTablo tbody').innerHTML = ''; musteriYukle(); sekme('musteri'); }

  // ================= MÜŞTERİ FİYAT ORTALAMALARI =================
  function cizOrtalama() {
    const satici = $('#fyOrtSatici').value, ara = kck($('#fyOrtAra').value), sf = $('#fyOrtSinif').value, tf = $('#fyOrtTanim').value, pb = $('#fyOrtPb').value, saticilar = satici ? [satici] : zincirSaticilar().map(s => s.ad);
    const cevir = usd => pb === 'TL' ? tlKarsiligi(usd) : pb === 'EUR' ? eurKarsiligi(usd) : usd, sem = pb === 'TL' ? '₺' : pb === 'EUR' ? '€' : '$';
    const rows = saticilar.flatMap(sat => { const sOran = saticiOranlari(sat); return V.musteriler.filter(c => (!sf || c.sinif === sf) && (!ara || kck([c.kod, c.kisaAd, c.resmiUnvan, c.sinif, c.temsilciler, c.ulke, sat].join(' ')).includes(ara))).map(c => {
      const t = musteriTanimi(sat, c.kod), l = V.urunler.map(u => fiyatSatiri(sat, c, u, sOran, t));
      const ts = topl(l.map(x => x.satis)), ta = topl(l.map(x => x.alis)), tm = topl(l.map(x => x.u.maliyet)), g = {};
      V.gruplar.forEach(gr => { const gl = l.filter(x => x.u.grupAnahtari === gr.anahtar), s = topl(gl.map(x => x.satis)), m = topl(gl.map(x => x.u.maliyet)); g[gr.anahtar] = { n: gl.length, ort: gl.length ? s / gl.length : 0, marj: m ? (s / m - 1) * 100 : 0 }; });
      const sirali = l.slice().sort((a, b) => a.marj - b.marj), genel = t ? t.oranlar.find(o => o.kapsam === 'Genel') : null;
      return { satici: sat, c, t, g, ort: l.length ? ts / l.length : 0, mOran: ta ? (ts / ta - 1) * 100 : 0, marj: tm ? (ts / tm - 1) * 100 : 0, enDusuk: sirali[0], enYuksek: sirali[sirali.length - 1], genel: genel ? genel.oran : null, nGrup: t ? t.oranlar.filter(o => o.kapsam === 'Grup').length : 0, nKalem: t ? t.oranlar.filter(o => o.kapsam === 'Urun').length : 0 };
    }); }).filter(r => !tf || (tf === 'var') === !!r.t);
    const uc = x => x ? `<b class="${marjRenk(x.marj)}">${pct(x.marj)}</b><small>${esc(x.u.ad)}</small>` : '—';
    const kol = [
      { k: 'ad', ad: 'Müşteri', v: r => r.c.kisaAd, h: r => `<b style="white-space:nowrap">${esc(r.c.kisaAd)}</b><small>${esc(r.c.kod)}</small>` },
      { k: 'satici', ad: 'Yetkili Satıcı', v: r => r.satici },
      { k: 'sinif', ad: 'Cari Sınıfı', v: r => r.c.sinif, h: r => sinifRozet(r.c.sinif) },
      { k: 'ulke', ad: 'Bölge · Ülke', v: r => (r.c.bolge || '') + ' ' + (r.c.ulke || ''), h: r => esc([r.c.bolge, r.c.ulke].filter(Boolean).join(' · ')) },
      { k: 'temsilci', ad: 'Satış Temsilcisi', v: r => r.c.temsilciler || '' },
      { k: 'genel', ad: 'Genel Oran', s: 1, v: r => r.t ? (r.genel ?? -1) : -2, h: r => r.t ? (r.genel != null ? pct(r.genel) : '—') : '<span class="text-danger">Tanımsız</span>' },
      ...V.gruplar.map(gr => ({ k: 'g_' + gr.anahtar, ad: gr.ad, ac: `${gr.ad}: ortalama müşteri satış fiyatı (${sem}) ve grubun uçtan uca marjı`, s: 1, v: r => r.g[gr.anahtar].ort, h: r => { const x = r.g[gr.anahtar], faal = (r.c.faaliyetGruplari || []).includes(gr.anahtar); return x.n ? `<b>${tl(cevir(x.ort))} ${sem}</b><small class="${faal ? 'faal' : ''}">${pct(x.marj)}${faal ? ' · Faaliyet Grubu' : ''}</small>` : '—'; } })),
      { k: 'ort', ad: `Ort. Satış Fiyatı (${sem})`, ac: 'Fiyat listesindeki bütün ürünlerin ortalama müşteri satış fiyatı', s: 1, v: r => r.ort, h: r => `<b>${tl(cevir(r.ort))} ${sem}</b>` },
      { k: 'mOran', ad: 'Ort. Müşteri Oranı', ac: 'Satıcı → müşteri ortalama kâr oranı (toplam satış fiyatı ÷ toplam satıcı alış fiyatı − 1)', s: 1, v: r => r.mOran, h: r => pct(r.mOran) },
      { k: 'marj', ad: 'Ort. Uçtan Uca Marj', ac: 'Toplam müşteri satış fiyatı ÷ toplam üretim maliyeti − 1', s: 1, v: r => r.marj, h: r => `<b class="${marjRenk(r.marj)}">${pct(r.marj)}</b>` },
      { k: 'enDusuk', ad: 'En Düşük Marj', s: 1, v: r => r.enDusuk ? r.enDusuk.marj : 0, h: r => uc(r.enDusuk) },
      { k: 'enYuksek', ad: 'En Yüksek Marj', s: 1, v: r => r.enYuksek ? r.enYuksek.marj : 0, h: r => uc(r.enYuksek) },
      { k: 'tanim', ad: 'Grup / Kalem Tanımı', s: 1, v: r => r.nGrup + r.nKalem, h: r => r.t ? `${r.nGrup} / ${r.nKalem}` : '—' }
    ];
    const ort = f => rows.length ? topl(rows.map(f)) / rows.length : 0;
    listeCiz('#fyOrtTablo', kol, rows, { sira: 'marj', yon: 1, yeniden: cizOrtalama, bos: 'Süzgece uyan müşteri yok.', tik: r => musteriyeGit(r.satici, r.c.kod),
      grup: `<tr class="fy-grp"><th colspan="6">Müşteri · ${esc(satici || 'Tüm Satıcılar')} Fiyat Listesi</th><th colspan="${V.gruplar.length}">Ürün Grubu · Ortalama Müşteri Satış Fiyatı (${sem}) · Uçtan Uca Marj</th><th colspan="6">Fiyat Listesi Geneli · ${V.urunler.length} Ürün</th></tr>`,
      dip: () => `<td colspan="6">Ortalama · ${rows.length} Fiyat Listesi</td>${V.gruplar.map(gr => `<td class="s">${tl(cevir(ort(r => r.g[gr.anahtar].ort)))} ${sem}<small>${pct(ort(r => r.g[gr.anahtar].marj))}</small></td>`).join('')}<td class="s">${tl(cevir(ort(r => r.ort)))} ${sem}</td><td class="s">${pct(ort(r => r.mOran))}</td><td class="s">${pct(ort(r => r.marj))}</td><td colspan="3"></td>` });
    $('#fyOrtNot').textContent = `${rows.length} fiyat listesi · ${new Set(rows.map(r => r.c.kod)).size} müşteri · ${rows.filter(r => r.t).length} tanımlı · ${V.urunler.length} ürün · fiyatlandırma kuru USD/TL ${k4(kurDegerleri().fiyatUsdTl)} · satıra tıklayın: müşterinin fiyat listesi`;
  }

  // ================= KÂR ORANI SORGUSU =================
  /** son 12 ayın fatura satışları: cariKod|ürünKod → { kg, tl, son } (Konsolide Satış Raporu verisi) */
  function satisOzeti() {
    if (!SATIS) return null; if (SATIS.ozet) return SATIS.ozet;
    const d = new Date(+SATIS.bugun.slice(0, 4) - 1, +SATIS.bugun.slice(5, 7) - 1, +SATIS.bugun.slice(8, 10)), bas = d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0'), m = {};
    SATIS.kalem.forEach(x => { if (x.b.tarih < bas) return; const a = x.b.cariKod + '|' + x.k.kod, o = m[a] || (m[a] = { kg: 0, tl: 0, son: '' }); o.kg += x.k.birim === 'kg' ? x.k.miktar : 0; o.tl += x.netTl; if (x.b.tarih > o.son) o.son = x.b.tarih; });
    return SATIS.ozet = m;
  }
  function cizSorgu() {
    const esik = $('#fySorEsik').value, yon = $('#fySorYon').value, olcu = $('#fySorOlcu').value, sat = $('#fySorSatici').value, ara = kck($('#fySorUrun').value), gf = $('#fySorGrup').value, mara = kck($('#fySorMusteri').value), sf = $('#fySorSinif').value, tanimli = $('#fySorTanimli').checked, satisli = $('#fySorSatisli').checked;
    if (satisli && !SATIS) { $('#fySorNot').textContent = 'Satış verisi yükleniyor…'; satisYukle().then(cizSorgu).catch(x => { $('#fySorSatisli').checked = false; mesaj('danger', 'Satış verisi alınamadı: ' + esc(x.message)); cizSorgu(); }); return; }
    const so = satisOzeti(), rows = [], uc = {}, urunler = V.urunler.filter(u => (!gf || u.grupAnahtari === gf) && (!ara || kck([u.kod, u.ad, u.seri, u.urunGrubu, u.urunSinifi].join(' ')).includes(ara)));
    const musteriler = V.musteriler.filter(c => (!sf || c.sinif === sf) && (!mara || kck([c.kod, c.kisaAd, c.resmiUnvan].join(' ')).includes(mara)));
    (sat ? [sat] : zincirSaticilar().map(s => s.ad)).forEach(satici => {
      const sOran = saticiOranlari(satici);
      musteriler.forEach(c => {
        const t = musteriTanimi(satici, c.kod); if (tanimli && !t) return;
        urunler.forEach(u => {
          const x = fiyatSatiri(satici, c, u, sOran, t); x.deger = olcu === 'musteri' ? x.mc.oran : x.marj; x.sa = so ? so[c.kod + '|' + u.kod] : null;
          if (satisli && !x.sa) return;
          if (!uc.min || x.deger < uc.min.deger) uc.min = x; if (!uc.max || x.deger > uc.max.deger) uc.max = x;
          if (sayiMi(esik) && !(yon === 'alt' ? x.deger < Number(esik) : x.deger > Number(esik))) return;
          x.fark = sayiMi(esik) ? x.deger - Number(esik) : 0; rows.push(x);
        });
      });
    });
    const olcuAd = olcu === 'musteri' ? 'Müşteri Oranı' : 'Uçtan Uca Marj', ucx = yon === 'alt' ? uc.min : uc.max;
    const bos = sayiMi(esik) && ucx ? `${olcuAd} % ${esik} ${yon === 'alt' ? 'altında' : 'üstünde'} fiyat yok. Süzgeçlere uyan en ${yon === 'alt' ? 'düşük' : 'yüksek'} oran: ${pct(ucx.deger)} (${ucx.u.ad} · ${ucx.c.kisaAd} · ${ucx.satici}).` : 'Koşula uyan ürün × müşteri fiyatı yok.';
    const kol = [
      { k: 'kod', ad: 'Ürün Kodu', v: x => x.u.kod, h: x => `<b>${esc(x.u.kod)}</b>` },
      { k: 'urun', ad: 'Ürün Adı', v: x => x.u.ad, h: x => `<b>${esc(x.u.ad)}</b><small>${esc(x.u.hiyerarsi)}</small>` },
      { k: 'grup', ad: 'Ürün Grubu', v: x => x.u.anaGrup + ' ' + x.u.grup, h: x => `${esc(x.u.grup)}<small>${esc(x.u.anaGrup)}</small>` },
      { k: 'satici', ad: 'Yetkili Satıcı', v: x => x.satici },
      { k: 'musteri', ad: 'Müşteri', v: x => x.c.kisaAd, h: x => `<b>${esc(x.c.kisaAd)}</b><small>${esc(x.c.kod)}</small>` },
      { k: 'sinif', ad: 'Cari Sınıfı', v: x => x.c.sinif, h: x => sinifRozet(x.c.sinif) },
      { k: 'maliyet', ad: 'Toplam Maliyet ($)', ac: 'Üretim + ambalaj maliyeti, risk payı dahil', s: 1, v: x => x.u.maliyet, h: x => tl(x.u.maliyet) },
      { k: 'alis', ad: 'Satıcı Alış Fiyatı ($)', s: 1, v: x => x.alis, h: x => tl(x.alis) },
      { k: 'oran', ad: 'Müşteri Oranı', ac: 'Satıcı → müşteri kâr oranı ve tanımın kaynağı (Kalem > Grup > Genel)', s: 1, v: x => x.mc.oran, h: x => `<b>${pct(x.mc.oran)}</b> ${kaynakRozet(x.mc.kaynak)}` },
      { k: 'satis', ad: 'Müşteriye Satış Fiyatı ($)', s: 1, v: x => x.satis, h: x => `<b>${tl(x.satis)}</b>` },
      { k: 'satisTl', ad: 'Müşteriye Satış Fiyatı (₺)', s: 1, v: x => x.satis, h: x => tl(tlKarsiligi(x.satis)) },
      { k: 'marj', ad: 'Uçtan Uca Marj', ac: 'Müşteri satış fiyatı ÷ üretim maliyeti − 1', s: 1, v: x => x.marj, h: x => `<b class="${marjRenk(x.marj)}">${pct(x.marj)}</b>` },
      { k: 'fark', ad: 'Eşik Farkı (Puan)', ac: `${olcuAd} − eşik`, s: 1, v: x => x.fark, h: x => sayiMi(esik) ? `<span class="fy-fark ${x.fark < 0 ? 'negatif' : 'pozitif'}">${x.fark > 0 ? '+' : ''}${tl(x.fark)}</span>` : '—' },
      { k: 'kg', ad: 'Satış · Son 12 Ay (kg)', ac: 'Bu müşteriye bu üründen son 12 ayda kesilen fatura miktarı (Konsolide Satış Raporu)', s: 1, v: x => x.sa ? x.sa.kg : -1, h: x => !so ? '<span class="text-muted">—</span>' : x.sa ? `<b>${tam(x.sa.kg)}</b><small>${trh(x.sa.son)}</small>` : '<span class="text-muted">Satış Yok</span>' },
      { k: 'stl', ad: 'Net Satış · Son 12 Ay (₺)', s: 1, v: x => x.sa ? x.sa.tl : -1, h: x => x.sa ? tam(x.sa.tl) + ' ₺' : '<span class="text-muted">—</span>' }
    ];
    listeCiz('#fySorTablo', kol, rows, { sira: olcu === 'musteri' ? 'oran' : 'marj', yon: 1, yeniden: cizSorgu, bos, tik: x => musteriyeGit(x.satici, x.c.kod, x.u.kod) });
    $('#fySorNot').innerHTML = `${sayiMi(esik) ? `<b>${olcuAd} ${yon === 'alt' ? '&lt;' : '&gt;'} % ${esc(esik)}</b> · ` : ''}${tam(rows.length)} ürün × müşteri fiyatı · ${new Set(rows.map(x => x.u.kod)).size} ürün · ${new Set(rows.map(x => x.c.kod)).size} müşteri${so ? ` · satışı olan ${tam(rows.filter(x => x.sa).length)}` : ''} · satıra tıklayın: müşterinin fiyat listesi`;
  }

  // ================= ÜRÜN FİYAT KARŞILAŞTIRMASI =================
  function cizUrunK() {
    const satici = $('#fyUkSatici').value, ara = kck($('#fyUkAra').value), gf = $('#fyUkGrup').value, sOran = saticiOranlari(satici), enAz = Number($('#fyUkFark').value) || 0;
    const tanimlar = V.musteriTanimlari.filter(m => m.satici === satici).map(m => ({ t: m, c: V.musteriler.find(c => c.kod === m.cariKodu) })).filter(x => x.c);
    const rows = V.urunler.filter(u => (!gf || u.grupAnahtari === gf) && (!ara || kck([u.kod, u.ad, u.seri, u.urunGrubu].join(' ')).includes(ara))).map(u => {
      const l = tanimlar.map(x => fiyatSatiri(satici, x.c, u, sOran, x.t)).sort((a, b) => a.satis - b.satis), n = l.length, min = l[0], max = l[n - 1];
      return { u, n, alis: saticiFiyati(sOran, u), min, max, ort: n ? topl(l.map(x => x.satis)) / n : 0, fark: n && min.satis ? (max.satis / min.satis - 1) * 100 : 0, ortMarj: n && u.maliyet ? (topl(l.map(x => x.satis)) / n / u.maliyet - 1) * 100 : 0 };
    }).filter(r => r.fark >= enAz);
    const uc = x => x ? `<b>${tl(x.satis)} $</b><small>${esc(x.c.kisaAd)} · ${pct(x.marj)}</small>` : '—';
    const kol = [
      { k: 'kod', ad: 'Ürün Kodu', v: r => r.u.kod, h: r => `<b>${esc(r.u.kod)}</b>` },
      { k: 'urun', ad: 'Ürün Adı', v: r => r.u.ad, h: r => `<b>${esc(r.u.ad)}</b><small>${esc(r.u.hiyerarsi)}</small>` },
      { k: 'grup', ad: 'Ürün Grubu', v: r => r.u.anaGrup + ' ' + r.u.grup, h: r => `${esc(r.u.grup)}<small>${esc(r.u.anaGrup)}</small>` },
      { k: 'maliyet', ad: 'Toplam Maliyet ($)', ac: 'Üretim + ambalaj maliyeti, risk payı dahil', s: 1, v: r => r.u.maliyet, h: r => tl(r.u.maliyet) },
      { k: 'alis', ad: 'Satıcı Alış Fiyatı ($)', s: 1, v: r => r.alis, h: r => tl(r.alis) },
      { k: 'n', ad: 'Müşteri', ac: 'Bu satıcıda fiyat tanımı olan müşteri sayısı', s: 1, v: r => r.n },
      { k: 'min', ad: 'En Düşük Fiyat', ac: 'En düşük müşteri satış fiyatı, müşterisi ve uçtan uca marjı', s: 1, v: r => r.min ? r.min.satis : 0, h: r => uc(r.min) },
      { k: 'max', ad: 'En Yüksek Fiyat', s: 1, v: r => r.max ? r.max.satis : 0, h: r => uc(r.max) },
      { k: 'ort', ad: 'Ortalama Fiyat ($)', s: 1, v: r => r.ort, h: r => `<b>${tl(r.ort)}</b>` },
      { k: 'fark', ad: 'Fiyat Farkı', ac: 'En yüksek fiyat ÷ en düşük fiyat − 1', s: 1, v: r => r.fark, h: r => `<b>${pct(r.fark)}</b>` },
      { k: 'minMarj', ad: 'En Düşük Marj', s: 1, v: r => r.min ? r.min.marj : 0, h: r => r.min ? `<b class="${marjRenk(r.min.marj)}">${pct(r.min.marj)}</b>` : '—' },
      { k: 'ortMarj', ad: 'Ort. Uçtan Uca Marj', s: 1, v: r => r.ortMarj, h: r => pct(r.ortMarj) }
    ];
    listeCiz('#fyUkTablo', kol, rows, { sira: 'fark', yon: -1, yeniden: cizUrunK, bos: tanimlar.length ? 'Süzgece uyan ürün yok.' : `${satici} için müşteri fiyat tanımı yok.`, tik: r => { $('#fySorSatici').value = satici; $('#fySorUrun').value = r.u.kod; $('#fySorEsik').value = ''; $('#fySorTanimli').checked = true; sekme('sorgu'); } });
    $('#fyUkNot').textContent = `${rows.length} ürün · ${tanimlar.length} tanımlı müşteri · satıra tıklayın: ürünün müşteri müşteri fiyatları`;
  }

  // ================= SATIŞ UYARILARI =================
  let SATIS = null, satisSoz = null;
  /** kesilen satış faturalarının kalemleri (Konsolide Satış Raporu verisi): bir kez okunur */
  function satisYukle() {
    return satisSoz || (satisSoz = fetch('/SatisRaporu/Veri').then(r => { if (!r.ok) throw new Error('Sunucu yanıtı ' + r.status); return r.json(); }).then(j => {
      const kalem = []; (j.belgeler || []).forEach(b => { if (b.tur !== 'Fatura') return; b.kalemler.forEach(k => { const netTl = k.tutar * b.kur; kalem.push({ b, k, netTl, maliyetTl: k.maliyetTl, kar: netTl - k.maliyetTl, oran: k.maliyetTl > 0 ? (netTl / k.maliyetTl - 1) * 100 : null }); }); });
      return SATIS = { bugun: j.bugun, kalem };
    }).catch(x => { satisSoz = null; throw x; }));
  }
  // minimum kâr oranı: şirket bazında, bu tarayıcıda saklanır ('' = şirketine özel değer girilmemiş olanlar için genel değer)
  const MIN_VARSAYILAN = 20;
  let MIN = {}; try { MIN = JSON.parse(localStorage.getItem('fy-min-oran') || '{}') || {}; } catch (e) { MIN = {}; }
  const minOran = sirket => sayiMi(MIN[sirket]) ? Number(MIN[sirket]) : sayiMi(MIN['']) ? Number(MIN['']) : MIN_VARSAYILAN;
  const uyariSatirlari = (sirket, bas, pazar, ara) => SATIS.kalem.filter(x => x.oran !== null && (!sirket || x.b.sirket === sirket) && (!bas || x.b.tarih >= bas) && (!pazar || x.b.pazar === pazar) && x.oran < minOran(x.b.sirket) && (!ara || kck([x.b.no, x.b.cari, x.b.cariKod, x.b.temsilci, x.k.kod, x.k.ad, x.k.grup].join(' ')).includes(ara)));
  function donemBasi(v) { const b = SATIS.bugun, y = +b.slice(0, 4), a = +b.slice(5, 7) - 1, g = d => d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0'); return v === 'ay' ? g(new Date(y, a, 1)) : v === 'son3' ? g(new Date(y, a - 2, 1)) : v === 'yil' ? y + '-01-01' : v === 'son12' ? g(new Date(y, a - 11, 1)) : ''; }
  function uyariRozet() { if (!SATIS) return; const n = uyariSatirlari('', donemBasi('yil'), '', '').length, r = $('#fyNUyari'); r.textContent = n; r.style.display = n ? '' : 'none'; r.title = `Bu yıl kâr oranı minimumun altında kalan ${n} satış kalemi`; }
  function cizUyari() {
    if (!SATIS) { $('#fyUyNot').textContent = 'Satış verisi yükleniyor…'; satisYukle().then(() => { uyariSecim(); uyariRozet(); cizUyari(); }).catch(x => { $('#fyUyNot').textContent = ''; $('#fyUyTablo tbody').innerHTML = `<tr><td class="na-bos">Satış verisi alınamadı: ${esc(x.message)}</td></tr>`; }); return; }
    const sirket = $('#fyUySirket').value, pazar = $('#fyUyPazar').value, ara = kck($('#fyUyAra').value), rows = uyariSatirlari(sirket, donemBasi($('#fyUyDonem').value), pazar, ara);
    const mi = $('#fyUyMin'); if (document.activeElement !== mi) mi.value = sayiMi(MIN[sirket]) ? MIN[sirket] : ''; mi.placeholder = String(minOran(sirket)).replace('.', ',');
    const sirketler = [...new Set(SATIS.kalem.map(x => x.b.sirket))].sort((a, b) => a.localeCompare(b, TRL));
    $('#fyUyMinler').innerHTML = sirketler.map(s => `<span class="fy-pill ${s === sirket ? 'sec' : ''}" data-sirket="${esc(s)}" title="${esc(s)}: minimum kâr oranı % ${minOran(s)}${sayiMi(MIN[s]) ? '' : ' (genel değer)'} · tıklayın: bu şirketi seçin">${esc(s)} <b>% ${String(minOran(s)).replace('.', ',')}</b></span>`).join('');
    rows.forEach(x => { x.min = minOran(x.b.sirket); x.eksik = x.maliyetTl * (1 + x.min / 100) - x.netTl; x.seviye = x.oran < 0 ? 'Zararına' : x.oran < x.min / 2 ? 'Kritik' : 'Düşük'; });
    const sv = { 'Zararına': 0, 'Kritik': 1, 'Düşük': 2 }, pbS = { TL: '₺', USD: '$', EUR: '€' };
    const kol = [
      { k: 'seviye', ad: 'Uyarı', ac: 'Zararına: maliyetin altında · Kritik: minimumun yarısının altında · Düşük: minimumun altında', v: x => sv[x.seviye], h: x => `<span class="fy-uyari ${x.seviye === 'Zararına' ? 'zarar' : x.seviye === 'Kritik' ? 'kritik' : ''}">${x.seviye}</span>` },
      { k: 'tarih', ad: 'Tarih', s: 1, v: x => x.b.tarih, h: x => trh(x.b.tarih) },
      { k: 'no', ad: 'Belge No', v: x => x.b.no, h: x => `<b>${esc(x.b.no)}</b>` },
      { k: 'sirket', ad: 'Şirket', v: x => x.b.sirket },
      { k: 'musteri', ad: 'Müşteri', v: x => x.b.cari, h: x => `<b>${esc(x.b.cari)}</b><small>${esc(x.b.cariKod)}</small>` },
      { k: '', ik: 1, ad: 'Cari Ekstre', adHtml: '<i class="fas fa-file-invoice"></i>', v: x => x.b.cariKod, h: x => `<a class="fy-ek" href="/Rapor20?bpCode=${encodeURIComponent(x.b.cariKod)}" target="_blank" rel="noopener" title="Cari Ekstre'ye Git: ${esc(x.b.cari)}"><i class="fas fa-file-invoice"></i></a>` },
      { k: 'temsilci', ad: 'Satış Temsilcisi', v: x => x.b.temsilci },
      { k: 'urun', ad: 'Ürün', v: x => x.k.ad, h: x => `<b>${esc(x.k.ad)}</b><small>${esc(x.k.kod)}</small>` },
      { k: 'grup', ad: 'Faaliyet Grubu', v: x => x.k.grup },
      { k: 'miktar', ad: 'Miktar', s: 1, v: x => x.k.miktar, h: x => `${tam(x.k.miktar)} ${esc(x.k.birim)}` },
      { k: 'fiyat', ad: 'Birim Fiyat', s: 1, v: x => x.k.fiyat * x.b.kur, h: x => `${tl(x.k.fiyat)} ${pbS[x.b.pb] || x.b.pb}` },
      { k: 'net', ad: 'Tutar (₺)', s: 1, v: x => x.netTl, h: x => tl(x.netTl) + ' ₺' },
      { k: 'maliyet', ad: 'Maliyet (₺)', s: 1, v: x => x.maliyetTl, h: x => tl(x.maliyetTl) + ' ₺' },
      { k: 'kar', ad: 'Kâr (₺)', s: 1, v: x => x.kar, h: x => `<span class="${x.kar < 0 ? 'fy-fark negatif' : ''}">${tl(x.kar)} ₺</span>` },
      { k: 'oran', ad: 'Kâr Oranı', ac: 'Maliyet üzerinden: tutar ÷ maliyet − 1', s: 1, v: x => x.oran, h: x => `<b class="fy-fark negatif">${pct(x.oran)}</b>` },
      { k: 'min', ad: 'Minimum', s: 1, v: x => x.min, h: x => pct(x.min) },
      { k: 'eksik', ad: 'Eksik Kâr (₺)', ac: 'Minimum kâr oranını tutturmak için gereken tutar − gerçekleşen tutar', s: 1, v: x => x.eksik, h: x => `<b>${tl(x.eksik)} ₺</b>` }
    ];
    const tn = topl(rows.map(x => x.netTl)), tmal = topl(rows.map(x => x.maliyetTl)), te = topl(rows.map(x => x.eksik));
    listeCiz('#fyUyTablo', kol, rows, { sira: 'oran', yon: 1, yeniden: cizUyari, bos: 'Minimum kâr oranının altında satış yok.',
      dip: () => `<td colspan="11">Toplam · ${tam(rows.length)} Kalem</td><td class="s">${tl(tn)} ₺</td><td class="s">${tl(tmal)} ₺</td><td class="s">${tl(tn - tmal)} ₺</td><td class="s">${tmal ? pct((tn / tmal - 1) * 100) : '—'}</td><td></td><td class="s">${tl(te)} ₺</td>` });
    $('#fyUyNot').innerHTML = rows.length ? `<b class="text-danger">${tam(rows.length)} uyarı</b> · ${rows.filter(x => x.seviye === 'Zararına').length} zararına · ${rows.filter(x => x.seviye === 'Kritik').length} kritik · ${new Set(rows.map(x => x.b.cariKod)).size} müşteri · ${new Set(rows.map(x => x.k.kod)).size} ürün · eksik kâr ${tam(te)} ₺` : 'Uyarı yok';
  }
  function uyariSecim() { const s = $('#fyUySirket'), sec = s.value; s.innerHTML = '<option value="">Tüm Şirketler</option>' + [...new Set(SATIS.kalem.map(x => x.b.sirket))].sort((a, b) => a.localeCompare(b, TRL)).map(x => `<option>${esc(x)}</option>`).join(''); s.value = sec; }

  // ================= GENEL GİDER PAYI (13 Kas 2026) =================
  // Sabit ve genel gider kalemleri (Bütçe Raporu; işaretli olanlar) ÷ üretim rakamı (kg; ay ay girilir, girilmeyen ayda satış miktarı yedek) ÷ USD/TL = ürün başına eklenmesi
  // gereken $ / kg. Şirket = üst şeritte seçili üretici. Hesap yalnız gösterir; Toplam Maliyete kendiliğinden eklenmez. Üretim rakamlarını yalnız Yönetici kaydeder.
  let GG = null, ggIstek = 0;
  let GG_HARIC = []; try { GG_HARIC = JSON.parse(localStorage.getItem('fy-gider-haric') || '[]') || []; } catch (e) { GG_HARIC = []; }
  const ggHaricYaz = () => { try { localStorage.setItem('fy-gider-haric', JSON.stringify(GG_HARIC)); } catch (e) { /* yoksay */ } };
  async function giderYukle() {
    const no = ++ggIstek, yil = $('#fyGgYil').value || '';
    const j = await (await fetch(`/FiyatYonetimi/GenelGider?uretici=${encodeURIComponent(URETICI)}&yil=${encodeURIComponent(yil)}`)).json(); if (no !== ggIstek) return false;
    GG = j; GG.giris = j.uretim.map(x => x ?? '');
    $('#fyGgYil').innerHTML = j.yillar.map(y => `<option>${y}</option>`).join(''); $('#fyGgYil').value = j.yil;
    const aopt = j.aylar.map((a, i) => `<option value="${i + 1}">${esc(a)}</option>`).join(''), bas = $('#fyGgBas'), bit = $('#fyGgBit'), ilk = !bas.options.length;
    if (ilk) { bas.innerHTML = aopt; bit.innerHTML = aopt; bas.value = 1; }
    if (ilk || GG.sonYil !== j.yil) bit.value = j.kapanmisAy || 12; GG.sonYil = j.yil;
    return true;
  }
  function cizGider() {
    if (!GG || GG.uretici !== URETICI || String(GG.yil) !== String($('#fyGgYil').value || GG.yil)) { $('#fyGgNot').textContent = 'Yükleniyor…'; giderYukle().then(ok => { if (ok) cizGider(); }).catch(x => { $('#fyGgNot').textContent = 'Veri alınamadı: ' + x.message; }); return; }
    if (!SATIS && !cizGider.satisIstendi) { cizGider.satisIstendi = true; satisYukle().then(() => { if ($('#fySekmeler button.aktif')?.dataset.p === 'gider') cizGider(); }).catch(() => { }); }
    const bas = +$('#fyGgBas').value || 1, bit = Math.max(bas, +$('#fyGgBit').value || 12), olcu = $('#fyGgOlcu').value, aylar = []; for (let m = bas; m <= bit; m++) aylar.push(m - 1);
    const tutar = (k, i) => olcu === 'butce' || !k.gercek || k.gercek[i] == null ? (k.butce[i] || 0) : k.gercek[i];
    const secili = GG.kalemler.filter(k => !GG_HARIC.includes(k.kod));
    // satış miktarı (kg): üretim rakamı girilmeyen ayda yedek olarak kullanılır
    const satisKg = new Array(12).fill(0); if (SATIS) SATIS.kalem.forEach(x => { if (x.b.sirket === GG.sirket && x.b.tarih.slice(0, 4) === String(GG.yil) && x.k.birim === 'kg') satisKg[+x.b.tarih.slice(5, 7) - 1] += x.k.miktar; });
    const ay = aylar.map(i => { const g = GG.giris[i], gider = topl(secili.map(k => tutar(k, i))), elle = sayiMi(g); return { i, giris: g, eski: GG.uretim[i] ?? '', gider, satis: satisKg[i], kg: elle ? Number(g) : satisKg[i], elle }; });
    const tGider = topl(ay.map(a => a.gider)), tKg = topl(ay.map(a => a.kg)), tlKg = tKg ? tGider / tKg : 0, usdKg = GG.kur ? tlKg / GG.kur : 0, yedek = ay.filter(a => !a.elle).length;
    $('#fyGgOzet').innerHTML = `<i class="fas fa-calculator"></i><b>Ürün Başına Eklenmesi Gereken Genel Gider Payı</b><span class="fy-gg-sonuc">${tKg ? tl(usdKg) + ' $ / kg' : '—'}</span>`
      + `<span class="not">${tam(tGider)} ₺ gider ÷ ${tam(tKg)} kg üretim = ${tl(tlKg)} ₺ / kg ÷ USD/TL ${k4(GG.kur)}${tKg && GG.ortMaliyet ? ` · ortalama toplam maliyetin (${tl(GG.ortMaliyet)} $ / kg) ${pct(usdKg / GG.ortMaliyet * 100)}'i` : ''}${yedek ? ` · <b>${yedek} ayda üretim girilmedi: ${SATIS ? 'satış miktarı kullanıldı' : 'miktar yok'}</b>` : ''}</span>`;
    // gider kalemleri
    const rows = GG.kalemler.map(k => ({ k, dahil: !GG_HARIC.includes(k.kod), donem: topl(aylar.map(i => tutar(k, i))) }));
    const kol = [
      { k: '', ik: 1, ad: 'Hesaba Dahil', adHtml: '<i class="fas fa-check"></i>', v: r => r.dahil ? 1 : 0, h: r => `<input type="checkbox" class="form-check-input fy-gg-sec" data-kod="${esc(r.k.kod)}" ${r.dahil ? 'checked' : ''} title="Hesaba dahil et / çıkar">` },
      { k: 'kod', ad: 'Kod', v: r => r.k.kod, h: r => `<b>${esc(r.k.kod)}</b>` },
      { k: 'grup', ad: 'Grup', v: r => r.k.grup },
      { k: 'ad', ad: 'Gider Kalemi', v: r => r.k.ad, h: r => `<b class="${r.dahil ? '' : 'fy-gg-haric'}">${esc(r.k.ad)}</b><small>${esc(r.k.altGrup)}</small>` },
      { k: 'donem', ad: 'Dönem Tutarı (₺)', s: 1, v: r => r.donem, h: r => `<span class="${r.dahil ? '' : 'fy-gg-haric'}">${tam(r.donem)} ₺</span>` },
      { k: 'aylik', ad: 'Aylık Ortalama (₺)', s: 1, v: r => r.donem / aylar.length, h: r => tam(r.donem / aylar.length) + ' ₺' },
      { k: 'pay', ad: 'Pay', ac: 'Hesaba dahil giderler içindeki payı', s: 1, v: r => r.dahil && tGider ? r.donem / tGider * 100 : 0, h: r => r.dahil && tGider ? pct(r.donem / tGider * 100) : '—' },
      { k: 'usd', ad: '$ / kg', ac: 'Bu kalemin ürün başına düşen payı', s: 1, v: r => r.dahil && tKg && GG.kur ? r.donem / tKg / GG.kur : 0, h: r => r.dahil && tKg && GG.kur ? `<b>${k4(r.donem / tKg / GG.kur)}</b>` : '—' }
    ];
    listeCiz('#fyGgKalemTablo', kol, rows, { sira: 'donem', yon: -1, yeniden: cizGider, bos: 'Bu şirket ve yıl için gider kalemi yok.', limit: 200,
      dip: () => `<td colspan="4">Hesaba Dahil · ${secili.length} / ${GG.kalemler.length} Kalem</td><td class="s">${tam(tGider)} ₺</td><td class="s">${tam(tGider / aylar.length)} ₺</td><td class="s">${pct(100)}</td><td class="s">${tKg ? k4(usdKg) : '—'}</td>` });
    // aylık üretim rakamları
    $('#fyGgAyTablo tbody').innerHTML = ay.map(a => `<tr><td><b>${esc(GG.aylar[a.i])}</b></td><td class="s">${tam(a.gider)} ₺</td><td class="s"><input type="number" step="1" min="0" class="form-control fy-mal fy-gg-uretim ${String(a.giris) !== String(a.eski) ? 'degisti' : ''}" data-ay="${a.i}" value="${a.giris}" placeholder="${a.satis ? tam(a.satis).replace(/\./g, '') : '—'}" title="Aylık üretim (kg). Boş bırakılırsa satış miktarı kullanılır." ${YONETICI ? '' : 'disabled'}></td><td class="s">${SATIS ? tam(a.satis) : '…'}</td><td class="s"><b>${tam(a.kg)}</b><small>${a.elle ? 'Üretim' : 'Satış (Yedek)'}</small></td><td class="s">${a.kg ? tl(a.gider / a.kg) : '—'}</td><td class="s"><b>${a.kg && GG.kur ? tl(a.gider / a.kg / GG.kur) : '—'}</b></td></tr>`).join('');
    $('#fyGgAyTablo tfoot').innerHTML = `<tr><td>Dönem</td><td class="s">${tam(tGider)} ₺</td><td class="s">${tam(topl(ay.filter(a => a.elle).map(a => a.kg)))}</td><td class="s">${SATIS ? tam(topl(ay.map(a => a.satis))) : '…'}</td><td class="s">${tam(tKg)}</td><td class="s">${tKg ? tl(tlKg) : '—'}</td><td class="s">${tKg ? tl(usdKg) : '—'}</td></tr>`;
    const kirli = GG.giris.some((v, i) => String(v) !== String(GG.uretim[i] ?? ''));
    $('#fyGgKaydet').disabled = !YONETICI || !kirli;
    $('#fyGgNot').textContent = `${GG.uretici}${GG.sirket !== GG.uretici ? ' (' + GG.sirket + ')' : ''} · ${GG.yil} · ${GG.aylar[bas - 1]} – ${GG.aylar[bit - 1]} · ${olcu === 'butce' ? 'bütçe' : 'gerçekleşen (yoksa bütçe)'}${kirli ? ' · kaydedilmemiş üretim rakamı' : ''}${YONETICI ? '' : ' · salt okunur'}`;
    kapBoyut();
  }
  function giderBagla() {
    ['#fyGgBas', '#fyGgBit', '#fyGgOlcu'].forEach(q => $(q).addEventListener('change', cizGider));
    $('#fyGgYil').addEventListener('change', () => { GG = null; cizGider(); });
    $('#fyGgKalemTablo').addEventListener('change', e => { const c = e.target.closest('.fy-gg-sec'); if (!c) return; const kod = c.dataset.kod; GG_HARIC = GG_HARIC.filter(x => x !== kod); if (!c.checked) GG_HARIC.push(kod); ggHaricYaz(); cizGider(); });
    $('#fyGgAyTablo').addEventListener('change', e => { const i = e.target.closest('.fy-gg-uretim'); if (!i || !GG) return; GG.giris[+i.dataset.ay] = i.value; cizGider(); });
    $('#fyGgSecim').addEventListener('click', e => { const b = e.target.closest('[data-gg]'); if (!b || !GG) return; const g = b.dataset.gg; GG_HARIC = g === 'tumu' ? [] : GG.kalemler.filter(k => k.grup !== g).map(k => k.kod); ggHaricYaz(); cizGider(); });
    $('#fyGgSatisYaz').addEventListener('click', () => { if (!GG) return; if (!SATIS) { mesaj('warning', 'Satış verisi henüz yüklenmedi; birazdan yeniden deneyin.'); return; } const kg = new Array(12).fill(0); SATIS.kalem.forEach(x => { if (x.b.sirket === GG.sirket && x.b.tarih.slice(0, 4) === String(GG.yil) && x.k.birim === 'kg') kg[+x.b.tarih.slice(5, 7) - 1] += x.k.miktar; }); GG.giris = GG.giris.map((v, i) => sayiMi(v) || !kg[i] ? v : String(Math.round(kg[i]))); cizGider(); });
    $('#fyGgKaydet').addEventListener('click', async () => { if (!GG) return; const j = await post('/FiyatYonetimi/UretimKaydet', { uretici: URETICI, yil: GG.yil, aylar: GG.giris.map(v => sayiMi(v) ? Number(v) : null) }); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) { GG = null; cizGider(); } });
    $('#fyGgExcel').addEventListener('click', async () => { if (!window.TabloExcel || !GG) return; try { await TabloExcel.indir('genel_gider_payi_' + GG.sirket.replace(/\s+/g, '_') + '_' + GG.yil, [{ tablo: '#fyGgKalemTablo', ad: 'Gider Kalemleri', baslik: 'Genel Gider Payı · Gider Kalemleri', altBaslik: $('#fyGgNot').textContent + ' · ' + $('#fyGgOzet').textContent.replace(/\s+/g, ' ') }, { tablo: '#fyGgAyTablo', ad: 'Aylık Üretim', baslik: 'Genel Gider Payı · Aylık Gider ve Üretim', altBaslik: $('#fyGgNot').textContent }]); } catch (x) { mesaj('danger', 'Excel üretilemedi: ' + esc(x.message)); } });
  }

  const YENI = { ortalama: cizOrtalama, sorgu: cizSorgu, urunk: cizUrunK, uyari: cizUyari, gider: cizGider };
  /** veri her yenilendiğinde: yeni sekmelerin seçim kutuları doldurulur (seçimler korunur) */
  function yeniSecimler() {
    const koru = (id, html, vars) => { const e = $(id), v = e.value; e.innerHTML = html; e.value = v || vars || ''; if (e.selectedIndex < 0) e.selectedIndex = 0; };
    const sopt = V.saticilar.map(s => `<option value="${esc(s.ad)}">${esc(s.ad)}</option>`).join(''), gopt = V.gruplar.map(g => `<option value="${esc(g.anahtar)}">${esc(g.ad)}</option>`).join(''), sinif = [...new Set(V.musteriler.map(c => c.sinif).filter(Boolean))].map(x => `<option>${esc(x)}</option>`).join('');
    koru('#fySorGrup', '<option value="">Tümü</option>' + gopt); koru('#fyUkGrup', '<option value="">Tümü</option>' + gopt);
    koru('#fyOrtSinif', '<option value="">Tümü</option>' + sinif); koru('#fySorSinif', '<option value="">Tümü</option>' + sinif);
  }
  function yeniBagla() {
    const bagla = (idler, fn) => idler.forEach(s => { $(s).addEventListener('input', fn); $(s).addEventListener('change', fn); });
    bagla(['#fyOrtSatici', '#fyOrtAra', '#fyOrtSinif', '#fyOrtTanim', '#fyOrtPb'], cizOrtalama);
    bagla(['#fySorEsik', '#fySorYon', '#fySorOlcu', '#fySorSatici', '#fySorUrun', '#fySorGrup', '#fySorMusteri', '#fySorSinif', '#fySorTanimli', '#fySorSatisli'], cizSorgu);
    bagla(['#fyUkSatici', '#fyUkAra', '#fyUkGrup', '#fyUkFark'], cizUrunK);
    bagla(['#fyUySirket', '#fyUyDonem', '#fyUyPazar', '#fyUyAra'], cizUyari);
    $('#fyUyMin').addEventListener('input', e => { const s = $('#fyUySirket').value, v = e.target.value; if (sayiMi(v)) MIN[s] = Number(v); else delete MIN[s]; try { localStorage.setItem('fy-min-oran', JSON.stringify(MIN)); } catch (x) { } cizUyari(); uyariRozet(); });
    $('#fyUyMinler').addEventListener('click', e => { const p = e.target.closest('[data-sirket]'); if (!p) return; $('#fyUySirket').value = p.dataset.sirket; cizUyari(); });
    $('#fyOrtExcel').addEventListener('click', () => listeExcel('#fyOrtTablo', 'musteri_fiyat_ortalamalari', 'Müşteri Fiyat Ortalamaları', ($('#fyOrtSatici').value || 'Tüm Satıcılar') + ' · ' + $('#fyOrtNot').textContent));
    $('#fySorExcel').addEventListener('click', () => listeExcel('#fySorTablo', 'kar_orani_sorgusu', 'Kâr Oranı Sorgusu', $('#fySorNot').textContent));
    $('#fyUkExcel').addEventListener('click', () => listeExcel('#fyUkTablo', 'urun_fiyat_karsilastirmasi', 'Ürün Fiyat Karşılaştırması', $('#fyUkSatici').value + ' · ' + $('#fyUkNot').textContent));
    $('#fyUyExcel').addEventListener('click', () => listeExcel('#fyUyTablo', 'satis_uyarilari', 'Satış Uyarıları', $('#fyUyNot').textContent));
    // uyarı sayısı sekme düğmesinde görünsün diye satış verisi arka planda okunur (ekranın açılışını bekletmez)
    satisYukle().then(() => { uyariSecim(); uyariRozet(); }).catch(() => { });
  }

  // ---- zincir seçimi (üst şerit): Üretici → Yetkili Satıcı → Müşteri. Üretici ve satıcı seçimi bütün sekmeleri sürer; satıcı listesinde üreticinin kendisi yoktur.
  function zincirSecenekleri() {
    const u = $('#fyUretici'); u.innerHTML = V.ureticiler.map(x => `<option>${esc(x)}</option>`).join(''); u.value = URETICI;
    const sopt = zincirSaticilar().map(s => `<option value="${esc(s.ad)}">${esc(s.ad)}</option>`).join(''), ilk = (zincirSaticilar()[0] || {}).ad || '';
    ['#fySatici', '#fyMSatici', '#fyOSatici', '#fyUkSatici'].forEach(id => { const e = $(id), v = e.value; e.innerHTML = sopt; e.value = v; if (e.selectedIndex < 0) e.value = ilk; });
    ['#fyOrtSatici', '#fySorSatici'].forEach(id => { const e = $(id), v = e.value; e.innerHTML = '<option value="">Tüm Satıcılar</option>' + sopt; e.value = v; if (e.selectedIndex < 0) e.value = ''; });
    $('#fyGrpUretici').textContent = 'Maliyet · ' + URETICI;
  }
  /** seçili satıcı bütün sekmelere yayılır (Müşteri Fiyatları, Özet, Ürün Fiyat Karşılaştırması aynı satıcıyla çalışır) */
  function saticiYay() { const v = $('#fySatici').value; ['#fyMSatici', '#fyOSatici', '#fyUkSatici'].forEach(id => { $(id).value = v; }); }
  function zincirDegisti(ureticiDegisti) {
    if (ureticiDegisti) { URETICI = $('#fyUretici').value; zincirSecenekleri(); try { const u = new URL(location.href); if (URETICI === V.uretici) u.searchParams.delete('uretici'); else u.searchParams.set('uretici', URETICI); history.replaceState(null, '', u); } catch (e) { /* yoksay */ } }
    saticiYay(); $('#fyUrunTablo tbody').innerHTML = ''; $('#fyMUrunTablo tbody').innerHTML = ''; saticiYukle(); musteriListesi(); musteriYukle();
    const ap = $('#fySekmeler button.aktif')?.dataset.p; if (ap === 'ozet') cizOzet(); if (YENI[ap]) YENI[ap](); kapBoyut();
  }
  function malGenelYukle() { const tp = $('#fyAmbalajTipi'); tp.innerHTML = (V.maliyet.ambalajTipleri || []).map(a => `<option value="${esc(a.kod)}">${esc(a.ad)}</option>`).join(''); tp.value = V.maliyet.ambalajTipi; tp.dataset.eski = tp.value; tp.disabled = !YONETICI; tp.classList.remove('degisti'); [['#fyGenelAmbalaj', V.maliyet.genelAmbalajElle], ['#fyGenelRisk', V.maliyet.genelRisk]].forEach(([id, v]) => { const i = $(id); i.value = v != null && v !== 0 || (id === '#fyGenelAmbalaj' && v === 0) ? v : ''; i.dataset.eski = i.value; i.disabled = !YONETICI; i.classList.remove('degisti'); }); ambalajBilgi(); }

  // ---- veri
  /// koru: oran kaydından sonra veri yenilenirken kaydedilmemiş kur taslağı ekranda tutulur (kur kaydında false → sunucu değerleri)
  async function yenile(koru = true) {
    const sSec = $('#fySatici').value, mSec = $('#fyMSatici').value, oSec = $('#fyOSatici').value;
    const kurTaslak = koru && V ? kurKirli().map(i => [i.id, i.value]) : [];
    V = await (await fetch('/FiyatYonetimi/Veri')).json(); YONETICI = !!(V.ben && V.ben.yonetici);
    if (!URETICI || !V.ureticiler.includes(URETICI)) URETICI = V.uretici;
    V.urunler.forEach(u => { u.uretim = u.maliyet; }); malGenelYukle(); maliyetUygula();   // u.maliyet artık geçerli toplam maliyet
    zincirSecenekleri(); if (sSec) $('#fySatici').value = sSec; if ($('#fySatici').selectedIndex < 0) $('#fySatici').selectedIndex = 0; saticiYay();
    $('#fyGrupF').innerHTML = '<option value="">Tümü</option>' + V.gruplar.map(g => `<option value="${esc(g.anahtar)}">${esc(g.ad)}</option>`).join(''); $('#fyMGrupF').innerHTML = '<option value="">Tüm Gruplar</option>' + V.gruplar.map(g => `<option value="${esc(g.anahtar)}">${esc(g.ad)}</option>`).join('');
    $('#fyMSinif').innerHTML = '<option value="">Tümü</option>' + [...new Set(V.musteriler.map(c => c.sinif).filter(Boolean))].map(x => `<option>${esc(x)}</option>`).join('');
    $('#fyUrunTablo tbody').innerHTML = ''; $('#fyMUrunTablo tbody').innerHTML = '';
    kurYukle();
    if (kurTaslak.length) { kurTaslak.forEach(([id, v]) => { const i = document.getElementById(id); i.value = v; kurIsaretle(i); }); kurHesapla(); }   // taslak geri yüklenir, tablo bu kurla çizilir
    saticiYukle(); musteriListesi(); musteriYukle(); if ($('#fySekmeler button.aktif')?.dataset.p === 'ozet') cizOzet();
    yeniSecimler(); { const ap = $('#fySekmeler button.aktif')?.dataset.p; if (YENI[ap]) YENI[ap](); }
    kapBoyut();
  }
  (async () => {
    try { await yenile(); } catch (e) { mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)); return; }
    $('#fySatici').addEventListener('change', () => zincirDegisti(false));
    $('#fyUretici').addEventListener('change', () => zincirDegisti(true));
    ['#fyGenelAmbalaj', '#fyGenelRisk'].forEach(q => $(q).addEventListener('input', e => { degistiIsaretle(e.target); ambalajBilgi(); cizUrunler(); }));
    $('#fyAmbalajTipi').addEventListener('change', e => { degistiIsaretle(e.target); ambalajBilgi(); cizUrunler(); });
    $('#fyMGenelRisk').addEventListener('input', e => { degistiIsaretle(e.target); cizMUrunler(); });
    giderBagla();
    window.addEventListener('resize', kapBoyut);
    // üstteki kartların yüksekliği sonradan değişirse (yazı tipi, uyarı satırı, Cari Bilgileri) tablo kabı yeniden ekranın altına oturtulur
    if (window.ResizeObserver) { let rz; new ResizeObserver(() => { cancelAnimationFrame(rz); rz = requestAnimationFrame(kapBoyut); }).observe(kok); }
    setTimeout(kapBoyut, 400);
    $('#fyCariAc').addEventListener('click', () => { const c = $('#fySaticiCari'); c.hidden = !c.hidden; kapBoyut(); });
    $('#fyMCariAc').addEventListener('click', () => { const c = $('#fyMCari'); c.hidden = !c.hidden; kapBoyut(); });
    ['#fyAra', '#fyGrupF', '#fyKaynakF'].forEach(s => { $(s).addEventListener('input', cizUrunler); $(s).addEventListener('change', cizUrunler); });
    $('#fyKaydet').addEventListener('click', saticiKaydet); $('#fyVazgec').addEventListener('click', () => { $('#fyUrunTablo tbody').innerHTML = ''; saticiYukle(); }); $('#fyExcel').addEventListener('click', saticiExcel);
    $('#fyMSatici').addEventListener('change', () => { musteriListesi(); $('#fyMUrunTablo tbody').innerHTML = ''; musteriYukle(); });
    ['#fyMAra', '#fyMSinif', '#fyMTanim'].forEach(s => { $(s).addEventListener('input', musteriListesi); $(s).addEventListener('change', musteriListesi); });
    ['#fyMUrunAra', '#fyMGrupF'].forEach(s => { $(s).addEventListener('input', cizMUrunler); $(s).addEventListener('change', cizMUrunler); });
    $('#fyMKaydet').addEventListener('click', musteriKaydet); $('#fyMVazgec').addEventListener('click', () => { $('#fyMUrunTablo tbody').innerHTML = ''; musteriYukle(); }); $('#fyMExcel').addEventListener('click', musteriExcel);
    $('#fyMKartAc').addEventListener('click', () => { if (SECILI) ac('/CariKart/Musteri?kod=' + encodeURIComponent(SECILI), 'Müşteri Cari Kartları'); });
    // Kur kutusu değişince ₺ sütunları iki sekmede de yeniden hesaplanır
    $$('.fy-kur-giris').forEach(i => i.addEventListener('input', () => { kurIsaretle(i); kurHesapla(); cizUrunler(); if (SECILI) cizMUrunler(); }));
    $('#fyKurKaydet').addEventListener('click', kurKaydet); $('#fyKurVazgec').addEventListener('click', kurVazgec);
    $('#fyOSatici').addEventListener('change', cizOzet);
    $$('#fySekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    yeniBagla();
    try { const p = new URLSearchParams(location.search); const sat = p.get('satici'), kod = p.get('kod'); const ur = p.get('uretici'); if (ur && V.ureticiler.includes(ur) && ur !== URETICI) { $('#fyUretici').value = ur; zincirDegisti(true); } if (sat && zincirSaticilar().some(s => s.ad === sat)) { $('#fySatici').value = sat; zincirDegisti(false); } if (kod) { SECILI = kod; musteriListesi(); musteriYukle(); sekme('musteri'); return; } } catch (e) { /* yoksay */ }
    sekme(kok.dataset.baslangic || 'satici'); window.NA_HAZIR = true;
  })();
})();
