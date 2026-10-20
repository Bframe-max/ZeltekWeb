/* =====================================================================
   ZELTEK - Script de creación de la base de datos con datos de prueba
   Ejecutar completo en SQL Server Management Studio (F5).
   Usuario de prueba para el sistema:  admin  /  Zeltek2026!
   ===================================================================== */
USE master;
GO
IF DB_ID('zelteknic') IS NOT NULL
BEGIN
    PRINT 'La base de datos zelteknic ya existe. No se hizo ningún cambio.';
    SET NOEXEC ON;
END
GO
CREATE DATABASE zelteknic;
GO
USE zelteknic;
GO

/* ============================ TABLAS ============================ */

CREATE TABLE Categoria (
    CategoriaID INT IDENTITY(1,1) CONSTRAINT PK_Categoria PRIMARY KEY,
    Nombre      NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categoria_Nombre UNIQUE
);

CREATE TABLE Marca (
    MarcaID INT IDENTITY(1,1) CONSTRAINT PK_Marca PRIMARY KEY,
    Nombre  NVARCHAR(100) NOT NULL CONSTRAINT UQ_Marca_Nombre UNIQUE,
    Estado  NVARCHAR(20)  NOT NULL CONSTRAINT DF_Marca_Estado DEFAULT 'Activa'
);

CREATE TABLE Producto (
    ProductoID      INT IDENTITY(1,1) CONSTRAINT PK_Producto PRIMARY KEY,
    Nombre          NVARCHAR(150) NOT NULL,
    Descripcion     NVARCHAR(500) NULL,
    MarcaID         INT NULL CONSTRAINT FK_Producto_Marca REFERENCES Marca(MarcaID),
    Modelo          NVARCHAR(100) NULL,
    EsUnico         BIT NOT NULL CONSTRAINT DF_Producto_EsUnico DEFAULT 1,
    CategoriaID     INT NOT NULL CONSTRAINT FK_Producto_Categoria REFERENCES Categoria(CategoriaID),
    CostoReferencia DECIMAL(18,2) NOT NULL CONSTRAINT DF_Producto_Costo DEFAULT 0,
    IsDeleted       BIT NOT NULL CONSTRAINT DF_Producto_IsDeleted DEFAULT 0
);

CREATE TABLE Proveedor (
    ProveedorID INT IDENTITY(1,1) CONSTRAINT PK_Proveedor PRIMARY KEY,
    Nombre      NVARCHAR(150) NOT NULL
);

CREATE TABLE Courier (
    CourierID INT IDENTITY(1,1) CONSTRAINT PK_Courier PRIMARY KEY,
    Nombre    NVARCHAR(100) NOT NULL
);

CREATE TABLE Envio (
    EnvioID                INT IDENTITY(1,1) CONSTRAINT PK_Envio PRIMARY KEY,
    CourierID              INT NULL CONSTRAINT FK_Envio_Courier REFERENCES Courier(CourierID),
    FechaRecibidoMiami     DATETIME NULL,
    FechaEnvio             DATETIME NULL,
    FechaRecibidoNicaragua DATETIME NULL,
    Tipo                   NVARCHAR(50) NULL,
    CostoTotal             DECIMAL(18,2) NOT NULL CONSTRAINT DF_Envio_Costo DEFAULT 0,
    TrackingCourier        NVARCHAR(100) NULL
);

CREATE TABLE Compra (
    CompraID          INT IDENTITY(1,1) CONSTRAINT PK_Compra PRIMARY KEY,
    ProveedorID       INT NOT NULL CONSTRAINT FK_Compra_Proveedor REFERENCES Proveedor(ProveedorID),
    VendedorOnline    NVARCHAR(150) NULL,
    FechaCompra       DATETIME NOT NULL CONSTRAINT DF_Compra_Fecha DEFAULT GETDATE(),
    TrackingProveedor NVARCHAR(100) NULL,
    URLCompra         NVARCHAR(300) NULL,
    CostoTotal        DECIMAL(18,2) NOT NULL CONSTRAINT DF_Compra_Costo DEFAULT 0,
    EnvioID           INT NULL CONSTRAINT FK_Compra_Envio REFERENCES Envio(EnvioID),
    EstadoCompra      NVARCHAR(50) NOT NULL CONSTRAINT DF_Compra_Estado DEFAULT 'En Transito',
    CostoEnvioTotal   DECIMAL(18,2) NOT NULL CONSTRAINT DF_Compra_CostoEnvio DEFAULT 0
);

