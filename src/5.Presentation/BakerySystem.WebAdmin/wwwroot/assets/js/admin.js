/**
 * BakeryAdmin WebAdmin UI Helpers
 * Controls theme switching (Dark/Light mode), Mobile Sidebar, and Dropdowns
 */

(function () {
    // 1. Initialize Theme from LocalStorage or System Preference
    function initTheme() {
        const savedTheme = localStorage.getItem('bakery-admin-theme');
        if (savedTheme === 'dark' || (!savedTheme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
            document.documentElement.classList.add('dark');
        } else {
            document.documentElement.classList.remove('dark');
        }
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
                    let target = returnUrl && returnUrl !== 'login' ? ('/' + returnUrl.replace(/^\//, '')) : '/';
                    if (!returnUrl || returnUrl === '/' || returnUrl === 'login') {
                        if (data.role === 'Employee') {
                            target = '/pos';
                        } else {
                            target = '/';
                        }
                    }
                    window.location.href = target;
                    return { success: true };
                } else {
                    return { success: false, errorMessage: data.errorMessage || 'Đăng nhập không thành công.' };
                }
            } catch (err) {
                return { success: false, errorMessage: 'Không thể kết nối đến máy chủ: ' + (err.message || 'Lỗi mạng') };
            }
        },
        logout: async function () {
            try {
                await fetch('/api/auth/logout', { method: 'POST' });
            } finally {
                window.location.href = '/login';
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
