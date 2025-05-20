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
    // Initialize cache with current patient conditions
    var allPatients = await _patientsCollection.Find(_ => true).ToListAsync(stoppingToken);
    foreach (var patient in allPatients)
    {
        var patientId = patient.Id.ToString();
        var conditionNames = patient.PatientRecord.HealthConditions.Select(hc => hc.Name).ToList();
        _conditionCache[patientId] = conditionNames;
    }

    var options = new ChangeStreamOptions
    {
        FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
    };

    var pipeline =
        PipelineDefinition<ChangeStreamDocument<Patient>, ChangeStreamDocument<Patient>>.Create(new BsonDocument[] { });

    using var cursor = await _patientsCollection.WatchAsync(pipeline, options, stoppingToken);

    await cursor.ForEachAsync(change =>
    {
        if (change.OperationType != ChangeStreamOperationType.Replace && change.OperationType != ChangeStreamOperationType.Create && change.FullDocument is null)
            return;

        var patient = change.FullDocument;
        var patientId = patient.Id.ToString();
        var newConditionNames = patient.PatientRecord.HealthConditions.Select(hc => hc.Name).ToList();

        // Retrieve the old list from the cache
        if (_conditionCache.TryGetValue(patientId, out var oldConditionNames))
        {
            // Determine added and removed conditions
            var addedConditions = newConditionNames.Except(oldConditionNames).ToList();
            var removedConditions = oldConditionNames.Except(newConditionNames).ToList();

            // Handle added conditions
            if (addedConditions.Any())
            {
                _hubContext.Clients.All.SendAsync("NewHealthConditionAdded", new
                {
                    patient = patient.PatientName,
                    condition = addedConditions
                }, cancellationToken: stoppingToken);
            }

            // Handle removed conditions
            if (removedConditions.Any())
            {
                _hubContext.Clients.All.SendAsync("HealthConditionRemoved", new
                {
                    patient = patient.PatientName,
                    condition = removedConditions
                }, cancellationToken: stoppingToken);
            }

            // Update the cache with the new list
            _conditionCache[patientId] = newConditionNames;
        }
        else
        {
            _conditionCache[patientId] = newConditionNames;

            // Send alert if there are any conditions
            if (newConditionNames.Any())
            {
                _hubContext.Clients.All.SendAsync("NewHealthConditionAdded", new
                {
                    patient = patient.PatientName,
                    condition = newConditionNames
                });
            }
        }
    }, stoppingToken);
}
}