// ============================================================
// İZİN TALEP, ONAY, KULLANIM VE PUANTAJ MODÜLÜ — ekran davranışı
// Veri: /IkTalep/IzinVeri. Bakiye, iş günü, statüler sunucuda hesaplanır (Data/IzinOrnek.cs); bu dosya çizer, form gönderir,
// önizleme ister (/IkTalep/IzinOnizleme) ve PDKS / SGK / Bordro'dan gelecek kaynak sistem olaylarını simüle eder.
// ============================================================
(function () {
  'use strict';
  const kok = document.getElementById('iz'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const gun = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 1 });
  const bugunIso = new Date().toISOString().slice(0, 10);
  const iso2tr = s => s ? s.split('-').reverse().join('.') : '';
  let V = null, aktifNo = kok.dataset.no || '', onizlemeZ = null;
  const RENK = { YILLIK: '#0ea5e9', MAZERET: '#f59e0b', HASTALIK: '#ef4444', UCRETSIZ: '#64748b', DOGUM: '#ec4899', BABALIK: '#8b5cf6', EVLILIK: '#10b981', VEFAT: '#334155', SUT: '#f472b6', IDARI: '#0891b2' };

  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const talepRozet = d => rozet(d, d === 'Onaylandı' ? 'ok' : d === 'Reddedildi' || d === 'İptal Edildi' ? 'hata' : /Revizyon/.test(d) ? 'bilgi' : 'bekle');
  const kullanimRozet = d => rozet(d, d === 'Tamamlandı' ? 'ok' : d === 'İptal' ? 'hata' : /İzinde/.test(d) ? 'bilgi' : d === 'Planlanmadı' ? 'bilgi' : 'bekle');
  const puantajRozet = d => d === '—' ? '<span class="text-muted">—</span>' : rozet(d, /Aktarıldı|İşlendi/.test(d) ? 'ok' : d === 'Belge Bekleniyor' ? 'hata' : 'bekle');
  const turRozet = t => `<span class="na-bs" style="background:${RENK[t.izinTuru] || '#94a3b8'}22;color:${RENK[t.izinTuru] || '#475569'}">${esc(t.turAdi)}</span>`;
  const kilit = '<i class="fas fa-lock na-kilit" title="Kaynak: PDKS / SGK / Bordro — talep ekranından değiştirilemez"></i>';

  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 9000); }
  async function post(url, veri) { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }); return r.json(); }
  function guncelle(j) {
    if (j.talep) { const i = V.talepler.findIndex(x => x.no === j.talep.no); if (i >= 0) V.talepler[i] = j.talep; else V.talepler.unshift(j.talep); }
    if (j.bakiye) { const i = V.bakiyeler.findIndex(b => b.personelNo === j.bakiye.personelNo); if (i >= 0) V.bakiyeler[i] = j.bakiye; }
    cizHepsi();
  }
  const benimMi = t => t.olusturan.kullaniciId === V.ben.kullaniciId || t.kullanan.kullaniciId === V.ben.kullaniciId || (t.vekil && t.vekil.kullaniciId === V.ben.kullaniciId);
  const bakiyeBul = pn => V.bakiyeler.find(b => b.personelNo === pn);
  const kisi = pn => V.personel.find(p => p.personelNo === pn);

  function sekme(p) {
    $$('#izSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('.na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    try { history.replaceState(null, '', location.pathname + location.search + '#' + p + (p === 'detay' && aktifNo ? '/' + aktifNo : '')); } catch (e) { }
  }
  $$('#izSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));

  // ---- gösterge kutucukları ----
  function cizKpi() {
    const T = V.talepler, hafta = new Date(); hafta.setDate(hafta.getDate() + 7); const haftaIso = hafta.toISOString().slice(0, 10);
    const izinde = T.filter(t => t.izinde), baslayan = T.filter(t => t.onaylandi && !t.iptal && t.baslangicIso > bugunIso && t.baslangicIso <= haftaIso),
      onay = T.filter(t => /Bekliyor$/.test(t.talepDurumu) && !/Revizyon/.test(t.talepDurumu)), belge = T.filter(t => t.belgeEksik),
      donus = T.filter(t => t.kullanimDurumu === 'Dönüş Teyidi Bekliyor'), puantaj = T.filter(t => t.puantajDurumu === 'Puantaj Bekliyor');
    const kut = [
      ['#0ea5e9', 'Bugün İzinde', `${izinde.length} Kişi`, izinde.map(t => t.kullanan.adSoyad.split(' ')[0]).join(', ') || '—', 'takip:izinde'],
      ['#7c3aed', 'Bu Hafta Başlayan', `${baslayan.length} İzin`, baslayan.map(t => t.kullanan.adSoyad.split(' ')[0] + ' ' + t.baslangic.slice(0, 5)).join(', ') || '—', 'takvim'],
      ['#f59e0b', 'Onay Bekleyen', `${onay.length} Talep`, `${gun(onay.reduce((s, t) => s + t.isGunu, 0))} iş günü`, 'onay'],
      ['#ef4444', 'Belge Bekleyen', `${belge.length} Talep`, belge.map(t => t.kullanan.adSoyad.split(' ')[0] + ' (' + t.turAdi.split(' ')[0] + ')').join(', ') || '—', 'takip:belge'],
      ['#16a34a', 'Dönüş Teyidi Bekleyen', `${donus.length} Kişi`, donus.map(t => t.kullanan.adSoyad.split(' ')[0]).join(', ') || '—', 'takip:donus'],
      ['#0891b2', 'Puantaj Bekleyen', `${puantaj.length} Talep`, `${gun(puantaj.reduce((s, t) => s + t.kullanilanGun, 0))} gün`, 'takip:puantaj']
    ];
    $('#izKpi').innerHTML = kut.map(k => `<div style="--k:${k[0]}" data-git="${k[4]}"><div class="l">${k[1]}</div><div class="v">${k[2]}</div><div class="s" title="${esc(k[3])}">${esc(k[3])}</div></div>`).join('');
    $$('#izKpi > div').forEach(d => d.addEventListener('click', () => {
      const [p, f] = d.dataset.git.split(':');
      if (p === 'takip') { $('#izTakipAra').value = ''; takipOzel = f === 'izinde' ? t => t.izinde : f === 'belge' ? t => t.belgeEksik : f === 'donus' ? t => t.kullanimDurumu === 'Dönüş Teyidi Bekliyor' : f === 'puantaj' ? t => t.puantajDurumu === 'Puantaj Bekliyor' : null; cizTakip(); }
      sekme(p);
    }));
    $('#izNListe').textContent = T.filter(benimMi).length; $('#izNOnay').textContent = onay.length; $('#izNTakip').textContent = T.length;
  }

  // ---- bakiye kartı ----
  function bakiyeHtml(b, k, kucuk) {
    if (!b) return '<div class="na-bos">Bakiye bulunamadı.</div>';
    const toplam = b.hakEdis + b.devir, kritik = b.kalan < 3;
    return `<div class="iz-bakiye">
      <div><div class="l">Hak Ediş</div><div class="v">${gun(b.hakEdis)}</div></div><div><div class="l">Devir</div><div class="v">${gun(b.devir)}</div></div><div><div class="l">Kıdem</div><div class="v">${b.kidemYil} yıl</div></div>
      <div><div class="l">Kullanılan</div><div class="v">${gun(b.kullanilan)}</div></div><div><div class="l">Planlanan</div><div class="v">${gun(b.planlanan)}</div></div><div><div class="l">Onay Bekleyen</div><div class="v">${gun(b.onayBekleyen)}</div></div>
      <div class="kalan ${kritik ? 'kritik' : ''}"><div class="l">Kalan Yıllık İzin</div><div class="v">${gun(b.kalan)} gün</div><div class="na-mini">net kullanılabilir ${gun(b.kullanilabilirNet)} gün · sonraki hak ediş ${b.sonrakiHakEdis} (+${gun(b.sonrakiHakEdisGun)})</div>
      <div class="iz-cubuk" title="kullanılan / planlanan / kalan"><span style="width:${toplam ? b.kullanilan / toplam * 100 : 0}%;background:#0369a1"></span><span style="width:${toplam ? b.planlanan / toplam * 100 : 0}%;background:#7dd3fc"></span></div></div>
    </div>
    <div class="na-mini">İşe giriş ${b.iseGiris} · mazeret ${gun(b.mazeretKullanilan)}/3 gün · raporlu ${gun(b.raporluGun)} gün · devir üst sınırı ${V.devirUstSinir} gün</div>` +
      (kucuk ? '' : `<table class="na-tablo mt-2" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Hareket</th><th class="s">Gün</th><th>Ref</th></tr></thead><tbody>${b.hareketler.map(h => `<tr><td>${h.tarih}</td><td>${esc(h.tur)} <small class="text-muted">${esc(h.aciklama)}</small></td><td class="s ${h.gun < 0 ? 'text-danger' : 'text-success'}">${h.gun > 0 ? '+' : ''}${gun(h.gun)}</td><td><small>${esc(h.ref)}</small></td></tr>`).join('')}</tbody></table>`);
  }

  // ---- yeni talep formu ----
  const kisiOto = (k, baslik) => `<div class="h">${baslik}</div>` + (!k ? '<span class="text-muted">Kişi seçilmedi</span>' : `<span>Personel No <b>${esc(k.personelNo)}</b></span><span>Ad Soyad <b>${esc(k.adSoyad)}</b></span><span>Şirket <b>${esc(k.sirket)}</b></span><span>Departman <b>${esc(k.departman)}</b></span><span>Ünvan <b>${esc(k.unvan)}</b></span><span>Yönetici <b>${esc((V.personel.find(p => p.rol === 'Yönetici' && p.departman === k.departman) || {}).adSoyad || '—')}</b></span><span>Aktif <b>${k.aktif ? 'Evet' : '<span class="text-danger">Hayır</span>'}</b></span><span>Kullanıcı ID <b>${esc(k.kullaniciId)}</b></span>`);
  function formHazirla() {
    const kOpt = (sel, bos) => (bos ? `<option value="">${bos}</option>` : '') + V.personel.map(p => `<option value="${esc(p.personelNo)}" ${p.personelNo === sel ? 'selected' : ''}>${esc(p.adSoyad)} — ${esc(p.sirket)} / ${esc(p.departman)}${p.aktif ? '' : ' (pasif)'}</option>`).join('');
    $('#izKullanan').innerHTML = kOpt(V.ben.personelNo); $('#izVekil').innerHTML = kOpt('', 'Seçiniz…');
    $('#izTur').innerHTML = V.turler.map(t => `<option value="${t.kod}">${esc(t.ad)}</option>`).join('');
    $('#izYarim').innerHTML = V.yarimGun.map(x => `<option>${esc(x)}</option>`).join('');
    $('#izOlusturan').innerHTML = kisiOto(V.ben, "Talebi oluşturan (İK'dan otomatik)");
    const n = new Date(); $('#izTarih').value = n.toLocaleDateString('tr-TR') + ' ' + n.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
    const b = new Date(); b.setDate(b.getDate() + 21); while (b.getDay() === 0 || b.getDay() === 6) b.setDate(b.getDate() + 1);
    $('#izBas').value = b.toISOString().slice(0, 10); const e = new Date(b); e.setDate(e.getDate() + 4); $('#izBit').value = e.toISOString().slice(0, 10);
    $('#izPolitika').value = V.politika; $('#izDoluluk').value = V.dolulukEsigi; $('#izUzun').value = V.uzunIzinEsigi;
    kullananDegisti(); turDegisti();
  }
  function kullananDegisti() {
    if (!V) return;
    const k = kisi($('#izKullanan').value);
    $('#izKullananBilgi').innerHTML = kisiOto(k, "Seçilen personel (İK'dan otomatik)");
    $('#izBakiyeKisi').textContent = k ? k.adSoyad : 'canlı hesap';
    $('#izBakiye').innerHTML = bakiyeHtml(bakiyeBul(k && k.personelNo), k, false);
    const u = $('#izKullananUyari'); u.className = 'na-uyari';
    if (k && !k.aktif) { u.className = 'na-uyari kirmizi'; u.innerHTML = '<i class="fas fa-ban me-1"></i>Personel aktif değil: izin açılamaz.'; }
    onizleme();
  }
  function turDegisti() {
    if (!V) return;
    const t = V.turler.find(x => x.kod === $('#izTur').value); if (!t) return;
    $('#izTurBilgi').innerHTML = `${esc(t.dayanak)} · ${t.ucretli ? 'ücretli' : '<b class="text-danger">ücretsiz</b>'}${t.bakiyedenDuser ? ' · bakiyeden düşer' : ''}${t.maxGun ? ' · en fazla ' + t.maxGun + ' gün' : ''}${t.enAzOnceBildirim ? ' · en az ' + t.enAzOnceBildirim + ' gün önce' : ''}<br>${esc(t.aciklama)}`;
    $('#izAciklamaZ').style.display = t.kod === 'MAZERET' || t.kod === 'UCRETSIZ' ? '' : 'none';
    $('#izBelgeNot').innerHTML = t.belgeGerekli ? '<b class="text-danger">belge zorunlu (rapor / evrak)</b>' : '(isteğe bağlı)';
    onizleme();
  }
  function onizleme() {
    clearTimeout(onizlemeZ);
    onizlemeZ = setTimeout(async () => {
      if (!V || !$('#izBas').value || !$('#izBit').value) return;
      const j = await post('/IkTalep/IzinOnizleme', { kullanan: $('#izKullanan').value, izinTuru: $('#izTur').value, baslangic: $('#izBas').value, bitis: $('#izBit').value, yarimGun: $('#izYarim').value, vekil: $('#izVekil').value });
      if (!j.success) { $('#izKurallar').innerHTML = `<span class="text-danger">${esc(j.message)}</span>`; return; }
      $('#izHesap').innerHTML = `<div class="h">Sistem hesabı (hafta sonu ve resmi tatil sayılmaz)</div><span>Takvim günü <b>${j.takvimGunu}</b></span><span>İş günü <b>${gun(j.isGunu)}</b></span><span>Hafta sonu <b>${j.haftaSonu}</b>${j.tatiller.length ? ' · tatil: ' + j.tatiller.map(esc).join(', ') : ''}</span><span>İşe dönüş <b>${esc(j.donus)}</b></span>`;
      $('#izAkis').innerHTML = j.akis.map((a, i) => (i ? '<i class="fas fa-chevron-right"></i>' : '') + `<span title="${esc(a.kisi)}">${esc(a.adim.replace('Talep Oluşturma', 'Talep'))}</span>`).join('');
      const kur = [];
      if (j.cakisma.length) kur.push(`<li class="text-danger">Çakışan izin: ${j.cakisma.join(', ')}</li>`);
      j.uyarilar.forEach(u => kur.push(`<li>${esc(u)}</li>`));
      $('#izKurallar').innerHTML = kur.length ? `<ul class="iz-uyari-liste">${kur.join('')}</ul>` : '<span class="text-success"><i class="fas fa-circle-check me-1"></i>Kural ihlali yok</span>';
      $('#izEkip').innerHTML = `<div class="na-mini mb-1">Departman mevcudu ${j.mevcut} · aynı tarihlerde izinli ${j.departmanIzinli.length} kişi (eşik %${Math.round(V.dolulukEsigi * 100)})</div>` + (j.departmanIzinli.length ? j.departmanIzinli.map(d => `<div class="na-ek"><i class="fas fa-user-clock"></i><span>${esc(d.adSoyad)}</span><span class="na-bs" style="background:#f1f5f9;color:#475569">${esc(d.tur)}</span><span class="m">${d.baslangic} – ${d.bitis}</span></div>`).join('') : '<div class="na-bos">Bu tarihlerde departmanda başka izinli yok.</div>');
      $('#izYarim').disabled = $('#izBas').value !== $('#izBit').value; if ($('#izYarim').disabled) $('#izYarim').value = 'Yok';
    }, 250);
  }
  $('#izKullanan').addEventListener('change', kullananDegisti);
  $('#izTur').addEventListener('change', turDegisti);
  ['#izBas', '#izBit', '#izYarim', '#izVekil'].forEach(s => $(s).addEventListener('change', onizleme));
  $('#izParamKaydet').addEventListener('click', async () => { const j = await post('/IkTalep/IzinParametre', { politika: $('#izPolitika').value, doluluk: $('#izDoluluk').value, uzun: $('#izUzun').value }); V.politika = j.politika; V.dolulukEsigi = j.dolulukEsigi; V.uzunIzinEsigi = j.uzunIzinEsigi; onizleme(); mesaj('success', 'Parametreler güncellendi.'); });
  $('#izForm').addEventListener('reset', () => setTimeout(formHazirla, 0));
  $('#izForm').addEventListener('submit', async e => {
    e.preventDefault(); if (!V) return;
    const f = e.target, d = {}; new FormData(f).forEach((v, k) => d[k] = v);
    d.ekler = Array.from($('#izEk').files || []).map(x => x.name).join('|');
    const eksik = $$('[required]', f).filter(x => !String(x.value).trim());
    if (eksik.length) { eksik.forEach(x => x.classList.add('is-invalid')); mesaj('danger', 'Zorunlu alanları doldurun.'); eksik[0].focus(); return; }
    if (d.bitis < d.baslangic) { mesaj('danger', 'Bitiş tarihi başlangıçtan önce olamaz.'); return; }
    $('#izGonder').disabled = true;
    try {
      const j = await post('/IkTalep/IzinOlustur', d);
      if (!j.success) { mesaj('danger', esc(j.message)); return; }
      mesaj('success', '<i class="fas fa-circle-check me-1"></i>' + esc(j.message));
      const r = await fetch('/IkTalep/IzinVeri'); V = await r.json(); f.reset(); detayAc(j.no);
    } finally { $('#izGonder').disabled = false; }
  });
  $('#izForm').addEventListener('input', e => e.target.classList.remove('is-invalid'));

  // ---- taleplerim ----
  function cizListe() {
    const l = V.talepler.filter(benimMi);
    $('#izListe tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${t.no}"><td><b>${t.no}</b>${t.revizyon ? ' <small class="text-muted">rev.' + t.revizyon + '</small>' : ''}</td><td>${esc(t.kullanan.adSoyad)}</td><td>${turRozet(t)}</td><td>${t.baslangic}</td><td>${t.bitis}</td><td class="s">${gun(t.isGunu)}${t.yarimGun !== 'Yok' ? ' <small>(½)</small>' : ''}</td><td>${esc(t.vekil ? t.vekil.adSoyad : '—')}</td><td>${talepRozet(t.talepDurumu)}</td><td>${kullanimRozet(t.kullanimDurumu)}</td><td>${puantajRozet(t.puantajDurumu)}</td></tr>`).join('') || '<tr><td colspan="10" class="na-bos">Talebiniz yok.</td></tr>';
    $$('#izListe tr.tik').forEach(r => r.addEventListener('click', () => detayAc(r.dataset.no)));
  }

  // ---- onay bekleyenler ----
  function cizOnay() {
    const l = V.talepler.filter(t => t.onaylar.some(o => o.durum === 'Onay Bekliyor')).sort((a, b) => a.baslangicIso.localeCompare(b.baslangicIso));
    $('#izOnayListe tbody').innerHTML = l.map(t => { const a = t.onaylar.find(o => o.durum === 'Onay Bekliyor'); return `<tr class="tik iz-onay-satir" data-no="${t.no}"><td><b>${t.no}</b></td><td>${esc(t.kullanan.adSoyad)}</td><td>${esc(t.departman)}</td><td>${turRozet(t)}</td><td>${t.baslangic} – ${t.bitis}</td><td class="s">${gun(t.isGunu)}</td><td>${esc(a.adim)}</td><td>${esc(a.kisi)}</td><td class="s">${gunFarki(t.tarih)} g</td><td>${t.uyarilar.length ? `<span class="text-warning" title="${esc(t.uyarilar.join(' '))}"><i class="fas fa-triangle-exclamation"></i> ${t.uyarilar.length}</span>` : '<span class="text-success"><i class="fas fa-check"></i></span>'}</td><td><button type="button" class="btn btn-success btn-sm py-0" data-hizli="Onayla" data-no="${t.no}"><i class="fas fa-check"></i></button> <button type="button" class="btn btn-outline-danger btn-sm py-0" data-hizli="Reddet" data-no="${t.no}"><i class="fas fa-xmark"></i></button></td></tr>`; }).join('') || '<tr><td colspan="11" class="na-bos">Onay bekleyen talep yok.</td></tr>';
    $$('#izOnayListe tr.tik').forEach(r => r.addEventListener('click', e => { if (!e.target.closest('button')) detayAc(r.dataset.no); }));
    $$('#izOnayListe [data-hizli]').forEach(b => b.addEventListener('click', async () => {
      let not = ''; if (b.dataset.hizli === 'Reddet') { not = prompt('Red gerekçesi (zorunlu):') || ''; if (!not.trim()) return; }
      const j = await post('/IkTalep/IzinOnayla', { no: b.dataset.no, eylem: b.dataset.hizli, not }); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message || (b.dataset.no + ' ' + b.dataset.hizli.toLowerCase() + 'ndı.')));
    }));
  }
  const gunFarki = ddmmyyyy => { const [d, m, y] = ddmmyyyy.split('.'); return Math.max(0, Math.round((Date.now() - new Date(+y, m - 1, +d)) / 864e5)); };

  // ---- takip raporu ----
  let takipOzel = null;
  function cizTakip() {
    const ara = ($('#izTakipAra').value || '').toLocaleLowerCase('tr-TR'), tf = $('#izTakipTur').value, df = $('#izTakipDurum').value, kf = $('#izTakipKullanim').value;
    const l = V.talepler.filter(t => (!ara || [t.no, t.kullanan.adSoyad, t.olusturan.adSoyad, t.turAdi, t.departman].join(' ').toLocaleLowerCase('tr-TR').includes(ara)) && (!tf || t.izinTuru === tf) && (!df || t.talepDurumu === df) && (!kf || t.kullanimDurumu === kf) && (!takipOzel || takipOzel(t)));
    $('#izTakip tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${t.no}"><td><b>${t.no}</b></td><td>${esc(t.sirket)}</td><td>${esc(t.departman)}</td><td>${esc(t.kullanan.adSoyad)}</td><td>${esc(t.olusturan.adSoyad)}</td><td>${esc(t.turAdi)}</td><td>${t.tarih}</td><td>${t.baslangic}</td><td>${t.bitis}</td><td class="s">${t.takvimGunu}</td><td class="s">${gun(t.isGunu)}</td><td class="s">${gun(t.kullanilanGun)}</td><td>${t.ucretli ? 'Evet' : 'Hayır'}</td><td>${esc(t.vekil ? t.vekil.adSoyad : '')}</td><td>${talepRozet(t.talepDurumu)}</td><td>${kullanimRozet(t.kullanimDurumu)}</td><td>${puantajRozet(t.puantajDurumu)}</td><td>${t.fiiliDonus || ''}</td><td>${t.donusTeyidi ? 'Evet' : (t.onaylandi && t.gecmis ? 'Hayır' : '')}</td><td>${t.belgeGerekli ? (t.belgeAlindi ? 'Alındı' : '<span class="text-danger">Eksik</span>') : '—'}</td><td>${esc(t.yonetici || '')}</td><td>${esc(t.direktor || '—')}</td><td>${esc(t.ik || '—')}</td><td>${esc(t.puantajDonemi || '')}</td></tr>`).join('') || '<tr><td colspan="24" class="na-bos">Kayıt yok.</td></tr>';
    $('#izTakipSayi').textContent = `${l.length} / ${V.talepler.length} kayıt`;
    $$('#izTakip tr.tik').forEach(r => r.addEventListener('click', () => detayAc(r.dataset.no)));
  }
  ['#izTakipAra', '#izTakipTur', '#izTakipDurum', '#izTakipKullanim'].forEach(s => $(s).addEventListener('input', () => { takipOzel = null; cizTakip(); }));
  function takipFiltreleri() {
    const uniq = f => V.talepler.map(f).filter((v, i, a) => a.indexOf(v) === i);
    $('#izTakipTur').innerHTML = '<option value="">Tür: tümü</option>' + V.turler.map(t => `<option value="${t.kod}">${esc(t.ad)}</option>`).join('');
    $('#izTakipDurum').innerHTML = '<option value="">Talep durumu: tümü</option>' + uniq(t => t.talepDurumu).map(x => `<option>${esc(x)}</option>`).join('');
    $('#izTakipKullanim').innerHTML = '<option value="">Kullanım: tümü</option>' + uniq(t => t.kullanimDurumu).map(x => `<option>${esc(x)}</option>`).join('');
  }

  // ---- ekip takvimi ----
  let takvimAy = new Date(new Date().getFullYear(), new Date().getMonth(), 1);
  function cizTakvim() {
    const dep = $('#izTakvimDep').value, y = takvimAy.getFullYear(), m = takvimAy.getMonth(), gunSayisi = new Date(y, m + 1, 0).getDate();
    $('#izAyAd').textContent = takvimAy.toLocaleDateString('tr-TR', { month: 'long', year: 'numeric' });
    const tatil = {}; V.tatiller.forEach(t => tatil[t.tarih] = t.ad);
    const kisiler = V.personel.filter(p => p.aktif && (!dep || p.departman === dep));
    const gunler = Array.from({ length: gunSayisi }, (_, i) => new Date(y, m, i + 1));
    const iso = d => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    let h = '<thead><tr><th style="width:150px;text-align:left;padding-left:6px">Personel</th>' + gunler.map(d => { const s = iso(d), hs = d.getDay() === 0 || d.getDay() === 6, tt = tatil[s] && !/yarım/.test(tatil[s]); return `<th class="${s === bugunIso ? 'bugun' : tt ? 'tt' : hs ? 'hs' : ''}" title="${esc(tatil[s] || '')}">${d.getDate()}<br><small>${['Pz', 'Pt', 'Sa', 'Ça', 'Pe', 'Cu', 'Ct'][d.getDay()]}</small></th>`; }).join('') + '</tr></thead><tbody>';
    kisiler.forEach(p => {
      const izinler = V.talepler.filter(t => t.kullanan.personelNo === p.personelNo && !t.iptal && t.talepDurumu !== 'Reddedildi');
      h += `<tr><td class="ad">${esc(p.adSoyad)}<br><small>${esc(p.departman)}</small></td>` + gunler.map(d => {
        const s = iso(d), hs = d.getDay() === 0 || d.getDay() === 6, tt = tatil[s] && !/yarım/.test(tatil[s]);
        const t = izinler.find(x => x.baslangicIso <= s && x.bitisIso >= s);
        if (!t) return `<td class="${tt ? 'tt' : hs ? 'hs' : ''}"></td>`;
        return `<td class="${tt ? 'tt' : hs ? 'hs' : ''}"><span class="iz ${t.baslangicIso === s ? 'bas' : ''} ${t.bitisIso === s ? 'son' : ''} ${t.onaylandi ? '' : 'bekliyor'}" style="background:${RENK[t.izinTuru] || '#94a3b8'}" data-no="${t.no}" title="${esc(t.no + ' · ' + t.turAdi + ' · ' + t.baslangic + ' – ' + t.bitis + ' · ' + t.talepDurumu)}"></span></td>`;
      }).join('') + '</tr>';
    });
    $('#izTakvim').innerHTML = h + '</tbody>';
    $('#izLejant').innerHTML = V.turler.map(t => `<span style="--c:${RENK[t.kod] || '#94a3b8'}">${esc(t.ad)}</span>`).join('') + '<span style="--c:#fee2e2">Resmi tatil</span><span style="--c:#f1f5f9">Hafta sonu</span><span style="--c:#cbd5e1">Çizgili: onay bekliyor</span>';
    $$('#izTakvim .iz').forEach(s => s.addEventListener('click', () => detayAc(s.dataset.no)));
  }
  $('#izAyOnce').addEventListener('click', () => { takvimAy = new Date(takvimAy.getFullYear(), takvimAy.getMonth() - 1, 1); cizTakvim(); });
  $('#izAySonra').addEventListener('click', () => { takvimAy = new Date(takvimAy.getFullYear(), takvimAy.getMonth() + 1, 1); cizTakvim(); });
  $('#izAyBugun').addEventListener('click', () => { takvimAy = new Date(new Date().getFullYear(), new Date().getMonth(), 1); cizTakvim(); });
  $('#izTakvimDep').addEventListener('change', cizTakvim);

  // ---- talep detayı ----
  function detayAc(no) { aktifNo = no; cizDetay(); sekme('detay'); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  function cizDetay() {
    const t = V.talepler.find(x => x.no === aktifNo), el = $('#izDetay');
    $('#izDetayNo').textContent = t ? t.no : '—';
    if (!t) { el.innerHTML = '<div class="na-bos">Listeden bir talep seçin.</div>'; return; }
    const bekleyen = t.onaylar.find(o => o.durum === 'Onay Bekliyor'), b = bakiyeBul(t.kullanan.personelNo);
    const adimSinif = d => d === 'Onaylandı' || d === 'Tamamlandı' ? 'ok' : d === 'Onay Bekliyor' || d === 'Revizyon' ? 'bekle' : d === 'Reddedildi' ? 'hata' : '';
    const kisiKart = (k, baslik, ek) => `<div class="na-kart h-100 mb-0"><div class="na-kart-b"><span>${baslik}</span><span class="not">İK</span></div><div class="na-kart-g na-kisi">${!k ? '<span class="na-bos">Belirtilmedi</span>' : `<span class="l">Personel No</span><span class="v">${esc(k.personelNo)}</span><span class="l">Ad Soyad</span><span class="v">${esc(k.adSoyad)}</span><span class="l">Şirket</span><span class="v">${esc(k.sirket)}</span><span class="l">Departman</span><span class="v">${esc(k.departman)}</span><span class="l">Ünvan</span><span class="v">${esc(k.unvan)}</span><span class="l">Aktif</span><span class="v">${k.aktif ? 'Evet' : '<span class="text-danger">Hayır</span>'}</span>${ek || ''}`}</div></div>`;
    const iptalEdilebilir = !t.iptal && t.talepDurumu !== 'Reddedildi' && !(t.onaylandi && t.baslangicIso <= bugunIso);
    el.innerHTML = `
    <div class="na-kart">
      <div class="na-kart-b"><span><i class="fas fa-file-invoice me-1"></i>Talep Üst Bilgileri — <b>${t.no}</b>${t.revizyon ? ` <span class="not">revizyon ${t.revizyon}</span>` : ''}</span>
        <span class="d-flex gap-1 flex-wrap align-items-center">${turRozet(t)}${t.belgeEksik ? rozet('Belge eksik', 'hata') : ''}${t.uyarilar.length ? `<span class="na-bs" style="background:#fef9c3;color:#713f12" title="${esc(t.uyarilar.join(' '))}"><i class="fas fa-triangle-exclamation me-1"></i>${t.uyarilar.length} kural uyarısı</span>` : ''}
        ${iptalEdilebilir ? '<button type="button" class="btn btn-outline-danger btn-sm py-0" id="izIptalBtn"><i class="fas fa-ban me-1"></i>İptal Et</button>' : ''}<button type="button" class="btn btn-light btn-sm py-0" id="izYazdir"><i class="fas fa-print me-1"></i>Yazdır</button></span></div>
      <div class="na-kart-g">
        <div class="na-ustbilgi">
          <div><div class="l">Talep No</div><div class="v">${t.no}</div></div><div><div class="l">Talep Tarihi / Saati</div><div class="v">${t.tarih} ${t.saat}</div></div><div><div class="l">Şirket</div><div class="v">${esc(t.sirket)}</div></div>
          <div><div class="l">Departman</div><div class="v">${esc(t.departman)}</div></div><div><div class="l">Talebi Oluşturan</div><div class="v">${esc(t.olusturan.adSoyad)}</div></div><div><div class="l">Entegrasyon</div><div class="v">${rozet(t.entegrasyonDurumu, 'ok')} <small class="text-muted">son senkron ${t.sonSenkron || '—'}</small></div></div>
        </div>
        <div class="na-durum3">
          <div><div class="l">Talep Durumu <small>(Workflow)</small></div><div class="v">${talepRozet(t.talepDurumu)}</div><div class="s">${bekleyen ? 'Sırada: ' + esc(bekleyen.kisi) : t.iptal ? esc(t.iptalNedeni) : t.onaylandi ? 'Onay tarihi ' + t.onayTarihi : ''}</div></div>
          <div><div class="l">Kullanım Durumu <small>(PDKS)</small></div><div class="v">${kullanimRozet(t.kullanimDurumu)}</div><div class="s">${t.fiiliBaslangic ? 'başladı ' + t.fiiliBaslangic : 'planlanan ' + t.baslangic}${t.fiiliDonus ? ' · döndü ' + t.fiiliDonus : ''} · kullanılan ${gun(t.kullanilanGun)} / ${gun(t.isGunu)} gün</div></div>
          <div><div class="l">Puantaj Durumu <small>(Bordro)</small></div><div class="v">${puantajRozet(t.puantajDurumu)}</div><div class="s">${t.puantajDonemi ? t.puantajDonemi + ' dönemi · ' + gun(t.puantajGun) + ' gün · ' + t.puantajTarihi : (t.ucretli ? 'ücretli izin' : 'ücretsiz — bordro kesintisi')}${t.sgkRaporNo ? ' · ' + t.sgkRaporNo : ''}</div></div>
        </div>
        <div id="izIptalForm" style="display:none;gap:8px;align-items:flex-end" class="mt-2 p-2 border rounded bg-light"><div class="flex-grow-1"><label class="form-label">İptal nedeni (zorunlu)</label><input class="form-control" id="izIptalNeden"></div><button type="button" class="btn btn-danger btn-sm" id="izIptalGonder">İptali Onayla</button><button type="button" class="btn btn-light btn-sm" id="izIptalVazgec">Vazgeç</button></div>
      </div>
    </div>
    <div class="row g-2 mb-2"><div class="col-md-6">${kisiKart(t.kullanan, '<i class="fas fa-user-check me-1"></i>İzni Kullanan Personel', `<span class="l">Kalan Yıllık İzin</span><span class="v">${b ? gun(b.kalan) + ' gün' : '—'}</span>`)}</div><div class="col-md-6">${kisiKart(t.vekil, '<i class="fas fa-user-group me-1"></i>Yerine Bakacak Kişi (Vekil)')}</div></div>
    <div class="row g-2">
      <div class="col-lg-7">
        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-calendar-plus me-1"></i>İzin Detayları</span>${t.talepDurumu === 'Revizyon Bekliyor' && benimMi(t) ? '<button type="button" class="btn btn-warning btn-sm py-0" id="izRevizeAc"><i class="fas fa-pen me-1"></i>Revize Et</button>' : ''}</div>
          <div class="na-kart-g">
            <div class="na-ustbilgi" style="grid-template-columns:repeat(4,1fr)">
              <div><div class="l">İzin Türü</div><div class="v">${esc(t.turAdi)}</div></div><div><div class="l">Başlangıç</div><div class="v">${t.baslangic}</div></div><div><div class="l">Bitiş</div><div class="v">${t.bitis}</div></div><div><div class="l">Yarım Gün</div><div class="v">${esc(t.yarimGun)}</div></div>
              <div><div class="l">Takvim Günü</div><div class="v">${t.takvimGunu}</div></div><div><div class="l">İş Günü</div><div class="v">${gun(t.isGunu)} ${kilit}</div></div><div><div class="l">Kullanılan</div><div class="v">${gun(t.kullanilanGun)} ${kilit}</div></div><div><div class="l">Ücret</div><div class="v">${t.ucretli ? 'Ücretli' : '<span class="text-danger">Ücretsiz</span>'}${t.bakiyedenDuser ? ' · bakiyeden' : ''}</div></div>
              <div><div class="l">Ulaşılacak Telefon</div><div class="v">${esc(t.iletisim || '—')}</div></div><div><div class="l">İzin Adresi</div><div class="v">${esc(t.adres || '—')}</div></div><div style="grid-column:span 2"><div class="l">Açıklama</div><div>${esc(t.aciklama || '—')}</div></div>
            </div>
            ${t.uyarilar.length ? `<div class="na-uyari sari mt-2"><b>Kural uyarıları:</b><ul class="iz-uyari-liste">${t.uyarilar.map(u => `<li>${esc(u)}</li>`).join('')}</ul></div>` : ''}
            <div id="izRevizeForm" style="display:none" class="mt-2 p-2 border rounded bg-light">
              <div class="row g-2 align-items-end"><div class="col-md-3"><label class="form-label">Yeni Başlangıç</label><input class="form-control" type="date" id="izRevBas" value="${t.baslangicIso}"></div><div class="col-md-3"><label class="form-label">Yeni Bitiş</label><input class="form-control" type="date" id="izRevBit" value="${t.bitisIso}"></div><div class="col-md-6 d-flex gap-1"><button type="button" class="btn btn-primary btn-sm" id="izRevGonder">Revizyonu Gönder</button><button type="button" class="btn btn-light btn-sm" id="izRevIptal">Vazgeç</button></div></div>
              <div class="form-text">Tarih değişince iş günü, bakiye kontrolü ve onay akışı yeniden hesaplanır.</div>
            </div>
          </div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-clipboard-check me-1"></i>Onay Süreci</span><span class="not">${t.onaylar.filter(o => o.sira > 1).map(o => o.adim.replace(' Onayı', '')).join(' + ')}</span></div>
          <div class="na-kart-g">
            ${t.onaylar.map(o => `<div class="na-adim ${adimSinif(o.durum)}"><span class="no">${o.sira}</span><div><div class="ad">${esc(o.adim)}</div><div class="k">${esc(o.kisi)}${o.not ? ' · <i>' + esc(o.not) + '</i>' : ''}</div></div><div class="sag">${o.durum === 'Onaylandı' || o.durum === 'Tamamlandı' ? rozet(o.durum, 'ok') : o.durum === 'Onay Bekliyor' ? rozet('Onay Bekliyor', 'bekle') : o.durum === 'Reddedildi' ? rozet('Reddedildi', 'hata') : o.durum === 'Revizyon' ? rozet('Revizyona Gönderildi', 'bilgi') : `<span class="text-muted">${esc(o.durum === 'Pasif' ? 'Önceki adım sonrası aktif' : o.durum)}</span>`}<div class="k">${o.tarih || ''}</div></div></div>`).join('')}
            ${bekleyen ? `<div class="d-flex gap-1 flex-wrap align-items-center mt-2 pt-2 border-top"><span class="na-mini me-1">Onaycı işlemi (${esc(bekleyen.kisi)} — örnek rol):</span>
              <button type="button" class="btn btn-success btn-sm py-0" data-onay="Onayla"><i class="fas fa-check me-1"></i>Onayla</button><button type="button" class="btn btn-outline-danger btn-sm py-0" data-onay="Reddet"><i class="fas fa-xmark me-1"></i>Reddet</button><button type="button" class="btn btn-outline-warning btn-sm py-0" data-onay="Revizyon"><i class="fas fa-rotate-left me-1"></i>Revizyona Gönder</button>
              <input class="form-control form-control-sm flex-grow-1" id="izOnayNot" placeholder="Not / red gerekçesi (red ve revizyonda zorunlu)"></div>
              <div class="na-oto mt-2"><div class="h">Onaycının gördüğü bilgi</div><span>Kalan bakiye <b>${b ? gun(b.kalan) : '—'} gün</b></span><span>Bu talep <b>${gun(t.isGunu)} gün</b></span><span>Onay bekleyen toplam <b>${b ? gun(b.onayBekleyen) : '—'} gün</b></span><span>Kıdem <b>${b ? b.kidemYil : '—'} yıl</b></span><span>Departman mevcudu <b>${V.personel.filter(p => p.aktif && p.departman === t.kullanan.departman && p.sirket === t.kullanan.sirket).length}</b></span><span>Aynı tarihte izinli <b>${V.talepler.filter(x => x.no !== t.no && x.onaylandi && !x.iptal && x.kullanan.departman === t.kullanan.departman && x.baslangicIso <= t.bitisIso && x.bitisIso >= t.baslangicIso).length}</b></span><span>Vekil <b>${esc(t.vekil ? t.vekil.adSoyad : '—')}</b></span><span>Belge <b>${t.belgeGerekli ? (t.belgeAlindi ? 'Alındı' : 'Bekleniyor') : 'Gerekmiyor'}</b></span></div>` : ''}
          </div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-wallet me-1"></i>Bakiye Etkisi</span><span class="not">canlı hesap ${kilit}</span></div>
          <div class="na-kart-g">${t.bakiyedenDuser ? bakiyeHtml(b, t.kullanan, true) : `<div class="na-mini">${esc(t.turAdi)} yıllık izin bakiyesinden düşmez${t.izinTuru === 'MAZERET' ? ` · bu yıl kullanılan mazeret ${b ? gun(b.mazeretKullanilan) : 0}/3 gün` : ''}${t.izinTuru === 'HASTALIK' ? ` · bu yıl raporlu ${b ? gun(b.raporluGun) : 0} gün` : ''}.</div>` + bakiyeHtml(b, t.kullanan, true)}</div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-fingerprint me-1"></i>PDKS, Belge ve Puantaj Bilgileri</span><span class="not">kaynak sistemlerden otomatik ${kilit}</span></div>
          <div class="na-kart-g">
            <div class="na-ustbilgi" style="grid-template-columns:repeat(4,1fr)">
              <div><div class="l">Fiili Başlangıç (PDKS)</div><div class="v">${t.fiiliBaslangic || '—'}</div></div><div><div class="l">Fiili Dönüş (PDKS)</div><div class="v">${t.fiiliDonus || '—'}</div></div><div><div class="l">Dönüş Teyidi</div><div class="v">${t.donusTeyidi ? `<span class="text-success"><i class="fas fa-signature me-1"></i>Teyit edildi</span> <small class="text-muted">${t.donusTeyitTarihi}</small>` : (t.onaylandi && !t.iptal && t.baslangicIso <= bugunIso ? '<button type="button" class="btn btn-outline-success btn-sm py-0" id="izDonusTeyit"><i class="fas fa-signature me-1"></i>"İzinden döndüm"</button>' : '—')}</div></div><div><div class="l">Belge</div><div class="v">${t.belgeGerekli ? (t.belgeAlindi ? rozet('Alındı', 'ok') : rozet('Bekleniyor', 'hata')) : '<span class="text-muted">Gerekmiyor</span>'}${t.sgkRaporNo ? ' <small>' + esc(t.sgkRaporNo) + '</small>' : ''}</div></div>
              <div><div class="l">Puantaj Dönemi</div><div class="v">${t.puantajDonemi || '—'}</div></div><div><div class="l">Puantaj Tarihi</div><div class="v">${t.puantajTarihi || '—'}</div></div><div><div class="l">Puantaja Aktarılan</div><div class="v">${t.puantajGun != null ? gun(t.puantajGun) + ' gün' : '—'}</div></div><div><div class="l">Bordro Etkisi</div><div class="v">${t.ucretli ? 'Ücretli izin' : '<span class="text-danger">Ücretsiz kesinti</span>'}</div></div>
            </div>
          </div>
        </div>
      </div>

      <div class="col-lg-5">
        <div class="na-kart na-olay">
          <div class="na-kart-b"><span><i class="fas fa-satellite-dish me-1"></i>Kaynak Sistem Olayı (simülasyon)</span><span class="not">PDKS · SGK · Bordro · İK</span></div>
          <div class="na-kart-g">
            <div class="na-mini mb-1">Gerçek kurulumda bu olaylar PDKS (giriş-çıkış), SGK (e-rapor) ve bordro sisteminden Talep No ile otomatik gelir. Her olay benzersiz Transaction ID taşır; aynı ID ikinci kez yok sayılır.</div>
            <div class="row g-1 align-items-end">
              <div class="col-7"><label class="form-label">Olay</label><select class="form-select" id="izOlayTur">
                <option value="IzinBasladi">IzinBasladi — PDKS: izin günü giriş yok</option>
                <option value="IzinBitti">IzinBitti — PDKS: işe dönüş (erken dönüş desteklenir)</option>
                <option value="RaporBildirimi">RaporBildirimi — SGK e-rapor</option>
                <option value="BelgeAlindi">BelgeAlindi — İK evrak teslimi</option>
                <option value="PuantajAktarildi">PuantajAktarildi — Bordro dönem kapanışı</option>
                <option value="PuantajTersKayit">PuantajTersKayit — Bordro ters kayıt</option></select></div>
              <div class="col-5"><label class="form-label">Transaction ID</label><input class="form-control" id="izOlayId" value="${'TX-' + Date.now().toString().slice(-6)}"></div>
              <div class="col-6" data-alan="tarih"><label class="form-label">Tarih</label><input class="form-control" type="date" id="izOlayTarih" value="${t.baslangicIso}"></div>
              <div class="col-6" data-alan="ref" style="display:none"><label class="form-label">Referans (rapor no / dosya / dönem)</label><input class="form-control" id="izOlayRef"></div>
              <div class="col-12 d-flex gap-1 mt-1"><button type="button" class="btn btn-warning btn-sm" id="izOlayGonder"><i class="fas fa-bolt me-1"></i>Olayı İşle</button><button type="button" class="btn btn-light btn-sm" id="izOlayTekrar" title="Aynı Transaction ID ile tekrar gönderir; mükerrer koruma testi">Aynı ID ile tekrar gönder</button></div>
            </div>
          </div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-timeline me-1"></i>Hareket Geçmişi / Timeline</span><span class="not">${t.zaman.length} hareket</span></div>
          <div class="na-kart-g"><ul class="na-zaman">${t.zaman.slice().reverse().map(z => `<li class="${/PDKS|SGK/.test(z.kaynak) ? 'fin' : /Bordro|İK/.test(z.kaynak) ? 'muh' : z.kaynak === 'Sistem' ? 'sis' : 'wf'}"><span class="t">${z.tarih}</span>${esc(z.hareket)} <span class="k">· ${esc(z.kaynak)}</span></li>`).join('')}</ul></div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-paperclip me-1"></i>Belge ve Ekler</span><span class="not">${t.ekler.length} dosya</span></div>
          <div class="na-kart-g">${t.ekler.map(e => `<div class="na-ek"><i class="fas ${/pdf/.test(e.ad) ? 'fa-file-pdf' : /png|jpg/.test(e.ad) ? 'fa-file-image' : 'fa-file'}"></i><span>${esc(e.ad)}</span><span class="na-bs" style="background:#f1f5f9;color:#475569">${esc(e.tur)}</span><span class="m">${esc(e.boyut)} · ${e.tarih} · ${esc(e.yukleyen)}</span></div>`).join('') || '<div class="na-bos">Ek dosya yok.</div>'}</div>
        </div>

        <div class="na-kart">
          <div class="na-kart-b"><span><i class="fas fa-shield-halved me-1"></i>Audit Log / Değişiklik Geçmişi</span><span class="not">${t.audit.length} kayıt</span></div>
          <div class="na-kart-g p-0"><div class="table-responsive" style="max-height:260px"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Saat</th><th>Tablo</th><th>Alan</th><th>Eski</th><th>Yeni</th><th>Kullanıcı</th><th>İşlem</th><th>Kaynak</th></tr></thead><tbody>
            ${t.audit.map(x => `<tr><td>${x.tarih}</td><td>${x.saat}</td><td><small>${esc(x.tablo)}</small></td><td><small>${esc(x.alan)}</small></td><td>${esc(x.eski)}</td><td>${esc(x.yeni)}</td><td>${esc(x.kullanici)}</td><td>${esc(x.islemTipi)}</td><td>${esc(x.kaynakSistem)}</td></tr>`).join('')}
            </tbody></table></div></div>
        </div>
      </div>
    </div>`;

    $('#izYazdir').addEventListener('click', () => window.print());
    const ib = $('#izIptalBtn'); if (ib) ib.addEventListener('click', () => { $('#izIptalForm').style.display = 'flex'; $('#izIptalNeden').focus(); });
    const iv = $('#izIptalVazgec'); if (iv) iv.addEventListener('click', () => { $('#izIptalForm').style.display = 'none'; });
    const ig = $('#izIptalGonder'); if (ig) ig.addEventListener('click', async () => { const j = await post('/IkTalep/IzinIptal', { no: t.no, neden: $('#izIptalNeden').value }); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message || 'Talep iptal edildi; planlanan gün bakiyeye iade edildi.')); });
    $$('[data-onay]', el).forEach(bt => bt.addEventListener('click', async () => {
      const not = $('#izOnayNot').value.trim();
      if (bt.dataset.onay !== 'Onayla' && !not) { mesaj('danger', bt.dataset.onay === 'Reddet' ? 'Red gerekçesi zorunludur.' : 'Revizyon açıklaması zorunludur.'); $('#izOnayNot').focus(); return; }
      const j = await post('/IkTalep/IzinOnayla', { no: t.no, eylem: bt.dataset.onay, not }); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message || (bt.dataset.onay + ' işlendi.')));
    }));
    const ra = $('#izRevizeAc'); if (ra) ra.addEventListener('click', () => { $('#izRevizeForm').style.display = 'block'; });
    const ri = $('#izRevIptal'); if (ri) ri.addEventListener('click', () => { $('#izRevizeForm').style.display = 'none'; });
    const rg = $('#izRevGonder'); if (rg) rg.addEventListener('click', async () => { const j = await post('/IkTalep/IzinRevize', { no: t.no, baslangic: $('#izRevBas').value, bitis: $('#izRevBit').value }); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message || 'Talep revize edildi; onay akışı yeniden başlatıldı.')); });
    const dt = $('#izDonusTeyit'); if (dt) dt.addEventListener('click', async () => { const j = await post('/IkTalep/IzinDonusTeyidi', { no: t.no }); guncelle(j); mesaj(j.success ? 'success' : 'danger', esc(j.message || 'Dönüş teyidi kaydedildi.')); });
    const olayTur = $('#izOlayTur');
    function olayAlanlari() { const o = olayTur.value; $('[data-alan=tarih]', el).style.display = /IzinBasladi|IzinBitti/.test(o) ? '' : 'none'; $('[data-alan=ref]', el).style.display = /RaporBildirimi|BelgeAlindi|PuantajAktarildi/.test(o) ? '' : 'none'; $('#izOlayTarih').value = o === 'IzinBitti' ? sonrakiGun(t.bitisIso) : t.baslangicIso; $('#izOlayRef').value = o === 'PuantajAktarildi' ? t.bitisIso.slice(0, 7) : o === 'BelgeAlindi' ? 'belge.pdf' : ''; }
    const sonrakiGun = s => { const d = new Date(s); d.setDate(d.getDate() + 1); while (d.getDay() === 0 || d.getDay() === 6) d.setDate(d.getDate() + 1); return d.toISOString().slice(0, 10); };
    olayTur.addEventListener('change', olayAlanlari); olayAlanlari();
    async function olayGonder(ayniId) {
      const o = olayTur.value;
      const j = await post('/IkTalep/IzinOlay', { no: t.no, olay: o, islemId: $('#izOlayId').value.trim(), ek1: /IzinBasladi|IzinBitti/.test(o) ? $('#izOlayTarih').value : $('#izOlayRef').value });
      guncelle(j); mesaj(j.success ? 'success' : 'warning', j.success ? `<i class="fas fa-bolt me-1"></i>${o} işlendi; kullanım, bakiye ve puantaj durumu yeniden hesaplandı.` : esc(j.message));
      if (j.success && !ayniId) $('#izOlayId').value = 'TX-' + Date.now().toString().slice(-6);
    }
    $('#izOlayGonder').addEventListener('click', () => olayGonder(false));
    $('#izOlayTekrar').addEventListener('click', () => olayGonder(true));
  }

  function cizHepsi() { cizKpi(); cizListe(); cizOnay(); takipFiltreleri(); cizTakip(); cizTakvim(); cizDetay(); kullananDegisti(); }

  fetch('/IkTalep/IzinVeri').then(r => r.json()).then(v => {
    V = v;
    const depler = V.personel.map(p => p.departman).filter((x, i, a) => a.indexOf(x) === i).sort((a, b) => a.localeCompare(b, 'tr'));
    $('#izTakvimDep').innerHTML = '<option value="">Tüm departmanlar</option>' + depler.map(d => `<option ${d === V.ben.departman ? 'selected' : ''}>${esc(d)}</option>`).join('');
    formHazirla(); cizKpi(); cizListe(); cizOnay(); takipFiltreleri(); cizTakip(); cizTakvim();
    const h = decodeURIComponent(location.hash.slice(1)); let p = kok.dataset.sekme || 'yeni';
    if (h) { const [hp, hn] = h.split('/'); p = hp; if (hn) aktifNo = hn; }
    if (!aktifNo && p === 'detay') { const ilk = V.talepler.find(benimMi); if (ilk) aktifNo = ilk.no; }
    cizDetay(); sekme(p); window.NA_HAZIR = true;
  }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
