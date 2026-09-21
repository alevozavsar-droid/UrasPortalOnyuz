// Üst çubuk: okunmamış mesaj rozeti (30 sn'de bir), yeni mesajda ses + toastr uyarısı + masaüstü bildirimi, sekme başlığında sayaç
(function () {
    var son = -1, sonBildirilen = 0, baslik = document.title, ctx = null, etkilesim = false;

    // Tarayıcılar sesi ilk kullanıcı etkileşiminden önce çalmaz; ilk tıklama/tuşta ses bağlamı hazırlanır ve bildirim izni istenir
    function hazirla() {
        if (etkilesim) return; etkilesim = true;
        try { ctx = new (window.AudioContext || window.webkitAudioContext)(); if (ctx.state === 'suspended') ctx.resume(); } catch (e) { }
        try { if (window.Notification && Notification.permission === 'default') Notification.requestPermission(); } catch (e) { }
    }
    document.addEventListener('click', hazirla, { once: true }); document.addEventListener('keydown', hazirla, { once: true });

    // İki tonlu kısa bildirim sesi (ayar gerektirmez, dosya yok)
    function ses() {
        try {
            if (!ctx) return; if (ctx.state === 'suspended') ctx.resume();
            var t = ctx.currentTime;
            [[740, 0], [988, 0.14]].forEach(function (n) {
                var o = ctx.createOscillator(), g = ctx.createGain();
                o.type = 'sine'; o.frequency.value = n[0];
                g.gain.setValueAtTime(0.0001, t + n[1]); g.gain.exponentialRampToValueAtTime(0.09, t + n[1] + 0.02); g.gain.exponentialRampToValueAtTime(0.0001, t + n[1] + 0.22);
                o.connect(g); g.connect(ctx.destination); o.start(t + n[1]); o.stop(t + n[1] + 0.25);
            });
        } catch (e) { }
    }
    window.mesajSesi = ses;

    function masaustu(r) {
        try {
            if (!window.Notification || Notification.permission !== 'granted' || !document.hidden) return;
            var n = new Notification('Yeni mesaj · ' + (r.sonAd || r.sonKod), { body: r.sonMetin || '', tag: 'uras-mesaj', renotify: true });
            n.onclick = function () { window.focus(); location.href = '/Mesaj?ile=' + encodeURIComponent(r.sonKod); n.close(); };
        } catch (e) { }
    }

    function sor() {
        if (document.hidden && son >= 0 && Math.random() < 0.5) return;   // gizli sekmede istekleri seyrelt
        $.getJSON('/Mesaj/OkunmamisSayisi').done(function (r) {
            if (!r || !r.success) return;
            var $b = $('#mesajSayaci'); $b.text(r.adet).toggle(r.adet > 0);
            document.title = (r.adet > 0 ? '(' + r.adet + ') ' : '') + baslik;
            var sohbetSayfasi = location.pathname.toLowerCase().indexOf('/mesaj') === 0;
            if (r.sonId && r.sonId > sonBildirilen && son >= 0 && r.adet > 0 && !sohbetSayfasi) {
                ses(); masaustu(r);
                if (window.toastr) toastr.info('<b>' + $('<div>').text(r.sonAd || r.sonKod).html() + ':</b> ' + $('<div>').text(r.sonMetin || '').html(), 'Yeni mesaj', { timeOut: 8000, escapeHtml: false, onclick: function () { location.href = '/Mesaj?ile=' + encodeURIComponent(r.sonKod); } });
            }
            if (r.sonId) sonBildirilen = Math.max(sonBildirilen, r.sonId);
            son = r.adet;
        });
    }
    if ($('#mesajSayaci').length) { sor(); setInterval(sor, 30000); document.addEventListener('visibilitychange', function () { if (!document.hidden) sor(); }); }
})();
