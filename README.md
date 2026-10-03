# Enterprise Healthcare Dotnet

This Blazor application is intended to be used to demonstrate features in the MongoDB C# Driver that are often of interest to enterprise companies.

- `with-efcore` - This branch is configured to use EF Core and the MongoDB EF Core Provider

In order to run this, you will need to provide your MongoDB Connection String. Keep it out of source control with user secrets (from the `EnterpriseHealthcareDotNet` folder):

```bash
dotnet user-secrets set MongoDBConnectionString "<YOUR MONGODB CONNECTION STRING>"
```

You will also need to set `CryptSharedLibPath` in `appsettings.json` for Queryable Encryption.

```bash
dotnet run
```

## Brand styling and fonts

The UI follows the MongoDB brand refresh (dark Slate theme, Spring Green accents). The brand typefaces (Söhne, Söhne Mono and MongoDB Value Serif) are licensed, so they are **not included** in this repository and the app falls back to system fonts out of the box.

If you are licensed to use them (for example, MongoDB employees via the brand portal), copy these files into `EnterpriseHealthcareDotNet/wwwroot/fonts/`. They are gitignored, so they will not be committed:

- `Sohne-Regular.ttf`
- `SohneCondensed-Bold.ttf`
- `SohneMono-Medium.ttf`
- `MongoDBValueSerif-Regular.otf`
