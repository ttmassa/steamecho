namespace SteamEcho.Core.Exceptions;

public class PrivateProfileException : Exception
{
    public PrivateProfileException() : base("The requested Steam profile is not public.")
    {
    }

    public PrivateProfileException(string message) : base(message)
    {
    }

    public PrivateProfileException(string message, Exception innerException) : base(message, innerException)
    {
    }
}