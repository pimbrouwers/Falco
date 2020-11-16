module Falco.Markup

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

/// Render XmlNode recursively to string representation
let renderNode (tag : XmlNode) =  
    let createKeyValue key value =
        strJoin "" [| key; "=\""; value ; "\"" |]

    let createAttr (attr : XmlAttribute) = 
        match attr with 
        | KeyValueAttr (key, value) -> createKeyValue key value
        | NonValueAttr key          -> key

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
        | TextNode text                           -> text
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

module Text =       
    /// Text XmlNode constructor
    let raw content = TextNode content
    
    /// Encoded-text XmlNode constructor
    let enc content = TextNode (WebUtility.HtmlEncode content)
    
module Elem =
    /// Standard XmlNode constructor
    let tag (tag : string) (attr : XmlAttribute list) (children : XmlNode list) =
        ((tag, List.toArray attr), children)
        |> ParentNode 

    /// Self-closing XmlNode constructor
    let selfClosingTag (tag : string) (attr : XmlAttribute list) =
        (tag, List.toArray attr)
        |> SelfClosingNode

    /// <html></html> HTML tag
    let html = tag "html"

    /// <head></head> HTML tag
    let head = tag "head"

    /// <title></title> HTML tag
    let title = tag "title"

    /// <style></style> HTML tag
    let style = tag "style"

    /// <base></base> HTML tag
    let ``base = tag "base"

    /// <body></body> HTML tag
    let body = tag "body"

    /// <div></div> HTML tag
    let div = tag "div"

    /// <a></a> HTML tag
    let a = tag "a"

    /// <h1></h1> HTML tag
    let h1 = tag "h1"

    /// <h2></h2> HTML tag
    let h2 = tag "h2"

    /// <h3></h3> HTML tag
    let h3 = tag "h3"

    /// <h4></h4> HTML tag
    let h4 = tag "h4"

    /// <h5></h5> HTML tag
    let h5 = tag "h5"

    /// <h6></h6> HTML tag
    let h6 = tag "h6"

    /// <p></p> HTML tag
    let p = tag "p"

    /// <span></span> HTML tag
    let span = tag "span"

    /// <em></em> HTML tag
    let em = tag "em"

    /// <strong></strong> HTML tag
    let strong = tag "strong"

    /// <b></b> HTML tag
    let b = tag "b"

    /// <u></u> HTML tag
    let u = tag "u"

    /// <i></i> HTML tag
    let i = tag "i"

    /// <blockquote></blockquote> HTML tag
    let blockquote = tag "blockquote"

    /// <pre></pre> HTML tag
    let pre = tag "pre"

    /// <code></code> HTML tag
    let code = tag "code"

    /// <small></small> HTML tag
    let small = tag "small"

    /// <sub></sub> HTML tag
    let sub = tag "sub"

    /// <sup></sup> HTML tag
    let sup = tag "sup"

    /// <dl></dl> HTML tag
    let dl = tag "dl"

    /// <dt></dt> HTML tag
    let dt = tag "dt"

    /// <dd></dd> HTML tag
    let dd = tag "dd"

    /// <ol></ol> HTML tag
    let ol = tag "ol"

    /// <ul></ul> HTML tag
    let ul = tag "ul"

    /// <li></li> HTML tag
    let li = tag "li"

    /// <fieldset></fieldset> HTML tag
    let fieldset = tag "fieldset"

    /// <form></form> HTML tag
    let form = tag "form"

    /// <label></label> HTML tag
    let label = tag "label"

    /// <legend></legend> HTML tag
    let legend = tag "legend"

    /// <textarea></textarea> HTML tag
    let textarea = tag "textarea"

    /// <select></select> HTML tag
    let select = tag "select"

    /// <option></option> HTML tag
    let option = tag "option"

    /// <optgroup></optgroup> HTML tag
    let optgroup = tag "optgroup"

    /// <table></table> HTML tag
    let table = tag "table"

    /// <tbody></tbody> HTML tag
    let tbody = tag "tbody"

    /// <tfoot></tfoot> HTML tag
    let tfoot = tag "tfoot"

    /// <thead></thead> HTML tag
    let thead = tag "thead"

    /// <tr></tr> HTML tag
    let tr = tag "tr"

    /// <th></th> HTML tag
    let th = tag "th"

    /// <td></td> HTML tag
    let td = tag "td"

    /// <iframe></iframe> HTML tag
    let iframe = tag "iframe"

    /// <figure></figure> HTML tag
    let figure = tag "figure"

    /// <figcaption></figcaption> HTML tag
    let figcaption = tag "figcaption"

    /// <article></article> HTML tag
    let article = tag "article"

    /// <aside></aside> HTML tag
    let aside = tag "aside"

    /// <canvas></canvas> HTML tag
    let canvas = tag "canvas"

    /// <details></details> HTML tag
    let details = tag "details"

    /// <footer></footer> HTML tag
    let footer = tag "footer"

    /// <hroup></hroup> HTML tag
    let hgroup = tag "hroup"

    /// <header></header> HTML tag
    let header = tag "header"

    /// <main></main> HTML tag
    let main = tag "main"

    /// <nav></nav> HTML tag
    let nav = tag "nav"

    /// <section></section> HTML tag
    let section = tag "section"

    /// <summary></summary> HTML tag
    let summary = tag "summary"

    /// <meta /> HTML tag
    let meta = selfClosingTag "meta"

    /// <link /> HTML tag
    let link = selfClosingTag "link"

    /// <img /> HTML tag
    let img = selfClosingTag "img"

    /// <hr /> HTML tag
    let hr = selfClosingTag "hr"

    /// <br /> HTML tag
    let br = selfClosingTag "br"

    /// <input /> HTML tag
    let input = selfClosingTag "input"

