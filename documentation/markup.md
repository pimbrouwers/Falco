# Falco.Markup

[![NuGet Version](https://img.shields.io/nuget/v/Falco.Markup.svg)](https://www.nuget.org/packages/Falco.Markup)
[![build](https://github.com/pimbrouwers/Falco.Markup/actions/workflows/build.yml/badge.svg)](https://github.com/pimbrouwers/Falco.Markup/actions/workflows/build.yml)

```fsharp
open Falco.Markup

let doc =
    Elem.html [] [
        Elem.body [ Attr.class' "100-vh" ] [
            Elem.h1 [] [ Text.raw "Hello world!" ] ] ]

renderHtml doc
```

[Falco.Markup](https://github.com/pimbrouwers/Falco.Markup) is an XML markup module that can be used to produce _any_ form of angle-bracket markup (i.e. HTML, SVG, XML etc.).

## Key Features

- Use native F# to produce any form of angle-bracket markup.
- Very simple to create reusable blocks of code (i.e., partial views and components).
- Easily extended by creating custom tags and attributes.
- Compiled as part of your assembly, leading to improved performance and simpler deployments.

## Design Goals

- Provide a tool to generate _any_ form of angle-bracket markup.
- Should be simple, extensible and integrate with existing .NET libraries.
- Can be easily learned.

## Overview

Falco.Markup is broken down into three primary modules. `Elem`, `Attr` and `Text`, which are used to generate elements, attributes and text nodes respectively. Each module contain a suite of functions mapping to the various element/attribute/node names. But can also be extended to create custom elements and attributes.

Primary elements are broken down into two types, `ParentNode` or `SelfClosingNode`.

`ParentNode` elements are those that can contain other elements. Represented as functions that receive two inputs: attributes and optionally elements.

```fsharp
let markup =
    Elem.div [ Attr.class' "heading" ] [
        Elem.h1 [] [ Text.raw "Hello world!" ] ]
```

`SelfClosingNode` elements are self-closing tags. Represented as functions that receive one input: attributes.

```fsharp
let markup =
    Elem.div [ Attr.class' "divider" ] [
        Elem.hr [] ]
```

Text is represented using the `TextNode` and created using one of the functions in the `Text` module.

```fsharp
let markup =
    Elem.div [] [
        Text.comment "An HTML comment"
        Elem.p [] [ Text.raw "A paragraph" ]
        Elem.p [] [ Text.rawf "Hello %s" "Jim" ]
        Elem.code [] [ Text.enc "<div>Hello</div>" ] // HTML encodes text before rendering
    ]
```

Attributes contain two subtypes as well, `KeyValueAttr` which represent key/value attributes or `NonValueAttr` which represent boolean attributes.

```fsharp
let markup =
    Elem.input [ Attr.type' "text"; Attr.required ]
```

Most [JavaScript Events](https://developer.mozilla.org/en-US/docs/Web/Events) have also been mapped in the `Attr` module. All of these events are prefixed with the word "on" (i.e., `Attr.onclick`, `Attr.onfocus` etc.)

```fsharp
let markup =
    Elem.button [ Attr.onclick "console.log(\"hello world\")" ] [ Text.raw "Click me" ]
```

## HTML

Though Falco.Markup can be used to produce any markup. It is first and foremost an HTML library.

### Combining views to create complex output

```fsharp
open Falco.Markup

// Components
let divider =
    Elem.hr [ Attr.class' "divider" ]

// Template
let master (title : string) (content : XmlNode list) =
    Elem.html [ Attr.lang "en" ] [
        Elem.head [] [
            Elem.title [] [ Text.raw title ]
        ]
        Elem.body [] content
    ]

// Views
let homeView =
    master "Homepage" [
        Elem.h1 [] [ Text.raw "Homepage" ]
        divider
        Elem.p [] [ Text.raw "Lorem ipsum dolor sit amet, consectetur adipiscing."]
    ]

let aboutView =
    master "About Us" [
        Elem.h1 [] [ Text.raw "About" ]
        divider
        Elem.p [] [ Text.raw "Lorem ipsum dolor sit amet, consectetur adipiscing."]
    ]
```

### Strongly-typed views

```fsharp
open Falco.Markup

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
                Elem.p [] [ Text.rawf "%s %s" person.First person.Last ]
            ]
        ]
    ]
```

### Merging Attributes

The markup module allows you to easily create components, an excellent way to reduce code repetition in your UI. To support runtime customization, it is advisable to ensure components (or reusable markup blocks) retain a similar function "shape" to standard elements. That being, `XmlAttribute list -> XmlNode list -> XmlNode`.

This means that you will inevitably end up needing to combine your predefined `XmlAttribute list` with a list provided at runtime. To facilitate this, the `Attr.merge` function will group attributes by key, and concatenate the values in the case of `KeyValueAttribute`.

```fsharp
open Falco.Markup

// Components
let heading (attrs : XmlAttribute list) (content : XmlNode list) =
    // safely combine the default XmlAttribute list with those provided
    // at runtime
    let attrs' =
        Attr.merge [ Attr.class' "text-large" ] attrs

    Elem.div [] [
        Elem.h1 [ attrs' ] content
    ]

// Template
let master (title : string) (content : XmlNode list) =
    Elem.html [ Attr.lang "en" ] [
        Elem.head [] [
            Elem.title [] [ Text.raw title ]
        ]
        Elem.body [] content
    ]

// Views
let homepage =
    master "Homepage" [
        heading [ Attr.class' "red" ] [ Text.raw "Welcome to the homepage" ]
        Elem.p [] [ Text.raw "Lorem ipsum dolor sit amet, consectetur adipiscing."]
    ]

let homepage =
    master "About Us" [
        heading [ Attr.class' "purple" ] [ Text.raw "This is what we're all about" ]
        Elem.p [] [ Text.raw "Lorem ipsum dolor sit amet, consectetur adipiscing."]
    ]
```

## Custom Elements & Attributes

Every effort has been taken to ensure the HTML and SVG specs are mapped to functions in the module. In the event an element or attribute you need is missing, you can either file an [issue](https://github.com/pimbrouwers/Falco.Markup/issues), or more simply extend the module in your project.

An example creating custom XML elements and using them to create a structured XML document:

```fsharp
open Falco.Makrup

module Elem =
    let books = Elem.create "books"
    let book = Elem.create "book"
    let name = Elem.create "name"

module Attr =
    let soldOut = Attr.createBool "soldOut"

let xmlDoc =
    Elem.books [] [
        Elem.book [ Attr.soldOut ] [
            Elem.name [] [ Text.raw "To Kill A Mockingbird" ]
        ]
    ]

let xml = renderXml xmlDoc
```

## SVG

Much of the SVG spec has been mapped to element and attributes functions. There is also an SVG template to help initialize a new drawing with a valid viewbox.

```fsharp
open Falco.Markup
open Falco.Markup.Svg

// https://developer.mozilla.org/en-US/docs/Web/SVG/Element/text#example
let svgDrawing =
    Templates.svg (0, 0, 240, 80) [
        Elem.style [] [
            Text.raw ".small { font: italic 13px sans-serif; }"
            Text.raw ".heavy { font: bold 30px sans-serif; }"
            Text.raw ".Rrrrr { font: italic 40px serif; fill: red; }"
        ]
        Elem.text [ Attr.x "20"; Attr.y "35"; Attr.class' "small" ] [ Text.raw "My" ]
        Elem.text [ Attr.x "40"; Attr.y "35"; Attr.class' "heavy" ] [ Text.raw "cat" ]
        Elem.text [ Attr.x "55"; Attr.y "55"; Attr.class' "small" ] [ Text.raw "is" ]
        Elem.text [ Attr.x "65"; Attr.y "55"; Attr.class' "Rrrrr" ] [ Text.raw "Grumpy!" ]
    ]

let svg = renderNode svgDrawing
```

## Find a bug?

There's an [issue](https://github.com/pimbrouwers/Falco.Markup/issues) for that.

## License

Built with ♥ by [Pim Brouwers](https://github.com/pimbrouwers) in Toronto, ON. Licensed under [Apache License 2.0](https://github.com/pimbrouwers/Falco.Markup/blob/master/LICENSE).
