module Falco.Tests.RequestValue

open Falco
open FsUnit.Xunit
open Xunit

[<Fact>]
let ``RequestValue should return empty RObject for incomplete request body`` () =
    let expected = RObject []
    ""
    |> RequestValue.parseString
    |> should equal expected

[<Fact>]
let ``RequestValue should parse simple pair`` () =
    let expected = RObject [ "name", RString "john doe" ]

    "name=john%20doe"
    |> RequestValue.parseString
    |> should equal expected

[<Fact>]
let ``RequestValue should parse multiple simple pairs`` () =
    let expected = RObject [
        "season", RString "summer"
        "orders", RNumber 2 ]

    "season=summer&orders=2"
    |> RequestValue.parseString
    |> should equal expected

[<Fact>]
let ``RequestValue should parse explicit list`` () =
    let expected = RObject [
        "season", RList [ RString "summer"; RString "winter" ] ]
    "season[]=summer&season[]=winter"
    |> RequestValue.parseString
    |> should equal expected

[<Fact>]
let ``RequestValue should parse indexed list`` () =
    let expected = RObject [
        "season", RList [ RString "summer"; RString "winter" ] ]
    "season[0]=summer&season[1]=winter"
    |> RequestValue.parseString
    |> should equal expected

[<Fact>]
let ``RequestValue should parse out-of-order indexed list`` () =
    let expected = RObject [
        "season", RList [ RString "summer"; RString "winter" ] ]
    "season[1]=winter&season[0]=summer"
    |> RequestValue.parseString
    |> should equal expected

[<Fact>]
let ``RequestValue should parse jagged indexed list`` () =
    let expected = RObject [
        "season", RList [ RString "summer"; RNull; RString "winter" ] ]
    "season[0]=summer&season[2]=winter"
    |> RequestValue.parseString
    |> should equal expected

[<Fact>]
let ``RequestValue should parse out-of-order, jagged indexed list`` () =
    let expected = RObject [
        "season", RList [ RString "summer"; RNull; RString "winter" ] ]
    "season[2]=winter&season[0]=summer"
    |> RequestValue.parseString
    |> should equal expected

[<Fact>]
let ``RequestValue should parse object with indexed list`` () =
    let expected = RObject [
        "user", RObject [
            "name", RString "john doe"
            "hobbies", RList [ RString "cycling"; RString "hiking" ] ] ]
    "user.name=john%20doe&user.hobbies[0]=cycling&user.hobbies[1]=hiking"
    |> RequestValue.parseString
    |> should equal expected

[<Fact>]
let ``RequestValue should parse complex`` () =
    let expected = RObject [
        "season", RString "summer"
        "orders", RNumber 2
        "tags", RList [ RString "clothing"; RString "shoes"]
        "user", RObject [
            "name", RString "john"
            "age", RNumber 97
            "hobbies", RList [ RString "cycling"; RString "hiking" ]
            "cards", RList [
                RObject [
                    "num", RNumber 123
                    "kind", RString "visa" ]
                RObject [
                    "num", RNumber 456
                    "kind", RString "visa" ] ] ] ]

    let requestValue =
        seq {
            "season", seq { "summer" }
            "orders", seq { "2" }
            "tags[]", seq { "clothing"; "shoes" }
            "user.name", seq { "john" }
            "user.age", seq { "97" }
            "user.hobbies[]", seq { "cycling"; "hiking" }
            "user.cards[].num", seq { "123"; "456" }
            "user.cards[].kind", seq { "visa"; "amex" }
        }
        |> dict
        |> RequestValue.parse

    requestValue |> should equal expected