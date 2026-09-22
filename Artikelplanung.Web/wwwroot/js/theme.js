// Farbmodus-Umschalter: merkt sich Hell/Dunkel im Browser (localStorage), Standard folgt
// prefers-color-scheme. Siehe rechnungsablage-design/design-system.md.
window.themeStore = {
    get: function () {
        try { return localStorage.getItem("artikelplanung_theme"); }
        catch { return null; }
    },
    set: function (wert) {
        try {
            if (wert) localStorage.setItem("artikelplanung_theme", wert);
            else localStorage.removeItem("artikelplanung_theme");
        } catch { /* Storage nicht verfügbar, z. B. privates Fenster */ }
    },
    apply: function (wert) {
        var el = document.documentElement;
        if (wert === "dark") el.setAttribute("data-theme", "dark");
        else if (wert === "light") el.setAttribute("data-theme", "light");
        else el.removeAttribute("data-theme");
    },
    prefersDark: function () {
        return typeof matchMedia === "function" && matchMedia("(prefers-color-scheme: dark)").matches;
    },
};
