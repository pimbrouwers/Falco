namespace Falco

open System

module StringUtils =
    /// stringf is an F# function that invokes any ToString(string) method.
    /// Credit: Mark Seeman - https://blog.ploeh.dk/2015/05/08/stringf/
    let inline stringf format (x : ^a) =
        (^a : (member ToString : string -> string) (x, format))

    /// Check if string is null or whitespace.
    let strEmpty str =
        String.IsNullOrWhiteSpace(str)

    /// Check if string is not null or whitespace.
    let strNotEmpty str =
        not(strEmpty str)

    /// Case & culture insensistive string equality.
    let strEquals s1 s2 =
        String.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase)

    /// Concat strings.
    let strConcat (list : string seq) =
        // String.Concat uses a StringBuilder when provided an IEnumerable
        // Url: https://github.com/microsoft/referencesource/blob/master/mscorlib/system/string.cs#L161
        String.Concat(list)

    /// Join strings with a separator.
    let strJoin (sep : string) (lst : string seq) =
        // String.Join uses a StringBuilder when provided an IEnumerable
        // Url: https://github.com/microsoft/referencesource/blob/master/mscorlib/system/string.cs#L161
        String.Join(sep, lst)

    /// Split string into substrings based on separator.
    let strSplit (sep : char array) (str : string) =
        str.Split(sep)

module StringParser =
    /// Helper to wrap .NET tryParser's.
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
    ///
    /// Returns None on failure, Some x on success.
    ///
    /// Case-insensitive comparison, and special cases for "yes", "no", "on",
    /// "off", "1", "0".
    let parseBoolean (value : string) =
        let v = value.ToUpperInvariant ()

        match v with
        | "ON" | "YES" | "1" -> Some true
        | "OFF" | "NO" | "0" -> Some false
        | v -> tryParseWith Boolean.TryParse v

    /// Attempt to parse, or failwith message.
    let parseOrFail parser msg v =
        match parser v with
        | Some v -> v
        | None   -> failwith msg

    /// Attempt to parse array, returns none for failure.
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
