using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using Microsoft.Extensions.Logging;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Products.Delete;

/// <summary>
/// Handler for soft-deleting a product in StoreHub.
/// </summary>
public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IProductRepository productRepository,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task HandleAsync(
        DeleteProductCommand command,
        CancellationToken cancellationToken = default)
    {
        await _productRepository.SoftDeleteAsync(command.Id, cancellationToken);
        _logger.LogInformation("Soft-deleted product {ProductId}", command.Id);
    }
}
