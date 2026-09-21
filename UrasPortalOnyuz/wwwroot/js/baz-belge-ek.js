/* Baz belgeden kopyalama — formdaki alanları baz belgeyle BİREBİR doldurur (fatura ekranları 109 / 111 / 114 / 116).
   Sunucu GetBaseDocument yanıtına "ek" koyar: { baslik: { DocDate, TaxDate, DocDueDate, DocRate, DocCur, NumAtCard, WTCode, U_* ... },
                                                 satirlar: { "<LineNum>": { wtLiable } } }
   Formda karşılığı OLMAYAN U_ alanları kayıt sırasında sunucuda eklenir (Services/BazBelgeUdf.cs), burada yalnızca görünen alanlar doldurulur.

   bazEkBaslik(ek, ekranYolu)   -> ilk baz belge bağlandıktan hemen sonra (bindBaseDocToForm sonrası)
   bazEkSatir(ek, baseEntry)    -> her baz belgenin satırları eklendikten sonra (çoklu kopyada her belge için)            */
(function () {
    'use strict';
    function val(o, k) { if (!o) return undefined; if (o[k] !== undefined) return o[k]; var kk = Object.keys(o).filter(function (x) { return x.toLowerCase() === k.toLowerCase(); })[0]; return kk ? o[kk] : undefined; }
    function bos(v) { return v === undefined || v === null || String(v).trim() === ''; }
    function tarih(v) { if (bos(v)) return ''; var s = String(v); var m = s.match(/^(\d{4}-\d{2}-\d{2})/); return m ? m[1] : ''; }
    function saat(v) {   // SAP saat alanı: 1430 / "14:30:00" -> "14:30"
        if (bos(v)) return ''; var s = String(v).trim();
        if (/^\d{1,2}:\d{2}/.test(s)) return s.substr(0, 5).padStart(5, '0');
        var n = parseInt(s, 10); if (isNaN(n)) return ''; return String(Math.floor(n / 100)).padStart(2, '0') + ':' + String(n % 100).padStart(2, '0');
    }
    function yaz(sel, v) { var $e = $(sel); if (!$e.length || bos(v)) return false; $e.val(v); return true; }

    function secKoy(sel, v, tetikle) {   // yalnizca listede varsa sec
        var $e = $(sel); if (!$e.length) return false;
        var s = v === undefined || v === null ? '' : String(v);
        if (!$e.find('option').filter(function () { return this.value === s; }).length) return false;
        $e.val(s); $e.trigger(tetikle ? 'change' : 'change.select2'); return true;
    }

    window.bazEkBaslik = function (ek, ekranYolu, kopya) {
        var b = ek && ek.baslik; if (!b) return;
        if (!kopya) window.kopyaKaynak = null;
        try {
            // Sahip (calisan) ve satis calisani
            var sahip = parseInt(val(b, 'OwnerCode'), 10); if (sahip > 0) secKoy('#OwnerCode', sahip);
            if (kopya) {
                var slp = parseInt(val(b, 'SlpCode'), 10); secKoy('#SalesPersonCode', slp > 0 ? slp : '');
                // Ayni tur belge cogaltiliyor: fatura tipi / muafiyet / islem tipi / aciklama birebir (bos ise bos)
                secKoy('#U_BE1_AKTAR', val(b, 'U_BE1_AKTAR') || '');
                secKoy('#U_BE1_SEND', val(b, 'U_BE1_SEND') || '', true);
                secKoy('#U_BE1_MUAFCODE', val(b, 'U_BE1_MUAFCODE') || '');
                if ($('#Comments').length) $('#Comments').val(val(b, 'Comments') || '');
            }
            // Tarihler ve kur: baz belgedeki değerler
            yaz('#DocDate', tarih(val(b, 'DocDate')));
            yaz('#TaxDate', tarih(val(b, 'TaxDate')));
            yaz('#DocDueDate', tarih(val(b, 'DocDueDate')));
            var pb = val(b, 'DocCur'), kur = parseFloat(val(b, 'DocRate'));
            if (!bos(pb) && $('#DocCurrency').length) $('#DocCurrency').val(pb === '##' ? 'TRY' : pb);
            if (!isNaN(kur) && kur > 0 && $('#ExchangeRate').length) $('#ExchangeRate').val(kur.toLocaleString('tr-TR', { minimumFractionDigits: 4, maximumFractionDigits: 6 }));
            if (!bos(val(b, 'NumAtCard'))) yaz('#NumAtCard', val(b, 'NumAtCard'));

            // Şoför / araç / firma (listede kayıtlı şoförse listeden seçilir, değilse alanlar tek tek)
            var sofor = val(b, 'U_BE1_SOFORSECIMI');
            var $sl = $('#DriverListSelect');
            if ($sl.length && !bos(sofor) && $sl.find('option[value="' + String(sofor).replace(/"/g, '') + '"]').length) {
                $sl.val(sofor).trigger('change');   // ekranin sofor secimi alanlari listeden doldurur
            } else {
                if ($sl.length) $sl.val('').trigger('change.select2');
                yaz('#U_BE1_SRCNAME', val(b, 'U_BE1_SOFORADSOYAD'));
                yaz('#U_BE1_SRCPLAKA', val(b, 'U_BE1_ARACPLAKASI') || val(b, 'U_BE1_SOFORPLAKA'));
                yaz('#U_BE1_SRCTITLE', val(b, 'U_BE1_SOFORUNVANI'));
                yaz('#U_BE1_SRCVKN', val(b, 'U_BE1_SOFORKIMLIK'));
                yaz('#U_BE1_SRCTEL', val(b, 'U_BE1_SOFORTEL'));
                yaz('#U_BE1_FIRMANO', val(b, 'U_BE1_FRMNO'));
            }
            yaz('#U_BE1_FIRMAADI', val(b, 'U_BE1_FRMADI'));
            yaz('#U_BE1_FIRMAVKN', val(b, 'U_BE1_FRMVKN'));
            yaz('#U_BE1_NAKLIYETARIHI', tarih(val(b, 'U_BE1_NAKLIYETARIHI')));
            yaz('#U_BE1_NAKLIYESAATI', saat(val(b, 'U_BE1_NAKLIYESAATI')));

            // İade referansı (iade faturası ekranı): baz belgede varsa o; baz satınalma faturasıysa o faturanın numarası ve belge tarihi
            if ($('#IadeFaturaNo').length) {
                var iNo = val(b, 'U_BE1_IADEFAT'), iTar = val(b, 'U_BE1_IADEDATE');
                if (bos(iNo) && window.aktifBazBelge && String(window.aktifBazBelge.type) === '18') {
                    var ref = String(val(b, 'NumAtCard') || '').trim();
                    if (ref.length === 16) { iNo = ref; iTar = val(b, 'TaxDate') || val(b, 'DocDate'); }
                }
                yaz('#IadeFaturaNo', iNo); yaz('#IadeFaturaTarihi', tarih(iTar));
            }
        } catch (e) { console.error('bazEkBaslik', e); }

        // Stopaj kodu (satınalma ekranları): cari kartındaki kod listesi yüklenir, baz belgedeki kod seçilir
        var $wt = $('#StopajKodu');
        var kart = $('#CardCode').val();
        if ($wt.length && kart && ekranYolu) {
            $.get(ekranYolu + '/GetCariStopajKodlari', { cardCode: kart }).done(function (res) {
                var kodlar = (res && res.data) || [];
                var opts = '<option value="">- Seçiniz -</option>';
                kodlar.forEach(function (k) { opts += '<option value="' + k.code + '">' + k.code + ' - ' + (k.name || '') + '</option>'; });
                var bazKod = val(b, 'WTCode');
                if (!bos(bazKod) && !kodlar.some(function (k) { return k.code === bazKod; })) opts += '<option value="' + bazKod + '">' + bazKod + ' (baz belgeden)</option>';
                $wt.html(opts);
                $wt.val(!bos(bazKod) ? bazKod : (kodlar.length === 1 ? kodlar[0].code : ''));
                // ekranin kendi dinleyicileri: satir stopaj isareti -> stopaj alani acik/kapali, kod degisimi -> toplamlar
                $('#linesBody tr .input-wt').first().trigger('change');
                if (!bos(bazKod) || kodlar.length === 1) $wt.val(!bos(bazKod) ? bazKod : kodlar[0].code);
                $wt.trigger('change');
            });
        }
    };

    window.bazEkSatir = function (ek, baseEntry) {
        var s = ek && ek.satirlar; if (!s) return;
        var kopya = baseEntry === 'kopya';
        $('#linesBody tr').each(function () {
            var $tr = $(this);
            if (!kopya && String($tr.attr('data-bentry')) !== String(baseEntry)) return;
            var no = kopya ? $tr.attr('data-kline') : $tr.attr('data-bline');
            var d = no === undefined ? null : s[String(no)]; if (!d) return;
            var $wt = $tr.find('.input-wt');
            if ($wt.length && d.wtLiable !== undefined) $wt.val(d.wtLiable ? 'Y' : 'N');
        });
        $('#linesBody tr .input-wt').first().trigger('change');   // ekranin stopaj durumu + toplam dinleyicisi
    };

    /**
     * "Cogalt": gorunen belgenin birebir kopyasi. Satirlar kaynak belgenin satir sirasiyla (LineNum) eslenir;
     * kayitta sunucu formda olmayan UDF'leri de bu kaynaktan kopyalar (KopyaKaynak + KopyaSatir).
     */
    window.bazEkKopya = function (ek, ekranYolu, kaynakEntry) {
        var s = (ek && ek.satirlar) || {};
        var nolar = Object.keys(s).map(Number).sort(function (a, b) { return a - b; });
        $('#linesBody tr').each(function (i) { if (nolar[i] !== undefined) $(this).attr('data-kline', nolar[i]); });
        window.bazEkSatir(ek, 'kopya');
        window.bazEkBaslik(ek, ekranYolu, true);
        window.kopyaKaynak = kaynakEntry;
    };

    // Yeni belge / baz belge / bul moduna gecince cogaltma kaynagi unutulur
    $(document).on('click', '#btnNewDoc, #btnBaseDoc, #btnFindMode', function () { window.kopyaKaynak = null; });
})();
