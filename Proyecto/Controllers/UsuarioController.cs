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
        // Registro de usuarios
        [HttpGet]
        public ActionResult Registro()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registro(Usuario model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Revisa si el correo ya está registrado
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

            // Encripta la contraseña
            string contraseñaHasheada = ConvertirSha256(model.CONTRASENA);

            // Inserta el nuevo usuario (se insertan los campos necesarios)
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

            // Redirige a la página de login tras un registro exitoso
            return RedirectToAction("Login");
        }

        // Ingreso
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string NOMBRE_USUARIO, string CONTRASENA)
        {
            if (string.IsNullOrEmpty(NOMBRE_USUARIO) || string.IsNullOrEmpty(CONTRASENA))
            {
                ModelState.AddModelError("", "Debes ingresar usuario y contraseña.");
                return View();
            }

            // Encripta la contraseña ingresada para compararla
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

            // Crea la sesión y redirige a la página principal
            Session["UsuarioId"] = usuario.Id;
            Session["NOMBRE_USUARIO"] = usuario.NOMBRE_USUARIO;
            return RedirectToAction("Index", "Home");
        }

        // Contraseña olvidada
        [HttpGet]
        public ActionResult OlvidoContraseña()
        {
            return View();
        }

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
            ViewBag.Mensaje = "Se ha generado un token de recuperación. (Simulación)";
            return View();
        }

        // Cambio de contraseña
        [HttpGet]
        public ActionResult CambioContraseña()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

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

        // Restablecer la contraseña por token
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

        // Método para encriptar contraseñas
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

        // Método para cerrar sesión
        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}








//using Proyecto.Models;
//using System;
//using System.Security.Cryptography;
//using System.Text;
//using System.Web.Mvc;
//using System.Configuration;
//using System.Data.SqlClient;

//namespace Proyecto.Controllers
//{
//    public class UsuarioController : Controller
//    {
//        //Registro de usuarios
//        [HttpGet]
//        public ActionResult Registro()
//        {
//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public ActionResult Registro(Usuario model)
//        {
//            if (!ModelState.IsValid)
//            {
//                return View(model);
//            }

//            //Revisa si el correo ya esta registrado
//            bool existe = false;
//            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
//            {
//                con.Open();
//                string queryExiste = "SELECT COUNT(*) FROM PROY_USUARIOS WHERE CORREO = @CORREO";
//                using (SqlCommand cmd = new SqlCommand(queryExiste, con))
//                {
//                    cmd.Parameters.AddWithValue("@CORREO", model.CorreoElectronico);

//                    int count = (int)cmd.ExecuteScalar();
//                    existe = (count > 0);
//                }
//            }

//            //Si existe envia mensaje informandolo
//            if (existe)
//            {
//                ModelState.AddModelError("", "El usuario o el correo ya existen.");
//                return View(model);
//            }

//            //Si no lo inserta pero primero encripta la contraseña
//            string contraseñaHasheada = ConvertirSha256(model.Contraseña);

//            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
//            {
//                con.Open();
//                string queryInsert = "INSERT INTO PROY_USUARIOS (NOMBRE,CORREO,CONTRASENA) VALUES (@NOMBRE,@CORREO,@CONTRASENA)";
//                using (SqlCommand cmd = new SqlCommand(queryInsert, con))
//                {
//                    cmd.Parameters.AddWithValue("@NOMBRE", model.NombreUsuario);
//                    cmd.Parameters.AddWithValue("@CORREO", model.CorreoElectronico);
//                    cmd.Parameters.AddWithValue("@CONTRASENA", contraseñaHasheada);

//                    cmd.ExecuteNonQuery();
//                }
//            }

//            // Si se registro bien lo envia a la pagina para que se loguee por primera vez
//            return RedirectToAction("Login");
//        }

//        //Ingreso
//        [HttpGet]
//        public ActionResult Login()
//        {
//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public ActionResult Login(string nombreUsuario, string contraseña)
//        {
//            //Valida que se haya escrito el usuario y la contrasena
//            if (string.IsNullOrEmpty(nombreUsuario) || string.IsNullOrEmpty(contraseña))
//            {
//                ModelState.AddModelError("", "Debes ingresar usuario y contraseña.");
//                return View();
//            }

