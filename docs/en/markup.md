# Markup

A core feature of Falco is the XML markup module. It can be used to produce any form of angle-bracket markup (i.e. HTML, SVG, XML etc.).

For example, the module is easily extended since creating new tags is simple. An example to render `<svg>`'s:

```fsharp
let svg (width : float) (height : float) =
    Elem.tag "svg" [
        Attr.create "version" "1.0"
        Attr.create "xmlns" "http://www.w3.org/2000/svg"
        Attr.create "viewBox" (sprintf "0 0 %f %f" width height)
    ]

let path d = Elem.tag "path" [ Attr.create "d" d ] []

let bars =
    svg 384.0 384.0 [
        path "M368 154.668H16c-8.832 0-16-7.168-16-16s7.168-16 16-16h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0M368 32H16C7.168 32 0 24.832 0 16S7.168 0 16 0h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0M368 277.332H16c-8.832 0-16-7.168-16-16s7.168-16 16-16h352c8.832 0 16 7.168 16 16s-7.168 16-16 16zm0 0"
    ]
```

## HTML View Engine

Most of the standard HTML tags & attributes have been built into the markup module and produce objects to represent the HTML node. Nodes are either:

- `Text` which represents `string` values. (Ex: `Text.raw "hello"`, `Text.rawf "hello %s" "world"`)
- `SelfClosingNode` which represent self-closing tags (Ex: `<br />`).
- `ParentNode` which represent typical tags with, optionally, other tags within it (Ex: `<div>...</div>`).

The benefits of using the Falco markup module as an HTML engine include:

- Writing your views in plain F#, directly in your assembly.
- Markup is compiled alongside the rest of your code, leading to improved performance and ultimately simpler deployments.

```fsharp
// Create an HTML5 document using built-in template
let doc =
    Templates.html5 "en"
        [ Elem.title [] [ Text.raw "Sample App" ] ] // <head></head>
        [ Elem.h1 [] [ Text.raw "Sample App" ] ]    // <body></body>
```

Since views are plain F# they can easily be made strongly-typed:
```fsharp
type Person = { FirstName : string; LastName : string }

let doc (person : Person) =
    Elem.html [ Attr.lang "en" ] [
        Elem.head [] [
            Elem.title [] [ Text.raw "Sample App" ]
        ]
        Elem.body [] [
            Elem.main [] [
                Elem.h1 [] [ Text.raw "Sample App" ]
                Elem.p  [] [ Text.rawf "%s %s" person.First person.Last ]
            ]
        ]
    ]
```

Views can also be combined to create more complex views and share output:

```fsharp
let master (title : string) (content : XmlNode list) =
    Elem.html [ Attr.lang "en" ] [
        Elem.head [] [
            Elem.title [] [ Text.raw "Sample App" ]
        ]
        Elem.body [] content
    ]

let divider =
    Elem.hr [ Attr.class' "divider" ]

let homeView =
    [
        Elem.h1 [] [ Text.raw "Homepage" ]
        divider
        Elem.p  [] [ Text.raw "Lorem ipsum dolor sit amet, consectetur adipiscing."]
    ]
    |> master "Homepage"

let aboutView =
    [
        Elem.h1 [] [ Text.raw "About" ]
        divider
        Elem.p  [] [ Text.raw "Lorem ipsum dolor sit amet, consectetur adipiscing."]
    ]
    |> master "About Us"
```
