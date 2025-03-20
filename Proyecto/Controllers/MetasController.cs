using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using Proyecto.Models;

namespace Proyecto.Controllers
{
    public class MetasController : Controller
    {
        // GET: Metas/Crear
        public ActionResult Crear()
        {
            return View();
        }

        // POST: Metas/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(MetaFinanciera meta)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
                {
                    string query = @"INSERT INTO PROY_METAS_FINANCIERAS (DESCRIPCION, MONTO_OBJETIVO, PROGRESO_ACTUAL, FECHA_LIMITE, ALCANZADA)
                                     VALUES (@DESCRIPCION, @MONTO_OBJETIVO, 0, @FECHA_LIMITE, 0)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DESCRIPCION", meta.Descripcion);
                        cmd.Parameters.AddWithValue("@MONTO_OBJETIVO", meta.MontoObjetivo);
                        cmd.Parameters.AddWithValue("@FECHA_LIMITE", meta.FechaLimite);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("Listar");
            }
            return View(meta);
        }

        // GET: Metas/Listar
        public ActionResult Listar()
        {
            List<MetaFinanciera> metas = new List<MetaFinanciera>();

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = "SELECT Id, DESCRIPCION, MONTO_OBJETIVO, PROGRESO_ACTUAL, FECHA_LIMITE, ALCANZADA FROM PROY_METAS_FINANCIERAS";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            metas.Add(new MetaFinanciera
                            {
                                Id = dr.GetInt32(0),
                                Descripcion = dr.GetString(1),
                                MontoObjetivo = dr.GetDecimal(2),
                                ProgresoActual = dr.GetDecimal(3),
                                FechaLimite = dr.GetDateTime(4),
                                Alcanzada = dr.GetBoolean(5)
                            });
                        }
                    }
                }
            }

            return View(metas);
        }
    }
}