//            //encripta la contraseña digitada para compararla con la guardada
//            string contraseñaHasheada = ConvertirSha256(contraseña);

//            //Validamos datos
//            Usuario usuario = null;
//            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
//            {
//                con.Open();
//                string queryLogin = "SELECT TOP 1 Id,NOMBRE,CORREO,CONTRASENA,TOKEN_RECUPERACION FROM PROY_USUARIOS WHERE NombreUsuario = @NombreUsuario AND Contraseña = @Contraseña";
//                using (SqlCommand cmd = new SqlCommand(queryLogin, con))
//                {
//                    cmd.Parameters.AddWithValue("@NombreUsuario", nombreUsuario);
//                    cmd.Parameters.AddWithValue("@Contraseña", contraseñaHasheada);

//                    using (SqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        if (reader.Read())
//                        {
//                            usuario = new Usuario
//                            {
//                                Id = Convert.ToInt32(reader["Id"]),
//                                NombreUsuario = reader["NombreUsuario"].ToString(),
//                                CorreoElectronico = reader["CorreoElectronico"].ToString(),
//                                Contraseña = reader["Contraseña"].ToString(),
//                                TokenRecuperacion = reader["TokenRecuperacion"]?.ToString()
//                            };
//                        }
//                    }
//                }
//            }

//            //Si los datos son incorrectos devuelve mensaje
//            if (usuario == null)
//            {
//                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
//                return View();
//            }

//            //Genera la sesion y entramos a la pagina principal
//            Session["UsuarioId"] = usuario.Id;
//            Session["NombreUsuario"] = usuario.NombreUsuario;
//            return RedirectToAction("Index", "Home");
//        }

//        //Contraseña olvidada
//        [HttpGet]
//        public ActionResult OlvidoContraseña()
//        {
//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public ActionResult OlvidoContraseña(string correoElectronico)
//        {
//            //Valida que se digite el correo
//            if (string.IsNullOrEmpty(correoElectronico))
//            {
//                ModelState.AddModelError("", "Ingresa tu correo electrónico.");
//                return View();
//            }

//            // Verificar si existe un usuario con ese correo
//            int userId = 0;
//            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
//            {
//                con.Open();
//                string queryUser = "SELECT Id FROM Usuarios WHERE CorreoElectronico = @CorreoElectronico";
//                using (SqlCommand cmd = new SqlCommand(queryUser, con))
//                {
//                    cmd.Parameters.AddWithValue("@CorreoElectronico", correoElectronico);
//                    object result = cmd.ExecuteScalar();
//                    if (result != null)
//                    {
//                        userId = Convert.ToInt32(result);
//                    }
//                }
//            }

//            //Si el correo no esta registrado envia un mensaje indicandolo
//            if (userId == 0)
//            {
//                ModelState.AddModelError("", "No existe un usuario con ese correo.");
//                return View();
//            }

//            // Si el correo si esta registrado entonces genera un token
//            string token = Guid.NewGuid().ToString();

//            // Guardar el token en la base de datos
//            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
//            {
//                con.Open();
//                string queryToken = "UPDATE Usuarios SET TokenRecuperacion = @Token WHERE Id = @Id";
//                using (SqlCommand cmd = new SqlCommand(queryToken, con))
//                {
//                    cmd.Parameters.AddWithValue("@Token", token);
//                    cmd.Parameters.AddWithValue("@Id", userId);
//                    cmd.ExecuteNonQuery();
//                }
//            }
//            //Envia el token para que el usuario ingrese
//            // Aquí podrías enviar un correo real con un link, p.ej.:
//            // https://tusitio.com/Usuario/CambioContraseña?token=EL_TOKEN_GENERADO
//            // Por simplicidad, solo mostramos un mensaje
//            ViewBag.Mensaje = "Se ha generado un token de recuperación. (Simulación)";
//            return View();
//        }

//        //Cambio de contrasena
//        [HttpGet]
//        public ActionResult CambioContraseña()
//        {
//            if (Session["UsuarioId"] == null)
//            {
//                return RedirectToAction("Login");
//            }
//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public ActionResult CambioContraseña(string contraseñaActual, string nuevaContraseña)
//        {
//            if (Session["UsuarioId"] == null)
//            {
//                return RedirectToAction("Login");
//            }