CREATE TABLE ItemInventario (
    ItemID            INT IDENTITY(1,1) CONSTRAINT PK_ItemInventario PRIMARY KEY,
    ProductoID        INT NOT NULL CONSTRAINT FK_Item_Producto REFERENCES Producto(ProductoID),
    CompraID          INT NOT NULL CONSTRAINT FK_Item_Compra REFERENCES Compra(CompraID),
    NumeroSerie       NVARCHAR(100) NULL,
    Condicion         NVARCHAR(50) NULL,
    SaludBateria      INT NULL CONSTRAINT CK_Item_Bateria CHECK (SaludBateria BETWEEN 0 AND 100),
    Procesador        NVARCHAR(100) NULL,
    RAM_GB            INT NULL,
    Almacenamiento_GB INT NULL,
    Color             NVARCHAR(50) NULL,
    CostoCompra       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Item_CostoCompra DEFAULT 0,
    CostoReparacion   DECIMAL(18,2) NOT NULL CONSTRAINT DF_Item_CostoRep DEFAULT 0,
    CostoEnvio        DECIMAL(18,2) NOT NULL CONSTRAINT DF_Item_CostoEnvio DEFAULT 0,
    PrecioVenta       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Item_Precio DEFAULT 0,
    Estado            NVARCHAR(50) NOT NULL CONSTRAINT DF_Item_Estado DEFAULT 'En Transito',
    CantidadInicial   INT NOT NULL CONSTRAINT DF_Item_CantIni DEFAULT 1,
    CantidadActual    INT NOT NULL CONSTRAINT DF_Item_CantAct DEFAULT 1,
    FechaRecepcion    DATETIME NULL,
    NotasReparacion   NVARCHAR(500) NULL
);
-- Un número de serie no puede repetirse (los ítems en tránsito aún no lo tienen)
CREATE UNIQUE INDEX UX_Item_NumeroSerie ON ItemInventario(NumeroSerie) WHERE NumeroSerie IS NOT NULL AND NumeroSerie <> '';

CREATE TABLE Cliente (
    ClienteID      INT IDENTITY(1,1) CONSTRAINT PK_Cliente PRIMARY KEY,
    Cedula         NVARCHAR(20) NULL,
    NombreCompleto NVARCHAR(150) NOT NULL,
    TipoCliente    NVARCHAR(50) NULL,
    Telefono       NVARCHAR(20) NULL,
    CreationDate   DATETIME NOT NULL CONSTRAINT DF_Cliente_Fecha DEFAULT GETDATE(),
    IsDeleted      BIT NOT NULL CONSTRAINT DF_Cliente_IsDeleted DEFAULT 0
);

CREATE TABLE Venta (
    VentaID       INT IDENTITY(1,1) CONSTRAINT PK_Venta PRIMARY KEY,
    ClienteID     INT NULL CONSTRAINT FK_Venta_Cliente REFERENCES Cliente(ClienteID),
    FechaVenta    DATETIME NOT NULL CONSTRAINT DF_Venta_Fecha DEFAULT GETDATE(),
    TotalVenta    DECIMAL(18,2) NOT NULL CONSTRAINT DF_Venta_Total DEFAULT 0,
    MetodoPago    NVARCHAR(50) NULL,
    GarantiaMeses INT NOT NULL CONSTRAINT DF_Venta_Garantia DEFAULT 0,
    NotasVenta    NVARCHAR(500) NULL,
    TipoVenta     NVARCHAR(50) NULL,
    CreatedBy     NVARCHAR(50) NULL
);

CREATE TABLE DetalleVenta (
    DetalleVentaID INT IDENTITY(1,1) CONSTRAINT PK_DetalleVenta PRIMARY KEY,
    VentaID        INT NOT NULL CONSTRAINT FK_Detalle_Venta REFERENCES Venta(VentaID) ON DELETE CASCADE,
    ItemID         INT NOT NULL CONSTRAINT FK_Detalle_Item REFERENCES ItemInventario(ItemID),
    ProductoID     INT NOT NULL CONSTRAINT FK_Detalle_Producto REFERENCES Producto(ProductoID),
    PrecioUnitario DECIMAL(18,2) NOT NULL,
    GarantiaMeses  INT NOT NULL CONSTRAINT DF_Detalle_Garantia DEFAULT 0,
    Cantidad       INT NOT NULL CONSTRAINT DF_Detalle_Cantidad DEFAULT 1 CONSTRAINT CK_Detalle_Cantidad CHECK (Cantidad > 0)
);

