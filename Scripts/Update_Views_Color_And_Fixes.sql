USE [ZeltekDB] -- Ajusta el nombre de tu BD si es necesario
GO

-- 1. Vista de Reporte de Ventas (Repara el error de Especificaciones e incluye Color)
CREATE OR ALTER VIEW [dbo].[vw_ReporteVentas]
AS
SELECT 
    v.VentaID,
    v.FechaVenta,
    ISNULL(c.NombreCompleto, 'Cliente Final') AS Cliente,
    c.TipoCliente, -- Aseguramos que TipoCliente esté disponible si se usa en reportes
    p.Nombre AS Producto,
    dv.Cantidad AS CantidadVendida,
    i.NumeroSerie,
    
    -- 🔥 Nueva lógica de Especificaciones incluyendo COLOR
    ISNULL(i.Procesador, '') + 
    CASE WHEN i.RAM_GB > 0 THEN ' • ' + CAST(i.RAM_GB AS VARCHAR) + 'GB' ELSE '' END + 
    CASE WHEN i.Almacenamiento_GB > 0 THEN ' • ' + CAST(i.Almacenamiento_GB AS VARCHAR) + 'GB' ELSE '' END +
    CASE WHEN i.Color IS NOT NULL AND i.Color <> '' THEN ' • ' + i.Color ELSE '' END AS Especificaciones,

    -- 🔥 Alias exactos para C#
    CAST((CASE 
            WHEN p.EsUnico = 1 THEN (i.CostoCompra + ISNULL(i.CostoEnvio,0) + ISNULL(i.CostoReparacion,0))
            ELSE (i.CostoCompra / NULLIF(i.CantidadInicial, 1)) -- Evitar div/0 y usar CantidadInicial para lotes
        END) * dv.Cantidad AS DECIMAL(12,2)) AS CostoTotalVenta,

    CAST((dv.PrecioUnitario * dv.Cantidad) AS DECIMAL(12,2)) AS VentaTotal,

    CAST(((dv.PrecioUnitario * dv.Cantidad) - 
    ((CASE WHEN p.EsUnico = 1 THEN (i.CostoCompra + ISNULL(i.CostoEnvio,0) + ISNULL(i.CostoReparacion,0))
      ELSE (i.CostoCompra / NULLIF(i.CantidadInicial, 1)) END) * dv.Cantidad)) AS DECIMAL(12,2)) AS Ganancia, -- Alias Ganancia para coincidir con el código

    v.MetodoPago,
    ISNULL(v.CreatedBy, 'Sistema') AS Vendedor
FROM dbo.DetalleVenta dv
INNER JOIN dbo.Venta v ON dv.VentaID = v.VentaID
INNER JOIN dbo.ItemInventario i ON dv.ItemID = i.ItemID
INNER JOIN dbo.Producto p ON i.ProductoID = p.ProductoID
LEFT JOIN dbo.Cliente c ON v.ClienteID = c.ClienteID
GO

-- 2. Vista de Resumen Financiero
CREATE OR ALTER VIEW [dbo].[vw_ResumenFinanciero]
AS
SELECT 
    YEAR(FechaVenta) as Anio,
    MONTH(FechaVenta) as Mes,
    SUM(VentaTotal) as TotalFacturado, 
    SUM(Ganancia) as GananciaBruta, -- Usamos Ganancia de la vista anterior
    SUM(CantidadVendida) as CantidadEquipos,
    -- Gastos Operativos se unen normalmente por fuera o se agregan aquí si la tabla existe
    (SELECT ISNULL(SUM(Monto),0) FROM GastosOperativos WHERE YEAR(FechaGasto) = YEAR(FechaVenta) AND MONTH(FechaGasto) = MONTH(FechaVenta) AND IsDeleted = 0) as GastosOperativos,
    (SUM(Ganancia) - (SELECT ISNULL(SUM(Monto),0) FROM GastosOperativos WHERE YEAR(FechaGasto) = YEAR(FechaVenta) AND MONTH(FechaGasto) = MONTH(FechaVenta) AND IsDeleted = 0)) as GananciaNeta
FROM vw_ReporteVentas
GROUP BY YEAR(FechaVenta), MONTH(FechaVenta)
GO

-- 3. Vista de Top Clientes
CREATE OR ALTER VIEW [dbo].[vw_TopClientes]
AS
SELECT TOP 10
    Cliente,
    TipoCliente, -- Incluimos TipoCliente
    COUNT(DISTINCT VentaID) AS CantidadCompras, 
    SUM(VentaTotal) AS TotalComprado,     
    SUM(Ganancia) AS GananciaGenerada, -- Incluimos Ganancia Generada
    MAX(FechaVenta) AS UltimaCompra
FROM vw_ReporteVentas
GROUP BY Cliente, TipoCliente
ORDER BY TotalComprado DESC
GO
