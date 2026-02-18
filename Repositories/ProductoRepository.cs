using Dapper;
using ZeltekWeb.Data;
using ZeltekWeb.Models.DTOs;
using ZeltekWeb.Repositories.Interfaces;

namespace ZeltekWeb.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProductoRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerTodosAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT 
                    p.ProductoID,
                    p.Nombre,
                    m.Nombre AS Marca,
                    ISNULL(c.Nombre, 'Sin categoría') AS Categoria,
                    p.EsUnico,
                    p.Descripcion,
                    p.Modelo,
                    p.CostoReferencia,
                    p.MarcaID,
                    p.CategoriaID
                FROM Producto p
                INNER JOIN Marca m ON p.MarcaID = m.MarcaID
                LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                WHERE p.IsDeleted = 0 
                AND p.TenantID = @TenantId
                ORDER BY p.Nombre";
                
            return await db.QueryAsync<ProductoDto>(sql, new { TenantId = tenantId });
        }

        public async Task<ProductoDto?> ObtenerPorIdAsync(int id, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT 
                    p.ProductoID,
                    p.Nombre,
                    m.Nombre AS Marca,
                    ISNULL(c.Nombre, 'Sin categoría') AS Categoria,
                    p.EsUnico,
                    p.Descripcion,
                    p.Modelo,
                    p.CostoReferencia,
                    p.MarcaID,
                    p.CategoriaID
                FROM Producto p
                INNER JOIN Marca m ON p.MarcaID = m.MarcaID
                LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                WHERE p.ProductoID = @ID 
                AND p.IsDeleted = 0 
                AND p.TenantID = @TenantId";

            return await db.QueryFirstOrDefaultAsync<ProductoDto>(sql, new { ID = id, TenantId = tenantId });
        }

        public async Task<int> CrearAsync(CreateProductoDto dto, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                INSERT INTO Producto
                (Nombre, Descripcion, MarcaID, Modelo, CategoriaID, EsUnico, CostoReferencia, TenantID, IsDeleted)
                VALUES
                (@Nombre, @Descripcion, @MarcaID, @Modelo, @CategoriaID, @EsUnico, @Costo, @TenantId, 0);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await db.ExecuteScalarAsync<int>(sql, new
            {
                dto.Nombre,
                dto.Descripcion,
                dto.MarcaID,
                dto.Modelo,
                dto.CategoriaID,
                dto.EsUnico,
                Costo = dto.CostoReferencia,
                TenantId = tenantId
            });
        }

        public async Task ActualizarAsync(UpdateProductoDto dto, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                UPDATE Producto SET
                    Nombre = @Nombre,
                    Descripcion = @Descripcion,
                    MarcaID = @MarcaID,
                    Modelo = @Modelo,
                    CategoriaID = @CategoriaID,
                    EsUnico = @EsUnico,
                    CostoReferencia = @Costo
                WHERE ProductoID = @ID 
                AND TenantID = @TenantId
                AND IsDeleted = 0";

            await db.ExecuteAsync(sql, new
            {
                dto.Nombre,
                dto.Descripcion,
                dto.MarcaID,
                dto.Modelo,
                dto.CategoriaID,
                dto.EsUnico,
                Costo = dto.CostoReferencia,
                ID = dto.ProductoID,
                TenantId = tenantId
            });
        }

        public async Task EliminarAsync(int id, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = "UPDATE Producto SET IsDeleted = 1 WHERE ProductoID = @ID AND TenantID = @TenantId";
            await db.ExecuteAsync(sql, new { ID = id, TenantId = tenantId });
        }

        public async Task ReactivarAsync(int id, int tenantId)
        {
             using var db = _connectionFactory.CreateConnection();
             string sql = "UPDATE Producto SET IsDeleted = 0 WHERE ProductoID = @ID AND TenantID = @TenantId";
             await db.ExecuteAsync(sql, new { ID = id, TenantId = tenantId });
        }

        public async Task<IEnumerable<MarcaDto>> ObtenerMarcasAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            // Assuming Marcas are also tenant specific now, or explicit shared ones? 
            // For now, strict tenant filtering.
            string sql = "SELECT MarcaID, Nombre FROM Marca WHERE TenantID = @TenantId ORDER BY Nombre";
            return await db.QueryAsync<MarcaDto>(sql, new { TenantId = tenantId });
        }

        public async Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = "SELECT CategoriaID, Nombre FROM Categoria WHERE TenantID = @TenantId ORDER BY Nombre";
            return await db.QueryAsync<CategoriaDto>(sql, new { TenantId = tenantId });
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerCargadoresYAccesoriosAsync(int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT 
                    p.ProductoID, p.Nombre
                FROM Producto p
                LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                WHERE p.IsDeleted = 0 
                AND p.EsUnico = 0
                AND p.TenantID = @TenantId
                AND (c.Nombre LIKE '%Cargador%' OR c.Nombre LIKE '%Accesorio%')
                ORDER BY p.Nombre";
            return await db.QueryAsync<ProductoDto>(sql, new { TenantId = tenantId });
        }

        public async Task<bool> ExisteNombreAsync(string nombre, int? excludeId, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT 1 FROM Producto 
                WHERE LOWER(RTRIM(LTRIM(Nombre))) = LOWER(@Nombre) 
                AND IsDeleted = 0
                AND (@ExcludeID IS NULL OR ProductoID != @ExcludeID)
                AND TenantID = @TenantId";
                
            return await db.ExecuteScalarAsync<int?>(sql, new { Nombre = nombre.Trim(), ExcludeID = excludeId, TenantId = tenantId }) != null;
        }

        public async Task<bool> ExisteMarcaAsync(string nombre, int tenantId)
        {
             using var db = _connectionFactory.CreateConnection();
             string sql = "SELECT 1 FROM Marca WHERE LOWER(RTRIM(LTRIM(Nombre))) = LOWER(@Nombre) AND TenantID = @TenantId";
             return await db.ExecuteScalarAsync<int?>(sql, new { Nombre = nombre.Trim(), TenantId = tenantId }) != null;
        }

        public async Task<bool> ExisteCategoriaAsync(string nombre, int tenantId)
        {
             using var db = _connectionFactory.CreateConnection();
             string sql = "SELECT 1 FROM Categoria WHERE LOWER(RTRIM(LTRIM(Nombre))) = LOWER(@Nombre) AND TenantID = @TenantId";
             return await db.ExecuteScalarAsync<int?>(sql, new { Nombre = nombre.Trim(), TenantId = tenantId }) != null;
        }

        public async Task<int> CrearMarcaRapidaAsync(string nombre, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = "INSERT INTO Marca (Nombre, TenantID) VALUES (@Nombre, @TenantId); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await db.ExecuteScalarAsync<int>(sql, new { Nombre = nombre.Trim(), TenantId = tenantId });
        }

        public async Task<int> CrearCategoriaRapidaAsync(string nombre, int tenantId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = "INSERT INTO Categoria (Nombre, TenantID) VALUES (@Nombre, @TenantId); SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await db.ExecuteScalarAsync<int>(sql, new { Nombre = nombre.Trim(), TenantId = tenantId });
        }
    }
}
