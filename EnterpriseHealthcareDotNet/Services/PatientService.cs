using EnterpriseHealthcareDotNet.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace EnterpriseHealthcareDotNet.Services;

public class PatientService(HealthcareDbContext healthcareDbContext)
{
    private readonly HealthcareDbContext _healthcareDbContext = healthcareDbContext;

    public List<Patient> GetAllPatients() => _healthcareDbContext.Patients.OrderBy(p => p.Id).AsNoTracking().ToList();


    public Patient? GetPatientByName(string name) =>
        _healthcareDbContext.Patients.FirstOrDefault(p => p.PatientName == name);

    public Patient? GetPatientById(string id)
    {
        var patient =  _healthcareDbContext.Patients.FirstOrDefault(p => p.Id == id);
        return patient;
    }

    public void AddPatient(Patient patient)
    {
        _healthcareDbContext.Patients.Add(patient);
        _healthcareDbContext.SaveChanges();
    }

    public void EditPatient(Patient patient)
    {
        var patientToEdit = _healthcareDbContext.Patients.FirstOrDefault(p => p.Id == patient.Id);

        if (patientToEdit != null)
        {
            patientToEdit.PatientName = patient.PatientName;
            patientToEdit.DateOfBirth = patient.DateOfBirth;
            patientToEdit.PatientRecord = patient.PatientRecord;

            _healthcareDbContext.Patients.Update(patientToEdit);
            _healthcareDbContext.SaveChanges();
        }
        else
        {
            throw new ArgumentException("Patient not found");
        }
    }

    public void DeletePatient(string patientId)
    {
        var patientToDelete = _healthcareDbContext.Patients.Find(patientId);

        if (patientToDelete != null)
        {
            _healthcareDbContext.Patients.Remove(patientToDelete);
            _healthcareDbContext.SaveChanges();
        }
        else
        {
            throw new ArgumentException("Patient not found");
        }
    }

    public List<Patient> SearchPatientsBySSNAsync(string searchSsn)
    {
        var patients = _healthcareDbContext.Patients.Where(p => p.PatientRecord.SSN == searchSsn).ToList();

        return patients;
    }

    public List<Patient> SearchPatientsByDOBAsync(DateTime startDate, DateTime endDate)
    {
        var patients = _healthcareDbContext.Patients.Where(p => p.DateOfBirth >= startDate && p.DateOfBirth <= endDate)
            .ToList();

        return patients;
    }
}