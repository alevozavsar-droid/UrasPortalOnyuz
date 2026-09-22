// ============================================================
// YANSITMA MODÜLÜ — ekran betiği (Views/GrupIci/Yansitma.cshtml; kurallar Data/YansitmaOrnek.cs; uçlar Controllers/YansitmaController.cs)
// Kaynak: URAS HOLDING Yansıtma Modülü IT Teknik ve İşlevsel Gereksinim Dokümanı v1.0
//   Sekme 1 İşlem Ekranı  : kaynak fatura seç → kalemleri TEK TEK işaretle → miktar / kâr % / (yetkiliyse) birim yansıtma → özet anında
//                           güncellenir → muhasebe kaydı simülasyonu (alt panel) → taslak kaydet / onaya gönder; detay, onay kuyruğu
//   Sekme 2 Raporu        : filtreler (§6.1), KPI (§6.4), ana grid (§6.2), satıra tıklayınca detay drawer (§6.3), Excel
//   Sekme 3 Muhasebe Kont.: kontrol listesi (§7.1), beklenen / gerçekleşen karşılaştırması (§7.2), cari mutabakat (§7.5), kurallar, eşleme tablosu
// Hesaplama (§3.1) ekranda anında yapılır; sunucu aynı formülle doğrular, validasyon / onay akışı / simülasyonu döner (/Yansitma/Onizleme).
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('yn'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const bh = s => { s = String(s ?? ''); return s ? s.charAt(0).toLocaleUpperCase('tr-TR') + s.slice(1) : s; };
  const para = (v, b = 2) => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: b, maximumFractionDigits: b });
  const mik = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 3 });
  const pct = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 2 }) + ' %';
  const r2 = v => Math.round((Number(v) || 0) * 100) / 100, r6 = v => Math.round((Number(v) || 0) * 1e6) / 1e6;
  const iso2tr = s => /^\d{4}-\d{2}-\d{2}$/.test(s || '') ? s.split('-').reverse().join('.') : (s ?? '');
  const bugunIso = new Date().toISOString().slice(0, 10);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const DURUM_TIP = { 'Taslak': 'notr', 'Onay Bekliyor': 'bekle', 'Onaylandı': 'bilgi', 'Fatura Oluştu': 'bilgi', 'Muhasebeleşti': 'bilgi', 'Uyumlu': 'ok', 'Reddedildi': 'hata', 'İptal': 'hata' };
  const durumRozet = d => rozet(d, DURUM_TIP[d] || 'notr');
  const KS_TIP = { 'UYUMLU': 'ok', 'KONTROL GEREKLİ': 'uyari', 'HESAP HATASI': 'kritik', 'TUTAR FARKI': 'kritik', 'KDV FARKI': 'kritik', 'CARİ HESAP HATASI': 'kritik', 'MASRAF MERKEZİ HATASI': 'uyari', 'PROJE HATASI': 'uyari', 'MUHASEBE KAYDI YOK': 'kritik', 'MÜKERRER KAYIT': 'kritik' };
  const KRITIK = ['TUTAR FARKI', 'KDV FARKI', 'CARİ HESAP HATASI', 'MUHASEBE KAYDI YOK', 'MÜKERRER KAYIT', 'HESAP HATASI'];
  const ksRozet = d => !d ? '<span class="yn-ks yok">KONTROL YOK</span>' : `<span class="yn-ks ${KS_TIP[d] || 'bilgi'}">${esc(d)}</span>`;
  const muhasebeDurumu = t => t.durum === 'Uyumlu' || t.durum === 'Muhasebeleşti' ? 'Muhasebeleşti' : t.durum === 'Fatura Oluştu' ? (t.kontrolDurumu === 'MUHASEBE KAYDI YOK' ? 'Fiş bekliyor (hata)' : 'Fiş bekliyor') : t.durum === 'Onaylandı' ? 'Fatura bekliyor' : '—';
  const SENARYOLAR = [['uyumlu', 'Uyumlu fiş'], ['hesap', 'Hesap kodu farklı (770 ↔ 760)'], ['kdv', 'KDV oranı / tutarı farklı'], ['tutar', 'Net tutar farklı'], ['cari', 'Yanlış karşı şirket carisi'], ['masraf', 'Masraf merkezi farklı'], ['referans', 'Fişte yansıtma no referansı yok'], ['mukerrer', 'Mükerrer fiş'], ['kur', 'Kur toleransı dışında'], ['yok', 'Fiş bulunamadı']];

  let V = null, ROL = 'Operasyon Kullanıcısı', aktifNo = kok.dataset.no || '', onizZ = null, SON = null, simAcik = false, kontrolSecili = null;
  let F = yeniF(), FAT = null;   // F: form durumu, FAT: seçili kaynak fatura (kalemleriyle)
  function yeniF() { return { id: 0, no: '', durum: '', versiyon: 1, satirlar: {} }; }
  const yetki = e => ((V && V.yetkiler[ROL]) || []).includes(e);

  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 9000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  async function post(url, veri) { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }); return r.json(); }
  async function yukle() { const r = await fetch('/Yansitma/Veri'); V = await r.json(); }
  function guncelle(j) { if (j && j.islem) { const i = V.islemler.findIndex(x => x.no === j.islem.no); if (i >= 0) V.islemler[i] = j.islem; else V.islemler.unshift(j.islem); } cizHepsi(); }
  const bul = no => V.islemler.find(t => t.no === no);
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + bugunIso + '.csv'; a.click(); };

  // ============================================================
  // ÜST SEKMELER (İşlem Ekranı · Raporu · Muhasebe Kontrolü) ve alt sekmeler
  // ============================================================
  function ust(u) {
    $$('#giUst button').forEach(b => b.classList.toggle('aktif', b.dataset.u === u));
    ['islem', 'rapor', 'kontrol'].forEach(p => $('#gi' + p.charAt(0).toUpperCase() + p.slice(1)).classList.toggle('aktif', p === u));
    try { const url = new URL(location.href); if (u === 'islem') url.searchParams.delete('ust'); else url.searchParams.set('ust', u); history.replaceState(null, '', url.toString()); } catch (e) { }
    if (u === 'rapor') cizRapor(); if (u === 'kontrol') cizKontrol();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
  $$('#giUst button').forEach(b => b.addEventListener('click', () => ust(b.dataset.u)));
  function sekme(p) { $$('#ynSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#giIslem > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); }
  $$('#ynSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));

  // ============================================================
  // BAŞLIK, ROL, KPI, PARAMETRELER
  // ============================================================
  function cizBaslik() {
    const rs = $('#ynRol'); if (!rs.options.length) { rs.innerHTML = V.roller.map(r => `<option>${esc(r)}</option>`).join(''); const b = V.ben.rol; ROL = b === 'Muhasebe' ? 'Muhasebe' : b === 'Yönetici' || b === 'Üst Yönetim' ? 'Yönetici / Onaycı' : b === 'Finans' ? 'Yönetici / Onaycı' : 'Operasyon Kullanıcısı'; rs.value = ROL; }
    $('#ynBen').textContent = `· ${V.ben.adSoyad} (${V.ben.sirket} / ${V.ben.departman})`;
    const sec = (id, l, ilk) => { const e = $(id); const v = e.value; e.innerHTML = (ilk ? `<option value="">${ilk}</option>` : '') + l.map(x => `<option value="${esc(x)}">${esc(x || '—')}</option>`).join(''); if (v) e.value = v; };
    sec('#ynYansitan', V.sirketler, 'Seçiniz…'); sec('#ynYansitilan', V.sirketler, 'Seçiniz…'); sec('#ynTur', V.turler); sec('#ynKaynakBelgeTuru', V.kaynakBelgeTurleri); sec('#ynMasrafMerkezi', V.masrafMerkezleri, 'Seçiniz…'); sec('#ynProje', V.projeler.filter(Boolean), 'Yok');
    if (!$('#ynMmList')) { const d = document.createElement('datalist'); d.id = 'ynMmList'; d.innerHTML = V.masrafMerkezleri.map(m => `<option value="${esc(m)}">`).join(''); document.body.appendChild(d); }
    $('#ynParam').innerHTML = Object.keys(V.parametreler).map(k => `<div class="na-param mb-1"><span>${esc(V.parametreEtiketler[k] || k)}:</span><input class="form-control" data-p="${k}" value="${esc(V.parametreler[k])}" style="width:90px"></div>`).join('') + '<button type="button" class="btn btn-light btn-sm py-0 mt-1" id="ynParamKaydet">Kaydet</button>';
    $('#ynParamKaydet').addEventListener('click', async () => { const d = {}; $$('#ynParam [data-p]').forEach(i => d[i.dataset.p] = i.value); const j = await post('/Yansitma/Parametre', d); V.parametreler = j.parametreler; onizleme(); mesaj('success', 'Parametreler güncellendi; onay akışı yeniden hesaplanır.'); });
    $('#ynListeDurum').innerHTML = '<option value="">Durum: Tümü</option>' + V.durumlar.map(d => `<option>${esc(d)}</option>`).join('');
    $('#ynKontrolDurumF').innerHTML = '<option value="">Kontrol durumu: Tümü</option><option value="_yok">Henüz kontrol yok</option>' + V.kontrolDurumlari.map(d => `<option>${esc(d)}</option>`).join('');
    const ms = (id, l) => { $(id).innerHTML = l.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); };
    if (!$('#ynRfYansitan').options.length) { ms('#ynRfYansitan', V.sirketler); ms('#ynRfYansitilan', V.sirketler); ms('#ynRfTur', V.turler); ms('#ynRfDurum', V.durumlar); ms('#ynRfKontrol', ['(kontrol yok)', ...V.kontrolDurumlari]); }
    $('#ynEsleme tbody').innerHTML = V.esleme.map(e => `<tr class="${e.aktif ? '' : 'text-muted'}"><td>${e.id}</td><td>${esc(e.sirket === '*' ? 'Tümü' : e.sirket)}</td><td>${esc(e.yansitmaTuru === '*' ? 'Tümü' : e.yansitmaTuru)}</td><td>${esc(e.kategori === '*' ? 'Tümü' : e.kategori)}</td><td><code>${esc(e.giderHesap)}</code> ${esc(e.giderHesapAdi)}</td><td><code>${esc(e.gelirHesap)}</code> ${esc(e.gelirHesapAdi)}</td><td>${esc(e.kdvKodu)}</td><td>${e.masrafMerkeziZorunlu ? '<i class="fas fa-check text-success"></i>' : ''}</td><td>${e.projeZorunlu ? '<i class="fas fa-check text-success"></i>' : ''}</td><td>${esc(e.gecerlilik)}</td><td>${e.aktif ? rozet('Aktif', 'ok') : rozet('Pasif', 'hata')}</td></tr>`).join('');
    $('#ynKontrolLegend').innerHTML = [['UYUMLU', 'Tüm kritik kontroller geçti. Blokaj yok.'], ['KONTROL GEREKLİ', 'Kritik olmayan fark ya da tolerans dışı durum (kur, para birimi, belge referansı). Opsiyonel.'], ['HESAP HATASI', 'Gerçek hesap beklenen hesaptan farklı. Düzeltme gerekir.'], ['TUTAR FARKI', 'Net / borç / alacak tutarı farklı. Kritik.'], ['KDV FARKI', 'KDV oranı / tutarı / vergi hesabı farklı. Kritik.'], ['CARİ HESAP HATASI', 'Yanlış karşı şirket carisi. Kritik.'], ['MASRAF MERKEZİ HATASI', 'Beklenen masraf merkezi farklı ya da boş. Politikaya bağlı.'], ['PROJE HATASI', 'Beklenen proje farklı ya da boş. Politikaya bağlı.'], ['MUHASEBE KAYDI YOK', 'Yansıtma faturası var ancak fiş bulunamadı. Kritik.'], ['MÜKERRER KAYIT', 'Aynı işlem için birden fazla aktif kayıt. Kritik.']].map(([d, a]) => `<li>${ksRozet(d)} ${esc(a)}</li>`).join('');
    $('#ynKontrolKurallar').innerHTML = ['Borç = Alacak: her şirket fişi kendi içinde dengeli olmalı.', 'Net tutar: simülasyon neti ile gerçekleşen net kayıt eşleşmeli (tolerans parametrik).', 'KDV tutarı ve oranı: 191 / 391 vergi hesabı ve vergi kodu tutarlı olmalı.', 'Hesap kodu: beklenen eşleme sonucu ile gerçek hesap aynı olmalı.', 'Cari hesap: karşı grup şirketinin cari kodu doğru olmalı.', 'Masraf merkezi ve proje: zorunlu hesaplarda aynı olmalı.', 'Para birimi ve kur: işlem dövizi fiş dövizi ile aynı; kur tolerans dahilinde.', 'Belge referansı: yansıtma no ve kaynak belge referansı fişte bulunmalı.', 'Mükerrer fiş: aynı yansıtma için birden fazla aktif muhasebe kaydı olmamalı.', 'Şirketler arası karşı kayıt: yansıtan alacağı ile yansıtılan borcu eşleşmeli.'].map(k => `<li>${esc(k)}</li>`).join('');
  }
  $('#ynRol').addEventListener('change', () => { ROL = $('#ynRol').value; yetkiUygula(); cizKalemler(); if (aktifNo) detayAc(aktifNo, true); if (kontrolSecili) cizKarsilastirma(kontrolSecili); });
  function yetkiUygula() {
    $('#ynYeni').style.display = yetki('yeni') ? '' : 'none'; $('#ynTaslak').style.display = yetki('taslak') ? '' : 'none'; $('#ynGonder').style.display = yetki('onayaGonder') ? '' : 'none'; $('#ynIptalBtn').style.display = yetki('iptal') && F.id ? '' : 'none';
    $('#ynFaturaSec').disabled = !yetki('faturaSec');
  }
  const buAy = t => (t.tarih || '').slice(0, 7) === bugunIso.slice(0, 7);
  function cizKpi() {
    const L = V.islemler, n = f => L.filter(f), tl = l => para(l.reduce((s, t) => s + t.ozet.toplamYansitmaTl, 0), 0) + ' TL';
    const k = [
      ['#64748b', 'Taslak', n(t => t.durum === 'Taslak'), 'Kaydedilmiş, Gönderilmemiş', 'Taslak'],
      ['#f59e0b', 'Onay Bekleyen', n(t => t.durum === 'Onay Bekliyor'), null, '_onay'],
      ['#0ea5e9', 'Fatura Oluşmamış', n(t => t.durum === 'Onaylandı'), 'Onaylı, ERP Belgesi Yok', 'Onaylandı'],
      ['#6366f1', 'Muhasebeleşmemiş', n(t => t.durum === 'Fatura Oluştu'), 'Fatura Var, Yevmiye Yok', 'Fatura Oluştu'],
      ['#dc2626', 'Kontrol Gerekli', n(t => t.kontrolDurumu && t.kontrolDurumu !== 'UYUMLU'), 'Beklenen ≠ Gerçekleşen', '_kontrol'],
      ['#16a34a', 'Bu Ay Net Yansıtma', n(t => buAy(t) && t.aktif), null, '_ay']
    ];
    $('#ynKpi').innerHTML = k.map(([r, b, l, alt, e]) => `<div style="--k:${r}" data-e="${e}"><div class="l">${b}</div><div class="v">${e === '_ay' ? tl(l) : l.length + ' İşlem'}</div><div class="s">${esc(bh(alt || tl(l)))}</div></div>`).join('');
    $$('#ynKpi > div').forEach(d => d.addEventListener('click', () => { const e = d.dataset.e; if (e === '_onay') { sekme('onay'); return; } if (e === '_kontrol') { ust('kontrol'); return; } $('#ynListeDurum').value = e.startsWith('_') ? '' : e; cizListe(); sekme('liste'); }));
    $('#ynNListe').textContent = L.length; $('#ynNOnay').textContent = L.filter(t => t.durum === 'Onay Bekliyor').length; $('#ynRaporN').textContent = L.length;
    const sorun = L.filter(t => t.kontrolDurumu && t.kontrolDurumu !== 'UYUMLU').length; $('#ynKontrolN').textContent = sorun ? sorun + ' fark' : 'uyumlu';
  }

  // ============================================================
  // FORM — başlık alanları, kaynak fatura, kalemler (§4.2–4.9)
  // ============================================================
  const H = { tarih: '#ynTarih', yansitan: '#ynYansitan', yansitilan: '#ynYansitilan', tur: '#ynTur', kaynakBelgeTuru: '#ynKaynakBelgeTuru', masrafMerkezi: '#ynMasrafMerkezi', proje: '#ynProje', departman: '#ynDepartman', aciklama: '#ynAciklama' };
  function formSifirla() {
    F = yeniF(); FAT = null; SON = null; simAcik = false;
    $('#ynNo').value = ''; $('#ynTarih').value = bugunIso; $('#ynYansitan').value = V.sirketler.includes(V.ben.sirket) ? V.ben.sirket : ''; $('#ynYansitilan').value = ''; $('#ynTur').value = 'Fatura'; $('#ynKaynakBelgeTuru').value = 'Alış Faturası';
    $('#ynParaBirimi').value = 'TRY'; $('#ynKur').value = 1; $('#ynKur').disabled = true; $('#ynMasrafMerkezi').value = ''; $('#ynProje').value = ''; $('#ynDepartman').value = V.ben.departman || ''; $('#ynAciklama').value = '';
    $('#ynSimKart').style.display = 'none'; cizFaturaOzeti(); cizKalemler(); durumRozeti(); yetkiUygula();
  }
  function durumRozeti() { $('#ynDurumRozet').textContent = F.id ? `${F.no} · ${F.durum}${F.versiyon > 1 ? ' · v' + F.versiyon : ''}` : 'Yeni · kaydedilmedi'; $('#ynNo').value = F.no || ''; $('#ynIptalBtn').style.display = yetki('iptal') && F.id ? '' : 'none'; }
  function girisYap() {
    const g = { id: F.id, rol: ROL, kaynakFaturaId: FAT ? FAT.id : 0, paraBirimi: $('#ynParaBirimi').value, kur: Number($('#ynKur').value) || 1, kalemler: [] };
    Object.keys(H).forEach(k => g[k] = $(H[k]).value);
    if (FAT) FAT.kalemler.forEach(k => { const s = F.satirlar[k.satirNo]; if (!s || !s.secili) return; g.kalemler.push({ kaynakFaturaId: FAT.id, satirNo: k.satirNo, yansitilacakMiktar: Number(s.miktar) || 0, karPct: Number(s.karPct) || 0, birimYansitma: s.manuel ? Number(s.birimYansitma) : null, manuelFiyat: !!s.manuel, kdvPct: s.kdvPct == null ? null : Number(s.kdvPct), masrafMerkezi: s.masrafMerkezi || '', proje: s.proje || '', gerekce: s.gerekce || '', dagitim: (s.dagitim || []).map(d => ({ sirket: d.sirket, miktar: Number(d.miktar) || 0, karPct: Number(d.karPct) || 0 })) }); });
    return g;
  }
  // satır hesabı (§3.1) — sunucuyla aynı formüller
  function hesapSatir(k, s) {
    const bm = k.birimMaliyet; let kar = Number(s.karPct) || 0, birim;
    if (s.manuel && s.birimYansitma != null && s.birimYansitma !== '') { birim = r6(s.birimYansitma); kar = bm > 0 ? r6((birim - bm) / bm * 100) : 0; } else birim = r6(bm + bm * kar / 100);
    let miktar = Number(s.miktar) || 0, toplam;
    if (s.dagitim && s.dagitim.length) { miktar = s.dagitim.reduce((a, d) => a + (Number(d.miktar) || 0), 0); toplam = s.dagitim.reduce((a, d) => a + r2((Number(d.miktar) || 0) * r6(bm * (1 + (Number(d.karPct) || 0) / 100))), 0); }
    else toplam = r2(miktar * birim);
    const kdvPct = s.kdvPct == null || s.kdvPct === '' ? k.kdvPct : Number(s.kdvPct), maliyet = r2(miktar * bm), kdv = r2(toplam * kdvPct / 100);
    return { miktar, kar, birim, birimKar: r6(bm * kar / 100), maliyet, toplam, kdvPct, kdv, brut: r2(toplam + kdv), karT: r2(toplam - maliyet), kalan: k.kalan, sonrasi: k.kalan - miktar };
  }
  function cizFaturaOzeti() {
    const e = $('#ynFaturaOzet');
    if (!FAT) { e.innerHTML = '<div class="na-bos">Kaynak belge seçilmedi. Yansıtan şirketi seçip <b>Kaynak Fatura Seç</b> ile faturayı açın; ardından yansıtılacak kalemleri tek tek işaretleyin.</div>'; $('#ynFaturaNot').textContent = 'Kaynak Fatura Seç ile yansıtan şirketin faturalarından birini seçin'; return; }
    $('#ynFaturaNot').textContent = `${FAT.faturaTuru} · ERP belge id ${FAT.id} · muhasebe fişi ${FAT.muhasebeFisNo}`;
    const c = [['Fatura No', FAT.faturaNo], ['Fatura Tarihi', FAT.tarihTr], ['Tedarikçi', FAT.tedarikci], ['Kategori', FAT.kategori], ['Para Birimi / Kur', FAT.paraBirimi + (FAT.paraBirimi !== 'TRY' ? ' · ' + para(FAT.kur, 4) : '')], ['Kullanıcı Firma', FAT.kullanimSirketi], ['Net Tutar', para(FAT.net) + ' ' + FAT.paraBirimi], ['KDV', para(FAT.kdv)], ['Genel Toplam', para(FAT.genelToplam)], ['Daha Önce Yansıtılan (net)', para(FAT.yansitilanTutar)], ['Kalan Yansıtılabilir (net)', para(FAT.kalanYansitilabilir)], ['Açıklama', FAT.aciklama]];
    e.innerHTML = `<div class="yn-fatura">${c.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${esc(v)}</div></div>`).join('')}</div>`;
  }
  function cizKalemler() {
    const tb = $('#ynKalemler tbody'), tf = $('#ynKalemler tfoot'), ara = ($('#ynKalemAra').value || '').toLocaleLowerCase('tr-TR');
    if (!FAT) { tb.innerHTML = '<tr><td colspan="20" class="na-bos">Kaynak fatura seçildiğinde satırlar burada listelenir.</td></tr>'; tf.innerHTML = ''; ozetCiz(); return; }
    const duzen = F.durum === '' || F.durum === 'Taslak' || F.durum === 'Reddedildi', mf = yetki('manuelFiyat') && duzen, kdvY = yetki('esleme') && duzen;
    tb.innerHTML = FAT.kalemler.filter(k => !ara || (k.ad + ' ' + k.kod).toLocaleLowerCase('tr-TR').includes(ara)).map(k => {
      const s = F.satirlar[k.satirNo] || (F.satirlar[k.satirNo] = { secili: false, miktar: '', karPct: Number($('#ynTopluKar').value) || 0, birimYansitma: null, manuel: false, kdvPct: null, masrafMerkezi: '', proje: '', gerekce: '', dagitim: [] });
      const h = hesapSatir(k, s), dag = s.dagitim && s.dagitim.length, kilitli = k.kalan <= 0, dis = !s.secili || !duzen || kilitli;
      return `<tr data-satir="${k.satirNo}" class="${s.secili ? 'secili' : ''} ${kilitli ? 'pasif' : ''}">
        <td><input type="checkbox" class="sec" ${s.secili ? 'checked' : ''} ${kilitli || !duzen ? 'disabled' : ''} title="${kilitli ? 'Kalan miktar yok: bu satır tamamen yansıtılmış' : 'Yalnız işaretli satırlar yansıtılır'}"></td>
        <td>${k.satirNo}</td><td title="${esc(k.ad)}">${esc(k.ad)}</td><td><code style="font-size:.64rem">${esc(k.kod)}</code></td>
        <td class="s">${mik(k.miktar)}</td><td class="s">${mik(k.oncedenYansitilan)}</td><td class="s"><b>${mik(k.kalan)}</b></td>
        <td class="s g"><input class="form-control" type="number" step="any" min="0" max="${k.kalan}" data-alan="miktar" value="${dag ? h.miktar : (s.miktar ?? '')}" ${dis || dag ? 'disabled' : ''} title="${dag ? 'Dağıtım toplamı' : '0 < miktar ≤ kalan'}"></td><td>${esc(k.birim)}</td>
        <td class="s">${para(k.birimMaliyet, 4)}<i class="fas fa-lock kilit" title="Kaynak net birim maliyet; salt okunur"></i></td><td class="s h" data-c="maliyet">${para(h.maliyet)}</td>
        <td class="s g"><input class="form-control k" type="number" step="0.01" data-alan="karPct" value="${dag ? '' : h.kar}" ${dis || dag ? 'disabled' : ''} placeholder="${dag ? 'dağıtım' : ''}"></td><td class="s h" data-c="birimKar">${para(h.birimKar, 4)}</td>
        <td class="s g"><input class="form-control ${s.manuel ? 'manuel' : ''}" type="number" step="0.0001" data-alan="birimYansitma" value="${dag ? '' : h.birim}" ${dis || dag || !mf ? 'disabled' : ''} title="${mf ? 'Manuel değiştirilirse Kâr % geriye doğru hesaplanır' : 'Yalnız yetkili rol değiştirebilir (Yönetici / Muhasebe / Sistem)'}"></td><td class="s h" data-c="toplam"><b>${para(h.toplam)}</b></td>
        <td class="s g"><input class="form-control k" type="number" step="1" data-alan="kdvPct" value="${h.kdvPct}" ${dis || !kdvY ? 'disabled' : ''} title="Kaynak / vergi kuralından gelir; yetkili değişiklik loglanır"></td><td class="s h" data-c="kdv">${para(h.kdv)}</td><td class="s h" data-c="brut">${para(h.brut)}</td>
        <td class="g"><input class="form-control mm" list="ynMmList" data-alan="masrafMerkezi" value="${esc(s.masrafMerkezi)}" ${dis ? 'disabled' : ''} placeholder="Başlıktaki"></td>
        <td><button type="button" class="btn btn-light btn-sm dagit" ${dis ? 'disabled' : ''}><i class="fas fa-diagram-project me-1"></i>Şirketlere Dağıt</button>${dag ? s.dagitim.map(d => `<span class="dag">${esc(d.sirket)}: ${mik(d.miktar)} ${esc(k.birim)} · %${d.karPct}</span>`).join('') : ''}${s.manuel ? '<span class="dag" style="color:#b45309">manuel fiyat' + (s.gerekce ? ': ' + esc(s.gerekce) : '') + '</span>' : ''}</td></tr>`;
    }).join('') || '<tr><td colspan="20" class="na-bos">Filtreye uyan kalem yok.</td></tr>';
    // olaylar
    $$('#ynKalemler tbody tr').forEach(tr => {
      const n = +tr.dataset.satir, s = F.satirlar[n], k = FAT.kalemler.find(x => x.satirNo === n);
      const cb = $('.sec', tr); if (cb) cb.addEventListener('change', () => { s.secili = cb.checked; if (s.secili && (s.miktar === '' || s.miktar == null)) s.miktar = k.kalan; cizKalemler(); onizleme(); });
      $$('input[data-alan]', tr).forEach(i => i.addEventListener('input', () => {
        const a = i.dataset.alan;
        if (a === 'birimYansitma') { s.manuel = i.value !== '' && Math.abs(Number(i.value) - r6(k.birimMaliyet * (1 + (Number(s.karPct) || 0) / 100))) > 1e-6; s.birimYansitma = i.value === '' ? null : Number(i.value); if (s.manuel && !s.gerekce) s.gerekce = 'Manuel birim fiyat'; i.classList.toggle('manuel', s.manuel); }
        else if (a === 'karPct') { s.karPct = i.value; s.manuel = false; s.birimYansitma = null; }
        else if (a === 'kdvPct') s.kdvPct = i.value; else s[a] = i.value;
        if (a === 'karPct' && Number(i.value) < 0 && !s.gerekce) s.gerekce = 'İskonto';
        satirGuncelle(tr, k, s); onizleme();
      }));
      const dg = $('.dagit', tr); if (dg) dg.addEventListener('click', () => dagitAc(k, s));
    });
    tfootCiz(); ozetCiz(); seciliSayac();
  }
  function satirGuncelle(tr, k, s) {
    const h = hesapSatir(k, s);
    $('[data-c=maliyet]', tr).textContent = para(h.maliyet); $('[data-c=birimKar]', tr).textContent = para(h.birimKar, 4); $('[data-c=toplam]', tr).innerHTML = '<b>' + para(h.toplam) + '</b>'; $('[data-c=kdv]', tr).textContent = para(h.kdv); $('[data-c=brut]', tr).textContent = para(h.brut);
    const mi = $('[data-alan=miktar]', tr); mi.classList.toggle('is-invalid', h.miktar <= 0 || h.miktar > k.kalan);
    const bi = $('[data-alan=birimYansitma]', tr), ki = $('[data-alan=karPct]', tr);
    if (document.activeElement !== bi) bi.value = h.birim; if (document.activeElement !== ki) ki.value = r2(h.kar);
    bi.classList.toggle('manuel', !!s.manuel);
    if (s.manuel) s.karPct = h.kar;
    tfootCiz(); ozetCiz();
  }
  const secililer = () => FAT ? FAT.kalemler.filter(k => F.satirlar[k.satirNo] && F.satirlar[k.satirNo].secili).map(k => ({ k, s: F.satirlar[k.satirNo], h: hesapSatir(k, F.satirlar[k.satirNo]) })) : [];
  function tfootCiz() {
    const l = secililer(), t = { maliyet: 0, toplam: 0, kdv: 0, brut: 0 }; l.forEach(x => { t.maliyet += x.h.maliyet; t.toplam += x.h.toplam; t.kdv += x.h.kdv; t.brut += x.h.brut; });
    $('#ynKalemler tfoot').innerHTML = FAT ? `<tr><td colspan="10">Seçili ${l.length} satır toplamı</td><td class="s">${para(t.maliyet)}</td><td></td><td></td><td></td><td class="s">${para(t.toplam)}</td><td></td><td class="s">${para(t.kdv)}</td><td class="s">${para(t.brut)}</td><td colspan="2"></td></tr>` : '';
  }
  function seciliSayac() { const n = secililer().length; $('#ynSeciliSayac').textContent = `${n} satır seçili` + (FAT ? ` / ${FAT.kalemler.length}`: ''); $('#ynHepsi').checked = FAT && n > 0 && n === FAT.kalemler.filter(k => k.kalan > 0).length; }
  function ozetCiz() {
    const l = secililer(), pb = FAT ? FAT.paraBirimi : 'TRY', kur = FAT && pb !== 'TRY' ? (Number($('#ynKur').value) || FAT.kur) : 1;
    const t = { m: 0, y: 0, k: 0, b: 0 }; l.forEach(x => { t.m += x.h.maliyet; t.y += x.h.toplam; t.k += x.h.kdv; }); t.b = t.y + t.k; const kar = t.y - t.m, ak = t.m > 0 ? kar / t.m * 100 : 0;
    const p = v => para(v) + ' ' + pb, tl = v => pb === 'TRY' ? '' : `<span class="l" style="grid-column:1/-1;text-align:right;font-size:.62rem">≈ ${para(v * kur, 0)} TL</span>`;
    $('#ynOzet').innerHTML = `<span class="l">Seçilen Kalem Sayısı</span><span class="v">${l.length}</span><span class="l">Toplam Maliyet</span><span class="v">${p(t.m)}</span>${tl(t.m)}<span class="l">Toplam Kâr</span><span class="v kar ${kar < 0 ? 'eksi' : ''}">${p(kar)}</span><span class="l">Ağırlıklı Ortalama Kâr %</span><span class="v kar ${kar < 0 ? 'eksi' : ''}">${pct(ak)}</span><span class="ayrac"></span><span class="l">Toplam Yansıtma Tutarı (net)</span><span class="v">${p(t.y)}</span>${tl(t.y)}<span class="l">KDV</span><span class="v">${p(t.k)}</span><span class="ayrac"></span><span class="l buyuk">Genel Toplam</span><span class="v buyuk">${p(t.b)}</span>${tl(t.b)}<span class="l" style="grid-column:1/-1;font-size:.62rem">Ağırlıklı kâr = Toplam Kâr / Toplam Maliyet × 100 (basit ortalama değil). Hedef: ${esc([...new Set(l.flatMap(x => x.s.dagitim && x.s.dagitim.length ? x.s.dagitim.map(d => d.sirket) : [$('#ynYansitilan').value || '—']))].join(', ') || '—')}</span>`;
    seciliSayac();
  }
  // toplu kâr, seçim, filtre
  $('#ynTopluKarUygula').addEventListener('click', () => { const v = Number($('#ynTopluKar').value) || 0; let n = 0; secililer().forEach(x => { if (x.s.dagitim && x.s.dagitim.length) { x.s.dagitim.forEach(d => d.karPct = v); } else { x.s.karPct = v; x.s.manuel = false; x.s.birimYansitma = null; } n++; }); cizKalemler(); onizleme(); mesaj('info', `Kâr oranı %${v} ${n} seçili kaleme uygulandı.`); });
  $('#ynTumunuSec').addEventListener('click', () => { if (!FAT) return; FAT.kalemler.forEach(k => { const s = F.satirlar[k.satirNo]; if (k.kalan > 0) { s.secili = true; if (!s.miktar) s.miktar = k.kalan; } }); cizKalemler(); onizleme(); });
  $('#ynHepsi').addEventListener('change', e => { if (!FAT) return; FAT.kalemler.forEach(k => { const s = F.satirlar[k.satirNo]; if (k.kalan > 0) { s.secili = e.target.checked; if (s.secili && !s.miktar) s.miktar = k.kalan; } }); cizKalemler(); onizleme(); });
  $('#ynTumunuKaldir').addEventListener('click', () => { Object.values(F.satirlar).forEach(s => s.secili = false); cizKalemler(); onizleme(); });
  $('#ynKalanDoldur').addEventListener('click', () => { secililer().forEach(x => { if (!(x.s.dagitim && x.s.dagitim.length)) x.s.miktar = x.k.kalan; }); cizKalemler(); onizleme(); });
  $('#ynKalemAra').addEventListener('input', cizKalemler);
  Object.values(H).forEach(sel => $(sel).addEventListener('change', () => { if (sel === '#ynYansitan' && FAT && FAT.sirket !== $('#ynYansitan').value) { FAT = null; F.satirlar = {}; cizFaturaOzeti(); cizKalemler(); mesaj('warning', 'Yansıtan şirket değişti: kaynak fatura ve kalemler temizlendi; yeniden fatura seçin.'); } if (sel === '#ynYansitilan') ozetCiz(); onizleme(); }));
  $('#ynKur').addEventListener('input', () => { ozetCiz(); onizleme(); });

  // ---- canlı önizleme: validasyon, onay akışı, simülasyon (sunucu)
  function onizleme() {
    clearTimeout(onizZ);
    onizZ = setTimeout(async () => {
      if (!V) return; const g = girisYap();
      if (!FAT || g.kalemler.length === 0) { $('#ynDogrulama').innerHTML = '<li class="text-muted">Kalem seçildikçe kurallar çalışır.</li>'; $('#ynDogrulamaNot').textContent = ''; $('#ynAkis').innerHTML = '<span class="text-muted">—</span>'; $('#ynAkisNedeni').textContent = ''; SON = null; if (simAcik) simCiz(null); return; }
      const j = await post('/Yansitma/Onizleme', g); SON = j;
      const h = j.hatalar || [], u = j.uyarilar || [];
      $('#ynDogrulama').innerHTML = (h.map(x => `<li class="h"><i class="fas fa-circle-xmark me-1"></i>${esc(x)}</li>`).join('') + u.map(x => `<li class="u"><i class="fas fa-triangle-exclamation me-1"></i>${esc(x)}</li>`).join('')) || '<li class="text-success"><i class="fas fa-circle-check me-1"></i>Kural ihlali yok; onaya gönderilebilir.</li>';
      $('#ynDogrulamaNot').textContent = h.length ? `${h.length} blokaj` : u.length ? `${u.length} uyarı` : 'temiz';
      $('#ynAkis').innerHTML = j.akis ? j.akis.map((a, i) => (i ? '<i class="fas fa-chevron-right"></i>' : '') + `<span title="${esc(a.kisi)}">${esc(a.adim)}</span>`).join('') : '<span class="text-muted">—</span>';
      $('#ynAkisNedeni').textContent = j.akisNedeni || '';
      if (simAcik) simCiz(j);
    }, 250);
  }
  // ---- simülasyon (alt panel)
  $('#ynSimBtn').addEventListener('click', () => { simAcik = true; $('#ynSimKart').style.display = ''; if (SON) simCiz(SON); else { simCiz(null); onizleme(); } $('#ynSimKart').scrollIntoView({ behavior: 'smooth', block: 'start' }); });
  function fisTablo(satirlar, sirket, opts = {}) {
    const l = satirlar.filter(x => x.sirket === sirket), b = l.reduce((s, x) => s + x.borc, 0), a = l.reduce((s, x) => s + x.alacak, 0), pb = l[0] && l[0].paraBirimi, dov = pb && pb !== 'TRY';
    const fis = opts.fis ? [...new Set(l.map(x => x.fisNo))].join(', ') : '';
    return `<div><div class="b"><span>${esc(sirket)} <small>· ${opts.taraf || ''}</small></span><small>${fis ? 'Fiş ' + esc(fis) + ' · ' : ''}${Math.abs(b - a) < 0.01 ? '<span class="text-success">borç = alacak</span>' : '<span class="text-danger">borç ≠ alacak</span>'}</small></div>
      <table class="yn-fis" data-dinamik="kapali"><thead><tr><th>Hesap</th><th>Hesap Adı / Detay</th><th class="s">Borç (TL)</th><th class="s">Alacak (TL)</th></tr></thead><tbody>
      ${l.map(x => `<tr><td><code>${esc(x.hesapKodu)}</code>${x.cariKod ? `<span class="k">cari ${esc(x.cariKod)}</span>` : ''}</td><td>${esc(x.hesapAdi)}<span class="k">${esc(x.kalemRef)} · ${esc(x.aciklama.split(' · ').slice(2).join(' · ') || x.aciklama)}${x.masrafMerkezi ? ' · MM ' + esc(x.masrafMerkezi) : ''}${x.proje ? ' · ' + esc(x.proje) : ''}${opts.kural !== false && x.kural ? ' · ' + esc(x.kural) : ''}${dov ? ' · ' + para(x.dovizTutar) + ' ' + esc(pb) + ' × ' + para(x.kur, 4) : ''}</span></td><td class="s">${x.borc ? para(x.borc) : ''}</td><td class="s">${x.alacak ? para(x.alacak) : ''}</td></tr>`).join('') || '<tr><td colspan="4" class="na-bos">satır yok</td></tr>'}
      </tbody><tfoot><tr class="top"><td colspan="2">TOPLAM</td><td class="s">${para(b)}</td><td class="s">${para(a)}</td></tr></tfoot></table></div>`;
  }
  function simHtml(satirlar, yansitan, opts) { const sirketler = [...new Set(satirlar.map(x => x.sirket))]; return sirketler.map(s => fisTablo(satirlar, s, Object.assign({ taraf: s === yansitan ? 'yansıtan · beklenen kayıt' : 'yansıtılan · beklenen kayıt' }, opts || {}))).join('') || '<div class="na-bos">Simülasyon satırı yok.</div>'; }
  function simCiz(j) {
    const ustEl = $('#ynSimUst'), simEl = $('#ynSim'), uy = $('#ynSimUyari');
    if (!j || !j.simulasyon || !j.simulasyon.length) { ustEl.innerHTML = ''; simEl.innerHTML = '<div class="na-bos">Kaynak fatura ve en az bir kalem seçildiğinde beklenen kayıtlar hesaplanır.</div>'; uy.innerHTML = j && j.simHatalar && j.simHatalar.length ? `<div class="na-uyari kirmizi">${j.simHatalar.map(esc).join('<br>')}</div>` : ''; return; }
    const o = j.ozet, pb = j.paraBirimi || 'TRY';
    ustEl.innerHTML = [['Yansıtma No', F.no || 'TASLAK (geçici)'], ['Yansıtan / Yansıtılan', `${$('#ynYansitan').value} → ${(o.hedefler || []).join(', ')}`], ['Kaynak Fatura', FAT ? `${FAT.faturaNo} · ${FAT.tarihTr} · ${FAT.tedarikci}` : '—'], ['Net / KDV / Genel Toplam', `${para(o.toplamYansitmaTl, 0)} / ${para(o.kdvTl, 0)} / ${para(o.genelToplamTl, 0)} TL`], ['Para Birimi / Kur', pb === 'TRY' ? 'TRY' : `${pb} · ${para(j.kur, 4)}`], ['Simülasyon Tarihi', j.simTarihi]].map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${esc(v)}</div></div>`).join('');
    simEl.innerHTML = simHtml(j.simulasyon, $('#ynYansitan').value);
    const h = j.simHatalar || [], u = (j.uyarilar || []).filter(x => /Dövizli|masraf merkezi eksik|proje eksik/i.test(x));
    uy.innerHTML = (h.length ? `<div class="na-uyari kirmizi"><b>Simülasyon blokajı:</b> ${h.map(esc).join(' · ')} — Onaya Gönder / Fatura Oluştur engellenir.</div>` : '') + (u.length ? `<div class="na-uyari sari">${u.map(esc).join('<br>')}</div>` : '') + '<div class="na-mini">Hesap kodları Mali İşler eşleme tablosundan gelir; snapshot onaya gönderme anında saklanır, kural sonradan değişse de geçmiş simülasyon bozulmaz.</div>';
  }

  // ---- Kaynak Fatura Seç penceresi (§4.3): sunucu tarafı arama + sayfalama
  let fmSayfa = 1, fmZ = null;
  function faturaModal() {
    const y = $('#ynYansitan').value; if (!y) { mesaj('warning', 'Önce yansıtan şirketi seçin; fatura listesi o şirkete göre filtrelenir.'); return; }
    $('#ynFmSirket').textContent = y; fmSayfa = 1; $('#ynFmAra').value = ''; fmYukle(); bootstrap.Modal.getOrCreateInstance($('#ynFaturaModal')).show();
  }
  async function fmYukle() {
    const y = $('#ynYansitan').value, r = await fetch(`/Yansitma/KaynakFaturalar?yansitan=${encodeURIComponent(y)}&ara=${encodeURIComponent($('#ynFmAra').value || '')}&haric=${F.id}&sayfa=${fmSayfa}&boy=12`), j = await r.json();
    $('#ynFmSayi').textContent = `${j.toplam} fatura`; $('#ynFmSayfa').textContent = `${fmSayfa} / ${Math.max(1, Math.ceil(j.toplam / j.boy))}`;
    $('#ynFmTablo tbody').innerHTML = j.faturalar.map(f => `<tr class="${f.kilitli ? 'kilit' : 'tik'} ${FAT && FAT.id === f.id ? 'sec' : ''}" data-id="${f.id}" title="${esc(f.kilitNedeni || 'Seçmek için tıklayın')}"><td>${f.kilitli ? '<i class="fas fa-lock"></i>' : `<input type="radio" name="fm" ${FAT && FAT.id === f.id ? 'checked' : ''}>`}</td><td><b>${esc(f.faturaNo)}</b><br><small class="text-muted">${esc(f.faturaTuru)} · ${f.kalemler.length} kalem</small></td><td>${esc(f.tarihTr)}</td><td>${esc(f.tedarikci)}</td><td style="max-width:220px;white-space:normal">${esc(f.aciklama)}</td><td>${esc(f.kategori)}</td><td class="s">${para(f.net)}</td><td class="s">${para(f.kdv)}</td><td class="s">${para(f.genelToplam)}</td><td>${esc(f.paraBirimi)}</td><td class="s">${para(f.yansitilanTutar)}</td><td class="s"><b>${para(f.kalanYansitilabilir)}</b></td><td>${f.kullanimSirketi !== y ? `<span class="na-bs" style="background:#fef3c7;color:#b45309">${esc(f.kullanimSirketi)}</span>` : esc(f.kullanimSirketi)}</td></tr>`).join('') || '<tr><td colspan="13" class="na-bos">Ölçüte uyan fatura yok.</td></tr>';
    $$('#ynFmTablo tr.tik').forEach(tr => tr.addEventListener('click', () => { const f = j.faturalar.find(x => x.id === +tr.dataset.id); faturaSec(f); bootstrap.Modal.getOrCreateInstance($('#ynFaturaModal')).hide(); }));
    fmYukle.son = j;
  }
  function faturaSec(f) {
    FAT = Object.assign({ sirket: $('#ynYansitan').value }, f); F.satirlar = {};
    $('#ynParaBirimi').value = f.paraBirimi; $('#ynKur').value = f.kur; $('#ynKur').disabled = f.paraBirimi === 'TRY' || !yetki('manuelFiyat');
    if (!$('#ynYansitilan').value && f.kullanimSirketi && f.kullanimSirketi !== FAT.sirket) $('#ynYansitilan').value = f.kullanimSirketi;
    if (!$('#ynMasrafMerkezi').value && V.masrafMerkezleri.includes(f.masrafMerkezi)) $('#ynMasrafMerkezi').value = f.masrafMerkezi;
    if (!$('#ynAciklama').value) $('#ynAciklama').value = `${f.faturaNo} · ${f.tedarikci} — ${f.aciklama}`.slice(0, 500);
    cizFaturaOzeti(); cizKalemler(); onizleme();
    mesaj('info', `<i class="fas fa-file-invoice me-1"></i>${esc(f.faturaNo)} seçildi: ${f.kalemler.length} kalem listelendi. Yansıtılacak satırları tek tek işaretleyin; miktar ve kâr oranını girin.`);
  }
  $('#ynFaturaSec').addEventListener('click', faturaModal);
  $('#ynFmAra').addEventListener('input', () => { clearTimeout(fmZ); fmZ = setTimeout(() => { fmSayfa = 1; fmYukle(); }, 300); });
  $('#ynFmOnce').addEventListener('click', () => { if (fmSayfa > 1) { fmSayfa--; fmYukle(); } });
  $('#ynFmSonra').addEventListener('click', () => { const j = fmYukle.son; if (j && fmSayfa * j.boy < j.toplam) { fmSayfa++; fmYukle(); } });

  // ---- Şirketlere Dağıt (§4.8)
  let dgK = null, dgS = null;
  function dagitAc(k, s) {
    dgK = k; dgS = s; $('#ynDgKalem').textContent = `Satır ${k.satirNo} · ${k.ad}`;
    $('#ynDgBilgi').innerHTML = `Kalan yansıtılabilir <b>${mik(k.kalan)} ${esc(k.birim)}</b> · birim maliyet <b>${para(k.birimMaliyet, 4)}</b>. Dağıtılan miktarların toplamı kalanı aşamaz; aynı satır aynı hedef şirkete tekrarlı dağıtılırsa uyarı verilir ve simülasyonda birleştirilir.`;
    const l = s.dagitim && s.dagitim.length ? s.dagitim.map(d => ({ ...d })) : [{ sirket: $('#ynYansitilan').value, miktar: Number(s.miktar) || k.kalan, karPct: Number(s.karPct) || 0 }];
    dgCiz(l); bootstrap.Modal.getOrCreateInstance($('#ynDagitModal')).show();
  }
  function dgCiz(l) {
    const y = $('#ynYansitan').value;
    $('#ynDgSatirlar').innerHTML = l.map((d, i) => `<div class="yn-dag-satir" data-i="${i}"><select class="form-select" data-a="sirket">${V.sirketler.filter(s => s !== y).map(s => `<option ${s === d.sirket ? 'selected' : ''}>${esc(s)}</option>`).join('')}</select><input class="form-control" type="number" step="any" min="0" data-a="miktar" value="${d.miktar}"><input class="form-control" type="number" step="0.01" data-a="karPct" value="${d.karPct}"><span class="s" data-c="birim"></span><span class="s" data-c="toplam"></span><button type="button" class="btn btn-light btn-sm py-0 sil"><i class="fas fa-xmark"></i></button></div>`).join('');
    const oku = () => $$('#ynDgSatirlar .yn-dag-satir').map(r => ({ sirket: $('[data-a=sirket]', r).value, miktar: Number($('[data-a=miktar]', r).value) || 0, karPct: Number($('[data-a=karPct]', r).value) || 0 }));
    const hesap = () => { const l2 = oku(); let top = 0; $$('#ynDgSatirlar .yn-dag-satir').forEach((r, i) => { const d = l2[i], bf = r6(dgK.birimMaliyet * (1 + d.karPct / 100)); $('[data-c=birim]', r).textContent = para(bf, 4); $('[data-c=toplam]', r).textContent = para(r2(d.miktar * bf)); top += d.miktar; }); const u = $('#ynDgUyari'); const tekrar = new Set(l2.map(d => d.sirket)).size !== l2.length; u.className = 'na-uyari ' + (top > dgK.kalan ? 'kirmizi' : tekrar ? 'sari' : 'mavi'); u.innerHTML = top > dgK.kalan ? `Toplam ${mik(top)} kalanı (${mik(dgK.kalan)}) aşıyor.` : tekrar ? `Toplam ${mik(top)} ${esc(dgK.birim)} · aynı hedef şirket birden fazla satırda; birleştirilecek.` : `Toplam ${mik(top)} ${esc(dgK.birim)} · işlem sonrası kalan ${mik(dgK.kalan - top)}.`; return { l2, top }; };
    $$('#ynDgSatirlar input, #ynDgSatirlar select').forEach(e => e.addEventListener('input', hesap));
    $$('#ynDgSatirlar .sil').forEach(b => b.addEventListener('click', () => { const l2 = oku(); l2.splice(+b.closest('.yn-dag-satir').dataset.i, 1); dgCiz(l2); }));
    dgCiz.oku = oku; hesap();
  }
  $('#ynDgEkle').addEventListener('click', () => { const l = dgCiz.oku(); const y = $('#ynYansitan').value; l.push({ sirket: V.sirketler.find(s => s !== y && !l.some(d => d.sirket === s)) || V.sirketler.find(s => s !== y), miktar: 0, karPct: Number(dgS.karPct) || 0 }); dgCiz(l); });
  $('#ynDgKaydet').addEventListener('click', () => { const l = dgCiz.oku().filter(d => d.miktar > 0); const top = l.reduce((a, d) => a + d.miktar, 0); if (top > dgK.kalan) { mesaj('danger', 'V05 Toplam dağıtım miktarı kalan miktarı aşamaz.'); return; } dgS.dagitim = l; dgS.manuel = false; dgS.birimYansitma = null; if (l.length) { dgS.miktar = top; dgS.secili = true; } bootstrap.Modal.getOrCreateInstance($('#ynDagitModal')).hide(); cizKalemler(); onizleme(); });
  $('#ynDgTemizle').addEventListener('click', () => { dgS.dagitim = []; bootstrap.Modal.getOrCreateInstance($('#ynDagitModal')).hide(); cizKalemler(); onizleme(); });

  // ---- üst aksiyonlar
  $('#ynYeni').addEventListener('click', () => { formSifirla(); sekme('yeni'); mesaj('info', 'Yeni yansıtma açıldı; Yansıtma No kayıtta otomatik üretilir.'); });
  function zorunluKontrol() { const eksik = $$('#ynForm [required]').filter(x => !String(x.value).trim()); eksik.forEach(x => x.classList.add('is-invalid')); if (eksik.length) { mesaj('danger', 'Zorunlu başlık alanlarını doldurun.'); eksik[0].focus(); return false; } if (!FAT) { mesaj('danger', 'Kaynak fatura seçilmedi.'); return false; } return true; }
  $('#ynForm').addEventListener('input', e => e.target.classList.remove('is-invalid'));
  $('#ynTaslak').addEventListener('click', async () => { if (!FAT) { mesaj('danger', 'Kaynak fatura seçilmedi.'); return; } const j = await post('/Yansitma/Kaydet', girisYap()); if (!j.success) { mesaj('danger', esc(j.message)); if (j.islem) guncelle(j); return; } F.id = j.islem.id; F.no = j.islem.no; F.durum = j.islem.durum; F.versiyon = j.islem.versiyon; guncelle(j); durumRozeti(); mesaj('success', `<i class="fas fa-floppy-disk me-1"></i>${esc(j.message)}` + (j.uyarilar && j.uyarilar.length ? ` <span class="text-warning">(${j.uyarilar.length} uyarı)</span>` : '')); });
  $('#ynGonder').addEventListener('click', async () => {
    if (!zorunluKontrol()) return; const g = girisYap(); if (!g.kalemler.length) { mesaj('danger', 'V02 En az bir kaynak kalem seçilmelidir.'); return; }
    $('#ynGonder').disabled = true;
    try { const j = await post('/Yansitma/OnayaGonder', g); if (j.islem) { F.id = j.islem.id; F.no = j.islem.no; F.durum = j.islem.durum; F.versiyon = j.islem.versiyon; guncelle(j); durumRozeti(); } if (!j.success) { mesaj('danger', `<b>Onaya gönderilemedi:</b> ${esc(j.message)}`); onizleme(); return; } mesaj('success', `<i class="fas fa-circle-check me-1"></i>${esc(j.message)}`); const no = j.no; formSifirla(); detayAc(no); }
    finally { $('#ynGonder').disabled = false; }
  });
  $('#ynIptalBtn').addEventListener('click', async () => { if (!F.id) return; const neden = prompt('İptal nedeni (zorunlu):') || ''; if (!neden.trim()) return; const j = await post('/Yansitma/Iptal', { no: F.no, neden, rol: ROL }); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) { const no = F.no; formSifirla(); detayAc(no); } });
  $('#ynBelge').addEventListener('click', () => { if (!FAT) { mesaj('warning', 'Kaynak belge seçilmedi.'); return; } window.open('/AlimOnay/Gorsel?id=' + FAT.id, '_blank'); });
  $('#ynExcel').addEventListener('click', () => { const l = secililer(); if (!l.length) { mesaj('warning', 'Dışa aktarılacak seçili kalem yok.'); return; } csvIndir('yansitma_' + (F.no || 'taslak'), ['Yansıtma No', 'Kaynak Fatura', 'Satır', 'Kalem', 'Kod', 'Fatura Miktarı', 'Önceden Yansıtılan', 'Kalan', 'Yansıtılacak', 'Birim', 'Birim Maliyet', 'Toplam Maliyet', 'Kâr %', 'Birim Kâr', 'Birim Yansıtma', 'Toplam Yansıtma', 'KDV %', 'KDV', 'KDV Dahil', 'Masraf Merkezi', 'Dağıtım', 'Rapor Zamanı'], l.map(x => [F.no || 'TASLAK', FAT.faturaNo, x.k.satirNo, x.k.ad, x.k.kod, x.k.miktar, x.k.oncedenYansitilan, x.k.kalan, x.h.miktar, x.k.birim, x.k.birimMaliyet, x.h.maliyet, r2(x.h.kar), x.h.birimKar, x.h.birim, x.h.toplam, x.h.kdvPct, x.h.kdv, x.h.brut, x.s.masrafMerkezi || $('#ynMasrafMerkezi').value, (x.s.dagitim || []).map(d => `${d.sirket} ${d.miktar} %${d.karPct}`).join(' | '), new Date().toLocaleString('tr-TR')])); });
  $('#ynPdf').addEventListener('click', () => { if (!simAcik && SON) { simAcik = true; $('#ynSimKart').style.display = ''; simCiz(SON); } setTimeout(() => window.print(), 100); });

  // ---- var olan işlemi forma yükle (Taslak / Reddedildi revize)
  async function formaYukle(t) {
    formSifirla(); F.id = t.id; F.no = t.no; F.durum = t.durum; F.versiyon = t.versiyon;
    $('#ynTarih').value = t.tarih; $('#ynYansitan').value = t.yansitan; $('#ynYansitilan').value = t.yansitilan; $('#ynTur').value = t.tur; $('#ynKaynakBelgeTuru').value = t.kaynakBelgeTuru || 'Alış Faturası'; $('#ynMasrafMerkezi').value = t.masrafMerkezi || ''; $('#ynProje').value = t.proje || ''; $('#ynDepartman').value = t.departman || ''; $('#ynAciklama').value = t.aciklama || '';
    const r = await fetch(`/Yansitma/KaynakFaturalar?yansitan=${encodeURIComponent(t.yansitan)}&ara=${encodeURIComponent(t.kaynakFaturaNo)}&haric=${t.id}&sayfa=1&boy=50`), j = await r.json();
    const f = j.faturalar.find(x => x.id === t.kaynakFaturaId); if (!f) { mesaj('danger', 'Kaynak fatura bulunamadı.'); return; }
    FAT = Object.assign({ sirket: t.yansitan }, f); $('#ynParaBirimi').value = f.paraBirimi; $('#ynKur').value = t.kur; $('#ynKur').disabled = f.paraBirimi === 'TRY' || !yetki('manuelFiyat');
    F.satirlar = {}; t.kalemler.forEach(k => F.satirlar[k.satirNo] = { secili: true, miktar: k.yansitilacakMiktar, karPct: k.karPct, birimYansitma: k.manuelFiyat ? k.birimYansitma : null, manuel: k.manuelFiyat, kdvPct: k.kdvPct, masrafMerkezi: k.masrafMerkezi || '', proje: k.proje || '', gerekce: k.gerekce || '', dagitim: (k.dagitim || []).map(d => ({ sirket: d.sirket, miktar: d.miktar, karPct: d.karPct })) });
    cizFaturaOzeti(); cizKalemler(); durumRozeti(); yetkiUygula(); onizleme(); sekme('yeni');
    mesaj('info', `${esc(t.no)} düzenleme için açıldı (${esc(t.durum)}${t.durum === 'Reddedildi' ? '; kaydedince taslağa revize olur, yeni versiyon açılır' : ''}).`);
  }

  // ============================================================
  // LİSTE, ONAY KUYRUĞU, DETAY
  // ============================================================
  const okLine = t => `${esc(t.yansitan)} → ${esc((t.hedefler || [t.yansitilan]).join(', '))}`;
  function cizListe() {
    const ara = ($('#ynListeAra').value || '').toLocaleLowerCase('tr-TR'), d = $('#ynListeDurum').value, benim = $('#ynListeBenim').value;
    const l = V.islemler.filter(t => (!d || t.durum === d) && (!benim || t.olusturan === V.ben.adSoyad) && (!ara || [t.no, t.yansitan, t.yansitilan, t.kaynakFaturaNo, t.kaynakTedarikci, t.yansitmaFaturaNo, t.aciklama].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
    $('#ynListe tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${t.no}"><td><b>${t.no}</b>${t.versiyon > 1 ? ` <small class="text-muted">v${t.versiyon}</small>` : ''}</td><td>${t.tarihTr}</td><td>${okLine(t)}</td><td>${esc(t.tur)}</td><td>${esc(t.kaynakFaturaNo)}<br><small class="text-muted">${esc(t.kaynakTedarikci)}</small></td><td class="s">${t.ozet.kalemSayisi}</td><td class="s">${para(t.ozet.toplamYansitmaTl, 0)}</td><td class="s">${pct(t.ozet.agirlikliKarPct)}</td><td class="s">${para(t.ozet.genelToplamTl, 0)}</td><td>${durumRozet(t.durum)}</td><td>${bos(t.yansitmaFaturaNo)}</td><td>${t.yansitmaFaturaNo ? ksRozet(t.kontrolDurumu) : '<span class="text-muted">—</span>'}</td><td>${esc(t.olusturan)}</td></tr>`).join('') || '<tr><td colspan="13" class="na-bos">Kayıt yok.</td></tr>';
    $('#ynListeSayi').textContent = `${l.length} / ${V.islemler.length} işlem`;
    $$('#ynListe tr.tik').forEach(r => r.addEventListener('click', () => detayAc(r.dataset.no)));
  }
  ['#ynListeAra', '#ynListeDurum', '#ynListeBenim'].forEach(s => { $(s).addEventListener('input', cizListe); $(s).addEventListener('change', cizListe); });
  function cizOnay() {
    const l = V.islemler.filter(t => t.durum === 'Onay Bekliyor');
    $('#ynOnayListe tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${t.no}"><td><b>${t.no}</b></td><td>${okLine(t)}</td><td>${esc(t.tur)}</td><td>${esc(t.kaynakFaturaNo)}</td><td class="s">${para(t.ozet.toplamYansitmaTl, 0)}</td><td class="s">${pct(t.ozet.agirlikliKarPct)}</td><td class="s">${para(t.ozet.genelToplamTl, 0)}</td><td>${esc(t.bekleyenAdim)}</td><td>${esc(t.bekleyenKisi)}</td><td><small>${esc(t.onayNedeni)}</small></td><td>${yetki('onay') ? `<button type="button" class="btn btn-success btn-sm py-0" data-hizli="Onayla" data-no="${t.no}"><i class="fas fa-check"></i></button> <button type="button" class="btn btn-outline-danger btn-sm py-0" data-hizli="Reddet" data-no="${t.no}"><i class="fas fa-xmark"></i></button>` : '<span class="yn-yetki">rol onay veremez</span>'}</td></tr>`).join('') || '<tr><td colspan="11" class="na-bos">Onay bekleyen yansıtma yok.</td></tr>';
    $$('#ynOnayListe tr.tik').forEach(r => r.addEventListener('click', e => { if (!e.target.closest('button')) detayAc(r.dataset.no); }));
    $$('#ynOnayListe [data-hizli]').forEach(b => b.addEventListener('click', () => onayVer(b.dataset.no, b.dataset.hizli)));
  }
  async function onayVer(no, eylem) { let not = ''; if (eylem === 'Reddet') { not = prompt('Ret nedeni (zorunlu):') || ''; if (!not.trim()) return; } else not = prompt('Onay notu (isteğe bağlı):') || ''; const j = await post('/Yansitma/Onay', { no, eylemAdi: eylem, not, rol: ROL }); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (aktifNo === no) detayAc(no, true); }

  function adimlarHtml(t) {
    const A = ['Taslak', 'Onay Bekliyor', 'Onaylandı', 'Fatura Oluştu', 'Muhasebeleşti', 'Uyumlu']; const i = A.indexOf(t.durum);
    let h = A.map((a, x) => `<span class="${i < 0 ? '' : x < i ? 'gecti' : x === i ? 'simdi' : ''}">${x + 1}. ${a}</span>`).join('');
    if (t.durum === 'Reddedildi' || t.durum === 'İptal') h += `<span class="hata">${t.durum === 'Reddedildi' ? 'X. Reddedildi' : 'X. İptal'}</span>`;
    return `<div class="yn-adimlar">${h}</div>`;
  }
  function kontrolTablo(t) {
    const l = (t.kontroller || []).filter(k => !k.alan.startsWith('Şirketler Arası'));
    if (!l.length) return '<div class="na-bos">Kontrol sonucu yok.</div>';
    return `<div class="table-responsive"><table class="na-tablo yn-kars" data-dinamik="kapali"><thead><tr><th>Kontrol Alanı</th><th>Şirket</th><th>Beklenen</th><th>Gerçekleşen</th><th>Sonuç</th></tr></thead><tbody>${l.map(k => `<tr><td>${esc(k.alan)}</td><td>${esc(k.sirket)}</td><td><code>${esc(k.beklenen)}</code></td><td class="${k.sonuc === 'UYUMLU' ? '' : k.kritik ? 'fark' : 'uyar'}"><code>${esc(k.gerceklesen)}</code></td><td>${ksRozet(k.sonuc)}</td></tr>`).join('')}</tbody></table></div>`;
  }
  function mutabakatHtml(t) {
    const l = (t.kontroller || []).filter(k => k.alan.startsWith('Şirketler Arası')); if (!l.length) return '<div class="na-bos">Her iki şirket fişi okunmadan mutabakat kontrolü yapılamaz.</div>';
    const pb = (t.kontroller || []).filter(k => k.alan === 'Para Birimi'), rf = (t.kontroller || []).filter(k => k.alan === 'Belge Referansı');
    const satir = (ad, b, g, s) => `<span>${esc(ad)}</span><span>${esc(b)}</span><span>${esc(g)}</span><span>${ksRozet(s)}</span>`;
    return `<div class="yn-mutabakat"><span class="h">Kontrol</span><span class="h">Yansıtan Şirket</span><span class="h">Yansıtılan Şirket</span><span class="h">Sonuç</span>${l.map(k => satir('Cari Tutar · ' + k.sirket, k.beklenen, k.gerceklesen, k.sonuc)).join('')}${pb.length ? satir('Para Birimi', pb[0].gerceklesen, (pb[1] || pb[0]).gerceklesen, pb.every(x => x.sonuc === 'UYUMLU') ? 'UYUMLU' : 'KONTROL GEREKLİ') : ''}${rf.length ? satir('Belge Referansı', rf[0].gerceklesen, (rf[1] || rf[0]).gerceklesen, rf.every(x => x.sonuc === 'UYUMLU') ? 'UYUMLU' : 'KONTROL GEREKLİ') : ''}</div>`;
  }
  function detayHtml(t, drawer) {
    const btn = (act, ad, cls, ikon, extra) => `<button type="button" class="btn ${cls} btn-sm py-0" data-act="${act}" data-no="${t.no}" style="font-size:.68rem" ${extra || ''}><i class="fas ${ikon} me-1"></i>${ad}</button>`;
    const eylemler = [];
    if ((t.durum === 'Taslak' || t.durum === 'Reddedildi') && yetki('taslak')) eylemler.push(btn('duzenle', t.durum === 'Reddedildi' ? 'Taslağa Revize Et' : 'Düzenle', 'btn-outline-primary', 'fa-pen'));
    if (t.durum === 'Taslak' && yetki('onayaGonder')) eylemler.push(btn('gonder', 'Onaya Gönder', 'btn-primary', 'fa-paper-plane'));
    if (t.durum === 'Onay Bekliyor' && yetki('onay')) { eylemler.push(btn('onayla', 'Onayla', 'btn-success', 'fa-check')); eylemler.push(btn('reddet', 'Reddet', 'btn-outline-danger', 'fa-xmark')); }
    if (t.durum === 'Onay Bekliyor') eylemler.push(btn('gericek', 'Geri Çek', 'btn-light', 'fa-rotate-left'));
    if (t.durum === 'Onaylandı' && yetki('faturaOlustur')) eylemler.push(btn('fatura', 'Yansıtma Faturası Oluştur (ERP)', 'btn-primary', 'fa-file-invoice-dollar'));
    if (t.yansitmaFaturaNo && yetki('fisAl')) eylemler.push(`<span class="d-inline-flex align-items-center gap-1"><select class="form-select form-select-sm" data-senaryo="${t.no}" style="font-size:.66rem;min-height:24px;padding:1px 22px 1px 6px;width:auto">${SENARYOLAR.map(([k, a]) => `<option value="${k}">${a}</option>`).join('')}</select>${btn('fisal', t.gercek.length ? 'Fişi Yeniden Oku' : 'Gerçek Fişi ERP\'den Oku', 'btn-outline-secondary', 'fa-download')}</span>`);
    if (t.gercek.length && yetki('kontrol')) eylemler.push(btn('kontrol', 'Kontrolü Çalıştır', 'btn-light', 'fa-rotate'));
    if (t.yansitmaFaturaNo) eylemler.push(btn('kontrolgit', 'Muhasebe Kontrolünde Gör', 'btn-light', 'fa-file-invoice'));
    if (t.aktif && !t.yansitmaFaturaNo && yetki('iptal')) eylemler.push(btn('iptal', 'İptal', 'btn-outline-danger', 'fa-ban'));
    if (t.yansitmaFaturaNo && t.aktif && yetki('iptal')) eylemler.push(btn('ters', 'Ters Kayıt Başlat', 'btn-outline-danger', 'fa-rotate-left', 'title="Oluşmuş ERP belgesi doğrudan silinmez; iade faturası / ters kayıt süreci"'));
    eylemler.push(btn('belge', 'Kaynak Belge', 'btn-light', 'fa-eye')); eylemler.push(btn('pdf', 'PDF', 'btn-light', 'fa-file-pdf')); eylemler.push(btn('excel', 'Excel', 'btn-light', 'fa-file-excel'));
    const ozet = t.ozet, pb = t.paraBirimi, dov = pb !== 'TRY';
    const ust = [['Yansıtma No', `${t.no}${t.versiyon > 1 ? ' · v' + t.versiyon : ''}`], ['Yansıtma Tarihi', t.tarihTr], ['Yansıtan Şirket', t.yansitan], ['Yansıtılan Şirket', (t.hedefler || [t.yansitilan]).join(', ')], ['Yansıtma Türü', t.tur], ['Kaynak Belge Türü', t.kaynakBelgeTuru], ['Kaynak Fatura', `${t.kaynakFaturaNo} · ${t.kaynakFaturaTarihi}`], ['Tedarikçi', t.kaynakTedarikci], ['Para Birimi / Kur', dov ? `${pb} · ${para(t.kur, 4)}` : 'TRY'], ['Masraf Merkezi / Proje', (t.masrafMerkezi || '—') + (t.proje ? ' · ' + t.proje : '')], ['Departman', t.departman || '—'], ['Oluşturan', `${t.olusturan} · ${t.olusturmaZamani}`]];
    return `${adimlarHtml(t)}
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-id-card me-1"></i>${esc(t.no)} · ${durumRozet(t.durum)} ${t.yansitmaFaturaNo ? ksRozet(t.kontrolDurumu) : ''}</span><span class="yn-aksiyon">${eylemler.join('')}</span></div>
        <div class="na-kart-g"><div class="na-ustbilgi">${ust.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${esc(v)}</div></div>`).join('')}<div style="grid-column:1/-1"><div class="l">Açıklama</div><div class="v" style="font-weight:500">${esc(t.aciklama)}</div></div></div>
        ${t.redNedeni ? `<div class="na-uyari kirmizi mt-2"><b>Ret nedeni:</b> ${esc(t.redNedeni)}</div>` : ''}${t.iptalNedeni ? `<div class="na-uyari kirmizi mt-2"><b>İptal nedeni:</b> ${esc(t.iptalNedeni)}</div>` : ''}
        <div class="na-durum3"><div><div class="l">Operasyon Durumu</div><div class="v">${esc(t.durum)}</div><div class="s">${t.bekleyenAdim ? 'bekleyen: ' + esc(t.bekleyenAdim) + ' · ' + esc(t.bekleyenKisi) : t.guncellemeZamani}</div></div><div><div class="l">Muhasebe Durumu</div><div class="v">${esc(muhasebeDurumu(t))}</div><div class="s">${t.yansitmaFaturaNo ? 'ERP ' + esc(t.yansitmaFaturaNo) + (t.fisler.length ? ' · fiş ' + esc(t.fisler.join(', ')) : '') : 'ERP belgesi yok'}</div></div><div><div class="l">Kontrol Durumu</div><div class="v">${t.kontrolDurumu ? ksRozet(t.kontrolDurumu) : '<span class="text-muted">kontrol yok</span>'}</div><div class="s">${t.kontrolTarihi ? 'son kontrol ' + esc(t.kontrolTarihi) : 'gerçek fiş bekleniyor'}</div></div></div></div></div>
      <div class="row g-2"><div class="col-lg-9">
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-list-check me-1"></i>Kalemler (${t.kalemler.length})</span><span class="not">kaynak satır · miktar · birim maliyet · kâr · yansıtma · KDV · kalan</span></div><div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Satır</th><th>Kalem</th><th class="s">Fatura Mkt.</th><th class="s">Önceden</th><th class="s">Yansıtılan</th><th class="s">İşlem Sonrası Kalan</th><th class="s">Birim Maliyet</th><th class="s">Kâr %</th><th class="s">Birim Yansıtma</th><th class="s">Toplam Yansıtma</th><th class="s">KDV %</th><th class="s">KDV Dahil</th><th>MM / Dağıtım</th></tr></thead><tbody>
          ${t.kalemler.map(k => `<tr><td>${k.satirNo}</td><td>${esc(k.kalemAd)}<br><small class="text-muted">${esc(k.kalemKod)} · ${esc(k.kategori)} · eşleme #${k.eslemeId || '—'}</small></td><td class="s">${mik(k.faturaMiktari)} ${esc(k.birim)}</td><td class="s">${mik(k.oncedenYansitilan)}</td><td class="s"><b>${mik(k.yansitilacakMiktar)}</b></td><td class="s">${mik(k.islemSonrasiKalan)}</td><td class="s">${para(k.birimMaliyet, 4)}</td><td class="s">${pct(k.karPct)}${k.manuelFiyat ? ' <i class="fas fa-pen text-warning" title="manuel fiyat: ' + esc(k.gerekce) + '"></i>' : ''}</td><td class="s">${para(k.birimYansitma, 4)}</td><td class="s"><b>${para(k.toplamYansitma)}</b></td><td class="s">${k.kdvPct}</td><td class="s">${para(k.kdvDahil)}</td><td>${esc(k.masrafMerkezi || t.masrafMerkezi || '—')}${k.dagitim.length ? '<br>' + k.dagitim.map(d => `<small class="text-info">${esc(d.sirket)}: ${mik(d.miktar)} · %${d.karPct}</small>`).join('<br>') : ''}</td></tr>`).join('')}
          <tr style="font-weight:800;background:#f8fafc"><td colspan="9">Toplam (${pb})</td><td class="s">${para(ozet.toplamYansitma)}</td><td></td><td class="s">${para(ozet.genelToplam)}</td><td></td></tr></tbody></table></div></div></div>
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-calculator me-1"></i>Muhasebe Kaydı Simülasyonu (snapshot)</span><span class="not">${t.simulasyonTarihi ? 'onaya gönderme anı · ' + esc(t.simulasyonTarihi) : 'onaya gönderilince saklanır'}</span></div><div class="na-kart-g">${t.simulasyon.length ? `<div class="yn-sim">${simHtml(t.simulasyon, t.yansitan)}</div>` : '<div class="na-bos">Snapshot yok (taslak).</div>'}</div></div>
        ${t.gercek.length || t.yansitmaFaturaNo ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-book me-1"></i>Gerçekleşen Muhasebe Kaydı (ERP / SAP)</span><span class="not">${t.gercekTarihi ? 'okundu ' + esc(t.gercekTarihi) : 'fiş bekleniyor'}</span></div><div class="na-kart-g">${t.gercek.length ? `<div class="yn-sim">${[...new Set(t.gercek.map(x => x.sirket))].map(s => fisTablo(t.gercek, s, { taraf: s === t.yansitan ? 'yansıtan · gerçek fiş' : 'yansıtılan · gerçek fiş', fis: true, kural: false })).join('')}</div>` : '<div class="na-bos">ERP\'de bu yansıtma için muhasebe fişi bulunamadı (MUHASEBE KAYDI YOK).</div>'}</div></div>` : ''}
        ${t.kontroller.length ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-code-compare me-1"></i>Muhasebe Kontrol Sonucu ${ksRozet(t.kontrolDurumu)}</span><span class="not">${esc(t.kontrolTarihi)}</span></div><div class="na-kart-g">${kontrolTablo(t)}<div class="mt-2">${mutabakatHtml(t)}</div></div></div>` : ''}
      </div><div class="col-lg-3">
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-sigma me-1"></i>Özet</span></div><div class="na-kart-g yn-ozet"><span class="l">Toplam Maliyet</span><span class="v">${para(ozet.toplamMaliyet)} ${pb}</span><span class="l">Toplam Kâr</span><span class="v kar ${ozet.toplamKar < 0 ? 'eksi' : ''}">${para(ozet.toplamKar)}</span><span class="l">Ağırlıklı Kâr %</span><span class="v">${pct(ozet.agirlikliKarPct)}</span><span class="ayrac"></span><span class="l">Net Yansıtma</span><span class="v">${para(ozet.toplamYansitma)}</span><span class="l">KDV</span><span class="v">${para(ozet.kdv)}</span><span class="l buyuk">Genel Toplam</span><span class="v buyuk">${para(ozet.genelToplam)}</span>${dov ? `<span class="l" style="grid-column:1/-1;text-align:right;font-size:.62rem">≈ ${para(ozet.genelToplamTl, 0)} TL</span>` : ''}</div></div>
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-route me-1"></i>Onay Süreci</span><span class="not">${esc(t.onayNedeni)}</span></div><div class="na-kart-g">${t.onaylar.length ? t.onaylar.map(o => `<div class="na-adim ${o.durum === 'Onaylandı' ? 'ok' : o.durum === 'Reddedildi' ? 'hata' : 'bekle'}"><span class="no">${o.sira}</span><div><div class="ad">${esc(o.adim)}</div><div class="k">${esc(o.kisi)}${o.not ? ' · ' + esc(o.not) : ''}</div></div><div class="sag">${rozet(o.durum, o.durum === 'Onaylandı' ? 'ok' : o.durum === 'Reddedildi' ? 'hata' : 'bekle')}<div class="k">${esc(o.tarih)}</div></div></div>`).join('') : '<div class="na-bos">Onay akışı onaya gönderilince oluşur.</div>'}</div></div>
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-plug me-1"></i>ERP Entegrasyonu</span><span class="not">${esc(t.entegrasyon.durum)}</span></div><div class="na-kart-g" style="font-size:.7rem">${t.yansitmaFaturaNo ? `<div><b>Yansıtma Faturası:</b> ${esc(t.yansitmaFaturaNo)} · DocEntry ${esc(t.erpDocEntry)}</div><div><b>Idempotency:</b> <code>${esc(t.entegrasyon.idempotencyKey)}</code></div><div class="text-muted">${esc(t.entegrasyon.cevap)}</div>` : '<div class="na-bos">ERP çağrısı henüz yapılmadı (BEKLIYOR).</div>'}${t.fisler.length ? `<div class="mt-1"><b>Muhasebe fişleri:</b> ${t.fisler.map(esc).join(', ')}</div>` : ''}</div></div>
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-paperclip me-1"></i>Belgeler</span></div><div class="na-kart-g"><div class="na-ek"><i class="fas fa-file-invoice"></i>Kaynak fatura ${esc(t.kaynakFaturaNo)}<span class="m"><a href="/AlimOnay/Gorsel?id=${t.kaynakFaturaId}" target="_blank">görüntüle</a></span></div>${t.tur === 'Hizmet' ? '<div class="na-ek"><i class="fas fa-file-contract"></i>Transfer fiyatlandırması dosyası<span class="m">ek</span></div>' : ''}${t.yansitmaFaturaNo ? `<div class="na-ek"><i class="fas fa-file-invoice-dollar"></i>Yansıtma faturası ${esc(t.yansitmaFaturaNo)}<span class="m">ERP</span></div>` : ''}<div class="na-mini"><input type="file" class="form-control form-control-sm" style="font-size:.66rem" multiple> destekleyici belge yükle</div></div></div>
        <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Audit Log</span><span class="not">eski / yeni değer</span></div><div class="na-kart-g p-0"><div class="table-responsive" style="max-height:260px"><table class="na-tablo yn-audit" data-dinamik="kapali"><thead><tr><th>Zaman</th><th>Alan</th><th>Eski → Yeni</th><th>Kim / Kaynak</th></tr></thead><tbody>${t.audit.map(a => `<tr><td><small>${esc(a.zaman)}</small></td><td><code>${esc(a.alan)}</code></td><td>${a.eski ? `<code>${esc(a.eski)}</code> → ` : ''}<code>${esc(a.yeni)}</code>${a.gerekce ? `<br><small class="text-muted">${esc(a.gerekce)}</small>` : ''}</td><td><small>${esc(a.kullanici)}<br class="d-none">· ${esc(a.kaynak)}</small></td></tr>`).join('')}</tbody></table></div></div></div>
      </div></div>`;
  }
  function detayAc(no, sessiz) {
    const t = bul(no); if (!t) { mesaj('warning', no + ' bulunamadı.'); return; }
    aktifNo = no; $('#ynDetayNo').textContent = no; $('#ynDetay').innerHTML = detayHtml(t); if (!sessiz) { ust('islem'); sekme('detay'); }
  }
  async function eylem(act, no, kaynak) {
    const t = bul(no); if (!t) return; let j = null;
    switch (act) {
      case 'duzenle': drawerKapat(); await formaYukle(t); return;
      case 'gonder': j = await post('/Yansitma/OnayaGonder', { id: t.id, rol: ROL }); break;
      case 'onayla': case 'reddet': await onayVer(no, act === 'onayla' ? 'Onayla' : 'Reddet'); return;
      case 'gericek': j = await post('/Yansitma/GeriCek', { no, rol: ROL }); break;
      case 'fatura': j = await post('/Yansitma/FaturaOlustur', { no, rol: ROL }); break;
      case 'fisal': { const s = $(`[data-senaryo="${no}"]`, kaynak) || $(`[data-senaryo="${no}"]`); j = await post('/Yansitma/FisAl', { no, rol: ROL, senaryo: s ? s.value : 'uyumlu' }); break; }
      case 'kontrol': j = await post('/Yansitma/Kontrol', { no, rol: ROL }); break;
      case 'kontrolgit': drawerKapat(); kontrolSecili = no; ust('kontrol'); return;
      case 'iptal': { const neden = prompt('İptal nedeni (zorunlu):') || ''; if (!neden.trim()) return; j = await post('/Yansitma/Iptal', { no, neden, rol: ROL }); break; }
      case 'ters': mesaj('warning', `${no}: oluşmuş ERP belgesi doğrudan silinmez. Ters kayıt (iade faturası) süreci Muhasebe tarafından ERP üzerinde başlatılır ve orijinal yansıtma ile zincir halinde ilişkilendirilir.`); return;
      case 'belge': window.open('/AlimOnay/Gorsel?id=' + t.kaynakFaturaId, '_blank'); return;
      case 'pdf': setTimeout(() => window.print(), 50); return;
      case 'excel': csvIndir('yansitma_' + no, ['Yansıtma No', 'Tarih', 'Yansıtan', 'Yansıtılan', 'Kaynak Fatura', 'Satır', 'Kalem', 'Yansıtılan Mkt.', 'Birim', 'Birim Maliyet', 'Kâr %', 'Birim Yansıtma', 'Toplam Yansıtma', 'KDV %', 'KDV Dahil', 'Durum', 'ERP Belge', 'Kontrol', 'Rapor Zamanı'], t.kalemler.map(k => [t.no, t.tarihTr, t.yansitan, t.yansitilan, t.kaynakFaturaNo, k.satirNo, k.kalemAd, k.yansitilacakMiktar, k.birim, k.birimMaliyet, k.karPct, k.birimYansitma, k.toplamYansitma, k.kdvPct, k.kdvDahil, t.durum, t.yansitmaFaturaNo || '', t.kontrolDurumu || '', new Date().toLocaleString('tr-TR')])); return;
    }
    if (j) { guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (aktifNo === no) detayAc(no, true); if ($('#ynDrawer').classList.contains('acik')) drawerAc(no); if (kontrolSecili === no && $('#giKontrol').classList.contains('aktif')) cizKontrol(); }
  }
  [$('#ynDetay'), $('#ynDrawerIc'), $('#ynKarsilastirma')].forEach(el => el.addEventListener('click', e => { const b = e.target.closest('[data-act]'); if (b) eylem(b.dataset.act, b.dataset.no, el); }));

  // ============================================================
  // SEKME 2 — YANSITMA RAPORU (§6)
  // ============================================================
  const secilenler = id => Array.from($(id).selectedOptions).map(o => o.value);
  function raporListesi() {
    const d1 = $('#ynRf1').value, d2 = $('#ynRf2').value, ya = secilenler('#ynRfYansitan'), yi = secilenler('#ynRfYansitilan'), tu = secilenler('#ynRfTur'), du = secilenler('#ynRfDurum'), ko = secilenler('#ynRfKontrol');
    const tx = id => ($(id).value || '').toLocaleLowerCase('tr-TR'), kf = tx('#ynRfKaynak'), er = tx('#ynRfErp'), te = tx('#ynRfTedarikci'), ka = tx('#ynRfKalem'), mm = tx('#ynRfMm'), ki = tx('#ynRfKisi');
    return V.islemler.filter(t => (!d1 || t.tarih >= d1) && (!d2 || t.tarih <= d2) && (!ya.length || ya.includes(t.yansitan)) && (!yi.length || (t.hedefler || [t.yansitilan]).some(h => yi.includes(h))) && (!tu.length || tu.includes(t.tur)) && (!du.length || du.includes(t.durum))
      && (!ko.length || ko.includes(t.kontrolDurumu || '(kontrol yok)')) && (!kf || (t.kaynakFaturaNo || '').toLocaleLowerCase('tr-TR').includes(kf)) && (!er || (t.yansitmaFaturaNo || '').toLocaleLowerCase('tr-TR').includes(er)) && (!te || (t.kaynakTedarikci || '').toLocaleLowerCase('tr-TR').includes(te))
      && (!ka || t.kalemler.some(k => (k.kalemKod + ' ' + k.kalemAd).toLocaleLowerCase('tr-TR').includes(ka))) && (!mm || (t.masrafMerkezi + ' ' + t.proje + ' ' + t.kalemler.map(k => k.masrafMerkezi + ' ' + k.proje).join(' ')).toLocaleLowerCase('tr-TR').includes(mm)) && (!ki || (t.olusturan + ' ' + t.onaylar.map(o => o.kisi).join(' ')).toLocaleLowerCase('tr-TR').includes(ki)));
  }
  function cizRapor() {
    if (!V) return;
    const l = raporListesi(), S = f => l.reduce((s, t) => s + f(t), 0), akt = l.filter(t => t.aktif);
    const ty = akt.reduce((s, t) => s + t.ozet.toplamYansitmaTl, 0), tm = akt.reduce((s, t) => s + t.ozet.toplamMaliyetTl, 0), tk = ty - tm;
    const kpi = [['#0d9488', 'Toplam Yansıtma', para(ty, 0) + ' TL', 'net, filtre kapsamı (red / iptal hariç)'], ['#64748b', 'Toplam Maliyet', para(tm, 0) + ' TL', 'kaynak maliyet'], ['#16a34a', 'Toplam Kâr', para(tk, 0) + ' TL', 'yansıtma − maliyet'], ['#0ea5e9', 'Ağırlıklı Kâr %', pct(tm > 0 ? tk / tm * 100 : 0), 'Toplam Kâr / Toplam Maliyet'],
      ['#f59e0b', 'Onay Bekleyen', l.filter(t => t.durum === 'Onay Bekliyor').length + ' İşlem', para(l.filter(t => t.durum === 'Onay Bekliyor').reduce((s, t) => s + t.ozet.genelToplamTl, 0), 0) + ' TL'], ['#6366f1', 'Fatura Oluşmamış', l.filter(t => t.durum === 'Onaylandı').length + ' İşlem', 'Onaylı, ERP Belgesi Yok'], ['#8b5cf6', 'Muhasebeleşmemiş', l.filter(t => t.durum === 'Fatura Oluştu').length + ' İşlem', 'Fatura Var, Yevmiye Yok'], ['#dc2626', 'Kontrol Gerekli', l.filter(t => t.kontrolDurumu && t.kontrolDurumu !== 'UYUMLU').length + ' İşlem', 'Uyumlu Olmayan Kontrol']];
    $('#ynRaporKpi').innerHTML = kpi.map(([r, b, v, a]) => `<div style="--k:${r}"><div class="l">${b}</div><div class="v">${v}</div><div class="s">${bh(a)}</div></div>`).join('');
    $('#ynRaporTablo tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${t.no}"><td><b>${t.no}</b></td><td>${t.tarihTr}</td><td>${esc(t.yansitan)}</td><td>${esc((t.hedefler || [t.yansitilan]).join(', '))}</td><td>${esc(t.kaynakFaturaNo)}</td><td>${bos(t.yansitmaFaturaNo)}</td><td class="s">${para(t.ozet.toplamMaliyetTl, 0)}</td><td class="s">${para(t.ozet.toplamYansitmaTl - t.ozet.toplamMaliyetTl, 0)}</td><td class="s">${pct(t.ozet.agirlikliKarPct)}</td><td class="s">${para(t.ozet.toplamYansitmaTl, 0)}</td><td class="s">${para(t.ozet.kdvTl, 0)}</td><td class="s">${para(t.ozet.genelToplamTl, 0)}</td><td>${durumRozet(t.durum)}</td><td>${esc(muhasebeDurumu(t))}</td><td>${t.yansitmaFaturaNo ? ksRozet(t.kontrolDurumu) : '<span class="text-muted">—</span>'}</td></tr>`).join('') || '<tr><td colspan="15" class="na-bos">Filtreye uyan işlem yok.</td></tr>';
    $('#ynRaporTablo tfoot').innerHTML = l.length ? `<tr style="font-weight:800;background:#f8fafc"><td colspan="6">Toplam · ${l.length} işlem (aktif ${akt.length})</td><td class="s">${para(tm, 0)}</td><td class="s">${para(tk, 0)}</td><td class="s">${pct(tm > 0 ? tk / tm * 100 : 0)}</td><td class="s">${para(ty, 0)}</td><td class="s">${para(akt.reduce((s, t) => s + t.ozet.kdvTl, 0), 0)}</td><td class="s">${para(akt.reduce((s, t) => s + t.ozet.genelToplamTl, 0), 0)}</td><td colspan="3"></td></tr>` : '';
    $('#ynRaporSayi').textContent = `${l.length} / ${V.islemler.length} işlem · tutarlar TL (dövizli işlemler işlem kuruyla)`;
    $$('#ynRaporTablo tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(r.dataset.no)));
  }
  $$('#giRapor .yn-filtre input, #giRapor .yn-filtre select').forEach(e => { e.addEventListener('input', cizRapor); e.addEventListener('change', cizRapor); });
  $('#ynRaporBuAy').addEventListener('click', () => { $('#ynRf1').value = bugunIso.slice(0, 8) + '01'; $('#ynRf2').value = bugunIso; cizRapor(); });
  $('#ynRaporTemizle').addEventListener('click', () => { $$('#giRapor .yn-filtre input').forEach(i => i.value = ''); $$('#giRapor .yn-filtre select').forEach(s => Array.from(s.options).forEach(o => o.selected = false)); cizRapor(); });
  $('#ynRaporExcel').addEventListener('click', () => { const l = raporListesi(); csvIndir('yansitma_raporu', ['Yansıtma No', 'Tarih', 'Yansıtan', 'Yansıtılan', 'Tür', 'Kaynak Fatura', 'Tedarikçi', 'Yansıtma Faturası', 'Toplam Maliyet (TL)', 'Toplam Kâr (TL)', 'Kâr %', 'Toplam Yansıtma (TL)', 'KDV (TL)', 'Genel Toplam (TL)', 'Durum', 'Muhasebe Durumu', 'Kontrol Durumu', 'Oluşturan', 'Rapor Zamanı'], l.map(t => [t.no, t.tarihTr, t.yansitan, (t.hedefler || [t.yansitilan]).join(', '), t.tur, t.kaynakFaturaNo, t.kaynakTedarikci, t.yansitmaFaturaNo || '', t.ozet.toplamMaliyetTl, r2(t.ozet.toplamYansitmaTl - t.ozet.toplamMaliyetTl), t.ozet.agirlikliKarPct, t.ozet.toplamYansitmaTl, t.ozet.kdvTl, t.ozet.genelToplamTl, t.durum, muhasebeDurumu(t), t.kontrolDurumu || '', t.olusturan, new Date().toLocaleString('tr-TR')])); });
  // detay drawer (§6.3) — sayfa değiştirmeden
  function drawerAc(no) { const t = bul(no); if (!t) return; $('#ynDrawerBaslik').innerHTML = `<i class="fas fa-file-lines me-1"></i>${esc(no)} · ${okLine(t)}`; $('#ynDrawerIc').innerHTML = detayHtml(t, true); $('#ynDrawerAc').dataset.no = no; $('#ynDrawer').classList.add('acik'); $('#ynPerde').classList.add('acik'); }
  function drawerKapat() { $('#ynDrawer').classList.remove('acik'); $('#ynPerde').classList.remove('acik'); }
  $('#ynDrawerKapat').addEventListener('click', drawerKapat); $('#ynPerde').addEventListener('click', drawerKapat);
  $('#ynDrawerAc').addEventListener('click', () => { const no = $('#ynDrawerAc').dataset.no; drawerKapat(); detayAc(no); });

  // ============================================================
  // SEKME 3 — YANSITMA MUHASEBE KONTROLÜ (§7)
  // ============================================================
  function kontrolListesi() {
    const ara = ($('#ynKontrolAra').value || '').toLocaleLowerCase('tr-TR'), d = $('#ynKontrolDurumF').value, oncesi = $('#ynKontrolOncesi').checked;
    return V.islemler.filter(t => t.aktif && (oncesi || t.yansitmaFaturaNo) && (!d || (d === '_yok' ? !t.kontrolDurumu : t.kontrolDurumu === d)) && (!ara || [t.no, t.yansitan, t.yansitilan, t.yansitmaFaturaNo, ...t.fisler].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  function cizKontrol() {
    if (!V) return;
    const F2 =V.islemler.filter(t => t.aktif && t.yansitmaFaturaNo), say = f => F2.filter(f), tl = l => para(l.reduce((s, t) => s + t.ozet.genelToplamTl, 0), 0) + ' TL';
    const kpi = [['#16a34a', 'Uyumlu', say(t => t.kontrolDurumu === 'UYUMLU'), 'UYUMLU'], ['#dc2626', 'Kritik Fark', say(t => KRITIK.includes(t.kontrolDurumu) && t.kontrolDurumu !== 'MUHASEBE KAYDI YOK'), '_kritik'], ['#b45309', 'Kontrol Gerekli', say(t => t.kontrolDurumu === 'KONTROL GEREKLİ'), 'KONTROL GEREKLİ'], ['#f59e0b', 'MM / Proje Farkı', say(t => t.kontrolDurumu === 'MASRAF MERKEZİ HATASI' || t.kontrolDurumu === 'PROJE HATASI'), '_mm'], ['#64748b', 'Muhasebe Kaydı Yok', say(t => t.kontrolDurumu === 'MUHASEBE KAYDI YOK'), 'MUHASEBE KAYDI YOK'], ['#0d9488', 'Kontrol Kapsamı', F2, '']];
    $('#ynKontrolKpi').innerHTML = kpi.map(([r, b, l, e]) => `<div style="--k:${r}" data-e="${e}"><div class="l">${b}</div><div class="v">${l.length} İşlem</div><div class="s">${tl(l)}</div></div>`).join('');
    $$('#ynKontrolKpi > div').forEach(d => d.addEventListener('click', () => { const e = d.dataset.e; $('#ynKontrolDurumF').value = e.startsWith('_') ? '' : e; $('#ynKontrolAra').value = ''; cizKontrolTablo(e === '_kritik' ? t => KRITIK.includes(t.kontrolDurumu) && t.kontrolDurumu !== 'MUHASEBE KAYDI YOK' : e === '_mm' ? t => /MASRAF|PROJE/.test(t.kontrolDurumu || '') : null); }));
    cizKontrolTablo(); if (kontrolSecili) cizKarsilastirma(kontrolSecili);
  }
  function cizKontrolTablo(ek) {
    const l = kontrolListesi().filter(t => !ek || ek(t));
    $('#ynKontrolTablo tbody').innerHTML = l.map(t => `<tr class="tik ${kontrolSecili === t.no ? 'table-active' : ''}" data-no="${t.no}"><td><b>${t.no}</b></td><td>${okLine(t)}</td><td class="s">${para(t.ozet.toplamYansitmaTl, 0)}</td><td class="s">${para(t.ozet.kdvTl, 0)}</td><td class="s">${para(t.ozet.genelToplamTl, 0)}</td><td>${bos(t.yansitmaFaturaNo)}</td><td>${t.gercek.length ? rozet('VAR', 'ok') + ' <small class="text-muted">' + esc(t.fisler.join(', ')) + '</small>' : t.yansitmaFaturaNo ? rozet('YOK', 'hata') : '<span class="text-muted">—</span>'}</td><td>${t.yansitmaFaturaNo ? ksRozet(t.kontrolDurumu) : durumRozet(t.durum)}</td><td><small>${esc(t.kontrolTarihi || '—')}</small></td></tr>`).join('') || '<tr><td colspan="9" class="na-bos">Kayıt yok.</td></tr>';
    $('#ynKontrolSayi').textContent = `${l.length} işlem`;
    $$('#ynKontrolTablo tr.tik').forEach(r => r.addEventListener('click', () => { kontrolSecili = r.dataset.no; $$('#ynKontrolTablo tr').forEach(x => x.classList.toggle('table-active', x.dataset.no === kontrolSecili)); cizKarsilastirma(kontrolSecili); }));
  }
  function cizKarsilastirma(no) {
    const t = bul(no); if (!t) return; $('#ynKarsNo').textContent = `${t.no} · ${t.yansitan} → ${(t.hedefler || [t.yansitilan]).join(', ')}`;
    const btn = (act, ad, cls, ikon) => `<button type="button" class="btn ${cls} btn-sm py-0" data-act="${act}" data-no="${t.no}" style="font-size:.66rem"><i class="fas ${ikon} me-1"></i>${ad}</button>`;
    const eylemler = [];
    if (t.yansitmaFaturaNo && yetki('fisAl')) eylemler.push(`<span class="d-inline-flex align-items-center gap-1"><select class="form-select form-select-sm" data-senaryo="${t.no}" style="font-size:.64rem;min-height:24px;padding:1px 22px 1px 6px;width:auto">${SENARYOLAR.map(([k, a]) => `<option value="${k}">${a}</option>`).join('')}</select>${btn('fisal', t.gercek.length ? 'Fişi yeniden oku' : 'Fişi ERP\'den oku', 'btn-outline-secondary', 'fa-download')}</span>`);
    if (t.gercek.length && yetki('kontrol')) eylemler.push(btn('kontrol', 'Kontrolü çalıştır', 'btn-light', 'fa-rotate'));
    eylemler.push(`<button type="button" class="btn btn-light btn-sm py-0" data-detay="${t.no}" style="font-size:.66rem"><i class="fas fa-up-right-from-square me-1"></i>Düzeltmeye yönlendir (detay)</button>`);
    if (!t.yansitmaFaturaNo) { $('#ynKarsilastirma').innerHTML = `<div class="na-bos">${esc(t.no)} henüz ERP yansıtma faturası oluşmamış (${esc(t.durum)}); muhasebe kontrolü gerçek kayıt oluştuktan sonra çalışır.</div><div class="yn-aksiyon mt-2">${eylemler.join('')}</div>`; $('#ynMutabakat').innerHTML = '<div class="na-bos">—</div>'; }
    else {
      const farklar = (t.kontroller || []).filter(k => k.sonuc !== 'UYUMLU');
      $('#ynKarsilastirma').innerHTML = `<div class="d-flex align-items-center gap-2 mb-2 flex-wrap">${ksRozet(t.kontrolDurumu)}<small class="text-muted">${t.kontrolTarihi ? 'kontrol ' + esc(t.kontrolTarihi) : 'kontrol yapılmadı'} · beklenen ${t.simulasyon.length} satır · gerçek ${t.gercek.length} satır (${t.fisler.length} fiş)</small></div>
        ${t.gercek.length ? kontrolTablo(t) : `<div class="na-uyari kirmizi" style="display:block">Yansıtma faturası ${esc(t.yansitmaFaturaNo)} var ancak ERP'de muhasebe fişi bulunamadı → <b>MUHASEBE KAYDI YOK</b>.</div>`}
        ${farklar.length ? `<div class="na-uyari ${farklar.some(f => f.kritik) ? 'kirmizi' : 'sari'} mt-2" style="display:block"><b>Fark açıklaması:</b> ${farklar.map(f => `${esc(f.alan)} (${esc(f.sirket)}): beklenen ${esc(f.beklenen)}, gerçekleşen ${esc(f.gerceklesen)} → ${esc(f.sonuc)}`).join('; ')}. ${farklar.some(f => f.kritik) ? 'Kritik fark: düzeltme fişi / ters kayıt gerekir.' : 'Kritik değil: opsiyonel düzeltme.'}</div>` : t.kontroller.length ? '<div class="na-uyari mavi mt-2" style="display:block">Tüm kritik kontroller geçti; işlem UYUMLU olarak işaretlendi.</div>' : ''}
        <div class="yn-aksiyon mt-2">${eylemler.join('')}</div>`;
      $('#ynMutabakat').innerHTML = mutabakatHtml(t);
      $$('#ynKarsilastirma [data-detay]').forEach(b => b.addEventListener('click', () => detayAc(b.dataset.detay)));
    }
  }
  ['#ynKontrolAra', '#ynKontrolDurumF', '#ynKontrolOncesi'].forEach(s => { $(s).addEventListener('input', () => cizKontrolTablo()); $(s).addEventListener('change', () => cizKontrolTablo()); });
  $('#ynKontrolHepsi').addEventListener('click', async () => { if (!yetki('kontrol')) { mesaj('warning', 'Kontrolü yalnız Muhasebe / Sistem Yöneticisi çalıştırır.'); return; } let n = 0; for (const t of V.islemler.filter(t => t.aktif && t.yansitmaFaturaNo)) { const j = await post('/Yansitma/Kontrol', { no: t.no, rol: ROL }); if (j.islem) { const i = V.islemler.findIndex(x => x.no === j.islem.no); if (i >= 0) V.islemler[i] = j.islem; n++; } } cizHepsi(); cizKontrol(); mesaj('success', `${n} işlemde muhasebe kontrolü yeniden çalıştırıldı.`); });
  $('#ynKontrolExcel').addEventListener('click', () => { const l = kontrolListesi(); const rows = []; l.forEach(t => { if (!t.kontroller.length) rows.push([t.no, t.yansitan, t.yansitilan, t.yansitmaFaturaNo || '', t.fisler.join(', '), '', '', '', '', t.kontrolDurumu || 'KONTROL YOK', '', new Date().toLocaleString('tr-TR')]); t.kontroller.forEach(k => rows.push([t.no, t.yansitan, t.yansitilan, t.yansitmaFaturaNo || '', t.fisler.join(', '), k.sirket, k.alan, k.beklenen, k.gerceklesen, k.sonuc, k.sonuc === 'UYUMLU' ? '' : (k.kritik ? 'kritik fark' : 'politikaya bağlı / tolerans dışı'), new Date().toLocaleString('tr-TR')])); }); csvIndir('yansitma_muhasebe_kontrol', ['Yansıtma No', 'Yansıtan', 'Yansıtılan', 'ERP Faturası', 'Fişler', 'Şirket', 'Kontrol Alanı', 'Beklenen', 'Gerçekleşen', 'Sonuç', 'Fark Nedeni', 'Rapor Zamanı'], rows); });

  // ============================================================
  // BAŞLANGIÇ
  // ============================================================
  function cizHepsi() { cizKpi(); cizListe(); cizOnay(); if (aktifNo && bul(aktifNo)) $('#ynDetay').innerHTML = detayHtml(bul(aktifNo)); if ($('#giRapor').classList.contains('aktif')) cizRapor(); if ($('#giKontrol').classList.contains('aktif')) cizKontrol(); }
  yukle().then(() => {
    cizBaslik(); formSifirla(); cizHepsi();
    const u = kok.dataset.ust || 'islem'; if (u !== 'islem') ust(u);
    if (aktifNo) detayAc(aktifNo); else sekme('yeni');
    window.TM_DETAY_AC = detayAc;
  }).catch(e => { const s = $('#naSonuc'); s.className = 'alert alert-danger py-2 px-3'; s.style.display = 'block'; s.textContent = 'Veri yüklenemedi: ' + e.message; });
})();
