using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Filters;
using ZeltekWeb.Models;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class GastosController : Controller
    {
        private readonly IConfiguration _config;

        public GastosController(IConfiguration config)
        {
            _config = config;
        }

        // =========================
        // LISTADO + RESUMEN
        // =========================
        public IActionResult Index(DateTime? desde, DateTime? hasta)
        {
            var lista = new List<GastoListViewModel>();
            var resumen = new GastosResumenViewModel();

            var cadena = _config.GetConnectionString("ZeltekDB");
            using var con = new SqlConnection(cadena);
            con.Open();

            // -------- RESUMEN --------
            var sqlResumen = @"
                SELECT
                    COUNT(*) AS CantidadGastos,
                    ISNULL(SUM(Monto), 0) AS TotalGastos
                FROM GastoOperativo
                WHERE (@Desde IS NULL OR FechaGasto >= @Desde)
                  AND (@Hasta IS NULL OR FechaGasto <= @Hasta)
            ";

            using (var cmdResumen = new SqlCommand(sqlResumen, con))
            {
                cmdResumen.Parameters.AddWithValue("@Desde", (object?)desde ?? DBNull.Value);
                cmdResumen.Parameters.AddWithValue("@Hasta", (object?)hasta ?? DBNull.Value);

                using var drResumen = cmdResumen.ExecuteReader();
                if (drResumen.Read())
                {
                    resumen.CantidadGastos = drResumen.GetInt32(0);
                    resumen.TotalGastos = drResumen.GetDecimal(1);
                }
            }

            // -------- LISTADO --------
            var sqlListado = @"
                SELECT
                    GastoID,
                    FechaGasto,
                    Descripcion,
                    CategoriaGasto,
                    Monto
                FROM GastoOperativo
                WHERE (@Desde IS NULL OR FechaGasto >= @Desde)
                  AND (@Hasta IS NULL OR FechaGasto <= @Hasta)
                ORDER BY FechaGasto DESC
            ";

            using (var cmdListado = new SqlCommand(sqlListado, con))
            {
                cmdListado.Parameters.AddWithValue("@Desde", (object?)desde ?? DBNull.Value);
                cmdListado.Parameters.AddWithValue("@Hasta", (object?)hasta ?? DBNull.Value);

                using var dr = cmdListado.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new GastoListViewModel
                    {
                        GastoID = dr.GetInt32(0),
                        FechaGasto = dr.GetDateTime(1),
                        Descripcion = dr.GetString(2),
                        CategoriaGasto = dr.IsDBNull(3) ? null : dr.GetString(3),
                        Monto = dr.GetDecimal(4)
                    });
                }
            }

            // -------- GASTOS POR CATEGORÍA (para gráfico) --------
            var gastosPorCategoria = new List<GastoPorCategoriaViewModel>();
            var sqlCategoria = @"
                SELECT
                    ISNULL(CategoriaGasto, 'Sin categoría') AS Categoria,
                    SUM(Monto) AS Total
                FROM GastoOperativo
                WHERE (@Desde IS NULL OR FechaGasto >= @Desde)
                  AND (@Hasta IS NULL OR FechaGasto <= @Hasta)
                GROUP BY CategoriaGasto
                ORDER BY SUM(Monto) DESC";

            using (var cmdCat = new SqlCommand(sqlCategoria, con))
            {
                cmdCat.Parameters.AddWithValue("@Desde", (object?)desde ?? DBNull.Value);
                cmdCat.Parameters.AddWithValue("@Hasta", (object?)hasta ?? DBNull.Value);
                using var drCat = cmdCat.ExecuteReader();
                while (drCat.Read())
                {
                    gastosPorCategoria.Add(new GastoPorCategoriaViewModel
                    {
                        Categoria = drCat.GetString(0),
                        Total = drCat.GetDecimal(1)
                    });
                }
            }

            ViewBag.Resumen = resumen;
            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            ViewBag.GastosPorCategoria = gastosPorCategoria;

            return View(lista);
        }

        // =========================
        // CREAR
        // =========================
        public IActionResult Create()
        {
            ViewBag.Categorias = ObtenerCategorias();
            return View(new GastoCreateViewModel());
        }

        [HttpPost]
        public IActionResult Create(GastoCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = ObtenerCategorias();
                return View(model);
            }

            var cadena = _config.GetConnectionString("ZeltekDB");
            using var con = new SqlConnection(cadena);
            con.Open();

            var sql = @"
                INSERT INTO GastoOperativo
                (FechaGasto, Descripcion, CategoriaGasto, Monto)
                VALUES
                (@Fecha, @Descripcion, @Categoria, @Monto)
            ";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Fecha", model.FechaGasto);
            cmd.Parameters.AddWithValue("@Descripcion", model.Descripcion);
            cmd.Parameters.AddWithValue("@Categoria", (object?)model.CategoriaGasto ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Monto", model.Monto);

            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDITAR
        // =========================
        public IActionResult Edit(int id)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");
            using var con = new SqlConnection(cadena);
            con.Open();

            var sql = @"
                SELECT
                    GastoID,
                    FechaGasto,
                    Descripcion,
                    CategoriaGasto,
                    Monto
                FROM GastoOperativo
                WHERE GastoID = @ID
            ";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@ID", id);

            using var dr = cmd.ExecuteReader();
            if (!dr.Read())
                return NotFound();

            var model = new GastoEditViewModel
            {
                GastoID = dr.GetInt32(0),
                FechaGasto = dr.GetDateTime(1),
                Descripcion = dr.GetString(2),
                CategoriaGasto = dr.IsDBNull(3) ? null : dr.GetString(3),
                Monto = dr.GetDecimal(4)
            };

            ViewBag.Categorias = ObtenerCategorias();
            return View(model);
        }

        [HttpPost]
        public IActionResult Edit(GastoEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categorias = ObtenerCategorias();
                return View(model);
            }

            var cadena = _config.GetConnectionString("ZeltekDB");
            using var con = new SqlConnection(cadena);
            con.Open();

            var sql = @"
                UPDATE GastoOperativo
                SET
                    FechaGasto = @Fecha,
                    Descripcion = @Descripcion,
                    CategoriaGasto = @Categoria,
                    Monto = @Monto
                WHERE GastoID = @ID
            ";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Fecha", model.FechaGasto);
            cmd.Parameters.AddWithValue("@Descripcion", model.Descripcion);
            cmd.Parameters.AddWithValue("@Categoria", (object?)model.CategoriaGasto ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Monto", model.Monto);
            cmd.Parameters.AddWithValue("@ID", model.GastoID);

            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ELIMINAR
        // =========================
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");
            using var con = new SqlConnection(cadena);
            con.Open();

            var sql = "DELETE FROM GastoOperativo WHERE GastoID = @ID";

            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@ID", id);
            cmd.ExecuteNonQuery();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // CATEGORÍAS
        // =========================
        private static string[] ObtenerCategorias()
        {
            return new[]
            {
                "Logística",
                "Servicios",
                "Marketing",
                "Insumos",
                "Mantenimiento",
                "Comisiones",
                "Impuestos",
                "Otros"
            };
        }
    }
}
