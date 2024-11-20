using System.Security.Cryptography;
using System.Text;

namespace marten_docs_pii;

public class AesEncryptionService : IEncryptionService
{
    private readonly string _key;
    private readonly string _iv;

    public AesEncryptionService(string key, string iv)
    {
        _key = key;
        _iv = iv;
    }

    public static (string key, string iv) GenerateKeyAndIv()
    {
        using var aes = Aes.Create();
        var key = Convert.ToBase64String(aes.Key);
        var iv = Convert.ToBase64String(aes.IV);
        
        return (key, iv);
    }

    public Task<string> EncryptAsync(string plainText, string? key = null)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(_key);
        aes.IV = Convert.FromBase64String(_iv);

        var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        return Task.FromResult(Convert.ToBase64String(cipherBytes));
    }

    public Task<(bool success, string plainText)> TryDecryptAsync(string cipherText, string? key=null)
    {
        try
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_key);
            aes.IV = Convert.FromBase64String(_iv);

            var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(cipherText);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            var plainText = Encoding.UTF8.GetString(plainBytes);
            
            return Task.FromResult((true, plainText));
        }
        catch
        {
            return Task.FromResult((false, string.Empty));
        }
    }
}