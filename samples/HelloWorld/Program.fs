module HelloWorld.Program

open Falco
open Falco.Markup
open Falco.Routing
open Falco.HostBuilder

type Person =
    { FirstName : string
      LastName : string }

let form =
    Templates.html5 "en" [] [
        Elem.form [ Attr.method "post" ] [
            Elem.input [ Attr.name "Person.first_name"; Attr.value "first_name1"; Attr.placeholder "first name" ]
            Elem.input [ Attr.name "Person.last_name"; Attr.value "last_name1"; Attr.placeholder "last name" ]
            Elem.br []
            Elem.input [ Attr.name "Person.first_name"; Attr.value "first_name2"; Attr.placeholder "first name" ]
            Elem.input [ Attr.name "Person.last_name"; Attr.value "last_name2"; Attr.placeholder "last name" ]
            Elem.br []
            Elem.input [ Attr.name "Person.first_name"; Attr.value "first_name3"; Attr.placeholder "first name" ]
            Elem.input [ Attr.name "Person.last_name"; Attr.value "last_name4"; Attr.placeholder "last name" ]
            Elem.br []
            Elem.input [ Attr.type' "Submit" ]
        ]
    ]

let formHandler : HttpHandler =
    Request.mapForm (fun f ->
        let people : StringCollectionReader list = f.GetChildren("Person")

        [ for person in people do
            { FirstName = person.GetString "first_name" "John"
              LastName = person.GetString "last_name" "Doe" } ])
        Response.ofJson

webHost [||] {
    use_https

    endpoints [
        get "/form" (Response.ofHtml form)
        post "/form" formHandler
    ]
}