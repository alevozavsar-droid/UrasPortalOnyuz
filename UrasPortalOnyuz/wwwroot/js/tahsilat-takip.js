// ============================================================
// TAHSİLAT RAPORU (/Rapor117, 16 Kas 2026) — Views/Rapor117/Index.cshtml · stil wwwroot/css/tahsilat-takip.css
// Kullanıcı: "Tahsilat raporunu da İhracat Finansal Takibi tarzında geliştir; tahsilattaki kişi tüm detayları buradan takip edebilsin, toplu vade farkı kesebilsin,
// banka hareketlerinden düşenleri müşteri seçimini buradan yapabilsin; Adat Hesaplama raporunu da tahsilata sekme olarak ekle."
//   Veri    : /Tahsilat/KonsolideTakip (müşteri carileri + açık fatura kalemleri + tahsilatlar + fark faturaları) · /Tahsilat/BankaHareketleri (bankadan gelen girişler + müşteri listesi)
//   İşlemler: /Tahsilat/BankaMusteriSec (banka hareketine müşteri seç, istenirse taslak tahsilat) · /Tahsilat/TopluVadeFarki (seçilen tahsilatlara vade farkı faturası)
//   Sekmeler: ozet · cari (Alacak Takibi) · acik (Açık Faturalar) · hafta (Haftalık Beklenen) | banka · liste · yeni | vftakip (Vade Farkı Takibi) · vade (Vade Farkı Faturalama) · fatura | adat
//   Vade Farkı Takibi (16 Kas 2026, ikinci istek): müşteri bazında mizan bakiyesi, vadesi geçmiş, ay sonuna kadar vadesi gelecek, ay sonu vade farkı (otomatik kesilecek), kesilen vade farkı, son ödeme,
//     vade farkı sonrası ödeme / satış, limit, satış durdur, mutabakat; satıra tıklayınca müşteri detayı (#tkDetay, /Tahsilat/MusteriDetay).
//     İşlemler: /Tahsilat/SatisDurdur · /Tahsilat/MutabakatGonder (doğrudan müşteriye, E-Mutabakat) · /Tahsilat/MusteriVadeFarki (ay sonunu beklemeden bugüne kadar kes).
//     yeni · liste · fatura → Tahsilat giriş panelleri (#th, tahsilat.js; kendi şeridi gizli, buradan sürülür) · adat → Adat Hesaplama (#ad, adat.js)
//   Tablolar: sütun tanımı [anahtar, başlık, tür (m metin · p sayı · d tarih), değer, hücre HTML'i, dip toplamı]; başlığa tıklayınca sıralama; Excel = ekrandaki tablo (TabloExcel).
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('tk'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const TR = 'tr-TR', kucuk = s => String(s ?? '').toLocaleLowerCase(TR);
  // 19 Kas 2026 (kullanıcı: "$ PB Getir kutucuğu: tüm para birimlerini $ cinsinden getirsin"): PBG = 'USD' iken bütün TL tutarlar güncel USD kuruyla (K.kurlar.USD) dolara çevrilir; başlıklardaki\u00A0(₺) → ($)
  let PBG = ''; try { PBG = localStorage.getItem('tkPb') === 'USD' ? 'USD' : ''; } catch (x) { }
  const usdKur = () => (typeof K !== 'undefined' && K && K.kurlar && Number(K.kurlar.USD)) || 0;
  const tl = v => PBG === 'USD' && usdKur() ? Number((v || 0) / usdKur()).toLocaleString(TR, { maximumFractionDigits: 0 }) + '\u00A0$' : Number(v || 0).toLocaleString(TR, { maximumFractionDigits: 0 }) + '\u00A0₺';
  const n2 = v => Number(v || 0).toLocaleString(TR, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const tl2 = v => PBG === 'USD' && usdKur() ? n2((v || 0) / usdKur()) + '\u00A0$' : n2(v) + '\u00A0₺';   // iki ondalıklı tutar (vade farkı)
  const pbBaslik = s => PBG === 'USD' ? String(s).replace(/\(₺\)/g, '($)') : s;
  const trh = iso => { if (!iso) return '—'; const p = String(iso).slice(0, 10).split('-'); return p.length === 3 ? `${p[2]}.${p[1]}.${p[0]}` : iso; };
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const riskRozet = d => rozet(d || '—', d === 'Riskli' ? 'hata' : d === 'İzlemede' ? 'bekle' : 'ok');
  const tire = '<span class="text-muted">—</span>';
  const topla = (l, f) => l.reduce((s, x) => s + (Number(f(x)) || 0), 0);
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => { s.style.display = 'none'; }, 12000); }
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }).then(r => r.json());

  let K = null, B = null;                                   // KonsolideTakip · BankaHareketleri
  // Cari kart süzgeçleri (kullanıcı 16 Kas 2026: "cari kart filtrelemeleri biraz daha olsun; cari sınıfı, ilişki durumu … filtrelenebilir, dinamik olmalı").
  // 19 Kas 2026 (kullanıcı: "Konsolide Stok Raporu tarzında 4 – 5 sabit filtre, sonra Tüm Süzgeçler; birden fazla seçime onay versin — ilişki durumunda hem Grup Dışı hem İlişkili"):
  // ortak SuzgecCubuk (wwwroot/js/suzgec-cubuk.js). Her süzgeç ÇOKLU SEÇİM: F[k] = [] (süzgeç yok) ya da seçili değerler; v(k) müşterinin o alandaki değer(ler)i, biri seçime uyarsa müşteri geçer.
  // Seçenekler KADEMELİDİR: bir süzgecin seçenekleri, diğer seçimlere uyan müşterilerde geçen değerlerdir (secenekler). Yeni süzgeç eklemek için bu listeye satır (ve gerekirse PANEL grubuna anahtar) eklemek yeter.
  const SUZ = [
    { k: 'sirket', ad: 'Şirket', bos: 'Tümü (Konsolide)', v: k => [k.sirket] }, { k: 'bolge', ad: 'Bölge', v: k => [k.bolge] }, { k: 'ulke', ad: 'Ülke', v: k => [k.suzgec.ulke] }, { k: 'sinif', ad: 'Cari Sınıfı', v: k => [k.sinif] },
    { k: 'iliski', ad: 'İlişki Durumu', v: k => [k.iliski] }, { k: 'rol', ad: 'Cari Rolü', v: k => [k.suzgec.rol] }, { k: 'durum', ad: 'Kart Durumu', v: k => [k.suzgec.durum] }, { k: 'vadeSarti', ad: 'Vade Şartı', v: k => [k.vadeSarti] },
    { k: 'anaGrup', ad: 'Faaliyet Ana Grubu', v: k => k.suzgec.anaGruplar || [] }, { k: 'grup', ad: 'Faaliyet Grubu', v: k => (k.suzgec.gruplar || []).filter(x => !F.anaGrup.length || F.anaGrup.some(a => x.startsWith(a + '|'))).map(x => x.replace('|', ' › ')) }, { k: 'temsilci', ad: 'Temsilci', v: k => k.suzgec.temsilciler || [] },
    { k: 'risk', ad: 'Risk', v: k => [k.risk] }, { k: 'limit', ad: 'Limit Durumu', v: k => [k.limitDurum] }, { k: 'satis', ad: 'Satış Durumu', v: k => [k.vf.satisDurdu ? 'Durduruldu' : 'Açık'] }, { k: 'mutabakat', ad: 'Mutabakat', v: k => [k.vf.mutabakat] },
    // 19 Kas 2026 (kullanıcı: "filtrelemelere işlem tipi X / R ekle, ikisini de seçebileyim"): açık fatura kalemlerinin ve tahsilatların işlem tipi; seçilince müşteri toplamları yalnız o tipteki kalemlerden hesaplanır (cariler → agg)
    { k: 'tip', ad: 'İşlem Tipi', v: k => [...new Set(k.kalemler.map(x => x.islemTipi).filter(Boolean))] }];
  const HIZLI = ['sirket', 'tip', 'bolge', 'iliski', 'temsilci', 'sinif'];   // çubukta her zaman görünen sabit süzgeçler
  const PANEL = [{ ad: 'Cari Kart', alanlar: ['ulke', 'rol', 'durum', 'vadeSarti'] }, { ad: 'Faaliyet', alanlar: ['anaGrup', 'grup'] }, { ad: 'Takip', alanlar: ['risk', 'limit', 'satis', 'mutabakat'] }];
  const F = { ara: '', cari: '' }; SUZ.forEach(s => { F[s.k] = []; });
  const suzUyar = (k, haric) => SUZ.every(s => s.k === haric || !F[s.k].length || s.v(k).some(x => F[s.k].includes(x)));
  const suzAktif = haric => SUZ.some(s => s.k !== haric && F[s.k].length);
  // süzgeç seçenekleri: diğer seçimlere uyan müşterilerdeki değerler + kayıt sayısı (kademeli)
  function secenekler(a) {
    const s = SUZ.find(x => x.k === a), m = new Map();
    if (a === 'bolge' && BOLGE_KILIT) return [{ v: BOLGE_KILIT, n: (K ? K.satirlar : []).filter(k => k.bolge === BOLGE_KILIT).length }];   // kilitli bölge (İhracat Finansal Takibi): yalnız Yurtdışı
    (K ? K.satirlar : []).filter(k => suzUyar(k, a)).forEach(k => s.v(k).filter(Boolean).forEach(v => m.set(v, (m.get(v) || 0) + 1)));
    return [...m.entries()].sort((x, y) => x[0].localeCompare(y[0], TR)).map(([v, n]) => ({ v, n }));
  }
  const SZ = window.SuzgecCubuk.kur({ kap: $('#tkFiltreKart'), alanlar: SUZ, hizli: HIZLI, gruplar: PANEL, secenekler, F, ek: $('#tkEk'), alt: $('#tkKapsam'), araYer: 'Müşteri kodu, adı, belge no, gönderen…',   // kapsam sayacı Temizle'nin altında (19 Kas 2026)
    tumIpucu: 'Öbür süzgeçler: ülke, cari rolü, kart durumu, vade şartı, faaliyet ana grubu ve grubu, risk, limit, satış durumu, mutabakat', degisti: k => { if (k === 'temizle') { F.cari = ''; DN.bas = DN.bit = ''; DN.ad = ''; donemYaz(); } bolgeKilit(); ciz(); } });
  // 19 Kas 2026: GÖMÜLÜ / KİLİTLİ BÖLGE — İhracat Finansal Takibi "Tahsilat Takibi · Yurtdışı" sekmesi bu raporu ?gomulu=1&bolge=Yurtdışı ile çerçeve içinde açar: bölge süzgeci Yurtdışı'na kilitli (Temizle bile kaldırmaz),
  //   üst başlık gizli (tk-gomulu). Böylece Tahsilat raporunun BÜTÜN sekmeleri yurtdışına uyarlanmış ve iki rapor aynı veriyle senkron çalışır.
  //   19 Kas 2026 gece (kullanıcı: "iki ayrı rapor oldu, tek raporda birleştirelim"): İHRACAT KİPİ (data-ihracat="1", /IhracatFinans): bu sayfa İhracat Finansal Takibi'nin kendisidir — bölge Yurtdışı kilitli (seçenekte Yurtiçi yok),
  //   şeridin başında ib* sekmeleri (İhracat Özeti, Beyanname / İBKB Dosyaları, İBKB Belgeleri, Müşteri Bazlı, Şube Bakiyeleri, DAB Bağlantıları → tek panel data-p="ib", ihracat-finans.js çizer; window.IhracatFinans ↔ window.TahsilatTakip).
  const IHRACAT = kok.dataset.ihracat === '1', BOLGE_KILIT = kok.dataset.bolge || new URLSearchParams(location.search).get('bolge') || '';
  const IB = p => /^ib[A-Z]/.test(p || '');
  function bolgeKilit() { if (BOLGE_KILIT && (F.bolge.length !== 1 || F.bolge[0] !== BOLGE_KILIT)) { F.bolge = [BOLGE_KILIT]; SZ.yaz(); } }
  bolgeKilit();
  // ---------- 19 Kas 2026: işlem tipi toplamları, dönem (Hızlı Dönem + tarih aralığı), dövizli görünüm, aylık matris ----------
  const kalemUy = x => !F.tip.length || F.tip.includes(x.islemTipi || '');
  const kal = k => F.tip.length ? k.kalemler.filter(kalemUy) : k.kalemler;
  /** müşterinin açık fatura toplamları seçili işlem tipindeki kalemlerden (sunucudaki formülün aynısı; süzgeç yokken sunucu değerleriyle eşit) */
  function agg(k) { const l = kal(k), kova = (a, b) => topla(l.filter(x => x.gun >= a && x.gun <= b), x => x.tl), gec = l.filter(x => x.gun < 0); return { gecmis: kova(-1e9, -1), gecmisAdet: gec.length, maxGecikme: gec.length ? Math.max(...gec.map(x => -x.gun)) : 0, buHafta: kova(0, 7), g8_30: kova(8, 30), g31_60: kova(31, 60), g60: kova(61, 1e9), acikToplam: topla(l, x => x.tl), acikAdet: l.length, kalemler: l }; }
  // dövizli görünüm (kullanıcı: "tüm sekmelerde yurt dışı müşteri borçları için dövizli getir kutucuğu"): tutar belge para birimiyle, TL karşılığı ipucunda; tercih tarayıcıda saklanır
  let DV = false; try { DV = localStorage.getItem('tkDoviz') === '1'; } catch (x) { }
  const pbAd = pb => !pb || pb === 'TL' || pb === 'TRY' ? '₺' : pb;
  const bolunmez = s => String(s || '').replace(/(\d) (?=[A-Za-z₺$€£])/g, '$1\u00A0');   // sunucudan gelen "513.881,34 TL · 44.629,52 EUR" gibi metinlerde tutar ile para birimi arası bölünmez boşluk (20 Kas 2026)
  const dovizli = l => (l || []).some(x => pbAd(x.pb) !== '₺');
  /** listeyi para birimine göre toplar → "83.527,77 EUR · 1.000\u00A0₺"; alan: satırın belge para birimi tutarı (varsayılan kalan) */
  function pbTop(l, alan) { const m = {}; (l || []).forEach(x => { const p = pbAd(x.pb); m[p] = (m[p] || 0) + (Number(alan ? alan(x) : x.kalan) || 0); }); return Object.keys(m).filter(p => Math.abs(m[p]) > 0.005).sort((a, b) => (a === '₺') - (b === '₺') || a.localeCompare(b)).map(p => p === '₺' ? tl(m[p]) : n2(m[p]) + ' ' + p).join(' · '); }
  /** tutar hücresi: dövizli görünümde ve listede TL dışı belge varsa para birimiyle (TL karşılığı ipucunda), yoksa TL */
  const tutarH = (tlDeger, l, alan) => DV && dovizli(l) ? `<span title="${esc(tl(tlDeger))}">${esc(pbTop(l, alan))}</span>` : tl(tlDeger);
  // dönem: Tahsilatlar, Kesilen Fark Faturaları, Kesilecek Vade Farkı, Aylık Tahsilatlar, Planlanan ↔ Gerçekleşen sekmelerinde (çubuktaki Hızlı Dönem düğmeleri + tarih aralığı)
  const DN = { bas: '', bit: '', ad: '' }, DONEMLI = ['liste', 'fatura', 'vade', 'aylik', 'plan', 'satis'];
  const iso = d => d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
  const bugunD = () => new Date(((K && K.bugun) || iso(new Date())) + 'T00:00:00');
  function donemKur(ad) {
    const b = bugunD(), y = b.getFullYear(), m = b.getMonth(), son = (yy, mm) => new Date(yy, mm + 1, 0);
    const pzt = new Date(b); pzt.setDate(b.getDate() - ((b.getDay() + 6) % 7)); const paz = new Date(pzt); paz.setDate(pzt.getDate() + 6);   // bu hafta: Pazartesi – Pazar
    const r = { bugun: [b, b], buHafta: [pzt, paz], buAy: [new Date(y, m, 1), son(y, m)], son90: [new Date(y, m, b.getDate() - 90), b], tumu: null }[ad];   // 19 Kas 2026 (kullanıcı): Bugün · Bu Hafta · Bu Ay · Son 90 Gün · Tümü
    DN.bas = r ? iso(r[0]) : ''; DN.bit = r ? iso(r[1]) : ''; DN.ad = r ? ad : ''; donemYaz(); ciz();
  }
  const donemUy = t => (!DN.bas || String(t || '').slice(0, 10) >= DN.bas) && (!DN.bit || String(t || '').slice(0, 10) <= DN.bit);
  const donemMetni = () => DN.bas || DN.bit ? `${DN.bas ? trh(DN.bas) : '…'} – ${DN.bit ? trh(DN.bit) : '…'}` : 'tüm tarihler';
  function donemYaz() { $('#tkDonemBas').value = DN.bas; $('#tkDonemBit').value = DN.bit; $$('#tkDonem button[data-donem]').forEach(b => b.classList.toggle('aktif', b.dataset.donem === (DN.ad || (!DN.bas && !DN.bit ? 'tumu' : '')))); }
  /** aylık tablolar için ay listesi (yyyy-MM): dönem seçiliyse o aralık (en çok 24 ay), yoksa içinde bulunulan yıl */
  function aylar() { const b = bugunD(); const bas = DN.bas ? new Date(DN.bas + 'T00:00:00') : new Date(b.getFullYear(), 0, 1), bit = DN.bit ? new Date(DN.bit + 'T00:00:00') : new Date(b.getFullYear(), 11, 31); const l = []; for (let d = new Date(bas.getFullYear(), bas.getMonth(), 1); d <= bit && l.length < 24; d.setMonth(d.getMonth() + 1)) l.push(d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0')); return l; }
  const AYAD = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];
  const ayEtiket = ym => ym === 'disi' ? 'Dönem Dışı' : AYAD[+ym.slice(5, 7) - 1] + ' ' + ym.slice(0, 4);   // 'disi': seçili ayların dışında kalan kayıtlar (toplamlar öbür sekmelerle tutsun diye)
  const trIso = s => { const p = String(s || '').split('.'); return p.length === 3 ? `${p[2]}-${p[1]}-${p[0]}` : String(s || '').slice(0, 10); };   // dd.MM.yyyy → yyyy-MM-dd
  /** aylar sütun, kalemler satır (Nakit Akış Planlama'daki Aylık Ödemeler düzeni): satir = { ad, alt?, deger: ym → sayı, h?: (ym | null, değer) → html, toplam?: aylar → sayı, toplamH?, sinif? } */
  function matris(id, ilkBaslik, ays, satirlar, bos) {
    const t = $('#tkTablo-' + id), bugunYm = (K.bugun || '').slice(0, 7); t._mx = satirlar;   // 19 Kas 2026 gece (kullanıcı: "rakamlara tıklayınca detay"): satırda liste(ym) varsa hücre tıklanır → #tkPen'de kalem listesi
    t.tHead.innerHTML = `<tr><th class="ad sirasiz">${pbBaslik(ilkBaslik)}${PBG === 'USD' ? ' <small>($)</small>' : ''}</th>${ays.map(ym => `<th class="sirasiz ${ym === bugunYm ? 'bugun-ay' : ''}">${ayEtiket(ym)}</th>`).join('')}<th class="sirasiz">Toplam</th></tr>`;
    // 20 Kas 2026 (kullanıcı: "tüm raporlarda alt toplam"): son satır toplam satırıysa dipte (tfoot) çizilir — kaydırmada görünür kalır, Excel'de dip satırı olur
    const dipI = satirlar.length > 1 && satirlar[satirlar.length - 1].sinif === 'toplam' ? satirlar.length - 1 : -1;
    const satirH = (s, si) => { const vals = ays.map(ym => s.deger(ym)); const top = s.toplam ? s.toplam(ays) : vals.reduce((a, v) => a + (Number(v) || 0), 0); const h = s.h || ((ym, v) => v ? tl(v) : tire);
      const hc = (ym, v) => { const ic = h(ym, v); return s.liste && ic && ic !== tire ? `<a href="#" class="tk-mx" data-mx="${si}|${ym == null ? '' : ym}" title="Tıklayınca kalem kalem detay">${ic}</a>` : ic; };
      return `<tr class="${s.sinif || ''}"><td class="ad">${s.adH || `<b>${esc(s.ad)}</b>`}${s.alt ? `<small>${esc(s.alt)}</small>` : ''}</td>${ays.map((ym, i) => `<td class="${ym === bugunYm ? 'bugun-ay' : ''}">${hc(ym, vals[i])}</td>`).join('')}<td class="top">${s.toplamH ? s.toplamH(top, ays) : hc(null, top)}</td></tr>`; };
    t.tBodies[0].innerHTML = satirlar.map((s, si) => si === dipI ? '' : satirH(s, si)).join('') || `<tr><td colspan="${ays.length + 2}" class="na-bos">${bos || 'Kayıt yok.'}</td></tr>`;
    t.tFoot.innerHTML = dipI >= 0 ? satirH(satirlar[dipI], dipI) : ''; t._kol = null;
  }
  /** matris hücresi detayı: tahsilat satırı / fatura kapama dağıtımı / açık kalem / fark faturası — alanlar hangisi varsa */
  function mxDetayH(items) {
    const rows = items.map(x => { const kod = x.cariKodu || x.kod || '', belge = x.no || x.belgeNo || x.belge || ''; const tarih = x.odemeTarihi || x.tarihIso || x.tarih || x.vade || ''; const iso = String(tarih).includes('.') ? trIso(tarih) : String(tarih).slice(0, 10);
      const tutar = x.tutar != null && x.pb ? `${n2(x.tutar)}\u00A0${esc(pbAd(x.pb))}` : x.kalan != null && x.pb ? `${n2(x.kalan)}\u00A0${esc(pbAd(x.pb))}` : x.matrah != null ? n2(x.matrah) + '\u00A0₺' : tire; const tlv = x.tutarTl ?? x.tl ?? x.toplam ?? 0; const gun = x.gecikmeGun != null ? x.gecikmeGun : (x.gun != null ? -x.gun : null);
      return [kod && belge ? belgeLink(kod, belge) : `<b>${esc(belge)}</b>`, trh(iso), `${esc(kod)} ${esc(x.cariAd || x.kisaAd || '')}`.trim(), esc(x.yontem || x.odemeYontem || x.kaynak || x.tur || x.kullanici || ''), tutar, `<b>${tl(tlv)}</b>`, gun == null ? tire : gun > 0 ? `<span class="gec">${gun} gün gecikme</span>` : gun < 0 ? `<span class="ok">${-gun} gün erken</span>` : 'vadesinde']; });
    return mt(['Belge / Tahsilat No', 'Tarih', 'Müşteri', 'Yöntem · Kaynak · Kişi', 'Tutar', 'TL', 'Gecikme'], rows, 'Kayıt yok.') + `<div class="text-muted mt-1" style="font-size:.62rem">${items.length} kayıt · toplam ${tl(topla(items, x => Number(x.tutarTl ?? x.tl ?? x.toplam ?? 0)))}</div>`;
  }
  kok.addEventListener('click', e => { const a = e.target.closest('a.tk-mx'); if (!a) return; e.preventDefault(); const t = a.closest('table'); const [si, ym] = a.dataset.mx.split('|'); const s = t && t._mx && t._mx[+si]; if (!s || !s.liste) return; const items = s.liste(ym || null) || []; penAc(`<i class="fas fa-table-cells me-1"></i>${esc(s.ad)} · ${ym ? ayEtiket(ym) : 'Toplam'}`, s.alt || '', mxDetayH(items), '<span class="bos"></span><button type="button" class="tk-dgm" data-p-kapat>Kapat</button>'); });
  // TIKLANIR TUTARLAR (20 Kas 2026; kullanıcı: "raporlardaki sayılar olabildiğince dinamik olmalı; çek / senet 598.252 ₺'ye tıkladığımda hemen o çekin bilgileri gelmeli"):
  //   vade kovaları (Gecikmiş · Bu Hafta · 8–30 · 31–60 · 60+), Çek / Senet ve Toplam Risk tutarı → #tkPen'de kalem kalem (açık faturalar / portföydeki çek · senetler)
  const kovaLink = (k, tur, ic) => `<a href="#" class="tk-kova" data-kova="${esc(k.kod)}|${tur}" title="Tıklayınca kalem kalem">${ic}</a>`;
  const KOVA = { gecmis: ['Gecikmiş Açık Faturalar', x => x.gun < 0], buHafta: ['Bu Hafta Vadesi Gelen Faturalar', x => x.gun >= 0 && x.gun <= 7], g8_30: ['8–30 Gün İçinde Vadesi Gelen Faturalar', x => x.gun >= 8 && x.gun <= 30], g31_60: ['31–60 Gün İçinde Vadesi Gelen Faturalar', x => x.gun >= 31 && x.gun <= 60], g60: ['60+ Gün Sonra Vadesi Gelen Faturalar', x => x.gun >= 61] };
  const cekTablo = k => { const l = k.cekler || []; const gunH = g => g < 0 ? `<span class="gec">${-g} gün geçti</span>` : g === 0 ? 'bugün' : `${g} gün kaldı`;
    return mt(['Belge No', 'Tür', 'Alınış Tarihi', 'Vade', 'Vadeye', 'Tutar', 'TL Karşılığı', 'İşlem Tipi', 'Açıklama'], l.map(x => [`<b>${esc(x.belge || '—')}</b>`, esc(x.tip || ''), trh(x.tarih), `<b>${trh(x.vade)}</b>`, gunH(x.gun), `${n2(x.tutar)} ${esc(pbAd(x.pb))}`, `<b>${tl(x.tl)}</b>`, esc(x.islemTipi || ''), esc(x.aciklama || '')]), 'Portföyde çek / senet yok.')
      + `<div class="text-muted mt-1" style="font-size:.66rem"><b>${l.length} belge · toplam ${tl(topla(l, x => x.tl))}</b></div>`; };
  kok.addEventListener('click', e => { const a = e.target.closest('a.tk-kova'); if (!a) return; e.preventDefault(); e.stopPropagation(); const [kod, tur] = a.dataset.kova.split('|'), k = cariBul(kod); if (!k) return;
    const kalem = f => k.kalemler.filter(f).map(x => Object.assign({ kod: k.kod, kisaAd: k.kisaAd }, x)), kapat = '<span class="bos"></span><button type="button" class="tk-dgm" data-p-kapat>Kapat</button>', ad = `${esc(k.kod)} ${esc(k.kisaAd)}`;
    if (tur === 'cek') penAc(`<i class="fas fa-money-check me-1"></i>Çek / Senet · ${ad}`, 'portföydeki (vadesi bekleyen) belgeler', cekTablo(k), kapat);
    else if (tur === 'risk') penAc(`<i class="fas fa-shield-halved me-1"></i>Toplam Risk · ${ad}`, `açık faturalar ${tl(k.acikToplam || 0)} + çek / senet ${tl(k.cek || 0)} = ${tl((k.acikToplam || 0) + (k.cek || 0))}`, `<div class="tk-nbaslik">Açık Faturalar</div>${mxDetayH(kalem(() => true))}<div class="tk-nbaslik mt-2">Çek / Senet</div>${cekTablo(k)}`, kapat);
    else if (KOVA[tur]) penAc(`<i class="fas fa-file-invoice me-1"></i>${KOVA[tur][0]} · ${ad}`, '', mxDetayH(kalem(KOVA[tur][1])), kapat);
  });
  // hızlı süzgeçler: yalnız gecikmiş faturalar · yalnız müşterisi seçilmemiş banka girişleri · yalnız faturası kesilmemiş vade farkları · Vade Farkı Takibi: vadesi geçmişler / limit aşanlar / vade farkı sonrası ödemeyenler
  const H = { gecmis: false, bankaAcik: true, vadeDurum: 'tumu', kapamaAcik: true, vfGecmis: false, vfLimit: false, vfOdemeYok: false, surecGecmis: false, surecAranmayan: false };
  let SG = 'musteri', ND = '';                              // Aylık Süreç: görünüm (musteri | not) · Görüşme Notları görünümünde okunan dönem (yyyy-MM; boş = bu ay)
  const SIRA = {};                                          // sekme tablosu → { k, y }
  const SECILI = new Set();                                 // Vade Farkı Faturalama: seçili tahsilat no'ları
  const SECM = new Set();                                   // Vade Farkı Takibi: seçili müşteri kodları
  let KANAL = 'E-posta';                                    // mutabakat gönderim kanalı
  const TH = { yeni: 'yeni', liste: 'liste', fatura: 'fatura' };   // Tahsilat giriş panellerine giden sekmeler → #th içindeki panel
  const RAPOR = ['ozet', 'cari', 'kartlar', 'acik', 'hafta', 'banka', 'aylik', 'plan', 'surec', 'vftakip', 'vade', 'kapama', 'satis', 'ozel'];
  const q0 = new URLSearchParams(location.search);
  let aktif = kok.dataset.sekme || (kok.dataset.no ? 'liste' : kok.dataset.kod ? 'yeni' : 'cari'), kirli = false, HAFTA = [], haftaSec = 0;   // 20 Kas 2026 (kullanıcı): iki rapor da Alacak Takibi ile açılır
  // YENİ BANKA GİRİŞİ UYARISI (kullanıcı 17 Kas 2026: "Banka Tahsilatları sekmesine yeni bir ödeme düştüğünde uyarı gibi bir ikon koy ya da farklı renk; anlayalım bankaya ödeme geldiğini").
  // Görülen banka girişleri tarayıcıda tutulur (localStorage 'tkBankaGorulen'); listede olup görülmemiş giriş varsa sekme sarı + zil ikonu olur, sekme açılınca görülmüş sayılır.
  // O oturumda yeni gelenler tabloda sarı satır ve "Yeni" rozetiyle kalır. İlk kullanımda (kayıt yokken) yalnız bugünün girişleri yeni sayılır. Banka girişleri 60 saniyede bir yeniden okunur.
  const YENI = new Set(); let gorulenIlk = false;
  const gorulenOku = () => { try { const v = localStorage.getItem('tkBankaGorulen'); return v ? new Set(JSON.parse(v)) : null; } catch (x) { return null; } };
  function bankaYeni() {
    if (!B || !B.hareketler) return; const g = gorulenOku(), l = B.hareketler;
    if (!g) { if (!gorulenIlk) { gorulenIlk = true; l.filter(h => h.tarih === (K && K.bugun)).forEach(h => YENI.add(h.id)); } } else l.filter(h => !g.has(h.id)).forEach(h => YENI.add(h.id));
    const bekleyen = l.filter(h => YENI.has(h.id) && !bankaGoruldu.has(h.id)).length, dg = $('#tkSekmeler button[data-p="banka"]'); if (!dg) return;
    dg.classList.toggle('yeni', bekleyen > 0); dg.title = bekleyen > 0 ? `${bekleyen} yeni banka girişi` : '';
    let zil = $('.tk-zil', dg); if (bekleyen > 0 && !zil) dg.insertAdjacentHTML('beforeend', '<i class="fas fa-bell tk-zil"></i>'); else if (!bekleyen && zil) zil.remove();
  }
  const bankaGoruldu = new Set();   // bu oturumda Banka Tahsilatları sekmesi açıkken görülen yeni girişler (sekmedeki uyarı söner, tablodaki vurgu kalır)
  function bankaGorulduYaz() { if (!B || !B.hareketler) return; B.hareketler.forEach(h => { if (YENI.has(h.id)) bankaGoruldu.add(h.id); }); try { localStorage.setItem('tkBankaGorulen', JSON.stringify(B.hareketler.map(h => h.id))); } catch (x) { /* saklanamazsa uyarı her açılışta yeniden çıkar */ } bankaYeni(); }

  // ---------- süzme ----------
  const araUyar = (...p) => !F.ara || kucuk(p.join(' ')).includes(kucuk(F.ara));
  const sinifAd = k => `${k.bolge || ''} · ${k.sinif || ''}`;   // cari sınıfı bölgeyle birlikte (Ticari Müşteriler ve Özel Takip iki bölgede de var)
  // işlem tipi seçiliyse müşteri satırı o tipteki kalemlerin toplamlarıyla (agg) kopyalanır: bütün sekmeler aynı sayıları görür
  const cariler = () => K.satirlar.filter(k => suzUyar(k) && (!F.cari || k.kod === F.cari) && araUyar(k.kod, k.kisaAd, k.resmiUnvan, k.temsilci, k.sirket, k.kalemler.map(x => x.belge).join(' '))).map(k => F.tip.length ? Object.assign({}, k, agg(k)) : k);
  const cariBul = kod => K.satirlar.find(k => k.kod === kod);
  const tahsilatlar = () => (K.tahsilatlar || []).filter(t => { const c = cariBul(t.cariKodu); return (c ? suzUyar(c) : !suzAktif()) && (!F.cari || t.cariKodu === F.cari) && (!F.tip.length || F.tip.includes(t.islemTipi)) && araUyar(t.no, t.cariKodu, t.cariAd, t.yontem, t.vadeFarkiFaturaNo); });
  const islenenler = () => tahsilatlar().filter(t => t.islendi);
  const vadeFarklilar = () => tahsilatlar().filter(t => t.islendi && t.vadeFarki > 0.005);
  // Gecikmeli tahsilat (19 Kas 2026): işlenmiş ve en az bir faturayı vadesinden sonra kapatan tahsilat; vade farkı durumu Kesildi / Kesilmedi / Fark Yok
  const gecikmeli = () => tahsilatlar().filter(t => t.islendi && (t.dagitim || []).some(d => d.gecikmeGun > 0));
  const vfDurum = t => t.vadeFarkiFaturaNo ? 'Kesildi' : t.vadeFarki > 0.005 ? 'Kesilmedi' : 'Fark Yok';
  // banka girişleri: şirket süzgeci hareketin şirketine bakar; diğer cari kart süzgeçleri yalnız müşterisi seçilmiş hareketlerde karşılaştırılabilir (seçilmemişler o süzgeçler açıkken gizlenir)
  // müşterisi seçilmemiş girişte bölge süzgeci para birimine göre tahmin edilir (19 Kas 2026 gece: İhracat kipinde Yurtdışı kilidi → döviz girişleri görünür, TL girişleri Yurtiçi'ne)
  const bolgeTahmin = h => !F.bolge.length || (F.bolge.includes('Yurtdışı') && h.pb !== 'TL') || (F.bolge.includes('Yurtiçi') && h.pb === 'TL');
  const bankaSuz = h => { const c = h.cariKod && cariBul(h.cariKod); return (!F.sirket.length || F.sirket.includes(h.sirket)) && (c ? suzUyar(c, 'sirket') : (!SUZ.some(s => s.k !== 'sirket' && s.k !== 'bolge' && F[s.k].length) && bolgeTahmin(h))); };
  const bankalar = () => ((B && B.hareketler) || []).filter(h => bankaSuz(h) && (!F.cari || h.cariKod === F.cari) && araUyar(h.no, h.karsiAd, h.aciklama, h.referans, h.banka, h.cariAd, h.sirket));
  const bankaAcik = h => h.durum !== 'Onaylandı';
  const oran = () => Number(K.vadeFarkiAylikOran) || 0;

  // ---------- tablo motoru ----------
  // sütun: { k, ad, t: 'm' | 'p' | 'd', v: r => değer, h: r => HTML (yoksa değerden), top: r => dip toplamına katılan sayı (yoksa toplam yok), ac: başlık ipucu, sirasiz }
  const P = (k, ad, v, ek) => Object.assign({ k, ad, t: 'p', v, h: r => { const x = v(r); return x ? tl(x) : tire; }, top: v }, ek || {});
  const M = (k, ad, v, ek) => Object.assign({ k, ad, t: 'm', v, h: r => esc(v(r) || '') || tire }, ek || {});
  const D = (k, ad, v, ek) => Object.assign({ k, ad, t: 'd', v, h: r => trh(v(r)) }, ek || {});
  // SATIR SEÇİMİ VE SEÇİLİ TOPLAM (19 Kas 2026, kullanıcı: "tüm sekmelerde alt toplam ve seçileni toplayabilmeliyim; Haftalık Beklenen'de 2. ve 3. haftayı seçip toplayabileyim"):
  //   her tabloda ilk sütun onay kutusu (başlıktaki kutu görünenlerin hepsi); dipte "Toplam · n" satırının altında "Seçili · n" satırı yalnız seçili satırların toplamlarıyla.
  //   Kendi seçim sütunu olan tablolar (Kesilecek Vade Farkı, Vade Farkı Takibi) secimsiz + secili(r) ile aynı dip satırını kullanır. Anahtar: sec.anahtar(r) ya da kod / no / id / belge / ad.
  const SEC = {};   // tablo id → Set(anahtar)
  const secAnahtar = (sec, r, i) => String(sec.anahtar ? sec.anahtar(r) : (r.kod ?? r.no ?? r.id ?? r.belge ?? r.ad ?? r.i ?? i));
  const secKume = id => SEC[id] || (SEC[id] = new Set());
  function tablo(id, kol, satirlar, sec) {
    sec = sec || {}; const t = $('#tkTablo-' + id), sr = SIRA[id] || sec.sira || null; let l = satirlar.slice();
    if (sr) { const c = kol.find(x => x.k === sr.k); if (c) l.sort((a, b) => { const x = c.v(a), y = c.v(b); return (c.t === 'p' ? (Number(x) || 0) - (Number(y) || 0) : String(x ?? '').localeCompare(String(y ?? ''), TR)) * sr.y; }); }
    const S = sec.secimsiz ? null : secKume(id); const anahtarlar = new Map(); l.forEach((r, i) => anahtarlar.set(r, secAnahtar(sec, r, i)));
    const secili = r => S ? S.has(anahtarlar.get(r)) : sec.secili ? !!sec.secili(r) : false; const secL = (S || sec.secili) ? l.filter(secili) : [];
    if (S) kol = [{ k: '_sec', ad: `<input type="checkbox" data-tsec-hepsi="${id}" title="Görünen satırların hepsini seç / bırak"${l.length && l.every(secili) ? ' checked' : ''}>`, t: 'm', v: () => '', sirasiz: 1, sinif: 'sec', h: r => `<input type="checkbox" data-tsec="${id}" data-a="${esc(anahtarlar.get(r))}" title="Seç → dipte Seçili toplamı"${secili(r) ? ' checked' : ''}>` }, ...kol];
    t.tHead.innerHTML = '<tr>' + kol.map(c => `<th data-k="${c.k}" class="${c.sirasiz ? 'sirasiz' : sr && sr.k === c.k ? (sr.y > 0 ? 'art' : 'azl') : ''}"${c.ac ? ` title="${esc(c.ac)}"` : ''}>${pbBaslik(c.ad)}</th>`).join('') + '</tr>';
    t.tBodies[0].innerHTML = l.length ? l.map((r, i) => `<tr${sec.satir ? ' ' + sec.satir(r, i) : ''}${secili(r) ? ' data-secili="1"' : ''}>${kol.map(c => `<td${c.sinif ? ` class="${typeof c.sinif === 'function' ? c.sinif(r) : c.sinif}"` : ''}>${c.h(r)}</td>`).join('')}</tr>`).join('') : `<tr><td colspan="${kol.length}" class="na-bos">${sec.bos || 'Süzgece uyan kayıt yok.'}</td></tr>`;
    const dipSatir = (ad, L, sinif) => `<tr class="${sinif || ''}">` + kol.map((c, i) => `<td>${i === (S ? 1 : 0) ? (sec.dipAd ? sec.dipAd(L, ad) : `${ad} · ${L.length}`) : c.top ? (c.dipH ? c.dipH(L) : tl(topla(L, c.top))) : ''}</td>`).join('') + '</tr>';
    t.tFoot.innerHTML = l.length && sec.dip !== false ? dipSatir('Toplam', l) + (secL.length ? dipSatir('Seçili', secL, 'secili') : '') : (secL.length ? dipSatir('Seçili', secL, 'secili') : '');
    t._kol = kol; t._id = id; t._liste = l; return l;
  }
  kok.addEventListener('click', e => {   // başlığa tıklayınca sıralama
    const th = e.target.closest('.tk-tablo thead th'); if (!th || th.classList.contains('sirasiz') || e.target.closest('input')) return; const t = th.closest('table'); if (!t._kol) return;
    const c = t._kol.find(x => x.k === th.dataset.k), sr = SIRA[t._id]; SIRA[t._id] = { k: c.k, y: sr && sr.k === c.k ? -sr.y : (c.t === 'm' ? 1 : -1) }; ciz();
  });

  // ---------- sekmeler ----------
  const musteriH = k => `<b>${esc(k.kod)}</b> ${esc(k.kisaAd)}<small>${esc(k.temsilci || '—')}${k.vadeSarti ? ' · ' + esc(k.vadeSarti) : ''}</small>`;
  const baglantilar = kod => `<a class="ikon" href="/Rapor117?sekme=yeni&kod=${encodeURIComponent(kod)}" title="Yeni tahsilat (bu müşteri)"><i class="fas fa-hand-holding-dollar"></i></a><a class="ikon" href="/Rapor117?sekme=adat&kod=${encodeURIComponent(kod)}" title="Adat hesapla (bu müşteri)"><i class="fas fa-chart-line"></i></a><a class="ikon" href="/Rapor20?bpCode=${encodeURIComponent(kod)}" target="_blank" rel="noopener" title="Cari Ekstre"><i class="fas fa-file-invoice"></i></a><a class="ikon" href="/CariKart/Musteri?kod=${encodeURIComponent(kod)}" data-kart="${esc(kod)}" title="Cari kart sayfası (pencerede; limit düzenleme, risk, faaliyetler)"><i class="fas fa-address-card"></i></a>`;
  // HIZLI ALAN DÜZENLEME (19 Kas 2026): Müşteri Cari Kartları › Vade hücresi → satır içi seçim + Kaydet; /CariKart/HizliGuncelle, kart günlüğüne yazılır, rapor yeniden okunur
  function hizliDuzenle(a) {
    const td = a.closest('td'), kod = a.dataset.kod, alan = a.dataset.hizli, k = cariBul(kod); if (!td || !k) return;
    const sec = alan === 'VadeSarti' ? `<select class="form-select form-select-sm" data-deger>${(K.vadeSartlari || []).map(v => `<option${v === k.vadeSarti ? ' selected' : ''}>${esc(v)}</option>`).join('')}</select>`
      : alan === 'VfOran' ? `<span class="tk-oran-giris">%<input class="form-control form-control-sm" data-deger inputmode="decimal" value="${n2(k.vf && k.vf.oran != null ? k.vf.oran : oran())}" title="aylık vade farkı oranı; 0 = varsayılan"></span>`
      : `<input class="form-control form-control-sm" data-deger value="${esc(k[alan.charAt(0).toLowerCase() + alan.slice(1)] || '')}">`;
    td.innerHTML = `<span class="tk-hizli-form" data-kod="${esc(kod)}" data-alan="${alan}">${sec}<button type="button" class="tk-dgm ana" data-hizli-kaydet title="Kaydet"><i class="fas fa-check"></i></button><button type="button" class="tk-dgm" data-hizli-vazgec title="Vazgeç"><i class="fas fa-xmark"></i></button></span>`;
    const el = $('[data-deger]', td); el.focus(); el.select && el.select(); el.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); hizliKaydet(td.querySelector('.tk-hizli-form')); } if (e.key === 'Escape') ciz(); });
  }
  const sayi = v => parseFloat(String(v ?? '').replace(/\./g, '').replace(',', '.')) || 0;
  async function oranKaydet(kodlar, oranDeger) {
    const d = await post('/Tahsilat/OranKaydet', { Kodlar: kodlar, Oran: oranDeger > 0 ? oranDeger : null }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    mesaj(d.success ? 'success' : 'danger', '<i class="fas fa-percent me-1"></i>' + esc(d.message)); if (d.success) await yukle(); else ciz();
  }
  async function hizliKaydet(f) {
    if (f.dataset.alan === 'VfOran') { await oranKaydet([f.dataset.kod], sayi($('[data-deger]', f).value)); return; }
    const d = await post('/CariKart/HizliGuncelle', { Kod: f.dataset.kod, Alan: f.dataset.alan, Deger: $('[data-deger]', f).value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    mesaj(d.success ? 'success' : 'danger', '<i class="fas fa-pen me-1"></i>' + esc(d.message)); if (d.success) await yukle(); else ciz();
  }
  // CARİ KART PENCERESİ (19 Kas 2026, kullanıcı: "satıra tıklayınca Müşteri Cari Kartları'ndaki gibi sayfa açılsın, limit düzeltmelerini buradan yapabilelim"): kart sayfası pencerede (iframe); kapanınca rapor yeniden okunur
  const kPen = $('#tkKart');
  function kartAc(kod) { const k = cariBul(kod); const url = '/CariKart/Musteri?kod=' + encodeURIComponent(kod); $('#tkKartAd').textContent = `${kod} · ${k ? k.kisaAd : ''} · Cari Kartı`; $('#tkKartAc').href = url; const fr = $('#tkKartIframe'); fr.src = url + '&detay=1'; kPen.hidden = false; kPen.dataset.kod = kod; }   // detay=1: pencerede yalnız kart detayı (liste yok; kullanıcı 19 Kas: "tek ekran yap, sol kısım yinelenmesin")
  function kartKapat() { if (kPen.hidden) return; kPen.hidden = true; $('#tkKartIframe').src = 'about:blank'; kirli = true; yukle(); }   // kartta limit / bilgi değişmiş olabilir
  kPen.addEventListener('click', e => { if (e.target === kPen || e.target.closest('[data-kart-kapat]')) kartKapat(); });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !kPen.hidden) kartKapat(); });
  const hizli = (id, html) => { const el = $('#tkIslem-' + id); if (el) el.innerHTML = html; };

  function cizCari() {
    const l = cariler();
    tablo('cari', [
      M('sirket', 'Şirket', k => k.sirket), M('musteri', 'Müşteri', k => k.kod, { h: musteriH }),
      // 19 Kas 2026 (kullanıcı: "Bakiye ile Açık Toplam arasındaki fark ne? mizan bakiyesini sütun olarak getir"): Bakiye = cari hesabın (120) mizan bakiyesi; Açık Toplam = açık faturaların kalanı;
      // fark = faturalarla kapatılmamış ödeme / avans (ayrı sütun). Dövizli görünümde tutarlar belge para birimiyle (TL ipucunda).
      P('gecmis', 'Gecikmiş\u00A0(₺)', k => k.gecmis, { h: k => k.gecmis > 0 ? `${kovaLink(k, 'gecmis', `<span class="gec">${tutarH(k.gecmis, k.kalemler.filter(x => x.gun < 0))}</span>`)}<small>${k.gecmisAdet} fatura · ${k.maxGecikme} gün</small>` : tire }),
      P('buHafta', 'Bu Hafta\u00A0(₺)', k => k.buHafta, { h: k => k.buHafta ? kovaLink(k, 'buHafta', tutarH(k.buHafta, k.kalemler.filter(x => x.gun >= 0 && x.gun <= 7))) : tire }), P('g8_30', '8–30 Gün\u00A0(₺)', k => k.g8_30, { h: k => k.g8_30 ? kovaLink(k, 'g8_30', tutarH(k.g8_30, k.kalemler.filter(x => x.gun >= 8 && x.gun <= 30))) : tire }),
      P('g31_60', '31–60 Gün\u00A0(₺)', k => k.g31_60, { h: k => k.g31_60 ? kovaLink(k, 'g31_60', tutarH(k.g31_60, k.kalemler.filter(x => x.gun >= 31 && x.gun <= 60))) : tire }), P('g60', '60+ Gün\u00A0(₺)', k => k.g60, { h: k => k.g60 ? kovaLink(k, 'g60', tutarH(k.g60, k.kalemler.filter(x => x.gun >= 61))) : tire }),
      // BAKİYE MUTABAKATI (19 Kas 2026, kullanıcı: "sütunları topladığımda bakiyeye ulaşmalıyım, her şeyi kalem kalem görebilmeliyim"):
      //   Açık Toplam + Fatura Dışı Borç − Kapatılmamış Ödeme = Bakiye; dört hücre de tıklanınca kalem kalem pencere (#tkKalem); bakiyenin altında muhasebe uyumu rozeti (#tkUyum)
      P('acikToplam', 'Açık Toplam\u00A0(₺)', k => k.acikToplam, { h: k => kalemLink(k, 'acik', k.acikToplam ? `<b>${tutarH(k.acikToplam, k.kalemler)}</b><small>${kalemOzet(k)}</small>` : tire), ac: 'Açık faturaların kalanı (bugünkü kurla TL): satış faturaları + vade / kur farkı faturaları. Tıklayınca kalemler.' }),
      P('fdb', '+ Fatura Dışı Borç\u00A0(₺)', k => fdbTop(k), { h: k => { const v = fdbTop(k), l = fdbListe(k); return kalemLink(k, 'fdb', v ? `${tutarH(v, l, x => x.tutar)}<small>${[...new Set(l.map(x => fdbTipAd(x.tip)))].join(' · ')}</small>` : tire); }, ac: 'Fatura olmayan borç hareketleri: kur farkı değerlemesi (muhasebe virmanı, yalnız R), devir, iade çek vb. Bakiyeye girer, açık fatura listesinde görünmez. Tıklayınca kalemler.' }),
      P('kapanmamis', '− Kapatılmamış Ödeme / Avans\u00A0(₺)', k => kapanmamis(k), { h: k => { const v = kapanmamis(k); return kalemLink(k, 'odeme', v > 0.5 ? `<span class="uy">${tl(v)}</span><small>${odemeListe(k).length} ödeme kapama bekliyor</small>` : v < -0.5 ? `<span class="gec">${tl(v)}</span><small>fazla kapama · ödeme kaydı yok</small>` : tire); }, ac: 'Müşteriden alınmış ama faturalarla eşlenmemiş ödeme ve avanslar (açık toplam + fatura dışı borç − bakiye). Eksi değer: fatura kapatılmış ama karşılığında ödeme hareketi yok (fazla kapama). Yeni Tahsilat › Otomatik Kapat ile faturalara dağıtılır. Tıklayınca kalemler.' }),
      // 19 Kas 2026 (kullanıcı): Bakiye ve Muhasebe Uyumu ayrı sütun; tutara, "Uyumsuz" rozetine ve farka tıklayınca Bakiye Karşılaştırması penceresi; "kalem kalem" bağlantısı mutabakat penceresi
      P('bakiye', '= Bakiye\u00A0(₺)', k => bakiyeSec(k), { h: k => `<a href="#" class="tk-uyum" data-uyum="${esc(k.kod)}" title="Tıklayınca Bakiye Karşılaştırması (muhasebe kaynaklarıyla)">${DV && k.vf.mizanDoviz && !F.tip.length ? `<b title="${esc(tl(bakiyeSec(k)))}">${esc(bolunmez(k.vf.mizanDoviz))}</b>` : `<b>${tl(bakiyeSec(k))}</b>`}</a><small>${kalemLink(k, 'ozet', 'kalem kalem')}</small>`, ac: 'Cari hesap bakiyesi (tüm borç − alacak, TL karşılığı) = Açık Toplam + Fatura Dışı Borç − Kapatılmamış Ödeme. Tutara tıklayınca Bakiye Karşılaştırması; "kalem kalem" ile mutabakat denklemi.' }),
      M('uyum', 'Muhasebe Uyumu', k => (k.uyum || {}).durum || '', { h: k => uyumRozet(k), ac: 'Bu raporun bakiyesi ↔ muhasebe hesap ekstresi (120.01.xxx), ± 0,50\u00A0₺. Rozete ya da farka tıklayınca Cari Ekstre Raporu, Hesap Ekstresi ve Mizan dahil bütün kaynaklar kalem kalem.' }),
      P('cek', 'Çek / Senet\u00A0(₺)', k => k.cek, { h: k => k.cek ? `${kovaLink(k, 'cek', tl(k.cek))}<small>${k.cekAdet} belge</small>` : tire, ac: 'Portföydeki (vadesi bekleyen) çek ve senetler. Tutara tıklayınca belgeler kalem kalem.' }),
      // 19 Kas 2026 (kullanıcı: "açık toplam + çek / senet bu iki sütunu topla, Toplam Risk sütunu ekle, Çek / Senet'ten sonra gelsin")
      P('toplamRisk', 'Toplam Risk\u00A0(₺)', k => (k.acikToplam || 0) + (k.cek || 0), { h: k => (k.acikToplam || k.cek) ? kovaLink(k, 'risk', `<b>${tl((k.acikToplam || 0) + (k.cek || 0))}</b>`) : tire, ac: 'Açık Toplam + Çek / Senet: müşterideki toplam alacak riski. Tutara tıklayınca açık faturalar ve çek / senetler.' }),
      // 19 Kas 2026 (kullanıcı): yalnız tutar + son tahsilat tarihi; tıklayınca Tahsilatlar sekmesi bu müşteri / dönemle açılır
      P('son30', 'Son 30 Gün Tahsilat\u00A0(₺)', k => k.son30Tahsilat, { h: k => `${k.son30Tahsilat ? `<a href="#" class="tk-link ok" data-tahliste="${esc(k.kod)}" data-dn="son30" title="Tahsilatlar › bu müşteri · son 30 gün">${tl(k.son30Tahsilat)}</a>` : tire}<small>${k.sonTahsilat ? `<a href="#" class="tk-link" data-tahliste="${esc(k.kod)}" data-dn="tumu" title="Tahsilatlar › bu müşteri · tüm tarihler">${esc(k.sonTahsilat)}</a>` : 'tahsilat yok'}</small>` }),
      M('risk', 'Risk', k => k.risk, { h: k => `${riskRozet(k.risk)}<small>${esc(k.limitDurum || '')}</small>` }),
      { k: 'islem', ad: 'İşlem', t: 'm', v: () => '', h: k => baglantilar(k.kod), sinif: 'islem', sirasiz: 1 }
    ], l, { sira: { k: 'gecmis', y: -1 }, satir: k => `class="tik" data-cari="${esc(k.kod)}" title="${esc(k.riskNeden || '')} · tıklayınca açık faturaları"`, bos: 'Süzgece uyan müşteri yok.' });
    hizli('cari', '');
  }
  // MÜŞTERİ CARİ KARTLARI (19 Kas 2026, kullanıcı: "Müşteri Cari Kartları sekmesini getir, filtreleme tahsilat raporundaki tarzda"): /CariKart/Musteri listesinin sütunları, aynı süzgeç çubuğuyla
  function cizKartlar() {
    const l = cariler(), kt = k => k.kart || {};
    tablo('kartlar', [
      M('kod', 'Cari Kodu', k => k.kod, { h: k => `<b>${esc(k.kod)}</b><small>${esc(k.sirket)}</small>` }), M('ad', 'Kısa Ad / Resmi Ünvan', k => k.kisaAd, { h: k => `<b>${esc(k.kisaAd)}</b><small>${esc(k.resmiUnvan || '')}</small>` }),
      M('vkn', 'VKN / Vergi Dairesi', k => kt(k).vergiNo, { h: k => kt(k).vergiNo ? `${esc(kt(k).vergiNo)}<small>${esc(kt(k).vergiDairesi || '')}</small>` : tire }),
      M('yer', 'Bölge · Ülke · Şehir', k => k.bolge, { h: k => `${esc(k.bolge || '')}<small>${[k.ulke, kt(k).sehir].filter(Boolean).map(esc).join(' / ')}</small>` }),
      M('sinif', 'Cari Sınıfı', k => k.sinif), M('iliski', 'İlişki', k => k.iliski, { h: k => rozet(k.iliski || '—', k.iliski === 'Grup Dışı' ? 'bilgi' : 'bekle') }),
      M('rol', 'Rol · Eş Cari', k => kt(k).rol, { h: k => `${esc(kt(k).rol || '—')}${kt(k).esCari ? `<small>${esc(kt(k).esCari)}</small>` : ''}` }),
      M('faaliyet', 'Ana Faaliyet', k => kt(k).anaFaaliyet), M('temsilci', 'Temsilci', k => k.temsilci),
      // 19 Kas 2026 (kullanıcı: "vadeye tıkladığımda kolaylıkla düzenleyebileyim"): hücre tıklanınca satır içi seçim kutusu; /CariKart/HizliGuncelle
      M('vade', 'Vade', k => k.vadeSarti, { sinif: 'hizli', h: k => `<a href="#" class="tk-hizli" data-hizli="VadeSarti" data-kod="${esc(k.kod)}" title="Tıklayınca vade şartını değiştir">${esc(k.vadeSarti || '—')} <i class="fas fa-pen"></i></a>` }),
      P('limit', 'Kredi Limiti\u00A0(₺)', k => kt(k).limit, { h: k => kt(k).limitTur === 'Yok' ? rozet('Limit Yok', 'bilgi') : `${tl(kt(k).limit)}<small>${esc(kt(k).limitTur)}</small>` }),
      { k: 'kullanim', ad: 'Limit Kullanımı', t: 'p', v: k => kt(k).limitYuzde == null ? -1 : kt(k).limitYuzde, h: k => kt(k).limitTur === 'Yok' ? tire : `${limitRozet(kt(k).limitDurum)}<small>${tl(kt(k).limitRisk)}${kt(k).limitYuzde != null ? ' · %\u00A0' + Number(kt(k).limitYuzde).toLocaleString(TR) : ''}</small>` },
      P('bakiye', 'Bakiye\u00A0(₺)', k => k.vf.mizanBakiye, { h: k => `<b>${tutarH(k.vf.mizanBakiye, [])}</b>${k.vf.mizanDoviz ? `<small>${esc(bolunmez(k.vf.mizanDoviz))}</small>` : ''}` }),
      P('gecmis', 'Vadesi Geçmiş\u00A0(₺)', k => k.gecmis, { h: k => k.gecmis > 0 ? `<span class="gec">${tl(k.gecmis)}</span><small>${k.gecmisAdet} fatura · ${k.maxGecikme} gün</small>` : tire }),
      M('risk', 'Risk', k => k.risk, { h: k => riskRozet(k.risk) }), M('durum', 'Durum', k => kt(k).durum, { h: k => rozet(kt(k).durum || '—', kt(k).durum === 'Aktif' ? 'ok' : 'bilgi') }),
      M('iletisim', 'Yetkili · Telefon · E-posta', k => kt(k).yetkili, { h: k => `${esc(kt(k).yetkili || '—')}<small>${[kt(k).telefon, kt(k).eposta].filter(Boolean).map(esc).join(' · ')}</small>` }),
      { k: 'islem', ad: 'İşlem', t: 'm', v: () => '', sinif: 'islem', sirasiz: 1, h: k => `<a class="ikon" href="/CariKart/Musteri?kod=${encodeURIComponent(k.kod)}" data-kart="${esc(k.kod)}" title="Cari kart sayfası (pencerede)"><i class="fas fa-address-card"></i></a><a class="ikon" href="#" data-ilt="${esc(k.kod)}" title="Müşteri iletişimi"><i class="fab fa-whatsapp"></i></a>${baglantilar(k.kod)}` }
    ], l, { sira: { k: 'kod', y: 1 }, satir: k => `class="tik" data-kart="${esc(k.kod)}" title="Tıklayınca cari kart sayfası (limit, risk, faaliyetler, talepler)"`, bos: 'Süzgece uyan müşteri kartı yok.' });
    hizli('kartlar', `<span class="not">${l.length} kart · ${l.filter(k => kt(k).durum === 'Aktif').length} aktif · ${l.filter(k => kt(k).limitDurum === 'Limit Aşıldı').length} limit aşımı</span><a class="tk-dgm" href="/CariKart/Musteri" target="_blank" rel="noopener"><i class="fas fa-address-card"></i>Müşteri Cari Kartları Ekranı</a>`);
  }
  const kalemler = l => l.flatMap(k => k.kalemler.map(x => Object.assign({ kod: k.kod, kisaAd: k.kisaAd, temsilci: k.temsilci, vadeSarti: k.vadeSarti, sirket: k.sirket, gecikme: x.gun < 0 ? -x.gun : 0, vf: x.gun < 0 ? Math.round(x.tl * oran() / 100 / 30 * -x.gun * 100) / 100 : 0 }, x)));
  // 19 Kas 2026 (kullanıcı: "Açık Faturalar'a tek tıkla vadesi geçmiş / vadesi gelecek gibi süzgeç"): vade durumu kovaları — Tümü · Vadesi Geçmiş (1–30 / 31–60 / 60+ gün) · Bu Hafta · Ay Sonuna Kadar · Sonraki Aylar; her kovada adet ve\u00A0₺
  const AV = { tumu: 'Tümü', gecmis: 'Vadesi Geçmiş', g30: '1–30 Gün Gecikmiş', g60: '31–60 Gün Gecikmiş', g90: '60+ Gün Gecikmiş', buHafta: 'Bu Hafta Vadesi Gelen', aySonu: 'Ay Sonuna Kadar', sonra: 'Sonraki Aylar' };
  function avUy(x, k) {
    const b = bugunD(), pazar = new Date(b); pazar.setDate(b.getDate() + (7 - ((b.getDay() + 6) % 7) - 1)); const haftaBit = iso(pazar), aySonu = K.aySonu || haftaBit;
    switch (k) { case 'gecmis': return x.gun < 0; case 'g30': return x.gun < 0 && x.gun >= -30; case 'g60': return x.gun < -30 && x.gun >= -60; case 'g90': return x.gun < -60; case 'buHafta': return x.gun >= 0 && x.vade <= haftaBit; case 'aySonu': return x.gun >= 0 && x.vade <= aySonu; case 'sonra': return x.vade > aySonu; default: return true; }
  }
  function cizAcik() {
    if (H.gecmis) { H.acikVade = 'gecmis'; H.gecmis = false; }   // Özet'teki "Gecikmiş" bağlantısı eski anahtarla gelir
    const tum = kalemler(cariler()), av = H.acikVade || 'tumu', l = tum.filter(x => avUy(x, av));
    tablo('acik', [   // 19 Kas 2026 gece (kullanıcı): Fatura Tarihi sütunu (Tahsilat ve İhracat Finansal Takibi aynı tablo)
      D('tarih', 'Fatura Tarihi', x => x.tarih, { h: x => `${trh(x.tarih)}<small>${x.tarih ? Math.round((bugunD() - new Date(x.tarih + 'T00:00:00')) / 86400000) + ' gün' : ''}</small>`, ac: 'Faturanın kesildiği tarih; altında faturanın yaşı (bugün − fatura tarihi).' }), D('vade', 'Vade', x => x.vade), M('sirket', 'Şirket', x => x.sirket), M('musteri', 'Müşteri', x => x.kod, { h: musteriH }), M('belge', 'Belge No', x => x.belge, { h: x => belgeLink(x.kod, x.belge) }), M('tip', 'İşlem Tipi', x => x.islemTipi),
      { k: 'kalan', ad: 'Kalan (Belge Para Birimi)', t: 'p', v: x => x.kalan, h: x => `${n2(x.kalan)}\u00A0${esc(x.pb)}` }, P('tl', 'Kalan\u00A0(₺)', x => x.tl, { h: x => `<b>${tl(x.tl)}</b>` }),
      { k: 'gun', ad: 'Gecikme (Gün)', t: 'p', v: x => -x.gun, h: x => x.gun < 0 ? `<span class="gec">${-x.gun} Gün Gecikti</span>` : x.gun === 0 ? 'Bugün' : `${x.gun} Gün Kaldı` },
      P('vf', 'Bugüne Vade Farkı\u00A0(₺)', x => x.vf, { ac: `Bugün tahsil edilirse hesaplanacak vade farkı: kalan × aylık % ${n2(oran())} ÷ 30 × gecikme günü (KDV hariç). Fatura, tahsilat işlenince Vade Farkı sekmesinden kesilir.` })
    ], l, { sira: { k: 'vade', y: 1 }, bos: av === 'tumu' ? 'Açık fatura yok.' : `${AV[av]} kovasında açık fatura yok.` });
    const kova = (k, kisa, sinif) => { const kl = tum.filter(x => avUy(x, k)); return `<button type="button" class="tk-dgm tk-av${sinif ? ' ' + sinif : ''}${av === k ? ' acik' : ''}" data-av="${k}" title="${AV[k]} · ${kl.length} fatura · ${tl(topla(kl, x => x.tl))}">${kisa}<small>${kl.length} · ${tl2(topla(kl, x => x.tl))}</small></button>`; };
    const gecmisAcik = ['gecmis', 'g30', 'g60', 'g90'].includes(av);
    hizli('acik', `<span class="tk-av-grup">${kova('tumu', 'Tümü')}${kova('gecmis', 'Vadesi Geçmiş', 'gec')}${gecmisAcik ? `<span class="tk-av-alt">${kova('g30', '1–30 gün', 'gec')}${kova('g60', '31–60 gün', 'gec')}${kova('g90', '60+ gün', 'gec')}</span>` : ''}${kova('buHafta', 'Bu Hafta', 'yakin')}${kova('aySonu', 'Ay Sonuna Kadar', 'yakin')}${kova('sonra', 'Sonraki Aylar')}</span>`);
  }
  function cizHafta() {
    const kal = kalemler(cariler()), kovalar = [{ ad: 'Gecikmiş', l: kal.filter(x => x.gun < 0), aralik: trh(K.bugun) + ' öncesi' }], bugun = new Date(K.bugun + 'T00:00:00');
    const gunTr = d => { const x = new Date(bugun); x.setDate(x.getDate() + d); return x.toLocaleDateString(TR); };
    for (let h = 0; h < 8; h++) kovalar.push({ ad: h === 0 ? 'Bu Hafta' : `${h + 1}. Hafta`, l: kal.filter(x => x.gun >= h * 7 && x.gun <= h * 7 + 6), aralik: `${gunTr(h * 7)} – ${gunTr(h * 7 + 6)}` });
    kovalar.push({ ad: '8 Haftadan Sonra', l: kal.filter(x => x.gun > 55), aralik: gunTr(56) + ' sonrası' });
    kovalar.forEach((k, i) => { k.i = i; k.tutar = topla(k.l, x => x.tl); k.cari = new Set(k.l.map(x => x.kod)).size; }); HAFTA = kovalar; if (haftaSec >= kovalar.length) haftaSec = 0;
    tablo('hafta', [M('ad', 'Hafta', k => k.i, { h: k => `<b>${k.ad}</b>`, sirasiz: 1 }), M('aralik', 'Tarih Aralığı', k => k.aralik, { sirasiz: 1 }), { k: 'adet', ad: 'Fatura', t: 'p', v: k => k.l.length, h: k => k.l.length || tire, top: k => k.l.length, dipH: l => topla(l, k => k.l.length), sirasiz: 1 },
      { k: 'cari', ad: 'Müşteri', t: 'p', v: k => k.cari, h: k => k.cari || tire, sirasiz: 1 }, P('tutar', 'Beklenen\u00A0(₺)', k => k.tutar, { h: k => `<b class="${k.i === 0 ? 'gec' : ''}">${tutarH(k.tutar, k.l)}</b>`, sirasiz: 1 })],
      kovalar, { satir: k => `class="tik${k.i === haftaSec ? ' secili' : ''}" data-hafta="${k.i}"` });
    const s = kovalar[haftaSec]; $('#tkHaftaNot').textContent = `${s.ad} · ${s.l.length} fatura · ${tl(s.tutar)}`;
    tablo('haftaDetay', [D('vade', 'Vade', x => x.vade, { h: x => `${trh(x.vade)}${x.gun < 0 ? `<small class="gec">${-x.gun} gün gecikme</small>` : ''}` }), M('musteri', 'Müşteri', x => x.kod, { h: x => `<b>${esc(x.kod)}</b> ${esc(x.kisaAd)}<small>${esc(x.sirket)}</small>` }), M('belge', 'Belge No', x => x.belge, { h: x => belgeLink(x.kod, x.belge) }),
      { k: 'kalan', ad: 'Kalan', t: 'p', v: x => x.kalan, h: x => `${n2(x.kalan)}\u00A0${esc(x.pb)}` }, P('tl', 'Kalan\u00A0(₺)', x => x.tl)], s.l, { sira: { k: 'vade', y: 1 }, bos: 'Bu haftada fatura yok.' });
  }
  // müşteri önerisi: gönderen adıyla ortak kelimesi en çok olan müşteri (şirket ekleri atılır)
  const ATLA = new Set(['a.ş.', 'aş', 'a.ş', 'ltd', 'ltd.', 'şti', 'şti.', 'san', 'san.', 'tic', 'tic.', 've', 'sanayi', 'ticaret', 'limited', 'şirketi', 'anonim']);
  const kelimeler = s => kucuk(s).replace(/[.,()\-\/]/g, ' ').split(/\s+/).filter(w => w.length > 2 && !ATLA.has(w));
  function oneriler(h) { const w = kelimeler(h.karsiAd + ' ' + (h.aciklama || '')); if (!w.length) return []; return (B.musteriler || []).map(m => ({ m, p: kelimeler(m.ad + ' ' + (m.unvan || '')).filter(x => w.includes(x)).length + (m.sirket === h.sirket ? 0.5 : 0) })).filter(x => x.p >= 1).sort((a, b) => b.p - a.p).slice(0, 5).map(x => x.m); }
  function cizBanka() {
    const tum = bankalar(), l = tum.filter(h => !H.bankaAcik || bankaAcik(h)).map(h => Object.assign(h, { _on: bankaAcik(h) ? oneriler(h)[0] : null }));
    tablo('banka', [
      D('tarih', 'Tarih', h => h.tarih, { h: h => trh(h.tarih) + (YENI.has(h.id) ? `<small>${rozet('Yeni', 'bekle')}</small>` : '') }), M('sirket', 'Şirket', h => h.sirket), M('banka', 'Banka', h => h.banka, { h: h => `${esc(h.banka)}<small>${belgeLink2(h.cariKod || '', h.no)}</small>` }),
      M('gonderen', 'Gönderen', h => h.karsiAd, { h: h => `<b>${esc(h.karsiAd || '—')}</b><small>${esc(h.islemTuru || '')}</small>` }), M('aciklama', 'Açıklama · Referans', h => h.aciklama, { h: h => `${esc(h.aciklama || '')}<small>${esc(h.referans || '')}</small>` }),
      P('tutar', 'Tutar\u00A0(₺)', h => h.tutar, { h: h => `<b>${tl(h.tutar)}</b>${h.pb !== 'TL' ? `<small>${n2(h.dovizTutar)}\u00A0${esc(h.pb)} · kur ${Number(h.kur).toLocaleString(TR, { maximumFractionDigits: 4 })}</small>` : ''}` }),
      M('musteri', 'Müşteri', h => h.cariAd || '', { h: h => bankaAcik(h) ? (h._on ? `<small>öneri</small>${esc(h._on.ad)}` : tire) : `<b>${esc(h.cariKod || '')}</b> ${esc(h.cariAd || '')}<small>${esc(h.onaylayan || '')}${h.onayTarihi ? ' · ' + esc(h.onayTarihi) : ''}</small>` }),
      M('durum', 'Durum', h => h.durum, { h: h => bankaAcik(h) ? rozet('Müşteri Bekliyor', 'bekle') : rozet('Müşteri Seçildi', 'ok') }),
      M('tahsilat', 'Tahsilat Kaydı', h => h.tahsilatNo || '', { h: h => h.tahsilatNo ? `<a href="#" data-tahsilat="${esc(h.tahsilatNo)}" title="Tahsilatlar sekmesinde aç"><b>${esc(h.tahsilatNo)}</b></a><small>${esc(h.tahsilatDurum || '')}</small>` : tire }),
      { k: 'islem', ad: 'İşlem', t: 'm', v: () => '', sinif: 'islem', sirasiz: 1, h: h => bankaAcik(h) ? `<button type="button" class="tk-dgm ana" data-musteri="${h.id}"><i class="fas fa-user-check"></i>Müşteri Seç</button>` : h.tahsilatNo || !h.musteriVar ? '' : `<button type="button" class="tk-dgm" data-taslak="${h.id}" title="Bu hareket için taslak tahsilat aç"><i class="fas fa-plus"></i>Taslak Tahsilat</button>` }
    ], l, { sira: { k: 'tarih', y: -1 }, satir: h => YENI.has(h.id) ? 'class="yeni"' : '', bos: H.bankaAcik ? 'Müşterisi seçilmemiş banka girişi yok.' : 'Banka girişi yok.' });
    bankaGorulduYaz();
    hizli('banka', `<span class="not">${tum.filter(bankaAcik).length} hareket müşteri bekliyor · son 90 gün</span><button type="button" class="tk-dgm${H.bankaAcik ? ' acik' : ''}" data-h="bankaAcik" title="Yalnız müşterisi seçilmemiş girişler">Yalnız Müşteri Bekleyenler</button>`);
  }
  const vfKdv = t => Math.round(t.vadeFarki * (Number(K.kdvOran) || 0)) / 100;
  function cizVade() {
    const tum = gecikmeli().filter(t => donemUy(t.tarihIso)), l = tum.filter(t => H.vadeDurum === 'tumu' || (H.vadeDurum === 'kesildi' ? vfDurum(t) === 'Kesildi' : vfDurum(t) === 'Kesilmedi')), kesilebilir = l.filter(t => vfDurum(t) === 'Kesilmedi');
    [...SECILI].forEach(no => { if (!kesilebilir.some(t => t.no === no)) SECILI.delete(no); });
    tablo('vade', [
      { k: 'sec', ad: `<input type="checkbox" id="tkVadeHepsi" title="Listedeki kesilmemişlerin hepsini seç"${kesilebilir.length && kesilebilir.every(t => SECILI.has(t.no)) ? ' checked' : ''}>`, t: 'm', v: () => '', sirasiz: 1, h: t => vfDurum(t) !== 'Kesilmedi' ? '' : `<input type="checkbox" data-sec="${esc(t.no)}"${SECILI.has(t.no) ? ' checked' : ''}>` },
      M('no', 'Tahsilat No', t => t.no, { h: t => `<a href="#" data-tahsilat="${esc(t.no)}" title="Tahsilatlar sekmesinde aç"><b>${esc(t.no)}</b></a>` }), D('tarih', 'Tahsilat Tarihi', t => t.tarihIso), M('sirket', 'Şirket', t => t.sirket),
      M('musteri', 'Müşteri', t => t.cariKodu, { h: t => `<b>${esc(t.cariKodu)}</b> ${esc(t.cariAd)}` }), M('yontem', 'Yöntem', t => t.yontem), P('tutarTl', 'Tahsilat\u00A0(₺)', t => t.tutarTl),
      { k: 'fatura', ad: 'Geciken Fatura', t: 'p', v: t => new Set(t.dagitim.filter(d => d.gecikmeGun > 0).map(d => d.belgeNo)).size, h: t => new Set(t.dagitim.filter(d => d.gecikmeGun > 0).map(d => d.belgeNo)).size },
      P('gecTutar', 'Geç Ödenen\u00A0(₺)', t => t.dagitim.filter(d => d.gecikmeGun > 0).reduce((s, d) => s + (d.tutarTl || 0), 0)),
      { k: 'gun', ad: 'En Uzun Gecikme (Gün)', t: 'p', v: t => Math.max(0, ...t.dagitim.map(d => d.gecikmeGun || 0)), h: t => Math.max(0, ...t.dagitim.map(d => d.gecikmeGun || 0)) },
      P('vadeFarki', 'Vade Farkı\u00A0(₺)', t => t.vadeFarki, { h: t => `<b>${tl2(t.vadeFarki)}</b>`, dipH: x => tl2(topla(x, t => t.vadeFarki)) }), P('kdv', `KDV\u00A0%\u00A0${Number(K.kdvOran) || 0}\u00A0(₺)`, vfKdv, { h: t => tl2(vfKdv(t)), dipH: x => tl2(topla(x, vfKdv)) }),
      P('toplam', 'Fatura Toplamı\u00A0(₺)', t => t.vadeFarki + vfKdv(t), { h: t => `<b>${tl2(t.vadeFarki + vfKdv(t))}</b>`, dipH: x => tl2(topla(x, t => t.vadeFarki + vfKdv(t))) }),
      M('durum', 'Vade Farkı Durumu', vfDurum, { h: t => { const d = vfDurum(t); return d === 'Kesildi' ? `${rozet('Kesildi', 'ok')} ${belgeLink(t.cariKodu, t.vadeFarkiFaturaNo)}` : d === 'Kesilmedi' ? rozet('Kesilmedi', 'hata') : `<span title="Gecikme tolerans günü içinde ya da bu günler daha önce (ay sonu / robot) faturalandı">${rozet('Fark Yok', 'bilgi')}</span>`; } }),
      { k: 'islem', ad: 'İşlem', t: 'm', v: () => '', sinif: 'islem', sirasiz: 1, h: t => vfDurum(t) !== 'Kesilmedi' ? '' : `<button type="button" class="tk-dgm" data-kes="${esc(t.no)}"><i class="fas fa-file-invoice"></i>Fatura Kes</button>` }
    ], l, { sira: { k: 'tarih', y: -1 }, bos: H.vadeDurum === 'kesilmedi' ? 'Vade farkı kesilmemiş gecikmeli tahsilat yok.' : H.vadeDurum === 'kesildi' ? 'Vade farkı kesilmiş gecikmeli tahsilat yok.' : 'Vadesinden sonra ödeme alınan tahsilat yok.', secimsiz: true, secili: t => SECILI.has(t.no) });
    const sec = kesilebilir.filter(t => SECILI.has(t.no)), nK = tum.filter(t => vfDurum(t) === 'Kesildi').length, nKm = tum.filter(t => vfDurum(t) === 'Kesilmedi').length;
    const vd = (k, ad, n) => `<button type="button" class="tk-dgm${H.vadeDurum === k ? ' acik' : ''}" data-vd="${k}">${ad}${n == null ? '' : ` <small>${n}</small>`}</button>`;
    hizli('vade', `<span class="not" title="Vadesinden sonra ödeme alınan tahsilatlar. Vade farkı = kapanan tutar × kur × aylık oran / 30 × gecikme günü; Yeni Tahsilat'ta 'Vade Farkı Faturasını Da Kes' açıkken tahsilatla birlikte kesilir, kapalıysa burada Kesilmedi olarak bekler ve buradan kesilir. Ay sonu / robot kesimi Vade Farkı Takibi'nde">${tum.length} gecikmeli tahsilat · ${donemMetni()} · aylık % ${n2(oran())}</span>${vd('tumu', 'Tümü', tum.length)}${vd('kesildi', 'Vade Farkı Kesilmiş', nK)}${vd('kesilmedi', 'Kesilmemiş', nKm)}<button type="button" class="tk-dgm ana" id="tkVadeKes" title="Seçilen tahsilatların her biri için vade farkı faturası kes"${sec.length ? '' : ' disabled'}><i class="fas fa-file-invoice-dollar"></i>Seçilenlere Fatura Kes${sec.length ? ` (${sec.length} · ${tl2(topla(sec, t => t.vadeFarki + vfKdv(t)))})` : ''}</button>`);
  }
  // ---------- GENEL PENCERE (#tkPen): kapama önerisi, tahsilat hareketleri ----------
  const pPen = $('#tkPen');
  function penAc(ad, alt, govde, altSerit) { if (!pPen) return; $('#tkPAd').innerHTML = ad; $('#tkPAlt').innerHTML = alt || ''; $('#tkPGovde').innerHTML = govde; $('#tkPAltSerit').innerHTML = altSerit || ''; pPen.hidden = false; }
  function penKapat() { if (pPen) pPen.hidden = true; }
  if (pPen) { pPen.addEventListener('click', e => { if (e.target === pPen || e.target.closest('[data-p-kapat]')) penKapat(); }); document.addEventListener('keydown', e => { if (e.key === 'Escape' && !pPen.hidden) penKapat(); }); }
  // Aylık Süreç "Bu Ay Alınan Tahsilat", Alacak Takibi "Son 30 Gün", Vade Farkı Takibi "Son Ödeme" tutarına tıklayınca: cari hesaptaki tahsilat hareketleri (dönem) — 19 Kas 2026
  async function odemelerAc(kod, dn) {
    const b = bugunD(); let bas = '', bit = '', ad = 'tüm tarihler';
    if (dn === 'buAy') { bas = iso(new Date(b.getFullYear(), b.getMonth(), 1)); bit = iso(new Date(b.getFullYear(), b.getMonth() + 1, 0)); ad = 'bu ay'; } else if (dn === 'son30') { bas = iso(new Date(b.getFullYear(), b.getMonth(), b.getDate() - 30)); bit = iso(b); ad = 'son 30 gün'; }
    const d = await fetch(`/Tahsilat/CariOdemeler?kod=${encodeURIComponent(kod)}&bas=${bas}&bit=${bit}`).then(r => r.json()).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (!d.success) { mesaj('danger', esc(d.message)); return; }
    const satir = d.hareketler.map(h => `<tr><td>${esc(h.tarih)}</td><td>${rozet(h.tip, h.tip === 'Çek' || h.tip === 'Senet' ? 'bekle' : 'ok')}</td><td>${belgeLink2(d.kod, h.belgeNo)}${h.tahsilatNo ? `<br><a href="#" data-tahsilat="${esc(h.tahsilatNo)}" title="Tahsilat kaydını aç"><small>${esc(h.tahsilatNo)}</small></a>` : ''}</td><td>${esc(h.vade || '—')}</td><td class="s">${esc(h.islemTipi)}</td><td class="e">${n2(h.tutar)}\u00A0${esc(pbAd(h.paraBirimi))}</td><td class="e"><b>${tl(h.tl)}</b></td><td>${rozet(h.durum, h.durum === 'Kapandı' ? 'ok' : h.durum === 'Vade Bekliyor' ? 'bilgi' : 'bekle')}${h.kalan > 0.005 ? `<small>açık ${n2(h.kalan)}\u00A0${esc(pbAd(h.paraBirimi))}</small>` : ''}</td><td><small>${esc(h.aciklama || '')}</small></td></tr>`).join('') || '<tr><td colspan="9" class="na-bos">Bu dönemde tahsilat hareketi yok.</td></tr>';
    penAc(`<i class="fas fa-hand-holding-dollar me-1"></i>${esc(d.kod)} · ${esc(d.kisaAd)} · Tahsilat Hareketleri`, `${ad}${d.bas ? ` · ${esc(d.bas)} – ${esc(d.bit)}` : ''} · ${d.adet} hareket · ${tl(d.toplamTl)} · cari hesap (havale, çek, senet, nakit)`,
      `<div class="table-responsive"><table class="na-tablo tk-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Tip</th><th>Belge</th><th>Vade</th><th class="s">X/R</th><th class="e">Tutar</th><th class="e">₺</th><th>Durum</th><th>Açıklama</th></tr></thead><tbody>${satir}</tbody><tfoot><tr><td colspan="6">Toplam · ${d.adet}</td><td class="e">${tl(d.toplamTl)}</td><td colspan="2"></td></tr></tfoot></table></div>`,
      `<a class="tk-dgm" href="/Rapor20?bpCode=${encodeURIComponent(d.kod)}" target="_blank" rel="noopener"><i class="fas fa-file-invoice"></i>Cari Ekstre</a><a class="tk-dgm" href="/Rapor117?sekme=yeni&kod=${encodeURIComponent(d.kod)}"><i class="fas fa-plus"></i>Tahsilat Al</a>`);
  }
  // ---------- KAPAMA ROBOTU (19 Kas 2026, kullanıcı: "filtreleyerek ya da cariyi arayarak toplu veya tek cari bazlı mutabakat kapama robotu") ----------
  //   Satır = müşteri: eşleşmemiş ödemeler, açık faturalar, önerilen eşleşme (birebir + FIFO, aynı para birimi), son kapama. Üstteki süzgeç çubuğu ve arama kümeyi daraltır; "Seçilenleri Kapat" / "Listedekilerin Hepsini Kapat" / satırda "Kapat".
  const SECK = new Set();
  function cizKapama() {
    // 19 Kas 2026 (kullanıcı): süzgeç yoksa otomatik kapama gereken bütün cariler — cari rolü Sadece Müşteri + Çift Rol varsayılan (çubuktaki Cari Rolü süzgeci seçilince o geçerli)
    const rolVarsayilan = !(F.rol && F.rol.length), tum = cariler().filter(k => !rolVarsayilan || ['Sadece Müşteri', 'Çift Rol'].includes((k.suzgec || {}).rol || '')), l = tum.filter(k => !H.kapamaAcik || (k.kapama && k.kapama.oneri > 0)); [...SECK].forEach(kod => { if (!l.some(k => k.kod === kod)) SECK.delete(kod); });
    const kp = k => k.kapama || { oneri: 0, oneriTl: 0, birebir: 0, fifo: 0, odemeAdet: 0, odemeTl: 0, faturaAdet: 0, faturaTl: 0, sonKapama: '', sonKapamaKim: '', sonKapamaAdet: 0, sonKapamaTl: 0 };
    tablo('kapama', [
      { k: 'sec', ad: `<input type="checkbox" id="tkKapHepsi" title="Listedeki müşterilerin hepsini seç"${l.length && l.every(k => SECK.has(k.kod)) ? ' checked' : ''}>`, t: 'm', v: () => '', sirasiz: 1, h: k => `<input type="checkbox" data-seck="${esc(k.kod)}"${SECK.has(k.kod) ? ' checked' : ''}>` },
      M('musteri', 'Müşteri', k => k.kod, { h: k => `<b>${esc(k.kod)}</b> ${esc(k.kisaAd)}<small>${esc(sinifAd(k))} · ${esc(k.sirket)}</small>` }),
      P('bakiye', 'Bakiye\u00A0(₺)', k => k.bakiye, { h: k => `<b>${tl(k.bakiye)}</b>` }),
      P('fatura', 'Açık Fatura\u00A0(₺)', k => kp(k).faturaTl, { h: k => kp(k).faturaAdet ? `${tl(kp(k).faturaTl)}<small>${kp(k).faturaAdet} fatura</small>` : tire }),
      P('odeme', 'Eşleşmemiş Ödeme\u00A0(₺)', k => kp(k).odemeTl, { h: k => kp(k).odemeAdet ? `<span class="gec">${tl(kp(k).odemeTl)}</span><small>${kp(k).odemeAdet} ödeme · kapama bekliyor</small>` : tire, ac: 'Faturayla eşleştirilmemiş tahsilat / çek / senet (cari hesapta açık alacak).' }),
      P('oneri', 'Robot Önerisi\u00A0(₺)', k => kp(k).oneriTl, { h: k => kp(k).oneri ? `<a href="#" class="tk-link ok" data-kapama-oneri="${esc(k.kod)}" title="Eşleşme önerisini gör">${tl(kp(k).oneriTl)}</a><small>${kp(k).oneri} eşleşme · ${kp(k).birebir} birebir · ${kp(k).fifo} FIFO</small>` : `${rozet('Eşleşme Yok', 'bilgi')}<small>${kp(k).odemeAdet ? 'aynı para biriminde açık fatura yok' : 'eşleşmemiş ödeme yok'}</small>`, dipH: x => tl(topla(x, k => kp(k).oneriTl)), ac: 'Robot çalışırsa kapatacağı tutar: önce birebir tutar eşleşmesi, sonra en eski vadeden FIFO; yalnız aynı para biriminde.' }),
      P('son', 'Son Kapama\u00A0(₺)', k => kp(k).sonKapamaTl, { h: k => kp(k).sonKapama ? `${tl(kp(k).sonKapamaTl)}<small>${esc(kp(k).sonKapama.slice(0, 10).split('-').reverse().join('.'))} · ${kp(k).sonKapamaAdet} eşleşme · ${esc(kp(k).sonKapamaKim)}</small>` : tire, dipH: () => '' }),
      { k: 'islem', ad: 'İşlem', t: 'm', v: () => '', sinif: 'islem', sirasiz: 1, h: k => `${kp(k).oneri ? `<button type="button" class="tk-dgm ana" data-kapat-cari="${esc(k.kod)}" title="Bu caride robotu çalıştır (öneriyi uygula)"><i class="fas fa-robot"></i>Kapat</button>` : ''}<a class="ikon" href="#" data-kapama-oneri="${esc(k.kod)}" title="Eşleşmemiş ödemeler ve açık faturalar"><i class="fas fa-list-check"></i></a><a class="ikon" href="/Rapor64?kod=${encodeURIComponent(k.kod)}" target="_blank" rel="noopener" title="Otomatik Kapatma ekranı (elle eşleştirme)"><i class="fas fa-arrows-left-right"></i></a><a class="ikon" href="/Rapor20?bpCode=${encodeURIComponent(k.kod)}" target="_blank" rel="noopener" title="Cari Ekstre"><i class="fas fa-file-invoice"></i></a>` }
    ], l, { sira: { k: 'oneri', y: -1 }, bos: H.kapamaAcik ? 'Süzgece uyan müşterilerde kapatılacak eşleşme yok.' : 'Süzgece uyan müşteri yok.', secimsiz: true, secili: k => SECK.has(k.kod) });
    const sec = l.filter(k => SECK.has(k.kod)), secOneri = sec.filter(k => kp(k).oneri > 0), hepsi = l.filter(k => kp(k).oneri > 0);
    hizli('kapama', `<span class="not" title="Kapama robotu: eşleşmemiş ödemeleri açık faturalarla birebir + FIFO eşleştirip kapatır (aynı para birimi); üstteki süzgeç ve arama kümeyi daraltır">${tum.filter(k => kp(k).oneri > 0).length} müşteride ${tl(topla(tum, k => kp(k).oneriTl))} kapatılabilir · rol: ${rolVarsayilan ? 'Sadece Müşteri + Çift Rol (varsayılan)' : F.rol.join(' + ')}</span>`
      + `<button type="button" class="tk-dgm${H.kapamaAcik ? ' acik' : ''}" data-h="kapamaAcik" title="Yalnız kapatılacak eşleşmesi olan müşteriler">Yalnız Eşleşmesi Olanlar</button>`
      + `<button type="button" class="tk-dgm ana" id="tkKapamaSec" title="Seçilen müşterilerde robotu çalıştır"${secOneri.length ? '' : ' disabled'}><i class="fas fa-robot"></i>Seçilenleri Kapat${secOneri.length ? ` (${secOneri.length} · ${tl(topla(secOneri, k => kp(k).oneriTl))})` : ''}</button>`
      + `<button type="button" class="tk-dgm" id="tkKapamaHepsi" title="Listedeki (süzgece uyan) bütün müşterilerde robotu çalıştır"${hepsi.length ? '' : ' disabled'}><i class="fas fa-robot"></i>Listedekilerin Hepsini Kapat${hepsi.length ? ` (${hepsi.length} · ${tl(topla(hepsi, k => kp(k).oneriTl))})` : ''}</button>`);
  }
  async function kapamaOneriAc(kod) {
    const d = await fetch('/Tahsilat/KapamaOneri?kod=' + encodeURIComponent(kod)).then(r => r.json()).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (!d.success) { mesaj('danger', esc(d.message)); return; }
    const cift = d.ciftler.map(x => `<tr><td>${belgeLink(d.kod, x.faturaNo)}<small>${esc(x.faturaTarih)} · vade ${esc(x.vade)} · kalan ${n2(x.faturaKalan)}\u00A0${esc(pbAd(x.pb))}</small></td><td>${belgeLink2(d.kod, x.odemeNo)}<small>${esc(x.odemeTip)} · ${esc(x.odemeTarih)} · açık ${n2(x.odemeKalan)}\u00A0${esc(pbAd(x.pb))}</small></td><td class="e"><b>${n2(x.tutar)}\u00A0${esc(pbAd(x.pb))}</b><small>${tl(x.tl)}</small></td><td class="s ${x.gun > 0 ? 'gec' : ''}">${x.gun}</td><td>${rozet(x.yontem, x.yontem === 'Birebir' ? 'ok' : 'bilgi')}</td></tr>`).join('') || '<tr><td colspan="5" class="na-bos">Kapatılacak eşleşme yok.</td></tr>';
    const od = d.odemeler.map(h => `<tr><td>${esc(h.tarih)}</td><td>${esc(h.tip)}</td><td>${belgeLink2(d.kod, h.belgeNo)}</td><td class="e">${n2(h.kalan)}\u00A0${esc(pbAd(h.paraBirimi))}</td><td class="e">${tl(h.tl)}</td><td><small>${esc(h.aciklama || '')}</small></td></tr>`).join('') || '<tr><td colspan="6" class="na-bos">Eşleşmemiş ödeme yok.</td></tr>';
    const fa = d.faturalar.map(h => `<tr><td>${belgeLink(d.kod, h.belgeNo)}</td><td>${esc(h.tarih)}</td><td>${esc(h.vade)}</td><td class="s ${h.gun > 0 ? 'gec' : ''}">${h.gun}</td><td class="s">${esc(h.islemTipi)}</td><td class="e">${n2(h.kalan)}\u00A0${esc(pbAd(h.paraBirimi))}</td><td class="e">${tl(h.tl)}</td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Açık fatura yok.</td></tr>';
    const gec = d.gecmis.map(g => `<span class="rt-durum rt-bilgi" title="${esc(g.kullanici)}">${esc(g.zaman)} · ${g.adet} eşleşme · ${tl(g.tl)}</span>`).join(' ') || '<span class="text-muted">daha önce kapama yapılmadı</span>';
    penAc(`<i class="fas fa-robot me-1"></i>${esc(d.kod)} · ${esc(d.kisaAd)} · Kapama Robotu Önerisi`, `${d.ciftler.length} eşleşme · ${tl(d.ciftler.reduce((a, x) => a + x.tl, 0))} · önce birebir, sonra en eski vadeden FIFO · yalnız aynı para birimi`,
      `<div class="tk-iki"><div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-money-bill-wave me-1"></i>Eşleşmemiş Ödemeler (${d.odemeler.length})</span></div><div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo tk-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Tip</th><th>Belge</th><th class="e">Açık</th><th class="e">₺</th><th>Açıklama</th></tr></thead><tbody>${od}</tbody></table></div></div></div>
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-file-invoice me-1"></i>Açık Faturalar (${d.faturalar.length})</span></div><div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo tk-tablo" data-dinamik="kapali"><thead><tr><th>Belge</th><th>Tarih</th><th>Vade</th><th class="s">Gün</th><th class="s">X/R</th><th class="e">Kalan</th><th class="e">₺</th></tr></thead><tbody>${fa}</tbody></table></div></div></div></div>
       <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-link me-1"></i>Önerilen Eşleşmeler (${d.ciftler.length})</span><span class="not">Kapat ile uygulanır: fatura ve ödeme kalanları düşer</span></div><div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo tk-tablo" data-dinamik="kapali"><thead><tr><th>Fatura</th><th>Ödeme</th><th class="e">Kapanacak</th><th class="s">Gün</th><th>Yöntem</th></tr></thead><tbody>${cift}</tbody></table></div></div></div>
       <div class="na-mini"><b>Son kapamalar:</b> ${gec}</div>`,
      `${d.ciftler.length ? `<button type="button" class="tk-dgm ana" data-kapat-cari="${esc(d.kod)}"><i class="fas fa-robot"></i>Bu Cariyi Kapat (${d.ciftler.length} · ${tl(d.ciftler.reduce((a, x) => a + x.tl, 0))})</button>` : ''}<a class="tk-dgm" href="/Rapor64?kod=${encodeURIComponent(d.kod)}" target="_blank" rel="noopener"><i class="fas fa-arrows-left-right"></i>Elle Eşleştir (Otomatik Kapatma)</a>`);
  }
  async function kapamaCalistir(kodlar) {
    kodlar = (kodlar || []).filter(Boolean); if (!kodlar.length) { mesaj('warning', 'Müşteri seçin.'); return; }
    const adlar = kodlar.slice(0, 3).map(k => { const c = cariBul(k); return c ? `${c.kod} ${c.kisaAd}` : k; }).join(', ') + (kodlar.length > 3 ? ` … (+${kodlar.length - 3})` : '');
    if (!confirm(`Kapama robotu ${kodlar.length} caride çalışsın mı?\n${adlar}\n\nEşleşmemiş ödemeler açık faturalarla (birebir + FIFO) kapatılır; fatura ve ödeme kalanları düşer.`)) return;
    const d = await post('/Tahsilat/KapamaRobotu', { Kodlar: kodlar }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    mesaj(d.success && d.eslesme > 0 ? 'success' : d.success ? 'info' : 'danger', '<i class="fas fa-robot me-1"></i>' + esc(d.message)); penKapat(); SECK.clear(); if (d.success) await yukle();
  }
  // ---------- Vade Farkı Takibi (müşteri bazında) ----------
  const limitRozet = d => rozet(d || '—', d === 'Limit Aşıldı' ? 'hata' : d === 'Limite Yakın' ? 'bekle' : d === 'Limit Yok' ? 'bilgi' : 'ok');
  const sonrasiRozet = d => rozet(d, d === 'Ödeme Yaptı' ? 'ok' : d.includes('Satış') ? 'hata' : 'bekle');
  const mutRozet = d => rozet(d || 'Gönderilmedi', d === 'Mutabık' ? 'ok' : d === 'İtirazlı' ? 'hata' : d === 'Yanıt Bekliyor' ? 'bekle' : 'bilgi');
  const durdurOner = k => k.vf.limitDurum === 'Limit Aşıldı' || (k.vf.sonrasiDurum || '').includes('Satış');   // satış durdurma önerilen müşteri
  const durdurNeden = k => [k.vf.limitDurum === 'Limit Aşıldı' ? `Limit aşıldı: risk ${tl(k.vf.limitRisk)} / limit ${tl(k.vf.limit)}` : '', k.gecmis > 0 ? `vadesi geçmiş ${tl(k.gecmis)} (${k.maxGecikme} gün)` : '', (k.vf.sonrasiDurum || '').startsWith('Ödeme Yok') ? `${trh(k.vf.refTarih)} vade farkı faturasından sonra ödeme yok` : ''].filter(Boolean).join(' · ');
  const vfSuz = k => (!H.vfGecmis || k.gecmis > 0) && (!H.vfLimit || k.vf.limitDurum === 'Limit Aşıldı') && (!H.vfOdemeYok || (k.vf.sonrasiDurum || '').startsWith('Ödeme Yok'));
  function cizVfTakip() {
    const tum = cariler(), l = tum.filter(vfSuz); [...SECM].forEach(kod => { if (!l.some(k => k.kod === kod)) SECM.delete(kod); });
    tablo('vftakip', [
      { k: 'sec', ad: `<input type="checkbox" id="tkVfHepsi" title="Listedeki müşterilerin hepsini seç"${l.length && l.every(k => SECM.has(k.kod)) ? ' checked' : ''}>`, t: 'm', v: () => '', sirasiz: 1, h: k => `<input type="checkbox" data-secm="${esc(k.kod)}"${SECM.has(k.kod) ? ' checked' : ''}>` },
      M('musteri', 'Müşteri', k => k.kod, { h: k => `<b>${esc(k.kod)}</b> ${esc(k.kisaAd)}<small>${esc(sinifAd(k))}${k.iliski && k.iliski !== 'Grup Dışı' ? ' · ' + esc(k.iliski) : ''} · ${esc(k.sirket)}</small>` }),
      P('mizan', 'Toplam Bakiye · Mizan\u00A0(₺)', k => k.vf.mizanBakiye, { h: k => DV && k.vf.mizanDoviz ? `<b title="${esc(tl(k.vf.mizanBakiye))}">${esc(bolunmez(k.vf.mizanDoviz))}</b>` : `<b>${tl(k.vf.mizanBakiye)}</b>${k.vf.mizanDoviz ? `<small>${esc(bolunmez(k.vf.mizanDoviz))}</small>` : ''}`, ac: 'Müşterinin 120 hesabının mizan bakiyesi (borç − alacak, TL karşılığı); altında para birimi kırılımı.' }),
      P('gecmis', 'Vadesi Geçmiş\u00A0(₺)', k => k.gecmis, { h: k => k.gecmis > 0 ? `${kovaLink(k, 'gecmis', `<span class="gec">${tutarH(k.gecmis, k.kalemler.filter(x => x.gun < 0))}</span>`)}<small>${k.gecmisAdet} fatura · ${k.maxGecikme} gün</small>` : tire }),
      P('aySonu', 'Ay Sonuna Kadar Vadesi Gelecek\u00A0(₺)', k => k.vf.aySonuGelecek, { h: k => k.vf.aySonuGelecek ? tutarH(k.vf.aySonuGelecek, k.kalemler.filter(x => x.gun >= 0 && x.vade <= K.aySonu)) : tire, ac: `Bugünden ${trh(K.aySonu)} tarihine kadar vadesi gelecek açık faturalar.` }),
      // 19 Kas 2026 gece (kullanıcı): yurtdışı müşteride (dövizli tahsilat) ay sonu / robot kesimi yok → "Tahsilatta Kesilir" rozeti, bugüne doğan tutar bilgi
      P('aySonuVf', 'Kesilecek VF · Ay Sonu\u00A0(₺)', k => tahsilatta(k) ? k.vf.bugunVf : k.vf.aySonuVf, { h: k => tahsilatta(k) ? `${rozet('Tahsilatta Kesilir', 'bilgi')}<small>yurtdışı · ay sonu kesimi yok${k.vf.bugunVf > 0.005 ? ` · bugüne doğan ${tl2(k.vf.bugunVf)} (${k.vf.bugunVfAdet} fatura)` : ''}</small>` : k.vf.aySonuVf ? `<b>${tl2(k.vf.aySonuVf)}</b><small>bugün kesilse ${tl2(k.vf.bugunVf)} · ${k.vf.aySonuVfAdet} gecikmiş fatura</small>` : `${tire}<small>kesilecek vade farkı yok</small>`, dipH: x => tl2(topla(x, k => k.vf.aySonuVf)), ac: `${trh(K.aySonu)} günü otomatik kesilecek vade farkı (KDV hariç; o güne kadar ödeme olmazsa). Müşterinin oranı. Kapatılmamış ödeme / avans varsa önce virmanla açık faturalardan düşülür, vade farkı kalan üzerinden hesaplanır. YURTDIŞI müşteride (dövizli tahsilat) ay sonu / robot kesimi yapılmaz; vade farkı yalnız tahsilat işlenirken kesilir — hücrede bugüne doğan tutar bilgi olarak.` }),
      // 19 Kas 2026 (kullanıcı: "vade farkı oranı sütun olarak eklensin, kolaylıkla değiştirilebilsin"): müşteriye özel aylık oran; tıklayınca satır içi giriş; 0 = varsayılana dön
      { k: 'oran', ad: 'VF Oranı (% / Ay)', t: 'p', v: k => k.vf.oran, h: k => `<a href="#" class="tk-hizli" data-hizli="VfOran" data-kod="${esc(k.kod)}" title="Tıklayınca bu müşterinin aylık vade farkı oranını değiştir (0 = varsayılan % ${n2(oran())})">%\u00A0${n2(k.vf.oran)} <i class="fas fa-pen"></i></a><small>${k.vf.oranOzel ? 'müşteriye özel' : 'varsayılan'}</small>`, ac: 'Bu müşterinin aylık vade farkı oranı: ay sonu / robot kesimi, tahsilat kapaması ve adat bu oranla hesaplanır. Varsayılan genel oran; müşteriye özel oran kalıcıdır.' },
      // 19 Kas 2026 (öneri 5): KUR DEĞERLEMESİ (R) — R dövizli açık faturalarda muhasebe değerlemesi bekleyen kur farkı (bugünkü kur − baz); muhasebe dönem sonu virmanını atınca sıfırlanır; X değerlenmez
      { k: 'kurDeg', ad: 'Kur Değerlemesi · R\u00A0(₺)', t: 'p', v: k => (k.kur || {}).rFark || 0, h: k => { const q = k.kur; if (!q) return tire; const bekliyor = q.rAdet > 0 && Math.abs(q.rFark) > 0.5; if (!q.rAdet && !q.sonDegerleme) return `${tire}<small>${q.xAdet ? `${q.xAdet} X dövizli fatura · değerlenmez` : 'dövizli R fatura yok'}</small>`; return `${bekliyor ? `<b class="${q.rFark > 0 ? 'gec' : 'ok'}">${tl2(q.rFark)}</b>` : rozet('Değerleme Güncel', 'ok')}<small>${q.sonDegerleme ? `son ${esc(q.sonDegerleme)} · ${esc(q.sonDekont)}` : 'henüz değerleme yok'}${bekliyor ? ` · <b>Değerleme Bekliyor</b> (${q.rAdet} fatura)` : ''}${q.xAdet ? ` · X ${q.xAdet} fatura değerlenmez` : ''}</small>`; }, dipH: x => tl2(topla(x, k => (k.kur || {}).rFark || 0)), ac: 'R (resmî) dövizli açık faturalarda muhasebe değerlemesi bekleyen kur farkı: kalan × (bugünkü kur − baz kur; baz = son değerleme kuru ya da fatura kuru). Muhasebe dönem sonu virmanını (dekont) atınca sıfırlanır; rapor dekont atmaz. X faturalar değerlenmez, kur farkı tahsilatta faturaya döner.' },
      // 19 Kas 2026 (kullanıcı: "Kesilen Vade Farkı anlam karışıklığı yaratıyor"): başlık = daha önce kesilmiş vade farkı FATURALARININ toplamı; altında adet, son tarih, ödenmemiş kısım
      P('kesilen', 'Kesilmiş VF Faturaları · Toplam\u00A0(₺)', k => k.vf.kesilen, { h: k => k.vf.kesilenAdet ? `${tl2(k.vf.kesilen)}<small>${k.vf.kesilenAdet} fatura kesildi · son ${trh(k.vf.sonTarih)}</small>${k.vf.acik > 0.005 ? `<small class="gec">henüz ödenmemiş ${tl2(k.vf.acik)}</small>` : '<small class="ok">hepsi ödendi</small>'}` : `${tire}<small>hiç vade farkı faturası kesilmedi</small>`, dipH: x => tl2(topla(x, k => k.vf.kesilen)), ac: 'Bu müşteriye bugüne kadar kesilmiş vade farkı faturalarının toplamı (KDV dahil): kaç fatura, en son ne zaman, ne kadarı hâlâ ödenmedi. Kesilecek olan soldaki "Kesilecek VF · Ay Sonu" sütunundadır.' }),
      P('sonOdeme', 'Son Ödeme', k => k.vf.sonOdemeTl, { h: k => k.vf.sonOdeme ? `<a href="#" class="tk-link ok" data-tahliste="${esc(k.kod)}" data-dn="tumu" title="Tahsilatlar › bu müşteri (${k.vf.sonOdemeGun} gün önce)">${tl(k.vf.sonOdemeTl)}</a><small>${trh(k.vf.sonOdeme)}</small>` : rozet('Ödeme Yok', 'hata') }),
      M('sonrasi', 'Vade Farkı Sonrası', k => k.vf.sonrasiDurum, { h: k => k.vf.sonrasiDurum ? `${sonrasiRozet(k.vf.sonrasiDurum)}<small>${k.vf.sonrasiSatisAdet ? `${k.vf.sonrasiSatisAdet} satış · ${tl(k.vf.sonrasiSatis)}` : 'satış yok'}</small>` : tire, ac: 'Bugünden önce kesilen son vade farkı faturasından sonra müşteri ödeme yaptı mı, müşteriye fatura kesilmeye devam ediyor mu.' }),
      { k: 'limit', ad: 'Limit Kullanımı', t: 'p', v: k => k.vf.limitYuzde == null ? -1 : k.vf.limitYuzde, h: k => `${limitRozet(k.vf.limitDurum)}${k.vf.limitTur !== 'Yok' ? `<small>${tl(k.vf.limitRisk)} / ${tl(k.vf.limit)}${k.vf.limitYuzde != null ? ' · %\u00A0' + Number(k.vf.limitYuzde).toLocaleString(TR) : ''}</small>` : ''}` },
      M('satis', 'Satış', k => k.vf.satisDurdu ? 'Durduruldu' : 'Açık', { sinif: 'islem', h: k => k.vf.satisDurdu ? `${rozet('Satış Durduruldu', 'hata')}<small>${esc(k.vf.satisKim)} · ${esc(k.vf.satisZaman)}</small>` : durdurOner(k) ? `<button type="button" class="tk-dgm uyari" data-durdur="${esc(k.kod)}" title="Satış durdur uyarısı ver"><i class="fas fa-ban"></i>Satış Durdur</button>` : rozet('Açık', 'ok') }),
      M('mutabakat', 'Mutabakat', k => k.vf.mutabakat, { h: k => `${mutRozet(k.vf.mutabakat)}<small>${k.vf.mutabakatTarih ? esc(k.vf.mutabakatTarih) + ' · ' + esc(k.vf.mutabakatKanal) : k.vf.sonMutabik ? 'son mutabık ' + esc(k.vf.sonMutabik) : ''}</small>` }),
      { k: 'islem', ad: 'İşlem', t: 'm', v: () => '', sinif: 'islem', sirasiz: 1, h: k => `<a class="ikon" href="#" data-mut="${esc(k.kod)}" title="Müşteriye mutabakat gönder (${esc(KANAL)})"><i class="fas fa-envelope-circle-check"></i></a><a class="ikon" href="/Rapor117?sekme=yeni&kod=${encodeURIComponent(k.kod)}" title="Tahsilat al (bu müşteri)"><i class="fas fa-hand-holding-dollar"></i></a><a class="ikon" href="/Rapor20?bpCode=${encodeURIComponent(k.kod)}" target="_blank" rel="noopener" title="Cari Ekstre"><i class="fas fa-file-invoice"></i></a>` }
    ], l, { sira: { k: 'gecmis', y: -1 }, satir: k => `class="tik${k.vf.satisDurdu ? ' durdu' : ''}" data-detay="${esc(k.kod)}" title="Tıklayınca müşteri detayı"`, bos: 'Süzgece uyan müşteri yok.', secimsiz: true, secili: k => SECM.has(k.kod) });
    const sec = l.filter(k => SECM.has(k.kod)), vfSec = sec.filter(k => k.vf.bugunVf > 0.005 && !tahsilatta(k));   // yurtdışı: Şimdi Kes yok
    // 19 Kas 2026 (kullanıcı): yazının hemen sağında 1–12 ay rozetleri; o ay sonu vade farkı faturaları kesildiyse (robot / otomatik) yeşil; tıklayınca Kesilen Fark Faturaları o ayla açılır
    const ilkKesim = Math.min(...(K.vfAylar || []).filter(a => a.kesildi).map(a => a.ay), 99);   // ilk kesimden önceki aylar "kayıt yok" (kırmızı değil)
    const aylar = (K.vfAylar || []).map(a => `<button type="button" class="tk-ay${a.kesildi ? ' ok' : a.gecti && a.ay > ilkKesim ? ' yok' : ''}${a.buAy ? ' buay' : ''}" data-vfay="${a.bas}|${a.bit}" title="${esc(a.ad)}: ${a.kesildi ? `${a.adet} vade farkı faturası · ${a.musteri} müşteri · ${tl(a.tutar)} · son ${esc(a.sonTarih)}` : a.gecti ? (a.ay > ilkKesim ? 'ay sonu vade farkı faturası kesilmedi' : 'kayıt yok (ilk kesimden önce)') : a.buAy ? `bu ay · otomatik kesim ${trh(K.aySonu)}` : 'henüz gelmedi'}">${a.ay}</button>`).join('');
    hizli('vftakip', `<span class="not">otomatik kesim ${trh(K.aySonu)} · son kesim ${trh(K.sonOtomatikAySonu)}</span><span class="tk-aylar" title="Bu yıl ay sonu vade farkı faturaları: yeşil = kesildi, kırmızı = geçti ve kesilmedi, çerçeveli = bu ay">${aylar}</span>`
      + `<button type="button" class="tk-dgm${H.vfGecmis ? ' acik' : ''}" data-h="vfGecmis">Vadesi Geçmişler</button><button type="button" class="tk-dgm${H.vfLimit ? ' acik' : ''}" data-h="vfLimit">Limit Aşanlar</button><button type="button" class="tk-dgm${H.vfOdemeYok ? ' acik' : ''}" data-h="vfOdemeYok" title="Son vade farkı faturasından sonra ödeme yapmayanlar">Ödeme Yok</button>`
      + `<select id="tkKanal" title="Mutabakat gönderim kanalı">${['E-posta', 'WhatsApp', 'İkisi'].map(x => `<option${x === KANAL ? ' selected' : ''}>${x}</option>`).join('')}</select>`
      + `<button type="button" class="tk-dgm ana" id="tkMutGonder" title="Seçilen müşterilere doğrudan mutabakat gönder"${sec.length ? '' : ' disabled'}><i class="fas fa-envelope-circle-check"></i>Mutabakat Gönder${sec.length ? ` (${sec.length})` : ''}</button>`
      + `<button type="button" class="tk-dgm" id="tkVfSimdi" title="Ay sonunu beklemeden seçilen müşterilere bugüne kadar olan vade farkını kes (müşteri başına tek fatura)"${vfSec.length ? '' : ' disabled'}><i class="fas fa-file-invoice-dollar"></i>Şimdi Kes${vfSec.length ? ` (${vfSec.length} · ${tl2(topla(vfSec, k => k.vf.bugunVf))})` : ''}</button>`
      + `<span class="tk-oran-toplu" title="Seçilen müşterilerin aylık vade farkı oranını topluca değiştir (0 = varsayılan)"><i class="fas fa-percent"></i><input id="tkOranToplu" inputmode="decimal" placeholder="oran" value=""><button type="button" class="tk-dgm" id="tkOranUygula"${sec.length ? '' : ' disabled'}>Seçilenlere Oran${sec.length ? ` (${sec.length})` : ''}</button></span>`);
  }
  // ---------- Aylık Süreç: arama (2. – 3. hafta) → vade farkı robotu (son hafta) → ay sonu otomatik; görüşme notları ----------
  const gunAy = iso => { const p = String(iso || '').split('-'); return p.length === 3 ? `${p[2]}.${p[1]}` : ''; };
  const sonucRozet = s => rozet(s, s === 'Ödeme Yapıldı' ? 'ok' : s === 'Ödeme Sözü Alındı' ? 'bilgi' : s === 'İtiraz Var' || s === 'Ulaşılamadı' ? 'hata' : 'bekle');
  const sozYazi = n => n.sozTarihi ? `söz ${trh(n.sozTarihi)}${n.sozTutar ? ' · ' + tl(n.sozTutar) : ''}` : '';
  const kisalt = (s, n) => { s = String(s || ''); return s.length > n ? s.slice(0, n - 1) + '…' : s; };
  const aranacakMi = k => k.gecmis > 0 || k.vf.aySonuGelecek > 0;      // bu ay aranacak müşteri: vadesi geçmiş ya da ay sonuna kadar vadesi gelecek bakiyesi olan
  const tahsilatta = k => (k.vf || {}).vfKural === 'Tahsilatta';          // 19 Kas 2026 gece: yurtdışı müşteri — vade farkı yalnız tahsilat işlenirken (ay sonu / robot / elle kesim yok)
  const robotAday = k => k.vf.bugunVf > 0.005 && !k.takip.robotFatura && !tahsilatta(k);   // robotun fatura keseceği müşteri: ödenmemiş gecikmiş faturası olan, bu ay faturası kesilmemiş, yurtdışı değil
  function cizSurec() {
    const S = K.surec, tum = cariler(), aranacak = tum.filter(aranacakMi), arandi = aranacak.filter(k => k.takip.buAyNot > 0), aday = tum.filter(robotAday), dahil = aday.filter(k => !k.takip.haric), haric = tum.filter(k => k.takip.haric && !tahsilatta(k)), kesilen = tum.filter(k => k.takip.robotFatura), yd = tum.filter(k => tahsilatta(k) && k.vf.bugunVf > 0.005);
    const adim = (no, ad, tarih, bilgi, simdi, ek) => `<div class="${simdi ? 'simdi' : ''}"><span class="no">${no}</span><div class="govde"><div class="ad">${ad}<small>${tarih}</small></div><div class="bilgi">${bilgi}</div></div>${ek || ''}</div>`;
    $('#tkAdimlar').innerHTML = adim(1, 'Müşteri Araması', `${gunAy(S.aramaBas)} – ${trh(S.aramaBit)}`, `${arandi.length} / ${aranacak.length} müşteri arandı`, S.asama === 'Arama')
      + adim(2, 'Vade Farkı Robotu', `${gunAy(S.robotBas)} – ${trh(K.aySonu)}`, `${dahil.length} müşteri · ${tl2(topla(dahil, k => k.vf.bugunVf))}${haric.length ? ` · ${haric.length} robot dışı` : ''}${kesilen.length ? ` · ${kesilen.length} müşteriye kesildi` : ''}${yd.length ? ` · <span title="Yurtdışı müşteride vade farkı yalnız tahsilat işlenirken kesilir (dövizli tahsilat)">${yd.length} yurtdışı tahsilatta kesilir (${tl2(topla(yd, k => k.vf.bugunVf))})</span>` : ''}`, S.asama === 'Robot',
        `<button type="button" class="tk-dgm${S.asama === 'Robot' ? ' ana' : ''}" id="tkRobot" title="Ödeme yapmamış müşterilere bu ayın vade farkı faturasını kes (robot dışı işaretliler atlanır)"${dahil.length ? '' : ' disabled'}><i class="fas fa-robot"></i>Robotu Çalıştır</button>`)
      + adim(3, 'Ay Sonu Otomatik', trh(K.aySonu), '', false);
    const donemler = [...new Set([S.donem, ...K.notlar.map(n => n.donem)])].sort().reverse(), donem = ND && donemler.includes(ND) ? ND : S.donem;
    // 19 Kas 2026: müşteri iletişimi — Gelen Talepler görünümü (WhatsApp'tan gelen belge istekleri + mesajlar), otomatik yanıt anahtarı, satırda İletişim düğmesi (pencere #tkIlt)
    const bekleyenTalep = talepler().filter(t => t.durum === 'Bekliyor');
    const gorunum = `<button type="button" class="tk-dgm${SG === 'musteri' ? ' acik' : ''}" data-g="musteri">Müşteriler</button><button type="button" class="tk-dgm${SG === 'not' ? ' acik' : ''}" data-g="not">Görüşme Notları</button><button type="button" class="tk-dgm${SG === 'talep' ? ' acik' : ''}${bekleyenTalep.length ? ' bekliyor' : ''}" data-g="talep" title="WhatsApp / e-posta ile gelen belge talepleri, ödeme sözleri, itirazlar ve mesajlar"><i class="fab fa-whatsapp"></i>Gelen Talepler${bekleyenTalep.length ? ` <span class="n">${bekleyenTalep.length}</span>` : ''}</button><label class="tk-oto" title="Açıkken müşteri WhatsApp'tan ekstre / ödeme planı / mutabakat isteyince belge hemen gönderilir; kapalıyken Gelen Talepler'de bekler"><input type="checkbox" id="tkOto"${K.otoYanit ? ' checked' : ''}> Otomatik Yanıt</label>`;
    if (SG === 'talep') {
      const gelen = iletisimler().filter(i => i.yon === 'Gelen' && (!F.cari || i.cariKodu === F.cari));
      const l = [...talepler().filter(t => !F.cari || t.cariKodu === F.cari).map(t => ({ tip: 'talep', zaman: t.zaman, tarih: t.tarih, cariKodu: t.cariKodu, cariAd: t.cariAd, kanal: t.kanal, tur: t.tur, metin: t.metin, durum: t.durum, id: t.id, gonderim: t.gonderim })),
        ...gelen.map(i => ({ tip: 'gelen', zaman: i.zaman, tarih: i.tarih, cariKodu: i.cariKodu, cariAd: i.cariAd, kanal: i.kanal, tur: (i.icerik || []).join(', ') || 'Mesaj', metin: i.metin, durum: i.sozTutar ? `söz ${tl(i.sozTutar)}${i.sozTarihi ? ' · ' + trh(i.sozTarihi) : ''}` : '', id: i.id }))]
        .filter(x => { const c = cariBul(x.cariKodu); return (c ? suzUyar(c) : !suzAktif()) && araUyar(x.cariKodu, x.cariAd, x.metin, x.tur); });
      $('#tkSurecBaslik').textContent = 'Gelen Talepler ve Mesajlar · WhatsApp / E-posta';
      tablo('surec', [
        M('zaman', 'Zaman', x => x.tarih + x.zaman, { h: x => esc(x.zaman) }), M('musteri', 'Müşteri', x => x.cariKodu, { h: x => `<b>${esc(x.cariKodu)}</b> ${esc(x.cariAd)}` }), M('kanal', 'Kanal', x => x.kanal, { h: x => `<i class="${x.kanal === 'WhatsApp' ? 'fab fa-whatsapp' : 'fas fa-envelope'} me-1"></i>${esc(x.kanal)}` }),
        M('tur', 'Tür', x => x.tur, { h: x => x.tip === 'talep' ? rozet('Belge Talebi · ' + x.tur, 'bekle') : rozet(x.tur, x.tur.includes('İtiraz') ? 'hata' : x.tur.includes('Ödeme Sözü') ? 'ok' : 'bilgi') }),
        M('metin', 'Mesaj', x => x.metin, { sinif: 'metin', h: x => esc(x.metin) }),
        M('durum', 'Durum', x => x.durum, { h: x => x.tip === 'talep' ? `${rozet(x.durum, x.durum === 'Bekliyor' ? 'hata' : 'ok')}${x.gonderim ? `<small>${esc(x.gonderim)}</small>` : ''}` : (x.durum ? `<b>${esc(x.durum)}</b>` : rozet('Okundu', 'bilgi')) }),
        { k: 'islem', ad: 'İşlem', t: 'm', v: () => '', sinif: 'islem', sirasiz: 1, h: x => x.tip === 'talep' && x.durum === 'Bekliyor' ? `<button type="button" class="tk-dgm ana" data-ilt="${esc(x.cariKodu)}" data-talep="${x.id}" data-tur="${esc(x.tur)}"><i class="fas fa-paper-plane"></i>Gönder</button>` : `<button type="button" class="tk-dgm" data-ilt="${esc(x.cariKodu)}"><i class="fab fa-whatsapp"></i>İletişim</button>` }
      ], l, { sira: { k: 'zaman', y: -1 }, dip: false, satir: x => `class="tik${x.tip === 'talep' && x.durum === 'Bekliyor' ? ' bekliyor' : ''}" data-detay="${esc(x.cariKodu)}" title="Tıklayınca müşteri detayı"`, bos: 'Gelen talep ya da mesaj yok. Müşteri WhatsApp\'tan yazınca burada görünür; denemek için İletişim penceresindeki "Müşteri yanıtını simüle et" kullanılır.' });
      hizli('surec', `<span class="not">${bekleyenTalep.length} talep bekliyor · ${gelen.length} gelen mesaj</span>${gorunum}`);
      return;
    }
    if (SG === 'not') {
      const l = K.notlar.filter(n => n.donem === donem && (!F.cari || n.cariKodu === F.cari) && (() => { const c = cariBul(n.cariKodu); return c ? suzUyar(c) : !suzAktif(); })() && araUyar(n.cariKodu, n.cariAd, n.metin, n.gorusulen, n.sonuc, n.kullanici));
      $('#tkSurecBaslik').textContent = 'Görüşme Notları';
      tablo('surec', [
        M('zaman', 'Tarih', n => n.tarih + n.zaman, { h: n => esc(n.zaman) }), M('musteri', 'Müşteri', n => n.cariKodu, { h: n => `<b>${esc(n.cariKodu)}</b> ${esc(n.cariAd)}` }), M('kanal', 'Kanal', n => n.kanal), M('kisi', 'Görüşülen Kişi', n => n.gorusulen),
        M('sonuc', 'Sonuç', n => n.sonuc, { h: n => sonucRozet(n.sonuc) }), M('soz', 'Ödeme Sözü', n => n.sozTarihi, { h: n => n.sozTarihi ? `${trh(n.sozTarihi)}${n.sozTutar ? `<small>${tl(n.sozTutar)}</small>` : ''}` : tire }),
        M('metin', 'Görüşme Notu', n => n.metin, { sinif: 'metin', h: n => esc(n.metin) + (n.ornek ? '<span class="ornek">ÖRNEK</span>' : '') }), M('kim', 'Giren', n => n.kullanici),
        { k: 'islem', ad: 'İşlem', t: 'm', v: () => '', sinif: 'islem', sirasiz: 1, h: n => n.benim ? `<button type="button" class="tk-dgm" data-notsil="${n.id}" title="Notu sil"><i class="fas fa-trash"></i></button>` : '' }
      ], l, { sira: { k: 'zaman', y: -1 }, dip: false, satir: n => `class="tik" data-detay="${esc(n.cariKodu)}" title="Tıklayınca müşteri detayı"`, bos: 'Bu dönemde görüşme notu yok.' });
      hizli('surec', `<span class="not">${l.length} not</span><select id="tkDonemSec" title="Okunacak dönem">${donemler.map(d => `<option value="${d}"${d === donem ? ' selected' : ''}>${d.slice(5)}.${d.slice(0, 4)}</option>`).join('')}</select>${gorunum}`);
      return;
    }
    const l = tum.filter(k => (!H.surecGecmis || k.gecmis > 0) && (!H.surecAranmayan || (aranacakMi(k) && !k.takip.buAyNot)));
    $('#tkSurecBaslik').textContent = `Aylık Süreç · ${S.donemAd}`;
    const eskiAcik = k => (k.kalemler || []).reduce((e, x) => !e || x.tarih < e.tarih ? x : e, null);   // kalanı olan en eski tarihli fatura
    tablo('surec', [
      // 19 Kas 2026 (kullanıcı): sütun sırası Alacak Takibi mantığında — Vadesi Geçmiş · Ay Sonuna Kadar · Açık Toplam · Toplam Bakiye · Son Kesilen Fatura · robot · arama · görüşme · Bu Ay Alınan Tahsilat (en sonda)
      M('musteri', 'Müşteri', k => k.kod, { h: k => `<b>${esc(k.kod)}</b> ${esc(k.kisaAd)}<small>${esc(sinifAd(k))} · ${esc(k.sirket)}</small>` }),
      P('gecmis', 'Vadesi Geçmiş\u00A0(₺)', k => k.gecmis, { h: k => k.gecmis > 0 ? `${kovaLink(k, 'gecmis', `<span class="gec">${tutarH(k.gecmis, k.kalemler.filter(x => x.gun < 0))}</span>`)}<small>${k.gecmisAdet} fatura · ${k.maxGecikme} gün</small>` : tire, ac: 'Vadesi bugünden önce dolmuş açık faturaların kalanı (Alacak Takibi › Gecikmiş ile aynı).' }),
      P('aySonu', 'Ay Sonuna Kadar Vadesi Gelecek\u00A0(₺)', k => k.vf.aySonuGelecek, { h: k => k.vf.aySonuGelecek ? tutarH(k.vf.aySonuGelecek, k.kalemler.filter(x => x.gun >= 0 && x.vade <= K.aySonu)) : tire, ac: `Bugünden ${trh(K.aySonu)} tarihine kadar vadesi gelecek açık faturalar: bu ay tahsil edilmesi gereken tutar.` }),
      P('acikToplam', 'Açık Toplam\u00A0(₺)', k => k.acikToplam, { h: k => k.acikToplam ? `${tutarH(k.acikToplam, k.kalemler)}<small>${k.acikAdet} fatura</small>` : tire }),
      P('bakiye', 'Toplam Bakiye\u00A0(₺)', k => k.vf.mizanBakiye, { h: k => `<b>${tl(k.vf.mizanBakiye)}</b>${k.vf.mizanDoviz ? `<small>${esc(bolunmez(k.vf.mizanDoviz))}</small>` : ''}`, ac: 'Cari hesap bakiyesi (Alacak Takibi › Bakiye ile aynı).' }),
      // 20 Kas 2026 (kullanıcı: "Son Kesilen Faturanın soluna en eski açık fatura tarihi, altına da geçen gün veya gelecek gün sayısı … 04.10.2024 / 777 gün gibi"): kalanı olan en eski tarihli fatura; üstte fatura tarihi, altta bugüne göre gün (ileri tarihliyse "gün sonra"); tıklayınca fatura açılır
      M('eskiFatura', 'En Eski Açık Fatura', k => (eskiAcik(k) || {}).tarih || '', { h: k => { const f = eskiAcik(k); if (!f) return tire; const g = Math.round((new Date(K.bugun) - new Date(f.tarih)) / 86400000);
        return `<a href="#" class="tk-link" data-fat-ac="${esc(f.belge)}" data-kod="${esc(k.kod)}" title="${esc(f.belge)} · kalan ${n2(f.kalan)} ${esc(pbAd(f.pb))} · vade ${trh(f.vade)}${f.gun < 0 ? ` · ${-f.gun} gün gecikmiş` : ` · vadeye ${f.gun} gün`} — faturayı aç">${trh(f.tarih)}</a><small>${g >= 0 ? `${g} gün` : `${-g} gün sonra`}</small>`; }, ac: 'Kalanı olan (açık) en eski tarihli fatura: üstte fatura tarihi, altta fatura tarihinden bugüne geçen gün sayısı. Tıklayınca fatura detayı açılır.' }),
      // 19 Kas 2026 (kullanıcı): yalnız tarih; tıklayınca Fatura İncelemesi penceresi
      // 19 Kas 2026 (kullanıcı): üstte tutar, altta tarih — "Bu Ay Alınan Tahsilat" ile aynı düzen; tıklayınca Fatura İncelemesi
      P('sonFatura', 'Son Kesilen Fatura\u00A0(₺)', k => (k.sonFatura || {}).tl || 0, { h: k => k.sonFatura ? `<a href="#" class="tk-link" data-fat-ac="${esc(k.sonFatura.belge)}" data-kod="${esc(k.kod)}" title="${esc(k.sonFatura.belge)} · ${n2(k.sonFatura.tutar)}\u00A0${esc(pbAd(k.sonFatura.pb))}${k.sonFatura.kalan > 0.005 ? ' · açık' : ' · kapandı'} — faturayı aç">${k.sonFatura.pb && k.sonFatura.pb !== 'TL' && DV ? `${n2(k.sonFatura.tutar)}\u00A0${esc(pbAd(k.sonFatura.pb))}` : tl(k.sonFatura.tl)}</a><small>${trh(k.sonFatura.tarih)}</small>` : tire, dipH: () => '', ac: 'Müşteriye kesilen son satış faturası (vade / kur farkı faturaları hariç): üstte tutar, altta tarih; tıklayınca fatura detayı açılır.' }),
      M('arama', 'Arama', k => (k.takip.buAyNot ? '1' : aranacakMi(k) ? '0' : '2') + k.kod, { h: k => k.takip.buAyNot ? `${rozet('Arandı', 'ok')}<small>${esc(k.takip.buAySonNot.zaman.slice(0, 10))} · ${k.takip.buAyNot} not</small>` : aranacakMi(k) ? rozet('Aranmadı', 'bekle') : tire }),
      M('sonNot', 'Son Görüşme', k => k.takip.sonNot ? k.takip.sonNot.tarih : '', { sinif: 'metin', h: k => { const n = k.takip.sonNot; return n ? `${sonucRozet(n.sonuc)} ${n.sozTarihi ? `<b>${esc(sozYazi(n))}</b> ` : ''}${esc(kisalt(n.metin, 110))}<small>${esc(n.zaman)} · ${esc(n.kanal)}${n.gorusulen ? ' · ' + esc(n.gorusulen) : ''} · ${esc(n.kullanici)}</small>` : '<span class="text-muted">not yok</span>'; } }),
      P('robotVf', 'Robot Vade Farkı\u00A0(₺)', k => k.takip.robotFatura ? 0 : k.vf.bugunVf, { h: k => k.takip.robotFatura ? `${rozet('Kesildi', 'ok')}<small>${belgeLink(k.kod, k.takip.robotFatura)}</small>` : tahsilatta(k) ? (k.vf.bugunVf > 0.005 ? `${tl2(k.vf.bugunVf)}<small>yurtdışı · tahsilatta kesilir</small>` : tire) : k.vf.bugunVf > 0.005 ? `<b>${tl2(k.vf.bugunVf)}</b><small>ay sonu ${tl2(k.vf.aySonuVf)}</small>` : tire, dipH: x => tl2(topla(x.filter(k => robotAday(k) && !k.takip.haric), k => k.vf.bugunVf)), ac: 'Robot bugün çalışırsa kesilecek vade farkı (KDV hariç). Dip toplam: robota dahil müşteriler. Yurtdışı müşteri robota girmez; tutarı bilgi (tahsilat işlenirken kesilir).' }),
      M('robot', 'Robot', k => tahsilatta(k) ? 'Tahsilatta Kesilir' : k.takip.haric ? 'Robot Dışı' : 'Dahil', { sinif: 'islem', h: k => tahsilatta(k) ? `${rozet('Tahsilatta Kesilir', 'bilgi')}<small>yurtdışı · dövizli tahsilat</small>` : k.takip.haric ? `${rozet(k.takip.haricSurekli ? 'Robot Dışı · Sürekli' : 'Robot Dışı', 'bekle')}<small>${esc(kisalt(k.takip.haricNeden, 40))}</small><button type="button" class="tk-dgm" data-dahil="${esc(k.kod)}">Dahil Et</button>` : `${rozet('Dahil', 'ok')} <button type="button" class="tk-dgm" data-haric="${esc(k.kod)}" title="Bu ay robotun dışında bırak">Hariç Tut</button>` }),
      P('buAyOdeme', 'Bu Ay Alınan Tahsilat\u00A0(₺)', k => k.takip.buAyOdeme, { h: k => k.takip.buAyOdeme ? `<a href="#" class="tk-link ok" data-tahliste="${esc(k.kod)}" data-dn="buAy" title="${k.takip.buAyOdemeAdet} tahsilat — Tahsilatlar › bu müşteri · bu ay">${tl(k.takip.buAyOdeme)}</a><small>${trh(k.vf.sonOdeme)}</small>` : `${rozet('Bu Ay Tahsilat Yok', 'hata')}<small>${k.vf.sonOdeme ? `<a href="#" class="tk-link" data-tahliste="${esc(k.kod)}" data-dn="tumu" title="Tahsilatlar › bu müşteri">${trh(k.vf.sonOdeme)}</a>` : 'hiç tahsilat yok'}</small>`, ac: 'Bu ay içinde müşteriden alınan tahsilatlar (havale, çek, senet, nakit); altında son tahsilat tarihi. Tıklayınca Tahsilatlar sekmesi açılır.' }),
      { k: 'islem', ad: 'İşlem', t: 'm', v: () => '', sinif: 'islem', sirasiz: 1, h: k => { const bt = bekleyenTalep.filter(t => t.cariKodu === k.kod).length; return `<button type="button" class="tk-dgm ana" data-not="${esc(k.kod)}"><i class="fas fa-pen"></i>Not Ekle</button><button type="button" class="tk-dgm${bt ? ' bekliyor' : ''}" data-ilt="${esc(k.kod)}" title="WhatsApp / e-posta: mutabakat, cari ekstre, ödeme planı gönder; müşteri yanıtları"><i class="fab fa-whatsapp"></i>İletişim${bt ? ` <span class="n">${bt}</span>` : ''}</button>`; } }
    ], l, { sira: { k: 'gecmis', y: -1 }, satir: k => `class="tik" data-detay="${esc(k.kod)}" title="Tıklayınca müşteri detayı"`, bos: 'Süzgece uyan müşteri yok.' });
    hizli('surec', `<button type="button" class="tk-dgm${H.surecGecmis ? ' acik' : ''}" data-h="surecGecmis">Vadesi Geçmişler</button><button type="button" class="tk-dgm${H.surecAranmayan ? ' acik' : ''}" data-h="surecAranmayan" title="Bu ay görüşme notu girilmemiş, aranması gereken müşteriler">Aranmayanlar</button>${gorunum}`);
  }
  // ---------- görüşme notu penceresi ----------
  const nPencere = $('#tkNot'); let NK = '';   // not girilen müşteri kodu
  const notListeH = l => l.length ? l.map(n => `<div><div class="ust2"><b>${esc(n.zaman)}</b>${sonucRozet(n.sonuc)}<span>${esc(n.kanal)}${n.gorusulen ? ' · ' + esc(n.gorusulen) : ''}</span>${n.sozTarihi ? `<b>${esc(sozYazi(n))}</b>` : ''}<span>${esc(n.kullanici)}</span>${n.ornek ? '<span class="ornek">ÖRNEK</span>' : ''}${n.benim ? `<a href="#" data-notsil="${n.id}" title="Notu sil"><i class="fas fa-trash"></i></a>` : ''}</div>${esc(n.metin)}</div>`).join('') : '<div class="bos">Bu müşteri için görüşme notu yok.</div>';
  function notAc(kod) {
    const k = cariBul(kod); if (!k) return; NK = kod;
    $('#tkNAd').textContent = `Görüşme Notu · ${k.kod} ${k.kisaAd}`;
    $('#tkNAlt').textContent = [`vadesi geçmiş ${tl(k.gecmis)}`, `ay sonuna kadar ${tl(k.vf.aySonuGelecek)}`, k.vf.sonOdeme ? `son ödeme ${trh(k.vf.sonOdeme)}` : 'ödeme yok', k.temsilci ? 'temsilci ' + k.temsilci : ''].filter(Boolean).join(' · ');
    $('#tkNKanal').innerHTML = K.kanallar.map(x => `<option>${esc(x)}</option>`).join(''); $('#tkNSonuc').innerHTML = '<option value="">Seçin…</option>' + K.sonuclar.map(x => `<option>${esc(x)}</option>`).join('');
    $('#tkNKisi').value = k.yetkili || ''; $('#tkNSozTarih').value = ''; $('#tkNSozTutar').value = ''; $('#tkNMetin').value = ''; $('#tkNHata').textContent = '';
    $('#tkNListe').innerHTML = notListeH(K.notlar.filter(n => n.cariKodu === kod)); nPencere.hidden = false; $('#tkNMetin').focus();
  }
  const notKapat = () => { nPencere.hidden = true; NK = ''; };
  async function notKaydet() {
    const dg = $('#tkNKaydet'); dg.disabled = true;
    const d = await post('/Tahsilat/NotKaydet', { Kod: NK, Kanal: $('#tkNKanal').value, Gorusulen: $('#tkNKisi').value, Sonuc: $('#tkNSonuc').value, SozTarihi: $('#tkNSozTarih').value, SozTutar: Number($('#tkNSozTutar').value) || 0, Not: $('#tkNMetin').value }).catch(() => ({ success: false, message: 'Ağ hatası.' })); dg.disabled = false;
    if (!d.success) { $('#tkNHata').textContent = d.message || 'Kaydedilemedi.'; return; }
    notKapat(); islemSonu(d, 'fa-pen');
  }
  async function notSil(id) { if (!confirm('Görüşme notu silinsin mi?')) return; const d = await post('/Tahsilat/NotSil', { Id: id }).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (d.success && !nPencere.hidden) { const kod = NK; await islemSonu(d, 'fa-trash'); notAc(kod); } else islemSonu(d, 'fa-trash'); }
  nPencere.addEventListener('click', e => {
    if (e.target === nPencere || e.target.closest('[data-n="kapat"]')) { notKapat(); return; }
    const sl = e.target.closest('[data-notsil]'); if (sl) { e.preventDefault(); notSil(+sl.dataset.notsil); return; }
    if (e.target.closest('#tkNKaydet')) notKaydet();
  });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !nPencere.hidden) notKapat(); });
  // ---------- vade farkı robotu: robot dışı bırakma / çalıştırma ----------
  async function robotHaric(kod, haric, surekli) {
    const k = cariBul(kod); if (!k) return; let neden = '';
    if (haric) { neden = prompt(`${k.kod} ${k.kisaAd} ${surekli ? 'SÜREKLİ' : 'bu ay'} vade farkı robotunun dışında bırakılacak.\nNeden:`, k.takip.sonNot && k.takip.sonNot.sozTarihi ? `Ödeme sözü: ${trh(k.takip.sonNot.sozTarihi)}` : ''); if (neden === null) return; if (!neden.trim()) { mesaj('danger', 'Robot dışında bırakma nedenini yazın.'); return; } }
    islemSonu(await post('/Tahsilat/RobotHaric', { Kod: kod, Haric: haric, Neden: neden, Surekli: !!surekli }).catch(() => ({ success: false, message: 'Ağ hatası.' })), 'fa-robot');
  }
  async function robotCalistir() {
    const S = K.surec, aday = K.satirlar.filter(robotAday), dahil = aday.filter(k => !k.takip.haric), haric = aday.filter(k => k.takip.haric); if (!dahil.length) return;
    if (!confirm(`${S.asama !== 'Robot' ? `Ayın son haftası ${trh(S.robotBas)} tarihinde başlıyor; robot şimdi ERKEN çalıştırılacak.\n\n` : ''}Vade farkı robotu çalıştırılsın mı?\n\n${dahil.length} müşteriye fatura kesilecek · matrah ${tl2(topla(dahil, k => k.vf.bugunVf))} (KDV hariç)${haric.length ? `\nRobot dışı: ${haric.map(k => k.kisaAd).join(', ')}` : ''}\n\nMüşteri başına tek fatura kesilir ve carisine borç yazılır.`)) return;
    islemSonu(await post('/Tahsilat/RobotCalistir', {}).catch(() => ({ success: false, message: 'Ağ hatası.' })), 'fa-robot');
  }

  // ---------- müşteri iletişimi (19 Kas 2026): WhatsApp / e-posta ile mutabakat, cari ekstre, ödeme planı; gelen yanıtlar (ödeme sözü, itiraz, belge talebi) ----------
  // v2 (kullanıcı): (1) Kime Gönderilecek — kayıtlı kişiler son / sık kullanılan sırasıyla, biri seçilir; sağda Kayıtlı Kişiler (satır içi düzenleme) + "Yeni Kişi Ekle";
  //                 (2) içerik kartlarında "Önizle": PDF'ler pencerede açılır, Mutabakat bakiye + yanıt bağlantısı metniyle gösterilir; "Gönderilecek Mesaj" canlı önizleme;
  //                 (3) Yazışma Geçmişi WhatsApp ve E-posta ayrı sütunlarda (120.008 için örnek yazışma sunucudan, "Örnek" rozetiyle).
  const talepler = () => K.talepler || [], iletisimler = () => K.iletisimler || [];
  const iPencere = $('#tkIlt'); let IK = '', KISI = null, SECKISI = 0, onzZ = null;   // IK: açık müşteri · KISI: kişi paketi · SECKISI: seçili alıcı (kişi id)
  const ORNEK_YANIT = { soz: 'Merhaba, 1.500.000 TL havaleyi 25.11.2026 tarihinde yapacağız.', itiraz: 'İtiraz ediyoruz: son vade farkı faturası kayıtlarımızda yok, ödememiz hesabınıza işlenmemiş görünüyor.', ekstre: 'Güncel cari ekstremizi gönderir misiniz?', plan: 'Ödeme planı simülasyonunu paylaşabilir misiniz?', fatura: 'Açık faturaların listesini ve ödeme planını istiyoruz.', mutabik: 'Bakiyeyle mutabıkız, teşekkürler.' };
  const ICERIK = { 'Mutabakat': { ikon: 'fa-scale-balanced', a: 'Bakiye (döviz kırılımıyla) ve müşterinin "Mutabıkız / İtiraz" yanıtı vereceği bağlantı; e-mutabakat kaydı açılır' }, 'Cari Ekstre': { ikon: 'fa-file-invoice', a: 'Cari hesap ekstresi PDF: bütün hareketler, para birimi kırılımı, açık faturalar', pdf: 'EkstrePdf' }, 'Ödeme Planı · Tahsilat Simülasyonu': { ikon: 'fa-calculator', a: 'PDF: tahsilat özeti + ödeme kapanış simülasyonu (belge, vade, gün, kalan, vade farkı)', pdf: 'OdemePlaniPdf', tur: 'simulasyon' }, 'Ödeme Planı · Faturalar': { ikon: 'fa-list-ol', a: 'PDF: aylık adat dağılımı ve vade farkı + fatura bazında ekstre detayı', pdf: 'OdemePlaniPdf', tur: 'faturalar' } };
  const icerikUrl = x => { const c = ICERIK[x]; return c && c.pdf ? `/Tahsilat/${c.pdf}?kod=${encodeURIComponent(IK)}${c.tur ? '&tur=' + c.tur : ''}` : ''; };
  const gunOnce = iso => { if (!iso) return ''; const g = Math.round((bugunD() - new Date(iso.slice(0, 10) + 'T00:00:00')) / 86400000); return g <= 0 ? 'bugün' : g === 1 ? 'dün' : `${g} gün önce`; };
  function iltAc(kod, talepId, tur) {
    const k = cariBul(kod); if (!k) return; IK = kod; SECKISI = 0; const t = talepId ? talepler().find(x => x.id === +talepId) : null;
    $('#tkIAd').innerHTML = `<i class="fab fa-whatsapp me-1"></i>${esc(k.kod)} · ${esc(k.kisaAd)} · Müşteri İletişimi`;
    $('#tkIAlt').textContent = [k.resmiUnvan, k.sirket, `bakiye ${tl(k.vf.mizanBakiye)}`, k.gecmis > 0 ? `vadesi geçmiş ${tl(k.gecmis)}` : 'gecikme yok', k.vf.mutabakat && k.vf.mutabakat !== 'Gönderilmedi' ? 'e-mutabakat ' + k.vf.mutabakat : ''].filter(Boolean).join(' · ');
    const kartlar = Object.keys(ICERIK).map(x => `<div class="kart" data-icerik="${esc(x)}"><label><input type="checkbox" name="tkIIcerik" value="${esc(x)}"${(t && t.tur === x) || (tur && tur === x) ? ' checked' : ''}><i class="fas ${ICERIK[x].ikon}"></i><b>${esc(x)}</b></label><small>${esc(ICERIK[x].a)}</small><button type="button" class="tk-dgm" data-onizle="${esc(x)}"><i class="fas fa-eye"></i>Önizle</button></div>`).join('');
    $('#tkIGovde').innerHTML = `<div class="tk-ilt v2">
      <div class="tk-ilt-alici"><div class="b"><i class="fas fa-user-check me-1"></i>1 · Kime Gönderilecek <small>son kullanılan ve sık kullanılan önce · tıklayıp seçin</small></div><div class="liste" id="tkIAlici"><div class="bos">Yükleniyor…</div></div></div>
      <div class="tk-ilt-kisi"><div class="b"><i class="fas fa-address-book me-1"></i>Kayıtlı Kişiler <small>satırda düzenleyip Kaydet'e basın · ★ ana yetkili</small><button type="button" class="tk-dgm ana" id="tkIYeniKisi"><i class="fas fa-user-plus"></i>Yeni Kişi Ekle</button></div><div class="liste" id="tkIKisiler"><div class="bos">Yükleniyor…</div></div></div>
      <div class="tk-ilt-form">
        <div class="b"><i class="fas fa-paper-plane me-1"></i>2 · Ne Gönderilecek${t ? ` · <span class="rt-durum rt-bekle">Talep #${t.id} · ${esc(t.tur)} · ${esc(t.zaman)}</span>` : ''}</div>
        <div class="satir"><span class="e">Kanal</span><label><input type="checkbox" name="tkIKanal" value="WhatsApp" checked> <i class="fab fa-whatsapp"></i> WhatsApp</label><label><input type="checkbox" name="tkIKanal" value="E-posta"${t ? '' : ' checked'}> <i class="fas fa-envelope"></i> E-posta</label><small id="tkIKanalNot">ikisini de seçebilirsiniz</small></div>
        <div class="satir kartlar"><span class="e">İçerik</span><div class="kartlar">${kartlar}</div></div>
        <div class="satir"><span class="e">Ek Not</span><textarea id="tkIMesaj" rows="2" maxlength="2000" placeholder="İsteğe bağlı not (bakiye, bağlantılar ve yanıt yönergesi otomatik eklenir)…">${t ? `Talebiniz üzerine ${esc(t.tur)} ektedir.` : ''}</textarea></div>
        <div class="satir metin"><span class="e">Gönderilecek Mesaj</span><pre id="tkIMetinOnz" class="metin">…</pre></div>
        <div class="satir dugmeler"><button type="button" class="tk-dgm ana" id="tkIGonder"><i class="fas fa-paper-plane"></i>Gönder</button><span class="text-danger" id="tkIHata"></span></div>
        <div id="tkISonuc" class="sonuc" hidden></div>
      </div>
      <div class="tk-ilt-onz"><div class="b"><i class="fas fa-eye me-1"></i>Önizleme <small id="tkIOnzBaslik">içerik kartındaki Önizle ile belgeyi burada görün</small><a class="tk-dgm" id="tkIOnzAc" href="#" target="_blank" rel="noopener" hidden><i class="fas fa-arrow-up-right-from-square"></i>Yeni Sekmede Aç</a></div><div class="ic" id="tkIOnz"><div class="bos">Mutabakat kartında bakiye ve yanıt bağlantısı, PDF kartlarında belgenin kendisi gösterilir.</div></div></div>
      <div class="tk-ilt-gecmis"><div class="b"><i class="fas fa-comments me-1"></i>3 · Yazışma Geçmişi <small>WhatsApp ve e-posta ayrı; örnek yazışmalar "Örnek" rozetiyle</small></div>
        <div class="iki"><div class="kanal"><div class="bb wa"><i class="fab fa-whatsapp"></i> WhatsApp <span class="n" id="tkIGecmisWaN"></span></div><div class="liste" id="tkIGecmisWa"></div></div><div class="kanal"><div class="bb ml"><i class="fas fa-envelope"></i> E-posta <span class="n" id="tkIGecmisMlN"></span></div><div class="liste" id="tkIGecmisMl"></div></div></div></div>
      <div class="tk-ilt-sim">
        <div class="b"><i class="fas fa-reply me-1"></i>Müşteri yanıtını simüle et <small>(gerçek WhatsApp webhook'u aynı uca bağlanır: /Tahsilat/WhatsAppGelen)</small></div>
        <div class="satir"><span class="e">Örnek</span>${Object.entries({ soz: 'Ödeme Sözü', itiraz: 'İtiraz', ekstre: 'Ekstre İsteği', plan: 'Ödeme Planı İsteği', fatura: 'Fatura Listesi İsteği', mutabik: 'Mutabık' }).map(([a, b]) => `<button type="button" class="tk-dgm" data-ornek="${a}">${b}</button>`).join('')}</div>
        <div class="satir"><span class="e">Gelen mesaj</span><textarea id="tkISimMetin" rows="2" maxlength="1000" placeholder="Müşterinin WhatsApp'tan yazdığı metin…"></textarea><button type="button" class="tk-dgm" id="tkISimule"><i class="fab fa-whatsapp"></i>Gelen Mesajı İşle</button></div>
      </div>
    </div>`;
    iltGecmis(); kisiYukle(); iPencere.hidden = false; const mk = $('#tkIMesaj'); if (mk) mk.focus({ preventScroll: true });
  }
  // ---- alıcı seçimi: kayıtlı kişiler, son / sık kullanılan sırasıyla
  function aliciCiz() {
    const kap = $('#tkIAlici'); if (!kap || !KISI) return; const giden = iletisimler().filter(i => i.cariKodu === IK && i.yon === 'Giden');
    const l = (KISI.kisiler || []).map(k => { const g = giden.filter(i => i.kisiId === k.id); return Object.assign({ adet: g.length, son: g.length ? g.map(i => i.zamanIso || i.tarih).sort().reverse()[0] : '' }, k); }).sort((a, b) => (b.son || '').localeCompare(a.son || '') || b.adet - a.adet || (b.anaYetkili - a.anaYetkili));
    if (!l.length) { kap.innerHTML = '<div class="bos">Kayıtlı kişi yok — sağdan "Yeni Kişi Ekle".</div>'; return; }
    if (!SECKISI || !l.some(k => k.id === SECKISI)) SECKISI = (l.find(k => k.son) || l.find(k => k.anaYetkili) || l[0]).id;
    // satır düzeni (kullanıcı: "şablonlar karışmış, yazılar okunmuyor"): tek satırda seçim · ad · unvan · WhatsApp · e-posta · son / sık kullanım; uzun metin üç noktayla kısalır, tamamı ipucunda
    kap.innerHTML = `<div class="alici bas"><span></span><span>Kişi</span><span>Unvan</span><span><i class="fab fa-whatsapp"></i> WhatsApp</span><span><i class="fas fa-envelope"></i> E-posta</span><span>Kullanım</span></div>` + l.map(k => `<div class="alici${k.id === SECKISI ? ' sec' : ''}" data-alici="${k.id}" title="Tıklayınca alıcı olarak seçilir"><span class="sec-ikon"><i class="fas ${k.id === SECKISI ? 'fa-circle-check' : 'fa-circle'}"></i></span><span class="ad" title="${esc(k.adSoyad)}">${k.anaYetkili ? '<i class="fas fa-star ana" title="ana yetkili"></i>' : ''}<b>${esc(k.adSoyad)}</b></span><span class="unvan" title="${esc(k.unvan || '')}">${esc(k.unvan || '—')}</span><span class="${k.wa ? 'var' : 'yok'}" title="${esc(k.cepTelefonu || 'cep yok')}"><i class="fab fa-whatsapp"></i>${esc(k.cepTelefonu || 'cep yok')}</span><span class="${k.mail ? 'var' : 'yok'}" title="${esc(k.eposta || 'e-posta yok')}"><i class="fas fa-envelope"></i>${esc(k.eposta || 'e-posta yok')}</span><span class="kul">${k.son ? `${gunOnce(k.son)} · ${k.adet} gönderim` : 'henüz gönderim yok'}</span></div>`).join('');
    kanalUyumla(); metinOnizle();
  }
  /** seçili alıcıya göre kanal kutuları: cep yoksa WhatsApp, e-posta yoksa E-posta kapanır */
  function kanalUyumla() { const k = (KISI.kisiler || []).find(x => x.id === SECKISI); if (!k) return; const wa = $('input[name=tkIKanal][value="WhatsApp"]'), ml = $('input[name=tkIKanal][value="E-posta"]'); wa.disabled = !k.wa; ml.disabled = !k.mail; if (!k.wa) wa.checked = false; if (!k.mail) ml.checked = false; if (!wa.checked && !ml.checked) { if (k.wa) wa.checked = true; else if (k.mail) ml.checked = true; } $('#tkIKanalNot').textContent = `${esc(k.adSoyad)} → ${[k.wa ? 'WhatsApp ' + k.cepTelefonu : '', k.mail ? 'e-posta ' + k.eposta : ''].filter(Boolean).join(' · ') || 'iletişim bilgisi yok; Kayıtlı Kişiler\'den ekleyin'}`; }
  // ---- gönderilecek mesajın canlı önizlemesi ve içerik kartı önizlemesi
  function metinOnizle() {
    clearTimeout(onzZ); onzZ = setTimeout(async () => {
      const ic = $$('input[name=tkIIcerik]:checked').map(x => x.value).join('|'), mesaj = ($('#tkIMesaj') || {}).value || '';
      const d = await fetch(`/Tahsilat/IletisimOnizle?kod=${encodeURIComponent(IK)}&kisiId=${SECKISI}&icerik=${encodeURIComponent(ic)}&mesaj=${encodeURIComponent(mesaj)}`).then(r => r.json()).catch(() => null);
      const p = $('#tkIMetinOnz'); if (p && d && d.success) p.textContent = d.metin;
    }, 250);
  }
  async function icerikOnizle(x) {
    const kap = $('#tkIOnz'), ac = $('#tkIOnzAc'); $('#tkIOnzBaslik').textContent = x; const url = icerikUrl(x);
    if (url) { kap.innerHTML = `<iframe src="${esc(url)}#toolbar=0&view=FitH" title="${esc(x)}"></iframe>`; ac.href = url; ac.hidden = false; return; }
    ac.hidden = true; kap.innerHTML = '<div class="bos">Yükleniyor…</div>';
    const d = await fetch(`/Tahsilat/IletisimOnizle?kod=${encodeURIComponent(IK)}&kisiId=${SECKISI}&icerik=Mutabakat`).then(r => r.json()).catch(() => null); if (!d || !d.success) { kap.innerHTML = '<div class="bos">Önizleme alınamadı.</div>'; return; }
    const m = d.mutabakat; kap.innerHTML = `<div class="mut"><div class="satir"><b>Bakiye</b><span>${esc(m.bakiye)}</span></div><div class="satir"><b>Açık Fatura</b><span>${m.acikAdet} adet · ${esc(m.acikTl)}</span></div><div class="satir"><b>Yanıt Bağlantısı</b><span>${m.link ? `<a href="${esc(m.link)}" target="_blank" rel="noopener">${esc(m.link)}</a>` : '—'}</span></div><div class="satir"><b>Müşteriye Giden Mutabakat Metni</b></div><pre>${esc(m.sablon)}</pre><small>Müşteri bağlantıdan "Mutabıkız" ya da "İtiraz" seçer; sonuç Vade Farkı Takibi › Mutabakat sütununa ve yazışma geçmişine düşer.</small></div>`;
  }
  // ---- kayıtlı kişiler (satır içi düzenleme + yeni kişi)
  async function kisiYukle() { const d = await fetch('/Tahsilat/IletisimKisiler?kod=' + encodeURIComponent(IK)).then(r => r.json()).catch(() => null); KISI = d && d.success ? d.paket : { kisiler: [], waUygun: false, mailUygun: false }; kisiCiz(); aliciCiz(); }
  const kisiSatir = (k, yeni) => `<div class="kisi${yeni ? ' yeni' : ''}${k.anaYetkili ? ' ana' : ''}" data-id="${k.id || 0}">
      <label class="ana" title="Ana yetkili: varsayılan alıcı"><input type="radio" name="tkKAna" value="${k.id || 0}"${k.anaYetkili ? ' checked' : ''}><i class="fas fa-star"></i></label>
      <input class="ad" value="${esc(k.adSoyad || '')}" placeholder="Ad Soyad *" autocomplete="off"><input class="unvan" value="${esc(k.unvan || '')}" placeholder="Unvan" autocomplete="off">
      <span class="alan"><i class="fab fa-whatsapp ${k.wa ? 'var' : 'yok'}"></i><input class="cep" value="${esc(k.cepTelefonu || '')}" placeholder="05xx xxx xx xx" inputmode="tel" autocomplete="off"></span>
      <span class="alan"><i class="fas fa-envelope ${k.mail ? 'var' : 'yok'}"></i><input class="eposta" value="${esc(k.eposta || '')}" placeholder="e-posta" inputmode="email" autocomplete="off"></span>
      <button type="button" class="tk-dgm ana" data-kisi-kaydet title="Kaydet"><i class="fas fa-floppy-disk"></i>${yeni ? 'Ekle' : 'Kaydet'}</button>${yeni ? '<button type="button" class="tk-dgm" data-kisi-vazgec>Vazgeç</button>' : '<button type="button" class="tk-dgm" data-kisi-sil title="Kişiyi sil"><i class="fas fa-trash"></i></button>'}
    </div>`;
  function kisiCiz() {
    const kap = $('#tkIKisiler'); if (!kap || !KISI) return; const l = KISI.kisiler || [];
    kap.innerHTML = (l.length ? l.map(k => kisiSatir(k, false)).join('') : '<div class="bos">Kayıtlı kişi yok.</div>') + `<div class="ozet"><span class="text-danger" id="tkKHata"></span></div>`;
  }
  async function kisiKaydet(el, sil) {
    const id = +el.dataset.id, g = { Kod: IK, Id: id, Sil: !!sil, AdSoyad: ($('.ad', el) || {}).value, Unvan: ($('.unvan', el) || {}).value, CepTelefonu: ($('.cep', el) || {}).value, Eposta: ($('.eposta', el) || {}).value, AnaYetkili: !!($('input[type=radio]', el) || {}).checked };
    if (sil && !confirm('Kişi silinsin mi?')) return;
    const d = await post('/Tahsilat/IletisimKisiKaydet', g).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    const h = $('#tkKHata'); if (!d.success) { if (h) h.textContent = d.message || 'Kaydedilemedi.'; return; }
    KISI = d.paket; if (!id && d.paket.kisiler.length) SECKISI = Math.max(...d.paket.kisiler.map(x => x.id));   // yeni eklenen kişi alıcı olur
    kisiCiz(); aliciCiz(); mesaj('success', '<i class="fas fa-address-book me-1"></i>' + esc(d.message)); kirli = true;
    const k = cariBul(IK); if (k) { const ana = (KISI.kisiler || []).find(x => x.anaYetkili); if (ana) k.yetkili = ana.adSoyad; }
  }
  // ---- yazışma geçmişi: WhatsApp ve e-posta ayrı sütunlar
  function iltGecmis() {
    const l = iletisimler().filter(i => i.cariKodu === IK);
    const balon = i => `<div class="balon ${i.yon === 'Gelen' ? 'gelen' : 'giden'}"><div class="ust2"><b>${i.yon === 'Gelen' ? 'Müşteri' : esc(i.kullanici || 'Biz')}</b>${i.kime ? `<span class="kime">${i.yon === 'Gelen' ? 'yazan' : '→'} ${esc(i.kime)}</span>` : ''}${(i.icerik || []).map(x => rozet(x, x === 'İtiraz' ? 'hata' : x === 'Ödeme Sözü' || x === 'Mutabık' ? 'ok' : 'bilgi')).join(' ')}${i.otomatik ? rozet('Otomatik', 'bekle') : ''}${i.ornek ? '<span class="ornek">ÖRNEK</span>' : ''}${i.sozTutar ? `<b class="soz">söz ${tl(i.sozTutar)}${i.sozTarihi ? ' · ' + trh(i.sozTarihi) : ''}</b>` : ''}<span class="zaman">${esc(i.zaman)}</span></div><div class="metin">${esc(i.metin)}</div>${(i.baglantilar || []).length ? `<div class="bag">${i.baglantilar.map(b => `<a href="${esc(b.url)}" target="_blank" rel="noopener"><i class="fas fa-file-pdf"></i>${esc(b.ad)}</a>`).join('')}</div>` : ''}</div>`;
    const yaz = (id, kanal, bos) => { const x = l.filter(i => i.kanal === kanal); const kap = $(id); if (!kap) return; kap.innerHTML = x.length ? x.map(balon).join('') : `<div class="bos">${bos}</div>`; const n = $(id + 'N'); if (n) n.textContent = x.length || ''; };
    yaz('#tkIGecmisWa', 'WhatsApp', 'WhatsApp yazışması yok.'); yaz('#tkIGecmisMl', 'E-posta', 'E-posta yazışması yok.');
  }
  async function iltGonder() {
    const kanallar = $$('input[name=tkIKanal]:checked').map(x => x.value), icerik = $$('input[name=tkIIcerik]:checked').map(x => x.value), mesaj_ = $('#tkIMesaj').value, hata = $('#tkIHata'); hata.textContent = '';
    if (!SECKISI) { hata.textContent = 'Alıcı seçin (1 · Kime Gönderilecek).'; return; } if (!kanallar.length) { hata.textContent = 'Kanal seçin.'; return; } if (!icerik.length && !mesaj_.trim()) { hata.textContent = 'İçerik seçin ya da not yazın.'; return; }
    const dg = $('#tkIGonder'); dg.disabled = true; const talepId = +(iPencere.dataset.talep || 0);
    const d = await post('/Tahsilat/IletisimGonder', { Kod: IK, KisiId: SECKISI, Kanallar: kanallar, Icerik: icerik, Mesaj: mesaj_, TalepId: talepId }).catch(() => ({ success: false, message: 'Ağ hatası.' })); dg.disabled = false;
    if (!d.success) { hata.textContent = d.message || 'Gönderilemedi.'; return; }
    const s = $('#tkISonuc'); s.hidden = false; s.innerHTML = `<div class="ok"><i class="fas fa-circle-check me-1"></i>${esc(d.message)}</div><div class="dugmeler">${d.waLink ? `<a class="tk-dgm ana" href="${esc(d.waLink)}" target="_blank" rel="noopener"><i class="fab fa-whatsapp"></i>WhatsApp'ta Aç ve Gönder</a>` : ''}${d.mailLink ? `<a class="tk-dgm" href="${esc(d.mailLink)}"><i class="fas fa-envelope"></i>E-posta Taslağını Aç</a>` : ''}<button type="button" class="tk-dgm" data-kopyala><i class="fas fa-copy"></i>Metni Kopyala</button></div>`;
    s.dataset.metin = d.metin; if (d.waLink) window.open(d.waLink, '_blank', 'noopener');
    iPencere.dataset.talep = ''; await yukle(); iltGecmis(); aliciCiz();
  }
  async function iltSimule() {
    const metin = $('#tkISimMetin').value.trim(); if (!metin) { mesaj('warning', 'Gelen mesaj metnini yazın ya da örneklerden seçin.'); return; }
    const d = await post('/Tahsilat/WhatsAppGelen', { Kod: IK, Metin: metin }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    mesaj(d.success ? 'success' : 'danger', `<i class="fab fa-whatsapp me-1"></i>${esc(d.message)}`); if (!d.success) return;
    (d.uyari || []).forEach(u => uyariGoster(u)); $('#tkISimMetin').value = ''; await yukle(); iltGecmis();
  }
  /** WhatsApp'tan gelen önemli olay (ödeme sözü, itiraz, belge talebi): sağ üstte kalıcı uyarı kartı; sekmedeki zil ciz() içinde (talepUyari) */
  function uyariGoster(m) { let k = $('#tkUyarilar'); if (!k) { k = document.createElement('div'); k.id = 'tkUyarilar'; k.className = 'tk-uyarilar'; document.body.appendChild(k); } const e = document.createElement('div'); e.innerHTML = `<i class="fab fa-whatsapp"></i><span>${esc(m)}</span><button type="button" title="Kapat">×</button>`; e.querySelector('button').addEventListener('click', () => e.remove()); k.appendChild(e); setTimeout(() => e.remove(), 20000); }
  function talepUyari() { const n = talepler().filter(t => t.durum === 'Bekliyor').length, dg = $('#tkSekmeler button[data-p="surec"]'); if (!dg) return; dg.classList.toggle('yeni', n > 0); dg.title = n ? `${n} müşteri WhatsApp'tan belge istiyor (gönderim bekliyor)` : ''; let zil = $('.tk-zil', dg); if (n && !zil) dg.insertAdjacentHTML('beforeend', '<i class="fas fa-bell tk-zil"></i>'); else if (!n && zil) zil.remove(); }
  iPencere.addEventListener('click', e => {
    if (e.target === iPencere || e.target.closest('[data-i="kapat"]')) { iPencere.hidden = true; iPencere.dataset.talep = ''; return; }
    if (e.target.closest('#tkIGonder')) { iltGonder(); return; } if (e.target.closest('#tkISimule')) { iltSimule(); return; }
    const al = e.target.closest('[data-alici]'); if (al) { SECKISI = +al.dataset.alici; aliciCiz(); return; }
    const on = e.target.closest('[data-onizle]'); if (on) { e.preventDefault(); icerikOnizle(on.dataset.onizle); return; }
    if (e.target.closest('#tkIYeniKisi')) { const kap = $('#tkIKisiler'); if (!$('.kisi.yeni', kap)) { kap.insertAdjacentHTML('afterbegin', kisiSatir({ id: 0 }, true)); $('.kisi.yeni .ad', kap).focus(); } return; }
    if (e.target.closest('[data-kisi-vazgec]')) { const y = e.target.closest('.kisi'); if (y) y.remove(); return; }
    const or = e.target.closest('[data-ornek]'); if (or) { $('#tkISimMetin').value = ORNEK_YANIT[or.dataset.ornek] || ''; return; }
    const kp = e.target.closest('[data-kopyala]'); if (kp) { navigator.clipboard && navigator.clipboard.writeText($('#tkISonuc').dataset.metin || ''); kp.innerHTML = '<i class="fas fa-check"></i>Kopyalandı'; return; }
    const kk = e.target.closest('[data-kisi-kaydet]'); if (kk) { kisiKaydet(kk.closest('.kisi'), false); return; }
    const ks = e.target.closest('[data-kisi-sil]'); if (ks) { kisiKaydet(ks.closest('.kisi'), true); }
  });
  iPencere.addEventListener('keydown', e => { if (e.key === 'Enter' && e.target.closest('.kisi input:not([type=radio])')) { e.preventDefault(); kisiKaydet(e.target.closest('.kisi'), false); } });
  iPencere.addEventListener('change', e => { if (e.target.name === 'tkKAna') { const el = e.target.closest('.kisi'); if (+el.dataset.id) kisiKaydet(el, false); return; } if (e.target.name === 'tkIKanal' || e.target.name === 'tkIIcerik') metinOnizle(); });
  iPencere.addEventListener('input', e => { if (e.target.id === 'tkIMesaj') metinOnizle(); });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !iPencere.hidden) iPencere.hidden = true; });

  // ---------- bakiye mutabakatı (19 Kas 2026): Açık Toplam + Fatura Dışı Borç − Kapatılmamış Ödeme = Bakiye; kalem kalem pencere ----------
  const fdbListe = k => (k.fdb || []).filter(kalemUy), fdbTop = k => topla(fdbListe(k), x => x.tl);
  // 19 Kas 2026 (kullanıcı): "Kur Farkı" hareketi = muhasebenin dönem sonu DEĞERLEME virmanı (dekont); tahsilattan doğan kur farkı FATURASI ayrı (KFF-… fatura, açık fatura listesinde)
  const fdbTipAd = t => t === 'Kur Farkı' ? 'Kur Farkı Değerlemesi (muhasebe virmanı)' : t;
  // 19 Kas 2026 (kullanıcı: "tüm sekmelerde belge no tıklayınca çalışsın"): fatura numarası → Fatura İncelemesi penceresi (tahsilat.js 'thFaturaAc')
  const belgeLink = (kod, belge) => belge ? `<a href="#" class="tk-link" data-fat-ac="${esc(belge)}" data-kod="${esc(kod)}" title="${esc(belge)} · fatura detayını aç"><b>${esc(belge)}</b></a>` : tire;
  const k4 = v => Number(v || 0).toLocaleString(TR, { minimumFractionDigits: 4, maximumFractionDigits: 4 });
  // fatura olmayan belgeler (tahsilat, çek, senet, dekont, banka hareketi) → Belge İncelemesi penceresi (/Tahsilat/BelgeDetay); fatura çıkarsa Fatura İncelemesi'ne yönlenir
  const belgeLink2 = (kod, no) => no ? `<a href="#" class="tk-link" data-belge="${esc(no)}" data-kod="${esc(kod || '')}" title="${esc(no)} · belge incelemesi"><b>${esc(no)}</b></a>` : tire;
  async function belgeAc(kod, no) {
    const d = await fetch(`/Tahsilat/BelgeDetay?kod=${encodeURIComponent(kod || '')}&no=${encodeURIComponent(no)}`).then(r => r.json()).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (!d.success) { mesaj('warning', esc(d.message)); return; }
    if (d.tur === 'Fatura') { window.dispatchEvent(new CustomEvent('thFaturaAc', { detail: { kod: d.kod, no: d.no } })); return; }
    const kv = (l, v, s) => `<div><div class="l">${l}</div><div class="v">${v == null || v === '' ? '—' : v}</div><div class="s">${s || '&nbsp;'}</div></div>`;
    if (d.tur === 'TahsilatKaydi') {   // 19 Kas 2026 gece: tahsilat kaydı numarası (Tahsilatlar listesi, banka penceresi) → kaydın incelemesi
      const t = d.kayit;
      penAc(`<i class="fas fa-receipt me-1"></i>${esc(t.no)} · Tahsilat Kaydı · Belge İncelemesi`, `${esc(t.cariKodu)} ${esc(t.cariAd)} · ${esc(t.tarih)} · ${esc(t.yontem)} · ${esc(t.islemTipi)} · ${esc(t.kullanici || '')} ${esc(t.zaman || '')}`,
        `<div class="tk-dkpi">${kv('Durum', rozet(t.durum, t.islendi ? 'ok' : 'bekle'), t.islendi ? esc(t.islemeZamani || '') : 'taslak · Tahsilatlar › İşleme Al')}${kv('Tutar', `<b>${esc(t.tutarTr)}</b>`, esc(t.tutarTlTr))}${kv('Mod', esc(t.mod), `${(t.dagitim || []).length} fatura kapaması`)}${kv('Avans', t.avans > 0.005 ? esc(t.avansTr) : '—', 'faturayla eşlenmeyen kısım')}${kv('Kur Farkı', t.kurFarki ? esc(t.kurFarkiTr) : '—', t.kurFarkiFaturaNo ? belgeLink(t.cariKodu, t.kurFarkiFaturaNo) : (t.kurFarki ? 'faturası kesilmedi' : ''))}${kv('Vade Farkı', t.vadeFarki > 0 ? esc(t.vadeFarkiTr) : '—', t.vadeFarkiFaturaNo ? belgeLink(t.cariKodu, t.vadeFarkiFaturaNo) : (t.vadeFarki > 0 ? 'faturası kesilmedi' : ''))}</div>
         <div class="tk-dgrid">${kart('fa-money-bill-wave', `Ödeme Satırları · ${(t.satirlar || []).length}`, mt(['#', 'Yöntem', 'Tarih', 'Belge', 'Banka', 'Tutar', 'TL', 'Cari Hareket', 'Not'], (t.satirlar || []).map(s => [s.sira, esc(s.yontem), esc(s.tarih), esc(s.belgeNo || '—'), esc(s.banka || '—'), esc(s.tutarTr), esc(s.tutarTlTr), s.hareketNo ? belgeLink2(t.cariKodu, s.hareketNo) : (t.islendi ? '—' : 'işlenmedi'), esc(s.not || '')]), 'Satır yok.'), 'iki')}${kart('fa-link', `Kapattığı Faturalar · ${(t.dagitim || []).length}`, mt(['Fatura', 'Ödeme', 'Ödeme Tarihi', 'Gün', 'Kapanan', 'Kalan (Sonra)', 'Vade Farkı', 'Kur Farkı'], (t.dagitim || []).map(x => [belgeLink(t.cariKodu, x.belgeNo), `#${x.odemeSira} ${esc(x.odemeYontem || '')}`, esc(x.odemeTarihi || ''), x.gecikmeGun > 0 ? `<span class="gec">${x.gecikmeGun}</span>` : x.gecikmeGun, `${esc(x.tutarTr)}\u00A0${esc(pbAd(x.pb))}`, esc(x.faturaKalanTr), x.vadeFarki > 0 ? esc(x.vadeFarkiTr) : tire, x.kurFarki ? esc(x.kurFarkiTr) : tire]), 'Fatura eşleşmesi yok; tutar bakiyeye avans olarak kalır.'), 'iki')}</div>${t.aciklama ? `<div class="na-mini mt-1"><b>Açıklama:</b> ${esc(t.aciklama)}</div>` : ''}`,
        `<a class="tk-dgm" href="/Tahsilat/TahsilatPdf?no=${encodeURIComponent(t.no)}" target="_blank" rel="noopener"><i class="fas fa-file-pdf"></i>PDF</a><a class="tk-dgm" href="/Rapor20?bpCode=${encodeURIComponent(t.cariKodu)}" target="_blank" rel="noopener"><i class="fas fa-file-invoice"></i>Cari Ekstre</a><span class="bos"></span><button type="button" class="tk-dgm" data-p-kapat>Kapat</button>`);
      return;
    }
    if (d.tur === 'Banka') {
      const b = d.banka; penAc(`<i class="fas fa-building-columns me-1"></i>${esc(b.no)} · Banka Hareketi · Belge İncelemesi`, `${esc(b.sirket)} · ${esc(b.banka)} · ${esc(b.tarih)} · ${esc(b.yon || '')}`,
        `<div class="tk-dkpi">${kv('Durum', rozet(b.durum, b.durum === 'Onaylandı' ? 'ok' : 'bekle'), b.onaylayan ? `${esc(b.onaylayan)} · ${esc(b.onayTarihi)}` : 'muhasebeleşmedi')}${kv('Tutar', `<b>${tl(b.tutar)}</b>`, b.pb && b.pb !== 'TL' && b.pb !== 'TRY' ? `${n2(b.dovizTutar)}\u00A0${esc(b.pb)} · kur ${k4(b.kur)}` : 'TL')}${kv('Gönderen', esc(b.karsiAd), esc(b.karsiIban || ''))}${kv('Müşteri', b.cariKod ? `<b>${esc(b.cariKod)}</b> ${esc(b.cariAd)}` : 'seçilmedi', b.cariKod ? '' : 'Banka Tahsilatları › Müşteri Seç')}${kv('Fiş', esc(b.yevmiyeNo), b.hesapKodu ? `hesap ${esc(b.hesapKodu)}` : '')}${kv('Tahsilat Kaydı', b.tahsilatNo ? `<a href="#" data-tahsilat="${esc(b.tahsilatNo)}">${esc(b.tahsilatNo)}</a>` : '—', b.tahsilatNo ? 'Tahsilatlar sekmesinde aç' : 'tahsilat kaydı yok')}</div>
         <div class="na-mini mt-2"><b>İşlem türü:</b> ${esc(b.islemTuru || '—')} · <b>Açıklama:</b> ${esc(b.aciklama || '—')} · <b>Referans:</b> ${esc(b.referans || '—')} · <b>IBAN:</b> ${esc(b.iban || '—')}${b.onayNotu ? ` · <b>Onay notu:</b> ${esc(b.onayNotu)}` : ''}</div>`,
        `<a class="tk-dgm" href="/Rapor123?sekme=banka" target="_blank" rel="noopener"><i class="fas fa-clipboard-check"></i>Takip Paneli</a>${b.cariKod ? `<a class="tk-dgm" href="/Rapor20?bpCode=${encodeURIComponent(b.cariKod)}" target="_blank" rel="noopener"><i class="fas fa-file-invoice"></i>Cari Ekstre</a>` : ''}`); return;
    }
    const h = d.hareket, kod2 = d.kod; const tutar = h.borc > 0 ? h.borc : h.alacak, yon = h.borc > 0 ? 'Borç' : 'Alacak';
    const tahs = d.tahsilat ? `<div class="na-mini mt-1"><b>Tahsilat kaydı:</b> <a href="#" data-tahsilat="${esc(d.tahsilat.no)}">${esc(d.tahsilat.no)}</a> · ${esc(d.tahsilat.tarih)} · ${esc(d.tahsilat.yontem)} · ${n2(d.tahsilat.tutar)}\u00A0${esc(pbAd(d.tahsilat.pb))} · ${esc(d.tahsilat.durum)} · ${esc(d.tahsilat.kullanici)}${d.tahsilat.satir ? ` · satır #${d.tahsilat.satir.sira} ${esc(d.tahsilat.satir.yontem)} ${esc(d.tahsilat.satir.belgeNo || '')} ${esc(d.tahsilat.satir.banka || '')}` : ''}</div>` : '';
    const dagT = d.dagitim && d.dagitim.length ? kart('fa-link', `Kapattığı Faturalar (tahsilat dağıtımı) · ${d.dagitim.length}`, mt(['Fatura', 'Kapanan', 'Gün', 'Vade Farkı\u00A0(₺)', 'Kur Farkı\u00A0(₺)', 'Fatura Kalanı'], d.dagitim.map(x => [belgeLink(kod2, x.belgeNo), `${n2(x.tutar)}\u00A0${esc(pbAd(x.pb))}`, x.gun > 0 ? `<span class="gec">${x.gun}</span>` : x.gun, x.vadeFarki ? n2(x.vadeFarki) : tire, x.kurFarki ? n2(x.kurFarki) : tire, n2(x.kalanSonra)]))) : '';
    const kapT = d.kapamalar && d.kapamalar.length ? kart('fa-arrows-left-right', `Kapamalar (virman / robot / otomatik kapatma) · ${d.kapamalar.length}`, mt(['Zaman', 'Mod', 'Fatura', 'Tutar', 'Yöntem', 'Kim'], d.kapamalar.map(x => [esc(x.zaman), esc(x.mod), belgeLink(kod2, x.faturaNo), `${n2(x.tutar)}\u00A0${esc(pbAd(x.pb))}`, esc(x.yontem), esc(x.kullanici)]))) : '';
    const degT = d.degerleme ? kart('fa-arrow-trend-up', `Kur Farkı Değerlemesi · ${esc(d.degerleme.donem)} · fiş ${esc(d.degerleme.fisNo)} · ${esc(d.degerleme.kurlar)}`, mt(['Fatura', 'Kalan', 'Baz Kur', 'Yeni Kur', 'Fark\u00A0(₺)'], d.degerleme.faturalar.map(f => [belgeLink(kod2, f.belgeNo), `${n2(f.kalan)}\u00A0${esc(pbAd(f.pb))}`, `${k4(f.bazKur)}<small>${esc(f.bazKaynak)}</small>`, k4(f.yeniKur), `<b>${n2(f.fark)}</b>`]))) : '';
    penAc(`<i class="fas fa-file-lines me-1"></i>${esc(h.belgeNo)} · ${esc(h.tip)} · Belge İncelemesi`, `${esc(kod2)} ${esc(d.kisaAd)} · ${esc(h.tarih)} · ${esc(h.islemTipi)}${h.vade ? ' · vade ' + esc(h.vade) : ''}`,
      `<div class="tk-dkpi">${kv(yon, `<b>${n2(tutar)}\u00A0${esc(pbAd(h.pb))}</b>`, tl(h.tl))}${kv('Kalan (Eşlenmemiş)', h.kalan > 0.005 ? `<span class="gec">${n2(h.kalan)}\u00A0${esc(pbAd(h.pb))}</span>` : '0', h.kalan > 0.005 ? `${tl(h.kalanTl)} · kapama bekliyor` : 'tamamı eşlendi')}${kv('Durum', esc(h.durum || '—'), h.tip === 'Kur Farkı' ? 'muhasebe virmanı (dekont)' : h.tip)}${kv('İşlem Tipi', esc(h.islemTipi), '')}${kv('Hareket Sonrası Bakiye', tl(h.bakiyeSonra), 'cari hesap')}</div>
       <div class="na-mini mt-1"><b>Açıklama:</b> ${esc(h.aciklama || '—')}</div>${tahs}<div class="tk-dgrid mt-2">${dagT}${kapT}${degT}</div>`,
      `${d.tahsilat ? `<a class="tk-dgm ana" href="#" data-tahsilat="${esc(d.tahsilat.no)}"><i class="fas fa-receipt"></i>Tahsilat Kaydını Aç</a>` : ''}<a class="tk-dgm" href="/Rapor20?bpCode=${encodeURIComponent(kod2)}" target="_blank" rel="noopener"><i class="fas fa-file-invoice"></i>Cari Ekstre</a>`);
  }
  const fdbRozet = x => x.tip === 'Kur Farkı' ? `<span class="rt-durum rt-deg" title="Dönem sonu kur değerlemesi: muhasebenin attığı virman (dekont, KDV yok, yalnız R dövizli açık faturalar)">Kur Farkı Değerlemesi · Muhasebe Virmanı</span>` : rozet(x.tip, 'bekle');
  const odemeListe = k => (k.odemeAcik || []).filter(kalemUy);
  /** bakiye: işlem tipi seçiliyse o tiplerin bakiyesi (X + R = cari hesap bakiyesi) */
  const bakiyeSec = k => F.tip.length ? topla(F.tip, t => t === 'X' ? k.bakiyeX : t === 'R' ? k.bakiyeR : 0) : k.vf.mizanBakiye;
  /** kapatılmamış ödeme / avans: denklemden kalan (açık + fatura dışı − bakiye); listedeki ödemelerle farkı "kapama farkı" olarak pencerede gösterilir */
  const kapanmamis = k => Math.round((k.acikToplam + fdbTop(k) - bakiyeSec(k)) * 100) / 100;
  const kalemOzet = k => { const s = k.kalemler.filter(x => x.tur === 'Satış').length, f = k.kalemler.length - s; return `${s} satış${f ? ` · ${f} fark faturası` : ''}`; };
  const kalemLink = (k, bolum, ic) => `<a href="#" class="tk-kalem" data-kalem="${esc(k.kod)}" data-bolum="${bolum}" title="Tıklayınca bakiye mutabakatı kalem kalem">${ic}</a>`;
  const kPencere = $('#tkKalem');
  function kalemAc(kod, bolum) {
    const k = cariler().find(x => x.kod === kod) || cariBul(kod); if (!k) return;
    const kal = k.kalemler, fdb = fdbListe(k), od = odemeListe(k), ac = k.acikToplam, fd = fdbTop(k), kp = kapanmamis(k), bk = bakiyeSec(k), odTop = topla(od, x => x.tl), kapamaFarki = Math.round((kp - odTop) * 100) / 100;
    const pbH = x => `${n2(x.tutar)}\u00A0${esc(pbAd(x.pb))}`, tipNot = F.tip.length ? ` · işlem tipi ${F.tip.join(' + ')}` : '';
    const kutu = (ad, deger, alt, sinif) => `<div class="${sinif || ''}"><div class="l">${ad}</div><div class="v">${deger}</div><div class="s">${alt || '&nbsp;'}</div></div>`;
    $('#tkKAd').innerHTML = `${esc(k.kod)} · ${esc(k.kisaAd)} · Bakiye Mutabakatı · Kalem Kalem`;
    $('#tkKAlt').textContent = [k.resmiUnvan, k.sirket, `kur ${trh(K.bugun)}`, F.tip.length ? 'işlem tipi ' + F.tip.join(' + ') : 'bütün işlem tipleri'].filter(Boolean).join(' · ');
    $('#tkKGovde').innerHTML = `<div class="tk-dkpi tk-denklem">${[
      kutu('Açık Toplam', tl(ac), kalemOzet(k)), kutu('+ Fatura Dışı Borç', tl(fd), `${fdb.length} kalem`), kutu('− Kapatılmamış Ödeme / Avans', tl(kp), kp < -0.5 ? 'fazla kapama: fatura kapatılmış, ödeme kaydı yok' : `${od.length} ödeme kapama bekliyor`, kp > 0.5 ? 'uyari' : kp < -0.5 ? 'kotu' : ''),
      kutu('= Bakiye', `<b>${tl(bk)}</b>`, k.vf.mizanDoviz || 'cari hesap', 'esas'), kutu('Kontrol', Math.abs(ac + fd - kp - bk) < 0.5 ? '<span class="ok">Tutuyor</span>' : `<span class="gec">${tl(ac + fd - kp - bk)}</span>`, 'açık + fatura dışı − kapatılmamış − bakiye'),
      kutu('Kapama Farkı', Math.abs(kapamaFarki) < 0.5 ? '<span class="ok">0\u00A0₺</span>' : `<span class="gec">${tl(kapamaFarki)}</span>`, 'kapatılmamış ödeme − listedeki ödemeler')].join('')}</div>
      <div class="tk-dgrid">
        ${kart('fa-file-invoice', `1 · Açık Faturalar · ${kal.length} · ${tl(ac)}`, mt(['Belge', 'Tür', 'Fatura Tarihi', 'Vade', 'Gün', 'Tip', 'Fatura Tutarı', 'Kalan', 'Baz Kur', 'Kalan\u00A0(₺)'], kal.map(x => [belgeLink(k.kod, x.belge), rozet(x.tur, x.tur === 'Satış' ? 'bilgi' : 'bekle'), trh(x.tarih), trh(x.vade), x.gun < 0 ? `<span class="gec">${-x.gun} gün gecikti</span>` : x.gun === 0 ? 'bugün' : `${x.gun} gün var`, esc(x.islemTipi || ''), pbH(x), `${n2(x.kalan)}\u00A0${esc(pbAd(x.pb))}`, pbAd(x.pb) === 'TL' ? tire : `${k4(x.bazKur || x.faturaKuru)}<small>${esc((x.bazKaynak || 'Fatura Kuru').split(' (')[0])}</small>`, `<b>${tl(x.tl)}</b>`]), 'Açık fatura yok.'), 'iki')}
        ${kart('fa-receipt', `2 · Fatura Dışı Borç · ${fdb.length} · ${tl(fd)}`, mt(['Belge', 'Belge Tipi', 'Tarih', 'Tip', 'Tutar', 'TL Karşılığı', 'Açıklama'], fdb.map(x => [belgeLink2(k.kod, x.belge), fdbRozet(x), trh(x.tarih), esc(x.islemTipi || ''), pbH(x), `<b>${tl(x.tl)}</b>`, `<small>${esc(x.aciklama || '')}</small>`]), 'Fatura dışı borç kalemi yok (kur farkı değerlemesi, devir, iade vb.).'))}
        ${kart('fa-hand-holding-dollar', `3 · Kapatılmamış Ödeme / Avans · ${od.length} · ${tl(odTop)}`, mt(['Belge', 'Belge Tipi', 'Tarih', 'Tip', 'Ödeme', 'Eşlenmemiş Kalan', 'Kalan\u00A0(₺)', 'Açıklama'], od.map(x => [belgeLink2(k.kod, x.belge), rozet(x.tip, 'ok'), trh(x.tarih), esc(x.islemTipi || ''), pbH(x), `${n2(x.kalan)}\u00A0${esc(pbAd(x.pb))}`, `<b>${tl(x.tl)}</b>`, `<small>${esc(x.aciklama || '')}</small>`]).concat(Math.abs(kapamaFarki) > 0.5 ? [['<b>Kapama farkı</b>', rozet('Hesap', 'bekle'), '', '', '', '', `<b>${tl(kapamaFarki)}</b>`, `<small>${kapamaFarki < 0 ? 'fazla kapama: fatura kalanı düşürülmüş ama karşılığında ödeme (alacak) hareketi yok — Nakit Akış / çek tahsili kaydı cariye işlenmemiş olabilir' : 'denklemden kalan ile listedeki kapama bekleyen ödemeler arasındaki fark (kısmi kapamalar / kur yuvarlaması)'}</small>`]] : []), 'Kapama bekleyen ödeme yok.'), 'iki')}
        ${kart('fa-equals', '4 · Denklem', mt(['Kalem', 'Tutar\u00A0(₺)', 'Kaynak'], [['Açık Toplam', `<b>${tl(ac)}</b>`, 'açık faturaların kalanı × güncel kur'], ['+ Fatura Dışı Borç', `<b>${tl(fd)}</b>`, 'fatura olmayan borç hareketleri'], ['− Kapatılmamış Ödeme / Avans', `<b>${tl(kp)}</b>`, 'faturalarla eşlenmemiş alacak'], ['= Bakiye', `<b>${tl(bk)}</b>`, `cari hesap (tüm borç − alacak)${tipNot}`]]))}
      </div>`;
    $('#tkKAltSerit').innerHTML = `<a class="tk-dgm ana" href="/Rapor117?sekme=yeni&kod=${encodeURIComponent(k.kod)}"><i class="fas fa-link"></i>Yeni Tahsilat › Otomatik Kapat</a><a class="tk-dgm" href="#" data-uyum="${esc(k.kod)}"><i class="fas fa-scale-balanced"></i>Muhasebe Uyumu</a><a class="tk-dgm" href="/Rapor20?bpCode=${encodeURIComponent(k.kod)}" target="_blank" rel="noopener"><i class="fas fa-file-invoice"></i>Cari Ekstre</a><a class="tk-dgm" href="/CariKart/Musteri?kod=${encodeURIComponent(k.kod)}" target="_blank" rel="noopener"><i class="fas fa-address-card"></i>Cari Kart</a><span class="bos"></span><button type="button" class="tk-dgm" data-k="kapat">Kapat</button>`;
    kPencere.hidden = false; const hedef = { acik: 1, fdb: 2, odeme: 3 }[bolum]; if (hedef) { const kk = $$('#tkKGovde .tk-dkart')[hedef - 1]; if (kk) kk.scrollIntoView({ block: 'nearest' }); }
  }
  kPencere.addEventListener('click', e => { if (e.target === kPencere || e.target.closest('[data-k="kapat"]')) { kPencere.hidden = true; return; } const u = e.target.closest('[data-uyum]'); if (u) { e.preventDefault(); kPencere.hidden = true; uyumAc(u.dataset.uyum); } });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !kPencere.hidden) kPencere.hidden = true; });

  // ---------- bakiye uyumu (19 Kas 2026): rozet + kalem kalem karşılaştırma penceresi ----------
  // beş kaynak (19 Kas 2026): cari hesap ↔ muhasebe hesap ekstresi, Cari Ekstre Raporu, Hesap Ekstresi Raporu, Mizan — hepsi uyuyorsa Uyumlu; ipucunda kaynak kaynak durum
  const uyumRozet = k => { const u = k.uyum || {}; const t = u.durum === 'Uyumlu' ? 'ok' : u.durum === 'Uyumsuz' ? 'hata' : 'bekle'; const ip = (u.kaynaklar || []).map(x => `${x.ad}: ${x.durum}${x.fark && Math.abs(x.fark) >= 0.5 ? ' (' + (x.fark > 0 ? '+' : '') + tl(x.fark) + ')' : ''}`).join('\n'); return `<a href="#" class="tk-uyum" data-uyum="${esc(k.kod)}" title="${esc(ip || u.durum || '')}\n${u.sorun || 0} satır farkı · tıklayınca bütün kaynaklar kalem kalem">${rozet(u.durum || '—', t)}<small>${u.uyumlu != null ? `${u.uyumlu}/${u.toplam} kaynak uyumlu` : ''}${u.durum === 'Uyumsuz' ? ` · <span class="gec">${u.fark > 0 ? '+' : ''}${tl(u.fark)}</span>` : ''}</small>${k.uyumGorev ? `<small class="${k.uyumGorev.gecikti ? 'gec' : ''}" title="açık düzeltme görevi"><i class="fas fa-user-check"></i> görev #${k.uyumGorev.id} · ${esc(k.uyumGorev.durum)} · ${esc(k.uyumGorev.atanan)}${k.uyumGorev.termin ? ' · ' + trh(k.uyumGorev.termin) : ''}</small>` : k.uyumKontrol ? `<small title="son bakiye kontrolü">kontrol ${trh(k.uyumKontrol.tarih)} · ${esc(k.uyumKontrol.durum)}</small>` : ''}</a>`; };
  const uPencere = $('#tkUyum');
  const uDurumRz = d => rozet(d, d === 'Uyumlu' || d === 'Esas' ? 'ok' : d === 'Uyumsuz' ? 'hata' : d === 'Örnek Veri' || d === 'Veri Yok' ? 'bilgi' : 'bekle');
  async function uyumAc(kod) {
    const d = await fetch('/Tahsilat/BakiyeKarsilastir?kod=' + encodeURIComponent(kod)).then(r => r.json()).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message || 'Karşılaştırma okunamadı.')); return; }
    const kutu = (ad, deger, alt, sinif) => `<div class="${sinif || ''}"><div class="l">${ad}</div><div class="v">${deger}</div><div class="s">${alt || '&nbsp;'}</div></div>`;
    const para = v => v == null ? tire : tl(v), farkH = v => v == null ? tire : Math.abs(v) < 0.5 ? '<span class="ok">0\u00A0₺</span>' : `<span class="gec">${v > 0 ? '+' : ''}${tl(v)}</span>`;
    const muh = d.kaynaklar[1], r20 = d.kaynaklar[2], r39 = d.kaynaklar[3], mz = d.kaynaklar[4], T = d.tahsilat, H = d.hesapOzet, ks = x => x.durum === 'Uyumlu' ? '' : 'kotu';
    $('#tkUAd').innerHTML = `${esc(d.kod)} · ${esc(d.ad)} · Bakiye Karşılaştırması ${uDurumRz(d.durum)} <small>${d.uyumlu}/${d.toplam} kaynak uyumlu</small>`;
    $('#tkUAlt').textContent = [d.unvan, d.sirket, d.hesapKodu ? 'muhasebe hesabı ' + d.hesapKodu : 'muhasebe hesabı tanımsız', trh(d.bugun), 'Uyumlu = beş kaynağın hepsi ± 0,50\u00A0₺ içinde'].filter(Boolean).join(' · ');
    $('#tkUGovde').innerHTML = `<div class="tk-dkpi">${[
      kutu('1 · Tahsilat Raporu · Cari Hesap', tl(d.bakiye), 'esas · tüm borç − alacak · güncel kur'),
      kutu('2 · Muhasebe Hesap Ekstresi', para(muh.bakiye), `${d.hesapKodu || 'hesap tanımsız'} · fark ${muh.fark == null ? '—' : tl(muh.fark)}`, ks(muh)),
      kutu('3 · Cari Ekstre Raporu', para(r20.bakiye), `fark ${r20.fark == null ? '—' : tl(r20.fark)}`, ks(r20)),
      kutu('4 · Hesap Ekstresi Raporu', para(r39.bakiye), `fark ${r39.fark == null ? '—' : tl(r39.fark)}`, ks(r39)),
      kutu('5 · Mizan ' + (d.hesapKodu || ''), para(mz.bakiye), `fark ${mz.fark == null ? '—' : tl(mz.fark)}`, ks(mz)),
      kutu('Açık Fatura Toplamı', tl(T.acikToplam), `${T.acikAdet} fatura · ${H.sorun} satır farkı · ${H.eslesen} eşleşen`)].join('')}</div>
      <div class="tk-dgrid">
        ${kart('fa-scale-balanced', 'Kaynak Bazında Bakiyeler', mt(['Kaynak', 'Rapor', 'Borç\u00A0(₺)', 'Alacak\u00A0(₺)', 'Bakiye\u00A0(₺)', 'Fark (Rapor − Kaynak)', 'Durum', 'Not', ''], d.kaynaklar.map(x => [`<b>${esc(x.ad)}</b>`, `<small>${esc(x.rapor)}</small>`, para(x.borc), para(x.alacak), x.bakiye == null ? tire : `<b>${tl(x.bakiye)}</b>`, farkH(x.fark), uDurumRz(x.durum), `<small>${esc(x.not)}</small>`, `<a class="tk-dgm" href="${esc(x.link)}" target="_blank" rel="noopener"><i class="fas fa-arrow-up-right-from-square"></i>Aç</a>`]), 'Kaynak yok.'), 'tam')}
        ${kart('fa-coins', 'Para Birimi Kırılımı · Cari Hesap', mt(['PB', 'Borç', 'Alacak', 'Net', 'Kur', 'TL Karşılığı', 'Açık Fatura'], d.dovizler.map(x => [`<b>${esc(x.pb)}</b>`, n2(x.borc), n2(x.alacak), `<b>${n2(x.net)}</b>`, Number(x.kur).toLocaleString(TR, { maximumFractionDigits: 4 }), tl(x.tl), x.acikAdet ? `${x.acikAdet} · ${n2(x.acikTutar)}\u00A0${esc(x.pb)}` : tire]), 'Hareket yok.'))}
        ${kart('fa-list-check', 'Bu Raporun Kalemleri', mt(['Kalem', 'Tutar\u00A0(₺)', 'Açıklama'], [['Cari Hesap Bakiyesi', `<b>${tl(d.bakiye)}</b>`, 'Alacak Takibi › Bakiye'], ['Açık Fatura Toplamı', tl(T.acikToplam), `${T.acikAdet} fatura · kalan × güncel kur`], ['Kapatılmamış Ödeme / Avans', tl(T.kapanmamis), 'açık toplam − bakiye'], ['Vadesi Geçmiş', tl(T.vadesiGecmis), ''], ['Bakiye X / R', `${tl(T.bakiyeX)} / ${tl(T.bakiyeR)}`, 'işlem tipine göre'], ['Çek / Senet', tl(T.cek), `${T.cekAdet} belge`], ['Cari Satır Toplamı', `${tl(H.cariBorc)} / ${tl(H.cariAlacak)}`, `${H.satir} hareket · borç / alacak`], ['Muhasebe Satır Toplamı', `${tl(H.hesapBorc)} / ${tl(H.hesapAlacak)}`, `${H.hesapSatir} fiş satırı · borç / alacak`]]))}
        ${kart('fa-not-equal', `Satır Farkları · Cari ↔ Muhasebe · ${d.farklar.length}`, mt(['Tarih', 'Belge', 'Tip', 'PB', 'Cari\u00A0(₺)', 'Muhasebe\u00A0(₺)', 'Fark\u00A0(₺)', 'Durum', 'Neden', 'Öneri', 'Fiş'], d.farklar.map(f => [esc(f.tarih), `<b>${esc(f.belge)}</b>`, esc(f.tip), esc(f.pb), para(f.cari), para(f.hesap), farkH(f.fark), rozet(f.durum, f.durum === 'Tutar Farkı' ? 'bekle' : 'hata'), `<small>${esc(f.neden || '')}</small>`, `<small>${esc(f.oneri || '')}</small>`, `<small>${esc(f.fis || '')}</small>`]), d.hesapKodu ? 'Cari ile muhasebe hesabı satır satır uyuşuyor.' : 'Muhasebe hesabı tanımsız; karşılaştırılacak satır yok.'), 'iki')}
      </div>
      ${uyumGorevKart(d)}`;
    UY = d; uyumGorevBagla();
    $('#tkUAltSerit').innerHTML = `<a class="tk-dgm" href="/Rapor141?kod=${encodeURIComponent(d.kod)}" target="_blank" rel="noopener"><i class="fas fa-code-compare"></i>Cari ↔ Hesap Ekstresi Mutabakatı</a><a class="tk-dgm" href="/Rapor20?bpCode=${encodeURIComponent(d.kod)}" target="_blank" rel="noopener"><i class="fas fa-file-invoice"></i>Cari Ekstre Raporu</a><a class="tk-dgm" href="/Rapor39" target="_blank" rel="noopener"><i class="fas fa-book"></i>Hesap Ekstresi</a><a class="tk-dgm" href="/Rapor28?calistir=1&anaHesap=120" target="_blank" rel="noopener"><i class="fas fa-table-list"></i>Mizan</a><a class="tk-dgm" href="/CariKart/Musteri?kod=${encodeURIComponent(d.kod)}" target="_blank" rel="noopener"><i class="fas fa-address-card"></i>Cari Kart</a><span class="bos"></span><span class="text-muted" id="tkUNot" style="font-size:.62rem">${d.kontrol ? `son kontrol ${esc(d.kontrol.tarih)} · ${esc(d.kontrol.kullanici)} · ${esc(d.kontrol.durum)} (${d.kontrolSayisi} kayıt)` : 'ilk kontrol'}</span><button type="button" class="tk-dgm" data-u="kapat">Vazgeç</button><button type="button" class="tk-dgm ana" id="tkUKaydet"><i class="fas fa-floppy-disk"></i>Kaydet</button>`;
    uPencere.hidden = false;
  }
  // ---------- KONTROL SONUCU + DÜZELTME GÖREVİ (19 Kas 2026; kullanıcı: "İhracat'ta yaptığımızın benzeri, ilgili kişilere görev atayabilelim"): IbkbOrnek görev motoru, Kaynak = Tahsilat ----------
  //   Kaydet → bakiye kontrolü kaydı (+ işaretliyse görev: Atandı), atanan portal kullanıcısıysa mesaj kutusuna, e-posta posta uygulamasında açılır; görev akışı Atandı → Düzeltildi (atanan) → Onaylandı / İade (muhasebe).
  let UY = null;
  const ugRz = d => rozet(d, d === 'Onaylandı' ? 'ok' : d === 'Düzeltildi' ? 'bilgi' : d === 'İade Edildi' || d === 'İptal' ? 'hata' : 'bekle');
  const ugOncRz = o => rozet(o, o === 'Acil' ? 'hata' : o === 'Yüksek' ? 'bekle' : 'bilgi');
  function uyumGorevKart(d) {
    const K2 = d.gorevKisiler || [], ac = d.gorev, farkVar = d.durum !== 'Uyumlu', bugun = bugunD(), termin = iso(new Date(bugun.getFullYear(), bugun.getMonth(), bugun.getDate() + 7));
    const kisiSec = `<select class="form-select form-select-sm" id="tkUgAtanan"><option value="">— seçin —</option>${K2.map(k => `<option value="${esc(k.ad)}" data-kod="${esc(k.kod || '')}" data-eposta="${esc(k.eposta || '')}">${esc(k.ad)}${k.bolum ? ' · ' + esc(k.bolum) : ''}${k.kod ? ' (' + esc(k.kod) + ')' : ''}</option>`).join('')}<option value="__diger">Diğer (adı yazın)</option></select>`;
    const alan = (id, ad, ic, genis) => `<div class="${genis ? 'g2' : ''}"><label>${ad}</label>${ic}</div>`;
    const gorevSatir = g => `<tr class="${g.acik ? '' : 'kapali'}"><td><b>#${g.id}</b><small>${esc(g.tarih)}</small></td><td>${ugRz(g.durum)}${g.gecikti ? ` <span class="gec">gecikti</span>` : ''}${g.iadeSayisi ? `<small>${g.iadeSayisi} iade</small>` : ''}</td><td>${esc(g.atanan)}${g.atananEposta ? `<small>${esc(g.atananEposta)}</small>` : ''}</td><td>${g.termin ? trh(g.termin) : tire}${g.acik && g.terminGun != null ? `<small>${g.terminGun} gün</small>` : ''}</td><td>${ugOncRz(g.oncelik)}</td><td><small>${esc(g.aciklama || '')}</small>${g.duzeltmeNotu ? `<small>düzeltme: ${esc(g.duzeltmeNotu)} · ${esc(g.duzelten)}</small>` : ''}${g.onayNotu ? `<small>onay / iade: ${esc(g.onayNotu)}</small>` : ''}</td><td><small>${esc(g.atayan)}</small></td>
      <td class="islem">${g.durum === 'Atandı' || g.durum === 'İade Edildi' ? `<button type="button" class="tk-dgm ana" data-ug-islem="duzeltildi" data-ug-id="${g.id}" title="Düzeltmeyi yaptım, onaya gönder"><i class="fas fa-check"></i>Düzeltildi</button>` : ''}${g.durum === 'Düzeltildi' ? `<button type="button" class="tk-dgm ana" data-ug-islem="onayla" data-ug-id="${g.id}" title="Muhasebe onayı: görev kapanır"><i class="fas fa-stamp"></i>Onayla</button><button type="button" class="tk-dgm uyari" data-ug-islem="iade" data-ug-id="${g.id}" title="Düzeltme yetersiz, yeniden yapılsın"><i class="fas fa-rotate-left"></i>İade</button>` : ''}${g.acik ? `<button type="button" class="tk-dgm" data-ug-islem="iptal" data-ug-id="${g.id}"><i class="fas fa-xmark"></i>İptal</button>` : ''}${g.atananEposta ? `<a href="#" class="ikon" data-ug-eposta="${g.id}" title="Atanan kişiye gidecek e-postayı posta uygulamasında aç"><i class="fas fa-envelope"></i></a>` : ''}</td></tr>`;
    return `<div class="tk-dkart tam tk-ugorev"><div class="b"><i class="fas fa-user-check me-1"></i>Kontrol Sonucu ve Düzeltme Görevi${ac ? ` · <span class="text-warning">açık görev #${ac.id} · ${esc(ac.durum)} · ${esc(ac.atanan)}</span>` : ''}</div><section class="ic">
      <div class="tk-ug-form"><div><label>Kontrol Sonucu</label><select class="form-select form-select-sm" id="tkUgDurum">${(d.kontrolDurumlari || ['Uyumlu', 'Fark Var', 'Açıklama Bekliyor']).map(x => `<option${(farkVar ? x === 'Fark Var' : x === 'Uyumlu') ? ' selected' : ''}>${esc(x)}</option>`).join('')}</select></div><div class="g3"><label>Not (Fark Nedeni / Yapılacak)</label><input class="form-control form-control-sm" id="tkUgNot" value="${esc(d.kontrol && d.kontrol.not || '')}" placeholder="ör. 120.01.148 hesabına aktarılmamış fatura; muhasebe fişi düzeltilecek"></div></div>
      <div class="tk-ug-bolum"><label class="tk-ug-ac"><input type="checkbox" id="tkUgAc"${farkVar && !ac ? ' checked' : ''}> Düzeltmeyi yapacak kişiye görev ata (e-posta + portal mesajı)</label>
        <div class="tk-ug-form" id="tkUgForm" style="display:${farkVar && !ac ? '' : 'none'}">${alan('atanan', 'Atanan Kişi', kisiSec, true)}<div id="tkUgDigerKap" style="display:none"><label>Diğer Kişi Adı</label><input class="form-control form-control-sm" id="tkUgDiger"></div><div><label>E-posta (Bildirim)</label><input class="form-control form-control-sm" id="tkUgEposta" placeholder="ad@uras.com.tr"></div><div><label>Termin</label><input type="date" class="form-control form-control-sm" id="tkUgTermin" value="${termin}"></div><div><label>Öncelik</label><select class="form-select form-select-sm" id="tkUgOncelik">${(d.oncelikler || ['Normal', 'Yüksek', 'Acil']).map(x => `<option>${esc(x)}</option>`).join('')}</select></div>${alan('yapilacak', 'Yapılacak (Boş = Kontrol Notu)', '<input class="form-control form-control-sm" id="tkUgYapilacak">', true)}</div>
        <small class="text-muted">Kaydet'te görev kaydı yazılır; atanan portal kullanıcısıysa mesaj kutusuna düşer ve e-posta cihazdaki posta uygulamasında hazır açılır (SMTP bağlanınca doğrudan gider). Akış: Atandı → Düzeltildi (atanan) → Onaylandı / İade (muhasebe); kendi düzeltmenizi onaylayamazsınız.</small></div>
      <table class="tk-dtablo" data-dinamik="kapali"><thead><tr>${['Görev', 'Durum', 'Atanan', 'Termin', 'Öncelik', 'Yapılacak · Notlar', 'Atayan', 'İşlem'].map(b => `<th>${b}</th>`).join('')}</tr></thead><tbody>${(d.gorevler || []).map(gorevSatir).join('') || '<tr><td colspan="8" class="bos">Bu caride düzeltme görevi yok.</td></tr>'}</tbody></table>
    </section></div>`;
  }
  function uyumGorevBagla() {
    const ac = $('#tkUgAc'), f = $('#tkUgForm'), sel = $('#tkUgAtanan'); if (!ac || !sel) return;
    ac.addEventListener('change', () => { f.style.display = ac.checked ? '' : 'none'; });
    sel.addEventListener('change', () => { const o = sel.selectedOptions[0]; if (o && o.dataset.eposta) $('#tkUgEposta').value = o.dataset.eposta; $('#tkUgDigerKap').style.display = sel.value === '__diger' ? '' : 'none'; });
  }
  const ugEposta = (alici, konu, govde) => { if (!alici) { mesaj('warning', 'Atanan kişinin e-postası yok.'); return; } location.href = 'mailto:' + encodeURIComponent(alici) + '?subject=' + encodeURIComponent(konu || '') + '&body=' + encodeURIComponent((govde || '').replace(/\{portal\}/g, location.origin)); };
  async function uyumKaydet() {
    if (!UY) return; const g = id => ($('#' + id) || {}).value || ''; const sel = $('#tkUgAtanan'); const o = sel && sel.selectedOptions[0]; const diger = g('tkUgAtanan') === '__diger';
    const gorev = $('#tkUgAc') && $('#tkUgAc').checked ? { Atanan: diger ? g('tkUgDiger').trim() : g('tkUgAtanan'), AtananKod: diger || !o ? '' : o.dataset.kod || '', AtananEposta: g('tkUgEposta').trim(), Termin: g('tkUgTermin') || null, Oncelik: g('tkUgOncelik') || 'Normal', Aciklama: g('tkUgYapilacak'), Konu: `${UY.kod} ${UY.ad} · bakiye / muhasebe uyumu düzeltme` } : null;
    if (gorev && !gorev.Atanan) { mesaj('warning', 'Görevi yapacak kişiyi seçin (ya da görev kutusunu kapatın).'); return; }
    const dg = $('#tkUKaydet'); dg.disabled = true;
    const d = await post('/Tahsilat/UyumKontrolKaydet', { CariKodu: UY.kod, Durum: g('tkUgDurum'), Not: g('tkUgNot'), BakiyeOzet: UY.bakiyeOzet, FarkOzet: UY.farkOzet, Gorev: gorev }).catch(() => ({ success: false, message: 'Ağ hatası.' })); dg.disabled = false;
    mesaj(d.success ? 'success' : 'danger', '<i class="fas fa-user-check me-1"></i>' + esc(d.message)); if (!d.success) return;
    if (d.gorev && d.gorev.atananEposta) ugEposta(d.gorev.atananEposta, d.gorev.konu, d.gorev.govde);
    const kod = UY.kod; kirli = true; await yukle(); uyumAc(kod);
  }
  async function uyumGorevIslem(id, islem) {
    const soru = { duzeltildi: 'Düzeltme açıklaması (isteğe bağlı):', onayla: 'Onay notu (isteğe bağlı):', iade: 'İade nedeni (zorunlu):', iptal: 'İptal nedeni (isteğe bağlı):' }[islem];
    const not = prompt(soru, ''); if (not === null) return; if (islem === 'iade' && !not.trim()) { mesaj('warning', 'İade nedeni zorunludur.'); return; }
    const d = await post('/Tahsilat/UyumGorevKaydet', { Id: id, Islem: islem, Not: not }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    mesaj(d.success ? 'success' : 'danger', '<i class="fas fa-user-check me-1"></i>' + esc(d.message)); if (!d.success) return;
    if (islem === 'iade' && d.gorev && d.gorev.atananEposta) ugEposta(d.gorev.atananEposta, d.gorev.konu, d.gorev.govde);
    const kod = UY && UY.kod; kirli = true; await yukle(); if (kod) uyumAc(kod);
  }
  uPencere.addEventListener('click', e => {
    if (e.target.closest('#tkUKaydet')) { uyumKaydet(); return; }
    const gi = e.target.closest('[data-ug-islem]'); if (gi) { uyumGorevIslem(+gi.dataset.ugId, gi.dataset.ugIslem); return; }
    const ge = e.target.closest('[data-ug-eposta]'); if (ge) { e.preventDefault(); const g = (UY && UY.gorevler || []).find(x => x.id === +ge.dataset.ugEposta); if (g) ugEposta(g.atananEposta, g.epostaKonu, g.epostaGovde); }
  });
  // e-posta / portal mesajındaki bağlantı: ?gorev=ID → görevin carisi için Bakiye Karşılaştırması
  if (kok.dataset.gorev) { const t = setInterval(() => { if (!K) return; clearInterval(t); fetch('/Tahsilat/UyumGorev?id=' + encodeURIComponent(kok.dataset.gorev)).then(r => r.json()).then(d => { if (d.success && d.gorev) uyumAc(d.gorev.cariKodu); else mesaj('warning', esc(d.message || 'Görev bulunamadı.')); }).catch(() => { }); }, 400); }
  uPencere.addEventListener('click', e => { if (e.target === uPencere || e.target.closest('[data-u="kapat"]')) uPencere.hidden = true; });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !uPencere.hidden) uPencere.hidden = true; });

  // ---------- müşteri detayı ----------
  const dPencere = $('#tkDetay'); let DK = null;   // açık detay: { kod, d }
  // detay tabloları: data-dinamik="kapali" (kabuğun süzgeç / sıralama satırı eklenmez); kartların gövdesi kendi içinde kayar, pencere ekrana sığar
  const mt = (bas, sat, bos) => `<table class="tk-dtablo" data-dinamik="kapali"><thead><tr>${bas.map(b => `<th>${b}</th>`).join('')}</tr></thead><tbody>${sat.length ? sat.map(r => `<tr>${r.map(h => `<td>${h}</td>`).join('')}</tr>`).join('') : `<tr><td colspan="${bas.length}" class="bos">${bos}</td></tr>`}</tbody></table>`;
  const kart = (ikon, ad, ic, sinif) => `<div class="tk-dkart${sinif ? ' ' + sinif : ''}"><div class="b"><i class="fas ${ikon} me-1"></i>${ad}</div><section class="ic">${ic}</section></div>`;
  function detayCiz() {
    const d = DK.d, k = d.kart, v = d.vf, kutu = (ad, deger, alt, sinif) => `<div class="${sinif || ''}"><div class="l">${ad}</div><div class="v">${deger}</div><div class="s">${alt || '&nbsp;'}</div></div>`;
    $('#tkDAd').innerHTML = `${esc(k.kod)} · ${esc(k.kisaAd)} ${v.satisDurdu ? rozet('Satış Durduruldu', 'hata') : ''}`;
    $('#tkDAlt').textContent = [k.resmiUnvan, `${k.bolge} · ${k.sinif}`, k.iliski, k.ulke + (k.sehir ? ' / ' + k.sehir : ''), k.sirket, k.vadeSarti, k.temsilci ? 'temsilci ' + k.temsilci : ''].filter(Boolean).join(' · ');
    const acik = d.acik.filter(x => !x.vfFaturasi);
    $('#tkDGovde').innerHTML = `<div class="tk-dkpi">${[
      kutu('Toplam Bakiye · Mizan', tl(v.mizanBakiye), esc(v.mizanDoviz) || `hesap ${esc(v.mizanHesap)}`),
      kutu('Vadesi Geçmiş', tl(d.risk.gecmis), d.risk.gecmisAdet ? `${d.risk.gecmisAdet} fatura · en uzun ${d.risk.maxGecikme} gün` : 'gecikme yok', d.risk.gecmis > 0 ? 'kotu' : ''),
      kutu('Ay Sonuna Kadar Vadesi Gelecek', tl(v.aySonuGelecek), `${trh(d.aySonu)} tarihine kadar`),
      v.vfKural === 'Tahsilatta' ? kutu('Vade Farkı · Tahsilatta Kesilir', tl2(v.bugunVf), 'yurtdışı · bugüne doğan · ay sonu / robot kesimi yok', '') : kutu('Ay Sonu Vade Farkı', tl2(v.aySonuVf), `bugüne ${tl2(v.bugunVf)} · KDV hariç`, v.aySonuVf > 0 ? 'uyari' : ''),
      kutu('Kesilen Vade Farkı', tl2(v.kesilen), v.kesilenAdet ? `${v.kesilenAdet} fatura · ödenmemiş ${tl2(v.acik)}` : 'fatura yok', v.acik > 0.005 ? 'kotu' : ''),
      kutu('Limit', v.limitTur === 'Yok' ? 'Limit Yok' : tl(v.limit), v.limitTur === 'Yok' ? 'limit izlenmez' : `risk ${tl(v.limitRisk)}${v.limitYuzde != null ? ' · %\u00A0' + Number(v.limitYuzde).toLocaleString(TR) : ''} · ${esc(v.limitDurum)}`, v.limitDurum === 'Limit Aşıldı' ? 'kotu' : '')].join('')}</div>
      <div class="tk-dbilgi">
        <span><b>Son Ödeme:</b> ${v.sonOdeme ? `${trh(v.sonOdeme)} · ${tl(v.sonOdemeTl)} · ${v.sonOdemeGun} gün önce` : 'yok'}</span>
        <span><b>Vade Farkı Sonrası:</b> ${v.sonrasiDurum ? `${sonrasiRozet(v.sonrasiDurum)} ${esc(v.refNo)} (${trh(v.refTarih)}) faturasından sonra · ödeme ${v.sonrasiOdemeAdet ? tl(v.sonrasiOdeme) : 'yok'} · satış ${v.sonrasiSatisAdet ? `${v.sonrasiSatisAdet} fatura ${tl(v.sonrasiSatis)}` : 'yok'}` : 'vade farkı faturası kesilmemiş'}</span>
        <span><b>Satış:</b> ${v.satisDurdu ? `${rozet('Durduruldu', 'hata')} ${esc(v.satisNeden)} · ${esc(v.satisKim)} ${esc(v.satisZaman)}` : rozet('Açık', 'ok')}</span>
        <span><b>Mutabakat:</b> ${mutRozet(v.mutabakat)} ${v.mutabakatTarih ? `${esc(v.mutabakatBelge)} · ${esc(v.mutabakatTarih)} · ${esc(v.mutabakatKanal)}` : ''}${v.sonMutabik ? ` · son mutabık ${esc(v.sonMutabik)}` : ''} · yetkili ${esc(k.mutabakatYetkilisi || 'tanımlı değil')}</span>
        <span><b>Risk:</b> ${riskRozet(d.risk.durum)} ${esc(d.risk.neden)}</span>
        <span><b>Vade Farkı Robotu:</b> ${v.vfKural === 'Tahsilatta' ? `${rozet('Tahsilatta Kesilir', 'bilgi')} yurtdışı müşteri · dövizli tahsilat · ay sonu / robot / elle kesim yok` : d.takip.haric ? `${rozet(d.takip.haricSurekli ? 'Robot Dışı · Sürekli' : 'Robot Dışı · Bu Ay', 'bekle')} ${esc(d.takip.haricNeden)} · ${esc(d.takip.haricKim)}` : rozet('Dahil', 'ok')}${d.takip.robotFatura ? ` · bu ay kesildi: ${esc(d.takip.robotFatura)}` : ''} · bu ay ödeme ${d.takip.buAyOdemeAdet ? tl(d.takip.buAyOdeme) : 'yok'}</span>
      </div>
      <div class="tk-dgrid">
        ${kart('fa-file-invoice', `Açık Faturalar · ${acik.length}`, mt(['Vade', 'Belge No', 'Kalan', 'Kalan\u00A0(₺)', 'Gecikme', 'Son Kesim', 'Bugüne Vade Farkı\u00A0(₺)', 'Ay Sonu Vade Farkı\u00A0(₺)'], acik.map(x => [trh(x.vade), belgeLink(k.kod, x.belge), `${n2(x.kalan)}\u00A0${esc(x.pb)}`, tl(x.tl), x.gun < 0 ? `<span class="gec">${-x.gun} gün</span>` : x.gun === 0 ? 'bugün' : `${x.gun} gün var`, x.vfSon ? trh(x.vfSon) : tire, x.vfBugun ? n2(x.vfBugun) : tire, v.vfKural === 'Tahsilatta' ? '<small>tahsilatta</small>' : x.vfAySonu ? `<b>${n2(x.vfAySonu)}</b>` : tire]), 'Açık fatura yok.'), 'iki')}
        ${kart('fa-percent', `Vade Farkı Faturaları · ${d.faturalar.length}`, mt(['Fatura No', 'Tarih · Vade', 'Matrah\u00A0(₺)', 'Toplam\u00A0(₺)', 'Ödeme'], d.faturalar.map(f => [`<b>${esc(f.no)}</b><small>${esc(f.tahsilatNo ? 'Tahsilat ' + f.tahsilatNo : f.kaynak + (f.donem ? ' · ' + f.donem : ''))}</small>`, `${esc(f.tarih)}<small>vade ${esc(f.vade)}</small>`, n2(f.matrah), `<b>${n2(f.toplam)}</b>`, rozet(f.odeme, f.odeme === 'Ödendi' ? 'ok' : f.odeme === 'Vadesi Geçti' ? 'hata' : 'bekle') + (f.kalan > 0.005 && f.kalan < f.toplam - 0.005 ? `<small>kalan ${n2(f.kalan)}</small>` : '')]), 'Bu müşteriye vade farkı faturası kesilmedi.'))}
        ${kart('fa-comments', `Görüşme Notları · ${d.notlar.length}`, `<div class="tk-notlar">${notListeH(d.notlar)}</div>`)}
        ${kart('fa-money-bill-transfer', 'Son Ödemeler', mt(['Tarih', 'Belge', 'Tutar', 'Kapatılan'], d.odemeler.map(o => [trh(o.tarih) + (o.vfSonrasi ? ' <i class="fas fa-circle-check ok" title="Son vade farkı faturasından sonra"></i>' : ''), `${esc(o.belge)}<small>${esc(o.tip)}</small>`, `${tl(o.tl)}${o.pb !== 'TL' ? `<small>${n2(o.tutar)}\u00A0${esc(o.pb)}</small>` : ''}`, `<small title="${esc(o.aciklama || '')}">${esc(kisalt(o.aciklama, 44))}</small>`]), 'Ödeme kaydı yok.'))}
        ${kart('fa-cart-shopping', 'Vade Farkı Sonrası Satışlar', mt(['Fatura Tarihi', 'Belge No', 'Vade', 'Tutar', 'Kalan'], (d.satislar || []).map(s => [trh(s.tarih), `<b>${esc(s.belge)}</b>`, trh(s.vade), `${tl(s.tl)}${s.pb !== 'TL' ? `<small>${n2(s.tutar)}\u00A0${esc(s.pb)}</small>` : ''}`, n2(s.kalan)]), d.satislar ? 'Son vade farkı faturasından sonra satış faturası kesilmedi.' : 'Vade farkı faturası kesilmemiş.'))}
        ${kart('fa-envelope-circle-check', 'Mutabakat Gönderimleri', mt(['Belge', 'Kanal · Kime', 'Bakiye', 'Sonuç'], (d.gonderimler || []).map(g => [`<b>${esc(g.belgeNo)}</b><small>${esc(g.tarih)}</small>`, `${esc(g.kanal)}<small>${esc(g.kime)}</small>`, `<small>${esc(g.ozet)}</small>`, mutRozet(g.sonuc === 'Bekliyor' ? 'Yanıt Bekliyor' : g.sonuc) + (g.yanit ? `<small>${esc(g.yanit)}</small>` : '')]), 'Bu müşteriye mutabakat gönderilmedi.'))}
        ${kart('fa-clock-rotate-left', 'Kart Günlüğü · Satış Durumu / Vade Farkı / Limit', mt(['Zaman', 'Kim', 'Konu', 'Eski', 'Yeni', 'Açıklama'], d.gunluk.map(a => [esc(a.zaman), esc(a.kullanici), esc(a.alan), esc(a.eski), `<small>${esc(a.yeni)}</small>`, `<small>${esc(a.gerekce || '')}</small>`]), 'Kayıt yok.'), 'iki')}
      </div>`;
    const ydK = v.vfKural === 'Tahsilatta';   // yurtdışı: robot ve "Şimdi Kes" düğmeleri yok (vade farkı tahsilatta)
    $('#tkDAltSerit').innerHTML = `<button type="button" class="tk-dgm ana" data-d="not"><i class="fas fa-pen"></i>Not Ekle</button>${ydK ? '' : d.takip.haric ? '<button type="button" class="tk-dgm" data-d="dahil"><i class="fas fa-robot"></i>Robota Dahil Et</button>' : '<button type="button" class="tk-dgm" data-d="haric" title="Bu ay vade farkı robotunun dışında bırak"><i class="fas fa-robot"></i>Robot Dışı (Bu Ay)</button><button type="button" class="tk-dgm" data-d="haricSurekli" title="Her ay vade farkı robotunun dışında bırak"><i class="fas fa-robot"></i>Robot Dışı (Sürekli)</button>'}<button type="button" class="tk-dgm${v.satisDurdu ? '' : ' uyari'}" data-d="satis"><i class="fas ${v.satisDurdu ? 'fa-play' : 'fa-ban'}"></i>${v.satisDurdu ? 'Satışı Yeniden Aç' : 'Satış Durdur'}</button><button type="button" class="tk-dgm" data-d="mutabakat"><i class="fas fa-envelope-circle-check"></i>Mutabakat Gönder (${esc(KANAL)})</button>${ydK ? `<span class="not" title="Yurtdışı müşteride vade farkı yalnız tahsilat işlenirken kesilir (Yeni Tahsilat › Vade Farkı Faturasını Da Kes)"><i class="fas fa-circle-info"></i> vade farkı tahsilatta kesilir</span>` : `<button type="button" class="tk-dgm" data-d="vfkes"${v.bugunVf > 0.005 ? '' : ' disabled'}><i class="fas fa-file-invoice-dollar"></i>Şimdi Vade Farkı Kes${v.bugunVf > 0.005 ? ` (${tl2(v.bugunVf)})` : ''}</button>`}`
      + `<span class="bos"></span><a class="tk-dgm" href="/Rapor117?sekme=yeni&kod=${encodeURIComponent(DK.kod)}"><i class="fas fa-hand-holding-dollar"></i>Tahsilat Al</a><a class="tk-dgm" href="/Rapor20?bpCode=${encodeURIComponent(DK.kod)}" target="_blank" rel="noopener"><i class="fas fa-file-invoice"></i>Cari Ekstre</a><a class="tk-dgm" href="/CariKart/Musteri?kod=${encodeURIComponent(DK.kod)}" target="_blank" rel="noopener"><i class="fas fa-address-card"></i>Cari Kart</a><button type="button" class="tk-dgm" data-d="kapat">Kapat</button>`;
  }
  async function detayAc(kod) {
    const d = await fetch('/Tahsilat/MusteriDetay?kod=' + encodeURIComponent(kod)).then(r => r.json()).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message || 'Müşteri detayı okunamadı.')); return; }
    DK = { kod, d }; detayCiz(); dPencere.hidden = false;
  }
  const detayKapat = () => { dPencere.hidden = true; DK = null; };
  // ---------- müşteri işlemleri: satış durdur · mutabakat · şimdi vade farkı kes ----------
  async function islemSonu(d, ikon) { mesaj(d.success ? 'success' : 'danger', `<i class="fas ${ikon} me-1"></i>` + esc(d.message)); if (d.success) { await yukle(); if (DK) await detayAc(DK.kod); } }
  async function satisDurdur(kod) {
    const k = cariBul(kod); if (!k) return; let neden = '';
    if (!k.vf.satisDurdu) { neden = prompt(`${k.kod} ${k.kisaAd} için SATIŞ DURDUR uyarısı verilecek.\nNeden:`, durdurNeden(k) || ''); if (neden === null) return; if (!neden.trim()) { mesaj('danger', 'Satış durdurma nedeni yazın.'); return; } }
    else if (!confirm(`${k.kod} ${k.kisaAd} için satış yeniden açılsın mı?`)) return;
    islemSonu(await post('/Tahsilat/SatisDurdur', { Kod: kod, Durdur: !k.vf.satisDurdu, Neden: neden }).catch(() => ({ success: false, message: 'Ağ hatası.' })), 'fa-ban');
  }
  async function mutabakatGonder(kodlar) {
    if (!kodlar.length) return; const adlar = kodlar.map(kod => { const k = cariBul(kod); return k ? k.kisaAd : kod; });
    if (!confirm(`${kodlar.length} müşteriye ${KANAL === 'İkisi' ? 'e-posta ve WhatsApp' : KANAL} ile mutabakat gönderilsin mi?\n\n${adlar.slice(0, 8).join(', ')}${adlar.length > 8 ? ' …' : ''}\n\nBakiye, kayıtlı mutabakat yetkilisine yanıt bağlantısıyla gider.`)) return;
    const d = await post('/Tahsilat/MutabakatGonder', { Kodlar: kodlar, Kanal: KANAL }).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (d.success) SECM.clear(); islemSonu(d, 'fa-envelope-circle-check');
  }
  async function vfSimdiKes(kodlar) {
    const l = kodlar.map(cariBul).filter(k => k && k.vf.bugunVf > 0.005); if (!l.length) return;
    if (!confirm(`${l.length} müşteriye bugüne kadar olan vade farkı faturası kesilsin mi?\n\nMatrah ${tl2(topla(l, k => k.vf.bugunVf))} (KDV hariç)\nMüşteri başına tek fatura kesilir, carisine borç yazılır; ay sonu kesimi kalan günlerden devam eder.`)) return;
    const d = await post('/Tahsilat/MusteriVadeFarki', { Kodlar: l.map(k => k.kod) }).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (d.success) SECM.clear(); islemSonu(d, 'fa-file-invoice-dollar');
  }
  dPencere.addEventListener('click', e => {
    if (e.target === dPencere || e.target.closest('[data-d="kapat"]')) { detayKapat(); return; } if (!DK) return;
    const sl = e.target.closest('[data-notsil]'); if (sl) { e.preventDefault(); notSil(+sl.dataset.notsil); return; }
    if (e.target.closest('[data-d="not"]')) { notAc(DK.kod); return; } if (e.target.closest('[data-d="haric"]')) { robotHaric(DK.kod, true, false); return; } if (e.target.closest('[data-d="haricSurekli"]')) { robotHaric(DK.kod, true, true); return; } if (e.target.closest('[data-d="dahil"]')) { robotHaric(DK.kod, false); return; }
    if (e.target.closest('[data-d="satis"]')) satisDurdur(DK.kod); else if (e.target.closest('[data-d="mutabakat"]')) mutabakatGonder([DK.kod]); else if (e.target.closest('[data-d="vfkes"]')) vfSimdiKes([DK.kod]);
  });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !dPencere.hidden) detayKapat(); });

  function cizOzet() {
    const l = cariler(), bk = bankalar().filter(bankaAcik), vf = vadeFarklilar().filter(t => !t.vadeFarkiFaturaNo), taslak = tahsilatlar().filter(t => !t.islendi);
    const kutu = (renk, ad, deger, alt, git) => `<div style="--k:${renk}"${git ? ` data-git="${git}" title="İlgili sekmeyi aç"` : ''}><div class="l">${ad}</div><div class="v">${deger}</div><div class="s">${alt}</div></div>`;
    // 19 Kas 2026 (kullanıcı: "Özet'i yaptığımız bütün sekmelere göre düzenle"): kutular ve bekleyen işler 16 sekmenin hepsini kapsar; her satır ilgili sekmeye gider (Sekme sütunu)
    const SEK = Object.fromEntries($$('#tkSekmeler button').map(b => [b.dataset.p, b.textContent.trim()])), ay = (K.bugun || '').slice(0, 7), bk2 = bl => { const ac = bl.filter(bankaAcik); return ac.length ? `${ac.length} · ${tl(topla(ac, h => h.tutar))}` : '—'; };
    const kp = k => k.kapama || {}, kur = k => k.kur || {}, buAyBelge = (K.faturalar || []).filter(f => String(f.tarihIso || '').startsWith(ay));
    const aySonuGelecek = kalemler(l).filter(x => x.gun >= 0 && x.vade <= K.aySonu), uyumsuz = l.filter(k => (k.uyum || {}).durum === 'Uyumsuz'), gorevli = l.filter(k => k.uyumGorev), degBekleyen = l.filter(k => kur(k).rAdet > 0 && Math.abs(kur(k).rFark || 0) > 0.5), kapanabilir = l.filter(k => kp(k).oneri > 0);
    $('#tkKpi').innerHTML = [
      kutu('#0369a1', 'Açık Alacak', tl(topla(l, k => k.acikToplam)), `${l.length} müşteri · ${topla(l, k => k.acikAdet)} fatura`, 'cari'),
      kutu('#dc2626', 'Gecikmiş', tl(topla(l, k => k.gecmis)), `${l.filter(k => k.gecmis > 0).length} müşteri · ${topla(l, k => k.gecmisAdet)} fatura`, 'acik:gecmis'),
      kutu('#f59e0b', 'Bu Hafta Vadesi Gelen', tl(topla(l, k => k.buHafta)), `${l.filter(k => k.buHafta > 0).length} müşteri`, 'hafta'),
      kutu('#d97706', `Ay Sonuna Kadar (${trh(K.aySonu)})`, tl(topla(aySonuGelecek, x => x.tl)), `${aySonuGelecek.length} fatura · bu ay tahsil edilmesi gereken`, 'acik'),
      kutu('#15803d', 'Son 30 Gün Tahsilat', tl(topla(l, k => k.son30Tahsilat)), `${topla(l, k => k.son30Adet)} tahsilat`, 'cari'),
      kutu('#7c3aed', 'İçerideki Çek / Senet', tl(topla(l, k => k.cek)), `${topla(l, k => k.cekAdet)} belge`, 'cari'),
      kutu('#b45309', 'Müşteri Bekleyen Banka Girişi', tl(topla(bk, h => h.tutar)), `${bk.length} hareket`, 'banka'),
      kutu('#be123c', 'Kesilmemiş Vade Farkı (Gecikmeli)', tl2(topla(vf, t => t.vadeFarki)), `${vf.length} tahsilat · KDV hariç`, 'vade'),
      kutu('#9f1239', `Ay Sonu Kesilecek VF (${trh(K.aySonu)})`, tl2(topla(l, k => k.vf.aySonuVf)), `${l.filter(k => k.vf.aySonuVf > 0.005).length} müşteri · avans virmanı düşülmüş`, 'vftakip:vfGecmis'),
      kutu('#6d28d9', 'Kur Değerlemesi Bekleyen (R)', tl2(topla(degBekleyen, k => kur(k).rFark)), `${degBekleyen.length} müşteri · muhasebe virmanı`, 'vftakip'),
      kutu('#0f766e', 'Kapama Robotu · Kapatılabilir', tl(topla(kapanabilir, k => kp(k).oneriTl)), `${kapanabilir.length} müşteri · eşleşmemiş ödeme`, 'kapama'),
      kutu('#b91c1c', 'Muhasebe Uyumu · Uyumsuz', String(uyumsuz.length), `${gorevli.length} açık düzeltme görevi · ${tl(topla(uyumsuz, k => Math.abs((k.uyum || {}).fark || 0)))} fark`, 'cari'),
      kutu('#475569', 'Taslak Tahsilat', String(taslak.length), `${tl(topla(taslak, t => t.tutarTl))} · işleme alınmadı`, 'liste'),
      kutu('#334155', `Bu Ay Kesilen Fark Belgeleri (${K.surec.donemAd})`, tl(topla(buAyBelge, f => f.toplam)), `${buAyBelge.filter(f => f.tur === 'Vade Farkı').length} vade farkı · ${buAyBelge.filter(f => f.tur === 'Kur Farkı').length} kur farkı · ${buAyBelge.filter(f => f.degerleme).length} değerleme`, 'fatura')
    ].join('');
    const isler = [
      { s: 'cari', ad: 'Muhasebe Uyumu Uyumsuz Müşteri (beş kaynak)', adet: uyumsuz.length, tutar: topla(uyumsuz, k => Math.abs((k.uyum || {}).fark || 0)), git: 'cari' },
      { s: 'cari', ad: 'Açık Bakiye Düzeltme Görevi', adet: gorevli.length, tutar: topla(gorevli, k => Math.abs((k.uyum || {}).fark || 0)), git: 'cari', not: gorevli.filter(k => k.uyumGorev.gecikti).length ? `${gorevli.filter(k => k.uyumGorev.gecikti).length} termini geçti` : '' },
      { s: 'cari', ad: 'Gecikmiş Alacağı Olan Müşteri', adet: l.filter(k => k.gecmis > 0).length, tutar: topla(l, k => k.gecmis), git: 'cari' },
      { s: 'cari', ad: 'Son 30 Günde Tahsilatı Olmayan Müşteri', adet: l.filter(k => k.son30Adet === 0 && k.acikToplam > 0).length, tutar: topla(l.filter(k => k.son30Adet === 0 && k.acikToplam > 0), k => k.acikToplam), git: 'cari' },
      { s: 'kartlar', ad: 'Riskli Müşteri', adet: l.filter(k => k.risk === 'Riskli').length, tutar: topla(l.filter(k => k.risk === 'Riskli'), k => k.acikToplam), git: 'kartlar' },
      { s: 'kartlar', ad: 'Limiti Aşan Müşteri', adet: l.filter(k => k.vf.limitDurum === 'Limit Aşıldı').length, tutar: topla(l, k => k.vf.limitAsim), git: 'vftakip:vfLimit' },
      { s: 'acik', ad: 'Vadesi Geçmiş Açık Fatura', adet: topla(l, k => k.gecmisAdet), tutar: topla(l, k => k.gecmis), git: 'acik:gecmis' },
      { s: 'acik', ad: `Ay Sonuna Kadar Vadesi Gelecek Fatura (${trh(K.aySonu)})`, adet: aySonuGelecek.length, tutar: topla(aySonuGelecek, x => x.tl), git: 'acik' },
      { s: 'hafta', ad: 'Bu Hafta Vadesi Gelen Fatura', adet: kalemler(l).filter(x => x.gun >= 0 && x.gun <= 7).length, tutar: topla(l, k => k.buHafta), git: 'hafta' },
      { s: 'banka', ad: 'Müşterisi Seçilmemiş Banka Girişi', adet: bk.length, tutar: topla(bk, h => h.tutar), git: 'banka' },
      { s: 'liste', ad: 'İşleme Alınmamış Taslak Tahsilat', adet: taslak.length, tutar: topla(taslak, t => t.tutarTl), git: 'liste' },
      { s: 'vade', ad: 'Gecikmeli Tahsilat · Vade Farkı Kesilmemiş', adet: vf.length, tutar: topla(vf, t => t.vadeFarki), git: 'vade' },
      { s: 'vftakip', ad: `Ay Sonu Kesilecek Vade Farkı (${trh(K.aySonu)})`, adet: l.filter(k => k.vf.aySonuVf > 0.005).length, tutar: topla(l, k => k.vf.aySonuVf), git: 'vftakip:vfGecmis' },
      { s: 'vftakip', ad: 'Yurtdışı · Tahsilatta Kesilecek Vade Farkı (bugüne doğan)', adet: l.filter(k => tahsilatta(k) && k.vf.bugunVf > 0.005).length, tutar: topla(l.filter(tahsilatta), k => k.vf.bugunVf), git: 'vftakip' },
      { s: 'vftakip', ad: 'Kur Değerlemesi Bekleyen R Faturası Olan Müşteri', adet: degBekleyen.length, tutar: topla(degBekleyen, k => kur(k).rFark), git: 'vftakip' },
      { s: 'vftakip', ad: 'Vade Farkı Sonrası Ödeme Yapmayan Müşteri', adet: l.filter(k => (k.vf.sonrasiDurum || '').startsWith('Ödeme Yok')).length, tutar: topla(l.filter(k => (k.vf.sonrasiDurum || '').startsWith('Ödeme Yok')), k => k.vf.acik), git: 'vftakip:vfOdemeYok' },
      { s: 'vftakip', ad: 'Satışı Durdurulan Müşteri', adet: l.filter(k => k.vf.satisDurdu).length, tutar: topla(l.filter(k => k.vf.satisDurdu), k => k.vf.mizanBakiye), git: 'vftakip' },
      { s: 'kapama', ad: 'Kapama Robotu · Eşleşmemiş Ödemesi Kapatılabilir Müşteri', adet: kapanabilir.length, tutar: topla(kapanabilir, k => kp(k).oneriTl), git: 'kapama' },
      { s: 'fatura', ad: `Bu Ay Kesilen Fark Faturası / Değerleme (${K.surec.donemAd})`, adet: buAyBelge.length, tutar: topla(buAyBelge, f => f.toplam), git: 'fatura' },
      { s: 'surec', ad: `Bu Ay Aranmayan Müşteri (${K.surec.donemAd})`, adet: l.filter(k => aranacakMi(k) && !k.takip.buAyNot).length, tutar: topla(l.filter(k => aranacakMi(k) && !k.takip.buAyNot), k => k.gecmis + k.vf.aySonuGelecek), git: 'surec:surecAranmayan' },
      { s: 'surec', ad: 'Vade Farkı Robotu Dışında Bırakılan Müşteri', adet: l.filter(k => k.takip.haric).length, tutar: topla(l.filter(k => k.takip.haric), k => k.vf.bugunVf), git: 'surec' },
      { s: 'surec', ad: 'WhatsApp\'tan Gelen Belge Talebi (Gönderim Bekliyor)', adet: talepler().filter(t => t.durum === 'Bekliyor' && l.some(k => k.kod === t.cariKodu)).length, tutar: 0, git: 'surec:talep' }];
    tablo('is', [M('s', 'Sekme', x => SEK[x.s] || x.s, { h: x => `<span class="rt-durum rt-bilgi">${esc(SEK[x.s] || x.s)}</span>`, sirasiz: 1 }), M('ad', 'İş', x => x.ad, { h: x => `<b>${esc(x.ad)}</b>${x.not ? `<small class="gec">${esc(x.not)}</small>` : ''}`, sirasiz: 1 }), { k: 'adet', ad: 'Adet', t: 'p', v: x => x.adet, h: x => x.adet || tire, sirasiz: 1, top: x => x.adet, dipH: L => topla(L, x => x.adet) || tire }, { k: 'tutar', ad: 'Tutar\u00A0(₺)', t: 'p', v: x => x.tutar, h: x => x.tutar ? tl(x.tutar) : tire, sirasiz: 1 }], isler, { dipAd: L => `Toplam · ${L.filter(x => x.adet).length} iş`, secimsiz: true, satir: x => `class="tik${x.adet ? '' : ' sonuk'}" data-git="${x.git}"` });
    { const n = $('#tkOzNot-is'); if (n) n.textContent = `${isler.filter(x => x.adet).length} iş bekliyor`; }
    // ---------- 19 Kas 2026 gece (kullanıcı: "Tahsilat Özet'i İhracat Özet'i gibi geliştir"): İhracat'taki gibi çok kartlı özet — bölge × sınıf, vade kovaları, aylık akış, risk / limit / satış, banka, vade farkı ayları, açık görevler ----------
    const oz = (id, html, not) => { const e = $('#tkOz-' + id); if (e) e.innerHTML = html; const n = $('#tkOzNot-' + id); if (n && not != null) n.textContent = not; };
    const gitTr = (git, hucreler) => `<tr class="tik" data-git="${git}">${hucreler.map(h => `<td>${h}</td>`).join('')}</tr>`;
    // dip (20 Kas 2026): toplam satırı tfoot'ta — kart içi kaydırmada altta sabit
    const ozTablo = (bas, rows, bos, dip) => `<table class="tk-dtablo" data-dinamik="kapali"><thead><tr>${bas.map(b => `<th>${b}</th>`).join('')}</tr></thead><tbody>${rows.join('') || `<tr><td colspan="${bas.length}" class="bos">${bos}</td></tr>`}</tbody>${rows.length && dip ? `<tfoot>${dip}</tfoot>` : ''}</table>`;
    // Bölge × Cari Sınıfı
    { const grp = new Map(); l.forEach(k => { const key = `${k.bolge || '—'}|${k.sinif || 'Sınıfsız'}`; if (!grp.has(key)) grp.set(key, { bolge: k.bolge || '—', sinif: k.sinif || 'Sınıfsız', l: [] }); grp.get(key).l.push(k); });
      const rows = [...grp.values()].sort((a, b) => ((a.bolge === 'Yurtdışı') - (b.bolge === 'Yurtdışı')) || a.bolge.localeCompare(b.bolge, TR) || a.sinif.localeCompare(b.sinif, TR)).map(g => gitTr('cari', [`<b>${esc(g.bolge)}</b>`, esc(g.sinif), g.l.length, `<b>${tl(topla(g.l, k => k.acikToplam))}</b>`, g.l.some(k => k.gecmis > 0) ? `<span class="gec">${tl(topla(g.l, k => k.gecmis))}</span>` : tire, tl(topla(g.l, k => k.son30Tahsilat)), topla(g.l, k => k.vf.aySonuVf) > 0.005 ? tl2(topla(g.l, k => k.vf.aySonuVf)) : tire, g.l.filter(k => (k.uyum || {}).durum === 'Uyumsuz').length || tire]));
      const T2 = `<tr><td colspan="2"><b>Toplam</b></td><td><b>${l.length}</b></td><td><b>${tl(topla(l, k => k.acikToplam))}</b></td><td><b class="gec">${tl(topla(l, k => k.gecmis))}</b></td><td><b>${tl(topla(l, k => k.son30Tahsilat))}</b></td><td><b>${tl2(topla(l, k => k.vf.aySonuVf))}</b></td><td><b>${uyumsuz.length || tire}</b></td></tr>`;
      oz('bolge', ozTablo(['Bölge', 'Cari Sınıfı', 'Müşteri', 'Açık Alacak', 'Gecikmiş', 'Son 30 Gün Tahsilat', 'Ay Sonu VF', 'Uyumsuz'], rows, 'Müşteri yok.', T2), `${grp.size} bölge · sınıf`); }
    // Vade kovaları (açık fatura kalemleri)
    { const kal = kalemler(l), kova = (ad, f, git) => { const x = kal.filter(f); return gitTr(git, [`<b>${ad}</b>`, x.length || tire, new Set(x.map(y => y.kod)).size || tire, x.length ? `<b>${tl(topla(x, y => y.tl))}</b>` : tire, topla(x, y => y.vf) > 0.005 ? tl2(topla(x, y => y.vf)) : tire]); };
      const rows = [kova('90+ gün gecikmiş', x => x.gun < -90, 'acik:gecmis'), kova('61–90 gün gecikmiş', x => x.gun >= -90 && x.gun < -60, 'acik:gecmis'), kova('31–60 gün gecikmiş', x => x.gun >= -60 && x.gun < -30, 'acik:gecmis'), kova('1–30 gün gecikmiş', x => x.gun >= -30 && x.gun < 0, 'acik:gecmis'), kova('Bu hafta (0–7 gün)', x => x.gun >= 0 && x.gun <= 7, 'hafta'), kova('8–30 gün', x => x.gun > 7 && x.gun <= 30, 'acik'), kova('31–60 gün', x => x.gun > 30 && x.gun <= 60, 'acik'), kova('60+ gün', x => x.gun > 60, 'acik')];
      const dipV = `<tr><td><b>Toplam</b></td><td><b>${kal.length}</b></td><td><b>${new Set(kal.map(y => y.kod)).size}</b></td><td><b>${tl(topla(kal, y => y.tl))}</b></td><td><b>${tl2(topla(kal, y => y.vf))}</b></td></tr>`;
      oz('vade', ozTablo(['Kova', 'Fatura', 'Müşteri', 'Kalan\u00A0(₺)', 'Bugüne VF\u00A0(₺)'], kal.length ? rows : [], 'Açık fatura yok.', dipV), `${kal.length} açık fatura · ${tl(topla(kal, y => y.tl))}`); }
    // Aylık tahsilat akışı (işlenmiş tahsilatlar, tahsilat ayına göre; dönem seçimine uyar)
    { const T = islenenler(), ays = aylar(), ym = s => String(s || '').slice(0, 7), sat = T.flatMap(t => t.satirlar.map(s => Object.assign({ ay: ym(t.tarihIso) }, s)));
      const gos = ays.filter(a => sat.some(s => s.ay === a) || a === (K.bugun || '').slice(0, 7)), xs = sat.filter(s => gos.includes(s.ay)), yt = f => tl(topla(xs.filter(f), s => s.tutarTl));
      const dipA = `<tr><td><b>Toplam</b></td><td><b>${new Set(T.filter(t => gos.includes(ym(t.tarihIso))).map(t => t.no)).size || tire}</b></td><td><b>${tl(topla(xs, s => s.tutarTl))}</b></td><td><b>${yt(s => /Havale|EFT|Kart/.test(s.yontem))}</b></td><td><b>${yt(s => s.yontem === 'Nakit')}</b></td><td><b>${yt(s => s.yontem === 'Çek' || s.yontem === 'Senet')}</b></td></tr>`;
      const rows = gos.map(a => { const x = sat.filter(s => s.ay === a), y = f => tl(topla(x.filter(f), s => s.tutarTl)); return gitTr('aylik', [`<b>${ayEtiket(a)}</b>`, new Set(T.filter(t => ym(t.tarihIso) === a).map(t => t.no)).size || tire, x.length ? `<b>${tl(topla(x, s => s.tutarTl))}</b>` : tire, y(s => /Havale|EFT|Kart/.test(s.yontem)), y(s => s.yontem === 'Nakit'), y(s => s.yontem === 'Çek' || s.yontem === 'Senet')]); });
      oz('ay', ozTablo(['Ay', 'Tahsilat', 'Toplam\u00A0(₺)', 'Havale · Kart', 'Nakit', 'Çek · Senet'], rows, 'Dönemde işlenmiş tahsilat yok.', dipA), `${T.length} işlenmiş tahsilat · ${donemMetni()}`); }
    // Risk · limit · satış durumu
    { const say = (ad, f, git, tutarF) => { const x = l.filter(f); return gitTr(git, [`<b>${ad}</b>`, x.length || tire, x.length ? tl(topla(x, tutarF || (k => k.acikToplam))) : tire]); };
      const rows = [`<tr><td colspan="3" class="bos" style="text-align:left;color:#334155;font-weight:800">RİSK</td></tr>`, say('Riskli', k => k.risk === 'Riskli', 'kartlar'), say('İzlemede', k => k.risk === 'İzlemede', 'kartlar'), say('Normal', k => k.risk !== 'Riskli' && k.risk !== 'İzlemede', 'kartlar'),
        `<tr><td colspan="3" class="bos" style="text-align:left;color:#334155;font-weight:800">KREDİ LİMİTİ</td></tr>`, say('Limit Aşıldı', k => k.vf.limitDurum === 'Limit Aşıldı', 'vftakip:vfLimit', k => k.vf.limitAsim), say('Limite Yakın', k => /Yakın/.test(k.vf.limitDurum || ''), 'kartlar'), say('Limit Yok', k => k.vf.limitTur === 'Yok', 'kartlar'),
        `<tr><td colspan="3" class="bos" style="text-align:left;color:#334155;font-weight:800">SATIŞ</td></tr>`, say('Satışı Durdurulan', k => k.vf.satisDurdu, 'vftakip', k => k.vf.mizanBakiye), say('Vade Farkı Sonrası Ödeme Yok', k => (k.vf.sonrasiDurum || '').startsWith('Ödeme Yok'), 'vftakip:vfOdemeYok', k => k.vf.acik)];
      oz('risk', ozTablo(['Durum', 'Müşteri', 'Tutar\u00A0(₺)'], l.length ? rows : [], 'Müşteri yok.', `<tr><td><b>Toplam</b></td><td><b>${l.length}</b></td><td><b>${tl(topla(l, k => k.acikToplam))}</b></td></tr>`), `${l.filter(k => k.risk === 'Riskli').length} riskli · ${l.filter(k => k.vf.limitDurum === 'Limit Aşıldı').length} limit aşımı · ${l.filter(k => k.vf.satisDurdu).length} satış durdu`); }
    // Banka girişleri (banka bazında; süzgeçli küme)
    { const bl = bankalar(), bk = [...new Set(bl.map(h => h.banka || '—'))].sort((a, b) => a.localeCompare(b, TR));
      const rows = bk.map(b => { const x = bl.filter(h => (h.banka || '—') === b), ac = x.filter(bankaAcik); return gitTr('banka', [`<b>${esc(b)}</b>`, x.length, `<b>${tl(topla(x, h => h.tutar))}</b>`, ac.length ? `<span class="gec">${ac.length} · ${tl(topla(ac, h => h.tutar))}</span>` : tire, x.length - ac.length || tire, x.filter(h => h.pb !== 'TL').length || tire]); });
      oz('banka', ozTablo(['Banka', 'Giriş', 'Tutar\u00A0(₺)', 'Müşteri Bekliyor', 'Müşteri Seçildi', 'Dövizli'], rows, 'Son 90 günde banka girişi yok.', `<tr><td><b>Toplam</b></td><td><b>${bl.length}</b></td><td><b>${tl(topla(bl, h => h.tutar))}</b></td><td><b>${bk2(bl)}</b></td><td><b>${bl.filter(h => !bankaAcik(h)).length}</b></td><td><b>${bl.filter(h => h.pb !== 'TL').length}</b></td></tr>`), `${bl.length} giriş · son 90 gün · ${bl.filter(bankaAcik).length} müşteri bekliyor`); }
    // Vade farkı ay sonu kesimleri (bu yıl)
    { const va = K.vfAylar || []; const rows = va.map(a => gitTr('fatura', [`<b>${esc(a.ad)}</b>`, a.kesildi ? rozet('Kesildi', 'ok') : a.gecti ? rozet('Kesilmedi', 'hata') : a.buAy ? rozet('Bu Ay', 'bilgi') : rozet('Gelmedi', 'bekle'), a.adet || tire, a.musteri || tire, a.tutar ? `<b>${tl(a.tutar)}</b>` : tire, a.sonTarih ? esc(a.sonTarih) : tire]));
      oz('vf', ozTablo(['Ay', 'Ay Sonu Kesimi', 'Fatura', 'Müşteri', 'Tutar\u00A0(₺)', 'Son Kesim'], rows, 'Kayıt yok.', `<tr><td colspan="2"><b>Toplam</b></td><td><b>${topla(va, a => a.adet || 0) || tire}</b></td><td></td><td><b>${tl(topla(va, a => a.tutar || 0))}</b></td><td></td></tr>`), `${va.filter(a => a.kesildi).length} ayda kesildi · ${tl(topla(va, a => a.tutar || 0))}`); }
    // Açık düzeltme görevleri (Bakiye Karşılaştırması)
    { const g = l.filter(k => k.uyumGorev).sort((a, b) => (b.uyumGorev.gecikti - a.uyumGorev.gecikti) || (a.uyumGorev.termin || '').localeCompare(b.uyumGorev.termin || ''));
      const rows = g.map(k => `<tr class="tik" data-uyum="${esc(k.kod)}"><td><b>${esc(k.kod)}</b> ${esc(k.kisaAd)}</td><td>#${k.uyumGorev.id} ${rozet(k.uyumGorev.durum, k.uyumGorev.durum === 'Düzeltildi' ? 'bilgi' : 'bekle')}</td><td>${esc(k.uyumGorev.atanan)}</td><td class="${k.uyumGorev.gecikti ? 'gec' : ''}">${k.uyumGorev.termin ? trh(k.uyumGorev.termin) : tire}${k.uyumGorev.gecikti ? ' · gecikti' : ''}</td><td>${esc(k.uyumGorev.oncelik || '')}</td><td>${(k.uyum || {}).fark ? tl(Math.abs(k.uyum.fark)) : tire}</td></tr>`);
      oz('gorev', ozTablo(['Müşteri', 'Görev', 'Atanan', 'Termin', 'Öncelik', 'Fark\u00A0(₺)'], rows, 'Açık düzeltme görevi yok.', `<tr><td colspan="5"><b>Toplam · ${g.length} görev</b></td><td><b>${tl(topla(g, k => Math.abs((k.uyum || {}).fark || 0)))}</b></td></tr>`), `${g.length} açık görev · ${g.filter(k => k.uyumGorev.gecikti).length} termini geçti`); }
    const grup = anahtarlar => { const g = {}; l.forEach(k => anahtarlar(k).forEach(a => { const p = g[a] || (g[a] = { ad: a, n: 0, bakiye: 0, gecmis: 0, gecikmeli: 0, buHafta: 0, acik: 0, cek: 0, son30: 0, riskli: 0 }); p.n++; p.bakiye += k.bakiye; p.gecmis += k.gecmis; if (k.gecmis > 0) p.gecikmeli++; p.buHafta += k.buHafta; p.acik += k.acikToplam; p.cek += k.cek; p.son30 += k.son30Tahsilat; if (k.risk === 'Riskli') p.riskli++; })); return Object.values(g); };
    const ozetKol = (ad, alan) => [M('ad', ad, p => p.ad, { h: p => `<b>${esc(p.ad)}</b>` }), { k: 'n', ad: 'Müşteri', t: 'p', v: p => p.n, h: p => p.n, top: p => p.n, dipH: x => topla(x, p => p.n) }, P('acik', 'Açık Alacak\u00A0(₺)', p => p.acik), P('gecmis', 'Gecikmiş\u00A0(₺)', p => p.gecmis, { h: p => p.gecmis ? `<span class="gec">${tl(p.gecmis)}</span><small>${p.gecikmeli} müşteri</small>` : tire }), P('buHafta', 'Bu Hafta\u00A0(₺)', p => p.buHafta), P('cek', 'Çek / Senet\u00A0(₺)', p => p.cek), P('son30', 'Son 30 Gün Tahsilat\u00A0(₺)', p => p.son30, { sinif: 'ok' }), { k: 'riskli', ad: 'Riskli', t: 'p', v: p => p.riskli, h: p => p.riskli || tire, top: p => p.riskli, dipH: x => topla(x, p => p.riskli) || '' }].map(c => Object.assign(c, { _alan: alan }));
    tablo('sirket', ozetKol('Şirket', 'sirket'), grup(k => [k.sirket]), { sira: { k: 'acik', y: -1 }, satir: p => `class="tik" data-suz="sirket" data-deger="${esc(p.ad)}"` });
    // 20 Kas 2026 (alt toplam): birden çok temsilcisi olan müşteri her temsilcide sayıldığından dip satırı satırların toplamı değil, müşteri kümesinin kendi toplamıdır (çift sayım yok)
    const tmsDip = { n: () => l.length, acik: () => tl(topla(l, k => k.acikToplam)), gecmis: () => tl(topla(l, k => k.gecmis)), buHafta: () => tl(topla(l, k => k.buHafta)), son30: () => tl(topla(l, k => k.son30Tahsilat)) };
    tablo('temsilci', ozetKol('Temsilci', 'temsilci').filter(c => !['cek', 'riskli'].includes(c.k)).map(c => tmsDip[c.k] ? Object.assign(c, { top: c.top || c.v, dipH: tmsDip[c.k] }) : c), grup(k => (k.temsilci || '').split(', ').filter(Boolean).length ? (k.temsilci || '').split(', ').filter(Boolean) : ['Temsilci Yok']), { sira: { k: 'acik', y: -1 }, satir: p => `class="tik" data-suz="temsilci" data-deger="${esc(p.ad)}"` });
  }
  // ---------- AYLIK TAHSİLATLAR (19 Kas 2026): yönteme göre · vade ayına göre alınan çek / senet · tahsilatı yapan kişiye göre ----------
  const YONTEMLER = ['Nakit', 'Çek', 'Senet', 'Havale / EFT', 'Kredi Kartı'];
  function cizAylik() {
    const ays = aylar(), ayK = new Set(ays), T = islenenler(), ym = s => String(s || '').slice(0, 7);
    const sat = T.flatMap(t => t.satirlar.map(s => Object.assign({ tahsilatYm: ym(t.tarihIso), vadeYm: ym(s.tarihIso), kullanici: t.kullanici, cariKodu: t.cariKodu, cariAd: t.cariAd, no: t.no }, s)));
    const yontemler = [...YONTEMLER, ...sat.map(s => s.yontem)].filter((y, i, a) => y && a.indexOf(y) === i);
    const hucre = (l, birim) => l.length ? `${DV && dovizli(l) ? `<span title="${esc(tl(topla(l, s => s.tutarTl)))}">${esc(pbTop(l, s => s.tutar))}</span>` : tl(topla(l, s => s.tutarTl))}<small>${l.length} ${birim}</small>` : tire;
    const satir = (ad, f, alan, birim, alt) => { const L = sat.filter(f), li = y => y ? L.filter(s => s[alan] === y) : L.filter(s => ayK.has(s[alan])); return { ad, alt, deger: y => topla(L.filter(s => s[alan] === y), s => s.tutarTl), h: y => hucre(li(y), birim), liste: li }; };
    // 19 Kas 2026 (kullanıcı: "Aylık Tahsilatlar'ı İhracat Finansal Takibi ile senkronize et; yurt içi ve yurt dışı tahsilatlar olarak birleşimi raporla"):
    //   Pazar matrisi — Yurtiçi (tahsilat kaydı) · Yurtdışı tahsilat kaydı · Yurtdışı İHRACAT BANKA GİRİŞİ (İhracat Finansal Takibi'nin banka tahsilatları, K.ihracatTahsilat, TL karşılığı) · Yurtdışı toplam · genel toplam.
    //   İhracat banka girişi süzgece müşteri cari kartıyla uyar (kart bulunamazsa yalnız süzgeç yokken); Alacak Takibi'nde zaten yurtdışı müşteriler olduğu için aynı yurtdışı müşteri iki kaynakta da görünebilir — satır adında kaynak yazılıdır, çift sayım toplamda ayrı gösterilir.
    const bolgeOf = s => { const c = cariBul(s.cariKodu); return c ? c.bolge : ''; }; const satB = sat.map(s => Object.assign({ bolge: bolgeOf(s) }, s));
    const ihr = (K.ihracatTahsilat || []).filter(t => { const c = cariBul(t.musteriKodu); return (c ? suzUyar(c) : (!suzAktif() || (BOLGE_KILIT === 'Yurtdışı'))) && (!F.cari || t.musteriKodu === F.cari) && araUyar(t.musteriKodu, t.musteriAd, t.banka, t.referans, t.dosyaNo, t.belgeNo); })
      .map(t => ({ tahsilatYm: ym(t.tarihIso), tutarTl: t.tl, tutar: t.tutar, pb: t.pb, kalan: t.tutar, durum: t.durum, cariKodu: t.musteriKodu, yontem: 'Havale / EFT', bolge: 'Yurtdışı' }));
    const ihrLink = (ad, sekme) => IHRACAT ? `<a href="#" class="tk-link" data-git="ib${sekme[0].toUpperCase()}${sekme.slice(1)}" title="İhracat › banka tahsilatları"><b>${esc(ad)}</b></a>` : `<a href="/IhracatFinans?sekme=ib${sekme[0].toUpperCase()}${sekme.slice(1)}" target="_blank" rel="noopener" class="tk-link" title="İhracat Finansal Takibi › banka tahsilatları (yeni sekme)"><b>${esc(ad)}</b> <i class="fas fa-arrow-up-right-from-square" style="font-size:.56rem"></i></a>`;
    // sütunlar: seçili aylar + "Dönem Dışı" (Planlanan ↔ Gerçekleşen'deki gibi) → Toplam sütunu bütün kayıtları verir, Özet / İhracat kartıyla tutar
    const Fy2 = (L, y) => y === 'disi' ? L.filter(s => !ayK.has(s.tahsilatYm)) : y ? L.filter(s => s.tahsilatYm === y) : L;
    const satL = (ad, L, alt, ek) => Object.assign({ ad, alt, deger: y => topla(Fy2(L, y), s => s.tutarTl), h: y => hucre(Fy2(L, y), 'satır'), liste: y => Fy2(L, y) }, ek || {});
    // 19 Kas 2026 gece (kullanıcı): "Yurtdışı · İhracat Banka Girişi" satırı KALDIRILDI; yalnız Yurtiçi ve Yurtdışı tahsilat kaydı + genel toplam (ihracat banka girişleri İhracat Finansal Takibi'nde kalır, başlık notunda bağlantı)
    const yi = satB.filter(s => s.bolge === 'Yurtiçi'), yd = satB.filter(s => s.bolge === 'Yurtdışı');
    matris('aylikPazar', 'Pazar', [...ays, 'disi'], BOLGE_KILIT ? [satL(`${BOLGE_KILIT} · Tahsilat Kaydı`, satB.filter(s => s.bolge === BOLGE_KILIT), ''), satL('Toplam', satB.filter(s => s.bolge === BOLGE_KILIT), '', { sinif: 'toplam' })] : [   // kilitli bölgede (İhracat Finansal Takibi) yalnız o bölgenin satırı
      satL('Yurtiçi · Tahsilat Kaydı', yi, 'işlenen tahsilatlar (bölge Yurtiçi)'),
      satL('Yurtdışı · Tahsilat Kaydı', yd, 'işlenen tahsilatlar (bölge Yurtdışı)'),
      satL('Genel Toplam · Yurtiçi + Yurtdışı', yi.concat(yd), '', { sinif: 'toplam' })], 'Dönemde tahsilat yok.');
    $('#tkAylikPazarNot').innerHTML = `${yi.length} yurtiçi · ${yd.length} yurtdışı tahsilat kaydı · ${ihr.length} ihracat banka girişi ${ihrLink('İhracat Finansal Takibi', 'tamamlanan')}`;
    // 19 Kas 2026 gece (kullanıcı: "cari sınıfına göre bölünmüş ay ay tahsilat detayları; Tahsilat raporunda yurtiçi / yurtdışı ayrımına dikkat"): bölge › cari sınıfı satırları, bölge ara toplamı, genel toplam; kilitli bölgede (İhracat) yalnız o bölge
    const satS = satB.map(s => { const c = cariBul(s.cariKodu); return Object.assign({ sinif: (c && c.sinif) || 'Sınıfsız' }, s); });
    const bolgeler = BOLGE_KILIT ? [BOLGE_KILIT] : [...new Set(satS.map(s => s.bolge).filter(Boolean))].sort((a, b) => (a === 'Yurtdışı') - (b === 'Yurtdışı') || a.localeCompare(b, TR));
    const sinifSatirlar = [];
    bolgeler.forEach(bl => {
      const lb = satS.filter(s => s.bolge === bl), siniflar = [...new Set(lb.map(s => s.sinif))].sort((a, b) => a.localeCompare(b, TR));
      siniflar.forEach(sn => sinifSatirlar.push(satL(`${bl} · ${sn}`, lb.filter(s => s.sinif === sn), `${new Set(lb.filter(s => s.sinif === sn).map(s => s.cariKodu)).size} müşteri`)));
      if (siniflar.length) sinifSatirlar.push(satL(`${bl} Toplam`, lb, `${siniflar.length} cari sınıfı`, { sinif: 'toplam' }));
    });
    if (!BOLGE_KILIT && bolgeler.length > 1) sinifSatirlar.push(satL('Genel Toplam', satS.filter(s => s.bolge), '', { sinif: 'toplam' }));
    matris('aylikSinif', 'Bölge · Cari Sınıfı', [...ays, 'disi'], sinifSatirlar, 'Dönemde tahsilat yok.');
    $('#tkAylikSinifNot').textContent = `${new Set(satS.map(s => s.sinif)).size} cari sınıfı · ${bolgeler.join(' + ')}`;
    // 19 Kas 2026 gece (kullanıcı: "satışçı bazlı tahsilat detayı"): bölge › satış temsilcisi satırları ay ay (müşteri sayısı altında), bölge toplamı, genel toplam; temsilci müşteri cari kartından (yoksa "Atanmadı"); hücreler tıklanır
    const satT = satB.map(s => { const c = cariBul(s.cariKodu); return Object.assign({ temsilci: ((c && c.temsilci) || '').split(',')[0].trim() || 'Atanmadı' }, s); }), temsilciSatirlar = [];   // birden çok temsilcili müşteride ana (ilk) temsilci; tutar tek satıra yazılır, çift sayım yok
    bolgeler.forEach(bl => {
      const lb = satT.filter(s => s.bolge === bl), tms = [...new Set(lb.map(s => s.temsilci))].sort((a, b) => (a === 'Atanmadı') - (b === 'Atanmadı') || a.localeCompare(b, TR));
      tms.forEach(tm => temsilciSatirlar.push(satL(`${bl} · ${tm}`, lb.filter(s => s.temsilci === tm), `${new Set(lb.filter(s => s.temsilci === tm).map(s => s.cariKodu)).size} müşteri`)));
      if (tms.length) temsilciSatirlar.push(satL(`${bl} Toplam`, lb, `${tms.length} satış temsilcisi`, { sinif: 'toplam' }));
    });
    if (!BOLGE_KILIT && bolgeler.length > 1) temsilciSatirlar.push(satL('Genel Toplam', satT.filter(s => s.bolge), '', { sinif: 'toplam' }));
    matris('aylikTemsilci', 'Bölge · Satış Temsilcisi', [...ays, 'disi'], temsilciSatirlar, 'Dönemde tahsilat yok.');
    $('#tkAylikTemsilciNot').textContent = `${new Set(satT.map(s => s.temsilci)).size} satış temsilcisi · ${bolgeler.join(' + ')}`;
    matris('aylikYontem', 'Yöntem', ays, [...yontemler.map(y => satir(y, s => s.yontem === y, 'tahsilatYm', 'satır')), Object.assign(satir('Toplam', () => true, 'tahsilatYm', 'satır'), { sinif: 'toplam' })], 'Dönemde işlenmiş tahsilat yok.');
    const vadeli = s => s.yontem === 'Çek' || s.yontem === 'Senet';
    matris('aylikCek', 'Belge', ays, [satir('Çek', s => s.yontem === 'Çek', 'vadeYm', 'belge'), satir('Senet', s => s.yontem === 'Senet', 'vadeYm', 'belge'), Object.assign(satir('Toplam', vadeli, 'vadeYm', 'belge'), { sinif: 'toplam' })], 'Dönemde vadesi olan çek / senet yok.');
    const kisiler = [...new Set(T.map(t => t.kullanici).filter(Boolean))].sort((a, b) => a.localeCompare(b, TR));
    matris('aylikKisi', 'Tahsilatı Yapan', ays, [...kisiler.map(k => satir(k, s => s.kullanici === k, 'tahsilatYm', 'satır')), Object.assign(satir('Toplam', () => true, 'tahsilatYm', 'satır'), { sinif: 'toplam' })], 'Dönemde işlenmiş tahsilat yok.');
    const donem = sat.filter(s => ayK.has(s.tahsilatYm));
    $('#tkAylikNot').textContent = `${T.length} işlenmiş tahsilat · ${donemMetni()} · ${tl(topla(donem, s => s.tutarTl))} · ${new Set(donem.map(s => s.kullanici)).size} kişi`;
  }
  // ---------- SATIŞ RAPORU (20 Kas 2026; kullanıcı: "X R ayrı olarak ay ay ne satışı yapılmış, bölge (yurtiçi ve yurtdışı) ve cari sınıfını ayrı ayrı göstererek … altında satışçı bazlı da satışları detaylı göster";
  //            İhracat'ta yalnız yurtdışı + "özellikle şube satışlarını da alta ekle, ayrı ayrı görelim") ----------
  //   Veri: müşterinin satış faturaları (k.satislar; vade / kur farkı faturaları hariç, fatura tarihine göre aya yazılır). Süzgeçler, cari seçimi, arama ve İşlem Tipi (X / R) geçerli; dönem Hızlı Dönem'den (yoksa içinde bulunulan yıl),
  //   seçili ayların dışındaki faturalar "Dönem Dışı" sütununda (Toplam bütün faturaları verir). 1) bölge × X / R ayrı satırlar · 2) bölge › cari sınıfı (hücrede R üstte / X altta) · 3) bölge › satış temsilcisi · 4) İhracat: şubeler tek tek.
  // 20 Kas 2026 gece (kullanıcı: "satış raporu tasarımını ve kullanışlılığını geliştir, seçilenleri topla vs; tasarım çok geri seviye kaldı"):
  //   · üstte özet kartları (dönem satışı, R, X, ortalama fatura, en yüksek ay, bu ay ↔ geçen ay, en çok satan temsilci, en büyük müşteri)
  //   · GÖRÜNÜM düğmeleri: Tümü (bütün tablolar alt alta) ya da tek kırılım — tek tablo ekranın dibine kadar uzar (başlık ve dip toplam yapışkan)
  //   · her tabloda SEÇİM KUTUSU: seçilen satırlar dipte "Seçili · n" satırında toplanır (R / X ayrı, ay ay; tıklayınca fatura fatura); başlıktaki kutu hepsini seçer
  //   · başlığa tıklayınca SIRALAMA (ad, herhangi bir ay, Toplam): büyükten küçüğe → küçükten büyüğe → varsayılan; ara toplam satırları yerinde kalır
  //   · PAY sütunu (satırın genel toplam içindeki payı, ince çubukla) · "R / X Ayrı Göster" kapatılınca hücrede tek tutar · yeni kırılımlar: Müşteri, Ülke
  //   Görünüm ve R / X tercihi tarayıcıda saklanır (tkSatis); seçim ve sıralama sayfa açıkken korunur.
  const ST = { gorunum: 'tumu', rx: true }; try { const s = JSON.parse(localStorage.getItem('tkSatis') || 'null'); if (s) { ST.gorunum = s.gorunum || 'tumu'; ST.rx = s.rx !== false; } } catch (x) { }
  const stSakla = () => { try { localStorage.setItem('tkSatis', JSON.stringify(ST)); } catch (x) { } };
  const SSEC = {}, SSIRA = {}, ssec = id => SSEC[id] || (SSEC[id] = new Set());   // tablo → seçili satır adları · tablo → { k: 'ad' | yyyy-MM | 'top', y: 1 | -1 }
  const payH = (v, genel) => { if (!v || !genel) return tire; const p = v / genel * 100; return `<span class="tk-pay"><i style="width:${Math.max(0, Math.min(100, p)).toFixed(1)}%"></i><b>% ${p.toLocaleString(TR, { maximumFractionDigits: 1 })}</b></span>`; };
  /** Satış Raporu tablosu: aylar sütun, kırılım satır. satir = { ad, alt?, adH?, deger: ym | null → sayı, h: ym | null → html, liste: ym | null → faturalar, sinif: 'toplam' (ara / genel toplam), ara (seçilemeyen ara satır), _L }.
   *  Son satır 'toplam' ise dipte (tfoot) çizilir; secYap(ad, seçiliSatırlar) → "Seçili" satırı. Hücre tıklaması ortak tk-mx işleyicisinde (t._mx sırasıyla). */
  function satisTablo(id, ilkBaslik, kol, satirlar, bos, secYap) {
    const t = $('#tkTablo-' + id); if (!t) return; const bugunYm = (K.bugun || '').slice(0, 7), S = ssec(id), sr = SSIRA[id], yaprak = s => !s.sinif && !s.ara;
    const dipI = satirlar.length > 1 && satirlar[satirlar.length - 1].sinif === 'toplam' ? satirlar.length - 1 : -1, dip = dipI >= 0 ? satirlar[dipI] : null;
    let govde = satirlar.filter((s, i) => i !== dipI);
    if (sr) {   // ara toplam satırları yerinde kalır; aralarındaki satırlar dizilir
      const deg = s => sr.k === 'ad' ? s.ad : s.deger(sr.k === 'top' ? null : sr.k), cikti = []; let par = [];
      const bosalt = () => { par.sort((a, b) => { const x = deg(a), y = deg(b); return (typeof x === 'number' ? x - y : String(x).localeCompare(String(y), TR)) * sr.y; }); cikti.push(...par); par = []; };
      govde.forEach(s => { if (yaprak(s)) par.push(s); else { bosalt(); cikti.push(s); } }); bosalt(); govde = cikti;
    }
    const yapraklar = govde.filter(yaprak); [...S].forEach(a => { if (!yapraklar.some(s => s.ad === a)) S.delete(a); });   // süzgeç değişince listede kalmayan seçim düşer
    const secili = yapraklar.filter(s => S.has(s.ad)), secSatir = secili.length && secYap ? secYap(`Seçili · ${secili.length}`, secili) : null;
    const hepsi = govde.concat(dip ? [dip] : [], secSatir ? [secSatir] : []); t._mx = hepsi; t._kol = null;
    const genel = dip ? dip.deger(null) : topla(yapraklar, s => s.deger(null)), ok = k => sr && sr.k === k ? (sr.y > 0 ? ' art' : ' azl') : '';
    t.tHead.innerHTML = `<tr><th class="sec sirasiz"><input type="checkbox" data-ssec-hepsi="${id}" title="Satırların hepsini seç / bırak"${yapraklar.length && secili.length === yapraklar.length ? ' checked' : ''}></th><th class="ad${ok('ad')}" data-sk="ad" title="Sıralamak için tıklayın">${pbBaslik(ilkBaslik)}${PBG === 'USD' ? ' <small>($)</small>' : ''}</th>${kol.map(ym => `<th class="${ym === bugunYm ? 'bugun-ay' : ''}${ok(ym)}" data-sk="${ym}" title="Sıralamak için tıklayın">${ayEtiket(ym)}</th>`).join('')}<th class="${ok('top').trim()}" data-sk="top" title="Sıralamak için tıklayın">Toplam</th><th class="pay sirasiz" title="Satırın genel toplam içindeki payı">Pay</th></tr>`;
    const hc = (s, i, ym) => { const ic = s.h(ym); return s.liste && ic && ic !== tire ? `<a href="#" class="tk-mx" data-mx="${i}|${ym == null ? '' : ym}" title="Tıklayınca fatura fatura detay">${ic}</a>` : ic; };
    const satirH = (s, i) => { const y = yaprak(s), sc = y && S.has(s.ad);
      return `<tr class="${s.sinif || ''}${s.ara ? ' ara' : ''}"${sc ? ' data-secili="1"' : ''}><td class="sec">${y ? `<input type="checkbox" data-ssec="${id}" data-a="${esc(s.ad)}" title="Seç → dipte Seçili toplamı"${sc ? ' checked' : ''}>` : ''}</td><td class="ad">${s.adH || `<b>${esc(s.ad)}</b>`}${s.alt ? `<small>${esc(s.alt)}</small>` : ''}</td>${kol.map(ym => `<td class="${ym === bugunYm ? 'bugun-ay' : ''}">${hc(s, i, ym)}</td>`).join('')}<td class="top">${hc(s, i, null)}</td><td class="pay">${payH(s.deger(null), genel)}</td></tr>`; };
    t.tBodies[0].innerHTML = govde.map(satirH).join('') || `<tr><td colspan="${kol.length + 4}" class="na-bos">${bos || 'Kayıt yok.'}</td></tr>`;
    t.tFoot.innerHTML = (dip ? satirH(dip, govde.length) : '') + (secSatir ? satirH(secSatir, govde.length + (dip ? 1 : 0)) : '');
  }
  function cizSatis() {
    const ays = aylar(), ayK = new Set(ays), C = cariler(), bugunYm = (K.bugun || '').slice(0, 7);
    const sat = C.flatMap(k => (k.satislar || []).filter(kalemUy).map(x => Object.assign({ cariKodu: k.kod, cariAd: k.kisaAd, musteri: `${k.kod} ${k.kisaAd}`, bolge: k.bolge || 'Bölgesiz', sinif: k.sinif || 'Sınıfsız', ulke: k.ulke || (k.suzgec || {}).ulke || 'Ülkesiz', temsilci: (k.temsilci || '').split(',')[0].trim() || 'Atanmadı', ym: x.tarih.slice(0, 7), tarihIso: x.tarih, tutarTl: x.tl, tip: x.islemTipi === 'R' ? 'R' : 'X', tur: 'Satış · ' + (x.islemTipi === 'R' ? 'R' : 'X') }, x)));
    const Fy = (L, y) => y === 'disi' ? L.filter(s => !ayK.has(s.ym)) : y ? L.filter(s => s.ym === y) : L, T = l => topla(l, s => s.tutarTl);
    const tutar = l => l.length ? (DV && dovizli(l) ? `<span title="${esc(tl(T(l)))}">${esc(pbTop(l, s => s.tutar))}</span>` : tl(T(l))) : '—';
    /** R üstte / X altta hücre; yalnız dolu olan satır yazılır ("R —" / "X —" yok); Toplam satırı yalnız ikisi de varsa. "R / X Ayrı Göster" kapalıyken tek tutar */
    const rx = (l, toplamli) => { if (!l.length) return tire; if (!ST.rx) return tutar(l); const r = l.filter(s => s.tip === 'R'), x = l.filter(s => s.tip === 'X');
      return `<span class="tk-rx">${r.length ? `<span class="r"><small>R</small>${tutar(r)}</span>` : ''}${x.length ? `<span class="x"><small>X</small>${tutar(x)}</span>` : ''}${toplamli && r.length && x.length ? `<span class="t"><small>Toplam</small><b>${tutar(l)}</b></span>` : ''}</span>`; };
    const duz = (ad, L, alt, ek) => Object.assign({ ad, alt, _L: L, deger: y => T(Fy(L, y)), h: y => { const l = Fy(L, y); return l.length ? `${tutar(l)}<small>${l.length} fatura</small>` : tire; }, liste: y => Fy(L, y) }, ek || {});   // tek tip satır (R ya da X ya da toplam)
    const rxS = (ad, L, alt, ek) => Object.assign({ ad, alt, _L: L, deger: y => T(Fy(L, y)), h: y => rx(Fy(L, y), y == null), liste: y => Fy(L, y) }, ek || {});   // hücrede R / X ayrı
    const birlesik = sec => [...new Set(sec.flatMap(s => s._L))], fatSay = l => `${l.length} fatura`;
    const secDuz = (ad, sec) => { const l = birlesik(sec); return duz(ad, l, fatSay(l), { sinif: 'toplam secili' }); }, secRx = (ad, sec) => { const l = birlesik(sec); return rxS(ad, l, fatSay(l), { sinif: 'toplam secili' }); };
    const kol = [...ays, 'disi'], bos = 'Dönemde satış faturası yok.';
    const bolgeler = BOLGE_KILIT ? [BOLGE_KILIT] : [...new Set(sat.map(s => s.bolge))].sort((a, b) => (a === 'Yurtdışı') - (b === 'Yurtdışı') || a.localeCompare(b, TR)), cokBolge = !BOLGE_KILIT && bolgeler.length > 1;
    const musteriSay = l => `${new Set(l.map(s => s.cariKodu)).size} müşteri`, donem = sat.filter(s => ayK.has(s.ym)), R = donem.filter(s => s.tip === 'R'), X = donem.filter(s => s.tip === 'X');
    // ---- görünüm düğmeleri ve tercih
    const kartlar = $$('.tk-satis .na-kart[data-sg]'); if (ST.gorunum !== 'tumu' && !kartlar.some(k => k.dataset.sg === ST.gorunum)) ST.gorunum = 'tumu';
    $('#tkSatisGor').innerHTML = '<span class="e">Görünüm</span>' + [['tumu', 'Tümü']].concat(kartlar.map(k => [k.dataset.sg, k.dataset.sgAd])).map(([k, ad]) => `<button type="button" data-sgor="${k}" class="${ST.gorunum === k ? 'aktif' : ''}">${esc(ad)}</button>`).join('');
    kartlar.forEach(k => { k.hidden = ST.gorunum !== 'tumu' && k.dataset.sg !== ST.gorunum; }); $('#tkSatisRx').checked = ST.rx;
    // ---- özet kartları (seçili dönem; dönem dışı faturalar sayılmaz)
    const ayTop = ays.map(y => [y, T(donem.filter(s => s.ym === y))]), enAy = ayTop.reduce((a, b) => b[1] > a[1] ? b : a, ['', 0]);
    const grupEn = alan => { const m = new Map(); donem.forEach(s => m.set(s[alan], (m.get(s[alan]) || 0) + s.tutarTl)); return [...m.entries()].reduce((a, b) => b[1] > a[1] ? b : a, ['', 0]); };
    const buAy = T(sat.filter(s => s.ym === bugunYm)), oncekiYm = (() => { const d = new Date(bugunYm + '-01T00:00:00'); d.setMonth(d.getMonth() - 1); return iso(d).slice(0, 7); })(), gecenAy = T(sat.filter(s => s.ym === oncekiYm));
    const degisim = gecenAy ? (buAy - gecenAy) / gecenAy * 100 : null, enTem = grupEn('temsilci'), enMus = grupEn('musteri'), yuzde = (a, b) => b ? `% ${(a / b * 100).toLocaleString(TR, { maximumFractionDigits: 1 })}` : '—';
    const kutu = (ad, deger, alt) => `<div style="--k:#475569"><div class="l">${ad}</div><div class="v">${deger}</div><div class="s">${alt || '&nbsp;'}</div></div>`;
    $('#tkSatisKpi').innerHTML = [
      kutu('Satış · Dönem', tl(T(donem)), `${donem.length} fatura · ${musteriSay(donem)}`), kutu('R Satış', tl(T(R)), `${yuzde(T(R), T(donem))} · ${R.length} fatura`), kutu('X Satış', tl(T(X)), `${yuzde(T(X), T(donem))} · ${X.length} fatura`),
      kutu('Ortalama Fatura', donem.length ? tl(T(donem) / donem.length) : '—', ays.length ? `aylık ortalama ${tl(T(donem) / ays.length)}` : ''), kutu('En Yüksek Ay', enAy[0] ? esc(ayEtiket(enAy[0])) : '—', enAy[0] ? tl(enAy[1]) : ''),
      kutu(`Bu Ay · ${esc(ayEtiket(bugunYm))}`, tl(buAy), degisim == null ? 'geçen ay satış yok' : `geçen aya göre <b class="${degisim >= 0 ? 'ok' : 'gec'}">${degisim >= 0 ? '+' : '−'} % ${Math.abs(degisim).toLocaleString(TR, { maximumFractionDigits: 1 })}</b>`),
      kutu('En Çok Satan Temsilci', enTem[0] ? esc(enTem[0]) : '—', enTem[0] ? `${tl(enTem[1])} · ${yuzde(enTem[1], T(donem))}` : ''), kutu('En Büyük Müşteri', enMus[0] ? esc(enMus[0]) : '—', enMus[0] ? `${tl(enMus[1])} · ${yuzde(enMus[1], T(donem))}` : '')].join('');
    $('#tkSatisNot').textContent = `${sat.length} fatura · ${donemMetni()}${sat.length !== donem.length ? ` · ${sat.length - donem.length} fatura dönem dışı` : ''}`;
    // 1) bölge × X / R: her bölgede R satırı, X satırı, bölge toplamı; sonda genel toplam
    const tipSatir = [];
    bolgeler.forEach(bl => { const lb = sat.filter(s => s.bolge === bl), r = lb.filter(s => s.tip === 'R'), x = lb.filter(s => s.tip === 'X');
      tipSatir.push(duz(`${bl} · R`, r, musteriSay(r)), duz(`${bl} · X`, x, musteriSay(x))); if (cokBolge) tipSatir.push(duz(`${bl} Toplam`, lb, musteriSay(lb), { sinif: 'toplam' })); });
    if (cokBolge) tipSatir.push(duz('Genel R', sat.filter(s => s.tip === 'R'), '', { ara: 1 }), duz('Genel X', sat.filter(s => s.tip === 'X'), '', { ara: 1 }));
    tipSatir.push(duz(cokBolge ? 'Genel Toplam · R + X' : 'Toplam · R + X', sat, musteriSay(sat), { sinif: 'toplam' }));
    satisTablo('satisTip', 'Bölge · İşlem Tipi', kol, sat.length ? tipSatir : [], bos, secDuz);
    $('#tkSatisTipNot').textContent = `R ${tl(T(R))} · X ${tl(T(X))}`;
    // 2) bölge › kırılım (cari sınıfı · satış temsilcisi · müşteri · ülke): hücrede R üstte / X altta; bölge ara toplamı, genel toplam
    const kirilim = (id, baslik, alan, sonaAt, notId, birim, altF) => {   // altF(değer, faturalar) → satır adının altındaki bilgi (yoksa müşteri sayısı)
      if (!$('#tkTablo-' + id)) return; const satirlar = [];
      bolgeler.forEach(bl => { const lb = sat.filter(s => s.bolge === bl), ds = [...new Set(lb.map(s => s[alan]))].sort((a, b) => (a === sonaAt) - (b === sonaAt) || a.localeCompare(b, TR));
        ds.forEach(d => { const l = lb.filter(s => s[alan] === d); satirlar.push(rxS(cokBolge ? `${bl} · ${d}` : d, l, altF ? altF(d, l) : musteriSay(l))); });   // tek bölgede (İhracat) "Yurtdışı ·" ön eki yazılmaz
        if (ds.length && cokBolge) satirlar.push(rxS(`${bl} Toplam`, lb, `${ds.length} ${birim}`, { sinif: 'toplam' })); });
      satirlar.push(rxS(cokBolge ? 'Genel Toplam' : 'Toplam', sat, musteriSay(sat), { sinif: 'toplam' }));
      satisTablo(id, cokBolge ? baslik : baslik.replace('Bölge · ', ''), kol, sat.length ? satirlar : [], bos, secRx);
      $('#' + notId).textContent = `${new Set(sat.map(s => s[alan])).size} ${birim}`;
    };
    kirilim('satisSinif', 'Bölge · Cari Sınıfı', 'sinif', 'Sınıfsız', 'tkSatisSinifNot', 'cari sınıfı');
    kirilim('satisTemsilci', 'Bölge · Satış Temsilcisi', 'temsilci', 'Atanmadı', 'tkSatisTemsilciNot', 'satış temsilcisi', (d, l) => [(K.temsilciUnvan || {})[d], musteriSay(l)].filter(Boolean).join(' · '));   // birden çok temsilcili müşteride ana (ilk) temsilci; çift sayım yok
    kirilim('satisMusteri', 'Bölge · Müşteri', 'musteri', '', 'tkSatisMusteriNot', 'müşteri', (d, l) => [l[0].temsilci, l[0].sinif, fatSay(l)].join(' · '));
    kirilim('satisUlke', 'Bölge · Ülke', 'ulke', 'Ülkesiz', 'tkSatisUlkeNot', 'ülke');
    // İhracat Finansal Takibi: şube satışları — cari sınıfı "şube" olan her müşteri (yurtdışı şubeler) ayrı satır
    if ($('#tkTablo-satisSube')) {
      const sb = sat.filter(s => /şube/i.test(s.sinif)), kodlar = [...new Set(sb.map(s => s.cariKodu))].sort((a, b) => T(sb.filter(s => s.cariKodu === b)) - T(sb.filter(s => s.cariKodu === a)));
      const satirlar = kodlar.map(kd => { const l = sb.filter(s => s.cariKodu === kd), c = cariBul(kd); return rxS(`${kd} ${l[0].cariAd || ''}`.trim(), l, [c && c.ulke, c && c.sirket].filter(Boolean).join(' · ')); });
      if (kodlar.length) satirlar.push(rxS('Şubeler Toplam', sb, `${kodlar.length} şube`, { sinif: 'toplam' }));
      satisTablo('satisSube', 'Şube', kol, satirlar, 'Dönemde şube satışı yok.', secRx);
      $('#tkSatisSubeNot').textContent = `${kodlar.length} şube · ${sb.length} fatura · toplam satışın ${T(sat) ? '% ' + n2(T(sb) / T(sat) * 100) : '—'}`;
    }
    cizSubeYil();
  }
  /** İhracat › Satış Raporu'nun en altı: Şube Bakiyeleri sekmesindeki kendi şubelerimizin YILLIK satışları (ihracat-finans.js verisi; aynı kayıt → senkron). Kaynak ekstrelerde yalnız yıllık toplam olduğundan aylık kırılım yoktur.
   *  Satır = ÜLKE (kullanıcı: "Boya + Kağıt olarak değil ülke bazlı ayır"), sütun = yıl; gerçek mal bedeli yoksa tek tutar, varsa R ve Gerçek iki satır ("boş olanları yazma"). Seçim kutusu → dipte Seçili satırı (para birimine göre ayrı).
   *  Şirket süzgeci ve arama geçerli; ülke adına tıklayınca Şube Bakiyeleri sekmesi açılır */
  function cizSubeYil() {
    const t = $('#tkTablo-satisSubeYil'); if (!t) return;
    const hepsi = window.IhracatFinans && window.IhracatFinans.subeler ? window.IhracatFinans.subeler() : [];
    if (!hepsi.length && !cizSubeYil.bekledi) { cizSubeYil.bekledi = true; setTimeout(() => { if (aktif === 'satis') cizSubeYil(); }, 1800); }   // ihracat verisi henüz gelmediyse bir kez yeniden dene
    const SB = hepsi.filter(s => (!F.sirket.length || F.sirket.includes(s.sirket)) && araUyar(s.sube, s.ulke, s.kod, s.sirket, s.temsilci));
    const uy = s => ((s.ulke || s.sube || '').trim()) + '|' + (s.pb || 'TL'), tekil = l => [...new Set(l.filter(Boolean))];
    const G = tekil(SB.map(uy)).map(a => { const l = SB.filter(s => uy(s) === a), ulke = a.split('|')[0], pb = a.split('|')[1], sr = tekil(l.map(s => s.sirket));
      const ad = l.length === 1 && l[0].sube && !kucuk(l[0].sube).startsWith(kucuk(ulke)) ? `${ulke} · ${l[0].sube}` : ulke;
      return { ad, ulke, pb, l, alt: [tekil(l.map(s => s.temsilci)).join(', '), sr.join(' + '), pbAd(pb)].filter(Boolean).join(' · ') }; }).sort((x, y) => x.ad.localeCompare(y.ad, TR));
    const yilSira = y => parseInt(String(y).slice(0, 4), 10) || 0, yillar = [...new Set(SB.flatMap(s => (s.yillar || []).map(y => y.yil)))].sort((a, b) => yilSira(a) - yilSira(b) || String(a).localeCompare(String(b)));
    const sim = pb => ({ USD: '$', EUR: '€', GBP: '£' })[pb] || pbAd(pb), pr = (v, pb) => Number(v || 0).toLocaleString(TR, { maximumFractionDigits: 0 }) + ' ' + esc(sim(pb));
    const yT = (g, y, alan) => topla(g.l, s => ((s.yillar || []).find(x => x.yil === y) || {})[alan]), gT = (g, alan) => topla(g.l, s => topla(s.yillar || [], y => y[alan]));
    const cift = (rf, gm, pb, esas) => { const var_ = v => Math.abs(v) > 0.005; if (!var_(rf) && !var_(gm)) return tire; if (!var_(gm)) return esas != null ? `<b>${pr(rf, pb)}</b>` : pr(rf, pb);
      return `<span class="tk-rx">${var_(rf) ? `<span class="r"><small>R</small>${pr(rf, pb)}</span>` : ''}<span class="x"><small>Gerçek</small>${esas != null ? `<b>${pr(gm, pb)}</b>` : pr(gm, pb)}</span></span>`; };
    const S = ssec('satisSubeYil'); [...S].forEach(a => { if (!G.some(g => g.ad === a)) S.delete(a); }); const GS = G.filter(g => S.has(g.ad));
    t.tHead.innerHTML = `<tr><th class="sec sirasiz"><input type="checkbox" data-ssec-hepsi="satisSubeYil" title="Ülkelerin hepsini seç / bırak"${G.length && GS.length === G.length ? ' checked' : ''}></th><th class="ad sirasiz">Ülke</th>${yillar.map(y => `<th class="sirasiz">${esc(y)}</th>`).join('')}<th class="sirasiz">Toplam</th></tr>`;
    t.tBodies[0].innerHTML = G.map(g => `<tr${S.has(g.ad) ? ' data-secili="1"' : ''}><td class="sec"><input type="checkbox" data-ssec="satisSubeYil" data-a="${esc(g.ad)}" title="Seç → dipte Seçili toplamı"${S.has(g.ad) ? ' checked' : ''}></td><td class="ad"><a href="#" class="tk-link" data-git="ibSube" title="Şube Bakiyeleri sekmesini aç"><b>${esc(g.ad)}</b></a>${g.alt ? `<small>${esc(g.alt)}</small>` : ''}</td>${yillar.map(y => `<td>${cift(yT(g, y, 'resmiFatura'), yT(g, y, 'gercekMalBedeli'), g.pb)}</td>`).join('')}<td class="top">${cift(gT(g, 'resmiFatura'), gT(g, 'gercekMalBedeli'), g.pb, true)}</td></tr>`).join('') || `<tr><td colspan="${yillar.length + 3}" class="na-bos">Şube kaydı yok.</td></tr>`;
    // dip toplam: satış esası (gerçek mal bedeli varsa o, yoksa resmî fatura − iade); para birimine göre ayrı (farklı para birimleri toplanmaz); altında seçili ülkelerin toplamı
    const dipSatir = (L, ad, alt, sinif) => { const pbler = tekil(L.map(g => g.pb)), dipH = f => pbler.map(pb => { const v = topla(L.filter(g => g.pb === pb), f); return Math.abs(v) > 0.005 ? pr(v, pb) : ''; }).filter(Boolean).join('<br>') || '—';
      return `<tr class="${sinif}"><td class="sec"></td><td class="ad"><b>${ad}</b><small>${alt}</small></td>${yillar.map(y => `<td>${dipH(g => yT(g, y, 'satisEsas'))}</td>`).join('')}<td class="top"><b>${dipH(g => gT(g, 'satisEsas'))}</b></td></tr>`; };
    t.tFoot.innerHTML = G.length ? dipSatir(G, `Toplam · ${G.length} ülke`, 'satış esası', 'toplam') + (GS.length ? dipSatir(GS, `Seçili · ${GS.length}`, esc(GS.map(g => g.ulke).join(' + ')), 'toplam secili') : '') : '';
    $('#tkSatisSubeYilNot').textContent = G.length ? `${G.length} ülke · ${SB.length} şube kaydı · ${yillar.length} yıl` : '';
  }
  // Satış Raporu etkileşimi: satır seçimi, hepsini seç, R / X anahtarı, görünüm düğmeleri, başlıkla sıralama
  kok.addEventListener('change', e => {
    const c = e.target.closest('[data-ssec]'); if (c) { const S = ssec(c.dataset.ssec); if (c.checked) S.add(c.dataset.a); else S.delete(c.dataset.a); cizSatis(); return; }
    const h = e.target.closest('[data-ssec-hepsi]'); if (h) { const id = h.dataset.ssecHepsi, S = ssec(id); $$('tbody [data-ssec]', $('#tkTablo-' + id)).forEach(x => { if (h.checked) S.add(x.dataset.a); else S.delete(x.dataset.a); }); cizSatis(); return; }
    if (e.target.id === 'tkSatisRx') { ST.rx = e.target.checked; stSakla(); cizSatis(); }
  });
  kok.addEventListener('click', e => {
    const g = e.target.closest('[data-sgor]'); if (g) { ST.gorunum = g.dataset.sgor; stSakla(); cizSatis(); return; }
    const th = e.target.closest('.tk-satis th[data-sk]'); if (!th || e.target.closest('input')) return;
    const id = th.closest('table').id.replace('tkTablo-', ''), k = th.dataset.sk, ilk = k === 'ad' ? 1 : -1, sr = SSIRA[id];
    if (!sr || sr.k !== k) SSIRA[id] = { k, y: ilk }; else if (sr.y === ilk) sr.y = -ilk; else delete SSIRA[id];
    cizSatis();
  });
  // ---------- ÇEK AKIBETİ RAPORU sekmesi (20 Kas 2026; kullanıcı: "tahsilat raporuna Çek Akıbeti Raporu sekmesi ekle, senkronize çalışsın"): raporun kendisi çerçeve içinde (ayrı kopya yok → veri tek kaynak);
  //            ilk açılışta yüklenir, bu sayfada tahsilat kaydı değiştiyse (çek girişi vb.) sekmeye dönünce yeniden okunur; çerçeve ekranın dibine kadar uzar ----------
  let cekKirli = false;
  function cekAc() {
    const f = $('#tkCekCerceve'); if (!f) return;
    if (!f.dataset.bagli) { f.dataset.bagli = '1'; f.addEventListener('load', () => { try { const k = f.contentDocument.getElementById('sekmeCubuguKap'); if (k) k.style.display = 'none'; } catch (x) { /* yoksay */ } }); }   // çerçeve içinde kabuğun sekme çubuğu / "Menüleri gizle" düğmesi gereksiz
    if (!f.getAttribute('src')) f.setAttribute('src', f.dataset.src); else if (cekKirli) { try { f.contentWindow.location.reload(); } catch (x) { f.setAttribute('src', f.dataset.src); } }
    cekKirli = false; cekBoyut();
  }
  function cekBoyut() { const f = $('#tkCekCerceve'); if (f && f.offsetParent !== null) f.style.height = Math.max(480, window.innerHeight - (f.getBoundingClientRect().top + window.scrollY) - 14) + 'px'; }
  window.addEventListener('resize', () => { clearTimeout(cekBoyut.z); cekBoyut.z = setTimeout(cekBoyut, 120); });
  window.addEventListener('thYenile', () => { cekKirli = true; });
  // ---------- PLANLANAN ↔ GERÇEKLEŞEN (19 Kas 2026): vade ayına göre yapılması gereken ↔ zamanında / gecikmeli tahsil edilen / açık; tahsilat ayına göre; vade farkı doğan ↔ kesilen ----------
  function cizPlan() {
    // 19 Kas 2026 (kullanıcı: "tüm sekmelerdeki rakamlar birbiriyle uyumlu olsun"): seçili ayların dışında kalan kayıtlar "Dönem Dışı" sütununda → toplamlar Alacak Takibi / Özet ile birebir
    const ays = aylar(), ayK = new Set(ays), aysT = [...ays, 'disi'], C = cariler(), T = islenenler(), ym = s => String(s || '').slice(0, 7);
    const dag = T.flatMap(t => t.dagitim.map(d => Object.assign({ vadeYm: trIso(d.vade).slice(0, 7), odemeYm: trIso(d.odemeTarihi).slice(0, 7), no: t.no, cariKodu: t.cariKodu, cariAd: t.cariAd }, d))).filter(kalemUy);   // fatura kapatan ödemeler (ödeme günü − vade = gecikme)
    const acik = C.flatMap(k => k.kalemler.map(x => Object.assign({ vadeYm: ym(x.vade), kod: k.kod, kisaAd: k.kisaAd }, x)));                                                                        // hâlâ açık kalemler
    const zam = dag.filter(d => d.gecikmeGun <= 0), gec = dag.filter(d => d.gecikmeGun > 0);
    const Fy = (l, y, key) => !y ? l : l.filter(x => y === 'disi' ? !ayK.has(x[key || 'vadeYm']) : x[key || 'vadeYm'] === y);   // y: ay · 'disi' · null (hepsi)
    const S = (l, y, alan, key) => topla(Fy(l, y, key), alan);
    const ortGun = l => { const t = topla(l, d => d.tutarTl); return t ? topla(l, d => d.tutarTl * d.gecikmeGun) / t : 0; };
    const planlanan = y => S(dag, y, d => d.tutarTl) + S(acik, y, x => x.tl);
    const yuzde = (a, b) => b ? `<b>${Math.round(a / b * 100)} %</b>` : tire, gunH = v => v < -0.5 ? `<span class="ok">${Math.round(-v)} gün erken</span>` : v > 0.5 ? `<span class="gec">${Math.round(v)} gün</span>` : 'vadesinde';
    matris('planVade', 'Vade Ayına Göre', aysT, [
      { ad: 'Vadesi Gelen · Yapılması Gereken', alt: 'kapanan + hâlâ açık', deger: planlanan, h: (y, v) => v ? `<b>${tl(v)}</b>` : tire, liste: y => Fy(dag, y).concat(Fy(acik, y)) },
      { ad: 'Zamanında Tahsil Edilen', alt: 'ödeme günü ≤ vade', deger: y => S(zam, y, d => d.tutarTl), h: (y, v) => v ? `<span class="ok">${tl(v)}</span>` : tire, liste: y => Fy(zam, y) },
      { ad: 'Gecikmeli Tahsil Edilen', alt: 'ödeme günü > vade', deger: y => S(gec, y, d => d.tutarTl), h: (y, v) => { const l = Fy(gec, y); return v ? `<span class="gec">${tl(v)}</span><small>ort. ${Math.round(ortGun(l))} gün gecikme</small>` : tire; }, liste: y => Fy(gec, y) },
      { ad: 'Hâlâ Açık · Tahsil Edilmedi', alt: 'bugün itibarıyla = Alacak Takibi açık toplamı', deger: y => S(acik, y, x => x.tl), h: (y, v) => { const l = Fy(acik, y); return v ? `${tutarH(v, l)}<small>${l.length} fatura</small>` : tire; }, liste: y => Fy(acik, y) },
      { ad: 'Gerçekleşme', alt: 'tahsil edilen / vadesi gelen', deger: y => S(dag, y, d => d.tutarTl), h: (y, v) => y ? yuzde(v, planlanan(y)) : '', toplam: () => S(dag, null, d => d.tutarTl), toplamH: v => yuzde(v, planlanan(null)), sinif: 'toplam' }
    ]);
    matris('planAy', 'Tahsilat Ayına Göre', aysT, [
      { ad: 'Tahsil Edilen', alt: 'fatura kapatan ödemeler', deger: y => S(dag, y, d => d.tutarTl, 'odemeYm'), h: (y, v) => v ? `<b>${tl(v)}</b>` : tire, liste: y => Fy(dag, y, 'odemeYm') },
      { ad: 'Zamanında', deger: y => S(zam, y, d => d.tutarTl, 'odemeYm'), h: (y, v) => v ? `<span class="ok">${tl(v)}</span>` : tire, liste: y => Fy(zam, y, 'odemeYm') },
      { ad: 'Gecikmeli', deger: y => S(gec, y, d => d.tutarTl, 'odemeYm'), h: (y, v) => { const l = Fy(gec, y, 'odemeYm'); return v ? `<span class="gec">${tl(v)}</span><small>ort. ${Math.round(ortGun(l))} gün</small>` : tire; }, liste: y => Fy(gec, y, 'odemeYm') },
      { ad: 'Ortalama Gecikme', alt: 'tutar ağırlıklı; eksi = vadeden önce', deger: y => ortGun(Fy(dag, y, 'odemeYm')), h: (y, v) => y ? (Fy(dag, y, 'odemeYm').length ? gunH(v) : tire) : '', toplam: () => ortGun(dag), toplamH: v => dag.length ? gunH(v) : tire, sinif: 'toplam' }
    ]);
    const kodlar = new Set(C.map(k => k.kod)), FT = (K.faturalar || []).filter(f => kodlar.has(f.cariKodu) && f.tur === 'Vade Farkı' && kalemUy(f)).map(f => Object.assign({ fYm: ym(f.tarihIso) }, f)), bugunYm = ym(K.bugun);
    const acikVf = topla(C, k => k.vf.bugunVf);   // açık gecikmiş faturalardan bugüne kadar doğmuş, henüz kesilmemiş vade farkı (içinde bulunulan aya yazılır)
    const kes = (ad, kaynak, alt) => ({ ad, alt, deger: y => S(FT.filter(f => f.kaynak === kaynak), y, f => f.matrah, 'fYm'), h: (y, v) => v ? `<span class="ok">${tl2(v)}</span>` : tire, liste: y => Fy(FT.filter(f => f.kaynak === kaynak), y, 'fYm') });
    const kesilmemisT = T.filter(t => !t.vadeFarkiFaturaNo).map(t => Object.assign({ tYm: ym(t.tarihIso) }, t));
    matris('planVf', 'Vade Farkı (KDV hariç)', aysT, [
      { ad: 'Tahsilatta Doğan', alt: 'ödeme vadeden sonra alındı (çek vadesi vb.)', deger: y => S(dag, y, d => d.vadeFarki, 'odemeYm'), h: (y, v) => v ? `${tl2(v)}` : tire, liste: y => Fy(dag, y, 'odemeYm').filter(d => d.vadeFarki > 0.005) },
      kes('Kesilen · Tahsilat Sırasında', 'Tahsilat', 'tahsilatla birlikte kesilen'), kes('Kesilen · Ay Sonu / Robot', 'Ay Sonu', 'vadesi geçtiği için'), kes('Kesilen · Şimdi Kes (Elle)', 'Elle'),
      { ad: 'Kesilmemiş', alt: 'tahsilatta doğup faturası kesilmeyen + açık gecikmiş faturalardan bugüne (bu ay)', deger: y => S(kesilmemisT, y, t => t.vadeFarki, 'tYm') + (y === bugunYm || (y === null) ? acikVf : 0) - (y === 'disi' && !ayK.has(bugunYm) ? -acikVf : 0), h: (y, v) => v ? `<span class="gec">${tl2(v)}</span>` : tire, sinif: 'toplam', liste: y => Fy(kesilmemisT, y, 'tYm').map(t => Object.assign({ tutarTl: t.vadeFarki, yontem: 'Tahsilatta doğan · kesilmedi' }, t)) }
    ]);
    $('#tkPlanNot').textContent = `${C.length} müşteri · ${donemMetni()} · vadesi gelen ${tl(planlanan(null))} · tahsil edilen ${tl(S(dag, null, d => d.tutarTl))} · açık ${tl(S(acik, null, x => x.tl))}`;
  }
  // ---------- ÖZEL RAPOR sekmesi (20 Kas 2026; kullanıcı: "İhracat Finansal Takibi ve Tahsilat raporlarına Konsolide İK Paneli'nde yaptığımız gibi Özel Rapor sekmesi ekleyelim, istenilen şekilde raporlar yapılabilsin") ----------
  //   1) VERİ: Müşteriler · Açık Faturalar · Satış Faturaları · Tahsilatlar (satırın ne olduğu). 2) SÜTUNLAR: yongalara tıklayınca eklenir / çıkar. 3) KOŞULLAR: alan + karşılaştırma + değer (hepsi birlikte sağlanır).
  //   4) GRUPLA: bir ya da iki alana göre toplar (ör. Satış Temsilcisi › Fatura Ayı): kayıt sayısı + seçili tutar sütunlarının toplamı; grup satırına tıklayınca o grubun satırları pencerede.
  //   Üst çubuğun süzgeçleri, arama, İşlem Tipi ve (Satış / Tahsilat / Müşteri-satış sütunlarında) Hızlı Dönem burada da geçerli. Kayıtlı raporlar SUNUCUDA ve herkes için ortak (/Tahsilat/OzelRaporKaydet · OzelRaporSil;
  //   K.ozelRaporlar); son çalışılan durum tarayıcıda (tkOzelRapor). Veri saklanmaz — rapor her açılışta güncel veriden hesaplanır. Sütun türleri: m metin · p ₺ tutar (toplanır) · n sayı (top: 1 ise toplanır) ·
  //   v belge para birimiyle tutar (para birimine göre ayrı toplanır) · d tarih · a ay (yyyy-MM, "Kas 2026" yazılır).
  const orM = (k, ad, v, g, ek) => Object.assign({ k, ad, t: 'm', v, g }, ek || {}), orP = (k, ad, v, g, ek) => Object.assign({ k, ad, t: 'p', v, g }, ek || {}), orN = (k, ad, v, g, ek) => Object.assign({ k, ad, t: 'n', v, g }, ek || {});
  const orD = (k, ad, v, g) => ({ k, ad, t: 'd', v: r => trIso(v(r) || ''), g }), orA = (k, ad, v, g) => ({ k, ad, t: 'a', v: r => trIso(v(r) || '').slice(0, 7), g }), orV = (k, ad, v, pb, g) => ({ k, ad, t: 'v', v, pb, g });
  const orSz = r => r.k.suzgec || {}, orVf = r => r.k.vf || {}, orTem = r => (r.k.temsilci || '').split(',')[0].trim();
  const orSat = k => (k.satislar || []).filter(x => kalemUy(x) && donemUy(x.tarih));   // müşterinin dönemdeki satış faturaları (İşlem Tipi süzgeci geçerli)
  const orKartH = (r, ic) => r.k.kod && cariBul(r.k.kod) ? `<a href="#" class="tk-link" data-or-kart="${esc(r.k.kod)}" title="Cari kartını aç">${ic}</a>` : ic;
  const OR_KIMLIK = [
    orM('kod', 'Cari Kodu', r => r.k.kod, 'Müşteri', { h: r => orKartH(r, `<b>${esc(r.k.kod || '')}</b>`) }), orM('ad', 'Müşteri', r => r.k.kisaAd, 'Müşteri', { h: r => orKartH(r, esc(r.k.kisaAd || '')) }), orM('unvan', 'Resmî Unvan', r => r.k.resmiUnvan, 'Müşteri'),
    orM('sirket', 'Şirket', r => r.k.sirket, 'Müşteri'), orM('bolge', 'Bölge', r => r.k.bolge, 'Müşteri'), orM('ulke', 'Ülke', r => r.k.ulke || orSz(r).ulke, 'Müşteri'), orM('sehir', 'Şehir', r => orSz(r).sehir, 'Müşteri'),
    orM('sinif', 'Cari Sınıfı', r => r.k.sinif, 'Müşteri'), orM('iliski', 'İlişki Durumu', r => r.k.iliski, 'Müşteri'), orM('temsilci', 'Satış Temsilcisi', orTem, 'Müşteri'), orM('temsilciUnvan', 'Temsilci Unvanı', r => (K.temsilciUnvan || {})[orTem(r)] || '', 'Müşteri'),
    orM('vadeSarti', 'Vade Şartı', r => r.k.vadeSarti, 'Müşteri'), orM('rol', 'Cari Rolü', r => orSz(r).rol, 'Müşteri'), orM('kartDurum', 'Kart Durumu', r => orSz(r).durum, 'Müşteri'), orM('anaGrup', 'Faaliyet Ana Grubu', r => (orSz(r).anaGruplar || []).join(', '), 'Müşteri'),
    orM('yetkili', 'Yetkili', r => r.k.yetkili || orSz(r).yetkili, 'Müşteri'), orM('telefon', 'Telefon', r => orSz(r).telefon, 'Müşteri'), orM('eposta', 'E-posta', r => orSz(r).eposta, 'Müşteri'), orM('vergiNo', 'Vergi No', r => orSz(r).vergiNo, 'Müşteri')];
  const OR_CARI = [
    orP('bakiye', 'Bakiye', r => bakiyeSec(r.k), 'Alacak'), orP('bakiyeR', 'Bakiye R', r => r.k.bakiyeR, 'Alacak'), orP('bakiyeX', 'Bakiye X', r => r.k.bakiyeX, 'Alacak'), orP('acikToplam', 'Açık Toplam', r => r.k.acikToplam, 'Alacak'), orN('acikAdet', 'Açık Fatura (Adet)', r => r.k.acikAdet, 'Alacak', { top: 1 }),
    orP('gecmis', 'Gecikmiş', r => r.k.gecmis, 'Alacak'), orN('gecmisAdet', 'Gecikmiş Fatura (Adet)', r => r.k.gecmisAdet, 'Alacak', { top: 1 }), orN('maxGecikme', 'En Uzun Gecikme (Gün)', r => r.k.maxGecikme, 'Alacak'),
    orP('buHafta', 'Bu Hafta Vadeli', r => r.k.buHafta, 'Alacak'), orP('g8_30', '8–30 Gün Vadeli', r => r.k.g8_30, 'Alacak'), orP('g31_60', '31–60 Gün Vadeli', r => r.k.g31_60, 'Alacak'), orP('g60', '60+ Gün Vadeli', r => r.k.g60, 'Alacak'),
    orP('cek', 'Çek / Senet', r => r.k.cek, 'Alacak'), orN('cekAdet', 'Çek / Senet (Adet)', r => r.k.cekAdet, 'Alacak', { top: 1 }), orP('toplamRisk', 'Toplam Risk', r => (r.k.acikToplam || 0) + (r.k.cek || 0), 'Alacak'),
    orP('son30', 'Son 30 Gün Tahsilat', r => r.k.son30Tahsilat, 'Tahsilat'), orN('son30Adet', 'Son 30 Gün Tahsilat (Adet)', r => r.k.son30Adet, 'Tahsilat', { top: 1 }), orD('sonTahsilat', 'Son Tahsilat Tarihi', r => r.k.sonTahsilatIso, 'Tahsilat'), orP('sonTahsilatTl', 'Son Tahsilat Tutarı', r => r.k.sonTahsilatTl, 'Tahsilat'), orP('buAyOdeme', 'Bu Ay Tahsilat', r => (r.k.takip || {}).buAyOdeme, 'Tahsilat'),
    orP('satis', 'Satış · Dönem', r => topla(orSat(r.k), x => x.tl), 'Satış'), orP('satisR', 'Satış R · Dönem', r => topla(orSat(r.k).filter(x => x.islemTipi === 'R'), x => x.tl), 'Satış'), orP('satisX', 'Satış X · Dönem', r => topla(orSat(r.k).filter(x => x.islemTipi !== 'R'), x => x.tl), 'Satış'),
    orN('satisAdet', 'Satış Faturası (Adet) · Dönem', r => orSat(r.k).length, 'Satış', { top: 1 }), orD('sonFatura', 'Son Fatura Tarihi', r => (r.k.sonFatura || {}).tarih, 'Satış'), orP('sonFaturaTl', 'Son Fatura Tutarı', r => (r.k.sonFatura || {}).tl, 'Satış'),
    orP('aySonuVf', 'Ay Sonu Vade Farkı', r => orVf(r).aySonuVf, 'Vade Farkı'), orP('bugunVf', 'Bugüne Kadar Vade Farkı', r => orVf(r).bugunVf, 'Vade Farkı'), orP('kesilenVf', 'Kesilen Vade Farkı', r => orVf(r).kesilen, 'Vade Farkı'), orN('vfOran', '% Vade Farkı Oranı (Aylık)', r => orVf(r).oran, 'Vade Farkı'),
    orM('risk', 'Risk', r => r.k.risk, 'Limit ve Risk'), orM('limitDurum', 'Limit Durumu', r => r.k.limitDurum, 'Limit ve Risk'), orP('limit', 'Limit', r => orVf(r).limit, 'Limit ve Risk'), orP('limitRisk', 'Limit Riski', r => orVf(r).limitRisk, 'Limit ve Risk'),
    orN('limitYuzde', '% Limit Kullanımı', r => orVf(r).limitYuzde, 'Limit ve Risk'), orP('limitAsim', 'Limit Aşımı', r => orVf(r).limitAsim, 'Limit ve Risk'), orM('satisDurum', 'Satış Durumu', r => orVf(r).satisDurdu ? 'Durduruldu' : 'Açık', 'Limit ve Risk'),
    orM('mutabakat', 'Mutabakat', r => orVf(r).mutabakat, 'Limit ve Risk'), orM('uyum', 'Muhasebe Uyumu', r => (r.k.uyum || {}).durum, 'Limit ve Risk'), orP('uyumFark', 'Muhasebe Farkı', r => (r.k.uyum || {}).fark, 'Limit ve Risk')];
  const orBelgeH = r => belgeLink(r.k.kod, r.x.belge), orTip = r => r.x.islemTipi === 'R' ? 'R' : 'X';
  const OR_KAYNAK = {
    musteri: { ad: 'Müşteriler', birim: 'müşteri', donem: true, anahtar: r => r.k.kod, liste: () => cariler().map(k => ({ k })), kol: OR_KIMLIK.concat(OR_CARI), vars: ['kod', 'ad', 'sirket', 'temsilci', 'acikToplam', 'gecmis', 'cek', 'toplamRisk', 'son30', 'risk'] },
    acik: { ad: 'Açık Faturalar', birim: 'fatura', donem: false, anahtar: r => r.k.kod + '|' + r.x.belge, liste: () => cariler().flatMap(k => k.kalemler.map(x => ({ k, x }))), vars: ['kod', 'ad', 'temsilci', 'belge', 'tarih', 'vade', 'gecikme', 'islemTipi', 'kalanPb', 'tl'],
      kol: OR_KIMLIK.concat([orM('belge', 'Belge No', r => r.x.belge, 'Fatura', { h: orBelgeH }), orD('tarih', 'Fatura Tarihi', r => r.x.tarih, 'Fatura'), orA('ay', 'Fatura Ayı', r => r.x.tarih, 'Fatura'), orD('vade', 'Vade', r => r.x.vade, 'Fatura'), orA('vadeAy', 'Vade Ayı', r => r.x.vade, 'Fatura'),
        orN('kalanGun', 'Vadeye Kalan (Gün)', r => r.x.gun, 'Fatura'), orN('gecikme', 'Gecikme (Gün)', r => r.x.gun < 0 ? -r.x.gun : 0, 'Fatura'), orM('vadeDurum', 'Vade Durumu', r => r.x.gun < 0 ? 'Gecikmiş' : 'Vadesi Gelmedi', 'Fatura'), orM('islemTipi', 'İşlem Tipi', orTip, 'Fatura'), orM('tur', 'Fatura Türü', r => r.x.tur, 'Fatura'),
        orM('pb', 'Para Birimi', r => pbAd(r.x.pb), 'Fatura'), orV('tutar', 'Fatura Tutarı', r => r.x.tutar, r => r.x.pb, 'Fatura'), orV('kalanPb', 'Kalan', r => r.x.kalan, r => r.x.pb, 'Fatura'), orP('tl', 'Kalan', r => r.x.tl, 'Fatura')]) },
    satis: { ad: 'Satış Faturaları', birim: 'fatura', donem: true, anahtar: r => r.k.kod + '|' + r.x.belge, liste: () => cariler().flatMap(k => orSat(k).map(x => ({ k, x }))), vars: ['kod', 'ad', 'temsilci', 'belge', 'tarih', 'islemTipi', 'tutar', 'tl', 'durum'],
      kol: OR_KIMLIK.concat([orM('belge', 'Belge No', r => r.x.belge, 'Fatura', { h: orBelgeH }), orD('tarih', 'Fatura Tarihi', r => r.x.tarih, 'Fatura'), orA('ay', 'Fatura Ayı', r => r.x.tarih, 'Fatura'), orM('yil', 'Fatura Yılı', r => String(r.x.tarih || '').slice(0, 4), 'Fatura'), orM('islemTipi', 'İşlem Tipi', orTip, 'Fatura'),
        orM('pb', 'Para Birimi', r => pbAd(r.x.pb), 'Fatura'), orV('tutar', 'Fatura Tutarı', r => r.x.tutar, r => r.x.pb, 'Fatura'), orP('tl', 'Fatura Tutarı', r => r.x.tl, 'Fatura'), orV('kalanPb', 'Kalan', r => r.x.kalan, r => r.x.pb, 'Fatura'), orM('durum', 'Durum', r => r.x.kalan > 0.005 ? 'Açık' : 'Kapalı', 'Fatura')]) },
    tahsilat: { ad: 'Tahsilatlar', birim: 'tahsilat', donem: true, anahtar: r => r.x.no, vars: ['no', 'tarih', 'kod', 'ad', 'temsilci', 'yontem', 'islemTipi', 'tutar', 'tutarTl', 'durum'],
      liste: () => tahsilatlar().filter(t => donemUy(t.tarihIso)).map(x => ({ k: cariBul(x.cariKodu) || { kod: x.cariKodu, kisaAd: x.cariAd, sirket: x.sirket }, x })),
      kol: OR_KIMLIK.concat([orM('no', 'Tahsilat No', r => r.x.no, 'Tahsilat', { h: r => `<a href="#" class="tk-link" data-or-tah="${esc(r.x.no)}" title="Tahsilat kaydını aç"><b>${esc(r.x.no)}</b></a>` }), orD('tarih', 'Tahsilat Tarihi', r => r.x.tarihIso, 'Tahsilat'), orA('ay', 'Tahsilat Ayı', r => r.x.tarihIso, 'Tahsilat'),
        orM('yil', 'Tahsilat Yılı', r => String(r.x.tarihIso || '').slice(0, 4), 'Tahsilat'), orM('yontem', 'Yöntem', r => r.x.yontem, 'Tahsilat'), orM('islemTipi', 'İşlem Tipi', orTip, 'Tahsilat'), orM('pb', 'Para Birimi', r => pbAd(r.x.paraBirimi), 'Tahsilat'),
        orV('tutar', 'Tutar', r => r.x.tutar, r => r.x.paraBirimi, 'Tahsilat'), orP('tutarTl', 'Tutar', r => r.x.tutarTl, 'Tahsilat'), orM('durum', 'Durum', r => r.x.durum, 'Tahsilat'), orP('vadeFarki', 'Vade Farkı', r => r.x.vadeFarki, 'Tahsilat'), orP('kurFarki', 'Kur Farkı', r => r.x.kurFarki, 'Tahsilat'),
        orM('vfFatura', 'Vade Farkı Fatura No', r => r.x.vadeFarkiFaturaNo, 'Tahsilat'), orN('cekAdet', 'Çek (Adet)', r => r.x.cekAdet, 'Tahsilat', { top: 1 }), orM('kaydeden', 'Kaydeden', r => r.x.kullanici, 'Tahsilat'), orM('aciklama', 'Açıklama', r => r.x.aciklama, 'Tahsilat')]) }
  };
  const OR_OP = [['=', '='], ['!=', '≠'], ['>=', '≥'], ['<=', '≤'], ['>', '>'], ['<', '<'], ['icerir', 'içerir'], ['icermez', 'içermez'], ['dolu', 'dolu'], ['bos', 'boş']];
  const OR_SAKLA = 'tkOzelRapor';
  const orVarsayilan = kaynak => ({ kaynak, sutunlar: OR_KAYNAK[kaynak].vars.slice(), kosullar: [], grup: [], ad: '' });
  /** dışarıdan gelen ayarı (tarayıcı / kayıtlı rapor) doğrular: bilinmeyen kaynak, sütun ve koşul alanları atılır */
  function orTemizle(a) {
    const kaynak = a && OR_KAYNAK[a.kaynak] ? a.kaynak : 'musteri', var_ = k => OR_KAYNAK[kaynak].kol.some(c => c.k === k);
    const sutunlar = (Array.isArray(a && a.sutunlar) ? a.sutunlar : []).filter(var_);
    return { kaynak, sutunlar: sutunlar.length ? sutunlar : OR_KAYNAK[kaynak].vars.slice(), kosullar: (Array.isArray(a && a.kosullar) ? a.kosullar : []).filter(c => c && var_(c.a)).map(c => ({ a: c.a, o: OR_OP.some(o => o[0] === c.o) ? c.o : '=', v: String(c.v ?? '') })),
      grup: (Array.isArray(a && a.grup) ? a.grup : []).filter(var_).slice(0, 2), ad: String((a && a.ad) || '').slice(0, 60) };
  }
  let orAyar = orVarsayilan('musteri'), orAcik = true; try { const s = JSON.parse(localStorage.getItem(OR_SAKLA) || 'null'); if (s) orAyar = orTemizle(s); } catch (x) { }
  const orSakla = () => { try { localStorage.setItem(OR_SAKLA, JSON.stringify(orAyar)); } catch (x) { } };
  const orKolonlar = () => OR_KAYNAK[orAyar.kaynak].kol, orKol = k => orKolonlar().find(c => c.k === k), orDeger = (c, r) => { try { const v = c.v(r); return v == null ? '' : v; } catch (x) { return ''; } };
  const orSayisal = c => c.t === 'p' || c.t === 'n' || c.t === 'v', orBaslik = c => c.t === 'p' ? c.ad + ' (₺)' : c.t === 'v' ? c.ad + ' (Belge PB)' : c.ad;
  const orSayiYaz = x => Number(x || 0).toLocaleString(TR, { maximumFractionDigits: 1 });
  const orPbTop = (L, c) => { const m = {}; L.forEach(r => { const p = pbAd(c.pb(r)); m[p] = (m[p] || 0) + (Number(orDeger(c, r)) || 0); }); return Object.keys(m).filter(p => Math.abs(m[p]) > 0.005).sort((a, b) => (a === '₺') - (b === '₺') || a.localeCompare(b)).map(p => n2(m[p]) + ' ' + esc(p)).join('<br>'); };
  /** sütun tanımı → tablo motoru sütunu (satır = kayıt) */
  function orMotor(c) {
    const v = r => orDeger(c, r);
    if (c.t === 'p') return P(c.k, orBaslik(c), r => Number(v(r)) || 0, c.h ? { h: c.h } : null);
    if (c.t === 'n') return P(c.k, c.ad, r => Number(v(r)) || 0, { sinif: 'sayac', h:   /* sayac: tablo-hizala ₺ eklemesin (adet, gün, %) */ r => Number(v(r)) ? orSayiYaz(v(r)) : tire, top: c.top ? r => Number(v(r)) || 0 : null, dipH: L => orSayiYaz(topla(L, v)) });
    if (c.t === 'v') return P(c.k, orBaslik(c), r => Number(v(r)) || 0, { h: r => Number(v(r)) ? n2(v(r)) + ' ' + esc(pbAd(c.pb(r))) : tire, dipH: L => orPbTop(L, c) || '' });
    if (c.t === 'd') return D(c.k, c.ad, v);
    if (c.t === 'a') return M(c.k, c.ad, v, { h: r => v(r) ? ayEtiket(v(r)) : tire });
    return M(c.k, c.ad, r => String(v(r)), c.h ? { h: c.h } : null);
  }
  /** koşul: değer girilmemişse süzmez (dolu / boş hariç); sayı Türkçe yazılır (80.000 · 12,5); tarih gg.aa.yyyy ya da takvimden */
  function orUyar(kosul, r) {
    const c = orKol(kosul.a); if (!c) return true; const x = orDeger(c, r), o = kosul.o, v = String(kosul.v ?? '').trim(), bosMu = orSayisal(c) ? !Number(x) : String(x).trim() === '';
    if (o === 'dolu') return !bosMu; if (o === 'bos') return bosMu; if (v === '') return true;
    let a, b;
    if (orSayisal(c)) { a = Number(x) || 0; b = sayi(v); if (o === 'icerir') return String(x).includes(v); if (o === 'icermez') return !String(x).includes(v); }
    else if (c.t === 'd') { a = String(x); b = trIso(v); } else if (c.t === 'a') { a = String(x); b = /^\d{4}-\d{2}/.test(v) ? v.slice(0, 7) : trIso(v).slice(0, 7); } else { a = kucuk(x); b = kucuk(v); }
    if (o === 'icerir') return String(a).includes(String(b)); if (o === 'icermez') return !String(a).includes(String(b));
    const k = typeof a === 'number' ? a - b : String(a).localeCompare(String(b), TR);
    return o === '=' ? k === 0 : o === '!=' ? k !== 0 : o === '>=' ? k >= 0 : o === '<=' ? k <= 0 : o === '>' ? k > 0 : k < 0;
  }
  const orKosulMetni = () => orAyar.kosullar.filter(c => orKol(c.a) && (c.v !== '' || c.o === 'dolu' || c.o === 'bos')).map(c => { const col = orKol(c.a), op = (OR_OP.find(o => o[0] === c.o) || ['', c.o])[1]; return `${orBaslik(col)} ${op}${c.o === 'dolu' || c.o === 'bos' ? '' : ' ' + (col.t === 'd' ? trh(trIso(c.v)) : col.t === 'a' && /^\d{4}-\d{2}/.test(c.v) ? ayEtiket(c.v.slice(0, 7)) : c.v)}`; }).join(' · ');
  const orGruplar = () => { const g = []; orKolonlar().forEach(c => { if (!g.includes(c.g)) g.push(c.g); }); return g; };
  const orSecenekH = (secili, bosAd, uygun) => `<option value="">${esc(bosAd)}</option>` + orGruplar().map(g => { const l = orKolonlar().filter(c => c.g === g && (!uygun || uygun(c))); return l.length ? `<optgroup label="${esc(g)}">${l.map(c => `<option value="${esc(c.k)}"${c.k === secili ? ' selected' : ''}>${esc(orBaslik(c))}</option>`).join('')}</optgroup>` : ''; }).join('');
  function orSutunCiz() {
    $('#tkOrSutunlar').innerHTML = orGruplar().map(g => `<div class="tk-or-grup"><b>${esc(g)}</b>${orKolonlar().filter(c => c.g === g).map(c => `<label class="tk-or-sutun${orAyar.sutunlar.includes(c.k) ? ' secili' : ''}"><input type="checkbox" value="${esc(c.k)}"${orAyar.sutunlar.includes(c.k) ? ' checked' : ''}>${esc(orBaslik(c))}</label>`).join('')}</div>`).join('');
  }
  /** metin alanında değer listeden seçilir (o alandaki farklı değer 60'ı geçmiyorsa); tarih → takvim, ay → ay seçici, sayı → kutu */
  function orKosulCiz() {
    const hepsi = OR_KAYNAK[orAyar.kaynak].liste();
    $('#tkOrKosullar').innerHTML = orAyar.kosullar.map((c, i) => {
      const col = orKol(c.a), degersiz = c.o === 'dolu' || c.o === 'bos'; let deger;
      if (degersiz) deger = '<span></span>';
      else if (col.t === 'd') deger = `<input type="date" class="form-control orV" value="${esc(trIso(c.v))}" aria-label="Değer">`;
      else if (col.t === 'a') deger = `<input type="month" class="form-control orV" value="${esc(c.v)}" aria-label="Değer">`;
      else { const ds = col.t === 'm' && (c.o === '=' || c.o === '!=') ? [...new Set(hepsi.map(r => String(orDeger(col, r))).filter(Boolean))].sort((a, b) => a.localeCompare(b, TR)) : null;
        deger = ds && ds.length && ds.length <= 60 ? `<select class="form-select orV" aria-label="Değer"><option value="">Seçin…</option>${ds.concat(c.v && !ds.includes(c.v) ? [c.v] : []).map(d => `<option${d === c.v ? ' selected' : ''}>${esc(d)}</option>`).join('')}</select>` : `<input class="form-control orV" value="${esc(c.v)}" placeholder="${orSayisal(col) ? '80.000' : 'değer'}" aria-label="Değer">`; }
      return `<div class="tk-or-kosul" data-i="${i}"><select class="form-select orA" aria-label="Alan">${orSecenekH(c.a, 'Alan…')}</select><select class="form-select orO" aria-label="Karşılaştırma">${OR_OP.map(o => `<option value="${o[0]}"${o[0] === c.o ? ' selected' : ''}>${o[1]}</option>`).join('')}</select>${deger}<button type="button" class="tk-dgm orSil" title="Koşulu kaldır"><i class="fas fa-xmark"></i></button></div>`;
    }).join('') || '<div class="tk-or-bos">Koşul yok: bütün kayıtlar listelenir.</div>';
  }
  function orUstCiz() {
    $('#tkOrKaynak').innerHTML = Object.keys(OR_KAYNAK).map(k => `<option value="${k}"${k === orAyar.kaynak ? ' selected' : ''}>${esc(OR_KAYNAK[k].ad)}</option>`).join('');
    const grupUygun = c => c.t === 'm' || c.t === 'a' || c.t === 'd';
    $('#tkOrGrup1').innerHTML = orSecenekH(orAyar.grup[0] || '', 'Gruplama Yok', grupUygun); $('#tkOrGrup2').innerHTML = orSecenekH(orAyar.grup[1] || '', 'İkinci Düzey Yok', c => grupUygun(c) && c.k !== orAyar.grup[0]); $('#tkOrGrup2').disabled = !orAyar.grup[0];
    const sel = $('#tkOrKayitli'), L = K.ozelRaporlar || []; sel.innerHTML = '<option value="">Kayıtlı Raporlar</option>' + L.map(r => `<option value="${esc(r.ad)}"${r.ad === orAyar.ad ? ' selected' : ''} title="${esc((r.kullanici || '') + ' · ' + (r.zaman || ''))}">${esc(r.ad)}</option>`).join('');
    if (document.activeElement !== $('#tkOrAd')) $('#tkOrAd').value = orAyar.ad || '';
    $('#tkOrAyar').hidden = !orAcik; $('#tkOrAyarAc').checked = orAcik;
  }
  let orKurulu = '';
  function cizOzel() {
    const KY = OR_KAYNAK[orAyar.kaynak], imza = orAyar.kaynak + '|' + (K.ozelRaporlar || []).length;
    orUstCiz(); if (orKurulu !== imza || !$('#tkOrSutunlar').children.length) { orSutunCiz(); orKosulCiz(); orKurulu = imza; }
    const tum = KY.liste(), L = tum.filter(r => orAyar.kosullar.every(c => orUyar(c, r))), S = orKolonlar().filter(c => orAyar.sutunlar.includes(c.k)), gk = orAyar.grup.map(orKol).filter(Boolean);
    let satirSayisi = L.length;
    if (gk.length) {
      // GRUPLU: satır = grup; grup alanları + Kayıt + seçili sütunlardan toplanabilenler (₺ tutar, adet, belge PB tutarı)
      const m = new Map(); L.forEach(r => { const a = gk.map(c => String(orDeger(c, r))).join(' ¦ '); if (!m.has(a)) m.set(a, { a, l: [] }); m.get(a).l.push(r); });
      const G = [...m.values()], yaz = (c, r) => { const v = orDeger(c, r); return v === '' ? '(boş)' : c.t === 'd' ? trh(v) : c.t === 'a' ? ayEtiket(v) : String(v); };
      const kol = gk.map(c => M('g_' + c.k, c.ad, g => String(orDeger(c, g.l[0])), { h: g => `<b>${esc(yaz(c, g.l[0]))}</b>` }));
      kol.push(P('_n', `Adet (${KY.birim})`, g => g.l.length, { sinif: 'sayac', h: g => `<a href="#" class="tk-link" data-or-grup="${esc(g.a)}" title="Bu grubun satırlarını göster">${orSayiYaz(g.l.length)}</a>`, dipH: LL => orSayiYaz(topla(LL, g => g.l.length)) }));
      S.filter(c => c.t === 'p' || c.t === 'v' || (c.t === 'n' && c.top)).forEach(c => kol.push(P(c.k, orBaslik(c), g => topla(g.l, r => Number(orDeger(c, r)) || 0), c.t === 'v' ? { h: g => orPbTop(g.l, c) || tire, dipH: LL => orPbTop(LL.flatMap(g => g.l), c) || '' } : c.t === 'n' ? { h: g => orSayiYaz(topla(g.l, r => Number(orDeger(c, r)) || 0)), dipH: LL => orSayiYaz(topla(LL, g => topla(g.l, r => Number(orDeger(c, r)) || 0))) } : null)));
      tablo('ozel', kol, G, { anahtar: g => g.a, sira: { k: '_n', y: -1 }, bos: 'Koşullara uyan kayıt yok.', dipAd: (LL, ad) => `${ad} · ${LL.length} grup` });
      cizOzel.gruplar = m; satirSayisi = G.length;
    } else { tablo('ozel', S.map(orMotor), L, { anahtar: KY.anahtar, bos: 'Koşullara uyan kayıt yok.', dipAd: (LL, ad) => `${ad} · ${LL.length} ${KY.birim}` }); cizOzel.gruplar = null; }
    const km = orKosulMetni();
    $('#tkOrBaslik').innerHTML = `<i class="fas fa-filter me-1"></i>Özel Rapor${orAyar.ad ? ' · ' + esc(orAyar.ad) : ''} · ${esc(KY.ad)}${gk.length ? ' · ' + esc(gk.map(c => c.ad).join(' › ')) + ' Bazında' : ''}`;
    $('#tkOrNot').textContent = `${L.length} / ${tum.length} ${KY.birim}${gk.length ? ` · ${satirSayisi} grup` : ''} · ${km ? 'koşul: ' + km : 'koşul yok'}${KY.donem ? ' · ' + donemMetni() : ''}`;
    orSakla();
  }
  /** grup satırındaki kayıt sayısına tıklayınca: o grubun satırları seçili sütunlarla pencerede (en çok 500 satır) */
  function orGrupAc(a) {
    const g = cizOzel.gruplar && cizOzel.gruplar.get(a); if (!g) return; const KY = OR_KAYNAK[orAyar.kaynak], S = orKolonlar().filter(c => orAyar.sutunlar.includes(c.k)).map(orMotor), gk = orAyar.grup.map(orKol).filter(Boolean), l = g.l.slice(0, 500);
    const ad = gk.map(c => { const v = orDeger(c, g.l[0]); return `${esc(c.ad)}: <b>${esc(v === '' ? '(boş)' : c.t === 'd' ? trh(v) : c.t === 'a' ? ayEtiket(v) : v)}</b>`; }).join(' · ');
    penAc(ad, `${g.l.length} ${KY.birim}${g.l.length > l.length ? ` · ilk ${l.length} satır` : ''}`, `<div class="table-responsive"><table class="na-tablo tk-tablo"><thead><tr>${S.map(c => `<th class="sirasiz">${pbBaslik(c.ad)}</th>`).join('')}</tr></thead><tbody>${l.map(r => `<tr>${S.map(c => `<td>${c.h(r)}</td>`).join('')}</tr>`).join('')}</tbody><tfoot><tr class="toplam">${S.map((c, i) => `<td>${i === 0 ? `Toplam · ${g.l.length}` : c.top ? (c.dipH ? c.dipH(g.l) : tl(topla(g.l, c.top))) : ''}</td>`).join('')}</tr></tfoot></table></div>`);
  }
  async function orKaydet() {
    const ad = ($('#tkOrAd').value || '').trim(); if (!ad) { mesaj('warning', 'Rapor adı girin.'); $('#tkOrAd').focus(); return; }
    if ((K.ozelRaporlar || []).some(r => kucuk(r.ad) === kucuk(ad)) && kucuk(orAyar.ad) !== kucuk(ad) && !confirm(`"${ad}" adlı kayıtlı rapor var. Üzerine yazılsın mı?`)) return;
    const d = await post('/Tahsilat/OzelRaporKaydet', { Ad: ad, Ayar: JSON.stringify(Object.assign({}, orAyar, { ad })) }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    mesaj(d.success ? 'success' : 'danger', '<i class="fas fa-floppy-disk me-1"></i>' + esc(d.message)); if (d.success) { K.ozelRaporlar = d.ozelRaporlar; orAyar.ad = ad; cizOzel(); }
  }
  function orYukle(ad) { const r = (K.ozelRaporlar || []).find(x => x.ad === ad); if (!r) return; let a = null; try { a = JSON.parse(r.ayar); } catch (x) { } if (!a) { mesaj('danger', 'Kayıtlı rapor okunamadı.'); return; } orAyar = orTemizle(Object.assign(a, { ad: r.ad })); orKurulu = ''; delete SIRA.ozel; ciz(); }
  async function orSil() {
    const ad = $('#tkOrKayitli').value; if (!ad) { mesaj('info', 'Silinecek kayıtlı raporu seçin.'); return; } if (!confirm(`"${ad}" raporu herkes için silinecek. Emin misiniz?`)) return;
    const d = await post('/Tahsilat/OzelRaporSil', { Ad: ad }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    mesaj(d.success ? 'success' : 'danger', '<i class="fas fa-trash me-1"></i>' + esc(d.message)); if (d.success) { K.ozelRaporlar = d.ozelRaporlar; if (orAyar.ad === ad) orAyar.ad = ''; cizOzel(); }
  }
  if ($('#tkOrKaynak')) {
    const kosulYenile = () => { orKosulCiz(); cizOzel(); };
    $('#tkOrKaynak').addEventListener('change', e => { orAyar = orVarsayilan(e.target.value); orKurulu = ''; delete SIRA.ozel; ciz(); });   // veri kaynağı değişince sütun / koşul / gruplama o kaynağın varsayılanına döner
    $('#tkOrGrup1').addEventListener('change', e => { orAyar.grup = e.target.value ? [e.target.value].concat(orAyar.grup[1] && orAyar.grup[1] !== e.target.value ? [orAyar.grup[1]] : []) : []; delete SIRA.ozel; cizOzel(); });
    $('#tkOrGrup2').addEventListener('change', e => { orAyar.grup = orAyar.grup.slice(0, 1).concat(e.target.value ? [e.target.value] : []); cizOzel(); });
    $('#tkOrAyarAc').addEventListener('change', e => { orAcik = e.target.checked; cizOzel(); });
    $('#tkOrSutunlar').addEventListener('change', e => { const c = e.target.closest('input[type=checkbox]'); if (!c) return; if (c.checked) { if (!orAyar.sutunlar.includes(c.value)) orAyar.sutunlar.push(c.value); } else orAyar.sutunlar = orAyar.sutunlar.filter(x => x !== c.value); c.closest('label').classList.toggle('secili', c.checked); cizOzel(); });
    $('#tkOrSutunSifirla').addEventListener('click', e => { e.preventDefault(); orAyar.sutunlar = OR_KAYNAK[orAyar.kaynak].vars.slice(); orSutunCiz(); cizOzel(); });
    $('#tkOrKosulEkle').addEventListener('click', () => { const ilk = orKolonlar().find(c => c.t === 'p') || orKolonlar()[0]; orAyar.kosullar.push({ a: ilk.k, o: orSayisal(ilk) ? '>=' : '=', v: '' }); kosulYenile(); const son = $$('#tkOrKosullar .orV').pop(); if (son) son.focus(); });
    $('#tkOrKosullar').addEventListener('change', e => {
      const r = e.target.closest('.tk-or-kosul'); if (!r) return; const c = orAyar.kosullar[+r.dataset.i]; if (!c) return;
      if (e.target.classList.contains('orA')) { const col = orKol(e.target.value); if (!col) return; c.a = col.k; c.v = ''; c.o = orSayisal(col) ? '>=' : '='; kosulYenile(); return; }
      if (e.target.classList.contains('orO')) { c.o = e.target.value; kosulYenile(); return; }
      if (e.target.classList.contains('orV')) { c.v = e.target.value.trim(); cizOzel(); }
    });
    $('#tkOrKosullar').addEventListener('input', e => { if (!e.target.matches('input.orV')) return; const c = orAyar.kosullar[+e.target.closest('.tk-or-kosul').dataset.i]; if (c) { c.v = e.target.value.trim(); cizOzel(); } });
    $('#tkOrKosullar').addEventListener('click', e => { const b = e.target.closest('.orSil'); if (!b) return; orAyar.kosullar.splice(+b.closest('.tk-or-kosul').dataset.i, 1); kosulYenile(); });
    $('#tkOrKaydet').addEventListener('click', orKaydet); $('#tkOrAd').addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); orKaydet(); } });
    $('#tkOrKayitli').addEventListener('change', e => { if (e.target.value) orYukle(e.target.value); });
    $('#tkOrSil').addEventListener('click', orSil);
    $('#tkOrPdf').addEventListener('click', () => { if (!window.TabloExcel) { mesaj('danger', 'PDF aktarımı yüklenemedi.'); return; } window.TabloExcel.pdf('ozel_rapor', [{ tablo: '#tkTablo-ozel', ad: 'Özel Rapor', baslik: (IHRACAT ? 'İhracat Finansal Takibi · ' : 'Tahsilat · ') + $('#tkOrBaslik').textContent.trim(), altBaslik: $('#tkOrNot').textContent.trim() }]); });
    $('#tkTablo-ozel').addEventListener('click', e => {
      const g = e.target.closest('a[data-or-grup]'); if (g) { e.preventDefault(); orGrupAc(g.dataset.orGrup); return; }
      const t = e.target.closest('a[data-or-tah]'); if (t) { e.preventDefault(); tahsilatAc(t.dataset.orTah); return; }
      const k = e.target.closest('a[data-or-kart]'); if (k) { e.preventDefault(); kartAc(k.dataset.orKart); }
    });
    if (pPen) pPen.addEventListener('click', e => { const k = e.target.closest('a[data-or-kart]'), t = e.target.closest('a[data-or-tah]'); if (k) { e.preventDefault(); penKapat(); kartAc(k.dataset.orKart); } else if (t) { e.preventDefault(); penKapat(); tahsilatAc(t.dataset.orTah); } });
  }

  const CIZ = { ozel: cizOzel, ozet: cizOzet, cari: cizCari, kartlar: cizKartlar, acik: cizAcik, hafta: cizHafta, banka: cizBanka, aylik: cizAylik, plan: cizPlan, surec: cizSurec, vftakip: cizVfTakip, vade: cizVade, kapama: cizKapama, satis: cizSatis };
  function ciz() {
    if (!K) return; const rapor = RAPOR.includes(aktif), cubuk = aktif !== 'yeni' && aktif !== 'adat' && aktif !== 'cek';   // çubuk Tahsilatlar, Kesilen Fark Faturaları ve (19 Kas gece, kullanıcı) ihracat sekmelerinde de: müşteri kümesi ihracat listelerine gider (ibSuzgecYaz)
    $('#tkFiltreKart').style.display = cubuk ? '' : 'none'; $('#tkExcel').style.display = aktif === 'yeni' || aktif === 'adat' || aktif === 'cek' ? 'none' : '';
    if (aktif === 'cek') cekAc();
    yeniDugmeYerlestir(cubuk);
    $('#tkDonem').hidden = !(DONEMLI.includes(aktif) || (aktif === 'ozel' && OR_KAYNAK[orAyar.kaynak].donem));   // Özel Rapor: dönem yalnız tarihli veri kaynaklarında (Açık Faturalar'da yok)
    const cip = $('#tkCariCip'), c = F.cari && cariBul(F.cari); cip.hidden = !c; if (c) cip.innerHTML = `${esc(c.kod)} ${esc(c.kisaAd)} <i class="fas fa-xmark"></i>`;
    if (cubuk) { SZ.yaz(); $('#tkKapsam').textContent = `${cariler().length} / ${(BOLGE_KILIT ? K.satirlar.filter(k => k.bolge === BOLGE_KILIT) : K.satirlar).length} müşteri${PBG === 'USD' && usdKur() ? ` · 1 $ = ${n2(usdKur())}\u00A0₺` : ''}`; }
    if (rapor) { CIZ[aktif](); if (IHRACAT && aktif === 'plan') cizHafta(); } else if (IHRACAT && aktif === 'ibOzet') cizOzet();   // ihracat kipi: Tahsilat Özeti gövdesi İhracat Özeti panelinde; Haftalık Beklenen tabloları Planlanan ↔ Gerçekleşen'de
    if (IB(aktif)) ibSuzgecYaz();
    if (TH[aktif]) { disSuzgecYaz(); if (aktif === 'liste') cizVade(); }   // Tahsilatlar sekmesinin altında Gecikmeli Tahsilatlar (vade farkı kesilmiş / kesilmemiş; toplu kesim)
    bankaYeni(); talepUyari();
  }
  // 19 Kas 2026 gece (kullanıcı: "Yeni Tahsilat düğmesini tüm sekmelerde görünecek şekilde Temizle'nin soluna, sabit"): düğme çubuk görünürken #tkEk'in sonunda (Temizle'nin solu), çubuk gizliyken başlık sağında Excel'in solunda
  function yeniDugmeYerlestir(cubuk) {   // (kullanıcı, kesin): süzgeç çubuğunda TEMİZLE'NİN SOLUNDA; çubuğun olmadığı sekmelerde (Yeni Tahsilat, Adat) başlıkta Excel'in solunda
    const d = $('#tkYeniDgm'); if (!d) return; d.classList.toggle('aktif', aktif === 'yeni');
    if (cubuk) { const sag = $('#tkFiltreKart .sz-sag'), tem = $('#tkFiltreKart .sz-temizle'); if (sag && tem && d.nextElementSibling !== tem) sag.insertBefore(d, tem); }
    else { const sag = $('#tk > .na-ust .sag'), ex = $('#tkExcel'); if (sag && d.parentElement !== sag) sag.insertBefore(d, ex || null); }
  }
  { const d = $('#tkYeniDgm'); if (d) d.addEventListener('click', () => sekme('yeni')); }
  /** ihracat sekmelerine (ihracat-finans.js) çubuğun süzgeci (19 Kas gece, kullanıcı: "ihracat sekmelerine de aynı filtreyi uygula"): müşteri kümesi (şirket / bölge / sınıf / temsilci / ilişki / arama) + şirket listesi; değişmediyse yayınlanmaz */
  let ibSuzSon = '';
  function ibSuzgecYaz() {
    const aktifSuz = SUZ.some(s => s.k !== 'bolge' && s.k !== 'sirket' && F[s.k].length) || F.cari || F.ara;   // kilitli bölge tek başına küme daraltmaz (ihracat zaten yurtdışı); şirket müşteri kümesi değil, dosyanın kendi şirketiyle süzülür
    const c = aktifSuz ? cariler() : null;   // Excel'den gelen dosyalarda cari kodu kartla eşleşmeyebilir (120.02.xxx) → müşteri ADI da eşleme anahtarı
    const d = { kodlar: c ? [...new Set(c.map(k => k.kod))] : null, adlar: c ? [...new Set(c.flatMap(k => [k.kisaAd, k.resmiUnvan]).filter(Boolean).map(a => a.toLocaleUpperCase('tr-TR')))] : null, sirketler: F.sirket.slice(), ara: F.ara };
    const key = JSON.stringify(d); if (key === ibSuzSon) return; ibSuzSon = key; window.ibDisSuzgec = d; window.dispatchEvent(new CustomEvent('ibSuzgec', { detail: d }));
  }
  /** tahsilat.js listelerine (Tahsilatlar, Kesilen Fark Faturaları) çubuğun süzgeci: dönem, müşteri kümesi (cari süzgeci / arama varsa), işlem tipi */
  function disSuzgecYaz() { const aktifSuz = suzAktif() || F.cari || F.ara; window.thSuzgec = { bas: DN.bas, bit: DN.bit, kodlar: aktifSuz ? new Set(cariler().map(k => k.kod)) : null, tipler: F.tip.slice(), doviz: DV }; window.dispatchEvent(new CustomEvent('thSuzgec')); }
  /** şerit düğmesi + panel seçimi (ihracat sekmeleri tek panelde: data-p="ib"); ihracat-finans.js kendi sekmesini değiştirince de buradan işaretlenir (window.TahsilatTakip.isaretle) */
  function isaretle(p, ilk) {
    aktif = p; $$('#tkSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    const panel = IB(p) ? 'ib' : TH[p] ? 'th' : p; $$('.na-panel', kok).filter(x => x.parentElement === kok).forEach(x => x.classList.toggle('aktif', x.dataset.p === panel));
    if (!ilk) { const q = new URLSearchParams(location.search); q.set('sekme', p); history.replaceState(null, '', location.pathname + '?' + q.toString()); }
  }
  function sekme(p, ilk) {
    if (IHRACAT && p === 'hafta') p = 'plan';   // ihracat kipi: Haftalık Beklenen tabloları Planlanan ↔ Gerçekleşen'de
    if (p === 'vade') p = 'liste';              // 19 Kas 2026 gece (kullanıcı): Gecikmeli Tahsilatlar ayrı sekme değil — Tahsilatlar sekmesinin altında (her iki raporda)
    if (p !== 'yeni' && !$$('#tkSekmeler button').some(b => b.dataset.p === p)) p = IB(p) && IHRACAT ? 'ibOzet' : 'cari';   // şeritte olmayan sekme (ör. ihracat kipinde Vade Farkı Takibi) → Alacak Takibi; 'yeni' şeritte değil, sabit düğme
    isaretle(p, ilk);
    if (TH[p]) { const ic = $(`#thSekmeler button[data-p="${TH[p]}"]`); if (ic && !ic.classList.contains('aktif')) ic.click(); }
    if (IB(p)) {   // ihracat paneli: ihracat-finans.js çizer (ilk açılışta kendi başlangıcı); Tahsilat çubuğu burada da görünür ve müşteri kümesini ihracat listelerine verir; İhracat Özeti'ndeki Tahsilat Özeti gövdesi bu betikten
      if (window.IhracatFinans) window.IhracatFinans.sekme(p.slice(2, 3).toLowerCase() + p.slice(3));
      if (K && kirli && p === 'ibOzet') yukle(); else ciz(); kirli = p !== 'ibOzet'; return;
    }
    if ((RAPOR.includes(p) || p === 'liste') && kirli) yukle(); else ciz();   // Tahsilatlar sekmesi de rapor verisi okur (altındaki Gecikmeli Tahsilatlar tablosu)
    if (!RAPOR.includes(p)) kirli = true;   // giriş / adat panellerinde kayıt değişmiş olabilir: rapor sekmesine dönünce veri yeniden okunur
  }
  window.TahsilatTakip = { sekme: p => sekme(p), isaretle: p => isaretle(p), aktif: () => aktif }; window.__tkSZ = SZ;
  // tahsilat.js kendi panelini değiştirirse (kayıttan sonra Tahsilatlar'a geçer, ?no ile açılır) üst şerit de onu gösterir
  const icSerit = $('#thSekmeler'); if (icSerit) new MutationObserver(() => { const a = $('button.aktif', icSerit); if (!a || !TH[aktif] || TH[aktif] === a.dataset.p) return; aktif = a.dataset.p; $$('#tkSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === aktif)); ciz(); }).observe(icSerit, { attributes: true, subtree: true, attributeFilter: ['class'] });
  window.addEventListener('thHazir', () => { if (TH[aktif]) { const ic = $(`#thSekmeler button[data-p="${TH[aktif]}"]`); if (ic && !ic.classList.contains('aktif') && !q0.get('no') && !kok.dataset.no) ic.click(); } });
  const thYenile = d => window.dispatchEvent(new CustomEvent('thYenile', { detail: { tahsilatlar: d.tahsilatlar, faturalar: d.faturalar } }));
  const tahsilatAc = no => { sekme('liste'); window.dispatchEvent(new CustomEvent('thDetayAc', { detail: no })); };

  // ---------- banka hareketine müşteri seçimi ----------
  const modal = $('#tkModal'); let MS = null;   // { h, kod }
  function musteriListe() {
    const q = kucuk($('#tkMAra').value.trim()), on = MS.oneri.map(m => m.kod);
    const l = (B.musteriler || []).filter(m => !q || kucuk(m.kod + ' ' + m.ad + ' ' + (m.unvan || '')).includes(q)).sort((a, b) => (on.includes(b.kod) ? 1 : 0) - (on.includes(a.kod) ? 1 : 0) || (b.sirket === MS.h.sirket ? 1 : 0) - (a.sirket === MS.h.sirket ? 1 : 0)).slice(0, 80);
    $('#tkMListe').innerHTML = l.map(m => `<div data-kod="${esc(m.kod)}" class="${m.kod === MS.kod ? 'secili' : ''}"><b>${esc(m.kod)}</b><span>${esc(m.ad)}${on.includes(m.kod) ? ' <i>ÖNERİ</i>' : ''}</span><small>${esc(m.sirket || '')}</small></div>`).join('') || '<div>Aramaya uyan müşteri yok.</div>';
  }
  function musteriAc(h, yalnizTaslak) {
    MS = { h, kod: yalnizTaslak ? h.cariKod : '', oneri: oneriler(h), yalnizTaslak }; if (!MS.kod && MS.oneri.length) MS.kod = MS.oneri[0].kod;
    $('#tkMAlt').textContent = `${trh(h.tarih)} · ${h.sirket} · ${h.banka} · ${h.karsiAd || 'gönderen yok'} · ${tl(h.tutar)}${h.aciklama ? ' · ' + h.aciklama : ''}`;
    $('#tkMAra').value = ''; $('#tkMHata').textContent = ''; $('#tkMTaslak').checked = !!yalnizTaslak; musteriListe(); modal.hidden = false; $('#tkMAra').focus();   // 19 Kas 2026: varsayılan "Yeni Tahsilat'ta Aç"; taslak kutusu yalnız Taslak Tahsilat düğmesinden gelince işaretli
  }
  // yeniTahsilat = true (19 Kas 2026): müşteri eşlenir, taslak açılmaz; hareket Yeni Tahsilat'a ödeme satırı olarak taşınır (tahsilat.js 'thOnDoldur'), kapama / vade farkı / adat orada
  async function musteriKaydet(yeniTahsilat) {
    if (!MS.kod) { $('#tkMHata').textContent = 'Listeden müşteri seçin.'; return; } const dg = $('#tkMKaydet'), dy = $('#tkMYeni'); dg.disabled = true; if (dy) dy.disabled = true;
    const h = MS.h, kod = MS.kod;
    const d = await post('/Tahsilat/BankaMusteriSec', { Id: h.id, CariKod: kod, TaslakOlustur: !yeniTahsilat && $('#tkMTaslak').checked }).catch(() => ({ success: false, message: 'Ağ hatası.' })); dg.disabled = false; if (dy) dy.disabled = false;
    if (!d.success) { $('#tkMHata').textContent = d.message || 'Kaydedilemedi.'; return; }
    const i = B.hareketler.findIndex(x => x.id === d.hareket.id); if (i >= 0) B.hareketler[i] = d.hareket; if (d.tahsilatlar) { K.tahsilatlar = d.tahsilatlar; K.faturalar = d.faturalar; thYenile(d); }
    modal.hidden = true; MS = null; mesaj('success', '<i class="fas fa-user-check me-1"></i>' + esc(d.message) + (yeniTahsilat ? ' Hareket Yeni Tahsilat\'a ödeme satırı olarak taşındı; kontrol edip Kaydet ve İşle.' : '')); ciz();
    if (yeniTahsilat) { sekme('yeni'); window.dispatchEvent(new CustomEvent('thOnDoldur', { detail: { kod, tarih: String(h.tarih || '').slice(0, 10), tutar: h.tutar, pb: h.pb || 'TL', yontem: 'Havale / EFT', banka: h.banka || '', belgeNo: h.no || '', aciklama: `Banka hareketi ${h.no || ''} · ${h.banka || ''}${h.karsiAd ? ' · ' + h.karsiAd : ''}${h.aciklama ? ' · ' + h.aciklama : ''}` } })); }
  }
  modal.addEventListener('click', e => {
    if (e.target === modal || e.target.closest('[data-m="kapat"]')) { modal.hidden = true; MS = null; return; }
    const s = e.target.closest('#tkMListe [data-kod]'); if (s) { MS.kod = s.dataset.kod; $('#tkMHata').textContent = ''; musteriListe(); return; }
    if (e.target.closest('#tkMKaydet')) musteriKaydet(false);
    if (e.target.closest('#tkMYeni')) musteriKaydet(true);
  });
  $('#tkMAra').addEventListener('input', musteriListe);
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !modal.hidden) { modal.hidden = true; MS = null; } });

  // ---------- toplu vade farkı faturası ----------
  async function vadeKes(nolar) {
    const l = vadeFarklilar().filter(t => nolar.includes(t.no) && !t.vadeFarkiFaturaNo); if (!l.length) return;
    if (!confirm(`${l.length} tahsilat için vade farkı faturası kesilsin mi?\n\nMatrah ${tl2(topla(l, t => t.vadeFarki))} · KDV dahil ${tl2(topla(l, t => t.vadeFarki + vfKdv(t)))}\nHer tahsilat için ayrı fatura kesilir ve müşterinin carisine borç yazılır.`)) return;
    const d = await post('/Tahsilat/TopluVadeFarki', { Nolar: l.map(t => t.no) }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (d.tahsilatlar) { K.tahsilatlar = d.tahsilatlar; K.faturalar = d.faturalar; thYenile(d); } SECILI.clear();
    mesaj(d.success ? 'success' : 'danger', '<i class="fas fa-file-invoice-dollar me-1"></i>' + esc(d.message)); kirli = true; ciz();
  }

  // ---------- tıklamalar ----------
  kok.addEventListener('click', e => {
    const hz = e.target.closest('[data-h]'); if (hz) { H[hz.dataset.h] = !H[hz.dataset.h]; ciz(); return; }
    const vdb = e.target.closest('[data-vd]'); if (vdb) { H.vadeDurum = vdb.dataset.vd; ciz(); return; }
    const avb = e.target.closest('[data-av]'); if (avb) { H.acikVade = avb.dataset.av === H.acikVade ? 'tumu' : avb.dataset.av; ciz(); return; }
    const dn = e.target.closest('#tkDonem button[data-donem]'); if (dn) { donemKur(dn.dataset.donem); return; }
    const uy = e.target.closest('[data-uyum]'); if (uy) { e.preventDefault(); e.stopPropagation(); uyumAc(uy.dataset.uyum); return; }
    const km = e.target.closest('[data-kalem]'); if (km) { e.preventDefault(); e.stopPropagation(); kalemAc(km.dataset.kalem, km.dataset.bolum); return; }
    const il = e.target.closest('[data-ilt]'); if (il) { e.preventDefault(); e.stopPropagation(); iPencere.dataset.talep = il.dataset.talep || ''; iltAc(il.dataset.ilt, il.dataset.talep, il.dataset.tur); return; }
    // 19 Kas 2026: son fatura → Fatura İncelemesi penceresi (tahsilat.js dinler); tutar / tarih → Tahsilatlar sekmesi (müşteri + dönem); ay rozeti → Kesilen Fark Faturaları (o ay)
    const fa = e.target.closest('[data-fat-ac]'); if (fa) { e.preventDefault(); e.stopPropagation(); window.dispatchEvent(new CustomEvent('thFaturaAc', { detail: { kod: fa.dataset.kod, no: fa.dataset.fatAc } })); return; }
    const bl = e.target.closest('[data-belge]'); if (bl) { e.preventDefault(); e.stopPropagation(); belgeAc(bl.dataset.kod, bl.dataset.belge); return; }
    const tlst = e.target.closest('[data-tahliste]'); if (tlst) { e.preventDefault(); e.stopPropagation(); odemelerAc(tlst.dataset.tahliste, tlst.dataset.dn || 'tumu'); return; }
    const kpo = e.target.closest('[data-kapama-oneri]'); if (kpo) { e.preventDefault(); e.stopPropagation(); kapamaOneriAc(kpo.dataset.kapamaOneri); return; }
    const kpk = e.target.closest('[data-kapat-cari]'); if (kpk) { e.preventDefault(); e.stopPropagation(); kapamaCalistir([kpk.dataset.kapatCari]); return; }
    if (e.target.closest('#tkKapamaSec')) { kapamaCalistir([...SECK]); return; }
    if (e.target.closest('#tkKapamaHepsi')) { kapamaCalistir(($('#tkTablo-kapama')._liste || []).filter(k => k.kapama && k.kapama.oneri > 0).map(k => k.kod)); return; }
    if (e.target.id === 'tkKapHepsi') { ($('#tkTablo-kapama')._liste || []).forEach(k => e.target.checked ? SECK.add(k.kod) : SECK.delete(k.kod)); ciz(); return; }
    const sk = e.target.closest('[data-seck]'); if (sk) { sk.checked ? SECK.add(sk.dataset.seck) : SECK.delete(sk.dataset.seck); ciz(); return; }
    const vfa = e.target.closest('[data-vfay]'); if (vfa) { const [b, s] = vfa.dataset.vfay.split('|'); DN.bas = b; DN.bit = s; DN.ad = ''; donemYaz(); sekme('fatura'); return; }
    const hz2 = e.target.closest('[data-hizli]'); if (hz2) { e.preventDefault(); e.stopPropagation(); hizliDuzenle(hz2); return; }
    if (e.target.closest('.tk-hizli-form')) { e.stopPropagation(); const f = e.target.closest('.tk-hizli-form'); if (e.target.closest('[data-hizli-kaydet]')) hizliKaydet(f); else if (e.target.closest('[data-hizli-vazgec]')) ciz(); return; }
    const ka = e.target.closest('[data-kart]'); if (ka) { if (ka.tagName === 'TR' && e.target.closest('a,button,input,select')) return; e.preventDefault(); e.stopPropagation(); kartAc(ka.dataset.kart); return; }
    const ta = e.target.closest('[data-tahsilat]'); if (ta) { e.preventDefault(); tahsilatAc(ta.dataset.tahsilat); return; }
    const ms = e.target.closest('[data-musteri]'); if (ms) { const h = B.hareketler.find(x => x.id === +ms.dataset.musteri); if (h) musteriAc(h); return; }
    const tz = e.target.closest('[data-taslak]'); if (tz) { const h = B.hareketler.find(x => x.id === +tz.dataset.taslak); if (h) { MS = { h, kod: h.cariKod, oneri: [] }; $('#tkMTaslak').checked = true; musteriKaydet(); } return; }
    const ks = e.target.closest('[data-kes]'); if (ks) { vadeKes([ks.dataset.kes]); return; }
    if (e.target.closest('#tkOranUygula')) { const v = sayi(($('#tkOranToplu') || {}).value); if (!$('#tkOranToplu').value.trim()) { mesaj('warning', 'Oran yazın (0 = varsayılan).'); return; } oranKaydet([...SECM], v); return; }
    if (e.target.closest('#tkVadeKes')) { vadeKes([...SECILI]); return; }
    const nt = e.target.closest('[data-not]'); if (nt) { notAc(nt.dataset.not); return; }
    const ns = e.target.closest('[data-notsil]'); if (ns) { e.preventDefault(); notSil(+ns.dataset.notsil); return; }
    const hr = e.target.closest('[data-haric]'); if (hr) { robotHaric(hr.dataset.haric, true, false); return; }
    const dh = e.target.closest('[data-dahil]'); if (dh) { robotHaric(dh.dataset.dahil, false); return; }
    if (e.target.closest('#tkRobot')) { robotCalistir(); return; }
    const gr = e.target.closest('[data-g]'); if (gr) { SG = gr.dataset.g; delete SIRA.surec; ciz(); return; }
    const dr = e.target.closest('[data-durdur]'); if (dr) { satisDurdur(dr.dataset.durdur); return; }
    const mu = e.target.closest('[data-mut]'); if (mu) { e.preventDefault(); mutabakatGonder([mu.dataset.mut]); return; }
    if (e.target.closest('#tkMutGonder')) { mutabakatGonder([...SECM]); return; }
    if (e.target.closest('#tkVfSimdi')) { vfSimdiKes([...SECM]); return; }
    const dt = e.target.closest('tr[data-detay]'); if (dt && !e.target.closest('a,button,input,select')) { detayAc(dt.dataset.detay); return; }
    const gk = e.target.closest('#tkKpi [data-git], #tkTablo-is [data-git]'); if (gk) { const [p, ek] = gk.dataset.git.split(':'); if (ek === 'gecmis') H.gecmis = true; if (p === 'vftakip') { H.vfGecmis = H.vfLimit = H.vfOdemeYok = false; if (ek) H[ek] = true; } if (p === 'surec') { SG = ek === 'talep' ? 'talep' : 'musteri'; H.surecGecmis = H.surecAranmayan = false; if (ek && ek !== 'talep') H[ek] = true; } sekme(p); return; }
    if (e.target.closest('input[type=checkbox]')) return;   // seçim kutuları satır tıklamasını tetiklemez
    const sz = e.target.closest('tr[data-suz]'); if (sz) { const a = sz.dataset.suz, v = sz.dataset.deger; if (K.satirlar.some(k => SUZ.find(s => s.k === a).v(k).includes(v))) { F[a] = F[a].length === 1 && F[a][0] === v ? [] : [v]; sekme('cari'); } return; }
    const hf = e.target.closest('tr[data-hafta]'); if (hf) { haftaSec = +hf.dataset.hafta; ciz(); return; }
    const cr = e.target.closest('tr[data-cari]'); if (cr && !e.target.closest('a,button,input')) { F.cari = cr.dataset.cari; sekme('acik'); return; }
    if (e.target.closest('#tkCariCip')) { F.cari = ''; ciz(); }
  });
  kok.addEventListener('change', e => {
    const ts = e.target.closest('[data-tsec]'); if (ts) { const S = secKume(ts.dataset.tsec); ts.checked ? S.add(ts.dataset.a) : S.delete(ts.dataset.a); ciz(); return; }
    const th = e.target.closest('[data-tsec-hepsi]'); if (th) { const id = th.dataset.tsecHepsi, S = secKume(id), t = $('#tkTablo-' + id); $$('tbody [data-tsec]', t).forEach(x => th.checked ? S.add(x.dataset.a) : S.delete(x.dataset.a)); ciz(); return; }
    if (e.target.id === 'tkVadeHepsi') { const l = $('#tkTablo-vade')._liste.filter(t => vfDurum(t) === 'Kesilmedi'); l.forEach(t => e.target.checked ? SECILI.add(t.no) : SECILI.delete(t.no)); ciz(); return; }
    const s = e.target.closest('[data-sec]'); if (s) { s.checked ? SECILI.add(s.dataset.sec) : SECILI.delete(s.dataset.sec); ciz(); return; }
    if (e.target.id === 'tkVfHepsi') { $('#tkTablo-vftakip')._liste.forEach(k => e.target.checked ? SECM.add(k.kod) : SECM.delete(k.kod)); ciz(); return; }
    const sm = e.target.closest('[data-secm]'); if (sm) { sm.checked ? SECM.add(sm.dataset.secm) : SECM.delete(sm.dataset.secm); ciz(); return; }
    if (e.target.id === 'tkKanal') { KANAL = e.target.value; ciz(); return; }
    if (e.target.id === 'tkDonemBas' || e.target.id === 'tkDonemBit') { DN.bas = $('#tkDonemBas').value; DN.bit = $('#tkDonemBit').value; DN.ad = ''; donemYaz(); ciz(); return; }
    if (e.target.id === 'tkDoviz') { DV = e.target.checked; if (DV) { PBG = ''; $('#tkUsd').checked = false; } try { localStorage.setItem('tkDoviz', DV ? '1' : '0'); localStorage.setItem('tkPb', PBG); } catch (x) { } ciz(); return; }
    if (e.target.id === 'tkUsd') { PBG = e.target.checked ? 'USD' : ''; if (PBG) { DV = false; $('#tkDoviz').checked = false; } try { localStorage.setItem('tkPb', PBG); localStorage.setItem('tkDoviz', DV ? '1' : '0'); } catch (x) { } ciz(); return; }   // iki görünüm birlikte olmaz: dövizli = belge para birimi, $ = hepsi dolar
    if (e.target.id === 'tkOto') { const acik = e.target.checked; post('/Tahsilat/OtoYanitAyarla', { Acik: acik }).then(d => { K.otoYanit = acik; mesaj(d.success ? 'success' : 'danger', '<i class="fab fa-whatsapp me-1"></i>' + esc(d.message)); }); return; }
    if (e.target.id === 'tkDonemSec') { ND = e.target.value; ciz(); }
  });
  $$('#tkSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  // İHRACAT ŞERİDİ YERLEŞİMİ (20 Kas 2026; kullanıcı: "alt satırdaki sekmeleri rapor genişliğine sığdır, kalanları üst satıra yerleştir; yukarıda az, aşağıda çok; harfleri küçültme"):
  //   bütün sekmeler görünür; satır kırıcı (.tk-satir-kir) sondan başlayarak genişliğe sığan en çok sekmenin önüne konur. Hepsi tek satıra sığarsa kırıcı sona alınır. Genişlik değişince yeniden ölçülür.
  if (IHRACAT) {
    const serit = $('#tkSekmeler'), kir = serit && $('.tk-satir-kir', serit);
    const seritYerlestir = () => {
      const dg = $$('button[data-p]', serit), W = serit.clientWidth - 28; if (!dg.length || W <= 0) return;   // 28 px pay: sonradan eklenen zil ikonu ve yuvarlama
      const gap = parseFloat(getComputedStyle(serit).columnGap) || 0, gen = b => { const cs = getComputedStyle(b); return b.getBoundingClientRect().width + parseFloat(cs.marginLeft) + parseFloat(cs.marginRight); };
      // yazı boyu hiç küçülmez; iki satıra sığmıyorsa önce ikonlar gizlenir (ikonsuz), yine sığmıyorsa düğme iç boşluğu daralır (sik)
      let i = dg.length, ust = 0;
      for (const kip of [[], ['ikonsuz'], ['ikonsuz', 'sik']]) {
        serit.classList.remove('ikonsuz', 'sik'); kip.forEach(c => serit.classList.add(c));
        let alt = 0; i = dg.length; while (i > 0) { const w = gen(dg[i - 1]) + (i < dg.length ? gap : 0); if (alt + w > W) break; alt += w; i--; }
        ust = dg.slice(0, i).reduce((a, b) => a + gen(b) + gap, 0); if (ust <= W + gap) break;
      }
      if (i <= 0) { if (serit.lastElementChild !== kir) serit.appendChild(kir); } else if (kir.nextElementSibling !== dg[i]) serit.insertBefore(kir, dg[i]);
    };
    if (kir) { seritYerlestir(); new ResizeObserver(seritYerlestir).observe(serit); if (document.fonts && document.fonts.ready) document.fonts.ready.then(seritYerlestir); setTimeout(seritYerlestir, 1200); }
  }
  // süzgeç değişimi, arama ve Temizle ortak çubuktan gelir (SZ → degisti → ciz); Temizle müşteri seçimini de kaldırır
  // Excel: ekrandaki tablo(lar) göründüğü gibi
  $('#tkExcel').addEventListener('click', () => {
    if (!window.TabloExcel) { mesaj('danger', 'Excel aktarımı yüklenemedi.'); return; }
    if (IB(aktif)) { if (window.IhracatFinans) window.IhracatFinans.excel(); return; }   // ihracat sekmeleri: ihracat-finans.js'in Excel'i (açık sekmenin tablosu)
    const S = { ozet: [['#tkTablo-is', 'Bekleyen İşler'], ['#tkTablo-sirket', 'Şirket Özeti'], ['#tkTablo-temsilci', 'Temsilci Özeti']], cari: [['#tkTablo-cari', 'Alacak Takibi']], kartlar: [['#tkTablo-kartlar', 'Müşteri Cari Kartları']], acik: [['#tkTablo-acik', 'Açık Faturalar']], hafta: [['#tkTablo-hafta', 'Haftalık Beklenen'], ['#tkTablo-haftaDetay', 'Haftanın Faturaları']],
      banka: [['#tkTablo-banka', 'Banka Tahsilatları']], surec: [['#tkTablo-surec', SG === 'not' ? 'Görüşme Notları' : 'Aylık Süreç']], vftakip: [['#tkTablo-vftakip', 'Vade Farkı Takibi']], vade: [['#tkTablo-vade', 'Gecikmeli Tahsilatlar']], kapama: [['#tkTablo-kapama', 'Kapama Robotu']], liste: [['#thListe', 'Tahsilatlar'], ['#tkTablo-vade', 'Gecikmeli Tahsilatlar']], fatura: [['#thFaturaListe', 'Kesilen Fark Faturaları']],
      ozel: [['#tkTablo-ozel', 'Özel Rapor' + (orAyar.ad ? ' · ' + orAyar.ad : '')]],
      // Satış Raporu: yalnız ekranda görünen kırılımlar (Görünüm düğmesi) aktarılır
      satis: [['#tkTablo-satisTip', 'Aylık Satışlar · X - R'], ['#tkTablo-satisSinif', 'Aylık Satışlar · Cari Sınıfı'], ['#tkTablo-satisTemsilci', 'Aylık Satışlar · Satış Temsilcisi'], ['#tkTablo-satisMusteri', 'Aylık Satışlar · Müşteri'], ['#tkTablo-satisUlke', 'Aylık Satışlar · Ülke'], ['#tkTablo-satisSube', 'Şube Satışları'], ['#tkTablo-satisSubeYil', 'Şube Satışları · Yıllara Göre']].filter(x => { const el = $(x[0]), kart = el && el.closest('[data-sg]'); return el && !(kart && kart.hidden); }),
      aylik: [['#tkTablo-aylikPazar', 'Aylık Tahsilatlar · Yurtiçi / Yurtdışı'], ['#tkTablo-aylikSinif', 'Aylık Tahsilatlar · Cari Sınıfı'], ['#tkTablo-aylikTemsilci', 'Aylık Tahsilatlar · Satış Temsilcisi'], ['#tkTablo-aylikYontem', 'Aylık Tahsilatlar · Yöntem'], ['#tkTablo-aylikCek', 'Vade Ayına Göre Çek · Senet'], ['#tkTablo-aylikKisi', 'Tahsilatı Yapan Kişi']], plan: [['#tkTablo-planVade', 'Vade Ayına Göre Planlanan'], ['#tkTablo-planAy', 'Tahsilat Ayına Göre'], ['#tkTablo-planVf', 'Vade Farkı Doğan - Kesilen'], ...(IHRACAT ? [['#tkTablo-hafta', 'Haftalık Beklenen'], ['#tkTablo-haftaDetay', 'Haftanın Faturaları']] : [])] }[aktif]; if (!S) return;
    window.TabloExcel.indir('tahsilat_' + aktif, S.map(x => ({ tablo: x[0], ad: x[1], baslik: 'Tahsilat · ' + x[1] })));
  });

  // ---------- veri ----------
  const secDoldur = (el, l, v) => { el.innerHTML = '<option value="">Tümü</option>' + l.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); el.value = l.includes(v) ? v : ''; };
  async function yukle() {
    try {
      const [k, b] = await Promise.all([fetch('/Tahsilat/KonsolideTakip').then(r => r.json()), fetch('/Tahsilat/BankaHareketleri').then(r => r.json())]);
      K = k; B = b; kirli = false;
      ciz();
    } catch (x) { mesaj('danger', 'Veri yüklenemedi: ' + esc(x.message)); }
  }
  // 19 Kas 2026 gece (kullanıcı: "İhracat Özet'i baştan yerleştir, gereksiz boşluk kalmasın"): İhracat Özeti paneli — KPI kutuları üstte tam genişlik, bütün kartlar (tahsilat + ihracat özetleri) TEK sütun akışında (CSS columns; kart yükseklikleri farklı olsa da boşluk kalmaz)
  if (IHRACAT) {
    const oz = $('#ibkb .na-panel[data-p="ozet"]'); const row = oz && oz.querySelector('.row');
    if (oz && row) {
      const mas = document.createElement('div'); mas.className = 'ib-ozet-mas';
      const al = sel => { const k = $(sel); return k ? k.closest('.na-kart') : null; };
      const sira = ['#tkTablo-is', '#tkOz-bolge', '#ibTablo-ozetSirket', '#tkOz-vade', '#ibTablo-ozetTemsilci', '#tkOz-ay', '#tkTablo-temsilci', '#tkTablo-sirket', '#ibTablo-ozetSatis', '#ibTablo-ozetTahsilat', '#tkOz-risk', '#ibTablo-ozetBakiye', '#tkOz-banka', '#ibTablo-ozetUlke', '#tkOz-vf', '#ibTablo-ozetDoviz', '#ibTablo-ozetDurum', '#tkOz-gorev', '#ibTablo-ozetSube', '#ibTablo-ozetGorev', '#ibTablo-ozetGorevPerf', '#ibTablo-ozetAy'];
      sira.map(al).filter(Boolean).forEach(k => mas.appendChild(k));
      oz.querySelectorAll('.tk-oz-mas > .na-kart, .row .na-kart').forEach(k => mas.appendChild(k));   // listede unutulan kart kalmasın
      row.replaceWith(mas); oz.querySelectorAll('.tk-iki, .tk-oz-mas, .tk-ozet-ic > .na-kart-b, .tk-ozet-ic ~ .na-kart-b').forEach(e => { if (e.children.length === 0 || e.classList.contains('na-kart-b')) e.remove(); });
    }
  }
  // Gecikmeli Tahsilatlar kartı (Index'te #tkVadeKart) Tahsilatlar panelinin (#th › liste) altına taşınır — tahsilat.js'in paneli, tahsilat-takip.js'in tablosu
  { const vk = $('#tkVadeKart'), lp = $('#th .na-panel[data-p="liste"]'); if (vk && lp) lp.appendChild(vk); }
  $('#tkDoviz').checked = DV && !PBG; $('#tkUsd').checked = PBG === 'USD'; if (PBG) DV = false; donemYaz();
  sekme(aktif, true); yukle();
  // banka girişleri 60 saniyede bir yeniden okunur: yeni giriş düşünce sekmede uyarı çıkar (açık pencere ya da yarım kalan seçim varken tablo yeniden çizilmez)
  setInterval(async () => {
    if (document.hidden || !K) return; const b = await fetch('/Tahsilat/BankaHareketleri').then(r => r.json()).catch(() => null); if (!b || !b.hareketler) return;
    B = b; if (aktif === 'banka' && modal.hidden) ciz(); else bankaYeni();
  }, 60000);
})();

// ============================================================
// SAYFAYI DOLDUR (20 Kas 2026, kullanıcı: "sayfaları daha verimli kullan, aşağı kadar getir" — Tahsilat ve İhracat Finansal Takibi):
//   sekmenin en alttaki tablosu ekranın (çerçevenin) dibine kadar uzar; satırlar sığmazsa SAYFA değil TABLO kayar — üstteki sekme şeridi ve süzgeçler,
//   sütun başlıkları (yapışkan) ve dip toplam (yapışkan) hep ekranda kalır. Yan yana kartlı sekmelerde (Özet, Aylık Tahsilatlar, Planlanan ↔ Gerçekleşen) ve
//   tabloya 280 px'ten az yer kalan sekmelerde uygulanmaz (sayfa eskisi gibi kayar). Her çizimden, sekme değişiminden ve pencere boyutu değişiminden sonra yeniden ölçülür.
// ============================================================
(() => {
  'use strict';
  const kok = document.querySelector('.tk'); if (!kok || !window.MutationObserver) return;
  const gor = e => e.offsetParent !== null, kutu = e => e.getBoundingClientRect(), saydam = c => !c || c === 'transparent' || /rgba\(\s*0,\s*0,\s*0,\s*0\s*\)/.test(c);
  const temizle = () => kok.querySelectorAll('.tk-tam').forEach(e => { e.classList.remove('tk-tam'); ['height', 'max-height', 'overflow'].forEach(p => e.style.removeProperty(p)); e.querySelectorAll('thead th, tfoot td, tfoot th').forEach(h => { h.style.removeProperty('top'); if (h.dataset.tamZemin) { h.style.removeProperty('background-color'); delete h.dataset.tamZemin; } }); });
  function sigdir() {
    const onceki = kok.querySelector('.tk-tam'), kaydir = onceki ? [onceki, onceki.scrollTop, onceki.scrollLeft] : null;
    temizle();
    const kaplar = [...kok.querySelectorAll('.table-responsive')].filter(e => gor(e) && e.querySelector('table') && !e.closest('.tk-oz-mas, .ib-ozet-mas, .tk-oz, .tk-pencere, .modal'));
    if (!kaplar.length) return;
    const hedef = kaplar.reduce((a, b) => kutu(b).bottom > kutu(a).bottom ? b : a), hr = kutu(hedef);
    if (kaplar.some(e => e !== hedef && kutu(e).bottom > hr.top + 2)) return;   // yan yana ya da iç içe tablo kartları: sayfa kayar
    let alt = kutu(kok).bottom - hr.bottom;   // tablonun altında kalan (kart iç boşlukları) + kökün altındaki sayfa boşlukları
    for (let e = kok; e && e !== document.documentElement; e = e.parentElement) { const cs = getComputedStyle(e); alt += parseFloat(cs.marginBottom) || 0; if (e !== kok) alt += (parseFloat(cs.paddingBottom) || 0) + (parseFloat(cs.borderBottomWidth) || 0); }
    const h = Math.floor(window.innerHeight - (hr.top + window.scrollY) - alt - 2);
    if (h < 280) return;
    hedef.classList.add('tk-tam'); hedef.style.setProperty('height', h + 'px', 'important'); hedef.style.setProperty('max-height', 'none', 'important'); hedef.style.setProperty('overflow', 'auto', 'important');
    const t = hedef.querySelector('table');
    if (t && t.tHead) { let ust = 0; for (const tr of t.tHead.rows) { for (const th of tr.cells) { th.style.top = ust + 'px'; if (saydam(getComputedStyle(th).backgroundColor)) { th.style.backgroundColor = '#f8fafc'; th.dataset.tamZemin = '1'; } } ust += kutu(tr).height; } }
    if (t && t.tFoot) for (const tr of t.tFoot.rows) for (const td of tr.cells) if (saydam(getComputedStyle(td).backgroundColor)) { td.style.backgroundColor = '#f1f5f9'; td.dataset.tamZemin = '1'; }
    if (kaydir && kaydir[0] === hedef) { hedef.scrollTop = kaydir[1]; hedef.scrollLeft = kaydir[2]; }
    if (window.scrollY > 0 && document.documentElement.scrollHeight <= window.innerHeight + 2) window.scrollTo(0, 0);
  }
  let z = 0; const planla = () => { clearTimeout(z); z = setTimeout(sigdir, 90); };
  new MutationObserver(planla).observe(kok, { childList: true, subtree: true });
  window.addEventListener('resize', planla); kok.addEventListener('click', e => { if (e.target.closest('#tkSekmeler button, .na-sekme button')) planla(); });
  if (document.fonts && document.fonts.ready) document.fonts.ready.then(planla);
  planla();
})();
