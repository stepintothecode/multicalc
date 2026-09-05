// Keys that fire the moment a finger lands, and again as it slides onto the next one,
// the way the phone's own calculator behaves. Waiting for a full press and release makes
// fast entry feel heavy, and a finger that drifts off a key loses the press entirely.
//
// Touch events rather than pointer events: touchmove keeps firing at the element the
// touch started on, so the keypad sees every move even after the finger leaves the first
// key. Pointer events need explicit capture for that and the moves did not arrive.
// Which key is under the finger is a hit test, never the event target.
window.multicalcKeypad = {
    attach: function (root, dotnet) {
        let last = null;

        function keyAt(x, y) {
            const element = document.elementFromPoint(x, y);
            if (!element || !element.closest) {
                return null;
            }

            const key = element.closest('[data-key]');
            return key && root.contains(key) && !key.disabled ? key : null;
        }

        function fire(key) {
            if (!key || key === last) {
                return;
            }

            if (last) {
                last.classList.remove('is-down');
            }

            last = key;
            key.classList.add('is-down');
            dotnet.invokeMethodAsync('PressFromPointer', key.dataset.key);
        }

        function release() {
            if (last) {
                last.classList.remove('is-down');
                last = null;
            }
        }

        root.addEventListener('touchstart', function (event) {
            const touch = event.changedTouches[0];
            last = null;
            fire(keyAt(touch.clientX, touch.clientY));

            // Also stops the browser synthesising a click, so nothing fires twice.
            event.preventDefault();
        }, { passive: false });

        root.addEventListener('touchmove', function (event) {
            const touch = event.changedTouches[0];
            fire(keyAt(touch.clientX, touch.clientY));
            event.preventDefault();
        }, { passive: false });

        root.addEventListener('touchend', release);
        root.addEventListener('touchcancel', release);

        // A mouse, for anyone running this on a desktop. Down rather than click, to match.
        root.addEventListener('mousedown', function (event) {
            if (event.button !== 0) {
                return;
            }

            last = null;
            fire(keyAt(event.clientX, event.clientY));
            event.preventDefault();
        });

        root.addEventListener('mousemove', function (event) {
            if (event.buttons === 1) {
                fire(keyAt(event.clientX, event.clientY));
            }
        });

        root.addEventListener('mouseup', release);
        root.addEventListener('mouseleave', release);
    }
};
