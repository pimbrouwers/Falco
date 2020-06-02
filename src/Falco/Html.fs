module Falco.ViewEngine

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


/// XmlAttribute constructor
let attr key value = KeyValue (key, value)
let attrBool key = BooleanValue key 

/// Text XmlNode constructor
let raw content = Text content

/// Encoded-text XmlNode constructor
let enc content = Text (WebUtility.HtmlEncode content)

/// Standard XmlNode constructor
let tag (tag : string) (attr : XmlAttribute list) (children : XmlNode list) =
    ((tag, List.toArray attr), children)
    |> ParentNode 

/// Self-closing XmlNode constructor
let selfClosingTag (tag : string) (attr : XmlAttribute list) =
    (tag, List.toArray attr)
    |> SelfClosingNode

/// Render XmlNode recursively to string representation
let renderNode tag =   
    let rec buildXml doc tag =
        let createAttrs attrs =
            attrs
            |> Array.map (fun attr ->
                match attr with 
                | KeyValue (k, v) -> strJoin "" [| k; "=\""; v; "\"" |]
                | BooleanValue k  -> k)            
            |> strJoin " "            

           
        match tag with 
        | Text text -> 
            text :: doc
        | SelfClosingNode (e, attrs) -> 
            if attrs.Length > 0 then
                strJoin "" [| "<"; e; " "; (createAttrs attrs); " />" |] 
            else 
                strJoin "" [| "<"; e; " />" |]
            :: doc            
        | ParentNode ((e, attrs), children) ->        
            let c =             
                [|
                    for c in children do 
                        buildXml [] c 
                        |> List.toArray 
                        |> strJoin ""
                |]
            
            if attrs.Length > 0 then
                strJoin "" [| "<"; e; " "; (createAttrs attrs); ">"; (strJoin "" c); "</"; e; ">" |]
            else 
                strJoin "" [| "<"; e; ">"; (strJoin "" c); "</"; e; ">" |]
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

/// ------------
/// HTML Tags
/// ------------
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
let _httpEquiv v   = attr "http-equip" v
let _lang v        = attr "lang" v
let _charset v     = attr "charset" v
let _content v     = attr "content" v
let _id v          = attr "id" v
let _class v       = attr "class" v
let _name v        = attr "name" v
let _alt v         = attr "alt" v
let _title v       = attr "title" v
let _rel v         = attr "rel" v
let _href v        = attr "href" v
let _target v      = attr "target" v
let _src v         = attr "src" v
let _width v       = attr "width" v
let _height v      = attr "height" v
let _style v       = attr "style" v

/// Forms
let _novalidate    = attrBool "novalidate" 
let _action v      = attr "action" v
let _method v      = attr "method" v
let _enctype v     = attr "enctype" v

/// Inputs
let _accept v       = attr "accept" v
let _autocomplete v = attr "autocomplete" v
let _autofocus      = attrBool "autofocus"
let _checked        = attrBool "checked" 
let _disabled       = attrBool "disabled"
let _for v          = attr "for" v
let _max v          = attr "max" v
let _maxlength v    = attr "maxlength" v
let _min v          = attr "min" v
let _multiple       = attrBool "multiple"
let _pattern v      = attr "pattern" v
let _placeholder v  = attr "placeholder" v
let _readonly       = attrBool "readonly"
let _required       = attrBool "required"
let _rows v         = attr "rows" v
let _selected       = attrBool "selected"
let _step v         = attr "step" v
let _type v         = attr "type" v
let _value v        = attr "value" v