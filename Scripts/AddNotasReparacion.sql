-- Añade la columna NotasReparacion a ItemInventario si no existe.
-- Necesaria para: vista Reparaciones (Problema reportado) y al finalizar reparación (guardar notas).
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.ItemInventario') AND name = 'NotasReparacion'
)
BEGIN
    ALTER TABLE dbo.ItemInventario ADD NotasReparacion NVARCHAR(500) NULL;
    PRINT 'Columna NotasReparacion agregada.';
END
ELSE
    PRINT 'La columna NotasReparacion ya existe.';
