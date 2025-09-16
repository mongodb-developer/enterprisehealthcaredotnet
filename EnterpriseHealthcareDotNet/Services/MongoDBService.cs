using EnterpriseHealthcareDotNet.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Authentication.Oidc;

namespace EnterpriseHealthcareDotNet.Services;

public class MongoDBService
{
    private readonly IConfiguration _appSettings;
    private IMongoCollection<Patient>? _patientsCollection;
    private IMongoClient? _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenAcquisition _tokenAcquisition;

    public MongoDBService(IConfiguration appSettings, IHttpContextAccessor httpContextAccessor, ITokenAcquisition tokenAcquisition)
    {
        _appSettings = appSettings;
        _httpContextAccessor = httpContextAccessor;
        _tokenAcquisition = tokenAcquisition;

        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);
    }

    public async Task InitializeAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            // No HTTP context, do not initialize (let UI handle login prompt)
            return;
        }


        // Use Microsoft.Identity.Web to get the access token for the user
        string[] scopes = new[] { "c3f76165-c572-4916-8a91-8a98c252492d/.default" }; // Replace with your API scope if needed
        var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);

        if (string.IsNullOrEmpty(accessToken))
        {
            // User not authenticated, do not initialize (let UI handle login prompt)
            return;
        }

        var authenticatedConnString = _appSettings["MongoDBConnectionString"] + "?authMechanism=MONGODB-OIDC&authSource=$external";
        var mongoDBClientSettings = MongoClientSettings.FromConnectionString(authenticatedConnString);
        // Pass the acquired access token directly to the OIDC callback
        mongoDBClientSettings.Credential = MongoCredential.CreateOidcCredential(new AccessTokenOidcCallback(accessToken));

        _client = new MongoClient(mongoDBClientSettings);
        try
        {
            var result = await _client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

            if (result.GetValue("ok") == 1.0)
            {
                _patientsCollection = _client.GetDatabase("medicalRecords").GetCollection<Patient>("patients");
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to MongoDB: {ex.Message}");
            throw;
        }
    }

    // OIDC callback that simply returns the provided access token
    private class AccessTokenOidcCallback : IOidcCallback
    {
        private readonly string _accessToken;
        public AccessTokenOidcCallback(string accessToken)
        {
            _accessToken = accessToken;
        }
        public OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            return new OidcAccessToken(_accessToken, expiresIn: null);
        }
        public Task<OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetOidcAccessToken(parameters, cancellationToken));
        }
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

    public async Task UpdatePatientAsync(string id, Patient patient)
    {
        if (_patientsCollection == null)
            throw new InvalidOperationException("Patients collection is not initialized");

        await _patientsCollection.ReplaceOneAsync(p => p.Id == ObjectId.Parse(id), patient);
    }

    public async Task DeletePatientAsync(string id)
    {
        if (_patientsCollection == null)
            throw new InvalidOperationException("Patients collection is not initialized");

        await _patientsCollection.DeleteOneAsync(p => p.Id == ObjectId.Parse(id));
    }

    public async Task<List<Patient>> SearchPatientsBySSNAsync(string searchSsn)
    {
        if (_patientsCollection == null)
            throw new InvalidOperationException("Patients collection is not initialized");
        var filter = Builders<Patient>.Filter.Eq("patientRecord.sSN", searchSsn);

        var patients = await _patientsCollection.Find(filter).ToListAsync();
        return patients;
    }

    public async Task<List<Patient>> SearchPatientsByDOBAsync(DateTime startDate, DateTime endDate)
    {
        if (_patientsCollection == null)
            throw new InvalidOperationException("Patients collection is not initialized");

        var filter = Builders<Patient>.Filter.And(
            Builders<Patient>.Filter.Gte("dateOfBirth", startDate),
            Builders<Patient>.Filter.Lte("dateOfBirth", endDate)
        );

        var patients = await _patientsCollection.Find(filter).ToListAsync();
        return patients;
    }
}
