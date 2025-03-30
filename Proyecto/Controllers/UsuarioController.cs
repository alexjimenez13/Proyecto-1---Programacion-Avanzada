using Proyecto.Models;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;

namespace Proyecto.Controllers
{
    public class UsuarioController : Controller
    {
        // -------------------- ACCIONES DE REGISTRO --------------------

        // GET: Muestra la vista de registro de usuario.
        [HttpGet]
        public ActionResult Registro()
        {
            return View();
        }

        // POST: Procesa el registro de un nuevo usuario.
        // Valida el modelo, revisa si el correo ya existe, encripta la contraseña e inserta el usuario en la BD.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registro(Usuario model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verificar si el correo ya está registrado.
            bool existe = false;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                con.Open();
                string queryExiste = "SELECT COUNT(*) FROM PROY_USUARIOS WHERE CORREO = @CORREO";
                using (SqlCommand cmd = new SqlCommand(queryExiste, con))
                {
                    cmd.Parameters.AddWithValue("@CORREO", model.CORREO);
                    int count = (int)cmd.ExecuteScalar();
                    existe = (count > 0);
                }
            }

            if (existe)
            {
                ModelState.AddModelError("", "El usuario o el correo ya existen.");
                return View(model);
            }

            // Encripta la contraseña usando SHA256.
            string contraseñaHasheada = ConvertirSha256(model.CONTRASENA);

