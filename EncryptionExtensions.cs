using System.Linq.Expressions;
using Marten;

namespace marten_docs_pii;

public static class EncryptionExtensions
{
    public static void UseEncryptionRulesForProtectedInformation(
        this StoreOptions options,
        IEncryptionService encryptionService)
    {
        EncryptionRules.Initialize(encryptionService);
        
        // Replace the default serializer with our decrypting serializer
        var innerSerializer = options.Serializer();
        options.Serializer(new EncryptionSerializer(
            innerSerializer,
            EncryptionRules.Instance));
    }

    public static MartenRegistry.DocumentMappingExpression<T> AddEncryptionRuleForProtectedInformation<T>(
        this MartenRegistry.DocumentMappingExpression<T> documentMappingExpression, 
        Expression<Func<T, object>> memberExpression) where T : class
    {
        EncryptionRules.Instance.AddEncryptionRule(memberExpression);
        return documentMappingExpression;
    }
}