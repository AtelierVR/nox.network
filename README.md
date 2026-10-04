# nox.network — asset pipeline

`nox.network` also hosts the **generic asset pipeline client** shared by every asset type
(avatars, worlds, mods, props, …). It drives the same routes for all of them; only the
`AssetEndpoint` changes.

The server side is described in `docs/UPLOAD_PIPELINE.md` of the Nox node repository.

## Layers

| Layer | Assembly | Content |
|-------|----------|---------|
| `SDK/Assets` | `Nox.Network` | `IAssetsAPI`, `IAsset`, `IAssetRelease`, `IAssetFile`, `IAssetStatus`, request DTOs, `IAssetCache`, `IAssetDownload` |
| `CCK/Assets` | `Nox.CCK.Network` | Serializable models, JSON converters, `AssetRoutes`, `AssetHash`, request bodies |
| `Runtime/Assets` | `Nox.Network.Runtime` | `AssetsClient` (`IAssetsAPI` implementation) and the content-addressed `AssetCache` |

Other mods resolve the API through the mod loader:

```csharp
var assets = coreAPI.ModAPI.GetMod("network")?.GetInstance<IAssetsAPI>();
```

## Endpoints

An asset type is described by its logical type and its REST collection:

```csharp
public static readonly AssetEndpoint Avatars = new AssetEndpoint("avatar", "avatars");
public static readonly AssetEndpoint Mods    = new AssetEndpoint("mod", "mods");
```

`server` is a node address (`name@server` host); passing `null` falls back to the current
`server` config entry. The session token stored in `servers.<address>._token` is attached to
every request automatically.

## Download

Downloads are content-addressed: the cache is keyed by the file hash, so the same bundle is
only fetched once, and concurrent requests for the same URL share a single transfer.

```csharp
// GET …/mods/{id}/releases/{release}/files/{name}, verified against the slot hash
var path = await assets.Download(server, Mods, "my-mod", "v1.0.0", "my-mod-windows.bundle", (progress, bytes) => { });

// Any URL (thumbnail, image, …) can use the same cache
var cover = await assets.DownloadUrl(imageUrl);

var slot = await assets.FetchFile(server, Mods, "my-mod", "v1.0.0", "my-mod-windows.bundle");
var hash = slot.Hashs[0];             // "sha256:…" reported by the API
assets.Cache.Has(hash);               // already downloaded? (algorithm + digest)
assets.Cache.PathFor(hash);           // local path
assets.Cache.Clear(hash);             // drop one entry
```

Lower-level control (cancellation, progress event, de-duplication) is available through
`IAssetCache.Download(...)`, which returns an `IAssetDownload`.

## Conventions

| Value | Type | Notes |
|-------|------|-------|
| `owner`, `contributors`, `uploader` | `Identifier` (`Nox.CCK.Utils`) | parsed from the `id@server` / `name@server` aliases through `StringToIdentifierConverter`; `Identifier.Invalid` when absent |
| `title`, `description`, `changelog` | `TranslatedString` (`Nox.CCK.Convertors`) | accepts a plain string or a `{ lang: text }` object; a single-entry value is written back as a plain string, which is what the API expects |
| `hashs`, `hash` | `IAssetHash` (`AssetHash`) | the `<algorithm>:<hex>` form is kept as-is: `Type` (`sha256`), `Digest` (`byte[]`, decoded through `HexaToBytes`, with a `Base64ToBytes` fallback), `Hex` (lowercase) and `Value` (what goes in `Content-Hash`); `AssetHash.Parse`, `ToFileName`, `Has` and `Matches` bridge to the cache |
| `attributes`, `alias` | `IAssetAttribute[]` | `{ key, value }` pairs, not Nox tags |
| `url` | `string` | with `?full=1` the API sends `{ proxified, original }`: the proxified URL is exposed |
| `status` | `IAssetStatus` | the API's `status` as `AssetState` (`Queued`, `Processing`, `Completed`, `Failed`) plus `progress` and `message`; poll while `refetch_at` is set |
| `channel` | `AssetChannel` | the API's `stable`, `candidate` or `nightly` |
| timestamps | `DateTime` | epoch milliseconds (decoded by `UnixTimestampToDateTime`); an omitted field reads as `default` (`DateTime.MinValue`) — no nullable dates |
| `release.Value` / `release.Resolved` | `int` | `-1` is the "no release" sentinel; `Auto` tells which one to use. `Resolved` is the release **id**, so `FetchRelease` it to read its name, files and channel |

