/**
 * Knowledge Base page JavaScript
 */
document.addEventListener('DOMContentLoaded', function () {
    initKnowledgeBaseModals();
    initArticleCards();
    initCategoryFilters();
    initSearch();
});

/**
 * Initialize knowledge base modal handlers
 */
function initKnowledgeBaseModals() {
    // New Article button
    const newArticleBtn = document.getElementById('new-article-btn');
    if (newArticleBtn) {
        newArticleBtn.addEventListener('click', function () {
            openModal('new-article-modal');
        });
    }

    // Edit from View modal
    const editFromViewBtn = document.getElementById('edit-article-from-view');
    if (editFromViewBtn) {
        editFromViewBtn.addEventListener('click', function () {
            closeModal('view-article-modal');
            setTimeout(() => {
                openModal('edit-article-modal');
            }, 200);
        });
    }
}

/**
 * Initialize article card click handlers
 */
function initArticleCards() {
    const articleCards = document.querySelectorAll('.article-card');

    articleCards.forEach(card => {
        card.addEventListener('click', function (e) {
            // Get article data from card
            const articleTitle = card.querySelector('.article-title')?.textContent || 'Article';
            const articleCategory = card.querySelector('.article-category')?.textContent || 'General';

            // Update view modal with article data
            updateViewArticleModal(card);

            // Open view modal
            openModal('view-article-modal');
        });

        // Add visual feedback
        card.style.cursor = 'pointer';
    });
}

/**
 * Update view article modal with data from card
 */
function updateViewArticleModal(card) {
    const modal = document.getElementById('view-article-modal');
    if (!modal) return;

    const articleTitle = card.querySelector('.article-title')?.textContent || 'Article';
    const articleCategory = card.querySelector('.article-category')?.textContent || 'General';
    const articleDescription = card.querySelector('.article-description')?.textContent || '';
    const articleAuthor = card.querySelector('.meta-author span')?.textContent || 'Unknown';
    const articleDate = card.querySelector('.meta-date span')?.textContent || '';
    const articleViews = card.querySelector('.meta-views span')?.textContent || '0';

    // Update modal header
    const modalTitle = modal.querySelector('.modal-header h2');
    const modalSubtitle = modal.querySelector('.modal-subtitle');

    if (modalTitle) modalTitle.textContent = articleTitle;
    if (modalSubtitle) modalSubtitle.textContent = articleCategory;

    // Update detail values
    const authorValue = modal.querySelector('.detail-item:nth-child(1) .detail-value');
    const dateValue = modal.querySelector('.detail-item:nth-child(2) .detail-value');
    const viewsValue = modal.querySelector('.detail-item:nth-child(3) .detail-value');
    const summaryValue = modal.querySelector('.detail-section:nth-child(2) .detail-description');

    if (authorValue) authorValue.textContent = articleAuthor;
    if (dateValue) dateValue.textContent = articleDate;
    if (viewsValue) viewsValue.textContent = articleViews;
    if (summaryValue) summaryValue.textContent = articleDescription;
}

/**
 * Initialize category filter handlers
 */
function initCategoryFilters() {
    const categoryItems = document.querySelectorAll('.category-item');

    categoryItems.forEach(item => {
        item.addEventListener('click', function () {
            // Remove active from all items
            categoryItems.forEach(i => i.classList.remove('active'));
            // Add active to clicked item
            this.classList.add('active');

            // Get category name
            const categoryName = this.querySelector('span:not(.category-count)')?.textContent?.toLowerCase() || 'all';

            // Filter articles
            filterArticlesByCategory(categoryName);
        });
    });
}

/**
 * Filter articles by category
 */
function filterArticlesByCategory(category) {
    const articles = document.querySelectorAll('.article-card');

    articles.forEach(article => {
        if (category === 'all articles') {
            article.style.display = '';
        } else {
            const articleCategory = article.querySelector('.article-category')?.textContent?.toLowerCase() || '';
            article.style.display = articleCategory.includes(category) ? '' : 'none';
        }
    });
}

/**
 * Initialize search functionality
 */
function initSearch() {
    const searchInput = document.querySelector('.search-input');

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const query = this.value.toLowerCase();
            const articles = document.querySelectorAll('.article-card');

            articles.forEach(article => {
                const title = article.querySelector('.article-title')?.textContent?.toLowerCase() || '';
                const description = article.querySelector('.article-description')?.textContent?.toLowerCase() || '';

                const matches = title.includes(query) || description.includes(query);
                article.style.display = matches ? '' : 'none';
            });
        });
    }
}
