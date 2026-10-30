# History

## 3.0.1

- 2026-10-30:
  - search: the bounding box can cross the antimeridian (its SW corner longitude is then greater than the NE one, as in GeoJSON), rather than being rejected. Added a polygonal `region` filter (`region`, `containedInRegion`), whose longitudes can exceed 180 to cross the antimeridian; regions are made valid, and both boxes and regions are split at the antimeridian with `ST_WrapX` when needed. Region parsing and validation are in `SpatialRegion`.
  - search: equality tokens (`=` or no operator) which are stopwords or digits only are dropped, like when indexing. Before, any text including e.g. `of` or `the` (like a full place title) matched nothing when all the tokens were required.
  - README: documented stopwords in the text syntax; fixed the temples example, as Pleiades has both `temple` and `temple-2` place types.
  - unified versions.
  - updated Docker compose.

## 3.0.0

- 2026-10-29:
  - API: string values missing in the data (often empty strings in the source) are returned as `null`; the place bounding box property is `bbox`.
  - index: the rank is reduced for entities whose review state is not `published` (rather than only `pending`, which is no longer used by Pleiades).
  - CLI: the log level is set by `LogLevel` in `appsettings.json` (default `Information`, also in debug builds, where the indexer logged each token); fixed old logs not deleted in debug builds; the interactive `query` command no longer loops forever without a console.
  - README: added the full API reference.
- 2026-10-29: revision for Embix 5 and Fusi.Api.Auth 7.1 (breaking changes):
  - **Embix 5**: SqlKata dropped. The index builder factory writes via `PgSqlIndexWriter` (`COPY`); the metadata supplier takes an `IDbConnectionFactory`; the CLI `index` command drops secondary indexes before the bulk load and recreates them at its end. The profile data queries no longer page records, and location years are indexed too (unknown 0-0 years as NULL). ⚠️ Rebuild the index with `pleitool index ... -c`.
  - **search**: rewritten with parameterized SQL (`QuickSearchBuilder` takes a `SqlDialect` and returns a `PagedSqlQuery`); `IQuickSearch` is async, and adds `GetPlaceAsync` and `GetNearbyAsync`. Fixed: invalid SQL from mismatched subquery aliases; duplicate results for places with more than one type, and places without type excluded; type and spatial filters repeated for each token rather than applied once; tokens empty after filtering matching any place; BC years ignored (`YearMin`/`YearMax` filters were applied only when positive); year filter now matches overlapping ranges; distances computed without index (now `ST_DWithin` on geography); the bounding box was built by string concatenation and `IsBBoxContainer` was ignored (now `ST_Intersects`/`ST_Covers` on the place's bounding box); unlocated places returned 0,0 coordinates (now null). Added: multiple place types, sort by distance (KNN), distance in results.
  - **schema and performance**: added indexes on all the foreign keys, `place_meta`, `place.title`, GiST indexes on `place.geo_pt`, `place.geo_bbx::geometry`, `location.geo`; Pleiades index tables with a covering occurrence index including rank and years, and trigram/pattern token indexes. Only the `postgis` and `pg_trgm` extensions are created (raster, topology and sfcgal were unused). `pleitool pop-spatial` adds missing indexes to existing databases.
  - **import**: fixed `place_attestations` table name; links (`connectsWith` and connections) to places read before the source place were never resolved, and every other target URI was skipped; duplicate place links; `E` import flag meaning connections rather than creators; EF change tracker growing with the whole import. `pop-spatial` no longer maps unlocated places to 0,0, and can be rerun.
  - **API**: auth aligned with Fusi.Api.Auth 7.1 (`AddJwtBearerAuthentication`, Identity lockout and unique email, async seeding retry, secrets masked in environment dump); added the auth, account and user controllers; added `api/places/{id}` and `api/places/{id}/nearby`. Fixed: search connection string read from a missing setting; database created twice at first start, with a broken index script; paging defaults failing validation; rate limiter applied before authentication and sending an email for each rejected request; exception handler pointing to a missing `/Error` page. Added validation of search parameters, response caching, problem details, and XML documentation for OpenAPI.
- 2026-10-29: upgraded to NET 10.
- 2025-02-04: updated packages and auth code.

## 2.0.0

- 2024-11-30: ⚠️ upgraded to NET 9.

## 1.0.0

- 2024-06-13: upgraded to NET 8.
- 2023-01-12:
  - upgraded to NET 7.
  - refactored CLI to use [Spectre Console](https://spectreconsole.net)
  - updated packages.
- 2022-09-18: updated packages.
- 2021-11-09: upgraded to NET 6.
