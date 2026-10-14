using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.XFrontend.Core.Statistics.Models;

namespace VirtoCommerce.XFrontend.Core.Statistics.Schemas;

public class InputStatisticsPeriodType : ExtendableInputObjectGraphType<StatisticsPeriod>
{
    public InputStatisticsPeriodType()
    {
        Name = "InputStatisticsPeriod";

        Field(x => x.From, nullable: true).Description("Inclusive lower bound of the order creation date (no lower bound when omitted).");
        Field(x => x.To, nullable: true).Description("Inclusive upper bound of the order creation date (no upper bound when omitted).");
    }
}
