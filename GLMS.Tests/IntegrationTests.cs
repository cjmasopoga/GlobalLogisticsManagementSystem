using System.Net;
using System.Net.Http.Json;
using GLMS.API.Data;
using GLMS.API.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace GLMS.Tests;

// ---------------------------------------------------------------------------
// Custom WebApplicationFactory - replaces SQL Server with InMemory EF Core
// ---------------------------------------------------------------------------
public class GlmsApiFactory : WebApplicationFactory<Program>
{
    // One shared root → all DbContext scopes (i.e. all HTTP requests) see the
    // same in-memory store for the lifetime of this factory instance.
    private static readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove every descriptor whose service type involves ApiDbContext.
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(ApiDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<ApiDbContext>) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericArguments().Any(t => t == typeof(ApiDbContext))))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            // Register InMemory replacement using the shared root so all
            // requests within one test run share the same data store.
            services.AddDbContext<ApiDbContext>(options =>
            {
                options.UseInMemoryDatabase("GlmsTestDb", _dbRoot);
                options.EnableServiceProviderCaching(false);
            });
        });
    }
}

// ---------------------------------------------------------------------------
// Shared collection fixture - one factory instance per test collection
// ---------------------------------------------------------------------------
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<GlmsApiFactory> { }

// ---------------------------------------------------------------------------
// Auth Endpoint Tests
// ---------------------------------------------------------------------------
[Collection("Integration")]
public class AuthIntegrationTests
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(GlmsApiFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("admin", "Admin@1234"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<TokenDto>();
        Assert.NotNull(token);
        Assert.False(string.IsNullOrWhiteSpace(token!.Token));
        Assert.Equal("admin", token.Username);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("admin", "WrongPassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("hacker", "Admin@1234"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

// ---------------------------------------------------------------------------
// Clients Endpoint Tests
// ---------------------------------------------------------------------------
[Collection("Integration")]
public class ClientsIntegrationTests
{
    private readonly HttpClient _client;

    public ClientsIntegrationTests(GlmsApiFactory factory)
    {
        _client = factory.CreateClient();
        AttachToken();
    }

    private void AttachToken()
    {
        var response = _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("admin", "Admin@1234")).GetAwaiter().GetResult();
        var token = response.Content.ReadFromJsonAsync<TokenDto>().GetAwaiter().GetResult();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token!.Token);
    }

    [Fact]
    public async Task GetClients_ReturnsOkAndList()
    {
        var response = await _client.GetAsync("/api/clients");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<ClientDto>>();
        Assert.NotNull(list);
    }

    [Fact]
    public async Task CreateClient_ValidData_Returns201AndClientDto()
    {
        var response = await _client.PostAsJsonAsync("/api/clients",
            new CreateClientDto("Acme Corp", "acme@example.com", "EMEA"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ClientDto>();
        Assert.NotNull(created);
        Assert.Equal("Acme Corp", created!.Name);
        Assert.Equal("EMEA", created.Region);
    }

    [Fact]
    public async Task GetClientById_ExistingClient_Returns200()
    {
        var create = await _client.PostAsJsonAsync("/api/clients",
            new CreateClientDto("Test Client", "test@test.com", "APAC"));
        var created = await create.Content.ReadFromJsonAsync<ClientDto>();

        var response = await _client.GetAsync($"/api/clients/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetClientById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/clients/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteClient_ExistingClient_Returns204()
    {
        var create = await _client.PostAsJsonAsync("/api/clients",
            new CreateClientDto("To Delete", "del@test.com", "NA"));
        var created = await create.Content.ReadFromJsonAsync<ClientDto>();

        var response = await _client.DeleteAsync($"/api/clients/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}

// ---------------------------------------------------------------------------
// Contracts Endpoint Tests
// ---------------------------------------------------------------------------
[Collection("Integration")]
public class ContractsIntegrationTests
{
    private readonly HttpClient _client;

    public ContractsIntegrationTests(GlmsApiFactory factory)
    {
        _client = factory.CreateClient();
        AttachToken();
    }

    private void AttachToken()
    {
        var response = _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("admin", "Admin@1234")).GetAwaiter().GetResult();
        var token = response.Content.ReadFromJsonAsync<TokenDto>().GetAwaiter().GetResult();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token!.Token);
    }

    private async Task<ClientDto> CreateClientAsync(string name = "Logistics Co")
    {
        var r = await _client.PostAsJsonAsync("/api/clients",
            new CreateClientDto(name, $"{name}@test.com", "EMEA"));
        return (await r.Content.ReadFromJsonAsync<ClientDto>())!;
    }

    [Fact]
    public async Task GetContracts_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/contracts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateContract_ValidData_Returns201()
    {
        var client = await CreateClientAsync("Contract Owner");
        var response = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractDto(client.Id, DateTime.UtcNow,
                DateTime.UtcNow.AddYears(1), "Active", "Premium"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var contract = await response.Content.ReadFromJsonAsync<ContractDto>();
        Assert.NotNull(contract);
        Assert.Equal("Premium", contract!.ServiceLevel);
        Assert.Equal("Active", contract.Status);
    }

    [Fact]
    public async Task GetContractById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/contracts/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchContractStatus_ValidTransition_Returns204()
    {
        var client = await CreateClientAsync("Status Patcher");
        var create = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractDto(client.Id, DateTime.UtcNow,
                DateTime.UtcNow.AddYears(1), "Draft", "Standard"));
        var contract = await create.Content.ReadFromJsonAsync<ContractDto>();

        var patch = await _client.PatchAsJsonAsync(
            $"/api/contracts/{contract!.Id}/status",
            new PatchContractStatusDto("Active"));

        Assert.Equal(HttpStatusCode.NoContent, patch.StatusCode);
    }
}

// ---------------------------------------------------------------------------
// Service Requests Endpoint Tests
// ---------------------------------------------------------------------------
[Collection("Integration")]
public class ServiceRequestsIntegrationTests
{
    private readonly HttpClient _client;

    public ServiceRequestsIntegrationTests(GlmsApiFactory factory)
    {
        _client = factory.CreateClient();
        AttachToken();
    }

    private void AttachToken()
    {
        var response = _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("admin", "Admin@1234")).GetAwaiter().GetResult();
        var token = response.Content.ReadFromJsonAsync<TokenDto>().GetAwaiter().GetResult();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token!.Token);
    }

    private async Task<ContractDto> CreateActiveContractAsync()
    {
        var clientResp = await _client.PostAsJsonAsync("/api/clients",
            new CreateClientDto("SR Owner", "sr@test.com", "NA"));
        var client = await clientResp.Content.ReadFromJsonAsync<ClientDto>();

        var contractResp = await _client.PostAsJsonAsync("/api/contracts",
            new CreateContractDto(client!.Id, DateTime.UtcNow,
                DateTime.UtcNow.AddYears(1), "Active", "Standard"));
        return (await contractResp.Content.ReadFromJsonAsync<ContractDto>())!;
    }

    [Fact]
    public async Task GetServiceRequests_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/servicerequests");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateServiceRequest_ActiveContract_Returns201()
    {
        var contract = await CreateActiveContractAsync();
        var response = await _client.PostAsJsonAsync("/api/servicerequests",
            new CreateServiceRequestDto(contract.Id, "Ship 10 pallets", 500m, "Pending"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var sr = await response.Content.ReadFromJsonAsync<ServiceRequestDto>();
        Assert.NotNull(sr);
        Assert.Equal("Pending", sr!.Status);
        Assert.Equal(500m, sr.CostUsd);
    }

    [Fact]
    public async Task CreateServiceRequest_NonExistentContract_Returns404Or400()
    {
        var response = await _client.PostAsJsonAsync("/api/servicerequests",
            new CreateServiceRequestDto(99999, "Ghost request", 100m, "Pending"));

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 404 or 400 but got {response.StatusCode}");
    }

    [Fact]
    public async Task GetServiceRequestById_NonExistent_Returns404()
    {
        var response = await _client.GetAsync("/api/servicerequests/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
