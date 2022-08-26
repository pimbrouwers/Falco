# Configuration Builder

A thin wrapper around ConfigurationBuilder exposing a clean API for reading configuration values.

| Operation | Description |
| --------- | ----------- |
| [base_path] | Set the base path of the `ConfigurationBuilder`. |
| [add_env] | Add Environment Variables to the `ConfigurationBuilder`. |
| [required_json] | Add required [INI|JSON|XML] file to the `ConfigurationBuilder`. |
| [optional_json] | Add optional [INI|JSON|XML] file to the `ConfigurationBuilder`. |
| [add_dict] | Add an in-memory `Dictionary<string, string>` to the `ConfigurationBuilder`. |

```fsharp
open Falco.HostBuilder

[<EntryPoint>]
let main args =
    let env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")

    let config = configuration args {
        add_env
        required_json "appsettings.json"
        optional_json (String.Concat([|"appsettings."; env; ".json"|]))
    }

    // ...
```