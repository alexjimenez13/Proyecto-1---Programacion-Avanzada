using System;
using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }


        [Required(ErrorMessage = "El correo electrónico es requerido")]
        [EmailAddress(ErrorMessage = "Debes ingresar un correo válido")]
        [StringLength(150)]
        public string CORREO { get; set; }

        [Required]
        [StringLength(100)]
        public string NOMBRE_USUARIO { get; set; }

        [Required]
        [StringLength(100)]
        public string NOMBRE { get; set; }

        [Required]
        [StringLength(100)]
        public string PRIMER_APELLIDO { get; set; }

        [Required]
        [StringLength(255)]
        public string SEGUNDO_APELLIDO { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(255)]
        public string CONTRASENA { get; set; }

        [StringLength(255)]
        public string TOKEN_RECUPERACION { get; set; }
    }

}