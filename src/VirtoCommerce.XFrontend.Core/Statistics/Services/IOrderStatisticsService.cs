using System.Threading.Tasks;
using VirtoCommerce.XFrontend.Core.Statistics.Models;

namespace VirtoCommerce.XFrontend.Core.Statistics.Services;

public interface IOrderStatisticsService
{
    Task<OrderStatistics> GetAsync(OrderStatisticsCriteria criteria);
}
