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
    let serialize (w : StringWriter, xml : XmlNode) =
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
    XmlNode.serialize(w, tag)

/// Render XmlNode as HTML string
let renderHtml (tag : XmlNode) =
    let sb = Text.StringBuilder()
    let w = new StringWriter(sb, CultureInfo.InvariantCulture)
    w.Write "<!DOCTYPE html>"
    XmlNode.serialize(w, tag)

/// Render XmlNode as XML string
let renderXml (tag : XmlNode) =
    let sb = Text.StringBuilder()
    let w = new StringWriter(sb, CultureInfo.InvariantCulture)
    w.Write "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
    XmlNode.serialize(w, tag)

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

    // Main root
    let html = tag "html"

    // Document metadata
    let base' = selfClosingTag "base"
    let head = tag "head"
    let link = selfClosingTag "link"
    let meta = selfClosingTag "meta"
    let style = tag "style"
    let title = tag "title"

    // Sectioning root
    let body = tag "body"

    // Content sectioning
    let address = tag "address"
    let article = tag "article"
    let aside = tag "aside"
    let footer = tag "footer"
    let header = tag "header"
    let h1 = tag "h1"
    let h2 = tag "h2"
    let h3 = tag "h3"
    let h4 = tag "h4"
    let h5 = tag "h5"
    let h6 = tag "h6"
    let main = tag "main"
    let nav = tag "nav"
    let section = tag "section"

    // Text content
    let blockquote = tag "blockquote"
    let dd = tag "dd"
    let div = tag "div"
    let dl = tag "dl"
    let dt = tag "dt"
    let figcaption = tag "figcaption"
    let figure = tag "figure"
    let hr = selfClosingTag "hr"
    let li = tag "li"
    let menu = tag "menu"
    let ol = tag "ol"
    let p = tag "p"
    let pre = tag "pre"
    let ul = tag "ul"

    // Inline text semantics
    let a = tag "a"
    let abbr = tag "abbr"
    let b = tag "b"
    let bdi = tag "bdi"
    let bdo = tag "bdo"
    let br = selfClosingTag "br"
    let cite = tag "cite"
    let code = tag "code"
    let data = tag "data"
    let dfn = tag "dfn"
    let em = tag "em"
    let i = tag "i"
    let kbd = tag "kbd"
    let mark = tag "mark"
    let q = tag "q"
    let rp = tag "rp"
    let rt = tag "rt"
    let ruby = tag "ruby"
    let s = tag "s"
    let samp = tag "samp"
    let small = tag "small"
    let span = tag "span"
    let strong = tag "strong"
    let sub = tag "sub"
    let sup = tag "sup"
    let time = tag "time"
    let u = tag "u"
    let var = tag "var"
    let wbr = selfClosingTag "wbr"

    // Image and multimedia
    let area = tag "area"
    let audio = tag "audio"
    let img = selfClosingTag "img"
    let map = tag "map"
    let track = selfClosingTag "track"
    let video = tag "video"

    // Embedded content
    let embed = selfClosingTag "embed"
    let iframe = tag "iframe"
    let object = tag "object"
    let picture = tag "picture"
    let portal = tag "portal"
    let source = selfClosingTag "source"

    // SVG and MathML
    let svg = tag "svg"
    let math = tag "math"

    // Scripting
    let canvas = tag "canvas"
    let noscript = tag "noscript"
    let script = tag "script"

    // Demarcating edits
    let del = tag "del"
    let ins = tag "ins"

    // Table content
    let caption = tag "caption"
    let col = selfClosingTag "col"
    let colgroup = tag "colgroup"
    let table = tag "table"
    let tbody = tag "tbody"
    let td = tag "td"
    let tfoot = tag "tfoot"
    let th = tag "th"
    let thead = tag "thead"
    let tr = tag "tr"

    // Forms
    let button = tag "button"
    let datalist = tag "datalist"
    let fieldset = tag "fieldset"
    let form = tag "form"
    let input = selfClosingTag "input"
    let label = tag "label"
    let legend = tag "legend"
    let meter = tag "meter"
    let optgroup = tag "optgroup"
    let option = tag "option"
    let output = tag "output"
    let progress = tag "progress"
    let select = tag "select"
    let textarea = tag "textarea"

    // Interactive elements
    let details = tag "details"
    let dialog = tag "dialog"
    let summary = tag "summary"

    // Web Components
    let slot = tag "slot"
    let template = tag "template"

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

    let accept = create "accept"
    let acceptCharset = create "accept-charset"
    let accesskey = create "accesskey"
    let action = create "action"
    let align = create "align"
    let allow = create "allow"
    let alt = create "alt"
    let async = createBool "async"
    let autocapitalize = create "autocapitalize"
    let autocomplete = create "autocomplete"
    let autofocus = createBool "autofocus"
    let autoplay = createBool "autoplay"
    let background = create "background"
    let bgcolor = create "bgcolor"
    let border = create "border"
    let buffered = create "buffered"
    let capture = create "capture"
    let challenge = create "challenge"
    let charset = create "charset"
    let checked' = createBool "checked"
    let cite = create "cite"
    let class' = create "class"
    let code = create "code"
    let codebase = create "codebase"
    let color = create "color"
    let cols = create "cols"
    let colspan = create "colspan"
    let content = create "content"
    let contenteditable = create "contenteditable"
    let contextmenu = create "contextmenu"
    let controls = createBool "controls"
    let coords = create "coords"
    let crossorigin = create "crossorigin"
    let csp = create "csp"
    let data = create "data"
    let dataAttr name = create (sprintf "data-%s" name)
    let datetime = create "datetime"
    let decoding = create "decoding"
    let default' = createBool "default"
    let defer = createBool "defer"
    let dir = create "dir"
    let dirname = create "dirname"
    let disabled = createBool "disabled"
    let download = create "download"
    let draggable = create "draggable"
    let enctype = create "enctype"
    let enterkeyhint = create "enterkeyhint"
    let for' = create "for"
    let form = create "form"
    let formaction = create "formaction"
    let formenctype = create "formenctype"
    let formmethod = create "formmethod"
    let formnovalidate = createBool "formnovalidate"
    let formtarget = create "formtarget"
    let headers = create "headers"
    let height = create "height"
    let hidden = createBool "hidden"
    let high = create "high"
    let href = create "href"
    let hreflang = create "hreflang"
    let httpEquiv = create "http-equiv"
    let icon = create "icon"
    let id = create "id"
    let importance = create "importance"
    let integrity = create "integrity"
    let inputmode = create "inputmode"
    let ismap = createBool "ismap"
    let itemprop = create "itemprop"
    let keytype = create "keytype"
    let kind = create "kind"
    let label = create "label"
    let lang = create "lang"
    let loading = create "loading"
    let list = create "list"
    let loop = createBool "loop"
    let low = create "low"
    let max = create "max"
    let maxlength = create "maxlength"
    let minlength = create "minlength"
    let media = create "media"
    let method = create "method"
    let min = create "min"
    let multiple = createBool "multiple"
    let muted = createBool "muted"
    let name = create "name"
    let novalidate = createBool "novalidate"
    let open' = create "open"
    let optimum = create "optimum"
    let pattern = create "pattern"
    let ping = create "ping"
    let placeholder = create "placeholder"
    let poster = create "poster"
    let preload = create "preload"
    let radiogroup = create "radiogroup"
    let readonly = createBool "readonly"
    let referrerpolicy = create "referrerpolicy"
    let rel = create "rel"
    let required = createBool "required"
    let reversed = createBool "reversed"
    let role = create "role"
    let rows = create "rows"
    let rowspan = create "rowspan"
    let sandbox = create "sandbox"
    let scope = create "scope"
    let selected = createBool "selected"
    let shape = create "shape"
    let size = create "size"
    let sizes = create "sizes"
    let slot = create "slot"
    let span = create "span"
    let spellcheck = create "spellcheck"
    let src = create "src"
    let srcdoc = create "srcdoc"
    let srclang = create "srclang"
    let srcset = create "srcset"
    let start = create "start"
    let step = create "step"
    let style = create "style"
    let tabindex = create "tabindex"
    let target = create "target"
    let title = create "title"
    let translate = create "translate"
    let type' = create "type"
    let usemap = create "usemap"
    let value = create "value"
    let width = create "width"
    let wrap = create "wrap"

    // Events
    let onabort = create "abort"
    let onafterprint = create "afterprint"
    let onanimationend = create "animationend"
    let onanimationiteration = create "animationiteration"
    let onanimationstart = create "animationstart"
    let onbeforeprint = create "beforeprint"
    let onbeforeunload = create "beforeunload"
    let onblur = create "blur"
    let oncanplay = create "canplay"
    let oncanplaythrough = create "canplaythrough"
    let onchange = create "change"
    let onclick = create "click"
    let oncontextmenu = create "contextmenu"
    let oncopy = create "copy"
    let oncut = create "cut"
    let ondblclick = create "dblclick"
    let ondrag = create "drag"
    let ondragend = create "dragend"
    let ondragenter = create "dragenter"
    let ondragleave = create "dragleave"
    let ondragover = create "dragover"
    let ondragstart = create "dragstart"
    let ondrop = create "drop"
    let ondurationchange = create "durationchange"
    let onended = create "ended"
    let onerror = create "error"
    let onfocus = create "focus"
    let onfocusin = create "focusin"
    let onfocusout = create "focusout"
    let onfullscreenchange = create "fullscreenchange"
    let onfullscreenerror = create "fullscreenerror"
    let onhashchange = create "hashchange"
    let oninput = create "input"
    let oninvalid = create "invalid"
    let onkeydown = create "keydown"
    let onkeypress = create "keypress"
    let onkeyup = create "keyup"
    let onload = create "load"
    let onloadeddata = create "loadeddata"
    let onloadedmetadata = create "loadedmetadata"
    let onloadstart = create "loadstart"
    let onmessage = create "message"
    let onmousedown = create "mousedown"
    let onmouseenter = create "mouseenter"
    let onmouseleave = create "mouseleave"
    let onmousemove = create "mousemove"
    let onmouseover = create "mouseover"
    let onmouseout = create "mouseout"
    let onmouseup = create "mouseup"
    let onmousewheel = create "mousewheel"
    let onoffline = create "offline"
    let ononline = create "online"
    let onopen = create "open"
    let onpagehide = create "pagehide"
    let onpageshow = create "pageshow"
    let onpaste = create "paste"
    let onpause = create "pause"
    let onplay = create "play"
    let onplaying = create "playing"
    let onpopstate = create "popstate"
    let onprogress = create "progress"
    let onratechange = create "ratechange"
    let onresize = create "resize"
    let onreset = create "reset"
    let onscroll = create "scroll"
    let onsearch = create "search"
    let onseeked = create "seeked"
    let onseeking = create "seeking"
    let onselect = create "select"
    let onshow = create "show"
    let onstalled = create "stalled"
    let onstorage = create "storage"
    let onsubmit = create "submit"
    let onsuspend = create "suspend"
    let ontimeupdate = create "timeupdate"
    let ontoggle = create "toggle"
    let ontouchcancel = create "touchcancel"
    let ontouchend = create "touchend"
    let ontouchmove = create "touchmove"
    let ontouchstart = create "touchstart"
    let ontransitionend = create "transitionend"
    let onunload = create "unload"
    let onvolumechange = create "volumechange"
    let onwaiting = create "waiting"
    let onwheel = create "wheel"

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