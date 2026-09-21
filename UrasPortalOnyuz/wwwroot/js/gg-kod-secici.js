/*
 * Gider / Gelir kod seçici (Rapor164 kataloğu)
 * ---------------------------------------------
 * Fatura, yevmiye ve seçim ekranlarında kullanılan ortak, bağımlılıksız seçim penceresi.
 * Ana Grup → Alt Grup → Kalem sırasıyla tıklanarak ya da üstteki kutuya yazarak arama ile seçim yapılır.
 * Son seçilen kodlar tarayıcıda (localStorage) tutulur ve hızlı seçim için üstte gösterilir.
 *
 * Kullanım:
 *   GGSecici.ac({ kodlar: [...], tip: 'GIDER'|'GELIR'|'TIS'|null, secili: 'GDR.01.01.01.07', onSec: function (kod, kalem) {} });
 *   kodlar: /Rapor164/Kodlar çıktısı ({tip, kokKod, kod, anaGrup, altGrup, kalem, ad}).
 *
 * Fatura/yevmiye ekranlarında ".input-gider" seçim kutusunun yanındaki ".btn-gg-sec" düğmesi otomatik bağlanır:
 * satırdaki hesap 6 ile başlıyorsa gelir, 7 ile başlıyorsa gider listesi açılır; hesap bilinmiyorsa hepsi.
 */
