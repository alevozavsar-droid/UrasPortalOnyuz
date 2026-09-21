// Ana sayfa komuta merkezi efektleri (Home/Index) - acik tema.
/* ===== Komuta merkezi efektleri: saat, sayaç, sloganlar, aurora, ışık/eğilme, sıralı belirme ===== */
        (function () {
            // Canlı saat
            var saatEl = document.getElementById('heroSaat');
            function saat() { if (saatEl) saatEl.textContent = new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' }); }
            saat(); setInterval(saat, 1000);

            // Sayaç animasyonu
            document.querySelectorAll('.sayac').forEach(function (el) {
                var hedef = parseInt(el.dataset.hedef) || 0, bas = null;
                function adim(t) { if (!bas) bas = t; var k = Math.min((t - bas) / 1200, 1); el.textContent = Math.round(hedef * (1 - Math.pow(1 - k, 3))); if (k < 1) requestAnimationFrame(adim); }
                requestAnimationFrame(adim);
            });

            // Dönen sloganlar
            var sloganlar = ['Bugün hangi rapora bakıyoruz?', 'Cari ekstre, adat ve vade farkı bir tık uzağınızda.', 'Çek ibrazı ve tahsilatlar tek ekranda.', 'İpucu: "/" tuşu ile hemen arayın.'];
            var tag = document.getElementById('heroTagline'), si = 0;
            if (tag) setInterval(function () { si = (si + 1) % sloganlar.length; tag.innerHTML = '<span>' + sloganlar[si] + '</span>'; }, 5000);

            // Hero aurora parçacıkları
            var c = document.getElementById('heroCanvas');
            if (c && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
                var x = c.getContext('2d'), w, h, p = [], DPR = Math.min(devicePixelRatio || 1, 1.5), raf;
                function boyut() { w = c.width = c.offsetWidth * DPR; h = c.height = c.offsetHeight * DPR; p = []; for (var i = 0; i < 45; i++) p.push({ x: Math.random() * w, y: Math.random() * h, vx: (Math.random() - .5) * .3 * DPR, vy: (Math.random() - .5) * .3 * DPR, r: (Math.random() * 1.5 + .5) * DPR }); }
                function kare() {
                    x.clearRect(0, 0, w, h); x.lineWidth = DPR;
                    for (var i = 0; i < p.length; i++) {
                        var a = p[i]; a.x += a.vx; a.y += a.vy; if (a.x < 0 || a.x > w) a.vx *= -1; if (a.y < 0 || a.y > h) a.vy *= -1;
                        x.beginPath(); x.arc(a.x, a.y, a.r, 0, Math.PI * 2); x.fillStyle = 'rgba(37,99,235,.35)'; x.fill();
                        for (var j = i + 1; j < p.length; j++) { var b = p[j], dx = a.x - b.x, dy = a.y - b.y, d = dx * dx + dy * dy, m = (130 * DPR) * (130 * DPR); if (d < m) { x.strokeStyle = 'rgba(37,99,235,' + (.12 * (1 - d / m)).toFixed(3) + ')'; x.beginPath(); x.moveTo(a.x, a.y); x.lineTo(b.x, b.y); x.stroke(); } }
                    }
                    raf = requestAnimationFrame(kare);
                }
                boyut(); addEventListener('resize', boyut); raf = requestAnimationFrame(kare);
                document.addEventListener('visibilitychange', function () { if (document.hidden) cancelAnimationFrame(raf); else raf = requestAnimationFrame(kare); });
            }

            // İmleç ışığı + 3B eğilme
            document.querySelectorAll('.dashboard-chart-card, .dashboard-widget, .report-card').forEach(function (k) {
                k.classList.add('spot');
                k.addEventListener('mousemove', function (e) {
                    var r = k.getBoundingClientRect(), mx = e.clientX - r.left, my = e.clientY - r.top;
                    k.style.setProperty('--mx', mx + 'px'); k.style.setProperty('--my', my + 'px');
                });
                            });

            // Sıralı belirme (görünür olunca)
            var kolonlar = document.querySelectorAll('.report-item-col');
            if ('IntersectionObserver' in window) {
                var go = new IntersectionObserver(function (girisler) {
                    girisler.forEach(function (g, i) { if (g.isIntersecting) { g.target.style.transitionDelay = (i % 8) * 60 + 'ms'; g.target.classList.add('goster'); go.unobserve(g.target); } });
                }, { rootMargin: '0px 0px -5% 0px' });
                kolonlar.forEach(function (k) { go.observe(k); });
            } else kolonlar.forEach(function (k) { k.classList.add('goster'); });

            // Kayan şerit: tıklayınca ilgili kategori filtresi
            document.querySelectorAll('.ticker-item').forEach(function (t) {
                t.addEventListener('click', function () {
                    var pill = document.querySelector('.category-pill[data-group="' + t.dataset.group.replace(/"/g, '\\"') + '"]');
                    if (pill) { pill.click(); pill.scrollIntoView({ behavior: 'smooth', block: 'center', inline: 'center' }); }
                });
            });

            // Hero KPI'ları dashboard verisiyle doldur (mevcut widget metinlerini izler)
            function esle(kaynakId, hedefId) {
                var k = document.getElementById(kaynakId), h = document.getElementById(hedefId); if (!k || !h) return;
                var yaz = function () { h.textContent = k.textContent === 'Yükleniyor...' ? '…' : k.textContent; };
                yaz(); new MutationObserver(yaz).observe(k, { childList: true, characterData: true, subtree: true });
            }
            esle('w-portfoy', 'hk-portfoy'); esle('w-bekleyen', 'hk-bekleyen'); esle('kdvNetText', 'hk-kdv');
        })();
