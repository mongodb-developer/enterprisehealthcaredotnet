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
    private readonly QueryableEncryptionHelpers _qeHelpers;
    private readonly string _uri;
    private readonly string _keyVaultDatabaseName = "encryption";
    private readonly string _keyVaultCollectionName = "__keyVault";
    private readonly string _encryptedDatabaseName = "medicalRecords";
    private readonly string _encryptedCollectionName = "patients";
    private readonly string _kmsProviderName = "local";
    private CollectionNamespace _keyVaultNamespace;
    private Dictionary<string, IReadOnlyDictionary<string, object>> _kmsProviderCredentials;

    public MongoDBService(IConfiguration appSettings)
    {
        _appSettings = appSettings;
        _uri = appSettings["MongoDBConnectionString"] ?? throw new ArgumentNullException(nameof(_uri), "MongoDbUri cannot be null");
        _keyVaultNamespace = CollectionNamespace.FromFullName($"{_keyVaultDatabaseName}.{_keyVaultCollectionName}");
        _qeHelpers = new QueryableEncryptionHelpers((IConfigurationRoot)_appSettings);
        _kmsProviderCredentials = _qeHelpers.GetKmsProviderCredentials(_kmsProviderName,
            generateNewLocalKey: false);
        
        InitAsync().GetAwaiter().GetResult();
    }

    public async Task InitAsync()
    {
        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);
        
        MongoClientSettings.Extensions.AddAutoEncryption(); // .NET/C# Driver v3.0 or later only
        var clientSettings = MongoClientSettings.FromConnectionString(_uri);
        clientSettings.AutoEncryptionOptions = _qeHelpers.GetAutoEncryptionOptions(
            _keyVaultNamespace,
            _kmsProviderCredentials);
        var encryptedClient = new MongoClient(clientSettings);
        
        var keyDatabase = encryptedClient.GetDatabase(_keyVaultDatabaseName);
        
        var encryptedFields = new BsonDocument
        {
            {
                "fields", new BsonArray
                {
                    new BsonDocument
                    {
                        { "keyId", BsonNull.Value },
                        { "path", "patientRecord.sSN" },
                        { "bsonType", "string" },
                        { "queries", new BsonDocument("queryType", "equality") }
                    },
                    new BsonDocument
                    {
                        { "keyId", BsonNull.Value },
                        { "path", "dateOfBirth" },
                        { "bsonType", "date" },
                        { "queries", new BsonDocument("queryType", "range") }
                    }
                }
            }
        };
        
        var patientDatabase = encryptedClient.GetDatabase(_encryptedDatabaseName);

        var clientEncryption = _qeHelpers.GetClientEncryption(encryptedClient,
            _keyVaultNamespace,
            _kmsProviderCredentials);

        var customerMasterKeyCredentials = _qeHelpers.GetCustomerMasterKeyCredentials(_kmsProviderName);

        if (!encryptedClient.GetDatabase(_encryptedDatabaseName).ListCollectionNames().ToList()
                .Contains(_encryptedCollectionName))
        {
            try
            {
                // start-create-encrypted-collection
                var createCollectionOptions = new CreateCollectionOptions<Patient>
                {
                    EncryptedFields = encryptedFields
                };

                await clientEncryption.CreateEncryptedCollectionAsync(patientDatabase,
                    _encryptedCollectionName,
                    createCollectionOptions,
                    _kmsProviderName,
                    customerMasterKeyCredentials);
                // end-create-encrypted-collection
            }
            catch (Exception e)
            {
                throw new Exception("Unable to create encrypted collection due to the following error: " + e.Message);
            }
        }
        _patientsCollection = encryptedClient.GetDatabase(_encryptedDatabaseName).
            GetCollection<Patient>(_encryptedCollectionName);
        
    }
    
    public async Task<List<Patient>> GetPatientsAsync()
    {
        if (_patientsCollection == null)
            throw new InvalidOperationException("Patients collection is not initialized");
            
        return await _patientsCollection.Find(_ => true).ToListAsync();
    }

    public async Task<Patient> GetPatientAsync(string id)
    {
        if (_patientsCollection == null)
            throw new InvalidOperationException("Patients collection is not initialized");
        
        return await _patientsCollection.Find(p => p.Id == ObjectId.Parse(id)).FirstOrDefaultAsync();
    }
    
     public async Task AddPatientAsync(Patient patient)
    {
        if (_patientsCollection == null)
            throw new InvalidOperationException("Patients collection is not initialized");
        
        await _patientsCollection.InsertOneAsync(patient);
    }
}