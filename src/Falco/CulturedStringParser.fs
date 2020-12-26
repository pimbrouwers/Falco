module Falco.CulturedStringParser

open System
open System.Globalization

let parseInt32 (culture: string) (readString: string) = 
    match Int32.TryParse (readString, NumberStyles.Integer, CultureInfo (culture, true)) with
    | true, value -> Some value
    | false, _ -> None

let parseInt = parseInt32
