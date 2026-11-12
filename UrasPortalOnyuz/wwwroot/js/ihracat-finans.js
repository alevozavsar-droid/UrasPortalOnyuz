// ============================================================
// İHRACAT FİNANSAL TAKİBİ — ekran betiği (Views/IhracatFinans/Index.cshtml; veri /IhracatFinans/Veri; kaynak Data/IbkbOrnek.cs). 11 Kas 2026.
// "İhracat Tahsilat Raporu" + "İBKB Beyanname Takibi" birleşik, konsolide (tüm şirketler), dinamik rapor. Satış → beyanname (GÇB) → banka tahsilatı → DAB / İBKB belgesi zinciri.
// Sekmeler: Özet · İhracat Satışları (Yurtdışı Satış Siparişi'nden canlı; dosyasız faturadan tek tuşla İBKB dosyası) · Beyanname / İBKB Dosyaları (ekle / düzenle / sil) ·
//   Banka Tahsilatları (bankadan gelen döviz girişi; "Eşleştir" → aday dosyalar skorla, seçimde "İBKB oluşturulsun mu?" sorulur; dosyaya DAB satırı yazılır) ·
//   İBKB Belgeleri (bankaya verilecek basılabilir form /IhracatFinans/Belge/{id}; banka İBKB no / durum) · Müşteri Bazlı · Vade Takibi · DAB Bağlantıları.
// Sunucu hesaplar (gereken, getirilen, kalan, vade, kalan gün, takip) pakette gelir; filtre ve özetler istemcide. Hücreler yatay + dikey ortada (tablo-hizala data-dikey="orta"); Excel ortak TabloExcel.
// Yazım: liste değerleri Title Case; tutarlar kendi dövizinde ($ € £ ₽); dövizi bilinmeyen satır "Döviz Belirsiz" rozeti (uydurulmaz).
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('ibkb'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const n0 = v => Number(v) || 0;
  const para = v => n0(v).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const sembol = pb => ({ USD: '$', EUR: '€', GBP: '£', RUB: '₽', TRY: '₺', TL: '₺' })[pb] || (pb ? pb : '?');
  const tutar = (v, pb) => para(v) + ' ' + sembol(pb);
  const tarih = s => { if (!s) return '—'; const p = String(s).slice(0, 10).split('-'); return p.length === 3 ? `${p[2]}.${p[1]}.${p[0]}` : s; };
  const AYLAR = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
  const bos = '<span class="text-muted">—</span>';
  const rz = (m, t) => `<span class="ib2-rz ${t}">${esc(m)}</span>`;
  const dabRz = d => rz(d || 'Bekliyor', d === 'Yapıldı' || d === 'Peşin Yapıldı' ? 'ok' : d === 'Gerek Yok' ? 'gri' : d === 'Kısmi' ? 'bekle' : d === 'İptal' ? 'hata' : 'mavi');
  const takipRz = t => rz(t || 'Açık', t === 'Kapandı' ? 'ok' : t === 'Muaf' ? 'gri' : t === 'Vadesi Geçti' ? 'hata' : t === 'Vade Yaklaşıyor' || t === 'Kısmi' ? 'bekle' : t === 'İptal' || t === 'Bedel Farkı' ? 'mor' : 'mavi');
  // beyanname ↔ gerçek ihracat bedeli farkı (11 Kas 2026): fark hücresi (+ beyanname fazlası kırmızı, − eksik turuncu) ve neden
  const farkH = d => !d.bedelFarki ? bos : `<span style="font-weight:800;color:${d.bedelFarki > 0 ? '#b91c1c' : '#b45309'}">${d.bedelFarki > 0 ? '+' : ''}${tutar(d.bedelFarki, d.doviz)}</span><small>${d.bedelFarki > 0 ? 'beyanname fazlası' : 'beyanname eksiği'}${d.farkNedeni ? ' · ' + esc(d.farkNedeni) : ''}</small>`;
  const dovizRz = d => d ? rz(d, 'teal') : '<span class="ib2-rz hata" title="Excel listesinde döviz yazılı değil; kayıtta seçilmeli">Döviz Belirsiz</span>';
  const sinif = d => d.takip === 'Kapandı' ? 'kapandi' : d.takip === 'Vadesi Geçti' ? 'gecti' : d.takip === 'Vade Yaklaşıyor' ? 'yakin' : d.takip === 'İptal' || d.takip === 'Muaf' ? 'soluk' : '';
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 10000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }).then(r => r.json()).catch(() => ({ success: false, message: 'Ağ hatası.' }));
  /** döviz bazında toplam: Map döviz → tutar (dövizi boş satırlar '?' altında) */
  const DOVIZ_SIRA = { USD: 0, EUR: 1, GBP: 2, RUB: 3, '?': 9 };
  const dovizTop = (L, f) => { const m = new Map(); L.forEach(d => { const pb = d.doviz || '?'; m.set(pb, (m.get(pb) || 0) + n0(typeof f === 'function' ? f(d) : d[f])); }); return [...m].filter(([, v]) => Math.abs(v) > 0.004).sort((a, b) => (DOVIZ_SIRA[a[0]] ?? 5) - (DOVIZ_SIRA[b[0]] ?? 5)); };
  const sy = v => `<td class="sayac">${v}</td>`;   // adet hücresi: tablo-hizala ₺ eklemesin
  const kisaUsd = v => n0(v).toLocaleString('tr-TR', { maximumFractionDigits: 0 }) + ' $';
  const usdK = (tutar, doviz) => n0(tutar) * (n0((V.usdKur || {})[doviz]) || (doviz ? 1 : 0));   // ABD doları karşılığı (döviz boşsa 0)
  const dovizH = (L, f, kucuk) => { const l = dovizTop(L, f); return l.length ? (kucuk ? l.map(([pb, v]) => tutar(v, pb)).join(' · ') : l.map(([pb, v], i) => i === 0 ? tutar(v, pb) : `<small>${tutar(v, pb)}</small>`).join('')) : bos; };
  const kisaDoviz = (L, f) => dovizTop(L, f).map(([pb, v]) => n0(v).toLocaleString('tr-TR', { maximumFractionDigits: 0 }) + ' ' + sembol(pb)).join(' · ') || '—';

  let V = null, F = { sirket: '', yil: '', dab: '', takip: '', doviz: '', ara: '' };
  const aktifSekme = () => ($('#ibSekmeler button.aktif') || {}).dataset?.p || 'ozet';

  // ---------- yükleme
  async function yukle(deneme = 0) {
    let j; try { const r = await fetch('/IhracatFinans/Veri', { cache: 'no-store' }); if (!r.ok) throw new Error('HTTP ' + r.status); j = await r.json(); }
    catch (e) { if (deneme < 4) { mesaj('warning', `Veri yükleniyor… sunucu yanıt vermedi, yeniden deneniyor (${deneme + 1} / 4).`); setTimeout(() => yukle(deneme + 1), 3000); return; } mesaj('danger', 'Veri yüklenemedi (' + esc(e.message) + ').'); return; }
    veri(j);
  }
  function veri(j) {
    if (!j || !j.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = j; ['dosyalar', 'sirketler', 'dabDurumlari', 'takipDurumlari', 'dovizler', 'yillar', 'bankalar', 'ulkeMuaf', 'ulkeYarim', 'satislar', 'tahsilatlar', 'belgeler', 'tahsilatDurumlari', 'belgeDurumlari'].forEach(k => { if (!Array.isArray(V[k])) V[k] = []; }); V.dosyalar.forEach(d => { if (!Array.isArray(d.dablar)) d.dablar = []; }); V.usdKur = V.usdKur || { USD: 1 }; V.esikUsd = n0(V.esikUsd) || 15000;
    if (!V.tahsilatDurumlari.length) V.tahsilatDurumlari = ['Eşleşmedi', 'Eşleşti', 'İBKB Düzenlendi']; if (!V.belgeDurumlari.length) V.belgeDurumlari = ['Hazırlandı', 'Bankaya Gönderildi', 'Banka Onayladı', 'İptal'];
    $('#ibKalici').style.display = V.kalici ? '' : 'none'; filtreDoldur(); ciz();
    // e-posta / portal mesajındaki bağlantı (?gorev=ID): görev penceresi açılır
    if (!veri.gorevAcildi && +kok.dataset.gorev) { veri.gorevAcildi = true; const g = (V.gorevler || []).find(x => x.id === +kok.dataset.gorev); if (g) gorevAc(g); else mesaj('warning', `#${kok.dataset.gorev} numaralı görev bulunamadı.`); }
  }
  $('#ibModal').addEventListener('hidden.bs.modal', () => { $('#ibMKaydet').style.display = ''; });
  function secenekler(sel, degerler, mevcut) { const v = mevcut ?? sel.value; sel.innerHTML = '<option value="">Tümü</option>' + degerler.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); sel.value = degerler.map(String).includes(String(v)) ? v : ''; }
  function filtreDoldur() {
    secenekler($('#ibS'), V.sirketler, F.sirket); F.sirket = $('#ibS').value;
    secenekler($('#ibY'), V.yillar.map(String), F.yil); F.yil = $('#ibY').value;
    // durum süzgeci sekmeye göre: dosya sekmelerinde DAB / İBKB durumu, Banka Tahsilatları'nda tahsilat durumu, İBKB Belgeleri'nde belge durumu, İhracat Satışları'nda sipariş durumu
    const p = aktifSekme(), DE = p === 'tahsilat' ? ['Kaynak', V.tahsilatKaynaklari || []] : p === 'tamamlanan' ? ['Tahsilat Durumu', V.tahsilatDurumlari.filter(x => x !== 'Eşleşmedi')] : p === 'belgeler' ? ['Belge Durumu', V.belgeDurumlari] : p === 'satis' ? ['Sipariş Durumu', ['Yüklendi', 'Faturalandı']] : p === 'bakiye' ? ['Kontrol / Görev', ['Kontrol Yok', 'Kontrol Gecikti', ...(V.bakiyeKontrolDurumlari || []), ...BAKIYE_EK]] : p === 'sube' ? ['Satış Temsilcisi', V.temsilciler || []] : p === 'musteri' ? ['Satış Temsilcisi', [...(V.temsilciler || []), 'Atanmadı']] : ['DAB / İBKB Durumu', V.dabDurumlari];
    $('#ibDEtiket').textContent = DE[0]; secenekler($('#ibD'), DE[1], F.dab); F.dab = $('#ibD').value;
    secenekler($('#ibT'), V.takipDurumlari, F.takip); F.takip = $('#ibT').value;
    secenekler($('#ibV'), [...V.dovizler, '?'], F.doviz); F.doviz = $('#ibV').value;
  }
  // Müşteri Bazlı'da durum süzgeci satış temsilcisidir (12 Kas 2026: "temsilci sütunu olsun, temsilcisi filtrelenebilsin"); öbür dosya sekmelerinde DAB / İBKB durumu
  const dabOlcut = d => !F.dab || (aktifSekme() === 'musteri' ? (d.temsilci || 'Atanmadı') === F.dab : d.dabDurumu === F.dab);
  const liste = () => V.dosyalar.filter(d => (!F.sirket || d.sirket === F.sirket) && (!F.yil || String(d.yil) === F.yil) && dabOlcut(d) && (!F.takip || d.takip === F.takip) && (!F.doviz || (d.doviz || '?') === F.doviz) && (!kpi.takipOlmayan || !kpi.takipOlmayan.includes(d.takip))
    && (!F.ara || [d.dosyaNo, d.musteri, d.musteriKodu, d.beyannameNo, d.faturaNo, d.siparisNo, d.temsilci, d.aciklama, d.dabBanka, d.dabNotu, d.not, d.ulke, ...d.dablar.map(p => p.banka + ' ' + p.no + ' ' + p.not)].join(' ').toLocaleLowerCase('tr-TR').includes(F.ara.toLocaleLowerCase('tr-TR'))));
  const gecerli = L => L.filter(d => d.dabDurumu !== 'İptal');
  const ara = (alanlar) => !F.ara || alanlar.join(' ').toLocaleLowerCase('tr-TR').includes(F.ara.toLocaleLowerCase('tr-TR'));
  const satisListe = () => V.satislar.filter(s => (!F.sirket || s.sirket === F.sirket) && (!F.yil || (s.faturaTarihi || s.tarih || '').slice(0, 4) === F.yil) && (!F.dab || s.durum === F.dab) && (!F.doviz || s.doviz === F.doviz) && ara([s.no, s.faturaNo, s.cari, s.cariKodu, s.ulke, s.dosyaNo, s.temsilci]));
  // banka tahsilatları iki sekmede (11 Kas 2026 "banka tahsilatı sekmesi çok uzun olur"): Açık = Eşleşmedi (durum süzgeci kaynak); Tamamlanan = Eşleşti / İBKB Düzenlendi (dosya + belge ile)
  const tahsilatTum = () => V.tahsilatlar.filter(t => (!F.sirket || t.sirket === F.sirket) && (!F.yil || (t.tarih || '').slice(0, 4) === F.yil) && (!F.doviz || t.doviz === F.doviz) && ara([t.banka, t.gonderen, t.musteriKodu, t.referans, t.aciklama, t.dosyaNo, t.beyannameNo, t.belgeNo]));
  const tahsilatListe = () => tahsilatTum().filter(t => t.durum === 'Eşleşmedi' && (!F.dab || t.kaynak === F.dab));
  const tamamlananListe = () => tahsilatTum().filter(t => t.durum !== 'Eşleşmedi' && (!F.dab || t.durum === F.dab));
  const belgeListe = () => V.belgeler.filter(b => (!F.sirket || b.sirket === F.sirket) && (!F.yil || (b.tarih || '').slice(0, 4) === F.yil) && (!F.dab || b.durum === F.dab) && (!F.doviz || b.doviz === F.doviz) && ara([b.no, b.banka, b.dosyaNo, b.beyannameNo, b.faturaNo, b.musteri, b.musteriKodu, b.bankaIbkbNo, b.tahsilatRef, b.not]));
  const tahsilatRz = d => rz(d || 'Eşleşmedi', d === 'İBKB Düzenlendi' ? 'ok' : d === 'Eşleşti' ? 'mavi' : 'bekle');
  // cari bakiyeler (mizan): yurtdışı müşteri carileri + İBKB dosyası karşılaştırması + kontrol işareti
  // durum süzgeci: kontrol durumu ya da görev / mutabakat ölçütü (12 Kas 2026)
  const BAKIYE_EK = ['Görev Açık', 'Onay Bekliyor', 'Mutabakat Gönderildi', 'Mutabakat İtirazlı'];
  const bakiyeOlcut = c => !F.dab || (F.dab === 'Görev Açık' ? !!c.gorev : F.dab === 'Onay Bekliyor' ? c.gorev && c.gorev.durum === 'Düzeltildi' : F.dab === 'Mutabakat Gönderildi' ? !!c.sonGonderim : F.dab === 'Mutabakat İtirazlı' ? c.sonGonderim && c.sonGonderim.durum === 'İtirazlı' : c.kontrolDurumu === F.dab);
  const bakiyeListe = () => (V.cariBakiyeler || []).filter(c => (!F.sirket || c.sirket === F.sirket) && bakiyeOlcut(c) && (!F.doviz || c.dovizler.some(d => d.pb === F.doviz)) && ara([c.kod, c.ad, c.resmiUnvan, c.ulke, c.sirket, (c.kontrol || {}).not, (c.gorev || {}).atanan]));
  const kontrolRz = d => rz(d || 'Kontrol Yok', d === 'Uyumlu' ? 'ok' : d === 'Fark Var' ? 'hata' : d === 'Kontrol Gecikti' ? 'bekle' : d === 'Açıklama Bekliyor' ? 'mor' : 'gri');
  const gorevRz = d => rz(d || 'Atandı', d === 'Onaylandı' ? 'ok' : d === 'Düzeltildi' ? 'mor' : d === 'İade Edildi' ? 'hata' : d === 'İptal' ? 'gri' : 'bekle');
  const oncelikRz = o => rz(o || 'Normal', o === 'Acil' ? 'hata' : o === 'Yüksek' ? 'bekle' : 'gri');
  const BEN = kok.dataset.kullanici || '', BEN_AD = kok.dataset.kullaniciAd || '';
  const belgeRz = d => rz(d || 'Hazırlandı', d === 'Banka Onayladı' ? 'ok' : d === 'Bankaya Gönderildi' ? 'mavi' : d === 'İptal' ? 'hata' : 'bekle');

  // ---------- KPI
  function kpi(L) {
    const G = gecerli(L), acik = G.filter(d => d.takip === 'Açık' || d.takip === 'Vade Yaklaşıyor' || d.takip === 'Vadesi Geçti' || d.takip === 'Kısmi' || d.takip === 'Bedel Farkı'), gecti = G.filter(d => d.takip === 'Vadesi Geçti'), yakin = G.filter(d => d.takip === 'Vade Yaklaşıyor'), kapandi = G.filter(d => d.takip === 'Kapandı'), muaf = G.filter(d => d.takip === 'Muaf');
    // her kart tıklanır (11 Kas 2026, "bu kısım dinamik olsun, tıkladığımda açılsın"): ilgili sekme o süzgeçle açılır; kartın alt metni tam gösterilir. [renk, başlık, değer, alt metin, sekme, süzgeç {takip | dab}]
    const K = [
      ['#0f766e', 'İhracat Dosyası', `${G.length}`, `${[...new Set(G.map(d => d.sirket))].length} şirket · ${[...new Set(G.map(d => d.yil))].length} yıl · ${[...new Set(G.map(d => d.musteriKodu || d.musteri))].length} müşteri · ${L.length - G.length} iptal`, 'liste', {}],
      ['#1e40af', 'Beyanname Tutarı', kisaDoviz(G, 'tutar').split(' · ')[0] || '—', kisaDoviz(G, 'tutar'), 'ozet', {}],
      ['#b45309', 'Gereken Bedel', kisaDoviz(G, 'gereken').split(' · ')[0] || '—', `ülke gerekliliği × tutar · ${kisaDoviz(G, 'gereken')}`, 'liste', { takipOlmayan: ['Muaf', 'İptal'] }],
      ['#15803d', 'Getirilen (DAB / İBKB)', kisaDoviz(G, 'getirilen').split(' · ')[0] || '—', `${kapandi.length} dosya kapandı · ${kisaDoviz(G, 'getirilen')}`, 'dab', {}],
      ['#dc2626', 'Kalan Bedel', kisaDoviz(acik, 'kalan').split(' · ')[0] || '—', `${acik.length} açık dosya · ${kisaDoviz(acik, 'kalan')}${G.some(d => d.bedelFarki) ? ` · gerçek bedele göre kalan ${kisaDoviz(acik, 'kalanGercek')} · beyanname fazlası ${kisaDoviz(G, 'beyannameFazlasi')} (${G.filter(d => d.bedelFarki).length} dosya)` : ''}`, 'vade', {}],
      ['#9d174d', 'Vadesi Geçen', `${gecti.length}`, `${yakin.length} dosyada vade 30 gün içinde · vade = başlangıç + ${V.vadeGun} gün`, 'vade', { takip: 'Vadesi Geçti' }],
      ['#64748b', 'Muaf · Gerek Yok', `${muaf.length}`, `${G.filter(d => d.esikAlti).length} dosya ${kisaUsd(V.esikUsd)} eşiği altı · ${G.filter(d => d.dabDurumu === 'Kısmi').length} kısmi · ${G.filter(d => d.dabDurumu === 'Peşin Yapıldı').length} peşin · ${G.filter(d => !d.doviz).length} döviz belirsiz`, 'liste', { dab: 'Gerek Yok' }],
      ['#7c3aed', 'Banka Tahsilatı · İBKB', `${tahsilatListe().length} açık`, `${tahsilatTum().length} tahsilat (${tamamlananListe().length} tamamlandı) · açık ${kisaDoviz(tahsilatListe(), 'tutar')} · ${belgeListe().filter(b => b.durum !== 'İptal').length} İBKB belgesi (${belgeListe().filter(b => b.durum === 'Hazırlandı').length} bankaya gidecek) · ${satisListe().filter(s => !s.dosyaId).length} satışın dosyası yok`, 'tahsilat', {}]
    ];
    const acikKart = kpi.acik || '';
    $('#ibKpi').innerHTML = K.map((k, i) => `<div class="ib-kpi${acikKart === k[1] ? ' acik' : ''}" style="--k:${k[0]}" data-i="${i}" data-sekme="${k[4]}" data-suzgec="${esc(JSON.stringify(k[5]))}" title="${esc(k[3])} — tıklayınca ${esc(({ liste: 'Beyanname / İBKB Dosyaları', ozet: 'Özet', dab: 'DAB Bağlantıları', vade: 'Vade Takibi', tahsilat: 'Banka Tahsilatları', belgeler: 'İBKB Belgeleri', satis: 'İhracat Satışları' })[k[4]])} sekmesi bu süzgeçle açılır"><div class="l">${esc(k[1])}</div><div class="v">${esc(k[2])}</div><div class="s">${esc(k[3])}</div></div>`).join('');
  }   // (sekme şeridindeki "334 / 334 dosya · …" kapsam yazısı 11 Kas 2026'da kullanıcı isteğiyle kaldırıldı)
  function tablo(p, basliklar, satirlar, dip, bosMetin) {
    const t = $('#ibTablo-' + p); if (!t) return;
    t.tHead.innerHTML = '<tr>' + basliklar.map(b => `<th>${esc(b)}</th>`).join('') + '</tr>';
    t.tBodies[0].innerHTML = satirlar.length ? satirlar.join('') : `<tr><td colspan="${basliklar.length}" class="na-bos text-center">${bosMetin || 'Filtreye uyan dosya yok.'}</td></tr>`;
    t.tFoot.innerHTML = satirlar.length && dip ? dip : '';
  }
  const notH = s => `<td class="metin"><small class="ib2-not" title="Tamamını görmek için tıklayın">${esc(s || '')}</small></td>`;
  const dosyaLink = d => `<a href="#" class="ib-duzenle" data-id="${d.id}" title="Dosyayı aç / düzenle">${esc(d.dosyaNo)}</a>`;
  const musteriH = d => `${esc(d.musteri || '—')}${d.musteriKodu ? `<small>${esc(d.musteriKodu)}</small>` : ''}`;

  // ---------- Özet
  function cizOzet(L) {
    const G = gecerli(L);
    // şirket × yıl
    const gr = [...new Set(G.map(d => d.sirket + '|' + d.yil))].sort().map(k => { const [s, y] = k.split('|'); const l = G.filter(d => d.sirket === s && String(d.yil) === y); return { s, y, l }; });
    const say = (l, f) => l.filter(f).length;
    tablo('ozetSirket', ['Şirket', 'Yıl', 'Dosya', 'Yapıldı', 'Gerek Yok', 'Kısmi', 'Bekliyor', 'Vadesi Geçti', 'Beyanname Tutarı', 'Gereken', 'Getirilen', 'Kalan'],
      gr.map(g => `<tr><td class="ad">${esc(g.s)}</td><td class="sayac">${g.y}</td><td class="sayac">${g.l.length}</td><td class="sayac">${say(g.l, d => d.dabDurumu === 'Yapıldı' || d.dabDurumu === 'Peşin Yapıldı')}</td><td class="sayac">${say(g.l, d => d.dabDurumu === 'Gerek Yok')}</td><td class="sayac">${say(g.l, d => d.dabDurumu === 'Kısmi')}</td><td class="sayac">${say(g.l, d => d.dabDurumu === 'Bekliyor')}</td><td class="sayac">${say(g.l, d => d.takip === 'Vadesi Geçti')}</td><td class="para">${dovizH(g.l, 'tutar')}</td><td class="para">${dovizH(g.l, 'gereken')}</td><td class="para">${dovizH(g.l, 'getirilen')}</td><td class="para genel">${dovizH(g.l, 'kalan')}</td></tr>`),
      `<tr><td colspan="2">Toplam · ${gr.length} şirket-yıl</td><td class="sayac">${G.length}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Yapıldı' || d.dabDurumu === 'Peşin Yapıldı')}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Gerek Yok')}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Kısmi')}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Bekliyor')}</td><td class="sayac">${say(G, d => d.takip === 'Vadesi Geçti')}</td><td class="para">${dovizH(G, 'tutar')}</td><td class="para">${dovizH(G, 'gereken')}</td><td class="para">${dovizH(G, 'getirilen')}</td><td class="para genel">${dovizH(G, 'kalan')}</td></tr>`);
    $('#ibNot-ozetSirket').textContent = `${gr.length} şirket-yıl · iptal dosyalar dışarıda`;
    // döviz
    const dv = [...new Set(G.map(d => d.doviz || '?'))].sort();
    tablo('ozetDoviz', ['Döviz', 'Dosya', 'Beyanname Tutarı', 'Gereken Bedel', 'Getirilen', 'Kalan', 'Kapandı', 'Açık', 'Muaf'],
      dv.map(pb => { const l = G.filter(d => (d.doviz || '?') === pb); return `<tr><td class="ad">${pb === '?' ? dovizRz('') : rz(pb, 'teal')}</td><td class="sayac">${l.length}</td><td class="para">${tutar(l.reduce((s, d) => s + n0(d.tutar), 0), pb)}</td><td class="para">${tutar(l.reduce((s, d) => s + n0(d.gereken), 0), pb)}</td><td class="para">${tutar(l.reduce((s, d) => s + n0(d.getirilen), 0), pb)}</td><td class="para genel">${tutar(l.reduce((s, d) => s + n0(d.kalan), 0), pb)}</td><td class="sayac">${say(l, d => d.takip === 'Kapandı')}</td><td class="sayac">${say(l, d => d.takip === 'Açık' || d.takip === 'Vade Yaklaşıyor' || d.takip === 'Vadesi Geçti' || d.takip === 'Kısmi')}</td><td class="sayac">${say(l, d => d.takip === 'Muaf')}</td></tr>`; }),
      `<tr><td>Toplam</td><td class="sayac">${G.length}</td><td colspan="4"><small>her döviz kendi satırında · kur uygulanmaz</small></td><td class="sayac">${say(G, d => d.takip === 'Kapandı')}</td><td class="sayac">${say(G, d => /Açık|Vade|Kısmi/.test(d.takip))}</td><td class="sayac">${say(G, d => d.takip === 'Muaf')}</td></tr>`);
    $('#ibNot-ozetDoviz').textContent = `${dv.length} döviz · tutarlar kendi para biriminde`;
    // durum dağılımı
    const satirD = (etiket, f, rzH) => { const l = G.filter(f); return `<tr><td class="ad">${rzH}</td><td class="sayac">${l.length}</td><td>${G.length ? Math.round(l.length / G.length * 100) : 0} %</td><td class="para">${dovizH(l, 'tutar', true)}</td><td class="para">${dovizH(l, 'kalan', true)}</td></tr>`; };
    tablo('ozetDurum', ['Durum', 'Dosya', 'Pay', 'Beyanname Tutarı', 'Kalan'],
      [...V.dabDurumlari.filter(x => x !== 'İptal').map(x => satirD(x, d => d.dabDurumu === x, dabRz(x))), `<tr><td colspan="5" style="background:#f8fafc;font-weight:800;font-size:.6rem;text-align:left">TAKİP DURUMU (vade = başlangıç + ${V.vadeGun} gün)</td></tr>`, ...V.takipDurumlari.filter(x => x !== 'İptal').map(x => satirD(x, d => d.takip === x, takipRz(x)))],
      `<tr><td>Toplam</td><td class="sayac">${G.length}</td><td>100 %</td><td class="para">${dovizH(G, 'tutar', true)}</td><td class="para">${dovizH(G, 'kalan', true)}</td></tr>`);
    $('#ibNot-ozetDurum').textContent = `DAB / İBKB durumu bankadan; takip durumu vade ve bedele göre hesaplanır`;
    // aylık
    const aylar = [...new Set(G.filter(d => d.baslangic).map(d => d.baslangic.slice(0, 7)))].sort();
    tablo('ozetAy', ['Ay', 'Dosya', 'Yapıldı', 'Gerek Yok', 'Bekliyor', 'Beyanname Tutarı', 'Kalan'],
      aylar.map(a => { const l = G.filter(d => d.baslangic && d.baslangic.slice(0, 7) === a); const [y, m] = a.split('-'); return `<tr><td class="ad">${AYLAR[+m - 1]} ${y}</td><td class="sayac">${l.length}</td><td class="sayac">${say(l, d => d.dabDurumu === 'Yapıldı' || d.dabDurumu === 'Peşin Yapıldı')}</td><td class="sayac">${say(l, d => d.dabDurumu === 'Gerek Yok')}</td><td class="sayac">${say(l, d => d.dabDurumu === 'Bekliyor' || d.dabDurumu === 'Kısmi')}</td><td class="para">${dovizH(l, 'tutar', true)}</td><td class="para">${dovizH(l, 'kalan', true)}</td></tr>`; }),
      `<tr><td>Toplam · ${aylar.length} ay</td><td class="sayac">${G.filter(d => d.baslangic).length}</td><td colspan="3"><small>${G.filter(d => !d.baslangic).length} dosyada başlangıç tarihi yok</small></td><td class="para">${dovizH(G.filter(d => d.baslangic), 'tutar', true)}</td><td class="para">${dovizH(G.filter(d => d.baslangic), 'kalan', true)}</td></tr>`);
    $('#ibNot-ozetAy').textContent = 'dosya başlangıç (fiili ihraç) ayına göre';

    // ---- ihracata dair öbür özetler (11 Kas 2026, "özet kısmına yıl yıl satış temsilcisine göre ihracat rakamları, satış ve tahsilat özetleri gelsin"): satış / tahsilat / belge / cari bakiye süzgeçleri şirket · yıl · döviz · arama (DAB durumu süzgeci yalnız dosyalara)
    const sYil = s => (s.faturaTarihi || s.tarih || '').slice(0, 4);
    const S = V.satislar.filter(s => (!F.sirket || s.sirket === F.sirket) && (!F.yil || sYil(s) === F.yil) && (!F.doviz || s.doviz === F.doviz) && ara([s.no, s.faturaNo, s.cari, s.cariKodu, s.ulke, s.dosyaNo, s.temsilci]));
    const T = tahsilatTum(), B = V.belgeler.filter(b => (!F.sirket || b.sirket === F.sirket) && (!F.yil || (b.tarih || '').slice(0, 4) === F.yil) && (!F.doviz || b.doviz === F.doviz) && ara([b.no, b.banka, b.dosyaNo, b.musteri, b.bankaIbkbNo]));
    const C = (V.cariBakiyeler || []).filter(c => (!F.sirket || c.sirket === F.sirket) && (!F.doviz || c.dovizler.some(d => d.pb === F.doviz)) && ara([c.kod, c.ad, c.resmiUnvan, c.ulke, c.sirket]));
    const usdTop = l => l.reduce((a, d) => a + n0(d.usdKarsiligi), 0), acikMi = d => /Açık|Vade|Kısmi|Bedel/.test(d.takip || '');
    const yilSirala = (a, b) => b.y.localeCompare(a.y);

    // Yıl × Satış Temsilcisi: İBKB dosyaları (temsilci dosyadan ya da müşteri cari kartından) + aynı yılın yurtdışı satış siparişleri
    const tAd = t => t || 'Atanmadı', tk = new Map(), tKey = (t, y) => { const k = tAd(t) + '|' + y; if (!tk.has(k)) tk.set(k, { t: tAd(t), y: String(y), d: [], s: [] }); return tk.get(k); };
    G.forEach(d => tKey(d.temsilci, d.yil).d.push(d)); S.forEach(s => tKey(s.temsilci, sYil(s)).s.push(s));
    const tRows = [...tk.values()].sort((a, b) => yilSirala(a, b) || (a.t === 'Atanmadı') - (b.t === 'Atanmadı') || usdTop(b.d) - usdTop(a.d) || a.t.localeCompare(b.t, 'tr'));   // yıl yeni → eski, temsilciler ABD $ toplamına göre, Atanmadı en sonda
    const musteriSay = g => new Set([...g.d.map(d => d.musteriKodu || d.musteri), ...g.s.map(s => s.cariKodu || s.cari)].filter(Boolean)).size;
    tablo('ozetTemsilci', ['Yıl', 'Satış Temsilcisi', 'İhracat Dosyası', 'Müşteri', 'Beyanname Tutarı', 'ABD $ Karşılığı', 'Gereken', 'Getirilen', 'Kalan', 'Açık Dosya', 'Sipariş', 'Sipariş Tutarı'],
      tRows.map(g => `<tr class="${g.t === 'Atanmadı' ? 'yakin' : ''}"><td class="sayac">${g.y}</td><td class="ad">${g.t === 'Atanmadı' ? '<span class="ib2-rz bekle" title="Dosyada ve müşteri cari kartında satış temsilcisi yok; dosyayı açıp Satış Temsilcisi alanını doldurun">Atanmadı</span>' : esc(g.t)}</td><td class="sayac">${g.d.length || bos}</td><td class="sayac">${musteriSay(g)}</td><td class="para">${dovizH(g.d, 'tutar')}</td><td class="para genel">${g.d.length ? tutar(usdTop(g.d), 'USD') : bos}</td><td class="para">${dovizH(g.d, 'gereken')}</td><td class="para">${dovizH(g.d, 'getirilen')}</td><td class="para">${dovizH(g.d, 'kalan')}</td><td class="sayac">${say(g.d, acikMi) || bos}</td><td class="sayac">${g.s.length || bos}</td><td class="para">${dovizH(g.s, 'tutar')}</td></tr>`),
      `<tr><td colspan="2">Toplam · ${new Set(tRows.map(g => g.t)).size} temsilci · ${new Set(tRows.map(g => g.y)).size} yıl</td><td class="sayac">${G.length}</td><td class="sayac">${new Set([...G.map(d => d.musteriKodu || d.musteri), ...S.map(s => s.cariKodu || s.cari)].filter(Boolean)).size}</td><td class="para">${dovizH(G, 'tutar')}</td><td class="para genel">${tutar(usdTop(G), 'USD')}</td><td class="para">${dovizH(G, 'gereken')}</td><td class="para">${dovizH(G, 'getirilen')}</td><td class="para">${dovizH(G, 'kalan')}</td><td class="sayac">${say(G, acikMi)}</td><td class="sayac">${S.length}</td><td class="para">${dovizH(S, 'tutar')}</td></tr>`,
      'Dosya ya da satış yok.');
    $('#ibNot-ozetTemsilci').textContent = `${say(G, d => !d.temsilci)} dosyada temsilci atanmadı · ABD $ karşılığı dönem kuruyla · sipariş sütunları Yurtdışı Satış Siparişi'nden`;

    // İhracat Satışları özeti: yıl × fatura kesen şirket (Yurtdışı Satış Siparişi; yüklenmiş / faturalanmış)
    const sk = [...new Set(S.map(s => sYil(s) + '|' + s.sirket))].map(k => { const [y, sr] = k.split('|'); return { y, sr, l: S.filter(s => sYil(s) === y && s.sirket === sr) }; }).sort((a, b) => yilSirala(a, b) || a.sr.localeCompare(b.sr, 'tr'));
    const tl = l => l.reduce((a, s) => a + n0(s.tutarTl), 0), kg = l => l.reduce((a, s) => a + n0(s.kg), 0);
    tablo('ozetSatis', ['Yıl', 'Şirket', 'Sipariş', 'Yüklendi', 'Faturalandı', 'Müşteri', 'Satış Tutarı', 'TL Karşılığı', 'Miktar (kg)', 'İBKB Dosyası Var', 'Dosyası Yok', 'Eşik Altı'],
      sk.map(g => `<tr><td class="sayac">${g.y || '—'}</td><td class="ad">${esc(g.sr || '—')}</td><td class="sayac">${g.l.length}</td><td class="sayac">${say(g.l, s => s.durum === 'Yüklendi')}</td><td class="sayac">${say(g.l, s => s.durum === 'Faturalandı')}</td><td class="sayac">${new Set(g.l.map(s => s.cariKodu || s.cari)).size}</td><td class="para">${dovizH(g.l, 'tutar')}</td><td class="para genel">${tutar(tl(g.l), 'TRY')}</td><td class="sayac">${kg(g.l).toLocaleString('tr-TR', { maximumFractionDigits: 0 })}</td><td class="sayac">${say(g.l, s => s.dosyaId)}</td><td class="sayac">${say(g.l, s => !s.dosyaId) ? `<a href="#" class="ib-git" data-p="satis" title="İhracat Satışları sekmesi">${say(g.l, s => !s.dosyaId)}</a>` : bos}</td><td class="sayac">${say(g.l, s => s.esikAlti) || bos}</td></tr>`),
      `<tr><td colspan="2">Toplam · ${sk.length} yıl-şirket</td><td class="sayac">${S.length}</td><td class="sayac">${say(S, s => s.durum === 'Yüklendi')}</td><td class="sayac">${say(S, s => s.durum === 'Faturalandı')}</td><td class="sayac">${new Set(S.map(s => s.cariKodu || s.cari)).size}</td><td class="para">${dovizH(S, 'tutar')}</td><td class="para genel">${tutar(tl(S), 'TRY')}</td><td class="sayac">${kg(S).toLocaleString('tr-TR', { maximumFractionDigits: 0 })}</td><td class="sayac">${say(S, s => s.dosyaId)}</td><td class="sayac">${say(S, s => !s.dosyaId)}</td><td class="sayac">${say(S, s => s.esikAlti)}</td></tr>`,
      'Yüklenmiş / faturalanmış yurtdışı satış yok.');
    $('#ibNot-ozetSatis').textContent = `${S.length} sipariş · Yurtdışı Satış Siparişi'nden canlı · yıl fatura tarihine göre`;

    // Banka tahsilatı · İBKB özeti: yıl × banka
    const bk = [...new Set([...T.map(t => (t.tarih || '').slice(0, 4) + '|' + t.banka), ...B.map(b => (b.tarih || '').slice(0, 4) + '|' + b.banka)])].map(k => { const [y, bn] = k.split('|'); return { y, bn, t: T.filter(t => (t.tarih || '').slice(0, 4) === y && t.banka === bn), b: B.filter(b => (b.tarih || '').slice(0, 4) === y && b.banka === bn) }; }).sort((a, b) => yilSirala(a, b) || a.bn.localeCompare(b.bn, 'tr'));
    const acikT = l => l.filter(t => t.durum === 'Eşleşmedi');
    tablo('ozetTahsilat', ['Yıl', 'Banka', 'Tahsilat', 'Tahsilat Tutarı', 'Açık (Eşleşmedi)', 'Açık Tutar', 'Eşleşti', 'İBKB Düzenlendi', 'İBKB Belgesi', 'Bankaya Gidecek', 'Banka Onayladı', 'Belge Tutarı'],
      bk.map(g => `<tr class="${acikT(g.t).length ? 'yakin' : ''}"><td class="sayac">${g.y || '<span class="ib2-rz gri" title="Excel\'den bağlanan DAB satırlarında tarih yok">Tarihsiz</span>'}</td><td class="ad">${g.bn ? esc(g.bn) : '<span class="ib2-rz gri" title="Excel\'den bağlanan DAB satırlarında banka yok">Banka Belirsiz</span>'}</td><td class="sayac">${g.t.length || bos}</td><td class="para">${dovizH(g.t, 'tutar')}</td><td class="sayac">${acikT(g.t).length ? `<a href="#" class="ib-git" data-p="tahsilat" title="Banka Tahsilatları (Açık)">${acikT(g.t).length}</a>` : bos}</td><td class="para">${dovizH(acikT(g.t), 'tutar')}</td><td class="sayac">${say(g.t, t => t.durum === 'Eşleşti') || bos}</td><td class="sayac">${say(g.t, t => t.durum === 'İBKB Düzenlendi') || bos}</td><td class="sayac">${say(g.b, b => b.durum !== 'İptal') || bos}</td><td class="sayac">${say(g.b, b => b.durum === 'Hazırlandı') || bos}</td><td class="sayac">${say(g.b, b => b.durum === 'Banka Onayladı') || bos}</td><td class="para">${dovizH(g.b.filter(b => b.durum !== 'İptal'), 'tutar')}</td></tr>`),
      `<tr><td colspan="2">Toplam · ${new Set(bk.map(g => g.bn)).size} banka</td><td class="sayac">${T.length}</td><td class="para">${dovizH(T, 'tutar')}</td><td class="sayac">${acikT(T).length}</td><td class="para">${dovizH(acikT(T), 'tutar')}</td><td class="sayac">${say(T, t => t.durum === 'Eşleşti')}</td><td class="sayac">${say(T, t => t.durum === 'İBKB Düzenlendi')}</td><td class="sayac">${say(B, b => b.durum !== 'İptal')}</td><td class="sayac">${say(B, b => b.durum === 'Hazırlandı')}</td><td class="sayac">${say(B, b => b.durum === 'Banka Onayladı')}</td><td class="para">${dovizH(B.filter(b => b.durum !== 'İptal'), 'tutar')}</td></tr>`,
      'Banka tahsilatı yok.');
    $('#ibNot-ozetTahsilat').textContent = `${T.length} tahsilat (${acikT(T).length} açık) · ${say(B, b => b.durum !== 'İptal')} İBKB belgesi · bankadan gelen döviz girişleri`;

    // Ülke bazında ihracat (dosyalar): Ek-2 muaf · Ek-3 %50 kuralıyla
    const uk = [...new Set(G.map(d => d.ulke || 'Belirsiz'))].map(u => ({ u, l: G.filter(d => (d.ulke || 'Belirsiz') === u) })).sort((a, b) => usdTop(b.l) - usdTop(a.l) || a.u.localeCompare(b.u, 'tr'));
    const kural = u => V.ulkeMuaf.includes(u) ? '<span class="ib2-rz gri">Ek-2 Muaf</span>' : V.ulkeYarim.includes(u) ? '<span class="ib2-rz mor">Ek-3 %50</span>' : u === 'Belirsiz' ? '<span class="ib2-rz bekle">Ülke Yok</span>' : '<span class="ib2-rz teal">Tam</span>';
    tablo('ozetUlke', ['Ülke', 'Kural', 'Dosya', 'Müşteri', 'Beyanname Tutarı', 'ABD $ Karşılığı', 'Gereken', 'Getirilen', 'Kalan', 'Açık', 'Vadesi Geçti'],
      uk.map(g => `<tr><td class="ad">${esc(g.u)}</td><td>${kural(g.u)}</td><td class="sayac">${g.l.length}</td><td class="sayac">${new Set(g.l.map(d => d.musteriKodu || d.musteri)).size}</td><td class="para">${dovizH(g.l, 'tutar')}</td><td class="para genel">${tutar(usdTop(g.l), 'USD')}</td><td class="para">${dovizH(g.l, 'gereken')}</td><td class="para">${dovizH(g.l, 'getirilen')}</td><td class="para">${dovizH(g.l, 'kalan')}</td><td class="sayac">${say(g.l, acikMi) || bos}</td><td class="sayac">${say(g.l, d => d.takip === 'Vadesi Geçti') || bos}</td></tr>`),
      `<tr><td colspan="2">Toplam · ${uk.length} ülke</td><td class="sayac">${G.length}</td><td class="sayac">${new Set(G.map(d => d.musteriKodu || d.musteri)).size}</td><td class="para">${dovizH(G, 'tutar')}</td><td class="para genel">${tutar(usdTop(G), 'USD')}</td><td class="para">${dovizH(G, 'gereken')}</td><td class="para">${dovizH(G, 'getirilen')}</td><td class="para">${dovizH(G, 'kalan')}</td><td class="sayac">${say(G, acikMi)}</td><td class="sayac">${say(G, d => d.takip === 'Vadesi Geçti')}</td></tr>`);
    $('#ibNot-ozetUlke').textContent = `${uk.length} ülke · ${say(G, d => !d.ulke)} dosyada ülke yok · ülke dosyadan ya da müşteri cari kartından`;

    // Cari bakiye özeti (mizan): döviz bazında yurtdışı müşteri bakiyeleri ↔ İBKB dosya kalanı
    const pbs = [...new Set(C.flatMap(c => c.dovizler.map(d => d.pb)))].sort((a, b) => (a === 'TRY') - (b === 'TRY') || a.localeCompare(b));
    const dvs = pb => C.flatMap(c => c.dovizler.filter(d => d.pb === pb)), top = (l, f) => l.reduce((a, d) => a + n0(d[f]), 0);
    tablo('ozetBakiye', ['Döviz', 'Cari', 'Net Bakiye', 'TL Karşılığı', 'Açık Fatura', 'Açık Fatura Tutarı', 'Dosya Açık Kalanı', 'Fark (Bakiye − Dosya)', 'Eski Bakiye'],
      pbs.map(pb => { const l = dvs(pb); return `<tr><td class="ad">${rz(pb, 'teal')}</td><td class="sayac">${l.length}</td><td class="para genel">${tutar(top(l, 'net'), pb)}</td><td class="para">${tutar(top(l, 'tl'), 'TRY')}</td><td class="sayac">${top(l, 'acikFaturaAdet') || bos}</td><td class="para">${tutar(top(l, 'acikFaturaTutar'), pb)}</td><td class="para">${pb === 'TRY' ? bos : tutar(top(l, 'dosyaKalan'), pb)}</td><td class="para">${pb === 'TRY' ? bos : `<span style="font-weight:800;color:${top(l, 'fark') > 0.005 ? '#b91c1c' : top(l, 'fark') < -0.005 ? '#b45309' : '#15803d'}">${tutar(top(l, 'fark'), pb)}</span>`}</td><td class="sayac">${say(l, d => d.eskiBakiyeUyari) || bos}</td></tr>`; }),
      `<tr><td>Toplam</td><td class="sayac">${C.length} cari</td><td colspan="1"><small>${say(C, c => c.kontrolDurumu === 'Kontrol Yok' || c.kontrolDurumu === 'Kontrol Gecikti')} kontrol bekliyor · ${say(C, c => c.farkVar)} fark var</small></td><td class="para">${tutar(C.reduce((a, c) => a + n0(c.bakiyeTl), 0), 'TRY')}</td><td class="sayac">${C.reduce((a, c) => a + n0(c.acikFaturaAdet), 0)}</td><td colspan="3"><small>${C.reduce((a, c) => a + n0(c.dosyaSayisi), 0)} İBKB dosyası bağlı · ${C.reduce((a, c) => a + n0(c.acikDosya), 0)} açık</small></td><td class="sayac">${say(C, c => c.eskiBakiyeUyari)}</td></tr>`,
      'Yurtdışı müşteri carisi yok.');
    $('#ibNot-ozetBakiye').textContent = `${C.length} yurtdışı müşteri carisi · bakiye zamanı ${V.bakiyeZamani || ''} · ayrıntı Cari Bakiyeler (Mizan) sekmesinde`;

    // Yurtdışı şube bakiyeleri (kendi şubelerimiz; kullanıcı: "şube detaylarını da özet kısmında görebilelim"): şubeye tıklayınca Şube Bakiyeleri sekmesinde detay
    const SB = subeListe(true), SBD = subeDv(SB);
    tablo('ozetSube', ['Şube', 'Ülke', 'Grup Şirketi', 'Satış Temsilcisi', 'Dönem', 'Satış', 'Toplam Ödeme', 'Bakiye (Devir)', 'Son Yıl Farkı', 'Son Ekstre'],
      SB.map(s => `<tr class="tik"><td class="ad"><a href="#" class="ib-sube-git" data-id="${s.id}" title="Şube detayı">${esc(s.sube)}</a></td><td>${esc(s.ulke)}</td><td>${esc(s.sirket || '—')}</td><td>${s.temsilci ? esc(s.temsilci) : '<span class="ib2-rz bekle">Atanmadı</span>'}</td><td class="sayac">${s.yilSayisi ? `${esc(s.ilkYil)} – ${esc(s.sonYil)}` : '—'}</td><td class="para">${tutar(s.satisEsas, s.pb)}</td><td class="para">${tutar(s.toplamOdeme, s.pb)}</td><td class="para genel">${isaretli(s.bakiye, s.pb)}</td><td class="para">${s.yilSayisi ? isaretli(s.sonYilFark, s.pb) : bos}</td><td>${sonEkstreH(s)}</td></tr>`),
      `<tr><td colspan="5">Toplam · ${SB.length} şube · ${new Set(SB.map(s => s.ulke)).size} ülke</td><td class="para">${dovizH(SBD, 'satisEsas')}</td><td class="para">${dovizH(SBD, 'toplamOdeme')}</td><td class="para genel">${dovizH(SBD, 'bakiye')}</td><td class="para">${dovizH(SBD, 'sonYilFark')}</td><td><small>${SB.filter(s => !s.sonGonderim || gunFarki(s.sonGonderim.tarih) > 90).length} şubeye ekstre bekliyor</small></td></tr>`,
      'Şube yok.');
    $('#ibNot-ozetSube').textContent = `${SB.length} şube · eksi bakiye: şube bize borçlu · ayrıntı Şube Bakiyeleri sekmesinde`;

    // Bekleyen görevler + düzeltme performansı (12 Kas 2026; "rapor sayfamızda bekleyen görevler gibi bir şey olsun … performans takibi")
    const GO = gorevListe('acik'); gorevTablo('ozetGorev', GO, true); perfTablo('ozetGorevPerf');
    $('#ibNot-ozetGorev').textContent = `${GO.length} açık görev · ${GO.filter(g => g.durum === 'Düzeltildi').length} muhasebe onayı bekliyor · ${GO.filter(g => g.gecikti).length} termini geçti · ayrıntı Cari Bakiyeler sekmesinde`;
    $('#ibNot-ozetGorevPerf').textContent = `${(V.gorevPerformans || []).length} kişi · atama → düzeltme → onay süreleri`;

  }

  // ---------- Dosya Listesi
  function cizListe(L) {
    const bas = ['Şirket', 'Yıl', 'Dosya No', 'Müşteri', 'Başlangıç', 'Kapanış', 'GÇB (Beyanname) No', 'Beyanname Tutarı', 'Gerçek İhracat Bedeli (Proforma / Fatura)', 'Beyanname − Gerçek Fark', 'Döviz', 'Ülke Gerekliliği', 'Gereken Bedel (Beyanname)', 'Gereken (Gerçek Bedel)', 'DAB / İBKB Durumu', 'Banka', 'Getirilen', 'Kalan (Beyanname)', 'Kalan (Gerçek)', 'Vade (180 Gün)', 'Kalan Gün', 'Takip', 'Not', 'İşlem'];
    const rows = L.map(d => `<tr class="${sinif(d)}" data-id="${d.id}"><td class="ad">${esc(d.sirket)}</td><td class="sayac">${d.yil}</td><td class="ad">${dosyaLink(d)}</td><td class="metin">${musteriH(d)}</td><td>${tarih(d.baslangic)}</td><td>${tarih(d.kapanis)}</td><td>${esc(d.beyannameNo || '—')}</td><td class="para">${d.tutar ? tutar(d.tutar, d.doviz) : bos}</td><td class="para">${d.gercekBedel ? tutar(d.gercekBedel, d.doviz) : (d.tutar ? `<small title="Gerçek bedel girilmedi; beyanname ile aynı kabul">= beyanname</small>` : bos)}</td><td>${farkH(d)}</td><td>${dovizRz(d.doviz)}</td><td>${n0(d.ulkeOran).toLocaleString('tr-TR')} %</td><td class="para">${d.gereken ? tutar(d.gereken, d.doviz) : bos}</td><td class="para">${d.bedelFarki ? tutar(d.gerekenGercek, d.doviz) : bos}</td><td>${dabRz(d.dabDurumu)}${d.esikAlti ? `<small title="ABD doları karşılığı ${para(d.usdKarsiligi)} $ ≤ ${kisaUsd(V.esikUsd)} eşiği: DAB / İBKB gerekmez">eşik altı · ${kisaUsd(d.usdKarsiligi)}</small>` : d.dabNotu && d.dabNotu.toUpperCase() !== d.dabDurumu.toUpperCase() ? `<small>${esc(d.dabNotu.slice(0, 40))}</small>` : ''}</td><td>${esc(d.dabBanka || [...new Set(d.dablar.map(p => p.banka).filter(Boolean))].join(', ') || '—')}</td><td class="para">${d.getirilen ? tutar(d.getirilen, d.doviz) + (d.dablar.length ? `<small>${d.dablar.length} DAB satırı</small>` : '') : bos}</td><td class="para genel">${d.kalan ? tutar(d.kalan, d.doviz) + (d.beyannameFazlasi ? `<small>beyanname fazlası ${tutar(d.beyannameFazlasi, d.doviz)}</small>` : '') : (d.takip === 'Kapandı' ? '<small>tamamlandı</small>' : bos)}</td><td class="para">${d.bedelFarki ? (d.kalanGercek ? tutar(d.kalanGercek, d.doviz) : '<small>gerçek bedel tamam</small>') : bos}</td><td>${tarih(d.vade)}</td><td>${d.kalanGun === null || d.kalanGun === undefined || d.takip === 'Kapandı' || d.takip === 'Muaf' || d.takip === 'İptal' ? bos : `<b style="color:${d.kalanGun < 0 ? '#b91c1c' : d.kalanGun <= 30 ? '#b45309' : '#166534'}">${d.kalanGun}</b>`}</td><td>${takipRz(d.takip)}</td>${notH([d.aciklama && d.aciklama !== d.musteri ? d.aciklama : '', d.not].filter(Boolean).join(' · '))}<td><button type="button" class="btn btn-light border ib-duzenle" data-id="${d.id}" title="Düzenle"><i class="fas fa-pen"></i></button> <button type="button" class="btn btn-light border text-danger ib-sil" data-id="${d.id}" title="Kaydı sil"><i class="fas fa-trash"></i></button></td></tr>`);
    const G = gecerli(L);
    const farkli = G.filter(d => d.bedelFarki);
    const dip = `<tr><td colspan="7">Toplam · ${L.length} dosya · ${L.length - G.length} iptal · ${[...new Set(L.map(d => d.musteriKodu || d.musteri))].length} müşteri · ${farkli.length} dosyada beyanname ≠ gerçek bedel</td><td class="para">${dovizH(G, 'tutar')}</td><td class="para">${dovizH(G, 'gercekBedelEtkin')}</td><td class="para">${farkli.length ? dovizH(farkli, 'bedelFarki') : bos}</td><td colspan="2"></td><td class="para">${dovizH(G, 'gereken')}</td><td class="para">${dovizH(G, 'gerekenGercek')}</td><td colspan="2"><small>${G.filter(d => d.dabDurumu === 'Yapıldı' || d.dabDurumu === 'Peşin Yapıldı').length} yapıldı · ${G.filter(d => d.dabDurumu === 'Gerek Yok').length} gerek yok · ${G.filter(d => d.dabDurumu === 'Bekliyor').length} bekliyor · ${G.filter(d => d.dabDurumu === 'Kısmi').length} kısmi</small></td><td class="para">${dovizH(G, 'getirilen')}</td><td class="para">${dovizH(G, 'kalan')}${farkli.length ? `<small>beyanname fazlası ${kisaDoviz(G, 'beyannameFazlasi')}</small>` : ''}</td><td class="para">${dovizH(G, 'kalanGercek')}</td><td colspan="2"><small>${G.filter(d => d.takip === 'Vadesi Geçti').length} vadesi geçti · ${G.filter(d => d.takip === 'Vade Yaklaşıyor').length} yaklaşıyor · ${G.filter(d => d.takip === 'Bedel Farkı').length} bedel farkı</small></td><td colspan="3"></td></tr>`;
    tablo('liste', bas, rows, dip, 'Filtreye uyan ihracat dosyası yok. "İhracat Dosyası Ekle" ile yeni dosya girin.');
    $('#ibNot-liste').textContent = `${L.length} dosya · şirket, yıl ve dosya numarasına göre · dosya numarasına tıklayınca düzenleme açılır`;
  }

  // ---------- Müşteri Bazlı
  function cizMusteri(L) {
    const G = gecerli(L);
    const m = new Map(); G.forEach(d => { const k = d.musteriKodu || d.musteri.toLocaleUpperCase('tr-TR'); if (!m.has(k)) m.set(k, { kod: d.musteriKodu, ad: d.musteri, l: [] }); m.get(k).l.push(d); });
    const M = [...m.values()].sort((a, b) => b.l.length - a.l.length || a.ad.localeCompare(b.ad, 'tr'));
    const bas = ['Müşteri', 'Müşteri Kodu', 'Şirket', 'Satış Temsilcisi', 'Dosya', 'Yapıldı', 'Gerek Yok', 'Kısmi', 'Bekliyor', 'Vadesi Geçti', 'Beyanname Tutarı', 'Gerçek İhracat Bedeli', 'Beyanname − Gerçek Fark', 'Farklı Dosya', 'Gereken (Beyanname)', 'Gereken (Gerçek)', 'Getirilen', 'Kalan (Beyanname)', 'Kalan (Gerçek)', 'Beyanname Fazlası', 'İlk Dosya', 'Son Dosya', 'Bankalar'];
    const say = (l, f) => l.filter(f).length;
    const farkHucre = l => { const f = l.filter(d => d.bedelFarki); return f.length ? dovizH(f, 'bedelFarki') + `<small>${[...new Set(f.map(d => d.farkNedeni).filter(Boolean))].join(', ')}</small>` : bos; };
    const rows = M.map(x => { const ts = x.l.map(d => d.baslangic).filter(Boolean).sort(), fk = x.l.filter(d => d.bedelFarki); return `<tr class="tik ib-musteri-git${fk.length ? ' yakin' : ''}" data-ad="${esc(x.ad)}" title="${fk.length ? fk.length + ' dosyada beyanname ile gerçek bedel farklı' : ''}"><td class="ad">${esc(x.ad || '—')}</td><td>${esc(x.kod || '—')}</td><td>${[...new Set(x.l.map(d => d.sirket))].join(' · ')}</td><td>${(() => { const t = [...new Set(x.l.map(d => d.temsilci).filter(Boolean))]; return t.length ? esc(t.join(' · ')) + (x.l.some(d => !d.temsilci) ? '<small>bazı dosyalarda atanmadı</small>' : '') : '<span class="ib2-rz bekle" title="Dosyada ve müşteri cari kartında satış temsilcisi yok">Atanmadı</span>'; })()}</td><td class="sayac">${x.l.length}</td><td class="sayac">${say(x.l, d => d.dabDurumu === 'Yapıldı' || d.dabDurumu === 'Peşin Yapıldı')}</td><td class="sayac">${say(x.l, d => d.dabDurumu === 'Gerek Yok')}</td><td class="sayac">${say(x.l, d => d.dabDurumu === 'Kısmi')}</td><td class="sayac">${say(x.l, d => d.dabDurumu === 'Bekliyor')}</td><td class="sayac">${say(x.l, d => d.takip === 'Vadesi Geçti') || bos}</td><td class="para">${dovizH(x.l, 'tutar')}</td><td class="para">${dovizH(x.l, 'gercekBedelEtkin')}</td><td>${farkHucre(x.l)}</td><td class="sayac">${fk.length ? `<span class="ib2-rz mor">${fk.length}</span>` : bos}</td><td class="para">${dovizH(x.l, 'gereken')}</td><td class="para">${dovizH(x.l, 'gerekenGercek')}</td><td class="para">${dovizH(x.l, 'getirilen')}</td><td class="para genel">${dovizH(x.l, 'kalan')}</td><td class="para">${dovizH(x.l, 'kalanGercek')}</td><td class="para">${fk.length ? dovizH(x.l, 'beyannameFazlasi') : bos}</td><td>${tarih(ts[0])}</td><td>${tarih(ts[ts.length - 1])}</td><td>${[...new Set(x.l.flatMap(d => [d.dabBanka, ...d.dablar.map(p => p.banka)]).filter(Boolean))].join(', ') || bos}</td></tr>`; });
    const farkliG = G.filter(d => d.bedelFarki);
    const dip = `<tr><td colspan="3">Toplam · ${M.length} müşteri · ${[...new Set(farkliG.map(d => d.musteriKodu || d.musteri))].length} müşteride bedel farkı</td><td><small>${[...new Set(G.map(d => d.temsilci).filter(Boolean))].length} temsilci · ${M.filter(x => x.l.every(d => !d.temsilci)).length} müşteri atanmadı</small></td><td class="sayac">${G.length}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Yapıldı' || d.dabDurumu === 'Peşin Yapıldı')}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Gerek Yok')}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Kısmi')}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Bekliyor')}</td><td class="sayac">${say(G, d => d.takip === 'Vadesi Geçti')}</td><td class="para">${dovizH(G, 'tutar')}</td><td class="para">${dovizH(G, 'gercekBedelEtkin')}</td><td>${farkliG.length ? dovizH(farkliG, 'bedelFarki') : bos}</td><td class="sayac">${farkliG.length || bos}</td><td class="para">${dovizH(G, 'gereken')}</td><td class="para">${dovizH(G, 'gerekenGercek')}</td><td class="para">${dovizH(G, 'getirilen')}</td><td class="para genel">${dovizH(G, 'kalan')}</td><td class="para">${dovizH(G, 'kalanGercek')}</td><td class="para">${farkliG.length ? dovizH(G, 'beyannameFazlasi') : bos}</td><td colspan="3"></td></tr>`;
    tablo('musteri', bas, rows, dip, 'Filtreye uyan müşteri yok.');
    $('#ibNot-musteri').textContent = `${M.length} müşteri · dosya sayısına göre · satıra tıklayınca Dosya Listesi o müşteriye daralır · temsilci süzgeci üstte`;
  }

  // ---------- Vade Takibi
  function cizVade(L) {
    const A = gecerli(L).filter(d => d.takip === 'Açık' || d.takip === 'Vade Yaklaşıyor' || d.takip === 'Vadesi Geçti' || d.takip === 'Kısmi' || d.takip === 'Bedel Farkı').sort((a, b) => (a.vade || '9999').localeCompare(b.vade || '9999'));
    const bas = ['Takip', 'Kalan Gün', 'Vade (Başlangıç + 180)', 'Başlangıç', 'Şirket', 'Dosya No', 'Müşteri', 'GÇB No', 'Beyanname Tutarı', 'Gerçek İhracat Bedeli', 'Fark', 'Gereken (Beyanname)', 'Gereken (Gerçek)', 'Getirilen', 'Kalan (Beyanname)', 'Kalan (Gerçek)', 'DAB / İBKB Durumu', 'Banka', 'Not'];
    const rows = A.map(d => `<tr class="${sinif(d)}"><td>${takipRz(d.takip)}</td><td>${d.kalanGun === null || d.kalanGun === undefined ? '<span class="ib2-rz gri" title="Başlangıç tarihi yok">Tarih Yok</span>' : `<b style="color:${d.kalanGun < 0 ? '#b91c1c' : d.kalanGun <= 30 ? '#b45309' : '#166534'}">${d.kalanGun}</b>`}</td><td>${tarih(d.vade)}</td><td>${tarih(d.baslangic)}</td><td class="ad">${esc(d.sirket)}</td><td class="ad">${dosyaLink(d)}</td><td class="metin">${musteriH(d)}</td><td>${esc(d.beyannameNo || '—')}</td><td class="para">${tutar(d.tutar, d.doviz)}</td><td class="para">${d.gercekBedel ? tutar(d.gercekBedel, d.doviz) : '<small>= beyanname</small>'}</td><td>${farkH(d)}</td><td class="para">${tutar(d.gereken, d.doviz)}</td><td class="para">${d.bedelFarki ? tutar(d.gerekenGercek, d.doviz) : bos}</td><td class="para">${d.getirilen ? tutar(d.getirilen, d.doviz) : bos}</td><td class="para genel">${tutar(d.kalan, d.doviz)}${d.beyannameFazlasi ? `<small>beyanname fazlası ${tutar(d.beyannameFazlasi, d.doviz)}</small>` : ''}</td><td class="para">${d.bedelFarki ? (d.kalanGercek ? tutar(d.kalanGercek, d.doviz) : '<span class="ib2-rz ok">Gerçek Bedel Tamam</span>') : bos}</td><td>${dabRz(d.dabDurumu)}</td><td>${esc(d.dabBanka || '—')}</td>${notH([d.farkNedeni ? 'Fark: ' + d.farkNedeni : '', d.dabNotu, d.not].filter(Boolean).join(' · '))}</tr>`);
    const dip = `<tr><td colspan="8">Toplam · ${A.length} açık dosya · ${A.filter(d => d.takip === 'Vadesi Geçti').length} vadesi geçti · ${A.filter(d => d.takip === 'Vade Yaklaşıyor').length} 30 gün içinde · ${A.filter(d => d.takip === 'Bedel Farkı').length} bedel farkı · ${A.filter(d => !d.baslangic).length} tarihsiz</td><td class="para">${dovizH(A, 'tutar')}</td><td class="para">${dovizH(A, 'gercekBedelEtkin')}</td><td>${A.some(d => d.bedelFarki) ? dovizH(A.filter(d => d.bedelFarki), 'bedelFarki') : bos}</td><td class="para">${dovizH(A, 'gereken')}</td><td class="para">${dovizH(A, 'gerekenGercek')}</td><td class="para">${dovizH(A, 'getirilen')}</td><td class="para genel">${dovizH(A, 'kalan')}</td><td class="para">${dovizH(A, 'kalanGercek')}</td><td colspan="3"></td></tr>`;
    tablo('vade', bas, rows, dip, 'Açık dosya yok: tüm dosyalar kapandı ya da muaf.');
    $('#ibNot-vade').textContent = `${A.length} açık dosya · vadeye göre (en yakın üstte)`;
  }

  // ---------- DAB Bağlantıları
  function cizDab(L) {
    const S = []; L.forEach(d => d.dablar.forEach((p, i) => S.push({ d, p, i })));
    const bas = ['Şirket', 'Dosya No', 'Müşteri', 'GÇB No', 'Beyanname Tutarı', 'DAB Tutarı', 'DAB Dövizi', 'DAB Tarihi', 'Banka', 'DAB / İBKB No', 'Dosya Durumu', 'Not'];
    const rows = S.map(({ d, p }) => `<tr class="${sinif(d)}"><td class="ad">${esc(d.sirket)}</td><td class="ad">${dosyaLink(d)}</td><td class="metin">${musteriH(d)}</td><td>${esc(d.beyannameNo || '—')}</td><td class="para">${tutar(d.tutar, d.doviz)}</td><td class="para genel">${tutar(p.tutar, p.doviz)}${p.doviz !== d.doviz ? '<small>beyanname dövizinden farklı</small>' : ''}</td><td>${dovizRz(p.doviz)}</td><td>${tarih(p.tarih)}</td><td>${esc(p.banka || d.dabBanka || '—')}</td><td>${esc(p.no || '—')}</td><td>${dabRz(d.dabDurumu)}</td>${notH(p.not)}</tr>`);
    const dip = `<tr><td colspan="5">Toplam · ${S.length} DAB satırı · ${[...new Set(S.map(x => x.d.id))].length} dosya</td><td class="para genel">${dovizH(S.map(x => ({ doviz: x.p.doviz, t: x.p.tutar })), 't')}</td><td colspan="6"></td></tr>`;
    tablo('dab', bas, rows, dip, 'Bağlı DAB satırı yok. Dosya kaydında "Bağlanan DAB / İBKB" bölümünden tutar, döviz, banka ve tarih girin.');
    $('#ibNot-dab').textContent = `${S.length} DAB satırı · Excel'deki "… BAĞLANDI" notlarından ve ekrandan girilenlerden`;
  }

  // ---------- İhracat Satışları (Yurtdışı Satış Siparişi'nden canlı; 11 Kas 2026 "satışlar da düşsün")
  function cizSatis() {
    const S = satisListe();
    const bas = ['Şirket', 'Sipariş No', 'Durum', 'Fatura No', 'Fatura Tarihi', 'Yükleme', 'Müşteri', 'Ülke', 'Temsilci', 'Ödeme Şekli', 'Teslim', 'Tutar', 'Döviz', 'TL Karşılığı', 'İBKB Dosyası', 'Dosya Takip', 'Eşik', 'İşlem'];
    const rows = S.map(s => { const d = s.dosyaId ? V.dosyalar.find(x => x.id === s.dosyaId) : null; return `<tr class="${d ? sinif(d) : 'eslesmedi'}"><td class="ad">${esc(s.sirket)}</td><td class="ad">${esc(s.no || '—')}</td><td>${rz(s.durum, s.durum === 'Faturalandı' ? 'ok' : 'mavi')}</td><td>${esc(s.faturaNo || '—')}</td><td>${tarih(s.faturaTarihi)}</td><td>${tarih(s.yukleme)}</td><td class="metin">${esc(s.cari)}<small>${esc(s.cariKodu || '')}</small></td><td>${esc(s.ulke || '—')}</td><td>${esc(s.temsilci || '—')}</td><td>${esc(s.odemeSekli || '—')}</td><td>${esc(s.teslimSekli || '—')}</td><td class="para">${tutar(s.tutar, s.doviz)}</td><td>${dovizRz(s.doviz)}</td><td class="para">${tutar(s.tutarTl, 'TRY')}</td><td>${d ? `<a href="#" class="ib-duzenle" data-id="${d.id}">${esc(d.dosyaNo)}</a><small>${esc(d.beyannameNo || 'GÇB yok')}</small>` : '<span class="ib2-rz bekle" title="Bu fatura için beyanname / İBKB dosyası açılmamış">Dosya Yok</span>'}</td><td>${d ? takipRz(d.takip) : bos}</td><td>${s.esikAlti ? '<span class="ib2-rz gri" title="ABD doları karşılığı eşik altı: DAB / İBKB gerekmez">Eşik Altı</span>' : '<span class="ib2-rz teal">Bedel Getirilecek</span>'}</td><td>${d ? '' : `<button type="button" class="btn btn-light border ib-satis-dosya" data-id="${s.id}" title="Bu satıştan beyanname / İBKB dosyası aç (bilgiler dolu gelir)"><i class="fas fa-folder-plus"></i> Dosya Aç</button>`}</td></tr>`; });
    const dip = `<tr><td colspan="11">Toplam · ${S.length} ihracat satışı · ${S.filter(s => s.dosyaId).length} dosyalı · ${S.filter(s => !s.dosyaId).length} dosyasız · ${S.filter(s => s.esikAlti).length} eşik altı</td><td class="para">${dovizH(S, 'tutar')}</td><td></td><td class="para">${tutar(S.reduce((t, s) => t + n0(s.tutarTl), 0), 'TRY')}</td><td colspan="4"></td></tr>`;
    tablo('satis', bas, rows, dip, 'Filtreye uyan yüklenmiş / faturalanmış ihracat siparişi yok.');
    $('#ibNot-satis').textContent = `${S.length} satış · Yurtdışı Satış Siparişi'nden (yüklenmiş ve faturalanmış) canlı · "Dosya Aç" bilgileri doldurur`;
  }
  // ---------- Banka Tahsilatları ("tahsilat raporu bankalardan otomatik geliyor; hangi ihracat belge numarası ile eşleşecek seçimi")
  function cizTahsilat() {
    const T = tahsilatListe(), tam = tamamlananListe();
    $('#ibTahsilatOzet').innerHTML = `<b>${T.length} açık (eşleşmemiş) tahsilat</b> · ${kisaDoviz(T, 'tutar') || '—'} · bankadan gelen her döviz girişi bir ihracat dosyasına (GÇB) bağlanır; "Eşleştir" aday dosyaları (aynı şirket ve döviz, açık, tutarı yakın) sıralar, seçimde İBKB belgesi üretilip üretilmeyeceği sorulur. Eşleşen tahsilatlar <a href="#" class="ib-git" data-p="tamamlanan">Tamamlanan Tahsilatlar</a> sekmesine geçer (${tam.length} kayıt). Banka / ERP entegrasyonu bağlanınca tahsilatlar otomatik düşer; şimdilik "Banka Tahsilatı Ekle" ile girilir.`;
    const bas = ['Şirket', 'Banka', 'Tarih (Valör)', 'Gönderen', 'Müşteri Kodu', 'Tutar', 'Döviz', 'Referans / Dekont', 'Açıklama', 'Kaynak', 'Kayıt', 'İşlem'];
    const rows = T.map(t => `<tr class="eslesmedi"><td class="ad">${esc(t.sirket)}</td><td>${esc(t.banka || '—')}</td><td>${tarih(t.tarih)}</td><td class="metin">${esc(t.gonderen || '—')}</td><td>${esc(t.musteriKodu || '—')}</td><td class="para genel">${tutar(t.tutar, t.doviz)}</td><td>${dovizRz(t.doviz)}</td><td>${esc(t.referans || '—')}</td>${notH(t.aciklama)}<td>${rz(t.kaynak, t.kaynak === 'Banka (Otomatik)' ? 'teal' : 'gri')}</td><td>${tarih(t.olusturma)}<small>${esc(t.olusturan || '')}</small></td><td><button type="button" class="btn btn-primary ib-eslestir" data-id="${t.id}" title="İhracat dosyasıyla eşleştir"><i class="fas fa-link"></i> Eşleştir</button> <button type="button" class="btn btn-light border ib-tahsilat-duzenle" data-id="${t.id}" title="Düzenle"><i class="fas fa-pen"></i></button> <button type="button" class="btn btn-light border text-danger ib-tahsilat-sil" data-id="${t.id}" title="Sil"><i class="fas fa-trash"></i></button></td></tr>`);
    const dip = `<tr><td colspan="5">Toplam · ${T.length} açık tahsilat · ${[...new Set(T.map(t => t.banka))].filter(Boolean).length} banka</td><td class="para genel">${dovizH(T, 'tutar')}</td><td colspan="6"></td></tr>`;
    tablo('tahsilat', bas, rows, dip, 'Açık banka tahsilatı yok: gelen bedellerin tamamı ihracat dosyalarıyla eşleşti. Yeni tahsilat için "Banka Tahsilatı Ekle".');
    $('#ibNot-tahsilat').textContent = `${T.length} açık tahsilat · tarihe göre · eşleşince Tamamlanan Tahsilatlar'a geçer`;
  }
  // ---------- Tamamlanan Tahsilatlar: eşleşmiş tahsilat + dosya + İBKB belgesi tek satırda
  function cizTamamlanan() {
    const T = tamamlananListe();
    const bas = ['Durum', 'Şirket', 'Dosya No', 'GÇB No', 'Müşteri', 'Beyanname Tutarı', 'Dosya DAB Durumu', 'Banka', 'Tahsilat Tarihi', 'Tahsilat Tutarı', 'Referans', 'İBKB Belgesi', 'Belge Durumu', 'Banka İBKB No', 'Eşleştirme', 'Kaynak', 'İşlem'];
    const rows = T.map(t => { const d = V.dosyalar.find(x => x.id === t.dosyaId), b = V.belgeler.find(x => x.id === t.ibkbBelgeId); return `<tr class="${t.durum === 'İBKB Düzenlendi' ? 'kapandi' : ''}"><td>${tahsilatRz(t.durum)}</td><td class="ad">${esc(t.sirket)}</td><td class="ad">${d ? `<a href="#" class="ib-duzenle" data-id="${d.id}">${esc(d.dosyaNo)}</a>` : esc(t.dosyaNo || '—')}</td><td>${esc(t.beyannameNo || (d && d.beyannameNo) || '—')}</td><td class="metin">${d ? musteriH(d) : esc(t.gonderen || '—')}</td><td class="para">${d ? tutar(d.tutar, d.doviz) : bos}</td><td>${d ? dabRz(d.dabDurumu) + (d.kalan ? `<small>kalan ${tutar(d.kalan, d.doviz)}</small>` : '') : bos}</td><td>${esc(t.banka || '—')}</td><td>${tarih(t.tarih)}</td><td class="para genel">${tutar(t.tutar, t.doviz)}</td><td>${esc(t.referans || '—')}</td><td>${b ? `<a href="/IhracatFinans/Belge/${b.id}" target="_blank" rel="noopener" title="Bankaya verilecek formu aç">${esc(b.no)}</a>` : '<span class="ib2-rz gri" title="Eşleştirmede İBKB istenmedi">Belge Yok</span>'}</td><td>${b ? belgeRz(b.durum) : bos}</td><td>${b && b.bankaIbkbNo ? esc(b.bankaIbkbNo) + `<small>${tarih(b.bankaTarihi)}</small>` : bos}</td><td>${t.eslestirme ? tarih(t.eslestirme) + `<small>${esc(t.eslestiren || '')}</small>` : bos}</td><td>${rz(t.kaynak, t.kaynak === 'Banka (Otomatik)' ? 'teal' : 'gri')}</td><td>${b ? `<a class="btn btn-light border" href="/IhracatFinans/Belge/${b.id}" target="_blank" rel="noopener" title="Yazdır / PDF"><i class="fas fa-print"></i></a> <button type="button" class="btn btn-light border ib-belge-durum" data-id="${b.id}" title="Belge durumu · banka İBKB no"><i class="fas fa-pen"></i></button> ` : ''}<button type="button" class="btn btn-light border ib-eslesme-kaldir" data-id="${t.id}" title="Eşleşmeyi kaldır (DAB satırı silinir, belge iptal olur; tahsilat Açık sekmesine döner)"><i class="fas fa-link-slash"></i></button></td></tr>`; });
    const dip = `<tr><td colspan="5">Toplam · ${T.length} tamamlanan tahsilat · ${T.filter(t => t.durum === 'İBKB Düzenlendi').length} İBKB düzenlendi · ${T.filter(t => t.durum === 'Eşleşti').length} yalnız eşleşti · ${[...new Set(T.map(t => t.dosyaId))].length} dosya</td><td colspan="4"></td><td class="para genel">${dovizH(T, 'tutar')}</td><td colspan="7"></td></tr>`;
    tablo('tamamlanan', bas, rows, dip, 'Tamamlanan tahsilat yok. Banka Tahsilatları (Açık) sekmesinde "Eşleştir" ile bağlanan tahsilatlar burada listelenir.');
    $('#ibNot-tamamlanan').textContent = `${T.length} tahsilat · ihracat dosyası ve İBKB belgesiyle birlikte · tarihe göre`;
  }
  // ---------- İBKB Belgeleri (bankaya verilecek form)
  function cizBelgeler() {
    const B = belgeListe();
    const bas = ['Belge No', 'Tarih', 'Durum', 'Şirket', 'Banka', 'Dosya No', 'GÇB No', 'Fatura No', 'Müşteri', 'Ülke', 'Beyanname Tutarı', 'Tahsilat Tutarı', 'Tahsilat Tarihi', 'Referans', 'Banka İBKB No', 'Banka Tarihi', 'Not', 'Hazırlayan', 'İşlem'];
    const rows = B.map(b => `<tr class="${b.durum === 'Banka Onayladı' ? 'kapandi' : b.durum === 'İptal' ? 'soluk' : b.durum === 'Hazırlandı' ? 'yakin' : ''}"><td class="ad"><a href="/IhracatFinans/Belge/${b.id}" target="_blank" rel="noopener" title="Formu aç / yazdır">${esc(b.no)}</a></td><td>${tarih(b.tarih)}</td><td>${belgeRz(b.durum)}</td><td class="ad">${esc(b.sirket)}</td><td>${esc(b.banka || '—')}</td><td class="ad">${b.dosyaId ? `<a href="#" class="ib-duzenle" data-id="${b.dosyaId}">${esc(b.dosyaNo)}</a>` : esc(b.dosyaNo || '—')}</td><td>${esc(b.beyannameNo || '—')}</td><td>${esc(b.faturaNo || '—')}</td><td class="metin">${esc(b.musteri || '—')}<small>${esc(b.musteriKodu || '')}</small></td><td>${esc(b.ulke || '—')}</td><td class="para">${tutar(b.beyannameTutar, b.beyannameDoviz)}</td><td class="para genel">${tutar(b.tutar, b.doviz)}</td><td>${tarih(b.tahsilatTarihi)}</td><td>${esc(b.tahsilatRef || '—')}</td><td>${esc(b.bankaIbkbNo || '—')}</td><td>${tarih(b.bankaTarihi)}</td>${notH(b.not)}<td>${esc(b.olusturan || '—')}<small>${esc(b.olusturma || '')}</small></td><td><a class="btn btn-light border" href="/IhracatFinans/Belge/${b.id}" target="_blank" rel="noopener" title="Yazdır / PDF"><i class="fas fa-print"></i></a> <button type="button" class="btn btn-light border ib-belge-durum" data-id="${b.id}" title="Durum · banka İBKB no"><i class="fas fa-pen"></i></button>${b.durum === 'İptal' ? ` <button type="button" class="btn btn-light border text-danger ib-belge-sil" data-id="${b.id}" title="İptal belgeyi sil"><i class="fas fa-trash"></i></button>` : ''}</td></tr>`);
    const dip = `<tr><td colspan="11">Toplam · ${B.length} belge · ${B.filter(b => b.durum === 'Hazırlandı').length} hazırlandı · ${B.filter(b => b.durum === 'Bankaya Gönderildi').length} bankada · ${B.filter(b => b.durum === 'Banka Onayladı').length} onaylandı · ${B.filter(b => b.durum === 'İptal').length} iptal</td><td class="para genel">${dovizH(B.filter(b => b.durum !== 'İptal'), 'tutar')}</td><td colspan="7"></td></tr>`;
    tablo('belgeler', bas, rows, dip, 'İBKB belgesi yok. Banka Tahsilatları sekmesinde tahsilatı dosyayla eşleştirirken "İBKB oluşturulsun mu?" sorusuna evet deyince belge burada oluşur.');
    $('#ibNot-belgeler').textContent = `${B.length} belge · belge numarasına tıklayınca bankaya verilecek form açılır (Yazdır → PDF)`;
  }

  // ---------- Cari Bakiyeler (Mizan): muhasebe cari bakiyeleri (120 yurtdışı müşteri hesapları; portalda Cari Mutabakat Yönetimi'nin kaynağı) ↔ İBKB dosyalarının açık kalanı; düzenli kontrol işareti (11 Kas 2026)
  function cizBakiye() {
    const C = bakiyeListe(), gecikti = C.filter(c => c.kontrolDurumu === 'Kontrol Gecikti' || c.kontrolDurumu === 'Kontrol Yok'), farkli = C.filter(c => c.farkVar);
    const GA = (V.gorevler || []).filter(g => g.acik), onayB = GA.filter(g => g.durum === 'Düzeltildi'), gonderilen = C.filter(c => c.sonGonderim), itirazli = C.filter(c => c.sonGonderim && c.sonGonderim.durum === 'İtirazlı');
    const bas = ['Kontrol', 'Cari Kodu', 'Müşteri', 'Ülke', 'Şirket', 'Döviz Bakiyeleri (Mizan)', 'TL Karşılığı', 'Açık Fatura', 'En Eski Açık Fatura · Yaş', 'Son Hareket', 'Son Mutabakat', 'Mutabakat Durumu', 'İBKB Dosyaları', 'Dosya Açık Kalanı (Gerçek)', 'Fark (Bakiye − Dosya)', 'Son Kontrol', 'Kontrol Notu', 'Düzeltme Görevi', 'Müşteri Mutabakatı (Buradan)', 'İşlem'];
    const gorevHuc = c => c.gorev ? `<a href="#" class="ib-gorev-ac" data-id="${c.gorev.id}" title="Görevi aç: düzeltildi işaretle / onayla / iade et">${gorevRz(c.gorev.durum)}</a><small>${esc(c.gorev.atanan)} · termin ${tarih(c.gorev.termin)}${c.gorev.gecikti ? ' · <b style="color:#b91c1c">gecikti</b>' : c.gorev.terminGun !== null ? ` · ${c.gorev.terminGun} gün` : ''}</small>` : c.gorevSayisi ? `<small>${c.onaylananGorev} / ${c.gorevSayisi} görev onaylı</small>` : bos;
    const mutabakatHuc = c => c.sonGonderim ? `${tarih(c.sonGonderim.tarih)} · ${c.sonGonderim.kanal === 'WhatsApp' ? '<i class="fab fa-whatsapp" style="color:#16a34a"></i>' : '<i class="fas fa-envelope" style="color:#2563eb"></i>'} ${esc(c.sonGonderim.icerik)}<small>${gonderimRz(c.sonGonderim.durum)} ${c.sonGonderim.gun} gün önce${c.gonderimSayisi > 1 ? ' · ' + c.gonderimSayisi + ' gönderim' : ''}</small>` : '<span class="ib2-rz gri" title="Bu ekrandan müşteriye mutabakat gönderilmedi">Gönderilmedi</span>';
    const dvH = c => c.dovizler.length ? [...c.dovizler].sort((a, b) => (a.pb === 'TRY') - (b.pb === 'TRY')).map((d, i) => `${i === 0 ? '' : '<small>'}${tutar(d.net, d.pb)}${d.acikFaturaAdet ? ' · ' + d.acikFaturaAdet + ' açık fatura' : ''}${i === 0 ? '' : '</small>'}`).join('') : bos;
    const kalanH = c => c.dosyaKalan.length ? c.dosyaKalan.map((k, i) => `${i === 0 ? '' : '<small>'}${tutar(k.kalan, k.pb)}${i === 0 ? '' : '</small>'}`).join('') : (c.dosyaSayisi ? '<small>açık dosya yok</small>' : bos);
    const farkHuc = c => { const f = c.dovizler.filter(d => d.pb !== 'TRY' && Math.abs(d.fark) > 0.005); return f.length ? f.map((d, i) => `${i === 0 ? '' : '<small>'}<span style="font-weight:800;color:${d.fark > 0 ? '#b91c1c' : '#b45309'}">${d.fark > 0 ? '+' : ''}${tutar(d.fark, d.pb)}</span>${i === 0 ? '' : '</small>'}`).join('') + '<small>' + (f.some(d => d.fark > 0) ? 'cari bakiye dosyalardan fazla' : 'dosya kalanı bakiyeden fazla') + '</small>' : (c.dovizler.length ? '<span class="ib2-rz ok">Uyumlu</span>' : bos); };
    const rows = C.map(c => `<tr class="${c.kontrolDurumu === 'Fark Var' ? 'gecti' : c.kontrolDurumu === 'Kontrol Gecikti' || c.kontrolDurumu === 'Kontrol Yok' ? 'yakin' : c.kontrolDurumu === 'Uyumlu' ? 'kapandi' : ''}"><td>${kontrolRz(c.kontrolDurumu)}${c.kontrol && c.kontrol.gun !== null ? `<small>${c.kontrol.gun} gün önce</small>` : ''}</td><td class="ad sayac">${esc(c.kod)}</td><td class="metin">${esc(c.ad)}<small>${esc(c.resmiUnvan || '')}</small></td><td>${esc(c.ulke || '—')}</td><td>${esc(c.sirket || '—')}</td><td class="para">${dvH(c)}</td><td class="para genel">${tutar(c.bakiyeTl, 'TRY')}</td><td class="sayac">${c.acikFaturaAdet || bos}</td><td>${c.enEskiAcikFatura ? tarih(c.enEskiAcikFatura) + `<small>${c.bakiyeYasi} gün${c.eskiBakiyeUyari ? ' · eski bakiye' : ''}</small>` : bos}</td><td>${tarih(c.sonHareket)}</td><td>${tarih(c.sonMutabakat)}</td><td>${rz(c.mutabakatDurumu || '—', 'gri')}</td><td>${c.dosyaSayisi ? `<a href="#" class="ib-bakiye-dosya" data-kod="${esc(c.kod)}" title="Beyanname / İBKB Dosyaları bu müşteriye daralır">${c.dosyaSayisi} dosya</a><small>${c.acikDosya} açık</small>` : '<span class="ib2-rz gri" title="Bu cari koduyla İBKB dosyası yok">Dosya Yok</span>'}</td><td class="para">${kalanH(c)}</td><td>${farkHuc(c)}</td><td>${c.kontrol ? `${esc(c.kontrol.tarih)}<small>${esc(c.kontrol.kullanici || '')} · ${esc(c.kontrol.durum)}</small>` : bos}</td>${notH(c.kontrol ? c.kontrol.not : '')}<td>${gorevHuc(c)}</td><td>${mutabakatHuc(c)}</td><td><button type="button" class="btn btn-primary ib-bakiye-kontrol" data-kod="${esc(c.kod)}" title="Bakiyeyi kontrol et, işaretle; fark varsa düzeltme görevi ata"><i class="fas fa-clipboard-check"></i> Kontrol Et</button> <button type="button" class="btn btn-success ib-cari-mutabakat" data-kod="${esc(c.kod)}" title="Müşteriye mutabakat gönder: bakiye ya da ekstre, e-posta / WhatsApp"><i class="fas fa-paper-plane"></i></button> <a class="btn btn-light border" href="/CariMutabakat?kod=${encodeURIComponent(c.kod)}" target="_blank" rel="noopener" title="Cari Mutabakat Yönetimi · ekstre ve resmi mutabakat süreci"><i class="fas fa-scale-balanced"></i></a></td></tr>`);
    const dip = `<tr><td colspan="6">Toplam · ${C.length} cari · ${gecikti.length} kontrol bekliyor · ${farkli.length} farklı · ${C.filter(c => c.kontrolDurumu === 'Uyumlu').length} uyumlu</td><td class="para genel">${tutar(C.reduce((s, c) => s + n0(c.bakiyeTl), 0), 'TRY')}</td><td class="sayac">${C.reduce((s, c) => s + n0(c.acikFaturaAdet), 0)}</td><td colspan="4"></td><td class="sayac">${C.reduce((s, c) => s + n0(c.dosyaSayisi), 0)}</td><td colspan="4"></td><td><small>${C.filter(c => c.gorev).length} caride açık görev</small></td><td><small>${gonderilen.length} gönderildi · ${itirazli.length} itirazlı</small></td><td></td></tr>`;
    tablo('bakiye', bas, rows, dip, 'Yurtdışı müşteri carisi bulunamadı.');
    $('#ibNot-bakiye').textContent = `${C.length} cari · ${gecikti.length} kontrol bekliyor · ${farkli.length} farklı · ${GA.length} açık görev (${onayB.length} onay bekliyor) · ${gonderilen.length} mutabakat gönderildi · bakiye zamanı ${V.bakiyeZamani || ''}`;
    cizGorev();
  }
  // ---------- düzeltme görevleri (12 Kas 2026): Kontrol Et'te atama → atanan "Düzeltildi" → muhasebeci "Onayla" / "İade Et"; bekleyen görevler + kişi performansı
  let gorevMod = 'acik';
  const gorevListe = mod => (V.gorevler || []).filter(g => (mod === 'tum' || (mod === 'onay' ? g.durum === 'Düzeltildi' : mod === 'benim' ? g.acik && ((BEN && g.atananKod && g.atananKod.toLowerCase() === BEN.toLowerCase()) || (BEN_AD && g.atanan === BEN_AD)) : g.acik)) && (!F.sirket || g.sirket === F.sirket) && ara([g.cariKodu, g.cariAd, g.atanan, g.atayan, g.konu, g.aciklama, g.durum]));
  const gun1 = v => v === null || v === undefined ? bos : `${n0(v).toLocaleString('tr-TR', { maximumFractionDigits: 1 })} gün`;
  function gorevTablo(id, L, kisa) {
    const bas = ['Durum', 'Öncelik', 'Cari', 'Konu · Yapılacak', 'Bakiye · Fark', 'Atanan', 'Atayan · Tarih', 'Termin', 'Düzeltme', 'Onay', 'İade', 'İşlem'];
    const rows = L.map(g => `<tr class="${g.acik && g.gecikti ? 'gorev-gecikti' : g.durum === 'Düzeltildi' ? 'gorev-onay' : g.durum === 'Onaylandı' ? 'kapandi' : g.durum === 'İptal' ? 'soluk' : ''}"><td><a href="#" class="ib-gorev-ac" data-id="${g.id}" title="Görevi aç">${gorevRz(g.durum)}</a><small>#${g.id}${g.acik ? ` · ${gun1(g.bekleyenGun)} bekliyor` : ''}</small></td><td>${oncelikRz(g.oncelik)}</td><td class="ad"><a href="#" class="ib-gorev-cari" data-kod="${esc(g.cariKodu)}" title="Cari satırına git">${esc(g.cariAd)}</a><small>${esc(g.cariKodu)}${g.sirket ? ' · ' + esc(g.sirket) : ''}</small></td><td class="metin"><small class="ib2-not" title="Tamamını görmek için tıklayın"><b>${esc(g.konu)}</b>${g.aciklama ? ' — ' + esc(g.aciklama) : ''}</small></td><td class="para"><small>${esc(g.bakiyeOzet || '—')}</small><small>fark ${esc(g.farkOzet || '—')}</small></td><td>${esc(g.atanan)}${g.atananEposta ? `<small>${esc(g.atananEposta)}</small>` : ''}</td><td>${esc(g.atayan || '—')}<small>${esc(g.tarih)}</small></td><td>${g.termin ? tarih(g.termin) + `<small>${g.gecikti ? '<b style="color:#b91c1c">gecikti</b>' : g.acik && g.terminGun !== null ? g.terminGun + ' gün' : ''}</small>` : bos}</td><td>${g.duzeltme ? `${esc(g.duzeltme)}<small>${esc(g.duzelten)} · ${gun1(g.sureGun)}</small>` : bos}</td><td>${g.onay ? `${esc(g.onay)}<small>${esc(g.onaylayan)}</small>` : g.durum === 'Düzeltildi' ? '<span class="ib2-rz mor">Onay Bekliyor</span>' : bos}</td><td class="sayac">${g.iadeSayisi || bos}</td><td><button type="button" class="btn btn-primary ib-gorev-ac" data-id="${g.id}" title="${g.durum === 'Düzeltildi' ? 'Onayla / iade et' : g.acik ? 'Düzeltildi işaretle / yeniden ata' : 'Görev geçmişi'}"><i class="fas ${g.durum === 'Düzeltildi' ? 'fa-stamp' : g.acik ? 'fa-check' : 'fa-clock-rotate-left'}"></i></button></td></tr>`);
    const dip = `<tr><td colspan="4">Toplam · ${L.length} görev · ${L.filter(g => g.durum === 'Atandı' || g.durum === 'İade Edildi').length} düzeltme bekliyor · ${L.filter(g => g.durum === 'Düzeltildi').length} onay bekliyor · ${L.filter(g => g.durum === 'Onaylandı').length} onaylandı</td><td colspan="3"></td><td><small>${L.filter(g => g.gecikti).length} termin aşımı</small></td><td colspan="4"></td></tr>`;
    tablo(id, bas, rows, dip, kisa ? 'Açık düzeltme görevi yok.' : 'Görev yok. "Kontrol Et" penceresinde fark görülünce düzeltme görevi atanır.');
  }
  function perfTablo(id) {
    const P = (V.gorevPerformans || []).filter(p => !F.ara || ara([p.atanan]));
    tablo(id, ['Kişi', 'Görev', 'Açık', 'Onay Bekleyen', 'Onaylanan', 'İade', 'Termin Aşımı', 'Ort. Düzeltme', 'Ort. Onay', 'Zamanında'],
      P.map(p => `<tr class="${p.gecikmis ? 'yakin' : ''}"><td class="ad">${esc(p.atanan)}</td><td class="sayac">${p.toplam}</td><td class="sayac">${p.acik || bos}</td><td class="sayac">${p.onayBekleyen || bos}</td><td class="sayac">${p.onaylanan || bos}</td><td class="sayac">${p.iade || bos}</td><td class="sayac">${p.gecikmis ? `<b style="color:#b91c1c">${p.gecikmis}</b>` : bos}</td><td>${gun1(p.ortSure)}</td><td>${gun1(p.ortOnay)}</td><td class="sayac">${p.zamanindaYuzde === null ? bos : `<span class="ib2-rz ${p.zamanindaYuzde >= 80 ? 'ok' : p.zamanindaYuzde >= 50 ? 'bekle' : 'hata'}">${p.zamanindaYuzde} %</span>`}</td></tr>`),
      `<tr><td>Toplam · ${P.length} kişi</td><td class="sayac">${P.reduce((a, p) => a + p.toplam, 0)}</td><td class="sayac">${P.reduce((a, p) => a + p.acik, 0)}</td><td class="sayac">${P.reduce((a, p) => a + p.onayBekleyen, 0)}</td><td class="sayac">${P.reduce((a, p) => a + p.onaylanan, 0)}</td><td class="sayac">${P.reduce((a, p) => a + p.iade, 0)}</td><td class="sayac">${P.reduce((a, p) => a + p.gecikmis, 0)}</td><td colspan="3"></td></tr>`, 'Henüz görev atanmadı.');
  }
  function cizGorev() {
    if (!$('#ibTablo-gorev')) return;
    const L = gorevListe(gorevMod); gorevTablo('gorev', L); perfTablo('gorevPerf');
    $$('#ibGorevSuzgec button').forEach(b => b.classList.toggle('aktif', b.dataset.g === gorevMod));
    $('#ibNot-gorev').textContent = `${L.length} görev · akış: Atandı → Düzeltildi (atanan) → Onaylandı / İade (muhasebe) · bağlantı e-posta ve portal mesajıyla gider`;
    $('#ibNot-gorevPerf').textContent = `${(V.gorevPerformans || []).length} kişi · ortalama düzeltme süresi atamadan "Düzeltildi"ye`;
  }
  // görev atama formu (Kontrol Et, görev penceresi "yeniden ata", mutabakat itirazı)
  const gorevAtaH = (g, baslik, acikMi) => { g = g || {}; const K = V.gorevKisiler || []; const sec = g.atanan || ''; const termin = g.termin || new Date(new Date(V.bugun).getTime() + 7 * 86400000).toISOString().slice(0, 10);
    return `<div class="ib2-bolum mt-2" id="ibGorevBolum"><div class="b"><span><i class="fas fa-user-check me-1"></i>${esc(baslik || 'Düzeltme Görevi Ata')}</span><label style="text-transform:none;font-size:.66rem;font-weight:700;color:#0f172a;cursor:pointer"><input type="checkbox" id="ibf_gorevAc" class="me-1" ${acikMi ? 'checked' : ''}>Düzeltmeyi yapacak kişiye görev ata (e-posta + portal mesajı)</label></div><div class="g" id="ibGorevForm" style="display:${acikMi ? '' : 'none'}"><div class="ib2-form">
      <div class="g2"><label for="ibf_atanan">Atanan Kişi</label><select class="form-select" id="ibf_atanan"><option value="">— seçin —</option>${K.map(k => `<option value="${esc(k.ad)}" data-kod="${esc(k.kod)}" data-eposta="${esc(k.eposta)}" ${k.ad === sec ? 'selected' : ''}>${esc(k.ad)}${k.bolum ? ' · ' + esc(k.bolum) : ''}${k.kod ? ' (' + esc(k.kod) + ')' : ''}</option>`).join('')}<option value="__diger" ${sec && !K.some(k => k.ad === sec) ? 'selected' : ''}>Diğer (adı yazın)</option></select></div>
      ${alan('atananAd', 'Diğer Kişi Adı', 'text', sec && !K.some(k => k.ad === sec) ? sec : '')}${alan('atananEposta', 'E-posta (Bildirim)', 'text', g.atananEposta || '')}
      ${alan('termin', 'Termin', 'date', termin)}${alan('oncelik', 'Öncelik', 'select', { liste: V.gorevOncelikleri || ['Normal', 'Yüksek', 'Acil'], v: g.oncelik || 'Normal' })}${alan('gorevAciklama', 'Yapılacak (Boş = Kontrol Notu)', 'text', g.aciklama || '', 'g2')}
    </div><small class="text-muted" style="font-size:.6rem">Kaydet'te görev kaydı yazılır, atanan portal kullanıcısıysa mesaj kutusuna düşer ve e-posta cihazdaki posta uygulamasında hazır açılır (SMTP bağlanınca doğrudan gider).</small></div></div>`; };
  function gorevFormBagla() {
    const ac = $('#ibf_gorevAc'), f = $('#ibGorevForm'), sel = $('#ibf_atanan'); if (!ac || !sel) return;
    ac.addEventListener('change', () => { f.style.display = ac.checked ? '' : 'none'; });
    const diger = () => { const d = sel.value === '__diger'; $('#ibf_atananAd').closest('div').style.display = d ? '' : 'none'; };
    sel.addEventListener('change', () => { const o = sel.selectedOptions[0]; if (o && o.dataset.eposta) $('#ibf_atananEposta').value = o.dataset.eposta; diger(); }); diger();
  }
  const gorevOku = () => { const g = id => ($('#ibf_' + id) || {}).value ?? ''; const sel = $('#ibf_atanan'); const o = sel && sel.selectedOptions[0]; const diger = g('atanan') === '__diger'; return { atanan: diger ? g('atananAd').trim() : g('atanan'), atananKod: diger || !o ? '' : o.dataset.kod || '', atananEposta: g('atananEposta').trim(), termin: g('termin') || null, oncelik: g('oncelik') || 'Normal', aciklama: g('gorevAciklama') }; };
  const gorevIstiyor = () => { const ac = $('#ibf_gorevAc'); return ac && ac.checked; };
  const epostaAc = (alici, konu, govde) => { if (!alici) return; window.location.href = 'mailto:' + encodeURIComponent(alici) + '?subject=' + encodeURIComponent(konu || '') + '&body=' + encodeURIComponent((govde || '').replace(/\{portal\}/g, location.origin)); };
  function gorevAc(g) {
    modalTur = 'gorev'; belgeDuzenlenen = g; const c = (V.cariBakiyeler || []).find(x => x.kod === g.cariKodu);
    $('#ibMBaslik').textContent = `Düzeltme Görevi #${g.id} · ${g.cariKodu} · ${g.cariAd} · ${g.durum}`; $('#ibMNot').innerHTML = `${esc(g.atayan)} atadı · ${esc(g.tarih)}${g.bildirim ? ' · ' + esc(g.bildirim) : ''} · <a href="#" class="ib-gorev-eposta" title="Atanan kişiye gidecek e-postayı posta uygulamasında aç"><i class="fas fa-envelope me-1"></i>e-postayı aç</a>`;
    const islemler = g.durum === 'Atandı' || g.durum === 'İade Edildi' ? [['duzeltildi', 'Düzeltildi — düzeltmeyi yaptım, onaya gönder', 'fa-check'], ['guncelle', 'Yeniden Ata / Termin Değiştir', 'fa-user-pen'], ['iptal', 'İptal Et', 'fa-xmark']]
      : g.durum === 'Düzeltildi' ? [['onayla', 'Onayla (muhasebe) — görev kapanır', 'fa-stamp'], ['iade', 'İade Et — düzeltme yetersiz, yeniden yapılsın', 'fa-rotate-left'], ['guncelle', 'Yeniden Ata', 'fa-user-pen'], ['iptal', 'İptal Et', 'fa-xmark']] : [];
    $('#ibMGovde').innerHTML = `<div class="ib2-onizle mb-2"><div><b>Cari:</b> ${esc(g.cariKodu)} ${esc(g.cariAd)}${g.sirket ? ' · ' + esc(g.sirket) : ''}</div><div><b>Mizan bakiyesi:</b> ${esc(g.bakiyeOzet || '—')}</div><div><b>Fark:</b> ${esc(g.farkOzet || '—')}</div><div><b>Atanan:</b> ${esc(g.atanan)}${g.atananEposta ? ' · ' + esc(g.atananEposta) : ''}</div><div><b>Termin:</b> ${tarih(g.termin)} ${g.gecikti ? '<span class="ib2-rz hata">Gecikti</span>' : g.acik && g.terminGun !== null ? `(${g.terminGun} gün)` : ''} · ${oncelikRz(g.oncelik)}</div><div><b>Durum:</b> ${gorevRz(g.durum)}${g.iadeSayisi ? ` · ${g.iadeSayisi} iade` : ''}</div><div style="grid-column:span 3"><b>Yapılacak:</b> ${esc(g.aciklama || '—')}</div>${g.duzeltme ? `<div style="grid-column:span 3"><b>Düzeltme:</b> ${esc(g.duzeltme)} · ${esc(g.duzelten)} · ${gun1(g.sureGun)} — ${esc(g.duzeltmeNotu || '')}</div>` : ''}${g.onay ? `<div style="grid-column:span 3"><b>Onay:</b> ${esc(g.onay)} · ${esc(g.onaylayan)} — ${esc(g.onayNotu || '')}</div>` : ''}${c && c.gonderimSayisi ? `<div style="grid-column:span 3"><b>Müşteri mutabakatı:</b> ${esc(c.sonGonderim.tarih)} ${esc(c.sonGonderim.kanal)} · ${esc(c.sonGonderim.durum)}${c.sonGonderim.not ? ' — ' + esc(c.sonGonderim.not) : ''}</div>` : ''}</div>
      ${islemler.length ? `<div class="ib2-islem">${islemler.map(([k, e, i], n) => `<label><input type="radio" name="ibIslem" value="${k}" ${n === 0 ? 'checked' : ''}><i class="fas ${i}"></i>${esc(e)}</label>`).join('')}</div><div class="ib2-form">${alan('not', 'Not (Düzeltme Açıklaması / Onay Notu / İade Nedeni)', 'text', '', 'g4')}</div>` : '<div class="text-muted" style="font-size:.7rem">Görev kapanmış; yalnız geçmiş görüntülenir.</div>'}
      ${islemler.some(x => x[0] === 'guncelle') ? gorevAtaH({ atanan: g.atanan, atananEposta: g.atananEposta, termin: g.termin, oncelik: g.oncelik, aciklama: g.aciklama }, 'Yeniden Atama / Termin', false).replace('id="ibGorevBolum"', 'id="ibGorevBolum" style="display:none"') : ''}
      <div class="mt-2" style="font-size:.6rem;font-weight:800;color:#64748b;text-transform:uppercase;letter-spacing:.04em">Geçmiş</div><div class="ib2-gecmis">${(g.gecmis || []).map(a => `<div><b>${esc(a.tarih)}</b> · ${esc(a.kullanici)} · ${esc(a.islem)}${a.not ? ' — ' + esc(a.not) : ''}</div>`).join('') || '<div>—</div>'}</div>`;
    gorevFormBagla();
    $$('input[name=ibIslem]').forEach(r => r.addEventListener('change', () => { const b = $('#ibGorevBolum'); if (b) { b.style.display = r.value === 'guncelle' && r.checked ? '' : 'none'; const ac = $('#ibf_gorevAc'); if (ac) { ac.checked = r.value === 'guncelle'; $('#ibGorevForm').style.display = ac.checked ? '' : 'none'; } } }));
    $('.ib-gorev-eposta', $('#ibMNot')).addEventListener('click', e => { e.preventDefault(); if (!g.atananEposta) { mesaj('warning', 'Atanan kişinin e-postası yok; "Yeniden Ata" ile girin.'); return; } epostaAc(g.atananEposta, g.epostaKonu, g.epostaGovde); });
    $('#ibMKaydet').style.display = islemler.length ? '' : 'none';
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  function bakiyeKontrolAc(c) {
    modalTur = 'bakiye'; belgeDuzenlenen = c;
    const ozet = c.dovizler.map(d => tutar(d.net, d.pb)).join(' · ') || '—', fark = c.dovizler.filter(d => d.pb !== 'TRY' && Math.abs(d.fark) > 0.005).map(d => (d.fark > 0 ? '+' : '') + tutar(d.fark, d.pb)).join(' · ') || 'yok';
    $('#ibMBaslik').textContent = `Bakiye Kontrolü · ${c.kod} · ${c.ad}`; $('#ibMNot').textContent = c.kontrol ? `Son kontrol ${c.kontrol.tarih} · ${c.kontrol.kullanici} · ${c.kontrol.durum} (${c.kontrolSayisi} kayıt)` : 'İlk kontrol';
    $('#ibMGovde').innerHTML = `<div class="ib2-onizle mb-2"><div><b>Mizan bakiyesi:</b> ${esc(ozet)} (${tutar(c.bakiyeTl, 'TRY')})</div><div><b>İBKB dosya kalanı:</b> ${c.dosyaKalan.map(k => tutar(k.kalan, k.pb)).join(' · ') || '—'} (${c.dosyaSayisi} dosya, ${c.acikDosya} açık)</div><div><b>Fark:</b> ${esc(fark)}</div><div><b>Açık fatura:</b> ${c.acikFaturaAdet || 0} · en eski ${tarih(c.enEskiAcikFatura)} (${c.bakiyeYasi ?? '—'} gün)</div><div><b>Son hareket:</b> ${tarih(c.sonHareket)}</div><div><b>Son mutabakat:</b> ${tarih(c.sonMutabakat)}</div></div>
      <div class="ib2-form">${alan('durum', 'Kontrol Sonucu', 'select', { liste: V.bakiyeKontrolDurumlari, v: c.farkVar ? 'Fark Var' : 'Uyumlu' })}${alan('not', 'Not (Fark Nedeni / Yapılacak)', 'text', c.kontrol ? c.kontrol.not : '', 'g3')}</div>
      ${c.gorev ? `<div class="alert alert-warning py-1 px-2 mt-2 mb-0" style="font-size:.68rem"><i class="fas fa-list-check me-1"></i>Bu caride açık görev var: #${c.gorev.id} ${gorevRz(c.gorev.durum)} ${esc(c.gorev.atanan)} · termin ${tarih(c.gorev.termin)} — <a href="#" class="ib-gorev-ac" data-id="${c.gorev.id}">görevi aç</a>. Yeni atama yapılırsa ikinci görev açılır.</div>` : ''}
      ${gorevAtaH(null, 'Düzeltme Görevi Ata', c.farkVar && !c.gorev)}`;
    gorevFormBagla();
    bakiyeKontrolAc.ozet = ozet; bakiyeKontrolAc.fark = fark;
    $('#ibMKaydet').style.display = '';
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  // ---------- müşteriye mutabakat gönderimi (12 Kas 2026): bakiye ya da ekstre, e-posta / WhatsApp; kayıt burada + Cari Mutabakat Yönetimi günlüğünde; yanıt Mutabık / İtirazlı (itirazda görev ata)
  const cariAdAyni = (c, ad) => !!ad && ((c.ad || '').trim().toLocaleLowerCase('tr-TR') === ad.trim().toLocaleLowerCase('tr-TR') || (c.resmiUnvan || '').trim().toLocaleLowerCase('tr-TR') === ad.trim().toLocaleLowerCase('tr-TR'));
  const cariAcikDosyalar = c => V.dosyalar.filter(d => d.dabDurumu !== 'İptal' && (d.musteriKodu === c.kod || cariAdAyni(c, d.musteri)) && n0(d.kalanGercek) > 0.005);
  const ekstreLink = c => `${location.origin}/IhracatFinans/CariEkstre?kod=${encodeURIComponent(c.kod)}&t=${c.ekstreJeton || ''}`;
  function mutabakatMetni(c, icerik, kanal) {
    const y = c.yetkili || {}, sirket = c.sirket || 'Uras Grubu', unvan = c.resmiUnvan || c.ad, bugun = tarih(V.bugun);
    const bak = c.dovizler.filter(d => Math.abs(d.net) > 0.004).map(d => `${tutar(Math.abs(d.net), d.pb)} ${d.net > 0 ? '(firmanız borçlu)' : '(şirketimiz borçlu)'}`).join(' · ') || 'bakiye yok (0,00)';
    const acik = c.acikFaturaAdet ? `Açık fatura: ${c.acikFaturaAdet} adet` + (c.enEskiAcikFatura ? ` (en eski ${tarih(c.enEskiAcikFatura)})` : '') : '';
    const D = cariAcikDosyalar(c);
    if (kanal === 'WhatsApp') return `Merhaba ${y.ad || 'Yetkili'}, ${sirket} olarak ${c.kod} ${unvan} cari hesabınızın ${bugun} itibarıyla bakiyesi: ${bak}. ${acik ? acik + '. ' : ''}${icerik === 'Ekstre' ? `Ayrıntılı ekstre: ${ekstreLink(c)} · ` : ''}Mutabık iseniz "Mutabıkız", fark varsa açıklamasıyla "İtiraz" yanıtı vermenizi rica ederiz. ${BEN_AD || 'Muhasebe'} · ${sirket}`;
    const L = [`Sayın ${y.ad || 'Yetkili'},`, '', `${sirket} nezdindeki ${c.kod} ${unvan} cari hesabınızın ${bugun} tarihi itibarıyla bakiyesi:`, bak];
    if (acik) L.push(acik);
    if (D.length) { L.push('', 'Bedeli beklenen ihracat faturaları:'); D.forEach(d => L.push(`- ${d.faturaNo || d.dosyaNo}${d.baslangic ? ' · ' + tarih(d.baslangic) : ''} · ${tutar(d.gercekBedelEtkin, d.doviz)} · kalan ${tutar(d.kalanGercek, d.doviz)}`)); }
    L.push('', 'Kayıtlarınızla uyuşuyorsa "Mutabıkız", fark varsa fark açıklamasıyla "İtiraz" yanıtı vermenizi rica ederiz.');
    if (icerik === 'Ekstre') L.push('', `Ayrıntılı cari hesap ekstresi (hareketler, açık faturalar): ${ekstreLink(c)}`);
    L.push('', 'Saygılarımızla,', `${BEN_AD || 'Muhasebe'} · ${sirket} Muhasebe`);
    return L.join('\n');
  }
  function mutabakatAc(c) {
    modalTur = 'cariMutabakat'; belgeDuzenlenen = c; const y = c.yetkili || {};
    const kanal0 = y.eposta || !y.cep ? 'E-posta' : 'WhatsApp';
    $('#ibMBaslik').textContent = `Mutabakat Gönder · ${c.kod} · ${c.ad}`; $('#ibMNot').innerHTML = (y.ad ? `Mutabakat yetkilisi: ${esc(y.ad)}${y.unvan ? ' · ' + esc(y.unvan) : ''}` : 'Cari kartında mutabakat yetkilisi yok; alıcıyı yazın') + (c.sonGonderim ? ` · son gönderim ${esc(c.sonGonderim.tarih)} ${esc(c.sonGonderim.kanal)} (${esc(c.sonGonderim.durum)})` : '');
    $('#ibMGovde').innerHTML = `<div class="ib2-onizle mb-2"><div><b>Mizan bakiyesi:</b> ${c.dovizler.map(d => tutar(d.net, d.pb)).join(' · ') || '—'}</div><div><b>Açık fatura:</b> ${c.acikFaturaAdet || 0} · ${cariAcikDosyalar(c).length} açık ihracat dosyası</div><div><b>Fark (bakiye − dosya):</b> ${c.dovizler.filter(d => d.pb !== 'TRY' && Math.abs(d.fark) > 0.005).map(d => (d.fark > 0 ? '+' : '') + tutar(d.fark, d.pb)).join(' · ') || 'yok'}</div><div><b>E-posta:</b> ${esc(y.eposta || '—')}</div><div><b>Cep (WhatsApp):</b> ${esc(y.cep || '—')}</div><div><b>Ekstre bağlantısı:</b> <a href="${esc(ekstreLink(c))}" target="_blank" rel="noopener">önizle</a></div></div>
      <div class="ib2-form">${alan('kanal', 'Kanal', 'select', { liste: V.subeKanallar || ['E-posta', 'WhatsApp'], v: kanal0 })}${alan('icerik', 'İçerik', 'select', { liste: V.cariIcerikler || ['Bakiye', 'Ekstre'], v: 'Bakiye' })}${alan('alici', 'Alıcı (E-posta ya da WhatsApp Numarası)', 'text', kanal0 === 'WhatsApp' ? y.cep : y.eposta)}${alan('aliciAd', 'Alıcı Adı', 'text', y.ad || '')}
      ${alan('konu', 'Konu', 'text', `${c.sirket || 'Uras Grubu'} · ${tarih(V.bugun)} tarihli cari hesap mutabakatı (${c.kod} ${c.ad})`, 'g4')}${alan('mesaj', 'Mesaj (Şablondan; Düzenlenebilir)', 'uzun', mutabakatMetni(c, 'Bakiye', kanal0)).replace('rows="2"', 'rows="12" style="height:auto"').replace('class="form-control"', 'class="form-control ib2-mesaj"')}</div>
      <small class="text-muted" style="font-size:.6rem">Kaydet'te gönderim kaydı yazılır (burada ve Cari Mutabakat Yönetimi günlüğünde), cihazdaki posta / WhatsApp uygulaması mesajla açılır (SMTP / WhatsApp Business API bağlanınca doğrudan gider). Ekstre bağlantısı portal girişi istemez; "İçerik: Ekstre" seçince mesaja eklenir.</small>
      ${c.gonderimler && c.gonderimler.length ? `<div class="mt-2" style="font-size:.6rem;font-weight:800;color:#64748b;text-transform:uppercase;letter-spacing:.04em">Önceki gönderimler</div><table class="ib2-aday"><thead><tr><th>Tarih</th><th>Kanal</th><th>İçerik</th><th>Alıcı</th><th>Bakiye</th><th>Durum</th><th>Gönderen</th><th>Yanıt / Not</th><th></th></tr></thead><tbody>${c.gonderimler.map(g => `<tr><td>${esc(g.tarih)}</td><td>${esc(g.kanal)}</td><td>${esc(g.icerik)}</td><td>${esc(g.aliciAd ? g.aliciAd + ' · ' : '')}${esc(g.alici)}</td><td>${esc(g.bakiyeOzet)}</td><td>${gonderimRz(g.durum)}</td><td>${esc(g.kullanici)}</td><td>${esc(g.not || '')}</td><td><button type="button" class="btn btn-light border ib-cari-yanit" data-id="${g.id}" data-kod="${esc(c.kod)}" title="Yanıt işaretle (Mutabık / İtirazlı)"><i class="fas fa-reply"></i></button></td></tr>`).join('')}</tbody></table>` : ''}`;
    const yenile = () => { const k = $('#ibf_kanal').value, i = $('#ibf_icerik').value; $('#ibf_mesaj').value = mutabakatMetni(c, i, k); };
    $('#ibf_kanal').addEventListener('change', () => { $('#ibf_alici').value = $('#ibf_kanal').value === 'WhatsApp' ? (y.cep || '') : (y.eposta || ''); yenile(); });
    $('#ibf_icerik').addEventListener('change', yenile);
    $('#ibMKaydet').style.display = '';
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  function cariGonderimDurumAc(g, c) {
    modalTur = 'cariGonderimDurum'; belgeDuzenlenen = g; cariGonderimDurumAc.cari = c;
    $('#ibMBaslik').textContent = `Mutabakat Yanıtı · ${c.kod} · ${c.ad} · ${g.tarih} · ${g.kanal}`; $('#ibMNot').textContent = `Alıcı ${g.aliciAd ? g.aliciAd + ' · ' : ''}${g.alici} · ${g.icerik} · bakiye ${g.bakiyeOzet}${g.mtBelgeNo ? ' · ' + g.mtBelgeNo : ''}`;
    $('#ibMGovde').innerHTML = `<div class="ib2-form">${alan('durum', 'Yanıt / Durum', 'select', { liste: V.subeGonderimDurumlari || ['Gönderildi', 'Mutabık', 'İtirazlı', 'Yanıt Bekleniyor'], v: g.durum })}${alan('not', 'Müşteri Yanıtı / Fark Açıklaması', 'text', g.not, 'g3')}</div>
      ${gorevAtaH({ aciklama: g.not ? 'Müşteri itirazı: ' + g.not : '' }, 'İtirazda Düzeltme Görevi Ata (Boş = Müşteri Yanıtı)', g.durum === 'İtirazlı' && !c.gorev)}`;
    gorevFormBagla();
    $('#ibf_durum').addEventListener('change', () => { const it = $('#ibf_durum').value === 'İtirazlı'; const ac = $('#ibf_gorevAc'); ac.checked = it && !c.gorev; $('#ibGorevForm').style.display = ac.checked ? '' : 'none'; });
    $('#ibMKaydet').style.display = '';
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  // ---------- Yurtdışı Şube Bakiyeleri (11 Kas 2026): kendi şubelerimiz (Fas, Tunus, Cezayir, ABD, Meksika, Mısır, Portekiz, Özbekistan, Dubai) tek sayfada; şubeye tıklayınca yıl bazında detay; ekstre e-posta / WhatsApp ile
  let subeSecili = 0;
  const subeListe = (dabHaric) => (V.subeler || []).filter(s => (!F.sirket || s.sirket === F.sirket) && (dabHaric || !F.dab || s.temsilci === F.dab) && (!F.doviz || s.pb === F.doviz) && (!F.yil || s.yillar.some(y => (y.yil || '').startsWith(F.yil))) && ara([s.kod, s.sube, s.ulke, s.sirket, s.urunGrubu, s.temsilci, s.sorumlu, s.not, s.eposta, s.telefon]));
  const isaretli = (v, pb) => `<span style="font-weight:800;color:${v < -0.005 ? '#b91c1c' : v > 0.005 ? '#15803d' : '#475569'}">${tutar(v, pb)}</span>`;
  const subeDv = L => L.map(s => Object.assign({ doviz: s.pb }, s));
  const gonderimRz = d => rz(d || '—', d === 'Mutabık' ? 'ok' : d === 'İtirazlı' ? 'hata' : d === 'Yanıt Bekleniyor' ? 'bekle' : 'mavi');
  const gunFarki = t => t ? Math.floor((new Date(V.bugun) - new Date(String(t).slice(0, 10))) / 86400000) : null;
  const sonEkstreH = s => s.sonGonderim ? `${tarih(s.sonGonderim.tarih)} · ${esc(s.sonGonderim.kanal)}<small>${gonderimRz(s.sonGonderim.durum)} ${gunFarki(s.sonGonderim.tarih)} gün önce · ${esc(s.sonGonderim.donem)}</small>` : '<span class="ib2-rz bekle" title="Bu şubeye henüz ekstre gönderilmedi">Gönderilmedi</span>';
  function cizSube() {
    const S = subeListe(), D = subeDv(S), bekleyen = S.filter(s => !s.sonGonderim || gunFarki(s.sonGonderim.tarih) > 90), itiraz = S.filter(s => s.sonGonderim && s.sonGonderim.durum === 'İtirazlı');
    $('#ibSubeOzet').innerHTML = `<span><b>${S.length} şube</b> · ${new Set(S.map(s => s.ulke)).size} ülke · bakiye ${dovizH(D, 'bakiye', true)} (eksi: şube bize borçlu) · satış ${dovizH(D, 'satisEsas', true)} · ödeme ${dovizH(D, 'toplamOdeme', true)} · <b>${bekleyen.length}</b> şubeye 90 gündür ekstre gitmedi · <b>${itiraz.length}</b> itirazlı</span><button type="button" class="btn btn-primary ib-sube-yeni"><i class="fas fa-plus me-1"></i>Şube Ekle</button>`;
    const bas = ['Şube', 'Ülke', 'Grup Şirketi', 'Ürün', 'Satış Temsilcisi', 'Sorumlu', 'PB', 'Dönem', 'Sermaye', 'Resmi Fatura (Satış)', 'Gerçek Mal Bedeli', 'Toplam Ödeme', 'Bakiye (Devir)', 'Son Yıl Farkı', 'Son Ekstre', 'İletişim', 'İşlem'];
    const rows = S.map(s => `<tr class="tik ib-sube-satir ${s.id === subeSecili ? 'secili' : ''}" data-id="${s.id}" title="Tıklayınca şube detayı açılır"><td class="ad">${esc(s.sube)}<small>${esc(s.kod)}</small></td><td>${esc(s.ulke)}</td><td>${s.sirket ? esc(s.sirket) : '<span class="ib2-rz bekle" title="Kaynak tabloda şirket yok; düzenleyip seçin">Belirsiz</span>'}</td><td>${esc(s.urunGrubu || '—')}</td><td>${s.temsilci ? esc(s.temsilci) : '<span class="ib2-rz bekle">Atanmadı</span>'}</td><td>${esc(s.sorumlu || '—')}</td><td>${rz(s.pb, 'teal')}</td><td class="sayac">${s.yilSayisi ? `${esc(s.ilkYil)} – ${esc(s.sonYil)}<small>${s.yilSayisi} yıl</small>` : '<span class="ib2-rz gri">Veri Yok</span>'}</td><td class="para">${s.sermaye ? tutar(s.sermaye, s.pb) : bos}</td><td class="para">${tutar(s.resmiFatura, s.pb)}</td><td class="para">${s.gercekMalBedeli ? tutar(s.gercekMalBedeli, s.pb) : bos}</td><td class="para">${tutar(s.toplamOdeme, s.pb)}</td><td class="para genel">${isaretli(s.bakiye, s.pb)}</td><td class="para">${s.yilSayisi ? isaretli(s.sonYilFark, s.pb) + `<small>${esc(s.sonYil)}</small>` : bos}</td><td>${sonEkstreH(s)}</td><td>${s.eposta ? `<small><i class="fas fa-envelope me-1"></i>${esc(s.eposta)}</small>` : ''}${s.telefon ? `<small><i class="fab fa-whatsapp me-1"></i>${esc(s.telefon)}</small>` : ''}${!s.eposta && !s.telefon ? '<span class="ib2-rz gri" title="Düzenle ile e-posta / telefon girin">İletişim Yok</span>' : ''}</td><td><button type="button" class="btn btn-primary ib-sube-detay" data-id="${s.id}" title="Yıl bazında detay"><i class="fas fa-magnifying-glass"></i></button> <button type="button" class="btn btn-success ib-sube-ekstre" data-id="${s.id}" title="Ekstre gönder (e-posta / WhatsApp)"><i class="fas fa-paper-plane"></i></button> <button type="button" class="btn btn-light border ib-sube-duzenle" data-id="${s.id}" title="Düzenle"><i class="fas fa-pen"></i></button> <button type="button" class="btn btn-light border text-danger ib-sube-sil" data-id="${s.id}" title="Sil"><i class="fas fa-trash"></i></button></td></tr>`);
    const dip = `<tr><td colspan="8">Toplam · ${S.length} şube · ${new Set(S.map(s => s.ulke)).size} ülke · ${S.filter(s => !s.temsilci).length} temsilci atanmadı</td><td class="para">${dovizH(D, 'sermaye')}</td><td class="para">${dovizH(D, 'resmiFatura')}</td><td class="para">${dovizH(D, 'gercekMalBedeli')}</td><td class="para">${dovizH(D, 'toplamOdeme')}</td><td class="para genel">${dovizH(D, 'bakiye')}</td><td class="para">${dovizH(D, 'sonYilFark')}</td><td colspan="3"><small>${S.filter(s => s.sonGonderim).length} şubeye ekstre gitti · ${bekleyen.length} bekliyor</small></td></tr>`;
    tablo('sube', bas, rows, dip, 'Şube yok. "Şube Ekle" ile açın.');
    $('#ibNot-sube').textContent = `${S.length} şube · bakiye = sermaye + ödemeler − satış (birikimli) · satıra tıklayınca detay`;
    subeDetay(subeSecili, true);
  }
  function subeDetay(id, sessiz) {
    const s = (V.subeler || []).find(x => x.id === id); const k = $('#ibSubeDetay');
    if (!s) { subeSecili = 0; k.style.display = 'none'; return; }
    subeSecili = id; k.style.display = '';
    $$('#ibTablo-sube tr.ib-sube-satir').forEach(r => r.classList.toggle('secili', +r.dataset.id === id));
    $('#ibSubeDetayBaslik').innerHTML = `<i class="fas fa-globe me-1"></i>${esc(s.sube)} · ${esc(s.ulke)}${s.sirket ? ' · ' + esc(s.sirket) : ''} · bakiye ${isaretli(s.bakiye, s.pb)}`;
    $('#ibSubeDetayNot').textContent = `${s.yilSayisi} yıl · ${s.gonderimSayisi} gönderim · kaynak ${s.kaynak || '—'}${s.guncelleme ? ' · güncelleme ' + s.guncelleme + ' ' + (s.guncelleyen || '') : ''}`;
    $('#ibSubeBilgi').innerHTML = `<span><b>Satış Temsilcisi:</b> ${esc(s.temsilci || 'Atanmadı')}</span><span><b>Sorumlu:</b> ${esc(s.sorumlu || '—')}</span><span><b>Ürün:</b> ${esc(s.urunGrubu || '—')}</span><span><b>E-posta:</b> ${esc(s.eposta || '—')}</span><span><b>Telefon:</b> ${esc(s.telefon || '—')}</span>${s.adres ? `<span><b>Adres:</b> ${esc(s.adres)}</span>` : ''}${s.not ? `<span style="flex-basis:100%"><b>Not:</b> ${esc(s.not)}</span>` : ''}
      <span style="margin-left:auto;display:flex;gap:4px"><button type="button" class="btn btn-success ib-sube-ekstre" data-id="${s.id}"><i class="fas fa-paper-plane me-1"></i>Ekstre Gönder</button><a class="btn btn-light border" href="/IhracatFinans/SubeEkstre/${s.id}" target="_blank" rel="noopener" title="Basılabilir ekstre (Yazdır → PDF)"><i class="fas fa-file-pdf me-1"></i>Ekstre (PDF)</a><button type="button" class="btn btn-light border ib-sube-duzenle" data-id="${s.id}"><i class="fas fa-pen me-1"></i>Düzenle</button><button type="button" class="btn btn-light border ib-sube-kapat" title="Detayı kapat"><i class="fas fa-xmark"></i></button></span>`;
    const Y = s.yillar, pb = s.pb, var_ = f => Y.some(y => n0(y[f]));
    const kol = [['yil', 'Yıl', 'sayac'], ['sermaye', 'Sermaye', 'para', true], ['resmiFatura', 'Resmi Fatura', 'para'], ['gercekMalBedeli', 'Gerçek Mal Bedeli', 'para', true], ['iadeFaturasi', 'İade Faturası', 'para', true], ['komisyon', 'Komisyon', 'para', true], ['resmiOdeme', 'Resmi Ödeme', 'para'], ['digerOdeme', 'Diğer Ödeme', 'para'], ['nakliye', 'Nakliye', 'para', true], ['diger', 'Diğer', 'para', true], ['duzeltme', 'Düzeltme', 'para', true], ['toplamOdeme', 'Toplam Ödeme', 'para'], ['fark', 'Ödeme − Satış', 'para'], ['devir', 'Devir (Bakiye)', 'para genel'], ['not', 'Not', 'metin']].filter(c => !c[3] || var_(c[0]));
    // tutara tıklayınca kalem detayı (12 Kas 2026): satış alanlarında ihracat faturaları, ödeme alanlarında ödeme kalemleri; toplam ödeme tüm ödeme kalemleri
    const kalemSay = (y, alan) => (s.kalemler || []).filter(k => k.yil === y.yil && (alan === 'toplamOdeme' ? ODEME_ALAN.includes(k.alan) : k.alan === alan)).length;
    const kalemLink = (y, alan, ic) => { const n = kalemSay(y, alan); return `<a href="#" class="ib-sube-kalem" data-id="${s.id}" data-yil="${esc(y.yil)}" data-alan="${alan}" title="${esc(ALAN_AD[alan] || 'Toplam Ödeme')} · ${esc(y.yil)}: kalem detayını aç (${n} kalem)">${ic}</a>${n ? `<small>${n} kalem</small>` : ''}`; };
    const huc = (y, c) => c[0] === 'yil' ? `<td class="sayac">${esc(y.yil)}</td>` : c[0] === 'not' ? notH(y.not) : c[0] === 'fark' || c[0] === 'devir' ? `<td class="${c[2]}">${isaretli(y[c[0]], pb)}</td>` : c[0] === 'sermaye' || c[0] === 'duzeltme' ? `<td class="${c[2]}">${n0(y[c[0]]) ? tutar(y[c[0]], pb) : bos}</td>` : `<td class="${c[2]}">${n0(y[c[0]]) || kalemSay(y, c[0]) ? kalemLink(y, c[0], n0(y[c[0]]) ? tutar(y[c[0]], pb) : '0,00 ' + sembol(pb)) : bos}</td>`;
    tablo('subeYil', kol.map(c => c[1]), Y.map(y => `<tr class="${y.devir < -0.005 ? '' : y.devir > 0.005 ? 'kapandi' : ''}">${kol.map(c => huc(y, c)).join('')}</tr>`),
      `<tr>${kol.map(c => c[0] === 'yil' ? `<td>Toplam · ${Y.length} yıl</td>` : c[0] === 'not' ? '<td></td>' : c[0] === 'fark' || c[0] === 'devir' ? `<td class="${c[2]}">${isaretli(s.bakiye, pb)}</td>` : `<td class="${c[2]}">${tutar(s[c[0]] ?? Y.reduce((a, y) => a + n0(y[c[0]]), 0), pb)}</td>`).join('')}</tr>`, 'Yıl satırı girilmemiş; "Düzenle" ile ekleyin.');
    tablo('subeGonderim', ['Tarih', 'Kanal', 'Alıcı', 'Dönem', 'Bakiye', 'Durum', 'Gönderen', 'Not', 'İşlem'],
      s.gonderimler.map(g => `<tr><td>${esc(g.tarih)}</td><td>${g.kanal === 'WhatsApp' ? '<i class="fab fa-whatsapp me-1" style="color:#16a34a"></i>' : '<i class="fas fa-envelope me-1" style="color:#2563eb"></i>'}${esc(g.kanal)}</td><td>${esc(g.alici)}</td><td>${esc(g.donem)}</td><td class="para">${esc(g.bakiye)}</td><td>${gonderimRz(g.durum)}</td><td>${esc(g.kullanici)}</td>${notH(g.not)}<td><button type="button" class="btn btn-light border ib-sube-gonderim-durum" data-id="${g.id}" data-sube="${s.id}" title="Yanıt / durum işaretle (Mutabık · İtirazlı)"><i class="fas fa-reply"></i></button></td></tr>`),
      '', 'Henüz ekstre gönderilmedi.');
    $('#ibNot-subeGonderim').textContent = `${s.gonderimler.length} gönderim · e-posta / WhatsApp`;
    if (!sessiz) k.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
  // ---- şube yıl kalemleri (12 Kas 2026; "70.091,50 $ tıkladım, bu bakiyeyi oluşturan ihracatlar satır satır; ödeme detayları kalem kalem açılmalı")
  const ALAN_AD = { resmiFatura: 'Resmi Fatura', gercekMalBedeli: 'Gerçek Mal Bedeli', iadeFaturasi: 'İade Faturası', komisyon: 'Komisyon', resmiOdeme: 'Resmi Ödeme', digerOdeme: 'Diğer Ödeme', nakliye: 'Nakliye', diger: 'Diğer' };
  const ODEME_ALAN = ['resmiOdeme', 'digerOdeme', 'nakliye', 'diger', 'komisyon'], SATIS_ALAN = ['resmiFatura', 'gercekMalBedeli', 'iadeFaturasi'];
  const kalemDurum = { id: 0, yil: '', alan: '' };
  function subeKalemAc(s, yil, alan) {
    modalTur = 'subeKalem'; belgeDuzenlenen = s; Object.assign(kalemDurum, { id: s.id, yil, alan });
    const y = s.yillar.find(x => x.yil === yil) || {}, pb = s.pb, toplamMi = alan === 'toplamOdeme', alanlar = toplamMi ? ODEME_ALAN : [alan];
    const K = (s.kalemler || []).filter(k => k.yil === yil && alanlar.includes(k.alan)), kTop = K.reduce((a, k) => a + n0(k.tutar), 0), yilDeger = toplamMi ? n0(y.toplamOdeme) : n0(y[alan]), fark = Math.round((yilDeger - kTop) * 100) / 100;
    const satisMi = SATIS_ALAN.includes(alan);
    // referans: aynı yıl + şube ülkesi (+ şirket) ihracat dosyaları (satış alanları) ya da bu dosyalara bağlı banka tahsilatları (ödeme alanları)
    const yilBas = (yil || '').slice(0, 4), yilSon = (yil || '').includes('-') ? yil.split('-')[1].trim().slice(0, 4) : yilBas;
    const yilUyar = v => { const t = String(v || '').slice(0, 4); return t >= yilBas && t <= yilSon; };
    const D = V.dosyalar.filter(d => d.dabDurumu !== 'İptal' && (d.ulke || '').toLocaleLowerCase('tr-TR') === (s.ulke || '').toLocaleLowerCase('tr-TR') && (!s.sirket || d.sirket === s.sirket) && yilUyar(d.yil) && !K.some(k => k.belgeNo && (k.belgeNo === d.faturaNo || k.belgeNo === d.dosyaNo)));
    const T = satisMi ? [] : V.tahsilatlar.filter(t => t.dosyaId && D.concat(V.dosyalar.filter(d => (d.ulke || '').toLocaleLowerCase('tr-TR') === (s.ulke || '').toLocaleLowerCase('tr-TR'))).some(d => d.id === t.dosyaId) && yilUyar(t.tarih) && !K.some(k => k.belgeNo && k.belgeNo === (t.referans || 'TAH-' + t.id)));
    $('#ibMBaslik').textContent = `${s.sube} · ${yil} · ${toplamMi ? 'Toplam Ödeme' : ALAN_AD[alan]} · kalem detayı`;
    $('#ibMNot').textContent = `Yıl satırı ${tutar(yilDeger, pb)} · kalem toplamı ${tutar(kTop, pb)}${Math.abs(fark) > 0.004 ? ' · fark ' + tutar(fark, pb) : ' · eşit'} · kaynak tabloda yalnız yıllık toplam var; kalemler buradan girilir`;
    const alanSec = v => `<select class="form-select" data-k="alan">${alanlar.map(a => `<option value="${a}" ${a === v ? 'selected' : ''}>${ALAN_AD[a]}</option>`).join('')}</select>`;
    const satir = k => `<tr><td>${toplamMi ? esc(ALAN_AD[k.alan]) : ''}</td><td>${tarih(k.tarih)}</td><td>${esc(k.belgeNo || '—')}</td><td>${esc(k.aciklama || '')}${k.not ? `<br><small class="text-muted">${esc(k.not)}</small>` : ''}</td><td class="para">${tutar(k.tutar, pb)}</td><td><small>${esc(k.kaynak || '')} · ${esc(k.olusturan || '')}</small></td><td><button type="button" class="btn btn-light border text-danger ib-kalem-sil" data-id="${k.id}" title="Kalemi sil"><i class="fas fa-xmark"></i></button></td></tr>`;
    const yeni = `<tr class="table-light"><td>${toplamMi ? alanSec(alan === 'toplamOdeme' ? 'resmiOdeme' : alan) : `<input type="hidden" data-k="alan" value="${alan}">${satisMi ? 'Fatura' : 'Ödeme'}`}</td><td><input class="form-control" type="date" data-k="tarih" value="${yilBas ? yilBas + '-01-01' : ''}"></td><td><input class="form-control" data-k="belgeNo" placeholder="${satisMi ? 'Fatura / GÇB no' : 'Dekont / referans'}"></td><td><input class="form-control" data-k="aciklama" placeholder="Açıklama"></td><td><input class="form-control" type="number" step="0.01" data-k="tutar" placeholder="0,00" style="text-align:right"></td><td><input class="form-control" data-k="not" placeholder="Not"></td><td><button type="button" class="btn btn-primary ib-kalem-ekle" title="Kalem ekle"><i class="fas fa-plus"></i></button></td></tr>`;
    $('#ibMGovde').innerHTML = `<div class="ib2-onizle mb-2"><div><b>Şube:</b> ${esc(s.sube)} · ${esc(s.ulke)}${s.sirket ? ' · ' + esc(s.sirket) : ''}</div><div><b>Yıl satırı (${esc(toplamMi ? 'Toplam Ödeme' : ALAN_AD[alan])}):</b> ${tutar(yilDeger, pb)}</div><div><b>Kalem toplamı:</b> ${tutar(kTop, pb)} (${K.length} kalem)${Math.abs(fark) > 0.004 ? ` · <span style="color:#b91c1c;font-weight:800">fark ${tutar(fark, pb)}</span>` : ' · <span class="ib2-rz ok">Eşit</span>'}</div></div>
      <table class="ib2-kalem"><thead><tr><th>${toplamMi ? 'Alan' : 'Tür'}</th><th>Tarih</th><th>Belge No</th><th>Açıklama</th><th style="text-align:right">Tutar (${esc(pb)})</th><th>Kaynak</th><th></th></tr></thead><tbody>${K.map(satir).join('') || `<tr><td colspan="7" class="text-muted" style="font-size:.66rem">Bu yıl ve alanda kalem girilmemiş; kaynak tabloda yalnız yıllık toplam (${tutar(yilDeger, pb)}) var. Aşağıdaki satırdan kalem ekleyin ya da referans listesinden alın.</td></tr>`}${yeni}</tbody>${K.length ? `<tfoot><tr><td colspan="4">Toplam · ${K.length} kalem</td><td class="para">${tutar(kTop, pb)}</td><td colspan="2">${Math.abs(fark) > 0.004 && !toplamMi ? `<button type="button" class="btn btn-warning ib-kalem-esitle" style="font-size:.62rem;padding:1px 8px" title="Yıl satırındaki ${esc(ALAN_AD[alan])} tutarını kalem toplamı (${tutar(kTop, pb)}) yapar"><i class="fas fa-equals me-1"></i>Yıl Satırını Kalem Toplamıyla Eşitle</button>` : ''}</td></tr></tfoot>` : ''}</table>
      ${D.length || T.length ? `<div class="mt-2" style="font-size:.6rem;font-weight:800;color:#64748b;text-transform:uppercase;letter-spacing:.04em">${satisMi ? `Referans · ${esc(s.ulke)} ülkesine ${esc(yil)} yılı ihracat dosyaları (bu rapordaki beyanname / İBKB dosyaları)` : `Referans · ${esc(s.ulke)} dosyalarına bağlı banka tahsilatları (${esc(yil)})`}</div>
      <table class="ib2-kalem"><thead><tr><th>Şirket</th><th>Dosya / Fatura</th><th>Tarih</th><th>Müşteri / Gönderen</th><th style="text-align:right">Tutar</th><th></th></tr></thead><tbody>${satisMi
        ? D.slice(0, 60).map(d => `<tr><td>${esc(d.sirket)}</td><td>${esc(d.dosyaNo)}${d.faturaNo ? ' · ' + esc(d.faturaNo) : ''}<br><small>${esc(d.beyannameNo || '')}</small></td><td>${tarih(d.baslangic)}</td><td>${esc(d.musteri)}</td><td class="para">${tutar(d.gercekBedelEtkin, d.doviz)}</td><td><button type="button" class="btn btn-light border ib-kalem-aday" data-belge="${esc(d.faturaNo || d.dosyaNo)}" data-tarih="${esc(d.baslangic || '')}" data-aciklama="${esc(d.musteri + ' · ' + d.sirket + ' · ' + d.dosyaNo)}" data-tutar="${d.gercekBedelEtkin}" data-kaynak="İhracat dosyası" title="Kalem olarak ekle"><i class="fas fa-plus"></i> Ekle</button></td></tr>`).join('')
        : T.slice(0, 60).map(t => `<tr><td>${esc(t.sirket)}</td><td>${esc(t.dosyaNo || '')} · ${esc(t.referans || 'TAH-' + t.id)}</td><td>${tarih(t.tarih)}</td><td>${esc(t.gonderen || '')} · ${esc(t.banka || '')}</td><td class="para">${tutar(t.tutar, t.doviz)}</td><td><button type="button" class="btn btn-light border ib-kalem-aday" data-belge="${esc(t.referans || 'TAH-' + t.id)}" data-tarih="${esc(t.tarih || '')}" data-aciklama="${esc((t.gonderen || '') + ' · ' + (t.banka || '') + ' · ' + (t.dosyaNo || ''))}" data-tutar="${t.tutar}" data-kaynak="Banka tahsilatı" title="Kalem olarak ekle"><i class="fas fa-plus"></i> Ekle</button></td></tr>`).join('')}</tbody></table>
      <small class="text-muted" style="font-size:.6rem">Referans satırları ülkeye göre eşleştirilmiştir (şube carisi ihracat dosyasında ayrı yazılmaz); eklenen kalem belge numarasıyla listeden düşer. Dövizi şubeninkinden farklı tutarlar olduğu gibi aktarılır, gerekirse düzeltin.</small>` : `<small class="text-muted" style="font-size:.6rem">${satisMi ? `${esc(yil)} yılında ${esc(s.ulke)} ülkesine kayıtlı ihracat dosyası yok.` : `${esc(yil)} yılında ${esc(s.ulke)} dosyalarına bağlı banka tahsilatı yok.`}</small>`}`;
    $('#ibMKaydet').style.display = 'none';
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  async function kalemYaz(o) {
    const j = await post('/IhracatFinans/SubeKalemKaydet', Object.assign({ subeId: kalemDurum.id, yil: kalemDurum.yil }, o));
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    veri(j.veri); mesaj('success', esc(j.message)); subeKalemAc(V.subeler.find(x => x.id === kalemDurum.id), kalemDurum.yil, kalemDurum.alan);
  }
  const yilSatirH = (y) => { y = y || {}; const sayi = k => `<td><input class="form-control" type="number" step="0.01" data-k="${k}" value="${esc(y[k] || '')}"></td>`; return `<tr><td><input class="form-control" data-k="yil" value="${esc(y.yil || '')}" placeholder="2026"></td>${['sermaye', 'resmiFatura', 'gercekMalBedeli', 'iadeFaturasi', 'komisyon', 'resmiOdeme', 'digerOdeme', 'nakliye', 'diger', 'duzeltme'].map(sayi).join('')}<td class="metin"><input class="form-control" data-k="not" value="${esc(y.not || '')}"></td><td><button type="button" class="btn btn-light border text-danger ib-sube-yil-sil" title="Satırı kaldır"><i class="fas fa-xmark"></i></button></td></tr>`; };
  function subeAc(s) {
    modalTur = 'sube'; belgeDuzenlenen = s || null; const yeni = !s; s = s || { kod: '', sube: '', ulke: '', sirket: '', urunGrubu: '', temsilci: '', sorumlu: '', pb: 'USD', eposta: '', telefon: '', adres: '', not: '', yillar: [] };
    $('#ibMBaslik').textContent = yeni ? 'Yeni Yurtdışı Şube' : `Şube · ${s.sube} · ${s.ulke}`; $('#ibMNot').textContent = yeni ? '' : `${s.kod} · kaynak ${s.kaynak || '—'}`;
    $('#ibMGovde').innerHTML = `
      <div class="ib2-bolum"><div class="b"><span><i class="fas fa-globe me-1"></i>Şube</span></div><div class="g"><div class="ib2-form">
        ${alan('kod', 'Kod (Boş = Otomatik)', 'text', s.kod)}${alan('sube', 'Şube Adı', 'text', s.sube)}${alan('ulke', 'Ülke', 'text', s.ulke)}${alan('sirket', 'Grup Şirketi', 'text', s.sirket).replace('<input ', '<input list="ibSirketler" ')}
        <datalist id="ibSirketler">${V.sirketler.map(x => `<option value="${esc(x)}">`).join('')}</datalist>
        ${alan('urunGrubu', 'Ürün Grubu (Boya / Kağıt)', 'text', s.urunGrubu)}${alan('temsilci', 'Satış Temsilcisi', 'text', s.temsilci).replace('<input ', '<input list="ibTemsilciler" ')}${alan('sorumlu', 'Şube Sorumlusu', 'text', s.sorumlu)}${alan('pb', 'Para Birimi', 'select', { liste: V.dovizler, v: s.pb })}
        <datalist id="ibTemsilciler">${(V.temsilciler || []).map(x => `<option value="${esc(x)}">`).join('')}</datalist>
        ${alan('eposta', 'E-posta (Ekstre Gönderimi)', 'text', s.eposta)}${alan('telefon', 'Telefon / WhatsApp (Ülke Koduyla, +212…)', 'text', s.telefon)}${alan('adres', 'Adres', 'text', s.adres, 'g2')}${alan('not', 'Not', 'uzun', s.not)}
      </div></div></div>
      <div class="ib2-bolum"><div class="b"><span><i class="fas fa-calendar me-1"></i>Yıl Satırları (${esc(s.pb)})</span><button type="button" class="btn btn-light btn-sm border" id="ibSubeYilEkle" style="font-size:.62rem;padding:0 8px"><i class="fas fa-plus me-1"></i>Yıl Ekle</button></div><div class="g p-1">
        <table class="ib2-yil"><thead><tr><th>Yıl</th><th>Sermaye</th><th>Resmi Fatura</th><th>Gerçek Mal Bedeli</th><th>İade Faturası</th><th>Komisyon</th><th>Resmi Ödeme</th><th>Diğer Ödeme</th><th>Nakliye</th><th>Diğer</th><th>Düzeltme (±)</th><th>Not</th><th></th></tr></thead><tbody id="ibSubeYillar">${(s.yillar || []).map(yilSatirH).join('')}</tbody></table>
        <small class="text-muted" style="font-size:.6rem">Ödeme − Satış = sermaye + ödemeler + düzeltme − satış esası (gerçek mal bedeli varsa o, yoksa resmi fatura − iade). Ondalık için nokta ya da virgül.</small>
      </div></div>`;
    $('#ibSubeYilEkle').addEventListener('click', () => { const t = $('#ibSubeYillar'); const son = t.lastElementChild; const y = son ? String(+((son.querySelector('[data-k=yil]').value || '').slice(0, 4)) + 1 || '') : String(new Date().getFullYear()); t.insertAdjacentHTML('beforeend', yilSatirH({ yil: y })); });
    $('#ibMGovde').addEventListener('click', e => { const x = e.target.closest('.ib-sube-yil-sil'); if (x) x.closest('tr').remove(); });
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  function subeOku() {
    const g = id => ($('#ibf_' + id) || {}).value ?? '';
    const yillar = $$('#ibSubeYillar tr').map(tr => { const v = k => tr.querySelector(`[data-k="${k}"]`).value; const o = { yil: v('yil').trim(), not: v('not') }; ['sermaye', 'resmiFatura', 'gercekMalBedeli', 'iadeFaturasi', 'komisyon', 'resmiOdeme', 'digerOdeme', 'nakliye', 'diger', 'duzeltme'].forEach(k => o[k] = SAYI(v(k))); return o; }).filter(y => y.yil);
    return { id: belgeDuzenlenen ? belgeDuzenlenen.id : 0, kod: g('kod'), sube: g('sube'), ulke: g('ulke'), sirket: g('sirket'), urunGrubu: g('urunGrubu'), temsilci: g('temsilci'), sorumlu: g('sorumlu'), paraBirimi: g('pb'), eposta: g('eposta'), telefon: g('telefon'), adres: g('adres'), not: g('not'), yillar };
  }
  // ekstre gönderimi: kanal E-posta (mailto:) ya da WhatsApp (wa.me); kayıt sunucuda, mesaj şablondan (SMTP / WhatsApp Business API bağlanınca doğrudan gider)
  const ekstreMetni = (s, donem) => { const sat = s.yillar.filter(y => !donem.yil || donem.yil.includes(y.yil)); const L = [`Sayın ${s.sube} (${s.ulke}) yetkilisi,`, '', `${s.sirket || 'Uras Grubu'} ile şubeniz arasındaki ${donem.ad} dönemi cari hesap ekstresi aşağıdadır. Lütfen kayıtlarınızla karşılaştırıp mutabık olduğunuzu ya da itirazınızı bildiriniz.`, '', `Yıl | Satış | Ödeme | Ödeme − Satış | Devir (${s.pb})`]; sat.forEach(y => L.push(`${y.yil} | ${para(y.satisEsas)} | ${para(y.toplamOdeme)} | ${para(y.fark)} | ${para(y.devir)}`)); L.push('', `BAKİYE (${tarih(V.bugun)}): ${tutar(s.bakiye, s.pb)} · ${s.bakiye < 0 ? 'şube şirketimize borçlu' : s.bakiye > 0 ? 'şirketimiz şubeye borçlu' : 'bakiye sıfır'}`, '', `Ayrıntılı ekstre: ${location.origin}/IhracatFinans/SubeEkstre/${s.id}`, '', `Saygılarımızla,`, `${s.temsilci || s.sirket || 'Uras Grubu'} · İhracat Finansal Takibi`); return L.join('\n'); };
  function ekstreAc(s) {
    modalTur = 'subeEkstre'; belgeDuzenlenen = s;
    const donem = { ad: s.yilSayisi ? `${s.ilkYil} – ${s.sonYil}` : String(new Date().getFullYear()), yil: null };
    $('#ibMBaslik').textContent = `Ekstre Gönder · ${s.sube} · ${s.ulke}`; $('#ibMNot').textContent = s.sonGonderim ? `Son gönderim ${s.sonGonderim.tarih} · ${s.sonGonderim.kanal} · ${s.sonGonderim.durum}` : 'İlk gönderim';
    $('#ibMGovde').innerHTML = `<div class="ib2-onizle mb-2"><div><b>Bakiye:</b> ${isaretli(s.bakiye, s.pb)}</div><div><b>Dönem:</b> ${esc(donem.ad)} (${s.yilSayisi} yıl)</div><div><b>Temsilci:</b> ${esc(s.temsilci || '—')}</div></div>
      <div class="ib2-form">${alan('kanal', 'Kanal', 'select', { liste: V.subeKanallar || ['E-posta', 'WhatsApp'], v: s.eposta || !s.telefon ? 'E-posta' : 'WhatsApp' })}${alan('alici', 'Alıcı (E-posta Adresi ya da WhatsApp Numarası)', 'text', s.eposta || s.telefon || '', 'g2')}${alan('donem', 'Dönem', 'text', donem.ad)}
      ${alan('konu', 'Konu', 'text', `${s.sube} şube cari hesap ekstresi · ${donem.ad} · mutabakat`, 'g4')}${alan('mesaj', 'Mesaj (Şablondan; Düzenlenebilir)', 'uzun', ekstreMetni(s, donem)).replace('rows="2"', 'rows="12" style="height:auto"').replace('class="form-control"', 'class="form-control ib2-mesaj"')}</div>
      <small class="text-muted" style="font-size:.6rem">Kaydet'te gönderim kaydı yazılır ve cihazdaki posta / WhatsApp uygulaması mesajla açılır (SMTP / WhatsApp Business API bağlanınca doğrudan gönderilir). Ekstre bağlantısı mesajın içinde; PDF için "Ekstre (PDF)".</small>`;
    $('#ibf_kanal').addEventListener('change', () => { const k = $('#ibf_kanal').value; $('#ibf_alici').value = k === 'WhatsApp' ? (s.telefon || '') : (s.eposta || ''); });
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  function gonderimDurumAc(g, s) {
    modalTur = 'subeGonderimDurum'; belgeDuzenlenen = g;
    $('#ibMBaslik').textContent = `Gönderim Yanıtı · ${s.sube} · ${g.tarih} · ${g.kanal}`; $('#ibMNot').textContent = `Alıcı ${g.alici} · ${g.donem} · bakiye ${g.bakiye}`;
    $('#ibMGovde').innerHTML = `<div class="ib2-form">${alan('durum', 'Durum', 'select', { liste: V.subeGonderimDurumlari || ['Gönderildi', 'Mutabık', 'İtirazlı', 'Yanıt Bekleniyor'], v: g.durum })}${alan('not', 'Yanıt / Not', 'text', g.not, 'g3')}</div>`;
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  const CIZ = { ozet: cizOzet, satis: cizSatis, liste: cizListe, tahsilat: cizTahsilat, tamamlanan: cizTamamlanan, belgeler: cizBelgeler, musteri: cizMusteri, sube: cizSube, bakiye: cizBakiye, vade: cizVade, dab: cizDab };
  const OZEL = ['satis', 'tahsilat', 'tamamlanan', 'belgeler', 'bakiye', 'sube'];   // dosya listesi yerine kendi listesini çizen sekmeler (durum süzgeci de kendine göre)
  function ciz() { if (!V) return; const L = liste(); kpi(L); const p = aktifSekme(); if (OZEL.includes(p)) CIZ[p](); else (CIZ[p] || cizOzet)(L); }
  function sekme(p) { if (!CIZ[p]) p = 'ozet'; const onceki = aktifSekme(); $$('#ibSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#ibkb > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); if (V && onceki !== p) { const grup = x => OZEL.includes(x) || x === 'musteri' ? x : 'dosya'; if (grup(onceki) !== grup(p)) { F.dab = ''; } filtreDoldur(); } ciz(); }
  $('#ibSekmeler').addEventListener('click', e => { const b = e.target.closest('button[data-p]'); if (b) sekme(b.dataset.p); });
  ['#ibS', '#ibY', '#ibD', '#ibT', '#ibV'].forEach(s => $(s).addEventListener('change', () => { F.sirket = $('#ibS').value; F.yil = $('#ibY').value; F.dab = $('#ibD').value; F.takip = $('#ibT').value; F.doviz = $('#ibV').value; ciz(); }));
  $('#ibAra').addEventListener('input', () => { F.ara = $('#ibAra').value; ciz(); });
  $('#ibTemizle').addEventListener('click', () => { F = { sirket: '', yil: '', dab: '', takip: '', doviz: '', ara: '' }; kpi.takipOlmayan = null; kpi.acik = ''; ['#ibS', '#ibY', '#ibD', '#ibT', '#ibV', '#ibAra'].forEach(s => $(s).value = ''); ciz(); });
  ['#ibD', '#ibT'].forEach(s => $(s).addEventListener('change', () => { kpi.takipOlmayan = null; }));   // elle süzgeç seçilince kartın gizli süzgeci kalkar
  $('#ibExcel').addEventListener('click', () => {
    const p = aktifSekme(), ad = ($(`#ibSekmeler button[data-p="${p}"]`) || {}).textContent?.trim() || p;
    if (!window.TabloExcel) { mesaj('danger', 'Excel bileşeni yüklenemedi; sayfayı yenileyin.'); return; }
    const sayfalar = p === 'ozet'
      ? [['ozetSirket', 'Şirket × Yıl'], ['ozetTemsilci', 'Yıl × Satış Temsilcisi'], ['ozetSatis', 'İhracat Satışları'], ['ozetTahsilat', 'Banka Tahsilatı · İBKB'], ['ozetSube', 'Şube Bakiyeleri'], ['ozetGorev', 'Bekleyen Görevler'], ['ozetGorevPerf', 'Düzeltme Performansı'], ['ozetDoviz', 'Döviz Bazında Bedel'], ['ozetUlke', 'Ülke Bazında'], ['ozetDurum', 'Durum Dağılımı'], ['ozetBakiye', 'Cari Bakiye Özeti'], ['ozetAy', 'Aylık Dosya Akışı']].map(([t, a], i) => Object.assign({ tablo: '#ibTablo-' + t, ad: a.slice(0, 31), baslik: 'İhracat Finansal Takibi · Özet · ' + a }, i === 0 ? { kpi: '#ibKpi' } : {}))
      : p === 'bakiye'
        ? [{ tablo: '#ibTablo-bakiye', ad: 'Cari Bakiyeler', baslik: 'İhracat Finansal Takibi · Cari Bakiyeler (Mizan)', kpi: '#ibKpi' }, { tablo: '#ibTablo-gorev', ad: 'Bekleyen Görevler', baslik: 'Cari Düzeltme Görevleri' }, { tablo: '#ibTablo-gorevPerf', ad: 'Düzeltme Performansı', baslik: 'Düzeltme Performansı · Kişi Bazında' }]
      : p === 'sube' && subeSecili
        ? [{ tablo: '#ibTablo-sube', ad: 'Şube Bakiyeleri', baslik: 'İhracat Finansal Takibi · Şube Bakiyeleri', kpi: '#ibKpi' }, { tablo: '#ibTablo-subeYil', ad: 'Şube Detayı', baslik: $('#ibSubeDetayBaslik').textContent.trim() }, { tablo: '#ibTablo-subeGonderim', ad: 'Gönderimler', baslik: 'Ekstre / Mutabakat Gönderimleri' }]
        : [{ tablo: '#ibTablo-' + p, ad: ad.slice(0, 31), baslik: 'İhracat Finansal Takibi · ' + ad, kpi: '#ibKpi' }];
    window.TabloExcel.indir('ibkb_' + p, sayfalar).catch(err => mesaj('danger', 'Excel indirilemedi: ' + esc(err.message)));
  });

  // ---------- form (ekle / düzenle)
  const SAYI = (v, ond = 2) => { let s = String(v ?? '').trim(); if (s.includes(',')) s = s.replace(/\./g, '').replace(',', '.'); const x = Number(s); return isNaN(x) ? 0 : Math.round(x * 10 ** ond) / 10 ** ond; };
  const alan = (id, etiket, tip, deger, ek) => {
    const v = deger ?? '';
    if (tip === 'select') return `<div class="${ek || ''}"><label for="ibf_${id}">${esc(etiket)}</label><select class="form-select" id="ibf_${id}">${(deger.liste || []).map(x => `<option value="${esc(x)}" ${x === deger.v ? 'selected' : ''}>${esc(x)}</option>`).join('')}</select></div>`;
    if (tip === 'uzun') return `<div class="${ek || 'g4'}"><label for="ibf_${id}">${esc(etiket)}</label><textarea class="form-control" id="ibf_${id}" rows="2">${esc(v)}</textarea></div>`;
    return `<div class="${ek || ''}"><label for="ibf_${id}">${esc(etiket)}</label><input class="form-control" id="ibf_${id}" type="${tip === 'date' ? 'date' : tip === 'sayi' ? 'number' : 'text'}" ${tip === 'sayi' ? 'step="0.01" min="0"' : ''} value="${esc(v)}"></div>`;
  };
  const dabSatirH = (p, i) => `<tr><td><input class="form-control" type="number" step="0.01" min="0" data-k="tutar" value="${esc(p.tutar ?? '')}"></td><td><select class="form-select" data-k="doviz">${V.dovizler.map(x => `<option ${x === (p.doviz || 'USD') ? 'selected' : ''}>${x}</option>`).join('')}</select></td><td><input class="form-control" type="date" data-k="tarih" value="${esc(p.tarih || '')}"></td><td><input class="form-control" data-k="banka" list="ibBankalar" value="${esc(p.banka || '')}"></td><td><input class="form-control" data-k="no" value="${esc(p.no || '')}"></td><td><input class="form-control" data-k="not" value="${esc(p.not || '')}"></td><td><button type="button" class="btn btn-light border text-danger ib-dab-sil" title="Satırı kaldır"><i class="fas fa-xmark"></i></button></td></tr>`;
  let duzenlenen = null, modalTur = 'dosya', eslesTahsilat = null, belgeDuzenlenen = null;
  function formAc(d, onDolu) {
    modalTur = 'dosya'; duzenlenen = d || null; const yeni = !d; d = d || Object.assign({ sirket: F.sirket || V.sirketler[0], yil: new Date().getFullYear(), dosyaNo: '', musteriKodu: '', musteri: '', ulke: '', faturaNo: '', siparisNo: '', baslangic: '', kapanis: '', beyannameNo: '', doviz: 'USD', tutar: '', ulkeOran: 100, dabDurumu: 'Bekliyor', dabBanka: '', dabKalan: '', dabNotu: '', dablar: [], not: '', aciklama: '' }, onDolu || {});
    $('#ibMBaslik').textContent = yeni ? 'Yeni İhracat Dosyası' : `İhracat Dosyası · ${d.sirket} · ${d.dosyaNo}`;
    $('#ibMNot').textContent = yeni ? '' : `Kaynak: ${d.kaynak || '—'} · oluşturma ${tarih(d.olusturma)} ${d.olusturan || ''}${d.guncelleme ? ' · güncelleme ' + d.guncelleme + ' ' + (d.guncelleyen || '') : ''}`;
    $('#ibMGovde').innerHTML = `
      <div class="ib2-bolum"><div class="b"><span><i class="fas fa-file-export me-1"></i>İhracat Dosyası</span></div><div class="g"><div class="ib2-form">
        ${alan('sirket', 'Şirket', 'select', { liste: V.sirketler, v: d.sirket })}${alan('yil', 'Yıl', 'sayi', d.yil)}${alan('dosyaNo', 'Dosya No', 'text', d.dosyaNo)}${alan('beyannameNo', 'GÇB (Beyanname) No', 'text', d.beyannameNo)}
        ${alan('musteriKodu', 'Müşteri Kodu', 'text', d.musteriKodu)}${alan('musteri', 'Müşteri', 'text', d.musteri, 'g2')}${alan('ulke', 'Ülke (Ek-2 Muaf · Ek-3 %50 Listesi)', 'text', d.ulke).replace('<input ', '<input list="ibUlkeler" ')}
        <datalist id="ibUlkeler">${[...V.ulkeMuaf.map(u => u + ' (Ek-2 muaf)'), ...V.ulkeYarim.map(u => u + ' (Ek-3 %50)')].map(u => `<option value="${esc(u.split(' (')[0])}">${esc(u)}</option>`).join('')}</datalist>
        ${alan('faturaNo', 'Fatura No (İhracat Satışı)', 'text', d.faturaNo)}${alan('siparisNo', 'Sipariş No', 'text', d.siparisNo)}${alan('baslangic', 'Başlangıç (Fiili İhraç)', 'date', d.baslangic)}${alan('kapanis', 'Kapanış', 'date', d.kapanis)}
        ${alan('temsilci', 'Satış Temsilcisi' + (d.temsilciKaynak === 'cari kart' ? ` (Boş = Cari Kart: ${d.temsilci})` : ' (Boş = Cari Kartın Temsilcisi)'), 'text', d.temsilciKayit || '').replace('<input ', '<input list="ibTemsilciler" ')}
        <datalist id="ibTemsilciler">${(V.temsilciler || []).map(x => `<option value="${esc(x)}">`).join('')}</datalist>
        ${alan('tutar', 'Beyanname Tutarı', 'sayi', d.tutar)}${alan('doviz', 'Döviz', 'select', { liste: V.dovizler, v: d.doviz || 'USD' })}
        ${alan('gercekBedel', 'Gerçek İhracat Bedeli (Proforma / Fatura; Boş = Beyanname)', 'sayi', d.gercekBedel || '')}${alan('farkNedeni', 'Fark Nedeni', 'text', d.farkNedeni).replace('<input ', '<input list="ibFarkNedenleri" ')}
        <datalist id="ibFarkNedenleri">${(V.farkNedenleri || []).map(x => `<option value="${esc(x)}">`).join('')}</datalist>
        ${alan('ulkeOran', 'Ülke Gerekliliği % (Getirilmesi Gereken Pay)', 'sayi', d.ulkeOran)}${alan('aciklama', 'Dosya Açıklaması (Excel)', 'text', d.aciklama, 'g3')}
      </div></div></div>
      <div class="ib2-bolum"><div class="b"><span><i class="fas fa-building-columns me-1"></i>DAB / İBKB (Banka)</span></div><div class="g"><div class="ib2-form">
        ${alan('dabDurumu', 'DAB / İBKB Durumu', 'select', { liste: V.dabDurumlari, v: d.dabDurumu })}${alan('dabBanka', 'Banka', 'text', d.dabBanka)}${alan('dabKalan', 'Kısmi Kalan Tutar (Beyanname Dövizi)', 'sayi', d.dabKalan || '')}${alan('dabNotu', 'Banka Bildirimi / DAB Notu', 'text', d.dabNotu)}
      </div>
      <table class="ib2-dab mt-2"><thead><tr><th>DAB Tutarı</th><th>Döviz</th><th>Tarih</th><th>Banka</th><th>DAB / İBKB No</th><th>Not</th><th></th></tr></thead><tbody id="ibDabSatirlar">${(d.dablar || []).map(dabSatirH).join('')}</tbody></table>
      <datalist id="ibBankalar">${V.bankalar.map(b => `<option value="${esc(b)}">`).join('')}</datalist>
      <button type="button" class="btn btn-light btn-sm border mt-1" id="ibDabEkle" style="font-size:.64rem"><i class="fas fa-plus me-1"></i>DAB Satırı Ekle</button>
      </div></div>
      <div class="ib2-bolum"><div class="b"><span><i class="fas fa-calculator me-1"></i>Önizleme</span></div><div class="g"><div class="ib2-onizle" id="ibOnizle"></div></div></div>
      <div class="ib2-form">${alan('not', 'Not', 'uzun', d.not)}</div>`;
    onizle();
    $('#ibMGovde').addEventListener('input', onizle); $('#ibMGovde').addEventListener('change', onizle);
    $('#ibDabEkle').addEventListener('click', () => { $('#ibDabSatirlar').insertAdjacentHTML('beforeend', dabSatirH({ doviz: $('#ibf_doviz').value, banka: $('#ibf_dabBanka').value }, 0)); });
    $('#ibMGovde').addEventListener('click', e => { const s = e.target.closest('.ib-dab-sil'); if (s) { s.closest('tr').remove(); onizle(); } });
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  function formOku() {
    const g = id => ($('#ibf_' + id) || {}).value ?? '';
    const dablar = $$('#ibDabSatirlar tr').map(tr => ({ tutar: SAYI(tr.querySelector('[data-k="tutar"]').value), doviz: tr.querySelector('[data-k="doviz"]').value, tarih: tr.querySelector('[data-k="tarih"]').value || null, banka: tr.querySelector('[data-k="banka"]').value, no: tr.querySelector('[data-k="no"]').value, not: tr.querySelector('[data-k="not"]').value })).filter(p => p.tutar > 0);
    return { id: duzenlenen ? duzenlenen.id : 0, sirket: g('sirket'), yil: +g('yil') || 0, dosyaNo: g('dosyaNo'), aciklama: g('aciklama'), musteriKodu: g('musteriKodu'), musteri: g('musteri'), ulke: g('ulke'), faturaNo: g('faturaNo'), siparisNo: g('siparisNo'), temsilci: g('temsilci'), baslangic: g('baslangic') || null, kapanis: g('kapanis') || null, beyannameNo: g('beyannameNo'), doviz: g('doviz'), tutar: SAYI(g('tutar')), gercekBedel: SAYI(g('gercekBedel')), farkNedeni: g('farkNedeni'), ulkeOran: SAYI(g('ulkeOran')) || 100, dabDurumu: g('dabDurumu'), dabBanka: g('dabBanka'), dabKalan: SAYI(g('dabKalan')), dabNotu: g('dabNotu'), dablar, not: g('not') };
  }
  function onizle() {
    // otomatik kurallar (sunucu Kaydet ile aynı): Ek-2 ülkesi → oran 0 + Gerek Yok; Ek-3 ülkesi → oran 50; ABD doları karşılığı ≤ eşik ve durum Bekliyor → Gerek Yok
    let otomatik = ''; const sel = $('#ibf_dabDurumu'), oranEl = $('#ibf_ulkeOran'), ulke = ($('#ibf_ulke') || {}).value || '';
    if (sel && oranEl) {
      if (V.ulkeMuaf.includes(ulke)) { if (oranEl.value !== '0') oranEl.value = 0; if (sel.value === 'Bekliyor') sel.value = 'Gerek Yok'; otomatik = `${ulke}: Ek-2 ülkesi, bedel getirme zorunlu değil`; }
      else if (V.ulkeYarim.includes(ulke) && SAYI(oranEl.value) >= 100) { oranEl.value = 50; otomatik = `${ulke}: Ek-3 ülkesi, %50`; }
      const o0 = formOku(), usd = usdK(o0.tutar, o0.doviz);
      if (sel.value === 'Bekliyor' && usd > 0 && usd <= V.esikUsd) { sel.value = 'Gerek Yok'; otomatik = `${kisaUsd(usd)} karşılığı ≤ ${kisaUsd(V.esikUsd)} eşiği: DAB / İBKB gerekmez (otomatik)`; }
    }
    const o = formOku(), muaf = o.dabDurumu === 'Gerek Yok' || o.dabDurumu === 'İptal', gereken = muaf ? 0 : Math.round(o.tutar * o.ulkeOran) / 100, usd = usdK(o.tutar, o.doviz);
    const ayni = o.dablar.filter(p => p.doviz === o.doviz).reduce((s, p) => s + p.tutar, 0);
    const getirilen = muaf ? 0 : (o.dabDurumu === 'Yapıldı' || o.dabDurumu === 'Peşin Yapıldı') ? gereken : o.dabDurumu === 'Kısmi' ? (o.dabKalan > 0 ? Math.max(0, o.tutar - o.dabKalan) : ayni) : ayni;   // sunucu formülüyle aynı: kısmi → tutar − bankanın bildirdiği kalan
    const kalan = muaf || o.dabDurumu === 'Yapıldı' || o.dabDurumu === 'Peşin Yapıldı' ? 0 : Math.max(0, gereken - getirilen);
    const vade = o.baslangic ? new Date(new Date(o.baslangic).getTime() + V.vadeGun * 86400000) : null, kalanGun = vade ? Math.round((vade - new Date(V.bugun)) / 86400000) : null;
    const el = $('#ibOnizle'); if (!el) return;
    const gercek = o.gercekBedel > 0 && Math.abs(o.gercekBedel - o.tutar) > 0.004 ? o.gercekBedel : 0, fark = gercek ? Math.round((o.tutar - gercek) * 100) / 100 : 0, gerekenGercek = muaf ? 0 : Math.round((gercek || o.tutar) * o.ulkeOran) / 100, kalanGercek = muaf || o.dabDurumu === 'Yapıldı' || o.dabDurumu === 'Peşin Yapıldı' ? 0 : Math.max(0, gerekenGercek - getirilen);
    el.innerHTML = [['ABD Doları Karşılığı', usd ? kisaUsd(usd) + (usd <= V.esikUsd ? ' · eşik altı' : ' · eşik üstü') : '—'], ['Gereken Bedel (Beyanname)', tutar(gereken, o.doviz)], ['Getirilen', tutar(getirilen, o.doviz) + (o.dablar.length ? ` (${o.dablar.length} DAB)` : '')], ['Kalan (Beyanname)', tutar(kalan, o.doviz)], ['Beyanname − Gerçek Fark', gercek ? `<span style="color:${fark > 0 ? '#b91c1c' : '#b45309'};font-weight:800">${fark > 0 ? '+' : ''}${tutar(fark, o.doviz)}</span> · gereken (gerçek) ${tutar(gerekenGercek, o.doviz)} · kalan (gerçek) ${tutar(kalanGercek, o.doviz)}${fark > 0 ? ` · beyanname fazlası ${tutar(Math.max(0, gereken - gerekenGercek), o.doviz)}` : ''}` : '— (gerçek bedel = beyanname)'], ['Vade', vade ? vade.toLocaleDateString('tr-TR') + (kalanGun !== null ? ` · ${kalanGun} gün` : '') : '—'], ['Otomatik Kural', otomatik || '—']].map(([l, v]) => `<div><b>${l}:</b> ${v}</div>`).join('');
  }
  // ---------- banka tahsilatı formu
  function tahsilatAc(t) {
    modalTur = 'tahsilat'; duzenlenen = t || null; const yeni = !t; t = t || { sirket: F.sirket || V.sirketler[0], banka: '', tarih: V.bugun, gonderen: '', musteriKodu: '', tutar: '', doviz: 'USD', referans: '', aciklama: '' };
    $('#ibMBaslik').textContent = yeni ? 'Banka Tahsilatı (İhracat Bedeli Girişi)' : `Banka Tahsilatı #${t.id} · ${t.banka} · ${tutar(t.tutar, t.doviz)}`;
    $('#ibMNot').textContent = yeni ? 'Banka / ERP entegrasyonu bağlanınca bu kayıtlar otomatik gelir ("Banka (Otomatik)" kaynağıyla).' : `Kaynak: ${t.kaynak} · ${t.olusturan || ''} ${t.olusturma || ''}`;
    $('#ibMGovde').innerHTML = `<div class="ib2-form">
      ${alan('sirket', 'Şirket (Hesap Sahibi)', 'select', { liste: V.sirketler, v: t.sirket })}${alan('banka', 'Banka', 'text', t.banka).replace('<input ', '<input list="ibBankalar" ')}${alan('tarih', 'Tarih (Valör)', 'date', t.tarih)}${alan('referans', 'Banka Referansı / Dekont No', 'text', t.referans)}
      ${alan('gonderen', 'Gönderen (Yurtdışı Alıcı)', 'text', t.gonderen, 'g2')}${alan('musteriKodu', 'Müşteri Kodu', 'text', t.musteriKodu)}${alan('tutar', 'Tutar', 'sayi', t.tutar)}${alan('doviz', 'Döviz', 'select', { liste: V.dovizler, v: t.doviz || 'USD' })}
      ${alan('aciklama', 'Açıklama (Swift / Banka Açıklaması)', 'text', t.aciklama, 'g3')}
      <datalist id="ibBankalar">${V.bankalar.map(b => `<option value="${esc(b)}">`).join('')}</datalist></div>`;
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  function tahsilatOku() { const g = id => ($('#ibf_' + id) || {}).value ?? ''; return { id: duzenlenen ? duzenlenen.id : 0, sirket: g('sirket'), banka: g('banka'), tarih: g('tarih') || null, gonderen: g('gonderen'), musteriKodu: g('musteriKodu'), tutar: SAYI(g('tutar')), doviz: g('doviz'), referans: g('referans'), aciklama: g('aciklama') }; }
  // ---------- eşleştirme: aday dosyalar skorla (aynı şirket + döviz + açık; müşteri kodu ve tutar yakınlığı puan), seçim + "İBKB oluştur"
  function eslestirAc(t) {
    modalTur = 'eslestir'; eslesTahsilat = t;
    const acik = d => d.dabDurumu !== 'İptal' && d.takip !== 'Kapandı';
    const adaylar = V.dosyalar.filter(d => acik(d)).map(d => {
      let skor = 0, neden = [];
      if (d.sirket === t.sirket) { skor += 30; } else neden.push('farklı şirket');
      if ((d.doviz || '') === t.doviz) { skor += 25; } else neden.push('farklı döviz');
      if (t.musteriKodu && d.musteriKodu === t.musteriKodu) { skor += 25; neden.push('müşteri kodu eşleşti'); } else if (t.gonderen && d.musteri && (d.musteri.toLocaleLowerCase('tr-TR').includes(t.gonderen.toLocaleLowerCase('tr-TR').slice(0, 8)) || t.gonderen.toLocaleLowerCase('tr-TR').includes(d.musteri.toLocaleLowerCase('tr-TR').slice(0, 8)))) { skor += 15; neden.push('müşteri adı benzer'); }
      const hedef = n0(d.kalan) || n0(d.gereken) || n0(d.tutar); if (hedef > 0) { const fark = Math.abs(hedef - t.tutar) / hedef; if (fark < 0.005) { skor += 20; neden.push('tutar birebir'); } else if (fark < 0.1) { skor += 12; neden.push('tutar yakın'); } else if (t.tutar < hedef) { skor += 5; neden.push('kısmi ödeme'); } }
      if (d.takip === 'Muaf') skor -= 10;
      return { d, skor, neden: neden.join(', ') };
    }).sort((a, b) => b.skor - a.skor || (a.d.vade || '').localeCompare(b.d.vade || '')).slice(0, 40);
    $('#ibMBaslik').textContent = `Tahsilatı İhracat Dosyasıyla Eşleştir · ${t.banka} · ${tutar(t.tutar, t.doviz)} · ${tarih(t.tarih)}`;
    $('#ibMNot').textContent = 'Seçilen dosyaya DAB satırı yazılır; Kaydet\'e basınca İBKB belgesi oluşturulup oluşturulmayacağı sorulur.';
    $('#ibMGovde').innerHTML = `<div class="ib2-tahsilat-ozet"><b>Gelen bedel:</b> ${tutar(t.tutar, t.doviz)} · ${esc(t.banka || '—')} · ${tarih(t.tarih)} · gönderen ${esc(t.gonderen || '—')}${t.musteriKodu ? ' (' + esc(t.musteriKodu) + ')' : ''}${t.referans ? ' · ref ' + esc(t.referans) : ''}${t.aciklama ? ' · ' + esc(t.aciklama) : ''}</div>
      <div class="ib2-form mb-2"><div class="g2"><label for="ibAdayAra">Aday Dosya Ara (dosya no, GÇB, müşteri)</label><input class="form-control" id="ibAdayAra" placeholder="yazdıkça süzülür"></div><div class="g2"><label>&nbsp;</label><label style="text-transform:none;font-size:.72rem;color:#0f172a"><input type="checkbox" id="ibIbkbOlustur" checked class="me-1">Eşleştirince İBKB belgesi oluştur (bankaya verilecek form)</label></div></div>
      <div class="table-responsive" style="max-height:48vh"><table class="ib2-aday" id="ibAdayTablo"><thead><tr><th></th><th>Uygunluk</th><th>Şirket</th><th>Dosya No</th><th>GÇB No</th><th>Müşteri</th><th>Beyanname</th><th>Gereken</th><th>Getirilen</th><th>Kalan</th><th>Takip</th><th>Vade</th><th>Neden</th></tr></thead><tbody>${adaylar.map(a => `<tr data-id="${a.d.id}" data-ara="${esc([a.d.dosyaNo, a.d.beyannameNo, a.d.musteri, a.d.musteriKodu].join(' ').toLocaleLowerCase('tr-TR'))}"><td><input type="radio" name="ibAday" value="${a.d.id}"></td><td><span class="skor">${a.skor}</span></td><td>${esc(a.d.sirket)}</td><td><b>${esc(a.d.dosyaNo)}</b></td><td>${esc(a.d.beyannameNo || '—')}</td><td>${esc(a.d.musteri)}<br><small>${esc(a.d.musteriKodu || '')}</small></td><td>${tutar(a.d.tutar, a.d.doviz)}</td><td>${tutar(a.d.gereken, a.d.doviz)}</td><td>${tutar(a.d.getirilen, a.d.doviz)}</td><td><b>${tutar(a.d.kalan, a.d.doviz)}</b></td><td>${takipRz(a.d.takip)}</td><td>${tarih(a.d.vade)}</td><td><small>${esc(a.neden)}</small></td></tr>`).join('') || '<tr><td colspan="13" class="text-muted">Açık ihracat dosyası yok.</td></tr>'}</tbody></table></div>`;
    const tb = $('#ibAdayTablo tbody');
    tb.addEventListener('click', e => { const tr = e.target.closest('tr[data-id]'); if (!tr) return; tb.querySelectorAll('tr').forEach(r => r.classList.remove('sec')); tr.classList.add('sec'); tr.querySelector('input[type=radio]').checked = true; });
    $('#ibAdayAra').addEventListener('input', e => { const q = e.target.value.toLocaleLowerCase('tr-TR'); tb.querySelectorAll('tr[data-id]').forEach(r => r.style.display = !q || r.dataset.ara.includes(q) ? '' : 'none'); });
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  // ---------- İBKB belgesi durumu
  function belgeAc(b) {
    modalTur = 'belge'; belgeDuzenlenen = b;
    $('#ibMBaslik').textContent = `İBKB Belgesi · ${b.no} · ${tutar(b.tutar, b.doviz)} · ${b.banka}`; $('#ibMNot').innerHTML = `<a href="/IhracatFinans/Belge/${b.id}" target="_blank" rel="noopener"><i class="fas fa-print me-1"></i>Formu aç / yazdır</a>`;
    $('#ibMGovde').innerHTML = `<div class="ib2-form">${alan('durum', 'Belge Durumu', 'select', { liste: V.belgeDurumlari, v: b.durum })}${alan('bankaIbkbNo', 'Banka İBKB No', 'text', b.bankaIbkbNo)}${alan('bankaTarihi', 'Banka İşlem Tarihi', 'date', b.bankaTarihi)}<div></div>${alan('not', 'Not', 'uzun', b.not)}</div>
      <div class="ib2-onizle mt-2"><div><b>Dosya:</b> ${esc(b.dosyaNo)} · GÇB ${esc(b.beyannameNo || '—')}</div><div><b>Müşteri:</b> ${esc(b.musteri)}</div><div><b>Tahsilat:</b> ${tutar(b.tutar, b.doviz)} · ${tarih(b.tahsilatTarihi)} · ${esc(b.tahsilatRef || '—')}</div></div>`;
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).show();
  }
  $('#ibYeni').addEventListener('click', () => formAc(null));
  $('#ibTahsilatYeni').addEventListener('click', () => tahsilatAc(null));
  $('#ibMKaydet').addEventListener('click', async () => {
    let j;
    if (modalTur === 'dosya') { const o = formOku(); if (!o.sirket || !o.dosyaNo) { mesaj('warning', 'Şirket ve dosya no zorunludur.'); return; } j = await post('/IhracatFinans/Kaydet', o); }
    else if (modalTur === 'tahsilat') { const o = tahsilatOku(); if (!o.sirket || !(o.tutar > 0)) { mesaj('warning', 'Şirket ve tutar zorunludur.'); return; } j = await post('/IhracatFinans/TahsilatKaydet', o); }
    else if (modalTur === 'eslestir') {
      const sec = $('#ibAdayTablo input[name=ibAday]:checked'); if (!sec) { mesaj('warning', 'Eşleştirilecek ihracat dosyasını seçin.'); return; }
      const d = V.dosyalar.find(x => x.id === +sec.value);
      // kullanıcı isteği: belge seçildiğinde "İBKB oluşturmak istiyor musun?" sorulsun, ona göre belge düzenlensin
      const ibkb = window.confirm(`${eslesTahsilat.banka || 'Banka'} · ${tutar(eslesTahsilat.tutar, eslesTahsilat.doviz)} tahsilatı ${d.sirket} · ${d.dosyaNo}${d.beyannameNo ? ' (GÇB ' + d.beyannameNo + ')' : ''} dosyasına bağlanacak.\n\nİBKB belgesi oluşturulsun mu? (bankaya verilecek başvuru formu hazırlanır)\n\nTamam = İBKB oluştur · İptal = yalnız eşleştir`);
      j = await post('/IhracatFinans/Eslestir', { tahsilatId: eslesTahsilat.id, dosyaId: d.id, ibkbOlustur: ibkb });
      if (j.success && j.belgeId) window.open('/IhracatFinans/Belge/' + j.belgeId, '_blank', 'noopener');
    }
    else if (modalTur === 'belge') { const g = id => ($('#ibf_' + id) || {}).value ?? ''; j = await post('/IhracatFinans/BelgeDurum', { id: belgeDuzenlenen.id, durum: g('durum'), bankaIbkbNo: g('bankaIbkbNo'), bankaTarihi: g('bankaTarihi') || null, not: g('not') }); }
    else if (modalTur === 'bakiye') {
      const g = id => ($('#ibf_' + id) || {}).value ?? ''; const gorev = gorevIstiyor() ? gorevOku() : null;
      if (gorev && !gorev.atanan) { mesaj('warning', 'Görev atamak için kişi seçin (ya da kutuyu kaldırın).'); return; }
      j = await post('/IhracatFinans/BakiyeKontrolKaydet', { cariKodu: belgeDuzenlenen.kod, durum: g('durum'), not: g('not'), bakiyeOzet: bakiyeKontrolAc.ozet || '', farkOzet: bakiyeKontrolAc.fark || '', gorev });
      if (j.success && j.gorev && j.gorev.atananEposta) epostaAc(j.gorev.atananEposta, j.gorev.konu, j.gorev.govde);   // atanan kişiye e-posta (posta uygulaması)
    }
    else if (modalTur === 'gorev') {
      const r = $('input[name=ibIslem]:checked'); if (!r) { mesaj('warning', 'İşlem seçin.'); return; } const g = id => ($('#ibf_' + id) || {}).value ?? '';
      const o = { id: belgeDuzenlenen.id, islem: r.value, not: g('not') };
      if (r.value === 'iade' && !o.not.trim()) { mesaj('warning', 'İade nedeni zorunludur.'); return; }
      if (r.value === 'guncelle') { const a = gorevOku(); if (!a.atanan) { mesaj('warning', 'Atanacak kişiyi seçin.'); return; } Object.assign(o, a); }
      if (r.value === 'iptal' && !window.confirm(`#${belgeDuzenlenen.id} görevi iptal edilsin mi?`)) return;
      j = await post('/IhracatFinans/GorevKaydet', o);
      if (j.success && j.gorev && (r.value === 'guncelle' || r.value === 'iade') && j.gorev.atananEposta) epostaAc(j.gorev.atananEposta, j.gorev.konu, j.gorev.govde);
    }
    else if (modalTur === 'cariMutabakat') {
      const g = id => ($('#ibf_' + id) || {}).value ?? ''; const c = belgeDuzenlenen, kanal = g('kanal'), alici = g('alici').trim(), mesajM = g('mesaj'), konu = g('konu');
      if (!alici) { mesaj('warning', kanal === 'WhatsApp' ? 'WhatsApp numarası girin (ülke koduyla).' : 'E-posta adresi girin.'); return; }
      j = await post('/IhracatFinans/CariGonderim', { cariKodu: c.kod, kanal, icerik: g('icerik'), alici, aliciAd: g('aliciAd'), konu, mesaj: mesajM, bakiyeOzet: c.dovizler.map(d => tutar(d.net, d.pb)).join(' · ') });
      if (j.success) {
        if (kanal === 'WhatsApp') window.open('https://wa.me/' + alici.replace(/[^\d]/g, '') + '?text=' + encodeURIComponent(mesajM), '_blank', 'noopener');
        else window.location.href = 'mailto:' + encodeURIComponent(alici) + '?subject=' + encodeURIComponent(konu) + '&body=' + encodeURIComponent(mesajM);
      }
    }
    else if (modalTur === 'cariGonderimDurum') {
      const g = id => ($('#ibf_' + id) || {}).value ?? ''; const c = cariGonderimDurumAc.cari, gorev = gorevIstiyor() ? gorevOku() : null;
      if (gorev && !gorev.atanan) { mesaj('warning', 'Görev atamak için kişi seçin (ya da kutuyu kaldırın).'); return; }
      j = await post('/IhracatFinans/CariGonderim', { id: belgeDuzenlenen.id, durum: g('durum'), not: g('not') });
      if (j.success && gorev) {   // itiraz → düzeltme görevi
        const jg = await post('/IhracatFinans/GorevKaydet', Object.assign({ islem: 'ata', cariKodu: c.kod, gonderimId: belgeDuzenlenen.id }, gorev, { aciklama: gorev.aciklama || ('Müşteri itirazı: ' + g('not')) }));
        if (jg.success) { j = jg; j.message = 'Mutabakat yanıtı işlendi · ' + jg.message; if (jg.gorev && jg.gorev.atananEposta) epostaAc(jg.gorev.atananEposta, jg.gorev.konu, jg.gorev.govde); } else mesaj('warning', 'Yanıt kaydedildi; görev açılamadı: ' + esc(jg.message));
      }
    }
    else if (modalTur === 'sube') { const o = subeOku(); if (!o.sube || !o.ulke) { mesaj('warning', 'Şube adı ve ülke zorunludur.'); return; } j = await post('/IhracatFinans/SubeKaydet', o); if (j.success && j.id) subeSecili = j.id; }
    else if (modalTur === 'subeEkstre') {
      const g = id => ($('#ibf_' + id) || {}).value ?? ''; const s = belgeDuzenlenen, kanal = g('kanal'), alici = g('alici').trim(), mesajM = g('mesaj'), konu = g('konu');
      if (!alici) { mesaj('warning', kanal === 'WhatsApp' ? 'WhatsApp numarası girin (ülke koduyla).' : 'E-posta adresi girin.'); return; }
      j = await post('/IhracatFinans/SubeGonderim', { subeId: s.id, kanal, alici, donem: g('donem'), konu, mesaj: mesajM, bakiye: tutar(s.bakiye, s.pb) });
      if (j.success) {   // cihazdaki posta / WhatsApp uygulamasını mesajla aç
        if (kanal === 'WhatsApp') window.open('https://wa.me/' + alici.replace(/[^\d]/g, '') + '?text=' + encodeURIComponent(mesajM), '_blank', 'noopener');
        else window.location.href = 'mailto:' + encodeURIComponent(alici) + '?subject=' + encodeURIComponent(konu) + '&body=' + encodeURIComponent(mesajM);
      }
    }
    else if (modalTur === 'subeGonderimDurum') { const g = id => ($('#ibf_' + id) || {}).value ?? ''; j = await post('/IhracatFinans/SubeGonderim', { id: belgeDuzenlenen.id, durum: g('durum'), not: g('not') }); }
    if (!j || !j.success) { mesaj('danger', esc((j || {}).message || 'Kaydedilemedi.')); return; }
    bootstrap.Modal.getOrCreateInstance($('#ibModal')).hide(); veri(j.veri); mesaj('success', esc(j.message));
  });
  // KPI kartı: ilgili sekme + süzgeç; kart "açık" kalır (alt metin tam), aynı karta ikinci tıklama kapatır
  $('#ibKpi').addEventListener('click', e => {
    const k = e.target.closest('.ib-kpi'); if (!k) return;
    const baslik = k.querySelector('.l').textContent, sz = JSON.parse(k.dataset.suzgec || '{}');
    kpi.acik = kpi.acik === baslik ? '' : baslik;
    F.dab = sz.dab || ''; F.takip = sz.takip || ''; $('#ibD').value = F.dab; $('#ibT').value = F.takip;
    kpi.takipOlmayan = sz.takipOlmayan || null;
    sekme(k.dataset.sekme);
  });
  const tikla = async e => {
    const d = e.target.closest('.ib-duzenle'); if (d) { e.preventDefault(); formAc(V.dosyalar.find(x => x.id === +d.dataset.id)); return; }
    const s = e.target.closest('.ib-sil'); if (s) { const x = V.dosyalar.find(y => y.id === +s.dataset.id); if (!x || !window.confirm(`${x.sirket} · ${x.dosyaNo} · ${x.musteri}: ihracat dosyası silinsin mi?`)) return; const j = await post('/IhracatFinans/Sil', { id: x.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); return; }
    const m = e.target.closest('.ib-musteri-git'); if (m) { F.ara = m.dataset.ad; $('#ibAra').value = F.ara; sekme('liste'); return; }
    const gt = e.target.closest('.ib-git'); if (gt) { e.preventDefault(); sekme(gt.dataset.p); return; }
    const bk = e.target.closest('.ib-bakiye-kontrol'); if (bk) { bakiyeKontrolAc((V.cariBakiyeler || []).find(c => c.kod === bk.dataset.kod)); return; }
    // düzeltme görevleri + müşteri mutabakatı (12 Kas 2026)
    const ga = e.target.closest('.ib-gorev-ac'); if (ga) { e.preventDefault(); const g = (V.gorevler || []).find(x => x.id === +ga.dataset.id); if (g) gorevAc(g); return; }
    const gc = e.target.closest('.ib-gorev-cari'); if (gc) { e.preventDefault(); F.ara = gc.dataset.kod; $('#ibAra').value = F.ara; sekme('bakiye'); return; }
    const cm = e.target.closest('.ib-cari-mutabakat'); if (cm) { mutabakatAc((V.cariBakiyeler || []).find(c => c.kod === cm.dataset.kod)); return; }
    const cy = e.target.closest('.ib-cari-yanit'); if (cy) { const c = (V.cariBakiyeler || []).find(x => x.kod === cy.dataset.kod); const g = (c.gonderimler || []).find(x => x.id === +cy.dataset.id); if (g) cariGonderimDurumAc(g, c); return; }
    const gs = e.target.closest('#ibGorevSuzgec button'); if (gs) { gorevMod = gs.dataset.g; cizGorev(); return; }
    const bdy = e.target.closest('.ib-bakiye-dosya'); if (bdy) { e.preventDefault(); F.ara = bdy.dataset.kod; $('#ibAra').value = F.ara; sekme('liste'); return; }
    // yurtdışı şube bakiyeleri (11 Kas 2026)
    const sy = e.target.closest('.ib-sube-yeni'); if (sy) { subeAc(null); return; }
    const sdz = e.target.closest('.ib-sube-duzenle'); if (sdz) { subeAc(V.subeler.find(x => x.id === +sdz.dataset.id)); return; }
    const sek = e.target.closest('.ib-sube-ekstre'); if (sek) { ekstreAc(V.subeler.find(x => x.id === +sek.dataset.id)); return; }
    const ssl = e.target.closest('.ib-sube-sil'); if (ssl) { const x = V.subeler.find(y => y.id === +ssl.dataset.id); if (!x || !window.confirm(`${x.sube} · ${x.ulke}: şube ve ${x.yilSayisi} yıl satırı, ${x.gonderimSayisi} gönderim kaydı silinsin mi?`)) return; const j = await post('/IhracatFinans/SubeSil', { id: x.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } if (subeSecili === x.id) subeSecili = 0; veri(j.veri); mesaj('success', esc(j.message)); return; }
    const sgd = e.target.closest('.ib-sube-gonderim-durum'); if (sgd) { const s = V.subeler.find(x => x.id === +sgd.dataset.sube); gonderimDurumAc(s.gonderimler.find(g => g.id === +sgd.dataset.id), s); return; }
    const skp = e.target.closest('.ib-sube-kapat'); if (skp) { subeDetay(0); $$('#ibTablo-sube tr.secili').forEach(r => r.classList.remove('secili')); return; }
    const sgt = e.target.closest('.ib-sube-git'); if (sgt) { e.preventDefault(); subeSecili = +sgt.dataset.id; sekme('sube'); subeDetay(subeSecili); return; }
    // şube yıl kalemleri (12 Kas 2026)
    const skl = e.target.closest('.ib-sube-kalem'); if (skl) { e.preventDefault(); subeKalemAc(V.subeler.find(x => x.id === +skl.dataset.id), skl.dataset.yil, skl.dataset.alan); return; }
    const kek = e.target.closest('.ib-kalem-ekle'); if (kek) { const tr = kek.closest('tr'), v = k => (tr.querySelector(`[data-k="${k}"]`) || {}).value ?? ''; const tutarK = SAYI(v('tutar')); if (!tutarK) { mesaj('warning', 'Kalem tutarı girin.'); return; } await kalemYaz({ alan: v('alan'), tarih: v('tarih') || null, belgeNo: v('belgeNo'), aciklama: v('aciklama'), tutar: tutarK, not: v('not') }); return; }
    const kad = e.target.closest('.ib-kalem-aday'); if (kad) { const alanSel = $('#ibMGovde [data-k="alan"]'); await kalemYaz({ alan: alanSel ? alanSel.value : kalemDurum.alan, tarih: kad.dataset.tarih || null, belgeNo: kad.dataset.belge, aciklama: kad.dataset.aciklama, tutar: SAYI(kad.dataset.tutar), kaynak: kad.dataset.kaynak }); return; }
    const ksl = e.target.closest('.ib-kalem-sil'); if (ksl) { if (!window.confirm('Kalem silinsin mi?')) return; const j = await post('/IhracatFinans/SubeKalemSil', { subeId: kalemDurum.id, id: +ksl.dataset.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); subeKalemAc(V.subeler.find(x => x.id === kalemDurum.id), kalemDurum.yil, kalemDurum.alan); return; }
    const kes = e.target.closest('.ib-kalem-esitle'); if (kes) { if (!window.confirm(`${kalemDurum.yil} yılı ${ALAN_AD[kalemDurum.alan]} tutarı kalem toplamıyla değiştirilsin mi? (eski değer yıl notuna yazılır)`)) return; const j = await post('/IhracatFinans/SubeYilEsitle', { subeId: kalemDurum.id, yil: kalemDurum.yil, alan: kalemDurum.alan }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); subeKalemAc(V.subeler.find(x => x.id === kalemDurum.id), kalemDurum.yil, kalemDurum.alan); return; }
    const sdt = e.target.closest('.ib-sube-detay') || (e.target.closest('.ib-sube-satir') && !e.target.closest('button, a') ? e.target.closest('.ib-sube-satir') : null); if (sdt) { subeDetay(+sdt.dataset.id); return; }
    // banka tahsilatı · eşleştirme · İBKB belgesi · satıştan dosya (11 Kas 2026)
    const td = e.target.closest('.ib-tahsilat-duzenle'); if (td) { tahsilatAc(V.tahsilatlar.find(x => x.id === +td.dataset.id)); return; }
    const tsl = e.target.closest('.ib-tahsilat-sil'); if (tsl) { const x = V.tahsilatlar.find(y => y.id === +tsl.dataset.id); if (!x || !window.confirm(`${x.banka} · ${tutar(x.tutar, x.doviz)} · ${x.gonderen}: banka tahsilatı silinsin mi?`)) return; const j = await post('/IhracatFinans/TahsilatSil', { id: x.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); return; }
    const es = e.target.closest('.ib-eslestir'); if (es) { eslestirAc(V.tahsilatlar.find(x => x.id === +es.dataset.id)); return; }
    const ek = e.target.closest('.ib-eslesme-kaldir'); if (ek) { const x = V.tahsilatlar.find(y => y.id === +ek.dataset.id); if (!x || !window.confirm(`${x.dosyaNo} dosyasıyla eşleşme kaldırılsın mı? DAB satırı silinir, varsa İBKB belgesi iptal olur.`)) return; const j = await post('/IhracatFinans/EslesmeKaldir', { id: x.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); return; }
    const bd = e.target.closest('.ib-belge-durum'); if (bd) { belgeAc(V.belgeler.find(x => x.id === +bd.dataset.id)); return; }
    const bs = e.target.closest('.ib-belge-sil'); if (bs) { const x = V.belgeler.find(y => y.id === +bs.dataset.id); if (!x || !window.confirm(`${x.no} iptal belgesi silinsin mi?`)) return; const j = await post('/IhracatFinans/BelgeSil', { id: x.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); return; }
    const sd = e.target.closest('.ib-satis-dosya'); if (sd) { const s = V.satislar.find(x => x.id === +sd.dataset.id); if (!s) return; formAc(null, { sirket: V.sirketler.includes(s.sirket) ? s.sirket : V.sirketler[0], yil: +(s.faturaTarihi || s.tarih || '').slice(0, 4) || new Date().getFullYear(), dosyaNo: '', musteriKodu: s.cariKodu || '', musteri: s.cari || '', ulke: s.ulke || '', faturaNo: s.faturaNo || '', siparisNo: s.no || '', baslangic: s.yukleme || s.faturaTarihi || '', tutar: s.tutar, doviz: s.doviz === 'TRY' ? 'USD' : s.doviz, aciklama: `${s.no || ''} ${s.faturaNo || ''} · Yurtdışı Satış Siparişi'nden açıldı`.trim() }); return; }
    const nt = e.target.closest('.ib2-not'); if (nt) { nt.closest('td').classList.toggle('acik'); return; }
  };
  kok.addEventListener('click', tikla);
  $('#ibModal').addEventListener('click', e => { if (!kok.contains(e.target)) tikla(e); });   // modal sayfa kabuğunca #ibkb dışına taşınıyor; pencere içindeki bağlantılar (görev aç, yanıt) de aynı yöneticiye gider

  // ---------- başlangıç
  sekme(kok.dataset.sekme || 'ozet'); yukle();
})();
