namespace Falco.HostBuilder

open System.IO
open Microsoft.Extensions.Configuration

type ConfigFile =
    | IniFile of path : string
    | XmlFile of path : string
    | JsonFile of path : string

type ConfigBuilderSpec =
    { AddEnvVars    : bool
      BasePath      : string
      RequiredFiles : ConfigFile list
      OptionalFiles : ConfigFile list
      InMemory      : Map<string, string> }

    static member Empty =
        { AddEnvVars    = false
          BasePath      = Directory.GetCurrentDirectory()
          RequiredFiles = []
          OptionalFiles = []
          InMemory      = Map.empty }

/// Computation expression to allow for elegant IConfiguration construction
type ConfigBuilder (args : string[]) =
    member _.Yield(_) = ConfigBuilderSpec.Empty

    member _.Run(conf : ConfigBuilderSpec) =
        let mutable bldr = ConfigurationBuilder().SetBasePath(conf.BasePath)

        bldr.AddCommandLine(args) |> ignore

        if conf.AddEnvVars then
            bldr.AddEnvironmentVariables() |> ignore

        for file in conf.RequiredFiles do
            match file with
            | IniFile file  -> bldr.AddIniFile(file, optional = false, reloadOnChange = true)
            | JsonFile file -> bldr.AddJsonFile(file, optional = false, reloadOnChange = true)
            | XmlFile file  -> bldr.AddXmlFile(file, optional = false, reloadOnChange = true)
            |> ignore

        for file in conf.OptionalFiles do
            match file with
            | IniFile file  -> bldr.AddIniFile(file, optional = true, reloadOnChange = true)
            | JsonFile file -> bldr.AddJsonFile(file, optional = true, reloadOnChange = true)
            | XmlFile file  -> bldr.AddXmlFile(file, optional = true, reloadOnChange = true)
            |> ignore

        if conf.InMemory.Keys.Count > 0 then
            bldr.AddInMemoryCollection(conf.InMemory) |> ignore

        bldr.Build() :> IConfiguration

    /// Set the base path of the ConfigurationBuilder.
    [<CustomOperation("base_path")>]
    member _.SetBasePath (conf : ConfigBuilderSpec, basePath : string) =
        { conf with BasePath = basePath }

    /// Add Environment Variables to the ConfigurationBuilder.
    [<CustomOperation("add_env")>]
    member _.AddEnvVars (conf : ConfigBuilderSpec) =
        { conf with AddEnvVars = true }

    /// Add an in-memory collection to the ConfigurationBuilder.
    ///
    /// Note: This is operation replaces the existing In Memory Collection.
    [<CustomOperation("in_memory")>]
    member _.AddInMemoryValues (conf : ConfigBuilderSpec, pairs : (string * string) seq) =
        let inMemory = Map.ofSeq pairs
        { conf with InMemory = inMemory }

    /// Add required config INI file to the ConfigurationBuilder.
    [<CustomOperation("required_ini")>]
    member _.AddRequiredIniFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with RequiredFiles = (IniFile filePath) :: conf.RequiredFiles }

    /// Add optional config INI file to the ConfigurationBuilder.
    [<CustomOperation("optional_ini")>]
    member _.AddOptionalIniFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with OptionalFiles = (IniFile filePath) :: conf.OptionalFiles }

    /// Add required config XML file to the ConfigurationBuilder.
    [<CustomOperation("required_xml")>]
    member _.AddRequiredXmlFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with RequiredFiles = (XmlFile filePath) :: conf.RequiredFiles }

    /// Add optional config XML file to the ConfigurationBuilder.
    [<CustomOperation("optional_xml")>]
    member _.AddOptionalXmlFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with OptionalFiles = (XmlFile filePath) :: conf.OptionalFiles }

    /// Add required config JSON file to the ConfigurationBuilder.
    [<CustomOperation("required_json")>]
    member _.AddRequiredJsonFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with RequiredFiles = (JsonFile filePath) :: conf.RequiredFiles }

    /// Add optional config JSON file to the ConfigurationBuilder.
    [<CustomOperation("optional_json")>]
    member _.AddOptionalJsonFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with OptionalFiles = (JsonFile filePath) :: conf.OptionalFiles }

[<AutoOpen>]
module ConfigurationBuilder =
    /// Computation expression to allow for elegant IConfiguration construction
    let configuration args = ConfigBuilder(args)
