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

    let chunks = [];
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
        if (status) status.textContent = isActive ? (text('msgReading') || 'Reading article aloud.') : (text('msgStopped') || 'Article audio stopped.');
    }

    const joiner = isUrdu ? '\u06D4 ' : '. ';

    function getArticleText() {
        const title = document.querySelector('h1[itemprop="headline"]')?.innerText?.trim() ?? '';
        const summary = document.querySelector('article > p')?.innerText?.trim() ?? '';
        const body = document.querySelector('.bn-article-body');
        if (!body) return [title, summary].filter(Boolean).join(joiner);

        const cleanBody = body.cloneNode(true);
        cleanBody.querySelectorAll('script, style, iframe, video, audio, button, form, .bn-ad-slot, .bn-ad-code-block, [aria-label="Advertisement"]')
            .forEach(element => element.remove());
        const content = (cleanBody.innerText || cleanBody.textContent || '').trim();
        return [title, summary, content].filter(Boolean).join(joiner);
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

        const utterance = new SpeechSynthesisUtterance(chunks[nextChunk++]);
        utterance.lang = voice ? voice.lang : speechLang;
        if (voice) utterance.voice = voice;
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
        chunks = splitIntoChunks(getArticleText());
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
