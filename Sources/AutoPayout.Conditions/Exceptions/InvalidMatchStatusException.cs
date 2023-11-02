using AutoPayout.Conditions.Constants;
using AutoPayout.Exceptions;

namespace AutoPayout.Conditions.Exceptions;

public class InvalidMatchStatusException : ServiceException
{
    public InvalidMatchStatusException()
    {
        Description = ErrorMessage.InvalidStatus;
    }
}