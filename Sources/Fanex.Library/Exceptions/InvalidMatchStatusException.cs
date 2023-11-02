using Fanex.AutoPayoutSettlement.Core.Exceptions;
using Fanex.Library.Constants;

namespace Fanex.Library.Exceptions;

public class InvalidMatchStatusException : ServiceException
{
    public InvalidMatchStatusException()
    {
        Description = ErrorMessage.InvalidStatus;
    }
}