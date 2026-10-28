// ============================================================
// TAHSİLAT (Views/Rapor117/Index.cshtml) — çoklu ödeme satırı, otomatik / elle kur, otomatik (FIFO) / manuel fatura seçimi, canlı kapanış simülasyonu, taslak → işleme al, PDF / Excel, otomatik kapama
//   Uçlar: /Tahsilat/Veri · Faturalar?kod= · Kur?pb=&tarih= · Onizle · Kaydet · IslemeAl · Sil · FaturaKes · OtomatikKapat · EkstrePdf/Excel · SimulasyonPdf/Excel · TahsilatPdf/Excel (Controllers/TahsilatController.cs; kurallar Data/TahsilatOrnek.cs)
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('th'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const n2 = v => Number(v || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const n4 = v => Number(v || 0).toLocaleString('tr-TR', { minimumFractionDigits: 4, maximumFractionDigits: 4 });
  const tl = v => n2(v) + ' ₺';
  const sayi = v => parseFloat(String(v ?? '').replace(/\./g, '').replace(',', '.')) || 0;
  const yerelIso = d => { d = d || new Date(); return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0'); };
  const bugunIso = yerelIso();
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const riskRozet = d => rozet(d, d === 'Riskli' ? 'hata' : d === 'İzlemede' ? 'bekle' : 'ok');
  const durumRozet = d => `<span class="ci-rozet ${d === 'Taslak' ? 'taslak' : 'islendi'}">${esc(d)}</span>`;
  const yontemRozet = y => `<span class="ci-rozet ${y === 'Nakit' ? 'nakit' : (y === 'Çek' || y === 'Senet') ? 'cek' : ''}">${esc(y)}</span>`;
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + bugunIso + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 14000); window.scrollTo({ top: 0, behavior: 'smooth' }); }
  async function post(url, veri) { const r = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }); return r.json(); }
  async function indir(url, veri, ad) { const r = veri ? await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(veri) }) : await fetch(url); if (!r.ok) { mesaj('danger', 'Dosya üretilemedi.'); return; } const b = await r.blob(); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad; a.click(); }
  let V = null, FAT = [], BILGI = null, SAT = [], MOD = 'Otomatik', ON = null, SON = null, onZ = null, siraSayac = 0;
  const cari = () => V.cariler.find(c => c.kod === $('#thCari').value);
  function sekme(p) { $$('#thSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('.na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); }

  // ---- cari listesi (kod / ad araması) ve özet
  function cariDoldur() {
    const q = ($('#thCariAra').value || '').toLocaleLowerCase('tr-TR'), sel = $('#thCari'), eski = sel.value;
    const l = V.cariler.filter(c => !q || [c.kod, c.kisaAd, c.resmiUnvan].join(' ').toLocaleLowerCase('tr-TR').includes(q));
    sel.innerHTML = '<option value="">Cari seçiniz…</option>' + l.map(c => `<option value="${esc(c.kod)}">${esc(c.kod)} · ${esc(c.kisaAd)}${c.acikAdet ? ` (${c.acikAdet} açık fatura · ${esc(tl(c.acikTl))})` : ' (açık fatura yok)'}</option>`).join('');
    sel.value = l.some(c => c.kod === eski) ? eski : (q && l.length === 1 ? l[0].kod : '');
    $('#thCariSayi').textContent = `${l.length} / ${V.cariler.length} müşteri`;
    if (sel.value !== eski) cariSec();
  }
  function cariOzet() {
    const c = cari(); const o = $('#thCariOzet'), i = $('#thCariIslem');
    if (!c) { o.innerHTML = '<span class="text-muted">Cari seçince bakiye, son ödeme / yükleme, gecikme ve açık faturalar burada görünür.</span>'; i.style.setProperty('display', 'none', 'important'); return; }
    const b = BILGI || {};
    o.innerHTML = [`<span class="k buyuk"><small>Bakiye (TL)</small><b>${esc(c.bakiyeTr)}</b></span>`, `<span class="k"><small>X / R</small><b>${esc(c.bakiyeXTr)} / ${esc(c.bakiyeRTr)}</b></span>`, `<span class="k ${c.vadesiGecmisAdet ? 'gec' : ''}"><small>Vadesi Geçmiş</small><b>${esc(c.vadesiGecmisTr)}${c.vadesiGecmisAdet ? ` · ${c.vadesiGecmisAdet} fatura · ${c.maxGecikme} gün` : ''}</b></span>`, `<span class="k"><small>Açık Faturalar</small><b>${c.acikPb.map(p => `${p.adet} × ${esc(p.kalanTr)}`).join(' · ') || '—'}</b></span>`,
      `<span class="k ${b.sonOdemeGun > 45 ? 'gec' : 'ok'}"><small>Son Ödeme</small><b>${b.sonOdeme ? `${esc(b.sonOdeme)} · ${esc(b.sonOdemeTr)} · ${esc(b.sonOdemeTur)} (${b.sonOdemeGun} gün önce)` : '—'}</b></span>`, `<span class="k"><small>Son Yükleme (Fatura)</small><b>${b.sonYukleme ? `${esc(b.sonYukleme)} · ${esc(b.sonYuklemeTr)} (${b.sonYuklemeGun} gün önce)` : '—'}</b></span>`, `<span class="k ${b.ortalamaGecikme > 30 ? 'gec' : ''}"><small>Ort. Ödeme Gecikmesi</small><b>${b.ortalamaGecikme != null ? Number(b.ortalamaGecikme).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + ' gün' : '—'}</b></span>`, `<span class="k"><small>Son 12 Ay</small><b>${b.faturaAdet12Ay || 0} fatura ${esc(b.fatura12AyTr || '')} · ${b.odemeAdet12Ay || 0} ödeme ${esc(b.odeme12AyTr || '')}</b></span>`,
      `<span class="k ${b.eslesmemisOdeme ? 'gec' : ''}"><small>Eşleşmemiş Ödeme</small><b>${b.eslesmemisOdeme ? `${b.eslesmemisOdeme} · ${esc(b.eslesmemisOdemeTr)}` : 'yok'}</b></span>`, `<span class="k"><small>Vade Bekleyen Çek / Senet</small><b>${b.vadeBekleyenCek ? `${b.vadeBekleyenCek} · ${esc(b.vadeBekleyenCekTr)}` : '—'}</b></span>`, `<span class="k"><small>Vade · Temsilci</small><b>${esc(c.vadeSarti || '—')} · ${esc(c.temsilci || '—')}</b></span>`, `<span class="k"><small>Risk</small><b>${riskRozet(c.risk)}</b></span>`].join('');
    i.style.removeProperty('display');
    i.innerHTML = `<button type="button" class="btn ${BILGI && BILGI.kapatilabilir ? 'btn-warning' : 'btn-light'} btn-sm py-0" id="thOtoKapat" style="font-size:.64rem" title="Tahsilat almadan önce bu caride kapaması yapılmamış ödemeleri faturalarla eşleştirip kapatır (birebir + FIFO)"><i class="fas fa-link me-1"></i>Otomatik Kapat${BILGI && BILGI.kapatilabilir ? ` (${BILGI.kapatilabilir} eşleşme · ${esc(tl(BILGI.kapatilabilirTl))})` : ''}</button>
      <a class="btn btn-light btn-sm py-0" style="font-size:.64rem" href="/Rapor117/EkstrePdf?kod=${encodeURIComponent(c.kod)}" onclick="event.preventDefault(); window.open('/Tahsilat/EkstrePdf?kod=${encodeURIComponent(c.kod)}')"><i class="fas fa-file-pdf me-1"></i>Ekstre PDF</a><a class="btn btn-light btn-sm py-0" style="font-size:.64rem" href="/Tahsilat/EkstreExcel?kod=${encodeURIComponent(c.kod)}"><i class="fas fa-file-excel me-1"></i>Ekstre Excel</a>
      <a class="btn btn-light btn-sm py-0" style="font-size:.64rem" target="_blank" rel="noopener" href="/Rapor20?bpCode=${encodeURIComponent(c.kod)}"><i class="fas fa-file-invoice me-1"></i>Ekstre Raporu</a><a class="btn btn-light btn-sm py-0" style="font-size:.64rem" href="/CariKart/Musteri?kod=${encodeURIComponent(c.kod)}"><i class="fas fa-address-card me-1"></i>Kart</a><a class="btn btn-light btn-sm py-0" style="font-size:.64rem" href="/Rapor21?kod=${encodeURIComponent(c.kod)}"><i class="fas fa-chart-line me-1"></i>Adat</a><a class="btn btn-light btn-sm py-0" style="font-size:.64rem" href="/Rapor64?kod=${encodeURIComponent(c.kod)}"><i class="fas fa-arrows-left-right me-1"></i>Otomatik Kapatma</a>`;
    $('#thOtoKapat').addEventListener('click', otoKapat);
    const pbler = c.pbler.length ? c.pbler : ['TL']; const pbSel = $('#thPb'), pbEski = pbSel.value; pbSel.innerHTML = pbler.map(p => `<option>${p}</option>`).join(''); pbSel.value = pbler.includes(pbEski) ? pbEski : pbler[0];
  }
  async function cariSec() { ON = null; SON = null; SAT = []; await faturaYukle(); cariOzet(); satirCiz(); cizFaturalar(); ozet(); }
  async function faturaYukle() {
    const c = cari(); FAT = []; BILGI = null; if (!c) { cizFaturalar(); return; }
    const j = await (await fetch('/Tahsilat/Faturalar?kod=' + encodeURIComponent(c.kod))).json(); FAT = j.faturalar || []; BILGI = Object.assign({}, j.bilgi, { kapatilabilir: j.kapatilabilir, kapatilabilirTl: j.kapatilabilirTl });
    FAT.forEach(f => f.sec = true); $('#thNFat').textContent = FAT.length;
  }
  async function otoKapat() {
    const c = cari(); if (!c) return; if (!confirm(`${c.kod} ${c.kisaAd}: kapaması yapılmamış ödemeler faturalarla otomatik eşleştirilip kapatılsın mı?`)) return;
    const j = await post('/Tahsilat/OtomatikKapat', { kod: c.kod }); mesaj(j.success && j.eslesme > 0 ? 'success' : 'info', esc(j.message)); await veriYenile();
  }

  // ---- ödeme satırları
  function satirEkle(yontem) {
    const c = cari(); if (!c) { mesaj('warning', 'Önce cari seçin.'); return; }
    const vadeli = yontem === 'Çek' || yontem === 'Senet'; const t = new Date($('#thTarih').value || bugunIso); if (vadeli) t.setDate(t.getDate() + 60);
    SAT.push({ sira: ++siraSayac, yontem, tarih: yerelIso(t), belgeNo: '', banka: yontem === 'Nakit' ? '' : (V.bankalar[0] || ''), pb: $('#thPb').value, kur: Number(V.kurlar[$('#thPb').value] || 1), kurElle: false, tutar: 0, not: '' });
    satirCiz(); const son = $$('#thSatirlar input.tutar').pop(); if (son) son.focus();
  }
  function satirCiz() {
    const pb = $('#thPb').value, tb = $('#thSatirlar tbody'); const kurlu = pb !== 'TL';
    tb.innerHTML = SAT.map((s, i) => `<tr data-i="${i}"><td class="s">${i + 1}</td><td class="giris"><select class="ic yontem">${V.yontemler.map(y => `<option ${y === s.yontem ? 'selected' : ''}>${esc(y)}</option>`).join('')}</select></td><td class="giris"><input type="date" class="ic tarih" value="${esc(s.tarih)}" title="${(s.yontem === 'Çek' || s.yontem === 'Senet') ? 'Çek / senet vadesi' : 'Ödeme tarihi'}"></td><td class="giris"><input class="ic belge" value="${esc(s.belgeNo)}" placeholder="${(s.yontem === 'Çek' || s.yontem === 'Senet') ? 'Çek / senet no *' : 'Dekont / makbuz'}"></td><td class="giris"><select class="ic banka"><option value="">—</option>${V.bankalar.map(b => `<option ${b === s.banka ? 'selected' : ''}>${esc(b)}</option>`).join('')}</select></td><td class="giris e"><input class="ic kur e ${kurlu && !s.kurElle ? 'kilit' : ''}" value="${n4(s.kur)}" ${kurlu ? '' : 'disabled'}></td><td class="s">${kurlu ? `<button type="button" class="ci-rozet ${s.kurElle ? 'taslak' : 'islendi'} kurkaynak" style="border:0;cursor:pointer" title="Tıklayınca otomatik (TCMB) ↔ elle">${s.kurElle ? 'Elle' : 'Otomatik'}</button>` : '<span class="text-muted">TL</span>'}</td><td class="giris e"><input class="ic tutar e" inputmode="decimal" value="${s.tutar ? n2(s.tutar) : ''}" placeholder="0,00"></td><td class="e"><b>${tl(s.tutar * s.kur)}</b></td><td class="giris"><input class="ic not" value="${esc(s.not)}" placeholder="not"></td><td class="ci-git"><button type="button" class="sil" title="Satırı sil"><i class="fas fa-xmark"></i></button></td></tr>`).join('') || '<tr><td colspan="11" class="na-bos">Yukarıdaki düğmelerle ödeme satırı ekleyin (nakit, çek, senet, havale, kart — birlikte).</td></tr>';
    const toplam = SAT.reduce((a, s) => a + s.tutar, 0), toplamTl = SAT.reduce((a, s) => a + s.tutar * s.kur, 0);
    $('#thSatirlar tfoot').innerHTML = SAT.length ? `<tr><td colspan="7">Toplam · ${SAT.length} satır · ${Object.entries(SAT.reduce((g, s) => { g[s.yontem] = (g[s.yontem] || 0) + 1; return g; }, {})).map(([y, n]) => `${n} ${y}`).join(', ')}</td><td class="e">${n2(toplam)} ${esc(pb)}</td><td class="e">${tl(toplamTl)}</td><td colspan="2"></td></tr>` : '';
    $('#thNSatir').textContent = SAT.length;
    $$('#thSatirlar tbody tr[data-i]').forEach(r => {
      const i = +r.dataset.i, s = SAT[i];
      $('.yontem', r).addEventListener('change', e => { s.yontem = e.target.value; if (s.yontem === 'Nakit') s.banka = ''; satirCiz(); onizle(); });
      $('.tarih', r).addEventListener('change', async e => { s.tarih = e.target.value; if (!s.kurElle && pb !== 'TL') await kurOto(s); satirCiz(); onizle(); });
      $('.belge', r).addEventListener('input', e => s.belgeNo = e.target.value); $('.not', r).addEventListener('input', e => s.not = e.target.value); $('.banka', r).addEventListener('change', e => s.banka = e.target.value);
      $('.tutar', r).addEventListener('change', e => { s.tutar = sayi(e.target.value); satirCiz(); onizle(); }); $('.tutar', r).addEventListener('input', onizleGecikmeli);
      const k = $('.kur', r); if (k) { k.addEventListener('change', e => { s.kur = sayi(e.target.value) || s.kur; s.kurElle = true; satirCiz(); onizle(); }); }
      const kk = $('.kurkaynak', r); if (kk) kk.addEventListener('click', async () => { s.kurElle = !s.kurElle; if (!s.kurElle) await kurOto(s); satirCiz(); onizle(); });
      $('.sil', r).addEventListener('click', () => { SAT.splice(i, 1); satirCiz(); onizle(); });
    });
  }
  async function kurOto(s) { const j = await (await fetch(`/Tahsilat/Kur?pb=${encodeURIComponent(s.pb)}&tarih=${s.tarih}`)).json(); if (j.success) { s.kur = j.kur; s.kurAciklama = j.aciklama; } }
  const satirlarIstek = () => SAT.map((s, i) => ({ sira: i + 1, yontem: s.yontem, tarih: s.tarih, belgeNo: s.belgeNo, banka: s.banka, pb: s.pb, kur: s.kur, kurElle: s.kurElle, tutar: s.tutar, not: s.not }));
  const secim = () => { const o = {}; FAT.filter(f => f.sec && f.pb === $('#thPb').value).forEach(f => o[f.belgeNo] = f.sinir || 0); return o; };
  const istek = (isle) => { const c = cari(); return { kod: c.kod, tarih: $('#thTarih').value, islemTipi: $('#thTip').value, pb: $('#thPb').value, mod: MOD, secim: MOD === 'Manuel' ? secim() : null, satirlar: satirlarIstek(), aciklama: $('#thAciklama').value, isle: !!isle }; };

  // ---- fatura tablosu + simülasyon
  function onizleGecikmeli() { clearTimeout(onZ); onZ = setTimeout(() => { $$('#thSatirlar input.tutar').forEach((inp, i) => { if (SAT[i]) SAT[i].tutar = sayi(inp.value); }); onizle(); }, 400); }
  async function onizle() {
    const c = cari(); if (!c) { ON = null; cizFaturalar(); ozet(); return; }
    if (!SAT.length) { ON = null; cizFaturalar(); ozet(); return; }
    const j = await post('/Tahsilat/Onizle', istek(false)); ON = j.success ? j : null; cizFaturalar(); ozet();
  }
  function cizFaturalar() {
    const pb = $('#thPb').value, l = FAT.filter(f => f.pb === pb), tb = $('#thFaturalar tbody'); const d = {}; (ON?.dagitim || []).forEach(x => { (d[x.belgeNo] = d[x.belgeNo] || []).push(x); });
    tb.innerHTML = l.map(f => { const es = d[f.belgeNo] || []; const kapanan = es.reduce((a, x) => a + x.tutar, 0), kalanSonra = es.length ? es[es.length - 1].faturaKalan : f.kalan; const secili = MOD === 'Otomatik' || f.sec;
      return `<tr data-no="${esc(f.belgeNo)}" class="${es.length ? 'secili' : (!secili ? 'kapali' : '')}"><td class="s">${MOD === 'Manuel' ? `<input type="checkbox" class="fsec" ${f.sec ? 'checked' : ''}>` : '<i class="fas fa-wand-magic-sparkles text-muted" title="otomatik"></i>'}</td><td><b>${esc(f.belgeNo)}</b><br><small class="text-muted">${esc(f.aciklama || '')}</small></td><td>${esc(f.tarih)}</td><td>${esc(f.vade)}</td><td class="s ${f.gecikme > 0 ? 'gec' : 'erken'}">${f.gecikme > 0 ? f.gecikme + ' gün' : (f.gecikme < 0 ? -f.gecikme + ' gün kaldı' : 'bugün')}</td><td class="s">${esc(f.islemTipi)}</td><td class="e"><b>${esc(f.kalanTr)}</b></td><td class="e">${f.pb === 'TL' ? '—' : n4(f.faturaKuru)}</td><td class="giris e">${MOD === 'Manuel' ? `<input class="ic sinir e" inputmode="decimal" value="${f.sinir ? n2(f.sinir) : ''}" placeholder="kalanı" title="Boş = kalanın tamamı">` : '—'}</td><td class="e ${kapanan ? 'ok' : ''}">${kapanan ? `<b>${n2(kapanan)}</b>` : '—'}</td><td class="e ${kalanSonra <= 0.005 && es.length ? 'ok' : ''}">${es.length ? (kalanSonra <= 0.005 ? '<b>0,00 · kapandı</b>' : n2(kalanSonra)) : '—'}</td><td><small>${es.map(x => `#${x.odemeSira} ${esc(x.odemeYontem)} ${esc(x.odemeTarihi)}`).join('<br>') || '—'}</small></td><td class="s ${es.some(x => x.gecikmeGun > 0) ? 'gec' : (es.length ? 'erken' : '')}">${es.map(x => x.gecikmeGun).join(' / ') || '—'}</td><td class="e ${es.some(x => x.kurFarki > 0) ? 'gec' : ''}">${es.length && es.some(x => x.kurFarki) ? n2(es.reduce((a, x) => a + x.kurFarki, 0)) : '—'}</td><td class="e ${es.some(x => x.vadeFarki > 0) ? 'gec' : ''}">${es.length && es.some(x => x.vadeFarki > 0) ? n2(es.reduce((a, x) => a + x.vadeFarki, 0)) : '—'}</td></tr>`; }).join('') || `<tr><td colspan="15" class="na-bos">${cari() ? `Bu caride ${esc(pb)} açık fatura yok.` : 'Önce cari seçin.'}</td></tr>`;
    $('#thFaturalar tfoot').innerHTML = l.length ? `<tr><td colspan="6">${l.length} açık fatura · ${l.filter(f => f.gecikme > 0).length} gecikmiş${MOD === 'Manuel' ? ` · ${l.filter(f => f.sec).length} seçili` : ''}</td><td class="e">${n2(l.reduce((s, f) => s + f.kalan, 0))} ${esc(pb)}</td><td colspan="2"></td><td class="e">${ON ? n2(ON.dagitilan) + ' ' + esc(pb) : '—'}</td><td class="e">${ON ? `${ON.tamKapanan} fatura kapandı` : '—'}</td><td colspan="2"></td><td class="e">${ON ? esc(tl(ON.kurFarki)) : '—'}</td><td class="e">${ON ? esc(tl(ON.vadeFarki)) : '—'}</td></tr>` : '';
    $$('#thFaturalar input.fsec').forEach(i => i.addEventListener('change', () => { const f = FAT.find(x => x.belgeNo === i.closest('tr').dataset.no); f.sec = i.checked; onizle(); }));
    $$('#thFaturalar input.sinir').forEach(i => i.addEventListener('change', () => { const f = FAT.find(x => x.belgeNo === i.closest('tr').dataset.no); f.sinir = sayi(i.value); onizle(); }));
    $('#thHepsi').style.display = $('#thHicbiri').style.display = MOD === 'Manuel' ? '' : 'none';
  }
  function ozet() {
    const c = cari(), pb = $('#thPb').value, o = $('#thOzet'), kdv = Number(V.kdvOran); const tutar = SAT.reduce((a, s) => a + s.tutar, 0), tutarTl = SAT.reduce((a, s) => a + s.tutar * s.kur, 0);
    if (!c) { o.innerHTML = '<div class="l">Cari</div><div class="v text-muted">seçilmedi</div>'; return; }
    const gr = Object.entries(SAT.reduce((g, s) => { g[s.yontem] = (g[s.yontem] || 0) + s.tutar; return g; }, {}));
    const r = [['Cari', `${esc(c.kod)} · ${esc(c.kisaAd)}`], ['Tahsilat Tutarı', `<span class="buyuk">${n2(tutar)} ${esc(pb)}</span>`], ['TL Karşılığı', tl(tutarTl)], ...gr.map(([y, t]) => ['· ' + y, `${n2(t)} ${esc(pb)}`]), ['Fatura Seçimi', MOD === 'Manuel' ? `Manuel · ${FAT.filter(f => f.sec && f.pb === pb).length} fatura` : 'Otomatik (FIFO)'], ['ayrac'],
      ['Kapanan Fatura', ON ? `${ON.kapananFatura} adet (${ON.tamKapanan} tam) · ${n2(ON.dagitilan)} ${esc(pb)}` : '—'], ['Bakiyeye Avans', ON ? `<span class="${ON.avans > 0.005 ? 'yesil' : ''}">${n2(ON.avans)} ${esc(pb)}</span>` : '—'], ['ayrac'],
      ['Vade Farkı', ON ? `<span class="${ON.vadeFarki > 0 ? 'kirmizi' : ''}">${tl(ON.vadeFarki)}</span>` : '—'], ['Vade Farkı (KDV dahil)', ON && ON.vadeFarki > 0 ? tl(ON.vadeFarki * (1 + kdv / 100)) : '—'],
      ['Kur Farkı', ON ? `<span class="${ON.kurFarki > 0 ? 'kirmizi' : ''}">${tl(ON.kurFarki)}</span>` : (pb === 'TL' ? '<span class="text-muted">TL faturada yok</span>' : '—')], ['Kur Farkı (KDV dahil)', ON && ON.kurFarki > 0 ? tl(ON.kurFarki * (1 + kdv / 100)) : '—']];
    o.innerHTML = r.map(x => x[0] === 'ayrac' ? '<div class="ayrac"></div>' : `<div class="l">${x[0]}</div><div class="v">${x[1]}</div>`).join('');
    $('#thOzetNot').textContent = ON ? 'önizleme · kaydedince işlenir' : 'ödeme satırı girince hesaplanır';
    $('#thKff').disabled = !(SON && SON.islendi && SON.kurFarki > 0 && !SON.kurFarkiFaturaNo); $('#thVff').disabled = !(SON && SON.islendi && SON.vadeFarki > 0 && !SON.vadeFarkiFaturaNo);
  }
  function rehber() { $('#thRehber').innerHTML = [['Ödeme satırları', 'nakit, çek, senet, havale ve kart aynı tahsilatta; çek / senette tarih = vade, belge no zorunlu.'], ['Kur', `dövizde satır tarihinin TCMB kuru otomatik gelir; "Elle" ile değiştirilir.`], ['Otomatik', 'ödemeler tarih sırasıyla (aynı gün önce nakit) en eski vadeli faturadan başlayarak kapatır; artan kısım avans.'], ['Manuel', 'kapatılacak faturalar işaretlenir; sınır yazılırsa faturaya en çok o kadar uygulanır.'], ['Vade Farkı', `kapanan × kur × %${Number(V.vadeFarkiAylikOran).toLocaleString('tr-TR')} / 30 × gün (ödeme tarihi − vade; yalnız gecikmede).`], ['Kur Farkı', 'dövizli faturada kapanan × (ödeme kuru − fatura tarihindeki kur).'], ['Taslak', 'cariye işlenmez; Tahsilatlar sekmesinden "İşleme Al" ile aynı kayıt işlenir (çekler teslim alınınca vb.).']].map(([b, a]) => `<div><b>${b}:</b> ${esc(a)}</div>`).join(''); }
  async function kaydet(isle) {
    const c = cari(); if (!c) { mesaj('danger', 'Cari seçin.'); return; } if (!SAT.length || SAT.every(s => !(s.tutar > 0))) { mesaj('danger', 'En az bir ödeme satırına tutar girin.'); return; }
    const b = isle ? $('#thKaydetIsle') : $('#thKaydetTaslak'); b.disabled = true;
    try {
      const j = await post('/Tahsilat/Kaydet', istek(isle));
      if (!j.success) { mesaj('danger', `<b>Kaydedilemedi:</b> ${esc(j.message)}`); return; }
      SON = j.tahsilat; V.tahsilatlar.unshift(j.tahsilat); mesaj('success', `<i class="fas fa-circle-check me-1"></i>${esc(j.message)}${isle && (j.tahsilat.kurFarki > 0 || j.tahsilat.vadeFarki > 0) ? ' Sağdaki düğmelerle fark faturası kesebilirsiniz.' : ''} <a href="/Tahsilat/TahsilatPdf?no=${encodeURIComponent(j.tahsilat.no)}" target="_blank">PDF</a> · <a href="/Tahsilat/TahsilatExcel?no=${encodeURIComponent(j.tahsilat.no)}">Excel</a>`);
      if (isle) { SAT = []; } await veriYenile(); satirCiz(); cizListe(); ozet();
    } finally { b.disabled = false; }
  }
  async function faturaKes(tur, no) {
    const hedef = no || (SON && SON.no); if (!hedef) return; const j = await post('/Tahsilat/FaturaKes', { tahsilatNo: hedef, tur });
    if (!j.success) { mesaj('danger', esc(j.message)); return; }
    const i = V.tahsilatlar.findIndex(t => t.no === hedef); if (i >= 0) V.tahsilatlar[i] = j.tahsilat; if (SON && SON.no === hedef) SON = j.tahsilat; V.faturalar.unshift(j.fatura);
    mesaj('success', `<i class="fas fa-file-invoice me-1"></i>${esc(j.message)}`); cizListe(); cizFaturaListe(); faturaOnizle(j.fatura.no); ozet(); await veriYenile(); if (no) detay(no);
  }
  async function veriYenile() { const j = await (await fetch('/Tahsilat/Veri')).json(); V.cariler = j.cariler; V.tahsilatlar = j.tahsilatlar; V.faturalar = j.faturalar; cariDoldur(); await faturaYukle(); cariOzet(); cizFaturalar(); cizListe(); cizFaturaListe(); onizle(); }
  function yeni() { SON = null; ON = null; SAT = []; $('#thAciklama').value = ''; $('#thTarih').value = bugunIso; MOD = 'Otomatik'; $$('#thMod button').forEach(b => b.classList.toggle('aktif', b.dataset.mod === MOD)); FAT.forEach(f => { f.sec = true; f.sinir = 0; }); satirCiz(); cizFaturalar(); ozet(); sekme('yeni'); }

  // ---- listeler
  function cizListe() {
    const q = ($('#thListeAra').value || '').toLocaleLowerCase('tr-TR'), du = $('#thListeDurum').value; const l = V.tahsilatlar.filter(t => (!du || t.durum === du) && (!q || [t.no, t.cariKodu, t.cariAd, t.yontem, t.aciklama, t.satirlar.map(s => s.belgeNo).join(' ')].join(' ').toLocaleLowerCase('tr-TR').includes(q)));
    $('#thListe tbody').innerHTML = l.map(t => `<tr class="tik" data-no="${esc(t.no)}"><td><b>${esc(t.no)}</b></td><td>${esc(t.tarih)}</td><td><b>${esc(t.cariKodu)}</b> ${esc(t.cariAd)}</td><td>${esc(t.yontem)}<br><small class="text-muted">${t.satirAdet} satır${t.cekAdet ? ` · ${t.cekAdet} vadeli` : ''}</small></td><td class="s">${esc(t.islemTipi)}</td><td class="e"><b>${esc(t.tutarTr)}</b></td><td class="e">${esc(t.tutarTlTr)}</td><td class="s">${t.mod === 'Manuel' ? 'Manuel' : 'Otomatik'}</td><td>${durumRozet(t.durum)}${t.islendi ? `<br><small class="text-muted">${esc(t.islemeZamani)}</small>` : ''}</td><td class="s">${t.dagitim.length ? [...new Set(t.dagitim.map(d => d.belgeNo))].length : 0}</td><td class="e">${t.avans > 0.005 ? esc(t.avansTr) : '—'}</td><td class="e ${t.kurFarki > 0 ? 'gec' : ''}">${t.kurFarki ? esc(t.kurFarkiTr) : '—'}</td><td class="e ${t.vadeFarki > 0 ? 'gec' : ''}">${t.vadeFarki > 0 ? esc(t.vadeFarkiTr) : '—'}</td><td>${[t.kurFarkiFaturaNo, t.vadeFarkiFaturaNo].filter(Boolean).map(esc).join(', ') || (t.islendi && (t.kurFarki > 0 || t.vadeFarki > 0) ? '<span class="text-warning">kesilmedi</span>' : '—')}</td><td><small>${esc(t.kullanici)}<br>${esc(t.zaman)}</small></td><td class="ci-git">${t.islendi ? '' : `<button type="button" data-isle="${esc(t.no)}" title="İşleme Al (cariye işle / ibraz)" style="color:#15803d"><i class="fas fa-play"></i></button><button type="button" data-sil="${esc(t.no)}" title="Taslağı sil"><i class="fas fa-trash"></i></button>`}<a href="/Tahsilat/TahsilatPdf?no=${encodeURIComponent(t.no)}" target="_blank" title="PDF"><i class="fas fa-file-pdf"></i></a><a href="/Tahsilat/TahsilatExcel?no=${encodeURIComponent(t.no)}" title="Excel"><i class="fas fa-file-excel"></i></a></td></tr>`).join('') || '<tr><td colspan="16" class="na-bos">Kayıt yok.</td></tr>';
    $('#thNListe').textContent = V.tahsilatlar.length; $('#thNTaslak').textContent = V.tahsilatlar.filter(t => !t.islendi).length + ' taslak'; $('#thNFatura').textContent = V.faturalar.length;
    $$('#thListe tr.tik').forEach(r => r.addEventListener('click', e => { if (e.target.closest('a,button')) return; detay(r.dataset.no); }));
    $$('#thListe [data-isle]').forEach(b => b.addEventListener('click', () => islemeAl(b.dataset.isle))); $$('#thListe [data-sil]').forEach(b => b.addEventListener('click', () => sil(b.dataset.sil)));
  }
  async function islemeAl(no) {
    const t = V.tahsilatlar.find(x => x.no === no); if (!confirm(`${no} (${t ? t.tutarTr : ''}) cari hesabına işlensin mi? Fatura kalanları düşer, Takip Paneli'ne düşer.`)) return;
    const j = await post('/Tahsilat/IslemeAl', { no }); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) { await veriYenile(); detay(no); }
  }
  async function sil(no) { if (!confirm(`${no} taslağı silinsin mi?`)) return; const j = await post('/Tahsilat/Sil', { no }); mesaj(j.success ? 'success' : 'danger', esc(j.message)); if (j.success) { $('#thDetay').innerHTML = ''; await veriYenile(); } }
  function detay(no) {
    const t = V.tahsilatlar.find(x => x.no === no); if (!t) return; $$('#thListe tr.tik').forEach(r => r.classList.toggle('secili', r.dataset.no === no));
    $('#thDetay').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-receipt me-1"></i>${esc(t.no)} · ${esc(t.cariKodu)} ${esc(t.cariAd)} · ${esc(t.tutarTr)} (${esc(t.yontem)}, ${esc(t.islemTipi)}) ${durumRozet(t.durum)}</span><span class="d-flex gap-1 flex-wrap">${t.islendi ? '' : `<button type="button" class="btn btn-success btn-sm py-0" data-isle style="font-size:.64rem"><i class="fas fa-play me-1"></i>İşleme Al</button><button type="button" class="btn btn-outline-danger btn-sm py-0" data-sil style="font-size:.64rem"><i class="fas fa-trash me-1"></i>Sil</button>`}${t.islendi && t.kurFarki > 0 && !t.kurFarkiFaturaNo ? `<button type="button" class="btn btn-outline-primary btn-sm py-0" data-kes="Kur Farkı" style="font-size:.64rem"><i class="fas fa-file-invoice me-1"></i>Kur Farkı Faturası Kes</button>` : ''}${t.islendi && t.vadeFarki > 0 && !t.vadeFarkiFaturaNo ? `<button type="button" class="btn btn-outline-primary btn-sm py-0" data-kes="Vade Farkı" style="font-size:.64rem"><i class="fas fa-file-invoice me-1"></i>Vade Farkı Faturası Kes</button>` : ''}${[t.kurFarkiFaturaNo, t.vadeFarkiFaturaNo].filter(Boolean).map(f => `<button type="button" class="btn btn-light btn-sm py-0" data-fat="${esc(f)}" style="font-size:.64rem"><i class="fas fa-print me-1"></i>${esc(f)}</button>`).join('')}<a class="btn btn-light btn-sm py-0" style="font-size:.64rem" target="_blank" href="/Tahsilat/TahsilatPdf?no=${encodeURIComponent(t.no)}"><i class="fas fa-file-pdf me-1"></i>PDF</a><a class="btn btn-light btn-sm py-0" style="font-size:.64rem" href="/Tahsilat/TahsilatExcel?no=${encodeURIComponent(t.no)}"><i class="fas fa-file-excel me-1"></i>Excel</a>${t.islendi ? `<a class="btn btn-light btn-sm py-0" style="font-size:.64rem" target="_blank" rel="noopener" href="/Rapor123?sekme=${esc(t.panelSekme)}&sirket=${encodeURIComponent(t.sirketDb)}&bas=${esc(t.tarihIso)}&bit=${esc(t.tarihIso)}" title="Konsolide Finansal Takip Paneli"><i class="fas fa-clipboard-check me-1"></i>Takip Paneli</a>` : ''}<a class="btn btn-light btn-sm py-0" style="font-size:.64rem" target="_blank" rel="noopener" href="/Rapor20?bpCode=${encodeURIComponent(t.cariKodu)}"><i class="fas fa-file-invoice me-1"></i>Ekstre</a></span></div>
      <div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo ci-liste" data-dinamik="kapali"><thead><tr><th class="s">#</th><th>Yöntem</th><th>Tarih / Vade</th><th>Belge No</th><th>Banka</th><th class="e">Kur</th><th class="e">Tutar</th><th class="e">TL</th><th>Cari Hareket</th><th>Not</th></tr></thead><tbody>${t.satirlar.map(s => `<tr><td class="s">${s.sira}</td><td>${yontemRozet(s.yontem)}</td><td>${esc(s.tarih)}</td><td>${esc(s.belgeNo || '—')}</td><td>${esc(s.banka || '—')}</td><td class="e">${n4(s.kur)}${s.kurElle ? ' <small>(elle)</small>' : ''}</td><td class="e"><b>${esc(s.tutarTr)}</b></td><td class="e">${esc(s.tutarTlTr)}</td><td><small>${esc(s.hareketNo || (t.islendi ? '' : 'işlenmedi'))}</small></td><td><small>${esc(s.not)}</small></td></tr>`).join('')}</tbody></table></div>
      <div class="table-responsive"><table class="na-tablo ci-liste" data-dinamik="kapali"><thead><tr><th>Fatura</th><th>Fatura Tarihi</th><th>Vade</th><th>Ödeme</th><th>Ödeme Tarihi</th><th class="s">Gün</th><th class="s">Tip</th><th class="e">Kalan (Önce)</th><th class="e">Kapanan</th><th class="e">Kalan (Sonra)</th><th class="e">Fatura Kuru</th><th class="e">Ödeme Kuru</th><th class="e">Kur Farkı</th><th class="e">Vade Farkı</th></tr></thead><tbody>${t.dagitim.map(d => `<tr><td><b>${esc(d.belgeNo)}</b></td><td>${esc(d.faturaTarihi)}</td><td>${esc(d.vade)}</td><td>#${d.odemeSira} ${esc(d.odemeYontem)}${d.odemeBelge ? ' ' + esc(d.odemeBelge) : ''}</td><td>${esc(d.odemeTarihi)}</td><td class="s ${d.gecikmeGun > 0 ? 'gec' : 'erken'}">${d.gecikmeGun}</td><td class="s">${esc(d.islemTipi)}</td><td class="e">${esc(d.kalanTr)} ${esc(d.pb)}</td><td class="e"><b>${esc(d.tutarTr)} ${esc(d.pb)}</b></td><td class="e">${esc(d.faturaKalanTr)}</td><td class="e">${d.pb === 'TL' ? '—' : n4(d.faturaKuru)}</td><td class="e">${d.pb === 'TL' ? '—' : n4(d.tahsilatKuru)}</td><td class="e ${d.kurFarki > 0 ? 'gec' : ''}">${d.kurFarki ? esc(d.kurFarkiTr) : '—'}</td><td class="e ${d.vadeFarki > 0 ? 'gec' : ''}">${d.vadeFarki > 0 ? esc(d.vadeFarkiTr) : '—'}</td></tr>`).join('') || '<tr><td colspan="14" class="na-bos">Fatura eşleşmesi yok; tutar bakiyeye avans olarak kalır.</td></tr>'}</tbody><tfoot><tr><td colspan="8">Toplam · avans ${esc(t.avansTr)}${t.aciklama ? ' · ' + esc(t.aciklama) : ''}</td><td class="e">${n2(t.dagitilan)} ${esc(t.paraBirimi)}</td><td colspan="3"></td><td class="e">${esc(t.kurFarkiTr)}</td><td class="e">${esc(t.vadeFarkiTr)}</td></tr></tfoot></table></div></div></div>`;
    const d = $('#thDetay'); const b = (s, f) => { const e = $(s, d); if (e) e.addEventListener('click', f); };
    b('[data-isle]', () => islemeAl(no)); b('[data-sil]', () => sil(no)); $$('[data-kes]', d).forEach(x => x.addEventListener('click', () => faturaKes(x.dataset.kes, no))); $$('[data-fat]', d).forEach(x => x.addEventListener('click', () => { sekme('fatura'); faturaOnizle(x.dataset.fat); }));
    d.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
  function cizFaturaListe() {
    $('#thFaturaListe tbody').innerHTML = V.faturalar.map(f => `<tr class="tik" data-no="${esc(f.no)}"><td><b>${esc(f.no)}</b></td><td>${esc(f.tarih)}</td><td>${esc(f.vade)}</td><td><b>${esc(f.cariKodu)}</b> ${esc(f.cariAd)}</td><td>${rozet(f.tur, f.tur === 'Kur Farkı' ? 'bekle' : 'hata')}</td><td class="e">${esc(f.matrahTr)}</td><td class="e">${esc(f.kdvTr)}</td><td class="e"><b>${esc(f.toplamTr)}</b></td><td>${esc(f.tahsilatNo)}</td><td><small>${esc(f.kullanici)}</small></td></tr>`).join('') || '<tr><td colspan="10" class="na-bos">Henüz fark faturası kesilmedi.</td></tr>';
    $('#thNFatura').textContent = V.faturalar.length; $$('#thFaturaListe tr.tik').forEach(r => r.addEventListener('click', () => faturaOnizle(r.dataset.no)));
  }
  function faturaOnizle(no) {
    const f = V.faturalar.find(x => x.no === no); if (!f) return; $$('#thFaturaListe tr.tik').forEach(r => r.classList.toggle('secili', r.dataset.no === no));
    $('#thFaturaOnizle').innerHTML = `<div class="th-fatura"><div class="ust"><div><h5>${esc(f.tur.toLocaleUpperCase('tr-TR'))} FATURASI</h5><div class="no">${esc(f.no)} · İşlem tipi ${esc(f.islemTipi)} · Tahsilat ${esc(f.tahsilatNo)}</div></div><div class="text-end"><div><b>Tarih:</b> ${esc(f.tarih)}</div><div><b>Vade:</b> ${esc(f.vade)}</div><div><b>Düzenleyen:</b> ${esc(f.kullanici)}</div></div></div>
      <div><b>Sayın</b> ${esc(f.cariUnvan)}<br><small class="text-muted">${esc(f.cariKodu)} · VKN ${esc(f.vergiNo || '—')} · ${esc(f.vergiDairesi || '—')}</small></div>
      <table><thead><tr><th>#</th><th>Açıklama</th><th class="e">Tutar (₺)</th></tr></thead><tbody>${f.kalemler.map((k, i) => `<tr><td>${i + 1}</td><td>${esc(k.aciklama)}</td><td class="e">${esc(k.tutarTr)}</td></tr>`).join('')}</tbody><tfoot><tr><td colspan="2" class="e">Matrah</td><td class="e">${esc(f.matrahTr)}</td></tr><tr><td colspan="2" class="e">KDV %${Number(f.kdvOran).toLocaleString('tr-TR')}</td><td class="e">${esc(f.kdvTr)}</td></tr><tr class="toplam"><td colspan="2" class="e">GENEL TOPLAM</td><td class="e">${esc(f.toplamTr)}</td></tr></tfoot></table>
      <div class="text-muted mt-2" style="font-size:.6rem">Bu belge ön yüz örneğidir; cari hesabına ${esc(f.tarih)} tarihinde ${esc(f.toplamTr)} tutarında Fatura hareketi (${esc(f.islemTipi)}) olarak işlenmiştir. ${esc(f.zaman)}</div></div>`;
  }

  // ---- başlat
  function baslat() {
    $('#thKurlar').innerHTML = Object.keys(V.kurlar).filter(p => p !== 'TL').map(p => `<span>${p} ${Number(V.kurlar[p]).toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>`).join('') + `<span class="text-muted" style="background:none;border:none">kur ${esc(V.kurTarihi)} · vade farkı %${Number(V.vadeFarkiAylikOran).toLocaleString('tr-TR')} / ay · KDV %${Number(V.kdvOran)}</span>`;
    $('#thTarih').value = bugunIso;
    cariDoldur(); rehber(); ozet(); cizListe(); cizFaturaListe(); satirCiz();
    $('#thCariAra').addEventListener('input', cariDoldur);
    $('#thCari').addEventListener('change', cariSec);
    $('#thPb').addEventListener('change', async () => { const pb = $('#thPb').value; for (const s of SAT) { s.pb = pb; s.kurElle = false; if (pb === 'TL') s.kur = 1; else await kurOto(s); } FAT.forEach(f => { f.sec = true; f.sinir = 0; }); satirCiz(); onizle(); });
    $('#thTarih').addEventListener('change', onizle); $('#thTip').addEventListener('change', onizle);
    $$('[data-ekle]').forEach(b => b.addEventListener('click', () => satirEkle(b.dataset.ekle))); $('#thSatirTemizle').addEventListener('click', () => { SAT = []; satirCiz(); onizle(); });
    $$('#thMod button').forEach(b => b.addEventListener('click', () => { MOD = b.dataset.mod; $$('#thMod button').forEach(x => x.classList.toggle('aktif', x === b)); cizFaturalar(); onizle(); }));
    $('#thHepsi').addEventListener('click', () => { FAT.forEach(f => f.sec = true); onizle(); }); $('#thHicbiri').addEventListener('click', () => { FAT.forEach(f => f.sec = false); onizle(); }); $('#thFatHepsi').addEventListener('change', e => { FAT.forEach(f => f.sec = e.target.checked); onizle(); });
    $('#thTutarDoldur').addEventListener('click', () => { const c = cari(); if (!c) return; const pb = $('#thPb').value; const t = FAT.filter(f => f.pb === pb && (MOD === 'Otomatik' || f.sec)).reduce((a, f) => a + (MOD === 'Manuel' && f.sinir ? Math.min(f.sinir, f.kalan) : f.kalan), 0); if (!t) { mesaj('warning', 'Seçili açık fatura yok.'); return; } if (!SAT.length) satirEkle('Havale / EFT'); const dagitilmis = SAT.slice(0, -1).reduce((a, s) => a + s.tutar, 0); SAT[SAT.length - 1].tutar = Math.max(0, Math.round((t - dagitilmis) * 100) / 100); satirCiz(); onizle(); });
    $('#thKaydetIsle').addEventListener('click', () => kaydet(true)); $('#thKaydetTaslak').addEventListener('click', () => kaydet(false));
    $('#thSimPdf').addEventListener('click', () => { const c = cari(); if (!c || !SAT.length) { mesaj('warning', 'Cari ve ödeme satırı girin.'); return; } indir('/Tahsilat/SimulasyonPdf', istek(false), `TahsilatSimulasyon_${c.kod}.pdf`); });
    $('#thSimExcel').addEventListener('click', () => { const c = cari(); if (!c || !SAT.length) { mesaj('warning', 'Cari ve ödeme satırı girin.'); return; } indir('/Tahsilat/SimulasyonExcel', istek(false), `TahsilatSimulasyon_${c.kod}.xlsx`); });
    $('#thKff').addEventListener('click', () => faturaKes('Kur Farkı')); $('#thVff').addEventListener('click', () => faturaKes('Vade Farkı')); $('#thYeni').addEventListener('click', yeni);
    $('#thListeAra').addEventListener('input', cizListe); $('#thListeDurum').addEventListener('change', cizListe);
    $('#thListeExcel').addEventListener('click', () => csvIndir('tahsilatlar', ['Tahsilat No', 'Tarih', 'Cari Kodu', 'Cari', 'Yöntem', 'Satır', 'İşlem Tipi', 'Para Birimi', 'Tutar', 'Kur', 'TL Karşılığı', 'Mod', 'Durum', 'Kapanan Fatura', 'Avans', 'Kur Farkı (₺)', 'Vade Farkı (₺)', 'Kur Farkı Faturası', 'Vade Farkı Faturası', 'Açıklama', 'Kim', 'Zaman', 'İşleme Alan', 'İşleme Zamanı'], V.tahsilatlar.map(t => [t.no, t.tarih, t.cariKodu, t.cariAd, t.yontem, t.satirAdet, t.islemTipi, t.paraBirimi, t.tutar, t.kur, t.tutarTl, t.mod, t.durum, [...new Set(t.dagitim.map(d => d.belgeNo))].join(' | '), t.avans, t.kurFarki, t.vadeFarki, t.kurFarkiFaturaNo, t.vadeFarkiFaturaNo, t.aciklama, t.kullanici, t.zaman, t.islemeAlan, t.islemeZamani])));
    $('#thYazdir').addEventListener('click', () => { if ($('#thFaturaOnizle .th-fatura')) setTimeout(() => window.print(), 50); });
    $$('#thSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    const q = new URLSearchParams(location.search); const kod = kok.dataset.kod || q.get('kod'); if (kod && V.cariler.some(c => c.kod === kod)) { $('#thCari').value = kod; cariSec(); }
    const no = kok.dataset.no || q.get('no'); if (no && V.tahsilatlar.some(t => t.no === no)) { sekme('liste'); detay(no); }
  }
  fetch('/Tahsilat/Veri').then(r => r.json()).then(j => { V = j; baslat(); }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
