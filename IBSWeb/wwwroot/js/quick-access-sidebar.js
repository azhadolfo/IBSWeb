/**
 * Quick Access Sidebar
 * - Tracks frequently clicked nav links via localStorage
 * - Search scans ALL nav links in the DOM (not just tracked ones)
 * - Auto-opens on the home page (/)
 *
 * Storage keys:
 *   qa_clicks   — JSON object { url: { label, count, company } }
 *   qa_open     — "true" | "false"  (panel open/closed state)
 */

(function () {
    'use strict';

    const STORAGE_KEY = 'qa_clicks';
    const STATE_KEY   = 'qa_open';
    const MAX_RECENT  = 5;
    const MAX_TOP     = 8;
    const MIN_COUNT   = 1;

    /* ─── Safe Storage Helpers ─── */

    /**
     * Safely retrieve a value from localStorage with a fallback.
     * Catches SecurityError, QuotaExceededError, or any other exception.
     */
    function safeLocalGet(key, defaultValue) {
        try {
            return localStorage.getItem(key) ?? defaultValue;
        } catch (err) {
            // SecurityError, QuotaExceededError, or other storage access exceptions
            console.warn(`Cannot access localStorage['${key}']:`, err);
            return defaultValue;
        }
    }

    /**
     * Safely retrieve a value from sessionStorage with a fallback.
     * Catches SecurityError, QuotaExceededError, or any other exception.
     */
    function safeSessionGet(key, defaultValue) {
        try {
            return sessionStorage.getItem(key) ?? defaultValue;
        } catch (err) {
            // SecurityError, QuotaExceededError, or other storage access exceptions
            console.warn(`Cannot access sessionStorage['${key}']:`, err);
            return defaultValue;
        }
    }

    /**
     * Safely write a value to localStorage.
     * Catches SecurityError, QuotaExceededError, or any other exception.
     */
    function safeLocalSet(key, value) {
        try {
            localStorage.setItem(key, value);
        } catch (err) {
            // SecurityError, QuotaExceededError, or other storage access exceptions
            console.warn(`Cannot write to localStorage['${key}']:`, err);
        }
    }

    /**
     * Safely write a value to sessionStorage.
     * Catches SecurityError, QuotaExceededError, or any other exception.
     */
    function safeSessionSet(key, value) {
        try {
            sessionStorage.setItem(key, value);
        } catch (err) {
            // SecurityError, QuotaExceededError, or other storage access exceptions
            console.warn(`Cannot write to sessionStorage['${key}']:`, err);
        }
    }

    /* ─── Data Helpers ─── */

    function getClicks() {
        try { return JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}'); }
        catch { return {}; }
    }

    function saveClicks(data) {
        safeLocalSet(STORAGE_KEY, JSON.stringify(data));
    }

    function clearClicks() {
        try {
            localStorage.removeItem(STORAGE_KEY);
            localStorage.removeItem('qa_recent');
        } catch { /* quota/security error */ }
    }

    function getRecent() {
        try { return JSON.parse(localStorage.getItem('qa_recent') || '[]'); }
        catch { return []; }
    }

    function pushRecent(url, label, breadcrumb) {
        const company = getCompanyFromUrl(url);
        let recent    = getRecent().filter(r => r.url !== url);
        recent.unshift({ url, label, company, breadcrumb: breadcrumb || '' });
        if (recent.length > MAX_RECENT) recent = recent.slice(0, MAX_RECENT);
        safeLocalSet('qa_recent', JSON.stringify(recent));
    }

    function isPanelOpen() {
        return safeLocalGet(STATE_KEY, '') !== 'false';
    }

    function isHomePage() {
        return window.location.pathname === '/';
    }

    /* ─── URL helpers ─── */

    function getCurrentCompany() {
        const fromInput = (document.getElementById('hfCompany')?.value || '').trim();
        const fromData  = (
            document.documentElement.dataset.selectedCompany ||
            document.body.dataset.selectedCompany ||
            ''
        ).trim();
        return fromInput || fromData;
    }

    function getCompanyFromUrl(url) {
        const lower = (url || '').toLowerCase();
        if (lower.includes('/filpride/')) return 'Filpride';
        if (lower.includes('/mobility/')) return 'Mobility';
        if (lower.includes('/bienes/'))   return 'Bienes';
        if (lower.includes('/mmsi/'))     return 'MMSI';
        return '';
    }

    function sanitizeUrl(url) {
        if (!url || typeof url !== 'string') return null;
        try {
            const parsed = new URL(url, window.location.origin);
            const protocol = parsed.protocol.toLowerCase();
            if (protocol !== 'http:' && protocol !== 'https:') return null;
            if (parsed.origin !== window.location.origin) return null;
            return parsed.pathname + parsed.search + parsed.hash;
        } catch {
            return null;
        }
    }

    function isAvailable(entry) {
        const current = getCurrentCompany();
        // If no company is selected yet (e.g. home page), show everything
        if (!current) return true;
        // If the link has no company in its URL, always show it
        if (!entry.company) return true;
        return entry.company === current;
    }

    function isHomeUrl(url) {
        return url === '/' || url.toLowerCase().includes('/home/');
    }

    /* ─── Breadcrumb resolver ─── */

    function getBreadcrumb(anchor) {
        const parts = [];
        let el = anchor.parentElement;

        while (el && !el.classList.contains('navbar-nav')) {
            if (el.classList.contains('dropdown-menu')) {
                const toggle = el.previousElementSibling ||
                    el.parentElement?.querySelector(':scope > .dropdown-toggle, :scope > .nav-link.dropdown-toggle');
                if (toggle) {
                    const text = (toggle.textContent || '').trim();
                    if (text) parts.unshift(text);
                }
            }
            el = el.parentElement;
        }

        return parts.join(' › ');
    }

    /* ─── Collect all nav links from the DOM ─── */

    function getAllNavLinks() {
        const seen  = new Set();
        const links = [];

        document.querySelectorAll(
            'nav.navbar a.dropdown-item:not([href="#"]):not([href=""]),' +
            'nav.navbar a.nav-link:not([href="#"]):not([href=""])'
        ).forEach(anchor => {
            const rawUrl  = anchor.getAttribute('href') || '';
            const url     = sanitizeUrl(rawUrl);
            const label   = (anchor.textContent || '').trim();
            if (!url || !label || isHomeUrl(url)) return;
            if (anchor.classList.contains('dropdown-toggle')) return;
            const company = getCompanyFromUrl(url);
            const key     = url + '|' + company;
            if (seen.has(key)) return;
            seen.add(key);
            links.push({
                url,
                label,
                breadcrumb : getBreadcrumb(anchor),
                company,
            });
        });

        return links;
    }

    /* ─── Track clicks ─── */

    const _tracked = new WeakSet();

    function attachTracking() {
        const navLinks = document.querySelectorAll(
            'nav.navbar a.nav-link:not([href="#"]):not([href=""]),' +
            'nav.navbar a.dropdown-item:not([href="#"]):not([href=""])'
        );

        navLinks.forEach(anchor => {
            if (_tracked.has(anchor)) return;
            _tracked.add(anchor);

            anchor.addEventListener('click', function () {
                const rawUrl     = this.getAttribute('href') || this.href;
                const url        = sanitizeUrl(rawUrl);
                const label      = (this.textContent || '').trim();
                const breadcrumb = getBreadcrumb(this);
                if (!url || !label) return;
                if (isHomeUrl(url)) return;
                recordClick(url, label, breadcrumb);
            });
        });
    }

    function recordClick(url, label, breadcrumb) {
        const data    = getClicks();
        const company = getCompanyFromUrl(url);
        if (!data[url]) {
            data[url] = { label, count: 0, company, breadcrumb: breadcrumb || '' };
        }
        data[url].count++;
        data[url].label     = label;
        data[url].company   = company;
        data[url].breadcrumb = breadcrumb || data[url].breadcrumb || '';
        saveClicks(data);
        pushRecent(url, label, breadcrumb);
    }

    /* ─── Build sidebar HTML ─── */

    function buildSidebar() {
        const panel = document.createElement('div');
        panel.id = 'qa-panel';

        // Auto-open on home page, otherwise restore saved state
        if (!isHomePage() && !isPanelOpen()) {
            panel.classList.add('qa-hidden');
        }

        panel.innerHTML = `
            <div class="qa-panel-header">
                <span>Quick Access</span>
                <button id="qa-close-btn" title="Close" aria-label="Close Quick Access sidebar">
                    <i class="bi bi-x"></i>
                </button>
            </div>
            <div class="qa-search-wrap">
                <input type="text" id="qa-search" placeholder="Search all links…" autocomplete="off" />
            </div>
            <div class="qa-list-area" id="qa-list"></div>
            <div class="qa-footer">
                <button class="qa-clear-btn" id="qa-clear" title="Clear history">
                    <i class="bi bi-trash"></i> Reset history
                </button>
            </div>
        `;

        const toggleBtn = document.createElement('button');
        toggleBtn.id = 'qa-toggle-btn';
        toggleBtn.title = 'Quick Access';
        toggleBtn.setAttribute('aria-label', 'Toggle Quick Access sidebar');
        toggleBtn.setAttribute('aria-expanded', String(isHomePage() || isPanelOpen()));
        toggleBtn.innerHTML = '';
        if (isHomePage() || isPanelOpen()) toggleBtn.classList.add('qa-panel-open');

        document.body.appendChild(panel);
        document.body.appendChild(toggleBtn);

        toggleBtn.addEventListener('click', togglePanel);
        document.getElementById('qa-close-btn').addEventListener('click', togglePanel);
        document.getElementById('qa-clear').addEventListener('click', () => {
            if (confirm('Clear all Quick Access history?')) {
                clearClicks();
                renderList();
            }
        });
        document.getElementById('qa-search').addEventListener('input', function () {
            const term = this.value.trim().toLowerCase();
            safeSessionSet('qa_search', this.value.trim());
            renderList(term);
        });

        // Restore saved search term from session
        const savedSearch = safeSessionGet('qa_search', '');
        const searchInput = document.getElementById('qa-search');
        searchInput.value = savedSearch;
        renderList(savedSearch.toLowerCase());

        document.addEventListener('click', function (e) {
            const panel      = document.getElementById('qa-panel');
            const toggleBtn  = document.getElementById('qa-toggle-btn');
            const navTrigger = document.getElementById('qa-nav-trigger');
            if (!panel.classList.contains('qa-hidden') &&
                !panel.contains(e.target) &&
                !toggleBtn.contains(e.target) &&
                !(navTrigger && navTrigger.contains(e.target))) {
                togglePanel();
            }
        });
    }

    /* ─── Render list ─── */

    function renderList(filter) {
        const list   = document.getElementById('qa-list');
        if (!list) return;
        const clicks = getClicks();
        const recent = getRecent();

        list.innerHTML = '';

        // ── Search mode: scan all nav links from the DOM ──
        if (filter) {
            const allLinks = getAllNavLinks()
                .filter(l => isAvailable(l))
                .filter(l =>
                    l.label.toLowerCase().includes(filter) ||
                    l.url.toLowerCase().includes(filter)
                );

            if (allLinks.length > 0) {
                const label = document.createElement('div');
                label.className = 'qa-section-label';
                label.textContent = `Results (${allLinks.length})`;
                list.appendChild(label);

                allLinks.forEach(l => {
                    const clickData = clicks[l.url];
                    list.appendChild(makeItem(l.url, l.label, clickData?.count || null, l.breadcrumb));
                });
            } else {
                const empty = document.createElement('div');
                empty.className = 'qa-empty';
                empty.textContent = 'No links found.';
                list.appendChild(empty);
            }
            return;
        }

        // ── Default mode: Most Used + Recent ──
        let topItems = Object.entries(clicks)
            .filter(([, v]) => v.count >= MIN_COUNT && isAvailable(v))
            .sort((a, b) => b[1].count - a[1].count)
            .slice(0, MAX_TOP);

        if (topItems.length > 0) {
            const label = document.createElement('div');
            label.className = 'qa-section-label';
            label.textContent = 'Most Used';
            list.appendChild(label);

            topItems.forEach(([url, v]) => {
                list.appendChild(makeItem(url, v.label, v.count, v.breadcrumb));
            });
        }

        let recentFiltered = recent.filter(r => isAvailable(r));

        if (recentFiltered.length > 0) {
            if (topItems.length > 0) {
                const hr = document.createElement('hr');
                hr.className = 'qa-divider';
                list.appendChild(hr);
            }
            const label = document.createElement('div');
            label.className = 'qa-section-label';
            label.textContent = 'Recent';
            list.appendChild(label);

            recentFiltered.forEach(r => {
                list.appendChild(makeItem(r.url, r.label, null, r.breadcrumb));
            });
        }

        if (topItems.length === 0 && recentFiltered.length === 0) {
            const empty = document.createElement('div');
            empty.className = 'qa-empty';
            empty.textContent = 'Click nav links to start building quick access.';
            list.appendChild(empty);
        }
    }

    function makeItem(url, label, count, breadcrumb) {
        const safeUrl = sanitizeUrl(url);
        const a = document.createElement('a');
        // codeql[js/xss-through-dom] safeUrl is always a same-origin relative path produced by sanitizeUrl()
        a.href      = safeUrl !== null ? safeUrl : '#';
        a.className = 'qa-item';
        a.title     = count > 1 ? `${label} — visited ${count}×` : label;

        const labelEl = document.createElement('span');
        labelEl.className = 'qa-item-label';
        labelEl.textContent = label;
        a.appendChild(labelEl);

        if (breadcrumb) {
            const crumbEl = document.createElement('span');
            crumbEl.className = 'qa-item-crumb';
            crumbEl.textContent = breadcrumb;
            a.appendChild(crumbEl);
        }

        a.addEventListener('click', function (e) {
            e.stopPropagation();
            if (safeUrl) recordClick(safeUrl, label, breadcrumb);
            togglePanel();
        });

        return a;
    }

    /* ─── Toggle ─── */

    function togglePanel() {
        const panel     = document.getElementById('qa-panel');
        const toggleBtn = document.getElementById('qa-toggle-btn');
        if (!panel) return;
        const isNowHidden = panel.classList.toggle('qa-hidden');
        safeLocalSet(STATE_KEY, isNowHidden ? 'false' : 'true');

        if (toggleBtn) {
            toggleBtn.setAttribute('aria-expanded', String(!isNowHidden));
            toggleBtn.classList.toggle('qa-panel-open', !isNowHidden);
        }

        if (!isNowHidden) {
            const search = document.getElementById('qa-search');
            if (search) {
                const savedSearch = safeSessionGet('qa_search', '');
                search.value = savedSearch;
                renderList(savedSearch.toLowerCase());
                search.focus();
            }
        }
    }

    /* ─── Navbar trigger ─── */

    function injectNavbarTrigger() {
        const navList = document.querySelector('nav.navbar ul.navbar-nav');
        if (!navList) return;

        const li = document.createElement('li');
        li.className = 'nav-item';
        li.id = 'qa-nav-trigger';

        const btn = document.createElement('a');
        btn.className = 'nav-link';
        btn.href = '#';
        btn.title = 'Quick Access';
        btn.setAttribute('aria-label', 'Toggle Quick Access sidebar');
        btn.innerHTML = '<i class="bi bi-lightning-charge-fill"></i>';
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            togglePanel();
        });

        li.appendChild(btn);
        navList.insertBefore(li, navList.firstChild);
    }

    /* ─── Init ─── */

    function init() {
        buildSidebar();
        injectNavbarTrigger();
        attachTracking();

        const navbar = document.querySelector('nav.navbar');
        if (navbar) {
            const observer = new MutationObserver(() => attachTracking());
            observer.observe(navbar, { childList: true, subtree: true });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();