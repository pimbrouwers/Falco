﻿module AppName.UI

open Falco.Markup

/// Reusable components
module Common = 
    /// Display a list of errors as <ul>...</ul>
    let errorSummary (errors : string list) =
        match errors.Length with
        | n when n > 0 ->
            Elem.ul [ Attr.class' "pa2 red bg-washed-red ba b--red" ] (errors |> List.map (fun e -> Elem.li [] [ Text.raw e ]))

        | _ -> 
            Elem.div [] []

    /// Page title as <h1></h1>
    let pageTitle (title : string) =
        Elem.h1 [] [ Text.raw title ]

    /// Page subtitle as <h1></h1>
    let subTitle (title : string) =
        Elem.h2 [] [ Text.raw title ]

/// Form elements
module Forms =
    let private inputCss = "db w-100 mb3 pa2 ba b--black-20"

    let private input (inputType : string) (value : string) (attrs : XmlAttribute list) =
        [
            Attr.type' inputType                  
            Attr.value value
        ]   
        @ attrs
        |> Elem.input

    let inputText name value attrs = 
        input "text" value (attrs |> Attr.merge [ Attr.class' inputCss; Attr.name name ])
            
    let label for' text = 
        Elem.label [ Attr.for' for'; Attr.class' "db" ] [ Text.raw text ]
    
    let submit value =
        input "submit" value []
        
/// Website layouts
module Layouts =
    /// Master layout which accepts a title and content for <body></body>
    let master (htmlTitle : string) (content : XmlNode list) =
        Templates.html5 "en"
            [
                Elem.title [] [ Text.raw htmlTitle ]
                Elem.link  [ Attr.href "/style.css"; Attr.rel "stylesheet" ] 
            ]
            content