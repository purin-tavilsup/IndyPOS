namespace IndyPOS.Application.Common.Constants;

/// <summary>The audiences Cloud.Auth issues tokens for; each resource server accepts only its own.</summary>
public static class AuthResources
{
    /// <summary>CloudApi, the business API. Every store token is for this audience.</summary>
    public const string CloudApi = "indypos-api";

    /// <summary>Cloud.Auth's own store-client API, which only CloudApi's service client may call.</summary>
    public const string Auth = "indypos-auth";
}

/// <summary>The confidential clients Cloud.Auth's own APIs trust. Both services read the secret from configuration.</summary>
public static class AuthServiceClients
{
    /// <summary>CloudApi's client, which registers, deactivates and deletes store clients.</summary>
    public const string CloudApiClientId = "cloud-api";

    public const string CloudApiSecretKey = "ServiceClients:CloudApi:ClientSecret";
}

/// <summary>The scopes Cloud.Auth knows.</summary>
public static class AuthScopes
{
    /// <summary>A store uploads its outbox events.</summary>
    public const string SyncWrite = "sync.write";

    /// <summary>A store downloads master data (users).</summary>
    public const string MasterRead = "master.read";

    /// <summary>CloudApi creates, disables and deletes store clients in Auth.</summary>
    public const string StoreClients = "auth.store-clients";
}
