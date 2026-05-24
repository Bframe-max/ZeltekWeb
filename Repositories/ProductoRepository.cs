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

        public async Task<IEnumerable<ProductoDto>> ObtenerTodosAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT p.ProductoID, p.Nombre, m.Nombre AS Marca,
                    ISNULL(c.Nombre, 'Sin categoría') AS Categoria,
                    p.EsUnico, p.Descripcion, p.Modelo, p.CostoReferencia, p.MarcaID, p.CategoriaID
                FROM Producto p
                INNER JOIN Marca m ON p.MarcaID = m.MarcaID
                LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                WHERE p.IsDeleted = 0
                ORDER BY p.Nombre";
            return await db.QueryAsync<ProductoDto>(sql);
        }

        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT p.ProductoID, p.Nombre, m.Nombre AS Marca,
                    ISNULL(c.Nombre, 'Sin categoría') AS Categoria,
                    p.EsUnico, p.Descripcion, p.Modelo, p.CostoReferencia, p.MarcaID, p.CategoriaID
                FROM Producto p
                INNER JOIN Marca m ON p.MarcaID = m.MarcaID
                LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                WHERE p.ProductoID = @ID AND p.IsDeleted = 0";
            return await db.QueryFirstOrDefaultAsync<ProductoDto>(sql, new { ID = id });
        }

        public async Task<int> CrearAsync(CreateProductoDto dto)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                INSERT INTO Producto (Nombre, Descripcion, MarcaID, Modelo, CategoriaID, EsUnico, CostoReferencia, IsDeleted)
                VALUES (@Nombre, @Descripcion, @MarcaID, @Modelo, @CategoriaID, @EsUnico, @Costo, 0);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await db.ExecuteScalarAsync<int>(sql, new
            {
                dto.Nombre, dto.Descripcion, dto.MarcaID, dto.Modelo,
                dto.CategoriaID, dto.EsUnico, Costo = dto.CostoReferencia
            });
        }

        public async Task ActualizarAsync(UpdateProductoDto dto)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                UPDATE Producto SET
                    Nombre = @Nombre, Descripcion = @Descripcion, MarcaID = @MarcaID,
                    Modelo = @Modelo, CategoriaID = @CategoriaID, EsUnico = @EsUnico, CostoReferencia = @Costo
                WHERE ProductoID = @ID AND IsDeleted = 0";
            await db.ExecuteAsync(sql, new
            {
                dto.Nombre, dto.Descripcion, dto.MarcaID, dto.Modelo,
                dto.CategoriaID, dto.EsUnico, Costo = dto.CostoReferencia, ID = dto.ProductoID
            });
        }

        public async Task EliminarAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            await db.ExecuteAsync("UPDATE Producto SET IsDeleted = 1 WHERE ProductoID = @ID", new { ID = id });
        }

        public async Task ReactivarAsync(int id)
        {
            using var db = _connectionFactory.CreateConnection();
            await db.ExecuteAsync("UPDATE Producto SET IsDeleted = 0 WHERE ProductoID = @ID", new { ID = id });
        }

        public async Task<IEnumerable<MarcaDto>> ObtenerMarcasAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            return await db.QueryAsync<MarcaDto>("SELECT MarcaID, Nombre FROM Marca ORDER BY Nombre");
        }

        public async Task<IEnumerable<CategoriaDto>> ObtenerCategoriasAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            return await db.QueryAsync<CategoriaDto>("SELECT CategoriaID, Nombre FROM Categoria ORDER BY Nombre");
        }

        public async Task<IEnumerable<ProductoDto>> ObtenerCargadoresYAccesoriosAsync()
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT p.ProductoID, p.Nombre
                FROM Producto p
                LEFT JOIN Categoria c ON p.CategoriaID = c.CategoriaID
                WHERE p.IsDeleted = 0 AND p.EsUnico = 0
                AND (c.Nombre LIKE '%Cargador%' OR c.Nombre LIKE '%Accesorio%')
                ORDER BY p.Nombre";
            return await db.QueryAsync<ProductoDto>(sql);
        }

        public async Task<bool> ExisteNombreAsync(string nombre, int? excludeId)
        {
            using var db = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT 1 FROM Producto
                WHERE LOWER(RTRIM(LTRIM(Nombre))) = LOWER(@Nombre)
                AND IsDeleted = 0
                AND (@ExcludeID IS NULL OR ProductoID != @ExcludeID)";
            return await db.ExecuteScalarAsync<int?>(sql, new { Nombre = nombre.Trim(), ExcludeID = excludeId }) != null;
        }

        public async Task<bool> ExisteMarcaAsync(string nombre)
        {
            using var db = _connectionFactory.CreateConnection();
            return await db.ExecuteScalarAsync<int?>(
                "SELECT 1 FROM Marca WHERE LOWER(RTRIM(LTRIM(Nombre))) = LOWER(@Nombre)",
                new { Nombre = nombre.Trim() }) != null;
        }

        public async Task<bool> ExisteCategoriaAsync(string nombre)
        {
            using var db = _connectionFactory.CreateConnection();
            return await db.ExecuteScalarAsync<int?>(
                "SELECT 1 FROM Categoria WHERE LOWER(RTRIM(LTRIM(Nombre))) = LOWER(@Nombre)",
                new { Nombre = nombre.Trim() }) != null;
        }

        public async Task<int> CrearMarcaRapidaAsync(string nombre)
        {
            using var db = _connectionFactory.CreateConnection();
            return await db.ExecuteScalarAsync<int>(
                "INSERT INTO Marca (Nombre) VALUES (@Nombre); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { Nombre = nombre.Trim() });
        }

        public async Task<int> CrearCategoriaRapidaAsync(string nombre)
        {
            using var db = _connectionFactory.CreateConnection();
            return await db.ExecuteScalarAsync<int>(
                "INSERT INTO Categoria (Nombre) VALUES (@Nombre); SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { Nombre = nombre.Trim() });
        }
    }
}
