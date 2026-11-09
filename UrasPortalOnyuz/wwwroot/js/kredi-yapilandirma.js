// ============================================================
// KREDİ YAPILANDIRMA RAPORU — ekran betiği (Views/KrediYapilandirma/Index.cshtml; veri /KrediYapilandirma/Veri; kaynak Data/KrediYapilandirmaOrnek.cs)
// Sekmeler: Krediler (öncesi ↔ sonrası karşılaştırması) · Yapılandırma Öncesi · Yapılandırma Sonrası · Teminatlar · Taksit Takvimi · Aylık Nakit Yükü.
// Kredi Ekle / Düzenle modalı: kimlik, yapılandırma öncesi, sonrası (yeni taksit boşsa anüite ile hesaplanır; canlı önizleme sunucu formülüyle aynı), teminat satırları, not, nakit akışa aktarım.
// Kaydet → sunucu taksitleri Kasa Nakit Akışı'na (Havale / EFT) ve Planlı Düzenli Ödemeler'e yazar; paket (veri) geri döner, tüm sekmeler yeniden çizilir.
// Yazım: liste değerleri ve etiketler Title Case; tutarlar ₺; tablo hizası tablo-hizala.js; Excel ortak TabloExcel.
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
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 10000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }).then(r => r.json()).catch(() => ({ success: false, message: 'Ağ hatası.' }));

  let V = null, F = { sirket: '', banka: '', durum: '', ara: '' }, takvimKredi = '';
  const aktifSekme = () => ($('#kySekmeler button.aktif') || {}).dataset?.p || 'krediler';

  // ---------- yükleme
  async function yukle() { let j; try { j = await fetch('/KrediYapilandirma/Veri').then(r => r.json()); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; } veri(j); }
  function veri(j) { if (!j || !j.success) { mesaj('danger', 'Veri yüklenemedi.'); return; } V = j; filtreDoldur(); $('#kyKalici').style.display = V.kalici ? '' : 'none'; ciz(); }
  function secenekler(sel, degerler, mevcut) { const v = mevcut ?? sel.value; sel.innerHTML = '<option value="">Tümü</option>' + degerler.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); sel.value = degerler.includes(v) ? v : ''; }
  function filtreDoldur() {
    secenekler($('#kyS'), [...new Set(V.krediler.map(k => k.sirket))].sort((a, b) => a.localeCompare(b, 'tr')), F.sirket); F.sirket = $('#kyS').value;
    secenekler($('#kyB'), [...new Set(V.krediler.map(k => k.banka))].sort((a, b) => a.localeCompare(b, 'tr')), F.banka); F.banka = $('#kyB').value;
    secenekler($('#kyD'), V.durumlar, F.durum); F.durum = $('#kyD').value;
    const tk = $('#kyTakvimKredi'); if (tk) { tk.innerHTML = '<option value="">Tüm Krediler</option>' + V.krediler.map(k => `<option value="${k.id}">${esc(k.no)} · ${esc(k.banka)} · ${esc(k.krediNo || k.krediTuru)}</option>`).join(''); tk.value = V.krediler.some(k => String(k.id) === takvimKredi) ? takvimKredi : ''; takvimKredi = tk.value; }
  }
  const aramaMetni = k => [k.no, k.sirket, k.banka, k.sube, k.krediNo, k.krediTuru, k.protokolNo, k.durum, k.oncekiDurum, k.not, ...(k.teminatlar || []).map(t => [t.tur, t.aciklama, t.tapuBilgisi, t.malik, t.not].join(' '))].join(' ').toLocaleLowerCase('tr-TR');
  function liste() { const a = F.ara.trim().toLocaleLowerCase('tr-TR'); return V.krediler.filter(k => (!F.sirket || k.sirket === F.sirket) && (!F.banka || k.banka === F.banka) && (!F.durum || k.durum === F.durum) && (!a || aramaMetni(k).includes(a))); }
  const top = (l, f) => l.reduce((s, k) => s + n0(typeof f === 'function' ? f(k) : k[f]), 0);

  // ---------- çizim
  function ciz() {
    if (!V) return;
    const L = liste();
    $('#kyKapsam').textContent = `${L.length} / ${V.krediler.length} kredi · ${[...new Set(L.map(k => k.sirket))].length} şirket · ${[...new Set(L.map(k => k.banka))].length} banka · ${V.bugun ? tarih(V.bugun) : ''}`;
    kpi(L);
    const p = aktifSekme(); (CIZ[p] || cizKrediler)(L);
  }
  function kpi(L) {
    const aktif = L.filter(k => k.durum !== 'İptal'), onceTaksit = top(aktif, 'taksit'), sonraTaksit = top(aktif, 'uygulananTaksit');
    const geciken = top(aktif, 'gecikenTutar') + top(aktif, 'gecikmeFaizi'), kasaSatir = top(L, k => k.kasaOzet.satir), kasaOdendi = top(L, k => k.kasaOzet.odendi);
    const K = [
      ['#1e40af', 'Yapılandırılan Kredi', `${aktif.length}`, `${[...new Set(aktif.map(k => k.banka))].length} banka · ${[...new Set(aktif.map(k => k.sirket))].length} şirket · ${L.filter(k => k.durum === 'İptal').length} iptal`],
      ['#b45309', 'Öncesi Toplam Borç', kisa(top(aktif, 'oncesiToplamBorc')), `kalan anapara ${kisa(top(aktif, 'kalanAnapara'))} · geciken + faiz + masraf ${kisa(geciken + top(aktif, 'masraf'))}`],
      ['#0f766e', 'Sonrası Toplam Geri Ödeme', kisa(top(aktif, 'toplamGeriOdeme')), `peşinat ${kisa(top(aktif, 'pesinat'))} · yeni anapara ${kisa(top(aktif, 'yeniAnapara'))} · ${top(aktif, 'taksitSayisi')} taksit`],
      ['#7c3aed', 'Aylık Taksit Yükü', `${kisa(onceTaksit)} → ${kisa(sonraTaksit)}`, `fark ${sonraTaksit - onceTaksit >= 0 ? '+' : ''}${kisa(sonraTaksit - onceTaksit)} / ay`],
      ['#dc2626', 'Toplam Faiz (Sonrası)', kisa(top(aktif, 'toplamFaiz')), `BSMV dahil · anapara ${kisa(top(aktif, 'toplamAnapara'))}`],
      ['#9d174d', 'Geciken Taksit (Öncesi)', `${top(aktif, 'gecikenTaksit')} adet`, `${kisa(top(aktif, 'gecikenTutar'))} · gecikme faizi ${kisa(top(aktif, 'gecikmeFaizi'))}`],
      ['#0369a1', 'Teminat · İpotek', kisa(top(aktif, 'teminatToplam')), `ipotek ${kisa(top(aktif, 'ipotekToplam'))} · ekspertiz ${kisa(top(aktif, 'ekspertizToplam'))}`],
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
  const islemH = k => `<td><button type="button" class="btn btn-light border ky-duzenle" data-id="${k.id}" title="Düzenle"><i class="fas fa-pen"></i></button> <button type="button" class="btn btn-light border ky-takvim" data-id="${k.id}" title="Taksit takvimi"><i class="fas fa-calendar-days"></i></button> <button type="button" class="btn btn-light border ky-aktar" data-id="${k.id}" data-aktar="${k.nakitAkisaAktar ? '0' : '1'}" title="${k.nakitAkisaAktar ? 'Nakit akışa aktarımı kapat (ödenmemiş taksit satırları kaldırılır)' : 'Taksitleri Kasa Nakit Akışı ve Planlı Ödemeler\'e aktar'}"><i class="fas ${k.nakitAkisaAktar ? 'fa-link-slash' : 'fa-paper-plane'}"></i></button></td>`;
  const kasaH = k => `<td>${k.nakitAkisaAktar ? (k.kasaOzet.satir ? `<a href="${esc(k.kasaOzet.link)}" target="_blank" rel="noopener" title="Kasa Nakit Akışı › Havale / EFT">${k.kasaOzet.satir} satır</a><small>${k.kasaOzet.odendi} ödendi · ${k.kasaOzet.planli} planlı${k.duzenli ? ' · ' + esc(k.duzenli.no) : ''}</small>` : '<span class="ky-rz gri">Satır yok</span>') : '<span class="ky-rz gri">Aktarım Kapalı</span>'}</td>`;

  function cizKrediler(L) {
    const grup = [['Kredi', 5], ['Yapılandırma Öncesi', 4], ['Yapılandırma Sonrası', 9], ['Teminat', 1], ['Akış', 2]];
    const bas = ['Kredi No', 'Şirket', 'Banka · Şube', 'Kredi No · Tür', 'Durum', 'Kalan Anapara', 'Geciken Taksit', 'Eski Taksit', 'Toplam Borç', 'Peşinat', 'Yeni Anapara', 'Faiz % (Eski → Yeni)', 'Vade Ay (Kalan → Yeni)', 'Yeni Taksit', 'Aylık Fark', 'Toplam Geri Ödeme', 'Toplam Faiz', 'Borç Farkı', 'Teminat · İpotek', 'Kasa Nakit Akışı', 'İşlem'];
    const pesinatH = k => k.pesinat > 0 ? `${tl(k.pesinat)}<small>${tarih(k.pesinatTarihi)} · ${k.pesinatOdendi ? 'ödendi' : 'ödenecek'}</small>` : bos;
    const rows = L.map(k => `<tr class="${k.ornek ? 'ornek' : ''}" data-id="${k.id}">${noH(k)}<td>${esc(k.sirket)}</td><td class="ad">${esc(k.banka)}<small>${esc(k.sube || '')}</small></td><td>${esc(k.krediNo || '—')}<small>${esc(k.krediTuru)}</small></td><td>${durumRz(k.durum)}<small>${k.yapilandirmaTarihi ? 'yapılandırma ' + tarih(k.yapilandirmaTarihi) : ''}</small></td>
      <td class="para">${tl(k.kalanAnapara)}<small>${k.kalanVade ? k.kalanVade + ' ay kaldı' : ''}</small></td><td class="para">${k.gecikenTaksit ? `${k.gecikenTaksit} adet<small>${tl(k.gecikenTutar)}</small>` : bos}</td><td class="para">${tl(k.taksit)}<small>${yuzde(k.faizOrani)}</small></td><td class="para">${tl(k.oncesiToplamBorc)}<small>gecikme faizi ${kisa(k.gecikmeFaizi)} · masraf ${kisa(k.masraf)}</small></td>
      <td class="para">${pesinatH(k)}</td><td class="para">${tl(k.yeniAnapara)}</td><td>${yuzde(k.faizOrani)} → <b>${yuzde(k.yeniFaizOrani)}</b><small>BSMV ${yuzde(k.bsmvOrani)}</small></td><td>${k.kalanVade} → <b>${k.yeniVade}</b>${k.odemesizAy ? `<small>${k.odemesizAy} ay ödemesiz</small>` : ''}</td><td class="para">${tl(k.uygulananTaksit)}<small>${k.yeniTaksit > 0 ? 'banka taksiti' : 'hesaplanan'} · ${k.taksitSayisi} taksit</small></td><td>${fark(k.aylikFark)}</td><td class="para">${tl(k.toplamGeriOdeme)}<small>${k.pesinat > 0 ? 'peşinat dahil · ' : ''}${tarih(k.ilkTaksitTarihi)} – ${tarih(k.sonTaksitTarihi)}</small></td><td class="para">${tl(k.toplamFaiz)}</td><td>${fark(k.borcFarki)}</td>
      <td class="para">${k.teminatlar.length ? `${tl(k.teminatToplam)}<small>${k.teminatlar.length} teminat · ipotek ${kisa(k.ipotekToplam)}</small>` : bos}</td>${kasaH(k)}${islemH(k)}</tr>`);
    const dip = `<tr><td colspan="5">Toplam · ${L.length} kredi</td><td class="para">${tl(top(L, 'kalanAnapara'))}</td><td class="para">${top(L, 'gecikenTaksit')} adet<small>${tl(top(L, 'gecikenTutar'))}</small></td><td class="para">${tl(top(L, 'taksit'))}</td><td class="para">${tl(top(L, 'oncesiToplamBorc'))}</td><td class="para">${tl(top(L, 'pesinat'))}</td><td class="para">${tl(top(L, 'yeniAnapara'))}</td><td></td><td></td><td class="para">${tl(top(L, 'uygulananTaksit'))}</td><td>${fark(top(L, 'aylikFark'))}</td><td class="para">${tl(top(L, 'toplamGeriOdeme'))}</td><td class="para">${tl(top(L, 'toplamFaiz'))}</td><td>${fark(top(L, 'borcFarki'))}</td><td class="para">${tl(top(L, 'teminatToplam'))}</td><td colspan="2"><small>${top(L, k => k.kasaOzet.satir)} taksit satırı · ${top(L, k => k.kasaOzet.odendi)} ödendi</small></td></tr>`;
    tablo('krediler', bas, rows, dip, null, grup);
    $('#kyNot-krediler').textContent = `${L.length} kredi · Aylık Fark = yeni taksit − eski taksit · Borç Farkı = toplam geri ödeme − öncesi toplam borç · satıra tıklayın: düzenle`;
    $('#kyAciklama-krediler').textContent = 'Öncesi: kalan anapara + geciken taksitler + gecikme faizi + masraf = Toplam Borç. Sonrası: peşinat protokolde peşin ödenir, kalan yeni anapara üzerinden ödemesiz dönem ve yeni vade ile eşit taksit (banka taksit tutarı verildiyse o kullanılır); Toplam Geri Ödeme = peşinat + taksitlerin toplamı, Toplam Faiz = BSMV dahil faiz. Teminatlar sekmesinde ipotek / kefalet ayrıntıları, Taksit Takvimi\'nde peşinat ve taksitlerin Kasa Nakit Akışı durumu.';
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
  function cizTeminat(L) {
    const bas = ['Kredi No', 'Şirket', 'Banka', 'Kredi No', 'Teminat Türü', 'Açıklama', 'Tapu / Kimlik Bilgisi', 'Derece', 'Malik / Kefil', 'Teminat Tutarı', 'Ekspertiz Değeri', 'Teminat / Yeni Anapara %', 'Not', 'İşlem'];
    const rows = []; L.forEach(k => (k.teminatlar || []).forEach(t => rows.push(`<tr class="${k.ornek ? 'ornek' : ''}" data-id="${k.id}">${noH(k)}<td>${esc(k.sirket)}</td><td class="ad">${esc(k.banka)}</td><td>${esc(k.krediNo || '—')}</td><td>${rz(t.tur, t.tur === 'İpotek' ? 'mavi' : 'gri')}</td><td>${esc(t.aciklama || '—')}</td><td>${esc(t.tapuBilgisi || '—')}</td><td>${esc(t.derece || '—')}</td><td>${esc(t.malik || '—')}</td><td class="para">${tl(t.tutar)}</td><td class="para">${t.ekspertiz ? tl(t.ekspertiz) : bos}</td><td>${k.yeniAnapara > 0 ? yuzde(t.tutar / k.yeniAnapara * 100) : '—'}</td><td><small style="display:inline">${esc(t.not || '')}</small></td>${islemH(k)}</tr>`)));
    const tTop = top(L, 'teminatToplam'), eTop = top(L, 'ekspertizToplam'), aTop = top(L, 'yeniAnapara');
    const dip = `<tr><td colspan="9">Toplam · ${rows.length} teminat · ${L.length} kredi</td><td class="para">${tl(tTop)}</td><td class="para">${tl(eTop)}</td><td>${aTop > 0 ? yuzde(tTop / aTop * 100) : '—'}</td><td colspan="2"><small>ipotek ${tl(top(L, 'ipotekToplam'))}</small></td></tr>`;
    tablo('teminat', bas, rows, dip, 'Teminat kaydı yok. Kredi kartında "Teminat Ekle" ile ipotek, kefalet, rehin ve teminat mektuplarını girin.');
    $('#kyNot-teminat').textContent = 'ipotek: tapu bilgisi (il / ilçe / ada / parsel), derece, ipotek tutarı, ekspertiz değeri, malik · kefalet: kefil · rehin / teminat mektubu / çek-senet / blokaj';
    $('#kyAciklama-teminat').textContent = 'Teminat / Yeni Anapara %: teminat tutarının yapılandırılan anaparaya oranı (banka teminat marjı kontrolü). Ekspertiz değeri ipotekli taşınmazın bankaca kabul edilen değeridir.';
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
  const CIZ = { krediler: cizKrediler, oncesi: cizOncesi, sonrasi: cizSonrasi, teminat: cizTeminat, takvim: cizTakvim, aylik: cizAylik };

  // ---------- sekme / filtre
  function sekme(p) { $$('#kySekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#ky > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); ciz(); }
  $('#kySekmeler').addEventListener('click', e => { const b = e.target.closest('button[data-p]'); if (b) sekme(b.dataset.p); });
  ['#kyS', '#kyB', '#kyD'].forEach(s => $(s).addEventListener('change', () => { F.sirket = $('#kyS').value; F.banka = $('#kyB').value; F.durum = $('#kyD').value; ciz(); }));
  $('#kyAra').addEventListener('input', () => { F.ara = $('#kyAra').value; ciz(); });
  $('#kyTemizle').addEventListener('click', () => { F = { sirket: '', banka: '', durum: '', ara: '' }; ['#kyS', '#kyB', '#kyD', '#kyAra'].forEach(s => $(s).value = ''); ciz(); });
  const tk = $('#kyTakvimKredi'); if (tk) tk.addEventListener('change', () => { takvimKredi = tk.value; ciz(); });
  kok.addEventListener('click', async e => {
    const d = e.target.closest('.ky-duzenle'); if (d) { e.preventDefault(); formAc(V.krediler.find(k => k.id === +d.dataset.id)); return; }
    const t = e.target.closest('.ky-takvim'); if (t) { takvimKredi = t.dataset.id; if (tk) tk.value = takvimKredi; sekme('takvim'); return; }
    const a = e.target.closest('.ky-aktar'); if (a) { const k = V.krediler.find(x => x.id === +a.dataset.id); const ac = a.dataset.aktar === '1'; if (!k) return; if (!window.confirm(ac ? `${k.no} · ${k.banka}: taksitler Kasa Nakit Akışı'na (Havale / EFT) ve Planlı Düzenli Ödemeler'e aktarılsın mı?` : `${k.no} · ${k.banka}: nakit akışa aktarım kapatılsın mı? Ödenmemiş taksit satırları kaldırılır, düzenli ödeme pasife alınır.`)) return; const j = await post('/KrediYapilandirma/Aktar', { id: k.id, aktar: ac }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); }
  });
  $('#kyYeni').addEventListener('click', () => formAc(null));
  $('#kyExcel').addEventListener('click', () => {
    const p = aktifSekme(), ad = ($(`#kySekmeler button[data-p="${p}"]`) || {}).textContent?.trim() || p;
    if (!window.TabloExcel) { mesaj('danger', 'Excel bileşeni yüklenemedi; sayfayı yenileyin.'); return; }
    window.TabloExcel.indir('kredi_yapilandirma_' + p, [{ tablo: '#kyTablo-' + p, ad: ad.slice(0, 31), baslik: 'Kredi Yapılandırma Raporu · ' + ad, altBaslik: $('#kyKapsam').textContent.trim(), kpi: '#kyKpi', not: '#kyAciklama-' + p }]).catch(err => mesaj('danger', 'Excel indirilemedi: ' + esc(err.message)));
  });

  // ---------- kredi formu (modal)
  const SAYI = (v, ond = 2) => { const x = Number(String(v ?? '').replace(/\./g, '').replace(',', '.')); return isNaN(x) ? 0 : Math.round(x * 10 ** ond) / 10 ** ond; };
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
  function alanHtml(a, v) {
    const [k, ad, tip, kaynak] = a; const id = 'kyf_' + k; const lbl = `<label for="${id}">${esc(ad)}</label>`;
    if (tip === 'select') return `<div class="ky-f">${lbl}<select class="form-select" id="${id}" data-k="${k}">${(kaynak === 'sirketler' ? V.sirketler.map(s => s.ad) : V[kaynak]).map(o => `<option value="${esc(o)}" ${o === v ? 'selected' : ''}>${esc(o)}</option>`).join('')}</select></div>`;
    if (tip === 'liste') return `<div class="ky-f">${lbl}<input class="form-control" id="${id}" data-k="${k}" list="kyDl_${k}" value="${esc(v ?? '')}" maxlength="80"><datalist id="kyDl_${k}">${(V[kaynak] || []).map(o => `<option value="${esc(o)}">`).join('')}</datalist></div>`;
    if (tip === 'date') return `<div class="ky-f">${lbl}<input class="form-control" type="date" id="${id}" data-k="${k}" value="${esc(v ?? '')}"></div>`;
    if (tip === 'para') return `<div class="ky-f">${lbl}<input class="form-control" type="number" step="0.01" min="0" id="${id}" data-k="${k}" data-tip="para" value="${v ? Number(v) : ''}" placeholder="0,00"></div>`;
    if (tip === 'oran') return `<div class="ky-f">${lbl}<input class="form-control" type="number" step="0.01" min="0" max="300" id="${id}" data-k="${k}" data-tip="oran" value="${v ?? ''}"></div>`;
    if (tip === 'tam') return `<div class="ky-f">${lbl}<input class="form-control" type="number" step="1" min="0" id="${id}" data-k="${k}" data-tip="tam" value="${v ?? ''}"></div>`;
    if (tip === 'onay') return `<div class="ky-f">${lbl}<label class="d-flex align-items-center gap-2" style="font-size:.72rem;text-transform:none;font-weight:600;color:#0f172a;margin-top:5px"><input type="checkbox" id="${id}" data-k="${k}" data-tip="onay" ${v ? 'checked' : ''}> Ödendi (Kasa Nakit Akışı satırı ÖDENDİ yazılır)</label></div>`;
    return `<div class="ky-f">${lbl}<input class="form-control" id="${id}" data-k="${k}" value="${esc(v ?? '')}" maxlength="120"></div>`;
  }
  const teminatSatir = (t, i) => `<tr data-i="${i}"><td><select class="form-select kyt" data-k="tur">${V.teminatTurleri.map(o => `<option ${o === (t.tur || 'İpotek') ? 'selected' : ''}>${esc(o)}</option>`).join('')}</select></td><td><input class="form-control kyt" data-k="aciklama" value="${esc(t.aciklama || '')}" placeholder="gayrimenkul / araç / kefil tanımı" maxlength="160"></td><td><input class="form-control kyt" data-k="tapuBilgisi" value="${esc(t.tapuBilgisi || '')}" placeholder="il / ilçe / ada / parsel · plaka · TCKN" maxlength="160"></td><td><input class="form-control kyt" data-k="derece" value="${esc(t.derece || '')}" placeholder="1. derece" maxlength="40" style="width:90px"></td><td><input class="form-control kyt" data-k="malik" value="${esc(t.malik || '')}" placeholder="malik / kefil" maxlength="120"></td><td><input class="form-control kyt" type="number" step="0.01" min="0" data-k="tutar" value="${t.tutar ? Number(t.tutar) : ''}" style="width:130px"></td><td><input class="form-control kyt" type="number" step="0.01" min="0" data-k="ekspertiz" value="${t.ekspertiz ? Number(t.ekspertiz) : ''}" style="width:130px"></td><td><input class="form-control kyt" data-k="not" value="${esc(t.not || '')}" maxlength="160"></td><td><button type="button" class="btn btn-link btn-sm p-0 text-danger kyTeminatSil" title="Teminatı kaldır"><i class="fas fa-times"></i></button></td></tr>`;
  let duzenlenen = null;
  function formAc(k) {
    duzenlenen = k; const d = k || { sirket: V.sirketler[0]?.ad, krediTuru: 'Taksitli Ticari Kredi', durum: 'Yapılandırıldı', oncekiDurum: 'Gecikmede', bsmvOrani: 5, nakitAkisaAktar: true, teminatlar: [] };
    $('#kyMBaslik').textContent = k ? `${k.no} · ${k.banka} · ${k.krediNo || k.krediTuru}` : 'Yeni Kredi Yapılandırması';
    $('#kyMGovde').innerHTML = `
      <div class="ky-bolum"><div class="b"><span><i class="fas fa-id-card me-1"></i>Kredi Kimliği</span><span style="text-transform:none;font-weight:600;color:#64748b">Şirket: Kasa Nakit Akışı şirketleri · banka listeden ya da elle</span></div><div class="g">${alanlar.kimlik.map(a => alanHtml(a, d[a[0]])).join('')}</div></div>
      <div class="ky-bolum oncesi"><div class="b"><span><i class="fas fa-clock-rotate-left me-1"></i>Yapılandırma Öncesi</span><span id="kyOnceBorc" style="text-transform:none;font-weight:700"></span></div><div class="g">${alanlar.oncesi.map(a => alanHtml(a, d[a[0]])).join('')}</div></div>
      <div class="ky-bolum sonrasi"><div class="b"><span><i class="fas fa-file-signature me-1"></i>Yapılandırma Sonrası</span><button type="button" class="btn btn-light btn-sm py-0 border" id="kyAnaparaOner" style="font-size:.6rem;text-transform:none" title="Yeni anapara = kalan anapara + geciken taksit tutarı + gecikme faizi + masraf"><i class="fas fa-wand-magic-sparkles me-1"></i>Yeni Anaparayı Öner</button></div><div class="g">${alanlar.sonrasi.map(a => alanHtml(a, d[a[0]])).join('')}</div>
        <div class="ky-onizleme" id="kyOnizleme"></div></div>
      <div class="ky-bolum teminat"><div class="b"><span><i class="fas fa-house-lock me-1"></i>Teminatlar · İpotek</span><button type="button" class="btn btn-light btn-sm py-0 border" id="kyTeminatEkle" style="font-size:.6rem;text-transform:none"><i class="fas fa-plus me-1"></i>Teminat Ekle</button></div>
        <div style="padding:6px 10px;overflow-x:auto"><table class="ky-teminat"><thead><tr><th>Tür</th><th>Açıklama</th><th>Tapu / Kimlik Bilgisi</th><th>Derece</th><th>Malik / Kefil</th><th>Teminat Tutarı ₺</th><th>Ekspertiz ₺</th><th>Not</th><th></th></tr></thead><tbody id="kyTeminatlar">${(d.teminatlar || []).map(teminatSatir).join('')}</tbody></table><div class="text-muted" style="font-size:.62rem;margin-top:4px">İpotek: tapu bilgisi, derece, ipotek tutarı, ekspertiz değeri ve malik · Kefalet: kefil adı · Rehin / teminat mektubu / çek-senet / nakit blokaj</div></div></div>
      <div class="ky-bolum"><div class="b"><span><i class="fas fa-note-sticky me-1"></i>Not ve Akış</span></div><div class="g"><div class="ky-f g2"><label for="kyf_not">Not</label><textarea class="form-control" id="kyf_not" data-k="not" rows="2" maxlength="600">${esc(d.not || '')}</textarea></div>
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
    const d = { id: duzenlenen ? duzenlenen.id : 0, teminatlar: [], nakitAkisaAktar: $('#kyf_aktar').checked };
    $$('#kyMGovde [data-k]').forEach(el => { if (el.classList.contains('kyt')) return; const k = el.dataset.k, tip = el.dataset.tip; d[k] = tip === 'onay' ? el.checked : tip === 'para' || tip === 'oran' ? SAYI(el.value, tip === 'oran' ? 4 : 2) : tip === 'tam' ? Math.max(0, parseInt(el.value, 10) || 0) : (el.value ?? '').trim(); });
    $$('#kyTeminatlar tr').forEach(tr => { const t = {}; $$('.kyt', tr).forEach(el => { t[el.dataset.k] = el.type === 'number' ? SAYI(el.value) : el.value.trim(); }); d.teminatlar.push(t); });
    return d;
  }
  function onizle() {
    const d = formOku(), h = hesapla(d), onceBorc = d.kalanAnapara + d.gecikenTutar + d.gecikmeFaizi + d.masraf, teminat = d.teminatlar.reduce((s, t) => s + n0(t.tutar), 0);
    $('#kyOnceBorc').textContent = `Toplam Borç: ${tl(onceBorc)}`;
    const ilk = d.ilkTaksitTarihi || (d.yapilandirmaTarihi ? (() => { const t = new Date(d.yapilandirmaTarihi); t.setMonth(t.getMonth() + 1 + (d.odemesizAy || 0)); return t.toISOString().slice(0, 10); })() : '');
    const son = ilk && h.n ? (() => { const t = new Date(ilk); t.setMonth(t.getMonth() + h.n - 1); return t.toISOString().slice(0, 10); })() : '';
    $('#kyOnizleme').innerHTML = [
      ['Taksit Sayısı', h.n ? `${h.n}` : '—', d.odemesizAy ? `${d.odemesizAy} ay ödemesiz` : 'ödemesiz dönem yok'],
      ['Hesaplanan Taksit', h.taksit ? tl(h.taksit) : '—', `aylık oran ${(d.yeniFaizOrani / 12 * (1 + d.bsmvOrani / 100)).toLocaleString('tr-TR', { maximumFractionDigits: 4 })} % (BSMV dahil)`],
      ['Uygulanacak Taksit', h.uygulanan ? tl(h.uygulanan) : '—', d.yeniTaksit > 0 ? 'banka protokolü tutarı' : 'hesaplanan tutar'],
      ['Aylık Fark', h.uygulanan ? `${h.uygulanan - d.taksit >= 0 ? '+' : ''}${tl(h.uygulanan - d.taksit)}` : '—', `eski taksit ${tl(d.taksit)}`],
      ['Toplam Geri Ödeme', h.genel ? tl(h.genel) : '—', `${h.pesinat ? 'peşinat ' + tl(h.pesinat) + ' · ' : ''}taksitler ${tl(h.toplam)} · faiz ${tl(h.faiz)} · anapara ${tl(h.anapara)}`],
      ['Borç Farkı · Teminat', h.genel ? `${h.genel - onceBorc >= 0 ? '+' : ''}${tl(h.genel - onceBorc)}` : '—', `teminat ${tl(teminat)} (${d.yeniAnapara > 0 ? yuzde(teminat / d.yeniAnapara * 100) : '—'})${ilk ? ' · ' + tarih(ilk) + (son ? ' – ' + tarih(son) : '') : ''}`]
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

  // ---------- başlangıç
  sekme(kok.dataset.sekme || 'krediler'); yukle();
})();
