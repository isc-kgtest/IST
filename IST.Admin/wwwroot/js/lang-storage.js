// Language storage helper — persists selected language in localStorage.
window.langStorage = {
    get: function () {
        try {
            var v = localStorage.getItem('lang');
            return (v === 'ru' || v === 'kg') ? v : 'ru';
        } catch (e) { return 'ru'; }
    },
    set: function (lang) {
        try { localStorage.setItem('lang', lang); } catch (e) { }
        document.documentElement.lang = lang;
    }
};

// Theme storage helper — persists dark/light mode in localStorage.
window.themeStorage = {
    get: function () {
        try {
            var v = localStorage.getItem('theme');
            if (v === 'dark') return true;
            if (v === 'light') return false;
        } catch (e) { }
        return null; // no preference saved — caller decides default
    },
    set: function (isDark) {
        try { localStorage.setItem('theme', isDark ? 'dark' : 'light'); } catch (e) { }
    }
};
