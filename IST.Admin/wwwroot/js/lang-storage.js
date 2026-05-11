// Language storage helper — persists selected language in a cookie (1 year).
window.langStorage = {
    get: function () {
        var m = document.cookie.match(/(?:^|;\s*)lang=([^;]*)/);
        return m ? m[1] : 'ru';
    },
    set: function (lang) {
        document.cookie = 'lang=' + lang + ';path=/;max-age=31536000;SameSite=Strict';
        document.documentElement.lang = lang;
    }
};
