// Wer gerade am PC sitzt, pro Browser gemerkt (1 Jahr) - siehe PersonenAuswahl.razor.
window.personCookie = {
    get: function () {
        const match = document.cookie.match(/(?:^|; )artikelplanung_person=([^;]*)/);
        return match ? decodeURIComponent(match[1]) : null;
    },
    set: function (name) {
        const oneYearInSeconds = 365 * 24 * 60 * 60;
        document.cookie = `artikelplanung_person=${encodeURIComponent(name)}; max-age=${oneYearInSeconds}; path=/; SameSite=Lax`;
    },
};
