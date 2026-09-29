// ============================================================
// NUMUNE GÖNDERİM TAKİP RAPORU — ekran betiği (Views/NumuneGonderim/Index.cshtml; veri /NumuneGonderim/Veri; kaynaklar Data/NumuneOrnek.cs + Data/CariKartOrnek.cs)
// Sekme sırası: Aylık Numune Özeti (varsayılan açılış) → Müşteri Bazlı → Numune Listesi
// Aylık Numune Özeti : yıl + şirket + bölge + temsilci; KPI; "Aylık Numune Gönderimi ve Sonuç" (gönderim adedi, yurtiçi / yurtdışı, numune bedeli, gönderim
//                      masrafı, toplam maliyet, teslim edilen, teklife / siparişe dönen, dönüşüm, sipariş tutarı, ort. teslim süresi) 12 ay + Toplam + Aylık Ort.;
//                      satış temsilcisi ve kargo firması tabloları; sonuç dağılımı; Excel. Sade tutulur: şirket bazlı kırılım yoktur (temsilciden anlaşılır).
// Müşteri Bazlı      : cari kart sütunları (CARİ_MASTER) + yıl / şirket bazında numune, bedel, masraf, sonuç; satır → cari kart + numune geçmişi drawer'ı; Excel
// Numune Listesi     : dönem + şirket + durum + sonuç + kargo + gönderim türü + amaç + masraf kime + cari kart filtreleri; KPI (tıklayınca filtre); Şirket × Bölge
//                      ve Ay matrisleri; kargo dağılımı; en çok numune gönderilen 10 müşteri; satır → gönderim detayı (kalemler, kargo takibi, hareketler, sonuç, cari kart)
// Kural: siparişe dönen numune gönderim ayına yazılır; iptal gönderimler toplama girmez; Aylık Ort. = Toplam / veri olan ay sayısı.
// Cari kart rozetleri / CARİ_MASTER bloğu Müşteri Cari Kartları ekranıyla (wwwroot/js/cari-kart.js) ve Müşteri Teklifleri ile aynı.
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Kargoya Verildi", "Siparişe Döndü").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('ng'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const bh = s => { s = String(s ?? ''); return s ? s.charAt(0).toLocaleUpperCase('tr-TR') + s.slice(1) : s; };
  const tl = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const ond = (v, n) => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: n, maximumFractionDigits: n });
  const yuzde = (pay, payda) => payda ? (pay / payda * 100).toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%' : '—';
  const oranRozet = (pay, payda) => { if (!payda) return '<span class="text-muted">—</span>'; const o = pay / payda; return `<span class="ng-oran ${o >= .3 ? 'iyi' : o >= .15 ? 'orta' : 'dusuk'}">${yuzde(pay, payda)}</span>`; };
  const iso = d => d.toISOString().slice(0, 10);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const durumRozet = d => rozet(d, d === 'Teslim Edildi' ? 'ok' : d === 'Teslim Edilemedi' || d === 'İptal' ? 'hata' : d === 'Hazırlanıyor' ? 'bilgi' : 'bekle');
  const sonucRozet = d => rozet(d, d === 'Siparişe Döndü' || d === 'Onaylandı' ? 'ok' : d === 'Teklif Verildi' ? 'bilgi' : d === 'Reddedildi' ? 'hata' : d === 'Sonuçsuz' ? 'notr' : 'bekle');
  const kartRozet = d => rozet(d, d === 'Aktif' ? 'ok' : 'notr');
  const bolgeRozet = b => `<span class="ng-bolge ${b === 'Yurtdışı' ? 'disi' : 'ici'}">${esc(b)}</span>`;
  const kargoRozet = k => `<span class="ng-kargo ${k === 'DHL Express' ? 'dhl' : k === 'UPS' ? 'ups' : k === 'FedEx' ? 'fedex' : k === 'TNT' ? 'tnt' : k === 'Turkish Cargo' ? 'thy' : k === 'Şirket Aracı' ? 'arac' : ''}">${esc(k)}</span>`;
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const sekmeli = kok.dataset.sekme === '1';
  const AYLAR = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
  let V = null, L = [], CARI = {};
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 8000); }
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + iso(new Date()) + '.csv'; a.click(); };
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');
  const gecerli = t => t.durum !== 'İptal';
  const teslim = t => t.durum === 'Teslim Edildi';
  const yolda = t => ['Hazırlanıyor', 'Kargoya Verildi', 'Yolda', 'Gümrükte'].includes(t.durum);
  const siparis = t => t.sonuc === 'Siparişe Döndü';
  const teklif = t => t.sonuc === 'Teklif Verildi' || t.sonuc === 'Siparişe Döndü';
  const bedel = l => l.reduce((s, t) => s + Number(t.numuneBedeli), 0);
  const masraf = l => l.reduce((s, t) => s + Number(t.toplamMasraf), 0);
  const gonderenMasraf = l => l.reduce((s, t) => s + (t.masrafKime === 'Alıcı' ? Number(t.gumrukMasrafi) + Number(t.sigortaMasrafi) : Number(t.toplamMasraf)), 0);
  const satisTl = l => l.reduce((s, t) => s + Number(t.ilgiliSatisTl), 0);
  const ortTeslim = l => { const d = l.filter(t => t.teslimSuresi !== null && t.teslimSuresi !== undefined); return d.length ? d.reduce((s, t) => s + t.teslimSuresi, 0) / d.length : null; };
  const cari = kod => CARI[kod] || {};
  const iliskiRozet = i => `<span class="ck-tip ${i === 'Grup İçi' ? 'grup' : i === 'İlişkili Cari' ? 'iliskili' : ''}">${esc(i || '—')}</span>`;
  const rolRozet = r => `<span class="ck-tip ${r === 'Çift Rol' ? 'cift' : ''}">${esc(r || '—')}</span>`;
  const sinifRozet = s => `<span class="ck-tip ${s === 'Özel Takip' ? 'ozel' : ''}">${esc(s || '—')}</span>`;
  const musteriHtml = t => { const c = cari(t.cariKodu); return `<b>${esc(t.cari)}</b><small>${esc(c.resmiUnvan || '')}</small>`; };
  const sinifIliskiHtml = t => { const c = cari(t.cariKodu); return `${sinifRozet(c.sinif)} ${iliskiRozet(c.iliskiDurumu)}`; };
  const faaliyetVar = (c, ana, grup, temsilci) => (c.faaliyetler || []).some(f => (!ana || f.anaGrup === ana) && (!grup || f.grup === grup) && (!temsilci || f.satisTemsilcisi === temsilci));

  // ---- sekmeler
  function sekme(p) {
    $$('#ngSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('#ng > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    if (V) { if (p === 'ozet') cizOzet(); if (p === 'musteri') cizMusteri(); if (p === 'liste') ciz(); }
    try { const u = new URL(location.href); if (p === 'ozet') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }

  // ---- ortak yardımcılar (yıl bazlı)
  const yilVerisi = (yil, sf, bf, tf) => V.numuneler.filter(t => gecerli(t) && t.tarih.slice(0, 4) === String(yil) && (!sf || t.sirket === sf) && (!bf || t.bolge === bf) && (!tf || t.temsilci === tf));
  const veriAyi = l => new Set(l.map(t => t.tarih.slice(5, 7))).size;
  const aylikTutar = (l, deger) => { const a = new Array(12).fill(0); l.forEach(t => a[+t.tarih.slice(5, 7) - 1] += Number(deger(t)) || 0); return a; };
  const aylikAdet = (l, f) => { const a = new Array(12).fill(0); l.forEach(t => { if (!f || f(t)) a[+t.tarih.slice(5, 7) - 1]++; }); return a; };
  const hucre = v => v ? kisa(v) : '<span class="text-muted">—</span>';
  /// tip: tl | adet | oran (yüzde) | bolum (pay / payda, ör. ortalama) | gun (pay / payda, 1 ondalık)
  function satirHtml(ad, a, ayN, cls, tip, ipucu) {
    if (tip === 'oran' || tip === 'bolum' || tip === 'gun') {
      const pay = a.reduce((s, x) => s + x.pay, 0), payda = a.reduce((s, x) => s + x.payda, 0);
      const f = (p, q) => q ? (tip === 'oran' ? yuzde(p, q) : tip === 'gun' ? ond(p / q, 1) : kisa(p / q)) : '<span class="text-muted">—</span>';
      return `<tr class="${tip === 'oran' ? 'oran' : ''} ${cls || ''}"><td class="ad" title="${esc(ipucu || ad)}">${esc(ad)}</td>${a.map(x => `<td class="s">${f(x.pay, x.payda)}</td>`).join('')}<td class="s top">${f(pay, payda)}</td><td class="s ort">${f(pay, payda)}</td></tr>`;
    }
    const top = a.reduce((s, v) => s + v, 0);
    const f = tip === 'adet' ? (v => v ? kisa(v) : '<span class="text-muted">—</span>') : hucre;
    return `<tr class="${cls || ''}"><td class="ad" title="${esc(ipucu || ad)}">${esc(ad)}</td>${a.map(v => `<td class="s">${f(v)}</td>`).join('')}<td class="s top">${f(top)}</td><td class="s ort">${ayN && top ? (tip === 'adet' ? ond(top / ayN, 1) : kisa(top / ayN)) : '<span class="text-muted">—</span>'}</td></tr>`;
  }
  const grupHtml = (ad, not) => `<tr class="grp"><td class="ad">${esc(ad)}</td><td colspan="14">${not ? `<small>${esc(not)}</small>` : ''}</td></tr>`;
  const oranAdet = (l, f) => { const p = aylikAdet(l, f), q = aylikAdet(l); return p.map((v, i) => ({ pay: v, payda: q[i] })); };
  const bolumDizisi = (l, pay, payda) => { const p = aylikTutar(l, pay), q = aylikTutar(l, payda); return p.map((v, i) => ({ pay: v, payda: q[i] })); };
  function yilSecenekleri(sel) {
    const yillar = [...new Set(V.numuneler.map(t => t.tarih.slice(0, 4)))].sort().reverse(); if (!yillar.includes(String(V.yil))) yillar.unshift(String(V.yil));
    sel.innerHTML = yillar.map(y => `<option value="${y}">${y}</option>`).join(''); sel.value = String(V.yil);
  }
  function tabloCsv(tabloId, ad, yil) {
    const bas = ['Açıklama', ...AYLAR, 'Toplam ' + yil, 'Aylık Ort.'];
    const rows = $$('#' + tabloId + ' tbody tr').filter(tr => !tr.classList.contains('ara')).map(tr => { const td = $$('td', tr); if (tr.classList.contains('grp')) return [td[0].textContent.trim()]; return td.map(x => x.textContent.trim().replace('—', '')); });
    csvIndir(ad + '_' + yil, bas, rows);
  }
  // Sade aylık özet: adet (yurtiçi / yurtdışı), bedel, masraf, toplam maliyet, teslim, teklife / siparişe dönen, dönüşüm, sipariş tutarı, ort. teslim süresi
  function ozetSatirlari(l, ayN) {
    return [
      satirHtml('Gönderilen Numune Adedi', aylikAdet(l), ayN, 'top', 'adet', 'İptal edilmemiş tüm gönderimler'),
      satirHtml('Yurtiçi', aylikAdet(l, t => t.bolge === 'Yurtiçi'), ayN, 'r', 'adet'),
      satirHtml('Yurtdışı', aylikAdet(l, t => t.bolge === 'Yurtdışı'), ayN, 'r', 'adet'),
      satirHtml('Numune Bedeli (Gerçek Bedel)', aylikTutar(l, t => t.numuneBedeli), ayN, 'top', 'tl', 'Kalemlerin KDV hariç liste fiyatıyla bedeli, TL'),
      satirHtml('Gönderim Masrafı (Kargo + Gümrük + Sigorta)', aylikTutar(l, t => t.toplamMasraf), ayN, 'top', 'tl'),
      satirHtml('Toplam Numune Maliyeti (Bedel + Masraf)', aylikTutar(l, t => Number(t.numuneBedeli) + Number(t.toplamMasraf)), ayN, 'genel', 'tl'),
      satirHtml('Teslim Edilen Adet', aylikAdet(l, teslim), ayN, '', 'adet'),
      satirHtml('Teklife Dönen Adet', aylikAdet(l, teklif), ayN, '', 'adet', 'Numune tesliminden sonraki 45 gün içinde aynı faaliyet grubunda teklif verilen (siparişe dönenler dahil)'),
      satirHtml('Siparişe Dönen Adet', aylikAdet(l, siparis), ayN, 'top', 'adet', 'Bağlı teklif satışa döndü; gönderim ayına yazılır'),
      satirHtml('Siparişe Dönüşüm Oranı', oranAdet(l, siparis), ayN, '', 'oran', 'Siparişe Dönen / Gönderilen Numune'),
      satirHtml('Siparişe Dönen Numunelerin Sipariş Tutarı', aylikTutar(l, t => t.ilgiliSatisTl), ayN, 'top', 'tl', 'Bağlı teklifin sipariş tutarı (TL)'),
      satirHtml('Ortalama Teslim Süresi (Gün)', bolumDizisi(l, t => teslim(t) ? t.teslimSuresi : 0, t => teslim(t) ? 1 : 0), ayN, 'r', 'gun', 'Teslim edilen gönderilerde kargoya veriliş → teslim')
    ];
  }

  // ---- sekme 1: aylık özet
  function cizOzet() {
    const yil = $('#ngOYil').value, sf = $('#ngOSirket').value, bf = $('#ngOBolge').value, tf = $('#ngOTemsilci').value, l = yilVerisi(yil, sf, bf, tf), ayN = veriAyi(l);
    const ici = l.filter(t => t.bolge === 'Yurtiçi'), disi = l.filter(t => t.bolge === 'Yurtdışı'), sp = l.filter(siparis), yl = l.filter(yolda), ot = ortTeslim(l);
    $('#ngOYilBaslik').textContent = yil; $$('#ngAylik thead .yil').forEach(th => th.textContent = 'Toplam ' + yil);
    const kpi = [['#0f172a', 'Gönderilen Numune', l.length + ' Adet', `${kisa(bedel(l))} TL Bedel · ${new Set(l.map(t => t.cariKodu)).size} Müşteri`],
      ['#0f766e', 'Yurtiçi', ici.length + ' Adet', `${kisa(bedel(ici))} TL Bedel · ${kisa(masraf(ici))} TL Masraf`],
      ['#1d4ed8', 'Yurtdışı', disi.length + ' Adet', `${kisa(bedel(disi))} TL Bedel · ${kisa(masraf(disi))} TL Masraf`],
      ['#b45309', 'Gönderim Masrafı', kisa(masraf(l)) + ' TL', `Kargo ${kisa(l.reduce((s, t) => s + Number(t.kargoMasrafi), 0))} · Gümrük ${kisa(l.reduce((s, t) => s + Number(t.gumrukMasrafi), 0))} · Ort. ${kisa(l.length ? masraf(l) / l.length : 0)} TL`],
      ['#7c3aed', 'Yolda / Teslim Bekleyen', yl.length + ' Adet', `${l.filter(teslim).length} Teslim Edildi · ${l.filter(t => t.durum === 'Teslim Edilemedi').length} Teslim Edilemedi · Ort. Teslim ${ot === null ? '—' : ond(ot, 1) + ' Gün'}`],
      ['#166534', 'Siparişe Dönen', sp.length + ' Adet', `Dönüşüm ${yuzde(sp.length, l.length)} · Sipariş ${kisa(satisTl(l))} TL · Teklif Verildi ${l.filter(t => t.sonuc === 'Teklif Verildi').length}`]];
    $('#ngOKpi').innerHTML = kpi.map(x => `<div style="--k:${x[0]};cursor:default"><div class="l">${x[1]}</div><div class="v">${x[2]}</div><div class="s">${esc(bh(x[3]))}</div></div>`).join('');
    $('#ngAylik tbody').innerHTML = l.length ? ozetSatirlari(l, ayN).join('') : '<tr><td colspan="15" class="na-bos">Bu yıl için numune kaydı yok.</td></tr>';
    // kargo firması
    const kg = {}; l.forEach(t => { const kk = t.kargoFirmasi + '|' + t.gonderimTuru; const p = kg[kk] || (kg[kk] = { ad: t.kargoFirmasi, tur: t.gonderimTuru, n: 0, ici: 0, disi: 0, kargo: 0, diger: 0, ts: 0, tn: 0, teslim: 0, sorun: 0 }); p.n++; if (t.bolge === 'Yurtiçi') p.ici++; else p.disi++; p.kargo += Number(t.kargoMasrafi); p.diger += Number(t.gumrukMasrafi) + Number(t.sigortaMasrafi); if (teslim(t)) { p.teslim++; p.ts += t.teslimSuresi; p.tn++; } if (t.durum === 'Teslim Edilemedi') p.sorun++; });
    const kk = Object.keys(kg).sort((x, y) => kg[y].n - kg[x].n);
    $('#ngKargo tbody').innerHTML = kk.map(x => { const p = kg[x]; return `<tr><td>${kargoRozet(p.ad)}</td><td>${esc(p.tur)}</td><td class="s">${p.n}</td><td class="s">${p.ici} / ${p.disi}</td><td class="s">${tl(p.kargo)}</td><td class="s">${p.diger ? tl(p.diger) : '<span class="text-muted">—</span>'}</td><td class="s">${tl((p.kargo + p.diger) / p.n)}</td><td class="s">${p.tn ? ond(p.ts / p.tn, 1) : '—'}</td><td class="s">${p.teslim}</td><td class="s">${p.sorun || '<span class="text-muted">—</span>'}</td></tr>`; }).join('') || '<tr><td colspan="10" class="na-bos">Kayıt yok.</td></tr>';
    $('#ngKargo tfoot').innerHTML = l.length ? `<tr><td>Toplam</td><td></td><td class="s">${l.length}</td><td class="s">${ici.length} / ${disi.length}</td><td class="s">${tl(l.reduce((s, t) => s + Number(t.kargoMasrafi), 0))}</td><td class="s">${tl(l.reduce((s, t) => s + Number(t.gumrukMasrafi) + Number(t.sigortaMasrafi), 0))}</td><td class="s">${tl(masraf(l) / l.length)}</td><td class="s">${ot === null ? '—' : ond(ot, 1)}</td><td class="s">${l.filter(teslim).length}</td><td class="s">${l.filter(t => t.durum === 'Teslim Edilemedi').length}</td></tr>` : '';
    // temsilci
    const tem = {}; l.forEach(t => { const p = tem[t.temsilci] || (tem[t.temsilci] = { n: 0, bedel: 0, masraf: 0, teklif: 0, sip: 0, satis: 0, m: new Set() }); p.n++; p.bedel += Number(t.numuneBedeli); p.masraf += Number(t.toplamMasraf); if (teklif(t)) p.teklif++; if (siparis(t)) { p.sip++; p.satis += Number(t.ilgiliSatisTl); } p.m.add(t.cariKodu); });
    const tk = Object.keys(tem).sort((x, y) => tem[y].n - tem[x].n);
    $('#ngTemsilci tbody').innerHTML = tk.map(x => `<tr><td><b>${esc(x)}</b></td><td class="s">${tem[x].n}</td><td class="s">${tl(tem[x].bedel)}</td><td class="s">${tl(tem[x].masraf)}</td><td class="s">${tem[x].teklif}</td><td class="s">${tem[x].sip}</td><td class="s">${oranRozet(tem[x].sip, tem[x].n)}</td><td class="s">${tem[x].satis ? tl(tem[x].satis) : '<span class="text-muted">—</span>'}</td><td class="s">${tem[x].m.size}</td></tr>`).join('') || '<tr><td colspan="9" class="na-bos">Kayıt yok.</td></tr>';
    $('#ngTemsilci tfoot').innerHTML = l.length ? `<tr><td>Toplam</td><td class="s">${l.length}</td><td class="s">${tl(bedel(l))}</td><td class="s">${tl(masraf(l))}</td><td class="s">${l.filter(teklif).length}</td><td class="s">${sp.length}</td><td class="s">${oranRozet(sp.length, l.length)}</td><td class="s">${tl(satisTl(l))}</td><td class="s">${new Set(l.map(t => t.cariKodu)).size}</td></tr>` : '';
    // sonuç dağılımı
    const sn = {}; l.forEach(t => { sn[t.sonuc] = (sn[t.sonuc] || 0) + 1; });
    const sk = V.sonuclar.filter(s => sn[s]), maks = Math.max(1, ...sk.map(s => sn[s]));
    $('#ngOSonuc').innerHTML = sk.map(s => `<div class="ng-bar"><span>${sonucRozet(s)}</span><span class="b"><i style="width:${Math.max(2, sn[s] / maks * 100)}%"></i></span><span class="v">${sn[s]} <small>%${(sn[s] / l.length * 100).toFixed(1)}</small></span></div>`).join('') || '<div class="na-bos">Kayıt yok.</div>';
    $('#ngONot').textContent = `${yil}${sf ? ' · ' + sf : ''}${bf ? ' · ' + bf : ''}${tf ? ' · ' + tf : ''} · ${l.length} gönderim · ${sp.length} siparişe döndü · veri olan ay sayısı ${ayN} · Aylık Ort. = Toplam / ${ayN || '—'} · tutarlar TL, KDV hariç · siparişe dönen numuneler gönderim ayına yazılır`;
  }

  // ---- sekme 2: müşteri bazlı
  function musteriOzeti(yil, sf) {
    const l = V.numuneler.filter(t => gecerli(t) && (!yil || t.tarih.slice(0, 4) === String(yil)) && (!sf || t.sirket === sf));
    const m = {};
    l.forEach(t => { const p = m[t.cariKodu] || (m[t.cariKodu] = { kod: t.cariKodu, n: 0, ici: 0, disi: 0, bedel: 0, masraf: 0, teslim: 0, teklif: 0, sip: 0, satis: 0, son: '', sonTr: '', sirketler: new Set() }); p.n++; if (t.bolge === 'Yurtiçi') p.ici++; else p.disi++; p.bedel += Number(t.numuneBedeli); p.masraf += Number(t.toplamMasraf); if (teslim(t)) p.teslim++; if (teklif(t)) p.teklif++; if (siparis(t)) { p.sip++; p.satis += Number(t.ilgiliSatisTl); } if (t.tarih > p.son) { p.son = t.tarih; p.sonTr = t.tarihTr; } p.sirketler.add(t.sirket); });
    return Object.values(m);
  }
  function cizMusteri() {
    const yil = $('#ngMYil').value, sf = $('#ngMSirket').value, v = id => $(id).value, ara = ($('#ngMAra').value || '').toLocaleLowerCase('tr-TR');
    let l = musteriOzeti(yil, sf).map(p => ({ ...p, c: cari(p.kod) })).filter(p => { const c = p.c; return (!v('#ngMBolge') || c.bolge === v('#ngMBolge')) && (!v('#ngMUlke') || c.ulke === v('#ngMUlke')) && (!v('#ngMSinif') || c.sinif === v('#ngMSinif')) && (!v('#ngMIliski') || c.iliskiDurumu === v('#ngMIliski')) && (!v('#ngMRol') || c.rol === v('#ngMRol')) && (!v('#ngMDurum') || c.durum === v('#ngMDurum')) && (!v('#ngMErp') || c.sirket === v('#ngMErp'))
      && (!v('#ngMAna') || faaliyetVar(c, v('#ngMAna'))) && (!v('#ngMGrup') || faaliyetVar(c, v('#ngMAna'), v('#ngMGrup'))) && (!v('#ngMTemsilci') || faaliyetVar(c, '', '', v('#ngMTemsilci')))
      && (!ara || [p.kod, c.kisaAd, c.resmiUnvan, c.vergiNo, c.yetkili, c.eposta, c.sehir, c.esCariKodu].join(' ').toLocaleLowerCase('tr-TR').includes(ara)); });
    const s = $('#ngMSira').value;
    l.sort((a, b) => s === 'kod' ? a.kod.localeCompare(b.kod) : s === 'adet' ? b.n - a.n : s === 'masraf' ? b.masraf - a.masraf : s === 'oran' ? (b.n ? b.sip / b.n : 0) - (a.n ? a.sip / a.n : 0) : b.bedel - a.bedel);
    $('#ngMusteriTablo tbody').innerHTML = l.map(p => { const c = p.c; return `<tr class="tik" data-kod="${esc(p.kod)}"><td><b>${esc(p.kod)}</b></td><td><b>${esc(c.kisaAd || '')}</b><br><small class="text-muted">${esc(c.resmiUnvan || '')}</small></td><td>${esc(c.vergiNo || '')}<br><small class="text-muted">${esc(c.vergiDairesi || '—')}</small></td><td>${esc(c.kartTipi || '')}</td><td>${esc(c.bolge || '')} · ${esc(c.ulke || '')}${c.sehir ? ' · ' + esc(c.sehir) : ''}</td><td>${sinifRozet(c.sinif)}</td><td>${iliskiRozet(c.iliskiDurumu)}</td><td>${rolRozet(c.rol)}${c.esCariKodu ? `<br><small class="text-muted">eş ${esc(c.esCariKodu)}</small>` : ''}</td><td>${bos(c.anaFaaliyet)}</td><td class="s">${(c.faaliyetler || []).length}</td><td><small>${bos(c.temsilciler)}</small></td><td>${bos(c.vadeSarti)}</td><td>${kartRozet(c.durum || 'Aktif')}</td><td>${bos(c.sirket)}<small>Gönderen: ${esc([...p.sirketler].join(', '))}</small></td><td class="s"><b>${p.n}</b><small>${p.ici} İçi / ${p.disi} Dışı</small></td><td class="s">${tl(p.bedel)}</td><td class="s">${tl(p.masraf)}</td><td class="s">${p.teslim}</td><td class="s">${p.teklif}</td><td class="s">${p.sip}</td><td class="s">${oranRozet(p.sip, p.n)}</td><td class="s">${p.satis ? tl(p.satis) : '<span class="text-muted">—</span>'}</td><td>${esc(p.sonTr)}</td></tr>`; }).join('') || '<tr><td colspan="23" class="na-bos">Filtreye uyan müşteri yok.</td></tr>';
    const n = l.reduce((s, p) => s + p.n, 0), sp = l.reduce((s, p) => s + p.sip, 0);
    $('#ngMTopAdet').textContent = n; $('#ngMTopBedel').textContent = tl(l.reduce((s, p) => s + p.bedel, 0)); $('#ngMTopMasraf').textContent = tl(l.reduce((s, p) => s + p.masraf, 0)); $('#ngMTopTeslim').textContent = l.reduce((s, p) => s + p.teslim, 0); $('#ngMTopTeklif').textContent = l.reduce((s, p) => s + p.teklif, 0); $('#ngMTopSiparis').textContent = sp; $('#ngMTopOran').innerHTML = oranRozet(sp, n); $('#ngMTopSatis').textContent = tl(l.reduce((s, p) => s + p.satis, 0));
    $('#ngMSayi').textContent = `${l.length} müşteri · ${yil}${sf ? ' · ' + sf : ''} · satıra tıklayınca cari kart bilgileri ve numune geçmişi açılır`;
    $('#ngNMusteri').textContent = l.length;
    $$('#ngMusteriTablo tr.tik').forEach(r => r.addEventListener('click', () => cariDrawer(r.dataset.kod)));
    cizMusteri.son = l;
  }
  function musteriExcel() {
    const l = cizMusteri.son || [];
    csvIndir('musteri_bazli_numune_ozeti_' + $('#ngMYil').value, ['Cari Kodu', 'Kısa Ad', 'Kısaltmalı Ünvan', 'Resmi Ünvan', 'Vergi No', 'Vergi Dairesi', 'Kart Tipi', 'Bölge', 'Ülke', 'Şehir', 'Sınıf', 'İlişki Durumu', 'Rol', 'Eş Cari', 'Yetkili', 'Telefon', 'E-posta', 'Vade', 'Durum', 'Ana Faaliyet', 'Faaliyet Grupları', 'Satış Temsilcileri', 'Şirket', 'Not', 'Gönderen Şirketler', 'Gönderim', 'Yurtiçi', 'Yurtdışı', 'Numune Bedeli (TL)', 'Gönderim Masrafı (TL)', 'Teslim Edilen', 'Teklife Dönen', 'Siparişe Dönen', 'Dönüşüm', 'Sipariş Tutarı (TL)', 'Son Gönderim'],
      l.map(p => { const c = p.c; return [p.kod, c.kisaAd, c.kisaltmaliUnvan, c.resmiUnvan, c.vergiNo, c.vergiDairesi, c.kartTipi, c.bolge, c.ulke, c.sehir, c.sinif, c.iliskiDurumu, c.rol, c.esCariKodu, c.yetkili, c.telefon, c.eposta, c.vadeSarti, c.durum, c.anaFaaliyet, (c.faaliyetler || []).map(f => f.anaGrup + ' / ' + f.grup).join(' | '), c.temsilciler, c.sirket, c.not, [...p.sirketler].join(', '), p.n, p.ici, p.disi, sayi(p.bedel), sayi(p.masraf), p.teslim, p.teklif, p.sip, yuzde(p.sip, p.n), sayi(p.satis), p.sonTr]; }));
  }

  // ---- sekme 3: liste — dönem ve filtre
  function aralik() {
    const d = $('#ngDonem').value, bugun = new Date(V.bugun + 'T00:00:00');
    const ayBas = new Date(bugun.getFullYear(), bugun.getMonth(), 1);
    if (d === 'buay') return [iso(ayBas), V.bugun];
    if (d === 'gecenay') { const b = new Date(bugun.getFullYear(), bugun.getMonth() - 1, 1), e = new Date(bugun.getFullYear(), bugun.getMonth(), 0); return [iso(b), iso(e)]; }
    if (d === '30' || d === '90') { const b = new Date(bugun); b.setDate(b.getDate() - Number(d) + 1); return [iso(b), V.bugun]; }
    if (d === 'yil') return [bugun.getFullYear() + '-01-01', V.bugun];
    if (d === 'ozel') return [$('#ngBas').value || '', $('#ngBit').value || ''];
    return ['', ''];
  }
  function donemDegisti() { const ozel = $('#ngDonem').value === 'ozel'; $('#ngBas').disabled = $('#ngBit').disabled = !ozel; if (!ozel) { const [b, e] = aralik(); $('#ngBas').value = b; $('#ngBit').value = e; } ciz(); }
  function liste() {
    const ara = ($('#ngAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value, [bas, bit] = aralik();
    return V.numuneler.filter(t => { const c = cari(t.cariKodu); return (!bas || t.tarih >= bas) && (!bit || t.tarih <= bit) && (!v('#ngSirket') || t.sirket === v('#ngSirket')) && (!v('#ngDurum') || t.durum === v('#ngDurum')) && (!v('#ngSonuc') || t.sonuc === v('#ngSonuc')) && (!v('#ngKargoF') || t.kargoFirmasi === v('#ngKargoF')) && (!v('#ngTur') || t.gonderimTuru === v('#ngTur')) && (!v('#ngAmacF') || t.amac === v('#ngAmacF')) && (!v('#ngMasrafKime') || t.masrafKime === v('#ngMasrafKime'))
      && (!v('#ngTemsilciF') || t.temsilci === v('#ngTemsilciF')) && (!v('#ngAnaGrup') || t.anaGrup === v('#ngAnaGrup')) && (!v('#ngGrupF') || t.faaliyetGrubu === v('#ngGrupF'))
      && (!v('#ngSinif') || c.sinif === v('#ngSinif')) && (!v('#ngBolge') || t.bolge === v('#ngBolge')) && (!v('#ngUlke') || t.ulke === v('#ngUlke')) && (!v('#ngIliski') || c.iliskiDurumu === v('#ngIliski')) && (!v('#ngRol') || c.rol === v('#ngRol'))
      && (!ara || [t.no, t.gonderimNo, t.cari, t.cariKodu, t.konu, t.sirket, t.temsilci, t.kargoFirmasi, t.irsaliyeNo, t.proformaNo, t.ilgiliTeklifNo, t.ilgiliSiparisNo, t.aliciKisi, c.resmiUnvan, ...t.kalemler.map(k => k.urun + ' ' + k.parti)].join(' ').toLocaleLowerCase('tr-TR').includes(ara)); });
  }

  // ---- sekme 3 çizim
  function ciz() {
    L = liste();
    const g = L.filter(gecerli), ici = g.filter(t => t.bolge === 'Yurtiçi'), disi = g.filter(t => t.bolge === 'Yurtdışı'), yl = g.filter(yolda), sp = g.filter(siparis);
    const kpi = [['#0f172a', 'Gönderilen Numune', g.length + ' Adet', `${kisa(bedel(g))} TL Bedel · ${new Set(g.map(t => t.cariKodu)).size} Müşteri`, ''],
      ['#0f766e', 'Yurtiçi', ici.length + ' Adet', `${kisa(masraf(ici))} TL Masraf`, 'bolge:Yurtiçi'],
      ['#1d4ed8', 'Yurtdışı', disi.length + ' Adet', `${kisa(masraf(disi))} TL Masraf · ${disi.filter(t => t.kargoFirmasi === 'DHL Express').length} DHL`, 'bolge:Yurtdışı'],
      ['#b45309', 'Gönderim Masrafı', kisa(masraf(g)) + ' TL', `Ort. ${kisa(g.length ? masraf(g) / g.length : 0)} TL · Alıcı Ödemeli ${g.filter(t => t.masrafKime === 'Alıcı').length}`, ''],
      ['#7c3aed', 'Yolda / Teslim Bekleyen', yl.length + ' Adet', `${g.filter(t => t.durum === 'Gümrükte').length} Gümrükte · ${g.filter(t => t.durum === 'Teslim Edilemedi').length} Teslim Edilemedi`, 'yolda'],
      ['#166534', 'Siparişe Dönen', sp.length + ' Adet', `Dönüşüm ${yuzde(sp.length, g.length)} · Sipariş ${kisa(satisTl(g))} TL`, 'sonuc:Siparişe Döndü']];
    $('#ngKpi').innerHTML = kpi.map(x => `<div style="--k:${x[0]}" data-k="${esc(x[4])}" title="Tıklayınca filtre uygular"><div class="l">${x[1]}</div><div class="v">${x[2]}</div><div class="s">${esc(bh(x[3]))}</div></div>`).join('');
    $$('#ngKpi > div').forEach(x => x.addEventListener('click', () => { const k = x.dataset.k; if (!k) { $('#ngBolge').value = ''; $('#ngSonuc').value = ''; $('#ngDurum').value = ''; } else if (k === 'yolda') { $('#ngDurum').value = 'Yolda'; } else { const [a, b] = k.split(':'); $(a === 'bolge' ? '#ngBolge' : '#ngSonuc').value = b; } ciz(); }));
    $('#ngTablo tbody').innerHTML = L.map(t => `<tr class="tik" data-id="${t.id}"><td>${esc(t.tarihTr)}</td><td><b>${esc(t.no)}</b></td><td>${esc(t.sirket)}</td><td>${esc(t.cariKodu)}</td><td>${musteriHtml(t)}</td><td>${sinifIliskiHtml(t)}</td><td>${bolgeRozet(t.bolge)}<small>${esc([t.ulke, t.sehir].filter(Boolean).join(' · '))}</small></td><td>${esc(t.temsilci)}</td><td>${esc(t.amac)}</td><td>${esc(t.konu)}<small>${t.kalemSayisi} Kalem · ${ond(t.agirlik, 1)} kg</small></td><td class="s"><b>${tl(t.numuneBedeli)}</b>${t.paraBirimi !== 'TL' ? `<small>${tl(t.proformaBedeli)} ${esc(t.paraBirimi)}</small>` : ''}</td><td>${kargoRozet(t.kargoFirmasi)}<small>${esc(t.gonderimTuru)}</small></td><td class="ng-takip">${esc(t.gonderimNo)}</td><td class="s">${tl(t.toplamMasraf)}${t.masrafKime === 'Alıcı' ? '<small>Alıcı Ödemeli</small>' : t.gumrukMasrafi ? `<small>Gümrük ${kisa(t.gumrukMasrafi)}</small>` : ''}</td><td>${durumRozet(t.durum)}</td><td>${t.teslimTr ? esc(t.teslimTr) + `<small>${t.teslimSuresi} Gün</small>` : `<small>Tahmini ${esc(t.tahminiTeslimTr)}</small>`}</td><td>${sonucRozet(t.sonuc)}${t.ilgiliTeklifNo ? `<small>${esc(t.ilgiliTeklifNo)}${t.ilgiliSiparisNo ? ' · ' + esc(t.ilgiliSiparisNo) : ''}</small>` : ''}</td></tr>`).join('') || '<tr><td colspan="17" class="na-bos">Filtreye uyan gönderim yok.</td></tr>';
    $('#ngTopBedel').textContent = tl(bedel(g)) + ' TL'; $('#ngTopSayi').textContent = g.length + ' Gönderim'; $('#ngTopMasraf').textContent = tl(masraf(g)) + ' TL'; $('#ngTopSonuc').innerHTML = `${g.filter(teslim).length} Teslim · ${sp.length} Siparişe Döndü ${oranRozet(sp.length, g.length)}`;
    $('#ngSayi').textContent = `${L.length} / ${V.numuneler.length} gönderim · ${L.length - g.length} iptal (toplama girmez) · satıra tıklayınca gönderim detayı, kargo takibi ve cari kart bilgileri açılır`;
    $('#ngNListe').textContent = L.length;
    $$('#ngTablo tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    // şirket × bölge
    const m = {}; g.forEach(t => { const p = m[t.sirket] || (m[t.sirket] = { ici: 0, disi: 0, n: 0, bedel: 0, kargo: 0, diger: 0, top: 0, sip: 0 }); if (t.bolge === 'Yurtiçi') p.ici++; else p.disi++; p.n++; p.bedel += Number(t.numuneBedeli); p.kargo += Number(t.kargoMasrafi); p.diger += Number(t.gumrukMasrafi) + Number(t.sigortaMasrafi); p.top += Number(t.toplamMasraf); if (siparis(t)) p.sip++; });
    const satir = (ad, p, kalin) => `<tr class="${kalin ? 'toplam' : ''}"><td>${esc(ad)}</td><td class="s">${p.ici}</td><td class="s">${p.disi}</td><td class="s"><b>${p.n}</b></td><td class="s">${tl(p.bedel)}</td><td class="s">${tl(p.kargo)}</td><td class="s">${p.diger ? tl(p.diger) : '<span class="text-muted">—</span>'}</td><td class="s"><b>${tl(p.top)}</b></td><td class="s">${p.sip}</td><td class="s">${oranRozet(p.sip, p.n)}</td></tr>`;
    const sk = V.sirketler.filter(s => m[s]); const tm = { ici: 0, disi: 0, n: 0, bedel: 0, kargo: 0, diger: 0, top: 0, sip: 0 }; sk.forEach(s => Object.keys(tm).forEach(kk => tm[kk] += m[s][kk]));
    $('#ngMatris tbody').innerHTML = sk.length ? sk.map(s => satir(s, m[s])).join('') + satir('Konsolide Toplam', tm, true) : '<tr><td colspan="10" class="na-bos">Kayıt yok.</td></tr>';
    // ay
    const ay = {}; g.forEach(t => { const kk = t.tarih.slice(0, 7); const p = ay[kk] || (ay[kk] = { n: 0, ici: 0, disi: 0, bedel: 0, masraf: 0, teslim: 0, sip: 0, ts: 0, tn: 0 }); p.n++; if (t.bolge === 'Yurtiçi') p.ici++; else p.disi++; p.bedel += Number(t.numuneBedeli); p.masraf += Number(t.toplamMasraf); if (teslim(t)) { p.teslim++; p.ts += t.teslimSuresi; p.tn++; } if (siparis(t)) p.sip++; });
    const ayAdi = kk => AYLAR[+kk.slice(5, 7) - 1] + ' ' + kk.slice(0, 4);
    const aySatir = (ad, p, kalin) => `<tr class="${kalin ? 'toplam' : ''}"><td>${esc(ad)}</td><td class="s">${p.n}</td><td class="s">${p.ici} / ${p.disi}</td><td class="s">${tl(p.bedel)}</td><td class="s">${tl(p.masraf)}</td><td class="s">${p.teslim}</td><td class="s">${p.sip}</td><td class="s">${oranRozet(p.sip, p.n)}</td><td class="s">${p.tn ? ond(p.ts / p.tn, 1) : '—'}</td></tr>`;
    const ak = Object.keys(ay).sort((x, y) => y.localeCompare(x)); const ta = { n: g.length, ici: ici.length, disi: disi.length, bedel: bedel(g), masraf: masraf(g), teslim: g.filter(teslim).length, sip: sp.length, ts: g.filter(teslim).reduce((s, t) => s + t.teslimSuresi, 0), tn: g.filter(teslim).length };
    $('#ngAyMatris tbody').innerHTML = ak.length ? ak.map(kk => aySatir(ayAdi(kk), ay[kk])).join('') + aySatir('Toplam', ta, true) : '<tr><td colspan="9" class="na-bos">Kayıt yok.</td></tr>';
    // kargo dağılımı
    const kg = {}; g.forEach(t => { const p = kg[t.kargoFirmasi] || (kg[t.kargoFirmasi] = { n: 0, top: 0 }); p.n++; p.top += Number(t.toplamMasraf); });
    const kl = Object.keys(kg).sort((x, y) => kg[y].n - kg[x].n), maks = kl.length ? kg[kl[0]].n : 1;
    $('#ngKargoDagilim').innerHTML = kl.map(x => `<div class="ng-bar"><span>${kargoRozet(x)}</span><span class="b"><i style="width:${Math.max(2, kg[x].n / maks * 100)}%"></i></span><span class="v">${kg[x].n} <small>· ${kisa(kg[x].top)} TL</small></span></div>`).join('') || '<div class="na-bos">Kayıt yok.</div>';
    // en çok numune gönderilen müşteriler
    const mu = {}; g.forEach(t => { const p = mu[t.cariKodu] || (mu[t.cariKodu] = { ad: t.cari, n: 0, bedel: 0, masraf: 0, sip: 0 }); p.n++; p.bedel += Number(t.numuneBedeli); p.masraf += Number(t.toplamMasraf); if (siparis(t)) p.sip++; });
    $('#ngMusteriler tbody').innerHTML = Object.keys(mu).sort((x, y) => mu[y].n - mu[x].n).slice(0, 10).map((x, i) => `<tr class="tik" data-kod="${esc(x)}" style="cursor:pointer"><td>${i + 1}</td><td><b>${esc(x)}</b> · ${esc(mu[x].ad)}<small>${esc(cari(x).resmiUnvan || '')}</small></td><td>${sinifRozet(cari(x).sinif)}</td><td class="s">${mu[x].n}</td><td class="s">${tl(mu[x].bedel)}</td><td class="s">${tl(mu[x].masraf)}</td><td class="s">${mu[x].sip} ${oranRozet(mu[x].sip, mu[x].n)}</td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Kayıt yok.</td></tr>';
    $$('#ngMusteriler tr.tik').forEach(r => r.addEventListener('click', () => cariDrawer(r.dataset.kod)));
  }

  // ---- drawer: cari kart bloğu (CARİ_MASTER §3 sırası + faaliyet satırları) — Müşteri Cari Kartları ekranıyla aynı standart
  function cariKartHtml(c) {
    if (!c || !c.kod) return '<div class="na-kart"><div class="na-kart-g na-bos">Cari kart bulunamadı.</div></div>';
    const a = [['1. Cari Kodu', c.kod], ['2. Cari Kısa Adı', c.kisaAd], ['3. Kısaltmalı Ünvan', c.kisaltmaliUnvan], ['4. Resmi Ünvan', c.resmiUnvan], ['5. Vergi No / TCKN', c.vergiNo], ['6. Vergi Dairesi', c.vergiDairesi], ['7. Cari Kart Tipi', c.kartTipi], ['8. Cari Bölge', c.bolge], ['9. Ülke', c.ulke], ['10. Şehir', c.sehir],
      ['11. Cari Sınıfı', c.sinif], ['12. İlişki Durumu', c.iliskiDurumu], ['13. Cari Rolü', c.rol], ['14. Eş Cari Kodu', c.esCariKodu ? c.esCariKodu + (c.esKartAd ? ' · ' + c.esKartAd : '') : ''], ['15. Yetkili Kişi', c.yetkili], ['16. Telefon', c.telefon], ['17. E-posta', c.eposta], ['18. Vade / Ödeme Şartı', c.vadeSarti], ['19. Durum', c.durum], ['Şirket (ERP)', c.sirket]];
    const f = c.faaliyetler || [];
    return `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-database me-1"></i>CARİ_MASTER</span><span class="not">${esc(c.olusturmaTarihi || '')}${c.olusturan ? ' · ' + esc(c.olusturan) : ''}${c.kaynakTalepNo ? ' · talep ' + esc(c.kaynakTalepNo) : ''}</span></div><div class="na-kart-g"><div class="ck-master">${a.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}<div style="grid-column:1/-1"><div class="l">20. Not</div><div class="v" style="font-weight:500">${bos(c.not)}</div></div></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-diagram-project me-1"></i>CARİ_FAALİYET_EŞLEŞTİRME (${f.length})</span><span class="not">faaliyet grubu bazında satış temsilcisi · ana faaliyet tek satırda Evet</span></div><div class="na-kart-g p-0">${f.length ? `<div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>#</th><th>Faaliyet Ana Grubu</th><th>Faaliyet Grubu</th><th>Satış Temsilcisi</th><th>Ana Faaliyet</th><th>Ürün Sınıfı › Grubu › Serisi › Adı</th><th>Durum</th><th>Başlangıç</th><th>Not</th></tr></thead><tbody>${f.map(x => `<tr class="${x.anaFaaliyet ? 'table-active' : ''}"><td>${x.sira}</td><td>${esc(x.anaGrup)}</td><td><b>${esc(x.grup)}</b></td><td>${bos(x.satisTemsilcisi)}</td><td>${x.anaFaaliyet ? rozet('Evet', 'ok') : 'Hayır'}</td><td><small>${esc([x.urunSinifi, x.urunGrubu, x.urunSerisi, x.urunAdi].filter(Boolean).join(' › ') || '—')}</small></td><td>${kartRozet(x.durum)}</td><td>${bos(x.baslangicTr)}</td><td><small>${esc(x.not || '')}</small></td></tr>`).join('')}</tbody></table></div>` : '<div class="na-bos">Faaliyet eşleştirmesi yok.</div>'}</div></div>`;
  }
  function gecmisHtml(kod, seciliId) {
    const l = V.numuneler.filter(t => t.cariKodu === kod), g = l.filter(gecerli);
    return `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Müşteriye Gönderilen Numuneler</span><span class="not">Tüm dönem · ${g.length} gönderim · bedel ${tl(bedel(g))} TL · masraf ${tl(masraf(g))} TL · ${g.filter(siparis).length} siparişe döndü</span></div><div class="na-kart-g p-0"><div class="table-responsive" style="max-height:280px"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Numune No</th><th>Gönderen Şirket</th><th>Temsilci</th><th>İçerik</th><th class="s">Bedel (TL)</th><th>Kargo</th><th class="s">Masraf (TL)</th><th>Durum</th><th>Sonuç</th></tr></thead><tbody>${l.map(x => `<tr class="tik ${x.id === seciliId ? 'table-active' : ''}" data-id="${x.id}"><td>${esc(x.tarihTr)}</td><td>${esc(x.no)}</td><td>${esc(x.sirket)}</td><td>${esc(x.temsilci)}</td><td>${esc(x.konu)}</td><td class="s">${tl(x.numuneBedeli)}</td><td>${kargoRozet(x.kargoFirmasi)}</td><td class="s">${tl(x.toplamMasraf)}</td><td>${durumRozet(x.durum)}</td><td>${sonucRozet(x.sonuc)}</td></tr>`).join('')}</tbody></table></div></div></div>`;
  }
  function ac(url, baslik) { if (window.top && window.top !== window && typeof window.top.portalSekmeAc === 'function') window.top.portalSekmeAc(baslik, url); else window.open(url + (sekmeli ? (url.includes('?') ? '&' : '?') + '_sekme=1' : ''), '_blank'); }
  function drawerAksiyon(c, t) {
    const b = [`<button type="button" class="btn btn-light btn-sm py-0" data-ac="/CariKart/Musteri?kod=${encodeURIComponent(c.kod || '')}" data-b="Müşteri Cari Kartları" style="font-size:.66rem"><i class="fas fa-address-card me-1"></i>Cari Kartı Aç</button>`];
    if (t && t.gonderimNoLink) b.unshift(`<a class="btn btn-light btn-sm py-0" href="${esc(t.gonderimNoLink)}" target="_blank" rel="noopener" style="font-size:.66rem"><i class="fas fa-location-crosshairs me-1"></i>${esc(t.kargoFirmasi)} Takip</a>`);
    if (t && t.ilgiliTeklifNo) b.unshift(`<button type="button" class="btn btn-light btn-sm py-0" data-ac="/MusteriTeklif?no=${encodeURIComponent(t.ilgiliTeklifNo)}" data-b="Müşteri Teklifleri" style="font-size:.66rem"><i class="fas fa-file-signature me-1"></i>Teklif ${esc(t.ilgiliTeklifNo)}</button>`);
    $('#ngDrawerAksiyon').innerHTML = b.join('');
    $$('#ngDrawerAksiyon [data-ac]').forEach(x => x.addEventListener('click', () => ac(x.dataset.ac, x.dataset.b)));
  }
  function drawerAc(id) {
    const t = V.numuneler.find(x => x.id === id); if (!t) return; const c = cari(t.cariKodu);
    $('#ngDrawerBaslik').innerHTML = `<i class="fas fa-box-open me-1"></i>${esc(t.no)} · ${esc(t.sirket)} → ${esc(t.cari)} ${bolgeRozet(t.bolge)} ${durumRozet(t.durum)}`;
    drawerAksiyon(c, t);
    const a = [['Numune No', t.no], ['Gönderim Tarihi', t.tarihTr], ['Gönderen Şirket', t.sirket], ['Müşteri', t.cariKodu + ' · ' + t.cari], ['Cari Bölge', t.bolge + ' · ' + [t.ulke, t.sehir].filter(Boolean).join(' / ')], ['Alıcı Kişi', t.aliciKisi], ['Alıcı Adresi', t.aliciAdres], ['Alıcı Telefon', t.aliciTelefon], ['Gönderen Satış Temsilcisi', t.temsilci],
      ['Amaç', t.amac], ['Faaliyet Grubu', t.anaGrup + ' / ' + t.faaliyetGrubu], ['Konu', t.konu], ['Numune İrsaliyesi', t.irsaliyeNo], ['Proforma Fatura', t.proformaNo], ['İhracat Beyannamesi', t.beyannameNo], ['Hazırlayan', t.hazirlayan], ['Onaylayan', t.onaylayan], ['Kaynak Sistem', t.kaynak]];
    const k = [['Kargo Firması', t.kargoFirmasi], ['Gönderim Türü', t.gonderimTuru], ['Gönderim (Takip) No', t.gonderimNo], ['Ağırlık', ond(t.agirlik, 2) + ' kg'], ['Desi', ond(t.desi, 1)], ['Masraf Kime', t.masrafKime], ['Kargo Faturası', t.masrafPB !== 'TL' ? `${tl(t.masrafDoviz)} ${t.masrafPB} · kur ${tl(t.kur)}` : tl(t.kargoMasrafi) + ' TL'], ['Tahmini Teslim', t.tahminiTeslimTr], ['Teslim Tarihi', t.teslimTr ? `${t.teslimTr} (${t.teslimSuresi} gün)` : ''], ['Teslim Alan', t.teslimAlan], ['Son Hareket', t.sonHareket]];
    const s = [['Sonuç', t.sonuc], ['Geri Bildirim Tarihi', t.geriBildirimTr], ['İlgili Teklif', t.ilgiliTeklifNo], ['İlgili Sipariş', t.ilgiliSiparisNo], ['Sipariş Tutarı (TL)', t.ilgiliSatisTl ? tl(t.ilgiliSatisTl) + ' TL' : '']];
    const tipCls = h => h.tip === 'teslim' ? 'fin' : h.tip === 'gumruk' ? 'wf' : h.tip === 'kargo' ? 'muh' : h.tip === 'hata' ? '' : 'sis';
    $('#ngDrawerIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-receipt me-1"></i>Gönderim Bilgileri</span><span class="not">${esc(t.kaynak)}</span></div><div class="na-kart-g"><div class="ng-master">${a.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}<div style="grid-column:1/-1"><div class="l">Not</div><div class="v" style="font-weight:500">${bos(t.not)}</div></div></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-list-ol me-1"></i>Numune Kalemleri ve Gerçek Bedeli</span><span class="not">${t.kalemSayisi} Kalem · KDV hariç liste fiyatı${t.paraBirimi !== 'TL' ? ` · proforma ${tl(t.proformaBedeli)} ${esc(t.paraBirimi)} (kur ${tl(t.kur)})` : ''}</span></div><div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>#</th><th>Ürün</th><th>Hiyerarşi</th><th>Parti / Lot</th><th class="s">Miktar</th><th>Birim</th><th class="s">Birim Bedel (TL)</th><th class="s">Bedel (TL)</th></tr></thead><tbody>${t.kalemler.map(x => `<tr><td>${x.sira}</td><td><b>${esc(x.urun)}</b></td><td><small>${esc(x.hiyerarsi)}</small></td><td class="ng-takip">${esc(x.parti)}</td><td class="s">${ond(x.miktar, 1)}</td><td>${esc(x.birim)}</td><td class="s">${tl(x.birimBedel)}</td><td class="s">${tl(x.bedel)}</td></tr>`).join('')}</tbody><tfoot><tr><td colspan="7" style="font-weight:700">Numune Bedeli</td><td class="s" style="font-weight:700">${tl(t.numuneBedeli)} TL</td></tr></tfoot></table></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-truck-fast me-1"></i>Kargo ve Gönderim Masrafları</span><span class="not">${kargoRozet(t.kargoFirmasi)}</span></div><div class="na-kart-g"><div class="ng-masraf mb-2"><div><div class="l">Kargo</div><div class="v">${kisa(t.kargoMasrafi)} TL</div></div><div><div class="l">Gümrük / İhracat Beyanı</div><div class="v">${t.gumrukMasrafi ? kisa(t.gumrukMasrafi) + ' TL' : '—'}</div></div><div><div class="l">Sigorta</div><div class="v">${t.sigortaMasrafi ? kisa(t.sigortaMasrafi) + ' TL' : '—'}</div></div><div class="top"><div class="l">Toplam Gönderim Masrafı</div><div class="v">${kisa(t.toplamMasraf)} TL</div></div></div><div class="ng-master">${k.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${l === 'Gönderim (Takip) No' ? `<span class="ng-takip">${bos(v)}</span>` : bos(v)}</div></div>`).join('')}</div><div class="na-mini">Toplam numune maliyeti (bedel + masraf): <b>${tl(Number(t.numuneBedeli) + Number(t.toplamMasraf))} TL</b>${t.masrafKime === 'Alıcı' ? ' · kargo bedeli alıcıdan tahsil edilir' : ''}</div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-route me-1"></i>Kargo Hareketleri</span><span class="not">${t.hareketler.length} hareket</span></div><div class="na-kart-g"><ul class="na-zaman">${t.hareketler.map(h => `<li class="${tipCls(h)}"><span class="t">${esc(h.zaman)}</span><b>${esc(h.yer)}</b> · ${esc(h.aciklama)}${h.tip === 'hata' ? ' ' + rozet('Sorun', 'hata') : ''}</li>`).join('') || '<li>Henüz hareket yok.</li>'}</ul></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-flag-checkered me-1"></i>Sonuç ve Geri Bildirim</span><span class="not">${sonucRozet(t.sonuc)}</span></div><div class="na-kart-g"><div class="ng-master">${s.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}<div style="grid-column:1/-1"><div class="l">Müşteri Geri Bildirimi</div><div class="v" style="font-weight:500">${bos(t.geriBildirim)}</div></div></div></div></div>
      ${cariKartHtml(c)}${gecmisHtml(t.cariKodu, t.id)}`;
    $$('#ngDrawerIc tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    $('#ngDrawer').classList.add('acik'); $('#ngPerde').classList.add('acik'); $('#ngDrawer').scrollTop = 0;
  }
  function cariDrawer(kod) {
    const c = cari(kod); if (!c.kod) { mesaj('warning', esc(kod) + ' için cari kart bulunamadı.'); return; }
    $('#ngDrawerBaslik').innerHTML = `<i class="fas fa-address-card me-1"></i>${esc(c.kod)} · ${esc(c.kisaAd)} ${kartRozet(c.durum || 'Aktif')}`;
    drawerAksiyon(c, null);
    $('#ngDrawerIc').innerHTML = cariKartHtml(c) + gecmisHtml(kod, 0);
    $$('#ngDrawerIc tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    $('#ngDrawer').classList.add('acik'); $('#ngPerde').classList.add('acik'); $('#ngDrawer').scrollTop = 0;
  }
  function drawerKapat() { $('#ngDrawer').classList.remove('acik'); $('#ngPerde').classList.remove('acik'); }

  // ---- excel (sekme 3)
  function excel() {
    csvIndir('numune_gonderimleri', ['Tarih', 'Numune No', 'Gönderen Şirket', 'Cari Kodu', 'Müşteri', 'Resmi Ünvan', 'Vergi No', 'Cari Sınıfı', 'İlişki Durumu', 'Cari Bölge', 'Ülke', 'Şehir', 'Alıcı Kişi', 'Alıcı Adresi', 'Alıcı Telefon', 'Satış Temsilcisi', 'Amaç', 'Faaliyet Ana Grubu', 'Faaliyet Grubu', 'İçerik', 'Kalem Sayısı', 'Numune Bedeli (TL)', 'Proforma Bedeli', 'Para Birimi', 'Kur', 'Kargo Firması', 'Gönderim Türü', 'Gönderim (Takip) No', 'Ağırlık (kg)', 'Desi', 'Kargo Masrafı (TL)', 'Kargo Faturası (Döviz)', 'Masraf PB', 'Gümrük / İhracat Beyanı (TL)', 'Sigorta (TL)', 'Toplam Gönderim Masrafı (TL)', 'Masraf Kime', 'Toplam Maliyet (TL)', 'Tahmini Teslim', 'Teslim Tarihi', 'Teslim Süresi (Gün)', 'Teslim Alan', 'Durum', 'Son Hareket', 'Numune İrsaliyesi', 'Proforma No', 'İhracat Beyannamesi', 'Sonuç', 'Geri Bildirim Tarihi', 'Geri Bildirim', 'İlgili Teklif', 'İlgili Sipariş', 'Sipariş Tutarı (TL)', 'Hazırlayan', 'Onaylayan', 'Not'],
      L.map(t => { const c = cari(t.cariKodu); return [t.tarihTr, t.no, t.sirket, t.cariKodu, t.cari, c.resmiUnvan, c.vergiNo, c.sinif, c.iliskiDurumu, t.bolge, t.ulke, t.sehir, t.aliciKisi, t.aliciAdres, t.aliciTelefon, t.temsilci, t.amac, t.anaGrup, t.faaliyetGrubu, t.konu, t.kalemSayisi, sayi(t.numuneBedeli), t.paraBirimi !== 'TL' ? sayi(t.proformaBedeli) : '', t.paraBirimi, sayi(t.kur), t.kargoFirmasi, t.gonderimTuru, t.gonderimNo, sayi(t.agirlik), sayi(t.desi), sayi(t.kargoMasrafi), t.masrafPB !== 'TL' ? sayi(t.masrafDoviz) : '', t.masrafPB, sayi(t.gumrukMasrafi), sayi(t.sigortaMasrafi), sayi(t.toplamMasraf), t.masrafKime, sayi(Number(t.numuneBedeli) + Number(t.toplamMasraf)), t.tahminiTeslimTr, t.teslimTr, t.teslimSuresi ?? '', t.teslimAlan, t.durum, t.sonHareket, t.irsaliyeNo, t.proformaNo, t.beyannameNo, t.sonuc, t.geriBildirimTr, t.geriBildirim, t.ilgiliTeklifNo, t.ilgiliSiparisNo, t.ilgiliSatisTl ? sayi(t.ilgiliSatisTl) : '', t.hazirlayan, t.onaylayan, t.not]; }));
  }
  function kalemExcel() {
    const rows = []; L.forEach(t => t.kalemler.forEach(k => rows.push([t.tarihTr, t.no, t.sirket, t.cariKodu, t.cari, t.temsilci, k.sira, k.urun, k.hiyerarsi, k.parti, sayi(k.miktar), k.birim, sayi(k.birimBedel), sayi(k.bedel), t.kargoFirmasi, t.gonderimNo, t.durum, t.sonuc])));
    csvIndir('numune_kalemleri', ['Tarih', 'Numune No', 'Gönderen Şirket', 'Cari Kodu', 'Müşteri', 'Satış Temsilcisi', '#', 'Ürün', 'Hiyerarşi', 'Parti / Lot', 'Miktar', 'Birim', 'Birim Bedel (TL)', 'Bedel (TL)', 'Kargo Firması', 'Gönderim No', 'Durum', 'Sonuç'], rows);
  }
  function matrisExcel() {
    const rows = $$('#ngMatris tbody tr').map(tr => $$('td', tr).map(td => td.textContent.trim().replace('—', '')));
    csvIndir('numune_sirket_ozeti', ['Gönderen Şirket', 'Yurtiçi', 'Yurtdışı', 'Toplam Gönderim', 'Numune Bedeli', 'Kargo Masrafı', 'Gümrük + Sigorta', 'Toplam Masraf', 'Siparişe Dönen', 'Dönüşüm'], rows);
  }
  function ozetExcel() {
    const yil = $('#ngOYil').value; tabloCsv('ngAylik', 'aylik_numune_ozeti', yil);
    setTimeout(() => csvIndir('aylik_numune_ozeti_temsilci_' + yil, ['Satış Temsilcisi', 'Gönderim', 'Numune Bedeli', 'Gönderim Masrafı', 'Teklife Dönen', 'Siparişe Dönen', 'Dönüşüm', 'Sipariş Tutarı', 'Müşteri'], $$('#ngTemsilci tbody tr, #ngTemsilci tfoot tr').map(tr => $$('td', tr).map(td => td.textContent.trim().replace('—', '')))), 300);
    setTimeout(() => csvIndir('aylik_numune_ozeti_kargo_' + yil, ['Kargo Firması', 'Gönderim Türü', 'Gönderim', 'Yurtiçi / Yurtdışı', 'Kargo Masrafı', 'Gümrük + Sigorta', 'Ort. Masraf', 'Ort. Teslim (Gün)', 'Teslim Edildi', 'Sorunlu'], $$('#ngKargo tbody tr, #ngKargo tfoot tr').map(tr => $$('td', tr).map(td => td.textContent.trim().replace('—', '')))), 600);
  }

  // ---- başlat
  fetch('/NumuneGonderim/Veri').then(r => r.json()).then(v => {
    V = v; V.cariler.forEach(c => CARI[c.kod] = c);
    const doldur = (id, l) => { $(id).innerHTML = '<option value="">Tümü</option>' + l.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); };
    const tumGruplar = Object.values(V.faaliyetHiyerarsisi).flat();
    doldur('#ngDurum', V.durumlar); doldur('#ngSonuc', V.sonuclar); doldur('#ngKargoF', V.kargoFirmalari); doldur('#ngTur', V.gonderimTurleri); doldur('#ngAmacF', V.amaclar); doldur('#ngMasrafKime', V.masrafKimeler);
    doldur('#ngTemsilciF', V.temsilciler); doldur('#ngOTemsilci', V.temsilciler); doldur('#ngMTemsilci', V.temsilciler); doldur('#ngAnaGrup', V.anaGruplar); doldur('#ngMAna', V.anaGruplar); doldur('#ngGrupF', tumGruplar); doldur('#ngMGrup', tumGruplar);
    doldur('#ngSinif', V.siniflar); doldur('#ngMSinif', V.siniflar); doldur('#ngBolge', V.bolgeler); doldur('#ngMBolge', V.bolgeler); doldur('#ngUlke', V.ulkeler); doldur('#ngMUlke', V.ulkeler); doldur('#ngIliski', V.iliskiDurumlari); doldur('#ngMIliski', V.iliskiDurumlari); doldur('#ngRol', V.roller); doldur('#ngMRol', V.roller); doldur('#ngMErp', V.erpSirketleri);
    const bagimliGrup = (anaId, grupId) => { const ana = $(anaId).value, sec = $(grupId).value; doldur(grupId, ana ? (V.faaliyetHiyerarsisi[ana] || []) : tumGruplar); $(grupId).value = sec; };
    $('#ngAnaGrup').addEventListener('change', () => bagimliGrup('#ngAnaGrup', '#ngGrupF')); $('#ngMAna').addEventListener('change', () => bagimliGrup('#ngMAna', '#ngMGrup'));
    yilSecenekleri($('#ngOYil')); yilSecenekleri($('#ngMYil'));
    const LISTE_F = ['#ngSirket', '#ngDurum', '#ngSonuc', '#ngKargoF', '#ngTur', '#ngAmacF', '#ngMasrafKime', '#ngBolge', '#ngUlke', '#ngSinif', '#ngIliski', '#ngRol', '#ngAnaGrup', '#ngGrupF', '#ngTemsilciF'];
    ['#ngAra', '#ngBas', '#ngBit', ...LISTE_F].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    $('#ngDonem').addEventListener('change', donemDegisti);
    $('#ngTemizle').addEventListener('click', () => { $('#ngAra').value = ''; LISTE_F.forEach(s => $(s).value = ''); bagimliGrup('#ngAnaGrup', '#ngGrupF'); $('#ngDonem').value = 'yil'; donemDegisti(); });
    $('#ngExcel').addEventListener('click', excel); $('#ngKalemExcel').addEventListener('click', kalemExcel); $('#ngMatrisExcel').addEventListener('click', matrisExcel);
    ['#ngOYil', '#ngOSirket', '#ngOBolge', '#ngOTemsilci'].forEach(s => $(s).addEventListener('change', cizOzet)); $('#ngOExcel').addEventListener('click', ozetExcel);
    ['#ngMAra', '#ngMYil', '#ngMSirket', '#ngMBolge', '#ngMUlke', '#ngMSinif', '#ngMIliski', '#ngMRol', '#ngMDurum', '#ngMAna', '#ngMGrup', '#ngMTemsilci', '#ngMErp', '#ngMSira'].forEach(s => { $(s).addEventListener('input', cizMusteri); $(s).addEventListener('change', cizMusteri); }); $('#ngMExcel').addEventListener('click', musteriExcel);
    $$('#ngSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    $('#ngDrawerKapat').addEventListener('click', drawerKapat); $('#ngPerde').addEventListener('click', drawerKapat);
    donemDegisti(); cizMusteri(); sekme(kok.dataset.baslangic || 'ozet'); window.NA_HAZIR = true;
    try { const p = new URLSearchParams(location.search); const no = p.get('no'), kod = p.get('kod'); if (no) { const t = V.numuneler.find(x => x.no.toLowerCase() === no.toLowerCase()); if (t) drawerAc(t.id); else mesaj('warning', esc(no) + ' bulunamadı.'); } else if (kod) cariDrawer(kod); } catch (e) { /* yoksay */ }
  }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