(function () {
    if (window.GGSecici) return;
    var CSS = '\
.ggs-bg{position:fixed;inset:0;background:rgba(15,23,42,.5);z-index:2000;display:none;align-items:center;justify-content:center;font-family:inherit}\
.ggs-bg.on{display:flex}\
.ggs{background:#fff;color:#0f172a;border-radius:14px;width:min(1100px,96vw);height:min(720px,92vh);display:flex;flex-direction:column;box-shadow:0 24px 70px -20px rgba(0,0,0,.5);overflow:hidden}\
.ggs-head{display:flex;align-items:center;gap:12px;padding:12px 16px;border-bottom:1px solid #e5e7eb;background:#f8fafc}\
.ggs-head h3{margin:0;font-size:1rem;font-weight:700;flex:1;white-space:nowrap}\
.ggs-tabs{display:flex;gap:6px}\
.ggs-tab{border:1px solid #cbd5e1;background:#fff;border-radius:20px;padding:5px 14px;font-size:.8rem;font-weight:700;cursor:pointer;color:#475569}\
.ggs-tab.gider.on{background:#fee2e2;border-color:#fca5a5;color:#b91c1c}\
.ggs-tab.gelir.on{background:#dcfce7;border-color:#86efac;color:#166534}\
.ggs-tab.tis.on{background:#ede9fe;border-color:#c4b5fd;color:#6d28d9}\
.ggs-x{border:none;background:#e2e8f0;border-radius:8px;width:32px;height:32px;cursor:pointer;font-size:1rem;color:#334155}\
.ggs-ara{padding:10px 16px;border-bottom:1px solid #eef2f7;display:flex;gap:10px;align-items:center;flex-wrap:wrap}\
.ggs-ara input{flex:1;min-width:240px;border:1px solid #cbd5e1;border-radius:9px;padding:9px 12px;font-size:.95rem}\
.ggs-ara input:focus{outline:2px solid #93c5fd;border-color:#60a5fa}\
.ggs-son{display:flex;gap:6px;flex-wrap:wrap;align-items:center;font-size:.76rem;color:#64748b}\
.ggs-chip{border:1px solid #bfdbfe;background:#eff6ff;color:#1d4ed8;border-radius:20px;padding:3px 10px;font-size:.76rem;cursor:pointer;max-width:320px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}\
.ggs-chip:hover{background:#dbeafe}\
.ggs-oneri{padding:8px 16px;border-bottom:1px solid #eef2f7;display:flex;gap:8px;align-items:center;flex-wrap:wrap;font-size:.78rem;color:#92400e;background:#fffbeb}\
.ggs-oneri .ggs-chip{border-color:#fde68a;background:#fef3c7;color:#92400e}\
.ggs-oneri .ggs-chip:hover{background:#fde68a}\
.ggs-yol{padding:6px 16px;font-size:.8rem;color:#475569;border-bottom:1px solid #eef2f7;min-height:30px}\
.ggs-yol b{color:#0f172a}\
.ggs-cols{flex:1;display:grid;grid-template-columns:1fr 1fr 1.3fr;min-height:0}\
.ggs-col{border-right:1px solid #eef2f7;display:flex;flex-direction:column;min-height:0}\
.ggs-col:last-child{border-right:none}\
.ggs-col h4{margin:0;padding:8px 14px;font-size:.7rem;text-transform:uppercase;letter-spacing:.4px;color:#64748b;background:#f8fafc;border-bottom:1px solid #eef2f7}\
.ggs-list{overflow-y:auto;flex:1;padding:6px}\
.ggs-it{display:flex;align-items:center;gap:8px;padding:9px 10px;border-radius:9px;cursor:pointer;font-size:.86rem;line-height:1.25;border:1px solid transparent}\
.ggs-it:hover{background:#f1f5f9}\
.ggs-it.on{background:#eff6ff;border-color:#bfdbfe;color:#1d4ed8;font-weight:700}\
.ggs-it .no{font-family:Consolas,monospace;font-size:.72rem;color:#94a3b8;min-width:22px}\
.ggs-it .cnt{margin-left:auto;font-size:.68rem;background:#e2e8f0;color:#475569;border-radius:20px;padding:1px 7px}\
.ggs-it .kod{font-family:Consolas,monospace;font-size:.74rem;color:#1d4ed8;margin-left:auto;white-space:nowrap}\
.ggs-it.secili{background:#dcfce7;border-color:#86efac}\
.ggs-sonuc{flex:1;overflow-y:auto;padding:8px 12px}\
.ggs-sonuc .ggs-it{border-bottom:1px solid #f1f5f9;border-radius:0}\
.ggs-sonuc .yol{font-size:.72rem;color:#64748b;display:block}\
.ggs-bos{padding:30px;text-align:center;color:#94a3b8;font-size:.86rem}\
.ggs-foot{padding:8px 16px;border-top:1px solid #e5e7eb;font-size:.76rem;color:#64748b;display:flex;justify-content:space-between;align-items:center;gap:10px;flex-wrap:wrap}\
.ggs-temizle{border:1px solid #fca5a5;background:#fff;color:#b91c1c;border-radius:8px;padding:5px 12px;font-size:.78rem;font-weight:600;cursor:pointer}\
@media (max-width:760px){.ggs-cols{grid-template-columns:1fr;overflow-y:auto}.ggs-col{border-right:none;border-bottom:1px solid #eef2f7;max-height:34vh}}\
';
    var TIP_AD = { GIDER: "Gider", GELIR: "Gelir", TIS: "Teşvik / İstisna" };
    var st = { kodlar: [], tip: 'GIDER', tipler: ['GIDER', 'GELIR', 'TIS'], ana: null, alt: null, secili: '', onSec: null, q: '', onerilen: [] };
    var el = {};

    function esc(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }
    function norm(s) { return String(s || '').toLocaleLowerCase('tr-TR').replace(/i̇/g, 'i'); }
    function kur() {
        if (el.bg) return;
        var style = document.createElement('style'); style.textContent = CSS; document.head.appendChild(style);
        var bg = document.createElement('div'); bg.className = 'ggs-bg';
        bg.innerHTML =
            '<div class="ggs" role="dialog" aria-modal="true">' +
            '<div class="ggs-head"><h3><i class="fa-solid fa-sitemap"></i> Gider / Gelir Kodu Seç</h3><div class="ggs-tabs"></div><button type="button" class="ggs-x" title="Kapat (Esc)">✕</button></div>' +
            '<div class="ggs-ara"><input type="text" placeholder="Ara: kalem adı, alt grup, ana grup ya da kod (ör. kira, elektrik, GDR.05)..." autocomplete="off"><div class="ggs-son"></div></div>' +
            '<div class="ggs-oneri" hidden></div>' +
            '<div class="ggs-yol"></div>' +
            '<div class="ggs-cols">' +
            '<div class="ggs-col"><h4>1. Ana Grup</h4><div class="ggs-list c-ana"></div></div>' +
            '<div class="ggs-col"><h4>2. Alt Grup</h4><div class="ggs-list c-alt"></div></div>' +
            '<div class="ggs-col"><h4>3. Kalem (tıkla = seç)</h4><div class="ggs-list c-kalem"></div></div>' +
            '</div>' +
            '<div class="ggs-sonuc" hidden></div>' +
            '<div class="ggs-foot"><span class="ggs-bilgi">Ana grup → alt grup → kalem sırasıyla tıklayın ya da yukarıya yazarak arayın. Enter = ilk sonucu seç, Esc = kapat.</span><button type="button" class="ggs-temizle" hidden>Kodu temizle</button></div>' +
            '</div>';
        document.body.appendChild(bg);
        el.bg = bg; el.tabs = bg.querySelector('.ggs-tabs'); el.ara = bg.querySelector('.ggs-ara input'); el.son = bg.querySelector('.ggs-son');
        el.yol = bg.querySelector('.ggs-yol'); el.cols = bg.querySelector('.ggs-cols'); el.ana = bg.querySelector('.c-ana'); el.alt = bg.querySelector('.c-alt'); el.kalem = bg.querySelector('.c-kalem');
        el.sonuc = bg.querySelector('.ggs-sonuc'); el.temizle = bg.querySelector('.ggs-temizle'); el.oneri = bg.querySelector('.ggs-oneri');
        bg.querySelector('.ggs-x').addEventListener('click', kapat);
        bg.addEventListener('click', function (e) { if (e.target === bg) kapat(); });
        el.temizle.addEventListener('click', function () { sec('', null); });
        el.ara.addEventListener('input', function () { st.q = el.ara.value.trim(); render(); });
        el.ara.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { var ilk = el.sonuc.querySelector('.ggs-it'); if (ilk && !el.sonuc.hidden) { e.preventDefault(); ilk.click(); } }
        });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && el.bg.classList.contains('on')) kapat(); });
    }
    function sonlar(tip) { try { return JSON.parse(localStorage.getItem('gg_son_' + tip) || '[]'); } catch (e) { return []; } }
    function sonEkle(k) {
        try {
            var arr = sonlar(k.tip).filter(function (x) { return x !== k.kod; }); arr.unshift(k.kod);
            localStorage.setItem('gg_son_' + k.tip, JSON.stringify(arr.slice(0, 8)));
        } catch (e) { }
    }
    function liste() { return st.kodlar.filter(function (k) { return k.tip === st.tip; }); }
    function renderTabs() {
        el.tabs.innerHTML = st.tipler.map(function (t) { return '<button type="button" class="ggs-tab ' + t.toLowerCase() + (t === st.tip ? ' on' : '') + '" data-tip="' + t + '">' + esc(TIP_AD[t]) + '</button>'; }).join('');
        el.tabs.querySelectorAll('.ggs-tab').forEach(function (b) { b.addEventListener('click', function () { st.tip = b.dataset.tip; st.ana = null; st.alt = null; render(); }); });
    }
    function renderSon() {
        var arr = sonlar(st.tip).map(function (kod) { return st.kodlar.find(function (k) { return k.kod === kod; }); }).filter(Boolean);
        el.son.innerHTML = arr.length ? '<span>Son seçilenler:</span>' + arr.map(function (k) { return '<span class="ggs-chip" data-kod="' + esc(k.kod) + '" title="' + esc(k.ad) + '">' + esc(k.kalem) + '</span>'; }).join('') : '';
        el.son.querySelectorAll('.ggs-chip').forEach(function (c) { c.addEventListener('click', function () { var k = st.kodlar.find(function (x) { return x.kod === c.dataset.kod; }); if (k) sec(k.kod, k); }); });
    }
    function renderOneri() {
        var arr = (st.onerilen || []).map(function (kod) { return st.kodlar.find(function (k) { return k.kod === kod; }); }).filter(function (k) { return k && k.tip === st.tip; });
        el.oneri.hidden = !arr.length;
        el.oneri.innerHTML = arr.length ? '<span><i class="fa-solid fa-star"></i> Bu cariden önerilen:</span>' + arr.map(function (k) { return '<span class="ggs-chip" data-kod="' + esc(k.kod) + '" title="' + esc(k.ad) + '">' + esc(k.kalem) + '</span>'; }).join('') : '';
        el.oneri.querySelectorAll('.ggs-chip').forEach(function (c) { c.addEventListener('click', function () { var k = st.kodlar.find(function (x) { return x.kod === c.dataset.kod; }); if (k) sec(k.kod, k); }); });
    }
    function render() {
        renderTabs(); renderSon(); renderOneri();
        el.temizle.hidden = !st.secili;
        var L = liste();
        if (st.q) {
            var q = norm(st.q).split(/\s+/).filter(Boolean);
            var hits = L.filter(function (k) { var h = norm(k.ad + ' ' + k.kod + ' ' + k.kokKod); return q.every(function (p) { return h.indexOf(p) >= 0; }); }).slice(0, 200);
            el.cols.hidden = true; el.sonuc.hidden = false; el.yol.innerHTML = '<b>' + hits.length + '</b> sonuç' + (hits.length >= 200 ? ' (ilk 200)' : '') + ' — <b>' + esc(TIP_AD[st.tip]) + '</b>';
            el.sonuc.innerHTML = hits.length ? hits.map(function (k) {
                return '<div class="ggs-it' + (k.kod === st.secili ? ' secili' : '') + '" data-kod="' + esc(k.kod) + '"><div><b>' + esc(k.kalem) + '</b><span class="yol">' + esc(k.anaGrup) + ' › ' + esc(k.altGrup) + '</span></div><span class="kod">' + esc(k.kod) + '</span></div>';
            }).join('') : '<div class="ggs-bos">Eşleşen kalem yok. Farklı bir kelime deneyin ya da sekmeyi değiştirin.</div>';
            el.sonuc.querySelectorAll('.ggs-it').forEach(function (d) { d.addEventListener('click', function () { var k = L.find(function (x) { return x.kod === d.dataset.kod; }); sec(k.kod, k); }); });
            return;
        }
        el.cols.hidden = false; el.sonuc.hidden = true;
        // seçili kod varsa ilk açılışta onun grubuna git
        if (st.ana === null && st.secili) { var s = L.find(function (k) { return k.kod === st.secili; }); if (s) { st.ana = s.anaGrup; st.alt = s.altGrup; } }
        var analar = []; L.forEach(function (k) { if (analar.indexOf(k.anaGrup) < 0) analar.push(k.anaGrup); });
        el.ana.innerHTML = analar.map(function (a) {
            var n = L.filter(function (k) { return k.anaGrup === a; }); var no = n[0].kokKod.split('.')[1];
            return '<div class="ggs-it' + (a === st.ana ? ' on' : '') + '" data-a="' + esc(a) + '"><span class="no">' + esc(no) + '</span><span>' + esc(a) + '</span><span class="cnt">' + n.length + '</span></div>';
        }).join('') || '<div class="ggs-bos">Bu listede kalem yok.</div>';
        el.ana.querySelectorAll('.ggs-it').forEach(function (d) { d.addEventListener('click', function () { st.ana = d.dataset.a; st.alt = null; render(); }); });
        var altlar = []; L.filter(function (k) { return k.anaGrup === st.ana; }).forEach(function (k) { if (altlar.indexOf(k.altGrup) < 0) altlar.push(k.altGrup); });
        el.alt.innerHTML = st.ana ? altlar.map(function (b) {
            var n = L.filter(function (k) { return k.anaGrup === st.ana && k.altGrup === b; }); var no = n[0].kokKod.split('.')[2];
            return '<div class="ggs-it' + (b === st.alt ? ' on' : '') + '" data-b="' + esc(b) + '"><span class="no">' + esc(no) + '</span><span>' + esc(b) + '</span><span class="cnt">' + n.length + '</span></div>';
        }).join('') : '<div class="ggs-bos">← Önce ana grup seçin</div>';
        el.alt.querySelectorAll('.ggs-it').forEach(function (d) { d.addEventListener('click', function () { st.alt = d.dataset.b; render(); }); });
        var kalemler = L.filter(function (k) { return k.anaGrup === st.ana && k.altGrup === st.alt; });
        el.kalem.innerHTML = st.alt ? kalemler.map(function (k) {
            return '<div class="ggs-it' + (k.kod === st.secili ? ' secili' : '') + '" data-kod="' + esc(k.kod) + '" title="' + esc(k.ad) + '"><span class="no">' + esc(k.kokKod.split('.')[3]) + '</span><span>' + esc(k.kalem) + '</span><span class="kod">' + esc(k.kod) + '</span></div>';
        }).join('') : '<div class="ggs-bos">← Alt grup seçin</div>';
        el.kalem.querySelectorAll('.ggs-it').forEach(function (d) { d.addEventListener('click', function () { var k = L.find(function (x) { return x.kod === d.dataset.kod; }); sec(k.kod, k); }); });
        el.yol.innerHTML = '<b>' + esc(TIP_AD[st.tip]) + '</b>' + (st.ana ? ' › ' + esc(st.ana) : '') + (st.alt ? ' › ' + esc(st.alt) : '') + (st.secili ? ' &nbsp;|&nbsp; seçili: <b>' + esc(st.secili) + '</b>' : '');
    }
    function sec(kod, k) {
        if (k) sonEkle(k);
        var cb = st.onSec; kapat();
        if (typeof cb === 'function') cb(kod, k || null);
    }
    function kapat() { if (el.bg) el.bg.classList.remove('on'); }
    function ac(o) {
        kur();
        o = o || {};
        st.kodlar = o.kodlar || window.GG_KODLAR || [];
        st.onSec = o.onSec; st.secili = (o.secili || '').trim(); st.q = ''; st.ana = null; st.alt = null; st.onerilen = o.onerilen || [];
        var mevcutTipler = []; st.kodlar.forEach(function (k) { if (mevcutTipler.indexOf(k.tip) < 0) mevcutTipler.push(k.tip); });
        st.tipler = ['GIDER', 'GELIR', 'TIS'].filter(function (t) { return mevcutTipler.indexOf(t) >= 0; });
        if (o.tip === 'GIDER') st.tipler = st.tipler.filter(function (t) { return t !== 'GELIR'; });
        else if (o.tip === 'GELIR') st.tipler = st.tipler.filter(function (t) { return t !== 'GIDER'; });
        var seciliK = st.kodlar.find(function (k) { return k.kod === st.secili; });
        st.tip = seciliK ? seciliK.tip : (o.tip && st.tipler.indexOf(o.tip) >= 0 ? o.tip : st.tipler[0] || 'GIDER');
        el.bg.querySelector('h3').innerHTML = '<i class="fa-solid fa-sitemap"></i> ' + esc(o.baslik || 'Gider / Gelir Kodu Seç');
        el.ara.value = '';
        if (!st.kodlar.length) { el.yol.innerHTML = '<span style="color:#b91c1c">Kod kataloğu yüklenemedi (Rapor164). Sayfayı yenileyin.</span>'; }
        el.bg.classList.add('on');
        render();
        setTimeout(function () { el.ara.focus(); }, 30);
    }
    /* Hesap tipi (sunucudaki GiderGelirKatalog.HesapTipi ile aynı kural): 7 → GIDER; 6 → GELIR, ancak
       (-) niteliğindeki 6'lılar (61x, 62x, 63x, 65x, 66x, 68x, 691) → GIDER; kod istenmeyen hesaplar
       (620/621 Satılan Mamül/Ticari Mal Maliyeti; 761/771/781 yansıtma hesapları, mekanik karşı kayıtlar) ve diğer → null. */
    var GIDER_6 = /^(61|62|63|65[3-9]|66|68|691)/;
    var KOD_ISTENMEYEN = /^(620|621|761|771|781)/;
    function hesapTipi(hesap) {
        var h = String(hesap || '').replace(/\./g, '').trim();
        if (KOD_ISTENMEYEN.test(h)) return null;
        if (h.charAt(0) === '7') return 'GIDER';
        if (h.charAt(0) === '6') return GIDER_6.test(h) ? 'GIDER' : 'GELIR';
        return null;
    }
    /* Satırdaki hesap koduna göre liste tipi; hesap bilinmiyorsa null (hepsi). */
    function tipTahmin(sel) {
        if (sel && sel.getAttribute && sel.getAttribute('data-tip')) return sel.getAttribute('data-tip');   // satırın hesabından belirlenmiş tip
        var tr = sel && sel.closest ? sel.closest('tr') : null; if (!tr) return null;
        var acct = tr.querySelector('.acct-select, .select-account, .input-account, .hesap-select');
        return hesapTipi(acct ? acct.value : '');
    }
    // ".btn-gg-sec" düğmeleri: yanındaki ".input-gider" için pencereyi açar
    document.addEventListener('click', function (e) {
        var b = e.target.closest ? e.target.closest('.btn-gg-sec') : null; if (!b) return;
        e.preventDefault();
        var kap = b.closest('td') || b.parentElement;
        var sel = kap ? kap.querySelector('.input-gider') : null; if (!sel) return;
        ac({
            kodlar: window.GG_KODLAR || [], tip: tipTahmin(sel), secili: sel.value || '',
            onSec: function (kod) {
                if (window.ggKodSec && window.jQuery) window.ggKodSec(window.jQuery(sel), kod);
                else { sel.value = kod; sel.dispatchEvent(new Event('change', { bubbles: true })); }
            }
        });
    });
    window.GGSecici = { ac: ac, kapat: kapat, tipTahmin: tipTahmin, hesapTipi: hesapTipi };
})();
