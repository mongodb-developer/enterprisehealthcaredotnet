# Enterprise Healthcare Dotnet

This Blazor application is intended to be used to demonstrate features in the MongoDB C# Driver that are often of interest to enterprise companies.

This `main` branch contains a basic healthcare application that allows you to create, read, update and delete patients as well as search by SSN or by ranges of date of birth. This is intended to be the starting point for many future tutorials on adding specific features.

There are also other branches available to demo other specific features:

- `with-queryable-encryption` - This branch is configured to use Queryable Encryption, a feature unique to MongoDB that encrypts your data both in transit and at rest!
- `with-change-streams` - This branch shows how to implement change streams with the MongoDB C# driver.

Further information on how to run it can be found on each branch as the requirements can differ.

## Brand styling and fonts

The UI follows the MongoDB brand refresh (dark Slate theme, Spring Green accents). The brand typefaces (Söhne, Söhne Mono and MongoDB Value Serif) are licensed, so they are **not included** in this repository and the app falls back to system fonts out of the box.

If you are licensed to use them (for example, MongoDB employees via the brand portal), copy these files into `EnterpriseHealthcareDotNet/wwwroot/fonts/`. They are gitignored, so they will not be committed:

- `Sohne-Regular.ttf`
- `SohneCondensed-Bold.ttf`
- `SohneMono-Medium.ttf`
- `MongoDBValueSerif-Regular.otf`
