module Falco.Tests.HostBuilder

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.Json
open Falco.HostBuilder
open FsUnit.Xunit
open Microsoft.Extensions.Configuration

[<Fact>]
let ``args should be available`` () =
    let falcoArgVarKey = "FALCO_ARG_VAR"
    let falcoArgVar = "--" + falcoArgVarKey + "=TEST"
    let config : IConfiguration = configuration [|falcoArgVar|] {
        add_env
    }

    config.AsEnumerable()
    |> Seq.exists (fun x -> x.Key = falcoArgVarKey)
    |> should equal true

[<Fact>]
let ``add_env should add the environment variables`` () =
    let falcoEnvVar = "FALCO_ENV_VAR"
    Environment.SetEnvironmentVariable(falcoEnvVar, "TEST")
    let config : IConfiguration = configuration [||] {
        add_env
    }

    config.AsEnumerable()
    |> Seq.exists (fun x -> x.Key = falcoEnvVar)
    |> should equal true