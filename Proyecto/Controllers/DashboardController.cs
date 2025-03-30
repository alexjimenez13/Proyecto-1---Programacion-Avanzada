using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using Proyecto.Models;

namespace Proyecto.Controllers
{
    public class DashboardController : Controller
    {
        public ActionResult Dashboard()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Usuario");
            }

            int usuarioId = Convert.ToInt32(Session["UsuarioId"]);
            DateTime fechaActual = DateTime.Now; // Puedes permitir seleccionar otro mes si lo requieres

            // Obtener ingresos y gastos reales
            decimal totalIngresos = ObtenerTotalIngresos(usuarioId, fechaActual);
            decimal totalGastosTransacciones = ObtenerTotalGastos(usuarioId, fechaActual);
            decimal totalAbonos = ObtenerTotalAbonos(usuarioId, fechaActual);
            decimal totalGastos = totalGastosTransacciones + totalAbonos;

            DashboardViewModel model = new DashboardViewModel
            {
                FechaSeleccionada = fechaActual,
                TotalIngresos = totalIngresos,
                TotalGastos = totalGastos,
                PresupuestoMensual = ObtenerPresupuestoMensual(usuarioId, fechaActual),
                PresupuestoIngresos = ObtenerPresupuestoIngresos(usuarioId, fechaActual),
                PresupuestoGastos = ObtenerPresupuestoGastos(usuarioId, fechaActual),
                Transacciones = ObtenerTransacciones(usuarioId, fechaActual),
                Metas = ObtenerMetas(usuarioId),
                Abonos = ObtenerAbonosLista(usuarioId, fechaActual),
                GastosPorCategoria = ObtenerGastosPorCategoria(usuarioId, fechaActual)
            };

