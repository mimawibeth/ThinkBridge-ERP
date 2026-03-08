/* ==========================================
   ThinkBridge ERP - My Subscription Page Script
   ========================================== */
(function () {
    'use strict';

    let paymentHistoryLoaded = false;

    document.addEventListener('DOMContentLoaded', function () {
        loadSubscription();
        loadSubscriptionAlert();

        // Load payment history when modal opens
        const historyModal = document.getElementById('paymentHistoryModal');
        if (historyModal) {
            const observer = new MutationObserver(function () {
                if (historyModal.classList.contains('active') && !paymentHistoryLoaded) {
                    loadPaymentHistory();
                }
            });
            observer.observe(historyModal, { attributes: true, attributeFilter: ['class'] });
        }
    });

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

    async function loadSubscription() {
        const loading = document.getElementById('subscription-loading');
        const empty = document.getElementById('subscription-empty');
        const content = document.getElementById('subscription-content');

        const result = await apiGet('/api/companyadmin/subscription');

        loading.style.display = 'none';

        if (!result.success || !result.data) {
            empty.style.display = 'flex';
            return;
        }

        const sub = result.data;
        content.style.display = 'flex';
        content.style.flexDirection = 'column';
        content.style.gap = 'var(--spacing-lg)';

        currentSubscriptionId = sub.subscriptionId;

        // Status badge
        const statusEl = document.getElementById('plan-status');
        statusEl.textContent = sub.status === 'GracePeriod' ? 'Grace Period' : sub.status;
        statusEl.className = 'plan-badge ' + sub.status.toLowerCase();

        // Plan info
        document.getElementById('plan-name').textContent = sub.planName;
        document.getElementById('plan-company').textContent = sub.companyName;

        // Price
        const price = sub.price;
        document.getElementById('plan-price').textContent = price === 0 ? 'Free' : `₱${price.toLocaleString()}`;
        document.getElementById('plan-cycle').textContent = price === 0 ? '' : `/${sub.billingCycle.toLowerCase()}`;

        // Dates
        const dateOpts = { year: 'numeric', month: 'long', day: 'numeric' };
        document.getElementById('plan-start').textContent = new Date(sub.startDate).toLocaleDateString(undefined, dateOpts);

        if (sub.endDate) {
            const endDate = new Date(sub.endDate);
            document.getElementById('plan-end').textContent = endDate.toLocaleDateString(undefined, dateOpts);

            const now = new Date();
            const diffMs = endDate - now;
            const diffDays = Math.max(0, Math.ceil(diffMs / (1000 * 60 * 60 * 24)));
            const daysEl = document.getElementById('plan-days-left');
            daysEl.textContent = diffDays + (diffDays === 1 ? ' day' : ' days');
            if (diffDays <= 7) daysEl.style.color = 'var(--danger)';
            else if (diffDays <= 14) daysEl.style.color = 'var(--warning)';
        } else {
            document.getElementById('plan-end').textContent = 'No expiry';
            document.getElementById('plan-days-left').textContent = '—';
        }

        // Usage - Users
        document.getElementById('usage-users').textContent = sub.currentUsers;
        const maxUsers = sub.maxUsers;
        document.getElementById('usage-max-users').textContent = maxUsers ? maxUsers : '∞';
        if (maxUsers) {
            const pct = Math.min(100, Math.round((sub.currentUsers / maxUsers) * 100));
            const bar = document.getElementById('usage-users-bar');
            bar.style.width = pct + '%';
            if (pct >= 90) bar.classList.add('danger');
            else if (pct >= 75) bar.classList.add('warning');
        }

        // Usage - Projects
        document.getElementById('usage-projects').textContent = sub.currentProjects;
        const maxProjects = sub.maxProjects;
        document.getElementById('usage-max-projects').textContent = maxProjects ? maxProjects : '∞';
        if (maxProjects) {
            const pct = Math.min(100, Math.round((sub.currentProjects / maxProjects) * 100));
            const bar = document.getElementById('usage-projects-bar');
            bar.style.width = pct + '%';
            if (pct >= 90) bar.classList.add('danger');
            else if (pct >= 75) bar.classList.add('warning');
        }

        // Features
        renderFeatures(sub);

        // Auto-renew toggle (only for paid plans)
        if (sub.price > 0) {
            initAutoRenewToggle(sub.autoRenew);
        }
    }

    function renderFeatures(sub) {
        const container = document.getElementById('features-list');
        const features = [];

        features.push(`${sub.maxUsers ? sub.maxUsers + ' Users' : 'Unlimited Users'}`);
        features.push(`${sub.maxProjects ? sub.maxProjects + ' Projects' : 'Unlimited Projects'}`);
        features.push('Task Management');
        features.push('Product Lifecycle');
        features.push('Knowledge Base');
        features.push('Calendar & Scheduling');
        features.push('Audit Logging');

        if (sub.planName.toLowerCase().includes('professional') || sub.planName.toLowerCase().includes('enterprise')) {
            features.push('Advanced Reports');
            features.push('Priority Support');
        }
        if (sub.planName.toLowerCase().includes('enterprise')) {
            features.push('Custom Integrations');
            features.push('Dedicated Account Manager');
        }

        const checkSvg = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"></polyline></svg>';
        container.innerHTML = features.map(f => `<div class="feature-item">${checkSvg}<span>${escapeHtml(f)}</span></div>`).join('');
    }

    // ─── Subscription Alert & Auto-Renew ─────────

    let currentSubscriptionId = 0;

    async function loadSubscriptionAlert() {
        var banner = document.getElementById('subAlertBanner');
        if (!banner) return;

        var result = await apiGet('/api/companyadmin/subscription/alert');
        if (!result || !result.success || !result.data) {
            banner.style.display = 'none';
            return;
        }

        var a = result.data;
        document.getElementById('subAlertMessage').textContent = a.message;
        if (a.alertType === 'grace-period' || a.alertType === 'auto-renew-failed') {
            banner.classList.add('alert-danger');
        }
        banner.style.display = 'flex';

        // Show renew button if in grace period or expiring soon
        var renewBtn = document.getElementById('btnRenewNow');
        if (renewBtn && (a.alertType === 'grace-period' || a.alertType === 'expiring-soon' || a.alertType === 'auto-renew-failed')) {
            renewBtn.style.display = 'inline-flex';
        }
    }

    function initAutoRenewToggle(autoRenew) {
        var actionsSection = document.getElementById('planRenewalActions');
        var toggle = document.getElementById('autoRenewToggle');
        if (!actionsSection || !toggle) return;

        actionsSection.style.display = 'flex';
        toggle.checked = autoRenew;

        toggle.addEventListener('change', async function () {
            var enabled = toggle.checked;
            try {
                var resp = await fetch('/api/companyadmin/subscription/auto-renew', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'same-origin',
                    body: JSON.stringify({ enabled: enabled })
                });
                var result = await resp.json();
                if (result.success) {
                    if (typeof showToast === 'function') showToast(result.message, 'success');
                } else {
                    toggle.checked = !enabled;
                    if (typeof showToast === 'function') showToast(result.message || 'Failed to update.', 'error');
                }
            } catch (e) {
                toggle.checked = !enabled;
                if (typeof showToast === 'function') showToast('Network error.', 'error');
            }
        });
    }

    window.renewSubscription = async function () {
        if (!currentSubscriptionId) return;
        var btn = document.getElementById('btnRenewNow');
        if (btn) { btn.disabled = true; btn.textContent = 'Processing...'; }

        try {
            var resp = await fetch('/api/subscription/renew', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify({ subscriptionId: currentSubscriptionId })
            });
            var result = await resp.json();
            if (result.success && result.checkoutUrl) {
                window.location.href = result.checkoutUrl;
            } else {
                if (typeof showToast === 'function') showToast(result.message || 'Renewal failed.', 'error');
                if (btn) { btn.disabled = false; btn.textContent = 'Renew Now'; }
            }
        } catch (e) {
            if (typeof showToast === 'function') showToast('Network error.', 'error');
            if (btn) { btn.disabled = false; btn.textContent = 'Renew Now'; }
        }
    };

    // ─── Payment History ─────────────────────────

    async function loadPaymentHistory() {
        const loading = document.getElementById('payment-history-loading');
        const empty = document.getElementById('payment-history-empty');
        const content = document.getElementById('payment-history-content');

        loading.style.display = 'flex';
        empty.style.display = 'none';
        content.style.display = 'none';

        const result = await apiGet('/api/companyadmin/payment-history');

        loading.style.display = 'none';

        if (!result.success || !result.data || result.data.length === 0) {
            empty.style.display = 'flex';
            return;
        }

        paymentHistoryLoaded = true;
        content.style.display = 'block';

        const tbody = document.getElementById('payment-history-body');
        const dateOpts = { timeZone: 'Asia/Manila', year: 'numeric', month: 'short', day: 'numeric' };

        function toUtc(ds) {
            if (!ds) return null;
            const s = String(ds);
            return new Date(s.endsWith('Z') || s.includes('+') ? s : s + 'Z');
        }

        tbody.innerHTML = result.data.map(p => {
            const d = p.paidAt ? toUtc(p.paidAt) : toUtc(p.createdAt);
            const date = d ? d.toLocaleDateString('en-US', dateOpts) : '—';
            const invoice = p.invoiceNumber || '—';
            const statusClass = getStatusClass(p.status);
            return `<tr>
                <td>${escapeHtml(date)}</td>
                <td>${escapeHtml(invoice)}</td>
                <td>${escapeHtml(p.planName)}</td>
                <td><strong>₱${p.amount.toLocaleString()}</strong></td>
                <td><span class="payment-status ${statusClass}">${escapeHtml(p.status)}</span></td>
                <td>
                    <button class="btn-receipt" onclick="window.viewReceipt(${p.paymentId})" title="View Receipt">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                            <polyline points="14 2 14 8 20 8"></polyline>
                            <line x1="16" y1="13" x2="8" y2="13"></line>
                            <line x1="16" y1="17" x2="8" y2="17"></line>
                            <polyline points="10 9 9 9 8 9"></polyline>
                        </svg>
                    </button>
                </td>
            </tr>`;
        }).join('');
    }

    function getStatusClass(status) {
        switch ((status || '').toLowerCase()) {
            case 'paid': case 'completed': case 'succeeded': return 'status-paid';
            case 'pending': return 'status-pending';
            case 'failed': case 'expired': return 'status-failed';
            default: return '';
        }
    }

    // ─── Receipt ─────────────────────────────────

    window.viewReceipt = async function (paymentId) {
        // Open modal
        if (typeof openModal === 'function') openModal('receiptModal');

        const loading = document.getElementById('receipt-loading');
        const content = document.getElementById('receipt-content');

        loading.style.display = 'flex';
        content.style.display = 'none';

        const result = await apiGet(`/api/companyadmin/payment-history/${paymentId}/receipt`);

        loading.style.display = 'none';

        if (!result.success || !result.data) {
            if (typeof showToast === 'function') showToast('Failed to load receipt.', 'error');
            if (typeof closeModal === 'function') closeModal('receiptModal');
            return;
        }

        const r = result.data;
        const dateOpts = { year: 'numeric', month: 'long', day: 'numeric' };
        const shortDateOpts = { year: 'numeric', month: 'short', day: 'numeric' };

        content.style.display = 'block';

        document.getElementById('receipt-invoice-number').textContent = r.invoiceNumber ? `Invoice ${r.invoiceNumber}` : `Payment #${r.paymentId}`;
        document.getElementById('receipt-company').textContent = r.companyName;
        document.getElementById('receipt-plan').textContent = r.planName;
        document.getElementById('receipt-cycle').textContent = r.billingCycle || '—';
        document.getElementById('receipt-amount').textContent = `₱${r.amount.toLocaleString()}`;
        document.getElementById('receipt-method').textContent = r.paymentMethod || r.provider || '—';
        document.getElementById('receipt-provider').textContent = r.provider || '—';
        document.getElementById('receipt-txn').textContent = r.externalTransactionId || '—';
        document.getElementById('receipt-date').textContent = r.paidAt ? new Date(r.paidAt).toLocaleDateString(undefined, dateOpts) : '—';

        const statusEl = document.getElementById('receipt-status');
        statusEl.textContent = r.status;
        statusEl.className = 'receipt-status ' + getStatusClass(r.status);

        // Subscription period
        const start = r.subscriptionStart ? new Date(r.subscriptionStart).toLocaleDateString(undefined, shortDateOpts) : '—';
        const end = r.subscriptionEnd ? new Date(r.subscriptionEnd).toLocaleDateString(undefined, shortDateOpts) : 'No expiry';
        document.getElementById('receipt-period').textContent = `${start} — ${end}`;
    };

    window.downloadReceipt = function () {
        if (typeof html2pdf === 'undefined') {
            // Fallback: open printable window if CDN failed to load
            var w = window.open('', '_blank', 'width=600,height=700');
            w.document.write(buildReceiptHTML());
            w.document.close();
            w.focus();
            setTimeout(function () { w.print(); }, 400);
            return;
        }

        var container = document.createElement('div');
        container.innerHTML = buildReceiptHTML();

        var invoiceNum = (document.getElementById('receipt-invoice-number').textContent || 'Receipt').replace(/[^a-zA-Z0-9_-]/g, '_');

        html2pdf().set({
            margin: [15, 18, 15, 18],
            filename: invoiceNum + '.pdf',
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: { scale: 2, useCORS: true, logging: false },
            jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }
        }).from(container).save();
    };

    function buildReceiptHTML() {
        var inv = escapeHtml(document.getElementById('receipt-invoice-number').textContent);
        var company = escapeHtml(document.getElementById('receipt-company').textContent);
        var date = escapeHtml(document.getElementById('receipt-date').textContent);
        var plan = escapeHtml(document.getElementById('receipt-plan').textContent);
        var cycle = escapeHtml(document.getElementById('receipt-cycle').textContent);
        var period = escapeHtml(document.getElementById('receipt-period').textContent);
        var amount = escapeHtml(document.getElementById('receipt-amount').textContent);
        var method = escapeHtml(document.getElementById('receipt-method').textContent);
        var status = escapeHtml(document.getElementById('receipt-status').textContent);
        var provider = escapeHtml(document.getElementById('receipt-provider').textContent);
        var txn = escapeHtml(document.getElementById('receipt-txn').textContent);
        var year = new Date().getFullYear();

        return '<!DOCTYPE html><html><head><meta charset="utf-8"><title>' + inv + '</title>' +
            '<style>' +
            'body{font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,sans-serif;margin:0;padding:48px 40px;color:#1a1a2e;max-width:600px;margin:0 auto;}' +
            '.brand{font-size:0.7rem;color:#9ca3af;margin:0 0 6px;letter-spacing:0.3px;}' +
            'h1{font-size:1.4rem;font-weight:700;margin:0 0 2px;}' +
            '.inv{font-size:0.82rem;color:#6b7280;margin:0 0 28px;}' +
            '.row{display:flex;justify-content:space-between;margin-bottom:14px;}' +
            '.cell{display:flex;flex-direction:column;gap:2px;}' +
            '.cell-r{text-align:right;}' +
            '.lbl{font-size:0.65rem;font-weight:600;text-transform:uppercase;letter-spacing:0.5px;color:#6b7280;}' +
            '.val{font-size:0.92rem;}' +
            'hr{border:none;border-top:1px solid #e5e7eb;margin:8px 0 16px;}' +
            '.amt-block{text-align:center;padding:16px 0;}' +
            '.amt{font-size:1.6rem;font-weight:700;color:#0B4F6C;}' +
            '.badge{display:inline-block;padding:2px 10px;border-radius:12px;font-size:0.72rem;font-weight:600;}' +
            '.badge-paid{background:#d1fae5;color:#065f46;}' +
            '.badge-pending{background:#fef3c7;color:#92400e;}' +
            '.badge-failed{background:#fee2e2;color:#991b1b;}' +
            '.txn{font-size:0.75rem;font-family:monospace;word-break:break-all;color:#6b7280;}' +
            '.foot{text-align:center;font-size:0.7rem;color:#9ca3af;margin-top:24px;padding-top:12px;border-top:1px dashed #e5e7eb;}' +
            '</style></head><body>' +
            '<p class="brand">ThinkBridge ERP</p>' +
            '<h1>Payment Receipt</h1>' +
            '<p class="inv">' + inv + '</p>' +
            '<div class="row"><div class="cell"><span class="lbl">Billed To</span><span class="val">' + company + '</span></div>' +
            '<div class="cell cell-r"><span class="lbl">Date Paid</span><span class="val">' + date + '</span></div></div>' +
            '<hr>' +
            '<div class="row"><div class="cell"><span class="lbl">Plan</span><span class="val">' + plan + '</span></div>' +
            '<div class="cell cell-r"><span class="lbl">Billing Cycle</span><span class="val">' + cycle + '</span></div></div>' +
            '<div class="row"><div class="cell"><span class="lbl">Subscription Period</span><span class="val">' + period + '</span></div></div>' +
            '<hr>' +
            '<div class="amt-block"><span class="lbl">Amount Paid</span><br><span class="amt">' + amount + '</span></div>' +
            '<hr>' +
            '<div class="row"><div class="cell"><span class="lbl">Payment Method</span><span class="val">' + method + '</span></div>' +
            '<div class="cell cell-r"><span class="lbl">Status</span><span class="badge badge-' + status.toLowerCase() + '">' + status + '</span></div></div>' +
            '<div class="row"><div class="cell"><span class="lbl">Provider</span><span class="val">' + provider + '</span></div></div>' +
            '<hr>' +
            '<div class="row"><div class="cell"><span class="lbl">Transaction ID</span><span class="txn">' + txn + '</span></div></div>' +
            '<p class="foot">This is a computer-generated receipt. No signature required.<br>ThinkBridge ERP &copy; ' + year + '</p>' +
            '</body></html>';
    }

    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
})();
