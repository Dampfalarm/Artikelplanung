// Esc schließt die Vollbild-Detailansicht, unabhängig davon, welches Element gerade fokussiert ist.
window.escBinder = {
    _handler: null,
    listen: function (dotnetRef) {
        this.unlisten();
        this._handler = function (ev) {
            if (ev.key === "Escape") dotnetRef.invokeMethodAsync("EscGedrueckt");
        };
        document.addEventListener("keydown", this._handler);
    },
    unlisten: function () {
        if (this._handler) {
            document.removeEventListener("keydown", this._handler);
            this._handler = null;
        }
    },
};
