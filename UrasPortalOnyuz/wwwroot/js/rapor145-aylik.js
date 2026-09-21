// Rapor145 — Aylık Karşılaştırma sekmesi (irsaliye↔irsaliye, fatura↔fatura; tutar + kalem kıyası)
(function () {
    var veri = null, tip = '15', seciliAy = '', yuklendi = false;
    var kac = function (s) { return $('<div>').text(s == null ? '' : String(s)).html(); };
    var para = function (v, pb) { if (v == null) return '-'; return Number(v).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + (pb ? ' ' + pb : ''); };
    var sayi = function (v) { return v == null ? '-' : Number(v).toLocaleString('tr-TR', { maximumFractionDigits: 3 }); };
    var tarih = function (s) { if (!s) return ''; var d = new Date(s); return isNaN(d) ? String(s).substring(0, 10) : d.toLocaleDateString('tr-TR'); };
    var ayKodu = function (s) { var d = new Date(s); return isNaN(d) ? String(s).substring(0, 7) : d.getFullYear() + '-' + ('0' + (d.getMonth() + 1)).slice(-2); };
    var AYLAR = ['Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran', 'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'];
    var ayAdi = function (k) { var p = k.split('-'); return AYLAR[parseInt(p[1], 10) - 1] + ' ' + p[0]; };

    // Belge durumu
    var tutarKiyasla = true;   // irsaliyelerde hedef tarafta fiyat olmadığı için tutar kıyaslanmaz (sunucu söyler)
    function durum(r) {
        if (!r.hedefDocEntry) return { kod: 'eksik', etiket: 'Hedefte yok', sinif: 'bg-danger' };
        var tutarFark = tutarKiyasla && Math.abs((r.docTotal || 0) - (r.hedefTutar || 0)) > 0.05;
        var kalem = (r.kalemFark || 0) + (r.kalemEksik || 0) + (r.kalemFazla || 0);
        if (tutarFark && kalem) return { kod: 'fark', etiket: 'Tutar + kalem farkı', sinif: 'bg-warning text-dark' };
        if (tutarFark) return { kod: 'fark', etiket: 'Tutar farkı', sinif: 'bg-warning text-dark' };
        if (kalem) return { kod: 'fark', etiket: 'Kalem farkı', sinif: 'bg-warning text-dark' };
        return { kod: 'ok', etiket: 'Eşleşti', sinif: 'bg-success' };
    }

    function yukle() {
        tip = $('#ayTip').val();
        $('#loader-aylik').show(); $('#tbody-aylik-ozet').empty(); $('#tbody-aylik-detay').html('<tr><td colspan="9" class="bos-mesaj">Ay seçin</td></tr>');
        $.ajax({ url: '/Rapor145/GetAylikKarsilastirma', data: { objType: tip }, timeout: 300000 }).done(function (res) {
            $('#loader-aylik').hide();
            if (!res || !res.success) { $('#tbody-aylik-ozet').html('<tr><td colspan="9" class="bos-mesaj text-danger fw-bold">' + kac(res && res.message) + '</td></tr>'); return; }
            // Sunucu satırları sözlük olarak döner (DocDate, HedefDocNum …); JS tarafında camelCase kullanıyoruz
            var camel = function (o) { var r = {}; Object.keys(o || {}).forEach(function (k) { r[k.charAt(0).toLowerCase() + k.slice(1)] = o[k]; }); return r; };
            res.kaynak = (res.kaynak || []).map(camel); res.fazla = (res.fazla || []).map(camel);
            veri = res; yuklendi = true; tutarKiyasla = res.tutarKiyasla !== false;
            $('.ay-tutar-not').toggle(!tutarKiyasla);
            // Ay listesi
            var aylar = {};
            (veri.kaynak || []).forEach(function (r) { var a = ayKodu(r.docDate); (aylar[a] = aylar[a] || { k: [], f: [] }).k.push(r); });
            (veri.fazla || []).forEach(function (r) { var a = ayKodu(r.docDate); (aylar[a] = aylar[a] || { k: [], f: [] }).f.push(r); });
            veri.aylar = aylar;
            var $ay = $('#ayAy').empty().append('<option value="">Tüm aylar (özet)</option>');
            Object.keys(aylar).sort().reverse().forEach(function (a) { $ay.append($('<option>').val(a).text(ayAdi(a))); });
            var ayListesi = Object.keys(aylar).sort().reverse();
            if (seciliAy && aylar[seciliAy]) $ay.val(seciliAy);
            else { seciliAy = ayListesi.length ? ayListesi[0] : ''; $ay.val(seciliAy); }   // açılışta en son ay seçili gelir
            ozetCiz(); detayCiz();
        }).fail(function () { $('#loader-aylik').hide(); $('#tbody-aylik-ozet').html('<tr><td colspan="9" class="bos-mesaj text-danger fw-bold">Sunucuya ulaşılamadı.</td></tr>'); });
    }

    function ozetCiz() {
        if (!veri) return;
        var h = '', T = { ka: 0, kt: 0, ha: 0, ht: 0, eksik: 0, fazla: 0, fark: 0, ok: 0 };
        Object.keys(veri.aylar).sort().reverse().forEach(function (a) {
            var g = veri.aylar[a], ka = g.k.length, kt = 0, ha = 0, ht = 0, eksik = 0, fark = 0, ok = 0;
            g.k.forEach(function (r) { kt += r.docTotal || 0; var d = durum(r); if (d.kod === 'eksik') eksik++; else { ha++; ht += r.hedefTutar || 0; if (d.kod === 'ok') ok++; else fark++; } });
            g.f.forEach(function (r) { ha++; ht += r.docTotal || 0; });
            var fazla = g.f.length;
            T.ka += ka; T.kt += kt; T.ha += ha; T.ht += ht; T.eksik += eksik; T.fazla += fazla; T.fark += fark; T.ok += ok;
            var sorun = eksik + fazla + fark;
            h += '<tr class="ay-satir ' + (seciliAy === a ? 'table-primary' : '') + '" data-ay="' + a + '" style="cursor:pointer">'
                + '<td class="fw-bold">' + ayAdi(a) + '</td>'
                + '<td class="text-end">' + ka + '</td><td class="text-end">' + para(kt) + '</td>'
                + '<td class="text-end">' + ha + '</td><td class="text-end">' + para(ht) + '</td>'
                + '<td class="text-end ' + (tutarKiyasla && Math.abs(kt - ht) > 0.05 ? 'text-danger fw-bold' : 'text-muted') + '">' + (tutarKiyasla ? para(kt - ht) : '<span title="İrsaliyede hedefte fiyat yok">—</span>') + '</td>'
                + '<td class="text-center">' + (eksik ? '<span class="badge bg-danger">' + eksik + '</span>' : '<span class="text-muted">0</span>') + '</td>'
                + '<td class="text-center">' + (fazla ? '<span class="badge bg-secondary">' + fazla + '</span>' : '<span class="text-muted">0</span>') + '</td>'
                + '<td class="text-center">' + (fark ? '<span class="badge bg-warning text-dark">' + fark + '</span>' : '<span class="text-muted">0</span>') + '</td>'
                + '<td class="text-center">' + (sorun ? '<span class="badge bg-danger-subtle text-danger border border-danger">' + sorun + ' sorun</span>' : '<span class="badge bg-success">tamam</span>') + '</td>'
                + '</tr>';
        });
        h += '<tr class="table-light fw-bold"><td>Toplam</td><td class="text-end">' + T.ka + '</td><td class="text-end">' + para(T.kt) + '</td><td class="text-end">' + T.ha + '</td><td class="text-end">' + para(T.ht) + '</td>'
            + '<td class="text-end ' + (Math.abs(T.kt - T.ht) > 0.05 ? 'text-danger' : '') + '">' + para(T.kt - T.ht) + '</td><td class="text-center">' + T.eksik + '</td><td class="text-center">' + T.fazla + '</td><td class="text-center">' + T.fark + '</td><td class="text-center">' + (T.eksik + T.fazla + T.fark) + '</td></tr>';
        $('#tbody-aylik-ozet').html(h || '<tr><td colspan="10" class="bos-mesaj">Belge yok</td></tr>');
        $('#aylikOzetBaslik').text((veri.tipAdi || '') + ' · ' + Object.keys(veri.aylar).length + ' ay');
    }

    function detayCiz() {
        if (!veri) return;
        var sadeceSorun = $('#aySadeceSorun').is(':checked'), ara = ($('#ayAra').val() || '').toLowerCase();
        var aylar = seciliAy ? [seciliAy] : Object.keys(veri.aylar).sort().reverse();
        var h = '', n = 0;
        aylar.forEach(function (a) {
            var g = veri.aylar[a]; if (!g) return;
            var satirlar = [];
            g.k.forEach(function (r) { satirlar.push({ yon: 'k', r: r, d: durum(r), t: r.docDate }); });
            g.f.forEach(function (r) { satirlar.push({ yon: 'f', r: r, d: { kod: 'fazla', etiket: 'Kaynakta yok', sinif: 'bg-secondary' }, t: r.docDate }); });
            satirlar.sort(function (x, y) { return new Date(x.t) - new Date(y.t); });
            satirlar.forEach(function (s) {
                if (sadeceSorun && s.d.kod === 'ok') return;
                var r = s.r, metin = (r.docNum + ' ' + (r.hedefDocNum || '') + ' ' + (r.numAtCard || '')).toLowerCase();
                if (ara && metin.indexOf(ara) < 0) return;
                n++;
                if (s.yon === 'k') {
                    var kalem = (r.kalemFark || 0) + (r.kalemEksik || 0) + (r.kalemFazla || 0);
                    var etiket = (veri.tipAdi || '') + ' ' + r.docNum;
                    var secKutu = '<input type="checkbox" class="form-check-input ay-sec" data-yon="k" data-objtype="' + tip + '" data-k="' + r.docEntry + '" data-h="' + (r.hedefDocEntry || '') + '" data-durum="' + s.d.kod + '" data-etiket="' + kac(etiket) + '">';
                    var eylem = '';
                    if (!r.hedefDocEntry) eylem = '<div class="d-flex gap-1 justify-content-end flex-wrap"><button type="button" class="btn btn-sm btn-warning fw-bold ay-kuyruk" data-objtype="' + tip + '" data-k="' + r.docEntry + '" data-etiket="' + kac(etiket) + '" title="Kaynak belgeyi aktarım kuyruğuna al; servis hedefte oluşturur"><i class="fas fa-rotate me-1"></i>Kuyruğa Al</button>'
                        + '<button type="button" class="btn btn-sm btn-outline-success ay-onar" data-yon="k" data-k="' + r.docEntry + '" data-no="' + kac(r.docNum) + '" data-ref="' + kac(r.numAtCard || '') + '" data-tutar="' + (r.docTotal || 0) + '" data-tarih="' + kac(r.docDate) + '" title="Belge SELVI\'de var ama numarası yanlış girilmişse: doğru hedef belgeyi seç, Service Layer ile düzelt"><i class="fas fa-link me-1"></i>Onar / eşle</button></div>';
                    else eylem = '<div class="d-flex gap-1 justify-content-end flex-wrap"><button type="button" class="btn btn-sm btn-outline-primary ay-kalem" data-k="' + r.docEntry + '" data-h="' + r.hedefDocEntry + '" data-no="' + kac(r.docNum) + ' → ' + kac(r.hedefDocNum) + '"><i class="fas fa-list me-1"></i>Kalemler</button>'
                        + '<button type="button" class="btn btn-sm ' + (s.d.kod === 'fark' ? 'btn-danger' : 'btn-outline-danger') + ' ay-iptal" data-objtype="' + tip + '" data-k="' + r.docEntry + '" data-h="' + r.hedefDocEntry + '" data-etiket="' + kac(etiket + ' → ' + r.hedefDocNum) + '" data-yeniden="1" title="Hedefteki belgeyi SAP\'de iptal et, kaynağı yeniden kuyruğa al"><i class="fas fa-arrows-rotate me-1"></i>İptal & yeniden aktar</button></div>';
                    h += '<tr class="' + (s.d.kod === 'eksik' ? 'table-danger' : s.d.kod === 'fark' ? 'table-warning' : '') + '">'
                        + '<td class="text-center">' + secKutu + '</td>'
                        + '<td>' + tarih(r.docDate) + '</td>'
                        + '<td><b>' + kac(r.docNum) + '</b><div class="small text-muted">entry ' + kac(r.docEntry) + (r.numAtCard ? ' · ' + kac(r.numAtCard) : '') + '</div></td>'
                        + '<td class="text-end">' + para(r.docTotal, r.docCur) + '</td>'
                        + '<td>' + (r.hedefDocNum ? '<b>' + kac(r.hedefDocNum) + '</b><div class="small text-muted">' + tarih(r.hedefTarih) + ' · entry ' + kac(r.hedefDocEntry) + (r.eslesmeYolu === 'belgeno' ? ' · <span class="text-info">belge no ile</span>' : '') + '</div>' : '<span class="text-danger fw-bold">—</span>') + '</td>'
                        + '<td class="text-end">' + (r.hedefDocEntry ? para(r.hedefTutar, r.hedefPb) : '-') + '</td>'
                        + '<td class="text-end ' + (tutarKiyasla && r.hedefDocEntry && Math.abs((r.docTotal || 0) - (r.hedefTutar || 0)) > 0.05 ? 'text-danger fw-bold' : 'text-muted') + '">' + (r.hedefDocEntry && tutarKiyasla ? para((r.docTotal || 0) - (r.hedefTutar || 0)) : '-') + '</td>'
                        + '<td class="text-center">' + (r.hedefDocEntry ? (kalem ? '<span class="badge bg-warning text-dark" title="' + (r.kalemFark || 0) + ' farklı, ' + (r.kalemEksik || 0) + ' hedefte yok, ' + (r.kalemFazla || 0) + ' kaynakta yok">' + kalem + '</span>' : '<span class="text-success"><i class="fas fa-check"></i></span>') : '-') + '</td>'
                        + '<td><span class="badge ' + s.d.sinif + '">' + s.d.etiket + '</span></td>'
                        + '<td class="text-end">' + eylem + '</td>'
                        + '</tr>';
                } else {
                    var etiketF = (veri.tipAdi || '') + ' hedef ' + r.docNum;
                    h += '<tr class="table-secondary">'
                        + '<td class="text-center"><input type="checkbox" class="form-check-input ay-sec" data-yon="f" data-objtype="' + tip + '" data-k="" data-h="' + r.docEntry + '" data-durum="fazla" data-etiket="' + kac(etiketF) + '"></td>'
                        + '<td>' + tarih(r.docDate) + '</td><td><span class="text-muted">—</span></td><td></td>'
                        + '<td><b>' + kac(r.docNum) + '</b><div class="small text-muted">entry ' + kac(r.docEntry) + (r.numAtCard ? ' · ref ' + kac(r.numAtCard) : '') + (r.kaynakEntry ? ' · kaynak entry ' + kac(r.kaynakEntry) + ' (eşleşmiyor)' : '') + '</div></td>'
                        + '<td class="text-end">' + para(r.docTotal, r.docCur) + '</td><td></td><td></td>'
                        + '<td><span class="badge bg-secondary">Kaynakta yok</span></td>'
                        + '<td class="text-end"><div class="d-flex gap-1 justify-content-end flex-wrap"><button type="button" class="btn btn-sm btn-outline-success ay-onar" data-yon="f" data-h="' + r.docEntry + '" data-no="' + kac(r.docNum) + '" data-ref="' + kac(r.numAtCard || '') + '" data-tutar="' + (r.docTotal || 0) + '" data-tarih="' + kac(r.docDate) + '" title="Bu hedef belgeyi doğru kaynak belgeye bağla (Service Layer ile numarası düzeltilir)"><i class="fas fa-link me-1"></i>Kaynakla eşle</button>'
                        + '<button type="button" class="btn btn-sm btn-outline-danger ay-iptal" data-objtype="' + tip + '" data-k="" data-h="' + r.docEntry + '" data-etiket="' + kac(etiketF) + '" data-yeniden="0" title="Hedefteki belgeyi SAP\'de iptal et"><i class="fas fa-ban me-1"></i>Hedefi iptal et</button></div></td></tr>';
                }
            });
        });
        $('#tbody-aylik-detay').html(h || '<tr><td colspan="10" class="bos-mesaj">' + (sadeceSorun ? 'Sorunlu belge yok 🎉' : 'Belge yok') + '</td></tr>');
        $('#ayHepsi').prop('checked', false); secimSayac();
        $('#aylikDetayBaslik').text((seciliAy ? ayAdi(seciliAy) : 'Tüm aylar') + ' · ' + n + ' belge' + (seciliAy ? '' : ' (ay seçmek için yukarıdaki listeyi ya da özet tablodaki satırı kullanın)'));
    }

    // Kalem karşılaştırma penceresi
    $(document).on('click', '.ay-kalem', function () {
        var k = $(this).data('k'), hd = $(this).data('h');
        $('#ayKalemBaslik').text($(this).data('no'));
        $('#ayKalemGovde').html('<tr><td colspan="9" class="text-center py-4"><i class="fas fa-spinner fa-spin me-1"></i>Kalemler getiriliyor…</td></tr>');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('ayKalemModal')).show();
        $.ajax({ url: '/Rapor145/GetKalemKarsilastirma', data: { objType: tip, docEntry: k, hedefDocEntry: hd }, timeout: 120000 }).done(function (res) {
            if (!res || !res.success) { $('#ayKalemGovde').html('<tr><td colspan="9" class="text-danger text-center">' + kac(res && res.message) + '</td></tr>'); return; }
            var h = '', kt = 0, ht = 0;
            var yolAdi = { kod: 'URAS kodu', hedefkod: 'aynı kod', aciklama: 'açıklama', hizmet: 'hizmet sırası' };
            (res.satirlar || []).forEach(function (s) {
                var eksik = s.durum === 'eksik', fazla = s.durum === 'fazla', fark = s.durum === 'fark';
                var neden = s.farkNedeni || '';
                var mF = fark && neden.indexOf('miktar') >= 0, fF = fark && neden.indexOf('fiyat') >= 0, tF = fark && neden.indexOf('tutar') >= 0;
                kt += s.kTutar || 0; ht += s.hTutar || 0;
                var cls = eksik ? 'table-danger' : fazla ? 'table-secondary' : fark ? 'table-warning' : '';
                h += '<tr class="' + cls + '"><td><b>' + kac(s.itemCode) + '</b>' + (s.hedefKod && s.hedefKod !== s.itemCode ? '<div class="small text-muted">hedef: ' + kac(s.hedefKod) + '</div>' : '') + (s.eslesme && s.eslesme !== 'kod' ? '<div class="small text-info">eşleşme: ' + (yolAdi[s.eslesme] || s.eslesme) + '</div>' : '') + '</td><td>' + kac(s.ad) + '</td>'
                    + '<td class="text-end">' + sayi(s.kMiktar) + '</td><td class="text-end ' + (mF ? 'text-danger fw-bold' : '') + '">' + sayi(s.hMiktar) + '</td>'
                    + '<td class="text-end">' + para(s.kFiyat) + '</td><td class="text-end ' + (fF ? 'text-danger fw-bold' : '') + '">' + para(s.hFiyat) + '</td>'
                    + '<td class="text-end">' + para(s.kTutar) + '</td><td class="text-end ' + (tF ? 'text-danger fw-bold' : '') + '">' + para(s.hTutar) + '</td>'
                    + '<td>' + (eksik ? '<span class="badge bg-danger">Hedefte yok</span>' : fazla ? '<span class="badge bg-secondary">Kaynakta yok</span>' : fark ? '<span class="badge bg-warning text-dark">' + kac(neden) + '</span>' : '<span class="badge bg-success">aynı</span>') + '</td></tr>';
            });
            h += '<tr class="table-light fw-bold"><td colspan="6" class="text-end">Kalem toplamı</td><td class="text-end">' + para(kt) + '</td><td class="text-end ' + (Math.abs(kt - ht) > 0.05 ? 'text-danger' : '') + '">' + para(ht) + '</td><td></td></tr>';
            $('#ayKalemGovde').html(h);
        }).fail(function () { $('#ayKalemGovde').html('<tr><td colspan="9" class="text-danger text-center">Sunucuya ulaşılamadı.</td></tr>'); });
    });

    // ---- Müdahale: kuyruğa al / iptal et & yeniden aktar ----
    function secimSayac() {
        var $s = $('.ay-sec:checked'); var kuyruk = $s.filter('[data-durum="eksik"]').length, iptal = $s.filter('[data-h!=""]').length;
        $('#aySecimSayi').text($s.length ? $s.length + ' seçili' : '');
        $('#ayTopluKuyruk').prop('disabled', !kuyruk).find('span').text(kuyruk ? ' (' + kuyruk + ')' : '');
        $('#ayTopluIptal').prop('disabled', !iptal).find('span').text(iptal ? ' (' + iptal + ')' : '');
    }
    $(document).on('change', '.ay-sec', secimSayac);
    $('#ayHepsi').on('change', function () { $('.ay-sec').prop('checked', this.checked); secimSayac(); });
    function yenidenYukle() { yuklendi = false; yukle(); }

    // Tekil kuyruğa al: sayfadaki ortak kuyrugaGonder (onay + sonuç özeti) kullanılır
    $(document).on('click', '.ay-kuyruk', function () {
        var $b = $(this);
        kuyrugaGonder([{ ObjType: String($b.data('objtype')), DocEntry: String($b.data('k')), Etiket: $b.data('etiket') }], yenidenYukle);
    });
    $('#ayTopluKuyruk').on('click', function () {
        var paket = $('.ay-sec:checked[data-durum="eksik"]').map(function () { return { ObjType: String($(this).data('objtype')), DocEntry: String($(this).data('k')), Etiket: $(this).data('etiket') }; }).get();
        if (paket.length) kuyrugaGonder(paket, yenidenYukle);
    });

    // İptal & yeniden aktar: onay penceresi
    var iptalPaket = [];
    function iptalPenceresi(paket) {
        iptalPaket = paket;
        var h = '';
        paket.forEach(function (p) { h += '<li><b>' + kac(p.Etiket) + '</b> — hedef entry ' + p.HedefDocEntry + (p.YenidenAktar ? ' · <span class="text-success">iptal sonrası kaynak ' + p.KaynakDocEntry + ' kuyruğa alınır</span>' : ' · <span class="text-danger">yalnızca iptal</span>') + '</li>'; });
        $('#ayIptalListe').html(h); $('#ayIptalSonuc').addClass('d-none').empty(); $('#ayIptalOnay').prop('checked', false); $('#ayIptalBtn').prop('disabled', true).show();
        $('#ayIptalSayi').text(paket.length);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('ayIptalModal')).show();
    }
    $(document).on('click', '.ay-iptal', function () {
        var $b = $(this);
        iptalPenceresi([{ ObjType: String($b.data('objtype')), HedefDocEntry: parseInt($b.data('h'), 10), KaynakDocEntry: $b.data('k') === '' ? null : parseInt($b.data('k'), 10), YenidenAktar: String($b.data('yeniden')) === '1', Etiket: $b.data('etiket') }]);
    });
    $('#ayTopluIptal').on('click', function () {
        var paket = $('.ay-sec:checked[data-h!=""]').map(function () { var k = $(this).data('k'); return { ObjType: String($(this).data('objtype')), HedefDocEntry: parseInt($(this).data('h'), 10), KaynakDocEntry: k === '' ? null : parseInt(k, 10), YenidenAktar: k !== '', Etiket: $(this).data('etiket') }; }).get();
        if (paket.length) iptalPenceresi(paket);
    });
    $('#ayIptalOnay').on('change', function () { $('#ayIptalBtn').prop('disabled', !this.checked); });
    $('#ayIptalBtn').on('click', function () {
        var $b = $(this).prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>SAP\'de iptal ediliyor…');
        var stokTamamla = $('#ayIptalStok').is(':checked');
        iptalPaket.forEach(function (p) { p.StokTamamla = stokTamamla; });
        $.ajax({ url: '/Rapor145/HedefIptalVeYenidenAktar', type: 'POST', contentType: 'application/json', data: JSON.stringify(iptalPaket), timeout: 600000 }).done(function (res) {
            var liste = (res && res.data) || [], h = '';
            if (!res || !res.success) h += '<div class="alert alert-danger mb-2">' + kac(res && res.message) + '</div>';
            liste.forEach(function (x) { h += '<div class="d-flex gap-2 align-items-start mb-1"><span class="badge ' + (x.basarili ? 'bg-success' : 'bg-danger') + '">' + (x.basarili ? 'OK' : 'HATA') + '</span><div><b>' + kac(x.etiket) + '</b><div class="small">' + kac(x.mesaj) + '</div></div></div>'; });
            $('#ayIptalSonuc').removeClass('d-none').html(h || '<div class="text-muted">Sonuç yok.</div>');
            $b.hide(); yenidenYukle();
        }).fail(function () { $('#ayIptalSonuc').removeClass('d-none').html('<div class="alert alert-danger">Sunucuya ulaşılamadı.</div>'); $b.prop('disabled', false).html('<i class="fas fa-ban me-1"></i>İptal et'); });
    });

    // ---- Onar / eşle: yanlış numaralı hedef belgeyi doğru kaynağa bağla ----
    var onarSabit = null;   // { yon: 'k'|'f', entry, no, ref, tutar, tarih }
    function onarAdaylar() {
        // sabit taraf kaynaksa adaylar: hedefte olup kaynakta olmayanlar; sabit taraf hedefse adaylar: hedefi olmayan kaynaklar
        var liste = onarSabit.yon === 'k'
            ? (veri.fazla || []).map(function (r) { return { entry: r.docEntry, no: r.docNum, ref: r.numAtCard || '', tutar: r.docTotal || 0, tarih: r.docDate, ek: r.kaynakEntry ? 'kaynak entry ' + r.kaynakEntry + ' (eşleşmiyor)' : '' }; })
            : (veri.kaynak || []).filter(function (r) { return !r.hedefDocEntry; }).map(function (r) { return { entry: r.docEntry, no: r.docNum, ref: r.numAtCard || '', tutar: r.docTotal || 0, tarih: r.docDate, ek: '' }; });
        var ara = ($('#ayOnarAra').val() || '').toLowerCase();
        if (ara) liste = liste.filter(function (a) { return (a.no + ' ' + a.ref + ' ' + a.entry).toLowerCase().indexOf(ara) >= 0; });
        liste.forEach(function (a) { a.tutarFark = Math.abs(a.tutar - onarSabit.tutar); a.gunFark = Math.abs((new Date(a.tarih) - new Date(onarSabit.tarih)) / 86400000); });
        liste.sort(function (x, y) { return (tutarKiyasla ? (x.tutarFark - y.tutarFark) : 0) || (x.gunFark - y.gunFark); });
        var h = '';
        liste.slice(0, 60).forEach(function (a) {
            var yakin = tutarKiyasla ? a.tutarFark <= 0.05 : a.gunFark <= 3;
            h += '<tr class="' + (yakin ? 'table-success' : '') + '" style="cursor:pointer"><td><input type="radio" name="ayOnarSecim" class="form-check-input" value="' + a.entry + '"></td>'
                + '<td>' + tarih(a.tarih) + '</td><td><b>' + kac(a.no) + '</b><div class="small text-muted">entry ' + a.entry + (a.ref ? ' · ref ' + kac(a.ref) : '') + (a.ek ? ' · ' + kac(a.ek) : '') + '</div></td>'
                + '<td class="text-end">' + para(a.tutar) + '</td><td class="text-end ' + (a.tutarFark > 0.05 ? 'text-danger' : 'text-success') + '">' + (tutarKiyasla ? para(a.tutarFark) : '-') + '</td><td class="text-end">' + Math.round(a.gunFark) + ' gün</td></tr>';
        });
        $('#ayOnarListe').html(h || '<tr><td colspan="6" class="text-center text-muted py-3">Aday belge yok (bu tipte ' + (onarSabit.yon === 'k' ? 'kaynakta olmayan hedef belge' : 'hedefi olmayan kaynak belge') + ' bulunmuyor).</td></tr>');
        $('#ayOnarBtn').prop('disabled', true);
    }
    $(document).on('click', '.ay-onar', function () {
        var $b = $(this);
        onarSabit = { yon: $b.data('yon'), entry: $b.data('yon') === 'k' ? $b.data('k') : $b.data('h'), no: $b.data('no'), ref: $b.data('ref'), tutar: parseFloat($b.data('tutar')) || 0, tarih: $b.data('tarih') };
        $('#ayOnarSabit').html((onarSabit.yon === 'k' ? '<span class="badge bg-primary me-1">Kaynak (URASKIMYA)</span>' : '<span class="badge bg-secondary me-1">Hedef (SELVI)</span>') + '<b>' + kac(onarSabit.no) + '</b> · ' + tarih(onarSabit.tarih) + ' · ' + para(onarSabit.tutar) + (onarSabit.ref ? ' · ref ' + kac(onarSabit.ref) : ''));
        $('#ayOnarAdayBaslik').text(onarSabit.yon === 'k' ? 'SELVI\'deki hangi belge bu belgenin karşılığı?' : 'URASKIMYA\'daki hangi belge bu belgenin kaynağı?');
        $('#ayOnarAra').val(''); $('#ayOnarSonuc').addClass('d-none').empty(); $('#ayOnarBtn').show();
        bootstrap.Modal.getOrCreateInstance(document.getElementById('ayOnarModal')).show();
        onarAdaylar();
    });
    $('#ayOnarAra').on('input', onarAdaylar);
    $(document).on('click', '#ayOnarListe tr', function (e) { if (!$(e.target).is('input')) $(this).find('input[type=radio]').prop('checked', true); $('#ayOnarBtn').prop('disabled', !$('input[name=ayOnarSecim]:checked').length); });
    $(document).on('change', 'input[name=ayOnarSecim]', function () { $('#ayOnarBtn').prop('disabled', false); });
    $('#ayOnarBtn').on('click', function () {
        var secilen = parseInt($('input[name=ayOnarSecim]:checked').val(), 10); if (!secilen) return;
        var k = onarSabit.yon === 'k' ? onarSabit.entry : secilen, hd = onarSabit.yon === 'k' ? secilen : onarSabit.entry;
        if (!confirm('SELVI\'deki hedef belge (entry ' + hd + ') Service Layer ile düzeltilecek:\n• U_KaynakDocEntry = ' + k + '\n• Belge no (NumAtCard) = kaynağın e-fatura / belge numarası\n\nDevam edilsin mi?')) return;
        var $b = $(this).prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Düzeltiliyor…');
        $.ajax({ url: '/Rapor145/HedefOnar', type: 'POST', contentType: 'application/json', data: JSON.stringify([{ ObjType: tip, KaynakDocEntry: k, HedefDocEntry: hd, Etiket: (veri.tipAdi || '') + ' ' + k + ' → ' + hd }]), timeout: 180000 }).done(function (res) {
            var liste = (res && res.data) || [], h = '';
            if (!res || !res.success) h += '<div class="alert alert-danger mb-2">' + kac(res && res.message) + '</div>';
            liste.forEach(function (x) { h += '<div class="alert ' + (x.basarili ? 'alert-success' : 'alert-danger') + ' mb-1 small"><b>' + kac(x.etiket) + '</b><br>' + kac(x.mesaj) + '</div>'; });
            $('#ayOnarSonuc').removeClass('d-none').html(h || '<div class="text-muted">Sonuç yok.</div>');
            $b.hide(); yenidenYukle();
        }).fail(function () { $('#ayOnarSonuc').removeClass('d-none').html('<div class="alert alert-danger">Sunucuya ulaşılamadı.</div>'); $b.prop('disabled', false).html('<i class="fas fa-link me-1"></i>Eşle ve düzelt'); });
    });

    $(document).on('shown.bs.tab', '#tab-aylik', function () { if (!yuklendi) yukle(); });
    $('#ayTip').on('change', function () { yuklendi = false; yukle(); });
    $('#ayAy').on('change', function () { seciliAy = this.value; ozetCiz(); detayCiz(); });
    $(document).on('click', '.ay-satir', function () { seciliAy = $(this).data('ay'); $('#ayAy').val(seciliAy); ozetCiz(); detayCiz(); document.getElementById('aylikDetayBaslik').scrollIntoView({ behavior: 'smooth', block: 'start' }); });
    $('#aySadeceSorun').on('change', detayCiz);
    $('#ayAra').on('input', detayCiz);
    $('#ayYenile').on('click', function () { yuklendi = false; yukle(); });
    window.aylikYenile = function () { if ($('#tab-aylik').hasClass('active')) { yuklendi = false; yukle(); } else yuklendi = false; };
})();
