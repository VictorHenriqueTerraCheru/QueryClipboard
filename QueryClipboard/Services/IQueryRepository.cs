using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QueryClipboard.Models;

namespace QueryClipboard.Services
{
    public interface IQueryRepository
    {
        Task<List<QueryItem>> GetAllQueriesAsync();
        Task<QueryItem?> GetQueryByIdAsync(Guid id);
        Task<List<QueryItem>> GetQueriesByCategoryAsync(string category);
        Task AddQueryAsync(QueryItem query);
        Task UpdateQueryAsync(QueryItem query);
        Task DeleteQueryAsync(Guid id);
        Task<List<QueryItem>> SearchQueriesAsync(string searchTerm);
        Task IncrementUsageAsync(Guid id);
    }
}
