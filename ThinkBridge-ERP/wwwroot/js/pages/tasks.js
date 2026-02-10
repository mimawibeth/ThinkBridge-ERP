/**
 * ThinkBridge ERP - Tasks Page JavaScript
 * ==========================================
 */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        initTaskModals();
        initTaskRows();
    });

    // ===========================================
    // Task Modal Handlers
    // ===========================================
    function initTaskModals() {
        // New Task Button
        const newTaskBtn = document.getElementById('new-task-btn');
        if (newTaskBtn) {
            newTaskBtn.addEventListener('click', function () {
                window.openModal('new-task-modal');
            });
        }

        // Close handlers for new task modal
        const taskModalClose = document.getElementById('task-modal-close');
        const taskModalCancel = document.getElementById('task-modal-cancel');

        if (taskModalClose) {
            taskModalClose.addEventListener('click', function () {
                window.closeModal('new-task-modal');
            });
        }

        if (taskModalCancel) {
            taskModalCancel.addEventListener('click', function () {
                window.closeModal('new-task-modal');
            });
        }

        // Edit button from view modal
        const editFromView = document.getElementById('edit-task-from-view');
        if (editFromView) {
            editFromView.addEventListener('click', function () {
                window.closeModal('view-task-modal');
                setTimeout(function () {
                    window.openModal('edit-task-modal');
                }, 200);
            });
        }
    }

    // ===========================================
    // Task Row Click Handlers
    // ===========================================
    function initTaskRows() {
        const taskRows = document.querySelectorAll('.tasks-table tbody tr');

        taskRows.forEach(function (row) {
            // Make the task name clickable
            const taskInfo = row.querySelector('.task-info');
            if (taskInfo) {
                taskInfo.style.cursor = 'pointer';

                taskInfo.addEventListener('click', function (e) {
                    // Don't open modal if clicking on checkbox
                    if (e.target.closest('.checkbox')) {
                        return;
                    }

                    // Get task data from the row
                    const taskId = row.querySelector('.task-id')?.textContent || '';
                    const taskName = row.querySelector('.task-name')?.textContent || '';

                    // Update view modal with task data
                    updateViewModal(taskId, taskName);

                    // Open view modal
                    window.openModal('view-task-modal');
                });
            }
        });
    }

    // ===========================================
    // Update View Modal with Task Data
    // ===========================================
    function updateViewModal(taskId, taskName) {
        const viewModal = document.getElementById('view-task-modal');
        if (!viewModal) return;

        // Update header
        const header = viewModal.querySelector('.modal-header h2');
        const subtitle = viewModal.querySelector('.modal-subtitle');

        if (header) header.textContent = taskName;
        if (subtitle) subtitle.textContent = taskId;
    }

})();
