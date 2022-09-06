namespace Falco.Markup

open System
open System.Collections.Generic

module Attr =
    /// XmlAttribute KeyValueAttr constructor
    let create (key : string) (value : string) =
        KeyValueAttr (key, value)

    /// XmlAttribute NonValueAttr constructor
    let createBool (key : string) =
        NonValueAttr key

    /// Merge two XmlAttribute lists
    let merge (attrs1 : XmlAttribute list) (attrs2 : XmlAttribute list) =
        let joinValues v2 v1 = String.Concat([| v1; " "; v2 |])

        // convert left list into dictionary
        let merged = Dictionary<string, string option>()

        for attr in attrs1 do
            match attr with
            | NonValueAttr name          -> merged.Add(name, None)
            | KeyValueAttr (name, value) -> merged.Add(name, Some value)

        // check right list against dictionary, updating as appropriate
        for attr in attrs2 do
            match attr with
            | NonValueAttr name ->
                if not (merged.ContainsKey name) then
                    merged.Add(name, None)

            | KeyValueAttr (name, value) ->
                if merged.ContainsKey(name) then
                    let newValue = Option.map (joinValues value) merged[name]
                    merged[name] <- newValue
                else
                    merged.Add(name, Some value)

        // inputs are now merged, convert dictionary back into of XmlAttribute
        [
            for Operators.KeyValue (name, values) in merged do
                match values with
                | None ->
                    NonValueAttr name
                | Some value ->
                    KeyValueAttr (name, value)
        ]

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
    let dataAttr name = create (String.Concat [ "data-"; name ])
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
