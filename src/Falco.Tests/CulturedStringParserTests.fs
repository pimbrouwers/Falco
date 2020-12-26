module Falco.Tests.CulturedStringParser

open System
open Xunit
open FsUnit.Xunit

module CulturedStringParser = 
    open Falco.CulturedStringParser

    [<Theory>]
    [<InlineData("1", 1s)>]
    [<InlineData("32767", Int16.MaxValue)>]
    [<InlineData("-32768", Int16.MinValue)>]
    let ``parseInt16 should be some`` toParse result =
        toParse
        |> parseInt16 ""
        |> should equal (Some result)

    [<Theory>]
    [<InlineData("")>]
    [<InlineData("32768")>]
    [<InlineData("-32769")>]
    let ``parseInt16 should be none`` toParse =
        toParse
        |> parseInt16 ""
        |> should equal None

    [<Theory>]    
    [<InlineData("1", 1)>]    
    [<InlineData("2147483647", 2147483647)>]
    [<InlineData("-2147483648", -2147483648)>]
    let ``parseInt should be some`` toParse result =
        toParse 
        |> parseInt ""
        |> should equal (Some result)

    [<Theory>]
    [<InlineData("")>]
    [<InlineData("2147483648")>]
    [<InlineData("-2147483649")>]
    let ``parseInt should be none`` toParse =
        toParse
        |> parseInt ""
        |> should equal None
