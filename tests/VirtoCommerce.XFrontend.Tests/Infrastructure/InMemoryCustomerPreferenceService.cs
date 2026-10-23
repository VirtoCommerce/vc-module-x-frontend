using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;

namespace VirtoCommerce.XFrontend.Tests.Infrastructure;

/// <summary>
/// In-memory customer preferences keyed by (user id, preference name), with the Customer module's search over them.
/// Names are built from their parts exactly as the Customer module's <c>CustomerPreferenceService.GetName</c> does
/// (joined with dots), so tests can assert the stored name and see which user owns which row. The search filters by
/// user id and exact name and pages like the real one, so <c>SearchAllAsync</c> works over it.
/// </summary>
public sealed class InMemoryCustomerPreferenceService : ICustomerPreferenceService, ICustomerPreferenceSearchService
{
    public ConcurrentDictionary<(string UserId, string Name), string> Values { get; } = new();

    public Task<string> GetValue(string userId, IList<string> nameParts)
    {
        return GetValue(userId, string.Join(".", nameParts));
    }

    public Task<string> GetValue(string userId, string name)
    {
        return Task.FromResult(Values.GetValueOrDefault((userId, name)));
    }

    public Task SaveValue(string userId, IList<string> nameParts, string value)
    {
        return SaveValue(userId, string.Join(".", nameParts), value);
    }

    public Task SaveValue(string userId, string name, string value)
    {
        Values[(userId, name)] = value;

        return Task.CompletedTask;
    }

    public Task<CustomerPreferenceSearchResult> SearchAsync(CustomerPreferenceSearchCriteria criteria, bool clone = true)
    {
        var preferences = Values
            .Where(x => (criteria.UserId == null || x.Key.UserId == criteria.UserId) && (criteria.Name == null || x.Key.Name == criteria.Name))
            .OrderBy(x => x.Key.UserId)
            .ThenBy(x => x.Key.Name)
            .Select(x => new CustomerPreference { Id = $"{x.Key.UserId}|{x.Key.Name}", UserId = x.Key.UserId, Name = x.Key.Name, Value = x.Value })
            .ToList();

        var result = new CustomerPreferenceSearchResult
        {
            TotalCount = preferences.Count,
            Results = preferences.Skip(criteria.Skip).Take(criteria.Take).ToList(),
        };

        return Task.FromResult(result);
    }
}
