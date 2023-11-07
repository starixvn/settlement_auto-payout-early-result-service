using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Exceptions;

public class RunningOddsException : ServiceException
{
    public RunningOddsException()
    {
        Description = ErrorMessage.MatchRunningOdds;
    }
}