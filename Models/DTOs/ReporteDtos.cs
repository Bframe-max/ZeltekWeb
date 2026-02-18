using System;
using System.Collections.Generic;

namespace ZeltekWeb.Models.DTOs
{
    public class TendenciaVentasDto
    {
        public string Mes { get; set; } = string.Empty; // yyyy-MM
        public string MesLabel { get; set; } = string.Empty; // MMM yy
        public decimal TotalVentas { get; set; }
        public int CantidadVentas { get; set; }
        public int CantidadEquipos { get; set; }
    }

    public class TopProductoDto
    {
        public string ProductoNombre { get; set; } = string.Empty;
        public int CantidadVentas { get; set; }
        public decimal TotalVendido { get; set; }
    }

    public class ResumenFinancieroDto
    {
        public decimal IngresoTotal { get; set; } // Facturación / Revenue
        public decimal GananciaBruta { get; set; } // Ingreso - Costo (Profit)
        public decimal GastosOperativos { get; set; }
        public decimal GananciaNeta => GananciaBruta - GastosOperativos;
    }

    public class InventarioDormidoDto
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public int DiasEnStock { get; set; }
        public decimal CostoTotal { get; set; }
        public decimal PrecioVenta { get; set; }
        public string? Procesador { get; set; }
        public int RAM_GB { get; set; }
        public int Almacenamiento_GB { get; set; }
    }

    public class TopRevendedorDto
    {
        public string NombreCliente { get; set; } = string.Empty;
        public string TipoCliente { get; set; } = string.Empty;
        public int CantidadCompras { get; set; }
        public decimal TotalComprado { get; set; }
        public decimal GananciaGenerada { get; set; }
        public DateTime? UltimaCompra { get; set; }
    }
    
    public class ValorInventarioDto 
    {
        public decimal TotalValor { get; set; }
    }
    
    public class ReporteDashboardDto
    {
        public List<TendenciaVentasDto> Tendencia { get; set; } = new();
        public List<TopProductoDto> TopProductos { get; set; } = new();
        public ResumenFinancieroDto ResumenFinanciero { get; set; } = new();
        public ValorInventarioDto ValorInventario { get; set; } = new();
        public List<InventarioDormidoDto> InventarioDormido { get; set; } = new();
        public List<TopRevendedorDto> TopRevendedores { get; set; } = new();
        
        public int MesesTendencia { get; set; }
        public int PeriodoGananciaMeses { get; set; }
    }
}
