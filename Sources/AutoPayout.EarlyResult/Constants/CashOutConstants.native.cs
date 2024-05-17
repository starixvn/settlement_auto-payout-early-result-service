namespace AutoPayout.EarlyResult.Constants;
public static class CashOutConstants
{

    public static string[] ResourceOrder = ["00", "Q1", "Q2", "Q3", "Q4", "01"];

    public static string[] ResourceWithOutFinalOrder = ["Q1", "Q2", "Q3", "Q4", "OT"];

    public static Dictionary<string, int> QuarterListHalf = new()
    {
        { "00", 0 },
        { "Q1", 1 },
        { "Q2", 2 },
        { "Q3", 3 },
        { "Q4", 4 },
        { "01", 5 },
    };

}
