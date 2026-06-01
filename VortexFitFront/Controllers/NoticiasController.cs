using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VortexFit.Data;
using VortexFit.Models;

namespace VortexFit.Controllers;

public class NoticiasController : Controller
{
    private readonly VortexFitDbContext _db;

    public NoticiasController(VortexFitDbContext db)
    {
        _db = db;
    }

    // ── LISTADO ───────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index(string? categoria = null)
    {
        await SeedIfEmptyAsync();

        var query = _db.Noticias
            .Where(n => n.Activo)
            .AsQueryable();

        if (!string.IsNullOrEmpty(categoria) && categoria != "Todas")
            query = query.Where(n => n.Categoria == categoria);

        var noticias = await query
            .OrderByDescending(n => n.FechaPublicacion)
            .ToListAsync();

        var categorias = await _db.Noticias
            .Where(n => n.Activo)
            .Select(n => n.Categoria)
            .Distinct()
            .ToListAsync();

        ViewBag.Categorias      = categorias;
        ViewBag.CategoriaActual = categoria ?? "Todas";
        return View(noticias);
    }

    // ── DETALLE ───────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var noticia = await _db.Noticias
            .FirstOrDefaultAsync(n => n.IdNoticia == id && n.Activo);

        if (noticia == null) return NotFound();

        // Otras noticias (relacionadas)
        ViewBag.Relacionadas = await _db.Noticias
            .Where(n => n.Activo && n.IdNoticia != id)
            .OrderByDescending(n => n.FechaPublicacion)
            .Take(3)
            .ToListAsync();

