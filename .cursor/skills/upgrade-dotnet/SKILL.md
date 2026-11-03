---
name: upgrade-dotnet
description: Upgrade nopCommerce to a newer .NET SDK/runtime (TFM, packages, Docker, CI, ClearPluginAssemblies). Use when the user asks to upgrade .NET, retarget netX.0, bump the SDK in global.json, or move to the latest .NET version.
---

# Upgrade nopCommerce .NET

Project-local playbook for moving this repo to a newer .NET major/minor. Agents read this cold mid-task. Follow Assess → Install → Retarget → Packages → Infra → Build/fix → Verify → Ship.

Do **not** guess the “latest” version from memory. Resolve it from Microsoft release metadata (below). Prefer GA/LTS when the user says “stable”; otherwise take the newest published channel (including go-live RC when that is what NuGet/MCR expose).

## Assess

1. Read `global.json`, `src/Directory.Build.props`, `Dockerfile`, `.github/workflows/dotnet.yml`.
2. Inventory TFMs: `rg -n 'net[0-9]+\.0' --glob '*.{csproj,props,proj,json,txt}'`.
3. Inventory framework-tied packages (versions must track the runtime):
   - `Microsoft.AspNetCore.*`
   - `Microsoft.Extensions.Caching.*`
   - `Microsoft.Data.Sqlite`
   - `System.Configuration.ConfigurationManager`
   - `Microsoft.AspNetCore.Authentication.Facebook`
4. Resolve the target channel:

```bash
curl -sL "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/<MAJOR.MINOR>/releases.json" \
  | python3 -c 'import sys,json;d=json.load(sys.stdin);print(d["latest-sdk"], d["latest-runtime"])'
# Confirm ASP.NET package versions exist on NuGet (example):
# Microsoft.AspNetCore.Mvc.NewtonsoftJson → same version as the runtime build.
```

5. Create branch `cursor/upgrade-dotnet-<major>-2329` (or the run’s branch suffix) off the preferred base.

Record target SDK, runtime, TFM (`netN.0`), and whether prerelease is allowed.

## Install SDK

Install into `$HOME/.dotnet` so the system image SDK is not required:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --version <SDK_VERSION> --install-dir "$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet --list-sdks
```

For RCs, pin the exact SDK in `global.json` and set `"allowPrerelease": true`. For GA, pin the feature band and keep `"allowPrerelease": false` unless the team wants otherwise.

## Retarget

Update every TFM touchpoint (full list in [reference.md](reference.md)):

1. `src/Directory.Build.props` → `<TargetFramework>netN.0</TargetFramework>` (libraries inherit this).
2. Every plugin `*.csproj` and matching `Notes.txt` that hard-code TFM.
3. `src/Build/ClearPluginAssemblies.proj`, `src/Build/src/ClearPluginAssemblies/ClearPluginAssemblies.csproj`.
4. Rebuild the checked-in helper and refresh runtimeconfig:

```bash
dotnet build src/Build/src/ClearPluginAssemblies/ClearPluginAssemblies.csproj -c Release
cp -f src/Build/src/ClearPluginAssemblies/bin/Release/netN.0/ClearPluginAssemblies.dll src/Build/
cp -f src/Build/src/ClearPluginAssemblies/bin/Release/netN.0/ClearPluginAssemblies.runtimeconfig.json src/Build/
```

If the runtime is still an RC, `ClearPluginAssemblies.runtimeconfig.json` must name the **exact** framework version (e.g. `11.0.0-rc.3.…`), not `N.0.0`. Otherwise `dotnet ClearPluginAssemblies.dll` exits 150 and plugin post-build cleanup fails.

Do **not** bump unrelated packages whose versions only look like TFMs (e.g. Mapster `10.0.12`).

## Packages

Bump only framework-aligned `PackageReference` versions to the target runtime build. Leave third-party packages alone unless restore/build demands it.

Remove `PackageReference`s that the new SDK flags as unnecessary framework surface (e.g. NU1510 for in-box packages) **only after** confirming call sites still compile.

## Infra

| File | What to change |
|---|---|
| `global.json` | SDK version, `rollForward`, `allowPrerelease` |
| `Dockerfile` | `mcr.microsoft.com/dotnet/sdk:N.0-alpine` and `aspnet:N.0-alpine` (confirm tags on MCR) |
| `.github/workflows/dotnet.yml` | `dotnet-version`; for RCs also `dotnet-quality: preview` |
| `README.md` | “runs on .NET N” blurb |

## Build / Fix

```bash
dotnet build src/NopCommerce.sln -c Release
dotnet test src/Tests/Nop.Tests/Nop.Tests.csproj -c Release --no-build
```

Treat new failures as upgrade fallout until proven otherwise. Common .NET 11+ trap: correctly-rounded `double`↔`decimal` conversions break NUnit `[TestCase(12.37, …)]` feeding `decimal` parameters, and `Convert.ToDecimal(Math.Pow(…))` shipping cube-root dimensions. Fixes:

- Bind decimal cases with **string** TestCase args: `[TestCase("12.37", "12.40", …)]`.
- When converting double→decimal for display/shipping math, round with `G15` significant digits (see `ShippingService.ConvertDoubleToDecimal` pattern) rather than raw `Convert.ToDecimal(double)`.

Read Microsoft compatibility docs for the target major before inventing workarounds. Details: [reference.md](reference.md).

## Verify

After green unit tests, prove the storefront with the sibling skill:

```bash
# Follow .cursor/skills/verify-nopcommerce/SKILL.md
# launch → install → search MacBook → admin-login → evidence under artifacts/
```

Minimum proof: isolated launch on a free port, sample-data install, homepage chrome, one catalog search, admin login. Copy screenshots to `/opt/cursor/artifacts/` for the PR.

## Ship

1. Logical commits (TFM/infra first; behavioral fixes second).
2. Push branch; open/update PR against the preferred base (`develop`).
3. PR body: target SDK/runtime, RC vs GA note, build/test counts, verify-nopcommerce run id + screenshots.

## Done checklist

```
- [ ] Target SDK/runtime resolved from release-metadata (not memory)
- [ ] TFM updated in Directory.Build.props + all hard-coded plugin/build paths
- [ ] ClearPluginAssemblies.dll + runtimeconfig refreshed and runnable
- [ ] Framework-tied NuGet packages aligned; unrelated packages left alone
- [ ] global.json / Docker / CI / README updated
- [ ] Release build + Nop.Tests green
- [ ] verify-nopcommerce smoke proof attached
```
