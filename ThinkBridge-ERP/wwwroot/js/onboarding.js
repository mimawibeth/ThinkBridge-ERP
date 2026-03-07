/**
 * ThinkBridge ERP - Onboarding Tour & Help Panel
 * ================================================
 */
(function () {
    'use strict';

    /* ==================== SVG Icon Library ==================== */

    const svgIcons = {
        rocket: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4.5 16.5c-1.5 1.26-2 5-2 5s3.74-.5 5-2c.71-.84.7-2.13-.09-2.91a2.18 2.18 0 0 0-2.91-.09z"></path><path d="M12 15l-3-3a22 22 0 0 1 2-3.95A12.88 12.88 0 0 1 22 2c0 2.72-.78 7.5-6 11a22.35 22.35 0 0 1-4 2z"></path><path d="M9 12H4s.55-3.03 2-4c1.62-1.08 5 0 5 0"></path><path d="M12 15v5s3.03-.55 4-2c1.08-1.62 0-5 0-5"></path></svg>',
        users: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle><path d="M23 21v-2a4 4 0 0 0-3-3.87"></path><path d="M16 3.13a4 4 0 0 1 0 7.75"></path></svg>',
        user: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>',
        building: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg>',
        shield: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path></svg>',
        folder: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path></svg>',
        checkSquare: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 11 12 14 22 4"></polyline><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"></path></svg>',
        calendar: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>',
        barChart: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="20" x2="12" y2="10"></line><line x1="18" y1="20" x2="18" y2="4"></line><line x1="6" y1="20" x2="6" y2="16"></line></svg>',
        messageCircle: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z"></path></svg>',
        bell: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path><path d="M13.73 21a2 2 0 0 1-3.46 0"></path></svg>',
        box: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path><polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline><line x1="12" y1="22.08" x2="12" y2="12"></line></svg>',
        fileText: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line><polyline points="10 9 9 9 8 9"></polyline></svg>',
        refreshCw: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 4 23 10 17 10"></polyline><polyline points="1 20 1 14 7 14"></polyline><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path></svg>',
        settings: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="3"></circle><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09a1.65 1.65 0 0 0-1.08-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"></path></svg>',
        target: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><circle cx="12" cy="12" r="6"></circle><circle cx="12" cy="12" r="2"></circle></svg>',
        trendingUp: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 6 13.5 15.5 8.5 10.5 1 18"></polyline><polyline points="17 6 23 6 23 12"></polyline></svg>',
        hand: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path><path d="M12 2v2"></path><path d="M4.93 4.93l1.41 1.41"></path><path d="M19.07 4.93l-1.41 1.41"></path></svg>'
    };

    /* ==================== Role-Based Tour Steps ==================== */

    const tourSteps = {
        companyadmin: [
            {
                title: 'Welcome to ThinkBridge ERP!',
                desc: 'Your all-in-one platform for managing projects, teams, and products. Let\u2019s take a quick tour of what you can do as an Administrator.',
                svgIcon: 'rocket',
                features: []
            },
            {
                title: 'Team & User Management',
                desc: 'Manage your organization\u2019s workforce.',
                svgIcon: 'users',
                features: [
                    { svgIcon: 'user', cls: 'blue', title: 'Manage Users', desc: 'Add, edit, or deactivate team members and assign roles.' },
                    { svgIcon: 'building', cls: 'purple', title: 'Company Settings', desc: 'Configure your company profile and subscription.' },
                    { svgIcon: 'shield', cls: 'red', title: 'Role Assignment', desc: 'Assign Project Manager or Team Member roles to users.' }
                ]
            },
            {
                title: 'Projects & Tasks',
                desc: 'Create and oversee all projects across your company.',
                svgIcon: 'barChart',
                features: [
                    { svgIcon: 'folder', cls: 'blue', title: 'Projects', desc: 'Create projects, set timelines, and track progress.' },
                    { svgIcon: 'checkSquare', cls: 'green', title: 'Tasks', desc: 'Break projects into tasks and assign them to team members.' },
                    { svgIcon: 'calendar', cls: 'orange', title: 'Calendar', desc: 'View project milestones and deadlines on the calendar.' }
                ]
            },
            {
                title: 'Reports & Collaboration',
                desc: 'Stay informed and connected with your team.',
                svgIcon: 'trendingUp',
                features: [
                    { svgIcon: 'barChart', cls: 'purple', title: 'Reports', desc: 'Generate project and task reports with visual analytics.' },
                    { svgIcon: 'messageCircle', cls: 'teal', title: 'Activity Feed', desc: 'Share updates, announcements, and collaborate with your team.' },
                    { svgIcon: 'bell', cls: 'orange', title: 'Notifications', desc: 'Stay updated with real-time alerts on important changes.' }
                ]
            }
        ],

        projectmanager: [
            {
                title: 'Welcome, Project Manager!',
                desc: 'You\u2019re in charge of projects, tasks, and your team. Here\u2019s a quick overview of your tools.',
                svgIcon: 'target',
                features: []
            },
            {
                title: 'Project Management',
                desc: 'Create and manage your projects end-to-end.',
                svgIcon: 'folder',
                features: [
                    { svgIcon: 'folder', cls: 'blue', title: 'Projects', desc: 'Create projects, set milestones, and monitor progress.' },
                    { svgIcon: 'checkSquare', cls: 'green', title: 'Task Board', desc: 'Create tasks, assign team members, and track status.' },
                    { svgIcon: 'calendar', cls: 'orange', title: 'Calendar', desc: 'Visualize deadlines and upcoming milestones.' }
                ]
            },
            {
                title: 'Product Lifecycle',
                desc: 'Manage products from ideation to launch.',
                svgIcon: 'refreshCw',
                features: [
                    { svgIcon: 'box', cls: 'purple', title: 'Product Pipeline', desc: 'Track products through lifecycle stages with kanban view.' },
                    { svgIcon: 'fileText', cls: 'blue', title: 'Knowledge Base', desc: 'Create and manage documentation for your team.' },
                    { svgIcon: 'trendingUp', cls: 'green', title: 'Reports', desc: 'View detailed project and task analytics.' }
                ]
            },
            {
                title: 'Team & Collaboration',
                desc: 'Work effectively with your team.',
                svgIcon: 'users',
                features: [
                    { svgIcon: 'users', cls: 'blue', title: 'Team Members', desc: 'View your team and monitor workload distribution.' },
                    { svgIcon: 'messageCircle', cls: 'teal', title: 'Activity Feed', desc: 'Post updates and collaborate with your team in real time.' },
                    { svgIcon: 'bell', cls: 'orange', title: 'Notifications', desc: 'Get alerted on task updates, mentions, and deadlines.' }
                ]
            }
        ],

        teammember: [
            {
                title: 'Welcome to ThinkBridge ERP!',
                desc: 'Here\u2019s a quick guide to help you get started and stay productive.',
                svgIcon: 'rocket',
                features: []
            },
            {
                title: 'Your Tasks',
                desc: 'Stay on top of your assigned work.',
                svgIcon: 'checkSquare',
                features: [
                    { svgIcon: 'checkSquare', cls: 'green', title: 'My Tasks', desc: 'View all tasks assigned to you, update status, and track deadlines.' },
                    { svgIcon: 'calendar', cls: 'orange', title: 'Calendar', desc: 'See your upcoming deadlines and events at a glance.' },
                    { svgIcon: 'bell', cls: 'red', title: 'Notifications', desc: 'Receive alerts when tasks are assigned or due dates approach.' }
                ]
            },
            {
                title: 'Projects & Products',
                desc: 'View the projects and products you\u2019re part of.',
                svgIcon: 'folder',
                features: [
                    { svgIcon: 'folder', cls: 'blue', title: 'My Projects', desc: 'Access projects you\u2019re assigned to and track their progress.' },
                    { svgIcon: 'refreshCw', cls: 'purple', title: 'Product Lifecycle', desc: 'View product stages and related work items.' },
                    { svgIcon: 'fileText', cls: 'teal', title: 'Knowledge Base', desc: 'Browse documentation and resources shared by your team.' }
                ]
            },
            {
                title: 'Collaboration',
                desc: 'Stay connected with your team.',
                svgIcon: 'messageCircle',
                features: [
                    { svgIcon: 'messageCircle', cls: 'teal', title: 'Activity Feed', desc: 'Share updates, comment on posts, and stay in the loop.' },
                    { svgIcon: 'settings', cls: 'blue', title: 'Settings', desc: 'Update your profile, change password, and customize preferences.' }
                ]
            }
        ]
    };

    /* ==================== FAQ Content ==================== */

    const faqItems = [
        { q: 'How do I change my password?', a: 'Go to Settings from the sidebar, then click the "Change Password" tab. Enter your current password and set a new one.' },
        { q: 'How do I update my profile?', a: 'Navigate to Settings from the sidebar. You can update your name, phone number, and avatar color from the Profile tab.' },
        { q: 'How are tasks assigned to me?', a: 'Project Managers create tasks and assign team members. You\u2019ll receive a notification when a new task is assigned to you.' },
        { q: 'How do I update a task\u2019s status?', a: 'Open My Tasks from the sidebar. Use the status dropdown on any task card to quickly change its status (To Do, In Progress, In Review, Completed).' },
        { q: 'What is the Product Lifecycle module?', a: 'The Product Lifecycle module lets you track products through stages like Concept, Development, Testing, and Launch using a visual kanban board.' },
        { q: 'How do I use the Activity Feed?', a: 'Go to Activity from the sidebar. You can create posts, comment on others\u2019 updates, and mention team members using @.' },
        { q: 'How do notifications work?', a: 'Click the bell icon in the top bar to view recent notifications. You\u2019ll be notified about task assignments, mentions, and project updates.' },
        { q: 'Can I reopen this guide later?', a: 'Yes! Click the book icon next to the notification bell in the top bar, then select "Restart System Tour".' }
    ];

    /* ==================== Tour State ==================== */

    let currentStep = 0;
    let currentRole = 'teammember';
    let tourActive = false;

    /* ==================== Initialization ==================== */

    document.addEventListener('DOMContentLoaded', function () {
        injectTourHTML();
        injectHelpPanelHTML();

        // Auto-show onboarding only once, only on dashboard pages
        const path = window.location.pathname.toLowerCase();
        const isDashboard = path === '/web/dashboard'
            || path === '/web/projectmanagerdashboard'
            || path === '/web/teammemberdashboard';

        if (isDashboard) {
            const onboardingDone = document.getElementById('topbar-onboarding-done');
            const roleEl = document.getElementById('topbar-user-role');
            if (onboardingDone && roleEl) {
                currentRole = roleEl.value || 'teammember';
                const alreadyDone = onboardingDone.value === 'True' || localStorage.getItem('tb_onboarding_done') === '1';
                if (currentRole !== 'superadmin' && !alreadyDone) {
                    setTimeout(function () { startTour(); }, 600);
                }
            }
        }
    });

    /* ==================== Tour HTML Injection ==================== */

    function injectTourHTML() {
        const overlay = document.createElement('div');
        overlay.className = 'onboarding-overlay';
        overlay.id = 'onboarding-overlay';
        overlay.innerHTML = `
            <div class="onboarding-card" id="onboarding-card">
                <div class="onboarding-header" id="ob-header"></div>
                <div class="onboarding-body" id="ob-body"></div>
                <div class="onboarding-footer" id="ob-footer"></div>
            </div>`;
        document.body.appendChild(overlay);
    }

    function injectHelpPanelHTML() {
        // Overlay
        const panelOverlay = document.createElement('div');
        panelOverlay.className = 'help-panel-overlay';
        panelOverlay.id = 'help-panel-overlay';
        panelOverlay.onclick = function () { closeHelpPanel(); };
        document.body.appendChild(panelOverlay);

        // Panel
        const panel = document.createElement('div');
        panel.className = 'help-panel';
        panel.id = 'help-panel';

        const faqHTML = faqItems.map((f, i) => `
            <div class="faq-item" id="faq-item-${i}">
                <button class="faq-question" onclick="toggleFaq(${i})">
                    <span>${escHTML(f.q)}</span>
                    <svg class="faq-chevron" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"></polyline></svg>
                </button>
                <div class="faq-answer">${escHTML(f.a)}</div>
            </div>`).join('');

        panel.innerHTML = `
            <div class="help-panel-header">
                <h2>Help &amp; Guide</h2>
                <button class="help-panel-close" onclick="closeHelpPanel()">&times;</button>
            </div>
            <div class="help-panel-body">
                <button class="help-tour-btn" onclick="restartTourFromHelp()">
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><polygon points="10 8 16 12 10 16 10 8"></polygon></svg>
                    Restart System Tour
                </button>

                <div class="help-section-title">Frequently Asked Questions</div>
                <div class="faq-list">${faqHTML}</div>
            </div>`;
        document.body.appendChild(panel);
    }

    /* ==================== Tour Rendering ==================== */

    function renderStep() {
        const steps = tourSteps[currentRole] || tourSteps.teammember;
        const step = steps[currentStep];
        const total = steps.length;

        // Header
        document.getElementById('ob-header').innerHTML = `
            <div class="ob-icon">${svgIcons[step.svgIcon] || svgIcons.rocket}</div>
            <div class="ob-step-label">Step ${currentStep + 1} of ${total}</div>
            <h2>${escHTML(step.title)}</h2>
            <p>${escHTML(step.desc)}</p>`;

        // Body (features list)
        if (step.features.length > 0) {
            document.getElementById('ob-body').innerHTML = `
                <ul class="ob-features-list">
                    ${step.features.map(f => `
                        <li>
                            <div class="ob-feat-icon ${f.cls}">${svgIcons[f.svgIcon] || ''}</div>
                            <div class="ob-feat-text">
                                <h4>${escHTML(f.title)}</h4>
                                <p>${escHTML(f.desc)}</p>
                            </div>
                        </li>`).join('')}
                </ul>`;
        } else {
            document.getElementById('ob-body').innerHTML = '';
        }

        // Footer (dots + buttons)
        const dots = Array.from({ length: total }, (_, i) =>
            `<div class="ob-dot ${i === currentStep ? 'active' : ''}"></div>`).join('');

        const isFirst = currentStep === 0;
        const isLast = currentStep === total - 1;

        let btns = `<button class="ob-btn ob-btn-skip" onclick="skipTour()">Skip</button>`;
        if (!isFirst) btns += `<button class="ob-btn ob-btn-prev" onclick="prevStep()">Back</button>`;
        if (isLast) {
            btns += `<button class="ob-btn ob-btn-finish" onclick="finishTour()">Get Started</button>`;
        } else {
            btns += `<button class="ob-btn ob-btn-next" onclick="nextStep()">Next</button>`;
        }

        document.getElementById('ob-footer').innerHTML = `
            <div class="ob-dots">${dots}</div>
            <div class="ob-footer-btns">${btns}</div>`;
    }

    /* ==================== Tour Navigation ==================== */

    function startTour() {
        const roleEl = document.getElementById('topbar-user-role');
        currentRole = roleEl ? roleEl.value : 'teammember';
        currentStep = 0;
        tourActive = true;
        renderStep();
        document.getElementById('onboarding-overlay').classList.add('active');
    }

    function nextStep() {
        const steps = tourSteps[currentRole] || tourSteps.teammember;
        if (currentStep < steps.length - 1) {
            currentStep++;
            renderStep();
        }
    }

    function prevStep() {
        if (currentStep > 0) {
            currentStep--;
            renderStep();
        }
    }

    function skipTour() {
        closeTour();
        markOnboardingComplete();
    }

    function finishTour() {
        closeTour();
        markOnboardingComplete();
        if (typeof showToast === 'function') {
            showToast('You\u2019re all set! Explore the system at your own pace.', 'success');
        }
    }

    function closeTour() {
        tourActive = false;
        document.getElementById('onboarding-overlay').classList.remove('active');
    }

    function markOnboardingComplete() {
        localStorage.setItem('tb_onboarding_done', '1');
        fetch('/api/settings/onboarding/complete', {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json' }
        }).catch(function (e) { console.error('Failed to mark onboarding complete:', e); });
    }

    /* ==================== Help Panel ==================== */

    function openHelpPanel() {
        document.getElementById('help-panel-overlay').classList.add('active');
        document.getElementById('help-panel').classList.add('open');
    }

    function closeHelpPanel() {
        document.getElementById('help-panel').classList.remove('open');
        document.getElementById('help-panel-overlay').classList.remove('active');
    }

    function restartTourFromHelp() {
        closeHelpPanel();
        setTimeout(function () { startTour(); }, 200);
    }

    function toggleFaq(index) {
        var el = document.getElementById('faq-item-' + index);
        if (el) el.classList.toggle('open');
    }

    /* ==================== Utility ==================== */

    function escHTML(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    /* ==================== Expose to Global Scope ==================== */

    window.startTour = startTour;
    window.openHelpPanel = openHelpPanel;
    window.closeHelpPanel = closeHelpPanel;
    window.restartTourFromHelp = restartTourFromHelp;
    window.toggleFaq = toggleFaq;
    window.nextStep = nextStep;
    window.prevStep = prevStep;
    window.skipTour = skipTour;
    window.finishTour = finishTour;

})();
