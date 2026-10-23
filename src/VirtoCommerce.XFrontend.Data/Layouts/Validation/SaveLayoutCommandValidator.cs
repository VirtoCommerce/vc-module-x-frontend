using System;
using System.Linq;
using System.Text;
using FluentValidation;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Core.Layouts.Commands;

namespace VirtoCommerce.XFrontend.Data.Layouts.Validation;

public class SaveLayoutCommandValidator : AbstractValidator<SaveLayoutCommand>
{
    public SaveLayoutCommandValidator()
    {
        // No dots: the preference name joins scope and store id with dots, so "a.b" would read store "b" of scope "a".
        RuleFor(x => x.Scope).NotEmpty().Matches(ModuleConstants.Layouts.ScopePattern);

        RuleFor(x => x.StoreId).Matches(ModuleConstants.Layouts.StoreIdPattern).When(x => !string.IsNullOrEmpty(x.StoreId));

        RuleFor(x => x.Regions)
            .Must(x => x.Count <= ModuleConstants.Layouts.MaxRegions)
            .WithMessage($"A layout can have at most {ModuleConstants.Layouts.MaxRegions} regions.");

        RuleFor(x => x.Regions)
            .Must(x => Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(x)) <= ModuleConstants.Layouts.MaxLayoutSize)
            .WithMessage($"A layout must not exceed {ModuleConstants.Layouts.MaxLayoutSize} bytes as JSON.");

        // Across regions too: the storefront keeps one block per id.
        RuleFor(x => x.Regions)
            .Must(x => x.SelectMany(region => region.Blocks).GroupBy(block => block.Id, StringComparer.OrdinalIgnoreCase).All(group => group.Count() == 1))
            .WithMessage("A block id can appear only once in a layout.");

        RuleForEach(x => x.Regions).SetValidator(AbstractTypeFactory<LayoutRegionValidator>.TryCreateInstance());
    }
}
