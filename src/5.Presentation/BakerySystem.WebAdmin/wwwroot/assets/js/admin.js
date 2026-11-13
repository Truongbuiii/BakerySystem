/**
 * BakeryAdmin WebAdmin UI Helpers
 * Controls theme switching (Dark/Light mode), Mobile Sidebar, and Dropdowns
 */


(function () {
    // 1. Initialize Theme (Enforce Light Theme)
    function initTheme() {
        document.documentElement.classList.remove('dark');
        document.documentElement.classList.add('light');
        try {
            localStorage.removeItem('bakery-admin-theme');
        } catch (e) { }
    }

    // Run theme init immediately to prevent flash of wrong theme
    initTheme();

    function initSidebarState() {
        if (localStorage.getItem('bakery-admin-sidebar-collapsed') === 'true') {
            document.querySelector('.bakery-layout-root')?.classList.add('sidebar-collapsed');
        }
    }

    initSidebarState();

    function setupSidebarTooltips() {
        const sidebar = document.getElementById('admin-sidebar');
        if (!sidebar || sidebar.dataset.tooltipsReady === 'true') return;

        const tooltip = document.createElement('div');
        tooltip.className = 'bakery-sidebar-tooltip';
        tooltip.setAttribute('role', 'tooltip');
        document.body.appendChild(tooltip);

        let activeLink = null;
        const hideTooltip = function () {
            activeLink = null;
            tooltip.classList.remove('is-visible');
        };

        sidebar.addEventListener('mouseover', function (event) {
            const link = event.target.closest('.bakery-nav-link');
            if (!link || !sidebar.contains(link)) return;

            const label = link.getAttribute('aria-label') || link.getAttribute('title');
            if (!label) return;

            activeLink = link;
            tooltip.textContent = label;
            const rect = link.getBoundingClientRect();
            tooltip.style.left = `${rect.right + 10}px`;
            tooltip.style.top = `${rect.top + rect.height / 2}px`;
            tooltip.classList.add('is-visible');
        });

        sidebar.addEventListener('mouseout', function (event) {
            const link = event.target.closest('.bakery-nav-link');
            if (link && (!event.relatedTarget || !link.contains(event.relatedTarget))) {
                hideTooltip();
            }
        });

        window.addEventListener('resize', hideTooltip);
        window.addEventListener('scroll', hideTooltip, true);
        sidebar.dataset.tooltipsReady = 'true';
    }

    setupSidebarTooltips();
    if (!document.getElementById('admin-sidebar')) {
        const sidebarObserver = new MutationObserver(function () {
            if (document.getElementById('admin-sidebar')) {
                setupSidebarTooltips();
                sidebarObserver.disconnect();
            }
        });
        sidebarObserver.observe(document.body, { childList: true, subtree: true });
    }

    window.bakeryAdmin = {
        toggleTheme: function () {
            const isDark = document.documentElement.classList.toggle('dark');
            localStorage.setItem('bakery-admin-theme', isDark ? 'dark' : 'light');
            // Re-render chart tooltips if needed
            if (window.initBakeryDashboardCharts) {
                setTimeout(window.initBakeryDashboardCharts, 100);
            }
            return isDark;
        },
        getTheme: function () {
            return document.documentElement.classList.contains('dark') ? 'dark' : 'light';
        },
        playNewOrderSound: function () {
            try {
                const AudioCtx = window.AudioContext || window.webkitAudioContext;
                if (!AudioCtx) return;
                const ctx = new AudioCtx();
                if (ctx.state === 'suspended') {
                    ctx.resume();
                }
                const now = ctx.currentTime;
                
                const osc1 = ctx.createOscillator();
                const gain1 = ctx.createGain();
                osc1.type = 'sine';
                osc1.frequency.setValueAtTime(659.25, now);
                gain1.gain.setValueAtTime(0.2, now);
                gain1.gain.exponentialRampToValueAtTime(0.0001, now + 0.35);
                osc1.connect(gain1);
                gain1.connect(ctx.destination);
                osc1.start(now);
                osc1.stop(now + 0.35);

                const osc2 = ctx.createOscillator();
                const gain2 = ctx.createGain();
                osc2.type = 'sine';
                osc2.frequency.setValueAtTime(880, now + 0.14);
                gain2.gain.setValueAtTime(0.25, now + 0.14);
                gain2.gain.exponentialRampToValueAtTime(0.0001, now + 0.65);
                osc2.connect(gain2);
                gain2.connect(ctx.destination);
                osc2.start(now + 0.14);
                osc2.stop(now + 0.65);
            } catch (e) {
                // Audio autoplay might be blocked before first user interaction
            }
        },
        toggleSidebar: function () {
            const sidebar = document.getElementById('admin-sidebar');
            const backdrop = document.getElementById('sidebar-backdrop');
            if (sidebar) {
                sidebar.classList.toggle('-translate-x-full');
                sidebar.classList.toggle('sidebar-open');
            }
            if (backdrop) {
                backdrop.classList.toggle('hidden');
            }
        },
        toggleSidebarCollapse: function () {
            const layout = document.querySelector('.bakery-layout-root');
            if (!layout) return;

            const isCollapsed = layout.classList.toggle('sidebar-collapsed');
            localStorage.setItem('bakery-admin-sidebar-collapsed', isCollapsed ? 'true' : 'false');

            document.querySelectorAll('.bakery-sidebar-collapse-toggle').forEach(button => {
                button.title = isCollapsed ? 'Mở rộng menu' : 'Thu gọn menu';
                button.setAttribute('aria-label', button.title);
            });
            document.querySelector('.bakery-sidebar-tooltip')?.classList.remove('is-visible');
        },
        closeSidebar: function () {
            const sidebar = document.getElementById('admin-sidebar');
            const backdrop = document.getElementById('sidebar-backdrop');
            if (sidebar) {
                sidebar.classList.add('-translate-x-full');
                sidebar.classList.remove('sidebar-open');
            }
            if (backdrop) {
                backdrop.classList.add('hidden');
            }
        },
        toggleDropdown: function (dropdownId) {
            const dropdown = document.getElementById(dropdownId);
            if (!dropdown) return;

            const isHidden = dropdown.classList.contains('hidden');
            
            // Close all dropdowns first
            document.querySelectorAll('[data-admin-dropdown]').forEach(el => {
                if (el.id !== dropdownId) el.classList.add('hidden');
            });

            if (isHidden) {
                dropdown.classList.remove('hidden');
            } else {
                dropdown.classList.add('hidden');
            }
        }
    };

    window.bakeryAuth = {
        resolveTargetUrl: function (role, returnUrl) {
            const cleanReturn = returnUrl ? ('/' + returnUrl.replace(/^\/+/, '')) : '';
            if (role === 'Customer') {
                if (cleanReturn && (cleanReturn.startsWith('/shop') || cleanReturn.startsWith('/my-orders') || cleanReturn.startsWith('/profile'))) {
                    return cleanReturn;
                }
                return '/shop';
            }
            if (role === 'Employee') {
                if (cleanReturn && cleanReturn !== '/' && !cleanReturn.startsWith('/login')) {
                    return cleanReturn;
                }
                return '/pos';
            }
            if (cleanReturn && !cleanReturn.startsWith('/login')) {
                return cleanReturn;
            }
            return '/';
        },
        login: async function (username, password, returnUrl) {
            try {
                const resp = await fetch('/api/auth/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username: username, password: password })
                });
                if (!resp.ok) {
                    let errText = 'Lỗi máy chủ (' + resp.status + ')';
                    try {
                        const errJson = await resp.json();
                        if (errJson.errorMessage) errText = errJson.errorMessage;
                        else if (errJson.title) errText = errJson.title;
                    } catch (_) {}
                    return { success: false, errorMessage: errText };
                }
                const data = await resp.json();
                if (data.success) {
                    window.location.href = window.bakeryAuth.resolveTargetUrl(data.role, returnUrl);
                    return { success: true };
                } else {
                    return { success: false, errorMessage: data.errorMessage || 'Đăng nhập không thành công.' };
                }
            } catch (err) {
                return { success: false, errorMessage: 'Không thể kết nối đến máy chủ: ' + (err.message || 'Lỗi mạng') };
            }
        },
        register: async function (payload) {
            try {
                const resp = await fetch('/api/auth/register', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                });
                if (!resp.ok) {
                    return { success: false, errorMessage: 'Lỗi máy chủ (' + resp.status + ')' };
                }
                const data = await resp.json();
                if (data.success) {
                    window.location.href = '/shop';
                    return { success: true };
                }
                return { success: false, errorMessage: data.errorMessage || 'Đăng ký không thành công.' };
            } catch (err) {
                return { success: false, errorMessage: 'Không thể kết nối đến máy chủ: ' + (err.message || 'Lỗi mạng') };
            }
        },
        logout: async function () {
            if (window.bakeryAuth && window.bakeryAuth.stopIdleTimer) {
                window.bakeryAuth.stopIdleTimer();
            }
            try {
                await fetch('/api/auth/logout', { method: 'POST' });
            } finally {
                window.location.href = '/login';
            }
        },
        startIdleTimer: function (timeoutMs) {
            const limit = Number(timeoutMs) > 0 ? Number(timeoutMs) : (30 * 60 * 1000);
            if (window.__bakeryIdleState && window.__bakeryIdleState.active) {
                return;
            }

            let timerId = null;
            let lastReset = 0;

            const triggerTimeout = async function () {
                window.bakeryAuth.stopIdleTimer();
                try {
                    await fetch('/api/auth/logout', { method: 'POST' });
                } catch (_) { }
                window.location.href = '/login?reason=timeout';
            };

            const resetTimer = function (force) {
                const now = Date.now();
                if (!force && (now - lastReset) < 3000) {
                    return;
                }
                lastReset = now;
                if (timerId) {
                    clearTimeout(timerId);
                }
                timerId = setTimeout(triggerTimeout, limit);
            };

            const events = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll'];
            const onActivity = function () { resetTimer(false); };
            events.forEach(ev => document.addEventListener(ev, onActivity, { passive: true }));

            window.__bakeryIdleState = {
                active: true,
                cleanup: function () {
                    if (timerId) clearTimeout(timerId);
                    events.forEach(ev => document.removeEventListener(ev, onActivity));
                }
            };
            resetTimer(true);
        },
        stopIdleTimer: function () {
            if (window.__bakeryIdleState) {
                try { window.__bakeryIdleState.cleanup(); } catch (_) { }
                window.__bakeryIdleState = null;
            }
        }
    };

    // Close dropdowns when clicking outside
    document.addEventListener('click', function (e) {
        if (!e.target.closest('[data-dropdown-trigger]') && !e.target.closest('[data-admin-dropdown]')) {
            document.querySelectorAll('[data-admin-dropdown]').forEach(el => el.classList.add('hidden'));
        }
    });

    // Auto-close mobile sidebar when window is resized to desktop width (>= 1024px)
    window.addEventListener('resize', function () {
        if (window.innerWidth >= 1024) {
            const sidebar = document.getElementById('admin-sidebar');
            const backdrop = document.getElementById('sidebar-backdrop');
            if (sidebar) {
                sidebar.classList.remove('sidebar-open');
            }
            if (backdrop) {
                backdrop.classList.add('hidden');
            }
        }
    });
})();


