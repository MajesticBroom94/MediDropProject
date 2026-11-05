(() => {
    "use strict";
    const links = [...document.querySelectorAll('[data-notification-link]')];
    if (!links.length) return;

    const endpoint = '/Notification/Live';
    const badgeSelectors = '.nav-count, .provider-nav-count, .provider-notice-count, .admin-nav-count';
    let cursor = null;
    let busy = false;
    let toastTimer = null;
    const host = document.createElement('aside');
    host.className = 'medidrop-live-notice';
    host.setAttribute('role', 'status');
    host.setAttribute('aria-live', 'polite');
    document.body.append(host);

    function updateBadges(count) {
        const label = count > 99 ? '99+' : String(count);
        for (const link of links) {
            let badge = link.querySelector('[data-live-count]');
            if (!badge) {
                badge = link.querySelector(badgeSelectors);
                if (badge) badge.dataset.liveCount = '';
            }
            if (count === 0) {
                if (badge) badge.remove();
                continue;
            }
            if (!badge) {
                badge = document.createElement('span');
                badge.dataset.liveCount = '';
                if (link.classList.contains('top-notice') || link.classList.contains('admin-bell')) {
                    // Original badge style is inherited from these existing header anchors.
                } else if (link.classList.contains('provider-top-notice')) {
                    badge.className = 'provider-notice-count';
                } else if (link.closest('.admin-nav')) {
                    badge.className = 'admin-nav-count';
                } else if (link.closest('nav') && document.querySelector('.provider-top-notice')) {
                    badge.className = 'provider-nav-count';
                } else {
                    badge.className = 'nav-count';
                }
                link.append(badge);
            }
            badge.textContent = label;
        }
    }

    // Position the popup beneath the visible header bell (not the sidebar link).
    // Its entrance animation starts at the bell's screen coordinates.
    function positionToastAtBell() {
        const bell = document.querySelector('.top-notice[data-notification-link], .provider-top-notice[data-notification-link], .admin-bell[data-notification-link]')
            || links[0];
        if (!bell) return;

        const rect = bell.getBoundingClientRect();
        const viewportWidth = document.documentElement.clientWidth;
        const width = Math.min(390, viewportWidth <= 600 ? 360 : 390, viewportWidth - 20);
        const left = Math.max(10, Math.min(rect.right - width + 26, viewportWidth - width - 10));
        const top = Math.max(12, Math.min(rect.bottom + 15, window.innerHeight - 100));
        const bellCenterX = rect.left + rect.width / 2;
        const bellCenterY = rect.top + rect.height / 2;
        const originX = Math.max(0, Math.min(width, bellCenterX - left));

        host.style.left = `${left}px`;
        host.style.right = 'auto';
        host.style.top = `${top}px`;
        host.style.setProperty('--notice-origin-x', `${originX}px`);
        host.style.setProperty('--notice-offset-x', `${bellCenterX - (left + originX)}px`);
        host.style.setProperty('--notice-offset-y', `${bellCenterY - top}px`);
        host.style.setProperty('--notice-arrow-right', `${Math.max(8, Math.min(width - 24, width - originX - 7))}px`);
    }

    window.addEventListener('resize', () => { if (host.hasChildNodes()) positionToastAtBell(); });
    window.addEventListener('scroll', () => { if (host.hasChildNodes()) positionToastAtBell(); }, { passive: true });

    function showToast(item, additional) {
        positionToastAtBell();
        clearTimeout(toastTimer);
        host.replaceChildren();
        const card = document.createElement('div');
        card.className = 'medidrop-toast-card';
        const icon = document.createElement('span');
        icon.className = 'medidrop-toast-icon';
        icon.innerHTML = '<i class="fa-regular fa-bell" aria-hidden="true"></i>';
        const content = document.createElement('div');
        content.className = 'medidrop-toast-content';
        const eyebrow = document.createElement('span');
        eyebrow.className = 'medidrop-toast-eyebrow';
        eyebrow.textContent = additional ? `New notification (+${additional} more)` : 'New notification';
        const title = document.createElement('strong');
        title.textContent = item.title;
        const message = document.createElement('p');
        message.textContent = item.message;
        const actions = document.createElement('div');
        actions.className = 'medidrop-toast-actions';
        const open = document.createElement('a');
        open.href = item.url || links[0].href;
        open.textContent = item.url ? 'View details' : 'View notifications';
        const dismiss = document.createElement('button');
        dismiss.type = 'button';
        dismiss.textContent = 'Dismiss';
        dismiss.addEventListener('click', () => host.replaceChildren());
        actions.append(open, dismiss);
        content.append(eyebrow, title, message, actions);
        card.append(icon, content);
        host.append(card);
        toastTimer = setTimeout(() => host.replaceChildren(), 9000);
    }

    async function poll() {
        if (busy || document.hidden) return;
        busy = true;
        
        try {
            const url = cursor === null ? endpoint : `${endpoint}?sinceId=${encodeURIComponent(cursor)}`;
            const response = await fetch(url, { credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'application/json' } });
           
            if (!response.ok) 
                return;
            
            
            const data = await response.json();
            updateBadges(data.unreadCount);
            // Don't pop up notifications that were already present on page load.
            if (cursor !== null && data.notifications?.length) {
                showToast(data.notifications[0], Math.max(0, data.notifications.length - 1));
            }
            cursor = Math.max(cursor ?? 0, data.latestId ?? 0);
        } 
        
        catch (error) {
            // A temporary connection failure should not disturb the current page.
            console.debug('[MediDrop notifications] Poll unavailable', error);
        } 
        
        finally {
            busy = false;
        }
    }

    poll();
    setInterval(poll, 5000);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) poll(); });
})();
