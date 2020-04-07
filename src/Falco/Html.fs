module Falco.ViewEngine

open System.Net
    
// Specifies an XML-style attribute
type XmlAttribute =
    | KeyValue of string * string
    | NonValue  of string

// Represents an XML-style element containing attributes
type XmlElement = 
    string * XmlAttribute[]

// Describes the different XML-style node patterns
type XmlNode =
    | ParentNode      of XmlElement * XmlNode list 
    | SelfClosingNode of XmlElement                
    | Text            of string   


// XmlAttribute constructor
let attr key value = 
    match value with 
    | Some v -> KeyValue (key, v)
    | None   -> NonValue key

// Text XmlNode constructor
let raw content = Text content

// Encoded-text XmlNode constructor
let enc content = Text (WebUtility.HtmlEncode content)

// Standard XmlNode constructor
let tag (tag : string) (attr : XmlAttribute list) (children : XmlNode list) =
    ((tag, List.toArray attr), children)
    |> ParentNode 

// Self-closing XmlNode constructor
let selfClosingTag (tag : string) (attr : XmlAttribute list) =
    (tag, List.toArray attr)
    |> SelfClosingNode

// Render XmlNode recursively to string representation
let renderNode tag =   
    let rec buildXml doc tag =
        let createAttrs attrs =
            attrs
            |> Array.map (fun attr ->
                match attr with 
                | KeyValue (k, v) -> (sprintf " %s=\"%s\"" k v)
                | NonValue k      -> k)
            |> strJoin ""

           
        match tag with 
        | Text text -> 
            text :: doc
        | SelfClosingNode (e, attrs) -> 
            sprintf "<%s%s />" e (createAttrs attrs) :: doc
        | ParentNode ((e, attrs), children) ->        
            let c =             
                [|
                    for c in children do 
                        buildXml [] c 
                        |> List.toArray 
                        |> strJoin ""
                |]
            sprintf "<%s%s>%s</%s>" e (createAttrs attrs) (strJoin "" c) e :: doc
    
    buildXml [] tag
    |> List.toArray
    |> strJoin ""

// Render XmlNode as HTML string
let renderHtml tag =
    [|
        "<!DOCTYPE html>"
        renderNode tag
    |]
    |> strJoin ""

// ------------
// HTML Tags
// ------------
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

// HTML 5
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

// ------------
// HTML Attributes
// ------------
let _httpEquiv v   = attr "http-equip" (Some v)
let _lang v        = attr "lang" (Some v)
let _charset v     = attr "charset" (Some v)
let _content v     = attr "content" (Some v)
let _id v          = attr "id" (Some v)
let _class v       = attr "class" (Some v)
let _name v        = attr "name" (Some v)
let _alt v         = attr "alt" (Some v)
let _title v       = attr "title" (Some v)
let _rel v         = attr "rel" (Some v)
let _href v        = attr "href" (Some v)
let _target v      = attr "target" (Some v)
let _src v         = attr "src" (Some v)
let _width v       = attr "width" (Some v)
let _height v      = attr "height" (Some v)
let _style v       = attr "style" (Some v)
let _novalidate v  = attr "novalidate" None
let _action v      = attr "action" (Some v)
let _method v      = attr "method" (Some v)
let _enctype v     = attr "enctype" (Some v)
let _for v         = attr "for" (Some v)
let _type v        = attr "type" (Some v)
let _value v       = attr "value" (Some v)
let _placeholder v = attr "placeholder" (Some v)
let _multiple v    = attr "multiple" None
let _accept v      = attr "accept" (Some v)
let _min v         = attr "min" (Some v)
let _max v         = attr "max" (Some v)
let _maxlength v   = attr "maxlength" (Some v)
let _checked       = attr "checked" None
let _selected      = attr "selected" None
let _disabled v    = attr "disabled" None
let _readonly v    = attr "readonly" None