using System.Data.Common;
using Marten;
using Weasel.Core;

namespace marten_docs_pii;

public class EncryptionSerializer(
    ISerializer innerSerializer,
    EncryptionRules encryptionRules)
    : ISerializer
{
    public async Task<string> ToJsonAsync(object? document)
    {
        var doc = document != null && encryptionRules.HasEncryptionRules(document) 
            ? await encryptionRules.EncryptDocumentAsync(document) 
            : document;
        return innerSerializer.ToJson(doc);
    }

    public string ToJson(object? document)
    {
        // For backward compatibility, we'll run the async method synchronously
        return ToJsonAsync(document).GetAwaiter().GetResult();
    }

    public async Task<T> FromJsonAsync<T>(Stream stream)
    {
        var obj = innerSerializer.FromJson<T>(stream);
        if (obj != null && encryptionRules.HasEncryptionRules(obj))
        {
            return (T)await encryptionRules.DecryptDocumentAsync(obj);
        }
        return obj;
    }

    public T FromJson<T>(Stream stream)
    {
        // For backward compatibility, we'll run the async method synchronously
        return FromJsonAsync<T>(stream).GetAwaiter().GetResult();
    }

    public async Task<T> FromJsonAsync<T>(DbDataReader reader, int index)
    {
        var obj = innerSerializer.FromJson<T>(reader, index);
        if (obj != null && encryptionRules.HasEncryptionRules(obj))
        {
            return (T)await encryptionRules.DecryptDocumentAsync(obj);
        }
        return obj;
    }

    public T FromJson<T>(DbDataReader reader, int index)
    {
        // For backward compatibility, we'll run the async method synchronously
        return FromJsonAsync<T>(reader, index).GetAwaiter().GetResult();
    }

    public async ValueTask<T> FromJsonAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
        var obj = await innerSerializer.FromJsonAsync<T>(stream, cancellationToken);
        if (obj != null && encryptionRules.HasEncryptionRules(obj))
        {
            return (T)await encryptionRules.DecryptDocumentAsync(obj);
        }
        return obj;
    }

    public async ValueTask<T> FromJsonAsync<T>(DbDataReader reader, int index, CancellationToken cancellationToken)
    {
        var obj = await innerSerializer.FromJsonAsync<T>(reader, index, cancellationToken);
        if (obj != null && encryptionRules.HasEncryptionRules(obj))
        {
            return (T)await encryptionRules.DecryptDocumentAsync(obj);
        }
        return obj;
    }

    public async Task<object> FromJsonAsync(Type type, Stream stream)
    {
        var obj = innerSerializer.FromJson(type, stream);
        return encryptionRules.HasEncryptionRules(obj) 
            ? await encryptionRules.DecryptDocumentAsync(obj) 
            : obj;
    }

    public object FromJson(Type type, Stream stream)
    {
        // For backward compatibility, we'll run the async method synchronously
        return FromJsonAsync(type, stream).GetAwaiter().GetResult();
    }

    public async Task<object> FromJsonAsync(Type type, DbDataReader reader, int index)
    {
        var obj = innerSerializer.FromJson(type, reader, index);
        return encryptionRules.HasEncryptionRules(obj) 
            ? await encryptionRules.DecryptDocumentAsync(obj) 
            : obj;
    }

    public object FromJson(Type type, DbDataReader reader, int index)
    {
        // For backward compatibility, we'll run the async method synchronously
        return FromJsonAsync(type, reader, index).GetAwaiter().GetResult();
    }

    public async ValueTask<object> FromJsonAsync(Type type, Stream stream, CancellationToken cancellationToken)
    {
        var obj = await innerSerializer.FromJsonAsync(type, stream, cancellationToken);
        return encryptionRules.HasEncryptionRules(obj) 
            ? await encryptionRules.DecryptDocumentAsync(obj) 
            : obj;
    }

    public async ValueTask<object> FromJsonAsync(Type type, DbDataReader reader, int index,
        CancellationToken cancellationToken)
    {
        var obj = await innerSerializer.FromJsonAsync(type, reader, index, cancellationToken);
        return encryptionRules.HasEncryptionRules(obj) 
            ? await encryptionRules.DecryptDocumentAsync(obj) 
            : obj;
    }

    public async Task<string> ToCleanJsonAsync(object? document)
    {
        var doc = document != null && encryptionRules.HasEncryptionRules(document) 
            ? await encryptionRules.DecryptDocumentAsync(document) 
            : document;
        return innerSerializer.ToJson(doc);
    }

    public string ToCleanJson(object? document)
    {
        // For backward compatibility, we'll run the async method synchronously
        return ToCleanJsonAsync(document).GetAwaiter().GetResult();
    }

    public string ToJsonWithTypes(object document)
    {
        var doc = encryptionRules.HasEncryptionRules(document) 
            ? encryptionRules.EncryptDocumentAsync(document).GetAwaiter().GetResult() 
            : document;
        return innerSerializer.ToJsonWithTypes(doc);
    }

    public EnumStorage EnumStorage { get; } = innerSerializer.EnumStorage;
    public Casing Casing { get; } = innerSerializer.Casing;
    public ValueCasting ValueCasting { get; } = innerSerializer.ValueCasting;
}