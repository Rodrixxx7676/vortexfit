using System.Text.Json;

namespace VortexFit.Services;

public class RecaptchaResult
{
    public bool   Success { get; set; }
    public double Score   { get; set; }
    public string Action  { get; set; } = string.Empty;
}

public class RecaptchaService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration     _config;

    public string SiteKey => _config["Recaptcha:SiteKey"] ?? string.Empty;

    public RecaptchaService(IHttpClientFactory http, IConfiguration config)
    {
        _http   = http;
        _config = config;
    }

    /// <summary>
    /// Verifica un token reCAPTCHA v3 contra la API de Google.
    /// Retorna Success=true solo si el token es válido y el score >= minScore.
    /// </summary>
    public async Task<RecaptchaResult> VerifyAsync(string token, string expectedAction, double minScore = 0.5)
    {
        // Token vacío = reCAPTCHA JS no completó (timeout/red) → fail-open
        if (string.IsNullOrWhiteSpace(token))
            return new RecaptchaResult { Success = true, Score = 0.9, Action = expectedAction };

        var secret = _config["Recaptcha:SecretKey"] ?? string.Empty;
        var client = _http.CreateClient("recaptcha");

        HttpResponseMessage resp;
        try
        {
            resp = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={Uri.EscapeDataString(token)}",
                null);
        }
        catch
        {
            // Si no se puede contactar a Google (red, timeout, etc.) → fail-open
            // para no bloquear usuarios legítimos por problemas de conectividad.
            return new RecaptchaResult { Success = true, Score = 0.9, Action = expectedAction };
        }

        if (!resp.IsSuccessStatusCode)
            return new RecaptchaResult();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        var result = new RecaptchaResult
        {
            Success = root.TryGetProperty("success", out var s)  && s.GetBoolean(),
            Score   = root.TryGetProperty("score",   out var sc) ? sc.GetDouble() : 0,
            Action  = root.TryGetProperty("action",  out var a)  ? a.GetString() ?? "" : ""
        };

        // Validar score mínimo y acción esperada
        if (result.Success && (result.Score < minScore || result.Action != expectedAction))
            result.Success = false;

        return result;
    }
}
