module Parser


open System
open FSharp.Data
open DataStructures
open System.Text.RegularExpressions


let extractSecondLevelDomain (url: string) : string option =
    let pattern = @"^https?://(?:[^/]+\.)*([^/.]+\.[^/.]+)"
    let matchResult = Regex.Match(url, pattern)
    if matchResult.Success then
        Some matchResult.Groups.[1].Value
    else
        None

let prioritizeDifferentDomains (links: string list) : string list =
    let distinctDomains =
        links
        |> List.choose extractSecondLevelDomain
        |> List.distinct

    let prioritizeLink (link: string) =
        let secondLevelDomain = extractSecondLevelDomain link
        match secondLevelDomain with
        | Some domain ->
            if List.exists (fun d -> d <> domain) distinctDomains then
                1
            else
                2
        | None -> 2
    links |> List.sortBy prioritizeLink
    
let isAbsoluteUrl (url: string) : bool =
    Uri.IsWellFormedUriString(url, UriKind.Absolute)

let createURIfromRelative(baseurl:string, url:string) : string option =
    try
        if not <| isAbsoluteUrl baseurl then
           let absolute = Uri(Uri(baseurl), url).ToString()
           Some absolute
        else
            Some baseurl
    with
        | _ -> None

let linksof(data:HtmlDocument, baseurl:string) : string list =
    let result = 
        data.Descendants [ "a" ]
        |> Seq.choose (fun x ->
            x.TryGetAttribute("href")
            |> Option.map (fun a -> a.Value()))
        |> Seq.toList
        |> List.filter isAbsoluteUrl
    result


let searchJSReferences (data: HtmlDocument, baseurl:string): string array =
    let javascriptLinks =
        data.Descendants "script"
        |> Seq.choose (fun x ->
            x.TryGetAttribute("src")
            |> Option.map(fun b -> b.Value()))
        |> Seq.toArray

    let (absolute, relative) = javascriptLinks |> Array.partition(fun x -> isAbsoluteUrl x)
    relative
    |> Array.choose (fun x -> 
        createURIfromRelative(baseurl, x)
        |> Option.map (fun x -> x))
    |> Array.append absolute

let searchCSSReferences (data : HtmlDocument, baseurl:string) : string array =
    let cssReferences =
        data.Descendants "link"
        |> Seq.filter (fun x -> x.HasAttribute("rel", "stylesheet"))
        |> Seq.map (fun x -> x.AttributeValue("href"))
        |> Array.ofSeq
    let (absolute, relative) = cssReferences |> Array.partition isAbsoluteUrl
    relative
    |> Array.choose(fun x -> 
        createURIfromRelative(baseurl,x))
    |> Array.append absolute



let extractImageInfo (data:HtmlDocument, baseurl:string) : ImageInfo list =
    data.Descendants ["img"]
    |> Seq.choose (fun node ->
        let description = node.AttributeValue("alt")
        let url = node.AttributeValue("src")
        
        if not (String.IsNullOrWhiteSpace(url)) then
            match isAbsoluteUrl url with
            | true -> Some {Link=url; Alt=description}
            | false ->
                match createURIfromRelative(baseurl, url) with
                | Some x -> Some {Link=x; Alt=description}
                | _ -> None
        else
            None
        )
    |> Seq.toList

let extractMetaTags (html: HtmlDocument) =
    
    let getTitle () =
        try
            html.Descendants "title" |> Seq.map (fun x -> x.InnerText()) |> Seq.head
        with
            | _ -> ""
    let getDescription () =
        try
            html.Descendants "meta"
            |> Seq.filter (fun x -> x.HasAttribute("name", "description"))
            |> Seq.map (fun x -> x.Attribute("content").ToString())
            |> Seq.head
        with
            | _ -> ""
    
    let getCanonicalLink () =
        try
            html.Descendants "link"
            |> Seq.filter (fun x -> x.HasAttribute("rel","canonical"))
            |> Seq.map (fun x -> x.AttributeValue("href"))
            |> Seq.head
        with
            | _ -> ""
    
    let title = getTitle ()
    let description = getDescription()
    let canonicalLink = getCanonicalLink ()
    [|title; description.ToString(); canonicalLink|]

let filterFileEndings (input: string) : bool =
    match input.EndsWith(".pdf") || input.EndsWith(".png") || input.EndsWith(".webp") ||
          input.EndsWith(".jpg") || input.EndsWith(".jpeg") ||
          input.EndsWith(".doc") || input.EndsWith(".docx") ||
          input.EndsWith(".xls") || input.EndsWith(".xlsx") ||
          input.EndsWith(".ppt") || input.EndsWith(".pptx") ||
          input.EndsWith(".zip") || input.EndsWith(".rar") ||
          input.EndsWith(".exe") || input.EndsWith(".dll") ||
          input.EndsWith(".mp3") || input.EndsWith(".wav") ||
          input.EndsWith(".mp4") || input.EndsWith(".avi") ||
          input.EndsWith(".css") || input.EndsWith(".js") ||
          input.EndsWith(".xml") || input.EndsWith(".json") ||
          input.EndsWith(".svg") || input.EndsWith(".ico") ||
          input.EndsWith(".woff") || input.EndsWith(".woff2") with
    | true -> false
    | false -> true



let extractEmails (data:string) =
    let emailRegex = Regex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}\b", RegexOptions.IgnoreCase)
    data
    |> emailRegex.Matches
    |> Seq.cast<Match>
    |> Seq.map (fun m -> m.Value)
    |> Seq.filter filterFileEndings

let getprioritylinksof(links:string list, baseurl:string) : string list =
    let (absolutes, relatives) = links |> List.partition isAbsoluteUrl
    relatives
    |> List.choose (fun x -> 
        createURIfromRelative(baseurl, x)
        |> Option.map (fun x -> x))
    |> List.append absolutes
    |> prioritizeDifferentDomains



