namespace Falco.Markup

open System

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
