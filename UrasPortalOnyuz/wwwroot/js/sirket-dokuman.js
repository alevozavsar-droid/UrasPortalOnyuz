// ============================================================
// ŞİRKET DÖKÜMANLARI (Views/SirketDokuman/Index.cshtml; veri /SirketDokuman/Veri, belge /SirketDokuman/Belge, kayıt Kaydet (multipart) / Surum (multipart) / Yenile / Arsivle, dosya /SirketDokuman/Dosya?id=&surum=, şirket dosyası /SirketDokuman/SirketPdf?kod=)
//   Belgeler (şirket bazında liste) · Şirket × Kategori matrisi · Yenileme Takvimi · Eksik Belgeler · Şirketler (künye) · Yeni Belge · Günlük; çekmece: bilgiler, yenile, sürümler (aç / yükle), arşiv, günlük.
//   Kategori kırılımı: data-kategori (menüden /SirketDokuman/Kurulus …) ön süzgeç olarak gelir; üstteki kırılım pilleri diğer kategorilere geçirir.
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('sd'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const tarih = s => { if (!s) return '—'; const p = String(s).slice(0, 10).split('-'); return p.length === 3 ? `${p[2]}.${p[1]}.${p[0]}` : s; };
  const kb = v => v == null ? '' : v >= 1048576 ? (v / 1048576).toFixed(1) + ' MB' : Math.round(v / 1024) + ' KB';
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 12000); }
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }).then(r => r.json());
  const postForm = (url, fd) => fetch(url, { method: 'POST', body: fd }).then(r => r.json());
  const getJ = url => fetch(url).then(r => r.json());
  const rozet = (m, t) => `<span class="sd-rozet ${t}">${esc(m)}</span>`;
  const AYLAR = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];

  let V = null, F = { sirket: kok.dataset.sirket || '', kategori: kok.dataset.kategori || '', tur: '', durum: '', sorumlu: '', ara: '' }, A = null, ilk = true;
  const sirketBul = k => (V.sirketler || []).find(s => s.kod === k) || {};
  const kategoriBul = k => (V.kategoriler || []).find(x => x.kod === k) || {};
  const turBul = k => (V.turler || []).find(x => x.kod === k) || {};
  const sirketRozet = b => `<span class="sd-sirket" style="background:${esc(b.sirketRenk || sirketBul(b.sirketKod).renk || '#64748b')}">${esc(b.sirket || sirketBul(b.sirketKod).ad)}</span>`;
  const durumRozet = d => d === 'Geçerli' ? rozet('Geçerli', 'ok') : d === 'Süresiz' ? rozet('Süresiz', 'bilgi') : d === 'Süresi Yaklaşıyor' ? rozet('Süresi Yaklaşıyor', 'uyar') : d === 'Süresi Doldu' ? rozet('Süresi Doldu', 'hata') : d === 'Arşiv' ? rozet('Arşiv', 'notr') : d === 'EKSİK' ? rozet('Eksik', 'hata') : rozet(d, 'notr');
  const kalanMetni = b => b.kalan == null ? 'süresiz' : b.kalan < 0 ? `${-b.kalan} gün önce doldu` : b.kalan === 0 ? 'bugün doluyor' : `${b.kalan} gün kaldı`;
  const q = () => (F.ara || '').toLocaleLowerCase('tr-TR');
  const uyar = b => (!F.sirket || b.sirketKod === F.sirket) && (!F.kategori || b.kategori === F.kategori) && (!F.tur || b.turKod === F.tur) && (!F.sorumlu || b.sorumlu === F.sorumlu) && durumUyar(b) && (!q() || [b.no, b.ad, b.belgeNo, b.kurum, b.sirket, b.sorumlu, b.not, b.kategoriAd].join(' ').toLocaleLowerCase('tr-TR').includes(q()));
  const durumUyar = b => { const d = F.durum; if (d === '') return !b.arsiv; if (d === 'Arşiv') return b.arsiv; if (d === 'zorunlu') return b.zorunlu && !b.arsiv; return b.durum === d; };
  const liste = () => (V.belgeler || []).filter(uyar);
  const kategoriUrl = k => k ? '/SirketDokuman/' + (kategoriBul(k).action || '') : '/SirketDokuman';

  function sekme(p) { $$('#sdSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#sd > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); }

  // ---------- kırılım pilleri, kpi
  function cizKirilim() {
    const a = (V.belgeler || []).filter(b => !b.arsiv);
    $('#sdKirilim').innerHTML = `<a class="sd-pill ${F.kategori === '' ? 'sec' : ''}" href="/SirketDokuman"><i class="fas fa-layer-group me-1"></i>Tümü <b>${a.length}</b></a>` + V.kategoriler.map(k => { const n = a.filter(b => b.kategori === k.kod).length, u = a.filter(b => b.kategori === k.kod && b.durum === 'Süresi Yaklaşıyor').length, h = a.filter(b => b.kategori === k.kod && b.durum === 'Süresi Doldu').length + (V.eksikler || []).filter(e => e.kategori === k.kod).length; return `<a class="sd-pill ${F.kategori === k.kod ? 'sec' : ''}" href="${esc(k.url)}" title="${esc(k.aciklama)}"><i class="fas ${esc(k.ikon)} me-1"></i>${esc(k.ad)} <b>${n}</b>${h ? ` <span class="text-danger">●${h}</span>` : ''}${u ? ` <span style="color:#b45309">●${u}</span>` : ''}</a>`; }).join('');
    $('#sdKapsam').textContent = `${V.sirketler.length} şirket · ${V.kategoriler.length} kırılım · ${V.turler.length} belge türü`;
  }
  function cizKpi() {
    const L = (V.belgeler || []).filter(b => (!F.kategori || b.kategori === F.kategori) && (!F.sirket || b.sirketKod === F.sirket)); const a = L.filter(b => !b.arsiv); const eksik = (V.eksikler || []).filter(e => (!F.kategori || e.kategori === F.kategori) && (!F.sirket || e.sirketKod === F.sirket));
    const k = [
      { k: '', l: 'Toplam Belge', v: String(a.length), s: `${new Set(a.map(b => b.sirketKod)).size} şirket · ${new Set(a.map(b => b.turKod)).size} tür`, r: '#4338ca' },
      { k: 'Geçerli', l: 'Geçerli', v: String(a.filter(b => b.durum === 'Geçerli').length), s: `${a.filter(b => b.durum === 'Süresiz').length} süresiz belge ayrıca`, r: '#15803d' },
      { k: 'Süresi Yaklaşıyor', l: 'Süresi Yaklaşıyor', v: String(a.filter(b => b.durum === 'Süresi Yaklaşıyor').length), s: `${V.yaklasanGun} gün içinde dolacak`, r: a.some(b => b.durum === 'Süresi Yaklaşıyor') ? '#b45309' : '#15803d' },
      { k: 'Süresi Doldu', l: 'Süresi Doldu', v: String(a.filter(b => b.durum === 'Süresi Doldu').length), s: 'yenilenmeli', r: a.some(b => b.durum === 'Süresi Doldu') ? '#dc2626' : '#15803d' },
      { k: 'eksik', l: 'Eksik Belge', v: String(eksik.length), s: 'zorunlu olup bulunmayan', r: eksik.length ? '#dc2626' : '#15803d' },
      { k: 'zorunlu', l: 'Zorunlu Belgeler', v: String(a.filter(b => b.zorunlu).length), s: 'her şirkette bulunmalı', r: '#0369a1' },
      { k: 'Arşiv', l: 'Arşiv', v: String(L.filter(b => b.arsiv).length), s: 'eski / geçersiz belgeler', r: '#94a3b8' }
    ];
    $('#sdKpi').innerHTML = k.map(x => `<div style="--k:${x.r}" data-durum="${x.k}" class="${F.durum === x.k && x.k ? 'aktif' : ''}" title="Tıklayınca liste süzülür"><div class="l">${esc(x.l)}</div><div class="v">${esc(x.v)}</div><div class="s">${esc(x.s)}</div></div>`).join('');
  }

  // ---------- belge listesi (şirket bazında)
  function cizTablo() {
    const L = liste();
    $('#sdTablo thead').innerHTML = '<tr><th style="width:9%">Şirket</th><th style="width:11%">Kategori</th><th style="width:20%">Belge · Belge No</th><th style="width:12%">Düzenleyen Kurum</th><th style="width:7%">Düzenleme</th><th style="width:10%">Geçerlilik</th><th style="width:8%">Durum</th><th style="width:10%">Sorumlu</th><th style="width:9%">Sürüm · Dosya</th><th style="width:4%"></th></tr>';
    const gruplar = []; L.forEach(b => { let g = gruplar.find(x => x.k === b.sirketKod); if (!g) { g = { k: b.sirketKod, ad: b.sirket, l: [] }; gruplar.push(g); } g.l.push(b); });
    $('#sdTablo tbody').innerHTML = L.length ? gruplar.map(g => g.l.map(b => `<tr class="tik ${b.arsiv ? 'arsiv' : b.durum === 'Süresi Doldu' ? 'dolan' : b.durum === 'Süresi Yaklaşıyor' ? 'yaklasan' : ''}" data-id="${b.id}"><td class="sol">${sirketRozet(b)}</td><td class="sol" title="${esc(b.kategoriAd)}">${esc(b.kategoriAd)}</td><td class="sol" title="${esc(b.ad)}"><b>${esc(b.ad)}</b> ${b.zorunlu ? '<span class="sd-zorunlu">ZORUNLU</span>' : ''}<small>${esc(b.belgeNo || '—')} · ${esc(b.no)}${b.gizlilik !== 'Genel' ? ' · ' + esc(b.gizlilik) : ''}</small></td><td class="sol" title="${esc(b.kurum)}">${esc(b.kurum)}</td><td>${tarih(b.duzenleme)}</td><td class="sol"><b>${b.bitis ? tarih(b.bitis) : 'Süresiz'}</b><small class="${b.kalan != null && b.kalan < 0 ? 'text-danger fw-bold' : ''}">${esc(kalanMetni(b))}${b.sureliAy ? ' · ' + b.sureliAy + ' ay' : ''}</small></td><td>${durumRozet(b.durum)}</td><td class="sol" title="${esc(b.sorumlu)}">${esc(b.sorumlu)}</td><td class="sol">${b.surum ? `<a href="/SirketDokuman/Dosya?id=${b.id}" target="_blank" rel="noopener" onclick="event.stopPropagation()" title="${esc(b.dosya)}"><i class="fas fa-file-pdf me-1"></i>v${b.surum}</a><small>${esc(b.dosya)} · ${kb(b.boyut)}${b.dosyaVar ? '' : ' · kapak'}</small>` : '<small>dosya yok</small>'}</td><td><button type="button" class="btn btn-light border btn-xs sd-ac"><i class="fas fa-folder-open"></i></button></td></tr>`).join('') + `<tr class="ara"><td colspan="2" class="text-end">${esc(g.ad)} · ${g.l.length} belge</td><td colspan="8" class="sol"><small style="display:inline">geçerli ${g.l.filter(b => b.durum === 'Geçerli' || b.durum === 'Süresiz').length} · yaklaşan ${g.l.filter(b => b.durum === 'Süresi Yaklaşıyor').length} · dolan ${g.l.filter(b => b.durum === 'Süresi Doldu').length} · eksik ${(V.eksikler || []).filter(e => e.sirketKod === g.k && (!F.kategori || e.kategori === F.kategori)).length}</small></td></tr>`).join('') : '<tr><td colspan="10" class="na-bos text-center">Süzgece uyan belge yok.</td></tr>';
    $('#sdTablo tfoot').innerHTML = L.length ? `<tr class="toplam"><td colspan="2" class="text-end">TOPLAM · ${L.length} belge · ${gruplar.length} şirket</td><td colspan="8" class="sol">geçerli ${L.filter(b => b.durum === 'Geçerli' || b.durum === 'Süresiz').length} · süresi yaklaşan ${L.filter(b => b.durum === 'Süresi Yaklaşıyor').length} · süresi dolan ${L.filter(b => b.durum === 'Süresi Doldu').length} · zorunlu ${L.filter(b => b.zorunlu).length}</td></tr>` : '';
    $('#sdSayi').textContent = `${L.length} / ${(V.belgeler || []).filter(b => !b.arsiv).length} belge · sarı 60 gün içinde dolacak · kırmızı süresi dolmuş · satıra tıklayınca belge, sürümler ve yenileme açılır`;
    $('#sdN-belgeler').textContent = L.length; cizKpi();
  }
  // ---------- matris
  function cizMatris() {
    const M = (V.matris || []).filter(m => !F.sirket || m.kod === F.sirket); const K = V.kategoriler;
    $('#sdMatris thead').innerHTML = '<tr><th class="sol" style="width:12%">Şirket</th>' + K.map(k => `<th title="${esc(k.ad)}"><i class="fas ${esc(k.ikon)}"></i><br>${esc(k.ad.split(' ')[0])}</th>`).join('') + '<th>Toplam</th><th>Yaklaşan</th><th>Dolan</th><th>Eksik</th></tr>';
    const hucre = (m, c) => { const cls = c.eksik > 0 || c.dolan > 0 ? 'hata' : c.yaklasan > 0 ? 'uyar' : c.toplam > 0 ? 'ok' : 'bos'; return `<span class="sd-h ${cls}" data-s="${esc(m.kod)}" data-k="${esc(c.kod)}" title="${esc(m.ad)} · ${esc(kategoriBul(c.kod).ad)} · ${c.toplam} belge / ${c.beklenen} tür · yaklaşan ${c.yaklasan} · dolan ${c.dolan} · eksik ${c.eksik}">${c.toplam}/${c.beklenen}<small>${c.eksik ? 'eksik ' + c.eksik : c.dolan ? 'dolan ' + c.dolan : c.yaklasan ? 'yakl. ' + c.yaklasan : c.toplam ? 'tam' : '—'}</small></span>`; };
    $('#sdMatris tbody').innerHTML = M.map(m => `<tr><td class="sol"><span class="sd-sirket" style="background:${esc(m.renk)}">${esc(m.ad)}</span></td>${m.kategoriler.map(c => `<td>${hucre(m, c)}</td>`).join('')}<td class="para">${m.toplam}</td><td class="para ${m.yaklasan ? 'text-warning' : ''}">${m.yaklasan}</td><td class="para ${m.dolan ? 'text-danger' : ''}">${m.dolan}</td><td class="para ${m.eksik ? 'text-danger' : ''}">${m.eksik}</td></tr>`).join('');
    $('#sdMatris tfoot').innerHTML = `<tr class="toplam"><td class="sol">TOPLAM</td>${K.map(k => `<td>${M.reduce((a, m) => a + (m.kategoriler.find(c => c.kod === k.kod) || {}).toplam, 0)}</td>`).join('')}<td class="para">${M.reduce((a, m) => a + m.toplam, 0)}</td><td class="para">${M.reduce((a, m) => a + m.yaklasan, 0)}</td><td class="para">${M.reduce((a, m) => a + m.dolan, 0)}</td><td class="para">${M.reduce((a, m) => a + m.eksik, 0)}</td></tr>`;
  }
  // ---------- yenileme takvimi
  function cizTakvim() {
    const n = +$('#sdTakvimAy').value || 6; const bugun = new Date(V.bugun + 'T00:00:00'); const son = new Date(bugun); son.setMonth(son.getMonth() + n);
    const a = (V.belgeler || []).filter(b => !b.arsiv && b.bitis && (!F.sirket || b.sirketKod === F.sirket) && (!F.kategori || b.kategori === F.kategori));
    const dolan = a.filter(b => b.kalan < 0).sort((x, y) => x.bitis.localeCompare(y.bitis)); const aylar = [];
    for (let i = 0; i < n; i++) { const d = new Date(bugun.getFullYear(), bugun.getMonth() + i, 1); const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`; aylar.push({ key, ad: `${AYLAR[d.getMonth()]} ${d.getFullYear()}`, l: a.filter(b => b.kalan >= 0 && b.bitis.slice(0, 7) === key).sort((x, y) => x.bitis.localeCompare(y.bitis)) }); }
    const kart = (baslik, l, cls) => `<div class="sd-ay ${cls}"><div class="b"><span>${esc(baslik)}</span><span>${l.length}</span></div><ul>${l.length ? l.map(b => `<li data-id="${b.id}">${sirketRozet(b)} <b>${esc(b.ad)}</b><small>${tarih(b.bitis)} · ${esc(kalanMetni(b))} · ${esc(b.sorumlu)}</small></li>`).join('') : '<li class="text-muted" style="cursor:default">belge yok</li>'}</ul></div>`;
    $('#sdTakvim').innerHTML = kart('Süresi Dolmuş · Hemen Yenile', dolan, 'gecmis') + aylar.map(m => kart(m.ad, m.l, '')).join('');
    $('#sdN-takvim').textContent = dolan.length + a.filter(b => b.durum === 'Süresi Yaklaşıyor').length;
  }
  // ---------- eksik belgeler
  function cizEksik() {
    const E = (V.eksikler || []).filter(e => (!F.sirket || e.sirketKod === F.sirket) && (!F.kategori || e.kategori === F.kategori));
    $('#sdEksikTablo thead').innerHTML = '<tr><th style="width:12%">Şirket</th><th style="width:16%">Kategori</th><th style="width:26%">Belge Türü (Zorunlu)</th><th style="width:16%">Düzenleyen Kurum</th><th style="width:8%">Süre</th><th style="width:14%">Sorumlu</th><th style="width:8%"></th></tr>';
    $('#sdEksikTablo tbody').innerHTML = E.length ? E.map(e => `<tr class="dolan"><td class="sol"><span class="sd-sirket" style="background:${esc(e.sirketRenk)}">${esc(e.sirket)}</span></td><td class="sol">${esc(e.kategoriAd)}</td><td class="sol"><b>${esc(e.tur)}</b> <span class="sd-zorunlu">ZORUNLU</span></td><td class="sol">${esc(e.kurum)}</td><td>${e.sureliAy ? e.sureliAy + ' ay' : 'süresiz'}</td><td class="sol">${esc(e.sorumlu)}</td><td><button type="button" class="btn btn-primary btn-xs sd-eksik-ekle" data-s="${esc(e.sirketKod)}" data-t="${esc(e.turKod)}"><i class="fas fa-plus me-1"></i>Ekle</button></td></tr>`).join('') : '<tr><td colspan="7" class="na-bos text-center">Eksik zorunlu belge yok.</td></tr>';
    $('#sdN-eksik').textContent = E.length;
  }
  // ---------- şirket kartları
  function cizSirketler() {
    $('#sdSirketler').innerHTML = V.sirketler.map(s => { const m = (V.matris || []).find(x => x.kod === s.kod) || {}; return `<div class="sd-kart" style="--r:${esc(s.renk)}"><h5>${esc(s.ad)}</h5><div class="u">${esc(s.unvan)}<br>${esc(s.adres)} · kuruluş ${s.kurulusYili} · ${esc(s.faaliyet)}</div><div class="k"><span>VKN <b>${esc(s.vergiNo)}</b> · ${esc(s.vergiDairesi)}</span><span>Ticaret Sicil <b>${esc(s.sicilNo)}</b></span><span>MERSİS <b>${esc(s.mersis)}</b></span><span>Yetkili <b>${esc(s.yetkili)}</b></span></div><div class="s">${rozet((m.toplam || 0) + ' belge', 'bilgi')}${m.yaklasan ? rozet(m.yaklasan + ' yaklaşan', 'uyar') : ''}${m.dolan ? rozet(m.dolan + ' dolan', 'hata') : ''}${m.eksik ? rozet(m.eksik + ' eksik', 'hata') : rozet('eksik yok', 'ok')}</div><div class="btns"><button type="button" class="btn btn-light border btn-xs sd-sirket-liste" data-s="${esc(s.kod)}"><i class="fas fa-list me-1"></i>Belgeleri</button><button type="button" class="btn btn-light border btn-xs sd-sirket-eksik" data-s="${esc(s.kod)}"><i class="fas fa-triangle-exclamation me-1"></i>Eksikler</button><a class="btn btn-outline-primary btn-xs" href="/SirketDokuman/SirketPdf?kod=${esc(s.kod)}" target="_blank" rel="noopener"><i class="fas fa-file-pdf me-1"></i>Şirket Dosyası PDF</a></div></div>`; }).join('');
  }
  function cizGunluk() {
    const G = (V.gunluk || []).filter(g => !F.sirket || g.sirket === sirketBul(F.sirket).ad);
    $('#sdGunlukTablo thead').innerHTML = '<tr><th style="width:11%">Zaman</th><th style="width:12%">Kullanıcı</th><th style="width:12%">Şirket</th><th style="width:22%">Belge</th><th style="width:12%">İşlem</th><th style="width:31%">Açıklama</th></tr>';
    $('#sdGunlukTablo tbody').innerHTML = G.length ? G.map(g => `<tr class="tik" data-id="${g.belgeId}"><td class="sol">${esc(g.zaman)}</td><td class="sol">${esc(g.kullanici)}</td><td class="sol">${esc(g.sirket)}</td><td class="sol"><b>${esc(g.no)}</b> ${esc(g.ad)}</td><td class="sol">${esc(g.islem)}</td><td class="sol" title="${esc(g.aciklama)}">${esc(g.aciklama)}</td></tr>`).join('') : '<tr><td colspan="6" class="na-bos text-center">Kayıt yok.</td></tr>';
  }

  // ---------- çekmece
  async function drawerAc(id) {
    let r; try { r = await getJ('/SirketDokuman/Belge?id=' + id); } catch (e) { r = { success: false, message: 'Ağ hatası.' }; }
    if (!r.success) { mesaj('warning', esc(r.message)); return; }
    A = r.belge; cizDrawer(); $('#sdDrawer').classList.add('acik'); $('#sdPerde').classList.add('acik'); $('#sdDrawer').scrollTop = 0;
  }
  function drawerKapat() { $('#sdDrawer').classList.remove('acik'); $('#sdPerde').classList.remove('acik'); A = null; }
  function cizDrawer() {
    const b = A; const bugun = V.bugun; const oneri = (() => { if (!b.sureliAy) return ''; const d = new Date(bugun + 'T00:00:00'); d.setMonth(d.getMonth() + b.sureliAy); return d.toISOString().slice(0, 10); })();
    $('#sdDBaslik').innerHTML = `${sirketRozet(b)} ${esc(b.ad)} ${durumRozet(b.durum)} ${b.zorunlu ? '<span class="sd-zorunlu">ZORUNLU</span>' : ''} <span class="text-muted" style="font-size:.66rem">${esc(b.no)}</span>`;
    $('#sdDIc').innerHTML = `
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-file-lines me-1"></i>Belge Bilgileri</span><span class="not">${esc(b.kategoriAd)} · ${esc(b.tur)}</span></div><div class="na-kart-g"><div class="na-ustbilgi">${[['Şirket', `${b.sirket} (${b.sirketKod})`], ['Belge No', b.belgeNo || '—'], ['Düzenleyen Kurum', b.kurum], ['Düzenleme', tarih(b.duzenleme)], ['Geçerlilik Bitişi', b.bitis ? `${tarih(b.bitis)} · ${kalanMetni(b)}` : 'Süresiz'], ['Durum', b.durum], ['Sorumlu', b.sorumlu], ['Gizlilik', b.gizlilik], ['Süre', b.sureliAy ? b.sureliAy + ' ay (tür tanımı)' : 'süresiz tür'], ['Zorunlu', b.zorunlu ? 'Evet · her şirkette bulunmalı' : 'Hayır'], ['Son Sürüm', b.surum ? `v${b.surum} · ${b.yukleme} · ${b.yukleyen}` : '—'], ['Not', b.not || '—']].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('')}</div>
        <div class="d-flex gap-1 flex-wrap mt-2"><a class="btn btn-primary btn-sm" style="font-size:.68rem" href="/SirketDokuman/Dosya?id=${b.id}" target="_blank" rel="noopener"><i class="fas fa-file-pdf me-1"></i>Belgeyi Aç${b.dosyaVar ? '' : ' (Kapak)'}</a><button type="button" class="btn btn-light border btn-sm" style="font-size:.68rem" id="dDuzenle"><i class="fas fa-pen me-1"></i>Bilgileri Düzenle</button><a class="btn btn-light border btn-sm" style="font-size:.68rem" href="/SirketDokuman/SirketPdf?kod=${esc(b.sirketKod)}" target="_blank" rel="noopener"><i class="fas fa-building me-1"></i>Şirket Dosyası PDF</a><button type="button" class="btn ${b.arsiv ? 'btn-outline-success' : 'btn-outline-danger'} btn-sm ms-auto" style="font-size:.68rem" id="dArsiv"><i class="fas ${b.arsiv ? 'fa-box-open' : 'fa-box-archive'} me-1"></i>${b.arsiv ? 'Arşivden Çıkar' : 'Arşivle'}</button></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-rotate me-1"></i>Yenile · Yeni Geçerlilik</span><span class="not">${b.sureliAy ? `tür süresi ${b.sureliAy} ay · öneri ${tarih(oneri)}` : 'süresiz tür; gerekirse bitiş tarihi girin'}</span></div><div class="na-kart-g"><div class="sd-yenile"><div><label>Yeni Belge No</label><input class="form-control" id="ynlNo" value="${esc(b.belgeNo)}"></div><div><label>Düzenleme</label><input type="date" class="form-control" id="ynlDuz" value="${bugun}"></div><div><label>Geçerlilik Bitişi</label><input type="date" class="form-control" id="ynlBit" value="${oneri}"></div><div><label>Not</label><input class="form-control" id="ynlNot" placeholder="yenileme notu"></div><div><label>&nbsp;</label><button type="button" class="btn btn-primary btn-sm py-0" id="ynlKaydet" style="font-size:.68rem;height:28px"><i class="fas fa-rotate me-1"></i>Yenile</button></div></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-layer-group me-1"></i>Sürümler · Dosyalar</span><span class="not">${b.surumler.length} sürüm · en yeni üstte</span></div><div class="na-kart-g p-0"><table class="na-tablo sd-tablo" data-dinamik="kapali"><thead><tr><th>Sürüm</th><th>Tarih</th><th>Yükleyen</th><th>Dosya</th><th>Boyut</th><th>Not</th><th></th></tr></thead><tbody>${b.surumler.map(v => `<tr><td><b>v${v.no}</b></td><td>${esc(v.tarih)}</td><td class="sol">${esc(v.kullanici)}</td><td class="sol" title="${esc(v.dosya)}">${esc(v.dosya)}${v.gercek ? '' : ' <small style="display:inline">(örnek · kapak)</small>'}</td><td class="para">${kb(v.boyut)}</td><td class="sol">${esc(v.not || '')}</td><td><a class="btn btn-light border btn-xs" href="/SirketDokuman/Dosya?id=${b.id}&surum=${v.no}" target="_blank" rel="noopener"><i class="fas fa-up-right-from-square"></i></a></td></tr>`).join('') || '<tr><td colspan="7" class="na-bos text-center">Dosya yok.</td></tr>'}</tbody></table></div>
        <div class="na-kart-g"><div class="sd-yenile" style="grid-template-columns:2fr 1.4fr auto"><div><label>Yeni Sürüm Dosyası (PDF / Görsel / Office)</label><input type="file" class="form-control" id="srDosya" accept=".pdf,.jpg,.jpeg,.png,.doc,.docx,.xls,.xlsx"></div><div><label>Not</label><input class="form-control" id="srNot" placeholder="taranmış aslı, noter onaylı…"></div><div><label>&nbsp;</label><button type="button" class="btn btn-success btn-sm py-0" id="srYukle" style="font-size:.68rem;height:28px"><i class="fas fa-upload me-1"></i>Yükle</button></div></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Günlük</span></div><div class="na-kart-g"><ul class="na-zaman">${b.hareketler.map(h => `<li class="${/Yenilendi|Sürüm/.test(h.islem) ? 'fin' : /Arşiv/.test(h.islem) ? 'wf' : 'sis'}"><span class="t">${esc(h.zaman)}</span><b>${esc(h.islem)}</b> · ${esc(h.aciklama)} <span class="k">${esc(h.kullanici)}</span></li>`).join('') || '<li>Kayıt yok.</li>'}</ul></div></div>`;
  }
  async function islemSonu(r) { if (!r.success) { mesaj('danger', '<i class="fas fa-triangle-exclamation me-1"></i>' + esc(r.message)); return false; } mesaj('success', '<i class="fas fa-check me-1"></i>' + esc(r.message)); await yukle(true); if (r.belge) { A = r.belge; if ($('#sdDrawer').classList.contains('acik')) cizDrawer(); } return true; }
  $('#sdDIc').addEventListener('click', async e => {
    const b = A; if (!b) return;
    if (e.target.closest('#ynlKaydet')) { const r = await post('/SirketDokuman/Yenile', { Id: b.id, BelgeNo: $('#ynlNo').value, DuzenlemeTarihi: $('#ynlDuz').value, GecerlilikBitis: $('#ynlBit').value, Not: $('#ynlNot').value }).catch(() => ({ success: false, message: 'Ağ hatası.' })); await islemSonu(r); return; }
    if (e.target.closest('#srYukle')) { const f = $('#srDosya').files[0]; if (!f) { mesaj('warning', 'Dosya seçin.'); return; } const fd = new FormData(); fd.append('id', b.id); fd.append('not', $('#srNot').value); fd.append('dosya', f); const r = await postForm('/SirketDokuman/Surum', fd).catch(() => ({ success: false, message: 'Ağ hatası.' })); await islemSonu(r); return; }
    if (e.target.closest('#dArsiv')) { if (!confirm(`${b.no} ${b.arsiv ? 'arşivden çıkarılsın' : 'arşivlensin'} mi?`)) return; const r = await post('/SirketDokuman/Arsivle', { Id: b.id, Arsiv: !b.arsiv }).catch(() => ({ success: false, message: 'Ağ hatası.' })); await islemSonu(r); return; }
    if (e.target.closest('#dDuzenle')) { formDoldur(b); drawerKapat(); sekme('yeni'); return; }
  });

  // ---------- yeni belge formu
  function turSecenekleri() {
    const s = $('#ynSirket').value, k = $('#ynKategori').value; const T = (V.turler || []).filter(t => (!k || t.kategori === k) && (!t.sadece || t.sadece.includes(s)));
    const onceki = $('#ynTur').value; $('#ynTur').innerHTML = T.map(t => `<option value="${esc(t.kod)}">${esc(t.ad)}${t.zorunlu ? ' · zorunlu' : ''}${t.sureliAy ? ' · ' + t.sureliAy + ' ay' : ''}</option>`).join('') || '<option value="">Bu şirket için tanımlı tür yok</option>';
    if (T.some(t => t.kod === onceki)) $('#ynTur').value = onceki; turDegisti();
  }
  function turDegisti() {
    const t = turBul($('#ynTur').value); if (!t.kod) { $('#ynTurBilgi').textContent = ''; return; }
    if (!$('#ynId').value) { $('#ynKurum').value = t.kurum || ''; $('#ynSorumlu').value = kategoriBul(t.kategori).sorumlu || ''; }
    const duz = $('#ynDuzenleme').value || V.bugun; if (t.sureliAy) { const d = new Date(duz + 'T00:00:00'); d.setMonth(d.getMonth() + t.sureliAy); $('#ynBitis').value = d.toISOString().slice(0, 10); } else if (!$('#ynId').value) $('#ynBitis').value = '';
    $('#ynTurBilgi').innerHTML = `<b>${esc(t.ad)}</b> · ${t.zorunlu ? 'her şirkette bulunması zorunlu' : 'isteğe bağlı'} · ${t.sureliAy ? 'geçerlilik ' + t.sureliAy + ' ay (bitiş otomatik önerildi)' : 'süresiz'} · düzenleyen ${esc(t.kurum)}${t.sadece ? ' · yalnız: ' + t.sadece.map(x => sirketBul(x).ad).join(', ') : ''}`;
  }
  function formKur() {
    $('#ynSirket').innerHTML = V.sirketler.map(s => `<option value="${esc(s.kod)}">${esc(s.ad)}</option>`).join(''); if (F.sirket) $('#ynSirket').value = F.sirket;
    $('#ynKategori').innerHTML = '<option value="">Tümü</option>' + V.kategoriler.map(k => `<option value="${esc(k.kod)}">${esc(k.ad)}</option>`).join(''); if (F.kategori) $('#ynKategori').value = F.kategori;
    $('#ynSorumlular').innerHTML = (V.sorumlular || []).map(x => `<option value="${esc(x)}">`).join(''); $('#ynDuzenleme').value = V.bugun; turSecenekleri();
  }
  function formDoldur(b) { formTemizle(); $('#ynId').value = b.id; $('#ynSirket').value = b.sirketKod; $('#ynKategori').value = b.kategori; turSecenekleri(); $('#ynTur').value = b.turKod; $('#ynAd').value = b.ad; $('#ynBelgeNo').value = b.belgeNo || ''; $('#ynKurum').value = b.kurum || ''; $('#ynDuzenleme').value = b.duzenleme; $('#ynBitis').value = b.bitis || ''; $('#ynSorumlu').value = b.sorumlu || ''; $('#ynGizlilik').value = b.gizlilik || 'Genel'; $('#ynNot').value = b.not || ''; turDegisti(); $('#ynBitis').value = b.bitis || ''; }
  function formTemizle() { $('#ynId').value = ''; $('#ynAd').value = ''; $('#ynBelgeNo').value = ''; $('#ynNot').value = ''; $('#ynDosya').value = ''; $('#ynGizlilik').value = 'Genel'; $('#ynDuzenleme').value = V.bugun; turSecenekleri(); }
  $('#ynSirket').addEventListener('change', turSecenekleri); $('#ynKategori').addEventListener('change', turSecenekleri); $('#ynTur').addEventListener('change', turDegisti); $('#ynDuzenleme').addEventListener('change', turDegisti);
  $('#ynTemizle').addEventListener('click', formTemizle);
  $('#ynKaydet').addEventListener('click', async () => {
    if (!$('#ynTur').value) { mesaj('warning', 'Belge türü seçin.'); return; }
    const fd = new FormData($('#sdForm')); if ($('#ynId').value) fd.append('Id', $('#ynId').value);
    const r = await postForm('/SirketDokuman/Kaydet', fd).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (await islemSonu(r)) { formTemizle(); sekme('belgeler'); if (r.belge) drawerAc(r.belge.id); }
  });

  // ---------- olaylar
  $$('#sdSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p))); $('#sdYeniBtn').addEventListener('click', () => sekme('yeni'));
  $('#sdDKapat').addEventListener('click', drawerKapat); $('#sdPerde').addEventListener('click', drawerKapat);
  ['#sdSirketF', '#sdKategoriF', '#sdTurF', '#sdDurumF', '#sdSorumluF'].forEach(s => $(s).addEventListener('change', () => { F.sirket = $('#sdSirketF').value; F.kategori = $('#sdKategoriF').value; F.tur = $('#sdTurF').value; F.durum = $('#sdDurumF').value; F.sorumlu = $('#sdSorumluF').value; turFiltre(); cizHepsi(); }));
  $('#sdAra').addEventListener('input', () => { F.ara = $('#sdAra').value; cizTablo(); });
  $('#sdKpi').addEventListener('click', e => { const k = e.target.closest('[data-durum]'); if (!k) return; const d = k.dataset.durum; if (d === 'eksik') { sekme('eksik'); return; } F.durum = F.durum === d ? '' : d; $('#sdDurumF').value = F.durum; sekme('belgeler'); cizTablo(); });
  $('#sdTablo').addEventListener('click', e => { const tr = e.target.closest('tr.tik[data-id]'); if (tr && !e.target.closest('a')) drawerAc(+tr.dataset.id); });
  $('#sdGunlukTablo').addEventListener('click', e => { const tr = e.target.closest('tr.tik[data-id]'); if (tr) drawerAc(+tr.dataset.id); });
  $('#sdTakvim').addEventListener('click', e => { const li = e.target.closest('li[data-id]'); if (li) drawerAc(+li.dataset.id); }); $('#sdTakvimAy').addEventListener('change', cizTakvim);
  $('#sdMatris').addEventListener('click', e => { const h = e.target.closest('.sd-h'); if (!h) return; F.sirket = h.dataset.s; F.kategori = h.dataset.k; F.durum = ''; $('#sdSirketF').value = F.sirket; $('#sdKategoriF').value = F.kategori; $('#sdDurumF').value = ''; turFiltre(); sekme('belgeler'); cizHepsi(); });
  $('#sdEksikTablo').addEventListener('click', e => { const b = e.target.closest('.sd-eksik-ekle'); if (!b) return; formTemizle(); $('#ynSirket').value = b.dataset.s; const t = turBul(b.dataset.t); $('#ynKategori').value = t.kategori || ''; turSecenekleri(); $('#ynTur').value = b.dataset.t; turDegisti(); sekme('yeni'); });
  $('#sdSirketler').addEventListener('click', e => { const l = e.target.closest('.sd-sirket-liste'), k = e.target.closest('.sd-sirket-eksik'); if (!l && !k) return; F.sirket = (l || k).dataset.s; $('#sdSirketF').value = F.sirket; turFiltre(); cizHepsi(); sekme(l ? 'belgeler' : 'eksik'); });
  $('#sdExcel').addEventListener('click', () => window.open(`/SirketDokuman/Excel?kategori=${encodeURIComponent(F.kategori)}&sirket=${encodeURIComponent(F.sirket)}`, '_blank'));
  function turFiltre() { const T = (V.turler || []).filter(t => !F.kategori || t.kategori === F.kategori); const o = $('#sdTurF').value; $('#sdTurF').innerHTML = '<option value="">Tümü</option>' + T.map(t => `<option value="${esc(t.kod)}">${esc(t.ad)}</option>`).join(''); if (T.some(t => t.kod === o)) $('#sdTurF').value = o; else F.tur = ''; }
  function cizHepsi() { cizKirilim(); cizTablo(); cizMatris(); cizTakvim(); cizEksik(); cizSirketler(); cizGunluk(); }

  // ---------- veri
  async function yukle(sessiz) {
    let r; try { r = await getJ('/SirketDokuman/Veri'); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r;
    if (ilk) {
      ilk = false;
      $('#sdSirketF').innerHTML = '<option value="">Tümü</option>' + V.sirketler.map(s => `<option value="${esc(s.kod)}">${esc(s.ad)}</option>`).join(''); $('#sdSirketF').value = F.sirket;
      $('#sdKategoriF').innerHTML = '<option value="">Tümü</option>' + V.kategoriler.map(k => `<option value="${esc(k.kod)}">${esc(k.ad)}</option>`).join(''); $('#sdKategoriF').value = F.kategori;
      $('#sdSorumluF').innerHTML = '<option value="">Tümü</option>' + (V.sorumlular || []).map(x => `<option>${esc(x)}</option>`).join(''); turFiltre(); formKur();
      sekme(kok.dataset.sekme || 'belgeler'); if (kok.dataset.id) drawerAc(+kok.dataset.id);
    }
    cizHepsi();
  }
  yukle();
})();
