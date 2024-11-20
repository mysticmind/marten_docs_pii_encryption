namespace marten_docs_pii;

public interface IEncryptionService
{
    Task<string> EncryptAsync(string plainText, string? key = null);
    Task<(bool success, string plainText)> TryDecryptAsync(string cipherText, string? key=null);
    Task DropEncryptionKeyAsync(string key) => new(() => { });
}