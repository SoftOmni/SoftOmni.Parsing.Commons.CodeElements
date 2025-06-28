namespace SoftOmni.Parsing.Commons.CodeElements.Exceptions;

public class InvalidCodeException : ArgumentException
{
    public InvalidCodeException(string message) : base(message)
    { }
}