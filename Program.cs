using Marten;
using marten_docs_pii;

// Generate encryption key and IV
// var (key, iv) = AesEncryptionService.GenerateKeyAndIv();
// Console.WriteLine($"Key: {key}, IV: {iv}");

// these should be stored securely
const string key = "tKDM8/ZCTZkRtKi7ZKDALBTEE/+WmMA5SEpWp02Y0qs=";
const string iv = "L/G6cEvpCK/0XUS2kWsKoA==";

var encryptionService = new AesEncryptionService(key, iv);
// var encryptionService = new VaultEncryptionService("http://localhost:8300", "root");

await using var store = DocumentStore.For(opts =>
{
    opts.Connection("Host=localhost;Database=marten_testing;Username=postgres;Password=postgres");
    opts.UseEncryptionRulesForProtectedInformation(encryptionService);
    opts.Schema.For<Person>()
        .AddEncryptionRuleForProtectedInformation(x => x.Name)
        .AddEncryptionRuleForProtectedInformation(x => x.Phone)
        .AddEncryptionRuleForProtectedInformation(x => x.Address.Street);
});

await using var session = store.LightweightSession();

// Create and store a person
var person1 = new Person(
    Guid.NewGuid(), 
    "John Doe", 
    "111-111", 
    new Address("123 Main St", "Anytown"));

session.Store(person1);
await session.SaveChangesAsync(); // Data will be automatically encrypted before saving

// await encryptionService.DropEncryptionKeyAsync(person1.Id.ToString());

// Query back to verify
var id = person1.Id;
var person = await session.LoadAsync<Person>(id);
Console.WriteLine($"Name: {person?.Name}"); // Will show decrypted value
Console.WriteLine($"Phone: {person?.Phone}"); // Will show decrypted value
Console.WriteLine($"Street: {person?.Address.Street}, City: {person?.Address.City}");

// You can also query multiple records
var allPeople = await session.Query<Person>().ToListAsync();
foreach (var p in allPeople)
{
    // All sensitive fields will be automatically decrypted
    Console.WriteLine($"Name: {p.Name}, Phone: {p.Phone}, Street: {p.Address.Street}");
}

public interface IHasEncryptionKey { 
    string EncryptionKey { get; } 
}

public record Address(string Street, string City);

public record Person(Guid Id, string Name, string Phone, Address Address) : IHasEncryptionKey
{
    public string EncryptionKey => Id.ToString();
}