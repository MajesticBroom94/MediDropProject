(() => {
    const form = document.getElementById('scheduleForm');
    if (!form) return;

    const toMinutes = value => {
        if (!value || !value.includes(':')) return null;
        const [hours, minutes] = value.split(':').map(Number);
        if (Number.isNaN(hours) || Number.isNaN(minutes)) return null;
        return hours * 60 + minutes;
    };

    const parseLocalDate = value => {
        if (!value) return null;
        const [year, month, day] = value.split('-').map(Number);
        if (!year || !month || !day) return null;
        return new Date(year, month - 1, day, 0, 0, 0, 0);
    };

    const startDateInput = document.getElementById('scheduleStartDate');
    const maxMedicationLines = Number(form.dataset.maxMedicationLines || 3);

    const getStartDate = () => {
        const value = startDateInput?.value || form.dataset.startDate;
        return parseLocalDate(value);
    };


    // Existing schedules open in a clear read-only summary first. The full
    // editor is revealed only when the patient deliberately chooses Edit.
    const editToggle = document.getElementById('scheduleEditToggle');
    const editShell = document.getElementById('scheduleEditShell');

    editToggle?.addEventListener('click', () => {
        const opening = editShell?.classList.contains('is-collapsed') ?? false;
        editShell?.classList.toggle('is-collapsed', !opening);
        editToggle.setAttribute('aria-expanded', opening ? 'true' : 'false');
        editToggle.innerHTML = opening
            ? '<i class="fa-solid fa-xmark"></i> <text>&nbsp; Close editor</text>'
            : '<i class="fa-solid fa-pen"></i> <text>&nbsp; Edit schedule</text>';

        if (opening) {
            editShell?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    });

    const periodMinutes = periodIndex => {
        const ids = ['morningTime', 'middayTime', 'eveningTime'];
        return toMinutes(document.getElementById(ids[periodIndex])?.value);
    };

    // Day tabs ---------------------------------------------------------------
    const dayButtons = Array.from(document.querySelectorAll('.schedule-day[data-day]'));
    const dayPanels = Array.from(document.querySelectorAll('.schedule-editor-day[data-day-panel]'));
    const previousScheduleDay = document.getElementById('previousScheduleDay');
    const nextScheduleDay = document.getElementById('nextScheduleDay');
    const mobileCurrentDay = document.getElementById('mobileCurrentDay');
    const mobileDayPosition = document.getElementById('mobileDayPosition');

    let activeDayIndex = Math.max(0, dayButtons.findIndex(button => button.classList.contains('active')));

    const updateMobileDayNav = () => {
        const button = dayButtons[activeDayIndex];
        if (!button) return;

        const dayName = button.childNodes[0]?.textContent?.trim() || `Day ${activeDayIndex + 1}`;
        const dayNumber = button.querySelector('span')?.textContent?.trim() || '';

        if (mobileCurrentDay) {
            mobileCurrentDay.textContent = `${dayName} ${dayNumber}`.trim();
        }

        if (mobileDayPosition) {
            mobileDayPosition.textContent = `Day ${activeDayIndex + 1} of ${dayButtons.length}`;
        }

        if (previousScheduleDay) previousScheduleDay.disabled = activeDayIndex === 0;
        if (nextScheduleDay) nextScheduleDay.disabled = activeDayIndex === dayButtons.length - 1;
    };

    const activateDay = (index, scrollOnMobile = false) => {
        if (index < 0 || index >= dayButtons.length) return;

        activeDayIndex = index;
        const selectedButton = dayButtons[index];
        const day = selectedButton.dataset.day;

        dayButtons.forEach((item, itemIndex) => {
            const active = itemIndex === index;
            item.classList.toggle('active', active);
            item.setAttribute('aria-selected', active ? 'true' : 'false');
        });

        dayPanels.forEach(panel => {
            panel.classList.toggle('active', panel.dataset.dayPanel === day);
        });

        updateMobileDayNav();

        if (scrollOnMobile && window.matchMedia('(max-width: 700px)').matches) {
            const activePanel = dayPanels.find(panel => panel.dataset.dayPanel === day);
            activePanel?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    };

    dayButtons.forEach((button, index) => {
        button.addEventListener('click', () => activateDay(index, true));
    });

    previousScheduleDay?.addEventListener('click', () => activateDay(activeDayIndex - 1, true));
    nextScheduleDay?.addEventListener('click', () => activateDay(activeDayIndex + 1, true));

    updateMobileDayNav();

    const refreshSlotCount = slot => {
        const badge = slot.querySelector('.slot-count');
        if (!badge) return;

        const committed = slot.querySelectorAll('.committed-row').length;
        const selectedDrafts = Array.from(slot.querySelectorAll('.draft-row .schedule-medication-select'))
            .filter(select => Number(select.value) > 0)
            .length;

        badge.textContent = `${committed + selectedDrafts} / ${maxMedicationLines} used`;
    };

    const setDraftRowState = select => {
        const row = select.closest('.draft-row');
        if (!row) return;

        const qty = row.querySelector('.schedule-quantity-input');
        const selected = Number(select.value) > 0;

        row.classList.toggle('has-medication', selected);

        if (qty) {
            if (selected && (!qty.value || Number(qty.value) < 1)) {
                qty.value = '1';
            }

            if (!selected) {
                qty.value = '';
            }
        }

        const slot = row.closest('.schedule-slot-editor');
        if (slot) refreshSlotCount(slot);
    };

    document.querySelectorAll('.schedule-medication-select').forEach(select => {
        select.addEventListener('change', () => setDraftRowState(select));
        setDraftRowState(select);
    });

    // Date/time availability -------------------------------------------------
    const refreshDateUi = () => {
        const start = getStartDate();
        if (!start) return;

        if (startDateInput) {
            form.dataset.startDate = startDateInput.value;
        }

        document.querySelectorAll('.schedule-day[data-day-offset]').forEach(button => {
            const offset = Number(button.dataset.dayOffset || 0);
            const date = new Date(start);
            date.setDate(start.getDate() + offset);

            const weekday = date.toLocaleDateString(undefined, { weekday: 'short' });
            button.innerHTML = `${weekday}<span>${date.getDate()}</span>`;
        });

        updateMobileDayNav();

        document.querySelectorAll('[data-day-date]').forEach(label => {
            const offset = Number(label.dataset.dayDate || 0);
            const date = new Date(start);
            date.setDate(start.getDate() + offset);
            label.textContent = date.toLocaleDateString(undefined, {
                day: '2-digit',
                month: 'long',
                year: 'numeric'
            });
        });

        document.querySelectorAll('[data-day-name]').forEach(label => {
            const offset = Number(label.dataset.dayName || 0);
            const date = new Date(start);
            date.setDate(start.getDate() + offset);
            label.textContent = date.toLocaleDateString(undefined, { weekday: 'long' });
        });

        const now = new Date();

        document.querySelectorAll('.schedule-slot-editor[data-day-offset][data-period-index]').forEach(slot => {
            const offset = Number(slot.dataset.dayOffset || 0);
            const period = Number(slot.dataset.periodIndex || 0);
            const minutes = periodMinutes(period);
            if (minutes == null) return;

            const scheduled = new Date(start);
            scheduled.setDate(start.getDate() + offset);
            scheduled.setHours(Math.floor(minutes / 60), minutes % 60, 0, 0);

            const passed = scheduled <= now;
            const serverEditable = slot.dataset.serverEditable === 'true';
            const hasCommitted = slot.dataset.hasCommitted === 'true';

            // A historical committed slot stays read-only without showing the noisy
            // "time passed" warning. The warning is for an otherwise-empty slot that
            // can no longer be loaded for the chosen day/time.
            const showPassedWarning = form.dataset.scheduleMode === 'create' && passed && !hasCommitted;
            slot.classList.toggle('slot-time-passed', showPassedWarning);

            const warning = slot.querySelector('.slot-time-passed-message');
            if (warning) warning.style.display = showPassedWarning ? 'flex' : 'none';

            slot.querySelectorAll('.draft-row .schedule-item-control').forEach(control => {
                control.disabled = !serverEditable || passed || slot.classList.contains('slot-cancelled');
            });

            const cancelButton = slot.querySelector('.cancel-slot');
            if (cancelButton) {
                cancelButton.disabled = passed || !serverEditable;
            }
        });
    };

    // Cancellation -----------------------------------------------------------
    document.addEventListener('click', event => {
        const cancel = event.target.closest('.cancel-slot');
        if (!cancel) return;

        const slot = cancel.closest('.schedule-slot-editor');
        if (!slot || cancel.disabled) return;

        const cancelled = slot.classList.toggle('slot-cancelled');
        const hidden = slot.querySelector('.cancelled-input');
        if (hidden) hidden.value = cancelled ? 'true' : 'false';

        cancel.innerHTML = cancelled
            ? '<i class="fa-solid fa-arrow-rotate-left"></i> Restore dispense slot'
            : '<i class="fa-solid fa-ban"></i> Cancel dispense slot';

        slot.querySelectorAll('.draft-row .schedule-item-control').forEach(control => {
            control.disabled = cancelled;
        });
    });

    const bindTime = (inputId, key) => {
        const input = document.getElementById(inputId);
        if (!input) return;

        input.addEventListener('input', () => {
            document.querySelectorAll(`[data-time-label="${key}"]`).forEach(label => {
                label.textContent = input.value;
            });
            refreshDateUi();
        });
    };

    bindTime('morningTime', 'morning');
    bindTime('middayTime', 'midday');
    bindTime('eveningTime', 'evening');
    startDateInput?.addEventListener('change', refreshDateUi);

    const showClientError = message => {
        const box = document.getElementById('scheduleClientError');
        if (!box) return;

        box.textContent = message;
        box.hidden = !message;

        if (message) {
            box.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    };

    const selectedMedicationRows = () =>
        Array.from(document.querySelectorAll('.schedule-medication-select'))
            .filter(select => !select.disabled && Number(select.value) > 0);

    refreshDateUi();

    // Final client-side convenience validation. The controller repeats the
    // important rules and remains authoritative.
    form.addEventListener('submit', event => {
        const morning = toMinutes(document.getElementById('morningTime')?.value);
        const midday = toMinutes(document.getElementById('middayTime')?.value);
        const evening = toMinutes(document.getElementById('eveningTime')?.value);

        let error = '';

        const start = getStartDate();
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        if (!start) {
            error = 'Choose a schedule start date.';
        } else if (start < today && form.dataset.scheduleMode === 'create') {
            error = 'The schedule start date cannot be in the past.';
        } else if (morning == null || midday == null || evening == null) {
            error = 'Choose all three dispense times.';
        } else if (morning >= 12 * 60) {
            error = 'Morning dispense time must be between 00:00 and 11:59.';
        } else if (midday < 12 * 60 || midday >= 18 * 60) {
            error = 'Midday dispense time must be between 12:00 and 17:59.';
        } else if (evening < 18 * 60) {
            error = 'Evening dispense time must be between 18:00 and 23:59.';
        } else if (midday - morning < 120) {
            error = 'Morning and midday dispense times must be at least 2 hours apart.';
        } else if (evening - midday < 120) {
            error = 'Midday and evening dispense times must be at least 2 hours apart.';
        }

        if (!error) {
            const rows = selectedMedicationRows();

            for (const select of rows) {
                const row = select.closest('.draft-row');
                const qty = Number(row?.querySelector('.schedule-quantity-input')?.value || 0);
                if (qty < 1) {
                    error = 'Enter a quantity for every selected medication.';
                    break;
                }
            }
        }

        if (!error && form.dataset.scheduleMode === 'create') {
            const committedCount = document.querySelectorAll('.committed-row').length;
            if (selectedMedicationRows().length + committedCount === 0) {
                error = 'Add at least one medication before creating the schedule.';
            }
        }

        if (!error) {
            // Do not allow medication to be added to a period that has already passed.
            const invalidPastSlot = Array.from(document.querySelectorAll('.schedule-slot-editor.slot-time-passed'))
                .some(slot => Array.from(slot.querySelectorAll('.schedule-medication-select'))
                    .some(select => Number(select.value) > 0));

            if (invalidPastSlot) {
                error = 'Medication cannot be added to a dispense time that has already passed.';
            }
        }

        if (error) {
            event.preventDefault();
            showClientError(error);
            return;
        }

        showClientError('');
    });
})();
