// ============================================================
// GRUP İÇİ ÖZEL FİNANSAL İŞLEMLER — üst sekmeler (Yansıtma / Mahsup / Temlik ekranları)
// Kaynak: GRUP_ICI_OZEL_FINANSAL_ISLEMLER_SEKME_YAPISI.pdf — her 2. alt menü altında üç sekme, Sekme 1 varsayılan:
//   Sekme 1  <Modül> İşlem Ekranı     → talep motoru ekranı (talep-modulu.js; #giIslem)
//   Sekme 2  <Modül> Raporu           → bu modülün işlemleri: gösterge kutucukları, şirket bazında açık işlemler, tür dağılımı, liste
//   Sekme 3  <Modül> Muhasebe Kontrolü → fiş / fatura / mutabakat / kapanış kontrolü; her işlem için kontrol sonucu, olay kaydı
// Rapor ve kontrol ayrı menü DEĞİLDİR; aynı sayfada sekme olarak çalışır (?ust=rapor|kontrol ile doğrudan açılabilir).
// Veri: /IkTalep/TmVeri?kod=… (sekmeye her geçişte yeniden okunur; işlem ekranındaki değişiklikler anında yansır).
// ============================================================
(() => {
  const kok = document.getElementById('tm'), ustBar = document.getElementById('giUst');
  if (!kok || !ustBar) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const para = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const sayi = s => { const n = parseFloat(String(s ?? '').replace(/\./g, '').replace(',', '.')); return isNaN(n) ? 0 : n; };   // "90.000,00" → 90000
  const KOD = kok.dataset.kod;
  const VERI = (window.GI_VERI_URL || '/IkTalep/TmVeri?kod=') + KOD;
  const KISA = { YANSITMA: 'Yansıtma', MAHSUP: 'Mahsup', TEMLIK: 'Temlik' }[KOD] || '';
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const talepRozet = d => rozet(d, d === 'Onaylandı' ? 'ok' : d === 'Reddedildi' || d === 'İptal Edildi' ? 'hata' : /Revizyon/.test(d) ? 'bilgi' : 'bekle');
  const durumRozet = d => d === '—' || !d ? '<span class="text-muted">—</span>' : rozet(d, /Kapandı|Kabul|Muhasebeleşti|Kaydedildi$|Mutabık|Teyit|Tahsil Edildi/.test(d) ? 'ok' : /Red|Fark/.test(d) ? 'hata' : 'bekle');
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const trh = s => (s || '').split('-').reverse().join('.');
  let V = null;

  // ---- Modüle özel yardımcılar ----
  const tutar = t => KOD === 'YANSITMA' ? sayi(t.hesap.toplamSayi) : sayi(t.degerler.tutar);
  const taraflar = t => KOD === 'YANSITMA' ? [t.degerler.yansitan, t.degerler.yansitilan] : KOD === 'MAHSUP' ? [t.degerler.sirketA, t.degerler.sirketB, t.degerler.sirketC].filter(Boolean) : [t.degerler.temlikEden, t.degerler.temlikAlan];
  const acikMi = t => t.onaylandi && !t.iptal && t.durum3 !== 'Kapandı';
  const onayBekliyor = t => /Bekliyor$/.test(t.talepDurumu) && !/Revizyon/.test(t.talepDurumu);

  // ---- Üst sekme geçişi ----
  function ust(u) {
    $$('#giUst button').forEach(b => b.classList.toggle('aktif', b.dataset.u === u));
    ['islem', 'rapor', 'kontrol'].forEach(p => { const el = document.getElementById('gi' + p.charAt(0).toUpperCase() + p.slice(1)); if (el) el.classList.toggle('aktif', p === u); });
    try { const url = new URL(location.href); if (u === 'islem') url.searchParams.delete('ust'); else url.searchParams.set('ust', u); history.replaceState(null, '', url.toString()); } catch (e) { }
    if (u !== 'islem') yukle().then(() => u === 'rapor' ? cizRapor() : cizKontrol()).catch(e => hata(e));
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
  $$('#giUst button').forEach(b => b.addEventListener('click', () => ust(b.dataset.u)));
  function hata(e) { const s = $('#naSonuc'); if (!s) return; s.className = 'alert alert-danger py-2 px-3'; s.style.display = 'block'; s.textContent = 'Veri yüklenemedi: ' + e.message; }
  async function yukle() { const r = await fetch(VERI); V = await r.json(); sayaclar(); }
  function sayaclar() {
    const n1 = $('#giRaporN'), n2 = $('#giKontrolN');
    if (n1) n1.textContent = V.talepler.length;
    if (n2) { const k = V.talepler.filter(t => t.onaylandi && !t.iptal).map(kontrolSonucu).filter(s => s.cls === 'hata').length; n2.textContent = k ? k + ' sorun' : 'tamam'; }
  }
  // Detayı işlem ekranında aç (talep-modulu.js › window.TM_DETAY_AC)
  function detayaGit(no) { ust('islem'); if (window.TM_DETAY_AC) window.TM_DETAY_AC(no); }
  const satirTikla = sel => $$(sel + ' tr.tik').forEach(r => r.addEventListener('click', () => detayaGit(r.dataset.no)));
  const kpiKutu = l => l.map(k => `<div style="--k:${k.renk}"><div class="l">${esc(k.baslik)}</div><div class="v">${esc(k.deger)}</div><div class="s">${esc(k.alt)}</div></div>`).join('');

  // ============================================================
  // SEKME 2 — <Modül> Raporu
  // ============================================================
  // Takip sütunları modül tanımından gelir (modul.takipSutunlar: "d.alan" değer, "h.alan" sistem hesabı, "e.alan" kaynak sistem, "onay.Adım")
  const deger = (t, k) => k === 'no' ? t.no : k === 'tur' ? t.turAdi : k === 'kullanan' ? t.kullanan.adSoyad : k.startsWith('d.') ? (t.degerler[k.slice(2)] ?? '') : k.startsWith('h.') ? (t.hesap[k.slice(2)] ?? '') : k.startsWith('e.') ? (t.ek[k.slice(2)] ?? '') : k.startsWith('onay.') ? (t.onay[k.slice(5)] ?? '') : (t[k] ?? '');
  const sayisalMi = (b, k) => /tutar|toplam|bakiye|alacak|bedel|fark|tahsil|TL\)|%/i.test(b) || /tutar|toplam|bakiye|fark|tahsil|bedel|alacak/i.test(k);
  const hucre = (t, s) => {
    const k = s.anahtar, v = deger(t, k);
    if (k === 'no') return `<td><b>${esc(v)}</b></td>`;
    if (k === 'talepDurumu') return `<td>${talepRozet(v)}</td>`;
    if (k === 'durum2' || k === 'durum3') return `<td>${durumRozet(v)}</td>`;
    if (/^d\.(tarih|vade)$/.test(k)) return `<td>${bos(trh(v))}</td>`;
    if (sayisalMi(s.baslik, k)) return `<td class="s">${v === '' ? '<span class="text-muted">—</span>' : esc(/,|\./.test(v) && !/^\d+$/.test(v) ? v : para(v))}</td>`;
    return `<td>${bos(v)}</td>`;
  };
  function cizRapor() {
    const L = V.talepler, sut = V.modul.takipSutunlar || [];
    $('#giRaporKpi').innerHTML = kpiKutu(V.kpiler || []);
    // tür filtresi (bir kez)
    const turSec = $('#giRaporTur'); if (turSec.options.length <= 1) turSec.innerHTML += V.modul.turler.map(x => `<option value="${esc(x.kod)}">${esc(x.ad)}</option>`).join('');
    $('#giRaporTablo thead tr').innerHTML = sut.map(s => `<th class="${sayisalMi(s.baslik, s.anahtar) ? 's' : ''}">${esc(s.baslik)}</th>`).join('');
    const ciz = () => {
      const ara = ($('#giRaporAra').value || '').toLocaleLowerCase('tr-TR'), tf = turSec.value, df = $('#giRaporDurum').value;
      const l = L.filter(t => (!tf || t.turKod === tf) && (!ara || [t.no, t.ozet, t.kullanan.adSoyad, ...taraflar(t), ...Object.values(t.ek)].join(' ').toLocaleLowerCase('tr-TR').includes(ara))
        && (!df || (df === 'onay' && onayBekliyor(t)) || (df === 'acik' && acikMi(t)) || (df === 'kapandi' && t.durum3 === 'Kapandı') || (df === 'red' && (t.talepDurumu === 'Reddedildi' || t.iptal))));
      $('#giRaporTablo tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${esc(t.no)}">${sut.map(s => hucre(t, s)).join('')}</tr>`).join('') || `<tr><td colspan="${sut.length}" class="na-bos">Kayıt yok.</td></tr>`;
      $('#giRaporSayi').textContent = `${l.length} / ${L.length} işlem · açık ${para(l.filter(acikMi).reduce((s, t) => s + tutar(t), 0))} TL`;
      satirTikla('#giRaporTablo');
    };
    ['#giRaporAra', '#giRaporTur', '#giRaporDurum'].forEach(s => { const el = $(s); el.oninput = ciz; });
    ciz();
    // şirket bazında açık işlemler
    const sirk = {}; L.filter(acikMi).forEach(t => taraflar(t).forEach(s => { sirk[s] = sirk[s] || { n: 0, tl: 0 }; sirk[s].n++; sirk[s].tl += tutar(t); }));
    $('#giRaporSirket').innerHTML = Object.keys(sirk).sort((a, b) => sirk[b].tl - sirk[a].tl).map(s => `<div><b>${esc(s)}</b><div class="s"><span>${sirk[s].n} açık işlem</span><span>${para(sirk[s].tl)} TL</span></div></div>`).join('') || '<div class="na-bos">Açık işlem yok.</div>';
    // türe göre dağılım
    const turler = V.modul.turler.map(x => { const l = L.filter(t => t.turKod === x.kod); return { ad: x.ad, n: l.length, tl: l.reduce((s, t) => s + tutar(t), 0) }; });
    const mx = Math.max(1, ...turler.map(x => x.n));
    $('#giRaporTurler').innerHTML = turler.map(x => `<div class="r"><span>${esc(x.ad)} <span class="text-muted">· ${para(x.tl)} TL</span></span><span class="v">${x.n}</span><span class="b"><i style="width:${Math.round(x.n / mx * 100)}%"></i></span></div>`).join('');
    // süreç özeti
    $('#giRaporSurec').innerHTML = `<div class="mb-1"><b>Akış:</b> ${esc(V.modul.akisOzeti)}</div><ul class="mb-0 ps-3">${(V.modul.kurallar || []).slice(0, 3).map(k => `<li>${esc(k)}</li>`).join('')}</ul>`;
  }

  // ============================================================
  // SEKME 3 — <Modül> Muhasebe Kontrolü
  // ============================================================
  // Her işlem için kaynak sistemden gelen fiş / fatura / sözleşme / mutabakat / kapanış alanları kontrol edilir; sonuç ok / bekle / hata.
  const S = (cls, metin) => ({ cls, metin });
  const KONTROL = {
    YANSITMA: {
      sutunlar: ['Talep No', 'Yansıtan → Yansıtılan', 'Dönem', 'Toplam (TL)', 'Fatura No · Tarih', 'Karşı Yanıt', 'Fiş (Yansıtan · gelir)', 'Fiş (Yansıtılan · gider)', 'Mutabakat', 'Kapanış', 'Kontrol Sonucu'],
      satir: t => [`<b>${esc(t.no)}</b>`, `${esc(t.degerler.yansitan)} → ${esc(t.degerler.yansitilan)}`, esc(t.degerler.donem), `<span class="s">${esc(t.hesap.toplam || para(tutar(t)))}</span>`, t.ek.faturaNo ? `${esc(t.ek.faturaNo)} <span class="text-muted">· ${esc(t.ek.faturaTarihi)}</span>` : bos(''), t.ek.karsiYanit ? rozet(t.ek.karsiYanit, t.ek.karsiYanit === 'Kabul' ? 'ok' : 'hata') + (t.ek.redNedeni ? ` <span class="text-muted">${esc(t.ek.redNedeni)}</span>` : '') : bos(''), bos(t.ek.fisYansitan), bos(t.ek.fisYansitilan), bos(t.ek.mutabakatTarihi), bos(t.ek.kapanisTarihi)],
      sonuc: t => {
        if (t.ek.kapanisTarihi) return S('ok', 'Tamam · kapandı');
        if (!t.ek.faturaNo) return S('bekle', 'Fatura bekleniyor (e-fatura)');
        if (t.ek.karsiYanit === 'Red') return S('hata', 'Karşı taraf reddetti — revize gerekli');
        if (t.ek.karsiYanit !== 'Kabul') return S('bekle', 'Karşı kabul bekleniyor');
        const a = !!t.ek.fisYansitan, b = !!t.ek.fisYansitilan;
        if (!a && !b) return S('hata', 'Fiş yok (iki taraf)');
        if (!a) return S('hata', 'Eksik fiş: yansıtan (gelir) tarafı');
        if (!b) return S('hata', 'Eksik fiş: yansıtılan (gider) tarafı');
        if (!t.ek.mutabakatTarihi) return S('bekle', 'Mutabakat bekleniyor');
        return S('bilgi', 'Mutabık · kapanış bekleniyor');
      },
      adimlar: ['E-fatura kesildi mi, fatura no ve tarih dolu mu?', 'Karşı şirket ticari fatura yanıtı verdi mi (kabul / red)?', 'Yansıtan tarafta gelir, yansıtılan tarafta gider fişi oluştu mu?', 'İki fiş tutarı fatura toplamıyla eşit mi; cari mutabakat sağlandı mı?', 'Mutabakat sonrası kapanış kaydı atıldı mı?'],
      olayAlan: { INVOICE_NO: 'Fatura kesildi', INVOICE_RESPONSE: 'Karşı taraf yanıtı', JOURNAL_A: 'Fiş (yansıtan)', JOURNAL_B: 'Fiş (yansıtılan)', RECONCILED: 'Mutabakat', STATUS: 'Durum' }
    },
    MAHSUP: {
      sutunlar: ['Talep No', 'Şirket A ⇄ B (C)', 'Tarih', 'Tutar (TL)', 'Fiş A', 'Fiş B', 'Mutabakat Sonucu', 'Fark (TL)', 'Kapanış', 'Kontrol Sonucu'],
      satir: t => [`<b>${esc(t.no)}</b>`, taraflar(t).map(esc).join(' ⇄ '), esc(trh(t.degerler.tarih)), `<span class="s">${para(t.degerler.tutar)}</span>`, bos(t.ek.fisA), bos(t.ek.fisB), t.ek.mutabakatSonuc ? rozet(t.ek.mutabakatSonuc, t.ek.mutabakatSonuc === 'Mutabık' ? 'ok' : 'hata') + ` <span class="text-muted">${esc(t.ek.mutabakatTarihi || '')}</span>` : bos(''), bos(t.ek.mutabakatFark), bos(t.ek.kapanisTarihi)],
      sonuc: t => {
        if (t.ek.kapanisTarihi) return S('ok', 'Tamam · kapandı');
        const a = !!t.ek.fisA, b = !!t.ek.fisB;
        if (!a && !b) return S('hata', 'Fiş yok (A ve B)');
        if (!a) return S('hata', 'Eksik fiş: Şirket A');
        if (!b) return S('hata', 'Eksik fiş: Şirket B');
        if (t.ek.mutabakatSonuc === 'Fark Var') return S('hata', `Mutabakat farkı ${t.ek.mutabakatFark || ''} TL — düzeltme kaydı gerekli`);
        if (!t.ek.mutabakatSonuc) return S('bekle', 'Mutabakat bekleniyor');
        return S('bilgi', 'Mutabık · kapanış bekleniyor');
      },
      adimlar: ['Mahsup tutarı ERP\'deki karşılıklı bakiyelerin küçüğünü aşıyor mu?', 'A tarafında ve B tarafında mahsup fişi oluştu mu?', 'İki fişin tutarı ve tarihi birbirini tutuyor mu?', 'Cari mutabakat sonucu mutabık mı, fark varsa düzeltme kaydı atıldı mı?', 'Kapanış sonrası ERP bakiyeleri güncellendi mi?'],
      olayAlan: { JOURNAL_A: 'Fiş (A)', JOURNAL_B: 'Fiş (B)', RECONCILIATION: 'Mutabakat', STATUS: 'Durum' }
    },
    TEMLIK: {
      sutunlar: ['Talep No', 'Temlik Eden → Alan', 'Borçlu', 'Temlik (TL)', 'Sözleşme No · Tarih', 'Borçluya Bildirim', 'Borçlu Teyidi', 'Tahsil Edilen (TL)', 'Kapanış', 'Kontrol Sonucu'],
      satir: t => [`<b>${esc(t.no)}</b>`, `${esc(t.degerler.temlikEden)} → ${esc(t.degerler.temlikAlan)}`, esc(t.degerler.borclu), `<span class="s">${para(t.degerler.tutar)}</span>`, t.ek.noterNo ? `${esc(t.ek.noterNo)} <span class="text-muted">· ${esc(t.ek.sozlesmeTarihi)}</span>` : bos(''), t.ek.bildirimTarihi ? `${esc(t.ek.bildirimYontemi)} <span class="text-muted">· ${esc(t.ek.bildirimTarihi)}</span>` : bos(''), bos(t.ek.teyitTarihi), t.ek.tahsilEdilen ? `${esc(t.ek.tahsilEdilen)} <span class="text-muted">/ ${para(t.degerler.tutar)}</span>` : bos(''), bos(t.ek.kapanisTarihi)],
      sonuc: t => {
        if (t.ek.kapanisTarihi) return S('ok', 'Tamam · kapandı');
        if (!t.ek.sozlesmeTarihi) return S('bekle', 'Sözleşme bekleniyor (Hukuk)');
        if (!t.ek.bildirimTarihi) return S('hata', 'Borçluya bildirim yok (TBK m.186 riski)');
        const th = sayi(t.ek.tahsilEdilen), tt = sayi(t.degerler.tutar);
        if (th >= tt && tt > 0) return S('bilgi', 'Tahsilat tamam · kapanış bekleniyor');
        if (th > 0) return S('bekle', `Kısmi tahsilat · kalan ${para(tt - th)} TL`);
        return S('bekle', t.ek.teyitTarihi ? 'Tahsilat bekleniyor' : 'Borçlu teyidi ve tahsilat bekleniyor');
      },
      adimlar: ['Temlik sözleşmesi imzalandı mı (noter / e-imza no)?', 'Borçluya ihbarname gönderildi mi (TBK m.186)?', 'Borçlu ödemeyi temlik alana yapacağını teyit etti mi?', 'Tahsilat temlik tutarıyla eşleşti mi, kısmi tahsilatlarda kalan tutar doğru mu?', 'Temlik eden ve alan tarafta cari / alacak kayıtları kapatıldı mı?'],
      olayAlan: { CONTRACT: 'Sözleşme', NOTIFIED: 'Borçluya bildirim', DEBTOR_CONFIRMED: 'Borçlu teyidi', COLLECTED: 'Tahsilat', STATUS: 'Durum' }
    }
  };
  const K = KONTROL[KOD];
  function kontrolSonucu(t) {
    if (t.iptal) return S('gri', 'İptal edildi');
    if (t.talepDurumu === 'Reddedildi') return S('gri', 'Reddedildi');
    if (!t.onaylandi) return S('gri', 'Onay sürecinde · ' + t.talepDurumu);
    return K ? K.sonuc(t) : S('gri', '—');
  }
  function cizKontrol() {
    if (!K) return;
    const L = V.talepler;
    $('#giKontrolTablo thead tr').innerHTML = K.sutunlar.map((s, i) => `<th class="${/\(TL\)/.test(s) ? 's' : ''}">${esc(s)}</th>`).join('');
    const ciz = () => {
      const ara = ($('#giKontrolAra').value || '').toLocaleLowerCase('tr-TR'), sf = $('#giKontrolSonuc').value, onayDa = $('#giKontrolOnay').checked, iptalDa = $('#giKontrolIptal').checked;
      const l = L.map(t => ({ t, s: kontrolSonucu(t) })).filter(({ t, s }) => (t.onaylandi || (onayDa && !t.iptal && t.talepDurumu !== 'Reddedildi') || (iptalDa && (t.iptal || t.talepDurumu === 'Reddedildi')))
        && (!sf || s.cls === sf) && (!ara || [t.no, ...taraflar(t), ...Object.values(t.ek), t.degerler.borclu || ''].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
      $('#giKontrolTablo tbody').innerHTML = l.map(({ t, s }) => `<tr class="tik" data-no="${esc(t.no)}">${K.satir(t).map(h => `<td>${h}</td>`).join('')}<td>${rozet(s.metin, s.cls === 'gri' ? 'bilgi' : s.cls)}</td></tr>`).join('') || `<tr><td colspan="${K.sutunlar.length}" class="na-bos">Kayıt yok.</td></tr>`;
      $('#giKontrolSayi').textContent = `${l.length} işlem`;
      satirTikla('#giKontrolTablo');
    };
    ['#giKontrolAra', '#giKontrolSonuc', '#giKontrolOnay', '#giKontrolIptal'].forEach(s => { const el = $(s); el.oninput = ciz; el.onchange = ciz; });
    ciz();
    // özet kutucukları (onaylı işlemler)
    const O = L.filter(t => t.onaylandi && !t.iptal).map(t => ({ t, s: K.sonuc(t) }));
    const say = c => O.filter(x => x.s.cls === c), tl = l => para(l.reduce((s, x) => s + tutar(x.t), 0)) + ' TL';
    $('#giKontrolKpi').innerHTML = kpiKutu([
      { renk: '#16a34a', baslik: 'Kontrol Tamam', deger: `${say('ok').length} İşlem`, alt: tl(say('ok')) },
      { renk: '#dc2626', baslik: 'Eksik / Hatalı', deger: `${say('hata').length} İşlem`, alt: say('hata').length ? tl(say('hata')) : 'eksik fiş, red, fark, bildirim' },
      { renk: '#f59e0b', baslik: 'Bekleyen Adım', deger: `${say('bekle').length} İşlem`, alt: tl(say('bekle')) },
      { renk: '#0ea5e9', baslik: 'Kapanışa Hazır', deger: `${say('bilgi').length} İşlem`, alt: 'mutabık / tahsilat tamam' },
      { renk: '#64748b', baslik: 'Onay Sürecinde', deger: `${L.filter(t => !t.onaylandi && !t.iptal && t.talepDurumu !== 'Reddedildi').length} Talep`, alt: 'kontrol kapsamı dışında' },
      { renk: V.modul.renk || '#0d9488', baslik: 'Açık Toplam', deger: para(L.filter(acikMi).reduce((s, t) => s + tutar(t), 0)) + ' TL', alt: `${L.filter(acikMi).length} kapanmamış işlem` }
    ]);
    $$('#giKontrolKpi > div').forEach((d, i) => d.addEventListener('click', () => { $('#giKontrolSonuc').value = ['ok', 'hata', 'bekle', 'bilgi', '', ''][i]; if (i === 4) $('#giKontrolOnay').checked = true; ciz(); }));
    // olay kaydı: audit satırlarından muhasebe / kaynak sistem olayları
    // (iş akışı onay satırları — kaynak "Workflow" — muhasebe olayı değildir; dışarıda tutulur)
    const olaylar = [];
    const anahtar = a => (a.tarih || '').split('.').reverse().join('') + ' ' + (a.saat || '');   // dd.MM.yyyy → yyyyMMdd (sıralama)
    L.forEach(t => (t.audit || []).forEach(a => { const alan = a.alan || '', kaynak = a.kaynakSistem || ''; if (K.olayAlan[alan] && kaynak !== 'Workflow') olaylar.push({ k: anahtar(a), tarih: `${a.tarih || ''} ${a.saat || ''}`.trim(), no: t.no, olay: K.olayAlan[alan], deger: a.yeni || '', kaynak }); }));
    olaylar.sort((x, y) => y.k.localeCompare(x.k));
    $('#giKontrolOlay tbody').innerHTML = olaylar.slice(0, 30).map(o => `<tr class="tik" data-no="${esc(o.no)}"><td class="gi-olay">${esc(o.tarih)}</td><td><b>${esc(o.no)}</b></td><td>${esc(o.olay)}</td><td class="gi-olay"><code>${esc(o.deger)}</code></td><td class="gi-olay">${esc(o.kaynak)}</td></tr>`).join('') || '<tr><td colspan="5" class="na-bos">Kaynak sistem olayı yok.</td></tr>';
    satirTikla('#giKontrolOlay');
    $('#giKontrolAdimlar').innerHTML = K.adimlar.map(a => `<li>${esc(a)}</li>`).join('');
    $('#giKontrolKurallar').innerHTML = (V.modul.kurallar || []).map(k => `<li>${esc(k)}</li>`).join('');
  }

  // ---- Başlangıç: sekme sayaçları ve ?ust= ile gelen sekme ----
  const ilk = ustBar.dataset.ust || 'islem';
  yukle().then(() => { if (ilk === 'rapor') cizRapor(); else if (ilk === 'kontrol') cizKontrol(); }).catch(e => hata(e));
})();
