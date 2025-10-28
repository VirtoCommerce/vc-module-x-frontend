using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.WhiteLabeling.ExperienceApi.Models;
using VirtoCommerce.WhiteLabeling.ExperienceApi.Queries;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Queries;
using VirtoCommerce.XFrontend.Core.Models;
using VirtoCommerce.XFrontend.Core.Queries;

namespace VirtoCommerce.XFrontend.Data.Queries;

public class PageContextQueryHandler : IQueryHandler<PageContextQuery, PageContexResponse>
{
    private readonly IMediator _mediator;
    private readonly IModuleCatalog _moduleCatalog;
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

    public PageContextQueryHandler(IMediator mediator, IModuleCatalog moduleCatalog, Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        _mediator = mediator;
        _moduleCatalog = moduleCatalog;
        _userManagerFactory = userManagerFactory;
    }

    public async Task<PageContexResponse> Handle(PageContextQuery request, CancellationToken cancellationToken)
    {
        var userTask = GetUserAsync(request);
        var storeTask = GetStoreAsync(request);

        await Task.WhenAll(userTask, storeTask);

        var user = userTask.Result;
        var storeResponse = storeTask.Result;

        var slugInfoResponseTask = GetSlugInfoAsync(request, storeResponse.StoreId, user.Id);
        var tasks = new List<Task> { slugInfoResponseTask };

        Task<ExpWhiteLabelingSetting> whiteLabelingSettingTask = null;
        if (IsWhiteLabelingModuleInstalled())
        {
            whiteLabelingSettingTask = GetWhiteLabelingSettingAsync(request, storeResponse.StoreId, user.Id);
            tasks.Add(whiteLabelingSettingTask);
        }

        await Task.WhenAll(tasks);

        var result = new PageContexResponse
        {
            User = user,
            StoreResponse = storeResponse,
            SlugInfoResponse = slugInfoResponseTask.Result,
            WhiteLabelingSetting = whiteLabelingSettingTask?.Result,
        };

        return result;
    }

    protected virtual async Task<ApplicationUser> GetUserAsync(PageContextQuery request)
    {
        ApplicationUser result;

        if (request.IsAnonymous)
        {
            result = AnonymousUser.Instance;

            if (!request.UserId.IsNullOrEmpty() && !await UserExistsAsync(request.UserId))
            {
                result.Id = request.UserId;
            }
        }
        else
        {
            result = await _mediator.Send(new GetUserQuery
            {
                UserName = request.UserName,
            });
        }

        return result;
    }

    protected virtual Task<StoreResponse> GetStoreAsync(PageContextQuery request) => _mediator.Send(new GetStoreQuery
    {
        CultureName = request.CultureName,
        Domain = request.Domain,
        StoreId = request.StoreId,
    });

    protected virtual Task<SlugInfoResponse> GetSlugInfoAsync(PageContextQuery request, string storeId, string userId) => _mediator.Send(new SlugInfoQuery
    {
        Permalink = request.Permalink,
        CultureName = request.CultureName,
        OrganizationId = request.OrganizationId,
        UserId = userId,
        StoreId = storeId,
    });

    protected virtual Task<ExpWhiteLabelingSetting> GetWhiteLabelingSettingAsync(PageContextQuery request, string storeId, string userId) => _mediator.Send(new GetWhiteLabelingSettingsQuery
    {
        CultureName = request.CultureName,
        OrganizationId = request.OrganizationId,
        UserId = userId,
        StoreId = storeId,
    });

    private async Task<bool> UserExistsAsync(string userId)
    {
        var userManager = _userManagerFactory();
        var user = await userManager.FindByIdAsync(userId);
        return user != null;
    }

    /// <summary>
    /// Checks if WhiteLabeling Module is installed.
    /// </summary>
    /// <returns></returns>
    private bool IsWhiteLabelingModuleInstalled()
    {
        return _moduleCatalog.Modules.Any(m => m.ModuleName == "VirtoCommerce.WhiteLabeling");
    }
}
