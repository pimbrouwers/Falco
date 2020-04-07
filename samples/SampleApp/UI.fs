module SampleApp.UI

open Falco.ViewEngine

let svg (width : float) (height : float) =
    tag "svg" [
            attr "version" "1.0"
            attr "xmlns" "http://www.w3.org/2000/svg"
            attr "viewBox" (sprintf "0 0 %f %f" width height)
        ]

let path d = tag "path" [ attr "d" d ] []

let bars =
    svg 384.0 384.0 [
            path "M368 154.668H16c-8.832 0-16-7.168-16-16s7.168-16 16-16h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0M368 32H16C7.168 32 0 24.832 0 16S7.168 0 16 0h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0M368 277.332H16c-8.832 0-16-7.168-16-16s7.168-16 16-16h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0"
        ]

let master pageTitle content = 
    html [ _lang "en" ] [
    head [] [
        meta  [ _charset "UTF-8" ]
        meta  [ _httpEquiv "X-UA-Compatible"; _content "IE=edge,chrome=1" ]
        meta  [ _name "viewport"; _content "width=device-width,initial-scale=1" ]
        title [] [ raw pageTitle ]                                        
        link  [ _href "/style.css"; _rel "stylesheet"]
    ]
    body [] [                     
            main [] content
        ]
] 

let homeView = 
    master "Home" [
            h1 [] [ raw "Sample App" ]
        ]

let newUserView =
    let pageTitle = "New User"
    master pageTitle [
            h1   [] [ raw pageTitle ]
            form [ _method "post" ] [
                    label [] [ raw "First Name"]
                    input [ _name "first" ] 
                    label [] [ raw "Last Name"]
                    input [ _name "last" ]
                    input [ _type "submit"; _value "Save" ]
                ]
        ]

let searchView =
    let pageTitle = "Search"
    master pageTitle [
        h1   [] [ raw pageTitle ]
        form [ _method "get"; _action "/search-results" ] [                
                input [ _name "frag" ]                 
                input [ _name "take"; _type "hidden"; _value "10" ]

                input [ _name "n"; _type "hidden"; _value "1" ]
                input [ _name "n"; _type "hidden"; _value "2" ]
                input [ _name "n"; _type "hidden"; _value "3" ]

                input [ _type "submit"; _value "Search" ]
            ]
    ]