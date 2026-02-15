-- =============================================================================
-- Script: Insertar garantías faltantes para ventas de HOY (12 meses por defecto)
-- Ejecutar en ZeltekDB cuando falten registros en Garantias por ventas ya hechas.
-- =============================================================================

-- Inserta en Garantias una fila por cada DetalleVenta de ventas de hoy
-- que aún no tenga registro en Garantias (VentaID + ProductoID).
-- Se asume 12 meses de garantía por defecto.
INSERT INTO Garantias (VentaID, ProductoID, FechaInicio, FechaFin, Estado)
SELECT
    dv.VentaID,
    dv.ProductoID,
    v.FechaVenta AS FechaInicio,
    DATEADD(MONTH, 12, v.FechaVenta) AS FechaFin,
    'Activa'
FROM DetalleVenta dv
INNER JOIN Venta v ON dv.VentaID = v.VentaID
WHERE CAST(v.FechaVenta AS DATE) = CAST(GETDATE() AS DATE)
  AND NOT EXISTS (
      SELECT 1
      FROM Garantias g
      WHERE g.VentaID = dv.VentaID
        AND g.ProductoID = dv.ProductoID
  );

-- Ver cuántas filas se insertaron (opcional)
-- SELECT @@ROWCOUNT AS FilasInsertadas;
