(() => {
    const sidebar = document.getElementById('patientSidebar');
    const scrim = document.getElementById('sidebarScrim');
    const menu = document.getElementById('menuButton');
    const more = document.getElementById('mobileMore');
    if (!sidebar || !scrim) return;

    const open = () => document.body.classList.add('sidebar-open');
    const close = () => document.body.classList.remove('sidebar-open');
    menu?.addEventListener('click', open);
    more?.addEventListener('click', open);
    scrim.addEventListener('click', close);
})();

(() => {
    const dayButtons = document.querySelectorAll('.schedule-day[data-day]');
    const dayPanels = document.querySelectorAll('.schedule-editor-day[data-day-panel]');
    if (!dayButtons.length || !dayPanels.length) return;

    dayButtons.forEach(button => {
        button.addEventListener('click', () => {
            const day = button.dataset.day;
            dayButtons.forEach(item => item.classList.toggle('active', item === button));
            dayPanels.forEach(panel => panel.classList.toggle('active', panel.dataset.dayPanel === day));
        });
    });
})();

(() => {
    const template = document.getElementById('medicationRowTemplate');
    if (!template) return;

    const refreshCount = (slot) => {
        const count = slot.querySelectorAll('.medication-edit-row').length;
        const badge = slot.querySelector('.slot-count');
        if (!badge) return;
        badge.textContent = count === 0 ? 'No medication' : `${count} medication${count === 1 ? '' : 's'}`;
        badge.classList.toggle('empty', count === 0);
    };

    document.addEventListener('click', event => {
        const add = event.target.closest('.add-medication');
        if (add) {
            const slot = add.closest('.schedule-slot-editor');
            if (!slot || slot.classList.contains('slot-cancelled')) return;
            slot.querySelector('.slot-medication-list')?.appendChild(template.content.cloneNode(true));
            refreshCount(slot);
            return;
        }

        const remove = event.target.closest('.remove-medication');
        if (remove) {
            const row = remove.closest('.medication-edit-row');
            const slot = remove.closest('.schedule-slot-editor');
            if (row?.classList.contains('committed-row')) return;
            row?.remove();
            if (slot) refreshCount(slot);
            return;
        }

        const cancel = event.target.closest('.cancel-slot');
        if (cancel) {
            const slot = cancel.closest('.schedule-slot-editor');
            if (!slot) return;

            const cancelled = slot.classList.toggle('slot-cancelled');
            cancel.innerHTML = cancelled
                ? '<i class="fa-solid fa-arrow-rotate-left"></i> Undo cancellation'
                : '<i class="fa-solid fa-ban"></i> Cancel dispense slot';

            slot.querySelectorAll('select, input, .add-medication').forEach(control => {
                control.disabled = cancelled;
            });
        }
    });

    const bindTime = (inputId, key) => {
        const input = document.getElementById(inputId);
        if (!input) return;
        input.addEventListener('input', () => {
            document.querySelectorAll(`[data-time-label="${key}"]`).forEach(label => label.textContent = input.value);
        });
    };

    bindTime('morningTime', 'morning');
    bindTime('middayTime', 'midday');
    bindTime('eveningTime', 'evening');
})();

(() => {
    const sidebar = document.getElementById('patientSidebar');
    const scrim = document.getElementById('sidebarScrim');
    const menu = document.getElementById('menuButton');
    const more = document.getElementById('mobileMore');
    if (!sidebar || !scrim) return;

    const open = () => document.body.classList.add('sidebar-open');
    const close = () => document.body.classList.remove('sidebar-open');

    menu?.addEventListener('click', open);
    more?.addEventListener('click', open);
    scrim.addEventListener('click', close);
})();