### Selecting a file

A release holds one file per variant, discriminated by its `attributes` (`platform`, `arch`,
`engine` as `<engine>[:<version>]`). Two helpers pick the right one, mirroring `BestImage`:

```csharp
var release = await assets.FetchPreferredRelease(server, Worlds, "42");

// Windows / Unity 6000.4 variant, rejecting the other platforms and engine versions
var bundle = release.BestFile(PlatformExtensions.CurrentPlatform, EngineExtensions.CurrentEngine);

bundle?.Url;        // download URL (proxified with ?full=1)
bundle.CacheKey();  // "sha256-<hex>", the cache file name
bundle.Attribute("arch");
```

`BestFile` only rejects a candidate on a declared attribute: a file without `platform`/`engine`
stays usable as a fallback, and the most specific match wins (platform and engine declared, then
the one carrying an engine version).

## Upload

```csharp
// 1. create the asset                      PUT /mods
var asset = await assets.Create(
	server, Mods,
	new AssetCreateRequest { Name = "my-mod", Title = new TranslatedString { ["en"] = "My Mod" } }
);

// 2. create the release                    PUT /mods/{id}/releases
var release = await assets.CreateRelease(server, Mods, asset.Name, new AssetReleaseRequest { Name = "v1.0.0" });

// 3 + 4. reserve a slot                     POST …/releases/{rid}/files
//        then stream the bytes             PUT {slot.Url}
await assets.Upload(
	server, Mods, asset.Name, release.Name, "my-mod-windows.bundle",
	new AssetFileReservation {
		Name       = "my-mod-windows.bundle",
		Mime       = "application/octet-stream",
		Attributes = new[] {
			new AssetAttribute("platform", "windows"),
			new AssetAttribute("engine", "unity:6000.4")
		}
	},
	progress: (value, bytes) => { }
);
```

`UploadFile` and `ReserveFile` are exposed separately when the two steps must be controlled
independently (retrying a failed slot re-`PUT`s the same `fileId`). Since the `PUT` analyzes,
validates and completes the file synchronously, the returned `IAssetFile` is already
`completed` — or `failed`, in which case the slot is kept for a retry.

`AssetUploadOptions` forwards the optional integrity headers: `Content-Hash` (computed with
`Hashing.HashFile(AssetHash.Sha256, path)` when omitted), `Content-Signature` and a slot
name/`Content-Type` override.

## Search and other operations

```csharp
var page = await assets.Search(server, Mods, new AssetSearchRequest { Query = "bunny", Limit = 20 });
foreach (var item in page.Items) { /* item.Id, item.Name, item.Release, item.Images … */ }

var next = await page.Next();                   // the client wires the navigation
var asset = await assets.Fetch(server, Mods, "my-mod", full: true);
var preferred = await assets.FetchPreferredRelease(server, Mods, "my-mod");
var slot = await assets.FetchFile(server, Mods, "my-mod", "v1.0.0", "my-mod-windows.bundle");
```

The collection route accepts `query`, `offset`, `limit` (`1`–`100`, default `20`) and `full=1`
only; `Full = true` expands the `url` and release preference of the items.

### Typed collections

Search, fetch, create and update are generic: pass the model of the asset type to deserialize
the items and the reply as that model instead of the generic `Asset` (a typed model adds what
the API returns for that type, e.g. a world's `capacity`).

```csharp
var world = await assets.Fetch<World>(server, Worlds, "42");
var page  = await assets.Search<AssetSearchResponse<World>>(server, Worlds, new AssetSearchRequest { Limit = 20 });
var item  = page.Items[0];   // World, not Asset
```

A type is free to rely on its own release naming: worlds name each release after the world
version it publishes (`"1"`, `"2"`, …), so a world identifier pinned with `?v=2` resolves to the
release named `2` — the release routes accept a name wherever they accept an id.

## Statuses and polling

`IAssetStatus` normalizes both wire shapes (`"completed"` and
`{ "status": "queued", "progress": 0, "message": "…" }`) into a `Status` (`AssetState`),
`Progress` and `Message` — an unknown or omitted value reads as `AssetState.Unknown`.

- `IAssetFile.RefetchAt` / `IAssetRelease.RefetchAt` are set while the resource is pending and
  omitted once it is stable: poll again at or after that timestamp, and stop when it is unset.
- `IAssetFile.Url` points at the upload endpoint while the slot is pending and at the
  download endpoint once the file is `completed`.
