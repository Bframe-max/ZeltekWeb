using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ZeltekWeb.Filters;
using ZeltekWeb.Models;

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class VentasController : Controller
    {
        private readonly IConfiguration _config;

        public VentasController(IConfiguration config)
        {
            _config = config;
        }

        // =========================
        // LISTADO + RESUMEN + FILTRO
        // =========================
        public IActionResult Index(DateTime? desde, DateTime? hasta)
        {
            var lista = new List<VentaListViewModel>();
            var resumen = new VentasResumenViewModel();

            var cadena = _config.GetConnectionString("ZeltekDB");

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            // =========================
            // RESUMEN
            // =========================
            var sqlResumen = @"
                SELECT
                    COUNT(*) AS CantidadVentas,
                    SUM(d.PrecioUnitario) AS TotalVentas,
                    SUM(i.CostoCompra) AS TotalCosto
                FROM Venta v
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
                WHERE 1 = 1";

            if (desde.HasValue)
                sqlResumen += " AND v.FechaVenta >= @Desde";

            if (hasta.HasValue)
                sqlResumen += " AND v.FechaVenta <= @Hasta";

            using (var cmdResumen = new SqlCommand(sqlResumen, con))
            {
                if (desde.HasValue)
                    cmdResumen.Parameters.AddWithValue("@Desde", desde.Value.Date);

                if (hasta.HasValue)
                    cmdResumen.Parameters.AddWithValue("@Hasta", hasta.Value.Date.AddDays(1).AddSeconds(-1));

                using var dr = cmdResumen.ExecuteReader();
                if (dr.Read())
                {
                    resumen.CantidadVentas = dr.GetInt32(0);
                    resumen.TotalVentas = dr.IsDBNull(1) ? 0 : dr.GetDecimal(1);
                    resumen.TotalCosto = dr.IsDBNull(2) ? 0 : dr.GetDecimal(2);
                    resumen.GananciaTotal = resumen.TotalVentas - resumen.TotalCosto;
                }
            }

            // =========================
            // LISTADO
            // =========================
            var sql = @"
                SELECT
                    v.VentaID,
                    v.FechaVenta,
                    p.Nombre AS Producto,
                    i.NumeroSerie,
                    i.CostoCompra,
                    d.PrecioUnitario,
                    (d.PrecioUnitario - i.CostoCompra) AS Ganancia,
                    v.MetodoPago
                FROM Venta v
                INNER JOIN DetalleVenta d ON v.VentaID = d.VentaID
                INNER JOIN ItemInventario i ON d.ItemID = i.ItemID
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE 1 = 1";

            if (desde.HasValue)
                sql += " AND v.FechaVenta >= @Desde";

            if (hasta.HasValue)
                sql += " AND v.FechaVenta <= @Hasta";

            sql += " ORDER BY v.FechaVenta DESC";

            using (var cmd = new SqlCommand(sql, con))
            {
                if (desde.HasValue)
                    cmd.Parameters.AddWithValue("@Desde", desde.Value.Date);

                if (hasta.HasValue)
                    cmd.Parameters.AddWithValue("@Hasta", hasta.Value.Date.AddDays(1).AddSeconds(-1));

                using var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new VentaListViewModel
                    {
                        VentaID = dr.GetInt32(0),
                        FechaVenta = dr.GetDateTime(1),
                        Producto = dr.GetString(2),
                        NumeroSerie = dr.IsDBNull(3) ? null : dr.GetString(3),
                        Costo = dr.GetDecimal(4),
                        PrecioVenta = dr.GetDecimal(5),
                        Ganancia = dr.GetDecimal(6),
                        MetodoPago = dr.IsDBNull(7) ? "-" : dr.GetString(7)
                    });
                }
            }

            ViewBag.Resumen = resumen;
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");

            return View(lista);
        }

        // =========================
        // FORMULARIO REGISTRAR VENTA
        // =========================
        public IActionResult Create(int itemId)
        {
            var cadena = _config.GetConnectionString("ZeltekDB");

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            var sql = @"
                SELECT 
                    i.ItemID,
                    p.Nombre,
                    i.NumeroSerie,
                    i.PrecioVenta
                FROM ItemInventario i
                INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                WHERE i.ItemID = @ItemID
                  AND i.Estado = 'En Stock'";

            using SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@ItemID", itemId);

            using SqlDataReader dr = cmd.ExecuteReader();
            if (!dr.Read())
                return NotFound();

            var model = new VentaCreateViewModel
            {
                ItemID = dr.GetInt32(0),
                Producto = dr.GetString(1),
                NumeroSerie = dr.IsDBNull(2) ? null : dr.GetString(2),
                PrecioUnitario = dr.GetDecimal(3),
                MetodoPago = "Efectivo"
            };

            return View(model);
        }

        // =========================
        // GUARDAR VENTA (CON CLIENTE)
        // =========================
        [HttpPost]
        public IActionResult Create(VentaCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var cadena = _config.GetConnectionString("ZeltekDB");

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();
            using var tx = con.BeginTransaction();

            try
            {
                // =========================
                // CLIENTE: BUSCAR O CREAR
                // =========================
                int clienteId;

                var sqlBuscarCliente = @"
                    SELECT ClienteID
                    FROM Cliente
                    WHERE Cedula = @Cedula AND IsDeleted = 0";

                using (var cmdBuscar = new SqlCommand(sqlBuscarCliente, con, tx))
                {
                    cmdBuscar.Parameters.AddWithValue("@Cedula", model.Cedula);

                    var result = cmdBuscar.ExecuteScalar();

                    if (result != null)
                    {
                        clienteId = (int)result;
                    }
                    else
                    {
                        var sqlCrearCliente = @"
                            INSERT INTO Cliente
                            (Cedula, NombreCompleto, Telefono, TipoCliente, CreationDate)
                            OUTPUT INSERTED.ClienteID
                            VALUES
                            (@Cedula, @Nombre, @Telefono, @TipoCliente, GETDATE())";

                        using var cmdCrear = new SqlCommand(sqlCrearCliente, con, tx);
                        cmdCrear.Parameters.AddWithValue("@Cedula", model.Cedula);
                        cmdCrear.Parameters.AddWithValue("@Nombre", model.NombreCompleto ?? "Cliente");
                        cmdCrear.Parameters.AddWithValue("@Telefono", model.Telefono ?? "");
                        cmdCrear.Parameters.AddWithValue("@TipoCliente", model.TipoCliente);

                        clienteId = (int)cmdCrear.ExecuteScalar();
                    }
                }

                // =========================
                // CREAR VENTA
                // =========================
                var sqlVenta = @"
                    INSERT INTO Venta
                    (ClienteID, FechaVenta, TotalVenta, MetodoPago)
                    OUTPUT INSERTED.VentaID
                    VALUES
                    (@ClienteID, GETDATE(), @TotalVenta, @MetodoPago)";

                int ventaId;
                using (var cmdVenta = new SqlCommand(sqlVenta, con, tx))
                {
                    cmdVenta.Parameters.AddWithValue("@ClienteID", clienteId);
                    cmdVenta.Parameters.AddWithValue("@TotalVenta", model.PrecioUnitario);
                    cmdVenta.Parameters.AddWithValue("@MetodoPago", model.MetodoPago ?? "Efectivo");

                    ventaId = (int)cmdVenta.ExecuteScalar();
                }

                // =========================
                // DETALLE VENTA
                // =========================
                var sqlDetalle = @"
                    INSERT INTO DetalleVenta
                    (VentaID, ItemID, PrecioUnitario, GarantiaMeses, ProductoID)
                    SELECT
                        @VentaID,
                        i.ItemID,
                        @Precio,
                        @Garantia,
                        i.ProductoID
                    FROM ItemInventario i
                    WHERE i.ItemID = @ItemID";

                using (var cmdDetalle = new SqlCommand(sqlDetalle, con, tx))
                {
                    cmdDetalle.Parameters.AddWithValue("@VentaID", ventaId);
                    cmdDetalle.Parameters.AddWithValue("@ItemID", model.ItemID);
                    cmdDetalle.Parameters.AddWithValue("@Precio", model.PrecioUnitario);
                    cmdDetalle.Parameters.AddWithValue("@Garantia", model.GarantiaMeses);
                    cmdDetalle.ExecuteNonQuery();
                }

                // =========================
                // ITEM → VENDIDO
                // =========================
                var sqlUpdate = @"
                    UPDATE ItemInventario
                    SET Estado = 'Vendido'
                    WHERE ItemID = @ItemID";

                using (var cmdUpdate = new SqlCommand(sqlUpdate, con, tx))
                {
                    cmdUpdate.Parameters.AddWithValue("@ItemID", model.ItemID);
                    cmdUpdate.ExecuteNonQuery();
                }

                tx.Commit();
                return RedirectToAction("Index");
            }
            catch
            {
                tx.Rollback();
                throw;
            }




        }



        public IActionResult Rapida()
        {
            return View(new VentaRapidaViewModel());
        }




        [HttpPost]
        public IActionResult Rapida(VentaRapidaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var cadena = _config.GetConnectionString("ZeltekDB");

            using SqlConnection con = new SqlConnection(cadena);
            con.Open();

            using SqlCommand cmd = new SqlCommand("sp_RegistrarVenta", con);
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@NombreCliente", model.NombreCliente);
            cmd.Parameters.AddWithValue("@Telefono", model.Telefono);
            cmd.Parameters.AddWithValue("@TipoCliente", model.TipoCliente);

            cmd.Parameters.AddWithValue("@FechaVenta", model.FechaVenta);
            cmd.Parameters.AddWithValue("@MetodoPago", model.MetodoPago);

            cmd.Parameters.AddWithValue("@NumeroSerie",
                string.IsNullOrWhiteSpace(model.NumeroSerie)
                    ? DBNull.Value
                    : model.NumeroSerie);

            cmd.Parameters.AddWithValue("@ProductoID",
                model.ProductoID.HasValue
                    ? model.ProductoID
                    : DBNull.Value);

            cmd.Parameters.AddWithValue("@PrecioUnitario", model.PrecioUnitario);
            cmd.Parameters.AddWithValue("@GarantiaMeses", model.GarantiaMeses);

            try
            {
                cmd.ExecuteNonQuery();
                return RedirectToAction("Index");
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }





    }
}
