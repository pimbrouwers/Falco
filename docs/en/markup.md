# Markup

[![NuGet Version](https://img.shields.io/nuget/v/Falco.Markup.svg)](https://www.nuget.org/packages/Falco.Markup)
[![build](https://github.com/pimbrouwers/Falco/actions/workflows/build.yml/badge.svg)](https://github.com/pimbrouwers/Falco/actions/workflows/build.yml)

A core feature of Falco is the XML markup module. It can be used to produce __any__ form of angle-bracket markup (i.e. HTML, SVG, XML etc.) within a Falco project and is also available directly as a standalone [NuGet](https://www.nuget.org/packages/Falco.Markup) package

## HTML

_All_ of the standard HTML tags & attributes have a functional representation that produces objects to represent the HTML node. Nodes are either:

- `Text` which represents `string` values. (Ex: `Text.raw "hello"`, `Text.rawf "hello %s" "world"`)
- `SelfClosingNode` which represent self-closing tags (Ex: `<br />`).
- `ParentNode` which represent typical tags with, optionally, other tags within it (Ex: `<div>...</div>`).

The benefits of using the Falco markup module as an HTML engine include:

- Writing your views in plain F#, directly in your assembly.
- Markup is compiled alongside the rest of your code, leading to improved performance and ultimately simpler deployments.

Since views are plain F# they can easily be made strongly-typed:

```fsharp
type Person =
    { FirstName : string
      LastName : string }

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

Rumor has it that the Falco [creator](https://twitter.com/pim_brouwers) makes a dog sound every time he uses `Text.rawf`.

> **Note**: I am the creator, and this is entirely true.

Views can also be combined to create more complex views and share output (i.e., view components):

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

## SVG


## Custom Elements & Attributes

Every effort has been taken to ensure the HTML and SVG specs are mapped to functions in the module. In the event an element or attribute you need is missing, you can either file an [issue](https://github.com/pimbrouwers/Falco/issues), or more simply extend the module in your project.


An example creating custom XML elements and using them to create a structured XML document:

```fsharp
open Falco.Makrup

module Elem =
    let books = Elem.create "books"
    let book = Elem.create "book"
    let name = Elem.create "name"

module Attr =
    let soldOut = Attr.createBool "soldout"

let xmlDoc =
    Elem.books [] [
        Elem.book [ Attr.soldOut ] [
            Elem.name [] [ Text.raw "To Kill A Mockingbird" ]
        ]
    ]

let xml = renderXml xmlDoc
```