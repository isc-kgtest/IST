// Language storage helper — primary store is localStorage; mirror to a cookie
// so the server can read it during SSR pre-render and avoid a RU→KG flash.
window.langStorage = {
    get: function () {
        try {
            var v = localStorage.getItem('lang');
            if (v === 'ru' || v === 'kg') return v;
        } catch (e) { }
        // fall back to the SSR-mirror cookie if localStorage is empty
        var m = document.cookie.match(/(?:^|;\s*)lang=([^;]*)/);
        return (m && (m[1] === 'ru' || m[1] === 'kg')) ? m[1] : 'ru';
    },
    set: function (lang) {
        try { localStorage.setItem('lang', lang); } catch (e) { }
        document.cookie = 'lang=' + lang + ';path=/;max-age=31536000;SameSite=Lax';
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
