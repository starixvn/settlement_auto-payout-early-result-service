namespace AutoPayout.Conditions.Constants;

public static class ResultStatusId
{
    public const int Running = 1;

    public const int AutoCompleted = 2;

    public const int ManualCompleted = 5;
}

public static class EventStatus
{
    public const string Running = "running";
}

public static class SettlementStatusId
{
    public const int Calculated = 4;

    public const int Running = 0;
}