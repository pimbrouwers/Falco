namespace Falco.HostBuilder

open System.IO
open Microsoft.Extensions.Configuration

type ConfigFile =
    | IniFile of path : string
    | XmlFile of path : string
    | JsonFile of path : string


type ConfigBuilderSpec =
    { AddEnvVars     : bool
      BasePath       : string
      RequiredFiles  : ConfigFile list
      OptionalFiles  : ConfigFile list
      InMemory       : Map<string, string>
      AddUserSecrets : bool}

    static member Empty =
        { AddEnvVars     = false
          BasePath       = Directory.GetCurrentDirectory()
          RequiredFiles  = []
          OptionalFiles  = []
          InMemory       = Map.empty
          AddUserSecrets = false }

/// Computation expression to allow for elegant IConfiguration construction.
type ConfigBuilder (args : string[]) =
    member _.Yield(_) = ConfigBuilderSpec.Empty

    member _.Run(conf : ConfigBuilderSpec) =
        let mutable bldr = ConfigurationBuilder().SetBasePath(conf.BasePath)

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

        if conf.AddUserSecrets then
            bldr.AddUserSecrets() |> ignore

        if conf.AddEnvVars then
            bldr.AddEnvironmentVariables() |> ignore

        bldr.AddCommandLine(args) |> ignore

        bldr.Build() :> IConfiguration

    /// Sets the base path of the ConfigurationBuilder.
    [<CustomOperation("base_path")>]
    member _.SetBasePath (conf : ConfigBuilderSpec, basePath : string) =
        { conf with BasePath = basePath }

    /// Adds Environment Variables to the ConfigurationBuilder.
    [<CustomOperation("add_env")>]
    member _.AddEnvVars (conf : ConfigBuilderSpec) =
        { conf with AddEnvVars = true }

    /// Adds an in-memory collection to the ConfigurationBuilder.
    ///
    /// Note: This is operation replaces the existing In Memory Collection.
    [<CustomOperation("in_memory")>]
    member _.AddInMemoryValues (conf : ConfigBuilderSpec, pairs : (string * string) seq) =
        let inMemory = Map.ofSeq pairs
        { conf with InMemory = inMemory }

    /// Adds required config INI file to the ConfigurationBuilder.
    [<CustomOperation("required_ini")>]
    member _.AddRequiredIniFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with RequiredFiles = (IniFile filePath) :: conf.RequiredFiles }

    /// Adds optional config INI file to the ConfigurationBuilder.
    [<CustomOperation("optional_ini")>]
    member _.AddOptionalIniFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with OptionalFiles = (IniFile filePath) :: conf.OptionalFiles }

    /// Adds required config XML file to the ConfigurationBuilder.
    [<CustomOperation("required_xml")>]
    member _.AddRequiredXmlFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with RequiredFiles = (XmlFile filePath) :: conf.RequiredFiles }

    /// Adds optional config XML file to the ConfigurationBuilder.
    [<CustomOperation("optional_xml")>]
    member _.AddOptionalXmlFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with OptionalFiles = (XmlFile filePath) :: conf.OptionalFiles }

    /// Adds required config JSON file to the ConfigurationBuilder.
    [<CustomOperation("required_json")>]
    member _.AddRequiredJsonFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with RequiredFiles = (JsonFile filePath) :: conf.RequiredFiles }

    /// Adds optional config JSON file to the ConfigurationBuilder.
    [<CustomOperation("optional_json")>]
    member _.AddOptionalJsonFile (conf : ConfigBuilderSpec, filePath : string) =
        { conf with OptionalFiles = (JsonFile filePath) :: conf.OptionalFiles }

    ///Adds optional user secrets to the ConfigurationBuilder.
    [<CustomOperation("optional_user_secrets")>]
    member _.AddOptionalUserSecrets (conf : ConfigBuilderSpec) =
        { conf with AddUserSecrets = true }

[<AutoOpen>]
module ConfigurationBuilder =
    /// Computation expression to allow for elegant IConfiguration construction.
    let configuration args = ConfigBuilder(args)
