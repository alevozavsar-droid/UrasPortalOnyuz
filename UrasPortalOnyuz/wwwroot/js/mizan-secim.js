// Mizan (Rapor28) açılır çoklu seçim yardımcıları — görünümden taşındı (CS8103 dize sınırı)
        // Açılır çoklu seçim (Ana Hesap / Detay Hesap / İşlem Tipi): seçilenler tek tek etiket olarak değil,
        // "N seçildi" rozetiyle özetlenir; listede seçili satır onay işaretiyle vurgulanır; üstte Hepsini Seç / Temizle.
        function initializeSelect2WithButtons(selector, placeholderText) {
            var $sel = $(selector);
            $sel.select2({
                theme: "bootstrap-5",
                placeholder: placeholderText,
                closeOnSelect: false,
                allowClear: false,
                width: '100%',
                dropdownCssClass: 'mz-acilir',
                language: { noResults: function () { return 'Sonuç bulunamadı'; }, searching: function () { return 'Aranıyor…'; } },
                templateResult: function (d) {
                    if (!d.id) return d.text;
                    var $o = $('<span class="mz-opt"><i class="fas fa-check"></i><span class="mz-opt-yazi"></span></span>');
                    $o.find('.mz-opt-yazi').text(d.text);
                    return $o;
                },
                templateSelection: function (d) { return d.text; }
            }).on('select2:open', function () {
                var $dropdown = $('.mz-acilir').last();
                if ($dropdown.find('.mz-eylem').length === 0) {
                    var $bar = $('<div class="mz-eylem"><button type="button" class="mz-eylem-btn hepsi"><i class="fas fa-check-double"></i> Hepsini Seç</button><button type="button" class="mz-eylem-btn temizle"><i class="fas fa-eraser"></i> Temizle</button><span class="mz-eylem-sayac"></span></div>');
                    $dropdown.prepend($bar);
                    $bar.find('.hepsi').on('click', function (e) { e.preventDefault(); $sel.find('option').prop('selected', true); $sel.trigger('change'); });
                    $bar.find('.temizle').on('click', function (e) { e.preventDefault(); $sel.val(null).trigger('change'); });
                }
                sayacGuncelle();
            }).on('change', function () { updateSelect2Placeholder(selector, placeholderText); sayacGuncelle(); });
            function sayacGuncelle() { $('.mz-acilir').last().find('.mz-eylem-sayac').text((($sel.val() || []).length) + ' / ' + $sel.find('option').length); }
            updateSelect2Placeholder(selector, placeholderText);
        }

        function updateSelect2Placeholder(selector, placeholderText) {
            var $sel = $(selector);
            var count = ($sel.val() || []).length;
            var $rendered = $sel.next('.select2-container').find('.select2-selection__rendered');
            $rendered.find('.mz-secim-ozet, .mz-secim-bos').remove();
            if (count === 0) {
                $rendered.prepend($('<li class="mz-secim-bos"></li>').text(placeholderText));
            } else {
                $rendered.prepend($('<li class="mz-secim-ozet"><i class="fas fa-check-circle me-1"></i><span></span></li>').find('span').text(count + ' seçildi').end());
            }
        }
