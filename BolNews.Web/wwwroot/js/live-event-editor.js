/*
 * Quill editors for Live Events (intro box + each update's details).
 * Same toolbar family and the same social-embed format as the Article Editor, so embeds written here are
 * restored on the public page exactly like article embeds (div.quill-social-embed + base64 data-raw-html).
 *
 *   LiveQuill.attach(hostDiv, hiddenTextarea, { placeholder, height })
 *   LiveQuill.syncAll()       // copies every editor's HTML into its hidden textarea (call before submit)
 */
(function () {
    'use strict';
    if (!window.Quill) return;

    const BlockEmbed = Quill.import('blots/block/embed');
    const Delta = Quill.import('delta');

    const encode = v => btoa(unescape(encodeURIComponent(v ?? '')));
    const decode = v => { try { return decodeURIComponent(escape(atob(v))); } catch { return ''; } };

    class SocialEmbedBlot extends BlockEmbed {
        static create(value) {
            const node = super.create();
            node.setAttribute('contenteditable', 'false');
            node.setAttribute('data-raw-html', encode(value));

            let platform = 'Social Media';
            const m = (value || '').match(/(?:www\.)?([a-zA-Z0-9-]+)\.(?:com|net|org|be)/i);
            if (m && m[1]) {
                platform = m[1].charAt(0).toUpperCase() + m[1].slice(1);
                if (platform.toLowerCase() === 'youtu') platform = 'YouTube';
                if (platform.toLowerCase() === 'fb') platform = 'Facebook';
            }
            const box = document.createElement('div');
            box.className = 'embed-placeholder-box';
            box.style.cssText = 'border:1px dashed #9aa4b2;border-radius:6px;padding:.6rem;text-align:center;background:#f6f8fb;color:#333;cursor:pointer';
            box.textContent = '🌐 ' + platform + ' embed (renders live on the website, click to remove)';
            node.appendChild(box);
            return node;
        }
        static value(node) {
            const data = node.getAttribute('data-raw-html');
            return data ? decode(data) : node.innerHTML;
        }
    }
    SocialEmbedBlot.blotName = 'socialEmbed';
    SocialEmbedBlot.tagName = 'div';
    SocialEmbedBlot.className = 'quill-social-embed';
    Quill.register(SocialEmbedBlot, true);

    Quill.import('ui/icons')['socialEmbed'] =
        '<span style="font-size:11px;font-weight:700;line-height:18px">Embed</span>';

    const editors = [];

    // Quill 2 writes every list as <ol><li data-list="bullet|ordered">; turn that into real <ul>/<ol>.
    function fixLists(root) {
        root.querySelectorAll('ol').forEach(ol => {
            const items = Array.from(ol.children).filter(c => c.tagName === 'LI');
            if (!items.some(li => li.hasAttribute('data-list'))) return;
            const frag = document.createDocumentFragment();
            let cur = null, curType = null;
            items.forEach(li => {
                const type = li.getAttribute('data-list') === 'bullet' ? 'ul' : 'ol';
                if (type !== curType) { cur = document.createElement(type); frag.appendChild(cur); curType = type; }
                li.removeAttribute('data-list');
                li.querySelectorAll('.ql-ui').forEach(n => n.remove());
                cur.appendChild(li);
            });
            ol.replaceWith(frag);
        });
    }

    function toHtml(q) {
        const hasMedia = q.root.querySelector('img, .quill-social-embed');
        if (!hasMedia && q.getText().trim() === '') return '';
        const div = document.createElement('div');
        div.innerHTML = q.root.innerHTML;
        div.querySelectorAll('.ql-cursor').forEach(n => n.remove());
        fixLists(div);
        // Drop empty blocks left at the very end (e.g. an empty list item after an embed).
        const isEmpty = el => !el.querySelector('img, .quill-social-embed') && el.textContent.trim() === '';
        while (div.lastElementChild && /^(P|UL|OL|H[1-6])$/.test(div.lastElementChild.tagName) && isEmpty(div.lastElementChild)) div.lastElementChild.remove();
        return div.innerHTML;
    }

    function attach(host, textarea, opts) {
        opts = opts || {};
        host.innerHTML = '';
        const q = new Quill(host, {
            theme: 'snow',
            placeholder: opts.placeholder || '',
            modules: {
                toolbar: {
                    container: [
                        [{ header: [2, 3, 4, false] }],
                        ['bold', 'italic', 'underline', 'strike'],
                        [{ color: [] }, { background: [] }],
                        [{ align: [] }],
                        [{ list: 'ordered' }, { list: 'bullet' }],
                        ['blockquote'],
                        ['link'],
                        ['socialEmbed'],
                        ['clean']
                    ],
                    handlers: {
                        socialEmbed: function () {
                            const code = prompt('Paste the full social media embed code (X/Twitter, Facebook, Instagram, YouTube, TikTok...):');
                            if (!code || !code.trim()) return;
                            const range = this.quill.getSelection(true);
                            this.quill.insertEmbed(range.index, 'socialEmbed', code.trim(), 'user');
                            this.quill.setSelection(range.index + 1, 0, 'silent');
                        }
                    }
                }
            }
        });

        q.clipboard.addMatcher('div.quill-social-embed', node => {
            const data = node.getAttribute('data-raw-html');
            return new Delta().insert({ socialEmbed: data ? decode(data) : node.innerHTML });
        });

        if (textarea.value && textarea.value.trim()) q.clipboard.dangerouslyPasteHTML(textarea.value);

        host.addEventListener('click', e => {
            const embed = e.target.closest('.quill-social-embed');
            if (!embed) return;
            if (confirm('Remove this social media embed?')) { const blot = Quill.find(embed); if (blot) blot.remove(); }
        });

        editors.push({ q, textarea, host });
        return q;
    }

    function syncAll() {
        for (let i = editors.length - 1; i >= 0; i--) {
            if (!document.body.contains(editors[i].host)) { editors.splice(i, 1); continue; }
            editors[i].textarea.value = toHtml(editors[i].q);
        }
    }

    window.LiveQuill = { attach, syncAll };
})();
