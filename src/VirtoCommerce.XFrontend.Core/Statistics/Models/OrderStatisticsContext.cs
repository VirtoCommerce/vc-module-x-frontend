using System.Collections.Generic;

namespace VirtoCommerce.XFrontend.Core.Statistics.Models;

public class OrderStatisticsContext
{
    public string CustomerId { get; set; }

    public IList<string> OrganizationIds { get; set; }

    public string StoreId { get; set; }

    public string CurrencyCode { get; set; }
}
