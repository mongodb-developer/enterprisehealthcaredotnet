using EnterpriseHealthcareDotNet.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;

namespace EnterpriseHealthcareDotNet.Services;

public class MongoDBService
{
    private readonly IConfiguration _appSettings;
    private IMongoCollection<Patient>? _patientsCollection;
    private readonly string _uri;
    private readonly string _keyVaultDatabaseName = "encryption";
    private readonly string _keyVaultCollectionName = "__keyVault";
    private readonly string _encryptedDatabaseName = "medicalRecords";
    private readonly string _encryptedCollectionName = "patients";
    private readonly string _kmsProviderName = "local";
    private readonly CollectionNamespace _keyVaultNamespace;
    private readonly Dictionary<string, IReadOnlyDictionary<string, object>> _kmsProviderCredentials;

    public MongoDBService(IConfiguration appSettings)
    {
        _appSettings = appSettings;
        _uri = appSettings["MongoDBConnectionString"] ?? throw new ArgumentNullException(nameof(_uri), "MongoDbUri cannot be null");
        _keyVaultNamespace = CollectionNamespace.FromFullName($"{_keyVaultDatabaseName}.{_keyVaultCollectionName}");
        
        InitAsync().GetAwaiter().GetResult();
    }

    public async Task InitAsync()
    {
       
    }
    
}