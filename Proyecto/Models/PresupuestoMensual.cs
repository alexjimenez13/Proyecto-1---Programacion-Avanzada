using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Proyecto.Models
{
        public class Presupuesto
        {
            public int ID { get; set; }
            public int USUARIO_ID { get; set; }
            public int MES { get; set; }
            public int AÑO { get; set; }
            public decimal MONTO_INGRESOS { get; set; }
            public decimal MONTO_GASTOS { get; set; }
            public decimal PRESUPUESTO_MENSUAL { get; set; }
        }

        public class Meta
        {
            public int ID { get; set; }
            public int USUARIO_ID { get; set; }
            public string TIPO_META { get; set; }
            public decimal MONTO_OBJETIVO { get; set; }
            public string DESCRIPCION { get; set; }
            public bool CUMPLIDA { get; set; }

            [Required(ErrorMessage = "La fecha de cumplimiento es obligatoria")]
            [DataType(DataType.Date)]
            public DateTime FECHA_CUMPLIMIENTO { get; set; }
        }

        public class PresupuestoViewModel
        {
            public Presupuesto Presupuesto { get; set; }
            public List<Meta> Metas { get; set; }
            public List<Presupuesto> PresupuestosHistoricos { get; set; }
        }
    
}