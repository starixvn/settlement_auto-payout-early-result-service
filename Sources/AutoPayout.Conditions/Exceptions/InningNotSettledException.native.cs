using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Exceptions;

internal class InningNotSettledException : ServiceException
{
    public InningNotSettledException()
    {
        Description = ErrorMessage.InningsNotSettled;
    }
}