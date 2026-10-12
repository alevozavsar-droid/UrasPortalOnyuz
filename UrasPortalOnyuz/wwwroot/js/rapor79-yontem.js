// ============================================================
// KASA NAKİT AKIŞI — ödeme yöntemi sekmeleri (Views/Rapor79/Index.cshtml › .yontem-panel; ana betik wwwroot/js/rapor79.js)
//   sekme geçişi      : .odeme-filter-btn (ana projedeki süzgeç düğmeleri) → "Tümü" ana liste, diğerleri yöntem paneli; seçim tarayıcıda saklanır (r79Yontem:<db>)
//   Çek / Senet       : satırdaki "Belge Seç" → #belgeSecModal (/Rapor79/SecilebilirBelgeler: portföyde, planlanmamış, kullanılabilir) → /Rapor79/BelgePlanla
//                       → belgeler Çek Akıbeti Raporu'nda "Planlandı" (planlanan tedarikçi = satırın kurumu), satır tutarı belge toplamı; "Kaldır" → /Rapor79/BelgePlanKaldir
//   belge hücreleri   : satırın data-belgeler verisinden çizilir (no, banka, keşideci, kimden geldi, vade, tutar, akıbet + plan ↔ ibraz kontrolü)
//   yönteme özel alan : .yontem-alan (IBAN, dekont no, kasa, makbuz, kart, taksit, mahsup fişi, temlik …) değişince /Rapor79/YontemAlanKaydet
//   ana liste senkronu: planlama sonrası Tümü sekmesindeki satırın tutar / PB / vade / not / durum alanları güncellenir
// ============================================================
(function () {
    'use strict';
    var $ = window.jQuery; if (!$) return;
    var csrf = $('input[name="__RequestVerificationToken"]').first().val() || '';
    var para = function (v) { return (Number(v) || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); };
    var tarih = function (s) { if (!s) return '-'; var d = new Date(s); return isNaN(d) ? String(s) : d.toLocaleDateString('tr-TR'); };
    var esc = function (s) { return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/"/g, '&quot;'); };
    var sayiOku = function (t) { return parseFloat(String(t || '0').replace(/\./g, '').replace(',', '.')) || 0; };
    var toast = function (tip, m) { if (window.toastr && toastr[tip]) toastr[tip](m); else alert(m); };
    var kontrolRozet = function (k, x) {
        if (k === 'Uyumlu') return '<span class="badge yp-kontrol-uyumlu" title="Planlanan tedarikçiye çıktı"><i class="fas fa-check"></i> Uyumlu</span>';
        if (k === 'Farklı') return '<span class="badge yp-kontrol-farkli" title="DİKKAT: plan ' + esc(x.planlananTedarikci) + ', çıkış ' + esc(x.ibrazYeri) + '"><i class="fas fa-triangle-exclamation"></i> FARKLI → ' + esc(x.ibrazYeri) + '</span>';
        if (k === 'İbraz Bekliyor') return '<span class="badge yp-kontrol-bekliyor" title="Planlandı, henüz çıkış yok"><i class="fas fa-hourglass-half"></i> İbraz Bekliyor</span>';
        if (k === 'Plansız İbraz') return '<span class="badge yp-kontrol-plansiz">Plansız İbraz</span>';
        return '';
    };
    var akibetRozet = function (a) { var r = a === 'Planlandı' ? 'bg-info text-dark' : a === 'Ciro Edildi' ? 'bg-secondary' : a === 'Portföyde' ? 'bg-light text-dark border' : (a || '').indexOf('Tahsil') >= 0 ? 'bg-success' : a === 'Karşılıksız' ? 'bg-danger' : 'bg-light text-dark border'; return '<span class="badge ' + r + '">' + esc(a || '-') + '</span>'; };
    var vadeRozet = function (v) { var r = v === 'Vadesi Geçti' ? 'bg-danger' : v === 'Bugün' || v === 'Bu Hafta' ? 'bg-warning text-dark' : v === 'Bu Ay' ? 'bg-info text-dark' : 'bg-light text-dark border'; return '<span class="badge ' + r + '">' + esc(v || '') + '</span>'; };

    // ---- belge hücreleri (Çek / Senet panelleri)
    function belgeHucreleri(tr, row) {
        var b = row.belgeler || [];
        var kolon = function (cls, fn) { tr.find('td.' + cls).html(b.length ? b.map(function (x) { return '<div class="yp-b">' + fn(x) + '</div>'; }).join('') : '<div class="yp-b text-muted">—</div>'); };
        kolon('yp-no', function (x) { return '<b>' + esc(x.no) + '</b> <span class="badge bg-light text-dark border">' + esc(x.belge) + '</span>'; });
        kolon('yp-banka', function (x) { return esc(x.banka); });
        kolon('yp-kesideci', function (x) { return esc(x.asilBorclu); });
        kolon('yp-kimden', function (x) { return esc(x.kimdenGeldi); });
        kolon('yp-vade', function (x) { return tarih(x.vade); });
        kolon('yp-btutar', function (x) { return para(x.tutar) + ' ' + (x.pb === 'TRY' ? '₺' : esc(x.pb)); });
        kolon('yp-akibet', function (x) { return akibetRozet(x.akibet) + ' ' + kontrolRozet(x.kontrol, x); });
        tr.find('td.yp-islem-belge').html((b.length ? b.map(function (x) { return '<div class="yp-b"><button type="button" class="btn btn-link btn-sm p-0 text-danger yp-kaldir" data-cek="' + x.cekDocEntry + '" title="Planı kaldır: belge portföye döner (Çek Akıbeti: Portföyde)"' + (x.akibet !== 'Planlandı' ? ' disabled' : '') + '><i class="fas fa-times"></i> Kaldır</button></div>'; }).join('') : '') + '<div class="yp-b"><button type="button" class="btn btn-primary btn-sm py-0 yp-sec"><i class="fas fa-plus"></i> Belge Seç</button></div>');
        if (row.tutar !== undefined) {
            tr.find('.yp-tutar').text(para(row.tutar)); tr.find('.yp-tutartl').text(para(row.tutarTL)); tr.find('.yp-pb').text(row.pb === 'TRY' ? 'TL' : row.pb);
            tr.find('.yp-durum').text(row.durum || ''); tr.find('.yp-plan').text(tarih(row.planlananTarih)); tr.find('.yp-not').text(row.notlar || '');
            tr.css('background-color', row.durum === 'ÖDENDİ' ? '#d4edda' : row.durum === 'Gecikti' ? '#f8d7da' : row.durum === 'Özel Onay' ? '#e2d9f3' : row.durum === 'Planlandı' ? '#cff4fc' : 'transparent');
        }
        tr.attr('data-belgeler', JSON.stringify(b));
    }
    $('tr.yontem-satir').each(function () { var tr = $(this), b = []; try { b = JSON.parse(tr.attr('data-belgeler') || '[]'); } catch (e) { } if (tr.find('td.yp-no').length) belgeHucreleri(tr, { belgeler: b }); });

    // ---- sekme geçişi (ana projedeki .odeme-filter-btn tıklaması ana listeyi süzer; burada panel değişir)
    function sekmeGoster(db, method) {
        var sec = $('#company-' + db);
        sec.find('.ana-liste').toggle(method === 'Tümü');
        sec.find('.yontem-panel').hide();
        if (method !== 'Tümü') sec.find('.yontem-panel[data-method="' + method + '"]').show();
        try { localStorage.setItem('r79Yontem:' + db, method); } catch (e) { }
    }
    $(document).on('click', '.odeme-filter-btn', function () { sekmeGoster($(this).data('dbname'), $(this).data('method')); });
    $('.odeme-yontem-grubu').each(function () {
        var db = $(this).data('dbname'), m = null; try { m = localStorage.getItem('r79Yontem:' + db); } catch (e) { }
        if (m && m !== 'Tümü') { var b = $(this).find('.odeme-filter-btn[data-method="' + m + '"]'); if (b.length) b.trigger('click'); }
    });

    // ---- Düzenle: ana listedeki satırın kalem düğmesi (duzenleModal)
    $(document).on('click', '.yp-duzenle', function () {
        var tr = $(this).closest('tr'); var legacy = $('.main-payment-table[data-dbname="' + tr.data('dbname') + '"] tr.data-row[data-docentry="' + tr.data('docentry') + '"] .edit-btn');
        if (legacy.length) legacy.trigger('click'); else toast('info', 'Bu kayıt ana listede yok; Tümü sekmesinden düzenleyin.');
    });

    // ---- yönteme özel alan kaydı
    $(document).on('change', '.yontem-alan', function () {
        var inp = $(this), tr = inp.closest('tr');
        fetch('/Rapor79/YontemAlanKaydet', { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrf }, body: JSON.stringify({ DbName: tr.data('dbname'), DocEntry: tr.data('docentry'), Alan: inp.data('alan'), Deger: inp.val() }) })
            .then(function (r) { return r.json(); }).then(function (d) { if (d.success) { inp.addClass('is-valid'); setTimeout(function () { inp.removeClass('is-valid'); }, 1500); } else toast('error', d.message || 'Kaydedilemedi.'); })
            .catch(function () { toast('error', 'Ağ hatası.'); });
    });

    // ---- belge seçimi
    var secim = { db: null, doc: null, belge: 'Çek', tr: null, liste: [] };
    function sayac() {
        var c = $('.bs-chk:checked'), t = 0; c.each(function () { t += parseFloat(this.dataset.tutar) || 0; });
        $('#bsSayi').text(c.length); $('#bsToplam').text(para(t));
        var satirTutar = secim.tr ? sayiOku(secim.tr.find('.yp-tutar').text()) : 0;
        $('#bsFark').text(c.length && satirTutar ? '(satır tutarı ' + para(satirTutar) + ' ₺ · fark ' + (t - satirTutar >= 0 ? '+' : '') + para(t - satirTutar) + ' ₺; satır tutarı belge toplamı olur)' : '');
    }
    function listeCiz() {
        var q = ($('#bsAra').val() || '').toLocaleLowerCase('tr-TR');
        var l = secim.liste.filter(function (x) { return !q || [x.no, x.asilBorclu, x.kimdenGeldi, x.banka, x.musteriKod].join(' ').toLocaleLowerCase('tr-TR').indexOf(q) >= 0; });
        $('#bsGovde').html(l.length ? l.map(function (x) {
            return '<tr><td><input type="checkbox" class="form-check-input bs-chk" value="' + x.docEntry + '" data-tutar="' + x.tutar + '"></td><td class="fw-bold">' + esc(x.no) + '</td><td>' + esc(x.banka) + ' / ' + esc(x.sube) + '</td><td>' + esc(x.asilBorclu) + '</td><td>' + esc(x.kimdenGeldi) + '<br><small class="text-muted">' + esc(x.musteriKod) + '</small></td><td>' + tarih(x.vade) + '</td><td>' + vadeRozet(x.vadeDurumu) + '</td><td class="text-end fw-bold">' + para(x.tutar) + ' ₺</td><td>' + tarih(x.portfoyGiris) + '</td></tr>';
        }).join('') : '<tr><td colspan="9" class="text-center text-muted py-3">Portföyde seçilebilir ' + secim.belge.toLocaleLowerCase('tr-TR') + ' yok (planlanmamış ve kullanılabilir olmalı).</td></tr>');
        $('#bsHepsi').prop('checked', false); sayac();
    }
    $(document).on('click', '.yp-sec', function () {
        var tr = $(this).closest('tr');
        secim = { db: tr.data('dbname'), doc: tr.data('docentry'), belge: tr.closest('.yontem-panel').data('method') || 'Çek', tr: tr, liste: [] };
        $('#bsBaslik').text(secim.belge + ' Seç'); $('#bsKurum').text(tr.find('.yp-kurum').text()); $('#bsBilgi').text('Satır tutarı ' + tr.find('.yp-tutar').text() + ' ' + tr.find('.yp-pb').text() + ' · seçilen belgeler Çek Akıbeti Raporu\'nda "Planlandı" olur, planlanan tedarikçi bu kurum');
        $('#bsAra').val(''); $('#bsGovde').html('<tr><td colspan="9" class="text-center text-muted py-3"><i class="fas fa-spinner fa-spin"></i> Portföy getiriliyor…</td></tr>'); sayac();
        $('#belgeSecModal').modal('show');
        fetch('/Rapor79/SecilebilirBelgeler?dbName=' + encodeURIComponent(secim.db) + '&belge=' + encodeURIComponent(secim.belge)).then(function (r) { return r.json(); })
            .then(function (d) { secim.liste = (d && d.data) || []; listeCiz(); })
            .catch(function () { $('#bsGovde').html('<tr><td colspan="9" class="text-center text-danger py-3">Ağ hatası.</td></tr>'); });
    });
    $(document).on('input', '#bsAra', listeCiz);
    $(document).on('change', '.bs-chk', sayac);
    $(document).on('change', '#bsHepsi', function () { $('.bs-chk').prop('checked', this.checked); sayac(); });
    $(document).on('click', '#bsOnayla', function () {
        var ids = $('.bs-chk:checked').map(function () { return parseInt(this.value); }).get();
        if (!ids.length) { toast('warning', 'En az bir belge seçin.'); return; }
        var btn = $(this).prop('disabled', true);
        fetch('/Rapor79/BelgePlanla', { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrf }, body: JSON.stringify({ DbName: secim.db, DocEntry: secim.doc, CekDocEntryler: ids }) })
            .then(function (r) { return r.json(); }).then(function (d) {
                btn.prop('disabled', false);
                if (!d.success) { toast('error', d.message || 'Planlanamadı.'); return; }
                $('#belgeSecModal').modal('hide'); toast('success', d.message); satirGuncelle(secim.db, secim.doc, d.row);
            }).catch(function () { btn.prop('disabled', false); toast('error', 'Ağ hatası.'); });
    });
    $(document).on('click', '.yp-kaldir', function () {
        var tr = $(this).closest('tr'), cek = parseInt(this.dataset.cek);
        if (!confirm('Belgenin planı kaldırılsın mı? Belge portföye döner (Çek Akıbeti: Portföyde).')) return;
        fetch('/Rapor79/BelgePlanKaldir', { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrf }, body: JSON.stringify({ DbName: tr.data('dbname'), DocEntry: tr.data('docentry'), CekDocEntry: cek }) })
            .then(function (r) { return r.json(); }).then(function (d) { if (!d.success) { toast('error', d.message || 'Kaldırılamadı.'); return; } toast('info', d.message); satirGuncelle(tr.data('dbname'), tr.data('docentry'), d.row); })
            .catch(function () { toast('error', 'Ağ hatası.'); });
    });

    // ---- satır güncelleme (panel + ana liste senkronu)
    function satirGuncelle(db, doc, row) {
        if (!row) return;
        var tr = $('.yontem-tablo[data-dbname="' + db + '"] tr.yontem-satir[data-docentry="' + doc + '"]');
        if (tr.length) { if (tr.find('td.yp-no').length) belgeHucreleri(tr, row); else { tr.find('.yp-tutar').text(para(row.tutar)); tr.find('.yp-tutartl').text(para(row.tutarTL)); tr.find('.yp-not').text(row.notlar || ''); tr.find('.yp-durum').text(row.durum || ''); } }
        var legacy = $('.main-payment-table[data-dbname="' + db + '"] tr.data-row[data-docentry="' + doc + '"]');
        if (legacy.length) {
            legacy.find('.tutar-input').val(Number(row.tutar).toFixed(4)); legacy.find('.tutar-display').val(para(row.tutar));
            legacy.find('.tutartl-input').val(Number(row.tutarTL).toFixed(4)); legacy.find('.tutartl-display').val(para(row.tutarTL));
            legacy.find('.kur-input').val(Number(row.kur || 1).toFixed(4)); legacy.find('.para-birimi-select').val(row.pb);
            if (row.planlananTarih) legacy.find('input[name="U_PlanlananTarih"]').val(row.planlananTarih);
            legacy.find('[name="U_Notlar"]').val(row.notlar || ''); legacy.find('select[name="U_Durum"]').val(row.durum); legacy.find('.eski-durum-input').val(row.durum);
            if (row.odemeYontemi) legacy.find('select[name="U_OdemeYontemi"]').val(row.odemeYontemi);
            legacy.css('background-color', row.durum === 'ÖDENDİ' ? '#d4edda' : row.durum === 'Gecikti' ? '#f8d7da' : row.durum === 'Özel Onay' ? '#e2d9f3' : row.durum === 'Planlandı' ? '#cff4fc' : 'transparent');
            legacy.find('.chk-row').attr('data-tutartl', Number(row.tutarTL).toFixed(4)).data('tutartl', Number(row.tutarTL).toFixed(4));
            if (typeof window.calculateAllTotals === 'function') window.calculateAllTotals();
            if (typeof window.odemeYontemSayilariniGuncelle === 'function') window.odemeYontemSayilariniGuncelle();
        }
        var panel = tr.closest('.yontem-panel'); if (panel.length) panelToplam(panel);
    }
    function panelToplam(panel) {
        var t = 0, n = 0; panel.find('tr.yontem-satir').each(function () { n++; t += sayiOku($(this).find('.yp-tutartl').text()); });
        panel.find('.yp-panel-toplam').text(para(t)); panel.find('.yp-panel-sayi').text(n);
    }
    window.r79YontemSatirGuncelle = satirGuncelle;
})();
