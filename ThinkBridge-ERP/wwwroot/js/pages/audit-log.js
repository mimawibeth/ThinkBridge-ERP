/* ==========================================
   ThinkBridge ERP - Audit Log Page Script
   ========================================== */
(function () {
    'use strict';

    let currentPage = 1;
    const pageSize = 15;
    let currentSearch = '';
    let currentAction = 'All';
    let currentDateRange = '';
    let totalPages = 1;

    // ─── Init ────────────────────────────────────

    document.addEventListener('DOMContentLoaded', function () {
        initSearch();
        initFilters();
        loadAuditLogs();
    });

    // ─── API Helper ──────────────────────────────

    async function apiGet(url) {
        try {
            const resp = await fetch(url, { credentials: 'same-origin' });
            if (!resp.ok) {
                const err = await resp.json().catch(() => null);
                return { success: false, message: err?.message || `Error ${resp.status}` };
            }
            return await resp.json();
        } catch (e) {
            return { success: false, message: 'Network error.' };
        }
    }

    // ─── Search ──────────────────────────────────

    function initSearch() {
        const searchInput = document.getElementById('audit-search');
        if (!searchInput) return;

        let timeout;
        searchInput.addEventListener('input', function () {
            clearTimeout(timeout);
            timeout = setTimeout(() => {
                currentSearch = this.value.trim();
                currentPage = 1;
                loadAuditLogs();
            }, 400);
        });
    }

    // ─── Filters ─────────────────────────────────

    function initFilters() {
        // Action filter dropdown
        const actionDropdown = document.getElementById('action-filter');
        const actionBtn = document.getElementById('action-filter-btn');
        const actionItems = document.querySelectorAll('.action-filter-item');

        if (actionBtn && actionDropdown) {
            actionBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                actionDropdown.classList.toggle('open');
                document.getElementById('date-filter')?.classList.remove('open');
            });

            actionItems.forEach(item => {
                item.addEventListener('click', function () {
                    currentAction = this.dataset.value;
                    actionBtn.querySelector('.filter-text').textContent = this.textContent;
                    actionDropdown.classList.remove('open');
                    currentPage = 1;
                    loadAuditLogs();
                });
            });
        }

        // Date range filter dropdown
        const dateDropdown = document.getElementById('date-filter');
        const dateBtn = document.getElementById('date-filter-btn');
        const dateItems = document.querySelectorAll('.date-filter-item');

        if (dateBtn && dateDropdown) {
            dateBtn.addEventListener('click', function (e) {
                e.stopPropagation();
                dateDropdown.classList.toggle('open');
                document.getElementById('action-filter')?.classList.remove('open');
            });

            dateItems.forEach(item => {
                item.addEventListener('click', function () {
                    currentDateRange = this.dataset.value;
                    dateBtn.querySelector('.filter-text').textContent = this.textContent;
                    dateDropdown.classList.remove('open');
                    currentPage = 1;
                    loadAuditLogs();
                });
            });
        }

        // Close dropdowns on outside click
        document.addEventListener('click', function () {
            document.querySelectorAll('.filter-dropdown.open').forEach(d => d.classList.remove('open'));
        });
    }

    // ─── Load Audit Logs ─────────────────────────

    async function loadAuditLogs() {
        const tbody = document.getElementById('audit-log-body');
        if (!tbody) return;

        tbody.innerHTML = '<tr><td colspan="6" style="text-align:center; padding:2rem; color:var(--text-muted);">Loading audit logs...</td></tr>';

        const params = new URLSearchParams({
            page: currentPage,
            pageSize: pageSize
        });

        if (currentSearch) params.append('search', currentSearch);
        if (currentAction && currentAction !== 'All') params.append('action', currentAction);
        if (currentDateRange) params.append('dateRange', currentDateRange);

        const result = await apiGet(`/api/companyadmin/auditlogs?${params}`);

        if (!result.success) {
            tbody.innerHTML = `<tr><td colspan="6" style="text-align:center; padding:2rem; color:var(--danger);">${escapeHtml(result.message)}</td></tr>`;
            return;
        }

        const logs = result.data || [];
        totalPages = result.pagination?.totalPages || 1;
        const totalCount = result.pagination?.totalCount || 0;

        if (logs.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align:center; padding:2rem; color:var(--text-muted);">No audit logs found.</td></tr>';
            renderPagination(0);
            return;
        }

        let html = '';
        logs.forEach(log => {
            const date = new Date(log.createdAt);
            const dateStr = date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
            const timeStr = date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', second: '2-digit' });
            const initials = getInitials(log.userName);
            const actionClass = getActionClass(log.action);

            html += `<tr>
                <td class="timestamp">
                    <span class="date">${escapeHtml(dateStr)}</span>
                    <span class="time">${escapeHtml(timeStr)}</span>
                </td>
                <td class="user">
                    <div class="user-cell">
                        <span class="user-avatar">${escapeHtml(initials)}</span>
                        <div class="user-info">
                            <span class="user-name">${escapeHtml(log.userName)}</span>
                            <span class="user-email">${escapeHtml(log.userEmail)}</span>
                        </div>
                    </div>
                </td>
                <td>
                    <span class="action-badge ${actionClass}">${escapeHtml(log.action)}</span>
                </td>
                <td class="resource">
                    <span class="resource-type">${escapeHtml(log.entityName)}</span>
                    <span class="resource-name">ID: ${log.entityId}</span>
                </td>
                <td class="details">${escapeHtml(log.action)} on ${escapeHtml(log.entityName)}</td>
                <td class="ip-address">${escapeHtml(log.ipAddress || '—')}</td>
            </tr>`;
        });

        tbody.innerHTML = html;
        renderPagination(totalCount);
    }

    // ─── Pagination ──────────────────────────────

    function renderPagination(totalCount) {
        const info = document.getElementById('pagination-info');
        const controls = document.getElementById('pagination-controls');

        if (info) {
            const start = totalCount === 0 ? 0 : (currentPage - 1) * pageSize + 1;
            const end = Math.min(currentPage * pageSize, totalCount);
            info.innerHTML = `Showing <strong>${start}-${end}</strong> of <strong>${totalCount}</strong> entries`;
        }

        if (!controls) return;

        let html = '';

        // Prev button
        html += `<button class="pagination-btn" ${currentPage <= 1 ? 'disabled' : ''} onclick="auditGoToPage(${currentPage - 1})">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15 18 9 12 15 6"></polyline></svg>
        </button>`;

        // Page numbers
        const maxVisible = 5;
        let startPage = Math.max(1, currentPage - Math.floor(maxVisible / 2));
        let endPage = Math.min(totalPages, startPage + maxVisible - 1);
        if (endPage - startPage < maxVisible - 1) {
            startPage = Math.max(1, endPage - maxVisible + 1);
        }

        if (startPage > 1) {
            html += `<button class="pagination-btn" onclick="auditGoToPage(1)">1</button>`;
            if (startPage > 2) html += '<span class="pagination-ellipsis">...</span>';
        }

        for (let i = startPage; i <= endPage; i++) {
            html += `<button class="pagination-btn ${i === currentPage ? 'active' : ''}" onclick="auditGoToPage(${i})">${i}</button>`;
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) html += '<span class="pagination-ellipsis">...</span>';
            html += `<button class="pagination-btn" onclick="auditGoToPage(${totalPages})">${totalPages}</button>`;
        }

        // Next button
        html += `<button class="pagination-btn" ${currentPage >= totalPages ? 'disabled' : ''} onclick="auditGoToPage(${currentPage + 1})">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
        </button>`;

        controls.innerHTML = html;
    }

    window.auditGoToPage = function (page) {
        if (page < 1 || page > totalPages) return;
        currentPage = page;
        loadAuditLogs();
    };

    // ─── Helpers ─────────────────────────────────

    function getInitials(name) {
        if (!name) return '?';
        return name.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2);
    }

    function getActionClass(action) {
        if (!action) return '';
        const a = action.toLowerCase();
        if (a.includes('create') || a.includes('add')) return 'create';
        if (a.includes('update') || a.includes('edit') || a.includes('change')) return 'update';
        if (a.includes('delete') || a.includes('remove')) return 'delete';
        if (a.includes('upload')) return 'upload';
        if (a.includes('login') || a.includes('auth')) return 'login';
        if (a.includes('permission') || a.includes('role')) return 'permission';
        return 'update';
    }

    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
})();
