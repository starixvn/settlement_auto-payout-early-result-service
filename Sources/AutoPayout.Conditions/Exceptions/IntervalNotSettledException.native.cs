using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Exceptions;

public class IntervalNotSettledException : ServiceException
{
    public IntervalNotSettledException()
    {
        Description = ErrorMessage.IntervalNotSettled;
    }
}