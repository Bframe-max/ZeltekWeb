namespace ZeltekWeb.Models.DTOs
{
    public class InventarioItemDto
    {
        public int ItemID { get; set; }
        public int ProductoID { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public decimal CostoTotal { get; set; }
        public decimal PrecioVenta { get; set; }
        public string? Procesador { get; set; }
        public int RAM_GB { get; set; }
        public int Almacenamiento_GB { get; set; }
        public int CantidadInicial { get; set; }
        public int CantidadActual { get; set; }
        public bool EsUnico { get; set; }
        public string? Color { get; set; }
    }

    public class RecepcionProductoDto
    {
        public int ItemID { get; set; }
        public int CompraID { get; set; }
        public string? NumeroSerie { get; set; }
        public string? Condicion { get; set; }
        public string? Procesador { get; set; }
        public int? Ram { get; set; }
        public int? Almacenamiento { get; set; }
        public string? Color { get; set; }
        public decimal CostoEnvio { get; set; }
        public decimal CostoOtros { get; set; }
        public bool RequiereCargadorDeStock { get; set; }
        public int? IdCargador { get; set; }
        public string Usuario { get; set; } = string.Empty;
    }

    public class InspeccionCalidadDto
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public bool EsUnico { get; set; }
        public int CantidadTotal { get; set; }
        public int CantidadAprobada { get; set; }
        public decimal CostoUnitario { get; set; }
        public string? Condicion { get; set; }
        public int? SaludBateriaPorcentaje { get; set; }
        public string? NotasInspeccion { get; set; }
        public string? MotivoRechazo { get; set; }
    }

    public class ReparacionDto
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public string? ProblemaReportado { get; set; }
        public DateTime? FechaIngreso { get; set; }
    }

    public class FinalizarReparacionDto
    {
        public int ItemID { get; set; }
        public decimal CostoRepuestos { get; set; }
        public decimal CostoManoObra { get; set; }
        public string? NotasReparacion { get; set; }
    }
}
