/**
 * Product Lifecycle page JavaScript
 */
document.addEventListener('DOMContentLoaded', function () {
    initProductLifecycleModals();
    initProductCards();
    initStageFilters();
});

/**
 * Initialize product lifecycle modal handlers
 */
function initProductLifecycleModals() {
    // Edit from View modal
    const editFromViewBtn = document.getElementById('edit-product-from-view');
    if (editFromViewBtn) {
        editFromViewBtn.addEventListener('click', function () {
            closeModal('view-product-modal');
            setTimeout(() => {
                openModal('edit-product-modal');
            }, 200);
        });
    }
}

/**
 * Initialize product card click handlers
 */
function initProductCards() {
    const productCards = document.querySelectorAll('.product-card');

    productCards.forEach(card => {
        card.addEventListener('click', function (e) {
            // Don't trigger if clicking on action buttons
            if (e.target.closest('.product-actions')) {
                return;
            }

            // Get product data from card
            const productName = card.querySelector('.product-name')?.textContent || 'Product';
            const productId = card.querySelector('.product-id')?.textContent || 'PLM-000';

            // Update view modal with product data
            updateViewProductModal(card);

            // Open view modal
            openModal('view-product-modal');
        });

        // Add visual feedback
        card.style.cursor = 'pointer';
    });
}

/**
 * Update view product modal with data from card
 */
function updateViewProductModal(card) {
    const modal = document.getElementById('view-product-modal');
    if (!modal) return;

    const productName = card.querySelector('.product-name')?.textContent || 'Product';
    const productId = card.querySelector('.product-id')?.textContent || 'PLM-000';

    // Update modal header
    const modalTitle = modal.querySelector('.modal-header h2');
    const modalSubtitle = modal.querySelector('.modal-subtitle');

    if (modalTitle) modalTitle.textContent = productName;
    if (modalSubtitle) modalSubtitle.textContent = productId;
}

/**
 * Initialize stage filter tabs
 */
function initStageFilters() {
    const stageTabs = document.querySelectorAll('.stage-tab');

    stageTabs.forEach(tab => {
        tab.addEventListener('click', function () {
            // Remove active from all tabs
            stageTabs.forEach(t => t.classList.remove('active'));
            // Add active to clicked tab
            this.classList.add('active');

            // Filter products by stage
            const stage = this.dataset.stage;
            filterProductsByStage(stage);
        });
    });
}

/**
 * Filter products by lifecycle stage
 */
function filterProductsByStage(stage) {
    const products = document.querySelectorAll('.product-card');

    products.forEach(product => {
        if (stage === 'all') {
            product.style.display = '';
        } else {
            const productStage = product.dataset.stage;
            product.style.display = productStage === stage ? '' : 'none';
        }
    });
}
