// Rapor79 sayfa betigi (Views/Rapor79/Index.cshtml icinden tasindi; Razor ifadesi icermez).
const csrfToken = $('input[name="__RequestVerificationToken"]').val();

// KAYIT KUYRUĞU (Sıfır veri kaybı için)
let saveQueue = [];
let isProcessingQueue = false;
let enqueueDebounceTimer = null;

$(document).ready(function () {

    // --- SATIR ÇEK SEÇİM MODALI ARAMA İŞLEMİ ---
    $('#satirCekSearchInput').on('keyup', function () {
        var value = $(this).val().toLowerCase();
        
        $("#satirCekBody tr").filter(function () {
            if ($(this).find('td').length > 1) {
                $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
            }
        });
    });

    $('#satirCekSecimModal').on('show.bs.modal', function () {
        $('#satirCekSearchInput').val('');
    });

    // 1. Sayfa yüklendiğinde hafızadaki sekmeyi otomatik aç.
    //    Toplam hesabından ÖNCE yapılır: hesaplama bir satırda hata verirse
    //    kullanıcı en azından firma tablosunu görür. Hafızadaki firma artık
    //    listede yoksa anahtar silinir ve karşılama ekranı kalır (boş sayfa yok).
    try {
        var kaydedilenSekme = localStorage.getItem('aktifFirmaSekmesi');
        if (kaydedilenSekme) {
            if ($(kaydedilenSekme).length > 0) {
                $('#welcomeMessage').hide();
                $('.company-section').hide();
                $(kaydedilenSekme).show();
                setTimeout(() => $('html, body').scrollTop($(kaydedilenSekme).offset().top - kaydirmaPayi()), 100);
            } else {
                localStorage.removeItem('aktifFirmaSekmesi');
                $('#welcomeMessage').show();
            }
        }
    } catch (e) { console.warn('Sekme geri yüklenemedi', e); }

    try { calculateAllTotals(); } catch (e) { console.error('Toplamlar hesaplanamadı', e); }

    // ---- MÜKERRER KARŞILAŞTIRMA (IT #66) ----
    var mkDurum = { dbName: null, docEntry: 0, digerler: '' };
    function mkPara(n) { return n == null ? '' : Number(n).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function mkTarih(s) { if (!s) return ''; var d = new Date(s); return isNaN(d) ? '' : d.toLocaleDateString('tr-TR'); }
    function mkEsc(s) { return $('<div>').text(s == null ? '' : String(s)).html(); }
    function mkKarar(db, docEntry, karar, asil, digerler) {
        var not = $('#mkNot').val().trim();
        fetch('/Rapor79/MukerrerKarar', { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken },
            body: JSON.stringify({ dbName: db, docEntry: docEntry, karar: karar, asilDocEntry: asil, digerler: digerler, not: not }) })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                if (!res.success) { toastr.error(res.message); return; }
                toastr.success(res.message);
                $('#mukerrerModal').modal('hide');
                setTimeout(function () { location.reload(); }, 700);
            }).catch(function (e) { toastr.error('Hata: ' + e); });
    }
    $(document).on('click', '.mukerrer-btn', function (e) {
        e.preventDefault(); e.stopPropagation();
        mkDurum = { dbName: $(this).data('dbname'), docEntry: parseInt($(this).data('docentry')), digerler: String($(this).data('digerler')) };
        $('#mkBody').html('<tr><td colspan="15" class="text-center text-muted py-3"><i class="fas fa-spinner fa-spin"></i> Satırlar okunuyor...</td></tr>');
        $('#mkNot').val('');
        $('#mukerrerModal').modal('show');
        fetch('/Rapor79/MukerrerDetay?dbName=' + encodeURIComponent(mkDurum.dbName) + '&docEntries=' + encodeURIComponent(mkDurum.docEntry + ',' + mkDurum.digerler))
            .then(function (r) { return r.json(); })
            .then(function (res) {
                if (!res.success) { $('#mkBody').html('<tr><td colspan="15" class="text-danger">' + mkEsc(res.message) + '</td></tr>'); return; }
                var kararlar = {}; (res.kararlar || []).forEach(function (k) { kararlar[k.docEntry] = k; });
                var satirlar = res.data || [];
                $('#mkBody').html(satirlar.map(function (s) {
                    var benim = s.docEntry === mkDurum.docEntry;
                    var odendi = String(s.durum || '').toUpperCase().indexOf('ÖDEND') >= 0;
                    var k = kararlar[s.docEntry];
                    var digerleri = satirlar.filter(function (x) { return x.docEntry !== s.docEntry; }).map(function (x) { return x.docEntry; });
                    var asil = digerleri.length ? digerleri[0] : null;
                    return '<tr class="' + (benim ? 'table-warning' : '') + '">'
                        + '<td class="fw-bold">#' + s.docEntry + (benim ? ' <span class="badge bg-warning text-dark">bu</span>' : '') + '</td>'
                        + '<td>' + mkEsc(s.faturaNo) + '</td><td>' + mkEsc(s.kurum) + '</td><td>' + mkEsc(s.urun) + '</td>'
                        + '<td>' + mkTarih(s.planlanan) + '</td><td class="text-end">' + mkPara(s.tutar) + ' ' + mkEsc(s.pb) + '</td><td class="text-end">' + mkPara(s.tutarTL) + '</td>'
                        + '<td>' + mkEsc(s.durum) + (s.mutabakat ? '<div class="text-muted" style="font-size:.66rem;">' + mkEsc(s.mutabakat) + '</div>' : '') + '</td><td>' + (s.odenmeTarihi ? mkTarih(s.odenmeTarihi) + ' / ' + mkPara(s.odenenTutar) : '—') + '</td>'
                        + '<td>' + mkEsc(s.kaynak) + '</td><td>' + mkEsc(s.satinalmaci) + '</td><td>' + mkEsc(s.kayitTipi) + (s.belgeTipi ? '<div class="text-muted" style="font-size:.66rem;">' + mkEsc(s.belgeTipi) + '</div>' : '') + '</td>'
                        + '<td style="white-space:normal; max-width:200px;">' + mkEsc(s.notlar) + '</td>'
                        + '<td>' + (k ? '<span class="badge ' + (k.karar === 'DEGIL' ? 'bg-success' : 'bg-secondary') + '">' + (k.karar === 'DEGIL' ? 'mükerrer değil' : 'mükerrer') + '</span><div class="text-muted" style="font-size:.66rem;">' + mkEsc(k.kullanici) + ' ' + mkTarih(k.tarih) + '</div>' : '') + '</td>'
                        + '<td class="text-nowrap">' + (odendi ? '<span class="text-muted small">ödenmiş, kapatılamaz</span>'
                            : '<button type="button" class="btn btn-sm btn-outline-danger mk-kapat" data-docentry="' + s.docEntry + '" data-asil="' + (asil || '') + '" title="Bu satırı mükerrer olarak kapat (durumu Mükerrer olur, listeden düşer)"><i class="fas fa-times"></i> Bu satır mükerrer</button>') + '</td>'
                        + '</tr>';
                }).join(''));
            }).catch(function (e) { $('#mkBody').html('<tr><td colspan="15" class="text-danger">Sunucuya ulaşılamadı: ' + mkEsc(e) + '</td></tr>'); });
    });
    $(document).on('click', '.mk-kapat', function () {
        var d = parseInt($(this).data('docentry')), asil = parseInt($(this).data('asil')) || null;
        if (!confirm('#' + d + ' satırı mükerrer olarak kapatılacak (durumu "Mükerrer" olur, listeden düşer). Asıl kayıt #' + asil + ' kalır. Devam edilsin mi?')) return;
        mkKarar(mkDurum.dbName, d, 'MUKERRER', asil, '');
    });
    $('#mkDegilBtn').on('click', function () { mkKarar(mkDurum.dbName, mkDurum.docEntry, 'DEGIL', null, mkDurum.digerler); });
    $(document).on('click', '.mukerrer-geri-btn', function (e) {
        e.preventDefault(); e.stopPropagation();
        if (!confirm('Bu satır için mükerrer uyarısı yeniden açılsın mı?')) return;
        mkKarar($(this).data('dbname'), parseInt($(this).data('docentry')), 'GERIAL', null, '');
    });

    // 2. Sekmeye tıklandığında hafızaya al
    $(document).on('click', '.hizli-git-btn', function(e) {
        e.preventDefault();
        var targetId = $(this).data('target');
        
        localStorage.setItem('aktifFirmaSekmesi', targetId);
        
        $('#welcomeMessage').hide();
        $('.company-section').hide();
        var targetDiv = $(targetId);
        if (targetDiv.length) {
            targetDiv.fadeIn(300);
            $('html, body').animate({ scrollTop: targetDiv.offset().top - kaydirmaPayi() }, 300);
        }
    });

    // SATIR SEÇİMİNDE TURUNCU-SARI RENK EKLEME/ÇIKARMA
    $(document).on('change', '.chk-row', function() {
        if ($(this).is(':checked')) {
            $(this).closest('tr').addClass('selected-row');
        } else {
            $(this).closest('tr').removeClass('selected-row');
        }
        updateGlobalSelectedTotal();
    });

    function loadSatinalmacilar(dbName, selectElement, selectedValue = null) {
        selectElement.empty().append('<option value="">Yükleniyor...</option>').prop('disabled', true);
        $.get('/Rapor79/GetSatinalmacilar', { dbName: dbName }, function(res) {
            selectElement.empty().append('<option value="">Seçiniz...</option>');
            if (res && res.length > 0) {
                res.forEach(function(item) {
                    selectElement.append(new Option(item, item));
                });
            }
            if(selectedValue) {
                if (selectElement.find("option[value='" + selectedValue + "']").length === 0) {
                    selectElement.append(new Option(selectedValue, selectedValue));
                }
                selectElement.val(selectedValue);
            }
            selectElement.prop('disabled', false);
        }).fail(function() {
            selectElement.empty().append('<option value="">Bağlantı Hatası!</option>');
        });
    }

    // TOPLU DEKONT YÜKLEME
    $(document).on('click', '.btn-toplu-dekont', function() {
        var dbName = $(this).data('dbname');
        var selectedRows = $('.chk-row[data-dbname="' + dbName + '"]:checked');
        
        if (selectedRows.length === 0) {
            toastr.warning('Lütfen dekont yüklemek için tablodan en az bir satır seçin.');
            return;
        }
        
        var docEntries = [];
        selectedRows.each(function() {
            docEntries.push($(this).data('docentry'));
        });

        $('#topluDekontUploadDbName').val(dbName);
        $('#topluDekontUploadDocEntries').val(docEntries.join(','));
        $('#topluSeciliSatirSayisi').text(selectedRows.length);
        $('#topluDekontDosya').val('');
        $('#topluSecilenMail').val('');
        $('#topluDekontUploadModal').modal('show');
    });
    
    $('#btnTopluDekontKaydet').click(function() {
        var fileInput = $('#topluDekontDosya')[0];
        if (fileInput.files.length === 0) {
            toastr.warning('Lütfen yüklenecek bir dosya seçin.');
            return;
        }

        var form = $('#topluDekontUploadForm')[0];
        var formData = new FormData(form);
        var btn = $(this);
        btn.html('<i class="fas fa-spinner fa-spin"></i> Yükleniyor...').prop('disabled', true);

        $.ajax({
            url: '/Rapor79/UploadTopluDekont',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': csrfToken },
            success: function(res) {
                btn.html('<i class="fas fa-save"></i> Yükle').prop('disabled', false);
                if (res.success) {
                    toastr.success(res.message);
                    $('#topluDekontUploadModal').modal('hide');
                    setTimeout(function() { location.reload(); }, 1000);
                } else {
                    toastr.error(res.message || "Dosya yüklenemedi.");
                }
            },
            error: function() {
                toastr.error("Ağ hatası, işlem gerçekleştirilemedi.");
                btn.html('<i class="fas fa-save"></i> Yükle').prop('disabled', false);
            }
        });
    });

    // TABLO OTOMATİK SIRALAMA MANTIĞI
    function reorderTableRows(tbody) {
        let rows = tbody.find('tr.data-row').toArray();
        
        rows.sort(function(a, b) {
            let getRank = (status) => {
                if (!status) return 3;
                status = status.toUpperCase();
                if (status.includes('ÖZEL ONAY')) return 1; // Yeni durum en üste yakın
                if (status.includes('PLANLANDI')) return 2;
                if (status.includes('BEKLİYOR')) return 3;
                if (status.includes('GECİKTİ')) return 4;
                if (status.includes('ÖDENDİ')) return 5;
                return 3;
            };

            let statusA = $(a).find('.durum-select').val() || '';
            let statusB = $(b).find('.durum-select').val() || '';
            let rankA = getRank(statusA);
            let rankB = getRank(statusB);

            if (rankA !== rankB) {
                return rankA - rankB;
            }

            let methodA = $(a).find('select[name="U_OdemeYontemi"]').val() || 'ZZZ';
            let methodB = $(b).find('select[name="U_OdemeYontemi"]').val() || 'ZZZ';
            if (methodA !== methodB) {
                return methodA.localeCompare(methodB);
            }

            let currA = $(a).find('select[name="U_ParaBirimi"]').val() || 'ZZZ';
            let currB = $(b).find('select[name="U_ParaBirimi"]').val() || 'ZZZ';
            if (currA !== currB) {
                return currA.localeCompare(currB);
            }

            if (rankA === 5) { 
                let dateA = new Date($(a).find('input[name="U_OdenmeTarihi"]').val() || '1900-01-01').getTime();
                let dateB = new Date($(b).find('input[name="U_OdenmeTarihi"]').val() || '1900-01-01').getTime();
                return dateB - dateA;
            } else { 
                let dateA = new Date($(a).find('input[name="U_PlanlananTarih"]').val() || '2099-12-31').getTime();
                let dateB = new Date($(b).find('input[name="U_PlanlananTarih"]').val() || '2099-12-31').getTime();
                return dateA - dateB;
            }
        });

        $.each(rows, function(index, row) {
            $(row).find('.row-number').text(index + 1); 
            tbody.append(row);
            let nextRow = $(row).next('.sub-detail-row');
            if (nextRow.length) {
                tbody.append(nextRow);
            }
        });
        
        tbody.append(tbody.find('.total-row'));
    }

    // ÇEK SEÇİM İŞLEMLERİ
    $(document).on('change', '.odeme-yontemi-select', function() {
        const row = $(this).closest('tr'); 

        if ($(this).val() === 'Çek' || $(this).val() === 'Senet') {
            const dbName = row.closest('tbody').data('dbname');
            const docEntry = row.data('docentry');

            $('#satirCekDbName').val(dbName);
            $('#satirCekRowDocEntry').val(docEntry);
            $('#satirCekFirmaAdi').text(dbName);
            $('#satirSeciliCekSayisi').text('0');
            $('#satirSeciliCekToplam').text('0,00 ₺');
            $('#chkAllSatirCek').prop('checked', false);

            $('#satirCekBody').html('<tr><td colspan="8" class="text-center text-muted"><i class="fas fa-spinner fa-spin"></i> Portföydeki belgeler getiriliyor...</td></tr>');
            $('#satirCekSecimModal').modal('show');

            $.ajax({
                url: '/Rapor79/GetPortfoyCekleriDetayli',
                type: 'GET',
                data: { dbName: dbName },
                success: function(res) {
                    if(res.success && res.data && res.data.length > 0) {
                        let html = '';
                        res.data.forEach(c => {
                            let vadeFormatli = c.vadeTarihi ? new Date(c.vadeTarihi).toLocaleDateString('tr-TR') : '-';
                            let subeGosterim = c.sube ? ' / ' + c.sube : '';
                            html += `<tr>
                                        <td class="text-center align-middle">
                                            <input type="checkbox" class="chk-satir-cek custom-checkbox" 
                                                data-tutar="${c.cekTutari}" 
                                                data-tutartl="${c.tlKarsiligi}"
                                                data-pb="${c.paraBirimi}"
                                                data-vade="${c.vadeTarihi}"
                                                data-cekno="${c.cekNumarasi}"
                                                value="${c.docEntry}">
                                        </td>
                                        <td class="fw-bold align-middle">${c.cekNumarasi}</td>
                                        <td class="align-middle">
                                            <span class="badge bg-secondary">${c.belgeTipi}</span><br>
                                            <small class="text-muted fw-bold">${c.islemTipiAdi || ''}</small>
                                        </td>
                                        <td class="align-middle">${c.bankaAdi}${subeGosterim}</td>
                                        <td class="align-middle">${c.cekKimdenGeldi} <br> <small class="text-muted">${c.asilBorclu}</small></td>
                                        <td class="align-middle">${vadeFormatli}</td>
                                        <td class="text-end num-input align-middle">${formatMoneyStr(c.cekTutari)} ${c.paraBirimi}</td>
                                        <td class="text-end fw-bold text-success align-middle">${formatMoneyStr(c.tlKarsiligi)} ₺</td>
                                    </tr>`;
                        });
                        $('#satirCekBody').html(html);
                    } else {
                        $('#satirCekBody').html('<tr><td colspan="8" class="text-center text-warning fw-bold py-4">Portföyde çek veya senet bulunamadı.</td></tr>');
                    }
                },
                error: function() {
                    $('#satirCekBody').html('<tr><td colspan="8" class="text-center text-danger py-4">Ağ hatası oluştu!</td></tr>');
                }
            });
        } else {
            enqueueSave(row); // Timeout yerine anında kuyruğa al
        }
    });

    $(document).on('change', '.chk-satir-cek', function() {
        let count = 0;
        let totalTL = 0;
        $('.chk-satir-cek:checked').each(function() {
            count++;
            totalTL += parseFloat($(this).data('tutartl')) || 0;
        });
        $('#satirSeciliCekSayisi').text(count);
        $('#satirSeciliCekToplam').text(formatMoneyStr(totalTL) + ' ₺');
    });

    $('#chkAllSatirCek').change(function() {
        $('.chk-satir-cek').prop('checked', $(this).is(':checked')).trigger('change');
    });

    $('#btnSatirCekOnayla').click(function() {
        const checked = $('.chk-satir-cek:checked');
        if(checked.length === 0) {
            toastr.warning('Lütfen en az bir belge seçin.');
            return;
        }

        const dbName = $('#satirCekDbName').val();
        const docEntry = $('#satirCekRowDocEntry').val();
        const actualRow = $(`.main-payment-table[data-dbname="${dbName}"] tr.data-row[data-docentry="${docEntry}"]`);

        let totalTutar = 0;
        let totalTL = 0;
        let firstPb = '';
        let firstVade = '';
        let notes = [];

        checked.each(function(i) {
            totalTutar += parseFloat($(this).data('tutar')) || 0;
            totalTL += parseFloat($(this).data('tutartl')) || 0;
            if(i === 0) {
                firstPb = $(this).data('pb');
                firstVade = $(this).data('vade');
            }
            notes.push($(this).data('cekno'));
        });

        actualRow.find('.tutar-input').val(totalTutar.toFixed(4));
        actualRow.find('.tutar-display').val(formatMoneyStr(totalTutar));
        
        actualRow.find('.tutartl-input').val(totalTL.toFixed(4));
        actualRow.find('.tutartl-display').val(formatMoneyStr(totalTL));
        
        actualRow.find('.para-birimi-select').val(firstPb || 'TRY');
        
        if(firstVade) {
            actualRow.find('input[name="U_PlanlananTarih"]').val(firstVade.split('T')[0]); 
        }
        
        let existingNote = actualRow.find('[name="U_Notlar"]').val();
        let newNote = `Belge(ler): ${notes.join(', ')}`;
        actualRow.find('[name="U_Notlar"]').val(existingNote ? existingNote + ' | ' + newNote : newNote);

        let calcKur = totalTutar > 0 ? (totalTL / totalTutar) : 1;
        actualRow.find('.kur-input').val(calcKur.toFixed(4));
        actualRow.find('.kur-display').val(formatRateStr(calcKur));

        $('#satirCekSecimModal').modal('hide');
        
        enqueueSave(actualRow);
        calculateAllTotals();
        toastr.success('Seçilen belge bilgileri satıra başarıyla uygulandı.');
    });


    $(document).on('click', '.btn-portfoy-cek-ekle', function() {
        var dbName = $(this).data('dbname');
        $('#portfoyCekDbName').text(dbName);
        $('#cekModalDbNameValue').val(dbName);
        $('#seciliCekSayisi').text('0');
        $('#chkAllPortfoyCek').prop('checked', false);
        
        $('#portfoyCekBody').html('<tr><td colspan="8" class="text-center text-muted"><i class="fas fa-spinner fa-spin"></i> Belgeler getiriliyor, lütfen bekleyin...</td></tr>');
        $('#portfoyCekEkleModal').modal('show');

        $.ajax({
            url: '/Rapor79/GetPortfoyCekleriDetayli',
            type: 'GET',
            data: { dbName: dbName },
            success: function(res) {
                if(res.success && res.data && res.data.length > 0) {
                    var html = '';
                    res.data.forEach(function(c) {
                        let vadeFormatli = c.vadeTarihi ? new Date(c.vadeTarihi).toLocaleDateString('tr-TR') : '-';
                        let subeGosterim = c.sube ? ' / ' + c.sube : '';
                        
                        html += `<tr>
                            <td class="text-center align-middle">
                                <input type="checkbox" class="chk-portfoy-cek custom-checkbox" value="${c.docEntry}">
                            </td>
                            <td class="fw-bold align-middle">${c.cekNumarasi}</td>
                            <td class="align-middle">
                                <span class="badge bg-secondary">${c.belgeTipi}</span><br>
                                <small class="text-muted fw-bold">${c.islemTipiAdi || ''}</small>
                            </td>
                            <td class="align-middle">${c.bankaAdi}${subeGosterim}</td>
                            <td class="align-middle">${c.cekKimdenGeldi} <br> <small class="text-muted">${c.asilBorclu}</small></td>
                            <td class="align-middle">${vadeFormatli}</td>
                            <td class="text-end num-input align-middle">${formatMoneyStr(c.cekTutari)} ${c.paraBirimi}</td>
                            <td class="text-end fw-bold text-success align-middle">${formatMoneyStr(c.tlKarsiligi)} ₺</td>
                        </tr>`;
                    });
                    $('#portfoyCekBody').html(html);
                } else {
                    $('#portfoyCekBody').html('<tr><td colspan="8" class="text-center text-warning fw-bold py-4">Portföde bekleyen belge bulunamadı.</td></tr>');
                }
            },
            error: function() {
                $('#portfoyCekBody').html('<tr><td colspan="8" class="text-center text-danger py-4">Belgeler çekilirken bir ağ hatası oluştu!</td></tr>');
            }
        });
    });

    $(document).on('change', '.chk-portfoy-cek', function() {
        $('#seciliCekSayisi').text($('.chk-portfoy-cek:checked').length);
    });

    $('#chkAllPortfoyCek').change(function() {
        var isChecked = $(this).is(':checked');
        $('.chk-portfoy-cek').prop('checked', isChecked);
        $('#seciliCekSayisi').text($('.chk-portfoy-cek:checked').length);
    });

    $('#btnPortfoyCekKaydet').click(function() {
        var dbName = $('#cekModalDbNameValue').val();
        var selectedIds = [];
        
        $('.chk-portfoy-cek:checked').each(function() {
            selectedIds.push(parseInt($(this).val()));
        });

        if(selectedIds.length === 0) {
            toastr.warning('Lütfen akışa eklenecek en az bir belge seçin.');
            return;
        }

        var btn = $(this);
        btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Kaydediliyor...');

        $.ajax({
            url: '/Rapor79/TopluCekleriAkisaEkle',
            type: 'POST',
            contentType: 'application/json',
            headers: { 'RequestVerificationToken': csrfToken },
            data: JSON.stringify({ DbName: dbName, SecilenCekDocEntryListesi: selectedIds }),
            success: function(res) {
                if(res.success) {
                    toastr.success(res.message);
                    $('#portfoyCekEkleModal').modal('hide');
                    
                    setTimeout(function() { 
                        location.reload(); 
                    }, 1000);
                } else {
                    toastr.error(res.message || "Kaydedilirken bir hata oluştu.");
                    btn.prop('disabled', false).html('<i class="fas fa-save"></i> Seçilenleri Tabloya Ekle');
                }
            },
            error: function() {
                toastr.error('Ağ hatası oluştu, işlem gerçekleştirilemedi.');
                btn.prop('disabled', false).html('<i class="fas fa-save"></i> Seçilenleri Tabloya Ekle');
            }
        });
    });

    // Ödeme yöntemi süzmesi: yan yana düğmeler, seçili olan vurgulanır.
    $(document).on('click', '.odeme-filter-btn', function (e) {
        e.preventDefault();
        var dugme = $(this);
        var method = dugme.data('method');
        var dbName = dugme.data('dbname');

        dugme.closest('.odeme-yontem-grubu').find('.odeme-filter-btn').removeClass('aktif');
        dugme.addClass('aktif');

        $('.main-payment-table[data-dbname="' + dbName + '"] tbody.sortable-body tr.data-row').each(function () {
            var rowMethod = $(this).find('select[name="U_OdemeYontemi"]').val() || "";
            if (method === 'Tümü' || rowMethod === method) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    });

    // Her yöntemin yanina o sirketteki kayit sayisi yazilir; bos olanlar soluk.
    window.odemeYontemSayilariniGuncelle = function () {
        $('.odeme-yontem-grubu').each(function () {
            var grup = $(this);
            var dbName = grup.data('dbname');
            var satirlar = $('.main-payment-table[data-dbname="' + dbName + '"] tbody.sortable-body tr.data-row');

            grup.find('.odeme-filter-btn').each(function () {
                var d = $(this);
                var method = d.data('method');
                var adet = (method === 'Tümü')
                    ? satirlar.length
                    : satirlar.filter(function () {
                          return ($(this).find('select[name="U_OdemeYontemi"]').val() || "") === method;
                      }).length;

                d.find('.sayi').text(adet);
                d.toggleClass('bos', adet === 0 && method !== 'Tümü');
            });
        });
    };

    odemeYontemSayilariniGuncelle();

    $(document).on('change', 'select[name="U_OdemeYontemi"]', function () {
        odemeYontemSayilariniGuncelle();
    });

    $(document).on('click', '.pdf-preview-btn', function(e) {
        e.preventDefault();
        var rawUrl = $(this).data('url');
        
        if (rawUrl) {
            var cleanUrl = String(rawUrl).trim().replace(/\\/g, '/');
            var fileName = cleanUrl.substring(cleanUrl.lastIndexOf('/') + 1);
            
            var finalUrl = '';
            var title = $(this).attr('title') || '';
            
            if (fileName.toLowerCase().includes('talimat') || title.toLowerCase().includes('talimat')) {
                finalUrl = '/Talimatlar/' + fileName;
            } else {
                finalUrl = '/Dekontlar/' + fileName;
            }
            
            // Sayfa yakinlastirmasi (CSS zoom) altinda Chrome'un PDF
            // goruntuleyicisi cerceve icinde bos kalabiliyor. Onizleme
            // penceresine kabuk zoom'unun tersi verilir; boylece pencerenin
            // etkin zoom'u 1 olur ve belge normal boyutta cizilir.
            var kabukZoom = (window.SUNUCU_ZOOM || 100) / 100;
            var onizlemePencere = document.getElementById('pdfPreviewModal');
            if (onizlemePencere) onizlemePencere.style.zoom = kabukZoom !== 1 ? String(1 / kabukZoom) : '';

            // Dosya sunucuda yoksa (ornegin yerel gelistirme ortaminda,
            // sunucuya sonradan yuklenen dekontlar bu makinede bulunmaz)
            // iframe sessizce bos kalir. Once varligi kontrol edilip
            // kullaniciya acik bir mesaj verilir.
            fetch(finalUrl, { method: 'HEAD', cache: 'no-store' })
                .then(function (r) {
                    if (!r.ok) {
                        toastr.error('Belge sunucuda bulunamadı: ' + fileName + ' (HTTP ' + r.status + ')', 'Önizleme', { timeOut: 8000 });
                        return;
                    }
                    $('#pdfPreviewIframe').attr('src', finalUrl);
                    $('#modalDownloadBtn').attr('href', finalUrl);
                    $('#modalYeniSekmeBtn').attr('href', finalUrl);
                    $('#pdfPreviewModal').modal('show');
                })
                .catch(function () {
                    // Kontrol yapilamazsa eski davranis: dogrudan ac.
                    $('#pdfPreviewIframe').attr('src', finalUrl);
                    $('#modalDownloadBtn').attr('href', finalUrl);
                    $('#modalYeniSekmeBtn').attr('href', finalUrl);
                    $('#pdfPreviewModal').modal('show');
                });
        }
    });

    $('#pdfPreviewModal').on('hidden.bs.modal', function () {
        $('#pdfPreviewIframe').attr('src', '');
    });

    function parseMoneyToFloat(str) {
        if (!str && str !== 0) return 0;
        var safeStr = str.toString().replace(/\./g, '').replace(',', '.');
        return parseFloat(safeStr) || 0;
    }

    function formatMoneyStr(val) {
        if (!val && val !== 0) return '';
        return parseFloat(val).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function formatRateStr(val) {
        if (!val && val !== 0) return '';
        return parseFloat(val).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 });
    }

    function updateGlobalSelectedTotal() {
        let total = 0;
        $('.chk-row:checked').each(function() {
            total += parseFloat($(this).data('tutartl')) || 0;
        });
        $('#globalSelectedTotal').text('₺' + formatMoneyStr(total));
    }

    function updateGlobalSelectedBakiyeTotal() {
        let totalBakiye = 0;
        $('.chk-bakiye:checked').each(function() {
            totalBakiye += parseFloat($(this).data('tutartl')) || 0;
        });
        $('#globalSelectedBakiyeTotal').text('₺' + formatMoneyStr(totalBakiye));
    }

    $(document).on('change', '.chk-row, .chk-all-company', function() { updateGlobalSelectedTotal(); });
    $(document).on('change', '.chk-bakiye', function() { updateGlobalSelectedBakiyeTotal(); });
    $(document).on('change', '.chk-all-bakiye', function() {
        const dbName = $(this).data('dbname');
        const isChecked = $(this).is(':checked');
        $('.summary-table[data-dbname="'+dbName+'"] .chk-bakiye').prop('checked', isChecked);
        updateGlobalSelectedBakiyeTotal();
    });

    $(document).on('change', '.chk-all-company', function() {
        var isChecked = $(this).is(':checked');
        var table = $(this).closest('table');
        table.find('.chk-row').prop('checked', isChecked).trigger('change');
    });

    function fetchSystemRateAndUpdateRow(row) {
        var dbName = row.closest('tbody').data('dbname');
        var pb = row.find('.para-birimi-select').val();
        var tutar = parseFloat(row.find('.tutar-input').val()) || 0;

        if (pb === 'TRY' || !pb) {
            row.find('.kur-display').val('1,0000');
            row.find('.kur-input').val('1');
            
            var tTL = tutar * 1;
            row.find('.tutartl-display').val(formatMoneyStr(tTL));
            row.find('.tutartl-input').val(tTL.toFixed(4));
            
            calculateAllTotals();
            updateGlobalSelectedTotal();
            enqueueSave(row);
        } else {
            $.get('/Rapor79/GetGuncelKur', { dbName: dbName, pb: pb }, function(rate) {
                row.find('.kur-display').val(formatRateStr(rate));
                row.find('.kur-input').val(rate.toFixed(4));
                
                var tTL = tutar * rate;
                row.find('.tutartl-display').val(formatMoneyStr(tTL));
                row.find('.tutartl-input').val(tTL.toFixed(4));
                
                calculateAllTotals();
                updateGlobalSelectedTotal();
                enqueueSave(row);
            });
        }
    }

    function recalculateRowTL(row) {
        var tutar = parseFloat(row.find('.tutar-input').val()) || 0;
        var kur = parseFloat(row.find('.kur-input').val()) || 1;
        var tTL = tutar * kur;
        row.find('.tutartl-display').val(formatMoneyStr(tTL));
        row.find('.tutartl-input').val(tTL.toFixed(4));
        calculateAllTotals();
        updateGlobalSelectedTotal();
        enqueueSave(row);
    }

    $(document).on('change', '.para-birimi-select', function() {
        fetchSystemRateAndUpdateRow($(this).closest('tr'));
    });

    $(document).on('blur', '.format-money, .format-rate', function() {
        var valNum = parseMoneyToFloat($(this).val());
        if (isNaN(valNum) || $(this).val() === '') {
            $(this).val('');
            if($(this).siblings('input[type="hidden"]').length) $(this).siblings('input[type="hidden"]').val('');
        } else {
            if($(this).hasClass('format-rate')){
                $(this).val(formatRateStr(valNum));
            } else {
                $(this).val(formatMoneyStr(valNum));
            }
            if($(this).siblings('input[type="hidden"]').length) $(this).siblings('input[type="hidden"]').val(valNum.toFixed(4));
        }
        
        if ($(this).hasClass('tutar-display') || $(this).hasClass('kur-display')) {
            recalculateRowTL($(this).closest('tr')); 
        } else {
            calculateAllTotals();
            updateGlobalSelectedTotal();
            enqueueSave($(this).closest('tr'));
        }
    });

    $(document).on('focus', '.format-money, .format-rate', function() {
        var hiddenVal = $(this).siblings('input[type="hidden"]').length ? $(this).siblings('input[type="hidden"]').val() : null;
        if (hiddenVal && !$(this).prop('readonly')) {
            $(this).val(hiddenVal.replace('.', ','));
        }
    });

    function calculateAllTotals() {
        $('.company-section').each(function() {
            var $section = $(this);
            var sumTutarTL = 0;
            var sumOdenen = 0;

            $section.find('.main-payment-table tbody tr.data-row').each(function() {
                sumTutarTL += parseFloat($(this).find('.tutartl-input').val()) || 0;
                sumOdenen += parseFloat($(this).find('.odenen-tutar-input').val()) || 0;
            });

            $section.find('.toplam-tutar-tl-label').text('₺' + formatMoneyStr(sumTutarTL));
            $section.find('.toplam-odenen-column-label').text('₺' + formatMoneyStr(sumOdenen));

            $section.find('.summary-total-to-pay').text('₺' + formatMoneyStr(sumTutarTL));
            $section.find('.summary-total-paid').text('₺' + formatMoneyStr(sumOdenen));
            
            var balance = sumTutarTL - sumOdenen;
            $section.find('.summary-balance').text('₺' + formatMoneyStr(balance));
        });
    }

    $('.delete-btn').click(function(){
        var id = $(this).data('id');
        var db = $(this).data('dbname');
        var tr = $(this).closest('tr');
        if(confirm("Silmek istediğinize emin misiniz?")){
            $.post('/Rapor79/Sil', { docEntry: id, dbName: db }, function(res){
                if(res.success){ 
                    tr.fadeOut(function(){ 
                        $(this).remove(); 
                        if(tr.next().hasClass('sub-detail-row')) { tr.next().remove(); }
                        calculateAllTotals(); 
                        updateGlobalSelectedTotal(); 
                    }); 
                    toastr.success("Kayıt silindi."); 
                }
            });
        }
    });

    $(document).on('click', '.belge-detay-btn', function () {
        var btn = $(this);
        var tr = btn.closest('tr');
        var kaynakDbName = btn.data('kaynakdb'); 
        var docEntry = btn.data('docentry');
        var belgeTipi = btn.data('belgetipi');

        if (tr.next().hasClass('sub-detail-row')) {
            tr.next().fadeOut(200, function() { $(this).remove(); });
            btn.find('i').removeClass('fa-chevron-circle-up').addClass('fa-chevron-circle-down');
            return;
        }

        btn.find('i').removeClass('fa-chevron-circle-down').addClass('fa-spinner fa-spin');

        $.get('/Rapor79/GetBelgeAltDetay', { kaynakDbName: kaynakDbName, belgeTipi: belgeTipi, docEntry: docEntry }, function (res) {
            btn.find('i').removeClass('fa-spinner fa-spin').addClass('fa-chevron-circle-up');

            if (res.success && res.data && res.data.length > 0) {
                var firstRow = res.data[0];
                var faturaNo = firstRow.FaturaNo || firstRow.faturaNo || '';
                var pb = firstRow.ParaBirimi || firstRow.paraBirimi || 'TRY';

                var toplamKdvsiz = 0;
                var toplamKdv = 0;
                var genelToplam = 0;

                var kalemlerTable = 
                    '<table class="table table-bordered table-striped table-hover mt-2 mb-2" style="font-size:11px; background-color: white;">' +
                        '<thead style="background-color: #eef2f7; border-bottom: 1px solid rgba(15,23,42,.14);">' +
                            '<tr>' +
                                '<th style="color: #000; font-weight: bold;">Ürün/Hizmet Kodu</th>' +
                                '<th style="color: #000; font-weight: bold;">Açıklama</th>' +
                                '<th class="text-center" style="color: #000; font-weight: bold;">Miktar</th>' +
                                '<th class="text-end" style="color: #000; font-weight: bold;">KDV\'siz Tutar</th>' +
                                '<th class="text-end" style="color: #000; font-weight: bold;">KDV Tutarı</th>' +
                                '<th class="text-end" style="color: #000; font-weight: bold;">KDV\'li Toplam</th>' +
                            '</tr>' +
                        '</thead>' +
                        '<tbody>';

                res.data.forEach(function(k) {
                    var miktar = k.Miktar || k.miktar || 0;
                    var kdvsiz = k.KdvsizTutar || k.kdvsizTutar || 0;
                    var kdv = k.KdvTutari || k.kdvTutari || 0;
                    var kdvli = k.KdvliTutar || k.kdvliTutar || 0;

                    toplamKdvsiz += kdvsiz;
                    toplamKdv += kdv;
                    genelToplam += kdvli;

                    kalemlerTable += 
                        '<tr>' +
                            '<td>' + (k.ItemCode || k.itemCode || '') + '</td>' +
                            '<td>' + (k.KalemAdi || k.kalemAdi || '') + '</td>' +
                            '<td class="text-center">' + miktar.toFixed(2) + '</td>' +
                            '<td class="text-end">' + formatMoneyStr(kdvsiz) + ' ' + pb + '</td>' +
                            '<td class="text-end text-danger">' + formatMoneyStr(kdv) + ' ' + pb + '</td>' +
                            '<td class="text-end fw-bold text-success">' + formatMoneyStr(kdvli) + ' ' + pb + '</td>' +
                        '</tr>';
                });

                kalemlerTable += 
                        '</tbody>' +
                        '<tfoot>' +
                            '<tr style="background-color: #eef2f7; font-weight: 600; border-top: 1px solid rgba(15,23,42,.14);">' +
                                '<td colspan="3" class="text-end" style="color: #000;">BELGE TOPLAMLARI:</td>' +
                                '<td class="text-end" style="color: #000;">' + formatMoneyStr(toplamKdvsiz) + ' ' + pb + '</td>' +
                                '<td class="text-end text-danger">' + formatMoneyStr(toplamKdv) + ' ' + pb + '</td>' +
                                '<td class="text-end text-success">' + formatMoneyStr(genelToplam) + ' ' + pb + '</td>' +
                            '</tr>' +
                        '</tfoot>' +
                    '</table>';

                var faturaText = faturaNo ? '(' + faturaNo + ')' : '';
                var subRow = 
                    '<tr class="sub-detail-row bg-light" style="display:none;">' +
                        '<td colspan="4"></td>' + 
                        '<td colspan="19">' +
                            '<div class="p-3 border border-primary rounded shadow-sm" style="background-color: #f8f9fa;">' +
                                '<h6 class="text-primary fw-bold mb-3 border-bottom pb-2">' +
                                    '<i class="fas fa-list"></i> ' + belgeTipi + ' Detayları ' + faturaText +
                                '</h6>' +
                                kalemlerTable +
                            '</div>' +
                        '</td>' +
                    '</tr>';

                tr.after(subRow);
                tr.next().fadeIn(300);
            } else {
                toastr.error(res.message || "Belge detayı bulunamadı.");
                btn.find('i').removeClass('fa-spinner fa-spin').addClass('fa-chevron-circle-down');
            }
        }).fail(function() {
            toastr.error("Detaylar çekilirken ağ hatası oluştu.");
            btn.find('i').removeClass('fa-spinner fa-spin').addClass('fa-chevron-circle-down');
        });
    });

    // ---- Bir ödemeye ait dekont listesi (IT talebi #11) --------------
    $(document).on('click', '.dekont-liste-btn', function () {
        let dekontlar = [];
        try { dekontlar = JSON.parse($(this).attr('data-dekontlar') || '[]'); } catch (e) { dekontlar = []; }

        const dbName = $(this).data('dbname');
        const docEntry = $(this).data('docentry');
        const govde = $('#dekontListeGovde');

        if (!dekontlar.length) {
            govde.html('<div class="text-muted text-center py-3">Kayıtlı dekont yok.</div>');
        } else {
            let html = '<div class="table-responsive"><table class="table table-sm align-middle mb-0">' +
                       '<thead><tr><th>Açıklama</th><th>Dosya</th><th>Yükleyen</th><th>Tarih</th><th class="text-end">İşlem</th></tr></thead><tbody>';

            dekontlar.forEach(function (d) {
                const kacir = t => $('<div>').text(t == null ? '' : t).html();
                const tarih = d.Tarih ? new Date(d.Tarih).toLocaleString('tr-TR') : '-';
                html += '<tr>' +
                    '<td class="fw-semibold">' + (d.Aciklama ? kacir(d.Aciklama) : '<span class="text-muted">-</span>') + '</td>' +
                    '<td style="font-size:.8rem;">' + kacir(d.DosyaAdi) + '</td>' +
                    '<td style="font-size:.8rem;">' + kacir(d.Yukleyen) + '</td>' +
                    '<td style="font-size:.8rem;">' + kacir(tarih) + '</td>' +
                    '<td class="text-end text-nowrap">' +
                        '<button type="button" class="btn btn-sm btn-outline-primary pdf-preview-btn" data-url="' + kacir(d.DosyaYolu) + '" title="Görüntüle"><i class="fas fa-eye"></i></button> ' +
                        '<button type="button" class="btn btn-sm btn-outline-danger dekont-tek-sil" data-dekontid="' + d.Id + '" data-dbname="' + kacir(dbName) + '" data-docentry="' + docEntry + '" title="Sil"><i class="fas fa-trash"></i></button>' +
                    '</td></tr>';
            });

            html += '</tbody></table></div>';
            govde.html(html);
        }

        new bootstrap.Modal(document.getElementById('dekontListeModal')).show();
    });

    $(document).on('click', '.dekont-tek-sil', function () {
        if (!confirm('Bu dekont silinsin mi?')) return;
        const dugme = $(this);

        $.post('/Rapor79/DeleteDekont', {
            dbName: dugme.data('dbname'),
            docEntry: dugme.data('docentry'),
            dekontId: dugme.data('dekontid')
        }, function (v) {
            if (v.success) {
                toastr.success(v.message || 'Dekont silindi.');
                setTimeout(() => location.reload(), 800);
            } else {
                toastr.error(v.message || 'Silinemedi.');
            }
        }).fail(function () { toastr.error('Silme isteği başarısız.'); });
    });

    $(document).on('click', '.open-dekont-modal-btn', function() {
        var dbName = $(this).data('dbname');
        var docEntry = $(this).data('docentry');
        $('#dekontUploadDbName').val(dbName);
        $('#dekontUploadDocEntry').val(docEntry);
        $('#dekontDosya').val(''); 
        $('#secilenMail').val('');
        $('#dekontAciklama').val(''); 
        $('#dekontUploadModal').modal('show');
    });

    $('#btnDekontKaydet').click(function() {
        var fileInput = $('#dekontDosya')[0];
        if (fileInput.files.length === 0) {
            toastr.warning('Lütfen yüklenecek bir dosya seçin.');
            return;
        }

        var form = $('#dekontUploadForm')[0];
        var formData = new FormData(form);
        var btn = $(this);
        btn.html('<i class="fas fa-spinner fa-spin"></i> Yükleniyor...').prop('disabled', true);

        $.ajax({
            url: '/Rapor79/UploadDekont',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: { 'RequestVerificationToken': csrfToken },
            success: function(res) {
                btn.html('<i class="fas fa-save"></i> Yükle').prop('disabled', false);
                if (res.success) {
                    toastr.success(res.message);
                    $('#dekontUploadModal').modal('hide');
                    
                    var dbName = $('#dekontUploadDbName').val();
                    var docEntry = $('#dekontUploadDocEntry').val();
                    var td = $('.open-dekont-modal-btn[data-docentry="'+docEntry+'"][data-dbname="'+dbName+'"]').closest('td');
                    
                    td.html(`
                        <div class="btn-group w-100" role="group">
                            <button type="button" class="btn btn-sm btn-outline-primary p-1 pdf-preview-btn" data-url="${res.filePath}" title="Dekontu Önizle">
                                <i class="fas fa-eye"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-danger p-1 delete-dekont-btn" data-dbname="${dbName}" data-docentry="${docEntry}" title="Dekontu Sil">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    `);
                } else {
                    toastr.error(res.message || "Dosya yüklenemedi.");
                }
            },
            error: function() {
                toastr.error("Ağ hatası, işlem gerçekleştirilemedi.");
                btn.html('<i class="fas fa-save"></i> Yükle').prop('disabled', false);
            }
        });
    });

    $(document).on('click', '.delete-dekont-btn', function() {
        var btn = $(this);
        var dbName = btn.data('dbname');
        var docEntry = btn.data('docentry');

        if (confirm('Bu dekontu silmek istediğinize emin misiniz? (Sildiğinizde yenisini yükleyebilirsiniz)')) {
            var td = btn.closest('td');
            btn.html('<i class="fas fa-spinner fa-spin"></i>').prop('disabled', true);

            $.ajax({
                url: '/Rapor79/DeleteDekont',
                type: 'POST',
                data: { dbName: dbName, docEntry: docEntry },
                headers: { 'RequestVerificationToken': csrfToken },
                success: function(res) {
                    if (res.success) {
                        toastr.success(res.message);
                        td.html(`
                            <button type="button" class="btn btn-sm btn-outline-secondary p-1 w-100 open-dekont-modal-btn" 
                                    data-dbname="${dbName}" 
                                    data-docentry="${docEntry}" 
                                    title="Dekont Yükle">
                                <i class="fas fa-upload"></i> Yükle
                            </button>
                        `);
                    } else {
                        toastr.error(res.message || 'Dekont silinemedi.');
                        btn.html('<i class="fas fa-trash"></i>').prop('disabled', false);
                    }
                },
                error: function() {
                    toastr.error('Ağ hatası, silinemedi.');
                    btn.html('<i class="fas fa-trash"></i>').prop('disabled', false);
                }
            });
        }
    });

    // ==============================================================
    // --- AUTO-SAVE VE OTOMATİK RENK/SIRALAMA VE KUYRUK YÖNETİMİ ---
    // ==============================================================

    function getRowPayload(row) {
        const dbName = row.closest('tbody').data('dbname');
        const docEntry = row.data('docentry');
        
        return {
            DbName: dbName,
            DocEntry: docEntry,
            U_Sira: row.find('.sira-input').val(),
            U_Renk: row.find('.row-color-input').val(),
            U_KayitTipi: row.find('input[name="U_KayitTipi"]').val(),
            U_FaturaSiparisNo: row.find('input[name="U_FaturaSiparisNo"]').val(),
            U_KaynakFirma: row.find('input[name="U_KaynakFirma"]').val(),
            U_PlanlananTarih: row.find('input[name="U_PlanlananTarih"]').val() || null,
            U_Kurum: row.find('[name="U_Kurum"]').val(),
            U_Satinalmaci: row.find('input[name="U_Satinalmaci"]').val(),
            U_UrunHizmet: row.find('[name="U_UrunHizmet"]').val(),
            U_ParaBirimi: row.find('select[name="U_ParaBirimi"]').val(),
            U_Kur: parseFloat(row.find('.kur-input').val()) || 1, 
            U_Tutar: parseFloat(row.find('.tutar-input').val()) || 0,
            U_TutarTL: parseFloat(row.find('.tutartl-input').val()) || 0,
            U_OdemeYontemi: row.find('select[name="U_OdemeYontemi"]').val(),
            U_OdenmeTarihi: row.find('input[name="U_OdenmeTarihi"]').val() || null,
            U_OdenenTutar: parseFloat(row.find('.odenen-tutar-input').val()) || 0,
            U_OdenenTutarPB: row.find('select[name="U_OdenenTutarPB"]').val() || 'TRY',
            U_Durum: row.find('select[name="U_Durum"]').val(),
            // HATA ÇÖZÜMÜ: ESKİ DURUMU EKLİYORUZ
            U_EskiDurum: row.find('.eski-durum-input').val(),
            U_Notlar: row.find('[name="U_Notlar"]').val(),
            U_BagliBelgeTipi: row.find('input[name="U_BagliBelgeTipi"]').val(),
            U_BagliDocEntry: parseInt(row.find('input[name="U_BagliDocEntry"]').val()) || null,
            U_IslemTipi: row.find('select[name="U_IslemTipi"]').val(),
            U_MutabakatDurumu: row.find('select[name="U_MutabakatDurumu"]').val() 
        };
    }

    // KUYRUĞA EKLEME
    function enqueueSave(row) {
        const docEntry = row.data('docentry');
        if(!docEntry || docEntry == 0) return;

        const payload = getRowPayload(row);

        const existingIndex = saveQueue.findIndex(item => item.payload.DocEntry === docEntry && item.payload.DbName === payload.DbName);
        if (existingIndex > -1) {
            saveQueue[existingIndex] = { payload: payload, row: row }; // Sadece son hali kuyrukta tut
        } else {
            saveQueue.push({ payload: payload, row: row });
        }

        processQueue();
    }

    // KUYRUK İŞLEYİCİ
    function processQueue() {
        if (isProcessingQueue || saveQueue.length === 0) return;
        
        isProcessingQueue = true;
        const currentItem = saveQueue.shift(); // İlk elemanı al
        const payload = currentItem.payload;
        const row = currentItem.row;

        row.addClass('saving-row');

        fetch('/Rapor79/AjaxRowGuncelle?dbName=' + payload.DbName, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken },
            body: JSON.stringify(payload)
        }).then(res => res.json()).then(data => {
            row.removeClass('saving-row');
            if(data.success) {
                const indicator = row.closest('.company-section').find('.auto-save-indicator');
                indicator.fadeIn().delay(2000).fadeOut();
                
                // HATA ÇÖZÜMÜ: Başarılı kayıttan sonra eski durumu güncel duruma eşitliyoruz.
                row.find('.eski-durum-input').val(payload.U_Durum);

                row.find('.chk-row').data('kurum', payload.U_Kurum);
                row.find('.chk-row').data('urun', payload.U_UrunHizmet);
                row.find('.chk-row').data('tutar', payload.U_Tutar);
                row.find('.chk-row').data('tutartl', payload.U_TutarTL);
                row.find('.chk-row').data('parabirimi', payload.U_ParaBirimi);
                updateGlobalSelectedTotal();
            } else {
                toastr.error("Kayıt başarısız: " + data.message);
            }
        }).catch(err => {
            row.removeClass('saving-row');
            toastr.error("Ağ hatası, kaydedilemedi.");
        }).finally(() => {
            isProcessingQueue = false;
            processQueue(); // Sonraki işlemi tetikle
        });
    }

    $(document).on('focus', '.auto-save-trigger', function() {
        $(this).data('prev-val', $(this).val());
    });

    $('.auto-save-trigger').each(function() {
        $(this).data('prev-val', $(this).val());
    });

    // ---- Mutabakat muafiyeti (IT talebi #9) ------------------------
    // Muafiyet mutabakat kolonuna yazılmaz; portalde ayrı tutulur.
    // .auto-save-trigger sınıfı yok, bu yüzden satırın toplu kaydetme
    // akışını tetiklemez.
    $(document).on('change', '.muafiyet-select', function () {
        const secim = $(this);
        const satir = secim.closest('tr');
        const oncekiDeger = secim.data('prev-val') || '';
        const yeniDeger = secim.val() || '';

        fetch('/Rapor79/MuafiyetKaydet', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                DbName: secim.data('dbname'),
                DocEntry: parseInt(secim.data('docentry'), 10),
                Sebep: yeniDeger
            })
        })
        .then(c => c.json())
        .then(v => {
            if (!v.success) {
                toastr.error(v.message || 'Muafiyet kaydedilemedi.');
                secim.val(oncekiDeger);
                return;
            }

            secim.data('prev-val', yeniDeger);
            secim.toggleClass('muafiyet-dolu', yeniDeger !== '');
            secim.toggleClass('muafiyet-bos', yeniDeger === '');

            // Satır rengi mutabık gibi davransın (ödemeye hazır).
            const durumVal = satir.find('.durum-select').val();
            const mutabakatVal = satir.find('select[name="U_MutabakatDurumu"]').val();
            if (durumVal === 'Planlandı') {
                const renk = (mutabakatVal === 'Mutabık' || yeniDeger !== '') ? '#fff3cd' : '#cff4fc';
                satir.css('background-color', renk);
                satir.find('.row-color-input').val(renk);
            }

            toastr.success(yeniDeger === ''
                ? 'Mutabakat muafiyeti kaldırıldı.'
                : 'Mutabakat muafiyeti kaydedildi: ' + yeniDeger);
        })
        .catch(() => {
            toastr.error('Muafiyet kaydedilemedi (bağlantı hatası).');
            secim.val(oncekiDeger);
        });
    });

    $('.muafiyet-select').each(function () { $(this).data('prev-val', $(this).val()); });

    $(document).on('input change', '.auto-save-trigger', function() {
        const row = $(this).closest('tr');
        let needsReorder = false;

        // --- ÖZEL ONAY VE ÖDENDİ KONTROLÜ BAŞLANGICI ---
        if ($(this).hasClass('durum-select')) {
            let durumVal = $(this).val();
            let prevVal = $(this).data('prev-val');
            let mutabakatVal = row.find('select[name="U_MutabakatDurumu"]').val();
            let mutabakatTarihCell = row.find('.mutabakat-tarih-cell').text().trim();
            let isOzelOnaySwitch = $('#ozelOnayModu').is(':checked');
            let userCanSetOzelOnay = $('#canSetOzelOnay').val() === 'true';

            // Özel Onay Yetkisi Kontrolü (SADECE FARKLI BİR DURUMDAN ÖZEL ONAYA ÇEKİLİYORSA)
            if (durumVal === 'Özel Onay' && prevVal !== 'Özel Onay' && !userCanSetOzelOnay) {
                toastr.error('Özel Onay durumuna alma yetkiniz bulunmamaktadır.');
                $(this).val(prevVal);
                return false;
            }

            // ÖDENDİ yetkisi (Özel Onaydan geliyorsa geç)
            // IT talebi #9: vergi, kontör/yakıt yükleme, elektrik/su/doğalgaz
            // faturası gibi mutabakatı yapılamayan işlemler için satırda
            // muafiyet seçilmişse mutabakat şartı aranmaz. Mutabakat
            // kolonunun kendisine dokunulmaz.
            let mutabakatMuaf = ($(row).find('.muafiyet-select').val() || '') !== '';

            if (durumVal === 'ÖDENDİ' && !isOzelOnaySwitch && !mutabakatMuaf && prevVal !== 'Özel Onay' && prevVal !== 'ÖDENDİ') {
                if (mutabakatVal !== 'Mutabık' || mutabakatTarihCell === '-' || mutabakatTarihCell === '') {
                    toastr.error('Durumu "ÖDENDİ" yapabilmek için Mutabakat "Mutabık" olmalı. ' +
                                 'Mutabakatı yapılamayan bir işlemse (vergi, kontör/yakıt, elektrik/su/doğalgaz faturası) ' +
                                 'Mutabakat sütunundan "Mutabakat gerekmiyor" altındaki uygun sebebi seçin.');
                    
                    $(this).val(prevVal);
                    return false; 
                }
            }
            $(this).data('prev-val', $(this).val()); 
        }
        // --- ÖDENDİ KONTROLÜ BİTİŞİ ---

        if ($(this).hasClass('durum-select')) {
            let durumVal = row.find('.durum-select').val();
            let mutabakatVal = row.find('select[name="U_MutabakatDurumu"]').val();
            let color = 'transparent';

            if (durumVal === 'ÖDENDİ') color = '#d4edda'; 
            else if (durumVal === 'Gecikti') color = '#f8d7da'; 
            else if (durumVal === 'Özel Onay') color = '#e2d9f3'; // Mor
            else if (durumVal === 'Planlandı') {
                // Muafiyetli işlem de mutabık gibi ödemeye hazırdır.
                let muaf = ($(row).find('.muafiyet-select').val() || '') !== '';
                if (mutabakatVal === 'Mutabık' || muaf) color = '#fff3cd'; 
                else color = '#cff4fc'; 
            }
            else if (durumVal === 'Bekliyor') color = '#ffffff';

            row.css('background-color', color);
            row.find('.row-color-input').val(color);
            
            needsReorder = true;
        }

        if ($(this).attr('name') === 'U_PlanlananTarih' || $(this).attr('name') === 'U_OdenmeTarihi' || $(this).hasClass('odeme-yontemi-select') || $(this).hasClass('para-birimi-select')) {
            needsReorder = true;
        }

        if (needsReorder) {
            reorderTableRows(row.closest('tbody'));
        }

        if (!$(this).hasClass('para-birimi-select') && !$(this).hasClass('tutar-display') && !$(this).hasClass('odenen-tutar-display') && !$(this).hasClass('kur-display') && !$(this).hasClass('odeme-yontemi-select')) {
            
            // Kuyruğa Alma İşlemi (Yazma işlemleri için küçük bir gecikme ekliyoruz, hemen ardından kuyruğa itiyoruz)
            clearTimeout(row.data('saveTimeout'));
            var newTimeout = setTimeout(() => {
                enqueueSave(row);
            }, 500); 
            row.data('saveTimeout', newTimeout);

        }
    });

    // ==============================================================
    // --- SAĞ TIK MENÜSÜ VE LOGLAR ---
    // ==============================================================
    var activeContextRow = null;
    $(document).on('contextmenu', '.sortable-body tr.data-row', function(e) {
        e.preventDefault();
        activeContextRow = $(this);
        $('#rowContextMenu').css({ display: 'block', left: e.pageX + 'px', top: e.pageY + 'px' });
    });

    $(document).click(function() { $('#rowContextMenu').hide(); });

    $('#btnShowLogs').click(function(e) {
        e.preventDefault();
        $('#rowContextMenu').hide(); 

        if (activeContextRow) {
            var dbName = activeContextRow.closest('tbody').data('dbname');
            var docEntry = activeContextRow.data('docentry');

            if (!docEntry || docEntry == 0) {
                toastr.warning("Bu kayıt henüz sisteme kaydedilmemiş.");
                return;
            }

            $('#logTabloBody').html('<tr><td colspan="4" class="text-center text-muted"><i class="fas fa-spinner fa-spin"></i> Yükleniyor...</td></tr>');
            $('#logModal').modal('show');

            $.ajax({
                url: '/Rapor79/GetRowLogs',
                type: 'GET',
                data: { dbName: dbName, docEntry: docEntry },
                success: function (res) {
                    if (res.success) {
                        let html = '';
                        if(res.data.length === 0) {
                            html = '<tr><td colspan="4" class="text-center text-danger">Bu belgeye ait geçmiş kaydı bulunamadı.</td></tr>';
                        } else {
                            res.data.forEach(x => {
                                let islemTarihi = x.IslemTarihi || x.islemTarihi;
                                let kullanici = x.Kullanici || x.kullanici || '-';
                                let islemTipi = x.IslemTipi || x.islemTipi || '';
                                let aciklama = x.Aciklama || x.aciklama || '';

                                let dateObj = new Date(islemTarihi);
                                let formatliTarih = dateObj.toLocaleDateString('tr-TR') + ' ' + dateObj.toLocaleTimeString('tr-TR');
                                
                                let islemTipiBadge = 'bg-secondary';
                                if(islemTipi.includes('GÜNCEL')) islemTipiBadge = 'bg-info text-dark';
                                if(islemTipi.includes('EKLEME')) islemTipiBadge = 'bg-success';
                                if(islemTipi.includes('SİL') || islemTipi.includes('SIL')) islemTipiBadge = 'bg-danger';
                                if(islemTipi.includes('DEKONT') || islemTipi.includes('TALİMAT')) islemTipiBadge = 'bg-primary';

                                html += `<tr>
                                            <td>${formatliTarih}</td>
                                            <td class="fw-bold text-primary">${kullanici}</td>
                                            <td><span class="badge ${islemTipiBadge}">${islemTipi}</span></td>
                                            <td>${aciklama}</td>
                                         </tr>`;
                            });
                        }
                        $('#logTabloBody').html(html);
                    } else {
                        $('#logTabloBody').html(`<tr><td colspan="4" class="text-center text-danger">${res.message}</td></tr>`);
                    }
                },
                error: function() {
                    $('#logTabloBody').html('<tr><td colspan="4" class="text-center text-danger">Bağlantı hatası oluştu.</td></tr>');
                }
            });
        }
    });

    $('#btnLogGetir').click(function() {
        var dbName = $('#logFirmaSelect').val();
        var startDate = $('#logStartDate').val();
        var endDate = $('#logEndDate').val();
        var sadeceSilinenler = $('#chkSadeceSilinenler').is(':checked');

        $('#sistemLoglariBody').html('<tr><td colspan="5" class="text-center"><i class="fas fa-spinner fa-spin"></i> Yükleniyor...</td></tr>');
        
        $.get('/Rapor79/GetSistemLoglari', { 
            dbName: dbName,
            startDate: startDate,
            endDate: endDate,
            sadeceSilinenler: sadeceSilinenler
        }, function(res) {
            if (res.success) {
                let html = '';
                if(res.data.length === 0) {
                    html = '<tr><td colspan="5" class="text-center text-warning">Kayıt bulunamadı.</td></tr>';
                } else {
                    res.data.forEach(x => {
                        let islemTarihi = x.IslemTarihi || x.islemTarihi;
                        let kullanici = x.Kullanici || x.kullanici || '-';
                        let islemTipi = x.IslemTipi || x.islemTipi || '';
                        let aciklama = x.Aciklama || x.aciklama || '';
                        let docEntry = x.DocEntry || x.docEntry || '-';

                        let dateObj = new Date(islemTarihi);
                        let formatliTarih = dateObj.toLocaleDateString('tr-TR') + ' ' + dateObj.toLocaleTimeString('tr-TR');
                        
                        let islemTipiBadge = 'bg-secondary';
                        if(islemTipi.includes('GÜNCEL')) islemTipiBadge = 'bg-info text-dark';
                        if(islemTipi.includes('EKLEME')) islemTipiBadge = 'bg-success';
                        if(islemTipi.includes('SİL') || islemTipi.includes('SIL')) islemTipiBadge = 'bg-danger';
                        if(islemTipi.includes('DEKONT') || islemTipi.includes('TALİMAT')) islemTipiBadge = 'bg-primary';

                        let trClass = (islemTipi.includes('SİL') || islemTipi.includes('SIL')) ? 'table-danger' : '';

                        html += `<tr class="${trClass}">
                                    <td>${formatliTarih}</td>
                                    <td class="fw-bold text-primary">${kullanici}</td>
                                    <td><span class="badge ${islemTipiBadge}">${islemTipi}</span></td>
                                    <td class="fw-bold text-muted">${docEntry}</td>
                                    <td>${aciklama}</td>
                                 </tr>`;
                    });
                }
                $('#sistemLoglariBody').html(html);
            } else {
                $('#sistemLoglariBody').html(`<tr><td colspan="5" class="text-center text-danger">${res.message}</td></tr>`);
            }
        }).fail(function() {
            $('#sistemLoglariBody').html('<tr><td colspan="5" class="text-center text-danger">Bağlantı hatası oluştu.</td></tr>');
        });
    });

    $('#sistemLoglariModal').on('show.bs.modal', function () {
        if(!$('#logStartDate').val()) {
            var date = new Date();
            $('#logStartDate').val(new Date(date.getFullYear(), date.getMonth(), 1).toISOString().split('T')[0]);
            $('#logEndDate').val(new Date(date.getFullYear(), date.getMonth() + 1, 0).toISOString().split('T')[0]);
        }
        $('#btnLogGetir').trigger('click');
    });

    $('.sortable-header').click(function() {
        const table = $(this).closest('table');
        const tbody = table.find('tbody.sortable-body');
        const rows = tbody.find('tr.data-row').toArray();
        const index = $(this).index();
        const type = $(this).data('type') || 'string';
        let asc = $(this).data('asc') || false;

        table.find('.sortable-header i').attr('class', 'fas fa-sort text-muted');
        asc = !asc;
        $(this).data('asc', asc);
        $(this).find('i').attr('class', asc ? 'fas fa-sort-up text-dark' : 'fas fa-sort-down text-dark');

        rows.sort((a, b) => {
            let valA = $(a).children('td').eq(index).find('input[type="text"], textarea, input[type="date"], select, input[type="hidden"]:not(.sira-input)').val() || $(a).children('td').eq(index).text();
            let valB = $(b).children('td').eq(index).find('input[type="text"], textarea, input[type="date"], select, input[type="hidden"]:not(.sira-input)').val() || $(b).children('td').eq(index).text();
            
            valA = valA.trim(); valB = valB.trim();

            if (type === 'number') {
                valA = parseMoneyToFloat(valA); valB = parseMoneyToFloat(valB);
                return asc ? valA - valB : valB - valA;
            } else if (type === 'date') {
                valA = new Date(valA).getTime() || 0; valB = new Date(valB).getTime() || 0;
                return asc ? valA - valB : valB - valA;
            } else {
                return asc ? valA.localeCompare(valB) : valB.localeCompare(valA);
            }
        });

        var dbName = tbody.data('dbname');

        tbody.find('tr.sub-detail-row').remove();
        $('.belge-detay-btn i').removeClass('fa-chevron-circle-up').addClass('fa-chevron-circle-down');

        $.each(rows, function(i, row) { 
            var newIndex = i + 1;
            $(row).find('.sira-input').val(newIndex);
            $(row).find('.row-number').text(newIndex);
            tbody.append(row); 
        });
        
        tbody.append(table.find('.total-row')); 
    });

    // ==============================================================
    // --- MODALLARIN YENİ KONTROLLERİ (ÖZEL ONAY) ---
    // ==============================================================
    $('#ekleModal').on('show.bs.modal', function(e) {
        var btn = $(e.relatedTarget);
        var dbName = btn.data('dbname');
        $('#ModalDbName').val(dbName);
        $('#ModalFirmaAdi').text(dbName);
        
        $('#yeniSirketSelect').val(dbName).trigger('change'); 
        
        $('#yeniCariSelect').val(null).trigger('change').prop('disabled', false);
        $('#yeniKayitTipiSelect').val('').prop('disabled', true);
        $('#yeniBelgeSelect').empty().prop('disabled', true);
        $('#yeniSatinalmaciSelect').empty().append('<option value="">Önce Şirket Seçin...</option>').prop('disabled', true);

        $('#KurumInput, #UrunHizmetInput, #TutarInput, #TutarHiddenInput, #TutarTLInput, #TutarTLHiddenInput, #OdenenTutarInput, #OdenenTutarHiddenInput, #FaturaSiparisNoInput, #YeniKaynakFirmaInput').val('');
        $('#ParaBirimiInput').val('TRY').trigger('change'); 
        $('#OrijinalTutarHidden').val('');
        $('#YuzdeInput').val(100);
        $('#KurHesapInput').val('1,0000');
        $('#KurHiddenInput').val('1');

        // MODAL AÇILDIĞINDA DURUMU 'Bekliyor' OLARAK SIFIRLAR
        $('#EkleDurum').val('Bekliyor');
    });

    // Ekle Modal Form Submit Kontrolü (Özel Onay / ÖDENDİ)
    $('#ekleModal form').submit(function(e) {
        let durumVal = $(this).find('[name="U_Durum"]').val();
        let isOzelOnaySwitch = $('#ozelOnayModu').is(':checked');
        let userCanSetOzelOnay = $('#canSetOzelOnay').val() === 'true';

        if (durumVal === 'Özel Onay' && !userCanSetOzelOnay) {
            toastr.error('Özel Onay yetkiniz bulunmamaktadır.');
            e.preventDefault();
            return false;
        }

        if (durumVal === 'ÖDENDİ' && !isOzelOnaySwitch) {
            toastr.error('Yeni ödeme eklerken durumu doğrudan "ÖDENDİ" yapamazsınız (Özel Onay Modu aktif değilse). Lütfen önce "Özel Onay" seçiniz veya mutabakat kurallarını tamamlayınız.');
            e.preventDefault();
            return false;
        }
    });

    $('#yeniSirketSelect').change(function() {
        let seciliSirket = $(this).val();
        $('#yeniCariSelect').val(null).trigger('change').prop('disabled', !seciliSirket);
        
        $('#yeniKayitTipiSelect').val('').prop('disabled', true);
        $('#yeniBelgeSelect').empty().prop('disabled', true);

        if (seciliSirket) {
            loadSatinalmacilar(seciliSirket, $('#yeniSatinalmaciSelect'));
        } else {
            $('#yeniSatinalmaciSelect').empty().append('<option value="">Önce Şirket Seçin...</option>').prop('disabled', true);
        }
    });

    $('#yeniCariSelect').select2({
        dropdownParent: $('#ekleModal'),
        placeholder: 'Cari aramak için yazın...',
        width: '100%',
        language: { searching: function() { return "Kayıtlar aranıyor..."; } },
        ajax: {
            url: '/Rapor79/GetCariListesi', 
            dataType: 'json', 
            delay: 250,
            data: function (params) { 
                return { 
                    term: params.term || '',
                    dbName: $('#yeniSirketSelect').val()
                }; 
            }, 
            processResults: function (data) { 
                return { results: $.map(data, function (item) { return { id: item, text: item } }) }; 
            }
        }
    }).on('select2:select', function (e) {
        $('#yeniKayitTipiSelect').prop('disabled', false).val('');
        $('#yeniBelgeSelect').empty().prop('disabled', true);
    });

    $('#yeniKayitTipiSelect').change(function() {
        let dbName = $('#yeniSirketSelect').val();
        let cardName = $('#yeniCariSelect').val();
        let belgeTipi = $(this).val();

        if(dbName && cardName && belgeTipi) {
            $('#yeniBelgeSelect').prop('disabled', false).html('<option value="">Yükleniyor...</option>');
            $.get('/Rapor79/GetBelgelerForCari', { dbName: dbName, cardName: cardName, belgeTipi: belgeTipi }, function(res) {
                $('#yeniBelgeSelect').empty().append('<option value="">Seçim Yapın...</option>');
                if(res.length === 0) {
                    $('#yeniBelgeSelect').append('<option value="" disabled>Kayıt Bulunamadı!</option>');
                } else {
                    res.forEach(r => {
                        let tarih = r.docDate ? new Date(r.docDate).toLocaleDateString('tr-TR') : '';
                        let gosterilenBelgeNo = r.faturaNo ? r.faturaNo : r.docNum;
                        
                        let text = `[${r.belgeTipi}] ${tarih} | No: ${gosterilenBelgeNo} | ${formatMoneyStr(r.docTotalOrijinal)} ${r.paraBirimi} | Kalan: ${formatMoneyStr(r.docTotalTL)} ₺`;
                        let option = $('<option></option>').val(r.docEntry).text(text).data('raw', r);
                        $('#yeniBelgeSelect').append(option);
                    });
                }
            });
        }
    });

    $('#yeniBelgeSelect').change(function() {
        let selectedOption = $(this).find('option:selected');
        if(!selectedOption.val()) return;
        
        let data = selectedOption.data('raw');
        
        $('#KurumInput').val(data.cardName);
        $('#UrunHizmetInput').val(data.itemAcctName || 'Fatura/Sipariş/Bakiye Kaydı');
        $('#OdenenTutarInput').val(formatMoneyStr(data.odenenTutar));
        $('#OdenenTutarHiddenInput').val(data.odenenTutar.toFixed(4));
        $('#BagliBelgeTipi').val(data.belgeTipi);
        $('#BagliDocEntry').val(data.docEntry);
        
        let gosterilenBelgeNo = data.faturaNo ? data.faturaNo : (data.docNum || 'BAKİYE');
        $('#FaturaSiparisNoInput').val(gosterilenBelgeNo); 
        
        $('#IslemTipiInput').val(data.u_BE1_Aktar || '');
        $('#YeniKaynakFirmaInput').val(data.dbName);

        if (data.satinalmaci) {
            if ($('#yeniSatinalmaciSelect').find("option[value='" + data.satinalmaci + "']").length === 0) {
                $('#yeniSatinalmaciSelect').append(new Option(data.satinalmaci, data.satinalmaci));
            }
            $('#yeniSatinalmaciSelect').val(data.satinalmaci);
        }
        
        $('#TutarInput').val(formatMoneyStr(data.docTotalOrijinal)); 
        $('#TutarHiddenInput').val(data.docTotalOrijinal.toFixed(4));
        $('#OrijinalTutarHidden').val(data.docTotalOrijinal);
        $('#YuzdeInput').val(100);

        $('#ParaBirimiInput').val(data.paraBirimi).trigger('change'); 
        
        setTimeout(function() { triggerModalTlCalculation(); }, 500);
        toastr.info(data.firma + ' şirketinden belge bilgileri çekildi.');
    });


    function triggerModalTlCalculation() {
        var k = parseMoneyToFloat($('#KurHesapInput').val());
        $('#KurHiddenInput').val(k.toFixed(4));
        var t = parseMoneyToFloat($('#TutarInput').val());
        var tTL = k * t;
        if (tTL > 0) {
            $('#TutarTLInput').val(formatMoneyStr(tTL));
            $('#TutarTLHiddenInput').val(tTL.toFixed(4));
        } else {
            $('#TutarTLInput').val(''); $('#TutarTLHiddenInput').val('');
        }
    }

    $('#ParaBirimiInput').change(function() {
        var pb = $(this).val();
        var dbName = $('#ModalDbName').val();
        if(pb === 'TRY' || !pb) {
            $('#KurHesapInput').val('1,0000').prop('disabled', true);
            $('#TutarTLInput').prop('readonly', true).addClass('readonly-bg');
            triggerModalTlCalculation();
        } else {
            $('#KurHesapInput').prop('disabled', false);
            $('#TutarTLInput').prop('readonly', true).addClass('readonly-bg');
            $.get('/Rapor79/GetGuncelKur', { dbName: dbName, pb: pb }, function(rate) {
                $('#KurHesapInput').val(formatRateStr(rate));
                triggerModalTlCalculation();
            });
        }
    });

    $('#TutarInput, #KurHesapInput').on('keyup change blur input', function() { triggerModalTlCalculation(); });

    function loadOdenenler() {
        var s = $('#odenenlerStart').val();
        var e = $('#odenenlerEnd').val();
        var q = $('#odenenlerSearch').val();
        $('#odenenlerBody').html('<tr><td colspan="10" class="text-center py-4">Sorgulanıyor...</td></tr>');
        
        $.get('/Rapor79/GetOdenenler', { start: s, end: e, search: q }, function(res) {
            var html = '';
            var total = 0;
            res.forEach(function(item) {
                total += item.tutarTL || 0;
                var dt = item.odenmeTarihi ? new Date(item.odenmeTarihi).toLocaleDateString('tr-TR') : '-';
                
                var dekontBtn = item.dekontLink ? '<button type="button" class="btn btn-sm btn-outline-primary p-1 pdf-preview-btn" data-url="'+item.dekontLink+'"><i class="fas fa-eye"></i> Önizle</button>' : '<span class="text-muted">-</span>';
                
                html += '<tr>' +
                        '<td><span class="badge bg-secondary">' + item.firma + '</span></td>' +
                        '<td>' + dt + '</td>' +
                        '<td>' + (item.kurum || '-') + '</td>' +
                        '<td>' + (item.urunHizmet || '-') + '</td>' +
                        '<td>' + (item.odemeYontemi || '-') + '</td>' +
                        '<td><span class="fw-bold">' + (item.faturaSiparisNo || '-') + '</span></td>' +
                        '<td class="text-center">' + dekontBtn + '</td>' +
                        '<td class="text-end">' + formatMoneyStr(item.tutar) + ' ' + (item.paraBirimi || 'TL') + '</td>' +
                        '<td class="text-end fw-bold text-success">' + formatMoneyStr(item.tutarTL) + ' ₺</td>' +
                    '</tr>';
            });
            if(res.length === 0) { html = '<tr><td colspan="10" class="text-center text-danger">Kayıt bulunamadı.</td></tr>'; } 
            else { html += '<tr class="table-success"><td colspan="8" class="text-end fw-bold">KONSOLİDE TOPLAM:</td><td class="text-end fw-bold text-danger">' + formatMoneyStr(total) + ' ₺</td></tr>'; }
            $('#odenenlerBody').html(html);
        });
    }
    $('#btnOdenenlerGetir').click(loadOdenenler);
    $('#odenenlerSearch').on('keyup', function(e) { if(e.key === 'Enter') loadOdenenler(); });
    $('#odenenlerModal').on('show.bs.modal', function() {
        if(!$('#odenenlerStart').val()) {
            var date = new Date();
            $('#odenenlerStart').val(new Date(date.getFullYear(), date.getMonth(), 1).toISOString().split('T')[0]);
            $('#odenenlerEnd').val(new Date(date.getFullYear(), date.getMonth() + 1, 0).toISOString().split('T')[0]);
            loadOdenenler();
        }
    });

    $('#YuzdeInput').on('input', function() {
        var baseTutar = parseFloat($('#OrijinalTutarHidden').val()) || 0;
        var yuzde = parseFloat($(this).val()) || 0;
        
        if(baseTutar > 0) {
            var hesaplananTutar = baseTutar * (yuzde / 100);
            $('#TutarInput').val(formatMoneyStr(hesaplananTutar));
            $('#TutarHiddenInput').val(hesaplananTutar.toFixed(4));
            triggerModalTlCalculation();
        }
    });

    $('#TutarInput').on('blur', function() {
        var elleGirilenTutar = parseFloat($('#TutarHiddenInput').val()) || 0;
        if(elleGirilenTutar > 0) {
            $('#OrijinalTutarHidden').val(elleGirilenTutar);
            $('#YuzdeInput').val(100);
        }
    });

    // Edit Modal Form Submit Kontrolü (ÖDENDİ ve Özel Onay)
    $('#duzenleModal form').submit(function(e) {
        let durumVal = $('#EditDurum').val();
        let eskiDurumVal = $('#EditEskiDurumHidden').val();
        let mutabakatVal = $('#EditMutabakatHidden').val();
        let isOzelOnaySwitch = $('#ozelOnayModu').is(':checked');
        let userCanSetOzelOnay = $('#canSetOzelOnay').val() === 'true';

        // SADECE FARKLI BİR DURUMDAN ÖZEL ONAYA ÇEKİLİYORSA YETKİ SOR
        if (durumVal === 'Özel Onay' && eskiDurumVal !== 'Özel Onay' && !userCanSetOzelOnay) {
            toastr.error('Özel Onay durumuna alma yetkiniz bulunmamaktadır.');
            e.preventDefault();
            return false;
        }

        if (durumVal === 'ÖDENDİ' && !isOzelOnaySwitch && eskiDurumVal !== 'Özel Onay' && eskiDurumVal !== 'ÖDENDİ') {
            if (mutabakatVal !== 'Mutabık') { 
                toastr.error('Durumu "ÖDENDİ" yapabilmek için Mutabakat durumu "Mutabık" olmalı veya "Özel Onay" süreci tamamlanmalıdır!');
                e.preventDefault();
                return false;
            }
        }
    });

    $(document).on('click', '.edit-btn', function() {
        var tr = $(this).closest('tr');
        var dbName = $(this).data('dbname');

        var docEntry = tr.data('docentry');
        var kurum = tr.find('[name="U_Kurum"]').val();
        var satinalmaci = tr.find('input[name="U_Satinalmaci"]').val();
        var urun = tr.find('[name="U_UrunHizmet"]').val();
        var planTarih = tr.find('input[name="U_PlanlananTarih"]').val();
        var odemeYontemi = tr.find('select[name="U_OdemeYontemi"]').val();
        
        var pb = tr.find('select[name="U_ParaBirimi"]').val();
        if (pb === 'EURO') pb = 'EUR';
        if (pb === 'TL') pb = 'TRY';

        var kur = tr.find('.kur-input').val(); 
        var tutar = tr.find('.tutar-input').val();
        var tutarTL = tr.find('.tutartl-input').val();
        var odemeTarihi = tr.find('input[name="U_OdenmeTarihi"]').val();
        var odenenTutar = tr.find('.odenen-tutar-input').val();
        var odenenPB = tr.find('select[name="U_OdenenTutarPB"]').val() || 'TRY';

        var durum = tr.find('select[name="U_Durum"]').val();
        var mutabakat = tr.find('select[name="U_MutabakatDurumu"]').val();
        var notlar = tr.find('[name="U_Notlar"]').val();
        var kaynakFirma = tr.find('input[name="U_KaynakFirma"]').val();
        var bagliBelgeTipi = tr.find('input[name="U_BagliBelgeTipi"]').val();
        var bagliDocEntry = tr.find('input[name="U_BagliDocEntry"]').val();
        var faturaNo = tr.find('input[name="U_FaturaSiparisNo"]').val();
        var islemTipi = tr.find('select[name="U_IslemTipi"]').val();

        $('#EditDbName').val(dbName);
        $('#EditDocEntry').val(docEntry);
        $('#EditKurum').val(kurum);
        $('#EditUrunHizmet').val(urun);
        $('#EditPlanlananTarih').val(planTarih);
        $('#EditOdemeYontemi').val(odemeYontemi);
        $('#EditParaBirimi').val(pb);
        $('#EditDurum').val(durum);
        $('#EditEskiDurumHidden').val(durum); // Durum değişimi kontrolü için
        
        $('#EditMutabakat').val(mutabakat);
        $('#EditMutabakatHidden').val(mutabakat);

        $('#EditNotlar').val(notlar);
        $('#EditIslemTipi').val(islemTipi);

        $('#EditKaynakFirma').val(kaynakFirma);
        $('#EditBagliBelgeTipi').val(bagliBelgeTipi);
        $('#EditBagliDocEntry').val(bagliDocEntry);
        $('#EditFaturaSiparisNo').val(faturaNo);

        $('#EditTutar').val(formatMoneyStr(tutar));
        $('#EditTutarHidden').val(tutar);
        $('#EditTutarTL').val(formatMoneyStr(tutarTL));
        $('#EditTutarTLHidden').val(tutarTL);
        $('#EditOdenenTutar').val(formatMoneyStr(odenenTutar));
        $('#EditOdenenTutarHidden').val(odenenTutar);
        $('#EditOdenmeTarihi').val(odemeTarihi);

        $('#EditOdenenTutarPB').val(odenenPB);
        $('#EditKurHesap').val(formatRateStr(kur));
        $('#EditKurHidden').val(kur);

        if(pb === 'TRY' || !pb) {
            $('#EditKurHesap').prop('disabled', true);
        } else {
            $('#EditKurHesap').prop('disabled', false);
        }
        
        loadSatinalmacilar(dbName, $('#EditSatinalmaci'), satinalmaci);

        $('#duzenleModal').modal('show');
    });

    function calculateEditTlTutar() {
        var k = parseMoneyToFloat($('#EditKurHesap').val());
        $('#EditKurHidden').val(k.toFixed(4));
        var t = parseMoneyToFloat($('#EditTutar').val());
        var tTL = k * t;
        if (tTL > 0) {
            $('#EditTutarTL').val(formatMoneyStr(tTL));
            $('#EditTutarTLHidden').val(tTL.toFixed(4));
        } else {
            $('#EditTutarTL').val('');
            $('#EditTutarTLHidden').val('');
        }
    }

    $('#EditParaBirimi').change(function() {
        var pb = $(this).val();
        var dbName = $('#EditDbName').val();
        
        if (pb === 'EURO') pb = 'EUR';
        if (pb === 'TL') pb = 'TRY';

        if(pb === 'TRY' || !pb) {
            $('#EditKurHesap').val('1,0000').prop('disabled', true);
            calculateEditTlTutar();
        } else {
            $('#EditKurHesap').prop('disabled', false);
            if(!dbName) return; 

            $.get('/Rapor79/GetGuncelKur', { dbName: dbName, pb: pb }, function(rate) {
                $('#EditKurHesap').val(formatRateStr(rate));
                calculateEditTlTutar();
            });
        }
    });

    $('#EditTutar, #EditKurHesap').on('keyup change blur input', function() { 
        calculateEditTlTutar(); 
    });

});
