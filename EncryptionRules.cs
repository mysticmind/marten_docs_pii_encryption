using System.Linq.Expressions;
using System.Reflection;

namespace marten_docs_pii;

public class EncryptionRules
{
    private readonly Dictionary<Type, List<LambdaExpression>> _encryptionRules = new();
    private static volatile EncryptionRules? _instance;
    private static readonly object Lock = new();
    private readonly IEncryptionService _encryptionService;

    private EncryptionRules(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public static void Initialize(IEncryptionService encryptionService)
    {
        if (_instance != null)
        {
            throw new InvalidOperationException("EncryptionRules has already been initialized");
        }

        lock (Lock)
        {
            if (_instance == null)
            {
                _instance = new EncryptionRules(encryptionService);
            }
            else
            {
                throw new InvalidOperationException("EncryptionRules has already been initialized");
            }
        }
    }

    public static EncryptionRules Instance 
    {
        get
        {
            if (_instance == null)
            {
                throw new InvalidOperationException(
                    "Call UseEncryptionRulesForProtectedInformation on StoreOptions before using it");
            }
            return _instance;
        }
    }

    public void AddEncryptionRule<T>(Expression<Func<T, object>> propertySelector) where T : class
    {
        if (!_encryptionRules.ContainsKey(typeof(T)))
        {
            _encryptionRules[typeof(T)] = [];
        }

        _encryptionRules[typeof(T)].Add(propertySelector);
    }

    public async Task<object> EncryptDocumentAsync(object document)
    {
        return await TransformDocumentAsync(document, true);
    }

    public async Task<object> DecryptDocumentAsync(object document)
    {
        document = await TransformDocumentAsync(document, false);
        return document;
    }

    private async Task<object> TransformDocumentAsync(object document, bool encrypt)
    {
        var documentType = document.GetType();
        if (!_encryptionRules.TryGetValue(documentType, out var expressions))
        {
            return document;
        }

        string? key = null;

        // check if document implement IHasEncryptionKey
        if (documentType.GetInterfaces().Any(x => x == typeof(IHasEncryptionKey)))
        {
            key = ((IHasEncryptionKey)document).EncryptionKey;
        }


        // For records, we need to create a new instance
        var currentObj = document;
        var anyChanges = false;
        
        foreach (var expression in expressions)
        {
            var memberExp = GetMemberExpression(expression.Body);
            if (memberExp == null) continue;

            var value = GetPropertyValue(currentObj, memberExp);
            if (value == null) continue;

            var currentValue = value.ToString()!;
            string transformedValue;
            
            if (encrypt)
            {
                transformedValue = await _encryptionService.EncryptAsync(currentValue, key);
                anyChanges = true;
            }
            else
            {
                var decryptResult = await _encryptionService.TryDecryptAsync(currentValue, key);
                if (decryptResult.success)
                {
                    transformedValue = decryptResult.plainText;
                    anyChanges = true;
                }
                else
                {
                    continue;
                }
            }
            
            if (memberExp.Expression is MemberExpression parentMemberExp)
            {
                // Handle nested property (like Prop1.childProp)
                var parentValue = GetPropertyValue(currentObj, parentMemberExp);
                if (parentValue == null) continue;

                var propertyName = memberExp.Member.Name;
                var newParentObj = CreateNewWithProperty(parentValue, propertyName, transformedValue);
                
                // Update the parent property on the main document
                var parentPropName = parentMemberExp.Member.Name;
                currentObj = CreateNewWithProperty(currentObj, parentPropName, newParentObj);
            }
            else
            {
                // Handle top-level property
                var propertyName = memberExp.Member.Name;
                currentObj = CreateNewWithProperty(currentObj, propertyName, transformedValue);
            }
        }

        return anyChanges ? currentObj : document;
    }

    private static object CreateNewWithProperty(object obj, string propertyName, object newValue)
    {
        var type = obj.GetType();
        var constructor = type.GetConstructors().First();
        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            if (string.Equals(param.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                args[i] = newValue;
            }
            else
            {
                var prop = type.GetProperty(param.Name!, BindingFlags.Public | BindingFlags.Instance);
                args[i] = prop!.GetValue(obj)!;
            }
        }

        return constructor.Invoke(args);
    }

    private static object? GetPropertyValue(object obj, MemberExpression memberExp)
    {
        var members = new List<MemberExpression>();
        var current = memberExp;
        
        while (current != null)
        {
            members.Add(current);
            current = current.Expression as MemberExpression;
        }

        members.Reverse();

        var value = obj;
        foreach (var member in members)
        {
            if (value == null) return null;
            if (member.Member is PropertyInfo prop)
            {
                value = prop.GetValue(value);
            }
        }

        return value;
    }

    private static MemberExpression? GetMemberExpression(Expression expression)
    {
        return expression switch
        {
            MemberExpression memberExp => memberExp,
            UnaryExpression unaryExp => unaryExp.Operand as MemberExpression,
            _ => null
        };
    }

    public bool HasEncryptionRules(object document)
    {
        return _encryptionRules.ContainsKey(document.GetType());
    }
}