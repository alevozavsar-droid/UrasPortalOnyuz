// ============================================================
// CARİ BAKİYE RAPORU (Views/Rapor65/Index.cshtml) — Müşteri ve tedarikçi carilerinin bakiye / vade / çek-senet / risk / limit görünümü
//   Veri: /CariKart/Veri (kartlar: CARİ_MASTER + limit + risk; Data/CariKartOrnek.cs, Data/MutabakatOrnek.cs)
//   Süzgeçler Müşteri / Tedarikçi Cari Kartları ile aynı kimlikler (ckAra, ckFTip … ckFRisk); satırdan karta (/CariKart/…?kod=) ve Cari Ekstre'ye (/Rapor20?bpCode=) geçiş
// ============================================================
(() => {
  'use strict';
  const kok = document.getElementById('cb'); if (!kok) return;
  const $ = (s, k) => (k || document).querySelector(s), $$ = (s, k) => Array.from((k || document).querySelectorAll(s));
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const tl = v => Number(v || 0).toLocaleString('tr-TR', { maximumFractionDigits: 0 }) + ' ₺';
  const bos = v => v === undefined || v === null || v === '' ? '<span class="text-muted">—</span>' : esc(v);
  const rozet = (m, t) => `<span class="rt-durum rt-${t}">${esc(m)}</span>`;
  const riskRozet = d => rozet(d || '—', d === 'Riskli' ? 'hata' : d === 'İzlemede' ? 'bekle' : d === 'Risksiz' ? 'ok' : 'notr');
  const durumRozet = d => rozet(d, d === 'Aktif' ? 'ok' : 'hata');
  const sinifRozet = s => `<span class="ck-tip ${s === 'Özel Takip' ? 'ozel' : ''}">${esc(s || '—')}</span>`;
  const bugunIso = new Date().toISOString().slice(0, 10);
  const csvIndir = (ad, basliklar, satirlar) => { const t = [basliklar, ...satirlar].map(r => r.map(x => `"${String(x ?? '').replace(/"/g, '""')}"`).join(';')).join('\r\n'); const b = new Blob(['﻿' + t], { type: 'text/csv;charset=utf-8' }); const a = document.createElement('a'); a.href = URL.createObjectURL(b); a.download = ad + '_' + bugunIso + '.csv'; a.click(); };
  const secDoldur = (el, l, ilk, deger) => { const v = deger !== undefined ? deger : el.value; el.innerHTML = (ilk !== null ? `<option value="">${esc(ilk)}</option>` : '') + l.map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); el.value = l.includes(v) ? v : ''; };
  let V = null;
  const TIPLER = ['Müşteri', 'Tedarikçi'];
  const ted = k => k.kartTipi === 'Tedarikçi';
  const siniflar = (tip, bolge) => V.sinifMatrisi[tip + '|' + bolge] || V.sinifMatrisi[tip + '|*'] || [];
  const gruplar = ana => V.faaliyetHiyerarsisi[ana] || [];
  function mesaj(tip, m) { const s = $('#naSonuc'); s.className = 'alert py-2 px-3 mb-2 alert-' + tip; s.innerHTML = m; s.style.display = 'block'; }

  function kullanimHucre(L) {
    if (!L) return '—'; if (L.tur === 'Yok') return `<span class="ck-kul yok" title="Limit tanımlı değil · toplam risk ${esc(L.toplamRiskTr)}">Limit Yok</span>`;
    const y = L.kullanimYuzde != null ? Number(L.kullanimYuzde) : (L.risk > 0 ? 999 : 0), g = Math.min(y, 100), r = L.durum === 'Limit Aşıldı' ? '#dc2626' : L.durum === 'Limite Yakın' ? '#f59e0b' : '#16a34a';
    const yazi = L.kullanimYuzde != null ? '%' + Math.round(y).toLocaleString('tr-TR') : '∞';
    return `<span class="ck-kul" style="--y:${g}%;--r:${r}" title="${esc(L.durum)} · kullanım ${yazi} · risk ${esc(L.toplamRiskTr)} / limit ${esc(L.limitTr)} · kalan ${esc(L.kalanTr)}"><i></i><b>${yazi}</b></span>`;
  }
  // ---- liste: süzgeçler kart ekranıyla aynı
  function liste() {
    const ara = ($('#ckAra').value || '').toLocaleLowerCase('tr-TR'), v = id => $(id).value;
    return V.kartlar.filter(k => TIPLER.includes(k.kartTipi) && k.risk && k.limit)
      .filter(k => (!v('#ckFTip') || k.kartTipi === v('#ckFTip')) && (!v('#ckFBolge') || k.bolge === v('#ckFBolge')) && (!v('#ckFUlke') || k.ulke === v('#ckFUlke')) && (!v('#ckFSinif') || k.sinif === v('#ckFSinif')) && (!v('#ckFIliski') || k.iliskiDurumu === v('#ckFIliski')) && (!v('#ckFRol') || k.rol === v('#ckFRol')) && (!v('#ckFDurum') || k.durum === v('#ckFDurum')) && (!v('#ckFSirket') || k.sirket === v('#ckFSirket'))
        && (!v('#ckFAna') || k.faaliyetler.some(f => f.anaGrup === v('#ckFAna'))) && (!v('#ckFGrup') || k.faaliyetler.some(f => f.grup === v('#ckFGrup') && (!v('#ckFAna') || f.anaGrup === v('#ckFAna')))) && (!v('#ckFTemsilci') || k.faaliyetler.some(f => f.satisTemsilcisi === v('#ckFTemsilci')))
        && (!v('#ckFLimit') || k.limit.durum === v('#ckFLimit')) && (!v('#ckFRisk') || k.risk.durum === v('#ckFRisk'))
        && (!ara || [k.kod, k.kisaAd, k.resmiUnvan, k.vergiNo, k.yetkili, k.eposta, k.sehir].join(' ').toLocaleLowerCase('tr-TR').includes(ara)));
  }
  function sirala(l) {
    const s = $('#cbSirala').value, n = (a, b) => b - a;
    const f = { risk: (a, b) => n(a.risk.toplamRisk, b.risk.toplamRisk), bakiye: (a, b) => n(a.risk.acikBakiye, b.risk.acikBakiye), gecmis: (a, b) => n(a.risk.vadesiGecmis, b.risk.vadesiGecmis), gecikme: (a, b) => n(a.risk.maxGecikme, b.risk.maxGecikme), kullanim: (a, b) => n(a.limit.kullanimYuzde ?? -1, b.limit.kullanimYuzde ?? -1), kod: (a, b) => a.kod.localeCompare(b.kod), ad: (a, b) => a.kisaAd.localeCompare(b.kisaAd, 'tr') }[s] || (() => 0);
    return l.slice().sort(f);
  }
  const toplam = (l, f) => l.reduce((s, k) => s + (Number(f(k)) || 0), 0);
  function kpi(l) {
    const m = l.filter(k => !ted(k)), t = l.filter(ted);
    const kutu = (k, l1, v, s, sec) => `<div style="--k:${k}" ${sec ? `data-sec="${sec}" title="Süzgeçle"` : ''}><div class="l">${l1}</div><div class="v">${v}</div><div class="s">${s}</div></div>`;
    $('#cbKpi').innerHTML = [
      kutu('#0369a1', 'Müşteri Alacağımız', tl(toplam(m, k => k.risk.acikBakiye)), `${m.length} müşteri · açık bakiye`),
      kutu('#b45309', 'Tedarikçi Borcumuz', tl(toplam(t, k => k.risk.acikBakiye)), `${t.length} tedarikçi · açık bakiye`),
      kutu('#dc2626', 'Vadesi Geçmiş', tl(toplam(l, k => k.risk.vadesiGecmis)), `${l.filter(k => k.risk.vadesiGecmisAdet > 0).length} caride · ${toplam(l, k => k.risk.vadesiGecmisAdet)} kalem`),
      kutu('#2563eb', 'Vadesi Gelecek', tl(toplam(l, k => k.risk.vadesiGelecek)), `${toplam(l, k => k.risk.vadesiGelecekAdet)} açık kalem`),
      kutu('#7c3aed', 'İçerideki Çek / Senet', tl(toplam(l, k => k.risk.cek)), `${toplam(l, k => k.risk.cekAdet)} belge vade bekliyor`),
      kutu('#991b1b', 'Riskli Cari', l.filter(k => k.risk.durum === 'Riskli').length, `${l.filter(k => k.risk.durum === 'İzlemede').length} izlemede · 30 gün kuralı`, 'risk:Riskli'),
      kutu('#f59e0b', 'Limit Aşan', l.filter(k => k.limit.durum === 'Limit Aşıldı').length, `${l.filter(k => k.limit.durum === 'Limite Yakın').length} limite yakın`, 'limit:Limit Aşıldı')].join('');
    $$('#cbKpi [data-sec]').forEach(d => d.addEventListener('click', () => { const [a, v] = d.dataset.sec.split(':'); const e = $(a === 'risk' ? '#ckFRisk' : '#ckFLimit'); e.value = e.value === v ? '' : v; ciz(); }));
  }
  function satir(k) {
    const R = k.risk, L = k.limit, kartUrl = `/CariKart/${ted(k) ? 'Tedarikci' : 'Musteri'}?kod=${encodeURIComponent(k.kod)}`, ekstreUrl = `/Rapor20?bpCode=${encodeURIComponent(k.kod)}`;
    const pb = (R.dovizler || []).map(d => `<small>${esc(d.pb)} ${esc(d.netTr)}</small>`).join('') || '<small class="text-muted">—</small>';
    const gec = R.vadesiGecmis > 0 ? `<b>${esc(R.vadesiGecmisTr)}</b><br><small>${R.vadesiGecmisAdet} kalem · ${R.maxGecikme} gün</small>` : '<span class="text-muted">—</span>';
    const gel = R.vadesiGelecek > 0 ? `${esc(R.vadesiGelecekTr)}<br><small class="text-muted">${R.vadesiGelecekAdet} kalem · ${esc(R.enYakinVade)}</small>` : '<span class="text-muted">—</span>';
    const cek = R.cek > 0 ? `${esc(R.cekTr)}<br><small class="text-muted">${R.cekAdet} belge · ${esc(R.enYakinCekVade)}</small>` : '<span class="text-muted">—</span>';
    return `<tr class="tik ${ted(k) ? 'tedarikci' : ''}" data-kod="${esc(k.kod)}" title="${esc(R.neden)}">
      <td class="k-kod"><b>${esc(k.kod)}</b></td><td title="${esc(k.bolge)} · ${esc(k.ulke)}${k.sehir ? ' · ' + esc(k.sehir) : ''} · ${esc(k.temsilciler || 'temsilci yok')}"><b>${esc(k.kisaAd)}</b><br><small class="text-muted">${esc(k.resmiUnvan)}</small></td><td class="k-tip">${esc(k.kartTipi)}</td><td>${sinifRozet(k.sinif)}</td><td class="k-vade">${bos(k.vadeSarti)}</td>
      <td class="pb">${pb}</td><td class="e bakiye"><b>${esc(R.acikBakiyeTr)}</b><br><small class="text-muted">${ted(k) ? 'borcumuz' : 'alacağımız'}</small></td><td class="e gecmis">${gec}</td><td class="e">${gel}</td><td class="e">${cek}</td><td class="e"><b>${esc(R.toplamRiskTr)}</b></td>
      <td class="e">${L.tur === 'Yok' ? '<span class="text-muted">Limit Yok</span>' : `<b>${esc(L.limitTr)}</b><br><small class="text-muted">${L.tur === 'Dinamik' ? `Dinamik %${Number(L.dinamikYuzde).toLocaleString('tr-TR')}` : 'Manuel'}</small>`}</td><td class="k-kul">${kullanimHucre(L)}</td><td class="k-risk">${riskRozet(R.durum)}</td><td class="k-durum">${durumRozet(k.durum)}</td>
      <td class="k-git"><a href="${kartUrl}" title="Cari kartı aç (detay paneli)"><i class="fas fa-address-card"></i></a><a href="${ekstreUrl}" target="_blank" rel="noopener" title="Cari Ekstre raporu (yeni sekme)"><i class="fas fa-file-invoice"></i></a></td></tr>`;
  }
  function altToplam(ad, l, cls) {
    if (!l.length) return '';
    return `<tr class="${cls}"><td colspan="6" class="text-end">${ad} · ${l.length} cari</td><td class="e">${tl(toplam(l, k => k.risk.acikBakiye))}</td><td class="e gecmis">${tl(toplam(l, k => k.risk.vadesiGecmis))}</td><td class="e">${tl(toplam(l, k => k.risk.vadesiGelecek))}</td><td class="e">${tl(toplam(l, k => k.risk.cek))}</td><td class="e">${tl(toplam(l, k => k.risk.toplamRisk))}</td><td class="e">${tl(toplam(l, k => k.limit.tur === 'Yok' ? 0 : k.limit.limit))}</td><td colspan="4">${l.filter(k => k.risk.durum === 'Riskli').length} riskli · ${l.filter(k => k.limit.durum === 'Limit Aşıldı').length} limit aşan</td></tr>`;
  }
  function ciz() {
    const l = sirala(liste()), hepsi = V.kartlar.filter(k => TIPLER.includes(k.kartTipi));
    $('#cbTablo tbody').innerHTML = l.map(satir).join('') || '<tr><td colspan="16" class="na-bos">Süzgece uyan cari yok.</td></tr>';
    const m = l.filter(k => !ted(k)), t = l.filter(ted);
    $('#cbTablo tfoot').innerHTML = (m.length && t.length ? altToplam('Müşteri Toplamı', m, 'alt') + altToplam('Tedarikçi Toplamı', t, 'alt') : '') + altToplam('Genel Toplam', l, '');
    $('#ckSayi').textContent = `${l.length} / ${hepsi.length} cari`;
    kpi(l); ciz.son = l;
  }
  function baslat() {
    secDoldur($('#ckFTip'), TIPLER, 'Tümü', ''); secDoldur($('#ckFBolge'), V.bolgeler, 'Tümü', ''); secDoldur($('#ckFIliski'), V.iliskiDurumlari, 'Tümü', ''); secDoldur($('#ckFRol'), V.roller2, 'Tümü', ''); secDoldur($('#ckFAna'), Object.keys(V.faaliyetHiyerarsisi), 'Tümü', ''); secDoldur($('#ckFSirket'), V.sirketler, 'Tümü', '');
    secDoldur($('#ckFTemsilci'), [...new Set(Object.values(V.satisTemsilcileri).flat())].sort(), 'Tümü', '');
    const bagimli = () => { const tip = $('#ckFTip').value, bolge = $('#ckFBolge').value; const tipler = tip ? [tip] : TIPLER; const l = [...new Set(tipler.flatMap(t => bolge ? siniflar(t, bolge) : siniflar(t, 'Yurtiçi').concat(siniflar(t, 'Yurtdışı'))))]; secDoldur($('#ckFSinif'), l, 'Tümü'); const ulkeler = [...new Set(V.kartlar.filter(k => tipler.includes(k.kartTipi) && (!bolge || k.bolge === bolge)).map(k => k.ulke))].sort(); secDoldur($('#ckFUlke'), ulkeler, 'Tümü'); secDoldur($('#ckFGrup'), $('#ckFAna').value ? gruplar($('#ckFAna').value) : [...new Set(Object.values(V.faaliyetHiyerarsisi).flat())], 'Tümü'); };
    $$('.ck-filtre input, .ck-filtre select').forEach(e => { const h = () => { if (['ckFTip', 'ckFBolge', 'ckFAna'].includes(e.id)) bagimli(); ciz(); }; e.addEventListener('input', h); e.addEventListener('change', h); });
    $('#cbSirala').addEventListener('change', ciz);
    $('#ckTemizle').addEventListener('click', () => { $$('.ck-filtre input').forEach(i => i.value = ''); $$('.ck-filtre select').forEach(s => s.value = ''); bagimli(); ciz(); });
    $('#cbExcel').addEventListener('click', () => csvIndir('cari_bakiye', ['Cari Kodu', 'Kısa Ad', 'Resmi Ünvan', 'Tip', 'Bölge', 'Ülke', 'Sınıf', 'İlişki', 'Temsilci', 'Vade', 'Bakiye (PB)', 'Toplam Bakiye (₺)', 'Vadesi Geçmiş (₺)', 'Geçmiş Kalem', 'En Uzun Gecikme (Gün)', 'Vadesi Gelecek (₺)', 'Çek / Senet (₺)', 'Toplam Risk (₺)', 'Kredi Limiti (₺)', 'Limit Türü', 'Kullanım %', 'Limit Durumu', 'Risk Durumu', 'Risk Nedeni', 'Durum'],
      (ciz.son || []).map(k => [k.kod, k.kisaAd, k.resmiUnvan, k.kartTipi, k.bolge, k.ulke, k.sinif, k.iliskiDurumu, k.temsilciler, k.vadeSarti, (k.risk.dovizler || []).map(d => d.pb + ' ' + d.netTr).join(' | '), k.risk.acikBakiye, k.risk.vadesiGecmis, k.risk.vadesiGecmisAdet, k.risk.maxGecikme, k.risk.vadesiGelecek, k.risk.cek, k.risk.toplamRisk, k.limit.tur === 'Yok' ? '' : k.limit.limit, k.limit.tur, k.limit.kullanimYuzde ?? '', k.limit.durum, k.risk.durum, k.risk.neden, k.durum])));
    // adres satırı ön ayarları (?tip=&risk=&limit=&ara=)
    const q = new URLSearchParams(location.search); const on = (id, p) => { const v = q.get(p); if (v && $(id)) $(id).value = v; };
    on('#ckFTip', 'tip'); on('#ckFTip', 'cariTipi'); on('#ckFRisk', 'risk'); on('#ckFLimit', 'limit'); if (q.get('ara') || q.get('searchKeyword')) $('#ckAra').value = q.get('ara') || q.get('searchKeyword');
    bagimli(); ciz();
  }
  fetch('/CariKart/Veri').then(r => r.json()).then(j => { V = j; baslat(); }).catch(e => mesaj('danger', 'Veri yüklenemedi: ' + esc(e.message)));
})();
