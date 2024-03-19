module Falco.Tests.FormValue

open System
open System.Collections.Generic
open System.IO
open System.Threading
open System.Threading.Tasks
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
        "orders", FNumber 2 ]

    "season=summer&orders=2"
    |> FormValue.parse
    |> should equal expected

[<Fact>]
let ``FormValue should parse complex`` () =
    let expected = FObject [
        "season", FString "summer"
        "orders", FNumber 2
        "tags", FList [ FString "clothing"; FString "shoes"]
        "user", FObject [
            "name", FString "john"
            "age", FNumber 97
            "hobbies", FList [ FString "cycling"; FString "hiking" ]
            "cards", FList [
                FObject [
                    "num", FNumber 123
                    "kind", FString "visa" ]
                FObject [
                    "num", FNumber 456
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

    let formData = FormData(FormCollection(formDict), None)
    formData.Values |> should equal expected

[<Fact>]
let ``Can make FormData from IFormCollection`` () =
    FormData(FormCollection(Dictionary()), Some (FormFileCollection() :> IFormFileCollection))
    |> should not' throw

[<Fact>]
let ``Can safely get IFormFile from IFormCollection`` () =
    let formFileName = "abc.txt"

    let emptyFormData = FormData(FormCollection(Dictionary()), Some (FormFileCollection() :> IFormFileCollection))
    emptyFormData.TryGetFile formFileName
    |> shouldBeNone

    let formFile =
        { new IFormFile with
            member _.ContentDisposition = String.Empty
            member _.ContentType = String.Empty
            member _.FileName = String.Empty
            member _.Headers = HeaderDictionary()
            member _.Length = Int64.MinValue
            member _.Name = formFileName
            member _.CopyTo (target: Stream) : unit = ()
            member _.CopyToAsync (target: Stream, cancellationToken: CancellationToken) : Task = Task.CompletedTask
            member _.OpenReadStream  () :  Stream = System.IO.Stream.Null }

    let formFileCollection = FormFileCollection()
    formFileCollection.Add(formFile)
    let formFileData = new FormData(FormCollection(Dictionary()), Some(formFileCollection))

    emptyFormData.TryGetFile formFileName
    |> shouldBeNone

type City = { Name : string; YearFounded : int option }
type CityResult = { Count : int; Results : City list }
type Weather = { Season : string; Temperature : float; Effects : string list; Cities : CityResult }

[<Fact>]
let ``FormValue extensions should work`` () =
    let expected =
        { Season = "summer"
          Temperature = 23.5
          Effects = [ "overcast"; "wind gusts" ]
          Cities = {
            Count = 2
            Results = [ { Name = "Toronto"; YearFounded = Some 123 }; { Name = "Tokyo"; YearFounded = None } ] } }

    let f = FObject [
        "season", FString "summer"
        "temperature", FNumber 23.5
        "effects", FList [ FString "overcast"; FString "wind gusts"]
        "cities", FObject [
            "count", FNumber 2
            "results", FList [
                FObject [ "name", FString "Toronto"; "year_founded", FNumber 123 ]
                FObject [ "name", FString "Tokyo" ] ] ] ]

    { Season = f?season.AsString()
      Temperature = f?temperature.AsFloat()
      Effects = [
        for e in f?effects.AsList() do
            e.AsString() ]
      Cities = {
        Count = f?cities?count.AsInt()
        Results = [
            for c in f?cities?results.AsList() do
                { Name = c?name.AsString()
                  YearFounded = c?year_founded.AsIntOption() } ] } }
    |> should equal expected