namespace VortexFit.Models;

public class EjercicioRutina
{
    public string Nombre    { get; set; } = "";
    public string Series    { get; set; } = "";   // "4×10"
    public string Musculo   { get; set; } = "";
    public string Icono     { get; set; } = "fa-dumbbell";
    public string Color     { get; set; } = "#3DD9D9";
    public string? Nota     { get; set; }
}

public class DiaRutina
{
    public string Dia       { get; set; } = "";
    public string Abrev     { get; set; } = "";
    public string Tipo      { get; set; } = "Fuerza";  // Fuerza / Cardio / Descanso
    public string Icono     { get; set; } = "fa-dumbbell";
    public string Color     { get; set; } = "#3DD9D9";
    public bool   Descanso  { get; set; }
    public List<EjercicioRutina> Ejercicios { get; set; } = new();
}

public class RutinaViewModel
{
    public Socio           Socio      { get; set; } = null!;
    public string          Entrenador { get; set; } = "";
    public string          Objetivo   { get; set; } = "";
    public List<DiaRutina> Semana     { get; set; } = new();
}

// ─── Nutrición ────────────────────────────────────────────────
public class ItemComida
{
    public string  Nombre   { get; set; } = "";
    public string  Porcion  { get; set; } = "";
    public string? Proteina { get; set; }
    public string? Carbs    { get; set; }
    public string? Grasas   { get; set; }
}

public class Comida
{
    public string          Tipo      { get; set; } = "";
    public string          Hora      { get; set; } = "";
    public string          Icono     { get; set; } = "";
    public string          Color     { get; set; } = "";
    public int             Calorias  { get; set; }
    public List<ItemComida> Items    { get; set; } = new();
}

public class NutricionViewModel
{
    public Socio         Socio          { get; set; } = null!;
    public string        Objetivo       { get; set; } = "";
    public int           CaloriasDiarias{ get; set; }
    public string        Proteinas      { get; set; } = "";
    public string        Carbohidratos  { get; set; } = "";
    public string        Grasas         { get; set; } = "";
    public List<Comida>  Comidas        { get; set; } = new();
    public List<string>  Consejos       { get; set; } = new();
}
