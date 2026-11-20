// ============================================================
// URAS PORTEKİZ RAPORU (/UrasPortekiz, 20 Kas 2026) — veri /UrasPortekiz/Veri (Data/UrasPortekizOrnek.cs; Excel'den, açıklamalar Türkçeye çevrilmiş).
//   Sekmeler: Özet · Satışlar · Müşteri Bazında · Ürün Bazında · Şirket Banka Hesabı · Tasarruf Hesabı · Murat Bey Özel Hesap · Kasa (EUR) · Ödeme Öngörüsü.
//   Süzgeçler çoklu seçim (ortak SuzgecCubuk; sekmeye göre ilgili olanlar görünür, seçenekler kademeli), her tabloda alt toplam (süzülen bütün satırlar),
//   başlığa tıklayınca sıralama, uzun listelerde sayfalama (250 satır), Excel = süzülmüş listenin tamamı. Tutarlar EUR; tutar ile € arası bölünmez boşluk.
//   Satır yapıları (sunucu dizileri): satış [belge, tarih, müşteri#, ürün#, ambalaj kg, adet, net kg, kg fiyatı, tutar, KDV oranı, KDV, KDV dahil, vade#, iptal];
//   hesap [tarih, belge, açıklama (TR), asıl metin (EN), giriş, çıkış, bakiye, tür]; öngörü [tarih, açıklama, asıl metin, giriş, çıkış, bakiye | null, tür, bakiye satırı mı].
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('up'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  const TR = 'tr-TR', NB = ' ', tire = '<span class="text-muted">—</span>';
  const n2 = v => (Number(v) || 0).toLocaleString(TR, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const para = v => Math.abs(Number(v) || 0) < 0.005 ? tire : `<span class="${v < 0 ? 'eksi' : ''}">${n2(v)}${NB}€</span>`;
  const paraM = v => n2(v) + NB + '€';   // düz metin (KPI, Excel)
  const kg = v => Math.abs(Number(v) || 0) < 0.0005 ? tire : (Number(v) || 0).toLocaleString(TR, { maximumFractionDigits: 2 }) + NB + 'kg';
  const adet = v => (Number(v) || 0).toLocaleString(TR, { maximumFractionDigits: 2 });
  const trh = s => s ? s.slice(8, 10) + '.' + s.slice(5, 7) + '.' + s.slice(0, 4) : '—';
  const yuzde = (a, b) => b ? '%' + NB + (a / b * 100).toLocaleString(TR, { minimumFractionDigits: 1, maximumFractionDigits: 1 }) : '—';
  const AYLAR = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
  const ayAd = a => AYLAR[+a - 1] || a, kucuk = s => String(s ?? '').toLocaleLowerCase(TR), topla = (l, f) => l.reduce((s, x) => s + (Number(f(x)) || 0), 0);
  const HESAP = { sirket: 'Şirket Banka Hesabı', tasarruf: 'Tasarruf Hesabı', ozel: 'Murat Bey Özel Hesap', kasa: 'Kasa (EUR)' };
  const SAYFA = 250;
  let V = null, SZ = null, aktif = kok.dataset.sekme || 'ozet';
  const durum = {};   // sekme → { k: sıralama sütunu, y: yön, s: sayfa }
  const D = p => durum[p] || (durum[p] = { k: null, y: 1, s: 0 });
  function mesaj(tip, m) { const s = $('#upSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.textContent = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => { s.style.display = 'none'; }, 9000); }

  // ---------- süzgeç ----------
  const ALANLAR = [{ k: 'yil', ad: 'Yıl' }, { k: 'ay', ad: 'Ay' }, { k: 'musteri', ad: 'Müşteri' }, { k: 'urun', ad: 'Ürün' }, { k: 'vade', ad: 'Vade' }, { k: 'tur', ad: 'Hareket Türü' }, { k: 'yon', ad: 'Yön' }];
  const GORUNEN = { ozet: ['yil'], gider: ['tur'], bakiye: ['musteri'], tahsilat: ['yil', 'ay', 'musteri'], aylik: ['yil', 'musteri', 'urun', 'vade'], satis: ['yil', 'ay', 'musteri', 'urun', 'vade'], musteri: ['yil', 'ay', 'urun', 'vade'], urun: ['yil', 'ay', 'musteri', 'vade'], sirket: ['yil', 'ay', 'tur', 'yon'], tasarruf: ['yil', 'ay', 'yon'], ozel: ['yil', 'ay', 'tur', 'yon'], kasa: ['yil', 'yon'], ongoru: ['tur', 'yon'] };
  const F = () => SZ.F, uy = (k, v) => !F()[k].length || F()[k].includes(String(v));
  const hesapMi = p => !!HESAP[p];
  /** satış satırları (haric: bu süzgeç uygulanmaz → kademeli seçenek listesi) */
  function satisSuz(haric, aramali = true) {
    const ara = aramali ? kucuk(F().ara) : '', u = k => k !== haric && GORUNEN[aktif === 'ozet' ? 'ozet' : aktif] && (aktif === 'ozet' ? k === 'yil' : (GORUNEN[aktif] || []).includes(k));
    return V.satis.filter(r => (!u('yil') || uy('yil', r[1].slice(0, 4) || '—')) && (!u('ay') || uy('ay', r[1].slice(5, 7) || '—')) && (!u('musteri') || uy('musteri', V.musteriler[r[2]])) && (!u('urun') || uy('urun', V.urunler[r[3]])) && (!u('vade') || uy('vade', V.vadeler[r[12]] || '—'))
      && (!ara || kucuk(r[0] + ' ' + V.musteriler[r[2]] + ' ' + V.urunler[r[3]] + ' ' + trh(r[1])).includes(ara)));
  }
  const yonu = r => r[4] > 0 && !(r[5] > 0) ? 'Giriş' : r[5] > 0 ? 'Çıkış' : '—';
  function hesapSuz(ad, haric) {
    const ara = kucuk(F().ara), g = GORUNEN[aktif === 'ozet' ? 'ozet' : ad] || [], u = k => k !== haric && g.includes(k);
    return V[ad].filter(r => (!u('yil') || uy('yil', r[0].slice(0, 4))) && (!u('ay') || uy('ay', r[0].slice(5, 7))) && (!u('tur') || uy('tur', r[7])) && (!u('yon') || uy('yon', yonu(r))) && (!ara || aktif === 'ozet' || kucuk(r[2] + ' ' + r[3] + ' ' + r[1] + ' ' + trh(r[0])).includes(ara)));
  }
  const ongoruYon = r => r[3] > 0 && !(r[4] > 0) ? 'Giriş' : r[4] > 0 ? 'Çıkış' : '—';
  function ongoruSuz(haric) { const ara = kucuk(F().ara), u = k => k !== haric; return V.ongoru.filter(r => (!u('tur') || uy('tur', r[6])) && (!u('yon') || uy('yon', ongoruYon(r))) && (!ara || kucuk(r[1] + ' ' + r[2]).includes(ara))); }
  /** süzgeç seçenekleri: görünen sekmenin verisinden, öbür süzgeçlere uyan satırlardan (sayılı) */
  function secenekler(k) {
    const say = new Map(), ekle = v => say.set(v, (say.get(v) || 0) + 1);
    if (aktif === 'gider') giderSatir(true).forEach(r => ekle(r[7]));
    else if (aktif === 'bakiye') bakiyeSatir(true).forEach(o => ekle(o.ad));
    else if (aktif === 'tahsilat') tahsilatSuz(k).forEach(t => ekle(k === 'yil' ? t.tarih.slice(0, 4) : k === 'ay' ? t.tarih.slice(5, 7) : t.ad));
    else if (aktif === 'ongoru') ongoruSuz(k).forEach(r => ekle(k === 'tur' ? r[6] : ongoruYon(r)));
    else if (hesapMi(aktif)) hesapSuz(aktif, k).forEach(r => ekle(k === 'yil' ? r[0].slice(0, 4) : k === 'ay' ? r[0].slice(5, 7) : k === 'tur' ? r[7] : yonu(r)));
    else satisSuz(k, false).forEach(r => ekle(k === 'yil' ? (r[1].slice(0, 4) || '—') : k === 'ay' ? (r[1].slice(5, 7) || '—') : k === 'musteri' ? V.musteriler[r[2]] : k === 'urun' ? V.urunler[r[3]] : (V.vadeler[r[12]] || '—')));
    const l = [...say.entries()].map(([v, n]) => ({ v: String(v), ad: k === 'ay' ? ayAd(v) : String(v), n }));
    return k === 'yil' ? l.sort((a, b) => b.v.localeCompare(a.v)) : k === 'ay' ? l.sort((a, b) => a.v.localeCompare(b.v)) : k === 'musteri' || k === 'urun' ? l.sort((a, b) => b.n - a.n || a.v.localeCompare(b.v, TR)) : l.sort((a, b) => a.v.localeCompare(b.v, TR));
  }
  function suzgecGorunum() { const g = GORUNEN[aktif] || []; $$('#upSuzgec .sz-cs').forEach(d => { d.hidden = !g.includes(d.dataset.f); }); }

  // ---------- tablo yardımcıları ----------
  /** sütun: { k, ad, v: sıralama değeri, h: hücre html'i, s: hücre sınıfı, x: Excel metni (yoksa v) } */
  function tabloCiz(p, kolonlar, satirlar, dip, sec) {
    sec = sec || {}; const t = $(`table[data-tablo="${p}"]`), d = D(p), panel = t.closest('.up-panel');
    const sk = kolonlar.find(c => c.k === d.k); let l = satirlar;
    if (sk) l = satirlar.slice().sort((a, b) => { const x = sk.v(a), y = sk.v(b); return (typeof x === 'number' && typeof y === 'number' ? x - y : String(x ?? '').localeCompare(String(y ?? ''), TR, { numeric: true })) * d.y; });
    const sayfaSayisi = Math.max(1, Math.ceil(l.length / SAYFA)); if (d.s >= sayfaSayisi) d.s = sayfaSayisi - 1; if (d.s < 0) d.s = 0;
    const gor = l.slice(d.s * SAYFA, d.s * SAYFA + SAYFA);
    t.tHead.innerHTML = `<tr>${kolonlar.map(c => `<th data-k="${c.k}" class="${d.k === c.k ? 'sirali' + (d.y < 0 ? ' azalan' : '') : ''}">${esc(c.ad)}</th>`).join('')}</tr>`;
    t.tBodies[0].innerHTML = gor.map((r, i) => `<tr class="${sec.satirSinif ? sec.satirSinif(r) : ''}">${kolonlar.map(c => `<td class="${c.s || ''}">${c.h ? c.h(r, d.s * SAYFA + i) : esc(c.v(r))}</td>`).join('')}</tr>`).join('') || `<tr><td colspan="${kolonlar.length}" class="up-bos">Süzgece uyan kayıt yok.</td></tr>`;
    t.tFoot.innerHTML = dip ? `<tr>${kolonlar.map((c, i) => `<td class="${i === 0 ? 'sol' : ''}">${dip[c.k] ?? ''}</td>`).join('')}</tr>` : '';
    $('[data-baslik]', panel).textContent = sec.baslik || ''; $('[data-say]', panel).textContent = sec.say || '';
    $('[data-sayfa]', panel).innerHTML = sayfaSayisi > 1 ? `<button type="button" data-s="-1" ${d.s === 0 ? 'disabled' : ''} title="Önceki sayfa">‹</button>${(d.s * SAYFA + 1).toLocaleString(TR)}–${Math.min(l.length, d.s * SAYFA + SAYFA).toLocaleString(TR)} / ${l.length.toLocaleString(TR)}<button type="button" data-s="1" ${d.s >= sayfaSayisi - 1 ? 'disabled' : ''} title="Sonraki sayfa">›</button>` : '';
    t._excel = { kolonlar, satirlar: l, dip, baslik: sec.baslik, say: sec.say };
    boyut();
  }
  /** özet kartı (küçük tablo, sayfalama yok, alt toplam var) */
  const kart = (id, baslik, bas, satirlar, dip, genis, ek) => `<div class="up-kart${genis ? ' genis' : ''}"><div class="up-kart-b"><span class="b">${esc(baslik)}</span>${ek || ''}</div><section class="up-kaydir"><table class="up-tablo" id="${id}" data-dinamik="kapali"><thead><tr>${bas.map(b => `<th>${esc(b)}</th>`).join('')}</tr></thead><tbody>${satirlar.map(r => `<tr>${r.map((h, i) => `<td class="${i === 0 ? 'sol' : 'sayi'}">${h}</td>`).join('')}</tr>`).join('') || `<tr><td colspan="${bas.length}" class="up-bos">Kayıt yok.</td></tr>`}</tbody><tfoot><tr>${dip.map((h, i) => `<td class="${i === 0 ? 'sol' : ''}">${h}</td>`).join('')}</tr></tfoot></table></section></div>`;
  const git = (sekme, f, v, ic) => `<a class="up-git" data-git="${sekme}" data-f="${f}" data-v="${esc(v)}" title="Tıklayınca ayrıntı">${ic}</a>`;
  /** tek tablolu sekmede tablo ekranın dibine kadar uzar (başlık ve dip toplam yapışkan; sayfa değil tablo kayar) */
  function boyut() { $$('.up-panel.aktif section[data-tam]').forEach(s => { const ust = s.getBoundingClientRect().top + window.scrollY; s.style.height = Math.max(280, window.innerHeight - ust - 14) + 'px'; }); }

  // ---------- sekmeler ----------
  const belgeSay = l => new Set(l.filter(r => !r[13]).map(r => r[1].slice(0, 4) + '/' + r[0])).size;   // fatura numarası her yıl 1'den başlar → anahtar yıl + numara
  function kpiCiz() {
    const l = satisSuz(null, false).filter(r => !r[13]), yillar = [...new Set(V.satis.map(r => r[1].slice(0, 4)).filter(Boolean))].sort(), buYil = yillar[yillar.length - 1], gecen = yillar[yillar.length - 2];
    const yilTop = y => topla(V.satis.filter(r => !r[13] && r[1].slice(0, 4) === y), r => r[8]);
    const sonOng = V.ongoru.filter(r => r[5] != null).pop(), b = V.bakiye, diger = b.tasarruf + b.ozel + b.kasa;
    const kpi = [['#0f172a', aktif === 'ozet' && F().yil.length ? 'Satış (Seçili Yıllar, KDV Hariç)' : 'Satış (Süzülen, KDV Hariç)', paraM(topla(l, r => r[8])), `KDV dahil ${paraM(topla(l, r => r[11]))} · ${belgeSay(l).toLocaleString(TR)} fatura`],
      ['#0f766e', `${buYil || ''} Yılı Satışı (KDV Hariç)`, paraM(yilTop(buYil)), gecen ? `${gecen}: ${paraM(yilTop(gecen))}` : ''],
      ['#1d4ed8', 'Şirket Banka Bakiyesi', paraM(b.sirket), `son hareket ${trh((V.sirket[V.sirket.length - 1] || [''])[0])}`],
      ['#7c3aed', 'Diğer Hesaplar', paraM(diger), `tasarruf ${paraM(b.tasarruf)} · Murat Bey özel ${paraM(b.ozel)} · kasa ${paraM(b.kasa)}`],
      [sonOng && sonOng[5] < 0 ? '#dc2626' : '#15803d', 'Öngörü Dönem Sonu Bakiyesi', sonOng ? paraM(sonOng[5]) : '—', sonOng ? `${trh(sonOng[0])} itibarıyla · ${V.ongoru.length} kalem` : ''],
      ['#b45309', 'Müşteri · Ürün', `${new Set(l.map(r => r[2])).size} · ${new Set(l.map(r => r[3])).size}`, `${topla(l, r => r[6]).toLocaleString(TR, { maximumFractionDigits: 0 })}${NB}kg net`]];
    $('#upKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${esc(k[1])}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
  }
  function ozetCiz() {
    const S = satisSuz(null, false).filter(r => !r[13]), B = hesapSuz('sirket', null), tS = topla(S, r => r[8]);
    const grupla = (l, anahtar) => { const m = new Map(); l.forEach(r => { const k = anahtar(r); if (!m.has(k)) m.set(k, []); m.get(k).push(r); }); return m; };
    // 1) yıllara göre satış
    const yS = [...grupla(S, r => r[1].slice(0, 4) || '—').entries()].sort((a, b) => b[0].localeCompare(a[0]));
    const satisYil = yS.map(([y, l]) => { const t = topla(l, r => r[8]), k = topla(l, r => r[6]); return [git('satis', 'yil', y, `<b>${esc(y)}</b>`), belgeSay(l).toLocaleString(TR), kg(k), para(t), para(topla(l, r => r[10])), para(topla(l, r => r[11])), k ? para(t / k) : tire, yuzde(t, tS)]; });
    const kS = topla(S, r => r[6]);
    // 2) yıllara göre şirket banka hesabı (yıl sonu bakiyesi: o yılın son hareketindeki bakiye — süzgeçten bağımsız)
    const yB = [...grupla(B, r => r[0].slice(0, 4)).entries()].sort((a, b) => b[0].localeCompare(a[0]));
    const yilSonu = y => { const l = V.sirket.filter(r => r[0].slice(0, 4) === y); return l.length ? l[l.length - 1][6] : 0; };
    const bankaYil = yB.map(([y, l]) => { const g = topla(l, r => r[4]), c = topla(l, r => r[5]); return [git('sirket', 'yil', y, `<b>${esc(y)}</b>`), l.length.toLocaleString(TR), `<span class="giris">${paraM(g)}</span>`, `<span class="cikis">${paraM(c)}</span>`, para(g - c), para(yilSonu(y))]; });
    const gB = topla(B, r => r[4]), cB = topla(B, r => r[5]);
    // 3) hareket türleri (şirket banka hesabı)
    const tur = [...grupla(B, r => r[7]).entries()].map(([t, l]) => ({ t, n: l.length, g: topla(l, r => r[4]), c: topla(l, r => r[5]) })).sort((a, b) => (b.g + b.c) - (a.g + a.c));
    const turSatir = tur.map(x => [git('sirket', 'tur', x.t, esc(x.t)), x.n.toLocaleString(TR), x.g ? `<span class="giris">${paraM(x.g)}</span>` : tire, x.c ? `<span class="cikis">${paraM(x.c)}</span>` : tire, x.c ? yuzde(x.c, cB) : tire]);
    // 4) hesap bakiyeleri
    const hesap = Object.keys(HESAP).map(h => { const l = V[h], son = l[l.length - 1]; return [git(h, '', '', esc(HESAP[h])), l.length.toLocaleString(TR), son ? trh(son[0]) : '—', `<span class="giris">${paraM(topla(l, r => r[4]))}</span>`, `<span class="cikis">${paraM(topla(l, r => r[5]))}</span>`, `<b>${para(V.bakiye[h])}</b>`]; });
    const bT = V.bakiye.sirket + V.bakiye.tasarruf + V.bakiye.ozel + V.bakiye.kasa;
    // 5) ilk 15 müşteri · 6) ilk 15 ürün
    const ilk = (anahtar, adlar, f) => [...grupla(S, anahtar).entries()].map(([i, l]) => ({ ad: adlar[i], n: belgeSay(l), k: topla(l, r => r[6]), t: topla(l, r => r[8]) })).sort((a, b) => b.t - a.t).slice(0, 15).map((x, i) => [`${i + 1}. ` + git('satis', f, x.ad, esc(x.ad)), x.n.toLocaleString(TR), kg(x.k), para(x.t), x.k ? para(x.t / x.k) : tire, yuzde(x.t, tS)]);
    const mS = new Set(S.map(r => r[2])).size, uS = new Set(S.map(r => r[3])).size;
    $('#upOzet').innerHTML =
      kart('upOzYil', 'Yıllara Göre Satış', ['Yıl', 'Fatura', 'Net Kg', `Tutar${NB}(€)`, `KDV${NB}(€)`, `KDV Dahil${NB}(€)`, `Ort. Kg Fiyatı`, 'Pay'], satisYil, [`Toplam · ${yS.length} yıl`, belgeSay(S).toLocaleString(TR), kg(kS), paraM(tS), paraM(topla(S, r => r[10])), paraM(topla(S, r => r[11])), kS ? paraM(tS / kS) : '', ''])
      + kart('upOzBanka', 'Yıllara Göre Şirket Banka Hesabı', ['Yıl', 'Hareket', `Giriş${NB}(€)`, `Çıkış${NB}(€)`, `Net${NB}(€)`, `Yıl Sonu Bakiyesi`], bankaYil, [`Toplam · ${yB.length} yıl`, B.length.toLocaleString(TR), paraM(gB), paraM(cB), paraM(gB - cB), paraM(V.bakiye.sirket)])
      + kart('upOzTur', 'Hareket Türleri · Şirket Banka Hesabı', ['Tür', 'Hareket', `Giriş${NB}(€)`, `Çıkış${NB}(€)`, 'Çıkış Payı'], turSatir, [`Toplam · ${tur.length} tür`, B.length.toLocaleString(TR), paraM(gB), paraM(cB), ''])
      + kart('upOzHesap', 'Hesap Bakiyeleri', ['Hesap', 'Hareket', 'Son Hareket', `Toplam Giriş`, `Toplam Çıkış`, 'Bakiye'], hesap, ['Toplam · 4 hesap', '', '', '', '', paraM(bT)])
      + kart('upOzMusteri', 'En Çok Satış Yapılan 15 Müşteri', ['Müşteri', 'Fatura', 'Net Kg', `Tutar${NB}(€)`, 'Ort. Kg Fiyatı', 'Pay'], ilk(r => r[2], V.musteriler, 'musteri'), [`Tüm müşteriler · ${mS}`, belgeSay(S).toLocaleString(TR), kg(kS), paraM(tS), kS ? paraM(tS / kS) : '', ''])
      + kart('upOzUrun', 'En Çok Satılan 15 Ürün', ['Ürün', 'Fatura', 'Net Kg', `Tutar${NB}(€)`, 'Ort. Kg Fiyatı', 'Pay'], ilk(r => r[3], V.urunler, 'urun'), [`Tüm ürünler · ${uS}`, belgeSay(S).toLocaleString(TR), kg(kS), paraM(tS), kS ? paraM(tS / kS) : '', '']);
  }
  function satisCiz() {
    const l = satisSuz(null), g = l.filter(r => !r[13]); if (!D('satis').k) Object.assign(D('satis'), { k: 'tarih', y: -1 });
    tabloCiz('satis', [
      { k: 'belge', ad: 'Belge No', v: r => +r[0] || r[0], h: r => `<b>${esc(r[0])}</b>` }, { k: 'tarih', ad: 'Belge Tarihi', v: r => (r[1] || '0000-00-00') + String(1e7 + (+r[0] || 0)), h: r => r[1] ? trh(r[1]) : tire, x: r => r[1] ? trh(r[1]) : '' },   // tarihsiz (iptal) satırlar en sonda
      { k: 'musteri', ad: 'Müşteri', v: r => V.musteriler[r[2]], s: 'sol' }, { k: 'urun', ad: 'Ürün', v: r => V.urunler[r[3]], s: 'sol' },
      { k: 'ambalaj', ad: 'Ambalaj (Kg)', v: r => r[4], h: r => r[4] ? adet(r[4]) : tire, s: 'sayi' }, { k: 'adet', ad: 'Ambalaj Adedi', v: r => r[5], h: r => r[5] ? adet(r[5]) : tire, s: 'sayi' },
      { k: 'kg', ad: 'Net Kg', v: r => r[6], h: r => kg(r[6]), s: 'sayi', x: r => adet(r[6]) }, { k: 'fiyat', ad: 'Kg Fiyatı (€)', v: r => r[7], h: r => r[7] ? (+r[7]).toLocaleString(TR, { minimumFractionDigits: 2, maximumFractionDigits: 4 }) + NB + '€' : tire, s: 'sayi', x: r => n2(r[7]) },
      { k: 'tutar', ad: 'Tutar (€)', v: r => r[8], h: r => para(r[8]), s: 'sayi', x: r => n2(r[8]) }, { k: 'oran', ad: 'KDV Oranı', v: r => r[9], h: r => r[9] ? '%' + NB + Math.round(r[9] * 100) : tire, s: 'sayi', x: r => r[9] ? '% ' + Math.round(r[9] * 100) : '' },
      { k: 'kdv', ad: 'KDV (€)', v: r => r[10], h: r => para(r[10]), s: 'sayi', x: r => n2(r[10]) }, { k: 'toplam', ad: 'KDV Dahil (€)', v: r => r[11], h: r => `<b>${para(r[11])}</b>`, s: 'sayi', x: r => n2(r[11]) },
      { k: 'vade', ad: 'Vade', v: r => V.vadeler[r[12]] || '', h: r => V.vadeler[r[12]] ? `<span class="up-rz">${esc(V.vadeler[r[12]])}</span>` : tire }
    ], l, { belge: `Toplam · ${belgeSay(g).toLocaleString(TR)} fatura`, urun: `${g.length.toLocaleString(TR)} satır`, adet: adet(topla(g, r => r[5])), kg: kg(topla(g, r => r[6])), tutar: paraM(topla(g, r => r[8])), kdv: paraM(topla(g, r => r[10])), toplam: paraM(topla(g, r => r[11])) },
      { baslik: 'Satışlar', say: `${g.length.toLocaleString(TR)} / ${V.satis.filter(r => !r[13]).length.toLocaleString(TR)} satır`, satirSinif: r => r[13] ? 'iptal' : '' });
  }
  /** AYLIK SATIŞLAR (20 Kas 2026, kullanıcı: "tüm yılların ay ay satış rakamlarını görebilelim"): satır = ay (Ocak … Aralık), sütun = yıl + Toplam; hücrede KDV hariç tutar, altında net kg ve fatura sayısı.
   *  Dip toplam = yıl toplamı. Hücreye tıklayınca Satışlar sekmesi o yıl ve ay ile açılır. Süzgeçler (yıl, müşteri, ürün, vade) geçerli */
  function aylikCiz() {
    const S = satisSuz(null).filter(r => !r[13] && r[1]), yillar = [...new Set(S.map(r => r[1].slice(0, 4)))].sort(), m = new Map();
    S.forEach(r => { const k = r[1].slice(0, 7); let o = m.get(k); if (!o) m.set(k, o = { t: 0, k: 0, b: new Set() }); o.t += r[8]; o.k += r[6]; o.b.add(r[0]); });
    const hucre = (y, a) => m.get(y + '-' + a), aylar = AYLAR.map((ad, i) => ({ ad, a: String(i + 1).padStart(2, '0') }));
    const yilTop = y => topla(aylar, x => (hucre(y, x.a) || { t: 0 }).t), ayTop = x => topla(yillar, y => (hucre(y, x.a) || { t: 0 }).t), tS = topla(S, r => r[8]);
    const hH = (o, y, a) => o ? `<a class="up-git" data-git="satis" data-f="yil" data-v="${y}" data-f2="ay" data-v2="${a}" title="${o.b.size} fatura · ${adet(o.k)} kg — tıklayınca o ayın satışları"><b>${paraM(o.t)}</b></a><small class="up-alt">${adet(o.k)}${NB}kg · ${o.b.size} fatura</small>` : tire;
    if (!D('aylik').k) Object.assign(D('aylik'), { k: 'ay', y: 1 });
    const dip = { ay: `Toplam · ${yillar.length} yıl`, toplam: paraM(tS), pay: '' };
    yillar.forEach(y => { const l = S.filter(r => r[1].slice(0, 4) === y); dip['y' + y] = `${paraM(yilTop(y))}<small class="up-alt">${adet(topla(l, r => r[6]))}${NB}kg · ${belgeSay(l).toLocaleString(TR)} fatura</small>`; });
    tabloCiz('aylik', [
      { k: 'ay', ad: 'Ay', v: x => x.a, h: x => `<b>${x.ad}</b>`, x: x => x.ad },
      ...yillar.map(y => ({ k: 'y' + y, ad: y, v: x => (hucre(y, x.a) || { t: 0 }).t, h: x => hH(hucre(y, x.a), y, x.a), s: 'sayi', x: x => hucre(y, x.a) ? n2(hucre(y, x.a).t) : '' })),
      { k: 'toplam', ad: 'Toplam (€)', v: ayTop, h: x => `<b>${para(ayTop(x))}</b>`, s: 'sayi', x: x => n2(ayTop(x)) }, { k: 'pay', ad: 'Pay', v: ayTop, h: x => yuzde(ayTop(x), tS), s: 'sayi', x: x => yuzde(ayTop(x), tS) }
    ], aylar, dip, { baslik: 'Aylık Satışlar · KDV Hariç (€)', say: `${yillar.length} yıl · ${belgeSay(S).toLocaleString(TR)} fatura` });
  }
  // ---------- ayrıntı penceresi (rakama / müşteriye tıklayınca kalem kalem) ----------
  const pen = $('#upPen'), penKapat = () => { pen.hidden = true; };
  function penAc(ad, alt, govde) { $('#upPenAd').textContent = ad; $('#upPenAlt').textContent = alt || ''; $('#upPenGovde').innerHTML = govde; pen.hidden = false; $('#upPenGovde').scrollTop = 0; }
  pen.addEventListener('click', e => { if (e.target === pen || e.target.closest('#upPenKapat')) penKapat(); });
  document.addEventListener('keydown', e => { if (e.key === 'Escape' && !pen.hidden) penKapat(); });
  const penTablo = (baslik, bas, satirlar, dip) => `<div class="up-kart mb-2"><div class="up-kart-b"><span class="b">${esc(baslik)}</span><span class="say">${satirlar.length.toLocaleString(TR)} satır</span></div><table class="up-tablo" data-dinamik="kapali"><thead><tr>${bas.map(b => `<th>${esc(b)}</th>`).join('')}</tr></thead><tbody>${satirlar.map(r => `<tr>${r.map((h, i) => `<td class="${i === 0 ? 'sol' : ''}">${h}</td>`).join('')}</tr>`).join('') || `<tr><td colspan="${bas.length}" class="up-bos">Kayıt yok.</td></tr>`}</tbody>${dip ? `<tfoot><tr>${dip.map((h, i) => `<td class="${i === 0 ? 'sol' : ''}">${h}</td>`).join('')}</tr></tfoot>` : ''}</table></div>`;
  /** tıklanınca çalışacak işlevler (her çizimde sıfırlanır): ac(fn) → <a data-ac="i"> */
  let acilir = []; const ac = (fn, ic, ipucu) => `<a class="up-git" data-ac="${acilir.push(fn) - 1}" title="${esc(ipucu || 'Tıklayınca kalem kalem')}">${ic}</a>`;

  // ---------- AYLIK GİDERLER (20 Kas 2026; kullanıcı: "şirket hesabını analiz et, ay ay gider raporu oluştur … rakama tıkladığımda maaş mı HGS mi benzin mi kalem kalem görebileyim") ----------
  //   Gider = şirket banka hesabının ÇIKIŞLARI; hesaplar arası transferler (tasarruf hesabına aktarım vb.) gider değildir, sayılmaz. Kalem ayrımı sunucuda (UrasPortekizCeviri.Kalem → satırın 9. alanı).
  //   Üstte seçili yılın kalem × ay tablosu (yıl düğmeleri kart başlığında), altta ay × yıl. Her rakam tıklanır: kalem özeti + hareketler tek tek. Hareket Türü süzgeci ve arama geçerli.
  let giderYil = null;
  function giderSatir(turHaric) { const ara = kucuk(F().ara); return V.sirket.filter(r => r[5] > 0 && r[7] !== 'Hesaplar Arası Transfer' && (turHaric || uy('tur', r[7])) && (!ara || kucuk(r[2] + ' ' + r[3] + ' ' + r[8]).includes(ara))); }
  function giderDetay(baslik, l) {
    const top = topla(l, r => r[5]), m = new Map(); l.forEach(r => { const o = m.get(r[8]) || { n: 0, t: 0 }; o.n++; o.t += r[5]; m.set(r[8], o); });
    const kalemler = [...m.entries()].sort((a, b) => b[1].t - a[1].t);
    const ozet = kalemler.length > 1 ? penTablo('Kalem Özeti', ['Kalem', 'Hareket', 'Tutar (€)', 'Pay'], kalemler.map(([k, o]) => [`<b>${esc(k)}</b>`, o.n.toLocaleString(TR), `<span class="cikis">${paraM(o.t)}</span>`, yuzde(o.t, top)]), [`Toplam · ${kalemler.length} kalem`, l.length.toLocaleString(TR), paraM(top), '']) : '';
    const sirali = l.slice().sort((a, b) => a[0].localeCompare(b[0]));
    penAc(baslik, `${l.length.toLocaleString(TR)} hareket · ${paraM(top)}`, ozet + penTablo('Hareketler', ['Tarih', 'Belge No', 'Açıklama', 'Kalem', 'Tutar (€)'], sirali.map(r => [trh(r[0]), esc(r[1]) || tire, `<span title="${esc(r[3])}">${esc(r[2])}</span>`, `<span class="up-rz">${esc(r[8])}</span>`, `<span class="cikis">${paraM(r[5])}</span>`]), [`Toplam · ${l.length.toLocaleString(TR)} hareket`, '', '', '', paraM(top)]));
  }
  function giderCiz() {
    const G = giderSatir(), yillar = [...new Set(G.map(r => r[0].slice(0, 4)))].sort(), sonYil = (V.sonHareket || '').slice(0, 4);
    if (!giderYil || !yillar.includes(giderYil)) giderYil = yillar.includes(sonYil) ? sonYil : yillar[yillar.length - 1] || '';
    const aylar = AYLAR.map((ad, i) => ({ ad, a: String(i + 1).padStart(2, '0') })), T = l => topla(l, r => r[5]);
    const hucre = (l, baslik, kalin) => l.length ? ac(() => giderDetay(baslik, l), `${kalin ? '<b>' : ''}${paraM(T(l))}${kalin ? '</b>' : ''}`, `${l.length} hareket — tıklayınca kalem kalem`) : tire;
    // 1) seçili yıl: kalem × ay
    const Y = G.filter(r => r[0].slice(0, 4) === giderYil), kalemler = [...new Set(Y.map(r => r[8]))].sort((a, b) => T(Y.filter(r => r[8] === b)) - T(Y.filter(r => r[8] === a))), tY = T(Y);
    const kSatir = kalemler.map(k => { const L = Y.filter(r => r[8] === k); return [`<b>${esc(k)}</b><small class="up-alt">${L.length} hareket</small>`, ...aylar.map(x => hucre(L.filter(r => r[0].slice(5, 7) === x.a), `${k} · ${x.ad} ${giderYil}`)), hucre(L, `${k} · ${giderYil}`, true), yuzde(T(L), tY)]; });
    const kDip = [`Toplam · ${kalemler.length} kalem`, ...aylar.map(x => hucre(Y.filter(r => r[0].slice(5, 7) === x.a), `${x.ad} ${giderYil} · Bütün Giderler`, true)), hucre(Y, `${giderYil} · Bütün Giderler`, true), ''];
    const yilDgm = `<span class="up-yil">${yillar.map(y => `<button type="button" data-gyil="${y}" class="${y === giderYil ? 'aktif' : ''}">${y}</button>`).join('')}</span>`;
    // 2) bütün yıllar: ay × yıl
    const aSatir = aylar.map(x => { const L = G.filter(r => r[0].slice(5, 7) === x.a); return [`<b>${x.ad}</b>`, ...yillar.map(y => hucre(L.filter(r => r[0].slice(0, 4) === y), `${x.ad} ${y} · Bütün Giderler`)), hucre(L, `${x.ad} · Bütün Yıllar`, true)]; });
    const aDip = [`Toplam · ${yillar.length} yıl`, ...yillar.map(y => hucre(G.filter(r => r[0].slice(0, 4) === y), `${y} · Bütün Giderler`, true)), hucre(G, 'Bütün Giderler', true)];
    $('#upGider').innerHTML = kart('upGiderKalem', `Gider Kalemleri · Ay Ay · ${giderYil}`, ['Kalem', ...aylar.map(x => x.ad), 'Toplam (€)', 'Pay'], kSatir, kDip, false, yilDgm + `<span class="say">${Y.length.toLocaleString(TR)} hareket · şirket banka hesabı · hesaplar arası transfer hariç</span>`)
      + kart('upGiderYil', 'Aylık Giderler · Yıllara Göre', ['Ay', ...yillar, 'Toplam (€)'], aSatir, aDip, false, `<span class="say">${G.length.toLocaleString(TR)} hareket · ${paraM(T(G))}</span>`);
  }

  // ---------- MÜŞTERİ BAKİYELERİ ve TAHSİLATLAR (20 Kas 2026; kullanıcı: "müşteri bazında bakiye raporu hazırla, tahsilatları müşteri bazlı analiz et, fatura − tahsilattan müşteri bakiyelerini görelim; istersen bir tahsilat sekmesi eklersin") ----------
  //   Dosyada müşteri bakiyesi sayfası YOK: bakiye burada hesaplanır = müşterinin faturaları (KDV dahil) − müşteriden alınan tahsilatlar.
  //   Tahsilat = hesap hareketlerindeki "PAYMENT RECEIVED FROM <müşteri> - FT <yıl>A1/<no>+<no> (RECEIPT …)" satırları (şirket banka hesabı, kasa, Murat Bey özel hesap).
  //   Müşteri eşlemesi önce FATURA NUMARASINDAN (FT 2026A1/870 → 2026 yılının 870 no.lu faturası → o faturanın müşterisi), fatura bulunamazsa ADDAN (aksanlar ve noktalama atılarak; ad birbiriyle başlıyorsa ya da ilk iki sözcük aynıysa).
  //   Hiçbiriyle eşleşmeyen tahsilatlar "Eşleşmeyen Tahsilatlar" satırında toplanır (bakiyeleri bozmasın diye gizlenmez).
  const adNorm = s => String(s || '').normalize('NFD').replace(/[̀-ͯ]/g, '').toUpperCase().replace(/[^A-Z0-9 ]+/g, ' ').replace(/\b(LDA|UNIP|UNIPESSOAL|SOCIEDADE|SA|S A)\b/g, ' ').replace(/\s+/g, ' ').trim();
  /** "FT 2020A1/521 (REMAINING)+523 + FT2021A1/16+41" → ['2020/521', '2020/523', '2021/16', '2021/41'] (makbuz numarası sayılmaz) */
  function ftCoz(s) { const out = [], t = String(s || '').replace(/\(RECEIPTS?[^)]*\)/gi, ' '), re = /FTS?\s*(\d{4})\s*A\d*\s*\/\s*((?:\d+|\s*[+,&]\s*|\s*\((?:REMAINING|PARTIAL|PART)\)\s*)+)/gi; let m; while ((m = re.exec(t))) (m[2].match(/\d+/g) || []).forEach(n => out.push(m[1] + '/' + String(+n))); return out; }
  function tahsilatVeri() {
    if (V._th) return V._th;
    const fat = new Map(); V.satis.forEach(r => { if (r[13] || !r[1]) return; const k = r[1].slice(0, 4) + '/' + String(+r[0] || r[0]); let o = fat.get(k); if (!o) fat.set(k, o = { k, m: r[2], tarih: r[1], no: r[0], top: 0, vade: V.vadeler[r[12]] || '' }); o.top += r[11]; });
    const normlar = V.musteriler.map((a, i) => ({ i, n: adNorm(a) })).filter(x => x.n && x.n !== 'FATURA IPTAL');
    const adBul = isim => { const n = adNorm(isim); if (!n) return -1; let x = normlar.find(o => o.n === n); if (x) return x.i; x = normlar.filter(o => n.startsWith(o.n) || o.n.startsWith(n)); if (x.length === 1) return x[0].i; const iki = n.split(' ').slice(0, 2).join(' '); x = normlar.filter(o => o.n.split(' ').slice(0, 2).join(' ') === iki); return x.length === 1 ? x[0].i : -1; };
    const l = [];
    [['sirket', 'Şirket Banka Hesabı'], ['kasa', 'Kasa (EUR)'], ['ozel', 'Murat Bey Özel Hesap']].forEach(([h, hesap]) => V[h].forEach(r => {
      const m = /^PAYMENTS? RECEIVED FROM\s+(.+?)(\s*-\s*(?:FTS?|FAC|INVOICE|NC|ND)(?![A-Z]).*)?$/i.exec(r[3]); if (!m || !(r[4] > 0)) return;
      const isim = m[1].trim(), refs = ftCoz(r[3]), bulunan = refs.map(k => fat.get(k)).filter(Boolean), mi = bulunan.length ? bulunan[0].m : adBul(isim);
      l.push({ tarih: r[0], hesap, isim, refs, bulunan, m: mi, ad: mi >= 0 ? V.musteriler[mi] : 'Eşleşmedi: ' + isim, yol: bulunan.length ? 'Fatura No' : mi >= 0 ? 'Ad' : 'Eşleşmedi', tutar: r[4], tr: r[2], asil: r[3], belge: r[1], makbuz: ((/\(RECEIPTS?\s*([^)]+)\)/i.exec(r[3]) || [])[1] || '').trim() });
    }));
    const gecen = new Map(); l.forEach(t => t.bulunan.forEach(f => { if (!gecen.has(f.k)) gecen.set(f.k, t); }));   // fatura → onu ilk anan tahsilat
    return (V._th = { l, fat, gecen });
  }
  function tahsilatSuz(haric) { const ara = kucuk(F().ara), u = k => k !== haric; return tahsilatVeri().l.filter(t => (!u('yil') || uy('yil', t.tarih.slice(0, 4))) && (!u('ay') || uy('ay', t.tarih.slice(5, 7))) && (!u('musteri') || uy('musteri', t.ad)) && (!ara || kucuk(t.ad + ' ' + t.isim + ' ' + t.asil + ' ' + trh(t.tarih)).includes(ara))); }
  function tahsilatCiz() {
    const l = tahsilatSuz(null), tum = tahsilatVeri().l; if (!D('tahsilat').k) Object.assign(D('tahsilat'), { k: 'tarih', y: -1 });
    const sira = new Map(tum.map((t, i) => [t, i]));
    tabloCiz('tahsilat', [
      { k: 'tarih', ad: 'Tarih', v: t => t.tarih + String(1e6 + sira.get(t)), h: t => trh(t.tarih), x: t => trh(t.tarih) }, { k: 'musteri', ad: 'Müşteri', v: t => t.ad, h: t => t.m >= 0 ? ac(() => musteriDetay(t.m), `<b>${esc(t.ad)}</b>`, 'Müşterinin fatura ve tahsilatları') : `<span class="eksi">${esc(t.ad)}</span>`, s: 'sol' },
      { k: 'yol', ad: 'Eşleşme', v: t => t.yol, h: t => `<span class="up-rz">${esc(t.yol)}</span>` }, { k: 'hesap', ad: 'Hesap', v: t => t.hesap }, { k: 'fatura', ad: 'Fatura No', v: t => t.refs.join(', '), h: t => t.refs.length ? esc(t.refs.join(', ')) : tire, s: 'metin' },
      { k: 'makbuz', ad: 'Makbuz', v: t => t.makbuz, h: t => esc(t.makbuz) || tire }, { k: 'tutar', ad: 'Tahsilat (€)', v: t => t.tutar, h: t => `<span class="giris">${paraM(t.tutar)}</span>`, s: 'sayi', x: t => n2(t.tutar) }, { k: 'asil', ad: 'Asıl Metin (İngilizce)', v: t => t.asil, s: 'asil' }
    ], l, { tarih: `Toplam · ${l.length.toLocaleString(TR)} tahsilat`, musteri: `${new Set(l.map(t => t.ad)).size} müşteri`, yol: `${l.filter(t => t.m < 0).length} eşleşmedi`, tutar: paraM(topla(l, t => t.tutar)) },
      { baslik: 'Tahsilatlar · Müşteriden Alınan Ödemeler', say: `${l.length.toLocaleString(TR)} / ${tum.length.toLocaleString(TR)} tahsilat` });
  }
  /** müşteri bakiye satırları: fatura (KDV dahil) − tahsilat; müşteri süzgeci ve arama */
  function bakiyeSatir(suzgecsiz) {
    const { l, fat, gecen } = tahsilatVeri(), m = new Map(), al = i => { let o = m.get(i); if (!o) m.set(i, o = { m: i, ad: i >= 0 ? V.musteriler[i] : 'Eşleşmeyen Tahsilatlar', f: [], t: [] }); return o; };
    fat.forEach(f => al(f.m).f.push(f)); l.forEach(t => al(t.m >= 0 ? t.m : -1).t.push(t));
    const ara = suzgecsiz ? '' : kucuk(F().ara);
    return [...m.values()].map(o => { o.fatura = topla(o.f, f => f.top); o.tahsil = topla(o.t, t => t.tutar); o.bakiye = Math.round((o.fatura - o.tahsil) * 100) / 100; o.acik = o.f.filter(f => !gecen.has(f.k) && f.top > 0.005).sort((a, b) => a.tarih.localeCompare(b.tarih)); o.sonF = o.f.map(f => f.tarih).sort().pop() || ''; o.sonT = o.t.map(t => t.tarih).sort().pop() || ''; return o; })
      .filter(o => suzgecsiz || (uy('musteri', o.ad) && (!ara || kucuk(o.ad).includes(ara))));
  }
  function musteriDetay(mi) {
    const o = bakiyeSatir(true).find(x => x.m === mi); if (!o) return; const { gecen } = tahsilatVeri(), gun = t => Math.round((new Date(V.sonHareket) - new Date(t)) / 864e5);
    const fS = o.f.slice().sort((a, b) => b.tarih.localeCompare(a.tarih)).map(f => { const t = gecen.get(f.k); return [trh(f.tarih), `<b>${esc(f.k)}</b>`, esc(f.vade) || tire, para(f.top), t ? `<span class="giris">${trh(t.tarih)}</span>` : `<span class="cikis">tahsilatta geçmiyor · ${gun(f.tarih)}${NB}gün</span>`]; });
    const tS = o.t.slice().sort((a, b) => b.tarih.localeCompare(a.tarih)).map(t => [trh(t.tarih), esc(t.hesap), t.refs.length ? esc(t.refs.join(', ')) : tire, esc(t.makbuz) || tire, `<span class="giris">${paraM(t.tutar)}</span>`]);
    penAc(o.ad, `fatura ${paraM(o.fatura)} − tahsilat ${paraM(o.tahsil)} = bakiye ${paraM(o.bakiye)}`,
      penTablo('Faturalar (KDV Dahil)', ['Tarih', 'Fatura No', 'Vade', 'Tutar (€)', 'Tahsilat'], fS, [`Toplam · ${o.f.length} fatura`, '', '', paraM(o.fatura), `${o.acik.length} fatura tahsilatta geçmiyor`])
      + penTablo('Tahsilatlar', ['Tarih', 'Hesap', 'Fatura No', 'Makbuz', 'Tutar (€)'], tS, [`Toplam · ${o.t.length} tahsilat`, '', '', '', paraM(o.tahsil)]));
  }
  function bakiyeCiz() {
    const l = bakiyeSatir(false); if (!D('bakiye').k) Object.assign(D('bakiye'), { k: 'bakiye', y: -1 });
    const gun = t => Math.round((new Date(V.sonHareket) - new Date(t)) / 864e5), acikT = o => topla(o.acik, f => f.top);
    tabloCiz('bakiye', [
      { k: 'ad', ad: 'Müşteri', v: o => o.ad, h: o => o.m >= 0 ? ac(() => musteriDetay(o.m), `<b>${esc(o.ad)}</b>`, 'Fatura ve tahsilatları tek tek') : ac(() => penAc('Eşleşmeyen Tahsilatlar', 'faturası ya da müşterisi bulunamayan tahsilat satırları', penTablo('Tahsilatlar', ['Tarih', 'Hesap', 'Asıl Metin', 'Tutar (€)'], o.t.map(t => [trh(t.tarih), esc(t.hesap), esc(t.asil), paraM(t.tutar)]), [`Toplam · ${o.t.length}`, '', '', paraM(o.tahsil)])), `<b class="eksi">${esc(o.ad)}</b>`), s: 'sol' },
      { k: 'fAdet', ad: 'Fatura', v: o => o.f.length, s: 'sayi' }, { k: 'fatura', ad: 'Faturalanan · KDV Dahil (€)', v: o => o.fatura, h: o => para(o.fatura), s: 'sayi', x: o => n2(o.fatura) },
      { k: 'tAdet', ad: 'Tahsilat', v: o => o.t.length, s: 'sayi' }, { k: 'tahsil', ad: 'Tahsil Edilen (€)', v: o => o.tahsil, h: o => o.tahsil ? `<span class="giris">${paraM(o.tahsil)}</span>` : tire, s: 'sayi', x: o => n2(o.tahsil) },
      { k: 'bakiye', ad: 'Bakiye (€)', v: o => o.bakiye, h: o => `<b>${para(o.bakiye)}</b>`, s: 'sayi', x: o => n2(o.bakiye) },
      { k: 'acik', ad: 'Tahsilatta Geçmeyen Fatura', v: acikT, h: o => o.acik.length ? `${paraM(acikT(o))}<small class="up-alt">${o.acik.length} fatura</small>` : tire, s: 'sayi', x: o => o.acik.length ? n2(acikT(o)) : '' },
      { k: 'eski', ad: 'En Eski Geçmeyen Fatura', v: o => o.acik.length ? o.acik[0].tarih : '9999', h: o => o.acik.length ? `${trh(o.acik[0].tarih)}<small class="up-alt">${gun(o.acik[0].tarih)}${NB}gün</small>` : tire, x: o => o.acik.length ? trh(o.acik[0].tarih) : '' },
      { k: 'sonF', ad: 'Son Fatura', v: o => o.sonF, h: o => o.sonF ? trh(o.sonF) : tire, x: o => o.sonF ? trh(o.sonF) : '' }, { k: 'sonT', ad: 'Son Tahsilat', v: o => o.sonT, h: o => o.sonT ? trh(o.sonT) : tire, x: o => o.sonT ? trh(o.sonT) : '' }
    ], l, { ad: `Toplam · ${l.filter(o => o.m >= 0).length.toLocaleString(TR)} müşteri`, fAdet: topla(l, o => o.f.length).toLocaleString(TR), fatura: paraM(topla(l, o => o.fatura)), tAdet: topla(l, o => o.t.length).toLocaleString(TR), tahsil: paraM(topla(l, o => o.tahsil)), bakiye: paraM(topla(l, o => o.bakiye)), acik: paraM(topla(l, acikT)) },
      { baslik: 'Müşteri Bakiyeleri · Fatura − Tahsilat', say: `${l.filter(o => o.bakiye > 0.5).length} müşteride alacak · ${l.filter(o => o.bakiye < -0.5).length} müşteride fazla tahsilat`, satirSinif: o => o.m < 0 ? 'bakiye' : '' });
  }
  /** müşteri / ürün bazında özet */
  function kirilimCiz(p) {
    const mu = p === 'musteri', i = mu ? 2 : 3, adlar = mu ? V.musteriler : V.urunler, S = satisSuz(null).filter(r => !r[13]), m = new Map();
    S.forEach(r => { let o = m.get(r[i]); if (!o) m.set(r[i], o = { ad: adlar[r[i]], b: new Set(), s: 0, k: 0, t: 0, v: 0, top: 0, ilk: r[1], son: r[1], cesit: new Set() }); o.b.add(r[1].slice(0, 4) + '/' + r[0]); o.s++; o.k += r[6]; o.t += r[8]; o.v += r[10]; o.top += r[11]; o.cesit.add(mu ? r[3] : r[2]); if (r[1] && (!o.ilk || r[1] < o.ilk)) o.ilk = r[1]; if (r[1] > o.son) o.son = r[1]; });
    const l = [...m.values()], tS = topla(l, o => o.t); if (!D(p).k) Object.assign(D(p), { k: 'tutar', y: -1 });
    tabloCiz(p, [
      { k: 'ad', ad: mu ? 'Müşteri' : 'Ürün', v: o => o.ad, h: o => git('satis', mu ? 'musteri' : 'urun', o.ad, `<b>${esc(o.ad)}</b>`), s: 'sol' }, { k: 'fatura', ad: 'Fatura', v: o => o.b.size, s: 'sayi' }, { k: 'satir', ad: 'Satır', v: o => o.s, s: 'sayi' },
      { k: 'cesit', ad: mu ? 'Ürün Çeşidi' : 'Müşteri Sayısı', v: o => o.cesit.size, s: 'sayi' }, { k: 'kg', ad: 'Net Kg', v: o => o.k, h: o => kg(o.k), s: 'sayi', x: o => adet(o.k) },
      { k: 'tutar', ad: 'Tutar (€)', v: o => o.t, h: o => `<b>${para(o.t)}</b>`, s: 'sayi', x: o => n2(o.t) }, { k: 'kdv', ad: 'KDV (€)', v: o => o.v, h: o => para(o.v), s: 'sayi', x: o => n2(o.v) }, { k: 'toplam', ad: 'KDV Dahil (€)', v: o => o.top, h: o => para(o.top), s: 'sayi', x: o => n2(o.top) },
      { k: 'ort', ad: 'Ort. Kg Fiyatı (€)', v: o => o.k ? o.t / o.k : 0, h: o => o.k ? para(o.t / o.k) : tire, s: 'sayi', x: o => o.k ? n2(o.t / o.k) : '' }, { k: 'pay', ad: 'Pay', v: o => o.t, h: o => yuzde(o.t, tS), s: 'sayi', x: o => yuzde(o.t, tS) },
      { k: 'ilk', ad: 'İlk Fatura', v: o => o.ilk, h: o => trh(o.ilk), x: o => trh(o.ilk) }, { k: 'son', ad: 'Son Fatura', v: o => o.son, h: o => trh(o.son), x: o => trh(o.son) }
    ], l, { ad: `Toplam · ${l.length.toLocaleString(TR)} ${mu ? 'müşteri' : 'ürün'}`, fatura: belgeSay(S).toLocaleString(TR), satir: S.length.toLocaleString(TR), kg: kg(topla(l, o => o.k)), tutar: paraM(tS), kdv: paraM(topla(l, o => o.v)), toplam: paraM(topla(l, o => o.top)), ort: topla(l, o => o.k) ? paraM(tS / topla(l, o => o.k)) : '' },
      { baslik: mu ? 'Müşteri Bazında Satış' : 'Ürün Bazında Satış', say: `${l.length.toLocaleString(TR)} ${mu ? 'müşteri' : 'ürün'}` });
  }
  function hesapCiz(p) {
    const l = hesapSuz(p, null), sirket = p === 'sirket'; if (!D(p).k) Object.assign(D(p), { k: 'tarih', y: -1 });
    const sira = new Map(V[p].map((r, i) => [r, i]));
    tabloCiz(p, [
      { k: 'tarih', ad: 'Tarih', v: r => r[0] + String(1e7 + sira.get(r)), h: r => trh(r[0]), x: r => trh(r[0]) }, ...(sirket ? [{ k: 'belge', ad: 'Belge No', v: r => +r[1] || r[1], h: r => esc(r[1]) || tire }] : []),
      { k: 'aciklama', ad: 'Açıklama', v: r => r[2], h: r => `<span title="${esc(r[3])}">${esc(r[2])}</span>`, s: 'metin' }, { k: 'tur', ad: 'Tür', v: r => r[7], h: r => `<span class="up-rz">${esc(r[7])}</span>` },
      { k: 'giris', ad: 'Giriş (€)', v: r => r[4], h: r => r[4] ? `<span class="giris">${paraM(r[4])}</span>` : tire, s: 'sayi', x: r => r[4] ? n2(r[4]) : '' }, { k: 'cikis', ad: 'Çıkış (€)', v: r => r[5], h: r => r[5] ? `<span class="cikis">${paraM(r[5])}</span>` : tire, s: 'sayi', x: r => r[5] ? n2(r[5]) : '' },
      { k: 'bakiye', ad: 'Bakiye (€)', v: r => r[6], h: r => para(r[6]), s: 'sayi', x: r => n2(r[6]) }, { k: 'asil', ad: 'Asıl Metin (İngilizce)', v: r => r[3], s: 'asil' }
    ], l, { tarih: `Toplam · ${l.length.toLocaleString(TR)} hareket`, giris: paraM(topla(l, r => r[4])), cikis: paraM(topla(l, r => r[5])), bakiye: `Net ${paraM(topla(l, r => r[4]) - topla(l, r => r[5]))}`, asil: `Hesap bakiyesi ${paraM(V.bakiye[p])}` },
      { baslik: HESAP[p], say: `${l.length.toLocaleString(TR)} / ${V[p].length.toLocaleString(TR)} hareket` });
  }
  function ongoruCiz() {
    const l = ongoruSuz(null), sira = new Map(V.ongoru.map((r, i) => [r, i])), son = V.ongoru.filter(r => r[5] != null).pop(), kalem = l.filter(r => !r[7]);
    tabloCiz('ongoru', [
      { k: 'sira', ad: 'Sıra', v: r => sira.get(r), h: r => sira.get(r) + 1, x: r => String(sira.get(r) + 1) }, { k: 'tarih', ad: 'Tarih', v: r => r[0] + String(1e5 + sira.get(r)), h: r => trh(r[0]), x: r => trh(r[0]) },
      { k: 'aciklama', ad: 'Açıklama', v: r => r[1], h: r => `<span title="${esc(r[2])}">${esc(r[1])}</span>`, s: 'metin' }, { k: 'tur', ad: 'Tür', v: r => r[6], h: r => r[7] ? `<span class="up-rz">Açılış Bakiyesi</span>` : `<span class="up-rz">${esc(r[6])}</span>` },
      { k: 'giris', ad: 'Giriş (€)', v: r => r[3], h: r => r[3] ? `<span class="giris">${paraM(r[3])}</span>` : tire, s: 'sayi', x: r => r[3] ? n2(r[3]) : '' }, { k: 'cikis', ad: 'Çıkış (€)', v: r => r[4], h: r => r[4] ? `<span class="cikis">${paraM(r[4])}</span>` : tire, s: 'sayi', x: r => r[4] ? n2(r[4]) : '' },
      { k: 'bakiye', ad: 'Bakiye (€)', v: r => r[5] ?? 0, h: r => r[5] == null ? tire : `<b>${para(r[5])}</b>`, s: 'sayi', x: r => r[5] == null ? '' : n2(r[5]) }, { k: 'asil', ad: 'Asıl Metin (İngilizce)', v: r => r[2], s: 'asil' }
    ], l, { sira: `Toplam · ${kalem.length} kalem`, giris: paraM(topla(kalem, r => r[3])), cikis: paraM(topla(kalem, r => r[4])), bakiye: son ? paraM(son[5]) : '', asil: son ? `Dönem sonu ${trh(son[0])}` : '' },
      { baslik: 'Ödeme Öngörüsü · Önümüzdeki Haftalar', say: `${kalem.length} kalem`, satirSinif: r => r[7] ? 'bakiye' : '' });
  }
  function ciz() {
    if (!V) return; acilir = []; kpiCiz();
    if (aktif === 'ozet') ozetCiz(); else if (aktif === 'gider') giderCiz(); else if (aktif === 'bakiye') bakiyeCiz(); else if (aktif === 'tahsilat') tahsilatCiz(); else if (aktif === 'satis') satisCiz(); else if (aktif === 'aylik') aylikCiz(); else if (aktif === 'musteri' || aktif === 'urun') kirilimCiz(aktif); else if (aktif === 'ongoru') ongoruCiz(); else hesapCiz(aktif);
  }
  function sekme(p) {
    aktif = p; $$('#upSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('.up-panel', kok).forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    if (SZ) { suzgecGorunum(); (GORUNEN[p] || []).forEach(k => SZ.buda(k, secenekler(k).map(s => s.v))); SZ.yaz(); }   // seçenek listesi sekmeye göre değişir: bu sekmede karşılığı olmayan seçimler düşer
    ciz();
    try { const u = new URL(location.href); if (p === 'ozet') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }

  // ---------- Excel: süzülmüş listenin tamamı (sayfalama yok); Özet'te ekrandaki kartlar ----------
  //   20 Kas 2026 (kullanıcı: "sekmeleri PDF veya Excel olarak yükleyebilelim"): aynı sayfa tanımı Excel (.xlsx) ve PDF için; çok kartlı sekmelerde (Özet, Aylık Giderler) ekrandaki bütün tablolar, liste sekmelerinde süzülmüş listenin tamamı
  function excel(bicim) {
    if (!window.TabloExcel) { mesaj('danger', 'Dışa aktarma bileşeni yüklenemedi; sayfayı yenileyin.'); return; }
    if (!V) return; const pdf = bicim === 'pdf';
    const duz = h => String(h ?? '').replace(/<small[^>]*>.*?<\/small>/g, '').replace(/<[^>]+>/g, '').replace(/ /g, ' ').replace(/&amp;/g, '&').trim(), ek = `${V.dosya} · son hareket ${trh(V.sonHareket)}`;
    let sayfalar;
    if (aktif === 'ozet' || aktif === 'gider') sayfalar = $$(aktif === 'ozet' ? '#upOzet table' : '#upGider table').map(t => ({ tablo: t, ad: t.closest('.up-kart').querySelector('.b').textContent.trim().slice(0, 28), baslik: 'Uras Portekiz Raporu · ' + t.closest('.up-kart').querySelector('.b').textContent.trim(), altBaslik: ek }));
    else {
      const t = $(`table[data-tablo="${aktif}"]`), e = t._excel; if (!e) return;
      const satirlar = e.satirlar.map(r => e.kolonlar.map(c => c.x ? c.x(r) : duz(c.v(r))));
      if (e.dip) satirlar.push(e.kolonlar.map(c => duz(e.dip[c.k]).replace(/\s*€/g, '')));
      sayfalar = [{ ad: (e.baslik || aktif).slice(0, 28), baslik: 'Uras Portekiz Raporu · ' + (e.baslik || ''), altBaslik: `${e.say || ''} · ${ek}`, basliklar: e.kolonlar.map(c => c.ad), satirlar }];
    }
    const satirSay = sayfalar.reduce((s, x) => s + (x.satirlar ? x.satirlar.length : 0), 0);
    if (pdf && satirSay > 3000 && !window.confirm(`Bu sekmede ${satirSay.toLocaleString(TR)} satır var; PDF çok sayfalı ve büyük olur. Süzgeçle daraltmak daha iyi olabilir. Yine de PDF oluşturulsun mu?`)) return;
    mesaj('info', (pdf ? 'PDF' : 'Excel') + ' hazırlanıyor…');
    (pdf ? window.TabloExcel.pdf : window.TabloExcel.indir)('uras_portekiz_' + aktif + '_' + (V.sonHareket || ''), sayfalar).then(() => { $('#upSonuc').style.display = 'none'; }).catch(e => mesaj('danger', (pdf ? 'PDF' : 'Excel') + ' indirilemedi: ' + e.message));
  }

  // ---------- başlat ----------
  async function yukle() {
    const bekle = $('#upSonuc'); bekle.className = 'alert py-2 px-3 mb-2 alert-info'; bekle.textContent = 'Rapor dosyası okunuyor… (ilk açılışta birkaç saniye sürebilir)'; bekle.style.display = 'block';
    let v; try { v = await fetch('/UrasPortekiz/Veri').then(r => r.json()); } catch (e) { mesaj('danger', 'Veri yüklenemedi: ' + e.message); return; }
    bekle.style.display = 'none';
    if (!v.success) { mesaj('warning', v.message || 'Veri yok.'); return; }
    V = v;
    $('#upDosya').textContent = v.dosya; $('#upTarih').textContent = `son hareket ${trh(v.sonHareket)}${v.raporTarihi ? ' · dosya tarihi ' + trh(v.raporTarihi) : ''} · tutarlar EUR`;
    if (!SZ) SZ = window.SuzgecCubuk.kur({ kap: $('#upSuzgec'), alanlar: ALANLAR, hizli: ALANLAR.map(a => a.k), secenekler, deger: (k, d) => k === 'ay' ? ayAd(d) : null, araYer: 'Belge, müşteri, ürün, açıklama ara…', degisti: () => { Object.values(durum).forEach(d => { d.s = 0; }); ciz(); } });
    sekme(aktif);
  }
  kok.addEventListener('click', e => {
    const b = e.target.closest('#upSekmeler button'); if (b) { sekme(b.dataset.p); return; }
    const th = e.target.closest('table[data-tablo] thead th[data-k]'); if (th) { const p = th.closest('table').dataset.tablo, d = D(p); if (d.k === th.dataset.k) d.y = -d.y; else { d.k = th.dataset.k; d.y = 1; } d.s = 0; ciz(); return; }
    const sf = e.target.closest('[data-sayfa] button'); if (sf) { D(aktif).s += +sf.dataset.s; ciz(); $(`table[data-tablo="${aktif}"]`).closest('section').scrollTop = 0; return; }
    const gy = e.target.closest('[data-gyil]'); if (gy) { giderYil = gy.dataset.gyil; ciz(); return; }
    const a = e.target.closest('a[data-ac]'); if (a) { e.preventDefault(); const fn = acilir[+a.dataset.ac]; if (fn) fn(); return; }
    const g = e.target.closest('a.up-git'); if (g) { e.preventDefault(); const f = g.dataset.f; if (f) { ALANLAR.forEach(a => { if (a.k !== 'yil') F()[a.k] = []; }); F()[f] = [g.dataset.v]; if (g.dataset.f2) F()[g.dataset.f2] = [g.dataset.v2]; F().ara = ''; } D(g.dataset.git).s = 0; sekme(g.dataset.git); }   // seçili yıl korunur, öbür süzgeçler temizlenir, tıklanan değer süzgeç olur
  });
  $('#upExcel').addEventListener('click', () => excel('xlsx')); $('#upPdf').addEventListener('click', () => excel('pdf'));
  $('#upYukle').addEventListener('change', async e => {
    const f = e.target.files[0]; if (!f) return; const fd = new FormData(); fd.append('dosya', f); mesaj('info', f.name + ' yükleniyor…');
    try { const j = await fetch('/UrasPortekiz/Yukle', { method: 'POST', body: fd }).then(r => r.json()); mesaj(j.success ? 'success' : 'danger', j.message); if (j.success) await yukle(); } catch (x) { mesaj('danger', 'Yüklenemedi: ' + x.message); }
    e.target.value = '';
  });
  window.addEventListener('resize', () => { clearTimeout(boyut.z); boyut.z = setTimeout(boyut, 120); });
  yukle();
})();
