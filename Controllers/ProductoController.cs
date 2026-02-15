using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Filters;
using Dapper;
using ZeltekWeb.Models;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class ProductoController : Controller
    {
        private readonly IConfiguration _config;

        public ProductoController(IConfiguration config)
        {
            _config = config;
        }

        // =========================
        // LISTADO
        // =========================
        public IActionResult Index()
        {
            var lista = new List<ProductoViewModel>();
            var cadena = _config.GetConnectionString("ZeltekDB");

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            var sql = @"
                SELECT 
                    p.ProductoID,
                    p.Nombre,
                    m.Nombre AS Marca,
                    c.Nombre AS Categoria,
                    p.EsUnico
                FROM Producto p
                INNER JOIN Marca m ON p.MarcaID = m.MarcaID
                LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                WHERE p.IsDeleted = 0
                ORDER BY p.Nombre";

            using SqlCommand cmd = new SqlCommand(sql, con);
            using var dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new ProductoViewModel
                {
                    ProductoID = dr.GetInt32(0),
                    Nombre = dr.GetString(1),
                    Marca = dr.GetString(2),
                    Categoria = dr.IsDBNull(3) ? "Sin categoría" : dr.GetString(3),
                    EsUnico = dr.GetBoolean(4)
                });
            }

            return View(lista);
        }

        // =========================
        // CREAR
        // =========================
        public IActionResult Create()
        {
            ViewBag.Marcas = ObtenerMarcas();
            ViewBag.Categorias = ObtenerCategorias();
            return View("CreateEdit", new ProductoCreateEditViewModel());
        }


        [HttpPost]
        public IActionResult Create(ProductoCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var camposConError = string.Join(", ", ModelState
                    .Where(x => x.Value?.Errors?.Count > 0)
                    .Select(x => string.IsNullOrEmpty(x.Key) ? "(general)" : x.Key));
                ModelState.AddModelError("", $"[Validación] Campos con error: {camposConError}. Revisa Nombre y Marca.");
                ViewBag.Marcas = ObtenerMarcas();
                ViewBag.Categorias = ObtenerCategorias();
                return View("CreateEdit", model);
            }

            try
            {
                var nombreTrim = model.Nombre?.Trim() ?? "";
                if (!string.IsNullOrEmpty(nombreTrim))
                {
                    var cadenaCheck = _config.GetConnectionString("ZeltekDB");
                    using (var conCheck = new SqlConnection(cadenaCheck))
                    {
                        conCheck.Open();
                        using var cmdCheck = new SqlCommand(
                            "SELECT 1 FROM Producto WHERE LOWER(RTRIM(LTRIM(Nombre))) = LOWER(@Nombre) AND IsDeleted = 0", conCheck);
                        cmdCheck.Parameters.AddWithValue("@Nombre", nombreTrim);
                        if (cmdCheck.ExecuteScalar() != null)
                        {
                            ModelState.AddModelError("Nombre", "Ya existe un producto activo con ese nombre. No se permiten duplicados.");
                            ViewBag.Marcas = ObtenerMarcas();
                            ViewBag.Categorias = ObtenerCategorias();
                            return View("CreateEdit", model);
                        }
                    }
                }

                var cadena = _config.GetConnectionString("ZeltekDB");
                using SqlConnection con = new SqlConnection(cadena);
                con.Open();

                var sql = @"
                    INSERT INTO Producto
                    (Nombre, Descripcion, MarcaID, Modelo, CategoriaID, EsUnico, CostoReferencia)
                    VALUES
                    (@Nombre, @Descripcion, @MarcaID, @Modelo, @CategoriaID, @EsUnico, @Costo)";

                using SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Nombre", nombreTrim);
                cmd.Parameters.AddWithValue("@Descripcion", (object?)model.Descripcion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MarcaID", model.MarcaID);
                cmd.Parameters.AddWithValue("@Modelo", (object?)model.Modelo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CategoriaID", (object?)model.CategoriaID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EsUnico", model.EsUnico);
                cmd.Parameters.AddWithValue("@Costo", (object?)model.CostoReferencia ?? DBNull.Value);

                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var mensajeCompleto = ex.ToString() + (ex.InnerException != null ? "\n--- Inner ---\n" + ex.InnerException.ToString() : "");
                ModelState.AddModelError("", "Error al guardar: " + (ex.InnerException?.Message ?? ex.Message));
                ModelState.AddModelError("", "[DEBUG] Excepción completa:\n" + mensajeCompleto);
                ViewBag.Marcas = ObtenerMarcas();
                ViewBag.Categorias = ObtenerCategorias();
                return View("CreateEdit", model);
            }
        }

        // =========================
        // EDITAR
        // =========================
        public IActionResult Edit(int id)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");
            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            var sql = @"
        SELECT ProductoID, Nombre, Descripcion, MarcaID, Modelo,
               CategoriaID, EsUnico, CostoReferencia
        FROM Producto
        WHERE ProductoID = @ID AND IsDeleted = 0";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@ID", id);

            using var dr = cmd.ExecuteReader();
            if (!dr.Read()) return NotFound();

            var model = new ProductoCreateEditViewModel
            {
                ProductoID = dr.GetInt32(0),
                Nombre = dr.GetString(1),
                Descripcion = dr.IsDBNull(2) ? null : dr.GetString(2),
                MarcaID = dr.GetInt32(3),
                Modelo = dr.IsDBNull(4) ? null : dr.GetString(4),
                CategoriaID = dr.IsDBNull(5) ? null : dr.GetInt32(5),
                EsUnico = dr.GetBoolean(6),
                CostoReferencia = dr.IsDBNull(7) ? null : dr.GetDecimal(7)
            };

            ViewBag.Marcas = ObtenerMarcas();
            ViewBag.Categorias = ObtenerCategorias();

            return View("CreateEdit", model);
        }

        [HttpPost]
        public IActionResult Edit(int id, ProductoCreateEditViewModel model)
        {
            if (model.ProductoID != id) return NotFound();
            if (!ModelState.IsValid)
            {
                var camposConError = string.Join(", ", ModelState
                    .Where(x => x.Value?.Errors?.Count > 0)
                    .Select(x => string.IsNullOrEmpty(x.Key) ? "(general)" : x.Key));
                ModelState.AddModelError("", $"[Validación] Campos con error: {camposConError}. Revisa Nombre y Marca.");
                ViewBag.Marcas = ObtenerMarcas();
                ViewBag.Categorias = ObtenerCategorias();
                return View("CreateEdit", model);
            }

            var nombreTrim = model.Nombre?.Trim() ?? "";
            if (!string.IsNullOrEmpty(nombreTrim))
            {
                var cadenaCheck = _config.GetConnectionString("ZeltekDB");
                using (var conCheck = new SqlConnection(cadenaCheck))
                {
                    conCheck.Open();
                    using var cmdCheck = new SqlCommand(
                        "SELECT 1 FROM Producto WHERE LOWER(RTRIM(LTRIM(Nombre))) = LOWER(@Nombre) AND ProductoID <> @ID AND IsDeleted = 0", conCheck);
                    cmdCheck.Parameters.AddWithValue("@Nombre", nombreTrim);
                    cmdCheck.Parameters.AddWithValue("@ID", id);
                    if (cmdCheck.ExecuteScalar() != null)
                    {
                        ModelState.AddModelError("Nombre", "Ya existe otro producto activo con ese nombre. No se permiten duplicados.");
                        ViewBag.Marcas = ObtenerMarcas();
                        ViewBag.Categorias = ObtenerCategorias();
                        return View("CreateEdit", model);
                    }
                }
            }

            var cadena = _config.GetConnectionString("ZeltekDB");
            try
            {
                using SqlConnection con = new SqlConnection(cadena);
                con.Open();

                var sql = @"
                    UPDATE Producto SET
                        Nombre = @Nombre,
                        Descripcion = @Descripcion,
                        MarcaID = @MarcaID,
                        Modelo = @Modelo,
                        CategoriaID = @CategoriaID,
                        EsUnico = @EsUnico,
                        CostoReferencia = @Costo
                    WHERE ProductoID = @ID AND IsDeleted = 0";

                using SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@ID", id);
                cmd.Parameters.AddWithValue("@Nombre", nombreTrim);
                cmd.Parameters.AddWithValue("@Descripcion", (object?)model.Descripcion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MarcaID", model.MarcaID);
                cmd.Parameters.AddWithValue("@Modelo", (object?)model.Modelo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CategoriaID", (object?)model.CategoriaID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@EsUnico", model.EsUnico);
                cmd.Parameters.AddWithValue("@Costo", (object?)model.CostoReferencia ?? DBNull.Value);

                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar: " + (ex.InnerException?.Message ?? ex.Message));
                ViewBag.Marcas = ObtenerMarcas();
                ViewBag.Categorias = ObtenerCategorias();
                return View("CreateEdit", model);
            }
        }

        // =========================
        // HELPERS
        // =========================
        private List<SelectListItem> ObtenerMarcas()
        {
            var lista = new List<SelectListItem>();
            var cadena = _config.GetConnectionString("ZeltekDB");

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            var sql = "SELECT MarcaID, Nombre FROM Marca ORDER BY Nombre";
            using SqlCommand cmd = new SqlCommand(sql, con);
            using var dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new SelectListItem
                {
                    Value = dr.GetInt32(0).ToString(),
                    Text = dr.GetString(1)
                });
            }

            return lista;
        }

        private List<SelectListItem> ObtenerCategorias()
        {
            var lista = new List<SelectListItem>();
            var cadena = _config.GetConnectionString("ZeltekDB");

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            var sql = "SELECT CategoriaID, Nombre FROM Categoria ORDER BY Nombre";
            using SqlCommand cmd = new SqlCommand(sql, con);
            using var dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                lista.Add(new SelectListItem
                {
                    Value = dr.GetInt32(0).ToString(),
                    Text = dr.GetString(1)
                });




            }



            return lista;
        }




        public IActionResult Delete(int id)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");
            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            var sql = "UPDATE Producto SET IsDeleted = 1 WHERE ProductoID = @ID";
            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@ID", id);

            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
        }

        // =========================
        // AJAX API
        // =========================

        [HttpGet]
        public IActionResult CheckNombre(string nombre, int? excludeId)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return Json(new { exists = false });

            using var con = new SqlConnection(_config.GetConnectionString("ZeltekDB"));
            var sql = @"
                SELECT ProductoID, IsDeleted 
                FROM Producto 
                WHERE Nombre = @Nombre 
                AND (@ExcludeID IS NULL OR ProductoID != @ExcludeID)";

            var result = con.QueryFirstOrDefault(sql, new { Nombre = nombre.Trim(), ExcludeID = excludeId });

            if (result != null)
            {
                return Json(new 
                { 
                    exists = true, 
                    isDeleted = (bool)result.IsDeleted, 
                    id = (int)result.ProductoID 
                });
            }

            return Json(new { exists = false });
        }

        [HttpPost]
        public IActionResult CrearMarcaRapida(string nombre)
        {
            try
            {
                var nombreTrim = nombre?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(nombreTrim)) return Json(new { success = false, message = "El nombre es requerido." });

                var cadena = _config.GetConnectionString("ZeltekDB");
                using (var con = new SqlConnection(cadena))
                {
                    con.Open();
                    using (var cmdCheck = new SqlCommand("SELECT 1 FROM Marca WHERE LOWER(RTRIM(LTRIM(Nombre))) = LOWER(@Nombre)", con))
                    {
                        cmdCheck.Parameters.AddWithValue("@Nombre", nombreTrim);
                        if (cmdCheck.ExecuteScalar() != null)
                            return Json(new { success = false, message = "⛔ Esta marca ya existe. No se permiten duplicados." });
                    }
                    using (var cmdInsert = new SqlCommand(
                        "INSERT INTO Marca (Nombre) VALUES (@Nombre); SELECT CAST(SCOPE_IDENTITY() AS INT);", con))
                    {
                        cmdInsert.Parameters.AddWithValue("@Nombre", nombreTrim);
                        var idRaw = cmdInsert.ExecuteScalar();
                        var id = idRaw != null && idRaw != DBNull.Value ? Convert.ToInt32(idRaw) : 0;
                        if (id <= 0) return Json(new { success = false, message = "No se pudo obtener el ID de la marca." });
                        return Json(new { success = true, id, nombre = nombreTrim });
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Error al guardar la marca: " + msg });
            }
        }

        [HttpPost]
        public IActionResult CrearCategoriaRapida(string nombre)
        {
            try
            {
                var nombreTrim = nombre?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(nombreTrim)) return Json(new { success = false, message = "El nombre es requerido." });

                var cadena = _config.GetConnectionString("ZeltekDB");
                using (var con = new SqlConnection(cadena))
                {
                    con.Open();
                    using (var cmdCheck = new SqlCommand("SELECT 1 FROM Categoria WHERE LOWER(RTRIM(LTRIM(Nombre))) = LOWER(@Nombre)", con))
                    {
                        cmdCheck.Parameters.AddWithValue("@Nombre", nombreTrim);
                        if (cmdCheck.ExecuteScalar() != null)
                            return Json(new { success = false, message = "⛔ Esta categoría ya existe. No se permiten duplicados." });
                    }
                    using (var cmdInsert = new SqlCommand(
                        "INSERT INTO Categoria (Nombre) VALUES (@Nombre); SELECT CAST(SCOPE_IDENTITY() AS INT);", con))
                    {
                        cmdInsert.Parameters.AddWithValue("@Nombre", nombreTrim);
                        var idRaw = cmdInsert.ExecuteScalar();
                        var id = idRaw != null && idRaw != DBNull.Value ? Convert.ToInt32(idRaw) : 0;
                        if (id <= 0) return Json(new { success = false, message = "No se pudo obtener el ID de la categoría." });
                        return Json(new { success = true, id, nombre = nombreTrim });
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = "Error al guardar la categoría: " + msg });
            }
        }

        [HttpPost]
        public IActionResult Reactivar(int id)
        {
            using var con = new SqlConnection(_config.GetConnectionString("ZeltekDB"));
            con.Execute("UPDATE Producto SET IsDeleted = 0 WHERE ProductoID = @ID", new { ID = id });
            return RedirectToAction("Edit", new { id });
        }

    }
}
