using EnterpriseHealthcareDotNet.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace EnterpriseHealthcareDotNet.Services;

public class PatientService(HealthcareDbContext healthcareDbContext)
{
    public List<Patient> GetAllPatients() => healthcareDbContext.Patients.OrderBy(p => p.Id).AsNoTracking().ToList();

    
    public Patient? GetPatientByName(string name) =>
        healthcareDbContext.Patients.FirstOrDefault(p => p.PatientName == name);

    public Patient? GetPatientById(string id)
    {
        var patient =  healthcareDbContext.Patients.FirstOrDefault(p => p.Id == id);
        return patient;
    }

    public void AddPatient(Patient patient)
    {
        healthcareDbContext.Patients.Add(patient);
        healthcareDbContext.SaveChanges();
    }

    public void EditPatient(Patient patient)
    {
        var patientToEdit = healthcareDbContext.Patients.FirstOrDefault(p => p.Id == patient.Id);

        if (patientToEdit != null)
        {
            patientToEdit.PatientName = patient.PatientName;
            patientToEdit.DateOfBirth = patient.DateOfBirth;
            patientToEdit.PatientRecord = patient.PatientRecord;

            healthcareDbContext.SaveChanges();
        }
        else
        {
            throw new ArgumentException("Patient not found");
        }
    }

    public void DeletePatient(string patientId)
    {
        var patientToDelete = healthcareDbContext.Patients.Find(patientId);

        if (patientToDelete != null)
        {
            healthcareDbContext.Patients.Remove(patientToDelete);
            healthcareDbContext.SaveChanges();
        }
        else
        {
            throw new ArgumentException("Patient not found");
        }
    }

    public List<Patient> SearchPatientsBySSNAsync(string searchSsn)
    {
        var patients = healthcareDbContext.Patients.Where(p => p.PatientRecord.SSN == searchSsn).ToList();

        return patients;
    }

    public List<Patient> SearchPatientsByDOBAsync(DateTime startDate, DateTime endDate)
    {
        var patients = healthcareDbContext.Patients.Where(p => p.DateOfBirth >= startDate && p.DateOfBirth <= endDate)
            .ToList();

        return patients;
    }
}