using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// User login service that authenticates via StoreHub API.
/// On successful login, syncs products from StoreHub to local cache.
/// </summary>
public class StoreHubUserLogInService : IUserLogInService
{
    private readonly IStoreHubClient _storeHubClient;
    private readonly IProductCacheService _productCache;
    private readonly IEventAggregator _eventAggregator;
    private readonly StoreHubOptions _options;
    private readonly ILogger<StoreHubUserLogInService> _logger;

    public StoreHubUserLogInService(
        IStoreHubClient storeHubClient,
        IProductCacheService productCache,
        IEventAggregator eventAggregator,
        IOptions<StoreHubOptions> options,
        ILogger<StoreHubUserLogInService> logger)
    {
        _storeHubClient = storeHubClient;
        _productCache = productCache;
        _eventAggregator = eventAggregator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> LogInAsync(string username, string password)
    {
        try
        {
            _logger.LogInformation("Attempting StoreHub login for user: {Username}", username);

            // Authenticate with StoreHub API
            var response = await _storeHubClient.LoginAsync(username, password);

            if (!response.Success || response.User is null)
            {
                _logger.LogWarning("Login failed for user: {Username}. Error: {Error}",
                    username, response.ErrorMessage);
                return false;
            }

            _logger.LogInformation("Login successful for user: {Username}", username);

            // Sync products if enabled
            if (_options.AutoSyncProductsOnStartup)
            {
                await SyncProductsAsync();
            }

            // Publish login event
            var loggedInUser = new StoreHubLoggedInUser(response.User);
            _eventAggregator.GetEvent<UserLoggedInEvent>().Publish(loggedInUser);

            return true;
        }
        catch (StoreHubClientException ex)
        {
            _logger.LogError(ex, "StoreHub connection error during login");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return false;
        }
    }

    public void LogOut()
    {
        _storeHubClient.ClearAuthToken();
        _eventAggregator.GetEvent<UserLoggedOutEvent>().Publish();
        _logger.LogInformation("User logged out");
    }

    private async Task SyncProductsAsync()
    {
        _logger.LogInformation("Syncing products from StoreHub...");

        var result = await _productCache.SyncProductsAsync();

        if (result.Success)
        {
            _logger.LogInformation("Product sync complete. {Count} products cached.", result.ProductCount);
        }
        else
        {
            _logger.LogWarning("Product sync failed: {Error}", result.ErrorMessage);
        }
    }

    /// <summary>
    /// ILoggedInUser implementation for StoreHub users.
    /// </summary>
    private class StoreHubLoggedInUser : ILoggedInUser
    {
        private readonly StoreUserDto _user;

        public StoreHubLoggedInUser(StoreUserDto user)
        {
            _user = user;
        }

        // StoreHub uses Guid, but legacy code expects int UserId
        // Use a hash or 0 for compatibility
        public int UserId => 0;

        public Guid? StoreHubUserId => _user.Id;

        public string FirstName => _user.FirstName;

        public string LastName => _user.LastName;

        public int RoleId => _user.RoleId;
    }
}
