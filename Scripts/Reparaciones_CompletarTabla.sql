-- Completar tabla Reparaciones para el flujo de Inventario (Enviar a reparación / Finalizar).
-- Ejecutar en ZeltekDB. La tabla debe tener al menos: ReparacionID, ItemID, ProblemaReportado,
-- TrabajoRealizado, CostoTotal, Estado (NVARCHAR con longitud, ej. 50).

-- Añadir FechaEntrada (opcional; para mostrar "Fecha ingreso" en la lista)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Reparaciones') AND name = 'FechaEntrada')
BEGIN
    ALTER TABLE dbo.Reparaciones ADD FechaEntrada DATETIME NULL;
    PRINT 'Columna FechaEntrada agregada.';
END

-- Añadir FechaSalida (opcional; para historial de cuándo se finalizó)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Reparaciones') AND name = 'FechaSalida')
BEGIN
    ALTER TABLE dbo.Reparaciones ADD FechaSalida DATETIME NULL;
    PRINT 'Columna FechaSalida agregada.';
END

-- Si ProblemaReportado o TrabajoRealizado son NVARCHAR sin longitud, ampliar:
IF (SELECT max_length FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Reparaciones') AND name = 'ProblemaReportado') < 100
BEGIN
    ALTER TABLE dbo.Reparaciones ALTER COLUMN ProblemaReportado NVARCHAR(500) NULL;
    PRINT 'ProblemaReportado ampliado a NVARCHAR(500).';
END
IF (SELECT max_length FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Reparaciones') AND name = 'TrabajoRealizado') < 100
BEGIN
    ALTER TABLE dbo.Reparaciones ALTER COLUMN TrabajoRealizado NVARCHAR(500) NULL;
    PRINT 'TrabajoRealizado ampliado a NVARCHAR(500).';
END
