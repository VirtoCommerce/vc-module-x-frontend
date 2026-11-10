using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using VirtoCommerce.XFrontend.Core.Statistics.Models;
using VirtoCommerce.XFrontend.Core.Statistics.Queries;
using VirtoCommerce.XFrontend.Core.Statistics.Services;

namespace VirtoCommerce.XFrontend.Data.Statistics.Queries;

public class OrderStatisticsQueryHandler : IQueryHandler<OrderStatisticsQuery, OrderStatisticsContext>
{
    private readonly IStatisticsCurrencyResolver _currencyResolver;
    private readonly IMemberService _memberService;

    public OrderStatisticsQueryHandler(IStatisticsCurrencyResolver currencyResolver, IMemberService memberService)
    {
        _currencyResolver = currencyResolver;
        _memberService = memberService;
    }

    public virtual async Task<OrderStatisticsContext> Handle(OrderStatisticsQuery request, CancellationToken cancellationToken)
    {
        var result = AbstractTypeFactory<OrderStatisticsContext>.TryCreateInstance();
        if (request.OrganizationScope)
        {
            // The query builder has checked the permission and that there is a current organization.
            result.OrganizationIds = [request.OrganizationId];
            result.CustomerIds = string.IsNullOrEmpty(request.MemberId)
                ? null
                : await GetMemberAccountIdsAsync(request.MemberId, request.OrganizationId);
        }
        else
        {
            result.CustomerId = request.CustomerId;
            result.OrganizationIds = string.IsNullOrEmpty(request.OrganizationId) ? null : [request.OrganizationId];
        }

        // An empty store id means all stores, like null: one scope, so one DataLoader key and one cache entry.
        result.StoreId = string.IsNullOrEmpty(request.StoreId) ? null : request.StoreId;
        result.CurrencyCode = await _currencyResolver.ResolveCurrencyCodeAsync(request.CurrencyCode, result.StoreId)
            ?? throw new ExecutionError(GetUnresolvedCurrencyMessage(request.CurrencyCode, result.StoreId));

        return result;
    }

    /// <summary>
    /// The accounts whose orders are the member's, sorted so that one member is always one cache key. A member
    /// outside the organization is refused exactly like one that does not exist, so the answer tells nothing about
    /// other organizations' contacts.
    /// </summary>
    protected virtual async Task<IList<string>> GetMemberAccountIdsAsync(string memberId, string organizationId)
    {
        var contact = await _memberService.GetByIdAsync(memberId, MemberResponseGroup.WithSecurityAccounts.ToString()) as Contact;

        if (contact?.Organizations?.Contains(organizationId, StringComparer.OrdinalIgnoreCase) != true)
        {
            throw AuthorizationError.Forbidden("The member does not belong to the current organization.");
        }

        return (contact.SecurityAccounts ?? [])
            .Select(x => x.Id)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    protected virtual string GetUnresolvedCurrencyMessage(string currencyCode, string storeId)
    {
        if (!string.IsNullOrEmpty(currencyCode))
        {
            return $"Currency '{currencyCode}' is not configured.";
        }

        return string.IsNullOrEmpty(storeId)
            ? "No currency was requested and no primary currency is configured."
            : $"No currency was requested and no configured currency could be resolved for store '{storeId}'.";
    }
}
