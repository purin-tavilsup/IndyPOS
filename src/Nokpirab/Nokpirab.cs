using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Nokpirab.Tests")]
namespace Nokpirab;

internal class Nokpirab : INokpirab
{
    private readonly IServiceProvider _serviceProvider;

    public Nokpirab(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        var handler = GetHandler(handlerType, commandType);
        
        await handler.HandleAsync((dynamic)command, cancellationToken);
    }

    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        var handler = GetHandler(handlerType, commandType);
        
        return await handler.HandleAsync((dynamic)command, cancellationToken);
    }

    public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = GetHandler(handlerType, queryType);
        
        return await handler.HandleAsync((dynamic)query, cancellationToken);
    }
    
    private dynamic GetHandler(Type handlerType, Type requestType)
    {
        try
        {
            return _serviceProvider.GetRequiredService(handlerType);
        }
        catch (InvalidOperationException)
        {
            throw new HandlerNotFoundException($"No handler was found for '{requestType.Name}'");
        }
    }
}