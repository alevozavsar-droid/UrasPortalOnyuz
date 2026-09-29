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
  let V = null, YONETICI = false;
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
  const saticiOranlari = ad => (V.saticilar.find(s => s.ad === ad) || { oranlar: [] }).oranlar;
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
    try { const u = new URL(location.href); if (p === 'satici') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }

  // ================= SEKME 1: YETKİLİ SATICI =================
  function saticiYukle() {
    const s = V.saticilar.find(x => x.ad === $('#fySatici').value); if (!s) return;
    const sk = $('#fySaticiKutu'); sk.innerHTML = `<i class="fas fa-store me-1"></i>${esc(s.ad)}<small>${esc(s.sirket)} · ${esc(s.cariKodu)} · ${esc(s.bolge)}</small>`;
    sk.dataset.url = '/CariKart/Musteri?kod=' + encodeURIComponent(s.cariKodu); sk.dataset.baslik = 'Müşteri Cari Kartları'; sk.title = `${s.ad} cari kartını aç (${s.cariKodu})`;   // akış kutusu tıklanınca cari kart (rapor-tema.js)
    $('#fyGrpAlis').textContent = `Satıcı Alış Fiyatı · ${V.uretici} → ${s.ad}`;
    $('#fySaticiNot').textContent = `${s.not || ''} · ${s.oranlar.length} oran tanımı · ${s.musteriSayisi} müşteri tanımı`;
    $('#fySaticiCari').innerHTML = cariHtml(s.cari);
    const genel = s.oranlar.find(o => o.kapsam === 'Genel');
    $('#fyGenel').value = genel ? genel.oran : ''; $('#fyGenel').dataset.eski = genel ? genel.oran : ''; $('#fyGenel').disabled = !YONETICI; $('#fyGenel').classList.remove('degisti');
    $('#fyGruplar').innerHTML = V.gruplar.map(g => { const o = s.oranlar.find(x => x.kapsam === 'Grup' && x.anahtar === g.anahtar); return `<div><b>${esc(g.ad)}<small>${V.urunler.filter(u => u.grupAnahtari === g.anahtar).length} ürün · grup oranı</small></b><span>%</span>${oranGiris('', o ? o.oran : '', g.anahtar, 'Grup')}</div>`; }).join('');
    $$('#fyGruplar input, #fyGenel').forEach(i => i.addEventListener('input', () => { degistiIsaretle(i); cizUrunler(); }));
    cizUrunler();
  }
  function cizUrunler() {
    const s = V.saticilar.find(x => x.ad === $('#fySatici').value); if (!s) return;
    // kaydedilmemiş ekran değerleriyle hesapla (kalem girişleri tabloda; ilk çizimde sunucu değerleri)
    const kalemler = {}; $$('#fyUrunTablo input.fy-oran').forEach(i => kalemler[i.dataset.k] = i.value);
    const oranlar = [];
    const g = $('#fyGenel').value; if (sayiMi(g)) oranlar.push({ kapsam: 'Genel', anahtar: '', oran: Number(g) });
    $$('#fyGruplar input').forEach(i => { if (sayiMi(i.value)) oranlar.push({ kapsam: 'Grup', anahtar: i.dataset.k, oran: Number(i.value) }); });
    V.urunler.forEach(u => { const v = u.kod in kalemler ? kalemler[u.kod] : (s.oranlar.find(x => x.kapsam === 'Urun' && x.anahtar === u.kod) || {}).oran; if (sayiMi(v)) oranlar.push({ kapsam: 'Urun', anahtar: u.kod, oran: Number(v) }); });
    const ara = ($('#fyAra').value || '').toLocaleLowerCase('tr-TR'), gf = $('#fyGrupF').value, kf = $('#fyKaynakF').value;
    const l = V.urunler.map(u => ({ u, c: oranCoz(oranlar, u) })).map(x => ({ ...x, fiyat: Math.round(x.u.maliyet * (1 + x.c.oran / 100) * 100) / 100 })).filter(x => (!gf || x.u.grupAnahtari === gf) && (!kf || x.c.kaynak === kf) && (!ara || [x.u.kod, x.u.ad, x.u.seri, x.u.urunGrubu].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    $('#fyUrunTablo tbody').innerHTML = l.map(x => { const u = x.u, kalem = u.kod in kalemler ? kalemler[u.kod] : ((s.oranlar.find(o => o.kapsam === 'Urun' && o.anahtar === u.kod) || {}).oran ?? ''); const eski = (s.oranlar.find(o => o.kapsam === 'Urun' && o.anahtar === u.kod) || {}).oran ?? ''; return `<tr data-kod="${esc(u.kod)}"><td><b>${esc(u.kod)}</b></td><td><b>${esc(u.ad)}</b><small>${esc(u.hiyerarsi)}</small></td><td>${esc(u.anaGrup)}<small>${esc(u.grup)}</small></td><td>${esc(u.birim)}</td><td class="s">${tl(u.maliyet)}</td><td class="s"><input type="number" step="0.01" class="form-control fy-oran ${String(kalem) !== String(eski) ? 'degisti' : ''}" data-k="${esc(u.kod)}" data-kapsam="Urun" data-eski="${eski}" value="${kalem}" placeholder="—" ${YONETICI ? '' : 'disabled'}></td><td class="s"><b>${pct(x.c.oran)}</b></td><td>${kaynakRozet(x.c.kaynak)}</td><td class="s"><b>${tl(x.fiyat)}</b></td><td class="s">${tl(tlKarsiligi(x.fiyat))}</td></tr>`; }).join('') || '<tr><td colspan="10" class="na-bos">Filtreye uyan ürün yok.</td></tr>';
    $$('#fyUrunTablo input.fy-oran').forEach(i => i.addEventListener('input', () => { degistiIsaretle(i); cizUrunler.ertele(); }));
    const tm = l.reduce((a, x) => a + x.u.maliyet, 0), tf = l.reduce((a, x) => a + x.fiyat, 0);
    $('#fyTopMaliyet').textContent = tl(tm) + ' $'; $('#fyTopFiyat').textContent = tl(tf) + ' $'; $('#fyTopFiyatTl').textContent = tl(tlKarsiligi(tf)) + ' ₺'; $('#fyOrtOran').textContent = tm ? pct((tf / tm - 1) * 100) + ' ort.' : '—';
    $('#fyKurBilgi').textContent = k4(kurDegerleri().fiyatUsdTl) + (kurKirli().length ? ' (kaydedilmemiş)' : '');
    const tanimsiz = l.filter(x => x.c.kaynak === 'Tanımsız').length; const n = degisenSayisi('[data-p=satici]');
    $('#fyDegisiklik').innerHTML = `${l.length} ürün · ${tanimsiz ? `<span class="text-danger">${tanimsiz} ürün tanımsız (kârsız fiyatlanır)</span> · ` : ''}${n ? `<b class="text-warning">${n} kaydedilmemiş değişiklik</b>` : 'Kaydedilmemiş değişiklik yok'}${YONETICI ? '' : ' · salt okunur'}`;
    $('#fyKaydet').disabled = !YONETICI || n === 0;
  }
  cizUrunler.ertele = () => { clearTimeout(cizUrunler.z); cizUrunler.z = setTimeout(() => { const a = document.activeElement; const kod = a && a.dataset ? a.dataset.k : null; cizUrunler(); if (kod) { const i = $(`#fyUrunTablo input[data-k="${kod}"]`); if (i) { i.focus(); } } }, 600); };
  async function saticiKaydet() {
    const g = { satici: $('#fySatici').value, genel: sayiMi($('#fyGenel').value) ? Number($('#fyGenel').value) : null, gruplar: $$('#fyGruplar input').map(i => ({ anahtar: i.dataset.k, oran: sayiMi(i.value) ? Number(i.value) : null })), urunler: $$('#fyUrunTablo input.fy-oran').filter(i => (i.value || '') !== (i.dataset.eski || '')).map(i => ({ anahtar: i.dataset.k, oran: sayiMi(i.value) ? Number(i.value) : null })) };
    const j = await post('/FiyatYonetimi/SaticiKaydet', g); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) await yenile();
  }
  function saticiExcel() {
    const s = V.saticilar.find(x => x.ad === $('#fySatici').value), rows = $$('#fyUrunTablo tbody tr[data-kod]').map(tr => { const td = $$('td', tr); return [td[0].textContent.trim(), $('b', td[1]).textContent, $('small', td[1]).textContent, td[2].textContent.trim(), td[3].textContent.trim(), td[4].textContent.trim(), $('input', td[5]).value, td[6].textContent.trim(), td[7].textContent.trim(), td[8].textContent.trim(), td[9].textContent.trim()]; });
    const k = kurDegerleri(); rows.push([]); rows.push([(kurKirli().length ? 'Fiyatlandırma kuru (KAYDEDİLMEMİŞ ekran değeri)' : 'Fiyatlandırma kuru'), `USD/TL ${k4(k.fiyatUsdTl)} (piyasa ${k4(k.usd)} + %${pct(k.marj).replace('%', '')})`]);
    csvIndir('yetkili_satici_fiyat_listesi_' + (s ? s.sirket : ''), ['Kod', 'Ürün', 'Hiyerarşi', 'Ürün Grubu', 'Birim', 'Üretim Maliyeti ($)', 'Kalem Oranı %', 'Uygulanan Oran', 'Kaynak', 'Satıcı Alış Fiyatı ($)', 'Satıcı Alış Fiyatı (₺)'], rows);
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
    $('#fyMSaticiKutu').innerHTML = `<i class="fas fa-store me-1"></i>${esc(satici)}<small>Alış fiyatı ($ / ₺, 1. katman)</small>`;
    $('#fyMGrpAlis').textContent = `Satıcı Alış Fiyatı · ${V.uretici} → ${satici}`; if (!SECILI) $('#fyMGrpSatis').textContent = `Müşteriye Satış Fiyatı · ${satici} → Müşteri`;
  }
  function musteriYukle() {
    const satici = $('#fyMSatici').value, c = V.musteriler.find(x => x.kod === SECILI);
    const mk = $('#fyMMusteriKutu');
    if (!c) { $('#fyMCari').innerHTML = '<div class="na-bos" style="grid-column:1/-1">Soldaki listeden müşteri seçin.</div>'; mk.innerHTML = '<i class="fas fa-user me-1"></i>Müşteri<small>Seçilmedi · soldaki listeden seçin</small>'; delete mk.dataset.url; mk.dataset.git = 'musteri|#fyMAra'; mk.title = 'Müşteri seçmek için listeye git'; $('#fyMGruplar').innerHTML = ''; $('#fyMUrunTablo tbody').innerHTML = '<tr><td colspan="12" class="na-bos">Müşteri seçilmedi.</td></tr>'; $('#fyMGenel').value = ''; $('#fyMGenel').disabled = true; $('#fyMKaydet').disabled = true; $('#fyMDegisiklik').textContent = ''; return; }
    const t = musteriTanimi(satici, c.kod) || { oranlar: [] };
    mk.innerHTML = `<i class="fas fa-user me-1"></i>${esc(c.kod)} · ${esc(c.kisaAd)}<small>${esc(c.sinif)} · ${esc(c.iliskiDurumu)}${t.zaman ? ' · son kayıt ' + esc(t.zaman) + ' ' + esc(t.kullanici) : ' · tanım yok'}</small>`;
    delete mk.dataset.git; mk.dataset.url = '/CariKart/Musteri?kod=' + encodeURIComponent(c.kod); mk.dataset.baslik = 'Müşteri Cari Kartları'; mk.title = `${c.kisaAd} cari kartını aç (${c.kod})`;
    $('#fyMGrpAlis').textContent = `Satıcı Alış Fiyatı · ${V.uretici} → ${satici}`; $('#fyMGrpSatis').textContent = `Müşteriye Satış Fiyatı · ${satici} → ${c.kisaAd}`;
    $('#fyMCari').innerHTML = cariHtml(c);
    const genel = t.oranlar.find(o => o.kapsam === 'Genel');
    $('#fyMGenel').value = genel ? genel.oran : ''; $('#fyMGenel').dataset.eski = genel ? genel.oran : ''; $('#fyMGenel').disabled = !YONETICI; $('#fyMGenel').classList.remove('degisti');
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
    const l = V.urunler.map(u => { const alis = saticiFiyati(sOran, u), mc = oranCoz(oranlar, u), satis = Math.round(alis * (1 + mc.oran / 100) * 100) / 100; return { u, alis, mc, satis }; }).filter(x => (!gf || x.u.grupAnahtari === gf) && (!ara || [x.u.kod, x.u.ad, x.u.seri].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    // EUR / TL karşılığı: fiyatlandırma kuru (piyasa + döviz kâr marjı) — sunucudaki FyKur.TlKarsiligi / EurKarsiligi ile aynı formül
    $('#fyMUrunTablo tbody').innerHTML = l.map(x => { const u = x.u, eski = (t.oranlar.find(o => o.kapsam === 'Urun' && o.anahtar === u.kod) || {}).oran ?? '', kalem = u.kod in kalemler ? kalemler[u.kod] : eski; return `<tr data-kod="${esc(u.kod)}"><td><b>${esc(u.kod)}</b></td><td><b>${esc(u.ad)}</b><small>${esc(u.hiyerarsi)}</small></td><td>${esc(u.anaGrup)}<small>${esc(u.grup)}</small></td><td class="s">${tl(x.alis)}<small>${pct(oranCoz(sOran, u).oran)} ${esc(oranCoz(sOran, u).kaynak)}</small></td><td class="s">${tl(tlKarsiligi(x.alis))}</td><td class="s"><input type="number" step="0.01" class="form-control fy-oran ${String(kalem) !== String(eski) ? 'degisti' : ''}" data-k="${esc(u.kod)}" data-kapsam="Urun" data-eski="${eski}" value="${kalem}" placeholder="—" ${YONETICI ? '' : 'disabled'}></td><td class="s"><b>${pct(x.mc.oran)}</b></td><td>${kaynakRozet(x.mc.kaynak)}</td><td class="s"><b>${tl(x.satis)}</b></td><td class="s">${tl(eurKarsiligi(x.satis))}</td><td class="s">${tl(tlKarsiligi(x.satis))}</td><td class="s">${pct((x.satis / u.maliyet - 1) * 100)}</td></tr>`; }).join('') || '<tr><td colspan="12" class="na-bos">Filtreye uyan ürün yok.</td></tr>';
    $$('#fyMUrunTablo input.fy-oran').forEach(i => i.addEventListener('input', () => { degistiIsaretle(i); cizMUrunler.ertele(); }));
    const ta = l.reduce((a, x) => a + x.alis, 0), ts = l.reduce((a, x) => a + x.satis, 0), tm = l.reduce((a, x) => a + x.u.maliyet, 0);
    $('#fyMTopAlis').textContent = tl(ta) + ' $'; $('#fyMTopAlisTl').textContent = tl(tlKarsiligi(ta)) + ' ₺'; $('#fyMTopSatis').textContent = tl(ts) + ' $'; $('#fyMTopEur').textContent = tl(eurKarsiligi(ts)) + ' €'; $('#fyMTopTl').textContent = tl(tlKarsiligi(ts)) + ' ₺'; $('#fyMOrtOran').textContent = ta ? pct((ts / ta - 1) * 100) + ' ort.' : '—'; $('#fyMOrtMarj').textContent = tm ? pct((ts / tm - 1) * 100) : '—';
    const tanimsiz = l.filter(x => x.mc.kaynak === 'Tanımsız').length, n = degisenSayisi('[data-p=musteri]');
    $('#fyMDegisiklik').innerHTML = `${l.length} ürün · ${tanimsiz ? `<span class="text-danger">${tanimsiz} ürün tanımsız (satıcı alış fiyatına, kârsız)</span> · ` : ''}${n ? `<b class="text-warning">${n} kaydedilmemiş değişiklik</b>` : 'Kaydedilmemiş değişiklik yok'}${YONETICI ? '' : ' · salt okunur'}`;
    $('#fyMKaydet').disabled = !YONETICI || n === 0;
  }
  cizMUrunler.ertele = () => { clearTimeout(cizMUrunler.z); cizMUrunler.z = setTimeout(() => { const a = document.activeElement; const kod = a && a.dataset ? a.dataset.k : null; cizMUrunler(); if (kod) { const i = $(`#fyMUrunTablo input[data-k="${kod}"]`); if (i) i.focus(); } }, 600); };
  async function musteriKaydet() {
    if (!SECILI) return;
    const g = { satici: $('#fyMSatici').value, cariKodu: SECILI, genel: sayiMi($('#fyMGenel').value) ? Number($('#fyMGenel').value) : null, gruplar: $$('#fyMGruplar input').map(i => ({ anahtar: i.dataset.k, oran: sayiMi(i.value) ? Number(i.value) : null })), urunler: $$('#fyMUrunTablo input.fy-oran').filter(i => (i.value || '') !== (i.dataset.eski || '')).map(i => ({ anahtar: i.dataset.k, oran: sayiMi(i.value) ? Number(i.value) : null })) };
    const j = await post('/FiyatYonetimi/MusteriKaydet', g); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) await yenile();
  }
  function musteriExcel() {
    const c = V.musteriler.find(x => x.kod === SECILI); if (!c) { mesaj('warning', 'Önce müşteri seçin.'); return; }
    const rows = $$('#fyMUrunTablo tbody tr[data-kod]').map(tr => { const td = $$('td', tr); return [c.kod, c.kisaAd, $('#fyMSatici').value, td[0].textContent.trim(), $('b', td[1]).textContent, td[2].textContent.trim(), td[3].childNodes[0].textContent.trim(), td[4].textContent.trim(), $('input', td[5]).value, td[6].textContent.trim(), td[7].textContent.trim(), td[8].textContent.trim(), td[9].textContent.trim(), td[10].textContent.trim(), td[11].textContent.trim()]; });
    // Dipnot, satırlarla aynı kurla (ekrandaki, kaydedilmemişse geçici) yazılır; tr-TR biçim
    const k = kurDegerleri(), kirli = kurKirli().length > 0; rows.push([]); rows.push([kirli ? 'Fiyatlandırma kuru (KAYDEDİLMEMİŞ ekran değeri)' : 'Fiyatlandırma kuru', `USD/TL ${k4(k.fiyatUsdTl)} (piyasa ${k4(k.usd)} + %${pct(k.marj).replace('%', '')})`, `EUR paritesi ${k4(k.fiyatParite)} (piyasa ${k4(k.parite)})`, kirli ? '' : V.kur.tarih, V.kur.kaynak]);
    csvIndir('musteri_fiyat_listesi_' + c.kod, ['Cari Kodu', 'Müşteri', 'Yetkili Satıcı', 'Kod', 'Ürün', 'Ürün Grubu', 'Satıcı Alış Fiyatı ($)', 'Satıcı Alış Fiyatı (₺)', 'Kalem Oranı %', 'Uygulanan Oran', 'Kaynak', 'Müşteriye Satış Fiyatı ($)', 'Müşteriye Satış Fiyatı (€)', 'Müşteriye Satış Fiyatı (₺)', 'Uçtan Uca Marj'], rows);
  }
  function ac(url, baslik) { if (window.top && window.top !== window && typeof window.top.portalSekmeAc === 'function') window.top.portalSekmeAc(baslik, url); else window.open(url + (sekmeli ? (url.includes('?') ? '&' : '?') + '_sekme=1' : ''), '_blank'); }

  // ================= SEKME 3: ÖZET VE GÜNLÜK =================
  function cizOzet() {
    const bas = `<tr><th>Yetkili Satıcı</th><th class="s">Genel</th>${V.gruplar.map(g => `<th class="s">${esc(g.ad)}</th>`).join('')}<th class="s">Kalem Tanımı</th><th class="s">Ort. Oran</th><th class="s">Müşteri</th></tr>`;
    $('#fySaticiMatris thead').innerHTML = bas;
    $('#fySaticiMatris tbody').innerHTML = V.saticilar.map(s => { const genel = s.oranlar.find(o => o.kapsam === 'Genel'); const tm = V.urunler.reduce((a, u) => a + u.maliyet, 0), tf = V.urunler.reduce((a, u) => a + saticiFiyati(s.oranlar, u), 0); return `<tr><td><b>${esc(s.ad)}</b><br><small class="text-muted">${esc(s.cariKodu)} · ${esc(s.bolge)}</small></td><td class="s">${genel ? pct(genel.oran) : '—'}</td>${V.gruplar.map(g => { const o = s.oranlar.find(x => x.kapsam === 'Grup' && x.anahtar === g.anahtar); return `<td class="s">${o ? `<b>${pct(o.oran)}</b>` : genel ? `<span class="text-muted">${pct(genel.oran)}</span>` : '<span class="text-danger">—</span>'}</td>`; }).join('')}<td class="s">${s.oranlar.filter(o => o.kapsam === 'Urun').length}</td><td class="s">${pct((tf / tm - 1) * 100)}</td><td class="s">${s.musteriSayisi}</td></tr>`; }).join('');
    const satici = $('#fyOSatici').value, sOran = saticiOranlari(satici);
    $('#fyMusteriMatris thead').innerHTML = `<tr><th>Müşteri</th><th>Cari Sınıfı</th><th class="s">Genel</th>${V.gruplar.map(g => `<th class="s">${esc(g.ad)}</th>`).join('')}<th class="s">Kalem</th><th class="s">Ort. Uçtan Uca Marj</th><th>Son Kayıt</th></tr>`;
    const ml = V.musteriTanimlari.filter(m => m.satici === satici);
    // (boş satır colspan: 6 sabit sütun + grup sütunları)
    $('#fyMusteriMatris tbody').innerHTML = ml.map(m => { const c = V.musteriler.find(x => x.kod === m.cariKodu) || {}; const genel = m.oranlar.find(o => o.kapsam === 'Genel'); const tm = V.urunler.reduce((a, u) => a + u.maliyet, 0), ts = V.urunler.reduce((a, u) => a + saticiFiyati(sOran, u) * (1 + oranCoz(m.oranlar, u).oran / 100), 0); return `<tr><td><b>${esc(m.cariKodu)}</b> · ${esc(c.kisaAd || '')}</td><td>${sinifRozet(c.sinif)} ${iliskiRozet(c.iliskiDurumu)}</td><td class="s">${genel ? pct(genel.oran) : '—'}</td>${V.gruplar.map(g => { const o = m.oranlar.find(x => x.kapsam === 'Grup' && x.anahtar === g.anahtar); return `<td class="s">${o ? `<b>${pct(o.oran)}</b>` : genel ? `<span class="text-muted">${pct(genel.oran)}</span>` : '<span class="text-danger">—</span>'}</td>`; }).join('')}<td class="s">${m.oranlar.filter(o => o.kapsam === 'Urun').length}</td><td class="s">${pct((ts / tm - 1) * 100)}</td><td><small>${esc(m.zaman)} · ${esc(m.kullanici)}</small></td></tr>`; }).join('') || `<tr><td colspan="${6 + V.gruplar.length}" class="na-bos">${esc(satici)} için müşteri tanımı yok.</td></tr>`;
    $('#fyAudit tbody').innerHTML = V.audit.map(a => `<tr><td>${esc(a.zaman)}</td><td>${esc(a.kullanici)}</td><td>${esc(a.katman)}</td><td>${esc(a.hedef)}</td><td>${esc(a.alan)}</td><td class="s">${esc(a.eski || '—')}</td><td class="s"><b>${esc(a.yeni || '(silindi)')}</b></td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Kayıt yok.</td></tr>';
  }

  // ---- veri
  /// koru: oran kaydından sonra veri yenilenirken kaydedilmemiş kur taslağı ekranda tutulur (kur kaydında false → sunucu değerleri)
  async function yenile(koru = true) {
    const sSec = $('#fySatici').value, mSec = $('#fyMSatici').value, oSec = $('#fyOSatici').value;
    const kurTaslak = koru && V ? kurKirli().map(i => [i.id, i.value]) : [];
    V = await (await fetch('/FiyatYonetimi/Veri')).json(); YONETICI = !!(V.ben && V.ben.yonetici);
    const sopt = V.saticilar.map(s => `<option value="${esc(s.ad)}">${esc(s.ad)}</option>`).join('');
    ['#fySatici', '#fyMSatici', '#fyOSatici'].forEach((id, i) => { $(id).innerHTML = sopt; $(id).value = [sSec, mSec, oSec][i] || V.saticilar[0].ad; });
    $('#fyGrupF').innerHTML = '<option value="">Tümü</option>' + V.gruplar.map(g => `<option value="${esc(g.anahtar)}">${esc(g.ad)}</option>`).join(''); $('#fyMGrupF').innerHTML = '<option value="">Tüm Gruplar</option>' + V.gruplar.map(g => `<option value="${esc(g.anahtar)}">${esc(g.ad)}</option>`).join('');
    $('#fyMSinif').innerHTML = '<option value="">Tümü</option>' + [...new Set(V.musteriler.map(c => c.sinif).filter(Boolean))].map(x => `<option>${esc(x)}</option>`).join('');
    $('#fyUrunTablo tbody').innerHTML = ''; $('#fyMUrunTablo tbody').innerHTML = '';
    kurYukle();
    if (kurTaslak.length) { kurTaslak.forEach(([id, v]) => { const i = document.getElementById(id); i.value = v; kurIsaretle(i); }); kurHesapla(); }   // taslak geri yüklenir, tablo bu kurla çizilir
    saticiYukle(); musteriListesi(); musteriYukle(); if ($('#fySekmeler button.aktif')?.dataset.p === 'ozet') cizOzet();
  }
  (async () => {
    try { await yenile(); } catch (e) { mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)); return; }
    $('#fySatici').addEventListener('change', () => { $('#fyUrunTablo tbody').innerHTML = ''; saticiYukle(); });
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
    try { const p = new URLSearchParams(location.search); const sat = p.get('satici'), kod = p.get('kod'); if (sat && V.saticilar.some(s => s.ad === sat)) { $('#fySatici').value = sat; $('#fyMSatici').value = sat; $('#fyUrunTablo tbody').innerHTML = ''; saticiYukle(); musteriListesi(); } if (kod) { SECILI = kod; musteriListesi(); musteriYukle(); sekme('musteri'); return; } } catch (e) { /* yoksay */ }
    sekme(kok.dataset.baslangic || 'satici'); window.NA_HAZIR = true;
  })();
})();
