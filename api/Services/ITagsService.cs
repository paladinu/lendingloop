using Api.Models;

namespace Api.Services;

public interface ITagsService
{
    Task<List<SystemTag>> GetAllActiveTagsAsync();
    Task<SystemTag?> GetTagByNameAsync(string name);
    Task InitializeDefaultTagsAsync();
    Task IncrementTagUsageAsync(string tagName);
    Task DecrementTagUsageAsync(string tagName);
    Task<Dictionary<string, int>> GetTagUsageStatisticsAsync();
}
