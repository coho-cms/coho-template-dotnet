# coho-template-dotnet

The starting point for your content library: a .NET library, generated from your Coho CMS project's content types, that your sites and services reference. Each content type becomes a record in its own file, and the library's client returns them.

**In testing.** The `CohoCms` packages this depends on are alphas on NuGet.

It is a skeleton. `coho-ir.json` is an empty placeholder type model, and `src/YourOrg.Content/Coho/Coho.cs` is what it generates: no content types, only the untyped reads, so the template builds as it stands. Generating yours in step 4 replaces both, with your project's type model and a file per content type.

## Set up

1. **Create the library** with `dotnet new`, which names it for you:

   ```bash
   dotnet new install CohoCms.Templates::0.1.0-alpha.3
   dotnet new coho-library -n Acme.Content
   cd Acme.Content
   ```

   Or create your repository from this template on GitHub, and rename `YourOrg.Content` everywhere it appears: folder and file names, the solution, `coho.config.json`'s `outDir` and `namespace`, and the code.

2. **Point it at your project** with a project token with read access. Coho says which account and project the token is for, so there are no ids to copy, and the token goes in `.env`, which is in `.gitignore`:

   ```bash
   coho token create --role viewer --label "acme-content library"
   dotnet tool restore
   dotnet coho-codegen init --url https://app.coho.example --ref v1.4.0
   ```

   For CI, set the same token as the `COHO_TOKEN` secret.

3. **Generate your types**, and commit the type model and the generated code:

   ```bash
   dotnet coho-codegen pull
   dotnet coho-codegen generate
   dotnet build
   ```

4. **Choose where it is published.** The release workflow publishes to GitHub Packages; change the `--source` in `.github/workflows/release.yml` for another registry. Then remove `<IsPackable>false</IsPackable>` from the library's project file, which is there so nothing is published by accident.

## See what's there

The catalog lists every content type and how many entries it has, read live from delivery with a delivery key, the way an app reads it. Add these to `.env`:

```
COHO_DELIVERY_URL=https://delivery.coho.example
COHO_DELIVERY_KEY=<a delivery key for the project>
COHO_REF=qa
```

`COHO_REF` is the tag or environment to read; leave it empty to read the ref in `coho.config.json`. A key made for one environment reads only that one. Then, from the repository's root:

```bash
dotnet run --project tools/Catalog
```

```
Content on qa:

  blogPost     Blog post         2
  tag          Tag               3
  teamMember   Team member       2

  7 entries in 3 types
```

To see what is on `dev` or a branch, before it is released, add `--preview`. It reads live through Coho with the `COHO_TOKEN` you pull with, and needs no delivery key:

```bash
dotnet run --project tools/Catalog -- --preview
dotnet run --project tools/Catalog -- --preview --ref feature/spring-campaign
```

Its code is in `tools/Catalog`, an example of reading content with the generated types. It is a development tool: it is never packed or published, so it is not part of the library.

## When the content types change

Cut a tag in Coho, then:

```bash
dotnet coho-codegen pull --ref v1.4.0
dotnet coho-codegen generate
```

Review and commit the diff. CI's `check` fails whenever the generated code doesn't match the committed type model.

To release, run the **Release** workflow with the Coho tag and the version to publish. It pulls that tag's types, generates, tests and publishes.

Your own reads go in `src/YourOrg.Content/CohoReads.cs`, built on the generated ones. `Coho/` is regenerated and never edited, so a schema change that breaks one of your reads fails this library's build, not an app.

## Using the library in an app

For a project with a `blogPost` type, in an ASP.NET Core app:

```bash
dotnet add package Acme.Content
```

```csharp
using Acme.Content;

builder.Services.AddCoho(); // from the COHO_* values in the app's configuration

// in a page or endpoint, given a Coho:
var home = await coho.HomepageAsync();
var latest = await coho.BlogPost.ListAsync(new() { Order = BlogPost.SortBy.PublishedAt.Descending, Limit = 3, Depth = 1 });
```

```
# Released content, in production: set as the app's environment or user secrets.
COHO_DELIVERY_URL=https://delivery.coho.example
COHO_DELIVERY_KEY=<a delivery key>
COHO_PROJECT=0192…
COHO_REF=prod
COHO_LOCALE=en-US
```

Without a host, pass a config to `new Coho(...)` instead, or build one from the environment with `CohoEnvironment.FromEnvironment()`, from `CohoCms.Delivery`.

To show asset fields in Razor Pages, MVC views or Blazor, add [`CohoCms.Razor`](https://github.com/coho-cms/coho-razor): `<coho-image src="@post.Hero" width="800" />` renders an image resized at the edge, and `<coho-asset-link asset="@post.Brochure" />` a link to download a file.

### Previews

To show `dev` or a branch before it is released, such as on a developer's machine or a preview deployment, read it with a project token instead of a delivery key. Only the configuration changes:

```
COHO_URL=https://api.coho.example
COHO_ACCOUNT=0191…
COHO_PROJECT=0192…
COHO_PREVIEW_TOKEN=coho_pt_…
COHO_REF=dev
```

The answers are the same shapes, so the app's code doesn't change. Setting a preview token and a delivery key together is refused.

## Three credentials, kept apart

| | Who holds it | For |
|---|---|---|
| `COHO_TOKEN` | This library's CI, and you when you pull | Reading the content types. Never deployed |
| A delivery key | Each app, on the server | Reading released content at runtime |
| A preview token | An app, on the server, wherever it previews | Reading unreleased content. A project token with the viewer role: `coho token create --role viewer` |

The library reads no configuration of its own, so a key or token is only ever where the app runs. Keep them on the server: one in a Blazor WebAssembly app is one anyone can read, and a preview token reads everything unreleased. The client refuses to start with a token in a browser.

## Development

The .NET 10 SDK. The library targets `net8.0` and `net10.0`.

```bash
dotnet build
dotnet test
dotnet format
```

`dotnet format --verify-no-changes` is what CI checks: Allman braces, an opening brace on its own line, and four-space indents. The generated `Coho/` is marked as generated, so analyzers and the formatter leave it alone; codegen writes it in the same style.

## License

Apache-2.0
