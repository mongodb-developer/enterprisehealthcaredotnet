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
            PipelineDefinition<ChangeStreamDocument<Patient>, ChangeStreamDocument<Patient>>.Create(new BsonDocument[]
                { });

        using var cursor = await _patientsCollection.WatchAsync(pipeline, options, stoppingToken);

        await cursor.ForEachAsync(change =>
        {
            if (change.OperationType != ChangeStreamOperationType.Replace || change.FullDocument is null)
                return;

            var patient = change.FullDocument;
            var patientId = patient.Id.ToString();
            var newHealthConditions = patient.PatientRecord.HealthConditions;

            // Retrieve the old list from the cache
            if (_conditionCache.TryGetValue(patientId, out var oldHealthConditions))
            {
                // Project the new health conditions to strings (e.g., ConditionName)
                var newConditionNames = newHealthConditions.Select(hc => hc.Name).ToList();

                // Determine added and removed conditions
                var addedConditions = newConditionNames.Except(oldHealthConditions).ToList();
                var removedConditions = oldHealthConditions.Except(newConditionNames).ToList();

                // Log or handle the changes
                if (addedConditions.Any())
                {
                    _hubContext.Clients.All.SendAsync("NewHealthConditionAdded", new
                    {
                        patient = patient.PatientName,
                        condition = addedConditions
                    }, cancellationToken: stoppingToken);
                }

                if (removedConditions.Any())
                {
                    Console.WriteLine(
                        $"Removed conditions for patient {patientId}: {string.Join(", ", removedConditions)}");
                }

                // Update the cache with the new list
                _conditionCache[patientId] = newConditionNames;
            }
            else
            {
                var conditionNames = newHealthConditions.Select(hc => hc.Name).ToList();
                _conditionCache[patientId] = conditionNames;

                // Send alert if there are any conditions
                if (conditionNames.Any())
                {
                    _hubContext.Clients.All.SendAsync("NewHealthConditionAdded", new
                    {
                        patient = patient.PatientName,
                        condition = conditionNames
                    });
                }
            }
        }, stoppingToken);
    }
}