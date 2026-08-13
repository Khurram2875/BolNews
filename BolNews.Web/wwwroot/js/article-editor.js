document.addEventListener('DOMContentLoaded', function () {

    const editorElement = document.getElementById('editor');
    const contentField = document.getElementById('Content');

    // Shared script can safely be loaded on pages
    // that do not contain the article editor.
    if (!editorElement || !contentField) {
        return;
    }

    // ============================================================
    // QUILL IMPORTS
    // ============================================================

    const Parchment = Quill.import('parchment');
    const BlockEmbed = Quill.import('blots/block/embed');
    const ImageBlot = Quill.import('formats/image');
    const Delta = Quill.import('delta');


    // ============================================================
    // GRAMMAR HIGHLIGHT FORMAT
    // ============================================================

    const GrammarHighlightAttributor =
        new Parchment.ClassAttributor(
            'grammarHighlight',
            'grammar-highlight',
            {
                scope: Parchment.Scope.INLINE
            }
        );

    Quill.register(
        GrammarHighlightAttributor,
        true
    );


    // ============================================================
    // IMAGE SIZE ATTRIBUTOR
    // ============================================================

    const ImageSizeAttributor =
        new Parchment.Attributor(
            'data-size',
            'data-size',
            {
                scope: Parchment.Scope.INLINE,
                whitelist: [
                    'small',
                    'medium',
                    'large',
                    'full'
                ]
            }
        );

    Quill.register(
        ImageSizeAttributor,
        true
    );
    const MediaLibraryFormat = new Parchment.Attributor('mediaLibrary', 'data-media-library-noop', {
        scope: Parchment.Scope.INLINE
    });
    Quill.register(MediaLibraryFormat, true);

    // ============================================================
    // SOCIAL EMBED BLOT
    // ============================================================

    class SocialEmbedBlot extends BlockEmbed {

        static create(value) {

            const node = super.create();

            node.setAttribute(
                'class',
                'quill-social-embed'
            );

            node.setAttribute(
                'contenteditable',
                'false'
            );

            node.setAttribute(
                'data-raw-html',
                encodeSocialEmbedHtml(value)
            );

            let platformName = 'Social Media';

            const domainMatch = value.match(
                /(?:www\.)?([a-zA-Z0-9-]+)\.(?:com|net|org|be)/i
            );

            if (domainMatch && domainMatch[1]) {

                platformName =
                    domainMatch[1]
                        .charAt(0)
                        .toUpperCase()
                    +
                    domainMatch[1].slice(1);

                if (
                    platformName.toLowerCase()
                    === 'youtu'
                ) {
                    platformName = 'YouTube';
                }

                if (
                    platformName.toLowerCase()
                    === 'fb'
                ) {
                    platformName = 'Facebook';
                }
            }

            node.innerHTML = `
                <div class="embed-placeholder-box">

                    <span class="embed-icon">
                        🌐
                    </span>

                    <p class="embed-title">
                        ${platformName} Integration Widget
                    </p>

                    <small class="embed-subtitle">
                        (The complete interactive embed will
                        render live on your main website, you can also preview from article list page)
                    </small>

                </div>
            `;

            return node;
        }


        static value(node) {

            const encodedData =
                node.getAttribute(
                    'data-raw-html'
                );

            if (encodedData) {

                return decodeSocialEmbedHtml(encodedData);
            }

            return node.innerHTML;
        }
    }

    class MediaEmbedBlot extends BlockEmbed {
        static create(value) {
            const node = super.create();
            node.classList.add('quill-media-embed');
            node.setAttribute('contenteditable', 'false');
            node.setAttribute('data-media-type', value.type);
            node.setAttribute('data-url', value.url);
            node.innerHTML = value.type === 'audio'
                ? `<audio controls src="${value.url}"></audio>`
                : `<video controls src="${value.url}" style="max-width:100%"></video>`;
            return node;
        }

        static value(node) {
            return { type: node.getAttribute('data-media-type'), url: node.getAttribute('data-url') };
        }
    }


    SocialEmbedBlot.blotName = 'socialEmbed';
    SocialEmbedBlot.tagName = 'div';
    SocialEmbedBlot.className = 'quill-social-embed';

    MediaEmbedBlot.blotName = 'mediaEmbed';
    MediaEmbedBlot.tagName = 'div';
    MediaEmbedBlot.className = 'quill-media-embed';


    Quill.register(
        ImageBlot,
        true
    );

    Quill.register(
        SocialEmbedBlot,
        true
    );

    Quill.register(MediaEmbedBlot, true);


    // ============================================================
    // QUILL INITIALIZATION
    // ============================================================

    let selectedImage = null;
    let activeGrammarIssues = [];

    const icons = Quill.import('ui/icons');
    icons['mediaLibrary'] = '<svg viewBox="0 0 18 18"><rect class="ql-stroke" height="10" width="12" x="3" y="4"></rect><circle class="ql-fill" cx="6" cy="7" r="1"></circle><polyline class="ql-even ql-fill" points="5,12 5,9 9,11 11,9 13,12"></polyline></svg>';

    const quill = new Quill(
        '#editor',
        {
            theme: 'snow',

            modules: {

                toolbar: [

                    [
                        {
                            header: [
                                1,
                                2,
                                3,
                                4,
                                false
                            ]
                        }
                    ],

                    [
                        'bold',
                        'italic',
                        'underline',
                        'strike'
                    ],

                    [
                        { color: [] },
                        { background: [] }
                    ],

                    [
                        { align: [] }
                    ],

                    [
                        { list: 'ordered' },
                        { list: 'bullet' }
                    ],

                    [
                        'blockquote'
                    ],

                    [
                        'link',
                        'image',
                        'mediaLibrary'
                    ],

                    [
                        'clean'
                    ]
                ]
            }
        }
    );


    // Makes Quill available in DevTools.
    window.quill = quill;


    quill.clipboard.addMatcher(
        'div.quill-social-embed',
        function (node) {
            const rawHtml =
                getSocialEmbedRawHtml(node);

            return new Delta().insert({
                socialEmbed: rawHtml
            });
        }
    );

    quill.clipboard.addMatcher(
        'div[data-media-type][data-url]',
        function (node) {
            return new Delta().insert({
                mediaEmbed: {
                    type: node.getAttribute('data-media-type'),
                    url: node.getAttribute('data-url')
                }
            });
        }
    );


    // ============================================================
    // LOAD EXISTING ARTICLE CONTENT
    // ============================================================

    if (contentField.value) {

        quill.clipboard.dangerouslyPasteHTML(
            contentField.value
        );
    }


    // ============================================================
    // IMAGE UPLOAD
    // ============================================================

    const toolbar =
        quill.getModule('toolbar');

    toolbar.addHandler(
        'image',
        imageHandler
    );

    toolbar.addHandler('mediaLibrary', () => document.querySelector('[data-media-picker="inline"]')?.click());


    async function imageHandler() {

        const input =
            document.createElement('input');

        input.type = 'file';
        input.accept = 'image/*';

        input.click();


        input.onchange = async () => {

            const file = input.files[0];

            if (!file) {
                return;
            }

            const formData =
                new FormData();

            formData.append(
                'upload',
                file
            );

            try {

                const token =
                    document.querySelector(
                        'input[name="__RequestVerificationToken"]'
                    )?.value;

                const response = await fetch(
                    '/Admin/Articles/UploadEditorImage',
                    {
                        method: 'POST',

                        headers: {
                            'RequestVerificationToken':
                                token
                        },

                        body: formData
                    }
                );

                if (!response.ok) {
                    throw new Error(
                        'Upload failed'
                    );
                }

                const result =
                    await response.json();

                const range =
                    quill.getSelection(true);

                quill.insertEmbed(
                    range.index,
                    'image',
                    result.url
                );

                quill.setSelection(
                    range.index + 1
                );
            }
            catch (error) {

                console.error(error);

                alert(
                    'Image upload failed.'
                );
            }
        };
    }


    // ============================================================
    // IMAGE SELECTION
    // ============================================================

    quill.root.addEventListener(
        'click',
        function (e) {

            document
                .querySelectorAll(
                    '.ql-editor img'
                )
                .forEach(function (img) {

                    img.classList.remove(
                        'selected-image'
                    );
                });

            if (e.target.tagName === 'IMG') {

                selectedImage = e.target;

                selectedImage
                    .classList
                    .add(
                        'selected-image'
                    );
            }
            else {

                selectedImage = null;
            }
        }
    );


    // ============================================================
    // IMAGE ALIGNMENT
    // ============================================================

    const imgLeft =
        document.getElementById('imgLeft');

    const imgCenter =
        document.getElementById('imgCenter');

    const imgRight =
        document.getElementById('imgRight');


    function applyImageAlignment(
        alignmentClass
    ) {

        if (!selectedImage) {
            return;
        }

        const parent =
            selectedImage.parentElement;

        parent.classList.remove(
            'ql-align-left',
            'ql-align-center',
            'ql-align-right'
        );

        parent.classList.add(
            alignmentClass
        );
    }


    if (imgLeft) {

        imgLeft.addEventListener(
            'click',
            function () {

                applyImageAlignment(
                    'ql-align-left'
                );
            }
        );
    }


    if (imgCenter) {

        imgCenter.addEventListener(
            'click',
            function () {

                applyImageAlignment(
                    'ql-align-center'
                );
            }
        );
    }


    if (imgRight) {

        imgRight.addEventListener(
            'click',
            function () {

                applyImageAlignment(
                    'ql-align-right'
                );
            }
        );
    }


    // ============================================================
    // IMAGE SIZE
    // ============================================================

    const imgSmall =
        document.getElementById('imgSmall');

    const imgMedium =
        document.getElementById('imgMedium');

    const imgLarge =
        document.getElementById('imgLarge');

    const imgFull =
        document.getElementById('imgFull');


    function applyImageSize(size) {

        if (!selectedImage) {
            return;
        }

        selectedImage.setAttribute(
            'data-size',
            size
        );

        const blot =
            Quill.find(selectedImage);

        if (blot) {

            blot.format(
                'data-size',
                size
            );

            quill.update();

            contentField.value =
                cleanHtml(
                    quill.root.innerHTML
                );
        }
    }


    if (imgSmall) {

        imgSmall.addEventListener(
            'click',
            () => applyImageSize('small')
        );
    }


    if (imgMedium) {

        imgMedium.addEventListener(
            'click',
            () => applyImageSize('medium')
        );
    }


    if (imgLarge) {

        imgLarge.addEventListener(
            'click',
            () => applyImageSize('large')
        );
    }


    if (imgFull) {

        imgFull.addEventListener(
            'click',
            () => applyImageSize('full')
        );
    }
    // ============================================================
    // SOCIAL EMBED
    // ============================================================

    const btnSocialEmbed =
        document.getElementById(
            'btnSocialEmbed'
        );


    if (btnSocialEmbed) {

        btnSocialEmbed.addEventListener(
            'click',
            function () {

                const embedCode = prompt(
                    'Paste your full Social Media Embed Code:'
                );

                if (
                    embedCode &&
                    embedCode.trim() !== ''
                ) {

                    const range =
                        quill.getSelection(true);

                    quill.insertEmbed(
                        range.index,
                        'socialEmbed',
                        embedCode.trim()
                    );

                    quill.setSelection(
                        range.index + 1
                    );
                }
            }
        );
    }


    // ============================================================
    // REMOVE SOCIAL EMBED
    // ============================================================

    quill.root.addEventListener(
        'dblclick',
        function (e) {

            const embedWrapper =
                e.target.closest(
                    '.quill-social-embed'
                );

            if (!embedWrapper) {
                return;
            }

            const confirmDelete =
                confirm(
                    'Do you want to remove this social media embed?'
                );

            if (!confirmDelete) {
                return;
            }

            const blot =
                Quill.find(embedWrapper);

            if (blot) {

                blot.remove();

                quill.update();
            }
        }
    );


    // ============================================================
    // GRAMMAR REVIEW ELEMENTS
    // ============================================================

    const btnCheckGrammar =
        document.getElementById(
            'btnCheckGrammar'
        );

    const btnCloseGrammar =
        document.getElementById(
            'btnCloseGrammar'
        );

    const btnApplyAllGrammar =
        document.getElementById(
            'btnApplyAllGrammar'
        );

    const editorColumn =
        document.getElementById(
            'editorColumn'
        );

    const grammarPanelColumn =
        document.getElementById(
            'grammarPanelColumn'
        );

    const grammarLoading =
        document.getElementById(
            'grammarLoading'
        );

    const grammarSuggestions =
        document.getElementById(
            'grammarSuggestions'
        );

    const grammarIssueCount =
        document.getElementById(
            'grammarIssueCount'
        );

    const grammarActions =
        document.getElementById(
            'grammarActions'
        );


    // ============================================================
    // CHECK GRAMMAR - REAL API
    // ============================================================

    if (btnCheckGrammar) {

        btnCheckGrammar.addEventListener(
            'click',
            async function () {

                openGrammarPanel();

                clearGrammarHighlights();

                activeGrammarIssues = [];

                grammarLoading
                    ?.classList
                    .remove('d-none');

                if (grammarSuggestions) {
                    grammarSuggestions.innerHTML = '';
                }

                grammarActions
                    ?.classList
                    .add('d-none');

                if (grammarIssueCount) {

                    grammarIssueCount.textContent =
                        'Checking content...';
                }

                btnCheckGrammar.disabled = true;


                try {

                    const editorText =
                        quill.getText();

                    if (!editorText.trim()) {

                        renderGrammarSuggestions([]);

                        if (grammarIssueCount) {

                            grammarIssueCount.textContent =
                                'No content to check';
                        }

                        return;
                    }


                    const token =
                        document.querySelector(
                            'input[name="__RequestVerificationToken"]'
                        )?.value;


                    const response = await fetch(
                        '/Admin/Articles/CheckGrammar',
                        {
                            method: 'POST',

                            headers: {
                                'Content-Type':
                                    'application/json',

                                'RequestVerificationToken':
                                    token
                            },

                            body: JSON.stringify({
                                text: editorText,
                                language: 'en-US'
                            })
                        }
                    );


                    if (!response.ok) {

                        const errorData =
                            await response
                                .json()
                                .catch(() => null);

                        throw new Error(
                            errorData?.message
                            ?? 'Grammar check failed.'
                        );
                    }


                    const data =
                        await response.json();


                    activeGrammarIssues =
                        (data.issues ?? [])
                            .map(
                                function (issue) {

                                    return {
                                        id: issue.id,

                                        category:
                                            issue.category,

                                        original:
                                            issue.original,

                                        replacement:
                                            issue.replacement,

                                        message:
                                            issue.message,

                                        ruleId:
                                            issue.ruleId,

                                        index:
                                            issue.offset,

                                        length:
                                            issue.length
                                    };
                                }
                            );


                    activeGrammarIssues =
                        normalizeGrammarIssues(
                            activeGrammarIssues
                        );


                    refreshGrammarReview();
                }
                catch (error) {

                    console.error(
                        'Grammar check error:',
                        error
                    );

                    activeGrammarIssues = [];

                    clearGrammarHighlights();

                    if (grammarIssueCount) {

                        grammarIssueCount.textContent =
                            'Grammar check failed';
                    }

                    if (grammarSuggestions) {

                        grammarSuggestions.innerHTML = `
                            <div class="alert alert-danger mb-0">
                                ${escapeHtml(
                            error.message
                            || 'Unable to check grammar.'
                        )}
                            </div>
                        `;
                    }

                    grammarActions
                        ?.classList
                        .add('d-none');
                }
                finally {

                    grammarLoading
                        ?.classList
                        .add('d-none');

                    btnCheckGrammar.disabled =
                        false;
                }
            }
        );
    }


    // ============================================================
    // OPEN / CLOSE GRAMMAR PANEL
    // ============================================================

    if (btnCloseGrammar) {

        btnCloseGrammar.addEventListener(
            'click',
            function () {

                closeGrammarPanel();
            }
        );
    }


    function openGrammarPanel() {

        if (
            !editorColumn ||
            !grammarPanelColumn
        ) {
            return;
        }

        editorColumn.classList.remove(
            'col-12'
        );

        editorColumn.classList.add(
            'col-lg-8'
        );

        grammarPanelColumn.classList.remove(
            'd-none'
        );
    }


    function closeGrammarPanel() {

        if (
            !editorColumn ||
            !grammarPanelColumn
        ) {
            return;
        }

        grammarPanelColumn.classList.add(
            'd-none'
        );

        editorColumn.classList.remove(
            'col-lg-8'
        );

        editorColumn.classList.add(
            'col-12'
        );
    }


    // ============================================================
    // GRAMMAR PANEL BUTTONS
    // ============================================================

    if (grammarSuggestions) {

        grammarSuggestions.addEventListener(
            'click',
            function (e) {

                const applyButton =
                    e.target.closest(
                        '.btn-apply-grammar'
                    );

                if (applyButton) {

                    e.stopPropagation();

                    const issueId =
                        Number(
                            applyButton.dataset.issueId
                        );

                    applyGrammarIssue(
                        issueId
                    );

                    return;
                }


                const ignoreButton =
                    e.target.closest(
                        '.btn-ignore-grammar'
                    );

                if (ignoreButton) {

                    e.stopPropagation();

                    const issueId =
                        Number(
                            ignoreButton.dataset.issueId
                        );

                    ignoreGrammarIssue(
                        issueId
                    );
                }
            }
        );
    }


    if (btnApplyAllGrammar) {

        btnApplyAllGrammar.addEventListener(
            'click',
            function () {

                applyAllGrammarIssues();
            }
        );
    }
    // ============================================================
    // APPLY ONE GRAMMAR SUGGESTION
    // ============================================================

    function applyGrammarIssue(issueId) {

        const issueIndex =
            activeGrammarIssues.findIndex(
                issue => issue.id === issueId
            );

        if (issueIndex === -1) {
            return;
        }

        const issue =
            activeGrammarIssues[issueIndex];


        const currentIndex =
            resolveGrammarIssueIndex(issue);


        if (currentIndex === -1) {

            alert(
                'The article content has changed since the grammar check. ' +
                'Please run Check Grammar again.'
            );

            return;
        }


        issue.index =
            currentIndex;

        issue.length =
            issue.original.length;


        // Verify that the article text still matches
        // the text checked by the grammar service.
        const currentText =
            quill.getText(
                issue.index,
                issue.length
            );


        if (currentText !== issue.original) {

            alert(
                'The article content has changed since the grammar check. ' +
                'Please run Check Grammar again.'
            );

            return;
        }


        // Remove the original text.
        quill.deleteText(
            issue.index,
            issue.length,
            'user'
        );


        // Insert the suggested replacement.
        quill.insertText(
            issue.index,
            issue.replacement,
            'user'
        );


        const lengthDifference =
            issue.replacement.length
            -
            issue.length;


        // Remove the resolved issue.
        activeGrammarIssues.splice(
            issueIndex,
            1
        );


        // Update the positions of all issues
        // located after the corrected text.
        activeGrammarIssues.forEach(
            function (remainingIssue) {

                if (
                    remainingIssue.index >
                    issue.index
                ) {

                    remainingIssue.index +=
                        lengthDifference;
                }
            }
        );


        refreshGrammarReview();
    }


    // ============================================================
    // IGNORE ONE GRAMMAR SUGGESTION
    // ============================================================

    function ignoreGrammarIssue(issueId) {

        const issueIndex =
            activeGrammarIssues.findIndex(
                issue => issue.id === issueId
            );

        if (issueIndex === -1) {
            return;
        }


        // Remove the issue from the current review only.
        // The article text remains unchanged.
        activeGrammarIssues.splice(
            issueIndex,
            1
        );


        refreshGrammarReview();
    }


    // ============================================================
    // APPLY ALL GRAMMAR SUGGESTIONS
    // ============================================================

    function applyAllGrammarIssues() {

        if (
            activeGrammarIssues.length === 0
        ) {
            return;
        }


        // Work on a copy of the active issues.
        const issuesToApply = [
            ...activeGrammarIssues
        ];


        // Process from the end of the article backwards
        // so earlier replacements cannot shift later offsets.
        issuesToApply.sort(
            (a, b) => b.index - a.index
        );


        // Validate every issue before making any changes.
        const invalidIssue =
            issuesToApply.find(
                function (issue) {

                    const currentIndex =
                        resolveGrammarIssueIndex(
                            issue
                        );

                    if (currentIndex === -1) {
                        return true;
                    }


                    issue.index =
                        currentIndex;

                    issue.length =
                        issue.original.length;

                    return false;
                }
            );


        if (invalidIssue) {

            alert(
                'The article content has changed since the grammar check. ' +
                'Please run Check Grammar again.'
            );

            return;
        }


        // Apply all replacements.
        issuesToApply.forEach(
            function (issue) {

                quill.deleteText(
                    issue.index,
                    issue.length,
                    'user'
                );

                quill.insertText(
                    issue.index,
                    issue.replacement,
                    'user'
                );
            }
        );


        activeGrammarIssues = [];


        refreshGrammarReview();
    }


    // ============================================================
    // REFRESH GRAMMAR REVIEW
    // ============================================================

    function refreshGrammarReview() {

        clearGrammarHighlights();

        activeGrammarIssues =
            normalizeGrammarIssues(
                activeGrammarIssues
            );


        activeGrammarIssues.forEach(
            function (issue) {

                quill.formatText(
                    issue.index,
                    issue.length,
                    'grammarHighlight',
                    'error',
                    'silent'
                );
            }
        );


        renderGrammarSuggestions(
            activeGrammarIssues
        );
    }


    // ============================================================
    // CLEAR GRAMMAR HIGHLIGHTS
    // ============================================================

    function clearGrammarHighlights() {

        quill.formatText(
            0,
            quill.getLength(),
            'grammarHighlight',
            false,
            'silent'
        );
    }


    // ============================================================
    // NORMALIZE GRAMMAR ISSUE POSITIONS
    // ============================================================

    function normalizeGrammarIssues(issues) {

        return issues
            .map(
                function (issue) {

                    const index =
                        resolveGrammarIssueIndex(
                            issue
                        );

                    if (index === -1) {
                        return null;
                    }

                    return {
                        ...issue,
                        index: index,
                        length:
                            issue.original.length
                    };
                }
            )
            .filter(
                function (issue) {
                    return issue !== null;
                }
            );
    }


    function resolveGrammarIssueIndex(issue) {

        const editorText =
            quill.getText();

        const expectedLength =
            issue.original.length;

        if (
            Number.isInteger(issue.index) &&
            issue.index >= 0 &&
            quill.getText(
                issue.index,
                expectedLength
            ) === issue.original
        ) {
            return issue.index;
        }

        const fromCurrentOffset =
            editorText.indexOf(
                issue.original,
                Math.max(issue.index || 0, 0)
            );

        if (fromCurrentOffset !== -1) {
            return fromCurrentOffset;
        }

        return editorText.indexOf(
            issue.original
        );
    }


    // ============================================================
    // RENDER GRAMMAR SUGGESTIONS
    // ============================================================

    function renderGrammarSuggestions(
        issues
    ) {

        if (
            !grammarSuggestions ||
            !grammarIssueCount
        ) {
            return;
        }


        grammarSuggestions.innerHTML = '';


        grammarIssueCount.textContent =
            `${issues.length} issue${issues.length === 1 ? '' : 's'} found`;


        if (issues.length === 0) {

            grammarSuggestions.innerHTML = `
                <div class="alert alert-success mb-0">
                    No grammar issues found.
                </div>
            `;


            grammarActions
                ?.classList
                .add('d-none');


            return;
        }


        issues.forEach(
            function (issue) {

                const suggestion =
                    document.createElement(
                        'div'
                    );


                suggestion.className =
                    'grammar-suggestion';


                suggestion.dataset.issueId =
                    issue.id;

                suggestion.dataset.index =
                    issue.index;

                suggestion.dataset.length =
                    issue.length;


                suggestion.innerHTML = `

                    <div class="d-flex justify-content-between align-items-center mb-2">

                        <span class="badge text-bg-secondary">
                            ${escapeHtml(issue.category)}
                        </span>

                    </div>


                    <div class="mb-2">

                        <span class="grammar-suggestion-original">
                            ${escapeHtml(issue.original)}
                        </span>

                        <span class="mx-2">
                            →
                        </span>

                        <span class="grammar-suggestion-replacement text-success">
                            ${escapeHtml(issue.replacement)}
                        </span>

                    </div>


                    <div class="small text-muted mb-3">
                        ${escapeHtml(issue.message)}
                    </div>


                    <div class="d-flex gap-2">

                        <button
                            type="button"
                            class="btn btn-sm btn-primary btn-apply-grammar"
                            data-issue-id="${issue.id}">

                            Apply

                        </button>


                        <button
                            type="button"
                            class="btn btn-sm btn-outline-secondary btn-ignore-grammar"
                            data-issue-id="${issue.id}">

                            Ignore

                        </button>

                    </div>
                `;


                grammarSuggestions.appendChild(
                    suggestion
                );


                // Clicking a suggestion card navigates
                // to the corresponding text in Quill.
                suggestion.addEventListener(
                    'click',
                    function (e) {

                        if (
                            e.target.closest('button')
                        ) {
                            return;
                        }


                        const index =
                            Number(
                                suggestion.dataset.index
                            );

                        const length =
                            Number(
                                suggestion.dataset.length
                            );


                        quill.setSelection(
                            index,
                            length,
                            'silent'
                        );


                        quill.scrollSelectionIntoView();


                        quill.focus();
                    }
                );
            }
        );


        grammarActions
            ?.classList
            .remove('d-none');
    }
    // ============================================================
    // CONTENT SYNCHRONIZATION
    // ============================================================

    quill.on(
        'text-change',
        function () {

            contentField.value =
                cleanHtml(
                    quill.root.innerHTML
                );
        }
    );


    // ============================================================
    // FORM SUBMISSION
    // ============================================================

    //const form =
    //    document.querySelector('form');
    const articleForm = document.getElementById('articleForm');

    if (articleForm) {

        articleForm.addEventListener(
            'submit',
            function () {

                contentField.value =
                    cleanHtml(
                        quill.root.innerHTML
                    );
            }
        );
    }


    // ============================================================
    // HTML HELPERS
    // ============================================================

    function escapeHtml(value) {

        const div =
            document.createElement('div');


        div.textContent =
            value ?? '';


        return div.innerHTML;
    }


    function cleanHtml(html) {

        const div =
            document.createElement('div');


        div.innerHTML = html;


        // --------------------------------------------------------
        // REMOVE TEMPORARY IMAGE SELECTION MARKER
        // --------------------------------------------------------

        div.querySelectorAll('img')
            .forEach(
                function (img) {

                    img.classList.remove(
                        'selected-image'
                    );
                }
            );


        // --------------------------------------------------------
        // REMOVE TEMPORARY GRAMMAR HIGHLIGHTS
        // --------------------------------------------------------

        div.querySelectorAll(
            '[class*="grammar-highlight-"]'
        )
            .forEach(
                function (span) {

                    span.replaceWith(
                        ...span.childNodes
                    );
                }
            );


        // --------------------------------------------------------
        // REMOVE UNWANTED PASTED FORMATTING
        // --------------------------------------------------------

        div.querySelectorAll('span')
            .forEach(
                function (span) {

                    const style =
                        span.getAttribute(
                            'style'
                        );


                    if (
                        style
                        &&
                        style.includes(
                            'color: rgb(0, 0, 0)'
                        )
                        &&
                        style.includes(
                            'background-color: rgb(255, 255, 255)'
                        )
                    ) {

                        span.replaceWith(
                            ...span.childNodes
                        );
                    }
                }
            );

        div.querySelectorAll('.quill-social-embed')
            .forEach(
                function (embed) {
                    const rawHtml =
                        getSocialEmbedRawHtml(embed);

                    embed.setAttribute(
                        'contenteditable',
                        'false'
                    );

                    embed.setAttribute(
                        'data-raw-html',
                        encodeSocialEmbedHtml(rawHtml)
                    );

                    embed.innerHTML =
                        SocialEmbedBlot.create(rawHtml).innerHTML;
                }
            );

        div.querySelectorAll('div[data-media-type][data-url]')
            .forEach(
                function (embed) {
                    embed.classList.add(
                        'quill-media-embed'
                    );

                    embed.setAttribute(
                        'contenteditable',
                        'false'
                    );
                }
            );


        return div.innerHTML;
    }


    function getSocialEmbedRawHtml(node) {

        const encodedData =
            node.getAttribute(
                'data-raw-html'
            );

        if (encodedData) {

            return decodeSocialEmbedHtml(
                encodedData
            );
        }

        return node.innerHTML;
    }


    function encodeSocialEmbedHtml(value) {

        return btoa(
            unescape(
                encodeURIComponent(value ?? '')
            )
        );
    }


    function decodeSocialEmbedHtml(value) {

        try {

            return decodeURIComponent(
                escape(
                    atob(value)
                )
            );
        }
        catch {

            return '';
        }
    }

});
