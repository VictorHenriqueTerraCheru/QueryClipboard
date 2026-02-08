using Microsoft.Data.SqlClient;
using QueryClipboard.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QueryClipboard.Services
{
    public class SqlServerQueryRepository : IQueryRepository
    {
        private readonly string _connectionString;

        public SqlServerQueryRepository(string connectionString)
        {
            _connectionString = connectionString;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                var createTableSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Queries')
                    BEGIN
                        CREATE TABLE Queries (
                            Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                            Name NVARCHAR(200) NOT NULL,
                            SqlQuery NVARCHAR(MAX) NOT NULL,
                            Category NVARCHAR(100) NOT NULL,
                            Description NVARCHAR(500),
                            CreatedAt DATETIME2 DEFAULT GETDATE(),
                            LastUsed DATETIME2 DEFAULT GETDATE(),
                            UsageCount INT DEFAULT 0
                        );

                        -- Inserir queries de exemplo
                        INSERT INTO Queries (Name, Category, Description, SqlQuery)
                        VALUES 
                        ('Buscar Usuários Ativos', 'DBA', 'Lista todos os usuários ativos no sistema', 
                         'SELECT Id, Nome, Email, DataCriacao' + CHAR(13) + CHAR(10) + 
                         'FROM Usuarios' + CHAR(13) + CHAR(10) + 
                         'WHERE Ativo = 1' + CHAR(13) + CHAR(10) + 
                         'ORDER BY DataCriacao DESC'),
                        
                        ('Top 10 Produtos Mais Vendidos', 'Reports', 'Produtos com maior volume de vendas no mês',
                         'SELECT TOP 10' + CHAR(13) + CHAR(10) + 
                         '    p.Nome,' + CHAR(13) + CHAR(10) + 
                         '    COUNT(*) as TotalVendas,' + CHAR(13) + CHAR(10) + 
                         '    SUM(v.Valor) as ValorTotal' + CHAR(13) + CHAR(10) + 
                         'FROM Produtos p' + CHAR(13) + CHAR(10) + 
                         'INNER JOIN Vendas v ON p.Id = v.ProdutoId' + CHAR(13) + CHAR(10) + 
                         'WHERE v.DataVenda >= DATEADD(MONTH, -1, GETDATE())' + CHAR(13) + CHAR(10) + 
                         'GROUP BY p.Nome' + CHAR(13) + CHAR(10) + 
                         'ORDER BY TotalVendas DESC'),
                        
                        ('Verificar Locks no Banco', 'Dev', 'Identifica processos com locks ativos',
                         'SELECT ' + CHAR(13) + CHAR(10) + 
                         '    request_session_id as SPID,' + CHAR(13) + CHAR(10) + 
                         '    DB_NAME(resource_database_id) as DatabaseName,' + CHAR(13) + CHAR(10) + 
                         '    resource_type,' + CHAR(13) + CHAR(10) + 
                         '    request_mode,' + CHAR(13) + CHAR(10) + 
                         '    request_status' + CHAR(13) + CHAR(10) + 
                         'FROM sys.dm_tran_locks' + CHAR(13) + CHAR(10) + 
                         'WHERE resource_type <> ''DATABASE''');
                    END
                ";

                using var command = new SqlCommand(createTableSql, connection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao inicializar banco de dados: {ex.Message}", ex);
            }
        }

        public async Task<List<QueryItem>> GetAllQueriesAsync()
        {
            var queries = new List<QueryItem>();
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT * FROM Queries ORDER BY LastUsed DESC";
            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                queries.Add(MapQueryItem(reader));
            }

            return queries;
        }

        public async Task<QueryItem?> GetQueryByIdAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT * FROM Queries WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapQueryItem(reader);
            }

            return null;
        }

        public async Task<List<QueryItem>> GetQueriesByCategoryAsync(string category)
        {
            var queries = new List<QueryItem>();
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT * FROM Queries WHERE Category = @Category ORDER BY Name";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Category", category);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                queries.Add(MapQueryItem(reader));
            }

            return queries;
        }

        public async Task AddQueryAsync(QueryItem query)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO Queries (Id, Name, SqlQuery, Category, Description, CreatedAt, LastUsed, UsageCount)
                VALUES (@Id, @Name, @SqlQuery, @Category, @Description, @CreatedAt, @LastUsed, @UsageCount)
            ";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", query.Id == Guid.Empty ? Guid.NewGuid() : query.Id);
            command.Parameters.AddWithValue("@Name", query.Name);
            command.Parameters.AddWithValue("@SqlQuery", query.SqlQuery);
            command.Parameters.AddWithValue("@Category", query.Category);
            command.Parameters.AddWithValue("@Description", query.Description ?? string.Empty);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            command.Parameters.AddWithValue("@LastUsed", DateTime.Now);
            command.Parameters.AddWithValue("@UsageCount", 0);

            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateQueryAsync(QueryItem query)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                UPDATE Queries 
                SET Name = @Name,
                    SqlQuery = @SqlQuery,
                    Category = @Category,
                    Description = @Description
                WHERE Id = @Id
            ";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", query.Id);
            command.Parameters.AddWithValue("@Name", query.Name);
            command.Parameters.AddWithValue("@SqlQuery", query.SqlQuery);
            command.Parameters.AddWithValue("@Category", query.Category);
            command.Parameters.AddWithValue("@Description", query.Description ?? string.Empty);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteQueryAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "DELETE FROM Queries WHERE Id = @Id";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<QueryItem>> SearchQueriesAsync(string searchTerm)
        {
            var queries = new List<QueryItem>();
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT * FROM Queries 
                WHERE Name LIKE @Search 
                   OR SqlQuery LIKE @Search 
                   OR Description LIKE @Search
                   OR Category LIKE @Search
                ORDER BY LastUsed DESC
            ";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                queries.Add(MapQueryItem(reader));
            }

            return queries;
        }

        public async Task IncrementUsageAsync(Guid id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                UPDATE Queries 
                SET UsageCount = UsageCount + 1,
                    LastUsed = GETDATE()
                WHERE Id = @Id
            ";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);

            await command.ExecuteNonQueryAsync();
        }

        private QueryItem MapQueryItem(SqlDataReader reader)
        {
            return new QueryItem
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                SqlQuery = reader.GetString(reader.GetOrdinal("SqlQuery")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) 
                    ? string.Empty 
                    : reader.GetString(reader.GetOrdinal("Description")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastUsed = reader.GetDateTime(reader.GetOrdinal("LastUsed")),
                UsageCount = reader.GetInt32(reader.GetOrdinal("UsageCount"))
            };
        }
    }
}
