using System.Text;
using FluentValidation;
using Newtonsoft.Json;
using VirtoCommerce.XFrontend.Core;
using VirtoCommerce.XFrontend.Core.Layouts.Models;

namespace VirtoCommerce.XFrontend.Data.Layouts.Validation;

public class LayoutSettingValidator : AbstractValidator<LayoutSetting>
{
    public LayoutSettingValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(ModuleConstants.Layouts.MaxSettingKeyLength);

        RuleFor(x => x.Value)
            .Must(x => Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(x)) <= ModuleConstants.Layouts.MaxSettingValueSize)
            .WithMessage($"A setting value must not exceed {ModuleConstants.Layouts.MaxSettingValueSize} bytes as JSON.");
    }
}
