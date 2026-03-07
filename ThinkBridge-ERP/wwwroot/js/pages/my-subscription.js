/* ==========================================
   ThinkBridge ERP - My Subscription Page Script
   ========================================== */
(function () {
    'use strict';

    let paymentHistoryLoaded = false;

    document.addEventListener('DOMContentLoaded', function () {
        loadSubscription();

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

        // Status badge
        const statusEl = document.getElementById('plan-status');
        statusEl.textContent = sub.status;
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
        const dateOpts = { year: 'numeric', month: 'short', day: 'numeric' };

        tbody.innerHTML = result.data.map(p => {
            const date = p.paidAt ? new Date(p.paidAt).toLocaleDateString(undefined, dateOpts)
                : new Date(p.createdAt).toLocaleDateString(undefined, dateOpts);
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

    window.printReceipt = function () {
        const content = document.getElementById('receipt-content');
        if (!content) return;

        const invoiceNum = document.getElementById('receipt-invoice-number').textContent;
        const printWindow = window.open('', '_blank', 'width=600,height=700');
        printWindow.document.write(`<!DOCTYPE html>
<html><head><title>${escapeHtml(invoiceNum)} - ThinkBridge ERP</title>
<style>
body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 40px; color: #1a1a2e; max-width: 560px; margin: 0 auto; }
h1 { font-size: 1.5rem; margin: 0 0 4px; }
.subtitle { font-size: 0.875rem; color: #6b7280; margin: 0 0 24px; }
.brand { font-size: 0.75rem; color: #6b7280; margin-bottom: 20px; }
.section-title { font-size: 0.7rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; color: #6b7280; margin: 0 0 4px; }
.value { font-size: 0.95rem; margin: 0 0 16px; }
.amount { font-size: 1.25rem; font-weight: 700; color: #2563eb; }
.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0 20px; }
hr { border: none; border-top: 1px solid #e5e7eb; margin: 16px 0; }
.status { display: inline-block; padding: 2px 10px; border-radius: 12px; font-size: 0.75rem; font-weight: 600; }
.status-paid { background: #d1fae5; color: #065f46; }
.status-pending { background: #fef3c7; color: #92400e; }
.status-failed { background: #fee2e2; color: #991b1b; }
.footer { margin-top: 32px; text-align: center; font-size: 0.75rem; color: #9ca3af; }
</style></head><body>
<p class="brand">ThinkBridge ERP</p>
<h1>Payment Receipt</h1>
<p class="subtitle">${escapeHtml(invoiceNum)}</p>
<p class="section-title">Company</p>
<p class="value">${escapeHtml(document.getElementById('receipt-company').textContent)}</p>
<div class="grid">
<div><p class="section-title">Plan</p><p class="value">${escapeHtml(document.getElementById('receipt-plan').textContent)}</p></div>
<div><p class="section-title">Billing Cycle</p><p class="value">${escapeHtml(document.getElementById('receipt-cycle').textContent)}</p></div>
</div>
<hr>
<div class="grid">
<div><p class="section-title">Amount Paid</p><p class="value amount">${escapeHtml(document.getElementById('receipt-amount').textContent)}</p></div>
<div><p class="section-title">Payment Method</p><p class="value">${escapeHtml(document.getElementById('receipt-method').textContent)}</p></div>
</div>
<div class="grid">
<div><p class="section-title">Status</p><p class="value"><span class="status ${document.getElementById('receipt-status').className.replace('receipt-status', '').trim()}">${escapeHtml(document.getElementById('receipt-status').textContent)}</span></p></div>
<div><p class="section-title">Date Paid</p><p class="value">${escapeHtml(document.getElementById('receipt-date').textContent)}</p></div>
</div>
<hr>
<div class="grid">
<div><p class="section-title">Provider</p><p class="value">${escapeHtml(document.getElementById('receipt-provider').textContent)}</p></div>
<div><p class="section-title">Transaction ID</p><p class="value" style="word-break:break-all;font-size:0.8rem;">${escapeHtml(document.getElementById('receipt-txn').textContent)}</p></div>
</div>
<p class="section-title">Subscription Period</p>
<p class="value">${escapeHtml(document.getElementById('receipt-period').textContent)}</p>
<p class="footer">This is a computer-generated receipt. No signature required.<br>ThinkBridge ERP &copy; ${new Date().getFullYear()}</p>
</body></html>`);
        printWindow.document.close();
        printWindow.focus();
        setTimeout(() => printWindow.print(), 300);
    };

    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
})();
