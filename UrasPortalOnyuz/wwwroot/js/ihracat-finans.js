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
  }
  function secenekler(sel, degerler, mevcut) { const v = mevcut ?? sel.value; sel.innerHTML = '<option value="">Tümü</option>' + degerler.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); sel.value = degerler.map(String).includes(String(v)) ? v : ''; }
  function filtreDoldur() {
    secenekler($('#ibS'), V.sirketler, F.sirket); F.sirket = $('#ibS').value;
    secenekler($('#ibY'), V.yillar.map(String), F.yil); F.yil = $('#ibY').value;
    // durum süzgeci sekmeye göre: dosya sekmelerinde DAB / İBKB durumu, Banka Tahsilatları'nda tahsilat durumu, İBKB Belgeleri'nde belge durumu, İhracat Satışları'nda sipariş durumu
    const p = aktifSekme(), DE = p === 'tahsilat' ? ['Kaynak', V.tahsilatKaynaklari || []] : p === 'tamamlanan' ? ['Tahsilat Durumu', V.tahsilatDurumlari.filter(x => x !== 'Eşleşmedi')] : p === 'belgeler' ? ['Belge Durumu', V.belgeDurumlari] : p === 'satis' ? ['Sipariş Durumu', ['Yüklendi', 'Faturalandı']] : ['DAB / İBKB Durumu', V.dabDurumlari];
    $('#ibDEtiket').textContent = DE[0]; secenekler($('#ibD'), DE[1], F.dab); F.dab = $('#ibD').value;
    secenekler($('#ibT'), V.takipDurumlari, F.takip); F.takip = $('#ibT').value;
    secenekler($('#ibV'), [...V.dovizler, '?'], F.doviz); F.doviz = $('#ibV').value;
  }
  const liste = () => V.dosyalar.filter(d => (!F.sirket || d.sirket === F.sirket) && (!F.yil || String(d.yil) === F.yil) && (!F.dab || d.dabDurumu === F.dab) && (!F.takip || d.takip === F.takip) && (!F.doviz || (d.doviz || '?') === F.doviz) && (!kpi.takipOlmayan || !kpi.takipOlmayan.includes(d.takip))
    && (!F.ara || [d.dosyaNo, d.musteri, d.musteriKodu, d.beyannameNo, d.faturaNo, d.siparisNo, d.aciklama, d.dabBanka, d.dabNotu, d.not, d.ulke, ...d.dablar.map(p => p.banka + ' ' + p.no + ' ' + p.not)].join(' ').toLocaleLowerCase('tr-TR').includes(F.ara.toLocaleLowerCase('tr-TR'))));
  const gecerli = L => L.filter(d => d.dabDurumu !== 'İptal');
  const ara = (alanlar) => !F.ara || alanlar.join(' ').toLocaleLowerCase('tr-TR').includes(F.ara.toLocaleLowerCase('tr-TR'));
  const satisListe = () => V.satislar.filter(s => (!F.sirket || s.sirket === F.sirket) && (!F.yil || (s.faturaTarihi || s.tarih || '').slice(0, 4) === F.yil) && (!F.dab || s.durum === F.dab) && (!F.doviz || s.doviz === F.doviz) && ara([s.no, s.faturaNo, s.cari, s.cariKodu, s.ulke, s.dosyaNo, s.temsilci]));
  // banka tahsilatları iki sekmede (11 Kas 2026 "banka tahsilatı sekmesi çok uzun olur"): Açık = Eşleşmedi (durum süzgeci kaynak); Tamamlanan = Eşleşti / İBKB Düzenlendi (dosya + belge ile)
  const tahsilatTum = () => V.tahsilatlar.filter(t => (!F.sirket || t.sirket === F.sirket) && (!F.yil || (t.tarih || '').slice(0, 4) === F.yil) && (!F.doviz || t.doviz === F.doviz) && ara([t.banka, t.gonderen, t.musteriKodu, t.referans, t.aciklama, t.dosyaNo, t.beyannameNo, t.belgeNo]));
  const tahsilatListe = () => tahsilatTum().filter(t => t.durum === 'Eşleşmedi' && (!F.dab || t.kaynak === F.dab));
  const tamamlananListe = () => tahsilatTum().filter(t => t.durum !== 'Eşleşmedi' && (!F.dab || t.durum === F.dab));
  const belgeListe = () => V.belgeler.filter(b => (!F.sirket || b.sirket === F.sirket) && (!F.yil || (b.tarih || '').slice(0, 4) === F.yil) && (!F.dab || b.durum === F.dab) && (!F.doviz || b.doviz === F.doviz) && ara([b.no, b.banka, b.dosyaNo, b.beyannameNo, b.faturaNo, b.musteri, b.musteriKodu, b.bankaIbkbNo, b.tahsilatRef, b.not]));
  const tahsilatRz = d => rz(d || 'Eşleşmedi', d === 'İBKB Düzenlendi' ? 'ok' : d === 'Eşleşti' ? 'mavi' : 'bekle');
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
    $('#ibKapsam').textContent = `${L.length} / ${V.dosyalar.length} dosya · ${[...new Set(L.map(d => d.sirket))].length} şirket · ${acik.length} açık · ${kapandi.length} kapandı · ${muaf.length} muaf${kpi.takipOlmayan ? ' · kart süzgeci: ' + kpi.takipOlmayan.join(' / ') + ' hariç' : ''} · ${V.bugun ? tarih(V.bugun) : ''}`;
  }
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
    $('#ibAciklama-ozet').textContent = `Kaynak: ihracat departmanının İBKB / DAB listeleri (IMKB_LISTELERI.xlsx, 2025-URAS · 2026-URAS · 2026-SELVİ) + ekrandan girilen dosyalar. EŞİK: beyanname tutarının ABD doları karşılığı ${kisaUsd(V.esikUsd)} ve altındaysa DAB / İBKB durumu kendiliğinden "Gerek Yok" olur (TCMB İhracat Genelgesi md. 28/1-a: 15.000 $ veya eşitine kadar noksanlığı olan ihracat hesapları DAB aranmadan bankaca kapatılır; genelgede ayrıca "İBKB düzenlenmez" diye tutar maddesi yoktur — ihracat departmanı uygulaması). Ek-2 ülkesine ihracatta bedel getirme zorunlu değil (oran 0), Ek-3 ülkesinde %50. Kur: ${Object.entries(V.usdKur).filter(([k]) => k !== 'USD').map(([k, v]) => '1 ' + k + ' = ' + n0(v).toLocaleString('tr-TR', { maximumFractionDigits: 4 }) + ' $').join(', ')}. Gereken bedel = beyanname tutarı × ülke gerekliliği (%); Gerek Yok ve İptal dosyalarda 0. Getirilen = bankadan gelen DAB / İBKB satırlarının toplamı (DAB "Yapıldı" dosyada gerekenin tamamı); Kalan = gereken − getirilen. Vade = dosya başlangıcından ${V.vadeGun} gün (ihracat bedelinin yurda getirilme süresi); 30 gün içinde dolan vade "Vade Yaklaşıyor", geçen "Vadesi Geçti". Dövizler ayrı toplanır, kur uygulanmaz.`;
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
    $('#ibAciklama-liste').textContent = 'Excel listesinin tüm sütunları: dosya no, açıklama (notta), müşteri kodu ve adı, başlangıç / kapanış, GÇB no, beyanname tutarı ve dövizi, ülke gerekliliği, DAB kullanımı (bankanın bildirdiği durum ve notu), bağlanan DAB tutarları. GERÇEK İHRACAT BEDELİ: proforma / fatura ile müşteriyle anlaşılan tutar beyannameden farklıysa (ör. beyanname 100.000 $, proforma 50.000 $ müşteri talebiyle) dosya kaydına girilir; fark + nedeni sütunda, gereken ve kalan bedel hem beyannameye hem gerçek bedele göre ayrı gösterilir, beyanname fazlası (hiç tahsil edilemeyecek kısım) kalan altında yazar. Gerçek bedel tamamen gelmişse takip durumu "Bedel Farkı" olur (beyanname düzeltme / terkin konusu). "Döviz Belirsiz" = Excel satırında döviz yazılmamış; kayıt açılıp döviz seçilmeli.';
  }

  // ---------- Müşteri Bazlı
  function cizMusteri(L) {
    const G = gecerli(L);
    const m = new Map(); G.forEach(d => { const k = d.musteriKodu || d.musteri.toLocaleUpperCase('tr-TR'); if (!m.has(k)) m.set(k, { kod: d.musteriKodu, ad: d.musteri, l: [] }); m.get(k).l.push(d); });
    const M = [...m.values()].sort((a, b) => b.l.length - a.l.length || a.ad.localeCompare(b.ad, 'tr'));
    const bas = ['Müşteri', 'Müşteri Kodu', 'Şirket', 'Dosya', 'Yapıldı', 'Gerek Yok', 'Kısmi', 'Bekliyor', 'Vadesi Geçti', 'Beyanname Tutarı', 'Gerçek İhracat Bedeli', 'Beyanname − Gerçek Fark', 'Farklı Dosya', 'Gereken (Beyanname)', 'Gereken (Gerçek)', 'Getirilen', 'Kalan (Beyanname)', 'Kalan (Gerçek)', 'Beyanname Fazlası', 'İlk Dosya', 'Son Dosya', 'Bankalar'];
    const say = (l, f) => l.filter(f).length;
    const farkHucre = l => { const f = l.filter(d => d.bedelFarki); return f.length ? dovizH(f, 'bedelFarki') + `<small>${[...new Set(f.map(d => d.farkNedeni).filter(Boolean))].join(', ')}</small>` : bos; };
    const rows = M.map(x => { const ts = x.l.map(d => d.baslangic).filter(Boolean).sort(), fk = x.l.filter(d => d.bedelFarki); return `<tr class="tik ib-musteri-git${fk.length ? ' yakin' : ''}" data-ad="${esc(x.ad)}" title="${fk.length ? fk.length + ' dosyada beyanname ile gerçek bedel farklı' : ''}"><td class="ad">${esc(x.ad || '—')}</td><td>${esc(x.kod || '—')}</td><td>${[...new Set(x.l.map(d => d.sirket))].join(' · ')}</td><td class="sayac">${x.l.length}</td><td class="sayac">${say(x.l, d => d.dabDurumu === 'Yapıldı' || d.dabDurumu === 'Peşin Yapıldı')}</td><td class="sayac">${say(x.l, d => d.dabDurumu === 'Gerek Yok')}</td><td class="sayac">${say(x.l, d => d.dabDurumu === 'Kısmi')}</td><td class="sayac">${say(x.l, d => d.dabDurumu === 'Bekliyor')}</td><td class="sayac">${say(x.l, d => d.takip === 'Vadesi Geçti') || bos}</td><td class="para">${dovizH(x.l, 'tutar')}</td><td class="para">${dovizH(x.l, 'gercekBedelEtkin')}</td><td>${farkHucre(x.l)}</td><td class="sayac">${fk.length ? `<span class="ib2-rz mor">${fk.length}</span>` : bos}</td><td class="para">${dovizH(x.l, 'gereken')}</td><td class="para">${dovizH(x.l, 'gerekenGercek')}</td><td class="para">${dovizH(x.l, 'getirilen')}</td><td class="para genel">${dovizH(x.l, 'kalan')}</td><td class="para">${dovizH(x.l, 'kalanGercek')}</td><td class="para">${fk.length ? dovizH(x.l, 'beyannameFazlasi') : bos}</td><td>${tarih(ts[0])}</td><td>${tarih(ts[ts.length - 1])}</td><td>${[...new Set(x.l.flatMap(d => [d.dabBanka, ...d.dablar.map(p => p.banka)]).filter(Boolean))].join(', ') || bos}</td></tr>`; });
    const farkliG = G.filter(d => d.bedelFarki);
    const dip = `<tr><td colspan="3">Toplam · ${M.length} müşteri · ${[...new Set(farkliG.map(d => d.musteriKodu || d.musteri))].length} müşteride bedel farkı</td><td class="sayac">${G.length}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Yapıldı' || d.dabDurumu === 'Peşin Yapıldı')}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Gerek Yok')}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Kısmi')}</td><td class="sayac">${say(G, d => d.dabDurumu === 'Bekliyor')}</td><td class="sayac">${say(G, d => d.takip === 'Vadesi Geçti')}</td><td class="para">${dovizH(G, 'tutar')}</td><td class="para">${dovizH(G, 'gercekBedelEtkin')}</td><td>${farkliG.length ? dovizH(farkliG, 'bedelFarki') : bos}</td><td class="sayac">${farkliG.length || bos}</td><td class="para">${dovizH(G, 'gereken')}</td><td class="para">${dovizH(G, 'gerekenGercek')}</td><td class="para">${dovizH(G, 'getirilen')}</td><td class="para genel">${dovizH(G, 'kalan')}</td><td class="para">${dovizH(G, 'kalanGercek')}</td><td class="para">${farkliG.length ? dovizH(G, 'beyannameFazlasi') : bos}</td><td colspan="3"></td></tr>`;
    tablo('musteri', bas, rows, dip, 'Filtreye uyan müşteri yok.');
    $('#ibNot-musteri').textContent = `${M.length} müşteri · dosya sayısına göre · satıra tıklayınca Dosya Listesi o müşteriye daralır`;
    $('#ibAciklama-musteri').textContent = 'Müşteri kodu Excel listesinden (URAS listelerinde 120.02.xxx cari kodu; SELVİ listesinde kod yok, aynı adlı müşteri URAS listesindeki kodla eşlendi). Tutarlar dövize göre ayrı; kalan = açık dosyaların getirilmemiş bedeli. BEYANNAME ≠ GERÇEK BEDEL: müşteriyle anlaşılan proforma / fatura tutarı beyannameden düşükse (müşteri talebi, iskonto, kısmi sevkiyat …) satır sarı; fark, farklı dosya sayısı, gerçek bedele göre gereken / kalan ve beyanname fazlası (hiç gelmeyecek kısım) ayrı sütunda. Satıra tıklayınca Beyanname / İBKB Dosyaları o müşteriye daralır.';
  }

  // ---------- Vade Takibi
  function cizVade(L) {
    const A = gecerli(L).filter(d => d.takip === 'Açık' || d.takip === 'Vade Yaklaşıyor' || d.takip === 'Vadesi Geçti' || d.takip === 'Kısmi' || d.takip === 'Bedel Farkı').sort((a, b) => (a.vade || '9999').localeCompare(b.vade || '9999'));
    const bas = ['Takip', 'Kalan Gün', 'Vade (Başlangıç + 180)', 'Başlangıç', 'Şirket', 'Dosya No', 'Müşteri', 'GÇB No', 'Beyanname Tutarı', 'Gerçek İhracat Bedeli', 'Fark', 'Gereken (Beyanname)', 'Gereken (Gerçek)', 'Getirilen', 'Kalan (Beyanname)', 'Kalan (Gerçek)', 'DAB / İBKB Durumu', 'Banka', 'Not'];
    const rows = A.map(d => `<tr class="${sinif(d)}"><td>${takipRz(d.takip)}</td><td>${d.kalanGun === null || d.kalanGun === undefined ? '<span class="ib2-rz gri" title="Başlangıç tarihi yok">Tarih Yok</span>' : `<b style="color:${d.kalanGun < 0 ? '#b91c1c' : d.kalanGun <= 30 ? '#b45309' : '#166534'}">${d.kalanGun}</b>`}</td><td>${tarih(d.vade)}</td><td>${tarih(d.baslangic)}</td><td class="ad">${esc(d.sirket)}</td><td class="ad">${dosyaLink(d)}</td><td class="metin">${musteriH(d)}</td><td>${esc(d.beyannameNo || '—')}</td><td class="para">${tutar(d.tutar, d.doviz)}</td><td class="para">${d.gercekBedel ? tutar(d.gercekBedel, d.doviz) : '<small>= beyanname</small>'}</td><td>${farkH(d)}</td><td class="para">${tutar(d.gereken, d.doviz)}</td><td class="para">${d.bedelFarki ? tutar(d.gerekenGercek, d.doviz) : bos}</td><td class="para">${d.getirilen ? tutar(d.getirilen, d.doviz) : bos}</td><td class="para genel">${tutar(d.kalan, d.doviz)}${d.beyannameFazlasi ? `<small>beyanname fazlası ${tutar(d.beyannameFazlasi, d.doviz)}</small>` : ''}</td><td class="para">${d.bedelFarki ? (d.kalanGercek ? tutar(d.kalanGercek, d.doviz) : '<span class="ib2-rz ok">Gerçek Bedel Tamam</span>') : bos}</td><td>${dabRz(d.dabDurumu)}</td><td>${esc(d.dabBanka || '—')}</td>${notH([d.farkNedeni ? 'Fark: ' + d.farkNedeni : '', d.dabNotu, d.not].filter(Boolean).join(' · '))}</tr>`);
    const dip = `<tr><td colspan="8">Toplam · ${A.length} açık dosya · ${A.filter(d => d.takip === 'Vadesi Geçti').length} vadesi geçti · ${A.filter(d => d.takip === 'Vade Yaklaşıyor').length} 30 gün içinde · ${A.filter(d => d.takip === 'Bedel Farkı').length} bedel farkı · ${A.filter(d => !d.baslangic).length} tarihsiz</td><td class="para">${dovizH(A, 'tutar')}</td><td class="para">${dovizH(A, 'gercekBedelEtkin')}</td><td>${A.some(d => d.bedelFarki) ? dovizH(A.filter(d => d.bedelFarki), 'bedelFarki') : bos}</td><td class="para">${dovizH(A, 'gereken')}</td><td class="para">${dovizH(A, 'gerekenGercek')}</td><td class="para">${dovizH(A, 'getirilen')}</td><td class="para genel">${dovizH(A, 'kalan')}</td><td class="para">${dovizH(A, 'kalanGercek')}</td><td colspan="3"></td></tr>`;
    tablo('vade', bas, rows, dip, 'Açık dosya yok: tüm dosyalar kapandı ya da muaf.');
    $('#ibNot-vade').textContent = `${A.length} açık dosya · vadeye göre (en yakın üstte)`;
    $('#ibAciklama-vade').textContent = `İhracat bedelinin yurda getirilme süresi fiili ihraçtan itibaren ${V.vadeGun} gündür; burada dosya başlangıç tarihi esas alınır. Gerçek bedel beyannameden düşükse kalan iki ayrı sütunda izlenir: Kalan (Beyanname) yasal yükümlülük, Kalan (Gerçek) müşteriden fiilen beklenen; gerçek bedel tamamlanıp beyanname fazlası kaldıysa takip "Bedel Farkı" olur. Vadesi geçen dosyalarda banka DAB / İBKB düzenlenmemişse ihracat ve finans birlikte takip etmeli (DAB yapıldıysa dosya kaydında durum güncellenir, satır listeden düşer). Başlangıç tarihi olmayan Excel satırları "Tarih Yok" rozetiyle en altta.`;
  }

  // ---------- DAB Bağlantıları
  function cizDab(L) {
    const S = []; L.forEach(d => d.dablar.forEach((p, i) => S.push({ d, p, i })));
    const bas = ['Şirket', 'Dosya No', 'Müşteri', 'GÇB No', 'Beyanname Tutarı', 'DAB Tutarı', 'DAB Dövizi', 'DAB Tarihi', 'Banka', 'DAB / İBKB No', 'Dosya Durumu', 'Not'];
    const rows = S.map(({ d, p }) => `<tr class="${sinif(d)}"><td class="ad">${esc(d.sirket)}</td><td class="ad">${dosyaLink(d)}</td><td class="metin">${musteriH(d)}</td><td>${esc(d.beyannameNo || '—')}</td><td class="para">${tutar(d.tutar, d.doviz)}</td><td class="para genel">${tutar(p.tutar, p.doviz)}${p.doviz !== d.doviz ? '<small>beyanname dövizinden farklı</small>' : ''}</td><td>${dovizRz(p.doviz)}</td><td>${tarih(p.tarih)}</td><td>${esc(p.banka || d.dabBanka || '—')}</td><td>${esc(p.no || '—')}</td><td>${dabRz(d.dabDurumu)}</td>${notH(p.not)}</tr>`);
    const dip = `<tr><td colspan="5">Toplam · ${S.length} DAB satırı · ${[...new Set(S.map(x => x.d.id))].length} dosya</td><td class="para genel">${dovizH(S.map(x => ({ doviz: x.p.doviz, t: x.p.tutar })), 't')}</td><td colspan="6"></td></tr>`;
    tablo('dab', bas, rows, dip, 'Bağlı DAB satırı yok. Dosya kaydında "Bağlanan DAB / İBKB" bölümünden tutar, döviz, banka ve tarih girin.');
    $('#ibNot-dab').textContent = `${S.length} DAB satırı · Excel'deki "… BAĞLANDI" notlarından ve ekrandan girilenlerden`;
    $('#ibAciklama-dab').textContent = 'Bankadan gelen her DAB / İBKB satırı dosyasına bağlıdır; beyanname dövizinden farklı dövizde gelen DAB (ör. USD beyannameye EUR DAB) satırda işaretlenir, getirilen hesabına kur olmadan girmez. Excel listesindeki "21,000 USD - 2,783 USD VE 6,259.57 EUR BAĞLANDI" gibi notlar satırlara çözüldü; banka ve tarih Excel\'de olmadığından boş.';
  }

  // ---------- İhracat Satışları (Yurtdışı Satış Siparişi'nden canlı; 11 Kas 2026 "satışlar da düşsün")
  function cizSatis() {
    const S = satisListe();
    const bas = ['Şirket', 'Sipariş No', 'Durum', 'Fatura No', 'Fatura Tarihi', 'Yükleme', 'Müşteri', 'Ülke', 'Temsilci', 'Ödeme Şekli', 'Teslim', 'Tutar', 'Döviz', 'TL Karşılığı', 'İBKB Dosyası', 'Dosya Takip', 'Eşik', 'İşlem'];
    const rows = S.map(s => { const d = s.dosyaId ? V.dosyalar.find(x => x.id === s.dosyaId) : null; return `<tr class="${d ? sinif(d) : 'eslesmedi'}"><td class="ad">${esc(s.sirket)}</td><td class="ad">${esc(s.no || '—')}</td><td>${rz(s.durum, s.durum === 'Faturalandı' ? 'ok' : 'mavi')}</td><td>${esc(s.faturaNo || '—')}</td><td>${tarih(s.faturaTarihi)}</td><td>${tarih(s.yukleme)}</td><td class="metin">${esc(s.cari)}<small>${esc(s.cariKodu || '')}</small></td><td>${esc(s.ulke || '—')}</td><td>${esc(s.temsilci || '—')}</td><td>${esc(s.odemeSekli || '—')}</td><td>${esc(s.teslimSekli || '—')}</td><td class="para">${tutar(s.tutar, s.doviz)}</td><td>${dovizRz(s.doviz)}</td><td class="para">${tutar(s.tutarTl, 'TRY')}</td><td>${d ? `<a href="#" class="ib-duzenle" data-id="${d.id}">${esc(d.dosyaNo)}</a><small>${esc(d.beyannameNo || 'GÇB yok')}</small>` : '<span class="ib2-rz bekle" title="Bu fatura için beyanname / İBKB dosyası açılmamış">Dosya Yok</span>'}</td><td>${d ? takipRz(d.takip) : bos}</td><td>${s.esikAlti ? '<span class="ib2-rz gri" title="ABD doları karşılığı eşik altı: DAB / İBKB gerekmez">Eşik Altı</span>' : '<span class="ib2-rz teal">Bedel Getirilecek</span>'}</td><td>${d ? '' : `<button type="button" class="btn btn-light border ib-satis-dosya" data-id="${s.id}" title="Bu satıştan beyanname / İBKB dosyası aç (bilgiler dolu gelir)"><i class="fas fa-folder-plus"></i> Dosya Aç</button>`}</td></tr>`; });
    const dip = `<tr><td colspan="11">Toplam · ${S.length} ihracat satışı · ${S.filter(s => s.dosyaId).length} dosyalı · ${S.filter(s => !s.dosyaId).length} dosyasız · ${S.filter(s => s.esikAlti).length} eşik altı</td><td class="para">${dovizH(S, 'tutar')}</td><td></td><td class="para">${tutar(S.reduce((t, s) => t + n0(s.tutarTl), 0), 'TRY')}</td><td colspan="4"></td></tr>`;
    tablo('satis', bas, rows, dip, 'Filtreye uyan yüklenmiş / faturalanmış ihracat siparişi yok.');
    $('#ibNot-satis').textContent = `${S.length} satış · Yurtdışı Satış Siparişi'nden (yüklenmiş ve faturalanmış) canlı · "Dosya Aç" bilgileri doldurur`;
    $('#ibAciklama-satis').textContent = 'İhracat satışları Yurtdışı Satış Siparişi ekranından canlı gelir (ERP bağlanınca fatura kayıtları). Her satış için beyanname / İBKB dosyası fatura no, sipariş no ya da müşteri + tutar eşleşmesiyle bulunur; dosyası olmayan faturada "Dosya Aç" ile müşteri, tutar, döviz, fatura ve yükleme tarihi dolu bir İBKB dosyası açılır. Eşik: ABD doları karşılığı 15.000 $ ve altı satışta DAB / İBKB gerekmez.';
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
    $('#ibAciklama-tahsilat').textContent = 'Eşleştirme: tahsilat seçilen ihracat dosyasına DAB satırı olarak yazılır; dosyanın gereken bedeli karşılandıysa DAB / İBKB durumu "Yapıldı", kısmen karşılandıysa "Kısmi" olur. İBKB belgesi istenirse İBKB-YYYY-0001 numaralı başvuru formu üretilir ve İBKB Belgeleri sekmesine düşer; form bankaya verilecek biçimde basılır. Durum süzgeci burada tahsilatın kaynağını (Banka (Otomatik) / Elle / Excel) süzer.';
  }
  // ---------- Tamamlanan Tahsilatlar: eşleşmiş tahsilat + dosya + İBKB belgesi tek satırda
  function cizTamamlanan() {
    const T = tamamlananListe();
    const bas = ['Durum', 'Şirket', 'Dosya No', 'GÇB No', 'Müşteri', 'Beyanname Tutarı', 'Dosya DAB Durumu', 'Banka', 'Tahsilat Tarihi', 'Tahsilat Tutarı', 'Referans', 'İBKB Belgesi', 'Belge Durumu', 'Banka İBKB No', 'Eşleştirme', 'Kaynak', 'İşlem'];
    const rows = T.map(t => { const d = V.dosyalar.find(x => x.id === t.dosyaId), b = V.belgeler.find(x => x.id === t.ibkbBelgeId); return `<tr class="${t.durum === 'İBKB Düzenlendi' ? 'kapandi' : ''}"><td>${tahsilatRz(t.durum)}</td><td class="ad">${esc(t.sirket)}</td><td class="ad">${d ? `<a href="#" class="ib-duzenle" data-id="${d.id}">${esc(d.dosyaNo)}</a>` : esc(t.dosyaNo || '—')}</td><td>${esc(t.beyannameNo || (d && d.beyannameNo) || '—')}</td><td class="metin">${d ? musteriH(d) : esc(t.gonderen || '—')}</td><td class="para">${d ? tutar(d.tutar, d.doviz) : bos}</td><td>${d ? dabRz(d.dabDurumu) + (d.kalan ? `<small>kalan ${tutar(d.kalan, d.doviz)}</small>` : '') : bos}</td><td>${esc(t.banka || '—')}</td><td>${tarih(t.tarih)}</td><td class="para genel">${tutar(t.tutar, t.doviz)}</td><td>${esc(t.referans || '—')}</td><td>${b ? `<a href="/IhracatFinans/Belge/${b.id}" target="_blank" rel="noopener" title="Bankaya verilecek formu aç">${esc(b.no)}</a>` : '<span class="ib2-rz gri" title="Eşleştirmede İBKB istenmedi">Belge Yok</span>'}</td><td>${b ? belgeRz(b.durum) : bos}</td><td>${b && b.bankaIbkbNo ? esc(b.bankaIbkbNo) + `<small>${tarih(b.bankaTarihi)}</small>` : bos}</td><td>${t.eslestirme ? tarih(t.eslestirme) + `<small>${esc(t.eslestiren || '')}</small>` : bos}</td><td>${rz(t.kaynak, t.kaynak === 'Banka (Otomatik)' ? 'teal' : 'gri')}</td><td>${b ? `<a class="btn btn-light border" href="/IhracatFinans/Belge/${b.id}" target="_blank" rel="noopener" title="Yazdır / PDF"><i class="fas fa-print"></i></a> <button type="button" class="btn btn-light border ib-belge-durum" data-id="${b.id}" title="Belge durumu · banka İBKB no"><i class="fas fa-pen"></i></button> ` : ''}<button type="button" class="btn btn-light border ib-eslesme-kaldir" data-id="${t.id}" title="Eşleşmeyi kaldır (DAB satırı silinir, belge iptal olur; tahsilat Açık sekmesine döner)"><i class="fas fa-link-slash"></i></button></td></tr>`; });
    const dip = `<tr><td colspan="5">Toplam · ${T.length} tamamlanan tahsilat · ${T.filter(t => t.durum === 'İBKB Düzenlendi').length} İBKB düzenlendi · ${T.filter(t => t.durum === 'Eşleşti').length} yalnız eşleşti · ${[...new Set(T.map(t => t.dosyaId))].length} dosya</td><td colspan="4"></td><td class="para genel">${dovizH(T, 'tutar')}</td><td colspan="7"></td></tr>`;
    tablo('tamamlanan', bas, rows, dip, 'Tamamlanan tahsilat yok. Banka Tahsilatları (Açık) sekmesinde "Eşleştir" ile bağlanan tahsilatlar burada listelenir.');
    $('#ibNot-tamamlanan').textContent = `${T.length} tahsilat · ihracat dosyası ve İBKB belgesiyle birlikte · tarihe göre`;
    $('#ibAciklama-tamamlanan').textContent = 'Eşleşmiş (dosyasına bağlanmış) ve İBKB belgesi düzenlenmiş tahsilatlar: tek satırda ihracat dosyası (GÇB, müşteri, beyanname tutarı, DAB durumu ve kalan), bankadan gelen bedel ve İBKB belgesinin durumu. Banka İBKB numarası gelince belge durumu buradan ya da İBKB Belgeleri sekmesinden "Banka Onayladı" yapılır. Eşleşme kaldırılırsa tahsilat Açık sekmesine döner.';
  }
  // ---------- İBKB Belgeleri (bankaya verilecek form)
  function cizBelgeler() {
    const B = belgeListe();
    const bas = ['Belge No', 'Tarih', 'Durum', 'Şirket', 'Banka', 'Dosya No', 'GÇB No', 'Fatura No', 'Müşteri', 'Ülke', 'Beyanname Tutarı', 'Tahsilat Tutarı', 'Tahsilat Tarihi', 'Referans', 'Banka İBKB No', 'Banka Tarihi', 'Not', 'Hazırlayan', 'İşlem'];
    const rows = B.map(b => `<tr class="${b.durum === 'Banka Onayladı' ? 'kapandi' : b.durum === 'İptal' ? 'soluk' : b.durum === 'Hazırlandı' ? 'yakin' : ''}"><td class="ad"><a href="/IhracatFinans/Belge/${b.id}" target="_blank" rel="noopener" title="Formu aç / yazdır">${esc(b.no)}</a></td><td>${tarih(b.tarih)}</td><td>${belgeRz(b.durum)}</td><td class="ad">${esc(b.sirket)}</td><td>${esc(b.banka || '—')}</td><td class="ad">${b.dosyaId ? `<a href="#" class="ib-duzenle" data-id="${b.dosyaId}">${esc(b.dosyaNo)}</a>` : esc(b.dosyaNo || '—')}</td><td>${esc(b.beyannameNo || '—')}</td><td>${esc(b.faturaNo || '—')}</td><td class="metin">${esc(b.musteri || '—')}<small>${esc(b.musteriKodu || '')}</small></td><td>${esc(b.ulke || '—')}</td><td class="para">${tutar(b.beyannameTutar, b.beyannameDoviz)}</td><td class="para genel">${tutar(b.tutar, b.doviz)}</td><td>${tarih(b.tahsilatTarihi)}</td><td>${esc(b.tahsilatRef || '—')}</td><td>${esc(b.bankaIbkbNo || '—')}</td><td>${tarih(b.bankaTarihi)}</td>${notH(b.not)}<td>${esc(b.olusturan || '—')}<small>${esc(b.olusturma || '')}</small></td><td><a class="btn btn-light border" href="/IhracatFinans/Belge/${b.id}" target="_blank" rel="noopener" title="Yazdır / PDF"><i class="fas fa-print"></i></a> <button type="button" class="btn btn-light border ib-belge-durum" data-id="${b.id}" title="Durum · banka İBKB no"><i class="fas fa-pen"></i></button>${b.durum === 'İptal' ? ` <button type="button" class="btn btn-light border text-danger ib-belge-sil" data-id="${b.id}" title="İptal belgeyi sil"><i class="fas fa-trash"></i></button>` : ''}</td></tr>`);
    const dip = `<tr><td colspan="11">Toplam · ${B.length} belge · ${B.filter(b => b.durum === 'Hazırlandı').length} hazırlandı · ${B.filter(b => b.durum === 'Bankaya Gönderildi').length} bankada · ${B.filter(b => b.durum === 'Banka Onayladı').length} onaylandı · ${B.filter(b => b.durum === 'İptal').length} iptal</td><td class="para genel">${dovizH(B.filter(b => b.durum !== 'İptal'), 'tutar')}</td><td colspan="7"></td></tr>`;
    tablo('belgeler', bas, rows, dip, 'İBKB belgesi yok. Banka Tahsilatları sekmesinde tahsilatı dosyayla eşleştirirken "İBKB oluşturulsun mu?" sorusuna evet deyince belge burada oluşur.');
    $('#ibNot-belgeler').textContent = `${B.length} belge · belge numarasına tıklayınca bankaya verilecek form açılır (Yazdır → PDF)`;
    $('#ibAciklama-belgeler').textContent = 'Belge akışı: Hazırlandı → Bankaya Gönderildi → Banka Onayladı (banka İBKB numarası ve tarihi kaydedilir) · İptal. Form: ihracatçı, ihracat işlemi (dosya, GÇB, fatura, beyanname tutarı, alıcı, ülke), gelen bedel (tutar, valör, referans) ve banka kaydı bölümleri; 2018-32/48 Tebliğ ve TCMB İhracat Genelgesi atıfıyla.';
  }

  const CIZ = { ozet: cizOzet, satis: cizSatis, liste: cizListe, tahsilat: cizTahsilat, tamamlanan: cizTamamlanan, belgeler: cizBelgeler, musteri: cizMusteri, vade: cizVade, dab: cizDab };
  const OZEL = ['satis', 'tahsilat', 'tamamlanan', 'belgeler'];   // dosya listesi yerine kendi listesini çizen sekmeler (durum süzgeci de kendine göre)
  function ciz() { if (!V) return; const L = liste(); kpi(L); const p = aktifSekme(); if (OZEL.includes(p)) CIZ[p](); else (CIZ[p] || cizOzet)(L); }
  function sekme(p) { if (!CIZ[p]) p = 'ozet'; const onceki = aktifSekme(); $$('#ibSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#ibkb > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); if (V && onceki !== p) { const grup = x => OZEL.includes(x) ? x : 'dosya'; if (grup(onceki) !== grup(p)) { F.dab = ''; } filtreDoldur(); } ciz(); }
  $('#ibSekmeler').addEventListener('click', e => { const b = e.target.closest('button[data-p]'); if (b) sekme(b.dataset.p); });
  ['#ibS', '#ibY', '#ibD', '#ibT', '#ibV'].forEach(s => $(s).addEventListener('change', () => { F.sirket = $('#ibS').value; F.yil = $('#ibY').value; F.dab = $('#ibD').value; F.takip = $('#ibT').value; F.doviz = $('#ibV').value; ciz(); }));
  $('#ibAra').addEventListener('input', () => { F.ara = $('#ibAra').value; ciz(); });
  $('#ibTemizle').addEventListener('click', () => { F = { sirket: '', yil: '', dab: '', takip: '', doviz: '', ara: '' }; kpi.takipOlmayan = null; kpi.acik = ''; ['#ibS', '#ibY', '#ibD', '#ibT', '#ibV', '#ibAra'].forEach(s => $(s).value = ''); ciz(); });
  ['#ibD', '#ibT'].forEach(s => $(s).addEventListener('change', () => { kpi.takipOlmayan = null; }));   // elle süzgeç seçilince kartın gizli süzgeci kalkar
  $('#ibExcel').addEventListener('click', () => {
    const p = aktifSekme(), ad = ($(`#ibSekmeler button[data-p="${p}"]`) || {}).textContent?.trim() || p;
    if (!window.TabloExcel) { mesaj('danger', 'Excel bileşeni yüklenemedi; sayfayı yenileyin.'); return; }
    const sayfalar = p === 'ozet'
      ? [['ozetSirket', 'Şirket × Yıl'], ['ozetDoviz', 'Döviz Bazında Bedel'], ['ozetDurum', 'Durum Dağılımı'], ['ozetAy', 'Aylık Dosya Akışı']].map(([t, a], i) => Object.assign({ tablo: '#ibTablo-' + t, ad: a.slice(0, 31), baslik: 'İBKB Beyanname Takibi · Özet · ' + a, altBaslik: $('#ibKapsam').textContent.trim() }, i === 0 ? { kpi: '#ibKpi', not: '#ibAciklama-ozet' } : {}))
      : [{ tablo: '#ibTablo-' + p, ad: ad.slice(0, 31), baslik: 'İBKB Beyanname Takibi · ' + ad, altBaslik: $('#ibKapsam').textContent.trim(), kpi: '#ibKpi', not: '#ibAciklama-' + p }];
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
    return { id: duzenlenen ? duzenlenen.id : 0, sirket: g('sirket'), yil: +g('yil') || 0, dosyaNo: g('dosyaNo'), aciklama: g('aciklama'), musteriKodu: g('musteriKodu'), musteri: g('musteri'), ulke: g('ulke'), faturaNo: g('faturaNo'), siparisNo: g('siparisNo'), baslangic: g('baslangic') || null, kapanis: g('kapanis') || null, beyannameNo: g('beyannameNo'), doviz: g('doviz'), tutar: SAYI(g('tutar')), gercekBedel: SAYI(g('gercekBedel')), farkNedeni: g('farkNedeni'), ulkeOran: SAYI(g('ulkeOran')) || 100, dabDurumu: g('dabDurumu'), dabBanka: g('dabBanka'), dabKalan: SAYI(g('dabKalan')), dabNotu: g('dabNotu'), dablar, not: g('not') };
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
  kok.addEventListener('click', async e => {
    const d = e.target.closest('.ib-duzenle'); if (d) { e.preventDefault(); formAc(V.dosyalar.find(x => x.id === +d.dataset.id)); return; }
    const s = e.target.closest('.ib-sil'); if (s) { const x = V.dosyalar.find(y => y.id === +s.dataset.id); if (!x || !window.confirm(`${x.sirket} · ${x.dosyaNo} · ${x.musteri}: ihracat dosyası silinsin mi?`)) return; const j = await post('/IhracatFinans/Sil', { id: x.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); return; }
    const m = e.target.closest('.ib-musteri-git'); if (m) { F.ara = m.dataset.ad; $('#ibAra').value = F.ara; sekme('liste'); return; }
    const gt = e.target.closest('.ib-git'); if (gt) { e.preventDefault(); sekme(gt.dataset.p); return; }
    // banka tahsilatı · eşleştirme · İBKB belgesi · satıştan dosya (11 Kas 2026)
    const td = e.target.closest('.ib-tahsilat-duzenle'); if (td) { tahsilatAc(V.tahsilatlar.find(x => x.id === +td.dataset.id)); return; }
    const tsl = e.target.closest('.ib-tahsilat-sil'); if (tsl) { const x = V.tahsilatlar.find(y => y.id === +tsl.dataset.id); if (!x || !window.confirm(`${x.banka} · ${tutar(x.tutar, x.doviz)} · ${x.gonderen}: banka tahsilatı silinsin mi?`)) return; const j = await post('/IhracatFinans/TahsilatSil', { id: x.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); return; }
    const es = e.target.closest('.ib-eslestir'); if (es) { eslestirAc(V.tahsilatlar.find(x => x.id === +es.dataset.id)); return; }
    const ek = e.target.closest('.ib-eslesme-kaldir'); if (ek) { const x = V.tahsilatlar.find(y => y.id === +ek.dataset.id); if (!x || !window.confirm(`${x.dosyaNo} dosyasıyla eşleşme kaldırılsın mı? DAB satırı silinir, varsa İBKB belgesi iptal olur.`)) return; const j = await post('/IhracatFinans/EslesmeKaldir', { id: x.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); return; }
    const bd = e.target.closest('.ib-belge-durum'); if (bd) { belgeAc(V.belgeler.find(x => x.id === +bd.dataset.id)); return; }
    const bs = e.target.closest('.ib-belge-sil'); if (bs) { const x = V.belgeler.find(y => y.id === +bs.dataset.id); if (!x || !window.confirm(`${x.no} iptal belgesi silinsin mi?`)) return; const j = await post('/IhracatFinans/BelgeSil', { id: x.id }); if (!j.success) { mesaj('danger', esc(j.message)); return; } veri(j.veri); mesaj('success', esc(j.message)); return; }
    const sd = e.target.closest('.ib-satis-dosya'); if (sd) { const s = V.satislar.find(x => x.id === +sd.dataset.id); if (!s) return; formAc(null, { sirket: V.sirketler.includes(s.sirket) ? s.sirket : V.sirketler[0], yil: +(s.faturaTarihi || s.tarih || '').slice(0, 4) || new Date().getFullYear(), dosyaNo: '', musteriKodu: s.cariKodu || '', musteri: s.cari || '', ulke: s.ulke || '', faturaNo: s.faturaNo || '', siparisNo: s.no || '', baslangic: s.yukleme || s.faturaTarihi || '', tutar: s.tutar, doviz: s.doviz === 'TRY' ? 'USD' : s.doviz, aciklama: `${s.no || ''} ${s.faturaNo || ''} · Yurtdışı Satış Siparişi'nden açıldı`.trim() }); return; }
    const nt = e.target.closest('.ib2-not'); if (nt) { nt.closest('td').classList.toggle('acik'); return; }
  });

  // ---------- başlangıç
  sekme(kok.dataset.sekme || 'ozet'); yukle();
})();
