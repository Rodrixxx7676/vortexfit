using Microsoft.EntityFrameworkCore;
using VortexFit.Data;
using WebPush;

namespace VortexFit.Services;

public class PushNotificationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration       _config;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        IServiceScopeFactory scopeFactory,
        IConfiguration       config,
        ILogger<PushNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _config       = config;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnviarRecordatorios(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en PushNotificationService");
            }
            // Revisar cada minuto
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task EnviarRecordatorios(CancellationToken ct)
    {
        var publicKey  = _config["Vapid:PublicKey"]  ?? string.Empty;
        var privateKey = _config["Vapid:PrivateKey"] ?? string.Empty;
        var subject    = _config["Vapid:Subject"]    ?? "mailto:admin@stylegym.com";

        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VortexFitDbContext>();

        // Calcular ventana: clases que empiezan entre 55 y 65 minutos desde ahora
        var ahora     = DateTime.Now;
        var diaActual = ahora.DayOfWeek switch
        {
            DayOfWeek.Monday    => "Lunes",
            DayOfWeek.Tuesday   => "Martes",
            DayOfWeek.Wednesday => "Miércoles",
            DayOfWeek.Thursday  => "Jueves",
            DayOfWeek.Friday    => "Viernes",
            DayOfWeek.Saturday  => "Sábado",
            _                   => string.Empty
        };

        if (string.IsNullOrEmpty(diaActual)) return;

        // Buscar reservas de hoy confirmadas
        var reservasHoy = await db.Reservas
            .Where(r => r.Dia == diaActual && r.Estado == "Confirmada")
            .Include(r => r.Socio)
            .ToListAsync(ct);

        foreach (var reserva in reservasHoy)
        {
            if (!TimeOnly.TryParse(reserva.Hora, out var horaClase)) continue;

            var minutosHasta = (horaClase - TimeOnly.FromDateTime(ahora)).TotalMinutes;
            if (minutosHasta is < 55 or > 65) continue;

            // Buscar suscripciones del socio
            var suscripciones = await db.PushSuscripciones
                .Where(p => p.IdSocio == reserva.IdSocio)
                .ToListAsync(ct);

            foreach (var sub in suscripciones)
            {
                try
                {
                    var pushSub = new PushSubscription(sub.Endpoint, sub.P256dh, sub.AuthKey);
                    var vapid   = new VapidDetails(subject, publicKey, privateKey);
                    var payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        title = $"¡Tu clase de {reserva.NombreClase} empieza en 1 hora!",
                        body  = $"{reserva.Hora} con {reserva.Instructor}. ¡Prepárate!",
                        icon  = "/images/icons/icon-192.png",
                        url   = "/Dashboard/Clases"
                    });

                    var client = new WebPushClient();
                    await client.SendNotificationAsync(pushSub, payload, vapid);
                }
                catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    // Suscripción expirada — eliminar
                    db.PushSuscripciones.Remove(sub);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo enviar push a socio {Id}", reserva.IdSocio);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
