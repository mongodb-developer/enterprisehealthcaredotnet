using System.Security.Cryptography;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
namespace EnterpriseHealthcareDotNet.Services;

public class QueryableEncryptionHelpers
{
  private readonly IConfigurationRoot _appSettings;
  private readonly string _cryptSharedLibPath;
  public QueryableEncryptionHelpers(IConfigurationRoot appSettings)
  {
    _appSettings = appSettings;
    _cryptSharedLibPath = _appSettings["CryptSharedLibPath"] ?? throw new ArgumentNullException("CryptSharedLibPath", "Path to the Automatic Encryption Shared Library must be provided in appsettings.json");
    
    var relativeLibPath = _appSettings["CryptSharedLibPath"] ??
                          "mongo_crypt_shared_v1-macos-arm64-enterprise-8.2.0/lib/mongo_crypt_v1.dylib";
    var projectRoot = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
    var absoluteLibPath = Path.GetFullPath(Path.Combine(projectRoot, relativeLibPath));

    Console.WriteLine($"Resolved CryptSharedLibPath: {absoluteLibPath}");
    Console.WriteLine($"Exists: {File.Exists(absoluteLibPath)}");
  }
  
  public static Guid GetOrCreateDataKey(
    ClientEncryption clientEncryption,
    IMongoCollection<BsonDocument> keyVaultCollection,
    string kmsProvider,
    string keyAltName)
  {
    // Try to find existing key by alternate name
    var filter = Builders<BsonDocument>.Filter.Eq("keyAltNames", keyAltName);
    var existing = keyVaultCollection.Find(filter).FirstOrDefault();

    if (existing != null)
    {
      return existing["_id"].AsGuid;
    }

    // Otherwise, create new key with alternate name
    var options = new DataKeyOptions(alternateKeyNames: new[] { keyAltName });
    return clientEncryption.CreateDataKey(kmsProvider, options, CancellationToken.None);
  }

  public Dictionary<string, IReadOnlyDictionary<string, object>> 
GetKmsProviderCredentials(string kmsProviderName,
        bool generateNewLocalKey)
  {
    if(kmsProviderName == "local")
    {
      if (generateNewLocalKey)
      {
        File.Delete("customer-master-key.txt");

        // start-generate-local-key
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        try
        { 
          var bytes = new byte[96];
          randomNumberGenerator.GetBytes(bytes);
          var localCustomerMasterKeyBase64 = Convert.ToBase64String(bytes);
          File.WriteAllText("customer-master-key.txt", localCustomerMasterKeyBase64);
        }
        catch (Exception e)
        {
          throw new Exception("Unable to write Customer Master Key file due to the following error: " + e.Message);
        }
    // end-generate-local-key
    }

    // start-get-local-key
    // WARNING: Do not use a local key file in a production application
   var kmsProviderCredentials = new Dictionary<string, IReadOnlyDictionary<string, object>>();
   try
   {
     var localCustomerMasterKeyBase64 = File.ReadAllText("customer-master-key.txt");
     var localCustomerMasterKeyBytes = Convert.FromBase64String(localCustomerMasterKeyBase64);

     if (localCustomerMasterKeyBytes.Length != 96)
     {
       throw new Exception("Expected the customer master key file to be 96 bytes.");
     }

     var localOptions = new Dictionary<string, object>
     {
       { "key", localCustomerMasterKeyBytes }
     };

     kmsProviderCredentials.Add("local", localOptions);
     }
      // end-get-local-key
     catch (Exception e)
     {
       throw new Exception("Unable to read the Customer Master Key due to the following error: " + e.Message);
     }
 return kmsProviderCredentials;

 }

 throw new Exception("Unrecognized value for KMS provider name \"" + kmsProviderName + "\"  encountered while retrieving KMS credentials.");
 }

public BsonDocument GetCustomerMasterKeyCredentials(string kmsProvider)
{
  if (kmsProvider == "local")
  {
    // start-kmip-local-cmk-credentials
    var customerMasterKeyCredentials = new BsonDocument();
    // end-kmip-local-cmk-credentials
    return customerMasterKeyCredentials;
  }
  else
    {
      throw new Exception("Unrecognized value for KMS provider name \"" + kmsProvider + "\"  encountered while retrieving Customer Master Key credentials.");
    }
  }

public AutoEncryptionOptions GetAutoEncryptionOptions(CollectionNamespace keyVaultNamespace,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviderCredentials)
{
  var kmsProvider = kmsProviderCredentials.Keys.First();

  var sharedLibPath = Path.GetFullPath(
    _appSettings["CryptSharedLibPath"] ??
    "EnterpriseHealthcareDotNet/mongo_crypt_shared_v1-macos-arm64-enterprise-8.2.0/lib/mongo_crypt_v1.dylib"
  );

  Console.WriteLine($"Resolved CryptSharedLibPath: {sharedLibPath}");
  Console.WriteLine($"Exists: {File.Exists(sharedLibPath)}");

  var extraOptions = new Dictionary<string, object>
  {
    { "cryptSharedLibRequired", true },
    { "cryptSharedLibPath", sharedLibPath }
  };

var autoEncryptionOptions = new AutoEncryptionOptions(
keyVaultNamespace,
kmsProviderCredentials,
extraOptions: extraOptions);
// end-auto-encryption-options

  return autoEncryptionOptions;
}

public ClientEncryption GetClientEncryption(IMongoClient keyVaultClient,
        CollectionNamespace keyVaultNamespace, Dictionary<string, IReadOnlyDictionary<string, object>> kmsProviderCredentials)
{
  var kmsProvider = kmsProviderCredentials.Keys.First();

  // start-client-encryption
  var clientEncryptionOptions = new ClientEncryptionOptions(
                keyVaultClient: keyVaultClient,
                keyVaultNamespace: keyVaultNamespace,
                kmsProviders: kmsProviderCredentials
  );
  var clientEncryption = new ClientEncryption(clientEncryptionOptions);
            // end-client-encryption
            return clientEncryption;
  }
}