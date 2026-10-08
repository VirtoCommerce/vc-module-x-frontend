using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.XFrontend.Core.Statistics.Models;

namespace VirtoCommerce.XFrontend.Core.Statistics.Services;

public interface IOrderStatisticsService
{
    Task<OrderStatistics> GetAsync(OrderStatisticsCriteria criteria);

    Task<IDictionary<string, OrderStatistics>> GetByOrganizationAsync(OrderStatisticsCriteria criteria);
}
