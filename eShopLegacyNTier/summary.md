# eShopLegacyNTier — .NET Framework → .NET 10 Migration Summary

## Final Build Status

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Both projects build cleanly targeting .NET 10.

---

## Projects Migrated

| Project | From | To | SDK |
|---|---|---|---|
| eShopWCFService | .NET 4.6.1 (WCF/EF6) | net10.0 | Microsoft.NET.Sdk.Web |
| eShopWinForms | .NET 4.7 (WinForms/WCF client) | net10.0-windows | Microsoft.NET.Sdk |

---

## Changes Made

### Solution File (`eShopLegacyNTier.sln`)
- Added `eShopWinForms` project entry (it was referenced in configuration but missing as a declared project).

### eShopWCFService

**Project File (`eShopWCFService.csproj`)**
- Replaced legacy MSBuild-style `.csproj` with SDK-style targeting `net10.0`.
- Replaced EF6 (`EntityFramework 6.1.3`) with EF Core 10 (`Microsoft.EntityFrameworkCore.SqlServer 10.0.11`).
- Replaced `System.ServiceModel` framework assemblies with `CoreWCF.Http 1.9.1` / `CoreWCF.Primitives 1.9.1`.
- Removed `packages.config`, `System.Web.*`, `System.ServiceModel.*` framework references.
- `CatalogService.svc` retained as a non-compiled content item (no longer drives routing).

**Program.cs (new)**
- Created ASP.NET Core + CoreWCF host.
- Registers `EntityModel` (EF Core `DbContext`) via `AddDbContext<EntityModel>`.
- Configures CoreWCF service endpoint at `/CatalogService.svc` using `BasicHttpBinding`.
- Calls `db.Database.EnsureCreated()` + `CatalogDBInitializer.Seed(db)` on startup to preserve the original initializer behavior.
- Connection string sourced from environment variable `ConnectionString` (highest priority) → `appsettings.json` `ConnectionStrings:EntityModel` → hardcoded local default.

**appsettings.json (new)**
- Migrated connection string from `Web.config` `<connectionStrings>`.

**ICatalogService.cs**
- Replaced `using System.ServiceModel;` with `using CoreWCF;`.
- `[ServiceContract]` and `[OperationContract]` now resolve from the `CoreWCF` namespace.

**CatalogService.svc.cs**
- Replaced `using System.Data.Entity;` with `using Microsoft.EntityFrameworkCore;`.
- Replaced `using System.ServiceModel;` with `using CoreWCF;`.
- Removed `using System.ServiceModel.Web;`.
- Removed parameterless constructor (DI now injects `EntityModel` via constructor).

**EntityModel.cs**
- Replaced EF6 `DbContext` constructor taking connection string with `DbContextOptions<EntityModel>` constructor for DI injection.
- Removed `Database.SetInitializer(new CatalogDBInitializer())` — EF Core doesn't support this API.
- Updated `OnModelCreating` signature from `DbModelBuilder` to `ModelBuilder`.
- Moved `[Table("CatalogItemsStock")]` mapping to `ToTable()` fluent call.

**CatalogDBInitializer.cs**
- Replaced EF6 `CreateDatabaseIfNotExists<EntityModel>` base class pattern with a static `Seed(EntityModel context)` method.
- Seeding is guarded by `if (!context.CatalogTypes.Any())` to avoid re-seeding.

**CatalogConfiguration.cs**
- Removed `using System.Web;`.
- Simplified to expose only `ConnectionStringName` constant; full connection string logic moved to `Program.cs`.

**PreconfiguredData.cs**
- Removed `using System.Web;`.
- Added explicit `Id` values to `DiscountItem` seed records (required for deterministic seeding without identity columns).

**Model files** (CatalogItem.cs, CatalogBrand.cs, CatalogType.cs, CatalogItemsStock.cs, DiscountItem.cs)
- Removed `using System.Data.Entity.Spatial;` (namespace does not exist in EF Core).
- Removed `using System.Web;` from `DiscountItem.cs`.

