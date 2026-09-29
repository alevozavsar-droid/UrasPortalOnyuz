// ============================================================
// MÜŞTERİ TEKLİFLERİ — ekran betiği (Views/MusteriTeklif/Index.cshtml; veri /MusteriTeklif/Veri; kaynaklar Data/TeklifOrnek.cs + Data/CariKartOrnek.cs)
// Sekme sırası: Aylık Teklif Özeti (varsayılan açılış) → Müşteri Bazlı → Teklif Listesi
// Aylık Teklif Özeti : yıl + şirket + temsilci seçimi; KPI; "Aylık Teklif ve Satışa Dönüşüm" (verilen teklif adet / tutar, satışa dönen adet / tutar,
//                      dönüşüm oranı tutar / adet, açık, reddedilen, süresi dolan, ortalama teklif) 12 ay + Toplam + Aylık Ort.; şirket bazlı aynı
//                      düzen; temsilci ve faaliyet ana grubu tabloları; Excel (CSV)
// Müşteri Bazlı      : cari kart sütunları (CARİ_MASTER) + yıl / şirket bazında teklif ve dönüşüm; satır → cari kart + teklif geçmişi drawer'ı; Excel
// Teklif Listesi     : dönem + şirket + durum + temsilci + ana grup + cari sınıfı + bölge + ilişki + para birimi filtreleri, KPI (tıklayınca filtre),
//                      Şirket × Durum ve Ay × Dönüşüm matrisleri, kaybetme nedenleri, en büyük 10 müşteri, satır → teklif detayı (kalemler + cari kart) drawer'ı
// Kural: satışa dönen teklif teklifin verildiği aya yazılır (kohort); Satış (TL) = sipariş tutarı; iptal teklifler toplama girmez; Aylık Ort. = Toplam / veri olan ay sayısı.
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Satışa Döndü", "Süresi Doldu"); şablon başlıkları CSS ile büyük harf gösterilir.
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('mt'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const bh = s => { s = String(s ?? ''); return s ? s.charAt(0).toLocaleUpperCase('tr-TR') + s.slice(1) : s; };
  const tl = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const yuzde = (pay, payda) => payda ? (pay / payda * 100).toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%' : '—';
  const oranRozet = (pay, payda) => { if (!payda) return '<span class="text-muted">—</span>'; const o = pay / payda; return `<span class="mt-oran ${o >= .5 ? 'iyi' : o >= .3 ? 'orta' : 'dusuk'}">${yuzde(pay, payda)}</span>`; };
  const iso = d => d.toISOString().slice(0, 10);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const durumRozet = d => rozet(d, d === 'Satışa Döndü' ? 'ok' : d === 'Reddedildi' || d === 'İptal' ? 'hata' : d === 'Süresi Doldu' ? 'notr' : d === 'Taslak' ? 'bilgi' : 'bekle');
  const kartRozet = d => rozet(d, d === 'Aktif' ? 'ok' : 'notr');
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const sekmeli = kok.dataset.sekme === '1';
  const AYLAR = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
  let V = null, L = [], CARI = {};
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 8000); }
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + iso(new Date()) + '.csv'; a.click(); };
  const sayi = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).replace(/\./g, '');
  const gecerli = t => t.durum !== 'İptal';
  const dondu = t => t.durum === 'Satışa Döndü';
  const acik = t => t.durum === 'Gönderildi' || t.durum === 'Revize Edildi';
  const kayip = t => t.durum === 'Reddedildi' || t.durum === 'Süresi Doldu';
  const topla = l => l.reduce((s, t) => s + Number(t.tutarTl), 0);
  const satis = l => l.filter(dondu).reduce((s, t) => s + Number(t.satisTutariTl), 0);
  const cari = kod => CARI[kod] || {};
  // Cari kart rozetleri ve hücreleri Müşteri Cari Kartları ekranıyla (wwwroot/js/cari-kart.js) aynı
  const iliskiRozet = i => `<span class="ck-tip ${i === 'Grup İçi' ? 'grup' : i === 'İlişkili Cari' ? 'iliskili' : ''}">${esc(i || '—')}</span>`;
  const rolRozet = r => `<span class="ck-tip ${r === 'Çift Rol' ? 'cift' : ''}">${esc(r || '—')}</span>`;
  const sinifRozet = s => `<span class="ck-tip ${s === 'Özel Takip' ? 'ozel' : ''}">${esc(s || '—')}</span>`;
  const musteriHtml = t => { const c = cari(t.cariKodu); return `<b>${esc(t.cari)}</b><small>${esc(c.resmiUnvan || '')}</small>`; };
  const sinifIliskiHtml = t => { const c = cari(t.cariKodu); return `${sinifRozet(c.sinif)} ${iliskiRozet(c.iliskiDurumu)}<small>${esc([c.bolge, c.ulke, c.sehir].filter(Boolean).join(' · '))}</small>`; };
  const faaliyetVar = (c, ana, grup, temsilci) => (c.faaliyetler || []).some(f => (!ana || f.anaGrup === ana) && (!grup || f.grup === grup) && (!temsilci || f.satisTemsilcisi === temsilci));

  // ---- sekmeler
  function sekme(p) {
    $$('#mtSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('#mt > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    if (V) { if (p === 'ozet') cizOzet(); if (p === 'musteri') cizMusteri(); if (p === 'liste') ciz(); }
    try { const u = new URL(location.href); if (p === 'ozet') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }

  // ---- ortak yardımcılar (yıl bazlı)
  const yilVerisi = (yil, sf, tf) => V.teklifler.filter(t => gecerli(t) && t.tarih.slice(0, 4) === String(yil) && (!sf || t.sirket === sf) && (!tf || t.temsilci === tf));
  const veriAyi = l => new Set(l.map(t => t.tarih.slice(5, 7))).size;
  const aylikTutar = (l, deger) => { const a = new Array(12).fill(0); l.forEach(t => a[+t.tarih.slice(5, 7) - 1] += Number(deger(t))); return a; };
  const aylikAdet = l => { const a = new Array(12).fill(0); l.forEach(t => a[+t.tarih.slice(5, 7) - 1]++); return a; };
  const hucre = v => v ? kisa(v) : '<span class="text-muted">—</span>';
  /// tip: tl | adet | oran (oran: a = [{pay, payda}], toplam ve ortalama oranların oranı)
  function satirHtml(ad, a, ayN, cls, tip, ipucu) {
    if (tip === 'oran' || tip === 'bolum') {   // oran: yüzde · bolum: pay / payda (ör. ortalama teklif tutarı = tutar / adet)
      const pay = a.reduce((s, x) => s + x.pay, 0), payda = a.reduce((s, x) => s + x.payda, 0);
      const f = (p, q) => q ? (tip === 'oran' ? yuzde(p, q) : kisa(p / q)) : '<span class="text-muted">—</span>';
      return `<tr class="${tip === 'oran' ? 'oran' : ''} ${cls || ''}"><td class="ad" title="${esc(ipucu || ad)}">${esc(ad)}</td>${a.map(x => `<td class="s">${f(x.pay, x.payda)}</td>`).join('')}<td class="s top">${f(pay, payda)}</td><td class="s ort">${f(pay, payda)}</td></tr>`;
    }
    const top = a.reduce((s, v) => s + v, 0);
    const f = tip === 'adet' ? (v => v ? kisa(v) : '<span class="text-muted">—</span>') : hucre;
    return `<tr class="${cls || ''}"><td class="ad" title="${esc(ipucu || ad)}">${esc(ad)}</td>${a.map(v => `<td class="s">${f(v)}</td>`).join('')}<td class="s top">${f(top)}</td><td class="s ort">${ayN && top ? (tip === 'adet' ? (top / ayN).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) : kisa(top / ayN)) : '<span class="text-muted">—</span>'}</td></tr>`;
  }
  const grupHtml = (ad, not) => `<tr class="grp"><td class="ad">${esc(ad)}</td><td colspan="14">${not ? `<small>${esc(not)}</small>` : ''}</td></tr>`;
  const oranDizisi = (l, pay, payda) => { const p = aylikTutar(l, pay), q = aylikTutar(l, payda); return p.map((v, i) => ({ pay: v, payda: q[i] })); };
  const oranAdet = l => { const p = aylikAdet(l.filter(dondu)), q = aylikAdet(l); return p.map((v, i) => ({ pay: v, payda: q[i] })); };
  function yilSecenekleri(sel) {
    const yillar = [...new Set(V.teklifler.map(t => t.tarih.slice(0, 4)))].sort().reverse(); if (!yillar.includes(String(V.yil))) yillar.unshift(String(V.yil));
    sel.innerHTML = yillar.map(y => `<option value="${y}">${y}</option>`).join(''); sel.value = String(V.yil);
  }
  function tabloCsv(tabloId, ad, yil) {
    const bas = ['Açıklama', ...AYLAR, 'Toplam ' + yil, 'Aylık Ort.'];
    const rows = $$('#' + tabloId + ' tbody tr').filter(tr => !tr.classList.contains('ara')).map(tr => { const td = $$('td', tr); if (tr.classList.contains('grp')) return [td[0].textContent.trim()]; return td.map(x => x.textContent.trim().replace('—', '')); });
    csvIndir(ad + '_' + yil, bas, rows);
  }
  /// Bir teklif listesi için özet satır dizisi (aylık tablo ve şirket blokları ortak)
  function ozetSatirlari(l, ayN, onEk, tam) {
    const d = l.filter(dondu), r = [];
    r.push(satirHtml(onEk + 'Verilen Teklif Adedi', aylikAdet(l), ayN, '', 'adet', 'İptal edilmemiş tüm teklifler (taslak dahil)'));
    r.push(satirHtml(onEk + 'Verilen Teklif Tutarı', aylikTutar(l, t => t.tutarTl), ayN, 'top', 'tl', 'Teklif tutarı TL, KDV hariç'));
    r.push(satirHtml(onEk + 'Satışa Dönen Adet', aylikAdet(d), ayN, '', 'adet', 'Durumu Satışa Döndü olan teklifler, teklif ayına göre'));
    r.push(satirHtml(onEk + 'Satışa Dönen Tutar', aylikTutar(l, t => dondu(t) ? t.satisTutariTl : 0), ayN, 'top', 'tl', 'Sipariş tutarı (kısmi siparişte sipariş edilen kısım)'));
    r.push(satirHtml(onEk + 'Dönüşüm Oranı (Tutar)', oranDizisi(l, t => dondu(t) ? t.satisTutariTl : 0, t => t.tutarTl), ayN, '', 'oran', 'Satışa Dönen Tutar / Verilen Teklif Tutarı'));
    r.push(satirHtml(onEk + 'Dönüşüm Oranı (Adet)', oranAdet(l), ayN, '', 'oran', 'Satışa Dönen Adet / Verilen Teklif Adedi'));
    if (tam) {
      r.push(satirHtml(onEk + 'Açık Teklif Tutarı (Gönderildi / Revize)', aylikTutar(l, t => acik(t) ? t.tutarTl : 0), ayN, 'r', 'tl'));
      r.push(satirHtml(onEk + 'Reddedilen Teklif Tutarı', aylikTutar(l, t => t.durum === 'Reddedildi' ? t.tutarTl : 0), ayN, 'r', 'tl'));
      r.push(satirHtml(onEk + 'Süresi Dolan Teklif Tutarı', aylikTutar(l, t => t.durum === 'Süresi Doldu' ? t.tutarTl : 0), ayN, 'r', 'tl'));
      r.push(satirHtml(onEk + 'Taslak Teklif Tutarı', aylikTutar(l, t => t.durum === 'Taslak' ? t.tutarTl : 0), ayN, 'r', 'tl'));
      const tut = aylikTutar(l, t => t.tutarTl), adet = aylikAdet(l);
      r.push(satirHtml(onEk + 'Ortalama Teklif Tutarı', tut.map((v, i) => ({ pay: v, payda: adet[i] })), ayN, 'r', 'bolum', 'Aylık teklif tutarı / teklif adedi; Toplam sütunu yılın ortalaması'));
    }
    return r;
  }

  // ---- sekme 1: aylık teklif özeti
  function cizOzet() {
    const yil = $('#mtOYil').value, sf = $('#mtOSirket').value, tf = $('#mtOTemsilci').value, l = yilVerisi(yil, sf, tf), ayN = veriAyi(l), d = l.filter(dondu), a = l.filter(acik), k = l.filter(kayip);
    $('#mtOYilBaslik').textContent = yil; $$('#mtAylik thead .yil, #mtSirketTablo thead .yil').forEach(th => th.textContent = 'Toplam ' + yil);
    const kpi = [['#0f172a', 'Verilen Teklif', kisa(topla(l)) + ' TL', `${l.length} Teklif · ${new Set(l.map(t => t.cariKodu)).size} Müşteri`],
      ['#166534', 'Satışa Dönen', kisa(satis(l)) + ' TL', `${d.length} Teklif · ${d.filter(t => t.faturaNo).length} Faturalandı`],
      ['#0f766e', 'Dönüşüm Oranı (Tutar)', yuzde(satis(l), topla(l)), `Adet Bazında ${yuzde(d.length, l.length)}`],
      ['#1d4ed8', 'Açık Teklif', kisa(topla(a)) + ' TL', `${a.length} Teklif Cevap Bekliyor`],
      ['#b91c1c', 'Kaybedilen', kisa(topla(k)) + ' TL', `${k.filter(t => t.durum === 'Reddedildi').length} Reddedildi · ${k.filter(t => t.durum === 'Süresi Doldu').length} Süresi Doldu`],
      ['#7c3aed', 'Ortalama Teklif', kisa(l.length ? topla(l) / l.length : 0) + ' TL', `Ortalama Sipariş ${kisa(d.length ? satis(l) / d.length : 0)} TL`]];
    $('#mtOKpi').innerHTML = kpi.map(x => `<div style="--k:${x[0]};cursor:default"><div class="l">${x[1]}</div><div class="v">${x[2]}</div><div class="s">${esc(bh(x[3]))}</div></div>`).join('');
    // aylık konsolide tablo
    $('#mtAylik tbody').innerHTML = l.length ? ozetSatirlari(l, ayN, '', true).join('') : '<tr><td colspan="15" class="na-bos">Bu yıl için teklif kaydı yok.</td></tr>';
    // şirket bazlı
    const rows = [];
    V.sirketler.filter(s => !sf || s === sf).forEach(s => {
      const ls = l.filter(t => t.sirket === s); if (!ls.length) return;
      rows.push(grupHtml(s, `${ls.length} teklif · ${ls.filter(dondu).length} satışa döndü · ${new Set(ls.map(t => t.cariKodu)).size} müşteri`));
      rows.push(...ozetSatirlari(ls, ayN, s + ' ', false));
      rows.push('<tr class="ara"><td colspan="15"></td></tr>');
    });
    if (!sf && l.length) { rows.push(satirHtml('Tüm Şirketler Verilen Teklif Tutarı', aylikTutar(l, t => t.tutarTl), ayN, 'genel', 'tl')); rows.push(satirHtml('Tüm Şirketler Satışa Dönen Tutar', aylikTutar(l, t => dondu(t) ? t.satisTutariTl : 0), ayN, 'genel', 'tl')); rows.push(satirHtml('Tüm Şirketler Dönüşüm Oranı (Tutar)', oranDizisi(l, t => dondu(t) ? t.satisTutariTl : 0, t => t.tutarTl), ayN, 'genel', 'oran')); }
    $('#mtSirketTablo tbody').innerHTML = rows.join('') || '<tr><td colspan="15" class="na-bos">Bu yıl için teklif kaydı yok.</td></tr>';
    // temsilci
    const tem = {}; l.forEach(t => { const p = tem[t.temsilci] || (tem[t.temsilci] = { n: 0, top: 0, dn: 0, sat: 0, acik: 0, m: new Set() }); p.n++; p.top += Number(t.tutarTl); if (dondu(t)) { p.dn++; p.sat += Number(t.satisTutariTl); } if (acik(t)) p.acik += Number(t.tutarTl); p.m.add(t.cariKodu); });
    const tk = Object.keys(tem).sort((x, y) => tem[y].top - tem[x].top);
    $('#mtTemsilci tbody').innerHTML = tk.map(x => `<tr><td><b>${esc(x)}</b></td><td class="s">${tem[x].n}</td><td class="s">${tl(tem[x].top)}</td><td class="s">${tem[x].dn}</td><td class="s">${tl(tem[x].sat)}</td><td class="s">${oranRozet(tem[x].sat, tem[x].top)}</td><td class="s">${tem[x].acik ? tl(tem[x].acik) : '<span class="text-muted">—</span>'}</td><td class="s">${tem[x].m.size}</td></tr>`).join('') || '<tr><td colspan="8" class="na-bos">Kayıt yok.</td></tr>';
    $('#mtTemsilci tfoot').innerHTML = l.length ? `<tr><td>Toplam</td><td class="s">${l.length}</td><td class="s">${tl(topla(l))}</td><td class="s">${d.length}</td><td class="s">${tl(satis(l))}</td><td class="s">${oranRozet(satis(l), topla(l))}</td><td class="s">${tl(topla(a))}</td><td class="s">${new Set(l.map(t => t.cariKodu)).size}</td></tr>` : '';
    // ana grup
    const gr = {}; l.forEach(t => { const p = gr[t.anaGrup] || (gr[t.anaGrup] = { n: 0, top: 0, sat: 0 }); p.n++; p.top += Number(t.tutarTl); if (dondu(t)) p.sat += Number(t.satisTutariTl); });
    $('#mtGrup tbody').innerHTML = Object.keys(gr).sort((x, y) => gr[y].top - gr[x].top).map(x => `<tr><td><b>${esc(x)}</b></td><td class="s">${gr[x].n}</td><td class="s">${tl(gr[x].top)}</td><td class="s">${tl(gr[x].sat)}</td><td class="s">${oranRozet(gr[x].sat, gr[x].top)}</td></tr>`).join('') || '<tr><td colspan="5" class="na-bos">Kayıt yok.</td></tr>';
    $('#mtONot').textContent = `${yil}${sf ? ' · ' + sf : ''}${tf ? ' · ' + tf : ''} · ${l.length} teklif · ${d.length} satışa döndü · veri olan ay sayısı ${ayN} · Aylık Ort. = Toplam / ${ayN || '—'} · tutarlar TL, KDV hariç · satışa dönen teklifler teklifin verildiği aya yazılır`;
  }

  // ---- sekme 2: müşteri bazlı
  function musteriOzeti(yil, sf) {
    const l = V.teklifler.filter(t => gecerli(t) && (!yil || t.tarih.slice(0, 4) === String(yil)) && (!sf || t.sirket === sf));
    const m = {};
    l.forEach(t => { const p = m[t.cariKodu] || (m[t.cariKodu] = { kod: t.cariKodu, n: 0, top: 0, dn: 0, sat: 0, acik: 0, son: '', sonTr: '', sirketler: new Set() }); p.n++; p.top += Number(t.tutarTl); if (dondu(t)) { p.dn++; p.sat += Number(t.satisTutariTl); } if (acik(t)) p.acik += Number(t.tutarTl); if (t.tarih > p.son) { p.son = t.tarih; p.sonTr = t.tarihTr; } p.sirketler.add(t.sirket); });
    return Object.values(m);
  }
  function cizMusteri() {
    const yil = $('#mtMYil').value, sf = $('#mtMSirket').value, v = id => $(id).value, ara = ($('#mtMAra').value || '').toLocaleLowerCase('tr-TR');
    // Filtre mantığı Müşteri Cari Kartları ekranındaki kartListesi() ile aynı (bölge, ülke, sınıf, ilişki, rol, durum, faaliyet ana grubu / grubu / temsilci, ERP şirketi, arama)
    let l = musteriOzeti(yil, sf).map(p => ({ ...p, c: cari(p.kod) })).filter(p => { const c = p.c; return (!v('#mtMBolge') || c.bolge === v('#mtMBolge')) && (!v('#mtMUlke') || c.ulke === v('#mtMUlke')) && (!v('#mtMSinif') || c.sinif === v('#mtMSinif')) && (!v('#mtMIliski') || c.iliskiDurumu === v('#mtMIliski')) && (!v('#mtMRol') || c.rol === v('#mtMRol')) && (!v('#mtMDurum') || c.durum === v('#mtMDurum')) && (!v('#mtMErp') || c.sirket === v('#mtMErp'))
      && (!v('#mtMAna') || faaliyetVar(c, v('#mtMAna'))) && (!v('#mtMGrup') || faaliyetVar(c, v('#mtMAna'), v('#mtMGrup'))) && (!v('#mtMTemsilci') || faaliyetVar(c, '', '', v('#mtMTemsilci')))
      && (!ara || [p.kod, c.kisaAd, c.resmiUnvan, c.vergiNo, c.yetkili, c.eposta, c.sehir, c.esCariKodu].join(' ').toLocaleLowerCase('tr-TR').includes(ara)); });
    const s = $('#mtMSira').value;
    l.sort((a, b) => s === 'kod' ? a.kod.localeCompare(b.kod) : s === 'satis' ? b.sat - a.sat : s === 'oran' ? (b.top ? b.sat / b.top : 0) - (a.top ? a.sat / a.top : 0) : s === 'acik' ? b.acik - a.acik : b.top - a.top);
    // Sütunlar Müşteri Cari Kartları listesiyle aynı sırada (kod, kısa ad / resmi ünvan, VKN / vergi dairesi, tip, bölge · ülke · şehir, sınıf, ilişki, rol · eş cari, ana faaliyet, faaliyet sayısı, temsilciler, vade, durum) + ERP şirketi + teklif sütunları
    $('#mtMusteriTablo tbody').innerHTML = l.map(p => { const c = p.c; return `<tr class="tik" data-kod="${esc(p.kod)}"><td><b>${esc(p.kod)}</b></td><td><b>${esc(c.kisaAd || '')}</b><br><small class="text-muted">${esc(c.resmiUnvan || '')}</small></td><td>${esc(c.vergiNo || '')}<br><small class="text-muted">${esc(c.vergiDairesi || '—')}</small></td><td>${esc(c.kartTipi || '')}</td><td>${esc(c.bolge || '')} · ${esc(c.ulke || '')}${c.sehir ? ' · ' + esc(c.sehir) : ''}</td><td>${sinifRozet(c.sinif)}</td><td>${iliskiRozet(c.iliskiDurumu)}</td><td>${rolRozet(c.rol)}${c.esCariKodu ? `<br><small class="text-muted">eş ${esc(c.esCariKodu)}</small>` : ''}</td><td>${bos(c.anaFaaliyet)}</td><td class="s">${(c.faaliyetler || []).length}</td><td><small>${bos(c.temsilciler)}</small></td><td>${bos(c.vadeSarti)}</td><td>${kartRozet(c.durum || 'Aktif')}</td><td>${bos(c.sirket)}<small>Teklif: ${esc([...p.sirketler].join(', '))}</small></td><td class="s">${p.n}</td><td class="s"><b>${tl(p.top)}</b></td><td class="s">${p.dn}</td><td class="s">${tl(p.sat)}</td><td class="s">${oranRozet(p.sat, p.top)}</td><td class="s">${p.acik ? tl(p.acik) : '<span class="text-muted">—</span>'}</td><td>${esc(p.sonTr)}</td></tr>`; }).join('') || '<tr><td colspan="21" class="na-bos">Filtreye uyan müşteri yok.</td></tr>';
    const top = l.reduce((s, p) => s + p.top, 0), sat = l.reduce((s, p) => s + p.sat, 0);
    $('#mtMTopTeklif').textContent = l.reduce((s, p) => s + p.n, 0); $('#mtMTopTeklifTl').textContent = tl(top); $('#mtMTopSatis').textContent = l.reduce((s, p) => s + p.dn, 0); $('#mtMTopSatisTl').textContent = tl(sat); $('#mtMTopOran').innerHTML = oranRozet(sat, top); $('#mtMTopAcik').textContent = tl(l.reduce((s, p) => s + p.acik, 0));
    $('#mtMSayi').textContent = `${l.length} müşteri · ${yil}${sf ? ' · ' + sf : ''} · satıra tıklayınca cari kart bilgileri ve teklif geçmişi açılır`;
    $('#mtNMusteri').textContent = l.length;
    $$('#mtMusteriTablo tr.tik').forEach(r => r.addEventListener('click', () => cariDrawer(r.dataset.kod)));
    cizMusteri.son = l;
  }
  function musteriExcel() {
    const l = cizMusteri.son || [];
    // Başlıklar Müşteri Cari Kartları Excel'iyle aynı sırada; sonuna teklif sütunları eklenir
    csvIndir('musteri_bazli_teklif_ozeti_' + $('#mtMYil').value, ['Cari Kodu', 'Kısa Ad', 'Kısaltmalı Ünvan', 'Resmi Ünvan', 'Vergi No', 'Vergi Dairesi', 'Kart Tipi', 'Bölge', 'Ülke', 'Şehir', 'Sınıf', 'İlişki Durumu', 'Rol', 'Eş Cari', 'Yetkili', 'Telefon', 'E-posta', 'Vade', 'Durum', 'Ana Faaliyet', 'Faaliyet Grupları', 'Satış Temsilcileri', 'Şirket', 'Not', 'Teklif Veren Şirketler', 'Teklif Adedi', 'Teklif Tutarı (TL)', 'Satışa Dönen Adet', 'Satış Tutarı (TL)', 'Dönüşüm (Tutar)', 'Açık Teklif (TL)', 'Son Teklif'],
      l.map(p => { const c = p.c; return [p.kod, c.kisaAd, c.kisaltmaliUnvan, c.resmiUnvan, c.vergiNo, c.vergiDairesi, c.kartTipi, c.bolge, c.ulke, c.sehir, c.sinif, c.iliskiDurumu, c.rol, c.esCariKodu, c.yetkili, c.telefon, c.eposta, c.vadeSarti, c.durum, c.anaFaaliyet, (c.faaliyetler || []).map(f => f.anaGrup + ' / ' + f.grup).join(' | '), c.temsilciler, c.sirket, c.not, [...p.sirketler].join(', '), p.n, sayi(p.top), p.dn, sayi(p.sat), yuzde(p.sat, p.top), sayi(p.acik), p.sonTr]; }));
  }

  // ---- sekme 3: teklif listesi — dönem
  function aralik() {
    const d = $('#mtDonem').value, bugun = new Date(V.bugun + 'T00:00:00');
    const ayBas = new Date(bugun.getFullYear(), bugun.getMonth(), 1);
    if (d === 'buay') return [iso(ayBas), V.bugun];
    if (d === 'gecenay') { const b = new Date(bugun.getFullYear(), bugun.getMonth() - 1, 1), e = new Date(bugun.getFullYear(), bugun.getMonth(), 0); return [iso(b), iso(e)]; }
    if (d === '30' || d === '90') { const b = new Date(bugun); b.setDate(b.getDate() - Number(d) + 1); return [iso(b), V.bugun]; }
    if (d === 'yil') return [bugun.getFullYear() + '-01-01', V.bugun];
    if (d === 'ozel') return [$('#mtBas').value || '', $('#mtBit').value || ''];
    return ['', ''];
  }
  function donemDegisti() { const ozel = $('#mtDonem').value === 'ozel'; $('#mtBas').disabled = $('#mtBit').disabled = !ozel; if (!ozel) { const [b, e] = aralik(); $('#mtBas').value = b; $('#mtBit').value = e; } ciz(); }
  function liste() {
    const ara = ($('#mtAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value, [bas, bit] = aralik();
    return V.teklifler.filter(t => { const c = cari(t.cariKodu); return (!bas || t.tarih >= bas) && (!bit || t.tarih <= bit) && (!v('#mtSirket') || t.sirket === v('#mtSirket')) && (!v('#mtDurum') || t.durum === v('#mtDurum')) && (!v('#mtTemsilciF') || t.temsilci === v('#mtTemsilciF')) && (!v('#mtAnaGrup') || t.anaGrup === v('#mtAnaGrup')) && (!v('#mtGrupF') || t.faaliyetGrubu === v('#mtGrupF'))
      && (!v('#mtSinif') || c.sinif === v('#mtSinif')) && (!v('#mtBolge') || c.bolge === v('#mtBolge')) && (!v('#mtUlke') || c.ulke === v('#mtUlke')) && (!v('#mtIliski') || c.iliskiDurumu === v('#mtIliski')) && (!v('#mtRol') || c.rol === v('#mtRol')) && (!v('#mtPb') || t.paraBirimi === v('#mtPb'))
      && (!ara || [t.no, t.cari, t.cariKodu, t.konu, t.sirket, t.temsilci, t.siparisNo, t.faturaNo, t.faaliyetGrubu, c.resmiUnvan, ...t.kalemler.map(k => k.urun)].join(' ').toLocaleLowerCase('tr-TR').includes(ara)); });
  }

  // ---- sekme 3 çizim
  function ciz() {
    L = liste();
    const g = L.filter(gecerli), d = g.filter(dondu), a = g.filter(acik), k = g.filter(kayip);
    const kpi = [['#0f172a', 'Verilen Teklif', kisa(topla(g)) + ' TL', `${g.length} Teklif · ${new Set(g.map(t => t.cariKodu)).size} Müşteri`, ''],
      ['#166534', 'Satışa Döndü', kisa(satis(g)) + ' TL', `${d.length} Teklif · Dönüşüm ${yuzde(satis(g), topla(g))}`, 'Satışa Döndü'],
      ['#1d4ed8', 'Gönderildi', kisa(topla(g.filter(t => t.durum === 'Gönderildi'))) + ' TL', `${g.filter(t => t.durum === 'Gönderildi').length} Teklif Cevap Bekliyor`, 'Gönderildi'],
      ['#b45309', 'Revize Edildi', kisa(topla(g.filter(t => t.durum === 'Revize Edildi'))) + ' TL', `${g.filter(t => t.durum === 'Revize Edildi').length} Teklif`, 'Revize Edildi'],
      ['#b91c1c', 'Reddedildi', kisa(topla(g.filter(t => t.durum === 'Reddedildi'))) + ' TL', `${g.filter(t => t.durum === 'Reddedildi').length} Teklif`, 'Reddedildi'],
      ['#64748b', 'Süresi Doldu', kisa(topla(g.filter(t => t.durum === 'Süresi Doldu'))) + ' TL', `${g.filter(t => t.durum === 'Süresi Doldu').length} Teklif · Kaybedilen Toplam ${kisa(topla(k))} TL`, 'Süresi Doldu']];
    $('#mtKpi').innerHTML = kpi.map(x => `<div style="--k:${x[0]}" data-k="${esc(x[4])}" title="Tıklayınca filtre uygular"><div class="l">${x[1]}</div><div class="v">${x[2]}</div><div class="s">${esc(bh(x[3]))}</div></div>`).join('');
    $$('#mtKpi > div').forEach(x => x.addEventListener('click', () => { $('#mtDurum').value = x.dataset.k; ciz(); }));
    // tablo
    $('#mtTablo tbody').innerHTML = L.map(t => `<tr class="tik" data-id="${t.id}"><td>${esc(t.tarihTr)}</td><td><b>${esc(t.no)}</b>${t.revizyon ? `<span class="mt-rev" title="Revizyon">R${t.revizyon}</span>` : ''}</td><td>${esc(t.sirket)}</td><td>${esc(t.cariKodu)}</td><td>${musteriHtml(t)}</td><td>${sinifIliskiHtml(t)}</td><td>${esc(t.temsilci)}</td><td>${esc(t.anaGrup)} / <b>${esc(t.faaliyetGrubu)}</b></td><td>${esc(t.konu)}<small>${t.kalemSayisi} Kalem</small></td><td class="s">${tl(t.tutar)}</td><td>${esc(t.paraBirimi)}</td><td class="s"><b>${tl(t.tutarTl)}</b>${t.paraBirimi !== 'TL' ? `<small>kur ${tl(t.kur)}</small>` : ''}</td><td>${esc(t.gecerlilikTr)}</td><td>${durumRozet(t.durum)}${t.kaybetmeNedeni ? `<small>${esc(t.kaybetmeNedeni)}</small>` : ''}</td><td>${bos(t.siparisNo)}${t.siparisTr ? `<small>${esc(t.siparisTr)}${t.faturaNo ? ' · ' + esc(t.faturaNo) : ''}</small>` : ''}</td><td class="s">${dondu(t) ? tl(t.satisTutariTl) : '<span class="text-muted">—</span>'}</td></tr>`).join('') || '<tr><td colspan="16" class="na-bos">Filtreye uyan teklif yok.</td></tr>';
    $('#mtTopSayi').textContent = g.length + ' Teklif'; $('#mtTopTl').textContent = tl(topla(g)) + ' TL'; $('#mtTopOran').innerHTML = `Dönüşüm ${oranRozet(satis(g), topla(g))} · ${d.length} Satışa Döndü`; $('#mtTopSatis').textContent = tl(satis(g)) + ' TL';
    $('#mtSayi').textContent = `${L.length} / ${V.teklifler.length} teklif · ${L.length - g.length} iptal (toplama girmez) · satıra tıklayınca teklif detayı, kalemler ve cari kart bilgileri açılır`;
    $('#mtNListe').textContent = L.length;
    $$('#mtTablo tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    // şirket × durum
    const DURUM_SUTUN = [['acik', t => acik(t)], ['Satışa Döndü', t => t.durum === 'Satışa Döndü'], ['Reddedildi', t => t.durum === 'Reddedildi'], ['Süresi Doldu', t => t.durum === 'Süresi Doldu'], ['Taslak', t => t.durum === 'Taslak']];
    const m = {}; g.forEach(t => { const p = m[t.sirket] || (m[t.sirket] = { n: 0, top: 0, sat: 0, acik: 0, 'Satışa Döndü': 0, Reddedildi: 0, 'Süresi Doldu': 0, Taslak: 0 }); DURUM_SUTUN.forEach(([kk, f]) => { if (f(t)) p[kk] += Number(t.tutarTl); }); p.top += Number(t.tutarTl); p.n++; if (dondu(t)) p.sat += Number(t.satisTutariTl); });
    const satir = (ad, p, kalin) => `<tr class="${kalin ? 'toplam' : ''}"><td>${esc(ad)}</td>${DURUM_SUTUN.map(([kk]) => `<td class="s">${p[kk] ? tl(p[kk]) : '<span class="text-muted">—</span>'}</td>`).join('')}<td class="s"><b>${tl(p.top)}</b></td><td class="s">${oranRozet(p.sat, p.top)}</td><td class="s">${p.n}</td></tr>`;
    const sk = V.sirketler.filter(s => m[s]); const tm = { n: 0, top: 0, sat: 0, acik: 0, 'Satışa Döndü': 0, Reddedildi: 0, 'Süresi Doldu': 0, Taslak: 0 }; sk.forEach(s => Object.keys(tm).forEach(kk => tm[kk] += m[s][kk]));
    $('#mtMatris tbody').innerHTML = sk.length ? sk.map(s => satir(s, m[s])).join('') + satir('Konsolide Toplam', tm, true) : '<tr><td colspan="9" class="na-bos">Kayıt yok.</td></tr>';
    // ay × dönüşüm
    const ay = {}; g.forEach(t => { const kk = t.tarih.slice(0, 7); const p = ay[kk] || (ay[kk] = { n: 0, top: 0, dn: 0, sat: 0, acik: 0 }); p.n++; p.top += Number(t.tutarTl); if (dondu(t)) { p.dn++; p.sat += Number(t.satisTutariTl); } if (acik(t)) p.acik += Number(t.tutarTl); });
    const ayAdi = kk => AYLAR[+kk.slice(5, 7) - 1] + ' ' + kk.slice(0, 4);
    const aySatir = (ad, p, kalin) => `<tr class="${kalin ? 'toplam' : ''}"><td>${esc(ad)}</td><td class="s">${p.n}</td><td class="s">${tl(p.top)}</td><td class="s">${p.dn}</td><td class="s">${tl(p.sat)}</td><td class="s">${oranRozet(p.sat, p.top)}</td><td class="s">${yuzde(p.dn, p.n)}</td><td class="s">${p.acik ? tl(p.acik) : '<span class="text-muted">—</span>'}</td></tr>`;
    const ak = Object.keys(ay).sort((x, y) => y.localeCompare(x)); const ta = { n: g.length, top: topla(g), dn: d.length, sat: satis(g), acik: topla(a) };
    $('#mtAyMatris tbody').innerHTML = ak.length ? ak.map(kk => aySatir(ayAdi(kk), ay[kk])).join('') + aySatir('Toplam', ta, true) : '<tr><td colspan="8" class="na-bos">Kayıt yok.</td></tr>';
    // kaybetme nedenleri
    const ned = {}; k.forEach(t => { const kk = t.kaybetmeNedeni || 'Belirtilmedi'; ned[kk] = ned[kk] || { top: 0, n: 0 }; ned[kk].top += Number(t.tutarTl); ned[kk].n++; });
    const nl = Object.keys(ned).sort((x, y) => ned[y].top - ned[x].top), maks = nl.length ? ned[nl[0]].top : 1, genel = topla(k) || 1;
    $('#mtNedenler').innerHTML = nl.map(x => `<div class="mt-bar"><span title="${esc(x)}">${esc(x)}<small class="text-muted"> · ${ned[x].n}</small></span><span class="b"><i style="width:${Math.max(2, ned[x].top / maks * 100)}%"></i></span><span class="v">${kisa(ned[x].top)} <small>%${(ned[x].top / genel * 100).toFixed(1)}</small></span></div>`).join('') || '<div class="na-bos">Kaybedilen teklif yok.</div>';
    // en büyük müşteriler
    const mu = {}; g.forEach(t => { const p = mu[t.cariKodu] || (mu[t.cariKodu] = { ad: t.cari, n: 0, top: 0, sat: 0 }); p.n++; p.top += Number(t.tutarTl); if (dondu(t)) p.sat += Number(t.satisTutariTl); });
    $('#mtMusteriler tbody').innerHTML = Object.keys(mu).sort((x, y) => mu[y].top - mu[x].top).slice(0, 10).map((x, i) => `<tr class="tik" data-kod="${esc(x)}" style="cursor:pointer"><td>${i + 1}</td><td><b>${esc(x)}</b> · ${esc(mu[x].ad)}<small>${esc(cari(x).resmiUnvan || '')}</small></td><td>${sinifRozet(cari(x).sinif)}</td><td class="s">${mu[x].n}</td><td class="s">${tl(mu[x].top)}</td><td class="s">${tl(mu[x].sat)}</td><td class="s">${oranRozet(mu[x].sat, mu[x].top)}</td></tr>`).join('') || '<tr><td colspan="7" class="na-bos">Kayıt yok.</td></tr>';
    $$('#mtMusteriler tr.tik').forEach(r => r.addEventListener('click', () => cariDrawer(r.dataset.kod)));
  }

  // ---- drawer: ortak cari kart bloğu (CARİ_MASTER §3 sırası + faaliyet satırları) — Müşteri Cari Kartları ekranıyla aynı standart
  function cariKartHtml(c) {
    if (!c || !c.kod) return '<div class="na-kart"><div class="na-kart-g na-bos">Cari kart bulunamadı.</div></div>';
    // Alan adları ve sırası Müşteri Cari Kartları drawer'ındaki masterHtml() / faaliyetTablo() ile bire bir (§3 numaralı 20 alan)
    const a = [['1. Cari Kodu', c.kod], ['2. Cari Kısa Adı', c.kisaAd], ['3. Kısaltmalı Ünvan', c.kisaltmaliUnvan], ['4. Resmi Ünvan', c.resmiUnvan], ['5. Vergi No / TCKN', c.vergiNo], ['6. Vergi Dairesi', c.vergiDairesi], ['7. Cari Kart Tipi', c.kartTipi], ['8. Cari Bölge', c.bolge], ['9. Ülke', c.ulke], ['10. Şehir', c.sehir],
      ['11. Cari Sınıfı', c.sinif], ['12. İlişki Durumu', c.iliskiDurumu], ['13. Cari Rolü', c.rol], ['14. Eş Cari Kodu', c.esCariKodu ? c.esCariKodu + (c.esKartAd ? ' · ' + c.esKartAd : '') : ''], ['15. Yetkili Kişi', c.yetkili], ['16. Telefon', c.telefon], ['17. E-posta', c.eposta], ['18. Vade / Ödeme Şartı', c.vadeSarti], ['19. Durum', c.durum], ['Şirket (ERP)', c.sirket]];
    const f = c.faaliyetler || [];
    return `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-database me-1"></i>CARİ_MASTER</span><span class="not">${esc(c.olusturmaTarihi || '')}${c.olusturan ? ' · ' + esc(c.olusturan) : ''}${c.kaynakTalepNo ? ' · talep ' + esc(c.kaynakTalepNo) : ''}</span></div><div class="na-kart-g"><div class="ck-master">${a.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}<div style="grid-column:1/-1"><div class="l">20. Not</div><div class="v" style="font-weight:500">${bos(c.not)}</div></div></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-diagram-project me-1"></i>CARİ_FAALİYET_EŞLEŞTİRME (${f.length})</span><span class="not">faaliyet grubu bazında satış temsilcisi · ana faaliyet tek satırda Evet</span></div><div class="na-kart-g p-0">${f.length ? `<div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>#</th><th>Faaliyet Ana Grubu</th><th>Faaliyet Grubu</th><th>Satış Temsilcisi</th><th>Ana Faaliyet</th><th>Ürün Sınıfı › Grubu › Serisi › Adı</th><th>Durum</th><th>Başlangıç</th><th>Not</th></tr></thead><tbody>${f.map(x => `<tr class="${x.anaFaaliyet ? 'table-active' : ''}"><td>${x.sira}</td><td>${esc(x.anaGrup)}</td><td><b>${esc(x.grup)}</b></td><td>${bos(x.satisTemsilcisi)}</td><td>${x.anaFaaliyet ? rozet('Evet', 'ok') : 'Hayır'}</td><td><small>${esc([x.urunSinifi, x.urunGrubu, x.urunSerisi, x.urunAdi].filter(Boolean).join(' › ') || '—')}</small></td><td>${kartRozet(x.durum)}</td><td>${bos(x.baslangicTr)}</td><td><small>${esc(x.not || '')}</small></td></tr>`).join('')}</tbody></table></div>` : '<div class="na-bos">Faaliyet eşleştirmesi yok.</div>'}</div></div>`;
  }
  function teklifGecmisiHtml(kod, seciliId) {
    const l = V.teklifler.filter(t => t.cariKodu === kod), g = l.filter(gecerli);
    return `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Müşteriye Verilen Teklifler</span><span class="not">Tüm dönem · ${g.length} teklif · ${tl(topla(g))} TL · satışa dönen ${tl(satis(g))} TL (${yuzde(satis(g), topla(g))})</span></div><div class="na-kart-g p-0"><div class="table-responsive" style="max-height:280px"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Teklif No</th><th>Şirket</th><th>Faaliyet Grubu</th><th>Konu</th><th class="s">Tutar (TL)</th><th>Durum</th><th class="s">Satış (TL)</th></tr></thead><tbody>${l.map(x => `<tr class="tik ${x.id === seciliId ? 'table-active' : ''}" data-id="${x.id}"><td>${esc(x.tarihTr)}</td><td>${esc(x.no)}</td><td>${esc(x.sirket)}</td><td>${esc(x.faaliyetGrubu)}</td><td>${esc(x.konu)}</td><td class="s">${tl(x.tutarTl)}</td><td>${durumRozet(x.durum)}</td><td class="s">${dondu(x) ? tl(x.satisTutariTl) : '<span class="text-muted">—</span>'}</td></tr>`).join('')}</tbody></table></div></div></div>`;
  }
  function ac(url, baslik) { if (window.top && window.top !== window && typeof window.top.portalSekmeAc === 'function') window.top.portalSekmeAc(baslik, url); else window.open(url + (sekmeli ? (url.includes('?') ? '&' : '?') + '_sekme=1' : ''), '_blank'); }
  function drawerAksiyon(c) {
    $('#mtDrawerAksiyon').innerHTML = `<button type="button" class="btn btn-light btn-sm py-0" id="mtKartAc" style="font-size:.66rem"><i class="fas fa-address-card me-1"></i>Cari Kartı Aç</button>`;
    $('#mtKartAc').addEventListener('click', () => ac('/CariKart/Musteri?kod=' + encodeURIComponent(c.kod), 'Müşteri Cari Kartları'));
  }
  function drawerAc(id) {
    const t = V.teklifler.find(x => x.id === id); if (!t) return; const c = cari(t.cariKodu);
    $('#mtDrawerBaslik').innerHTML = `<i class="fas fa-file-signature me-1"></i>${esc(t.no)}${t.revizyon ? ` <span class="mt-rev">R${t.revizyon}</span>` : ''} · ${esc(t.sirket)} → ${esc(t.cari)} ${durumRozet(t.durum)}`;
    drawerAksiyon(c);
    const a = [['Teklif No', t.no], ['Tarih', t.tarihTr], ['Şirket', t.sirket], ['Müşteri', t.cariKodu + ' · ' + t.cari], ['Satış Temsilcisi', t.temsilci], ['Faaliyet Grubu', t.anaGrup + ' / ' + t.faaliyetGrubu], ['Konu', t.konu], ['Tutar', tl(t.tutar) + ' ' + t.paraBirimi], ['Kur', t.paraBirimi === 'TL' ? '1,00' : tl(t.kur)], ['Tutar (TL)', tl(t.tutarTl) + ' TL'],
      ['Geçerlilik', t.gecerlilikTr], ['Durum', t.durum + (t.revizyon ? ' (Revizyon ' + t.revizyon + ')' : '')], ['Karar Tarihi', t.kararTr], ['Sipariş No', t.siparisNo], ['Sipariş Tarihi', t.siparisTr], ['Satış Tutarı (TL)', dondu(t) ? tl(t.satisTutariTl) + ' TL' + (t.satisTutariTl < t.tutarTl ? ' (Kısmi)' : '') : ''], ['Fatura No', t.faturaNo], ['Kaybetme Nedeni', t.kaybetmeNedeni], ['Hazırlayan', t.hazirlayan], ['Onaylayan', t.onaylayan], ['Kaynak Sistem', t.kaynak]];
    $('#mtDrawerIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-receipt me-1"></i>Teklif Bilgileri</span><span class="not">${esc(t.kaynak)}</span></div><div class="na-kart-g"><div class="mt-master">${a.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}<div style="grid-column:1/-1"><div class="l">Açıklama</div><div class="v" style="font-weight:500">${bos(t.aciklama)}</div></div></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-list-ol me-1"></i>Teklif Kalemleri</span><span class="not">${t.kalemSayisi} Kalem · ${esc(t.paraBirimi)} · KDV hariç</span></div><div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo mt-kalem" data-dinamik="kapali"><thead><tr><th>#</th><th>Ürün</th><th>Hiyerarşi</th><th class="s">Miktar</th><th>Birim</th><th class="s">Birim Fiyat</th><th class="s">Tutar</th><th class="s">Tutar (TL)</th></tr></thead><tbody>${t.kalemler.map(k => `<tr><td>${k.sira}</td><td><b>${esc(k.urun)}</b></td><td><small style="display:block">${esc(k.hiyerarsi)}</small></td><td class="s">${kisa(k.miktar)}</td><td>${esc(k.birim)}</td><td class="s">${tl(k.birimFiyat)}</td><td class="s">${tl(k.tutar)}</td><td class="s">${tl(k.tutar * t.kur)}</td></tr>`).join('')}</tbody><tfoot><tr><td colspan="6" style="font-weight:700">Toplam</td><td class="s" style="font-weight:700">${tl(t.tutar)} ${esc(t.paraBirimi)}</td><td class="s" style="font-weight:700">${tl(t.tutarTl)} TL</td></tr></tfoot></table></div></div></div>
      ${cariKartHtml(c)}${teklifGecmisiHtml(t.cariKodu, t.id)}`;
    $$('#mtDrawerIc tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    $('#mtDrawer').classList.add('acik'); $('#mtPerde').classList.add('acik'); $('#mtDrawer').scrollTop = 0;
  }
  function cariDrawer(kod) {
    const c = cari(kod); if (!c.kod) { mesaj('warning', esc(kod) + ' için cari kart bulunamadı.'); return; }
    $('#mtDrawerBaslik').innerHTML = `<i class="fas fa-address-card me-1"></i>${esc(c.kod)} · ${esc(c.kisaAd)} ${kartRozet(c.durum || 'Aktif')}`;
    drawerAksiyon(c);
    $('#mtDrawerIc').innerHTML = cariKartHtml(c) + teklifGecmisiHtml(kod, 0);
    $$('#mtDrawerIc tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    $('#mtDrawer').classList.add('acik'); $('#mtPerde').classList.add('acik'); $('#mtDrawer').scrollTop = 0;
  }
  function drawerKapat() { $('#mtDrawer').classList.remove('acik'); $('#mtPerde').classList.remove('acik'); }

  // ---- excel (sekme 3)
  function excel() {
    csvIndir('musteri_teklifleri', ['Tarih', 'Teklif No', 'Revizyon', 'Şirket', 'Cari Kodu', 'Müşteri', 'Resmi Ünvan', 'Vergi No', 'Cari Sınıfı', 'Bölge', 'Ülke', 'Şehir', 'İlişki Durumu', 'Vade Şartı', 'Satış Temsilcisi', 'Faaliyet Ana Grubu', 'Faaliyet Grubu', 'Konu', 'Kalem Sayısı', 'Tutar', 'Para Birimi', 'Kur', 'Tutar (TL)', 'Geçerlilik', 'Durum', 'Karar Tarihi', 'Sipariş No', 'Sipariş Tarihi', 'Satış Tutarı (TL)', 'Fatura No', 'Kaybetme Nedeni', 'Hazırlayan', 'Onaylayan', 'Kaynak', 'Açıklama'],
      L.map(t => { const c = cari(t.cariKodu); return [t.tarihTr, t.no, t.revizyon || '', t.sirket, t.cariKodu, t.cari, c.resmiUnvan, c.vergiNo, c.sinif, c.bolge, c.ulke, c.sehir, c.iliskiDurumu, c.vadeSarti, t.temsilci, t.anaGrup, t.faaliyetGrubu, t.konu, t.kalemSayisi, sayi(t.tutar), t.paraBirimi, sayi(t.kur), sayi(t.tutarTl), t.gecerlilikTr, t.durum, t.kararTr, t.siparisNo, t.siparisTr, dondu(t) ? sayi(t.satisTutariTl) : '', t.faturaNo, t.kaybetmeNedeni, t.hazirlayan, t.onaylayan, t.kaynak, t.aciklama]; }));
  }
  function matrisExcel() {
    const rows = $$('#mtMatris tbody tr').map(tr => $$('td', tr).map(td => td.textContent.trim().replace('—', '')));
    csvIndir('musteri_teklifleri_sirket_ozeti', ['Şirket', 'Açık (Gönderildi / Revize)', 'Satışa Döndü', 'Reddedildi', 'Süresi Doldu', 'Taslak', 'Toplam Teklif', 'Dönüşüm', 'Adet'], rows);
  }
  function ozetExcel() {
    const yil = $('#mtOYil').value; tabloCsv('mtAylik', 'aylik_teklif_ozeti', yil);
    setTimeout(() => tabloCsv('mtSirketTablo', 'aylik_teklif_ozeti_sirket', yil), 300);
    setTimeout(() => csvIndir('aylik_teklif_ozeti_temsilci_' + yil, ['Temsilci', 'Teklif', 'Teklif Tutarı', 'Satışa Dönen', 'Satış Tutarı', 'Dönüşüm (Tutar)', 'Açık Teklif', 'Müşteri'], $$('#mtTemsilci tbody tr, #mtTemsilci tfoot tr').map(tr => $$('td', tr).map(td => td.textContent.trim().replace('—', '')))), 600);
  }

  // ---- başlat
  fetch('/MusteriTeklif/Veri').then(r => r.json()).then(v => {
    V = v; V.cariler.forEach(c => CARI[c.kod] = c);
    const doldur = (id, l) => { $(id).innerHTML = '<option value="">Tümü</option>' + l.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); };
    const tumGruplar = Object.values(V.faaliyetHiyerarsisi).flat();
    doldur('#mtDurum', V.durumlar); doldur('#mtTemsilciF', V.temsilciler); doldur('#mtOTemsilci', V.temsilciler); doldur('#mtMTemsilci', V.temsilciler); doldur('#mtAnaGrup', V.anaGruplar); doldur('#mtMAna', V.anaGruplar); doldur('#mtGrupF', tumGruplar); doldur('#mtMGrup', tumGruplar);
    doldur('#mtSinif', V.siniflar); doldur('#mtMSinif', V.siniflar); doldur('#mtBolge', V.bolgeler); doldur('#mtMBolge', V.bolgeler); doldur('#mtUlke', V.ulkeler); doldur('#mtMUlke', V.ulkeler); doldur('#mtIliski', V.iliskiDurumlari); doldur('#mtMIliski', V.iliskiDurumlari); doldur('#mtRol', V.roller); doldur('#mtMRol', V.roller); doldur('#mtMErp', V.erpSirketleri); doldur('#mtPb', V.paraBirimleri);
    // Faaliyet Grubu listesi seçilen ana gruba göre daralır (Cari Kart ekranındaki bağımlı seçim kuralı)
    const bagimliGrup = (anaId, grupId) => { const ana = $(anaId).value, sec = $(grupId).value; doldur(grupId, ana ? (V.faaliyetHiyerarsisi[ana] || []) : tumGruplar); $(grupId).value = sec; };
    $('#mtAnaGrup').addEventListener('change', () => bagimliGrup('#mtAnaGrup', '#mtGrupF')); $('#mtMAna').addEventListener('change', () => bagimliGrup('#mtMAna', '#mtMGrup'));
    yilSecenekleri($('#mtOYil')); yilSecenekleri($('#mtMYil'));
    const LISTE_F = ['#mtSirket', '#mtDurum', '#mtPb', '#mtBolge', '#mtUlke', '#mtSinif', '#mtIliski', '#mtRol', '#mtAnaGrup', '#mtGrupF', '#mtTemsilciF'];
    ['#mtAra', '#mtBas', '#mtBit', ...LISTE_F].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    $('#mtDonem').addEventListener('change', donemDegisti);
    $('#mtTemizle').addEventListener('click', () => { $('#mtAra').value = ''; LISTE_F.forEach(s => $(s).value = ''); bagimliGrup('#mtAnaGrup', '#mtGrupF'); $('#mtDonem').value = 'yil'; donemDegisti(); });
    $('#mtExcel').addEventListener('click', excel); $('#mtMatrisExcel').addEventListener('click', matrisExcel);
    ['#mtOYil', '#mtOSirket', '#mtOTemsilci'].forEach(s => $(s).addEventListener('change', cizOzet)); $('#mtOExcel').addEventListener('click', ozetExcel);
    ['#mtMAra', '#mtMYil', '#mtMSirket', '#mtMBolge', '#mtMUlke', '#mtMSinif', '#mtMIliski', '#mtMRol', '#mtMDurum', '#mtMAna', '#mtMGrup', '#mtMTemsilci', '#mtMErp', '#mtMSira'].forEach(s => { $(s).addEventListener('input', cizMusteri); $(s).addEventListener('change', cizMusteri); }); $('#mtMExcel').addEventListener('click', musteriExcel);
    $$('#mtSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    $('#mtDrawerKapat').addEventListener('click', drawerKapat); $('#mtPerde').addEventListener('click', drawerKapat);
    donemDegisti(); cizMusteri(); sekme(kok.dataset.baslangic || 'ozet'); window.NA_HAZIR = true;
    // derin bağlantı: ?no=TKL-… teklif detayı, ?kod=120.001 cari kart
    try { const p = new URLSearchParams(location.search); const no = p.get('no'), kod = p.get('kod'); if (no) { const t = V.teklifler.find(x => x.no.toLowerCase() === no.toLowerCase()); if (t) drawerAc(t.id); else mesaj('warning', esc(no) + ' bulunamadı.'); } else if (kod) cariDrawer(kod); } catch (e) { /* yoksay */ }
  }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
