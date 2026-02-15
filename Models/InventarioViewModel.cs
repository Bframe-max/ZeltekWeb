namespace ZeltekWeb.Models
{
    public class ItemInventarioViewModel
    {
        public int ItemID { get; set; }
        public int ProductoID { get; set; }
        public string ProductoNombre { get; set; }
        public string NumeroSerie { get; set; }
        public string Estado { get; set; }

        // Costos y Precios
        public decimal CostoTotal { get; set; }
        public decimal PrecioVenta { get; set; }

        // Especificaciones
        public string Procesador { get; set; }
        public int RAM_GB { get; set; }
        public int Almacenamiento_GB { get; set; }
        public string Color { get; set; }

        // Inventario por lotes (accesorios)
        public int CantidadInicial { get; set; }
        public int CantidadActual { get; set; }
        public bool EsUnico { get; set; }
    }

    /// <summary>
    /// ViewModel para recibir un producto (laptop) con opción de asignar cargador del stock (kitting).
    /// Coincide con los parámetros de sp_RecibirLaptopConAccesorios.
    /// </summary>
    public class RecepcionProductoViewModel
    {
        public int ItemID { get; set; }
        public int CompraID { get; set; }
        public int ProductoID_Laptop { get; set; }
        public string NumeroSerie { get; set; } = "";
        public string Condicion { get; set; } = "";
        public string Procesador { get; set; } = "";
        public int? Ram { get; set; }
        public int? Almacenamiento { get; set; }
        public decimal CostoEnvio { get; set; }
        public decimal CostoOtros { get; set; }
        public bool ProductoNuevo { get; set; }
        public bool RequiereCargadorDeStock { get; set; }
        public int? IdCargador { get; set; }
        public string? SaludBateria { get; set; }
        public string? Color { get; set; }
    }
}