module AppName.UI

open Falco.Markup

/// Reusable components
module Components = 
    /// Display a list of errors as <ul>...</ul>
    let errorSummary (errors : string list) =
        match errors.Length with
        | n when n > 0 ->
            Elem.ul [ Attr.class' "pa2 red bg-washed-red ba b--red" ] (errors |> List.map (fun e -> Elem.li [] [ Text.raw e ]))

        | _ -> 
            Elem.div [] []

/// Website layouts
module Layouts =
    /// Master layout
    let master (htmlTitle : string) (content : XmlNode list) =
        Templates.html5 "en"
            [
                Elem.title [] [ Text.raw htmlTitle ]
                Elem.link  [ Attr.href "/style.css"; Attr.rel "stylesheet" ] 
            ]
            content