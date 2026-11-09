using Domain.Model;

namespace DocumentManagementSystem.Services;

public interface IDocumentSearchService
{
    Task<IReadOnlyList<SearchDocument>> SearchAsync(string query, CancellationToken cancellationToken);
    Task DeleteAsync(int documentId, CancellationToken cancellationToken);
}
