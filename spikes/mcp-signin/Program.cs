// THROWAWAY SPIKE: proves Claude + ChatGPT can sign in to an MCP endpoint through our own
// OpenIddict with Client ID Metadata Documents (CIMD) + PKCE and Google upstream.
// Never merged. See README.md for how to run it and what it found.
using McpSignIn;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.AspNetCore.Authentication;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

var builder = WebApplication.CreateBuilder(args);
var publicUrl = builder.Configuration["Spike:PublicUrl"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("Set Spike:PublicUrl to the tunnel's https URL.");
var mcpResource = $"{publicUrl}/mcp";

builder.Services.AddDbContext<SpikeDbContext>(options =>
{
    options.UseInMemoryDatabase("spike");
    options.UseOpenIddict();
});

builder.Services.AddHttpClient(CimdClientRegistrar.HttpClientName, client =>
       {
           client.Timeout = TimeSpan.FromSeconds(5);
           client.MaxResponseContentBufferSize = 64 * 1024;
       })
       .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<CimdClientRegistrar>();

builder.Services.AddOpenIddict()
       .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<SpikeDbContext>())
       .AddServer(options =>
       {
           options.SetIssuer(new Uri(publicUrl))
                  .SetConfigurationEndpointUris(".well-known/openid-configuration",
                                                ".well-known/oauth-authorization-server")
                  .SetAuthorizationEndpointUris("connect/authorize")
                  .SetTokenEndpointUris("connect/token")
                  .AllowAuthorizationCodeFlow()
                  .AllowRefreshTokenFlow()
                  .RequireProofKeyForCodeExchange()
                  .RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.OfflineAccess, "mcp")
                  .RegisterResources(new Uri(mcpResource))
                  .IgnoreScopePermissions()
                  .IgnoreResourcePermissions()
                  .AddDevelopmentEncryptionCertificate()
                  .AddDevelopmentSigningCertificate()
                  .DisableAccessTokenEncryption();

           options.UseAspNetCore()
                  .EnableAuthorizationEndpointPassthrough()
                  .DisableTransportSecurityRequirement();

           // CIMD: an unknown https client_id is fetched and registered before OpenIddict looks it up.
           options.AddEventHandler<ExtractAuthorizationRequestContext>(handler =>
               handler.UseScopedHandler<CimdClientRegistrar>().SetOrder(int.MaxValue));
           options.AddEventHandler<ExtractTokenRequestContext>(handler =>
               handler.UseScopedHandler<CimdClientRegistrar>().SetOrder(int.MaxValue));

           // Claude picks CIMD only when both of these are advertised; otherwise it falls back to DCR.
           options.AddEventHandler<HandleConfigurationRequestContext>(handler =>
               handler.UseInlineHandler(context =>
                      {
                          context.TokenEndpointAuthenticationMethods.Add(ClientAuthenticationMethods.None);
                          context.Metadata["client_id_metadata_document_supported"] = true;
                          return default;
                      })
                      .SetOrder(int.MaxValue));
       })
       .AddValidation(options =>
       {
           options.UseLocalServer();
           options.AddAudiences(mcpResource);
           options.UseAspNetCore();
       });

builder.Services.AddAuthentication(options =>
       {
           options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
           options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
       })
       .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddGoogle(options =>
       {
           options.ClientId = builder.Configuration["Spike:Google:ClientId"] ?? "missing";
           options.ClientSecret = builder.Configuration["Spike:Google:ClientSecret"] ?? "missing";
           options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
       })
       .AddMcp(options => options.ResourceMetadata = new()
       {
           Resource = mcpResource,
           // Must equal OpenIddict's issuer exactly, and a host-only Uri always ends in "/".
           AuthorizationServers = { new Uri(publicUrl).AbsoluteUri },
           ScopesSupported = ["mcp"],
       });
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMcpServer()
       .WithHttpTransport()
       .WithTools<WhoAmITool>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<RequestLogMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "IndyPOS sign-in spike. MCP endpoint: /mcp");
// A CIMD document for testing the registrar by hand with curl, before a real client tries.
app.MapGet("/test-client.json", () => Results.Json(new
{
    client_id = $"{publicUrl}/test-client.json",
    client_name = "Spike test client",
    redirect_uris = new[] { "http://localhost:33418/callback" },
    token_endpoint_auth_method = "none",
}));
app.MapAuthorizeEndpoint(mcpResource);
app.MapMcp("/mcp").RequireAuthorization();

app.Run();