            return View(model);
        }

        // Ingresos reales del mes
        private decimal ObtenerTotalIngresos(int usuarioId, DateTime fecha)
        {
            decimal total = 0;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT ISNULL(SUM(MONTO),0)
                    FROM PROY_TRANSACCIONES 
                    WHERE USUARIO_ID = @UsuarioId 
                      AND TIPO = 'Ingreso' 
                      AND MONTH(FECHA) = @Mes 
                      AND YEAR(FECHA) = @Anio";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@Mes", fecha.Month);
                    cmd.Parameters.AddWithValue("@Anio", fecha.Year);
                    con.Open();
                    total = Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
            return total;
        }

        // Gastos reales (excluyendo abonos) del mes
        private decimal ObtenerTotalGastos(int usuarioId, DateTime fecha)
        {
            decimal total = 0;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT ISNULL(SUM(MONTO),0)
                    FROM PROY_TRANSACCIONES 
                    WHERE USUARIO_ID = @UsuarioId 
                      AND TIPO = 'Gasto'
                      AND MONTH(FECHA) = @Mes 
                      AND YEAR(FECHA) = @Anio";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@Mes", fecha.Month);
                    cmd.Parameters.AddWithValue("@Anio", fecha.Year);
                    con.Open();
                    total = Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
            return total;
        }

        // Suma total de los abonos a metas realizados en el mes
        private decimal ObtenerTotalAbonos(int usuarioId, DateTime fecha)
        {
            decimal total = 0;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT ISNULL(SUM(A.MONTO),0)
                    FROM PROY_ABONOS A
                    INNER JOIN PROY_METAS M ON A.META_ID = M.ID
                    WHERE M.USUARIO_ID = @UsuarioId
                      AND MONTH(A.FECHA) = @Mes
                      AND YEAR(A.FECHA) = @Anio";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@Mes", fecha.Month);
                    cmd.Parameters.AddWithValue("@Anio", fecha.Year);
                    con.Open();
                    total = Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
            return total;
        }

        // Lista de abonos realizados (detalle)
        private List<AbonosMeta> ObtenerAbonosLista(int usuarioId, DateTime fecha)
        {
            List<AbonosMeta> abonos = new List<AbonosMeta>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT A.ID, A.META_ID, A.MONTO, A.FECHA, M.TIPO_META
                    FROM PROY_ABONOS A
                    INNER JOIN PROY_METAS M ON A.META_ID = M.ID
                    WHERE M.USUARIO_ID = @UsuarioId
                      AND MONTH(A.FECHA) = @Mes
                      AND YEAR(A.FECHA) = @Anio";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@Mes", fecha.Month);
                    cmd.Parameters.AddWithValue("@Anio", fecha.Year);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            AbonosMeta abono = new AbonosMeta
                            {
                                ID = dr.GetInt32(0),
                                META_ID = dr.GetInt32(1),
                                MONTO = dr.GetDecimal(2),
                                FECHA = dr.GetDateTime(3),
                                MetaNombre = dr.IsDBNull(4) ? string.Empty : dr.GetString(4)
                            };
                            abonos.Add(abono);
                        }
                    }
                }
            }
            return abonos;
        }

        // Lista de transacciones del mes
        private List<Transaccion> ObtenerTransacciones(int usuarioId, DateTime fecha)
        {
            List<Transaccion> transacciones = new List<Transaccion>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT T.ID, T.TIPO, T.CATEGORIA_ID, T.FECHA, T.NUMERO_FACTURA,
                           T.COMENTARIO, T.MONTO, C.NOMBRE AS CategoriaNombre
                    FROM PROY_TRANSACCIONES T
                    LEFT JOIN PROY_CATEGORIAS C ON T.CATEGORIA_ID = C.ID
                    WHERE T.USUARIO_ID = @UsuarioId 
                      AND MONTH(T.FECHA) = @Mes 
                      AND YEAR(T.FECHA) = @Anio";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@Mes", fecha.Month);
                    cmd.Parameters.AddWithValue("@Anio", fecha.Year);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Transaccion t = new Transaccion
                            {
                                ID = dr.GetInt32(0),
                                TIPO = dr.GetString(1),
                                CATEGORIAID = dr.GetInt32(2),
                                FECHA = dr.GetDateTime(3),
                                NUMEROFACTURA = dr.IsDBNull(4) ? null : dr.GetString(4),
                                COMENTARIO = dr.IsDBNull(5) ? null : dr.GetString(5),
                                MONTO = dr.GetDecimal(6),
                                Categoria = new Categoria
                                {
                                    NOMBRE = dr.IsDBNull(7) ? "" : dr.GetString(7)
                                }
                            };
                            transacciones.Add(t);
                        }
                    }
                }
            }
            return transacciones;
        }

        // Lista de metas del usuario
        private List<Meta> ObtenerMetas(int usuarioId)
        {
            List<Meta> metas = new List<Meta>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT ID, TIPO_META, MONTO_OBJETIVO, FECHA_CUMPLIMIENTO 
                    FROM PROY_METAS 
                    WHERE USUARIO_ID = @UsuarioId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Meta m = new Meta
                            {
                                ID = dr.GetInt32(0),
                                TIPO_META = dr.GetString(1),
                                MONTO_OBJETIVO = dr.GetDecimal(2),
                                FECHA_CUMPLIMIENTO = dr.GetDateTime(3)
                            };
                            metas.Add(m);
                        }
                    }
                }
            }
            return metas;
        }

        // Obtener presupuesto mensual global (PRESUPUESTO_MENSUAL)
        private decimal ObtenerPresupuestoMensual(int usuarioId, DateTime fecha)
        {
            decimal monto = 0;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT TOP 1 PRESUPUESTO_MENSUAL
                    FROM PROY_PRESUPUESTOS
                    WHERE USUARIO_ID = @UsuarioId
                      AND MES = @Mes
                      AND [AÑO] = @Anio";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@Mes", fecha.Month);
                    cmd.Parameters.AddWithValue("@Anio", fecha.Year);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                        monto = Convert.ToDecimal(result);
                }
            }
            return monto;
        }

        // Obtener presupuesto establecido para ingresos
        private decimal ObtenerPresupuestoIngresos(int usuarioId, DateTime fecha)
        {
            decimal monto = 0;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT TOP 1 MONTO_INGRESOS
                    FROM PROY_PRESUPUESTOS
                    WHERE USUARIO_ID = @UsuarioId
                      AND MES = @Mes
                      AND [AÑO] = @Anio";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@Mes", fecha.Month);
                    cmd.Parameters.AddWithValue("@Anio", fecha.Year);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                        monto = Convert.ToDecimal(result);
                }
            }
            return monto;
        }

        // Obtener presupuesto establecido para gastos
        private decimal ObtenerPresupuestoGastos(int usuarioId, DateTime fecha)
        {
            decimal monto = 0;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT TOP 1 MONTO_GASTOS
                    FROM PROY_PRESUPUESTOS
                    WHERE USUARIO_ID = @UsuarioId
                      AND MES = @Mes
                      AND [AÑO] = @Anio";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@Mes", fecha.Month);
                    cmd.Parameters.AddWithValue("@Anio", fecha.Year);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                        monto = Convert.ToDecimal(result);
                }
            }
            return monto;
        }

        // Agrupar gastos por categoría del mes (solo de transacciones tipo 'Gasto')
        private Dictionary<string, decimal> ObtenerGastosPorCategoria(int usuarioId, DateTime fecha)
        {
            Dictionary<string, decimal> gastosPorCategoria = new Dictionary<string, decimal>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT C.NOMBRE, SUM(T.MONTO)
                    FROM PROY_TRANSACCIONES T
                    INNER JOIN PROY_CATEGORIAS C ON T.CATEGORIA_ID = C.ID
                    WHERE T.USUARIO_ID = @UsuarioId 
                      AND T.TIPO = 'Gasto'
                      AND MONTH(T.FECHA) = @Mes 
                      AND YEAR(T.FECHA) = @Anio
                    GROUP BY C.NOMBRE";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    cmd.Parameters.AddWithValue("@Mes", fecha.Month);
                    cmd.Parameters.AddWithValue("@Anio", fecha.Year);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            string categoria = dr.IsDBNull(0) ? "Sin Categoría" : dr.GetString(0);
                            decimal total = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);
                            gastosPorCategoria[categoria] = total;
                        }
                    }
                }
            }
            return gastosPorCategoria;
        }
    }
}
