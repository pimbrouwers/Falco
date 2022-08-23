module Falco.HostBuilder

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.DataProtection
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.ResponseCompression
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

// ------------
// Config Builder
// ------------
type ConfigurationSpec =
    { BasePath     : string
      RequiredJson : string list
      OptionalJson : string list
      AddEnvVars   : bool }

    static member Empty =
        { BasePath     = IO.Directory.GetCurrentDirectory()
          RequiredJson = []
          OptionalJson = []
          AddEnvVars   = false }

type ConfigBuilder (args : string[]) =
    member _.Yield(_) = ConfigurationSpec.Empty

    member _.Run(conf : ConfigurationSpec) =
        let mutable bldr = ConfigurationBuilder().SetBasePath(conf.BasePath)

        bldr <- bldr.AddCommandLine (args)

        if conf.AddEnvVars then
            bldr <- bldr.AddEnvironmentVariables()

        for json in conf.RequiredJson do
            bldr <- bldr.AddJsonFile(json, optional = false, reloadOnChange = true)

        for json in conf.OptionalJson do
            bldr <- bldr.AddJsonFile(json, optional = true, reloadOnChange = true)

        bldr.Build() :> IConfiguration

    /// Set the base path of the ConfigurationBuilder.
    [<CustomOperation("base_path")>]
    member _.SetBasePath (conf : ConfigurationSpec, basePath : string) =
        { conf with BasePath = basePath }

    /// Add Environment Variables to the ConfigurationBuilder.
    [<CustomOperation("add_env")>]
    member _.AddEnvVars (conf : ConfigurationSpec) =
        { conf with AddEnvVars = true }

    /// Add required JSON file to the ConfigurationBuilder.
    [<CustomOperation("required_json")>]
    member _.AddRequiredJsonFile (conf : ConfigurationSpec, filePath : string) =
        { conf with RequiredJson = filePath :: conf.RequiredJson }

    /// Add optional JSON file to the ConfigurationBuilder.
    [<CustomOperation("optional_json")>]
    member _.AddOptionalJsonFile (conf : ConfigurationSpec, filePath : string) =
        { conf with OptionalJson = filePath :: conf.OptionalJson }


let configuration args = ConfigBuilder(args)

// ------------
// Host Builder
// ------------

/// Represents the eventual existence of a runnable IWebhost
type HostConfig =
    { WebHost    : IWebHostBuilder -> IWebHostBuilder
      Logging    : ILoggingBuilder -> ILoggingBuilder
      Services   : IServiceCollection -> IServiceCollection
      Middleware : IApplicationBuilder -> IApplicationBuilder
      NotFound   : HttpHandler option
      Endpoints  : HttpEndpoint list
      Builder    : HttpEndpoint list -> IWebHostBuilder -> IWebHostBuilder
      IsCustom   : bool }

    static member Empty =
        let defaultBuilder (endpoints : HttpEndpoint list) (webHost : IWebHostBuilder) =
            let configureServices (services : IServiceCollection) =
                services.AddFalco() |> ignore

            let configureApp (app : IApplicationBuilder) =
                app.UseDeveloperExceptionPage()
                   .UseFalco(endpoints) |> ignore

            webHost.ConfigureServices(configureServices).Configure(configureApp)

        { WebHost    = id
          Logging    = id
          Services   = id
          Middleware = id
          NotFound   = None
          Endpoints  = []
          Builder    = defaultBuilder
          IsCustom   = false }

