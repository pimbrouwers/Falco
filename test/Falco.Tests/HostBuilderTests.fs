module Falco.Tests.HostBuilder

open System
open Falco.HostBuilder
open FsUnit.Xunit
open Microsoft.Extensions.Configuration
open Xunit

let configHasKey (key : string) (config : IConfiguration) =
    config.AsEnumerable()
    |> Seq.exists (fun x -> x.Key = key)
[<Fact>]
let ``args should be available`` () =
    let falcoArgVarKey = "FALCO_ARG_VAR"
    let falcoArgVar = "--" + falcoArgVarKey + "=TEST"
    let config : IConfiguration = configuration [|falcoArgVar|] {
        required_json "appsettings.json"
    }

    config
    |> configHasKey falcoArgVarKey
    |> should equal true

[<Fact>]
let ``add_env should add the environment variables`` () =
    let falcoEnvVar = "FALCO_ENV_VAR"
    Environment.SetEnvironmentVariable(falcoEnvVar, "TEST")
    let config : IConfiguration = configuration [||] {
        add_env
    }

    config
    |> configHasKey falcoEnvVar
    |> should equal true

[<Fact>]
let ``in_memory should add the collection of values`` () =
    let falcoMemVarKey = "FALCO_MEM_VAR"
    let values = [ falcoMemVarKey, "TEST" ]
    let config : IConfiguration = configuration [||] {
        in_memory values
    }
    config
    |> configHasKey falcoMemVarKey
    |> should equal true

[<Fact>]
let ``required_json should add the JSON file`` () =
    let falcoJsonVar = "FALCO_JSON_VAR"
    let config : IConfiguration = configuration [||] {
        required_json "appsettings.json"
    }

    config
    |> configHasKey falcoJsonVar
    |> should equal true

[<Fact>]
let ``optional_json should add the JSON file`` () =
    let falcoJsonVar = "FALCO_JSON_VAR"
    let config : IConfiguration = configuration [||] {
        optional_json "appsettings.json"
    }

    config
    |> configHasKey falcoJsonVar
    |> should equal true

[<Fact>]
let ``optional_json should not throw error for missing file`` () =
    let falcoJsonVar = "FALCO_JSON_VAR"
    let config : IConfiguration = configuration [||] {
        optional_json "fake.json"
    }

    config
    |> configHasKey falcoJsonVar
    |> should equal false