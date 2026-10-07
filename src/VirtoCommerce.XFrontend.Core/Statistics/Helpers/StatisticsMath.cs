namespace VirtoCommerce.XFrontend.Core.Statistics.Helpers;

public static class StatisticsMath
{
    public static decimal? ChangePercent(decimal previous, decimal current)
    {
        return previous == 0m ? null : (current - previous) / previous * 100m;
    }
}
