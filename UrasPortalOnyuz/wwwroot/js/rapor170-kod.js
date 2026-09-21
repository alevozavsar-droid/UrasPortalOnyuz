/* Gider / Gelir Analizi (Rapor170) — satır listesinden gider / gelir kodu atama ve güncelleme.
   Seçim penceresi, Rapor163 ve fatura ekranlarındaki ile AYNI ortak bileşendir: wwwroot/js/gg-kod-secici.js (GGSecici).
   Görünüm şu köprüleri sağlar:  window.gaDurum() -> { satirlar, tur, db }   ve   window.gaKodSonrasi()  (kayıttan sonra yenile). */
(function () {
    'use strict';
    var KOD_ONBELLEK = {};      // dbKey ('' = şirket eki olmadan) -> kod listesi
    var ONERI_ONBELLEK = {};    // dbKey|cardCode|tip -> kök kod listesi

    function stilEkle() {
        if (document.getElementById('gaKodStil')) return;
        var st = document.createElement('style'); st.id = 'gaKodStil';
        st.textContent =
            '.ga-kod-btn{cursor:pointer;transition:.15s}.ga-kod-btn:hover{background:#ccfbf1;border-color:#14b8a6}' +
            '.ga-kod-bos{color:#b45309!important;background:#fffbeb!important;border:1px dashed #f59e0b!important}' +
            '.ga-sec,#gaHepsi{width:15px;height:15px;cursor:pointer;accent-color:#0d9488}';
        document.head.appendChild(st);
    }

    function kodlariGetir(dbKey) {
        if (KOD_ONBELLEK[dbKey]) return Promise.resolve(KOD_ONBELLEK[dbKey]);
        return fetch('/Rapor170/Kodlar?dbKey=' + encodeURIComponent(dbKey)).then(function (r) { return r.json(); }).then(function (res) {
            if (!res.success) throw new Error(res.message || 'Kodlar alınamadı.');
            return (KOD_ONBELLEK[dbKey] = res.data || []);
        });
    }
    function oneriGetir(dbKey, cardCode, tip) {
        if (!dbKey || !cardCode) return Promise.resolve([]);
        var a = dbKey + '|' + cardCode + '|' + tip;
        if (ONERI_ONBELLEK[a]) return Promise.resolve(ONERI_ONBELLEK[a]);
        return fetch('/Rapor170/Oneri?dbKey=' + encodeURIComponent(dbKey) + '&cardCode=' + encodeURIComponent(cardCode) + '&tip=' + tip)
            .then(function (r) { return r.json(); }).then(function (res) { return (ONERI_ONBELLEK[a] = (res && res.data) || []); }).catch(function () { return []; });
    }
    function tekDeger(dizi, fn) { var ilk = fn(dizi[0]); return dizi.every(function (x) { return fn(x) === ilk; }) ? ilk : null; }

    function ac(indeksler) {
        var d = window.gaDurum ? window.gaDurum() : null; if (!d) return;
        if (!window.GGSecici) { toastr.error('Kod seçici yüklenemedi; sayfayı yenileyin.'); return; }
        var hedef = indeksler.filter(function (i) { return d.satirlar[i]; });
        if (!hedef.length) { toastr.warning('Önce kod atanacak satırları seçin.'); return; }
        var satirlar = hedef.map(function (i) { return d.satirlar[i]; });

        var dbKey = tekDeger(satirlar, function (s) { return s.dbKey; }) || '';          // birden çok şirket -> eksiz liste
        var cardCode = dbKey ? tekDeger(satirlar, function (s) { return s.cardCode || ''; }) : null;
        var varsayilanTip = d.tur === 'Gelir' ? 'GELIR' : 'GIDER';
        // Satırın hesabından tip (Rapor163 ile aynı kural); seçimde tipler karışıksa ekranda seçili tür
        var tip = tekDeger(satirlar, function (s) { return window.GGSecici.hesapTipi(s.hesapKodu) || varsayilanTip; }) || varsayilanTip;
        var mevcutKod = tekDeger(satirlar, function (s) { return s.kod || ''; }) || '';
        var baslik = satirlar.length === 1 ? 'Satır için kod seç' : satirlar.length + ' satır için kod seç';

        Promise.all([kodlariGetir(dbKey), oneriGetir(dbKey, cardCode, tip)]).then(function (sonuc) {
            var kodlar = sonuc[0], oneriKok = sonuc[1];
            var kokBul = function (kok) { return kodlar.filter(function (k) { return k.kokKod === kok; })[0]; };
            // Mevcut kod (tam kod) listedeki karşılığına çevrilir: eksiz listede kök kod ile eşleşir
            var secili = '';
            if (mevcutKod) { var es = kodlar.filter(function (k) { return k.kod === mevcutKod || mevcutKod === k.kokKod || mevcutKod.indexOf(k.kokKod + '.') === 0; }).sort(function (a, b) { return b.kokKod.length - a.kokKod.length; })[0]; if (es) secili = es.kod; }
            var onerilen = oneriKok.map(function (kok) { var k = kokBul(kok); return k ? k.kod : null; }).filter(Boolean);
            window.GGSecici.ac({
                kodlar: kodlar, tip: tip, secili: secili, baslik: baslik, onerilen: onerilen,
                onSec: function (kod) {
                    var k = kod ? kodlar.filter(function (x) { return x.kod === kod; })[0] : null;
                    if (kod && !k) { toastr.error('Seçilen kod listede bulunamadı.'); return; }
                    if (!kod && !confirm(satirlar.length + ' satırın gider / gelir kodu kaldırılacak. Devam edilsin mi?')) return;
                    kaydet(hedef, k ? k.kokKod : '');
                }
            });
        }).catch(function (e) { toastr.error(String(e && e.message || e)); });
    }

    function kaydet(hedef, kok) {
        var d = window.gaDurum();
        var paket = { KokKod: kok, Satirlar: hedef.map(function (i) { var s = d.satirlar[i]; return { DbKey: s.dbKey, TransId: s.transId, LineId: s.lineId }; }) };
        toastr.info(paket.Satirlar.length + ' satır yazılıyor…', '', { timeOut: 1500 });
        fetch('/Rapor170/KodKaydet', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' }, body: JSON.stringify(paket) })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                if (res.success) toastr.success(res.message); else if (res.kismi) toastr.warning(res.message, '', { timeOut: 12000 }); else { toastr.error(res.message, '', { timeOut: 12000 }); return; }
                ONERI_ONBELLEK = {};
                if (window.gaKodSonrasi) window.gaKodSonrasi();
            })
            .catch(function (e) { toastr.error('Kaydedilemedi: ' + e); });
    }

    // ---- görünümün çağırdığı uçlar ----
    window.gaKodAc = function (i) { ac([i]); };
    window.gaKodSecilenler = function () {
        var ids = []; document.querySelectorAll('.ga-sec:checked').forEach(function (c) { ids.push(parseInt(c.dataset.i, 10)); });
        ac(ids);
    };
    window.gaSecimGuncelle = function () {
        var n = document.querySelectorAll('.ga-sec:checked').length, b = document.getElementById('btnKodAta');
        if (b) { b.style.display = document.querySelector('.ga-sec') ? '' : 'none'; b.innerHTML = '<i class="fas fa-tags"></i> ' + (n ? n + ' satıra kod ata' : 'Seçilenlere kod ata'); b.disabled = !n; b.style.opacity = n ? 1 : .55; }
        var h = document.getElementById('gaHepsi'); if (h) { var t = document.querySelectorAll('.ga-sec').length; h.checked = t > 0 && n === t; h.indeterminate = n > 0 && n < t; }
    };
    stilEkle();
    // Tablo her yeniden çizildiğinde (grup görünümü ↔ satır görünümü) toplu atama düğmesini güncelle
    var govde = document.getElementById('tbody');
    if (govde && window.MutationObserver) new MutationObserver(function () { window.gaSecimGuncelle(); }).observe(govde, { childList: true });
    document.addEventListener('change', function (e) {
        if (e.target.id === 'gaHepsi') { document.querySelectorAll('.ga-sec').forEach(function (c) { c.checked = e.target.checked; }); window.gaSecimGuncelle(); }
        else if (e.target.classList && e.target.classList.contains('ga-sec')) window.gaSecimGuncelle();
    });
    // Shift ile aralık seçimi
    var sonTik = null;
    document.addEventListener('click', function (e) {
        if (!e.target.classList || !e.target.classList.contains('ga-sec')) return;
        var kutular = Array.prototype.slice.call(document.querySelectorAll('.ga-sec')), i = kutular.indexOf(e.target);
        if (e.shiftKey && sonTik != null && sonTik !== i) { var a = Math.min(sonTik, i), z = Math.max(sonTik, i); for (var j = a; j <= z; j++) kutular[j].checked = e.target.checked; window.gaSecimGuncelle(); }
        sonTik = i;
    });
})();
