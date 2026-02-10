/**
 * ThinkBridge ERP - Global JavaScript
 * ==========================================
 */

(function () {
    'use strict';

    // ===========================================
    // DOM Ready Handler
    // ===========================================
    document.addEventListener('DOMContentLoaded', function () {
        initSidebar();
        initActiveNavHighlight();
        initDropdowns();
        initModals();
        initTabs();
        initSettingsNav();
    });

    // ===========================================
    // Sidebar Toggle
    // ===========================================
    function initSidebar() {
        const sidebar = document.querySelector('.sidebar');
        const sidebarToggle = document.querySelector('.sidebar-toggle');
        const sidebarOverlay = document.querySelector('.sidebar-overlay');
        const sidebarCollapseBtn = document.querySelector('.sidebar-collapse-btn');

        // Mobile toggle
        if (sidebarToggle) {
            sidebarToggle.addEventListener('click', function () {
                sidebar.classList.toggle('open');
                sidebarOverlay.classList.toggle('active');
            });
        }

        // Overlay click to close
        if (sidebarOverlay) {
            sidebarOverlay.addEventListener('click', function () {
                sidebar.classList.remove('open');
                sidebarOverlay.classList.remove('active');
            });
        }

        // Desktop collapse toggle
        if (sidebarCollapseBtn) {
            sidebarCollapseBtn.addEventListener('click', function () {
                document.body.classList.toggle('sidebar-collapsed');

                // Save preference
                const isCollapsed = document.body.classList.contains('sidebar-collapsed');
                localStorage.setItem('sidebarCollapsed', isCollapsed);
            });

            // Restore preference
            const savedState = localStorage.getItem('sidebarCollapsed');
            if (savedState === 'true') {
                document.body.classList.add('sidebar-collapsed');
            }
        }
    }

    // ===========================================
    // Active Navigation Highlight
    // ===========================================
    function initActiveNavHighlight() {
        const currentPath = window.location.pathname.toLowerCase();
        const navLinks = document.querySelectorAll('.nav-link');

        navLinks.forEach(function (link) {
            const href = link.getAttribute('href');
            if (href) {
                // Extract just the pathname from the href (ignore query params)
                let hrefPath;
                try {
                    // Handle both absolute and relative URLs
                    const url = new URL(href, window.location.origin);
                    hrefPath = url.pathname.toLowerCase();
                } catch (e) {
                    // Fallback: split on '?' to remove query params
                    hrefPath = href.split('?')[0].toLowerCase();
                }

                // Check if current path matches the nav link path
                if (currentPath === hrefPath ||
                    (hrefPath !== '/' && currentPath.startsWith(hrefPath))) {
                    link.classList.add('active');
                } else {
                    link.classList.remove('active');
                }
            }
        });
    }

    // ===========================================
    // Dropdowns (Notifications & User)
    // ===========================================
    function initDropdowns() {
        // Notification dropdown
        const notificationBtn = document.querySelector('.notification-btn');
        const notificationDropdown = document.querySelector('.notification-dropdown');

        if (notificationBtn && notificationDropdown) {
            notificationBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                notificationDropdown.classList.toggle('active');

                // Close user dropdown if open
                const userDropdown = document.querySelector('.user-dropdown');
                if (userDropdown) {
                    userDropdown.classList.remove('active');
                }
            });
        }

        // User dropdown
        const userDropdownBtn = document.querySelector('.user-dropdown-btn');
        const userDropdown = document.querySelector('.user-dropdown');

        if (userDropdownBtn && userDropdown) {
            userDropdownBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                userDropdown.classList.toggle('active');
                userDropdownBtn.classList.toggle('active');

                // Close notification dropdown if open
                if (notificationDropdown) {
                    notificationDropdown.classList.remove('active');
                }
            });
        }

        // Close dropdowns when clicking outside
        document.addEventListener('click', function () {
            if (notificationDropdown) {
                notificationDropdown.classList.remove('active');
            }
            if (userDropdown) {
                userDropdown.classList.remove('active');
                if (userDropdownBtn) {
                    userDropdownBtn.classList.remove('active');
                }
            }
        });

        // Prevent dropdown from closing when clicking inside
        const dropdowns = document.querySelectorAll('.notification-dropdown, .user-dropdown');
        dropdowns.forEach(function (dropdown) {
            dropdown.addEventListener('click', function (e) {
                e.stopPropagation();
            });
        });
    }

    // ===========================================
    // Modal Management
    // ===========================================
    function initModals() {
        // Open modal buttons
        const modalTriggers = document.querySelectorAll('[data-modal-open]');
        modalTriggers.forEach(function (trigger) {
            trigger.addEventListener('click', function () {
                const modalId = this.getAttribute('data-modal-open');
                openModal(modalId);
            });
        });

        // Close modal buttons
        const modalCloseButtons = document.querySelectorAll('[data-modal-close]');
        modalCloseButtons.forEach(function (btn) {
            btn.addEventListener('click', function () {
                const modal = this.closest('.modal-overlay');
                if (modal) {
                    closeModal(modal.id);
                }
            });
        });

        // Close modal when clicking overlay
        const modalOverlays = document.querySelectorAll('.modal-overlay');
        modalOverlays.forEach(function (overlay) {
            overlay.addEventListener('click', function (e) {
                if (e.target === overlay) {
                    closeModal(overlay.id);
                }
            });
        });

        // Close modal on escape key
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                const activeModal = document.querySelector('.modal-overlay.active');
                if (activeModal) {
                    closeModal(activeModal.id);
                }
            }
        });
    }

    function openModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.add('active');
            document.body.style.overflow = 'hidden';
        }
    }

    function closeModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.classList.remove('active');
            document.body.style.overflow = '';
        }
    }

    // Expose modal functions globally
    window.openModal = openModal;
    window.closeModal = closeModal;

    // ===========================================
    // Tabs
    // ===========================================
    function initTabs() {
        const tabContainers = document.querySelectorAll('.tabs-container');

        tabContainers.forEach(function (container) {
            const tabs = container.querySelectorAll('.tab-btn');

            tabs.forEach(function (tab) {
                tab.addEventListener('click', function () {
                    // Remove active from all tabs
                    tabs.forEach(function (t) {
                        t.classList.remove('active');
                    });

                    // Add active to clicked tab
                    this.classList.add('active');

                    // Handle tab content if data-tab attribute exists
                    const tabId = this.getAttribute('data-tab');
                    if (tabId) {
                        const tabContents = document.querySelectorAll('.tab-content');
                        tabContents.forEach(function (content) {
                            content.classList.remove('active');
                        });

                        const activeContent = document.getElementById(tabId);
                        if (activeContent) {
                            activeContent.classList.add('active');
                        }
                    }
                });
            });
        });
    }

    // ===========================================
    // Settings Navigation
    // ===========================================
    function initSettingsNav() {
        const settingsNavItems = document.querySelectorAll('.settings-nav-item');
        const settingsSections = document.querySelectorAll('.settings-section');

        if (settingsNavItems.length === 0) return;

        settingsNavItems.forEach(function (item) {
            item.addEventListener('click', function (e) {
                e.preventDefault();

                // Remove active from all nav items
                settingsNavItems.forEach(function (nav) {
                    nav.classList.remove('active');
                });

                // Add active to clicked item
                this.classList.add('active');

                // Show corresponding section
                const sectionId = this.getAttribute('data-section');
                if (sectionId) {
                    settingsSections.forEach(function (section) {
                        section.style.display = 'none';
                    });

                    const activeSection = document.getElementById(sectionId);
                    if (activeSection) {
                        activeSection.style.display = 'block';
                    }
                }
            });
        });
    }

    // ===========================================
    // Form Validation Helpers
    // ===========================================
    window.validateForm = function (formId) {
        const form = document.getElementById(formId);
        if (!form) return false;

        let isValid = true;
        const requiredFields = form.querySelectorAll('[required]');

        requiredFields.forEach(function (field) {
            if (!field.value.trim()) {
                isValid = false;
                field.classList.add('error');
            } else {
                field.classList.remove('error');
            }
        });

        return isValid;
    };

    // ===========================================
    // Toast Notifications
    // ===========================================
    window.showToast = function (message, type) {
        type = type || 'info';

        // Create toast container if it doesn't exist
        let toastContainer = document.querySelector('.toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.className = 'toast-container';
            toastContainer.style.cssText = 'position: fixed; top: 20px; right: 20px; z-index: 9999;';
            document.body.appendChild(toastContainer);
        }

        // Create toast
        const toast = document.createElement('div');
        toast.className = 'toast toast-' + type;
        toast.style.cssText = 'background: var(--surface); padding: 16px 24px; border-radius: 8px; box-shadow: var(--shadow-lg); margin-bottom: 10px; animation: slideIn 0.3s ease;';
        toast.textContent = message;

        toastContainer.appendChild(toast);

        // Remove after 3 seconds
        setTimeout(function () {
            toast.style.animation = 'slideOut 0.3s ease';
            setTimeout(function () {
                toast.remove();
            }, 300);
        }, 3000);
    };

    // ===========================================
    // Password Toggle
    // ===========================================
    window.togglePassword = function (inputId) {
        const input = document.getElementById(inputId);
        if (input) {
            input.type = input.type === 'password' ? 'text' : 'password';
        }
    };

})();
