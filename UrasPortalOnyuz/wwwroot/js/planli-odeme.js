// ============================================================
// PLANLI ÖDEMELER (Views/PlanliOdeme/Index.cshtml) — Konsolide Finansal Takip Paneli › Düzenli Ödemeler sekmesiyle AYNI liste ve tasarım (js/takip-paneli.js cizDuzenli / dzFormAc karşılığı)
//   Veri: /PlanliOdemeler/Veri?sirket= (Data/DuzenliOdemeOrnek.cs; aynı nesne). Uçlar: Akit (bu ay / gelecek ay / tek kayıt), Kaydet (yeni / düzenle; bütçe kalemi), Durum (aktif / pasif),
//   PortfoyCekler?id=&ay= (seçilebilir portföy çekleri, vade uygunluğu), CekPlanla (Ciro / Tahsil), CekPlanKaldir.
//   Özet kutuları, şirket alt toplamlı liste (+ Çek Planı sütunu), çekmece (kayıt formu + ÇEK PLANI ay tablosu + portföyden seçim + akan aylar + günlük),
//   Çek Planı Matrisi (kayıt × önümüzdeki aylar: planlanan / ihtiyaç), Bu Ayın Ödeme Takvimi, süzgeçler, Excel.
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('po'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const para = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const pct = v => (Math.round((Number(v) || 0) * 100)) + '%';
  const tarih = s => { if (!s) return '—'; const p = String(s).slice(0, 10).split('-'); return p.length === 3 ? `${p[2]}.${p[1]}.${p[0]}` : s; };
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const durumRozet = d => rozet(d, d === 'ÖDENDİ' ? 'ok' : d === 'Gecikti' ? 'hata' : d === 'Planlandı' ? 'bilgi' : 'bekle');
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + new Date().toISOString().slice(0, 10) + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 12000); }
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }).then(r => r.json());
  const getJ = url => fetch(url).then(r => r.json());

  let V = null, F = { sirket: kok.dataset.sirket || '', durum: '', kategori: '', sekil: '', ara: '', matrisAy: 6 }, ilk = true, acikId = 0, CS = null;   // CS: açık çek seçimi { id, ay, veri, secim:Set }
  const sirketAdi = db => ((V && V.sirketler) || []).find(x => x.db === db)?.ad || db;
  const D = () => (V && V.duzenli) || { liste: [], ozet: {} };
  const dzBul = id => (D().liste || []).find(d => d.id === id);
  const q = () => (F.ara || '').toLocaleLowerCase('tr-TR');
  const araUyar = (...a) => { const s = q(); return !s || a.join(' ').toLocaleLowerCase('tr-TR').includes(s); };
  const durumUyar = d => F.durum === '' ? true : F.durum === 'odendi' ? !!d.buAy && d.buAy.durum === 'ÖDENDİ' : F.durum === 'bekliyor' ? d.durum === 'Aktif' && !!d.buAy && d.buAy.durum !== 'ÖDENDİ' : F.durum === 'akmadi' ? d.durum === 'Aktif' && !d.buAy : F.durum === 'cekli' ? !!(d.cekPlani && d.cekPlani.planliAy) : F.durum === 'ceksiz' ? d.durum === 'Aktif' && !(d.cekPlani && d.cekPlani.planliAy) : F.durum === 'pasif' ? d.durum !== 'Aktif' : d.durum === 'Aktif';
  const liste = () => (D().liste || []).filter(d => (!F.kategori || d.kategori === F.kategori) && (!F.sekil || d.odemeSekli === F.sekil) && durumUyar(d) && araUyar(d.no, d.tedarikci, d.urunHizmet, d.sirket, d.kategori, d.odemeSekli, d.sorumlu, d.not, d.butceKalemAd));
  const cp = d => d.cekPlani || { aylar: [], planliAy: 0, toplamPlanlanan: 0, ozet: '' };

  // ---------- tablo düzeni (Takip Paneli ile aynı sütunlar + Çek Planı)
  const K = (ad, w, h) => ({ ad, w, h: h || (/₺|Tutar/.test(ad) ? 'r' : /^(Sıra|Gün|Durum|)$/.test(ad) ? 'c' : 'l') });
  const KOL = [K('Sıra', 30), K('No', 58), K('Tedarikçi', 104), K('Ürün / Hizmet', 110), K('Şirket', 68), K('Ödeme Şekli', 60), K('Gün', 32), K('Ort. Aylık (₺)', 84), K('Kategori', 58), K('Geçen Ay', 92), K('Bu Ay · Nakit Akış', 118), K('Gelecek Ay', 92), K('Çek Planı', 112), K('Sorumlu', 62), K('Durum', 46), K('', 100)];
  function tabloKur(t, kol) {
    const topW = kol.reduce((s, k) => s + k.w, 0); let cg = t.querySelector('colgroup'); if (!cg) { cg = document.createElement('colgroup'); t.insertBefore(cg, t.tHead); }
    cg.innerHTML = kol.map(k => `<col style="width:${(k.w / topW * 100).toFixed(2)}%">`).join('');
    t.tHead.innerHTML = '<tr>' + kol.map(k => `<th class="h-${k.h}" title="${esc(k.ad)}">${esc(k.ad)}</th>`).join('') + '</tr>';
    let st = document.getElementById('poHiza-' + t.id); if (!st) { st = document.createElement('style'); st.id = 'poHiza-' + t.id; document.head.appendChild(st); }
    st.textContent = kol.map((k, i) => `#${t.id} > thead > tr > th:nth-child(${i + 1}), #${t.id} > tbody > tr > td:nth-child(${i + 1}) { text-align: ${k.h === 'r' ? 'right' : k.h === 'c' ? 'center' : 'left'} !important; }`).join('\n');
  }
  const dzAkisHucre = a => !a ? '<span class="tp-es yok">Akmadı</span>' : `<a class="tp-link" href="${esc(a.link)}" target="_blank" rel="noopener" title="Kasa Nakit Akışı'nda aç">#${a.docEntry}</a> ${durumRozet(a.durum)}<small>${a.durum === 'ÖDENDİ' ? 'ödendi ' + tarih(a.odenme) + (a.makbuz ? ' · ' + esc(a.makbuz) : '') : 'planlanan ' + tarih(a.planlanan) + (a.onay && a.onay !== 'Onaylandı' ? ' · ' + esc(a.onay) : '')}</small>`;
  const sekil = s => `<span class="tp-tur ${s === 'Nakit' ? 'kira' : s === 'Havale / EFT' ? 'havale' : 'cek'}">${esc(s)}</span>`;
  const cekHucre = d => { const c = cp(d); return c.planliAy ? `<b>→ ${esc(c.sonPlanliAyAdi)}</b><small>${c.planliAy} ay · ${kisa(c.toplamPlanlanan)} ₺${c.tamSonAyAdi ? ' · tam ' + esc(c.tamSonAyAdi) : ' · tam ay yok'}</small>` : (d.durum === 'Aktif' ? '<span class="tp-es yok">Çek Planı Yok</span>' : '<span class="tp-es disi">—</span>'); };

  // ---------- özet kutuları (panelle aynı sekiz kutu + çek planı)
  function cizOzet() {
    const o = D().ozet || {}, yuk = o.aylikYuk || 0;
    $('#poOzet').innerHTML = [
      { l: `Aylık Yük · ${o.ayAdi || ''}`, v: `${kisa(o.aylikYuk)} ₺`, s: `${o.aktif || 0} aktif · ${o.pasif || 0} pasif kayıt`, k: '#0f766e' },
      { l: 'Nakit / Çek', v: `${kisa(o.nakit)} / ${kisa(o.cek)}`, s: `havale ${kisa(o.havale)} ₺ · ödeme şekline göre aylık`, k: '#1d4ed8' },
      { l: 'Bu Ay Akan', v: `${o.akan || 0} / ${o.aktif || 0}`, s: `${kisa(o.akanTutar)} ₺ · akmamış ${o.eksik || 0} (${kisa(o.eksikTutar)} ₺)`, k: o.eksik ? '#b45309' : '#15803d' },
      { l: 'Bu Ay Ödenen', v: `${kisa(o.odenenTutar)} ₺`, s: `${o.odenen || 0} ödeme gerçekleşti`, k: '#15803d' },
      { l: 'Bu Ay Planlı (Bekleyen)', v: `${kisa(o.planliTutar)} ₺`, s: `${o.planli || 0} satır nakit akışta bekliyor`, k: '#0369a1' },
      { l: `Gelecek Ay · ${o.sonrakiAyAdi || ''}`, v: `${o.sonrakiAyAkan || 0} / ${o.aktif || 0}`, s: o.sonrakiAyAkan ? 'nakit akışa planlandı' : 'ay başında otomatik akar · Gelecek Ayı Planla', k: '#7c3aed' },
      { l: 'Çek Planı · Toplam', v: `${kisa(o.cekPlanToplam)} ₺`, s: o.cekPlanliKayit ? `${o.cekPlanliKayit} kayıt · ${o.cekPlanAdet || 0} belge · en ileri ${o.cekEnIleriAyAdi || '—'}` : 'henüz çek planlanmadı', k: '#9d174d' },
      { l: `Çek Karşılama · ${o.ayAdi || ''}`, v: pct(yuk ? (o.buAyCekPlanlanan || 0) / yuk : 0), s: `${kisa(o.buAyCekPlanlanan)} ₺ çek / ${kisa(yuk)} ₺ yük · ${o.buAyCekTam || 0} kayıt tam`, k: '#b45309' },
      { l: `Çek Karşılama · ${o.sonrakiAyAdi || ''}`, v: pct(yuk ? (o.sonrakiAyCekPlanlanan || 0) / yuk : 0), s: `${kisa(o.sonrakiAyCekPlanlanan)} ₺ çek · ${o.sonrakiAyCekTam || 0} kayıt tam`, k: '#0369a1' },
      { l: 'Şirketler · Kategoriler', v: `${(o.sirketler || []).length} · ${(o.kategoriler || []).length}`, s: (o.sirketler || []).map(s => `${s.sirket} ${kisa(s.tutar)}`).join(' · '), k: '#64748b' }
    ].map(k => `<div style="--k:${k.k}"><div class="l" title="${esc(k.l)}">${esc(k.l)}</div><div class="v" title="${esc(k.v)}">${esc(k.v)}</div><div class="s" title="${esc(k.s)}">${esc(k.s)}</div></div>`).join('');
  }
  // ---------- liste (şirket bazında alt toplamlar; panelle aynı satır düzeni + Çek Planı)
  function cizListe() {
    const L = liste(), t = $('#poTablo'); tabloKur(t, KOL); $('#poSorumlu').textContent = D().sorumlu || '';
    const gruplar = []; L.forEach(d => { let g = gruplar.find(x => x.sirket === d.sirket); if (!g) { g = { sirket: d.sirket, l: [] }; gruplar.push(g); } g.l.push(d); });
    const akan = l => l.reduce((s, d) => s + (d.buAy ? d.buAy.tutar : 0), 0), odenen = l => l.reduce((s, d) => s + (d.buAy && d.buAy.durum === 'ÖDENDİ' ? d.buAy.tutar : 0), 0), cekli = l => l.reduce((s, d) => s + cp(d).toplamPlanlanan, 0);
    let sira = 0;
    t.tBodies[0].innerHTML = L.length ? gruplar.map(g => g.l.map(d => {
      sira++; const pasif = d.durum !== 'Aktif';
      const islem = `<button type="button" class="btn btn-success btn-xs tp-dz-akit" title="Bu aya nakit akışa akıt" ${d.buAy || pasif ? 'disabled' : ''}><i class="fas fa-play"></i></button> <button type="button" class="btn btn-outline-primary btn-xs tp-dz-cek" title="Çek planla: portföyden ödeme gününe uygun çek seç" ${pasif ? 'disabled' : ''}><i class="fas fa-money-check-dollar"></i></button> <button type="button" class="btn btn-light border btn-xs tp-dz-duzenle" title="Düzenle"><i class="fas fa-pen"></i></button> <button type="button" class="btn btn-light border btn-xs tp-dz-durum" title="${pasif ? 'Aktif yap' : 'Pasife al (sonraki aylara akmaz)'}"><i class="fas ${pasif ? 'fa-circle-play' : 'fa-pause'}"></i></button>`;
      return `<tr class="tik ${pasif ? 'eksik' : d.buAy ? (d.buAy.durum === 'ÖDENDİ' ? 'onayli' : '') : 'bekliyor'}" data-dz="${d.id}"><td>${sira}</td><td><b>${esc(d.no)}</b></td><td class="sol kurum" title="${esc(d.tedarikci)}">${esc(d.tedarikci)}</td><td class="sol" title="${esc(d.urunHizmet)}">${esc(d.urunHizmet)}${d.not ? `<small>${esc(d.not)}</small>` : ''}</td><td class="sol">${esc(d.sirket)}</td><td>${sekil(d.odemeSekli)}</td><td>${d.gun}</td><td class="para">${para(d.aylikTutar)}</td><td><span class="tp-es disi" title="bütçe kalemi: ${esc(d.butceKalemAd || '')}">${esc(d.kategori)}</span></td><td class="sol">${dzAkisHucre(d.gecenAy)}</td><td class="sol">${dzAkisHucre(d.buAy)}</td><td class="sol">${d.sonrakiAy ? dzAkisHucre(d.sonrakiAy) : '<small>ay başında akar</small>'}</td><td class="sol">${cekHucre(d)}</td><td class="sol">${esc(d.sorumlu)}</td><td>${pasif ? '<span class="tp-onay eksik">Pasif</span>' : '<span class="tp-onay onayli">Aktif</span>'}</td><td style="white-space:nowrap">${islem}</td></tr>`;
    }).join('') + `<tr class="ara"><td colspan="7" class="text-end">${esc(g.sirket)} · ${g.l.filter(d => d.durum === 'Aktif').length} aktif kayıt · aylık yük</td><td class="para">${para(g.l.filter(d => d.durum === 'Aktif').reduce((s, d) => s + d.aylikTutar, 0))}</td><td colspan="2" class="sol"><small>bu ay akan ${para(akan(g.l))} ₺</small></td><td class="sol"><small>ödenen ${para(odenen(g.l))} ₺</small></td><td class="sol"></td><td class="sol"><small>çek planı ${para(cekli(g.l))} ₺</small></td><td colspan="3"></td></tr>`).join('') : `<tr><td colspan="${KOL.length}" class="na-bos text-center">Süzgece uyan planlı ödeme yok.</td></tr>`;
    const aktif = L.filter(d => d.durum === 'Aktif');
    t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="7" class="text-end">TOPLAM · ${aktif.length} aktif / ${L.length} kayıt · aylık yük (₺)</td><td class="para">${para(aktif.reduce((s, d) => s + d.aylikTutar, 0))}</td><td colspan="2" class="sol">bu ay akan ${para(akan(L))} ₺</td><td class="sol">ödenen ${para(odenen(L))} ₺</td><td class="sol">akmamış ${aktif.filter(d => !d.buAy).length}</td><td class="sol">çek planı ${para(cekli(L))} ₺ · ${L.filter(d => cp(d).planliAy).length} kayıt</td><td colspan="3"></td></tr>` : '';
    $('#poSayi').textContent = `${L.length} / ${(D().liste || []).length} kayıt · satıra tıklayınca kayıt, çek planı, akan aylar ve günlük açılır`;
  }
  // ---------- çek planı matrisi: kayıt × önümüzdeki N ay — planlanan / ihtiyaç (tam · kısmi · yok); tıklayınca o ay için portföyden çek seçimi
  function cizMatris() {
    const L = liste().filter(d => d.durum === 'Aktif'), n = F.matrisAy, t = $('#poMatris'); const ornek = L[0] ? cp(L[0]).aylar : [];
    const aylar = ornek.slice(0, n).map(a => ({ ay: a.ay, ayAdi: a.ayAdi }));
    const kol = [K('No', 52), K('Tedarikçi · Ürün / Hizmet', 170), K('Şirket', 66), K('Ort. Aylık (₺)', 80), ...aylar.map(a => K(a.ayAdi.slice(0, 3) + ' ' + a.ayAdi.slice(-4), 78, 'c')), K('Çek Planı Özeti', 150)];
    tabloKur(t, kol);
    const hucre = (d, i) => { const a = cp(d).aylar[i]; if (!a) return '<span class="po-m pasif">—</span>'; const cls = a.planlanan <= 0 ? 'yok' : a.tam ? 'tam' : 'kismi'; return `<span class="po-m ${cls}" data-dz="${d.id}" data-ay="${esc(a.ay)}" title="${esc(a.ayAdi)} · ödeme günü ${tarih(a.tarih)} · ihtiyaç ${para(a.ihtiyac)} ₺ · planlanan ${para(a.planlanan)} ₺ (${a.cekler.length} belge)${a.akis ? ' · nakit akış #' + a.akis.docEntry + ' ' + a.akis.durum : ''} — tıkla: çek seç">${a.planlanan > 0 ? kisa(a.planlanan) : '—'}<small>${a.planlanan > 0 ? pct(a.kapsama) + (a.cekler.length ? ' · ' + a.cekler.length + ' çek' : '') : 'çek yok'}</small></span>`; };
    t.tBodies[0].innerHTML = L.length ? L.map(d => `<tr data-dz="${d.id}"><td><b>${esc(d.no)}</b></td><td class="sol">${esc(d.tedarikci)}<small>${esc(d.urunHizmet)} · ${esc(d.odemeSekli)} · gün ${d.gun}</small></td><td class="sol">${esc(d.sirket)}</td><td class="para">${para(d.aylikTutar)}</td>${aylar.map((a, i) => `<td>${hucre(d, i)}</td>`).join('')}<td class="sol">${cekHucre(d)}</td></tr>`).join('') : `<tr><td colspan="${kol.length}" class="na-bos text-center">Aktif kayıt yok.</td></tr>`;
    const top = i => L.reduce((s, d) => { const a = cp(d).aylar[i]; return a ? { ih: s.ih + a.ihtiyac, pl: s.pl + a.planlanan, tam: s.tam + (a.tam ? 1 : 0) } : s; }, { ih: 0, pl: 0, tam: 0 });
    t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="3" class="text-end">TOPLAM · ${L.length} aktif kayıt · ihtiyaç / planlanan çek</td><td class="para">${para(L.reduce((s, d) => s + d.aylikTutar, 0))}</td>${aylar.map((a, i) => { const s = top(i); return `<td class="sol"><small>ihtiyaç ${kisa(s.ih)}</small><b>${kisa(s.pl)}</b><small>${pct(s.ih ? s.pl / s.ih : 0)} · ${s.tam} tam</small></td>`; }).join('')}<td class="sol">${kisa(L.reduce((s, d) => s + cp(d).toplamPlanlanan, 0))} ₺ · ${L.filter(d => cp(d).planliAy).length} kayıt</td></tr>` : '';
    $('#poMatrisNot').textContent = `yeşil tam karşılandı · sarı kısmi · kırmızı çek yok · hücreye tıklayınca o ay için portföyden çek seçilir · ${aylar.length ? aylar[0].ayAdi + ' – ' + aylar[aylar.length - 1].ayAdi : ''}`;
  }
  // ---------- bu ayın ödeme takvimi: gün bazında kartlar
  function cizTakvim() {
    const o = D().ozet || {}, L = liste(), bugun = (V && V.bugun) || '', gunBugun = +bugun.slice(8, 10);
    $('#poTakvimAy').textContent = o.ayAdi || '';
    const gunler = {}; L.forEach(d => { (gunler[d.gun] = gunler[d.gun] || []).push(d); });
    const sirali = Object.keys(gunler).map(Number).sort((a, b) => a - b);
    $('#poTakvim').innerHTML = sirali.length ? sirali.map(g => {
      const l = gunler[g].slice().sort((a, b) => b.aylikTutar - a.aylikTutar), toplam = l.filter(d => d.durum === 'Aktif').reduce((s, d) => s + d.aylikTutar, 0);
      const nokta = d => d.durum !== 'Aktif' ? '' : !d.buAy ? 'yok' : d.buAy.durum === 'ÖDENDİ' ? 'ok' : 'plan';
      const cekBuAy = d => { const a = cp(d).aylar[0]; return a && a.planlanan > 0 ? ` · çek ${kisa(a.planlanan)}` : ''; };
      return `<div class="po-gun ${g < gunBugun ? 'gecti' : ''} ${g === gunBugun ? 'bugun' : ''}"><div class="g"><b>${g}</b> ${esc((o.ayAdi || '').split(' ')[0])}<span class="t">${kisa(toplam)} ₺</span></div><ul>${l.map(d => `<li title="${esc(d.no + ' · ' + d.tedarikci + ' · ' + d.urunHizmet + ' · ' + d.sirket + ' · ' + para(d.aylikTutar) + ' ₺' + (d.buAy ? ' · nakit akış #' + d.buAy.docEntry + ' ' + d.buAy.durum : d.durum === 'Aktif' ? ' · bu aya akmadı' : ' · pasif') + cekBuAy(d))}"><i class="d ${nokta(d)}"></i><span class="ad">${esc(d.tedarikci)}<small>${esc(d.urunHizmet)} · ${esc(d.sirket)}${cekBuAy(d)}</small></span><b>${kisa(d.aylikTutar)}</b></li>`).join('')}</ul></div>`;
    }).join('') : '<div class="na-bos">Süzgece uyan kayıt yok.</div>';
  }
  function ciz() { if (!V) return; $('#poKapsam').textContent = `${F.sirket ? sirketAdi(F.sirket) : 'Tüm Şirketler (Konsolide)'} · ${(D().liste || []).length} kayıt`; cizOzet(); cizListe(); cizMatris(); cizTakvim(); }

  // ---------- çekmece: kayıt formu + ÇEK PLANI (ay tablosu, portföyden seçim) + akan aylar + günlük (panelle aynı + çek planı)
  function cekPlaniHtml(d) {
    const c = cp(d); if (!c.aylar || !c.aylar.length) return '';
    const kapBar = a => `<span class="po-kap"><i class="${a.tam ? '' : a.planlanan > 0 ? 'kismi' : 'yok'}" style="width:${Math.min(100, Math.round((a.kapsama || 0) * 100))}%"></i></span>${pct(a.kapsama)}`;
    const chip = (a, x) => `<span class="po-chip ${x.kullanim === 'Ciro' ? 'ciro' : ''} ${x.uygun ? '' : 'gec'}" title="${esc(x.no + ' · ' + x.musteri + ' · ' + x.banka + ' · vade ' + tarih(x.vade) + ' · ' + para(x.tutar) + ' ₺ · ' + (x.kullanim === 'Ciro' ? 'tedarikçiye ciro' : 'bankaya tahsil') + ' · Çek Akıbeti: ' + x.durum + (x.uygun ? '' : ' · DİKKAT vade ödeme gününden ' + (-x.gunFark) + ' gün sonra') + ' · ' + x.kullanici + ' ' + x.zaman)}">${esc(x.no)} · ${tarih(x.vade)} · ${kisa(x.tutar)}${x.uygun ? '' : ' ⚠'}<i class="fas fa-xmark po-cek-sil" data-ay="${esc(a.ay)}" data-cek="${x.cekDocEntry}" title="Plandan çıkar (portföye döner)"></i></span>`;
    return `<div class="na-kart" id="poCekKart"><div class="na-kart-b"><span><i class="fas fa-money-check-dollar me-1"></i>Çek Planı · Hangi Aya Kadar, Ne Kadar</span><span class="not">${c.ozet ? esc(c.ozet) : 'henüz çek planlanmadı'} · öneri: ${c.kullanimOneri === 'Ciro' ? 'tedarikçiye ciro' : 'bankaya tahsil (ödeme nakit / havale)'}${c.sonAkanAyAdi ? ' · nakit akış satırı → ' + esc(c.sonAkanAyAdi) : ''}</span></div>
      <div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo tp-fis" data-dinamik="kapali"><thead><tr><th>Ay</th><th>Ödeme Günü</th><th class="s">İhtiyaç (₺)</th><th>Nakit Akış</th><th>Planlanan Çek / Senet</th><th class="s">Planlanan (₺)</th><th>Karşılama</th><th></th></tr></thead><tbody>${c.aylar.map(a => `<tr class="${a.tam ? 'onayli' : a.planlanan > 0 ? 'bekliyor' : ''}"><td><b>${esc(a.ayAdi)}</b></td><td>${tarih(a.tarih)}</td><td class="para">${para(a.ihtiyac)}</td><td>${a.akis ? `<a class="tp-link" href="${esc(a.akis.link)}" target="_blank" rel="noopener">#${a.akis.docEntry}</a> ${durumRozet(a.akis.durum)}` : '<small>satır yok</small>'}</td><td style="white-space:normal">${a.cekler.length ? a.cekler.map(x => chip(a, x)).join('') : '<span class="text-muted">—</span>'}</td><td class="para">${a.planlanan ? para(a.planlanan) : '—'}</td><td>${kapBar(a)}</td><td><button type="button" class="btn btn-outline-primary btn-xs po-cek-sec" data-ay="${esc(a.ay)}" title="Portföyden bu ay için çek seç"><i class="fas fa-plus me-1"></i>Çek Seç</button></td></tr>`).join('')}</tbody></table></div><div id="poCekSec"></div>
      <div class="na-mini px-2 pb-2">Ödeme nakit / havale olsa bile (ör. maaş) o tarihe uygun vadeli müşteri çeki <b>bankaya tahsil</b> için planlanır; çek tahsil edilince ödeme bankadan yapılır. Ödeme şekli çek olan kayıtlarda çek <b>tedarikçiye ciro</b> için o ayın Kasa Nakit Akışı satırına bağlanır (satır yoksa açılır). Planlanan belge Çek Akıbeti'nde "Planlandı" olur; ✕ ile portföye döner.</div></div>`;
  }
  function dzFormAc(d, cekAy) {
    const DD = D(); acikId = d ? d.id : 0; CS = null;
    const sel = (id, liste, v, etiket) => `<select class="form-select form-select-sm" id="${id}" style="font-size:.72rem">${(liste || []).map(x => { const val = etiket ? x.kod : x, ad = etiket ? etiket(x) : x; return `<option value="${esc(val)}" ${val === v ? 'selected' : ''}>${esc(ad)}</option>`; }).join('')}</select>`;
    const inp = (id, v, tip, ph) => `<input class="form-control form-control-sm" id="${id}" type="${tip || 'text'}" value="${esc(v ?? '')}" placeholder="${esc(ph || '')}" style="font-size:.72rem" ${tip === 'number' ? 'min="0" step="any"' : ''}>`;
    $('#poDBaslik').innerHTML = d ? `${esc(d.no)} <span class="tp-pill">${esc(d.tedarikci)} · ${esc(d.sirket)}</span> ${d.durum === 'Aktif' ? '<span class="tp-onay onayli">Aktif</span>' : '<span class="tp-onay eksik">Pasif</span>'}${cp(d).planliAy ? ` <span class="tp-pill" title="çek planı">${esc(cp(d).ozet)}</span>` : ''}` : '<i class="fas fa-plus me-1"></i>Yeni Planlı Ödeme';
    $('#poDIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-calendar-check me-1"></i>${d ? 'Planlı Ödeme Kaydı' : 'Yeni Planlı Ödeme'}</span><span class="not">sorumlu ${esc(DD.sorumlu || '')} · her ay otomatik nakit akışa akar · bütçeye otomatik gelir</span></div><div class="na-kart-g"><div class="row g-2" style="font-size:.72rem">
        <div class="col-md-6"><label class="form-label mb-0">Tedarikçi</label>${inp('dz_tedarikci', d?.tedarikci, 'text', 'ör. CK Boğaziçi')}</div><div class="col-md-6"><label class="form-label mb-0">Ürün / Hizmet</label>${inp('dz_urun', d?.urunHizmet, 'text', 'ör. Elektrik Ödemesi')}</div>
        <div class="col-md-4"><label class="form-label mb-0">Şirket</label>${sel('dz_sirket', DD.sirketler, d?.sirket || (F.sirket ? sirketAdi(F.sirket) : (DD.sirketler || [])[0]))}</div><div class="col-md-4"><label class="form-label mb-0">Ödeme Şekli</label>${sel('dz_sekil', DD.odemeSekilleri, d?.odemeSekli || 'Çek')}</div><div class="col-md-4"><label class="form-label mb-0">Kategori</label>${sel('dz_kategori', DD.kategoriler, d?.kategori || 'Hizmet')}</div>
        <div class="col-md-3"><label class="form-label mb-0">Ödeme Günü (1–28)</label>${inp('dz_gun', d?.gun ?? 10, 'number')}</div><div class="col-md-4"><label class="form-label mb-0">Ort. Aylık Tutar (₺)</label>${inp('dz_tutar', d?.aylikTutar ?? '', 'number')}</div><div class="col-md-5"><label class="form-label mb-0">Bütçe Kalemi (Bütçe Raporu'na otomatik)</label>${sel('dz_kalem', DD.butceKalemleri, d?.butceKalem || '', k => `${k.kod} · ${k.ad}`)}</div>
        <div class="col-md-12"><label class="form-label mb-0">Not</label>${inp('dz_not', d?.not, 'text', 'isteğe bağlı')}</div>
        <div class="col-12 d-flex gap-2 justify-content-end flex-wrap">${d && d.durum === 'Aktif' && !d.buAy ? '<button type="button" class="btn btn-outline-success btn-sm" id="dz_akit"><i class="fas fa-play me-1"></i>Bu Aya Akıt</button>' : ''}${d ? `<button type="button" class="btn btn-light border btn-sm" id="dz_durum"><i class="fas ${d.durum === 'Aktif' ? 'fa-pause' : 'fa-circle-play'} me-1"></i>${d.durum === 'Aktif' ? 'Pasife Al' : 'Aktif Yap'}</button>` : ''}<button type="button" class="btn btn-success btn-sm" id="dz_kaydet"><i class="fas fa-save me-1"></i>${d ? 'Kaydet' : 'Ekle'}</button></div></div>
        <div class="na-mini mt-2">Değişiklik bir sonraki akışta geçerli olur; bu aya akmış satır Kasa Nakit Akışı'nda düzenlenir. Aynı kayıt Konsolide Finansal Takip Paneli › Düzenli Ödemeler'de de görünür; bütçe kalemi seçilmezse kategori / hizmete göre otomatik eşlenir.</div></div></div>
      ${d ? cekPlaniHtml(d) : ''}
      ${d ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-money-bill-transfer me-1"></i>Nakit Akışa Akan Aylar</span><span class="not">${d.akisSayisi} ay · toplam ${para(d.toplamAkan)} ₺</span></div><div class="na-kart-g p-0"><table class="na-tablo tp-fis" data-dinamik="kapali"><thead><tr><th>Ay</th><th>Kayıt</th><th>Yöntem</th><th>Planlanan</th><th>Durum</th><th class="s">Tutar (₺)</th><th>Makbuz · Fiş</th></tr></thead><tbody>${(d.akislar || []).map(a => `<tr><td>${esc(a.ay)}</td><td><a class="tp-link" href="${esc(a.link)}" target="_blank" rel="noopener">#${a.docEntry}</a></td><td>${esc(a.yontem)}</td><td>${tarih(a.planlanan)}</td><td>${durumRozet(a.durum)}${a.durum === 'ÖDENDİ' ? `<small>${tarih(a.odenme)}</small>` : ''}</td><td class="para">${para(a.tutar)}</td><td><small>${esc(a.makbuz || '')} ${esc(a.yevmiye || '')}</small></td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Henüz akmadı.</td></tr>'}</tbody></table></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Günlük</span></div><div class="na-kart-g"><ul class="na-zaman">${(d.gunluk || []).map(g => `<li class="${/Aktı|Kayıt|Çek Plan/.test(g.islem) ? 'fin' : /Pasif|Kaldır/.test(g.islem) ? 'wf' : 'sis'}"><span class="t">${esc(g.tarih)}</span><b>${esc(g.islem)}</b> · ${esc(g.aciklama)} <span class="k">${esc(g.kullanici)}</span></li>`).join('') || '<li>Kayıt yok.</li>'}</ul></div></div>` : ''}`;
    $('#poDrawer').classList.add('acik'); $('#poPerde').classList.add('acik'); if (!cekAy) $('#poDrawer').scrollTop = 0;
    $('#dz_kaydet').addEventListener('click', async () => {
      const r = await post('/PlanliOdemeler/Kaydet', { Id: d ? d.id : 0, Tedarikci: $('#dz_tedarikci').value, UrunHizmet: $('#dz_urun').value, Sirket: $('#dz_sirket').value, OdemeSekli: $('#dz_sekil').value, Kategori: $('#dz_kategori').value, Gun: +$('#dz_gun').value, AylikTutar: +$('#dz_tutar').value, Not: $('#dz_not').value, ButceKalem: $('#dz_kalem').value }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
      if (!r.success) { mesaj('danger', esc(r.message)); return; } mesaj('success', '<i class="fas fa-check me-1"></i>' + esc(r.message)); await yukle(true); drawerKapat();
    });
    const ak = $('#dz_akit'); if (ak) ak.addEventListener('click', () => akit(d));
    const du = $('#dz_durum'); if (du) du.addEventListener('click', () => durumDegistir(d));
    if (cekAy) cekSecAc(d, cekAy);
  }
  // portföyden çek seçimi (çekmece içinde): vade uygunluğu, arama, şirket, yalnız uygun; toplam ↔ ihtiyaç; kullanım (Tahsil / Ciro); Planla
  async function cekSecAc(d, ay) {
    const kap = $('#poCekSec'); if (!kap) return; kap.innerHTML = '<div class="po-cek-panel">Portföy çekleri yükleniyor…</div>';
    let r; try { r = await getJ(`/PlanliOdemeler/PortfoyCekler?id=${d.id}&ay=${encodeURIComponent(ay)}`); } catch (e) { r = { success: false, message: 'Ağ hatası.' }; }
    if (!r.success) { kap.innerHTML = `<div class="po-cek-panel text-danger">${esc(r.message)}</div>`; return; }
    CS = { id: d.id, ay, veri: r, secim: new Set(r.cekler.filter(c => c.secili).map(c => c.docEntry)), uygun: true, ara: '', sirket: '', kullanim: r.kullanimOneri };
    cekSecCiz(); kap.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  }
  function cekSecCiz() {
    const kap = $('#poCekSec'); if (!kap || !CS) return; const r = CS.veri, q2 = CS.ara.toLocaleLowerCase('tr-TR');
    const l = r.cekler.filter(c => c.secili || ((!CS.uygun || c.uygunluk === 'Uygun' || c.uygunluk === 'Sınırda') && (!CS.sirket || c.sirket === CS.sirket) && (!q2 || [c.no, c.musteri, c.banka, c.sirket].join(' ').toLocaleLowerCase('tr-TR').includes(q2))));
    const seciliYeni = r.cekler.filter(c => CS.secim.has(c.docEntry) && !c.secili), toplamYeni = seciliYeni.reduce((s, c) => s + c.tutar, 0), kalan = Math.max(0, r.ihtiyac - r.planlanan - toplamYeni);
    const uyg = u => `<span class="po-uyg ${u === 'Uygun' ? 'u' : u === 'Sınırda' ? 's' : u === 'Vade Geç' ? 'g' : 'p'}">${esc(u)}</span>`;
    const sirketler = [...new Set(r.cekler.map(c => c.sirket))].sort();
    kap.innerHTML = `<div class="po-cek-panel"><div class="ust"><b><i class="fas fa-money-check-dollar me-1"></i>${esc(r.ayAdi)} için çek seç</b><span>ödeme günü <b>${tarih(r.tarih)}</b></span><span>ihtiyaç <b>${para(r.ihtiyac)} ₺</b></span><span>planlı <b>${para(r.planlanan)} ₺</b></span><span>seçilen <b>${para(toplamYeni)} ₺</b></span><span class="${kalan > 0 ? 'text-danger' : 'text-success'}">kalan <b>${para(kalan)} ₺</b></span><span class="ms-auto">kullanım ${CS.kullanim === 'Ciro' ? '<span class="po-chip ciro">tedarikçiye ciro</span>' : '<span class="po-chip">bankaya tahsil · ödeme nakit / havale</span>'}</span></div>
      <div class="filtre"><label class="d-flex align-items-center gap-1" style="font-size:.66rem"><input type="checkbox" id="pcUygun" ${CS.uygun ? 'checked' : ''}> yalnız vadesi uygun / sınırda</label><select class="form-select" id="pcSirket"><option value="">Tüm Portföyler</option>${sirketler.map(s => `<option ${s === CS.sirket ? 'selected' : ''}>${esc(s)}</option>`).join('')}</select><input class="form-control" id="pcAra" placeholder="Çek no, müşteri, banka…" value="${esc(CS.ara)}"><select class="form-select" id="pcKullanim">${(r.kullanimlar || ['Tahsil', 'Ciro']).map(k => `<option value="${k}" ${k === CS.kullanim ? 'selected' : ''}>${k === 'Ciro' ? 'Ciro · tedarikçiye verilecek' : 'Tahsil · bankaya tahsil, ödeme nakit / havale'}</option>`).join('')}</select><button type="button" class="btn btn-light border btn-sm py-0" id="pcOto" style="font-size:.64rem" title="Vadesi ödeme gününe en yakın uygun çeklerden ihtiyacı karşılayacak kadar seç"><i class="fas fa-wand-magic-sparkles me-1"></i>Otomatik Seç</button><span class="text-muted" style="font-size:.62rem">${l.length} / ${r.cekler.length} belge</span></div>
      <div class="table-responsive" style="max-height:260px"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th></th><th>Belge</th><th>Portföy</th><th>Kimden Geldi</th><th>Banka</th><th>Vade</th><th class="s">Gün</th><th class="s">Tutar (₺)</th><th>Uygunluk</th></tr></thead><tbody>${l.length ? l.map(c => `<tr class="${CS.secim.has(c.docEntry) ? 'sec' : ''} ${c.secili ? 'kapali' : ''}" data-cek="${c.docEntry}"><td><input type="checkbox" class="pcSec" ${CS.secim.has(c.docEntry) ? 'checked' : ''} ${c.secili ? 'disabled' : ''}></td><td><b>${esc(c.no)}</b><small>${esc(c.belge)} · ${esc(c.islemTipi)}</small></td><td>${esc(c.sirket)}</td><td>${esc(c.musteri)}</td><td>${esc(c.banka)}</td><td>${esc(c.vadeTr)}</td><td class="s">${c.gunFark >= 0 ? c.gunFark + ' gün önce' : (-c.gunFark) + ' gün sonra'}</td><td class="s"><b>${para(c.tutar)}</b></td><td>${c.secili ? '<span class="po-uyg p">Bu Aya Planlı</span>' : uyg(c.uygunluk)}</td></tr>`).join('') : '<tr><td colspan="9" class="na-bos">Uygun portföy çeki yok (süzgeci gevşetin).</td></tr>'}</tbody></table></div>
      <div class="alt"><button type="button" class="btn btn-primary btn-sm" id="pcPlanla" ${seciliYeni.length ? '' : 'disabled'}><i class="fas fa-check me-1"></i>Seçilenleri Planla (${seciliYeni.length})</button><button type="button" class="btn btn-light border btn-sm" id="pcKapat">Kapat</button><span class="toplam">${para(toplamYeni)} ₺ seçildi · ${kalan > 0 ? 'eksik ' + para(kalan) + ' ₺' : 'ihtiyaç karşılandı'}</span></div></div>`;
    $('#pcUygun').addEventListener('change', e => { CS.uygun = e.target.checked; cekSecCiz(); }); $('#pcSirket').addEventListener('change', e => { CS.sirket = e.target.value; cekSecCiz(); }); $('#pcAra').addEventListener('input', e => { CS.ara = e.target.value; cekSecCiz(); $('#pcAra').focus(); }); $('#pcKullanim').addEventListener('change', e => { CS.kullanim = e.target.value; cekSecCiz(); });
    $$('#poCekSec .pcSec').forEach(cb => cb.addEventListener('change', () => { const id = +cb.closest('tr').dataset.cek; if (cb.checked) CS.secim.add(id); else CS.secim.delete(id); cekSecCiz(); }));
    // otomatik seçim: ihtiyaca en yakın — önce kalanı aşmayan en büyük uygun çekler (vadesi yakın), sonra kalanı kapatan en küçük çek
    $('#pcOto').addEventListener('click', () => {
      let kal = r.ihtiyac - r.planlanan; const uyg = r.cekler.filter(c => !c.secili && (c.uygunluk === 'Uygun' || c.uygunluk === 'Sınırda'));
      uyg.filter(c => c.tutar <= kal * 1.1).sort((a, b) => b.tutar - a.tutar || Math.abs(a.gunFark) - Math.abs(b.gunFark)).forEach(c => { if (kal > 0) { CS.secim.add(c.docEntry); kal -= c.tutar; } });
      if (kal > 0) { const k = uyg.filter(c => !CS.secim.has(c.docEntry) && c.tutar >= kal).sort((a, b) => a.tutar - b.tutar || Math.abs(a.gunFark) - Math.abs(b.gunFark))[0]; if (k) { CS.secim.add(k.docEntry); kal -= k.tutar; } }
      cekSecCiz();
    });
    $('#pcKapat').addEventListener('click', () => { CS = null; kap.innerHTML = ''; });
    $('#pcPlanla').addEventListener('click', async () => {
      const cekler = r.cekler.filter(c => CS.secim.has(c.docEntry) && !c.secili).map(c => c.docEntry); if (!cekler.length) return;
      const rr = await post('/PlanliOdemeler/CekPlanla', { Id: CS.id, Ay: CS.ay, Cekler: cekler, Kullanim: CS.kullanim }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
      mesaj(rr.success ? 'success' : 'danger', '<i class="fas fa-money-check-dollar me-1"></i>' + esc(rr.message)); if (rr.success) { const ay = CS.ay; await yukle(true); const d2 = dzBul(CS ? CS.id : acikId); if (d2) dzFormAc(d2, ay); }
    });
  }
  function drawerKapat() { $('#poDrawer').classList.remove('acik'); $('#poPerde').classList.remove('acik'); acikId = 0; CS = null; }
  async function akit(d) { const o = D().ozet || {}; const r = await post('/PlanliOdemeler/Akit', { Ay: o.ay, Id: d.id }).catch(() => ({ success: false, message: 'Ağ hatası.' })); mesaj(r.success ? 'success' : 'danger', esc(r.message)); if (r.success) { await yukle(true); drawerKapat(); } }
  async function durumDegistir(d) { const yeni = d.durum === 'Aktif' ? 'Pasif' : 'Aktif'; if (yeni === 'Pasif' && !confirm(`${d.no} ${d.tedarikci} pasife alınsın mı? Sonraki aylara akmaz; açılmış satırlar nakit akışta kalır.`)) return; const r = await post('/PlanliOdemeler/Durum', { Id: d.id, Durum: yeni }).catch(() => ({ success: false, message: 'Ağ hatası.' })); mesaj(r.success ? 'info' : 'danger', esc(r.message)); if (r.success) { await yukle(true); drawerKapat(); } }
  async function cekPlanKaldir(id, ay, cek) {
    if (!confirm('Belge bu ayın çek planından çıkarılsın mı? (portföye döner)')) return;
    const r = await post('/PlanliOdemeler/CekPlanKaldir', { Id: id, Ay: ay, CekDocEntry: cek }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    mesaj(r.success ? 'info' : 'danger', esc(r.message)); if (r.success) { await yukle(true); const d2 = dzBul(id); if (d2) dzFormAc(d2, null); }
  }

  // ---------- olaylar
  kok.addEventListener('click', async e => {
    const b = e.target.closest('#poAkit,#poSonraki,#poYeni,.tp-dz-akit,.tp-dz-duzenle,.tp-dz-durum,.tp-dz-cek');
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
      if (b.classList.contains('tp-dz-cek')) { const a = cp(d).aylar.find(x => x.planlanan <= 0) || cp(d).aylar[0]; dzFormAc(d, a ? a.ay : null); return; }
      if (b.classList.contains('tp-dz-akit')) { await akit(d); return; }
      if (b.classList.contains('tp-dz-durum')) { await durumDegistir(d); }
      return;
    }
    const m = e.target.closest('.po-m[data-dz]'); if (m) { const d = dzBul(+m.dataset.dz); if (d) dzFormAc(d, m.dataset.ay); return; }
    const sil = e.target.closest('.po-cek-sil'); if (sil) { e.stopPropagation(); await cekPlanKaldir(acikId, sil.dataset.ay, +sil.dataset.cek); return; }
    const sec = e.target.closest('.po-cek-sec'); if (sec) { const d = dzBul(acikId); if (d) cekSecAc(d, sec.dataset.ay); return; }
    const tr = e.target.closest('#poTablo tr.tik[data-dz]'); if (tr && !e.target.closest('a,button')) { const d = dzBul(+tr.dataset.dz); if (d) dzFormAc(d); }
  });
  $('#poDKapat').addEventListener('click', drawerKapat); $('#poPerde').addEventListener('click', drawerKapat);
  $('#poSirket').addEventListener('change', () => { F.sirket = $('#poSirket').value; yukle(); });
  $('#poDurum').addEventListener('change', () => { F.durum = $('#poDurum').value; ciz(); }); $('#poKategori').addEventListener('change', () => { F.kategori = $('#poKategori').value; ciz(); }); $('#poSekil').addEventListener('change', () => { F.sekil = $('#poSekil').value; ciz(); });
  $('#poAra').addEventListener('input', () => { F.ara = $('#poAra').value; ciz(); });
  $('#poMatrisAy').addEventListener('change', () => { F.matrisAy = +$('#poMatrisAy').value; cizMatris(); });
  $('#poExcel').addEventListener('click', () => csvIndir('planli_odemeler', ['No', 'Tedarikçi', 'Ürün / Hizmet', 'Şirket', 'Ödeme Şekli', 'Gün', 'Ort. Aylık (₺)', 'Kategori', 'Bütçe Kalemi', 'Geçen Ay', 'Bu Ay', 'Gelecek Ay', 'Çek Planı Son Ay', 'Çek Planı Ay Sayısı', 'Çek Planı Toplam (₺)', 'Tam Karşılanan Son Ay', 'Sorumlu', 'Durum', 'Not'],
    liste().map(d => [d.no, d.tedarikci, d.urunHizmet, d.sirket, d.odemeSekli, d.gun, d.aylikTutar, d.kategori, d.butceKalemAd, d.gecenAy ? `#${d.gecenAy.docEntry} ${d.gecenAy.durum}` : 'akmadı', d.buAy ? `#${d.buAy.docEntry} ${d.buAy.durum}` : 'akmadı', d.sonrakiAy ? `#${d.sonrakiAy.docEntry} ${d.sonrakiAy.durum}` : '', cp(d).sonPlanliAyAdi || '', cp(d).planliAy || 0, cp(d).toplamPlanlanan || 0, cp(d).tamSonAyAdi || '', d.sorumlu, d.durum, d.not])));
  // pencereye dönünce tazele (panel / Kasa Nakit Akışı / Çek Akıbeti'nde yapılan değişiklikler)
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
      const ds = $('#poDurum'); if (ds && !ds.querySelector('option[value="cekli"]')) ds.insertAdjacentHTML('beforeend', '<option value="cekli">Çek Planı Var</option><option value="ceksiz">Çek Planı Yok (Aktif)</option>');
      if (DD.otomatik) mesaj('info', `<i class="fas fa-rotate me-1"></i>${esc(DD.otomatikMesaj)} (ay başı otomatik akışı tamamlandı)`);
      const id = +kok.dataset.id; if (id) { const d = dzBul(id); if (d) dzFormAc(d); }
    }
    ciz();
  }
  yukle();
})();
