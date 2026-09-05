/**
 * modern-navbar.js
 * Expanding Multi-Column Connected Dropdown Navbar controller for IBSWeb.MMSI.
 *
 * Features:
 *  - Miller Columns Deck: Clicking a parent item dynamically expands sub-columns to the right within the connected card
 *  - Top nav open/close (click + outside click + Escape)
 *  - Spotlight search (press / to focus, indexes all nested and parent links)
 *  - Responsive mobile drawer support
 *  - User dropdown menu (profile, settings, logout)
 */
(function () {
    'use strict';

    /* ══════════════════════════════════════════════════════
       Quick Access sidebar — suppressed in modern navbar
    ══════════════════════════════════════════════════════ */
    function suppressQuickAccess() {
        var panel   = document.getElementById('qa-panel');
        var trigger = document.getElementById('qa-nav-trigger');
        if (panel)   { panel.classList.add('qa-hidden'); panel.style.display = 'none'; }
        if (trigger) { trigger.style.display = 'none'; }
    }

    /* ══════════════════════════════════════════════════════
       Expanding Multi-Column Deck Controller
    ══════════════════════════════════════════════════════ */
    function setupColumnDecks() {
        var overlay = document.getElementById('mnav-overlay');
        var topItems = document.querySelectorAll('.mnav-item[data-nav-deck]');

        function closeAllDecks() {
            topItems.forEach(function (topItem) {
                topItem.classList.remove('mnav-open');
                var t = topItem.querySelector(':scope > .mnav-trigger');
                if (t) t.setAttribute('aria-expanded', 'false');
                resetDeck(topItem);
            });
            if (overlay) overlay.classList.remove('active');
        }

        function resetDeck(topItem) {
            var deck = topItem.querySelector('.mnav-column-deck');
            if (!deck) return;

            // Reset columns: show only level 1
            var cols = deck.querySelectorAll('.mnav-col');
            cols.forEach(function (col) {
                var level = parseInt(col.getAttribute('data-col-level') || '1', 10);
                if (level === 1) {
                    col.style.display = 'flex';
                } else {
                    col.style.display = 'none';
                    col.classList.remove('active-tree');
                }
            });

            // Reset active item states
            deck.querySelectorAll('.mnav-col-item.active').forEach(function (item) {
                item.classList.remove('active');
            });

            // Reset aria-expanded on expand buttons
            deck.querySelectorAll('.mnav-col-item[data-expand]').forEach(function (item) {
                item.setAttribute('aria-expanded', 'false');
            });

            // Set width to 1 column
            if (window.innerWidth >= 1540) {
                deck.style.width = '270px';
            } else {
                deck.style.width = '100%';
            }
        }

        topItems.forEach(function (topItem) {
            var trigger = topItem.querySelector(':scope > .mnav-trigger');
            var deck    = topItem.querySelector('.mnav-column-deck');
            if (!trigger || !deck) return;

            trigger.addEventListener('click', function (e) {
                e.stopPropagation();
                var wasOpen = topItem.classList.contains('mnav-open');
                closeAllDecks();

                if (!wasOpen) {
                    resetDeck(topItem);
                    topItem.classList.add('mnav-open');
                    trigger.setAttribute('aria-expanded', 'true');
                    if (overlay) overlay.classList.add('active');
                }
            });

            // Handle Column Expand Triggers
            deck.addEventListener('click', function (e) {
                var expandBtn = e.target.closest('.mnav-col-item[data-expand]');
                if (!expandBtn) return;

                e.preventDefault();
                e.stopPropagation();

                var targetId = expandBtn.getAttribute('data-expand');
                var targetCol = deck.querySelector('.mnav-col[data-col-id="' + targetId + '"]');
                if (!targetCol) return;

                var currentCol = expandBtn.closest('.mnav-col');
                var currentLevel = parseInt(currentCol ? currentCol.getAttribute('data-col-level') || '1' : '1', 10);
                var targetLevel = parseInt(targetCol.getAttribute('data-col-level') || (currentLevel + 1), 10);

                // Toggle active state in current column
                currentCol.querySelectorAll('.mnav-col-item[data-expand]').forEach(function (i) {
                    i.classList.remove('active');
                    i.setAttribute('aria-expanded', 'false');
                });
                expandBtn.classList.add('active');
                expandBtn.setAttribute('aria-expanded', 'true');

                // Hide any columns at or above targetLevel that aren't the target
                deck.querySelectorAll('.mnav-col').forEach(function (col) {
                    var lvl = parseInt(col.getAttribute('data-col-level') || '1', 10);
                    if (lvl >= targetLevel && col !== targetCol) {
                        col.style.display = 'none';
                        col.classList.remove('active-tree');
                        col.querySelectorAll('.mnav-col-item.active').forEach(function (act) { act.classList.remove('active'); });
                    }
                });

                // Show target column
                targetCol.style.display = 'flex';
                targetCol.classList.add('active-tree');

                // Update deck width smoothly based on number of visible columns
                if (window.innerWidth >= 1540) {
                    var visibleCols = Array.from(deck.querySelectorAll('.mnav-col')).filter(function (col) {
                        return col.style.display === 'flex' || window.getComputedStyle(col).display === 'flex';
                    });
                    deck.style.width = (visibleCols.length * 270) + 'px';
                }
            });
        });

        if (overlay) overlay.addEventListener('click', closeAllDecks);
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeAllDecks(); });
    }

    /* ══════════════════════════════════════════════════════
       Spotlight search
    ══════════════════════════════════════════════════════ */
    function collectAllNavLinks() {
        var seen  = new Set();
        var links = [];

        function sanitize(url) {
            if (!url || url === '#' || url === '') return null;
            try {
                var p = new URL(url, window.location.origin);
                if (p.origin !== window.location.origin) return null;
                return p.pathname + p.search;
            } catch (_) { return null; }
        }

        function getCleanLabel(anchor) {
            var titleEl = anchor.querySelector('.mnav-col-item-title, .mnav-link-title, .title-md');
            if (titleEl) return titleEl.textContent.trim();

            var clone = anchor.cloneNode(true);
            clone.querySelectorAll('.material-symbols-outlined, i, svg, .mnav-col-item-sub, .mnav-chevron').forEach(function (el) { el.remove(); });
            return clone.textContent.trim().replace(/\s+/g, ' ');
        }

        function getItemIcon(anchor) {
            var iconEl = anchor.querySelector('.material-symbols-outlined:not(.mnav-chevron), i');
            if (iconEl) {
                if (iconEl.classList.contains('material-symbols-outlined')) {
                    var text = iconEl.textContent.trim();
                    if (text && text !== 'expand_more' && text !== 'chevron_right') {
                        return { type: 'material', icon: text };
                    }
                } else if (iconEl.className) {
                    return { type: 'class', icon: iconEl.className };
                }
            }
            return { type: 'material', icon: 'chevron_right' };
        }

        function getHierarchyPath(anchor) {
            var topItem = anchor.closest('.mnav-item');
            var topName = '';
            if (topItem) {
                var topTrig = topItem.querySelector(':scope > .mnav-trigger');
                if (topTrig) {
                    var clone = topTrig.cloneNode(true);
                    clone.querySelectorAll('.material-symbols-outlined, i, svg').forEach(function (el) { el.remove(); });
                    topName = clone.textContent.trim().replace(/\s+/g, ' ');
                }
            }

            var col = anchor.closest('.mnav-col');
            var subName = '';
            if (col) {
                var level = parseInt(col.getAttribute('data-col-level') || '1', 10);
                // Level 1 items belong directly under the top name (e.g. A. Receivable, A. Payable)
                // Sub-columns (level >= 2) provide a specific category like Check Voucher or Collection Receipts
                if (level >= 2) {
                    var headerEl = col.querySelector('.mnav-col-header');
                    if (headerEl) {
                        subName = headerEl.textContent.trim().replace(/\s+/g, ' ');
                    }
                }
            }

            if (topName && subName && topName.toLowerCase() !== subName.toLowerCase()) {
                return topName + ' › ' + subName;
            }
            return topName || subName || 'Navigation';
        }

        function push(anchor) {
            var raw = anchor.getAttribute('href') || '';
            var url = sanitize(raw);
            if (!url) return;
            if (url === '/' || url.toLowerCase().includes('/home/')) return;
            if (seen.has(url)) return;
            seen.add(url);

            var label = getCleanLabel(anchor);
            if (!label) return;

            var section = getHierarchyPath(anchor);
            var icon = getItemIcon(anchor);
            links.push({ url: url, label: label, section: section, icon: icon });
        }

        document.querySelectorAll('#modern-navbar a[href]:not([href="#"]):not([data-search-ignore])').forEach(push);

        return links;
    }

    function buildSearchDropdown() {
        var drop = document.createElement('div');
        drop.id = 'mnav-search-results';
        return drop;
    }

    function renderSearchResults(drop, query) {
        drop.innerHTML = '';

        if (!query) { drop.style.display = 'none'; return; }

        var q     = query.toLowerCase();
        var links = collectAllNavLinks().filter(function (l) {
            return l.label.toLowerCase().includes(q) || l.section.toLowerCase().includes(q) || l.url.toLowerCase().includes(q);
        });

        if (links.length === 0) {
            drop.style.display = 'block';
            drop.innerHTML = '<div class="mnav-search-empty"><span class="material-symbols-outlined">search_off</span>No results for "<strong>' + escapeHtml(query) + '</strong>"</div>';
            return;
        }

        drop.style.display = 'block';

        var grouped = {};
        links.forEach(function (l) {
            var sec = l.section || 'Navigation';
            if (!grouped[sec]) grouped[sec] = [];
            grouped[sec].push(l);
        });

        Object.entries(grouped).forEach(function (_a) {
            var sec = _a[0], items = _a[1];
            var header = document.createElement('div');
            header.className = 'mnav-search-section-header';
            header.textContent = sec;
            drop.appendChild(header);

            items.forEach(function (l) {
                var a = document.createElement('a');
                a.href = l.url;
                a.className = 'mnav-search-result-item';
                var iconHtml = l.icon && l.icon.type === 'class'
                    ? '<i class="' + escapeHtml(l.icon.icon) + '"></i>'
                    : '<span class="material-symbols-outlined">' + escapeHtml(l.icon ? l.icon.icon : 'chevron_right') + '</span>';
                a.innerHTML = iconHtml + '<span>' + highlightMatch(escapeHtml(l.label), escapeHtml(q)) + '</span>';
                drop.appendChild(a);
            });
        });
    }

    function escapeHtml(str) {
        return str.replace(/[&<>"']/g, function (c) { return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]; });
    }

    function highlightMatch(label, query) {
        var idx = label.toLowerCase().indexOf(query.toLowerCase());
        if (idx === -1) return label;
        return label.slice(0, idx) + '<mark>' + label.slice(idx, idx + query.length) + '</mark>' + label.slice(idx + query.length);
    }

    function setupSearch() {
        var wrap  = document.querySelector('.mnav-search-wrap');
        var input = document.querySelector('.mnav-search');
        if (!wrap || !input) return;

        wrap.style.position = 'relative';

        var drop = buildSearchDropdown();
        wrap.appendChild(drop);

        var activeIndex = -1;

        function getItems() {
            return Array.from(drop.querySelectorAll('.mnav-search-result-item'));
        }

        function updateSelection(items, index) {
            items.forEach(function (el) { el.classList.remove('selected'); });
            if (index >= 0 && index < items.length) {
                activeIndex = index;
                var current = items[activeIndex];
                current.classList.add('selected');
                current.scrollIntoView({ block: 'nearest' });
            } else {
                activeIndex = -1;
            }
        }

        input.addEventListener('input', function () {
            activeIndex = -1;
            renderSearchResults(drop, input.value.trim());
        });

        drop.addEventListener('mouseover', function (e) {
            var item = e.target.closest('.mnav-search-result-item');
            if (!item) return;
            var items = getItems();
            var idx = items.indexOf(item);
            if (idx === -1) return;
            items.forEach(function (el) { el.classList.remove('selected'); });
            item.classList.add('selected');
            activeIndex = idx;
        });

        wrap.addEventListener('keydown', function (e) {
            var items = getItems();
            var isOpen = drop.style.display === 'block';

            if (e.key === 'Escape') {
                if (isOpen) {
                    e.preventDefault();
                    drop.style.display = 'none';
                    activeIndex = -1;
                    input.blur();
                }
                return;
            }

            if (!isOpen || !items.length) {
                if (e.key === 'ArrowDown' && input.value.trim()) {
                    e.preventDefault();
                    renderSearchResults(drop, input.value.trim());
                    updateSelection(getItems(), 0);
                }
                return;
            }

            if (e.key === 'ArrowDown') {
                e.preventDefault();
                var nextIndex = activeIndex + 1 >= items.length ? 0 : activeIndex + 1;
                updateSelection(items, nextIndex);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                var prevIndex = activeIndex - 1 < 0 ? items.length - 1 : activeIndex - 1;
                updateSelection(items, prevIndex);
            } else if (e.key === 'Enter') {
                e.preventDefault();
                if (activeIndex >= 0 && items[activeIndex]) {
                    items[activeIndex].click();
                } else if (items.length > 0) {
                    items[0].click();
                }
            } else if (e.key === 'Tab') {
                drop.style.display = 'none';
                activeIndex = -1;
            }
        });

        document.addEventListener('click', function (e) {
            if (!wrap.contains(e.target)) {
                drop.style.display = 'none';
                activeIndex = -1;
            }
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === '/' && document.activeElement && document.activeElement.tagName !== 'INPUT' && document.activeElement.tagName !== 'TEXTAREA') {
                e.preventDefault();
                input.focus();
                input.select();
            }
        });
    }

    /* ══════════════════════════════════════════════════════
        Mobile Drawer Controller
        Clones desktop nav, flattens column-decks into
        a vertical accordion tree (.mm-* classes).
    ══════════════════════════════════════════════════════ */
    function flattenColumn(col, container, deck) {
        var children = col.querySelectorAll(':scope > .mnav-col-item, :scope > .mnav-divider');
        Array.from(children).forEach(function (child) {
            if (child.classList.contains('mnav-divider')) {
                var d = document.createElement('div');
                d.className = 'mm-divider';
                container.appendChild(d);
                return;
            }

            var expandId = child.getAttribute('data-expand');
            if (expandId) {
                var group = document.createElement('div');
                group.className = 'mm-group';

                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'mm-trigger';
                btn.setAttribute('aria-expanded', 'false');
                var content = child.querySelector('.mnav-col-item-content');
                if (content) {
                    btn.innerHTML = content.innerHTML;
                }
                btn.innerHTML += '<span class="material-symbols-outlined mm-chevron">expand_more</span>';
                group.appendChild(btn);

                var sub = document.createElement('div');
                sub.className = 'mm-sub';
                var targetCol = deck.querySelector('.mnav-col[data-col-id="' + expandId + '"]');
                if (targetCol) {
                    flattenColumn(targetCol, sub, deck);
                }
                group.appendChild(sub);
                container.appendChild(group);
            } else {
                var link = child.cloneNode(true);
                link.className = 'mm-item';
                link.removeAttribute('role');
                link.removeAttribute('id');
                link.querySelectorAll('[id]').forEach(function (el) { el.removeAttribute('id'); });
                link.querySelectorAll('[role]').forEach(function (el) { el.removeAttribute('role'); });
                container.appendChild(link);
            }
        });
    }

    function setupMobileDrawer() {
        var hamburger   = document.getElementById('mnav-hamburger');
        var drawerClose = document.getElementById('mnav-drawer-close');
        var drawer      = document.getElementById('mnav-drawer');
        var overlay     = document.getElementById('mnav-drawer-overlay');
        var navArea     = document.querySelector('.mnav-nav-area');
        if (!hamburger || !drawer || !overlay || !navArea) return;

        var desktopList = navArea.querySelector('.mnav-list');
        var drawerList  = drawer.querySelector('.mnav-list');
        if (!desktopList || !drawerList) return;

        // Clone desktop nav and flatten into accordion
        drawerList.innerHTML = '';
        Array.from(desktopList.children).forEach(function (li) {
            var deck = li.querySelector('.mnav-column-deck');
            var trigger = li.querySelector(':scope > .mnav-trigger');

            if (deck && trigger) {
                // Expandable group
                var group = document.createElement('div');
                group.className = 'mm-group';

                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'mm-trigger';
                btn.setAttribute('aria-expanded', 'false');
                // Copy trigger content (icon + label)
                var clone = trigger.cloneNode(true);
                // Remove expand_more / chevron icon from clone, we add our own
                clone.querySelectorAll('.material-symbols-outlined, i').forEach(function (icon) {
                    var text = (icon.textContent || '').trim();
                    if (text === 'expand_more' || text === 'chevron_right' || icon.classList.contains('mnav-chevron')) {
                        icon.remove();
                    }
                });
                btn.innerHTML = clone.innerHTML;
                btn.innerHTML += '<span class="material-symbols-outlined mm-chevron">expand_more</span>';
                group.appendChild(btn);

                var sub = document.createElement('div');
                sub.className = 'mm-sub';
                // Flatten all columns from the deck
                var cols = deck.querySelectorAll(':scope > .mnav-col:not([data-parent-col])');
                Array.from(cols).forEach(function (col) {
                    flattenColumn(col, sub, deck);
                });
                group.appendChild(sub);
                drawerList.appendChild(group);
            } else if (trigger) {
                // Direct link
                var link = document.createElement('a');
                link.className = 'mm-item';
                link.href = trigger.getAttribute('href') || '#';
                var cloneDirect = trigger.cloneNode(true);
                cloneDirect.removeAttribute('role');
                cloneDirect.removeAttribute('id');
                cloneDirect.querySelectorAll('[id]').forEach(function (el) { el.removeAttribute('id'); });
                cloneDirect.querySelectorAll('[role]').forEach(function (el) { el.removeAttribute('role'); });
                link.innerHTML = cloneDirect.innerHTML;
                drawerList.appendChild(link);
            }
        });

        // Accordion toggle via event delegation
        drawerList.addEventListener('click', function (e) {
            var btn = e.target.closest('.mm-trigger');
            if (!btn) return;
            e.preventDefault();

            var group = btn.closest('.mm-group');
            if (!group) return;

            var wasOpen = group.classList.contains('open');
            // Close siblings at same level
            var siblings = group.parentElement ? group.parentElement.children : [];
            Array.from(siblings).forEach(function (sib) {
                if (sib !== group && sib.classList.contains('mm-group')) {
                    sib.classList.remove('open');
                    var st = sib.querySelector(':scope > .mm-trigger');
                    if (st) st.setAttribute('aria-expanded', 'false');
                }
            });

            group.classList.toggle('open', !wasOpen);
            btn.setAttribute('aria-expanded', String(!wasOpen));
        });

        function open() {
            document.body.classList.add('mnav-drawer-open');
            overlay.classList.add('active');
            hamburger.setAttribute('aria-expanded', 'true');
        }

        function close() {
            document.body.classList.remove('mnav-drawer-open');
            overlay.classList.remove('active');
            hamburger.setAttribute('aria-expanded', 'false');
            // Collapse all open groups
            drawer.querySelectorAll('.mm-group.open').forEach(function (g) {
                g.classList.remove('open');
                var t = g.querySelector(':scope > .mm-trigger');
                if (t) t.setAttribute('aria-expanded', 'false');
            });
        }

        hamburger.addEventListener('click', function () {
            if (document.body.classList.contains('mnav-drawer-open')) { close(); } else { open(); }
        });
        if (drawerClose) {
            drawerClose.addEventListener('click', close);
        }
        overlay.addEventListener('click', close);
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && document.body.classList.contains('mnav-drawer-open')) close();
        });
        drawer.addEventListener('click', function (e) {
            if (e.target.closest('a[href], button[type="submit"]')) close();
        });
    }

    /* ══════════════════════════════════════════════════════
        User Dropdown
    ══════════════════════════════════════════════════════ */
    function setupUserDropdown() {
        var trigger = document.getElementById('mnav-user-trigger');
        var menu    = document.getElementById('mnav-dropdown-menu');
        if (!trigger || !menu) return;

        function close() {
            menu.classList.remove('open');
            trigger.setAttribute('aria-expanded', 'false');
        }

        trigger.addEventListener('click', function (e) {
            e.stopPropagation();
            var isOpen = menu.classList.contains('open');
            if (isOpen) { close(); } else { menu.classList.add('open'); trigger.setAttribute('aria-expanded', 'true'); }
        });

        document.addEventListener('click', function (e) {
            if (!trigger.contains(e.target)) close();
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') close();
        });
    }

    /* ══════════════════════════════════════════════════════
        Init
    ══════════════════════════════════════════════════════ */
    document.addEventListener('DOMContentLoaded', function () {
        suppressQuickAccess();
        setupColumnDecks();
        setupSearch();
        setupMobileDrawer();
        setupUserDropdown();
    });
})();