// Excel & PDF Helpers for BakeryAdmin
window.bakeryExcel = {
    exportToExcel: function (tableId, fileName) {
        const table = document.getElementById(tableId);
        if (!table) {
            console.error('Table not found: ' + tableId);
            return;
        }

        // Tạo nội dung HTML tương thích Excel XML
        let html = '<html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns="http://www.w3.org/TR/REC-html40">';
        html += '<head><meta charset="utf-8"><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet>';
        html += '<x:Name>DuLieu</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--></head>';
        html += '<body>';
        
        // Hỗ trợ cả một bảng đơn và một vùng chứa nhiều bảng báo cáo.
        const clones = table.tagName === 'TABLE'
            ? [table.cloneNode(true)]
            : Array.from(table.querySelectorAll('table')).map(item => item.cloneNode(true));

        clones.forEach(clone => {
            const headers = clone.querySelectorAll('thead th');
            const actionColumns = [];
            headers.forEach((th, idx) => {
                if (th.innerText.includes('Thao Tác') || th.innerText.includes('Thao tác')) {
                    actionColumns.push(idx);
                    th.remove();
                }
            });

            if (actionColumns.length > 0) {
                clone.querySelectorAll('tbody tr').forEach(row => {
                    const cells = Array.from(row.querySelectorAll('td'));
                    actionColumns.reverse().forEach(index => {
                        if (cells[index]) cells[index].remove();
                    });
                });
            }

            html += clone.outerHTML;
        });
        html += '</body></html>';

        const blob = new Blob([html], { type: 'application/vnd.ms-excel;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = (fileName || 'DuLieu') + '.xls';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    downloadTemplate: function (fileName, csvContent) {
        const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = (fileName || 'BieuMau') + '.csv';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
};

window.bakeryPdf = {
    printReportToPdf: function (title, printClass) {
        const originalTitle = document.title;
        const body = document.body;
        if (title) {
            document.title = title;
        }
        if (printClass) {
            body.classList.add(printClass);
        }

        const cleanup = () => {
            if (printClass) {
                body.classList.remove(printClass);
            }
            document.title = originalTitle;
            window.removeEventListener('afterprint', cleanup);
        };

        window.addEventListener('afterprint', cleanup, { once: true });
        window.print();
        setTimeout(cleanup, 5000);
    }
};
