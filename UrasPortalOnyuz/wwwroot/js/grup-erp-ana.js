// ============================================================
// ANA SAYFA — Grup ERP Portalı › index.html betiğinin uyarlaması.
// Yaklaşan izinler, doğum günleri, şirket içi mesajlar (sohbet + AI asistan), takipteki görevler.
// Veriler örnektir (Grup ERP'deki gibi); mesajlar grup-mesaj-data.js'ten gelir.
// Kart bağlantıları (data-sekme) kabuğun sekmesinde açılır; kabuk yoksa normal gezinir.
// ============================================================
(() => {
  const root = document.getElementById('ghAna');
  if (!root) return;
  const $ = s => document.querySelector(s);
  const esc = s => String(s == null ? '' : s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  const bas = ad => ad.split(/\s+/).slice(0, 2).map(p => p[0]).join('').toUpperCase();
  const renk = i => 'p' + (i % 6 + 1);
  const TR_LOWER = s => String(s).replace(/İ/g, 'i').replace(/I/g, 'ı').toLocaleLowerCase('tr-TR');

  // ---- Bağlantılar: kabuk sekmesinde aç ----
  root.addEventListener('click', e => {
    const bos = e.target.closest('a[href="#"]');
    if (bos) { e.preventDefault(); return; }   // <base href="/"> yüzünden "#" köke gidiyordu
    const a = e.target.closest('a[data-sekme]');
    if (!a || e.ctrlKey || e.metaKey || e.shiftKey) return;
    const href = a.getAttribute('href');
    if (!href) return;
    if (window.portalSekmeAc) { e.preventDefault(); window.portalSekmeAc(a.dataset.sekme || a.textContent.trim(), href); }
  });

  // ---- Yaklaşan izinler ----
  const izinler = [
    ['Serdar Sabri Aydın', 'Operasyon', 'Yıllık izin', '16 - 22 Eyl', 5, 'Bugün', 'red'],
    ['Ayşegül Zenit Ula', 'Finans - Tahsilat', 'Yıllık izin', '18 - 19 Eyl', 2, '2 gün sonra', 'amber'],
    ['Berkan Özkan', 'İhracat', 'Mazeret izni', '22 Eyl', 1, '6 gün sonra', ''],
    ['Melike Ural Kaya', 'Finans - Tahsilat', 'Yıllık izin', '28 Eyl - 9 Eki', 10, '12 gün sonra', ''],
    ['Özge İsen', 'Proje Yönetimi', 'Yıllık izin', '5 - 7 Eki', 3, '19 gün sonra', ''],
    ['Bedirhan Genç', 'Kalite Sistem', 'Rapor', '8 Eki', 1, '22 gün sonra', ''],
    ['Ahmet Özkan', 'Mali İşler', 'Yıllık izin', '12 - 16 Eki', 5, '26 gün sonra', ''],
    ['Sevgi Kaya', 'Satış', 'Yıllık izin', '26 - 30 Eki', 5, '40 gün sonra', '']
  ];
  $('#ghIzinOzet').textContent = `Bu hafta ${izinler.filter(i => i[6]).length} kişi izinde`;
  $('#ghYaklasanIzin').innerHTML = izinler.map(([ad, birim, tur, aralik, gun, ne, cls], i) => `<div class="satir" style="padding:5px 0"><span class="av ${renk(i)}" style="width:28px;height:28px;font-size:10.5px">${esc(bas(ad))}</span><div class="grow"><div class="t"><a href="#">${esc(ad)}</a></div><div class="s">${esc(birim)} · ${esc(tur)}</div></div><div class="r"><div><b>${esc(aralik)}</b> · ${gun} gün</div><div style="margin-top:2px"><span class="tag ${cls}${cls === 'red' ? ' dot' : ''}">${esc(ne)}</span></div></div></div>`).join('');

  // ---- Yaklaşan doğum günleri ----
  const dogum = [['Dinçer Özdemir', 'Yarın'], ['Savaş Özcan', '19 Eyl'], ['Bedirhan Genç', '22 Eyl'], ['Ramin Musayev', '4 Eki'], ['Mesut Akıncı', '6 Eki'], ['İnanç Bozkurt', '6 Eki'],
    ['Handan Yıldırım', '10 Eki'], ['Refik Polat', '10 Eki'], ['Selçuk Mert', '11 Eki'], ['Sevgi Kaya', '13 Eki'], ['Kerem Aksoy', '18 Eki'], ['Nazlı Erdem', '21 Eki'], ['Tolga Yaman', '2 Kas'], ['Gizem Tan', '9 Kas']];
  let dogumN = 10;
  const dogumCiz = () => { $('#ghDogum').innerHTML = dogum.slice(0, dogumN).map(([ad, t], i) => `<div class="satir"><span class="av ${renk(i + 2)}">${esc(bas(ad))}</span><div class="grow"><div class="t"><a href="#">${esc(ad)}</a></div></div><div class="r"><b>${esc(t)}</b></div></div>`).join(''); $('#ghDogumDaha').hidden = dogumN >= dogum.length; };
  $('#ghDogumDaha').addEventListener('click', e => { e.preventDefault(); dogumN += 10; dogumCiz(); });
  dogumCiz();

  // ---- Şirket içi mesajlar (grup-mesaj-data.js) ----
  const mesajlar = window.MESAJLAR || [], YANITLAR = window.MESAJ_YANITLARI || ['Teşekkürler, inceleyip döneceğim.'];
  const saatSimdi = () => new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
  let aiMode = false, acikSohbet = null, yazmaZamanlayici = null;
  function mesajCiz(f) {
    aiMode = f === 'ai';
    $('#ghMesajlar').hidden = aiMode; $('#ghAsistan').hidden = !aiMode;
    if (aiMode) { $('#ghMsgInput').placeholder = 'Asistana sorun… (ör. "Çek raporları nerede?")'; $('#ghMsgInput').disabled = false; $('#ghMsgInput').focus(); return; }
    if (acikSohbet) { sohbetCiz(); return; }
    $('#ghMsgInput').placeholder = 'Yanıtlamak için bir sohbet seçin…'; $('#ghMsgInput').disabled = true;
    $('#ghMesajlar').innerHTML = mesajlar.filter(m => !f || m.tip === f).map(m => `<div class="satir sohbet" data-ad="${esc(m.ad)}"><span class="av ${m.cls}">${esc(m.av)}</span><div class="grow"><div class="t">${esc(m.ad)} ${m.yeni ? `<span class="rozet">${m.yeni}</span>` : ''}</div><div class="s">${esc(m.son)}</div></div><div class="r ${m.y ? 'new' : ''}">${esc(m.z)}</div></div>`).join('') || '<div class="empty">Mesaj yok.</div>';
    document.querySelectorAll('#ghMesajlar .sohbet').forEach(r => r.addEventListener('click', () => sohbetAc(r.dataset.ad)));
  }
  function sohbetAc(ad) {
    acikSohbet = mesajlar.find(m => m.ad === ad); if (!acikSohbet) return;
    acikSohbet.yeni = 0; acikSohbet.y = false;
    sohbetCiz(); $('#ghMsgInput').focus();
  }
  function sohbetCiz(yaziyor = false) {
    const m = acikSohbet, duyuru = m.tip === 'duyuru';
    $('#ghMsgInput').disabled = duyuru; $('#ghMsgInput').placeholder = duyuru ? 'Duyurulara yanıt verilemez' : `${m.ad} sohbetine yaz…`;
    $('#ghMesajlar').innerHTML = `<div class="thread-hd"><button type="button" class="back" id="ghSohbetGeri">‹</button><span class="av ${m.cls}">${esc(m.av)}</span><div class="grow"><div class="t">${esc(m.ad)}</div><div class="s">${duyuru ? 'Duyuru kanalı' : m.tip === 'grup' ? 'Grup sohbeti' : 'Direkt mesaj'}${yaziyor ? ' · <span class="typing">yazıyor…</span>' : ''}</div></div></div>
      <div class="log">${m.gecmis.map(x => `<div class="bubble ${x.kim === 'Ben' ? 'user' : 'them'}"><div><div class="txt">${esc(x.txt)}</div><div class="meta">${x.kim !== 'Ben' && m.tip !== 'dm' ? esc(x.kim) + ' · ' : ''}${esc(x.z)}</div></div></div>`).join('')}${yaziyor ? '<div class="bubble them"><div class="txt dots"><i></i><i></i><i></i></div></div>' : ''}</div>`;
    $('#ghSohbetGeri').addEventListener('click', () => { acikSohbet = null; clearTimeout(yazmaZamanlayici); mesajCiz($('#ghTabs span.on').dataset.f); });
    $('#ghMesajlar').scrollTop = $('#ghMesajlar').scrollHeight;
  }
  function sohbeteGonder(v) {
    const m = acikSohbet; m.gecmis.push({ kim: 'Ben', txt: v, z: saatSimdi() }); m.son = 'Ben: ' + v; m.z = 'Şimdi'; m.y = false;
    sohbetCiz();
    clearTimeout(yazmaZamanlayici);
    yazmaZamanlayici = setTimeout(() => { if (acikSohbet !== m) return; sohbetCiz(true); yazmaZamanlayici = setTimeout(() => {
      if (acikSohbet !== m) return;
      const kim = m.tip === 'dm' ? m.ad : (m.gecmis.find(x => x.kim !== 'Ben') || {}).kim || m.ad;
      const cevap = YANITLAR[m.gecmis.length % YANITLAR.length];
      m.gecmis.push({ kim, txt: cevap, z: saatSimdi() }); m.son = (m.tip === 'dm' ? '' : kim + ': ') + cevap; m.z = 'Şimdi';
      sohbetCiz();
    }, 1400 + Math.random() * 1200); }, 900);
  }
  mesajCiz('');
  document.querySelectorAll('#ghTabs span').forEach(t => t.addEventListener('click', () => { document.querySelectorAll('#ghTabs span').forEach(x => x.classList.remove('on')); t.classList.add('on'); if (acikSohbet) { acikSohbet = null; clearTimeout(yazmaZamanlayici); } mesajCiz(t.dataset.f); }));
  $('#ghMsgSend').addEventListener('click', () => {
    const v = $('#ghMsgInput').value.trim(); if (!v) return;
    if (aiMode) { $('#ghMsgInput').value = ''; asistanaSor(v); return; }
    if (!acikSohbet || acikSohbet.tip === 'duyuru') return;
    $('#ghMsgInput').value = ''; sohbeteGonder(v);
  });
  $('#ghMsgInput').addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); $('#ghMsgSend').click(); } });
  $('#ghAiChips').addEventListener('click', e => { const b = e.target.closest('button[data-q]'); if (b) asistanaSor(b.dataset.q); });

  // ---- Takipteki görevler (örnek; Grup ERP'de hukuk ve tedarikçi talebi verisinden türetiliyordu) ----
  const gorevler = [
    { t: 'Sorumlu atayın: HS-2026-007 ABC Tekstil', b: 'Hukuk', due: 'Bugün', late: true, href: '/Modul/hukuk?yol=dava-takip/03', ad: 'Dava, Tahkim ve Soruşturma' },
    { t: 'Acil tedarikçi talebini değerlendirin: Anadolu Metal', b: 'Satın Alma', due: 'Bugün', late: true, href: '/Modul/talep?yol=yeni-talep/tedarikci', ad: 'Tedarikçi Talebi' },
    { t: '90 günü aşan süreci gözden geçirin: HS-2026-003 Alacak davası', b: 'Hukuk', due: '104 gündür açık', late: true, href: '/Modul/hukuk?yol=dava-takip/04', ad: 'İcra, Haciz ve Tahsilat' },
    { t: 'Eş cari kodunu tamamlayın: Deniz Boya Kimya', b: 'Muhasebe', due: 'Bu hafta', href: '/Rapor120', ad: 'Cari Bilgileri' },
    { t: 'Hadımköy depo kira ek protokolünü inceleyin', b: 'Hukuk', due: '25 Eyl', href: '/Modul/hukuk?yol=sozlesme-uyum/11', ad: 'Sözleşmeler' },
    { t: 'Eylül ayı yönetim raporunu onaylayın', b: 'Yönetim', due: '30 Eyl', href: '/Modul/yonetim?yol=yonetim-ozeti/sirket-grup-performansi', ad: 'Şirket ve Grup Performansı' },
    { t: 'Anadolu Metal tedarik sözleşmesi yenileme kararı', b: 'Satın Alma', due: '30 Kas', href: '/Modul/hukuk?yol=sozlesme-uyum/11', ad: 'Sözleşmeler' }
  ];
  $('#ghGorevSayi').textContent = `${gorevler.length} açık görev`;
  $('#ghGorevler').innerHTML = gorevler.map((g, i) => `<div class="satir" data-g="${i}"><span class="chk">✓</span><div class="grow"><div class="t"><a href="${esc(g.href)}" data-sekme="${esc(g.ad)}" style="color:inherit">${esc(g.t)}</a></div><div class="meta"><span class="tag">${esc(g.b)}</span><span class="due ${g.late ? 'late' : ''}">${esc(g.due)}</span></div></div></div>`).join('');
  document.querySelectorAll('#ghGorevler .chk').forEach(c => c.addEventListener('click', () => c.closest('.satir').classList.toggle('done')));

  // ---- AI Asistan (yerel yanıtlar: menü yapısı, görevler, izinler, mesajlar) ----
  const ARAMA = [];
  (window.MENU_YAPISI || []).forEach(m => {
    if (!m.alt) return;
    const kok = '/Modul/' + m.key;
    m.alt.forEach(a => {
      const gruplar = a.alt ? a.alt.map(b => ({ b, yol: `${m.ad} › ${a.ad}`, yolKey: `${a.key}/${b.key}` })) : [{ b: a, yol: m.ad, yolKey: a.key }];
      gruplar.forEach(({ b, yol, yolKey }) => (b.raporlar || []).forEach((r, i) => ARAMA.push({ ad: r.ad, yol: `${yol} › ${b.ad}`, href: `${kok}?yol=${yolKey}/r${i}` })));
    });
  });
  const mdLite = t => { let h = esc(t).replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, (m, a, u) => /^(https?:\/\/|[\w./?=&#%-]+)$/.test(u) ? `<a href="${u}" data-sekme="${a}">${a}</a>` : a).replace(/\*\*([^*]+)\*\*/g, '<b>$1</b>'); const satirlar = h.split('\n'); let out = '', liste = false; for (const s of satirlar) { const m = s.match(/^\s*[-•*]\s+(.*)$/); if (m) { if (!liste) { out += '<ul>'; liste = true; } out += `<li>${m[1]}</li>`; } else { if (liste) { out += '</ul>'; liste = false; } out += (out && !out.endsWith('</ul>') ? '\n' : '') + s; } } if (liste) out += '</ul>'; return out; };
  function balon(rol, html, cls = '') { const d = document.createElement('div'); d.className = `bubble ${rol}`; d.innerHTML = `${rol === 'bot' ? '<span class="ai-av">✨</span>' : ''}<div class="txt ${cls}">${html}</div>`; $('#ghAiLog').appendChild(d); $('#ghAsistan').scrollTop = $('#ghAsistan').scrollHeight; return d.querySelector('.txt'); }
  function yerelCevap(q) {
    const ql = TR_LOWER(q);
    if (/onay/.test(ql)) return 'Onayınızı bekleyen kayıtlar Onay Merkezi\'nde toplanır:\n- [Onay Ekranı](/Modul/onaylarim?yol=genel-onaylar/onay-merkezi)\n- [Satınalma Talebi Onay Ekranı](/Modul/onaylarim?yol=satin-alma-onaylari/talep-onaylari)\n- [Gider Onay Ekranı](/Modul/onaylarim?yol=mali-isler-onaylari/gider-kod-onaylari)';
    if (/görev|gorev/.test(ql)) return `${gorevler.length} açık göreviniz var:\n${gorevler.slice(0, 6).map(g => `- [${g.t}](${g.href}) · ${g.b} · ${g.due}`).join('\n')}`;
    if (/izin/.test(ql)) return `Yaklaşan izinler:\n${izinler.slice(0, 6).map(i => `- **${i[0]}** · ${i[2]} · ${i[3]} (${i[4]} gün)`).join('\n')}`;
    if (/mesaj|duyuru/.test(ql)) return `Okunmamış: ${mesajlar.filter(m => m.yeni).map(m => `**${m.ad}** (${m.yeni})`).join(', ') || 'yok'}.`;
    const kelimeler = ql.split(/\s+/).filter(k => k.length > 2 && !/^(nerede|nerde|raporu|rapor|ekranı|ekran|aç|göster|bul|hangi|nasıl|için|olan|var|mı|mi|mu|mü)$/.test(k));
    const bul = ARAMA.filter(x => kelimeler.length && kelimeler.every(k => TR_LOWER(x.ad + ' ' + x.yol).includes(k))).slice(0, 6);
    if (bul.length) return `Şunları buldum:\n${bul.map(x => `- [${x.ad}](${x.href}) · ${x.yol}`).join('\n')}`;
    return 'Bunu şu an yanıtlayamıyorum. Rapor veya ekran adı yazarsanız menüde bulurum; "onay", "görev", "izin" veya "mesaj" diye sorabilirsiniz.';
  }
  function asistanaSor(q) {
    balon('user', esc(q));
    const out = balon('bot', 'Düşünüyor…', 'thinking');
    setTimeout(() => { out.classList.remove('thinking'); out.innerHTML = mdLite(yerelCevap(q)); $('#ghAsistan').scrollTop = $('#ghAsistan').scrollHeight; }, 350);
  }
})();
