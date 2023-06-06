﻿using FluentValidation;
using MediatR;
using ValidationException = IndyPOS.Application.Common.Exceptions.ValidationException;

namespace IndyPOS.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse> // class, ICommand<TResponse>
{
	private readonly IEnumerable<IValidator<TRequest>> _validators;

	public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
	{
		_validators = validators;
	}

	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		if (!_validators.Any()) 
			return await next();

		var context = new ValidationContext<TRequest>(request);

		var errors = _validators.Select(x => x.Validate(context))
								.SelectMany(x => x.Errors)
								.Where(x => x != null)
								.ToList();

		if (errors.Any())
			throw new ValidationException(errors);

		return await next();
	}
}