namespace Falco

open System
open System.Collections.Generic
open Falco.StringParser

/// A safe string collection reader, with type utilities.
type StringCollectionReader (values : Map<string, string[]>) =
    member private _.TryGetValue (name : string) =
        let found =
            values
            |> Map.tryPick (fun key value ->
                if StringUtils.strEquals key name then Some value else None)

        match found with
        | Some v when v.Length > 0 -> Some v
        | _                        -> None

    member private x.TryGetBind (binder : string -> 'T option) (name : string) =
        x.TryGetValue name |> Option.bind (fun ary -> binder ary.[0])

    member private x.TryGetMapArray (binder : string -> 'T option) (name : string) =
        x.TryGetValue name |> Option.map (tryParseArray binder) |> Option.defaultValue [||]

    /// The keys in the collection reader.
    member _.Keys = values.Keys

    /// Safely retrieve a collection of readers.
    ///
    /// Intended to be used with the "dot notation" collection wire format.
    /// (i.e., Person.First=John&Person.Last=Doe&Person.First=Jane&Person.Last=Doe)
    member _.GetChildren (name : string) =
        let childMap =
            values
            |> Map.filter (fun key _ ->
                key.StartsWith(name + ".", StringComparison.OrdinalIgnoreCase))

        if Map.isEmpty childMap then []
        else
            let dict = Dictionary<int, Dictionary<string, string list>>()

            for key in childMap.Keys do
                for i = 0 to childMap.[key].Length - 1 do
                    let newKey = key.Substring(name.Length + 1)
                    let newValue = childMap.[key].[i]

                    if dict.ContainsKey i && dict.[i].ContainsKey newKey then
                        dict.[i].[newKey] <- newValue :: dict.[i].[newKey]
                    elif dict.ContainsKey i then
                        dict.[i].Add(newKey,[newValue])
                    else
                        let newDict = Dictionary<string, string list>()
                        newDict.Add(newKey, [newValue])
                        dict.Add(i, newDict)

            [
                for KeyValue (_, childDict) in dict do
                    [
                        for KeyValue (key, value) in childDict do
                            key, Array.ofList value
                    ]
            ]
            |> List.map (Map.ofList >> StringCollectionReader)


    // Primitive returing Option<'T>

    /// Safely retrieve String option (alias for StringCollectionReader.TryGetString).
    member x.TryGet (name : string) =
        x.TryGetBind (fun v -> Some v) name

    /// Safely retrieve String option.
    member x.TryGetString (name : string) =
        x.TryGet name

    /// Safely retrieve non-empty String option.
    member x.TryGetStringNonEmpty (name : string) =
        x.TryGetBind (fun v -> parseNonEmptyString v) name

    /// Safely retrieve Int16 option.
    member x.TryGetInt16 (name : string) =
        x.TryGetBind (fun v -> parseInt16 v) name

    /// Safely retrieve Int32 option.
    member x.TryGetInt32 (name : string) =
        x.TryGetBind (fun v -> parseInt32 v) name

    /// Safely retrieve Int option.
    member x.TryGetInt (name : string) =
        x.TryGetInt32 name

    /// Safely retrieve Int64 option.
    member x.TryGetInt64 (name : string) =
        x.TryGetBind (fun v -> parseInt64 v) name

    /// Safely retrieve Boolean option.
    member x.TryGetBoolean (name : string) =
        x.TryGetBind (fun v -> parseBoolean v) name

    /// Safely retrieve Float option.
    member x.TryGetFloat (name : string) =
        x.TryGetBind (fun v -> parseFloat v) name

    /// Safely retrieve Decimal option.
    member x.TryGetDecimal (name : string) =
        x.TryGetBind (fun v -> parseDecimal v) name

    /// Safely retrieve DateTime option.
    member x.TryGetDateTime (name : string) =
        x.TryGetBind (fun v -> parseDateTime v) name

    /// Safely retrieve DateTimeOffset option.
    member x.TryGetDateTimeOffset (name : string) =
        x.TryGetBind (fun v -> parseDateTimeOffset v) name

    /// Safely retrieve Guid option.
    member x.TryGetGuid (name : string) =
        x.TryGetBind (fun v -> parseGuid v) name

    /// Safely retrieve TimeSpan option.
    member x.TryGetTimeSpan (name : string) =
        x.TryGetBind (fun v -> parseTimeSpan v) name


    // Primitives - Get or Default

    /// Safely retrieve named String or defaultValue.
    member x.Get (name : string, ?defaultValue : String) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue "")
            (x.TryGet name)

    /// Safely retrieve named string or defaultValue.
    member x.GetString (name : string, ?defaultValue : String) =
        x.Get (name, ?defaultValue=defaultValue)

    /// Safely retrieve named non-empty String or defaultValue.
    member x.GetStringNonEmpty (name : string, ?defaultValue : String) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue "")
            (x.TryGetStringNonEmpty name)

    /// Safely retrieve named Int16 or defaultValue.
    member x.GetInt16 (name : string, ?defaultValue : Int16) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0s)
            (x.TryGetInt16 name)

    /// Safely retrieve named Int32 or defaultValue.
    member x.GetInt32 (name : string, ?defaultValue : Int32) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0)
            (x.TryGetInt32 name)

    /// Safely retrieve named Int or defaultValue.
    member x.GetInt (name : string, ?defaultValue : Int32) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0)
            (x.TryGetInt name)

    /// Safely retrieve named Int64 or defaultValue.
    member x.GetInt64 (name : string, ?defaultValue : Int64) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0L)
            (x.TryGetInt64 name)

    /// Safely retrieve named Boolean or defaultValue.
    member x.GetBoolean (name : string, ?defaultValue : Boolean) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue false)
            (x.TryGetBoolean name)

    /// Safely retrieve named Float or defaultValue.
    member x.GetFloat (name : string, ?defaultValue : float) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0)
            (x.TryGetFloat name)

    /// Safely retrieve named Decimal or defaultValue.
    member x.GetDecimal (name : string, ?defaultValue : Decimal) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0M)
            (x.TryGetDecimal name)

    /// Safely retrieve named DateTime or defaultValue.
    member x.GetDateTime (name : string, ?defaultValue : DateTime) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue DateTime.MinValue)
            (x.TryGetDateTime name)

    /// Safely retrieve named DateTimeOffset or defaultValue.
    member x.GetDateTimeOffset (name : string, ?defaultValue : DateTimeOffset) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue DateTimeOffset.MinValue)
            (x.TryGetDateTimeOffset name)

    /// Safely retrieve named Guid or defaultValue.
    member x.GetGuid (name : string, ?defaultValue : Guid) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue Guid.Empty)
            (x.TryGetGuid name)

    /// Safely retrieve named TimeSpan or defaultValue.
    member x.GetTimeSpan (name : string, ?defaultValue : TimeSpan) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue TimeSpan.MinValue)
            (x.TryGetTimeSpan name)


    // Array Primitives

    /// Safely retrieve the named String[].
    member x.GetStringArray (name : string) =
        x.TryGetMapArray Some name

    /// Safely retrieve the named String[].
    member x.GetArray (name : string) =
        x.TryGetMapArray Some name

    /// Safely retrieve the named String[] excluding empty & null values.
    member x.GetStringNonEmptyArray (name : string) =
        x.TryGetMapArray parseNonEmptyString name

    /// Safely retrieve the named Int16[].
    member x.GetInt16Array (name : string) =
        x.TryGetMapArray parseInt16 name

    /// Safely retrieve the named Int32[].
    member x.GetInt32Array (name : string) =
        x.TryGetMapArray parseInt32 name

    /// Safely retrieve the named Int[] (alias for StringCollectionReader.TryArrayInt32).
    member x.GetIntArray (name : string) =
        x.GetInt32Array name

    /// Safely retrieve the named Int64[].
    member x.GetInt64Array (name : string) =
        x.TryGetMapArray parseInt64 name

    /// Safely retrieve the named Boolean[].
    member x.GetBooleanArray (name : string) =
        x.TryGetMapArray parseBoolean name

    /// Safely retrieve the named Float[].
    member x.GetFloatArray (name : string) =
        x.TryGetMapArray parseFloat name

    /// Safely retrieve the named Decimal[].
    member x.GetDecimalArray (name : string) =
        x.TryGetMapArray parseDecimal name

    /// Safely retrieve the named DateTime[].
    member x.GetDateTimeArray (name : string) =
        x.TryGetMapArray parseDateTime name

    /// Safely retrieve the named DateTimeOffset[].
    member x.GetDateTimeOffsetArray (name : string) =
        x.TryGetMapArray parseDateTimeOffset name

    /// Safely retrieve the named Guid[].
    member x.GetGuidArray (name : string) =
        x.TryGetMapArray parseGuid name

    /// Safely retrieve the named TimeSpan[].
    member x.GetTimeSpanArray (name : string) =
        x.TryGetMapArray parseTimeSpan name

