using System;
using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.XFrontend.Core.Statistics.Models;

public class OrderStatisticsCriteria : ValueObject
{
    public string CustomerId { get; set; }

    public IList<string> OrganizationIds { get; set; }

    public string StoreId { get; set; }

    public IList<string> Statuses { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string CurrencyCode { get; set; }
}
