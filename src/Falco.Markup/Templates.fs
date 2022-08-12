namespace Falco.Markup

open System
open System.Globalization
open System.IO
open System.Net

module Templates =
    /// HTML 5 template with customizable <head> and <body>
    let html5 (langCode : string) (head : XmlNode list) (body : XmlNode list) =
        let defaultHead = [
            Elem.meta  [ Attr.charset "UTF-8" ]
            Elem.meta  [ Attr.httpEquiv "X-UA-Compatible"; Attr.content "IE=edge, chrome=1" ]
            Elem.meta  [ Attr.name "viewport"; Attr.content "width=device-width, initial-scale=1" ]
        ]

        Elem.html [ Attr.lang langCode; ] [
            Elem.head [] (defaultHead @ head)
            Elem.body [] body
        ]

    /// SVG Version 1.0 template with customizable viewBox width/height
    let svg (x : int, y : int, w : int, h : int) (content : XmlNode list) =
        Elem.svg [            
            Attr.create "viewBox" (sprintf "%i %i %i %i" x y w h)
            Attr.create "xmlns" "http://www.w3.org/2000/svg"
        ] content