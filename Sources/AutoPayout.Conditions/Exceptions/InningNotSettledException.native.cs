using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Exceptions;

public class InningNotSettledException : ServiceException
{
    public InningNotSettledException()
    {
        Description = ErrorMessage.InningsNotSettled;
    }
}