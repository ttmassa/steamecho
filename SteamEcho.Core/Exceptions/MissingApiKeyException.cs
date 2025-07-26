namespace SteamEcho.Core.Exceptions;

public class MissingApiKeyException : Exception
{
    public MissingApiKeyException() : base("Steam API key is missing from the configuration.")
    {
    }

    public MissingApiKeyException(string message) : base(message)
    {
    }

    public MissingApiKeyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}