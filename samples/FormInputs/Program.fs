module FormInputs.Program

open Falco
open Falco.Markup
open Falco.HostBuilder
open Falco.Routing

// ------------
// Handlers
// ------------
let displayForm : HttpHandler =
    let form =
        Templates.html5 "en" [] [
            Elem.h1 [] [ Text.raw "Form Inputs" ]

            Elem.form [ Attr.method "post" ] [
                Elem.label [] [ Text.raw "Text Input" ]
                Elem.input [ Attr.type' "text"; Attr.name "input_text" ]

                Elem.br []

                Elem.label [] [ Text.raw "Number Input" ]
                Elem.input [ Attr.type' "number"; Attr.name "input_number" ]

                Elem.br []

                Elem.label [] [
                    Text.raw "Am I checked?"
                    Elem.input [ Attr.type' "checkbox"; Attr.name "input_checkbox" ]
                ]

                Elem.br []

                Elem.label [] [
                    Text.raw "Checkboxes"
                    Elem.br []
                    Text.raw "A"
                    Elem.input [ Attr.type' "checkbox"; Attr.name "input_checkboxes"; Attr.value "A" ]
                    Text.raw "B"
                    Elem.input [ Attr.type' "checkbox"; Attr.name "input_checkboxes"; Attr.value "B" ]
                    Text.raw "C"
                    Elem.input [ Attr.type' "checkbox"; Attr.name "input_checkboxes"; Attr.value "C" ]
                ]

                Elem.br []

                Elem.label [] [ Text.raw "Radio" ]
                Elem.br []
                Elem.label [] [
                    Text.raw "Yes"
                    Elem.input [ Attr.type' "radio"; Attr.name "input_radio"; Attr.value "Yes" ]
                ]
                Elem.label [] [
                    Text.raw "No"
                    Elem.input [ Attr.type' "radio"; Attr.name "input_radio"; Attr.value "No" ]
                ]

                Elem.br []

                Elem.label [] [ Text.raw "Select" ]
                Elem.select [ Attr.name "input_select" ] [
                    Elem.option [ Attr.value "Option 1"] [ Text.raw "Option 1"]
                    Elem.option [ Attr.value "Option 2"] [ Text.raw "Option 2"] ]

                Elem.br []

                Elem.label [] [ Text.raw "Multiselect" ]
                Elem.select [ Attr.name "input_multiselect"; Attr.multiple  ] [
                    Elem.option [ Attr.value "Option 1"] [ Text.raw "Option 1"]
                    Elem.option [ Attr.value "Option 2"] [ Text.raw "Option 2"] ]

                Elem.input [ Attr.type' "submit" ]
            ]
        ]

    Response.ofHtml form

let handleForm : HttpHandler =
    let map (f : FormCollectionReader) =
        {| Text = f.Get "input_text" ""
           Number = f.GetInt "input_number" -1
           Checkbox = f.GetBoolean "input_checkbox" false
           Checkboxes = f.GetArray "input_checkboxes"
           Radios = f.GetBoolean "input_radio" false
           Select = f.GetString "input_select" ""
           Multiselect = f.GetArray "input_multiselect" |}

    Request.mapForm map Response.ofJson

[<EntryPoint>]
let main args =
    webHost args {
        endpoints [
            get  "/" displayForm
            post "/" handleForm
        ]
    }
    0