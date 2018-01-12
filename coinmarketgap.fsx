
(*
    Get the names and volum eof the top 20 exchanges by volume
*)



// Type providers
#r @"packages\Fsharp.Data.dll"
open System
open FSharp.Data
let exchangesPage = HtmlDocument.Load("https://coinmarketcap.com/exchanges/volume/24-hour/all/")

let exchangesInfoRows = exchangesPage.CssSelect(".table.table-condensed > tr")

let top20Exchanges = 
    exchangesInfoRows
    |> Seq.map(fun el -> el, el.AttributeValue("id"))
    |> Seq.filter(fun (_, attr) -> not <| String.IsNullOrEmpty(attr))
    |> Seq.map snd
    |> Seq.take 20
    |> List.ofSeq

let top20exchangesVolume = 
    exchangesInfoRows
    |> Seq.filter(fun row -> 
        let ems = row.CssSelect("td > em")
        if ems.IsEmpty then false
        else ems.Head.InnerText() = "Total"
    )
    |> Seq.map( fun volumeRow -> 
        volumeRow.Elements()
        |> Seq.item 1
        |> (fun td -> td.InnerText())
    )
    |> Seq.take 20
    |> List.ofSeq

let top20ExchangesWithVolume = List.zip top20Exchanges top20exchangesVolume

// get volume for each exchange