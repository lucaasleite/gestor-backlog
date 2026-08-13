using Azure.Core;
using Azure.Identity;

namespace GestorDeBacklogs.Api.Services;

public interface IAzureCliAuthService
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
    Task<bool> IsSignedInAsync(CancellationToken ct = default);
}

// Não faz login algum sozinho: usa a sessão que já existe na Azure CLI da máquina
// (feita via "az login" no terminal, fora do app). Sem sessão ativa, GetAccessTokenAsync lança.
public class AzureCliAuthService : IAzureCliAuthService
{
    private const string AzureDevOpsResourceAppId = "499b84ac-1321-427f-aa17-267ca6975798";
    private static readonly string[] Scopes = [$"{AzureDevOpsResourceAppId}/.default"];

    // Chamar "az" é caro (processo novo, interpretador Python subindo) - sem cache, a tela de
    // Work Items (que faz várias chamadas em paralelo à API do Azure DevOps) dispararia um "az
    // account get-access-token" por chamada. Cacheia o token em memória e só renova perto de
    // expirar; o timeout maior absorve a demora do primeiro "az" rodando dentro do container.
    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromMinutes(5);

    private readonly AzureCliCredential _credential = new(
        new AzureCliCredentialOptions { ProcessTimeout = TimeSpan.FromSeconds(60) });

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private AccessToken? _cachedToken;

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cachedToken is { } cached && cached.ExpiresOn - DateTimeOffset.UtcNow > RefreshBuffer)
        {
            return cached.Token;
        }

        await _refreshLock.WaitAsync(ct);
        try
        {
            if (_cachedToken is { } stillCached && stillCached.ExpiresOn - DateTimeOffset.UtcNow > RefreshBuffer)
            {
                return stillCached.Token;
            }

            var token = await _credential.GetTokenAsync(new TokenRequestContext(Scopes), ct);
            _cachedToken = token;
            return token.Token;
        }
        catch (CredentialUnavailableException ex)
        {
            throw new InvalidOperationException(
                "Azure CLI não encontrada ou sem login ativo. Instale a Azure CLI e rode 'az login' no terminal.", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            throw new InvalidOperationException(
                "Não foi possível obter um token via Azure CLI. Rode 'az login' novamente no terminal.", ex);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<bool> IsSignedInAsync(CancellationToken ct = default)
    {
        try
        {
            await GetAccessTokenAsync(ct);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