CREATE TABLE Garantias (
    GarantiaID   INT IDENTITY(1,1) CONSTRAINT PK_Garantias PRIMARY KEY,
    VentaID      INT NOT NULL CONSTRAINT FK_Garantia_Venta REFERENCES Venta(VentaID),
    ProductoID   INT NOT NULL CONSTRAINT FK_Garantia_Producto REFERENCES Producto(ProductoID),
    ItemID       INT NOT NULL CONSTRAINT FK_Garantia_Item REFERENCES ItemInventario(ItemID),
    FechaInicio  DATETIME NOT NULL,
    FechaFin     DATETIME NOT NULL,
    Estado       NVARCHAR(50) NOT NULL CONSTRAINT DF_Garantia_Estado DEFAULT 'Activa',
    TipoGarantia NVARCHAR(50) NULL CONSTRAINT DF_Garantia_Tipo DEFAULT 'Tienda'
);

CREATE TABLE Reparaciones (
    ReparacionID      INT IDENTITY(1,1) CONSTRAINT PK_Reparaciones PRIMARY KEY,
    ItemID            INT NOT NULL CONSTRAINT FK_Reparacion_Item REFERENCES ItemInventario(ItemID),
    GarantiaID        INT NULL CONSTRAINT FK_Reparacion_Garantia REFERENCES Garantias(GarantiaID),
    FechaIngreso      DATETIME NOT NULL CONSTRAINT DF_Reparacion_Ingreso DEFAULT GETDATE(),
    FechaSalida       DATETIME NULL,
    ProblemaReportado NVARCHAR(500) NULL,
    TrabajoRealizado  NVARCHAR(500) NULL,
    CostoRepuestos    DECIMAL(18,2) NOT NULL CONSTRAINT DF_Reparacion_Rep DEFAULT 0,
    CostoManoObra     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Reparacion_MO DEFAULT 0,
    CostoTotal        AS (CostoRepuestos + CostoManoObra),
    Estado            NVARCHAR(50) NOT NULL CONSTRAINT DF_Reparacion_Estado DEFAULT 'En Taller'
);

CREATE TABLE UsuarioSistema (
    UsuarioID    INT IDENTITY(1,1) CONSTRAINT PK_UsuarioSistema PRIMARY KEY,
    Usuario      NVARCHAR(50) NOT NULL CONSTRAINT UQ_Usuario UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Activo       BIT NOT NULL CONSTRAINT DF_Usuario_Activo DEFAULT 1,
    Rol          NVARCHAR(50) NOT NULL CONSTRAINT DF_Usuario_Rol DEFAULT 'Vendedor'
);

CREATE TABLE RegistroPerdidas (
    PerdidaID         INT IDENTITY(1,1) CONSTRAINT PK_RegistroPerdidas PRIMARY KEY,
    ProductoID        INT NOT NULL CONSTRAINT FK_Perdida_Producto REFERENCES Producto(ProductoID),
    UsuarioID         INT NULL CONSTRAINT FK_Perdida_Usuario REFERENCES UsuarioSistema(UsuarioID),
    NumeroSerie       NVARCHAR(100) NULL,
    Cantidad          INT NOT NULL CONSTRAINT DF_Perdida_Cantidad DEFAULT 1,
    CostoTotalPerdida DECIMAL(18,2) NOT NULL CONSTRAINT DF_Perdida_Costo DEFAULT 0,
    Motivo            NVARCHAR(300) NULL,
    FechaRegistro     DATETIME NOT NULL CONSTRAINT DF_Perdida_Fecha DEFAULT GETDATE()
);

CREATE TABLE GastoOperativo (
    GastoID        INT IDENTITY(1,1) CONSTRAINT PK_GastoOperativo PRIMARY KEY,
    Descripcion    NVARCHAR(300) NOT NULL,
    Monto          DECIMAL(18,2) NOT NULL CONSTRAINT CK_Gasto_Monto CHECK (Monto >= 0),
    CategoriaGasto NVARCHAR(100) NULL,
    FechaGasto     DATETIME NOT NULL CONSTRAINT DF_Gasto_Fecha DEFAULT GETDATE()
);

CREATE TABLE MovimientoCapital (
    MovimientoID   INT IDENTITY(1,1) CONSTRAINT PK_MovimientoCapital PRIMARY KEY,
    Fecha          DATETIME NOT NULL CONSTRAINT DF_Mov_Fecha DEFAULT GETDATE(),
    Monto          DECIMAL(18,2) NOT NULL,
    TipoMovimiento NVARCHAR(50) NOT NULL,
    Origen         NVARCHAR(200) NULL,
    Observacion    NVARCHAR(500) NULL
);

