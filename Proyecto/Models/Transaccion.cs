using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;


namespace Proyecto.Models
{
    public class Transaccion
    {
        public int ID { get; set; }             // Mapea la columna ID (IDENTITY en la BD)

        [Required(ErrorMessage = "El tipo de transacción es requerido.")]
        public string TIPO { get; set; }        // Mapea la columna TIPO

        [Required(ErrorMessage = "La categoría es requerida.")]
        public int CATEGORIAID { get; set; }    // Mapea la columna CATEGORIA_ID

        [Required(ErrorMessage = "La fecha es requerida.")]
        public DateTime FECHA { get; set; }     // Mapea la columna FECHA

        [Required(ErrorMessage = "La factura es requerida.")]
        public string NUMEROFACTURA { get; set; } // Mapea la columna NUMERO_FACTURA
        public string COMENTARIO { get; set; }  // Mapea la columna COMENTARIO

        [Required(ErrorMessage = "El monto es requerido.")]
        public decimal MONTO { get; set; }      // Mapea la columna MONTO

        // Propiedad de navegación opcional para mostrar el nombre de la categoría
        public Categoria Categoria { get; set; }
    }
    //public class Transaccion
    //{
    //    [Key]
    //    public int ID { get; set; }

    //    [Required(ErrorMessage = "El tipo de transacción es requerido.")]
    //    [StringLength(10)]
    //    public string TIPO { get; set; } // "INGRESO" o "GASTO"

    //    [Required(ErrorMessage = "La categoría es requerida.")]
    //    public int CATEGORIA_ID { get; set; }

    //    [ForeignKey("CATEGORIA_ID")]
    //    public virtual Categoria CATEGORIA { get; set; }

    //    [Required(ErrorMessage = "La fecha es requerida.")]
    //    public DateTime FECHA { get; set; }

    //    [StringLength(50)]
    //    public string NUMERO_FACTURA { get; set; }

    //    [StringLength(500)]
    //    public string COMENTARIO { get; set; }

    //    [Required(ErrorMessage = "El monto es requerido.")]
    //    public decimal MONTO { get; set; }

    //    // Propiedad para mostrar el nombre de la categoría (obtenida mediante JOIN)
    //    public Categoria Categoria { get; set; }
    //}

    public class TransaccionViewModel
    {
        public Transaccion NuevaTransaccion { get; set; }
        public IEnumerable<Transaccion> Transacciones { get; set; }
        public SelectList ListaCategorias { get; set; }
    }
}