# Falco.Markup

[![NuGet Version](https://img.shields.io/nuget/v/Falco.Markup.svg)](https://www.nuget.org/packages/Falco.Markup)
[![Build Status](https://travis-ci.org/pimbrouwers/Falco.svg?branch=master)](https://travis-ci.org/pimbrouwers/Falco)

A core feature of Falco is the XML markup module. It can be used to produce any form of angle-bracket markup (i.e. HTML, SVG, XML etc.).

### HTML View Engine

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

## Find a bug?

There's an [issue](https://github.com/pimbrouwers/Falco/issues) for that.

## License

Built with ♥ by [Pim Brouwers](https://github.com/pimbrouwers) in Toronto, ON. Licensed under [Apache License 2.0](https://github.com/pimbrouwers/Falco/blob/master/LICENSE).
