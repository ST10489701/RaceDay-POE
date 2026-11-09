namespace RaceDay.Web.Services;

/// <summary>The API answered with an error (or could not be reached).</summary>
public class ApiException : Exception
{
    public ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

/// <summary>The API could not be reached at all (it is down, or the network failed).</summary>
public class ApiUnavailableException : ApiException
{
    public ApiUnavailableException(string message) : base(StatusCodes.Status503ServiceUnavailable, message)
    {
    }
}
