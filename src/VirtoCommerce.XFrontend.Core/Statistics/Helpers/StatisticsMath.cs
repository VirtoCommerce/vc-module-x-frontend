namespace VirtoCommerce.XFrontend.Core.Statistics.Helpers;

public static class StatisticsMath
{
    private const decimal Percent = 100m;

    public static decimal? ChangePercent(decimal previous, decimal current)
    {
        return previous == 0m ? null : (current - previous) / previous * Percent;
    }
}
