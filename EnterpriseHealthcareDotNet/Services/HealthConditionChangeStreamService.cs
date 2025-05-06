using EnterpriseHealthcareDotNet.Hubs;
using EnterpriseHealthcareDotNet.Models;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using System.Linq;
using MongoDB.Bson;

namespace EnterpriseHealthcareDotNet.Services;

public class HealthConditionChangeStreamService : BackgroundService
{
    private readonly MongoDBService _mongoDBService;
    private readonly IHubContext<PharmacyHub> _hubContext;
    private readonly Dictionary<string, List<string>> _conditionCache = new();
    private IMongoCollection<Patient> _patientsCollection;

    public HealthConditionChangeStreamService(MongoDBService mongoDBService,
        IHubContext<PharmacyHub> hubContext)
    {
        _mongoDBService = mongoDBService;
        _hubContext = hubContext;
        _patientsCollection = _mongoDBService.GetPatientsCollection();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

       
    }
}