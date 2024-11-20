using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines.Transit;

namespace marten_docs_pii;

public class VaultEncryptionService: IEncryptionService
{
    private readonly VaultClient _client;
    private const string DefaultKeyName = "pii-key2";

    public VaultEncryptionService(string vaultAddress, string token)
    {
        var authMethod = new TokenAuthMethodInfo(token);
        var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
        _client = new VaultClient(vaultClientSettings);
    }
    
    public async Task<string> EncryptAsync(string plainText, string? key = null)
    {
        key ??= DefaultKeyName;
        var result = await _client.V1.Secrets.Transit.EncryptAsync(
            key,
            new EncryptRequestOptions
            {
                Base64EncodedPlainText = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText))
            });
            
        return result.Data.CipherText;
    }

    public async Task<(bool success, string plainText)> TryDecryptAsync(string cipherText, string? key = null)
    {
        var (success, plainText) = await TryDecryptInternalAsync(cipherText, key);
        return (success, plainText);
    }

    private async Task<(bool success, string plainText)> TryDecryptInternalAsync(string cipherText, string? key = null)
    {
        try
        {
            key ??= DefaultKeyName;
            var result = await _client.V1.Secrets.Transit.DecryptAsync(
                key,
                new DecryptRequestOptions { CipherText = cipherText });

            var decryptedBytes = Convert.FromBase64String(result.Data.Base64EncodedPlainText);
            return (true, System.Text.Encoding.UTF8.GetString(decryptedBytes));
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    public async Task DropEncryptionKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }
        
        await _client.V1.Secrets.Transit.UpdateEncryptionKeyConfigAsync(key, new UpdateKeyRequestOptions
        {
            DeletionAllowed = true
        });

        await _client.V1.Secrets.Transit.DeleteEncryptionKeyAsync(key);
    }
}