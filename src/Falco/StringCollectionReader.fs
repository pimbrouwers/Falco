[<AutoOpen>]
module Falco.StringCollectionReader

open System
open System.Collections.Generic
open Microsoft.Extensions.Primitives
open Falco.StringParser
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing

/// A safe string collection reader, with type utilities
[<AbstractClass>]
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

    new (cookies : IRequestCookieCollection) =
        let map =
            cookies
            |> Seq.map (fun kvp -> kvp.Key, [|kvp.Value|])
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

    /// Safely retrieve String option
    member this.TryGetString (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> Some v.[0])

    /// Safely retrieve Int16 option
    member this.TryGetInt16 (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> parseInt16 v.[0])

    /// Safely retrieve Int32 option
    member this.TryGetInt32 (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> parseInt32 v.[0])

    /// Safely retrieve Int64 option
    member this.TryGetInt64 (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> parseInt64 v.[0])

    /// Safely retrieve Boolean option
    member this.TryGetBoolean (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> parseBoolean v.[0])

    /// Safely retrieve Float option
    member this.TryGetFloat (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> parseFloat v.[0])

    /// Safely retrieve Decimal option
    member this.TryGetDecimal (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> parseDecimal v.[0])

    /// Safely retrieve DateTime option
    member this.TryGetDateTime (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> parseDateTime v.[0])

    /// Safely retrieve DateTimeOffset option
    member this.TryGetDateTimeOffset (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> parseDateTimeOffset v.[0])

    /// Safely retrieve Guid option
    member this.TryGetGuid (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> parseGuid v.[0])

    /// Safely retrieve TimeSpan option
    member this.TryGetTimeSpan (name : string) =
        name |> this.TryGetValue |> Option.bind (fun v -> parseTimeSpan v.[0])

    /// Safely retrieve non-empty String option
    member this.TryGetStringNonEmpty (name : string) =
        match this.TryGetString name with Some x when x <> "" -> Some x | _ -> None

    /// Safely retrieve String option
    member this.TryGet (name : string) =
        this.TryGetString name

    /// Safely retrieve Int option
    member this.TryGetInt (name : string) =
        this.TryGetInt32 name

    // ------------
    // Get or Default
    // ------------

    /// Safely retrieve named String or defaultValue
    member this.GetString (name : string) defaultValue =
        name |> this.TryGetString |> Option.defaultValue defaultValue

    /// Safely retrieve named non-empty String or defaultValue
    member this.GetStringNonEmpty (name : string) defaultValue =
        name |> this.TryGetStringNonEmpty |> Option.defaultValue defaultValue

    /// Safely retrieve named string or defaultValue
    member this.Get (name : string) defaultValue =
        name |> this.TryGet |> Option.defaultValue defaultValue

    /// Safely retrieve named Int16 or defaultValue
    member this.GetInt16 (name : string) defaultValue =
        name |> this.TryGetInt16 |> Option.defaultValue defaultValue

    /// Safely retrieve named Int32 or defaultValue
    member this.GetInt32 (name : string) defaultValue =
        name |> this.TryGetInt32 |> Option.defaultValue defaultValue

    /// Safely retrieve named Int or defaultValue
    member this.GetInt (name : string) defaultValue =
        name |> this.TryGetInt |> Option.defaultValue defaultValue

    /// Safely retrieve named Int64 or defaultValue
    member this.GetInt64 (name : string) defaultValue =
        name |> this.TryGetInt64 |> Option.defaultValue defaultValue

    /// Safely retrieve named Boolean or defaultValue
    member this.GetBoolean (name : string) defaultValue =
        name |> this.TryGetBoolean |> Option.defaultValue defaultValue

    /// Safely retrieve named Float or defaultValue
    member this.GetFloat (name : string) defaultValue =
        name |> this.TryGetFloat |> Option.defaultValue defaultValue

    /// Safely retrieve named Decimal or defaultValue
    member this.GetDecimal (name : string) defaultValue =
        name |> this.TryGetDecimal |> Option.defaultValue defaultValue

    /// Safely retrieve named DateTime or defaultValue
    member this.GetDateTime (name : string) defaultValue =
        name |> this.TryGetDateTime |> Option.defaultValue defaultValue

    /// Safely retrieve named DateTimeOffset or defaultValue
    member this.GetDateTimeOffset (name : string) defaultValue =
        name |> this.TryGetDateTimeOffset |> Option.defaultValue defaultValue

    /// Safely retrieve named Guid or defaultValue
    member this.GetGuid (name : string) defaultValue =
        name |> this.TryGetGuid |> Option.defaultValue defaultValue

    /// Safely retrieve named TimeSpan or defaultValue
    member this.GetTimeSpan (name : string) defaultValue =
        name |> this.TryGetTimeSpan |> Option.defaultValue defaultValue

    // ------------
    // Array Primitives
    // ------------

    /// Safely retrieve the named String[] option
    member this.TryArrayString (name : string) =
        name |> this.TryGetValue |> Option.map  (fun v -> v)

    /// Safely retrieve the named Int16[] option
    member this.TryArrayInt16 (name : string) =
        name |> this.TryGetValue |> Option.bind (tryParseArray parseInt16)

    /// Safely retrieve the named Int32[] option
    member this.TryArrayInt32 (name : string) =
        name |> this.TryGetValue |> Option.bind (tryParseArray parseInt32)

    /// Safely retrieve the named Int64[] option
    member this.TryArrayInt64 (name : string) =
        name |> this.TryGetValue |> Option.bind (tryParseArray parseInt64)

    /// Safely retrieve the named Boolean[] option
    member this.TryArrayBoolean (name : string) =
        name |> this.TryGetValue |> Option.bind (tryParseArray parseBoolean)

    /// Safely retrieve the named Float[] option
    member this.TryArrayFloat (name : string) =
        name |> this.TryGetValue |> Option.bind (tryParseArray parseFloat)

    /// Safely retrieve the named Decimal[] option
    member this.TryArrayDecimal (name : string) =
        name |> this.TryGetValue |> Option.bind (tryParseArray parseDecimal)

    /// Safely retrieve the named DateTime[] option
    member this.TryArrayDateTime (name : string) =
        name |> this.TryGetValue |> Option.bind (tryParseArray parseDateTime)

    /// Safely retrieve the named DateTimeOffset[] option
    member this.TryArrayDateTimeOffset (name : string) =
        name |> this.TryGetValue |> Option.bind (tryParseArray parseDateTimeOffset)

    /// Safely retrieve the named Guid[] option
    member this.TryArrayGuid (name : string) =
        name |> this.TryGetValue |> Option.bind (tryParseArray parseGuid)

    /// Safely retrieve the named TimeSpan[] option
    member this.TryArrayTimeSpan (name : string) =
        name |> this.TryGetValue |> Option.bind (tryParseArray parseTimeSpan)

    /// Safely retrieve the named Int[] option
    member this.TryArrayInt (name : string) =
        this.TryArrayInt32 name

