/* =============================================
   Style Gym — JavaScript Global
   ============================================= */

/* ══════════════════════════════════════════
   1. TOAST NOTIFICATIONS
══════════════════════════════════════════ */
const Toast = (() => {
    let container;

    function getContainer() {
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            document.body.appendChild(container);
        }
        return container;
    }

    function show(message, type = 'success', duration = 4000) {
        const c    = getContainer();
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;

        const icons = { success: '✅', error: '❌', warning: '⚠️', info: 'ℹ️' };
        toast.innerHTML = `
            <span class="toast-icon">${icons[type] || icons.info}</span>
            <span class="toast-msg">${message}</span>
            <button class="toast-close" onclick="this.parentElement.remove()">×</button>
        `;

        c.appendChild(toast);
        requestAnimationFrame(() => toast.classList.add('toast-visible'));

        setTimeout(() => {
            toast.classList.remove('toast-visible');
            toast.addEventListener('transitionend', () => toast.remove(), { once: true });
        }, duration);
    }

    return { show, success: m => show(m,'success'), error: m => show(m,'error'),
             warning: m => show(m,'warning'), info: m => show(m,'info') };
})();

/* Leer TempData inyectado en el HTML como data-attributes */
document.addEventListener('DOMContentLoaded', () => {
    const el = document.getElementById('toast-data');
    if (!el) return;
    const msg  = el.dataset.message;
    const type = el.dataset.type || 'success';
    if (msg) Toast.show(msg, type);
});


/* ══════════════════════════════════════════
   2. BOTONES CON ESTADO DE CARGA
══════════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', function () {
            // Solo si el form es válido (HTML5 native check)
            if (!form.checkValidity()) return;

            const btn = form.querySelector('[type="submit"]');
            if (!btn) return;

            const originalText = btn.innerHTML;
            btn.disabled   = true;
            btn.innerHTML  = `<span class="btn-spinner"></span> Procesando…`;
            btn.dataset.originalText = originalText;

            // Restaurar si hay error de validación (el servidor responde con la vista)
            setTimeout(() => {
                if (btn.disabled) {
                    btn.disabled  = false;
                    btn.innerHTML = originalText;
                }
            }, 8000);
        });
    });
});


/* ══════════════════════════════════════════
   3. ANIMACIONES DE ENTRADA (fade-in)
══════════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', () => {
    const observer = new IntersectionObserver(entries => {
        entries.forEach(e => {
            if (e.isIntersecting) {
                e.target.classList.add('animate-in');
                observer.unobserve(e.target);
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll(
        '.service-card, .plan-card, .testimonial-card, .stat-card, .dash-card'
    ).forEach(el => {
        el.classList.add('animate-ready');
        observer.observe(el);
    });
});


/* ══════════════════════════════════════════
   4. ADVERTENCIA DE SESIÓN (solo dashboard)
══════════════════════════════════════════ */
(function () {
    if (!document.body.classList.contains('dash-body')) return;

    const SESSION_MINUTES = 30;
    const WARN_BEFORE_MIN = 3;
    const warnAt  = (SESSION_MINUTES - WARN_BEFORE_MIN) * 60 * 1000;
    const logoutAt = SESSION_MINUTES * 60 * 1000;

    let warnTimer, logoutTimer;

    function resetTimers() {
        clearTimeout(warnTimer);
        clearTimeout(logoutTimer);

        warnTimer = setTimeout(() => {
            Toast.warning(`Tu sesión expira en ${WARN_BEFORE_MIN} minutos. Guarda tus cambios.`, 10000);
        }, warnAt);

        logoutTimer = setTimeout(() => {
            Toast.error('Sesión expirada. Redirigiendo al login…', 3000);
            setTimeout(() => { window.location.href = '/Account/Login'; }, 3000);
        }, logoutAt);
    }

    ['click', 'keydown', 'mousemove', 'scroll'].forEach(ev =>
        document.addEventListener(ev, resetTimers, { passive: true })
    );

    resetTimers();
})();


/* ══════════════════════════════════════════
   5. SMOOTH SCROLL para anclas de la landing
══════════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('a[href^="#"]').forEach(a => {
        a.addEventListener('click', e => {
            const target = document.querySelector(a.getAttribute('href'));
            if (target) {
                e.preventDefault();
                target.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    });
});


/* ══════════════════════════════════════════
   6. CONFIRMAR ACCIONES DESTRUCTIVAS
══════════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-confirm]').forEach(el => {
        el.addEventListener('click', e => {
            if (!confirm(el.dataset.confirm)) e.preventDefault();
        });
    });
});