CREATE TABLE Retiro (
    RetiroID INT IDENTITY(1,1) CONSTRAINT PK_Retiro PRIMARY KEY,
    Monto    DECIMAL(18,2) NOT NULL,
    Fecha    DATETIME NOT NULL CONSTRAINT DF_Retiro_Fecha DEFAULT GETDATE(),
    Motivo   NVARCHAR(300) NULL
);

CREATE TABLE CuentasPorCobrar (
    CobroID       INT IDENTITY(1,1) CONSTRAINT PK_CuentasPorCobrar PRIMARY KEY,
    Deudor        NVARCHAR(150) NOT NULL,
    Monto         DECIMAL(18,2) NOT NULL,
    Descripcion   NVARCHAR(300) NULL,
    FechaPrestamo DATETIME NOT NULL CONSTRAINT DF_Cobro_Fecha DEFAULT GETDATE(),
    Estado        NVARCHAR(50) NOT NULL CONSTRAINT DF_Cobro_Estado DEFAULT 'Pendiente'
);
GO

/* ============================ VISTAS ============================ */

CREATE VIEW vw_ReporteVentas
AS
SELECT
    v.VentaID,
    v.FechaVenta,
    ISNULL(c.NombreCompleto, 'Cliente Final') AS Cliente,
    c.TipoCliente,
    p.Nombre AS Producto,
    dv.Cantidad AS CantidadVendida,
    i.NumeroSerie,
    ISNULL(i.Procesador, '')
      + CASE WHEN i.RAM_GB > 0 THEN ' • ' + CAST(i.RAM_GB AS VARCHAR(10)) + 'GB RAM' ELSE '' END
      + CASE WHEN i.Almacenamiento_GB > 0 THEN ' • ' + CAST(i.Almacenamiento_GB AS VARCHAR(10)) + 'GB' ELSE '' END
      + CASE WHEN ISNULL(i.Color, '') <> '' THEN ' • ' + i.Color ELSE '' END AS Especificaciones,
    CAST((CASE WHEN p.EsUnico = 1 THEN i.CostoCompra + i.CostoEnvio + i.CostoReparacion
               ELSE i.CostoCompra / NULLIF(i.CantidadInicial, 0) END) * dv.Cantidad AS DECIMAL(12,2)) AS CostoTotalVenta,
    CAST(dv.PrecioUnitario * dv.Cantidad AS DECIMAL(12,2)) AS VentaTotal,
    CAST(dv.PrecioUnitario * dv.Cantidad
         - (CASE WHEN p.EsUnico = 1 THEN i.CostoCompra + i.CostoEnvio + i.CostoReparacion
                 ELSE i.CostoCompra / NULLIF(i.CantidadInicial, 0) END) * dv.Cantidad AS DECIMAL(12,2)) AS Ganancia,
    v.MetodoPago,
    ISNULL(v.CreatedBy, 'Sistema') AS Vendedor
FROM DetalleVenta dv
INNER JOIN Venta v          ON dv.VentaID = v.VentaID
INNER JOIN ItemInventario i ON dv.ItemID = i.ItemID
INNER JOIN Producto p       ON i.ProductoID = p.ProductoID
LEFT  JOIN Cliente c        ON v.ClienteID = c.ClienteID;
GO

CREATE VIEW vw_ResumenFinanciero
AS
SELECT
    r.Anio, r.Mes, r.TotalVentas, r.TotalCosto, r.GananciaBruta, r.CantidadEquipos,
    ISNULL(g.Gastos, 0) AS GastosOperativos,
    r.GananciaBruta - ISNULL(g.Gastos, 0) AS GananciaNeta
FROM (
    SELECT YEAR(FechaVenta) AS Anio, MONTH(FechaVenta) AS Mes,
           SUM(VentaTotal) AS TotalVentas, SUM(CostoTotalVenta) AS TotalCosto,
           SUM(Ganancia) AS GananciaBruta, SUM(CantidadVendida) AS CantidadEquipos
    FROM vw_ReporteVentas
    GROUP BY YEAR(FechaVenta), MONTH(FechaVenta)
) r
LEFT JOIN (
    SELECT YEAR(FechaGasto) AS Anio, MONTH(FechaGasto) AS Mes, SUM(Monto) AS Gastos
    FROM GastoOperativo
    GROUP BY YEAR(FechaGasto), MONTH(FechaGasto)
) g ON g.Anio = r.Anio AND g.Mes = r.Mes;
GO

