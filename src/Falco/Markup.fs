module Falco.Markup

open System.Net
open Falco.StringUtils

/// Specifies an XML-style attribute
type XmlAttribute =
    | KeyValue of string * string
    | BooleanValue of string

/// Represents an XML-style element containing attributes
type XmlElement = 
    string * XmlAttribute[]

/// Describes the different XML-style node patterns
type XmlNode =
    | ParentNode      of XmlElement * XmlNode list 
    | SelfClosingNode of XmlElement                
    | Text            of string   

/// Text XmlNode constructor
let raw content = Text content

/// Encoded-text XmlNode constructor
let enc content = Text (WebUtility.HtmlEncode content)

/// Render XmlNode recursively to string representation
let renderNode (tag : XmlNode) =  
    let createKeyValue key value =
        strJoin "" [| key; "=\""; value ; "\"" |]

    let createAttr (attr : XmlAttribute) = 
        match attr with 
        | KeyValue (key, value) -> createKeyValue key value
        | BooleanValue key      -> key

    let createAttrs (attrs : XmlAttribute[]) =
        attrs
        |> Array.map createAttr
        |> strJoin " "   

    let createSelfClosingTag (tag : string) (attrs : XmlAttribute[]) =
        if attrs.Length > 0 then
            strJoin "" [| "<"; tag; " "; (createAttrs attrs); " />" |] 
        else 
            strJoin "" [| "<"; tag; " />" |]

    let createTag (children : string) (tag : string) (attrs : XmlAttribute[]) =
        if attrs.Length > 0 then
            strJoin "" [| "<"; tag; " "; (createAttrs attrs); ">"; children; "</"; tag; ">" |]
        else 
            strJoin "" [| "<"; tag; ">"; children; "</"; tag; ">" |]

    let rec buildXml doc tag =   
        let buildChildXml (children : XmlNode list) =
            [|
                for c in children do 
                    buildXml [] c 
                    |> List.toArray 
                    |> strJoin ""
            |]
            |> strJoin ""

        match tag with 
        | Text text                           -> text
        | SelfClosingNode (tag, attrs)        -> createSelfClosingTag tag attrs
        | ParentNode ((tag, attrs), children) -> createTag (buildChildXml children) tag attrs 
        :: doc            
    
    buildXml [] tag
    |> List.toArray
    |> strJoin ""

/// Render XmlNode as HTML string
let renderHtml tag =
    [|
        "<!DOCTYPE html>"
        renderNode tag
    |]
    |> strJoin ""

module Elem =
    /// Standard XmlNode constructor
    let tag (tag : string) (attr : XmlAttribute list) (children : XmlNode list) =
        ((tag, List.toArray attr), children)
        |> ParentNode 

    /// Self-closing XmlNode constructor
    let selfClosingTag (tag : string) (attr : XmlAttribute list) =
        (tag, List.toArray attr)
        |> SelfClosingNode

    let html     = tag "html"
    let head     = tag "head"
    let title    = tag "title"
    let meta     = selfClosingTag "meta"
    let link     = selfClosingTag "link"
    let style    = tag "style"
    let ``base`` = tag "base"

    let body       = tag "body"
    let div        = tag "div"
    let a          = tag "a"
    let img        = selfClosingTag "img"
    let h1         = tag "h1"
    let h2         = tag "h2"
    let h3         = tag "h3"
    let h4         = tag "h4"
    let h5         = tag "h5"
    let h6         = tag "h6"
    let p          = tag "p"
    let span       = tag "span"
    let em         = tag "em"
    let strong     = tag "strong"
    let b          = tag "b"
    let u          = tag "u"
    let i          = tag "i"
    let blockquote = tag "blockquote"
    let pre        = tag "pre"
    let code       = tag "code"
    let sub        = tag "sub"
    let sup        = tag "sup"
    let dl         = tag "dl"
    let dt         = tag "dt"
    let dd         = tag "dd"
    let ol         = tag "ol"
    let ul         = tag "ul"
    let li         = tag "li"
    let hr         = selfClosingTag "hr"
    let br         = selfClosingTag "br"
    let fieldset   = tag "fieldset"
    let form       = tag "form"
    let label      = tag "label"
    let legend     = tag "legend"
    let input      = selfClosingTag "input"
    let textarea   = tag "textarea"
    let select     = tag "select"
    let option     = tag "option"
    let optgroup   = tag "optgroup"
    let table      = tag "table"
    let tbody      = tag "tbody"
    let tfoot      = tag "tfoot"
    let thead      = tag "thead"
    let tr         = tag "tr"
    let th         = tag "th"
    let td         = tag "td"
    let iframe     = tag "iframe"
    let figure     = tag "figure"
    let figcaption = tag "figcaption"

    /// HTML 5
    let article = tag "article"
    let aside   = tag "aside"
    let canvas  = tag "canvas"
    let details = tag "details"
    let footer  = tag "footer"
    let hgroup  = tag "hroup"
    let header  = tag "header"
    let main    = tag "main"
    let nav     = tag "nav"
    let section = tag "section"
    let summary = tag "summary"

/// ------------
/// HTML Attributes
/// ------------

module Attr = 
    /// XmlAttribute constructor
    let attr key value = KeyValue (key, value)
    let attrBool key = BooleanValue key 

    let httpEquiv v   = attr "http-equip" v
    let lang v        = attr "lang" v
    let charset v     = attr "charset" v
    let content v     = attr "content" v
    let id v          = attr "id" v
    let class' v      = attr "class" v
    let name v        = attr "name" v
    let alt v         = attr "alt" v
    let title v       = attr "title" v
    let rel v         = attr "rel" v
    let href v        = attr "href" v
    let target v      = attr "target" v
    let src v         = attr "src" v
    let width v       = attr "width" v
    let height v      = attr "height" v
    let style v       = attr "style" v

    /// Forms
    let novalidate    = attrBool "novalidate" 
    let action v      = attr "action" v
    let method v      = attr "method" v
    let enctype v     = attr "enctype" v

    /// Inputs
    let accept v       = attr "accept" v
    let autocomplete v = attr "autocomplete" v
    let autofocus      = attrBool "autofocus"
    let checked'       = attrBool "checked" 
    let disabled       = attrBool "disabled"
    let for' v         = attr "for" v
    let max v          = attr "max" v
    let maxlength v    = attr "maxlength" v
    let min v          = attr "min" v
    let multiple       = attrBool "multiple"
    let pattern v      = attr "pattern" v
    let placeholder v  = attr "placeholder" v
    let readonly       = attrBool "readonly"
    let required       = attrBool "required"
    let rows v         = attr "rows" v
    let selected       = attrBool "selected"
    let step v         = attr "step" v
    let type' v        = attr "type" v
    let value v        = attr "value" v