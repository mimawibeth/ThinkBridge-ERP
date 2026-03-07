// =============================================
// ThinkBridge ERP - Settings Page
// =============================================

(function () {
    'use strict';

    let profileData = null;      // cached profile from API
    let originalProfile = null;  // snapshot for cancel/dirty check

    // ------------------------------------------
    // Init
    // ------------------------------------------
    document.addEventListener('DOMContentLoaded', () => {
        initNavigation();
        loadProfile();
        bindProfileActions();
        bindPasswordActions();
        initThemeSelector();
        initAvatarColorPicker();
    });

    // ------------------------------------------
    // Sidebar navigation
    // ------------------------------------------
    function initNavigation() {
        document.querySelectorAll('.settings-nav-item').forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const sectionId = item.getAttribute('data-section');

                document.querySelectorAll('.settings-nav-item').forEach(n => n.classList.remove('active'));
                item.classList.add('active');

                document.querySelectorAll('.settings-section').forEach(s => s.classList.remove('active'));
                document.getElementById(sectionId)?.classList.add('active');
            });
        });
    }

    // ------------------------------------------
    // Load profile from API
    // ------------------------------------------
    async function loadProfile() {
        try {
            const res = await fetch('/api/settings/profile');
            if (!res.ok) throw new Error('Failed to load profile');
            profileData = await res.json();
            populateProfile(profileData);
            populateAccount(profileData);
        } catch (err) {
            console.error('Error loading profile:', err);
            showToast('Failed to load profile data.', 'error');
        }
    }

    // ------------------------------------------
    // Populate profile section
    // ------------------------------------------
    function populateProfile(data) {
        // Avatar initials
        const initials = ((data.fname?.[0] || '') + (data.lname?.[0] || '')).toUpperCase() || '?';
        document.getElementById('avatar-initials').textContent = initials;

        // Assign avatar color from user data
        const avatarEl = document.getElementById('settings-avatar');
        const color = data.avatarColor || '#0B4F6C';
        if (avatarEl) {
            avatarEl.style.background = color;
        }

        // Sync color picker selection
        document.querySelectorAll('.avatar-color-swatch').forEach(s => {
            s.classList.toggle('selected', s.dataset.color === color);
        });

        // Header info
        document.getElementById('profile-display-name').textContent = `${data.fname} ${data.lname}`;
        document.getElementById('profile-display-role').textContent = formatRole(data.role);

        // Form fields
        document.getElementById('first-name').value = data.fname || '';
        document.getElementById('last-name').value = data.lname || '';
        document.getElementById('email').value = data.email || '';
        document.getElementById('phone').value = data.phone || '';

        // Snapshot for cancel
        originalProfile = {
            fname: data.fname || '',
            lname: data.lname || '',
            phone: data.phone || ''
        };
    }

    // ------------------------------------------
    // Populate account section
    // ------------------------------------------
    function populateAccount(data) {
        document.getElementById('account-role').textContent = formatRole(data.role);
        document.getElementById('account-role-desc').textContent = getRoleDescription(data.role);
        document.getElementById('account-email').textContent = data.email || '—';
        document.getElementById('account-company').textContent = data.companyName || 'N/A (Platform Admin)';

        // Status badge
        const statusEl = document.getElementById('account-status');
        statusEl.textContent = data.status || '—';
        statusEl.className = 'detail-value status-badge status-' + (data.status || '').toLowerCase();

        // Dates
        document.getElementById('account-created').textContent = data.createdAt
            ? formatDate(data.createdAt) : '—';
        document.getElementById('account-last-login').textContent = data.lastLoginAt
            ? formatDateTime(data.lastLoginAt) : 'Never';
    }

    // ------------------------------------------
    // Profile save / cancel
    // ------------------------------------------
    function bindProfileActions() {
        document.getElementById('btn-save-profile')?.addEventListener('click', saveProfile);
        document.getElementById('btn-cancel-profile')?.addEventListener('click', cancelProfile);
    }

    async function saveProfile() {
        const fname = document.getElementById('first-name').value.trim();
        const lname = document.getElementById('last-name').value.trim();
        const phone = document.getElementById('phone').value.trim();
        const avatarColor = profileData?.avatarColor || '#0B4F6C';

        if (!fname || !lname) {
            showToast('First name and last name are required.', 'error');
            return;
        }

        const btn = document.getElementById('btn-save-profile');
        setButtonLoading(btn, true);

        try {
            const res = await fetch('/api/settings/profile', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ fname, lname, phone: phone || null, avatarColor })
            });

            const result = await res.json();
            if (!res.ok) throw new Error(result.message || 'Update failed');

            // Update cached data
            profileData.fname = result.fname;
            profileData.lname = result.lname;
            profileData.phone = result.phone;
            profileData.avatarColor = result.avatarColor;

            // Refresh display
            populateProfile(profileData);

            // Also refresh topbar name and color
            updateTopbar(result.fname, result.lname, result.avatarColor);

            showToast('Profile updated successfully.', 'success');
        } catch (err) {
            showToast(err.message, 'error');
        } finally {
            setButtonLoading(btn, false);
        }
    }

    function cancelProfile() {
        if (originalProfile) {
            document.getElementById('first-name').value = originalProfile.fname;
            document.getElementById('last-name').value = originalProfile.lname;
            document.getElementById('phone').value = originalProfile.phone;
        }
    }

    // ------------------------------------------
    // Password change
    // ------------------------------------------
    function bindPasswordActions() {
        document.getElementById('btn-change-password')?.addEventListener('click', changePassword);
        document.getElementById('btn-cancel-password')?.addEventListener('click', cancelPassword);

        // Real-time strength
        document.getElementById('new-password')?.addEventListener('input', (e) => {
            updatePasswordStrength(e.target.value);
        });

        // Match check
        document.getElementById('confirm-password')?.addEventListener('input', checkPasswordMatch);
        document.getElementById('new-password')?.addEventListener('input', checkPasswordMatch);
    }

    async function changePassword() {
        const currentPassword = document.getElementById('current-password').value;
        const newPassword = document.getElementById('new-password').value;
        const confirmPassword = document.getElementById('confirm-password').value;

        if (!currentPassword) {
            showToast('Current password is required.', 'error');
            return;
        }
        if (!newPassword || newPassword.length < 8) {
            showToast('New password must be at least 8 characters.', 'error');
            return;
        }
        if (newPassword !== confirmPassword) {
            showToast('Passwords do not match.', 'error');
            return;
        }

        const btn = document.getElementById('btn-change-password');
        setButtonLoading(btn, true);

        try {
            const res = await fetch('/api/settings/change-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ currentPassword, newPassword, confirmPassword })
            });

            const result = await res.json();
            if (!res.ok) throw new Error(result.message || 'Password change failed');

            // Clear form
            clearPasswordForm();
            showToast('Password changed successfully.', 'success');
        } catch (err) {
            showToast(err.message, 'error');
        } finally {
            setButtonLoading(btn, false);
        }
    }

    function cancelPassword() {
        clearPasswordForm();
    }

    function clearPasswordForm() {
        document.getElementById('current-password').value = '';
        document.getElementById('new-password').value = '';
        document.getElementById('confirm-password').value = '';
        resetPasswordStrength();
        const hint = document.getElementById('password-match-hint');
        if (hint) { hint.style.display = 'none'; hint.textContent = ''; }
    }

    // ------------------------------------------
    // Password strength meter
    // ------------------------------------------
    function updatePasswordStrength(password) {
        const bars = document.querySelectorAll('#password-strength .strength-bar');
        const text = document.getElementById('strength-text');

        if (!password) {
            resetPasswordStrength();
            return;
        }

        let score = 0;
        if (password.length >= 8) score++;
        if (/[A-Z]/.test(password)) score++;
        if (/[0-9]/.test(password)) score++;
        if (/[^A-Za-z0-9]/.test(password)) score++;

        const levels = [
            { label: 'Weak', color: '#ef4444' },
            { label: 'Fair', color: '#f59e0b' },
            { label: 'Good', color: '#3b82f6' },
            { label: 'Strong', color: '#10b981' }
        ];

        bars.forEach((bar, i) => {
            if (i < score) {
                bar.style.background = levels[score - 1].color;
            } else {
                bar.style.background = 'var(--border)';
            }
        });

        text.textContent = score > 0 ? levels[score - 1].label : '';
        text.style.color = score > 0 ? levels[score - 1].color : '';
    }

    function resetPasswordStrength() {
        document.querySelectorAll('#password-strength .strength-bar').forEach(b => {
            b.style.background = 'var(--border)';
        });
        const text = document.getElementById('strength-text');
        if (text) text.textContent = '';
    }

    function checkPasswordMatch() {
        const pw = document.getElementById('new-password').value;
        const cpw = document.getElementById('confirm-password').value;
        const hint = document.getElementById('password-match-hint');

        if (!cpw) {
            hint.style.display = 'none';
            return;
        }

        hint.style.display = 'block';
        if (pw === cpw) {
            hint.textContent = 'Passwords match';
            hint.style.color = '#10b981';
        } else {
            hint.textContent = 'Passwords do not match';
            hint.style.color = '#ef4444';
        }
    }

    // ------------------------------------------
    // Password visibility toggle
    // ------------------------------------------
    window.togglePasswordVisibility = function (inputId, btn) {
        const input = document.getElementById(inputId);
        if (!input) return;
        const isPassword = input.type === 'password';
        input.type = isPassword ? 'text' : 'password';
        btn.classList.toggle('active', isPassword);
    };

    // ------------------------------------------
    // Update topbar after profile save
    // ------------------------------------------
    function updateTopbar(fname, lname, avatarColor) {
        const fullName = `${fname} ${lname}`;
        // Try known topbar selectors
        const nameEl = document.querySelector('.user-name') || document.querySelector('.topbar-user-name');
        if (nameEl) nameEl.textContent = fullName;

        const avatarSpans = document.querySelectorAll('.user-avatar span, .topbar-avatar span');
        const initials = ((fname?.[0] || '') + (lname?.[0] || '')).toUpperCase();
        avatarSpans.forEach(s => s.textContent = initials);

        // Update topbar avatar color
        if (avatarColor) {
            const topbarAvatar = document.getElementById('topbar-avatar');
            if (topbarAvatar) topbarAvatar.style.background = avatarColor;
        }
    }

    // ------------------------------------------
    // Avatar Color Picker
    // ------------------------------------------
    function initAvatarColorPicker() {
        const container = document.getElementById('avatar-color-options');
        if (!container) return;

        container.addEventListener('click', (e) => {
            const swatch = e.target.closest('.avatar-color-swatch');
            if (!swatch) return;

            const color = swatch.dataset.color;
            if (!color) return;

            // Update selection UI
            container.querySelectorAll('.avatar-color-swatch').forEach(s => s.classList.remove('selected'));
            swatch.classList.add('selected');

            // Update cached profile data
            if (profileData) profileData.avatarColor = color;

            // Live preview on settings avatar
            const avatarEl = document.getElementById('settings-avatar');
            if (avatarEl) avatarEl.style.background = color;

            // Live preview on topbar avatar
            const topbarAvatar = document.getElementById('topbar-avatar');
            if (topbarAvatar) topbarAvatar.style.background = color;

            // Auto-save the color
            saveAvatarColor(color);
        });
    }

    async function saveAvatarColor(color) {
        if (!profileData) return;
        try {
            const res = await fetch('/api/settings/profile', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    fname: profileData.fname,
                    lname: profileData.lname,
                    phone: profileData.phone || null,
                    avatarColor: color
                })
            });
            const result = await res.json();
            if (!res.ok) throw new Error(result.message || 'Update failed');
            profileData.avatarColor = result.avatarColor;
            showToast('Avatar color updated.', 'success');
        } catch (err) {
            showToast('Failed to save avatar color.', 'error');
        }
    }

    // ------------------------------------------
    // Helpers
    // ------------------------------------------
    function formatRole(role) {
        if (!role) return '—';
        const map = {
            'SuperAdmin': 'Super Admin',
            'CompanyAdmin': 'Company Admin',
            'ProjectManager': 'Project Manager',
            'TeamMember': 'Team Member'
        };
        return map[role] || role;
    }

    function getRoleDescription(role) {
        const desc = {
            'SuperAdmin': 'Full platform access. Manage all companies, subscriptions, and system settings.',
            'CompanyAdmin': 'Full company access. Manage users, projects, billing, and company settings.',
            'ProjectManager': 'Create and manage projects, assign tasks, and oversee team activities.',
            'TeamMember': 'View assigned tasks, collaborate on projects, and participate in team activities.'
        };
        return desc[role] || 'Standard user role.';
    }

    function formatDate(dateStr) {
        if (!dateStr) return '—';
        return new Date(dateStr).toLocaleDateString('en-US', {
            year: 'numeric', month: 'long', day: 'numeric'
        });
    }

    function formatDateTime(dateStr) {
        if (!dateStr) return '—';
        return new Date(dateStr).toLocaleString('en-US', {
            year: 'numeric', month: 'short', day: 'numeric',
            hour: 'numeric', minute: '2-digit'
        });
    }

    function setButtonLoading(btn, loading) {
        if (!btn) return;
        const text = btn.querySelector('.btn-text');
        const spinner = btn.querySelector('.btn-spinner');
        if (loading) {
            btn.disabled = true;
            if (text) text.style.display = 'none';
            if (spinner) spinner.style.display = 'inline-flex';
        } else {
            btn.disabled = false;
            if (text) text.style.display = 'inline';
            if (spinner) spinner.style.display = 'none';
        }
    }

    // ------------------------------------------
    // Toast
    // ------------------------------------------
    function showToast(message, type = 'info') {
        const toast = document.getElementById('settings-toast');
        const msgEl = document.getElementById('toast-message');
        const iconEl = document.getElementById('toast-icon');

        if (!toast || !msgEl) return;

        // Icon
        const icons = {
            success: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg>',
            error: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>',
            info: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>'
        };

        iconEl.innerHTML = icons[type] || icons.info;
        msgEl.textContent = message;

        toast.className = 'settings-toast show ' + type;

        clearTimeout(toast._timeout);
        toast._timeout = setTimeout(() => {
            toast.classList.remove('show');
        }, 3500);
    }

    // ------------------------------------------
    // Theme Selector
    // ------------------------------------------
    function initThemeSelector() {
        const lightOption = document.getElementById('theme-light');
        const darkOption = document.getElementById('theme-dark');

        if (!lightOption || !darkOption) return;

        // Read current theme from localStorage / document attribute
        const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
        setThemeSelection(currentTheme);

        lightOption.addEventListener('click', () => applyTheme('light'));
        darkOption.addEventListener('click', () => applyTheme('dark'));
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('thinkbridge-theme', theme);
        setThemeSelection(theme);
        showToast(theme === 'dark' ? 'Dark mode enabled.' : 'Light mode enabled.', 'success');
    }

    function setThemeSelection(theme) {
        const lightOption = document.getElementById('theme-light');
        const darkOption = document.getElementById('theme-dark');
        if (!lightOption || !darkOption) return;

        lightOption.classList.toggle('selected', theme === 'light');
        darkOption.classList.toggle('selected', theme === 'dark');
    }

})();
