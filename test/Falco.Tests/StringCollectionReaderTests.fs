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
    scr.TryGet "FSTRING"   |> Option.iter (should equal "John Doe")
    scr.TryGet "FString"   |> Option.iter (should equal "John Doe")
    scr.TryGet "fstriNG"   |> Option.iter (should equal "John Doe")

    // arrays
    scr.TryArrayString "FSTRING" |> Option.iter (should equal [|"John Doe";"Jane Doe"|])
    scr.TryArrayString "fString" |> Option.iter (should equal [|"John Doe";"Jane Doe"|])
    scr.TryArrayString "fstriNG" |> Option.iter (should equal [|"John Doe";"Jane Doe"|])

[<Fact>] 
let ``Inline StringCollectionReader from query collection should resolve primitives`` () =
    let now = DateTime.Now.ToString()
    let offsetNow = DateTimeOffset.Now.ToString()
    let timespan = TimeSpan.FromSeconds(1.0).ToString()
    let guid = Guid().ToString()
     
    let values = 
        [ 
            "fstring", [|"John Doe"; "Jane Doe"|] |> StringValues
            "fint16", [|"16";"17"|] |> StringValues
            "fint32", [|"32";"33"|] |> StringValues
            "fint64", [|"64";"65"|] |> StringValues
            "fbool", [|"true";"false"|] |> StringValues
            "ffloat", [|"1.234";"1.235"|] |> StringValues
            "fdecimal", [|"4.567";"4.568"|] |> StringValues
            "fdatetime", [|now|] |> StringValues
            "fdatetimeoffset", [|offsetNow|] |> StringValues
            "ftimespan", [|timespan|] |> StringValues
            "fguid", [|guid|] |> StringValues
        ]
        |> Map.ofList
        |> fun m -> Dictionary(m)

    let scr = QueryCollectionReader(QueryCollection(values))

    // single values
    scr.TryGetString "fstring"                   |> Option.iter (should equal "John Doe")
    scr.TryGetStringNonEmpty "fstring"           |> Option.iter (should equal "John Doe")
    scr.TryGetInt16 "fint16"                     |> Option.iter (should equal 16s)
    scr.TryGetInt32 "fint32"                     |> Option.iter (should equal 32)
    scr.TryGetInt "fint32"                       |> Option.iter (should equal 32)
    scr.TryGetInt64 "fint64"                     |> Option.iter (should equal 64L)
    scr.TryGetBoolean "fbool"                    |> Option.iter (should equal true)
    scr.TryGetFloat "ffloat"                     |> Option.iter (should equal 1.234)
    scr.TryGetDecimal "fdecimal"                 |> Option.iter (should equal 4.567M)
    scr.TryGetDateTime "fdatetime"               |> Option.iter (should equal (DateTime.Parse(now)))
    scr.TryGetDateTimeOffset "fdatetimeoffset"   |> Option.iter (should equal (DateTimeOffset.Parse(offsetNow)))
    scr.TryGetTimeSpan "ftimespan"               |> Option.iter (should equal (TimeSpan.Parse(timespan)))
    scr.TryGetGuid "fguid"                       |> Option.iter (should equal (Guid.Parse(guid)))

    scr.GetString "fstring" "default_value"                         |> should equal "John Doe"
    scr.GetStringNonEmpty "fstring" "default_value"                 |> should equal "John Doe"
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
    scr.TryArrayString "_fstring"                |> Option.iter (should equal [||])
    scr.TryArrayString "fstring"                 |> Option.iter (should equal [|"John Doe";"Jane Doe"|])
    scr.TryArrayInt16 "fint16"                   |> Option.iter (should equal [|16s;17s|])
    scr.TryArrayInt32 "fint32"                   |> Option.iter (should equal [|32;33|])
    scr.TryArrayInt "fint32"                     |> Option.iter (should equal [|32;33|])
    scr.TryArrayInt64 "fint64"                   |> Option.iter (should equal [|64L;65L|])
    scr.TryArrayBoolean "fbool"                  |> Option.iter (should equal [|true;false|])
    scr.TryArrayFloat "ffloat"                   |> Option.iter (should equal [|1.234;1.235|])
    scr.TryArrayDecimal "fdecimal"               |> Option.iter (should equal [|4.567M;4.568M|])
    scr.TryArrayDateTime "fdatetime"             |> Option.iter (should equal [|DateTime.Parse(now)|])
    scr.TryArrayDateTimeOffset "fdatetimeoffset" |> Option.iter (should equal [|DateTimeOffset.Parse(offsetNow)|])
    scr.TryArrayTimeSpan "ftimespan"             |> Option.iter (should equal [|TimeSpan.Parse(timespan)|])
    scr.TryArrayGuid "fguid"                     |> Option.iter (should equal [|Guid.Parse(guid)|])
