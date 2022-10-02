# Falco

[![NuGet Version](https://img.shields.io/nuget/v/Falco.svg)](https://www.nuget.org/packages/Falco)
[![build](https://github.com/pimbrouwers/Falco/actions/workflows/build.yml/badge.svg)](https://github.com/pimbrouwers/Falco/actions/workflows/build.yml)

```fsharp
open Falco
open Falco.Routing
open Falco.HostBuilder

webHost [||] {
    endpoints [
        get "/" (Response.ofPlainText "Hello World")
    ]
}
```

[Falco](https://github.com/pimbrouwers/Falco) is a toolkit for building fast, functional-first and fault-tolerant web applications using F#.

- Built upon the high-performance components of ASP.NET Core.
- Optimized for building HTTP applications quickly.
- Seamlessly integrates with existing .NET Core middleware and libraries.

## Key Features

- Asynchronous [request handling](https://github.com/pimbrouwers/Falco/tree/master/doc/response.md).
- Simple and powerful [routing](https://github.com/pimbrouwers/Falco/tree/master/doc/routing.md) API.
- Fast, secure and configurable [web server](https://github.com/pimbrouwers/Falco/tree/master/doc/host.md).
- Native F# [view engine](https://github.com/pimbrouwers/Falco.Markup).
- Uniform API for [accessing request data](https://github.com/pimbrouwers/Falco/tree/master/doc/re  uest.md).
- [Authentication and security](https://github.com/pimbrouwers/Falco/tree/master/doc/security.md) utilities.
- Built-in support for [large uploads](https://github.com/pimbrouwers/Falco/tree/master/doc/request.md#multipartform-data-binding) and [binary responses](https://github.com/pimbrouwers/Falco/tree/master/doc/response.md#content-disposition).

## Design Goals

- Provide a toolset to build a working full-stack web application.
- Should be simple, extensible and integrate with existing .NET libraries.
- Can be easily learned.

## Learn

The best way to get started is by visiting the [documentation](https://falcoframework.com/docs). For questions and support please use [discussions](https://github.com/pimbrouwers/Falco/discussions). The issue list of this repo is **exclusively** for bug reports and feature requests.

If you want to stay in touch, feel free to reach out on [Twitter](https://twitter.com/falco_framework).

Have an article or video that you want to share? We'd love to hear from you! To add your content, visit this [discussion](https://github.com/pimbrouwers/Falco/discussions/82).

### Articles

- Istvan - [Running ASP.Net web application with Falco on AWS Lambda](https://dev.l1x.be/posts/2020/12/18/running-asp.net-web-application-with-falco-on-aws-lambda/)

### Videos

- Ben Gobeil - [Why I'm Using Falco Instead Of Saturn | How To Switch Your Backend In SAFE Stack | StonkWatch Ep.13](https://youtu.be/DTy5gIUWvpo)

## Contribute

Thank you for considering contributing to Falco, and to those who have already contributed! We appreciate (and actively resolve) PRs of all shapes and sizes.

We kindly ask that before submitting a pull request, you first submit an [issue](https://github.com/pimbrouwers/Falco/issues) or open a [discussion](https://github.com/pimbrouwers/Falco/discussions).


If functionality is added to the API, or changed, please kindly update the relevent [document](https://github.com/pimbrouwers/Falco/tree/master/docs). Unit tests must also be added and/or updated before a pull request can be successfully merged.

All pull requests should originate from the `develop` branch. A merge into this branch means that your changes are scheduled to go into production with the very next release, which could happen any time from the same day up to a couple weeks (depending on priorities and urgency).

Only pull requests which pass all build checks and comply with the general coding guidelines can be approved.

If you have any further questions, submit an [issue](https://github.com/pimbrouwers/Falco/issues) or open a [discussion](https://github.com/pimbrouwers/Falco/discussions) or reach out on [Twitter](https://twitter.com/falco_framework).

## Why "Falco"?

[Kestrel](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel) has been a game changer for the .NET web stack. In the animal kingdom, "Kestrel" is a name given to several members of the falcon genus. Also known as "Falco".

## Find a bug?

There's an [issue](https://github.com/pimbrouwers/Falco/issues) for that.

## License

Built with ♥ by [Pim Brouwers](https://github.com/pimbrouwers) in Toronto, ON. Licensed under [Apache License 2.0](https://github.com/pimbrouwers/Falco/blob/master/LICENSE).
