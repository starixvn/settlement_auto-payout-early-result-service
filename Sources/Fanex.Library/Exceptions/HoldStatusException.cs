using Fanex.AutoPayoutSettlement.Core.Exceptions;
using Fanex.Library.Constants;

namespace Fanex.Library.Exceptions;

public class HoldStatusException : ServiceException
{
    public HoldStatusException()
    {
        Description = ErrorMessage.MatchOnHold;
    }
}