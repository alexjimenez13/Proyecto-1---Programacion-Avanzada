using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using Proyecto.Models;

namespace Proyecto.Controllers
{
    public class PresupuestoController : Controller
    {
        // ===================== Acciones Públicas =====================

        // Acción que muestra la vista principal del presupuesto para el usuario actual.
        // Si el usuario no está logueado, lo redirige a la página de login.
        public ActionResult Presupuesto()
        {
            if (Session["UsuarioId"] == null)
                return RedirectToAction("Login", "Usuario");

            int usuarioId = (int)Session["UsuarioId"];

            // Se arma el ViewModel con:
            // - El presupuesto del mes actual.
            // - Las metas registradas del usuario.
            // - Los presupuestos históricos.
            var viewModel = new PresupuestoViewModel
            {
                Presupuesto = ObtenerPresupuestoMesActual(usuarioId),
                Metas = ObtenerMetasUsuario(usuarioId),
                PresupuestosHistoricos = ObtenerTodosLosPresupuestos(usuarioId)
            };

            return View(viewModel);
        }

        // Acción POST que actualiza o inserta el presupuesto del usuario.
        // Si ya existe un registro para el mes y año, se actualiza; de lo contrario, se inserta uno nuevo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Actualizar(PresupuestoViewModel viewModel)
        {
            int usuarioId = Convert.ToInt32(Session["UsuarioId"]);
            int mes = viewModel.Presupuesto.MES;
            int año = viewModel.Presupuesto.AÑO;

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                con.Open();
                // Verifica si existe un presupuesto para el mes y año del usuario.
                string queryCheck = "SELECT COUNT(*) FROM PROY_PRESUPUESTOS WHERE USUARIO_ID = @USUARIO_ID AND MES = @MES AND AÑO = @AÑO";
                using (SqlCommand cmdCheck = new SqlCommand(queryCheck, con))
                {
                    cmdCheck.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                    cmdCheck.Parameters.AddWithValue("@MES", mes);
                    cmdCheck.Parameters.AddWithValue("@AÑO", año);
                    int count = (int)cmdCheck.ExecuteScalar();

                    if (count > 0)
                    {
                        // Actualiza el presupuesto existente.
                        string queryUpdate = @"UPDATE PROY_PRESUPUESTOS 
                                               SET MONTO_INGRESOS = @MONTO_INGRESOS, 
                                                   MONTO_GASTOS = @MONTO_GASTOS, 
                                                   PRESUPUESTO_MENSUAL = @PRESUPUESTO_MENSUAL
                                               WHERE USUARIO_ID = @USUARIO_ID AND MES = @MES AND AÑO = @AÑO";
                        using (SqlCommand cmdUpdate = new SqlCommand(queryUpdate, con))
                        {
                            cmdUpdate.Parameters.AddWithValue("@MONTO_INGRESOS", viewModel.Presupuesto.MONTO_INGRESOS);
                            cmdUpdate.Parameters.AddWithValue("@MONTO_GASTOS", viewModel.Presupuesto.MONTO_GASTOS);
                            // Se calcula el presupuesto mensual como la diferencia entre ingresos y gastos.
                            decimal presupuestoCalculado = viewModel.Presupuesto.MONTO_INGRESOS - viewModel.Presupuesto.MONTO_GASTOS;
                            cmdUpdate.Parameters.AddWithValue("@PRESUPUESTO_MENSUAL", presupuestoCalculado);
                            cmdUpdate.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                            cmdUpdate.Parameters.AddWithValue("@MES", mes);
                            cmdUpdate.Parameters.AddWithValue("@AÑO", año);
                            cmdUpdate.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Inserta un nuevo presupuesto si no existe uno para el mes y año.
                        string queryInsert = @"INSERT INTO PROY_PRESUPUESTOS (USUARIO_ID, MES, AÑO, MONTO_INGRESOS, MONTO_GASTOS, PRESUPUESTO_MENSUAL)
                                               VALUES (@USUARIO_ID, @MES, @AÑO, @MONTO_INGRESOS, @MONTO_GASTOS, @PRESUPUESTO_MENSUAL)";
                        using (SqlCommand cmdInsert = new SqlCommand(queryInsert, con))
                        {
                            cmdInsert.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                            cmdInsert.Parameters.AddWithValue("@MES", mes);
                            cmdInsert.Parameters.AddWithValue("@AÑO", año);
                            cmdInsert.Parameters.AddWithValue("@MONTO_INGRESOS", viewModel.Presupuesto.MONTO_INGRESOS);
                            cmdInsert.Parameters.AddWithValue("@MONTO_GASTOS", viewModel.Presupuesto.MONTO_GASTOS);
                            decimal presupuestoCalculado = viewModel.Presupuesto.MONTO_INGRESOS - viewModel.Presupuesto.MONTO_GASTOS;
                            cmdInsert.Parameters.AddWithValue("@PRESUPUESTO_MENSUAL", presupuestoCalculado);
                            cmdInsert.ExecuteNonQuery();
                        }
                    }
                }
            }
            return RedirectToAction("Presupuesto");
        }

