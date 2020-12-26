module Falco.CulturedStringParser

open System
open System.Globalization

let tryParse parser (culture: string) (readString: string) =
    match parser (readString, NumberStyles.Integer, CultureInfo (culture, true)) with
    | true, value -> Some value
    | false, _ -> None

let parseInt16 = 
    tryParse (fun (readString, numberStyle, cultureInfo) -> Int16.TryParse (readString, numberStyle, cultureInfo))

let parseInt32 = 
    tryParse (fun (readString, numberStyle, cultureInfo) -> Int32.TryParse (readString, numberStyle, cultureInfo))

let parseInt = parseInt32

let parseInt64 = 
    tryParse (fun (readString, numberStyle, cultureInfo) -> Int64.TryParse (readString, numberStyle, cultureInfo))
