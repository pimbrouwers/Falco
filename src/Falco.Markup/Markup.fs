module Falco.Markup

open System
open System.Globalization
open System.IO
open System.Net

/// Specifies an XML-style attribute
type XmlAttribute =
    | KeyValueAttr of string * string
    | NonValueAttr of string

/// Represents an XML-style element containing attributes
type XmlElement =
    string * XmlAttribute[]

/// Describes the different XML-style node patterns
type XmlNode =
    | ParentNode      of XmlElement * XmlNode list
    | SelfClosingNode of XmlElement
    | TextNode        of string

module internal XmlNode =
    let serialize (w : StringWriter) (xml : XmlNode) =
        let _openChar = '<'
        let _closeChar = '>'
        let _term = '/'
        let _space = ' '
        let _equals = '='
        let _quote = '"'

        let writeAttributes attrs =
            for attr in (attrs : XmlAttribute[]) do
                if attrs.Length > 0 then
                    w.Write _space

                match attr with
                | NonValueAttr attrName ->
                    w.Write attrName

                | KeyValueAttr (attrName, attrValue) ->
                    w.Write attrName
                    w.Write _equals
                    w.Write _quote
                    w.Write attrValue
                    w.Write _quote

        let rec buildXml tag =
            match tag with
            | TextNode text ->
                w.Write text

            | SelfClosingNode (tag, attrs) ->
                w.Write _openChar
                w.Write tag
                writeAttributes attrs
                w.Write _space
                w.Write _term
                w.Write _closeChar

            | ParentNode ((tag, attrs), children) ->
                w.Write _openChar
                w.Write tag
                writeAttributes attrs
                w.Write _closeChar

                for c in children do
                    buildXml c

                w.Write _openChar
                w.Write _term
                w.Write tag
                w.Write _closeChar

        buildXml xml

        w.GetStringBuilder().ToString()

/// Render XmlNode recursively to string representation
let renderNode (tag : XmlNode) =
    let sb = Text.StringBuilder()
    let w = new StringWriter(sb, CultureInfo.InvariantCulture)    
    XmlNode.serialize w tag

/// Render XmlNode as HTML string
let renderHtml (tag : XmlNode) =
    let sb = Text.StringBuilder()
    let w = new StringWriter(sb, CultureInfo.InvariantCulture)
    w.Write "<!DOCTYPE html>"
    XmlNode.serialize w tag

let renderXml (tag : XmlNode) = 
    let sb = Text.StringBuilder()
    let w = new StringWriter(sb, CultureInfo.InvariantCulture)
    w.Write "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
    XmlNode.serialize w tag
    
module Text =
    /// Empty Text node
    let empty = TextNode String.Empty

    /// Encoded-text XmlNode constructor
    let enc content = TextNode (WebUtility.HtmlEncode content)

    /// Text XmlNode constructor
    let raw content = TextNode content

    /// Text XmlNode constructor that will invoke "sprintf template"
    let rawf template = Printf.kprintf raw template

    /// HTML Comment Text XmlNode construction
    let comment = rawf "<!-- %s -->"

