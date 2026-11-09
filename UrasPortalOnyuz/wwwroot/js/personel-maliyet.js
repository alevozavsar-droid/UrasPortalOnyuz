// ============================================================
// PERSONEL MALİYET RAPORU — ekran betiği (Views/PersonelMaliyet/Index.cshtml; veri /PersonelMaliyet/Veri; kayıt Kaydet / Ekle / Sil)
// "Personel Bordro ve Ödeme Tablosu" düzeni. Sütunlar sunucudan gelir (V.sutunlar: grup → ad, anahtar, tip, liste); tablo, dip toplam, kart ve Excel bu listeden kurulur.
//   Tipler: sira · metin · ad · tarih · secim · sayi (girilen tutar) · rx (R üstte / X altta) · gun (girilen gün) · hesap (hesaplanan) · genel (genel maliyet) · not · kart
// Özel Kişiler sekmesi: aynı tablo paneli "kapsam = ozel" ile (p.ozel = true satırlar; düzenleme, kart, Excel aynen). Ana tablo (kapsam bordro) bordrolu, özel ve SGK'sız üretim satırlarını birlikte gösterir; "Özel Kişiler Gizle" / "Üretim Personeli Gizle" ile ayrılır. Üretim satırları salt okunur, tıklayınca üretim düzenlemesi açılır.
// Ek sekmeler (salt okunur, V.ekSutunlar + V.uretim / V.primler): Üretim Personeli (SGK'sız, saat / gün bazlı X) · Prim Ödemeleri (ana tablodaki primin detayı + kişi bazında uyum) ·
//   Birleşik Tablo (Bordro + Özel + Üretim; net kalemler, Toplam Brüt Masraflar, Toplam İşveren, Genel Maliyet). "0 ₺ Sütunları Gizle" anahtarı tüm tablolarda. Kira sütunu (R / X, Mesai'nin solunda; 6 Kas 2026) ve "Çoklu Satırları Birleştir" anahtarı (aynı kişinin satırları tek satırda, yalnız bu sekmede).
//   Üretim Personeli / Özel Kişiler Gizle: kişi içeren tüm sekmelerde tek ortak durum (sayılar ve toplamlar sekmeler arasında uyumlu). Zam Senaryosu: toplu ve kişi bazlı R / X zam oranları; mevcut ve zamlı ara toplam ve genel maliyet; senaryolar kaydedilir ve Senaryo Karşılaştırması sayfasında yan yana görünür (veri değişmez).
// Ortak: filtreler (durum, SGK işvereni, fiili görev yeri, çatı departman, bölüm, unvan, özel durum, arama), KPI kutuları.
// Sekme "Personel Maliyet Tablosu": Düzenle modunda kimlik bilgileri (liste + serbest yazım), günler ve girilen tutarlar satır bazında değiştirilir; hesaplanan sütunlar
//   sunucuyla aynı bordro kuralıyla (hesapla) anında yenilenir; Kaydet toplu kaydeder. Her satırda Kart (Personel Cari Kartı çekmecesi) ve Sil. Personel Ekle yeni kart açar.
// Sekme "Özet Raporu": seçilebilir kırılım × maliyet kalemleri, SGK işvereni × çatı departman matrisi, maliyet kalemi dağılımı, unvan / özel durum / kıdem dağılımları, en yüksek maliyetli 10 personel, Özet Excel.
// Excel (tüm sekmeler): ekrandaki tablo göründüğü gibi .xlsx olur — ortak bileşen wwwroot/js/tablo-excel.js (TabloExcel.indir): başlık grupları, renkler, R üstte / X altta hücreler,
//   dip toplam, dondurulmuş başlık ve sol sütunlar, KPI kutuları, açıklama notu; gizli (0 ₺) sütunlar ve Kart sütunu dışarıda. Dosya sunucuda üretilir (POST /TabloExcel/Indir).
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Özel Durumu Yok", "İşten Ayrıldı", "10 Yıl ve Üzeri").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('pm'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const kurus = v => { const n = Math.round((Number(v) || 0) * 100) / 100; return Math.abs(n) < 0.005 ? 0 : n; };   // kayan nokta artığı (-0, 1e-13) → 0
  const kisa = v => ondalik(kurus(v), 2) + ' ₺';   // tüm tutarlar ₺ ile, virgülden sonra iki hane (9.230,79 ₺)
  const hucre = v => kurus(v) ? kisa(v) : '<span class="text-muted">—</span>';
  const ondalik = (v, b) => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: b, maximumFractionDigits: b });
  /** dövizli maaşta X-Net (₺): KalanX → toplam − R sabit; TamamiR → 0; TamamiX → toplam (sunucu ile aynı kural) */
  const dovizX = p => { const t = Math.round(Number(p.dovizTutar) * Number(p.kur) * 100) / 100; return p.dovizOdeme === 'TamamiR' ? 0 : p.dovizOdeme === 'TamamiX' ? t : Math.max(0, Math.round((t - Number(p.rNet || 0)) * 100) / 100); };
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
  const METIN = ['sgkIsvereni', 'fiiliGorevYeri', 'adSoyad', 'unvan', 'bolum', 'gorev', 'iseGiris', 'catiDepartman', 'ozelDurum', 'departmanKodu', 'departmanAciklamasi', 'sicilNo', 'sgkNo', 'durum', 'ayrilisTarihi', 'tcKimlik', 'telefon', 'eposta', 'doviz', 'not'];
  // özet kırılım kalemleri: [anahtar, başlık, X anahtarı (varsa → R üstte / X altta)]
  const KALEM = [['xNet', 'X-Net'], ['rNet', 'R-Net'], ['toplamNet', 'Toplam Net'], ['toplamEkCalisma', 'Ek Çalışma', 'toplamEkCalismaX'], ['toplamSosyal', 'Sosyal Yrd.', 'toplamSosyalX'], ['toplamPrim', 'Prim', 'toplamPrimX'], ['toplamYanHaklar', 'Yan Haklar'], ['toplamYasal', 'Yasal Öd.'], ['topEk', 'Ek Ödemeler Toplam', 'topEkX'], ['kesintiler', 'Toplam Kesintiler', 'kesintilerX'],
    ['toplamBrutKazanc', 'Toplam Brüt Kazanç'], ['sgkIsciToplam', 'SGK İşçi'], ['gvTutari', 'Gelir Vergisi'], ['dvTutari', 'Damga Vergisi'], ['sgkIsverenToplam', 'SGK İşveren'], ['genelMaliyet', 'Genel Maliyet']];
  const KALEM_GRUPBAS = ['toplamEkCalisma', 'topEk', 'kesintiler', 'toplamBrutKazanc', 'sgkIsciToplam', 'sgkIsverenToplam', 'genelMaliyet'];
  /** kişi satırına özet türev alanları ekler: Ek Ödemeler Toplam R / X, Toplam Kesintiler R / X */
  const ozetAlanlari = p => { const n = k => Number(p[k]) || 0; return { ...p, topEk: n('toplamEkCalisma') + n('toplamSosyal') + n('toplamPrim') + n('toplamYanHaklar') + n('toplamYasal') + n('digerEkOdemelerR'), topEkX: n('toplamEkCalismaX') + n('toplamSosyalX') + n('toplamPrimX'), kesintiler: n('kesintilerToplami'), kesintilerX: n('kesintilerToplamiX') }; };
  const KIRILIM_AD = { kaynak: 'Kaynak (Bordro / Özel / Üretim)', catiDepartman: 'Çatı Departman', sgkIsvereni: 'SGK İşvereni', fiiliGorevYeri: 'Fiili Görev Yeri', bolum: 'Bölüm', unvan: 'Unvan', gorev: 'Görev', departmanKodu: 'Departman Kodu', ozelDurum: 'Özel Durumu', kidemAralik: 'Kıdem Aralığı' };
  const KIDEM_SIRA = ['1 Yıldan Az', '1–3 Yıl', '3–5 Yıl', '5–10 Yıl', '10 Yıl ve Üzeri', 'Tarih Yok'];
  const kidemAralik = p => p.kidemYil === null || p.kidemYil === undefined ? 'Tarih Yok' : p.kidemYil < 1 ? '1 Yıldan Az' : p.kidemYil < 3 ? '1–3 Yıl' : p.kidemYil < 5 ? '3–5 Yıl' : p.kidemYil < 10 ? '5–10 Yıl' : '10 Yıl ve Üzeri';
  const kirilimDeger = (p, k) => k === 'kidemAralik' ? kidemAralik(p) : k === 'ozelDurum' ? (p.ozelDurum || 'Özel Durumu Yok') : (p[k] || '—');
  let V = null, L = [], duzenleme = false, taslak = {}, aktifSekme = 'tablo', kartSira = null, kartDegisti = false, kapsam = 'bordro';   // kapsam: bordro (Personel Maliyet Tablosu: tüm kişiler) | ozel (Özel Kişiler: p.ozel = true, aynı panel)   // taslak: sira → { alan: değer }; kartSira: açık kart (0 = yeni); kartDegisti: kartta tutar değişti
  let SUTUN = [], GIRIS = [], GUN = [], RX = [], RXX = [], XALAN = [], ETIKET = {};   // XALAN: bordroya girmeyen X tarafı alanlar (kaynak değerleri düşürmez)   // sunucudaki sütun tanımından: tüm sütunlar, girilen sayısal alanlar, gün alanları, rx (R üstte / X altta) anahtarları ve X alt alanları, anahtar → başlık
  /** Üretim Personeli / Özel Kişiler Gizle: kişi içeren tüm sekmelerde tek ortak durum (sayılar ve toplamlar sekmeler arasında uyumlu kalır) */
  const gizle = { uretim: false, ozel: false };
  const gizliKaynak = k => (k === 'Özel' && gizle.ozel) || (k === 'Üretim' && gizle.uretim);
  /** kişi kaydının kaynağı (sıra ya da ad ile): Özel, Bordro; adı SGK'sız üretim kaydında geçen kişi Üretim */
  const kayitKaynak = (sira, ad) => { const p = kisiBul(sira, ad); return p ? (p.ozel ? 'Özel' : 'Bordro') : (V.uretim || []).some(u => !u.sgkLi && u.adSoyad === ad) ? 'Üretim' : 'Bordro'; };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 8000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  async function post(url, veri) { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }); return r.json(); }
  /** ekrandaki tabloları göründüğü gibi Excel'e ya da PDF'e (bicim: 'xlsx' | 'pdf'; TabloExcel: başlık grupları, renkler, R/X hücreleri, dip toplam, dondurulmuş başlık ve sol sütunlar, KPI, not); dosya adı dönemle biter */
  function tabloExcel(dosya, sayfalar, bicim = 'xlsx') {
    if (!window.TabloExcel) { mesaj('danger', 'Excel bileşeni yüklenemedi; sayfayı yenileyip tekrar deneyin.'); return; }
    const pdf = bicim === 'pdf';
    (pdf ? window.TabloExcel.pdf : window.TabloExcel.indir)(`${dosya}_${V.donem}`, sayfalar).catch(e => mesaj('danger', (pdf ? 'PDF' : 'Excel') + ' indirilemedi: ' + esc(e.message)));
  }
  /** Excel ve PDF düğmelerini bağlar (ortak TabloExcel.dugme): fn('xlsx') ya da fn('pdf') çağrılır */
  const dugmeBagla = (xl, pd, fn) => { if (window.TabloExcel) window.TabloExcel.dugme(xl, pd, fn); };
  /** sekme şeridindeki başlık (ör. "Eylül 2026 Birleşik Personel Maliyeti"); şeritte başlık yoksa sekme düğmesinin adı */
  const panelBaslik = p => { const h = $(`.na-panel[data-p="${p}"] .pm-rapor-ust h5`); if (h) return h.textContent.trim(); const b = $(`#pmSekmeler button[data-p="${p}"]`); return b ? b.textContent.replace(/\d+\s*$/, '').trim() : ''; };

  // ---- sekmeler
  /** genel arama (#pmAra) ana listeyi süzer; sekmenin yeşil şeridinin sağına taşınır. Listeyi süzmeyen sekmelerde (Prim, Üretim, Muhasebe) gizlenir: onların kendi arama kutusu yoksa arama da yoktur */
  const GENEL_ARAMALI = ['tablo', 'ozel', 'ozet', 'zam', 'karsilastirma', 'ekcalisma', 'kesinti', 'birlesim', 'cikis', 'odeme', 'cari', 'ozelrapor'];
  const VARSAYILAN_SEKME = 'cari';   // panel bu sekmeyle açılır (kullanıcı, 6 Kas 2026: "Konsolide İK Paneli'ni açmak istediğimizde bu sayfa ile açılsın"); adreste ?sekme yoksa bu, görünümdeki varsayılan da aynı
  function genelAramaYerlestir(p, panel) {
    const kutu = $('#pmAra'), yuva = GENEL_ARAMALI.includes(p) ? $(`.na-panel[data-p="${panel}"] .ayar`) : null;
    if (yuva) yuva.prepend(kutu);
    kutu.style.display = yuva ? '' : 'none';
  }
  function sekme(p) {
    aktifSekme = p; const sek = p === 'karsilastirma' ? 'zam' : p; $$('#pmSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === sek));
    if (V) yerFiltreModu(p === 'cikis');   // senaryoda Görev Yeri = İstanbul / Çorlu
    const panel = p === 'ozel' ? 'tablo' : p; genelAramaYerlestir(p, panel); $$('#pm > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === panel));
    const kendiKpi = $(`[data-kpi="${panel}"]`);   // sekmenin KPI kutuları filtrenin üstünde, genel şeridin yerinde; diğer sekmelerin kutuları gizli
    $$('[data-kpi]').forEach(k => { k.style.display = k === kendiKpi ? '' : 'none'; });
    $('#pmKpi').style.display = (p === 'karsilastirma' || p === 'birlesim' || p === 'ozelrapor' || kendiKpi) ? 'none' : '';   // kendi KPI kutusu olan sekmede genel şerit gizlenir; Birleşik ve Özel Rapor'da KPI kutusu yok
    if (p === 'tablo' || p === 'ozel') {   // Özel Kişiler: aynı tablo paneli, kapsam = ozel (p.ozel = true satırlar)
      const yeni = p === 'ozel' ? 'ozel' : 'bordro';   // şerit başlığı yok (sekme adıyla aynıydı); kapsam ve dönem sayaçta (#pmSayi)
      if (yeni !== kapsam) { kapsam = yeni; if (duzenleme) modDegis(false); else if (V) ciz(); }
    }
    if (V && p === 'cari') cariCiz(); else if (V && p === 'ozelrapor') ozelRaporCiz(); else if (V && p === 'tesvik') tesvikCiz(); else if (V && p === 'ozet') cizOzet(); else if (V && EK[p]) ekCiz(p); else if (V && p === 'ekcalisma') ekCalismaCiz(); else if (V && p === 'kesinti') kesintiCiz(); else if (V && p === 'muhasebe') { muhasebeCiz(); mhTazele(); } else if (V && p === 'cikis') cikisCiz(); else if (V && p === 'odeme') odemeCiz(); else if (V && p === 'zam') zamCiz(); else if (V && p === 'karsilastirma') zamKarsCiz();
    if (V) sabitleHepsi();
    try { const u = new URL(location.href); if (p === VARSAYILAN_SEKME) u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }

  // ---- bordro hesabı (sunucudaki PmBordro.Hesapla ile aynı kural ve aynı parametreler). R = bordrolu ücret, X = bordro dışı ödeme.
  //      brutKaynak / isverenKaynak: kaynak tablodan gelen Toplam Brüt Maliyet ve İş Veren; varsa hesap bunlara bağlanır (tutar değişince düşer).
  /** fiili görev yeri → tüzel şirket (sunucudaki FiiliSirketler ile aynı; büyük-küçük harf duyarsız); eşleşmezse SGK işvereni */
  const fiiliSirket = p => { const k = (p.fiiliGorevYeri || '').toLocaleLowerCase('tr-TR'), e = Object.entries(V.odemeSirketi.fili).find(([ad]) => ad.toLocaleLowerCase('tr-TR') === k); return e ? e[1] : p.sgkIsvereni; };
  /** şirketin teşvik seçimi (V.tesvik.sirketOran, Şirket Teşvikleri sekmesi): toplam puan, aylık destek, engelli puanı, istisna bayrakları; seçim yoksa istisnalar açık, teşvik yok */
  const tesvikOran = p => (V.tesvik && V.tesvik.sirketOran && V.tesvik.sirketOran[p.sgkIsvereni]) || { puan: 0, puanEmekli: 0, aylik: 0, aylikEmekli: 0, engelliPuan: 0, gvIstisna: true, dvIstisna: true };
  /** kişi bazında teşvik indirimi (sunucu PersonelTesvikOrnek.Indirim ile aynı): SGK matrahı × puan + aylık destek × prim günü / 30 + engelli teşviki (asgari ücret üzerinden) */
  function tesvikHesapla(p, tes, emekli) {
    const m = Number(p.sgkMatrahi) || 0; if (m <= 0) return 0;
    const puan = emekli ? tes.puanEmekli : tes.puan, aylik = emekli ? tes.aylikEmekli : tes.aylik, gun = Math.min(30, Math.max(0, Number(p.primGun) || 0));
    let t = m * puan / 100 + aylik * gun / 30; if (p.ozelDurum === 'Engelli' && !emekli) t += Math.min(m, V.parametre.asgariBrut) * tes.engelliPuan / 100;
    return Math.round(t * 100) / 100;
  }
  function hesapla(p) {
    const P = V.parametre, n = k => Number(p[k]) || 0, R = v => Math.round(v * 100) / 100;
    const emekli = p.ozelDurum === 'Emekli', ay = P.ay, tavan = P.sgkTavan;
    const isciOran = emekli ? P.isciSgdp : P.isciIssizlik + P.isciMyo + P.isciGss;
    const tarife = k => { let v = 0, alt = 0; for (const [ust, oran] of P.dilimler) { if (k <= alt) break; v += (Math.min(k, ust) - alt) * oran; alt = ust; } return v; };
    const aylikGv = (m, a) => m <= 0 ? 0 : tarife(m * a) - tarife(m * (a - 1));
    const tes = tesvikOran(p), istisna = tes.gvIstisna ? R(aylikGv(P.asgariGvMatrahi, ay)) : 0, asgDamga = tes.dvIstisna ? P.asgariDamga : 0;   // istisnalar şirket seçimine bağlı (Şirket Teşvikleri sekmesi)
    let rNetE = n('rNet'), xNet = n('xNet'); if (dovizli(p)) { const toplam = R(n('dovizTutar') * n('kur')), od = p.dovizOdeme || ''; if (od === 'TamamiR') { rNetE = toplam; xNet = 0; } else if (od === 'TamamiX') { rNetE = 0; xNet = toplam; } else { xNet = Math.max(0, R(toplam - n('rNet'))); } }   // dövizli maaşta X-Net ₺ karşılığı
    p.xNet = xNet; p.xNet2 = xNet; p.toplamNet = xNet + rNetE;
    // grup toplamları (bordrodaki ekler)
    p.toplamEkCalisma = n('fazlaMesai') + n('bayramCalisma') + n('geceFarki') + n('pazarCalisma') + n('resmiTatilCalisma');
    p.toplamSosyal = n('yemek') + n('yol') + n('servis') + n('kiraDestegi') + n('egitim') + n('cocukYardimi');
    p.toplamPrim = n('maasFarki') + n('prim') + n('ozelOdeme') + n('kasaTazminati');
    p.toplamYanHaklar = n('ozelSaglik') + n('kiyafet') + n('koruyucuEkipman') + n('dogumYardimi') + n('evlilikYardimi');
    p.toplamYasal = n('kidemTazminati') + n('ihbarTazminati') + n('iseIade') + n('yillikIzinOdemesi') + n('raporFarki') + n('dogumIzni');
    const ekler = p.toplamEkCalisma + p.toplamSosyal + p.toplamPrim + p.toplamYanHaklar + p.toplamYasal, kaynak = n('brutKaynak') > 0;
    // 1) esas brüt: kaynak tablodan ya da R-Net'ten brütleştirme (net(B) artan olduğundan ikiye bölme)
    const net = b => { const isci = Math.min(b, tavan) * isciOran; const gv = Math.max(0, aylikGv(b - isci, ay) - istisna); const dv = Math.max(0, b * P.damga - asgDamga); return b - isci - gv - dv; };
    let brut = 0;
    if (kaynak) brut = R(Math.max(0, n('brutKaynak') - ekler));
    else if (rNetE > 0) { let lo = 0, hi = rNetE * 3 + 50000; for (let i = 0; i < 80; i++) { const mid = (lo + hi) / 2; if (net(mid) < rNetE) lo = mid; else hi = mid; } brut = R(hi); }
    const gunluk = brut / 30;
    p.brutUcret = brut; p.gunluk = R(gunluk);
    // 2) puantaj kazançları
    p.normalKazanc = R(n('normalGun') * gunluk); p.haftaSonuKazanc = R(n('haftaSonuGun') * gunluk); p.resmiTatilKazanc = R(n('resmiTatilGun') * gunluk); p.bayramKazanc = R(n('bayramGun') * gunluk); p.ucretliIzinKazanc = R(n('ucretliIzinGun') * gunluk);
    let kazanclar = p.normalKazanc + p.haftaSonuKazanc + p.resmiTatilKazanc + p.bayramKazanc + p.ucretliIzinKazanc;
    if (kaynak && n('normalGun') + n('haftaSonuGun') + n('resmiTatilGun') + n('bayramGun') + n('ucretliIzinGun') === 30) { p.normalKazanc = R(p.normalKazanc + brut - kazanclar); kazanclar = brut; }
    // 3) brüt kazanç ve SGK matrahı
    p.toplamBrutKazanc = R(kazanclar + ekler);
    p.sgkMatrahi = Math.min(kazanclar + p.toplamEkCalisma + p.toplamPrim + (p.toplamSosyal - n('servis') - n('yol')), tavan);   // yol ve servis SGK matrahı dışında
    p.tesvikIndirimi = tesvikHesapla(p, tes, emekli);   // şirketin faydalandığı teşvikler (seçime göre); Toplam İşveren brüt kalır, teşvik ayrı sütunlarda düşülür
    // 4) SGK işçi
    p.isciIssizlik = emekli ? 0 : R(p.sgkMatrahi * P.isciIssizlik); p.isciMyo = emekli ? 0 : R(p.sgkMatrahi * P.isciMyo); p.isciGss = emekli ? 0 : R(p.sgkMatrahi * P.isciGss); p.isciSgdp = emekli ? R(p.sgkMatrahi * P.isciSgdp) : 0;
    p.sgkIsciToplam = p.isciIssizlik + p.isciMyo + p.isciGss + p.isciSgdp;
    // 5) damga vergisi
    p.damgaMatrahi = R(Math.max(0, p.toplamBrutKazanc - p.toplamYanHaklar)); p.asgariDamga = p.damgaMatrahi > 0 ? asgDamga : 0; p.dvTutari = Math.max(0, R(p.damgaMatrahi * P.damga - p.asgariDamga));
    // 6) gelir vergisi
    p.gvMatrahi = R(Math.max(0, p.toplamBrutKazanc - p.toplamYasal - p.sgkIsciToplam));
    p.kumulatifMatrah = R(p.gvMatrahi * ay); p.asgariIstisna = p.gvMatrahi > 0 ? istisna : 0; p.asgariKumulatifMatrah = R(P.asgariGvMatrahi * ay);
    if (kaynak) { p.gvTutari = Math.max(0, R(p.toplamBrutKazanc - (rNetE + ekler) - p.sgkIsciToplam - p.dvTutari)); p.hesaplananGv = R(p.gvTutari + p.asgariIstisna + n('gvIndirimleri') + n('ozelSigortaIndirimi')); }
    else { p.hesaplananGv = R(aylikGv(p.gvMatrahi, ay)); p.gvTutari = Math.max(0, R(p.hesaplananGv - p.asgariIstisna - n('gvIndirimleri') - n('ozelSigortaIndirimi'))); }
    // 7) SGK işveren
    p.isvIssizlik = emekli ? 0 : R(p.sgkMatrahi * P.isvIssizlik); p.isvMyo = emekli ? 0 : R(p.sgkMatrahi * P.isvMyo); p.isvGss = emekli ? 0 : R(p.sgkMatrahi * P.isvGss); p.isvSgdp = emekli ? R(p.sgkMatrahi * P.isvSgdp) : 0;
    p.kvsk = R(p.sgkMatrahi * P.kvsk); p.hazineIndirimi = emekli ? 0 : R(p.sgkMatrahi * P.hazineIndirimi);
    if (n('isverenKaynak') > 0) {
      const z = n('isverenKaynak'), top = p.isvIssizlik + p.isvMyo + p.isvGss + p.isvSgdp + p.kvsk; p.hazineIndirimi = n('hazineKaynak');   // kaynaktaki hazine indirimi yalnız gösterim
      if (top > 0) { const k = z / top; p.isvIssizlik = R(p.isvIssizlik * k); p.isvGss = R(p.isvGss * k); p.kvsk = R(p.kvsk * k); p.isvSgdp = R(p.isvSgdp * k); p.isvMyo = R(p.isvMyo * k); }
      const fark = R(z - (p.isvIssizlik + p.isvMyo + p.isvGss + p.isvSgdp + p.kvsk)); if (emekli) p.isvSgdp = R(p.isvSgdp + fark); else p.isvMyo = R(p.isvMyo + fark);
      p.sgkIsverenToplam = z;
    } else { p.hazineIndirimi = p.tesvikIndirimi; p.sgkIsverenToplam = p.isvIssizlik + p.isvMyo + p.isvGss + p.isvSgdp + p.kvsk; }   // kaynak yoksa işveren brüt (resmi bordro gibi), indirim = seçime göre teşvik
    // 8) ödeme özeti ve maliyet (R bordrolu, X bordro dışı)
    // banka = anlaşılan net R-Net + R ek ödemeler − kesintiler (brütten türetilmez)
    p.rNet2 = rNetE; p.kesintilerToplami = n('avans') + n('icra') + n('borc') + n('ozelSigortaKesintisi') + n('sendikaKatkisi') + n('bes') + n('mazotKesintisi') + n('ihbarKesintisi'); p.digerEkR = R(ekler + n('digerEkOdemelerR')); p.kalanNetBanka = R(rNetE + p.digerEkR - p.kesintilerToplami);
    p.toplamEkCalismaX = n('fazlaMesaiX'); p.toplamSosyalX = n('yemekX') + n('yolX') + n('kiraDestegiX'); p.toplamPrimX = n('primX');   // X tarafı grup toplamları: brüt ve vergiye girmez (Kira Desteği-X dahil, 6 Kas 2026)
    p.digerEkOdemelerX = p.toplamEkCalismaX + p.toplamSosyalX + p.toplamPrimX;
    p.netRBordro = p.kalanNetBanka; p.netXBordro = xNet + p.digerEkOdemelerX; p.kalanNetElden = R(p.netXBordro - n('kesintilerToplamiX'));
    p.odemeSirketiR = p.netRBordro > 0 ? V.odemeSirketi.r : ''; p.odemeSirketiX = p.kalanNetElden > 0 ? fiiliSirket(p) : '';   // ödeme şirketi otomatik: R Uras Power, X fiili görev yeri şirketi
    p.araToplam = p.netXBordro + p.netRBordro; p.toplamBrutMasraflar = R(p.toplamBrutKazanc - p.kalanNetBanka); p.toplamIsveren = p.sgkIsverenToplam;
    p.genelMaliyet = R(p.araToplam + p.toplamBrutMasraflar + p.toplamIsveren);
    p.bankaOdeme = p.bankaBlokeli ? 0 : p.kalanNetBanka; p.nakitR = p.bankaBlokeli ? p.kalanNetBanka : 0; p.eldenOdeme = R(p.kalanNetElden + p.nakitR);   // bloke hesap: R tutarı elden (ödeme kanalı; maliyet değişmez)
    p.isverenNet = R(p.sgkIsverenToplam - p.tesvikIndirimi); p.genelMaliyetTesvik = R(p.genelMaliyet - p.tesvikIndirimi);   // teşvik sonrası (Şirket Teşvikleri seçimine göre)
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
  /** ana filtrelerin yüklemi (durum, SGK işvereni, görev yeri, çatı, bölüm, unvan, özel durum, arama): bordro, özel ve üretim satırlarında aynı */
  function filtreYuklem(durumYoksay) {
    const ara = ($('#pmAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value, oz = v('#pmOzel'), du = durumYoksay ? '' : v('#pmDurum');
    return p => (!du || (p.durum || 'Aktif') === du) && (!v('#pmSgk') || p.sgkIsvereni === v('#pmSgk')) && yerUyar(p.fiiliGorevYeri) && (!v('#pmCati') || p.catiDepartman === v('#pmCati')) && (!v('#pmBolum') || p.bolum === v('#pmBolum')) && (!v('#pmUnvan') || p.unvan === v('#pmUnvan'))
      && (!oz || (oz === '_yok' ? !p.ozelDurum : p.ozelDurum === oz))
      && (!ara || [p.adSoyad, p.cariKodu, p.gorev, p.bolum, p.unvan, p.not, p.sgkIsvereni, p.fiiliGorevYeri, p.catiDepartman, p.departmanKodu, p.departmanAciklamasi, p.sicilNo, p.sgkNo, p.tcKimlik, p.eposta].join(' ').toLocaleLowerCase('tr-TR').includes(ara));
  }
  /** ozelMi açıkça verilirse ya da kapsam ozel ise o gruptaki kişiler; Birleşik, Özet ve kişi listeleri açık değerle çağırır */
  function liste(durumYoksay, ozelMi) {
    const ozel = ozelMi === undefined ? kapsam === 'ozel' : ozelMi, uygun = filtreYuklem(durumYoksay);
    return V.personel.filter(p => !!p.ozel === ozel && uygun(p));
  }
  /** SGK'sız üretim personeli → Personel Maliyet Tablosu satırı (Özet ve Birleşik ile aynı tanım): tamamı X tarafı; hak ediş + tatil kazancı, fazla mesai, yemek, yol ve diğer sosyal X ek ödemesi; genel maliyet = toplam hak ediş */
  const uretimAnaSatir = u => { const n = k => Number(u[k]) || 0, hak = n('hakEdis') + n('tatilKazanci'), mesai = n('fazlaMesaiKazanci'), sosyal = n('toplamSosyal');
    return { kaynak: 'Üretim', uSira: u.sira, sira: -u.sira, adSoyad: u.adSoyad, unvan: 'Gündelikçi', gorev: u.gorev, not: u.not, sgkIsvereni: u.fiiliSirket, odemeSirketiR: '', odemeSirketiX: u.fiiliSirket, durum: 'Aktif', fiiliGorevYeri: u.fiiliGorevYeri, catiDepartman: 'Üretim', bolum: 'Üretim', ozelDurum: 'Gündelikçi', departmanKodu: 'D30', departmanAciklamasi: 'Üretim Departmanı',   // gündelikçi kimlik düzeni (6 Kas 2026): 23 kişide aynı
      xNet: hak, xNet2: hak, toplamNet: hak, yolX: n('yolX'), yemekX: n('yemekX'), kiraDestegiX: n('kiraX'), fazlaMesaiX: mesai, toplamEkCalismaX: mesai, toplamSosyalX: sosyal, digerEkOdemelerX: mesai + sosyal, kesintilerToplamiX: n('kesinti'), kalanNetElden: n('netOdenen'), netXBordro: hak + mesai + sosyal, araToplam: hak + mesai + sosyal, genelMaliyet: n('toplamMaliyet') }; };   // yemek / yol / kira üretim satırında R üstte / X altta gelir; SGK'sızda tamamı X alanında (9 Kas 2026)
  /** SGK'sız üretim personeli kayıtları (ana filtrelerle aynı yüklem; SGK'lı gündelikçi bordroda sayılır) */
  const uretimKayit = durumYoksay => { const uygun = filtreYuklem(durumYoksay); return (V.uretim || []).filter(u => !u.sgkLi && uygun(uretimAnaSatir(u))); };
  /** tüm kişiler: bordrolu, Özel Kişiler ve SGK'sız Üretim Personeli; gizleme anahtarı açık grup çıkar (Personel Maliyet Tablosu, Zam Senaryosu) */
  const tumListe = (durumYoksay, g = gizle) => [...liste(durumYoksay, false), ...(g.ozel ? [] : liste(durumYoksay, true)), ...(g.uretim ? [] : uretimKayit(durumYoksay).map(uretimAnaSatir))];
  /** Personel Maliyet Tablosu satırları: tüm kişiler; Özel Kişiler sekmesinde yalnız özel kişiler */
  function anaListe(durumYoksay) { return kapsam === 'ozel' ? liste(durumYoksay, true) : tumListe(durumYoksay); }
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
  function veriGuncelle(j) { ['personel', 'sonKayit', 'sgkIsverenleri', 'gorevYerleri', 'catiDepartmanlar', 'bolumler', 'unvanlar', 'gorevler', 'ozelDurumlar', 'durumlar', 'sutunlar', 'parametre', 'ekSutunlar', 'uretim', 'primler', 'donem', 'donemAd', 'donemler', 'ekCalisma', 'kesinti', 'muhasebe', 'tesvik', 'odemeler', 'finans'].forEach(k => { if (j[k] !== undefined) V[k] = j[k]; }); sutunlariKur(); listeleriDoldur(); donemKur(); ekKur(); kayitBilgi(); ciz(); }
  /** yeni bordro dönemi (ör. 2026-10): güncel dönemin kopyası; kesinti taksitleri o aya göre yazılır; oluşunca seçili dönem olur */
  async function yeniBordro() {
    if (duzenleme && !window.confirm('Kaydedilmemiş değişiklikler var. Yeni bordro oluşturulursa kaybolur, devam edilsin mi?')) return;
    const ay = window.prompt('Yeni bordro dönemi (yyyy-aa, örn. 2026-10):', '2026-10'); if (!ay) return;
    const kurOku = (s, v) => Number(String(window.prompt(s, v) || '').replace(',', '.'));
    const eur = kurOku('EUR kuru (₺, güncel):', '55.6306'), usd = kurOku('USD kuru (₺, güncel):', '48.9008');
    if (!(eur > 0) || !(usd > 0)) { mesaj('danger', 'EUR ve USD kuru sayı olarak girilmeli.'); return; }
    try { const r = await fetch('/PersonelMaliyet/DonemOlustur', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ donem: ay.trim(), kurlar: { EUR: eur, USD: usd } }) }); const j = await r.json(); if (!j.success) { mesaj('danger', esc(j.message)); return; } if (duzenleme) modDegis(false); veriGuncelle(j); mesaj('success', esc(j.message)); }
    catch (e) { mesaj('danger', 'Yeni bordro oluşturulamadı: ' + esc(e.message)); }
  }
  // ---- dönem (ay) seçimi: seçilen ayın verisi sunucudan yüklenir; kayıt / ekle / sil seçili döneme işlenir
  function donemKur() {
    const sel = $('#pmDonem'); sel.innerHTML = (V.donemler || []).map(d => `<option value="${esc(d.kod)}">${esc(d.ad)}${d.guncel ? ' (Güncel)' : ''}</option>`).join(''); sel.value = V.donem;
    $('#pmDonemPill').textContent = V.donemAd || V.yil;
    const ps = $('#pmPrimDonem'), pd = V.donem; if (ps && [...ps.options].some(o => o.value === pd)) ps.value = pd;   // prim sekmesi: seçili dönemin primleri
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
    const ayrilan = anaListe(true).filter(p => p.durum === 'İşten Ayrıldı').length, du = $('#pmDurum').value;   // Durum filtresinden bağımsız gerçek ayrılan sayısı
    const kpi = [['#0f172a', 'Personel', n, `${L.filter(p => p.ozelDurum === 'Emekli').length} Emekli · ${L.filter(p => p.ozelDurum === 'Gündelikçi').length} Gündelikçi · ${ayrilan} Ayrılan${du === 'Aktif' && ayrilan ? ' (Filtre Dışı)' : ''}`], ['#0f766e', 'Toplam Net Ücret', kisa(top('toplamNet')), `R (Bordro) ${kisa(top('rNet'))} · X ${kisa(top('xNet'))}`],
      ['#1d4ed8', 'Toplam Brüt Kazanç', kisa(top('toplamBrutKazanc')), `SGK Matrahı ${kisa(top('sgkMatrahi'))}`], ['#b45309', 'Vergi ve İşçi Payları', kisa(top('toplamBrutMasraflar')), `GV ${kisa(top('gvTutari'))} · DV ${kisa(top('dvTutari'))} · SGK İşçi ${kisa(top('sgkIsciToplam'))}`],
      ['#7c3aed', 'SGK İşveren', kisa(top('sgkIsverenToplam')), `Hazine İndirimi ${kisa(top('hazineIndirimi'))} · KVSK ${kisa(top('kvsk'))}`], ['#dc2626', 'Genel Maliyet', kisa(top('genelMaliyet')), `Ortalama ${kisa(n ? top('genelMaliyet') / n : 0)} / Kişi`]];
    $('#pmKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');

  }
  function dipToplam() {
    const top = k => toplam(L, k);
    $('#pmToplam').innerHTML = SUTUN.map(s => { const dk = `data-k="${s.anahtar}"`; if (s.tip === 'sira') return `<td ${dk}>Toplam</td>`; if (s.tip === 'ad') return `<td ${dk}>${L.length} Personel</td>`; if (s.tip === 'rx') return `<td class="s rx" ${dk}>${rxHucre(top(s.anahtar), top(s.anahtar + 'X'), kisa, true)}</td>`; if (s.tip === 'sayi' || s.tip === 'hesap') return `<td class="s" ${dk}>${kisa(top(s.anahtar))}</td>`; if (s.tip === 'genel') return `<td class="s genel" ${dk}>${kisa(top(s.anahtar))}</td>`; return `<td ${dk}></td>`; }).join('');
    const nufus = kapsam === 'ozel' ? V.personel.filter(p => p.ozel).length : V.personel.length + (V.uretim || []).filter(u => !u.sgkLi).length;   // ana tablo: bordrolu + özel + SGK'sız üretim (gizlenenler de sayılır)
    $('#pmTabloBaslik').textContent = `${V.donemAd || ''} ${kapsam === 'ozel' ? 'Özel Kişiler' : 'Personel Maliyet Tablosu'}`;
    $('#pmSayi').textContent = `${L.length} / ${nufus} ${kapsam === 'ozel' ? 'özel kişi' : 'personel'} · ${V.donemAd || ''} · aylık ₺${duzenleme ? ` · ${Object.keys(taslak).length} satırda değişiklik var, Kaydet ile saklanır` : ''}`;
  }

  // ---- sekme 1 çizim (sütun tanımına göre)
  const OZEL_SECENEK = [['', '—'], ['Emekli', 'Emekli'], ['Engelli', 'Engelli'], ['Gündelikçi', 'Gündelikçi']];
  /** rx hücresi: R tutarı üstte, X tutarı altta ("3.000 R" / "2.500 X"); etk: alt toplam (R, X ve TOPLAM üç satır) */
  /** alt toplam üç satırı: R, X ve TOPLAM (R + X); etiketler solda, tutarlar sağda (zam triad'ı ile aynı) */
  const rxToplam = (r, x, f) => { const g = f || hucre; return `<div class="rxt"><span class="r"><small>R</small>${g(r)}</span><span class="x"><small>X</small>${g(x)}</span><span class="t"><small>Toplam</small><b>${g((Number(r) || 0) + (Number(x) || 0))}</b></span></div>`; };
  const rxHucre = (r, x, f, etk) => etk ? rxToplam(r, x, f) : `<div class="rx"><span>${(f || hucre)(r)}<small>R</small></span><span class="x">${(f || hucre)(x)}<small>X</small></span></div>`;
  function hucreHtml(p, s, ilk, satirNo) {
    const k = s.anahtar, gb = ilk ? 'grupBas' : '', dk = `data-k="${k}"`, duz = duzenleme && !p.uSira;   // üretim satırı salt okunur (düzenleme yalnız bordro ve özel satırlarında)
    const al = etiket => `aria-label="${esc(etiket)} · ${esc(p.adSoyad || 'Yeni')}"`;   // ekran okuyucu: sütun adı + personel
    const metin = cls => duz ? `<input class="gir ${cls}" data-sira="${p.sira}" data-alan="${k}" value="${esc(p[k] ?? '')}" ${s.liste ? `list="${s.liste}"` : ''} maxlength="120" ${al(s.ad)}>` : esc(p[k]);
    switch (s.tip) {
      case 'sira': return `<td class="kucuk ${gb}" ${dk} title="Cari kodu ${esc(p.cariKodu)}">${satirNo}</td>`;   // sıra listedeki satır numarası (dinamik); kimlik cari kodu
      case 'ad': return `<td class="ad ${gb}" ${dk}>${metin('metin')}</td>`;
      case 'metin': return `<td class="${gb}" ${dk}>${metin(k === 'gorev' || k === 'departmanAciklamasi' ? 'genis' : 'dar')}</td>`;
      case 'tarih': return `<td class="kucuk ${gb}" ${dk}>${duz ? `<input class="gir tarih" type="date" data-sira="${p.sira}" data-alan="${k}" value="${esc(p[k] || '')}" ${al(s.ad)}>` : bos(p[k + 'Tr'])}</td>`;
      case 'secim': return `<td class="kucuk ${gb}" ${dk}>${duz ? `<select class="gir" data-sira="${p.sira}" data-alan="${k}" ${al(s.ad)}>${OZEL_SECENEK.map(o => `<option value="${esc(o[0])}" ${(p[k] || '') === o[0] ? 'selected' : ''}>${esc(o[1])}</option>`).join('')}</select>` : bos(p[k])}</td>`;
      case 'sayi': {
        const dv = k === 'xNet' && dovizli(p);   // dövizli maaş: X-Net kartta döviz tutarı × kur ile belirlenir
        return `<td class="s ${gb}" ${dk}>${duz ? `<input class="gir ${dv ? 'ro' : ''}" type="number" min="0" step="1" data-sira="${p.sira}" data-alan="${k}" value="${Number(p[k]) || 0}" ${dv ? 'readonly title="Dövizli maaş: kartta Döviz Tutarı × Kur ile belirlenir"' : ''} ${al(s.ad)}>` : hucre(p[k])}${dv ? `<small class="dv">${dovizNot(p)}</small>` : ''}</td>`;
      }
      case 'rx': return `<td class="s rx ${gb}" ${dk}>${duz ? `<div class="rx"><span><input class="gir" type="number" min="0" step="1" data-sira="${p.sira}" data-alan="${k}" value="${Number(p[k]) || 0}" ${al(s.ad + ' R')}><small>R</small></span><span class="x"><input class="gir x" type="number" min="0" step="1" data-sira="${p.sira}" data-alan="${k}X" value="${Number(p[k + 'X']) || 0}" ${al(s.ad + ' X')}><small>X</small></span></div>` : rxHucre(p[k], p[k + 'X'])}</td>`;
      case 'gun': return `<td class="s ${gb}" ${dk}>${duz ? `<input class="gir gun" type="number" min="0" max="31" step="0.5" data-sira="${p.sira}" data-alan="${k}" value="${Number(p[k]) || 0}" ${al(s.ad)}>` : gunFmt(p[k])}</td>`;
      case 'bilgi': return `<td class="${gb}" ${dk}>${bos(p[k])}</td>`;
      case 'hesap': return `<td class="s hesap ${gb}" ${dk}>${hucre(p[k])}</td>`;
      case 'genel': return `<td class="s genel ${gb}" ${dk}>${kisa(p[k])}</td>`;
      case 'not': return `<td class="${gb}" ${dk}>${duz ? `<input class="gir not" data-sira="${p.sira}" data-alan="not" value="${esc(p.not || '')}" maxlength="200" ${al('Not')}>` : `<small>${bos(p.not)}</small>`}</td>`;
      case 'kart': if (p.uSira) return `<td class="${gb}" ${dk}><span class="text-muted">—</span></td>`; return `<td class="${gb}" ${dk}><button type="button" class="btn btn-light btn-sm kart" data-sira="${p.sira}" title="Personel Cari Kartı ${esc(p.cariKodu)}"><i class="fas fa-id-card me-1"></i>Kart</button>${duz ? ` <button type="button" class="btn btn-light btn-sm sil" data-sira="${p.sira}" title="Satırı sil"><i class="fas fa-trash"></i></button>` : ''}</td>`;
      default: return `<td ${dk}>${bos(p[k])}</td>`;
    }
  }
  function satirHtml(p, satirNo) {
    const hucreler = V.sutunlar.map(g => g.sutunlar.map((s, i) => hucreHtml(p, s, i === 0, satirNo)).join('')).join('');
    return `<tr class="${p.ozelDurum === 'Emekli' ? 'emekli' : p.ozelDurum === 'Engelli' ? 'engelli' : ''} ${p.durum === 'İşten Ayrıldı' ? 'ayrildi' : ''} ${taslak[p.sira] ? 'degisti' : ''}" data-sira="${p.sira}"${p.uSira ? ` data-usira="${p.uSira}"` : ''}>${hucreler}</tr>`;
  }
  function ciz() {
    L = anaListe().map(etkin); kpiCiz();
    $('#pmTablo tbody').innerHTML = L.map((p, i) => satirHtml(p, i + 1)).join('') || `<tr><td colspan="${SUTUN.length}" class="na-bos">Filtreye uyan personel yok.</td></tr>`;
    dipToplam(); sifirGizle($('#pmTablo'), SUTUN, L, $('#pmSifirGizle').checked, V.sutunlar);
    $$('.pm-ana-gizle').forEach(e => { e.style.display = kapsam === 'ozel' ? 'none' : ''; });   // Özel Kişiler sekmesinde ana tablo anahtarları yok
    if (duzenleme) $$('#pmTablo .gir').forEach(i => i.addEventListener('input', () => { const s = +i.dataset.sira, a = i.dataset.alan; taslak[s] = taslak[s] || {}; taslak[s][a] = GIRIS.includes(a) ? (Number(i.value) || 0) : i.value; satirGuncelle(s, GIRIS.includes(a) || a === 'ozelDurum' || a === 'fiiliGorevYeri'); }));
    if (aktifSekme === 'ozet') cizOzet(); else if (aktifSekme === 'ozelrapor') ozelRaporCiz(); else if (EK[aktifSekme]) ekCiz(aktifSekme); else if (aktifSekme === 'ekcalisma') ekCalismaCiz(); else if (aktifSekme === 'kesinti') kesintiCiz(); else if (aktifSekme === 'muhasebe') muhasebeCiz(); else if (aktifSekme === 'cikis') cikisCiz(); else if (aktifSekme === 'odeme') odemeCiz(); else if (aktifSekme === 'zam') zamCiz(); else if (aktifSekme === 'karsilastirma') zamKarsCiz();
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
    const grup = tablo.querySelector('thead tr.grup th'); if (grup && grup.colSpan <= adet) uygula(grup, 0);   // grup başlığı sabit sütun sınırını aşıyorsa sabitlenmez (aksi halde tüm sütunların üstünde kalır)
    // birleşik (colspan) hücre sabit sütun sınırını aşıyorsa orada durulur; aksi halde yapışkan hücre sağındaki toplamları örter
    tablo.querySelectorAll('tbody tr, tfoot tr').forEach(tr => { const tds = Array.from(tr.children).filter(td => !td.classList.contains('gizli')); if (tds.length < adet || (tds.length === 1 && tds[0].colSpan > 1)) return; let i = 0; for (const td of tds) { if (i >= adet || i + (td.colSpan || 1) > adet) break; uygula(td, i); i += td.colSpan || 1; } });
  }
  /** tek tablolu sekmelerde (data-tam) kaydırma kabı ekranın kalanını doldurur: üst şeritler ince olduğundan en çok satır görünür */
  function kapBoyut() { $$('.na-panel.aktif .pm-kaydir[data-tam]').forEach(k => { const r = k.getBoundingClientRect(); k.style.maxHeight = Math.max(320, window.innerHeight - Math.max(r.top, 0) - 28) + 'px'; }); }
  /** bölme standardı (tüm rapor sekmeleri): listedeki sütunların (son başlık satırındaki ad) başında 2 px açık gri çizgi; başlık, gövde ve toplamda aynı konum. Ana tablo ile Kırılım kendi grup bölmelerini kullanır. */
  const BOLME = {
    pmZamTablo: ['Zam % (R / X)', 'Net Mevcut', 'Ek Ödemeler Toplam', 'Ara Toplam', 'Genel Maliyet'], pmZamKaynak: ['Mevcut Genel Maliyet'], pmZamOzetTablo: ['Toplu R %', 'Net Artışı'],
    pmOdemeTablo: ['R-Net', 'X-Net', 'Toplam Ödeme', 'Ödenen (Banka)', 'Ödeme Durumu'],
    pmFinansTablo: ['Bankadan Ödenecek (R)', 'Kasa Nakit Akışı · Banka (R)'],
    pmEkOzet: ['Saat Ücreti', 'Toplam Ek Çalışma', 'Talep (Adet)', 'Taleplerden Hesaplanan'], pmMesaiTablo: ['Planlanan (sa)', 'Karşılık', 'Gerçekleşme (PDKS)'],
    pmKesOzet: ['Avans', 'Kesintiler-R', 'Kalan Net (Banka)'], pmAvansTablo: ['Tutar', 'Talep Tarihi', 'Kasa Ödemesi', 'Nakit Akış (Finans)'], pmPlanTablo: ['Toplam', 'Başlangıç', 'Ekler'],
    pmUretimTablo: ['Günlük Ücret', 'Yemek', 'Toplam Hak Ediş', 'Not'],
    pmPrimTablo: ['Prim Türü', 'Hesaplanan Prim', 'Ödeme (R / X)'], pmPrimUyum: ['Dönem Primi', 'Fark'],
    pmBirlesimTablo: ['Net Toplam', 'Yol', 'Net Ödenen R', 'Toplam Brüt Masraflar'],
    pmCikisTablo: ['Net Toplam', 'Tahmini Kıdem', 'Senaryo'],
    pmMutabakat: ['R-Net', 'Genel Maliyet'], pmDepTablo: ['Mevcut ₺'], pmDepGorevTablo: ['Mevcut ₺'], pmTop10: ['Toplam Net'],
    pmDagUnvan: ['Genel Maliyet'], pmDagOzel: ['Genel Maliyet'], pmDagKidem: ['Genel Maliyet'], pmDagKaynak: ['Genel Maliyet'],
    pmMhSimTablo: ['Personel', 'Toplam Borç', 'Durum'], pmMhFisTablo: ['Personel', 'Borç', 'Hatalı Satır'], pmMhOzetTablo: ['Borç', 'Karşı Şirket'], pmMhEslemeTablo: ['Hesap Kodu', 'Geçerlilik']
  };
  /** bölmeleri çizer: sütun adı son başlık satırından okunur, aynı konum grup başlığı, gövde ve toplam satırlarında kullanılır; gizli (0 ₺) sütun atlanır, bölme bir sonraki görünür sütuna geçer */
  function bolmeCiz(tablo) {
    const liste = BOLME[tablo.id]; if (!liste || !tablo.tHead) return;
    $$('.grupBas', tablo).forEach(e => e.classList.remove('grupBas'));
    const basSatirlar = Array.from(tablo.tHead.rows), son = basSatirlar[basSatirlar.length - 1], kesim = new Set();
    let c = 0; for (const th of son.cells) { if (liste.includes(th.textContent.trim().replace(/\s+/g, ' '))) kesim.add(c); c += th.colSpan || 1; }
    const satirlar = [...basSatirlar, ...Array.from(tablo.tBodies).flatMap(b => Array.from(b.rows)), ...(tablo.tFoot ? Array.from(tablo.tFoot.rows) : [])];
    satirlar.forEach(tr => { let i = 0, bekle = false; for (const td of tr.cells) { if (kesim.has(i)) bekle = true; if (bekle && i > 0 && !td.classList.contains('gizli')) { td.classList.add('grupBas'); bekle = false; } i += td.colSpan || 1; } });
  }
  /** sütun genişliği (başlıklar en çok iki satır, alt satır üst satırdan uzun ya da eşit): başlığın iki satıra sığdığı içerik genişliği tablo-hizala.js ile aynı kırılımdan gelir; Sıra 40 px, diğerleri en az 104 px */
  function baslikGenislik(tablo) {
    if (!tablo.tHead || !tablo.tHead.rows.length) return;
    const son = tablo.tHead.rows[tablo.tHead.rows.length - 1];
    Array.from(son.cells).filter(th => th.colSpan <= 1).forEach(th => {
      const b = window.tabloBaslik.bilgi(th);
      th.style.minWidth = Math.max(th.cellIndex === 0 ? 40 : 104, Math.ceil(b.icerik + b.kutu + 3)) + 'px';
    });
  }
  const sabitleHepsi = () => requestAnimationFrame(() => { kapBoyut(); $$('.na-panel.aktif table.pm-ozet, .na-panel.aktif table.pm-excel').forEach(baslikGenislik); $$('.na-panel.aktif table.pm-ozet').forEach(bolmeCiz); $$('.na-panel.aktif table.pm-excel').forEach(t => { bolmeCiz(t); sabitle(t); }); });
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
    if (hesapMi) { const p = taslakla(bul(sira), taslak[sira]); SUTUN.filter(s => s.tip === 'hesap' || s.tip === 'genel' || s.tip === 'bilgi').forEach(s => { const td = tr.querySelector(`td[data-k="${s.anahtar}"]`); if (td) td.innerHTML = s.tip === 'genel' ? kisa(p[s.anahtar]) : s.tip === 'bilgi' ? bos(p[s.anahtar]) : hucre(p[s.anahtar]); }); }
    L = anaListe().map(etkin); kpiCiz(); dipToplam(); sabitleHepsi();
  }

  // ---- sekme 2: özet raporu
  function grupla(l, anahtar) { const m = {}; l.forEach(p => { const k = kirilimDeger(p, anahtar); const o = m[k] || (m[k] = { ad: k, n: 0 }); o.n++; KALEM.forEach(([f, , x]) => { o[f] = (o[f] || 0) + (Number(p[f]) || 0); if (x) o[x] = (o[x] || 0) + (Number(p[x]) || 0); }); }); return m; }
  /** özet raporu kapsamının parçaları (ana filtrelerle): bordro = Personel Maliyet Tablosu, ozel = Özel Kişiler, uretim = Üretim Personeli'nin SGK'sız gündelikçileri (tamamı elden X), sgkli = Üretim'deki SGK'lı gündelikçiler (bilgi: bordro satırında sayılır, ayrıca eklenmez) */
  function ozetParcalar() {
    const uretim = gizle.uretim ? [] : uretimKayit(false).map(u => ({ kaynak: 'Üretim', sira: -u.sira, adSoyad: u.adSoyad, unvan: 'Gündelikçi', gorev: u.gorev, sgkIsvereni: u.fiiliSirket, fiiliGorevYeri: u.fiiliGorevYeri, catiDepartman: 'Üretim', bolum: 'Üretim', ozelDurum: 'Gündelikçi', departmanKodu: 'D30', departmanAciklamasi: 'Üretim Departmanı', kidemYil: null,
      xNet: (Number(u.hakEdis) || 0) + (Number(u.tatilKazanci) || 0), rNet: 0, toplamNet: (Number(u.hakEdis) || 0) + (Number(u.tatilKazanci) || 0), toplamEkCalisma: 0, toplamEkCalismaX: Number(u.fazlaMesaiKazanci) || 0, toplamSosyal: 0, toplamSosyalX: Number(u.toplamSosyal) || 0, toplamPrim: 0, toplamPrimX: 0, toplamYanHaklar: 0, toplamYasal: 0, toplamBrutKazanc: 0, sgkIsciToplam: 0, gvTutari: 0, dvTutari: 0, sgkIsverenToplam: 0, toplamBrutMasraflar: 0, digerEkOdemelerX: 0, digerEkOdemelerR: 0, kesintilerToplami: 0, kesintilerToplamiX: Number(u.kesinti) || 0, genelMaliyet: Number(u.toplamMaliyet) || 0 }));
    const bordro = liste(false, false).map(etkin).map(p => ({ ...p, kaynak: 'Bordro' })), ozel = gizle.ozel ? [] : liste(false, true).map(etkin).map(p => ({ ...p, kaynak: 'Özel' }));
    const sgkli = (V.uretim || []).filter(u => u.sgkLi).map(u => ({ u, p: bordro.find(p => p.sira === u.personelSira) })).filter(x => x.p);   // filtreye giren bordro satırıyla eşleşenler
    return { bordro, ozel, uretim, sgkli };
  }
  /** özet raporu kapsamı: bordrolu personel + özel kişiler + SGK'sız gündelikçi üretim personeli — tüm maliyetler, kapsamdan bağımsız (SGK'lı gündelikçi mükerrer eklenmez) */
  function ozetListe() { const P = ozetParcalar(); return P.bordro.concat(P.ozel).concat(P.uretim).map(ozetAlanlari); }
  /** Kapsam Mutabakatı: kırılım toplamı = Personel Maliyet Tablosu + Özel Kişiler + Üretim (SGK'sız); SGK'lı gündelikçi bilgi satırı (bordroda sayıldı); her satır ilgili sekmeye gider */
  function mutabakatCiz(P, LO) {
    const top = (l, k) => toplam(l, k), satir = (sekmeAd, ad, l, aciklama, cls) => `<tr class="${cls || ''}" ${sekmeAd ? `data-sekme="${sekmeAd}"` : ''}><td><b>${esc(ad)}</b></td><td class="s">${l.length}${cls === 'bilgi' ? ' <small class="text-muted">(dahil)</small>' : ''}</td><td class="s">${l.length && !top(l, 'rNet') ? '—' : kisa(top(l, 'rNet'))}</td><td class="s">${kisa(top(l, 'xNet'))}</td><td class="s">${kisa(top(l, 'toplamNet'))}</td><td class="s">${kisa(top(l, 'genelMaliyet'))}</td><td><small>${aciklama}</small></td></tr>`;
    const sg = P.sgkli.map(x => x.p), hakedis = P.sgkli.reduce((s, x) => s + (Number(x.u.toplamHakedis) || 0), 0), mahsup = P.sgkli.reduce((s, x) => s + (Number(x.u.rMahsup) || 0), 0), elden = P.sgkli.reduce((s, x) => s + (Number(x.u.eldenX) || 0), 0);
    const parca = P.bordro.length + P.ozel.length + P.uretim.length, fark = ['xNet', 'rNet', 'toplamNet', 'genelMaliyet'].some(k => Math.abs(top(LO, k) - top(P.bordro, k) - top(P.ozel, k) - top(P.uretim, k)) > 1) || parca !== LO.length;
    const adlar = LO.map(p => p.adSoyad.toLocaleLowerCase('tr-TR').trim()), mukerrer = adlar.filter((a, i) => adlar.indexOf(a) !== i);
    $('#pmMutabakat tbody').innerHTML = satir('tablo', 'Personel Maliyet Tablosu', P.bordro, `Bordrolu personel · sekmenin dip toplamıyla aynı${sg.length ? ` · ${sg.length} SGK'lı gündelikçi bu satırlarda (R mahsup; elden fark X-Net'te)` : ''}`)
      + satir('ozel', 'Özel Kişiler', P.ozel, gizle.ozel ? 'Gizli (Özel Kişiler Gizle açık) · toplama girmez' : 'Özel Kişiler sekmesinin dip toplamıyla aynı · Personel Maliyet Tablosu\'nda da yer alır')
      + satir('uretim', 'Üretim Personeli (SGK\'sız)', P.uretim, gizle.uretim ? 'Gizli (Üretim Personeli Gizle açık) · toplama girmez' : 'Üretim Personeli sekmesinde "SGK\'sız (tamamı X)" satırları · R-Net yok, hak ediş elden X')
      + (sg.length ? satir('uretim', 'SGK\'lı Gündelikçi (Bilgi)', sg, `Üretim sekmesinde hak ediş ${kisa(hakedis)} − bordro Net-R mahsup ${kisa(mahsup)} = elden X ${kisa(elden)} → fark bordro satırının X-Net'ine, kira Kira Desteği-X'e yazıldı (yukarıda sayıldı, ayrıca eklenmez)`, 'bilgi') : '')
      + `<tr class="toplam"><td>Toplam (Kırılım)</td><td class="s">${LO.length}</td><td class="s">${kisa(top(LO, 'xNet'))}</td><td class="s">${kisa(top(LO, 'rNet'))}</td><td class="s">${kisa(top(LO, 'toplamNet'))}</td><td class="s">${kisa(top(LO, 'genelMaliyet'))}</td><td>${fark ? '<span class="pm-rz hata">Fark Var</span>' : '<span class="pm-rz ok">Mutabık</span>'} ${mukerrer.length ? `<span class="pm-rz hata">Mükerrer: ${esc([...new Set(mukerrer)].join(', '))}</span>` : '<span class="pm-rz gri">Mükerrer Yok</span>'} <small class="text-muted">${parca} = ${P.bordro.length} + ${P.ozel.length} + ${P.uretim.length}</small></td></tr>`;
  }
  function cizOzet() {
    const P = ozetParcalar(), LO = P.bordro.concat(P.ozel).concat(P.uretim).map(ozetAlanlari), k = $('#pmKirilim').value, genel = toplam(LO, 'genelMaliyet');
    mutabakatCiz(P, LO);
    $('#pmKirilimBaslik').textContent = KIRILIM_AD[k];
    $('#pmKirilimBas').innerHTML = `<th>${esc(KIRILIM_AD[k])}</th><th class="s">Personel</th>${KALEM.map(([f, ad, x], i) => `<th class="s ${i === 0 || KALEM_GRUPBAS.includes(f) ? 'grupBas' : ''}">${esc(ad)}</th>`).join('')}<th class="s">Ort. Maliyet</th><th class="s">Pay %</th>`;
    const m = grupla(LO, k);
    const keys = k === 'kidemAralik' ? KIDEM_SIRA.filter(x => m[x]) : Object.keys(m).sort((a, b) => m[b].genelMaliyet - m[a].genelMaliyet);
    const hucreK = (o, f, x, fmt) => x ? `<td class="s rx ${KALEM_GRUPBAS.includes(f) ? 'grupBas' : ''}">${rxHucre(o[f], o[x], fmt, !!fmt)}</td>` : `<td class="s ${f === 'genelMaliyet' ? 'genel' : ''} ${KALEM_GRUPBAS.includes(f) ? 'grupBas' : ''}">${fmt ? fmt(o[f]) : hucre(o[f])}</td>`;
    const satir = (o, cls) => `<tr class="${cls || ''}" ${cls ? '' : `data-deger="${esc(o.ad)}"`}><td class="ad">${esc(o.ad)}</td><td class="s">${o.n}</td>${KALEM.map(([f, , x]) => hucreK(o, f, x, cls ? kisa : null)).join('')}<td class="s">${kisa(o.n ? o.genelMaliyet / o.n : 0)}</td><td class="s">${pct(o.genelMaliyet, genel)}</td></tr>`;
    const T = { ad: 'Toplam', n: LO.length }; KALEM.forEach(([f, , x]) => { T[f] = toplam(LO, f); if (x) T[x] = toplam(LO, x); });
    $('#pmKirilimTablo tbody').innerHTML = keys.length ? keys.map(x => satir(m[x])).join('') + satir(T, 'toplam') : `<tr><td colspan="${KALEM.length + 4}" class="na-bos">Filtreye uyan personel yok.</td></tr>`;
    const sgkler = Object.keys(grupla(LO, 'sgkIsvereni')).sort((a, b) => LO.filter(p => p.sgkIsvereni === b).length - LO.filter(p => p.sgkIsvereni === a).length);
    const catilar = Object.keys(grupla(LO, 'catiDepartman')).sort((a, b) => LO.filter(p => p.catiDepartman === b).length - LO.filter(p => p.catiDepartman === a).length);
    $('#pmMatris thead').innerHTML = `<tr><th>SGK İşvereni</th>${catilar.map(c => `<th class="s">${esc(c)}</th>`).join('')}<th class="s">Toplam</th></tr>`;
    const h = l => l.length ? `<span class="n">${l.length}</span><small>${kisa(toplam(l, 'genelMaliyet'))}</small>` : '<span class="text-muted">—</span>';
    $('#pmMatris tbody').innerHTML = sgkler.map(s => { const ls = LO.filter(p => p.sgkIsvereni === s); return `<tr><td data-sgk="${esc(s)}"><b>${esc(s)}</b></td>${catilar.map(c => `<td class="s" data-sgk="${esc(s)}" data-cati="${esc(c)}">${h(ls.filter(p => p.catiDepartman === c))}</td>`).join('')}<td class="s" data-sgk="${esc(s)}">${h(ls)}</td></tr>`; }).join('') + `<tr class="toplam"><td>Toplam</td>${catilar.map(c => `<td class="s" data-cati="${esc(c)}">${h(LO.filter(p => p.catiDepartman === c))}</td>`).join('')}<td class="s">${h(LO)}</td></tr>`;
    const kal = [['R-Net Ücret (Bordro)', toplam(LO, 'rNet')], ['X-Net Ücret (Bordro Dışı)', toplam(LO, 'xNet')], ['Ek Ödemeler Toplam-R (Yol, Yemek, Mesai, Prim + diğer)', toplam(LO, 'digerEkR')], ['Ek Ödemeler Toplam-X (Yol, Yemek, Kira, Mesai, Prim)', toplam(LO, 'digerEkOdemelerX')], ['Vergi ve SGK İşçi Payları', toplam(LO, 'toplamBrutMasraflar') - toplam(LO, 'kesintilerToplami')], ['SGK İşveren Payı', toplam(LO, 'sgkIsverenToplam')]];
            const maks = Math.max(1, ...kal.map(x => x[1]));
    $('#pmKalemler').innerHTML = kal.map(([ad, v]) => `<div class="pm-bar"><span>${esc(ad)}</span><span class="b"><i style="width:${v ? Math.max(1.5, v / maks * 100) : 0}%"></i></span><span class="v">${v ? kisa(v) : '—'} <small>${v ? '%' + pct(v, genel) : ''}</small></span></div>`).join('');
    const dag = (id, anahtar, sira) => { const g = grupla(LO, anahtar); const ks = sira ? sira.filter(x => g[x]) : Object.keys(g).sort((a, b) => g[b].genelMaliyet - g[a].genelMaliyet); const r = o => `<tr data-anahtar="${anahtar}" data-deger="${esc(o.ad)}"><td>${esc(o.ad)}</td><td class="s">${o.n}</td><td class="s">${kisa(o.genelMaliyet)}</td><td class="s">${kisa(o.n ? o.genelMaliyet / o.n : 0)}</td></tr>`;
      $('#' + id + ' tbody').innerHTML = ks.length ? ks.map(x => r(g[x])).join('') + `<tr class="toplam"><td>Toplam</td><td class="s">${LO.length}</td><td class="s">${kisa(genel)}</td><td class="s">${kisa(LO.length ? genel / LO.length : 0)}</td></tr>` : '<tr><td colspan="4" class="na-bos">Kayıt yok.</td></tr>'; };
    dag('pmDagUnvan', 'unvan'); dag('pmDagOzel', 'ozelDurum', ['Emekli', 'Engelli', 'Gündelikçi', 'Özel Durumu Yok']); dag('pmDagKidem', 'kidemAralik', KIDEM_SIRA); dag('pmDagKaynak', 'kaynak', ['Bordro', 'Özel', 'Üretim']);
    $('#pmTop10 tbody').innerHTML = [...LO].sort((a, b) => b.genelMaliyet - a.genelMaliyet).slice(0, 10).map((p, i) => `<tr class="${p.sira > 0 ? 'kartAc' : ''}" data-sira="${p.sira}" style="${p.sira > 0 ? 'cursor:pointer' : ''}"><td>${i + 1}</td><td><b>${esc(p.adSoyad)}</b>${ozRozet(p)}${p.ozelDurum === 'Gündelikçi' ? ` <small class="text-muted">(Gündelikçi)</small>` : ''} <span class="pm-rz ${p.kaynak === 'Özel' ? 'mavi' : p.kaynak === 'Üretim' ? 'bekle' : 'gri'}">${esc(p.kaynak)}</span></td><td>${esc(p.unvan)}</td><td>${esc(p.gorev)}</td><td>${esc(p.sgkIsvereni)}</td><td>${esc(p.catiDepartman)}</td><td class="s">${kisa(p.toplamNet)}</td><td class="s">${kisa(p.toplamBrutKazanc)}</td><td class="s">${hucre(p.sgkIsverenToplam)}</td><td class="s"><b>${kisa(p.genelMaliyet)}</b></td><td class="s">${pct(p.genelMaliyet, genel)}</td></tr>`).join('') || '<tr><td colspan="11" class="na-bos">Kayıt yok.</td></tr>';
    $('#pmOzetKapsam').textContent = `${LO.filter(p => p.kaynak === 'Bordro').length} bordrolu · ${LO.filter(p => p.kaynak === 'Özel').length} özel kişi · ${LO.filter(p => p.kaynak === 'Üretim').length} SGK'sız gündelikçi · toplam ${LO.length} kişi · genel maliyet ${kisa(genel)}`;
    // zam senaryoları özeti (İşten Çıkış Senaryosu işaretleri bu sekmeye yansımaz; departman / görev bazında mevcut-çıkacak-kalan tabloları senaryonun kendi sekmesindedir)
    $('#pmZamOzetTablo tbody').innerHTML = zamSenaryo.map(s => { const o = zamOzet(s); return `<tr><td><b>${esc(s.ad)}</b></td><td class="s">${ondalik(s.r, 1)}</td><td class="s">${ondalik(s.x, 1)}</td><td class="s">${kisa(o.netA)}</td><td class="s">${kisa(o.gA)}</td><td class="s">${kurus(o.gA) ? zamYuzde(o.gA, o.gM) : '—'}</td><td class="s">${kisa(o.gA * 12)}</td></tr>`; }).join('') || '<tr><td colspan="7" class="na-bos">Kayıtlı zam senaryosu yok.</td></tr>';
    sabitleHepsi();
  }
  /** Özet Excel: ekrandaki her kart ayrı sayfa (kırılım, matris, kalem dağılımı, dağılımlar, en yüksek 10) — göründüğü gibi */
  function ozetExcel(bicim = 'xlsx') {
    const k = $('#pmKirilim').value, bas = `${V.yil} Personel Maliyet Özet Raporu`, alt = $('#pmOzetKapsam').textContent.trim();
    const kalemler = $$('#pmKalemler .pm-bar').map(d => [d.children[0].textContent.trim(), d.children[2].childNodes[0].textContent.trim(), (d.querySelector('small').textContent || '').trim()]);
    tabloExcel('personel_maliyet_ozet_raporu', [
      { tablo: '#pmKirilimTablo', ad: 'Kırılım', baslik: `${bas} · ${KIRILIM_AD[k]} Bazında Maliyet Kalemleri`, altBaslik: alt, not: '.na-panel[data-p="ozet"] .pm-not' },
      { tablo: '#pmMutabakat', ad: 'Kapsam Mutabakatı', baslik: `${bas} · Kapsam Mutabakatı`, altBaslik: alt },
      { tablo: '#pmZamOzetTablo', ad: 'Zam Senaryoları', baslik: `${bas} · Zam Senaryoları`, altBaslik: 'Kayıtlı senaryolar · aylık genel maliyet' },
      { tablo: '#pmMatris', ad: 'SGK × Çatı Departman', baslik: `${bas} · SGK İşvereni × Çatı Departman`, altBaslik: 'Personel sayısı · altında genel maliyet (₺)' },
      { ad: 'Kalem Dağılımı', baslik: `${bas} · Maliyet Kalemi Dağılımı`, altBaslik: 'Genel maliyet içindeki pay', basliklar: ['Kalem', 'Tutar', 'Pay'], satirlar: kalemler },
      { tablo: '#pmDagUnvan', ad: 'Unvan', baslik: `${bas} · Unvan Dağılımı` }, { tablo: '#pmDagOzel', ad: 'Özel Durum', baslik: `${bas} · Özel Durum Dağılımı` },
      { tablo: '#pmDagKidem', ad: 'Kıdem', baslik: `${bas} · Kıdem Dağılımı` }, { tablo: '#pmDagKaynak', ad: 'Kaynak', baslik: `${bas} · Kaynak Dağılımı` },
      { tablo: '#pmTop10', ad: 'En Yüksek 10', baslik: `${bas} · En Yüksek Maliyetli 10 Personel`, altBaslik: 'Filtreye göre · genel maliyet' }], bicim);
  }

  // ---- tablo düzenleme / toplu kayıt
  function modDegis(ac) { duzenleme = ac; if (!ac) taslak = {}; $('#pmDuzenle').style.display = ac ? 'none' : ''; $('#pmKaydet').style.display = ac ? '' : 'none'; $('#pmVazgec').style.display = ac ? '' : 'none'; ciz(); }
  async function kaydet() {
    const satirlar = Object.keys(taslak).map(s => ({ ...satirVeri({ ...bul(+s), ...taslak[s] }), degisen: Object.keys(taslak[s]) }));   // degisen: kalıcı günlük yalnız bu alanları geri yazar (6 Kas 2026)
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
  let kartOrijinal = null;   // karttaki kaynak alanlarının (yemek istisnası, kaynak brüt / işveren / hazine) açılıştaki değerleri: değişmeyen alan gönderilmez
  function kartAc(sira) {
    const p0 = sira ? etkin(bul(sira)) : { sira: 0, cariKodu: '335.' + String((V.personel.reduce((m, x) => Math.max(m, x.sira), 0) + 1)).padStart(3, '0') + ' (kayıtta kesinleşir)', durum: 'Aktif', ozelDurum: '', not: '', primGun: 30, normalGun: 22, haftaSonuGun: 8, ozel: kapsam === 'ozel' };
    if (sira && !p0) return; kartSira = sira; $('#pmKartHata').style.display = 'none';
    const p = { ...p0, ozelM: p0.ozel ? '1' : '0', bankaBlokeliM: p0.bankaBlokeli ? '1' : '0', odemeYapilmiyorM: p0.odemeYapilmiyor ? '1' : '0', sgkDetayYokM: p0.sgkDetayYok ? '1' : '0', dovizOdeme: p0.dovizOdeme || '' };   // Rapor Parametreleri seçimleri metin ('1' / '0'); kartOku mantıksala çevirir
    kartOrijinal = { sgkDisi: Number(p0.sgkDisi) || 0, brutKaynak: Number(p0.brutKaynak) || 0, isverenKaynak: Number(p0.isverenKaynak) || 0, hazineKaynak: Number(p0.hazineKaynak) || 0 };
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
        <div class="g2 pm-oto"><small>Ödeme Şirketi (otomatik) · R (banka): <b>${esc(p.odemeSirketiR || '—')}</b> · X (elden): <b>${esc(p.odemeSirketiX || '—')}</b></small></div><div></div>
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
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-sliders me-1"></i>Rapor Parametreleri</span><span class="not">Personel Cari Kartı sekmesi · ödeme kanalı, döviz düzeni, istisna ve kaynak değerler (resmi bordro)</span></div><div class="na-kart-g"><div class="pm-form">
        ${sec('ozelM', 'Kayıt Türü', [['0', 'Bordrolu Personel'], ['1', 'Özel Kişi']])}${sec('bankaBlokeliM', 'Banka Hesabı Blokeli (R Elden Ödenir)', [['0', 'Hayır'], ['1', 'Evet']])}${sec('odemeYapilmiyorM', 'Ödeme Yapılmıyor (Muhasebe Ödeme Kaydı Yok)', [['0', 'Hayır'], ['1', 'Evet']])}
        ${sec('dovizOdeme', 'Döviz Ödeme Düzeni', [['', 'R sabit, kalanı X (elden)'], ['TamamiR', 'Tamamı R (banka)'], ['TamamiX', 'Tamamı X (elden)']])}${inp('sgkDisi', 'Yemek İstisnası (SGK / Damga / GV Matrahından Düşülür)', '', { tip: 'number', ph: '0' })}${sec('sgkDetayYokM', 'SGK / Brüt Detayı', [['0', 'Var (bordro hesaplanır)'], ['1', 'Yok (yalnız net ödeme)']])}
        ${inp('brutKaynak', 'Kaynak Toplam Brüt (0 = model hesaplar)', '', { tip: 'number', ph: '0' })}${inp('isverenKaynak', 'Kaynak SGK İşveren (0 = model hesaplar)', '', { tip: 'number', ph: '0' })}${inp('hazineKaynak', 'Hazine İndirimi (Kaynak, Gösterim)', '', { tip: 'number', ph: '0' })}
      </div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-coins me-1"></i>Bordro Kalemleri (Aylık, ₺ · Günler)</span><span class="not">Hesaplanan sütunlar ve toplamlar otomatik</span></div><div class="na-kart-g"><div class="pm-form">${tutarGruplari}</div>
        <div class="pm-ozet-serit" id="pmKartOzet"></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-note-sticky me-1"></i><label for="pmk_not" class="m-0">Not</label></span></div><div class="na-kart-g"><textarea class="form-control" id="pmk_not" data-k="not" rows="2" maxlength="300">${esc(p.not || '')}</textarea></div></div>`;
    kartDegisti = false; kartDoviz(); kartOzet();
    $$('#pmKartIc [data-k]').forEach(e => e.addEventListener('input', () => {
      const k = e.dataset.k; $('#pmKartHata').style.display = 'none';
      if (k === 'doviz' || EK_SAYI.includes(k)) { kartDoviz(); kartOzet(); }   // döviz alanları: X-Net ₺ karşılığı yenilenir (bordroya girmez)
      else if (GIRIS.includes(k) || k === 'ozelDurum') { if (!XALAN.includes(k)) kartDegisti = true; kartOzet(); }
      else if (['sgkDisi', 'brutKaynak', 'isverenKaynak', 'hazineKaynak', 'sgkDetayYokM', 'dovizOdeme', 'ozelM', 'bankaBlokeliM', 'sgkIsvereni'].includes(k)) kartOzet();   // rapor parametreleri: özet yenilenir
      if (k === 'durum') $('#pmKartIc [data-k=ayrilisTarihi]').disabled = e.value !== 'İşten Ayrıldı';
    }));
    $('#pmKartIc [data-k=ayrilisTarihi]').disabled = (p.durum || 'Aktif') !== 'İşten Ayrıldı';
    $('#pmKartKaydet').addEventListener('click', kartKaydet); $('#pmKartVazgec').addEventListener('click', kartKapat); const s = $('#pmKartSil'); if (s) s.addEventListener('click', () => sil(kartSira));
    const cari = $('#pmKartCari'); if (cari) cari.addEventListener('click', ev => { if (window.top && window.top !== window && typeof window.top.portalSekmeAc === 'function') { ev.preventDefault(); window.top.portalSekmeAc('Personel Carileri', '/Rapor120'); } });
    $('#pmKart').classList.add('acik'); $('#pmPerde').classList.add('acik');
    try { const u = new URL(location.href); if (sira) u.searchParams.set('kod', bul(sira).cariKodu); else u.searchParams.delete('kod'); history.replaceState(null, '', u); } catch (e) { /* yoksay */ }
  }
  function kartOku() {
    const d = { sira: kartSira || 0, ozel: kartSira ? !!bul(kartSira).ozel : kapsam === 'ozel' };
    $$('#pmKartIc [data-k]').forEach(e => { const k = e.dataset.k; d[k] = GIRIS.includes(k) || EK_SAYI.includes(k) ? (Number(e.value) || 0) : e.value.trim(); });
    // Rapor Parametreleri: seçimler mantıksal; kaynak alanları yalnız değiştiyse gönderilir (gönderilmeyeni sunucu korur ya da tutar değişince düşürür)
    if (d.ozelM !== undefined) { d.ozel = d.ozelM === '1'; delete d.ozelM; }
    [['bankaBlokeliM', 'bankaBlokeli'], ['odemeYapilmiyorM', 'odemeYapilmiyor'], ['sgkDetayYokM', 'sgkDetayYok']].forEach(([m, k]) => { if (d[m] !== undefined) { d[k] = d[m] === '1'; delete d[m]; } });
    ['sgkDisi', 'brutKaynak', 'isverenKaynak', 'hazineKaynak'].forEach(k => { if (d[k] === undefined) return; const v = Number(d[k]) || 0; if (kartOrijinal && v === kartOrijinal[k]) delete d[k]; else d[k] = v; });
    return d;
  }
  /** kartta dövizli maaş: X-Net alanı ₺ karşılığıyla dolar ve kilitlenir; açıklama satırı yazılır */
  function kartDoviz() {
    const d = kartOku(), x = $('#pmk_xNet'), not = $('#pmKartDoviz'); if (!x) return;
    const dv = dovizli(d); x.readOnly = dv; x.classList.toggle('ro', dv);
    if (dv) { x.value = dovizX(d); not.innerHTML = `<i class="fas fa-coins me-1"></i>₺ karşılığı: <b>${kisa(d.dovizTutar * d.kur)}</b> (${dovizNot(d)}) → X-Net`; }
    else not.innerHTML = d.doviz ? '<span class="text-danger">Döviz Tutarı ve Kur sıfırdan büyük olmalıdır.</span>' : '<span class="text-muted">Maaş ₺ ödeniyorsa boş bırakın.</span>';
  }
  /** kart özeti: tutarlar değişmediyse kaynak tablo değerleriyle (tablodaki satırla aynı), değiştiyse modelle */
  /** kart özeti: kartta kaynak brüt değiştirildiyse o değer; değişmediyse ve tutarlar aynıysa kayıttaki kaynak değerler (tablodaki satırla aynı), tutar değiştiyse model */
  function kartOzet() { const k = kartSira ? bul(kartSira) : null, d = kartOku(), p = hesapla({ ...(k || {}), ...d, ...(k && !kartDegisti && d.brutKaynak === undefined ? { brutKaynak: k.brutKaynak, isverenKaynak: k.isverenKaynak } : {}) }); $('#pmKartOzet').innerHTML = [['Toplam Net', p.toplamNet], ['Toplam Brüt Kazanç', p.toplamBrutKazanc], ['Vergi ve İşçi Payları', p.toplamBrutMasraflar], ['SGK İşveren', p.sgkIsverenToplam], ['Genel Maliyet', p.genelMaliyet]].map(([l, v], i) => `<div class="${i === 4 ? 'genel' : ''}"><span>${l}</span><b>${kisa(v)}</b></div>`).join(''); }
  /** kalıcı günlük için karttaki değişen alanlar (6 Kas 2026): sayılar yaklaşık, seçimler mantıksal, metinler kırpılmış karşılaştırılır; yalnız değiştiğinde gönderilen kaynak alanları daima dahil */
  function degisenAlanlar(d, p) {
    if (!p) return undefined;
    return Object.keys(d).filter(k => {
      if (k === 'sira' || k === 'donem' || k === 'degisen') return false;
      if (['sgkDisi', 'brutKaynak', 'isverenKaynak', 'hazineKaynak'].includes(k)) return true;
      const a = d[k], b = p[k];
      if (typeof a === 'boolean') return a !== !!b;
      if (typeof a === 'number') return Math.abs(a - (Number(b) || 0)) > 0.0001;
      return String(a ?? '').trim() !== String(b ?? '').trim();
    });
  }
  async function kartKaydet() {
    const d = kartOku(), sira = kartSira, eskiDurum = sira ? (bul(sira).durum || 'Aktif') : null;
    d.donem = V.donem; if (sira) d.degisen = degisenAlanlar(d, bul(sira));
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

  // ---- excel (tablo): ekrandaki tablo göründüğü gibi (grup başlıkları, R üstte / X altta, gizli sütunlar hariç, dip toplam, KPI, açıklama)
  function excel(bicim = 'xlsx') {
    const ozel = kapsam === 'ozel', ad = ozel ? 'Özel Kişiler' : 'Personel Maliyet Tablosu';
    tabloExcel(ozel ? 'ozel_kisiler' : 'personel_maliyet_tablosu', [{ tablo: '#pmTablo', ad, baslik: `Konsolide İK Paneli · ${ad} · ${V.donemAd || ''}`, altBaslik: $('#pmSayi').textContent.trim(), kpi: '#pmKpi', not: '.na-panel[data-p="tablo"] .pm-not' }], bicim);
  }

  // ============================================================
  // EK SEKMELER: Üretim Personeli · Prim Ödemeleri · Özel Kişiler · Birleşik Tablo (sütun tanımı sunucudan: V.ekSutunlar; birleşim istemcide kurulur)
  // ============================================================
  const BIRLESIM_SUTUN = [
    { ad: 'Sıra', anahtar: 'sira', tip: 'sira' }, { ad: 'Kaynak', anahtar: 'kaynak', tip: 'kaynak' }, { ad: 'Adı-Soyadı', anahtar: 'adSoyad', tip: 'ad' }, { ad: 'Görev', anahtar: 'gorev', tip: 'metin' },
    // SGK İşvereni / Ödeyen ekranda ve Excel'de yok (gosterme); arama kutusu ("işveren") bu alanı yine tarar
    { ad: 'SGK İşvereni / Ödeyen', anahtar: 'isveren', tip: 'metin', gosterme: true }, { ad: 'Görev Yeri', anahtar: 'gorevYeri', tip: 'metin' },
    { ad: 'Net Toplam', anahtar: 'netToplam', tip: 'hesap' }, { ad: 'R-Net', anahtar: 'rNet', tip: 'sayi' }, { ad: 'X-Net', anahtar: 'xNet', tip: 'sayi' },
    // R üstte (bordrolu) / X altta (bordro dışı): ana tablodaki Yol, Yemek, Fazla Mesai, Prim ve Kesintiler Toplamı R / X değerleriyle birebir; toplanmaz (toplanırsa rakamlar yanıltır)
    { ad: 'Yol', anahtar: 'yol', tip: 'rx' }, { ad: 'Yemek', anahtar: 'yemek', tip: 'rx' }, { ad: 'Kira', anahtar: 'kira', tip: 'rx' }, { ad: 'Mesai', anahtar: 'mesai', tip: 'rx' }, { ad: 'Prim', anahtar: 'prim', tip: 'rx' }, { ad: 'Ek Ödemeler Toplam', anahtar: 'digerTop', tip: 'rx', otoGizle: true },   // otoGizle: tamamı sıfırsa sütun gelmez; dolu olduğu dönemde görünür
    { ad: 'Kesintiler', anahtar: 'kesinti', tip: 'rx' }, { ad: 'Net Ödenen R', anahtar: 'netOdenenR', tip: 'hesap' }, { ad: 'Net Ödenen X', anahtar: 'netOdenenX', tip: 'hesap' }, { ad: 'Net Ödenen Toplam', anahtar: 'netOdenen', tip: 'hesap' }, { ad: 'Toplam Brüt Masraflar', anahtar: 'toplamBrutMasraflar', tip: 'hesap' }, { ad: 'Toplam İşveren', anahtar: 'toplamIsveren', tip: 'hesap' }, { ad: 'Genel Maliyet', anahtar: 'genelMaliyet', tip: 'genel' }, { ad: 'Not', anahtar: 'not', tip: 'not' }
  ];
  const EK = {
    uretim: { tablo: '#pmUretimTablo', sayi: '#pmUretimSayi', sifir: '#pmUretimSifir', ara: '#pmUretimAra', birim: 'Personel', sutun: () => V.ekSutunlar.uretim, liste: () => { const y = $('#pmUretimYer').value, sgkli = $('#pmUretimSgkli').checked, sgksiz = $('#pmUretimSgksiz').checked; return V.uretim.filter(p => (!y || p.fiiliGorevYeri === y) && (p.sgkLi ? sgkli : sgksiz)); } },   // SGK'lı / SGK'sız anahtarları (6 Kas 2026): ikisi de açıkken tümü
    prim: { tablo: '#pmPrimTablo', sayi: '#pmPrimSayi', ara: '#pmPrimAra', birim: 'Prim Satırı', sutun: () => V.ekSutunlar.prim, liste: () => { const d = $('#pmPrimDonem').value; return V.primler.filter(p => (!d || d === 'yil' || p.donem === d) && !gizliKaynak(kayitKaynak(0, p.adSoyad))); } },
    birlesim: { tablo: '#pmBirlesimTablo', sayi: '#pmBirlesimSayi', sifir: '#pmBirlesimSifir', ara: '#pmBirlesimAra', birim: 'Kişi', sutun: () => BIRLESIM_SUTUN, liste: () => birlesimListe(undefined, $('#pmBirlesimTek').checked) },   // Çoklu Satırları Birleştir anahtarı yalnız burada
    cari: { tablo: '#pmCariTablo', sayi: '#pmCariSayi', ara: '#pmAra', birim: 'Kişi', sutun: () => CARI_SUTUN, liste: cariListe }   // Personel Cari Kartı: genel arama şeritte (GENEL_ARAMALI)
  };
  const ekSayisal = s => ['sayi', 'hesap', 'genel', 'gun', 'rx'].includes(s.tip);
  /** ek tablo hücresi (salt okunur): oran %, kur 4 hane, döviz tutarı 2 hane; tutarlar ₺; rx = R üstte / X altta (anahtar + 'X') */
  function ekHucre(p, s, satirNo) {
    const k = s.anahtar, v = p[k], dk = `data-k="${k}"`;
    switch (s.tip) {
      case 'sira': return `<td class="kucuk" ${dk}>${satirNo}</td>`;   // listedeki satır numarası (dinamik)
      case 'ad': return `<td class="ad" ${dk}>${adOz(v, p.sira0 || p.personelSira)}${p.satirSayisi > 1 ? ` <small class="text-muted" title="${esc(p.satirlar)}">${p.satirSayisi} satır</small>` : ''}</td>`;   // birleştirilmiş kişi: satır sayısı (işverenler ipucunda)
      case 'kaynak': return `<td class="kaynak k-${v === 'Üretim' ? 'uretim' : v === 'Özel' ? 'ozel' : 'bordro'}" ${dk}>${esc(v)}</td>`;
      case 'gun': return `<td class="s" ${dk}>${k === 'oran' ? (Number(v) ? '% ' + ondalik(v, 2) : '<span class="text-muted">—</span>') : k === 'kur' ? (Number(v) ? ondalik(v, 4) : '<span class="text-muted">—</span>') : k === 'dovizTutar' ? (Number(v) ? ondalik(v, 2) + ' ' + esc(p.doviz || '') : '<span class="text-muted">—</span>') : gunFmt(v)}</td>`;
      case 'sayi': return `<td class="s" ${dk}>${hucre(v)}</td>`;
      case 'rx': return `<td class="s rx" ${dk}>${rxHucre(v, p[k + 'X'])}</td>`;
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
    const S = S0.filter(s => !s.gosterme && (!s.otoGizle || L2.some(p => kurus(p[s.anahtar]) || (s.tip === 'rx' && kurus(p[s.anahtar + 'X'])))));   // otoGizle sütunları yalnız dolu olduğunda gelir; gosterme sütunu yalnız aramada kullanılır
    const t = $(E.tablo);
    $('thead tr', t).innerHTML = S.map(s => `<th class="${ekSayisal(s) ? 's' : ''}" data-k="${s.anahtar}">${esc(s.ad)}</th>`).join('');
    $('tbody', t).innerHTML = L2.map((p, i) => `<tr data-id="${p.sira ?? ''}" data-kaynak="${esc(p.kaynak || '')}" data-sira0="${p.sira0 ?? ''}" data-usira="${p.uSira ?? ''}">${S.map(s => ekHucre(p, s, i + 1)).join('')}</tr>`).join('') || `<tr><td colspan="${S.length}" class="na-bos">Kayıt yok.</td></tr>`;
    const top = k => toplam(L2, k), sayilmaz = ['oran', 'kur', 'dovizTutar', 'saatUcreti', 'gunlukSaat', 'gunlukUcret'];
    $('tfoot tr', t).innerHTML = S.map(s => { const dk = `data-k="${s.anahtar}"`; if (s.tip === 'sira') return `<td ${dk}>Toplam</td>`; if (s.tip === 'ad') return `<td ${dk}>${L2.length} ${E.birim}</td>`; if (s.tip === 'gun') return `<td class="s" ${dk}>${sayilmaz.includes(s.anahtar) ? '' : gunFmt(top(s.anahtar))}</td>`; if (s.tip === 'rx') return `<td class="s rx" ${dk}>${rxHucre(top(s.anahtar), top(s.anahtar + 'X'), kisa, true)}</td>`; if (s.tip === 'sayi' || s.tip === 'hesap') return `<td class="s" ${dk}>${sayilmaz.includes(s.anahtar) ? '' : kisa(top(s.anahtar))}</td>`; if (s.tip === 'genel') return `<td class="s genel" ${dk}>${kisa(top(s.anahtar))}</td>`; return `<td ${dk}></td>`; }).join('');
    const gizli = E.sifir ? sifirGizle(t, S, L2, $(E.sifir).checked) : 0;
    const tekBilgi = ad === 'birlesim' && $('#pmBirlesimTek').checked ? (() => { const b = L2.filter(p => p.satirSayisi > 1); return b.length ? ` · ${b.length} kişinin ${b.reduce((t, p) => t + p.satirSayisi, 0)} satırı birleştirildi` : ' · çoklu satırı olan kişi yok'; })() : '';
    const sgkBilgi = ad === 'uretim' ? ` · ${L2.filter(p => p.sgkLi).length} SGK'lı · ${L2.filter(p => !p.sgkLi).length} SGK'sız` : '';
    $(E.sayi).textContent = `${L2.length} ${E.birim.toLocaleLowerCase('tr-TR')} · aylık tutarlar ₺${gizli ? ` · ${gizli} sıfır sütun gizli` : ''}${tekBilgi}${sgkBilgi}`;
    if (ad === 'prim') primUyum(L2);
    E.son = { S, L: L2 }; sabitleHepsi();
  }
  /** ek sekme Excel'i: tablo göründüğü gibi; Prim'de uyum tablosu ikinci sayfa */
  function ekExcel(ad, bicim = 'xlsx') {
    const E = EK[ad]; if (!E.son) ekCiz(ad);
    const sayfa = { uretim: 'Üretim Personeli', prim: 'Prim Satırları', birlesim: 'Birleşik Tablo', cari: 'Personel Cari Kartı' }[ad], bas = panelBaslik(ad) || sayfa;
    const sayfalar = [{ tablo: E.tablo, ad: sayfa, baslik: `Konsolide İK Paneli · ${bas}`, altBaslik: $(E.sayi).textContent.trim(), not: `.na-panel[data-p="${ad}"] .pm-not` }];
    if (ad === 'prim') sayfalar.push({ tablo: '#pmPrimUyum', ad: 'Prim Uyumu', baslik: `Konsolide İK Paneli · ${bas} · Kişi Bazında Dönem Toplamı ↔ Personel Maliyet Tablosu` });
    if (ad === 'cari') sayfalar.push({ tablo: '#pmParamTablo', ad: 'Genel Parametreler', baslik: 'Konsolide İK Paneli · Genel Bordro Parametreleri' });
    tabloExcel('personel_maliyet_' + ad, sayfalar, bicim);
  }
  /** prim: dönem listesi (en yeni önce) + yıllık */
  function primDonemleriKur() {
    const sel = $('#pmPrimDonem'), v = sel.value, donemler = [...new Map(V.primler.map(p => [p.donem, p.donemAd])).entries()].sort((a, b) => b[0].localeCompare(a[0]));
    sel.innerHTML = donemler.map(([d, ad]) => `<option value="${esc(d)}">${esc(ad)}</option>`).join('') + `<option value="yil">${V.yil} Yıllık</option>`;
    sel.value = v && [...sel.options].some(o => o.value === v) ? v : (donemler[0] ? donemler[0][0] : 'yil');
  }
  /** kişi bazında dönem primi ↔ Personel Maliyet Tablosu'ndaki Prim — R üstte (bordrolu prim) / X altta (bordro dışı), fark da R / X ayrı; yıllıkta karşılaştırma yapılmaz */
  function primUyum(L2) {
    const secim = $('#pmPrimDonem').value, yil = secim === 'yil', ayni = !yil && secim === V.donem, bordroGoster = yil || ayni, m = {}, n = v => Number(v) || 0;   // karşılaştırma yalnız prim dönemi = yüklü bordro dönemi iken (5 Kas 2026: Eylül primleri Ekim bordrosuyla kıyaslanıp yanlış "Hata" veriyordu)
    L2.forEach(p => { const o = m[p.adSoyad] || (m[p.adSoyad] = { ad: p.adSoyad, r: 0, x: 0 }); if (p.odeme === 'R') o.r += n(p.hesaplananPrim); else o.x += n(p.hesaplananPrim); });
    const tablo = ad => { const k = V.personel.filter(p => p.adSoyad === ad && (p.durum || 'Aktif') === 'Aktif'); return { r: k.reduce((s, p) => s + n(p.prim), 0), x: k.reduce((s, p) => s + n(p.primX), 0) }; };
    const rows = Object.values(m).sort((a, b) => (b.r + b.x) - (a.r + a.x)), farkF = v => `<span class="rt-durum rt-${Math.abs(v) < 0.01 ? 'ok' : 'hata'}">${Math.abs(v) < 0.01 ? 'Uyumlu' : kisa(v)}</span>`;
    const farkli = `<span class="text-muted" title="Prim dönemi ile yüklü bordro dönemi (${esc(V.donemAd || V.donem || '')}) farklı; karşılaştırma için bordro dönemini seçin">—</span>`;
    $('#pmPrimUyum tbody').innerHTML = rows.map(o => { const t = tablo(o.ad); return `<tr data-ad="${esc(o.ad)}"><td class="ad">${adOz(o.ad)}</td><td class="s rx">${rxHucre(o.r, o.x)}</td><td class="s rx hesap">${bordroGoster ? rxHucre(t.r, t.x) : farkli}</td><td class="s rx">${ayni ? rxHucre(Math.round((o.r - t.r) * 100) / 100, Math.round((o.x - t.x) * 100) / 100, farkF) : farkli}</td></tr>`; }).join('')
      + (rows.length ? `<tr class="toplam"><td>Toplam · ${rows.length} kişi</td><td class="s rx">${rxHucre(rows.reduce((s, o) => s + o.r, 0), rows.reduce((s, o) => s + o.x, 0), kisa, true)}</td><td class="s rx">${bordroGoster ? rxHucre(rows.reduce((s, o) => s + tablo(o.ad).r, 0), rows.reduce((s, o) => s + tablo(o.ad).x, 0), kisa, true) : ''}</td><td></td></tr>` : '<tr><td colspan="4" class="na-bos">Kayıt yok.</td></tr>');
  }
  /** birleşim: Bordro + Özel (aynı bordro modeli; ana sekme filtreleriyle) + Üretim; prim kişinin satırında. Toplam Brüt Masraflar burada vergi + SGK işçi (kesintiler ayrı sütun) */
  /** birleşik satır: Yol / Yemek / Kira / Mesai / Prim / Kesintiler R (anahtar) ve X (anahtar + 'X') ayrı — ana tablodaki R / X değerleriyle birebir, toplanmaz. Kira: R = bordrodaki Kira Desteği, X = bordro dışı kira desteği (kiraDestegiX; dövizli destek bordro kuruyla ₺) */
  const bordroSatir = (p, kaynak) => { const n = k => Number(p[k]) || 0; return { kaynak, adSoyad: p.adSoyad, gorev: p.gorev, catiDepartman: p.catiDepartman, isveren: p.sgkIsvereni, gorevYeri: p.fiiliGorevYeri, kidemYil: p.kidemYil, brut: n('toplamBrutKazanc'), netToplam: n('rNet') + n('xNet'), rNet: p.rNet, xNet: p.xNet,
    yol: n('yol'), yolX: n('yolX'), yemek: n('yemek'), yemekX: n('yemekX'), kira: n('kiraDestegi'), kiraX: n('kiraDestegiX'), mesai: n('toplamEkCalisma'), mesaiX: n('toplamEkCalismaX'), prim: n('toplamPrim'), primX: n('toplamPrimX'), digerTop: n('digerEkR'), digerTopX: n('digerEkOdemelerX'),
    diger: n('toplamSosyal') - n('yemek') - n('yol') + n('toplamYanHaklar') + n('toplamYasal') + n('digerEkOdemelerR'),
    kesinti: n('kesintilerToplami'), kesintiX: n('kesintilerToplamiX'), netOdenen: n('bankaOdeme') + n('eldenOdeme'), netOdenenR: n('bankaOdeme'), netOdenenX: n('eldenOdeme'), araToplam: n('araToplam'), brutTam: n('toplamBrutMasraflar'),
    toplamBrutMasraflar: n('toplamBrutMasraflar') - n('kesintilerToplami'), toplamIsveren: p.toplamIsveren, genelMaliyet: p.genelMaliyet, not: p.not, sira0: p.sira }; };
  function birlesimListe(kaynakSecim, tekSatir) {
    const kaynak = kaynakSecim !== undefined ? kaynakSecim : $('#pmBirlesimKaynak').value, rows = [];
    if (!kaynak || kaynak === 'Bordro') liste(false, false).map(etkin).forEach(p => rows.push(bordroSatir(p, 'Bordro')));
    if ((!kaynak || kaynak === 'Özel') && !gizle.ozel) liste(false, true).map(etkin).forEach(p => rows.push(bordroSatir(p, 'Özel')));
    // SGK'lı gündelikçiler zaten bordro satırında (R + elden X); birleşime yalnız SGK'sız gündelikçiler ayrı satır olarak girer
    // ana tablonun filtreleri ve gizleme anahtarları üretim satırlarına da aynen uygulanır
    if ((!kaynak || kaynak === 'Üretim') && !gizle.uretim) uretimKayit(false).forEach(p => rows.push({ kaynak: 'Üretim', uSira: p.sira, adSoyad: p.adSoyad, gorev: p.gorev, catiDepartman: 'Üretim', isveren: p.fiiliSirket, gorevYeri: p.fiiliGorevYeri, netToplam: (Number(p.hakEdis) || 0) + (Number(p.tatilKazanci) || 0), rNet: 0, xNet: (Number(p.hakEdis) || 0) + (Number(p.tatilKazanci) || 0),
      yol: 0, yolX: Number(p.yolX) || 0, yemek: 0, yemekX: Number(p.yemekX) || 0, kira: 0, kiraX: Number(p.kiraX) || 0, mesai: 0, mesaiX: p.fazlaMesaiKazanci, prim: 0, primX: 0, diger: p.digerSosyal, digerTop: 0, digerTopX: (Number(p.yolX) || 0) + (Number(p.yemekX) || 0) + (Number(p.kiraX) || 0) + (Number(p.fazlaMesaiKazanci) || 0) + (Number(p.digerSosyal) || 0), kesinti: 0, kesintiX: p.kesinti, netOdenen: p.netOdenen, netOdenenR: 0, netOdenenX: p.netOdenen, toplamBrutMasraflar: 0, brutTam: 0, araToplam: uretimAnaSatir(p).araToplam, toplamIsveren: 0, genelMaliyet: p.toplamMaliyet, not: p.not }));   // SGK'sız gündelikçi: tamamı X tarafı
    const sonuc = tekSatir ? birlesimTeklestir(rows) : rows;   // "Çoklu Satırları Birleştir" yalnız Birleşik Tablo'dan açık gelir; İşten Çıkış ve Ödeme Planı satır bazında kalır
    sonuc.forEach((r, i) => r.sira = i + 1);
    return sonuc;
  }
  /** Çoklu Satırları Birleştir (6 Kas 2026): aynı kaynaktaki aynı kişinin satırları (ör. Cüneyt Arda Ceylan, 4 SGK işvereni) tek satırda toplanır — tutarlar toplanır, işveren / görev yeri / görev / not ayrık değerlerle birleşir, kart ilk kaydı açar (sira0 ilk satırın) */
  const BIRLESIM_TOPLANAN = ['brut', 'netToplam', 'rNet', 'xNet', 'yol', 'yolX', 'yemek', 'yemekX', 'kira', 'kiraX', 'mesai', 'mesaiX', 'prim', 'primX', 'digerTop', 'digerTopX', 'diger', 'kesinti', 'kesintiX', 'netOdenen', 'netOdenenR', 'netOdenenX', 'araToplam', 'brutTam', 'toplamBrutMasraflar', 'toplamIsveren', 'genelMaliyet'];
  function birlesimTeklestir(rows) {
    const anahtar = r => r.kaynak + '|' + (r.adSoyad || '').toLocaleLowerCase('tr-TR').replace(/\s+/g, ' ').trim(), grup = new Map();
    rows.forEach(r => { const k = anahtar(r); if (grup.has(k)) grup.get(k).push(r); else grup.set(k, [r]); });
    return [...grup.values()].map(g => {
      if (g.length === 1) return g[0];
      const b = { ...g[0] }, ayrik = f => [...new Set(g.map(f).filter(Boolean))];
      BIRLESIM_TOPLANAN.forEach(k => { b[k] = Math.round(g.reduce((s, r) => s + (Number(r[k]) || 0), 0) * 100) / 100; });
      b.isveren = ayrik(r => r.isveren).join(' / '); b.gorevYeri = ayrik(r => r.gorevYeri).join(' / '); b.gorev = ayrik(r => r.gorev).join(' / '); b.not = ayrik(r => r.not).join(' · ');
      b.kidemYil = g.map(r => r.kidemYil).find(v => v !== null && v !== undefined);
      b.satirSayisi = g.length; b.satirlar = g.map(r => r.isveren || r.gorevYeri || '').join(', ');
      return b;
    });
  }
  function ekKur() {
    $('#pmUretimYer').innerHTML = '<option value="">Tüm Görev Yerleri</option>' + [...new Set(V.uretim.map(p => p.fiiliGorevYeri))].map(y => `<option value="${esc(y)}">${esc(y)}</option>`).join('');
    primDonemleriKur();



  }

  // ============================================================
  // PERSONEL CARİ KARTI — raporu etkileyen tüm kişi parametreleri tek listede (satır → kart "Rapor Parametreleri"); genel bordro parametreleri V.parametre
  // ============================================================
  const CARI_SUTUN = [
    { ad: 'Sıra', anahtar: 'sira', tip: 'sira' }, { ad: 'Cari Kodu', anahtar: 'cariKodu', tip: 'metin' }, { ad: 'Adı-Soyadı', anahtar: 'adSoyad', tip: 'ad' }, { ad: 'Kaynak', anahtar: 'kaynak', tip: 'kaynak' },
    { ad: 'SGK İşvereni', anahtar: 'sgkIsvereni', tip: 'metin' }, { ad: 'Fiili Görev Yeri', anahtar: 'fiiliGorevYeri', tip: 'metin' }, { ad: 'Ödeme Şirketi-R', anahtar: 'odemeSirketiR', tip: 'metin' }, { ad: 'Ödeme Şirketi-X', anahtar: 'odemeSirketiX', tip: 'metin' },
    { ad: 'Durum', anahtar: 'durum', tip: 'metin' }, { ad: 'Özel Durumu', anahtar: 'ozelDurum', tip: 'metin' }, { ad: 'Bloke Hesap', anahtar: 'blokeM', tip: 'metin' }, { ad: 'Ödeme Yapılmıyor', anahtar: 'odemeYokM', tip: 'metin' },
    { ad: 'Döviz (PB · Tutar × Kur)', anahtar: 'dovizM', tip: 'metin' }, { ad: 'Döviz Düzeni', anahtar: 'dovizOdemeM', tip: 'metin' }, { ad: 'R-Net', anahtar: 'rNet', tip: 'sayi' }, { ad: 'X-Net', anahtar: 'xNet', tip: 'sayi' },
    { ad: 'Prim Gün', anahtar: 'primGun', tip: 'gun' }, { ad: 'Normal Gün', anahtar: 'normalGun', tip: 'gun' }, { ad: 'Hafta Sonu Gün', anahtar: 'haftaSonuGun', tip: 'gun' }, { ad: 'Yemek İstisnası', anahtar: 'sgkDisi', tip: 'sayi' }, { ad: 'SGK / Brüt Detayı', anahtar: 'sgkDetayM', tip: 'metin' },
    { ad: 'Kaynak Brüt', anahtar: 'brutKaynak', tip: 'sayi' }, { ad: 'Kaynak İşveren', anahtar: 'isverenKaynak', tip: 'sayi' }, { ad: 'Hazine İnd. (Kaynak)', anahtar: 'hazineKaynak', tip: 'sayi' }, { ad: 'Teşvik İndirimi', anahtar: 'tesvikIndirimi', tip: 'hesap' },
    { ad: 'Sicil', anahtar: 'sicilNo', tip: 'metin' }, { ad: 'SGK No', anahtar: 'sgkNo', tip: 'metin' }, { ad: 'İşe Giriş', anahtar: 'iseGirisTr', tip: 'metin' }, { ad: 'Kıdem (Yıl)', anahtar: 'kidemM', tip: 'metin' },
    { ad: 'TC Kimlik', anahtar: 'tcKimlik', tip: 'metin' }, { ad: 'Telefon', anahtar: 'telefon', tip: 'metin' }, { ad: 'E-posta', anahtar: 'eposta', tip: 'metin' }, { ad: 'Not', anahtar: 'not', tip: 'not' }
  ];
  /** cari kartı satırları: bordro + özel (gizli değilse), ana filtreler ve genel aramayla; seçim alanları metin olarak */
  function cariListe() {
    const duzen = { TamamiR: 'Tamamı R (banka)', TamamiX: 'Tamamı X (elden)' };
    return liste(false, false).concat(gizle.ozel ? [] : liste(false, true)).map(etkin).map(p => ({ ...p, kaynak: p.ozel ? 'Özel' : 'Bordro', sira0: p.sira, blokeM: p.bankaBlokeli ? 'Evet' : '—', odemeYokM: p.odemeYapilmiyor ? 'Evet' : '—',
      dovizM: p.doviz ? `${p.doviz} ${ondalik(p.dovizTutar, 2)} × ${ondalik(p.kur, 4)}` : '—', dovizOdemeM: p.doviz ? (duzen[p.dovizOdeme] || 'R sabit, kalanı X') : '—', sgkDetayM: p.sgkDetayYok ? 'Yok (yalnız net)' : 'Var', kidemM: p.kidemYil === null || p.kidemYil === undefined ? '—' : ondalik(p.kidemYil, 1) }));
  }
  function cariCiz() {
    const P = V.parametre || {}, yuzde = v => `% ${ondalik((Number(v) || 0) * 100, 2)}`;
    const satirlar = [
      ['Brüt Asgari Ücret (Aylık)', kisa(P.asgariBrut), 'Gelir vergisi ve damga istisnası ile SGK tavanının dayanağı'],
      ['SGK Prime Esas Kazanç Tavanı', kisa(P.sgkTavan), `Asgari ücretin ${ondalik((P.sgkTavan || 0) / (P.asgariBrut || 1), 0)} katı`],
      ['İşçi: İşsizlik / MYÖ / GSS', `${yuzde(P.isciIssizlik)} / ${yuzde(P.isciMyo)} / ${yuzde(P.isciGss)}`, 'SGK işçi payı (normal sigortalı)'],
      ['İşçi: SGDP (Emekli)', yuzde(P.isciSgdp), 'Sosyal güvenlik destek primi işçi payı'],
      ['İşveren: İşsizlik / MYÖ / GSS', `${yuzde(P.isvIssizlik)} / ${yuzde(P.isvMyo)} / ${yuzde(P.isvGss)}`, 'SGK işveren payı (brüt; teşvikler ayrı düşülür)'],
      ['İşveren: SGDP (Emekli) / KVSK', `${yuzde(P.isvSgdp)} / ${yuzde(P.kvsk)}`, 'Kısa vadeli sigorta kolu primi tüm sigortalılarda'],
      ['Damga Vergisi', `‰ ${ondalik((Number(P.damga) || 0) * 1000, 2)}`, 'Damga matrahı = brüt kazanç − yan haklar − yemek istisnası'],
      ['Gelir Vergisi Dilimleri', (P.dilimler || []).map(d => `${d[0] >= 1e14 ? 'üzeri' : kisa(d[0])}: ${yuzde(d[1])}`).join(' · '), 'Kümülatif matrah = aylık matrah × ay'],
      ['Hesap Ayı (Kümülatif)', String(P.ay || ''), 'Bugünün takvim ayı; kümülatif matrah ve asgari ücret istisnası bu aya göre'],
      ['Asgari Ücret GV Matrahı / Damga İstisnası', `${kisa(P.asgariGvMatrahi)} / ${kisa(P.asgariDamga)}`, 'Asgari ücret istisnaları; şirket bazında Şirket Teşvikleri sekmesinde açılıp kapanır']
    ];
    $('#pmParamTablo tbody').innerHTML = satirlar.map(r => `<tr><td>${esc(r[0])}</td><td class="s"><b>${r[1]}</b></td><td><small>${esc(r[2])}</small></td></tr>`).join('');
    $('#pmCariBaslik').textContent = `${V.donemAd || ''} Personel Cari Kartı`;
    ekCiz('cari');
  }

  // ============================================================
  // ŞİRKET TEŞVİK VE İNDİRİMLERİ — V.tesvik (Data/PersonelTesvikOrnek.cs): katalog × şirket seçimi; seçim anında kaydedilir, tüm hesaplar yenilenir
  // ============================================================
  function tesvikCiz() {
    const T = V.tesvik; if (!T) return; const S = T.sirketler || [], nn = v => Number(v) || 0;
    $('#pmTesvikBaslik').textContent = `${V.donemAd || ''} Şirket Teşvik ve İndirimleri`;
    $('#pmTesvikTablo thead tr').innerHTML = `<th>Kod</th><th>Teşvik / İndirim</th><th>Tür</th><th class="s">Oran / Tutar</th><th>Hesap</th>${S.map(s => `<th class="s">${esc(s)}</th>`).join('')}<th>Dayanak</th><th>Açıklama</th>`;
    const oranM = t => t.puan ? `${ondalik(t.puan, 2)} puan` : t.aylikTutar ? `${kisa(t.aylikTutar)} / ay` : '—';
    const hesapM = t => t.hesap === 'Evet' ? '<span class="pm-rz ok">Hesaba girer</span>' : t.hesap === 'İstisna' ? '<span class="pm-rz bekle">Vergi istisnası</span>' : '<span class="pm-rz gri">Bilgi</span>';
    $('#pmTesvikTablo tbody').innerHTML = T.tesvikler.map(t => `<tr data-kod="${esc(t.kod)}"><td class="kucuk">${esc(t.kod)}</td><td><b>${esc(t.ad)}</b>${t.kapsam === 'Engelli' ? ' <small class="alt">yalnız Engelli</small>' : ''}${t.emekliye ? '' : ' <small class="alt">emekliye uygulanmaz</small>'}</td><td>${esc(t.tur)}</td><td class="s">${oranM(t)}</td><td>${hesapM(t)}</td>${S.map(s => { const sec = (T.secim[s] || []).includes(t.kod); return `<td class="s"><label class="pm-tesvik-sec ${sec ? 'evet' : 'hayir'}" title="${esc(s)} · ${sec ? 'faydalanıyor' : 'muaf / faydalanamıyor'}"><input type="checkbox" class="pmTesvikKutu" data-sirket="${esc(s)}" data-kod="${esc(t.kod)}" ${sec ? 'checked' : ''}> ${sec ? 'Faydalanıyor' : 'Muaf'}</label></td>`; }).join('')}<td><small>${esc(t.dayanak)}</small></td><td><small>${esc(t.aciklama)}</small></td></tr>`).join('');
    // şirket bazında etki (seçili dönem; aktif bordrolu + özel, ana filtrelerle)
    const K = kisiTumu(), g = {};
    K.forEach(p => { const s = p.sgkIsvereni || '—'; const o = g[s] || (g[s] = { s, n: 0, matrah: 0, isv: 0, tesvik: 0, net: 0, genel: 0, genelT: 0 }); o.n++; o.matrah += nn(p.sgkMatrahi); o.isv += nn(p.sgkIsverenToplam); o.tesvik += nn(p.tesvikIndirimi); o.net += nn(p.isverenNet); o.genel += nn(p.genelMaliyet); o.genelT += nn(p.genelMaliyetTesvik); });
    const rows = Object.values(g).sort((a, b) => b.genel - a.genel), evetKod = new Set(T.tesvikler.filter(t => t.hesap === 'Evet').map(t => t.kod));
    $('#pmTesvikOzet tbody').innerHTML = rows.map(o => `<tr><td>${esc(o.s)}</td><td class="s">${o.n}</td><td><small>${(T.secim[o.s] || []).filter(k => evetKod.has(k)).join(', ') || '—'}</small></td><td class="s">${kisa(o.matrah)}</td><td class="s">${kisa(o.isv)}</td><td class="s hesap">${kisa(o.tesvik)}</td><td class="s hesap">${kisa(o.net)}</td><td class="s">${kisa(o.genel)}</td><td class="s genel">${kisa(o.genelT)}</td></tr>`).join('') || '<tr><td colspan="9" class="na-bos">Kayıt yok.</td></tr>';
    const top = k => rows.reduce((s, o) => s + o[k], 0);
    $('#pmTesvikOzet tfoot tr').innerHTML = `<td>Toplam</td><td class="s">${top('n')}</td><td></td><td class="s">${kisa(top('matrah'))}</td><td class="s">${kisa(top('isv'))}</td><td class="s">${kisa(top('tesvik'))}</td><td class="s">${kisa(top('net'))}</td><td class="s">${kisa(top('genel'))}</td><td class="s">${kisa(top('genelT'))}</td>`;
    $('#pmTesvikSayi').textContent = `${T.tesvikler.length} teşvik / istisna · ${S.length} şirket · ${K.length} kişi · ${V.donemAd || ''} · tutarlar ₺ · seçim bellekte tutulur`;
    sabitleHepsi();
  }
  /** katalog hücresindeki seçim: sunucuya kaydedilir, tüm veri ve sekme yenilenir; hata olursa kutu geri alınır */
  async function tesvikSec(ev) {
    const k = ev.target; if (!k.classList.contains('pmTesvikKutu')) return;
    const j = await post('/PersonelMaliyet/TesvikKaydet', { sirket: k.dataset.sirket, kod: k.dataset.kod, secili: k.checked, donem: V.donem });
    if (!j.success) { mesaj('danger', esc(j.message)); k.checked = !k.checked; return; }
    veriGuncelle(j); tesvikCiz(); mesaj('success', esc(j.message));
  }

  // ============================================================
  // EK ÇALIŞMALAR (mesai talepleri) ve DİĞER KESİNTİLER (şahsi avanslar, kesinti / icra planları) — V.ekCalisma / V.kesinti (Data/PersonelKesintiOrnek.cs), dönem bazında
  // ============================================================
  const rozet = (d, sinif) => `<span class="pm-rz ${sinif || (/(Onaylandı|Tamamlandı|Gerçekleşti|Aktarıldı|Kapandı|Teslim Edildi|ÖDENDİ|Uyumlu)/.test(d) ? 'ok' : /(Bekliyor|Bekleniyor|Planlandı|Planlı|Devam|Açık|Kısmi|Başlayacak)/.test(d) ? 'bekle' : /(Reddedildi|İptal|Geçti|Gerçekleşmedi)/.test(d) ? 'hata' : 'gri')}">${esc(d || '—')}</span>`;
  const kisiTumu = () => liste(false, false).concat(gizle.ozel ? [] : liste(false, true)).map(etkin);   // bordro + özel (gizli değilse), ana filtrelerle
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
    // Toplam Ek Çalışma: R üstte (bordrolu ek çalışma kalemleri) / X altta (bordro dışı mesai); fark R + X toplamıyla taleplerden hesaplanan arasında
    $('#pmEkOzet tbody').innerHTML = K.map((x, i) => { const p = x.p, f = Math.round((x.top - x.hes) * 100) / 100; return `<tr data-sira="${p.sira}"><td class="kucuk">${i + 1}</td><td class="ad">${esc(p.adSoyad)}${ozRozet(p)}</td><td>${esc(p.bolum)}</td><td>${esc(p.sgkIsvereni)}</td><td class="s">${hucre(x.su)}</td><td class="s">${hucre(p.fazlaMesai)}</td><td class="s">${hucre(p.fazlaMesaiX)}</td><td class="s">${hucre(p.bayramCalisma)}</td><td class="s">${hucre(p.geceFarki)}</td><td class="s">${hucre(p.pazarCalisma)}</td><td class="s">${hucre(p.resmiTatilCalisma)}</td><td class="s rx hesap">${rxHucre(p.toplamEkCalisma, p.toplamEkCalismaX)}</td><td class="s">${x.t.length || '—'}</td><td class="s">${saatFmt(x.saat)}</td><td class="s hesap">${hucre(x.hes)}</td><td class="s">${Math.abs(f) < 25 ? rozet('Uyumlu') + (Math.abs(f) >= 1 ? `<small class="alt">yuvarlama ${kisa(f)}</small>` : '') : `<span class="${f > 0 ? 'text-danger' : 'text-primary'}">${kisa(f)}</span><small class="alt">${f > 0 ? 'puantaj bekleyen / talepsiz' : 'tabloya girmemiş'}</small>`}</td></tr>`; }).join('') || '<tr><td colspan="16" class="na-bos">Ek çalışması olan personel yok.</td></tr>';
    const topK = k => K.reduce((s, x) => s + (Number(x[k]) || 0), 0), topP = k => K.reduce((s, x) => s + (Number(x.p[k]) || 0), 0);
    $('#pmEkOzet tfoot tr').innerHTML = `<td>Toplam</td><td>${K.length} Personel</td><td></td><td></td><td></td><td class="s">${kisa(topP('fazlaMesai'))}</td><td class="s">${kisa(topP('fazlaMesaiX'))}</td><td class="s">${kisa(topP('bayramCalisma'))}</td><td class="s">${kisa(topP('geceFarki'))}</td><td class="s">${kisa(topP('pazarCalisma'))}</td><td class="s">${kisa(topP('resmiTatilCalisma'))}</td><td class="s rx">${rxHucre(topP('toplamEkCalisma'), topP('toplamEkCalismaX'), kisa, true)}</td><td class="s">${K.reduce((s, x) => s + x.t.length, 0)}</td><td class="s">${saatFmt(topK('saat'))}</td><td class="s">${kisa(topK('hes'))}</td><td class="s">${kisa(topK('top') - topK('hes'))}</td>`;
    // talep satırları
    $('#pmMesaiTablo tbody').innerHTML = M.map(m => `<tr data-id="${m.id}"><td><b>${esc(m.no)}</b></td><td class="ad">${adOz(m.adSoyad, m.personelSira)}</td><td>${esc(m.bolum || '')}</td><td>${esc(m.tur)}</td><td>${esc(m.tarih)}</td><td>${esc(m.aralik)}</td><td class="s">${saatFmt(m.planlanan)}</td><td class="s">${m.fiili === null || m.fiili === undefined ? '<span class="text-muted">—</span>' : saatFmt(m.fiili)}</td><td class="s">×${ondalik(m.carpan, 2)}</td><td class="s">${hucre(m.saatUcreti)}</td><td class="s hesap">${hucre(m.tutar)}</td><td>${esc(m.karsilik)}</td><td>${rozet(m.talepDurumu)}</td><td>${m.yonetici ? `${esc(m.yonetici)}<small class="alt">${esc(m.yoneticiDurum)}${m.yoneticiTarih ? ' · ' + esc(m.yoneticiTarih) : ''}</small>` : '—'}</td><td><small>${bos(m.digerOnaylar)}</small></td><td>${rozet(m.gerceklesme)}${m.fiiliGiris ? `<small class="alt">${esc(m.fiiliGiris)}–${esc(m.fiiliCikis)}</small>` : ''}</td><td>${rozet(m.puantaj)}${m.puantajTarihi ? `<small class="alt">${esc(m.puantajTarihi)}</small>` : ''}</td><td>${esc(m.donem)}</td><td><small>${bos(m.proje)}</small></td><td class="kucuk">${esc(m.talepTarihi)}<small class="alt">${esc(m.olusturan || '')}</small></td></tr>`).join('') || '<tr><td colspan="20" class="na-bos">Filtreye uyan mesai talebi yok.</td></tr>';
    const topM = k => M.reduce((s, m) => s + (Number(m[k]) || 0), 0);
    $('#pmMesaiTablo tfoot tr').innerHTML = `<td>Toplam</td><td>${M.length} Talep</td><td></td><td></td><td></td><td></td><td class="s">${saatFmt(topM('planlanan'))}</td><td class="s">${saatFmt(topM('fiili'))}</td><td></td><td></td><td class="s">${kisa(topM('tutar'))}</td>${'<td></td>'.repeat(9)}`;
    const onayli = E.mesailer.filter(m => m.onaylandi && !m.iptal), bekleyen = E.mesailer.filter(m => /Bekliyor/.test(m.talepDurumu) && !/Revizyon/.test(m.talepDurumu));
    const kpi = [['#6366f1', 'Mesai Talebi', E.mesailer.length, `${onayli.length} onaylı · ${bekleyen.length} onay bekliyor · ${E.mesailer.filter(m => m.puantaj === 'Puantaj Bekliyor').length} puantaj bekliyor`], ['#0ea5e9', 'Fiili Saat', ondalik(E.mesailer.reduce((s, m) => s + (Number(m.fiili) || 0), 0), 1) + ' sa', `planlanan ${ondalik(E.mesailer.reduce((s, m) => s + (Number(m.planlanan) || 0), 0), 1)} sa`],
      ['#0f766e', 'Taleplerden Hesaplanan', kisa(E.mesailer.reduce((s, m) => s + (Number(m.tutar) || 0), 0)), `puantaja aktarılan ${kisa(E.mesailer.reduce((s, m) => s + (Number(m.puantajaAktarilan) || 0), 0))}`], ['#b45309', 'Tablodaki Ek Çalışma', kisa(topK('top')), `R ${kisa(topP('toplamEkCalisma'))} · X ${kisa(topP('toplamEkCalismaX'))} · ${K.length} personel · fark ${kisa(topK('top') - topK('hes'))}`],
      ['#dc2626', 'Reddedilen / İptal', E.mesailer.filter(m => /Reddedildi/.test(m.talepDurumu) || m.iptal).length, 'talep']];
    $('#pmEkKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $('#pmEkSayi').textContent = `${M.length} / ${E.mesailer.length} mesai talebi · ${V.donemAd} · tutarlar ₺ · satıra tıklayınca onay adımları ve zaman çizelgesi`;
 sabitleHepsi();
  }
  function kesintiCiz() {
    const E0 = V.kesinti || { avanslar: [], planlar: [], turler: [] }, ara = ($('#pmKesAra').value || '').toLocaleLowerCase('tr-TR'), tur = $('#pmKesTur').value;
    $('#pmKesBaslik').textContent = `${V.donemAd} Diğer Kesintiler`;
    const kisiler = kisiTumu(), kisiSet = new Set(kisiler.map(p => p.sira));
    const E = { ...E0, avanslar: (E0.avanslar || []).filter(a => (!a.personelSira || kisiSet.has(a.personelSira)) && !gizliKaynak(kayitKaynak(a.personelSira, a.adSoyad))), planlar: (E0.planlar || []).filter(k => kisiSet.has(k.personelSira)) };   // üstteki ortak filtrelerle senkron
    const ts = $('#pmKesTur'); if (ts.options.length <= 1) ts.innerHTML = '<option value="">Tüm Türler</option>' + E.turler.map(t => `<option value="${esc(t)}">${esc(t)}</option>`).join('');
    const A = E.avanslar.filter(a => (!tur || tur === 'Avans Kesintisi') && (!ara || [a.adSoyad, a.no, a.konu, a.sirket].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    const P = E.planlar.filter(k => (!tur || k.tur === tur) && (!ara || [k.adSoyad, k.kaynak, k.dosyaNo, k.alacakli, k.icraDairesi].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    // kişi özeti
    const K = kisiler.map(p => { const pl = E.planlar.filter(k => k.personelSira === p.sira && k.aktif), ac = E.avanslar.filter(a => a.personelSira === p.sira).reduce((s, a) => s + (Number(a.acikTutar) || 0), 0); const bit = E.planlar.filter(k => k.personelSira === p.sira && k.durum !== 'Tamamlandı').map(k => k.bitisDonem).sort().pop();
      return { p, pl, ac, bit, bitAd: bit ? (E.planlar.find(k => k.bitisDonem === bit && k.personelSira === p.sira) || {}).bitisAd : '' }; }).filter(x => (Number(x.p.kesintilerToplami) || 0) + (Number(x.p.kesintilerToplamiX) || 0) > 0 || x.pl.length || x.ac > 0);
    $('#pmKesOzet tbody').innerHTML = K.map((x, i) => { const p = x.p; return `<tr data-sira="${p.sira}"><td class="kucuk">${i + 1}</td><td class="ad">${esc(p.adSoyad)}${ozRozet(p)}</td><td>${esc(p.bolum)}</td><td>${esc(p.sgkIsvereni)}</td><td class="s">${hucre(p.avans)}</td><td class="s">${hucre(p.icra)}</td><td class="s">${hucre(p.borc)}</td><td class="s">${hucre(p.ozelSigortaKesintisi)}</td><td class="s">${hucre(p.sendikaKatkisi)}</td><td class="s">${hucre(p.bes)}</td><td class="s">${hucre(p.mazotKesintisi)}</td><td class="s">${hucre(p.ihbarKesintisi)}</td><td class="s hesap">${hucre(p.kesintilerToplami)}</td><td class="s hesap">${hucre(p.kesintilerToplamiX)}</td><td class="s">${hucre(p.kalanNetBanka)}</td><td class="s">${hucre(x.ac)}</td><td class="s">${x.pl.length || '—'}</td><td>${x.bit ? `${esc(x.bitAd)} <small class="alt">${esc(x.bit)}</small>` : '<span class="text-muted">—</span>'}</td></tr>`; }).join('') || '<tr><td colspan="18" class="na-bos">Kesintisi olan personel yok.</td></tr>';
    const topP = k => K.reduce((s, x) => s + (Number(x.p[k]) || 0), 0);
    $('#pmKesOzet tfoot tr').innerHTML = `<td>Toplam</td><td>${K.length} Personel</td><td></td><td></td><td class="s">${kisa(topP('avans'))}</td><td class="s">${kisa(topP('icra'))}</td><td class="s">${kisa(topP('borc'))}</td><td class="s">${kisa(topP('ozelSigortaKesintisi'))}</td><td class="s">${kisa(topP('sendikaKatkisi'))}</td><td class="s">${kisa(topP('bes'))}</td><td class="s">${kisa(topP('mazotKesintisi'))}</td><td class="s">${kisa(topP('ihbarKesintisi'))}</td><td class="s">${kisa(topP('kesintilerToplami'))}</td><td class="s">${kisa(topP('kesintilerToplamiX'))}</td><td class="s">${kisa(topP('kalanNetBanka'))}</td><td class="s">${kisa(K.reduce((s, x) => s + x.ac, 0))}</td><td class="s">${K.reduce((s, x) => s + x.pl.length, 0)}</td><td></td>`;
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
      ['#7c3aed', 'Diğer (BES · Sendika · Özel Sigorta · Mazot · İhbar)', kisa(topP('bes') + topP('sendikaKatkisi') + topP('ozelSigortaKesintisi') + topP('mazotKesintisi') + topP('ihbarKesintisi')), 'bordro kesintileri'], ['#0f766e', 'Kesintiler Toplamı', kisa(topP('kesintilerToplami') + topP('kesintilerToplamiX')), `R ${kisa(topP('kesintilerToplami'))} · X ${kisa(topP('kesintilerToplamiX'))}`], ['#0f172a', 'Plan Kalan Bakiye', kisa(tumP.reduce((s, k) => s + (Number(k.kalan) || 0), 0)), `${tumP.filter(k => k.durum === 'Devam Ediyor').length} devam eden plan`]];
    $('#pmKesKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $('#pmKesSayi').textContent = `${K.length} personel · ${A.length} avans talebi · ${P.length} kesinti planı · ${V.donemAd} · tutarlar ₺`;

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
    const kpi = s ? [['#0f172a', 'Personel Sayısı', s.personelSayisi, `${s.no} v${s.versiyon} · ${esc(s.kapsamAd)}`], ['#dc2626', 'Genel Maliyet', kisa(s.genelMaliyet), 'personel maliyeti · kontrol toplamı'], ['#0f766e', 'Ara Toplam', kisa(s.araToplam), 'fiili net ödeme (Net-X + Net-R)'],
      ['#7c3aed', 'Yansıtma Gereken', kisa(s.yansitmaGereken), 'SGK işvereni ≠ fiili görev yeri'], ['#1d4ed8', 'Başka Şirket Tarafından Ödenen', kisa(s.baskaSirketOdeme), 'SGK işv. ≠ ödeme şirketi (R/X ayrı)'], ['#b45309', 'Toplam Borç / Alacak', kisa(s.toplamBorc), `${s.fisSayisi} fiş · ${s.satirSayisi} satır`],
      [(s.dengesizFis || s.hataliSatir || s.mukerrerRisk) ? '#dc2626' : '#16a34a', 'Kontroller', `${s.dengesizFis} / ${s.hataliSatir} / ${s.mukerrerRisk}`, 'dengesiz fiş / hatalı hesap / mükerrer']]
      : [['#0f172a', 'Personel Sayısı', K.personel || 0, 'kaynak: Personel Maliyet Tablosu (aktif)'], ['#dc2626', 'Genel Maliyet', kisa(K.genelMaliyet), 'toplam personel maliyeti'], ['#0f766e', 'Ara Toplam', kisa(K.araToplam), 'fiili net ödeme'], ['#7c3aed', 'Yansıtma Gereken', kisa(K.yansitma), 'SGK işvereni ≠ fiili görev yeri'], ['#1d4ed8', 'Başka Şirket Tarafından Ödenen', kisa(K.baskaSirket), 'SGK işv. ≠ ödeme şirketi (R/X ayrı)'], ['#b45309', 'Ödeme Yapılmıyor Notu', K.odemeYok || 0, 'ödeme kaydı üretilmeyecek personel']];
    $('#pmMhKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${k[3]}</div></div>`).join('');
    // simülasyon listesi
    $('#pmMhSimTablo tbody').innerHTML = M.simulasyonlar.map(x => `<tr data-id="${x.id}" class="${s && x.id === s.id ? 'secili' : ''}"><td><b>${esc(x.no)}</b></td><td>v${x.versiyon}</td><td>${esc(x.donemAd)}</td><td>${esc(x.kapsamAd)}</td><td class="s sayac">${x.personelSayisi}</td><td class="s sayac">${x.fisSayisi}</td><td class="s sayac">${x.satirSayisi}</td><td class="s">${kisa(x.toplamBorc)}</td><td class="s">${kisa(x.toplamAlacak)}</td><td>${rozet(x.durum, x.durum === 'Kontrol Gerekiyor' || x.durum === 'Güncelliğini Yitirdi' ? 'hata' : x.durum === 'İptal' ? 'gri' : undefined)}</td><td>${esc(x.olusturan)}<small class="alt">${esc(x.olusturma)}</small></td><td>${x.onayaGonderen ? esc(x.onayaGonderen) + `<small class="alt">${esc(x.onayaGonderme)}</small>` : '—'}</td><td>${x.onaylayan ? esc(x.onaylayan) + `<small class="alt">${esc(x.onayTarihi)}</small>` : '—'}</td><td>${x.muhasebelestiren ? esc(x.muhasebelestiren) + `<small class="alt">${esc(x.muhasebeTarihi)}</small>` : '—'}</td><td>${x.guncel ? rozet('Güncel') : rozet('Veri Değişti', 'hata')}</td></tr>`).join('') || `<tr><td colspan="15" class="na-bos">Bu dönem için simülasyon yok (kapsamda aktif personel bulunamadı).</td></tr>`;
    mhSeciliCiz(s); mhEslemeCiz(M);
    $('#pmMhSayi').textContent = `${M.simulasyonlar.length} simülasyon · ${V.donemAd} · ${M.eslemeler.length} hesap eşleştirmesi (v${M.eslemeVersiyonu}) · kullanıcı ${esc(M.kullanici)} (${M.yetkili ? 'onay yetkili' : 'onay yetkisi yok'})`;

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
      return `<div class="${a.sinif || ''}">${lb}<input class="form-control" id="${id}" data-k="${a.k}" type="${a.tip === 'number' ? 'number' : a.tip === 'month' ? 'month' : a.tip === 'date' ? 'date' : 'text'}" ${a.tip === 'number' ? `min="0" step="${a.adim || '0.01'}"` : ''} ${a.liste ? `list="${a.liste}"` : ''} value="${esc(v ?? (a.tip === 'number' ? 0 : ''))}" ${z} ${a.ph ? `placeholder="${esc(a.ph)}"` : ''} ${a.maxlength ? `maxlength="${a.maxlength}"` : ''}></div>`;
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
    const r = sira ? (V.primler || []).find(x => x.sira === sira) : { sira: 0, donem: V.donem, adSoyad: '', primTuru: 'Ciro Primi', baz: 0, oran: 0, sabitTutar: 0, odeme: 'X', aciklama: '' };
    if (!r) return;
    const kisiler = V.personel.filter(p => (p.durum || 'Aktif') === 'Aktif').map(p => p.adSoyad).sort((a, b) => a.localeCompare(b, 'tr'));
    duzenleKart(`<i class="fas fa-sack-dollar me-1"></i>${sira ? 'Prim Satırı · ' + esc(r.adSoyad) : 'Yeni Prim Satırı'}`, [
      { k: 'donem', ad: 'Dönem (Prim Ayı)', tip: 'month', zorunlu: true }, { k: 'adSoyad', ad: 'Personel', tip: 'select', secenekler: (sira ? [] : [['', 'Seçin…']]).concat(kisiler), zorunlu: true }, { k: 'primTuru', ad: 'Prim Türü', tip: 'select', secenekler: (V.ekSutunlar && V.ekSutunlar.primTurleri) || ['Ciro Primi', 'Tahsilat Primi', 'Hedef Primi', 'Özel Ödeme'] },
      { k: 'baz', ad: 'Baz (Ciro / Tahsilat ₺)', tip: 'number' }, { k: 'oran', ad: 'Oran %', tip: 'number', adim: '0.01' }, { k: 'sabitTutar', ad: 'Sabit Tutar ₺ (Oran Yerine)', tip: 'number' },
      { k: 'odeme', ad: 'Ödeme', tip: 'select', secenekler: [['X', 'X (Bordro Dışı)'], ['R', 'R (Bordrolu)']] }, { k: 'aciklama', ad: 'Açıklama', tip: 'textarea' }
    ], r, d => post('/PersonelMaliyet/PrimKaydet', { ...d, sira: r.sira, bordroDonem: V.donem }), sira ? () => post('/PersonelMaliyet/PrimSil', { id: sira, donem: V.donem }) : null,
      d => { const h = d.sabitTutar > 0 ? d.sabitTutar : d.baz * d.oran / 100; return `Hesaplanan prim: <b>${kisa(h)}</b> · ${d.donem ? esc(sonrakiAy(d.donem)) : '—'} bordrosundaki Prim-${esc(d.odeme)} tutarı prim satırlarının toplamından yeniden yazılır${d.odeme === 'R' ? ' (R primi brüte girer; kaynak tablo değerleri düşer, model yeniden hesaplar)' : ''}.`; });
  }
  /** gündelikçi üretim personeli: günlük ücret × gün; SGK'lı ise bordro Net-R mahsup edilir, fark elden X. TEK KAYNAK (9 Kas 2026): SGK'lı gündelikçide yemek / yol / kira bordro kaydının R + X toplamıdır —
   *  formdan girilen toplam bordro kaydına yazılır (R resmi bordro, sabit; fazlası X; R'nin altı hata), fazla mesai bordro kaydından (Ek Çalışmalar / kişi kartı) gelir ve burada değiştirilmez */
  const n0 = v => Number(v) || 0;
  const bordroMesai = p => n0(p.fazlaMesai) + n0(p.fazlaMesaiX);
  function uretimDuzenle(sira) {
    const u = sira ? (V.uretim || []).find(x => x.sira === sira) : { sira: 0, fiiliGorevYeri: '', adSoyad: '', gorev: 'Üretim Personeli', vardiya: 'Gündüz', personelSira: 0, gunlukUcret: 2200, calisilanGun: 26, gunlukSaat: 7.5, fazlaMesaiSaat: 0, tatilGun: 0, yemek: 0, yol: 0, kira: 0, digerSosyal: 0, kesinti: 0, rMahsup: 0, not: '' };
    if (!u) return;
    const bordrolu = [['0', 'SGK\'sız gündelikçi (tamamı elden X)']].concat(V.personel.filter(p => (p.durum || 'Aktif') === 'Aktif' && !p.ozel).sort((a, b) => a.adSoyad.localeCompare(b.adSoyad, 'tr')).map(p => [String(p.sira), `${p.adSoyad} · ${p.cariKodu} · Net-R ${kisa(p.netRBordro)}`]));
    const p0 = u.personelSira > 0 ? bul(u.personelSira) : null;   // SGK'lı: etiketlerde bordro R kısmı (sabit)
    const rEtiket = (ad, r) => p0 ? `${ad} ₺ (toplam · bordro R ${kisa(r)} sabit, fazlası X)` : `${ad} ₺`;
    const top = k => n0(u[k]) + n0(u[k + 'X']);   // tabloda R üstte / X altta; formda toplam girilir (SGK'lıda sunucu R'yi sabit tutar, farkı X yazar; SGK'sızda tamamı X)
    const veri = { ...u, yemek: top('yemek'), yol: top('yol'), kira: top('kira'), ...(p0 ? { fazlaMesaiBordro: `${kisa(bordroMesai(p0))} (R ${kisa(p0.fazlaMesai)} + X ${kisa(p0.fazlaMesaiX)})` } : {}) };
    duzenleKart(`<i class="fas fa-industry me-1"></i>${sira ? 'Gündelikçi · ' + esc(u.adSoyad) : 'Yeni Gündelikçi Üretim Personeli'}`, [
      { k: 'adSoyad', ad: 'Adı-Soyadı', tip: 'text', zorunlu: true }, { k: 'fiiliGorevYeri', ad: 'Fiili Görev Yeri', tip: 'text', zorunlu: true, liste: 'dlYer' }, { k: 'gorev', ad: 'Görev', tip: 'text' },
      { k: 'vardiya', ad: 'Vardiya', tip: 'select', secenekler: (V.ekSutunlar && V.ekSutunlar.vardiyalar) || ['Gündüz', 'Akşam', 'Gece'] }, { k: 'personelSira', ad: 'Bordrolu Personel (SGK\'lı · R Mahsup)', tip: 'select', secenekler: bordrolu, sinif: 'g2' },
      { k: 'gunlukUcret', ad: 'Günlük Ücret ₺', tip: 'number' }, { k: 'calisilanGun', ad: 'Çalışılan Gün', tip: 'number', adim: '0.5' }, { k: 'gunlukSaat', ad: 'Günlük Saat', tip: 'number', adim: '0.5' },
      p0 ? { k: 'fazlaMesaiBordro', ad: 'Fazla Mesai ₺ (bordro kaydından; Ek Çalışmalar / kişi kartı)', tip: 'salt' } : { k: 'fazlaMesaiSaat', ad: 'Fazla Mesai (Saat, ×1,5)', tip: 'number', adim: '0.5' }, { k: 'tatilGun', ad: 'Tatil (Gün, ×2)', tip: 'number', adim: '0.5' }, { k: 'kesinti', ad: 'Avans / Kesinti ₺', tip: 'number' },
      { k: 'yemek', ad: rEtiket('Yemek', p0 && p0.yemek), tip: 'number' }, { k: 'yol', ad: rEtiket('Yol', p0 && p0.yol), tip: 'number' }, { k: 'kira', ad: p0 ? rEtiket('Kira', p0.kiraDestegi) : 'Kira ₺ (X)', tip: 'number' }, { k: 'digerSosyal', ad: 'Diğer Sosyal Yardım ₺', tip: 'number' }, { k: 'not', ad: 'Not', tip: 'textarea' }
    ], veri, d => post('/PersonelMaliyet/UretimKaydet', { ...d, sira: u.sira, personelSira: +d.personelSira || 0, donem: V.donem }), sira ? () => post('/PersonelMaliyet/UretimSil', { id: sira, donem: V.donem }) : null,
      d => { const p = +d.personelSira ? bul(+d.personelSira) : null; const su = d.gunlukSaat > 0 ? d.gunlukUcret / d.gunlukSaat : 0, h = d.gunlukUcret * d.calisilanGun, fm = p ? bordroMesai(p) : d.fazlaMesaiSaat * su * 1.5, t = d.tatilGun * d.gunlukUcret * 2;
        const kira = n0(d.kira), top = h + fm + t + d.yemek + d.yol + kira + d.digerSosyal, r = p ? n0(p.netRBordro) : 0, x = p ? Math.max(0, top - kira - r) + kira : top;
        const dusuk = p ? [['Yemek', d.yemek, n0(p.yemek)], ['Yol', d.yol, n0(p.yol)], ['Kira', kira, n0(p.kiraDestegi)]].filter(([, v, rr]) => v < rr - 0.005) : [];
        return `Hak ediş ${kisa(h)} · mesai ${kisa(fm)}${p ? ' (bordro R + X)' : ''} · tatil ${kisa(t)} · yemek ${kisa(d.yemek)} · yol ${kisa(d.yol)} · kira ${kisa(kira)} · <b>toplam hak ediş ${kisa(top)}</b>${p ? ` · bordro Net-R mahsup ${kisa(r)} → <b>elden X ${kisa(x)}</b> (fark ${kisa(x - kira)} kişinin X-Net'ine, kira Kira Desteği-X'e yazılır). Yemek, yol ve kira ${esc(p.adSoyad)} bordro kaydına yazılır: R kısmı (resmi bordro) sabit kalır, girilen toplamın R'yi aşan kısmı X olur; Personel Maliyet Tablosu, kişi kartı ve Üretim aynı değeri gösterir` : ` · SGK'sız: tamamı elden X ${kisa(top)}`} · net ödenen ${kisa(x - d.kesinti)}.${dusuk.length ? `<br><span class="text-danger">${dusuk.map(([a, v, rr]) => `${a} ${kisa(v)} bordro R tutarının (${kisa(rr)}) altına inemez`).join(' · ')}.</span>` : ''}`; });
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
    const tum = birlesimListe('');   // bordrolu, özel ve SGK'sız üretim: Birleşik Tablo ile birebir (gizleme anahtarları geçerli)
    const L2 = tum.filter(r => (!kaynak || r.kaynak === kaynak) && (!yalniz || cikisSecim.has(cikisAnahtar(r))) && (!ara || [r.adSoyad, r.gorev, r.isveren, r.gorevYeri, lokasyonAd(r.gorevYeri)].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    // satır bağı: Ara Toplam + Toplam Brüt Masraflar + Toplam İşveren = Aylık Genel Maliyet (ana tablodaki Ara Toplam ve Toplam Brüt Masraflar ile birebir; kesintiler bu tabloda ayrı gösterilmez)
    // Net Toplam ve Ek Ödemeler hücrelerinde R üstte / X altta (ana tablo ile aynı gösterim; toplanmış tek rakam yanıltır)
    const ekOdemeR = r => ['yol', 'yemek', 'mesai', 'prim', 'diger'].reduce((s, k) => s + (Number(r[k]) || 0), 0), ekOdemeX = r => ['yolX', 'yemekX', 'kiraX', 'mesaiX', 'primX'].reduce((s, k) => s + (Number(r[k]) || 0), 0);
    $('#pmCikisTablo tbody').innerHTML = L2.map((r, i) => { const k = cikisAnahtar(r), sec = cikisSecim.has(k), h = cikisHesap(r); return `<tr class="${sec ? 'cikacak' : ''}" data-k="${esc(k)}" data-kaynak="${esc(r.kaynak)}" data-sira0="${r.sira0 ?? ''}" data-usira="${r.uSira ?? ''}"><td class="kucuk">${i + 1}</td><td class="kaynak k-${r.kaynak === 'Üretim' ? 'uretim' : r.kaynak === 'Özel' ? 'ozel' : 'bordro'}">${esc(r.kaynak)}</td><td class="ad">${adOz(r.adSoyad, r.sira0)}</td><td>${esc(r.gorev || '')}</td><td>${lokasyon(r.gorevYeri)}</td><td class="s">${r.kidemYil === null || r.kidemYil === undefined ? '—' : ondalik(r.kidemYil, 1)}</td><td class="s rx" data-k="netToplam">${rxHucre(Number(r.rNet) || 0, Number(r.xNet) || 0)}</td><td class="s rx" data-k="ekOdeme">${rxHucre(ekOdemeR(r), ekOdemeX(r))}</td><td class="s" data-k="araToplam">${hucre(r.araToplam)}</td><td class="s" data-k="toplamBrutMasraflar">${hucre(r.brutTam)}</td><td class="s" data-k="toplamIsveren">${hucre(r.toplamIsveren)}</td><td class="s hesap" data-k="genelMaliyet">${hucre(r.genelMaliyet)}</td><td class="s">${hucre(h.yillik)}</td><td class="s">${hucre(h.kidem)}</td><td class="s">${hucre(h.ihbar)}</td><td class="s hesap">${hucre(h.cikis)}</td><td><select class="form-select pmCikisSec ${sec ? 'cikacak' : ''}" data-k="${esc(k)}" aria-label="Senaryo · ${esc(r.adSoyad)}"><option value="kal" ${sec ? '' : 'selected'}>Kalıyor</option><option value="cik" ${sec ? 'selected' : ''}>İşten Çıkacak</option></select></td></tr>`; }).join('') || '<tr><td colspan="17" class="na-bos">Filtreye uyan kişi yok.</td></tr>';
    const secili = tum.filter(r => cikisSecim.has(cikisAnahtar(r))), top = (l, k) => l.reduce((s, r) => s + (Number(r[k]) || 0), 0), topH = (l, k) => l.reduce((s, r) => s + cikisHesap(r)[k], 0);
    const mevcut = top(tum, 'genelMaliyet'), cikan = top(secili, 'genelMaliyet'), kalan = mevcut - cikan, cikisMaliyet = topH(secili, 'cikis');
    // dip toplam: her sütunun altında kendi toplamı (birleşik hücre yok; sola sabit hücreler toplamları örtmesin); R / X hücreleri altlı üstlü
    $('#pmCikisTablo tfoot tr').innerHTML = `<td>Toplam</td><td>${L2.length} kişi</td><td>${secili.length} işten çıkacak</td><td></td><td></td><td></td><td class="s rx" data-k="netToplam">${rxHucre(top(L2, 'rNet'), top(L2, 'xNet'), kisa, true)}</td><td class="s rx" data-k="ekOdeme">${rxHucre(L2.reduce((s, r) => s + ekOdemeR(r), 0), L2.reduce((s, r) => s + ekOdemeX(r), 0), kisa, true)}</td><td class="s" data-k="araToplam">${kisa(top(L2, 'araToplam'))}</td><td class="s" data-k="toplamBrutMasraflar">${kisa(top(L2, 'brutTam'))}</td><td class="s" data-k="toplamIsveren">${kisa(top(L2, 'toplamIsveren'))}</td><td class="s" data-k="genelMaliyet">${kisa(top(L2, 'genelMaliyet'))}</td><td class="s">${kisa(topH(L2, 'yillik'))}</td><td class="s">${kisa(topH(L2, 'kidem'))}</td><td class="s">${kisa(topH(L2, 'ihbar'))}</td><td class="s">${kisa(topH(L2, 'cikis'))}</td><td></td>`;
    const kpi = [['#0f172a', 'Mevcut Aylık Genel Maliyet', kisa(mevcut), `${tum.length} kişi · yıllık ${kisa(mevcut * 12)}`], ['#dc2626', 'İşten Çıkacak', `${secili.length} Kişi`, secili.length ? secili.slice(0, 4).map(r => r.adSoyad).join(', ') + (secili.length > 4 ? ' …' : '') : 'tabloda işaretleyin'],
      ['#b45309', 'Çıkacakların Aylık Maliyeti', kisa(cikan), `yıllık tasarruf ${kisa(cikan * 12)}`], ['#0f766e', 'Kalan Aylık Genel Maliyet', kisa(kalan), `${tum.length - secili.length} kişi · yıllık ${kisa(kalan * 12)}`],
      ['#7c3aed', 'Tahmini Tek Seferlik Çıkış Maliyeti', kisa(cikisMaliyet), `kıdem ${kisa(topH(secili, 'kidem'))} · ihbar ${kisa(topH(secili, 'ihbar'))}`], ['#1d4ed8', 'Geri Dönüş', cikan > 0 ? ondalik(cikisMaliyet / cikan, 1) + ' ay' : '—', 'çıkış maliyeti / aylık tasarruf']];
    $('#pmCikisKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $('#pmCikisSayi').textContent = `${L2.length} / ${tum.length} kişi · ${secili.length} işten çıkacak · ${V.donemAd} · tutarlar ₺ · senaryo veriyi değiştirmez, planınız saklanır`;
    cikisDepartmanCiz(tum);

    $$('#pmCikisTablo .pmCikisSec').forEach(c => c.addEventListener('change', () => { if (c.value === 'cik') cikisSecim.add(c.dataset.k); else cikisSecim.delete(c.dataset.k); cikisSakla(); cikisCiz(); }));
    sabitleHepsi();
  }
  function cikisSakla() { try { localStorage.pmCikisSecim = JSON.stringify([...cikisSecim]); } catch (e) { /* yoksay */ } }
  /** departman ve departman-görev bazında kişi sayısı ve aylık genel maliyet (₺): mevcut, işten çıkacak (senaryo işaretleri) ve kalan — yalnız bu sekmede (işaretler Özet, Ödeme Planı ve diğer sekmelere yansımaz) */
  function cikisDepartmanCiz(tum) {
    const cikacak = r => cikisSecim.has(cikisAnahtar(r)), tutar = r => Number(r.genelMaliyet) || 0, dep = {}, gor = {};
    tum.forEach(r => { const d = r.catiDepartman || '—', g = r.gorev || '—', c = cikacak(r) ? 1 : 0, t = tutar(r), ct = c ? t : 0; const a = dep[d] || (dep[d] = { ad: d, n: 0, c: 0, m: 0, mc: 0 }); a.n++; a.c += c; a.m += t; a.mc += ct; const b = gor[d + '|' + g] || (gor[d + '|' + g] = { d, g, n: 0, c: 0, m: 0, mc: 0 }); b.n++; b.c += c; b.m += t; b.mc += ct; });
    const cikanSay = tum.filter(cikacak).length, toplamTutar = tum.reduce((s, r) => s + tutar(r), 0), cikanTutar = tum.filter(cikacak).reduce((s, r) => s + tutar(r), 0);
    const depS = Object.values(dep).sort((a, b) => b.n - a.n || a.ad.localeCompare(b.ad, 'tr-TR')), gorS = Object.values(gor).sort((a, b) => dep[b.d].n - dep[a.d].n || b.n - a.n || a.g.localeCompare(b.g, 'tr-TR'));
    const para = (m, mc) => `<td class="s">${ondalik(m, 2)}</td><td class="s">${mc ? ondalik(mc, 2) : '—'}</td><td class="s">${ondalik(m - mc, 2)}</td>`;
    $('#pmDepTablo tbody').innerHTML = depS.map(a => `<tr><td>${esc(a.ad)}</td><td class="s sayac">${a.n}</td><td class="s sayac">${a.c || '—'}</td><td class="s sayac">${a.n - a.c}</td>${para(a.m, a.mc)}</tr>`).join('') + `<tr class="toplam"><td>Toplam</td><td class="s sayac">${tum.length}</td><td class="s sayac">${cikanSay || '—'}</td><td class="s sayac">${tum.length - cikanSay}</td>${para(toplamTutar, cikanTutar)}</tr>`;
    $('#pmDepGorevTablo tbody').innerHTML = gorS.map(b => `<tr><td>${esc(b.d)}</td><td>${esc(b.g)}</td><td class="s sayac">${b.n}</td><td class="s sayac">${b.c || '—'}</td><td class="s sayac">${b.n - b.c}</td>${para(b.m, b.mc)}</tr>`).join('') + `<tr class="toplam"><td>Toplam</td><td></td><td class="s sayac">${tum.length}</td><td class="s sayac">${cikanSay || '—'}</td><td class="s sayac">${tum.length - cikanSay}</td>${para(toplamTutar, cikanTutar)}</tr>`;
  }

  // ============================================================
  // ÖDEME PLANI — ödenecek / ödenmeyecek seçimiyle bankadan ve elden ödenecek tutarlar, finans ihtiyacı (planlama; muhasebe kayıtlarını değiştirmez). İşten Çıkış Senaryosu işaretleri buraya yansımaz (senaryo kendi sekmesine özeldir)
  // ============================================================
  let odemeSecim = {}; try { odemeSecim = JSON.parse(localStorage.pmOdemeDurum || '{}') || {}; } catch (e) { /* yoksay */ }
  let odemeAraBagli = false, odemeSatirlar = [];   // odemeSatirlar: son çizimdeki satırlar (gerçekleşen ödeme girişi için)
  /** GERÇEKLEŞEN ÖDEME GİRİŞİ (9 Kas 2026, "aynı rakam mı ödendi … farklı rakam ödendiyse manuel girebileyim"): planlanan banka / elden tutarın karşısına fiilen ödenen tutar, tarih ve not;
   *  "Planlanan Tutarları Yaz" aynı tutar ödendiyse doldurur; boş alan = girilmedi (null), 0 = ödenmedi. Kayıt sunucuda, dönem bazında, kalıcı (odeme günlüğü) */
  function odemeGirisAc(k) {
    const s = odemeSatirlar.find(x => x.k === k); if (!s) return;
    const o = s.od || {}, bosNull = id => { const e = $('#pmd_' + id); const v = e ? String(e.value).trim() : ''; return v === '' ? null : (Number(v) || 0); }, isaret = f => (f > 0 ? '+' : '') + kisa(f);
    const veri = { planBanka: kisa(s.banka), planElden: kisa(s.elden), odenenBanka: o.odenenBanka != null ? Number(o.odenenBanka) : '', odenenElden: o.odenenElden != null ? Number(o.odenenElden) : '', bankaTarih: o.bankaTarih || '', eldenTarih: o.eldenTarih || '', not: o.not || '' };
    duzenleKart(`<i class="fas fa-hand-holding-dollar me-1"></i>Gerçekleşen Ödeme · ${esc(s.r.adSoyad)} · ${esc(V.donemAd || '')}`, [
      { k: 'planBanka', ad: 'Planlanan (Banka)', tip: 'salt' }, { k: 'odenenBanka', ad: 'Ödenen (Banka) ₺', tip: 'number' }, { k: 'bankaTarih', ad: 'Banka Ödeme Tarihi', tip: 'date' },
      { k: 'planElden', ad: 'Planlanan (Elden)', tip: 'salt' }, { k: 'odenenElden', ad: 'Ödenen (Elden) ₺', tip: 'number' }, { k: 'eldenTarih', ad: 'Elden Ödeme Tarihi', tip: 'date' },
      { k: 'not', ad: 'Not (farkın nedeni vb.)', tip: 'textarea' }
    ], veri, d => post('/PersonelMaliyet/OdemeKaydet', { donem: V.donem, anahtar: k, odenenBanka: bosNull('odenenBanka'), odenenElden: bosNull('odenenElden'), bankaTarih: d.bankaTarih, eldenTarih: d.eldenTarih, not: d.not }), s.od ? () => post('/PersonelMaliyet/OdemeKaydet', { donem: V.donem, anahtar: k, sil: true }) : null,
      () => { const b = bosNull('odenenBanka'), e = bosNull('odenenElden'); if (b == null && e == null) return 'Fiilen ödenen tutarları girin; planlananla aynı ödendiyse <b>Planlanan Tutarları Yaz</b> düğmesini kullanın. Boş alan "girilmedi", 0 "ödenmedi" demektir.'; const fb = (b || 0) - s.banka, fe = (e || 0) - s.elden, f = fb + fe; return `Banka: ödenen ${kisa(b || 0)} − planlanan ${kisa(s.banka)} = <b>${isaret(fb)}</b> · Elden: ödenen ${kisa(e || 0)} − planlanan ${kisa(s.elden)} = <b>${isaret(fe)}</b> · toplam fark <b>${isaret(f)}</b> → <b>${Math.abs(f) < 0.005 ? 'Tam Ödendi' : f < 0 ? 'Eksik Ödendi' : 'Fazla Ödendi'}</b>.`; });
    const hb = document.createElement('button'); hb.type = 'button'; hb.className = 'btn btn-light btn-sm py-0'; hb.id = 'pmdPlanYaz'; hb.title = 'Bankadan ve elden planlanan tutarlar aynen ödendiyse'; hb.innerHTML = '<i class="fas fa-equals me-1"></i>Planlanan Tutarları Yaz';
    hb.addEventListener('click', () => { const b = $('#pmd_odenenBanka'), e = $('#pmd_odenenElden'); b.value = s.banka.toFixed(2); e.value = s.elden.toFixed(2); b.dispatchEvent(new Event('input', { bubbles: true })); });
    $('#pmTalepAksiyon').prepend(hb);
  }
  function odemeSakla() { try { localStorage.pmOdemeDurum = JSON.stringify(odemeSecim); } catch (e) { /* yoksay */ } }
  /** varsayılan: kayıttaki "Ödeme Yapılmıyor" notu olan kişi ödenmez; tarayıcıdaki seçim varsayılanı değiştirir */
  const odemeVarsayilan = r => { const b = r.kaynak === 'Üretim' ? null : bul(r.sira0); return !(b && b.bankaBlokeli) && (r.not || '').includes('Ödeme Yapılmıyor') ? 'odenmeyecek' : 'odenecek'; };   // bloke hesapta (R elden ödenir) eski not ödemeyi engellemez
  const odemeDurum = r => odemeSecim[cikisAnahtar(r)] || odemeVarsayilan(r);
  /** banka = Net-R Bordro (bankaya yatacak); elden = Kalan Net Tutar (Elden); SGK'sız gündelikçi tamamı elden */
  function odemeTutar(r) {
    const p = r.kaynak === 'Üretim' ? null : bul(r.sira0);
    if (!p) return { rNet: 0, digerR: 0, kesR: 0, kalanBanka: 0, banka: 0, xNet: Number(r.xNet) || 0, digerX: (Number(r.yolX) || 0) + (Number(r.yemekX) || 0) + (Number(r.kiraX) || 0) + (Number(r.mesaiX) || 0) + (Number(r.primX) || 0), kesX: Number(r.kesintiX) || 0, elden: Number(r.netOdenen) || 0, nakitR: 0 };
    const n = k => Number(p[k]) || 0;
    return { rNet: n('rNet2'), digerR: n('digerEkR'), kesR: n('kesintilerToplami'), kalanBanka: n('bankaOdeme'), banka: n('bankaOdeme'), xNet: n('xNet2'), digerX: n('digerEkOdemelerX'), kesX: n('kesintilerToplamiX'), elden: n('eldenOdeme'), nakitR: n('nakitR') };
  }
  function odemeCiz() {
    if (!odemeAraBagli) {
      $('#pmOdemeAra').addEventListener('input', odemeCiz);
      $$('#pmOdemeDurumOde, #pmOdemeDurumOdmz').forEach(c => c.addEventListener('change', odemeCiz));
      dugmeBagla('#pmOdemeExcel', '#pmOdemePdf', bicim => tabloExcel('odeme_plani', [{ tablo: '#pmOdemeTablo', ad: 'Ödeme Planı', baslik: `Konsolide İK Paneli · Ödeme Planı · ${V.donemAd || ''}`, altBaslik: $('#pmOdemeSayi').textContent.trim(), kpi: '#pmOdemeKpi', not: '.na-panel[data-p="odeme"] .pm-not' }, { tablo: '#pmFinansTablo', ad: 'Şirket Bazında Ödeme', baslik: `Konsolide İK Paneli · Şirket Bazında Toplu Ödeme · ${V.donemAd || ''}`, altBaslik: $('#pmFinansSayi').textContent.trim() }], bicim));
      odemeAraBagli = true;
    }
    $('#pmOdemeBaslik').textContent = `${V.donemAd} Ödeme Planı`;
    const ara = ($('#pmOdemeAra').value || '').toLocaleLowerCase('tr-TR'), goster = { odenecek: $('#pmOdemeDurumOde').checked, odenmeyecek: $('#pmOdemeDurumOdmz').checked };
    const tum = birlesimListe('').map(r => { const t = odemeTutar(r), k = cikisAnahtar(r), odenecek = odemeDurum(r) === 'odenecek', g = v => odenecek ? v : 0; return { r, k, odenecek, rNet: g(t.rNet), digerR: g(t.digerR), kesR: g(t.kesR), kalanBanka: g(t.kalanBanka), banka: g(t.banka), xNet: g(t.xNet), digerX: g(t.digerX), kesX: g(t.kesX), elden: g(t.elden), tutarTum: t.banka + t.elden, bloke: t.nakitR > 0 }; });
    tum.forEach(s => { s.toplam = s.banka + s.elden; });
    // gerçekleşen ödeme (9 Kas 2026): sunucudaki kayıt (V.odemeler, dönem bazında) → ödenen banka / elden, fark = ödenen − planlanan, durum
    const OD = {}; (V.odemeler || []).forEach(o => { OD[o.anahtar] = o; });
    tum.forEach(s => {
      const o = OD[s.k] || null, ob = o && o.odenenBanka != null ? Number(o.odenenBanka) : null, oe = o && o.odenenElden != null ? Number(o.odenenElden) : null;
      s.od = o; s.odBanka = ob; s.odElden = oe; s.odVar = ob != null || oe != null; s.farkBanka = ob == null ? null : ob - s.banka; s.farkElden = oe == null ? null : oe - s.elden;
      const topOd = (ob || 0) + (oe || 0), fark = topOd - s.toplam; s.odToplam = s.odVar ? topOd : null; s.odFark = s.odVar ? fark : null;
      s.odDurum = !s.odVar ? (s.odenecek ? 'Girilmedi' : '—') : Math.abs(fark) < 0.005 ? 'Tam Ödendi' : fark < 0 ? 'Eksik Ödendi' : 'Fazla Ödendi';
    });
    odemeSatirlar = tum;
    const odH = v => v == null ? '<span class="text-muted">—</span>' : hucre(v), isaret = f => (f > 0 ? '+' : '') + kisa(f);
    const farkH = f => f == null ? '<span class="text-muted">—</span>' : Math.abs(f) < 0.005 ? '<span class="pm-rz ok">0,00 ₺</span>' : `<span class="${f < 0 ? 'text-danger' : 'text-primary'} fw-bold">${isaret(f)}</span>`;
    const durumRz = s => s.odDurum === 'Tam Ödendi' ? '<span class="pm-rz ok">Tam Ödendi</span>' : s.odDurum === 'Eksik Ödendi' ? '<span class="pm-rz hata">Eksik Ödendi</span>' : s.odDurum === 'Fazla Ödendi' ? '<span class="pm-rz mavi">Fazla Ödendi</span>' : s.odDurum === 'Girilmedi' ? '<span class="pm-rz gri">Girilmedi</span>' : '<span class="text-muted">—</span>';
    const tarihKisa = t => t ? String(t).slice(0, 10).split('-').reverse().join('.') : '';
    const L2 = tum.filter(s => (s.odenecek ? goster.odenecek : goster.odenmeyecek) && (!ara || [s.r.adSoyad, s.r.isveren, s.r.gorevYeri, s.r.gorev].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    const top = (l, k) => l.reduce((s, x) => s + (Number(x[k]) || 0), 0);
    $('#pmOdemeTablo tbody').innerHTML = L2.map((s, i) => `<tr class="${s.odenecek ? '' : 'odenmez'}" data-k="${esc(s.k)}"><td class="kucuk">${i + 1}</td><td class="kaynak k-${s.r.kaynak === 'Üretim' ? 'uretim' : s.r.kaynak === 'Özel' ? 'ozel' : 'bordro'}">${esc(s.r.kaynak)}</td><td class="ad">${adOz(s.r.adSoyad, s.r.sira0)}${s.bloke ? ' <span class="pm-rz mavi" title="Banka hesabı blokeli: R tutarı elden ödenir">R → elden</span>' : ''}</td><td>${lokasyon(s.r.gorevYeri)}</td><td class="s">${hucre(s.rNet)}</td><td class="s">${hucre(s.digerR)}</td><td class="s">${hucre(s.kesR)}</td><td class="s">${hucre(s.kalanBanka)}</td><td class="s">${hucre(s.xNet)}</td><td class="s">${hucre(s.digerX)}</td><td class="s">${hucre(s.kesX)}</td><td class="s">${hucre(s.elden)}</td><td class="s hesap">${hucre(s.toplam)}</td><td class="s od-gir" title="Tıklayın: fiilen ödenen tutarı girin">${odH(s.odBanka)}${s.od && s.od.bankaTarih ? `<small class="alt">${esc(tarihKisa(s.od.bankaTarih))}</small>` : ''}</td><td class="s od-gir" title="Tıklayın: fiilen ödenen tutarı girin">${odH(s.odElden)}${s.od && s.od.eldenTarih ? `<small class="alt">${esc(tarihKisa(s.od.eldenTarih))}</small>` : ''}</td><td class="s od-gir">${farkH(s.farkBanka)}</td><td class="s od-gir">${farkH(s.farkElden)}</td><td class="od-gir">${durumRz(s)}${s.od && s.od.not ? `<small class="alt">${esc(s.od.not)}</small>` : ''}</td><td><select class="form-select pmOdemeDurum" data-k="${esc(s.k)}" aria-label="Ödeme · ${esc(s.r.adSoyad)}"><option value="odenecek" ${s.odenecek ? 'selected' : ''}>Ödenecek</option><option value="odenmeyecek" ${s.odenecek ? '' : 'selected'}>Ödenmeyecek</option></select></td></tr>`).join('') || '<tr><td colspan="19" class="na-bos">Filtreye uyan kişi yok.</td></tr>';
    $('#pmOdemeTablo tfoot tr').innerHTML = `<td>Toplam</td><td>${L2.length} kişi</td><td></td><td></td><td class="s">${kisa(top(L2, 'rNet'))}</td><td class="s">${kisa(top(L2, 'digerR'))}</td><td class="s">${kisa(top(L2, 'kesR'))}</td><td class="s">${kisa(top(L2, 'kalanBanka'))}</td><td class="s">${kisa(top(L2, 'xNet'))}</td><td class="s">${kisa(top(L2, 'digerX'))}</td><td class="s">${kisa(top(L2, 'kesX'))}</td><td class="s">${kisa(top(L2, 'elden'))}</td><td class="s">${kisa(top(L2, 'toplam'))}</td><td class="s">${kisa(top(L2, 'odBanka'))}</td><td class="s">${kisa(top(L2, 'odElden'))}</td><td class="s">${farkH(L2.some(s => s.farkBanka != null) ? top(L2, 'farkBanka') : null)}</td><td class="s">${farkH(L2.some(s => s.farkElden != null) ? top(L2, 'farkElden') : null)}</td><td><small>${L2.filter(s => s.odDurum === 'Tam Ödendi').length} tam · ${L2.filter(s => s.odDurum === 'Eksik Ödendi').length} eksik · ${L2.filter(s => s.odDurum === 'Fazla Ödendi').length} fazla · ${L2.filter(s => s.odDurum === 'Girilmedi').length} girilmedi</small></td><td>${L2.filter(s => s.odenecek).length} ödenecek</td>`;
    const ode = tum.filter(s => s.odenecek), odmz = tum.filter(s => !s.odenecek);
    const banka = top(ode, 'banka'), elden = top(ode, 'elden');
    const odeGir = ode.filter(s => s.odVar), odenen = top(odeGir, 'odToplam'), odFark = odenen - top(odeGir, 'toplam');   // gerçekleşen: yalnız ödeme girilen ödenecek satırlar
    const kpi = [['#0f172a', 'Ödenecek Kişi', `${ode.length} / ${tum.length}`, `ödenmeyecek ${odmz.length}`],
      ['#0f766e', 'Bankadan Ödenecek (Maaş)', kisa(banka), 'R net · Net-R Bordro'],
      ['#b45309', 'Elden Ödenecek (Maaş)', kisa(elden), 'X net · Kalan Net Tutar (Elden)'],
      ['#dc2626', 'Finans İhtiyacı', kisa(banka + elden), `bankadan ${kisa(banka)} · elden ${kisa(elden)}`],
      ['#64748b', 'Ödenmeyecek Tutar', kisa(top(odmz, 'tutarTum')), 'seçimle ödenmeyen net maaş'],
      ['#15803d', 'Gerçekleşen Ödeme', kisa(odenen), `${odeGir.length} / ${ode.length} kişi girildi · banka ${kisa(top(odeGir, 'odBanka'))} · elden ${kisa(top(odeGir, 'odElden'))}`],
      [odFark < -0.005 ? '#dc2626' : odFark > 0.005 ? '#2563eb' : '#64748b', 'Ödeme Farkı (Ödenen − Planlanan)', odeGir.length ? isaret(odFark) : '—', `${ode.filter(s => s.odDurum === 'Tam Ödendi').length} tam · ${ode.filter(s => s.odDurum === 'Eksik Ödendi').length} eksik · ${ode.filter(s => s.odDurum === 'Fazla Ödendi').length} fazla · ${ode.length - odeGir.length} girilmedi`]];
    $('#pmOdemeKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $('#pmOdemeSayi').textContent = `${L2.length} / ${tum.length} kişi · ${V.donemAd} · tutarlar ₺ · seçimler tarayıcıda saklanır`;

    $$('#pmOdemeTablo .pmOdemeDurum').forEach(c => c.addEventListener('change', () => { odemeSecim[c.dataset.k] = c.value; odemeSakla(); odemeCiz(); }));
    finansCiz(ode);
    sabitleHepsi();
  }
  // ---- ŞİRKET BAZINDA TOPLU ÖDEME → KASA NAKİT AKIŞI (9 Kas 2026, "firma firma görebilelim: ALV Kimya R ödeme şu kadar, X ödeme şu kadar … Kasa Nakit Akışı'na toplu gönderelim"):
  //      ödenecek satırlar fiili görev yeri şirketine göre (yansıtma esası) toplanır; sunucu şirket başına R (Havale / EFT) ve X (Nakit) satırı yazar (Data/PersonelFinansOrnek.cs); durum V.finans
  let finansBagli = false, finansListe = [];
  const KASA_SIRKETLER = ['Uras Kimya', 'Selvi Kimya', 'Selvi', 'Avrupa Paper', 'ALV Kimya', 'Alv Kimya', 'Daf Kimya', 'URS Makine', 'URS Makina', 'Uras Holding'];   // Kasa Nakit Akışı şirketleri (CekAkibetOrnek.Sirketler eşlemesi)
  const kasaYok = s => !KASA_SIRKETLER.some(k => k.toLocaleLowerCase('tr-TR') === String(s || '').toLocaleLowerCase('tr-TR'));
  const finansSirket = r => r.kaynak === 'Üretim' ? (r.isveren || '—') : ((bul(r.sira0) || {}).fiiliSirket || r.isveren || '—');   // satırın fiili görev yeri şirketi (sunucu DTO fiiliSirket; üretim satırında isveren)
  const isoBugun = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; };
  const trTarih = s => s ? String(s).slice(0, 10).split('-').reverse().join('.') : '';
  function finansSatirlar(ode) { const m = new Map(); ode.forEach(s => { const ad = finansSirket(s.r); const g = m.get(ad) || { sirket: ad, kisi: 0, banka: 0, elden: 0 }; g.kisi++; g.banka += s.banka; g.elden += s.elden; m.set(ad, g); }); return [...m.values()].sort((a, b) => a.sirket.localeCompare(b.sirket, 'tr')); }
  function finansCiz(ode) {
    const t = $('#pmFinansTablo'); if (!t) return;
    if (!finansBagli) { finansBagli = true; const ti = $('#pmFinansTarih'); if (ti && !ti.value) ti.value = isoBugun(); $('#pmFinansGonder').addEventListener('click', finansGonder); $('#pmFinansGeriAl').addEventListener('click', finansGeriAl); const git = $('#pmFinansGit'); if (git) git.addEventListener('click', () => $('#pmFinansBaslik').scrollIntoView({ behavior: 'smooth', block: 'start' })); }   // şeritteki kısayol: tabloya kaydır (href="#…" <base href> yüzünden köke gider, düğme kullanılır)
    const F = {}; (V.finans || []).forEach(f => { F[f.anahtar] = f; });
    const L = finansListe = finansSatirlar(ode);
    const durumH = (sirket, kanal, tutar) => { const f = F[`${V.donem}|${sirket}|${kanal}`]; if (!f) return tutar > 0 ? (kasaYok(sirket) ? '<span class="pm-rz hata" title="Kasa Nakit Akışı\'nda şirket tanımlı değil">Gönderilemez</span>' : '<span class="pm-rz gri">Gönderilmedi</span>') : '<span class="text-muted">—</span>';
      const degisti = Math.abs((Number(f.tutar) || 0) - tutar) > 0.005, rz = f.durum === 'ÖDENDİ' ? 'ok' : f.onay === 'Onay Bekliyor' ? 'gri' : 'mavi', ad = f.durum === 'ÖDENDİ' ? 'Ödendi' : f.onay === 'Onay Bekliyor' ? 'Nakit Onayı Bekliyor' : f.durum;
      return `<a href="/Rapor79?sirket=${encodeURIComponent(f.db)}" target="_blank" rel="noopener" title="Kasa Nakit Akışı · ${esc(f.kasaSirket || '')} · satır #${f.docEntry} · ${esc(f.yontem || '')}">#${f.docEntry}</a> <span class="pm-rz ${rz}">${esc(ad)}</span><small class="alt">${kisa(f.tutar)}${f.plan ? ' · plan ' + trTarih(f.plan) : ''}${f.durum === 'ÖDENDİ' && f.odenme ? ' · ödendi ' + trTarih(f.odenme) : ''}${degisti ? ' · <span class="text-danger fw-bold">tutar değişti, yeniden gönderin</span>' : ''}</small>`; };
    $('tbody', t).innerHTML = L.map(g => `<tr><td><b>${esc(g.sirket)}</b>${kasaYok(g.sirket) ? ' <small class="alt text-danger">Kasa Nakit Akışı\'nda tanımlı değil</small>' : ''}</td><td class="s">${g.kisi}</td><td class="s">${hucre(g.banka)}</td><td class="s">${hucre(g.elden)}</td><td class="s hesap">${hucre(g.banka + g.elden)}</td><td>${durumH(g.sirket, 'R', g.banka)}</td><td>${durumH(g.sirket, 'X', g.elden)}</td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Ödenecek satır yok.</td></tr>';
    const tb = L.reduce((s, g) => s + g.banka, 0), te = L.reduce((s, g) => s + g.elden, 0), G = (V.finans || []).filter(f => (f.anahtar || '').startsWith(V.donem + '|'));
    $('tfoot tr', t).innerHTML = `<td>Toplam · ${L.length} şirket</td><td class="s">${L.reduce((s, g) => s + g.kisi, 0)}</td><td class="s">${kisa(tb)}</td><td class="s">${kisa(te)}</td><td class="s">${kisa(tb + te)}</td><td colspan="2"><small>${G.length} satır Kasa Nakit Akışı'nda · ${G.filter(f => f.durum === 'ÖDENDİ').length} ödendi · ${G.filter(f => f.onay === 'Onay Bekliyor').length} nakit onayı bekliyor · gönderilen toplam ${kisa(G.reduce((s, f) => s + (Number(f.tutar) || 0), 0))}</small></td>`;
    $('#pmFinansSayi').textContent = `${L.length} şirket · ${V.donemAd} · yalnız "Ödenecek" satırlar · R bankadan Havale / EFT (İşlem Tipi R) · X elden Nakit (İşlem Tipi X, nakit onayı gerekir) · aynı dönem / şirket / kanal tek satır`;
  }
  async function finansGonder() {
    const L = finansListe.filter(g => !kasaYok(g.sirket) && (g.banka > 0 || g.elden > 0)), yok = finansListe.filter(g => kasaYok(g.sirket) && (g.banka > 0 || g.elden > 0));
    if (!L.length) { mesaj('warning', 'Gönderilecek ödenecek satır yok.'); return; }
    const tarih = $('#pmFinansTarih').value || isoBugun();
    if (!window.confirm(`${V.donemAd} için ${L.length} şirketin toplu personel ödemesi Kasa Nakit Akışı'na gönderilsin mi?\n\nBankadan (R): ${kisa(L.reduce((s, g) => s + g.banka, 0))}\nElden (X): ${kisa(L.reduce((s, g) => s + g.elden, 0))}\nPlanlanan tarih: ${trTarih(tarih)}${yok.length ? `\n\nKasa Nakit Akışı'nda tanımlı olmayan şirketler gönderilmez: ${yok.map(g => g.sirket).join(', ')}` : ''}`)) return;
    const j = await post('/PersonelMaliyet/FinansGonder', { donem: V.donem, planTarih: tarih, satirlar: finansListe.map(g => ({ sirket: g.sirket, kisi: g.kisi, banka: Math.round(g.banka * 100) / 100, elden: Math.round(g.elden * 100) / 100 })) });
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    veriGuncelle(j); mesaj('success', esc(j.message));
  }
  async function finansGeriAl() {
    const G = (V.finans || []).filter(f => (f.anahtar || '').startsWith(V.donem + '|') && f.durum !== 'ÖDENDİ');
    if (!G.length) { mesaj('warning', 'Bu dönem için Kasa Nakit Akışı\'nda kaldırılacak ödenmemiş satır yok.'); return; }
    if (!window.confirm(`${V.donemAd} için Kasa Nakit Akışı'ndaki ${G.length} ödenmemiş toplu personel ödemesi satırı kaldırılsın mı? (Ödenmiş satırlar kalır.)`)) return;
    const j = await post('/PersonelMaliyet/FinansGeriAl', { donem: V.donem });
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    veriGuncelle(j); mesaj('success', esc(j.message));
  }

  // ============================================================
  // ZAM SENARYOSU — maaş zammı planlaması (veri, bordro ve muhasebe kayıtları değişmez)
  //   Toplu R % ve Toplu X % tüm kişilere uygulanır ("R ve X aynı oran" açıksa X, R'yi izler). Zam % (R / X) sütunu kişiye özel orandır; boşaltılırsa toplu orana döner.
  //   Zam yalnız maaşa uygulanır: R-Net ve X-Net (dövizli maaşta döviz tutarı); üretimde günlük ücret (hak ediş ve tatil). Ek ödemeler (yol, yemek, prim, fazla mesai) sabit kalır.
  //   Satıra tıklanınca kişi kartı açılır: satırın tüm değerleri (mevcut, zamlı, artış, %) ve Net Zamlı X-Net tutarı (₺) kartta elle girilir; tutar yüzdenin yerine geçer ve tüm değerler buna göre hesaplanır.
  //   Bordrolu ve özel kişide zamsız ve zamlı tutar bordro kurallarıyla (hesapla) hesaplanır; farkı alınır.
  //   Ara Toplam = Net-X Bordro + Kalan Net (Banka); Genel Maliyet = Ara Toplam + Toplam Brüt Masraflar + SGK İşveren. Kesintiler raporda gösterilmez (maaştan düşer).
  //   Senaryolar (ad, R / X oranları, kişi bazlı oranlar ve tutarlar, gizleme) bu tarayıcıda saklanır; Senaryo Karşılaştırması sayfasında yan yana durur.
  // ============================================================
  const zamSayi = v => Math.max(0, Number(v) || 0);
  /** zam ayarı: r ve x (toplu %), ayni (X = R), kisi (anahtar → { r?, x?, xTl? }: kişi bazlı % ve X-Net tutarı); önceki sürümün tek oranlı kayıtları da okunur */
  function zamOku() {
    try {
      const a = JSON.parse(localStorage.pmZamAyar || 'null');
      if (a) return { r: zamSayi(a.r), x: zamSayi(a.x), ayni: a.ayni !== false, kisi: a.kisi || {}, dovizGizle: a.dovizGizle !== false };
      const t = zamSayi(localStorage.pmZamToplu), kisi = {};   // önceki sürüm: tek oran hem R hem X'e uygulanırdı
      Object.entries(JSON.parse(localStorage.pmZamKisi || '{}') || {}).forEach(([k, v]) => { kisi[k] = { r: zamSayi(v), x: zamSayi(v) }; });
      return { r: t, x: t, ayni: true, kisi, dovizGizle: true };
    } catch (e) { return { r: 0, x: 0, ayni: true, kisi: {}, dovizGizle: true }; }
  }
  let zamAyar = zamOku(), zamAcik = '', zamSon = [], zamCekK = null;   // zamAcik: kayıtlı senaryo yeni sekmede açıldıysa adı; zamSon: ekrandaki satırlar; zamCekK: kişi kartında açık kişinin anahtarı
  const zamAyarSakla = () => { try { localStorage.pmZamAyar = JSON.stringify(zamAyar); } catch (e) { /* yoksay */ } };
  /** zam ayarı değişince bu tarayıcıya yazılır ve tablo yenilenir */
  const zamAyarKaydet = () => { zamAyarSakla(); zamCiz(); };
  const zamAnahtar = p => p.uSira ? 'u' + p.uSira : String(p.sira);
  /** ekrandaki zam ayarı: X, aynı oran açıksa R'ye eşit; kişi bazlı değerler ve gizleme durumu */
  const zamAyarOku = () => ({ r: zamAyar.r, x: zamAyar.ayni ? zamAyar.r : zamAyar.x, ayni: zamAyar.ayni, kisi: zamAyar.kisi, gizle: { uretim: gizle.uretim, ozel: gizle.ozel }, dovizGizle: zamAyar.dovizGizle !== false });
  /** kişinin uygulanan oranları (%) ve X-Net tutarı (₺; girilmediyse undefined): kişi bazlı değer varsa o, yoksa toplu oran */
  const zamOranlar = (p, a) => { const o = (a.kisi || {})[zamAnahtar(p)] || {}; return { r: o.r === undefined ? a.r : zamSayi(o.r), x: o.x === undefined ? a.x : zamSayi(o.x), xTl: o.xTl === undefined ? undefined : zamSayi(o.xTl) }; };
  const kaynakAd = p => p.uSira ? 'Üretim' : p.ozel ? 'Özel' : 'Bordro';
  const zamYuzde = (a, b) => b ? '% ' + ondalik(a / b * 100, 1) : '—';
  /** bordrolu / özel kişi: zamsız ve zamlı bordro (hesapla) farkı; R ve X ayrı çarpanlarla; X-Net tutarı girildiyse yüzde yerine o. Kaynak brüt ve işveren değerleri düşürülür ki zam brüte ve SGK işverene yansısın */
  function zamBordro(p, fR, fX, xTl) {
    const zamli = { ...p, brutKaynak: null, isverenKaynak: null, rNet: (Number(p.rNet) || 0) * fR };
    if (dovizli(p)) zamli.dovizTutar = xTl !== undefined ? xTl / Number(p.kur) : (Number(p.dovizTutar) || 0) * fX; else zamli.xNet = xTl !== undefined ? xTl : (Number(p.xNet) || 0) * fX;
    const a = hesapla({ ...p, brutKaynak: null, isverenKaynak: null }), b = hesapla(zamli);
    return { dR: b.rNet - a.rNet, dX: b.xNet - a.xNet, dBrutMas: b.toplamBrutMasraflar - a.toplamBrutMasraflar, dIsv: b.sgkIsverenToplam - a.sgkIsverenToplam, dGenel: b.genelMaliyet - a.genelMaliyet };
  }
  /** SGK'sız üretim: günlük ücret X tarafında artar (hak ediş ve tatil kazancı); X-Net tutarı girildiyse mevcut X'ten farkı alınır; fazla mesai ve sosyal ödemeler sabit */
  function zamUretim(u, fX, xTl, xMevcut) {
    const g = Number(u.gunlukUcret) || 0, R2 = v => Math.round(v * 100) / 100, n = k => Number(u[k]) || 0;
    const hak = gun => R2(gun * n('calisilanGun')) + R2(n('tatilGun') * gun * 2);
    const dX = xTl !== undefined ? xTl - xMevcut : hak(g * fX) - hak(g);
    return { dR: 0, dX, dBrutMas: 0, dIsv: 0, dGenel: dX };
  }
  /** zam satırları: a ayarındaki görünen kişiler (kapsam ve gizleme dahil), mevcut değerler ve zam farkları */
  function zamSatirlari(a) {
    return tumListe(false, a.gizle).map(etkin).filter(p => !(a.dovizGizle !== false && dovizli(p))).map(p => {
      const o = zamOranlar(p, a), n = k => Number(p[k]) || 0, u = p.uSira ? (V.uretim || []).find(x => x.sira === p.uSira) : null;
      const m = { rNet: n('rNet'), xNet: n('xNet'), ekR: n('digerEkR'), ekX: n('digerEkOdemelerX'), kalan: n('netRBordro'), netX: n('netXBordro'), brutMas: n('toplamBrutMasraflar'), isv: n('toplamIsveren'), genel: n('genelMaliyet') };
      return { p, oran: o, kisi: (a.kisi || {})[zamAnahtar(p)] !== undefined, m, d: u ? zamUretim(u, 1 + o.x / 100, o.xTl, m.xNet) : zamBordro(p, 1 + o.r / 100, 1 + o.x / 100, o.xTl) };
    });
  }
  /** satırın değerleri: net üçlüleri {r, x, t} (mevcut ve zamlı); ek ödemeler sabit; diğerleri {m: mevcut, z: zamlı}; artışlar tek sayı */
  function zamDegerler(s) {
    const m = s.m, d = s.d;
    return {
      net: { r: m.rNet, x: m.xNet, t: m.rNet + m.xNet }, netZ: { r: m.rNet + d.dR, x: m.xNet + d.dX, t: m.rNet + m.xNet + d.dR + d.dX }, netArt: d.dR + d.dX, dR: d.dR, dX: d.dX,
      ek: { r: m.ekR, x: m.ekX },
      ara: { m: m.netX + m.kalan, z: m.netX + m.kalan + d.dR + d.dX },
      brut: { m: m.brutMas, z: m.brutMas + d.dBrutMas },
      isv: { m: m.isv, z: m.isv + d.dIsv },
      genel: { m: m.genel, z: m.genel + d.dGenel }, genelArt: d.dGenel
    };
  }
  /** sütun toplamları: net üçlüleri, mevcut / zamlı çiftleri ve artışlar */
  function zamTopla(H) {
    const T = { net: { r: 0, x: 0, t: 0 }, netZ: { r: 0, x: 0, t: 0 }, ek: { r: 0, x: 0 }, ara: { m: 0, z: 0 }, brut: { m: 0, z: 0 }, isv: { m: 0, z: 0 }, genel: { m: 0, z: 0 }, netArt: 0, dR: 0, dX: 0, genelArt: 0 };
    H.forEach(v => {
      ['net', 'netZ'].forEach(k => { T[k].r += v[k].r; T[k].x += v[k].x; T[k].t += v[k].t; });
      ['ara', 'brut', 'isv', 'genel'].forEach(k => { T[k].m += v[k].m; T[k].z += v[k].z; });
      T.ek.r += v.ek.r; T.ek.x += v.ek.x; T.netArt += v.netArt; T.dR += v.dR; T.dX += v.dX; T.genelArt += v.genelArt;
    });
    return T;
  }
  /** tablo sütunları (ekran ve Excel): oran = R / X zam % · rxt = R / X / Toplam · netArt = ₺ ve % · ek = R üstte, X altta (sabit) · iki = mevcut / zamlı · artis = ₺ ve % */
  const ZAM_SUTUN = [
    { ad: 'Adı-Soyadı', k: 'ad' }, { ad: 'Kaynak', k: 'kaynak' }, { ad: 'Zam % (R / X)', k: 'oran' },
    { ad: 'Net Mevcut', k: 'rxt', f: 'net' }, { ad: 'Net Zamlı', k: 'rxt', f: 'netZ' }, { ad: 'Net Artışı', k: 'netArt' },
    { ad: 'Ek Ödemeler Toplam', k: 'ek' },
    { ad: 'Ara Toplam', k: 'iki', f: 'ara' }, { ad: 'Toplam Brüt Masraflar', k: 'iki', f: 'brut' }, { ad: 'SGK İşveren', k: 'iki', f: 'isv' },
    { ad: 'Genel Maliyet', k: 'iki', f: 'genel' }, { ad: 'Artış', k: 'artis' }
  ];
  const zamIki = x => `<td class="s zi2"><span class="zi"><small>Mevcut</small>${hucre(x.m)}</span><span class="zi"><small>Zamlı</small><b>${hucre(x.z)}</b></span></td>`;
  /** R / X / Toplam çizgileri; tl: X-Net tutarı girilmiş (X çizgisi koyu gösterilir) */
  const zamUcIc = (x, tl) => `<span class="z3 r"><small>R</small>${hucre(x.r)}</span><span class="z3 x${tl ? ' tl' : ''}"><small>X</small>${hucre(x.x)}</span><span class="z3 t"><small>Toplam</small><b>${hucre(x.t)}</b></span>`;
  const zamUc = (x, tl) => `<td class="s zi3" data-sort="${kurus(x.t)}">${zamUcIc(x, tl)}</td>`;
  /** net artış: ₺ üstte, net toplam üzerinden yüzde altta */
  const zamNetArt = v => `<td class="s zi2" data-sort="${kurus(v.netArt)}"><span class="zi"><b>${kurus(v.netArt) ? '+ ' + kisa(v.netArt) : '—'}</b></span><span class="zi"><small>Net</small>${kurus(v.netArt) && v.net.t ? zamYuzde(v.netArt, v.net.t) : '—'}</span></td>`;
  const zamArtis = v => `<td class="s zi2" data-sort="${kurus(v.genelArt)}"><span class="zi"><b>${kurus(v.genelArt) ? '+ ' + kisa(v.genelArt) : '—'}</b></span><span class="zi"><small>Artış</small>${kurus(v.genelArt) ? zamYuzde(v.genelArt, v.genel.m) : '—'}</span></td>`;
  /** kişinin zam % hücresi: R üstte, X altta (Üretimde yalnız X); kişi bazlı değer koyu; X tutarı girildiyse yüzde kullanılmadığı belirtilir */
  function zamOranTd(r) {
    const k = zamAnahtar(r.p), o = zamAyar.kisi[k] || {};
    const giris = (alan, etiket) => `<span class="zc"><small>${etiket}</small><input class="gir zam-kisi${o[alan] !== undefined ? ' kisi' : ''}" type="number" min="0" step="0.5" data-zk="${esc(k)}" data-zt="${alan}" value="${esc(String(r.oran[alan]))}" aria-label="${etiket} zam yüzdesi · ${esc(r.p.adSoyad)}"${alan === 'x' && o.xTl !== undefined ? ' title="X-Net tutarı girildi; bu yüzde kullanılmıyor"' : ''}></span>`;
    return `<td class="s zam-cift" data-sort="${r.p.uSira ? r.oran.x : r.oran.r}">${r.p.uSira ? '' : giris('r', 'R')}${giris('x', 'X')}</td>`;
  }
  function zamSatirHtml(r, v) {
    const p = r.p, tl = r.oran.xTl !== undefined;
    return ZAM_SUTUN.map(s => {
      if (s.k === 'ad') return `<td class="ad">${p.uSira ? esc(p.adSoyad) : adOz(p.adSoyad, p.sira)}</td>`;
      if (s.k === 'kaynak') return `<td class="kaynak k-${p.uSira ? 'uretim' : p.ozel ? 'ozel' : 'bordro'}">${kaynakAd(p)}</td>`;
      if (s.k === 'oran') return zamOranTd(r);
      if (s.k === 'rxt') return zamUc(v[s.f], s.f === 'netZ' && tl);
      if (s.k === 'netArt') return zamNetArt(v);
      if (s.k === 'ek') return `<td class="s rx">${rxHucre(v.ek.r, v.ek.x)}</td>`;
      if (s.k === 'iki') return zamIki(v[s.f]);
      return zamArtis(v);
    }).join('');
  }
  function zamToplamHtml(T, n) {
    return ZAM_SUTUN.map(s => {
      if (s.k === 'ad') return '<td>Toplam</td>';
      if (s.k === 'kaynak') return `<td>${n} kişi</td>`;
      if (s.k === 'oran') return '<td></td>';
      if (s.k === 'rxt') return zamUc(T[s.f]);
      if (s.k === 'netArt') return zamNetArt(T);
      if (s.k === 'ek') return `<td class="s rx">${rxHucre(T.ek.r, T.ek.x, kisa, true)}</td>`;
      if (s.k === 'iki') return zamIki(T[s.f]);
      return zamArtis(T);
    }).join('');
  }
  /** Zam Özeti satırları (ekran ve Excel aynı veri): ad, tutar (tek değer ya da R / X / Toplam çizgileri; '+' işaretli artış) ve ayrıntı */
  function zamOzetSatirlari(S, a, T) {
    const gM = T.genel.m, gZ = T.genel.z, gD = T.genelArt, kisiSayi = S.filter(r => r.kisi).length;
    return [
      { ad: 'Mevcut Aylık Genel Maliyet', tutar: [['', gM]], ayrinti: `${S.length} kişi · yıllık ${kisa(gM * 12)}` },
      { ad: 'Zamlı Aylık Genel Maliyet', tutar: [['', gZ]], ayrinti: `R % ${ondalik(a.r, 1)} · X % ${ondalik(a.x, 1)} · kişi bazlı ${kisiSayi} kişi` },
      { ad: 'Aylık Artış', tutar: [['+', gD]], ayrinti: `${zamYuzde(gD, gM)} · yıllık + ${kisa(gD * 12)}` },
      { ad: 'Net Artışı (R + X)', tutar: [['+', T.netArt]], ayrinti: `${T.net.t ? zamYuzde(T.netArt, T.net.t) : '—'} · R-Net ${kisa(T.dR)} · X-Net ${kisa(T.dX)}` },
      { ad: 'Net Zamlı', tutar: [['R', T.netZ.r], ['X', T.netZ.x], ['Toplam', T.netZ.t]], ayrinti: `Mevcut: R ${kisa(T.net.r)} · X ${kisa(T.net.x)} · Toplam ${kisa(T.net.t)}` },
      { ad: 'Ek Ödemeler Toplam', tutar: [['R', T.ek.r], ['X', T.ek.x]], ayrinti: 'sabit: mevcut ve zamlı aynı' }
    ];
  }
  /** özet tutarı (ekran): tek değer düz, çoklu değer R / X / Toplam çizgileri */
  const zamOzetTutar = tutar => tutar.length === 1 ? `${tutar[0][0] === '+' ? '+ ' : ''}${kisa(tutar[0][1])}` : `<div class="zam-yigin">${tutar.map(([e, d]) => `<span class="${e === 'R' ? 'r' : e === 'X' ? 'x' : 't'}"><small>${e}</small>${kisa(d)}</span>`).join('')}</div>`;
  /** özet tutarı (Excel): tek değer düz metin, çoklu değer "R … · X … · Toplam …" */
  const zamOzetMetin = tutar => tutar.length === 1 ? `${tutar[0][0] === '+' ? '+ ' : ''}${kisa(tutar[0][1])}` : tutar.map(([e, d]) => `${e} ${kisa(d)}`).join(' · ');
  function zamCiz() {
    const a = zamAyarOku(), S = zamSatirlari(a), H = S.map(zamDegerler), T = zamTopla(H);
    const gM = T.genel.m, gZ = T.genel.z, gD = T.genelArt;
    zamSon = S;
    $('#pmZamBaslik').textContent = `${V.donemAd || ''} Zam Senaryosu${zamAcik ? ' · ' + zamAcik : ''}`;
    $('#pmZamTablo thead').innerHTML = `<tr class="tek">${ZAM_SUTUN.map(s => `<th class="${s.k === 'ad' || s.k === 'kaynak' ? '' : 's'}">${esc(s.ad)}</th>`).join('')}</tr>`;
    const govde = $('#pmZamTablo tbody');
    govde.innerHTML = S.map((r, i) => `<tr data-zk="${esc(zamAnahtar(r.p))}">${zamSatirHtml(r, H[i])}</tr>`).join('') || `<tr><td colspan="${ZAM_SUTUN.length}" class="na-bos">Filtreye uyan personel yok.</td></tr>`;
    Array.from(govde.rows).forEach((tr, i) => { if (S[i]) { tr._zr = S[i]; tr._zv = H[i]; tr.dataset.zk = zamAnahtar(S[i].p); } });   // Excel: satır verisi; satır tıklanınca kart (sıralama sonrası da geçerli)
    $('#pmZamTablo tfoot tr').innerHTML = zamToplamHtml(T, S.length);
    $('#pmZamOzet tbody').innerHTML = zamOzetSatirlari(S, a, T).map(s => `<tr><td class="ad">${esc(s.ad)}</td><td class="s">${zamOzetTutar(s.tutar)}</td><td>${esc(s.ayrinti)}</td></tr>`).join('');
    const grup = {};
    S.forEach(r => { const k = kaynakAd(r.p), o = grup[k] || (grup[k] = { ad: k, n: 0, g0: 0, gD: 0 }); o.n++; o.g0 += r.m.genel; o.gD += r.d.dGenel; });
    const kirSatir = o => `<tr><td class="ad">${esc(o.ad)}</td><td class="s">${o.n}</td><td class="s">${kisa(o.g0)}</td><td class="s genel">${kisa(o.g0 + o.gD)}</td><td class="s hesap">${kisa(o.gD)}</td><td class="s">${zamYuzde(o.gD, o.g0)}</td></tr>`;
    $('#pmZamKaynak tbody').innerHTML = Object.values(grup).sort((x, y) => y.g0 - x.g0).map(kirSatir).join('')
      + `<tr class="toplam"><td>Toplam</td><td class="s">${S.length}</td><td class="s">${kisa(gM)}</td><td class="s genel">${kisa(gZ)}</td><td class="s hesap">${kisa(gD)}</td><td class="s">${zamYuzde(gD, gM)}</td></tr>`;
    const kisiSayi = S.filter(r => r.kisi).length;
    $('#pmZamSayi').textContent = `${S.length} kişi · tutarlar ₺ · R % ${ondalik(a.r, 1)} · X % ${ondalik(a.x, 1)} · kişi bazlı ${kisiSayi} kişi · veri değişmez`;
    if (zamCekK !== null) zamCekOnizle();   // açık kişi kartı güncel değerlerle kalsın
    sabitleHepsi();
  }
  /** satırın zamlı değerleri: X-Net tutarı verilirse (tl) yüzde yerine o; yoksa yüzde (ana tabloyla aynı hesap) */
  function zamKartVeri(r, tl) {
    const d = r.p.uSira ? zamUretim((V.uretim || []).find(x => x.sira === r.p.uSira), 1 + r.oran.x / 100, tl, r.m.xNet) : zamBordro(r.p, 1 + r.oran.r / 100, 1 + r.oran.x / 100, tl);
    return zamDegerler({ m: r.m, d });
  }
  /** kişi kartının tablosu: satırın tüm değerleri (mevcut, zamlı, artış, artış %); X-Net tutarı girildiyse zamlı X ona göre */
  function zamKartCiz(r, tl) {
    const v = zamKartVeri(r, tl), artis = (z, m) => kurus(z - m) ? '+ ' + kisa(z - m) : '—', yuzde = (z, m) => kurus(z - m) && m ? zamYuzde(z - m, m) : '—';
    const satirlar = [
      { ad: 'R-Net (maaş)', m: v.net.r, z: v.netZ.r },
      { ad: 'X-Net (elden)', m: v.net.x, z: v.netZ.x, tl: tl !== undefined },
      { ad: 'Net Toplam (R + X)', m: v.net.t, z: v.netZ.t, vurgu: 1 },
      { ad: 'Ek Ödemeler R', m: v.ek.r, z: v.ek.r },
      { ad: 'Ek Ödemeler X', m: v.ek.x, z: v.ek.x },
      { ad: 'Ara Toplam', m: v.ara.m, z: v.ara.z },
      { ad: 'Toplam Brüt Masraflar', m: v.brut.m, z: v.brut.z },
      { ad: 'SGK İşveren', m: v.isv.m, z: v.isv.z },
      { ad: 'Genel Maliyet', m: v.genel.m, z: v.genel.z, vurgu: 1 }
    ];
    $('#pmZamCekTablo tbody').innerHTML = satirlar.map(s => `<tr class="${s.vurgu ? 'toplam' : ''}"><td>${esc(s.ad)}</td><td class="s">${hucre(s.m)}</td><td class="s${s.tl ? ' tl' : ''}">${hucre(s.z)}</td><td class="s">${artis(s.z, s.m)}</td><td class="s">${yuzde(s.z, s.m)}</td></tr>`).join('');
  }
  /** kişi kartı: satıra tıklanınca açılır; tüm değerler (mevcut, zamlı, artış, %) ve Net Zamlı X-Net tutarı (₺) burada */
  function zamCekmeceAc(k) {
    const r = zamSon.find(x => zamAnahtar(x.p) === k); if (!r) return;
    zamCekK = k;
    const o = zamAyar.kisi[k] || {};
    $('#pmZamCekBaslik').textContent = r.p.adSoyad;
    $('#pmZamCekAlt').textContent = `${kaynakAd(r.p)} · SGK ${r.p.sgkIsvereni || '—'} · Fiili görev yeri ${r.p.fiiliGorevYeri || '—'}`;
    $('#pmZamCekZam').textContent = `Uygulanan zam: R % ${ondalik(r.oran.r, 1)} · X % ${ondalik(r.oran.x, 1)}${o.r !== undefined || o.x !== undefined ? ' (kişi bazlı)' : ' (toplu)'}`;
    $('#pmZamCekTutar').value = o.xTl !== undefined ? o.xTl : '';
    zamCekOnizle();
    $('#pmZamCekmece').classList.add('acik'); $('#pmZamPerde').classList.add('acik');
    $('#pmZamCekTutar').focus();
  }
  /** kart, girilen X-Net tutarıyla yeniden çizilir (kaydetmeden önizleme); boşsa yüzde */
  function zamCekOnizle() {
    const r = zamSon.find(x => zamAnahtar(x.p) === zamCekK); if (!r) return;
    const t = $('#pmZamCekTutar').value, tl = t === '' ? undefined : zamSayi(t);
    zamKartCiz(r, tl);
    $('#pmZamCekNot').textContent = tl === undefined ? `Tutar boş: yüzde kullanılıyor (X-Net ${kisa(zamKartVeri(r).netZ.x)}).` : 'Girilen tutar kullanılıyor; kaydedince satırdaki değerler güncellenir.';
  }
  /** girilen tutar (ya da boşsa yüzdeye dönüş) kişinin kaydına yazılır; satır ve kart yeniden hesaplanır, kart açık kalır */
  function zamCekYaz(tl) {
    const k = zamCekK; if (k === null) return;
    const o = zamAyar.kisi[k] || {};
    if (tl === undefined) delete o.xTl; else o.xTl = tl;
    if (Object.keys(o).length) zamAyar.kisi[k] = o; else delete zamAyar.kisi[k];
    zamAyarKaydet();
    zamCekmeceAc(k);
    $('#pmZamCekNot').textContent = tl === undefined ? 'Yüzdeye dönüldü; satırdaki değerler yüzdeye göre hesaplandı.' : 'Kaydedildi: satırdaki tüm değerler bu tutara göre yeniden hesaplandı.';
  }
  function zamCekKapat() { zamCekK = null; $('#pmZamCekmece').classList.remove('acik'); $('#pmZamPerde').classList.remove('acik'); }
  /** Excel satırı: personel satırı (r) ya da toplam (r boş; n kişi sayısı); tutarlar ₺ biçimli, oranlar Türkçe ondalıklı sayı */
  function zamXlsSatir(r, v, n) {
    return ZAM_SUTUN.flatMap(s => {
      if (s.k === 'ad') return [r ? r.p.adSoyad : 'Toplam'];
      if (s.k === 'kaynak') return [r ? kaynakAd(r.p) : n + ' kişi'];
      if (s.k === 'oran') return r ? [r.p.uSira ? '' : ondalik(r.oran.r, 1), ondalik(r.oran.x, 1)] : ['', ''];
      if (s.k === 'rxt') return [kisa(v[s.f].r), kisa(v[s.f].x), kisa(v[s.f].t)];
      if (s.k === 'netArt') return [kisa(v.netArt), v.net.t && kurus(v.netArt) ? zamYuzde(v.netArt, v.net.t) : '—'];
      if (s.k === 'ek') return [kisa(v.ek.r), kisa(v.ek.x)];
      if (s.k === 'iki') return [kisa(v[s.f].m), kisa(v[s.f].z)];
      return [kisa(v.genelArt), kurus(v.genelArt) ? zamYuzde(v.genelArt, v.genel.m) : '—'];
    });
  }
  /** Excel: görünen sütunlar; tutarlar sayı olarak (Net R / X / Toplam, mevcut ve zamlı ayrı); satır sırası ekrandaki sıralama; Zam Özeti ayrı sayfa */
  function zamExcel(bicim = 'xlsx') {
    const satirlar = Array.from(document.querySelectorAll('#pmZamTablo tbody tr')).filter(tr => tr._zr);
    const S = satirlar.map(tr => tr._zr), H = satirlar.map(tr => tr._zv), T = zamTopla(H);
    const kol = s => s.k === 'rxt' ? [s.ad + ' R', s.ad + ' X', s.ad + ' Toplam'] : s.k === 'netArt' ? ['Net Artışı', 'Net Artışı %'] : s.k === 'iki' ? [s.ad + ' Mevcut', s.ad + ' Zamlı'] : s.k === 'ek' ? ['Ek Ödemeler Toplam-R', 'Ek Ödemeler Toplam-X'] : s.k === 'oran' ? ['R Zam %', 'X Zam %'] : s.k === 'artis' ? ['Artış', 'Artış %'] : [s.ad];
    const sayisal = ZAM_SUTUN.flatMap(s => kol(s).map(() => (s.k === 'ad' || s.k === 'kaynak' ? 0 : 1)));
    const tb = document.createElement('table');
    tb.innerHTML = `<thead><tr>${ZAM_SUTUN.flatMap(kol).map((h, j) => `<th class="${sayisal[j] ? 's' : ''}">${esc(h)}</th>`).join('')}</tr></thead>`
      + `<tbody>${S.map((r, i) => `<tr>${zamXlsSatir(r, H[i]).map((x, j) => `<td class="${sayisal[j] ? 's' : ''}">${esc(x)}</td>`).join('')}</tr>`).join('')}</tbody>`
      + `<tfoot><tr>${zamXlsSatir(null, T, S.length).map((x, j) => `<td class="${sayisal[j] ? 's' : ''}">${esc(x)}</td>`).join('')}</tr></tfoot>`;
    const ozet = document.createElement('table');
    ozet.innerHTML = `<thead><tr><th>Gösterge</th><th>Tutar</th><th>Ayrıntı</th></tr></thead><tbody>${zamOzetSatirlari(S, zamAyarOku(), T).map(s => `<tr><td>${esc(s.ad)}</td><td>${esc(zamOzetMetin(s.tutar))}</td><td>${esc(s.ayrinti)}</td></tr>`).join('')}</tbody>`;
    tabloExcel('zam_senaryosu', [{ tablo: tb, ad: 'Zam Senaryosu', baslik: `Konsolide İK Paneli · Zam Senaryosu · ${V.donemAd || ''}`, altBaslik: $('#pmZamSayi').textContent.trim(), not: '.na-panel[data-p="zam"] .pm-not' },
      { tablo: '#pmZamKaynak', ad: 'Kaynak Bazında', baslik: 'Konsolide İK Paneli · Zam Senaryosu · Kaynak Bazında' },
      { tablo: ozet, ad: 'Zam Özeti', baslik: 'Konsolide İK Paneli · Zam Senaryosu · Özet' }], bicim);
  }

  // ---- kayıtlı senaryolar (bu tarayıcıda) ve Senaryo Karşılaştırması (?sekme=karsilastirma)
  let zamSenaryo = []; try { zamSenaryo = JSON.parse(localStorage.pmZamSenaryo || '[]') || []; } catch (e) { zamSenaryo = []; }
  const zamSenaryoSakla = () => { try { localStorage.pmZamSenaryo = JSON.stringify(zamSenaryo); } catch (e) { /* yoksay */ } };
  const tarihAd = iso => new Date(iso).toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' });
  /** mevcut zam ayarını ad ile kaydeder; aynı adlı senaryo varsa onay ister ve üzerine yazar */
  function zamKaydet() {
    const ad = ($('#pmZamAd').value || '').trim().slice(0, 60);
    if (!ad) { mesaj('warning', 'Önce senaryo adını yazın.'); $('#pmZamAd').focus(); return; }
    const kucuk = ad.toLocaleLowerCase('tr-TR'), onceki = zamSenaryo.findIndex(s => s.ad.toLocaleLowerCase('tr-TR') === kucuk);
    if (onceki >= 0 && !window.confirm(`"${ad}" adlı senaryo zaten kayıtlı. Üzerine yazılsın mı?`)) return;
    const a = zamAyarOku(), kayit = { id: onceki >= 0 ? zamSenaryo[onceki].id : Date.now(), ad, tarih: new Date().toISOString(), r: a.r, x: a.x, ayni: a.ayni, kisi: JSON.parse(JSON.stringify(a.kisi)), gizle: a.gizle };
    if (onceki >= 0) zamSenaryo[onceki] = kayit; else zamSenaryo.push(kayit);
    zamSenaryoSakla(); $('#pmZamAd').value = '';
    mesaj('success', `Senaryo kaydedildi: ${esc(ad)}. Senaryo Karşılaştırması sayfasında yan yana görünür.`);
  }
  /** kayıtlı senaryoyu zam ayarına yükler; açılışta bu tarayıcının kayıtlı ayarı değişmez, düzenlenirse yeniden kaydedilir */
  function zamSenaryoYukle(s) {
    zamAyar = { r: zamSayi(s.r), x: zamSayi(s.x), ayni: s.ayni !== false, kisi: JSON.parse(JSON.stringify(s.kisi || {})), dovizGizle: s.dovizGizle !== false };
    if (zamAyar.ayni) zamAyar.x = zamAyar.r;
    zamAcik = s.ad; $('#pmZamAd').value = s.ad;
    gizle.uretim = !!(s.gizle && s.gizle.uretim); gizle.ozel = !!(s.gizle && s.gizle.ozel);
    $$('.pm-gizle').forEach(c => { c.checked = gizle[c.dataset.g]; });
  }
  /** toplu oran alanlarını ve "R ve X aynı oran" kutusunu zam ayarıyla eşitler */
  function zamUiYaz() { $('#pmZamOranR').value = zamAyar.r; $('#pmZamOranX').value = zamAyar.x; $('#pmZamAyni').checked = zamAyar.ayni; $('#pmZamOranX').disabled = zamAyar.ayni; $('#pmZamDoviz').checked = zamAyar.dovizGizle !== false; }
  /** kayıtlı senaryo yeni sekmede açılır (Zam Senaryosu, o senaryonun ayarıyla) */
  function zamSenaryoAc(id) {
    const u = new URL(location.href); u.searchParams.set('sekme', 'zam'); u.searchParams.set('senaryo', String(id));
    window.open(u.toString(), '_blank');
  }
  function zamSenaryoSil(id) {
    const s = zamSenaryo.find(x => String(x.id) === String(id));
    if (!s || !window.confirm(`"${s.ad}" senaryosu silinsin mi?`)) return;
    zamSenaryo = zamSenaryo.filter(x => x !== s); zamSenaryoSakla(); zamKarsCiz();
  }
  /** senaryonun toplamları: kişi sayısı, kişi bazlı, aylık genel maliyet (mevcut / zamlı), artışlar */
  function zamOzet(a) {
    const S = zamSatirlari(a), T = zamTopla(S.map(zamDegerler));
    return { n: S.length, kisi: S.filter(r => r.kisi).length, gM: T.genel.m, gZ: T.genel.z, gA: T.genelArt, netA: T.netArt, brutA: T.brut.z - T.brut.m, isvA: T.isv.z - T.isv.m };
  }
  /** karşılaştırma tablosunun sütunları; sayı sütunları (s) sağa yaslı */
  const SEN_SUTUN = [{ ad: 'Senaryo' }, { ad: 'Kayıt Tarihi' }, { ad: 'R Zam %', s: 1 }, { ad: 'X Zam %', s: 1 }, { ad: 'Kişi Bazlı', s: 1 }, { ad: 'Kişi', s: 1 },
    { ad: 'Mevcut Aylık Genel Maliyet', s: 1 }, { ad: 'Zamlı Aylık Genel Maliyet', s: 1 }, { ad: 'Aylık Artış', s: 1 }, { ad: 'Artış %', s: 1 }, { ad: 'Yıllık Artış', s: 1 },
    { ad: 'Net Artışı', s: 1 }, { ad: 'Brüt Masraf Artışı', s: 1 }, { ad: 'SGK İşveren Artışı', s: 1 }];
  /** karşılaştırma satırının değerleri (metin, SEN_SUTUN sırasıyla) */
  const senDegerler = (s, o) => [s.ad, s.tarih ? tarihAd(s.tarih) : '—', ondalik(s.r, 1), ondalik(s.x, 1), String(o.kisi), String(o.n), kisa(o.gM), kisa(o.gZ), kisa(o.gA), kurus(o.gA) ? zamYuzde(o.gA, o.gM) : '—', kisa(o.gA * 12), kisa(o.netA), kisa(o.brutA), kisa(o.isvA)];
  function senSatirHtml(s, o) {
    const d = senDegerler(s, o), id = esc(String(s.id));
    const tarih = s.tarih ? `<td class="kucuk" data-sort="${new Date(s.tarih).getTime()}">${esc(d[1])}</td>` : '<td class="kucuk">—</td>';
    const islem = s.id === 'mevcut' ? '' : `<button type="button" class="btn btn-light btn-sm py-0 senAc" data-sen="${id}" title="Senaryoyu yeni sekmede aç"><i class="fas fa-up-right-from-square me-1"></i>Aç</button> <button type="button" class="btn btn-light btn-sm py-0 senSil" data-sen="${id}" title="Senaryoyu sil"><i class="fas fa-trash"></i></button>`;
    return `<tr><td class="ad">${esc(d[0])}</td>${tarih}${d.slice(2).map(x => `<td class="s">${esc(x)}</td>`).join('')}<td>${islem}</td></tr>`;
  }
  /** Senaryo Karşılaştırması: ilk satır zamsız mevcut durum (kendi gizleme ayarıyla); diğer satırlar kayıtlı senaryolar; artışlar mevcut duruma göre */
  function zamKarsCiz() {
    const gecerli = { uretim: gizle.uretim, ozel: gizle.ozel };
    const satirlar = [{ id: 'mevcut', ad: 'Mevcut (zamsız)', r: 0, x: 0, kisi: {}, gizle: gecerli }, ...zamSenaryo].map(s => ({ s, o: zamOzet({ r: zamSayi(s.r), x: zamSayi(s.x), ayni: false, kisi: s.kisi || {}, gizle: s.gizle || gecerli }) }));
    $('#pmSenBaslik').textContent = `${V.donemAd || ''} Zam Senaryoları Karşılaştırma`;
    const govde = $('#pmSenTablo tbody');
    $('#pmSenTablo thead').innerHTML = `<tr class="tek">${SEN_SUTUN.map(c => `<th class="${c.s ? 's' : ''}">${esc(c.ad)}</th>`).join('')}<th></th></tr>`;
    govde.innerHTML = satirlar.map(({ s, o }) => senSatirHtml(s, o)).join('') || `<tr><td colspan="${SEN_SUTUN.length + 1}" class="na-bos">Kayıtlı senaryo yok. Zam Senaryosu'nda ayarlayıp "Senaryoyu Kaydet" ile saklayın.</td></tr>`;
    Array.from(govde.rows).forEach((tr, i) => { if (satirlar[i]) tr._sen = satirlar[i]; });   // Excel: satır verisi (sıralama sonrası sıra için)
    $('#pmSenSayi').textContent = `${zamSenaryo.length} kayıtlı senaryo · tarayıcıda saklanır · sonuçlar güncel veriye göre hesaplanır`;
    sabitleHepsi();
  }
  /** Karşılaştırma Excel'i: görünen satırlar ve sütunlar; sayı sütunları sayı olarak */
  function zamKarsExcel(bicim = 'xlsx') {
    const satirlar = Array.from(document.querySelectorAll('#pmSenTablo tbody tr')).filter(tr => tr._sen).map(tr => tr._sen);
    const tb = document.createElement('table');
    tb.innerHTML = `<thead><tr>${SEN_SUTUN.map(c => `<th class="${c.s ? 's' : ''}">${esc(c.ad)}</th>`).join('')}</tr></thead>`
      + `<tbody>${satirlar.map(({ s, o }) => `<tr>${senDegerler(s, o).map((x, j) => `<td class="${SEN_SUTUN[j].s ? 's' : ''}">${esc(x)}</td>`).join('')}</tr>`).join('')}</tbody>`;
    tabloExcel('zam_karsilastirma', [{ tablo: tb, ad: 'Zam Karşılaştırma', baslik: `Konsolide İK Paneli · Zam Senaryoları Karşılaştırma · ${V.donemAd || ''}`, altBaslik: $('#pmSenSayi').textContent.trim(), not: '.na-panel[data-p="karsilastirma"] .pm-not' }], bicim);
  }

  /** birleşik tablo satırı: bordro / özel kişi → personel kartı; üretim → üretim düzenleme */
  function birlesimSatirAc(tr) {
    const kaynak = tr.dataset.kaynak; if (kaynak === 'Üretim') { uretimDuzenle(+tr.dataset.usira); return; }
    const p = bul(+tr.dataset.sira0); if (!p) return; if (!!p.ozel !== (kapsam === 'ozel')) kapsam = p.ozel ? 'ozel' : 'bordro'; kartAc(p.sira);
  }

  /** ekrandaki tabloları göründüğü gibi Excel'e: her tablo ayrı sayfa; ilk sayfada sekmenin KPI kutuları, sayacı ve açıklaması */
  function domExcel(ad, tablolar, bicim = 'xlsx') {
    const panel = $('.na-panel.aktif'), p = panel ? panel.dataset.p : '', bas = panelBaslik(p), mini = panel ? panel.querySelector('.na-mini') : null;
    tabloExcel(ad, tablolar.filter(([id]) => $('#' + id)).map(([id, sayfa], i) => ({ tablo: '#' + id, ad: sayfa, baslik: `Konsolide İK Paneli · ${bas || sayfa}${bas ? ' · ' + sayfa : ''}`, altBaslik: i === 0 && mini ? mini.textContent.trim() : '', kpi: i === 0 && p ? $(`[data-kpi="${p}"]`) : null, not: i === 0 && panel ? panel.querySelector(':scope > .pm-not') : null })), bicim);
  }

  // ============================================================
  // ÖZEL RAPOR (6 Kas 2026, "yönetim genel maliyet 80.000 ₺ ve üzeri kişileri listele diyor … sütunları biz belirleyelim"): sütun seçimi + koşullar (hepsi birlikte sağlanır) ile anlık liste.
  //   Kapsam tumListe: bordrolu + özel + SGK'sız üretim (ana filtreler, genel arama ve gizleme anahtarları geçerli); hücreler ana tabloyla aynı (hucreHtml), dip toplam;
  //   kayıtlı raporlar ve son ayar tarayıcıda (localStorage.pmOzelRaporlar / pmOzelRaporAyar). Veri değişmez; her çizimde güncel V'den kurulur (senkron).
  // ============================================================
  const OR_SABIT = ['sira', 'adSoyad'];
  const OR_VARSAYILAN = ['sgkIsvereni', 'fiiliGorevYeri', 'gorev', 'sgkDurumu', 'rNet', 'xNet', 'toplamNet', 'toplamBrutMasraflar', 'toplamIsveren', 'genelMaliyet'];
  /** rapora özgü ek alanlar (ana tabloda sütunu yok; 6 Kas 2026 "SGK Durumu Var / Yok seçebilelim"): SGK Durumu — Var: bordrolu (SGK'lı gündelikçi dahil), Yok: SGK detayı olmayan kişi (sgkDetayYok) ve SGK'sız gündelikçi; Kaynak — Bordro / Özel / Üretim */
  const OR_EK = [{ ad: 'SGK Durumu', anahtar: 'sgkDurumu', tip: 'metin', secenek: ['Var', 'Yok'] }, { ad: 'Kaynak', anahtar: 'kaynak', tip: 'metin', secenek: ['Bordro', 'Özel', 'Üretim'] }];
  const orEkAlan = p => ({ ...p, kaynak: p.kaynak || (p.ozel ? 'Özel' : 'Bordro'), sgkDurumu: p.kaynak === 'Üretim' || p.sgkDetayYok ? 'Yok' : 'Var' });
  const orEtiket = k => ETIKET[k] || (OR_EK.find(e => e.anahtar === k) || {}).ad || k;
  /** koşul değeri listeden seçilen alanlar: ek alanlar, özel durum, durum ve kimlik listeleri (V'deki ayrık değerler); diğerleri serbest metin / sayı */
  const orSecenek = k => { const e = OR_EK.find(x => x.anahtar === k); if (e) return e.secenek; const l = { ozelDurum: V.ozelDurumlar, durum: V.durumlar, sgkIsvereni: V.sgkIsverenleri, fiiliGorevYeri: V.gorevYerleri, catiDepartman: V.catiDepartmanlar, bolum: V.bolumler, unvan: V.unvanlar, gorev: V.gorevler }[k]; return Array.isArray(l) && l.length ? l : null; };
  const OR_OP = [['>=', '≥'], ['>', '>'], ['=', '='], ['!=', '≠'], ['<', '<'], ['<=', '≤'], ['icerir', 'içerir'], ['icermez', 'içermez'], ['bos', 'boş'], ['dolu', 'dolu']];
  let orAyar = { sutunlar: OR_VARSAYILAN.slice(), kosullar: [{ a: 'genelMaliyet', o: '>=', v: '' }], ad: '', sgk: '' };   // sgk: şeritteki SGK Durumu seçimi ('' Hepsi | Var | Yok), koşullardan bağımsız
  try { const k = JSON.parse(localStorage.pmOzelRaporAyar || 'null'); if (k && Array.isArray(k.sutunlar)) orAyar = { sutunlar: k.sutunlar, kosullar: Array.isArray(k.kosullar) ? k.kosullar : [], ad: k.ad || '', sgk: k.sgk || '' }; } catch (e) { /* yoksay */ }
  let orKayitli = []; try { orKayitli = JSON.parse(localStorage.pmOzelRaporlar || '[]') || []; } catch (e) { /* yoksay */ }
  const orSakla = () => { try { localStorage.pmOzelRaporAyar = JSON.stringify(orAyar); localStorage.pmOzelRaporlar = JSON.stringify(orKayitli); } catch (e) { /* yoksay */ } };
  /** koşul değeri: 80.000 · 80000 · 60.000,50 · 12.5 (virgül varsa Türkçe biçim; yalnız binlik noktalar varsa atılır) */
  const orSayi = v => { const t = String(v ?? '').replace(/[₺%\s]/g, ''); if (!t) return NaN; const n = Number(t.includes(',') ? t.replace(/\./g, '').replace(',', '.') : (/^-?\d{1,3}(\.\d{3})+$/.test(t) ? t.replace(/\./g, '') : t)); return isNaN(n) ? NaN : n; };
  const orSutunlar = () => SUTUN.filter(s => s.tip !== 'kart').concat(OR_EK);
  const orGrup = k => { if (OR_EK.some(e => e.anahtar === k)) return 'Ek Alanlar'; const g = (V.sutunlar || []).find(x => x.sutunlar.some(s => s.anahtar === k)); return g ? g.grup : ''; };
  /** koşul alanları: tüm sütunlar (Sıra ve Kart hariç); R / X sütununun R ve X değeri ayrı alan */
  const orAlanlar = () => { const l = []; orSutunlar().forEach(s => { if (s.tip === 'sira') return; l.push({ k: s.anahtar, ad: s.ad + (s.tip === 'rx' ? ' (R)' : ''), sayisal: ekSayisal(s) }); if (s.tip === 'rx') l.push({ k: s.anahtar + 'X', ad: s.ad + ' (X)', sayisal: true }); }); return l; };
  const orSayisalMi = k => { const a = orAlanlar().find(x => x.k === k); return !!a && a.sayisal; };
  function orKosulUyar(p, c) {
    if (!c || !c.a || !c.o) return true;
    const sayisal = orSayisalMi(c.a), ham = p[c.a];
    if (c.o === 'bos') return sayisal ? !kurus(ham) : !String(ham ?? '').trim();
    if (c.o === 'dolu') return sayisal ? !!kurus(ham) : !!String(ham ?? '').trim();
    if (c.v === undefined || c.v === null || String(c.v).trim() === '') return true;   // değeri girilmemiş koşul süzmez
    if (sayisal) {
      const x = Number(ham) || 0, y = orSayi(c.v); if (isNaN(y)) return true;
      switch (c.o) { case '>=': return x >= y; case '>': return x > y; case '=': return Math.abs(x - y) < 0.005; case '!=': return Math.abs(x - y) >= 0.005; case '<': return x < y; case '<=': return x <= y; case 'icerir': return String(x).includes(String(c.v)); case 'icermez': return !String(x).includes(String(c.v)); default: return true; }
    }
    const s = String(ham ?? '').toLocaleLowerCase('tr-TR').trim(), v = String(c.v).toLocaleLowerCase('tr-TR').trim(), f = s.localeCompare(v, 'tr');
    switch (c.o) { case '=': return s === v; case '!=': return s !== v; case 'icerir': return s.includes(v); case 'icermez': return !s.includes(v); case '>=': return f >= 0; case '>': return f > 0; case '<': return f < 0; case '<=': return f <= 0; default: return true; }
  }
  const orKosulMetni = () => orAyar.kosullar.filter(c => c.a && c.o && (c.o === 'bos' || c.o === 'dolu' || String(c.v ?? '').trim() !== '')).map(c => `${orEtiket(c.a)} ${(OR_OP.find(o => o[0] === c.o) || [c.o, c.o])[1]}${c.o === 'bos' || c.o === 'dolu' ? '' : ' ' + c.v}`).join(' · ');
  function orSutunCiz() {
    const sec = new Set(orAyar.sutunlar), yonga = s => `<label class="pm-or-sutun ${sec.has(s.anahtar) ? 'secili' : ''}"><input type="checkbox" value="${esc(s.anahtar)}" ${sec.has(s.anahtar) ? 'checked' : ''}> ${esc(s.ad)}</label>`;
    $('#pmOrSutunlar').innerHTML = (V.sutunlar || []).map(g => { const l = g.sutunlar.filter(s => s.tip !== 'kart' && !OR_SABIT.includes(s.anahtar)); return l.length ? `<div class="pm-or-grup"><b>${esc(g.grup)}</b>${l.map(yonga).join('')}</div>` : ''; }).join('')
      + `<div class="pm-or-grup"><b>Ek Alanlar</b>${OR_EK.map(yonga).join('')}</div>`;
  }
  function orKosulCiz() {
    const alanlar = orAlanlar();
    $('#pmOrKosullar').innerHTML = orAyar.kosullar.map((c, i) => {
      const sec = orSecenek(c.a);   // listeli alanda değer açılır kutudan seçilir (SGK Durumu Var / Yok, Kaynak, Özel Durumu, Durum, işveren, görev yeri, çatı, bölüm, unvan, görev)
      const deger = sec ? `<select class="form-select orV" aria-label="Değer"><option value="">Hepsi</option>${sec.map(v => `<option value="${esc(v)}" ${String(c.v ?? '') === String(v) ? 'selected' : ''}>${esc(v)}</option>`).join('')}</select>` : `<input class="form-control orV" placeholder="değer (ör. 80.000)" value="${esc(c.v ?? '')}" aria-label="Değer">`;
      return `<div class="pm-or-kosul" data-i="${i}"><select class="form-select orA" aria-label="Alan">${alanlar.map(a => `<option value="${esc(a.k)}" ${a.k === c.a ? 'selected' : ''}>${esc(a.ad)}</option>`).join('')}</select><select class="form-select orO" aria-label="Karşılaştırma">${OR_OP.map(o => `<option value="${o[0]}" ${o[0] === c.o ? 'selected' : ''}>${o[1]}</option>`).join('')}</select>${deger}<button type="button" class="btn btn-light btn-sm py-0 orSil" title="Koşulu kaldır"><i class="fas fa-xmark"></i></button></div>`;
    }).join('') || '<div class="text-muted" style="font-size:.7rem">Koşul yok: tüm kişiler listelenir.</div>';
  }
  function ozelRaporCiz() {
    $('#pmOrBaslik').textContent = `${V.donemAd || ''} Özel Rapor${orAyar.ad ? ' · ' + orAyar.ad : ''}`;
    const S = orSutunlar().filter(s => OR_SABIT.includes(s.anahtar) || orAyar.sutunlar.includes(s.anahtar));
    const tum = tumListe(false).map(etkin).map(orEkAlan), L2 = tum.filter(p => (!orAyar.sgk || p.sgkDurumu === orAyar.sgk) && orAyar.kosullar.every(c => orKosulUyar(p, c)));   // şerit SGK seçimi + koşullar
    const t = $('#pmOrTablo'), dz = duzenleme; duzenleme = false;   // hücreler ana tabloyla aynı biçimde; düzenleme modunda bile salt okunur
    try {
      $('thead tr', t).innerHTML = S.map(s => `<th class="${ekSayisal(s) ? 's' : ''}" data-k="${s.anahtar}" title="${esc(orGrup(s.anahtar))}">${esc(s.ad)}</th>`).join('');
      $('tbody', t).innerHTML = L2.map((p, i) => `<tr data-sira="${p.sira}"${p.uSira ? ` data-usira="${p.uSira}"` : ''}>${S.map(s => hucreHtml(p, s, false, i + 1)).join('')}</tr>`).join('') || `<tr><td colspan="${S.length}" class="na-bos">Koşullara uyan kişi yok.</td></tr>`;
    } finally { duzenleme = dz; }
    const top = k => toplam(L2, k), sayilmaz = ['oran', 'kur', 'dovizTutar'];
    $('tfoot tr', t).innerHTML = S.map(s => { const dk = `data-k="${s.anahtar}"`; if (s.tip === 'sira') return `<td ${dk}>Toplam</td>`; if (s.tip === 'ad') return `<td ${dk}>${L2.length} Kişi</td>`; if (s.tip === 'rx') return `<td class="s rx" ${dk}>${rxHucre(top(s.anahtar), top(s.anahtar + 'X'), kisa, true)}</td>`; if (s.tip === 'gun') return `<td class="s" ${dk}>${sayilmaz.includes(s.anahtar) ? '' : gunFmt(top(s.anahtar))}</td>`; if (s.tip === 'sayi' || s.tip === 'hesap') return `<td class="s" ${dk}>${sayilmaz.includes(s.anahtar) ? '' : kisa(top(s.anahtar))}</td>`; if (s.tip === 'genel') return `<td class="s genel" ${dk}>${kisa(top(s.anahtar))}</td>`; return `<td ${dk}></td>`; }).join('');
    const km = orKosulMetni();
    $('#pmOrSayi').textContent = `${L2.length} / ${tum.length} kişi · ${S.length} sütun · SGK: ${orAyar.sgk || 'Hepsi'} · ${km ? 'koşul: ' + km : 'koşul yok'} · ${V.donemAd || ''} · aylık tutarlar ₺`;
    orKayitliDoldur(); orSakla(); sabitleHepsi();
  }
  function orKayitliDoldur() { const sel = $('#pmOrKayitli'), v = sel.value; sel.innerHTML = '<option value="">Kayıtlı Raporlar</option>' + orKayitli.map((r, i) => `<option value="${i}">${esc(r.ad)}</option>`).join(''); sel.value = [...sel.options].some(o => o.value === v) ? v : ''; }
  function orKaydet() {
    const ad = ($('#pmOrAd').value || '').trim(); if (!ad) { mesaj('warning', 'Rapor adı girin.'); $('#pmOrAd').focus(); return; }
    const r = { ad, sutunlar: orAyar.sutunlar.slice(), kosullar: orAyar.kosullar.map(c => ({ ...c })), sgk: orAyar.sgk || '' }, i = orKayitli.findIndex(x => x.ad.toLocaleLowerCase('tr-TR') === ad.toLocaleLowerCase('tr-TR'));
    if (i >= 0) orKayitli[i] = r; else orKayitli.push(r);
    orAyar.ad = ad; orSakla(); orKayitliDoldur(); $('#pmOrKayitli').value = String(i >= 0 ? i : orKayitli.length - 1); ozelRaporCiz(); mesaj('success', `"${esc(ad)}" raporu kaydedildi (bu tarayıcıda).`);
  }
  function orYukle(i) { const r = orKayitli[i]; if (!r) return; orAyar = { sutunlar: r.sutunlar.slice(), kosullar: r.kosullar.map(c => ({ ...c })), ad: r.ad, sgk: r.sgk || '' }; $('#pmOrAd').value = r.ad; $('#pmOrSgk').value = orAyar.sgk; orSutunCiz(); orKosulCiz(); ozelRaporCiz(); }
  function orSil() { const sel = $('#pmOrKayitli'), i = +sel.value; if (sel.value === '' || !orKayitli[i]) { mesaj('info', 'Silinecek kayıtlı raporu seçin.'); return; } const ad = orKayitli[i].ad; orKayitli.splice(i, 1); if (orAyar.ad === ad) orAyar.ad = ''; orSakla(); sel.value = ''; orKayitliDoldur(); ozelRaporCiz(); mesaj('success', `"${esc(ad)}" silindi.`); }

  // ============================================================
  // SIRALAMA — sütun başlığına tıklayınca artan (küçükten büyüğe, A → Z), tekrar tıklayınca azalan (Z → A), üçüncüde varsayılan sıra; Konsolide İK Paneli'nin tüm tablolarında
  //   Hücre anahtarı: girdi değeri; R üstte / X altta hücrenin toplamı; mevcut / zamlı hücrede zamlı değer; tarih (gg.aa.yyyy) kronolojik; Türkçe biçimli sayı (1.234,5 ₺, % 9,8); boş hücre her yönde sona.
  //   Yalnız satır düğümleri yeniden dizilir (veri değişmez). Sekme yeniden çizilince sütun ve yön korunur; tablo kimliği: id ya da sekme ve tablo sırası.
  // ============================================================
  const SIRALAMA = {};
  const srtBaslik = th => th.dataset.k || th.textContent.replace(/\s+/g, ' ').trim();
  const srtKimlik = t => { const pn = t.closest('.na-panel'); return t.id || (pn ? pn.dataset.p + '#' + Array.from(pn.querySelectorAll('table')).indexOf(t) : ''); };
  const srtSayi = s => { const t = String(s).replace(/[₺%+\s ]/g, '').replace(/\./g, '').replace(',', '.'); return /^-?\d+(\.\d+)?$/.test(t) ? Number(t) : null; };
  const srtIlkSayi = s => { const m = /-?\d[\d.]*(,\d+)?/.exec(String(s)); return m ? Number(m[0].replace(/\./g, '').replace(',', '.')) : 0; };
  /** hücrenin sıralama anahtarı: sayı, metin ya da null (boş) */
  function srtAnahtar(td) {
    if (td.dataset.sort !== undefined) { const v = td.dataset.sort, n = Number(v); return v !== '' && !isNaN(n) ? n : v; }
    const girdi = td.querySelector('input, select');
    if (girdi) { const v = girdi.tagName === 'SELECT' ? (girdi.options[girdi.selectedIndex] ? girdi.options[girdi.selectedIndex].textContent.trim() : '') : girdi.value.trim(); const n = Number(v); return v !== '' && !isNaN(n) ? n : (v || null); }
    if (td.querySelector('.rx')) return Array.from(td.querySelectorAll('.rx > span')).reduce((s, sp) => s + srtIlkSayi(sp.textContent), 0);
    if (td.querySelector('.zi')) { const b = td.querySelector('.zi b'); return srtIlkSayi(b ? b.textContent : td.textContent); }
    const t = td.textContent.replace(/\s+/g, ' ').trim();
    if (t === '' || t === '—') return null;
    const tr = /^(\d{2})\.(\d{2})\.(\d{4})$/.exec(t); if (tr) return Number(tr[3] + tr[2] + tr[1]);
    const n = srtSayi(t); return n !== null ? n : t.toLocaleLowerCase('tr-TR');
  }
  /** başlıktaki tireli sözcükler (Toplam-R, R-Net) tek parça kalsın diye kırılmaz bir sarmalayıcıya alınır; metin değişmez */
  const tireBagla = th => Array.from(th.childNodes).forEach(n => { if (n.nodeType !== 3 || !/\S-\S/.test(n.nodeValue)) return; const f = document.createDocumentFragment(); n.nodeValue.split(/(\S*\S-\S\S*)/).forEach(p => { if (!p) return; if (/\S-\S/.test(p)) { const s = document.createElement('span'); s.className = 'nw'; s.textContent = p; f.appendChild(s); } else f.appendChild(document.createTextNode(p)); }); n.replaceWith(f); });
  /** tablo satırlarını seçili sütuna göre dizer; toplam ve boş satırlar sona kalır; yalnız sıra değişir */
  function tabloSirala(t) {
    const tb = t.tBodies[0]; if (!tb || !t.closest('.na-panel')) return;
    const kimlik = srtKimlik(t), st = SIRALAMA[kimlik], bas = t.tHead && t.tHead.rows.length ? t.tHead.rows[t.tHead.rows.length - 1] : null;
    let i = -1;
    if (bas) Array.from(bas.cells).forEach((th, j) => { th.classList.add('srt'); th.classList.remove('srt-asc', 'srt-desc'); tireBagla(th); if (i < 0 && st && srtBaslik(th) === st.k) i = j; });
    if (bas && i >= 0) bas.cells[i].classList.add(st.dir > 0 ? 'srt-asc' : 'srt-desc');
    const satirlar = Array.from(tb.rows);
    if (satirlar.some(r => r._ord === undefined)) satirlar.forEach((r, k) => { r._ord = k; });
    const veri = satirlar.filter(r => !r.classList.contains('toplam') && r.cells.length > 1 && !r.querySelector('.na-bos'));
    const diger = satirlar.filter(r => veri.indexOf(r) < 0);
    const anahtar = new Map(); if (i >= 0) veri.forEach(r => anahtar.set(r, srtAnahtar(r.cells[i])));
    const sirali = veri.slice().sort((a, b) => {
      if (i < 0) return a._ord - b._ord;
      const x = anahtar.get(a), y = anahtar.get(b);
      if (x === null || y === null) return x === y ? a._ord - b._ord : (x === null ? 1 : -1);
      const c = typeof x === 'number' && typeof y === 'number' ? x - y : String(x).localeCompare(String(y), 'tr', { numeric: true, sensitivity: 'base' });
      return (c * st.dir) || (a._ord - b._ord);
    });
    const yeni = sirali.concat(diger);
    if (yeni.some((r, k) => r !== satirlar[k])) yeni.forEach(r => tb.appendChild(r));
  }
  /** başlık tıklaması: artan → azalan → varsayılan; yalnız tablonun son başlık satırındaki sütunlar */
  kok.addEventListener('click', ev => {
    const th = ev.target.closest('.na-panel thead th'); if (!th) return;
    const t = th.closest('table'), bas = t && t.tHead && t.tHead.rows.length ? t.tHead.rows[t.tHead.rows.length - 1] : null;
    if (!t || th.parentElement !== bas) return;
    const kimlik = srtKimlik(t), k = srtBaslik(th), st = SIRALAMA[kimlik];
    if (!st || st.k !== k) SIRALAMA[kimlik] = { k, dir: 1 }; else if (st.dir > 0) st.dir = -1; else delete SIRALAMA[kimlik];
    tabloSirala(t);
  });
  /** sekme yeniden çizildiğinde (satır düğümleri ve başlıklar değişince) seçili sıralama uygulanır */
  new MutationObserver(kayitlar => {
    const tablolar = new Set();
    kayitlar.forEach(m => { const el = m.target.nodeType === 1 ? m.target : m.target.parentElement; const t = el && el.closest('table'); if (t && t.closest('.na-panel')) tablolar.add(t); });
    tablolar.forEach(t => tabloSirala(t));
  }).observe(kok, { childList: true, subtree: true });

  // ---- başlat
  fetch('/PersonelMaliyet/Veri' + (kok.dataset.donem ? '?donem=' + encodeURIComponent(kok.dataset.donem) : '')).then(r => r.json()).then(v => {
    V = v; sutunlariKur(); listeleriDoldur(); ekKur(); donemKur();
    $('#pmDonem').addEventListener('change', donemDegis);
    $('#pmYeniBordro').addEventListener('click', yeniBordro);
    $('#pmSifirGizle').addEventListener('change', ciz);
    // Üretim Personeli / Özel Kişiler Gizle: tek ortak durum; her anahtar tüm sekmelerde senkron olur ve görünen sekme yeniden çizilir
    $$('.pm-gizle').forEach(c => { c.checked = false; c.addEventListener('change', () => { gizle[c.dataset.g] = c.checked; $$('.pm-gizle').forEach(d => { d.checked = gizle[d.dataset.g]; }); ciz(); if (aktifSekme === 'cari') cariCiz(); else if (aktifSekme === 'tesvik') tesvikCiz(); }); });   // yeni sekmeler kendi listelerini yeniler
    // Zam Senaryosu: toplu R / X oranı (aynı oran kutusu), kişi bazlı R / X ve X-Net tutarı, senaryo kaydı ve karşılaştırma
    $('#pmZamOranR').addEventListener('input', () => { zamAyar.r = zamSayi($('#pmZamOranR').value); if (zamAyar.ayni) { zamAyar.x = zamAyar.r; $('#pmZamOranX').value = zamAyar.r; } zamAyarKaydet(); });
    $('#pmZamOranX').addEventListener('input', () => { zamAyar.x = zamSayi($('#pmZamOranX').value); zamAyarKaydet(); });
    $('#pmZamAyni').addEventListener('change', () => { zamAyar.ayni = $('#pmZamAyni').checked; if (zamAyar.ayni) zamAyar.x = zamAyar.r; zamUiYaz(); zamAyarKaydet(); });
    $('#pmZamTemizle').addEventListener('click', () => { zamAyar.kisi = {}; zamAyarKaydet(); });
    $('#pmZamDoviz').addEventListener('change', () => { zamAyar.dovizGizle = $('#pmZamDoviz').checked; zamAyarKaydet(); });
    $('#pmZamTablo tbody').addEventListener('change', ev => {
      const i = ev.target.closest('input.zam-kisi'); if (!i) return;
      const k = i.dataset.zk, o = zamAyar.kisi[k] || {};
      if (i.value === '') delete o[i.dataset.zt]; else o[i.dataset.zt] = zamSayi(i.value);
      if (Object.keys(o).length) zamAyar.kisi[k] = o; else delete zamAyar.kisi[k];
      zamAyarKaydet();
    });
    $('#pmZamTablo tbody').addEventListener('click', ev => { if (ev.target.closest('input, button, select, a, label, textarea')) return; const tr = ev.target.closest('tr[data-zk]'); if (tr) zamCekmeceAc(tr.dataset.zk); });
    $('#pmZamCekTutar').addEventListener('input', zamCekOnizle);
    $('#pmZamCekTutar').addEventListener('keydown', ev => { if (ev.key === 'Enter') { ev.preventDefault(); $('#pmZamCekKaydet').click(); } });
    $('#pmZamCekKaydet').addEventListener('click', () => { const t = $('#pmZamCekTutar').value; zamCekYaz(t === '' ? undefined : zamSayi(t)); });
    $('#pmZamCekSifirla').addEventListener('click', () => zamCekYaz(undefined));
    $('#pmZamCekKapat').addEventListener('click', zamCekKapat); $('#pmZamPerde').addEventListener('click', zamCekKapat);
    dugmeBagla('#pmZamExcel', '#pmZamPdf', zamExcel);
    $('#pmZamKaydet').addEventListener('click', zamKaydet);
    $('#pmZamAd').addEventListener('keydown', ev => { if (ev.key === 'Enter') { ev.preventDefault(); zamKaydet(); } });
    $('#pmZamKarsilastir').addEventListener('click', () => sekme('karsilastirma'));
    $('#pmSenZam').addEventListener('click', () => sekme('zam'));
    dugmeBagla('#pmSenExcel', '#pmSenPdf', zamKarsExcel);
    $('#pmSenTablo tbody').addEventListener('click', ev => {
      const b = ev.target.closest('button[data-sen]'); if (!b) return;
      if (b.classList.contains('senSil')) zamSenaryoSil(b.dataset.sen); else zamSenaryoAc(b.dataset.sen);
    });
    const yeniSenaryo = zamSenaryo.find(s => String(s.id) === new URLSearchParams(location.search).get('senaryo'));
    if (yeniSenaryo) zamSenaryoYukle(yeniSenaryo);
    zamUiYaz();
    Object.keys(EK).forEach(ad => { const E = EK[ad]; [E.ara, E.sifir].filter(Boolean).forEach(s => { $(s).addEventListener('input', () => ekCiz(ad)); $(s).addEventListener('change', () => ekCiz(ad)); }); });
    const ekFiltre = { '#pmUretimYer': 'uretim', '#pmUretimSgkli': 'uretim', '#pmUretimSgksiz': 'uretim', '#pmPrimDonem': 'prim', '#pmBirlesimKaynak': 'birlesim', '#pmBirlesimTek': 'birlesim' };   // ek sekme filtreleri → sekmeyi yeniden çiz
    Object.keys(ekFiltre).forEach(s => $(s).addEventListener('change', () => ekCiz(ekFiltre[s])));
    // Özel Rapor: sütun seçimi, koşullar, kayıtlı raporlar (tarayıcıda), Excel / PDF, satır → kart
    orSutunCiz(); orKosulCiz(); $('#pmOrAd').value = orAyar.ad || ''; $('#pmOrSgk').value = orAyar.sgk || '';
    $('#pmOrSgk').addEventListener('change', () => { orAyar.sgk = $('#pmOrSgk').value; ozelRaporCiz(); });   // şerit: SGK Durumu Hepsi / Var / Yok
    $('#pmOrSutunlar').addEventListener('change', ev => { const c = ev.target.closest('input[type=checkbox]'); if (!c) return; if (c.checked) { if (!orAyar.sutunlar.includes(c.value)) orAyar.sutunlar.push(c.value); } else orAyar.sutunlar = orAyar.sutunlar.filter(x => x !== c.value); c.closest('label').classList.toggle('secili', c.checked); ozelRaporCiz(); });
    $('#pmOrSutunSifirla').addEventListener('click', ev => { ev.preventDefault(); orAyar.sutunlar = OR_VARSAYILAN.slice(); orSutunCiz(); ozelRaporCiz(); });
    const orKosulOku = r => { const c = orAyar.kosullar[+r.dataset.i]; if (!c) return; c.a = r.querySelector('.orA').value; c.o = r.querySelector('.orO').value; c.v = r.querySelector('.orV').value.trim(); ozelRaporCiz(); };
    $('#pmOrKosullar').addEventListener('change', ev => {
      const r = ev.target.closest('.pm-or-kosul'); if (!r) return; const c = orAyar.kosullar[+r.dataset.i]; if (!c) return;
      if (ev.target.classList.contains('orA') && ev.target.value !== c.a) { c.a = ev.target.value; c.v = ''; if (orSecenek(c.a)) c.o = '='; orKosulCiz(); ozelRaporCiz(); return; }   // alan değişince değer kutusu alana göre (liste / metin) yeniden kurulur
      orKosulOku(r);
    });
    $('#pmOrKosullar').addEventListener('input', ev => { const r = ev.target.classList.contains('orV') ? ev.target.closest('.pm-or-kosul') : null; if (r) orKosulOku(r); });
    $('#pmOrKosullar').addEventListener('click', ev => { const b = ev.target.closest('.orSil'); if (!b) return; orAyar.kosullar.splice(+b.closest('.pm-or-kosul').dataset.i, 1); orKosulCiz(); ozelRaporCiz(); });
    $('#pmOrKosulEkle').addEventListener('click', () => { orAyar.kosullar.push({ a: 'genelMaliyet', o: '>=', v: '' }); orKosulCiz(); ozelRaporCiz(); const son = $$('#pmOrKosullar .orV').pop(); if (son) son.focus(); });
    $('#pmOrAyarAc').addEventListener('change', () => { $('#pmOrAyar').style.display = $('#pmOrAyarAc').checked ? '' : 'none'; sabitleHepsi(); });
    $('#pmOrKaydet').addEventListener('click', orKaydet); $('#pmOrAd').addEventListener('keydown', ev => { if (ev.key === 'Enter') { ev.preventDefault(); orKaydet(); } });
    $('#pmOrKayitli').addEventListener('change', () => { const v = $('#pmOrKayitli').value; if (v !== '') orYukle(+v); });
    $('#pmOrSil').addEventListener('click', orSil);
    dugmeBagla('#pmOrExcel', '#pmOrPdf', bicim => tabloExcel('ozel_rapor', [{ tablo: '#pmOrTablo', ad: 'Özel Rapor', baslik: `Konsolide İK Paneli · ${$('#pmOrBaslik').textContent.trim()}`, altBaslik: $('#pmOrSayi').textContent.trim(), not: '.na-panel[data-p="ozelrapor"] .pm-not' }], bicim));
    $('#pmOrTablo tbody').addEventListener('click', ev => { if (etkilesim(ev)) return; const tr = ev.target.closest('tr[data-sira]'); if (!tr) return; if (tr.dataset.usira) uretimDuzenle(+tr.dataset.usira); else kisiKartAc(+tr.dataset.sira); });
    // Ek Çalışmalar / Diğer Kesintiler
    ['#pmEkAra', '#pmEkDurum'].forEach(s => { $(s).addEventListener('input', ekCalismaCiz); $(s).addEventListener('change', ekCalismaCiz); });
    ['#pmKesAra', '#pmKesTur'].forEach(s => { $(s).addEventListener('input', kesintiCiz); $(s).addEventListener('change', kesintiCiz); });
    dugmeBagla('#pmEkExcel', '#pmEkPdf', bicim => domExcel('ek_calismalar', [['pmEkOzet', 'Kişi Bazında Ek Çalışma'], ['pmMesaiTablo', 'Mesai Talepleri']], bicim));
    dugmeBagla('#pmKesExcel', '#pmKesPdf', bicim => domExcel('diger_kesintiler', [['pmKesOzet', 'Kişi Bazında Kesintiler'], ['pmAvansTablo', 'Şahsi Avans Talepleri'], ['pmPlanTablo', 'Kesinti ve İcra Planları']], bicim));
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
    dugmeBagla('#pmMhExcel', '#pmMhPdf', bicim => domExcel('muhasebe_kayitlari', [['pmMhSimTablo', 'Simülasyonlar'], ['pmMhFisTablo', 'Fişler'], ['pmMhOzetTablo', 'Özet Simülasyon']].concat(mhDetay ? [['pmMhSatirTablo', 'Personel Bazlı Satırlar']] : []).concat(mhEslemeAcik ? [['pmMhEslemeTablo', 'Hesap Eşleştirme']] : []), bicim));
    // satır düzenleme: üretim, prim, kesinti planı, birleşim
    $('#pmUretimTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr && tr.dataset.id) uretimDuzenle(+tr.dataset.id); });
    $('#pmPrimTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr && tr.dataset.id) primDuzenle(+tr.dataset.id); });
    $('#pmBirlesimTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-id]'); if (tr && tr.dataset.kaynak) birlesimSatirAc(tr); });
    $('#pmPlanTablo tbody').addEventListener('click', ev => { if (ev.target.closest('a, label, input, button')) return; const tr = ev.target.closest('tr[data-id]'); if (tr) planDuzenle(+tr.dataset.id); });
    // her satır tıklanabilir: kişi özetleri → kart; prim uyumu → kart; senaryo → kart / üretim; özet raporu → filtreye in
    ['#pmEkOzet tbody', '#pmKesOzet tbody'].forEach(s => $(s).addEventListener('click', ev => { if (etkilesim(ev)) return; const tr = ev.target.closest('tr[data-sira]'); if (tr) kisiKartAc(+tr.dataset.sira); }));
    $('#pmPrimUyum tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-ad]'); if (tr) kisiKartAc(0, tr.dataset.ad); });
    $('#pmCikisTablo tbody').addEventListener('click', ev => { if (etkilesim(ev)) return; const tr = ev.target.closest('tr[data-k]'); if (!tr) return; if (tr.dataset.kaynak === 'Üretim') uretimDuzenle(+tr.dataset.usira); else kisiKartAc(+tr.dataset.sira0); });
    $('#pmOdemeTablo tbody').addEventListener('click', ev => { if (etkilesim(ev)) return; const tr = ev.target.closest('tr[data-k]'); if (!tr) return; if (ev.target.closest('td.od-gir')) { odemeGirisAc(tr.dataset.k); return; } const i = tr.dataset.k.indexOf(':'); const kaynak = tr.dataset.k.slice(0, i), id = +tr.dataset.k.slice(i + 1); if (kaynak === 'Üretim') uretimDuzenle(id); else kisiKartAc(id); });   // ödeme satırı → kişi kartı (üretim → üretim düzenleme); Ödeme Durumu seçimi hariç (5 Kas 2026)
    // Personel Cari Kartı ve Şirket Teşvikleri sekmeleri (5 Kas 2026)
    $('#pmCariTablo tbody').addEventListener('click', ev => { if (etkilesim(ev)) return; const tr = ev.target.closest('tr[data-sira0]'); if (tr && tr.dataset.sira0) kisiKartAc(+tr.dataset.sira0); });
    $('#pmCariEkle').addEventListener('click', () => kartAc(0));
    dugmeBagla('#pmCariExcel', '#pmCariPdf', bicim => ekExcel('cari', bicim));
    $('#pmTesvikTablo tbody').addEventListener('change', tesvikSec);
    dugmeBagla('#pmTesvikExcel', '#pmTesvikPdf', bicim => domExcel('sirket_tesvikleri', [['pmTesvikTablo', 'Teşvik Kataloğu ve Şirket Seçimi'], ['pmTesvikOzet', 'Şirket Bazında Etki']], bicim));
    const filtreyeIn = (anahtar, deger, sgk) => {   // özet satırı → ilgili ana filtre seçilir, Personel Maliyet Tablosu'na dönülür
      const hedef = { catiDepartman: '#pmCati', sgkIsvereni: '#pmSgk', fiiliGorevYeri: '#pmYer', bolum: '#pmBolum', unvan: '#pmUnvan', ozelDurum: '#pmOzel' }[anahtar];
      if (anahtar === 'kaynak') { sekme(deger === 'Özel' ? 'ozel' : deger === 'Üretim' ? 'uretim' : 'tablo'); return; }
      if (!hedef) return; const e = $(hedef); const v = anahtar === 'ozelDurum' && deger === 'Özel Durumu Yok' ? '_yok' : deger; if ([...e.options].some(o => o.value === v)) e.value = v; else return;
      if (sgk) $('#pmSgk').value = [...$('#pmSgk').options].some(o => o.value === sgk) ? sgk : '';
      const ozelMi = V.personel.some(p => p.ozel && (p.durum || 'Aktif') === 'Aktif' && (anahtar === 'catiDepartman' ? p.catiDepartman : anahtar === 'sgkIsvereni' ? p.sgkIsvereni : anahtar === 'bolum' ? p.bolum : anahtar === 'unvan' ? p.unvan : anahtar === 'fiiliGorevYeri' ? p.fiiliGorevYeri : p.ozelDurum) === deger) && !V.personel.some(p => !p.ozel && (p.durum || 'Aktif') === 'Aktif' && (anahtar === 'catiDepartman' ? p.catiDepartman : anahtar === 'sgkIsvereni' ? p.sgkIsvereni : anahtar === 'bolum' ? p.bolum : anahtar === 'unvan' ? p.unvan : anahtar === 'fiiliGorevYeri' ? p.fiiliGorevYeri : p.ozelDurum) === deger);
      sekme(ozelMi ? 'ozel' : 'tablo'); ciz(); mesaj('info', `Filtre uygulandı: ${esc(KIRILIM_AD[anahtar] || anahtar)} = ${esc(deger)}${sgk ? ' · SGK İşvereni = ' + esc(sgk) : ''}. Temizle ile kaldırın.`);
    };
    $('#pmKirilimTablo tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-deger]'); if (tr) filtreyeIn($('#pmKirilim').value, tr.dataset.deger); });
    $('#pmMutabakat tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr[data-sekme]'); if (tr) sekme(tr.dataset.sekme); });   // mutabakat satırı → kaynağın sekmesi
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
    dugmeBagla('#pmCikisExcel', '#pmCikisPdf', bicim => domExcel('isten_cikis_senaryosu', [['pmCikisTablo', 'İşten Çıkış Senaryosu'], ['pmDepTablo', 'Departman Kalan'], ['pmDepGorevTablo', 'Departman ve Görev']], bicim));

    dugmeBagla('#pmUretimExcel', '#pmUretimPdf', bicim => ekExcel('uretim', bicim)); dugmeBagla('#pmPrimExcel', '#pmPrimPdf', bicim => ekExcel('prim', bicim)); dugmeBagla('#pmBirlesimExcel', '#pmBirlesimPdf', bicim => ekExcel('birlesim', bicim));
    ['#pmAra', '#pmDurum', '#pmSgk', '#pmYer', '#pmCati', '#pmBolum', '#pmUnvan', '#pmOzel'].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    $('#pmTemizle').addEventListener('click', () => { $('#pmAra').value = ''; ['#pmSgk', '#pmYer', '#pmCati', '#pmBolum', '#pmUnvan', '#pmOzel'].forEach(s => $(s).value = ''); $('#pmDurum').value = 'Aktif'; ciz(); });
    dugmeBagla('#pmExcel', '#pmPdf', excel); dugmeBagla('#pmOzetExcel', '#pmOzetPdf', ozetExcel); $('#pmKirilim').addEventListener('change', cizOzet);
    $('#pmDuzenle').addEventListener('click', () => modDegis(true)); $('#pmVazgec').addEventListener('click', () => modDegis(false)); $('#pmKaydet').addEventListener('click', kaydet); $('#pmEkle').addEventListener('click', () => kartAc(0));
    $('#pmTablo tbody').addEventListener('click', ev => { const k = ev.target.closest('button.kart'); if (k) { kartAc(+k.dataset.sira); return; } const s = ev.target.closest('button.sil'); if (s) { sil(+s.dataset.sira); return; } if (duzenleme || etkilesim(ev)) return; const tr = ev.target.closest('tr[data-sira]'); if (tr) { if (tr.dataset.usira) uretimDuzenle(+tr.dataset.usira); else kartAc(+tr.dataset.sira); } });   // satıra tıkla → kart; üretim satırı → üretim düzenleme
    $('#pmTop10 tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr.kartAc'); if (!tr) return; const p = bul(+tr.dataset.sira); if (!p) return; if (!!p.ozel !== (kapsam === 'ozel')) kapsam = p.ozel ? 'ozel' : 'bordro'; kartAc(p.sira); });
    $('#pmKartKapat').addEventListener('click', kartKapat); $('#pmPerde').addEventListener('click', kartKapat);
    $$('#pmSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    // yoğun yerleşim: rapor açıklaması "i" ile açılır; şerit alt yazıları başlık ipucunda; pencere boyutu değişince tablo kabı yeniden ölçülür (KPI kutuları ortak na-kpi ölçüsünde)
    $('#pmBilgi').addEventListener('click', () => { kok.classList.toggle('aciklama'); kapBoyut(); });
    $$('.pm-rapor-ust').forEach(u => { const h = u.querySelector('h5'), a = u.querySelector('.alt:not(#pmOzetKapsam)'); if (h && a) h.title = a.textContent.trim(); });
    window.addEventListener('resize', () => { clearTimeout(kapBoyut.z); kapBoyut.z = setTimeout(sabitleHepsi, 120); });
    kayitBilgi(); ciz(); sekme(kok.dataset.baslangic || VARSAYILAN_SEKME); window.NA_HAZIR = true;   // açılış sekmesi: Personel Cari Kartı (6 Kas 2026)
    const kisiP = +new URLSearchParams(location.search).get('kisi'); if (kisiP && bul(kisiP)) kartAc(kisiP);   // ?kisi=<sıra> (9 Kas 2026): Personel Carileri / Personel Dosyası'ndan gelen bağlantı kişi kartını açar
    const kod = kok.dataset.kod; if (kod) { const p = V.personel.find(x => x.cariKodu === kod); if (p) { if (!!p.ozel !== (kapsam === 'ozel')) sekme(p.ozel ? 'ozel' : 'tablo'); kartAc(p.sira); } }
  }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
