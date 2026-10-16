(() => {
    const button = document.querySelector('[data-article-audio-button]');
    if (!button) return;

    const status = document.querySelector('[data-article-audio-status]');
    const playIcon = button.querySelector('[data-audio-icon-play]');
    const stopIcon = button.querySelector('[data-audio-icon-stop]');

    if (!('speechSynthesis' in window) || !('SpeechSynthesisUtterance' in window)) {
        button.hidden = true;
        if (status) status.textContent = 'Text to speech is not available in this browser.';
        return;
    }

    let chunks = [];
    let nextChunk = 0;
    let active = false;

    function setActive(isActive) {
        active = isActive;
        button.setAttribute('aria-pressed', String(isActive));
        button.setAttribute('aria-label', isActive ? 'Stop reading article' : 'Listen to article');
        button.title = isActive ? 'Stop reading article' : 'Listen to article';
        playIcon.hidden = isActive;
        stopIcon.hidden = !isActive;
        if (status) status.textContent = isActive ? 'Reading article aloud.' : 'Article audio stopped.';
    }

    function getArticleText() {
        const title = document.querySelector('h1[itemprop="headline"]')?.innerText?.trim() ?? '';
        const summary = document.querySelector('article > p')?.innerText?.trim() ?? '';
        const body = document.querySelector('.bn-article-body');
        if (!body) return [title, summary].filter(Boolean).join('. ');

        const cleanBody = body.cloneNode(true);
        cleanBody.querySelectorAll('script, style, iframe, video, audio, button, form, .bn-ad-slot, .bn-ad-code-block, [aria-label="Advertisement"]')
            .forEach(element => element.remove());
        const content = (cleanBody.innerText || cleanBody.textContent || '').trim();
        return [title, summary, content].filter(Boolean).join('. ');
    }

    function splitIntoChunks(text, maxLength = 220) {
        const sentences = text.match(/[^.!?]+[.!?]+|[^.!?]+$/g) || [text];
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

    function speakNextChunk() {
        if (!active) return;
        if (nextChunk >= chunks.length) {
            setActive(false);
            return;
        }

        const utterance = new SpeechSynthesisUtterance(chunks[nextChunk++]);
        utterance.lang = document.documentElement.lang || navigator.language || 'en';
        utterance.onend = speakNextChunk;
        utterance.onerror = () => setActive(false);
        window.speechSynthesis.speak(utterance);
    }

    button.addEventListener('click', () => {
        if (active) {
            window.speechSynthesis.cancel();
            setActive(false);
            return;
        }

        chunks = splitIntoChunks(getArticleText());
        if (!chunks.length) {
            if (status) status.textContent = 'No article text is available to read.';
            return;
        }

        window.speechSynthesis.cancel();
        nextChunk = 0;
        setActive(true);
        speakNextChunk();
    });

    window.addEventListener('pagehide', () => window.speechSynthesis.cancel(), { once: true });
})();
