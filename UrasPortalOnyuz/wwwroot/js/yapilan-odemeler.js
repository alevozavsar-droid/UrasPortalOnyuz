// ============================================================
// YAPILAN ÖDEMELER — ekran betiği (Views/YapilanOdemeler/Index.cshtml; veri /YapilanOdemeler/Veri; kaynaklar Data/YapilanOdemelerOrnek.cs)
// Tüm grup şirketlerinin ödemeleri (Banka / Çek / Nakit Kasa / Nakit Avans) tek listede: dönem + şirket + tür + kategori + alıcı tipi +
// para birimi + durum + kaynak filtreleri, KPI kutuları (tıklayınca tür filtresi), Şirket × Tür ve Ay × Tür konsolide matrisleri,
// kategori dağılımı, en büyük 10 alıcı, satır detay drawer'ı (kaynak belgeye bağlantı), Excel (CSV) çıktıları.
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Vade Bekliyor").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('yo'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const bh = s => { s = String(s ?? ''); return s ? s.charAt(0).toLocaleUpperCase('tr-TR') + s.slice(1) : s; };
  const tl = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const iso = d => d.toISOString().slice(0, 10);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const durumRozet = d => rozet(d, d === 'Ödendi' ? 'ok' : d === 'İptal' ? 'hata' : 'bekle');
  const turRozet = o => `<span class="yo-tur ${esc(o.kanal)}">${esc(o.tur)}</span>`;
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const sekmeli = kok.dataset.sekme === '1';
  const KANALLAR = ['Banka', 'Cek', 'Kasa', 'Avans'];
  let V = null, L = [];
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 8000); }
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + iso(new Date()) + '.csv'; a.click(); };
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '').replace(',', ',');

  // ---- dönem
  function aralik() {
    const d = $('#yoDonem').value, bugun = new Date(V.bugun + 'T00:00:00');
    const ayBas = new Date(bugun.getFullYear(), bugun.getMonth(), 1);
    if (d === 'buay') return [iso(ayBas), V.bugun];
    if (d === 'gecenay') { const b = new Date(bugun.getFullYear(), bugun.getMonth() - 1, 1), e = new Date(bugun.getFullYear(), bugun.getMonth(), 0); return [iso(b), iso(e)]; }
    if (d === '30' || d === '90') { const b = new Date(bugun); b.setDate(b.getDate() - Number(d) + 1); return [iso(b), V.bugun]; }
    if (d === 'yil') return [bugun.getFullYear() + '-01-01', V.bugun];
    if (d === 'ozel') return [$('#yoBas').value || '', $('#yoBit').value || ''];
    return ['', ''];
  }
  function donemDegisti() { const ozel = $('#yoDonem').value === 'ozel'; $('#yoBas').disabled = $('#yoBit').disabled = !ozel; if (!ozel) { const [b, e] = aralik(); $('#yoBas').value = b; $('#yoBit').value = e; } ciz(); }

  // ---- filtre
  function liste() {
    const ara = ($('#yoAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value, [bas, bit] = aralik();
    return V.odemeler.filter(o => (!bas || o.tarih >= bas) && (!bit || o.tarih <= bit) && (!v('#yoSirket') || o.sirket === v('#yoSirket')) && (!v('#yoTur') || o.kanal === v('#yoTur')) && (!v('#yoKategori') || o.kategori === v('#yoKategori'))
      && (!v('#yoAliciTipi') || o.aliciTipi === v('#yoAliciTipi')) && (!v('#yoPb') || o.paraBirimi === v('#yoPb')) && (!v('#yoDurum') || o.durum === v('#yoDurum')) && (!v('#yoKaynak') || o.kaynak === v('#yoKaynak'))
      && (!ara || [o.no, o.alici, o.belge, o.aciklama, o.kaynakHesap, o.banka, o.sirket, o.kategori].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  const gecerli = o => o.durum !== 'İptal';
  const topla = l => l.filter(gecerli).reduce((s, o) => s + Number(o.tutarTl), 0);

  // ---- çizim
  function ciz() {
    L = liste();
    const g = L.filter(gecerli), kanal = k => g.filter(o => o.kanal === k);
    const vadeli = L.filter(o => o.durum === 'Vade Bekliyor');
    const kpi = [['#0f172a', 'Toplam Ödeme', kisa(topla(g)) + ' TL', `${g.length} İşlem · ${new Set(g.map(o => o.sirket)).size} Şirket`, ''],
      ['#1d4ed8', 'Banka (Havale / EFT)', kisa(topla(kanal('Banka'))) + ' TL', `${kanal('Banka').length} İşlem`, 'Banka'],
      ['#b45309', 'Çek', kisa(topla(kanal('Cek'))) + ' TL', `${kanal('Cek').length} Çek · ${vadeli.length} Vade Bekliyor`, 'Cek'],
      ['#0f766e', 'Nakit (Kasa)', kisa(topla(kanal('Kasa'))) + ' TL', `${kanal('Kasa').length} Kasa Fişi`, 'Kasa'],
      ['#7c3aed', 'Nakit Avans', kisa(topla(kanal('Avans'))) + ' TL', `${kanal('Avans').length} Teslim`, 'Avans'],
      ['#dc2626', 'Vade Bekleyen Çek', kisa(topla(vadeli)) + ' TL', 'Henüz Bankadan Çıkmadı', 'vade']];
    $('#yoKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}" data-k="${k[4]}" title="Tıklayınca filtre uygular"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(bh(k[3]))}</div></div>`).join('');
    $$('#yoKpi > div').forEach(d => d.addEventListener('click', () => { const k = d.dataset.k; if (k === 'vade') { $('#yoTur').value = 'Cek'; $('#yoDurum').value = 'Vade Bekliyor'; } else { $('#yoTur').value = k; if (k) $('#yoDurum').value = ''; } ciz(); }));
    // tablo
    $('#yoTablo tbody').innerHTML = L.map(o => `<tr class="tik" data-id="${o.id}"><td>${esc(o.tarihTr)}</td><td><b>${esc(o.no)}</b></td><td>${esc(o.sirket)}</td><td>${turRozet(o)}</td><td>${esc(o.kaynakHesap)}${o.banka && !o.kaynakHesap.includes(o.banka) ? `<small>${esc(o.banka)}</small>` : ''}</td><td><b>${esc(o.alici)}</b><small>${esc(o.aliciTipi)}</small></td><td>${esc(o.kategori)}</td><td>${bos(o.belge)}</td><td class="s">${tl(o.tutar)}</td><td>${esc(o.paraBirimi)}</td><td class="s"><b>${tl(o.tutarTl)}</b>${o.paraBirimi !== 'TL' ? `<small>kur ${tl(o.kur)}</small>` : ''}</td><td>${bos(o.vadeTr)}</td><td>${durumRozet(o.durum)}</td><td><small>${esc(o.kaynak)}</small></td></tr>`).join('') || '<tr><td colspan="14" class="na-bos">Filtreye uyan ödeme yok.</td></tr>';
    $('#yoTopSayi').textContent = g.length + ' İşlem'; $('#yoTopTl').textContent = tl(topla(g)) + ' TL';
    $('#yoSayi').textContent = `${L.length} / ${V.odemeler.length} ödeme · ${L.length - g.length} iptal (toplama girmez) · satıra tıklayınca ödeme detayı ve kaynak belge açılır`;
    $$('#yoTablo tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    // şirket × tür
    const matris = (anahtar, sirala) => { const m = {}; g.forEach(o => { const k = anahtar(o); const p = m[k] || (m[k] = { n: 0, top: 0, Banka: 0, Cek: 0, Kasa: 0, Avans: 0 }); p[o.kanal] += Number(o.tutarTl); p.top += Number(o.tutarTl); p.n++; }); return Object.keys(m).sort(sirala(m)).map(k => [k, m[k]]); };
    const satir = ([k, p], kalın) => `<tr class="${kalın ? 'toplam' : ''}"><td>${esc(k)}</td>${KANALLAR.map(c => `<td class="s">${p[c] ? tl(p[c]) : '<span class="text-muted">—</span>'}</td>`).join('')}<td class="s"><b>${tl(p.top)}</b></td><td class="s">${p.n}</td></tr>`;
    const toplamSatir = rows => { const t = { n: 0, top: 0, Banka: 0, Cek: 0, Kasa: 0, Avans: 0 }; rows.forEach(([, p]) => { KANALLAR.forEach(c => t[c] += p[c]); t.top += p.top; t.n += p.n; }); return t; };
    const s1 = matris(o => o.sirket, m => (a, b) => m[b].top - m[a].top);
    $('#yoMatris tbody').innerHTML = s1.length ? s1.map(r => satir(r)).join('') + satir(['Konsolide Toplam', toplamSatir(s1)], true) : '<tr><td colspan="7" class="na-bos">Kayıt yok.</td></tr>';
    const s2 = matris(o => o.tarih.slice(0, 7), () => (a, b) => b.localeCompare(a));
    const ayAdi = k => new Date(k + '-01T00:00:00').toLocaleDateString('tr-TR', { month: 'long', year: 'numeric' });
    $('#yoAylik tbody').innerHTML = s2.length ? s2.map(([k, p]) => satir([bh(ayAdi(k)), p])).join('') + satir(['Toplam', toplamSatir(s2)], true) : '<tr><td colspan="7" class="na-bos">Kayıt yok.</td></tr>';
    // kategori
    const kat = {}; g.forEach(o => { kat[o.kategori] = kat[o.kategori] || { top: 0, n: 0 }; kat[o.kategori].top += Number(o.tutarTl); kat[o.kategori].n++; });
    const kl = Object.keys(kat).sort((a, b) => kat[b].top - kat[a].top), maks = kl.length ? kat[kl[0]].top : 1, genel = topla(g) || 1;
    $('#yoKategoriler').innerHTML = kl.map(k => `<div class="yo-bar"><span title="${esc(k)}">${esc(k)}<small class="text-muted"> · ${kat[k].n}</small></span><span class="b"><i style="width:${Math.max(2, kat[k].top / maks * 100)}%"></i></span><span class="v">${kisa(kat[k].top)} <small>%${(kat[k].top / genel * 100).toFixed(1)}</small></span></div>`).join('') || '<div class="na-bos">Kayıt yok.</div>';
    // en büyük alıcılar
    const al = {}; g.forEach(o => { const p = al[o.alici] || (al[o.alici] = { tip: o.aliciTipi, top: 0, n: 0 }); p.top += Number(o.tutarTl); p.n++; });
    $('#yoAlicilar tbody').innerHTML = Object.keys(al).sort((a, b) => al[b].top - al[a].top).slice(0, 10).map((a, i) => `<tr><td>${i + 1}</td><td><b>${esc(a)}</b></td><td>${esc(al[a].tip)}</td><td class="s">${al[a].n}</td><td class="s">${tl(al[a].top)}</td></tr>`).join('') || '<tr><td colspan="5" class="na-bos">Kayıt yok.</td></tr>';
  }

  // ---- detay drawer
  function ac(url, baslik) { if (window.top && window.top !== window && typeof window.top.portalSekmeAc === 'function') window.top.portalSekmeAc(baslik, url); else window.open(url + (sekmeli ? (url.includes('?') ? '&' : '?') + '_sekme=1' : ''), '_blank'); }
  function drawerAc(id) {
    const o = V.odemeler.find(x => x.id === id); if (!o) return;
    $('#yoDrawerBaslik').innerHTML = `<i class="fas fa-money-bill-transfer me-1"></i>${esc(o.no)} · ${esc(o.sirket)} ${durumRozet(o.durum)}`;
    $('#yoDrawerAksiyon').innerHTML = o.link ? `<button type="button" class="btn btn-light btn-sm py-0" id="yoKaynakAc" style="font-size:.66rem"><i class="fas fa-arrow-up-right-from-square me-1"></i>${o.kanal === 'Avans' ? 'Avans Talebini Aç' : 'Kaynak Ekranı Aç'}</button>` : '';
    const a = [['Ödeme No', o.no], ['Tarih', o.tarihTr], ['Şirket', o.sirket], ['Ödeme Türü', o.tur], ['Kaynak Hesap', o.kaynakHesap], ['Banka', o.banka], ['Alıcı', o.alici], ['Alıcı Tipi', o.aliciTipi], ['Kategori', o.kategori],
      ['Tutar', tl(o.tutar) + ' ' + o.paraBirimi], ['Kur', o.paraBirimi === 'TL' ? '1,00' : tl(o.kur)], ['Tutar (TL)', tl(o.tutarTl) + ' TL'], ['Belge', o.belge], ['Vade', o.vadeTr], ['Durum', o.durum], ['Kaynak Sistem', o.kaynak], ['Onaylayan', o.onaylayan], ['İşlemi Yapan', o.islemiYapan]];
    $('#yoDrawerIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-receipt me-1"></i>Ödeme Bilgileri</span><span class="not">${esc(o.kaynak)}</span></div><div class="na-kart-g"><div class="yo-master">${a.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}<div style="grid-column:1/-1"><div class="l">Açıklama</div><div class="v" style="font-weight:500">${bos(o.aciklama)}</div></div></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-building me-1"></i>${esc(o.sirket)} · Aynı Alıcıya Yapılan Ödemeler</span><span class="not">Tüm dönem</span></div><div class="na-kart-g p-0"><div class="table-responsive" style="max-height:260px"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Ödeme No</th><th>Tür</th><th>Belge</th><th class="s">Tutar (TL)</th><th>Durum</th></tr></thead><tbody>${V.odemeler.filter(x => x.alici === o.alici && x.sirket === o.sirket).map(x => `<tr class="${x.id === o.id ? 'table-active' : ''}"><td>${esc(x.tarihTr)}</td><td>${esc(x.no)}</td><td>${turRozet(x)}</td><td>${bos(x.belge)}</td><td class="s">${tl(x.tutarTl)}</td><td>${durumRozet(x.durum)}</td></tr>`).join('')}</tbody></table></div></div></div>`;
    const b = $('#yoKaynakAc'); if (b) b.addEventListener('click', () => ac(o.link, o.kanal === 'Avans' ? 'Avans Talebi ' + o.belge : 'Aylık Alım Onay Ekranı'));
    $('#yoDrawer').classList.add('acik'); $('#yoPerde').classList.add('acik');
  }
  function drawerKapat() { $('#yoDrawer').classList.remove('acik'); $('#yoPerde').classList.remove('acik'); }

  // ---- excel
  function excel() {
    csvIndir('yapilan_odemeler', ['Tarih', 'Ödeme No', 'Şirket', 'Ödeme Türü', 'Kaynak Hesap', 'Banka', 'Alıcı', 'Alıcı Tipi', 'Kategori', 'Belge', 'Tutar', 'Para Birimi', 'Kur', 'Tutar (TL)', 'Vade', 'Durum', 'Kaynak', 'Onaylayan', 'İşlemi Yapan', 'Açıklama'],
      L.map(o => [o.tarihTr, o.no, o.sirket, o.tur, o.kaynakHesap, o.banka, o.alici, o.aliciTipi, o.kategori, o.belge, sayi(o.tutar), o.paraBirimi, sayi(o.kur), sayi(o.tutarTl), o.vadeTr || '', o.durum, o.kaynak, o.onaylayan, o.islemiYapan, o.aciklama]));
  }
  function matrisExcel() {
    const rows = $$('#yoMatris tbody tr').map(tr => $$('td', tr).map(td => td.textContent.trim().replace('—', '')));
    csvIndir('yapilan_odemeler_sirket_ozeti', ['Şirket', 'Banka (Havale / EFT)', 'Çek', 'Nakit (Kasa)', 'Nakit Avans', 'Toplam', 'İşlem'], rows);
  }

  // ---- başlat
  fetch('/YapilanOdemeler/Veri').then(r => r.json()).then(v => {
    V = v;
    const doldur = (id, l, deger) => { $(id).innerHTML = '<option value="">Tümü</option>' + l.map(x => `<option value="${esc(deger ? deger(x) : x)}">${esc(deger ? x.ad : x)}</option>`).join(''); };
    doldur('#yoTur', V.turler, t => t.kanal); doldur('#yoKategori', V.kategoriler); doldur('#yoAliciTipi', V.aliciTipleri); doldur('#yoPb', V.paraBirimleri); doldur('#yoDurum', V.durumlar); doldur('#yoKaynak', V.kaynaklar);
    ['#yoAra', '#yoBas', '#yoBit', '#yoSirket', '#yoTur', '#yoKategori', '#yoAliciTipi', '#yoPb', '#yoDurum', '#yoKaynak'].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    $('#yoDonem').addEventListener('change', donemDegisti);
    $('#yoTemizle').addEventListener('click', () => { $('#yoAra').value = ''; ['#yoSirket', '#yoTur', '#yoKategori', '#yoAliciTipi', '#yoPb', '#yoDurum', '#yoKaynak'].forEach(s => $(s).value = ''); $('#yoDonem').value = '90'; donemDegisti(); });
    $('#yoExcel').addEventListener('click', excel); $('#yoMatrisExcel').addEventListener('click', matrisExcel);
    $('#yoDrawerKapat').addEventListener('click', drawerKapat); $('#yoPerde').addEventListener('click', drawerKapat);
    donemDegisti(); window.NA_HAZIR = true;
  }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
