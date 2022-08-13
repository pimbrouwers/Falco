module Falco.StringParser

open System

/// Helper to wrap .NET tryParser's
let tryParseWith (tryParseFunc: string -> bool * _) =
    tryParseFunc >> function
    | true, v    -> Some v
    | false, _   -> None

let parseNonEmptyString x = if StringUtils.strEmpty x then None else Some x

let parseInt            = tryParseWith Int32.TryParse
let parseInt16          = tryParseWith Int16.TryParse
let parseInt32          = parseInt
let parseInt64          = tryParseWith Int64.TryParse
let parseFloat          = tryParseWith Double.TryParse
let parseDecimal        = tryParseWith Decimal.TryParse
let parseDateTime       = tryParseWith DateTime.TryParse
let parseDateTimeOffset = tryParseWith DateTimeOffset.TryParse
let parseTimeSpan       = tryParseWith TimeSpan.TryParse
let parseGuid           = tryParseWith Guid.TryParse

/// Attempt to parse boolean from string. 
/// Returns None on failure, Some x on success. 
/// Case-insensitive comparison, and special cases for "yes", "no", "on", "off", "1", "0".
let parseBoolean (value : string) = 
    let v = value.ToUpperInvariant ()

    match v with 
    | "ON" | "YES" | "1" -> Some true 
    | "OFF" | "NO" | "0" -> Some false
    | v -> tryParseWith Boolean.TryParse v
    
/// Attempt to parse, or failwith message
let parseOrFail parser msg v =
    match parser v with
    | Some v -> v
    | None   -> failwith msg

/// Attempt to parse array, returns none for failure
let tryParseArray parser ary =
    ary
    |> List.ofArray
    |> List.fold (fun acc i ->
        // accumulate successful parses
        match (parser i, acc) with
        | Some i, acc -> i :: acc
        | None, acc -> acc) []
    |> List.rev
    |> Array.ofList


