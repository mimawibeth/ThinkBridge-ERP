/**
 * ThinkBridge ERP - Projects Page JavaScript
 * ==========================================
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        initProjectModals();
        initProjectCards();
    });

    // ===========================================
    // Project Modal Handlers
    // ===========================================
    function initProjectModals() {
        // New Project Button
        const newProjectBtn = document.getElementById('new-project-btn');
        if (newProjectBtn) {
            newProjectBtn.addEventListener('click', function () {
                window.openModal('new-project-modal');
            });
        }

        // Close handlers for new project modal
        const modalClose = document.getElementById('modal-close');
        const modalCancel = document.getElementById('modal-cancel');

        if (modalClose) {
            modalClose.addEventListener('click', function () {
                window.closeModal('new-project-modal');
            });
        }

        if (modalCancel) {
            modalCancel.addEventListener('click', function () {
                window.closeModal('new-project-modal');
            });
        }

        // Edit button from view modal
        const editFromView = document.getElementById('edit-project-from-view');
        if (editFromView) {
            editFromView.addEventListener('click', function () {
                window.closeModal('view-project-modal');
                setTimeout(function () {
                    window.openModal('edit-project-modal');
                }, 200);
            });
        }
    }

    // ===========================================
    // Project Card Click Handlers
    // ===========================================
    function initProjectCards() {
        const projectCards = document.querySelectorAll('.project-card');

        projectCards.forEach(function (card) {
            // Make the card clickable to view details
            card.style.cursor = 'pointer';

            card.addEventListener('click', function (e) {
                // Don't open modal if clicking on a button or link
                if (e.target.closest('button') || e.target.closest('a')) {
                    return;
                }

                // Get project data from the card
                const projectId = card.querySelector('.project-id')?.textContent || '';
                const projectName = card.querySelector('.project-name')?.textContent || '';

                // Update view modal with project data
                updateViewModal(projectId, projectName);

                // Open view modal
                window.openModal('view-project-modal');
            });
        });
    }

    // ===========================================
    // Update View Modal with Project Data
    // ===========================================
    function updateViewModal(projectId, projectName) {
        const viewModal = document.getElementById('view-project-modal');
        if (!viewModal) return;

        // Update header
        const header = viewModal.querySelector('.modal-header h2');
        const subtitle = viewModal.querySelector('.modal-subtitle');

        if (header) header.textContent = projectName;
        if (subtitle) subtitle.textContent = projectId;
    }

})();
