# Upgrade .NET — reference

## File inventory (TFM / SDK)

| Path | Role |
|---|---|
| `global.json` | SDK pin |
| `src/Directory.Build.props` | Default TFM for libraries under `src/` |
| `src/Plugins/**/*.csproj` | Explicit plugin TFM (does not inherit alone—keep in sync) |
| `src/Plugins/**/Notes.txt` | Template snippets showing `<TargetFramework>` |
| `src/Build/ClearPluginAssemblies.proj` | MSBuild wrapper that execs the cleaner |
| `src/Build/ClearPluginAssemblies.dll` | Checked-in tool (rebuild on TFM change) |
| `src/Build/ClearPluginAssemblies.runtimeconfig.json` | Must match installed runtime (exact RC version when prerelease) |
| `src/Build/src/ClearPluginAssemblies/ClearPluginAssemblies.csproj` | Source for the cleaner |
| `Dockerfile` | `sdk:N.0-alpine` / `aspnet:N.0-alpine` |
| `.github/workflows/dotnet.yml` | `actions/setup-dotnet` version (+ `dotnet-quality` for RC) |
| `README.md` | User-facing “runs on .NET N” |

Bulk TFM replace (review diff after):

```bash
find . -type f \( -name '*.csproj' -o -name '*.props' -o -name '*.proj' -o -name 'Notes.txt' -o -name '*.runtimeconfig.json' \) \
  ! -path './.git/*' -print0 \
  | xargs -0 grep -l 'netOLD\.0' \
  | while read -r f; do sed -i 's/netOLD\.0/netNEW.0/g' "$f"; done
```

## Framework-tied packages

Bump these together with the runtime (NuGet version == runtime build id for Microsoft.AspNetCore.* / Microsoft.Extensions.*):

- `Microsoft.AspNetCore.Mvc.NewtonsoftJson` (`Nop.Core`)
- `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` (`Nop.Core`)
- `Microsoft.Extensions.Caching.StackExchangeRedis` (`Nop.Core`)
- `Microsoft.Extensions.Caching.SqlServer` (`Nop.Core`)
- `System.Configuration.ConfigurationManager` (`Nop.Data`)
- `Microsoft.Data.Sqlite` (`Nop.Tests`)
- `Microsoft.AspNetCore.Authentication.Facebook` (ExternalAuth.Facebook plugin)

Leave alone unless restore forces a move: Mapster, FluentMigrator, linq2db, Npgsql, SkiaSharp, Autofac, etc.

## Resolving “latest”

```bash
# Channel metadata
curl -sL "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/11.0/releases.json" | head

# MCR tag presence
curl -sL "https://mcr.microsoft.com/v2/dotnet/sdk/tags/list" \
  | python3 -c 'import sys,json; print("11.0-alpine" in set(json.load(sys.stdin)["tags"]))'
```

RC installs need the exact version string from `releases.json` (`latest-sdk`). Floating Docker tags like `11.0-alpine` may already point at RC while GA is pending—call that out in the PR.

## .NET 11+ floating-point / decimal pitfalls

Docs: [Decimal and BigInteger floating-point conversions are correctly rounded](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/11/decimal-biginteger-floating-point-conversions).

Symptoms seen in this repo:

1. **NUnit `TestCase` doubles → `decimal` params**  
   Expected becomes `12.369999…M` while production `Math.Round` returns `12.37M`.  
   Fix: string literals in attributes so NUnit parses exact decimals.

2. **Shipping cube-root dimensions** (`ShippingService.GetDimensionsAsync`)  
   `Convert.ToDecimal(Math.Pow(volume, 1/3))` can yield `3.999…` instead of `4`.  
   Fix: format double with `G15` then `decimal.Parse` (significant-digit rounding that matches pre-.NET 11 behavior).

3. **ClearPluginAssemblies on RC**  
   runtimeconfig `framework.version` of `N.0.0` will not roll forward onto `N.0.0-rc.*`. Pin the RC framework version or the post-build plugin cleanup fails with exit 150.

## Verify hook

After unit tests: `.cursor/skills/verify-nopcommerce/SKILL.md` — isolated PostgreSQL instance, not the environment `start` listener on `:5000`.
