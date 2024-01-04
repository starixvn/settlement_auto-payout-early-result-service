namespace AutoPayout.Conditions.Constants;

public static class ErrorCode
{
    public const int MatchInQueue = 1_001_003;

    public const int DeploymentError = 99;
}

public static class ErrorMessage
{
    public const string MatchInQueue =
        "Failed: This match is being calculated, please wait a few seconds until the on-going process is completed and try again.";

    public const string InconsistentScore = "Failed: Result({0}) values inconsistent between ";

    public const string MatchNotValid = "Failed: Match and sport type not supported";

    public const string ResultExpired = "Failed: Result({0}) insufficient Trader or Provider sources.";

    public const string MatchOnHold = "Failed: Cannot execute auto-settle process due to match is on hold.";

    public const string InvalidStatus = "Failed: FT has been calculated already.";

    public const string MatchRunningOdds = "Failed: ERROR! Cannot calculate due to unclosed odds. Please make sure you select correct match/interval/result to calculate.";

    public const string IntervalNotSettled = "Failed: Result(00) corresponding Innings/Intervals has not been calculated.";

    public const string FinalInconsistent = "Failed: FT(@score) is inconsistent with interval scores";
}