/// Represents a readable collection of parsed form value
type FormCollectionReader (form : IFormCollection, files : IFormFileCollection option) =
    inherit StringCollectionReader (form)

    /// The IFormFileCollection submitted in the request.
    /// 
    /// Note: Only present if form enctype="multipart/form-data".
    member _.Files = files

    /// Safely retrieve the named IFormFile option from the IFormFileCollection
    member this.TryGetFormFile (name : string) =        
        if StringUtils.strEmpty name then None 
        else 
            match this.Files with
            | None       -> None
            | Some files ->                
                let file = files.GetFile name

                if isNull(file) then None else Some file

/// Represents a readable collection of parsed HTTP header values
type HeaderCollectionReader (headers : IHeaderDictionary) =
    inherit StringCollectionReader (headers)

/// Represents a readble collection of query string values
type QueryCollectionReader (query : IQueryCollection) =
    inherit StringCollectionReader (query)

/// Represents a readble collection of route values
type RouteCollectionReader (route : RouteValueDictionary, query : IQueryCollection) =
    inherit StringCollectionReader (route)
    member _.Query = QueryCollectionReader(query)

/// Represents a readble collection of cookie values
type CookieCollectionReader (cookies: IRequestCookieCollection) =
    inherit StringCollectionReader(cookies)