// Dieses JavaScript fügt in der Jellyfin-Weboberfläche einen Tab-Button
// UND einen Sidebar-Menü-Link hinzu, der ein Overlay mit dem
// Requests-Interface öffnet.
//
// VERSION 8 (Iframe-Navigation):
// - Alle Funktionen von V7.
// - GEÄNDERT: Zurück-Button navigiert jetzt innerhalb des iFrames zurück,
//   anstatt direkt auf Home zu gehen. Nur wenn der iFrame auf der
//   Startseite ist, wird auf Home navigiert.
//
// WICHTIG: Es muss als Javascript injected werden...

(() => {
    const IF_SRC = '/plugins/requests/proxy/';
    const OV_ID = 'requests-overlay';
    const REQ_HASH = '#/requests';
    const HOME_HASH = '#/home';
    const SB_STYLE_ID = 'sb-fix-style';

    const BTN_TAB_ID = 'requests-tab-button';
    const BTN_SIDEBAR_ID = 'requests-sidebar-button';

    // Helpers
    const $ = (s, r = document) => r.querySelector(s);
    const $$ = (s, r = document) => Array.from(r.querySelectorAll(s));

    // ==========================================================
    // Overlay anlegen
    // ==========================================================
    function ensureOverlay() {
        // Style-Fix
        if (!document.getElementById(SB_STYLE_ID)) {
            const s = document.createElement('style');
            s.id = SB_STYLE_ID;
            s.textContent = `.emby-scrollbuttons { right: 0; z-index: 0 !important; }`;
            document.head.appendChild(s);
        }
        if (document.getElementById(OV_ID)) return;

        // Overlay-Erstellung
        const wrap = document.createElement('div');
        wrap.id = OV_ID;
        Object.assign(wrap.style, {
            position: 'fixed',
            inset: '0',
            display: 'none',
            pointerEvents: 'auto'
        });

        // Iframe
        const frame = document.createElement('iframe');
        frame.src = IF_SRC;
        frame.id = 'requests-iframe';
        frame.setAttribute('loading', 'eager');
        Object.assign(frame.style, {
            position: 'absolute',
            left: 0, right: 0, bottom: 0,
            top: '55px',
            width: '100%',
            height: 'calc(100% - 48px)',
            border: '0',
            display: 'block',
            background: 'transparent',
            pointerEvents: 'auto'
        });

        // iFrame-Listener
        frame.addEventListener('load', () => {
            try {
                const iframeWin = frame.contentWindow;
                const iframeDoc = iframeWin.document;
                if (!iframeDoc || !iframeWin) return;

                // 1. iFrame-Klick-Listener (Link-Fix für Jellyfin-Links)
                iframeDoc.addEventListener('click', (e) => {
                    const link = e.target.closest('a');
                    if (!link || !link.href) return;
                    const href = link.href;
                    if (href.includes('/web/index.html#!/item?id=')) {
                        e.preventDefault(); e.stopPropagation();
                        try {
                            const url = new URL(href);
                            const hashParams = new URLSearchParams(url.hash.split('?')[1] || '');
                            const id = hashParams.get('id');
                            const serverId = hashParams.get('serverId');
                            if (id && serverId) {
                                window.top.location.hash = `#/details?id=${id}&serverId=${serverId}`;
                            }
                        } catch (parseError) { /* ... */ }
                    }
                }, true);

            } catch (e) { /* Cross-origin error */ }
        });

        wrap.append(frame);
        document.body.appendChild(wrap);
    }

    // ---------- Iframe-Navigation ----------
    // Versucht im iFrame zurück zu navigieren
    function tryIframeBack() {
        const frame = document.getElementById('requests-iframe');
        if (!frame) return false;

        try {
            const iframeWin = frame.contentWindow;
            if (!iframeWin) return false;

            // Prüfe ob wir im iFrame zurück navigieren können
            // Jellyseerr ist eine SPA, also prüfen wir die Location
            const iframePath = iframeWin.location.pathname + iframeWin.location.search;
            const isOnStartPage = iframePath === '/plugins/requests/proxy/' ||
                                  iframePath === '/plugins/requests/proxy' ||
                                  iframePath === '/' ||
                                  iframePath === '';

            if (isOnStartPage) {
                // Wir sind auf der Startseite, also schließen wir das Overlay
                return false;
            }

            // Versuche zurück zu navigieren im iFrame
            iframeWin.history.back();
            return true;
        } catch (e) {
            // Cross-origin - können nicht auf iFrame zugreifen
            return false;
        }
    }

    // ---------- Overlay Show/Hide ----------
    function showOverlay() {
        ensureOverlay();
        const ov = document.getElementById(OV_ID);
        if (!ov || ov.style.display === 'block') return;

        ov.style.display = 'block';
        document.documentElement.style.overflow = 'hidden';
        document.body.style.overflow = 'hidden';
        document.title = 'Entdecken';
        activateTabButton(true);

        setTimeout(() => {
            const pageTitle = $('.pageTitle');
            if (pageTitle) {
                pageTitle.textContent = 'Entdecken';
            }
        }, 100);
    }

    function hideOverlay() {
        const ov = document.getElementById(OV_ID);
        if (!ov || ov.style.display === 'none') return;

        ov.style.display = 'none';
        ov.style.zIndex = '';
        document.documentElement.style.overflow = '';
        document.body.style.overflow = '';
        activateTabButton(false);
    }

    // ---------- Button-Aktivierung (Tabs) ----------
    function activateTabButton(on) {
        const btn = document.getElementById(BTN_TAB_ID);
        if (!btn) return;
        const slider = btn.closest('.emby-tabs-slider');
        if (!slider) return;
        if (on) {
            $$('.emby-tab-button', slider).forEach(b => {
                if (b.id !== BTN_TAB_ID) {
                    b.classList.remove('emby-tab-button-active', 'lastFocused');
                }
            });
            btn.classList.add('emby-tab-button-active');
        } else {
            btn.classList.remove('emby-tab-button-active');
        }
    }

    // ---------- Button-Aktivierung (Sidebar) ----------
    function activateSidebarLink(on) {
        const btn = document.getElementById(BTN_SIDEBAR_ID);
        if (!btn) return;
        const container = btn.closest('.mainDrawer-scrollContainer');
        if (!container) return;
        if (on) {
            $$('a.navMenuOption', container).forEach(b => {
                if (b.id !== BTN_SIDEBAR_ID) {
                    b.classList.remove('is-active');
                }
            });
            btn.classList.add('is-active');
        } else {
            btn.classList.remove('is-active');
        }
    }

    // ---------- Button in der Tabs-Leiste ----------
    function injectTabButton() {
        if (document.getElementById(BTN_TAB_ID)) return;
        const slider = document.querySelector('.emby-tabs-slider');
        if (!slider) return;
        const fav = $$('.emby-tab-button', slider).find(b => /Favoriten|Favorites/i.test(b.textContent || ''));
        const btn = document.createElement('button');
        btn.type = 'button'; btn.id = BTN_TAB_ID;
        btn.className = 'emby-tab-button emby-button';
        btn.innerHTML = '<div class="emby-button-foreground">Entdecken</div>';
        btn.style.cursor = 'pointer';
        btn.addEventListener('click', (e) => {
            e.preventDefault(); e.stopPropagation();
            if (window.location.hash !== REQ_HASH) window.location.hash = REQ_HASH;
        });
        if (fav && fav.parentElement === slider) slider.insertBefore(btn, fav.nextSibling);
        else slider.appendChild(btn);
    }

    // ---------- Link in der Sidebar ----------
    function injectSidebarLink() {
        if (document.getElementById(BTN_SIDEBAR_ID)) return;
        const homeLink = $('.mainDrawer-scrollContainer a.navMenuOption[href="#/home"]');
        if (!homeLink) return;
        const link = homeLink.cloneNode(true);
        link.id = BTN_SIDEBAR_ID; link.href = REQ_HASH;
        const icon = $('span.material-icons', link);
        if (icon) { icon.textContent = 'playlist_add'; icon.classList.remove('home'); }
        const text = $('.navMenuOptionText', link);
        if (text) { text.textContent = 'Entdecken'; }

        link.addEventListener('click', (e) => {
            e.preventDefault(); e.stopPropagation();
            if (window.location.hash !== REQ_HASH) window.location.hash = REQ_HASH;

            // Schließe die Sidebar
            const menuButton = $('button.mainDrawerButton');
            if (menuButton) {
              menuButton.click();
            }
        });
        homeLink.after(link);
    }

    // ---------- Routing-Logik ----------
    function handleRouting() {
        if (window.location.hash === REQ_HASH) {
            showOverlay();
            activateSidebarLink(true);
        } else {
            hideOverlay();
            activateSidebarLink(false);
        }
    }

    // ==========================================================
    // Navigation - Zurück-Button navigiert im iFrame
    // ==========================================================
    function wireNavigation() {
        // 1. Master-Listener für Hash-Änderungen
        window.addEventListener('hashchange', handleRouting);

        // 2. Master-Klick-Listener für UI-Interaktionen
        const masterClickListener = (ev) => {
            const ov = document.getElementById(OV_ID);
            const isOverlayOpen = (ov && ov.style.display !== 'none');

            // --- "Zurück"-Button im Header ---
            if (isOverlayOpen) {
                const backBtn = ev.target.closest('button.headerBackButton');
                if (backBtn) {
                    ev.preventDefault();
                    ev.stopPropagation();

                    // Versuche erst im iFrame zurück zu navigieren
                    const navigatedInIframe = tryIframeBack();

                    // Wenn das nicht geklappt hat (Startseite), gehe zur Jellyfin Home
                    if (!navigatedInIframe) {
                        window.top.location.hash = HOME_HASH;
                    }
                    return;
                }
            }

            // --- Logik zum Schließen bei Klick ---
            if (!isOverlayOpen) return;
            if (ev.target.closest(`#${OV_ID}`)) return;

            const a = ev.target.closest('a,button');
            if (!a) return;

            if (a.id === BTN_TAB_ID || a.id === BTN_SIDEBAR_ID) return;

            // Prüfen, ob es ein Navigations- oder Action-Element ist
            const href = a.getAttribute('href') || '';
            const isNavElement =
                href.startsWith('#/') ||
                a.classList.contains('navMenuOption') ||
                a.classList.contains('lnkMediaFolder') ||
                a.classList.contains('emby-tab-button');

            // Prüfen auf Header-Buttons (Suche, User)
            const isHeaderAction =
                a.classList.contains('headerSearchButton') ||
                a.classList.contains('headerUserButton');

            if (isNavElement || isHeaderAction) {
                hideOverlay();
            }
        };
        document.addEventListener('click', masterClickListener, true);

        // 3. Keyboard-Navigation (Escape und Backspace)
        document.addEventListener('keydown', (ev) => {
            const ov = document.getElementById(OV_ID);
            const isOverlayOpen = (ov && ov.style.display !== 'none');

            if (!isOverlayOpen) return;

            // Escape schließt das Overlay
            if (ev.key === 'Escape') {
                ev.preventDefault();
                window.top.location.hash = HOME_HASH;
                return;
            }

            // Backspace navigiert zurück (wenn nicht in einem Input-Feld)
            if (ev.key === 'Backspace' && !['INPUT', 'TEXTAREA'].includes(document.activeElement.tagName)) {
                ev.preventDefault();
                const navigatedInIframe = tryIframeBack();
                if (!navigatedInIframe) {
                    window.top.location.hash = HOME_HASH;
                }
            }
        });
    }

    // ---------- Boot & MutationObserver ----------
    const boot = () => {
        injectTabButton();
        injectSidebarLink();
        wireNavigation();
        handleRouting();
    };
    document.addEventListener('DOMContentLoaded', boot);
    boot();

    const mo = new MutationObserver(() => {
        injectTabButton();
        injectSidebarLink();
    });
    mo.observe(document.documentElement, {
        childList: true,
        subtree: true
    });
})();
