using EnterpriseHealthcareDotNet.Models;
using MongoDB.Driver;

namespace EnterpriseHealthcareDotNet.Services;

public class MongoDBService
{
    private readonly IConfiguration _appSettings;
    private IMongoCollection<Patient>? _patientsCollection;

    public MongoDBService(IConfiguration appSettings)
    {
        _appSettings = appSettings;
    }
}