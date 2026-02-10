/**
 * ThinkBridge ERP - Team Page JavaScript
 * ==========================================
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        initMemberModals();
        initMemberCards();
    });

    // ===========================================
    // Member Modal Handlers
    // ===========================================
    function initMemberModals() {
        // Add Member Button
        const addMemberBtn = document.getElementById('add-member-btn');
        if (addMemberBtn) {
            addMemberBtn.addEventListener('click', function () {
                window.openModal('add-member-modal');
            });
        }

        // Close handlers for add member modal
        const memberModalClose = document.getElementById('member-modal-close');
        const memberModalCancel = document.getElementById('member-modal-cancel');

        if (memberModalClose) {
            memberModalClose.addEventListener('click', function () {
                window.closeModal('add-member-modal');
            });
        }

        if (memberModalCancel) {
            memberModalCancel.addEventListener('click', function () {
                window.closeModal('add-member-modal');
            });
        }

        // Edit button from view modal
        const editFromView = document.getElementById('edit-member-from-view');
        if (editFromView) {
            editFromView.addEventListener('click', function () {
                window.closeModal('view-member-modal');
                setTimeout(function () {
                    window.openModal('edit-member-modal');
                }, 200);
            });
        }
    }

    // ===========================================
    // Member Card Click Handlers
    // ===========================================
    function initMemberCards() {
        const memberCards = document.querySelectorAll('.team-card');

        memberCards.forEach(function (card) {
            // Make the card clickable to view details (except menu button)
            card.addEventListener('click', function (e) {
                // Don't open modal if clicking on menu button
                if (e.target.closest('.card-menu-btn')) {
                    return;
                }

                // Get member data from the card
                const memberName = card.querySelector('.member-name')?.textContent || '';
                const memberRole = card.querySelector('.badge')?.textContent || '';

                // Update view modal with member data
                updateViewModal(memberName, memberRole);

                // Open view modal
                window.openModal('view-member-modal');
            });

            // Make card appear clickable
            card.style.cursor = 'pointer';
        });
    }

    // ===========================================
    // Update View Modal with Member Data
    // ===========================================
    function updateViewModal(memberName, memberRole) {
        const viewModal = document.getElementById('view-member-modal');
        if (!viewModal) return;

        // Update header
        const header = viewModal.querySelector('.modal-header h2');
        const subtitle = viewModal.querySelector('.modal-subtitle');

        if (header) header.textContent = memberName;
        if (subtitle) subtitle.textContent = memberRole;
    }

})();
