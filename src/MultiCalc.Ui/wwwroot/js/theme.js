// Sets or clears the theme attribute the stylesheet keys off.
window.multicalc = {
    setTheme: function (theme) {
        const root = document.documentElement;

        if (theme === 'system') {
            delete root.dataset.theme;
        } else {
            root.dataset.theme = theme;
        }
    }
};
