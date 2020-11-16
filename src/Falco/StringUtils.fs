module Falco.StringUtils

open System 

/// Check if string is null or whitespace
let strEmpty str =
    String.IsNullOrWhiteSpace(str)

/// Check if string is not null or whitespace
let strNotEmpty str =
    not(strEmpty str)

/// Case & culture insensistive string equality
let strEquals s1 s2 = 
    String.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase)

/// Concat strings
let strConcat (list : string seq) =
    // String.Concat uses a StringBuilder when provided an IEnumerable
    // Url: https://github.com/microsoft/referencesource/blob/master/mscorlib/system/string.cs#L161
    String.Concat(list)

/// Join strings with a separator
let strJoin (sep : string) (lst : string seq) = 
    // String.Join uses a StringBuilder when provided an IEnumerable
    // Url: https://github.com/microsoft/referencesource/blob/master/mscorlib/system/string.cs#L161
    String.Join(sep, lst)