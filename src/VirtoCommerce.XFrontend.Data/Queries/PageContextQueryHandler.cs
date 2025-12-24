using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CoreModule.Core.Common;
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

public class PageContextQueryHandler : IQueryHandler<PageContextQuery, PageContextResponse>
{
    private readonly IMediator _mediator;
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

    [Obsolete("Use PageContextQueryHandler(IMediator mediator, Func<UserManager<ApplicationUser>> userManagerFactory) constructor", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/platform/user-guide/versions/virto3-products-versions/")]
    private PageContextQueryHandler(IMediator mediator, IModuleCatalog moduleCatalog, Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        _mediator = mediator;
        _userManagerFactory = userManagerFactory;
    }

    public PageContextQueryHandler(IMediator mediator, Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        _mediator = mediator;
        _userManagerFactory = userManagerFactory;
    }

    public async Task<PageContextResponse> Handle(PageContextQuery request, CancellationToken cancellationToken)
    {
        var userTask = GetUserAsync(request);
        var storeTask = GetStoreAsync(request);

        await Task.WhenAll(userTask, storeTask);

        var user = userTask.Result;
        var storeResponse = storeTask.Result;

        var cultureName = GetCultureName(request.CultureName, storeResponse.DefaultLanguage?.CultureName, storeResponse.AvailableLanguages);

        var result = new PageContextResponse
        {
            User = user,
            StoreResponse = storeResponse,
            CultureName = cultureName,
            OrganizationId = request.OrganizationId,
            SlugInfoResponse = await GetSlugInfoAsync(request, storeResponse.StoreId, user.Id, cultureName)
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

    protected virtual Task<SlugInfoResponse> GetSlugInfoAsync(PageContextQuery request, string storeId, string userId, string cultureName) => _mediator.Send(new SlugInfoQuery
    {
        Permalink = request.Permalink,
        CultureName = cultureName,
        OrganizationId = request.OrganizationId,
        UserId = userId,
        StoreId = storeId,
    });

    [Obsolete("Not being called anymore. White labeling initialization moved to White labeling module.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/platform/user-guide/versions/virto3-products-versions/")]
    protected virtual Task<ExpWhiteLabelingSetting> GetWhiteLabelingSettingAsync(PageContextQuery request, string storeId, string userId, string cultureName) => _mediator.Send(new GetWhiteLabelingSettingsQuery
    {
        CultureName = cultureName,
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

    private static string GetCultureName(string cultureName, string defaultCultureName, IList<Language> availableLanguages)
    {
        if (cultureName.IsNullOrEmpty())
        {
            cultureName = defaultCultureName;
        }
        else if (cultureName.Length == 2)
        {
            cultureName = availableLanguages.FirstOrDefault(x => cultureName == x.TwoLetterLanguageName)?.CultureName;
            cultureName ??= defaultCultureName;
        }

        return cultureName;
    }
}
