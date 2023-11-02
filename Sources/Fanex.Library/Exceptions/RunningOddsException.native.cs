using Fanex.AutoPayoutSettlement.Core.Exceptions;
using Fanex.Library.Constants;

namespace Fanex.Library.Exceptions;

public class RunningOddsException : ServiceException
{
    public RunningOddsException()
    {
        Description = ErrorMessage.MatchRunningOdds;
    }
}