(() => {
    'use strict';

    const storageKey = 'bolnews.bookmarks.v1';
    const placeholderImage = '/images/placeholder.png';

    function readBookmarks() {
        try {
            const value = JSON.parse(window.localStorage.getItem(storageKey) || '[]');
            return Array.isArray(value) ? value.filter(item => item && typeof item.id === 'string' && typeof item.title === 'string' && isLocalPath(item.url)) : [];
        } catch {
            return [];
        }
    }

    function writeBookmarks(items) {
        try {
            window.localStorage.setItem(storageKey, JSON.stringify(items));
            return true;
        } catch {
            return false;
        }
    }

    function isLocalPath(value) {
        return typeof value === 'string' && value.startsWith('/') && !value.startsWith('//');
    }

    function updateNavCount(items) {
        document.querySelectorAll('[data-bookmarks-nav-count]').forEach(badge => {
            badge.textContent = String(items.length);
            badge.hidden = items.length === 0;
        });
    }

    function updateBookmarkButtons(items) {
        const saved = new Set(items.map(item => item.id));
        document.querySelectorAll('[data-bookmark-button]').forEach(button => {
            const isSaved = saved.has(String(button.dataset.articleId));
            button.setAttribute('aria-pressed', String(isSaved));
            button.setAttribute('aria-label', isSaved ? 'Remove saved article' : 'Save article');
            button.title = isSaved ? 'Remove saved article' : 'Save article';
            button.style.color = isSaved ? '#c4161c' : '';
        });
    }

    function makeSavedCard(article) {
        const column = document.createElement('div');
        column.className = 'col-12 col-sm-6 col-lg-4';

        const card = document.createElement('article');
        card.className = 'card h-100 shadow-sm';

        const link = document.createElement('a');
        link.href = article.url;
        link.className = 'text-decoration-none text-reset';

        const image = document.createElement('img');
        image.src = isLocalPath(article.image) ? article.image : placeholderImage;
        image.alt = '';
        image.loading = 'lazy';
        image.className = 'card-img-top';
        image.style.cssText = 'height:190px;object-fit:cover';

        const body = document.createElement('div');
        body.className = 'card-body';
        if (article.category) {
            const category = document.createElement('div');
            category.className = 'small text-danger fw-semibold mb-2';
            category.textContent = article.category;
            body.appendChild(category);
        }
        const title = document.createElement('h2');
        title.className = 'h6 mb-0';
        title.textContent = article.title;
        body.appendChild(title);

        link.append(image, body);

        const footer = document.createElement('div');
        footer.className = 'card-footer bg-white d-flex justify-content-between align-items-center';
        const savedAt = document.createElement('small');
        savedAt.className = 'text-muted';
        savedAt.textContent = article.savedAt ? `Saved ${new Date(article.savedAt).toLocaleDateString()}` : 'Saved';
        const remove = document.createElement('button');
        remove.type = 'button';
        remove.className = 'btn btn-sm btn-outline-secondary';
        remove.textContent = 'Remove';
        remove.setAttribute('aria-label', `Remove ${article.title} from saved articles`);
        remove.addEventListener('click', () => {
            const updated = readBookmarks().filter(item => item.id !== article.id);
            if (writeBookmarks(updated)) renderSavedPage(updated);
        });
        footer.append(savedAt, remove);

        card.append(link, footer);
        column.appendChild(card);
        return column;
    }

    function renderSavedPage(items) {
        const list = document.querySelector('[data-bookmarks-list]');
        if (!list) return;
        const empty = document.querySelector('[data-bookmarks-empty]');
        const count = document.querySelector('[data-bookmarks-count]');
        const clear = document.querySelector('[data-clear-bookmarks]');

        list.replaceChildren(...items.map(makeSavedCard));
        if (empty) empty.hidden = items.length > 0;
        if (count) count.textContent = items.length === 1 ? '1 saved article' : `${items.length} saved articles`;
        if (clear) clear.hidden = items.length === 0;
    }

    let bookmarks = readBookmarks();
    updateNavCount(bookmarks);
    updateBookmarkButtons(bookmarks);
    renderSavedPage(bookmarks);

    document.querySelectorAll('[data-bookmark-button]').forEach(button => {
        button.addEventListener('click', () => {
            const id = String(button.dataset.articleId || '');
            if (!id) return;
            bookmarks = readBookmarks();
            const existing = bookmarks.some(item => item.id === id);
            if (existing) {
                bookmarks = bookmarks.filter(item => item.id !== id);
            } else {
                const url = button.dataset.articleUrl || '';
                if (!isLocalPath(url)) return;
                bookmarks.unshift({
                    id,
                    title: button.dataset.articleTitle || 'Untitled article',
                    url,
                    category: button.dataset.articleCategory || '',
                    image: button.dataset.articleImage || '',
                    savedAt: new Date().toISOString()
                });
            }

            if (!writeBookmarks(bookmarks)) {
                button.setAttribute('aria-label', 'Browser storage is unavailable');
                return;
            }
            updateNavCount(bookmarks);
            updateBookmarkButtons(bookmarks);
            renderSavedPage(bookmarks);
        });
    });

    document.querySelector('[data-clear-bookmarks]')?.addEventListener('click', () => {
        if (writeBookmarks([])) {
            bookmarks = [];
            updateNavCount(bookmarks);
            updateBookmarkButtons(bookmarks);
            renderSavedPage(bookmarks);
        }
    });

    window.addEventListener('storage', event => {
        if (event.key !== storageKey) return;
        bookmarks = readBookmarks();
        updateNavCount(bookmarks);
        updateBookmarkButtons(bookmarks);
        renderSavedPage(bookmarks);
    });
})();