        // Acción que muestra la vista para registrar una nueva meta.
        public ActionResult RegistrarMeta()
        {
            return View();
        }

        // Acción POST que registra una nueva meta para el usuario en la base de datos.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarMeta(Meta meta)
        {
            if (ModelState.IsValid)
            {
                int usuarioId = Convert.ToInt32(Session["UsuarioId"]);
                meta.USUARIO_ID = usuarioId;
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
                {
                    string query = @"INSERT INTO PROY_METAS (USUARIO_ID, TIPO_META, MONTO_OBJETIVO, DESCRIPCION, FECHA_CUMPLIMIENTO, CUMPLIDA)
                             VALUES (@USUARIO_ID, @TIPO_META, @MONTO_OBJETIVO, @DESCRIPCION, @FECHA_CUMPLIMIENTO, 0)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@USUARIO_ID", meta.USUARIO_ID);
                        cmd.Parameters.AddWithValue("@TIPO_META", meta.TIPO_META);
                        cmd.Parameters.AddWithValue("@MONTO_OBJETIVO", meta.MONTO_OBJETIVO);
                        cmd.Parameters.AddWithValue("@DESCRIPCION", string.IsNullOrEmpty(meta.DESCRIPCION) ? (object)DBNull.Value : meta.DESCRIPCION);
                        cmd.Parameters.AddWithValue("@FECHA_CUMPLIMIENTO", meta.FECHA_CUMPLIMIENTO);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("VerMetas");
            }
            return View(meta);
        }

        public ActionResult VerMetas()
        {
            if (Session["UsuarioId"] == null)
                return RedirectToAction("Login", "Usuario");

            int usuarioId = Convert.ToInt32(Session["UsuarioId"]);
            var metas = ObtenerMetasUsuario(usuarioId);
            return View(metas);
        }



        // Acción que muestra la vista para comparar presupuestos.
        public ActionResult Comparar()
        {
            return View();
        }

        // Acción que compara el presupuesto registrado con las transacciones reales de un mes y año específicos.
        public ActionResult CompararPresupuesto(int mes, int año)
        {
            if (Session["UsuarioId"] == null)
                return RedirectToAction("Login", "Usuario");

            int usuarioId = (int)Session["UsuarioId"];
            // Obtiene el presupuesto para el mes y año indicados.
            var presupuesto = ObtenerPresupuestoMes(usuarioId, mes, año);

            // Si no existe un presupuesto, se crea uno por defecto.
            if (presupuesto == null)
            {
                presupuesto = new Presupuesto
                {
                    USUARIO_ID = usuarioId,
                    MES = mes,
                    AÑO = año,
                    MONTO_INGRESOS = 0,
                    MONTO_GASTOS = 0,
                    PRESUPUESTO_MENSUAL = 0
                };
            }

            // Calcula la diferencia entre lo presupuestado y lo real.
            var (diferenciaIngresos, diferenciaGastos) = CalcularDiferenciaPresupuesto(usuarioId, mes, año);

            ViewBag.DiferenciaIngresos = diferenciaIngresos;
            ViewBag.DiferenciaGastos = diferenciaGastos;
            ViewBag.Mes = mes;
            ViewBag.Año = año;

            return View(presupuesto);
        }

        // ===================== Métodos Privados de Ayuda =====================

        // Método que obtiene el presupuesto del mes actual para el usuario.
        // Si no existe en la BD, retorna un presupuesto con valores predeterminados (cero).
        private Presupuesto ObtenerPresupuestoMesActual(int usuarioId)
        {
            Presupuesto presupuesto = null;
            int mesActual = DateTime.Now.Month;
            int añoActual = DateTime.Now.Year;

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = "SELECT * FROM PROY_PRESUPUESTOS WHERE USUARIO_ID = @USUARIO_ID AND MES = @MES AND AÑO = @AÑO";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                    cmd.Parameters.AddWithValue("@MES", mesActual);
                    cmd.Parameters.AddWithValue("@AÑO", añoActual);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            presupuesto = new Presupuesto
                            {
                                ID = dr.GetInt32(dr.GetOrdinal("ID")),
                                USUARIO_ID = dr.GetInt32(dr.GetOrdinal("USUARIO_ID")),
                                MES = dr.GetInt32(dr.GetOrdinal("MES")),
                                AÑO = dr.GetInt32(dr.GetOrdinal("AÑO")),
                                MONTO_INGRESOS = dr.GetDecimal(dr.GetOrdinal("MONTO_INGRESOS")),
                                MONTO_GASTOS = dr.GetDecimal(dr.GetOrdinal("MONTO_GASTOS")),
                                PRESUPUESTO_MENSUAL = dr.GetDecimal(dr.GetOrdinal("PRESUPUESTO_MENSUAL"))
                            };
                        }
                    }
                }
            }

            if (presupuesto == null)
            {
                presupuesto = new Presupuesto
                {
                    USUARIO_ID = usuarioId,
                    MES = mesActual,
                    AÑO = añoActual,
                    MONTO_INGRESOS = 0,
                    MONTO_GASTOS = 0,
                    PRESUPUESTO_MENSUAL = 0
                };
            }

            return presupuesto;
        }

        // Método que obtiene la lista de metas del usuario desde la BD.
        private List<Meta> ObtenerMetasUsuario(int usuarioId)
        {
            var metas = new List<Meta>();

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = "SELECT * FROM PROY_METAS WHERE USUARIO_ID = @USUARIO_ID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            metas.Add(new Meta
                            {
                                ID = dr.GetInt32(dr.GetOrdinal("ID")),
                                USUARIO_ID = dr.GetInt32(dr.GetOrdinal("USUARIO_ID")),
                                TIPO_META = dr.GetString(dr.GetOrdinal("TIPO_META")),
                                MONTO_OBJETIVO = dr.GetDecimal(dr.GetOrdinal("MONTO_OBJETIVO")),
                                DESCRIPCION = dr.IsDBNull(dr.GetOrdinal("DESCRIPCION")) ? "" : dr.GetString(dr.GetOrdinal("DESCRIPCION")),
                                CUMPLIDA = dr.GetBoolean(dr.GetOrdinal("CUMPLIDA")),
                                FECHA_CUMPLIMIENTO = dr.GetDateTime(dr.GetOrdinal("FECHA_CUMPLIMIENTO"))
                            });
                        }
                    }
                }
            }
            return metas;
        }

        // Método que obtiene todos los presupuestos históricos del usuario, ordenados de más reciente a más antiguo.
        private List<Presupuesto> ObtenerTodosLosPresupuestos(int usuarioId)
        {
            var lista = new List<Presupuesto>();

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
            SELECT ID, USUARIO_ID, MES, AÑO, MONTO_INGRESOS, MONTO_GASTOS, PRESUPUESTO_MENSUAL
            FROM PROY_PRESUPUESTOS
            WHERE USUARIO_ID = @USUARIO_ID
            ORDER BY AÑO DESC, MES DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new Presupuesto
                            {
                                ID = dr.GetInt32(dr.GetOrdinal("ID")),
                                USUARIO_ID = dr.GetInt32(dr.GetOrdinal("USUARIO_ID")),
                                MES = dr.GetInt32(dr.GetOrdinal("MES")),
                                AÑO = dr.GetInt32(dr.GetOrdinal("AÑO")),
                                MONTO_INGRESOS = dr.GetDecimal(dr.GetOrdinal("MONTO_INGRESOS")),
                                MONTO_GASTOS = dr.GetDecimal(dr.GetOrdinal("MONTO_GASTOS")),
                                PRESUPUESTO_MENSUAL = dr.GetDecimal(dr.GetOrdinal("PRESUPUESTO_MENSUAL"))
                            });
                        }
                    }
                }
            }
            return lista;
        }

        // Método que obtiene el presupuesto de un mes y año específicos para el usuario.
        private Presupuesto ObtenerPresupuestoMes(int usuarioId, int mes, int año)
        {
            Presupuesto presupuesto = null;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
            SELECT TOP 1 ID, USUARIO_ID, MES, AÑO, MONTO_INGRESOS, MONTO_GASTOS, PRESUPUESTO_MENSUAL
            FROM PROY_PRESUPUESTOS
            WHERE USUARIO_ID = @USUARIO_ID AND MES = @MES AND AÑO = @AÑO";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                    cmd.Parameters.AddWithValue("@MES", mes);
                    cmd.Parameters.AddWithValue("@AÑO", año);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            presupuesto = new Presupuesto
                            {
                                ID = dr.GetInt32(dr.GetOrdinal("ID")),
                                USUARIO_ID = dr.GetInt32(dr.GetOrdinal("USUARIO_ID")),
                                MES = dr.GetInt32(dr.GetOrdinal("MES")),
                                AÑO = dr.GetInt32(dr.GetOrdinal("AÑO")),
                                MONTO_INGRESOS = dr.GetDecimal(dr.GetOrdinal("MONTO_INGRESOS")),
                                MONTO_GASTOS = dr.GetDecimal(dr.GetOrdinal("MONTO_GASTOS")),
                                PRESUPUESTO_MENSUAL = dr.GetDecimal(dr.GetOrdinal("PRESUPUESTO_MENSUAL"))
                            };
                        }
                    }
                }
            }
            return presupuesto;
        }

        // Método que obtiene la suma de ingresos y gastos de las transacciones del usuario en un mes y año específicos.
        private void ObtenerSumaTransacciones(int usuarioId, int mes, int año, out decimal sumaIngresos, out decimal sumaGastos)
        {
            sumaIngresos = 0;
            sumaGastos = 0;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
            SELECT TIPO, SUM(MONTO) AS Total 
            FROM PROY_TRANSACCIONES
            WHERE USUARIO_ID = @USUARIO_ID 
              AND MONTH(FECHA) = @MES
              AND YEAR(FECHA) = @AÑO
            GROUP BY TIPO";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                    cmd.Parameters.AddWithValue("@MES", mes);
                    cmd.Parameters.AddWithValue("@AÑO", año);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string tipo = dr["TIPO"].ToString();
                            decimal total = dr["Total"] != DBNull.Value ? Convert.ToDecimal(dr["Total"]) : 0;
                            if (tipo.Equals("INGRESO", StringComparison.OrdinalIgnoreCase))
                            {
                                sumaIngresos = total;
                            }
                            else if (tipo.Equals("GASTO", StringComparison.OrdinalIgnoreCase))
                            {
                                sumaGastos = total;
                            }
                        }
                    }
                }
            }
        }

        // Método que calcula la diferencia entre lo presupuestado y lo real (transacciones) para ingresos y gastos.
        private (decimal diferenciaIngresos, decimal diferenciaGastos) CalcularDiferenciaPresupuesto(int usuarioId, int mes, int año)
        {
            // Se obtiene el presupuesto registrado para el mes y año.
            Presupuesto presupuesto = null;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
            SELECT TOP 1 MES, AÑO, MONTO_INGRESOS, MONTO_GASTOS, PRESUPUESTO_MENSUAL
            FROM PROY_PRESUPUESTOS
            WHERE USUARIO_ID = @USUARIO_ID AND MES = @MES AND AÑO = @AÑO";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                    cmd.Parameters.AddWithValue("@MES", mes);
                    cmd.Parameters.AddWithValue("@AÑO", año);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            presupuesto = new Presupuesto
                            {
                                MES = dr.GetInt32(dr.GetOrdinal("MES")),
                                AÑO = dr.GetInt32(dr.GetOrdinal("AÑO")),
                                MONTO_INGRESOS = dr.GetDecimal(dr.GetOrdinal("MONTO_INGRESOS")),
                                MONTO_GASTOS = dr.GetDecimal(dr.GetOrdinal("MONTO_GASTOS")),
                                PRESUPUESTO_MENSUAL = dr.GetDecimal(dr.GetOrdinal("PRESUPUESTO_MENSUAL"))
                            };
                        }
                    }
                }
            }

            if (presupuesto == null)
            {
                // No hay presupuesto registrado para ese mes y año.
                return (0, 0);
            }

            // Se obtienen las sumas reales de ingresos y gastos a partir de las transacciones.
            decimal sumaIngresos, sumaGastos;
            ObtenerSumaTransacciones(usuarioId, mes, año, out sumaIngresos, out sumaGastos);

            // Se calcula la diferencia: valor presupuestado menos el valor real.
            decimal diferenciaIngresos = presupuesto.MONTO_INGRESOS - sumaIngresos;
            decimal diferenciaGastos = presupuesto.MONTO_GASTOS - sumaGastos;

            return (diferenciaIngresos, diferenciaGastos);
        }

        // Método privado para obtener una meta por su ID
        private Meta ObtenerMetaById(int id)
        {
            Meta meta = null;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = "SELECT * FROM PROY_METAS WHERE ID = @ID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            meta = new Meta
                            {
                                ID = dr.GetInt32(dr.GetOrdinal("ID")),
                                USUARIO_ID = dr.GetInt32(dr.GetOrdinal("USUARIO_ID")),
                                TIPO_META = dr.GetString(dr.GetOrdinal("TIPO_META")),
                                MONTO_OBJETIVO = dr.GetDecimal(dr.GetOrdinal("MONTO_OBJETIVO")),
                                DESCRIPCION = dr.IsDBNull(dr.GetOrdinal("DESCRIPCION")) ? "" : dr.GetString(dr.GetOrdinal("DESCRIPCION")),
                                CUMPLIDA = dr.GetBoolean(dr.GetOrdinal("CUMPLIDA"))
                            };
                        }
                    }
                }
            }
            return meta;
        }

        public ActionResult RegistrarAbono()
        {
            int usuarioId = Convert.ToInt32(Session["UsuarioId"]);
            List<Proyecto.Models.Meta> metasUsuario = new List<Proyecto.Models.Meta>();

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = "SELECT ID, TIPO_META FROM PROY_METAS WHERE USUARIO_ID = @USUARIO_ID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            metasUsuario.Add(new Proyecto.Models.Meta
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                TIPO_META = reader["TIPO_META"].ToString()
                            });
                        }
                    }
                }
            }
            ViewBag.Metas = new SelectList(metasUsuario, "ID", "TIPO_META");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarAbono(Proyecto.Models.AbonoMeta abono)
        {
            if (ModelState.IsValid)
            {
                int usuarioId = Convert.ToInt32(Session["UsuarioId"]);

                // Validar que la meta seleccionada pertenezca al usuario
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
                {
                    string queryCheck = "SELECT COUNT(*) FROM PROY_METAS WHERE ID = @META_ID AND USUARIO_ID = @USUARIO_ID";
                    using (SqlCommand cmdCheck = new SqlCommand(queryCheck, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@META_ID", abono.META_ID);
                        cmdCheck.Parameters.AddWithValue("@USUARIO_ID", usuarioId);
                        con.Open();
                        int count = (int)cmdCheck.ExecuteScalar();
                        if (count == 0)
                        {
                            ModelState.AddModelError("", "La meta seleccionada no pertenece a su cuenta.");
                            // Repoblar el dropdown para volver a mostrar la vista
                            // (se puede reutilizar el código del GET)
                            return View(abono);
                        }
                    }
                }

                // Insertar el abono en la tabla PROY_ABONOS
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
                {
                    string query = @"INSERT INTO PROY_ABONOS (META_ID, MONTO, FECHA)
                               VALUES (@META_ID, @MONTO, @FECHA)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@META_ID", abono.META_ID);
                        cmd.Parameters.AddWithValue("@MONTO", abono.MONTO);
                        cmd.Parameters.AddWithValue("@FECHA", abono.FECHA);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("VerMetas"); 
            }
            return View(abono);
        }





       



    }
}



