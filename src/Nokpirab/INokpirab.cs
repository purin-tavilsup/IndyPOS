namespace Nokpirab;

public interface INokpirab
{
	Task SendAsync(ICommand command, CancellationToken cancellationToken = default);
	
	Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
	
	Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}