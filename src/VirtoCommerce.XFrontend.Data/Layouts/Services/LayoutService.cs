using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.JsonConverters;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Core.Layouts.Models;
using VirtoCommerce.XFrontend.Core.Layouts.Services;

namespace VirtoCommerce.XFrontend.Data.Layouts.Services;

public class LayoutService : ILayoutService
{
    protected const string PreferenceName = "Layout";

    private static readonly JsonSerializerSettings _defaultSerializerSettings = new()
    {
        DateParseHandling = DateParseHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        Converters = { new PolymorphJsonConverter() },
    };

    private readonly ICustomerPreferenceService _customerPreferenceService;
    private readonly ICustomerPreferenceSearchService _customerPreferenceSearchService;
    private readonly ILogger<LayoutService> _logger;

    public LayoutService(
        ICustomerPreferenceService customerPreferenceService,
        ICustomerPreferenceSearchService customerPreferenceSearchService,
        ILogger<LayoutService> logger)
    {
        _customerPreferenceService = customerPreferenceService;
        _customerPreferenceSearchService = customerPreferenceSearchService;
        _logger = logger;
    }

    protected virtual JsonSerializerSettings SerializerSettings => _defaultSerializerSettings;

    public virtual async Task<Layout> GetLayoutAsync(string userId, string scope, string storeId)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(scope);

        var value = await _customerPreferenceService.GetValue(userId, BuildNameParts(scope, storeId));
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<Layout>(value, SerializerSettings);
        }
        catch (JsonException ex)
        {
            // The customer preferences API can store any value under this name. Read as none, the dashboard shows its
            // default and stays editable, and the user's next save replaces the value.
            _logger.LogWarning(ex, "Layout {Scope} of user {UserId} in store {StoreId} cannot be read and is ignored.", scope, userId, storeId);
            return null;
        }
    }

    public virtual async Task<Layout> SaveLayoutAsync(string userId, string scope, Layout layout, string storeId)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(scope);
        ArgumentNullException.ThrowIfNull(layout);

        var nameParts = BuildNameParts(scope, storeId);

        // Replacing a saved layout is always allowed; only a new one counts against the user's cap.
        if (string.IsNullOrEmpty(await _customerPreferenceService.GetValue(userId, nameParts)))
        {
            await EnsureRoomForNewLayoutAsync(userId);
        }

        layout.ModifiedDate = DateTime.UtcNow;

        var value = JsonConvert.SerializeObject(layout, SerializerSettings);
        await _customerPreferenceService.SaveValue(userId, nameParts, value);

        return layout;
    }

    protected virtual IList<string> BuildNameParts(string scope, string storeId)
    {
        List<string> parts = [PreferenceName, scope];

        if (!string.IsNullOrEmpty(storeId))
        {
            parts.Add(storeId);
        }

        return parts;
    }

    protected virtual async Task EnsureRoomForNewLayoutAsync(string userId)
    {
        var criteria = AbstractTypeFactory<CustomerPreferenceSearchCriteria>.TryCreateInstance();
        criteria.UserId = userId;

        var preferences = await _customerPreferenceSearchService.SearchAllNoCloneAsync(criteria);
        var layoutCount = preferences.Count(x => x.Name.StartsWithIgnoreCase($"{PreferenceName}."));

        if (layoutCount >= ModuleConstants.Layouts.MaxLayoutsPerUser)
        {
            throw new ValidationException([new ValidationFailure(nameof(Layout), $"A user can keep at most {ModuleConstants.Layouts.MaxLayoutsPerUser} layouts.")]);
        }
    }
}
