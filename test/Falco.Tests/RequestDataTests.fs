module Falco.Tests.RequestData

open System
open System.Collections.Generic
open Xunit
open Falco
open FsUnit.Xunit
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing

[<Fact>]
let ``Can make QueryCollectionReader from IQueryCollection`` () =
    QueryCollectionReader(QueryCollection(Dictionary()))
    |> should not' throw

[<Fact>]
let ``Can make HeaderCollectionReader from HeaderDictionary`` () =
    HeaderCollectionReader(HeaderDictionary())
    |> should not' throw

[<Fact>]
let ``Can make RouteCollectionReader from RouteValueDictionary`` () =
    RouteCollectionReader(RouteValueDictionary(), QueryCollection(Dictionary()))
    |> should not' throw

[<Fact>]
let ``Can make FormCollectionReader from IFormCollection`` () =
    FormCollectionReader(FormCollection(Dictionary()), Some (FormFileCollection() :> IFormFileCollection))
    |> should not' throw