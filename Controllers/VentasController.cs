using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using ZeltekWeb.Repositories; // Para IVentaRepository
using ZeltekWeb.Models; // Asegúrate de tener esto para VentaListViewModel

namespace ZeltekWeb.Controllers
{
    [Authorize]
    public class VentasController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IVentaRepository _ventaRepo;

        public VentasController(IConfiguration config, IVentaRepository ventaRepo)
        {
            _config = config;
            _ventaRepo = ventaRepo;
        }


        public async Task<IActionResult> Index(string buscar, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var historial = await _ventaRepo.ObtenerHistorialAsync(buscar, fechaInicio, fechaFin);
            var lista = historial.ToList();

            // Totales
            ViewBag.TotalVendido = lista.Sum(x => x.VentaTotal);
            ViewBag.TotalGanancia = lista.Sum(x => x.Ganancia);
            ViewBag.TotalEquipos = lista.Count;

            // Filtros visuales
            ViewData["Busqueda"] = buscar;
            ViewData["FechaInicio"] = fechaInicio?.ToString("yyyy-MM-dd");
            ViewData["FechaFin"] = fechaFin?.ToString("yyyy-MM-dd");

            return View(lista);
        }
        // ==========================================
        // 1. REGISTRAR VENTA RÁPIDA (POST)
        // ==========================================
        [HttpPost]
        public IActionResult CrearVentaRapida(
            int itemId,
            decimal precioFinal,
            DateTime fechaVenta,
            string metodoPago,
            string clienteNombre,
            string clienteCedula,
            string clienteCelular,
            int garantiaMeses,
            string notas,
            string tipoVenta,
            int clienteIdExistente = 0,
            int? productoId = null,
            int cantidadAVender = 1
        )
        {
            // Validación de campos obligatorios
            var nombreLimpio = clienteNombre?.Trim() ?? "";
            if (string.IsNullOrEmpty(nombreLimpio))
            {
                return Json(new { success = false, message = "⚠️ El nombre del cliente es obligatorio." });
            }
            if (precioFinal <= 0)
            {
                return Json(new { success = false, message = "⚠️ El precio de venta debe ser mayor a 0." });
            }
            if (string.IsNullOrWhiteSpace(metodoPago))
            {
                return Json(new { success = false, message = "⚠️ Debe seleccionar un método de pago." });
            }

            // Normalizar y validar la fecha de la venta
            var hoy = DateTime.Now.Date;
            if (fechaVenta == default)
            {
                fechaVenta = DateTime.Now;
            }

            var fechaSoloDia = fechaVenta.Date;

            // A. No permitir fechas futuras
            if (fechaSoloDia > hoy)
            {
                return Json(new
                {
                    success = false,
                    message = "⚠️ Error: No puedes registrar ventas en el futuro."
                });
            }

            // B. No permitir más de 5 días de antigüedad
            if (fechaSoloDia < hoy.AddDays(-5))
            {
                return Json(new
                {
                    success = false,
                    message = "⚠️ Error: Por seguridad, no se permiten registrar ventas con más de 5 días de antigüedad."
                });
            }

            // Mantener hora actual (TimeOfDay) para no dejar todas en 00:00
            var ahora = DateTime.Now;
            var fechaVentaConHora = fechaSoloDia.Add(ahora.TimeOfDay);

            string cadena = _config.GetConnectionString("ZeltekDB");
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        // A. DETERMINAR EL ID DEL CLIENTE
                        int clienteId = clienteIdExistente;
                        string celularLimpio = "";

                        if (!string.IsNullOrEmpty(clienteCelular))
                        {
                            celularLimpio = Regex.Replace(clienteCelular, "[^0-9]", "");
                            if (celularLimpio.Length > 8) celularLimpio = celularLimpio.Substring(0, 8);
                        }

                        // Valores válidos para TipoCliente (CHECK en BD: ej. Normal, Revendedor)
                        string tipoCliente = tipoVenta;
                        if (string.IsNullOrWhiteSpace(tipoCliente) || (tipoCliente != "Normal" && tipoCliente != "Revendedor"))
                            tipoCliente = "Normal";

                        if (clienteId == 0)
                        {
                            // 0. Buscar por Nombre (evitar clonar cliente existente)
                            if (!string.IsNullOrEmpty(nombreLimpio))
                            {
                                string sqlPorNombre = "SELECT ClienteID FROM Cliente WHERE LTRIM(RTRIM(NombreCompleto)) = @Nombre AND IsDeleted = 0";
                                using (SqlCommand cmd = new SqlCommand(sqlPorNombre, con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@Nombre", nombreLimpio);
                                    var result = cmd.ExecuteScalar();
                                    if (result != null) clienteId = (int)result;
                                }
                            }

                            // 1. Buscar por Teléfono (evitar duplicados: cliente recurrente por celular)
                            if (clienteId == 0 && !string.IsNullOrEmpty(celularLimpio))
                            {
                                string sqlPorTelefono = "SELECT ClienteID FROM Cliente WHERE Telefono = @Telefono AND IsDeleted = 0";
                                using (SqlCommand cmd = new SqlCommand(sqlPorTelefono, con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@Telefono", celularLimpio);
                                    var result = cmd.ExecuteScalar();
                                    if (result != null) clienteId = (int)result;
                                }
                            }

                            // 2. Si no se encontró por teléfono, buscar por Cédula (opcional)
                            if (clienteId == 0 && !string.IsNullOrEmpty(clienteCedula))
                            {
                                string sqlBuscar = "SELECT ClienteID FROM Cliente WHERE Cedula = @Cedula AND IsDeleted = 0";
                                using (SqlCommand cmd = new SqlCommand(sqlBuscar, con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@Cedula", clienteCedula);
                                    var result = cmd.ExecuteScalar();
                                    if (result != null) clienteId = (int)result;
                                }
                            }

                            // 3. Crear Cliente Nuevo solo si no existe
                            if (clienteId == 0)
                            {
                                var cedulaLimpia = string.IsNullOrWhiteSpace(clienteCedula) ? null : clienteCedula.Trim();
                                string sqlCrear = @"
                                    INSERT INTO Cliente (NombreCompleto, Cedula, Telefono, TipoCliente, CreationDate, CreatedBy, IsDeleted)
                                    OUTPUT INSERTED.ClienteID
                                    VALUES (@Nombre, @Cedula, @Telefono, @Tipo, GETDATE(), @Usuario, 0)";

                                using (SqlCommand cmd = new SqlCommand(sqlCrear, con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@Nombre", nombreLimpio);
                                    cmd.Parameters.AddWithValue("@Cedula", cedulaLimpia ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Telefono", string.IsNullOrEmpty(celularLimpio) ? "22222222" : celularLimpio);
                                    cmd.Parameters.AddWithValue("@Tipo", tipoCliente);
                                    cmd.Parameters.AddWithValue("@Usuario", User.Identity.Name ?? "Sistema");
                                    clienteId = (int)cmd.ExecuteScalar();
                                }
                            }
                        }

                        // B. CREAR VENTA
                        string sqlVenta = @"
                            INSERT INTO Venta (ClienteID, FechaVenta, TotalVenta, MetodoPago, GarantiaMeses, NotasVenta, CreatedBy, TipoVenta)
                            OUTPUT INSERTED.VentaID
                            VALUES (@ClienteID, @FechaVenta, @Total, @Metodo, @Garantia, @Notas, @Usuario, @Tipo)";

                        int ventaId;
                        using (SqlCommand cmd = new SqlCommand(sqlVenta, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@ClienteID", clienteId);
                            cmd.Parameters.AddWithValue("@FechaVenta", fechaVentaConHora);
                            cmd.Parameters.AddWithValue("@Total", precioFinal);
                            cmd.Parameters.AddWithValue("@Metodo", metodoPago);
                            cmd.Parameters.AddWithValue("@Garantia", garantiaMeses);
                            cmd.Parameters.AddWithValue("@Notas", notas ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Usuario", User.Identity.Name ?? "Sistema");
                            cmd.Parameters.AddWithValue("@Tipo", tipoCliente);
                            ventaId = (int)cmd.ExecuteScalar();
                        }

                        // C. ProductoID, EsUnico y CantidadActual desde ItemInventario + Producto
                        int productoIdVal = productoId ?? 0;
                        bool esUnico = true;
                        int cantidadActual = 1;
                        string sqlItemInfo = "SELECT i.ProductoID, ISNULL(p.EsUnico, 1), ISNULL(i.CantidadActual, 0) FROM ItemInventario i INNER JOIN Producto p ON i.ProductoID = p.ProductoID WHERE i.ItemID = @ItemID";
                        using (SqlCommand cmdInfo = new SqlCommand(sqlItemInfo, con, tx))
                        {
                            cmdInfo.Parameters.AddWithValue("@ItemID", itemId);
                            using (var drInfo = cmdInfo.ExecuteReader())
                            {
                                if (!drInfo.Read())
                                {
                                    tx.Rollback();
                                    return Json(new { success = false, message = "Error Técnico: Ítem no encontrado." });
                                }
                                productoIdVal = drInfo.GetInt32(0);
                                esUnico = drInfo.GetBoolean(1);
                                cantidadActual = drInfo.GetInt32(2);
                            }
                        }

                        int cantidad = cantidadAVender;
                        if (cantidad < 1) cantidad = 1;
                        if (esUnico) cantidad = 1;
                        else
                        {
                            if (cantidad > cantidadActual)
                            {
                                tx.Rollback();
                                return Json(new { success = false, message = $"⚠️ Stock insuficiente. Disponible: {cantidadActual}, solicitado: {cantidad}." });
                            }
                        }

                        decimal totalVenta = precioFinal * cantidad;

                        // B. Actualizar Total de la Venta (ya insertada con precioFinal; corregir si cantidad > 1)
                        if (cantidad > 1)
                        {
                            string sqlUpdTotal = "UPDATE Venta SET TotalVenta = @Total WHERE VentaID = @VentaID";
                            using (SqlCommand cmdUpd = new SqlCommand(sqlUpdTotal, con, tx))
                            {
                                cmdUpd.Parameters.AddWithValue("@Total", totalVenta);
                                cmdUpd.Parameters.AddWithValue("@VentaID", ventaId);
                                cmdUpd.ExecuteNonQuery();
                            }
                        }

                        // D. CREAR DETALLE: uno por unidad (VentaID, ItemID, ProductoID, PrecioUnitario)
                        string sqlDetalle = "INSERT INTO DetalleVenta (VentaID, ItemID, ProductoID, PrecioUnitario) VALUES (@VentaID, @ItemID, @ProductoID, @Precio)";
                        for (int k = 0; k < cantidad; k++)
                        {
                            using (SqlCommand cmd = new SqlCommand(sqlDetalle, con, tx))
                            {
                                cmd.Parameters.AddWithValue("@VentaID", ventaId);
                                cmd.Parameters.AddWithValue("@ItemID", itemId);
                                cmd.Parameters.AddWithValue("@ProductoID", productoIdVal);
                                cmd.Parameters.AddWithValue("@Precio", precioFinal);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // E. CREAR REGISTRO EN GARANTÍAS (si el usuario seleccionó garantía válida) — solo una por venta/ítem
                        if (garantiaMeses > 0)
                        {
                            var fechaInicioGarantia = fechaVentaConHora;
                            var fechaFinGarantia = fechaVentaConHora.AddMonths(garantiaMeses);
                            string sqlGarantia = @"
                                INSERT INTO Garantias (VentaID, ProductoID, ItemID, FechaInicio, FechaFin, Estado)
                                VALUES (@VentaID, @ProductoID, @ItemID, @FechaInicio, @FechaFin, 'Activa')";
                            using (SqlCommand cmdGar = new SqlCommand(sqlGarantia, con, tx))
                            {
                                cmdGar.Parameters.AddWithValue("@VentaID", ventaId);
                                cmdGar.Parameters.AddWithValue("@ProductoID", productoIdVal);
                                cmdGar.Parameters.AddWithValue("@ItemID", itemId);
                                cmdGar.Parameters.AddWithValue("@FechaInicio", fechaInicioGarantia);
                                cmdGar.Parameters.AddWithValue("@FechaFin", fechaFinGarantia);
                                cmdGar.ExecuteNonQuery();
                            }
                        }

                        // F. ACTUALIZAR INVENTARIO
                        int filasActualizadas;
                        if (esUnico)
                        {
                            string sqlUpdateItem = "UPDATE ItemInventario SET Estado = 'Vendido' WHERE ItemID = @ItemID AND Estado = 'En Stock'";
                            using (SqlCommand cmd = new SqlCommand(sqlUpdateItem, con, tx))
                            {
                                cmd.Parameters.AddWithValue("@ItemID", itemId);
                                filasActualizadas = cmd.ExecuteNonQuery();
                            }
                            if (filasActualizadas == 0)
                            {
                                tx.Rollback();
                                return Json(new { success = false, message = "⚠️ El producto ya fue vendido. Actualiza la página e intenta con otro ítem." });
                            }
                        }
                        else
                        {
                            string sqlUpdateAcc = "UPDATE ItemInventario SET CantidadActual = CantidadActual - @Cantidad WHERE ItemID = @ItemID AND Estado = 'En Stock' AND CantidadActual >= @Cantidad";
                            using (SqlCommand cmd = new SqlCommand(sqlUpdateAcc, con, tx))
                            {
                                cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                                cmd.Parameters.AddWithValue("@ItemID", itemId);
                                filasActualizadas = cmd.ExecuteNonQuery();
                            }
                            if (filasActualizadas == 0)
                            {
                                tx.Rollback();
                                return Json(new { success = false, message = "⚠️ Stock insuficiente o ítem no disponible. Actualiza la página." });
                            }
                        }

                        tx.Commit();
                        return Json(new
                        {
                            success = true,
                            message = "✅ Venta registrada correctamente."
                        });
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        var msgTecnico = "Error Técnico: " + ex.Message
                            + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                        return Json(new { success = false, message = msgTecnico });
                    }
                }
            }
        }

        // ==========================================
        // API: INFO PRODUCTO PARA PRECIO SUGERIDO (GET)
        // ==========================================
        /// <summary>
        /// Devuelve costo total del ítem (incl. reparaciones), precio sugerido (costo * 1.30) y nombre para la UX de venta.
        /// </summary>
        [HttpGet]
        public IActionResult GetProductoInfo(int id)
        {
            if (id <= 0) return Json(null);
            string cadena = _config.GetConnectionString("ZeltekDB");
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                string sql = @"
                    SELECT 
                        (i.CostoCompra + ISNULL(i.CostoEnvio, 0) + ISNULL(i.CostoReparacion, 0)) AS CostoTotal,
                        p.Nombre
                    FROM ItemInventario i
                    INNER JOIN Producto p ON i.ProductoID = p.ProductoID
                    WHERE i.ItemID = @ItemID";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@ItemID", id);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (!dr.Read()) return Json(null);
                        decimal costo = dr.GetDecimal(0);
                        string nombre = dr.GetString(1);
                        decimal precioSugerido = Math.Round(costo * 1.30m, 2);
                        return Json(new { costo, precioSugerido, nombre });
                    }
                }
            }
        }

        // ==========================================
        // 2. API: BUSCAR CLIENTES (GET)
        // ==========================================
        [HttpGet]
        public IActionResult BuscarClientes(string termino)
        {
            var resultados = new List<object>();
            string cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                string sql = @"
                    SELECT TOP 5 
                        c.ClienteID, c.NombreCompleto, c.Cedula, c.Telefono,
                        (SELECT TOP 1 FORMAT(FechaVenta, 'dd/MM/yyyy') FROM Venta WHERE ClienteID = c.ClienteID ORDER BY FechaVenta DESC) as UltimaCompra
                    FROM Cliente c
                    WHERE (c.NombreCompleto LIKE '%' + @Term + '%' OR c.Cedula LIKE '%' + @Term + '%')
                    AND c.IsDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Term", termino);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            resultados.Add(new
                            {
                                id = dr.GetInt32(0),
                                nombre = dr.GetString(1),
                                cedula = dr.IsDBNull(2) ? "" : dr.GetString(2),
                                telefono = dr.IsDBNull(3) ? "" : dr.GetString(3),
                                ultima = dr.IsDBNull(4) ? "Nuevo" : dr.GetString(4)
                            });
                        }
                    }
                }
            }
            return Json(resultados);
        }
    }
}