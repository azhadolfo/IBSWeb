/**
 * Quick Access Sidebar
 * Tracks frequently clicked nav links via localStorage and surfaces them
 * in a collapsible sidebar panel on the left edge of the screen.
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
       try { localStorage.setItem(STORAGE_KEY, JSON.stringify(data)); } catch { /* ignore */ }
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
        try {
            localStorage.setItem('qa_recent', JSON.stringify(recent));
        } catch (e) {
            console.warn('Quick Access: Failed to save recent items', e);
        }
    }

    function isPanelOpen() {
        return localStorage.getItem(STATE_KEY) !== 'false';
    }

    /* ─── URL helpers ─── */

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
     * Sanitize URLs — only allow same-origin HTTP(S) relative paths.
     * Prevents XSS via javascript: or cross-origin hrefs.
     */
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
        if (!entry.company) return true;
        return entry.company === getCurrentCompany();
    }

    /* ─── Track clicks on all existing nav links ─── */

    const _tracked = new WeakSet();

    function attachTracking() {
        const navLinks = document.querySelectorAll(
            'nav.navbar a.nav-link:not([href="#"]):not([href=""]):not([href^="http"]),' +
            'nav.navbar a.dropdown-item:not([href="#"]):not([href=""]):not([href^="http"])'
        );

        navLinks.forEach(anchor => {
            if (_tracked.has(anchor)) return;
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

    /* ─── Build sidebar HTML ─── */

    function buildSidebar() {
        // Panel
        const panel = document.createElement('div');
        panel.id = 'qa-panel';
        if (!isPanelOpen()) panel.classList.add('qa-hidden');

        panel.innerHTML = `
            <div class="qa-panel-header">
                <span>Quick Access</span>
                <button id="qa-close-btn" title="Close" aria-label="Close Quick Access sidebar">
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
        toggleBtn.setAttribute('aria-label', 'Toggle Quick Access sidebar');
        toggleBtn.setAttribute('aria-expanded', String(isPanelOpen()));
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

        // Close when clicking outside — exclude both toggle buttons
        document.addEventListener('click', function (e) {
            const panel      = document.getElementById('qa-panel');
            const toggleBtn  = document.getElementById('qa-toggle-btn');
            const navTrigger = document.getElementById('qa-nav-trigger');
            if (panel && toggleBtn &&
                !panel.classList.contains('qa-hidden') &&
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
        const clicks = getClicks();
        const recent = getRecent();

        list.innerHTML = '';

        // ── Most Used ──
        let topItems = Object.entries(clicks)
            .filter(([, v]) => v.count >= MIN_COUNT && isAvailable(v))
            .sort((a, b) => b[1].count - a[1].count)
            .slice(0, MAX_TOP);

        if (filter) {
            topItems = topItems.filter(([url, v]) =>
                v.label.toLowerCase().includes(filter) ||
                url.toLowerCase().includes(filter)
            );
        }

        if (topItems.length > 0) {
            const label = document.createElement('div');
            label.className = 'qa-section-label';
            label.textContent = 'Most Used';
            list.appendChild(label);

            topItems.forEach(([url, v]) => {
                list.appendChild(makeItem(url, v.label, v.count, v));
            });
        }

        // ── Recent ──
        // Fix: filter by availability first, THEN apply search filter on the result
        let recentFiltered = recent.filter(r => isAvailable(r));
        if (filter) {
            recentFiltered = recentFiltered.filter(r =>
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
            label.textContent = 'Recent';
            list.appendChild(label);

            recentFiltered.forEach(r => {
                list.appendChild(makeItem(r.url, r.label, null, r));
            });
        }

        // ── Empty state ──
        if (topItems.length === 0 && recentFiltered.length === 0) {
            const empty = document.createElement('div');
            empty.className = 'qa-empty';
            empty.textContent = filter ? 'No matches found.' : 'Click nav links to start building quick access.';
            list.appendChild(empty);
        }
    }

    function makeItem(url, label, count, entry) {
        const safeUrl = sanitizeUrl(url);
        const a = document.createElement('a');
        // codeql[js/xss-through-dom] safeUrl is always a same-origin relative path produced by sanitizeUrl()
        a.href = safeUrl !== null ? safeUrl : '#';
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
        const panel     = document.getElementById('qa-panel');
        const toggleBtn = document.getElementById('qa-toggle-btn');
        const isNowHidden = panel.classList.toggle('qa-hidden');
        localStorage.setItem(STATE_KEY, isNowHidden ? 'false' : 'true');

        // Keep aria-expanded in sync
        if (toggleBtn) toggleBtn.setAttribute('aria-expanded', String(!isNowHidden));

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

        // Fix: scope observer to navbar only, not entire body
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