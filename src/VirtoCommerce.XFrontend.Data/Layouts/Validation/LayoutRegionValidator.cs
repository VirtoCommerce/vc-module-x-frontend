using FluentValidation;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Data.Layouts.Validation;

public class LayoutRegionValidator : AbstractValidator<LayoutRegion>
{
    public LayoutRegionValidator()
    {
        RuleFor(x => x.Id).NotEmpty().Matches(ModuleConstants.Layouts.IdPattern);

        RuleFor(x => x.Blocks)
            .Must(x => x.Count <= ModuleConstants.Layouts.MaxBlocksPerRegion)
            .WithMessage($"A region can have at most {ModuleConstants.Layouts.MaxBlocksPerRegion} blocks.");

        RuleForEach(x => x.Blocks).SetValidator(new LayoutBlockValidator());
    }
}
