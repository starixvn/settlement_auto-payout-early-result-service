namespace AutoPayout.Conditions.Constants;

public static class ErrorCode
{
    public const int MatchInQueue = 1_001_003;

    public const int DeploymentError = 99;
}

public static class ErrorMessage
{
    public const string MatchInQueue =
        "Failed Auto Payout: This match is being calculated, please wait a few seconds until the on-going process is completed and try again.";

    public const string InconsistentScore = "Failed Auto Payout: Result({0}) values inconsistent between ";

    public const string MatchNotValid = "Failed Auto Payout: Match and Sport are not supported";

    public const string ResultExpired = "Failed Auto Payout: Result({0}) insufficient Trader or Provider sources.";

    public const string MatchOnHold = "Failed Auto Payout: Cannot execute auto-settle process due to match is on hold.";

    public const string InvalidStatus = "Failed Auto Payout: FT has been calculated already.";

    public const string MatchRunningOdds = "Failed Auto Payout: ERROR! Cannot calculate due to unclosed odds. Please make sure you select correct match/interval/result to calculate.";

    public const string IntervalNotSettled = "Failed Auto Payout: Result(00) corresponding Innings/Intervals has not been calculated.";

    public const string FinalInconsistent = "Failed Auto Payout: FT(@score) is inconsistent with interval scores.";

    public const string FinalInconsistentWithGameSession = "Failed Auto Payout: FT(@score) is inconsistent with game session information.";

    public const string FinalESportsInconsistent = "Failed Auto Payout: FT(@score) is inconsistent with Map information.";
    public const string NotTrader = "Failed: Result({0}) insufficient Trader source for second values.";
    public const string HalfTimeInconsistentWithGameSession = "Failed Auto Payout: HT({0}) due to invalid match type: GameSession({1}).";

    public const string RelatedResultInconsistent = "Failed Auto Payout: {0} due to values inconsistency: {1}";

    public const string RelatedResultNotCalculated = "Failed Auto Payout: {0} due to related result(s) not calculated: {1}";

    public const string FullTimeDraw = "Failed Auto Payout: {0} due to FT should not be tie.";

}