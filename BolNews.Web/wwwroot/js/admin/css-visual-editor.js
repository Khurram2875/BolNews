/* Point-and-click styling for the Publisher Settings live preview.
   Click "Pick element", click anything in the preview, change it with simple controls.
   Each change is written as a small labelled rule into the Additional CSS editor, so everything stays visible,
   undoable (Ctrl+Z in the editor) and versioned on Save. No new storage, no raw script. */
(function () {
  const frame = document.getElementById('bnPreviewFrame');
  const toggle = document.getElementById('bnPickToggle');
  const panel = document.getElementById('bnPickPanel');
  if (!frame || !toggle || !panel) return;
  const editor = () => window.bnCssEditorInstance;
  if (!editor()) { toggle.disabled = true; toggle.title = 'The CSS editor did not load'; return; }

  const END = '/* /visual */';
  const SCOPES = [
    { id: 'all', label: 'All screen sizes', media: null },
    { id: 'phone', label: 'Phones and small tablets only (up to 767px)', media: '(max-width: 767.98px)' },
    { id: 'desktop', label: 'Larger screens only (768px and up)', media: '(min-width: 768px)' }
  ];
  const CONTROLS = [
    { group: 'Text', key: 'color', label: 'Text colour', type: 'color' },
    { group: 'Text', key: 'font-size', label: 'Font size', type: 'size', hint: '18 or 1.2rem' },
    { group: 'Text', key: 'font-weight', label: 'Weight', type: 'select', options: ['', '300', '400', '500', '600', '700', '800'] },
    { group: 'Text', key: 'line-height', label: 'Line height', type: 'size', hint: '1.6 or 28px' },
    { group: 'Text', key: 'text-align', label: 'Align', type: 'select', options: ['', 'left', 'center', 'right', 'justify'] },
    { group: 'Text', key: 'text-transform', label: 'Letter case', type: 'select', options: ['', 'none', 'uppercase', 'capitalize'] },
    { group: 'Background', key: 'background-color', label: 'Background', type: 'color' },
    { group: 'Background', key: 'opacity', label: 'Opacity', type: 'size', hint: '0 to 1' },
    { group: 'Spacing inside (padding)', key: 'padding-top', label: 'Top', type: 'size', small: true },
    { group: 'Spacing inside (padding)', key: 'padding-right', label: 'Right', type: 'size', small: true },
    { group: 'Spacing inside (padding)', key: 'padding-bottom', label: 'Bottom', type: 'size', small: true },
    { group: 'Spacing inside (padding)', key: 'padding-left', label: 'Left', type: 'size', small: true },
    { group: 'Spacing outside (margin)', key: 'margin-top', label: 'Top', type: 'size', small: true },
    { group: 'Spacing outside (margin)', key: 'margin-right', label: 'Right', type: 'size', small: true },
    { group: 'Spacing outside (margin)', key: 'margin-bottom', label: 'Bottom', type: 'size', small: true },
    { group: 'Spacing outside (margin)', key: 'margin-left', label: 'Left', type: 'size', small: true },
    { group: 'Border and corners', key: 'border-width', label: 'Border width', type: 'size', small: true },
    { group: 'Border and corners', key: 'border-color', label: 'Border colour', type: 'color' },
    { group: 'Border and corners', key: 'border-radius', label: 'Corner radius', type: 'size', small: true },
    { group: 'Border and corners', key: 'no-shadow', label: 'Remove shadow', type: 'check' },
    { group: 'Size', key: 'width', label: 'Width', type: 'size', hint: '200 or 50% or auto' },
    { group: 'Size', key: 'max-width', label: 'Max width', type: 'size' },
    { group: 'Size', key: 'max-height', label: 'Max height', type: 'size' },
    { group: 'Show or hide', key: 'hide', label: 'Hide this element', type: 'check' }
  ];

  let active = false, selected = null, hovered = null, cands = [], selector = '', scope = 'all', vals = {}, writeTimer = 0;
  let liveStyle = null, statusEl = null;

  // ---- helpers ---------------------------------------------------------
  function el(tag, cls, text) { const e = document.createElement(tag); if (cls) e.className = cls; if (text !== undefined) e.textContent = text; return e; }
  function doc() { try { return frame.contentDocument && frame.contentDocument.documentElement ? frame.contentDocument : null; } catch (e) { return null; } }
  function bnClasses(e) { return Array.from(e.classList || []).filter(c => /^bn-[A-Za-z0-9_-]+$/.test(c)); }
  function describe(e) {
    const c = bnClasses(e)[0];
    let t = e.tagName.toLowerCase() + (c ? '.' + c : '');
    const txt = (e.children.length === 0 ? (e.textContent || '') : '').trim().replace(/\s+/g, ' ');
    if (txt) t += ' “' + (txt.length > 28 ? txt.slice(0, 28) + '…' : txt) + '”';
    return t;
  }
  function rgbToHex(v) {
    const m = /rgba?\(\s*(\d+)[ ,]+(\d+)[ ,]+(\d+)(?:[ ,/]+([\d.]+))?/.exec(v || '');
    if (!m) return '#000000';
    if (m[4] !== undefined && parseFloat(m[4]) === 0) return '#ffffff';
    return '#' + [m[1], m[2], m[3]].map(n => (+n).toString(16).padStart(2, '0')).join('');
  }

  // ---- selector candidates --------------------------------------------
  function nthPath(e, d) {
    const parts = []; let cur = e;
    for (let i = 0; i < 5 && cur && cur !== d.documentElement; i++) {
      if (cur === d.body) { parts.unshift('body'); break; }
      const bc = bnClasses(cur);
      if (bc.length && cur !== e) { parts.unshift('.' + bc[0]); break; }
      const tag = cur.tagName.toLowerCase();
      const same = cur.parentElement ? Array.from(cur.parentElement.children).filter(x => x.tagName === cur.tagName) : [cur];
      parts.unshift(same.length > 1 ? tag + ':nth-of-type(' + (same.indexOf(cur) + 1) + ')' : tag);
      cur = cur.parentElement;
    }
    return parts.join(' > ');
  }
  function candidatesFor(e, d) {
    const out = [];
    const add = (sel, label) => { if (sel && !out.some(o => o.sel === sel)) out.push({ sel, label }); };
    const own = bnClasses(e), tag = e.tagName.toLowerCase();
    own.forEach(c => add('.' + c, 'All elements with this class'));
    let anc = e.parentElement;
    while (anc && anc !== d.body && !bnClasses(anc).length) anc = anc.parentElement;
    if (anc && anc !== d.body) {
      bnClasses(anc).forEach(c => {
        add('.' + c + ' ' + tag, 'Every <' + tag + '> inside .' + c);
        own.forEach(oc => add('.' + c + ' .' + oc, '.' + oc + ' only inside .' + c));
      });
    }
    if (e.id && /^[A-Za-z][A-Za-z0-9_-]*$/.test(e.id)) add('#' + e.id, 'This element (by id)');
    add(nthPath(e, d), 'Same position inside its parent');
    return out;
  }
  function countMatches(sel) { const d = doc(); if (!d) return 0; try { return d.querySelectorAll(sel).length; } catch (e) { return 0; } }

  // ---- reading / writing rule blocks in the CSS editor -----------------
  function startMarker() { return '/* visual:' + scope + ' ' + selector + ' */'; }
  function findBlock(value) {
    const s = startMarker(); const from = value.indexOf(s); if (from < 0) return null;
    const e = value.indexOf(END, from); if (e < 0) return null;
    let to = e + END.length; if (value[to] === '\n') to++;
    return { from, to, text: value.slice(from, to) };
  }
  function declsFromVals() {
    const d = [];
    const v = k => vals[k];
    ['color', 'font-size', 'font-weight', 'line-height', 'text-align', 'text-transform', 'background-color', 'opacity'].forEach(k => { if (v(k)) d.push([k, v(k)]); });
    ['padding-top', 'padding-right', 'padding-bottom', 'padding-left', 'margin-top', 'margin-right', 'margin-bottom', 'margin-left'].forEach(k => { if (v(k)) d.push([k, v(k)]); });
    if (v('border-width')) { d.push(['border-style', 'solid']); d.push(['border-width', v('border-width')]); }
    if (v('border-color')) d.push(['border-color', v('border-color')]);
    ['border-radius', 'width', 'max-width', 'max-height'].forEach(k => { if (v(k)) d.push([k, v(k)]); });
    if (v('no-shadow')) d.push(['box-shadow', 'none']);
    if (v('hide')) d.push(['display', 'none']);
    return d;
  }
  function buildBlock(decls) {
    const sc = SCOPES.find(s => s.id === scope);
    const pad = sc.media ? '    ' : '  ';
    const body = decls.map(x => pad + x[0] + ': ' + x[1] + ' !important;').join('\n');
    const rule = sc.media
      ? '@media ' + sc.media + ' {\n  ' + selector + ' {\n' + body + '\n  }\n}'
      : selector + ' {\n' + body + '\n}';
    return startMarker() + '\n' + rule + '\n' + END + '\n';
  }
  function writeNow() {
    const ed = editor(); if (!ed || !selector) return;
    const value = ed.getValue(); const blk = findBlock(value); const decls = declsFromVals();
    const view = ed.view; let change;
    if (!decls.length) {
      if (!blk) { injectLive(); return; }
      let from = blk.from; if (from >= 2 && value.slice(from - 2, from) === '\n\n') from -= 1;
      change = { from, to: blk.to, insert: '' };
    } else if (blk) {
      change = { from: blk.from, to: blk.to, insert: buildBlock(decls) };
    } else {
      const prefix = value.length === 0 ? '' : (value.endsWith('\n\n') ? '' : value.endsWith('\n') ? '\n' : '\n\n');
      change = { from: value.length, insert: prefix + buildBlock(decls) };
    }
    view.dispatch({ changes: change });
    injectLive();
    setStatus(decls.length ? 'Written to the CSS editor. Click “Save publisher settings” to publish.' : 'Rule removed from the CSS editor.');
  }
  function scheduleWrite() { clearTimeout(writeTimer); writeTimer = setTimeout(writeNow, 250); }
  function flushWrite() { if (writeTimer) { clearTimeout(writeTimer); writeTimer = 0; writeNow(); } }

  function loadValsFromEditor() {
    vals = {};
    const blk = findBlock(editor().getValue()); if (!blk) return;
    const re = /([a-z-]+)\s*:\s*([^;{}]+?)\s*(?:!important)?\s*;/g; let m;
    const inner = blk.text.slice(startMarker().length);
    while ((m = re.exec(inner))) {
      const p = m[1], val = m[2].trim();
      if (p === 'display' && val === 'none') vals['hide'] = true;
      else if (p === 'box-shadow' && val === 'none') vals['no-shadow'] = true;
      else if (p !== 'border-style') vals[p] = val;
    }
  }

  // ---- live preview inside the frame ----------------------------------
  function injectLive() {
    const d = doc(); if (!d) return;
    try {
      let s = d.getElementById('bn-ve-live');
      if (!s) { s = d.createElement('style'); s.id = 'bn-ve-live'; (d.body || d.documentElement).appendChild(s); }
      s.textContent = editor().getValue();
      let h = d.getElementById('bn-ve-helper');
      if (!h) {
        h = d.createElement('style'); h.id = 'bn-ve-helper';
        h.textContent = '[data-bn-ve-hover]{outline:2px dashed #e11d48 !important;outline-offset:-2px !important;cursor:crosshair !important}[data-bn-ve-sel]{outline:2px solid #2563eb !important;outline-offset:-2px !important}';
        (d.body || d.documentElement).appendChild(h);
      }
      liveStyle = s;
    } catch (e) { /* ignore */ }
  }

  // ---- picking ---------------------------------------------------------
  function clearMarks() {
    const d = doc(); if (!d) return;
    d.querySelectorAll('[data-bn-ve-hover],[data-bn-ve-sel]').forEach(x => { x.removeAttribute('data-bn-ve-hover'); x.removeAttribute('data-bn-ve-sel'); });
  }
  function attach() {
    const d = doc(); if (!d || d.__bnVe) { injectLive(); return; }
    d.__bnVe = true;
    injectLive();
    d.addEventListener('mouseover', ev => {
      if (!active) return;
      if (hovered && hovered !== ev.target) hovered.removeAttribute('data-bn-ve-hover');
      hovered = ev.target;
      if (hovered !== selected) hovered.setAttribute('data-bn-ve-hover', '');
      const h = panel.querySelector('[data-ve-hover]'); if (h) h.textContent = describe(hovered);
    }, true);
    d.addEventListener('click', ev => {
      if (!active) return;
      ev.preventDefault(); ev.stopPropagation();
      select(ev.target);
    }, true);
    d.addEventListener('submit', ev => { if (active) ev.preventDefault(); }, true);
  }
  function onFrameLoad() { selected = null; hovered = null; if (active) { attach(); renderPanel(); } else { attach(); } }
  frame.addEventListener('load', onFrameLoad);

  function select(e) {
    flushWrite();
    const d = doc(); if (!d || !e || e === d.documentElement) return;
    clearMarks();
    selected = e; e.setAttribute('data-bn-ve-sel', '');
    cands = candidatesFor(e, d);
    selector = cands[0].sel;
    scope = scope || 'all';
    loadValsFromEditor();
    renderPanel();
  }

  // ---- value normalisation --------------------------------------------
  function normSize(key, raw) {
    let v = (raw || '').trim().toLowerCase(); if (!v) return '';
    if (key === 'opacity') { const n = parseFloat(v); return isNaN(n) ? null : String(Math.min(1, Math.max(0, n))); }
    if (key === 'line-height' && /^\d*\.?\d+$/.test(v)) return v;
    if (v === 'auto' && (key.startsWith('margin') || key === 'width' || key === 'max-width')) return 'auto';
    if (/^-?\d*\.?\d+$/.test(v)) v += 'px';
    if (!/^-?\d*\.?\d+(px|rem|em|%|vw|vh)$/.test(v)) return null;
    if ((key.startsWith('padding') || key.startsWith('border') || key === 'width' || key.startsWith('max-')) && v.startsWith('-')) return null;
    return v;
  }
  function setStatus(t) { if (statusEl) statusEl.textContent = t; }

  // ---- panel -----------------------------------------------------------
  function renderPanel() {
    panel.textContent = '';
    const head = el('div', 'd-flex justify-content-between align-items-center mb-2');
    head.appendChild(el('strong', '', 'Visual editor'));
    const close = el('button', 'btn btn-sm btn-outline-secondary', 'Done'); close.type = 'button';
    close.addEventListener('click', () => setActive(false));
    head.appendChild(close); panel.appendChild(head);

    if (!selected) {
      panel.appendChild(el('p', 'small text-muted', 'Click any element in the preview (a heading, the logo, a card, the footer…) to restyle it. Links will not open while picking.'));
      const hv = el('div', 'small'); hv.appendChild(el('span', 'text-muted', 'Pointing at: ')); const s = el('span'); s.setAttribute('data-ve-hover', ''); hv.appendChild(s);
      panel.appendChild(hv);
      statusEl = null; return;
    }
    const d = doc(); const cs = d ? d.defaultView.getComputedStyle(selected) : null;

    const sel = el('div', 'mb-2');
    sel.appendChild(el('div', 'small fw-semibold', 'Selected'));
    sel.appendChild(el('div', 'small', describe(selected)));
    const up = el('button', 'btn btn-sm btn-outline-secondary mt-1', '↑ Pick the parent instead'); up.type = 'button';
    up.addEventListener('click', () => { if (selected.parentElement && selected.parentElement !== d.documentElement) select(selected.parentElement); });
    sel.appendChild(up); panel.appendChild(sel);

    const apply = el('div', 'mb-2');
    apply.appendChild(el('label', 'form-label small fw-semibold mb-1', 'Apply the change to'));
    const sSel = el('select', 'form-select form-select-sm');
    cands.forEach(c => { const o = el('option', '', c.label + ' — ' + c.sel + '  (' + countMatches(c.sel) + ' on this page)'); o.value = c.sel; if (c.sel === selector) o.selected = true; sSel.appendChild(o); });
    sSel.addEventListener('change', () => { flushWrite(); selector = sSel.value; loadValsFromEditor(); renderPanel(); });
    apply.appendChild(sSel);
    const wSel = el('select', 'form-select form-select-sm mt-1');
    SCOPES.forEach(s => { const o = el('option', '', s.label); o.value = s.id; if (s.id === scope) o.selected = true; wSel.appendChild(o); });
    wSel.addEventListener('change', () => { flushWrite(); scope = wSel.value; loadValsFromEditor(); renderPanel(); });
    apply.appendChild(wSel);
    apply.appendChild(el('div', 'form-text', 'Tip: for “phones only”, switch the preview to Phone width above.'));
    panel.appendChild(apply);

    let lastGroup = null, row = null;
    CONTROLS.forEach(c => {
      if (c.group !== lastGroup) { panel.appendChild(el('div', 'small fw-semibold text-uppercase text-muted mt-3 mb-1', c.group)); lastGroup = c.group; row = null; }
      const cur = cs ? cs.getPropertyValue(c.key === 'hide' || c.key === 'no-shadow' ? 'display' : c.key) : '';
      if (c.type === 'color') {
        const r = el('div', 'd-flex align-items-center gap-2 mb-1');
        r.appendChild(el('label', 'small flex-grow-1 mb-0', c.label));
        const inp = el('input', 'form-control form-control-color form-control-sm'); inp.type = 'color';
        inp.value = /^#[0-9a-f]{6}$/i.test(vals[c.key] || '') ? vals[c.key] : rgbToHex(cur);
        inp.style.opacity = vals[c.key] ? '1' : '.55';
        inp.addEventListener('input', () => { vals[c.key] = inp.value; inp.style.opacity = '1'; scheduleWrite(); });
        const clr = el('button', 'btn btn-sm btn-outline-secondary flex-shrink-0', 'Clear'); clr.type = 'button';
        clr.addEventListener('click', () => { delete vals[c.key]; inp.style.opacity = '.55'; scheduleWrite(); });
        r.appendChild(inp); r.appendChild(clr); panel.appendChild(r);
      } else if (c.type === 'select') {
        const r = el('div', 'd-flex align-items-center gap-2 mb-1');
        r.appendChild(el('label', 'small flex-grow-1 mb-0', c.label));
        const s = el('select', 'form-select form-select-sm'); s.style.width = '140px';
        c.options.forEach(o => { const op = el('option', '', o || '(unchanged)'); op.value = o; if ((vals[c.key] || '') === o) op.selected = true; s.appendChild(op); });
        s.addEventListener('change', () => { if (s.value) vals[c.key] = s.value; else delete vals[c.key]; scheduleWrite(); });
        r.appendChild(s); panel.appendChild(r);
      } else if (c.type === 'check') {
        const r = el('div', 'form-check mb-1');
        const cb = el('input', 'form-check-input'); cb.type = 'checkbox'; cb.id = 've_' + c.key; cb.checked = !!vals[c.key];
        cb.addEventListener('change', () => { if (cb.checked) vals[c.key] = true; else delete vals[c.key]; scheduleWrite(); });
        const lb = el('label', 'form-check-label small', c.label); lb.htmlFor = cb.id;
        r.appendChild(cb); r.appendChild(lb); panel.appendChild(r);
      } else {
        const inRow = c.small && c.group.startsWith('Spacing') ;
        const r = el('div', 'd-flex align-items-center gap-2 mb-1');
        r.appendChild(el('label', 'small flex-grow-1 mb-0', c.label));
        const inp = el('input', 'form-control form-control-sm'); inp.type = 'text'; inp.style.width = '140px';
        inp.value = vals[c.key] || '';
        const ph = (cur || '').replace(/px$/, ''); inp.placeholder = c.hint || (ph && ph !== 'none' && ph !== 'normal' ? ph : '');
        inp.addEventListener('input', () => {
          const n = normSize(c.key, inp.value);
          if (n === null) { inp.classList.add('is-invalid'); return; }
          inp.classList.remove('is-invalid');
          if (n) vals[c.key] = n; else delete vals[c.key];
          scheduleWrite();
        });
        r.appendChild(inp); panel.appendChild(r);
      }
    });

    const foot = el('div', 'mt-3 d-flex gap-2 align-items-center');
    const reset = el('button', 'btn btn-sm btn-outline-danger', 'Reset this element'); reset.type = 'button';
    reset.addEventListener('click', () => { vals = {}; writeNow(); renderPanel(); });
    foot.appendChild(reset); panel.appendChild(foot);
    statusEl = el('div', 'small text-muted mt-2', 'Numbers are in pixels unless you add a unit (rem, %, …). Changes appear in the preview straight away and are written into the CSS editor above.');
    panel.appendChild(statusEl);
  }

  function setActive(on) {
    active = on;
    toggle.classList.toggle('btn-primary', on);
    toggle.classList.toggle('btn-outline-primary', !on);
    toggle.textContent = on ? 'Picking… (click to stop)' : 'Pick element';
    panel.style.display = on ? 'block' : 'none';
    if (on) {
      if (!doc()) { panel.style.display = 'block'; panel.textContent = ''; panel.appendChild(el('p', 'small text-danger', 'The preview is not loaded yet, or is showing a page outside this site. Click “Preview unsaved changes” first.')); return; }
      attach(); renderPanel();
    } else {
      flushWrite(); clearMarks(); selected = null; hovered = null;
    }
  }
  toggle.addEventListener('click', () => setActive(!active));
})();
