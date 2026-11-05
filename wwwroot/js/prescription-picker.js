(() => {
    const list = document.getElementById('prescription-items');
    const template = document.getElementById('prescription-item-template');
    const dialog = document.getElementById('prescription-medication-dialog');
    if (!list || !template || !dialog) return;

    const results = document.getElementById('rx-picker-results');
    const message = document.getElementById('rx-picker-message');
    const searchInput = document.getElementById('rx-picker-search');
    const pageLabel = document.getElementById('rx-picker-page');
    const prev = document.getElementById('rx-picker-prev');
    const next = document.getElementById('rx-picker-next');
    const endpoint = dialog.dataset.url;
    let currentRow = null;
    let page = 1, totalPages = 1, search = '';
    let requestId = 0;

    const rowSelector = '.prescription-entry';
    const rows = () => Array.from(list.querySelectorAll(':scope > ' + rowSelector));

    function reindex() {
        rows().forEach((row, i) => {
            row.querySelectorAll('[name], [id], [for], [data-valmsg-for]').forEach(element => {
                for (const attr of ['name', 'id', 'for', 'data-valmsg-for']) {
                    const value = element.getAttribute(attr);
                    if (!value) continue;
                    element.setAttribute(attr, value
                        .replace(/Items\[\d+\]/g, `Items[${i}]`)
                        .replace(/Items_\d+__/g, `Items_${i}__`));
                }
            });
        });
    }

    function addRow() {
        if (rows().length >= 20) return;
        list.insertAdjacentHTML('beforeend', template.innerHTML.replaceAll('__INDEX__', rows().length));
        reindex();
    }

    document.getElementById('add-prescription-item')?.addEventListener('click', addRow);
    if (rows().length === 0) addRow();

    list.addEventListener('click', event => {
        const remove = event.target.closest('.tab-remove-item');
        if (remove) {
            if (rows().length <= 1) return;
            remove.closest(rowSelector).remove();
            reindex();
            return;
        }
        const choose = event.target.closest('.choose-prescription-med');
        if (choose) {
            currentRow = choose.closest(rowSelector);
            dialog.hidden = false;
            document.body.classList.add('rx-picker-open');
            searchInput.value = '';
            search = '';
            load(1);
            searchInput.focus();
        }
    });

    function closePicker() {
        dialog.hidden = true;
        document.body.classList.remove('rx-picker-open');
        currentRow = null;
        requestId++; // ignore responses from any pending request
    }

    dialog.addEventListener('click', e => {
        if (e.target.closest('[data-close-picker]')) closePicker();
    });
    document.addEventListener('keydown', e => {
        if (e.key === 'Escape' && !dialog.hidden) closePicker();
    });
    document.getElementById('rx-picker-search-form').addEventListener('submit', e => {
        e.preventDefault();
        search = searchInput.value.trim();
        load(1);
    });
    prev.addEventListener('click', () => load(page - 1));
    next.addEventListener('click', () => load(page + 1));

    async function load(targetPage) {
        const id = ++requestId;
        message.textContent = 'Loading medications…';
        results.replaceChildren();
        prev.disabled = true;
        next.disabled = true;
        try {
            const url = new URL(endpoint, window.location.href);
            
            url.searchParams.set('search', search);
            url.searchParams.set('page', targetPage);
            
            const response = await fetch(url, {headers: {'Accept': 'application/json'}});
            
            if (!response.ok) 
                throw new Error('Unable to load medications. Please refresh and try again.');
            
            const data = await response.json();
            if (id !== requestId) 
                return;
            
            page = data.page;
            
            totalPages = data.totalPages;
            
            pageLabel.textContent = `Page ${page} of ${totalPages} · ${data.totalCount} results`;
            prev.disabled = page <= 1;
            next.disabled = page >= totalPages;
            message.textContent = data.items.length ? '' : 'No matching medications. Try another search.';

            for (const medication of data.items) {
                
                const row = document.createElement('div');
                row.className = 'rx-picker-result';
                const info = document.createElement('div');
                const name = document.createElement('strong');
                name.textContent = medication.name;
                const details = document.createElement('small');
                details.textContent = [medication.genericName, medication.strength, medication.form, medication.manufacturer].filter(Boolean).join(' · ');
                info.append(name, details);
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'btn btn-primary';
                button.textContent = 'Select';
                button.addEventListener('click', () => {
                    if (!currentRow) return;
                    currentRow.querySelector('.prescription-med-id').value = medication.id;
                    currentRow.querySelector('[data-selected-name]').textContent = [medication.name, medication.strength, medication.form].filter(Boolean).join(' · ');
                    closePicker();
                });
                row.append(info, button);
                results.append(row);
            }
        } catch (error) {
            if (id === requestId) message.textContent = error.message;
        }
    }
})();
