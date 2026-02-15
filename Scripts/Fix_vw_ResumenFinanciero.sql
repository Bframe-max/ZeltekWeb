-- =============================================
-- Script para corregir la vista vw_ResumenFinanciero
-- Razón: La vista vw_ReporteVentas cambió sus columnas (CostoTotal -> CostoTotalVenta, PrecioVenta -> VentaTotal)
-- =============================================

USE [ZeltekDB] -- Asegúrate de usar el nombre correcto de tu BD
GO

INSERT INTO [dbo].[GastosOperativos] ([Descripcion], [Monto], [FechaGasto], [Categoria], [CreatedBy], [CreationDate], [IsDeleted]) 
VALUES ('Ajuste Inicial', 0, GETDATE(), 'General', 'Sistema', GETDATE(), 0)
GO

-- Si la vista existe, la modificamos con ALTER
CREATE OR ALTER VIEW [dbo].[vw_ResumenFinanciero]
AS
SELECT
    YEAR(v.FechaVenta) AS Anio,
    MONTH(v.FechaVenta) AS Mes,

    -- Totales de Ventas
    SUM(v.VentaTotal) AS TotalVentas,
    SUM(v.CostoTotalVenta) AS TotalCosto,
    SUM(v.Ganancia) AS GananciaBruta,

    -- Cantidad de Equipos (COUNT)
    COUNT(*) AS CantidadEquipos,

    -- Gastos Operativos (Subquery para sumar gastos del mismo mes/año)
    (
        SELECT ISNULL(SUM(g.Monto), 0)
        FROM GastosOperativos g
        WHERE YEAR(g.FechaGasto) = YEAR(v.FechaVenta)
          AND MONTH(g.FechaGasto) = MONTH(v.FechaVenta)
          AND g.IsDeleted = 0
    ) AS GastosOperativos,

    -- Ganancia Neta
    (SUM(v.Ganancia) - (
        SELECT ISNULL(SUM(g.Monto), 0)
        FROM GastosOperativos g
        WHERE YEAR(g.FechaGasto) = YEAR(v.FechaVenta)
          AND MONTH(g.FechaGasto) = MONTH(v.FechaVenta)
          AND g.IsDeleted = 0
    )) AS GananciaNeta

FROM [dbo].[vw_ReporteVentas] v
GROUP BY YEAR(v.FechaVenta), MONTH(v.FechaVenta)
GO
