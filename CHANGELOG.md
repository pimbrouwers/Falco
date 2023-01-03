> Work in progress!

# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Removed

- Unused types `HttpContextAccessor` and `AsyncHttpContextAccessor`.

## [4.0.3] - 2023-01-01

### Added

- Working tutorial sample.
- Documentation website generator `/site`, and output `/docs`.

### Removed

- Internal utility functions `httpPipe` and `httpPipeTask`. See issue #94, #95.

## [4.0.2] - 2022-11-30

### Fixed

- NuGet package metadata, invalid readme.

### Changed

- Hello world sample to use ASP.NET static file middleware.
- Spelling and grammar of comments. See #96.

### Removed

- Unused internal function `String.parseInt`.

## [4.0.1] - 2022-11-23

### Added

- `Response.debugRequest`, which pretty prints the content of the current request to the screen.
- Related community projects and libraries to README.md.

### Fixed

- NuGet package metadata, invalid icon path.

## [4.0.0] - 2022-11-07

The project no longer intends to support anything prior to net6.0, which enables the built-in `task {}` computation expression.

### Added

- `StringCollectionReader.GetChildren`, safely retrieves a collection of readers. Intended to be used with the "dot notation" collection wire format (i.e., Person.First=John&Person.Last=Doe&Person.First=Jane&Person.Last=Doe).
- `MultipartReader.StreamSectionsAsync` for async streaming of multipart/form-data, following MSFT [spec](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads).
- `Services.inject` helpers, for CPS-style dependency injection, supporting up to five generic input types.
- `in_memory`, `required_ini`, `optional_ini`, `required_xml`, `optional_xml` custom operations added to the configuration builder.

### Changed

- `StringCollectionReader` abstract attribute removed, to support nested readers.
- `StringCollectionReader.Get{String|StringNonEmpty|Int16|Int32|Int|Int64|Boolean|Float|Decimal|DateTime|DateTimeOffset|Guid|TimeSpan}` default value made optional.
- Upgraded host builder expression from `IWebHostBuilde` to `WebApplication`.

### Removed

- `Falco.Markup`, module has been extracted into it's own [project](https://github.com/pimbrouwers/Falco.Markup).
- Additional `StringCollectionReader` constructors, per-collection type.
- `StringCollectionReader.TryArrayString`, use `StringCollectionReader.GetStringArray`
- `StringCollectionReader.TryArrayInt16`, use `StringCollectionReader.GetInt16Array`
- `StringCollectionReader.TryArrayInt32`, use `StringCollectionReader.GetInt32Array`
- `StringCollectionReader.TryArrayInt`, use `StringCollectionReader.GetIntArray`
- `StringCollectionReader.TryArrayInt64`, use `StringCollectionReader.GetInt64Array`
- `StringCollectionReader.TryArrayBoolean`, use `StringCollectionReader.GetBooleanArray`
- `StringCollectionReader.TryArrayFloat`, use `StringCollectionReader.GetFloatArray`
- `StringCollectionReader.TryArrayDecimal`, use `StringCollectionReader.GetDecimalArray`
- `StringCollectionReader.TryArrayDateTime`, use `StringCollectionReader.GetDateTimeArray`
- `StringCollectionReader.TryArrayDateTimeOffset`, use `StringCollectionReader.GetDateTimeOffsetArray`
- `StringCollectionReader.TryArrayGuid`, use `StringCollectionReader.GetGuidArray`
- `StringCollectionReader.TryArrayTimeSpan`, use `StringCollectionReader.GetTimeSpanArray`
- `HttpRequest.IsMultipart`, `HttpRequest.TryStreamFormAsync`, use `HttpRequest.StreamFormAsync()`
- `Request.tryBindRoute`, use `Request.getRoute`.
- `Request.tryBindQuery`, use `Request.getQuery`.
- `Request.tryBindForm`, use `Request.getForm`.
- `Request.tryBindFormStream`, use `Request.tryStreamForm`.
- `Request.tryBindCookie`, use `Request.getCookie`.
- `Request.getJson`, use `Request.getJsonOptions Constants.defaultJsonOptions`.
- `Request.tryBindJsonOptions`, use `Request.getJsonOptions`.
- `Request.tryBindJson`, use `Request.getJsonOptions Constants.defaultJsonOptions`.
- `Request.bindJson`, use `Request.mapJson`.
- `Request.bindRoute`, use `Request.mapRoute`.
- `Request.bindQuery`, use `Request.mapQuery`.
- `Request.bindCookie`, use `Request.mapCookie`.
- `Request.bindForm`, use `Request.mapForm`.
- `Request.bindFormStream`, use `Request.mapFormStream`.
- `Request.bindFormSecure`, use `Request.mapFormSecure`.
- `Request.bindFormStreamSecure`, use `Request.mapFormStreamSecure`.
- `Response.withHeader`, use `Response.withHeaders [ x ]`.
- `Response.redirect`, use `Response.redirectTemporarily` or `Response.redirectPermanently`

