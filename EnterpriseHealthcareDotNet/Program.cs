using EnterpriseHealthcareDotNet.Components;
using EnterpriseHealthcareDotNet.Models;
using EnterpriseHealthcareDotNet.Services;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.Encryption;
using MongoDB.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();



// var connectionString = builder.Configuration.GetValue<string>("MongoDBConnectionString");
// builder.Services.AddDbContext<HealthcareDbContext>(options => options.UseMongoDB(
//     connectionString ?? "", "MongoDBMedical"));

builder.Services.AddSingleton<QueryableEncryptionHelpers>(sp =>
{
    var config = (IConfigurationRoot)sp.GetRequiredService<IConfiguration>();
    return new QueryableEncryptionHelpers(config);
});

builder.Services.AddScoped<PatientService>();

var configuration = builder.Configuration;
var qeHelpers = new QueryableEncryptionHelpers(configuration);

string uri = configuration["MongoDBConnectionString"]!;
string keyVaultDb = configuration["KeyVaultDatabase"] ?? "encryption";
string keyVaultColl = configuration["KeyVaultCollection"] ?? "__keyVault";
string kmsProviderName = configuration["KmsProvider"] ?? "local";
string encryptedDb = configuration["EncryptedDatabase"] ?? "MongoDBMedical";
string cryptSharedLibPath = configuration["CryptSharedLibPath"] ?? throw new ArgumentNullException("CryptSharedLibPath", "Path to the Automatic Encryption Shared Library must be provided in appsettings.json");



var keyVaultNamespace = CollectionNamespace.FromFullName($"{keyVaultDb}.{keyVaultColl}");


// Generate/reuse KMS provider credentials
var kmsProviders = qeHelpers.GetKmsProviderCredentials(
    kmsProviderName,
    generateNewLocalKey: !File.Exists("customer-master-key.txt"));



// Configure MongoDB client settings for QE
MongoClientSettings.Extensions.AddAutoEncryption();
var clientSettings = MongoClientSettings.FromConnectionString(uri);
clientSettings.AutoEncryptionOptions = qeHelpers.GetAutoEncryptionOptions(
    keyVaultNamespace,
    kmsProviders);

using var clientEncryption = new ClientEncryption(
    new ClientEncryptionOptions(new MongoClient(clientSettings), keyVaultNamespace, kmsProviders));


var equalityKey = clientEncryption.CreateDataKey("local", new DataKeyOptions());
var rangeKey = clientEncryption.CreateDataKey("local", new DataKeyOptions());

builder.Services.AddSingleton(qeHelpers);
builder.Services.AddSingleton(new EncryptionKeys(equalityKey, rangeKey));
builder.Services.AddScoped<Patient>();

// Register DbContext with QE-enabled client
builder.Services.AddDbContext<HealthcareDbContext>(options =>
{
    options.UseMongoDB(new MongoOptionsExtension()
        .WithClientSettings(clientSettings)
        .WithDatabaseName(encryptedDb)
        .WithKeyVaultNamespace(keyVaultNamespace)
        .WithCryptProvider(CryptProvider.AutoEncryptSharedLibrary, cryptSharedLibPath)
        .WithKmsProviders(kmsProviders));
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();