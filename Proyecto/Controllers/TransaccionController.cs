using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using Proyecto.Models;

namespace Proyecto.Controllers
{
    public class TransaccionController : Controller
    {
        // ------------- ACCIONES PÚBLICAS -------------

        // Acción que muestra la vista para crear o filtrar transacciones.
        // Se limpia el ModelState y ViewData, se carga un ViewModel con una nueva transacción y
        // se asignan las categorías según el tipo recibido (si no se recibe, se cargan todas).
        public ActionResult Transacciones(string tipo)
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Usuario");
            }
            ModelState.Clear();
            ViewData.Clear();

            var viewModel = new TransaccionViewModel
            {
                NuevaTransaccion = new Transaccion(),
                ListaCategorias = string.IsNullOrEmpty(tipo)
                    ? ObtenerCategorias()
                    : ObtenerCategoriasPorTipo(tipo)
            };

            // Se asigna el tipo recibido a la nueva transacción
            viewModel.NuevaTransaccion.TIPO = tipo;

            return View(viewModel);
        }

        // Acción que muestra la vista con el listado de transacciones registradas.
        // Se limpia ModelState y ViewData y se arma el ViewModel con la lista de transacciones y 
        // un objeto para la creación de una nueva transacción.
        //public ActionResult VerTransacciones()
        //{
        //    if (Session["UsuarioId"] == null)
        //    {
        //        return RedirectToAction("Login", "Usuario");
        //    }
        //    ModelState.Clear();
        //    ViewData.Clear();

        //    var viewModel = new TransaccionViewModel
        //    {
        //        Transacciones = ObtenerTransacciones(),
        //        NuevaTransaccion = new Transaccion()
        //    };

        //    return View(viewModel);
        //}

        public ActionResult VerTransacciones()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login", "Usuario");
            }
            ModelState.Clear();
            ViewData.Clear();

            int usuarioId = Convert.ToInt32(Session["UsuarioId"]);
            var viewModel = new TransaccionViewModel
            {
                Transacciones = ObtenerTransacciones(),
                NuevaTransaccion = new Transaccion()
            };

            // Asigna al ViewBag la lista de abonos asociados a las metas del usuario.
            ViewBag.Abonos = ObtenerAbonos(usuarioId);

            return View(viewModel);
        }

        // Acción POST que inserta una nueva transacción en la base de datos.
        // Si el modelo no es válido, se recarga la vista con las categorías actualizadas.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult InsertarTransaccion(TransaccionViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                ModelState.Clear();
                ViewData.Clear();
                viewModel.ListaCategorias = ObtenerCategorias();
                return View("Transacciones", viewModel);
            }

            int usuarioId = Convert.ToInt32(Session["UsuarioId"]);
            // Se inserta la nueva transacción en la tabla de la base de datos.
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    INSERT INTO PROY_TRANSACCIONES (TIPO, CATEGORIA_ID, FECHA, NUMERO_FACTURA, COMENTARIO, MONTO, USUARIO_ID)
                    VALUES (@TIPO, @CATEGORIA_ID, @FECHA, @NUMERO_FACTURA, @COMENTARIO, @MONTO, @USUARIO_ID)";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    var model = viewModel.NuevaTransaccion;
                    cmd.Parameters.AddWithValue("@TIPO", model.TIPO);
                    cmd.Parameters.AddWithValue("@CATEGORIA_ID", model.CATEGORIAID);
                    cmd.Parameters.AddWithValue("@FECHA", model.FECHA);
                    cmd.Parameters.AddWithValue("@NUMERO_FACTURA",
                        string.IsNullOrEmpty(model.NUMEROFACTURA) ? (object)DBNull.Value : model.NUMEROFACTURA);
                    cmd.Parameters.AddWithValue("@COMENTARIO",
                        string.IsNullOrEmpty(model.COMENTARIO) ? (object)DBNull.Value : model.COMENTARIO);
                    cmd.Parameters.AddWithValue("@MONTO", model.MONTO);
                    cmd.Parameters.AddWithValue("@USUARIO_ID", usuarioId);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            // Se redirige a la acción que muestra el listado de transacciones actualizado.
            return RedirectToAction("VerTransacciones");
        }

        // Acción que elimina una transacción según su ID.
        public ActionResult Delete(int id)
        {
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = "DELETE FROM PROY_TRANSACCIONES WHERE ID=@ID";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("VerTransacciones");
        }

        // ------------- MÉTODOS PRIVADOS (HELPER) -------------

        // Método que obtiene las categorías filtradas por el tipo especificado.
        // Retorna un SelectList con el ID y NOMBRE de las categorías.
        private SelectList ObtenerCategoriasPorTipo(string tipo)
        {
            List<Categoria> categorias = new List<Categoria>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = "SELECT ID, NOMBRE FROM PROY_CATEGORIAS WHERE TIPO = @TIPO";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@TIPO", tipo);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            categorias.Add(new Categoria
                            {
                                ID = dr.GetInt32(0),
                                NOMBRE = dr.GetString(1)
                            });
                        }
                    }
                }
            }
            return new SelectList(categorias, "ID", "NOMBRE");
        }

        // Método que obtiene todas las categorías disponibles.
        // Retorna un SelectList con el ID y NOMBRE de cada categoría.
        private SelectList ObtenerCategorias()
        {
            List<Categoria> categorias = new List<Categoria>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = "SELECT ID, NOMBRE FROM PROY_CATEGORIAS";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            categorias.Add(new Categoria
                            {
                                ID = dr.GetInt32(0),
                                NOMBRE = dr.GetString(1)
                            });
                        }
                    }
                }
            }
            return new SelectList(categorias, "ID", "NOMBRE");
        }

        // Método que obtiene la lista de transacciones, realizando un JOIN con la tabla de categorías
        // para incluir el nombre de la categoría. Retorna una lista de objetos Transaccion.
        private List<Transaccion> ObtenerTransacciones()
        {
            int usuarioId = Convert.ToInt32(Session["UsuarioId"]);
            List<Transaccion> transacciones = new List<Transaccion>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT T.ID, T.TIPO, T.CATEGORIA_ID, T.FECHA, T.NUMERO_FACTURA,
                           T.COMENTARIO, T.MONTO, C.NOMBRE AS CategoriaNombre
                    FROM PROY_TRANSACCIONES T
                    LEFT JOIN PROY_CATEGORIAS C ON T.CATEGORIA_ID = C.ID AND T.USUARIO_ID = "+usuarioId+"";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            transacciones.Add(new Transaccion
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
                            });
                        }
                    }
                }
            }
            return transacciones;
        }


        public ActionResult VerTransaccionesAbonos()
        {
            int usuarioId = Convert.ToInt32(Session["UsuarioId"]);

            // Cargar transacciones (suponiendo que ya tienes este código)
            List<Transaccion> transacciones = new List<Transaccion>();
            // ... Código para obtener las transacciones del usuario ...

            // Cargar abonos asociados a las metas del usuario
            List<Proyecto.Models.AbonosMeta> abonos = new List<Proyecto.Models.AbonosMeta>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"SELECT A.ID, A.META_ID, A.MONTO, A.FECHA, M.TIPO_META 
                         FROM PROY_ABONOS A
                         INNER JOIN PROY_METAS M ON A.META_ID = M.ID
                         WHERE M.USUARIO_ID = @USUARIOId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@USUARIOId", usuarioId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            abonos.Add(new Proyecto.Models.AbonosMeta
                            {
                                ID = Convert.ToInt32(dr["ID"]),
                                META_ID = Convert.ToInt32(dr["META_ID"]),
                                MONTO = Convert.ToDecimal(dr["MONTO"]),
                                FECHA = Convert.ToDateTime(dr["FECHA"]),
                                // Propiedad extra para mostrar el nombre de la meta
                                MetaNombre = dr["TIPO_META"].ToString()
                            });
                        }
                    }
                }
            }

            // Asigna los abonos al ViewBag
            ViewBag.Abonos = abonos;

            // Si tu modelo actual es TransaccionViewModel y lo usas para transacciones, lo mantienes igual:
            var viewModel = new Proyecto.Models.TransaccionViewModel
            {
                Transacciones = transacciones
            };

            return View(viewModel);
        }

        private List<AbonosMeta> ObtenerAbonos(int usuarioId)
        {
            List<AbonosMeta> abonos = new List<AbonosMeta>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
            SELECT A.ID, A.META_ID, A.MONTO, A.FECHA, M.TIPO_META, M.MONTO_OBJETIVO
            FROM PROY_ABONOS A
            INNER JOIN PROY_METAS M ON A.META_ID = M.ID
            WHERE M.USUARIO_ID = @UsuarioId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            abonos.Add(new AbonosMeta
                            {
                                ID = dr.GetInt32(0),
                                META_ID = dr.GetInt32(1),
                                MONTO = dr.GetDecimal(2),
                                FECHA = dr.GetDateTime(3),
                                MetaNombre = dr.IsDBNull(4) ? "" : dr.GetString(4),
                                OBJETIVO = dr.GetDecimal(5)
                            });
                        }
                    }
                }
            }
            return abonos;
        }


    }
}

