# Enterprise Healthcare Dotnet

This Blazor application is intended to be used to demonstrate features in the MongoDB C# Driver that are often of interest to enterprise companies.

This `with-queryable-encryption` branch is configured to use Queryable Encryption, a feature unique to MongoDB that encrypts your data both in transit and at rest!

## Running the application

In order to run this application, you will need to follow a few steps:

1. Download the [MonggoCryptSharedLibrary](https://www.mongodb.com/try/download/enterprise). From the link, pick **crypt_shared** under the package dropdown for your platform.
2. Extract the downloaded file into the root of your cloned solution.
3. Copy the path to the .dylib or .dll file (depending on MacOS or Windows) and add to appsettings.json and appsettings.Development.json in place of the placeholder values currently there.
4. Add your MongoDB Connection string in the files as well while there.

```bash
dotnet run
```