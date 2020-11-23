[<AutoOpen>]
module Falco.StringCollectionReader

open System
open System.Collections.Generic
open Microsoft.Extensions.Primitives
open Falco.StringParser
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing

type StringCollectionReader internal (values : Map<string, string[]>) =

    new (kvpValues : KeyValuePair<string, StringValues> seq) =
        let map =
            kvpValues 
            |> Seq.map (fun kvp -> kvp.Key, kvp.Value.ToArray()) 
            |> Map.ofSeq

        StringCollectionReader(map)

    new (routeValues : RouteValueDictionary) =
        let map =
            routeValues
            |> Seq.map (fun kvp -> kvp.Key, [|Convert.ToString(kvp.Value, Globalization.CultureInfo.InvariantCulture)|])
            |> Map.ofSeq

        StringCollectionReader(map)

    /// Safely retrieve value
    member _.TryGetValue (name : string) =                 
        match values |> Map.tryFind name with
        | Some v when v.Length > 0 -> Some v
        | _                        -> None
    
    /// Retrieve value from StringCollectionReader
    member this.GetValue (name : string) = 
        match this.TryGetValue name with
        | Some v -> v 
        | None -> failwith (sprintf "Could not find %s" name)

    member this.TryGetString (name : string)                     = name |> this.TryGetValue |> Option.bind (fun v -> Some v.[0])
    member this.TryGetInt16 (name : string)                      = name |> this.TryGetValue |> Option.bind (fun v -> parseInt16 v.[0])
    member this.TryGetInt32 (name : string)                      = name |> this.TryGetValue |> Option.bind (fun v -> parseInt32 v.[0])
    member this.TryGetInt64 (name : string)                      = name |> this.TryGetValue |> Option.bind (fun v -> parseInt64 v.[0])
    member this.TryGetBoolean (name : string)                    = name |> this.TryGetValue |> Option.bind (fun v -> parseBoolean v.[0])
    member this.TryGetFloat (name : string)                      = name |> this.TryGetValue |> Option.bind (fun v -> parseFloat v.[0])
    member this.TryGetDecimal (name : string)                    = name |> this.TryGetValue |> Option.bind (fun v -> parseDecimal v.[0])
    member this.TryGetDateTime (name : string)                   = name |> this.TryGetValue |> Option.bind (fun v -> parseDateTime v.[0])
    member this.TryGetDateTimeOffset (name : string)             = name |> this.TryGetValue |> Option.bind (fun v -> parseDateTimeOffset v.[0])
    member this.TryGetGuid (name : string)                       = name |> this.TryGetValue |> Option.bind (fun v -> parseGuid v.[0])
    member this.TryGetTimeSpan (name : string)                   = name |> this.TryGetValue |> Option.bind (fun v -> parseTimeSpan v.[0])      
    member this.TryGetStringNonEmpty (name : string)             = match this.TryGetString name with Some x when x <> "" -> Some x | _ -> None
    member this.TryGet (name : string)                           = this.TryGetString name
    member this.TryGetInt (name : string)                        = this.TryGetInt32 name
    
    member this.GetString (name : string) defaultValue           = name |> this.TryGetString         |> Option.defaultValue defaultValue
    member this.GetStringNonEmpty (name : string) defaultValue   = name |> this.TryGetStringNonEmpty |> Option.defaultValue defaultValue
    member this.Get (name : string) defaultValue                 = name |> this.TryGet               |> Option.defaultValue defaultValue
    member this.GetInt16 (name : string) defaultValue            = name |> this.TryGetInt16          |> Option.defaultValue defaultValue
    member this.GetInt32 (name : string) defaultValue            = name |> this.TryGetInt32          |> Option.defaultValue defaultValue
    member this.GetInt (name : string) defaultValue              = name |> this.TryGetInt            |> Option.defaultValue defaultValue
    member this.GetInt64 (name : string) defaultValue            = name |> this.TryGetInt64          |> Option.defaultValue defaultValue
    member this.GetBoolean (name : string) defaultValue          = name |> this.TryGetBoolean        |> Option.defaultValue defaultValue
    member this.GetFloat (name : string) defaultValue            = name |> this.TryGetFloat          |> Option.defaultValue defaultValue
    member this.GetDecimal (name : string) defaultValue          = name |> this.TryGetDecimal        |> Option.defaultValue defaultValue
    member this.GetDateTime (name : string) defaultValue         = name |> this.TryGetDateTime       |> Option.defaultValue defaultValue
    member this.GetDateTimeOffset (name : string) defaultValue   = name |> this.TryGetDateTimeOffset |> Option.defaultValue defaultValue
    member this.GetGuid (name : string) defaultValue             = name |> this.TryGetGuid           |> Option.defaultValue defaultValue
    member this.GetTimeSpan (name : string) defaultValue         = name |> this.TryGetTimeSpan       |> Option.defaultValue defaultValue

    member this.TryArrayString (name : string)                   = name |> this.TryGetValue |> Option.map  (fun v -> v)
    member this.TryArrayInt16 (name : string)                    = name |> this.TryGetValue |> Option.bind (tryParseArray parseInt16)
    member this.TryArrayInt32 (name : string)                    = name |> this.TryGetValue |> Option.bind (tryParseArray parseInt32)
    member this.TryArrayInt64 (name : string)                    = name |> this.TryGetValue |> Option.bind (tryParseArray parseInt64)
    member this.TryArrayBoolean (name : string)                  = name |> this.TryGetValue |> Option.bind (tryParseArray parseBoolean)
    member this.TryArrayFloat (name : string)                    = name |> this.TryGetValue |> Option.bind (tryParseArray parseFloat)
    member this.TryArrayDecimal (name : string)                  = name |> this.TryGetValue |> Option.bind (tryParseArray parseDecimal)
    member this.TryArrayDateTime (name : string)                 = name |> this.TryGetValue |> Option.bind (tryParseArray parseDateTime)
    member this.TryArrayDateTimeOffset (name : string)           = name |> this.TryGetValue |> Option.bind (tryParseArray parseDateTimeOffset)
    member this.TryArrayGuid (name : string)                     = name |> this.TryGetValue |> Option.bind (tryParseArray parseGuid)
    member this.TryArrayTimeSpan (name : string)                 = name |> this.TryGetValue |> Option.bind (tryParseArray parseTimeSpan)
    member this.TryArrayInt (name : string)                      = this.TryArrayInt32 name

