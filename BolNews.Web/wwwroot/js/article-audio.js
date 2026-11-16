(() => {
    const button = document.querySelector('[data-article-audio-button]');
    if (!button) return;

    const status = document.querySelector('[data-article-audio-status]');
    const note = document.querySelector('[data-article-audio-note]');
    const text = key => button.dataset[key];
    const pageLang = (document.documentElement.lang || navigator.language || 'en').toLowerCase();
    const isUrdu = pageLang.startsWith('ur');
    // Urdu voices are tagged ur-PK (sometimes ur-IN); asking for plain "ur" makes some browsers fall back to English.
    const speechLang = isUrdu ? 'ur-PK' : (document.documentElement.lang || navigator.language || 'en');
    const sentenceEnd = '.!?\u06D4\u061F\u2026'; // also the Urdu full stop (۔) and question mark (؟)
    const sentenceRegex = new RegExp('[^' + sentenceEnd + ']+[' + sentenceEnd + ']+|[^' + sentenceEnd + ']+$', 'g');
    let voice = null;

    function showNote(message) {
        if (!note) return;
        note.textContent = message || '';
        note.hidden = !message;
    }
    const playIcon = button.querySelector('[data-audio-icon-play]');
    const stopIcon = button.querySelector('[data-audio-icon-stop]');

    if (!('speechSynthesis' in window) || !('SpeechSynthesisUtterance' in window)) {
        button.hidden = true;
        if (status) status.textContent = text('msgUnsupported') || 'Text to speech is not available in this browser.';
        return;
    }

    let chunks = []; // { text, segment }
    let nextChunk = 0;
    let active = false;

    function setActive(isActive) {
        active = isActive;
        button.setAttribute('aria-pressed', String(isActive));
        const label = isActive ? (text('labelStop') || 'Stop reading article') : (text('labelListen') || 'Listen to article');
        button.setAttribute('aria-label', label);
        button.title = label;
        playIcon.hidden = isActive;
        stopIcon.hidden = !isActive;
        if (!isActive) clearHighlight();
        if (status) status.textContent = isActive ? (text('msgReading') || 'Reading article aloud.') : (text('msgStopped') || 'Article audio stopped.');
    }

    const EXCLUDE = 'script, style, iframe, video, audio, button, form, noscript, .bn-ad-slot, .bn-ad-code-block, [aria-label="Advertisement"]';
    const BLOCK = 'p, h1, h2, h3, h4, h5, h6, li, blockquote, figcaption, div, td, th';
    const hasHighlights = typeof CSS !== 'undefined' && CSS.highlights && typeof Highlight !== 'undefined';
    const HIGHLIGHT_NAME = 'bn-tts';
    let segments = [];

    function locate(map, offset, preferEnd) {
        for (const entry of map) {
            if (offset < entry.end || (preferEnd && offset === entry.end)) {
                return { node: entry.node, offset: offset - entry.start };
            }
        }
        const last = map[map.length - 1];
        return { node: last.node, offset: last.end - last.start };
    }

    // One segment per spoken sentence, each with a DOM range so the exact text can be highlighted.
    function collectSegments() {
        const roots = [
            document.querySelector('h1[itemprop="headline"]'),
            document.querySelector('article > p'),
            document.querySelector('.bn-article-body')
        ].filter(Boolean);

        const result = [];
        for (const root of roots) {
            const groups = new Map();
            const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
            for (let node = walker.nextNode(); node; node = walker.nextNode()) {
                const parent = node.parentElement;
                if (!parent || parent.closest(EXCLUDE) || !node.nodeValue) continue;
                const block = parent.closest(BLOCK) || root;
                if (!groups.has(block)) groups.set(block, []);
                groups.get(block).push(node);
            }

            for (const nodes of groups.values()) {
                let full = '';
                const map = [];
                for (const node of nodes) {
                    map.push({ node, start: full.length, end: full.length + node.nodeValue.length });
                    full += node.nodeValue;
                }
                if (!full.trim()) continue;

                const regex = new RegExp(sentenceRegex.source, 'g');
                let match;
                while ((match = regex.exec(full)) !== null) {
                    const raw = match[0];
                    const lead = raw.length - raw.trimStart().length;
                    const spoken = raw.replace(/\s+/g, ' ').trim();
                    if (!/[\p{L}\p{N}]/u.test(spoken)) continue;
                    const start = locate(map, match.index + lead, false);
                    const end = locate(map, match.index + raw.trimEnd().length, true);
                    const range = document.createRange();
                    try {
                        range.setStart(start.node, start.offset);
                        range.setEnd(end.node, end.offset);
                    } catch { continue; }
                    result.push({ text: spoken, range });
                }
            }
        }
        return result;
    }

    function clearHighlight() {
        if (hasHighlights) CSS.highlights.delete(HIGHLIGHT_NAME);
    }

    function highlightSegment(index) {
        const segment = segments[index];
        if (!segment || !hasHighlights) return;
        CSS.highlights.set(HIGHLIGHT_NAME, new Highlight(segment.range));
        const rect = segment.range.getBoundingClientRect();
        if (button.dataset.autoScroll !== 'false' && (rect.top < 90 || rect.bottom > window.innerHeight - 90)) {
            const reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            const target = segment.range.startContainer.parentElement;
            if (target) target.scrollIntoView({ block: 'center', behavior: reduce ? 'auto' : 'smooth' });
        }
    }

    function splitIntoChunks(text, maxLength = 220) {
        const sentences = text.match(sentenceRegex) || [text];
        const result = [];
        let current = '';

        for (const sentence of sentences) {
            const part = sentence.trim();
            if (!part) continue;
            if (part.length > maxLength) {
                if (current) result.push(current);
                current = '';
                const words = part.split(/\s+/);
                let wordChunk = '';
                for (const word of words) {
                    if (wordChunk && (wordChunk + ' ' + word).length > maxLength) {
                        result.push(wordChunk);
                        wordChunk = word;
                    } else {
                        wordChunk = wordChunk ? wordChunk + ' ' + word : word;
                    }
                }
                current = wordChunk;
            } else if (current && (current + ' ' + part).length > maxLength) {
                result.push(current);
                current = part;
            } else {
                current = current ? current + ' ' + part : part;
            }
        }

        if (current) result.push(current);
        return result;
    }

    function pickVoice() {
        const voices = window.speechSynthesis.getVoices() || [];
        if (!isUrdu) return null; // English and other languages keep the browser's default voice for the page language
        const wanted = speechLang.toLowerCase();
        return voices.find(v => v.lang.toLowerCase().replace('_', '-') === wanted)
            || voices.find(v => v.lang.toLowerCase().startsWith('ur'))
            || null;
    }

    // Voices load asynchronously in Chrome/Edge, so wait briefly for them before deciding none exist.
    function whenVoicesReady() {
        return new Promise(resolve => {
            if ((window.speechSynthesis.getVoices() || []).length) return resolve();
            const done = () => { window.speechSynthesis.removeEventListener('voiceschanged', done); resolve(); };
            window.speechSynthesis.addEventListener('voiceschanged', done);
            setTimeout(done, 1500);
        });
    }

    function speakNextChunk() {
        if (!active) return;
        if (nextChunk >= chunks.length) {
            setActive(false);
            return;
        }

        const chunk = chunks[nextChunk++];
        const utterance = new SpeechSynthesisUtterance(chunk.text);
        utterance.lang = voice ? voice.lang : speechLang;
        if (voice) utterance.voice = voice;
        utterance.onstart = () => highlightSegment(chunk.segment);
        utterance.onend = speakNextChunk;
        utterance.onerror = () => setActive(false);
        window.speechSynthesis.speak(utterance);
    }

    button.addEventListener('click', async () => {
        if (active) {
            window.speechSynthesis.cancel();
            setActive(false);
            return;
        }

        showNote('');
        segments = collectSegments();
        chunks = segments.flatMap((segment, index) => splitIntoChunks(segment.text).map(text => ({ text, segment: index })));
        if (!chunks.length) {
            const empty = text('msgEmpty') || 'No article text is available to read.';
            if (status) status.textContent = empty;
            showNote(empty);
            return;
        }

        if (isUrdu) {
            await whenVoicesReady();
            voice = pickVoice();
            if (!voice) {
                const message = text('msgNovoice') || 'No Urdu voice is installed on this device.';
                if (status) status.textContent = message;
                showNote(message);
                return;
            }
        }

        window.speechSynthesis.cancel();
        nextChunk = 0;
        setActive(true);
        speakNextChunk();
    });

    window.addEventListener('pagehide', () => window.speechSynthesis.cancel(), { once: true });
})();
