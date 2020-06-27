module Falco.StringParser

open System

/// Helper to wrap .NET tryParser's
let tryParseWith (tryParseFunc: string -> bool * _) = 
    tryParseFunc >> function
    | true, v    -> Some v
    | false, _   -> None
  
let parseInt            = tryParseWith Int32.TryParse
let parseInt16          = tryParseWith Int16.TryParse
let parseInt32          = parseInt
let parseInt64          = tryParseWith Int64.TryParse
let parseBoolean        = tryParseWith Boolean.TryParse
let parseFloat          = tryParseWith Double.TryParse
let parseDecimal        = tryParseWith Decimal.TryParse
let parseDateTime       = tryParseWith DateTime.TryParse
let parseDateTimeOffset = tryParseWith DateTimeOffset.TryParse
let parseTimeSpan       = tryParseWith TimeSpan.TryParse
let parseGuid           = tryParseWith Guid.TryParse

/// Attempt to parse, or failwith message
let parseOrFail parser msg v =
    match parser v with 
    | Some v -> v
    | None   -> failwith msg

/// Attempt to parse array, returns none for failure
let tryParseArray parser ary =
    ary
    |> Seq.map parser
    |> Seq.fold (fun acc i ->
        match (i, acc) with
        | Some i, Some acc -> Some (Array.append acc [|i|])
        | _ -> None) (Some [||])    
    