CREATE VIEW vw_TopClientes
AS
SELECT TOP 10
    Cliente, TipoCliente,
    COUNT(DISTINCT VentaID) AS CantidadCompras,
    SUM(VentaTotal) AS TotalComprado,
    SUM(Ganancia) AS GananciaGenerada,
    MAX(FechaVenta) AS UltimaCompra
FROM vw_ReporteVentas
GROUP BY Cliente, TipoCliente
ORDER BY TotalComprado DESC;
GO

CREATE VIEW vw_GarantiasVigentes
AS
SELECT
    ISNULL(c.NombreCompleto, 'Cliente Final') AS Cliente,
    ISNULL(c.Telefono, '') AS Telefono,
    p.Nombre AS Producto,
    ISNULL(i.NumeroSerie, '') AS NumeroSerie,
    v.FechaVenta,
    DATEDIFF(MONTH, g.FechaInicio, g.FechaFin) AS GarantiaMeses,
    g.FechaFin AS FechaVenceGarantia,
    DATEDIFF(DAY, GETDATE(), g.FechaFin) AS DiasRestantes,
    i.Procesador,
    ISNULL(i.RAM_GB, 0) AS RAM_GB,
    ISNULL(i.Almacenamiento_GB, 0) AS Almacenamiento_GB,
    g.Estado
FROM Garantias g
INNER JOIN Venta v          ON g.VentaID = v.VentaID
INNER JOIN ItemInventario i ON g.ItemID = i.ItemID
INNER JOIN Producto p       ON g.ProductoID = p.ProductoID
LEFT  JOIN Cliente c        ON v.ClienteID = c.ClienteID
WHERE g.Estado = 'Activa' AND g.FechaFin >= CAST(GETDATE() AS DATE);
GO

/* ===================== PROCEDIMIENTO ALMACENADO ===================== */

CREATE PROCEDURE sp_RecibirLaptopConAccesorios
    @ItemID            INT,
    @NumeroSerie       NVARCHAR(100),
    @Condicion         NVARCHAR(50),
    @Procesador        NVARCHAR(100) = NULL,
    @Ram               INT = NULL,
    @Almacenamiento    INT = NULL,
    @Color             NVARCHAR(50) = NULL,
    @CostoEnvio        DECIMAL(18,2) = 0,
    @IdCargador        INT = NULL      -- ProductoID del cargador que se toma del stock
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE ItemInventario
        SET NumeroSerie = @NumeroSerie,
            Condicion = @Condicion,
            Procesador = @Procesador,
            RAM_GB = @Ram,
            Almacenamiento_GB = @Almacenamiento,
            Color = @Color,
            CostoEnvio = @CostoEnvio,
            Estado = CASE WHEN @Condicion LIKE '%Nuevo%' THEN 'En Stock' ELSE 'Por Revisar' END,
            FechaRecepcion = GETDATE()
        WHERE ItemID = @ItemID;

        IF @IdCargador IS NOT NULL
            UPDATE TOP (1) ItemInventario
            SET CantidadActual = CantidadActual - 1
            WHERE ProductoID = @IdCargador AND Estado = 'En Stock' AND CantidadActual > 0;

        -- Si ya no quedan ítems en camino, la compra queda recibida
        UPDATE c SET EstadoCompra = 'Recibida'
        FROM Compra c
        INNER JOIN ItemInventario i ON i.CompraID = c.CompraID AND i.ItemID = @ItemID
        WHERE NOT EXISTS (SELECT 1 FROM ItemInventario x
                          WHERE x.CompraID = c.CompraID
                            AND x.Estado NOT IN ('En Stock', 'Vendido', 'Por Revisar'));

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

/* ========================= DATOS DE PRUEBA ========================= */

-- Usuario: admin / Zeltek2026!  (hash PBKDF2 compatible con PasswordHasher de ASP.NET Core)
INSERT INTO UsuarioSistema (Usuario, PasswordHash, Activo, Rol) VALUES
('admin', 'AQAAAAIAAYagAAAAEBxubFLkszzY8PBOSfCZnz/RVPbO0TBmzw63g3O0KybsupQkRu/F9SX224OX2suFYw==', 1, 'Admin');

INSERT INTO Categoria (Nombre) VALUES ('Laptops'), ('Celulares'), ('Tablets'), ('Cargadores'), ('Accesorios');

INSERT INTO Marca (Nombre) VALUES ('Apple'), ('Dell'), ('HP'), ('Lenovo'), ('Samsung'), ('Genérico');

