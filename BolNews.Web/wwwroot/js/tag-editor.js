(function () {
    function initializeTagEditors() {
        document.querySelectorAll('.bn-tag-editor').forEach(function (editor) {
            const hidden = editor.querySelector('input[type="hidden"]');
            const input = editor.querySelector('[data-role="input"]');
            const chips = editor.querySelector('[data-role="chips"]');
            let tags = [];

            try {
                tags = JSON.parse(hidden.value || editor.dataset.existing || '[]');
            } catch {
                tags = [];
            }

            function normalize(value) {
                return value.trim().replace(/\s+/g, ' ');
            }

            function sync() {
                hidden.value = JSON.stringify(tags);
                chips.innerHTML = '';

                tags.forEach(function (tag, index) {
                    const chip = document.createElement('span');
                    chip.className = 'badge bg-primary d-inline-flex align-items-center gap-1';
                    chip.textContent = tag;

                    const remove = document.createElement('button');
                    remove.type = 'button';
                    remove.className = 'btn-close btn-close-white';
                    remove.style.fontSize = '0.55rem';
                    remove.setAttribute('aria-label', 'Remove tag');
                    remove.addEventListener('click', function () {
                        tags.splice(index, 1);
                        sync();
                    });

                    chip.appendChild(remove);
                    chips.appendChild(chip);
                });
            }

            function addTag(value) {
                const tag = normalize(value);
                if (!tag || tags.some(x => x.toLowerCase() === tag.toLowerCase()) || tags.length >= 25) {
                    return;
                }

                tags.push(tag);
                input.value = '';
                sync();
            }

            input.addEventListener('keydown', function (event) {
                if (event.key === 'Enter' || event.key === ',' || event.key === ';') {
                    event.preventDefault();
                    addTag(input.value);
                }
            });

            input.addEventListener('blur', function () {
                addTag(input.value);
            });

            editor.addEventListener('set-tags', function (event) {
                tags = Array.isArray(event.detail) ? event.detail : [];
                sync();
            });

            sync();
        });
    }

    document.addEventListener('DOMContentLoaded', initializeTagEditors);
})();
