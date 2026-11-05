document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.md-support-control').forEach(control => {
        const dialog = control.querySelector('.md-support-dialog');
        const open = control.querySelector('[data-support-open]');
        const close = control.querySelector('[data-support-close]');
        if (!dialog || !open || !close) return;
        open.addEventListener('click', () => dialog.showModal());
        close.addEventListener('click', () => dialog.close());
        dialog.addEventListener('click', event => {
            if (event.target === dialog) dialog.close();
        });
    });
});
