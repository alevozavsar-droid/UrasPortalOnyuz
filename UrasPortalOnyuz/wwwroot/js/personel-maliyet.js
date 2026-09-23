// ============================================================
// PERSONEL MALİYET RAPORU — ekran betiği (Views/PersonelMaliyet/Index.cshtml; veri /PersonelMaliyet/Veri; kayıt Kaydet / Ekle / Sil)
// "PERSONEL 19.06 GÜNCEL" tablosu. Ortak: filtreler (durum, SGK işvereni, fiili görev yeri, çatı departman, bölüm, ünvan, özel durum, arama), KPI kutuları.
// Sekme "Personel Maliyet Tablosu": kimlik + brüt ücret X/R + işveren payı + ek çalışma + sosyal yardımlar + prim + yan haklar + yasal ödemeler + genel maliyet + not.
//   Düzenle modunda kimlik bilgileri (liste + serbest yazım) ve tüm tutarlar satır bazında değiştirilir; Kaydet toplu kaydeder. Her satırda Kart (Personel Cari
//   Kartı çekmecesi: 335.xxx cari kodu, durum, ayrılış, TC kimlik, telefon, e-posta, ücret ve maliyet kalemleri, not — doğrudan kaydeder / siler) ve Sil.
//   Personel Ekle yeni kart açar. Excel (CSV).
// Sekme "Özet Raporu": seçilebilir kırılım × tüm maliyet kalemleri, SGK işvereni × çatı departman matrisi, maliyet kalemi dağılımı,
//   ünvan / özel durum / kıdem dağılımları, en yüksek maliyetli 10 personel (satır → kart), Özet Excel.
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Özel Durumu Yok", "İşten Ayrıldı", "10 Yıl ve Üzeri").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('pm'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const hucre = v => Number(v) ? kisa(v) : '<span class="text-muted">—</span>';
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const pct = (a, b) => b ? (a / b * 100).toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) : '0,0';
  const trTarih = iso => iso ? iso.split('-').reverse().join('.') : '';
  const GIRIS = ['xNet', 'rNet', 'brutUcMasraflari', 'topIsvPayi', 'topEkCalisma', 'yemek', 'yol', 'servis', 'kiraDestegi', 'egitim', 'satisPrimi', 'ozelOdeme', 'topYanHaklar', 'topYasalOd'];
  const METIN = ['sgkIsvereni', 'fiiliGorevYeri', 'adSoyad', 'unvan', 'bolum', 'gorev', 'iseGiris', 'catiDepartman', 'ozelDurum', 'durum', 'ayrilisTarihi', 'tcKimlik', 'telefon', 'eposta', 'not'];
  const KALEM = [['xNet', 'X-Net'], ['rNet', 'R-Net'], ['topNet', 'Top. Net'], ['brutUcMasraflari', 'Brüt Üc. Masr.'], ['toplamBrutUcret', 'Toplam Brüt'], ['topIsvPayi', 'İşv. Payı'], ['topEkCalisma', 'Ek Çalışma'], ['topSosyal', 'Sosyal Yrd.'], ['topPrim', 'Prim'], ['topYanHaklar', 'Yan Haklar'], ['topYasalOd', 'Yasal Öd.'], ['genelMaliyet', 'Genel Maliyet']];
  const TUTAR_ETIKET = { xNet: 'X-Net', rNet: 'R-Net', brutUcMasraflari: 'Brüt Üc. Masrafları', topIsvPayi: 'Top. İşv. Payı', topEkCalisma: 'Top. Ek Çalışma', yemek: 'Yemek', yol: 'Yol', servis: 'Servis', kiraDestegi: 'Kira Desteği', egitim: 'Eğitim', satisPrimi: 'Satış Primi', ozelOdeme: 'Özel Ödeme', topYanHaklar: 'Top. Yan Haklar', topYasalOd: 'Top. Yasal Öd.' };
  const KIRILIM_AD = { catiDepartman: 'Çatı Departman', sgkIsvereni: 'SGK İşvereni', fiiliGorevYeri: 'Fiili Görev Yeri', bolum: 'Bölüm', unvan: 'Ünvan', gorev: 'Görev', ozelDurum: 'Özel Durumu', kidemAralik: 'Kıdem Aralığı' };
  const KIDEM_SIRA = ['1 Yıldan Az', '1–3 Yıl', '3–5 Yıl', '5–10 Yıl', '10 Yıl ve Üzeri', 'Tarih Yok'];
  const kidemAralik = p => p.kidemYil === null || p.kidemYil === undefined ? 'Tarih Yok' : p.kidemYil < 1 ? '1 Yıldan Az' : p.kidemYil < 3 ? '1–3 Yıl' : p.kidemYil < 5 ? '3–5 Yıl' : p.kidemYil < 10 ? '5–10 Yıl' : '10 Yıl ve Üzeri';
  const kirilimDeger = (p, k) => k === 'kidemAralik' ? kidemAralik(p) : k === 'ozelDurum' ? (p.ozelDurum || 'Özel Durumu Yok') : (p[k] || '—');
  let V = null, L = [], duzenleme = false, taslak = {}, aktifSekme = 'tablo', kartSira = null;   // taslak: sira → { alan: değer }; kartSira: açık kart (0 = yeni)
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 8000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  async function post(url, veri) { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }); return r.json(); }
  const csvIndir = (ad, satirlar) => { const t = satirlar.map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + new Date().toISOString().slice(0, 10) + '.csv'; a.click(); };
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');

  // ---- sekmeler
  function sekme(p) {
    aktifSekme = p; $$('#pmSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#pm > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    if (V && p === 'ozet') cizOzet();
    try { const u = new URL(location.href); if (p === 'tablo') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }

  // ---- hesap (sunucudaki kuralla aynı)
  function hesapla(p) {
    const n = k => Number(p[k]) || 0;
    p.topNet = n('xNet') + n('rNet'); p.toplamBrutUcret = p.topNet + n('brutUcMasraflari');
    p.topSosyal = n('yemek') + n('yol') + n('servis') + n('kiraDestegi') + n('egitim'); p.topPrim = n('satisPrimi') + n('ozelOdeme');
    p.genelMaliyet = p.toplamBrutUcret + n('topIsvPayi') + n('topEkCalisma') + p.topSosyal + p.topPrim + n('topYanHaklar') + n('topYasalOd');
    return p;
  }
  const etkin = p => duzenleme && taslak[p.sira] ? hesapla({ ...p, ...taslak[p.sira] }) : p;
  const bul = sira => V.personel.find(x => x.sira === sira);
  /** sunucuya gidecek tam satır (kimlik + tutarlar + not) */
  function satirVeri(p) { const d = { sira: p.sira }; METIN.forEach(k => d[k] = p[k] ?? ''); GIRIS.forEach(k => d[k] = Number(p[k]) || 0); return d; }

  // ---- filtre
  function liste(durumYoksay) {
    const ara = ($('#pmAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value, oz = v('#pmOzel'), du = durumYoksay ? '' : v('#pmDurum');
    return V.personel.filter(p => (!du || (p.durum || 'Aktif') === du) && (!v('#pmSgk') || p.sgkIsvereni === v('#pmSgk')) && (!v('#pmYer') || p.fiiliGorevYeri === v('#pmYer')) && (!v('#pmCati') || p.catiDepartman === v('#pmCati')) && (!v('#pmBolum') || p.bolum === v('#pmBolum')) && (!v('#pmUnvan') || p.unvan === v('#pmUnvan'))
      && (!oz || (oz === '_yok' ? !p.ozelDurum : p.ozelDurum === oz))
      && (!ara || [p.adSoyad, p.cariKodu, p.gorev, p.bolum, p.unvan, p.not, p.sgkIsvereni, p.fiiliGorevYeri, p.catiDepartman, p.tcKimlik, p.eposta].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  const toplam = (l, k) => l.reduce((s, p) => s + (Number(p[k]) || 0), 0);

  // ---- seçim listeleri (filtreler + datalist'ler; seçili değer korunur)
  function listeleriDoldur() {
    const doldur = (id, l, ek) => { const e = $(id), v = e.value; e.innerHTML = '<option value="">Tümü</option>' + (ek || '') + l.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); e.value = l.includes(v) || v === '_yok' ? v : ''; };
    doldur('#pmSgk', V.sgkIsverenleri); doldur('#pmYer', V.gorevYerleri); doldur('#pmCati', V.catiDepartmanlar); doldur('#pmBolum', V.bolumler); doldur('#pmUnvan', V.unvanlar); doldur('#pmOzel', V.ozelDurumlar, '<option value="_yok">Özel Durumu Yok</option>');
    const dl = (id, l) => { $(id).innerHTML = l.map(x => `<option value="${esc(x)}">`).join(''); };
    dl('#dlSgk', V.sgkIsverenleri); dl('#dlYer', V.gorevYerleri); dl('#dlUnvan', V.unvanlar); dl('#dlBolum', V.bolumler); dl('#dlCati', V.catiDepartmanlar); dl('#dlGorev', V.gorevler || []);
  }
  /** sunucu yanıtındaki güncel veriyi alır ve ekranı tazeler */
  function veriGuncelle(j) { ['personel', 'sonKayit', 'sgkIsverenleri', 'gorevYerleri', 'catiDepartmanlar', 'bolumler', 'unvanlar', 'gorevler', 'ozelDurumlar', 'durumlar'].forEach(k => { if (j[k] !== undefined) V[k] = j[k]; }); listeleriDoldur(); kayitBilgi(); ciz(); }

  // ---- ortak: KPI + dip toplam
  function kpiCiz() {
    const n = L.length, top = k => toplam(L, k);
    const ayrilan = liste(true).filter(p => p.durum === 'İşten Ayrıldı').length, du = $('#pmDurum').value;   // Durum filtresinden bağımsız gerçek ayrılan sayısı
    const kpi = [['#0f172a', 'Personel', n, `${L.filter(p => p.ozelDurum === 'Emekli').length} Emekli · ${L.filter(p => p.ozelDurum === 'Engelli').length} Engelli · ${ayrilan} Ayrılan${du === 'Aktif' && ayrilan ? ' (Filtre Dışı)' : ''}`], ['#0f766e', 'Toplam Net Ücret', kisa(top('topNet')) + ' TL', `X ${kisa(top('xNet'))} · R ${kisa(top('rNet'))}`],
      ['#1d4ed8', 'Toplam Brüt Ücret', kisa(top('toplamBrutUcret')) + ' TL', `İşveren Payı ${kisa(top('topIsvPayi'))}`], ['#b45309', 'Sosyal Yardımlar', kisa(top('topSosyal')) + ' TL', `Yemek ${kisa(top('yemek'))} · Yol ${kisa(top('yol'))}`],
      ['#7c3aed', 'Primler', kisa(top('topPrim')) + ' TL', `Satış Primi ${kisa(top('satisPrimi'))} · Özel Ödeme ${kisa(top('ozelOdeme'))}`], ['#dc2626', 'Genel Maliyet', kisa(top('genelMaliyet')) + ' TL', `Ortalama ${kisa(n ? top('genelMaliyet') / n : 0)} TL / Kişi`]];
    $('#pmKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $('#pmNTablo').textContent = n;
  }
  function dipToplam() {
    const top = k => toplam(L, k), t = k => `<td class="s">${kisa(top(k))}</td>`;
    $('#pmToplam').innerHTML = `<td colspan="3">Toplam</td><td>${L.length} Personel</td><td colspan="7"></td>${t('xNet')}${t('rNet')}${t('topNet')}${t('brutUcMasraflari')}${t('toplamBrutUcret')}${t('topIsvPayi')}${t('topEkCalisma')}${t('yemek')}${t('yol')}${t('servis')}${t('kiraDestegi')}${t('egitim')}${t('topSosyal')}${t('satisPrimi')}${t('ozelOdeme')}${t('topPrim')}${t('topYanHaklar')}${t('topYasalOd')}<td class="s genel">${kisa(top('genelMaliyet'))}</td><td></td><td></td>`;
    $('#pmSayi').textContent = `${L.length} / ${V.personel.length} personel · aylık tutarlar TL · ${duzenleme ? Object.keys(taslak).length + ' satırda değişiklik var, Kaydet ile saklanır' : 'Düzenle: tabloda toplu düzeltme · Kart: personelin cari kartı'}`;
  }

  // ---- sekme 1 çizim
  function ciz() {
    L = liste().map(etkin); kpiCiz();
    const al = (p, etiket) => `aria-label="${esc(etiket)} · ${esc(p.adSoyad || 'Yeni')}"`;   // ekran okuyucu: sütun adı + personel
    const say = (p, k) => duzenleme ? `<input class="gir" type="number" min="0" step="1" data-sira="${p.sira}" data-alan="${k}" value="${Number(p[k]) || 0}" ${al(p, TUTAR_ETIKET[k])}>` : hucre(p[k]);
    const metin = (p, k, cls, liste, etiket) => duzenleme ? `<input class="gir ${cls}" data-sira="${p.sira}" data-alan="${k}" value="${esc(p[k] ?? '')}" ${liste ? `list="${liste}"` : ''} maxlength="120" ${al(p, etiket)}>` : esc(p[k]);
    const tarih = (p, k, etiket) => duzenleme ? `<input class="gir tarih" type="date" data-sira="${p.sira}" data-alan="${k}" value="${esc(p[k] || '')}" ${al(p, etiket)}>` : bos(p[k + 'Tr']);
    const sec = (p, k, l, etiket) => duzenleme ? `<select class="gir" data-sira="${p.sira}" data-alan="${k}" ${al(p, etiket)}>${l.map(o => `<option value="${esc(o[0])}" ${(p[k] || '') === o[0] ? 'selected' : ''}>${esc(o[1])}</option>`).join('')}</select>` : bos(p[k]);
    const not = p => duzenleme ? `<input class="gir not" data-sira="${p.sira}" data-alan="not" value="${esc(p.not || '')}" maxlength="200" ${al(p, 'Not')}>` : `<small>${bos(p.not)}</small>`;
    $('#pmTablo tbody').innerHTML = L.map(p => `<tr class="${p.ozelDurum === 'Emekli' ? 'emekli' : p.ozelDurum === 'Engelli' ? 'engelli' : ''} ${p.durum === 'İşten Ayrıldı' ? 'ayrildi' : ''} ${taslak[p.sira] ? 'degisti' : ''}" data-sira="${p.sira}">
      <td class="grupBas kucuk" title="${esc(p.cariKodu)}">${p.sira}</td><td>${metin(p, 'sgkIsvereni', 'dar', 'dlSgk', 'SGK İşvereni')}</td><td>${metin(p, 'fiiliGorevYeri', 'dar', 'dlYer', 'Fiili Görev Yeri')}</td><td class="ad">${metin(p, 'adSoyad', 'metin', '', 'Ad Soyad')}</td><td>${metin(p, 'unvan', 'dar', 'dlUnvan', 'Ünvan')}</td><td>${metin(p, 'bolum', 'dar', 'dlBolum', 'Bölüm')}</td><td>${metin(p, 'gorev', 'genis', 'dlGorev', 'Görev')}</td><td class="kucuk">${tarih(p, 'iseGiris', 'İşe Giriş')}</td><td class="s kucuk">${p.kidemYil === null || p.kidemYil === undefined ? '—' : p.kidemYil.toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}</td><td>${metin(p, 'catiDepartman', 'dar', 'dlCati', 'Çatı Departman')}</td><td class="kucuk">${sec(p, 'ozelDurum', [['', '—'], ['Emekli', 'Emekli'], ['Engelli', 'Engelli']], 'Özel Durumu')}</td>
      <td class="s grupBas">${say(p, 'xNet')}</td><td class="s">${say(p, 'rNet')}</td><td class="s hesap">${hucre(p.topNet)}</td><td class="s">${say(p, 'brutUcMasraflari')}</td><td class="s hesap">${hucre(p.toplamBrutUcret)}</td>
      <td class="s grupBas">${say(p, 'topIsvPayi')}</td><td class="s grupBas">${say(p, 'topEkCalisma')}</td>
      <td class="s grupBas">${say(p, 'yemek')}</td><td class="s">${say(p, 'yol')}</td><td class="s">${say(p, 'servis')}</td><td class="s">${say(p, 'kiraDestegi')}</td><td class="s">${say(p, 'egitim')}</td><td class="s hesap">${hucre(p.topSosyal)}</td>
      <td class="s grupBas">${say(p, 'satisPrimi')}</td><td class="s">${say(p, 'ozelOdeme')}</td><td class="s hesap">${hucre(p.topPrim)}</td>
      <td class="s grupBas">${say(p, 'topYanHaklar')}</td><td class="s grupBas">${say(p, 'topYasalOd')}</td>
      <td class="s genel grupBas">${kisa(p.genelMaliyet)}</td><td class="grupBas">${not(p)}</td>
      <td class="grupBas"><button type="button" class="btn btn-light btn-sm kart" data-sira="${p.sira}" title="Personel Cari Kartı ${esc(p.cariKodu)}"><i class="fas fa-id-card me-1"></i>Kart</button>${duzenleme ? ` <button type="button" class="btn btn-light btn-sm sil" data-sira="${p.sira}" title="Satırı sil"><i class="fas fa-trash"></i></button>` : ''}</td></tr>`).join('') || '<tr><td colspan="32" class="na-bos">Filtreye uyan personel yok.</td></tr>';
    dipToplam();
    if (duzenleme) $$('#pmTablo .gir').forEach(i => i.addEventListener('input', () => { const s = +i.dataset.sira, a = i.dataset.alan; taslak[s] = taslak[s] || {}; taslak[s][a] = GIRIS.includes(a) ? (Number(i.value) || 0) : i.value; satirGuncelle(s, GIRIS.includes(a)); }));
    if (aktifSekme === 'ozet') cizOzet();
  }
  /** düzenleme modunda tek satırın hesaplanan hücrelerini ve dip toplamı tazeler (girişleri yeniden çizmeden) */
  function satirGuncelle(sira, tutarMi) {
    const tr = $(`#pmTablo tr[data-sira="${sira}"]`); if (!tr) return; tr.classList.add('degisti');
    if (tutarMi) { const p = hesapla({ ...bul(sira), ...taslak[sira] }); const td = $$('td', tr); td[13].innerHTML = hucre(p.topNet); td[15].innerHTML = hucre(p.toplamBrutUcret); td[23].innerHTML = hucre(p.topSosyal); td[26].innerHTML = hucre(p.topPrim); td[29].innerHTML = kisa(p.genelMaliyet); }
    L = liste().map(etkin); kpiCiz(); dipToplam();
  }

  // ---- sekme 2: özet raporu
  function grupla(l, anahtar) { const m = {}; l.forEach(p => { const k = kirilimDeger(p, anahtar); const o = m[k] || (m[k] = { ad: k, n: 0 }); o.n++; KALEM.forEach(([f]) => o[f] = (o[f] || 0) + (Number(p[f]) || 0)); }); return m; }
  function cizOzet() {
    const k = $('#pmKirilim').value, genel = toplam(L, 'genelMaliyet');
    $('#pmKirilimBaslik').textContent = KIRILIM_AD[k]; $('#pmKirilimTh').textContent = KIRILIM_AD[k];
    const m = grupla(L, k);
    const keys = k === 'kidemAralik' ? KIDEM_SIRA.filter(x => m[x]) : Object.keys(m).sort((a, b) => m[b].genelMaliyet - m[a].genelMaliyet);
    const satir = (o, cls) => `<tr class="${cls || ''}"><td class="ad">${esc(o.ad)}</td><td class="s">${o.n}</td>${KALEM.map(([f]) => `<td class="s ${f === 'genelMaliyet' ? 'genel' : ''}">${hucre(o[f])}</td>`).join('')}<td class="s">${kisa(o.n ? o.genelMaliyet / o.n : 0)}</td><td class="s">${pct(o.genelMaliyet, genel)}</td></tr>`;
    const T = { ad: 'Toplam', n: L.length }; KALEM.forEach(([f]) => T[f] = toplam(L, f));
    $('#pmKirilimTablo tbody').innerHTML = keys.length ? keys.map(x => satir(m[x])).join('') + satir(T, 'toplam') : '<tr><td colspan="16" class="na-bos">Filtreye uyan personel yok.</td></tr>';
    const sgkler = Object.keys(grupla(L, 'sgkIsvereni')).sort((a, b) => L.filter(p => p.sgkIsvereni === b).length - L.filter(p => p.sgkIsvereni === a).length);
    const catilar = Object.keys(grupla(L, 'catiDepartman')).sort((a, b) => L.filter(p => p.catiDepartman === b).length - L.filter(p => p.catiDepartman === a).length);
    $('#pmMatris thead').innerHTML = `<tr><th>SGK İşvereni</th>${catilar.map(c => `<th class="s">${esc(c)}</th>`).join('')}<th class="s">Toplam</th></tr>`;
    const h = l => l.length ? `<span class="n">${l.length}</span><small>${kisa(toplam(l, 'genelMaliyet'))}</small>` : '<span class="text-muted">—</span>';
    $('#pmMatris tbody').innerHTML = sgkler.map(s => { const ls = L.filter(p => p.sgkIsvereni === s); return `<tr><td><b>${esc(s)}</b></td>${catilar.map(c => `<td class="s">${h(ls.filter(p => p.catiDepartman === c))}</td>`).join('')}<td class="s">${h(ls)}</td></tr>`; }).join('') + `<tr class="toplam"><td>Toplam</td>${catilar.map(c => `<td class="s">${h(L.filter(p => p.catiDepartman === c))}</td>`).join('')}<td class="s">${h(L)}</td></tr>`;
    const kal = [['xNet', 'X-Net Ücret'], ['rNet', 'R-Net Ücret'], ['brutUcMasraflari', 'Brüt Ücret Masrafları'], ['topIsvPayi', 'İşveren Payı'], ['topEkCalisma', 'Ek Çalışma'], ['yemek', 'Yemek'], ['yol', 'Yol'], ['servis', 'Servis'], ['kiraDestegi', 'Kira Desteği'], ['egitim', 'Eğitim'], ['satisPrimi', 'Satış Primi'], ['ozelOdeme', 'Özel Ödeme'], ['topYanHaklar', 'Yan Haklar'], ['topYasalOd', 'Yasal Ödemeler']].map(([f, ad]) => [ad, toplam(L, f)]);
    const maks = Math.max(1, ...kal.map(x => x[1]));
    $('#pmKalemler').innerHTML = kal.map(([ad, v]) => `<div class="pm-bar"><span>${esc(ad)}</span><span class="b"><i style="width:${v ? Math.max(1.5, v / maks * 100) : 0}%"></i></span><span class="v">${v ? kisa(v) : '—'} <small>${v ? '%' + pct(v, genel) : ''}</small></span></div>`).join('');
    const dag = (id, anahtar, sira) => { const g = grupla(L, anahtar); const ks = sira ? sira.filter(x => g[x]) : Object.keys(g).sort((a, b) => g[b].genelMaliyet - g[a].genelMaliyet); const r = o => `<tr><td>${esc(o.ad)}</td><td class="s">${o.n}</td><td class="s">${kisa(o.genelMaliyet)}</td><td class="s">${kisa(o.n ? o.genelMaliyet / o.n : 0)}</td></tr>`;
      $('#' + id + ' tbody').innerHTML = ks.length ? ks.map(x => r(g[x])).join('') + `<tr class="toplam"><td>Toplam</td><td class="s">${L.length}</td><td class="s">${kisa(genel)}</td><td class="s">${kisa(L.length ? genel / L.length : 0)}</td></tr>` : '<tr><td colspan="4" class="na-bos">Kayıt yok.</td></tr>'; };
    dag('pmDagUnvan', 'unvan'); dag('pmDagOzel', 'ozelDurum', ['Emekli', 'Engelli', 'Özel Durumu Yok']); dag('pmDagKidem', 'kidemAralik', KIDEM_SIRA);
    $('#pmTop10 tbody').innerHTML = [...L].sort((a, b) => b.genelMaliyet - a.genelMaliyet).slice(0, 10).map((p, i) => `<tr class="kartAc" data-sira="${p.sira}" style="cursor:pointer"><td>${i + 1}</td><td><b>${esc(p.adSoyad)}</b>${p.ozelDurum ? ` <small class="text-muted">(${esc(p.ozelDurum)})</small>` : ''}</td><td>${esc(p.unvan)}</td><td>${esc(p.gorev)}</td><td>${esc(p.sgkIsvereni)}</td><td>${esc(p.catiDepartman)}</td><td class="s">${kisa(p.topNet)}</td><td class="s">${hucre(p.topSosyal)}</td><td class="s">${hucre(p.topPrim)}</td><td class="s"><b>${kisa(p.genelMaliyet)}</b></td><td class="s">${pct(p.genelMaliyet, genel)}</td></tr>`).join('') || '<tr><td colspan="11" class="na-bos">Kayıt yok.</td></tr>';
  }
  function ozetExcel() {
    const k = $('#pmKirilim').value, rows = [];
    const tabloSatirlari = (id, bas) => { rows.push([bas]); rows.push($$('#' + id + ' thead th').map(t => t.textContent.trim())); $$('#' + id + ' tbody tr').forEach(tr => rows.push($$('td', tr).map(td => td.textContent.trim().replace('—', '')))); rows.push([]); };
    tabloSatirlari('pmKirilimTablo', `${KIRILIM_AD[k]} Bazında Maliyet Kalemleri (${V.yil}, aylık TL)`);
    tabloSatirlari('pmMatris', 'SGK İşvereni × Çatı Departman (personel sayısı ve genel maliyet)');
    rows.push(['Maliyet Kalemi Dağılımı']); rows.push(['Kalem', 'Tutar', 'Pay %']); $$('#pmKalemler .pm-bar').forEach(d => rows.push([d.children[0].textContent.trim(), d.children[2].childNodes[0].textContent.trim().replace('—', ''), (d.querySelector('small').textContent || '').replace('%', '')])); rows.push([]);
    tabloSatirlari('pmDagUnvan', 'Ünvan Dağılımı'); tabloSatirlari('pmDagOzel', 'Özel Durum Dağılımı'); tabloSatirlari('pmDagKidem', 'Kıdem Dağılımı'); tabloSatirlari('pmTop10', 'En Yüksek Maliyetli 10 Personel');
    csvIndir('personel_maliyet_ozet_raporu', rows);
  }

  // ---- tablo düzenleme / toplu kayıt
  function modDegis(ac) { duzenleme = ac; if (!ac) taslak = {}; $('#pmDuzenle').style.display = ac ? 'none' : ''; $('#pmKaydet').style.display = ac ? '' : 'none'; $('#pmVazgec').style.display = ac ? '' : 'none'; ciz(); }
  async function kaydet() {
    const satirlar = Object.keys(taslak).map(s => satirVeri({ ...bul(+s), ...taslak[s] }));
    if (!satirlar.length) { mesaj('info', 'Değişiklik yok.'); return; }
    const j = await post('/PersonelMaliyet/Kaydet', { satirlar });
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    duzenleme = false; taslak = {}; $('#pmDuzenle').style.display = ''; $('#pmKaydet').style.display = 'none'; $('#pmVazgec').style.display = 'none';
    veriGuncelle(j); mesaj('success', esc(j.message));
  }
  async function sil(sira) {
    const p = bul(sira); if (!p) return;
    if (!window.confirm(`${p.adSoyad} (${p.cariKodu}) satırı kalıcı olarak silinsin mi? İşten ayrılan personel silinmez; kartında Durum = İşten Ayrıldı yapılır.`)) return;
    const j = await post('/PersonelMaliyet/Sil', { sira });
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    delete taslak[sira]; if (kartSira === sira) kartKapat(); veriGuncelle(j); mesaj('success', esc(j.message));
  }
  function kayitBilgi() { $('#pmKayitBilgi').textContent = V.sonKayit ? `Son kayıt: ${V.sonKayit.kisi} · ${V.sonKayit.tarih}` : `Kaynak tablo: ${V.kaynak} · henüz portalda düzenlenmedi`; }

  // ---- Personel Cari Kartı (çekmece): mevcut kartı düzenler ya da yeni kart açar (kartSira = 0)
  function kartAc(sira) {
    const p = sira ? etkin(bul(sira)) : { sira: 0, cariKodu: '335.' + String((V.personel.reduce((m, x) => Math.max(m, x.sira), 0) + 1)).padStart(3, '0') + ' (kayıtta kesinleşir)', durum: 'Aktif', ozelDurum: '', not: '' };
    if (sira && !p) return; kartSira = sira; $('#pmKartHata').style.display = 'none';
    $('#pmKartBaslik').innerHTML = `<i class="fas fa-id-card me-1"></i>${sira ? esc(p.cariKodu) + ' · ' + esc(p.adSoyad) : 'Yeni Personel Cari Kartı'} ${sira ? `<span class="rt-durum rt-${p.durum === 'İşten Ayrıldı' ? 'hata' : 'ok'}">${esc(p.durum || 'Aktif')}</span>` : ''}`;
    $('#pmKartAksiyon').innerHTML = `<button type="button" class="btn btn-warning btn-sm py-0" id="pmKartKaydet"><i class="fas fa-floppy-disk me-1"></i>${sira ? 'Kaydet' : 'Kartı Aç'}</button>${sira ? `<button type="button" class="btn btn-light btn-sm py-0" id="pmKartSil" style="color:#b91c1c"><i class="fas fa-trash me-1"></i>Sil</button><a class="btn btn-light btn-sm py-0" id="pmKartCari" href="/Rapor120" target="_blank" title="İK › Personel Carileri ekranı"><i class="fas fa-address-book me-1"></i>Personel Carileri</a>` : ''}<button type="button" class="btn btn-light btn-sm py-0" id="pmKartVazgec">Vazgeç</button>`;
    // etiketler id/for ile girişe bağlı; zorunlu alanlarda required + aria-required (label'a tıklayınca odaklanır, ekran okuyucu adı okur)
    const inp = (k, etiket, cls, ek) => `<div class="${cls || ''}"><label class="${ek && ek.z ? 'z' : ''}" for="pmk_${k}">${etiket}</label><input class="form-control" id="pmk_${k}" data-k="${k}" value="${esc(p[k] ?? '')}" ${ek && ek.list ? `list="${ek.list}"` : ''} ${ek && ek.tip ? `type="${ek.tip}"` : ''} ${ek && ek.ph ? `placeholder="${esc(ek.ph)}"` : ''} ${ek && ek.max2 ? `max="${ek.max2}"` : ''} ${ek && ek.z ? 'required aria-required="true"' : ''} maxlength="${ek && ek.max ? ek.max : 120}"></div>`;
    const sec = (k, etiket, l) => `<div><label for="pmk_${k}">${etiket}</label><select class="form-select" id="pmk_${k}" data-k="${k}">${l.map(o => `<option value="${esc(o[0])}" ${(p[k] || '') === o[0] ? 'selected' : ''}>${esc(o[1])}</option>`).join('')}</select></div>`;
    const say = k => `<div><label for="pmk_${k}">${TUTAR_ETIKET[k]}</label><input class="form-control" id="pmk_${k}" type="number" min="0" step="1" data-k="${k}" value="${Number(p[k]) || 0}"></div>`;
    const bugunIso = new Date().toISOString().slice(0, 10);
    $('#pmKartIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-user me-1"></i>Kimlik ve Görev Bilgileri</span><span class="not">CARİ_MASTER · Personel Carileri (335)</span></div><div class="na-kart-g"><div class="pm-form">
        <div><label>Cari Kodu</label><input class="form-control ro" value="${esc(p.cariKodu)}" readonly></div>${inp('adSoyad', 'Ad Soyad', 'g2', { z: true })}
        ${inp('tcKimlik', 'TC Kimlik No', '', { ph: '11 hane', max: 11 })}${inp('sgkIsvereni', 'SGK İşvereni', '', { z: true, list: 'dlSgk' })}${inp('fiiliGorevYeri', 'Fiili Görev Yeri', '', { z: true, list: 'dlYer' })}
        ${inp('unvan', 'Ünvan', '', { z: true, list: 'dlUnvan' })}${inp('bolum', 'Bölüm', '', { z: true, list: 'dlBolum' })}${inp('gorev', 'Görev', '', { z: true, list: 'dlGorev' })}
        ${inp('iseGiris', 'İşe Giriş', '', { tip: 'date' })}${inp('catiDepartman', 'Çatı Departman', '', { z: true, list: 'dlCati' })}${sec('ozelDurum', 'Özel Durumu', [['', '—'], ['Emekli', 'Emekli'], ['Engelli', 'Engelli']])}
        ${sec('durum', 'Durum', [['Aktif', 'Aktif'], ['İşten Ayrıldı', 'İşten Ayrıldı']])}${inp('ayrilisTarihi', 'Ayrılış Tarihi', '', { tip: 'date', z: true, max2: bugunIso })}<div></div>
        ${inp('telefon', 'Telefon', '', { ph: '05xx xxx xx xx' })}${inp('eposta', 'E-posta', 'g2', { tip: 'email', ph: 'ad.soyad@sirket.com' })}
      </div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-coins me-1"></i>Ücret ve Maliyet Kalemleri (Aylık, TL)</span><span class="not">Toplamlar otomatik hesaplanır</span></div><div class="na-kart-g"><div class="pm-form">${GIRIS.map(say).join('')}</div>
        <div class="pm-ozet-serit" id="pmKartOzet"></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-note-sticky me-1"></i><label for="pmk_not" class="m-0">Not</label></span></div><div class="na-kart-g"><textarea class="form-control" id="pmk_not" data-k="not" rows="2" maxlength="300">${esc(p.not || '')}</textarea></div></div>`;
    kartOzet();
    $$('#pmKartIc [data-k]').forEach(e => e.addEventListener('input', () => { $('#pmKartHata').style.display = 'none'; if (GIRIS.includes(e.dataset.k)) kartOzet(); if (e.dataset.k === 'durum') $('#pmKartIc [data-k=ayrilisTarihi]').disabled = e.value !== 'İşten Ayrıldı'; }));
    $('#pmKartIc [data-k=ayrilisTarihi]').disabled = (p.durum || 'Aktif') !== 'İşten Ayrıldı';
    $('#pmKartKaydet').addEventListener('click', kartKaydet); $('#pmKartVazgec').addEventListener('click', kartKapat); const s = $('#pmKartSil'); if (s) s.addEventListener('click', () => sil(kartSira));
    const cari = $('#pmKartCari'); if (cari) cari.addEventListener('click', ev => { if (window.top && window.top !== window && typeof window.top.portalSekmeAc === 'function') { ev.preventDefault(); window.top.portalSekmeAc('Personel Carileri', '/Rapor120'); } });
    $('#pmKart').classList.add('acik'); $('#pmPerde').classList.add('acik');
    try { const u = new URL(location.href); if (sira) u.searchParams.set('kod', bul(sira).cariKodu); else u.searchParams.delete('kod'); history.replaceState(null, '', u); } catch (e) { /* yoksay */ }
  }
  function kartOku() { const d = { sira: kartSira || 0 }; $$('#pmKartIc [data-k]').forEach(e => { const k = e.dataset.k; d[k] = GIRIS.includes(k) ? (Number(e.value) || 0) : e.value.trim(); }); return d; }
  function kartOzet() { const p = hesapla(kartOku()); $('#pmKartOzet').innerHTML = [['Top. Net', p.topNet], ['Toplam Brüt', p.toplamBrutUcret], ['Top. Sosyal Yrd.', p.topSosyal], ['Top. Prim', p.topPrim], ['Genel Maliyet', p.genelMaliyet]].map(([l, v], i) => `<div class="${i === 4 ? 'genel' : ''}"><span>${l}</span><b>${kisa(v)} TL</b></div>`).join(''); }
  async function kartKaydet() {
    const d = kartOku(), sira = kartSira, eskiDurum = sira ? (bul(sira).durum || 'Aktif') : null;
    const j = sira ? await post('/PersonelMaliyet/Kaydet', { satirlar: [d] }) : await post('/PersonelMaliyet/Ekle', d);
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

  // ---- excel (tablo)
  function excel() {
    const bas = ['Sıra', 'Cari Kodu', 'SGK İşvereni', 'Fiili Görev Yeri', 'Ad Soyad', 'Ünvan', 'Bölüm', 'Görev', 'İşe Giriş', 'Kıdem (Yıl)', 'Çatı Departman', 'Özel Durumu', 'Durum', 'Ayrılış Tarihi', V.yil + ' X-Net', V.yil + ' R-Net', V.yil + ' Top. Net', 'Brüt Üc. Masrafları', 'Toplam Brüt Ücret', 'Top. İşv. Payı', 'Top. Ek Çalışma', 'Yemek', 'Yol', 'Servis', 'Kira Desteği', 'Eğitim', 'Top. Sosyal Yrd.', 'Satış Primi', 'Özel Ödeme', 'Top. Prim', 'Top. Yan Haklar', 'Top. Yasal Öd.', V.yil + ' Genel Maliyet', 'Not'];
    csvIndir('personel_maliyet_raporu', [bas, ...L.map(p => [p.sira, p.cariKodu, p.sgkIsvereni, p.fiiliGorevYeri, p.adSoyad, p.unvan, p.bolum, p.gorev, p.iseGirisTr || '', p.kidemYil ?? '', p.catiDepartman, p.ozelDurum, p.durum, p.ayrilisTr || '', sayi(p.xNet), sayi(p.rNet), sayi(p.topNet), sayi(p.brutUcMasraflari), sayi(p.toplamBrutUcret), sayi(p.topIsvPayi), sayi(p.topEkCalisma), sayi(p.yemek), sayi(p.yol), sayi(p.servis), sayi(p.kiraDestegi), sayi(p.egitim), sayi(p.topSosyal), sayi(p.satisPrimi), sayi(p.ozelOdeme), sayi(p.topPrim), sayi(p.topYanHaklar), sayi(p.topYasalOd), sayi(p.genelMaliyet), p.not || ''])]);
  }

  // ---- başlat
  fetch('/PersonelMaliyet/Veri').then(r => r.json()).then(v => {
    V = v; listeleriDoldur();
    ['#pmAra', '#pmDurum', '#pmSgk', '#pmYer', '#pmCati', '#pmBolum', '#pmUnvan', '#pmOzel'].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    $('#pmTemizle').addEventListener('click', () => { $('#pmAra').value = ''; ['#pmSgk', '#pmYer', '#pmCati', '#pmBolum', '#pmUnvan', '#pmOzel'].forEach(s => $(s).value = ''); $('#pmDurum').value = 'Aktif'; ciz(); });
    $('#pmExcel').addEventListener('click', excel); $('#pmOzetExcel').addEventListener('click', ozetExcel); $('#pmKirilim').addEventListener('change', cizOzet);
    $('#pmDuzenle').addEventListener('click', () => modDegis(true)); $('#pmVazgec').addEventListener('click', () => modDegis(false)); $('#pmKaydet').addEventListener('click', kaydet); $('#pmEkle').addEventListener('click', () => kartAc(0));
    $('#pmTablo tbody').addEventListener('click', ev => { const k = ev.target.closest('button.kart'); if (k) { kartAc(+k.dataset.sira); return; } const s = ev.target.closest('button.sil'); if (s) sil(+s.dataset.sira); });
    $('#pmTop10 tbody').addEventListener('click', ev => { const tr = ev.target.closest('tr.kartAc'); if (tr) kartAc(+tr.dataset.sira); });
    $('#pmKartKapat').addEventListener('click', kartKapat); $('#pmPerde').addEventListener('click', kartKapat);
    $$('#pmSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    kayitBilgi(); ciz(); sekme(kok.dataset.baslangic || 'tablo'); window.NA_HAZIR = true;
    const kod = kok.dataset.kod; if (kod) { const p = V.personel.find(x => x.cariKodu === kod); if (p) kartAc(p.sira); }
  }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
