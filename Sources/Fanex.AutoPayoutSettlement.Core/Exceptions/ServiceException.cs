namespace Fanex.AutoPayoutSettlement.Core.Exceptions;

public class ServiceException : Exception
{
    public ServiceException(string description)
    {
        Description = description;
    }

    public ServiceException()
    {
    }

    public string Description { get; set; }

    public bool NeedRetry { get; set; }

    public bool MarkFail { get; set; } = true;
}