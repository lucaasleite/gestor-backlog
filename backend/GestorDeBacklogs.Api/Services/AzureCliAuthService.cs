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

    private readonly AzureCliCredential _credential = new();

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        try
        {
            var token = await _credential.GetTokenAsync(new TokenRequestContext(Scopes), ct);
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
