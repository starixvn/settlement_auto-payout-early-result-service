using System.Runtime.Serialization;

namespace Fanex.Repository.Exceptions;

[Serializable]
public class ServiceException : Exception
{
    public ServiceException()
    {
    }

    public ServiceException(string message)
        : base(message)
    {
    }

    public ServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected ServiceException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public string Description { get; set; }

    public bool StopLoop { get; set; }
}