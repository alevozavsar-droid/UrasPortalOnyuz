// SetPassword sayfasi betikleri.
// Arka plan parçacıkları (giriş ekranıyla aynı)
        (function () {
            var c = document.getElementById('bgCanvas'); if (!c || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
            var x = c.getContext('2d'), w, h, p = [], raf, DPR = Math.min(devicePixelRatio || 1, 1.5);
            function boyut() { w = c.width = innerWidth * DPR; h = c.height = innerHeight * DPR; c.style.width = innerWidth + 'px'; c.style.height = innerHeight + 'px'; p = []; var n = Math.min(80, Math.floor(innerWidth * innerHeight / 18000)); for (var i = 0; i < n; i++) p.push({ x: Math.random() * w, y: Math.random() * h, vx: (Math.random() - .5) * .35 * DPR, vy: (Math.random() - .5) * .35 * DPR, r: (Math.random() * 1.6 + .6) * DPR }); }
            function kare() { x.clearRect(0, 0, w, h); x.lineWidth = DPR; for (var i = 0; i < p.length; i++) { var a = p[i]; a.x += a.vx; a.y += a.vy; if (a.x < 0 || a.x > w) a.vx *= -1; if (a.y < 0 || a.y > h) a.vy *= -1; x.beginPath(); x.arc(a.x, a.y, a.r, 0, Math.PI * 2); x.fillStyle = 'rgba(255,255,255,.55)'; x.fill(); for (var j = i + 1; j < p.length; j++) { var b = p[j], dx = a.x - b.x, dy = a.y - b.y, d = dx * dx + dy * dy, m = (150 * DPR) * (150 * DPR); if (d < m) { x.strokeStyle = 'rgba(255,255,255,' + (.14 * (1 - d / m)).toFixed(3) + ')'; x.beginPath(); x.moveTo(a.x, a.y); x.lineTo(b.x, b.y); x.stroke(); } } } raf = requestAnimationFrame(kare); }
            boyut(); addEventListener('resize', boyut); raf = requestAnimationFrame(kare);
            document.addEventListener('visibilitychange', function () { if (document.hidden) cancelAnimationFrame(raf); else raf = requestAnimationFrame(kare); });
        })();

        $(function () {
            // tema
            var btn = document.getElementById('themeToggleBtn'), tema = localStorage.getItem('theme') || 'light';
            document.documentElement.setAttribute('data-theme', tema); ikon(tema);
            btn.addEventListener('click', function () { tema = tema === 'light' ? 'dark' : 'light'; document.documentElement.setAttribute('data-theme', tema); localStorage.setItem('theme', tema); ikon(tema); });
            function ikon(t) { var i = btn.querySelector('i'); i.className = t === 'dark' ? 'fas fa-sun text-warning' : 'fas fa-moon'; }

            // göster/gizle
            $('[data-goz]').on('click', function () { var inp = $(this).siblings('input'), ac = inp.attr('type') === 'password'; inp.attr('type', ac ? 'text' : 'password'); $(this).find('i').toggleClass('fa-eye', !ac).toggleClass('fa-eye-slash', ac); });

            // güç ölçer + kurallar + eşleşme
            function guc() {
                var v = $('#yeniSifre').val() || '', k = { uzun: v.length >= 8, buyuk: /[A-ZÇĞİÖŞÜ]/.test(v), kucuk: /[a-zçğıöşü]/.test(v), rakam: /\d/.test(v), ozel: /[^A-Za-z0-9ÇĞİÖŞÜçğıöşü]/.test(v) };
                var puan = Object.values(k).filter(Boolean).length;
                $('.kural').each(function () { $(this).toggleClass('ok', !!k[$(this).data('k')]); });
                var yuzde = v ? Math.max(15, puan * 20) : 0, renk = puan <= 2 ? 'var(--danger)' : puan <= 3 ? 'var(--warn)' : puan === 4 ? 'var(--brand)' : 'var(--success)';
                $('#gucBar').css({ width: yuzde + '%', background: renk });
                $('#gucTxt').text(!v ? 'Şifre gücü' : puan <= 2 ? 'Zayıf' : puan <= 3 ? 'Orta' : puan === 4 ? 'İyi' : 'Güçlü'); $('#gucPuan').text(v ? puan + '/5' : '');
                eslesme();
            }
            function eslesme() { var a = $('#yeniSifre').val(), b = $('#sifreTekrar').val(), e = $('#eslesme'); if (!b) { e.attr('class', 'eslesme'); return; } e.attr('class', 'eslesme ' + (a === b ? 'ok' : 'no')).html(a === b ? '<i class="fas fa-check me-1"></i>Şifreler eşleşiyor' : '<i class="fas fa-times me-1"></i>Şifreler eşleşmiyor'); }
            $('#yeniSifre').on('input', guc); $('#sifreTekrar').on('input', eslesme);
            $('#sifreForm').on('submit', function () { if ($(this).valid && !$(this).valid()) return; $('#btnKaydet').addClass('loading'); });
        });
