using Duende.IdentityServer.Models;

namespace Portfolio.TradeOrderService.Auth;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("orders.read",  "Read trade orders"),
        new ApiScope("orders.write", "Submit trade orders")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("orders-api", "Trade Orders API")
        {
            Scopes = { "orders.read", "orders.write" }
        }
    ];

    public static IEnumerable<Client> Clients =>
    [
        // Machine-to-machine: Orders API calling downstream services
        new Client
        {
            ClientId = "orders-api-client",
            ClientName = "Orders API (M2M)",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("orders-api-secret".Sha256()) },
            AllowedScopes = { "orders.read", "orders.write" }
        },

        // Machine-to-machine: Sync/Outbox relay
        new Client
        {
            ClientId = "sync-worker",
            ClientName = "Outbox Relay Worker (M2M)",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            ClientSecrets = { new Secret("sync-worker-secret".Sha256()) },
            AllowedScopes = { "orders.read" }
        },

        // Browser: Dashboard (PKCE — no client secret)
        new Client
        {
            ClientId = "dashboard",
            ClientName = "Trade Order Dashboard",
            AllowedGrantTypes = GrantTypes.Code,
            RequireClientSecret = false,
            RequirePkce = true,
            AllowedCorsOrigins = { "http://localhost:3000" },
            RedirectUris = { "http://localhost:3000/callback" },
            PostLogoutRedirectUris = { "http://localhost:3000" },
            AllowedScopes = { "openid", "profile", "orders.read" }
        }
    ];
}
