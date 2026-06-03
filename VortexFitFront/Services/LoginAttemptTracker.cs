using Microsoft.Extensions.Caching.Memory;

namespace VortexFit.Services;

/// <summary>
/// Rastrea intentos fallidos de login por email.
/// Escala las penalizaciones y bloquea permanentemente si se excede el límite.
///
/// Escalada:
///   >= 5  intentos →  30 segundos
///   >= 10 intentos →  60 segundos
///   >= 15 intentos →   5 minutos
///   >= 20 intentos →  bloqueo permanente (24 h)
/// </summary>
public class LoginAttemptTracker
{
    private readonly IMemoryCache _cache;

    // Umbrales: (intentos mínimos, duración del bloqueo)
    private static readonly (int threshold, TimeSpan duration)[] Levels =
    [
        (20, TimeSpan.FromHours(24)),    // bloqueo permanente
        (15, TimeSpan.FromMinutes(5)),
        (10, TimeSpan.FromSeconds(60)),
        ( 5, TimeSpan.FromSeconds(30)),
    ];

    public LoginAttemptTracker(IMemoryCache cache)
    {
        _cache = cache;
    }

    // ── Clave de caché por email ───────────────────────────
    private static string CacheKey(string email) =>
        $"login_attempts:{email.Trim().ToLower()}";

    // ── Obtener o crear registro ───────────────────────────
    private AttemptRecord GetRecord(string email)
    {
        return _cache.GetOrCreate(CacheKey(email), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return new AttemptRecord();
        })!;
    }

    // ── Consultar estado de bloqueo ────────────────────────
    public (bool blocked, TimeSpan? remaining, bool permanent) CheckBlock(string email)
    {
        var rec = GetRecord(email);

        if (rec.LockedUntil is null || rec.LockedUntil <= DateTime.UtcNow)
            return (false, null, false);

        var remaining  = rec.LockedUntil.Value - DateTime.UtcNow;
        var permanent  = rec.Attempts >= 20;
        return (true, remaining, permanent);
    }

    // ── Registrar intento fallido ──────────────────────────
    public (bool nowBlocked, TimeSpan? lockDuration) RecordFailure(string email)
    {
        var rec = GetRecord(email);
        rec.Attempts++;

        TimeSpan? lockFor = null;
        foreach (var (threshold, duration) in Levels)
        {
            if (rec.Attempts >= threshold)
            {
                rec.LockedUntil = DateTime.UtcNow.Add(duration);
                lockFor = duration;
                break;
            }
        }

        _cache.Set(CacheKey(email), rec,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            });

        return (lockFor.HasValue, lockFor);
    }

    // ── Limpiar al loguearse correctamente ─────────────────
    public void RecordSuccess(string email) =>
        _cache.Remove(CacheKey(email));

    // ── Obtener conteo actual (para mostrar en UI) ─────────
    public int GetAttempts(string email) => GetRecord(email).Attempts;
}

// ── Modelo interno ─────────────────────────────────────────
internal class AttemptRecord
{
    public int       Attempts   { get; set; }
    public DateTime? LockedUntil { get; set; }
}
