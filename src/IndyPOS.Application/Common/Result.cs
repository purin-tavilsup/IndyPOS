namespace IndyPOS.Application.Common;

public class Result
{
	private Result(bool isSuccess, Error error)
	{
		if (isSuccess  && error != Error.None ||
			!isSuccess && error == Error.None)
		{
			throw new ArgumentException("Invalid error", nameof(error));
		}

		IsSuccess = isSuccess;
		Error = error;
	}

	public bool IsSuccess { get; }

	public bool IsFailure => !IsSuccess;

	public Error Error { get; }

	public static Result Success() => new(true, Error.None);

	public static Result Failure(Error error) => new(false, error);
}

public sealed record Error(string Code, string Description)
{
	public static readonly Error None = new(string.Empty, string.Empty);
}