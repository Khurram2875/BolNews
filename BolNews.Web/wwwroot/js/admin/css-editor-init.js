/* Turns the "Additional CSS" textarea in Publisher Settings into a code editor with a snippets / classes / variables panel.
   If the editor bundle fails to load, the plain textarea stays usable. */
(function () {
  const textarea = document.getElementById('CustomCss');
  const host = document.getElementById('bnCssEditorHost');
  if (!textarea || !host || !window.BnCssEditor) return;

  const classes = (window.BN_CSS_CLASSES || []).map(c => '.' + c);
  const variables = window.BN_CSS_VARIABLES || [];
  const descriptions = window.BN_CSS_DESCRIPTIONS || {};
  const snippets = window.BN_CSS_SNIPPETS || [];

  const editor = window.BnCssEditor.create({
    parent: host,
    value: textarea.value,
    classes: classes,
    variables: variables,
    placeholder: '.bn-brand-logo img { max-height: 70px; }',
    onChange: value => { textarea.value = value; updateCount(value); }
  });
  textarea.style.display = 'none';
  window.bnCssEditorInstance = editor;

  const countEl = document.getElementById('bnCssCount');
  function updateCount(value) {
    if (countEl) countEl.textContent = value.length.toLocaleString() + ' / 100,000 characters';
  }
  updateCount(textarea.value);

  // ---- reference panel -------------------------------------------------
  const list = document.getElementById('bnCssList');
  const search = document.getElementById('bnCssSearch');
  const tabs = document.querySelectorAll('[data-css-tab]');
  let tab = 'snippets';

  function el(tag, cls, text) {
    const e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text !== undefined) e.textContent = text;
    return e;
  }

  function render() {
    const q = (search.value || '').trim().toLowerCase();
    list.textContent = '';
    if (tab === 'snippets') {
      snippets.filter(s => !q || (s.title + ' ' + s.note + ' ' + s.css).toLowerCase().includes(q)).forEach(s => {
        const row = el('div', 'bn-css-item');
        const head = el('div', 'd-flex justify-content-between align-items-start gap-2');
        const title = el('div');
        title.appendChild(el('div', 'fw-semibold small', s.title));
        title.appendChild(el('div', 'text-muted small', s.note));
        const btn = el('button', 'btn btn-sm btn-outline-secondary flex-shrink-0', 'Add');
        btn.type = 'button';
        btn.addEventListener('click', () => editor.append(s.css));
        head.appendChild(title); head.appendChild(btn);
        row.appendChild(head);
        row.appendChild(el('pre', 'bn-css-code', s.css));
        list.appendChild(row);
      });
    } else if (tab === 'classes') {
      const all = (window.BN_CSS_CLASSES || []).filter(c => !q || (c + ' ' + (descriptions[c] || '')).toLowerCase().includes(q));
      // Described classes first, then the rest alphabetically.
      all.sort((a, b) => (descriptions[b] ? 1 : 0) - (descriptions[a] ? 1 : 0) || a.localeCompare(b));
      all.slice(0, 200).forEach(c => {
        const row = el('div', 'bn-css-item d-flex justify-content-between align-items-center gap-2');
        const t = el('div');
        t.appendChild(el('code', '', '.' + c));
        if (descriptions[c]) t.appendChild(el('div', 'text-muted small', descriptions[c]));
        const btn = el('button', 'btn btn-sm btn-outline-secondary flex-shrink-0', 'Insert');
        btn.type = 'button';
        btn.addEventListener('click', () => editor.append('.' + c + ' {\n  \n}'));
        row.appendChild(t); row.appendChild(btn);
        list.appendChild(row);
      });
      if (!all.length) list.appendChild(el('div', 'text-muted small p-2', 'No matching classes.'));
    } else {
      variables.filter(v => !q || (v.name + ' ' + (v.hint || '')).toLowerCase().includes(q)).forEach(v => {
        const row = el('div', 'bn-css-item d-flex justify-content-between align-items-center gap-2');
        const t = el('div');
        t.appendChild(el('code', '', v.name));
        if (v.hint) t.appendChild(el('div', 'text-muted small', v.hint));
        const btn = el('button', 'btn btn-sm btn-outline-secondary flex-shrink-0', 'Insert');
        btn.type = 'button';
        btn.addEventListener('click', () => editor.insert('var(' + v.name + ')'));
        row.appendChild(t); row.appendChild(btn);
        list.appendChild(row);
      });
    }
  }

  tabs.forEach(b => b.addEventListener('click', () => {
    tab = b.dataset.cssTab;
    tabs.forEach(x => x.classList.toggle('active', x === b));
    render();
  }));
  search.addEventListener('input', render);
  render();

  // ---- toolbar ---------------------------------------------------------
  const previewBtn = document.getElementById('bnCssPreview');
  if (previewBtn) previewBtn.addEventListener('click', () => {
    const real = document.querySelector('button[formtarget="bnPreviewFrame"]');
    if (real) real.click();
  });
})();
