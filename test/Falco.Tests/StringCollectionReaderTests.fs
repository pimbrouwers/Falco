module Falco.Tests.StringCollectionReader

open System
open System.Collections.Generic
open Xunit
open Falco
open FsUnit.Xunit
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.Primitives

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

[<Fact>]
let ``StringCollectionReader value lookups are case-insensitive`` () =
    let values = 
        [ 
            "FString", [|"John Doe"; "Jane Doe"|] |> StringValues                
        ]
        |> Map.ofList
        |> fun m -> Dictionary(m)

    let scr = QueryCollectionReader(QueryCollection(values))

    // single values
    scr.TryGet "FSTRING" |> shouldBeSome (should equal "John Doe")
    scr.TryGet "FString" |> shouldBeSome (should equal "John Doe")
    scr.TryGet "fstriNG" |> shouldBeSome (should equal "John Doe")

    // arrays
    scr.TryArrayString "FSTRING" |> should equal [|"John Doe";"Jane Doe"|]
    scr.TryArrayString "fString" |> should equal [|"John Doe";"Jane Doe"|]
    scr.TryArrayString "fstriNG" |> should equal [|"John Doe";"Jane Doe"|]

[<Fact>] 
let ``Inline StringCollectionReader from form collection should resolve primitives`` () =
    let now = DateTime.Now.ToString()
    let offsetNow = DateTimeOffset.Now.ToString()
    let timespan = TimeSpan.FromSeconds(1.0).ToString()
    let guid = Guid().ToString()
     
    let values = 
        [ 
            "emptystring", [|""|]
            "fstring", [|"John Doe"; "Jane Doe"|]
            "fint16", [|"16";"17"|]
            "fint32", [|"32";"33"|]
            "fint64", [|"64";"65"|]
            "fbool", [|"true";"false"|]
            "ffloat", [|"1.234";"1.235"|]
            "fdecimal", [|"4.567";"4.568"|]
            "fdatetime", [|now|]
            "fdatetimeoffset", [|offsetNow|]
            "ftimespan", [|timespan|]
            "fguid", [|guid|]
        ]
        |> List.map (fun (k, v) -> k, v |> StringValues)
        |> Map.ofList
        |> fun m -> Dictionary(m)


    let scr = FormCollectionReader(FormCollection(values), Some (FormFileCollection() :> IFormFileCollection))        
    
    // single values
    scr.TryGetString "_fstring"                  |> shouldBeNone
    scr.TryGetString "fstring"                   |> shouldBeSome (should equal "John Doe")
    scr.TryGetStringNonEmpty "fstring"           |> shouldBeSome (should equal "John Doe")
    scr.TryGetInt16 "fint16"                     |> shouldBeSome (should equal 16s)
    scr.TryGetInt32 "fint32"                     |> shouldBeSome (should equal 32)
    scr.TryGetInt "fint32"                       |> shouldBeSome (should equal 32)
    scr.TryGetInt64 "fint64"                     |> shouldBeSome (should equal 64L)
    scr.TryGetBoolean "fbool"                    |> shouldBeSome (should equal true)
    scr.TryGetFloat "ffloat"                     |> shouldBeSome (should equal 1.234)
    scr.TryGetDecimal "fdecimal"                 |> shouldBeSome (should equal 4.567M)
    scr.TryGetDateTime "fdatetime"               |> shouldBeSome (should equal (DateTime.Parse(now)))
    scr.TryGetDateTimeOffset "fdatetimeoffset"   |> shouldBeSome (should equal (DateTimeOffset.Parse(offsetNow)))
    scr.TryGetTimeSpan "ftimespan"               |> shouldBeSome (should equal (TimeSpan.Parse(timespan)))
    scr.TryGetGuid "fguid"                       |> shouldBeSome (should equal (Guid.Parse(guid)))

    scr.GetString "fstring" "default_value"                         |> should equal "John Doe"
    scr.GetStringNonEmpty "emptystring" "default_value"             |> should equal "default_value"
    scr.GetInt16 "fint16" -1s                                       |> should equal 16s
    scr.GetInt32 "fint32" -1                                        |> should equal 32
    scr.GetInt "fint32" -1                                          |> should equal 32
    scr.GetInt64 "fint64" 1L                                        |> should equal 64L
    scr.GetBoolean "fbool" false                                    |> should equal true
    scr.GetFloat "ffloat" 0.0                                       |> should equal 1.234
    scr.GetDecimal "fdecimal" 0.0M                                  |> should equal 4.567M
    scr.GetDateTime "fdatetime" DateTime.MinValue                   |> should equal (DateTime.Parse(now))
    scr.GetDateTimeOffset "fdatetimeoffset" DateTimeOffset.MinValue |> should equal (DateTimeOffset.Parse(offsetNow))
    scr.GetTimeSpan "ftimespan" TimeSpan.MinValue                   |> should equal (TimeSpan.Parse(timespan))
    scr.GetGuid "fguid" Guid.Empty                                  |> should equal (Guid.Parse(guid))

    scr.GetString "_fstring" "default_value"                    |> should equal  "default_value"                    
    scr.GetStringNonEmpty "_fstring" "default_value"            |> should equal  "default_value"            
    scr.GetInt16 "_fint16" -1s                                  |> should equal  -1s                                  
    scr.GetInt32 "_fint32" -1                                   |> should equal  -1                                   
    scr.GetInt "_fint32" -1                                     |> should equal  -1                                     
    scr.GetInt64 "_fint64" 1L                                   |> should equal  1L                                   
    scr.GetBoolean "_fbool" false                               |> should equal  false                               
    scr.GetFloat "_ffloat" 0.0                                  |> should equal  0.0                                  
    scr.GetDecimal "_fdecimal" 0.0M                             |> should equal  0.0M                             
    scr.GetDateTime "_fdatetime" DateTime.MinValue              |> should equal  DateTime.MinValue   
    scr.GetDateTimeOffset "_fdatetimeoffset" DateTimeOffset.MinValue |> should equal  DateTimeOffset.MinValue
    scr.GetTimeSpan "_ftimespan" TimeSpan.MinValue              |> should equal  TimeSpan.MinValue              
    scr.GetGuid "_fguid" Guid.Empty                             |> should equal  Guid.Empty                             
    
    // array values
    scr.TryArrayString "_fstring"                |> should equal [||]
    scr.TryArrayString "fstring"                 |> should equal [|"John Doe";"Jane Doe"|]
    scr.TryArrayInt16 "fint16"                   |> should equal [|16s;17s|]
    scr.TryArrayInt32 "fint32"                   |> should equal [|32;33|]
    scr.TryArrayInt "fint32"                     |> should equal [|32;33|]
    scr.TryArrayInt64 "fint64"                   |> should equal [|64L;65L|]
    scr.TryArrayBoolean "fbool"                  |> should equal [|true;false|]
    scr.TryArrayFloat "ffloat"                   |> should equal [|1.234;1.235|]
    scr.TryArrayDecimal "fdecimal"               |> should equal [|4.567M;4.568M|]
    scr.TryArrayDateTime "fdatetime"             |> should equal [|DateTime.Parse(now)|]
    scr.TryArrayDateTimeOffset "fdatetimeoffset" |> should equal [|DateTimeOffset.Parse(offsetNow)|]
    scr.TryArrayTimeSpan "ftimespan"             |> should equal [|TimeSpan.Parse(timespan)|]
    scr.TryArrayGuid "fguid"                     |> should equal [|Guid.Parse(guid)|]
