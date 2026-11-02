// ============================================================
// PERSONEL MALİYET RAPORU — ekran betiği (Views/PersonelMaliyet/Index.cshtml; veri /PersonelMaliyet/Veri; kayıt Kaydet / Ekle / Sil)
// "Personel Bordro ve Ödeme Tablosu" düzeni. Sütunlar sunucudan gelir (V.sutunlar: grup → ad, anahtar, tip, liste); tablo, dip toplam, kart ve Excel bu listeden kurulur.
//   Tipler: sira · metin · ad · tarih · secim · sayi (girilen tutar) · rx (R üstte / X altta) · gun (girilen gün) · hesap (hesaplanan) · genel (genel maliyet) · not · kart
// Özel Kişiler sekmesi: aynı tablo paneli "kapsam = ozel" ile (p.ozel = true satırlar; düzenleme, kart, Excel aynen). Ana tabloda özel kişiler görünmez.
// Ek sekmeler (salt okunur, V.ekSutunlar + V.uretim / V.primler): Üretim Personeli (SGK'sız, saat / gün bazlı X) · Prim Ödemeleri (ana tablodaki primin detayı + kişi bazında uyum) ·
//   Birleşik Tablo (Bordro + Özel + Üretim; net kalemler, Toplam Brüt Masraflar, Toplam İşveren, Genel Maliyet). "0 ₺ Sütunları Gizle" anahtarı tüm tablolarda.
// Ortak: filtreler (durum, SGK işvereni, fiili görev yeri, çatı departman, bölüm, unvan, özel durum, arama), KPI kutuları.
// Sekme "Personel Maliyet Tablosu": Düzenle modunda kimlik bilgileri (liste + serbest yazım), günler ve girilen tutarlar satır bazında değiştirilir; hesaplanan sütunlar
//   sunucuyla aynı bordro kuralıyla (hesapla) anında yenilenir; Kaydet toplu kaydeder. Her satırda Kart (Personel Cari Kartı çekmecesi) ve Sil. Personel Ekle yeni kart açar. Excel (CSV).
// Sekme "Özet Raporu": seçilebilir kırılım × maliyet kalemleri, SGK işvereni × çatı departman matrisi, maliyet kalemi dağılımı, unvan / özel durum / kıdem dağılımları, en yüksek maliyetli 10 personel, Özet Excel.
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Özel Durumu Yok", "İşten Ayrıldı", "10 Yıl ve Üzeri").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('pm'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const kurus = v => { const n = Math.round((Number(v) || 0) * 100) / 100; return Math.abs(n) < 0.005 ? 0 : n; };   // kayan nokta artığı (-0, 1e-13) → 0
  const tam = v => kurus(v).toLocaleString('tr-TR', { maximumFractionDigits: 0 });   // ₺ işaretsiz (adet, gün sayısı gibi)
  const kisa = v => tam(v) + ' ₺';   // tüm tutarlar ₺ ile
  const hucre = v => kurus(v) ? kisa(v) : '<span class="text-muted">—</span>';
  const ondalik = (v, b) => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: b, maximumFractionDigits: b });
  const dovizli = p => !!p.doviz && Number(p.dovizTutar) > 0 && Number(p.kur) > 0;   // dövizli maaş: X-Net = Döviz Tutarı × Kur
  const dovizNot = p => `${ondalik(p.dovizTutar, 2)} ${esc(p.doviz)} × ${ondalik(p.kur, 4)}`;
  /** emekli / engelli rozeti (ana tablodaki E · ♿ ile aynı; tüm sekmelerde ad yanında) */
  const ozRozet = p => !p ? '' : p.ozelDurum === 'Emekli' ? '<span class="pm-oz e" title="Emekli">· E</span>' : p.ozelDurum === 'Engelli' ? '<span class="pm-oz en" title="Engelli">· ♿</span>' : '';
  const kisiBul = (sira, ad) => (sira && V.personel.find(x => x.sira === +sira)) || (ad && V.personel.find(x => x.adSoyad === ad && (x.durum || 'Aktif') === 'Aktif')) || (ad && V.personel.find(x => x.adSoyad === ad)) || null;
  const adOz = (ad, sira) => esc(ad) + ozRozet(kisiBul(sira, ad));
  /** herhangi bir sekmeden kişinin kartını açar (özel kişiyse kapsamı değiştirir); talep çekmecesi açıksa kapatır */
  function kisiKartAc(sira, ad) { const p = kisiBul(sira, ad); if (!p) return false; talepKartKapat(); if (!!p.ozel !== (kapsam === 'ozel')) kapsam = p.ozel ? 'ozel' : 'bordro'; kartAc(p.sira); return true; }
  /** satır tıklaması etkileşimli bir öğeye geldiyse yoksay */
  const etkilesim = ev => !!ev.target.closest('button, input, select, a, label, textarea');
  const PARA_BIRIMI = [['', '₺ (TL)'], ['USD', 'USD'], ['EUR', 'EUR'], ['GBP', 'GBP']];
  const EK_SAYI = ['dovizTutar', 'kur'];   // sütunu olmayan sayısal kart alanları
  const gunFmt = v => Number(v) ? Number(v).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) : '<span class="text-muted">—</span>';
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const pct = (a, b) => b ? (a / b * 100).toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) : '0,0';
  const METIN = ['sgkIsvereni', 'fiiliGorevYeri', 'odemeSirketi', 'adSoyad', 'unvan', 'bolum', 'gorev', 'iseGiris', 'catiDepartman', 'ozelDurum', 'departmanKodu', 'departmanAciklamasi', 'sicilNo', 'sgkNo', 'durum', 'ayrilisTarihi', 'tcKimlik', 'telefon', 'eposta', 'doviz', 'not'];
  // özet kırılım kalemleri: [anahtar, başlık, X anahtarı (varsa → R üstte / X altta)]
  const KALEM = [['xNet', 'X-Net'], ['rNet', 'R-Net'], ['toplamNet', 'Toplam Net'], ['toplamEkCalisma', 'Ek Çalışma', 'toplamEkCalismaX'], ['toplamSosyal', 'Sosyal Yrd.', 'toplamSosyalX'], ['toplamPrim', 'Prim', 'toplamPrimX'], ['toplamYanHaklar', 'Yan Haklar'], ['toplamYasal', 'Yasal Öd.'], ['topEk', 'Top. Ek Ödemeler', 'topEkX'], ['kesintiler', 'Toplam Kesintiler', 'kesintilerX'],
    ['toplamBrutKazanc', 'Toplam Brüt Kazanç'], ['sgkIsciToplam', 'SGK İşçi'], ['gvTutari', 'Gelir Vergisi'], ['dvTutari', 'Damga Vergisi'], ['sgkIsverenToplam', 'SGK İşveren'], ['genelMaliyet', 'Genel Maliyet']];
  const KALEM_GRUPBAS = ['toplamEkCalisma', 'topEk', 'kesintiler', 'toplamBrutKazanc', 'sgkIsciToplam', 'sgkIsverenToplam', 'genelMaliyet'];
  /** kişi satırına özet türev alanları ekler: Top. Ek Ödemeler R / X, Toplam Kesintiler R / X */
  const ozetAlanlari = p => { const n = k => Number(p[k]) || 0; return { ...p, topEk: n('toplamEkCalisma') + n('toplamSosyal') + n('toplamPrim') + n('toplamYanHaklar') + n('toplamYasal') + n('digerEkOdemelerR'), topEkX: n('toplamEkCalismaX') + n('toplamSosyalX') + n('toplamPrimX'), kesintiler: n('kesintilerToplami'), kesintilerX: n('kesintilerToplamiX') }; };
  const KIRILIM_AD = { kaynak: 'Kaynak (Bordro / Özel / Üretim)', catiDepartman: 'Çatı Departman', sgkIsvereni: 'SGK İşvereni', fiiliGorevYeri: 'Fiili Görev Yeri', bolum: 'Bölüm', unvan: 'Unvan', gorev: 'Görev', departmanKodu: 'Departman Kodu', ozelDurum: 'Özel Durumu', kidemAralik: 'Kıdem Aralığı' };
  const KIDEM_SIRA = ['1 Yıldan Az', '1–3 Yıl', '3–5 Yıl', '5–10 Yıl', '10 Yıl ve Üzeri', 'Tarih Yok'];
  const kidemAralik = p => p.kidemYil === null || p.kidemYil === undefined ? 'Tarih Yok' : p.kidemYil < 1 ? '1 Yıldan Az' : p.kidemYil < 3 ? '1–3 Yıl' : p.kidemYil < 5 ? '3–5 Yıl' : p.kidemYil < 10 ? '5–10 Yıl' : '10 Yıl ve Üzeri';
  const kirilimDeger = (p, k) => k === 'kidemAralik' ? kidemAralik(p) : k === 'ozelDurum' ? (p.ozelDurum || 'Özel Durumu Yok') : (p[k] || '—');
  let V = null, L = [], duzenleme = false, taslak = {}, aktifSekme = 'tablo', kartSira = null, kartDegisti = false, kapsam = 'bordro';   // kapsam: bordro (Personel Maliyet Tablosu) | ozel (Özel Kişiler: p.ozel = true, aynı panel)   // taslak: sira → { alan: değer }; kartSira: açık kart (0 = yeni); kartDegisti: kartta tutar değişti
  let SUTUN = [], GIRIS = [], GUN = [], RX = [], RXX = [], XALAN = [], ETIKET = {};   // XALAN: bordroya girmeyen X tarafı alanlar (kaynak değerleri düşürmez)   // sunucudaki sütun tanımından: tüm sütunlar, girilen sayısal alanlar, gün alanları, rx (R üstte / X altta) anahtarları ve X alt alanları, anahtar → başlık
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 8000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  async function post(url, veri) { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }); return r.json(); }
  const csvIndir = (ad, satirlar) => { const t = satirlar.map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + new Date().toISOString().slice(0, 10) + '.csv'; a.click(); };
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');

  // ---- sekmeler
  function sekme(p) {
    aktifSekme = p; $$('#pmSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    if (V) yerFiltreModu(p === 'cikis');   // senaryoda Görev Yeri = İstanbul / Çorlu
    const panel = p === 'ozel' ? 'tablo' : p; $$('#pm > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === panel));
    $('#pmKpi').style.display = $(`#pm > .na-panel[data-p="${panel}"] .na-kpi`) ? 'none' : '';   // kendi KPI şeridi olan sekmede genel şerit gizlenir (yer kazancı)
    if (p === 'tablo' || p === 'ozel') {   // Özel Kişiler: aynı tablo paneli, kapsam = ozel (p.ozel = true satırlar)
      const yeni = p === 'ozel' ? 'ozel' : 'bordro';   // şerit başlığı yok (sekme adıyla aynıydı); kapsam ve dönem sayaçta (#pmSayi)
      if (yeni !== kapsam) { kapsam = yeni; if (duzenleme) modDegis(false); else if (V) ciz(); }
    }
    if (V && p === 'ozet') cizOzet(); else if (V && EK[p]) ekCiz(p); else if (V && p === 'ekcalisma') ekCalismaCiz(); else if (V && p === 'kesinti') kesintiCiz(); else if (V && p === 'muhasebe') { muhasebeCiz(); mhTazele(); } else if (V && p === 'cikis') cikisCiz();
    if (V) sabitleHepsi();
    try { const u = new URL(location.href); if (p === 'tablo') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }

  // ---- bordro hesabı (sunucudaki PmBordro.Hesapla ile aynı kural ve aynı parametreler). R = bordrolu ücret, X = bordro dışı ödeme.
  //      brutKaynak / isverenKaynak: kaynak tablodan gelen Toplam Brüt Maliyet ve İş Veren; varsa hesap bunlara bağlanır (tutar değişince düşer).
  function hesapla(p) {
    const P = V.parametre, n = k => Number(p[k]) || 0, R = v => Math.round(v * 100) / 100;
    const emekli = p.ozelDurum === 'Emekli', ay = P.ay, tavan = P.sgkTavan;
    const isciOran = emekli ? P.isciSgdp : P.isciIssizlik + P.isciMyo + P.isciGss;
    const tarife = k => { let v = 0, alt = 0; for (const [ust, oran] of P.dilimler) { if (k <= alt) break; v += (Math.min(k, ust) - alt) * oran; alt = ust; } return v; };
    const aylikGv = (m, a) => m <= 0 ? 0 : tarife(m * a) - tarife(m * (a - 1));
    const istisna = R(aylikGv(P.asgariGvMatrahi, ay)), asgDamga = P.asgariDamga;
    const xNet = dovizli(p) ? R(n('dovizTutar') * n('kur')) : n('xNet');   // dövizli maaşta X-Net ₺ karşılığı
    p.xNet = xNet; p.xNet2 = xNet; p.toplamNet = xNet + n('rNet');
    // grup toplamları (bordrodaki ekler)
    p.toplamEkCalisma = n('fazlaMesai') + n('bayramCalisma') + n('geceFarki') + n('pazarCalisma') + n('resmiTatilCalisma');
    p.toplamSosyal = n('yemek') + n('yol') + n('servis') + n('kiraDestegi') + n('egitim') + n('cocukYardimi');
    p.toplamPrim = n('maasFarki') + n('prim') + n('ozelOdeme');
    p.toplamYanHaklar = n('ozelSaglik') + n('kiyafet') + n('koruyucuEkipman') + n('dogumYardimi') + n('evlilikYardimi');
    p.toplamYasal = n('kidemTazminati') + n('ihbarTazminati') + n('iseIade') + n('yillikIzinOdemesi') + n('raporFarki') + n('dogumIzni');
    const ekler = p.toplamEkCalisma + p.toplamSosyal + p.toplamPrim + p.toplamYanHaklar + p.toplamYasal, kaynak = n('brutKaynak') > 0;
    // 1) esas brüt: kaynak tablodan ya da R-Net'ten brütleştirme (net(B) artan olduğundan ikiye bölme)
    const net = b => { const isci = Math.min(b, tavan) * isciOran; const gv = Math.max(0, aylikGv(b - isci, ay) - istisna); const dv = Math.max(0, b * P.damga - asgDamga); return b - isci - gv - dv; };
    let brut = 0;
    if (kaynak) brut = R(Math.max(0, n('brutKaynak') - ekler));
    else if (n('rNet') > 0) { let lo = 0, hi = n('rNet') * 3 + 50000; for (let i = 0; i < 80; i++) { const mid = (lo + hi) / 2; if (net(mid) < n('rNet')) lo = mid; else hi = mid; } brut = R(hi); }
    const gunluk = brut / 30;
    p.brutUcret = brut; p.gunluk = R(gunluk);
    // 2) puantaj kazançları
    p.normalKazanc = R(n('normalGun') * gunluk); p.haftaSonuKazanc = R(n('haftaSonuGun') * gunluk); p.resmiTatilKazanc = R(n('resmiTatilGun') * gunluk); p.bayramKazanc = R(n('bayramGun') * gunluk); p.ucretliIzinKazanc = R(n('ucretliIzinGun') * gunluk);
    let kazanclar = p.normalKazanc + p.haftaSonuKazanc + p.resmiTatilKazanc + p.bayramKazanc + p.ucretliIzinKazanc;
    if (kaynak && n('normalGun') + n('haftaSonuGun') + n('resmiTatilGun') + n('bayramGun') + n('ucretliIzinGun') === 30) { p.normalKazanc = R(p.normalKazanc + brut - kazanclar); kazanclar = brut; }
    // 3) brüt kazanç ve SGK matrahı
    p.toplamBrutKazanc = R(kazanclar + ekler);
    p.sgkMatrahi = Math.min(kazanclar + p.toplamEkCalisma + p.toplamPrim + (p.toplamSosyal - n('servis') - n('yol')), tavan);   // yol ve servis SGK matrahı dışında
    // 4) SGK işçi
    p.isciIssizlik = emekli ? 0 : R(p.sgkMatrahi * P.isciIssizlik); p.isciMyo = emekli ? 0 : R(p.sgkMatrahi * P.isciMyo); p.isciGss = emekli ? 0 : R(p.sgkMatrahi * P.isciGss); p.isciSgdp = emekli ? R(p.sgkMatrahi * P.isciSgdp) : 0;
    p.sgkIsciToplam = p.isciIssizlik + p.isciMyo + p.isciGss + p.isciSgdp;
    // 5) damga vergisi
    p.damgaMatrahi = R(Math.max(0, p.toplamBrutKazanc - p.toplamYanHaklar)); p.asgariDamga = p.damgaMatrahi > 0 ? asgDamga : 0; p.dvTutari = Math.max(0, R(p.damgaMatrahi * P.damga - p.asgariDamga));
    // 6) gelir vergisi
    p.gvMatrahi = R(Math.max(0, p.toplamBrutKazanc - p.toplamYasal - p.sgkIsciToplam));
    p.kumulatifMatrah = R(p.gvMatrahi * ay); p.asgariIstisna = p.gvMatrahi > 0 ? istisna : 0; p.asgariKumulatifMatrah = R(P.asgariGvMatrahi * ay);
    if (kaynak) { p.gvTutari = Math.max(0, R(p.toplamBrutKazanc - (n('rNet') + ekler) - p.sgkIsciToplam - p.dvTutari)); p.hesaplananGv = R(p.gvTutari + p.asgariIstisna + n('gvIndirimleri') + n('ozelSigortaIndirimi')); }
    else { p.hesaplananGv = R(aylikGv(p.gvMatrahi, ay)); p.gvTutari = Math.max(0, R(p.hesaplananGv - p.asgariIstisna - n('gvIndirimleri') - n('ozelSigortaIndirimi'))); }
    // 7) SGK işveren
    p.isvIssizlik = emekli ? 0 : R(p.sgkMatrahi * P.isvIssizlik); p.isvMyo = emekli ? 0 : R(p.sgkMatrahi * P.isvMyo); p.isvGss = emekli ? 0 : R(p.sgkMatrahi * P.isvGss); p.isvSgdp = emekli ? R(p.sgkMatrahi * P.isvSgdp) : 0;
    p.kvsk = R(p.sgkMatrahi * P.kvsk); p.hazineIndirimi = emekli ? 0 : R(p.sgkMatrahi * P.hazineIndirimi);
    if (n('isverenKaynak') > 0) {
      const z = n('isverenKaynak'), top = p.isvIssizlik + p.isvMyo + p.isvGss + p.isvSgdp + p.kvsk; p.hazineIndirimi = 0;
      if (top > 0) { const k = z / top; p.isvIssizlik = R(p.isvIssizlik * k); p.isvGss = R(p.isvGss * k); p.kvsk = R(p.kvsk * k); p.isvSgdp = R(p.isvSgdp * k); p.isvMyo = R(p.isvMyo * k); }
      const fark = R(z - (p.isvIssizlik + p.isvMyo + p.isvGss + p.isvSgdp + p.kvsk)); if (emekli) p.isvSgdp = R(p.isvSgdp + fark); else p.isvMyo = R(p.isvMyo + fark);
      p.sgkIsverenToplam = z;
    } else p.sgkIsverenToplam = p.isvIssizlik + p.isvMyo + p.isvGss + p.isvSgdp + p.kvsk - p.hazineIndirimi;
    // 8) ödeme özeti ve maliyet (R bordrolu, X bordro dışı)
    const bordroNet = p.toplamBrutKazanc - p.sgkIsciToplam - p.gvTutari - p.dvTutari;
    p.rNet2 = n('rNet'); p.kesintilerToplami = n('avans') + n('icra') + n('borc') + n('ozelSigortaKesintisi') + n('sendikaKatkisi') + n('bes'); p.kalanNetBanka = R(bordroNet - p.kesintilerToplami);
    p.toplamEkCalismaX = n('fazlaMesaiX'); p.toplamSosyalX = n('yemekX') + n('yolX'); p.toplamPrimX = n('primX');   // X tarafı grup toplamları: brüt ve vergiye girmez
    p.digerEkOdemelerX = p.toplamEkCalismaX + p.toplamSosyalX + p.toplamPrimX;
    p.netRBordro = R(p.kalanNetBanka + n('digerEkOdemelerR')); p.netXBordro = xNet + p.digerEkOdemelerX; p.kalanNetElden = R(p.netXBordro - n('kesintilerToplamiX'));
    p.araToplam = p.netXBordro + p.netRBordro; p.toplamBrutMasraflar = R(p.toplamBrutKazanc - p.kalanNetBanka); p.toplamIsveren = p.sgkIsverenToplam;
    p.genelMaliyet = R(p.araToplam + p.toplamBrutMasraflar + p.toplamIsveren);
    return p;
  }
  /** taslakta tutar / gün / özel durum değiştiyse kaynak tablo değerleri düşer (sunucu da kayıtta aynı şekilde siler) */
  const hesapDegisti = t => Object.keys(t || {}).some(k => (GIRIS.includes(k) && !XALAN.includes(k)) || k === 'ozelDurum');   // X tarafı tutarlar bordroya girmez; kaynak değerleri düşürmez
  const taslakla = (p, t) => hesapla({ ...p, ...t, ...(hesapDegisti(t) ? { brutKaynak: null, isverenKaynak: null } : {}) });
  const etkin = p => duzenleme && taslak[p.sira] ? taslakla(p, taslak[p.sira]) : p;
  const bul = sira => V.personel.find(x => x.sira === sira);
  /** sunucuya gidecek tam satır (kimlik + günler + tutarlar + not) */
  function satirVeri(p) { const d = { sira: p.sira, ozel: !!p.ozel }; METIN.forEach(k => d[k] = p[k] ?? ''); GIRIS.concat(EK_SAYI).forEach(k => d[k] = Number(p[k]) || 0); return d; }

  // ---- filtre
  function liste(durumYoksay, ozelMi) {
    const ara = ($('#pmAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value, oz = v('#pmOzel'), du = durumYoksay ? '' : v('#pmDurum');
    const ozel = ozelMi === undefined ? kapsam === 'ozel' : ozelMi;   // kapsam: özel kişiler ayrı sekmede, ana tabloda görünmez
    return V.personel.filter(p => !!p.ozel === ozel && (!du || (p.durum || 'Aktif') === du) && (!v('#pmSgk') || p.sgkIsvereni === v('#pmSgk')) && yerUyar(p.fiiliGorevYeri) && (!v('#pmCati') || p.catiDepartman === v('#pmCati')) && (!v('#pmBolum') || p.bolum === v('#pmBolum')) && (!v('#pmUnvan') || p.unvan === v('#pmUnvan'))
      && (!oz || (oz === '_yok' ? !p.ozelDurum : p.ozelDurum === oz))
      && (!ara || [p.adSoyad, p.cariKodu, p.gorev, p.bolum, p.unvan, p.not, p.sgkIsvereni, p.fiiliGorevYeri, p.catiDepartman, p.departmanKodu, p.departmanAciklamasi, p.sicilNo, p.sgkNo, p.tcKimlik, p.eposta].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  const toplam = (l, k) => l.reduce((s, p) => s + (Number(p[k]) || 0), 0);
  /** görev yeri filtresi: normal modda fiili görev yeri birebir; İşten Çıkış Senaryosu'nda lokasyon (İstanbul / Çorlu) */
  const yerUyar = yer => { const v = $('#pmYer').value; return !v || yer === v || ((v === 'İstanbul' || v === 'Çorlu') && lokasyonAd(yer) === v); };
  function yerFiltreModu(lokasyonMu) {
    const e = $('#pmYer'), l = $('#pmYerEtiket'); if (!e) return; const mod = lokasyonMu ? 'lokasyon' : 'fiili'; if (e.dataset.mod === mod) return;
    e.dataset.mod = mod; e.value = '';
    if (lokasyonMu) { l.textContent = 'Görev Yeri'; e.innerHTML = '<option value="">Tümü</option><option value="İstanbul">İstanbul</option><option value="Çorlu">Çorlu</option>'; }
    else { l.textContent = 'Fiili Görev Yeri'; e.innerHTML = '<option value="">Tümü</option>' + (V.gorevYerleri || []).map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); }
  }

  // ---- seçim listeleri (filtreler + datalist'ler; seçili değer korunur)
  function listeleriDoldur() {
    const doldur = (id, l, ek) => { const e = $(id), v = e.value; e.innerHTML = '<option value="">Tümü</option>' + (ek || '') + l.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); e.value = l.includes(v) || v === '_yok' ? v : ''; };
    doldur('#pmSgk', V.sgkIsverenleri); if ($('#pmYer').dataset.mod !== 'lokasyon') doldur('#pmYer', V.gorevYerleri); doldur('#pmCati', V.catiDepartmanlar); doldur('#pmBolum', V.bolumler); doldur('#pmUnvan', V.unvanlar); doldur('#pmOzel', V.ozelDurumlar, '<option value="_yok">Özel Durumu Yok</option>');
    const dl = (id, l) => { $(id).innerHTML = l.map(x => `<option value="${esc(x)}">`).join(''); };
    dl('#dlSgk', V.sgkIsverenleri); dl('#dlYer', V.gorevYerleri); dl('#dlUnvan', V.unvanlar); dl('#dlBolum', V.bolumler); dl('#dlCati', V.catiDepartmanlar); dl('#dlGorev', V.gorevler || []);
  }
  /** sunucu yanıtındaki güncel veriyi alır ve ekranı tazeler */
  function veriGuncelle(j) { ['personel', 'sonKayit', 'sgkIsverenleri', 'gorevYerleri', 'catiDepartmanlar', 'bolumler', 'unvanlar', 'gorevler', 'ozelDurumlar', 'durumlar', 'sutunlar', 'parametre', 'ekSutunlar', 'uretim', 'primler', 'donem', 'donemAd', 'donemler', 'ekCalisma', 'kesinti', 'muhasebe'].forEach(k => { if (j[k] !== undefined) V[k] = j[k]; }); sutunlariKur(); listeleriDoldur(); donemKur(); ekKur(); kayitBilgi(); ciz(); }
  // ---- dönem (ay) seçimi: seçilen ayın verisi sunucudan yüklenir; kayıt / ekle / sil seçili döneme işlenir
  const oncekiAy = d => { const y = +d.slice(0, 4), m = +d.slice(5, 7); return m === 1 ? `${y - 1}-12` : `${y}-${String(m - 1).padStart(2, '0')}`; };
  function donemKur() {
    const sel = $('#pmDonem'); sel.innerHTML = (V.donemler || []).map(d => `<option value="${esc(d.kod)}">${esc(d.ad)}${d.guncel ? ' (Güncel)' : ''}</option>`).join(''); sel.value = V.donem;
    $('#pmDonemPill').textContent = V.donemAd || V.yil;
    const ps = $('#pmPrimDonem'), pd = oncekiAy(V.donem); if (ps && [...ps.options].some(o => o.value === pd)) ps.value = pd;   // prim sekmesi: bu dönemin bordrosuna giren prim ayı
    try { const u = new URL(location.href); if (V.donemler && V.donemler.some(d => d.kod === V.donem && d.guncel)) u.searchParams.delete('donem'); else u.searchParams.set('donem', V.donem); history.replaceState(null, '', u); } catch (e) { /* yoksay */ }
  }
  async function donemDegis() {
    const d = $('#pmDonem').value; if (!d || d === V.donem) return;
    if (duzenleme) { if (Object.keys(taslak).length && !window.confirm('Kaydedilmemiş değişiklikler var. Dönem değiştirilince kaybolacak, devam edilsin mi?')) { $('#pmDonem').value = V.donem; return; } modDegis(false); }
    try { const j = await fetch('/PersonelMaliyet/Veri?donem=' + encodeURIComponent(d)).then(r => r.json()); veriGuncelle(j); if (aktifSekme === 'ozet') cizOzet(); else if (EK[aktifSekme]) ekCiz(aktifSekme); }
    catch (e) { mesaj('danger', 'Dönem verisi yüklenemedi: ' + esc(e.message)); $('#pmDonem').value = V.donem; }
  }
  function sutunlariKur() {
    SUTUN = V.sutunlar.flatMap(g => g.sutunlar); RX = SUTUN.filter(s => s.tip === 'rx').map(s => s.anahtar); RXX = RX.map(k => k + 'X'); XALAN = RXX.concat(['xNet', 'kesintilerToplamiX']);
    GIRIS = SUTUN.filter(s => s.tip === 'sayi' || s.tip === 'rx' || s.tip === 'gun').map(s => s.anahtar).concat(RXX); GUN = SUTUN.filter(s => s.tip === 'gun').map(s => s.anahtar);
    ETIKET = {}; SUTUN.forEach(s => { ETIKET[s.anahtar] = s.ad; if (s.tip === 'rx') ETIKET[s.anahtar + 'X'] = s.ad + ' (X)'; });
  }
  const girdiTipi = s => s.tip === 'sayi' || s.tip === 'rx' || s.tip === 'gun';

  // ---- ortak: KPI + dip toplam
  function kpiCiz() {
    const n = L.length, top = k => toplam(L, k);
    const ayrilan = liste(true).filter(p => p.durum === 'İşten Ayrıldı').length, du = $('#pmDurum').value;   // Durum filtresinden bağımsız gerçek ayrılan sayısı
    const kpi = [['#0f172a', 'Personel', n, `${L.filter(p => p.ozelDurum === 'Emekli').length} Emekli · ${L.filter(p => p.ozelDurum === 'Gündelikçi').length} Gündelikçi · ${ayrilan} Ayrılan${du === 'Aktif' && ayrilan ? ' (Filtre Dışı)' : ''}`], ['#0f766e', 'Toplam Net Ücret', kisa(top('toplamNet')), `R (Bordro) ${kisa(top('rNet'))} · X ${kisa(top('xNet'))}`],
      ['#1d4ed8', 'Toplam Brüt Kazanç', kisa(top('toplamBrutKazanc')), `SGK Matrahı ${kisa(top('sgkMatrahi'))}`], ['#b45309', 'Vergi ve İşçi Payları', kisa(top('toplamBrutMasraflar')), `GV ${kisa(top('gvTutari'))} · DV ${kisa(top('dvTutari'))} · SGK İşçi ${kisa(top('sgkIsciToplam'))}`],
      ['#7c3aed', 'SGK İşveren', kisa(top('sgkIsverenToplam')), `Hazine İndirimi ${kisa(top('hazineIndirimi'))} · KVSK ${kisa(top('kvsk'))}`], ['#dc2626', 'Genel Maliyet', kisa(top('genelMaliyet')), `Ortalama ${kisa(n ? top('genelMaliyet') / n : 0)} / Kişi`]];
    $('#pmKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $(kapsam === 'ozel' ? '#pmNOzel' : '#pmNTablo').textContent = n;
  }
  function dipToplam() {
    const top = k => toplam(L, k);
    $('#pmToplam').innerHTML = SUTUN.map(s => { const dk = `data-k="${s.anahtar}"`; if (s.tip === 'sira') return `<td ${dk}>Toplam</td>`; if (s.tip === 'ad') return `<td ${dk}>${L.length} Personel</td>`; if (s.tip === 'rx') return `<td class="s rx" ${dk}>${rxHucre(top(s.anahtar), top(s.anahtar + 'X'), kisa)}</td>`; if (s.tip === 'sayi' || s.tip === 'hesap') return `<td class="s" ${dk}>${kisa(top(s.anahtar))}</td>`; if (s.tip === 'genel') return `<td class="s genel" ${dk}>${kisa(top(s.anahtar))}</td>`; return `<td ${dk}></td>`; }).join('');
    $('#pmSayi').textContent = `${L.length} / ${V.personel.filter(p => !!p.ozel === (kapsam === 'ozel')).length} ${kapsam === 'ozel' ? 'özel kişi' : 'personel'} · ${V.donemAd || ''} · aylık ₺${duzenleme ? ` · ${Object.keys(taslak).length} satırda değişiklik var, Kaydet ile saklanır` : ''}`;
  }

  // ---- sekme 1 çizim (sütun tanımına göre)
  const OZEL_SECENEK = [['', '—'], ['Emekli', 'Emekli'], ['Engelli', 'Engelli'], ['Gündelikçi', 'Gündelikçi']];
  /** rx hücresi: R tutarı üstte, X tutarı altta ("3.000 R" / "2.500 X") */
  const rxHucre = (r, x, f) => `<div class="rx"><span>${(f || hucre)(r)}<small>R</small></span><span class="x">${(f || hucre)(x)}<small>X</small></span></div>`;
  function hucreHtml(p, s, ilk, satirNo) {
    const k = s.anahtar, gb = ilk ? 'grupBas' : '', dk = `data-k="${k}"`;
    const al = etiket => `aria-label="${esc(etiket)} · ${esc(p.adSoyad || 'Yeni')}"`;   // ekran okuyucu: sütun adı + personel
    const metin = cls => duzenleme ? `<input class="gir ${cls}" data-sira="${p.sira}" data-alan="${k}" value="${esc(p[k] ?? '')}" ${s.liste ? `list="${s.liste}"` : ''} maxlength="120" ${al(s.ad)}>` : esc(p[k]);
    switch (s.tip) {
      case 'sira': return `<td class="kucuk ${gb}" ${dk} title="Cari kodu ${esc(p.cariKodu)}">${satirNo}</td>`;   // sıra listedeki satır numarası (dinamik); kimlik cari kodu
      case 'ad': return `<td class="ad ${gb}" ${dk}>${metin('metin')}</td>`;
      case 'metin': return `<td class="${gb}" ${dk}>${metin(k === 'gorev' || k === 'departmanAciklamasi' ? 'genis' : 'dar')}</td>`;
      case 'tarih': return `<td class="kucuk ${gb}" ${dk}>${duzenleme ? `<input class="gir tarih" type="date" data-sira="${p.sira}" data-alan="${k}" value="${esc(p[k] || '')}" ${al(s.ad)}>` : bos(p[k + 'Tr'])}</td>`;
      case 'secim': return `<td class="kucuk ${gb}" ${dk}>${duzenleme ? `<select class="gir" data-sira="${p.sira}" data-alan="${k}" ${al(s.ad)}>${OZEL_SECENEK.map(o => `<option value="${esc(o[0])}" ${(p[k] || '') === o[0] ? 'selected' : ''}>${esc(o[1])}</option>`).join('')}</select>` : bos(p[k])}</td>`;
      case 'sayi': {
        const dv = k === 'xNet' && dovizli(p);   // dövizli maaş: X-Net kartta döviz tutarı × kur ile belirlenir
        return `<td class="s ${gb}" ${dk}>${duzenleme ? `<input class="gir ${dv ? 'ro' : ''}" type="number" min="0" step="1" data-sira="${p.sira}" data-alan="${k}" value="${Number(p[k]) || 0}" ${dv ? 'readonly title="Dövizli maaş: kartta Döviz Tutarı × Kur ile belirlenir"' : ''} ${al(s.ad)}>` : hucre(p[k])}${dv ? `<small class="dv">${dovizNot(p)}</small>` : ''}</td>`;
      }
      case 'rx': return `<td class="s rx ${gb}" ${dk}>${duzenleme ? `<div class="rx"><span><input class="gir" type="number" min="0" step="1" data-sira="${p.sira}" data-alan="${k}" value="${Number(p[k]) || 0}" ${al(s.ad + ' R')}><small>R</small></span><span class="x"><input class="gir x" type="number" min="0" step="1" data-sira="${p.sira}" data-alan="${k}X" value="${Number(p[k + 'X']) || 0}" ${al(s.ad + ' X')}><small>X</small></span></div>` : rxHucre(p[k], p[k + 'X'])}</td>`;
      case 'gun': return `<td class="s ${gb}" ${dk}>${duzenleme ? `<input class="gir gun" type="number" min="0" max="31" step="0.5" data-sira="${p.sira}" data-alan="${k}" value="${Number(p[k]) || 0}" ${al(s.ad)}>` : gunFmt(p[k])}</td>`;
      case 'hesap': return `<td class="s hesap ${gb}" ${dk}>${hucre(p[k])}</td>`;
      case 'genel': return `<td class="s genel ${gb}" ${dk}>${kisa(p[k])}</td>`;
      case 'not': return `<td class="${gb}" ${dk}>${duzenleme ? `<input class="gir not" data-sira="${p.sira}" data-alan="not" value="${esc(p.not || '')}" maxlength="200" ${al('Not')}>` : `<small>${bos(p.not)}</small>`}</td>`;
      case 'kart': return `<td class="${gb}" ${dk}><button type="button" class="btn btn-light btn-sm kart" data-sira="${p.sira}" title="Personel Cari Kartı ${esc(p.cariKodu)}"><i class="fas fa-id-card me-1"></i>Kart</button>${duzenleme ? ` <button type="button" class="btn btn-light btn-sm sil" data-sira="${p.sira}" title="Satırı sil"><i class="fas fa-trash"></i></button>` : ''}</td>`;
      default: return `<td ${dk}>${bos(p[k])}</td>`;
    }
  }
  function satirHtml(p, satirNo) {
    const hucreler = V.sutunlar.map(g => g.sutunlar.map((s, i) => hucreHtml(p, s, i === 0, satirNo)).join('')).join('');
    return `<tr class="${p.ozelDurum === 'Emekli' ? 'emekli' : p.ozelDurum === 'Engelli' ? 'engelli' : ''} ${p.durum === 'İşten Ayrıldı' ? 'ayrildi' : ''} ${taslak[p.sira] ? 'degisti' : ''}" data-sira="${p.sira}">${hucreler}</tr>`;
  }
  function ciz() {
    L = liste().map(etkin); kpiCiz();
    $('#pmTablo tbody').innerHTML = L.map((p, i) => satirHtml(p, i + 1)).join('') || `<tr><td colspan="${SUTUN.length}" class="na-bos">Filtreye uyan personel yok.</td></tr>`;
    dipToplam(); sifirGizle($('#pmTablo'), SUTUN, L, $('#pmSifirGizle').checked, V.sutunlar);
    if (duzenleme) $$('#pmTablo .gir').forEach(i => i.addEventListener('input', () => { const s = +i.dataset.sira, a = i.dataset.alan; taslak[s] = taslak[s] || {}; taslak[s][a] = GIRIS.includes(a) ? (Number(i.value) || 0) : i.value; satirGuncelle(s, GIRIS.includes(a) || a === 'ozelDurum'); }));
    if (aktifSekme === 'ozet') cizOzet(); else if (EK[aktifSekme]) ekCiz(aktifSekme); else if (aktifSekme === 'ekcalisma') ekCalismaCiz(); else if (aktifSekme === 'kesinti') kesintiCiz(); else if (aktifSekme === 'muhasebe') muhasebeCiz(); else if (aktifSekme === 'cikis') cikisCiz();
    sabitleHepsi();
  }
  /** Sola sabit sütunlar: Sıra'dan Adı-Soyadı'na kadar olan hücreler yatay kaydırmada yerinde kalır (başlık satırları zaten dikeyde yapışkan). Her çizimden sonra çağrılır; genişlikler ölçülüp left değerleri yazılır. */
  function sabitle(tablo) {
    if (!tablo) return;
    const bas = tablo.querySelector('thead tr.sutun') || tablo.querySelector('thead tr.tek') || tablo.querySelector('thead tr'); if (!bas) return;
    const ths = Array.from(bas.children).filter(th => !th.classList.contains('gizli'));
    let adet = ths.findIndex(th => th.textContent.trim().startsWith('Adı-Soyadı')) + 1; if (adet <= 0) return;
    $$('.sabit', tablo).forEach(e => { e.classList.remove('sabit'); e.style.left = ''; });
    let left = 0; const sollar = [];
    for (let i = 0; i < adet; i++) { sollar.push(left); left += ths[i].getBoundingClientRect().width; }
    const uygula = (hucre, i) => { hucre.classList.add('sabit'); hucre.style.left = sollar[i] + 'px'; };
    ths.slice(0, adet).forEach(uygula);
    const grup = tablo.querySelector('thead tr.grup th'); if (grup) uygula(grup, 0);
    // birleşik (colspan) hücre sabit sütun sınırını aşıyorsa orada durulur; aksi halde yapışkan hücre sağındaki toplamları örter
    tablo.querySelectorAll('tbody tr, tfoot tr').forEach(tr => { const tds = Array.from(tr.children).filter(td => !td.classList.contains('gizli')); if (tds.length < adet || (tds.length === 1 && tds[0].colSpan > 1)) return; let i = 0; for (const td of tds) { if (i >= adet || i + (td.colSpan || 1) > adet) break; uygula(td, i); i += td.colSpan || 1; } });
  }
  /** tek tablolu sekmelerde (data-tam) kaydırma kabı ekranın kalanını doldurur: üst şeritler ince olduğundan en çok satır görünür */
  function kapBoyut() { $$('.na-panel.aktif .pm-kaydir[data-tam]').forEach(k => { const r = k.getBoundingClientRect(); k.style.maxHeight = Math.max(320, window.innerHeight - Math.max(r.top, 0) - 28) + 'px'; }); }
  const sabitleHepsi = () => requestAnimationFrame(() => { kapBoyut(); $$('.na-panel.aktif table.pm-excel').forEach(sabitle); });
  /** "0 ₺ Sütunları Gizle": listedeki tüm satırlarda sıfır olan sayısal sütunlar (rx'te R ve X birlikte) gizlenir; grup başlıklarının colspan'ı daralır */
  function sifirGizle(tablo, sutunlar, satirlar, acik, gruplar) {
    const sifir = s => acik && ['sayi', 'rx', 'gun', 'hesap', 'genel'].includes(s.tip) && !satirlar.some(p => Number(p[s.anahtar]) || (s.tip === 'rx' && Number(p[s.anahtar + 'X'])));
    const gizli = new Set(sutunlar.filter(sifir).map(s => s.anahtar));
    $$('th[data-k], td[data-k]', tablo).forEach(e => e.classList.toggle('gizli', gizli.has(e.dataset.k)));
    if (gruplar) $$('thead tr.grup th', tablo).forEach((th, i) => { const g = gruplar[i]; if (!g) return; const n = g.sutunlar.filter(s => !gizli.has(s.anahtar)).length; th.colSpan = Math.max(1, n); th.classList.toggle('gizli', n === 0); });
    return gizli.size;
  }
  /** düzenleme modunda tek satırın hesaplanan hücrelerini ve dip toplamı tazeler (girişleri yeniden çizmeden) */
  function satirGuncelle(sira, hesapMi) {
    const tr = $(`#pmTablo tr[data-sira="${sira}"]`); if (!tr) return; tr.classList.add('degisti');
    if (hesapMi) { const p = taslakla(bul(sira), taslak[sira]); SUTUN.filter(s => s.tip === 'hesap' || s.tip === 'genel').forEach(s => { const td = tr.querySelector(`td[data-k="${s.anahtar}"]`); if (td) td.innerHTML = s.tip === 'genel' ? kisa(p[s.anahtar]) : hucre(p[s.anahtar]); }); }
    L = liste().map(etkin); kpiCiz(); dipToplam(); sabitleHepsi();
  }

  // ---- sekme 2: özet raporu
  function grupla(l, anahtar) { const m = {}; l.forEach(p => { const k = kirilimDeger(p, anahtar); const o = m[k] || (m[k] = { ad: k, n: 0 }); o.n++; KALEM.forEach(([f, , x]) => { o[f] = (o[f] || 0) + (Number(p[f]) || 0); if (x) o[x] = (o[x] || 0) + (Number(p[x]) || 0); }); }); return m; }
  /** özet raporu kapsamı: bordrolu personel + özel kişiler (ana filtrelerle) + SGK'sız gündelikçi üretim personeli — tüm maliyetler, kapsamdan bağımsız */
  function ozetListe() {
    const uretim = (V.uretim || []).filter(u => !u.sgkLi && yerUyar(u.fiiliGorevYeri)).map(u => ({ kaynak: 'Üretim', sira: -u.sira, adSoyad: u.adSoyad, unvan: 'Gündelikçi', gorev: u.gorev, sgkIsvereni: 'SGK Dışı (Gündelikçi)', fiiliGorevYeri: u.fiiliGorevYeri, catiDepartman: 'Üretim', bolum: 'Üretim', ozelDurum: 'Gündelikçi', kidemYil: null,
      xNet: (Number(u.hakEdis) || 0) + (Number(u.tatilKazanci) || 0), rNet: 0, toplamNet: (Number(u.hakEdis) || 0) + (Number(u.tatilKazanci) || 0), toplamEkCalisma: 0, toplamEkCalismaX: Number(u.fazlaMesaiKazanci) || 0, toplamSosyal: 0, toplamSosyalX: Number(u.toplamSosyal) || 0, toplamPrim: 0, toplamPrimX: 0, toplamYanHaklar: 0, toplamYasal: 0, toplamBrutKazanc: 0, sgkIsciToplam: 0, gvTutari: 0, dvTutari: 0, sgkIsverenToplam: 0, toplamBrutMasraflar: 0, digerEkOdemelerX: 0, digerEkOdemelerR: 0, kesintilerToplami: 0, kesintilerToplamiX: Number(u.kesinti) || 0, genelMaliyet: Number(u.toplamMaliyet) || 0 }));
    return liste(false, false).map(etkin).map(p => ({ ...p, kaynak: 'Bordro' })).concat(liste(false, true).map(etkin).map(p => ({ ...p, kaynak: 'Özel' }))).concat(uretim).map(ozetAlanlari);
  }
  function cizOzet() {
    const LO = ozetListe(), k = $('#pmKirilim').value, genel = toplam(LO, 'genelMaliyet');
    $('#pmKirilimBaslik').textContent = KIRILIM_AD[k];
    $('#pmKirilimBas').innerHTML = `<th>${esc(KIRILIM_AD[k])}</th><th class="s">Personel</th>${KALEM.map(([f, ad, x], i) => `<th class="s ${i === 0 || KALEM_GRUPBAS.includes(f) ? 'grupBas' : ''}">${esc(ad)}</th>`).join('')}<th class="s">Ort. Maliyet</th><th class="s">Pay %</th>`;
    const m = grupla(LO, k);
    const keys = k === 'kidemAralik' ? KIDEM_SIRA.filter(x => m[x]) : Object.keys(m).sort((a, b) => m[b].genelMaliyet - m[a].genelMaliyet);
    const hucreK = (o, f, x, fmt) => x ? `<td class="s rx ${KALEM_GRUPBAS.includes(f) ? 'grupBas' : ''}">${rxHucre(o[f], o[x], fmt)}</td>` : `<td class="s ${f === 'genelMaliyet' ? 'genel' : ''} ${KALEM_GRUPBAS.includes(f) ? 'grupBas' : ''}">${fmt ? fmt(o[f]) : hucre(o[f])}</td>`;
    const satir = (o, cls) => `<tr class="${cls || ''}" ${cls ? '' : `data-deger="${esc(o.ad)}"`}><td class="ad">${esc(o.ad)}</td><td class="s">${o.n}</td>${KALEM.map(([f, , x]) => hucreK(o, f, x, cls ? kisa : null)).join('')}<td class="s">${kisa(o.n ? o.genelMaliyet / o.n : 0)}</td><td class="s">${pct(o.genelMaliyet, genel)}</td></tr>`;
    const T = { ad: 'Toplam', n: LO.length }; KALEM.forEach(([f, , x]) => { T[f] = toplam(LO, f); if (x) T[x] = toplam(LO, x); });
    $('#pmKirilimTablo tbody').innerHTML = keys.length ? keys.map(x => satir(m[x])).join('') + satir(T, 'toplam') : `<tr><td colspan="${KALEM.length + 4}" class="na-bos">Filtreye uyan personel yok.</td></tr>`;
    const sgkler = Object.keys(grupla(LO, 'sgkIsvereni')).sort((a, b) => LO.filter(p => p.sgkIsvereni === b).length - LO.filter(p => p.sgkIsvereni === a).length);
    const catilar = Object.keys(grupla(LO, 'catiDepartman')).sort((a, b) => LO.filter(p => p.catiDepartman === b).length - LO.filter(p => p.catiDepartman === a).length);
    $('#pmMatris thead').innerHTML = `<tr><th>SGK İşvereni</th>${catilar.map(c => `<th class="s">${esc(c)}</th>`).join('')}<th class="s">Toplam</th></tr>`;
    const h = l => l.length ? `<span class="n">${l.length}</span><small>${kisa(toplam(l, 'genelMaliyet'))}</small>` : '<span class="text-muted">—</span>';
    $('#pmMatris tbody').innerHTML = sgkler.map(s => { const ls = LO.filter(p => p.sgkIsvereni === s); return `<tr><td data-sgk="${esc(s)}"><b>${esc(s)}</b></td>${catilar.map(c => `<td class="s" data-sgk="${esc(s)}" data-cati="${esc(c)}">${h(ls.filter(p => p.catiDepartman === c))}</td>`).join('')}<td class="s" data-sgk="${esc(s)}">${h(ls)}</td></tr>`; }).join('') + `<tr class="toplam"><td>Toplam</td>${catilar.map(c => `<td class="s" data-cati="${esc(c)}">${h(LO.filter(p => p.catiDepartman === c))}</td>`).join('')}<td class="s">${h(LO)}</td></tr>`;
    const kal = [['rNet', 'R-Net Ücret (Bordro)'], ['xNet', 'X-Net Ücret (Bordro Dışı)'], ['toplamEkCalisma', 'Ek Çalışmalar'], ['toplamSosyal', 'Sosyal Yardımlar'], ['toplamPrim', 'Prim ve Ek Kazançlar'], ['toplamYanHaklar', 'Yan Haklar'], ['toplamYasal', 'Yasal Ödemeler'], ['toplamBrutMasraflar', 'Vergi ve SGK İşçi Payları'], ['sgkIsverenToplam', 'SGK İşveren Payı']].map(([f, ad]) => [ad, toplam(LO, f)]);
    kal.push(['Diğer Ek Ödemeler (X + R)', toplam(LO, 'digerEkOdemelerX') + toplam(LO, 'digerEkOdemelerR')]);
    kal.push(['X Tarafı Ek Kalemler (Mesai · Sosyal · Prim)', toplam(LO, 'toplamEkCalismaX') + toplam(LO, 'toplamSosyalX') + toplam(LO, 'toplamPrimX')]);
    const maks = Math.max(1, ...kal.map(x => x[1]));
    $('#pmKalemler').innerHTML = kal.map(([ad, v]) => `<div class="pm-bar"><span>${esc(ad)}</span><span class="b"><i style="width:${v ? Math.max(1.5, v / maks * 100) : 0}%"></i></span><span class="v">${v ? kisa(v) : '—'} <small>${v ? '%' + pct(v, genel) : ''}</small></span></div>`).join('');
    const dag = (id, anahtar, sira) => { const g = grupla(LO, anahtar); const ks = sira ? sira.filter(x => g[x]) : Object.keys(g).sort((a, b) => g[b].genelMaliyet - g[a].genelMaliyet); const r = o => `<tr data-anahtar="${anahtar}" data-deger="${esc(o.ad)}"><td>${esc(o.ad)}</td><td class="s">${o.n}</td><td class="s">${kisa(o.genelMaliyet)}</td><td class="s">${kisa(o.n ? o.genelMaliyet / o.n : 0)}</td></tr>`;
      $('#' + id + ' tbody').innerHTML = ks.length ? ks.map(x => r(g[x])).join('') + `<tr class="toplam"><td>Toplam</td><td class="s">${LO.length}</td><td class="s">${kisa(genel)}</td><td class="s">${kisa(LO.length ? genel / LO.length : 0)}</td></tr>` : '<tr><td colspan="4" class="na-bos">Kayıt yok.</td></tr>'; };
    dag('pmDagUnvan', 'unvan'); dag('pmDagOzel', 'ozelDurum', ['Emekli', 'Engelli', 'Gündelikçi', 'Özel Durumu Yok']); dag('pmDagKidem', 'kidemAralik', KIDEM_SIRA); dag('pmDagKaynak', 'kaynak', ['Bordro', 'Özel', 'Üretim']);
    $('#pmTop10 tbody').innerHTML = [...LO].sort((a, b) => b.genelMaliyet - a.genelMaliyet).slice(0, 10).map((p, i) => `<tr class="${p.sira > 0 ? 'kartAc' : ''}" data-sira="${p.sira}" style="${p.sira > 0 ? 'cursor:pointer' : ''}"><td>${i + 1}</td><td><b>${esc(p.adSoyad)}</b>${ozRozet(p)}${p.ozelDurum === 'Gündelikçi' ? ` <small class="text-muted">(Gündelikçi)</small>` : ''} <span class="pm-rz ${p.kaynak === 'Özel' ? 'mavi' : p.kaynak === 'Üretim' ? 'bekle' : 'gri'}">${esc(p.kaynak)}</span></td><td>${esc(p.unvan)}</td><td>${esc(p.gorev)}</td><td>${esc(p.sgkIsvereni)}</td><td>${esc(p.catiDepartman)}</td><td class="s">${kisa(p.toplamNet)}</td><td class="s">${kisa(p.toplamBrutKazanc)}</td><td class="s">${hucre(p.sgkIsverenToplam)}</td><td class="s"><b>${kisa(p.genelMaliyet)}</b></td><td class="s">${pct(p.genelMaliyet, genel)}</td></tr>`).join('') || '<tr><td colspan="11" class="na-bos">Kayıt yok.</td></tr>';
    $('#pmOzetKapsam').textContent = `${LO.filter(p => p.kaynak === 'Bordro').length} bordrolu · ${LO.filter(p => p.kaynak === 'Özel').length} özel kişi · ${LO.filter(p => p.kaynak === 'Üretim').length} SGK'sız gündelikçi · toplam ${LO.length} kişi · genel maliyet ${kisa(genel)}`;
    sabitleHepsi();
  }
  function ozetExcel() {
    const k = $('#pmKirilim').value, rows = [];
    const temizle = t => t.trim().replace('—', '').replace(/\s*₺/g, '');   // Excel'de ₺ işareti olmadan
    const tabloSatirlari = (id, bas) => { rows.push([bas]); rows.push($$('#' + id + ' thead th').map(t => t.textContent.trim())); $$('#' + id + ' tbody tr').forEach(tr => rows.push($$('td', tr).map(td => temizle(td.textContent)))); rows.push([]); };
    tabloSatirlari('pmKirilimTablo', `${KIRILIM_AD[k]} Bazında Maliyet Kalemleri (${V.yil}, aylık ₺)`);
    tabloSatirlari('pmMatris', 'SGK İşvereni × Çatı Departman (personel sayısı ve genel maliyet)');
    rows.push(['Maliyet Kalemi Dağılımı']); rows.push(['Kalem', 'Tutar', 'Pay %']); $$('#pmKalemler .pm-bar').forEach(d => rows.push([d.children[0].textContent.trim(), temizle(d.children[2].childNodes[0].textContent), (d.querySelector('small').textContent || '').replace('%', '')])); rows.push([]);
    tabloSatirlari('pmDagUnvan', 'Unvan Dağılımı'); tabloSatirlari('pmDagOzel', 'Özel Durum Dağılımı'); tabloSatirlari('pmDagKidem', 'Kıdem Dağılımı'); tabloSatirlari('pmTop10', 'En Yüksek Maliyetli 10 Personel');
    csvIndir('personel_maliyet_ozet_raporu', rows);
  }

  // ---- tablo düzenleme / toplu kayıt
  function modDegis(ac) { duzenleme = ac; if (!ac) taslak = {}; $('#pmDuzenle').style.display = ac ? 'none' : ''; $('#pmKaydet').style.display = ac ? '' : 'none'; $('#pmVazgec').style.display = ac ? '' : 'none'; ciz(); }
  async function kaydet() {
    const satirlar = Object.keys(taslak).map(s => satirVeri({ ...bul(+s), ...taslak[s] }));
    if (!satirlar.length) { mesaj('info', 'Değişiklik yok.'); return; }
    const j = await post('/PersonelMaliyet/Kaydet', { satirlar, donem: V.donem });
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    duzenleme = false; taslak = {}; $('#pmDuzenle').style.display = ''; $('#pmKaydet').style.display = 'none'; $('#pmVazgec').style.display = 'none';
    veriGuncelle(j); mesaj('success', esc(j.message));
  }
  async function sil(sira) {
    const p = bul(sira); if (!p) return;
    if (!window.confirm(`${p.adSoyad} (${p.cariKodu}) satırı kalıcı olarak silinsin mi? İşten ayrılan personel silinmez; kartında Durum = İşten Ayrıldı yapılır.`)) return;
    const j = await post('/PersonelMaliyet/Sil', { sira, donem: V.donem });
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    delete taslak[sira]; if (kartSira === sira) kartKapat(); veriGuncelle(j); mesaj('success', esc(j.message));
  }
  function kayitBilgi() { const e = $('#pmKayitBilgi'); if (!e) return; e.textContent = V.sonKayit ? `Son kayıt: ${V.sonKayit.kisi} · ${V.sonKayit.tarih}` : `Kaynak tablo: ${V.kaynak} · henüz portalda düzenlenmedi`; }   // başlık şeridinde bilgi alanı yok; varsa doldurur

  // ---- Personel Cari Kartı (çekmece): mevcut kartı düzenler ya da yeni kart açar (kartSira = 0)
  function kartAc(sira) {
    const p = sira ? etkin(bul(sira)) : { sira: 0, cariKodu: '335.' + String((V.personel.reduce((m, x) => Math.max(m, x.sira), 0) + 1)).padStart(3, '0') + ' (kayıtta kesinleşir)', durum: 'Aktif', ozelDurum: '', not: '', primGun: 30, normalGun: 22, haftaSonuGun: 8 };
    if (sira && !p) return; kartSira = sira; $('#pmKartHata').style.display = 'none';
    $('#pmKartBaslik').innerHTML = `<i class="fas fa-id-card me-1"></i>${sira ? esc(p.cariKodu) + ' · ' + esc(p.adSoyad) : 'Yeni Personel Cari Kartı'} ${sira ? `<span class="rt-durum rt-${p.durum === 'İşten Ayrıldı' ? 'hata' : 'ok'}">${esc(p.durum || 'Aktif')}</span>` : ''}`;
    $('#pmKartAksiyon').innerHTML = `<button type="button" class="btn btn-warning btn-sm py-0" id="pmKartKaydet"><i class="fas fa-floppy-disk me-1"></i>${sira ? 'Kaydet' : 'Kartı Aç'}</button>${sira ? `<button type="button" class="btn btn-light btn-sm py-0" id="pmKartSil" style="color:#b91c1c"><i class="fas fa-trash me-1"></i>Sil</button><a class="btn btn-light btn-sm py-0" id="pmKartCari" href="/Rapor120" target="_blank" title="İK › Personel Carileri ekranı"><i class="fas fa-address-book me-1"></i>Personel Carileri</a>` : ''}<button type="button" class="btn btn-light btn-sm py-0" id="pmKartVazgec">Vazgeç</button>`;
    // etiketler id/for ile girişe bağlı; zorunlu alanlarda required + aria-required (label'a tıklayınca odaklanır, ekran okuyucu adı okur)
    const inp = (k, etiket, cls, ek) => `<div class="${cls || ''}"><label class="${ek && ek.z ? 'z' : ''}" for="pmk_${k}">${etiket}</label><input class="form-control" id="pmk_${k}" data-k="${k}" value="${esc(p[k] ?? '')}" ${ek && ek.list ? `list="${ek.list}"` : ''} ${ek && ek.tip ? `type="${ek.tip}"` : ''} ${ek && ek.ph ? `placeholder="${esc(ek.ph)}"` : ''} ${ek && ek.max2 ? `max="${ek.max2}"` : ''} ${ek && ek.z ? 'required aria-required="true"' : ''} maxlength="${ek && ek.max ? ek.max : 120}"></div>`;
    const sec = (k, etiket, l) => `<div><label for="pmk_${k}">${etiket}</label><select class="form-select" id="pmk_${k}" data-k="${k}">${l.map(o => `<option value="${esc(o[0])}" ${(p[k] || '') === o[0] ? 'selected' : ''}>${esc(o[1])}</option>`).join('')}</select></div>`;
    const say1 = (k, ad, gun) => `<div><label for="pmk_${k}">${esc(ad)}</label><input class="form-control" id="pmk_${k}" type="number" min="0" ${gun ? 'max="31" step="0.5"' : 'step="1"'} data-k="${k}" value="${Number(p[k]) || 0}"></div>`;
    const say = s => s.tip === 'rx' ? say1(s.anahtar, s.ad + ' (R)') + say1(s.anahtar + 'X', s.ad + ' (X)') : say1(s.anahtar, s.ad, s.tip === 'gun');   // rx: R ve X ayrı giriş
    const tutarGruplari = V.sutunlar.filter(g => g.sutunlar.some(girdiTipi)).map(g => `<div class="pm-grup-bas">${esc(g.grup)}</div>` + g.sutunlar.filter(girdiTipi).map(say).join('')).join('');
    const bugunIso = new Date().toISOString().slice(0, 10);
    $('#pmKartIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-user me-1"></i>Kimlik ve Görev Bilgileri</span><span class="not">CARİ_MASTER · Personel Carileri (335)</span></div><div class="na-kart-g"><div class="pm-form">
        <div><label>Cari Kodu</label><input class="form-control ro" value="${esc(p.cariKodu)}" readonly></div>${inp('adSoyad', 'Adı-Soyadı', 'g2', { z: true })}
        ${inp('tcKimlik', 'TC Kimlik No', '', { ph: '11 hane', max: 11 })}${inp('sgkIsvereni', 'SGK İşvereni', '', { z: true, list: 'dlSgk' })}${inp('fiiliGorevYeri', 'Fiili Görev Yeri', '', { z: true, list: 'dlYer' })}
        ${inp('odemeSirketi', 'Ödeme Şirketi (Net Ödemeyi Yapan)', '', { list: 'dlSgk', ph: 'boş = SGK işvereni' })}<div></div><div></div>
        ${inp('unvan', 'Unvan', '', { z: true, list: 'dlUnvan' })}${inp('bolum', 'Bölüm', '', { z: true, list: 'dlBolum' })}${inp('gorev', 'Görev', '', { z: true, list: 'dlGorev' })}
        ${inp('iseGiris', 'İşe Giriş Tarihi', '', { tip: 'date' })}${inp('catiDepartman', 'Çatı Departman', '', { z: true, list: 'dlCati' })}${sec('ozelDurum', 'Özel Durumu', OZEL_SECENEK)}
        ${inp('departmanKodu', 'Departman Kodu', '', { max: 20 })}${inp('departmanAciklamasi', 'Departman Açıklaması', 'g2')}
        ${inp('sicilNo', 'Sicil Numarası', '', { max: 30 })}${inp('sgkNo', 'SGK Numarası', '', { max: 30 })}<div></div>
        ${sec('durum', 'Durum', [['Aktif', 'Aktif'], ['İşten Ayrıldı', 'İşten Ayrıldı']])}${inp('ayrilisTarihi', 'Ayrılış Tarihi', '', { tip: 'date', z: true, max2: bugunIso })}<div></div>
        ${inp('telefon', 'Telefon', '', { ph: '05xx xxx xx xx' })}${inp('eposta', 'E-posta', 'g2', { tip: 'email', ph: 'ad.soyad@sirket.com' })}
        <div class="pm-grup-bas">Dövizli Maaş (Varsa) · X-Net = Döviz Tutarı × Kur</div>
        ${sec('doviz', 'Para Birimi', PARA_BIRIMI)}${inp('dovizTutar', 'Döviz Tutarı (Aylık)', '', { tip: 'number', ph: '0' })}${inp('kur', 'Kur (1 Birim = ? ₺)', '', { tip: 'number', ph: '0,0000' })}
        <div class="g3 pm-doviz-not" id="pmKartDoviz"></div>
      </div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-coins me-1"></i>Bordro Kalemleri (Aylık, ₺ · Günler)</span><span class="not">Hesaplanan sütunlar ve toplamlar otomatik</span></div><div class="na-kart-g"><div class="pm-form">${tutarGruplari}</div>
        <div class="pm-ozet-serit" id="pmKartOzet"></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-note-sticky me-1"></i><label for="pmk_not" class="m-0">Not</label></span></div><div class="na-kart-g"><textarea class="form-control" id="pmk_not" data-k="not" rows="2" maxlength="300">${esc(p.not || '')}</textarea></div></div>`;
    kartDegisti = false; kartDoviz(); kartOzet();
    $$('#pmKartIc [data-k]').forEach(e => e.addEventListener('input', () => {
      const k = e.dataset.k; $('#pmKartHata').style.display = 'none';
      if (k === 'doviz' || EK_SAYI.includes(k)) { kartDoviz(); kartOzet(); }   // döviz alanları: X-Net ₺ karşılığı yenilenir (bordroya girmez)
      else if (GIRIS.includes(k) || k === 'ozelDurum') { if (!XALAN.includes(k)) kartDegisti = true; kartOzet(); }
      if (k === 'durum') $('#pmKartIc [data-k=ayrilisTarihi]').disabled = e.value !== 'İşten Ayrıldı';
    }));
    $('#pmKartIc [data-k=ayrilisTarihi]').disabled = (p.durum || 'Aktif') !== 'İşten Ayrıldı';
    $('#pmKartKaydet').addEventListener('click', kartKaydet); $('#pmKartVazgec').addEventListener('click', kartKapat); const s = $('#pmKartSil'); if (s) s.addEventListener('click', () => sil(kartSira));
    const cari = $('#pmKartCari'); if (cari) cari.addEventListener('click', ev => { if (window.top && window.top !== window && typeof window.top.portalSekmeAc === 'function') { ev.preventDefault(); window.top.portalSekmeAc('Personel Carileri', '/Rapor120'); } });
    $('#pmKart').classList.add('acik'); $('#pmPerde').classList.add('acik');
    try { const u = new URL(location.href); if (sira) u.searchParams.set('kod', bul(sira).cariKodu); else u.searchParams.delete('kod'); history.replaceState(null, '', u); } catch (e) { /* yoksay */ }
  }
  function kartOku() { const d = { sira: kartSira || 0, ozel: kartSira ? !!bul(kartSira).ozel : kapsam === 'ozel' }; $$('#pmKartIc [data-k]').forEach(e => { const k = e.dataset.k; d[k] = GIRIS.includes(k) || EK_SAYI.includes(k) ? (Number(e.value) || 0) : e.value.trim(); }); return d; }
  /** kartta dövizli maaş: X-Net alanı ₺ karşılığıyla dolar ve kilitlenir; açıklama satırı yazılır */
  function kartDoviz() {
    const d = kartOku(), x = $('#pmk_xNet'), not = $('#pmKartDoviz'); if (!x) return;
    const dv = dovizli(d); x.readOnly = dv; x.classList.toggle('ro', dv);
    if (dv) { x.value = Math.round(d.dovizTutar * d.kur * 100) / 100; not.innerHTML = `<i class="fas fa-coins me-1"></i>₺ karşılığı: <b>${kisa(d.dovizTutar * d.kur)}</b> (${dovizNot(d)}) → X-Net`; }
    else not.innerHTML = d.doviz ? '<span class="text-danger">Döviz Tutarı ve Kur sıfırdan büyük olmalıdır.</span>' : '<span class="text-muted">Maaş ₺ ödeniyorsa boş bırakın.</span>';
  }
  /** kart özeti: tutarlar değişmediyse kaynak tablo değerleriyle (tablodaki satırla aynı), değiştiyse modelle */
  function kartOzet() { const k = kartSira ? bul(kartSira) : null, p = hesapla({ ...kartOku(), ...(k && !kartDegisti ? { brutKaynak: k.brutKaynak, isverenKaynak: k.isverenKaynak } : {}) }); $('#pmKartOzet').innerHTML = [['Toplam Net', p.toplamNet], ['Toplam Brüt Kazanç', p.toplamBrutKazanc], ['Vergi ve İşçi Payları', p.toplamBrutMasraflar], ['SGK İşveren', p.sgkIsverenToplam], ['Genel Maliyet', p.genelMaliyet]].map(([l, v], i) => `<div class="${i === 4 ? 'genel' : ''}"><span>${l}</span><b>${kisa(v)}</b></div>`).join(''); }
  async function kartKaydet() {
    const d = kartOku(), sira = kartSira, eskiDurum = sira ? (bul(sira).durum || 'Aktif') : null;
    d.donem = V.donem;
    const j = sira ? await post('/PersonelMaliyet/Kaydet', { satirlar: [d], donem: V.donem }) : await post('/PersonelMaliyet/Ekle', d);
    if (!j.success) {
      const h = $('#pmKartHata'); h.textContent = j.message; h.style.display = 'block'; $('#pmKart').scrollTo({ top: 0, behavior: 'smooth' });   // hata yapışkan başlığın altında; kaydırılmış çekmecede görünür kalsın
      const z = $$('#pmKartIc [data-k]').find(e => !e.disabled && !e.value.trim() && e.previousElementSibling && e.previousElementSibling.classList.contains('z')); if (z) z.focus();
      return;
    }
    if (sira) delete taslak[sira];
    const du = $('#pmDurum').value, ek = sira && eskiDurum !== d.durum && du && du !== d.durum ? ` Satır "${esc(du)}" filtresinin dışında kaldı; görmek için Durum = Tümü seçin.` : '';
    kartKapat(); veriGuncelle(j); mesaj('success', esc(j.message) + ek);
    if (j.yeniSira) { const tr = $(`#pmTablo tr[data-sira="${j.yeniSira}"]`); if (tr) tr.scrollIntoView({ block: 'center' }); }
  }
  function kartKapat() { kartSira = null; $('#pmKart').classList.remove('acik'); $('#pmPerde').classList.remove('acik'); try { const u = new URL(location.href); u.searchParams.delete('kod'); history.replaceState(null, '', u); } catch (e) { /* yoksay */ } }

  // ---- excel (tablo): sütun tanımındaki sırayla + cari kodu, durum, ayrılış, kıdem
  function excel() {
    const cols = SUTUN.filter(s => s.tip !== 'kart').flatMap(s => s.tip === 'rx' ? [{ ...s, tip: 'sayi', ad: s.ad + '-R' }, { ...s, tip: 'sayi', ad: s.ad + '-X', anahtar: s.anahtar + 'X' }] : [s]);   // rx sütunu Excel'de R ve X olarak iki sütun
    const bas = ['Cari Kodu', 'Durum', 'Ayrılış Tarihi', 'Kıdem (Yıl)', 'Para Birimi', 'Döviz Tutarı', 'Kur'].concat(cols.map(s => (s.anahtar === 'xNet' || s.anahtar === 'rNet' || s.anahtar === 'toplamNet' || s.anahtar === 'genelMaliyet') ? V.yil + ' ' + s.ad : s.ad));
    const deger = (p, s, i) => s.tip === 'sira' ? i + 1 : s.tip === 'tarih' ? (p[s.anahtar + 'Tr'] || '') : s.tip === 'gun' ? (Number(p[s.anahtar]) || 0) : (s.tip === 'sayi' || s.tip === 'hesap' || s.tip === 'genel') ? sayi(p[s.anahtar]) : (p[s.anahtar] ?? '');
    csvIndir('personel_maliyet_raporu', [bas, ...L.map((p, i) => [p.cariKodu, p.durum, p.ayrilisTr || '', p.kidemYil ?? '', p.doviz || '', dovizli(p) ? sayi(p.dovizTutar) : '', dovizli(p) ? ondalik(p.kur, 4) : ''].concat(cols.map(s => deger(p, s, i))))]);
  }

  // ============================================================
  // EK SEKMELER: Üretim Personeli · Prim Ödemeleri · Özel Kişiler · Birleşik Tablo (sütun tanımı sunucudan: V.ekSutunlar; birleşim istemcide kurulur)
  // ============================================================
  const BIRLESIM_SUTUN = [
    { ad: 'Sıra', anahtar: 'sira', tip: 'sira' }, { ad: 'Kaynak', anahtar: 'kaynak', tip: 'kaynak' }, { ad: 'Adı-Soyadı', anahtar: 'adSoyad', tip: 'ad' }, { ad: 'Görev', anahtar: 'gorev', tip: 'metin' },
    { ad: 'SGK İşvereni / Ödeyen', anahtar: 'isveren', tip: 'metin' }, { ad: 'Görev Yeri', anahtar: 'gorevYeri', tip: 'metin' },
    { ad: 'Net Toplam', anahtar: 'netToplam', tip: 'hesap' }, { ad: 'R-Net', anahtar: 'rNet', tip: 'sayi' }, { ad: 'X-Net', anahtar: 'xNet', tip: 'sayi' }, { ad: 'Yol', anahtar: 'yol', tip: 'sayi' }, { ad: 'Yemek', anahtar: 'yemek', tip: 'sayi' }, { ad: 'Mesai', anahtar: 'mesai', tip: 'sayi' }, { ad: 'Prim', anahtar: 'prim', tip: 'sayi' }, { ad: 'Diğer Ek Ödemeler', anahtar: 'diger', tip: 'sayi', otoGizle: true },   // otoGizle: tamamı sıfırsa sütun gelmez; dolu olduğu dönemde görünür
    { ad: 'Kesintiler', anahtar: 'kesinti', tip: 'sayi' }, { ad: 'Net Ödenen', anahtar: 'netOdenen', tip: 'hesap' }, { ad: 'Toplam Brüt Masraflar', anahtar: 'toplamBrutMasraflar', tip: 'hesap' }, { ad: 'Toplam İşveren', anahtar: 'toplamIsveren', tip: 'hesap' }, { ad: 'Genel Maliyet', anahtar: 'genelMaliyet', tip: 'genel' }, { ad: 'Not', anahtar: 'not', tip: 'not' }
  ];
  const EK = {
    uretim: { tablo: '#pmUretimTablo', sayi: '#pmUretimSayi', sifir: '#pmUretimSifir', ara: '#pmUretimAra', birim: 'Personel', sutun: () => V.ekSutunlar.uretim, liste: () => { const y = $('#pmUretimYer').value; return V.uretim.filter(p => !y || p.fiiliGorevYeri === y); } },
    prim: { tablo: '#pmPrimTablo', sayi: '#pmPrimSayi', ara: '#pmPrimAra', birim: 'Prim Satırı', sutun: () => V.ekSutunlar.prim, liste: () => { const d = $('#pmPrimDonem').value; return V.primler.filter(p => !d || d === 'yil' || p.donem === d); } },
    birlesim: { tablo: '#pmBirlesimTablo', sayi: '#pmBirlesimSayi', sifir: '#pmBirlesimSifir', ara: '#pmBirlesimAra', birim: 'Kişi', sutun: () => BIRLESIM_SUTUN, liste: birlesimListe }
  };
  const ekSayisal = s => ['sayi', 'hesap', 'genel', 'gun'].includes(s.tip);
  /** ek tablo hücresi (salt okunur): oran %, kur 4 hane, döviz tutarı 2 hane; tutarlar ₺ */
  function ekHucre(p, s, satirNo) {
    const k = s.anahtar, v = p[k], dk = `data-k="${k}"`;
    switch (s.tip) {
      case 'sira': return `<td class="kucuk" ${dk}>${satirNo}</td>`;   // listedeki satır numarası (dinamik)
      case 'ad': return `<td class="ad" ${dk}>${adOz(v, p.sira0 || p.personelSira)}</td>`;
      case 'kaynak': return `<td class="kaynak k-${v === 'Üretim' ? 'uretim' : v === 'Özel' ? 'ozel' : 'bordro'}" ${dk}>${esc(v)}</td>`;
      case 'gun': return `<td class="s" ${dk}>${k === 'oran' ? (Number(v) ? '% ' + ondalik(v, 2) : '<span class="text-muted">—</span>') : k === 'kur' ? (Number(v) ? ondalik(v, 4) : '<span class="text-muted">—</span>') : k === 'dovizTutar' ? (Number(v) ? ondalik(v, 2) + ' ' + esc(p.doviz || '') : '<span class="text-muted">—</span>') : gunFmt(v)}</td>`;
      case 'sayi': return `<td class="s" ${dk}>${hucre(v)}</td>`;
      case 'hesap': return `<td class="s hesap" ${dk}>${hucre(v)}</td>`;
      case 'genel': return `<td class="s genel" ${dk}>${kisa(v)}</td>`;
      case 'not': return `<td ${dk}><small>${bos(v)}</small></td>`;
      default: return `<td ${dk}>${bos(v)}</td>`;
    }
  }
  function ekCiz(ad) {
    const E = EK[ad], S0 = E.sutun(), ara = ($(E.ara).value || '').toLocaleLowerCase('tr-TR');
    const metinler = S0.filter(s => ['metin', 'ad', 'not', 'kaynak'].includes(s.tip)).map(s => s.anahtar);
    const L2 = E.liste().filter(p => !ara || metinler.map(k => p[k]).join(' ').toLocaleLowerCase('tr-TR').includes(ara));
    const S = S0.filter(s => !s.otoGizle || L2.some(p => kurus(p[s.anahtar])));   // otoGizle sütunları yalnız dolu olduğunda gelir
    const t = $(E.tablo);
    $('thead tr', t).innerHTML = S.map(s => `<th class="${ekSayisal(s) ? 's' : ''}" data-k="${s.anahtar}">${esc(s.ad)}</th>`).join('');
    $('tbody', t).innerHTML = L2.map((p, i) => `<tr data-id="${p.sira ?? ''}" data-kaynak="${esc(p.kaynak || '')}" data-sira0="${p.sira0 ?? ''}" data-usira="${p.uSira ?? ''}">${S.map(s => ekHucre(p, s, i + 1)).join('')}</tr>`).join('') || `<tr><td colspan="${S.length}" class="na-bos">Kayıt yok.</td></tr>`;
    const top = k => toplam(L2, k), sayilmaz = ['oran', 'kur', 'dovizTutar', 'saatUcreti', 'gunlukSaat', 'gunlukUcret'];
    $('tfoot tr', t).innerHTML = S.map(s => { const dk = `data-k="${s.anahtar}"`; if (s.tip === 'sira') return `<td ${dk}>Toplam</td>`; if (s.tip === 'ad') return `<td ${dk}>${L2.length} ${E.birim}</td>`; if (s.tip === 'gun') return `<td class="s" ${dk}>${sayilmaz.includes(s.anahtar) ? '' : gunFmt(top(s.anahtar))}</td>`; if (s.tip === 'sayi' || s.tip === 'hesap') return `<td class="s" ${dk}>${sayilmaz.includes(s.anahtar) ? '' : kisa(top(s.anahtar))}</td>`; if (s.tip === 'genel') return `<td class="s genel" ${dk}>${kisa(top(s.anahtar))}</td>`; return `<td ${dk}></td>`; }).join('');
    const gizli = E.sifir ? sifirGizle(t, S, L2, $(E.sifir).checked) : 0;
    $(E.sayi).textContent = `${L2.length} ${E.birim.toLocaleLowerCase('tr-TR')} · aylık tutarlar ₺${gizli ? ` · ${gizli} sıfır sütun gizli` : ''}`;
    if (ad === 'prim') primUyum(L2); if (ad === 'birlesim') birlesimKpi(L2);
    E.son = { S, L: L2 }; sabitleHepsi();
  }
  function ekExcel(ad) {
    const E = EK[ad]; if (!E.son) ekCiz(ad); const { S, L: L2 } = E.son;
    const deger = (p, s, i) => s.tip === 'sira' ? i + 1 : (s.tip === 'sayi' || s.tip === 'hesap' || s.tip === 'genel') ? sayi(p[s.anahtar]) : s.tip === 'gun' ? (Number(p[s.anahtar]) || 0) : (p[s.anahtar] ?? '');
    csvIndir('personel_maliyet_' + ad, [S.map(s => s.ad), ...L2.map((p, i) => S.map(s => deger(p, s, i)))]);
  }
  /** prim: dönem listesi (en yeni önce) + yıllık */
  function primDonemleriKur() {
    const sel = $('#pmPrimDonem'), v = sel.value, donemler = [...new Map(V.primler.map(p => [p.donem, p.donemAd])).entries()].sort((a, b) => b[0].localeCompare(a[0]));
    sel.innerHTML = donemler.map(([d, ad]) => `<option value="${esc(d)}">${esc(ad)}</option>`).join('') + `<option value="yil">${V.yil} Yıllık</option>`;
    sel.value = v && [...sel.options].some(o => o.value === v) ? v : (donemler[0] ? donemler[0][0] : 'yil');
  }
  /** kişi bazında dönem primi ↔ Personel Maliyet Tablosu'ndaki Prim (R + X); yıllıkta karşılaştırma yapılmaz */
  function primUyum(L2) {
    const yil = $('#pmPrimDonem').value === 'yil', m = {};
    L2.forEach(p => { const o = m[p.adSoyad] || (m[p.adSoyad] = { ad: p.adSoyad, donem: 0 }); o.donem += Number(p.hesaplananPrim) || 0; });
    const tablo = ad => { const k = V.personel.filter(p => p.adSoyad === ad && (p.durum || 'Aktif') === 'Aktif'); return k.reduce((s, p) => s + (Number(p.prim) || 0) + (Number(p.primX) || 0), 0); };
    const rows = Object.values(m).sort((a, b) => b.donem - a.donem);
    $('#pmPrimUyum tbody').innerHTML = rows.map(o => { const t = tablo(o.ad), f = yil ? null : Math.round((o.donem - t) * 100) / 100; return `<tr data-ad="${esc(o.ad)}"><td class="ad">${adOz(o.ad)}</td><td class="s">${kisa(o.donem)}</td><td class="s hesap">${kisa(t)}</td><td class="s">${yil ? '<span class="text-muted">—</span>' : `<span class="rt-durum rt-${Math.abs(f) < 0.01 ? 'ok' : 'hata'}">${Math.abs(f) < 0.01 ? 'Uyumlu' : kisa(f)}</span>`}</td></tr>`; }).join('') + (rows.length ? `<tr class="toplam"><td>Toplam · ${rows.length} kişi</td><td class="s">${kisa(rows.reduce((s, o) => s + o.donem, 0))}</td><td class="s">${kisa(rows.reduce((s, o) => s + tablo(o.ad), 0))}</td><td></td></tr>` : '<tr><td colspan="4" class="na-bos">Kayıt yok.</td></tr>');
  }
  /** birleşim: Bordro + Özel (aynı bordro modeli; ana sekme filtreleriyle) + Üretim; prim kişinin satırında. Toplam Brüt Masraflar burada vergi + SGK işçi (kesintiler ayrı sütun) */
  const bordroSatir = (p, kaynak) => ({ kaynak, adSoyad: p.adSoyad, gorev: p.gorev, isveren: p.sgkIsvereni, gorevYeri: p.fiiliGorevYeri, kidemYil: p.kidemYil, brut: Number(p.toplamBrutKazanc) || 0, netToplam: (Number(p.rNet) || 0) + (Number(p.xNet) || 0), rNet: p.rNet, xNet: p.xNet,
    yol: (Number(p.yol) || 0) + (Number(p.yolX) || 0), yemek: (Number(p.yemek) || 0) + (Number(p.yemekX) || 0), mesai: (Number(p.toplamEkCalisma) || 0) + (Number(p.toplamEkCalismaX) || 0), prim: (Number(p.toplamPrim) || 0) + (Number(p.toplamPrimX) || 0),
    diger: (Number(p.toplamSosyal) || 0) - (Number(p.yemek) || 0) - (Number(p.yol) || 0) + (Number(p.toplamYanHaklar) || 0) + (Number(p.toplamYasal) || 0) + (Number(p.digerEkOdemelerR) || 0),
    kesinti: (Number(p.kesintilerToplami) || 0) + (Number(p.kesintilerToplamiX) || 0), netOdenen: (Number(p.netRBordro) || 0) + (Number(p.kalanNetElden) || 0),
    toplamBrutMasraflar: (Number(p.toplamBrutMasraflar) || 0) - (Number(p.kesintilerToplami) || 0), toplamIsveren: p.toplamIsveren, genelMaliyet: p.genelMaliyet, not: p.not, sira0: p.sira });
  function birlesimListe(kaynakSecim) {
    const kaynak = kaynakSecim !== undefined ? kaynakSecim : $('#pmBirlesimKaynak').value, rows = [];
    if (!kaynak || kaynak === 'Bordro') liste(false, false).map(etkin).forEach(p => rows.push(bordroSatir(p, 'Bordro')));
    if (!kaynak || kaynak === 'Özel') liste(false, true).map(etkin).forEach(p => rows.push(bordroSatir(p, 'Özel')));
    // SGK'lı gündelikçiler zaten bordro satırında (R + elden X); birleşime yalnız SGK'sız gündelikçiler ayrı satır olarak girer
    // ana filtredeki görev yeri üretim satırlarına da uygulanır
    if (!kaynak || kaynak === 'Üretim') V.uretim.filter(p => !p.sgkLi && yerUyar(p.fiiliGorevYeri)).forEach(p => rows.push({ kaynak: 'Üretim', uSira: p.sira, adSoyad: p.adSoyad, gorev: p.gorev, isveren: 'SGK Dışı', gorevYeri: p.fiiliGorevYeri, netToplam: (Number(p.hakEdis) || 0) + (Number(p.tatilKazanci) || 0), rNet: 0, xNet: (Number(p.hakEdis) || 0) + (Number(p.tatilKazanci) || 0), yol: p.yol, yemek: p.yemek, mesai: p.fazlaMesaiKazanci, prim: 0, diger: p.digerSosyal, kesinti: p.kesinti, netOdenen: p.netOdenen, toplamBrutMasraflar: 0, toplamIsveren: 0, genelMaliyet: p.toplamMaliyet, not: p.not }));
    rows.forEach((r, i) => r.sira = i + 1);
    return rows;
  }
  function birlesimKpi(L2) {
    const n = k => L2.filter(r => r.kaynak === k).length, top = k => toplam(L2, k);
    const kpi = [['#0f172a', 'Kişi', L2.length, `Bordro ${n('Bordro')} · Üretim ${n('Üretim')} · Özel ${n('Özel')}`], ['#0f766e', 'Net Ödenen', kisa(top('netOdenen')), `R-Net ${kisa(top('rNet'))} · X-Net ${kisa(top('xNet'))}`],
      ['#b45309', 'Toplam Brüt Masraflar', kisa(top('toplamBrutMasraflar')), `Kesintiler ${kisa(top('kesinti'))}`], ['#7c3aed', 'Toplam İşveren', kisa(top('toplamIsveren')), 'SGK işveren payı'], ['#dc2626', 'Genel Maliyet', kisa(top('genelMaliyet')), `Ortalama ${kisa(L2.length ? top('genelMaliyet') / L2.length : 0)} / Kişi`]];
    $('#pmBirlesimKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $('#pmNBirlesim').textContent = L2.length;
  }
  function ekKur() {
    $('#pmUretimYer').innerHTML = '<option value="">Tüm Görev Yerleri</option>' + [...new Set(V.uretim.map(p => p.fiiliGorevYeri))].map(y => `<option value="${esc(y)}">${esc(y)}</option>`).join('');
    primDonemleriKur();
    $('#pmNUretim').textContent = V.uretim.length; $('#pmNPrim').textContent = V.primler.length; $('#pmNBirlesim').textContent = birlesimListe().length;
    $(kapsam === 'ozel' ? '#pmNTablo' : '#pmNOzel').textContent = liste(false, kapsam !== 'ozel').length;   // etkin olmayan kapsamın sayacı (etkin olanı kpiCiz yazar)
    $('#pmNEk').textContent = (V.ekCalisma && V.ekCalisma.mesailer || []).length; $('#pmNKes').textContent = V.kesinti ? (V.kesinti.planlar || []).length + (V.kesinti.avanslar || []).length : 0; $('#pmNMh').textContent = (V.muhasebe && V.muhasebe.simulasyonlar || []).length;
  }

  // ============================================================
  // EK ÇALIŞMALAR (mesai talepleri) ve DİĞER KESİNTİLER (şahsi avanslar, kesinti / icra planları) — V.ekCalisma / V.kesinti (Data/PersonelKesintiOrnek.cs), dönem bazında
  // ============================================================
  const rozet = (d, sinif) => `<span class="pm-rz ${sinif || (/(Onaylandı|Tamamlandı|Gerçekleşti|Aktarıldı|Kapandı|Teslim Edildi|ÖDENDİ|Uyumlu)/.test(d) ? 'ok' : /(Bekliyor|Bekleniyor|Planlandı|Planlı|Devam|Açık|Kısmi|Başlayacak)/.test(d) ? 'bekle' : /(Reddedildi|İptal|Geçti|Gerçekleşmedi)/.test(d) ? 'hata' : 'gri')}">${esc(d || '—')}</span>`;
  const kisiTumu = () => liste(false, false).concat(liste(false, true)).map(etkin);   // bordro + özel, ana filtrelerle
  const saatFmt = v => Number(v) ? ondalik(v, 2) : '—';
  function ekCalismaCiz() {
    const E0 = V.ekCalisma || { mesailer: [] }, ara = ($('#pmEkAra').value || '').toLocaleLowerCase('tr-TR'), durum = $('#pmEkDurum').value;
    $('#pmEkBaslik').textContent = `${V.donemAd} Ek Çalışmalar`;
    const kisiler = kisiTumu(), kisiSet = new Set(kisiler.map(p => p.sira)), adSet = new Set(kisiler.map(p => p.adSoyad));
    const E = { ...E0, mesailer: (E0.mesailer || []).filter(m => (m.personelSira ? kisiSet.has(m.personelSira) : adSet.has(m.adSoyad))) };   // üstteki ortak filtrelerle senkron
    const uyar = m => durum === 'onayli' ? m.onaylandi && !m.iptal : durum === 'bekliyor' ? /Bekliyor/.test(m.talepDurumu) && !/Revizyon/.test(m.talepDurumu) : durum === 'puantaj' ? m.puantaj === 'Puantaj Bekliyor' : durum === 'red' ? (/Reddedildi/.test(m.talepDurumu) || m.iptal) : true;
    const M = E.mesailer.filter(m => uyar(m) && (!ara || [m.adSoyad, m.no, m.proje, m.bolum, m.yonetici, m.tur].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    // kişi özeti: tablodaki ek çalışma ↔ puantaja aktarılan talepler
    const K = kisiler.map(p => { const t = E.mesailer.filter(m => m.personelSira === p.sira || m.adSoyad === p.adSoyad); const hes = t.reduce((s, m) => s + (Number(m.puantajaAktarilan) || 0), 0), top = (Number(p.toplamEkCalisma) || 0) + (Number(p.toplamEkCalismaX) || 0);
      return { p, t, hes, top, saat: t.reduce((s, m) => s + (Number(m.fiili) || 0), 0), su: t.length ? t[0].saatUcreti : Math.round(((Number(p.rNet) || 0) + (Number(p.xNet) || 0)) / (V.ekCalisma.aylikSaat || 225) * 100) / 100 }; }).filter(x => x.top > 0 || x.t.length);
    $('#pmEkOzet tbody').innerHTML = K.map((x, i) => { const p = x.p, f = Math.round((x.top - x.hes) * 100) / 100; return `<tr data-sira="${p.sira}"><td class="kucuk">${i + 1}</td><td class="ad">${esc(p.adSoyad)}${ozRozet(p)}</td><td>${esc(p.bolum)}</td><td>${esc(p.sgkIsvereni)}</td><td class="s">${hucre(x.su)}</td><td class="s">${hucre(p.fazlaMesai)}</td><td class="s">${hucre(p.fazlaMesaiX)}</td><td class="s">${hucre(p.bayramCalisma)}</td><td class="s">${hucre(p.geceFarki)}</td><td class="s">${hucre(p.pazarCalisma)}</td><td class="s">${hucre(p.resmiTatilCalisma)}</td><td class="s hesap">${hucre(x.top)}</td><td class="s">${x.t.length || '—'}</td><td class="s">${saatFmt(x.saat)}</td><td class="s hesap">${hucre(x.hes)}</td><td class="s">${Math.abs(f) < 25 ? rozet('Uyumlu') + (Math.abs(f) >= 1 ? `<small class="alt">yuvarlama ${kisa(f)}</small>` : '') : `<span class="${f > 0 ? 'text-danger' : 'text-primary'}">${kisa(f)}</span><small class="alt">${f > 0 ? 'puantaj bekleyen / talepsiz' : 'tabloya girmemiş'}</small>`}</td></tr>`; }).join('') || '<tr><td colspan="16" class="na-bos">Ek çalışması olan personel yok.</td></tr>';
    const topK = k => K.reduce((s, x) => s + (Number(x[k]) || 0), 0), topP = k => K.reduce((s, x) => s + (Number(x.p[k]) || 0), 0);
    $('#pmEkOzet tfoot tr').innerHTML = `<td>Toplam</td><td>${K.length} Personel</td><td></td><td></td><td></td><td class="s">${kisa(topP('fazlaMesai'))}</td><td class="s">${kisa(topP('fazlaMesaiX'))}</td><td class="s">${kisa(topP('bayramCalisma'))}</td><td class="s">${kisa(topP('geceFarki'))}</td><td class="s">${kisa(topP('pazarCalisma'))}</td><td class="s">${kisa(topP('resmiTatilCalisma'))}</td><td class="s">${kisa(topK('top'))}</td><td class="s">${K.reduce((s, x) => s + x.t.length, 0)}</td><td class="s">${saatFmt(topK('saat'))}</td><td class="s">${kisa(topK('hes'))}</td><td class="s">${kisa(topK('top') - topK('hes'))}</td>`;
    // talep satırları
    $('#pmMesaiTablo tbody').innerHTML = M.map(m => `<tr data-id="${m.id}"><td><b>${esc(m.no)}</b></td><td class="ad">${adOz(m.adSoyad, m.personelSira)}</td><td>${esc(m.bolum || '')}</td><td>${esc(m.tur)}</td><td>${esc(m.tarih)}</td><td>${esc(m.aralik)}</td><td class="s">${saatFmt(m.planlanan)}</td><td class="s">${m.fiili === null || m.fiili === undefined ? '<span class="text-muted">—</span>' : saatFmt(m.fiili)}</td><td class="s">×${ondalik(m.carpan, 2)}</td><td class="s">${hucre(m.saatUcreti)}</td><td class="s hesap">${hucre(m.tutar)}</td><td>${esc(m.karsilik)}</td><td>${rozet(m.talepDurumu)}</td><td>${m.yonetici ? `${esc(m.yonetici)}<small class="alt">${esc(m.yoneticiDurum)}${m.yoneticiTarih ? ' · ' + esc(m.yoneticiTarih) : ''}</small>` : '—'}</td><td><small>${bos(m.digerOnaylar)}</small></td><td>${rozet(m.gerceklesme)}${m.fiiliGiris ? `<small class="alt">${esc(m.fiiliGiris)}–${esc(m.fiiliCikis)}</small>` : ''}</td><td>${rozet(m.puantaj)}${m.puantajTarihi ? `<small class="alt">${esc(m.puantajTarihi)}</small>` : ''}</td><td>${esc(m.donem)}</td><td><small>${bos(m.proje)}</small></td><td class="kucuk">${esc(m.talepTarihi)}<small class="alt">${esc(m.olusturan || '')}</small></td></tr>`).join('') || '<tr><td colspan="20" class="na-bos">Filtreye uyan mesai talebi yok.</td></tr>';
    const topM = k => M.reduce((s, m) => s + (Number(m[k]) || 0), 0);
    $('#pmMesaiTablo tfoot tr').innerHTML = `<td>Toplam</td><td>${M.length} Talep</td><td></td><td></td><td></td><td></td><td class="s">${saatFmt(topM('planlanan'))}</td><td class="s">${saatFmt(topM('fiili'))}</td><td></td><td></td><td class="s">${kisa(topM('tutar'))}</td>${'<td></td>'.repeat(9)}`;
    const onayli = E.mesailer.filter(m => m.onaylandi && !m.iptal), bekleyen = E.mesailer.filter(m => /Bekliyor/.test(m.talepDurumu) && !/Revizyon/.test(m.talepDurumu));
    const kpi = [['#6366f1', 'Mesai Talebi', E.mesailer.length, `${onayli.length} onaylı · ${bekleyen.length} onay bekliyor · ${E.mesailer.filter(m => m.puantaj === 'Puantaj Bekliyor').length} puantaj bekliyor`], ['#0ea5e9', 'Fiili Saat', ondalik(E.mesailer.reduce((s, m) => s + (Number(m.fiili) || 0), 0), 1) + ' sa', `planlanan ${ondalik(E.mesailer.reduce((s, m) => s + (Number(m.planlanan) || 0), 0), 1)} sa`],
      ['#0f766e', 'Taleplerden Hesaplanan', kisa(E.mesailer.reduce((s, m) => s + (Number(m.tutar) || 0), 0)), `puantaja aktarılan ${kisa(E.mesailer.reduce((s, m) => s + (Number(m.puantajaAktarilan) || 0), 0))}`], ['#b45309', 'Tablodaki Ek Çalışma (R+X)', kisa(topK('top')), `${K.length} personel · fark ${kisa(topK('top') - topK('hes'))}`],
      ['#dc2626', 'Reddedilen / İptal', E.mesailer.filter(m => /Reddedildi/.test(m.talepDurumu) || m.iptal).length, 'talep']];
    $('#pmEkKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $('#pmEkSayi').textContent = `${M.length} / ${E.mesailer.length} mesai talebi · ${V.donemAd} · tutarlar ₺ · satıra tıklayınca onay adımları ve zaman çizelgesi`;
    $('#pmNEk').textContent = E.mesailer.length; sabitleHepsi();
  }
  function kesintiCiz() {
    const E0 = V.kesinti || { avanslar: [], planlar: [], turler: [] }, ara = ($('#pmKesAra').value || '').toLocaleLowerCase('tr-TR'), tur = $('#pmKesTur').value;
    $('#pmKesBaslik').textContent = `${V.donemAd} Diğer Kesintiler`;
    const kisiler = kisiTumu(), kisiSet = new Set(kisiler.map(p => p.sira));
    const E = { ...E0, avanslar: (E0.avanslar || []).filter(a => !a.personelSira || kisiSet.has(a.personelSira)), planlar: (E0.planlar || []).filter(k => kisiSet.has(k.personelSira)) };   // üstteki ortak filtrelerle senkron
    const ts = $('#pmKesTur'); if (ts.options.length <= 1) ts.innerHTML = '<option value="">Tüm Türler</option>' + E.turler.map(t => `<option value="${esc(t)}">${esc(t)}</option>`).join('');
    const A = E.avanslar.filter(a => (!tur || tur === 'Avans Kesintisi') && (!ara || [a.adSoyad, a.no, a.konu, a.sirket].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    const P = E.planlar.filter(k => (!tur || k.tur === tur) && (!ara || [k.adSoyad, k.kaynak, k.dosyaNo, k.alacakli, k.icraDairesi].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    // kişi özeti
    const K = kisiler.map(p => { const pl = E.planlar.filter(k => k.personelSira === p.sira && k.aktif), ac = E.avanslar.filter(a => a.personelSira === p.sira).reduce((s, a) => s + (Number(a.acikTutar) || 0), 0); const bit = E.planlar.filter(k => k.personelSira === p.sira && k.durum !== 'Tamamlandı').map(k => k.bitisDonem).sort().pop();
      return { p, pl, ac, bit, bitAd: bit ? (E.planlar.find(k => k.bitisDonem === bit && k.personelSira === p.sira) || {}).bitisAd : '' }; }).filter(x => (Number(x.p.kesintilerToplami) || 0) + (Number(x.p.kesintilerToplamiX) || 0) > 0 || x.pl.length || x.ac > 0);
    $('#pmKesOzet tbody').innerHTML = K.map((x, i) => { const p = x.p; return `<tr data-sira="${p.sira}"><td class="kucuk">${i + 1}</td><td class="ad">${esc(p.adSoyad)}${ozRozet(p)}</td><td>${esc(p.bolum)}</td><td>${esc(p.sgkIsvereni)}</td><td class="s">${hucre(p.avans)}</td><td class="s">${hucre(p.icra)}</td><td class="s">${hucre(p.borc)}</td><td class="s">${hucre(p.ozelSigortaKesintisi)}</td><td class="s">${hucre(p.sendikaKatkisi)}</td><td class="s">${hucre(p.bes)}</td><td class="s hesap">${hucre(p.kesintilerToplami)}</td><td class="s hesap">${hucre(p.kesintilerToplamiX)}</td><td class="s">${hucre(p.kalanNetBanka)}</td><td class="s">${hucre(x.ac)}</td><td class="s">${x.pl.length || '—'}</td><td>${x.bit ? `${esc(x.bitAd)} <small class="alt">${esc(x.bit)}</small>` : '<span class="text-muted">—</span>'}</td></tr>`; }).join('') || '<tr><td colspan="16" class="na-bos">Kesintisi olan personel yok.</td></tr>';
    const topP = k => K.reduce((s, x) => s + (Number(x.p[k]) || 0), 0);
    $('#pmKesOzet tfoot tr').innerHTML = `<td>Toplam</td><td>${K.length} Personel</td><td></td><td></td><td class="s">${kisa(topP('avans'))}</td><td class="s">${kisa(topP('icra'))}</td><td class="s">${kisa(topP('borc'))}</td><td class="s">${kisa(topP('ozelSigortaKesintisi'))}</td><td class="s">${kisa(topP('sendikaKatkisi'))}</td><td class="s">${kisa(topP('bes'))}</td><td class="s">${kisa(topP('kesintilerToplami'))}</td><td class="s">${kisa(topP('kesintilerToplamiX'))}</td><td class="s">${kisa(topP('kalanNetBanka'))}</td><td class="s">${kisa(K.reduce((s, x) => s + x.ac, 0))}</td><td class="s">${K.reduce((s, x) => s + x.pl.length, 0)}</td><td></td>`;
    // avans talepleri
    const onayH = o => o ? `${esc(o.kisi)}<small class="alt">${esc(o.durum)}${o.tarih ? ' · ' + esc(o.tarih) : ''}</small>` : '<span class="text-muted">—</span>';
    $('#pmAvansTablo tbody').innerHTML = A.map(a => { const od = a.odemeler[0]; return `<tr data-id="${a.id}"><td><b>${esc(a.no)}</b><small class="alt">${esc(a.talepTuru)}</small></td><td class="ad">${adOz(a.adSoyad, a.personelSira)}</td><td>${esc(a.sirket)}</td><td><small>${esc(a.konu)}</small></td><td class="s">${kisa(a.tutar)}</td><td class="s hesap">${hucre(a.onaylananTutar)}</td><td class="kucuk">${esc(a.tarih)}</td><td>${onayH(a.yonetici)}</td><td>${a.ustYonetim ? onayH(a.ustYonetim) : '<span class="text-muted">Gerekmedi</span>'}</td><td>${onayH(a.finans)}</td><td>${rozet(a.talepDurumu)}</td><td>${od ? `${esc(od.tarih)}<small class="alt">${esc(od.kasa)} · ${esc(od.fisNo)} · ${esc(od.teslimSekli)} · teslim alan ${esc(od.teslimAlan)}</small>` : rozet(a.odemeDurumu)}</td><td class="s">${hucre(a.teslimEdilen)}</td><td class="s">${hucre(a.acikTutar)}</td><td>${rozet(a.kapatmaDurumu)}${a.gecikenGun ? `<small class="alt">${a.gecikenGun} gün gecikti</small>` : ''}</td><td>${a.nakitAkis ? `${rozet(a.nakitAkis.durum)}<small class="alt">Rapor79 #${a.nakitAkis.docNum} · ${esc(a.nakitAkis.sirket)} · ${esc(a.nakitAkis.odenme || a.nakitAkis.planlanan || '')}${a.nakitAkis.makbuz ? ' · ' + esc(a.nakitAkis.makbuz) : ''}</small>` : rozet('Senkron Bekliyor', 'gri')}</td><td>${a.plan ? `${kisa(a.plan.aylikTaksit)} × ${a.plan.taksitSayisi}<small class="alt">ödenen ${a.plan.odenenTaksit} · kalan ${kisa(a.plan.kalan)} · bu dönem ${kisa(a.plan.donemTaksiti)}</small>` : '<span class="text-muted">—</span>'}</td><td>${a.plan ? `${esc(a.plan.bitisAd)}<small class="alt">${rozet(a.plan.durum).replace('pm-rz', 'pm-rz')}</small>` : '—'}</td></tr>`; }).join('') || '<tr><td colspan="18" class="na-bos">Şahsi avans talebi yok.</td></tr>';
    const topA = k => A.reduce((s, a) => s + (Number(a[k]) || 0), 0);
    $('#pmAvansTablo tfoot tr').innerHTML = `<td>Toplam</td><td>${A.length} Talep</td><td></td><td></td><td class="s">${kisa(topA('tutar'))}</td><td class="s">${kisa(topA('onaylananTutar'))}</td>${'<td></td>'.repeat(6)}<td class="s">${kisa(topA('teslimEdilen'))}</td><td class="s">${kisa(topA('acikTutar'))}</td><td></td><td></td><td></td><td></td>`;
    // planlar
    $('#pmPlanTablo tbody').innerHTML = P.map(k => `<tr data-id="${k.id}"><td>${rozet(k.tur, k.tur === 'İcra Kesintisi' ? 'hata' : k.tur === 'Avans Kesintisi' ? 'mavi' : 'gri')}</td><td class="ad">${adOz(k.adSoyad, k.personelSira)}</td><td>${esc(k.kaynak)}${k.dosyaNo && k.dosyaNo !== k.kaynak ? `<small class="alt">${esc(k.dosyaNo)}</small>` : ''}</td><td><small>${bos(k.icraDairesi)}</small></td><td><small>${bos(k.alacakli)}</small></td><td class="s">${kisa(k.toplamTutar)}</td><td class="s">${kisa(k.aylikTaksit)}</td><td class="s">${k.odenenTaksit} / ${k.taksitSayisi}</td><td class="s">${hucre(k.odenen)}</td><td class="s hesap">${hucre(k.kalan)}</td><td>${esc(k.baslangicAd)}</td><td><b>${esc(k.bitisAd)}</b><small class="alt">${k.kalanTaksit} taksit kaldı</small></td><td class="s ${k.aktif ? 'hesap' : ''}">${hucre(k.donemTaksiti)}</td><td>${rozet(k.durum)}</td><td>${k.ekler.map(e => `<a class="ek" href="/PersonelMaliyet/KesintiEk?id=${k.id}&no=${e.no}" target="_blank" title="${esc(e.yukleyen)} · ${esc(e.tarih)} · ${esc(e.boyut)}"><i class="fas fa-file-pdf me-1"></i>${esc(e.ad)}</a>`).join('')}<label class="btn btn-light btn-sm ekyukle" title="PDF / belge ekle"><i class="fas fa-paperclip"></i> Ekle<input type="file" accept=".pdf,image/*,.doc,.docx" data-id="${k.id}" class="pmEkDosya" hidden></label></td><td><small>${bos(k.aciklama)}</small></td><td class="kucuk">${esc(k.olusturan || '')}<small class="alt">${esc(k.olusturma)}</small></td></tr>`).join('') || '<tr><td colspan="17" class="na-bos">Kesinti planı yok.</td></tr>';
    const topPl = k => P.reduce((s, x) => s + (Number(x[k]) || 0), 0);
    $('#pmPlanTablo tfoot tr').innerHTML = `<td>Toplam</td><td>${P.length} Plan</td><td></td><td></td><td></td><td class="s">${kisa(topPl('toplamTutar'))}</td><td class="s">${kisa(topPl('aylikTaksit'))}</td><td></td><td class="s">${kisa(topPl('odenen'))}</td><td class="s">${kisa(topPl('kalan'))}</td><td></td><td></td><td class="s">${kisa(topPl('donemTaksiti'))}</td><td></td><td></td><td></td><td></td>`;
    $$('#pmPlanTablo .pmEkDosya').forEach(i => i.addEventListener('change', () => { if (i.files[0]) ekYukle(+i.dataset.id, i.files[0]); }));
    const tumP = E.planlar, acik = E.avanslar.reduce((s, a) => s + (Number(a.acikTutar) || 0), 0);
    const kpi = [['#1d4ed8', 'Avans Kesintisi', kisa(topP('avans')), `${E.avanslar.length} şahsi avans · açık ${kisa(acik)}`], ['#dc2626', 'İcra Kesintisi', kisa(topP('icra')), `${tumP.filter(k => k.tur === 'İcra Kesintisi').length} icra dosyası`], ['#b45309', 'Borç Kesintisi', kisa(topP('borc')), `${tumP.filter(k => k.tur === 'Borç Kesintisi').length} plan`],
      ['#7c3aed', 'Diğer (BES · Sendika · Özel Sigorta)', kisa(topP('bes') + topP('sendikaKatkisi') + topP('ozelSigortaKesintisi')), 'bordro kesintileri'], ['#0f766e', 'Kesintiler Toplamı', kisa(topP('kesintilerToplami') + topP('kesintilerToplamiX')), `R ${kisa(topP('kesintilerToplami'))} · X ${kisa(topP('kesintilerToplamiX'))}`], ['#0f172a', 'Plan Kalan Bakiye', kisa(tumP.reduce((s, k) => s + (Number(k.kalan) || 0), 0)), `${tumP.filter(k => k.durum === 'Devam Ediyor').length} devam eden plan`]];
    $('#pmKesKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $('#pmKesSayi').textContent = `${K.length} personel · ${A.length} avans talebi · ${P.length} kesinti planı · ${V.donemAd} · tutarlar ₺`;
    $('#pmNKes').textContent = E.planlar.length + E.avanslar.length;
    // form: personel listesi ve türler
    const ps = $('#pmkf_personel'), pv = ps.value; ps.innerHTML = '<option value="">Seçin…</option>' + V.personel.filter(p => (p.durum || 'Aktif') === 'Aktif').sort((a, b) => a.adSoyad.localeCompare(b.adSoyad, 'tr')).map(p => `<option value="${p.sira}">${esc(p.adSoyad)} · ${esc(p.cariKodu)}${p.ozel ? ' · özel' : ''}</option>`).join(''); ps.value = pv;
    const tsel = $('#pmkf_tur'); if (!tsel.options.length) tsel.innerHTML = E.turler.map(t => `<option value="${esc(t)}">${esc(t)}</option>`).join('');
    if (!$('#pmkf_baslangic').value) $('#pmkf_baslangic').value = V.donem;
    sabitleHepsi();
  }
  function kesintiOzet() { const t = Number($('#pmkf_toplam').value) || 0, k = Number($('#pmkf_taksit').value) || 0, b = $('#pmkf_baslangic').value; const n = t > 0 && k > 0 ? Math.ceil(t / Math.min(k, t)) : 0; let bit = ''; if (n && b) { const y = +b.slice(0, 4), m = +b.slice(5, 7) - 1 + n - 1; bit = `${y + Math.floor(m / 12)}-${String(m % 12 + 1).padStart(2, '0')}`; } $('#pmkf_ozet').value = n ? `${n} taksit · bitiş ${bit}` : ''; }
  async function kesintiFormGonder(ev) {
    ev.preventDefault(); const f = $('#pmKesFormu'); if (!f.reportValidity()) return;
    const fd = new FormData(f); fd.set('donem', V.donem);
    try { const r = await fetch('/PersonelMaliyet/KesintiEkle', { method: 'POST', body: fd }); const j = await r.json(); if (!j.success) { mesaj('danger', esc(j.message)); return; } f.reset(); $('#pmkf_ozet').value = ''; $('#pmKesForm').style.display = 'none'; veriGuncelle(j); mesaj('success', esc(j.message)); }
    catch (e) { mesaj('danger', 'Kayıt başarısız: ' + esc(e.message)); }
  }
  async function ekYukle(id, dosya) {
    const fd = new FormData(); fd.set('id', id); fd.set('donem', V.donem); fd.set('dosya', dosya);
    try { const r = await fetch('/PersonelMaliyet/KesintiEkYukle', { method: 'POST', body: fd }); const j = await r.json(); if (!j.success) { mesaj('danger', esc(j.message)); return; } veriGuncelle(j); mesaj('success', esc(j.message)); }
    catch (e) { mesaj('danger', 'Dosya yüklenemedi: ' + esc(e.message)); }
  }
  /** talep çekmecesi: mesai (tip m) ya da avans (tip a) — özet, onay adımları, ödemeler, nakit akış, plan, zaman çizelgesi */
  function talepKartAc(tip, id) {
    const t = tip === 'm' ? (V.ekCalisma.mesailer || []).find(x => x.id === id) : (V.kesinti.avanslar || []).find(x => x.id === id); if (!t) return;
    const g = (l, v) => `<div><span>${l}</span><b>${v === undefined || v === null || v === '' ? '—' : v}</b></div>`;
    const onay = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-route me-1"></i>Onay Adımları</span><span class="not">kim · ne zaman · durum</span></div><div class="na-kart-g p-0"><table class="pm-onay" data-dinamik="kapali"><thead><tr><th>#</th><th>Adım</th><th>Kişi</th><th>Durum</th><th>Tarih</th><th>Not</th></tr></thead><tbody>${t.onaylar.map(o => `<tr><td>${o.sira}</td><td>${esc(o.adim)}</td><td><b>${esc(o.kisi)}</b></td><td>${rozet(o.durum)}</td><td>${esc(o.tarih || '—')}</td><td><small>${bos(o.not)}</small></td></tr>`).join('')}</tbody></table></div></div>`;
    const zaman = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-timeline me-1"></i>Zaman Çizelgesi</span><span class="not">portal · workflow · PDKS / bordro · finans / kasa</span></div><div class="na-kart-g"><ul class="pm-zaman">${t.zaman.map(z => `<li>${esc(z.hareket)}<small>${esc(z.tarih)} · ${esc(z.kaynak)}</small></li>`).join('')}</ul></div></div>`;
    let ozet;
    if (tip === 'm') {
      $('#pmTalepBaslik').innerHTML = `<i class="fas fa-business-time me-1"></i>${esc(t.no)} · ${esc(t.adSoyad)} ${rozet(t.talepDurumu)}`;
      $('#pmTalepAksiyon').innerHTML = `${t.personelSira ? `<button type="button" class="btn btn-light btn-sm py-0 pmTalepKisi" data-sira="${t.personelSira}"><i class="fas fa-id-card me-1"></i>Personel Kartı</button>` : ''}<a class="btn btn-light btn-sm py-0" href="/IkTalep/Mesai" target="_blank"><i class="fas fa-up-right-from-square me-1"></i>Mesai Talebi Ekranı (Düzenle)</a>`;
      ozet = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock me-1"></i>Mesai Bilgileri</span><span class="not">${esc(t.tur)}</span></div><div class="na-kart-g"><div class="pm-ozet-grid">${g('Mesai Tarihi', esc(t.tarih))}${g('Saat', esc(t.aralik) + (t.mola && t.mola !== '0' ? ' · mola ' + esc(t.mola) + ' dk' : ''))}${g('Planlanan / Fiili', saatFmt(t.planlanan) + ' / ' + (t.fiili === null ? '—' : saatFmt(t.fiili)) + ' sa')}${g('PDKS Giriş – Çıkış', t.fiiliGiris ? esc(t.fiiliGiris) + ' – ' + esc(t.fiiliCikis) : '')}${g('Çarpan · Saat Ücreti', '×' + ondalik(t.carpan, 2) + ' · ' + kisa(t.saatUcreti))}${g('Tutar', kisa(t.tutar))}${g('Karşılık', esc(t.karsilik) + (t.serbestZaman !== '—' ? ' (' + esc(t.serbestZaman) + ' sa izin)' : ''))}${g('Gerçekleşme (PDKS)', rozet(t.gerceklesme))}${g('Puantaj', rozet(t.puantaj) + (t.puantajDonemi ? ' ' + esc(t.puantajDonemi) : ''))}${g('İş / Proje', esc(t.proje))}${g('Servis · Yemek', (t.servis ? 'Servis' : '') + (t.servis && t.yemek ? ' · ' : '') + (t.yemek ? 'Yemek' : ''))}${g('Bu Yıl Onaylı Mesai', esc(t.yillikToplam) + ' sa · 270 sınırına kalan ' + esc(t.kalanSinir))}${g('Talep', esc(t.talepTarihi) + ' · ' + esc(t.olusturan || ''))}${g('Bölüm · İşveren', esc(t.bolum || '') + ' · ' + esc(t.sgkIsvereni || ''))}${g('Onay Tarihi', esc(t.onayTarihi))}</div><div class="pm-not">${esc(t.gerekce || '')}${t.uyarilar && t.uyarilar.length ? '<br><b>Uyarılar:</b> ' + t.uyarilar.map(esc).join(' · ') : ''}</div></div></div>`;
    } else {
      $('#pmTalepBaslik').innerHTML = `<i class="fas fa-money-bill-transfer me-1"></i>${esc(t.no)} · ${esc(t.adSoyad)} ${rozet(t.talepDurumu)}`;
      $('#pmTalepAksiyon').innerHTML = `${t.personelSira ? `<button type="button" class="btn btn-light btn-sm py-0 pmTalepKisi" data-sira="${t.personelSira}"><i class="fas fa-id-card me-1"></i>Personel Kartı</button>` : ''}<a class="btn btn-light btn-sm py-0" href="/IkTalep/Avans" target="_blank"><i class="fas fa-up-right-from-square me-1"></i>Avans Talebi Ekranı (Düzenle)</a><a class="btn btn-light btn-sm py-0" href="/Rapor79" target="_blank"><i class="fas fa-vault me-1"></i>Kasa Nakit Akışı</a>`;
      const od = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-vault me-1"></i>Kasa Ödemeleri</span><span class="not">Finans / Kasa olayı (CashPaymentCreated)</span></div><div class="na-kart-g p-0"><table class="pm-onay" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Tutar</th><th>Kasa</th><th>Fiş</th><th>Teslim Eden</th><th>Teslim Alan</th><th>Şekil</th><th>Teyit</th></tr></thead><tbody>${t.odemeler.map(o => `<tr><td>${esc(o.tarih)}</td><td>${kisa(o.tutar)}</td><td>${esc(o.kasa)}</td><td>${esc(o.fisNo)}</td><td>${esc(o.teslimEden)}</td><td>${esc(o.teslimAlan)}</td><td>${esc(o.teslimSekli)}</td><td>${o.teslimAlanOnayi ? rozet('Teslim Edildi') : rozet('Teyit Bekliyor')}</td></tr>`).join('') || '<tr><td colspan="8" class="na-bos">Ödeme yok.</td></tr>'}</tbody></table></div></div>`;
      const na = t.nakitAkis ? `${g('Kasa Nakit Akışı', 'Rapor79 #' + t.nakitAkis.docNum + ' · ' + esc(t.nakitAkis.sirket))}${g('Nakit Akış Durumu', rozet(t.nakitAkis.durum) + ' ' + esc(t.nakitAkis.odenme || t.nakitAkis.planlanan || ''))}${g('Makbuz · Kasa', esc(t.nakitAkis.makbuz || '—') + ' · ' + esc(t.nakitAkis.kasa || ''))}` : g('Kasa Nakit Akışı', rozet('Senkron Bekliyor', 'gri'));
      const pl = t.plan ? `${g('Mahsup Planı', kisa(t.plan.aylikTaksit) + ' × ' + t.plan.taksitSayisi + ' taksit')}${g('Ödenen / Kalan', t.plan.odenenTaksit + ' taksit · kalan ' + kisa(t.plan.kalan))}${g('Başlangıç → Bitiş', esc(t.plan.baslangicAd) + ' → ' + esc(t.plan.bitisAd) + ' ' + rozet(t.plan.durum))}` : g('Mahsup Planı', '—');
      ozet = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-hand-holding-dollar me-1"></i>Avans Bilgileri</span><span class="not">${esc(t.talepTuru)}</span></div><div class="na-kart-g"><div class="pm-ozet-grid">${g('Konu', esc(t.konu))}${g('Tutar / Onaylanan', kisa(t.tutar) + ' / ' + kisa(t.onaylananTutar))}${g('Talep Tarihi', esc(t.tarih))}${g('Kullanım · Beklenen Kapatma', esc(t.kullanimTarihi) + ' · ' + esc(t.kapatmaTarihi))}${g('Teslim Edilen / Açık', kisa(t.teslimEdilen) + ' / ' + kisa(t.acikTutar))}${g('Ödeme · Kapatma Durumu', rozet(t.odemeDurumu) + ' ' + rozet(t.kapatmaDurumu))}${na}${pl}${g('Şirket · Bölüm', esc(t.sirket) + ' · ' + esc(t.bolum || ''))}</div><div class="pm-not">${esc(t.aciklama || '')}</div></div></div>` + od;
    }
    $('#pmTalepIc').innerHTML = ozet + onay + zaman;
    $$('#pmTalepAksiyon .pmTalepKisi').forEach(b => b.addEventListener('click', () => { const p = bul(+b.dataset.sira); if (!p) return; talepKartKapat(); if (!!p.ozel !== (kapsam === 'ozel')) kapsam = p.ozel ? 'ozel' : 'bordro'; kartAc(p.sira); }));
    $('#pmTalepKart').classList.add('acik'); $('#pmTalepPerde').classList.add('acik');
  }
  function talepKartKapat() { $('#pmTalepKart').classList.remove('acik'); $('#pmTalepPerde').classList.remove('acik'); }
  // ============================================================
  // MUHASEBE KAYITLARI — simülasyon → kontrol → onay → muhasebeleştirme → ters kayıt; hesap eşleştirme (V.muhasebe, Data/PersonelMuhasebeOrnek.cs)
  // ============================================================
  let mhSecili = null, mhHata = false, mhFisF = '', mhAra = '', mhEslemeAcik = false, mhDetay = false;   // mhDetay: personel bazlı satırlar (gizlilik: varsayılan kapalı, özet görünüm)
  const MH_AKIS = ['Taslak', 'Simülasyon Oluşturuldu', 'Kontrol / Analiz', 'Onaya Gönderildi', 'Onaylandı', 'Muhasebeleştirildi', 'Ters Kayıt'];
  const mhSim = () => { const M = V.muhasebe || { simulasyonlar: [] }; return M.simulasyonlar.find(s => s.id === mhSecili) || M.simulasyonlar.find(s => s.id === M.seciliId) || M.simulasyonlar[0] || null; };
  function muhasebeCiz() {
    const M = V.muhasebe || { simulasyonlar: [], eslemeler: [], hesaplar: [], sirketler: [], islemTurleri: [], kaynakOzet: {} }; const K = M.kaynakOzet || {};
    $('#pmMhBaslik').textContent = `${V.donemAd} Muhasebe Kayıtları`;
    const merkez = $('#pmMhMerkez'); if (merkez) merkez.href = '/PersonelMuhasebe?donem=' + encodeURIComponent(V.donem);   // ayrıntılı ekran (aynı veri)
    const s = mhSim(); mhSecili = s ? s.id : null; mhDetay = $('#pmMhDetay').checked;
    // akış şeridi
    const adim = !s ? 0 : s.durum === 'Simülasyon Oluşturuldu' ? 1 : (s.durum === 'Kontrol Gerekiyor' || s.durum === 'Kontrole Hazır' || s.durum === 'Güncelliğini Yitirdi') ? 2 : s.durum === 'Onaya Gönderildi' ? 3 : s.durum === 'Onaylandı' ? 4 : s.durum === 'Muhasebeleştirildi' ? 5 : s.durum === 'Ters Kayıt Yapıldı' ? 6 : 0;
    $('#pmMhAkis').innerHTML = MH_AKIS.map((a, i) => `<span class="${i < adim ? 'oldu' : i === adim ? (s && (s.durum === 'Kontrol Gerekiyor' || s.durum === 'Güncelliğini Yitirdi') && i === 2 ? 'hata' : 'simdi') : ''}">${i + 1}. ${a}${s && i === adim ? ' · ' + esc(s.durum) : ''}</span>`).join('') + (s && s.durum === 'İptal' ? '<span class="hata">İptal</span>' : '');
    // üst özet kartı (IT analiz §6)
    const kpi = s ? [['#0f172a', 'Personel Sayısı', s.personelSayisi, `${s.no} v${s.versiyon} · ${esc(s.kapsamAd)}`], ['#dc2626', 'Genel Maliyet', kisa(s.genelMaliyet), 'toplam personel maliyeti (kontrol toplamı)'], ['#0f766e', 'Ara Toplam', kisa(s.araToplam), 'fiili net ödeme (Net-X + Net-R)'],
      ['#7c3aed', 'Yansıtma Gereken', kisa(s.yansitmaGereken), 'SGK işvereni ≠ fiili görev yeri'], ['#1d4ed8', 'Başka Şirket Tarafından Ödenen', kisa(s.baskaSirketOdeme), 'SGK işvereni ≠ ödeme şirketi'], ['#b45309', 'Toplam Borç / Alacak', kisa(s.toplamBorc), `alacak ${kisa(s.toplamAlacak)} · ${s.fisSayisi} fiş · ${s.satirSayisi} satır`],
      [s.dengesizFis ? '#dc2626' : '#16a34a', 'Dengesiz Fiş', s.dengesizFis, 'borç ≠ alacak'], [s.hataliSatir ? '#dc2626' : '#16a34a', 'Hatalı Hesap / Eksik Eşleştirme', s.hataliSatir, 'kayıt engelleyen satır'], [s.mukerrerRisk ? '#dc2626' : '#16a34a', 'Mükerrer Risk', s.mukerrerRisk, 'aynı dönem muhasebeleştirilmiş']]
      : [['#0f172a', 'Personel Sayısı', K.personel || 0, 'kaynak: Personel Maliyet Tablosu (aktif)'], ['#dc2626', 'Genel Maliyet', kisa(K.genelMaliyet), 'toplam personel maliyeti'], ['#0f766e', 'Ara Toplam', kisa(K.araToplam), 'fiili net ödeme'], ['#7c3aed', 'Yansıtma Gereken', kisa(K.yansitma), 'SGK işvereni ≠ fiili görev yeri'], ['#1d4ed8', 'Başka Şirket Tarafından Ödenen', kisa(K.baskaSirket), 'SGK işvereni ≠ ödeme şirketi'], ['#b45309', 'Ödeme Yapılmıyor Notu', K.odemeYok || 0, 'ödeme kaydı üretilmeyecek personel']];
    $('#pmMhKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${k[3]}</div></div>`).join('');
    // simülasyon listesi
    $('#pmMhSimTablo tbody').innerHTML = M.simulasyonlar.map(x => `<tr data-id="${x.id}" class="${s && x.id === s.id ? 'secili' : ''}"><td><b>${esc(x.no)}</b></td><td>v${x.versiyon}</td><td>${esc(x.donemAd)}</td><td>${esc(x.kapsamAd)}</td><td class="s">${x.personelSayisi}</td><td class="s">${x.fisSayisi}</td><td class="s">${x.satirSayisi}</td><td class="s">${kisa(x.toplamBorc)}</td><td class="s">${kisa(x.toplamAlacak)}</td><td>${rozet(x.durum, x.durum === 'Kontrol Gerekiyor' || x.durum === 'Güncelliğini Yitirdi' ? 'hata' : x.durum === 'İptal' ? 'gri' : undefined)}</td><td>${esc(x.olusturan)}<small class="alt">${esc(x.olusturma)}</small></td><td>${x.onayaGonderen ? esc(x.onayaGonderen) + `<small class="alt">${esc(x.onayaGonderme)}</small>` : '—'}</td><td>${x.onaylayan ? esc(x.onaylayan) + `<small class="alt">${esc(x.onayTarihi)}</small>` : '—'}</td><td>${x.muhasebelestiren ? esc(x.muhasebelestiren) + `<small class="alt">${esc(x.muhasebeTarihi)}</small>` : '—'}</td><td>${x.guncel ? rozet('Güncel') : rozet('Veri Değişti', 'hata')}</td></tr>`).join('') || `<tr><td colspan="15" class="na-bos">Bu dönem için simülasyon yok (kapsamda aktif personel bulunamadı).</td></tr>`;
    mhSeciliCiz(s); mhEslemeCiz(M);
    $('#pmMhSayi').textContent = `${M.simulasyonlar.length} simülasyon · ${V.donemAd} · ${M.eslemeler.length} hesap eşleştirmesi (v${M.eslemeVersiyonu}) · kullanıcı ${esc(M.kullanici)} (${M.yetkili ? 'onay yetkili' : 'onay yetkisi yok'})`;
    $('#pmNMh').textContent = M.simulasyonlar.length;
  }
  function mhSeciliCiz(s) {
    const el = $('#pmMhSecili'); if (!s) { el.innerHTML = ''; return; }
    const M = V.muhasebe, yet = M.yetkili, acik = s.durum !== 'İptal';
    const btn = (id, ikon, yazi, aktif, sinif) => `<button type="button" class="btn ${sinif || 'btn-light'} btn-sm py-0" id="${id}" ${aktif ? '' : 'disabled'}><i class="fas ${ikon} me-1"></i>${yazi}</button>`;
    const aksiyon = `<div class="pm-aksiyon">${btn('pmMhHata', 'fa-triangle-exclamation', mhHata ? 'Tüm Satırlar' : 'Hataları Göster', true)}${btn('pmMhOnaya', 'fa-paper-plane', 'Onaya Gönder', acik && s.durum === 'Kontrole Hazır' && s.guncel)}${btn('pmMhOnayla', 'fa-check-double', 'Onayla', acik && s.durum === 'Onaya Gönderildi' && !s.kritikHata && s.guncel && yet, 'btn-warning')}${btn('pmMhKaydet', 'fa-book', 'Muhasebeleştir', acik && s.durum === 'Onaylandı' && s.guncel && yet, 'btn-warning')}${btn('pmMhTers', 'fa-rotate-left', 'Ters Kayıt Oluştur', s.durum === 'Muhasebeleştirildi' && yet)}${btn('pmMhIptal', 'fa-ban', 'İptal', acik && s.durum !== 'Muhasebeleştirildi' && s.durum !== 'Ters Kayıt Yapıldı')}<span class="text-muted" style="font-size:.64rem">${s.kritikHata ? 'Kritik hata var: Onayla kapalı.' : !s.guncel && acik && s.durum !== 'Muhasebeleştirildi' && s.durum !== 'Ters Kayıt Yapıldı' ? 'Kaynak veri / eşleştirme değişti: yeniden simülasyon gerekir.' : s.durum === 'Onaylandı' ? 'İçerik kilitli; Muhasebeleştir gerçek fişleri oluşturur.' : ''}</span></div>`;
    const kontrol = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-list-check me-1"></i>Simülasyon Kontrolleri — Onay Öncesi Zorunlu Testler</span><span class="not">kritik hata varsa onay kapalı</span></div><div class="na-kart-g p-0"><table class="pm-kontrol" data-dinamik="kapali"><tbody>${s.kontroller.map(k => `<tr class="${k.gecti ? '' : k.seviye === 'Kritik' ? 'hata' : 'uyari'}"><td class="n">${k.no}</td><td><b>${esc(k.ad)}</b></td><td>${rozet(k.seviye, k.seviye === 'Kritik' ? 'hata' : 'bekle')}</td><td>${k.gecti ? rozet('Geçti') : rozet(k.seviye === 'Kritik' ? 'Hata' : 'Uyarı', k.seviye === 'Kritik' ? 'hata' : 'bekle')}${k.adet ? ` <small>(${k.adet})</small>` : ''}</td><td>${esc(k.mesaj)}</td></tr>`).join('')}</tbody></table></div></div>`;
    const fisler = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-file-invoice me-1"></i>Fişler (Şirket Bazında)</span><span class="not">her tüzel şirket için ayrı fiş · Fiş Önizleme gerçek fiş formatına yakın borç / alacak gösterir</span></div><div class="na-kart-g p-0"><section class="pm-kaydir" style="max-height:40vh"><table class="na-tablo pm-excel pm-tik" id="pmMhFisTablo" data-dinamik="kapali"><thead><tr class="tek"><th>Fiş</th><th>Şirket</th><th>İşlem Grubu</th><th class="s">Personel</th><th class="s">Satır</th><th class="s">Borç</th><th class="s">Alacak</th><th class="s">Fark</th><th>Denge</th><th class="s">Hatalı Satır</th><th>Muhasebe Fiş No</th><th>Kayıt Tarihi</th><th>Ters Fiş</th><th></th></tr></thead><tbody>${s.fisler.map(f => `<tr class="${f.dengeli && !f.hataliSatir ? '' : 'hatali'}" data-fis="${esc(f.kod)}"><td><b>${esc(f.kod)}</b></td><td>${esc(f.sirket)}</td><td>${esc(f.islemGrubu)} · ${esc(f.baslik)}</td><td class="s">${f.personel}</td><td class="s">${f.satir}</td><td class="s">${kisa(f.toplamBorc)}</td><td class="s">${kisa(f.toplamAlacak)}</td><td class="s">${f.fark ? `<span class="text-danger">${kisa(f.fark)}</span>` : '—'}</td><td>${f.dengeli ? rozet('Dengeli') : rozet('Dengesiz', 'hata')}</td><td class="s">${f.hataliSatir || '—'}</td><td>${f.muhasebeFisNo ? `<b>${esc(f.muhasebeFisNo)}</b>` : '<span class="text-muted">— (onay öncesi fiş no yok)</span>'}</td><td class="kucuk">${esc(f.kayitTarihi || '—')}</td><td>${f.tersFisNo ? `<b>${esc(f.tersFisNo)}</b><small class="alt">${esc(f.tersTarihi)}</small>` : '—'}</td><td><button type="button" class="btn btn-light btn-sm kart pmMhOnizle" data-kod="${esc(f.kod)}"><i class="fas fa-eye me-1"></i>Fiş Önizleme</button></td></tr>`).join('')}</tbody><tfoot><tr><td>Toplam</td><td>${s.fisler.length} Fiş</td><td></td><td></td><td class="s">${s.satirSayisi}</td><td class="s">${kisa(s.toplamBorc)}</td><td class="s">${kisa(s.toplamAlacak)}</td><td class="s">${kisa(s.toplamBorc - s.toplamAlacak)}</td><td></td><td class="s">${s.hataliSatir}</td><td colspan="4"></td></tr></tfoot></table></section></div></div>`;
    // özet simülasyon (gizlilik): hesap + X/R + karşı şirket bazında toplanmış kayıtlar — muhasebeye atılacak hâl, personel adı yok
    const O = s.fisler.flatMap(f => f.hesapOzeti.map(h => ({ ...h, fisKodu: f.kod, sirket: f.sirket, islemGrubu: f.islemGrubu, baslik: f.baslik }))).filter(h => (!mhHata || h.hatali) && (!mhFisF || h.fisKodu === mhFisF) && (!mhAra || [h.hesapKodu, h.hesapAdi, h.sirket, h.karsiSirket, h.baslik].join(' ').toLocaleLowerCase('tr-TR').includes(mhAra)));
    const ozet = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-shield-halved me-1"></i>Özet Simülasyon — Muhasebeye Atılacak Kayıtlar (Genel)</span><span class="not d-flex gap-1 align-items-center"><input class="form-control" id="pmMhAra" placeholder="Hesap, şirket, karşı şirket…" value="${esc(mhAra)}" style="font-size:.66rem;height:24px;width:170px"><select class="form-select" id="pmMhFisF" style="font-size:.66rem;height:24px;width:auto"><option value="">Tüm Fişler</option>${s.fisler.map(f => `<option value="${esc(f.kod)}" ${f.kod === mhFisF ? 'selected' : ''}>${esc(f.kod)} · ${esc(f.sirket)} · ${esc(f.islemGrubu)}</option>`).join('')}</select><span>${O.length} kayıt · personel adı içermez · satıra tıkla → fiş önizleme</span></span></div><div class="na-kart-g p-0"><section class="pm-kaydir" style="max-height:55vh"><table class="na-tablo pm-excel pm-tik" id="pmMhOzetTablo" data-dinamik="kapali"><thead><tr class="tek"><th>Sim. No</th><th>Fiş</th><th>Şirket</th><th>İşlem Grubu</th><th>X/R</th><th>Hesap Kodu</th><th>Hesap Adı</th><th class="s">Borç</th><th class="s">Alacak</th><th>Karşı Şirket</th><th class="s">Personel</th><th>Kontrol</th></tr></thead><tbody>${O.map(h => `<tr class="${h.hatali ? 'hatali' : ''}" data-fis="${esc(h.fisKodu)}"><td class="kucuk">${esc(s.no)} v${s.versiyon}</td><td><b>${esc(h.fisKodu)}</b></td><td>${esc(h.sirket)}</td><td>${esc(h.islemGrubu)} · ${esc(h.baslik)}</td><td>${rozet(h.xr, h.xr === 'X' ? 'bekle' : h.xr === 'R' ? 'ok' : 'gri')}</td><td><b>${esc(h.hesapKodu)}</b></td><td>${esc(h.hesapAdi)}</td><td class="s">${hucre(h.borc)}</td><td class="s">${hucre(h.alacak)}</td><td>${bos(h.karsiSirket)}</td><td class="s">${h.personel}</td><td>${h.hatali ? rozet(`✗ ${h.hatali} satır`, 'hata') : rozet('✓')}</td></tr>`).join('') || '<tr><td colspan="12" class="na-bos">Filtreye uyan kayıt yok.</td></tr>'}</tbody><tfoot><tr><td>Toplam</td><td colspan="6">${O.length} kayıt</td><td class="s">${kisa(O.reduce((a, x) => a + (Number(x.borc) || 0), 0))}</td><td class="s">${kisa(O.reduce((a, x) => a + (Number(x.alacak) || 0), 0))}</td><td colspan="3"></td></tr></tfoot></table></section></div></div>`;
    const L2 = !mhDetay ? [] : (s.satirlar || []).filter(x => (!mhHata || x.hatalar.length) && (!mhFisF || x.fisKodu === mhFisF) && (!mhAra || [x.adSoyad, x.hesapKodu, x.hesapAdi, x.kaynakKalem, x.islemTuru, x.sirket, x.karsiSirket].join(' ').toLocaleLowerCase('tr-TR').includes(mhAra)));
    const satirlar = !mhDetay ? '' : `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-table-list me-1"></i>Personel Bazlı Satırlar — Detay (Yetkili Görünüm)</span><span class="not">${L2.length} / ${(s.satirlar || []).length} satır · aynı arama ve fiş filtresi geçerli · gizlilik: muhasebeye bu satırlar değil, özet kayıtlar atılır</span></div><div class="na-kart-g p-0"><section class="pm-kaydir" style="max-height:60vh"><table class="na-tablo pm-excel pm-tik" id="pmMhSatirTablo" data-dinamik="kapali"><thead><tr class="tek"><th>Sim. No</th><th>Fiş</th><th>Şirket</th><th>Personel</th><th>İşlem Türü</th><th>X/R</th><th>Kaynak Kalem</th><th>Hesap Kodu</th><th>Hesap Adı</th><th class="s">Borç</th><th class="s">Alacak</th><th>Karşı Şirket</th><th>Kontrol</th><th>Açıklama</th></tr></thead><tbody>${L2.slice(0, 1500).map(x => `<tr class="${x.hatalar.length ? 'hatali' : ''}" data-sira="${x.personelSira}" title="${esc(x.hatalar.join(' · '))}"><td class="kucuk">${esc(x.simNo)}</td><td><b>${esc(x.fisKodu)}</b></td><td>${esc(x.sirket)}</td><td class="ad"><a href="#" class="pmMhKisi" data-sira="${x.personelSira}">${esc(x.adSoyad)}</a>${ozRozet(kisiBul(x.personelSira))}</td><td>${esc(x.islemTuru)}</td><td>${rozet(x.xr, x.xr === 'X' ? 'bekle' : x.xr === 'R' ? 'ok' : 'gri')}</td><td>${esc(x.kaynakKalem)}</td><td><b>${esc(x.hesapKodu)}</b></td><td><small>${esc(x.hesapAdi)}</small></td><td class="s">${hucre(x.borc)}</td><td class="s">${hucre(x.alacak)}</td><td>${bos(x.karsiSirket)}</td><td>${x.hatalar.length ? rozet('✗ ' + x.hatalar[0], 'hata') : rozet('✓')}</td><td><small>${esc(x.aciklama)}</small></td></tr>`).join('') || '<tr><td colspan="14" class="na-bos">Filtreye uyan satır yok.</td></tr>'}</tbody><tfoot><tr><td>Toplam</td><td colspan="8">${L2.length} satır${L2.length > 1500 ? ' (ilk 1.500 gösteriliyor)' : ''}</td><td class="s">${kisa(L2.reduce((a, x) => a + (Number(x.borc) || 0), 0))}</td><td class="s">${kisa(L2.reduce((a, x) => a + (Number(x.alacak) || 0), 0))}</td><td colspan="3"></td></tr></tfoot></table></section></div></div>`;
    const audit = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clipboard-list me-1"></i>İzlenebilirlik / Audit Log</span><span class="not">${esc(s.no)} v${s.versiyon} · eşleştirme v${s.eslemeVersiyonu}</span></div><div class="na-kart-g"><ul class="pm-zaman">${s.audit.map(a => `<li><b>${esc(a.islem)}</b> — ${esc(a.detay)}<small>${esc(a.tarih)} · ${esc(a.kullanici)}</small></li>`).join('')}</ul></div></div>`;
    el.innerHTML = aksiyon + kontrol + ozet + fisler + satirlar + audit;
    $('#pmMhHata').addEventListener('click', () => { mhHata = !mhHata; mhSeciliCiz(s); });
    $('#pmMhAra').addEventListener('input', e => { mhAra = e.target.value.toLocaleLowerCase('tr-TR'); mhSeciliCiz(s); setTimeout(() => { const i = $('#pmMhAra'); if (i) { i.focus(); i.setSelectionRange(i.value.length, i.value.length); } }, 0); });
    $('#pmMhFisF').addEventListener('change', e => { mhFisF = e.target.value; mhSeciliCiz(s); });
    $('#pmMhOnaya').addEventListener('click', () => mhIslem(s.id, 'onayaGonder')); $('#pmMhOnayla').addEventListener('click', () => mhIslem(s.id, 'onayla')); $('#pmMhKaydet').addEventListener('click', () => { if (window.confirm(`${s.no} v${s.versiyon}: ${s.fisler.length} fiş gerçek muhasebe kaydına dönüştürülecek. Devam edilsin mi?`)) mhIslem(s.id, 'muhasebelestir'); });
    $('#pmMhTers').addEventListener('click', () => { const n = window.prompt('Ters kayıt nedeni (zorunlu):'); if (n && n.trim()) mhIslem(s.id, 'tersKayit', n.trim()); }); $('#pmMhIptal').addEventListener('click', () => { const n = window.prompt('İptal nedeni:'); if (n !== null) mhIslem(s.id, 'iptal', n); });
    $$('.pmMhOnizle', el).forEach(b => b.addEventListener('click', () => mhFisOnizle(s, b.dataset.kod)));
    $$('.pmMhKisi', el).forEach(a => a.addEventListener('click', ev => { ev.preventDefault(); const p = bul(+a.dataset.sira); if (p) { if (!!p.ozel !== (kapsam === 'ozel')) kapsam = p.ozel ? 'ozel' : 'bordro'; kartAc(p.sira); } }));
    sabitleHepsi();
  }
  function mhEslemeCiz(M) {
    $('#pmMhEsleme').style.display = mhEslemeAcik ? '' : 'none';
    const sirketler = M.sirketler || []; ['#pmme_sirket', '#pmme_karsi'].forEach((id, i) => { const e = $(id), v = e.value; e.innerHTML = `<option value="*">${i ? 'Tümü' : 'Tüm Şirketler'}</option>` + sirketler.map(s => `<option value="${esc(s)}">${esc(s)}</option>`).join(''); e.value = v || '*'; });
    const isl = $('#pmme_islem'); if (!isl.options.length) isl.innerHTML = (M.islemTurleri || []).map(t => `<option value="${esc(t)}">${esc(t)}</option>`).join('');
    $('#dlMhKalem').innerHTML = [...new Set((M.eslemeler || []).map(e => e.kaynakKalem))].map(k => `<option value="${esc(k)}">`).join('');
    $('#dlMhHesap').innerHTML = (M.hesaplar || []).map(h => `<option value="${esc(h.kod)}">${esc(h.ad)}</option>`).join('');
    $('#pmMhEslemeTablo tbody').innerHTML = (M.eslemeler || []).map(e => `<tr data-id="${e.id}" class="${e.aktif ? '' : 'ayrildi'}"><td class="kucuk">${e.id}</td><td>${e.sirket === '*' ? '<span class="text-muted">Tüm Şirketler</span>' : esc(e.sirket)}</td><td>${e.karsiSirket === '*' ? '<span class="text-muted">—</span>' : esc(e.karsiSirket)}</td><td>${esc(e.islemTuru)}</td><td>${esc(e.kaynakKalem)}</td><td>${rozet(e.xr === '*' ? 'X+R' : e.xr, e.xr === 'X' ? 'bekle' : e.xr === 'R' ? 'ok' : 'gri')}</td><td>${esc(e.yon)}</td><td><b>${esc(e.hesapKodu)}</b>${e.posting ? '' : ' <span class="pm-rz hata">üst hesap</span>'}</td><td>${esc(e.hesapAdi)}</td><td>${e.varsayilan ? '✓' : ''}</td><td class="kucuk">${esc(e.gecerlilikBas || '—')} – ${esc(e.gecerlilikBit || '∞')}</td><td>${rozet(e.aktif ? 'Aktif' : 'Pasif', e.aktif ? 'ok' : 'gri')}</td><td class="s">v${e.versiyon}</td><td class="kucuk">${esc(e.degistiren || '')}<small class="alt">${esc(e.degisme || '')}</small></td></tr>`).join('');
    $$('#pmMhEslemeTablo tbody tr').forEach(tr => tr.addEventListener('click', () => mhEslemeSec(+tr.dataset.id)));
  }
  function mhEslemeSec(id) {
    const e = (V.muhasebe.eslemeler || []).find(x => x.id === id); if (!e) return;
    $('#pmme_id').value = e.id; $('#pmme_sirket').value = e.sirket; $('#pmme_karsi').value = e.karsiSirket; $('#pmme_islem').value = e.islemTuru; $('#pmme_kalem').value = e.kaynakKalem; $('#pmme_xr').value = e.xr; $('#pmme_yon').value = e.yon; $('#pmme_hesap').value = e.hesapKodu; $('#pmme_hesapAd').value = e.hesapAdi; $('#pmme_bas').value = e.gecerlilikBas || ''; $('#pmme_bit').value = e.gecerlilikBit || ''; $('#pmme_varsayilan').checked = e.varsayilan; $('#pmme_aktif').checked = e.aktif; $('#pmme_kaydetYazi').textContent = `#${e.id} Güncelle`;
    $('#pmme_kalem').scrollIntoView({ block: 'center' });
  }
  function mhEslemeTemizle() { $('#pmMhEslemeForm').reset(); $('#pmme_id').value = ''; $('#pmme_hesapAd').value = ''; $('#pmme_kaydetYazi').textContent = 'Eşleştirme Ekle'; }
  async function mhEslemeKaydet(ev) {
    ev.preventDefault(); if (!$('#pmMhEslemeForm').reportValidity()) return;
    const d = { id: $('#pmme_id').value ? +$('#pmme_id').value : null, sirket: $('#pmme_sirket').value, karsiSirket: $('#pmme_karsi').value, islemTuru: $('#pmme_islem').value, kaynakKalem: $('#pmme_kalem').value.trim(), xr: $('#pmme_xr').value, yon: $('#pmme_yon').value, hesapKodu: $('#pmme_hesap').value.trim(), varsayilan: $('#pmme_varsayilan').checked, gecerlilikBas: $('#pmme_bas').value, gecerlilikBit: $('#pmme_bit').value, aktif: $('#pmme_aktif').checked, donem: V.donem };
    const j = await post('/PersonelMaliyet/MuhasebeEsleme', d); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    mhEslemeTemizle(); veriGuncelle(j); mesaj('success', esc(j.message));
  }
  /** Personel Muhasebe İşlemleri ekranında atılan adımlar burada da görünsün: sekmeye girişte veri tazelenir (düzenleme modunda dokunulmaz) */
  async function mhTazele() {
    if (duzenleme || mhTazele.calisiyor) return; mhTazele.calisiyor = true;
    try { const j = await fetch('/PersonelMaliyet/Veri?donem=' + encodeURIComponent(V.donem)).then(r => r.json()); if (j && j.muhasebe) { V.muhasebe = j.muhasebe; if (aktifSekme === 'muhasebe') muhasebeCiz(); } } catch (e) { /* yoksay */ } finally { mhTazele.calisiyor = false; }
  }
  async function mhIslem(id, islem, not) {
    const j = await post('/PersonelMaliyet/MuhasebeIslem', { id, islem, not: not || '', donem: V.donem }); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    mhSecili = id; veriGuncelle(j); mesaj('success', esc(j.message));
  }
  /** fiş önizleme: hesap bazında toplanmış borç / alacak (gerçek fiş formatına yakın) + satır listesi */
  function mhFisOnizle(s, kod) {
    const f = s.fisler.find(x => x.kod === kod); if (!f) return;
    $('#pmTalepBaslik').innerHTML = `<i class="fas fa-file-invoice me-1"></i>${esc(s.no)} v${s.versiyon} · ${esc(f.kod)} · ${esc(f.sirket)} ${f.dengeli ? rozet('Dengeli') : rozet('Dengesiz', 'hata')}${f.muhasebeFisNo ? ' ' + rozet('Fiş ' + f.muhasebeFisNo) : ''}`;
    $('#pmTalepAksiyon').innerHTML = f.muhasebeFisNo ? `<a class="btn btn-light btn-sm py-0" href="${esc(V.muhasebe.muhasebeEkrani || '/Muhasebe')}" target="_blank"><i class="fas fa-up-right-from-square me-1"></i>Muhasebe Fişleri</a>` : '';
    const satirlar = mhDetay ? (s.satirlar || []).filter(x => x.fisKodu === kod) : [];
    $('#pmTalepIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-scale-balanced me-1"></i>${esc(f.islemGrubu)} · ${esc(f.baslik)}</span><span class="not">${esc(V.donemAd)} · ${f.personel} personel · ${f.satir} satır · veritabanı ${esc(f.db)} · muhasebeye bu özet hâliyle atılır</span></div><div class="na-kart-g p-0"><table class="pm-tfis" data-dinamik="kapali"><thead><tr><th>Hesap Kodu</th><th>Hesap Adı</th><th>X/R</th><th>Karşı Şirket</th><th class="s">Borç</th><th class="s">Alacak</th><th class="s">Personel</th></tr></thead><tbody>${f.hesapOzeti.map(h => `<tr><td><b>${esc(h.hesapKodu)}</b></td><td>${esc(h.hesapAdi)}</td><td>${esc(h.xr)}</td><td>${bos(h.karsiSirket)}</td><td class="s">${hucre(h.borc)}</td><td class="s">${hucre(h.alacak)}</td><td class="s">${h.personel}</td></tr>`).join('')}<tr class="toplam"><td colspan="4">Toplam</td><td class="s">${kisa(f.toplamBorc)}</td><td class="s">${kisa(f.toplamAlacak)}</td><td class="s">${f.personel}</td></tr></tbody></table></div></div>
      ${mhDetay ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-users me-1"></i>Personel Bazında Satırlar (Yetkili Görünüm)</span><span class="not">kaynak kalem · X/R · karşı şirket</span></div><div class="na-kart-g p-0"><section class="pm-kaydir" style="max-height:50vh"><table class="pm-tfis" data-dinamik="kapali"><thead><tr><th>Personel</th><th>Kalem</th><th>X/R</th><th>Hesap</th><th class="s">Borç</th><th class="s">Alacak</th><th>Karşı</th><th>K.</th></tr></thead><tbody>${satirlar.map(x => `<tr><td>${esc(x.adSoyad)}</td><td>${esc(x.kaynakKalem)}</td><td>${esc(x.xr)}</td><td>${esc(x.hesapKodu)}</td><td class="s">${hucre(x.borc)}</td><td class="s">${hucre(x.alacak)}</td><td>${bos(x.karsiSirket)}</td><td>${x.hatalar.length ? '<span class="text-danger" title="' + esc(x.hatalar.join(' · ')) + '">✗</span>' : '✓'}</td></tr>`).join('')}</tbody></table></section></div></div>` : '<div class="pm-not">Personel bazlı satırlar gizli (bilgi gizliliği). Görmek için başlıktaki "Personel Detayını Göster" anahtarını açın.</div>'}`;
    $('#pmTalepKart').classList.add('acik'); $('#pmTalepPerde').classList.add('acik');
  }

  // ============================================================
  // SATIR DÜZENLEME ÇEKMECESİ (tüm sekmeler): alan tanımından form kurar; Kaydet ilgili uca gönderir, yanıt tüm veriyi yeniler → bordro satırı, kart ve cari aynı anda güncellenir
  // ============================================================
  function duzenleKart(baslik, alanlar, veri, kaydet, sil, onizle) {
    const alan = a => {
      const v = veri[a.k], id = `pmd_${a.k}`, z = a.zorunlu ? 'required' : '', lb = `<label for="${id}" class="${a.zorunlu ? 'z' : ''}">${esc(a.ad)}</label>`;
      if (a.tip === 'select') return `<div class="${a.sinif || ''}">${lb}<select class="form-select" id="${id}" data-k="${a.k}" ${z}>${(a.secenekler || []).map(o => { const [ov, ol] = Array.isArray(o) ? o : [o, o]; return `<option value="${esc(ov)}" ${String(v ?? '') === String(ov) ? 'selected' : ''}>${esc(ol)}</option>`; }).join('')}</select></div>`;
      if (a.tip === 'textarea') return `<div class="g3">${lb}<textarea class="form-control" id="${id}" data-k="${a.k}" rows="2" maxlength="300">${esc(v ?? '')}</textarea></div>`;
      if (a.tip === 'salt') return `<div class="${a.sinif || ''}"><label>${esc(a.ad)}</label><input class="form-control ro" value="${esc(v ?? '')}" readonly></div>`;
      return `<div class="${a.sinif || ''}">${lb}<input class="form-control" id="${id}" data-k="${a.k}" type="${a.tip === 'number' ? 'number' : a.tip === 'month' ? 'month' : 'text'}" ${a.tip === 'number' ? `min="0" step="${a.adim || '0.01'}"` : ''} ${a.liste ? `list="${a.liste}"` : ''} value="${esc(v ?? (a.tip === 'number' ? 0 : ''))}" ${z} ${a.ph ? `placeholder="${esc(a.ph)}"` : ''} ${a.maxlength ? `maxlength="${a.maxlength}"` : ''}></div>`;
    };
    $('#pmTalepBaslik').innerHTML = baslik;
    $('#pmTalepAksiyon').innerHTML = `<button type="button" class="btn btn-warning btn-sm py-0" id="pmdKaydet"><i class="fas fa-floppy-disk me-1"></i>Kaydet</button>${sil ? '<button type="button" class="btn btn-light btn-sm py-0" id="pmdSil" style="color:#b91c1c"><i class="fas fa-trash me-1"></i>Sil</button>' : ''}<button type="button" class="btn btn-light btn-sm py-0" id="pmdVazgec">Vazgeç</button>`;
    $('#pmTalepIc').innerHTML = `<div class="alert alert-danger py-1 px-2 mb-2" id="pmdHata" style="display:none;font-size:.7rem"></div><div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-pen me-1"></i>Düzenle</span><span class="not">Kaydet: değişiklik ilgili kişinin bordro satırına, kartına ve carisine yansır</span></div><div class="na-kart-g"><form class="pm-form" id="pmdForm">${alanlar.map(alan).join('')}</form><div class="pm-not mt-2" id="pmdNot"></div></div></div>`;
    const oku = () => { const d = { ...veri }; $$('#pmdForm [data-k]').forEach(e => { const a = alanlar.find(x => x.k === e.dataset.k); d[e.dataset.k] = a && a.tip === 'number' ? (Number(e.value) || 0) : e.value.trim(); }); return d; };
    const goster = () => { if (onizle) $('#pmdNot').innerHTML = onizle(oku()); }; goster();
    $$('#pmdForm [data-k]').forEach(e => e.addEventListener('input', goster));
    $('#pmdKaydet').addEventListener('click', async () => { if (!$('#pmdForm').reportValidity()) return; const j = await kaydet(oku()); if (!j.success) { const h = $('#pmdHata'); h.textContent = j.message; h.style.display = 'block'; return; } talepKartKapat(); veriGuncelle(j); mesaj('success', esc(j.message)); });
    if (sil) $('#pmdSil').addEventListener('click', async () => { if (!window.confirm('Kayıt silinsin mi?')) return; const j = await sil(); if (!j.success) { mesaj('danger', esc(j.message)); return; } talepKartKapat(); veriGuncelle(j); mesaj('success', esc(j.message)); });
    $('#pmdVazgec').addEventListener('click', talepKartKapat);
    $('#pmTalepKart').classList.add('acik'); $('#pmTalepPerde').classList.add('acik');
  }
  const sonrakiAy = d => { const y = +d.slice(0, 4), m = +d.slice(5, 7); return m === 12 ? `${y + 1}-01` : `${y}-${String(m + 1).padStart(2, '0')}`; };
  /** prim satırı: oran / baz / tür / ödeme değişince hesaplanan prim ve kişinin bordro primi (prim ayını izleyen bordro) güncellenir */
  function primDuzenle(sira) {
    const r = sira ? (V.primler || []).find(x => x.sira === sira) : { sira: 0, donem: oncekiAy(V.donem), adSoyad: '', primTuru: 'Ciro Primi', baz: 0, oran: 0, sabitTutar: 0, odeme: 'X', aciklama: '' };
    if (!r) return;
    const kisiler = V.personel.filter(p => (p.durum || 'Aktif') === 'Aktif').map(p => p.adSoyad).sort((a, b) => a.localeCompare(b, 'tr'));
    duzenleKart(`<i class="fas fa-sack-dollar me-1"></i>${sira ? 'Prim Satırı · ' + esc(r.adSoyad) : 'Yeni Prim Satırı'}`, [
      { k: 'donem', ad: 'Dönem (Prim Ayı)', tip: 'month', zorunlu: true }, { k: 'adSoyad', ad: 'Personel', tip: 'select', secenekler: (sira ? [] : [['', 'Seçin…']]).concat(kisiler), zorunlu: true }, { k: 'primTuru', ad: 'Prim Türü', tip: 'select', secenekler: (V.ekSutunlar && V.ekSutunlar.primTurleri) || ['Ciro Primi', 'Tahsilat Primi', 'Hedef Primi', 'Özel Ödeme'] },
      { k: 'baz', ad: 'Baz (Ciro / Tahsilat ₺)', tip: 'number' }, { k: 'oran', ad: 'Oran %', tip: 'number', adim: '0.01' }, { k: 'sabitTutar', ad: 'Sabit Tutar ₺ (Oran Yerine)', tip: 'number' },
      { k: 'odeme', ad: 'Ödeme', tip: 'select', secenekler: [['X', 'X (Bordro Dışı)'], ['R', 'R (Bordrolu)']] }, { k: 'aciklama', ad: 'Açıklama', tip: 'textarea' }
    ], r, d => post('/PersonelMaliyet/PrimKaydet', { ...d, sira: r.sira, bordroDonem: V.donem }), sira ? () => post('/PersonelMaliyet/PrimSil', { id: sira, donem: V.donem }) : null,
      d => { const h = d.sabitTutar > 0 ? d.sabitTutar : d.baz * d.oran / 100; return `Hesaplanan prim: <b>${kisa(h)}</b> · ${d.donem ? esc(sonrakiAy(d.donem)) : '—'} bordrosundaki Prim-${esc(d.odeme)} tutarı prim satırlarının toplamından yeniden yazılır${d.odeme === 'R' ? ' (R primi brüte girer; kaynak tablo değerleri düşer, model yeniden hesaplar)' : ''}.`; });
  }
  /** gündelikçi üretim personeli: günlük ücret × gün; SGK'lı ise bordro Net-R mahsup edilir, fark elden X olarak kişinin X-Net'ine yazılır */
  function uretimDuzenle(sira) {
    const u = sira ? (V.uretim || []).find(x => x.sira === sira) : { sira: 0, fiiliGorevYeri: '', adSoyad: '', gorev: 'Üretim Personeli', vardiya: 'Gündüz', personelSira: 0, gunlukUcret: 2200, calisilanGun: 26, gunlukSaat: 7.5, fazlaMesaiSaat: 0, tatilGun: 0, yemek: 0, yol: 0, digerSosyal: 0, kesinti: 0, rMahsup: 0, not: '' };
    if (!u) return;
    const bordrolu = [['0', 'SGK\'sız gündelikçi (tamamı elden X)']].concat(V.personel.filter(p => (p.durum || 'Aktif') === 'Aktif' && !p.ozel).sort((a, b) => a.adSoyad.localeCompare(b.adSoyad, 'tr')).map(p => [String(p.sira), `${p.adSoyad} · ${p.cariKodu} · Net-R ${kisa(p.netRBordro)}`]));
    duzenleKart(`<i class="fas fa-industry me-1"></i>${sira ? 'Gündelikçi · ' + esc(u.adSoyad) : 'Yeni Gündelikçi Üretim Personeli'}`, [
      { k: 'adSoyad', ad: 'Adı-Soyadı', tip: 'text', zorunlu: true }, { k: 'fiiliGorevYeri', ad: 'Fiili Görev Yeri', tip: 'text', zorunlu: true, liste: 'dlYer' }, { k: 'gorev', ad: 'Görev', tip: 'text' },
      { k: 'vardiya', ad: 'Vardiya', tip: 'select', secenekler: (V.ekSutunlar && V.ekSutunlar.vardiyalar) || ['Gündüz', 'Akşam', 'Gece'] }, { k: 'personelSira', ad: 'Bordrolu Personel (SGK\'lı · R Mahsup)', tip: 'select', secenekler: bordrolu, sinif: 'g2' },
      { k: 'gunlukUcret', ad: 'Günlük Ücret ₺', tip: 'number' }, { k: 'calisilanGun', ad: 'Çalışılan Gün', tip: 'number', adim: '0.5' }, { k: 'gunlukSaat', ad: 'Günlük Saat', tip: 'number', adim: '0.5' },
      { k: 'fazlaMesaiSaat', ad: 'Fazla Mesai (Saat, ×1,5)', tip: 'number', adim: '0.5' }, { k: 'tatilGun', ad: 'Tatil (Gün, ×2)', tip: 'number', adim: '0.5' }, { k: 'kesinti', ad: 'Avans / Kesinti ₺', tip: 'number' },
      { k: 'yemek', ad: 'Yemek ₺', tip: 'number' }, { k: 'yol', ad: 'Yol ₺', tip: 'number' }, { k: 'digerSosyal', ad: 'Diğer Sosyal Yardım ₺', tip: 'number' }, { k: 'not', ad: 'Not', tip: 'textarea' }
    ], u, d => post('/PersonelMaliyet/UretimKaydet', { ...d, sira: u.sira, personelSira: +d.personelSira || 0, donem: V.donem }), sira ? () => post('/PersonelMaliyet/UretimSil', { id: sira, donem: V.donem }) : null,
      d => { const su = d.gunlukSaat > 0 ? d.gunlukUcret / d.gunlukSaat : 0, h = d.gunlukUcret * d.calisilanGun, fm = d.fazlaMesaiSaat * su * 1.5, t = d.tatilGun * d.gunlukUcret * 2, top = h + fm + t + d.yemek + d.yol + d.digerSosyal; const p = +d.personelSira ? bul(+d.personelSira) : null; const r = p ? (Number(p.netRBordro) || 0) : 0, x = Math.max(0, top - r); return `Hak ediş ${kisa(h)} · mesai ${kisa(fm)} · tatil ${kisa(t)} · <b>toplam hak ediş ${kisa(top)}</b>${p ? ` · bordro Net-R mahsup ${kisa(r)} → <b>elden X ${kisa(x)}</b> (${esc(p.adSoyad)} X-Net'ine yazılır, R kısmı aynen kalır)` : ` · SGK'sız: tamamı elden X ${kisa(top)}`} · net ödenen ${kisa(x - d.kesinti)}.`; });
  }
  /** kesinti / icra planı: taksit, toplam, ödenen, başlangıç; kişinin tüm dönemlerdeki ilgili kesinti sütunu plan toplamına eşitlenir */
  function planDuzenle(id) {
    const k = (V.kesinti.planlar || []).find(x => x.id === id); if (!k) return;
    duzenleKart(`<i class="fas fa-file-invoice-dollar me-1"></i>${esc(k.tur)} · ${esc(k.adSoyad)}`, [
      { k: 'adSoyad', ad: 'Personel', tip: 'salt' }, { k: 'tur', ad: 'Kesinti Türü', tip: 'select', secenekler: V.kesinti.turler || [] }, { k: 'kaynak', ad: 'Kaynak (Talep No / Senet)', tip: 'text' },
      { k: 'dosyaNo', ad: 'Dosya No (İcra)', tip: 'text' }, { k: 'icraDairesi', ad: 'İcra Dairesi', tip: 'text' }, { k: 'alacakli', ad: 'Alacaklı', tip: 'text' },
      { k: 'toplamTutar', ad: 'Toplam Tutar ₺', tip: 'number', zorunlu: true }, { k: 'aylikTaksit', ad: 'Aylık Taksit ₺', tip: 'number', zorunlu: true }, { k: 'odenenTaksit', ad: 'Ödenen Taksit (Adet)', tip: 'number', adim: '1' },
      { k: 'baslangicDonem', ad: 'Başlangıç Dönemi', tip: 'month', zorunlu: true }, { k: 'aciklama', ad: 'Açıklama', tip: 'textarea' }
    ], k, d => post('/PersonelMaliyet/KesintiGuncelle', { ...d, id: k.id, donem: V.donem }), () => post('/PersonelMaliyet/KesintiSil', { id: k.id, donem: V.donem }),
      d => { const n = d.toplamTutar > 0 && d.aylikTaksit > 0 ? Math.ceil(d.toplamTutar / Math.min(d.aylikTaksit, d.toplamTutar)) : 0; let bit = ''; if (n && d.baslangicDonem) { const y = +d.baslangicDonem.slice(0, 4), m = +d.baslangicDonem.slice(5, 7) - 1 + n - 1; bit = `${y + Math.floor(m / 12)}-${String(m % 12 + 1).padStart(2, '0')}`; } return `${n} taksit · bitiş <b>${esc(bit || '—')}</b> · kalan ${kisa(Math.max(0, d.toplamTutar - d.aylikTaksit * d.odenenTaksit))}. Kişinin ${esc(V.donemAd)} bordrosundaki ${esc(d.tur)} sütunu bu türdeki planların dönem taksiti toplamı olur.`; });
  }
  // ============================================================
  // İŞTEN ÇIKIŞ SENARYOSU — Birleşik Tablo satırları; işaretlenenler ayrılırsa kalan maliyet, tasarruf, tahmini kıdem / ihbar (veri değişmez; işaretler localStorage'da)
  // ============================================================
  let cikisSecim = new Set(); try { cikisSecim = new Set(JSON.parse(localStorage.pmCikisSecim || '[]')); } catch (e) { /* yoksay */ }
  const cikisAnahtar = r => `${r.kaynak}:${r.kaynak === 'Üretim' ? r.uSira : r.sira0}`;
  /** görev yeri → lokasyon: Çorlu içeren yerler Çorlu, diğerleri (Asia dahil) İstanbul; hücrede altında asıl görev yeri */
  const lokasyonAd = yer => /çorlu/i.test(yer || '') ? 'Çorlu' : 'İstanbul';
  const lokasyon = yer => { const y = yer || '', l = lokasyonAd(y); return `<b>${l}</b>${y && y !== l ? `<small class="alt">${esc(y)}</small>` : ''}`; };
  const ihbarHafta = yil => yil === null || yil === undefined ? 0 : yil < 0.5 ? 2 : yil < 1.5 ? 4 : yil < 3 ? 6 : 8;
  function cikisHesap(r) {
    const tavan = Number($('#pmKidemTavan').value) || 0, yil = r.kaynak === 'Üretim' ? null : r.kidemYil, brut = r.kaynak === 'Üretim' ? 0 : r.brut;
    const kidem = yil === null || yil === undefined ? 0 : Math.round(Math.min(brut, tavan || brut) * yil * 100) / 100, ihbar = yil === null || yil === undefined ? 0 : Math.round(brut / 30 * 7 * ihbarHafta(yil) * 100) / 100;
    return { kidem, ihbar, cikis: kidem + ihbar, yillik: (Number(r.genelMaliyet) || 0) * 12 };
  }
  function cikisCiz() {
    $('#pmCikisBaslik').textContent = `${V.donemAd} İşten Çıkış Senaryosu`;
    const ara = ($('#pmCikisAra').value || '').toLocaleLowerCase('tr-TR'), kaynak = $('#pmCikisKaynak').value, yalniz = $('#pmCikisSecili').checked;
    const tum = birlesimListe('').filter(r => r.kaynak !== 'Özel');   // özel kişiler senaryoya girmez
    const L2 = tum.filter(r => (!kaynak || r.kaynak === kaynak) && (!yalniz || cikisSecim.has(cikisAnahtar(r))) && (!ara || [r.adSoyad, r.gorev, r.isveren, r.gorevYeri, lokasyonAd(r.gorevYeri)].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    // satır bağı: Net Toplam + Ek Ödemeler − Kesintiler = Net Ödenen; Net Ödenen + Kesintiler + Toplam Brüt Masraflar + Toplam İşveren = Genel Maliyet (Birleşik Tablo ile birebir)
    const ekOdeme = r => ['yol', 'yemek', 'mesai', 'prim', 'diger'].reduce((s, k) => s + (Number(r[k]) || 0), 0);
    $('#pmCikisTablo tbody').innerHTML = L2.map((r, i) => { const k = cikisAnahtar(r), sec = cikisSecim.has(k), h = cikisHesap(r); return `<tr class="${sec ? 'cikacak' : ''}" data-k="${esc(k)}" data-kaynak="${esc(r.kaynak)}" data-sira0="${r.sira0 ?? ''}" data-usira="${r.uSira ?? ''}"><td class="kucuk">${i + 1}</td><td class="kaynak k-${r.kaynak === 'Üretim' ? 'uretim' : r.kaynak === 'Özel' ? 'ozel' : 'bordro'}">${esc(r.kaynak)}</td><td class="ad">${adOz(r.adSoyad, r.sira0)}</td><td>${esc(r.gorev || '')}</td><td>${lokasyon(r.gorevYeri)}</td><td class="s">${r.kidemYil === null || r.kidemYil === undefined ? '—' : ondalik(r.kidemYil, 1)}</td><td class="s" data-k="netToplam">${hucre(r.netToplam)}</td><td class="s" data-k="ekOdeme">${hucre(ekOdeme(r))}</td><td class="s" data-k="kesinti">${hucre(r.kesinti)}</td><td class="s" data-k="netOdenen">${hucre(r.netOdenen)}</td><td class="s" data-k="toplamBrutMasraflar">${hucre(r.toplamBrutMasraflar)}</td><td class="s" data-k="toplamIsveren">${hucre(r.toplamIsveren)}</td><td class="s hesap" data-k="genelMaliyet">${hucre(r.genelMaliyet)}</td><td class="s">${hucre(h.yillik)}</td><td class="s">${hucre(h.kidem)}</td><td class="s">${hucre(h.ihbar)}</td><td class="s hesap">${hucre(h.cikis)}</td><td><select class="form-select pmCikisSec ${sec ? 'cikacak' : ''}" data-k="${esc(k)}" aria-label="Senaryo · ${esc(r.adSoyad)}"><option value="kal" ${sec ? '' : 'selected'}>Kalıyor</option><option value="cik" ${sec ? 'selected' : ''}>İşten Çıkacak</option></select></td></tr>`; }).join('') || '<tr><td colspan="18" class="na-bos">Filtreye uyan kişi yok.</td></tr>';
    const secili = tum.filter(r => cikisSecim.has(cikisAnahtar(r))), top = (l, k) => l.reduce((s, r) => s + (Number(r[k]) || 0), 0), topH = (l, k) => l.reduce((s, r) => s + cikisHesap(r)[k], 0);
    const mevcut = top(tum, 'genelMaliyet'), cikan = top(secili, 'genelMaliyet'), kalan = mevcut - cikan, cikisMaliyet = topH(secili, 'cikis');
    // dip toplam: her sütunun altında kendi toplamı (birleşik hücre yok; sola sabit hücreler toplamları örtmesin)
    $('#pmCikisTablo tfoot tr').innerHTML = `<td>Toplam</td><td>${L2.length} kişi</td><td>${secili.length} işten çıkacak</td><td></td><td></td><td></td><td class="s" data-k="netToplam">${kisa(top(L2, 'netToplam'))}</td><td class="s" data-k="ekOdeme">${kisa(L2.reduce((s, r) => s + ekOdeme(r), 0))}</td><td class="s" data-k="kesinti">${kisa(top(L2, 'kesinti'))}</td><td class="s" data-k="netOdenen">${kisa(top(L2, 'netOdenen'))}</td><td class="s" data-k="toplamBrutMasraflar">${kisa(top(L2, 'toplamBrutMasraflar'))}</td><td class="s" data-k="toplamIsveren">${kisa(top(L2, 'toplamIsveren'))}</td><td class="s" data-k="genelMaliyet">${kisa(top(L2, 'genelMaliyet'))}</td><td class="s">${kisa(topH(L2, 'yillik'))}</td><td class="s">${kisa(topH(L2, 'kidem'))}</td><td class="s">${kisa(topH(L2, 'ihbar'))}</td><td class="s">${kisa(topH(L2, 'cikis'))}</td><td></td>`;
    const kpi = [['#0f172a', 'Mevcut Aylık Genel Maliyet', kisa(mevcut), `${tum.length} kişi · yıllık ${kisa(mevcut * 12)}`], ['#dc2626', 'İşten Çıkacak', `${secili.length} Kişi`, secili.length ? secili.slice(0, 4).map(r => r.adSoyad).join(', ') + (secili.length > 4 ? ' …' : '') : 'tabloda işaretleyin'],
      ['#b45309', 'Çıkacakların Aylık Maliyeti', kisa(cikan), `yıllık tasarruf ${kisa(cikan * 12)}`], ['#0f766e', 'Kalan Aylık Genel Maliyet', kisa(kalan), `${tum.length - secili.length} kişi · yıllık ${kisa(kalan * 12)}`],
      ['#7c3aed', 'Tahmini Tek Seferlik Çıkış Maliyeti', kisa(cikisMaliyet), `kıdem ${kisa(topH(secili, 'kidem'))} · ihbar ${kisa(topH(secili, 'ihbar'))}`], ['#1d4ed8', 'Geri Dönüş', cikan > 0 ? ondalik(cikisMaliyet / cikan, 1) + ' ay' : '—', 'çıkış maliyeti / aylık tasarruf']];
    $('#pmCikisKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $('#pmCikisSayi').textContent = `${L2.length} / ${tum.length} kişi · ${secili.length} işten çıkacak · ${V.donemAd} · tutarlar ₺ · senaryo veriyi değiştirmez, planınız saklanır`;
    $('#pmNCikis').textContent = secili.length;
    $$('#pmCikisTablo .pmCikisSec').forEach(c => c.addEventListener('change', () => { if (c.value === 'cik') cikisSecim.add(c.dataset.k); else cikisSecim.delete(c.dataset.k); cikisSakla(); cikisCiz(); }));
    sabitleHepsi();
  }
  function cikisSakla() { try { localStorage.pmCikisSecim = JSON.stringify([...cikisSecim]); } catch (e) { /* yoksay */ } }

  /** birleşik tablo satırı: bordro / özel kişi → personel kartı; üretim → üretim düzenleme */
  function birlesimSatirAc(tr) {
    const kaynak = tr.dataset.kaynak; if (kaynak === 'Üretim') { uretimDuzenle(+tr.dataset.usira); return; }
    const p = bul(+tr.dataset.sira0); if (!p) return; if (!!p.ozel !== (kapsam === 'ozel')) kapsam = p.ozel ? 'ozel' : 'bordro'; kartAc(p.sira);
  }

  /** ekrandaki tabloları CSV'ye (₺ işaretsiz) */
  function domExcel(ad, tablolar) {
    const temiz = t => t.trim().replace(/\s+/g, ' ').replace('—', '').replace(/\s*₺/g, ''), rows = [];
    tablolar.forEach(([id, bas]) => { rows.push([bas]); rows.push($$('#' + id + ' thead th').map(t => t.textContent.trim())); $$('#' + id + ' tbody tr').forEach(tr => { if (!tr.querySelector('.na-bos')) rows.push($$('td', tr).map(td => temiz(td.textContent))); }); rows.push([]); });
    csvIndir(ad, rows);
  }

  // ---- başlat
  fetch('/PersonelMaliyet/Veri' + (kok.dataset.donem ? '?donem=' + encodeURIComponent(kok.dataset.donem) : '')).then(r => r.json()).then(v => {
    V = v; sutunlariKur(); listeleriDoldur(); ekKur(); donemKur();
    $('#pmDonem').addEventListener('change', donemDegis);
    $('#pmSifirGizle').addEventListener('change', ciz);
    Object.keys(EK).forEach(ad => { const E = EK[ad]; [E.ara, E.sifir].filter(Boolean).forEach(s => { $(s).addEventListener('input', () => ekCiz(ad)); $(s).addEventListener('change', () => ekCiz(ad)); }); });
    ['#pmUretimYer', '#pmPrimDonem', '#pmBirlesimKaynak'].forEach(s => $(s).addEventListener('change', () => ekCiz(s === '#pmUretimYer' ? 'uretim' : s === '#pmPrimDonem' ? 'prim' : 'birlesim')));
    // Ek Çalışmalar / Diğer Kesintiler
    ['#pmEkAra', '#pmEkDurum'].forEach(s => { $(s).addEventListener('input', ekCalismaCiz); $(s).addEventListener('change', ekCalismaCiz); });
    ['#pmKesAra', '#pmKesTur'].forEach(s => { $(s).addEventListener('input', kesintiCiz); $(s).addEventListener('change', kesintiCiz); });
    $('#pmEkExcel').addEventListener('click', () => domExcel('ek_calismalar_' + V.donem, [['pmEkOzet', 'Kişi Bazında Ek Çalışma ↔ Mesai Talepleri'], ['pmMesaiTablo', 'Mesai Talepleri']]));
    $('#pmKesExcel').addEventListener('click', () => domExcel('diger_kesintiler_' + V.donem, [['pmKesOzet', 'Kişi Bazında Kesintiler'], ['pmAvansTablo', 'Şahsi Avans Talepleri ve Ödemeleri'], ['pmPlanTablo', 'Kesinti ve İcra Planları']]));
    $('#pmMesaiTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr) talepKartAc('m', +tr.dataset.id); });
    $('#pmAvansTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr) talepKartAc('a', +tr.dataset.id); });
    $('#pmTalepKapat').addEventListener('click', talepKartKapat); $('#pmTalepPerde').addEventListener('click', talepKartKapat);
    $('#pmKesFormAc').addEventListener('click', () => { const f = $('#pmKesForm'); f.style.display = f.style.display === 'none' ? '' : 'none'; if (f.style.display !== 'none') { kesintiCiz(); $('#pmkf_personel').focus(); } });
    $('#pmKesFormKapat').addEventListener('click', () => { $('#pmKesForm').style.display = 'none'; });
    $('#pmKesFormu').addEventListener('submit', kesintiFormGonder); ['#pmkf_toplam', '#pmkf_taksit', '#pmkf_baslangic'].forEach(s => $(s).addEventListener('input', kesintiOzet));
    // Muhasebe Kayıtları
    $('#pmMhDetay').addEventListener('change', muhasebeCiz); $('#pmMhEslemeAc').addEventListener('click', () => { mhEslemeAcik = !mhEslemeAcik; $('#pmMhEsleme').style.display = mhEslemeAcik ? '' : 'none'; });
    $('#pmMhSimTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr) { mhSecili = +tr.dataset.id; mhFisF = ''; muhasebeCiz(); } });
    $('#pmMhEslemeForm').addEventListener('submit', mhEslemeKaydet); $('#pmme_temizle').addEventListener('click', mhEslemeTemizle);
    $('#pmme_hesap').addEventListener('input', () => { const h = (V.muhasebe.hesaplar || []).find(x => x.kod === $('#pmme_hesap').value.trim()); $('#pmme_hesapAd').value = h ? h.ad : ''; });
    $('#pmMhExcel').addEventListener('click', () => domExcel('muhasebe_kayitlari_' + V.donem, [['pmMhSimTablo', 'Simülasyonlar'], ['pmMhFisTablo', 'Fişler'], ['pmMhOzetTablo', 'Özet Simülasyon — Muhasebeye Atılacak Kayıtlar']].concat(mhDetay ? [['pmMhSatirTablo', 'Personel Bazlı Satırlar (Yetkili)']] : [])));
    // satır düzenleme: üretim, prim, kesinti planı, birleşim
    $('#pmUretimTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr && tr.dataset.id) uretimDuzenle(+tr.dataset.id); });
    $('#pmPrimTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr && tr.dataset.id) primDuzenle(+tr.dataset.id); });
    $('#pmBirlesimTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr && tr.dataset.kaynak) birlesimSatirAc(tr); });
    $('#pmPlanTablo tbody').addEventListener('click', ev => { if (ev.target.closest('a, label, input, button')) return; const tr = ev.target.closest('tr[data-id]'); if (tr) planDuzenle(+tr.dataset.id); });
    // her satır tıklanabilir: kişi özetleri → kart; prim uyumu → kart; senaryo → kart / üretim; özet raporu → filtreye in
    ['#pmEkOzet tbody', '#pmKesOzet tbody'].forEach(s => $(s).addEventListener('click', ev => { if (etkilesim(ev)) return; const tr = ev.target.closest('tr[data-sira]'); if (tr) kisiKartAc(+tr.dataset.sira); }));
    $('#pmPrimUyum tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-ad]'); if (tr) kisiKartAc(0, tr.dataset.ad); });
    $('#pmCikisTablo tbody').addEventListener('click', ev => { if (etkilesim(ev)) return; const tr = ev.target.closest('tr[data-k]'); if (!tr) return; if (tr.dataset.kaynak === 'Üretim') uretimDuzenle(+tr.dataset.usira); else kisiKartAc(+tr.dataset.sira0); });
    const filtreyeIn = (anahtar, deger, sgk) => {   // özet satırı → ilgili ana filtre seçilir, Personel Maliyet Tablosu'na dönülür
      const hedef = { catiDepartman: '#pmCati', sgkIsvereni: '#pmSgk', fiiliGorevYeri: '#pmYer', bolum: '#pmBolum', unvan: '#pmUnvan', ozelDurum: '#pmOzel' }[anahtar];
      if (anahtar === 'kaynak') { sekme(deger === 'Özel' ? 'ozel' : deger === 'Üretim' ? 'uretim' : 'tablo'); return; }
      if (!hedef) return; const e = $(hedef); const v = anahtar === 'ozelDurum' && deger === 'Özel Durumu Yok' ? '_yok' : deger; if ([...e.options].some(o => o.value === v)) e.value = v; else return;
      if (sgk) $('#pmSgk').value = [...$('#pmSgk').options].some(o => o.value === sgk) ? sgk : '';
      const ozelMi = V.personel.some(p => p.ozel && (p.durum || 'Aktif') === 'Aktif' && (anahtar === 'catiDepartman' ? p.catiDepartman : anahtar === 'sgkIsvereni' ? p.sgkIsvereni : anahtar === 'bolum' ? p.bolum : anahtar === 'unvan' ? p.unvan : anahtar === 'fiiliGorevYeri' ? p.fiiliGorevYeri : p.ozelDurum) === deger) && !V.personel.some(p => !p.ozel && (p.durum || 'Aktif') === 'Aktif' && (anahtar === 'catiDepartman' ? p.catiDepartman : anahtar === 'sgkIsvereni' ? p.sgkIsvereni : anahtar === 'bolum' ? p.bolum : anahtar === 'unvan' ? p.unvan : anahtar === 'fiiliGorevYeri' ? p.fiiliGorevYeri : p.ozelDurum) === deger);
      sekme(ozelMi ? 'ozel' : 'tablo'); ciz(); mesaj('info', `Filtre uygulandı: ${esc(KIRILIM_AD[anahtar] || anahtar)} = ${esc(deger)}${sgk ? ' · SGK İşvereni = ' + esc(sgk) : ''}. Temizle ile kaldırın.`);
    };
    $('#pmKirilimTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-deger]'); if (tr) filtreyeIn($('#pmKirilim').value, tr.dataset.deger); });
    $('#pmMatris tbody').addEventListener('click', ev => { const td = ev.target.closest('td[data-sgk], td[data-cati]'); if (!td) return; if (td.dataset.cati) filtreyeIn('catiDepartman', td.dataset.cati, td.dataset.sgk); else filtreyeIn('sgkIsvereni', td.dataset.sgk); });
    ['#pmDagUnvan', '#pmDagOzel', '#pmDagKidem', '#pmDagKaynak'].forEach(s => $(s + ' tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-anahtar]'); if (tr) filtreyeIn(tr.dataset.anahtar, tr.dataset.deger); }));
    $('#pmMhSecili').addEventListener('click', ev => {   // muhasebe: özet satırı / fiş satırı → fiş önizleme; detay satırı → kart; hatalı kontrol → hataları göster
      const s = mhSim(); if (!s || etkilesim(ev)) return;
      const oz = ev.target.closest('#pmMhOzetTablo tbody tr[data-fis]'); if (oz) { mhFisOnizle(s, oz.dataset.fis); return; }
      const fs = ev.target.closest('#pmMhFisTablo tbody tr[data-fis]'); if (fs) { mhFisOnizle(s, fs.dataset.fis); return; }
      const st = ev.target.closest('#pmMhSatirTablo tbody tr[data-sira]'); if (st) { kisiKartAc(+st.dataset.sira); return; }
      const kt = ev.target.closest('.pm-kontrol tr.hata, .pm-kontrol tr.uyari'); if (kt && !mhHata) { mhHata = true; mhSeciliCiz(s); }
    });
    $('#pmUretimEkle').addEventListener('click', () => uretimDuzenle(0)); $('#pmPrimEkle').addEventListener('click', () => primDuzenle(0));
    // işten çıkış senaryosu
    ['#pmCikisAra', '#pmCikisKaynak', '#pmCikisSecili', '#pmKidemTavan'].forEach(s => { $(s).addEventListener('input', cikisCiz); $(s).addEventListener('change', cikisCiz); });
    $('#pmCikisTemizle').addEventListener('click', () => { cikisSecim.clear(); cikisSakla(); cikisCiz(); });
    $('#pmCikisExcel').addEventListener('click', () => domExcel('isten_cikis_senaryosu_' + V.donem, [['pmCikisTablo', 'İşten Çıkış Senaryosu']]));
    $('#pmNCikis').textContent = cikisSecim.size;
    $('#pmUretimExcel').addEventListener('click', () => ekExcel('uretim')); $('#pmPrimExcel').addEventListener('click', () => ekExcel('prim')); $('#pmBirlesimExcel').addEventListener('click', () => ekExcel('birlesim'));
    ['#pmAra', '#pmDurum', '#pmSgk', '#pmYer', '#pmCati', '#pmBolum', '#pmUnvan', '#pmOzel'].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    $('#pmTemizle').addEventListener('click', () => { $('#pmAra').value = ''; ['#pmSgk', '#pmYer', '#pmCati', '#pmBolum', '#pmUnvan', '#pmOzel'].forEach(s => $(s).value = ''); $('#pmDurum').value = 'Aktif'; ciz(); });
    $('#pmExcel').addEventListener('click', excel); $('#pmOzetExcel').addEventListener('click', ozetExcel); $('#pmKirilim').addEventListener('change', cizOzet);
    $('#pmDuzenle').addEventListener('click', () => modDegis(true)); $('#pmVazgec').addEventListener('click', () => modDegis(false)); $('#pmKaydet').addEventListener('click', kaydet); $('#pmEkle').addEventListener('click', () => kartAc(0));
    $('#pmTablo tbody').addEventListener('click', ev => { const k = ev.target.closest('button.kart'); if (k) { kartAc(+k.dataset.sira); return; } const s = ev.target.closest('button.sil'); if (s) { sil(+s.dataset.sira); return; } if (duzenleme || etkilesim(ev)) return; const tr = ev.target.closest('tr[data-sira]'); if (tr) kartAc(+tr.dataset.sira); });   // satıra tıkla → kart
    $('#pmTop10 tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr.kartAc'); if (!tr) return; const p = bul(+tr.dataset.sira); if (!p) return; if (!!p.ozel !== (kapsam === 'ozel')) kapsam = p.ozel ? 'ozel' : 'bordro'; kartAc(p.sira); });
    $('#pmKartKapat').addEventListener('click', kartKapat); $('#pmPerde').addEventListener('click', kartKapat);
    $$('#pmSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    // yoğun yerleşim: rapor açıklaması "i" ile açılır; şerit alt yazıları başlık ipucunda; pencere boyutu değişince tablo kabı yeniden ölçülür (KPI kutuları ortak na-kpi ölçüsünde)
    $('#pmBilgi').addEventListener('click', () => { kok.classList.toggle('aciklama'); kapBoyut(); });
    $$('.pm-rapor-ust').forEach(u => { const h = u.querySelector('h5'), a = u.querySelector('.alt:not(#pmOzetKapsam)'); if (h && a) h.title = a.textContent.trim(); });
    window.addEventListener('resize', () => { clearTimeout(kapBoyut.z); kapBoyut.z = setTimeout(sabitleHepsi, 120); });
    kayitBilgi(); ciz(); sekme(kok.dataset.baslangic || 'tablo'); window.NA_HAZIR = true;
    const kod = kok.dataset.kod; if (kod) { const p = V.personel.find(x => x.cariKodu === kod); if (p) { if (!!p.ozel !== (kapsam === 'ozel')) sekme(p.ozel ? 'ozel' : 'tablo'); kartAc(p.sira); } }
  }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
