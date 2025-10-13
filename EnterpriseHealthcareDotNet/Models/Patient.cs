using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EnterpriseHealthcareDotNet.Models;

public class Patient
{
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string PatientName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public PatientRecord PatientRecord { get; set; }
}

public class PatientRecord
{
    public string SSN { get; set; }
    public List<HealthCondition> HealthConditions { get; set; }
}

public class HealthCondition
{
    public string Name { get; set; }
    public Status Status { get; set; }
    public DateTime Date { get; set; }
}
public enum Status
{
    Active,
    Past
}