INSERT INTO Producto (Nombre, Descripcion, MarcaID, Modelo, EsUnico, CategoriaID, CostoReferencia) VALUES
('MacBook Air M1',        'Laptop Apple 13 pulgadas',          1, 'A2337',        1, 1, 450),  -- 1
('Dell Latitude 7420',    'Laptop empresarial 14 pulgadas',    2, '7420',         1, 1, 280),  -- 2
('HP EliteBook 840 G7',   'Laptop empresarial 14 pulgadas',    3, '840 G7',       1, 1, 260),  -- 3
('Lenovo ThinkPad T14',   'Laptop empresarial 14 pulgadas',    4, 'T14 Gen 2',    1, 1, 300),  -- 4
('iPhone 13',             'Teléfono Apple 128GB',              1, 'A2482',        1, 2, 380),  -- 5
('Samsung Galaxy S22',    'Teléfono Samsung 128GB',            5, 'SM-S901U',     1, 2, 300),  -- 6
('iPad 9na generación',   'Tablet Apple 10.2 pulgadas',        1, 'A2602',        1, 3, 220),  -- 7
('Cargador USB-C 65W',    'Cargador universal para laptops',   6, 'USB-C 65W',    0, 4, 8),    -- 8
('Mouse inalámbrico',     'Mouse óptico 2.4GHz',               6, 'M-200',        0, 5, 4);    -- 9

INSERT INTO Proveedor (Nombre) VALUES ('eBay'), ('Amazon'), ('Back Market');
INSERT INTO Courier (Nombre) VALUES ('Cargo Expreso'), ('DHL'), ('Nica Box');

INSERT INTO Envio (CourierID, FechaRecibidoMiami, FechaEnvio, FechaRecibidoNicaragua, Tipo, CostoTotal, TrackingCourier) VALUES
(1, DATEADD(DAY,-115,GETDATE()), DATEADD(DAY,-112,GETDATE()), DATEADD(DAY,-105,GETDATE()), 'Aéreo', 60, 'CE-100231'),
(3, DATEADD(DAY,-55,GETDATE()),  DATEADD(DAY,-52,GETDATE()),  DATEADD(DAY,-46,GETDATE()),  'Aéreo', 45, 'NB-554120'),
(2, DATEADD(DAY,-3,GETDATE()),   NULL, NULL, 'Aéreo', 0, 'DHL-889120');

INSERT INTO Compra (ProveedorID, VendedorOnline, FechaCompra, TrackingProveedor, URLCompra, CostoTotal, EnvioID, EstadoCompra, CostoEnvioTotal) VALUES
(1, 'techdeals_usa',     DATEADD(DAY,-120,GETDATE()), '1Z999AA10123456784', 'https://www.ebay.com/itm/1001', 1230, 1, 'Recibida', 60),  -- 1
(2, 'Amazon Renewed',    DATEADD(DAY,-60,GETDATE()),  'TBA30012345678',     'https://www.amazon.com/dp/B01', 1500, 2, 'Recibida', 45),  -- 2
(1, 'laptop_outlet',     DATEADD(DAY,-7,GETDATE()),   '9400111899223344',   'https://www.ebay.com/itm/1002',  860, 3, 'En Transito', 0), -- 3
(3, 'Back Market',       DATEADD(DAY,-2,GETDATE()),   'BM-77120034',        'https://www.backmarket.com/p/1', 380, NULL, 'En Transito', 0); -- 4

INSERT INTO ItemInventario (ProductoID, CompraID, NumeroSerie, Condicion, SaludBateria, Procesador, RAM_GB, Almacenamiento_GB, Color,
                            CostoCompra, CostoReparacion, CostoEnvio, PrecioVenta, Estado, CantidadInicial, CantidadActual, FechaRecepcion) VALUES
