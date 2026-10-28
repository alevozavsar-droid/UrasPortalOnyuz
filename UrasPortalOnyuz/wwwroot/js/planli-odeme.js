// ============================================================
// PLANLI ÖDEMELER (Views/PlanliOdeme/Index.cshtml) — Konsolide Finansal Takip Paneli › Düzenli Ödemeler sekmesiyle AYNI liste ve tasarım (js/takip-paneli.js cizDuzenli / dzFormAc karşılığı)
//   Veri: /PlanliOdemeler/Veri?sirket= (Data/DuzenliOdemeOrnek.cs; aynı nesne). Uçlar: Akit (bu ay / gelecek ay / tek kayıt), Kaydet (yeni / düzenle), Durum (aktif / pasif).
//   Özet kutuları, şirket alt toplamlı liste, çekmece (kayıt formu + akan aylar + günlük) panelle birebir; ek: Bu Ayın Ödeme Takvimi (gün bazında), süzgeçler (şirket, durum, kategori, ödeme şekli, ara), Excel.
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('po'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const para = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const tarih = s => { if (!s) return '—'; const p = String(s).slice(0, 10).split('-'); return p.length === 3 ? `${p[2]}.${p[1]}.${p[0]}` : s; };
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const durumRozet = d => rozet(d, d === 'ÖDENDİ' ? 'ok' : d === 'Gecikti' ? 'hata' : d === 'Planlandı' ? 'bilgi' : 'bekle');
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + new Date().toISOString().slice(0, 10) + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 12000); }
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }).then(r => r.json());
  const getJ = url => fetch(url).then(r => r.json());

  let V = null, F = { sirket: kok.dataset.sirket || '', durum: '', kategori: '', sekil: '', ara: '' }, ilk = true;
  const sirketAdi = db => ((V && V.sirketler) || []).find(x => x.db === db)?.ad || db;
  const D = () => (V && V.duzenli) || { liste: [], ozet: {} };
  const dzBul = id => (D().liste || []).find(d => d.id === id);
  const q = () => (F.ara || '').toLocaleLowerCase('tr-TR');
  const araUyar = (...a) => { const s = q(); return !s || a.join(' ').toLocaleLowerCase('tr-TR').includes(s); };
  const durumUyar = d => F.durum === '' ? true : F.durum === 'odendi' ? !!d.buAy && d.buAy.durum === 'ÖDENDİ' : F.durum === 'bekliyor' ? d.durum === 'Aktif' && !!d.buAy && d.buAy.durum !== 'ÖDENDİ' : F.durum === 'akmadi' ? d.durum === 'Aktif' && !d.buAy : F.durum === 'pasif' ? d.durum !== 'Aktif' : d.durum === 'Aktif';
  const liste = () => (D().liste || []).filter(d => (!F.kategori || d.kategori === F.kategori) && (!F.sekil || d.odemeSekli === F.sekil) && durumUyar(d) && araUyar(d.no, d.tedarikci, d.urunHizmet, d.sirket, d.kategori, d.odemeSekli, d.sorumlu, d.not));

  // ---------- tablo düzeni (Takip Paneli ile aynı sütunlar)
  const K = (ad, w, h) => ({ ad, w, h: h || (/₺|Tutar/.test(ad) ? 'r' : /^(Sıra|Gün|Durum|)$/.test(ad) ? 'c' : 'l') });
  const KOL = [K('Sıra', 30), K('No', 58), K('Tedarikçi', 110), K('Ürün / Hizmet', 118), K('Şirket', 70), K('Ödeme Şekli', 62), K('Gün', 32), K('Ort. Aylık (₺)', 86), K('Kategori', 60), K('Geçen Ay', 96), K('Bu Ay · Nakit Akış', 124), K('Gelecek Ay', 96), K('Sorumlu', 64), K('Durum', 46), K('', 84)];
  function tabloKur(t, kol) {
    const topW = kol.reduce((s, k) => s + k.w, 0); let cg = t.querySelector('colgroup'); if (!cg) { cg = document.createElement('colgroup'); t.insertBefore(cg, t.tHead); }
    cg.innerHTML = kol.map(k => `<col style="width:${(k.w / topW * 100).toFixed(2)}%">`).join('');
    t.tHead.innerHTML = '<tr>' + kol.map(k => `<th class="h-${k.h}" title="${esc(k.ad)}">${esc(k.ad)}</th>`).join('') + '</tr>';
    let st = document.getElementById('poHiza-' + t.id); if (!st) { st = document.createElement('style'); st.id = 'poHiza-' + t.id; document.head.appendChild(st); }
    st.textContent = kol.map((k, i) => `#${t.id} > thead > tr > th:nth-child(${i + 1}), #${t.id} > tbody > tr > td:nth-child(${i + 1}) { text-align: ${k.h === 'r' ? 'right' : k.h === 'c' ? 'center' : 'left'} !important; }`).join('\n');
  }
  const dzAkisHucre = a => !a ? '<span class="tp-es yok">Akmadı</span>' : `<a class="tp-link" href="${esc(a.link)}" target="_blank" rel="noopener" title="Kasa Nakit Akışı'nda aç">#${a.docEntry}</a> ${durumRozet(a.durum)}<small>${a.durum === 'ÖDENDİ' ? 'ödendi ' + tarih(a.odenme) + (a.makbuz ? ' · ' + esc(a.makbuz) : '') : 'planlanan ' + tarih(a.planlanan) + (a.onay && a.onay !== 'Onaylandı' ? ' · ' + esc(a.onay) : '')}</small>`;
  const sekil = s => `<span class="tp-tur ${s === 'Nakit' ? 'kira' : s === 'Havale / EFT' ? 'havale' : 'cek'}">${esc(s)}</span>`;

  // ---------- özet kutuları (panelle aynı sekiz kutu)
  function cizOzet() {
    const o = D().ozet || {};
    $('#poOzet').innerHTML = [
      { l: `Aylık Yük · ${o.ayAdi || ''}`, v: `${kisa(o.aylikYuk)} ₺`, s: `${o.aktif || 0} aktif · ${o.pasif || 0} pasif kayıt`, k: '#0f766e' },
      { l: 'Nakit / Çek', v: `${kisa(o.nakit)} / ${kisa(o.cek)}`, s: `havale ${kisa(o.havale)} ₺ · ödeme şekline göre aylık`, k: '#1d4ed8' },
      { l: 'Bu Ay Akan', v: `${o.akan || 0} / ${o.aktif || 0}`, s: `${kisa(o.akanTutar)} ₺ · akmamış ${o.eksik || 0} (${kisa(o.eksikTutar)} ₺)`, k: o.eksik ? '#b45309' : '#15803d' },
      { l: 'Bu Ay Ödenen', v: `${kisa(o.odenenTutar)} ₺`, s: `${o.odenen || 0} ödeme gerçekleşti`, k: '#15803d' },
      { l: 'Bu Ay Planlı (Bekleyen)', v: `${kisa(o.planliTutar)} ₺`, s: `${o.planli || 0} satır nakit akışta bekliyor`, k: '#0369a1' },
      { l: `Gelecek Ay · ${o.sonrakiAyAdi || ''}`, v: `${o.sonrakiAyAkan || 0} / ${o.aktif || 0}`, s: o.sonrakiAyAkan ? 'nakit akışa planlandı' : 'ay başında otomatik akar · Gelecek Ayı Planla', k: '#7c3aed' },
      { l: 'Şirketler', v: String((o.sirketler || []).length), s: (o.sirketler || []).map(s => `${s.sirket} ${kisa(s.tutar)}`).join(' · '), k: '#9d174d' },
      { l: 'Kategoriler', v: String((o.kategoriler || []).length), s: (o.kategoriler || []).map(k => `${k.kategori} ${kisa(k.tutar)}`).join(' · '), k: '#64748b' }
    ].map(k => `<div style="--k:${k.k}"><div class="l" title="${esc(k.l)}">${esc(k.l)}</div><div class="v" title="${esc(k.v)}">${esc(k.v)}</div><div class="s" title="${esc(k.s)}">${esc(k.s)}</div></div>`).join('');
  }
  // ---------- liste (şirket bazında alt toplamlar; panelle aynı satır düzeni)
  function cizListe() {
    const L = liste(), t = $('#poTablo'); tabloKur(t, KOL); $('#poSorumlu').textContent = D().sorumlu || '';
    const gruplar = []; L.forEach(d => { let g = gruplar.find(x => x.sirket === d.sirket); if (!g) { g = { sirket: d.sirket, l: [] }; gruplar.push(g); } g.l.push(d); });
    const akan = l => l.reduce((s, d) => s + (d.buAy ? d.buAy.tutar : 0), 0), odenen = l => l.reduce((s, d) => s + (d.buAy && d.buAy.durum === 'ÖDENDİ' ? d.buAy.tutar : 0), 0);
    let sira = 0;
    t.tBodies[0].innerHTML = L.length ? gruplar.map(g => g.l.map(d => {
      sira++; const pasif = d.durum !== 'Aktif';
      const islem = `<button type="button" class="btn btn-success btn-xs tp-dz-akit" title="Bu aya nakit akışa akıt" ${d.buAy || pasif ? 'disabled' : ''}><i class="fas fa-play"></i></button> <button type="button" class="btn btn-light border btn-xs tp-dz-duzenle" title="Düzenle"><i class="fas fa-pen"></i></button> <button type="button" class="btn btn-light border btn-xs tp-dz-durum" title="${pasif ? 'Aktif yap' : 'Pasife al (sonraki aylara akmaz)'}"><i class="fas ${pasif ? 'fa-circle-play' : 'fa-pause'}"></i></button>`;
      return `<tr class="tik ${pasif ? 'eksik' : d.buAy ? (d.buAy.durum === 'ÖDENDİ' ? 'onayli' : '') : 'bekliyor'}" data-dz="${d.id}"><td>${sira}</td><td><b>${esc(d.no)}</b></td><td class="sol kurum" title="${esc(d.tedarikci)}">${esc(d.tedarikci)}</td><td class="sol" title="${esc(d.urunHizmet)}">${esc(d.urunHizmet)}${d.not ? `<small>${esc(d.not)}</small>` : ''}</td><td class="sol">${esc(d.sirket)}</td><td>${sekil(d.odemeSekli)}</td><td>${d.gun}</td><td class="para">${para(d.aylikTutar)}</td><td><span class="tp-es disi">${esc(d.kategori)}</span></td><td class="sol">${dzAkisHucre(d.gecenAy)}</td><td class="sol">${dzAkisHucre(d.buAy)}</td><td class="sol">${d.sonrakiAy ? dzAkisHucre(d.sonrakiAy) : '<small>ay başında akar</small>'}</td><td class="sol">${esc(d.sorumlu)}</td><td>${pasif ? '<span class="tp-onay eksik">Pasif</span>' : '<span class="tp-onay onayli">Aktif</span>'}</td><td style="white-space:nowrap">${islem}</td></tr>`;
    }).join('') + `<tr class="ara"><td colspan="7" class="text-end">${esc(g.sirket)} · ${g.l.filter(d => d.durum === 'Aktif').length} aktif kayıt · aylık yük</td><td class="para">${para(g.l.filter(d => d.durum === 'Aktif').reduce((s, d) => s + d.aylikTutar, 0))}</td><td colspan="2" class="sol"><small>bu ay akan ${para(akan(g.l))} ₺</small></td><td class="sol"><small>ödenen ${para(odenen(g.l))} ₺</small></td><td colspan="4"></td></tr>`).join('') : `<tr><td colspan="${KOL.length}" class="na-bos text-center">Süzgece uyan planlı ödeme yok.</td></tr>`;
    const aktif = L.filter(d => d.durum === 'Aktif');
    t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="7" class="text-end">TOPLAM · ${aktif.length} aktif / ${L.length} kayıt · aylık yük (₺)</td><td class="para">${para(aktif.reduce((s, d) => s + d.aylikTutar, 0))}</td><td colspan="2" class="sol">bu ay akan ${para(akan(L))} ₺</td><td class="sol">ödenen ${para(odenen(L))} ₺</td><td colspan="4" class="sol">akmamış ${aktif.filter(d => !d.buAy).length}</td></tr>` : '';
    $('#poSayi').textContent = `${L.length} / ${(D().liste || []).length} kayıt · satıra tıklayınca kayıt, akan aylar ve günlük açılır`;
  }
  // ---------- bu ayın ödeme takvimi: gün bazında kartlar (aktif + pasif; durum nakit akış satırından)
  function cizTakvim() {
    const o = D().ozet || {}, L = liste(), bugun = (V && V.bugun) || '', gunBugun = +bugun.slice(8, 10);
    $('#poTakvimAy').textContent = o.ayAdi || '';
    const gunler = {}; L.forEach(d => { (gunler[d.gun] = gunler[d.gun] || []).push(d); });
    const sirali = Object.keys(gunler).map(Number).sort((a, b) => a - b);
    $('#poTakvim').innerHTML = sirali.length ? sirali.map(g => {
      const l = gunler[g].slice().sort((a, b) => b.aylikTutar - a.aylikTutar), toplam = l.filter(d => d.durum === 'Aktif').reduce((s, d) => s + d.aylikTutar, 0);
      const nokta = d => d.durum !== 'Aktif' ? '' : !d.buAy ? 'yok' : d.buAy.durum === 'ÖDENDİ' ? 'ok' : 'plan';
      return `<div class="po-gun ${g < gunBugun ? 'gecti' : ''} ${g === gunBugun ? 'bugun' : ''}"><div class="g"><b>${g}</b> ${esc((o.ayAdi || '').split(' ')[0])}<span class="t">${kisa(toplam)} ₺</span></div><ul>${l.map(d => `<li title="${esc(d.no + ' · ' + d.tedarikci + ' · ' + d.urunHizmet + ' · ' + d.sirket + ' · ' + para(d.aylikTutar) + ' ₺' + (d.buAy ? ' · nakit akış #' + d.buAy.docEntry + ' ' + d.buAy.durum : d.durum === 'Aktif' ? ' · bu aya akmadı' : ' · pasif'))}"><i class="d ${nokta(d)}"></i><span class="ad">${esc(d.tedarikci)}<small>${esc(d.urunHizmet)} · ${esc(d.sirket)}</small></span><b>${kisa(d.aylikTutar)}</b></li>`).join('')}</ul></div>`;
    }).join('') : '<div class="na-bos">Süzgece uyan kayıt yok.</div>';
  }
  function ciz() { if (!V) return; $('#poKapsam').textContent = `${F.sirket ? sirketAdi(F.sirket) : 'Tüm Şirketler (Konsolide)'} · ${(D().liste || []).length} kayıt`; cizOzet(); cizListe(); cizTakvim(); }

  // ---------- çekmece: kayıt formu + akan aylar + günlük (panelle aynı)
  function dzFormAc(d) {
    const DD = D();
    const sel = (id, liste, v) => `<select class="form-select form-select-sm" id="${id}" style="font-size:.72rem">${(liste || []).map(x => `<option ${x === v ? 'selected' : ''}>${esc(x)}</option>`).join('')}</select>`;
    const inp = (id, v, tip, ph) => `<input class="form-control form-control-sm" id="${id}" type="${tip || 'text'}" value="${esc(v ?? '')}" placeholder="${esc(ph || '')}" style="font-size:.72rem" ${tip === 'number' ? 'min="0" step="any"' : ''}>`;
    $('#poDBaslik').innerHTML = d ? `${esc(d.no)} <span class="tp-pill">${esc(d.tedarikci)} · ${esc(d.sirket)}</span> ${d.durum === 'Aktif' ? '<span class="tp-onay onayli">Aktif</span>' : '<span class="tp-onay eksik">Pasif</span>'}` : '<i class="fas fa-plus me-1"></i>Yeni Planlı Ödeme';
    $('#poDIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-calendar-check me-1"></i>${d ? 'Planlı Ödeme Kaydı' : 'Yeni Planlı Ödeme'}</span><span class="not">sorumlu ${esc(DD.sorumlu || '')} · her ay otomatik nakit akışa akar</span></div><div class="na-kart-g"><div class="row g-2" style="font-size:.72rem">
        <div class="col-md-6"><label class="form-label mb-0">Tedarikçi</label>${inp('dz_tedarikci', d?.tedarikci, 'text', 'ör. CK Boğaziçi')}</div><div class="col-md-6"><label class="form-label mb-0">Ürün / Hizmet</label>${inp('dz_urun', d?.urunHizmet, 'text', 'ör. Elektrik Ödemesi')}</div>
        <div class="col-md-4"><label class="form-label mb-0">Şirket</label>${sel('dz_sirket', DD.sirketler, d?.sirket || (F.sirket ? sirketAdi(F.sirket) : (DD.sirketler || [])[0]))}</div><div class="col-md-4"><label class="form-label mb-0">Ödeme Şekli</label>${sel('dz_sekil', DD.odemeSekilleri, d?.odemeSekli || 'Çek')}</div><div class="col-md-4"><label class="form-label mb-0">Kategori</label>${sel('dz_kategori', DD.kategoriler, d?.kategori || 'Hizmet')}</div>
        <div class="col-md-3"><label class="form-label mb-0">Ödeme Günü (1–28)</label>${inp('dz_gun', d?.gun ?? 10, 'number')}</div><div class="col-md-4"><label class="form-label mb-0">Ort. Aylık Tutar (₺)</label>${inp('dz_tutar', d?.aylikTutar ?? '', 'number')}</div><div class="col-md-5"><label class="form-label mb-0">Not</label>${inp('dz_not', d?.not, 'text', 'isteğe bağlı')}</div>
        <div class="col-12 d-flex gap-2 justify-content-end flex-wrap">${d && d.durum === 'Aktif' && !d.buAy ? '<button type="button" class="btn btn-outline-success btn-sm" id="dz_akit"><i class="fas fa-play me-1"></i>Bu Aya Akıt</button>' : ''}${d ? `<button type="button" class="btn btn-light border btn-sm" id="dz_durum"><i class="fas ${d.durum === 'Aktif' ? 'fa-pause' : 'fa-circle-play'} me-1"></i>${d.durum === 'Aktif' ? 'Pasife Al' : 'Aktif Yap'}</button>` : ''}<button type="button" class="btn btn-success btn-sm" id="dz_kaydet"><i class="fas fa-save me-1"></i>${d ? 'Kaydet' : 'Ekle'}</button></div></div>
        <div class="na-mini mt-2">Değişiklik bir sonraki akışta geçerli olur; bu aya akmış satır Kasa Nakit Akışı'nda düzenlenir. Aynı kayıt Konsolide Finansal Takip Paneli › Düzenli Ödemeler'de de görünür.</div></div></div>
      ${d ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-money-bill-transfer me-1"></i>Nakit Akışa Akan Aylar</span><span class="not">${d.akisSayisi} ay · toplam ${para(d.toplamAkan)} ₺</span></div><div class="na-kart-g p-0"><table class="na-tablo tp-fis" data-dinamik="kapali"><thead><tr><th>Ay</th><th>Kayıt</th><th>Yöntem</th><th>Planlanan</th><th>Durum</th><th class="s">Tutar (₺)</th><th>Makbuz · Fiş</th></tr></thead><tbody>${(d.akislar || []).map(a => `<tr><td>${esc(a.ay)}</td><td><a class="tp-link" href="${esc(a.link)}" target="_blank" rel="noopener">#${a.docEntry}</a></td><td>${esc(a.yontem)}</td><td>${tarih(a.planlanan)}</td><td>${durumRozet(a.durum)}${a.durum === 'ÖDENDİ' ? `<small>${tarih(a.odenme)}</small>` : ''}</td><td class="para">${para(a.tutar)}</td><td><small>${esc(a.makbuz || '')} ${esc(a.yevmiye || '')}</small></td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Henüz akmadı.</td></tr>'}</tbody></table></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Günlük</span></div><div class="na-kart-g"><ul class="na-zaman">${(d.gunluk || []).map(g => `<li class="${/Aktı|Kayıt/.test(g.islem) ? 'fin' : /Pasif/.test(g.islem) ? 'wf' : 'sis'}"><span class="t">${esc(g.tarih)}</span><b>${esc(g.islem)}</b> · ${esc(g.aciklama)} <span class="k">${esc(g.kullanici)}</span></li>`).join('') || '<li>Kayıt yok.</li>'}</ul></div></div>` : ''}`;
    $('#poDrawer').classList.add('acik'); $('#poPerde').classList.add('acik'); $('#poDrawer').scrollTop = 0;
    $('#dz_kaydet').addEventListener('click', async () => {
      const r = await post('/PlanliOdemeler/Kaydet', { Id: d ? d.id : 0, Tedarikci: $('#dz_tedarikci').value, UrunHizmet: $('#dz_urun').value, Sirket: $('#dz_sirket').value, OdemeSekli: $('#dz_sekil').value, Kategori: $('#dz_kategori').value, Gun: +$('#dz_gun').value, AylikTutar: +$('#dz_tutar').value, Not: $('#dz_not').value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
      if (!r.success) { mesaj('danger', esc(r.message)); return; } mesaj('success', '<i class="fas fa-check me-1"></i>' + esc(r.message)); await yukle(true); drawerKapat();
    });
    const ak = $('#dz_akit'); if (ak) ak.addEventListener('click', () => akit(d));
    const du = $('#dz_durum'); if (du) du.addEventListener('click', () => durumDegistir(d));
  }
  function drawerKapat() { $('#poDrawer').classList.remove('acik'); $('#poPerde').classList.remove('acik'); }
  async function akit(d) { const o = D().ozet || {}; const r = await post('/PlanliOdemeler/Akit', { Ay: o.ay, Id: d.id }).catch(() => ({ success: false, message: 'Ağ hatası.' })); mesaj(r.success ? 'success' : 'danger', esc(r.message)); if (r.success) { await yukle(true); drawerKapat(); } }
  async function durumDegistir(d) { const yeni = d.durum === 'Aktif' ? 'Pasif' : 'Aktif'; if (yeni === 'Pasif' && !confirm(`${d.no} ${d.tedarikci} pasife alınsın mı? Sonraki aylara akmaz; açılmış satırlar nakit akışta kalır.`)) return; const r = await post('/PlanliOdemeler/Durum', { Id: d.id, Durum: yeni }).catch(() => ({ success: false, message: 'Ağ hatası.' })); mesaj(r.success ? 'info' : 'danger', esc(r.message)); if (r.success) { await yukle(true); drawerKapat(); } }

  // ---------- olaylar
  kok.addEventListener('click', async e => {
    const b = e.target.closest('#poAkit,#poSonraki,#poYeni,.tp-dz-akit,.tp-dz-duzenle,.tp-dz-durum');
    if (b) {
      e.stopPropagation(); const o = D().ozet || {};
      if (b.id === 'poYeni') { dzFormAc(null); return; }
      if (b.id === 'poAkit' || b.id === 'poSonraki') {
        const ay = b.id === 'poAkit' ? o.ay : o.sonrakiAy, ad = b.id === 'poAkit' ? o.ayAdi : o.sonrakiAyAdi;
        if (!confirm(`${ad} için ${F.sirket ? sirketAdi(F.sirket) : 'tüm şirketlerin'} akmamış planlı ödemeleri Kasa Nakit Akışı'na akıtılsın mı? (aynı aya akmış olanlar atlanır)`)) return;
        const r = await post('/PlanliOdemeler/Akit', { Ay: ay, DbName: F.sirket || '' }).catch(() => ({ success: false, message: 'Ağ hatası.' })); mesaj(r.success ? (r.eklenen ? 'success' : 'info') : 'danger', esc(r.message)); if (r.success) await yukle(true); return;
      }
      const tr = b.closest('tr[data-dz]'); const d = tr ? dzBul(+tr.dataset.dz) : null; if (!d) return;
      if (b.classList.contains('tp-dz-duzenle')) { dzFormAc(d); return; }
      if (b.classList.contains('tp-dz-akit')) { await akit(d); return; }
      if (b.classList.contains('tp-dz-durum')) { await durumDegistir(d); }
      return;
    }
    const tr = e.target.closest('#poTablo tr.tik[data-dz]'); if (tr && !e.target.closest('a,button')) { const d = dzBul(+tr.dataset.dz); if (d) dzFormAc(d); }
  });
  $('#poDKapat').addEventListener('click', drawerKapat); $('#poPerde').addEventListener('click', drawerKapat);
  $('#poSirket').addEventListener('change', () => { F.sirket = $('#poSirket').value; yukle(); });
  $('#poDurum').addEventListener('change', () => { F.durum = $('#poDurum').value; ciz(); }); $('#poKategori').addEventListener('change', () => { F.kategori = $('#poKategori').value; ciz(); }); $('#poSekil').addEventListener('change', () => { F.sekil = $('#poSekil').value; ciz(); });
  $('#poAra').addEventListener('input', () => { F.ara = $('#poAra').value; ciz(); });
  $('#poExcel').addEventListener('click', () => csvIndir('planli_odemeler', ['No', 'Tedarikçi', 'Ürün / Hizmet', 'Şirket', 'Ödeme Şekli', 'Gün', 'Ort. Aylık (₺)', 'Kategori', 'Geçen Ay', 'Bu Ay', 'Gelecek Ay', 'Sorumlu', 'Durum', 'Not'],
    liste().map(d => [d.no, d.tedarikci, d.urunHizmet, d.sirket, d.odemeSekli, d.gun, d.aylikTutar, d.kategori, d.gecenAy ? `#${d.gecenAy.docEntry} ${d.gecenAy.durum}` : 'akmadı', d.buAy ? `#${d.buAy.docEntry} ${d.buAy.durum}` : 'akmadı', d.sonrakiAy ? `#${d.sonrakiAy.docEntry} ${d.sonrakiAy.durum}` : '', d.sorumlu, d.durum, d.not])));
  // pencereye dönünce tazele (panel / Kasa Nakit Akışı'nda yapılan değişiklikler)
  let sonYukleme = 0; document.addEventListener('visibilitychange', () => { if (!document.hidden && Date.now() - sonYukleme > 20000) yukle(true); });

  // ---------- veri
  async function yukle(sessiz) {
    if (!sessiz) $('#poKapsam').textContent = 'yükleniyor…';
    let r; try { r = await getJ(`/PlanliOdemeler/Veri?sirket=${encodeURIComponent(F.sirket)}`); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r; sonYukleme = Date.now();
    if (ilk) {
      ilk = false; const DD = D();
      $('#poSirket').innerHTML = '<option value="">Tüm Şirketler (Konsolide)</option>' + V.sirketler.map(s => `<option value="${esc(s.db)}">${esc(s.ad)}</option>`).join(''); $('#poSirket').value = F.sirket;
      $('#poKategori').innerHTML = '<option value="">Tümü</option>' + (DD.kategoriler || []).map(k => `<option>${esc(k)}</option>`).join(''); $('#poSekil').innerHTML = '<option value="">Tümü</option>' + (DD.odemeSekilleri || []).map(k => `<option>${esc(k)}</option>`).join('');
      if (DD.otomatik) mesaj('info', `<i class="fas fa-rotate me-1"></i>${esc(DD.otomatikMesaj)} (ay başı otomatik akışı tamamlandı)`);
      const id = +kok.dataset.id; if (id) { const d = dzBul(id); if (d) dzFormAc(d); }
    }
    ciz();
  }
  yukle();
})();
