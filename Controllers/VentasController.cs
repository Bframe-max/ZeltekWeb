using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using ZeltekWeb.Models; // Asegúrate de tener esto para VentaListViewModel

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


        public IActionResult Index(string buscar, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var lista = new List<VentaListViewModel>();
            string cadena = _config.GetConnectionString("ZeltekDB");

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                string sql = @"
                    SELECT 
                        VentaID, FechaVenta, Cliente, TipoCliente, 
                        Producto, NumeroSerie, Especificaciones, 
                        CostoTotal, PrecioVenta, Ganancia, MetodoPago
                    FROM vw_ReporteVentas
                    WHERE (@Buscar IS NULL 
                           OR Cliente LIKE '%' + @Buscar + '%' 
                           OR Producto LIKE '%' + @Buscar + '%'
                           OR NumeroSerie LIKE '%' + @Buscar + '%')
                    AND (@FInicio IS NULL OR CAST(FechaVenta AS DATE) >= @FInicio)
                    AND (@FFin IS NULL OR CAST(FechaVenta AS DATE) <= @FFin)
                    ORDER BY FechaVenta DESC";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@Buscar", string.IsNullOrEmpty(buscar) ? DBNull.Value : buscar);
                    cmd.Parameters.AddWithValue("@FInicio", fechaInicio ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FFin", fechaFin ?? (object)DBNull.Value);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lista.Add(new VentaListViewModel
                            {
                                VentaID = dr.GetInt32(0),
                                FechaVenta = dr.GetDateTime(1),
                                Cliente = dr.GetString(2),
                                TipoCliente = dr.GetString(3),
                                Producto = dr.GetString(4),
                                NumeroSerie = dr.GetString(5),
                                Especificaciones = dr.GetString(6),
                                Costo = dr.GetDecimal(7),
                                PrecioVenta = dr.GetDecimal(8),
                                Ganancia = dr.GetDecimal(9),
                                MetodoPago = dr.GetString(10)
                            });
                        }
                    }
                }
            }

            // Totales
            ViewBag.TotalVendido = lista.Sum(x => x.PrecioVenta);
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
            string metodoPago,
            string clienteNombre,
            string clienteCedula,
            string clienteCelular,
            int garantiaMeses,
            string notas,
            string tipoVenta,
            int clienteIdExistente = 0
        )
        {
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

                        if (clienteId == 0)
                        {
                            // 1. Buscar por Cédula
                            if (!string.IsNullOrEmpty(clienteCedula))
                            {
                                string sqlBuscar = "SELECT ClienteID FROM Cliente WHERE Cedula = @Cedula AND IsDeleted = 0";
                                using (SqlCommand cmd = new SqlCommand(sqlBuscar, con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@Cedula", clienteCedula);
                                    var result = cmd.ExecuteScalar();
                                    if (result != null) clienteId = (int)result;
                                }
                            }

                            // 2. Crear Cliente Nuevo
                            if (clienteId == 0)
                            {
                                string sqlCrear = @"
                                    INSERT INTO Cliente (NombreCompleto, Cedula, Telefono, TipoCliente, CreationDate, CreatedBy, IsDeleted)
                                    OUTPUT INSERTED.ClienteID
                                    VALUES (@Nombre, @Cedula, @Telefono, @Tipo, GETDATE(), @Usuario, 0)";

                                using (SqlCommand cmd = new SqlCommand(sqlCrear, con, tx))
                                {
                                    cmd.Parameters.AddWithValue("@Nombre", string.IsNullOrEmpty(clienteNombre) ? "Consumidor Final" : clienteNombre);
                                    cmd.Parameters.AddWithValue("@Cedula", string.IsNullOrEmpty(clienteCedula) ? DBNull.Value : (object)clienteCedula);
                                    cmd.Parameters.AddWithValue("@Telefono", string.IsNullOrEmpty(celularLimpio) ? "22222222" : celularLimpio);
                                    cmd.Parameters.AddWithValue("@Tipo", tipoVenta ?? "Regular");
                                    cmd.Parameters.AddWithValue("@Usuario", User.Identity.Name ?? "Sistema");
                                    clienteId = (int)cmd.ExecuteScalar();
                                }
                            }
                        }

                        // B. CREAR VENTA
                        string sqlVenta = @"
                            INSERT INTO Venta (ClienteID, FechaVenta, TotalVenta, MetodoPago, GarantiaMeses, NotasVenta, CreatedBy, TipoVenta)
                            OUTPUT INSERTED.VentaID
                            VALUES (@ClienteID, GETDATE(), @Total, @Metodo, @Garantia, @Notas, @Usuario, @Tipo)";

                        int ventaId;
                        using (SqlCommand cmd = new SqlCommand(sqlVenta, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@ClienteID", clienteId);
                            cmd.Parameters.AddWithValue("@Total", precioFinal);
                            cmd.Parameters.AddWithValue("@Metodo", metodoPago);
                            cmd.Parameters.AddWithValue("@Garantia", garantiaMeses);
                            cmd.Parameters.AddWithValue("@Notas", notas ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Usuario", User.Identity.Name ?? "Sistema");
                            cmd.Parameters.AddWithValue("@Tipo", tipoVenta ?? "Regular");
                            ventaId = (int)cmd.ExecuteScalar();
                        }

                        // C. CREAR DETALLE
                        string sqlDetalle = "INSERT INTO DetalleVenta (VentaID, ItemID, PrecioUnitario) VALUES (@VentaID, @ItemID, @Precio)";
                        using (SqlCommand cmd = new SqlCommand(sqlDetalle, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@VentaID", ventaId);
                            cmd.Parameters.AddWithValue("@ItemID", itemId);
                            cmd.Parameters.AddWithValue("@Precio", precioFinal);
                            cmd.ExecuteNonQuery();
                        }

                        // D. ACTUALIZAR INVENTARIO
                        string sqlUpdateItem = "UPDATE ItemInventario SET Estado = 'Vendido' WHERE ItemID = @ItemID";
                        using (SqlCommand cmd = new SqlCommand(sqlUpdateItem, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@ItemID", itemId);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return RedirectToAction("Index", "Inventario");
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        throw new Exception("Error SQL: " + ex.Message);
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