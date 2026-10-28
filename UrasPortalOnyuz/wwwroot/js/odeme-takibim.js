// ============================================================
// ÖDEME TAKİBİM — Kasa Nakit Akışı Görünüm (Views/Rapor130/Index.cshtml; veri /Rapor130/Veri?kisi=, satır /Rapor130/Satir)
//   Kişiye özel: yalnız kendi talep ettiği / satınalmacısı olduğu ödemeler. Süreç şeridi: Talep → Onay → Nakit Akışa Planlandı → Talimat / Çek / Nakit Onayı → Ödendi → Dekont · Makbuz.
//   Sekmeler: Bekleyen Ödemelerim · Ödenenler · Ödeme Taleplerim; çekmece: süreç zaman çizelgesi, günlük, ekler, çek / senet belgeleri (imzalı ibraz belgesi).
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('ot'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const para = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const tarih = s => { if (!s) return '—'; const p = String(s).slice(0, 10).split('-'); return p.length === 3 ? `${p[2]}.${p[1]}.${p[0]}` : s; };
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + new Date().toISOString().slice(0, 10) + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 10000); }
  const getJ = url => fetch(url).then(r => r.json());
  const rozet = (m, t) => `<span class="ot-rozet ${t}">${esc(m)}</span>`;
  const durumRozet = r => r.odendi ? rozet('ÖDENDİ', 'ok') : r.gecikti ? rozet('Gecikti', 'hata') : r.onay === 'Onay Bekliyor' ? rozet('Onay Bekliyor', 'bekle') : r.durum === 'Planlandı' ? rozet('Planlandı', 'bilgi') : rozet(r.durum || 'Bekliyor', 'notr');
  const surec = r => { let simdi = false; return `<div class="ot-surec">${r.adimlar.map((a, i) => { const cls = a.ok ? 'ok' : !simdi ? (simdi = true, r.gecikti && a.ad === 'Ödendi' ? 'gec' : 'simdi') : ''; return `${i ? '<i class="fas fa-chevron-right"></i>' : ''}<span class="${cls}" title="${esc(a.ad + ' · ' + (a.not || '') + (a.tarih ? ' · ' + tarih(a.tarih) : ''))}">${a.ok ? '✓ ' : ''}${esc(a.ad)}</span>`; }).join('')}</div>`; };

  let V = null, F = { kisi: kok.dataset.kisi || '', durum: '', sirket: '', yontem: '', ara: '' }, ilk = true;
  const q = () => (F.ara || '').toLocaleLowerCase('tr-TR');
  const uyar = r => (!F.sirket || r.sirket === F.sirket) && (!F.yontem || r.yontem === F.yontem) && (F.durum === '' || (F.durum === 'acik' ? !r.odendi : F.durum === 'gec' ? r.gecikti : F.durum === 'onay' ? r.onay === 'Onay Bekliyor' || r.asama === 'Onay' : r.odendi)) && (!q() || [r.kurum, r.urun, r.belgeNo, r.belgeTipi, r.not, r.talimat, r.cekler, r.talepNo, r.sirket].join(' ').toLocaleLowerCase('tr-TR').includes(q()));
  const liste = () => (V.satirlar || []).filter(uyar);

  function sekme(p) { $$('#otSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#ot > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); }
  function kpi() {
    const o = V.ozet || {};
    $('#otKpi').innerHTML = [
      { l: 'Bekleyen Ödemem', v: String(o.acik || 0), s: `${kisa(o.acikTutar)} ₺ · nakit akışta`, k: '#0369a1' },
      { l: 'Geciken', v: String(o.gecikmis || 0), s: `${kisa(o.gecikmisTutar)} ₺ · planlanan tarih geçti`, k: o.gecikmis ? '#dc2626' : '#15803d' },
      { l: 'Bu Hafta Planlı', v: String(o.buHafta || 0), s: `${kisa(o.buHaftaTutar)} ₺ · 7 gün içinde`, k: '#b45309' },
      { l: 'Onay Bekleyen', v: String(o.onayBekleyen || 0), s: 'nakit onayı / talep onayı', k: '#7c3aed' },
      { l: 'Ödenen · Son 30 Gün', v: String(o.odenen30 || 0), s: `${kisa(o.odenen30Tutar)} ₺ · toplam ${o.odenenToplam || 0} ödendi`, k: '#15803d' },
      { l: 'Ödeme Taleplerim', v: String(o.talep || 0), s: `${o.talepAcik || 0} açık (onay bekliyor / onaylandı)`, k: '#0f766e' }
    ].map(k => `<div style="--k:${k.k}"><div class="l">${esc(k.l)}</div><div class="v">${esc(k.v)}</div><div class="s">${esc(k.s)}</div></div>`).join('');
  }
  function ciz() {
    if (!V) return; const L = liste(), acik = L.filter(r => !r.odendi), odenen = L.filter(r => r.odendi);
    $('#otKim').innerHTML = `<i class="fas fa-user me-1"></i>${esc(V.secili)}${V.benimMi ? ' (ben)' : ''}`; $('#otNot').textContent = `${V.satirlar.length} ödeme · ${(V.talepler || []).length} talep · yalnız ${V.secili} ile ilgili kayıtlar`;
    kpi();
    $('#otTablo-acik tbody').innerHTML = acik.length ? acik.map(r => `<tr class="tik ${r.gecikti ? 'gec' : ''}" data-db="${esc(r.dbName)}" data-doc="${r.docEntry}"><td>${esc(r.sirket)}</td><td><b>${esc(r.kurum)}</b><small>${esc(r.urun || '')}</small></td><td>${esc(r.belgeTipi || '')}<small>${esc(r.belgeNo || '')}</small></td><td class="para">${para(r.tutarTL)} ₺${r.pb !== 'TL' ? `<small>${para(r.tutar)} ${esc(r.pb)}</small>` : ''}</td><td>${tarih(r.planlanan)}${r.sonOdeme && r.sonOdeme !== r.planlanan ? `<small>son ödeme ${tarih(r.sonOdeme)}</small>` : ''}</td><td>${esc(r.yontem || '')}${r.talimat ? `<small>${esc(r.talimat)}</small>` : r.cekler ? `<small>${esc(r.cekler)}</small>` : ''}</td><td>${surec(r)}</td><td><b>${esc(r.asama)}</b><small>${esc((r.adimlar.find(a => !a.ok) || {}).not || '')}</small></td><td>${durumRozet(r)}</td><td><small style="display:inline">${esc(r.rol)}</small></td></tr>`).join('') : '<tr><td colspan="10" class="na-bos">Bekleyen ödemeniz yok.</td></tr>';
    $('#otTablo-acik tfoot').innerHTML = acik.length ? `<tr><td colspan="3"><b>Toplam · ${acik.length} ödeme</b></td><td class="para">${para(acik.reduce((s, r) => s + r.tutarTL, 0))} ₺</td><td colspan="6"><small style="display:inline">geciken ${acik.filter(r => r.gecikti).length} · onay bekleyen ${acik.filter(r => r.asama === 'Onay' || r.onay === 'Onay Bekliyor').length}</small></td></tr>` : '';
    $('#otTablo-odendi tbody').innerHTML = odenen.length ? odenen.map(r => `<tr class="tik odendi" data-db="${esc(r.dbName)}" data-doc="${r.docEntry}"><td>${esc(r.sirket)}</td><td><b>${esc(r.kurum)}</b><small>${esc(r.urun || '')}</small></td><td>${esc(r.belgeTipi || '')}<small>${esc(r.belgeNo || '')}</small></td><td class="para">${para(r.tutarTL)} ₺</td><td>${tarih(r.odenme)}</td><td>${esc(r.yontem || '')}</td><td>${esc(r.talimat || r.cekler || '—')}${r.ibraz ? `<small>ibraz ${esc(r.ibraz)}</small>` : ''}</td><td>${esc(r.dekont || r.makbuz || '—')}</td><td>${r.ekAdet ? rozet(r.ekAdet + ' ek', 'ok') : rozet('ek yok', 'notr')}</td><td><small style="display:inline">${esc(r.rol)}</small></td></tr>`).join('') : '<tr><td colspan="10" class="na-bos">Ödenen kayıt yok.</td></tr>';
    $('#otTablo-odendi tfoot').innerHTML = odenen.length ? `<tr><td colspan="3"><b>Toplam · ${odenen.length} ödeme</b></td><td class="para">${para(odenen.reduce((s, r) => s + r.tutarTL, 0))} ₺</td><td colspan="6"></td></tr>` : '';
    const T = (V.talepler || []).filter(t => !q() || [t.no, t.kurum, t.belgeNo, t.aciklama, t.konu].join(' ').toLocaleLowerCase('tr-TR').includes(q()));
    $('#otTablo-talep tbody').innerHTML = T.length ? T.map(t => `<tr><td><b>${esc(t.no)}</b><small>${esc(t.departman)}</small></td><td>${tarih(t.tarih)}</td><td>${esc(t.sirket)}</td><td><b>${esc(t.kurum)}</b><small>${esc(t.konu || t.aciklama || '')}</small></td><td>${esc(t.belgeTipi)}<small>${esc(t.belgeNo || '')}</small></td><td class="para">${para(t.tutarTL)} ₺</td><td>${tarih(t.sonOdeme)}${t.gecikme > 0 && t.durum !== 'Nakit Akışa Alındı' ? `<small style="color:#b91c1c">${t.gecikme} gün geçti</small>` : ''}</td><td>${esc(t.yontem)}</td><td>${rozet(t.durum, t.durum === 'Onaylandı' ? 'bilgi' : t.durum === 'Nakit Akışa Alındı' ? 'ok' : t.durum === 'Reddedildi' ? 'hata' : 'bekle')}${t.onaylayan ? `<small>${esc(t.onaylayan)} ${tarih(t.onayTarihi)}</small>` : ''}</td><td>${t.nakitAkisDocEntry ? `<a href="#" class="ot-satir" data-db="${esc(t.dbName)}" data-doc="${t.nakitAkisDocEntry}">#${t.nakitAkisDocEntry} · süreci aç</a>` : '<span class="text-muted">henüz alınmadı</span>'}</td><td>${t.talepLink ? `<a class="btn btn-light border btn-sm py-0" style="font-size:.62rem" href="${esc(t.talepLink)}" target="_blank" rel="noopener">Talep</a>` : ''}</td></tr>`).join('') : '<tr><td colspan="11" class="na-bos">Ödeme talebiniz yok.</td></tr>';
    $('#otN-acik').textContent = acik.length; $('#otN-odendi').textContent = odenen.length; $('#otN-talep').textContent = T.length;
  }

  // ---------- çekmece: süreç zaman çizelgesi + günlük + ekler + belgeler
  async function drawerAc(db, doc) {
    let r; try { r = await getJ(`/Rapor130/Satir?dbName=${encodeURIComponent(db)}&docEntry=${doc}&kisi=${encodeURIComponent(V.secili)}`); } catch (e) { r = { success: false, message: 'Ağ hatası.' }; }
    if (!r.success) { mesaj('warning', esc(r.message)); return; }
    const s = r.satir; let simdi = false;
    $('#otDBaslik').innerHTML = `#${s.docEntry} · ${esc(s.kurum)} <span class="ot-pill">${esc(s.sirket)}</span> ${durumRozet(s)}`;
    $('#otDIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-route me-1"></i>Ödeme Süreci</span><span class="not">sıradaki adım: <b>${esc(s.asama)}</b></span></div><div class="na-kart-g"><ul class="ot-adimlar">${s.adimlar.map(a => { const cls = a.ok ? 'ok' : !simdi ? (simdi = true, s.gecikti && a.ad === 'Ödendi' ? 'gec' : 'simdi') : ''; return `<li class="${cls}"><b>${esc(a.ad)}</b>${a.tarih ? `<span class="t">${tarih(a.tarih)}</span>` : ''}<small>${esc(a.not || '')}</small></li>`; }).join('')}</ul></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-receipt me-1"></i>Ödeme Bilgileri</span><span class="not">${esc(s.rol)}</span></div><div class="na-kart-g"><div class="na-ustbilgi">${[['Şirket', s.sirket], ['Cari', s.kurum], ['Konu', s.urun], ['Belge', `${s.belgeTipi || ''} ${s.belgeNo || ''}`], ['Tutar', `${para(s.tutar)} ${s.pb} · ${para(s.tutarTL)} ₺`], ['Planlanan', tarih(s.planlanan)], ['Son Ödeme', tarih(s.sonOdeme)], ['Yöntem', s.yontem], ['Durum', s.durum + (s.onay ? ' · ' + s.onay : '')], ['Talimat / Çek', s.talimat || s.cekler || '—'], ['Ödenme', s.odenme ? `${tarih(s.odenme)} · ${para(s.odenen || s.tutar)} ${s.pb}` : '—'], ['Dekont · Makbuz · İbraz', [s.dekont, s.makbuz, s.ibraz].filter(Boolean).join(' · ') || '—'], ['Talep Eden · Satınalmacı', `${s.talepEden || '—'} · ${s.satinalmaci || '—'}`], ['Ödeme Talebi', s.talepNo ? `${s.talepNo} · ${s.talepDurum || ''}` : '—'], ['Not', s.not || '—']].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v ?? '—')}</div></div>`).join('')}</div>${s.talepLink ? `<a class="btn btn-light border btn-sm mt-2" style="font-size:.66rem" href="${esc(s.talepLink)}" target="_blank" rel="noopener"><i class="fas fa-file-signature me-1"></i>Ödeme Talebini Aç</a>` : ''}</div></div>
      ${r.belgeler && r.belgeler.length ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-money-check-alt me-1"></i>Çek / Senet</span><span class="not">belge akıbeti ve imzalı ibraz belgesi</span></div><div class="na-kart-g p-0"><table class="na-tablo" data-dinamik="kapali" style="font-size:.7rem"><thead><tr><th>Belge</th><th>Kimden Geldi</th><th>Banka</th><th>Vade</th><th class="s">Tutar</th><th>Akıbet</th><th>İbraz</th><th>İmzalı Belge</th></tr></thead><tbody>${r.belgeler.map(b => `<tr><td><b>${esc(b.no)}</b><small>${esc(b.belge)}</small></td><td>${esc(b.kimdenGeldi || '')}</td><td>${esc(b.banka || '')}</td><td>${tarih(b.vade)}</td><td class="para">${para(b.tutar)}</td><td>${esc(b.akibet || '')}</td><td>${esc(b.ibrazYeri || '—')}${b.ibrazBelgeNo ? `<small>${esc(b.ibrazBelgeNo)}</small>` : ''}</td><td>${b.ibrazBelgeYolu ? `<a href="${esc(b.ibrazBelgeYolu)}" target="_blank" rel="noopener"><i class="fas fa-file-signature me-1"></i>aç</a>` : '<span class="text-muted">—</span>'}</td></tr>`).join('')}</tbody></table></div></div>` : ''}
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-paperclip me-1"></i>Ekler · Dekont / Makbuz / Ciro Bordrosu</span><span class="not">${(r.ekler || []).length} ek</span></div><div class="na-kart-g">${(r.ekler || []).length ? `<ul class="mb-0 ps-3" style="font-size:.7rem">${r.ekler.map(e => `<li><a href="${esc(e.yol)}" target="_blank" rel="noopener"><i class="fas fa-file me-1"></i>${esc(e.ad)}</a> <span class="text-muted">· ${esc(e.tur)} · ${esc(e.tarih)} ${esc(e.yukleyen || '')}${e.aciklama ? ' · ' + esc(e.aciklama) : ''}</span></li>`).join('')}</ul>` : '<div class="na-bos">Henüz ek yok (dekont / makbuz ödendikçe Kasa Nakit Akışı\'ndan eklenir).</div>'}</div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Günlük</span></div><div class="na-kart-g"><ul class="na-zaman">${(r.gunluk || []).map(g => `<li class="${/Ödeme|Onay/.test(g.tip) ? 'fin' : /Talimat|Çek/.test(g.tip) ? 'muh' : 'sis'}"><span class="t">${esc(g.tarih)}</span><b>${esc(g.tip)}</b> · ${esc(g.aciklama)} <span class="k">${esc(g.kullanici)}</span></li>`).join('') || '<li>Kayıt yok.</li>'}</ul></div></div>`;
    $('#otDrawer').classList.add('acik'); $('#otPerde').classList.add('acik'); $('#otDrawer').scrollTop = 0;
  }
  function drawerKapat() { $('#otDrawer').classList.remove('acik'); $('#otPerde').classList.remove('acik'); }

  // ---------- olaylar
  kok.addEventListener('click', e => { const a = e.target.closest('.ot-satir'); if (a) { e.preventDefault(); drawerAc(a.dataset.db, +a.dataset.doc); return; } const tr = e.target.closest('tr.tik[data-doc]'); if (tr && !e.target.closest('a,button')) drawerAc(tr.dataset.db, +tr.dataset.doc); });
  $$('#otSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  $('#otDKapat').addEventListener('click', drawerKapat); $('#otPerde').addEventListener('click', drawerKapat);
  $('#otKisi').addEventListener('change', () => { F.kisi = $('#otKisi').value; try { const u = new URL(location.href); u.searchParams.set('kisi', F.kisi); history.replaceState(null, '', u); } catch (e) { } yukle(); });
  ['#otDurum', '#otSirket', '#otYontem'].forEach(s => $(s).addEventListener('change', () => { F.durum = $('#otDurum').value; F.sirket = $('#otSirket').value; F.yontem = $('#otYontem').value; ciz(); }));
  $('#otAra').addEventListener('input', () => { F.ara = $('#otAra').value; ciz(); });
  $('#otExcel').addEventListener('click', () => csvIndir('odeme_takibim', ['Şirket', 'Cari', 'Konu', 'Belge Tipi', 'Belge No', 'Tutar (TL)', 'Planlanan', 'Yöntem', 'Durum', 'Sıradaki Adım', 'Talimat / Çek', 'Ödenme', 'Dekont · Makbuz', 'Rolüm'], liste().map(r => [r.sirket, r.kurum, r.urun, r.belgeTipi, r.belgeNo, r.tutarTL, r.planlanan, r.yontem, r.durum, r.asama, r.talimat || r.cekler, r.odenme, r.dekont || r.makbuz, r.rol])));
  let sonYukleme = 0; document.addEventListener('visibilitychange', () => { if (!document.hidden && Date.now() - sonYukleme > 20000) yukle(true); });

  async function yukle(sessiz) {
    if (!sessiz) $('#otNot').textContent = 'yükleniyor…';
    let r; try { r = await getJ(`/Rapor130/Veri?kisi=${encodeURIComponent(F.kisi)}`); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r; sonYukleme = Date.now(); F.kisi = V.secili;
    if (ilk) {
      ilk = false;
      $('#otKisi').innerHTML = V.kisiler.map(k => `<option value="${esc(k.ad)}">${esc(k.ad)} · ${esc(k.rol)} (${k.adet})${k.ad === V.ben ? ' — ben' : ''}</option>`).join(''); $('#otKisi').value = V.secili;
      $('#otSirket').innerHTML = '<option value="">Tümü</option>' + [...new Set(V.satirlar.map(r => r.sirket))].sort().map(s => `<option>${esc(s)}</option>`).join('');
      $('#otYontem').innerHTML = '<option value="">Tümü</option>' + [...new Set(V.satirlar.map(r => r.yontem).filter(Boolean))].sort().map(s => `<option>${esc(s)}</option>`).join('');
    } else if ($('#otKisi').value !== V.secili) { $('#otKisi').value = V.secili; $('#otSirket').innerHTML = '<option value="">Tümü</option>' + [...new Set(V.satirlar.map(r => r.sirket))].sort().map(s => `<option>${esc(s)}</option>`).join(''); }
    ciz();
  }
  yukle();
})();
