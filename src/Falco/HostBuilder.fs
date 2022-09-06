namespace Falco.HostBuilder

open System
open Falco
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.DataProtection
open Microsoft.AspNetCore.ResponseCompression
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type HostBuilderSpec =
    { Logging    : ILoggingBuilder -> ILoggingBuilder
      Services   : IServiceCollection -> IServiceCollection
      Middleware : IApplicationBuilder -> IApplicationBuilder
      NotFound   : HttpHandler option
      Endpoints  : HttpEndpoint list }

    static member Empty =
        { Logging    = id
          Services   = id
          Middleware = id
          NotFound   = None
          Endpoints  = [] }

/// Computation expression to allow for elegant IHost construction
type HostBuilder(args : string[]) =
    member _.Yield(_) = HostBuilderSpec.Empty

    member _.Run(conf : HostBuilderSpec) =
        let configureLogging (log : ILoggingBuilder) =
            log |> conf.Logging |> ignore

        let configureServices (svc : IServiceCollection) =
            let addFalco = fun (services : IServiceCollection) -> services.AddFalco()
            svc |> (addFalco >> conf.Services) |> ignore

        let configureApp (app : WebApplication) =
            let useFalco = fun (app : IApplicationBuilder) -> app.UseFalco (conf.Endpoints)

            let devExceptionHandler = fun (app : IApplicationBuilder) ->
                if FalcoExtensions.IsDevelopment app then app.UseDeveloperExceptionPage()
                else app

            let includeNotFound = fun (app : IApplicationBuilder) ->
                match conf.NotFound with
                | Some handler -> app.Run(HttpHandler.toRequestDelegate handler)
                | None -> ()

            (app :> IApplicationBuilder)
            |> (conf.Middleware >> useFalco >> includeNotFound)
            |> ignore

        let builder = WebApplication.CreateBuilder(args)
        configureLogging builder.Logging
        configureServices builder.Services

        let app = builder.Build()
        configureApp app

        app.Run()

    /// Register Falco HttpEndpoint's
    [<CustomOperation("endpoints")>]
    member _.Endpoints (conf : HostBuilderSpec, endpoints : HttpEndpoint list) =
        { conf with Endpoints = endpoints }

    // ------------
    // Service Collection
    // ------------

    /// Configure logging via ILogger
    [<CustomOperation("logging")>]
    member _.Logging (conf : HostBuilderSpec, fn : ILoggingBuilder -> ILoggingBuilder) =
        { conf with Logging = conf.Logging >> fn }

    /// Add a new service descriptor into the IServiceCollection.
    [<CustomOperation("add_service")>]
    member _.AddService (conf : HostBuilderSpec, fn : IServiceCollection -> IServiceCollection) =
        { conf with Services = conf.Services >> fn }

    /// Add Antiforgery support into the IServiceCollection.
    [<CustomOperation("add_antiforgery")>]
    member x.AddAntiforgery (conf : HostBuilderSpec) =
        x.AddService (conf, fun s -> s.AddAntiforgery())

    /// Add configured cookie(s) authentication into the IServiceCollection.
    [<CustomOperation("add_cookies")>]
    member x.AddCookies (
        conf : HostBuilderSpec,
        authConfig : AuthenticationOptions -> unit,
        cookies : (string * (CookieAuthenticationOptions -> unit)) list) =
        let addAuthentication (svc : IServiceCollection) =
            let x = svc.AddAuthentication(Action<AuthenticationOptions>(authConfig))

            for (scheme, config) in cookies do
                x.AddCookie(scheme, Action<CookieAuthenticationOptions>(config)) |> ignore

            svc

        x.AddService (conf, addAuthentication)

    /// Add default cookie authentication into the IServiceCollection.
    [<CustomOperation("add_cookie")>]
    member x.AddCookie (conf : HostBuilderSpec, scheme : string, config : CookieAuthenticationOptions -> unit) =
        x.AddService (conf, fun s -> s.AddAuthentication(scheme).AddCookie(config) |> ignore; s)


    /// Add default Authorization into the IServiceCollection.
    [<CustomOperation("add_authorization")>]
    member x.AddAuthorization (conf : HostBuilderSpec) =
        x.AddService (conf, fun svc -> svc.AddAuthorization())

    /// Add file system based data protection.
    [<CustomOperation("add_data_protection")>]
    member x.AddDataProtection (conf : HostBuilderSpec, dir : string) =
        let addDataProtection (svc : IServiceCollection) =
            svc.AddDataProtection().PersistKeysToFileSystem(IO.DirectoryInfo(dir))
            |> ignore
            svc

        x.AddService (conf, addDataProtection)

    /// Add IHttpClientFactory into the IServiceCollection
    [<CustomOperation("add_http_client")>]
    member x.AddHttpClient (conf : HostBuilderSpec) =
        x.AddService (conf, fun svc -> svc.AddHttpClient())

    // ------------
    // Application Builder
    // ------------

    /// Use the specified middleware.
    [<CustomOperation("use_middleware")>]
    member _.Use (conf : HostBuilderSpec, fn : IApplicationBuilder -> IApplicationBuilder) =
        { conf with Middleware = conf.Middleware >> fn }

    /// Use the specified middleware if the provided predicate is "true".
    [<CustomOperation("use_if")>]
    member _.UseIf (conf : HostBuilderSpec, pred : IApplicationBuilder -> bool, fn : IApplicationBuilder -> IApplicationBuilder) =
        { conf with Middleware = fun app -> if pred app then conf.Middleware(app) |> fn else conf.Middleware(app) }

    /// Use the specified middleware if the provided predicate is "true".
    [<CustomOperation("use_ifnot")>]
    member _.UseIfNot (conf : HostBuilderSpec, pred : IApplicationBuilder -> bool, fn : IApplicationBuilder -> IApplicationBuilder) =
        { conf with Middleware = fun app -> if not(pred app) then conf.Middleware(app) |> fn else conf.Middleware(app) }

    /// Use authorization middleware. Call before any middleware that depends
    /// on users being authenticated.
    [<CustomOperation("use_authentication")>]
    member x.UseAuthentication (conf : HostBuilderSpec) =
        x.Use (conf, fun app -> app.UseAuthentication())

    /// Register authorization service and enable middleware
    [<CustomOperation("use_authorization")>]
    member _.UseAuthorization (conf : HostBuilderSpec) =
        { conf with
               Services = conf.Services >> fun s -> s.AddAuthorization()
               Middleware = conf.Middleware >> fun app -> app.UseAuthorization() }

    /// Register HTTP Response caching service and enable middleware.
    [<CustomOperation("use_caching")>]
    member x.UseCaching(conf : HostBuilderSpec) =
        { conf with
               Services = conf.Services >> fun s -> s.AddResponseCaching()
               Middleware = conf.Middleware >> fun app -> app.UseResponseCaching() }

    /// Register Brotli + GZip HTTP Compression service and enable middleware.
    [<CustomOperation("use_compression")>]
    member _.UseCompression (conf : HostBuilderSpec) =
        let configureCompression (s : IServiceCollection) =
            let mimeTypes =
                let additionalMimeTypes = [|
                    "image/jpeg"
                    "image/png"
                    "image/svg+xml"
                    "font/woff"
                    "font/woff2"
                |]

                ResponseCompressionDefaults.MimeTypes
                |> Seq.append additionalMimeTypes

            s.AddResponseCompression(fun o ->
                o.Providers.Add<BrotliCompressionProvider>()
                o.Providers.Add<GzipCompressionProvider>()
                o.MimeTypes <- mimeTypes)


        { conf with
               Services = conf.Services >> configureCompression
               Middleware = conf.Middleware >> fun app -> app.UseResponseCompression() }

    /// Use automatic HSTS middleware (adds strict-transport-policy header).
    [<CustomOperation("use_hsts")>]
    member x.UseHsts (conf : HostBuilderSpec) =
        x.Use (conf, fun app -> app.UseHsts())

    /// Use automatic HTTPS redirection.
    [<CustomOperation("use_https")>]
    member x.UseHttps (conf : HostBuilderSpec) =
        x.Use (conf, fun app -> app.UseHttpsRedirection())

    /// Use Default File middleware. Must be called before use_static_files
    [<CustomOperation("use_default_files")>]
    member _.UseDefaultFiles (conf : HostBuilderSpec, ?config : DefaultFilesOptions) =
        let configureDefaultFiles (app : IApplicationBuilder) =
            match config with
            | Some x -> app.UseDefaultFiles(x)
            | None   -> app.UseDefaultFiles()

        { conf with Middleware = conf.Middleware >> configureDefaultFiles }

    /// Use Static File middleware.
    [<CustomOperation("use_static_files")>]
    member _.UseStaticFiles (conf : HostBuilderSpec, ?config : StaticFileOptions) =
        let configureStaticFiles (app : IApplicationBuilder) =
            match config with
            | Some x -> app.UseStaticFiles(x)
            | None   -> app.UseStaticFiles()

        { conf with Middleware = conf.Middleware >> configureStaticFiles }

    // ------------
    // Errors
    // ------------

    /// Include a catch-all (i.e., Not Found) HttpHandler (must be added last).
    [<CustomOperation("not_found")>]
    member _.NotFound (conf : HostBuilderSpec, handler : HttpHandler) =
        { conf with NotFound = Some handler }

[<AutoOpen>]
module WebHostBuilder =
    /// Computation expression to allow for elegant IHost construction
    let webHost args = HostBuilder(args)