        return View(noticia);
    }

    // ── SEED DE DEMO ─────────────────────────────────
    private async Task SeedIfEmptyAsync()
    {
        if (await _db.Noticias.AnyAsync()) return;

        var noticias = new List<Noticia>
        {
            new()
            {
                Titulo = "Nuevos horarios de clases para junio",
                Resumen = "Hemos ampliado nuestra oferta de clases para este mes. Ahora contamos con más horarios de spinning, yoga y CrossFit para adaptarnos mejor a tu agenda.",
                Contenido = @"¡Buenas noticias para todos los miembros de Style Gym!

A partir del 1 de junio, incorporamos nuevos horarios en todas nuestras disciplinas para que puedas entrenar en el momento que más te convenga.

**Cambios destacados:**
- Spinning: nuevas sesiones a las 6:00 AM y 9:00 PM de lunes a viernes
- Yoga: clases de relajación nocturna a las 8:30 PM los miércoles y viernes
- CrossFit: abrimos los domingos de 8:00 AM a 11:00 AM
- Zumba: sumamos una clase extra los sábados a mediodía

Recuerda que puedes reservar tu lugar desde el panel de tu cuenta con hasta 48 horas de anticipación. Los cupos son limitados, ¡no te quedes fuera!

Para cualquier consulta sobre los nuevos horarios, acércate a recepción o escríbenos por nuestros canales oficiales.",
                Categoria = "Comunicado",
                FechaPublicacion = DateTime.UtcNow.AddDays(-2),
                Activo = true
            },
            new()
            {
                Titulo = "Inauguración de la nueva zona de pesas libre",
                Resumen = "Completamos la renovación de nuestra sala de pesas libre con equipos de última generación. Más espacio, mejores máquinas y nueva iluminación.",
                Contenido = @"Style Gym sigue creciendo para darte la mejor experiencia de entrenamiento.

Nos complace anunciar que hemos finalizado la remodelación completa de nuestra zona de pesas libre, con una inversión significativa en equipamiento de primer nivel.

**Lo que encontrarás:**
- 40% más de espacio disponible
- Mancuernas desde 1 kg hasta 60 kg (juego completo)
- 8 nuevas barras olímpicas con platos bumper
- Zona de levantamiento con plataformas de madera
- Espejos de pared completos para control postural
- Sistema de iluminación LED con temperatura cálida

La nueva zona ya está disponible para todos los miembros. Si eres del plan Pro o Elite, tienes acceso prioritario en horarios pico (6-9 AM y 6-9 PM).

¡Te esperamos para que la estrenes!",
                Categoria = "Novedad",
                FechaPublicacion = DateTime.UtcNow.AddDays(-7),
                Activo = true
            },
            new()
            {
                Titulo = "Torneo interno de CrossFit — Inscripciones abiertas",
                Resumen = "El próximo 15 de junio organizamos nuestro primer torneo interno de CrossFit. Categorías para todos los niveles. Premios para los tres primeros lugares.",
                Contenido = @"¡Llega el momento de demostrar todo lo que has entrenado!

Style Gym organiza su primer torneo interno de CrossFit, abierto a todos los miembros activos. Será una jornada de camaradería, superación y mucha adrenalina.

**Detalles del evento:**
- Fecha: sábado 15 de junio, 2026
- Hora: 9:00 AM – 2:00 PM
- Lugar: Sala principal Style Gym

**Categorías:**
- Principiante (menos de 6 meses de entrenamiento)
- Intermedio (6 meses a 2 años)
- Avanzado (más de 2 años)

**Premios:**
- 1er lugar: membresía gratuita por 3 meses
- 2do lugar: membresía gratuita por 1 mes
- 3er lugar: kit de accesorios Style Gym

Las inscripciones cierran el 10 de junio. Cupos limitados a 30 participantes por categoría. Para inscribirte, acércate a recepción o escríbenos.

¡El único límite eres tú!",
                Categoria = "Evento",
                FechaPublicacion = DateTime.UtcNow.AddDays(-10),
                Activo = true
            },
            new()
            {
                Titulo = "Actualización de políticas de uso de instalaciones",
                Resumen = "Revisamos y actualizamos nuestro reglamento interno para garantizar un ambiente seguro y respetuoso para todos los miembros.",
                Contenido = @"Estimados miembros,

Con el objetivo de mantener Style Gym como un espacio seguro, ordenado y agradable para toda la comunidad, hemos actualizado nuestras políticas de uso de instalaciones, efectivas desde el 1 de junio de 2026.

**Principales cambios:**

1. **Reserva de equipos:** Los equipos de cardio solo pueden reservarse con 30 minutos de anticipación máxima en horarios pico.

2. **Uso de toalla:** Obligatorio el uso de toalla en todos los equipos. Style Gym provee toallas de cortesía en recepción.

3. **Tiempo máximo en equipos:** En horarios pico, el tiempo máximo en máquinas de cardio es de 45 minutos si hay espera.

4. **Fotografías y videos:** Permitidos solo con consentimiento de las personas que aparezcan en la toma.

5. **Acompañantes:** Los miembros pueden traer un invitado por semana con previo aviso en recepción (sujeto a disponibilidad).

Agradecemos tu comprensión y compromiso con nuestra comunidad. Estas políticas nos ayudan a brindarte siempre el mejor servicio.",
                Categoria = "Comunicado",
                FechaPublicacion = DateTime.UtcNow.AddDays(-15),
                Activo = true
            },
            new()
            {
                Titulo = "Nuevo instructor: Carlos Mendoza se une al equipo",
                Resumen = "Damos la bienvenida a Carlos Mendoza, especialista en entrenamiento funcional y preparación física de alto rendimiento.",
                Contenido = @"El equipo de Style Gym sigue sumando talento.

Nos llena de orgullo presentar a nuestro nuevo instructor, **Carlos Mendoza**, quien se une a nuestra familia a partir de este mes.

**Perfil profesional:**
- Licenciado en Ciencias del Deporte (PUCP)
- Certificación CrossFit Level 2
- 8 años de experiencia en entrenamiento funcional
- Ex preparador físico de selecciones universitarias nacionales

Carlos estará a cargo de las nuevas clases de Functional Training y también ofrecerá sesiones de entrenamiento personalizado para miembros de los planes Pro y Elite.

Sus horarios de clase estarán disponibles en la sección de horarios a partir de la próxima semana. Para agendar una sesión de entrenamiento personal, contáctalo directamente en recepción.

¡Bienvenido, Carlos!",
                Categoria = "Novedad",
                FechaPublicacion = DateTime.UtcNow.AddDays(-20),
                Activo = true
            },
        };

        _db.Noticias.AddRange(noticias);
        await _db.SaveChangesAsync();
    }
}