**CatalogServiceClient.cs — Deleted**
- This file was dead code in the server project — a client-side WCF proxy stub that conflicted with the CoreWCF-attributed `ICatalogService` and had method signature mismatches. The WinForms project's `Reference.cs` provides the real client proxy.

---

### eShopWinForms

**Project File (`eShopWinForms.csproj`)**
- Replaced legacy `.csproj` with SDK-style targeting `net10.0-windows`.
- Added `<UseWindowsForms>true</UseWindowsForms>`.
- Added `<EnableWindowsTargeting>true</EnableWindowsTargeting>` for cross-compilation on Linux.
- Added `System.ServiceModel.Http 8.1.2` / `System.ServiceModel.Primitives 8.1.2` for WCF client.
- Added `Newtonsoft.Json 13.0.3` (updated from 6.0.4).
- Added `System.Configuration.ConfigurationManager 10.0.0` for `ApplicationSettingsBase` support.
- Explicitly excluded `Helpers/*.cs` files — these contain UWP API code (`Windows.Storage`, `Windows.UI.*`) copied from a UWP project. They were never compiled (not in the original project's `<Compile>` items) and have the wrong namespace (`eShop.UWP.Helpers`). SDK-style projects auto-include all `.cs` files, so explicit exclusion was needed.

**Connected Services/eShopServiceReference/Reference.cs**
- Removed config-file-based `ClientBase<T>` constructors (`string endpointConfigurationName`, `string + string`, `string + EndpointAddress`) that do not exist in `System.ServiceModel.Primitives` 8.x.
- Retained only the `(Binding, EndpointAddress)` constructor, which is the supported pattern on .NET 10.

**Program.cs**
- Updated to use explicit `new BasicHttpBinding()` + `new EndpointAddress("http://localhost:62314/CatalogService.svc")` when constructing `CatalogServiceClient` instead of the removed config-based parameterless constructor.
- Removed unused `using eShopWinForms.Views;`.
- Added `using System.ServiceModel;` for `BasicHttpBinding` / `EndpointAddress`.

---

## Package Version Summary

| Package | Version | Used by |
|---|---|---|
| CoreWCF.Http | 1.9.1 | eShopWCFService |
| CoreWCF.Primitives | 1.9.1 | eShopWCFService |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.11 | eShopWCFService |
| Microsoft.EntityFrameworkCore.Design | 10.0.11 | eShopWCFService |
| System.ServiceModel.Http | 8.1.2 | eShopWinForms |
| System.ServiceModel.Primitives | 8.1.2 | eShopWinForms |
| Newtonsoft.Json | 13.0.3 | eShopWinForms |
| System.Configuration.ConfigurationManager | 10.0.0 | eShopWinForms |

---

## Next Steps

- **Database migration**: The service now uses `EnsureCreated()` which creates the schema from the EF Core model directly. For production, consider creating a proper EF Core migration (`dotnet ef migrations add InitialCreate`) to support incremental schema changes.
- **WCF endpoint URL**: The service endpoint URL in `Program.cs` (`http://localhost:62314/CatalogService.svc`) and in the WinForms `Program.cs` should be moved to `appsettings.json` / `App.config` for environment-specific configuration.
- **eShopWinForms runtime on Linux**: The WinForms project builds successfully on Linux (cross-compilation) but cannot run on Linux at runtime — it requires Windows. Deploy and run on Windows only.
- **CoreWCF metadata (WSDL)**: Verify that `serviceMetadataBehavior.HttpGetEnabled = true` correctly exposes the WSDL at `http://localhost:62314/CatalogService.svc?wsdl` after the CoreWCF service starts.
- **App.config in eShopWinForms**: The old `App.config` contained a `<system.serviceModel>` client endpoint entry that is no longer needed (the WCF client is now configured in code). The file still exists but its `<system.serviceModel>` section is effectively ignored; it can be cleaned up or removed.
