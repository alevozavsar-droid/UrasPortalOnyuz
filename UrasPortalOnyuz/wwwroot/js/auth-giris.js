// Login sayfasi betikleri.
// ---------- Hareketli arka plan (aurora + bağlantılı parçacıklar) ----------
        (function () {
            const canvas = document.getElementById('bgCanvas');
            if (!canvas || window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
            const ctx = canvas.getContext('2d');
            let w, h, parts = [], blobs = [], raf;
            const DPR = Math.min(window.devicePixelRatio || 1, 1.5);

            function resize() {
                w = canvas.width = Math.floor(innerWidth * DPR); h = canvas.height = Math.floor(innerHeight * DPR);
                canvas.style.width = innerWidth + 'px'; canvas.style.height = innerHeight + 'px';
                const n = Math.min(90, Math.floor(innerWidth * innerHeight / 16000));
                parts = Array.from({ length: n }, () => ({ x: Math.random() * w, y: Math.random() * h, vx: (Math.random() - .5) * .35 * DPR, vy: (Math.random() - .5) * .35 * DPR, r: (Math.random() * 1.6 + .6) * DPR }));
                blobs = [
                    { x: .2, y: .3, r: .45, c: 'rgba(59,130,246,.35)', s: .00025, p: 0 },
                    { x: .8, y: .7, r: .5, c: 'rgba(14,165,233,.30)', s: .0002, p: 2 },
                    { x: .55, y: .15, r: .35, c: 'rgba(129,140,248,.28)', s: .0003, p: 4 }
                ];
            }
            function frame(t) {
                ctx.clearRect(0, 0, w, h);
                // aurora lekeleri
                blobs.forEach(b => {
                    const cx = (b.x + Math.sin(t * b.s + b.p) * .08) * w, cy = (b.y + Math.cos(t * b.s * 1.3 + b.p) * .08) * h, r = b.r * Math.max(w, h) * .6;
                    const g = ctx.createRadialGradient(cx, cy, 0, cx, cy, r); g.addColorStop(0, b.c); g.addColorStop(1, 'rgba(0,0,0,0)');
                    ctx.fillStyle = g; ctx.fillRect(cx - r, cy - r, r * 2, r * 2);
                });
                // parcaciklar ve baglantilar
                ctx.lineWidth = 1 * DPR;
                for (let i = 0; i < parts.length; i++) {
                    const p = parts[i]; p.x += p.vx; p.y += p.vy;
                    if (p.x < 0 || p.x > w) p.vx *= -1; if (p.y < 0 || p.y > h) p.vy *= -1;
                    ctx.beginPath(); ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2); ctx.fillStyle = 'rgba(255,255,255,.55)'; ctx.fill();
                    for (let j = i + 1; j < parts.length; j++) {
                        const q = parts[j], dx = p.x - q.x, dy = p.y - q.y, d = dx * dx + dy * dy, max = (150 * DPR) ** 2;
                        if (d < max) { ctx.strokeStyle = 'rgba(255,255,255,' + (0.14 * (1 - d / max)).toFixed(3) + ')'; ctx.beginPath(); ctx.moveTo(p.x, p.y); ctx.lineTo(q.x, q.y); ctx.stroke(); }
                    }
                }
                raf = requestAnimationFrame(frame);
            }
            resize(); addEventListener('resize', resize); raf = requestAnimationFrame(frame);
            document.addEventListener('visibilitychange', () => { if (document.hidden) cancelAnimationFrame(raf); else raf = requestAnimationFrame(frame); });
        })();

        // ---------- Sol slaytlar ----------
        (function () {
            const slides = document.querySelectorAll('#slides .slide'), dots = document.querySelectorAll('#dots .dot');
            if (!slides.length) return;
            let i = 0, timer;
            function go(n) {
                slides[i].classList.remove('active'); dots[i].classList.remove('active');
                i = (n + slides.length) % slides.length;
                slides[i].classList.add('active');
                // ilerleme cubugunu sifirdan baslat
                const d = dots[i]; d.classList.remove('active'); void d.offsetWidth; d.classList.add('active');
                clearTimeout(timer); timer = setTimeout(() => go(i + 1), 5000);
            }
            dots.forEach((d, k) => d.addEventListener('click', () => go(k)));
            timer = setTimeout(() => go(1), 5000);
        })();

        $(document).ready(function () {
            // --- Şifre Değiştirme Linki Parametre Aktarımı ---
            $('#changePasswordLink').on('click', function (e) {
                var userCode = $('#userCodeInput').val();
                if (!userCode) {
                    e.preventDefault();
                    alert('Şifre belirlemek için lütfen önce "Kullanıcı Kodu" alanını doldurun.');
                    $('#userCodeInput').focus();
                } else {
                    var baseUrl = $(this).attr('href').split('?')[0];
                    $(this).attr('href', baseUrl + '?userCode=' + encodeURIComponent(userCode));
                }
            });

            // --- Şifre göster/gizle ---
            $('#togglePassword').on('click', function () {
                var inp = $('#passwordInput'), ico = $(this).find('i');
                var goster = inp.attr('type') === 'password';
                inp.attr('type', goster ? 'text' : 'password');
                ico.toggleClass('fa-eye', !goster).toggleClass('fa-eye-slash', goster);
            });

            // --- Caps Lock uyarısı ---
            $('#passwordInput').on('keyup keydown', function (e) {
                if (typeof e.originalEvent.getModifierState === 'function')
                    $('#capsHint').toggle(e.originalEvent.getModifierState('CapsLock'));
            }).on('blur', function () { $('#capsHint').hide(); });

            // --- Beni hatırla: kullanıcı kodu bu tarayıcıda saklanır, sonraki girişte hazır gelir ---
            try {
                var hatirlanan = localStorage.getItem('girisKullaniciKodu');
                if (hatirlanan && !$('#userCodeInput').val()) {
                    $('#userCodeInput').val(hatirlanan);
                    $('#rememberMeCheck').prop('checked', true);
                    $('#passwordInput').focus();
                }
            } catch (e) { }

            // --- Gönderim sırasında yükleniyor durumu ---
            $('#loginForm').on('submit', function () {
                if ($(this).valid && !$(this).valid()) return;
                try {
                    if ($('#rememberMeCheck').is(':checked')) localStorage.setItem('girisKullaniciKodu', $('#userCodeInput').val().trim());
                    else localStorage.removeItem('girisKullaniciKodu');
                } catch (e) { }
                $('#btnLogin').addClass('loading');
            });

            // --- TEMA (DARK/LIGHT) MANTIĞI ---
            const themeToggleBtn = document.getElementById('themeToggleBtn');
            const currentTheme = localStorage.getItem('theme') || 'light';
            document.documentElement.setAttribute('data-theme', currentTheme);
            updateThemeIcon(currentTheme);

            themeToggleBtn.addEventListener('click', () => {
                let theme = document.documentElement.getAttribute('data-theme');
                let switchToTheme = theme === 'light' ? 'dark' : 'light';
                document.documentElement.setAttribute('data-theme', switchToTheme);
                localStorage.setItem('theme', switchToTheme);
                updateThemeIcon(switchToTheme);
            });

            function updateThemeIcon(theme) {
                const icon = themeToggleBtn.querySelector('i');
                if (theme === 'dark') { icon.classList.remove('fa-moon'); icon.classList.add('fa-sun', 'text-warning'); }
                else { icon.classList.remove('fa-sun', 'text-warning'); icon.classList.add('fa-moon'); }
            }
        });
