# Enterprise Healthcare Dotnet

This Blazor application is intended to be used to demonstrate features in the MongoDB C# Driver that are often of interest to enterprise companies.

This `main` branch is a work in progress and is being slowly built up to act as a starting point for future content. There are other branches available to showcase specific features:

- `with-queryable-encryption` - This branch is configured to use Queryable Encryption, a feature unique to MongoDB that encrypts your data both in transit and at rest!

Further information on how to run it can be found on each branch as the requirements can differ.

## Brand styling and fonts

The UI follows the MongoDB brand refresh (dark Slate theme, Spring Green accents). The brand typefaces (Söhne, Söhne Mono and MongoDB Value Serif) are licensed, so they are **not included** in this repository and the app falls back to system fonts out of the box.

If you are licensed to use them (for example, MongoDB employees via the brand portal), copy these files into `EnterpriseHealthcareDotNet/wwwroot/fonts/`. They are gitignored, so they will not be committed:

- `Sohne-Regular.ttf`
- `SohneCondensed-Bold.ttf`
- `SohneMono-Medium.ttf`
- `MongoDBValueSerif-Regular.otf`