module Attr = 
    /// XmlAttribute KeyValueAttr constructor
    let create key value = KeyValueAttr (key, value)
    
    /// XmlAttribute NonValueAttr constructor
    let createBool key = NonValueAttr key 
    
    /// Merge two XmlAttribute lists
    let merge attrs1 attrs2 =
        attrs1 @ attrs2
        |> List.map (fun attr -> match attr with KeyValue(k, v) -> k, Some v | BooleanValue(k) -> k, None)    
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

    /// "http-equiv" HTML Attribute 
    let httpEquiv v = create "http-equip" v

    /// "lang" HTML Attribute 
    let lang v = create "lang" v

    /// "charset" HTML Attribute 
    let charset v = create "charset" v

    /// "content" HTML Attribute 
    let content v = create "content" v

    /// "id" HTML Attribute 
    let id v = create "id" v

    /// "class" HTML Attribute 
    let class' v = create "class" v

    /// "name" HTML Attribute 
    let name v = create "name" v

    /// "alt" HTML Attribute 
    let alt v = create "alt" v

    /// "title" HTML Attribute 
    let title v = create "title" v

    /// "rel" HTML Attribute 
    let rel v = create "rel" v

    /// "href" HTML Attribute 
    let href v = create "href" v

    /// "target" HTML Attribute 
    let target v = create "target" v

    /// "src" HTML Attribute 
    let src v = create "src" v

    /// "width" HTML Attribute 
    let width v = create "width" v

    /// "height" HTML Attribute 
    let height v = create "height" v

    /// "style" HTML Attribute 
    let style v = create "style" v

    /// "novalidate" HTML Attribute 
    let novalidate = createBool "novalidate" 

    /// "action" HTML Attribute 
    let action v = create "action" v

    /// "method" HTML Attribute 
    let method v = create "method" v

    /// "enctype" HTML Attribute 
    let enctype v = create "enctype" v

    /// "accept" HTML Attribute 
    let accept v = create "accept" v

    /// "autocomplete" HTML Attribute 
    let autocomplete v = create "autocomplete" v

    /// "autofocus" HTML Attribute 
    let autofocus = createBool "autofocus"

    /// "checked" HTML Attribute 
    let checked '       = createBool "checked" 

    /// "disabled" HTML Attribute 
    let disabled = createBool "disabled"

    /// "for" HTML Attribute 
    let for' v = create "for" v

    /// "max" HTML Attribute 
    let max v = create "max" v

    /// "maxlength" HTML Attribute 
    let maxlength v = create "maxlength" v

    /// "min" HTML Attribute 
    let min v = create "min" v

    /// "multiple" HTML Attribute 
    let multiple = createBool "multiple"

    /// "pattern" HTML Attribute 
    let pattern v = create "pattern" v

    /// "placeholder" HTML Attribute 
    let placeholder v = create "placeholder" v

    /// "readonly" HTML Attribute 
    let readonly = createBool "readonly"

    /// "required" HTML Attribute 
    let required = createBool "required"

    /// "rows" HTML Attribute 
    let rows v = create "rows" v

    /// "selected" HTML Attribute 
    let selected = createBool "selected"

    /// "step" HTML Attribute 
    let step v = create "step" v

    /// "type" HTML Attribute 
    let type' v = create "type" v

    /// "value" HTML Attribute 
    let value v = create "value" v

module Templates =
    let html5 (langCode : string) (head : XmlNode list) (body : XmlNode list) = 
        let defaultHead = [
            Elem.meta  [ Attr.charset "UTF-8" ]
            Elem.meta  [ Attr.httpEquiv "X-UA-Compatible"; Attr.content "IE=edge, chrome=1" ]
            Elem.meta  [ Attr.name "viewport"; Attr.content "width=device-width, initial-scale=1" ]                
        ]

        Elem.html [ Attr.lang "en"; ] [
            Elem.head [] (defaultHead @ head)
            Elem.body [] body
        ] 
