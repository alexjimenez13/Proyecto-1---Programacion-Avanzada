using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto.Models
{
    public class PresupuestoMensual
    {
        public int Id { get; set; }
        public int Mes { get; set; } // Valores de 1 a 12
        public int Año { get; set; }
        public decimal MontoPresupuesto { get; set; } // Presupuesto asignado
        public decimal GastosReales { get; set; }      // Gastos acumulados
        public decimal IngresosReales { get; set; }      // Ingresos acumulados (opcional)
    }
}