// ------------
// Readers
// ------------
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing

/// Represents a readable collection of parsed form value.
[<Sealed>]
type FormCollectionReader (form : IFormCollection, files : IFormFileCollection option) =
    inherit StringCollectionReader(
        form
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value.ToArray())
        |> Map.ofSeq)

    /// The IFormFileCollection submitted in the request.
    ///
    /// Note: Only Some if form enctype="multipart/form-data".
    member _.Files = files

    /// Safely retrieve the named IFormFile option from the IFormFileCollection.
    member x.TryGetFormFile (name : string) =
        if StringUtils.strEmpty name then None
        else
            match x.Files with
            | None       -> None
            | Some files ->
                let file = files.GetFile name

                if isNull(file) then None else Some file

/// Represents a readable collection of parsed HTTP header values.
[<Sealed>]
type HeaderCollectionReader (headers : IHeaderDictionary) =
    inherit StringCollectionReader (
        headers
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value.ToArray())
        |> Map.ofSeq)

/// Represents a readble collection of query string values.
[<Sealed>]
type QueryCollectionReader (query : IQueryCollection) =
    inherit StringCollectionReader (
        query
        |> Seq.map (fun kvp -> kvp.Key, kvp.Value.ToArray())
        |> Map.ofSeq)

/// Represents a readble collection of route values.
[<Sealed>]
type RouteCollectionReader (route : RouteValueDictionary, query : IQueryCollection) =
    inherit StringCollectionReader (
        route
        |> Seq.map (fun kvp ->
            kvp.Key,
            [|Convert.ToString(kvp.Value, Globalization.CultureInfo.InvariantCulture)|])
        |> Map.ofSeq)

    member _.Query = QueryCollectionReader(query)

/// Represents a readble collection of cookie values.
[<Sealed>]
type CookieCollectionReader (cookies: IRequestCookieCollection) =
    inherit StringCollectionReader(
        cookies
        |> Seq.map (fun kvp -> kvp.Key, [|kvp.Value|])
        |> Map.ofSeq)