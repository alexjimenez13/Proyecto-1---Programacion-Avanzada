using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using Proyecto.Models;

namespace Proyecto.Controllers
{
    public class PresupuestosController : Controller
    {
        // GET: Presupuestos/Crear
        public ActionResult Crear()
        {
            return View();
        }

        // POST: Presupuestos/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(PresupuestoMensual modelo)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
                {
                    string query = @"INSERT INTO PROY_PRESUPUESTOS (MES, AÑO, MONTO_PRESUPUESTO, GASTOS_REALES, INGRESOS_REALES)
                                     VALUES (@MES, @AÑO, @MONTO_PRESUPUESTO, 0, 0)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@MES", modelo.Mes);
                        cmd.Parameters.AddWithValue("@AÑO", modelo.Año);
                        cmd.Parameters.AddWithValue("@MONTO_PRESUPUESTO", modelo.MontoPresupuesto);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Listar");
            }
            return View(modelo);
        }

        // GET: Presupuestos/Listar
        public ActionResult Listar()
        {
            List<PresupuestoMensual> presupuestos = new List<PresupuestoMensual>();

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = "SELECT Id, MES, AÑO, MONTO_PRESUPUESTO, GASTOS_REALES, INGRESOS_REALES FROM PROY_PRESUPUESTOS";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            presupuestos.Add(new PresupuestoMensual
                            {
                                Id = dr.GetInt32(0),
                                Mes = dr.GetInt32(1),
                                Año = dr.GetInt32(2),
                                MontoPresupuesto = dr.GetDecimal(3),
                                GastosReales = dr.GetDecimal(4),
                                IngresosReales = dr.GetDecimal(5)
                            });
                        }
                    }
                }
            }

            return View(presupuestos);
        }
    }
}
