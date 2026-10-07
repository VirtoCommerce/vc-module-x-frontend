using FluentValidation;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Data.Layouts.Validation;

public class LayoutBlockValidator : AbstractValidator<LayoutBlock>
{
    public LayoutBlockValidator()
    {
        RuleFor(x => x.Id).NotEmpty().Matches(ModuleConstants.Layouts.IdPattern);
        RuleFor(x => x.Type).NotEmpty().Matches(ModuleConstants.Layouts.IdPattern);

        RuleFor(x => x.Settings)
            .Must(x => x.Count <= ModuleConstants.Layouts.MaxSettingsPerBlock)
            .WithMessage($"A block can have at most {ModuleConstants.Layouts.MaxSettingsPerBlock} settings.");

        RuleForEach(x => x.Settings).SetValidator(new LayoutSettingValidator());
    }
}
