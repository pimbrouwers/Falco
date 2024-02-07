namespace Falco

open System
open System.Collections.Generic

module internal StringUtils =
    /// Checks if string is null or whitespace.
    let strEmpty str =
        String.IsNullOrWhiteSpace(str)

    /// Checks if string is not null or whitespace.
    let strNotEmpty str =
        not(strEmpty str)

    /// Case & culture insensitive string equality.
    let strEquals s1 s2 =
        String.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase)

    /// Concats strings.
    let strConcat (lst : string seq) =
        // String.Concat uses a StringBuilder when provided an IEnumerable
        // Url: https://github.com/microsoft/referencesource/blob/master/mscorlib/system/string.cs#L161
        String.Concat(lst)

    /// Splits string into substrings based on separator.
    let strSplit (sep : char array) (str : string) =
        str.Split(sep)

module internal StringParser =
    /// Helper to wrap .NET tryParser's.
    let tryParseWith (tryParseFunc: string -> bool * _) (str : string) =
        let parsedResult = tryParseFunc str
        match parsedResult with
        | true, v    -> Some v
        | false, _   -> None

    let parseNonEmptyString x = if StringUtils.strEmpty x then None else Some x

    let parseInt16          = tryParseWith Int16.TryParse
    let parseInt64          = tryParseWith Int64.TryParse
    let parseInt32          = tryParseWith Int32.TryParse
    let parseFloat          = tryParseWith Double.TryParse
    let parseDecimal        = tryParseWith Decimal.TryParse
    let parseDateTime       = tryParseWith DateTime.TryParse
    let parseDateTimeOffset = tryParseWith DateTimeOffset.TryParse
    let parseTimeSpan       = tryParseWith TimeSpan.TryParse
    let parseGuid           = tryParseWith Guid.TryParse

    /// Attempts to parse boolean from string.
    ///
    /// Returns None on failure, Some x on success.
    ///
    /// Case-insensitive comparison, and special cases for "yes", "no", "on",
    /// "off", "1", "0".
    let parseBoolean (value : string) =
        let v = value.ToUpperInvariant()

        match v with
        | "ON" | "YES" | "1" -> Some true
        | "OFF" | "NO" | "0" -> Some false
        | v -> tryParseWith Boolean.TryParse v

    /// Attempts to parse, or failwith message.
    let parseOrFail parser msg v =
        match parser v with
        | Some v -> v
        | None   -> failwith msg

    /// Attempts to parse array, returns none for failure.
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