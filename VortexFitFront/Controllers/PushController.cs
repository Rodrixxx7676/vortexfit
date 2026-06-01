using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VortexFit.Data;
using VortexFit.Models;

namespace VortexFit.Controllers;

[Route("api/push")]
[ApiController]
public class PushController : ControllerBase
{
    private readonly VortexFitDbContext _db;
    private readonly IConfiguration    _config;

    public PushController(VortexFitDbContext db, IConfiguration config)
    {
        _db     = db;
        _config = config;
    }

    [HttpGet("publickey")]
    public IActionResult GetPublicKey()
    {
        var key = _config["Vapid:PublicKey"] ?? string.Empty;

        // Si no hay clave configurada, intentar cargar vapid-keys.json
        if (string.IsNullOrEmpty(key))
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), "vapid-keys.json");
            if (System.IO.File.Exists(file))
            {
                var json = System.IO.File.ReadAllText(file);
                var obj  = System.Text.Json.JsonDocument.Parse(json).RootElement;
                key = obj.GetProperty("PublicKey").GetString() ?? string.Empty;
            }
        }

        return Ok(new { publicKey = key });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeDto dto)
    {
        var socioId = HttpContext.Session.GetInt32("SocioId");
        if (socioId is null) return Unauthorized();

        // Eliminar suscripción anterior del mismo endpoint si existe
        var existente = await _db.PushSuscripciones
            .FirstOrDefaultAsync(p => p.Endpoint == dto.Endpoint);

        if (existente != null)
        {
            existente.IdSocio  = socioId.Value;
            existente.P256dh   = dto.P256dh;
            existente.AuthKey  = dto.Auth;
        }
        else
        {
            _db.PushSuscripciones.Add(new PushSuscripcion
            {
                IdSocio       = socioId.Value,
                Endpoint      = dto.Endpoint,
                P256dh        = dto.P256dh,
                AuthKey       = dto.Auth,
                FechaRegistro = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDto dto)
    {
        var sub = await _db.PushSuscripciones
            .FirstOrDefaultAsync(p => p.Endpoint == dto.Endpoint);

        if (sub != null)
        {
            _db.PushSuscripciones.Remove(sub);
            await _db.SaveChangesAsync();
        }

        return Ok();
    }
}

public record PushSubscribeDto(string Endpoint, string P256dh, string Auth);
public record UnsubscribeDto(string Endpoint);
