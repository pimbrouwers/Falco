# Configuration Builder

A thin wrapper around ConfigurationBuilder exposing a clean API for reading configuration values.

| Operation | Description |
| --------- | ----------- |
| [base_path] | Set the base path of the `ConfigurationBuilder`. |
| [add_env] | Add Environment Variables to the `ConfigurationBuilder`. |
| [config_file] | Add required [INI|JSON|XML] file to the `ConfigurationBuilder`. |
| [config_file_optional] | Add optional [INI|JSON|XML] file to the `ConfigurationBuilder`. |
| [add_dict] | Add an in-memory `Dictionary<string, string>` to the `ConfigurationBuilder`. |

```fsharp
open Falco.HostBuilder

[<EntryPoint>]
let main args =
    let env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")

    let config = configuration args {
        add_env
        config_file "appsettings.json"
        config_file_optional (String.Concat([|"appsettings."; env; ".json"|]))
    }

    // ...
```