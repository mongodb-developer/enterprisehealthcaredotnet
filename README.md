# Enterprise Healthcare Dotnet

This Blazor application is intended to be used to demonstrate features in the MongoDB C# Driver that are often of interest to enterprise companies.

This `with-oidc` branch shows how to use Azure EntraID alongside Workforce Federation in Atlas, to allow enterprise authentication to manage access to the database.

## Running the application

In order to run this application, you will need a few things in place:

1. Azure EntraID setup and configured for your tenant
2. MongoDB Atlas configured for OpenID Connect (OIDC)[https://www.mongodb.com/docs/atlas/workforce-oidc/#std-label-oidc-authentication-workforce]

```bash
dotnet run
```

3. This will then ask you to login with your Microsoft account for the tenant that you configured for EntraID.

**Note:** This application uses in-memory cache so will reset the session between application runs. For this reason, ensure you clear cookies in your browser between runs to avoid a session mismatch and an MSAL error appearing. In production, you can set up a distributed cache to handle this instead.
