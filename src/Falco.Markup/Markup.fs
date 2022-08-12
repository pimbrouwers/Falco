namespace Falco.Markup

open System
open System.Globalization
open System.IO
open System.Net

[<AutoOpen>]
module Renderer = 
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
    let create (tag : string) (attr : XmlAttribute list) (children : XmlNode list) =
        ((tag, List.toArray attr), children)
        |> ParentNode

    /// Self-closing XmlNode constructor
    let createSelfClosing (tag : string) (attr : XmlAttribute list) =
        (tag, List.toArray attr)
        |> SelfClosingNode

    // Main root
    let html = create "html"

    // Document metadata
    let base' = createSelfClosing "base"
    let head = create "head"
    let link = createSelfClosing "link"
    let meta = createSelfClosing "meta"
    let style = create "style"
    let title = create "title"

    // Sectioning root
    let body = create "body"

    // Content sectioning
    let address = create "address"
    let article = create "article"
    let aside = create "aside"
    let footer = create "footer"
    let header = create "header"
    let h1 = create "h1"
    let h2 = create "h2"
    let h3 = create "h3"
    let h4 = create "h4"
    let h5 = create "h5"
    let h6 = create "h6"
    let main = create "main"
    let nav = create "nav"
    let section = create "section"

    // Text content
    let blockquote = create "blockquote"
    let dd = create "dd"
    let div = create "div"
    let dl = create "dl"
    let dt = create "dt"
    let figcaption = create "figcaption"
    let figure = create "figure"
    let hr = createSelfClosing "hr"
    let li = create "li"
    let menu = create "menu"
    let ol = create "ol"
    let p = create "p"
    let pre = create "pre"
    let ul = create "ul"

    // Inline text semantics
    let a = create "a"
    let abbr = create "abbr"
    let b = create "b"
    let bdi = create "bdi"
    let bdo = create "bdo"
    let br = createSelfClosing "br"
    let cite = create "cite"
    let code = create "code"
    let data = create "data"
    let dfn = create "dfn"
    let em = create "em"
    let i = create "i"
    let kbd = create "kbd"
    let mark = create "mark"
    let q = create "q"
    let rp = create "rp"
    let rt = create "rt"
    let ruby = create "ruby"
    let s = create "s"
    let samp = create "samp"
    let small = create "small"
    let span = create "span"
    let strong = create "strong"
    let sub = create "sub"
    let sup = create "sup"
    let time = create "time"
    let u = create "u"
    let var = create "var"
    let wbr = createSelfClosing "wbr"

    // Image and multimedia
    let area = create "area"
    let audio = create "audio"
    let img = createSelfClosing "img"
    let map = create "map"
    let track = createSelfClosing "track"
    let video = create "video"

    // Embedded content
    let embed = createSelfClosing "embed"
    let iframe = create "iframe"
    let object = create "object"
    let picture = create "picture"
    let portal = create "portal"
    let source = createSelfClosing "source"

    // SVG and MathML
    let svg = create "svg"
    let math = create "math"

    // Scripting
    let canvas = create "canvas"
    let noscript = create "noscript"
    let script = create "script"

    // Demarcating edits
    let del = create "del"
    let ins = create "ins"

    // Table content
    let caption = create "caption"
    let col = createSelfClosing "col"
    let colgroup = create "colgroup"
    let table = create "table"
    let tbody = create "tbody"
    let td = create "td"
    let tfoot = create "tfoot"
    let th = create "th"
    let thead = create "thead"
    let tr = create "tr"

    // Forms
    let button = create "button"
    let datalist = create "datalist"
    let fieldset = create "fieldset"
    let form = create "form"
    let input = createSelfClosing "input"
    let label = create "label"
    let legend = create "legend"
    let meter = create "meter"
    let optgroup = create "optgroup"
    let option = create "option"
    let output = create "output"
    let progress = create "progress"
    let select = create "select"
    let textarea = create "textarea"

    // Interactive elements
    let details = create "details"
    let dialog = create "dialog"
    let summary = create "summary"

    // Web Components
    let slot = create "slot"
    let template = create "template"

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

module Svg = 
    let a = Elem.create "a"
    let animate = Elem.create "animate"
    let animateMotion = Elem.create "animateMotion"
    let animateTransform = Elem.create "animateTransform"
    let circle = Elem.create "circle"
    let clipPath = Elem.create "clipPath"
    let defs = Elem.create "defs"
    let desc = Elem.create "desc"
    let discard = Elem.create "discard"
    let ellipse = Elem.create "ellipse"
    let feBlend = Elem.create "feBlend"
    let feColorMatrix = Elem.create "feColorMatrix"
    let feComponentTransfer = Elem.create "feComponentTransfer"
    let feComposite = Elem.create "feComposite"
    let feConvolveMatrix = Elem.create "feConvolveMatrix"
    let feDiffuseLighting = Elem.create "feDiffuseLighting"
    let feDisplacementMap = Elem.create "feDisplacementMap"
    let feDistantLight = Elem.create "feDistantLight"
    let feDropShadow = Elem.create "feDropShadow"
    let feFlood = Elem.create "feFlood"
    let feFuncA = Elem.create "feFuncA"
    let feFuncB = Elem.create "feFuncB"
    let feFuncG = Elem.create "feFuncG"
    let feFuncR = Elem.create "feFuncR"
    let feGaussianBlur = Elem.create "feGaussianBlur"
    let feImage = Elem.create "feImage"
    let feMerge = Elem.create "feMerge"
    let feMergeNode = Elem.create "feMergeNode"
    let feMorphology = Elem.create "feMorphology"
    let feOffset = Elem.create "feOffset"
    let fePointLight = Elem.create "fePointLight"
    let feSpecularLighting = Elem.create "feSpecularLighting"
    let feSpotLight = Elem.create "feSpotLight"
    let feTile = Elem.create "feTile"
    let feTurbulence = Elem.create "feTurbulence"
    let filter = Elem.create "filter"
    let foreignObject = Elem.create "foreignObject"
    let g = Elem.create "g"
    let hatch = Elem.create "hatch"
    let hatchpath = Elem.create "hatchpath"
    let image = Elem.create "image"
    let line = Elem.create "line"
    let linearGradient = Elem.create "linearGradient"
    let marker = Elem.create "marker"
    let mask = Elem.create "mask"
    let metadata = Elem.create "metadata"
    let mpath = Elem.create "mpath"
    let path = Elem.create "path"
    let pattern = Elem.create "pattern"
    let polygon = Elem.create "polygon"
    let polyline = Elem.create "polyline"
    let radialGradient = Elem.create "radialGradient"
    let rect = Elem.create "rect"
    let script = Elem.create "script"
    let set = Elem.create "set"
    let stop = Elem.create "stop"
    let style = Elem.create "style"
    let svg = Elem.create "svg"
    let switch = Elem.create "switch"
    let symbol = Elem.create "symbol"
    let text = Elem.create "text"
    let textPath = Elem.create "textPath"
    let title = Elem.create "title"
    let tspan = Elem.create "tspan"
    let use' = Elem.create "use"
    let view = Elem.create "view"

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
    let svg (x : int, y : int, w : int, h : int) (content : XmlNode list) =
        Elem.svg [            
            Attr.create "viewBox" (sprintf "%i %i %i %i" x y w h)
            Attr.create "xmlns" "http://www.w3.org/2000/svg"
        ] content