using System;
using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models
{

    public class AbonosMeta
    {
        public int ID { get; set; }
        public int META_ID { get; set; }
        public decimal MONTO { get; set; }
        public DateTime FECHA { get; set; }
        // Propiedad para mostrar el nombre o tipo de meta
        public string MetaNombre { get; set; }
        public decimal OBJETIVO { get; set; }
    }


    public class AbonoMeta
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "Seleccione una meta.")]
        public int META_ID { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor que cero.")]
        public decimal MONTO { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime FECHA { get; set; }
    }
}
