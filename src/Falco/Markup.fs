module Falco.Markup

open System
open System.Globalization
open System.IO
open System.Net
open Falco.StringUtils

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
    let sb = Text.StringBuilder(5000)
    let w = new StringWriter(sb, CultureInfo.InvariantCulture)
    XmlNode.serialize w tag

/// Render XmlNode as HTML string
let renderHtml tag =
    let sb = Text.StringBuilder(5000)
    let w = new StringWriter(sb, CultureInfo.InvariantCulture)
    w.Write "<!DOCTYPE html>"
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

    /// HTML Tag <html></html>
    let html = tag "html"

    /// HTML Tag <head></head>
    let head = tag "head"

    /// HTML Tag <title></title>
    let title = tag "title"

    /// HTML Tag <style></style>
    let style = tag "style"

    /// HTML Tag <base></base>
    let ``base`` = tag "base"

    /// HTML Tag <body></body>
    let body = tag "body"

    /// HTML Tag <div></div>
    let div = tag "div"

    /// HTML Tag <a></a>
    let a = tag "a"

    /// HTML Tag <h1></h1>
    let h1 = tag "h1"

    /// HTML Tag <h2></h2>
    let h2 = tag "h2"

    /// HTML Tag <h3></h3>
    let h3 = tag "h3"

    /// HTML Tag <h4></h4>
    let h4 = tag "h4"

    /// HTML Tag <h5></h5>
    let h5 = tag "h5"

    /// HTML Tag <h6></h6>
    let h6 = tag "h6"

    /// HTML Tag <p></p>
    let p = tag "p"

    /// HTML Tag <span></span>
    let span = tag "span"

    /// HTML Tag <em></em>
    let em = tag "em"

    /// HTML Tag <strong></strong>
    let strong = tag "strong"

    /// HTML Tag <b></b>
    let b = tag "b"

    /// HTML Tag <u></u>
    let u = tag "u"

    /// HTML Tag <i></i>
    let i = tag "i"

    /// HTML Tag <blockquote></blockquote>
    let blockquote = tag "blockquote"

    /// HTML Tag <pre></pre>
    let pre = tag "pre"

    /// HTML Tag <code></code>
    let code = tag "code"

    /// HTML Tag <small></small>
    let small = tag "small"

    /// HTML Tag <sub></sub>
    let sub = tag "sub"

    /// HTML Tag <sup></sup>
    let sup = tag "sup"

    /// HTML Tag <dl></dl>
    let dl = tag "dl"

    /// HTML Tag <dt></dt>
    let dt = tag "dt"

    /// HTML Tag <dd></dd>
    let dd = tag "dd"

    /// HTML Tag <ol></ol>
    let ol = tag "ol"

    /// HTML Tag <ul></ul>
    let ul = tag "ul"

    /// HTML Tag <li></li>
    let li = tag "li"

    /// HTML Tag <button></button>
    let button = tag "button"

    /// HTML Tag <fieldset></fieldset>
    let fieldset = tag "fieldset"

    /// HTML Tag <form></form>
    let form = tag "form"

    /// HTML Tag <label></label>
    let label = tag "label"

    /// HTML Tag <legend></legend>
    let legend = tag "legend"

    /// HTML Tag <input />
    let input = selfClosingTag "input"

    /// HTML Tag <textarea></textarea>
    let textarea = tag "textarea"

    /// HTML Tag <select></select>
    let select = tag "select"

    /// HTML Tag <option></option>
    let option = tag "option"

    /// HTML Tag <optgroup></optgroup>
    let optgroup = tag "optgroup"

    /// HTML Tag <table></table>
    let table = tag "table"

    /// HTML Tag HTML Tag <tbody></tbody>
    let tbody = tag "tbody"

    /// HTML Tag <tfoot></tfoot>
    let tfoot = tag "tfoot"

    /// HTML Tag <thead></thead>
    let thead = tag "thead"

    /// HTML Tag <tr></tr>
    let tr = tag "tr"

    /// HTML Tag <th></th>
    let th = tag "th"

    /// HTML Tag <td></td>
    let td = tag "td"

    /// HTML Tag <iframe></iframe>
    let iframe = tag "iframe"

    /// HTML Tag <figure></figure>
    let figure = tag "figure"

    /// HTML Tag <figcaption></figcaption>
    let figcaption = tag "figcaption"

    /// HTML Tag <article></article>
    let article = tag "article"

    /// HTML Tag <aside></aside>
    let aside = tag "aside"

    /// HTML Tag <canvas></canvas>
    let canvas = tag "canvas"

    /// HTML Tag <details></details>
    let details = tag "details"

    /// HTML Tag <footer></footer>
    let footer = tag "footer"

    /// HTML Tag <hroup></hroup>
    let hgroup = tag "hroup"

    /// HTML Tag <header></header>
    let header = tag "header"

    /// HTML Tag <main></main>
    let main = tag "main"

    /// HTML Tag <nav></nav>
    let nav = tag "nav"

    /// HTML Tag <section></section>
    let section = tag "section"

    /// HTML Tag <summary></summary>
    let summary = tag "summary"

    /// HTML Tag <meta />
    let meta = selfClosingTag "meta"

    /// HTML Tag <link />
    let link = selfClosingTag "link"

    /// HTML Tag <img />
    let img = selfClosingTag "img"

    /// HTML Tag <hr />
    let hr = selfClosingTag "hr"

    /// HTML Tag <br />
    let br = selfClosingTag "br"

    /// HTML TAG <script></script>
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
                    | Some acc, Some v -> Some (strJoin " " [| acc; v |])) None
            match attrValue with
            | None   -> NonValueAttr(g)
            | Some v -> KeyValueAttr(g, v))

   /// HTML Attribute "http-equiv"
    let httpEquiv v = create "http-equip" v

    /// HTML Attribute "lang"
    let lang v = create "lang" v

    /// HTML Attribute "charset"
    let charset v = create "charset" v

    /// HTML Attribute "content"
    let content v = create "content" v

    /// HTML Attribute "id"
    let id v = create "id" v

    /// HTML Attribute "class"
    let class' v = create "class" v

    /// HTML Attribute "name"
    let name v = create "name" v

    /// HTML Attribute "alt"
    let alt v = create "alt" v

    /// HTML Attribute "title"
    let title v = create "title" v

    /// HTML Attribute "rel"
    let rel v = create "rel" v

    /// HTML Attribute "href"
    let href v = create "href" v

    /// HTML Attribute "target"
    let target v = create "target" v

    /// HTML Attribute "src"
    let src v = create "src" v

    /// HTML Attribute "width"
    let width v = create "width" v

    /// HTML Attribute "height"
    let height v = create "height" v

    /// HTML Attribute "style"
    let style v = create "style" v

    /// HTML Attribute "novalidate"
    let novalidate = createBool "novalidate"

    /// HTML Attribute "action"
    let action v = create "action" v

    /// HTML Attribute "method"
    let method v = create "method" v

    /// HTML Attribute "enctype"
    let enctype v = create "enctype" v

    /// HTML Attribute "accept"
    let accept v = create "accept" v

    /// HTML Attribute "autocomplete"
    let autocomplete v = create "autocomplete" v

    /// HTML Attribute "autofocus"
    let autofocus = createBool "autofocus"

    /// HTML Attribute "checked"
    let checked' = createBool "checked"

    /// HTML Attribute "disabled"
    let disabled = createBool "disabled"

    /// HTML Attribute "for"
    let for' v = create "for" v

    /// HTML Attribute "form"
    let form v = create "form" v

    /// HTML Attribute "max"
    let max v = create "max" v

    /// HTML Attribute "maxlength"
    let maxlength v = create "maxlength" v

    /// HTML Attribute "min"
    let min v = create "min" v

    /// HTML Attribute "multiple"
    let multiple = createBool "multiple"

    /// HTML Attribute "pattern"
    let pattern v = create "pattern" v

    /// HTML Attribute "placeholder"
    let placeholder v = create "placeholder" v

    /// HTML Attribute "readonly"
    let readonly = createBool "readonly"

    /// HTML Attribute "required"
    let required = createBool "required"

    /// HTML Attribute "rows"
    let rows v = create "rows" v

    /// HTML Attribute "selected"
    let selected = createBool "selected"

    /// HTML Attribute "step"
    let step v = create "step" v

    /// HTML Attribute "type"
    let type' v = create "type" v

    /// HTML Attribute "value"
    let value v = create "value" v

    /// HTML Attribute "colspan"
    let colspan v = create "colspan" v

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