## [3.1.14] - 2022-08-29

### Added

- Embedded readme and project icon into NuGet package.
- Additional obsolete attributes to functions in preparation for v4.x.

### Fixed

- `Request.mapJson` failing to invoke next handler, caused by a bad merge which left the valid function body commented out.

## [3.1.13] - 2022-08-11

### Added

- Obsolete attributes to `Request.bind{Json|Route|Query|Cookie|Form|FormStream|FormSecure|FormStreamSecure} functions in preparation for v4.x.

### Fixed

- Typo in `Markup.Attr.httpEquiv`.

## [3.1.12] - 2022-05-20

### Added

- `Auth.signInOptions` to establish an authenticated context for the provide scheme, options and principal
- `Markup.Attr.open'`.
- Missing .gitignore items for JetBrains.

## [3.1.11] - 2/8/2022

### Added

- `Auth.challenge` to challenge the specified authentication scheme.
- `Response.challengeWithRedirect` to challenge the specified authentication scheme and redirect URI.

### Fixed

- Website to address certain accessibility issues.

## [3.1.10] - 12/14/2021

### Changed

- Incorporated built-in `System.Task` expression, with compiler directives to continue supporting Ply usage.

## [3.1.9] - 12/6/2021

### Changed

- `StringCollectionReader` lookups made case-insensitive.

## [3.1.8] - 12/3/2021

### Added

- `net6.0` support.

### Changed

- Embedded PDBs to faciliate sourcelink.

## [3.1.7] - 9/24/2021

### Added

- `HttpVerb.toHttpMethodMetadata` to properly capture the `HttpVerb.ANY` to produce an empty `HttpMethodData` (not `HttpMethodData [| "ANY" |]`).

## [3.1.6] - 9/24/2021

### Removed

- Mistakenly added, experimental `Request.signOut` function.

## [3.1.5] - 9/24/2021

### Added

- Route name metadata to support ASP.NET link generator.
- Null check to internal `Response.writeString`.
- Explicit starting size for the internal `StringBuilder` within `XmlNodeSerializer`

## [3.1.4] - 8/24/2021

### Added

- Comparative view engine benchmarks.
- `XmlNodeSerializer` type.
- Source link support.

## [3.1.3] - 8/4/2021

### Added

