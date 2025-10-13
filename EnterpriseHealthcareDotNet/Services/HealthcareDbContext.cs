using EnterpriseHealthcareDotNet.Components.Pages;
using EnterpriseHealthcareDotNet.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace EnterpriseHealthcareDotNet.Services;

public class HealthcareDbContext(DbContextOptions<HealthcareDbContext> options, EncryptionKeys keys) : DbContext(options)
{
    public DbSet<Patient> Patients { get;  init; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToCollection("Patients");

            entity.Property(p => p.Id)
                .HasBsonRepresentation(BsonType.ObjectId);

            entity.Property(p => p.DateOfBirth)
                .IsEncryptedForRange(new DateTime(1900, 1, 1),
                    new DateTime(2100, 1, 1),
                    keys.DobKeyId);
          
            entity.OwnsOne(p => p.PatientRecord, pr =>
            {
                pr.Property(r => r.SSN).IsEncryptedForEquality(keys.SsnKeyId).HasElementName("sSN");

                // 👇 Configure the collection element name
                pr.OwnsMany(r => r.HealthConditions, hc =>
                {
                    hc.HasElementName("healthConditions"); // 👈 sets array field name

                    hc.Property(c => c.Name).HasElementName("name");
                    hc.Property(c => c.Status).HasElementName("status");
                    hc.Property(c => c.Date).HasElementName("date");
                });
            });
        });
    }
}

public record EncryptionKeys(Guid SsnKeyId, Guid DobKeyId);