//            int usuarioId = (int)Session["UsuarioId"];
//            bool credencialesCorrectas = false;
//            //Valida la contraseña actual
//            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
//            {
//                con.Open();
//                string queryCheck = "SELECT COUNT(*) FROM Usuarios WHERE Id = @Id AND Contraseña = @Contraseña";
//                using (SqlCommand cmd = new SqlCommand(queryCheck, con))
//                {
//                    cmd.Parameters.AddWithValue("@Id", usuarioId);
//                    cmd.Parameters.AddWithValue("@Contraseña", ConvertirSha256(contraseñaActual));

//                    int count = (int)cmd.ExecuteScalar();
//                    credencialesCorrectas = (count > 0);
//                }
//            }

//            //Si la contraseña actual es incorrecta entonces envia un mensaje
//            if (!credencialesCorrectas)
//            {
//                ModelState.AddModelError("", "La contraseña actual es incorrecta.");
//                return View();
//            }

//            //Si la contraseña actual es correcta entonces actualiza con la nueva
//            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
//            {
//                con.Open();
//                string queryUpdate = "UPDATE Usuarios SET Contraseña = @NuevaContraseña WHERE Id = @Id";
//                using (SqlCommand cmd = new SqlCommand(queryUpdate, con))
//                {
//                    cmd.Parameters.AddWithValue("@NuevaContraseña", ConvertirSha256(nuevaContraseña));
//                    cmd.Parameters.AddWithValue("@Id", usuarioId);
//                    cmd.ExecuteNonQuery();
//                }
//            }

//            ViewBag.Mensaje = "La contraseña se cambió exitosamente.";
//            return View();
//        }

//        //Resstablecer la contraseña por token
//        [HttpGet]
//        public ActionResult Restablecer(string token)
//        {
//            //Validamos que el token se digitara
//            if (string.IsNullOrEmpty(token))
//            {
//                return RedirectToAction("Login");
//            }

//            ViewBag.Token = token;
//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public ActionResult Restablecer(string token, string nuevaContraseña)
//        {
//            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(nuevaContraseña))
//            {
//                ModelState.AddModelError("", "Datos inválidos.");
//                return View();
//            }

//            // Verificar si el token existe en la base de datos
//            int userId = 0;
//            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
//            {
//                con.Open();
//                string queryCheck = "SELECT Id FROM Usuarios WHERE TokenRecuperacion = @Token";
//                using (SqlCommand cmd = new SqlCommand(queryCheck, con))
//                {
//                    cmd.Parameters.AddWithValue("@Token", token);
//                    object result = cmd.ExecuteScalar();
//                    if (result != null)
//                    {
//                        userId = Convert.ToInt32(result);
//                    }
//                }
//            }

//            //Si no existe el sistema lo indica
//            if (userId == 0)
//            {
//                ModelState.AddModelError("", "Token inválido.");
//                return View();
//            }

//            //Si el token es correcto entonces actualiza con la contraseña nueva y nos envia a loguearnos de nuevo
//            using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["CONEXION_DB"].ConnectionString))
//            {
//                con.Open();
//                string queryUpdate = "UPDATE Usuarios SET Contraseña = @Contraseña, TokenRecuperacion = NULL WHERE Id = @Id";
//                using (SqlCommand cmd = new SqlCommand(queryUpdate, con))
//                {
//                    cmd.Parameters.AddWithValue("@Contraseña", ConvertirSha256(nuevaContraseña));
//                    cmd.Parameters.AddWithValue("@Id", userId);
//                    cmd.ExecuteNonQuery();
//                }
//            }

//            return RedirectToAction("Login");
//        }

//        //Metodo para encriptar contraseñas
//        private string ConvertirSha256(string texto)
//        {
//            using (SHA256 sha256Hash = SHA256.Create())
//            {
//                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(texto));
//                StringBuilder builder = new StringBuilder();
//                for (int i = 0; i < bytes.Length; i++)
//                {
//                    builder.Append(bytes[i].ToString("x2"));
//                }
//                return builder.ToString();
//            }
//        }

//        //Metodo para cerrar sesion
//        [HttpGet]
//        public ActionResult Logout()
//        {
//            Session.Clear();
//            return RedirectToAction("Login");
//        }

//    }
//}