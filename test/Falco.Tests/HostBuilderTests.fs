module Falco.Tests.HostBuilder

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.Json
open Falco
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsUnit.Xunit
open NSubstitute
open Xunit
open Microsoft.AspNetCore.Routing
open Microsoft.Net.Http.Headers
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives

[<Fact>]
let ``xyz`` () = true