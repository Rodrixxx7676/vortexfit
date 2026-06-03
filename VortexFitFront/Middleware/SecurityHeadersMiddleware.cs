namespace VortexFit.Middleware;

/// <summary>
/// Añade cabeceras HTTP de seguridad a todas las respuestas y aplica
/// Cache-Control estricto a las rutas autenticadas.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // ── Prevenir MIME-sniffing ────────────────────────────
        headers["X-Content-Type-Options"] = "nosniff";

        // ── Protección contra clickjacking ───────────────────
        headers["X-Frame-Options"] = "SAMEORIGIN";

        // ── Filtro XSS heredado (navegadores antiguos) ───────
        headers["X-XSS-Protection"] = "1; mode=block";

        // ── No filtrar URL de origen a terceros ──────────────
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // ── Deshabilitar APIs sensibles del navegador ────────
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // ── Bloquear carga de contenido cross-domain ─────────
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // ── Eliminar cabeceras que revelan la tecnología ─────
        headers.Remove("X-Powered-By");
        // Server se elimina vía Kestrel (AddServerHeader = false en Program.cs)

        // ── Content Security Policy ──────────────────────────
        // unsafe-inline necesario porque Razor inyecta scripts inline.
        // En el futuro se puede migrar a nonces por vista.
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' " +
                "https://kit.fontawesome.com " +
                "https://ka-f.fontawesome.com " +
                "https://www.google.com " +
                "https://www.gstatic.com; " +
            "style-src 'self' 'unsafe-inline' " +
                "https://ka-f.fontawesome.com; " +
            "font-src 'self' data: " +
                "https://ka-f.fontawesome.com; " +
            "img-src 'self' data: https:; " +
            "connect-src 'self' " +
                "https://ka-f.fontawesome.com " +
                "https://www.google.com; " +
            "frame-src https://www.google.com; " +
            "worker-src 'self'; " +
            "manifest-src 'self'; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self';";

        // ── Cache-Control para rutas autenticadas ────────────
        // Impide que el navegador cachee páginas del dashboard/admin,
        // evitando que el botón "Atrás" las muestre tras cerrar sesión.
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/Dashboard", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
            headers["Pragma"]        = "no-cache";
        }

        await _next(context);
    }
}