module Elem =
    /// Standard XmlNode constructor
    let tag (tag : string) (attr : XmlAttribute list) (children : XmlNode list) =
        ((tag, List.toArray attr), children)
        |> ParentNode

    /// Self-closing XmlNode constructor
    let selfClosingTag (tag : string) (attr : XmlAttribute list) =
        (tag, List.toArray attr)
        |> SelfClosingNode

    let html = tag "html"
    let head = tag "head"
    let title = tag "title"
    let style = tag "style"
    let ``base`` = tag "base"
    let body = tag "body"
    let div = tag "div"
    let a = tag "a"
    let h1 = tag "h1"
    let h2 = tag "h2"
    let h3 = tag "h3"
    let h4 = tag "h4"
    let h5 = tag "h5"
    let h6 = tag "h6"
    let p = tag "p"
    let span = tag "span"
    let em = tag "em"
    let strong = tag "strong"
    let b = tag "b"
    let u = tag "u"
    let i = tag "i"
    let blockquote = tag "blockquote"
    let pre = tag "pre"
    let code = tag "code"
    let small = tag "small"
    let sub = tag "sub"
    let sup = tag "sup"
    let dl = tag "dl"
    let dt = tag "dt"
    let dd = tag "dd"
    let ol = tag "ol"
    let ul = tag "ul"
    let li = tag "li"
    let button = tag "button"
    let fieldset = tag "fieldset"
    let form = tag "form"
    let label = tag "label"
    let legend = tag "legend"
    let input = selfClosingTag "input"
    let textarea = tag "textarea"
    let select = tag "select"
    let option = tag "option"
    let optgroup = tag "optgroup"
    let table = tag "table"
    let tbody = tag "tbody"
    let tfoot = tag "tfoot"
    let thead = tag "thead"
    let tr = tag "tr"
    let th = tag "th"
    let td = tag "td"
    let iframe = tag "iframe"
    let figure = tag "figure"
    let figcaption = tag "figcaption"
    let article = tag "article"
    let aside = tag "aside"
    let canvas = tag "canvas"
    let details = tag "details"
    let footer = tag "footer"
    let hgroup = tag "hroup"
    let header = tag "header"
    let main = tag "main"
    let nav = tag "nav"
    let section = tag "section"
    let summary = tag "summary"
    let meta = selfClosingTag "meta"
    let link = selfClosingTag "link"
    let img = selfClosingTag "img"
    let hr = selfClosingTag "hr"
    let br = selfClosingTag "br"    
    let script = tag "script"

module Attr =
    /// XmlAttribute KeyValueAttr constructor
    let create key value = KeyValueAttr (key, value)

    /// XmlAttribute NonValueAttr constructor
    let createBool key = NonValueAttr key

    /// Merge two XmlAttribute lists
    let merge attrs1 attrs2 =
        // TODO replace the append with recursion and cons
        attrs1 @ attrs2
        |> List.map (fun attr -> match attr with KeyValueAttr(k, v) -> k, Some v | NonValueAttr(k) -> k, None)
        |> List.groupBy (fun (k, _) -> k)
        |> List.map (fun (g, attrs) ->
            let attrValue : string option =
                attrs
                |> List.fold (fun acc (_, v) ->
                    match acc, v with
                    | None, _          -> v
                    | Some _, None     -> acc
                    | Some acc, Some v -> Some (String.Join(" ", [| acc; v |]))) None
            match attrValue with
            | None   -> NonValueAttr(g)
            | Some v -> KeyValueAttr(g, v))

    let httpEquiv v = create "http-equiv" v
    let lang v = create "lang" v
    let charset v = create "charset" v
    let content v = create "content" v
    let id v = create "id" v
    let class' v = create "class" v
    let name v = create "name" v
    let alt v = create "alt" v
    let title v = create "title" v
    let rel v = create "rel" v
    let href v = create "href" v
    let target v = create "target" v
    let src v = create "src" v
    let width v = create "width" v
    let height v = create "height" v
    let style v = create "style" v
    let novalidate = createBool "novalidate"
    let action v = create "action" v
    let method v = create "method" v
    let enctype v = create "enctype" v
    let accept v = create "accept" v
    let autocomplete v = create "autocomplete" v
    let autofocus = createBool "autofocus"
    let checked' = createBool "checked"
    let disabled = createBool "disabled"
    let for' v = create "for" v
    let form v = create "form" v
    let max v = create "max" v
    let maxlength v = create "maxlength" v
    let min v = create "min" v
    let multiple = createBool "multiple"
    let pattern v = create "pattern" v
    let placeholder v = create "placeholder" v
    let readonly = createBool "readonly"
    let required = createBool "required"
    let rows v = create "rows" v
    let selected = createBool "selected"
    let step v = create "step" v
    let type' v = create "type" v
    let value v = create "value" v
    let colspan v = create "colspan" v
    let open' = createBool "open"

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
    let svg (width : float) (height : float) =
        Elem.tag "svg" [
            Attr.create "version" "1.0"
            Attr.create "xmlns" "http://www.w3.org/2000/svg"
            Attr.create "viewBox" (sprintf "0 0 %f %f" width height)
        ]