- Dependency on [Ply](https://github.com/crowded/ply).
- `Request.authenticate` to authenticate the current request using the provided scheme.

### Removed

- TPL helpers.

## [3.1.2] - 7/30/2021

### Changed

- CSRF validation switched to occur _after_ form is streamed, which includes enabling buffering for the request body.


## [3.1.1] - 7/27/2021

### Added

- `stringf` function.

### Removed

- Dependency on Taskbuilder.fs, replaced with TPL helpers.

## [3.1.0] - 7/27/2021

### Added

- `FalcoEndpointDataSource` to properly integrate with ASP.NET endpoint routing.
- Binary response handlers `Response.ofBinary`, `Response.ofAttachment`.
- `Auth.getClaimValue`
- `IServiceCollection` operations to the HostBuilder expression: `add_service`, `add_antiforgery`, `add_cookie`, `add_conf_cookies`, `add_authorization`, `add_data_protection`, `add_http_client`.
- `IApplicationBuilder` operations to the HostBuilder expression:
`use_middleware`, `use_if`, `use_ifnot`, `use_authentication`, `use_authorization`, `use_caching`, `use_compression`, `use_hsts`, `use_https`, `use_static_files`.
- `not_found` operation added to HostBuilder expression to serve default document when no endpoints are matched.

### Changed

- Internal `Response.writeBytes` to use `BodyWriter`.

### Fixed

- Optional JSON config file fix, misassigned.

### Removed

- MVC and REST templates

## [3.0.5] - 6/14/2021
## [3.0.4] - 5/5/2021
## [3.0.3] - 4/10/2021
## [3.0.2] - 12/8/2020
## [3.0.1] - 12/1/2020
## [3.0.0] - 11/27/2020
## [2.1.0] - 11/11/2020
## [2.0.4] - 11/9/2020
## [2.0.3] - 10/31/2020
## [2.0.2] - 7/31/2020
## [2.0.1] - 7/20/2020
## [2.0.0] - 7/12/2020
## [1.2.3] - 7/2/2020
## [1.2.2] - 6/29/2020
## [1.2.1] - 6/28/2020
## [1.2.0] - 6/23/2020
## [1.1.0] - 6/6/2020

[unreleased]: https://github.com/pimbrouwers/Falco/compare/v4.0.3...HEAD
[4.0.3]: https://github.com/pimbrouwers/Falco/compare/v4.0.2...v4.0.3
[4.0.2]: https://github.com/pimbrouwers/Falco/compare/v4.0.1...v4.0.2
[4.0.1]: https://github.com/pimbrouwers/Falco/compare/v4.0.0...v4.0.1
[4.0.0]: https://github.com/pimbrouwers/Falco/compare/v3.1.14...v4.0.0
[3.1.14]: https://github.com/pimbrouwers/Falco/compare/v3.1.13...v3.1.14
[3.1.13]: https://github.com/pimbrouwers/Falco/compare/v3.1.12...v3.1.13
[3.1.12]: https://github.com/pimbrouwers/Falco/compare/v3.1.11...v3.1.12
[3.1.11]: https://github.com/pimbrouwers/Falco/compare/v3.1.10...v3.1.11
[3.1.10]: https://github.com/pimbrouwers/Falco/compare/v3.1.9...v3.1.10
[3.1.9]: https://github.com/pimbrouwers/Falco/compare/v3.1.8...v3.1.9
[3.1.8]: https://github.com/pimbrouwers/Falco/compare/v3.1.7...v3.1.8
[3.1.7]: https://github.com/pimbrouwers/Falco/compare/v3.1.6...v3.1.7
[3.1.6]: https://github.com/pimbrouwers/Falco/compare/v3.1.5...v3.1.6
[3.1.5]: https://github.com/pimbrouwers/Falco/compare/v3.1.4...v3.1.5
[3.1.4]: https://github.com/pimbrouwers/Falco/compare/v3.1.3...v3.1.4
[3.1.3]: https://github.com/pimbrouwers/Falco/compare/v3.1.2...v3.1.3
[3.1.2]: https://github.com/pimbrouwers/Falco/compare/v3.1.1...v3.1.2
[3.1.1]: https://github.com/pimbrouwers/Falco/compare/v3.1.0...v3.1.1
[3.1.0]: https://github.com/pimbrouwers/Falco/compare/v3.0.5...v3.1.0
[3.0.5]: https://github.com/pimbrouwers/Falco/compare/v3.0.4...v3.0.5
[3.0.4]: https://github.com/pimbrouwers/Falco/compare/v3.0.3...v3.0.4
[3.0.3]: https://github.com/pimbrouwers/Falco/compare/v3.0.2...v3.0.3
[3.0.2]: https://github.com/pimbrouwers/Falco/compare/v3.0.1...v3.0.2
[3.0.1]: https://github.com/pimbrouwers/Falco/compare/v3.0.0...v3.0.1
[3.0.0]: https://github.com/pimbrouwers/Falco/compare/v2.1.0...v3.0.0
[2.1.0]: https://github.com/pimbrouwers/Falco/compare/v2.0.4...v2.1.0
[2.0.4]: https://github.com/pimbrouwers/Falco/compare/v2.0.3...v2.0.4
[2.0.3]: https://github.com/pimbrouwers/Falco/compare/v2.0.2...v2.0.3
[2.0.2]: https://github.com/pimbrouwers/Falco/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/pimbrouwers/Falco/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/pimbrouwers/Falco/compare/v1.2.3...v2.0.0
[1.2.3]: https://github.com/pimbrouwers/Falco/compare/v1.2.2...v1.2.3
[1.2.2]: https://github.com/pimbrouwers/Falco/compare/v1.2.1...v1.2.2
[1.2.1]: https://github.com/pimbrouwers/Falco/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/pimbrouwers/Falco/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/pimbrouwers/Falco/releases/tag/v1.1.0