type FormCollectionReader (form : IFormCollection, files : IFormFileCollection option) =    
    inherit StringCollectionReader (form)
    member _.Files = files

type HeaderCollectionReader (headers : IHeaderDictionary) =    
    inherit StringCollectionReader (headers)

type QueryCollectionReader (query : IQueryCollection) =
    inherit StringCollectionReader (query)

type RouteCollectionReader (route : RouteValueDictionary) =
    inherit StringCollectionReader (route)

type private StringValues with 
    member this.AsString () =
        match this.Count with
        | 0 -> failwith "StringValues is empty"
        | _ -> this.[0]
    
    member this.AsInt16 ()               = this.AsString() |> parseOrFail parseInt16 "Not a valid Int16"
    member this.AsInt32 ()               = this.AsString() |> parseOrFail parseInt32 "Not a valid Int32"
    member this.AsInt ()                 = this.AsInt32 ()
    member this.AsInt64 ()               = this.AsString() |> parseOrFail parseInt64 "Not a valid Int64"
    member this.AsBoolean ()             = this.AsString() |> parseOrFail parseBoolean "Not a valid Boolean"
    member this.AsFloat ()               = this.AsString() |> parseOrFail parseFloat "Not a valid Float"
    member this.AsDecimal ()             = this.AsString() |> parseOrFail parseDecimal "Not a valid Decimal"
    member this.AsDateTime ()            = this.AsString() |> parseOrFail parseDateTime "Not a valid DateTime"
    member this.AsDateTimeOffset ()      = this.AsString() |> parseOrFail parseDateTimeOffset "Not a valid DateTimeOffset"
    member this.AsGuid ()                = this.AsString() |> parseOrFail parseGuid "Not a valid Guid"
    member this.AsTimeSpan ()            = this.AsString() |> parseOrFail parseTimeSpan "Not a valid TimeSpan"
    member this.AsArrayString ()         = this.ToArray()
    member this.AsArrayInt16 ()          = this.AsArrayString() |> tryParseArray parseInt16 |> Option.defaultValue [||]
    member this.AsArrayInt32 ()          = this.AsArrayString() |> tryParseArray parseInt32 |> Option.defaultValue [||]
    member this.AsArrayInt ()            = this.AsArrayInt32 ()
    member this.AsArrayInt64 ()          = this.AsArrayString() |> tryParseArray parseInt64 |> Option.defaultValue [||]
    member this.AsArrayBoolean ()        = this.AsArrayString() |> tryParseArray parseBoolean |> Option.defaultValue [||]
    member this.AsArrayFloat ()          = this.AsArrayString() |> tryParseArray parseFloat |> Option.defaultValue [||]
    member this.AsArrayDecimal ()        = this.AsArrayString() |> tryParseArray parseDecimal |> Option.defaultValue [||]
    member this.AsArrayDateTime ()       = this.AsArrayString() |> tryParseArray parseDateTime |> Option.defaultValue [||]
    member this.AsArrayDateTimeOffset () = this.AsArrayString() |> tryParseArray parseDateTimeOffset |> Option.defaultValue [||]
    member this.AsArrayGuid ()           = this.AsArrayString() |> tryParseArray parseGuid |> Option.defaultValue [||]
    member this.AsArrayTimeSpan ()       = this.AsArrayString() |> tryParseArray parseTimeSpan |> Option.defaultValue [||]



