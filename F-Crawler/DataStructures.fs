module DataStructures

open System
open FSharp.Data


type ImageInfo = {
    Link: string
    Alt: string
}


type Document = {
    ResponseTime:int64
    Emails: string array
    Imagedata: ImageInfo list
    CSSlinks: string array
    JSlinks: string array
    CrawlDate: DateTime
    URL: string
    Links: string list
    HTMLstring : string
    Metatags : string array
}

let shuffleList<'a> (list: 'a list) : 'a list =
    let rng = Random()
    let mutable array = list |> List.toArray
    let rec shuffleArray (array: 'a[]) (index: int) =
        if index <= 0 then
            array
        else
            let randomIndex = rng.Next(index + 1)
            let temp = array.[index]
            array.[index] <- array.[randomIndex]
            array.[randomIndex] <- temp
            shuffleArray array (index - 1)

    shuffleArray array (Array.length array - 1) |> Array.toList
    

