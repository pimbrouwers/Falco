module Falco.Tests.Forms

open System.Collections.Generic
open Falco.Forms
open FsUnit.Xunit
open Xunit
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives

[<Fact>]
let ``FormValue should return empty FObject for incomplete request body`` () =
    let expected = FObject []
    ""
    |> FormValue.parse
    |> should equal expected 

[<Fact>]
let ``FormValue should parse simple pair`` () =
    let expected = FObject [ "name", FString "john doe" ]

    "name=john%20doe"
    |> FormValue.parse 
    |> should equal expected

[<Fact>]
let ``FormValue should parse multiple simple pairs`` () =
    let expected = FObject [ 
        "season", FString "summer"
        "orders", FInt 2 ]

    "season=summer&orders=2"
    |> FormValue.parse 
    |> should equal expected

[<Fact>]
let ``FormValue should parse complex`` () = 
    let expected = FObject [
        "season", FString "summer"
        "orders", FInt 2
        "tags", FList [ FString "clothing"; FString "shoes"]
        "user", FObject [
            "name", FString "john"
            "age", FInt 97
            "hobbies", FList [ FString "cycling"; FString "hiking" ]
            "cards", FList [
                FObject [
                    "num", FInt 123
                    "kind", FString "visa" ]
                FObject [
                    "num", FInt 456
                    "kind", FString "visa" ] ] ] ]

    let formDict = Dictionary<string, StringValues>()
    [
        "season", seq { "summer" }
        "orders", seq { "2" }
        "tags[]", seq { "clothing"; "shoes" }
        "user.name", seq { "john" }
        "user.age", seq { "97" }
        "user.hobbies[]", seq { "cycling"; "hiking" }
        "user.cards[].num", seq { "123"; "456" }
        "user.cards[].kind", seq { "visa"; "amex" }
    ]
    |> Seq.map (fun (k, v) -> k, StringValues(Array.ofSeq v))
    |> Seq.iter (fun kvp -> formDict.Add(kvp))

    FormCollection(formDict)
    |> FormValue.parseForm
    |> should equal expected

[<Fact>]
let ``Can make FormData from IFormCollection`` () =
    FormData(FormCollection(Dictionary()), Some (FormFileCollection() :> IFormFileCollection))
    |> should not' throw
