// Toplu Kalem Ana Verisi Güncelleme (Rapor173)
(function () {
    var onizleme = null;   // sunucudan gelen önizleme verisi
    var token = function () { return $('input[name="__RequestVerificationToken"]').val() || ''; };
    var perde = function (ac, yazi) { $('#ktPerdeYazi').text(yazi || 'İşleniyor…'); $('#ktPerde').css('display', ac ? 'flex' : 'none'); };
    var kac = function (s) { return $('<div>').text(s == null ? '' : String(s)).html(); };
    var adim = function (n) { $('.kt-adim').each(function () { var a = +$(this).data('adim'); $(this).toggleClass('aktif', a === n).toggleClass('bitti', a < n); }); };

    // ---- alan chip'leri ----
    function seciliSay() {
        var $sec = $(".kt-alan:checked");
        $("#ktSeciliSayi").text($sec.length);
        // Sol panelde seçilenlerin özeti (tıklayınca kaldırılır) + grup sayaçları
        var h = "";
        $sec.each(function () { h += "<span class=\"kt-secili-chip\">" + kac($(this).data("etiket")) + "<button type=\"button\" data-kolon=\"" + kac(this.value) + "\" title=\"Kaldır\"><i class=\"fas fa-xmark\"></i></button></span>"; });
        $("#ktSeciliListe").html(h || "<span class=\"text-muted small\">Henüz alan seçilmedi — sağdaki listeden tıkla.</span>");
        $(".kt-grup").each(function () { var t = $(this).find(".kt-alan").length, s = $(this).find(".kt-alan:checked").length; $(this).find(".kt-grup-sayac").text(s + "/" + t).toggleClass("dolu", s > 0); });
    }
    $(document).on("click", ".kt-secili-chip button", function () { $(".kt-alan[value=\"" + $(this).data("kolon") + "\"]").prop("checked", false).trigger("change"); });
    $("#btnTemizleAlan").on("click", function () { $(".kt-alan:checked").prop("checked", false).trigger("change"); });
    $(document).on('change', '.kt-alan', function () { $(this).closest('.kt-chip').toggleClass('secili', this.checked); seciliSay(); });
    $(document).on('click', '.kt-grup-hepsi', function () { $(this).closest('.kt-grup').find('.kt-alan:not(:checked)').prop('checked', true).trigger('change'); });
    $(document).on('click', '.kt-grup-hicbiri', function () { $(this).closest('.kt-grup').find('.kt-alan:checked').prop('checked', false).trigger('change'); });
    $('#ktAlanAra').on('input', function () {
        var q = (this.value || '').toLocaleLowerCase('tr-TR');
        $(".kt-chip").each(function () {
            var m = ($(this).data("kolon") + " " + $(this).data("etiket")).toLocaleLowerCase("tr-TR");
            $(this).toggleClass("gizli", !!q && m.indexOf(q) < 0);
        });
        $(".kt-grup").each(function () { $(this).toggleClass("gizli", !!q && $(this).find(".kt-chip:not(.gizli)").length === 0); });
    });

    // ---- kalem sayımı ----
    $('#btnSay').on('click', function () {
        var $s = $('#ktSayim').html('<i class="fas fa-spinner fa-spin me-1"></i> sayılıyor…');
        $.post('/Rapor173/KalemSay', { kodlar: $('#ktKodlar').val(), onek: $('#ktOnek').val(), __RequestVerificationToken: token() })
            .done(function (r) {
                if (!r.success) { $s.html('<span class="kt-eksik">' + kac(r.message) + '</span>'); return; }
                var h = '<b>' + r.bulunan + '</b> kalem bulundu.';
                if (r.ornek && r.ornek.length) h += ' <span class="text-muted">' + r.ornek.map(function (o) { return kac(o.kod); }).join(', ') + (r.bulunan > 10 ? ' …' : '') + '</span>';
                if (r.bulunamayan && r.bulunamayan.length) h += '<div class="kt-eksik mt-1"><i class="fas fa-triangle-exclamation me-1"></i>Bulunamayan kodlar: ' + r.bulunamayan.map(kac).join(', ') + '</div>';
                $s.html(h);
            })
            .fail(function () { $s.html('<span class="kt-eksik">Sunucuya ulaşılamadı.</span>'); });
    });

    // ---- şablon indir (POST → dosya) ----
    $('#btnSablon').on('click', function () {
        var alanlar = $('.kt-alan:checked').map(function () { return this.value; }).get();
        if (!alanlar.length) { alert('En az bir alan seçin.'); return; }
        if (!$('#ktKodlar').val().trim() && !$('#ktOnek').val().trim()) {
            if (!confirm('Kalem seçilmedi.\n\nBOŞ şablon indirilsin mi? Seçtiğin alanlar başlık olarak gelir, ItemCode sütununu Excel\'de sen doldurursun.\n\nMevcut değerlerle dolu şablon istiyorsan önce "Listeden toplu seç" ya da "Excel/CSV\'den kod yükle" ile kalemleri ekle.')) return;
        }
        var fd = new FormData();
        fd.append('kodlar', $('#ktKodlar').val()); fd.append('onek', $('#ktOnek').val());
        alanlar.forEach(function (a) { fd.append('alanlar', a); });
        fd.append('__RequestVerificationToken', token());
        perde(true, 'Şablon hazırlanıyor…');
        fetch('/Rapor173/SablonIndir', { method: 'POST', body: fd })
            .then(function (r) { if (!r.ok) return r.text().then(function (t) { throw new Error(t || 'Şablon üretilemedi.'); }); var ad = (r.headers.get('content-disposition') || '').match(/filename\*?=(?:UTF-8'')?"?([^";]+)/); return r.blob().then(function (b) { return { b: b, ad: ad ? decodeURIComponent(ad[1]) : 'Kalem-Guncelleme.xlsx' }; }); })
            .then(function (x) { var u = URL.createObjectURL(x.b), a = document.createElement('a'); a.href = u; a.download = x.ad; document.body.appendChild(a); a.click(); a.remove(); URL.revokeObjectURL(u); adim(2); })
            .catch(function (e) { alert(e.message); })
            .finally(function () { perde(false); });
    });

    // ---- yükle → önizle ----
    $('#ktDosya').on('change', function () {
        var f = this.files && this.files[0]; if (!f) return;
        $('#ktDosyaAd').text(f.name);
        var fd = new FormData(); fd.append('dosya', f); fd.append('__RequestVerificationToken', token());
        perde(true, 'Excel okunuyor, SAP ile karşılaştırılıyor…');
        fetch('/Rapor173/Onizle', { method: 'POST', body: fd }).then(function (r) { return r.json(); })
            .then(function (r) {
                if (!r.success) { alert(r.message); return; }
                onizleme = r; ciz(); adim(3);
                $('#ktOnizleme').removeClass('d-none'); $('#ktSonuc').addClass('d-none');
                $('#ktOnizleme')[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
            })
            .catch(function () { alert('Dosya yüklenemedi.'); })
            .finally(function () { perde(false); $('#ktDosya').val(''); });
    });
    $('#ktSadeceDegisen').on('change', ciz);

    function ciz() {
        if (!onizleme) return;
        var sadece = $('#ktSadeceDegisen').is(':checked');
        var html = '';
        onizleme.satirlar.forEach(function (s) {
            if (!s.bulundu) {
                html += '<tr class="satir-bulunamadi"><td class="kod">' + kac(s.itemCode) + '</td><td colspan="5" class="text-muted">SAP\'de böyle bir kalem yok — atlanacak</td><td><span class="kt-rozet yok">bulunamadı</span></td></tr>';
                return;
            }
            if (!s.alanlar.length) {
                if (!sadece) html += '<tr class="satir-ayni"><td class="kod">' + kac(s.itemCode) + '</td><td>' + kac(s.kalemAdi) + '</td><td colspan="4" class="text-muted">değişiklik yok</td><td><span class="kt-rozet ayni">aynı</span></td></tr>';
                return;
            }
            s.alanlar.forEach(function (a, i) {
                html += '<tr>'
                    + '<td class="kod">' + (i === 0 ? kac(s.itemCode) : '') + '</td>'
                    + '<td>' + (i === 0 ? kac(s.kalemAdi) : '') + '</td>'
                    + '<td>' + kac(a.etiket) + ' <small class="text-muted">' + kac(a.kolon) + '</small></td>'
                    + '<td>' + (a.eski === '' ? '<span class="bos">(boş)</span>' : '<span class="eski">' + kac(a.eski) + '</span>') + '</td>'
                    + '<td class="text-muted">→</td>'
                    + '<td>' + (a.yeni === '' ? '<span class="bos">(boşaltılacak)</span>' : '<span class="yeni">' + kac(a.yeni) + '</span>') + '</td>'
                    + '<td>' + (i === 0 ? '<span class="kt-rozet degisti">' + s.alanlar.length + ' alan</span>' : '') + '</td>'
                    + '</tr>';
            });
        });
        $('#ktTablo tbody').html(html || '<tr><td colspan="7" class="text-center text-muted py-4">Gösterilecek satır yok.</td></tr>');
        $('#ktOzet').html('<b>' + onizleme.toplam + '</b> satır · <b>' + onizleme.degisenKalem + '</b> kalemde <b>' + onizleme.degisenAlan + '</b> alan değişecek'
            + (onizleme.bulunamayan ? ' · <span class="text-danger">' + onizleme.bulunamayan + ' kod bulunamadı</span>' : ''));
        $('#btnUygula').prop('disabled', onizleme.degisenKalem === 0);
    }

    // ---- uygula ----
    $('#btnUygula').on('click', function () {
        if (!onizleme) return;
        var kalemler = onizleme.satirlar.filter(function (s) { return s.bulundu && s.alanlar.length; })
            .map(function (s) { return { itemCode: s.itemCode, alanlar: s.alanlar.map(function (a) { return { slOzellik: a.slOzellik, tur: a.tur, yeni: a.yeni }; }) }; });
        if (!kalemler.length) { alert('Uygulanacak değişiklik yok.'); return; }
        if (!confirm(kalemler.length + ' kalemde toplam ' + onizleme.degisenAlan + ' alan SAP\'ye yazılacak. Devam edilsin mi?')) return;
        perde(true, kalemler.length + ' kalem Service Layer ile güncelleniyor…');
        fetch('/Rapor173/Uygula', { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() }, body: JSON.stringify({ kalemler: kalemler }) })
            .then(function (r) { return r.json(); })
            .then(function (r) {
                if (!r.success) { alert(r.message); return; }
                var h = '';
                (r.sonuclar || []).forEach(function (s) {
                    h += '<div class="sat"><span class="kod me-2" style="font-family:Consolas,monospace;font-weight:700;">' + kac(s.itemCode) + '</span>'
                        + (s.ok ? '<span class="kt-rozet ok">güncellendi</span> <span class="text-muted">' + s.alan + ' alan</span>' : '<span class="kt-rozet hata">hata</span> <span class="text-danger">' + kac(s.mesaj) + '</span>') + '</div>';
                });
                $('#ktSonucOzet').html('<b>' + r.basarili + '</b> başarılı · <b class="text-danger">' + r.hatali + '</b> hatalı');
                $('#ktSonucListe').html(h);
                $('#ktSonuc').removeClass('d-none')[0].scrollIntoView({ behavior: 'smooth', block: 'start' });
                if (typeof toastr !== 'undefined') toastr[r.hatali ? 'warning' : 'success'](r.basarili + ' kalem güncellendi' + (r.hatali ? ', ' + r.hatali + ' hatalı' : '') + '.');
            })
            .catch(function () { alert('Sunucuya ulaşılamadı.'); })
            .finally(function () { perde(false); });
    });
})();


