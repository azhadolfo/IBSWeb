/**
 * Quick Access Sidebar
 * Tracks frequently clicked nav links via localStorage and surfaces them
 * in a collapsible sidebar panel on the right edge of the screen.
 *
 * Storage keys:
 *   qa_clicks   — JSON object { url: { label, count, company } }
 *   qa_open     — "true" | "false"  (panel open/closed state)
 */

(function () {
    'use strict';

    const STORAGE_KEY = 'qa_clicks';
    const STATE_KEY   = 'qa_open';
    const MAX_RECENT  = 5;   // "Recent" section cap
    const MAX_TOP     = 8;   // "Most Used" section cap
    const MIN_COUNT   = 1;   // minimum clicks to appear in Most Used

    /* ─── Helpers ─── */

    function getClicks() {
        try { return JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}'); }
        catch { return {}; }
    }

    function saveClicks(data) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
    }

    function clearClicks() {
        localStorage.removeItem(STORAGE_KEY);
        localStorage.removeItem('qa_recent');
    }

    function getRecent() {
        try { return JSON.parse(localStorage.getItem('qa_recent') || '[]'); }
        catch { return []; }
    }

    function pushRecent(url, label) {
        const company = getCompanyFromUrl(url);
        let recent    = getRecent().filter(r => r.url !== url);
        recent.unshift({ url, label, company });
        if (recent.length > MAX_RECENT) recent = recent.slice(0, MAX_RECENT);
        localStorage.setItem('qa_recent', JSON.stringify(recent));
    }
    function isPanelOpen() {
        return localStorage.getItem(STATE_KEY) !== 'false';
    }

    /* ─── Track clicks on all existing nav links ─── */

    // Keep a WeakSet so we never double-attach a listener to the same element
    const _tracked = new WeakSet();

    function attachTracking() {
        const navLinks = document.querySelectorAll(
            'nav.navbar a.nav-link:not([href="#"]):not([href=""]):not([href^="http"]),' +
            'nav.navbar a.dropdown-item:not([href="#"]):not([href=""])'
        );

        navLinks.forEach(anchor => {
            if (_tracked.has(anchor)) return; // already listening
            _tracked.add(anchor);

            anchor.addEventListener('click', function () {
                const url   = this.getAttribute('href') || this.href;
                const label = (this.textContent || '').trim();
                if (!url || url === '#' || !label) return;
                if (url === '/' || url.toLowerCase().includes('/home/')) return;

                recordClick(url, label);
            });
        });
    }

    function getCurrentCompany() {
        return (document.getElementById('hfCompany')?.value || '').trim();
    }

    function getCompanyFromUrl(url) {
        const lower = (url || '').toLowerCase();
        if (lower.includes('/filpride/')) return 'Filpride';
        if (lower.includes('/mobility/')) return 'Mobility';
        if (lower.includes('/bienes/'))   return 'Bienes';
        if (lower.includes('/mmsi/'))     return 'MMSI';
        return '';
    }

    /**
     * Sanitize URLs before using them in href attributes to prevent XSS via
     * dangerous schemes like "javascript:" or cross-origin URLs.
     * Returns a safe, normalized URL string, or null if the URL is not allowed.
     */
    function sanitizeUrl(url) {
        if (!url || typeof url !== 'string') return null;

        try {
            // Support relative URLs by resolving against the current origin
            const base = window.location.origin;
            const parsed = new URL(url, base);

            const protocol = parsed.protocol.toLowerCase();

            // Only allow HTTP(S) URLs on the same origin
            if (protocol !== 'http:' && protocol !== 'https:') {
                return null;
            }

            if (parsed.origin !== window.location.origin) {
                return null;
            }

            // Return the path/query/fragment relative to origin to preserve existing behavior
            return parsed.pathname + parsed.search + parsed.hash;
        } catch {
            // Malformed URLs are treated as unsafe
            return null;
        }
    }

    function recordClick(url, label) {
        const data    = getClicks();
        const company = getCompanyFromUrl(url);
        if (!data[url]) {
            data[url] = { label, count: 0, company };
        }
        data[url].count++;
        data[url].label   = label;
        data[url].company = company;
        saveClicks(data);
        pushRecent(url, label);
    }

    function isAvailable(entry) {
        if (!entry.company) return true;
        return entry.company === getCurrentCompany();
    }

    /* ─── Build sidebar HTML ─── */

    function buildSidebar() {
        // Panel
        const panel = document.createElement('div');
        panel.id = 'qa-panel';
        if (!isPanelOpen()) panel.classList.add('qa-hidden');

        panel.innerHTML = `
            <div class="qa-panel-header">
                <span>Quick Access</span>
                <button id="qa-close-btn" title="Close">
                    <i class="bi bi-x"></i>
                </button>
            </div>
            <div class="qa-search-wrap">
                <input type="text" id="qa-search" placeholder="Search links…" autocomplete="off" />
            </div>
            <div class="qa-list-area" id="qa-list"></div>
            <div class="qa-footer">
                <button class="qa-clear-btn" id="qa-clear" title="Clear history">
                    <i class="bi bi-trash"></i> Reset history
                </button>
            </div>
        `;

        // Toggle button
        const toggleBtn = document.createElement('button');
        toggleBtn.id = 'qa-toggle-btn';
        toggleBtn.title = 'Quick Access';
        toggleBtn.innerHTML = '';

        document.body.appendChild(panel);
        document.body.appendChild(toggleBtn);

        // Events
        toggleBtn.addEventListener('click', togglePanel);
        document.getElementById('qa-close-btn').addEventListener('click', togglePanel);
        document.getElementById('qa-clear').addEventListener('click', () => {
            if (confirm('Clear all Quick Access history?')) {
                clearClicks();
                renderList();
            }
        });
        document.getElementById('qa-search').addEventListener('input', function () {
            renderList(this.value.trim().toLowerCase());
        });

        renderList();

        // Close when clicking outside
        document.addEventListener('click', function (e) {
            const panel       = document.getElementById('qa-panel');
            const toggleBtn   = document.getElementById('qa-toggle-btn');
            const navTrigger  = document.getElementById('qa-nav-trigger');
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
        const list    = document.getElementById('qa-list');
        const clicks  = getClicks();
        const recent  = getRecent();

        list.innerHTML = '';

        // ── Most Used ──
        let topItems = Object.entries(clicks)
            .filter(([, v]) => v.count >= MIN_COUNT && isAvailable(v))
            .sort((a, b) => b[1].count - a[1].count)
            .slice(0, MAX_TOP);

        if (filter) {
            topItems = topItems.filter(([, v]) =>
                v.label.toLowerCase().includes(filter)
            );
        }

        if (topItems.length > 0) {
            const label = document.createElement('div');
            label.className = 'qa-section-label';
            label.innerHTML = 'Most Used';
            list.appendChild(label);

            topItems.forEach(([url, v]) => {
                list.appendChild(makeItem(url, v.label, v.count, v));
            });
        }

        // ── Recent ──
        let recentFiltered = recent.filter(r => isAvailable(r));
        if (filter) {
            recentFiltered = recent.filter(r =>
                r.label.toLowerCase().includes(filter) ||
                r.url.toLowerCase().includes(filter)
            );
        }

        if (recentFiltered.length > 0) {
            if (topItems.length > 0) {
                const hr = document.createElement('hr');
                hr.className = 'qa-divider';
                list.appendChild(hr);
            }
            const label = document.createElement('div');
            label.className = 'qa-section-label';
            label.innerHTML = 'Recent';
            list.appendChild(label);

            recentFiltered.forEach(r => {
                list.appendChild(makeItem(r.url, r.label, null, r));
            });
        }

        // ── Empty state ──
        if (topItems.length === 0 && recentFiltered.length === 0) {
            list.innerHTML = `
                <div class="qa-empty">
                    ${filter ? 'No matches found.' : 'Click nav links to start<br>building quick access.'}
                </div>`;
        }
    }

    function makeItem(url, label, count, entry) {
        const a = document.createElement('a');
        const safeUrl = sanitizeUrl(url);
        // If the URL is not considered safe, fall back to a non-navigating link
        a.href      = safeUrl !== null ? safeUrl : '#';
        a.className = 'qa-item';
        a.title     = count > 1 ? `${label} — visited ${count}×` : label;

        const labelEl = document.createElement('span');
        labelEl.textContent = label;

        a.appendChild(labelEl);

        a.addEventListener('click', function () {
            recordClick(url, label);
        });

        return a;
    }

    /* ─── Toggle ─── */

    function togglePanel() {
        const panel = document.getElementById('qa-panel');
        const isNowHidden = panel.classList.toggle('qa-hidden');
        localStorage.setItem(STATE_KEY, isNowHidden ? 'false' : 'true');

        if (!isNowHidden) {
            // Refresh list when opening
            const search = document.getElementById('qa-search');
            if (search) {
                search.value = '';
                renderList();
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

        const observer = new MutationObserver(() => attachTracking());
        observer.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();