            // Inserta el nuevo usuario en la base de datos.
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                con.Open();
                string queryInsert = "INSERT INTO PROY_USUARIOS (NOMBRE, CORREO, NOMBRE_USUARIO, PRIMER_APELLIDO, SEGUNDO_APELLIDO, CONTRASENA) " +
                                     "VALUES (@NOMBRE, @CORREO, @NOMBRE_USUARIO, @PRIMER_APELLIDO, @SEGUNDO_APELLIDO, @CONTRASENA)";
                using (SqlCommand cmd = new SqlCommand(queryInsert, con))
                {
                    cmd.Parameters.AddWithValue("@NOMBRE", model.NOMBRE);
                    cmd.Parameters.AddWithValue("@CORREO", model.CORREO);
                    cmd.Parameters.AddWithValue("@NOMBRE_USUARIO", model.NOMBRE_USUARIO);
                    cmd.Parameters.AddWithValue("@PRIMER_APELLIDO", model.PRIMER_APELLIDO);
                    cmd.Parameters.AddWithValue("@SEGUNDO_APELLIDO", model.SEGUNDO_APELLIDO);
                    cmd.Parameters.AddWithValue("@CONTRASENA", contraseñaHasheada);

                    cmd.ExecuteNonQuery();
                }
            }

            // Tras un registro exitoso, redirige a la vista de Login.
            return RedirectToAction("Login");
        }

        // -------------------- ACCIONES DE LOGIN --------------------

        // GET: Muestra la vista de login.
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: Procesa el ingreso de un usuario.
        // Valida que se ingresen los datos, encripta la contraseña y verifica las credenciales.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string NOMBRE_USUARIO, string CONTRASENA)
        {
            if (string.IsNullOrEmpty(NOMBRE_USUARIO) || string.IsNullOrEmpty(CONTRASENA))
            {
                ModelState.AddModelError("", "Debes ingresar usuario y contraseña.");
                return View();
            }

            // Encripta la contraseña ingresada para compararla con la guardada.
            string contraseñaHasheada = ConvertirSha256(CONTRASENA);

            Usuario usuario = null;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                con.Open();
                string queryLogin = "SELECT TOP 1 Id, NOMBRE, CORREO, NOMBRE_USUARIO, CONTRASENA, TOKEN_RECUPERACION " +
                                    "FROM PROY_USUARIOS " +
                                    "WHERE NOMBRE_USUARIO = @NOMBRE_USUARIO AND CONTRASENA = @CONTRASENA";
                using (SqlCommand cmd = new SqlCommand(queryLogin, con))
                {
                    cmd.Parameters.AddWithValue("@NOMBRE_USUARIO", NOMBRE_USUARIO);
                    cmd.Parameters.AddWithValue("@CONTRASENA", contraseñaHasheada);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            usuario = new Usuario
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                NOMBRE = reader["NOMBRE"].ToString(),
                                CORREO = reader["CORREO"].ToString(),
                                NOMBRE_USUARIO = reader["NOMBRE_USUARIO"].ToString(),
                                CONTRASENA = reader["CONTRASENA"].ToString(),
                                TOKEN_RECUPERACION = reader["TOKEN_RECUPERACION"]?.ToString()
                            };
                        }
                    }
                }
            }

            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View();
            }

            // Crea la sesión del usuario y redirige a la página principal.
            Session["UsuarioId"] = usuario.Id;
            Session["NOMBRE_USUARIO"] = usuario.NOMBRE_USUARIO;
            return RedirectToAction("Index", "Home");
        }

        // -------------------- ACCIONES DE RECUPERACIÓN DE CONTRASEÑA --------------------

        // GET: Muestra la vista para solicitar recuperación de contraseña.
        [HttpGet]
        public ActionResult OlvidoContraseña()
        {
            return View();
        }

        // POST: Procesa la solicitud de recuperación de contraseña.
        // Valida el correo, verifica si existe el usuario y genera un token de recuperación.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OlvidoContraseña(string CORREO)
        {
            if (string.IsNullOrEmpty(CORREO))
            {
                ModelState.AddModelError("", "Ingresa tu correo electrónico.");
                return View();
            }

            int userId = 0;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                con.Open();
                string queryUser = "SELECT Id FROM PROY_USUARIOS WHERE CORREO = @CORREO";
                using (SqlCommand cmd = new SqlCommand(queryUser, con))
                {
                    cmd.Parameters.AddWithValue("@CORREO", CORREO);
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        userId = Convert.ToInt32(result);
                    }
                }
            }

            if (userId == 0)
            {
                ModelState.AddModelError("", "No existe un usuario con ese correo.");
                return View();
            }

            // Genera un token único para recuperación.
            string token = Guid.NewGuid().ToString();
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                con.Open();
                string queryToken = "UPDATE PROY_USUARIOS SET TOKEN_RECUPERACION = @TOKEN WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(queryToken, con))
                {
                    cmd.Parameters.AddWithValue("@TOKEN", token);
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            // En un escenario real se enviaría el token por correo.
            ViewBag.Mensaje = "Se ha generado un token de recuperación. (Simulación)";
            return View();
        }

        // GET: Muestra la vista para cambiar la contraseña (usuario logueado).
        [HttpGet]
        public ActionResult CambioContraseña()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // POST: Procesa el cambio de contraseña del usuario logueado.
        // Valida la contraseña actual y, si es correcta, actualiza la contraseña en la BD.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambioContraseña(string contraseñaActual, string nuevaContraseña)
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login");
            }

            int usuarioId = (int)Session["UsuarioId"];
            bool credencialesCorrectas = false;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                con.Open();
                string queryCheck = "SELECT COUNT(*) FROM PROY_USUARIOS WHERE Id = @Id AND CONTRASENA = @CONTRASENA";
                using (SqlCommand cmd = new SqlCommand(queryCheck, con))
                {
                    cmd.Parameters.AddWithValue("@Id", usuarioId);
                    cmd.Parameters.AddWithValue("@CONTRASENA", ConvertirSha256(contraseñaActual));
                    int count = (int)cmd.ExecuteScalar();
                    credencialesCorrectas = (count > 0);
                }
            }

            if (!credencialesCorrectas)
            {
                ModelState.AddModelError("", "La contraseña actual es incorrecta.");
                return View();
            }

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                con.Open();
                string queryUpdate = "UPDATE PROY_USUARIOS SET CONTRASENA = @NUEVA_CONTRASENA WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(queryUpdate, con))
                {
                    cmd.Parameters.AddWithValue("@NUEVA_CONTRASENA", ConvertirSha256(nuevaContraseña));
                    cmd.Parameters.AddWithValue("@Id", usuarioId);
                    cmd.ExecuteNonQuery();
                }
            }

            ViewBag.Mensaje = "La contraseña se cambió exitosamente.";
            return View();
        }

        // GET: Muestra la vista para restablecer la contraseña usando un token.
        [HttpGet]
        public ActionResult Restablecer(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }
            ViewBag.Token = token;
            return View();
        }

        // POST: Procesa el restablecimiento de contraseña a partir de un token.
        // Valida el token y, de ser válido, actualiza la contraseña y elimina el token.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restablecer(string token, string nuevaContraseña)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(nuevaContraseña))
            {
                ModelState.AddModelError("", "Datos inválidos.");
                return View();
            }

            int userId = 0;
            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                con.Open();
                string queryCheck = "SELECT Id FROM PROY_USUARIOS WHERE TOKEN_RECUPERACION = @TOKEN";
                using (SqlCommand cmd = new SqlCommand(queryCheck, con))
                {
                    cmd.Parameters.AddWithValue("@TOKEN", token);
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        userId = Convert.ToInt32(result);
                    }
                }
            }

            if (userId == 0)
            {
                ModelState.AddModelError("", "Token inválido.");
                return View();
            }

            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
            {
                con.Open();
                string queryUpdate = "UPDATE PROY_USUARIOS SET CONTRASENA = @CONTRASENA, TOKEN_RECUPERACION = NULL WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(queryUpdate, con))
                {
                    cmd.Parameters.AddWithValue("@CONTRASENA", ConvertirSha256(nuevaContraseña));
                    cmd.Parameters.AddWithValue("@Id", userId);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Login");
        }

        // -------------------- MÉTODOS AUXILIARES --------------------

        // Método privado que encripta un texto usando el algoritmo SHA256.
        private string ConvertirSha256(string texto)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(texto));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // GET: Cierra la sesión del usuario y redirige a la vista de Login.
        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}


