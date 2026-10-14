// ============================================================
// KONSOLİDE FİNANSAL TAKİP PANELİ — modern ekran betiği (Views/Rapor123/Index.cshtml; veri /Rapor123/Veri; kaynak Data/FinekraOrnek.cs + NakitAkisOrnek.cs + CekAkibetOrnek.cs + MuhasebeOrnek.cs)
// Kasa Nakit Akışı tasarım dili: üst kartlar (tıklanınca filtre), sekmeler (Finekra · Banka · Kasa · Çek · Nakit Akış Eşleşme · Bakiyeler · Günlük), sabit düzenli tablolar, çekmece, modallar.
// Finekra: banka hareketleri otomatik akar → Onayla (cari / hesap seç, fiş önizleme) → muhasebe fişi (FinekraOnayla); eşleşmiş nakit akış satırı ÖDENDİ olur, fiş iki kez atılmaz.
// Eşleşme: otomatik (tutar ± 1 ₺, tarih ± 7 gün) ya da elle (FinekraAdaylar → FinekraEslestir); nakit akışta olmayan çıkış "Nakit Akışa Gönder" (FinekraNakitAkisaGonder) ile ÖDENDİ satır olur.
// Kasa / Banka / Çek satırlarında onay / eksik işareti (HareketOnay). Yazım: liste değerleri ve etiketler Title Case.
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('tp'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const para = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const tarih = s => { if (!s) return '—'; const p = String(s).slice(0, 10).split('-'); return p.length === 3 ? `${p[2]}.${p[1]}.${p[0]}` : s; };
  const saat = s => s && String(s).length > 10 ? String(s).slice(11, 16) : '';
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const bos = '<span class="text-muted">—</span>';
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + new Date().toISOString().slice(0, 10) + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 12000); }
  const csrf = ($('input[name="__RequestVerificationToken"]') || {}).value || '';
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrf }, body: JSON.stringify(body) }).then(r => r.json());
  const getJ = url => fetch(url).then(r => r.json());
  const modalKapat = el => { const m = bootstrap.Modal.getOrCreateInstance(el); if (el.classList.contains('show') && !m._isTransitioning) m.hide(); else el.addEventListener('shown.bs.modal', () => m.hide(), { once: true }); };
  const modalAc = el => bootstrap.Modal.getOrCreateInstance(el).show();

  let V = null, F = { sirket: kok.dataset.sirket || '', bas: kok.dataset.bas, bit: kok.dataset.bit, durum: '', ara: '', kpi: '', gun: '' }, acik = null;
  const aktifSekme = () => ($('#tpSekmeler button.aktif') || {}).dataset?.p || 'finekra';
  const bugun = () => V ? V.bugun : new Date().toISOString().slice(0, 10);
  const sirketAdi = db => (V.sirketler.find(x => x.dbName === db) || {}).ad || db;

  // ---------- rozetler
  const onayRozet = o => { const d = (o && o.durum) || ''; const t = o && o.kullanici ? `${o.kullanici}${o.tarih ? ' · ' + tarih(o.tarih) : ''}${o.notIcerik ? ' · ' + o.notIcerik : ''}` : (o && o.notIcerik) || ''; return d === 'ONAYLANDI' ? `<span class="tp-onay onayli" title="${esc(t)}">Onaylandı</span>` : d === 'EKSİK' ? `<span class="tp-onay eksik" title="${esc(t)}">Eksik / Kontrol</span>` : `<span class="tp-onay bekliyor" title="${esc(t)}">Onay Bekliyor</span>`; };
  const fnkOnayRozet = h => h.durum === 'Onaylandı' ? `<span class="tp-onay onayli" title="${esc(h.onaylayan || '')} ${esc(h.onayTarihi || '')}${h.onayNotu ? ' · ' + esc(h.onayNotu) : ''}">Onaylandı</span>` : '<span class="tp-onay bekliyor">Onay Bekliyor</span>';
  const nakitDisi = h => h.giris || h.islemTuru === 'Virman';
  const esRozet = h => h.nakitAkis ? (h.eslesme === 'Nakit Akışa Gönderildi' ? `<span class="tp-es gonder" title="${esc(h.eslesmeNotu)}">Nakit Akışa Gönderildi</span>` : `<span class="tp-es var" title="${esc(h.eslesme + ' · ' + h.eslesmeNotu)}">${esc(h.eslesme === 'Elle' ? 'Elle Eşleşti' : 'Eşleşti')}</span>`) : nakitDisi(h) ? `<span class="tp-es disi" title="${esc(h.eslesmeNotu)}">${h.islemTuru === 'Virman' ? 'Virman' : 'Tahsilat · Nakit Akış Dışı'}</span>` : `<span class="tp-es yok" title="Nakit akışta karşılığı yok">Eşleşmedi</span>`;
  // Kasa Nakit Akışı'na gönder düğmesi — her satırda: eşleşmiş → satırı aç; çıkış → gönder (virman "Diğer" satırı olur); tahsilat (giriş) → pasif (nakit akış ödeme planıdır, giriş satırı açılmaz)
  const gonderBtn = h => h.nakitAkis ? `<a class="btn btn-outline-primary btn-xs" href="${esc(h.nakitAkis.link)}" target="_blank" title="Nakit akışta: Kasa Nakit Akışı #${h.nakitAkis.docEntry} · ${esc(h.nakitAkis.kurum || '')} · ${esc(h.eslesme)} — satırı aç"><i class="fas fa-paper-plane"></i></a>` : h.giris ? `<button type="button" class="btn btn-light border text-muted btn-xs" disabled title="Tahsilat (giriş) hareketi: Kasa Nakit Akışı ödeme planıdır, giriş satırı açılmaz — onaylayıp müşteri carisine (120) kaydedin"><i class="fas fa-paper-plane"></i></button>` : `<button type="button" class="btn btn-primary btn-xs tp-fnk-gonder" title="Kasa Nakit Akışı'na ÖDENDİ satır olarak gönder${h.islemTuru === 'Virman' ? ' (virman: hesaplar arası aktarım, Diğer belge tipiyle)' : ''}"><i class="fas fa-paper-plane"></i></button>`;
  const turSinif = t => /Havale/.test(t) ? 'havale' : /Müşteri/.test(t) ? 'musteri' : /POS/.test(t) ? 'pos' : /Çek/.test(t) ? 'cek' : /Otomatik/.test(t) ? 'oto' : /Vergi/.test(t) ? 'vergi' : /Masraf/.test(t) ? 'masraf' : /Taksidi/.test(t) ? 'kredi' : /Kart/.test(t) ? 'kart' : /Kira/.test(t) ? 'kira' : /Virman/.test(t) ? 'virman' : '';
  const turRozet = t => `<span class="tp-tur ${turSinif(t)}">${esc(t)}</span>`;
  const kontrolRozet = (k, c) => k === 'Uyumlu' ? '<span class="tp-kontrol uyumlu" title="Planlanan tedarikçiye çıktı">Uyumlu</span>' : k === 'Farklı' ? `<span class="tp-kontrol farkli" title="DİKKAT: plan ${esc(c?.planlananTedarikci)}, çıkış ${esc(c?.ibrazYeri)}">FARKLI</span>` : k === 'İbraz Bekliyor' ? '<span class="tp-kontrol bekliyor">İbraz Bekliyor</span>' : k === 'Plansız İbraz' ? '<span class="tp-kontrol plansiz">Plansız İbraz</span>' : bos;
  const durumRozet = d => rozet(d, d === 'ÖDENDİ' ? 'ok' : d === 'Gecikti' ? 'hata' : d === 'Planlandı' ? 'bilgi' : 'bekle');
  const fnkTitle = h => `${h.no} · ${h.banka} ${h.hesapKodu} · ${h.referans} · ${h.aciklama}`;
  // döviz: tutar TL karşılığı üstte, altta döviz tutarı · kur · kaynak (Kasa Nakit Akışı ödenen tutar hücresiyle aynı düzen)
  const dovizli = h => !!h.pb && h.pb !== 'TRY';
  const kur2 = k => (Number(k) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const tutarF = h => dovizli(h) ? `<span title="TL karşılığı · ${para(h.dovizTutar)} ${esc(h.pb)} · kur ${esc(h.kurOzet || '')}">${para(h.tutar)}</span><small title="${para(h.dovizTutar)} ${esc(h.pb)} · ${esc(h.kurOzet || '')}">${kisa(h.dovizTutar)} ${esc(h.pb)} ×${kur2(h.kur)}</small>` : para(h.tutar);
  const yonTutar = h => `${h.yon} · ${para(h.tutar)} ₺${dovizli(h) ? ` (${para(h.dovizTutar)} ${h.pb} · ${h.kurOzet || ''})` : ''}`;
  // SAP: hareket işlenmiş mi (onaylanıp fiş atıldıysa Var)
  const sapRozet = h => h.sap ? `<span class="tp-onay onayli" title="SAP'ye işlendi · fiş ${esc(h.yevmiyeNo)}${h.onayTarihi ? ' · ' + esc(h.onayTarihi) : ''}">Var</span>` : '<span class="tp-onay eksik" title="SAP\'ye işlenmedi: onaylayıp cari / hesap seçince fiş atılır">Yok</span>';

  // ---------- süzgeçler
  const q = () => F.ara.trim().toLocaleLowerCase('tr-TR');
  const araUyar = (...a) => { const s = q(); return !s || a.join(' ').toLocaleLowerCase('tr-TR').includes(s); };
  const onayUyar = d => F.durum === '' || (F.durum === 'bekliyor' ? !d : F.durum === 'onayli' ? d === 'ONAYLANDI' : F.durum === 'eksik' ? d === 'EKSİK' : true);
  const fnkDurumUyar = h => F.durum === '' || (F.durum === 'bekliyor' ? h.durum !== 'Onaylandı' : F.durum === 'onayli' ? h.durum === 'Onaylandı' : F.durum === 'sapYok' ? !h.sap : F.durum === 'sapVar' ? !!h.sap : false);
  const KPI_FNK = { bekleyen: h => h.durum !== 'Onaylandı', eslesmeyen: h => !h.nakitAkis && !nakitDisi(h), sapYok: h => !h.sap };
  const KPI_HAR = { farkli: r => r.cek && r.cek.kontrol === 'Farklı', onaysiz: r => !r.onay.durum };
  const finekraSatirlar = () => V.finekra.filter(h => fnkDurumUyar(h) && (!F.kpi || !KPI_FNK[F.kpi] || KPI_FNK[F.kpi](h)) && araUyar(h.no, h.karsiAd, h.aciklama, h.referans, h.cariAd, h.yevmiyeNo, h.islemTuru, h.banka, h.sirket, h.karsiIban, h.nakitAkis?.kurum, String(h.nakitAkis?.docEntry || '')));
  const hareketSatirlar = tip => V.hareketler.filter(r => r.hesapTipi === tip && onayUyar(r.onay.durum) && (!F.kpi || !KPI_HAR[F.kpi] || KPI_HAR[F.kpi](r)) && araUyar(r.karsiAd, r.aciklama, r.kasaAdi, r.sirket, r.cek?.no, r.cek?.planlananTedarikci, r.cek?.ibrazYeri, String(r.islemNo), r.fnk?.no));
  const fnkBul = id => V.finekra.find(h => h.id === id);
  const fnkYerlestir = h => { if (!h) return; const i = V.finekra.findIndex(x => x.id === h.id); if (i >= 0) V.finekra[i] = h; else V.finekra.unshift(h); };

  // ---------- yükleme / çizim
  async function yukle(sessiz) {
    if (!sessiz) $('#tpKapsam').textContent = 'yükleniyor…';
    let r; try { r = await getJ(`/Rapor123/Veri?sirket=${encodeURIComponent(F.sirket)}&bas=${F.bas}&bit=${F.bit}&gun=${F.gun || ''}`); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r; ciz();
  }
  function ciz() {
    if (!V) return;
    $('#tpKapsam').textContent = `${F.sirket ? sirketAdi(F.sirket) : 'Tüm Şirketler (Konsolide)'} · ${tarih(F.bas)} – ${tarih(F.bit)}${F.kpi ? ' · kart filtresi açık' : ''}`;
    kpi(); sayaclar();
    const p = aktifSekme();
    if (p === 'finekra') cizFinekra(); else if (p === 'eslesme') cizEslesme(); else if (p === 'gunsonu') cizGunSonu(); else if (p === 'bakiye') cizBakiye(); else if (p === 'gunluk') cizGunluk(); else cizHareket(p);
    if (window.scrollY < 5) sabitUstOlc(); else { const kb = $('.na-panel.aktif > .na-kart > .na-kart-b'); if (kb) kok.style.setProperty('--tp-kartb', kb.offsetHeight + 'px'); }
  }
  function sayaclar() { $('#tpN-finekra').textContent = V.finekra.length; ['kasa', 'cek'].forEach(t => $('#tpN-' + t).textContent = V.hareketler.filter(r => r.hesapTipi === t.toUpperCase()).length); $('#tpN-eslesme').textContent = V.eslesme.length; $('#tpN-gunsonu').textContent = (V.gunSonu || []).length; $('#tpN-bakiye').textContent = ((V.bakiyeler || {}).detay || []).length; $('#tpN-gunluk').textContent = V.gunluk.length; }
  function kpi() {
    const o = V.ozet, f = o.finekra;
    const K = [
      { k: '#b45309', l: 'Finekra · Onay Bekleyen', v: String(f.bekleyen), s: `${kisa(f.bekleyenTutar)} ₺ · ${f.toplam} hareket · tıkla: süz`, f: 'bekleyen', p: 'finekra' },
      { k: '#991b1b', l: 'Nakit Akışta Olmayan Çıkış', v: String(f.eslesmeyen), s: `${kisa(f.eslesmeyenTutar)} ₺ · Nakit Akışa Gönder`, f: 'eslesmeyen', p: 'finekra' },
      { k: '#15803d', l: 'Banka Giriş (Finekra)', v: kisa(f.giris), s: `${f.girisAdet} hareket · TL karşılığı`, p: 'finekra' },
      { k: '#1d4ed8', l: 'Banka Çıkış (Finekra)', v: kisa(f.cikis), s: `${f.cikisAdet} hareket · SAP'ye işlenmemiş ${f.sapYok} · tıkla: süz`, f: 'sapYok', p: 'finekra' },
      { k: '#15803d', l: 'Kasa Giriş', v: kisa(o.kasa.giris), s: `${o.kasa.girisAdet} hareket`, p: 'kasa' },
      { k: '#0f766e', l: 'Kasa Çıkış', v: kisa(o.kasa.cikis), s: `${o.kasa.cikisAdet} hareket · onay bekleyen ${o.kasa.onayBekleyen}`, p: 'kasa' },
      { k: '#0369a1', l: 'Çek Giriş (Portföy)', v: kisa(o.cek.giris), s: `${o.cek.girisAdet} hareket`, p: 'cek' },
      { k: '#7c3aed', l: 'Çek Çıkış (Ciro · İbraz)', v: kisa(o.cek.cikis), s: `${o.cek.cikisAdet} hareket`, p: 'cek' },
      { k: '#dc2626', l: 'Plana Aykırı Çek', v: String(o.cek.farkli), s: o.cek.farkli ? 'plan ≠ çıkış · tıkla: süz' : 'uyarı yok', f: 'farkli', p: 'cek' },
      { k: '#9d174d', l: 'Gün Sonu Mutabakatı', v: `${o.gunSonu ? o.gunSonu.bakiyeOnayli : 0}/${o.gunSonu ? o.gunSonu.toplam : 0}`, s: `bakiye onaylı şirket · gün içi onaylı ${o.gunSonu ? o.gunSonu.gunIciOnayli : 0}${o.gunSonu && o.gunSonu.acikBildirim ? ' · açık bildirim ' + o.gunSonu.acikBildirim : ''}`, p: 'gunsonu' }
    ];
    $('#tpKpi').innerHTML = K.map(k => `<div class="${k.f && k.f === F.kpi ? 'aktif' : ''}" style="--k:${k.k}" ${k.f ? `data-kpi="${k.f}"` : ''} data-p="${k.p}" title="${k.f ? 'Tıkla: bu karta göre süz / kaldır' : 'Sekmeye git'}"><div class="l">${esc(k.l)}</div><div class="v">${k.v}</div><div class="s">${esc(k.s)}</div></div>`).join('');
  }
  // ---------- sütunlar
  const K = (ad, w) => ({ ad, w });
  const KOL = {
    finekra: [K('Sıra', 26), K('Tarih', 78), K('Şirket · Banka', 96), K('Referans · No', 86), K('İşlem Türü', 86), K('Karşı Taraf (Banka Kaydı)', 118), K('Banka Açıklaması', 120), K('Giriş (₺)', 88), K('Çıkış (₺)', 88), K('Cari · Muhasebe Hesabı', 116), K('Nakit Akış Eşleşmesi', 122), K('Onay', 92), K('SAP', 44), K('Fiş', 66), K('', 84)],
    banka: [K('Sıra', 26), K('Tarih', 62), K('Şirket', 72), K('Banka Hesabı', 104), K('Karşı Hesap · Cari', 126), K('Açıklama', 190), K('Tip', 34), K('Giriş (₺)', 86), K('Çıkış (₺)', 86), K('Nakit Akış · Banka Hareketi', 112), K('Onay', 88), K('', 64)],
    kasa: [K('Sıra', 26), K('Tarih', 62), K('Şirket', 72), K('Kasa', 100), K('Karşı Hesap · Cari', 126), K('Açıklama', 210), K('Tip', 34), K('Giriş (₺)', 86), K('Çıkış (₺)', 86), K('Nakit Akış', 90), K('Onay', 88), K('', 64)],
    cek: [K('Sıra', 26), K('Tarih', 62), K('Şirket', 72), K('Belge · Vade', 100), K('Karşı Taraf', 110), K('Planlanan Tedarikçi (Nakit Akış)', 118), K('İbraz / Ciro', 140), K('Kontrol', 72), K('Giriş (₺)', 84), K('Çıkış (₺)', 84), K('Onay', 88), K('', 64)],
    eslesme: [K('Sıra', 26), K('Planlanan · Ödenme', 84), K('Şirket', 72), K('Ödenecek Cari', 130), K('Belge', 100), K('Durum', 72), K('Tutar (₺)', 92), K('Banka Hareketi (Finekra)', 150), K('Fark (₺)', 72), K('Dekont · Fiş', 96), K('', 60)]
  };
  function tabloKur(t, kol) {
    const topW = kol.reduce((s, k) => s + k.w, 0); let cg = t.querySelector('colgroup'); if (!cg) { cg = document.createElement('colgroup'); t.insertBefore(cg, t.tHead); }
    cg.innerHTML = kol.map(k => `<col style="width:${(k.w / topW * 100).toFixed(2)}%">`).join('');
    t.tHead.innerHTML = '<tr>' + kol.map(k => `<th class="${/₺|Tutar|Fark/.test(k.ad) ? 's' : ''}" title="${esc(k.ad)}">${esc(k.ad)}</th>`).join('') + '</tr>';
  }
  // ---------- Finekra sekmesi
  function cizFinekra() {
    const f = V.ozet.finekra;
    $('#tpAkis').innerHTML = [
      { l: 'Son Akış', v: `${tarih(f.sonAkis)} ${saat(f.sonAkis)}`, s: 'Finekra API · banka hareketleri otomatik', k: '#0f766e' },
      { l: 'Dönem Hareketi', v: String(f.toplam), s: `giriş ${kisa(f.giris)} · çıkış ${kisa(f.cikis)} ₺`, k: '#1d4ed8' },
      { l: 'Onay Bekleyen', v: String(f.bekleyen), s: `${kisa(f.bekleyenTutar)} ₺ · onaylanınca fiş atılır`, k: '#b45309' },
      { l: 'SAP\'ye İşlendi (Onaylı)', v: String(f.onayli), s: `cari seçildi, fiş atıldı · işlenmemiş ${f.sapYok} (${kisa(f.sapYokTutar)} ₺)`, k: '#15803d' },
      { l: 'Nakit Akışta Olmayan Çıkış', v: String(f.eslesmeyen), s: `${kisa(f.eslesmeyenTutar)} ₺ · Nakit Akışa Gönder`, k: '#991b1b' }
    ].map(k => `<div style="--k:${k.k}"><div class="l">${esc(k.l)}</div><div class="v">${esc(k.v)}</div><div class="s">${esc(k.s)}</div></div>`).join('');
    $('#tpAkisNot').textContent = 'onay → cari / hesap → muhasebe fişi · eşleşme: otomatik (tutar ± 1 ₺, tarih ± 7 gün) ya da elle';
    const t = $('#tpTablo-finekra'), L = finekraSatirlar(); tabloKur(t, KOL.finekra);
    t.tBodies[0].innerHTML = L.length ? L.map((h, i) => {
      const na = h.nakitAkis;
      const nakit = na ? `<a class="tp-link" href="${esc(na.link)}" target="_blank" title="Kasa Nakit Akışı'nda aç">#${na.docEntry}</a> ${esRozet(h)}<small>${esc(na.kurum || '')} · ${esc(na.durum || '')}${na.fark ? ' · fark ' + para(na.fark) : ''}</small>` : `${esRozet(h)}<small>${nakitDisi(h) ? esc(h.eslesmeNotu) : '<span class="text-danger">nakit akışta karşılığı yok</span>'}</small>`;
      const cari = h.durum === 'Onaylandı' ? `<b>${esc(h.cariAd)}</b><small>${esc(h.karsiHesap)} ${esc(h.karsiHesapAdi)}${h.cariKod ? ' · ' + esc(h.cariKod) : ''}</small>` : h.oneri ? `<span class="text-muted" title="öneri (${esc(h.oneri.kaynak)})">${esc(h.oneri.cariAd || '')}</span><small>öneri: ${esc(h.oneri.karsiHesap)} · ${esc(h.oneri.kaynak)}</small>` : bos;
      // her satırda Kasa Nakit Akışı'na gönder: eşleşmişse satırı açar, çıkışsa gönderir (virman "Diğer" olarak), tahsilat (giriş) için pasif (nakit akış ödeme planıdır)
      const islem = `${h.durum !== 'Onaylandı' ? `<button type="button" class="btn btn-success btn-xs tp-fnk-onay" title="Onayla: cari / hesap seç, muhasebe fişi at"><i class="fas fa-check"></i></button> ` : ''}${gonderBtn(h)} ${!h.nakitAkis && !h.giris ? `<button type="button" class="btn btn-light border btn-xs tp-fnk-es" title="Nakit akış satırıyla elle eşleştir"><i class="fas fa-link"></i></button>` : ''}${h.nakitAkis && h.eslesme !== 'Nakit Akışa Gönderildi' ? `<button type="button" class="btn btn-light border btn-xs tp-fnk-es-kaldir" title="Eşleşmeyi kaldır"><i class="fas fa-unlink"></i></button>` : ''}`;
      return `<tr class="tik ${h.durum === 'Onaylandı' ? 'onayli' : 'bekliyor'} ${h.giris ? 'giris' : 'cikis'}" data-id="${h.id}"><td>${i + 1}</td><td title="${esc(h.tarih)}">${tarih(h.gun)}<small>${esc(saat(h.tarih))}</small></td><td class="sol"><b>${esc(h.sirket)}</b><small>${esc(h.banka)} · ${esc(h.hesapKodu)}</small></td><td class="sol" title="${esc(fnkTitle(h))}">${esc(h.referans)}<small>${esc(h.no)}</small></td><td>${turRozet(h.islemTuru)}</td><td class="sol kurum" title="${esc(h.karsiAd)}${h.karsiIban ? ' · ' + esc(h.karsiIban) : ''}">${esc(h.karsiAd)}<small>${esc(h.karsiIban || '')}</small></td><td class="sol" title="${esc(h.aciklama)}">${esc(h.aciklama)}</td><td class="para g">${h.giris ? tutarF(h) : ''}</td><td class="para c">${h.giris ? '' : tutarF(h)}</td><td class="sol">${cari}</td><td class="sol">${nakit}</td><td>${fnkOnayRozet(h)}${h.onaylayan ? `<small>${esc(h.onaylayan)}</small>` : ''}</td><td>${sapRozet(h)}</td><td class="sol">${h.yevmiyeNo ? `<b>${esc(h.yevmiyeNo)}</b>` : bos}</td><td style="white-space:nowrap">${islem}</td></tr>`;
    }).join('') : `<tr><td colspan="${KOL.finekra.length}" class="na-bos text-center">Süzgece uyan banka hareketi yok.</td></tr>`;
    const giris = L.filter(h => h.giris).reduce((s, h) => s + h.tutar, 0), cikis = L.filter(h => !h.giris).reduce((s, h) => s + h.tutar, 0);
    t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="7" class="text-end">TOPLAM (₺) · ${L.length} hareket · bekleyen ${L.filter(h => h.durum !== 'Onaylandı').length} · eşleşmeyen çıkış ${L.filter(h => !h.nakitAkis && !nakitDisi(h)).length}</td><td class="para">${para(giris)}</td><td class="para">${para(cikis)}</td><td colspan="6" class="sol">net ${para(giris - cikis)} ₺ (TL karşılığı) · SAP'ye işlenmemiş ${L.filter(h => !h.sap).length}</td></tr>` : '';
    $('#tpNot-finekra').textContent = '';
  }
  // ---------- Kasa / Banka / Çek
  function cizHareket(p) {
    const t = $('#tpTablo-' + p); if (!t) return; const tip = p.toUpperCase(), L = hareketSatirlar(tip); tabloKur(t, KOL[p]);
    const islem = r => `${r.onay.durum !== 'ONAYLANDI' ? `<button type="button" class="btn btn-success btn-xs tp-onayla" title="Onayla"><i class="fas fa-check"></i></button> ` : ''}${r.onay.durum !== 'EKSİK' ? `<button type="button" class="btn btn-light border btn-xs tp-eksik" title="Eksik / kontrol gerekli işaretle"><i class="fas fa-triangle-exclamation text-danger"></i></button>` : `<button type="button" class="btn btn-light border btn-xs tp-onay-kaldir" title="İşareti kaldır"><i class="fas fa-rotate-left"></i></button>`}`;
    const nakitH = r => r.nakitAkisDocEntry ? `<a class="tp-link" href="${esc(r.nakitAkisLink)}" target="_blank" title="Kasa Nakit Akışı'nda aç">#${r.nakitAkisDocEntry}</a>${r.fnk ? `<small>${esc(r.fnk.no)} · ${esc(r.fnk.durum)}</small>` : (p === 'banka' ? '<small class="text-danger">banka hareketi eşleşmedi</small>' : '<small>nakit ödeme · makbuz</small>')}` : `${bos}<small>${r.yon === 'Giriş' ? 'tahsilat' : 'nakit akış dışı'}</small>`;
    t.tBodies[0].innerHTML = L.length ? L.map((r, i) => {
      const sinif = `tik ${r.onay.durum === 'ONAYLANDI' ? 'onayli' : r.onay.durum === 'EKSİK' ? 'eksik' : 'bekliyor'} ${r.yon === 'Giriş' ? 'giris' : 'cikis'}`;
      if (p === 'cek') { const c = r.cek || {}; return `<tr class="${sinif}" data-key="${esc(r.key)}"><td>${i + 1}</td><td>${tarih(r.tarih)}</td><td class="sol">${esc(r.sirket)}</td><td class="sol"><b>${esc(c.no || '')}</b><small>${esc(c.belge || '')} · vade ${tarih(c.vade)}</small></td><td class="sol kurum" title="${esc(r.karsiAd)}">${esc(r.karsiAd)}<small>${esc(r.karsiKod || '')}</small></td><td class="sol" title="${esc(c.planKaynak || '')}">${esc(c.planlananTedarikci || '—')}<small>${esc(c.planKaynak || '')}</small></td><td class="sol wrap" title="${esc(r.aciklama)}">${c.ibrazYeri ? `${esc(c.ibrazTuru)}: <b>${esc(c.ibrazYeri)}</b><small>${tarih(c.ibrazTarihi)} · ${esc(c.ibrazBelgeNo || '')}</small>` : `<small>${esc(r.aciklama)}</small>`}</td><td>${kontrolRozet(c.kontrol, c)}</td><td class="para g">${r.borc ? para(r.borc) : ''}</td><td class="para c">${r.alacak ? para(r.alacak) : ''}</td><td>${onayRozet(r.onay)}</td><td style="white-space:nowrap">${islem(r)}</td></tr>`; }
      return `<tr class="${sinif}" data-key="${esc(r.key)}"><td>${i + 1}</td><td>${tarih(r.tarih)}</td><td class="sol">${esc(r.sirket)}</td><td class="sol">${esc(r.kasaAdi)}<small>${esc(r.kasaKodu)}</small></td><td class="sol kurum" title="${esc(r.karsiAd)}">${esc(r.karsiAd)}<small>${esc(r.karsiKod || '')}</small></td><td class="sol" title="${esc(r.aciklama)}">${esc(r.aciklama)}</td><td>${esc(r.islemTipi)}</td><td class="para g">${r.borc ? para(r.borc) : ''}</td><td class="para c">${r.alacak ? para(r.alacak) : ''}</td><td class="sol">${nakitH(r)}</td><td>${onayRozet(r.onay)}</td><td style="white-space:nowrap">${islem(r)}</td></tr>`;
    }).join('') : `<tr><td colspan="${KOL[p].length}" class="na-bos text-center">Süzgece uyan hareket yok.</td></tr>`;
    const giris = L.reduce((s, r) => s + r.borc, 0), cikis = L.reduce((s, r) => s + r.alacak, 0), gi = KOL[p].findIndex(k => k.ad.startsWith('Giriş'));
    t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="${gi}" class="text-end">TOPLAM (₺) · ${L.length} hareket · onay bekleyen ${L.filter(r => !r.onay.durum).length} · eksik ${L.filter(r => r.onay.durum === 'EKSİK').length}</td><td class="para">${para(giris)}</td><td class="para">${para(cikis)}</td><td colspan="${KOL[p].length - gi - 2}" class="sol">net ${para(giris - cikis)} ₺</td></tr>` : '';
    $('#tpNot-' + p).textContent = '';
  }
  // ---------- Nakit akış ↔ banka eşleşmesi
  function cizEslesme() {
    const t = $('#tpTablo-eslesme'); tabloKur(t, KOL.eslesme);
    const L = V.eslesme.filter(r => araUyar(r.kurum, r.belge, r.sirket, String(r.docEntry), r.fnk?.no, r.dekontNo, r.yevmiyeNo) && (F.durum === '' || (F.durum === 'bekliyor' ? !r.fnk : F.durum === 'onayli' ? !!r.fnk : false)));
    t.tBodies[0].innerHTML = L.length ? L.map((r, i) => `<tr class="tik ${r.fnk ? 'onayli' : r.durum === 'ÖDENDİ' ? 'eksik' : 'bekliyor'}" data-fnk="${r.fnk ? r.fnk.id : ''}"><td>${i + 1}</td><td>${tarih(r.planlanan)}<small>${r.odenme ? 'ödenme ' + tarih(r.odenme) : ''}</small></td><td class="sol">${esc(r.sirket)}</td><td class="sol kurum" title="${esc(r.kurum)}">${esc(r.kurum)}</td><td class="sol">${esc(r.belge || 'Cari Bakiye')}</td><td>${durumRozet(r.durum)}</td><td class="para">${para(r.tutarTL)}${r.pb !== 'TRY' ? `<small>${para(r.tutar)} ${esc(r.pb)} · ${r.kur}</small>` : ''}</td><td class="sol">${r.fnk ? `<b>${esc(r.fnk.no)}</b> ${r.fnk.eslesme === 'Elle' ? '<span class="tp-es var">Elle</span>' : r.fnk.eslesme === 'Nakit Akışa Gönderildi' ? '<span class="tp-es gonder">Gönderildi</span>' : '<span class="tp-es var">Otomatik</span>'}<small>${tarih(r.fnk.tarih)} · ${esc(r.fnk.banka)} · ${para(r.fnk.tutar)} ₺ · ${esc(r.fnk.durum)}</small>` : `<span class="tp-es yok">Banka Hareketi Yok</span><small>${r.durum === 'ÖDENDİ' ? '<span class="text-danger">ödendi ama bankada görünmüyor</span>' : 'ödeme bekleniyor'}</small>`}</td><td class="para ${r.fnk && r.fnk.fark ? 'text-danger' : ''}">${r.fnk ? (r.fnk.fark ? para(r.fnk.fark) : '0,00') : '—'}</td><td class="sol">${esc(r.dekontNo || '—')}<small>${esc(r.yevmiyeNo || '')}</small></td><td><a class="btn btn-light border btn-xs" href="${esc(r.link)}" target="_blank" title="Kasa Nakit Akışı'nda aç"><i class="fas fa-arrow-up-right-from-square"></i></a></td></tr>`).join('') : `<tr><td colspan="${KOL.eslesme.length}" class="na-bos text-center">Dönemde havale satırı yok.</td></tr>`;
    t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="6" class="text-end">${L.length} satır · eşleşen ${L.filter(r => r.fnk).length} · banka hareketi olmayan ${L.filter(r => !r.fnk).length}</td><td class="para">${para(L.reduce((s, r) => s + r.tutarTL, 0))}</td><td colspan="4"></td></tr>` : '';
    $('#tpNot-eslesme').textContent = '';
  }
  // ---------- Gün Sonu Mutabakatı: 1) gün içi işlemler onayı (Ahmet Özkan) → 2) kalan bakiyeler otomatik (kasa · banka · çek / senet X · R) → 3) fiziksel sayım / bakiye onayı ya da eksik / fazla bildirimi (Berk Berberoğlu)
  const gsRozet = (d, t) => d === 'Onaylandı' ? `<span class="tp-onay onayli" title="${esc(t || '')}">Onaylandı</span>` : d === 'Eksik / Fazla' ? `<span class="tp-onay eksik" title="${esc(t || '')}">Eksik / Fazla</span>` : `<span class="tp-onay bekliyor" title="${esc(t || '')}">Bekliyor</span>`;
  const adetTutar = (a, t, grup) => grup === 'Kasa' || grup === 'Banka' ? `${para(t)} ₺` : `<b>${a}</b> adet · ${para(t)} ₺`;
  const gsBul = db => (V.gunSonu || []).find(g => g.dbName === db);
  function cizGunSonu() {
    const G = V.gunSonu || [], S = V.sorumlular || { gunIci: {}, kasa: {} }; $('#tpGun').value = V.gun || '';
    const gi = G.filter(g => g.gunIci.durum === 'Onaylandı').length, so = G.filter(g => g.sayim.durum === 'Onaylandı').length, ef = G.filter(g => g.sayim.durum === 'Eksik / Fazla').length, acik = (V.bildirimler || []).filter(b => b.durum === 'Açık').length;
    const belge = G.reduce((s, g) => s + g.bakiyeler.filter(b => b.sayilir && b.grup !== 'Kasa').reduce((x, b) => x + b.adet, 0), 0);
    $('#tpAdimlar').innerHTML = [
      { n: 1, l: 'Gün İçi İşlemler Onayı', k: `${S.gunIci.ad || ''} · ${S.gunIci.unvan || ''}`, v: `${gi}/${G.length} şirket`, s: 'günün kasa / banka / çek hareketleri onaylanır; onaysız satırlar toplu onaylanır, bakiyeler kesinleşir', ok: G.length > 0 && gi === G.length },
      { n: 2, l: 'Kalan Bakiyeler (Otomatik)', k: 'Kasa · Banka · Çek / Senet X · R', v: `${belge} belge portföyde`, s: 'açılış − gün içi giriş + gün içi çıkış; gün içi onayından önce ön hesap', ok: G.length > 0 && gi === G.length },
      { n: 3, l: 'Fiziksel Sayım · Bakiye Onayı', k: `${S.kasa.ad || ''} · ${S.kasa.unvan || ''}`, v: `${so}/${G.length} şirket${ef ? ` · ${ef} eksik / fazla` : ''}`, s: acik ? `${acik} açık bildirim — finans çözünce yeniden sayım` : 'fiziksel çek / senet sayımı sistemle eşleşirse onaylanır, değilse eksik / fazla bildirilir', ok: G.length > 0 && so === G.length, hata: ef > 0 }
    ].map(a => `<div class="tp-adim ${a.ok ? 'ok' : a.hata ? 'hata' : ''}"><div class="n">${a.ok ? '<i class="fas fa-check"></i>' : a.n}</div><div><div class="l">${esc(a.l)}</div><div class="k">${esc(a.k)}</div><div class="v">${esc(a.v)}</div><div class="s">${esc(a.s)}</div></div></div>`).join('');
    $('#tpGunSonu').innerHTML = G.length ? G.map(g => {
      const kesin = g.kesin, sayimOk = g.sayim.durum === 'Onaylandı', sayimSatir = t => (g.sayim.satirlar || []).find(s => s.tur === t);
      const btns = `${!kesin ? `<button type="button" class="btn btn-success btn-xs tp-gs-onay" data-db="${esc(g.dbName)}" title="${esc(S.gunIci.ad)} onaylar: günün onaysız kasa / banka / çek hareketleri toplu onaylanır, kalan bakiyeler kesinleşir"><i class="fas fa-check-double me-1"></i>Gün İçi İşlemleri Onayla</button>` : `<button type="button" class="btn btn-light border btn-xs tp-gs-geri" data-db="${esc(g.dbName)}" title="Gün içi onayını geri al (bakiyeler yeniden hesaplanır, sayım sıfırlanır)"><i class="fas fa-rotate-left"></i></button>`}${kesin && !sayimOk ? ` <button type="button" class="btn btn-primary btn-xs tp-gs-sayim" data-db="${esc(g.dbName)}" title="${esc(S.kasa.ad)}: fiziksel çek / senet sayımını gir; eşleşirse onayla, değilse eksik / fazla bildir"><i class="fas fa-list-check me-1"></i>Fiziksel Sayım</button>` : ''}`;
      const rows = g.bakiyeler.map(b => {
        const s = sayimSatir(b.tur), var_ = s && (s.adet != null || s.tutar != null), fa = var_ ? (b.grup === 'Kasa' ? 0 : (s.adet ?? b.adet) - b.adet) : 0, ft = var_ ? Math.round(((s.tutar ?? b.tutar) - b.tutar) * 100) / 100 : 0;
        return `<tr class="${b.sayilir ? '' : 'bilgi'}"><td class="sol"><b>${esc(b.tur)}</b><small>${esc(b.grup)} · ${esc(b.kod)}${b.not ? ' · ' + esc(b.not) : ''}</small></td><td class="para">${adetTutar(b.acilisAdet, b.acilis, b.grup)}</td><td class="para g">${b.girisAdet || b.giris ? '+' + adetTutar(b.girisAdet, b.giris, b.grup) : '—'}</td><td class="para c">${b.cikisAdet || b.cikis ? '−' + adetTutar(b.cikisAdet, b.cikis, b.grup) : '—'}</td><td class="para kalan">${adetTutar(b.adet, b.tutar, b.grup)}</td><td class="para">${!b.sayilir ? '<small>banka: Finekra ile mutabık</small>' : var_ ? adetTutar(s.adet ?? b.adet, s.tutar ?? b.tutar, b.grup) : '<small>sayılmadı</small>'}</td><td class="para ${var_ ? (fa || ft ? 'text-danger' : 'text-success') : ''}">${var_ ? (fa || ft ? `${fa ? (fa > 0 ? '+' : '') + fa + ' adet · ' : ''}${(ft > 0 ? '+' : '') + para(ft)} ₺` : 'eşleşti') : '—'}</td></tr>`;
      }).join('');
      const acikB = (g.bildirimler || []).filter(b => b.durum === 'Açık');
      return `<div class="tp-gs-blok" data-db="${esc(g.dbName)}"><div class="tp-gs-baslik"><b>${esc(g.sirket)}</b> <span class="tp-pill">${tarih(g.gun)}</span>
          <span class="adim">1. Gün İçi ${gsRozet(g.gunIci.durum, g.gunIci.onaylayan ? g.gunIci.onaylayan + ' · ' + g.gunIci.tarih : '')}<small> ${g.gunIci.onaylayan ? `${esc(g.gunIci.onaylayan)} · ${esc(saat(g.gunIci.tarih))}` : `${g.gunIci.hareket} hareket · ${g.gunIci.onaysiz} onaysız${g.gunIci.eksik ? ' · ' + g.gunIci.eksik + ' eksik' : ''}${g.gunIci.finekraBekleyen ? ' · Finekra ' + g.gunIci.finekraBekleyen + ' bekleyen' : ''}`}</small></span>
          <span class="adim">2. Bakiyeler ${kesin ? '<span class="tp-onay onayli">Kesinleşti</span>' : '<span class="tp-onay bekliyor">Ön Hesap</span>'}</span>
          <span class="adim">3. Sayım ${gsRozet(g.sayim.durum, g.sayim.yapan ? g.sayim.yapan + ' · ' + g.sayim.tarih : '')}${g.sayim.yapan ? `<small> ${esc(g.sayim.yapan)} · ${esc(saat(g.sayim.tarih))}</small>` : ''}</span>
          <span class="ms-auto d-flex gap-1">${btns}</span></div>
        <table class="na-tablo tp-tablo tp-gs" data-dinamik="kapali"><colgroup><col style="width:22%"><col style="width:13%"><col style="width:13%"><col style="width:13%"><col style="width:14%"><col style="width:13%"><col style="width:12%"></colgroup><thead><tr><th>Tür · Hesap</th><th class="s">Açılış</th><th class="s">Gün İçi Giriş</th><th class="s">Gün İçi Çıkış</th><th class="s">Kalan (Gün Sonu)</th><th class="s">Fiziksel Sayım (${esc((S.kasa.ad || '').split(' ')[0])})</th><th class="s">Fark</th></tr></thead><tbody>${rows}</tbody></table>
        ${acikB.length ? `<div class="na-uyari sari m-2">${acikB.map(b => `<div><i class="fas fa-triangle-exclamation me-1"></i><b>${esc(b.tur)} ${esc(b.yon)}</b> ${b.adet ? b.adet + ' adet · ' : ''}${para(b.tutar)} ₺${b.belgeNolar ? ' · belgeler: ' + esc(b.belgeNolar) : ''}${b.aciklama ? ' · ' + esc(b.aciklama) : ''} <span class="text-muted">(${esc(b.kullanici)} · ${esc(b.tarih)})</span> <button type="button" class="btn btn-light border btn-xs tp-gs-coz ms-1" data-id="${b.id}" title="Çözüm açıklamasıyla kapat"><i class="fas fa-check me-1"></i>Çözüldü İşaretle</button></div>`).join('')}</div>` : ''}</div>`;
    }).join('') : '<div class="na-bos">Şirket yok.</div>';
    $('#tpNot-gunsonu').textContent = `${tarih(V.gun)} · gün içi onaylı ${gi}/${G.length} · bakiye onaylı ${so}/${G.length}${acik ? ' · açık bildirim ' + acik : ''}`;
    // eksik / fazla bildirimleri
    const B = V.bildirimler || [], tb = $('#tpBildirimler');
    tb.tBodies[0].innerHTML = B.length ? B.map(x => `<tr class="${x.durum === 'Açık' ? 'eksik' : 'onayli'}"><td class="sol">#${x.id}<small>${esc(x.tarih)} · ${esc(x.kullanici)}</small></td><td class="sol">${esc(x.sirket)}<small>${tarih(x.gun)}</small></td><td>${esc(x.tur)}</td><td>${x.yon === 'Eksik' ? '<span class="tp-onay eksik">Eksik</span>' : '<span class="tp-onay bekliyor">Fazla</span>'}<small>sistem ${x.sistemAdet} · sayılan ${x.sayilanAdet}</small></td><td class="para">${x.adet ? x.adet + ' · ' : ''}${para(x.tutar)}</td><td class="sol wrap"><small>${esc(x.belgeNolar || '')}${x.aciklama ? (x.belgeNolar ? ' · ' : '') + esc(x.aciklama) : ''}</small></td><td>${x.durum === 'Açık' ? '<span class="tp-onay eksik">Açık</span>' : `<span class="tp-onay onayli" title="${esc(x.cozum)} · ${esc(x.cozen)} ${esc(x.cozumTarihi || '')}">Çözüldü</span>`}</td><td>${x.durum === 'Açık' ? `<button type="button" class="btn btn-light border btn-xs tp-gs-coz" data-id="${x.id}" title="Çözüm açıklamasıyla kapat"><i class="fas fa-check"></i></button>` : ''}</td></tr>`).join('') : '<tr><td colspan="8" class="na-bos">Bildirim yok.</td></tr>';
    $('#tpNot-bildirim').textContent = `${B.filter(x => x.durum === 'Açık').length} açık · ${B.length} toplam`;
  }
  // ---------- Bakiyeler (ayrıntılı): özet kartları; kasa bakiyeleri şirket bazında nakit · çek X / R · senet X / R, alt toplamlar (X kasa = nakit + çek X + senet X, R kasa = çek R + senet R); banka SAP ↔ Finekra şirket alt toplamlı
  // matris hücresi: kuruşsuz tutar (tam değer, kullanılabilir / planlı / ileri tarihli ayrıntısı ipucunda)
  const adetHucre = (p, sinif) => `<td class="para ${sinif || ''}" title="${p.adet} adet · ${para(p.tutar)} ₺ (bugün itibarıyla portföyde: Portföyde + Planlandı) · kullanılabilir ${p.kullanilabilirAdet} adet · ${para(p.kullanilabilir)} ₺ · planlı ${p.planliAdet} adet · ${para(p.planli)} ₺${p.ileriAdet ? ` · ileri tarihli portföy girişi ${p.ileriAdet} adet · ${para(p.ileri)} ₺ (sayılmadı)` : ''}">${p.adet ? `<b>${p.adet}</b> adet · ${kisa(p.tutar)}` : '—'}<small>${p.adet ? `kul. ${p.kullanilabilirAdet}${p.planliAdet ? ' · plan ' + p.planliAdet : ''}` : ''}${p.ileriAdet ? `${p.adet ? ' · ' : ''}ileri +${p.ileriAdet}` : ''}</small></td>`;
  const tutarHucre = (v, sinif, title) => `<td class="para ${sinif || ''}" title="${esc(title || '')}${title ? ' · ' : ''}${para(v)} ₺">${kisa(v)}</td>`;
  function cizBakiye() {
    const D = (V.bakiyeler || {}).detay || [], b = (V.bakiyeler || {}).banka || [];
    const top = f => D.reduce((s, x) => s + f(x), 0), topP = k => ({ adet: top(x => x[k].adet), tutar: top(x => x[k].tutar), kullanilabilirAdet: top(x => x[k].kullanilabilirAdet), kullanilabilir: top(x => x[k].kullanilabilir), planliAdet: top(x => x[k].planliAdet), planli: top(x => x[k].planli), ileriAdet: top(x => x[k].ileriAdet || 0), ileri: top(x => x[k].ileri || 0) });
    const T = { banka: b.reduce((s, x) => s + x.sapTL, 0), finekra: b.reduce((s, x) => s + x.finekraTL, 0), fark: b.reduce((s, x) => s + x.fark, 0), nakit: top(x => x.nakit), cekX: topP('cekX'), cekR: topP('cekR'), senetX: topP('senetX'), senetR: topP('senetR') };
    T.cek = T.cekX.tutar + T.cekR.tutar; T.senet = T.senetX.tutar + T.senetR.tutar; T.portfoy = T.cek + T.senet; T.xKasa = T.nakit + T.cekX.tutar + T.senetX.tutar; T.rKasa = T.cekR.tutar + T.senetR.tutar; T.genel = T.banka + T.nakit + T.portfoy;
    $('#tpBakiyeOzet').innerHTML = [
      { l: 'Kullanılabilir Banka', v: `${kisa(T.banka)} ₺`, s: `${b.length} hesap · SAP · Finekra farkı ${kisa(T.fark)} ₺`, k: '#1d4ed8' },
      { l: 'Nakit Kasa Toplamı', v: `${kisa(T.nakit)} ₺`, s: `${D.length} şirket`, k: '#0f766e' },
      { l: 'Çek X Toplamı', v: `${kisa(T.cekX.tutar)} ₺`, s: `${T.cekX.adet} adet · kullanılabilir ${kisa(T.cekX.kullanilabilir)} ₺`, k: '#1d4ed8' },
      { l: 'Çek R Toplamı', v: `${kisa(T.cekR.tutar)} ₺`, s: `${T.cekR.adet} adet · kullanılabilir ${kisa(T.cekR.kullanilabilir)} ₺`, k: '#7c3aed' },
      { l: 'Çek + Senet Toplamı', v: `${kisa(T.portfoy)} ₺`, s: `çek ${kisa(T.cek)} · senet ${kisa(T.senet)} (X ${kisa(T.senetX.tutar)} · R ${kisa(T.senetR.tutar)})`, k: '#0369a1' },
      { l: 'X Kasa Toplamı', v: `${kisa(T.xKasa)} ₺`, s: 'nakit + çek X + senet X', k: '#1d4ed8' },
      { l: 'R Kasa Toplamı', v: `${kisa(T.rKasa)} ₺`, s: 'çek R + senet R', k: '#7c3aed' },
      { l: 'Genel Toplam', v: `${kisa(T.genel)} ₺`, s: 'banka + nakit kasa + çek / senet', k: '#15803d' }
    ].map(k => `<div style="--k:${k.k}"><div class="l" title="${esc(k.l)}">${esc(k.l)}</div><div class="v" title="${esc(k.v)}">${esc(k.v)}</div><div class="s" title="${esc(k.s)}">${esc(k.s)}</div></div>`).join('');
    $('#tpNot-bakiye').textContent = `${D.length} şirket · banka ${kisa(T.banka)} · nakit ${kisa(T.nakit)} · çek / senet ${kisa(T.portfoy)} ₺ · X kasa ${kisa(T.xKasa)} · R kasa ${kisa(T.rKasa)} ₺`;
    // kasa matrisi: şirket satırları + TOPLAM
    const tk = $('#tpBakiyeKasa');
    const satir = (x, sinif) => { const cek = x.cekX.tutar + x.cekR.tutar, senet = x.senetX.tutar + x.senetR.tutar, xk = x.nakit + x.cekX.tutar + x.senetX.tutar, rk = x.cekR.tutar + x.senetR.tutar; return `<tr class="${sinif || ''}"><td class="sol" title="${esc(x.sirket)}"><b>${esc(x.sirket)}</b>${x.nakitDetay ? `<small>${esc(x.nakitDetay)}</small>` : ''}</td>${tutarHucre(x.nakit, '', 'Nakit kasa')}${adetHucre(x.cekX, 'x')}${adetHucre(x.cekR, 'r')}<td class="para" title="Çek X + Çek R · ${para(cek)} ₺">${kisa(cek)}<small>${x.cekX.adet + x.cekR.adet} adet</small></td>${adetHucre(x.senetX, 'x')}${adetHucre(x.senetR, 'r')}<td class="para" title="Senet X + Senet R · ${para(senet)} ₺">${kisa(senet)}<small>${x.senetX.adet + x.senetR.adet} adet</small></td><td class="para" title="Çek + Senet · ${para(cek + senet)} ₺">${kisa(cek + senet)}<small>${x.cekX.adet + x.cekR.adet + x.senetX.adet + x.senetR.adet} adet</small></td>${tutarHucre(xk, 'x', 'X kasa = nakit + çek X + senet X')}${tutarHucre(rk, 'r', 'R kasa = çek R + senet R')}${tutarHucre(x.nakit + cek + senet, '', 'Kasa genel toplamı = nakit + çek + senet')}</tr>`; };
    tk.tBodies[0].innerHTML = D.length ? D.map(x => satir(x)).join('') : '<tr><td colspan="12" class="na-bos">Bakiye yok.</td></tr>';
    tk.tFoot.innerHTML = D.length ? satir({ sirket: `TOPLAM · ${D.length} şirket`, nakitDetay: '', nakit: T.nakit, cekX: T.cekX, cekR: T.cekR, senetX: T.senetX, senetR: T.senetR }, 'toplam') : '';
    $('#tpNot-kasa-bakiye').textContent = 'bugün itibarıyla portföy · kuruşsuz, tam değer ve kul. / plan / ileri ayrıntısı hücre ipucunda · X kasa = nakit + çek X + senet X · R kasa = çek R + senet R';
    // banka: SAP ↔ Finekra, şirket bazında kullanılabilir banka bakiyesi alt toplamı
    const tb = $('#tpBakiyeBanka'), gruplar = []; b.forEach(x => { let g = gruplar.find(y => y.sirket === x.sirket); if (!g) { g = { sirket: x.sirket, l: [] }; gruplar.push(g); } g.l.push(x); });
    const sum = (l, k) => l.reduce((s, x) => s + x[k], 0);
    tb.tBodies[0].innerHTML = gruplar.length ? gruplar.map(g => g.l.map(x => `<tr><td class="sol">${esc(x.sirket)}</td><td>${esc(x.hesapKodu)}</td><td class="sol">${esc(x.hesapAdi)}</td><td>${esc(x.pb === 'TRY' ? 'TL' : x.pb)}</td><td class="para">${para(x.sapTL)}${x.pb !== 'TRY' ? `<small>${para(x.sapDoviz)} ${esc(x.pb)}</small>` : ''}</td><td class="para">${para(x.finekraTL)}</td><td class="para ${x.fark ? 'text-danger' : 'text-success'}">${para(x.fark)}</td><td>${tarih(x.guncelleme)}</td></tr>`).join('') + `<tr class="ara"><td colspan="4" class="text-end">Kullanılabilir Banka Bakiyesi · ${esc(g.sirket)} · ${g.l.length} hesap</td><td class="para">${para(sum(g.l, 'sapTL'))}</td><td class="para">${para(sum(g.l, 'finekraTL'))}</td><td class="para ${sum(g.l, 'fark') ? 'text-danger' : 'text-success'}">${para(sum(g.l, 'fark'))}</td><td></td></tr>`).join('') : '<tr><td colspan="8" class="na-bos">Bakiye yok.</td></tr>';
    tb.tFoot.innerHTML = b.length ? `<tr class="toplam"><td colspan="4" class="text-end">KULLANILABİLİR BANKA BAKİYESİ TOPLAMI · ${gruplar.length} şirket · ${b.length} hesap</td><td class="para">${para(T.banka)}</td><td class="para">${para(T.finekra)}</td><td class="para ${T.fark ? 'text-danger' : 'text-success'}">${para(T.fark)}</td><td></td></tr>` : '';
    $('#tpNot-banka-bakiye').textContent = `SAP ile Finekra arasındaki fark: ${para(T.fark)} ₺`;
  }
  // gün içi işlemler onayı penceresi
  let GS = { g: null };
  function gunIciAc(g) {
    GS = { g }; const S = V.sorumlular; $('#tgNo').textContent = `${g.sirket} · ${tarih(g.gun)}`;
    $('#tgBilgi').innerHTML = [['Gün İçi Hareket', String(g.gunIci.hareket)], ['Onaysız Satır', String(g.gunIci.onaysiz)], ['Eksik / Kontrol İşaretli', String(g.gunIci.eksik)], ['Finekra Onay Bekleyen', String(g.gunIci.finekraBekleyen)]].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    $('#tgUyari').innerHTML = `${g.gunIci.onaysiz ? `<i class="fas fa-check-double me-1"></i>${g.gunIci.onaysiz} onaysız kasa / banka / çek satırı toplu onaylanır. ` : '<i class="fas fa-check me-1"></i>Günün tüm hareketleri zaten onaylı. '}${g.gunIci.eksik ? `<i class="fas fa-triangle-exclamation me-1 text-danger"></i>${g.gunIci.eksik} eksik / kontrol işaretli satır hariç tutulur. ` : ''}${g.gunIci.finekraBekleyen ? `<i class="fas fa-building-columns me-1"></i>Finekra'da ${g.gunIci.finekraBekleyen} banka hareketi onay bekliyor (cari seçilmeden fiş atılmaz). ` : ''}Onayla birlikte kalan bakiyeler (kasa, banka, çek / senet X · R) kesinleşir ve ${esc(S.kasa.ad)} fiziksel sayımına açılır.`;
    $('#tgBakiye tbody').innerHTML = g.bakiyeler.map(b => `<tr><td class="sol">${esc(b.tur)}<small style="display:block;color:#64748b">${esc(b.grup)} · ${esc(b.kod)}</small></td><td class="para">${adetTutar(b.acilisAdet, b.acilis, b.grup)}</td><td class="para">${adetTutar(b.girisAdet, b.giris, b.grup)}</td><td class="para">${adetTutar(b.cikisAdet, b.cikis, b.grup)}</td><td class="para"><b>${adetTutar(b.adet, b.tutar, b.grup)}</b></td></tr>`).join('');
    $('#tgOnaylayan').value = `${S.gunIci.ad} · ${S.gunIci.unvan}`; $('#tgNot').value = ''; modalAc($('#tpGunIciModal'));
  }
  $('#tgOnayla').addEventListener('click', async () => {
    const g = GS.g; if (!g) return;
    const d = await post('/Rapor123/GunIciOnayla', { DbName: g.dbName, Gun: g.gun, Onaylayan: V.sorumlular.gunIci.ad, Not: $('#tgNot').value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    modalKapat($('#tpGunIciModal')); mesaj('success', '<i class="fas fa-check-double me-1"></i>' + esc(d.message)); await yukle(true);
  });
  // fiziksel sayım penceresi
  function sayimAc(g) {
    GS = { g }; const S = V.sorumlular; $('#tsNo').textContent = `${g.sirket} · ${tarih(g.gun)}`; $('#tsYapan').value = `${S.kasa.ad} · ${S.kasa.unvan}`; $('#tsNot').value = ''; $('#tsBelge').value = '';
    $('#tsGovde').innerHTML = g.bakiyeler.filter(b => b.sayilir).map(b => { const s = (g.sayim.satirlar || []).find(x => x.tur === b.tur) || {}, kasa = b.grup === 'Kasa'; return `<tr data-tur="${esc(b.tur)}" data-grup="${esc(b.grup)}" data-adet="${b.adet}" data-tutar="${b.tutar}"><td class="sol"><b>${esc(b.tur)}</b><small style="display:block;color:#64748b">${esc(b.not || (kasa ? 'kasa nakit · isteğe bağlı' : ''))}</small></td><td class="para">${kasa ? '—' : b.adet}</td><td class="para">${para(b.tutar)}</td><td>${kasa ? '' : `<input type="number" class="form-control form-control-sm ts-adet" min="0" step="1" value="${s.adet ?? ''}" placeholder="adet">`}</td><td><input type="number" class="form-control form-control-sm ts-tutar" min="0" step="0.01" value="${s.tutar ?? ''}" placeholder="${kasa ? 'boş: sayılmadı' : 'tutar'}"></td><td class="para ts-fark">—</td><td>${kasa ? '' : `<button type="button" class="btn btn-light border btn-xs ts-liste" title="Portföy listesi: fiziksel belgelerle karşılaştır"><i class="fas fa-list"></i></button>`}</td></tr><tr class="ts-liste-satir d-none"><td colspan="7" class="sol p-0"></td></tr>`; }).join('');
    tsHesapla(); modalAc($('#tpSayimModal'));
  }
  function tsHesapla() {
    let fark = 0, eksikGiris = 0;
    $$('#tsGovde tr[data-tur]').forEach(tr => {
      const kasa = tr.dataset.grup === 'Kasa', a = tr.querySelector('.ts-adet'), t = tr.querySelector('.ts-tutar'), f = tr.querySelector('.ts-fark');
      const av = a && a.value !== '' ? +a.value : null, tv = t.value !== '' ? +t.value : null;
      if (av == null && tv == null) { f.textContent = kasa ? 'sayılmadı' : 'girilmedi'; f.className = 'para ts-fark text-muted'; if (!kasa) eksikGiris++; return; }
      const fa = kasa ? 0 : (av ?? +tr.dataset.adet) - +tr.dataset.adet, ft = Math.round(((tv ?? +tr.dataset.tutar) - +tr.dataset.tutar) * 100) / 100;
      if (fa || ft) { fark++; f.innerHTML = `${fa ? (fa > 0 ? '+' : '') + fa + ' adet · ' : ''}${(ft > 0 ? '+' : '') + para(ft)} ₺`; f.className = 'para ts-fark text-danger'; } else { f.textContent = 'eşleşti'; f.className = 'para ts-fark text-success'; }
    });
    $('#tsOzet').textContent = eksikGiris ? `${eksikGiris} tür henüz sayılmadı` : fark ? `${fark} türde fark var → Eksik / Fazla Bildir` : 'tüm sayım sistemle eşleşiyor → onaylanabilir';
    $('#tsOnayla').disabled = eksikGiris > 0 || fark > 0; $('#tsBildir').disabled = eksikGiris > 0 || fark === 0;
  }
  $('#tsGovde').addEventListener('input', tsHesapla);
  $('#tsGovde').addEventListener('click', async e => {
    const b = e.target.closest('.ts-liste'); if (!b) return; const tr = b.closest('tr'), ls = tr.nextElementSibling; if (!ls.classList.contains('d-none')) { ls.classList.add('d-none'); return; }
    ls.classList.remove('d-none'); ls.firstElementChild.innerHTML = '<div class="na-mini p-1"><i class="fas fa-spinner fa-spin"></i> portföy listesi…</div>';
    const d = await getJ(`/Rapor123/GunSonuPortfoy?sirket=${encodeURIComponent(GS.g.dbName)}&gun=${GS.g.gun}&tur=${encodeURIComponent(tr.dataset.tur)}`).catch(() => null); const L = d && d.success ? d.data : [];
    ls.firstElementChild.innerHTML = L.length ? `<table class="na-tablo tp-fis mb-0" data-dinamik="kapali"><thead><tr><th>#</th><th>Belge No</th><th>Banka</th><th>Kimden Geldi</th><th>Vade</th><th class="s">Tutar (₺)</th><th>Akıbet</th><th>Planlanan Tedarikçi</th></tr></thead><tbody>${L.map((k, i) => `<tr><td>${i + 1}</td><td><b>${esc(k.no)}</b>${k.girisGunu ? ' <span class="tp-onay bekliyor">bugün girdi</span>' : ''}</td><td class="sol">${esc(k.banka)}</td><td class="sol">${esc(k.musteri)}</td><td>${tarih(k.vade)}</td><td class="para">${para(k.tutar)}</td><td>${esc(k.islemTuru)}</td><td class="sol">${esc(k.planlanan || '—')}</td></tr>`).join('')}</tbody></table>` : '<div class="na-mini p-1">Portföyde belge yok.</div>';
  });
  async function sayimGonder(onayla) {
    const g = GS.g; if (!g) return;
    const sayim = $$('#tsGovde tr[data-tur]').map(tr => { const a = tr.querySelector('.ts-adet'), t = tr.querySelector('.ts-tutar'); return { Tur: tr.dataset.tur, Adet: a && a.value !== '' ? +a.value : null, Tutar: t.value !== '' ? +t.value : null }; });
    if (!onayla && !confirm('Sayım sistemle eşleşmiyor. Eksik / fazla bildirimi kaydedilsin mi? Finans ekibi bilgilendirilir; bildirim çözülünce yeniden sayım yapılır.')) return;
    const d = await post('/Rapor123/GunSonuSayim', { DbName: g.dbName, Gun: g.gun, SayimYapan: V.sorumlular.kasa.ad, Sayim: sayim, Onayla: onayla, Not: $('#tsNot').value, BelgeNolar: $('#tsBelge').value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    modalKapat($('#tpSayimModal')); mesaj(onayla ? 'success' : 'warning', `<i class="fas ${onayla ? 'fa-check' : 'fa-triangle-exclamation'} me-1"></i>` + esc(d.message)); await yukle(true);
  }
  $('#tsOnayla').addEventListener('click', () => sayimGonder(true)); $('#tsBildir').addEventListener('click', () => sayimGonder(false));
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.tp-gs-onay,.tp-gs-geri,.tp-gs-sayim,.tp-gs-coz'); if (!b) return; e.stopPropagation();
    if (b.classList.contains('tp-gs-coz')) { const c = prompt('Çözüm açıklaması (ör. belge kasada bulundu, kayıt düzeltildi, belge tedarikçiden geri alındı):'); if (c === null) return; const d = await post('/Rapor123/GunSonuBildirimCoz', { Id: +b.dataset.id, Cozum: c }).catch(() => ({ success: false, message: 'Ağ hatası.' })); mesaj(d.success ? 'success' : 'danger', esc(d.message)); if (d.success) await yukle(true); return; }
    const g = gsBul(b.dataset.db); if (!g) return;
    if (b.classList.contains('tp-gs-onay')) gunIciAc(g);
    else if (b.classList.contains('tp-gs-sayim')) sayimAc(g);
    else if (b.classList.contains('tp-gs-geri')) { if (!confirm(`${g.sirket} · ${tarih(g.gun)} gün içi onayı geri alınsın mı? Bakiyeler yeniden canlı hesaplanır, sayım sıfırlanır.`)) return; const d = await post('/Rapor123/GunIciGeriAl', { DbName: g.dbName, Gun: g.gun }).catch(() => ({ success: false, message: 'Ağ hatası.' })); mesaj(d.success ? 'info' : 'danger', esc(d.message)); if (d.success) await yukle(true); }
  });
  $('#tpGun').addEventListener('change', () => { F.gun = $('#tpGun').value; yukle(); });
  function cizGunluk() { const G = V.gunluk.filter(g => araUyar(g.no, g.aciklama, g.kullanici, g.sirket)); $('#tpGunluk tbody').innerHTML = G.length ? G.map(g => `<tr><td>${esc(g.tarih)}</td><td>${esc(g.kullanici)}</td><td>${esc(g.sirket)}</td><td><b>${esc(g.no)}</b></td><td>${rozet(g.islem, /Onay|Gönderildi/.test(g.islem) ? 'ok' : /Kaldır/.test(g.islem) ? 'hata' : 'bilgi')}</td><td class="sol">${esc(g.aciklama)}</td></tr>`).join('') : '<tr><td colspan="6" class="na-bos">Kayıt yok.</td></tr>'; $('#tpNot-gunluk').textContent = `${G.length} kayıt`; }

  // ---------- çekmece: Finekra hareketi
  const ustbilgi = l => `<div class="na-ustbilgi">${l.map(([k, v]) => `<div><div class="l">${esc(k)}</div><div class="v">${esc(v ?? '—')}</div></div>`).join('')}</div>`;
  const fisTablo = f => f ? `<table class="na-tablo tp-fis" data-dinamik="kapali"><thead><tr><th>Hesap</th><th>Hesap Adı · Açıklama</th><th class="s">Borç (₺)</th><th class="s">Alacak (₺)</th></tr></thead><tbody>${f.satirlar.map(s => `<tr><td>${esc(s.hesapKodu)}</td><td class="sol">${esc(s.hesapAdi)}<small style="display:block;color:#64748b">${esc(s.aciklama)}</small></td><td class="para">${s.borc ? para(s.borc) : ''}</td><td class="para">${s.alacak ? para(s.alacak) : ''}</td></tr>`).join('')}</tbody><tfoot><tr class="toplam"><td colspan="2" class="text-end">TOPLAM</td><td class="para">${para(f.toplamBorc)}</td><td class="para">${para(f.toplamAlacak)}</td></tr></tfoot></table>` : '<div class="na-bos">Fiş yok.</div>';
  async function drawerAc(h) {
    if (!h) return; acik = { tip: 'fnk', id: h.id }; const na = h.nakitAkis;
    $('#tpDBaslik').innerHTML = `${esc(h.no)} ${turRozet(h.islemTuru)} ${fnkOnayRozet(h)} ${esRozet(h)} <span class="tp-pill">${esc(h.sirket)} · ${esc(h.banka)}</span>`;
    const btn = (cls, ikon, ad, title, renk) => `<button type="button" class="btn ${renk || 'btn-light border'} btn-sm ${cls}" data-id="${h.id}" title="${esc(title || ad)}"><i class="fas ${ikon} me-1"></i>${ad}</button>`;
    const aks = [];
    if (h.durum !== 'Onaylandı') aks.push(btn('tp-fnk-onay', 'fa-check', 'Onayla · Cari / Hesap Seç · Fiş At', 'Onaylanınca muhasebe fişi atılır; eşleşmiş nakit akış satırı ÖDENDİ olur', 'btn-success'));
    if (!h.nakitAkis && !h.giris) { aks.push(btn('tp-fnk-gonder', 'fa-paper-plane', 'Nakit Akışa Gönder', h.islemTuru === 'Virman' ? 'Virman: Kasa Nakit Akışı\'nda Diğer belge tipiyle ÖDENDİ satır açılır' : 'Kasa Nakit Akışı\'nda ÖDENDİ havale satırı açılır; fiş onayda atılır', 'btn-primary')); aks.push(btn('tp-fnk-es', 'fa-link', 'Nakit Akış Satırıyla Eşleştir', 'Aday satırlardan seç')); }
    if (!h.nakitAkis && h.giris) aks.push(`<button type="button" class="btn btn-primary btn-sm" disabled title="Tahsilat (giriş) hareketi: Kasa Nakit Akışı ödeme planıdır, giriş satırı açılmaz"><i class="fas fa-paper-plane me-1"></i>Nakit Akışa Gönder (tahsilat: ödeme planı dışı)</button>`);
    if (h.nakitAkis) { aks.push(`<a class="btn btn-light border btn-sm" href="${esc(na.link)}" target="_blank"><i class="fas fa-money-bill-transfer me-1"></i>Kasa Nakit Akışı #${na.docEntry}</a>`); if (h.eslesme !== 'Nakit Akışa Gönderildi') aks.push(btn('tp-fnk-es-kaldir', 'fa-unlink', 'Eşleşmeyi Kaldır', 'Nakit akış bağını kaldır')); }
    $('#tpDIc').innerHTML = `<div class="tp-aksiyon">${aks.join('')}</div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-building-columns me-1"></i>Banka Hareketi · Finekra</span><span class="not">akış ${esc(h.akisZamani)} · ${esc(h.kaynak)}</span></div><div class="na-kart-g">${ustbilgi([['Şirket · Banka', `${h.sirket} · ${h.banka} ${h.hesapKodu}`], ['IBAN', h.iban], ['Tarih', `${tarih(h.gun)} ${saat(h.tarih)}`], ['Referans', h.referans], ['İşlem Türü', h.islemTuru], ['Yön · Tutar', yonTutar(h)], ['Karşı Taraf (Banka Kaydı)', h.karsiAd], ['Karşı IBAN', h.karsiIban || '—'], ['Banka Açıklaması', h.aciklama]])}</div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-user-check me-1"></i>Onay · Muhasebe Kaydı</span><span class="not">${h.durum === 'Onaylandı' ? 'onaylandı, fiş atıldı' : 'onay bekliyor: cari / hesap seçilince fiş atılır'}</span></div><div class="na-kart-g">${ustbilgi([['Durum', h.durum], ['Onaylayan · Tarih', h.onaylayan ? `${h.onaylayan} · ${h.onayTarihi}` : '—'], ['Cari', h.cariAd ? `${h.cariAd}${h.cariKod ? ' (' + h.cariKod + ')' : ''}` : (h.oneri ? 'öneri: ' + h.oneri.cariAd : '—')], ['Muhasebe Hesabı', h.karsiHesap ? `${h.karsiHesap} ${h.karsiHesapAdi}` : (h.oneri ? 'öneri: ' + h.oneri.karsiHesap : '—')], ['Fiş', h.yevmiyeNo || '—'], ['Not', h.onayNotu || '—']])}<div id="tpDFis" class="mt-2">${h.yevmiyeNo ? '<div class="na-mini">fiş yükleniyor…</div>' : ''}</div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-link me-1"></i>Kasa Nakit Akışı Eşleşmesi</span><span class="not">${esc(h.eslesmeNotu || '')}</span></div><div class="na-kart-g">${na ? ustbilgi([['Eşleşme', h.eslesme], ['Kayıt', `#${na.docEntry} · ${na.kurum}`], ['Durum', na.durum], ['Nakit Akış Tutarı', `${para(na.tutarTL)} ₺`], ['Fark', `${para(na.fark)} ₺`], ['Planlanan · Ödenme', `${tarih(na.planlanan)} · ${tarih(na.odenme)}`], ['Fiş (Nakit Akış)', na.yevmiyeNo || '—']]) : `<div class="na-uyari ${nakitDisi(h) ? 'mavi' : 'sari'}">${nakitDisi(h) ? (h.giris ? 'Tahsilat (giriş) hareketi: nakit akış ödeme planı dışında; onayla ve müşteri carisine kaydet.' : 'Virman: hesaplar arası aktarım, nakit akış dışı.') : 'Nakit akışta karşılığı yok. "Nakit Akışa Gönder" ile ÖDENDİ havale satırı açılır ya da bir satırla elle eşleştirilir.'}</div>`}</div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Günlük</span></div><div class="na-kart-g"><ul class="na-zaman">${(h.gunluk || []).map(g => `<li class="${/Onay|Gönderildi/.test(g.islem) ? 'fin' : /Kaldır/.test(g.islem) ? 'wf' : 'sis'}"><span class="t">${esc(g.tarih)}</span><b>${esc(g.islem)}</b> · ${esc(g.aciklama)} <span class="k">${esc(g.kullanici)}</span></li>`).join('') || '<li>Kayıt yok.</li>'}</ul></div></div>`;
    $('#tpDrawer').classList.add('acik'); $('#tpPerde').classList.add('acik'); $('#tpDrawer').scrollTop = 0;
    if (h.yevmiyeNo) { const d = await getJ(`/Rapor123/FinekraVeri?id=${h.id}`).catch(() => null); const kap = $('#tpDFis'); if (kap && d && d.success) kap.innerHTML = fisTablo(d.yevmiye); }
  }
  // ---------- çekmece: Kasa / Banka / Çek hareketi
  function hareketDrawerAc(r) {
    acik = { tip: 'har', key: r.key }; const c = r.cek;
    $('#tpDBaslik').innerHTML = `${esc(r.hesapTipi)} #${r.islemNo} ${onayRozet(r.onay)} <span class="tp-pill">${esc(r.sirket)}</span>`;
    const btn = (cls, ikon, ad, renk) => `<button type="button" class="btn ${renk || 'btn-light border'} btn-sm ${cls}" data-key="${esc(r.key)}"><i class="fas ${ikon} me-1"></i>${ad}</button>`;
    $('#tpDIc').innerHTML = `<div class="tp-aksiyon">${r.onay.durum !== 'ONAYLANDI' ? btn('tp-onayla', 'fa-check', 'Onayla', 'btn-success') : ''}${r.onay.durum !== 'EKSİK' ? btn('tp-eksik', 'fa-triangle-exclamation', 'Eksik / Kontrol İşaretle') : btn('tp-onay-kaldir', 'fa-rotate-left', 'İşareti Kaldır')}${r.nakitAkisDocEntry ? `<a class="btn btn-light border btn-sm" href="${esc(r.nakitAkisLink)}" target="_blank"><i class="fas fa-money-bill-transfer me-1"></i>Kasa Nakit Akışı #${r.nakitAkisDocEntry}</a>` : ''}${r.fnk ? `<button type="button" class="btn btn-light border btn-sm tp-fnk-ac" data-id="${r.fnk.id}"><i class="fas fa-building-columns me-1"></i>Banka Hareketi ${esc(r.fnk.no)}</button>` : ''}</div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-list me-1"></i>Hareket</span></div><div class="na-kart-g">${ustbilgi([['Şirket', r.sirket], ['Tarih', tarih(r.tarih)], ['Hesap', `${r.kasaAdi} · ${r.kasaKodu}`], ['Karşı Hesap · Cari', `${r.karsiAd} · ${r.karsiKod || ''}`], ['Yön · Tip', `${r.yon} · ${r.islemTipi}`], ['Tutar', `${para(r.borc || r.alacak)} ₺`], ['Açıklama', r.aciklama], ['Onay', `${r.onay.durum || 'Onay Bekliyor'}${r.onay.kullanici ? ' · ' + r.onay.kullanici + ' ' + tarih(r.onay.tarih) : ''}`], ['Not', r.onay.notIcerik ? `${r.onay.notBaslik || ''} ${r.onay.notIcerik}` : '—']])}</div></div>
      ${c ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-money-check-alt me-1"></i>Çek · Plan ↔ İbraz Kontrolü</span><span class="not">${kontrolRozet(c.kontrol, c)}</span></div><div class="na-kart-g">${ustbilgi([['Belge', `${c.belge} ${c.no}`], ['Vade', tarih(c.vade)], ['Planlanan Tedarikçi', c.planlananTedarikci || '—'], ['Plan Kaynağı', c.planKaynak || '—'], ['Planlama Tarihi', tarih(c.planlamaTarihi)], ['İbraz / Ciro', c.ibrazYeri ? `${c.ibrazTuru}: ${c.ibrazYeri}` : '—'], ['İbraz Tarihi · Belge', c.ibrazYeri ? `${tarih(c.ibrazTarihi)} · ${c.ibrazBelgeNo || ''}` : '—'], ['Kontrol', c.kontrol || '—']])}</div></div>` : ''}`;
    $('#tpDrawer').classList.add('acik'); $('#tpPerde').classList.add('acik'); $('#tpDrawer').scrollTop = 0;
  }
  function drawerKapat() { $('#tpDrawer').classList.remove('acik'); $('#tpPerde').classList.remove('acik'); acik = null; }
  $('#tpDKapat').addEventListener('click', drawerKapat); $('#tpPerde').addEventListener('click', drawerKapat);
  kok.addEventListener('click', e => {
    if (e.target.closest('input,select,a,button')) return;
    const tr = e.target.closest('tr.tik'); if (!tr) return;
    if (tr.dataset.id) { const h = fnkBul(+tr.dataset.id); if (h) drawerAc(h); return; }
    if (tr.dataset.key) { const r = V.hareketler.find(x => x.key === tr.dataset.key); if (r) hareketDrawerAc(r); return; }
    if (tr.dataset.fnk) { const h = fnkBul(+tr.dataset.fnk); if (h) drawerAc(h); }
  });
  kok.addEventListener('click', e => { const b = e.target.closest('.tp-fnk-ac'); if (!b) return; e.stopPropagation(); const h = fnkBul(+b.dataset.id); if (h) drawerAc(h); });
  const satirdanFnk = el => { const b = el.closest('[data-id]'); if (b && b.dataset.id) return fnkBul(+b.dataset.id); const tr = el.closest('tr'); return tr && tr.dataset.id ? fnkBul(+tr.dataset.id) : (acik && acik.tip === 'fnk' ? fnkBul(acik.id) : null); };
  const satirdanHar = el => { const b = el.closest('[data-key]'); const key = b ? b.dataset.key : (acik && acik.tip === 'har' ? acik.key : null); return key ? V.hareketler.find(x => x.key === key) : null; };

  // ---------- onay penceresi: cari / hesap → fiş önizleme → FinekraOnayla
  let O = { h: null };
  const cariBul = ad => (V.cariler || []).find(c => c.ad === ad);
  function onayAc(h) {
    O = { h }; $('#toNo').textContent = `${h.no} · ${h.sirket}`;
    $('#toBilgi').innerHTML = [['Banka · Hesap', `${h.banka} ${h.hesapKodu}`], ['Tarih', `${tarih(h.gun)} ${saat(h.tarih)}`], ['İşlem Türü', h.islemTuru], ['Yön · Tutar', yonTutar(h)], ['Karşı Taraf (Banka)', h.karsiAd], ['Açıklama', h.aciklama]].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    const o = h.oneri || {}; $('#toOneri').innerHTML = `<b>Öneri (${esc(o.kaynak || 'varsayılan')}):</b> ${esc(o.cariAd || '—')} · hesap ${esc(o.karsiHesap || '')}${h.nakitAkis ? ` · nakit akış #${h.nakitAkis.docEntry} ${esc(h.nakitAkis.kurum)} eşleşmiş` : ''}`;
    $('#toCariler').innerHTML = (V.cariler || []).map(c => `<option value="${esc(c.ad)}">${esc(c.kod)} · ${esc(c.tip)}</option>`).join('');
    $('#toHesap').innerHTML = (V.hesaplar || []).map(x => `<option value="${esc(x.kod)}" ${x.kod === o.karsiHesap ? 'selected' : ''}>${esc(x.kod)} ${esc(x.ad)}</option>`).join('');
    $('#toCari').value = o.cariAd || h.karsiAd || ''; $('#toNot').value = ''; toCariDegisti(false); toFis(); modalAc($('#tpOnayModal'));
  }
  function toCariDegisti(hesapGuncelle) { const c = cariBul($('#toCari').value); $('#toCariKod').textContent = c ? `${c.kod} · ${c.tip} · hesap ${c.hesap}` : ($('#toCari').value ? 'listede yok: yeni cari (kod sonra açılır)' : ''); if (c && hesapGuncelle) $('#toHesap').value = c.hesap; toFis(); }
  function toFis() {
    const h = O.h; if (!h) return; const hesap = $('#toHesap').value, ad = ($('#toHesap').selectedOptions[0] || {}).textContent || '', cari = $('#toCari').value || h.karsiAd, banka = `${h.banka} ${h.hesapKodu} · ${h.no}${dovizli(h) ? ` · ${para(h.dovizTutar)} ${h.pb} × ${h.kur} = TL karşılığı` : ''}`;
    const satirlar = h.giris ? [['102', 'Bankalar', banka, h.tutar, 0], [hesap, ad.replace(hesap, '').trim(), `${cari} · ${h.aciklama}`, 0, h.tutar]] : [[hesap, ad.replace(hesap, '').trim(), `${cari} · ${h.aciklama}`, h.tutar, 0], ['102', 'Bankalar', banka, 0, h.tutar]];
    $('#toFis tbody').innerHTML = satirlar.map(s => `<tr><td>${esc(s[0])}</td><td class="sol">${esc(s[1])}<small style="display:block;color:#64748b">${esc(s[2])}</small></td><td class="para">${s[3] ? para(s[3]) : ''}</td><td class="para">${s[4] ? para(s[4]) : ''}</td></tr>`).join('');
    const na = h.nakitAkis; $('#toFisNot').textContent = na && na.yevmiyeNo ? `nakit akış #${na.docEntry} fişi (${na.yevmiyeNo}) kullanılır, mükerrer fiş atılmaz` : 'onaylanınca bu fiş atılır';
    $('#toSenkron').innerHTML = na ? (na.durum === 'ÖDENDİ' ? `<i class="fas fa-link me-1"></i>Nakit akış #${na.docEntry} zaten ÖDENDİ; dekont no satıra yazılır.` : `<i class="fas fa-sync me-1"></i>Nakit akış #${na.docEntry} (${esc(na.durum)}) onayla birlikte <b>ÖDENDİ</b> olur, fişi nakit akıştan atılır.`) : (nakitDisi(h) ? '' : '<i class="fas fa-triangle-exclamation me-1 text-warning"></i>Nakit akışta eşleşen satır yok: fiş atılır; istenirse "Nakit Akışa Gönder" ile satır da açılır.');
  }
  $('#toCari').addEventListener('input', () => toCariDegisti(true)); $('#toHesap').addEventListener('change', toFis);
  $('#toOnayla').addEventListener('click', async () => {
    const h = O.h; if (!h) return; const c = cariBul($('#toCari').value);
    const d = await post('/Rapor123/FinekraOnayla', { Id: h.id, CariKod: c ? c.kod : '', CariAd: $('#toCari').value, KarsiHesap: $('#toHesap').value, Not: $('#toNot').value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    modalKapat($('#tpOnayModal')); fnkYerlestir(d.hareket); mesaj('success', '<i class="fas fa-check me-1"></i>' + esc(d.message)); await yukle(true); if (acik && acik.tip === 'fnk' && acik.id === h.id) drawerAc(fnkBul(h.id));
  });
  kok.addEventListener('click', e => { const b = e.target.closest('.tp-fnk-onay'); if (!b) return; e.stopPropagation(); const h = satirdanFnk(b); if (h) onayAc(h); });
  // toplu onay: eşleşmiş ve bekleyen hareketler önerilen cari / hesapla
  $('#tpTopluOnay').addEventListener('click', async () => {
    const L = finekraSatirlar().filter(h => h.durum !== 'Onaylandı' && h.nakitAkis && h.oneri); if (!L.length) { mesaj('info', 'Eşleşmiş ve onay bekleyen hareket yok (süzgece göre).'); return; }
    if (!confirm(`${L.length} eşleşmiş banka hareketi önerilen cari / hesapla onaylansın mı? Fişler atılır, bağlı nakit akış satırları ÖDENDİ olur.`)) return;
    let ok = 0; const hata = []; for (const h of L) { const d = await post('/Rapor123/FinekraOnayla', { Id: h.id, CariKod: h.oneri.cariKod, CariAd: h.oneri.cariAd, KarsiHesap: h.oneri.karsiHesap, Not: 'Toplu onay' }).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (d.success) ok++; else hata.push(h.no + ': ' + d.message); }
    mesaj(hata.length ? 'warning' : 'success', `<i class="fas fa-check-double me-1"></i>${ok} hareket onaylandı.${hata.length ? ' Hata: ' + esc(hata.join(' · ')) : ''}`); await yukle(true);
  });
  // nakit akışa gönder
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.tp-fnk-gonder'); if (!b) return; e.stopPropagation(); const h = satirdanFnk(b); if (!h) return;
    if (!confirm(`${h.no} · ${h.karsiAd} · ${para(h.tutar)} ₺ Kasa Nakit Akışı'na ÖDENDİ ${h.islemTuru === 'Virman' ? '"Diğer" (virman)' : 'havale'} satırı olarak gönderilsin mi?${h.yevmiyeNo ? ' (fiş ' + h.yevmiyeNo + ' satıra taşınır)' : ' (muhasebe fişi onayda, cari / hesap seçilince atılır)'}`)) return;
    const d = await post('/Rapor123/FinekraNakitAkisaGonder', { Id: h.id }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    fnkYerlestir(d.hareket); mesaj('success', '<i class="fas fa-paper-plane me-1"></i>' + esc(d.message) + (d.satir ? ` <a href="${esc(d.satir.link)}" target="_blank" class="ms-1"><i class="fas fa-arrow-up-right-from-square"></i> Kasa Nakit Akışı'nda aç</a>` : '')); await yukle(true); if (acik && acik.tip === 'fnk' && acik.id === h.id) drawerAc(fnkBul(h.id));
  });
  // eşleştir penceresi
  let E = { h: null };
  async function esAc(h) {
    E = { h }; $('#teNo').textContent = `${h.no} · ${para(h.tutar)} ₺`; $('#teBilgi').innerHTML = [['Banka', `${h.banka} ${h.hesapKodu}`], ['Tarih', tarih(h.gun)], ['Karşı Taraf', h.karsiAd], ['Açıklama', h.aciklama], ['Tutar', `${para(h.tutar)} ₺${dovizli(h) ? ` (${para(h.dovizTutar)} ${h.pb} · ${h.kurOzet || ''})` : ''}`], ['Durum', h.durum]].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    $('#teGovde').innerHTML = '<tr><td colspan="12" class="text-center text-muted py-3"><i class="fas fa-spinner fa-spin"></i> Adaylar getiriliyor…</td></tr>'; $('#teOzet').textContent = ''; modalAc($('#tpEsModal'));
    const d = await getJ(`/Rapor123/FinekraAdaylar?id=${h.id}`).catch(() => null); const L = d && d.success ? d.data : [];
    $('#teGovde').innerHTML = L.length ? L.map(a => `<tr><td><input type="radio" name="teSec" class="form-check-input" value="${a.docEntry}" data-db="${esc(a.dbName)}"></td><td>#${a.docEntry}</td><td class="fw-bold">${esc(a.kurum)}</td><td>${esc(a.belge || 'Cari Bakiye')}</td><td>${tarih(a.planlanan)}</td><td>${tarih(a.odenme)}</td><td>${durumRozet(a.durum)}</td><td class="text-end fw-bold">${para(a.tutarTL)}${a.pb !== 'TRY' ? `<br><small>${para(a.tutar)} ${esc(a.pb)}</small>` : ''}</td><td class="text-end ${a.fark ? 'text-danger' : 'text-success'}">${para(a.fark)}</td><td class="text-end">${a.gun}</td><td><small>${esc(a.iban || '')}</small></td><td><small>${esc(a.yevmiyeNo || '')}</small></td></tr>`).join('') : '<tr><td colspan="12" class="text-center text-muted py-3">Bağlanmamış havale satırı yok.</td></tr>';
    $('#teOzet').textContent = `${L.length} aday · tutar yakınlığına göre`;
  }
  kok.addEventListener('click', e => { const b = e.target.closest('.tp-fnk-es'); if (!b) return; e.stopPropagation(); const h = satirdanFnk(b); if (h) esAc(h); });
  $('#teEslestir').addEventListener('click', async () => {
    const s = $('input[name="teSec"]:checked'); if (!s) { mesaj('warning', 'Bir nakit akış satırı seçin.'); return; }
    const d = await post('/Rapor123/FinekraEslestir', { Id: E.h.id, DbName: s.dataset.db, DocEntry: +s.value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!d.success) { mesaj('danger', esc(d.message)); return; }
    modalKapat($('#tpEsModal')); fnkYerlestir(d.hareket); mesaj('success', '<i class="fas fa-link me-1"></i>' + esc(d.message)); await yukle(true); if (acik && acik.tip === 'fnk' && acik.id === E.h.id) drawerAc(fnkBul(E.h.id));
  });
  kok.addEventListener('click', async e => {
    const b = e.target.closest('.tp-fnk-es-kaldir'); if (!b) return; e.stopPropagation(); const h = satirdanFnk(b); if (!h || !confirm(`${h.no} eşleşmesi kaldırılsın mı?`)) return;
    const d = await post('/Rapor123/FinekraEslesmeKaldir', { Id: h.id }).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (!d.success) { mesaj('danger', esc(d.message)); return; }
    fnkYerlestir(d.hareket); mesaj('info', esc(d.message)); await yukle(true); if (acik && acik.tip === 'fnk' && acik.id === h.id) drawerAc(fnkBul(h.id));
  });
  // Kasa / Banka / Çek satırı onayı
  async function hareketOnay(r, durum) {
    let not = ''; if (durum === 'EKSİK') { not = prompt('Eksik / kontrol notu:', r.onay.notIcerik || ''); if (not === null) return; }
    const d = await post('/Rapor123/HareketOnay', { Key: r.key, Durum: durum, Not: not }).catch(() => ({ success: false, message: 'Ağ hatası.' })); if (!d.success) { mesaj('danger', esc(d.message)); return; }
    r.onay = d.onay; mesaj(durum === 'EKSİK' ? 'warning' : 'success', esc(d.message)); ciz(); if (acik && acik.tip === 'har' && acik.key === r.key) hareketDrawerAc(r);
  }
  kok.addEventListener('click', e => { const b = e.target.closest('.tp-onayla,.tp-eksik,.tp-onay-kaldir'); if (!b) return; e.stopPropagation(); const r = satirdanHar(b); if (r) hareketOnay(r, b.classList.contains('tp-onayla') ? 'ONAYLANDI' : b.classList.contains('tp-eksik') ? 'EKSİK' : ''); });

  // ---------- sekmeler / filtreler / üst kartlar / excel / sabit başlık
  function sekme(p) {
    if (!$(`#tpSekmeler button[data-p="${p}"]`)) p = 'finekra';
    $$('#tpSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#tp > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); ciz();
    try { const u = new URL(location.href); ['hesapTipi', 'tab', 'dbFilter', 'startDate', 'endDate'].forEach(k => u.searchParams.delete(k)); u.searchParams.set('sekme', p); u.searchParams.set('sirket', F.sirket || 'tumu'); history.replaceState(null, '', u); } catch (e) { }
  }
  $$('#tpSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  $('#tpSirket').addEventListener('change', () => { F.sirket = $('#tpSirket').value; yukle(); });
  $('#tpBas').addEventListener('change', () => { F.bas = $('#tpBas').value; yukle(); }); $('#tpBit').addEventListener('change', () => { F.bit = $('#tpBit').value; yukle(); });
  $$('.tp-hizli button').forEach(b => b.addEventListener('click', () => { const bit = new Date(); const bas = new Date(); bas.setDate(bas.getDate() - +b.dataset.gun); F.bas = bas.toISOString().slice(0, 10); F.bit = bit.toISOString().slice(0, 10); $('#tpBas').value = F.bas; $('#tpBit').value = F.bit; yukle(); }));
  $('#tpDurum').addEventListener('change', () => { F.durum = $('#tpDurum').value; ciz(); }); $('#tpAra').addEventListener('input', () => { F.ara = $('#tpAra').value; ciz(); });
  $('#tpTemizle').addEventListener('click', () => { F.durum = ''; F.ara = ''; F.kpi = ''; $('#tpDurum').value = ''; $('#tpAra').value = ''; ciz(); });
  $('#tpKpi').addEventListener('click', e => { const el = e.target.closest('[data-p]'); if (!el) return; const f = el.dataset.kpi || ''; if (f) F.kpi = F.kpi === f ? '' : f; else F.kpi = ''; sekme(el.dataset.p); });
  $('#tpYenile').addEventListener('click', async () => { await yukle(true); mesaj('info', '<i class="fas fa-rotate me-1"></i>Finekra akışı ve listeler yenilendi · son akış ' + esc(V.ozet.finekra.sonAkis)); });
  $('#tpExcel').addEventListener('click', () => {
    const p = aktifSekme();
    const hucreler = r => $$('td', r).map(td => td.textContent.trim().replace(/\s+/g, ' '));
    if (p === 'gunsonu') { const ts = $$('#tpGunSonu table.tp-gs'); if (!ts.length) return; const bas = ['Şirket', ...$$('thead th', ts[0]).map(x => x.textContent.trim())]; const rows = []; ts.forEach(t => { const sirket = t.closest('.tp-gs-blok').querySelector('.tp-gs-baslik b').textContent; $$('tbody tr', t).forEach(r => rows.push([sirket, ...hucreler(r)])); }); csvIndir('gun_sonu_mutabakati', bas, rows); return; }
    if (p === 'bakiye') { const tk = $('#tpBakiyeKasa'), tb = $('#tpBakiyeBanka'); const bas = $$('thead th', tk).map(x => x.textContent.trim()); const rows = [...$$('tbody tr', tk), ...$$('tfoot tr', tk)].filter(r => !r.querySelector('.na-bos')).map(hucreler); rows.push([]); rows.push($$('thead th', tb).map(x => x.textContent.trim())); [...$$('tbody tr', tb), ...$$('tfoot tr', tb)].filter(r => !r.querySelector('.na-bos')).forEach(r => rows.push(hucreler(r))); csvIndir('bakiyeler', bas, rows); return; }
    const t = $('#tpTablo-' + p) || $('#tpGunluk'); if (!t) return; const bas = $$('thead th', t).map(x => x.textContent.trim()); const rows = $$('tbody tr', t).filter(r => !r.querySelector('.na-bos')).map(r => $$('td', r).map(td => td.textContent.trim().replace(/\s+/g, ' '))); csvIndir('takip_paneli_' + p, bas, rows);
  });
  function sabitUstOlc() {
    const tk = kok.closest('.table-container'); if (tk) tk.style.setProperty('overflow', 'visible', 'important');
    let ust = 0; for (const e of document.querySelectorAll('.top-header, .toolbar-container, [class*="sticky"]')) { if (kok.contains(e)) continue; const cs = getComputedStyle(e); if (cs.position !== 'sticky' && cs.position !== 'fixed') continue; const r = e.getBoundingClientRect(); if (r.width < 300 || r.top < -5 || r.top > 220 || r.height === 0) continue; ust = Math.max(ust, r.bottom); }
    kok.style.setProperty('--tp-sabit-ust', Math.round(ust) + 'px'); const kb = $('.na-panel.aktif > .na-kart > .na-kart-b'); if (kb) kok.style.setProperty('--tp-kartb', kb.offsetHeight + 'px');
  }
  window.addEventListener('resize', sabitUstOlc);
  yukle();
})();
