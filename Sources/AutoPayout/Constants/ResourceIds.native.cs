namespace AutoPayout.Constants;
public class ResourceIds
{
    public const string Halftime = "01";

    public const string FullTime = "00";

    public const string OverTime = "OT";

    public static readonly string[] TimePattern = ["9090", "9089", "9134", "9136", "9137", "9131", "9127", "9129", "9121", "9123", "9126", "9058"];

    public static readonly string[] ResultHtBasketBall = { "Q1", "Q2" };

    public static readonly string[] ResultFtBasketBall = { "Q1", "Q2", "01", "Q3", "Q4", "OT" };

    public static readonly string[] ResultFtWithoutOTBasketBall = { "Q1", "Q2", "01", "Q3", "Q4" };

    public static readonly string[] ResultBasketBall = { "Q1", "Q2", "Q3", "Q4", "OT" };

}
