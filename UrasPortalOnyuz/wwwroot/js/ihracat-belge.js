// ============================================================
// İHRACAT BELGE GÖNDERİM TAKİBİ — ekran betiği (Views/IhracatBelge/Index.cshtml; veri /IhracatBelge/Veri; kaynaklar Data/IhracatBelgeOrnek.cs + Data/CariKartOrnek.cs)
// Sekme sırası: Aylık Özet (varsayılan açılış) → Müşteri Bazlı → Gönderim Listesi
// Aylık Özet       : yıl + şirket + ödeme şekli + gönderen; KPI; aylık tablo (gönderim, akreditif ibrazı, vesaik, müşteri / forwarder, belge adedi, masraf, teslim, süresinde /
//                    geç ibraz, rezerv, ödendi, tahsil tutarı, ort. teslim) 12 ay + Toplam + Aylık Ort.; operasyon uzmanı ve kargo firması tabloları; alıcı tipi dağılımı; Excel
// Müşteri Bazlı    : cari kart sütunları (CARİ_MASTER) + gönderim / akreditif / belge / masraf / fatura / geç ibraz / rezerv / ödendi; satır → cari kart + gönderim geçmişi; Excel
// Gönderim Listesi : dönem + şirket + ödeme + amaç + alıcı tipi + kargo + durum + banka sonucu + ibraz riski + belge türü + gönderen + cari kart filtreleri; KPI (tıklayınca filtre);
//                    ibraz riski listesi; Şirket × Amaç ve Ay matrisleri; kargo ve belge türü dağılımı; satır → detay (dosya / fatura, akreditif ve ibraz, belge seti, kargo, hareketler, cari kart)
// Kural: iptal gönderimler toplama girmez; ibraz riski sunucuda hesaplanır (IbGonderim.IbrazRiski); Aylık Ort. = Toplam / veri olan ay sayısı.
// Cari kart rozetleri / CARİ_MASTER bloğu Müşteri Cari Kartları ekranıyla (wwwroot/js/cari-kart.js) aynı.
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Tümü", "Akreditif İbrazı", "Rezerv Bildirildi").
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('ib'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const bh = s => { s = String(s ?? ''); return s ? s.charAt(0).toLocaleUpperCase('tr-TR') + s.slice(1) : s; };
  const tl = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const ond = (v, n) => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: n, maximumFractionDigits: n });
  const yuzde = (pay, payda) => payda ? (pay / payda * 100).toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%' : '—';
  const iso = d => d.toISOString().slice(0, 10);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const durumRozet = d => rozet(d, d === 'Teslim Edildi' ? 'ok' : d === 'Teslim Edilemedi' || d === 'İptal' ? 'hata' : d === 'Hazırlanıyor' ? 'bilgi' : 'bekle');
  const bankaRozet = d => !d || d === '—' ? '<span class="text-muted">—</span>' : rozet(d, d === 'Ödendi' || d === 'Uygun Bulundu' ? 'ok' : d === 'Rezerv Bildirildi' ? 'hata' : d === 'Rezerv Kaldırıldı' ? 'bilgi' : 'bekle');
  const riskRozet = r => r === '—' || !r ? '<span class="text-muted">—</span>' : `<span class="ib-risk ${r === 'Geç İbraz' ? 'gec' : r === 'Kritik' ? 'kritik' : 'ok'}">${esc(r)}</span>`;
  const kartRozet = d => rozet(d, d === 'Aktif' ? 'ok' : 'notr');
  const odemeRozet = o => `<span class="ib-odeme ${o === 'Akreditif (L/C)' ? 'lc' : o === 'Vesaik Mukabili' ? 'vesaik' : ''}">${esc(o)}</span>`;
  const kargoRozet = k => `<span class="ib-kargo ${k === 'DHL Express' ? 'dhl' : k === 'UPS' ? 'ups' : k === 'FedEx' ? 'fedex' : k === 'TNT' ? 'tnt' : k === 'Motorlu Kurye' ? 'kurye' : ''}">${esc(k)}</span>`;
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
  const lc = t => t.amac === 'Akreditif İbrazı';
  const vesaik = t => t.amac === 'Vesaik Tahsili';
  const musteriEvrak = t => t.amac === 'Müşteriye Orijinal Evrak' || t.amac === "Forwarder'a Evrak";
  const tasdik = t => t.amac === 'Konsolosluk Tasdiki' || t.amac === 'Ticaret Odası Onayı' || t.amac === 'Gümrük Müşavirine Evrak';
  const gec = t => t.ibrazRiski === 'Geç İbraz';
  const suresinde = t => lc(t) && teslim(t) && t.ibrazRiski === 'Süresinde';
  const rezerv = t => t.bankaDurumu === 'Rezerv Bildirildi' || t.bankaDurumu === 'Rezerv Kaldırıldı' || (t.rezervNotu && t.bankaDurumu === 'Ödendi');
  const odendi = t => t.bankaDurumu === 'Ödendi';
  const masraf = l => l.reduce((s, t) => s + Number(t.masraf), 0);
  const belge = l => l.reduce((s, t) => s + Number(t.belgeAdedi), 0);
  const tahsil = l => l.filter(odendi).reduce((s, t) => s + Number(t.faturaTutariTl), 0);
  const ortTeslim = l => { const d = l.filter(t => t.teslimSuresi !== null && t.teslimSuresi !== undefined); return d.length ? d.reduce((s, t) => s + t.teslimSuresi, 0) / d.length : null; };
  const cari = kod => CARI[kod] || {};
  const iliskiRozet = i => `<span class="ck-tip ${i === 'Grup İçi' ? 'grup' : i === 'İlişkili Cari' ? 'iliskili' : ''}">${esc(i || '—')}</span>`;
  const rolRozet = r => `<span class="ck-tip ${r === 'Çift Rol' ? 'cift' : ''}">${esc(r || '—')}</span>`;
  const sinifRozet = s => `<span class="ck-tip ${s === 'Özel Takip' ? 'ozel' : ''}">${esc(s || '—')}</span>`;
  const musteriHtml = t => { const c = cari(t.cariKodu); return `<b>${esc(t.cari)}</b><small>${esc(c.resmiUnvan || '')} · ${esc(t.ulke)}</small>`; };

  // ---- sekmeler
  function sekme(p) {
    $$('#ibSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p));
    $$('#ib > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p));
    if (V) { if (p === 'ozet') cizOzet(); if (p === 'musteri') cizMusteri(); if (p === 'liste') ciz(); }
    try { const u = new URL(location.href); if (p === 'ozet') u.searchParams.delete('sekme'); else u.searchParams.set('sekme', p); history.replaceState(null, '', u); } catch (e) { /* çerçeve içinde yoksay */ }
  }

  // ---- ortak yardımcılar (yıl bazlı)
  const yilVerisi = (yil, sf, of, gf) => V.gonderimler.filter(t => gecerli(t) && t.tarih.slice(0, 4) === String(yil) && (!sf || t.sirket === sf) && (!of || t.odemeSekli === of) && (!gf || t.gonderen === gf));
  const veriAyi = l => new Set(l.map(t => t.tarih.slice(5, 7))).size;
  const aylikTutar = (l, deger) => { const a = new Array(12).fill(0); l.forEach(t => a[+t.tarih.slice(5, 7) - 1] += Number(deger(t)) || 0); return a; };
  const aylikAdet = (l, f) => { const a = new Array(12).fill(0); l.forEach(t => { if (!f || f(t)) a[+t.tarih.slice(5, 7) - 1]++; }); return a; };
  const hucre = v => v ? kisa(v) : '<span class="text-muted">—</span>';
  function satirHtml(ad, a, ayN, cls, tip, ipucu) {
    if (tip === 'oran' || tip === 'gun') {
      const pay = a.reduce((s, x) => s + x.pay, 0), payda = a.reduce((s, x) => s + x.payda, 0);
      const f = (p, q) => q ? (tip === 'oran' ? yuzde(p, q) : ond(p / q, 1)) : '<span class="text-muted">—</span>';
      return `<tr class="${tip === 'oran' ? 'oran' : ''} ${cls || ''}"><td class="ad" title="${esc(ipucu || ad)}">${esc(ad)}</td>${a.map(x => `<td class="s">${f(x.pay, x.payda)}</td>`).join('')}<td class="s top">${f(pay, payda)}</td><td class="s ort">${f(pay, payda)}</td></tr>`;
    }
    const top = a.reduce((s, v) => s + v, 0);
    const f = tip === 'adet' ? (v => v ? kisa(v) : '<span class="text-muted">—</span>') : hucre;
    return `<tr class="${cls || ''}"><td class="ad" title="${esc(ipucu || ad)}">${esc(ad)}</td>${a.map(v => `<td class="s">${f(v)}</td>`).join('')}<td class="s top">${f(top)}</td><td class="s ort">${ayN && top ? (tip === 'adet' ? ond(top / ayN, 1) : kisa(top / ayN)) : '<span class="text-muted">—</span>'}</td></tr>`;
  }
  const oranAdet = (l, pay, payda) => { const p = aylikAdet(l, pay), q = aylikAdet(l, payda); return p.map((v, i) => ({ pay: v, payda: q[i] })); };
  const gunDizisi = l => { const p = aylikTutar(l, t => teslim(t) ? t.teslimSuresi : 0), q = aylikAdet(l, teslim); return p.map((v, i) => ({ pay: v, payda: q[i] })); };
  function yilSecenekleri(sel) {
    const yillar = [...new Set(V.gonderimler.map(t => t.tarih.slice(0, 4)))].sort().reverse(); if (!yillar.includes(String(V.yil))) yillar.unshift(String(V.yil));
    sel.innerHTML = yillar.map(y => `<option value="${y}">${y}</option>`).join(''); sel.value = String(V.yil);
  }
  function tabloCsv(tabloId, ad, yil) {
    const bas = ['Açıklama', ...AYLAR, 'Toplam ' + yil, 'Aylık Ort.'];
    csvIndir(ad + '_' + yil, bas, $$('#' + tabloId + ' tbody tr').map(tr => $$('td', tr).map(x => x.textContent.trim().replace('—', ''))));
  }

  // ---- sekme 1: aylık özet (sade)
  function cizOzet() {
    const yil = $('#ibOYil').value, sf = $('#ibOSirket').value, of = $('#ibOOdeme').value, gf = $('#ibOGonderen').value, l = yilVerisi(yil, sf, of, gf), ayN = veriAyi(l);
    const lcl = l.filter(lc), gc = l.filter(gec), rz = l.filter(rezerv), od = l.filter(odendi), ot = ortTeslim(l), yl = l.filter(yolda), kritik = l.filter(t => t.ibrazRiski === 'Kritik');
    $('#ibOYilBaslik').textContent = yil; $$('#ibAylik thead .yil').forEach(th => th.textContent = 'Toplam ' + yil);
    const kpi = [['#0f172a', 'Belge Gönderimi', l.length + ' Adet', `${belge(l)} Belge · ${new Set(l.map(t => t.dosyaNo)).size} İhracat Dosyası · ${new Set(l.map(t => t.cariKodu)).size} Müşteri`],
      ['#1e40af', 'Akreditif İbrazı', lcl.length + ' Adet', `${l.filter(vesaik).length} Vesaik · ${l.filter(musteriEvrak).length} Müşteri / Forwarder · ${l.filter(tasdik).length} Tasdik`],
      ['#b45309', 'Gönderim Masrafı', kisa(masraf(l)) + ' TL', `Ort. ${kisa(l.length ? masraf(l) / l.length : 0)} TL · ${l.filter(t => t.kargoFirmasi === 'DHL Express').length} DHL Express`],
      ['#7c3aed', 'Yolda / Teslim Bekleyen', yl.length + ' Adet', `${l.filter(teslim).length} Teslim Edildi · Ort. Teslim ${ot === null ? '—' : ond(ot, 1) + ' Gün'}`],
      ['#b91c1c', 'İbraz Riski', (gc.length + kritik.length) + ' Adet', `${gc.length} Geç İbraz · ${kritik.length} Kritik (≤ 3 Gün) · ${rz.length} Rezerv`],
      ['#166534', 'Tahsil Edilen', od.length + ' Adet', `${kisa(tahsil(l))} TL · ${l.filter(t => t.bankaDurumu === 'İnceleniyor' || t.bankaDurumu === 'Uygun Bulundu' || t.bankaDurumu === 'Rezerv Kaldırıldı').length} Banka Sürecinde`]];
    $('#ibOKpi').innerHTML = kpi.map(x => `<div style="--k:${x[0]};cursor:default"><div class="l">${x[1]}</div><div class="v">${x[2]}</div><div class="s">${esc(bh(x[3]))}</div></div>`).join('');
    $('#ibAylik tbody').innerHTML = l.length ? [
      satirHtml('Belge Gönderimi Adedi', aylikAdet(l), ayN, 'top', 'adet', 'İptal edilmemiş tüm gönderimler'),
      satirHtml('Akreditif İbrazı', aylikAdet(l, lc), ayN, 'r', 'adet'),
      satirHtml('Vesaik Tahsili', aylikAdet(l, vesaik), ayN, 'r', 'adet'),
      satirHtml('Müşteriye / Forwarder\'a Orijinal Evrak', aylikAdet(l, musteriEvrak), ayN, 'r', 'adet'),
      satirHtml('Konsolosluk / Ticaret Odası Tasdiki', aylikAdet(l, tasdik), ayN, 'r', 'adet'),
      satirHtml('Gönderilen Belge Adedi', aylikTutar(l, t => t.belgeAdedi), ayN, '', 'adet'),
      satirHtml('Gönderim Masrafı (Kargo)', aylikTutar(l, t => t.masraf), ayN, 'top', 'tl', 'Kargo faturası; döviz fatura günü kuruyla TL'),
      satirHtml('Teslim Edilen', aylikAdet(l, teslim), ayN, '', 'adet'),
      satirHtml('Süresinde İbraz Edilen Akreditif', aylikAdet(l, suresinde), ayN, '', 'adet', 'Teslim ≤ ibraz son tarihi'),
      satirHtml('Geç İbraz', aylikAdet(l, gec), ayN, 'r', 'adet', 'Teslim > ibraz son tarihi ya da süre doldu'),
      satirHtml('Süresinde İbraz Oranı', oranAdet(l, suresinde, t => lc(t) && teslim(t)), ayN, '', 'oran', 'Süresinde / teslim edilen akreditif ibrazı'),
      satirHtml('Rezerv Bildirilen', aylikAdet(l, rezerv), ayN, 'r', 'adet', 'Amir banka rezerv bildirdi (kaldırılanlar dahil)'),
      satirHtml('Bedeli Tahsil Edilen', aylikAdet(l, odendi), ayN, 'top', 'adet'),
      satirHtml('Tahsil Edilen Fatura Tutarı (TL)', aylikTutar(l, t => odendi(t) ? t.faturaTutariTl : 0), ayN, 'genel', 'tl', 'Ödendi durumundaki gönderimlerin fatura tutarı'),
      satirHtml('Ortalama Teslim Süresi (Gün)', gunDizisi(l), ayN, 'r', 'gun')
    ].join('') : '<tr><td colspan="15" class="na-bos">Bu yıl için gönderim kaydı yok.</td></tr>';
    // gönderen
    const gn = {}; l.forEach(t => { const p = gn[t.gonderen] || (gn[t.gonderen] = { n: 0, lc: 0, belge: 0, masraf: 0, sur: 0, gec: 0, rez: 0, ts: 0, tn: 0 }); p.n++; if (lc(t)) p.lc++; p.belge += t.belgeAdedi; p.masraf += Number(t.masraf); if (suresinde(t)) p.sur++; if (gec(t)) p.gec++; if (rezerv(t)) p.rez++; if (teslim(t)) { p.ts += t.teslimSuresi; p.tn++; } });
    $('#ibGonderen tbody').innerHTML = Object.keys(gn).sort((x, y) => gn[y].n - gn[x].n).map(x => { const p = gn[x]; return `<tr><td><b>${esc(x)}</b></td><td class="s">${p.n}</td><td class="s">${p.lc}</td><td class="s">${p.belge}</td><td class="s">${tl(p.masraf)}</td><td class="s">${p.sur}</td><td class="s">${p.gec || '<span class="text-muted">—</span>'}</td><td class="s">${p.rez || '<span class="text-muted">—</span>'}</td><td class="s">${p.tn ? ond(p.ts / p.tn, 1) : '—'}</td></tr>`; }).join('') || '<tr><td colspan="9" class="na-bos">Kayıt yok.</td></tr>';
    $('#ibGonderen tfoot').innerHTML = l.length ? `<tr><td>Toplam</td><td class="s">${l.length}</td><td class="s">${lcl.length}</td><td class="s">${belge(l)}</td><td class="s">${tl(masraf(l))}</td><td class="s">${l.filter(suresinde).length}</td><td class="s">${gc.length}</td><td class="s">${rz.length}</td><td class="s">${ot === null ? '—' : ond(ot, 1)}</td></tr>` : '';
    // kargo
    const kg = {}; l.forEach(t => { const kk = t.kargoFirmasi + '|' + t.gonderimTuru; const p = kg[kk] || (kg[kk] = { ad: t.kargoFirmasi, tur: t.gonderimTuru, n: 0, masraf: 0, ts: 0, tn: 0, teslim: 0, sorun: 0 }); p.n++; p.masraf += Number(t.masraf); if (teslim(t)) { p.teslim++; p.ts += t.teslimSuresi; p.tn++; } if (t.durum === 'Teslim Edilemedi') p.sorun++; });
    $('#ibKargo tbody').innerHTML = Object.keys(kg).sort((x, y) => kg[y].n - kg[x].n).map(x => { const p = kg[x]; return `<tr><td>${kargoRozet(p.ad)}</td><td>${esc(p.tur)}</td><td class="s">${p.n}</td><td class="s">${tl(p.masraf)}</td><td class="s">${tl(p.masraf / p.n)}</td><td class="s">${p.tn ? ond(p.ts / p.tn, 1) : '—'}</td><td class="s">${p.teslim}</td><td class="s">${p.sorun || '<span class="text-muted">—</span>'}</td></tr>`; }).join('') || '<tr><td colspan="8" class="na-bos">Kayıt yok.</td></tr>';
    $('#ibKargo tfoot').innerHTML = l.length ? `<tr><td>Toplam</td><td></td><td class="s">${l.length}</td><td class="s">${tl(masraf(l))}</td><td class="s">${tl(masraf(l) / l.length)}</td><td class="s">${ot === null ? '—' : ond(ot, 1)}</td><td class="s">${l.filter(teslim).length}</td><td class="s">${l.filter(t => t.durum === 'Teslim Edilemedi').length}</td></tr>` : '';
    // alıcı tipi
    const at = {}; l.forEach(t => { at[t.aliciTipi] = (at[t.aliciTipi] || 0) + 1; });
    const ak = V.aliciTipleri.filter(a => at[a]), maks = Math.max(1, ...ak.map(a => at[a]));
    $('#ibOAlici').innerHTML = ak.map(a => `<div class="ib-bar"><span>${esc(a)}</span><span class="b"><i style="width:${Math.max(2, at[a] / maks * 100)}%"></i></span><span class="v">${at[a]} <small>%${(at[a] / l.length * 100).toFixed(1)}</small></span></div>`).join('') || '<div class="na-bos">Kayıt yok.</div>';
    $('#ibONot').textContent = `${yil}${sf ? ' · ' + sf : ''}${of ? ' · ' + of : ''}${gf ? ' · ' + gf : ''} · ${l.length} gönderim · ${lcl.length} akreditif ibrazı · ${gc.length} geç ibraz · veri olan ay sayısı ${ayN} · Aylık Ort. = Toplam / ${ayN || '—'} · tutarlar TL`;
  }

  // ---- sekme 2: müşteri bazlı
  function musteriOzeti(yil, sf) {
    const l = V.gonderimler.filter(t => gecerli(t) && (!yil || t.tarih.slice(0, 4) === String(yil)) && (!sf || t.sirket === sf));
    const m = {};
    l.forEach(t => { const p = m[t.cariKodu] || (m[t.cariKodu] = { kod: t.cariKodu, n: 0, lc: 0, belge: 0, masraf: 0, fatura: 0, dosyalar: new Set(), gec: 0, rez: 0, od: 0, son: '', sonTr: '', sirketler: new Set() }); p.n++; if (lc(t)) p.lc++; p.belge += t.belgeAdedi; p.masraf += Number(t.masraf); if (!p.dosyalar.has(t.dosyaNo)) { p.dosyalar.add(t.dosyaNo); p.fatura += Number(t.faturaTutariTl); } if (gec(t)) p.gec++; if (rezerv(t)) p.rez++; if (odendi(t)) p.od++; if (t.tarih > p.son) { p.son = t.tarih; p.sonTr = t.tarihTr; } p.sirketler.add(t.sirket); });
    return Object.values(m);
  }
  function cizMusteri() {
    const yil = $('#ibMYil').value, sf = $('#ibMSirket').value, v = id => $(id).value, ara = ($('#ibMAra').value || '').toLocaleLowerCase('tr-TR');
    let l = musteriOzeti(yil, sf).map(p => ({ ...p, c: cari(p.kod) })).filter(p => { const c = p.c; return (!v('#ibMUlke') || c.ulke === v('#ibMUlke')) && (!v('#ibMSinif') || c.sinif === v('#ibMSinif')) && (!v('#ibMIliski') || c.iliskiDurumu === v('#ibMIliski')) && (!v('#ibMRol') || c.rol === v('#ibMRol')) && (!v('#ibMDurum') || c.durum === v('#ibMDurum')) && (!v('#ibMVade') || c.vadeSarti === v('#ibMVade')) && (!v('#ibMErp') || c.sirket === v('#ibMErp'))
      && (!v('#ibMTemsilci') || (c.faaliyetler || []).some(f => f.satisTemsilcisi === v('#ibMTemsilci'))) && (!ara || [p.kod, c.kisaAd, c.resmiUnvan, c.vergiNo, c.yetkili, c.eposta, c.sehir, c.esCariKodu].join(' ').toLocaleLowerCase('tr-TR').includes(ara)); });
    const s = $('#ibMSira').value;
    l.sort((a, b) => s === 'kod' ? a.kod.localeCompare(b.kod) : s === 'adet' ? b.n - a.n : s === 'masraf' ? b.masraf - a.masraf : b.fatura - a.fatura);
    $('#ibMusteriTablo tbody').innerHTML = l.map(p => { const c = p.c; return `<tr class="tik" data-kod="${esc(p.kod)}"><td><b>${esc(p.kod)}</b></td><td><b>${esc(c.kisaAd || '')}</b><br><small class="text-muted">${esc(c.resmiUnvan || '')}</small></td><td>${esc(c.vergiNo || '')}<br><small class="text-muted">${esc(c.vergiDairesi || '—')}</small></td><td>${esc(c.kartTipi || '')}</td><td>${esc(c.bolge || '')} · ${esc(c.ulke || '')}${c.sehir ? ' · ' + esc(c.sehir) : ''}</td><td>${sinifRozet(c.sinif)}</td><td>${iliskiRozet(c.iliskiDurumu)}</td><td>${rolRozet(c.rol)}${c.esCariKodu ? `<br><small class="text-muted">eş ${esc(c.esCariKodu)}</small>` : ''}</td><td>${bos(c.anaFaaliyet)}</td><td class="s">${(c.faaliyetler || []).length}</td><td><small>${bos(c.temsilciler)}</small></td><td>${bos(c.vadeSarti)}</td><td>${kartRozet(c.durum || 'Aktif')}</td><td>${bos(c.sirket)}<small>İhracatçı: ${esc([...p.sirketler].join(', '))}</small></td><td class="s"><b>${p.n}</b><small>${p.dosyalar.size} Dosya</small></td><td class="s">${p.lc}</td><td class="s">${p.belge}</td><td class="s">${tl(p.masraf)}</td><td class="s">${tl(p.fatura)}</td><td class="s">${p.gec || '<span class="text-muted">—</span>'}</td><td class="s">${p.rez || '<span class="text-muted">—</span>'}</td><td class="s">${p.od}</td><td>${esc(p.sonTr)}</td></tr>`; }).join('') || '<tr><td colspan="23" class="na-bos">Filtreye uyan müşteri yok.</td></tr>';
    const top = (f) => l.reduce((s, p) => s + p[f], 0);
    $('#ibMTopAdet').textContent = top('n'); $('#ibMTopLc').textContent = top('lc'); $('#ibMTopBelge').textContent = top('belge'); $('#ibMTopMasraf').textContent = tl(top('masraf')); $('#ibMTopFatura').textContent = tl(top('fatura')); $('#ibMTopGec').textContent = top('gec'); $('#ibMTopRezerv').textContent = top('rez'); $('#ibMTopOdendi').textContent = top('od');
    $('#ibMSayi').textContent = `${l.length} müşteri · ${yil}${sf ? ' · ' + sf : ''} · satıra tıklayınca cari kart bilgileri ve gönderim geçmişi açılır`;
    $('#ibNMusteri').textContent = l.length;
    $$('#ibMusteriTablo tr.tik').forEach(r => r.addEventListener('click', () => cariDrawer(r.dataset.kod)));
    cizMusteri.son = l;
  }
  function musteriExcel() {
    const l = cizMusteri.son || [];
    csvIndir('musteri_bazli_ihracat_belge_' + $('#ibMYil').value, ['Cari Kodu', 'Kısa Ad', 'Kısaltmalı Ünvan', 'Resmi Ünvan', 'Vergi No', 'Vergi Dairesi', 'Kart Tipi', 'Bölge', 'Ülke', 'Şehir', 'Sınıf', 'İlişki Durumu', 'Rol', 'Eş Cari', 'Yetkili', 'Telefon', 'E-posta', 'Vade', 'Durum', 'Ana Faaliyet', 'Faaliyet Grupları', 'Satış Temsilcileri', 'Şirket', 'Not', 'İhracatçı Şirketler', 'Gönderim', 'İhracat Dosyası', 'Akreditif İbrazı', 'Belge', 'Masraf (TL)', 'Fatura Tutarı (TL)', 'Geç İbraz', 'Rezerv', 'Ödendi', 'Son Gönderim'],
      l.map(p => { const c = p.c; return [p.kod, c.kisaAd, c.kisaltmaliUnvan, c.resmiUnvan, c.vergiNo, c.vergiDairesi, c.kartTipi, c.bolge, c.ulke, c.sehir, c.sinif, c.iliskiDurumu, c.rol, c.esCariKodu, c.yetkili, c.telefon, c.eposta, c.vadeSarti, c.durum, c.anaFaaliyet, (c.faaliyetler || []).map(f => f.anaGrup + ' / ' + f.grup).join(' | '), c.temsilciler, c.sirket, c.not, [...p.sirketler].join(', '), p.n, p.dosyalar.size, p.lc, p.belge, sayi(p.masraf), sayi(p.fatura), p.gec, p.rez, p.od, p.sonTr]; }));
  }

  // ---- sekme 3: liste
  function aralik() {
    const d = $('#ibDonem').value, bugun = new Date(V.bugun + 'T00:00:00');
    const ayBas = new Date(bugun.getFullYear(), bugun.getMonth(), 1);
    if (d === 'buay') return [iso(ayBas), V.bugun];
    if (d === 'gecenay') { const b = new Date(bugun.getFullYear(), bugun.getMonth() - 1, 1), e = new Date(bugun.getFullYear(), bugun.getMonth(), 0); return [iso(b), iso(e)]; }
    if (d === '30' || d === '90') { const b = new Date(bugun); b.setDate(b.getDate() - Number(d) + 1); return [iso(b), V.bugun]; }
    if (d === 'yil') return [bugun.getFullYear() + '-01-01', V.bugun];
    if (d === 'ozel') return [$('#ibBas').value || '', $('#ibBit').value || ''];
    return ['', ''];
  }
  function donemDegisti() { const ozel = $('#ibDonem').value === 'ozel'; $('#ibBas').disabled = $('#ibBit').disabled = !ozel; if (!ozel) { const [b, e] = aralik(); $('#ibBas').value = b; $('#ibBit').value = e; } ciz(); }
  function liste() {
    const ara = ($('#ibAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value, [bas, bit] = aralik();
    return V.gonderimler.filter(t => { const c = cari(t.cariKodu); return (!bas || t.tarih >= bas) && (!bit || t.tarih <= bit) && (!v('#ibSirket') || t.sirket === v('#ibSirket')) && (!v('#ibOdeme') || t.odemeSekli === v('#ibOdeme')) && (!v('#ibAmacF') || t.amac === v('#ibAmacF')) && (!v('#ibAliciTipi') || t.aliciTipi === v('#ibAliciTipi')) && (!v('#ibKargoF') || t.kargoFirmasi === v('#ibKargoF')) && (!v('#ibDurum') || t.durum === v('#ibDurum'))
      && (!v('#ibBanka') || t.bankaDurumu === v('#ibBanka')) && (!v('#ibRiskF') || t.ibrazRiski === v('#ibRiskF')) && (!v('#ibBelgeTur') || t.belgeler.some(b => b.tur === v('#ibBelgeTur'))) && (!v('#ibGonderenF') || t.gonderen === v('#ibGonderenF'))
      && (!v('#ibUlke') || t.ulke === v('#ibUlke')) && (!v('#ibSinif') || c.sinif === v('#ibSinif')) && (!v('#ibIliski') || c.iliskiDurumu === v('#ibIliski'))
      && (!ara || [t.no, t.gonderimNo, t.dosyaNo, t.faturaNo, t.akreditifNo, t.amirBanka, t.alici, t.cari, t.cariKodu, t.sirket, t.gonderen, t.kargoFirmasi, c.resmiUnvan, ...t.belgeler.map(b => b.no)].join(' ').toLocaleLowerCase('tr-TR').includes(ara)); });
  }
  function ciz() {
    L = liste();
    const g = L.filter(gecerli), lcl = g.filter(lc), yl = g.filter(yolda), gc = g.filter(gec), kritik = g.filter(t => t.ibrazRiski === 'Kritik'), rz = g.filter(rezerv), od = g.filter(odendi);
    const kpi = [['#0f172a', 'Belge Gönderimi', g.length + ' Adet', `${belge(g)} Belge · ${new Set(g.map(t => t.dosyaNo)).size} Dosya · ${new Set(g.map(t => t.cariKodu)).size} Müşteri`, ''],
      ['#1e40af', 'Akreditif İbrazı', lcl.length + ' Adet', `${g.filter(vesaik).length} Vesaik · ${g.filter(musteriEvrak).length} Müşteri / Forwarder`, 'amac:Akreditif İbrazı'],
      ['#b45309', 'Gönderim Masrafı', kisa(masraf(g)) + ' TL', `Ort. ${kisa(g.length ? masraf(g) / g.length : 0)} TL · ${g.filter(t => t.kargoFirmasi === 'DHL Express').length} DHL Express`, ''],
      ['#7c3aed', 'Yolda / Teslim Bekleyen', yl.length + ' Adet', `${g.filter(t => t.durum === 'Teslim Edilemedi').length} Teslim Edilemedi`, 'durum:Yolda'],
      ['#b91c1c', 'İbraz Riski', (gc.length + kritik.length) + ' Adet', `${gc.length} Geç İbraz · ${kritik.length} Kritik · ${rz.length} Rezerv`, 'risk:Geç İbraz'],
      ['#166534', 'Tahsil Edilen', od.length + ' Adet', `${kisa(tahsil(g))} TL`, 'banka:Ödendi']];
    $('#ibKpi').innerHTML = kpi.map(x => `<div style="--k:${x[0]}" data-k="${esc(x[4])}" title="Tıklayınca filtre uygular"><div class="l">${x[1]}</div><div class="v">${x[2]}</div><div class="s">${esc(bh(x[3]))}</div></div>`).join('');
    $$('#ibKpi > div').forEach(x => x.addEventListener('click', () => { const k = x.dataset.k; ['#ibAmacF', '#ibDurum', '#ibRiskF', '#ibBanka'].forEach(s => $(s).value = ''); if (k) { const [a, b] = k.split(':'); $(a === 'amac' ? '#ibAmacF' : a === 'durum' ? '#ibDurum' : a === 'risk' ? '#ibRiskF' : '#ibBanka').value = b; } ciz(); }));
    $('#ibTablo tbody').innerHTML = L.map(t => `<tr class="tik" data-id="${t.id}"><td>${esc(t.tarihTr)}</td><td><b>${esc(t.no)}</b></td><td>${esc(t.sirket)}</td><td>${esc(t.dosyaNo)}<small>${esc(t.faturaNo)} · ${tl(t.faturaTutari)} ${esc(t.paraBirimi)}</small></td><td>${esc(t.cariKodu)}</td><td>${musteriHtml(t)}</td><td>${odemeRozet(t.odemeSekli)}</td><td><b>${esc(t.alici)}</b><small>${esc(t.aliciTipi)}</small></td><td>${esc(t.amac)}</td><td><small style="display:block;max-width:220px;white-space:normal">${esc(t.belgeOzeti)}</small><small>${t.belgeAdedi} Belge</small></td><td>${t.akreditifNo ? `${esc(t.akreditifNo)}<small>İbraz son ${esc(t.ibrazSonTr)}${t.ibrazKalanGun !== null && !t.teslimTr ? ' · ' + t.ibrazKalanGun + ' gün' : ''}</small>` : '<span class="text-muted">—</span>'}</td><td>${riskRozet(t.ibrazRiski)}</td><td>${kargoRozet(t.kargoFirmasi)}<small>${esc(t.gonderimTuru)}</small></td><td class="ib-takip">${esc(t.gonderimNo)}</td><td class="s">${tl(t.masraf)}${t.masrafPB !== 'TL' ? `<small>${tl(t.masrafDoviz)} ${esc(t.masrafPB)}</small>` : ''}</td><td>${durumRozet(t.durum)}</td><td>${t.teslimTr ? esc(t.teslimTr) + `<small>${t.teslimSuresi} Gün · ${esc(t.teslimAlan)}</small>` : `<small>Tahmini ${esc(t.tahminiTeslimTr)}</small>`}</td><td>${bankaRozet(t.bankaDurumu)}${t.rezervNotu ? `<small>${esc(t.rezervNotu)}</small>` : t.odemeTr ? `<small>${esc(t.odemeTr)}</small>` : ''}</td><td>${esc(t.gonderen)}</td></tr>`).join('') || '<tr><td colspan="19" class="na-bos">Filtreye uyan gönderim yok.</td></tr>';
    $('#ibTopBelge').textContent = belge(g) + ' Belge'; $('#ibTopSayi').textContent = g.length + ' Gönderim · ' + lcl.length + ' Akreditif İbrazı'; $('#ibTopMasraf').textContent = tl(masraf(g)) + ' TL'; $('#ibTopSonuc').innerHTML = `${g.filter(teslim).length} Teslim · ${gc.length} Geç İbraz · ${rz.length} Rezerv · ${od.length} Ödendi (${kisa(tahsil(g))} TL)`;
    $('#ibSayi').textContent = `${L.length} / ${V.gonderimler.length} gönderim · ${L.length - g.length} iptal (toplama girmez) · satıra tıklayınca belge seti, akreditif / ibraz, kargo takibi ve cari kart bilgileri açılır`;
    $('#ibNListe').textContent = L.length;
    $$('#ibTablo tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    // ibraz riski listesi (tüm dönem; filtre bağımsız — teslim edilmemiş akreditif ibrazları + filtredeki geç ibrazlar)
    const risk = V.gonderimler.filter(t => gecerli(t) && lc(t) && (!teslim(t) || gec(t))).sort((a, b) => (a.ibrazKalanGun ?? 999) - (b.ibrazKalanGun ?? 999));
    $('#ibRisk tbody').innerHTML = risk.map(t => `<tr class="tik" data-id="${t.id}"><td><b>${esc(t.no)}</b><small>${esc(t.sirket)}</small></td><td>${esc(t.cari)}<small>${esc(t.ulke)}</small></td><td>${esc(t.akreditifNo)}</td><td>${esc(t.amirBanka)}</td><td>${esc(t.ibrazSonTr)}</td><td class="s">${t.ibrazKalanGun}</td><td>${riskRozet(t.ibrazRiski)}</td><td>${kargoRozet(t.kargoFirmasi)}<small class="ib-takip">${esc(t.gonderimNo)}</small></td><td>${durumRozet(t.durum)}${t.teslimTr ? `<small>${esc(t.teslimTr)}</small>` : ''}</td></tr>`).join('') || '<tr><td colspan="9" class="na-bos">Açık ibraz riski yok.</td></tr>';
    $$('#ibRisk tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    // şirket × amaç
    const m = {}; g.forEach(t => { const p = m[t.sirket] || (m[t.sirket] = { lc: 0, ves: 0, mus: 0, tas: 0, n: 0, belge: 0, masraf: 0, gec: 0 }); if (lc(t)) p.lc++; else if (vesaik(t)) p.ves++; else if (musteriEvrak(t)) p.mus++; else p.tas++; p.n++; p.belge += t.belgeAdedi; p.masraf += Number(t.masraf); if (gec(t)) p.gec++; });
    const satir = (ad, p, kalin) => `<tr class="${kalin ? 'toplam' : ''}"><td>${esc(ad)}</td><td class="s">${p.lc}</td><td class="s">${p.ves}</td><td class="s">${p.mus}</td><td class="s">${p.tas}</td><td class="s"><b>${p.n}</b></td><td class="s">${p.belge}</td><td class="s">${tl(p.masraf)}</td><td class="s">${p.gec || '<span class="text-muted">—</span>'}</td></tr>`;
    const sk = V.sirketler.filter(s => m[s]); const tm = { lc: 0, ves: 0, mus: 0, tas: 0, n: 0, belge: 0, masraf: 0, gec: 0 }; sk.forEach(s => Object.keys(tm).forEach(kk => tm[kk] += m[s][kk]));
    $('#ibMatris tbody').innerHTML = sk.length ? sk.map(s => satir(s, m[s])).join('') + satir('Konsolide Toplam', tm, true) : '<tr><td colspan="9" class="na-bos">Kayıt yok.</td></tr>';
    // ay
    const ay = {}; g.forEach(t => { const kk = t.tarih.slice(0, 7); const p = ay[kk] || (ay[kk] = { n: 0, lc: 0, belge: 0, masraf: 0, teslim: 0, sur: 0, gec: 0, ts: 0, tn: 0 }); p.n++; if (lc(t)) p.lc++; p.belge += t.belgeAdedi; p.masraf += Number(t.masraf); if (teslim(t)) { p.teslim++; p.ts += t.teslimSuresi; p.tn++; } if (suresinde(t)) p.sur++; if (gec(t)) p.gec++; });
    const ayAdi = kk => AYLAR[+kk.slice(5, 7) - 1] + ' ' + kk.slice(0, 4);
    const aySatir = (ad, p, kalin) => `<tr class="${kalin ? 'toplam' : ''}"><td>${esc(ad)}</td><td class="s">${p.n}</td><td class="s">${p.lc}</td><td class="s">${p.belge}</td><td class="s">${tl(p.masraf)}</td><td class="s">${p.teslim}</td><td class="s">${p.sur}</td><td class="s">${p.gec || '<span class="text-muted">—</span>'}</td><td class="s">${p.tn ? ond(p.ts / p.tn, 1) : '—'}</td></tr>`;
    const ak = Object.keys(ay).sort((x, y) => y.localeCompare(x)); const ta = { n: g.length, lc: lcl.length, belge: belge(g), masraf: masraf(g), teslim: g.filter(teslim).length, sur: g.filter(suresinde).length, gec: gc.length, ts: g.filter(teslim).reduce((s, t) => s + t.teslimSuresi, 0), tn: g.filter(teslim).length };
    $('#ibAyMatris tbody').innerHTML = ak.length ? ak.map(kk => aySatir(ayAdi(kk), ay[kk])).join('') + aySatir('Toplam', ta, true) : '<tr><td colspan="9" class="na-bos">Kayıt yok.</td></tr>';
    // kargo dağılımı
    const kg = {}; g.forEach(t => { const p = kg[t.kargoFirmasi] || (kg[t.kargoFirmasi] = { n: 0, top: 0 }); p.n++; p.top += Number(t.masraf); });
    const kl = Object.keys(kg).sort((x, y) => kg[y].n - kg[x].n), maks = kl.length ? kg[kl[0]].n : 1;
    $('#ibKargoDagilim').innerHTML = kl.map(x => `<div class="ib-bar"><span>${kargoRozet(x)}</span><span class="b"><i style="width:${Math.max(2, kg[x].n / maks * 100)}%"></i></span><span class="v">${kg[x].n} <small>· ${kisa(kg[x].top)} TL</small></span></div>`).join('') || '<div class="na-bos">Kayıt yok.</div>';
    // belge türü dağılımı
    const bt = {}; g.forEach(t => t.belgeler.forEach(b => { bt[b.tur] = (bt[b.tur] || 0) + b.adet; }));
    const bl = Object.keys(bt).sort((x, y) => bt[y] - bt[x]), bmaks = bl.length ? bt[bl[0]] : 1;
    $('#ibBelgeDagilim').innerHTML = bl.map(x => `<div class="ib-bar"><span title="${esc(x)}">${esc(x)}</span><span class="b"><i style="width:${Math.max(2, bt[x] / bmaks * 100)}%"></i></span><span class="v">${bt[x]} <small>Belge</small></span></div>`).join('') || '<div class="na-bos">Kayıt yok.</div>';
  }

  // ---- drawer
  function cariKartHtml(c) {
    if (!c || !c.kod) return '<div class="na-kart"><div class="na-kart-g na-bos">Cari kart bulunamadı.</div></div>';
    const a = [['1. Cari Kodu', c.kod], ['2. Cari Kısa Adı', c.kisaAd], ['3. Kısaltmalı Ünvan', c.kisaltmaliUnvan], ['4. Resmi Ünvan', c.resmiUnvan], ['5. Vergi No / TCKN', c.vergiNo], ['6. Vergi Dairesi', c.vergiDairesi], ['7. Cari Kart Tipi', c.kartTipi], ['8. Cari Bölge', c.bolge], ['9. Ülke', c.ulke], ['10. Şehir', c.sehir],
      ['11. Cari Sınıfı', c.sinif], ['12. İlişki Durumu', c.iliskiDurumu], ['13. Cari Rolü', c.rol], ['14. Eş Cari Kodu', c.esCariKodu ? c.esCariKodu + (c.esKartAd ? ' · ' + c.esKartAd : '') : ''], ['15. Yetkili Kişi', c.yetkili], ['16. Telefon', c.telefon], ['17. E-posta', c.eposta], ['18. Vade / Ödeme Şartı', c.vadeSarti], ['19. Durum', c.durum], ['Şirket (ERP)', c.sirket]];
    const f = c.faaliyetler || [];
    return `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-database me-1"></i>CARİ_MASTER</span><span class="not">${esc(c.olusturmaTarihi || '')}${c.olusturan ? ' · ' + esc(c.olusturan) : ''}</span></div><div class="na-kart-g"><div class="ck-master">${a.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}<div style="grid-column:1/-1"><div class="l">20. Not</div><div class="v" style="font-weight:500">${bos(c.not)}</div></div></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-diagram-project me-1"></i>CARİ_FAALİYET_EŞLEŞTİRME (${f.length})</span><span class="not">faaliyet grubu bazında satış temsilcisi · ana faaliyet tek satırda Evet</span></div><div class="na-kart-g p-0">${f.length ? `<div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>#</th><th>Faaliyet Ana Grubu</th><th>Faaliyet Grubu</th><th>Satış Temsilcisi</th><th>Ana Faaliyet</th><th>Ürün Sınıfı › Grubu › Serisi › Adı</th><th>Durum</th><th>Başlangıç</th><th>Not</th></tr></thead><tbody>${f.map(x => `<tr class="${x.anaFaaliyet ? 'table-active' : ''}"><td>${x.sira}</td><td>${esc(x.anaGrup)}</td><td><b>${esc(x.grup)}</b></td><td>${bos(x.satisTemsilcisi)}</td><td>${x.anaFaaliyet ? rozet('Evet', 'ok') : 'Hayır'}</td><td><small>${esc([x.urunSinifi, x.urunGrubu, x.urunSerisi, x.urunAdi].filter(Boolean).join(' › ') || '—')}</small></td><td>${kartRozet(x.durum)}</td><td>${bos(x.baslangicTr)}</td><td><small>${esc(x.not || '')}</small></td></tr>`).join('')}</tbody></table></div>` : '<div class="na-bos">Faaliyet eşleştirmesi yok.</div>'}</div></div>`;
  }
  function gecmisHtml(kod, seciliId) {
    const l = V.gonderimler.filter(t => t.cariKodu === kod), g = l.filter(gecerli);
    return `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-clock-rotate-left me-1"></i>Müşteriye Yapılan Belge Gönderimleri</span><span class="not">Tüm dönem · ${g.length} gönderim · ${g.filter(lc).length} akreditif ibrazı · masraf ${tl(masraf(g))} TL</span></div><div class="na-kart-g p-0"><div class="table-responsive" style="max-height:280px"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>Tarih</th><th>Gönderim No</th><th>Dosya</th><th>Amaç</th><th>Alıcı</th><th>Kargo · Takip No</th><th class="s">Masraf (TL)</th><th>Durum</th><th>İbraz</th><th>Banka</th></tr></thead><tbody>${l.map(x => `<tr class="tik ${x.id === seciliId ? 'table-active' : ''}" data-id="${x.id}"><td>${esc(x.tarihTr)}</td><td>${esc(x.no)}</td><td>${esc(x.dosyaNo)}</td><td>${esc(x.amac)}</td><td>${esc(x.alici)}</td><td>${kargoRozet(x.kargoFirmasi)}<small class="ib-takip">${esc(x.gonderimNo)}</small></td><td class="s">${tl(x.masraf)}</td><td>${durumRozet(x.durum)}</td><td>${riskRozet(x.ibrazRiski)}</td><td>${bankaRozet(x.bankaDurumu)}</td></tr>`).join('')}</tbody></table></div></div></div>`;
  }
  function ac(url, baslik) { if (window.top && window.top !== window && typeof window.top.portalSekmeAc === 'function') window.top.portalSekmeAc(baslik, url); else window.open(url + (sekmeli ? (url.includes('?') ? '&' : '?') + '_sekme=1' : ''), '_blank'); }
  function drawerAksiyon(c, t) {
    const b = [`<button type="button" class="btn btn-light btn-sm py-0" data-ac="/CariKart/Musteri?kod=${encodeURIComponent(c.kod || '')}" data-b="Müşteri Cari Kartları" style="font-size:.66rem"><i class="fas fa-address-card me-1"></i>Cari Kartı Aç</button>`];
    if (t && t.gonderimNoLink) b.unshift(`<a class="btn btn-light btn-sm py-0" href="${esc(t.gonderimNoLink)}" target="_blank" rel="noopener" style="font-size:.66rem"><i class="fas fa-location-crosshairs me-1"></i>${esc(t.kargoFirmasi)} Takip</a>`);
    $('#ibDrawerAksiyon').innerHTML = b.join('');
    $$('#ibDrawerAksiyon [data-ac]').forEach(x => x.addEventListener('click', () => ac(x.dataset.ac, x.dataset.b)));
  }
  function drawerAc(id) {
    const t = V.gonderimler.find(x => x.id === id); if (!t) return; const c = cari(t.cariKodu);
    $('#ibDrawerBaslik').innerHTML = `<i class="fas fa-file-shield me-1"></i>${esc(t.no)} · ${esc(t.sirket)} → ${esc(t.alici)} ${odemeRozet(t.odemeSekli)} ${durumRozet(t.durum)}`;
    drawerAksiyon(c, t);
    const a = [['Gönderim No', t.no], ['Kargoya Veriliş', t.tarihTr], ['İhracatçı Şirket', t.sirket], ['Müşteri', t.cariKodu + ' · ' + t.cari + ' · ' + t.ulke], ['İhracat Dosyası', t.dosyaNo], ['Fatura', t.faturaNo + ' · ' + t.faturaTarihiTr], ['Fatura Tutarı', tl(t.faturaTutari) + ' ' + t.paraBirimi + ' · ' + tl(t.faturaTutariTl) + ' TL (kur ' + tl(t.kur) + ')'], ['Ödeme Şekli', t.odemeSekli], ['Teslim Şekli', t.teslimSekli], ['Konşimento / Yükleme Tarihi', t.yuklemeTr],
      ['Amaç', t.amac], ['Alıcı Tipi', t.aliciTipi], ['Alıcı', t.alici], ['Alıcı Adresi', t.aliciAdres], ['Alıcı Kişi / Birim', t.aliciKisi], ['Gönderen Operasyon Uzmanı', t.gonderen], ['Onaylayan', t.onaylayan], ['Kaynak Sistem', t.kaynak]];
    const k = [['Kargo Firması', t.kargoFirmasi], ['Gönderim Türü', t.gonderimTuru], ['Gönderim (Takip) No', t.gonderimNo], ['Zarf', t.zarfAdedi + ' Doküman Zarfı · ' + ond(t.agirlik, 2) + ' kg'], ['Kargo Faturası', t.masrafPB !== 'TL' ? `${tl(t.masrafDoviz)} ${t.masrafPB} · ${tl(t.masraf)} TL` : tl(t.masraf) + ' TL'], ['Masraf Kime', t.masrafKime], ['Tahmini Teslim', t.tahminiTeslimTr], ['Teslim Tarihi', t.teslimTr ? `${t.teslimTr} (${t.teslimSuresi} gün)` : ''], ['Teslim Alan', t.teslimAlan], ['Son Hareket', t.sonHareket]];
    const tipCls = h => h.tip === 'teslim' ? 'fin' : h.tip === 'gumruk' ? 'wf' : h.tip === 'kargo' ? 'muh' : h.tip === 'hata' ? '' : 'sis';
    const lcHtml = t.akreditifNo ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-landmark me-1"></i>Akreditif ve İbraz</span><span class="not">${riskRozet(t.ibrazRiski)} ${bankaRozet(t.bankaDurumu)}</span></div><div class="na-kart-g"><div class="ib-ibraz"><div><div class="l">Konşimento Tarihi</div><div class="v">${esc(t.yuklemeTr)}</div></div><div class="${t.ibrazRiski === 'Geç İbraz' ? 'gec' : t.ibrazRiski === 'Kritik' ? 'kritik' : 'ok'}"><div class="l">İbraz Son Tarihi</div><div class="v">${esc(t.ibrazSonTr)}</div></div><div><div class="l">${t.teslimTr ? 'Bankaya Teslim' : 'Kalan Gün'}</div><div class="v">${t.teslimTr ? esc(t.teslimTr) : t.ibrazKalanGun}</div></div><div><div class="l">Akreditif Vadesi</div><div class="v">${esc(t.akreditifVadesiTr)}</div></div></div><div class="ib-master">${[['Akreditif No', t.akreditifNo], ['Amir Banka', t.amirBanka], ['İhbar / Teyit Bankası', t.ihbarBankasi], ['Banka Sonucu', t.bankaDurumu], ['Rezerv Notu', t.rezervNotu], ['Ödeme Tarihi', t.odemeTr]].map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}</div></div></div>` : (t.bankaDurumu && t.bankaDurumu !== '—' ? `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-landmark me-1"></i>Tahsil Bankası Sonucu</span><span class="not">${bankaRozet(t.bankaDurumu)}</span></div><div class="na-kart-g"><div class="ib-master">${[['Tahsil Bankası', t.alici], ['Banka Sonucu', t.bankaDurumu], ['Ödeme Tarihi', t.odemeTr]].map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}</div></div></div>` : '');
    $('#ibDrawerIc').innerHTML = `<div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-receipt me-1"></i>Gönderim ve İhracat Dosyası</span><span class="not">${esc(t.kaynak)}</span></div><div class="na-kart-g"><div class="ib-master">${a.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${bos(v)}</div></div>`).join('')}<div style="grid-column:1/-1"><div class="l">Not</div><div class="v" style="font-weight:500">${bos(t.not)}</div></div></div></div></div>
      ${lcHtml}
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-file-lines me-1"></i>Belge Seti</span><span class="not">${t.belgeAdedi} belge · ${t.belgeTurSayisi} tür</span></div><div class="na-kart-g p-0"><div class="table-responsive"><table class="na-tablo" data-dinamik="kapali"><thead><tr><th>#</th><th>Belge Türü</th><th>Belge No</th><th class="s">Adet</th><th>Nitelik</th><th>Not</th></tr></thead><tbody>${t.belgeler.map(b => `<tr><td>${b.sira}</td><td><b>${esc(b.tur)}</b></td><td class="ib-takip">${esc(b.no)}</td><td class="s">${b.adet}</td><td>${esc(b.nitelik)}</td><td><small>${esc(b.not || '')}</small></td></tr>`).join('')}</tbody></table></div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-truck-fast me-1"></i>Kargo ve Masraf</span><span class="not">${kargoRozet(t.kargoFirmasi)}</span></div><div class="na-kart-g"><div class="ib-master">${k.map(([l, v]) => `<div><div class="l">${l}</div><div class="v">${l === 'Gönderim (Takip) No' ? `<span class="ib-takip">${bos(v)}</span>` : bos(v)}</div></div>`).join('')}</div></div></div>
      <div class="na-kart"><div class="na-kart-b"><span><i class="fas fa-route me-1"></i>Kargo Hareketleri ve Banka Süreci</span><span class="not">${t.hareketler.length} hareket</span></div><div class="na-kart-g"><ul class="na-zaman">${t.hareketler.map(h => `<li class="${tipCls(h)}"><span class="t">${esc(h.zaman)}</span><b>${esc(h.yer)}</b> · ${esc(h.aciklama)}${h.tip === 'hata' ? ' ' + rozet('Sorun', 'hata') : ''}</li>`).join('') || '<li>Henüz hareket yok.</li>'}</ul></div></div>
      ${cariKartHtml(c)}${gecmisHtml(t.cariKodu, t.id)}`;
    $$('#ibDrawerIc tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    $('#ibDrawer').classList.add('acik'); $('#ibPerde').classList.add('acik'); $('#ibDrawer').scrollTop = 0;
  }
  function cariDrawer(kod) {
    const c = cari(kod); if (!c.kod) { mesaj('warning', esc(kod) + ' için cari kart bulunamadı.'); return; }
    $('#ibDrawerBaslik').innerHTML = `<i class="fas fa-address-card me-1"></i>${esc(c.kod)} · ${esc(c.kisaAd)} ${kartRozet(c.durum || 'Aktif')}`;
    drawerAksiyon(c, null);
    $('#ibDrawerIc').innerHTML = cariKartHtml(c) + gecmisHtml(kod, 0);
    $$('#ibDrawerIc tr.tik').forEach(r => r.addEventListener('click', () => drawerAc(+r.dataset.id)));
    $('#ibDrawer').classList.add('acik'); $('#ibPerde').classList.add('acik'); $('#ibDrawer').scrollTop = 0;
  }
  function drawerKapat() { $('#ibDrawer').classList.remove('acik'); $('#ibPerde').classList.remove('acik'); }

  // ---- excel
  function excel() {
    csvIndir('ihracat_belge_gonderimleri', ['Tarih', 'Gönderim No', 'İhracatçı Şirket', 'İhracat Dosyası', 'Fatura No', 'Fatura Tarihi', 'Fatura Tutarı', 'Para Birimi', 'Kur', 'Fatura Tutarı (TL)', 'Ödeme Şekli', 'Teslim Şekli', 'Konşimento Tarihi', 'Cari Kodu', 'Müşteri', 'Resmi Ünvan', 'Ülke', 'Şehir', 'Alıcı Tipi', 'Alıcı', 'Alıcı Adresi', 'Alıcı Kişi', 'Amaç', 'Akreditif No', 'Amir Banka', 'İhbar Bankası', 'İbraz Son Tarihi', 'Akreditif Vadesi', 'İbraz Riski', 'Kalan Gün', 'Banka Sonucu', 'Rezerv Notu', 'Ödeme Tarihi', 'Belge Adedi', 'Belge Seti', 'Kargo Firması', 'Gönderim Türü', 'Gönderim (Takip) No', 'Zarf', 'Ağırlık (kg)', 'Masraf (TL)', 'Kargo Faturası (Döviz)', 'Masraf PB', 'Masraf Kime', 'Tahmini Teslim', 'Teslim Tarihi', 'Teslim Süresi (Gün)', 'Teslim Alan', 'Durum', 'Son Hareket', 'Gönderen', 'Onaylayan', 'Not'],
      L.map(t => { const c = cari(t.cariKodu); return [t.tarihTr, t.no, t.sirket, t.dosyaNo, t.faturaNo, t.faturaTarihiTr, sayi(t.faturaTutari), t.paraBirimi, sayi(t.kur), sayi(t.faturaTutariTl), t.odemeSekli, t.teslimSekli, t.yuklemeTr, t.cariKodu, t.cari, c.resmiUnvan, t.ulke, t.sehir, t.aliciTipi, t.alici, t.aliciAdres, t.aliciKisi, t.amac, t.akreditifNo, t.amirBanka, t.ihbarBankasi, t.ibrazSonTr, t.akreditifVadesiTr, t.ibrazRiski, t.ibrazKalanGun ?? '', t.bankaDurumu, t.rezervNotu, t.odemeTr, t.belgeAdedi, t.belgeOzeti, t.kargoFirmasi, t.gonderimTuru, t.gonderimNo, t.zarfAdedi, sayi(t.agirlik), sayi(t.masraf), t.masrafPB !== 'TL' ? sayi(t.masrafDoviz) : '', t.masrafPB, t.masrafKime, t.tahminiTeslimTr, t.teslimTr, t.teslimSuresi ?? '', t.teslimAlan, t.durum, t.sonHareket, t.gonderen, t.onaylayan, t.not]; }));
  }
  function belgeExcel() {
    const rows = []; L.forEach(t => t.belgeler.forEach(b => rows.push([t.tarihTr, t.no, t.sirket, t.dosyaNo, t.faturaNo, t.cariKodu, t.cari, t.amac, t.alici, b.sira, b.tur, b.no, b.adet, b.nitelik, b.not, t.kargoFirmasi, t.gonderimNo, t.durum])));
    csvIndir('ihracat_belge_seti', ['Tarih', 'Gönderim No', 'İhracatçı Şirket', 'İhracat Dosyası', 'Fatura No', 'Cari Kodu', 'Müşteri', 'Amaç', 'Alıcı', '#', 'Belge Türü', 'Belge No', 'Adet', 'Nitelik', 'Not', 'Kargo Firması', 'Gönderim No', 'Durum'], rows);
  }
  function ozetExcel() {
    const yil = $('#ibOYil').value; tabloCsv('ibAylik', 'aylik_ihracat_belge_ozeti', yil);
    setTimeout(() => csvIndir('aylik_ihracat_belge_ozeti_gonderen_' + yil, ['Gönderen', 'Gönderim', 'Akreditif İbrazı', 'Belge', 'Masraf (TL)', 'Süresinde İbraz', 'Geç İbraz', 'Rezerv', 'Ort. Teslim (Gün)'], $$('#ibGonderen tbody tr, #ibGonderen tfoot tr').map(tr => $$('td', tr).map(td => td.textContent.trim().replace('—', '')))), 300);
    setTimeout(() => csvIndir('aylik_ihracat_belge_ozeti_kargo_' + yil, ['Kargo Firması', 'Gönderim Türü', 'Gönderim', 'Masraf (TL)', 'Ort. Masraf', 'Ort. Teslim (Gün)', 'Teslim Edildi', 'Sorunlu'], $$('#ibKargo tbody tr, #ibKargo tfoot tr').map(tr => $$('td', tr).map(td => td.textContent.trim().replace('—', '')))), 600);
  }

  // ---- başlat
  fetch('/IhracatBelge/Veri').then(r => r.json()).then(v => {
    V = v; V.cariler.forEach(c => CARI[c.kod] = c);
    const doldur = (id, l) => { $(id).innerHTML = '<option value="">Tümü</option>' + l.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); };
    doldur('#ibOdeme', V.odemeSekilleri); doldur('#ibOOdeme', V.odemeSekilleri); doldur('#ibAmacF', V.amaclar); doldur('#ibAliciTipi', V.aliciTipleri); doldur('#ibKargoF', V.kargoFirmalari); doldur('#ibDurum', V.durumlar); doldur('#ibBanka', V.bankaDurumlari); doldur('#ibRiskF', V.ibrazRiskleri); doldur('#ibBelgeTur', V.belgeTurleri); doldur('#ibGonderenF', V.gonderenler); doldur('#ibOGonderen', V.gonderenler);
    doldur('#ibUlke', V.ulkeler); doldur('#ibMUlke', V.ulkeler); doldur('#ibSinif', V.siniflar); doldur('#ibMSinif', V.siniflar); doldur('#ibIliski', V.iliskiDurumlari); doldur('#ibMIliski', V.iliskiDurumlari); doldur('#ibMRol', V.roller); doldur('#ibMErp', V.erpSirketleri);
    doldur('#ibMVade', [...new Set(V.cariler.map(c => c.vadeSarti).filter(Boolean))]); doldur('#ibMTemsilci', [...new Set(V.cariler.flatMap(c => (c.faaliyetler || []).map(f => f.satisTemsilcisi)).filter(Boolean))].sort());
    yilSecenekleri($('#ibOYil')); yilSecenekleri($('#ibMYil'));
    const LISTE_F = ['#ibSirket', '#ibOdeme', '#ibAmacF', '#ibAliciTipi', '#ibKargoF', '#ibDurum', '#ibBanka', '#ibRiskF', '#ibBelgeTur', '#ibGonderenF', '#ibUlke', '#ibSinif', '#ibIliski'];
    ['#ibAra', '#ibBas', '#ibBit', ...LISTE_F].forEach(s => { $(s).addEventListener('input', ciz); $(s).addEventListener('change', ciz); });
    $('#ibDonem').addEventListener('change', donemDegisti);
    $('#ibTemizle').addEventListener('click', () => { $('#ibAra').value = ''; LISTE_F.forEach(s => $(s).value = ''); $('#ibDonem').value = 'yil'; donemDegisti(); });
    $('#ibExcel').addEventListener('click', excel); $('#ibBelgeExcel').addEventListener('click', belgeExcel);
    ['#ibOYil', '#ibOSirket', '#ibOOdeme', '#ibOGonderen'].forEach(s => $(s).addEventListener('change', cizOzet)); $('#ibOExcel').addEventListener('click', ozetExcel);
    ['#ibMAra', '#ibMYil', '#ibMSirket', '#ibMUlke', '#ibMSinif', '#ibMIliski', '#ibMRol', '#ibMDurum', '#ibMVade', '#ibMTemsilci', '#ibMErp', '#ibMSira'].forEach(s => { $(s).addEventListener('input', cizMusteri); $(s).addEventListener('change', cizMusteri); }); $('#ibMExcel').addEventListener('click', musteriExcel);
    $$('#ibSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
    $('#ibDrawerKapat').addEventListener('click', drawerKapat); $('#ibPerde').addEventListener('click', drawerKapat);
    donemDegisti(); cizMusteri(); sekme(kok.dataset.baslangic || 'ozet'); window.NA_HAZIR = true;
    try { const p = new URLSearchParams(location.search); const no = p.get('no'), kod = p.get('kod'); if (no) { const t = V.gonderimler.find(x => x.no.toLowerCase() === no.toLowerCase()); if (t) drawerAc(t.id); else mesaj('warning', esc(no) + ' bulunamadı.'); } else if (kod) cariDrawer(kod); } catch (e) { /* yoksay */ }
  }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
