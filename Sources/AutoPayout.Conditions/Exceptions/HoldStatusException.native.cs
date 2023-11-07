using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Exceptions;

public class HoldStatusException : ServiceException
{
    public HoldStatusException()
    {
        Description = ErrorMessage.MatchOnHold;
    }
}