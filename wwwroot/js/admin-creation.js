(() => {
    const dialog = document.getElementById('adminCreationDialog');
    if (!dialog) return;

    const form = document.getElementById('adminCreationForm');
    const result = document.getElementById('adminCreationResult');
    const error = document.getElementById('adminCreationError');
    const submit = document.getElementById('submitAdminCreation');
    const emailOutput = document.getElementById('createdAdminEmail');
    const passwordOutput = document.getElementById('createdAdminPassword');
    let credentials = null;

    document.getElementById('openAdminCreation').addEventListener('click', () => {
        form.reset();
        form.hidden = false;
        result.hidden = true;
        error.hidden = true;
        credentials = null;
        dialog.showModal();
        document.getElementById('newAdminEmail').focus();
    });

    const close = () => dialog.close();
    document.getElementById('closeAdminCreation').addEventListener('click', close);
    document.getElementById('cancelAdminCreation').addEventListener('click', close);
    document.getElementById('doneAdminCreation').addEventListener('click', close);

    dialog.addEventListener('close', () => {
        credentials = null;
        emailOutput.textContent = '';
        passwordOutput.textContent = '';
    });

    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        if (!form.reportValidity()) return;
        error.hidden = true;
        submit.disabled = true;

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json' },
                body: new FormData(form)
            });
            const data = await response.json();
            if (!response.ok) {
                throw new Error(data.message || 'Unable to create the administrator.');
            }

            credentials = { email: data.email, password: data.password };
            emailOutput.textContent = credentials.email;
            passwordOutput.textContent = credentials.password;
            form.hidden = true;
            result.hidden = false;
        } catch (failure) {
            error.textContent = failure.message || 'Unable to create the administrator.';
            error.hidden = false;
        } finally {
            submit.disabled = false;
        }
    });

    document.getElementById('copyAdminCredentials').addEventListener('click', async (event) => {
        if (!credentials) return;
        const text = `MediDrop administrator\nEmail: ${credentials.email}\nTemporary password: ${credentials.password}`;
        try {
            await navigator.clipboard.writeText(text);
            event.currentTarget.textContent = 'Copied';
        } catch {
            event.currentTarget.textContent = 'Select and copy the credentials above';
        }
    });
})();
