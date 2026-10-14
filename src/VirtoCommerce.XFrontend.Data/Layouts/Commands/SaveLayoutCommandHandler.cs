using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XFrontend.Core.Layouts.Commands;
using VirtoCommerce.XFrontend.Core.Layouts.Models;
using VirtoCommerce.XFrontend.Core.Layouts.Services;

namespace VirtoCommerce.XFrontend.Data.Layouts.Commands;

public class SaveLayoutCommandHandler : IRequestHandler<SaveLayoutCommand, Layout>
{
    private readonly ILayoutService _layoutService;
    private readonly AbstractValidator<SaveLayoutCommand> _validator;
    private readonly IStoreService _storeService;

    public SaveLayoutCommandHandler(ILayoutService layoutService, AbstractValidator<SaveLayoutCommand> validator, IStoreService storeService)
    {
        _layoutService = layoutService;
        _validator = validator;
        _storeService = storeService;
    }

    public virtual async Task<Layout> Handle(SaveLayoutCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureStoreExistsAsync(request.StoreId);

        var layout = AbstractTypeFactory<Layout>.TryCreateInstance();
        layout.SchemaVersion = request.SchemaVersion;
        layout.Regions = request.Regions;

        return await _layoutService.SaveLayoutAsync(request.UserId, request.Scope, layout, request.StoreId);
    }

    protected virtual async Task EnsureStoreExistsAsync(string storeId)
    {
        // Each store id is a separate preference row: only real stores may get one.
        if (!string.IsNullOrEmpty(storeId) && await _storeService.GetNoCloneAsync(storeId) == null)
        {
            throw new ValidationException([new ValidationFailure(nameof(SaveLayoutCommand.StoreId), $"Store '{storeId}' does not exist.")]);
        }
    }
}
