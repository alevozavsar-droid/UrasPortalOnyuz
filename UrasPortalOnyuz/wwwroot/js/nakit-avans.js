// ============================================================
// NAKİT AVANS TALEP, ÖDEME, MAHSUP VE KAPATMA MODÜLÜ — ekran davranışı
// Veri: /IkTalep/AvansVeri. Bütün tutar ve statüler sunucuda hesaplanır (Data/NakitAvansOrnek.cs); bu dosya yalnızca çizer,
// form gönderir ve Finans / Muhasebe / ERP'den gelecek "kaynak sistem olaylarını" (§30) simüle eder.
// ============================================================
(function () {
  'use strict';
  const kok = document.getElementById('na'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const para = (v, pb) => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + (pb ? ' ' + pb : '');
  const tam = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const bugunIso = new Date().toISOString().slice(0, 10);
  let V = null, aktifNo = kok.dataset.no || '';

  // ---- rozetler ----
  const rozet = (metin, tur) => `<span class="rt-durum rt-${tur}">${esc(metin)}</span>`;
  const talepRozet = d => rozet(d, d === 'Onaylandı' ? 'ok' : d === 'Reddedildi' ? 'hata' : /Revizyon/.test(d) ? 'bilgi' : 'bekle');
  const odemeRozet = d => rozet(d, d === 'Ödeme Teslim Edildi' ? 'ok' : d === 'Kısmi Ödendi' ? 'bekle' : d === 'Ödeme İptal Edildi' ? 'hata' : 'bilgi');
  const kapatmaRozet = d => rozet(d, d === 'Kapandı' ? 'ok' : d === 'Süresi Geçti' ? 'hata' : d === 'Henüz Ödeme Yok' ? 'bilgi' : 'bekle');
  const yansRozet = d => !d ? '<span class="text-muted">—</span>' : rozet(d, d === 'Yansıtıldı' ? 'ok' : d === 'Hata' ? 'hata' : 'bekle');
  const oncelik = o => o === 'Çok Acil' ? `<span class="na-cokacil">${o}</span>` : o === 'Acil' ? `<span class="na-acil">${o}</span>` : o;
  const kilit = '<i class="fas fa-lock na-kilit" title="Kaynak: Finans / Muhasebe — talep ekranından değiştirilemez"></i>';

  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 9000); }
  async function post(url, veri) {
    const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) });
    return r.json();
  }
  function guncelle(t) { const i = V.talepler.findIndex(x => x.no === t.no); if (i >= 0) V.talepler[i] = t; else V.talepler.unshift(t); cizHepsi(); }
  const benimMi = t => t.olusturan.kullaniciId === V.ben.kullaniciId || t.kullanan.kullaniciId === V.ben.kullaniciId;

  // ---- sekmeler ----
  function sekme(p) {
    $$('#naSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('.na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    try { history.replaceState(null, '', location.pathname + location.search + '#' + p + (p === 'detay' && aktifNo ? '/' + aktifNo : '')); } catch (e) { }
  }
  $$('#naSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));

  // ---- §25.2 gösterge kutucukları ----
  function cizKpi() {
    const T = V.talepler;
    const odemeBek = T.filter(t => t.onaylandi && t.odemeBekleyen > 0), kismi = T.filter(t => t.odemeDurumu === 'Kısmi Ödendi'),
      acik = T.filter(t => t.teslimEdilen > 0 && !t.kapandi), gecik = T.filter(t => t.gecikmis),
      bugun = T.filter(t => t.kapatmaIso === bugunIso && !t.kapandi), yans = T.filter(t => t.yansitma && t.yansitma.durum === 'Bekliyor');
    const tl = l => l.reduce((s, t) => s + (t.paraBirimi === 'TL' ? Number(t.odemeBekleyen) : 0), 0);
    const kut = [
      ['#2563eb', 'Ödeme Bekleyen', `${odemeBek.length} Talep`, `${tam(odemeBek.reduce((s, t) => s + t.tutarTl / t.tutar * t.odemeBekleyen, 0))} TL`, 'bekleyen'],
      ['#f59e0b', 'Kısmi Ödenen', `${kismi.length} Talep`, `${tam(kismi.reduce((s, t) => s + t.tutarTl / t.tutar * t.odemeBekleyen, 0))} TL kalan`, 'bekleyen'],
      ['#16a34a', 'Açık Avans', `${acik.length} Talep`, `${tam(acik.reduce((s, t) => s + t.tutarTl / t.tutar * t.acikTutar, 0))} TL`, 'takip:acik'],
      ['#dc2626', 'Gecikmiş Avans', `${gecik.length} Talep`, `${tam(gecik.reduce((s, t) => s + t.tutarTl / t.tutar * t.acikTutar, 0))} TL`, 'takip:gecikmis'],
      ['#7c3aed', 'Bugün Kapanması Gereken', `${bugun.length} Talep`, bugun.map(t => t.no).join(', ') || '—', 'takip:bugun'],
      ['#0891b2', 'Yansıtma Bekleyen', `${yans.length} Kayıt`, yans.map(t => t.yansitma.masrafSirketi).filter((v, i, a) => a.indexOf(v) === i).join(', ') || '—', 'takip:yansitma']
    ];
    $('#naKpi').innerHTML = kut.map(k => `<div style="--k:${k[0]}" data-git="${k[4]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s">${esc(k[3])}</div></div>`).join('');
    $$('#naKpi > div').forEach(d => d.addEventListener('click', () => {
      const [p, f] = d.dataset.git.split(':');
      if (p === 'takip') { $('#naTakipAra').value = ''; $('#naTakipGecikmis').checked = f === 'gecikmis'; takipOzel = f === 'acik' ? t => t.teslimEdilen > 0 && !t.kapandi : f === 'bugun' ? t => t.kapatmaIso === bugunIso && !t.kapandi : f === 'yansitma' ? t => t.yansitma && t.yansitma.durum === 'Bekliyor' : null; cizTakip(); }
      sekme(p);
    }));
    $('#naNListe').textContent = T.filter(benimMi).length; $('#naNBekleyen').textContent = odemeBek.length; $('#naNTakip').textContent = T.length;
  }

  // ---- yeni talep formu ----
  function kisiOto(k, baslik) {
    return `<div class="h">${baslik}</div>` + (!k ? '<span class="text-muted">Kişi seçilmedi</span>' :
      `<span>Personel No <b>${esc(k.personelNo)}</b></span><span>Ad Soyad <b>${esc(k.adSoyad)}</b></span><span>Şirket <b>${esc(k.sirket)}</b></span><span>Departman <b>${esc(k.departman)}</b></span>
       <span>Ünvan <b>${esc(k.unvan)}</b></span><span>Masraf Merkezi <b>${esc(k.masrafMerkezi)}</b></span><span>Aktif <b>${k.aktif ? 'Evet' : '<span class="text-danger">Hayır</span>'}</b></span><span>Kullanıcı ID <b>${esc(k.kullaniciId)}</b></span>`);
  }
  function onayOnizleme(tutar, pb) {
    if (!V) return;
    const kur = { TL: 1, USD: 41.2, EUR: 47.9, AED: 11.2 }[pb] || 1, tl = (Number(tutar) || 0) * kur;
    const satir = V.limitler.find(l => tl >= l.min && tl <= l.max) || V.limitler[0];
    const ust = satir.ustYonetim === 'X' || (satir.ustYonetim === 'Parametre' && tl >= V.ustOnayEsigi);
    const k = $('#naKullanan').value, kisi = V.personel.find(p => p.personelNo === k);
    const yon = kisi ? (V.personel.find(p => p.rol === 'Yönetici' && p.departman === kisi.departman) || V.personel.find(p => p.rol === 'Yönetici')) : null;
    const adimlar = [['Talep', V.ben.adSoyad], ['Yönetici Onayı', yon ? yon.adSoyad : '—']];
    if (ust) adimlar.push(['Üst Yönetim Onayı', (V.personel.find(p => p.unvan === 'CFO') || {}).adSoyad]);
    if (V.politika === 'Ek üst onay iste' && !ust && kisi && acikAvanslar(kisi).length) adimlar.push(['Üst Yönetim Onayı', 'politika: açık avans']);
    adimlar.push(['Finans Uygunluk Onayı', (V.personel.find(p => p.rol === 'Finans' && /Müdür/.test(p.unvan)) || {}).adSoyad]);
    $('#naAkisOnizleme').innerHTML = adimlar.map((a, i) => (i ? '<i class="fas fa-chevron-right"></i>' : '') + `<span title="${esc(a[1])}">${esc(a[0])}</span>`).join('') + (pb !== 'TL' ? `<small class="text-muted ms-1">≈ ${tam(tl)} TL</small>` : '');
  }
  const acikAvanslar = k => V.talepler.filter(t => t.kullanan.personelNo === k.personelNo && t.teslimEdilen > 0 && !t.kapandi);
  function acikKontrol(k) {
    const el = $('#naAcikKontrol');
    if (!k) { el.innerHTML = '<div class="na-bos">Avansı kullanacak kişi seçilince Finans sisteminden canlı sorgulanır.</div>'; return; }
    const l = acikAvanslar(k), toplam = l.reduce((s, t) => s + t.tutarTl / t.tutar * t.acikTutar, 0), enEski = Math.max(0, ...l.map(t => t.bekleyenGun)), gec = l.filter(t => t.gecikmis).length;
    el.innerHTML = `<div class="na-formul" style="grid-template-columns:repeat(2,1fr)"><div><div class="l">Açık Avans Adedi</div><div class="v">${l.length}</div></div><div><div class="l">Toplam Açık Bakiye</div><div class="v">${tam(toplam)} TL</div></div><div><div class="l">En Eski Açık Avans</div><div class="v">${enEski} gün</div></div><div class="${gec ? 'acik pozitif' : ''}"><div class="l">Gecikmiş Avans</div><div class="v ${gec ? 'text-danger' : ''}">${gec}</div></div></div>` +
      (l.length ? `<div class="na-mini mt-1">${l.map(t => `<a href="#" data-ac="${t.no}">${t.no}</a> ${para(t.acikTutar, t.paraBirimi)}${t.gecikmis ? ' <span class="text-danger">(' + t.gecikenGun + ' gün gecikmiş)</span>' : ''}`).join(' · ')}</div>` : '');
    $$('[data-ac]', el).forEach(a => a.addEventListener('click', e => { e.preventDefault(); detayAc(a.dataset.ac); }));
    const u = $('#naKullananUyari'); u.className = 'na-uyari';
    if (!k.aktif) { u.className = 'na-uyari kirmizi'; u.innerHTML = '<i class="fas fa-ban me-1"></i>Personel aktif değil: talep açılamaz (T02).'; }
    else if (l.length) { u.className = 'na-uyari ' + (V.politika === 'Yeni talebi engelle' ? 'kirmizi' : 'sari'); u.innerHTML = `<i class="fas fa-triangle-exclamation me-1"></i>${esc(k.adSoyad)} adına ${l.length} açık avans var (${tam(toplam)} TL). Politika: <b>${esc(V.politika)}</b>.`; }
  }
  function grupIciUyari() {
    if (!V) return;
    const k = V.personel.find(p => p.personelNo === $('#naKullanan').value), s = $('#naSirket').value, u = $('#naGrupIci');
    if (k && s && k.sirket !== s) { u.className = 'na-uyari mavi'; u.innerHTML = `<i class="fas fa-building me-1"></i><b>Grup içi avans:</b> ödeyen şirket <b>${esc(s)}</b>, masrafın ait olduğu şirket <b>${esc(k.sirket)}</b>. Kapanışta grup içi yansıtma gereksinimi otomatik oluşur (§17).`; }
    else u.className = 'na-uyari';
  }
  function belgeSecenekleri() {
    if (!V) return;
    const fd = $('#naFatura').value, liste = V.belgeTurleri[fd] || [];
    $('#naBelgeler').innerHTML = liste.map((b, i) => `<label class="form-check form-check-inline m-0"><input class="form-check-input" type="checkbox" name="belge" value="${esc(b)}" ${i === 0 ? 'checked' : ''}> <span class="form-check-label">${esc(b)}</span></label>`).join('') || '<span class="na-bos">Önce fatura durumunu seçin</span>';
  }
  function formHazirla() {
    const sec = (id, l, sel) => $(id).innerHTML = l.map(x => `<option ${x === sel ? 'selected' : ''}>${esc(x)}</option>`).join('');
    sec('#naSirket', V.sirketler, V.ben.sirket); sec('#naOncelik', V.oncelikler, 'Normal'); sec('#naPb', V.paraBirimleri, 'TL');
    $('#naFatura').innerHTML = '<option value="">Seçiniz…</option>' + V.faturaDurumlari.map(x => `<option>${esc(x)}</option>`).join('');
    $('#naKategori').innerHTML = '<option value="">Seçiniz…</option>' + V.kategoriler.map(x => `<option>${esc(x)}</option>`).join('');
    $('#naKullanan').innerHTML = '<option value="">Seçiniz…</option>' + V.personel.map(p => `<option value="${esc(p.personelNo)}" ${p.personelNo === V.ben.personelNo ? 'selected' : ''}>${esc(p.adSoyad)} — ${esc(p.sirket)} / ${esc(p.departman)}${p.aktif ? '' : ' (pasif)'}</option>`).join('');
    $('#naOlusturan').innerHTML = kisiOto(V.ben, "Talebi oluşturan kişi (İK'dan otomatik)");
    const n = new Date(); $('#naTarih').value = n.toLocaleDateString('tr-TR') + ' ' + n.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    $('#naKullanim').value = bugunIso; const kp = new Date(); kp.setDate(kp.getDate() + 7); $('#naKapatma').value = kp.toISOString().slice(0, 10);
    $('#naPolitika').value = V.politika; $('#naEsik').value = V.ustOnayEsigi;
    $('#naLimitTablo').innerHTML = V.limitler.map(l => `<tr><td>${tam(l.min)}</td><td>${tam(l.max)}</td><td>${l.yonetici ? 'X' : ''}</td><td>${esc(l.ustYonetim)}</td><td>${l.finans ? 'X' : ''}</td></tr>`).join('');
    kullananDegisti(); belgeSecenekleri();
  }
  function kullananDegisti() {
    if (!V) return;
    const k = V.personel.find(p => p.personelNo === $('#naKullanan').value);
    $('#naKullananBilgi').innerHTML = kisiOto(k, 'Seçim sonrası İK / ERP\'den otomatik (masraf merkezi bu kişiye göre)');
    acikKontrol(k); grupIciUyari(); onayOnizleme($('#naTutar').value, $('#naPb').value);
  }
  $('#naKullanan').addEventListener('change', kullananDegisti);
  $('#naSirket').addEventListener('change', grupIciUyari);
  $('#naFatura').addEventListener('change', belgeSecenekleri);
  ['#naTutar', '#naPb'].forEach(s => $(s).addEventListener('input', () => onayOnizleme($('#naTutar').value, $('#naPb').value)));
  $('#naParamKaydet').addEventListener('click', async () => {
    const j = await post('/IkTalep/AvansParametre', { politika: $('#naPolitika').value, esik: $('#naEsik').value });
    V.politika = j.politika; V.ustOnayEsigi = j.ustOnayEsigi; kullananDegisti(); mesaj('success', 'Parametreler güncellendi.');
  });
  $('#naForm').addEventListener('reset', () => setTimeout(() => { formHazirla(); }, 0));
  $('#naForm').addEventListener('submit', async e => {
    e.preventDefault();
    if (!V) { mesaj('warning', 'Veriler henüz yükleniyor…'); return; }
    const f = e.target, d = {};
    new FormData(f).forEach((v, k) => { if (k !== 'belge') d[k] = v; });
    d.belgeTurleri = $$('input[name=belge]:checked', f).map(x => x.value).join('|');
    d.ekler = Array.from($('#naEk').files || []).map(x => x.name).join('|');
    const eksik = $$('[required]', f).filter(x => !String(x.value).trim());
    if (!$('#naKullanan').value) eksik.push($('#naKullanan'));
    if (eksik.length) { eksik.forEach(x => x.classList.add('is-invalid')); mesaj('danger', 'Zorunlu alanları doldurun.'); eksik[0].focus(); return; }
    if (!d.belgeTurleri) { mesaj('danger', 'En az bir beklenen belge türü seçin.'); return; }
    if (d.kapatmaTarihi < d.kullanimTarihi) { mesaj('danger', 'Beklenen kapatma tarihi kullanım tarihinden önce olamaz.'); return; }
    $('#naGonder').disabled = true;
    try {
      const j = await post('/IkTalep/AvansOlustur', d);
      if (!j.success) { mesaj('danger', esc(j.message)); return; }
      mesaj('success', '<i class="fas fa-circle-check me-1"></i>' + esc(j.message));
      guncelle(j.talep); f.reset(); detayAc(j.no);
    } finally { $('#naGonder').disabled = false; }
  });
  $('#naForm').addEventListener('input', e => e.target.classList.remove('is-invalid'));

  // ---- taleplerim ----
  function cizListe() {
    const l = V.talepler.filter(benimMi);
    $('#naListe tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${t.no}"><td><b>${t.no}</b>${t.revizyon ? ' <small class="text-muted">rev.' + t.revizyon + '</small>' : ''}</td><td>${t.tarih}</td><td>${esc(t.kullanan.adSoyad)}</td><td>${esc(t.konu)}</td><td class="s">${para(t.tutar, t.paraBirimi)}</td><td>${talepRozet(t.talepDurumu)}</td><td>${odemeRozet(t.odemeDurumu)}</td><td>${kapatmaRozet(t.kapatmaDurumu)}</td><td class="s ${t.acikTutar > 0 ? 'fw-bold' : ''}">${para(t.acikTutar, t.paraBirimi)}</td><td>${t.kapatmaTarihi}${t.gecikmis ? ' <span class="text-danger">+' + t.gecikenGun + 'g</span>' : ''}</td></tr>`).join('') || '<tr><td colspan="10" class="na-bos">Talebiniz yok.</td></tr>';
    $$('#naListe tr.tik').forEach(r => r.addEventListener('click', () => detayAc(r.dataset.no)));
  }

  // ---- §10 bekleyen ödemeler ----
  function cizBekleyen() {
    const l = V.talepler.filter(t => t.onaylandi && t.odemeBekleyen > 0).sort((a, b) => ['Çok Acil', 'Acil', 'Normal'].indexOf(a.oncelik) - ['Çok Acil', 'Acil', 'Normal'].indexOf(b.oncelik));
    $('#naBekleyen tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${t.no}"><td><b>${t.no}</b></td><td>${esc(t.sirket)}</td><td>${esc(t.olusturan.adSoyad)}</td><td>${esc(t.kullanan.adSoyad)}</td><td>${esc(t.departman)}</td><td>${esc(t.masrafMerkezi)}</td><td>${t.tarih}</td><td>${t.onayTarihi || '—'}</td><td class="s">${para(t.onaylananTutar)}</td><td>${t.paraBirimi}</td><td>${odemeRozet(t.odemeDurumu)}</td><td class="s fw-bold">${para(t.odemeBekleyen)}</td><td>${oncelik(t.oncelik)}</td><td class="s">${t.onayTarihi ? gun(t.onayTarihi) : '—'}</td><td><button type="button" class="btn btn-primary btn-sm py-0" data-no="${t.no}"><i class="fas fa-arrow-up-right-from-square me-1"></i>Aç</button></td></tr>`).join('') || '<tr><td colspan="15" class="na-bos">Ödeme bekleyen talep yok.</td></tr>';
    $$('#naBekleyen tr.tik').forEach(r => r.addEventListener('click', () => detayAc(r.dataset.no)));
  }
  const gun = ddmmyyyy => { const [d, m, y] = ddmmyyyy.split('.'); return Math.max(0, Math.round((Date.now() - new Date(+y, m - 1, +d)) / 864e5)); };

  // ---- §25.1 takip raporu ----
  let takipOzel = null;
  function cizTakip() {
    const ara = ($('#naTakipAra').value || '').toLocaleLowerCase('tr-TR'), kf = $('#naTakipKapatma').value, of = $('#naTakipOdeme').value, g = $('#naTakipGecikmis').checked;
    const l = V.talepler.filter(t => (!ara || [t.no, t.olusturan.adSoyad, t.kullanan.adSoyad, t.konu, t.sirket].join(' ').toLocaleLowerCase('tr-TR').includes(ara)) && (!kf || t.kapatmaDurumu === kf) && (!of || t.odemeDurumu === of) && (!g || t.gecikmis) && (!takipOzel || takipOzel(t)));
    $('#naTakip tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${t.no}"><td><b>${t.no}</b></td><td>${esc(t.sirket)}</td><td>${esc(t.olusturan.adSoyad)}</td><td>${esc(t.kullanan.adSoyad)}</td><td>${esc(t.departman)}</td><td>${esc(t.masrafMerkezi)}</td><td>${t.tarih}</td><td class="s">${para(t.tutar)}</td><td class="s">${para(t.onaylananTutar)}</td><td class="s">${para(t.teslimEdilen)}</td><td class="s">${para(t.mahsupEdilen)}</td><td class="s">${para(t.iadeEdilen)}</td><td class="s ${t.acikTutar > 0 ? 'fw-bold' : ''}">${para(t.acikTutar)}</td><td>${t.paraBirimi}</td><td>${esc(t.faturaDurumu)}</td><td>${odemeRozet(t.odemeDurumu)}</td><td>${kapatmaRozet(t.kapatmaDurumu)}</td><td>${t.fiiliKapatma || t.kapatmaTarihi + ' <small class="text-muted">(bekl.)</small>'}</td><td class="s ${t.gecikmis ? 'text-danger fw-bold' : ''}">${t.gecikenGun || ''}</td><td>${esc(t.yonetici || '')}</td><td>${esc(t.ustOnay || '—')}</td><td>${esc(t.finansOnayi || '')}</td><td>${esc(t.muhasebeFisNo || '—')}</td><td>${yansRozet(t.yansitma && t.yansitma.durum)}</td></tr>`).join('') || '<tr><td colspan="24" class="na-bos">Kayıt yok.</td></tr>';
    $('#naTakipSayi').textContent = `${l.length} / ${V.talepler.length} kayıt`;
    $$('#naTakip tr.tik').forEach(r => r.addEventListener('click', () => detayAc(r.dataset.no)));
  }
  ['#naTakipAra', '#naTakipKapatma', '#naTakipOdeme', '#naTakipGecikmis'].forEach(s => $(s).addEventListener('input', () => { takipOzel = null; cizTakip(); }));
  function takipFiltreleri() {
    const uniq = f => V.talepler.map(f).filter((v, i, a) => a.indexOf(v) === i);
    $('#naTakipKapatma').innerHTML = '<option value="">Kapatma: tümü</option>' + uniq(t => t.kapatmaDurumu).map(x => `<option>${esc(x)}</option>`).join('');
    $('#naTakipOdeme').innerHTML = '<option value="">Ödeme: tümü</option>' + uniq(t => t.odemeDurumu).map(x => `<option>${esc(x)}</option>`).join('');
  }

  // ---- §3 talep detayı ----
  function detayAc(no) { aktifNo = no; cizDetay(); sekme('detay'); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  function cizDetay() {
    const t = V.talepler.find(x => x.no === aktifNo), el = $('#naDetay');
    $('#naDetayNo').textContent = t ? t.no : '—';
    if (!t) { el.innerHTML = '<div class="na-bos">Listeden bir talep seçin.</div>'; return; }
    const bekleyenAdim = t.onaylar.find(o => o.durum === 'Onay Bekliyor');
    const adimSinif = d => d === 'Onaylandı' || d === 'Tamamlandı' ? 'ok' : d === 'Onay Bekliyor' || d === 'Revizyon' ? 'bekle' : d === 'Reddedildi' ? 'hata' : '';
    const kisiKart = (k, b) => `<div class="na-kart h-100 mb-0"><div class="na-kart-b"><span>${b}</span><span class="not">İK</span></div><div class="na-kart-g na-kisi">
      <span class="l">Personel No</span><span class="v">${esc(k.personelNo)}</span><span class="l">Ad Soyad</span><span class="v">${esc(k.adSoyad)}</span><span class="l">Şirket</span><span class="v">${esc(k.sirket)}</span>
      <span class="l">Departman</span><span class="v">${esc(k.departman)}</span><span class="l">Ünvan</span><span class="v">${esc(k.unvan)}</span><span class="l">Masraf Merkezi</span><span class="v">${esc(k.masrafMerkezi)}</span>
      <span class="l">Aktif Personel</span><span class="v">${k.aktif ? 'Evet' : '<span class="text-danger">Hayır</span>'}</span><span class="l">Kullanıcı ID</span><span class="v">${esc(k.kullaniciId)}</span></div></div>`;
    const acikK = acikAvanslar(t.kullanan).filter(x => x.no !== t.no);
    el.innerHTML = `
    <div class="na-kart">
      <div class="na-kart-b"><span><i class="fas fa-file-invoice me-1"></i>Talep Üst Bilgileri — <b>${t.no}</b>${t.revizyon ? ` <span class="not">revizyon ${t.revizyon}</span>` : ''}</span>
        <span class="d-flex gap-1 flex-wrap">${t.gecikmis ? rozet('Gecikmiş · ' + t.gecikenGun + ' gün', 'hata') : ''}${t.grupIci ? rozet('Grup içi', 'bilgi') : ''}${oncelik(t.oncelik)}
        <button type="button" class="btn btn-light btn-sm py-0" id="naYazdir"><i class="fas fa-print me-1"></i>Yazdır</button></span></div>
      <div class="na-kart-g">
        <div class="na-ustbilgi">
          <div><div class="l">Talep No</div><div class="v">${t.no}</div></div><div><div class="l">Talep Tarihi / Saati</div><div class="v">${t.tarih} ${t.saat}</div></div><div><div class="l">Şirket (ödeyen)</div><div class="v">${esc(t.sirket)}</div></div>
          <div><div class="l">Departman</div><div class="v">${esc(t.departman)}</div></div><div><div class="l">Masraf Merkezi</div><div class="v">${esc(t.masrafMerkezi)}</div></div><div><div class="l">Entegrasyon</div><div class="v">${rozet(t.entegrasyonDurumu, t.entegrasyonDurumu === 'Başarılı' ? 'ok' : t.entegrasyonDurumu === 'Hata' ? 'hata' : 'bekle')} <small class="text-muted">son senkron ${t.sonSenkron || '—'}</small></div></div>
        </div>
        <div class="na-durum3">
          <div><div class="l">Talep Durumu <small>(Workflow)</small></div><div class="v">${talepRozet(t.talepDurumu)}</div><div class="s">${bekleyenAdim ? 'Sırada: ' + esc(bekleyenAdim.kisi) : t.onaylandi ? 'Onaylanan tutar ' + para(t.onaylananTutar, t.paraBirimi) : ''}</div></div>
          <div><div class="l">Ödeme Durumu <small>(Finans)</small></div><div class="v">${odemeRozet(t.odemeDurumu)}</div><div class="s">Teslim ${para(t.teslimEdilen, t.paraBirimi)} · bekleyen ${para(t.odemeBekleyen, t.paraBirimi)}</div></div>
          <div><div class="l">Kapatma Durumu <small>(Sistem)</small></div><div class="v">${kapatmaRozet(t.kapatmaDurumu)}</div><div class="s">Açık ${para(t.acikTutar, t.paraBirimi)} · beklenen kapatma ${t.kapatmaTarihi}${t.fiiliKapatma ? ' · kapandı ' + t.fiiliKapatma : ''}</div></div>
        </div>
      </div>
    </div>
    <div class="row g-2 mb-2"><div class="col-md-6">${kisiKart(t.olusturan, '<i class="fas fa-user-pen me-1"></i>Talebi Oluşturan Kişi')}</div><div class="col-md-6">${kisiKart(t.kullanan, '<i class="fas fa-user-check me-1"></i>Avansı Kullanacak Kişi')}
      ${acikK.length ? `<div class="na-uyari sari mt-1"><i class="fas fa-triangle-exclamation me-1"></i>Bu kişinin ${acikK.length} başka açık avansı var: ${acikK.map(x => `<a href="#" data-ac="${x.no}">${x.no}</a> (${para(x.acikTutar, x.paraBirimi)})`).join(', ')}</div>` : ''}</div></div>
    <div class="row g-2">
      <div class="col-lg-7">
        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-list-check me-1"></i>Talep Detayları</span>${t.talepDurumu === 'Revizyon Bekliyor' && benimMi(t) ? '<button type="button" class="btn btn-warning btn-sm py-0" id="naRevizeAc"><i class="fas fa-pen me-1"></i>Revize Et</button>' : ''}</div>
          <div class="na-kart-g">
            <div class="na-ustbilgi" style="grid-template-columns:repeat(4,1fr)">
              <div><div class="l">Talep Türü</div><div class="v">${esc(t.talepTuru)}</div></div><div style="grid-column:span 3"><div class="l">Talep Konusu</div><div class="v">${esc(t.konu)}</div></div>
              <div style="grid-column:span 4"><div class="l">Detaylı Açıklama</div><div>${esc(t.aciklama)}</div></div>
              <div><div class="l">Talep Tutarı</div><div class="v">${para(t.tutar, t.paraBirimi)}${t.paraBirimi !== 'TL' ? ` <small class="text-muted">≈ ${tam(t.tutarTl)} TL</small>` : ''}</div></div><div><div class="l">Onaylanan Tutar</div><div class="v">${t.onaylandi ? para(t.onaylananTutar, t.paraBirimi) : '—'} ${kilit}</div></div>
              <div><div class="l">Tahmini Kullanım</div><div class="v">${t.kullanimTarihi}</div></div><div><div class="l">Beklenen Kapatma</div><div class="v">${t.kapatmaTarihi}</div></div>
              <div><div class="l">Fatura Durumu</div><div class="v">${esc(t.faturaDurumu)}</div></div><div><div class="l">Beklenen Belge Türü</div><div class="v">${t.belgeTurleri.map(esc).join(' + ')}</div></div>
              <div><div class="l">Harcama Kategorisi</div><div class="v">${esc(t.harcamaKategorisi)}</div></div><div><div class="l">Öncelik</div><div class="v">${oncelik(t.oncelik)}</div></div>
            </div>
            <div id="naRevizeForm" style="display:none" class="mt-2 p-2 border rounded bg-light">
              <div class="row g-2 align-items-end"><div class="col-md-3"><label class="form-label">Yeni Tutar (${t.paraBirimi})</label><input class="form-control" type="number" step="0.01" id="naRevTutar" value="${t.tutar}"></div><div class="col-md-5"><label class="form-label">Konu</label><input class="form-control" id="naRevKonu" value="${esc(t.konu)}"></div><div class="col-md-4 d-flex gap-1"><button type="button" class="btn btn-primary btn-sm" id="naRevGonder">Revizyonu Gönder</button><button type="button" class="btn btn-light btn-sm" id="naRevIptal">Vazgeç</button></div></div>
              <div class="form-text">Tutar limit bandını değiştiriyorsa onay matrisi yeniden hesaplanır (örn. 40.000 → 60.000 TL: Üst Yönetim Onayı devreye girer).</div>
            </div>
          </div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-clipboard-check me-1"></i>Onay Süreci</span><span class="not">limit master: ${tam(t.tutarTl)} TL → ${t.onaylar.filter(o => o.sira > 1).map(o => o.adim.replace(' Onayı', '')).join(' + ')}</span></div>
          <div class="na-kart-g">
            ${t.onaylar.map(o => `<div class="na-adim ${adimSinif(o.durum)}"><span class="no">${o.sira}</span><div><div class="ad">${esc(o.adim)}</div><div class="k">${esc(o.kisi)}${o.not ? ' · <i>' + esc(o.not) + '</i>' : ''}</div></div><div class="sag">${o.durum === 'Onaylandı' || o.durum === 'Tamamlandı' ? rozet(o.durum, 'ok') : o.durum === 'Onay Bekliyor' ? rozet('Onay Bekliyor', 'bekle') : o.durum === 'Reddedildi' ? rozet('Reddedildi', 'hata') : o.durum === 'Revizyon' ? rozet('Revizyona Gönderildi', 'bilgi') : `<span class="text-muted">${esc(o.durum === 'Pasif' ? 'Önceki adım sonrası aktif' : o.durum)}</span>`}<div class="k">${o.tarih || ''}</div></div></div>`).join('')}
            ${bekleyenAdim ? `<div class="d-flex gap-1 flex-wrap align-items-center mt-2 pt-2 border-top"><span class="na-mini me-1">Onaycı işlemi (${esc(bekleyenAdim.kisi)} — örnek rol):</span>
              <button type="button" class="btn btn-success btn-sm py-0" data-onay="Onayla"><i class="fas fa-check me-1"></i>Onayla</button><button type="button" class="btn btn-outline-danger btn-sm py-0" data-onay="Reddet"><i class="fas fa-xmark me-1"></i>Reddet</button><button type="button" class="btn btn-outline-warning btn-sm py-0" data-onay="Revizyon"><i class="fas fa-rotate-left me-1"></i>Revizyona Gönder</button>
              <input class="form-control form-control-sm flex-grow-1" id="naOnayNot" placeholder="Not / red gerekçesi (red ve revizyonda zorunlu)"></div>` : ''}
            ${bekleyenAdim && bekleyenAdim.adim === 'Finans Uygunluk Onayı' ? `<div class="na-oto mt-2"><div class="h">Finansın gördüğü bilgi (§9)</div><span>Talep / onaylanan <b>${para(t.tutar, t.paraBirimi)}</b></span><span>Şirket / PB <b>${esc(t.sirket)} · ${t.paraBirimi}</b></span><span>Kasa bakiyesi <b>412.500 TL</b> <small>(örnek)</small></span><span>Açık avans <b>${acikK.length} adet · ${tam(acikK.reduce((s, x) => s + x.tutarTl / x.tutar * x.acikTutar, 0))} TL</b></span><span>Gecikmiş açık avans <b>${acikK.filter(x => x.gecikmis).length}</b></span><span>Bekleyen ödeme yükü <b>${tam(V.talepler.filter(x => x.onaylandi && x.odemeBekleyen > 0).reduce((s, x) => s + x.tutarTl / x.tutar * x.odemeBekleyen, 0))} TL</b></span><span>Fatura / belge <b>${esc(t.faturaDurumu)} · ${t.belgeTurleri.join(', ')}</b></span><span>Grup içi yansıtma <b>${t.grupIci ? 'Gerekli' : 'Yok'}</b></span></div>` : ''}
          </div>
        </div>

        ${t.yansitma ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-building me-1"></i>Muhasebe Yansıması — Grup İçi Avans</span>${yansRozet(t.yansitma.durum)}</div><div class="na-kart-g na-ustbilgi" style="grid-template-columns:repeat(4,1fr)">
          <div><div class="l">İşlem Tipi</div><div class="v">${esc(t.yansitma.islemTipi)}</div></div><div><div class="l">Ödemeyi Yapan Şirket</div><div class="v">${esc(t.yansitma.odeyenSirket)}</div></div><div><div class="l">Masrafın Ait Olduğu Şirket</div><div class="v">${esc(t.yansitma.masrafSirketi)}</div></div><div><div class="l">Avansı Kullanan</div><div class="v">${esc(t.yansitma.kullanan)}</div></div>
          <div><div class="l">Masraf Merkezi</div><div class="v">${esc(t.yansitma.masrafMerkezi)}</div></div><div><div class="l">Yansıtma Tutarı</div><div class="v">${para(t.yansitma.tutar, t.paraBirimi)}</div></div><div><div class="l">Yansıtma Belge No</div><div class="v">${esc(t.yansitma.belgeNo)} ${kilit}</div></div><div><div class="l">Yansıtma Durumu</div><div class="v">${yansRozet(t.yansitma.durum)}</div></div></div></div>` : ''}

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-vault me-1"></i>Finans Teslim Bilgileri</span><span class="not">Kasa / Finans hareketlerinden otomatik ${kilit}</span></div>
          <div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih / Saat</th><th class="s">Teslim Edilen</th><th>PB</th><th>Teslim Şekli</th><th>Kasa</th><th>Kasa Çıkış Fiş No</th><th>Teslim Eden</th><th>Teslim Alan</th><th>Durum</th><th>Teslim Teyidi (§12)</th></tr></thead><tbody>
            ${t.odemeler.map(o => `<tr class="${o.durum === 'İptal' ? 'text-muted text-decoration-line-through' : ''}"><td>${o.tarih}</td><td class="s fw-bold">${para(o.tutar)}</td><td>${o.paraBirimi}</td><td>${esc(o.teslimSekli)}</td><td>${esc(o.kasa)}</td><td>${esc(o.fisNo)}</td><td>${esc(o.teslimEden)}</td><td>${esc(o.teslimAlan)}</td><td>${o.durum === 'İptal' ? rozet('İptal', 'hata') : rozet('Teslim Edildi', 'ok')}</td><td>${o.durum === 'İptal' ? '—' : o.teslimAlanOnayi ? `<span class="text-success"><i class="fas fa-signature me-1"></i>Teyit edildi</span> <small class="text-muted">${o.teslimOnayTarihi}</small>` : `<button type="button" class="btn btn-outline-success btn-sm py-0" data-teyit="${o.islemId}"><i class="fas fa-signature me-1"></i>"${tam(o.tutar)} ${t.paraBirimi} nakit teslim aldım"</button>`}</td></tr>`).join('') || '<tr><td colspan="10" class="na-bos">Henüz kasa çıkışı yok.</td></tr>'}
            </tbody></table></div>
            <div class="na-ustbilgi p-2" style="grid-template-columns:repeat(4,1fr)"><div><div class="l">Onaylanan Tutar</div><div class="v">${para(t.onaylananTutar, t.paraBirimi)}</div></div><div><div class="l">Teslim Edilen Toplam</div><div class="v">${para(t.teslimEdilen, t.paraBirimi)}</div></div><div><div class="l">Ödeme Bekleyen</div><div class="v">${para(t.odemeBekleyen, t.paraBirimi)}</div></div><div><div class="l">Ödeme Durumu</div><div class="v">${odemeRozet(t.odemeDurumu)}</div></div></div>
          </div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-scale-balanced me-1"></i>Mahsup / Kapatma Özeti</span><span class="not">Açık Tutar = Teslim − Mahsup − İade ${kilit}</span></div>
          <div class="na-kart-g">
            <div class="na-formul">
              <div><div class="l">Alınan / Teslim</div><div class="v">${para(t.teslimEdilen)}</div></div>
              <div><span class="op">−</span><div class="l">Mahsup Edilen</div><div class="v">${para(t.mahsupEdilen)}</div>${t.kontrolBekleyen > 0 ? `<div class="na-mini">+ ${para(t.kontrolBekleyen)} kontrol bekliyor</div>` : ''}</div>
              <div><span class="op">−</span><div class="l">İade Edilen</div><div class="v">${para(t.iadeEdilen)}</div></div>
              <div class="acik ${t.acikTutar > 0 ? 'pozitif' : ''}"><span class="op">=</span><div class="l">Bekleyen / Açık</div><div class="v">${para(t.acikTutar)} ${t.paraBirimi}</div></div>
              <div><div class="l">Kapatma Durumu</div><div class="v">${kapatmaRozet(t.kapatmaDurumu)}</div></div>
            </div>
            <div class="na-ustbilgi mt-2" style="grid-template-columns:repeat(4,1fr)"><div><div class="l">Son İşlem Tarihi / Saati</div><div class="v">${t.sonIslem || '—'}</div></div><div><div class="l">Bekleyen Gün</div><div class="v">${t.bekleyenGun}</div></div><div><div class="l">Beklenen Kapatma</div><div class="v">${t.kapatmaTarihi}</div></div><div><div class="l">Fiili Kapatma</div><div class="v">${t.fiiliKapatma || '—'}</div></div></div>
            ${t.teslimEdilen > 0 && t.acikTutar === 0 ? '<div class="na-uyari mavi mt-2"><i class="fas fa-circle-check me-1"></i>Kapanış eşitliği sağlandı: Teslim Edilen = Mahsup Edilen + İade Edilen. Sistem avansı otomatik kapattı.</div>' : t.acikTutar > 0 ? `<div class="na-uyari sari mt-2"><i class="fas fa-hourglass-half me-1"></i>Açık bakiye sıfır değil; "Kapandı" yapılamaz. Belge (mahsup) ya da kasaya iade bekleniyor.</div>` : ''}
          </div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-receipt me-1"></i>Harcama Belge Satırları</span><span class="not">Muhasebe — Talep No referanslı kayıtlar ${kilit}</span></div>
          <div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Belge No</th><th>Tedarikçi</th><th>Belge Türü</th><th class="s">Tutar</th><th>Mahsup Durumu</th><th>Muhasebe Fiş No</th><th>Yevmiye No</th><th>Hesap Kodu</th></tr></thead><tbody>
            ${t.harcamalar.map(h => `<tr><td>${h.tarih}</td><td><b>${esc(h.belgeNo)}</b></td><td>${esc(h.tedarikci)}</td><td>${esc(h.belgeTuru)}</td><td class="s">${para(h.tutar)} ${t.paraBirimi}</td><td>${rozet(h.mahsupDurumu, h.mahsupDurumu === 'Mahsup Edildi' ? 'ok' : h.mahsupDurumu === 'Reddedildi' ? 'hata' : 'bekle')}</td><td>${esc(h.muhasebeFisNo || '—')}</td><td>${esc(h.yevmiyeNo || '—')}</td><td>${esc(h.hesapKodu || '—')}</td></tr>`).join('') || '<tr><td colspan="9" class="na-bos">Muhasebeye ulaşmış harcama belgesi yok.</td></tr>'}
            </tbody></table></div></div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-rotate-left me-1"></i>Nakit İadeler</span><span class="not">Kasa giriş hareketleri · birden fazla iade desteklenir ${kilit}</span></div>
          <div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>İade Tarihi / Saati</th><th class="s">İade Tutarı</th><th>Kasa</th><th>Kasa Giriş Fiş No</th><th>İadeyi Alan</th><th>İşlem ID</th></tr></thead><tbody>
            ${t.iadeler.map(i => `<tr><td>${i.tarih}</td><td class="s fw-bold">${para(i.tutar)} ${t.paraBirimi}</td><td>${esc(i.kasa)}</td><td>${esc(i.fisNo)}</td><td>${esc(i.iadeyiAlan)}</td><td><small class="text-muted">${esc(i.islemId)}</small></td></tr>`).join('') || '<tr><td colspan="6" class="na-bos">İade yok.</td></tr>'}
            </tbody></table></div></div>
        </div>
      </div>

      <div class="col-lg-5">
        <div class="na-kart na-olay">
          <div class="na-kart-b"><span><i class="fas fa-satellite-dish me-1"></i>Kaynak Sistem Olayı (simülasyon)</span><span class="not">§30 · Finans / Kasa / Muhasebe / ERP</span></div>
          <div class="na-kart-g">
            <div class="na-mini mb-1">Gerçek kurulumda bu olaylar Finans ve Muhasebe sistemlerinden Talep No ile otomatik gelir; ekranda tutar elle girilmez. Burada entegrasyonu test etmek için olay üretilir; her olay benzersiz Transaction ID taşır, aynı ID ikinci kez gelirse yok sayılır (idempotent).</div>
            <div class="row g-1 align-items-end">
              <div class="col-7"><label class="form-label">Olay</label><select class="form-select" id="naOlayTur">
                <option value="CashPaymentCreated">CashPaymentCreated — kasa çıkışı (ödeme / kısmi ödeme)</option>
                <option value="ExpensePosted">ExpensePosted — harcama belgesi kaydı</option>
                <option value="JournalPosted">JournalPosted — muhasebe fişi (mahsup)</option>
                <option value="CashReturned">CashReturned — kasaya nakit iade</option>
                <option value="CashPaymentReversed">CashPaymentReversed — ödeme ters kaydı</option>
                <option value="ExpenseReversed">ExpenseReversed — belge iptali</option>
                <option value="CashReturnReversed">CashReturnReversed — iade geri alma</option>
                <option value="IntercompanyPosted">IntercompanyPosted — grup içi yansıtma</option></select></div>
              <div class="col-5"><label class="form-label">Transaction ID</label><input class="form-control" id="naOlayId" value="${'TX-' + Date.now().toString().slice(-6)}"></div>
              <div class="col-4" data-alan="tutar"><label class="form-label">Tutar (${t.paraBirimi})</label><input class="form-control" type="number" step="0.01" id="naOlayTutar" value="${t.odemeBekleyen > 0 ? t.odemeBekleyen : t.acikTutar > 0 ? t.acikTutar : ''}"></div>
              <div class="col-8" data-alan="kasa"><label class="form-label">Kasa</label><select class="form-select" id="naOlayKasa">${V.kasalar.map(k => `<option>${esc(k)}</option>`).join('')}</select></div>
              <div class="col-4" data-alan="belgeNo" style="display:none"><label class="form-label">Belge No</label><input class="form-control" id="naOlayBelgeNo" placeholder="FTR-125"></div>
              <div class="col-4" data-alan="tedarikci" style="display:none"><label class="form-label">Tedarikçi</label><input class="form-control" id="naOlayTedarikci" placeholder="ABC Ltd."></div>
              <div class="col-4" data-alan="belgeTuru" style="display:none"><label class="form-label">Belge Türü</label><select class="form-select" id="naOlayBelgeTuru"><option>Fatura</option><option>E-Fatura</option><option>E-Arşiv Fatura</option><option>Perakende Fiş</option><option>Makbuz</option><option>Resmî Kurum Belgesi</option></select></div>
              <div class="col-8" data-alan="ref" style="display:none"><label class="form-label">Ters kaydı yapılacak işlem</label><select class="form-select" id="naOlayRef"></select></div>
              <div class="col-12 d-flex gap-1 mt-1"><button type="button" class="btn btn-warning btn-sm" id="naOlayGonder"><i class="fas fa-bolt me-1"></i>Olayı İşle</button><button type="button" class="btn btn-light btn-sm" id="naOlayTekrar" title="Aynı Transaction ID ile tekrar gönderir; mükerrer koruma testi (T10)">Aynı ID ile tekrar gönder</button></div>
            </div>
          </div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-timeline me-1"></i>Finans Hareket Geçmişi / Timeline</span><span class="not">${t.zaman.length} hareket</span></div>
          <div class="na-kart-g"><ul class="na-zaman">${t.zaman.slice().reverse().map(z => `<li class="${/Finans|Kasa/.test(z.kaynak) ? 'fin' : /Muhasebe|ERP/.test(z.kaynak) ? 'muh' : z.kaynak === 'Sistem' ? 'sis' : 'wf'}"><span class="t">${z.tarih}</span>${esc(z.hareket)} <span class="k">· ${esc(z.kaynak)}</span></li>`).join('')}</ul></div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-paperclip me-1"></i>Belge ve Ekler</span><span class="not">${t.ekler.length} dosya</span></div>
          <div class="na-kart-g">${t.ekler.map(e => `<div class="na-ek"><i class="fas ${/pdf/.test(e.ad) ? 'fa-file-pdf' : /xls/.test(e.ad) ? 'fa-file-excel' : /png|jpg/.test(e.ad) ? 'fa-file-image' : 'fa-file'}"></i><span>${esc(e.ad)}</span><span class="na-bs" style="background:#f1f5f9;color:#475569">${esc(e.tur)}</span><span class="m">${esc(e.boyut)} · ${e.tarih} · ${esc(e.yukleyen)}</span></div>`).join('') || '<div class="na-bos">Ek dosya yok (teklif / proforma / liste / görsel eklenebilir).</div>'}
            <div class="mt-2 d-flex gap-1"><input type="file" class="form-control form-control-sm" id="naEkDosya" multiple><button type="button" class="btn btn-light btn-sm" id="naEkYukle">Ekle</button></div></div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-shield-halved me-1"></i>Audit Log / Değişiklik Geçmişi</span><span class="not">soft delete · ${t.audit.length} kayıt</span></div>
          <div class="na-kart-g p-0"><div class="table-responsive" style="max-height:260px"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Saat</th><th>Tablo</th><th>Alan</th><th>Eski</th><th>Yeni</th><th>Kullanıcı</th><th>İşlem</th><th>Kaynak</th></tr></thead><tbody>
            ${t.audit.map(x => `<tr><td>${x.tarih}</td><td>${x.saat}</td><td><small>${esc(x.tablo)}</small></td><td><small>${esc(x.alan)}</small></td><td>${esc(x.eski)}</td><td>${esc(x.yeni)}</td><td>${esc(x.kullanici)}</td><td>${esc(x.islemTipi)}</td><td>${esc(x.kaynakSistem)}</td></tr>`).join('')}
            </tbody></table></div></div>
        </div>
      </div>
    </div>`;

    // ---- detay olayları ----
    $$('[data-ac]', el).forEach(a => a.addEventListener('click', e => { e.preventDefault(); detayAc(a.dataset.ac); }));
    $('#naYazdir').addEventListener('click', () => window.print());
    $$('[data-onay]', el).forEach(b => b.addEventListener('click', async () => {
      const not = $('#naOnayNot').value.trim();
      if (b.dataset.onay !== 'Onayla' && !not) { mesaj('danger', b.dataset.onay === 'Reddet' ? 'Red gerekçesi zorunludur.' : 'Revizyon açıklaması zorunludur.'); $('#naOnayNot').focus(); return; }
      const j = await post('/IkTalep/AvansOnay', { no: t.no, eylem: b.dataset.onay, not });
      if (j.talep) guncelle(j.talep); mesaj(j.success ? 'success' : 'danger', esc(j.message || (b.dataset.onay + ' işlendi.')));
    }));
    const rev = $('#naRevizeAc'); if (rev) rev.addEventListener('click', () => { $('#naRevizeForm').style.display = 'block'; });
    const revI = $('#naRevIptal'); if (revI) revI.addEventListener('click', () => { $('#naRevizeForm').style.display = 'none'; });
    const revG = $('#naRevGonder'); if (revG) revG.addEventListener('click', async () => {
      const j = await post('/IkTalep/AvansRevize', { no: t.no, tutar: $('#naRevTutar').value, konu: $('#naRevKonu').value });
      if (j.talep) guncelle(j.talep); mesaj(j.success ? 'success' : 'danger', esc(j.message || 'Talep revize edildi; onay akışı yeniden başlatıldı.'));
    });
    $$('[data-teyit]', el).forEach(b => b.addEventListener('click', async () => {
      const j = await post('/IkTalep/AvansTeslimTeyidi', { no: t.no, islemId: b.dataset.teyit });
      if (j.talep) guncelle(j.talep); mesaj(j.success ? 'success' : 'danger', esc(j.message || 'Teslim teyidi kaydedildi (kullanıcı, tarih/saat, oturum).'));
    }));
    $('#naEkYukle').addEventListener('click', () => {
      const f = Array.from($('#naEkDosya').files || []); if (!f.length) return;
      f.forEach(x => t.ekler.push({ ad: x.name, tur: /pdf/.test(x.name) ? 'Teklif' : /xls/.test(x.name) ? 'Liste' : /png|jpg/.test(x.name) ? 'Görsel' : 'Belge', boyut: Math.round(x.size / 1024) + ' KB', tarih: new Date().toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' }), yukleyen: V.ben.adSoyad }));
      cizDetay(); mesaj('success', f.length + ' dosya eklendi (örnek: yalnızca bu oturumda tutulur).');
    });
    // olay simülasyonu alanları
    const olayTur = $('#naOlayTur');
    function olayAlanlari() {
      const o = olayTur.value, g = (alan, ac) => { const e = $(`[data-alan=${alan}]`, el); if (e) e.style.display = ac ? '' : 'none'; };
      g('tutar', /CashPaymentCreated|ExpensePosted|CashReturned/.test(o)); g('kasa', /CashPaymentCreated|CashReturned/.test(o));
      g('belgeNo', o === 'ExpensePosted'); g('tedarikci', o === 'ExpensePosted'); g('belgeTuru', o === 'ExpensePosted');
      const ref = /Reversed/.test(o); g('ref', ref);
      if (ref) { const l = o === 'CashPaymentReversed' ? t.odemeler.filter(x => x.durum !== 'İptal').map(x => [x.islemId, x.tarih + ' · ' + para(x.tutar)]) : o === 'ExpenseReversed' ? t.harcamalar.map(x => [x.belgeId, x.belgeNo + ' · ' + para(x.tutar)]) : t.iadeler.map(x => [x.islemId, x.tarih + ' · ' + para(x.tutar)]); $('#naOlayRef').innerHTML = l.map(x => `<option value="${esc(x[0])}">${esc(x[1])}</option>`).join('') || '<option value="">Kayıt yok</option>'; }
      $('#naOlayTutar').value = o === 'CashPaymentCreated' ? t.odemeBekleyen : o === 'CashReturned' ? t.acikTutar : o === 'ExpensePosted' ? Math.min(t.acikTutar, 3500) : '';
    }
    olayTur.addEventListener('change', olayAlanlari); olayAlanlari();
    async function olayGonder(ayniId) {
      const o = olayTur.value;
      const veri = { no: t.no, olay: o, islemId: $('#naOlayId').value.trim(), tutar: $('#naOlayTutar').value, ek1: /Reversed/.test(o) ? $('#naOlayRef').value : o === 'ExpensePosted' ? $('#naOlayBelgeNo').value : $('#naOlayKasa').value, ek2: o === 'ExpensePosted' ? $('#naOlayTedarikci').value : 'Nakit', ek3: o === 'ExpensePosted' ? $('#naOlayBelgeTuru').value : '' };
      const j = await post('/IkTalep/AvansOlay', veri);
      if (j.talep) guncelle(j.talep);
      mesaj(j.success ? 'success' : 'warning', j.success ? `<i class="fas fa-bolt me-1"></i>${o} işlendi; tutarlar ve statüler yeniden hesaplandı.` : esc(j.message));
      if (j.success && !ayniId) { $('#naOlayId').value = 'TX-' + Date.now().toString().slice(-6); }
    }
    $('#naOlayGonder').addEventListener('click', () => olayGonder(false));
    $('#naOlayTekrar').addEventListener('click', () => olayGonder(true));
  }

  function cizHepsi() { cizKpi(); cizListe(); cizBekleyen(); takipFiltreleri(); cizTakip(); cizDetay(); kullananDegisti(); }

  // ---- başlangıç ----
  fetch('/IkTalep/AvansVeri').then(r => r.json()).then(v => {
    V = v; formHazirla(); cizKpi(); cizListe(); cizBekleyen(); takipFiltreleri(); cizTakip();
    const h = decodeURIComponent(location.hash.slice(1)); let p = kok.dataset.sekme || 'yeni';
    if (h) { const [hp, hn] = h.split('/'); p = hp; if (hn) aktifNo = hn; }
    if (!aktifNo && p === 'detay') { const ilk = V.talepler.find(benimMi); if (ilk) aktifNo = ilk.no; }
    cizDetay(); sekme(p); window.NA_HAZIR = true;
  }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
