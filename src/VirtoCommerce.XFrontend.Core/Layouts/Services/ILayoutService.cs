using System.Threading.Tasks;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Core.Layouts.Services;

public interface ILayoutService
{
    Task<Layout> GetLayoutAsync(string userId, string scope, string storeId);

    Task<Layout> SaveLayoutAsync(string userId, string scope, Layout layout, string storeId);
}
