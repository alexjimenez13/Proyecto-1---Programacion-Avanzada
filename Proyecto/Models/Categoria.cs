using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models
{
    public class Categoria
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es requerido.")]
        [StringLength(100)]
        public string NOMBRE { get; set; }

        [StringLength(255)]
        public string DESCRIPCION { get; set; }

        [Required(ErrorMessage = "El tipo de categoría es requerido.")]
        [StringLength(10)]
        public string TIPO { get; set; } 
    }
}