using System.Runtime.Serialization;

namespace Fanex.Repository.Exceptions;

[Serializable]
public class HoldStatusException : ServiceException
{
    public HoldStatusException()
    {
        Description = "Failed: Cannot execute auto-settle process due to match is on hold.";
    }

    public HoldStatusException(string message)
        : base(message)
    {
    }

    public HoldStatusException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected HoldStatusException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}