-- Compra 1 (hace 120 días)
(1, 1, 'C02FK1ABQ6L4', 'Usado - Excelente', 91, 'Apple M1',      8, 256, 'Gris espacial', 450, 0,  15, 650, 'Vendido',  1, 1, DATEADD(DAY,-105,GETDATE())), -- 1
(2, 1, 'DL7420-8H2K9', 'Usado - Bueno',     86, 'Intel i5-1145G7',16, 256, 'Negro',         280, 0,  15, 420, 'Vendido',  1, 1, DATEADD(DAY,-105,GETDATE())), -- 2
(3, 1, 'HP840-5CG12', 'Usado - Bueno',      80, 'Intel i5-10310U',16, 512, 'Plata',         260, 25, 15, 390, 'En Stock', 1, 1, DATEADD(DAY,-105,GETDATE())), -- 3 (inventario dormido)
(8, 1, NULL,           'Nuevo/Accesorio', NULL, NULL,           NULL, NULL, 'Negro',        160, 0,  15,  20, 'En Stock', 20, 14, DATEADD(DAY,-105,GETDATE())), -- 4 lote cargadores
(9, 1, NULL,           'Nuevo/Accesorio', NULL, NULL,           NULL, NULL, 'Negro',         80, 0,   0,  12, 'En Stock', 20, 15, DATEADD(DAY,-105,GETDATE())), -- 5 lote mouse
-- Compra 2 (hace 60 días)
(5, 2, 'F17HQ2ZXPLJM', 'Usado - Excelente', 89, NULL,            4, 128, 'Azul',          380, 0,  10, 560, 'Vendido',  1, 1, DATEADD(DAY,-46,GETDATE())),  -- 6
(6, 2, 'R5CT20ABCDE',  'Usado - Bueno',     84, NULL,            8, 128, 'Verde',         300, 0,  10, 450, 'En Stock', 1, 1, DATEADD(DAY,-46,GETDATE())),  -- 7
(4, 2, 'PF3K9LMN',     'Usado - Excelente', 93, 'Intel i7-1165G7',16, 512, 'Negro',         300, 0,  15, 480, 'Vendido',  1, 1, DATEADD(DAY,-46,GETDATE())),  -- 8
(7, 2, 'DMPGK2Q1Q1GC', 'Usado - Regular',   78, 'Apple A13',      3,  64, 'Plata',         220, 0,  10, 330, 'En Reparación', 1, 1, DATEADD(DAY,-46,GETDATE())), -- 9
(2, 2, 'DL7420-3J8M1', 'Usado - Bueno',     88, 'Intel i7-1185G7',16, 512, 'Negro',         300, 0,  10, 460, 'Por Revisar', 1, 1, DATEADD(DAY,-10,GETDATE())), -- 10
-- Compra 3 y 4 (en camino)
(1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 430, 0, 0, 0, 'En Transito', 1, 1, NULL), -- 11
(4, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 430, 0, 0, 0, 'En Transito', 1, 1, NULL), -- 12
(5, 4, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 380, 0, 0, 0, 'En Transito', 1, 1, NULL); -- 13

INSERT INTO Cliente (Cedula, NombreCompleto, TipoCliente, Telefono, CreationDate) VALUES
('001-150398-0012A', 'María José López',     'Final',      '8888-1234', DATEADD(DAY,-100,GETDATE())), -- 1
('401-220791-0004K', 'Carlos Andrés Ruiz',   'Revendedor', '8765-4321', DATEADD(DAY,-95,GETDATE())),  -- 2
('081-030500-1001B', 'Ana Sofía Martínez',   'Final',      '5712-9988', DATEADD(DAY,-40,GETDATE())),  -- 3
('001-111288-0033C', 'TecnoFix Granada',     'Tecnico',    '8456-7788', DATEADD(DAY,-30,GETDATE())),  -- 4
('121-050695-0007M', 'José Luis Pérez',      'Revendedor', '8123-4567', DATEADD(DAY,-20,GETDATE()));  -- 5

INSERT INTO Venta (ClienteID, FechaVenta, TotalVenta, MetodoPago, GarantiaMeses, NotasVenta, TipoVenta, CreatedBy) VALUES
(1, DATEADD(DAY,-95,GETDATE()), 650, 'Transferencia', 3, 'Incluye cargador original', 'Final',      'admin'), -- 1
(2, DATEADD(DAY,-80,GETDATE()), 410, 'Efectivo',      1, 'Precio revendedor',         'Revendedor', 'admin'), -- 2
(2, DATEADD(DAY,-70,GETDATE()),  80, 'Efectivo',      0, '4 cargadores USB-C',        'Revendedor', 'admin'), -- 3
(3, DATEADD(DAY,-35,GETDATE()), 560, 'Tarjeta',       3, NULL,                        'Final',      'admin'), -- 4
(5, DATEADD(DAY,-12,GETDATE()), 470, 'Transferencia', 3, 'Precio revendedor',         'Revendedor', 'admin'), -- 5
(4, DATEADD(DAY,-2,GETDATE()),   36, 'Efectivo',      0, '3 mouse inalámbricos',      'Tecnico',    'admin'), -- 6
(1, DATEADD(DAY,-1,GETDATE()),   40, 'Efectivo',      0, '2 cargadores USB-C',        'Final',      'admin'); -- 7

