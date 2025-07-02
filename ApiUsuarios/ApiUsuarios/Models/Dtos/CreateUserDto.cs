using System.ComponentModel.DataAnnotations;

namespace Models.Dtos
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string Password { get; set; } = string.Empty;

        // Opcional: Si el admin puede definir el rol al crear.
        // Por ahora, el rol se tomará del default en el modelo User o se podría setear explícitamente en el controlador.
        // public string Role { get; set; } = "Agricultor";
    }
}
