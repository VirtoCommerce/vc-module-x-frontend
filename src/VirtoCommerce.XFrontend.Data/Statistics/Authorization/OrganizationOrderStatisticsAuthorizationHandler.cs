using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Security.Authorization;
using VirtoCommerce.XFrontend.Core.Statistics.Queries;
using CustomerModuleConstants = VirtoCommerce.CustomerModule.Core.ModuleConstants;
using ProfileModuleConstants = VirtoCommerce.ProfileExperienceApiModule.Data.ModuleConstants;

namespace VirtoCommerce.XFrontend.Data.Statistics.Authorization;

public sealed class OrganizationOrderStatisticsAuthorizationRequirement : PermissionAuthorizationRequirement
{
    public OrganizationOrderStatisticsAuthorizationRequirement() : base(ProfileModuleConstants.Security.Permissions.MyOrganizationOrderView)
    {
    }
}

public class OrganizationOrderStatisticsAuthorizationHandler : PermissionAuthorizationHandlerBase<OrganizationOrderStatisticsAuthorizationRequirement>
{
    private readonly IMemberService _memberService;
    private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;

    public OrganizationOrderStatisticsAuthorizationHandler(
        IMemberService memberService,
        IOrganizationMembershipSearchService organizationMembershipSearchService)
    {
        _memberService = memberService;
        _organizationMembershipSearchService = organizationMembershipSearchService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OrganizationOrderStatisticsAuthorizationRequirement requirement)
    {
        await base.HandleRequirementAsync(context, requirement);

        if (context.PendingRequirements.Contains(requirement))
        {
            return;
        }

        // The token's organization and permissions outlive a removal from the organization or a locked membership.
        var result = context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator) ||
            context.Resource is OrderStatisticsQuery query && await IsCustomerOrganization(context, query.OrganizationId);

        if (!result)
        {
            context.Fail();
        }
    }

    protected virtual async Task<bool> IsCustomerOrganization(AuthorizationHandlerContext context, string organizationId)
    {
        var memberId = GetMemberId(context);
        if (string.IsNullOrEmpty(organizationId) || string.IsNullOrEmpty(memberId))
        {
            return false;
        }

        var member = await _memberService.GetByIdAsync(memberId);
        if (!MemberAssignedToOrganization(member, organizationId))
        {
            return false;
        }

        var userId = GetCurrentUserId(context);
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var membership = await _organizationMembershipSearchService.GetMembershipAsync(userId, organizationId);
        if (membership?.IsCurrentlyLocked == true)
        {
            return false;
        }

        var effectiveStatus = OrganizationMembership.ResolveEffectiveStatus(membership?.Status, member.Status);
        return !CustomerModuleConstants.MembershipStatuses.IsBlocking(effectiveStatus);
    }

    private static string GetCurrentUserId(AuthorizationHandlerContext context)
    {
        return context.User.GetUserId();
    }

    private static string GetMemberId(AuthorizationHandlerContext context)
    {
        return context.User.FindFirstValue(PlatformConstants.Security.Claims.MemberIdClaimType);
    }

    private static bool MemberAssignedToOrganization(Member member, string organizationId)
    {
        return (member as IHasOrganizations)?.Organizations?.Contains(organizationId, StringComparer.OrdinalIgnoreCase) == true;
    }
}
