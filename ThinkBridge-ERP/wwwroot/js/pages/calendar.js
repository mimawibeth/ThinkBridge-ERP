/* ==========================================
   Calendar Module - Page Script
   ========================================== */

(function () {
    'use strict';

    // ─── State ───────────────────────────────────

    const userRole = document.getElementById('current-user-role')?.value || 'teammember';
    const canManage = userRole === 'companyadmin' || userRole === 'projectmanager';

    let currentDate = new Date();
    let currentView = 'month'; // month | week | list
    let events = [];
    let projects = [];
    let selectedProjectId = '';
    let currentEventId = null;

    // ─── API Helpers ─────────────────────────────

    async function apiGet(url) {
        const res = await fetch(url, { credentials: 'same-origin' });
        if (!res.ok) {
            try { return await res.json(); } catch { return { success: false, message: 'Request failed (' + res.status + ')' }; }
        }
        return res.json();
    }

    async function apiPost(url, body) {
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin',
            body: JSON.stringify(body)
        });
        if (!res.ok) {
            try { return await res.json(); } catch { return { success: false, message: 'Request failed (' + res.status + ')' }; }
        }
        return res.json();
    }

    async function apiPut(url, body) {
        const res = await fetch(url, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin',
            body: JSON.stringify(body)
        });
        if (!res.ok) {
            try { return await res.json(); } catch { return { success: false, message: 'Request failed (' + res.status + ')' }; }
        }
        return res.json();
    }

    async function apiDelete(url) {
        const res = await fetch(url, {
            method: 'DELETE',
            credentials: 'same-origin'
        });
        if (!res.ok) {
            try { return await res.json(); } catch { return { success: false, message: 'Request failed (' + res.status + ')' }; }
        }
        return res.json();
    }

    // ─── Initialization ─────────────────────────

    async function init() {
        bindEvents();
        await loadProjects();
        await loadEvents();
        renderCalendar();
    }

    function bindEvents() {
        document.getElementById('cal-prev')?.addEventListener('click', () => navigate(-1));
        document.getElementById('cal-next')?.addEventListener('click', () => navigate(1));
        document.getElementById('cal-today')?.addEventListener('click', goToToday);
        document.getElementById('cal-project-filter')?.addEventListener('change', onProjectFilterChange);

        document.querySelectorAll('.view-btn').forEach(btn => {
            btn.addEventListener('click', () => switchView(btn.dataset.view));
        });

        if (canManage) {
            document.getElementById('cal-add-event-btn')?.addEventListener('click', () => openEventForm());
        }

        // Color swatches
        document.querySelectorAll('.color-swatch').forEach(swatch => {
            swatch.addEventListener('click', () => {
                document.querySelectorAll('.color-swatch').forEach(s => s.classList.remove('selected'));
                swatch.classList.add('selected');
                document.getElementById('event-color').value = swatch.dataset.color;
            });
        });


    }

    // ─── Navigation ──────────────────────────────

    function navigate(delta) {
        if (currentView === 'month') {
            currentDate.setMonth(currentDate.getMonth() + delta);
        } else if (currentView === 'week') {
            currentDate.setDate(currentDate.getDate() + (delta * 7));
        } else {
            currentDate.setMonth(currentDate.getMonth() + delta);
        }
        loadEvents().then(() => renderCalendar());
    }

    function goToToday() {
        currentDate = new Date();
        loadEvents().then(() => renderCalendar());
    }

    function onProjectFilterChange() {
        selectedProjectId = document.getElementById('cal-project-filter').value;
        loadEvents().then(() => renderCalendar());
    }

    function switchView(view) {
        currentView = view;
        document.querySelectorAll('.view-btn').forEach(b => b.classList.remove('active'));
        document.querySelector(`.view-btn[data-view="${view}"]`)?.classList.add('active');

        document.getElementById('cal-month-view')?.classList.toggle('hidden', view !== 'month');
        document.getElementById('cal-week-view')?.classList.toggle('hidden', view !== 'week');
        document.getElementById('cal-list-view')?.classList.toggle('hidden', view !== 'list');

        renderCalendar();
    }

    // ─── Data Loading ────────────────────────────

    async function loadProjects() {
        try {
            const result = await apiGet('/api/calendar/projects');
            if (result.success) {
                projects = result.data || [];
                renderProjectFilters();
            }
        } catch (e) {
            console.error('Failed to load projects:', e);
        }
    }

    function renderProjectFilters() {
        const filterSelect = document.getElementById('cal-project-filter');
        const formSelect = document.getElementById('event-project');

        const filterOpts = '<option value="">All Projects</option>' +
            projects.map(p => `<option value="${p.projectId}">${escapeHtml(p.projectName)}</option>`).join('');

        const formOpts = '<option value="">No Project</option>' +
            projects.map(p => `<option value="${p.projectId}">${escapeHtml(p.projectName)}</option>`).join('');

        if (filterSelect) filterSelect.innerHTML = filterOpts;
        if (formSelect) formSelect.innerHTML = formOpts;
    }

    async function loadEvents() {
        const range = getDateRange();
        try {
            showLoading(true);
            let url = `/api/calendar/events/range?start=${range.start.toISOString()}&end=${range.end.toISOString()}`;
            if (selectedProjectId) url += `&projectId=${selectedProjectId}`;

            const result = await apiGet(url);
            if (result.success) {
                events = result.data || [];
            }
        } catch (e) {
            console.error('Failed to load events:', e);
        } finally {
            showLoading(false);
        }
    }

    function getDateRange() {
        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();

        if (currentView === 'month' || currentView === 'list') {
            const start = new Date(year, month, 1);
            start.setDate(start.getDate() - start.getDay()); // Go to Sunday
            const end = new Date(year, month + 1, 0);
            end.setDate(end.getDate() + (6 - end.getDay())); // Go to Saturday
            end.setHours(23, 59, 59, 999);
            return { start, end };
        } else {
            // Week view
            const dayOfWeek = currentDate.getDay();
            const start = new Date(currentDate);
            start.setDate(start.getDate() - dayOfWeek);
            start.setHours(0, 0, 0, 0);
            const end = new Date(start);
            end.setDate(end.getDate() + 6);
            end.setHours(23, 59, 59, 999);
            return { start, end };
        }
    }

    // ─── Calendar Rendering ──────────────────────

    function renderCalendar() {
        updateTitle();
        if (currentView === 'month') renderMonthView();
        else if (currentView === 'week') renderWeekView();
        else renderListView();
    }

    function updateTitle() {
        const title = document.getElementById('cal-title');
        if (!title) return;

        if (currentView === 'week') {
            const range = getDateRange();
            const opts = { month: 'short', day: 'numeric' };
            title.textContent = `${range.start.toLocaleDateString(undefined, opts)} – ${range.end.toLocaleDateString(undefined, opts)}, ${range.end.getFullYear()}`;
        } else {
            title.textContent = currentDate.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
        }
    }

    // ─── Month View ──────────────────────────────

    function renderMonthView() {
        const grid = document.getElementById('cal-grid');
        if (!grid) return;

        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();
        const firstDay = new Date(year, month, 1);
        const lastDay = new Date(year, month + 1, 0);

        const startDate = new Date(firstDay);
        startDate.setDate(startDate.getDate() - firstDay.getDay());

        let html = '';
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        const totalCells = Math.ceil((lastDay.getDate() + firstDay.getDay()) / 7) * 7;

        for (let i = 0; i < totalCells; i++) {
            const cellDate = new Date(startDate);
            cellDate.setDate(startDate.getDate() + i);

            const isOtherMonth = cellDate.getMonth() !== month;
            const isToday = cellDate.toDateString() === today.toDateString();
            const dayEvents = getEventsForDate(cellDate);
            const maxVisible = 3;

            let classes = 'calendar-day';
            if (isOtherMonth) classes += ' other-month';
            if (isToday) classes += ' today';

            const dateStr = cellDate.toISOString().split('T')[0];

            html += `<div class="${classes}" data-date="${dateStr}" onclick="calDayClick('${dateStr}')">`;
            html += `<span class="day-number">${cellDate.getDate()}</span>`;
            html += '<div class="day-events">';

            dayEvents.slice(0, maxVisible).forEach(evt => {
                const color = evt.color || '#0B4F6C';
                const time = `<span class="event-time">${formatTime(new Date(evt.startDate))}</span>`;
                html += `<div class="day-event" style="background:${escapeHtml(color)};" onclick="event.stopPropagation(); calViewEvent(${evt.eventId})" title="${escapeHtml(evt.title)}">${time}${escapeHtml(evt.title)}</div>`;
            });

            if (dayEvents.length > maxVisible) {
                html += `<div class="day-more-events" onclick="event.stopPropagation(); calDayClick('${dateStr}')">+${dayEvents.length - maxVisible} more</div>`;
            }

            html += '</div></div>';
        }

        grid.innerHTML = html;
    }

    // ─── Week View ───────────────────────────────

    function renderWeekView() {
        const headerEl = document.getElementById('week-header');
        const bodyEl = document.getElementById('week-body');
        if (!headerEl || !bodyEl) return;

        const range = getDateRange();
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

        // Header
        let headerHtml = '<div class="week-header-cell"></div>';
        for (let i = 0; i < 7; i++) {
            const d = new Date(range.start);
            d.setDate(d.getDate() + i);
            const isToday = d.toDateString() === today.toDateString();
            headerHtml += `<div class="week-header-cell${isToday ? ' today-col' : ''}">
                <span class="week-day-name">${dayNames[d.getDay()]}</span>
                <span class="week-day-number">${d.getDate()}</span>
            </div>`;
        }
        headerEl.innerHTML = headerHtml;

        // Body - 24 hour slots
        let bodyHtml = '';
        for (let hour = 0; hour < 24; hour++) {
            const label = hour === 0 ? '12 AM' : hour < 12 ? `${hour} AM` : hour === 12 ? '12 PM' : `${hour - 12} PM`;
            bodyHtml += `<div class="week-time-label">${label}</div>`;

            for (let day = 0; day < 7; day++) {
                const d = new Date(range.start);
                d.setDate(d.getDate() + day);
                d.setHours(hour, 0, 0, 0);

                const hourEvents = getEventsForHour(d);
                let cellHtml = '';
                hourEvents.forEach(evt => {
                    const color = evt.color || '#0B4F6C';
                    cellHtml += `<div class="day-event" style="background:${escapeHtml(color)};" onclick="calViewEvent(${evt.eventId})" title="${escapeHtml(evt.title)}">${escapeHtml(evt.title)}</div>`;
                });

                bodyHtml += `<div class="week-cell">${cellHtml}</div>`;
            }
        }
        bodyEl.innerHTML = bodyHtml;
    }

    // ─── List View ───────────────────────────────

    function renderListView() {
        const container = document.getElementById('cal-list-container');
        const emptyEl = document.getElementById('cal-empty');
        if (!container) return;

        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();
        const monthEvents = events.filter(e => {
            const start = new Date(e.startDate);
            return start.getFullYear() === year && start.getMonth() === month;
        }).sort((a, b) => new Date(a.startDate) - new Date(b.startDate));

        if (monthEvents.length === 0) {
            container.innerHTML = '';
            emptyEl?.classList.remove('hidden');
            return;
        }

        emptyEl?.classList.add('hidden');

        // Group by date
        const groups = {};
        monthEvents.forEach(evt => {
            const dateKey = new Date(evt.startDate).toLocaleDateString(undefined, { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
            if (!groups[dateKey]) groups[dateKey] = [];
            groups[dateKey].push(evt);
        });

        let html = '';
        Object.entries(groups).forEach(([dateLabel, evts]) => {
            html += `<div class="list-date-group">`;
            html += `<div class="list-date-header">${escapeHtml(dateLabel)}</div>`;
            evts.forEach(evt => {
                const color = evt.color || '#0B4F6C';
                const timeStr = `${formatTime(new Date(evt.startDate))} – ${formatTime(new Date(evt.endDate))}`;
                html += `<div class="list-event-item" onclick="calViewEvent(${evt.eventId})">`;
                html += `<div class="list-event-color" style="background:${escapeHtml(color)};"></div>`;
                html += `<div class="list-event-time">${timeStr}</div>`;
                html += `<div class="list-event-details">`;
                html += `<div class="list-event-title">${escapeHtml(evt.title)}</div>`;
                html += `<div class="list-event-meta">`;
                if (evt.projectName) {
                    html += `<span class="list-event-meta-item"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path></svg>${escapeHtml(evt.projectName)}</span>`;
                }
                if (evt.location) {
                    html += `<span class="list-event-meta-item"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"></path><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"></path></svg>${escapeHtml(evt.location)}</span>`;
                }
                html += `<span class="list-event-meta-item"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>${escapeHtml(evt.creatorName)}</span>`;
                html += `</div></div>`;
                html += `</div>`;
            });
            html += `</div>`;
        });

        container.innerHTML = html;
    }

    // ─── Event Helpers ───────────────────────────

    function getEventsForDate(date) {
        const dateStart = new Date(date);
        dateStart.setHours(0, 0, 0, 0);
        const dateEnd = new Date(date);
        dateEnd.setHours(23, 59, 59, 999);

        return events.filter(e => {
            const eStart = new Date(e.startDate);
            const eEnd = new Date(e.endDate);
            return eStart <= dateEnd && eEnd >= dateStart;
        });
    }

    function getEventsForHour(dateTime) {
        const hourStart = new Date(dateTime);
        const hourEnd = new Date(dateTime);
        hourEnd.setHours(hourEnd.getHours() + 1);

        return events.filter(e => {
            const eStart = new Date(e.startDate);
            const eEnd = new Date(e.endDate);
            return eStart < hourEnd && eEnd > hourStart;
        });
    }

    // ─── View Event Detail ───────────────────────

    window.calViewEvent = async function (eventId) {
        currentEventId = eventId;
        try {
            const result = await apiGet(`/api/calendar/events/${eventId}`);
            if (!result.success) {
                showToast(result.message || 'Failed to load event.', 'error');
                return;
            }
            const evt = result.data;
            document.getElementById('event-detail-title').textContent = evt.title;

            const start = new Date(evt.startDate);
            const end = new Date(evt.endDate);
            const dateOpts = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
            const timeStr = `${start.toLocaleDateString(undefined, dateOpts)}, ${formatTime(start)} – ${formatTime(end)}`;

            const priorityClass = `priority-${evt.priority.toLowerCase()}`;
            const color = evt.color || '#0B4F6C';

            let html = '<div class="event-detail-content">';

            // Date/Time
            html += `<div class="event-detail-row">
                <svg class="detail-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
                <div class="detail-content"><div class="detail-label">Date & Time</div><div class="detail-value">${timeStr}</div></div>
            </div>`;

            // Color
            html += `<div class="event-detail-row">
                <svg class="detail-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle></svg>
                <div class="detail-content"><div class="detail-label">Color</div><div class="detail-value"><span class="event-color-badge" style="background:${escapeHtml(color)};"></span></div></div>
            </div>`;

            // Project
            if (evt.projectName) {
                html += `<div class="event-detail-row">
                    <svg class="detail-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path></svg>
                    <div class="detail-content"><div class="detail-label">Project</div><div class="detail-value">${escapeHtml(evt.projectName)}</div></div>
                </div>`;
            }

            // Meeting Link
            if (evt.location) {
                const linkHtml = evt.location.startsWith('http') ? `<a href="${escapeHtml(evt.location)}" target="_blank" rel="noopener noreferrer" style="color:var(--primary); text-decoration:underline;">${escapeHtml(evt.location)}</a>` : escapeHtml(evt.location);
                html += `<div class="event-detail-row">
                    <svg class="detail-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"></path><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"></path></svg>
                    <div class="detail-content"><div class="detail-label">Meeting Link</div><div class="detail-value">${linkHtml}</div></div>
                </div>`;
            }

            // Description
            if (evt.description) {
                html += `<div class="event-detail-row">
                    <svg class="detail-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline><line x1="16" y1="13" x2="8" y2="13"></line><line x1="16" y1="17" x2="8" y2="17"></line></svg>
                    <div class="detail-content"><div class="detail-label">Description</div><div class="detail-value">${escapeHtml(evt.description)}</div></div>
                </div>`;
            }

            // Creator
            html += `<div class="event-detail-row">
                <svg class="detail-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
                <div class="detail-content"><div class="detail-label">Created By</div><div class="detail-value">${escapeHtml(evt.creatorName)} · ${formatDateRelative(evt.createdAt)}</div></div>
            </div>`;

            html += '</div>';

            document.getElementById('event-detail-body').innerHTML = html;
            window.openModal('event-detail-modal');
        } catch (e) {
            console.error('Failed to view event:', e);
            showToast('Failed to load event details.', 'error');
        }
    };

    // ─── Day Click → List events or create ───────

    window.calDayClick = function (dateStr) {
        if (currentView !== 'month') return;
        // If canManage, open form with that date pre-filled
        if (canManage) {
            openEventForm(null, dateStr);
        } else {
            // Switch to list view for that month
            switchView('list');
        }
    };

    // ─── Open Event Form (Create/Edit) ───────────

    function openEventForm(eventId, prefillDate) {
        document.getElementById('event-form').reset();
        document.getElementById('event-form-id').value = '';
        document.getElementById('event-form-title').textContent = eventId ? 'Edit Event' : 'Add Event';
        document.getElementById('event-color').value = '#0B4F6C';

        // Reset color swatch selection
        document.querySelectorAll('.color-swatch').forEach(s => s.classList.remove('selected'));
        document.querySelector('.color-swatch[data-color="#0B4F6C"]')?.classList.add('selected');

        // Reset input types
        document.getElementById('event-start').type = 'datetime-local';
        document.getElementById('event-end').type = 'datetime-local';

        if (eventId) {
            // Edit mode - load existing event data
            const evt = events.find(e => e.eventId === eventId);
            if (evt) {
                document.getElementById('event-form-id').value = evt.eventId;
                document.getElementById('event-title').value = evt.title;
                document.getElementById('event-description').value = evt.description || '';
                document.getElementById('event-location').value = evt.location || '';
                document.getElementById('event-project').value = evt.projectId || '';
                document.getElementById('event-color').value = evt.color || '#0B4F6C';

                document.getElementById('event-start').value = toLocalDatetimeString(new Date(evt.startDate));
                document.getElementById('event-end').value = toLocalDatetimeString(new Date(evt.endDate));

                // Select color swatch
                document.querySelectorAll('.color-swatch').forEach(s => s.classList.remove('selected'));
                document.querySelector(`.color-swatch[data-color="${evt.color || '#0B4F6C'}"]`)?.classList.add('selected');
            }
        } else if (prefillDate) {
            // New event with prefilled date
            const d = new Date(prefillDate + 'T09:00:00');
            const dEnd = new Date(prefillDate + 'T10:00:00');
            document.getElementById('event-start').value = toLocalDatetimeString(d);
            document.getElementById('event-end').value = toLocalDatetimeString(dEnd);
        }

        // Attach real-time date validation listeners
        const startInput = document.getElementById('event-start');
        const endInput = document.getElementById('event-end');
        startInput.removeEventListener('change', validateEventDates);
        endInput.removeEventListener('change', validateEventDates);
        startInput.addEventListener('change', validateEventDates);
        endInput.addEventListener('change', validateEventDates);
        // Clear any previous error
        document.getElementById('event-date-error').style.display = 'none';
        endInput.style.removeProperty('border-color');

        window.openModal('event-form-modal');
    }

    function validateEventDates() {
        const startVal = document.getElementById('event-start').value;
        const endVal = document.getElementById('event-end').value;
        const errorEl = document.getElementById('event-date-error');
        const endInput = document.getElementById('event-end');

        // Update min attribute on end input
        if (startVal) {
            endInput.min = startVal;
        }

        if (startVal && endVal && new Date(endVal) < new Date(startVal)) {
            errorEl.style.display = 'block';
            endInput.style.borderColor = '#ef4444';
        } else {
            errorEl.style.display = 'none';
            endInput.style.removeProperty('border-color');
        }
    }

    // ─── Save Event ──────────────────────────────

    window.calSaveEvent = async function () {
        const eventId = document.getElementById('event-form-id').value;
        const title = document.getElementById('event-title').value.trim();
        let startVal = document.getElementById('event-start').value;
        let endVal = document.getElementById('event-end').value;

        if (!title) {
            showToast('Please enter an event title.', 'warning');
            return;
        }
        if (!startVal || !endVal) {
            showToast('Please enter start and end dates.', 'warning');
            return;
        }

        let startDate = new Date(startVal);
        let endDate = new Date(endVal);

        if (endDate < startDate) {
            const errorEl = document.getElementById('event-date-error');
            errorEl.style.display = 'block';
            document.getElementById('event-end').style.borderColor = '#ef4444';
            showToast('End date/time must be equal to or after the start date/time.', 'warning');
            return;
        }

        const body = {
            title: title,
            description: document.getElementById('event-description').value.trim() || null,
            startDate: startDate.toISOString(),
            endDate: endDate.toISOString(),
            allDay: false,
            location: document.getElementById('event-location').value.trim() || null,
            priority: 'Medium',
            color: document.getElementById('event-color').value || null,
            projectId: document.getElementById('event-project').value ? parseInt(document.getElementById('event-project').value, 10) : null
        };

        try {
            document.getElementById('event-save-btn').disabled = true;
            let result;
            if (eventId) {
                result = await apiPut(`/api/calendar/events/${eventId}`, body);
            } else {
                result = await apiPost('/api/calendar/events', body);
            }

            if (result.success) {
                showToast(eventId ? 'Event updated successfully.' : 'Event created successfully.', 'success');
                window.closeModal('event-form-modal');
                await loadEvents();
                renderCalendar();
            } else {
                showToast(result.message || 'Failed to save event.', 'error');
            }
        } catch (e) {
            console.error('Failed to save event:', e);
            showToast('An error occurred while saving.', 'error');
        } finally {
            document.getElementById('event-save-btn').disabled = false;
        }
    };

    // ─── Edit Event ──────────────────────────────

    window.calEditEvent = function () {
        if (!currentEventId) return;
        window.closeModal('event-detail-modal');
        setTimeout(() => openEventForm(currentEventId), 200);
    };

    // ─── Delete Event ────────────────────────────

    window.calDeleteEvent = async function () {
        if (!currentEventId) return;
        if (!confirm('Are you sure you want to delete this event?')) return;

        try {
            const result = await apiDelete(`/api/calendar/events/${currentEventId}`);
            if (result.success) {
                showToast('Event deleted successfully.', 'success');
                window.closeModal('event-detail-modal');
                await loadEvents();
                renderCalendar();
            } else {
                showToast(result.message || 'Failed to delete event.', 'error');
            }
        } catch (e) {
            console.error('Failed to delete event:', e);
            showToast('An error occurred while deleting.', 'error');
        }
    };

    // ─── Utility Functions ───────────────────────

    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function toUtcDate(dateStr) {
        if (!dateStr) return new Date();
        const s = String(dateStr);
        return new Date(s.endsWith('Z') || s.includes('+') ? s : s + 'Z');
    }

    function formatTime(date) {
        return date.toLocaleTimeString('en-US', { timeZone: 'Asia/Manila', hour: 'numeric', minute: '2-digit', hour12: true });
    }

    function formatDateRelative(dateStr) {
        const d = toUtcDate(dateStr);
        const diffMs = Date.now() - d.getTime();
        const diffMins = Math.floor(diffMs / 60000);
        if (diffMins < 1) return 'just now';
        if (diffMins < 60) return `${diffMins}m ago`;
        const diffHrs = Math.floor(diffMins / 60);
        if (diffHrs < 24) return `${diffHrs}h ago`;
        const diffDays = Math.floor(diffHrs / 24);
        if (diffDays < 7) return `${diffDays}d ago`;
        return d.toLocaleDateString('en-US', { timeZone: 'Asia/Manila' });
    }

    function toLocalDatetimeString(date) {
        const pad = n => n.toString().padStart(2, '0');
        return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
    }

    function showLoading(show) {
        document.getElementById('cal-loading')?.classList.toggle('hidden', !show);
    }

    function showToast(message, type) {
        if (typeof window.showToast === 'function') {
            window.showToast(message, type);
        } else {
            console.log(`[${type}] ${message}`);
        }
    }

    // ─── Init ────────────────────────────────────

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();
