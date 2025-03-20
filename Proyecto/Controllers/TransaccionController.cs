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

        public ActionResult Transacciones(string tipo)
        {
            // Limpia ModelState y ViewData (si lo necesitas)
            ModelState.Clear();
            ViewData.Clear();

            var viewModel = new TransaccionViewModel
            {
                NuevaTransaccion = new Transaccion(),
                // Si se pasa un tipo, se filtran las categorías; de lo contrario se cargan todas
                ListaCategorias = string.IsNullOrEmpty(tipo)
                    ? ObtenerCategorias()
                    : ObtenerCategoriasPorTipo(tipo)
            };

            // Si se envió el tipo, asignarlo en el modelo para que se muestre la selección
            viewModel.NuevaTransaccion.TIPO = tipo;

            return View(viewModel);
        }



        public ActionResult VerTransacciones()
        {
            // 1. Limpiar por completo el ModelState y el ViewData para evitar la excepción
            ModelState.Clear();
            ViewData.Clear();

            // 2. Construir el ViewModel
            var viewModel = new TransaccionViewModel
            {
                Transacciones = ObtenerTransacciones(),   // Listado de transacciones
                NuevaTransaccion = new Transaccion()      // Objeto para el formulario
            };

            return View(viewModel); // Retorna la vista con el ViewModel
        }

        // GET: /Transaccion/Transacciones
        //public ActionResult Transacciones()
        //{
        //    // 1. Limpiar por completo el ModelState y el ViewData para evitar la excepción
        //    ModelState.Clear();
        //    ViewData.Clear();

        //    // 2. Construir el ViewModel
        //    var viewModel = new TransaccionViewModel
        //    {
        //        ListaCategorias = ObtenerCategorias(),    // Asignamos el SelectList
        //        //Transacciones = ObtenerTransacciones(),   // Listado de transacciones
        //        NuevaTransaccion = new Transaccion()      // Objeto para el formulario
        //    };

        //    return View(viewModel); // Retorna la vista con el ViewModel
        //}

        // POST: /Transaccion/InsertarTransaccion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult InsertarTransaccion(TransaccionViewModel viewModel)
        {
            // Validar el modelo
            if (!ModelState.IsValid)
            {
                // Limpiar por completo el ModelState y el ViewData
                ModelState.Clear();
                ViewData.Clear();

                // Volver a cargar el ViewModel (categorías y transacciones)
                viewModel.ListaCategorias = ObtenerCategorias();
                //viewModel.Transacciones = ObtenerTransacciones();

                // Retornamos la misma vista
                return View("Transacciones", viewModel);
            }

            // Insertar la transacción en la tabla (ID es identity)
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    INSERT INTO PROY_TRANSACCIONES (TIPO, CATEGORIA_ID, FECHA, NUMERO_FACTURA, COMENTARIO, MONTO)
                    VALUES (@TIPO, @CATEGORIA_ID, @FECHA, @NUMERO_FACTURA, @COMENTARIO, @MONTO)";
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

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            // Redirige a la acción GET para ver la tabla actualizada
            return RedirectToAction("VerTransacciones");
        }

        // GET: /Transaccion/Delete/5
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

        // Obtiene las categorías y crea un SelectList
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

        //Obtiene la lista de transacciones(JOIN con categoría)
        private List<Transaccion> ObtenerTransacciones()
        {
            List<Transaccion> transacciones = new List<Transaccion>();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                string query = @"
                    SELECT T.ID, T.TIPO, T.CATEGORIA_ID, T.FECHA, T.NUMERO_FACTURA,
                           T.COMENTARIO, T.MONTO, C.NOMBRE AS CategoriaNombre
                    FROM PROY_TRANSACCIONES T
                    LEFT JOIN PROY_CATEGORIAS C ON T.CATEGORIA_ID = C.ID";
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
    }
}
