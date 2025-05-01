using EnterpriseHealthcareDotNet.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace EnterpriseHealthcareDotNet.Services;

public class MongoDBService
{
    private readonly IConfiguration _appSettings;
    private IMongoCollection<Patient>? _patientsCollection;
    private IMongoClient? _client;

    public MongoDBService(IConfiguration appSettings)
    {
        _appSettings = appSettings;
        
        var camelCaseConvention = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("CamelCase", camelCaseConvention, type => true);

        _client = new MongoClient(appSettings["MongoDBConnectionString"] ??
                                  throw new ArgumentNullException("MongoDbUri cannot be null"));
        
        _patientsCollection = _client.GetDatabase("MongoDBMedical").GetCollection<Patient>("Patients");
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
        if(_patientsCollection == null)
            throw new InvalidOperationException("Patients collection is not initialized");
        
        await _patientsCollection.ReplaceOneAsync(p => p.Id == ObjectId.Parse(id), patient);
    }

    public async Task DeletePatientAsync(string id)
    {
        if(_patientsCollection == null)
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

    public IMongoCollection<Patient>? GetPatientsCollection()
    {
        if (_patientsCollection == null)
            throw new InvalidOperationException("Patients collection is not initialized");

        return _patientsCollection;
    }
}