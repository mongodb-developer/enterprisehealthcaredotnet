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
    
    var relativeLibPath = _appSettings["CryptSharedLibPath"] ??
                          "mongo_crypt_shared_v1-macos-arm64-enterprise-8.2.0/lib/mongo_crypt_v1.dylib";
    var projectRoot = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
    _cryptSharedLibPath = Path.GetFullPath(Path.Combine(projectRoot, relativeLibPath));
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

public AutoEncryptionOptions GetAutoEncryptionOptions(CollectionNamespace keyVaultNamespace,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviderCredentials)
{
  var extraOptions = new Dictionary<string, object>
  {
    { "cryptSharedLibRequired", true },
    { "cryptSharedLibPath",  _cryptSharedLibPath }
  };

var autoEncryptionOptions = new AutoEncryptionOptions(
keyVaultNamespace,
kmsProviderCredentials,
extraOptions: extraOptions);
// end-auto-encryption-options

  return autoEncryptionOptions;
}

}