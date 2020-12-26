module Falco.CulturedStringParser

open System
open System.Globalization

let tryParse parser style (culture: string) (readString: string) =
    match parser (readString, style, CultureInfo culture) with
    | true, value -> Some value
    | false, _ -> None

let parseInt16 = 
    tryParse 
        (fun (readString, numberStyle, cultureInfo) -> Int16.TryParse (readString, numberStyle, cultureInfo))
        NumberStyles.Integer

let parseInt32 = 
    tryParse 
        (fun (readString, numberStyle, cultureInfo) -> Int32.TryParse (readString, numberStyle, cultureInfo))
        NumberStyles.Integer

let parseInt = parseInt32

let parseInt64 = 
    tryParse 
        (fun (readString, numberStyle, cultureInfo) -> Int64.TryParse (readString, numberStyle, cultureInfo))
        NumberStyles.Integer

let parseFloat = 
    tryParse 
        (fun (readString, numberStyle, cultureInfo) -> Double.TryParse (readString, numberStyle, cultureInfo))
        NumberStyles.AllowDecimalPoint

let parseDecimal = 
    tryParse 
        (fun (readString, numberStyle, cultureInfo) -> Decimal.TryParse (readString, numberStyle, cultureInfo))
        NumberStyles.AllowDecimalPoint

let parseDateTime = 
    tryParse 
        (fun (readString, timeStyle, cultureInfo) -> DateTime.TryParse (readString, cultureInfo, timeStyle))
        DateTimeStyles.None

let parseDateTimeOffset =
    tryParse 
        (fun (readString, timeStyle, cultureInfo) -> DateTimeOffset.TryParse (readString, cultureInfo, timeStyle))
        DateTimeStyles.None

let parseTimeSpan =
    tryParse 
        (fun (readString, _, cultureInfo) -> TimeSpan.TryParse (readString, cultureInfo))
        DateTimeStyles.None
        