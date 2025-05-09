namespace Nokpirab;

public class HandlerNotFoundException : Exception
{
	public HandlerNotFoundException(string message) : base(message) { }
}