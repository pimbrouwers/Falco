module Falco.CulturedStringParser

open System
open System.Globalization

let parseInt (culture: string) (s: string) = 
    match Int32.TryParse (s, NumberStyles.Integer, CultureInfo (culture, true)) with
    | true, value -> Some value
    | false, _ -> None
    