using GestorDeBacklogs.Api.Config;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace GestorDeBacklogs.Api.Services;

public enum EntraLoginState { Pending, Success, Error }

public record EntraLoginStatus(EntraLoginState State, string? Message = null);

public record EntraDeviceCodeInfo(string VerificationUri, string UserCode, int ExpiresInSeconds);

public interface IEntraAuthService
{
    Task<EntraDeviceCodeInfo> StartDeviceCodeLoginAsync(CancellationToken ct = default);
    EntraLoginStatus GetLoginStatus();
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
    Task<bool> IsSignedInAsync();
    Task SignOutAsync();
}

public class EntraAuthService : IEntraAuthService
{
    // App ID de recurso do Azure DevOps, fixo e igual em qualquer tenant.
    private const string AzureDevOpsResourceAppId = "499b84ac-1321-427f-aa17-267ca6975798";
    private static readonly string[] Scopes = [$"{AzureDevOpsResourceAppId}/.default"];

    private readonly IPublicClientApplication _app;
    private readonly Task _cacheInitialization;
    private EntraLoginStatus _status = new(EntraLoginState.Error, "Nenhum login realizado ainda.");

    public EntraAuthService(IOptions<AzureAdSettings> options)
    {
        var settings = options.Value;
        _app = PublicClientApplicationBuilder.Create(settings.ClientId)
            .WithTenantId(settings.TenantId)
            .Build();

        _cacheInitialization = AttachTokenCacheAsync();
    }

    private async Task AttachTokenCacheAsync()
    {
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestorBacklogs", "msal_cache");
        Directory.CreateDirectory(cacheDir);

        var storageProperties = new StorageCreationPropertiesBuilder("msal.cache", cacheDir).Build();
        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(_app.UserTokenCache);
    }

    // O device code flow bloqueia até o usuário completar o login em outra aba/dispositivo, então
    // roda em background e devolve o código pro chamador assim que ele é emitido pelo callback do MSAL.
    public async Task<EntraDeviceCodeInfo> StartDeviceCodeLoginAsync(CancellationToken ct = default)
    {
        await _cacheInitialization;

        _status = new EntraLoginStatus(EntraLoginState.Pending, "Aguardando login...");
        var deviceCodeInfoTcs = new TaskCompletionSource<EntraDeviceCodeInfo>();

        _ = Task.Run(async () =>
        {
            try
            {
                await _app.AcquireTokenWithDeviceCode(Scopes, callback =>
                {
                    deviceCodeInfoTcs.TrySetResult(new EntraDeviceCodeInfo(
                        callback.VerificationUrl,
                        callback.UserCode,
                        Math.Max(0, (int)(callback.ExpiresOn - DateTimeOffset.UtcNow).TotalSeconds)));
                    return Task.CompletedTask;
                }).ExecuteAsync(ct);

                _status = new EntraLoginStatus(EntraLoginState.Success);
            }
            catch (Exception ex)
            {
                deviceCodeInfoTcs.TrySetException(ex);
                _status = new EntraLoginStatus(EntraLoginState.Error, ex.Message);
            }
        }, ct);

        return await deviceCodeInfoTcs.Task;
    }

    public EntraLoginStatus GetLoginStatus() => _status;

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        await _cacheInitialization;

        var accounts = await _app.GetAccountsAsync();
        var account = accounts.FirstOrDefault();
        if (account is null)
        {
            throw new InvalidOperationException("Nenhuma conta Microsoft conectada. Faça login novamente.");
        }

        try
        {
            var result = await _app.AcquireTokenSilent(Scopes, account).ExecuteAsync(ct);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            throw new InvalidOperationException("A sessão Microsoft expirou. Faça login novamente.");
        }
    }

    public async Task<bool> IsSignedInAsync()
    {
        await _cacheInitialization;
        var accounts = await _app.GetAccountsAsync();
        return accounts.Any();
    }

    public async Task SignOutAsync()
    {
        await _cacheInitialization;
        var accounts = await _app.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await _app.RemoveAsync(account);
        }

        _status = new EntraLoginStatus(EntraLoginState.Error, "Desconectado.");
    }
}
