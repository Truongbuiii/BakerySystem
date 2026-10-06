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
