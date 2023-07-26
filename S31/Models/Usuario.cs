using System.ComponentModel.DataAnnotations;

namespace S31.Models
{
	public class Usuario
	{
		[Required]
		public string? Nombre { get; set; }
		[Required]
		public string? Contraseña { get; set; }
		[Required]
		public string? Cubeta { get; set; }
    }
}