INSERT INTO DetalleVenta (VentaID, ItemID, ProductoID, PrecioUnitario, GarantiaMeses, Cantidad) VALUES
(1, 1, 1, 650, 3, 1),
(2, 2, 2, 410, 1, 1),
(3, 4, 8,  20, 0, 4),
(4, 6, 5, 560, 3, 1),
(5, 8, 4, 470, 3, 1),
(6, 5, 9,  12, 0, 3),
(7, 4, 8,  20, 0, 2);

INSERT INTO Garantias (VentaID, ProductoID, ItemID, FechaInicio, FechaFin, Estado, TipoGarantia) VALUES
(1, 1, 1, DATEADD(DAY,-95,GETDATE()), DATEADD(MONTH,3,DATEADD(DAY,-95,GETDATE())), 'Vencida', 'Tienda'),
(2, 2, 2, DATEADD(DAY,-80,GETDATE()), DATEADD(MONTH,1,DATEADD(DAY,-80,GETDATE())), 'Vencida', 'Tienda'),
(4, 5, 6, DATEADD(DAY,-35,GETDATE()), DATEADD(MONTH,3,DATEADD(DAY,-35,GETDATE())), 'Activa',  'Tienda'),
(5, 4, 8, DATEADD(DAY,-12,GETDATE()), DATEADD(MONTH,3,DATEADD(DAY,-12,GETDATE())), 'Activa',  'Tienda');

INSERT INTO Reparaciones (ItemID, FechaIngreso, ProblemaReportado, Estado) VALUES
(9, DATEADD(DAY,-5,GETDATE()), 'Pantalla con líneas verticales y batería al 78%', 'En Taller');
INSERT INTO Reparaciones (ItemID, FechaIngreso, FechaSalida, ProblemaReportado, TrabajoRealizado, CostoRepuestos, CostoManoObra, Estado) VALUES
(3, DATEADD(DAY,-100,GETDATE()), DATEADD(DAY,-98,GETDATE()), 'Teclado con teclas que no responden', 'Cambio de teclado', 18, 7, 'Finalizada');

INSERT INTO GastoOperativo (Descripcion, Monto, CategoriaGasto, FechaGasto) VALUES
('Publicidad en Facebook e Instagram', 40,  'Publicidad',  DATEADD(DAY,-90,GETDATE())),
('Internet del local',                 35,  'Servicios',   DATEADD(DAY,-85,GETDATE())),
('Transporte a Managua',               15,  'Transporte',  DATEADD(DAY,-62,GETDATE())),
('Internet del local',                 35,  'Servicios',   DATEADD(DAY,-55,GETDATE())),
('Publicidad en Facebook e Instagram', 50,  'Publicidad',  DATEADD(DAY,-30,GETDATE())),
('Internet del local',                 35,  'Servicios',   DATEADD(DAY,-25,GETDATE())),
('Bolsas y empaques',                  12,  'Insumos',     DATEADD(DAY,-8,GETDATE())),
('Transporte a Managua',               15,  'Transporte',  DATEADD(DAY,-3,GETDATE()));

INSERT INTO MovimientoCapital (Fecha, Monto, TipoMovimiento, Origen, Observacion) VALUES
(DATEADD(DAY,-125,GETDATE()), 3000, 'Aporte', 'Socios', 'Capital inicial'),
(DATEADD(DAY,-65,GETDATE()),  1000, 'Aporte', 'Socios', 'Capital para compra en Amazon');

INSERT INTO Retiro (Monto, Fecha, Motivo) VALUES (150, DATEADD(DAY,-40,GETDATE()), 'Retiro de utilidades');

INSERT INTO CuentasPorCobrar (Deudor, Monto, Descripcion, FechaPrestamo, Estado) VALUES
('Carlos Andrés Ruiz', 100, 'Saldo pendiente de la venta #2', DATEADD(DAY,-80,GETDATE()), 'Pendiente');

INSERT INTO RegistroPerdidas (ProductoID, UsuarioID, NumeroSerie, Cantidad, CostoTotalPerdida, Motivo, FechaRegistro) VALUES
(9, 1, NULL, 2, 8, 'Mouse dañados durante el envío', DATEADD(DAY,-100,GETDATE()));
GO

SET NOEXEC OFF;
GO
PRINT 'Proceso terminado. Revisa la pestaña Mensajes por si hubo errores.';
