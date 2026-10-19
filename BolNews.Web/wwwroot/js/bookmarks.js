(() => {
    'use strict';

    const storageKey = 'bolnews.bookmarks.v1';
    const placeholderImage = '/images/placeholder.png';
    const pageSize = 15;
    const pageByList = { saved: 1, recommended: 1 };
    let recommendations = [];

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
        link.href = article.url || (article.categorySlug && article.slug ? `/${article.categorySlug}/${article.slug}` : '#');
        link.className = 'text-decoration-none text-reset';

        const image = document.createElement('img');
        const articleImage = article.image || article.featuredImageThumb || article.featuredImageMedium || article.featuredImageLarge;
        image.src = isLocalPath(articleImage) ? articleImage : placeholderImage;
        image.alt = '';
        image.loading = 'lazy';
        image.className = 'card-img-top';
        image.style.cssText = 'height:190px;object-fit:cover';

        const body = document.createElement('div');
        body.className = 'card-body';
        const categoryName = article.category || article.categoryName;
        if (categoryName) {
            const category = document.createElement('div');
            category.className = 'small text-danger fw-semibold mb-2';
            category.textContent = categoryName;
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

    function renderCards(list, items, type) {
        if (!list) return;
        const pageCount = Math.max(1, Math.ceil(items.length / pageSize));
        pageByList[type] = Math.min(pageByList[type], pageCount);
        const start = (pageByList[type] - 1) * pageSize;
        list.replaceChildren(...items.slice(start, start + pageSize).map(makeSavedCard));

        const pagination = document.querySelector(`[data-saved-pagination="${type}"]`);
        if (!pagination) return;
        pagination.hidden = pageCount <= 1;
        pagination.replaceChildren();
        if (pageCount <= 1) return;

        const addButton = (label, page, disabled, current = false) => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = `saved-page-button${current ? ' active' : ''}`;
            button.textContent = label;
            button.disabled = disabled;
            if (current) button.setAttribute('aria-current', 'page');
            button.addEventListener('click', () => {
                pageByList[type] = page;
                if (type === 'saved') renderSavedPage(readBookmarks());
                else renderCards(document.querySelector('[data-recommended-list]'), recommendations, 'recommended');
            });
            pagination.appendChild(button);
        };

        addButton('Previous', pageByList[type] - 1, pageByList[type] === 1);
        for (let page = 1; page <= pageCount; page++) {
            addButton(String(page), page, false, page === pageByList[type]);
        }
        addButton('Next', pageByList[type] + 1, pageByList[type] === pageCount);
    }

    function renderSavedPage(items) {
        const list = document.querySelector('[data-bookmarks-list]');
        if (!list) return;
        const empty = document.querySelector('[data-bookmarks-empty]');
        const count = document.querySelector('[data-bookmarks-count]');
        const clear = document.querySelector('[data-clear-bookmarks]');

        renderCards(list, items, 'saved');
        if (empty) empty.hidden = items.length > 0;
        if (count) count.textContent = items.length === 1 ? '1 saved article' : `${items.length} saved articles`;
        if (clear) clear.hidden = items.length === 0;
    }

    async function loadRecommendations(items) {
        const list = document.querySelector('[data-recommended-list]');
        const empty = document.querySelector('[data-recommended-empty]');
        const loading = document.querySelector('[data-recommended-loading]');
        if (!list) return;
        list.replaceChildren();
        if (!items.length) {
            if (empty) empty.hidden = false;
            return;
        }

        if (empty) empty.hidden = true;
        if (loading) loading.hidden = false;
        try {
            const categories = [...new Set(items.map(item => item.category).filter(Boolean))];
            const query = new URLSearchParams();
            categories.forEach(category => query.append('categories', category));
            items.forEach(item => query.append('savedTitles', item.title));
            items.forEach(item => query.append('excludeIds', String(item.id)));
            const response = await fetch(`/saved-articles/recommended?${query}`, {
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) throw new Error('Could not load recommendations');
            recommendations = await response.json();
            renderCards(list, recommendations, 'recommended');
            if (empty) empty.hidden = recommendations.length > 0;
        } catch {
            if (empty) {
                empty.textContent = 'Recommendations are temporarily unavailable. Please try again later.';
                empty.hidden = false;
            }
        } finally {
            if (loading) loading.hidden = true;
        }
    }

    function activateTab(name) {
        document.querySelectorAll('[data-saved-tab]').forEach(tab => {
            const active = tab.dataset.savedTab === name;
            tab.classList.toggle('active', active);
            tab.setAttribute('aria-selected', String(active));
            tab.tabIndex = active ? 0 : -1;
        });
        document.querySelectorAll('[data-saved-panel]').forEach(panel => {
            panel.hidden = panel.dataset.savedPanel !== name;
        });
        if (name === 'recommended') loadRecommendations(readBookmarks());
    }

    let bookmarks = readBookmarks();
    updateNavCount(bookmarks);
    updateBookmarkButtons(bookmarks);
    renderSavedPage(bookmarks);

    document.querySelectorAll('[data-saved-tab]').forEach(tab => {
        tab.addEventListener('click', () => activateTab(tab.dataset.savedTab));
        tab.addEventListener('keydown', event => {
            if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
            event.preventDefault();
            const nextName = tab.dataset.savedTab === 'saved' ? 'recommended' : 'saved';
            document.querySelector(`[data-saved-tab="${nextName}"]`)?.focus();
            activateTab(nextName);
        });
    });

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
