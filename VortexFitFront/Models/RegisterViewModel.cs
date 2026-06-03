using System.ComponentModel.DataAnnotations;

namespace VortexFit.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "Máximo 50 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [Phone(ErrorMessage = "Ingresa un teléfono válido.")]
    [StringLength(15, MinimumLength = 9, ErrorMessage = "Entre 9 y 15 dígitos.")]
    [Display(Name = "Teléfono")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un plan.")]
    [Display(Name = "Plan de membresía")]
    public string Plan { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres.")]
    [RegularExpression(
        @"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
        ErrorMessage = "Debe tener al menos 1 mayúscula, 1 número y 1 carácter especial (ej. !@#$).")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Debes aceptar los términos y condiciones.")]
    [Display(Name = "Acepto los términos y condiciones")]
    public bool AcceptTerms { get; set; }

    // Token invisible de reCAPTCHA v3 — se llena por JS antes del submit
    public string RecaptchaToken { get; set; } = string.Empty;
}
