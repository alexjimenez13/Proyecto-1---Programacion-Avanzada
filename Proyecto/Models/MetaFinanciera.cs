using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto.Models
{
    public class MetaFinanciera
    {
        public int Id { get; set; }
        public string Descripcion { get; set; }
        public decimal MontoObjetivo { get; set; }
        public decimal ProgresoActual { get; set; }
        public DateTime FechaLimite { get; set; }
        public bool Alcanzada { get; set; }
    }
}