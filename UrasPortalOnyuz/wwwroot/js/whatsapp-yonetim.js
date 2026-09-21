// Sistem Yetkileri → WhatsApp sekmesi: numara yönetimi, deneme, son mesajlar
(function () {
    var yuklendi = false, liste = [];
    var token = function () { return $('input[name="__RequestVerificationToken"]').val() || ''; };
    var kac = function (s) { return $('<div>').text(s == null ? '' : String(s)).html(); };
    function hata(m) { if (window.Swal) Swal.fire('Hata', m || 'İşlem yapılamadı.', 'error'); else alert(m); }

    function tabloCiz() {
        var $tb = $('#waTablo tbody').empty();
        if (!liste.length) { $tb.html('<tr><td colspan="6" class="text-center text-muted py-3">Henüz numara tanımlı değil.</td></tr>'); }
        liste.forEach(function (k) {
            var sirket = (window.TUM_SIRKETLER || []).find(function (s) { return s.dbKey === k.varsayilanDbKey || s.DbKey === k.varsayilanDbKey; });
            $tb.append('<tr data-tel="' + kac(k.telefon) + '" class="' + (k.aktif ? '' : 'table-secondary opacity-75') + '">'
                + '<td class="fw-bold">' + kac(k.telefon) + (k.lid ? '<div class="small text-muted fw-normal" title="WhatsApp kimliği">lid ' + kac(k.lid) + '</div>' : '') + '</td><td>' + kac(k.userCode) + '</td><td>' + kac(k.ad) + '</td>'
                + '<td>' + kac(sirket ? (sirket.display || sirket.Display) : (k.varsayilanDbKey || '-')) + '</td>'
                + '<td><div class="form-check form-switch m-0"><input type="checkbox" class="form-check-input wa-aktif" ' + (k.aktif ? 'checked' : '') + '></div></td>'
                + '<td class="text-end"><button type="button" class="btn btn-sm btn-outline-danger wa-sil" title="Sil"><i class="fas fa-trash"></i></button></td></tr>');
        });
        var $sim = $('#waSimTelefon').empty();
        liste.forEach(function (k) { $sim.append($('<option>').val(k.telefon).text(k.telefon + ' · ' + (k.ad || k.userCode))); });
        if (!liste.length) $sim.append('<option value="">önce numara ekleyin</option>');
    }
    function yukle() {
        $.getJSON('/WhatsApp/Liste').done(function (r) {
            if (!r.success) { hata(r.message); return; }
            liste = r.liste || []; tabloCiz();
            var $k = $('#waKullanici').empty().append('<option value="">— kullanıcı seç —</option>');
            (r.kullanicilar || []).forEach(function (u) { $k.append($('<option>').val(u.kod).text(u.kod + (u.ad ? ' · ' + u.ad : ''))); });
            var $s = $('#waSirket'); if ($s.find('option').length <= 1) (window.TUM_SIRKETLER || []).forEach(function (s) { $s.append($('<option>').val(s.dbKey || s.DbKey).text(s.display || s.Display)); });
            $('#waAnahtarRozet').removeClass('bg-success bg-danger').addClass(r.anahtarTanimli ? 'bg-success' : 'bg-danger').text(r.anahtarTanimli ? 'köprü anahtarı tanımlı' : 'appsettings → WhatsApp:KopruAnahtari eksik');
            yuklendi = true;
        }).fail(function () { hata('Liste alınamadı.'); });
        logYukle();
    }
    function logYukle() {
        $.getJSON('/WhatsApp/Loglar').done(function (r) {
            var $tb = $('#waLogTablo tbody').empty();
            (r.loglar || []).forEach(function (l) {
                var t = l.tarih ? new Date(l.tarih) : null;
                $tb.append('<tr><td class="text-nowrap">' + (t ? t.toLocaleString('tr-TR') : '') + '</td><td>' + kac(l.telefon) + '</td><td>' + kac(l.mesaj) + '</td><td style="max-width:220px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;" title="' + kac(l.cevap) + '">' + kac(l.cevap) + '</td><td>' + (l.dosyaVar ? '<i class="fas fa-file-pdf text-danger"></i>' : '') + '</td></tr>');
            });
            if (!(r.loglar || []).length) $tb.html('<tr><td colspan="5" class="text-muted text-center py-2">Henüz mesaj yok.</td></tr>');
        });
    }
    $(document).on('shown.bs.tab', '#wa-tab', function () { if (!yuklendi) yukle(); });
    $('#waEkleBtn').on('click', function () {
        $.post('/WhatsApp/Kaydet', { telefon: $('#waTelefon').val(), userCode: $('#waKullanici').val(), ad: $('#waAd').val(), varsayilanDbKey: $('#waSirket').val(), lid: $('#waLid').val(), __RequestVerificationToken: token() })
            .done(function (r) { if (!r.success) { hata(r.message); return; } $('#waTelefon, #waAd, #waLid').val(''); yukle(); })
            .fail(function () { hata('Sunucuya ulaşılamadı.'); });
    });
    $(document).on('change', '.wa-aktif', function () {
        var tel = $(this).closest('tr').data('tel'), on = this.checked;
        $.post('/WhatsApp/AktifDegistir', { telefon: tel, aktif: on, __RequestVerificationToken: token() }).done(function () { $('tr[data-tel="' + tel + '"]').toggleClass('table-secondary opacity-75', !on); });
    });
    $(document).on('click', '.wa-sil', function () {
        var tel = $(this).closest('tr').data('tel');
        var git = function () { $.post('/WhatsApp/Sil', { telefon: tel, __RequestVerificationToken: token() }).done(yukle); };
        if (window.Swal) Swal.fire({ title: tel + ' silinsin mi?', icon: 'warning', showCancelButton: true, confirmButtonText: 'Sil', cancelButtonText: 'Vazgeç' }).then(function (r) { if (r.isConfirmed) git(); }); else if (confirm('Silinsin mi?')) git();
    });
    function simule() {
        var tel = $('#waSimTelefon').val(), msg = $('#waSimMesaj').val();
        if (!tel) { hata('Önce numara ekleyin.'); return; }
        $('#waSimCevap').html('<i class="fas fa-spinner fa-spin me-1"></i> işleniyor…');
        $.post('/WhatsApp/Simule', { telefon: tel, mesaj: msg, __RequestVerificationToken: token() }).done(function (r) {
            if (!r.success) { $('#waSimCevap').html('<span class="text-danger">' + kac(r.message) + '</span>'); return; }
            var h = '<div class="mb-2"><span class="badge bg-secondary me-1">sen</span>' + kac(msg) + '</div><div><span class="badge bg-success me-1">asistan</span>' + kac(r.cevap) + '</div>';
            if (r.dosyaBase64) h += '<div class="mt-2"><a class="btn btn-sm btn-outline-danger" download="' + kac(r.dosyaAdi) + '" href="data:' + (r.dosyaTuru || 'application/pdf') + ';base64,' + r.dosyaBase64 + '"><i class="fas fa-file me-1"></i>' + kac(r.dosyaAdi) + ' (' + Math.round(r.dosyaBoyut / 1024) + ' KB)</a>' + (r.ekDosyaAdi ? ' <span class="text-muted small">+ ' + kac(r.ekDosyaAdi) + ' (' + Math.round(r.ekDosyaBoyut / 1024) + ' KB)</span>' : '') + '</div>';
            if (r.secenekler && r.secenekler.secimler && r.secenekler.secimler.length) {
                h += '<div class="mt-2 p-2 rounded-2" style="background:#e8f5e9;border:1px dashed #25d366;"><div class="small fw-bold mb-1"><i class="fas fa-square-poll-vertical me-1"></i>' + kac(r.secenekler.baslik) + ' <span class="text-muted fw-normal">(WhatsApp\'ta anket olarak gelir, dokununca seçilir)</span></div>';
                r.secenekler.secimler.forEach(function (s) { h += '<button type="button" class="btn btn-sm btn-outline-success me-1 mb-1 wa-sim-secim" data-komut="' + kac(s.komut) + '">' + kac(s.etiket) + '</button>'; });
                h += '</div>';
            }
            $('#waSimCevap').html(h); logYukle();
        }).fail(function () { $('#waSimCevap').html('<span class="text-danger">Sunucuya ulaşılamadı.</span>'); });
    }
    $('#waSimBtn').on('click', simule);
    $(document).on('click', '.wa-sim-secim', function () { $('#waSimMesaj').val($(this).data('komut')); simule(); });
    $('#waSimMesaj').on('keydown', function (e) { if (e.key === 'Enter') { e.preventDefault(); simule(); } });
})();
