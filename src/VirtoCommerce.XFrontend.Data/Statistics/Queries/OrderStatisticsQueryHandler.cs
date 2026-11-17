using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
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
        result.OrganizationIds = string.IsNullOrEmpty(request.OrganizationId) ? null : [request.OrganizationId];
        if (request.OrganizationScope)
        {
            result.CustomerIds = string.IsNullOrEmpty(request.MemberId)
                ? null
                : await GetMemberAccountIdsAsync(request.MemberId, request.OrganizationId);
        }
        else
        {
            result.CustomerId = request.CustomerId;
        }

        // An empty store id means all stores, like null: one scope, so one DataLoader key and one cache entry.
        result.StoreId = string.IsNullOrEmpty(request.StoreId) ? null : request.StoreId;
        result.CurrencyCode = await _currencyResolver.ResolveCurrencyCodeAsync(request.CurrencyCode, result.StoreId)
            ?? throw new ExecutionError(GetUnresolvedCurrencyMessage(request.CurrencyCode, result.StoreId));

        return result;
    }

    protected virtual async Task<IList<string>> GetMemberAccountIdsAsync(string memberId, string organizationId)
    {
        var member = await _memberService.GetByIdAsync(memberId, MemberResponseGroup.WithSecurityAccounts.ToString());

        // Same error as for an unknown id, so it reveals nothing about other organizations' members.
        if ((member as IHasOrganizations)?.Organizations?.Contains(organizationId, StringComparer.OrdinalIgnoreCase) != true)
        {
            throw AuthorizationError.Forbidden("The member does not belong to the current organization.");
        }

        return ((member as IHasSecurityAccounts)?.SecurityAccounts ?? [])
            .Select(x => x.Id)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            // One member, one cache key, whatever order the accounts come in.
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