/// Computation expression to allow for elegant IHost construction
type HostBuilder(args : string[]) =
    member _.Yield(_) = HostConfig.Empty

    member _.Run(conf : HostConfig) =
        let configure =
            if conf.IsCustom then
                let wrappedBuilder = fun bldr -> conf.Builder conf.Endpoints bldr |> ignore
                Action<IWebHostBuilder>(wrappedBuilder)
            else
                let addFalco = fun (services : IServiceCollection) -> services.AddFalco ()

                let useFalco = fun (app : IApplicationBuilder) -> app.UseFalco (conf.Endpoints)

                let includeNotFound = fun (app : IApplicationBuilder) ->
                    match conf.NotFound with
                    | Some handler -> app.Run(HttpHandler.toRequestDelegate handler)
                    | None -> ()

                let wrappedBuilder = fun bldr ->
                    conf.WebHost(bldr)
                        .ConfigureLogging(conf.Logging >> ignore)
                        .ConfigureServices(addFalco >> conf.Services >> ignore)
                        .Configure(conf.Middleware >> useFalco >> includeNotFound)
                    |> ignore

                Action<IWebHostBuilder>(wrappedBuilder)

        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(configure)
            .Build()
            .Run()

    /// Register Falco HttpEndpoint's
    [<CustomOperation("endpoints")>]
    member _.Endpoints (conf : HostConfig, endpoints : HttpEndpoint list) =
        { conf with Endpoints = endpoints }

    // ------------
    // Service Collection
    // ------------

    /// Configure logging via ILogger
    [<CustomOperation("logging")>]
    member _.Logging (conf : HostConfig, fn : ILoggingBuilder -> ILoggingBuilder) =
        { conf with Logging = conf.Logging >> fn }

    /// Add a new service descriptor into the IServiceCollection.
    [<CustomOperation("add_service")>]
    member _.AddService (conf : HostConfig, fn : IServiceCollection -> IServiceCollection) =
        { conf with Services = conf.Services >> fn }

    /// Add Antiforgery support into the IServiceCollection.
    [<CustomOperation("add_antiforgery")>]
    member x.AddAntiforgery (conf : HostConfig) =
        x.AddService (conf, fun s -> s.AddAntiforgery())

    /// Add default cookie authentication into the IServiceCollection.
    [<CustomOperation("add_cookie")>]
    member x.AddCookie (conf : HostConfig) =
        x.AddService (conf, fun s -> s.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie() |> ignore; s)

    /// Add configured cookie(s) authentication into the IServiceCollection.
    [<CustomOperation("add_conf_cookies")>]
    member x.AddConfiguredCookies (conf : HostConfig, configure : AuthenticationOptions -> unit, cookieConfig : (string * (CookieAuthenticationOptions -> unit)) list) =
        let addAuthentication (svc : IServiceCollection) =
            let x = svc.AddAuthentication(Action<AuthenticationOptions>(configure))

            for (scheme, config) in cookieConfig do
                x.AddCookie(scheme, Action<CookieAuthenticationOptions>(config)) |> ignore

            svc

        x.AddService (conf, addAuthentication)

    /// Add default Authorization into the IServiceCollection.
    [<CustomOperation("add_authorization")>]
    member x.AddAuthorization (conf : HostConfig) =
        x.AddService (conf, fun svc -> svc.AddAuthorization())

    /// Add file system based data protection.
    [<CustomOperation("add_data_protection")>]
    member x.AddDataProtection (conf : HostConfig, dir : string) =
        let addDataProtection (svc : IServiceCollection) =
            svc.AddDataProtection().PersistKeysToFileSystem(IO.DirectoryInfo(dir))
            |> ignore
            svc

        x.AddService (conf, addDataProtection)

    /// Add IHttpClientFactory into the IServiceCollection
    [<CustomOperation("add_http_client")>]
    member x.AddHttpClient (conf : HostConfig) =
        x.AddService (conf, fun svc -> svc.AddHttpClient())

    // ------------
    // Application Builder
    // ------------

    /// Use the specified middleware.
    [<CustomOperation("use_middleware")>]
    member _.Use (conf : HostConfig, fn : IApplicationBuilder -> IApplicationBuilder) =
        { conf with Middleware = conf.Middleware >> fn }

    /// Use the specified middleware if the provided predicate is "true".
    [<CustomOperation("use_if")>]
    member _.UseIf (conf : HostConfig, pred : IApplicationBuilder -> bool, fn : IApplicationBuilder -> IApplicationBuilder) =
        { conf with Middleware = fun app -> if pred app then conf.Middleware(app) |> fn else conf.Middleware(app) }

    /// Use the specified middleware if the provided predicate is "true".
    [<CustomOperation("use_ifnot")>]
    member _.UseIfNot (conf : HostConfig, pred : IApplicationBuilder -> bool, fn : IApplicationBuilder -> IApplicationBuilder) =
        { conf with Middleware = fun app -> if not(pred app) then conf.Middleware(app) |> fn else conf.Middleware(app) }

    /// Use authorization middleware. Call before any middleware that depends
    /// on users being authenticated.
    [<CustomOperation("use_authentication")>]
    member x.UseAuthentication (conf : HostConfig) =
        x.Use (conf, fun app -> app.UseAuthentication())

    /// Register authorization service and enable middleware
    [<CustomOperation("use_authorization")>]
    member _.UseAuthorization (conf : HostConfig) =
        { conf with
               Services = conf.Services >> fun s -> s.AddAuthorization()
               Middleware = conf.Middleware >> fun app -> app.UseAuthorization() }

    /// Register HTTP Response caching service and enable middleware.
    [<CustomOperation("use_caching")>]
    member x.UseCaching(conf : HostConfig) =
        { conf with
               Services = conf.Services >> fun s -> s.AddResponseCaching()
               Middleware = conf.Middleware >> fun app -> app.UseResponseCaching() }

    /// Register Brotli + GZip HTTP Compression service and enable middleware.
    [<CustomOperation("use_compression")>]
    member _.UseCompression (conf : HostConfig) =
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
    member x.UseHsts (conf : HostConfig) =
        x.Use (conf, fun app -> app.UseHsts())

    /// Use automatic HTTPS redirection.
    [<CustomOperation("use_https")>]
    member x.UseHttps (conf : HostConfig) =
        x.Use (conf, fun app -> app.UseHttpsRedirection())

    /// Use Static File middleware.
    [<CustomOperation("use_static_files")>]
    member _.UseStaticFiles (conf : HostConfig) =
        { conf with Middleware = conf.Middleware >> fun app -> app.UseStaticFiles() }

    // Errors
    // ------------

    /// Include a catch-all (i.e., Not Found) HttpHandler (must be added last).
    [<CustomOperation("not_found")>]
    member _.NotFound (conf : HostConfig, handler : HttpHandler) =
        { conf with NotFound = Some handler }

/// A computation expression to make IHost construction easier
let webHost args = HostBuilder(args)
