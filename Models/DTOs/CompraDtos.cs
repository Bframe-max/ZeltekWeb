namespace ZeltekWeb.Models.DTOs
{
    public class CompraDto
    {
        public int CompraID { get; set; }
        public DateTime FechaCompra { get; set; }
        public string VendedorOnline { get; set; } = string.Empty;
        public string TrackingProveedor { get; set; } = string.Empty;
        public decimal CostoTotal { get; set; }
        public decimal CostoEnvioTotal { get; set; }
        public string EstadoCompra { get; set; } = string.Empty;
    }

    public class CompraListDto : CompraDto
    {
        public int CantidadItems { get; set; }
        public int ItemsPendientes { get; set; }
        public string ProductoPrincipal { get; set; } = string.Empty;
    }

    public class CreateCompraDto
    {
        public string Vendedor { get; set; } = string.Empty;
        public string? TrackingProveedor { get; set; }
        public DateTime FechaCompra { get; set; } = DateTime.Now;
        public int ProductoID { get; set; }
        public int Cantidad { get; set; }
        public decimal CostoTotalCompra { get; set; }
        public decimal? CostoEnvioTotal { get; set; }
        public string Usuario { get; set; } = string.Empty; // For audit
    }

    public class UpdateCompraDto
    {
        public int CompraID { get; set; }
        public string? TrackingProveedor { get; set; }
        public string EstadoCompra { get; set; } = string.Empty;
        public decimal CostoTotal { get; set; }
    }

    public class ItemCompraDto
    {
        public int ItemID { get; set; }
        public int ProductoID { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public decimal CostoCompra { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public bool EsAccesorio { get; set; }
        public bool EsUnico { get; set; }
        public string? Procesador { get; set; }
        public int? Ram { get; set; }
        public int? Almacenamiento { get; set; }
    }

    public class RecibirItemDto
    {
        public int ItemID { get; set; }
        public int CompraID { get; set; }
        public bool ProductoNuevo { get; set; }
        public string? SerieReal { get; set; }
        public decimal CostoEnvio { get; set; }
        public decimal CostoReparacion { get; set; }
        public string? Condicion { get; set; }
        public string? Bateria { get; set; }
        public int? Ram { get; set; }
        public int? Almacenamiento { get; set; }
        public string? Procesador { get; set; }
    }
}
