// ============================================================
// ORTAK CARİ SÜZGECİ (istemci) — Müşteri / Tedarikçi Cari Kartları'ndaki 14 süzgeç; sunucu alanları Data/CariSuzgec.cs (Alanlar / Secenekler)
//   Kullanım: const S = CariSuzgec.kur(kapsayiciEl, secenekler, { tip: 'Müşteri'|'Tedarikçi'|'' , tipSabit: bool, onChange: fn, temsilciEtiket: 'Satış Temsilcisi' });
//             liste.filter(S.uyuyor)  · S.temizle()  · S.urlOn(URLSearchParams)  · S.deger('tip')
//   Her ekranda aynı etiketler, aynı sıra, aynı bağımlı seçimler (Tip → Sınıf / Ülke; Ana Grup → Grup).
// ============================================================
(() => {
  'use strict';
  const esc = s => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;');
  const alanlar = [['ara', 'Ara', 'input'], ['tip', 'Cari Kart Tipi'], ['bolge', 'Cari Bölge'], ['ulke', 'Ülke'], ['sinif', 'Cari Sınıfı'], ['iliski', 'İlişki Durumu'], ['rol', 'Cari Rolü'], ['durum', 'Durum'], ['ana', 'Faaliyet Ana Grubu'], ['grup', 'Faaliyet Grubu'], ['temsilci', 'Satış Temsilcisi'], ['sirket', 'Şirket (ERP)'], ['limit', 'Limit Durumu'], ['risk', 'Risk Durumu']];
  function kur(kap, O, ayar) {
    ayar = ayar || {}; const on = ayar.onEk || 'cs'; const id = a => on + '_' + a;
    kap.classList.add('cs-filtre');
    kap.innerHTML = alanlar.map(([a, l, t]) => `<div><label>${esc(a === 'temsilci' && ayar.temsilciEtiket ? ayar.temsilciEtiket : l)}</label>${t === 'input' ? `<input class="form-control" id="${id(a)}" placeholder="Kod, ünvan, VKN, yetkili, e-posta…">` : `<select class="form-select" id="${id(a)}"><option value="">Tümü</option></select>`}</div>`).join('');
    const $ = a => kap.querySelector('#' + id(a)); const v = a => ($(a) ? $(a).value : '');
    const doldur = (a, l, ilk, deger) => { const e = $(a); const eski = deger !== undefined ? deger : e.value; e.innerHTML = `<option value="">${esc(ilk || 'Tümü')}</option>` + (l || []).map(x => `<option value="${esc(x)}">${esc(x)}</option>`).join(''); e.value = (l || []).includes(eski) ? eski : ''; };
    const siniflar = (tip, bolge) => O.sinifMatrisi[tip + '|' + bolge] || O.sinifMatrisi[tip + '|*'] || [];
    const tipler = ayar.tipler || O.kartTipleri;
    doldur('tip', tipler, ayar.tipSabit ? ayar.tip : (ayar.tipIlk || 'Tümü'), ayar.tip || ''); if (ayar.tipSabit) $('tip').disabled = true;
    doldur('bolge', O.bolgeler); doldur('iliski', O.iliskiDurumlari); doldur('rol', O.roller); doldur('durum', O.durumlar); doldur('ana', Object.keys(O.faaliyetHiyerarsisi)); doldur('temsilci', O.temsilciler); doldur('sirket', O.sirketler); doldur('limit', O.limitDurumlari); doldur('risk', O.riskDurumlari);
    let KART = [];
    function bagimli() {
      const tip = v('tip'), bolge = v('bolge');
      const l = tip ? siniflar(tip, bolge || 'Yurtiçi').concat(bolge ? [] : siniflar(tip, 'Yurtdışı')) : [...new Set(Object.values(O.sinifMatrisi).flat())]; doldur('sinif', [...new Set(l)]);
      const ulkeler = KART.length ? [...new Set(KART.filter(k => (!tip || k.kartTipi === tip) && (!bolge || k.bolge === bolge)).map(k => k.ulke).filter(Boolean))].sort() : O.ulkeler; doldur('ulke', ulkeler);
      doldur('grup', v('ana') ? (O.faaliyetHiyerarsisi[v('ana')] || []) : [...new Set(Object.values(O.faaliyetHiyerarsisi).flat())]);
    }
    bagimli();
    kap.querySelectorAll('input, select').forEach(e => ['input', 'change'].forEach(ev => e.addEventListener(ev, () => { if ([id('tip'), id('bolge'), id('ana')].includes(e.id)) bagimli(); if (ayar.onChange) ayar.onChange(); })));
    const S = {
      deger: v,
      kartlar: l => { KART = l || []; bagimli(); },
      uyuyor: k => {
        const ara = (v('ara') || '').toLocaleLowerCase('tr-TR');
        return (!v('tip') || k.kartTipi === v('tip')) && (!v('bolge') || k.bolge === v('bolge')) && (!v('ulke') || k.ulke === v('ulke')) && (!v('sinif') || k.sinif === v('sinif')) && (!v('iliski') || k.iliskiDurumu === v('iliski')) && (!v('rol') || k.rol === v('rol')) && (!v('durum') || k.durum === v('durum')) && (!v('sirket') || k.sirket === v('sirket'))
          && (!v('ana') || (k.anaGruplar || []).includes(v('ana'))) && (!v('grup') || (k.gruplar || []).some(g => g.split('|')[1] === v('grup') && (!v('ana') || g.split('|')[0] === v('ana')))) && (!v('temsilci') || (k.temsilciler || []).includes(v('temsilci')))
          && (!v('limit') || k.limitDurum === v('limit')) && (!v('risk') || k.risk === v('risk'))
          && (!ara || [k.kod, k.kisaAd, k.resmiUnvan, k.vergiNo, k.yetkili, k.eposta, k.sehir, k.temsilci].join(' ').toLocaleLowerCase('tr-TR').includes(ara));
      },
      temizle: () => { kap.querySelectorAll('input').forEach(i => i.value = ''); kap.querySelectorAll('select').forEach(s => { if (!s.disabled) s.value = ''; }); bagimli(); if (ayar.onChange) ayar.onChange(); },
      urlOn: q => { let d = false; alanlar.forEach(([a]) => { const x = q.get(a); if (x && $(a) && !$(a).disabled) { $(a).value = x; d = true; if (['tip', 'bolge', 'ana'].includes(a)) bagimli(); } }); if (d) { const s = q.get('sinif'); if (s) $('sinif').value = s; const g = q.get('grup'); if (g) $('grup').value = g; const u = q.get('ulke'); if (u) $('ulke').value = u; } return d; },
      ozet: () => alanlar.filter(([a]) => v(a) && !(a === 'tip' && ayar.tipSabit)).map(([a, l]) => `${l}: ${v(a)}`).join(' · ')
    };
    return S;
  }
  window.CariSuzgec = { kur };
})();
