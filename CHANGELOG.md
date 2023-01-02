> Work in progress!

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Removed

- Unused types `HttpContextAccessor` and `AsyncHttpContextAccessor`.

## [4.0.3] - 2023/01/01

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

- Related community projects and libraries to README.md.

### Fixed

- NuGet package metadata, invalid icon path.

## [4.0.0] - 2022-11-07

### Added

- `Services.inject` helpers, for CPS-style dependency injection, supporting up to five generic input types.

### Changed

- Upgraded host builder expression from `IWebHostBuilde` to `WebApplication`.


## [3.1.14] - 4 months ago
## [3.1.13] - 5 months ago
## [3.1.12] - 7 months ago
## [3.1.11] - 2/8/2022
## [3.1.10] - 12/14/2021
## [3.1.9] - 12/6/2021
## [3.1.8] - 12/3/2021
## [3.1.7] - 9/24/2021
## [3.1.6] - 9/24/2021
## [3.1.5] - 9/24/2021
## [3.1.4] - 8/24/2021
## [3.1.3] - 8/4/2021
## [3.1.2] - 7/30/2021
## [3.1.1] - 7/27/2021
## [3.1.0] - 7/27/2021
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