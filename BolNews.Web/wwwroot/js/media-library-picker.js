document.addEventListener('DOMContentLoaded', () => {
    const modal = document.getElementById('mediaLibraryModal');
    if (!modal) return;

    let mode = 'inline';
    let mediaById = new Map();
    const dialog = bootstrap.Modal.getOrCreateInstance(modal);
    const results = document.getElementById('mediaLibraryResults');

    async function load(query = '') {
        const response = await fetch(
            `/Admin/MediaLibrary/Picker?query=${encodeURIComponent(query)}`
        );

        const items = await response.json();

        mediaById = new Map(
            items.map(item => [item.id, item])
        );

        results.innerHTML = items.map(item => {

            let previewHtml;

            if (item.mediaType === 1) {

                // Image
                previewHtml = `
                <img src="${item.largeUrl || item.url}"
                     class="img-fluid mb-2"
                     style="height:100px;width:100%;object-fit:cover"
                     alt="${item.caption || item.originalFileName || 'Image'}">
            `;

            }
            else if (item.mediaType === 3) {

                // Video
                if (item.thumbnailUrl) {

                    previewHtml = `
                    <div class="position-relative mb-2"
                         style="height:100px;width:100%;overflow:hidden;">

                        <img src="${item.thumbnailUrl}"
                             class="img-fluid"
                             style="height:100%;width:100%;object-fit:cover"
                             alt="${item.caption || item.originalFileName || 'Video'}">

                        <span class="position-absolute top-50 start-50 translate-middle
                                     d-flex align-items-center justify-content-center
                                     bg-dark bg-opacity-75 rounded-circle"
                              style="width:42px;height:42px;">

                            <i class="bi bi-play-fill text-white fs-4"></i>

                        </span>

                    </div>
                `;

                }
                else {

                    // Older videos without thumbnails
                    previewHtml = `
                    <div class="py-3 text-center"
                         style="height:100px;">

                        <i class="bi bi-camera-video fs-2"></i>

                    </div>
                `;
                }

            }
            else {

                // Audio
                previewHtml = `
                <div class="py-3 text-center"
                     style="height:100px;">

                    <i class="bi bi-music-note-beamed fs-2"></i>

                </div>
            `;
            }

            return `
            <div class="col-md-4">

                <button type="button"
                        class="btn btn-light w-100 text-start media-library-item"
                        data-id="${item.id}">

                    ${previewHtml}

                    <small>
                        ${item.caption || item.originalFileName || 'Untitled media'}
                    </small>

                </button>

            </div>
        `;

        }).join('') || '<p class="text-muted">No media found.</p>';
    }

    document.querySelectorAll('[data-media-picker]').forEach(button => button.addEventListener('click', () => {
        mode = button.dataset.mediaPicker;
        load();
        dialog.show();
    }));

    document.getElementById('mediaLibrarySearch').addEventListener('input', event => load(event.target.value));

    results.addEventListener('click', event => {
        const button = event.target.closest('.media-library-item');
        const item = button && mediaById.get(Number(button.dataset.id));
        if (!item) return;

        if (mode === 'featured') {
            if (item.mediaType !== 1) return alert('Only images can be a featured image.');
            document.getElementById('FeaturedMediaId').value = item.id;
            const preview = document.getElementById('previewImage');
            if (preview) preview.src = item.largeUrl || item.url;

            setField('FeaturedImageAltText', item.altText);
            setField('FeaturedImageCaption', item.caption);
            setField('FeaturedImageCredit', item.credit);
            if (item.tags.length) {
                document.getElementById('FeaturedImageTagsInput_editor')
                    ?.dispatchEvent(new CustomEvent('set-tags', { detail: item.tags.map(tag => tag.name) }));
            }
        } else if (window.quill) {
            const range = window.quill.getSelection(true);
            if (item.mediaType === 1) window.quill.insertEmbed(range.index, 'image', item.url);
            else window.quill.insertEmbed(range.index, 'mediaEmbed', { type: item.mediaType === 2 ? 'audio' : 'video', url: item.url });
            window.quill.setSelection(range.index + 1);
        }

        dialog.hide();
    });

    function setField(id, value) {
        if (value) document.getElementById(id).value = value;
    }
});
