// ============================================================
// MUHATAPLAR İÇİN ÖZEL FİYATLAR (Views/Rapor95/Index.cshtml; veri /Rapor95/Veri, toplu /Rapor95/Toplu, kayıt /Rapor95/Kaydet, kopya /Rapor95/Kopyala, ürün /Rapor95/Urun, günlük /Rapor95/Gunluk)
//   Müşteri Fiyat Listesi: tüm ürünler; Yeni Fiyat sütunu düzenlenir (D: kaydedilmemiş değişiklikler, null = fiyatı kaldır).
//   Toplu: taban (Min / Liste / Son Satış) × (1 + %) → önizleme tabloya yazılır; grup bazında % ezer. Kaydet: geçerlilik tarihleriyle sunucuya; min altı yalnız Yönetici onayıyla.
//   Ürün Bazında: bir ürünün tüm müşterilerde fiyatı. Günlük: değişiklik kayıtları.
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('of'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const para = v => (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  const kisa = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 });
  const pct = v => (Number(v) || 0).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) + ' %';
  const tarih = s => { if (!s) return '—'; const p = String(s).slice(0, 10).split('-'); return p.length === 3 ? `${p[2]}.${p[1]}.${p[0]}` : s; };
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + new Date().toISOString().slice(0, 10) + '.csv'; a.click(); };
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; clearTimeout(mesaj.z); mesaj.z = setTimeout(() => s.style.display = 'none', 12000); }
  const post = (url, body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }).then(r => r.json());
  const getJ = url => fetch(url).then(r => r.json());
  const rozet = (m, t) => `<span class="of-rozet ${t}">${esc(m)}</span>`;
  const yonetici = kok.dataset.yonetici === '1';

  let V = null, F = { satici: kok.dataset.satici || '', kod: kok.dataset.kod || '', durum: '', grup: '', ara: '', musteriAra: '', musteriF: '' }, D = {}, S = new Set(), ilk = true, U = null;   // D: kod → { fiyat, kaynak, oran } · S: işaretli ürün kodları · U: ürün bazında veri
  const satir = kod => (V.satirlar || []).find(r => r.kod === kod);
  const kur = () => (V && V.kur) || { FiyatUsdTl: 1 };
  const tl = usd => (Number(usd) || 0) * (kur().fiyatUsdTl || kur().FiyatUsdTl || 1);
  const yeniFiyat = r => Object.prototype.hasOwnProperty.call(D, r.kod) ? D[r.kod] : null;   // null → değişiklik yok; {fiyat:null} → kaldır
  const etkinFiyat = r => { const d = yeniFiyat(r); return d ? d.fiyat : r.ozel ? r.ozel.fiyat : null; };
  const minAltiMi = r => { const f = etkinFiyat(r); return f != null && f < r.minFiyat - 0.0001; };
  const q = () => (F.ara || '').toLocaleLowerCase('tr-TR');
  const uyar = r => (!F.grup || r.grupAnahtari === F.grup) && (!q() || [r.kod, r.ad, r.seri, r.grup].join(' ').toLocaleLowerCase('tr-TR').includes(q())) && durumUyar(r);
  const durumUyar = r => { const d = F.durum; if (!d) return true; const o = r.ozel; if (d === 'fiyatsiz') return !o; if (d === 'aldigi-fiyatsiz') return r.aldi && !o; if (d === 'aldigi') return r.aldi; if (d === 'fiyatli') return !!o; if (d === 'degisen') return yeniFiyat(r) != null; if (d === 'minalti') return minAltiMi(r); if (d === 'suresi') return !!(o && o.bitis && (new Date(o.bitis) <= new Date(Date.now() + 30 * 86400000))); return true; };
  const liste = () => (V.satirlar || []).filter(uyar);

  // ---------- sekmeler
  function sekme(p) { $$('#ofSekmeler button').forEach(b => b.classList.toggle('aktif', b.dataset.p === p)); $$('#of > .na-panel').forEach(x => x.classList.toggle('aktif', x.dataset.p === p)); if (p === 'urun' && !U) urunYukle(); if (p === 'gunluk') gunlukCiz(); }

  // ---------- üst bilgi, kpi
  function cizCari() {
    const c = V.cari; $('#ofSaticiPill').innerHTML = `<i class="fas fa-store me-1"></i>${esc(V.satici.ad)} · ${esc(V.satici.bolge || '')}`; $('#ofKurBilgi').textContent = `USD/TL ${(kur().fiyatUsdTl || 0).toLocaleString('tr-TR', { minimumFractionDigits: 4 })} · ${kur().tarih || ''}`; $('#ofMinMarj').textContent = String(V.minMarj);
    if (!c) { $('#ofCari').innerHTML = '<div class="na-bos">Müşteri seçin.</div>'; return; }
    $('#ofCariLink').href = c.link;
    $('#ofCari').innerHTML = [['Müşteri', `${c.kod} · ${c.kisaAd}`], ['Resmi Ünvan', c.resmiUnvan], ['Kart Şirketi', c.sirket], ['Sınıf · Bölge', `${c.sinif || '—'} · ${c.bolge || '—'} · ${c.sehir || ''}`], ['Satış Temsilcisi', c.temsilci || '—'], ['Vade Şartı', c.vade || '—'], ['İlişki · Durum', `${c.iliski || '—'} · ${c.durum || ''}`], ['Yetkili', `${c.yetkili || '—'} ${c.telefon ? '· ' + c.telefon : ''}`], ['Faaliyet Grupları', (c.faaliyetler || []).join(' · ') || '—']].map(([l, v]) => `<div><div class="l">${esc(l)}</div><div class="v">${esc(v)}</div></div>`).join('');
    $('#ofSecNot').textContent = `${V.musteriler.length} müşteri · ${V.satirlar.length} ürün · ${V.ozet.fiyatli} özel fiyat tanımlı`;
  }
  function cizKpi() {
    const o = V.ozet || {}, degisen = Object.keys(D).length;
    $('#ofKpi').innerHTML = [
      { k: '', l: 'Toplam Ürün', v: String(o.toplamUrun || 0), s: `${(V.gruplar || []).length} ürün grubu`, r: '#0e7490' },
      { k: 'fiyatli', l: 'Özel Fiyatı Olan', v: String(o.fiyatli || 0), s: `${o.gecerli || 0} geçerli · ort. min üstü ${pct(o.ortMinUstu)}`, r: '#15803d' },
      { k: 'fiyatsiz', l: 'Fiyat Verilmemiş', v: String(o.fiyatsiz || 0), s: 'Min Fiyat + % ile toplu oluştur', r: o.fiyatsiz ? '#b45309' : '#15803d' },
      { k: 'aldigi-fiyatsiz', l: 'Aldığı · Fiyatsız', v: String(o.aldigiFiyatsiz || 0), s: `${o.aldigi || 0} ürün alıyor · ${kisa(o.son12AyMiktar)} kg / 12 ay`, r: o.aldigiFiyatsiz ? '#dc2626' : '#15803d' },
      { k: 'minalti', l: 'Min Altı', v: String(o.minAlti || 0), s: 'Min Fiyat Listesi altında', r: o.minAlti ? '#dc2626' : '#15803d' },
      { k: 'suresi', l: 'Süresi Dolan / Dolacak', v: `${o.suresiDolan || 0} / ${o.yakindaDolan || 0}`, s: 'dolmuş / 30 gün içinde', r: (o.suresiDolan || o.yakindaDolan) ? '#7c3aed' : '#15803d' },
      { k: 'degisen', l: 'Kaydedilmemiş', v: String(degisen), s: degisen ? 'Kaydet ile tanımlanır' : 'değişiklik yok', r: degisen ? '#0e7490' : '#94a3b8' }
    ].map(k => `<div style="--k:${k.r}" data-durum="${k.k}" class="${F.durum === k.k && k.k ? 'aktif' : ''}" title="Tıklayınca tablo bu duruma süzülür"><div class="l">${esc(k.l)}</div><div class="v">${esc(k.v)}</div><div class="s">${esc(k.s)}</div></div>`).join('');
  }

  // ---------- tablo
  const K = (ad, w, h) => ({ ad, w, h: h || (/\$|%|₺|kg|Miktar/.test(ad) ? 'r' : /^(|Birim|Durum|Kaynak)$/.test(ad) ? 'c' : 'l') });
  const KOL = [K('', 24, 'c'), K('Kod', 82), K('Ürün', 136), K('Grup · Seri', 104), K('Birim', 34), K('Min Fiyat ($)', 64), K('Liste Fiyatı ($)', 64), K('Son Satış ($)', 84), K('Son 12 Ay (kg)', 60), K('Mevcut Fiyat ($)', 78), K('Yeni Fiyat ($)', 96, 'c'), K('Min Üstü %', 58), K('₺ Karşılığı', 70), K('Kaynak', 66), K('Geçerlilik', 104), K('Durum', 84), K('', 56, 'c')];
  function tabloKur(t, kol) {
    const topW = kol.reduce((s, k) => s + k.w, 0); let cg = t.querySelector('colgroup'); if (!cg) { cg = document.createElement('colgroup'); t.insertBefore(cg, t.tHead); }
    cg.innerHTML = kol.map(k => `<col style="width:${(k.w / topW * 100).toFixed(2)}%">`).join('');
    t.tHead.innerHTML = '<tr>' + kol.map(k => `<th class="h-${k.h}" title="${esc(k.ad)}">${esc(k.ad)}</th>`).join('') + '</tr>';
    let st = document.getElementById('ofHiza-' + t.id); if (!st) { st = document.createElement('style'); st.id = 'ofHiza-' + t.id; document.head.appendChild(st); }
    st.textContent = kol.map((k, i) => `#${t.id} > thead > tr > th:nth-child(${i + 1}), #${t.id} > tbody > tr > td:nth-child(${i + 1}) { text-align: ${k.h === 'r' ? 'right' : k.h === 'c' ? 'center' : 'left'} !important; }`).join('\n');
  }
  const durumRozet = (r, d) => { if (d && d.fiyat == null) return rozet('Kaldırılacak', 'hata'); if (d) return minAltiMi(r) ? rozet('Min Altı · Kaydedilmedi', 'hata') : rozet('Yeni · Kaydedilmedi', 'bilgi'); switch (r.durum) { case 'Tanımlı': return rozet('Tanımlı', 'ok'); case 'Min Altı': return rozet('Min Altı', 'hata'); case 'Süresi Doldu': return rozet('Süresi Doldu', 'mor'); case 'Henüz Başlamadı': return rozet('Henüz Başlamadı', 'mor'); case 'Aldığı · Fiyatsız': return rozet('Aldığı · Fiyatsız', 'hata'); default: return rozet('Fiyatsız', 'uyar'); } };
  function cizTablo() {
    const L = liste(), t = $('#ofTablo'); tabloKur(t, KOL);
    const gruplar = []; L.forEach(r => { let g = gruplar.find(x => x.k === r.grupAnahtari); if (!g) { g = { k: r.grupAnahtari, ad: r.grup, l: [] }; gruplar.push(g); } g.l.push(r); });
    t.tBodies[0].innerHTML = L.length ? gruplar.map(g => g.l.map(r => {
      const d = yeniFiyat(r), f = etkinFiyat(r), o = r.ozel; const minAlti = minAltiMi(r);
      const cls = d ? (d.fiyat == null ? 'degisti' : minAlti ? 'minalti' : 'degisti') : !o ? (r.aldi ? 'aldi-fiyatsiz' : 'fiyatsiz') : r.durum === 'Min Altı' ? 'minalti' : /Süresi|Başlamadı/.test(r.durum) ? 'suresi' : '';
      const minUstu = f != null && r.minFiyat > 0 ? (f / r.minFiyat - 1) * 100 : null;
      const kaynak = d ? (d.fiyat == null ? '' : d.kaynak || 'Elle') : o ? o.kaynak : '';
      const gecerlilik = d && d.fiyat != null ? `${tarih($('#ofBas').value) } – ${$('#ofBit').value ? tarih($('#ofBit').value) : 'süresiz'}` : o ? `${tarih(o.baslangic)} – ${o.bitis ? tarih(o.bitis) : 'süresiz'}` : '';
      return `<tr class="${cls}" data-kod="${esc(r.kod)}"><td><input type="checkbox" class="of-sec-kutu" ${S.has(r.kod) ? 'checked' : ''}></td><td class="sol"><b>${esc(r.kod)}</b></td><td class="sol" title="${esc(r.ad)}">${esc(r.ad)}</td><td class="sol" title="${esc(r.grup + ' · ' + (r.seri || ''))}">${esc(r.grup)}<small>${esc(r.seri || '')}</small></td><td>${esc(r.birim)}</td><td class="para">${para(r.minFiyat)}<small>${para(r.minTl)} ₺</small></td><td class="para">${para(r.listeFiyati)}</td><td class="para">${r.sonSatis ? `${para(r.sonSatis.fiyat)}<small>${tarih(r.sonSatis.tarih)} · ${kisa(r.sonSatis.miktar)} kg</small>` : '<small>hiç almadı</small>'}</td><td class="para">${r.son12AyMiktar ? `${kisa(r.son12AyMiktar)}<small>${r.son12AySatis} satış</small>` : '—'}</td><td class="para">${o ? `<b>${para(o.fiyat)}</b><small>${o.kullanici || ''} · ${tarih(o.zaman)}</small>` : '<small>yok</small>'}</td><td><input type="number" step="0.01" min="0" class="form-control of-fiyat ${d ? (d.fiyat == null ? 'degisti' : minAlti ? 'minalti' : 'degisti') : ''}" value="${f != null ? Number(f).toFixed(2) : ''}" placeholder="${para(r.minFiyat)}" title="boş bırakılırsa mevcut fiyat kaldırılır · min ${para(r.minFiyat)} $"></td><td class="para ${minUstu != null && minUstu < 0 ? 'text-danger fw-bold' : ''}">${minUstu != null ? pct(minUstu) : '—'}</td><td class="para">${f != null ? para(tl(f)) : '—'}</td><td>${kaynak ? `<span class="of-kaynak">${esc(kaynak)}${d && d.oran != null ? ' ' + Number(d.oran).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) : o && o.oran != null && !d ? ' ' + Number(o.oran).toLocaleString('tr-TR', { maximumFractionDigits: 1 }) : ''}</span>` : ''}</td><td class="sol"><small style="display:inline">${esc(gecerlilik)}</small></td><td>${durumRozet(r, d)}</td><td style="white-space:nowrap"><button type="button" class="btn btn-outline-primary btn-xs of-min" title="Min Fiyat + % (yukarıdaki oran) yaz"><i class="fas fa-plus"></i>%</button> <button type="button" class="btn btn-light border btn-xs of-geri" title="Bu satırdaki değişikliği geri al / fiyatı kaldır"><i class="fas ${d ? 'fa-rotate-left' : 'fa-eraser'}"></i></button></td></tr>`;
    }).join('') + `<tr class="ara"><td colspan="5" class="text-end">${esc(g.ad)} · ${g.l.length} ürün · fiyatlı ${g.l.filter(r => etkinFiyat(r) != null).length} · fiyatsız ${g.l.filter(r => etkinFiyat(r) == null).length}</td><td class="para">${para(g.l.reduce((s, r) => s + r.minFiyat, 0) / g.l.length)}</td><td class="para">${para(g.l.reduce((s, r) => s + r.listeFiyati, 0) / g.l.length)}</td><td colspan="2" class="sol"><small style="display:inline">aldığı ${g.l.filter(r => r.aldi).length} · ${kisa(g.l.reduce((s, r) => s + r.son12AyMiktar, 0))} kg</small></td><td class="para">${(() => { const f = g.l.filter(r => etkinFiyat(r) != null); return f.length ? para(f.reduce((s, r) => s + etkinFiyat(r), 0) / f.length) : '—'; })()}</td><td class="sol"><small style="display:inline">ort. fiyat</small></td><td class="para">${(() => { const f = g.l.filter(r => etkinFiyat(r) != null && r.minFiyat > 0); return f.length ? pct(f.reduce((s, r) => s + (etkinFiyat(r) / r.minFiyat - 1) * 100, 0) / f.length) : '—'; })()}</td><td colspan="5"></td></tr>`).join('') : `<tr><td colspan="${KOL.length}" class="na-bos text-center">Süzgece uyan ürün yok.</td></tr>`;
    const fiyatli = L.filter(r => etkinFiyat(r) != null);
    t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="5" class="text-end">TOPLAM · ${L.length} ürün · fiyatlı ${fiyatli.length} · fiyatsız ${L.length - fiyatli.length} · değişen ${Object.keys(D).length}</td><td class="para">${para(L.reduce((s, r) => s + r.minFiyat, 0) / L.length)}</td><td class="para">${para(L.reduce((s, r) => s + r.listeFiyati, 0) / L.length)}</td><td colspan="2" class="sol">aldığı ${L.filter(r => r.aldi).length} · ${kisa(L.reduce((s, r) => s + r.son12AyMiktar, 0))} kg</td><td class="para">${fiyatli.length ? para(fiyatli.reduce((s, r) => s + etkinFiyat(r), 0) / fiyatli.length) : '—'}</td><td class="sol">ort. fiyat</td><td class="para">${fiyatli.length ? pct(fiyatli.filter(r => r.minFiyat > 0).reduce((s, r) => s + (etkinFiyat(r) / r.minFiyat - 1) * 100, 0) / fiyatli.length) : '—'}</td><td class="para">${fiyatli.length ? para(fiyatli.reduce((s, r) => s + tl(etkinFiyat(r)), 0) / fiyatli.length) : '—'}</td><td colspan="4" class="sol">min altı ${L.filter(minAltiMi).length}</td></tr>` : '';
    $('#ofSayi').textContent = `${L.length} / ${V.satirlar.length} ürün · sarı fiyatsız · kırmızı aldığı ama fiyatsız / min altı · mavi kaydedilmemiş yeni fiyat · mor süresi dolan`;
    $('#ofN-musteri').textContent = V.satirlar.length; kpiDegisiklik();
  }
  function kpiDegisiklik() {
    const n = Object.keys(D).length, kaldir = Object.values(D).filter(d => d.fiyat == null).length, minAlti = (V.satirlar || []).filter(r => yeniFiyat(r) && yeniFiyat(r).fiyat != null && minAltiMi(r)).length;
    $('#ofDegisiklik').textContent = n ? `${n} satır kaydedilmeyi bekliyor (${n - kaldir} fiyat · ${kaldir} kaldırma)` : 'Kaydedilmemiş değişiklik yok.';
    $('#ofMinUyari').textContent = minAlti ? `${minAlti} satır Min Fiyat Listesi'nin altında${yonetici ? ' — onay kutusunu işaretleyin' : ' — kaydedilemez'}` : '';
    $('#ofKaydet').disabled = n === 0; cizKpi();
  }
  function degistir(kod, fiyat, kaynak, oran) {
    const r = satir(kod); if (!r) return;
    if (fiyat == null) { if (r.ozel) D[kod] = { fiyat: null }; else delete D[kod]; }
    else { const f = Math.round(fiyat * 100) / 100; if (r.ozel && Math.abs(r.ozel.fiyat - f) < 0.005 && !kaynak) delete D[kod]; else D[kod] = { fiyat: f, kaynak: kaynak || 'Elle', oran: oran ?? null }; }
  }
  function satirGuncelle(tr) { const kod = tr.dataset.kod; const r = satir(kod); if (!r) return; const yeni = document.createElement('tbody'); const eskiScroll = $('#ofTablo').closest('.table-responsive').scrollTop; cizTablo(); $('#ofTablo').closest('.table-responsive').scrollTop = eskiScroll; }
  const oranDegeri = () => { const v = parseFloat(String($('#ofOran').value).replace(',', '.')); return isNaN(v) ? 0 : v; };
  const yuvarla = (v, adim) => Math.round(Math.round(v / adim) * adim * 100) / 100;

  // ---------- toplu
  function cizGrupOranlar() {
    $('#ofGrupOranlar').innerHTML = (V.gruplar || []).map(g => { const o = (V.ozet.gruplar || []).find(x => x.anahtar === g.anahtar) || {}; return `<div title="Bu grup için ayrı % (boş = genel oran)"><b>${esc(g.ad)}</b><small>${o.fiyatli || 0}/${o.urun || 0} fiyatlı${o.ortMinUstu != null ? ' · ort. ' + pct(o.ortMinUstu) : ''}</small><span>%</span><input type="number" step="0.5" class="form-control of-grup-oran-k" data-g="${esc(g.anahtar)}" placeholder="genel"></div>`; }).join('');
    $('#ofTopluGrup').innerHTML = (V.gruplar || []).map(g => `<option value="${esc(g.anahtar)}">${esc(g.ad)}</option>`).join('');
    formul();
  }
  function formul() { const t = $('#ofTaban').value, o = oranDegeri(), k = $('#ofKapsam').value; const tabanAd = t === 'liste' ? 'Liste Fiyatı' : t === 'sonSatis' ? 'Son Satış Fiyatı (yoksa Min)' : 'Min Fiyat Listesi'; const kapsamAd = $('#ofKapsam').selectedOptions[0].textContent; $('#ofFormul').innerHTML = `<b>Yeni Fiyat = ${esc(tabanAd)} × (1 + ${o.toLocaleString('tr-TR')} / 100)</b>, yuvarlama ${$('#ofYuvarla').selectedOptions[0].textContent} · kapsam: <b>${esc(kapsamAd)}</b> · grup kutusuna % yazılırsa o grupta genel oran yerine o kullanılır · sonuç min fiyatın altına inmez.`; }
  async function topluUygula() {
    const kapsam = $('#ofKapsam').value; const grupOranlari = {}; $$('.of-grup-oran-k').forEach(i => { const v = parseFloat(String(i.value).replace(',', '.')); if (!isNaN(v)) grupOranlari[i.dataset.g] = v; });
    const istek = { Satici: V.satici.ad, CariKodu: V.cari.kod, Oran: oranDegeri(), Kapsam: kapsam === 'secili' ? 'tumu' : kapsam, Grup: $('#ofTopluGrup').value, GrupOranlari: grupOranlari, Yuvarlama: parseFloat($('#ofYuvarla').value), Taban: $('#ofTaban').value, Urunler: kapsam === 'secili' ? [...S] : null };
    if (kapsam === 'secili' && S.size === 0) { mesaj('warning', 'Tabloda işaretli kalem yok.'); return; }
    if (kapsam === 'tumu' && !confirm('Tüm kalemlerin fiyatı yeniden hesaplanacak (mevcut özel fiyatların üzerine yazılır; Kaydet\'e basana kadar kalıcı değildir). Devam?')) return;
    const r = await post('/Rapor95/Toplu', istek).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!r.success) { mesaj('danger', esc(r.message)); return; }
    r.satirlar.forEach(x => degistir(x.kod, x.fiyat, x.kaynak, x.oran)); cizTablo();
    mesaj('info', `<i class="fas fa-calculator me-1"></i>${esc(r.message)} Tabloda mavi satırlar; gözden geçirip <b>Kaydet</b>.`);
  }

  // ---------- kaydet / vazgeç
  async function kaydet() {
    const satirlar = Object.entries(D).map(([kod, d]) => ({ UrunKod: kod, Fiyat: d.fiyat, Kaynak: d.kaynak, Oran: d.oran })); if (!satirlar.length) return;
    const r = await post('/Rapor95/Kaydet', { Satici: V.satici.ad, CariKodu: V.cari.kod, Satirlar: satirlar, Baslangic: $('#ofBas').value, Bitis: $('#ofBit').value, MinAltiOnay: !!($('#ofMinOnay') && $('#ofMinOnay').checked) }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    if (!r.success) { mesaj('danger', '<i class="fas fa-triangle-exclamation me-1"></i>' + esc(r.message)); return; }
    D = {}; S.clear(); mesaj('success', '<i class="fas fa-check me-1"></i>' + esc(r.message) + ` <a href="/Rapor95/Pdf?satici=${encodeURIComponent(V.satici.ad)}&kod=${encodeURIComponent(V.cari.kod)}" target="_blank" class="ms-1"><i class="fas fa-file-pdf"></i> Müşteri fiyat listesi PDF</a>`); await yukle(true);
  }
  function vazgec() { if (Object.keys(D).length && !confirm('Kaydedilmemiş değişiklikler silinsin mi?')) return; D = {}; cizTablo(); }

  // ---------- ürün bazında
  const UKOL = [K('Müşteri', 150), K('Kart Şirketi', 70), K('Sınıf · Bölge', 90), K('Temsilci', 80), K('Özel Fiyat ($)', 70), K('Min Üstü %', 58), K('₺ Karşılığı', 70), K('Son Satış ($)', 84), K('Son 12 Ay (kg)', 60), K('Kaynak', 64), K('Geçerlilik', 70), K('Oran Fiyatı ($)', 84), K('Durum', 64)];
  async function urunYukle(kodVer) {
    const kod = kodVer || $('#ofUrunSec').value || kok.dataset.urun || ((V.urunler || [])[0] || {}).kod; if (!kod) return;
    const r = await getJ(`/Rapor95/Urun?satici=${encodeURIComponent(V.satici.ad)}&kod=${encodeURIComponent(kod)}`).catch(() => ({ success: false })); if (!r.success) { mesaj('danger', 'Ürün verisi yüklenemedi.'); return; }
    U = r; $('#ofUrunSec').value = r.urun.kod; cizUrun();
  }
  function cizUrun() {
    if (!U) return; const u = U.urun, f = $('#ofUrunF').value; const t = $('#ofUrunTablo'); tabloKur(t, UKOL);
    $('#ofUrunBilgi').innerHTML = `<b>${esc(u.kod)}</b> ${esc(u.ad)} · ${esc(u.grup)} · min <b>${para(u.minFiyat)} $</b> · liste ${para(u.listeFiyati)} $ · ${para(u.minTl)} ₺`;
    const L = U.musteriler.filter(m => !f || (f === 'fiyatli' ? m.fiyat != null : f === 'fiyatsiz' ? m.fiyat == null : m.son12AyMiktar > 0));
    t.tBodies[0].innerHTML = L.length ? L.map(m => `<tr class="tik ${m.fiyat == null ? (m.son12AyMiktar > 0 ? 'aldi-fiyatsiz' : 'fiyatsiz') : m.minUstu < 0 ? 'minalti' : m.gecerli === false ? 'suresi' : ''}" data-kod="${esc(m.kod)}" title="Tıklayınca bu müşterinin fiyat listesi açılır"><td class="sol"><b>${esc(m.kod)}</b> ${esc(m.ad)}</td><td class="sol">${esc(m.sirket || '')}</td><td class="sol">${esc((m.sinif || '—') + ' · ' + (m.bolge || '—'))}</td><td class="sol">${esc(m.temsilci || '')}</td><td class="para">${m.fiyat != null ? `<b>${para(m.fiyat)}</b>` : '—'}</td><td class="para ${m.minUstu != null && m.minUstu < 0 ? 'text-danger fw-bold' : ''}">${m.minUstu != null ? pct(m.minUstu) : '—'}</td><td class="para">${m.tl != null ? para(m.tl) : '—'}</td><td class="para">${m.sonSatis ? `${para(m.sonSatis.fiyat)}<small>${tarih(m.sonSatis.tarih)} · ${kisa(m.sonSatis.miktar)} kg</small>` : '<small>almadı</small>'}</td><td class="para">${m.son12AyMiktar ? kisa(m.son12AyMiktar) : '—'}</td><td>${m.kaynak ? `<span class="of-kaynak">${esc(m.kaynak)}</span>` : ''}</td><td class="sol"><small style="display:inline">${m.fiyat != null ? (m.bitis ? '→ ' + tarih(m.bitis) : 'süresiz') : ''}</small></td><td class="para">${m.oranFiyat != null ? `${para(m.oranFiyat)}<small>${esc(m.oranKaynak || '')} oranı</small>` : '<small>tanım yok</small>'}</td><td>${m.fiyat == null ? rozet(m.son12AyMiktar > 0 ? 'Aldığı · Fiyatsız' : 'Fiyatsız', m.son12AyMiktar > 0 ? 'hata' : 'uyar') : m.minUstu < 0 ? rozet('Min Altı', 'hata') : m.gecerli === false ? rozet('Süresi Doldu', 'mor') : rozet('Tanımlı', 'ok')}</td></tr>`).join('') : `<tr><td colspan="${UKOL.length}" class="na-bos text-center">Kayıt yok.</td></tr>`;
    const fl = L.filter(m => m.fiyat != null);
    t.tFoot.innerHTML = L.length ? `<tr class="toplam"><td colspan="4" class="text-end">${L.length} müşteri · fiyatlı ${fl.length} · fiyatsız ${L.length - fl.length}</td><td class="para">${fl.length ? para(fl.reduce((s, m) => s + m.fiyat, 0) / fl.length) : '—'}</td><td class="para">${fl.length ? pct(fl.reduce((s, m) => s + (m.minUstu || 0), 0) / fl.length) : '—'}</td><td></td><td class="para">${(() => { const s = L.filter(m => m.sonSatis); return s.length ? para(s.reduce((a, m) => a + m.sonSatis.fiyat, 0) / s.length) : '—'; })()}</td><td class="para">${kisa(L.reduce((s, m) => s + m.son12AyMiktar, 0))}</td><td colspan="4" class="sol">min ${para(u.minFiyat)} $ · en düşük ${fl.length ? para(Math.min(...fl.map(m => m.fiyat))) : '—'} · en yüksek ${fl.length ? para(Math.max(...fl.map(m => m.fiyat))) : '—'}</td></tr>` : '';
  }

  // ---------- günlük
  const GKOL = [K('Zaman', 90), K('Kullanıcı', 80), K('Müşteri', 130), K('Ürün', 160), K('İşlem', 90), K('Eski', 100), K('Yeni', 120), K('Kaynak', 64)];
  async function gunlukCiz() {
    let g = V.gunluk || [];
    if ($('#ofGunlukKapsam').value === 'satici') { const r = await getJ(`/Rapor95/Gunluk?satici=${encodeURIComponent(V.satici.ad)}`).catch(() => null); if (r && r.success) g = r.gunluk; }
    const t = $('#ofGunlukTablo'); tabloKur(t, GKOL);
    t.tBodies[0].innerHTML = g.length ? g.map(x => `<tr><td class="sol">${esc(x.zaman)}</td><td class="sol">${esc(x.kullanici)}</td><td class="sol">${esc(x.cariKodu + ' ' + (x.cariAd || ''))}</td><td class="sol"><b>${esc(x.urunKod)}</b> ${esc(x.urunAd || '')}</td><td class="sol">${esc(x.islem)}</td><td class="para">${esc(x.eski || '—')}</td><td class="para">${esc(x.yeni || '—')}</td><td>${x.kaynak ? `<span class="of-kaynak">${esc(x.kaynak)}</span>` : ''}</td></tr>`).join('') : `<tr><td colspan="${GKOL.length}" class="na-bos text-center">Günlük kaydı yok.</td></tr>`;
    $('#ofN-gunluk').textContent = g.length;
  }

  // ---------- olaylar
  $$('#ofSekmeler button').forEach(b => b.addEventListener('click', () => sekme(b.dataset.p)));
  $('#ofSatici').addEventListener('change', () => { if (Object.keys(D).length && !confirm('Kaydedilmemiş değişiklikler var; satıcı değiştirilsin mi?')) { $('#ofSatici').value = V.satici.ad; return; } F.satici = $('#ofSatici').value; F.kod = ''; D = {}; S.clear(); U = null; yukle(); });
  $('#ofMusteri').addEventListener('change', () => { if (Object.keys(D).length && !confirm('Kaydedilmemiş değişiklikler var; müşteri değiştirilsin mi?')) { $('#ofMusteri').value = V.cari.kod; return; } F.kod = $('#ofMusteri').value; D = {}; S.clear(); yukle(); });
  $('#ofMusteriAra').addEventListener('input', () => { F.musteriAra = $('#ofMusteriAra').value; musteriSecenekleri(); }); $('#ofMusteriF').addEventListener('change', () => { F.musteriF = $('#ofMusteriF').value; musteriSecenekleri(); });
  ['#ofDurumF', '#ofGrupF'].forEach(s => $(s).addEventListener('change', () => { F.durum = $('#ofDurumF').value; F.grup = $('#ofGrupF').value; cizTablo(); }));
  $('#ofAra').addEventListener('input', () => { F.ara = $('#ofAra').value; cizTablo(); });
  $('#ofKpi').addEventListener('click', e => { const k = e.target.closest('[data-durum]'); if (!k) return; F.durum = F.durum === k.dataset.durum ? '' : k.dataset.durum; $('#ofDurumF').value = F.durum; cizTablo(); });
  ['#ofTaban', '#ofKapsam', '#ofYuvarla'].forEach(s => $(s).addEventListener('change', formul)); $('#ofOran').addEventListener('input', formul);
  $('#ofTopluUygula').addEventListener('click', topluUygula); $('#ofTopluTemizle').addEventListener('click', vazgec);
  $('#ofKaydet').addEventListener('click', kaydet); $('#ofVazgec').addEventListener('click', vazgec);
  $('#ofBas').addEventListener('change', cizTablo); $('#ofBit').addEventListener('change', cizTablo);
  $('#ofTablo').addEventListener('change', e => {
    const tr = e.target.closest('tr[data-kod]'); if (!tr) return; const kod = tr.dataset.kod;
    if (e.target.classList.contains('of-sec-kutu')) { if (e.target.checked) S.add(kod); else S.delete(kod); return; }
    if (e.target.classList.contains('of-fiyat')) { const v = parseFloat(String(e.target.value).replace(',', '.')); degistir(kod, isNaN(v) || e.target.value === '' ? null : v, 'Elle', null); satirGuncelle(tr); }
  });
  $('#ofTablo').addEventListener('click', e => {
    const b = e.target.closest('button'); if (!b) return; const tr = b.closest('tr[data-kod]'); if (!tr) return; const r = satir(tr.dataset.kod); if (!r) return;
    if (b.classList.contains('of-min')) { const o = oranDegeri(); degistir(r.kod, Math.max(r.minFiyat, yuvarla(r.minFiyat * (1 + o / 100), parseFloat($('#ofYuvarla').value))), 'Min + %', o); satirGuncelle(tr); }
    else if (b.classList.contains('of-geri')) { if (yeniFiyat(r)) delete D[r.kod]; else if (r.ozel) D[r.kod] = { fiyat: null }; satirGuncelle(tr); }
  });
  $('#ofSeciliHepsi').addEventListener('click', () => { const L = liste(); const hepsiSecili = L.every(r => S.has(r.kod)); L.forEach(r => hepsiSecili ? S.delete(r.kod) : S.add(r.kod)); cizTablo(); });
  $('#ofSeciliMin').addEventListener('click', () => { if (!S.size) { mesaj('warning', 'İşaretli kalem yok.'); return; } const o = oranDegeri(), y = parseFloat($('#ofYuvarla').value); S.forEach(kod => { const r = satir(kod); if (r) degistir(kod, Math.max(r.minFiyat, yuvarla(r.minFiyat * (1 + o / 100), y)), 'Min + %', o); }); cizTablo(); });
  $('#ofSeciliSil').addEventListener('click', () => { if (!S.size) { mesaj('warning', 'İşaretli kalem yok.'); return; } S.forEach(kod => { const r = satir(kod); if (r && r.ozel) D[kod] = { fiyat: null }; else delete D[kod]; }); cizTablo(); });
  $('#ofCsv').addEventListener('click', () => csvIndir('ozel_fiyat_' + (V.cari ? V.cari.kod : ''), ['Kod', 'Ürün', 'Grup', 'Seri', 'Birim', 'Min Fiyat ($)', 'Liste Fiyatı ($)', 'Son Satış ($)', 'Son Satış Tarihi', 'Son 12 Ay (kg)', 'Özel Fiyat ($)', 'Yeni Fiyat ($)', 'Min Üstü %', '₺ Karşılığı', 'Kaynak', 'Başlangıç', 'Bitiş', 'Durum'], liste().map(r => { const f = etkinFiyat(r); return [r.kod, r.ad, r.grup, r.seri, r.birim, r.minFiyat, r.listeFiyati, r.sonSatis ? r.sonSatis.fiyat : '', r.sonSatis ? r.sonSatis.tarih : '', r.son12AyMiktar, r.ozel ? r.ozel.fiyat : '', yeniFiyat(r) ? (yeniFiyat(r).fiyat ?? 'kaldır') : '', f != null && r.minFiyat ? Math.round((f / r.minFiyat - 1) * 1000) / 10 : '', f != null ? Math.round(tl(f) * 100) / 100 : '', r.ozel ? r.ozel.kaynak : '', r.ozel ? r.ozel.baslangic : '', r.ozel ? r.ozel.bitis || '' : '', r.durum]; })));
  $('#ofPdf').addEventListener('click', () => { if (!V.cari) return; window.open(`/Rapor95/Pdf?satici=${encodeURIComponent(V.satici.ad)}&kod=${encodeURIComponent(V.cari.kod)}`, '_blank'); });
  $('#ofExcelIndir').addEventListener('click', () => { if (!V.cari) return; window.open(`/Rapor95/Excel?satici=${encodeURIComponent(V.satici.ad)}&kod=${encodeURIComponent(V.cari.kod)}`, '_blank'); });
  $('#ofKopyaAc').addEventListener('click', () => { $('#ofKopyaKart').classList.toggle('acik'); $('#ofKopyaKaynak').innerHTML = (V.musteriler || []).filter(m => m.kod !== (V.cari || {}).kod && m.fiyatli > 0).map(m => `<option value="${esc(m.kod)}">${esc(m.kod)} · ${esc(m.ad)} (${m.fiyatli} fiyat)</option>`).join('') || '<option value="">Fiyatı olan başka müşteri yok</option>'; });
  $('#ofKopyaKapat').addEventListener('click', () => $('#ofKopyaKart').classList.remove('acik'));
  $('#ofKopyaUygula').addEventListener('click', async () => {
    const kaynak = $('#ofKopyaKaynak').value; if (!kaynak || !V.cari) return; const oran = parseFloat(String($('#ofKopyaOran').value).replace(',', '.'));
    if (!confirm(`${kaynak} müşterisinin fiyatları ${V.cari.kod} ${V.cari.kisaAd} için ${$('#ofKopyaKapsam').value === '1' ? 'yalnız fiyatsız kalemlere' : 'tüm kalemlere (üzerine yazılır)'} kopyalanıp kaydedilsin mi?`)) return;
    const r = await post('/Rapor95/Kopyala', { Satici: V.satici.ad, KaynakCari: kaynak, HedefCari: V.cari.kod, SadeceFiyatsiz: $('#ofKopyaKapsam').value === '1', Oran: isNaN(oran) ? null : oran }).catch(() => ({ success: false, message: 'Ağ hatası.' }));
    mesaj(r.success ? 'success' : 'danger', esc(r.message)); if (r.success) { $('#ofKopyaKart').classList.remove('acik'); D = {}; await yukle(true); }
  });
  $('#ofUrunSec').addEventListener('change', () => urunYukle($('#ofUrunSec').value)); $('#ofUrunF').addEventListener('change', cizUrun);
  $('#ofUrunAra').addEventListener('input', () => { const s = $('#ofUrunAra').value.toLocaleLowerCase('tr-TR'); $('#ofUrunSec').innerHTML = (V.urunler || []).filter(u => !s || [u.kod, u.ad, u.seri, u.grup].join(' ').toLocaleLowerCase('tr-TR').includes(s)).map(u => `<option value="${esc(u.kod)}">${esc(u.kod)} · ${esc(u.ad)} · ${esc(u.grup)}</option>`).join(''); if ($('#ofUrunSec').value) urunYukle($('#ofUrunSec').value); });
  $('#ofUrunTablo').addEventListener('click', e => { const tr = e.target.closest('tr.tik[data-kod]'); if (!tr) return; F.kod = tr.dataset.kod; $('#ofMusteri').value = F.kod; D = {}; S.clear(); sekme('musteri'); yukle(); });
  $('#ofUrunCsv').addEventListener('click', () => { if (!U) return; csvIndir('urun_fiyatlari_' + U.urun.kod, ['Müşteri Kodu', 'Müşteri', 'Kart Şirketi', 'Sınıf', 'Bölge', 'Temsilci', 'Özel Fiyat ($)', 'Min Üstü %', '₺', 'Son Satış ($)', 'Son Satış Tarihi', 'Son 12 Ay (kg)', 'Kaynak', 'Bitiş', 'Oran Fiyatı ($)'], U.musteriler.map(m => [m.kod, m.ad, m.sirket, m.sinif, m.bolge, m.temsilci, m.fiyat ?? '', m.minUstu ?? '', m.tl ?? '', m.sonSatis ? m.sonSatis.fiyat : '', m.sonSatis ? m.sonSatis.tarih : '', m.son12AyMiktar, m.kaynak || '', m.bitis || '', m.oranFiyat ?? ''])); });
  $('#ofGunlukKapsam').addEventListener('change', gunlukCiz);
  window.addEventListener('beforeunload', e => { if (Object.keys(D).length) { e.preventDefault(); e.returnValue = ''; } });

  function musteriSecenekleri() {
    const s = (F.musteriAra || '').toLocaleLowerCase('tr-TR'); const f = F.musteriF;
    const L = (V.musteriler || []).filter(m => (!s || [m.kod, m.ad, m.sirket, m.sinif, m.bolge, m.temsilci].join(' ').toLocaleLowerCase('tr-TR').includes(s)) && (!f || (f === 'fiyatli' ? m.fiyatli > 0 : m.fiyatli === 0)));
    $('#ofMusteri').innerHTML = L.map(m => `<option value="${esc(m.kod)}">${esc(m.kod)} · ${esc(m.ad)}${m.sirket ? ' · ' + esc(m.sirket) : ''} · ${m.fiyatli ? m.fiyatli + ' fiyat' : 'fiyat yok'}</option>`).join('') || '<option value="">Süzgece uyan müşteri yok</option>';
    if (V.cari && L.some(m => m.kod === V.cari.kod)) $('#ofMusteri').value = V.cari.kod;
  }

  // ---------- veri
  async function yukle(sessiz) {
    if (!sessiz) $('#ofSecNot').textContent = 'yükleniyor…';
    let r; try { r = await getJ(`/Rapor95/Veri?satici=${encodeURIComponent(F.satici)}&kod=${encodeURIComponent(F.kod)}`); } catch (e) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    if (!r.success) { mesaj('danger', 'Veri yüklenemedi.'); return; }
    V = r; F.satici = V.satici.ad; F.kod = V.cari ? V.cari.kod : ''; U = null;
    try { const u = new URL(location.href); u.searchParams.set('satici', F.satici); if (F.kod) u.searchParams.set('kod', F.kod); history.replaceState(null, '', u); } catch (e) { }
    if (ilk) {
      ilk = false;
      $('#ofSatici').innerHTML = V.saticilar.map(s => `<option value="${esc(s.ad)}">${esc(s.ad)} · ${esc(s.bolge)} (${s.musteri} müşteri)</option>`).join('');
      $('#ofGrupF').innerHTML = '<option value="">Tümü</option>' + V.gruplar.map(g => `<option value="${esc(g.anahtar)}">${esc(g.ad)}</option>`).join('');
      $('#ofUrunSec').innerHTML = (V.urunler || []).map(u => `<option value="${esc(u.kod)}">${esc(u.kod)} · ${esc(u.ad)} · ${esc(u.grup)}</option>`).join('');
      $('#ofBas').value = V.bugun;
      if (kok.dataset.urun) $('#ofUrunSec').value = kok.dataset.urun;
      sekme(kok.dataset.sekme || 'musteri');
    } else if ($('#ofSatici').value !== V.satici.ad) { $('#ofSatici').innerHTML = V.saticilar.map(s => `<option value="${esc(s.ad)}">${esc(s.ad)} · ${esc(s.bolge)} (${s.musteri} müşteri)</option>`).join(''); $('#ofUrunSec').innerHTML = (V.urunler || []).map(u => `<option value="${esc(u.kod)}">${esc(u.kod)} · ${esc(u.ad)} · ${esc(u.grup)}</option>`).join(''); }
    $('#ofSatici').value = V.satici.ad; musteriSecenekleri(); cizCari(); cizGrupOranlar(); cizTablo();
    if ($('#ofSekmeler button.aktif') && $('#ofSekmeler button.aktif').dataset.p === 'gunluk') gunlukCiz();
  }
  yukle();
})();
