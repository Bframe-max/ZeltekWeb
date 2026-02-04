using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Filters;
using ZeltekWeb.Models;

namespace ZeltekWeb.Controllers
{
    [AuthFilter]
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
                ViewBag.Marcas = ObtenerMarcas();
                ViewBag.Categorias = ObtenerCategorias();
                return View(model);
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
            cmd.Parameters.AddWithValue("@Nombre", model.Nombre);
            cmd.Parameters.AddWithValue("@Descripcion", (object?)model.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MarcaID", model.MarcaID);
            cmd.Parameters.AddWithValue("@Modelo", (object?)model.Modelo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoriaID", (object?)model.CategoriaID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EsUnico", model.EsUnico);
            cmd.Parameters.AddWithValue("@Costo", (object?)model.CostoReferencia ?? DBNull.Value);

            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
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

    }
}
