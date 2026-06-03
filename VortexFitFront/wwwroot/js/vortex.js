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

    const SESSION_MINUTES = 15;
    const WARN_BEFORE_MIN = 2;   // aviso a los 13 min
    const warnAt   = (SESSION_MINUTES - WARN_BEFORE_MIN) * 60 * 1000;
    const logoutAt = SESSION_MINUTES * 60 * 1000;

    let warnTimer, logoutTimer;

    function doLogout() {
        Toast.error('Sesión cerrada por inactividad. Redirigiendo…', 3000);
        // Intentar submit del form de logout para limpiar la sesión en el servidor
        setTimeout(() => {
            const form = document.getElementById('inactivityLogoutForm')
                      || document.querySelector('form[action*="Logout"]');
            if (form) { form.submit(); }
            else      { window.location.href = '/Account/Login'; }
        }, 2500);
    }

    function resetTimers() {
        clearTimeout(warnTimer);
        clearTimeout(logoutTimer);

        warnTimer = setTimeout(() => {
            Toast.warning(
                `Tu sesión cerrará en ${WARN_BEFORE_MIN} minutos por inactividad. ` +
                `Mueve el ratón o presiona una tecla para continuar.`,
                12000
            );
        }, warnAt);

        logoutTimer = setTimeout(doLogout, logoutAt);
    }

    ['click', 'keydown', 'mousemove', 'scroll', 'touchstart'].forEach(ev =>
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

/* ══════════════════════════════════════════
   THEME MANAGER — Modo claro / oscuro
══════════════════════════════════════════ */
const ThemeManager = (() => {
    const KEY = 'sg-theme';

    function apply(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        // Actualizar todos los iconos del toggle
        document.querySelectorAll('.theme-icon').forEach(icon => {
            icon.className = theme === 'light'
                ? 'fa-solid fa-moon theme-icon'
                : 'fa-solid fa-sun theme-icon';
        });
    }

    function toggle() {
        const current = document.documentElement.getAttribute('data-theme') || 'dark';
        const next    = current === 'dark' ? 'light' : 'dark';
        localStorage.setItem(KEY, next);
        apply(next);
    }

    function init() {
        const saved = localStorage.getItem(KEY) || 'dark';
        apply(saved);
        document.querySelectorAll('.btn-theme-toggle').forEach(btn => {
            btn.addEventListener('click', toggle);
        });
    }

    return { init, toggle };
})();

/* ══════════════════════════════════════════
   SOPORTE MODAL
══════════════════════════════════════════ */
const SoporteModal = (() => {
    function open() {
        const m = document.getElementById('soporteModal');
        if (m) { m.style.display = 'flex'; document.body.style.overflow = 'hidden'; }
    }

    function close() {
        const m = document.getElementById('soporteModal');
        if (m) { m.style.display = 'none'; document.body.style.overflow = ''; }
        const form = document.getElementById('soporteFormBody');
        const ok   = document.getElementById('soporteSuccess');
        const btn  = document.getElementById('btnEnviarSoporte');
        if (form) form.style.display = 'block';
        if (ok)   ok.style.display   = 'none';
        if (btn)  { btn.disabled = false; btn.innerHTML = '<i class="fa-solid fa-paper-plane"></i> Enviar mensaje'; }
    }

    async function enviar() {
        const nombre  = document.getElementById('sp-nombre')?.value.trim();
        const email   = document.getElementById('sp-email')?.value.trim();
        const asunto  = document.getElementById('sp-asunto')?.value;
        const mensaje = document.getElementById('sp-mensaje')?.value.trim();

        if (!nombre || !email || !mensaje) {
            Toast.error('Completa nombre, correo y mensaje.');
            return;
        }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            Toast.error('Ingresa un correo válido.');
            return;
        }

        const btn = document.getElementById('btnEnviarSoporte');
        btn.disabled = true;
        btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Enviando...';

        try {
            const res = await fetch('/Account/Soporte', {
                method:  'POST',
                headers: { 'Content-Type': 'application/json' },
                body:    JSON.stringify({ nombre, email, asunto, mensaje })
            });
            if (res.ok) {
                document.getElementById('soporteFormBody').style.display = 'none';
                document.getElementById('soporteSuccess').style.display  = 'block';
            } else {
                Toast.error('Error al enviar. Intenta de nuevo.');
                btn.disabled = false;
                btn.innerHTML = '<i class="fa-solid fa-paper-plane"></i> Enviar mensaje';
            }
        } catch {
            Toast.error('Sin conexión. Intenta más tarde.');
            btn.disabled = false;
            btn.innerHTML = '<i class="fa-solid fa-paper-plane"></i> Enviar mensaje';
        }
    }

    function init() {
        document.querySelectorAll('.btn-soporte').forEach(b => b.addEventListener('click', open));
        document.getElementById('closeSoporte')?.addEventListener('click', close);
        document.getElementById('soporteModal')?.addEventListener('click', e => {
            if (e.target === e.currentTarget) close();
        });
        document.getElementById('btnEnviarSoporte')?.addEventListener('click', enviar);
    }

    return { init, open, close };
})();

document.addEventListener('DOMContentLoaded', () => {
    ThemeManager.init();
    SoporteModal.init();
});
