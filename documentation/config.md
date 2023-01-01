# Configuration Builder

A thin wrapper around ConfigurationBuilder exposing a clean API for reading configuration values.

| Operation | Description |
| --------- | ----------- |
| [base_path](#base_path) | Set the base path of the `ConfigurationBuilder`. |
| [add_env](#add_env) | Add Environment Variables to the `ConfigurationBuilder`. |
| [required_json](#required_json) | Add required JSON file to the `ConfigurationBuilder`. |
| [optional_json](#optional_json) | Add optional JSON file to the `ConfigurationBuilder`. |
| [required_ini](#required_ini) | Add required INI file to the `ConfigurationBuilder`. |
| [optional_ini](#optional_ini) | Add optional INI file to the `ConfigurationBuilder`. |
| [required_xml](#required_xml) | Add required XML file to the `ConfigurationBuilder`. |
| [optional_xml](#optional_xml) | Add optional XML file to the `ConfigurationBuilder`. |
| [in_memory](#in_memory) | Add an in-memory `Dictionary<string, string>` to the `ConfigurationBuilder`. |

## `base_path`

```fsharp
let config = configuration [||] {
    base_path "C:\\SOME\\PATH"
}
```

## `add_env`

```fsharp
let config = configuration [||] {
    add_env
}
```

## `required_json`

```fsharp
let config = configuration [||] {
    required_json "appsettings.json"
}
```

## `optional_json`

```fsharp
let env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")

let config = configuration [||] {
    optional_json (String.Concat([|"appsettings."; env; ".json"|]))
}
```

## `required_ini`

```fsharp
let config = configuration [||] {
    required_ini "appsettings.ini"
}
```

## `optional_ini`

```fsharp
let env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")

let config = configuration [||] {
    optional_ini (String.Concat([|"appsettings."; env; ".ini"|]))
}
```

## `required_xml`

```fsharp
let config = configuration [||] {
    required_json "appsettings.xml"
}
```

## `optional_xml`

```fsharp
let env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")

let config = configuration [||] {
    optional_xml (String.Concat([|"appsettings."; env; ".xml"|]))
}
```

## `in_memory`

```fsharp
let pairs = [ "FALCO_VAR", "TEST" ]

let config = configuration [||] {
    in_memory pairs
}
```
