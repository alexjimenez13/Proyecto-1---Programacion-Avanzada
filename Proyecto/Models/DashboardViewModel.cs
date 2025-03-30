using System;
using System.Collections.Generic;

namespace Proyecto.Models
{
    public class DashboardViewModel
    {
        public decimal TotalIngresos { get; set; }          // Ingresos reales del mes
        public decimal TotalGastos { get; set; }            // Gastos reales (transacciones + abonos)
        public decimal PresupuestoMensual { get; set; }       // Presupuesto global del mes
        public decimal PresupuestoIngresos { get; set; }      // Presupuesto establecido para ingresos
        public decimal PresupuestoGastos { get; set; }        // Presupuesto establecido para gastos

        public List<Transaccion> Transacciones { get; set; }
        public List<Meta> Metas { get; set; }
        public List<AbonosMeta> Abonos { get; set; }          // Lista de abonos realizados
        public Dictionary<string, decimal> GastosPorCategoria { get; set; }
        public DateTime FechaSeleccionada { get; set; }
    }

}
