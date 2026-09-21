// Portal içi mesajlaşma (Mesaj/Index): sohbet, dosya/görsel ekleri, sürükle-bırak, pano yapıştırma
(function () {
    var $kok = $('.msj'); if (!$kok.length) return;
    var ben = $kok.data('ben') || '', secili = null, seciliAd = '', sonId = 0, kisiler = [], zamanlayici = null, kisiZamanlayici = null, gonderiliyor = false;
    var bekleyen = [];   // gönderilecek dosyalar: { dosya: File, onizleme: dataURL|null }
    var kac = function (s) { return $('<div>').text(s == null ? '' : String(s)).html(); };
    var token = function () { return $('input[name="__RequestVerificationToken"]').val() || ''; };
    var bas = function (ad) { var p = (ad || '?').trim().split(/\s+/); return ((p[0] || '?').charAt(0) + (p[1] ? p[1].charAt(0) : '')).toUpperCase(); };
    var saat = function (t) { var d = new Date(t); return isNaN(d) ? '' : d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }); };
    var boyut = function (b) { b = +b || 0; return b < 1024 ? b + ' B' : b < 1048576 ? (b / 1024).toFixed(0) + ' KB' : (b / 1048576).toFixed(1) + ' MB'; };
    var gunEtiketi = function (t) {
        var d = new Date(t), b = new Date(); b.setHours(0, 0, 0, 0); var g = new Date(d); g.setHours(0, 0, 0, 0);
        var fark = Math.round((b - g) / 86400000);
        return fark === 0 ? 'Bugün' : fark === 1 ? 'Dün' : d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'long', year: d.getFullYear() !== b.getFullYear() ? 'numeric' : undefined });
    };
    var kisaZaman = function (t) { if (!t) return ''; var d = new Date(t), b = new Date(); return d.toDateString() === b.toDateString() ? saat(t) : d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit' }); };
    var dosyaIkonu = function (ad) {
        var u = (ad || '').toLowerCase().split('.').pop();
        if (u === 'pdf') return { i: 'fa-file-pdf', c: 'pdf' };
        if (u === 'xls' || u === 'xlsx' || u === 'csv') return { i: 'fa-file-excel', c: 'xls' };
        if (u === 'doc' || u === 'docx') return { i: 'fa-file-word', c: 'doc' };
        if (u === 'ppt' || u === 'pptx') return { i: 'fa-file-powerpoint', c: 'ppt' };
        if (u === 'zip' || u === 'rar' || u === '7z') return { i: 'fa-file-zipper', c: 'zip' };
        if (['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp'].indexOf(u) >= 0) return { i: 'fa-file-image', c: 'img' };
        return { i: 'fa-file-lines', c: 'txt' };
    };

    // ---- kişiler ----
    function kisileriYukle() {
        $.getJSON('/Mesaj/Kisiler').done(function (r) {
            if (!r.success) return;
            kisiler = r.kisiler || [];
            var $l = $('#msjKisiler').empty(), q = ($('#msjAra').val() || '').toLocaleLowerCase('tr-TR'), toplam = 0;
            if (!kisiler.length) { $l.html('<div class="msj-bos">Kullanıcı bulunamadı.</div>'); return; }
            kisiler.forEach(function (k) {
                toplam += k.okunmamis || 0;
                var gizli = q && (k.ad + ' ' + k.kod).toLocaleLowerCase('tr-TR').indexOf(q) < 0;
                var son = k.sonMesaj ? (k.sonBenden ? 'Sen: ' : '') + k.sonMesaj : '';
                $l.append('<div class="msj-kisi ' + (secili === k.kod ? 'aktif' : '') + (gizli ? ' gizli' : '') + '" data-kod="' + kac(k.kod) + '" data-ad="' + kac(k.ad) + '">'
                    + '<div class="msj-avatar">' + kac(bas(k.ad)) + '</div>'
                    + '<div><div class="ad">' + kac(k.ad) + ' <small>' + kac(k.kod) + '</small></div><div class="son ' + (k.okunmamis ? 'yeni' : '') + '">' + (son ? kac(son) : '<span class="text-muted">Henüz mesaj yok</span>') + '</div></div>'
                    + '<div class="sag"><span class="zaman">' + kisaZaman(k.sonTarih) + '</span>' + (k.okunmamis ? '<span class="rozet">' + k.okunmamis + '</span>' : '') + '</div></div>');
            });
            $('#msjToplam').text(toplam ? toplam + ' yeni' : '');
            var ile = $kok.data('ile'); if (ile && !secili) { var k = kisiler.find(function (x) { return x.kod === ile; }); if (k) sohbetAc(k.kod, k.ad); }
        });
    }
    $('#msjAra').on('input', function () {
        var q = (this.value || '').toLocaleLowerCase('tr-TR');
        $('.msj-kisi').each(function () { $(this).toggleClass('gizli', !!q && ($(this).data('ad') + ' ' + $(this).data('kod')).toLocaleLowerCase('tr-TR').indexOf(q) < 0); });
    });
    $(document).on('click', '.msj-kisi', function () { sohbetAc($(this).data('kod'), $(this).data('ad')); });
    $('#msjGeri').on('click', function () { $kok.removeClass('sohbette'); });

    // ---- sohbet ----
    function sohbetAc(kod, ad) {
        secili = kod; seciliAd = ad; sonId = 0; sonGun = '';
        $('.msj-kisi').removeClass('aktif').filter('[data-kod="' + kod + '"]').addClass('aktif').find('.rozet').remove();
        $('#msjKisiAd').text(ad); $('#msjKisiKod').text(kod); $('#msjAvatar').text(bas(ad));
        $('#msjAkis').html('<div class="msj-bos"><i class="fas fa-spinner fa-spin me-1"></i> yükleniyor…</div>');
        $('#msjYaz').show(); $('#msjMetin').val('').trigger('focus'); bekleyen = []; ekleriCiz();
        $kok.addClass('sohbette');
        if (history.replaceState) history.replaceState(null, '', '/Mesaj?ile=' + encodeURIComponent(kod));
        sohbetiCek(true);
        clearInterval(zamanlayici); zamanlayici = setInterval(function () { sohbetiCek(false); }, 5000);
    }
    var sonGun = '';
    function ekHtml(e, benden) {
        if (e.resimMi) return '<a class="msj-ek-resim" href="/Mesaj/Ek/' + e.id + '" data-ad="' + kac(e.ad) + '"><img src="/Mesaj/Ek/' + e.id + '" alt="' + kac(e.ad) + '" loading="lazy"></a>';
        var ik = dosyaIkonu(e.ad);
        return '<a class="msj-ek-dosya" href="/Mesaj/Ek/' + e.id + '?indir=true" title="İndir"><span class="ik ' + ik.c + '"><i class="fas ' + ik.i + '"></i></span><span><div class="ad">' + kac(e.ad) + '</div><div class="boyut">' + boyut(e.boyut) + '</div></span><i class="fas fa-download indir"></i></a>';
    }
    function balon(m) {
        var h = '';
        var g = gunEtiketi(m.tarih); if (g !== sonGun) { h += '<div class="msj-gun">' + kac(g) + '</div>'; sonGun = g; }
        h += '<div class="msj-balon ' + (m.benden ? 'giden' : 'gelen') + '" data-id="' + m.id + '">';
        (m.ekler || []).forEach(function (e) { h += ekHtml(e, m.benden); });
        h += '<span class="metin">' + kac(m.metin) + '</span>'
            + '<span class="meta">' + saat(m.tarih) + (m.benden ? '<i class="fas ' + (m.okundu ? 'fa-check-double okundu' : 'fa-check') + '"></i>' : '') + '</span></div>';
        return h;
    }
    function sohbetiCek(ilk) {
        if (!secili) return;
        var kod = secili;
        $.getJSON('/Mesaj/Konusma', { kullanici: kod, sonId: sonId }).done(function (r) {
            if (!r.success || secili !== kod) return;
            var $a = $('#msjAkis'), altta = ilk || ($a[0].scrollHeight - $a.scrollTop() - $a.height() < 80);
            if (ilk) { $a.empty(); sonGun = ''; }
            var h = ''; (r.mesajlar || []).forEach(function (m) { h += balon(m); if (m.id > sonId) sonId = m.id; });
            if (h) { $a.find('.msj-bos').remove(); $a.append(h); }
            if (ilk && !(r.mesajlar || []).length) $a.html('<div class="msj-bos">Henüz mesaj yok. İlk mesajı sen yaz 👋</div>');
            if (r.sonOkunan) $a.find('.msj-balon.giden').each(function () { if (+$(this).data('id') <= r.sonOkunan) $(this).find('.meta i').removeClass('fa-check').addClass('fa-check-double okundu'); });
            if (h && altta) $a.scrollTop($a[0].scrollHeight);
            if (h && !ilk && (r.mesajlar || []).some(function (m) { return !m.benden; })) { try { ses(); } catch (e) { } kisileriYukle(); }
        });
    }
    $('#msjYenile').on('click', function () { if (secili) { sonId = 0; sohbetiCek(true); } kisileriYukle(); });

    // ---- ekler (seçim, sürükle-bırak, yapıştır) ----
    function ekleriCiz() {
        var $e = $('#msjEkler').empty();
        bekleyen.forEach(function (b, i) {
            var ik = dosyaIkonu(b.dosya.name);
            $e.append('<div class="msj-ek-cip">' + (b.onizleme ? '<img src="' + b.onizleme + '" alt="">' : '<span class="ik"><i class="fas ' + ik.i + '"></i></span>') + '<span><div class="ad">' + kac(b.dosya.name) + '</div><div class="boyut">' + boyut(b.dosya.size) + '</div></span><button type="button" data-i="' + i + '" title="Kaldır"><i class="fas fa-xmark"></i></button></div>');
        });
    }
    function dosyaEkle(dosyalar) {
        if (!secili) { if (window.toastr) toastr.warning('Önce bir kişi seçin.'); return; }
        Array.prototype.forEach.call(dosyalar || [], function (f) {
            if (!f || !f.size) return;
            if (f.size > 25 * 1024 * 1024) { if (window.toastr) toastr.error(f.name + ' 25 MB sınırını aşıyor.'); return; }
            var kayit = { dosya: f, onizleme: null }; bekleyen.push(kayit);
            if (/^image\//.test(f.type)) { var rd = new FileReader(); rd.onload = function (ev) { kayit.onizleme = ev.target.result; ekleriCiz(); }; rd.readAsDataURL(f); }
        });
        ekleriCiz(); $('#msjMetin').trigger('focus');
    }
    $('#msjEkBtn').on('click', function () { if (!secili) { if (window.toastr) toastr.warning('Önce bir kişi seçin.'); return; } $('#msjDosya').trigger('click'); });
    $('#msjDosya').on('change', function () { dosyaEkle(this.files); this.value = ''; });
    $(document).on('click', '.msj-ek-cip button', function () { bekleyen.splice(+$(this).data('i'), 1); ekleriCiz(); });
    var sayac = 0, $sag = $('#msjSag');
    $sag.on('dragenter', function (e) { e.preventDefault(); if (secili) { sayac++; $sag.addClass('birakiliyor'); } })
        .on('dragover', function (e) { e.preventDefault(); })
        .on('dragleave', function (e) { e.preventDefault(); if (--sayac <= 0) { sayac = 0; $sag.removeClass('birakiliyor'); } })
        .on('drop', function (e) { e.preventDefault(); sayac = 0; $sag.removeClass('birakiliyor'); var dt = e.originalEvent.dataTransfer; if (dt && dt.files && dt.files.length) dosyaEkle(dt.files); });
    $('#msjMetin').on('paste', function (e) {
        var ogeler = (e.originalEvent.clipboardData || {}).items; if (!ogeler) return;
        var dosyalar = [];
        for (var i = 0; i < ogeler.length; i++) if (ogeler[i].kind === 'file') { var f = ogeler[i].getAsFile(); if (f) { if (!f.name || f.name === 'image.png') { try { f = new File([f], 'yapistirilan-' + new Date().toISOString().replace(/[:.]/g, '-') + '.png', { type: f.type }); } catch (x) { } } dosyalar.push(f); } }
        if (dosyalar.length) { e.preventDefault(); dosyaEkle(dosyalar); }
    });

    // ---- gönder ----
    function gonder() {
        var metin = ($('#msjMetin').val() || '').trim();
        if ((!metin && !bekleyen.length) || !secili || gonderiliyor) return;
        gonderiliyor = true; $('#msjGonder').prop('disabled', true);
        var bitti = function () { gonderiliyor = false; $('#msjGonder').prop('disabled', false); $('#msjMetin').trigger('focus'); };
        var basarili = function (r) {
            if (!r.success) { if (window.toastr) toastr.error(r.message); else alert(r.message); return; }
            $('#msjMetin').val('').css('height', ''); bekleyen = []; ekleriCiz(); sohbetiCek(false); kisileriYukle();
        };
        if (bekleyen.length) {
            var fd = new FormData(); fd.append('alici', secili); fd.append('metin', metin); fd.append('__RequestVerificationToken', token());
            bekleyen.forEach(function (b) { fd.append('dosyalar', b.dosya, b.dosya.name); });
            $('.msj-ek-cip').addClass('yukleniyor');
            $.ajax({ url: '/Mesaj/GonderEk', type: 'POST', data: fd, processData: false, contentType: false, timeout: 600000 }).done(basarili)
                .fail(function () { if (window.toastr) toastr.error('Dosya gönderilemedi.'); $('.msj-ek-cip').removeClass('yukleniyor'); }).always(bitti);
        } else {
            $.post('/Mesaj/Gonder', { alici: secili, metin: metin, __RequestVerificationToken: token() }).done(basarili)
                .fail(function () { if (window.toastr) toastr.error('Mesaj gönderilemedi.'); }).always(bitti);
        }
    }
    $('#msjGonder').on('click', gonder);
    $('#msjMetin').on('keydown', function (e) { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); gonder(); } })
        .on('input', function () { this.style.height = 'auto'; this.style.height = Math.min(this.scrollHeight, 160) + 'px'; });

    // ---- görsel büyütme ----
    $(document).on('click', '.msj-ek-resim', function (e) { e.preventDefault(); $('#msjBuyutImg').attr('src', this.href); $('#msjBuyutIndir').attr('href', this.href + '?indir=true'); $('#msjBuyut').addClass('acik'); });
    $('#msjBuyutKapat, #msjBuyut').on('click', function (e) { if (e.target === this || $(e.target).closest('#msjBuyutKapat').length) $('#msjBuyut').removeClass('acik'); });
    $(document).on('keydown', function (e) { if (e.key === 'Escape') $('#msjBuyut').removeClass('acik'); });

    // Ses: üst çubuk betiğindeki ortak bildirim sesi (kullanıcı etkileşiminden sonra çalar)
    function ses() { if (typeof window.mesajSesi === 'function') window.mesajSesi(); }

    // ---- tam sayfa yükseklik (üst çubuktaki CSS zoom'a göre gerçek pencereye sığdırılır) ----
    function boyutla() {
        var el = $kok[0]; if (!el) return;
        var zoom = (parseInt(document.documentElement.getAttribute('data-zoom'), 10) || 100) / 100;
        el.style.height = ((window.innerHeight / zoom) - 130) + "px";
        var r = el.getBoundingClientRect();                       // ölçüp bir kez düzelt (zoom yorumu tarayıcıya göre değişebiliyor)
        var fark = (window.innerHeight - 12) - r.bottom;
        if (Math.abs(fark) > 2) el.style.height = (el.offsetHeight + fark / zoom) + "px";
    }
    boyutla();
    $(window).on('resize', boyutla);
    try { new MutationObserver(boyutla).observe(document.documentElement, { attributes: true, attributeFilter: ['data-zoom', 'style'] }); } catch (e) { }

    // ---- emoji paneli (harici kütüphane yok) ----
    var EMOJI = {
        '😀': '😀 😃 😄 😁 😆 😅 🤣 😂 🙂 🙃 😉 😊 😇 🥰 😍 🤩 😘 😗 😚 😋 😛 😜 🤪 😝 🤑 🤗 🤭 🤫 🤔 🤐 🤨 😐 😑 😶 😏 😒 🙄 😬 🤥 😌 😔 😪 🤤 😴 😷 🤒 🤕 🤢 🤮 🥵 🥶 🥴 😵 🤯 🤠 🥳 🥸 😎 🤓 🧐 😕 😟 🙁 😮 😯 😲 😳 🥺 😦 😧 😨 😰 😥 😢 😭 😱 😖 😣 😞 😓 😩 😫 🥱 😤 😡 😠 🤬 💀 💩 🤡 👻 👽 🤖',
        '👍': '👍 👎 👌 🤌 🤏 ✌️ 🤞 🤟 🤘 🤙 👈 👉 👆 👇 ☝️ 👋 🤚 🖐️ ✋ 🖖 👏 🙌 🤝 🙏 ✍️ 💪 🦾 🫡 🫶 👀 👁️ 🧠 🗣️ 👤 👥',
        '❤️': '❤️ 🧡 💛 💚 💙 💜 🖤 🤍 🤎 💔 ❤️‍🔥 💕 💞 💓 💗 💖 💘 💝 💟 ♥️ 😻 💌 💋',
        '🎉': '🎉 🎊 🎈 🎁 🏆 🥇 🥈 🥉 🎯 ⭐ 🌟 ✨ 🔥 💥 💫 ⚡ 🌈 ☀️ 🌤️ ⛅ 🌧️ ⛈️ ❄️ ☕ 🍵 🍰 🎂 🍕 🍔 🍺 🥂 🍾',
        '📎': '📎 📌 📍 📁 📂 📄 📃 📊 📈 📉 📋 📝 📅 📆 🗓️ 📞 ☎️ 📧 ✉️ 📦 🧾 💰 💳 🏦 💼 🖥️ 💻 🖨️ ⌨️ 🖱️ 🔒 🔑 🛠️ ⚙️ 🔍 🔔 🔕 🚚 🏭 🏢',
        '✅': '✅ ❌ ❎ ✔️ ☑️ ⚠️ ❗ ❓ ❕ ❔ 💯 🔴 🟠 🟡 🟢 🔵 🟣 ⚫ ⚪ 🔺 🔻 ➡️ ⬅️ ⬆️ ⬇️ ↩️ ↪️ 🔄 🔁 ⏰ ⏳ ⌛ 🆗 🆕 🆙 🔝 🔜 🚫 ⛔ 🆘 ℹ️'
    };
    (function kurEmoji() {
        var $s = $('#msjEmojiSekme'), $g = $('#msjEmojiIzgara'); if (!$s.length) return;
        Object.keys(EMOJI).forEach(function (k, i) { $s.append('<button type="button" data-k="' + k + '" class="' + (i === 0 ? 'aktif' : '') + '" title="Kategori">' + k + '</button>'); });
        function ciz(k) { $g.empty(); EMOJI[k].split(' ').forEach(function (e) { if (e) $g.append('<button type="button" data-e="' + e + '">' + e + '</button>'); }); }
        ciz(Object.keys(EMOJI)[0]);
        $s.on('click', 'button', function () { $s.find('button').removeClass('aktif'); $(this).addClass('aktif'); ciz($(this).data('k')); });
        $g.on('click', 'button', function () { emojiEkle($(this).data('e')); });
    })();
    function emojiEkle(e) {
        var ta = document.getElementById('msjMetin'), b = ta.selectionStart || 0, s = ta.selectionEnd || 0, v = ta.value;
        ta.value = v.substring(0, b) + e + v.substring(s); ta.selectionStart = ta.selectionEnd = b + e.length; ta.focus();
        $(ta).trigger('input');
    }
    $('#msjEmojiBtn').on('click', function (e) { e.stopPropagation(); if (!secili) { if (window.toastr) toastr.warning('Önce bir kişi seçin.'); return; } $('#msjEmoji').toggleClass('acik'); });
    $(document).on('click', function (e) { if (!$(e.target).closest('#msjEmoji, #msjEmojiBtn').length) $('#msjEmoji').removeClass('acik'); });
    $('#msjMetin').on('keydown', function (e) { if (e.key === 'Escape') $('#msjEmoji').removeClass('acik'); });

    kisileriYukle();
    kisiZamanlayici = setInterval(kisileriYukle, 20000);
    $(window).on('beforeunload', function () { clearInterval(zamanlayici); clearInterval(kisiZamanlayici); });
})();
