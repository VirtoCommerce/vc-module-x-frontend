using System;
using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.XFrontend.Core.Statistics.Models;

public class OrderStatisticsCriteria : ValueObject
{
    /// <summary>One customer's own orders; empty for an organization's orders (<see cref="OrganizationIds"/>).</summary>
    public string CustomerId { get; set; }

    /// <summary>
    /// Narrows an organization's orders to the orders of these accounts (one member's); null for all of them. An
    /// empty list matches no order: a member without an account has placed none.
    /// </summary>
    public IList<string> CustomerIds { get; set; }

    public IList<string> OrganizationIds { get; set; }

    public string StoreId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string CurrencyCode { get; set; }
}
