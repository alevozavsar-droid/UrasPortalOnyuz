// ============================================================
// PERSONEL MALİYET YANSITMASI — ekran betiği (Views/PersonelYansitma/Index.cshtml; veri /PersonelYansitma/Veri?donem=; kurallar Data/PersonelYansitmaOrnek.cs)
// Sekme "Yansıtma Hazırlığı": bordro dönemi → SGK işvereni ≠ fiili görev yeri olan personel otomatik listelenir (kalemler GDR.13 ↔ YNS.13),
//   satır detayı (kalemler + görev yeri dağılımı, pay düzenleme), seçim + "Seçilenleri Yansıt" → iki şirkete kayıt; Excel.
// Sekme "Yansıtma Kayıtları": kayıt listesi, kaynak → hedef matrisi, detay çekmecesi (kaynak fişi + hedef fişi + geçmiş), Fatura Oluştur / İptal, Excel.
// Sekme "Kalem Eşleme": maliyet alanı → gider kodu (katalog) ↔ yansıtma kodu; KDV parametresi; katalog listesi.
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Kaydedildi", "Faturalandı", "İptal").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('py'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const tl = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const pct = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 2 });
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const durumRozet = d => rozet(d, d === 'Kaydedildi' || d === 'Faturalandı' ? 'ok' : d === 'İptal' ? 'hata' : d === 'Kısmen Kaydedildi' ? 'bekle' : 'bilgi');
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const yon = (k, h, pay) => `<span class="py-yon"><b>${esc(k)}</b><span class="ok">→</span><b>${esc(h)}</b>${pay !== undefined ? `<span class="py-pay">%${pct(pay)}</span>` : ''}</span>`;
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');
  const csvIndir = (ad, satirlar) => { const t = satirlar.map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + new Date().toISOString().slice(0, 10) + '.csv'; a.click(); };
  let V = null, L = [], K = [], secili = new Set(), acikDetay = new Set(), aktifSekme = 'hazirlik';
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 10000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  async function post(url, veri) { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }); return r.json(); }
  const donem = () => $('#pyDonem').value || V.bugunDonem;

  // ---- sekmeler
  function sekme(p) {
    aktifSekme = p; $$('#pySekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#py > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    try { const u = new URL(location.href); if (p === 'hazirlik') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }

  // ---- veri
  async function yukle() {
    const r = await fetch('/PersonelYansitma/Veri?donem=' + encodeURIComponent(donem())); V = await r.json(); veriUygula();
  }
  function veriUygula() {
    const doldur = (id, l, ek) => { const e = $(id), v = e.value; e.innerHTML = '<option value="">Tümü</option>' + (ek || '') + l.map(x => `<option value="${esc(x.kod !== undefined ? x.kod : x)}">${esc(x.ad !== undefined ? x.ad : x)}</option>`).join(''); e.value = v; };
    doldur('#pyKaynak', V.sirketler); doldur('#pyHedef', V.sirketler); doldur('#pyCati', [...new Set(V.adaylar.map(a => a.catiDepartman))].sort());
    doldur('#pyKKaynak', V.sirketler); doldur('#pyKHedef', V.sirketler); doldur('#pyKDonem', V.donemler);
    $('#pyKdv').value = V.kdvOrani;
    cizHazirlik(); cizKayitlar(); cizEsleme();
  }

  // ---- sekme 1: hazırlık
  function adayListesi() {
    const ara = ($('#pyAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value;
    return V.adaylar.filter(a => (!v('#pyKaynak') || a.kaynakSirket === v('#pyKaynak')) && (!v('#pyHedef') || a.hedefler.some(h => h.sirket === v('#pyHedef'))) && (!v('#pyCati') || a.catiDepartman === v('#pyCati')) && (!v('#pyDurum') || a.durum === v('#pyDurum'))
      && (!ara || [a.adSoyad, a.gorev, a.cariKodu, a.unvan, a.sgkIsvereni, a.fiiliGorevYeri].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  function kpiCiz() {
    const A = V.adaylar, kay = V.kayitlar.filter(k => k.donem === donem() && k.durum !== 'İptal');
    const kalan = A.filter(a => a.durum !== 'Kaydedildi'), ciftler = new Set(A.flatMap(a => a.hedefler.map(h => a.kaynakSirket + '→' + h.sirket)));
    const kpi = [['#b45309', 'Yansıtma Gereken Personel', A.length, `${V.donemAdi} · ${kalan.length} Kayıtsız`], ['#0f172a', 'Yansıtılacak Toplam', kisa(A.reduce((s, a) => s + a.yansitilacak, 0)) + ' TL', `Aylık Maliyet ${kisa(A.reduce((s, a) => s + a.maliyet, 0))} TL`],
      ['#1d4ed8', 'Şirket Çifti', ciftler.size, 'Kaynak → Hedef'], ['#16a34a', 'Dönem Kayıtları', kay.length, `${kisa(kay.reduce((s, k) => s + k.toplam, 0))} TL · ${kay.filter(k => k.durum === 'Faturalandı').length} Faturalı`],
      ['#7c3aed', 'Seçili', secili.size, `${kisa(A.filter(a => secili.has(a.sira)).reduce((s, a) => s + a.yansitilacak, 0))} TL`], ['#64748b', 'Tüm Kayıtlar', V.kayitlar.length, `${V.kayitlar.filter(k => k.durum === 'İptal').length} İptal`]];
    $('#pyKpi').innerHTML = kpi.map(k => `<div style="--k:${k[0]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $('#pyNAday').textContent = A.length; $('#pyNKayit').textContent = V.kayitlar.length;
  }
  function detayHtml(a) {
    const kal = a.kalemler.length ? `<table class="na-tablo py-kalem" data-dinamik="kapali"><thead><tr><th>Maliyet Alanı</th><th>X/R</th><th>Gider Kodu</th><th>Gider Kalemi</th><th>Yansıtma Kodu</th><th>Yansıtma Gelir Kalemi</th><th class="s">Tutar (%100)</th>${a.hedefler.map(h => `<th class="s">${esc(h.sirket)} %${pct(h.pay)}</th>`).join('')}</tr></thead><tbody>${a.kalemler.map(k => `<tr><td>${esc(k.alanAdi)}</td><td>${esc(k.aktarimTipi || '—')}</td><td class="kod">${esc(k.giderKodu)}</td><td>${esc(k.giderKalem)}</td><td class="kod">${esc(k.yansitmaKodu)}</td><td>${esc(k.yansitmaKalem)}</td><td class="s">${tl(k.tutar)}</td>${a.hedefler.map(h => `<td class="s">${tl(Math.round(k.tutar * h.pay) / 100)}</td>`).join('')}</tr>`).join('')}<tr class="top"><td colspan="6"><b>Toplam</b></td><td class="s"><b>${tl(a.maliyet)}</b></td>${a.hedefler.map(h => `<td class="s"><b>${tl(h.tutar)}</b></td>`).join('')}</tr></tbody></table>` : '<div class="na-bos">Maliyet kalemi yok (tutarlar 0).</div>';
    const dag = `<div class="py-dagilim mt-2"><b>Görev Yeri Dağılımı${a.dagilimOzel ? ' (Özel)' : ''}:</b>${a.dagilim.map((d, i) => `<span>${esc(d.sirket)} <input type="number" min="0" max="100" step="0.5" data-sira="${a.sira}" data-i="${i}" data-sirket="${esc(d.sirket)}" value="${d.pay}"> %</span>`).join('')}<select class="form-select" data-ek="${a.sira}"><option value="">+ Şirket Ekle</option>${V.sirketler.filter(s => !a.dagilim.some(d => d.sirket === s)).map(s => `<option value="${esc(s)}">${esc(s)}</option>`).join('')}</select><button type="button" class="btn btn-light btn-sm py-0 dagKaydet" data-sira="${a.sira}" style="font-size:.64rem"><i class="fas fa-floppy-disk me-1"></i>Dağılımı Kaydet</button>${a.dagilimOzel ? `<button type="button" class="btn btn-light btn-sm py-0 dagSifirla" data-sira="${a.sira}" style="font-size:.64rem">Varsayılana Dön</button>` : ''}<span class="text-muted">SGK işvereni (${esc(a.kaynakSirket)}) payı yansıtılmaz; toplam %100 olmalı.</span></div>`;
    const kayit = a.hedefler.filter(h => h.kayit).map(h => `<span class="py-pill">${esc(h.sirket)}: ${esc(h.kayit)}</span>`).join(' ');
    return `<div class="table-responsive">${kal}</div>${dag}${kayit ? `<div class="mt-2" style="font-size:.66rem">Bu dönem kayıtları: ${kayit} <button type="button" class="btn btn-light btn-sm py-0 kayitAc" data-no="${esc(a.hedefler.find(h => h.kayit).kayit)}" style="font-size:.62rem">Kaydı Aç</button></div>` : ''}`;
  }
  const secilebilir = a => a.durum !== 'Kaydedildi' && a.durum !== 'Kalem Yok';
  function hepsiGuncelle() { const s = L.filter(secilebilir), h = $('#pyHepsi'); h.disabled = !s.length; h.checked = s.length > 0 && s.every(a => secili.has(a.sira)); }
  function cizHazirlik() {
    secili = new Set([...secili].filter(s => V.adaylar.some(a => a.sira === s && secilebilir(a))));   // listeden düşen / kaydedilen satırlar seçimden çıkar
    L = adayListesi(); kpiCiz();
    $('#pyTablo tbody').innerHTML = L.map(a => `<tr class="ana ${a.durum === 'Kaydedildi' ? 'kaydedildi' : ''}" data-sira="${a.sira}"><td><input type="checkbox" class="sec" data-sira="${a.sira}" ${secili.has(a.sira) ? 'checked' : ''} ${a.durum === 'Kaydedildi' ? 'disabled title="Bu dönemde kaydedildi"' : a.durum === 'Kalem Yok' ? 'disabled title="Maliyet kalemi yok (tutarlar 0)"' : ''}></td><td class="ad"><b>${esc(a.adSoyad)}</b><br><small class="text-muted">${esc(a.cariKodu)}</small></td><td>${esc(a.unvan)}<br><small class="text-muted">${esc(a.gorev)}</small></td><td>${esc(a.catiDepartman)}</td><td>${esc(a.sgkIsvereni)}</td><td>${esc(a.fiiliGorevYeri)}</td><td>${a.hedefler.map(h => yon(a.kaynakSirket, h.sirket, h.pay)).join('<br>')}</td><td class="s">${tl(a.maliyet)}</td><td class="s">%${pct(a.yansitilacakPay)}</td><td class="s"><b>${tl(a.yansitilacak)}</b></td><td class="s">${a.kalemler.length}</td><td>${durumRozet(a.durum)}</td></tr>${acikDetay.has(a.sira) ? `<tr class="detay" data-sira="${a.sira}"><td colspan="12">${detayHtml(a)}</td></tr>` : ''}`).join('') || '<tr><td colspan="12" class="na-bos">Bu dönemde yansıtma gerektiren personel yok (SGK işvereni ile fiili görev yeri aynı).</td></tr>';
    $('#pyTopMaliyet').textContent = tl(L.reduce((s, a) => s + a.maliyet, 0)) + ' TL'; $('#pyTopYansitma').textContent = tl(L.reduce((s, a) => s + a.yansitilacak, 0)) + ' TL'; $('#pyTopKisi').textContent = `${L.length} Personel`;
    $('#pySayi').textContent = `${L.length} / ${V.adaylar.length} personel · ${secili.size} seçili · ${V.donemAdi}`;
    hepsiGuncelle();
  }
  $('#pyTablo').addEventListener('click', async e => {
    const cb = e.target.closest('input.sec'); if (cb) { if (cb.checked) secili.add(+cb.dataset.sira); else secili.delete(+cb.dataset.sira); kpiCiz(); $('#pySayi').textContent = `${L.length} / ${V.adaylar.length} personel · ${secili.size} seçili · ${V.donemAdi}`; hepsiGuncelle(); return; }
    if (e.target.closest('input, select, button')) {
      const ks = e.target.closest('button.dagKaydet'); if (ks) { await dagilimKaydet(+ks.dataset.sira); return; }
      const sf = e.target.closest('button.dagSifirla'); if (sf) { const j = await post('/PersonelYansitma/DagilimKaydet', { sira: +sf.dataset.sira, donem: donem(), dagilim: [] }); if (!j.success) { mesaj('danger', esc(j.message)); return; } V = j; veriUygula(); mesaj('success', esc(j.message)); return; }
      const ka = e.target.closest('button.kayitAc'); if (ka) { sekme('kayitlar'); drawerAc(ka.dataset.no); return; }
      return;
    }
    const tr = e.target.closest('tr.ana'); if (!tr) return;
    const s = +tr.dataset.sira; if (acikDetay.has(s)) acikDetay.delete(s); else acikDetay.add(s); cizHazirlik();
  });
  $('#pyTablo').addEventListener('change', e => {
    const sel = e.target.closest('select[data-ek]'); if (!sel || !sel.value) return;
    // "+ Şirket Ekle": kutulara yazılmış (henüz kaydedilmemiş) paylar korunarak yeni şirket %0 ile eklenir; "(Özel)" işareti yalnız kayıt sonrası sunucudan gelir
    const sira = +sel.dataset.ek, a = V.adaylar.find(x => x.sira === sira); if (!a) return;
    a.dagilim = $$(`#pyTablo tr.detay[data-sira="${sira}"] .py-dagilim input[data-sira]`).map(i => ({ sirket: i.dataset.sirket, pay: Number(i.value) || 0 }));
    a.dagilim.push({ sirket: sel.value, pay: 0 }); cizHazirlik();
  });
  async function dagilimKaydet(sira) {
    const girisler = $$(`#pyTablo tr.detay[data-sira="${sira}"] .py-dagilim input[data-sira]`);
    const dagilim = girisler.map(i => ({ sirket: i.dataset.sirket, pay: Number(i.value) || 0 }));
    const j = await post('/PersonelYansitma/DagilimKaydet', { sira, donem: donem(), dagilim });
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    V = j; veriUygula(); mesaj('success', esc(j.message));
  }
  $('#pyHepsi').addEventListener('change', () => { L.filter(secilebilir).forEach(a => { if ($('#pyHepsi').checked) secili.add(a.sira); else secili.delete(a.sira); }); cizHazirlik(); });
  $('#pyTumunuSec').addEventListener('click', () => { L.filter(secilebilir).forEach(a => secili.add(a.sira)); cizHazirlik(); });
  $('#pySecimTemizle').addEventListener('click', () => { secili.clear(); cizHazirlik(); });
  $('#pyYansit').addEventListener('click', async () => {
    const siralar = [...secili].filter(s => V.adaylar.some(a => a.sira === s && secilebilir(a)));
    if (!siralar.length) { mesaj('warning', 'Yansıtılacak personel seçin (kayıtsız satırlar).'); return; }
    const secilenler = V.adaylar.filter(a => siralar.includes(a.sira));
    if (!window.confirm(`${V.donemAdi} dönemi için ${secilenler.length} personel, ${secilenler.reduce((s, a) => s + a.hedefler.length, 0)} yansıtma kaydı (${tl(secilenler.reduce((s, a) => s + a.yansitilacak, 0))} TL) oluşturulacak.\nHer kayıt SGK işvereni ve fiili görev yeri şirketine fiş olarak atılır. Devam edilsin mi?`)) return;
    const j = await post('/PersonelYansitma/Olustur', { donem: donem(), siralar });
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    secili.clear(); V = j; veriUygula(); mesaj('success', esc(j.message) + (j.yeniler && j.yeniler.length ? ` <button type="button" class="btn btn-light btn-sm py-0 ms-2" id="pyKayitlaraGit" style="font-size:.64rem">Kayıtları Gör</button>` : ''));
    const g = $('#pyKayitlaraGit'); if (g) g.addEventListener('click', () => { $('#pyKDonem').value = donem(); sekme('kayitlar'); cizKayitlar(); });
  });
  $('#pyExcel').addEventListener('click', () => csvIndir('personel_yansitma_hazirlik_' + donem(), [['Dönem', 'Cari Kodu', 'Ad Soyad', 'Ünvan', 'Görev', 'Çatı Departman', 'SGK İşvereni', 'Fiili Görev Yeri', 'Kaynak', 'Hedef', 'Pay %', 'Aylık Maliyet', 'Yansıtılacak', 'Durum'], ...L.flatMap(a => a.hedefler.map(h => [donem(), a.cariKodu, a.adSoyad, a.unvan, a.gorev, a.catiDepartman, a.sgkIsvereni, a.fiiliGorevYeri, a.kaynakSirket, h.sirket, pct(h.pay), sayi(a.maliyet), sayi(h.tutar), a.durum]))]));
  ['#pyAra', '#pyKaynak', '#pyHedef', '#pyCati', '#pyDurum'].forEach(s => { $(s).addEventListener('input', cizHazirlik); $(s).addEventListener('change', cizHazirlik); });
  $('#pyDonem').addEventListener('change', () => { secili.clear(); acikDetay.clear(); yukle(); });

  // ---- sekme 2: kayıtlar
  function kayitListesi() {
    const ara = ($('#pyKAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value;
    return V.kayitlar.filter(k => (!v('#pyKDonem') || k.donem === v('#pyKDonem')) && (!v('#pyKKaynak') || k.kaynakSirket === v('#pyKKaynak')) && (!v('#pyKHedef') || k.hedefSirket === v('#pyKHedef')) && (!v('#pyKDurum') || k.durum === v('#pyKDurum'))
      && (!ara || [k.no, k.adSoyad, k.cariKodu, k.kaynakFisNo, k.hedefFisNo, k.faturaNo, k.kaynakSirket, k.hedefSirket].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  function cizKayitlar() {
    K = kayitListesi();
    $('#pyKayitTablo tbody').innerHTML = K.map(k => `<tr class="tik" data-no="${esc(k.no)}"><td><b>${esc(k.no)}</b></td><td>${esc(k.donemAdi)}</td><td><b>${esc(k.adSoyad)}</b><br><small class="text-muted">${esc(k.cariKodu)} · ${esc(k.gorev)}</small></td><td>${yon(k.kaynakSirket, k.hedefSirket)}</td><td class="s">%${pct(k.pay)}</td><td class="s">${tl(k.net)}</td><td class="s">${k.kdv ? tl(k.kdv) : '—'}</td><td class="s"><b>${tl(k.toplam)}</b></td><td><small>${esc(k.kaynakFisNo)}</small></td><td><small>${esc(k.hedefFisNo)}</small></td><td>${bos(k.faturaNo)}</td><td>${durumRozet(k.durum)}</td><td><small>${esc(k.olusturan)}<br>${esc(k.zaman)}</small></td></tr>`).join('') || '<tr><td colspan="13" class="na-bos">Kayıt yok. Yansıtma Hazırlığı sekmesinden dönem seçip "Seçilenleri Yansıt" ile oluşturun.</td></tr>';
    const g = K.filter(k => k.durum !== 'İptal');
    $('#pyKSayi').textContent = `${K.length} / ${V.kayitlar.length} kayıt · iptal hariç ${tl(g.reduce((s, k) => s + k.toplam, 0))} TL · ${g.filter(k => k.durum === 'Faturalandı').length} faturalı`;
    // kaynak → hedef matrisi
    const kaynaklar = [...new Set(g.map(k => k.kaynakSirket))].sort(), hedefler = [...new Set(g.map(k => k.hedefSirket))].sort();
    const h = l => l.length ? `<b>${kisa(l.reduce((s, k) => s + k.toplam, 0))}</b><br><small class="text-muted">${l.length} kayıt · ${new Set(l.map(k => k.personelSira)).size} kişi</small>` : '<span class="text-muted">—</span>';
    $('#pyMatris thead').innerHTML = `<tr><th>Kaynak ↓ / Hedef →</th>${hedefler.map(x => `<th class="s">${esc(x)}</th>`).join('')}<th class="s">Toplam</th></tr>`;
    $('#pyMatris tbody').innerHTML = kaynaklar.length ? kaynaklar.map(ka => `<tr><td><b>${esc(ka)}</b></td>${hedefler.map(he => `<td class="s">${h(g.filter(k => k.kaynakSirket === ka && k.hedefSirket === he))}</td>`).join('')}<td class="s">${h(g.filter(k => k.kaynakSirket === ka))}</td></tr>`).join('') + `<tr class="toplam"><td>Toplam</td>${hedefler.map(he => `<td class="s">${h(g.filter(k => k.hedefSirket === he))}</td>`).join('')}<td class="s">${h(g)}</td></tr>` : '<tr><td class="na-bos">Kayıt yok.</td></tr>';
  }
  $('#pyKayitTablo').addEventListener('click', e => { const tr = e.target.closest('tr.tik'); if (tr) drawerAc(tr.dataset.no); });
  ['#pyKAra', '#pyKDonem', '#pyKKaynak', '#pyKHedef', '#pyKDurum'].forEach(s => { $(s).addEventListener('input', cizKayitlar); $(s).addEventListener('change', cizKayitlar); });
  $('#pyKayitExcel').addEventListener('click', () => csvIndir('personel_yansitma_kayitlari', [['Yansıtma No', 'Dönem', 'Cari Kodu', 'Ad Soyad', 'Görev', 'Kaynak (SGK İşvereni)', 'Hedef (Fiili Görev Yeri)', 'Pay %', 'Net', 'KDV', 'Toplam', 'Kaynak Fişi', 'Hedef Fişi', 'Fatura', 'Durum', 'Oluşturan', 'Zaman'], ...K.map(k => [k.no, k.donemAdi, k.cariKodu, k.adSoyad, k.gorev, k.kaynakSirket, k.hedefSirket, pct(k.pay), sayi(k.net), sayi(k.kdv), sayi(k.toplam), k.kaynakFisNo, k.hedefFisNo, k.faturaNo || '', k.durum, k.olusturan, k.zaman])]));
  $('#pyFisExcel').addEventListener('click', () => csvIndir('personel_yansitma_fis_satirlari', [['Yansıtma No', 'Dönem', 'Şirket', 'Taraf', 'Fiş No', 'Hesap', 'Kod', 'Hesap Adı', 'Borç', 'Alacak'], ...K.filter(k => k.durum !== 'İptal').flatMap(k => [...k.kaynakFis.map(f => [k.no, k.donemAdi, k.kaynakSirket, 'Kaynak (SGK İşvereni)', k.kaynakFisNo, f.hesap, f.kod, f.ad, sayi(f.borc), sayi(f.alacak)]), ...k.hedefFis.map(f => [k.no, k.donemAdi, k.hedefSirket, 'Hedef (Fiili Görev Yeri)', k.hedefFisNo, f.hesap, f.kod, f.ad, sayi(f.borc), sayi(f.alacak)])])]));

  // ---- kayıt çekmecesi (iki taraflı fiş)
  function fisTablo(satirlar) { const tb = satirlar.reduce((s, f) => s + f.borc, 0), ta = satirlar.reduce((s, f) => s + f.alacak, 0); return `<table class="na-tablo py-fis" data-dinamik="kapali"><thead><tr><th>Hesap</th><th>Kod</th><th>Hesap Adı</th><th class="s">Borç</th><th class="s">Alacak</th></tr></thead><tbody>${satirlar.map(f => `<tr><td class="kod">${esc(f.hesap)}</td><td class="kod">${esc(f.kod || '')}</td><td>${esc(f.ad)}</td><td class="s">${f.borc ? tl(f.borc) : ''}</td><td class="s">${f.alacak ? tl(f.alacak) : ''}</td></tr>`).join('')}<tr class="top"><td colspan="3">Toplam ${Math.abs(tb - ta) < 0.01 ? '<span class="rt-durum rt-ok">Borç = Alacak</span>' : '<span class="rt-durum rt-hata">Dengesiz</span>'}</td><td class="s">${tl(tb)}</td><td class="s">${tl(ta)}</td></tr></tbody></table>`; }
  function drawerAc(no) {
    const k = V.kayitlar.find(x => x.no === no); if (!k) return;
    $('#pyDrawerBaslik').innerHTML = `<i class="fas fa-file-invoice-dollar me-1"></i>${esc(k.no)} · ${esc(k.adSoyad)} ${durumRozet(k.durum)}`;
    $('#pyDrawerAksiyon').innerHTML = `${k.durum === 'Kaydedildi' ? `<button type="button" class="btn btn-warning btn-sm py-0" data-act="fatura"><i class="fas fa-file-invoice me-1"></i>Fatura Oluştur</button>` : ''}${k.durum !== 'İptal' ? `<button type="button" class="btn btn-light btn-sm py-0" data-act="iptal" style="color:#b91c1c"><i class="fas fa-ban me-1"></i>İptal</button>` : ''}<a class="btn btn-light btn-sm py-0" href="/PersonelMaliyet?kod=${encodeURIComponent(k.cariKodu)}" target="_blank"><i class="fas fa-id-card me-1"></i>Personel Kartı</a>`;
    const m = [['Yansıtma No', k.no], ['Dönem', k.donemAdi], ['Personel', `${k.adSoyad} (${k.cariKodu})`], ['Görev', k.gorev], ['Kaynak (SGK İşvereni)', k.kaynakSirket], ['Hedef (Fiili Görev Yeri)', k.hedefSirket], ['Pay', '%' + pct(k.pay)], ['Net / KDV / Toplam', `${tl(k.net)} / ${tl(k.kdv)} / ${tl(k.toplam)} TL`], ['Kaynak Fişi', k.kaynakFisNo], ['Hedef Fişi', k.hedefFisNo], ['Fatura', k.faturaNo || '—'], ['Oluşturan', `${k.olusturan} · ${k.zaman}`]];
    $('#pyDrawerIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-receipt me-1"></i>Yansıtma Bilgileri</span><span class="not">${k.durum === 'İptal' ? 'İptal: ' + esc(k.iptalNedeni) : 'Her iki şirkete de kayıt atıldı'}</span></div><div class="na-kart-g"><div class="py-master">${m.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${esc(v)}</div></div>`).join('')}</div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-list me-1"></i>Kalemler (GDR ↔ YNS)</span><span class="not">%${pct(k.pay)} pay uygulanmış tutarlar</span></div><div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo py-kalem" data-dinamik="kapali"><thead><tr><th>Maliyet Alanı</th><th>X/R</th><th>Gider Kodu</th><th>Gider Kalemi</th><th>Yansıtma Kodu</th><th>Yansıtma Gelir Kalemi</th><th class="s">Tutar</th></tr></thead><tbody>${k.kalemler.map(x => `<tr><td>${esc(x.alanAdi)}</td><td>${esc(x.aktarimTipi || '—')}</td><td class="kod">${esc(x.giderKodu)}</td><td>${esc(x.giderKalem)}</td><td class="kod">${esc(x.yansitmaKodu)}</td><td>${esc(x.yansitmaKalem)}</td><td class="s">${tl(x.tutar)}</td></tr>`).join('')}</tbody></table></div></div></div>
      <div class="row g-2"><div class="col-lg-6"><div class="py-fis-bas k"><i class="fas fa-building"></i>${esc(k.kaynakSirket)} · Kaynak Fişi<small>${esc(k.kaynakFisNo)} · Yansıtma Geliri</small></div>${fisTablo(k.kaynakFis)}</div><div class="col-lg-6"><div class="py-fis-bas h"><i class="fas fa-building-user"></i>${esc(k.hedefSirket)} · Hedef Fişi<small>${esc(k.hedefFisNo)} · Personel Gideri</small></div>${fisTablo(k.hedefFis)}</div></div>
      <div class="na-kart mt-2"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Geçmiş</span></div><div class="na-kart-g"><ul class="py-gecmis mb-0 ps-3">${k.gecmis.map(g => `<li>${esc(g)}</li>`).join('')}</ul></div></div>`;
    $$('#pyDrawerAksiyon [data-act]').forEach(b => b.addEventListener('click', async () => {
      if (b.dataset.act === 'fatura') { const j = await post('/PersonelYansitma/FaturaOlustur', { no: k.no, donem: donem() }); if (!j.success) { mesaj('danger', esc(j.message)); return; } V = j; veriUygula(); mesaj('success', esc(j.message)); drawerAc(k.no); }
      if (b.dataset.act === 'iptal') { const neden = window.prompt('İptal nedeni (zorunlu):', ''); if (neden === null) return; const j = await post('/PersonelYansitma/Iptal', { no: k.no, neden, donem: donem() }); if (!j.success) { mesaj('danger', esc(j.message)); return; } V = j; veriUygula(); mesaj('success', esc(j.message)); drawerKapat(); }
    }));
    $('#pyDrawer').classList.add('acik'); $('#pyPerde').classList.add('acik');
  }
  function drawerKapat() { $('#pyDrawer').classList.remove('acik'); $('#pyPerde').classList.remove('acik'); }
  $('#pyDrawerKapat').addEventListener('click', drawerKapat); $('#pyPerde').addEventListener('click', drawerKapat);

  // ---- sekme 3: kalem eşleme + katalog + KDV
  function cizEsleme() {
    const secenek = kod => `<select class="form-select" data-alan-kod>${V.katalog.map(k => `<option value="${esc(k.giderKodu)}" ${k.giderKodu === kod ? 'selected' : ''}>${esc(k.giderKodu)} · ${esc(k.kalem)}</option>`).join('')}</select>`;
    $('#pyEslemeTablo tbody').innerHTML = V.esleme.map(e => `<tr data-alan="${esc(e.alan)}"><td><b>${esc(e.alanAdi)}</b></td><td>${esc(e.aktarimTipi || '—')}</td><td>${secenek(e.giderKodu)}</td><td>${esc(e.giderAltGrup)} › ${esc(e.giderKalem)}</td><td class="kod">${esc(e.yansitmaKodu)}</td><td>${esc(e.yansitmaKalem)}</td></tr>`).join('');
    $$('#pyEslemeTablo select[data-alan-kod]').forEach(s => s.addEventListener('change', async () => { const j = await post('/PersonelYansitma/EslemeKaydet', { alan: s.closest('tr').dataset.alan, giderKodu: s.value, donem: donem() }); if (!j.success) { mesaj('danger', esc(j.message)); return; } V = j; veriUygula(); mesaj('success', esc(j.message)); }));
    $('#pyKatalog tbody').innerHTML = V.katalog.map(k => `<tr><td class="kod">${esc(k.giderKodu)}</td><td>${esc(k.altGrup)}</td><td>${esc(k.kalem)}</td><td class="kod">${esc(k.yansitmaKodu)}</td><td>${esc(k.yansitmaKalem)}</td></tr>`).join('');
    $('#pyKatalogSayi').textContent = `${V.katalog.length} kalem · GDR.13.AA.KK ↔ YNS.13.AA.KK`;
  }
  $('#pyKdvKaydet').addEventListener('click', async () => { const j = await post('/PersonelYansitma/KdvKaydet', { kdv: Number($('#pyKdv').value) || 0, donem: donem() }); if (!j.success) { mesaj('danger', esc(j.message)); return; } V = j; veriUygula(); mesaj('success', esc(j.message)); });

  // ---- başlat
  $$('#pySekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  sekme(kok.dataset.baslangic || 'hazirlik');
  yukle().then(() => { window.NA_HAZIR = true; }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
