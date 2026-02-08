using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QueryClipboard.Models;

namespace QueryClipboard.Services
{
    public class JsonQueryRepository : IQueryRepository
    {
        private readonly string _filePath;
        private List<QueryItem> _queries;

        public JsonQueryRepository()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QueryClipboard"
            );
            
            Directory.CreateDirectory(appDataPath);
            _filePath = Path.Combine(appDataPath, "queries.json");
            _queries = new List<QueryItem>();
            LoadQueries();
        }

        private void LoadQueries()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _queries = JsonConvert.DeserializeObject<List<QueryItem>>(json) ?? new List<QueryItem>();
                }
                else
                {
                    // Criar algumas queries de exemplo
                    _queries = GetSampleQueries();
                    SaveQueries();
                }
            }
            catch
            {
                _queries = new List<QueryItem>();
            }
        }

        private void SaveQueries()
        {
            var json = JsonConvert.SerializeObject(_queries, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public Task<List<QueryItem>> GetAllQueriesAsync()
        {
            return Task.FromResult(_queries.OrderByDescending(q => q.LastUsed).ToList());
        }

        public Task<QueryItem?> GetQueryByIdAsync(Guid id)
        {
            return Task.FromResult(_queries.FirstOrDefault(q => q.Id == id));
        }

        public Task<List<QueryItem>> GetQueriesByCategoryAsync(string category)
        {
            return Task.FromResult(_queries
                .Where(q => q.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(q => q.Name)
                .ToList());
        }

        public Task AddQueryAsync(QueryItem query)
        {
            query.Id = Guid.NewGuid();
            query.CreatedAt = DateTime.Now;
            query.LastUsed = DateTime.Now;
            _queries.Add(query);
            SaveQueries();
            return Task.CompletedTask;
        }

        public Task UpdateQueryAsync(QueryItem query)
        {
            var existing = _queries.FirstOrDefault(q => q.Id == query.Id);
            if (existing != null)
            {
                existing.Name = query.Name;
                existing.SqlQuery = query.SqlQuery;
                existing.Category = query.Category;
                existing.Description = query.Description;
                SaveQueries();
            }
            return Task.CompletedTask;
        }

        public Task DeleteQueryAsync(Guid id)
        {
            _queries.RemoveAll(q => q.Id == id);
            SaveQueries();
            return Task.CompletedTask;
        }

        public Task<List<QueryItem>> SearchQueriesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetAllQueriesAsync();

            var term = searchTerm.ToLower();
            return Task.FromResult(_queries
                .Where(q => 
                    q.Name.ToLower().Contains(term) ||
                    q.SqlQuery.ToLower().Contains(term) ||
                    q.Description.ToLower().Contains(term) ||
                    q.Category.ToLower().Contains(term))
                .OrderByDescending(q => q.LastUsed)
                .ToList());
        }

        public Task IncrementUsageAsync(Guid id)
        {
            var query = _queries.FirstOrDefault(q => q.Id == id);
            if (query != null)
            {
                query.UsageCount++;
                query.LastUsed = DateTime.Now;
                SaveQueries();
            }
            return Task.CompletedTask;
        }

        private List<QueryItem> GetSampleQueries()
        {
            return new List<QueryItem>
            {
                new QueryItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Buscar Usuários Ativos",
                    Category = "DBA",
                    Description = "Lista todos os usuários ativos no sistema",
                    SqlQuery = "SELECT Id, Nome, Email, DataCriacao\nFROM Usuarios\nWHERE Ativo = 1\nORDER BY DataCriacao DESC",
                    CreatedAt = DateTime.Now,
                    LastUsed = DateTime.Now,
                    UsageCount = 0
                },
                new QueryItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Top 10 Produtos Mais Vendidos",
                    Category = "Reports",
                    Description = "Produtos com maior volume de vendas no mês",
                    SqlQuery = "SELECT TOP 10\n    p.Nome,\n    COUNT(*) as TotalVendas,\n    SUM(v.Valor) as ValorTotal\nFROM Produtos p\nINNER JOIN Vendas v ON p.Id = v.ProdutoId\nWHERE v.DataVenda >= DATEADD(MONTH, -1, GETDATE())\nGROUP BY p.Nome\nORDER BY TotalVendas DESC",
                    CreatedAt = DateTime.Now,
                    LastUsed = DateTime.Now,
                    UsageCount = 0
                },
                new QueryItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Verificar Locks no Banco",
                    Category = "Dev",
                    Description = "Identifica processos com locks ativos",
                    SqlQuery = "SELECT \n    request_session_id as SPID,\n    DB_NAME(resource_database_id) as DatabaseName,\n    resource_type,\n    request_mode,\n    request_status\nFROM sys.dm_tran_locks\nWHERE resource_type <> 'DATABASE'",
                    CreatedAt = DateTime.Now,
                    LastUsed = DateTime.Now,
                    UsageCount = 0
                },
                new QueryItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Limpar Tabela Temporária",
                    Category = "Dev",
                    Description = "Remove registros antigos da tabela temp",
                    SqlQuery = "DELETE FROM TabelaTemporaria\nWHERE DataCriacao < DATEADD(DAY, -7, GETDATE())",
                    CreatedAt = DateTime.Now,
                    LastUsed = DateTime.Now,
                    UsageCount = 0
                }
            };
        }
    }
}
