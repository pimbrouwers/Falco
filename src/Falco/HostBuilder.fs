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

type ConfigBuilder () =
    member _.Yield(_) = ConfigurationSpec.Empty

    member _.Run(conf : ConfigurationSpec) =
        let mutable bldr = ConfigurationBuilder().SetBasePath(conf.BasePath)
        for json in conf.RequiredJson do
            bldr <- bldr.AddJsonFile(json, optional = false, reloadOnChange = true)

        for json in conf.OptionalJson do
            bldr <- bldr.AddJsonFile(json, optional = true, reloadOnChange = true)

        if conf.AddEnvVars then 
            bldr <- bldr.AddEnvironmentVariables()

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
        { conf with RequiredJson = filePath :: conf.OptionalJson }


let configuration = ConfigBuilder()

// Host Builder
// ------------

/// Represents the eventual existence of a runnable IWebhost
type HostConfig = 
    { WebHost    : IWebHostBuilder -> IWebHostBuilder              
      Endpoints  : HttpEndpoint list 
      Middleware : IApplicationBuilder -> IApplicationBuilder 
      Services   : IServiceCollection -> IServiceCollection
      NotFound   : HttpHandler option
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
          Endpoints  = [] 
          Middleware = id
          Services   = id
          NotFound   = None
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
                let enableFalco = fun (services : IServiceCollection) -> services.AddFalco ()

                let activateFalco = fun (app : IApplicationBuilder) -> app.UseFalco (conf.Endpoints)

                let includeNotFound = fun (app : IApplicationBuilder) -> 
                    match conf.NotFound with
                    | Some handler -> app.Run(HttpHandler.toRequestDelegate handler)
                    | None -> ()

                let wrappedBuilder = fun bldr -> 
                    conf.WebHost(bldr)
                        .ConfigureServices(enableFalco >> conf.Services >> ignore)
                        .Configure(conf.Middleware >> activateFalco >> includeNotFound)
                    |> ignore
                                            
                Action<IWebHostBuilder>(wrappedBuilder)
                    
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(configure)
            .Build()
            .Run()   
    
    /// Configure the IWebHost, when specified will overide all
    /// "with" and "add" operations.
    [<CustomOperation("configure")>]
    member _.Configure (conf : HostConfig, builder : HttpEndpoint list -> IWebHostBuilder -> IWebHostBuilder) =
        { conf with Builder = builder; IsCustom = true }

    /// Falco HttpEndpoint's
    [<CustomOperation("endpoints")>]
    member _.Endpoints (conf : HostConfig, endpoints : HttpEndpoint list) =
        { conf with Endpoints = endpoints }

    /// Apply the given configuration to the web host.
    [<CustomOperation("host_config")>]
    member _.HostConfig (conf : HostConfig, config : IConfiguration) =
        { conf with WebHost = conf.WebHost >> fun webHost -> webHost.UseConfiguration(config) }


    // Service Collection
    // ------------

    /// Add a new service descriptor into the IServiceCollection.
    [<CustomOperation("add")>]
    member _.Add (conf : HostConfig, fn : IServiceCollection -> IServiceCollection) =
        { conf with Services = conf.Services >> fn }

    /// Add Antiforgery support into the IServiceCollection.
    [<CustomOperation("add_antiforgery")>]
    member x.AddAntiforgery (conf : HostConfig) =
        x.Add (conf, fun s -> s.AddAntiforgery())
    
    
    /// Add default cookie authentication into the IServiceCollection.
    [<CustomOperation("add_cookie")>]
    member x.AddCookie (conf : HostConfig) =        
        x.Add (conf, fun s -> s.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie() |> ignore; s)


    /// Add configured cookie(s) authentication into the IServiceCollection.
    [<CustomOperation("add_conf_cookies")>]
    member x.AddConfiguredCookies (conf : HostConfig, configure : AuthenticationOptions -> unit, cookieConfig : (string * (CookieAuthenticationOptions -> unit)) list) =
        let addAuthentication (s : IServiceCollection) =    
            let x = s.AddAuthentication(Action<AuthenticationOptions>(configure))              

            for (scheme, config) in cookieConfig do                   
                x.AddCookie(scheme, Action<CookieAuthenticationOptions>(config)) |> ignore
            
            s

        x.Add (conf, addAuthentication)

    /// Add default Authorization into the IServiceCollection.
    [<CustomOperation("add_authorization")>]
    member x.AddAuthorization (conf : HostConfig) =
        x.Add (conf, fun s -> s.AddAuthorization())

    /// Add file system based data protection.
    [<CustomOperation("add_data_protection")>]
    member x.AddDataProtection (conf : HostConfig, dir : string) =
        let addDataProtection (s : IServiceCollection) =
            s.AddDataProtection().PersistKeysToFileSystem(IO.DirectoryInfo(dir))
            |> ignore
            s

        x.Add (conf, addDataProtection)

    /// Add IHttpClientFactory into the IServiceCollection
    [<CustomOperation("add_http_client")>]
    member x.AddHttpClient (conf : HostConfig) =
        x.Add (conf, fun s -> s.AddHttpClient())


    // Application Builder
    // ------------

    /// Activate the specified middleware.
    [<CustomOperation("plug")>]
    member _.Plug (conf : HostConfig, fn : IApplicationBuilder -> IApplicationBuilder) =
        { conf with Middleware = conf.Middleware >> fn }

    /// Activate the specified middleware if the provided predicate is "true".
    [<CustomOperation("plug_if")>]
    member _.PlugIf (conf : HostConfig, pred : bool, fn : IApplicationBuilder -> IApplicationBuilder) =
        { conf with Middleware = if pred then conf.Middleware >> fn else conf.Middleware }

    /// Activate the specified middleware if the provided predicate is "true".
    [<CustomOperation("plug_ifnot")>]
    member _.PlugIfNot (conf : HostConfig, pred : bool, fn : IApplicationBuilder -> IApplicationBuilder) =
        { conf with Middleware = if not(pred) then conf.Middleware >> fn else conf.Middleware }

    /// Activate authorization middleware. Call before 
    /// any middleware that depends on users being 
    /// authenticated.    
    [<CustomOperation("plug_authentication")>]
    member x.PlugAuthentication (conf : HostConfig) =
        x.Plug (conf, fun app -> app.UseAuthentication())

    /// Activate authorization middleware
    [<CustomOperation("plug_authorization")>]
    member x.PlugAuthorization (conf : HostConfig) =
        x.Plug (conf, fun app -> app.UseAuthorization())

    /// Activate HTTP Response caching.
    member x.PlugCaching(conf : HostConfig) =        
        { conf with
               Services = conf.Services >> fun s -> s.AddResponseCaching()
               Middleware = conf.Middleware >> fun app -> app.UseResponseCaching() }

    /// Activate Brotli + GZip HTTP Compression.
    [<CustomOperation("plug_compression")>]
    member _.PlugCompression (conf : HostConfig) =
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

    /// Activate developer exception page if predicate is true.
    [<CustomOperation("plug_if_developer_exception")>]
    member x.PlugIfDeveloperException(conf : HostConfig, pred : bool) =
        x.PlugIf(conf, pred, fun app -> app.UseDeveloperExceptionPage())

    /// Activate falco exception handler if predicate is true.
    [<CustomOperation("plug_if_falco_exception")>]
    member x.PlugIfFalcoException(conf : HostConfig, pred : bool, handler : HttpHandler) =
        x.PlugIf(conf, pred, fun app -> app.UseFalcoExceptionHandler(handler))

    /// Activate automatic HSTS middleware (adds 
    /// strict-transport-policy header).
    [<CustomOperation("plug_hsts")>]
    member x.PlugHsts (conf : HostConfig) =
        x.Plug (conf, fun app -> app.UseHsts())       

    /// Activate automatic HTTPS redirection.
    [<CustomOperation("plug_https")>]
    member x.PlugHttps (conf : HostConfig) =
        x.Plug (conf, fun app -> app.UseHttpsRedirection())       

    /// Activate Static File middleware.
    [<CustomOperation("plug_static_files")>]
    member _.PlugStaticFiles (conf : HostConfig) =
        { conf with Middleware = conf.Middleware >> fun app -> app.UseStaticFiles() }

    /// Include a catch-all (i.e., Not Found) HttpHandler (must be added last).
    [<CustomOperation("not_found")>]
    member _.NotFound (conf : HostConfig, handler : HttpHandler) =
        { conf with NotFound = Some handler }

/// A computation expression to make IHost construction easier 
let webHost args = HostBuilder(args)

