namespace Falco

open System
open System.Collections.Generic

/// A safe string collection reader, with type utilities.
type StringCollectionReader (values : IDictionary<string, string seq>) =
    let valuesLookup = Dictionary(values, StringComparer.OrdinalIgnoreCase)

    let tryGetValue (name : string) =
        match valuesLookup.ContainsKey name with
        | true when not(Seq.isEmpty valuesLookup[name]) -> Some valuesLookup[name]
        | _ -> None

    let tryGetBind (binder : string -> 'T option) (name : string) =
        tryGetValue name
        |> Option.bind (Seq.tryHead >> Option.bind binder)

    let tryGetMapArray (binder : string -> 'T option) (name : string) =
        let tryParseSeq (parser : string -> 'b option) seq =
            seq
            |> Seq.fold (fun (acc : List<'b>) (a : string) ->
                // accumulate successful parses
                match parser a with
                | Some b ->
                    acc.Add(b) |> ignore
                    acc
                | None -> acc) (List<'b>())
            |> Seq.cast

        tryGetValue name
        |> Option.map (tryParseSeq binder)
        |> Option.defaultValue Seq.empty
        |> Array.ofSeq

    /// The keys in the collection reader.
    member _.Keys = valuesLookup.Keys

    // ------------
    // Primitive returning Option<'T>
    // ------------

    /// Safely retrieves String option (alias for StringCollectionReader.TryGetString).
    member _.TryGet (name : string) =
        tryGetBind Some name

    /// Safely retrieves String option.
    member x.TryGetString (name : string) =
        x.TryGet name

    /// Safely retrieves non-empty String option.
    member _.TryGetStringNonEmpty (name : string) =
        tryGetBind StringParser.parseNonEmptyString name

    /// Safely retrieves Int16 option.
    member _.TryGetInt16 (name : string) =
        tryGetBind StringParser.parseInt16 name

    /// Safely retrieves Int32 option.
    member _.TryGetInt32 (name : string) =
        tryGetBind StringParser.parseInt32 name

    /// Safely retrieves Int option.
    member x.TryGetInt (name : string) =
        x.TryGetInt32 name

    /// Safely retrieves Int64 option.
    member _.TryGetInt64 (name : string) =
        tryGetBind StringParser.parseInt64 name

    /// Safely retrieves Boolean option.
    member _.TryGetBoolean (name : string) =
        tryGetBind StringParser.parseBoolean name

    /// Safely retrieves Float option.
    member _.TryGetFloat (name : string) =
        tryGetBind StringParser.parseFloat name

    /// Safely retrieves Decimal option.
    member _.TryGetDecimal (name : string) =
        tryGetBind StringParser.parseDecimal name

    /// Safely retrieves DateTime option.
    member _.TryGetDateTime (name : string) =
        tryGetBind StringParser.parseDateTime name

    /// Safely retrieves DateTimeOffset option.
    member _.TryGetDateTimeOffset (name : string) =
        tryGetBind StringParser.parseDateTimeOffset name

    /// Safely retrieves Guid option.
    member _.TryGetGuid (name : string) =
        tryGetBind StringParser.parseGuid name

    /// Safely retrieves TimeSpan option.
    member _.TryGetTimeSpan (name : string) =
        tryGetBind StringParser.parseTimeSpan name


    // ------------
    // Primitives - Get or Default
    // ------------

    /// Safely retrieves named String or defaultValue.
    member x.Get (name : string, ?defaultValue : String) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue "")
            (x.TryGet name)

    /// Safely retrieves named String or defaultValue.
    member x.GetString (name : string, ?defaultValue : String) =
        x.Get (name, ?defaultValue=defaultValue)

    /// Safely retrieves named non-empty String or defaultValue.
    member x.GetStringNonEmpty (name : string, ?defaultValue : String) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue "")
            (x.TryGetStringNonEmpty name)

    /// Safely retrieves named Int16 or defaultValue.
    member x.GetInt16 (name : string, ?defaultValue : Int16) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0s)
            (x.TryGetInt16 name)

    /// Safely retrieves named Int32 or defaultValue.
    member x.GetInt32 (name : string, ?defaultValue : Int32) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0)
            (x.TryGetInt32 name)

    /// Safely retrieves named Int or defaultValue.
    member x.GetInt (name : string, ?defaultValue : Int32) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0)
            (x.TryGetInt name)

    /// Safely retrieves named Int64 or defaultValue.
    member x.GetInt64 (name : string, ?defaultValue : Int64) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0L)
            (x.TryGetInt64 name)

    /// Safely retrieves named Boolean or defaultValue.
    member x.GetBoolean (name : string, ?defaultValue : Boolean) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue false)
            (x.TryGetBoolean name)

    /// Safely retrieves named Float or defaultValue.
    member x.GetFloat (name : string, ?defaultValue : float) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0)
            (x.TryGetFloat name)

    /// Safely retrieves named Decimal or defaultValue.
    member x.GetDecimal (name : string, ?defaultValue : Decimal) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue 0M)
            (x.TryGetDecimal name)

    /// Safely retrieves named DateTime or defaultValue.
    member x.GetDateTime (name : string, ?defaultValue : DateTime) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue DateTime.MinValue)
            (x.TryGetDateTime name)

    /// Safely retrieves named DateTimeOffset or defaultValue.
    member x.GetDateTimeOffset (name : string, ?defaultValue : DateTimeOffset) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue DateTimeOffset.MinValue)
            (x.TryGetDateTimeOffset name)

    /// Safely retrieves named Guid or defaultValue.
    member x.GetGuid (name : string, ?defaultValue : Guid) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue Guid.Empty)
            (x.TryGetGuid name)

    /// Safely retrieves named TimeSpan or defaultValue.
    member x.GetTimeSpan (name : string, ?defaultValue : TimeSpan) =
        Option.defaultWith
            (fun () -> defaultArg defaultValue TimeSpan.MinValue)
            (x.TryGetTimeSpan name)


    // ------------
    // Array Primitives
    // ------------

    /// Safely retrieves the named String[].
    member _.GetStringArray (name : string) =
        tryGetMapArray Some name

    /// Safely retrieves the named String[].
    member _.GetArray (name : string) =
        tryGetMapArray Some name

    /// Safely retrieves the named String[] excluding empty & null values.
    member _.GetStringNonEmptyArray (name : string) =
        tryGetMapArray StringParser.parseNonEmptyString name

    /// Safely retrieves the named Int16[].
    member _.GetInt16Array (name : string) =
        tryGetMapArray StringParser.parseInt16 name

    /// Safely retrieves the named Int32[].
    member _.GetInt32Array (name : string) =
        tryGetMapArray StringParser.parseInt32 name

    /// Safely retrieves the named Int[] (alias for StringCollectionReader.TryArrayInt32).
    member x.GetIntArray (name : string) =
        x.GetInt32Array name

    /// Safely retrieves the named Int64[].
    member _.GetInt64Array (name : string) =
        tryGetMapArray StringParser.parseInt64 name

    /// Safely retrieves the named Boolean[].
    member _.GetBooleanArray (name : string) =
        tryGetMapArray StringParser.parseBoolean name

    /// Safely retrieves the named Float[].
    member _.GetFloatArray (name : string) =
        tryGetMapArray StringParser.parseFloat name

    /// Safely retrieves the named Decimal[].
    member _.GetDecimalArray (name : string) =
        tryGetMapArray StringParser.parseDecimal name

    /// Safely retrieves the named DateTime[].
    member _.GetDateTimeArray (name : string) =
        tryGetMapArray StringParser.parseDateTime name

    /// Safely retrieves the named DateTimeOffset[].
    member _.GetDateTimeOffsetArray (name : string) =
        tryGetMapArray StringParser.parseDateTimeOffset name

    /// Safely retrieves the named Guid[].
    member _.GetGuidArray (name : string) =
        tryGetMapArray StringParser.parseGuid name

    /// Safely retrieves the named TimeSpan[].
    member _.GetTimeSpanArray (name : string) =
        tryGetMapArray StringParser.parseTimeSpan name