// ---- Kod kaynakları: Excel/CSV'den yükle, listeden toplu seç (tüm kalemler + seçilenler paneli) ----
(function () {
    var kac = function (s) { return $('<div>').text(s == null ? '' : String(s)).html(); };
    var token = function () { return $('input[name="__RequestVerificationToken"]').val() || ''; };
    function kodlar() { return ($('#ktKodlar').val() || '').split(/[\n\r,;\t]+/).map(function (k) { return k.trim(); }).filter(Boolean); }
    function kodSayac() { var n = kodlar().length; $('#ktKodSayac').text(n + ' kod'); }
    function kodEkle(yeni) {
        var mevcut = kodlar(), set = {}; mevcut.forEach(function (k) { set[k.toLowerCase()] = true; });
        var eklenen = 0; yeni.forEach(function (k) { k = String(k).trim(); if (k && !set[k.toLowerCase()]) { mevcut.push(k); set[k.toLowerCase()] = true; eklenen++; } });
        $('#ktKodlar').val(mevcut.join('\n')); kodSayac(); return eklenen;
    }
    $('#ktKodlar').on('input', kodSayac); kodSayac();
    $('#btnKodTemizle').on('click', function () { $('#ktKodlar').val(''); kodSayac(); $('#ktSayim').empty(); });

    // Excel / CSV'den kod
    $('#ktKodDosya').on('change', function () {
        var f = this.files && this.files[0]; if (!f) return;
        var fd = new FormData(); fd.append('dosya', f); fd.append('__RequestVerificationToken', token());
        $('#ktSayim').html('<i class="fas fa-spinner fa-spin me-1"></i> dosya okunuyor…');
        fetch('/Rapor173/KodlariOku', { method: 'POST', body: fd }).then(function (r) { return r.json(); })
            .then(function (r) {
                if (!r.success) { $('#ktSayim').html('<span class="kt-eksik">' + kac(r.message) + '</span>'); return; }
                var n = kodEkle(r.kodlar || []);
                $('#ktSayim').html('<b>' + r.adet + '</b> kod okundu, <b>' + n + '</b> yeni kod eklendi. "Kaç kalem?" ile SAP\'de var mı diye kontrol edebilirsin.');
            })
            .catch(function () { $('#ktSayim').html('<span class="kt-eksik">Dosya yüklenemedi.</span>'); })
            .finally(function () { $('#ktKodDosya').val(''); });
    });

    // ---- Listeden toplu seç ----
    var tum = null;          // [{kod, ad, grup, grupAd, aktif, birim, ara}]
    var gruplar = {};        // grupKod -> ad
    var suzulen = [];        // filtreye uyanlar
    var gosterilen = 0;      // kaç satır çizildi
    var SAYFA = 300;
    var secili = {};         // kod -> {kod, ad}
    var sonTiklanan = null;  // shift aralığı için
    var yukleniyor = false;

    function secOzet() {
        var keys = Object.keys(secili);
        $('#ktSecSayi').text(keys.length);
        if (!keys.length) { $('#ktSecSecili').html('<div class="text-muted small p-3 text-center">Henüz kalem seçilmedi. Soldaki listeden tıkla.</div>'); return; }
        var h = '';
        keys.sort().forEach(function (k) { var s = secili[k]; h += '<div class="kt-sec-sat" data-kod="' + kac(k) + '"><span class="kod">' + kac(k) + '</span><span class="ad">' + kac(s.ad) + '</span><button type="button" title="Kaldır"><i class="fas fa-xmark"></i></button></div>'; });
        $('#ktSecSecili').html(h);
    }
    function satirHtml(k) {
        var on = !!secili[k.kod];
        return '<tr data-kod="' + kac(k.kod) + '" class="' + (on ? 'secili' : '') + '">'
            + '<td class="text-center"><input type="checkbox" class="form-check-input kt-sec-kutu" ' + (on ? 'checked' : '') + ' tabindex="-1"></td>'
            + '<td class="kod">' + kac(k.kod) + '</td><td>' + kac(k.ad) + '</td><td class="text-muted small">' + kac(k.grupAd || k.grup) + '</td><td class="text-muted small">' + kac(k.birim) + '</td>'
            + '<td>' + (k.aktif ? '<span class="kt-rozet ok">aktif</span>' : '<span class="kt-rozet ayni">pasif</span>') + '</td></tr>';
    }
    function ciz(devam) {
        var $tb = $('#ktSecTablo tbody');
        if (!devam) { gosterilen = 0; $tb.empty(); }
        if (!suzulen.length) { $tb.html('<tr><td colspan="6" class="text-center text-muted py-4">Sonuç yok.</td></tr>'); $('#ktSecDaha').addClass('d-none'); return; }
        var h = '', son = Math.min(suzulen.length, gosterilen + SAYFA);
        for (var i = gosterilen; i < son; i++) h += satirHtml(suzulen[i]);
        $tb.append(h); gosterilen = son;
        $('#ktSecDaha').toggleClass('d-none', gosterilen >= suzulen.length);
        $('#btnSecDaha').text('Daha fazla göster (' + (suzulen.length - gosterilen) + ' kalem daha)');
        $('#ktSecOzet').text(suzulen.length + ' kalem · ' + gosterilen + ' gösteriliyor');
    }
    function suz() {
        if (!tum) return;
        var q = ($('#ktSecAra').val() || '').toLocaleLowerCase('tr-TR').trim(), g = $('#ktSecGrup').val(), d = $('#ktSecDurum').val();
        suzulen = tum.filter(function (k) {
            if (d === 'aktif' && !k.aktif) return false;
            if (d === 'pasif' && k.aktif) return false;
            if (g && k.grup !== g) return false;
            return !q || k.ara.indexOf(q) >= 0;
        });
        $('#ktSecHepsi').prop('checked', false);
        ciz(false);
    }
    function yukle() {
        if (tum || yukleniyor) return; yukleniyor = true;
        $.getJSON('/Rapor173/TumKalemler').done(function (r) {
            if (!r.success) { $('#ktSecTablo tbody').html('<tr><td colspan="6" class="text-danger text-center py-3">' + kac(r.message) + '</td></tr>'); return; }
            gruplar = {}; var $g = $('#ktSecGrup');
            (r.gruplar || []).forEach(function (g) { gruplar[g.kod] = g.ad; $g.append($('<option>').val(g.kod).text(g.ad)); });
            tum = (r.kalemler || []).map(function (a) { return { kod: a[0], ad: a[1] || '', grup: String(a[2]), grupAd: gruplar[String(a[2])] || '', aktif: a[3] === 1, birim: a[4] || '', ara: (a[0] + ' ' + (a[1] || '')).toLocaleLowerCase('tr-TR') }; });
            $('#ktSecToplam').text(tum.length + ' kalem');
            suz();
        }).fail(function () { $('#ktSecTablo tbody').html('<tr><td colspan="6" class="text-danger text-center py-3">Kalemler yüklenemedi.</td></tr>'); })
            .always(function () { yukleniyor = false; });
    }
    $('#btnKodSec').on('click', function () {
        bootstrap.Modal.getOrCreateInstance(document.getElementById('ktSecModal')).show();
        yukle();
        setTimeout(function () { $('#ktSecAra').trigger('focus'); }, 300);
    });
    var suzZaman = null;
    $('#ktSecAra').on('input', function () { clearTimeout(suzZaman); suzZaman = setTimeout(suz, 120); });
    $('#ktSecGrup, #ktSecDurum').on('change', suz);
    $('#btnSecDaha').on('click', function () { ciz(true); });

    function secDegistir(kod, on) {
        var k = null; if (tum) { for (var i = 0; i < tum.length; i++) { if (tum[i].kod === kod) { k = tum[i]; break; } } }
        if (on) secili[kod] = { kod: kod, ad: k ? k.ad : '' }; else delete secili[kod];
        var $tr = $('#ktSecTablo tbody tr[data-kod="' + kod.replace(/"/g, '\\"') + '"]');
        $tr.toggleClass('secili', on).find('.kt-sec-kutu').prop('checked', on);
    }
    $(document).on('click', '#ktSecTablo tbody tr[data-kod]', function (e) {
        var kod = $(this).data('kod') + '', on = !secili[kod];
        if (e.shiftKey && sonTiklanan !== null) {
            // aralık: son tıklanan ile bu satır arasındaki süzülmüş kalemler
            var a = -1, b = -1;
            for (var i = 0; i < suzulen.length; i++) { if (suzulen[i].kod === sonTiklanan) a = i; if (suzulen[i].kod === kod) b = i; }
            if (a >= 0 && b >= 0) { var lo = Math.min(a, b), hi = Math.max(a, b); for (var j = lo; j <= hi; j++) secDegistir(suzulen[j].kod, true); }
            else secDegistir(kod, on);
            window.getSelection && window.getSelection().removeAllRanges();
        } else secDegistir(kod, on);
        sonTiklanan = kod; secOzet();
    });
    $('#ktSecHepsi').on('change', function () { var on = this.checked; suzulen.forEach(function (k) { if (on) secili[k.kod] = { kod: k.kod, ad: k.ad }; else delete secili[k.kod]; }); $('#ktSecTablo tbody tr[data-kod]').each(function () { $(this).toggleClass('secili', on).find('.kt-sec-kutu').prop('checked', on); }); secOzet(); });
    $(document).on('click', '#ktSecSecili .kt-sec-sat button', function () { secDegistir($(this).closest('.kt-sec-sat').data('kod') + '', false); secOzet(); });
    $('#ktSecTemizle').on('click', function () { secili = {}; $('#ktSecTablo tbody tr[data-kod]').removeClass('secili').find('.kt-sec-kutu').prop('checked', false); secOzet(); });
    $('#btnSecEkle').on('click', function () {
        var n = kodEkle(Object.keys(secili));
        var m = bootstrap.Modal.getInstance(document.getElementById('ktSecModal')); if (m) m.hide();
        $('#ktSayim').html('<b>' + Object.keys(secili).length + '</b> kalem seçildi, <b>' + n + '</b> yeni kod listeye eklendi.');
        secili = {}; $('#ktSecTablo tbody tr[data-kod]').removeClass('secili').find('.kt-sec-kutu').prop('checked', false); secOzet();
    });
})();
