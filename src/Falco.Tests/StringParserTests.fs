module Falco.Tests.StringParser

open System
open Xunit
open FsUnit.Xunit

module StringParser = 
    open Falco.StringParser

    [<Theory>]    
    [<InlineData("1", 1)>]    
    [<InlineData("2147483647", 2147483647)>]
    [<InlineData("-2147483648", -2147483648)>]
    let ``parseInt should be some`` toParse result =
        toParse
        |> parseInt
        |> should equal (Some result)

    [<Theory>]
    [<InlineData("")>]
    [<InlineData("2147483648")>]
    [<InlineData("-2147483649")>]
    let ``parseInt should be none`` toParse =
        toParse 
        |> parseInt
        |> should equal None

    [<Theory>]
    [<InlineData("1", 1s)>]
    [<InlineData("32767", Int16.MaxValue)>]
    [<InlineData("-32768", Int16.MinValue)>]
    let ``parseInt16 should be some`` toParse result =
        toParse
        |> parseInt16
        |> should equal (Some result)

    [<Theory>]
    [<InlineData("")>]
    [<InlineData("32768")>]
    [<InlineData("-32769")>]
    let ``parseInt16 should be none`` toParse =
        toParse
        |> parseInt16
        |> should equal None

    [<Theory>]
    [<InlineData("1", 1L)>]
    [<InlineData("9223372036854775807", Int64.MaxValue)>]
    [<InlineData("-9223372036854775808", Int64.MinValue)>]
    let ``parseInt64 should be some`` toParse result =
        toParse
        |> parseInt64
        |> should equal (Some result)

    [<Theory>]
    [<InlineData("")>]
    [<InlineData("9223372036854775808")>]
    [<InlineData("-9223372036854775809")>]
    let ``parseInt64 should be none`` toParse =
        toParse
        |> parseInt64
        |> should equal None

    [<Theory>]
    [<InlineData("true", true)>]
    [<InlineData("false", false)>]
    let ``parseBool should be some`` toParse result =
        toParse
        |> parseBoolean
        |> should equal (Some result)

    [<Theory>]
    [<InlineData("")>]
    [<InlineData("treue")>]
    [<InlineData("fallse")>]
    let ``parseBool should be none`` toParse =
        toParse
        |> parseBoolean
        |> should equal None

    [<Theory>]
    [<InlineData("99.99", 99.99)>]    
    [<InlineData("1", 1.0)>]
    let ``parseFloat should be some`` toParse result = 
        toParse
        |> parseFloat
        |> should equal (Some result)

    [<Theory>]
    [<InlineData("falco")>]
    [<InlineData("")>]
    let ``parseFloat should be none`` toParse = 
        toParse
        |> parseFloat
        |> should equal None

    [<Theory>]
    [<InlineData("99.99", 99.99)>]    
    [<InlineData("1", 1.0)>]
    let ``parseDecimal should be some`` toParse (result : float) = 
        toParse
        |> parseDecimal
        |> should equal (Some (Convert.ToDecimal(result)))

    [<Theory>]
    [<InlineData("falco")>]
    [<InlineData("")>]
    let ``parseDecimal should be none`` toParse = 
        toParse
        |> parseDecimal
        |> should equal None


    [<Fact>]
    let ``parseDateTime should be some`` () =
        let dateStr = "2020-06-06 10:13:40 AM"
        dateStr
        |> parseDateTime
        |> should equal (Some (DateTime(2020, 6, 6, 10, 13, 40, 0)))

    [<Theory>]
    [<InlineData("falco")>]
    [<InlineData("")>]
    let ``parseDateTime should be none`` toParse = 
        toParse
        |> parseDateTime
        |> should equal None

    [<Fact>]
    let ``parseDatetimeOffset should be some`` () =
        let dateStr = "2020-06-06 10:13:40 AM -04:00"
        dateStr
        |> parseDateTimeOffset
        |> should equal (Some (DateTimeOffset(2020, 6, 6, 10, 13, 40, 0, TimeSpan(-4, 0, 0))))

    [<Theory>]
    [<InlineData("falco")>]
    [<InlineData("")>]
    let ``parseDateTimeOffset should be none`` toParse = 
        toParse
        |> parseDateTimeOffset
        |> should equal None

    [<Fact>]
    let ``parseTimeSpan should be some`` () =
        "00:00:01"
        |> parseTimeSpan
        |> should equal (Some (TimeSpan.FromSeconds(1.0)))

    [<Theory>]
    [<InlineData("falco")>]
    [<InlineData("")>]
    let ``parseTimeSpan should be none`` toParse = 
        toParse
        |> parseTimeSpan
        |> should equal None

    [<Fact>]
    let ``parseGuid should be some`` () =
        let guidStr = "8e0e2583-62cb-4812-9861-5759a1fb3eeb"
        let expected = Guid.Parse(guidStr);
        guidStr
        |> parseGuid
        |> should equal (Some expected)

    [<Theory>]
    [<InlineData("falco")>]
    [<InlineData("")>]
    let ``parseGuid should be none`` toParse = 
        toParse
        |> parseGuid
        |> should equal None


