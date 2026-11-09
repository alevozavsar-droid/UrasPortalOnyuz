// ============================================================
// KREDİ RAPORU — ekran betiği (Views/KrediYapilandirma/Index.cshtml; veri /KrediYapilandirma/Veri; kaynak Data/KrediYapilandirmaOrnek.cs)
// Sekmeler (iki küme): Krediler (öncesi ↔ sonrası) · Yapılandırma Öncesi · Yapılandırma Sonrası · Taksit Takvimi · Aylık Nakit Yükü | Rehin · İpotek Takibi · Teminat Çekleri · Belgeler · Sözleşme / GKS.
// Kredi Ekle / Düzenle modalı: kimlik, yapılandırma öncesi, sonrası (yeni taksit boşsa anüite ile hesaplanır; canlı önizleme sunucu formülüyle aynı), peşinat, teminat satırları (tesis / durum / fek), not, nakit akışa aktarım.
// Kaydet → sunucu taksitleri Kasa Nakit Akışı'na (Havale / EFT) ve Planlı Düzenli Ödemeler'e yazar; paket (veri) geri döner, tüm sekmeler yeniden çizilir.
// 9 Kas 2026 ("sözleşmelerin PDF'ini taratıp yükleyelim, GKS'leri yükleyelim, eski kredilerin rehin ve ipotek takibi, teminata verilen çekler"):
//   Rehin · İpotek Takibi = kredi kartındaki teminatlar + eski krediler için bağımsız kayıtlar (TeminatKaydet / TeminatSil; durum Aktif → Fek Bekliyor → Fek Edildi, Paraya Çevrildi);
//   Teminat Çekleri = bankaya teminat verilen müşteri çek / senetleri (CekKaydet / CekSil; Teminatta → İade Alındı / Tahsil Edildi / Karşılıksız);
//   Belgeler = taranmış sözleşme / protokol / GKS / ipotek / rehin / teminat mektubu / çek / ödeme planı / ekspertiz / fek yazısı, PDF-JPG-PNG (BelgeYukle çok parçalı; Belge/{id} aç, ?indir=1 indir; BelgeSil);
//   belge krediye, rehin kaydına ya da teminat çekine bağlanabilir. Durum filtresi sekmeye göre: kredi sekmelerinde yapılandırma durumu, Rehin'de teminat durumu, Çekler'de çek durumu, Belgeler'de belge türü.
// Yazım: liste değerleri ve etiketler Title Case; tutarlar ₺ (çekte kendi para birimi); tablo hizası tablo-hizala.js; Excel ortak TabloExcel.
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('ky'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const n0 = v => Number(v) || 0;
  const para = v => n0(v).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const tl = v => para(v) + ' ₺';
  const kisa = v => n0(v).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const yuzde = v => n0(v).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' %';
  const tarih = s => { if (!s) return '—'; const p = String(s).slice(0, 10).split('-'); return p.length === 3 ? `${p[2]}.${p[1]}.${p[0]}` : s; };
  const ayAd = ym => { const [y, m] = ym.split('-'); return ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'][+m - 1] + ' ' + y; };
  const bos = '<span class="text-muted">—</span>';
  const rz = (m, t) => `<span class="ky-rz ${t}">${esc(m)}</span>`;
  const durumRz = d => rz(d || '—', d === 'Yapılandırıldı' || d === 'Protokol İmzalandı' ? 'ok' : d === 'Tamamlandı' ? 'mavi' : d === 'İptal' ? 'hata' : 'bekle');
  const oncekiRz = d => rz(d || '—', d === 'Düzenli' ? 'ok' : d === 'Yasal Takipte' ? 'hata' : 'bekle');
  const kasaRz = (k, aktar) => !k ? (aktar ? '<span class="ky-rz gri">Aktarılmadı</span>' : '<span class="ky-rz gri">Aktarım Kapalı</span>') : k.durum === 'ÖDENDİ' ? rz('Ödendi' + (k.odenme ? ' · ' + tarih(k.odenme) : ''), 'ok') : rz(k.durum, k.durum === 'Gecikti' ? 'hata' : 'mavi');
  const fark = (v, tersOlumlu) => { const f = n0(v); if (Math.abs(f) < 0.005) return '<span class="text-muted">0,00 ₺</span>'; const kotu = tersOlumlu ? f < 0 : f > 0; return `<span class="ky-fark ${kotu ? 'art' : 'az'}">${f > 0 ? '+' : ''}${tl(f)}</span>`; };
  // rehin · ipotek / çek / belge yardımcıları (9 Kas 2026)
  const sembol = pb => ({ TRY: '₺', TL: '₺', USD: '$', EUR: '€', GBP: '£' })[pb] || pb || '₺';
  const tutarPb = (v, pb) => para(v) + ' ' + sembol(pb);
  const teminatDurumRz = d => rz(d || 'Aktif', d === 'Fek Edildi' ? 'ok' : d === 'Fek Bekliyor' ? 'bekle' : d === 'Paraya Çevrildi' ? 'hata' : 'mavi');
  const cekDurumRz = d => rz(d || 'Teminatta', d === 'İade Alındı' ? 'ok' : d === 'Tahsil Edildi' ? 'gri' : d === 'Karşılıksız' ? 'hata' : 'mavi');
  const belgeRenk = t => /GKS/.test(t) ? 'mor' : /İpotek|Rehin|Fek/.test(t) ? 'bekle' : /Çek|Mektubu/.test(t) ? 'ok' : /Sözleşme|Protokol|Ödeme Planı/.test(t) ? 'mavi' : 'gri';
  const aktifTeminat = d => d !== 'Fek Edildi' && d !== 'Paraya Çevrildi';
  const TEMINAT_BELGE = ['İpotek Belgesi', 'Rehin Sözleşmesi', 'Fek Yazısı', 'Ekspertiz Raporu', 'Teminat Mektubu'];
  const boyutH = b => n0(b) >= 1048576 ? (n0(b) / 1048576).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + ' MB' : Math.max(1, Math.round(n0(b) / 1024)) + ' KB';
  const dosyaH = b => `<a class="ky-dosya ${b.icerikTuru === 'application/pdf' ? 'pdf' : 'img'}" href="${esc(b.link)}" target="_blank" rel="noopener" title="Tarayıcıda aç"><i class="fas ${b.icerikTuru === 'application/pdf' ? 'fa-file-pdf' : 'fa-file-image'}"></i>${esc(b.dosyaAdi)}</a>${b.var === false ? ' ' + rz('Dosya Yok', 'hata') : ''}`;
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 10000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }).then(r => r.json()).catch(() => ({ success: false, message: 'Ağ hatası.' }));

  let V = null, F = { sirket: '', banka: '', durum: '', ara: '' }, takvimKredi = '', belgeKredi = '', belgeBag = null;   // belgeBag: { tip: 'cek' | 'teminat', id, ad } — Belgeler sekmesinde tek kayda bağlı belgeler
  const KREDI_SEKME = ['krediler', 'oncesi', 'sonrasi', 'takvim', 'aylik'];
  const aktifSekme = () => ($('#kySekmeler button.aktif') || {}).dataset?.p || 'krediler';
  const krediSec = id => V.krediler.find(k => k.id === +id);

  // ---------- yükleme
  async function yukle() { let j; try { j = await fetch('/KrediYapilandirma/Veri').then(r => r.json()); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; } veri(j); }
  function veri(j) {
    if (!j || !j.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = j; ['teminatlar', 'cekler', 'belgeler', 'teminatDurumlari', 'cekDurumlari', 'belgeTurleri'].forEach(k => { if (!Array.isArray(V[k])) V[k] = []; });
    filtreDoldur(); $('#kyKalici').style.display = V.kalici ? '' : 'none'; ciz();
  }
  function secenekler(sel, degerler, mevcut) { const v = mevcut ?? sel.value; sel.innerHTML = '<option value="">Tümü</option>' + degerler.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); sel.value = degerler.includes(v) ? v : ''; }
  const sirala = l => [...new Set(l.filter(Boolean))].sort((a, b) => a.localeCompare(b, 'tr'));
  function filtreDoldur() {
    const p = aktifSekme();
    secenekler($('#kyS'), sirala([...V.krediler.map(k => k.sirket), ...V.teminatlar.map(t => t.sirket), ...V.cekler.map(c => c.sirket), ...V.belgeler.map(b => b.sirket)]), F.sirket); F.sirket = $('#kyS').value;
    secenekler($('#kyB'), sirala([...V.krediler.map(k => k.banka), ...V.teminatlar.map(t => t.banka), ...V.cekler.map(c => c.banka), ...V.belgeler.map(b => b.banka)]), F.banka); F.banka = $('#kyB').value;
    // durum filtresi sekmeye göre: kredi durumu | teminat durumu | çek durumu | belge türü
    $('#kyDEtiket').textContent = p === 'belgeler' ? 'Belge Türü' : p === 'rehin' ? 'Teminat Durumu' : p === 'cekler' ? 'Çek Durumu' : 'Durum';
    secenekler($('#kyD'), p === 'rehin' ? V.teminatDurumlari : p === 'cekler' ? V.cekDurumlari : p === 'belgeler' ? V.belgeTurleri : V.durumlar, F.durum); F.durum = $('#kyD').value;
    const krediSecenekleri = V.krediler.map(k => `<option value="${k.id}">${esc(k.no)} · ${esc(k.banka)} · ${esc(k.krediNo || k.krediTuru)}</option>`).join('');
    const tk = $('#kyTakvimKredi'); if (tk) { tk.innerHTML = '<option value="">Tüm Krediler</option>' + krediSecenekleri; tk.value = V.krediler.some(k => String(k.id) === takvimKredi) ? takvimKredi : ''; takvimKredi = tk.value; }
    const bk = $('#kyBelgeKredi'); if (bk) { bk.innerHTML = '<option value="">Tüm Krediler</option><option value="yok">Krediye Bağlı Olmayan</option>' + krediSecenekleri; bk.value = belgeKredi === 'yok' || V.krediler.some(k => String(k.id) === belgeKredi) ? belgeKredi : ''; belgeKredi = bk.value; }
  }
  const aramaMetni = k => [k.no, k.sirket, k.banka, k.sube, k.krediNo, k.krediTuru, k.protokolNo, k.durum, k.oncekiDurum, k.not, ...(k.teminatlar || []).map(t => [t.tur, t.aciklama, t.tapuBilgisi, t.malik, t.not].join(' '))].join(' ').toLocaleLowerCase('tr-TR');
  const kucuk = s => String(s ?? '').toLocaleLowerCase('tr-TR');
  function liste(durumUygula = true, araUygula = true) { const a = kucuk(F.ara.trim()); return V.krediler.filter(k => (!F.sirket || k.sirket === F.sirket) && (!F.banka || k.banka === F.banka) && (!durumUygula || !F.durum || k.durum === F.durum) && (!araUygula || !a || aramaMetni(k).includes(a))); }
  const top = (l, f) => l.reduce((s, k) => s + n0(typeof f === 'function' ? f(k) : k[f]), 0);
  /** rehin · ipotek satırları: kredi kartındaki teminatlar (kaynak 'kredi') + bağımsız eski kredi kayıtları (kaynak 'eski'); fek bekleyenler üstte */
  function rehinListe() {
    const a = kucuk(F.ara.trim()); const rows = [];
    V.krediler.forEach(k => (k.teminatlar || []).forEach(t => rows.push({ kaynak: 'kredi', k, t, sirket: k.sirket, banka: k.banka, durum: t.durum || 'Aktif', ara: [k.no, k.sirket, k.banka, k.krediNo, k.protokolNo, t.tur, t.aciklama, t.tapuBilgisi, t.derece, t.malik, t.not].join(' ') })));
    V.teminatlar.forEach(t => rows.push({ kaynak: 'eski', k: t.krediId ? krediSec(t.krediId) : null, t, sirket: t.sirket, banka: t.banka, durum: t.durum || 'Aktif', ara: [t.sirket, t.banka, t.krediRef, t.krediNo, t.tur, t.aciklama, t.tapuBilgisi, t.derece, t.malik, t.not].join(' ') }));
    const sira = d => ({ 'Fek Bekliyor': 0, 'Aktif': 1, 'Paraya Çevrildi': 2, 'Fek Edildi': 3 })[d] ?? 1;
    return rows.filter(r => (!F.sirket || r.sirket === F.sirket) && (!F.banka || r.banka === F.banka) && (!F.durum || r.durum === F.durum) && (!a || kucuk(r.ara).includes(a))).sort((x, y) => sira(x.durum) - sira(y.durum) || x.banka.localeCompare(y.banka, 'tr'));
  }
  function cekListe() { const a = kucuk(F.ara.trim()); return V.cekler.filter(c => (!F.sirket || c.sirket === F.sirket) && (!F.banka || c.banka === F.banka) && (!F.durum || c.durum === F.durum) && (!a || kucuk([c.sirket, c.banka, c.krediNo, c.krediRef, c.belge, c.cekNo, c.kesideci, c.cekBankasi, c.not].join(' ')).includes(a))); }
  function belgeListe() {
    const a = kucuk(F.ara.trim());
    return V.belgeler.filter(b => (!F.sirket || b.sirket === F.sirket) && (!F.banka || b.banka === F.banka) && (!F.durum || b.tur === F.durum)
      && (!belgeKredi || (belgeKredi === 'yok' ? !b.krediId : String(b.krediId) === belgeKredi))
      && (!belgeBag || (belgeBag.tip === 'cek' ? b.cekId === belgeBag.id : b.teminatId === belgeBag.id))
      && (!a || kucuk([b.tur, b.baslik, b.krediNo, b.sirket, b.banka, b.dosyaAdi, b.aciklama, b.yukleyen].join(' ')).includes(a)));
  }
  const pbTop = L => { const m = new Map(); L.forEach(c => m.set(c.pb || 'TRY', (m.get(c.pb || 'TRY') || 0) + n0(c.tutar))); return [...m].map(([pb, v]) => tutarPb(v, pb)).join(' · ') || tl(0); };

  // ---------- çizim
  function ciz() {
    if (!V) return;
    const p = aktifSekme(), krediSek = KREDI_SEKME.includes(p); const L = liste(krediSek, krediSek);
    kpi(L);
    if (krediSek) { $('#kyKapsam').textContent = `${L.length} / ${V.krediler.length} kredi · ${[...new Set(L.map(k => k.sirket))].length} şirket · ${[...new Set(L.map(k => k.banka))].length} banka · ${V.bugun ? tarih(V.bugun) : ''}`; (CIZ[p] || cizKrediler)(L); }
    else (CIZ[p] || cizRehin)();   // teminat / belge sekmeleri kapsam metnini kendileri yazar
  }
  function kpi(L) {
    const aktif = L.filter(k => k.durum !== 'İptal'), onceTaksit = top(aktif, 'taksit'), sonraTaksit = top(aktif, 'uygulananTaksit');
    const geciken = top(aktif, 'gecikenTutar') + top(aktif, 'gecikmeFaizi'), kasaSatir = top(L, k => k.kasaOzet.satir), kasaOdendi = top(L, k => k.kasaOzet.odendi);
    const uy = x => (!F.sirket || x.sirket === F.sirket) && (!F.banka || x.banka === F.banka);
    const bagimsiz = V.teminatlar.filter(uy), bagAktif = bagimsiz.filter(t => aktifTeminat(t.durum));
    const fekBekleyen = aktif.reduce((s, k) => s + (k.teminatlar || []).filter(t => t.durum === 'Fek Bekliyor').length, 0) + bagimsiz.filter(t => t.durum === 'Fek Bekliyor').length;
    const cekler = V.cekler.filter(uy).filter(c => c.durum === 'Teminatta'), cekTl = cekler.filter(c => (c.pb || 'TRY') === 'TRY').reduce((s, c) => s + n0(c.tutar), 0), belgeN = V.belgeler.filter(uy).length;
    const K = [
      ['#1e40af', 'Yapılandırılan Kredi', `${aktif.length}`, `${[...new Set(aktif.map(k => k.banka))].length} banka · ${[...new Set(aktif.map(k => k.sirket))].length} şirket · ${L.filter(k => k.durum === 'İptal').length} iptal`],
      ['#b45309', 'Öncesi Toplam Borç', kisa(top(aktif, 'oncesiToplamBorc')), `kalan anapara ${kisa(top(aktif, 'kalanAnapara'))} · geciken + faiz + masraf ${kisa(geciken + top(aktif, 'masraf'))}`],
      ['#0f766e', 'Sonrası Toplam Geri Ödeme', kisa(top(aktif, 'toplamGeriOdeme')), `peşinat ${kisa(top(aktif, 'pesinat'))} · yeni anapara ${kisa(top(aktif, 'yeniAnapara'))} · ${top(aktif, 'taksitSayisi')} taksit`],
      ['#7c3aed', 'Aylık Taksit Yükü', `${kisa(onceTaksit)} → ${kisa(sonraTaksit)}`, `fark ${sonraTaksit - onceTaksit >= 0 ? '+' : ''}${kisa(sonraTaksit - onceTaksit)} / ay`],
      ['#dc2626', 'Toplam Faiz (Sonrası)', kisa(top(aktif, 'toplamFaiz')), `BSMV dahil · anapara ${kisa(top(aktif, 'toplamAnapara'))}`],
      ['#9d174d', 'Geciken Taksit (Öncesi)', `${top(aktif, 'gecikenTaksit')} adet`, `${kisa(top(aktif, 'gecikenTutar'))} · gecikme faizi ${kisa(top(aktif, 'gecikmeFaizi'))}`],
      ['#0369a1', 'Rehin · İpotek · Teminat', kisa(top(aktif, 'teminatToplam') + bagAktif.reduce((s, t) => s + n0(t.tutar), 0)), `ipotek ${kisa(top(aktif, 'ipotekToplam') + bagAktif.filter(t => t.tur === 'İpotek').reduce((s, t) => s + n0(t.tutar), 0))} · ${fekBekleyen} fek bekliyor · ${cekler.length} teminat çeki ${kisa(cekTl)} · ${belgeN} belge`],
      ['#15803d', 'Kasa Nakit Akışı', `${kasaSatir} taksit`, `${kasaOdendi} ödendi · ${kasaSatir - kasaOdendi} planlı · ${kisa(top(L, k => k.kasaOzet.planliTutar))} ₺ bekliyor`]
    ];
    $('#kyKpi').innerHTML = K.map(k => `<div style="--k:${k[0]}"><div class="l">${esc(k[1])}</div><div class="v">${esc(k[2])}</div><div class="s">${esc(k[3])}</div></div>`).join('');
  }
  function tablo(p, basliklar, satirlar, dip, bosMetin, grup) {
    const t = $('#kyTablo-' + p); if (!t) return;
    t.tHead.innerHTML = (grup ? '<tr class="grup">' + grup.map(g => `<th colspan="${g[1]}">${esc(g[0])}</th>`).join('') + '</tr>' : '') + '<tr>' + basliklar.map(b => `<th>${esc(b)}</th>`).join('') + '</tr>';
    t.tBodies[0].innerHTML = satirlar.length ? satirlar.join('') : `<tr><td colspan="${basliklar.length}" class="na-bos text-center">${bosMetin || 'Filtreye uyan kredi yok. "Kredi Ekle" ile yeni yapılandırma girin.'}</td></tr>`;
    t.tFoot.innerHTML = satirlar.length && dip ? dip : '';
  }
  const noH = k => `<td class="ad"><a href="#" class="ky-duzenle" data-id="${k.id}" title="Düzenle">${esc(k.no)}</a>${k.ornek ? ' ' + rz('Örnek', 'ornek') : ''}</td>`;
  const islemH = k => `<td><button type="button" class="btn btn-light border ky-duzenle" data-id="${k.id}" title="Düzenle"><i class="fas fa-pen"></i></button> <button type="button" class="btn btn-light border ky-takvim" data-id="${k.id}" title="Taksit takvimi"><i class="fas fa-calendar-days"></i></button> <button type="button" class="btn btn-light border ky-belge-yukle" data-kredi="${k.id}" title="Belge yükle: sözleşme / protokol / GKS (PDF)"><i class="fas fa-paperclip"></i></button> <button type="button" class="btn btn-light border ky-aktar" data-id="${k.id}" data-aktar="${k.nakitAkisaAktar ? '0' : '1'}" title="${k.nakitAkisaAktar ? 'Nakit akışa aktarımı kapat (ödenmemiş taksit satırları kaldırılır)' : 'Taksitleri Kasa Nakit Akışı ve Planlı Ödemeler\'e aktar'}"><i class="fas ${k.nakitAkisaAktar ? 'fa-link-slash' : 'fa-paper-plane'}"></i></button></td>`;
  const kasaH = k => `<td>${k.nakitAkisaAktar ? (k.kasaOzet.satir ? `<a href="${esc(k.kasaOzet.link)}" target="_blank" rel="noopener" title="Kasa Nakit Akışı › Havale / EFT">${k.kasaOzet.satir} satır</a><small>${k.kasaOzet.odendi} ödendi · ${k.kasaOzet.planli} planlı${k.duzenli ? ' · ' + esc(k.duzenli.no) : ''}</small>` : '<span class="ky-rz gri">Satır yok</span>') : '<span class="ky-rz gri">Aktarım Kapalı</span>'}</td>`;
  const belgeH = k => `<td>${k.belgeSayisi || k.cekSayisi ? `<a href="#" class="ky-belgeler" data-id="${k.id}" title="Belgeler sekmesinde bu kredinin belgeleri">${n0(k.belgeSayisi)} belge</a><small>${n0(k.cekSayisi)} teminat çeki</small>` : bos}</td>`;

  function cizKrediler(L) {
    const grup = [['Kredi', 5], ['Yapılandırma Öncesi', 4], ['Yapılandırma Sonrası', 9], ['Teminat · Belge', 2], ['Akış', 2]];
    const bas = ['Kredi No', 'Şirket', 'Banka · Şube', 'Kredi No · Tür', 'Durum', 'Kalan Anapara', 'Geciken Taksit', 'Eski Taksit', 'Toplam Borç', 'Peşinat', 'Yeni Anapara', 'Faiz % (Eski → Yeni)', 'Vade Ay (Kalan → Yeni)', 'Yeni Taksit', 'Aylık Fark', 'Toplam Geri Ödeme', 'Toplam Faiz', 'Borç Farkı', 'Teminat · İpotek', 'Belge · Çek', 'Kasa Nakit Akışı', 'İşlem'];
    const pesinatH = k => k.pesinat > 0 ? `${tl(k.pesinat)}<small>${tarih(k.pesinatTarihi)} · ${k.pesinatOdendi ? 'ödendi' : 'ödenecek'}</small>` : bos;
    const rows = L.map(k => `<tr class="${k.ornek ? 'ornek' : ''}" data-id="${k.id}">${noH(k)}<td>${esc(k.sirket)}</td><td class="ad">${esc(k.banka)}<small>${esc(k.sube || '')}</small></td><td>${esc(k.krediNo || '—')}<small>${esc(k.krediTuru)}</small></td><td>${durumRz(k.durum)}<small>${k.yapilandirmaTarihi ? 'yapılandırma ' + tarih(k.yapilandirmaTarihi) : ''}</small></td>
      <td class="para">${tl(k.kalanAnapara)}<small>${k.kalanVade ? k.kalanVade + ' ay kaldı' : ''}</small></td><td class="para">${k.gecikenTaksit ? `${k.gecikenTaksit} adet<small>${tl(k.gecikenTutar)}</small>` : bos}</td><td class="para">${tl(k.taksit)}<small>${yuzde(k.faizOrani)}</small></td><td class="para">${tl(k.oncesiToplamBorc)}<small>gecikme faizi ${kisa(k.gecikmeFaizi)} · masraf ${kisa(k.masraf)}</small></td>
      <td class="para">${pesinatH(k)}</td><td class="para">${tl(k.yeniAnapara)}</td><td>${yuzde(k.faizOrani)} → <b>${yuzde(k.yeniFaizOrani)}</b><small>BSMV ${yuzde(k.bsmvOrani)}</small></td><td>${k.kalanVade} → <b>${k.yeniVade}</b>${k.odemesizAy ? `<small>${k.odemesizAy} ay ödemesiz</small>` : ''}</td><td class="para">${tl(k.uygulananTaksit)}<small>${k.yeniTaksit > 0 ? 'banka taksiti' : 'hesaplanan'} · ${k.taksitSayisi} taksit</small></td><td>${fark(k.aylikFark)}</td><td class="para">${tl(k.toplamGeriOdeme)}<small>${k.pesinat > 0 ? 'peşinat dahil · ' : ''}${tarih(k.ilkTaksitTarihi)} – ${tarih(k.sonTaksitTarihi)}</small></td><td class="para">${tl(k.toplamFaiz)}</td><td>${fark(k.borcFarki)}</td>
      <td class="para">${k.teminatlar.length ? `${tl(k.teminatToplam)}<small>${k.teminatlar.length} teminat · ipotek ${kisa(k.ipotekToplam)}</small>` : bos}</td>${belgeH(k)}${kasaH(k)}${islemH(k)}</tr>`);
    const dip = `<tr><td colspan="5">Toplam · ${L.length} kredi</td><td class="para">${tl(top(L, 'kalanAnapara'))}</td><td class="para">${top(L, 'gecikenTaksit')} adet<small>${tl(top(L, 'gecikenTutar'))}</small></td><td class="para">${tl(top(L, 'taksit'))}</td><td class="para">${tl(top(L, 'oncesiToplamBorc'))}</td><td class="para">${tl(top(L, 'pesinat'))}</td><td class="para">${tl(top(L, 'yeniAnapara'))}</td><td></td><td></td><td class="para">${tl(top(L, 'uygulananTaksit'))}</td><td>${fark(top(L, 'aylikFark'))}</td><td class="para">${tl(top(L, 'toplamGeriOdeme'))}</td><td class="para">${tl(top(L, 'toplamFaiz'))}</td><td>${fark(top(L, 'borcFarki'))}</td><td class="para">${tl(top(L, 'teminatToplam'))}</td><td><small>${top(L, 'belgeSayisi')} belge · ${top(L, 'cekSayisi')} çek</small></td><td colspan="2"><small>${top(L, k => k.kasaOzet.satir)} taksit satırı · ${top(L, k => k.kasaOzet.odendi)} ödendi</small></td></tr>`;
    tablo('krediler', bas, rows, dip, null, grup);
    $('#kyNot-krediler').textContent = `${L.length} kredi · Aylık Fark = yeni taksit − eski taksit · Borç Farkı = toplam geri ödeme − öncesi toplam borç · satıra tıklayın: düzenle`;
    $('#kyAciklama-krediler').textContent = 'Öncesi: kalan anapara + geciken taksitler + gecikme faizi + masraf = Toplam Borç. Sonrası: peşinat protokolde peşin ödenir, kalan yeni anapara üzerinden ödemesiz dönem ve yeni vade ile eşit taksit (banka taksit tutarı verildiyse o kullanılır); Toplam Geri Ödeme = peşinat + taksitlerin toplamı, Toplam Faiz = BSMV dahil faiz. Rehin · İpotek Takibi sekmesinde teminat ayrıntıları ve fek durumu, Belgeler sekmesinde taranmış sözleşme / protokol / GKS, Taksit Takvimi\'nde peşinat ve taksitlerin Kasa Nakit Akışı durumu.';
  }
  function cizOncesi(L) {
    const bas = ['Kredi No', 'Şirket', 'Banka · Şube', 'Kredi No', 'Kredi Türü', 'Kullanım Tarihi', 'Anapara', 'Faiz %', 'Vade (Ay)', 'Taksit', 'Kalan Anapara', 'Kalan Vade (Ay)', 'Son Ödeme', 'Geciken Taksit', 'Geciken Tutar', 'Gecikme Faizi', 'Masraf / Komisyon', 'Toplam Borç', 'Önceki Durum', 'İşlem'];
    const rows = L.map(k => `<tr class="${k.ornek ? 'ornek' : ''}" data-id="${k.id}">${noH(k)}<td>${esc(k.sirket)}</td><td class="ad">${esc(k.banka)}<small>${esc(k.sube || '')}</small></td><td>${esc(k.krediNo || '—')}</td><td>${esc(k.krediTuru)}</td><td>${tarih(k.kullanimTarihi)}</td><td class="para">${tl(k.anapara)}</td><td>${yuzde(k.faizOrani)}</td><td>${k.vade}</td><td class="para">${tl(k.taksit)}</td><td class="para">${tl(k.kalanAnapara)}</td><td>${k.kalanVade}</td><td>${tarih(k.sonOdemeTarihi)}</td><td>${k.gecikenTaksit || '—'}</td><td class="para">${tl(k.gecikenTutar)}</td><td class="para">${tl(k.gecikmeFaizi)}</td><td class="para">${tl(k.masraf)}</td><td class="para genel">${tl(k.oncesiToplamBorc)}</td><td>${oncekiRz(k.oncekiDurum)}</td>${islemH(k)}</tr>`);
    const dip = `<tr><td colspan="6">Toplam · ${L.length} kredi</td><td class="para">${tl(top(L, 'anapara'))}</td><td></td><td></td><td class="para">${tl(top(L, 'taksit'))}</td><td class="para">${tl(top(L, 'kalanAnapara'))}</td><td></td><td></td><td>${top(L, 'gecikenTaksit')}</td><td class="para">${tl(top(L, 'gecikenTutar'))}</td><td class="para">${tl(top(L, 'gecikmeFaizi'))}</td><td class="para">${tl(top(L, 'masraf'))}</td><td class="para">${tl(top(L, 'oncesiToplamBorc'))}</td><td></td><td></td></tr>`;
    tablo('oncesi', bas, rows, dip);
    $('#kyNot-oncesi').textContent = 'yapılandırma öncesi: bankadan alınan kredinin orijinal koşulları, kalan borç ve gecikme durumu';
    $('#kyAciklama-oncesi').textContent = 'Toplam Borç = kalan anapara + geciken taksit tutarı + gecikme faizi + masraf / komisyon. Önceki Durum: Düzenli · Gecikmede · Yakın İzleme · Yasal Takipte.';
  }
  function cizSonrasi(L) {
    const bas = ['Kredi No', 'Şirket', 'Banka', 'Kredi No', 'Yapılandırma Tarihi', 'Protokol No', 'Peşinat', 'Yeni Anapara', 'Yeni Faiz %', 'BSMV %', 'Yeni Vade (Ay)', 'Ödemesiz Ay', 'Taksit Sayısı', 'Yeni Taksit', 'Hesaplanan Taksit', 'İlk Taksit', 'Son Taksit', 'Ödenen Taksit', 'Toplam Anapara', 'Toplam Faiz', 'Toplam Geri Ödeme', 'Durum', 'Kasa Nakit Akışı', 'İşlem'];
    const rows = L.map(k => `<tr class="${k.ornek ? 'ornek' : ''}" data-id="${k.id}">${noH(k)}<td>${esc(k.sirket)}</td><td class="ad">${esc(k.banka)}</td><td>${esc(k.krediNo || '—')}</td><td>${tarih(k.yapilandirmaTarihi)}</td><td>${esc(k.protokolNo || '—')}</td><td class="para">${k.pesinat > 0 ? `${tl(k.pesinat)}<small>${tarih(k.pesinatTarihi)} · ${k.pesinatOdendi ? 'ödendi' : 'ödenecek'}${k.pesinatKasa ? ' · #' + k.pesinatKasa.docEntry : ''}</small>` : bos}</td><td class="para">${tl(k.yeniAnapara)}</td><td>${yuzde(k.yeniFaizOrani)}</td><td>${yuzde(k.bsmvOrani)}</td><td>${k.yeniVade}</td><td>${k.odemesizAy || '—'}</td><td>${k.taksitSayisi}</td><td class="para">${tl(k.uygulananTaksit)}<small>${k.yeniTaksit > 0 ? 'banka' : 'hesaplanan'}</small></td><td class="para">${tl(k.hesaplananTaksit)}</td><td>${tarih(k.ilkTaksitTarihi)}</td><td>${tarih(k.sonTaksitTarihi)}</td><td>${k.odenenTaksit} / ${k.taksitSayisi}</td><td class="para">${tl(k.toplamAnapara)}</td><td class="para">${tl(k.toplamFaiz)}</td><td class="para genel">${tl(k.toplamGeriOdeme)}</td><td>${durumRz(k.durum)}</td>${kasaH(k)}${islemH(k)}</tr>`);
    const dip = `<tr><td colspan="6">Toplam · ${L.length} kredi</td><td class="para">${tl(top(L, 'pesinat'))}</td><td class="para">${tl(top(L, 'yeniAnapara'))}</td><td></td><td></td><td></td><td></td><td>${top(L, 'taksitSayisi')}</td><td class="para">${tl(top(L, 'uygulananTaksit'))}</td><td class="para">${tl(top(L, 'hesaplananTaksit'))}</td><td></td><td></td><td>${top(L, 'odenenTaksit')}</td><td class="para">${tl(top(L, 'toplamAnapara'))}</td><td class="para">${tl(top(L, 'toplamFaiz'))}</td><td class="para">${tl(top(L, 'toplamGeriOdeme'))}</td><td colspan="3"></td></tr>`;
    tablo('sonrasi', bas, rows, dip);
    $('#kyNot-sonrasi').textContent = 'yapılandırma sonrası: protokol koşulları ve taksit planı · Hesaplanan Taksit anüite formülüyle (aylık oran = yıllık faiz / 12 × (1 + BSMV))';
    $('#kyAciklama-sonrasi').textContent = 'Peşinat protokol tarihinde peşin ödenir ve taksitlendirilen yeni anaparanın dışındadır (Toplam Geri Ödeme = peşinat + taksitler). Yeni Taksit boş bırakılırsa hesaplanan taksit uygulanır; banka protokolündeki taksit girilirse o kullanılır, faiz / anapara ayrımı buna göre yapılır ve son taksit kalanı kapatır. Ödemesiz aylarda faiz anaparaya eklenir. Ödenen Taksit: yeni plandan şimdiye kadar ödenenler (Kasa Nakit Akışı\'nda ÖDENDİ yazılır).';
  }
  function cizTakvim(L) {
    const K = takvimKredi ? L.filter(k => String(k.id) === takvimKredi) : L;
    const bas = ['Kredi No', 'Banka', 'Şirket', 'Taksit No', 'Taksit Tarihi', 'Taksit Tutarı', 'Anapara', 'Faiz (BSMV Dahil)', 'Kalan Anapara', 'Ödendi (Plan)', 'Kasa Nakit Akışı', 'Planlı Ödeme'];
    const satirlar = []; K.forEach(k => { if (k.pesinat > 0) satirlar.push({ k, t: { no: 0, pesinat: true, tarih: k.pesinatTarihi || '', tutar: k.pesinat, anapara: k.pesinat, faiz: 0, kalan: k.yeniAnapara, odendi: k.pesinatOdendi, kasa: k.pesinatKasa } }); (k.taksitler || []).forEach(t => satirlar.push({ k, t })); });   // peşinat takvimin ilk satırı
    satirlar.sort((a, b) => a.t.tarih.localeCompare(b.t.tarih) || a.k.no.localeCompare(b.k.no, 'tr') || a.t.no - b.t.no);
    const bugun = V.bugun || '';
    const rows = satirlar.map(({ k, t }) => `<tr class="${t.odendi || (t.kasa && t.kasa.durum === 'ÖDENDİ') ? 'odendi' : (t.tarih < bugun ? 'gecikti' : '')}" data-id="${k.id}"><td class="ad"><a href="#" class="ky-duzenle" data-id="${k.id}">${esc(k.no)}</a></td><td>${esc(k.banka)}<small>${esc(k.krediNo || k.krediTuru)}</small></td><td>${esc(k.sirket)}</td><td>${t.pesinat ? rz('Peşinat', 'mavi') : `${t.no} / ${k.taksitSayisi}`}</td><td>${tarih(t.tarih)}<small>${ayAd(t.tarih.slice(0, 7))}</small></td><td class="para">${tl(t.tutar)}</td><td class="para">${tl(t.anapara)}</td><td class="para">${tl(t.faiz)}</td><td class="para">${tl(t.kalan)}</td><td>${t.odendi ? rz('Ödendi', 'ok') : (t.tarih < bugun ? rz('Vadesi Geçti', 'hata') : rz('Planlı', 'gri'))}</td><td>${t.kasa ? `<a href="${esc(t.kasa.link)}" target="_blank" rel="noopener" title="Kasa Nakit Akışı satırı">#${t.kasa.docEntry}</a> ${kasaRz(t.kasa, k.nakitAkisaAktar)}` : kasaRz(null, k.nakitAkisaAktar)}</td><td>${k.duzenli ? `<a href="${esc(k.duzenli.link)}" target="_blank" rel="noopener">${esc(k.duzenli.no)}</a>` : bos}</td></tr>`);
    const dip = `<tr><td colspan="5">Toplam · ${satirlar.filter(x => !x.t.pesinat).length} taksit${satirlar.some(x => x.t.pesinat) ? ' + ' + satirlar.filter(x => x.t.pesinat).length + ' peşinat' : ''} · ${K.length} kredi</td><td class="para">${tl(satirlar.reduce((s, x) => s + n0(x.t.tutar), 0))}</td><td class="para">${tl(satirlar.reduce((s, x) => s + n0(x.t.anapara), 0))}</td><td class="para">${tl(satirlar.reduce((s, x) => s + n0(x.t.faiz), 0))}</td><td></td><td>${satirlar.filter(x => x.t.odendi).length} ödendi</td><td>${satirlar.filter(x => x.t.kasa).length} satır · ${satirlar.filter(x => x.t.kasa && x.t.kasa.durum === 'ÖDENDİ').length} ödendi</td><td></td></tr>`;
    tablo('takvim', bas, rows, dip, 'Taksit yok: yapılandırma sonrası yeni anapara ve vade girildiğinde takvim oluşur.');
    $('#kyNot-takvim').textContent = `${satirlar.length} taksit · tarihe göre · Kasa Nakit Akışı sütunu satır numarası ve ödeme durumunu gösterir (Havale / EFT sekmesi)`;
    $('#kyAciklama-takvim').textContent = 'Peşinat (varsa) takvimin ilk satırıdır ve Kasa Nakit Akışı\'na "KRD-001 · Peşinat" satırı olarak yazılır. Her taksit planlanan tarihli Havale / EFT satırı olur (belge no "KRD-001 · 03/36"); satır orada ödendi işaretlenince burada Ödendi görünür. Kredi kartındaki "Ödenen Taksit" sayısı kadar ilk taksit ve "Peşinat Ödendi" işareti ödenmiş sayılır.';
  }
  function cizAylik(L) {
    const K = L.filter(k => k.durum !== 'İptal'); const aylar = new Map();
    const ayKey = (y, m) => `${y}-${String(m).padStart(2, '0')}`; const bugun = new Date(V.bugun || Date.now()); const by = bugun.getFullYear(), bm = bugun.getMonth() + 1;
    K.forEach(k => (k.taksitler || []).forEach(t => { const a = t.tarih.slice(0, 7); const o = aylar.get(a) || { ay: a, eski: 0, yeni: 0, anapara: 0, faiz: 0, taksit: 0, kalan: 0 }; o.yeni += n0(t.tutar); o.anapara += n0(t.anapara); o.faiz += n0(t.faiz); o.taksit++; aylar.set(a, o); }));
    K.forEach(k => { if (k.pesinat > 0 && k.pesinatTarihi) { const a = k.pesinatTarihi.slice(0, 7); const o = aylar.get(a) || { ay: a, eski: 0, yeni: 0, anapara: 0, faiz: 0, taksit: 0, kalan: 0 }; o.yeni += n0(k.pesinat); o.anapara += n0(k.pesinat); o.pesinat = (o.pesinat || 0) + n0(k.pesinat); aylar.set(a, o); } });   // peşinat kendi ayında
    // eski plan: yapılandırma olmasaydı kalan vade kadar eski taksit devam ederdi (bu aydan başlayarak)
    K.forEach(k => { for (let i = 0; i < n0(k.kalanVade); i++) { const d = new Date(by, bm - 1 + i, 1); const a = ayKey(d.getFullYear(), d.getMonth() + 1); const o = aylar.get(a) || { ay: a, eski: 0, yeni: 0, anapara: 0, faiz: 0, taksit: 0, kalan: 0 }; o.eski += n0(k.taksit); aylar.set(a, o); } });
    const sira = [...aylar.values()].sort((a, b) => a.ay.localeCompare(b.ay));
    // ay sonu kalan anapara: her kredinin o aya kadar ödenmiş taksitlerinden sonraki kalanı (taksiti olmayan kredide önceki kalan / yeni anapara)
    sira.forEach(o => { o.kalan = K.reduce((s, k) => { const ts = (k.taksitler || []).filter(t => t.tarih.slice(0, 7) <= o.ay); return s + (ts.length ? n0(ts[ts.length - 1].kalan) : n0(k.yeniAnapara)); }, 0); });
    const bas = ['Ay', 'Eski Plan Taksit Toplamı', 'Yeni Plan Taksit Toplamı', 'Fark (Yeni − Eski)', 'Taksit Adedi', 'Anapara', 'Faiz (BSMV Dahil)', 'Ay Sonu Kalan Anapara'];
    const rows = sira.map(o => `<tr class="${o.ay === ayKey(by, bm) ? 'odendi' : ''}"><td class="ad">${ayAd(o.ay)}${o.ay === ayKey(by, bm) ? ' ' + rz('Bu Ay', 'mavi') : ''}</td><td class="para">${o.eski ? tl(o.eski) : bos}</td><td class="para">${o.yeni ? tl(o.yeni) + (o.pesinat ? `<small>peşinat ${tl(o.pesinat)} dahil</small>` : '') : bos}</td><td>${fark(o.yeni - o.eski)}</td><td>${o.taksit || (o.pesinat ? 'peşinat' : '—')}</td><td class="para">${o.anapara ? tl(o.anapara) : bos}</td><td class="para">${o.faiz ? tl(o.faiz) : bos}</td><td class="para">${tl(o.kalan)}</td></tr>`);
    const dip = `<tr><td>Toplam · ${sira.length} ay</td><td class="para">${tl(sira.reduce((s, o) => s + o.eski, 0))}</td><td class="para">${tl(sira.reduce((s, o) => s + o.yeni, 0))}</td><td>${fark(sira.reduce((s, o) => s + o.yeni - o.eski, 0))}</td><td>${sira.reduce((s, o) => s + o.taksit, 0)}</td><td class="para">${tl(sira.reduce((s, o) => s + o.anapara, 0))}</td><td class="para">${tl(sira.reduce((s, o) => s + o.faiz, 0))}</td><td></td></tr>`;
    tablo('aylik', bas, rows, dip, 'Taksit takvimi olan kredi yok.');
    $('#kyNot-aylik').textContent = 'ay bazında nakit yükü: eski plan (kalan vade boyunca eski taksit) ↔ yeni plan (yapılandırma taksitleri)';
    $('#kyAciklama-aylik').textContent = 'Eski Plan: yapılandırma yapılmasaydı bu aydan itibaren kalan vade boyunca ödenecek eski taksitler. Yeni Plan: yapılandırma takviminin o aya düşen taksitleri (ödemesiz aylarda boş). Fark eksi ise aylık yük azaldı, artı ise arttı; vade uzadığı için yeni plan daha uzun sürer. Ay Sonu Kalan Anapara: tüm kredilerin o ay sonundaki kalan borcu.';
  }
  /** Rehin · İpotek Takibi: kredi kartındaki teminatlar + eski krediler için bağımsız kayıtlar; durum / tesis / fek, bağlı belge sayısı */
  function cizRehin() {
    const R = rehinListe();
    const bas = ['Kayıt', 'Şirket', 'Banka', 'Kredi / Referans', 'Teminat Türü', 'Açıklama', 'Tapu / Kimlik Bilgisi', 'Derece', 'Malik / Kefil', 'Teminat Tutarı', 'Ekspertiz Değeri', 'Tesis Tarihi', 'Durum', 'Fek Tarihi', 'Kredi Kapanış', 'Belge', 'Not', 'İşlem'];
    const rows = R.map(r => {
      const t = r.t, k = r.k, kredi = r.kaynak === 'kredi';
      const kayitH = kredi ? `<td class="ad"><a href="#" class="ky-duzenle" data-id="${k.id}" title="Kredi kartında düzenle">${esc(k.no)}</a>${k.ornek ? ' ' + rz('Örnek', 'ornek') : ''}<small>yapılandırılan kredi</small></td>` : `<td class="ad">${rz('Eski Kredi', 'gri')}<small>bağımsız kayıt ${t.id}</small></td>`;
      const refH = kredi ? `${esc(k.krediNo || '—')}<small>${esc(k.krediTuru)} · ${esc(k.durum)}</small>` : `${esc(t.krediRef || '—')}<small>${k ? 'yapılandırma <a href="#" class="ky-duzenle" data-id="' + k.id + '">' + esc(k.no) + '</a>' : 'yapılandırma kaydı yok'}</small>`;
      const uyari = r.durum === 'Fek Bekliyor' ? '<small>kredi kapandı · fek yazısı alınmalı</small>' : (kredi && k.durum === 'Tamamlandı' && r.durum === 'Aktif') || (!kredi && t.krediKapanis && r.durum === 'Aktif') ? '<small>kredi kapanmış · fek kontrol edilmeli</small>' : '';
      const belgeN = kredi ? V.belgeler.filter(b => b.krediId === k.id && TEMINAT_BELGE.includes(b.tur)).length : V.belgeler.filter(b => b.teminatId === t.id).length;
      const belge = belgeN ? `<a href="#" class="ky-belge-git" data-tip="${kredi ? 'kredi' : 'teminat'}" data-id="${kredi ? k.id : t.id}" title="Belgeler sekmesinde göster">${belgeN} belge</a>` : bos;
      const islem = kredi
        ? `<button type="button" class="btn btn-light border ky-duzenle" data-id="${k.id}" title="Kredi kartında düzenle"><i class="fas fa-pen"></i></button> <button type="button" class="btn btn-light border ky-belge-yukle" data-kredi="${k.id}" data-tur="İpotek Belgesi" title="İpotek / rehin belgesi yükle"><i class="fas fa-paperclip"></i></button>`
        : `<button type="button" class="btn btn-light border ky-rehin-duzenle" data-id="${t.id}" title="Düzenle"><i class="fas fa-pen"></i></button> <button type="button" class="btn btn-light border ky-belge-yukle" data-teminat="${t.id}" data-tur="İpotek Belgesi" title="İpotek / rehin belgesi yükle"><i class="fas fa-paperclip"></i></button> <button type="button" class="btn btn-light border text-danger ky-rehin-sil" data-id="${t.id}" title="Kaydı sil"><i class="fas fa-trash"></i></button>`;
      return `<tr class="${r.durum === 'Fek Edildi' ? 'soluk' : r.durum === 'Fek Bekliyor' ? 'gecikti' : ''}">${kayitH}<td>${esc(r.sirket)}</td><td class="ad">${esc(r.banka)}</td><td>${refH}</td><td>${rz(t.tur, t.tur === 'İpotek' ? 'mavi' : 'gri')}</td><td class="metin">${esc(t.aciklama || '—')}</td><td class="metin">${esc(t.tapuBilgisi || '—')}</td><td>${esc(t.derece || '—')}</td><td>${esc(t.malik || '—')}</td><td class="para">${tl(t.tutar)}</td><td class="para">${t.ekspertiz ? tl(t.ekspertiz) : bos}</td><td>${tarih(t.tesisTarihi)}</td><td>${teminatDurumRz(r.durum)}${uyari}</td><td>${tarih(t.fekTarihi)}</td><td>${kredi ? (k.durum === 'Tamamlandı' ? tarih(k.sonTaksitTarihi) : bos) : tarih(t.krediKapanis)}</td><td>${belge}</td><td class="metin"><small style="display:inline">${esc(t.not || '')}</small></td><td>${islem}</td></tr>`;
    });
    const aktif = R.filter(r => aktifTeminat(r.durum)), say = d => R.filter(r => r.durum === d).length, topla = (l, f) => l.reduce((s, r) => s + n0(r.t[f]), 0);
    const dip = `<tr><td colspan="9">Toplam · ${R.length} teminat · ${say('Aktif')} aktif · ${say('Fek Bekliyor')} fek bekliyor · ${say('Fek Edildi')} fek edildi · ${say('Paraya Çevrildi')} paraya çevrildi</td><td class="para">${tl(topla(aktif, 'tutar'))}<small>aktif · fek edilen hariç</small></td><td class="para">${tl(topla(aktif, 'ekspertiz'))}</td><td colspan="7"><small>ipotek ${tl(topla(aktif.filter(r => r.t.tur === 'İpotek'), 'tutar'))} · ${R.filter(r => r.kaynak === 'eski').length} eski kredi kaydı</small></td></tr>`;
    tablo('rehin', bas, rows, dip, 'Rehin / ipotek kaydı yok. Yapılandırılan kredinin teminatlarını kredi kartında "Teminat Ekle" ile, yapılandırma kaydı olmayan eski kredilerin ipotek ve rehinlerini "Rehin / İpotek Ekle" ile girin.');
    $('#kyKapsam').textContent = `${R.length} teminat · ${say('Aktif')} aktif · ${say('Fek Bekliyor')} fek bekliyor · ${[...new Set(R.map(r => r.banka))].length} banka · ${V.bugun ? tarih(V.bugun) : ''}`;
    $('#kyNot-rehin').textContent = `${R.length} teminat · fek bekleyenler üstte · Durum: Aktif → kredi kapanınca Fek Bekliyor → fek yazısı alınınca Fek Edildi`;
    $('#kyAciklama-rehin').textContent = 'Yapılandırılan kredilerin teminatları kredi kartından gelir (tesis tarihi, durum ve fek tarihi orada güncellenir). Eski ya da yapılandırma dışı kredilerin ipotek / rehinleri bağımsız kayıt olarak izlenir: hangi banka ve kredi için tesis edildi, kredi ne zaman kapandı, fek (ipotek / rehin kaldırma) yapıldı mı. Fek Bekliyor: kredi kapanmış ama ipotek / rehin tapu ya da sicilden kaldırılmamış; fek yazısı ve ipotek belgeleri Belgeler sekmesine yüklenir (ataç düğmesi kaydı bağlar).';
  }
  /** Teminat Çekleri: bankaya teminat verilen müşteri çek / senetleri; vadeye göre; teminatta kalan toplam dipte */
  function cizCekler() {
    const C = cekListe(), bugun = V.bugun || '';
    const bas = ['Şirket', 'Teminat Verilen Banka', 'Kredi / Referans', 'Tür', 'Çek / Senet No', 'Keşideci', 'Çekin Bankası', 'Vade', 'Tutar', 'Veriliş Tarihi', 'Durum', 'İade / Tahsil Tarihi', 'Belge', 'Not', 'İşlem'];
    const rows = C.map(c => {
      const k = c.krediId ? krediSec(c.krediId) : null, vadeGecti = c.durum === 'Teminatta' && c.vade && c.vade < bugun, belgeN = V.belgeler.filter(b => b.cekId === c.id).length;
      return `<tr class="${c.durum === 'Karşılıksız' ? 'gecikti' : c.durum === 'Teminatta' ? '' : 'soluk'}" data-id="${c.id}"><td>${esc(c.sirket)}</td><td class="ad">${esc(c.banka)}</td><td>${k ? `<a href="#" class="ky-duzenle" data-id="${k.id}">${esc(k.no)}</a><small>${esc(k.krediNo || k.krediTuru)}</small>` : `${esc(c.krediRef || '—')}<small>${c.krediRef ? 'yapılandırma kaydı yok' : 'GKS genel teminatı'}</small>`}</td><td>${rz(c.belge, c.belge === 'Senet' ? 'gri' : 'mavi')}</td><td class="ad"><a href="#" class="ky-cek-duzenle" data-id="${c.id}" title="Düzenle">${esc(c.cekNo)}</a></td><td>${esc(c.kesideci || '—')}</td><td>${esc(c.cekBankasi || '—')}</td><td>${tarih(c.vade)}${vadeGecti ? '<small>vadesi geçti · banka tahsil etmiş olabilir</small>' : ''}</td><td class="para">${tutarPb(c.tutar, c.pb)}</td><td>${tarih(c.verilisTarihi)}</td><td>${cekDurumRz(c.durum)}</td><td>${tarih(c.iadeTarihi)}</td><td>${belgeN ? `<a href="#" class="ky-belge-git" data-tip="cek" data-id="${c.id}" title="Belgeler sekmesinde göster">${belgeN} belge</a>` : bos}</td><td class="metin"><small style="display:inline">${esc(c.not || '')}</small></td><td><button type="button" class="btn btn-light border ky-cek-duzenle" data-id="${c.id}" title="Düzenle"><i class="fas fa-pen"></i></button> <button type="button" class="btn btn-light border ky-belge-yukle" data-cek="${c.id}" data-tur="Teminat Çeki" title="Çek fotokopisi yükle"><i class="fas fa-paperclip"></i></button> <button type="button" class="btn btn-light border text-danger ky-cek-sil" data-id="${c.id}" title="Kaydı sil"><i class="fas fa-trash"></i></button></td></tr>`;
    });
    const teminatta = C.filter(c => c.durum === 'Teminatta'), say = d => C.filter(c => c.durum === d).length;
    const dip = `<tr><td colspan="8">Toplam · ${C.length} çek / senet · ${teminatta.length} teminatta · ${say('İade Alındı')} iade · ${say('Tahsil Edildi')} tahsil · ${say('Karşılıksız')} karşılıksız</td><td class="para">${pbTop(teminatta)}<small>teminatta olanlar</small></td><td colspan="6"><small>tümü ${pbTop(C)} · vadesi geçen ${teminatta.filter(c => c.vade && c.vade < bugun).length}</small></td></tr>`;
    tablo('cekler', bas, rows, dip, 'Teminat çeki yok. "Teminat Çeki Ekle" ile bankaya teminat olarak verilen müşteri çeklerini / senetlerini girin; fotokopisini ataç düğmesiyle Belgeler\'e yükleyin.');
    $('#kyKapsam').textContent = `${C.length} çek / senet · teminatta ${teminatta.length} adet ${pbTop(teminatta)} · ${[...new Set(C.map(c => c.banka))].length} banka`;
    $('#kyNot-cekler').textContent = `${C.length} çek / senet · vadeye göre · teminatta ${pbTop(teminatta)}`;
    $('#kyAciklama-cekler').textContent = 'Bankaya kredi ya da GKS teminatı olarak verilen müşteri çekleri / senetleri: hangi şirket, hangi bankaya, hangi kredi için verdi; keşideci, çekin bankası, vade ve tutar. Durum: Teminatta → vadesinde banka tahsil ederse Tahsil Edildi (kredi borcundan düşer), kredi kapanınca geri alınırsa İade Alındı, karşılıksız çıkarsa Karşılıksız. Dip satırda teminatta kalan toplam (para birimine göre).';
  }
  /** Belgeler · Sözleşme / GKS: yüklenen PDF / görseller; tür, bağlı kayıt (kredi / çek / rehin), aç · indir · sil */
  function belgeBagH(b) {
    const parca = [];
    if (b.krediId) { const k = krediSec(b.krediId); parca.push(k ? `<a href="#" class="ky-duzenle" data-id="${k.id}">${esc(k.no)}</a> · ${esc(k.banka)}` : esc(b.krediNo || 'kredi silinmiş')); }
    if (b.cekId) { const c = V.cekler.find(x => x.id === b.cekId); parca.push(c ? `<a href="#" class="ky-cek-duzenle" data-id="${c.id}">Çek ${esc(c.cekNo)}</a> · ${esc(c.kesideci)}` : 'çek kaydı silinmiş'); }
    if (b.teminatId) { const t = V.teminatlar.find(x => x.id === b.teminatId); parca.push(t ? `<a href="#" class="ky-rehin-duzenle" data-id="${t.id}">${esc(t.tur)}</a> · ${esc(t.aciklama || t.tapuBilgisi)}` : 'rehin kaydı silinmiş'); }
    return parca.length ? parca.join('<br>') : '<span class="ky-rz gri">Genel · Bağsız</span>';
  }
  function cizBelgeler() {
    const B = belgeListe();
    const bas = ['Belge Türü', 'Başlık', 'Bağlı Kayıt', 'Şirket', 'Banka', 'Belge Tarihi', 'Dosya', 'Boyut', 'Yükleyen · Yükleme', 'Açıklama', 'İşlem'];
    const rows = B.map(b => `<tr data-id="${b.id}"><td>${rz(b.tur, belgeRenk(b.tur))}</td><td class="ad metin">${esc(b.baslik)}</td><td>${belgeBagH(b)}</td><td>${esc(b.sirket || '—')}</td><td>${esc(b.banka || '—')}</td><td>${tarih(b.tarih)}</td><td>${dosyaH(b)}</td><td>${boyutH(b.boyut)}</td><td>${esc(b.yukleyen || '—')}<small>${esc(b.yukleme)}</small></td><td class="metin"><small style="display:inline">${esc(b.aciklama || '')}</small></td><td><a class="btn btn-light border" href="${esc(b.link)}" target="_blank" rel="noopener" title="Tarayıcıda aç"><i class="fas fa-eye"></i></a> <a class="btn btn-light border" href="${esc(b.indir)}" title="İndir"><i class="fas fa-download"></i></a> <button type="button" class="btn btn-light border text-danger ky-belge-sil" data-id="${b.id}" title="Belgeyi sil"><i class="fas fa-trash"></i></button></td></tr>`);
    const turler = [...B.reduce((m, b) => m.set(b.tur, (m.get(b.tur) || 0) + 1), new Map())].map(([t, n]) => `${t} ${n}`).join(' · ');
    const dip = `<tr><td colspan="7">Toplam · ${B.length} belge${turler ? ' · ' + turler : ''}</td><td>${boyutH(B.reduce((s, b) => s + n0(b.boyut), 0))}</td><td colspan="3"></td></tr>`;
    tablo('belgeler', bas, rows, dip, 'Belge yok. "Belge Yükle" ile taranmış kredi sözleşmesi, yapılandırma protokolü, Genel Kredi Sözleşmesi (GKS), ipotek / rehin belgesi, teminat mektubu ya da teminat çeki fotokopisini (PDF / JPG / PNG, en çok 20 MB) yükleyin.');
    const e = $('#kyBelgeBag'); if (e) e.innerHTML = belgeBag ? `<span class="ky-chip"><i class="fas fa-link"></i>${esc(belgeBag.ad)}<button type="button" id="kyBelgeBagSil" title="Bağ filtresini kaldır">×</button></span>` : '';
    $('#kyKapsam').textContent = `${B.length} / ${V.belgeler.length} belge · ${boyutH(B.reduce((s, b) => s + n0(b.boyut), 0))} · ${[...new Set(B.map(b => b.banka).filter(Boolean))].length} banka`;
    $('#kyNot-belgeler').textContent = `${B.length} belge · yükleme sırasına göre · Dosya sütunundan tarayıcıda açılır`;
    $('#kyAciklama-belgeler').textContent = 'Türler: Kredi Sözleşmesi, Yapılandırma Protokolü, Genel Kredi Sözleşmesi (GKS), İpotek Belgesi, Rehin Sözleşmesi, Teminat Mektubu, Teminat Çeki, Ödeme Planı, Ekspertiz Raporu, Fek Yazısı, Diğer. Belge krediye (kredi satırındaki ataç), rehin / ipotek kaydına ya da teminat çekine bağlanabilir; GKS gibi genel belgelerde yalnız şirket ve banka seçilir. Dosyalar sunucuda App_Data/kredi-belgeler altında saklanır; silinince dosya da kaldırılır.';
  }
  const CIZ = { krediler: cizKrediler, oncesi: cizOncesi, sonrasi: cizSonrasi, takvim: cizTakvim, aylik: cizAylik, rehin: cizRehin, cekler: cizCekler, belgeler: cizBelgeler };

  // ---------- sekme / filtre
  function sekme(p) { if (!CIZ[p]) p = p === 'teminat' ? 'rehin' : 'krediler'; $$('#kySekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#ky > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); if (V) filtreDoldur(); ciz(); }
  $('#kySekmeler').addEventListener('click', e => { const b = e.target.closest('button[data-p]'); if (b) sekme(b.dataset.p); });
  ['#kyS', '#kyB', '#kyD'].forEach(s => $(s).addEventListener('change', () => { F.sirket = $('#kyS').value; F.banka = $('#kyB').value; F.durum = $('#kyD').value; ciz(); }));
  $('#kyAra').addEventListener('input', () => { F.ara = $('#kyAra').value; ciz(); });
  $('#kyTemizle').addEventListener('click', () => { F = { sirket: '', banka: '', durum: '', ara: '' }; ['#kyS', '#kyB', '#kyD', '#kyAra'].forEach(s => $(s).value = ''); belgeBag = null; belgeKredi = ''; const bk = $('#kyBelgeKredi'); if (bk) bk.value = ''; ciz(); });
  const tk = $('#kyTakvimKredi'); if (tk) tk.addEventListener('change', () => { takvimKredi = tk.value; ciz(); });
  const bk = $('#kyBelgeKredi'); if (bk) bk.addEventListener('change', () => { belgeKredi = bk.value; belgeBag = null; ciz(); });
  kok.addEventListener('click', async e => {
    const d = e.target.closest('.ky-duzenle'); if (d) { e.preventDefault(); formAc(V.krediler.find(k => k.id === +d.dataset.id)); return; }
    const t = e.target.closest('.ky-takvim'); if (t) { takvimKredi = t.dataset.id; if (tk) tk.value = takvimKredi; sekme('takvim'); return; }
    const a = e.target.closest('.ky-aktar'); if (a) { const k = V.krediler.find(x => x.id === +a.dataset.id); const ac = a.dataset.aktar === '1'; if (!k) return; if (!window.confirm(ac ? `${k.no} · ${k.banka}: taksitler Kasa Nakit Akışı'na (Havale / EFT) ve Planlı Düzenli Ödemeler'e aktarılsın mı?` : `${k.no} · ${k.banka}: nakit akışa aktarım kapatılsın mı? Ödenmemiş taksit satırları kaldırılır, düzenli ödeme pasife alınır.`)) return; const j = await post('/KrediYapilandirma/Aktar', { id: k.id, aktar: ac }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); return; }
    // belge / rehin / çek (9 Kas 2026)
    const bl = e.target.closest('.ky-belgeler'); if (bl) { e.preventDefault(); belgeKredi = bl.dataset.id; belgeBag = null; sekme('belgeler'); return; }
    const bg = e.target.closest('.ky-belge-git'); if (bg) {
      e.preventDefault(); const id = +bg.dataset.id;
      if (bg.dataset.tip === 'kredi') { belgeKredi = String(id); belgeBag = null; }
      else { belgeKredi = ''; belgeBag = bg.dataset.tip === 'cek' ? { tip: 'cek', id, ad: 'Çek ' + ((V.cekler.find(c => c.id === id) || {}).cekNo || id) } : { tip: 'teminat', id, ad: 'Rehin / İpotek · ' + ((V.teminatlar.find(x => x.id === id) || {}).banka || id) }; }
      sekme('belgeler'); return;
    }
    const by = e.target.closest('.ky-belge-yukle'); if (by) { altAc('belge', null, { krediId: +by.dataset.kredi || 0, teminatId: +by.dataset.teminat || 0, cekId: +by.dataset.cek || 0, tur: by.dataset.tur || 'Kredi Sözleşmesi' }); return; }
    const rd = e.target.closest('.ky-rehin-duzenle'); if (rd) { e.preventDefault(); altAc('rehin', V.teminatlar.find(x => x.id === +rd.dataset.id)); return; }
    const rs = e.target.closest('.ky-rehin-sil'); if (rs) { altSil('rehin', V.teminatlar.find(x => x.id === +rs.dataset.id), false); return; }
    const cd = e.target.closest('.ky-cek-duzenle'); if (cd) { e.preventDefault(); altAc('cek', V.cekler.find(x => x.id === +cd.dataset.id)); return; }
    const cs = e.target.closest('.ky-cek-sil'); if (cs) { altSil('cek', V.cekler.find(x => x.id === +cs.dataset.id), false); return; }
    const bs = e.target.closest('.ky-belge-sil'); if (bs) { const bb = V.belgeler.find(x => x.id === +bs.dataset.id); if (!bb || !window.confirm(`"${bb.baslik}" (${bb.dosyaAdi}) silinsin mi? Dosya sunucudan kaldırılır.`)) return; const j = await post('/KrediYapilandirma/BelgeSil', { id: bb.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); return; }
    if (e.target.closest('#kyBelgeBagSil')) { belgeBag = null; ciz(); }
  });
  $('#kyYeni').addEventListener('click', () => formAc(null));
  const rehinEkle = $('#kyRehinEkle'); if (rehinEkle) rehinEkle.addEventListener('click', () => altAc('rehin', null, { sirket: F.sirket || undefined, banka: F.banka || undefined }));
  const cekEkle = $('#kyCekEkle'); if (cekEkle) cekEkle.addEventListener('click', () => altAc('cek', null, { sirket: F.sirket || undefined, banka: F.banka || undefined }));
  ['#kyBelgeYukle', '#kyBelgeYukle2'].forEach(s => { const b = $(s); if (b) b.addEventListener('click', () => altAc('belge', null, belgeKredi && belgeKredi !== 'yok' ? { krediId: +belgeKredi } : belgeBag ? (belgeBag.tip === 'cek' ? { cekId: belgeBag.id, tur: 'Teminat Çeki' } : { teminatId: belgeBag.id, tur: 'İpotek Belgesi' }) : null)); });
  $('#kyExcel').addEventListener('click', () => {
    const p = aktifSekme(), ad = ($(`#kySekmeler button[data-p="${p}"]`) || {}).textContent?.trim() || p;
    if (!window.TabloExcel) { mesaj('danger', 'Excel bileşeni yüklenemedi; sayfayı yenileyin.'); return; }
    window.TabloExcel.indir('kredi_raporu_' + p, [{ tablo: '#kyTablo-' + p, ad: ad.slice(0, 31), baslik: 'Kredi Raporu · ' + ad, altBaslik: $('#kyKapsam').textContent.trim(), kpi: '#kyKpi', not: '#kyAciklama-' + p }]).catch(err => mesaj('danger', 'Excel indirilemedi: ' + esc(err.message)));
  });

  // ---------- ortak form alanları
  const SAYI = (v, ond = 2) => { const x = Number(String(v ?? '').replace(/\./g, '').replace(',', '.')); return isNaN(x) ? 0 : Math.round(x * 10 ** ond) / 10 ** ond; };
  /** alan tanımı [anahtar, etiket, tip, kaynak, ekSınıf]; tipler: select · selectBos (boş seçenekli) · secim (id'li liste, kaynak = () => [{v, a}]) · liste (datalist) · date · para · oran · tam · onay · uzun · dosya · metin */
  function alanHtml(a, v) {
    const [k, ad, tip, kaynak] = a; const id = 'kyf_' + k; const lbl = `<label for="${id}">${esc(ad)}</label>`; const sinif = 'ky-f' + (a[4] ? ' ' + a[4] : '');
    const liste = () => kaynak === 'sirketler' ? V.sirketler.map(s => s.ad) : Array.isArray(kaynak) ? kaynak : (V[kaynak] || []);
    if (tip === 'select' || tip === 'selectBos') return `<div class="${sinif}">${lbl}<select class="form-select" id="${id}" data-k="${k}">${tip === 'selectBos' ? '<option value="">—</option>' : ''}${liste().map(o => `<option value="${esc(o)}" ${o === v ? 'selected' : ''}>${esc(o)}</option>`).join('')}</select></div>`;
    if (tip === 'secim') return `<div class="${sinif}">${lbl}<select class="form-select" id="${id}" data-k="${k}" data-tip="secim"><option value="0">—</option>${kaynak().map(o => `<option value="${o.v}" ${+o.v === +(v || 0) ? 'selected' : ''}>${esc(o.a)}</option>`).join('')}</select></div>`;
    if (tip === 'liste') return `<div class="${sinif}">${lbl}<input class="form-control" id="${id}" data-k="${k}" list="kyDl_${k}" value="${esc(v ?? '')}" maxlength="80"><datalist id="kyDl_${k}">${(V[kaynak] || []).map(o => `<option value="${esc(o)}">`).join('')}</datalist></div>`;
    if (tip === 'date') return `<div class="${sinif}">${lbl}<input class="form-control" type="date" id="${id}" data-k="${k}" value="${esc(v ?? '')}"></div>`;
    if (tip === 'para') return `<div class="${sinif}">${lbl}<input class="form-control" type="number" step="0.01" min="0" id="${id}" data-k="${k}" data-tip="para" value="${v ? Number(v) : ''}" placeholder="0,00"></div>`;
    if (tip === 'oran') return `<div class="${sinif}">${lbl}<input class="form-control" type="number" step="0.01" min="0" max="300" id="${id}" data-k="${k}" data-tip="oran" value="${v ?? ''}"></div>`;
    if (tip === 'tam') return `<div class="${sinif}">${lbl}<input class="form-control" type="number" step="1" min="0" id="${id}" data-k="${k}" data-tip="tam" value="${v ?? ''}"></div>`;
    if (tip === 'onay') return `<div class="${sinif}">${lbl}<label class="d-flex align-items-center gap-2" style="font-size:.72rem;text-transform:none;font-weight:600;color:#0f172a;margin-top:5px"><input type="checkbox" id="${id}" data-k="${k}" data-tip="onay" ${v ? 'checked' : ''}> Ödendi (Kasa Nakit Akışı satırı ÖDENDİ yazılır)</label></div>`;
    if (tip === 'uzun') return `<div class="${sinif}">${lbl}<textarea class="form-control" id="${id}" data-k="${k}" rows="2" maxlength="600">${esc(v ?? '')}</textarea></div>`;
    if (tip === 'dosya') return `<div class="${sinif}">${lbl}<input class="form-control" type="file" id="${id}" data-k="${k}" data-tip="dosya" accept=".pdf,.jpg,.jpeg,.png" multiple><ul class="ky-dosya-liste mb-0 ps-3" id="kyDosyaListe"></ul></div>`;
    return `<div class="${sinif}">${lbl}<input class="form-control" id="${id}" data-k="${k}" value="${esc(v ?? '')}" maxlength="160"></div>`;
  }
  /** kabın data-k alanlarını nesneye okur (teminat satırı .kyt ve dosya alanı hariç) */
  function oku(kap) {
    const d = {};
    $$('[data-k]', kap).forEach(el => { if (el.classList.contains('kyt') || el.dataset.tip === 'dosya') return; const k = el.dataset.k, tip = el.dataset.tip; d[k] = tip === 'onay' ? el.checked : tip === 'para' || tip === 'oran' ? SAYI(el.value, tip === 'oran' ? 4 : 2) : tip === 'tam' || tip === 'secim' ? Math.max(0, parseInt(el.value, 10) || 0) : (el.value ?? '').trim(); });
    return d;
  }

  // ---------- kredi formu (modal)
  function hesapla(d) {
    const n = Math.max(0, d.yeniVade - Math.max(0, d.odemesizAy)), r = d.yeniFaizOrani / 100 / 12 * (1 + d.bsmvOrani / 100);
    const pesinat = n0(d.pesinat);
    if (n <= 0 || d.yeniAnapara <= 0) return { n: 0, taksit: 0, uygulanan: 0, toplam: 0, faiz: 0, anapara: 0, pesinat, genel: pesinat };
    const p = Math.round(d.yeniAnapara * Math.pow(1 + r, Math.max(0, d.odemesizAy)) * 100) / 100;
    const taksit = r <= 0 ? Math.round(p / n * 100) / 100 : Math.round(p * r / (1 - Math.pow(1 + r, -n)) * 100) / 100;
    const uyg = d.yeniTaksit > 0 ? d.yeniTaksit : taksit; let kalan = p, toplam = 0, faizT = 0, anaT = 0;
    for (let i = 1; i <= n && kalan > 0; i++) { const faiz = Math.round(kalan * r * 100) / 100; let ana = i === n ? kalan : Math.max(0, Math.round((uyg - faiz) * 100) / 100); if (ana > kalan) ana = kalan; const tutar = i === n || ana === kalan ? Math.round((ana + faiz) * 100) / 100 : uyg; kalan = Math.max(0, Math.round((kalan - ana) * 100) / 100); toplam += tutar; faizT += faiz; anaT += ana; }
    return { n, taksit, uygulanan: uyg, toplam, faiz: faizT, anapara: anaT, pesinat, genel: toplam + pesinat };   // genel = peşinat + taksitler (toplam geri ödeme)
  }
  const alanlar = {
    kimlik: [['sirket', 'Şirket', 'select', 'sirketler'], ['banka', 'Banka', 'liste', 'bankalar'], ['sube', 'Şube', 'text'], ['krediNo', 'Kredi No', 'text'], ['krediTuru', 'Kredi Türü', 'select', 'krediTurleri'], ['durum', 'Yapılandırma Durumu', 'select', 'durumlar'], ['protokolNo', 'Protokol / Sözleşme No', 'text'], ['yapilandirmaTarihi', 'Yapılandırma Tarihi', 'date']],
    oncesi: [['kullanimTarihi', 'Kullanım Tarihi', 'date'], ['anapara', 'Anapara ₺', 'para'], ['faizOrani', 'Yıllık Faiz %', 'oran'], ['vade', 'Vade (Ay)', 'tam'], ['taksit', 'Aylık Taksit ₺', 'para'], ['kalanAnapara', 'Kalan Anapara ₺', 'para'], ['kalanVade', 'Kalan Vade (Ay)', 'tam'], ['sonOdemeTarihi', 'Son Ödenen Taksit Tarihi', 'date'], ['gecikenTaksit', 'Geciken Taksit (Adet)', 'tam'], ['gecikenTutar', 'Geciken Taksit Tutarı ₺', 'para'], ['gecikmeFaizi', 'Gecikme Faizi ₺', 'para'], ['masraf', 'Masraf / Komisyon ₺', 'para'], ['oncekiDurum', 'Önceki Durum', 'select', 'oncekiDurumlar']],
    sonrasi: [['yeniAnapara', 'Yeni Anapara ₺ (Taksitlendirilen, Peşinat Hariç)', 'para'], ['yeniFaizOrani', 'Yeni Yıllık Faiz %', 'oran'], ['bsmvOrani', 'BSMV %', 'oran'], ['yeniVade', 'Yeni Vade (Ay)', 'tam'], ['odemesizAy', 'Ödemesiz Dönem (Ay)', 'tam'], ['yeniTaksit', 'Banka Taksit Tutarı ₺ (Boş = Hesaplanır)', 'para'], ['ilkTaksitTarihi', 'İlk Taksit Tarihi', 'date'], ['odenenTaksit', 'Ödenen Taksit (Yeni Plan)', 'tam'],
      ['pesinat', 'Peşinat ₺ (Protokolde Peşin Ödenen)', 'para'], ['pesinatTarihi', 'Peşinat Tarihi (Boş = Yapılandırma Tarihi)', 'date'], ['pesinatOdendi', 'Peşinat Ödendi', 'onay']]   // peşinat (9 Kas 2026): taksitlerin dışında, Kasa Nakit Akışı'na ayrı satır
  };
  // teminat satırı: tür, açıklama, tapu / kimlik, derece, malik, tutar, ekspertiz + (9 Kas 2026) tesis tarihi, durum, fek tarihi, not
  const teminatSatir = (t, i) => `<tr data-i="${i}"><td><select class="form-select kyt" data-k="tur">${V.teminatTurleri.map(o => `<option ${o === (t.tur || 'İpotek') ? 'selected' : ''}>${esc(o)}</option>`).join('')}</select></td><td><input class="form-control kyt" data-k="aciklama" value="${esc(t.aciklama || '')}" placeholder="gayrimenkul / araç / kefil tanımı" maxlength="160" style="min-width:150px"></td><td><input class="form-control kyt" data-k="tapuBilgisi" value="${esc(t.tapuBilgisi || '')}" placeholder="il / ilçe / ada / parsel · plaka · TCKN" maxlength="160" style="min-width:150px"></td><td><input class="form-control kyt" data-k="derece" value="${esc(t.derece || '')}" placeholder="1. derece" maxlength="40" style="width:80px"></td><td><input class="form-control kyt" data-k="malik" value="${esc(t.malik || '')}" placeholder="malik / kefil" maxlength="120" style="min-width:110px"></td><td><input class="form-control kyt" type="number" step="0.01" min="0" data-k="tutar" value="${t.tutar ? Number(t.tutar) : ''}" style="width:120px"></td><td><input class="form-control kyt" type="number" step="0.01" min="0" data-k="ekspertiz" value="${t.ekspertiz ? Number(t.ekspertiz) : ''}" style="width:120px"></td><td><input class="form-control kyt" type="date" data-k="tesisTarihi" value="${esc(t.tesisTarihi || '')}" style="width:128px"></td><td><select class="form-select kyt" data-k="durum" style="width:122px">${V.teminatDurumlari.map(o => `<option ${o === (t.durum || 'Aktif') ? 'selected' : ''}>${esc(o)}</option>`).join('')}</select></td><td><input class="form-control kyt" type="date" data-k="fekTarihi" value="${esc(t.fekTarihi || '')}" style="width:128px"></td><td><input class="form-control kyt" data-k="not" value="${esc(t.not || '')}" maxlength="160" style="min-width:100px"></td><td><button type="button" class="btn btn-link btn-sm p-0 text-danger kyTeminatSil" title="Teminatı kaldır"><i class="fas fa-times"></i></button></td></tr>`;
  let duzenlenen = null;
  function formAc(k) {
    duzenlenen = k; const d = k || { sirket: V.sirketler[0]?.ad, krediTuru: 'Taksitli Ticari Kredi', durum: 'Yapılandırıldı', oncekiDurum: 'Gecikmede', bsmvOrani: 5, nakitAkisaAktar: true, teminatlar: [] };
    $('#kyMBaslik').textContent = k ? `${k.no} · ${k.banka} · ${k.krediNo || k.krediTuru}` : 'Yeni Kredi Yapılandırması';
    const belgeler = k ? V.belgeler.filter(b => b.krediId === k.id) : [];
    $('#kyMGovde').innerHTML = `
      <div class="ky-bolum"><div class="b"><span><i class="fas fa-id-card me-1"></i>Kredi Kimliği</span><span style="text-transform:none;font-weight:600;color:#64748b">Şirket: Kasa Nakit Akışı şirketleri · banka listeden ya da elle</span></div><div class="g">${alanlar.kimlik.map(a => alanHtml(a, d[a[0]])).join('')}</div></div>
      <div class="ky-bolum oncesi"><div class="b"><span><i class="fas fa-clock-rotate-left me-1"></i>Yapılandırma Öncesi</span><span id="kyOnceBorc" style="text-transform:none;font-weight:700"></span></div><div class="g">${alanlar.oncesi.map(a => alanHtml(a, d[a[0]])).join('')}</div></div>
      <div class="ky-bolum sonrasi"><div class="b"><span><i class="fas fa-file-signature me-1"></i>Yapılandırma Sonrası</span><button type="button" class="btn btn-light btn-sm py-0 border" id="kyAnaparaOner" style="font-size:.6rem;text-transform:none" title="Yeni anapara = kalan anapara + geciken taksit tutarı + gecikme faizi + masraf − peşinat"><i class="fas fa-wand-magic-sparkles me-1"></i>Yeni Anaparayı Öner</button></div><div class="g">${alanlar.sonrasi.map(a => alanHtml(a, d[a[0]])).join('')}</div>
        <div class="ky-onizleme" id="kyOnizleme"></div></div>
      <div class="ky-bolum teminat"><div class="b"><span><i class="fas fa-house-lock me-1"></i>Teminatlar · Rehin · İpotek</span><button type="button" class="btn btn-light btn-sm py-0 border" id="kyTeminatEkle" style="font-size:.6rem;text-transform:none"><i class="fas fa-plus me-1"></i>Teminat Ekle</button></div>
        <div style="padding:6px 10px;overflow-x:auto"><table class="ky-teminat"><thead><tr><th>Tür</th><th>Açıklama</th><th>Tapu / Kimlik Bilgisi</th><th>Derece</th><th>Malik / Kefil</th><th>Teminat Tutarı ₺</th><th>Ekspertiz ₺</th><th>Tesis Tarihi</th><th>Durum</th><th>Fek Tarihi</th><th>Not</th><th></th></tr></thead><tbody id="kyTeminatlar">${(d.teminatlar || []).map(teminatSatir).join('')}</tbody></table><div class="text-muted" style="font-size:.62rem;margin-top:4px">İpotek: tapu bilgisi, derece, ipotek tutarı, ekspertiz değeri ve malik · Kefalet: kefil adı · Rehin / teminat mektubu / çek-senet / nakit blokaj. Durum: Aktif → kredi kapanınca Fek Bekliyor → fek yazısı alınınca Fek Edildi (Rehin · İpotek Takibi sekmesi). Teminat çekleri ayrı sekmede izlenir.</div></div></div>
      <div class="ky-bolum"><div class="b"><span><i class="fas fa-note-sticky me-1"></i>Not ve Akış</span>${k ? `<span style="text-transform:none;font-weight:600;color:#64748b"><i class="fas fa-paperclip me-1"></i>${belgeler.length} belge${belgeler.length ? ': ' + esc(belgeler.slice(0, 4).map(b => b.tur).join(', ')) + (belgeler.length > 4 ? ' …' : '') : ''} · <a href="#" class="ky-belgeler" data-id="${k.id}" data-bs-dismiss="modal">Belgeler sekmesi</a> · <a href="#" class="ky-belge-yukle" data-kredi="${k.id}" data-bs-dismiss="modal">Belge yükle</a></span>` : ''}</div><div class="g"><div class="ky-f g2"><label for="kyf_not">Not</label><textarea class="form-control" id="kyf_not" data-k="not" rows="2" maxlength="600">${esc(d.not || '')}</textarea></div>
        <div class="ky-f g2"><label>Kasa Nakit Akışı · Planlı Ödemeler</label><label class="d-flex align-items-center gap-2" style="font-size:.72rem;text-transform:none;font-weight:600;color:#0f172a;margin-top:4px"><input type="checkbox" id="kyf_aktar" ${d.nakitAkisaAktar ? 'checked' : ''}> Taksitleri Kasa Nakit Akışı'na (Havale / EFT satırları) ve Planlı Düzenli Ödemeler'e aktar</label><div class="text-muted" style="font-size:.62rem">Her taksit planlanan tarihli satır olur; yapılandırma değişince satırlar güncellenir, ödenmiş satıra dokunulmaz. "Teklif Aşamasında" ve "İptal" durumunda aktarılmaz.</div></div></div></div>`;
    $('#kyMSil').style.display = k ? '' : 'none'; $('#kyMOzet').textContent = k ? `${k.no} · oluşturma ${tarih(k.olusturma)} ${k.olusturan ? '(' + k.olusturan + ')' : ''}${k.guncelleme ? ' · güncelleme ' + k.guncelleme + ' ' + (k.guncelleyen || '') : ''}` : 'Yeni kayıt: kaydedince KRD numarası verilir.';
    $('#kyMGovde').addEventListener('input', onizle); $('#kyMGovde').addEventListener('change', onizle);
    $('#kyTeminatEkle').addEventListener('click', () => { const tb = $('#kyTeminatlar'); tb.insertAdjacentHTML('beforeend', teminatSatir({ tur: 'İpotek' }, tb.children.length)); onizle(); });
    $('#kyTeminatlar').addEventListener('click', e => { const b = e.target.closest('.kyTeminatSil'); if (b) { b.closest('tr').remove(); onizle(); } });
    $('#kyAnaparaOner').addEventListener('click', () => { const o = formOku(); $('#kyf_yeniAnapara').value = Math.max(0, Math.round((o.kalanAnapara + o.gecikenTutar + o.gecikmeFaizi + o.masraf - n0(o.pesinat)) * 100) / 100); onizle(); });   // peşinat düşülür
    onizle();
    bootstrap.Modal.getOrCreateInstance($('#kyModal')).show();
  }
  function formOku() {
    const d = Object.assign(oku($('#kyMGovde')), { id: duzenlenen ? duzenlenen.id : 0, teminatlar: [], nakitAkisaAktar: $('#kyf_aktar').checked });
    $$('#kyTeminatlar tr').forEach(tr => { const t = {}; $$('.kyt', tr).forEach(el => { t[el.dataset.k] = el.type === 'number' ? SAYI(el.value) : el.type === 'date' ? (el.value || null) : el.value.trim(); }); d.teminatlar.push(t); });   // boş tarih null (sunucu DateTime?)
    return d;
  }
  function onizle() {
    const d = formOku(), h = hesapla(d), onceBorc = d.kalanAnapara + d.gecikenTutar + d.gecikmeFaizi + d.masraf, teminat = d.teminatlar.filter(t => aktifTeminat(t.durum)).reduce((s, t) => s + n0(t.tutar), 0);
    $('#kyOnceBorc').textContent = `Toplam Borç: ${tl(onceBorc)}`;
    const ilk = d.ilkTaksitTarihi || (d.yapilandirmaTarihi ? (() => { const t = new Date(d.yapilandirmaTarihi); t.setMonth(t.getMonth() + 1 + (d.odemesizAy || 0)); return t.toISOString().slice(0, 10); })() : '');
    const son = ilk && h.n ? (() => { const t = new Date(ilk); t.setMonth(t.getMonth() + h.n - 1); return t.toISOString().slice(0, 10); })() : '';
    $('#kyOnizleme').innerHTML = [
      ['Taksit Sayısı', h.n ? `${h.n}` : '—', d.odemesizAy ? `${d.odemesizAy} ay ödemesiz` : 'ödemesiz dönem yok'],
      ['Hesaplanan Taksit', h.taksit ? tl(h.taksit) : '—', `aylık oran ${(d.yeniFaizOrani / 12 * (1 + d.bsmvOrani / 100)).toLocaleString('tr-TR', { maximumFractionDigits: 4 })} % (BSMV dahil)`],
      ['Uygulanacak Taksit', h.uygulanan ? tl(h.uygulanan) : '—', d.yeniTaksit > 0 ? 'banka protokolü tutarı' : 'hesaplanan tutar'],
      ['Aylık Fark', h.uygulanan ? `${h.uygulanan - d.taksit >= 0 ? '+' : ''}${tl(h.uygulanan - d.taksit)}` : '—', `eski taksit ${tl(d.taksit)}`],
      ['Toplam Geri Ödeme', h.genel ? tl(h.genel) : '—', `${h.pesinat ? 'peşinat ' + tl(h.pesinat) + ' · ' : ''}taksitler ${tl(h.toplam)} · faiz ${tl(h.faiz)} · anapara ${tl(h.anapara)}`],
      ['Borç Farkı · Teminat', h.genel ? `${h.genel - onceBorc >= 0 ? '+' : ''}${tl(h.genel - onceBorc)}` : '—', `aktif teminat ${tl(teminat)} (${d.yeniAnapara > 0 ? yuzde(teminat / d.yeniAnapara * 100) : '—'})${ilk ? ' · ' + tarih(ilk) + (son ? ' – ' + tarih(son) : '') : ''}`]
    ].map(x => `<div><div class="l">${esc(x[0])}</div><div class="v">${esc(x[1])}</div><div class="s">${esc(x[2])}</div></div>`).join('');
  }
  $('#kyMKaydet').addEventListener('click', async () => {
    const d = formOku(); if (!d.banka) { mesaj('warning', 'Banka girilmelidir.'); return; }
    const b = $('#kyMKaydet'); b.disabled = true;
    const j = await post('/KrediYapilandirma/Kaydet', d); b.disabled = false;
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    bootstrap.Modal.getOrCreateInstance($('#kyModal')).hide(); veri(j.veri); mesaj('success', '<i class="fas fa-check me-1"></i>' + esc(j.message));
  });
  $('#kyMSil').addEventListener('click', async () => {
    if (!duzenlenen) return; if (!window.confirm(`${duzenlenen.no} · ${duzenlenen.banka} silinsin mi? Kasa Nakit Akışı'ndaki ödenmemiş taksit satırları kaldırılır, düzenli ödeme pasife alınır.`)) return;
    const j = await post('/KrediYapilandirma/Sil', { id: duzenlenen.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    bootstrap.Modal.getOrCreateInstance($('#kyModal')).hide(); veri(j.veri); mesaj('success', esc(j.message));
  });

  // ---------- rehin / ipotek kaydı · teminat çeki · belge yükleme (ortak alt modal; 9 Kas 2026)
  let alt = null;   // { tip: 'rehin' | 'cek' | 'belge', kayit }
  const krediSecenek = () => V.krediler.map(k => ({ v: k.id, a: `${k.no} · ${k.banka} · ${k.krediNo || k.krediTuru}` }));
  const alanlarAlt = {
    rehin: [['sirket', 'Şirket', 'select', 'sirketler'], ['banka', 'Banka', 'liste', 'bankalar'], ['krediId', 'Yapılandırma Kaydı (Varsa)', 'secim', krediSecenek], ['krediRef', 'Eski Kredi / Sözleşme Referansı', 'text'],
      ['tur', 'Teminat Türü', 'select', 'teminatTurleri'], ['aciklama', 'Açıklama', 'text'], ['tapuBilgisi', 'Tapu / Kimlik Bilgisi', 'text'], ['derece', 'Derece', 'text'],
      ['malik', 'Malik / Kefil', 'text'], ['tutar', 'Teminat Tutarı ₺', 'para'], ['ekspertiz', 'Ekspertiz Değeri ₺', 'para'], ['tesisTarihi', 'Tesis Tarihi', 'date'],
      ['krediKapanis', 'Kredi Kapanış Tarihi', 'date'], ['durum', 'Durum', 'select', 'teminatDurumlari'], ['fekTarihi', 'Fek Tarihi', 'date'], ['not', 'Not', 'text']],
    cek: [['sirket', 'Şirket', 'select', 'sirketler'], ['banka', 'Teminat Verilen Banka', 'liste', 'bankalar'], ['krediId', 'Bağlı Kredi (Varsa)', 'secim', krediSecenek], ['krediRef', 'Kredi / GKS Referansı', 'text'],
      ['belge', 'Tür', 'select', ['Çek', 'Senet']], ['cekNo', 'Çek / Senet No', 'text'], ['kesideci', 'Keşideci', 'text'], ['cekBankasi', 'Çekin Bankası · Şube', 'text'],
      ['vade', 'Vade', 'date'], ['tutar', 'Tutar', 'para'], ['pb', 'Para Birimi', 'select', ['TRY', 'USD', 'EUR', 'GBP']], ['verilisTarihi', 'Bankaya Veriliş Tarihi', 'date'],
      ['durum', 'Durum', 'select', 'cekDurumlari'], ['iadeTarihi', 'İade / Tahsil Tarihi', 'date'], ['not', 'Not', 'text', null, 'g2']],
    belge: [['tur', 'Belge Türü', 'select', 'belgeTurleri'], ['baslik', 'Başlık (Boş = Dosya Adı)', 'text'], ['krediId', 'Bağlı Kredi', 'secim', krediSecenek], ['tarih', 'Belge Tarihi', 'date'],
      ['teminatId', 'Bağlı Rehin / İpotek Kaydı', 'secim', () => V.teminatlar.map(t => ({ v: t.id, a: `${t.banka} · ${t.tur} · ${t.aciklama || t.tapuBilgisi}` }))], ['cekId', 'Bağlı Teminat Çeki', 'secim', () => V.cekler.map(c => ({ v: c.id, a: `${c.cekNo} · ${c.kesideci} · ${tutarPb(c.tutar, c.pb)}` }))],
      ['sirket', 'Şirket (Boş = Kredinin Şirketi)', 'selectBos', 'sirketler'], ['banka', 'Banka (Boş = Kredinin Bankası)', 'liste', 'bankalar'],
      ['aciklama', 'Açıklama', 'text', null, 'g2'], ['dosya', 'Dosya (PDF / JPG / PNG · En Çok 20 MB · Birden Çok Seçilebilir)', 'dosya', null, 'g2']]
  };
  const ALT = {
    rehin: { ikon: 'fa-house-lock', sinif: 'teminat', yeni: 'Yeni Rehin / İpotek Kaydı (Eski Kredi)', ad: k => 'Rehin / İpotek Kaydı · ' + k.banka + ' · ' + k.tur, aciklama: 'Yapılandırma kaydı olmayan (eski / kapanmış) kredilerin ipotek ve rehinleri için. Yapılandırılan kredinin teminatını kredi kartındaki Teminatlar bölümüne girin. Kredi kapanınca durumu Fek Bekliyor, fek yazısı alınınca Fek Edildi yapın.', url: '/KrediYapilandirma/TeminatKaydet', silUrl: '/KrediYapilandirma/TeminatSil' },
    cek: { ikon: 'fa-money-check-dollar', sinif: 'cek', yeni: 'Yeni Teminat Çeki', ad: k => 'Teminat Çeki · ' + k.cekNo + ' · ' + k.banka, aciklama: 'Bankaya kredi ya da GKS teminatı olarak verilen müşteri çeki / senedi. Vadesinde banka tahsil ederse Tahsil Edildi, kredi kapanınca geri alınırsa İade Alındı, karşılıksız çıkarsa Karşılıksız işaretleyin.', url: '/KrediYapilandirma/CekKaydet', silUrl: '/KrediYapilandirma/CekSil' },
    belge: { ikon: 'fa-file-arrow-up', sinif: 'belge', yeni: 'Belge Yükle · Sözleşme / Protokol / GKS / İpotek / Çek', aciklama: 'Taranmış PDF ya da fotoğraf (JPG / PNG, en çok 20 MB). Krediye, rehin / ipotek kaydına ya da teminat çekine bağlanabilir; GKS gibi genel belgelerde yalnız şirket ve banka seçin. Birden çok dosya seçilirse her biri aynı bilgilerle ayrı belge olur.' }
  };
  function altAc(tip, kayit, on) {
    alt = { tip, kayit: kayit || null }; const T = ALT[tip];
    const varsayilan = tip === 'rehin' ? { sirket: V.sirketler[0]?.ad, tur: 'İpotek', durum: 'Aktif' } : tip === 'cek' ? { sirket: V.sirketler[0]?.ad, belge: 'Çek', pb: 'TRY', durum: 'Teminatta' } : { tur: 'Kredi Sözleşmesi' };
    const d = Object.assign(varsayilan, Object.fromEntries(Object.entries(on || {}).filter(([, v]) => v !== undefined && v !== null)), kayit || {});
    if (tip !== 'belge' && !kayit && d.krediId) { const k = krediSec(d.krediId); if (k) { d.sirket = k.sirket; d.banka = d.banka || k.banka; } }
    const baslik = kayit ? T.ad(kayit) : T.yeni;
    $('#kyAMIkon').className = 'fas ' + T.ikon + ' me-2 text-primary'; $('#kyAMBaslik').textContent = baslik;
    $('#kyAMGovde').innerHTML = `<div class="ky-bolum ${T.sinif}"><div class="b"><span><i class="fas ${T.ikon} me-1"></i>${esc(baslik)}</span></div><div class="g">${alanlarAlt[tip].map(a => alanHtml(a, d[a[0]])).join('')}</div></div><div class="ky-not">${esc(T.aciklama)}</div>`;
    $('#kyAMSil').style.display = kayit && tip !== 'belge' ? '' : 'none';
    $('#kyAMKaydet').innerHTML = tip === 'belge' ? '<i class="fas fa-file-arrow-up me-1"></i>Yükle' : '<i class="fas fa-save me-1"></i>Kaydet';
    $('#kyAMOzet').textContent = kayit ? `kayıt ${kayit.id} · oluşturma ${tarih(kayit.olusturma)} ${kayit.olusturan ? '(' + kayit.olusturan + ')' : ''}` : tip === 'belge' ? 'Dosya sunucuda App_Data/kredi-belgeler altında saklanır; listeden açılır, indirilir, silinir.' : 'Yeni kayıt.';
    const fi = $('#kyf_dosya'); if (fi) fi.addEventListener('change', () => { $('#kyDosyaListe').innerHTML = Array.from(fi.files).map(f => `<li>${esc(f.name)} · ${boyutH(f.size)}${f.size > 20 * 1048576 ? ' <b class="text-danger">20 MB sınırını aşıyor</b>' : ''}</li>`).join(''); });
    bootstrap.Modal.getOrCreateInstance($('#kyAltModal')).show();
  }
  async function altSil(tip, kayit, modalKapat) {
    if (!kayit) return; const T = ALT[tip];
    const bagli = V.belgeler.filter(b => tip === 'rehin' ? b.teminatId === kayit.id : b.cekId === kayit.id).length;
    if (!window.confirm(`${T.ad(kayit)} silinsin mi?${bagli ? ` Bağlı ${bagli} belge kalır, bağı kalkar.` : ''}`)) return;
    const j = await post(T.silUrl, { id: kayit.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; }
    if (modalKapat) bootstrap.Modal.getOrCreateInstance($('#kyAltModal')).hide(); veri(j.veri); mesaj('success', esc(j.message));
  }
  $('#kyAMKaydet').addEventListener('click', async () => {
    if (!alt) return; const d = oku($('#kyAMGovde')), b = $('#kyAMKaydet');
    if (alt.tip === 'belge') {
      const fi = $('#kyf_dosya'), dosyalar = Array.from((fi && fi.files) || []); if (!dosyalar.length) { mesaj('warning', 'Dosya seçilmedi (PDF / JPG / PNG).'); return; }
      if (dosyalar.some(f => f.size > 20 * 1048576)) { mesaj('warning', 'Dosya 20 MB sınırını aşıyor.'); return; }
      b.disabled = true; let son = null, hata = null, n = 0;
      for (const f of dosyalar) {
        const fd = new FormData(); fd.append('dosya', f); Object.entries(d).forEach(([k, v]) => fd.append(k, v == null ? '' : String(v))); if (dosyalar.length > 1 && d.baslik) fd.set('baslik', `${d.baslik} · ${++n}`);
        const j = await fetch('/KrediYapilandirma/BelgeYukle', { method: 'POST', body: fd }).then(r => r.json()).catch(() => ({ success: false, message: 'Ağ hatası.' }));
        if (!j.success) { hata = j.message; break; } son = j;
      }
      b.disabled = false; if (son) veri(son.veri); if (hata) { mesaj('danger', esc(hata)); return; }
      bootstrap.Modal.getOrCreateInstance($('#kyAltModal')).hide(); mesaj('success', '<i class="fas fa-check me-1"></i>' + (dosyalar.length > 1 ? `${dosyalar.length} belge yüklendi.` : esc(son.message))); return;
    }
    if (!d.banka) { mesaj('warning', 'Banka girilmelidir.'); return; }
    d.id = alt.kayit ? alt.kayit.id : 0; b.disabled = true;
    const j = await post(ALT[alt.tip].url, d); b.disabled = false;
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    bootstrap.Modal.getOrCreateInstance($('#kyAltModal')).hide(); veri(j.veri); mesaj('success', '<i class="fas fa-check me-1"></i>' + esc(j.message));
  });
  $('#kyAMSil').addEventListener('click', () => { if (alt && alt.kayit) altSil(alt.tip, alt.kayit, true); });

  // ---------- başlangıç
  sekme(kok.dataset.sekme || 'krediler'); yukle();
})();
