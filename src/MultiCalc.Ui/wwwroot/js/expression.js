// The display is editable, so a caret can be put anywhere in it and the text can be
// selected, copied and pasted like text anywhere else.
//
// The keyboard stays away: inputmode="none" and virtualkeyboardpolicy="manual" on the
// element ask for the caret without the keys, because the keypad below is the keyboard.
//
// Offsets crossing this boundary are always counted in characters of the visible text,
// never in DOM nodes, which keeps the C# side free of anything to do with the DOM.
window.multicalcExpression = {
    attach: function (field, dotnet) {
        function offset() {
            const selection = window.getSelection();

            if (!selection || selection.rangeCount === 0) {
                return 0;
            }

            const range = selection.getRangeAt(0);

            if (!field.contains(range.endContainer)) {
                return 0;
            }

            const upToCaret = range.cloneRange();
            upToCaret.selectNodeContents(field);
            upToCaret.setEnd(range.endContainer, range.endOffset);

            return upToCaret.toString().length;
        }

        field.addEventListener('input', function () {
            dotnet.invokeMethodAsync('Edited', field.innerText, offset());
        });

        // Where the caret ended up after a tap or a drag. Reported separately from an edit
        // so that moving it costs a caret update and nothing else.
        function moved() {
            dotnet.invokeMethodAsync('CaretMoved', offset());
        }

        field.addEventListener('click', moved);
        field.addEventListener('keyup', moved);

        // Plain text only. A paste carrying formatting would otherwise drop markup into the
        // display, and the browser would keep it.
        field.addEventListener('paste', function (event) {
            event.preventDefault();

            const text = (event.clipboardData || window.clipboardData).getData('text/plain');
            document.execCommand('insertText', false, text.replace(/\s+/g, ' '));
        });

        // Enter is equals, not a new line: this is one line of text and always will be.
        field.addEventListener('keydown', function (event) {
            if (event.key === 'Enter') {
                event.preventDefault();
                dotnet.invokeMethodAsync('EnterPressed');
            }
        });
    },

    // Writes the model's text into the display and puts the caret back where the model says
    // it is. The text is set here rather than rendered by Blazor because the browser owns
    // the inside of an editable element and rearranges it as the person edits.
    render: function (field, text, caret, landed) {
        // The display is laid out right to left so a long expression keeps its end in view,
        // and the text itself sits in a span that is not, or the operators between the
        // numbers would be reordered. Rebuilt here whenever an edit has flattened it away.
        let inner = field.firstElementChild;

        if (!inner || inner.tagName !== 'SPAN') {
            field.textContent = '';
            inner = document.createElement('span');
            inner.className = 'expression-inner';
            inner.setAttribute('dir', 'ltr');
            field.appendChild(inner);
        }

        if (inner.textContent !== text) {
            inner.textContent = text;
        }

        if (landed) {
            // Removing the class and reading a layout property restarts the animation, which
            // otherwise plays once and never again for the same element.
            field.classList.remove('lands');
            void field.offsetWidth;
            field.classList.add('lands');
        }

        window.multicalcExpression.setCaret(field, caret);
    },

    // Nothing happens while the display is not the focused element, so tapping a key does
    // not steal focus back to a display nobody is editing.
    setCaret: function (field, offset) {
        if (document.activeElement !== field) {
            return;
        }

        const walker = document.createTreeWalker(field, NodeFilter.SHOW_TEXT);
        let node = walker.nextNode();
        let seen = 0;

        while (node) {
            const length = node.nodeValue.length;

            if (seen + length >= offset) {
                const range = document.createRange();
                range.setStart(node, offset - seen);
                range.collapse(true);

                const selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(range);

                return;
            }

            seen += length;
            node = walker.nextNode();
        }
    }
};
