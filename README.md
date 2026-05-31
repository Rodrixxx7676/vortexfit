# 🌀 VortexFit

Sistema de gestión para gimnasio desarrollado con **ASP.NET Core MVC (.NET 10)** y **Oracle Database 23ai**.

> Proyecto de portafolio — ficticio con fines demostrativos.

---

## 🚀 Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core MVC 10 |
| Base de datos | Oracle 23ai (Docker) |
| ORM | Entity Framework Core 9 + Oracle.EntityFrameworkCore |
| Seguridad | BCrypt.Net-Next (hashing de contraseñas) |
| Frontend | Razor Views (.cshtml) + CSS Variables + JS Vanilla |
| Sesiones | ASP.NET Core Session (MemoryCache) |

---

## 📁 Estructura del proyecto

```
vortexfit/
├── VortexFitFront/              # Proyecto principal MVC
│   ├── Controllers/
│   │   ├── HomeController.cs    # Landing y errores
│   │   ├── AccountController.cs # Login / Registro / Logout
│   │   └── DashboardController.cs # Panel del socio
│   ├── Data/
│   │   └── VortexFitDbContext.cs
│   ├── Models/
│   │   ├── Socio.cs             # Entidad Oracle
│   │   ├── LoginViewModel.cs
│   │   ├── RegisterViewModel.cs
│   │   ├── DashboardViewModel.cs
│   │   └── PerfilViewModel.cs
│   ├── Views/
│   │   ├── Home/                # Landing pública
│   │   ├── Account/             # Login y Registro
│   │   ├── Dashboard/           # Panel y Perfil
│   │   └── Shared/              # Layouts y errores
│   ├── wwwroot/
│   │   ├── css/vortex.css       # Estilos globales
│   │   ├── css/dashboard.css    # Estilos del dashboard
│   │   └── js/vortex.js         # JS global (toasts, animaciones)
│   └── Migrations/              # Migraciones EF Core
└── vortexfit.slnx
```

---

## ⚙️ Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Oracle 23ai en Docker (ver sección de configuración)

---

## 🐳 Configurar Oracle con Docker

```bash
# Descargar imagen Oracle Free (23ai)
docker pull container-registry.oracle.com/database/free:latest

# Crear y correr el contenedor
docker run -d \
  --name oracle-23ai \
  -p 1521:1521 \
  -e ORACLE_PWD=TemporalDev2025 \
  container-registry.oracle.com/database/free:latest

# Verificar que esté listo (esperar ~2 min)
docker logs -f oracle-23ai
```

---

## 🏃 Correr el proyecto

```bash
# 1. Iniciar Oracle
docker start oracle-23ai

# 2. Entrar al proyecto
cd VortexFitFront

# 3. Aplicar migraciones (primera vez)
dotnet ef database update

# 4. Correr la aplicación
dotnet run --urls "http://localhost:5050"
```

Abrir en el navegador: **http://localhost:5050**

---

## 🌐 Rutas disponibles

| URL | Descripción | Acceso |
|-----|-------------|--------|
| `/` | Landing page pública | Público |
| `/Account/Register` | Crear cuenta | Público |
| `/Account/Login` | Iniciar sesión | Público |
| `/Dashboard` | Panel del socio | 🔒 Requiere login |
| `/Dashboard/Perfil` | Editar perfil | 🔒 Requiere login |

---

## 🗄️ Modelo de base de datos

### Tabla `SOCIOS`

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `ID_SOCIO` | NUMBER (PK) | Identificador auto-incremental |
| `NOMBRE_COMPLETO` | NVARCHAR2(100) | Nombre del socio |
| `EMAIL` | NVARCHAR2(150) | Correo único (índice) |
| `TELEFONO` | NVARCHAR2(15) | Teléfono (opcional) |
| `PASSWORD_HASH` | NVARCHAR2(255) | Hash BCrypt |
| `PLAN` | NVARCHAR2(20) | Basico / Pro / Elite |
| `ESTADO` | NVARCHAR2(20) | Activo / Inactivo / Suspendido |
| `FECHA_REGISTRO` | TIMESTAMP | Auto: SYSDATE |
| `FECHA_VENCIMIENTO` | TIMESTAMP | Vencimiento de membresía |

---

## 🔐 Cadena de conexión

En `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OracleDb": "User Id=SYSTEM;Password=TemporalDev2025;Data Source=localhost:1521/FREE;"
  }
}
```

> ⚠️ En producción, usa variables de entorno o un gestor de secretos.

---

## 🎨 Paleta de colores

| Variable | Hex | Uso |
|----------|-----|-----|
| `--bg-dark` | `#0D1B2A` | Fondo principal |
| `--bg-card` | `#112236` | Tarjetas |
| `--primary` | `#1E90FF` | Azul eléctrico |
| `--accent` | `#00B4D8` | Cian vibrante |
| `--light-blue` | `#90E0EF` | Azul suave |

---

## ✅ Funcionalidades implementadas

- [x] Landing page responsive (mobile-first)
- [x] Registro de socios con validación
- [x] Login con BCrypt
- [x] Sesiones (30 min de inactividad)
- [x] Dashboard del socio
- [x] Edición de perfil y cambio de contraseña
- [x] Toast notifications
- [x] Páginas de error 404 / 500 estilizadas
- [x] Animaciones de entrada
- [x] Advertencia de sesión próxima a expirar
- [x] Validación cliente (jQuery Unobtrusive)
- [x] Menú hamburguesa responsive
- [x] SEO meta tags + Open Graph

---

## 👤 Autor

**Rodri** — [@Rodrixxx7676](https://github.com/Rodrixxx7676)
