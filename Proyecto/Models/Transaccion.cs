using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Proyecto.Models
{
    public class Transaccion
    {
        public int ID { get; set; }            

        [Required(ErrorMessage = "El tipo de transacción es requerido.")]
        public string TIPO { get; set; }        

        [Required(ErrorMessage = "La categoría es requerida.")]
        public int CATEGORIAID { get; set; }   

        [Required(ErrorMessage = "La fecha es requerida.")]
        public DateTime FECHA { get; set; }     

        [Required(ErrorMessage = "La factura es requerida.")]
        public string NUMEROFACTURA { get; set; } 
        public string COMENTARIO { get; set; }  

        [Required(ErrorMessage = "El monto es requerido.")]
        public decimal MONTO { get; set; }      
        public Categoria Categoria { get; set; }
        public int USUARIO_ID { get; set; }
    }

    public class TransaccionViewModel
    {
        public Transaccion NuevaTransaccion { get; set; }
        public IEnumerable<Transaccion> Transacciones { get; set; }
        public SelectList ListaCategorias { get; set; }
    }
}