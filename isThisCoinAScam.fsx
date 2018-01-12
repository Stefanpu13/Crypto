
(*
    get list of all altcoins that have daily volume more than 10 million usd
    Of these, get data from https://isthiscoinascam.com
        - possibility of coin being a scam: 'The profile is 100% complete' column
        
*)

#load "utils.fsx"
open Utils

#r @"packages\Fsharp.Data.dll"

open FSharp.Data.Runtime.BaseTypes
open System
open FSharp.Data

//#time


let allCoinsPage = HtmlDocument.Load("https://coinmarketcap.com/all/views/all/")

let coinRows = 
    allCoinsPage.CssSelect("#currencies-all > tbody > tr")

// let (|ValidUSDPrice|_|) (price: string) = 
//    let mutable v = 0L
//    let priceStr = price.Substring(1).Replace(",", "")
//    if Int64.TryParse(priceStr, &v) then Some(v)
//    else None   

let getInnerText (parent: HtmlNode) selector = 
    match Seq.tryHead (parent.CssSelect(selector)) with
    |Some  el ->  el.InnerText() 
    |None -> "Element does not exist"


let coinsBasicInfo = 
    coinRows 
    |> Seq.map(fun coinRow -> 
        let coinName = getInnerText coinRow ".currency-name-container"        
        let coinSymbol = getInnerText coinRow ".col-symbol"        
        let coinVolume = getInnerText coinRow ".volume"
            
        (
            coinName, 
            coinSymbol, 
            match coinVolume with
            | ValidUSDPrice p -> p 
            |_ -> 0L
        )    
    )

let allCoinsCheck = HtmlDocument.Load("https://isthiscoinascam.com/#")

let rows = allCoinsCheck.CssSelect("#example > tbody > tr")

let coinsCodeAndProfile = 
    rows |> Seq.map (fun row ->
        let code = row.Elements() |> Seq.item 1 |> (fun el -> el.InnerText())
        let profileValue = 
            row.Elements() 
            |> Seq.last 
            |> (fun el -> getInnerText el ".progress-bar span")
            |> int

        (code, profileValue)        
    )

let coinsWithCapInInterval low high = 
    Seq.filter (fun (_,_,v) -> low <= v && v <= high ) coinsBasicInfo


let coinsInIntervalWithHighProfile =
    coinsWithCapInInterval 5_000_000L 900_000_000L
    |> Seq.filter(fun (_, code, _) -> 
        Seq.exists(fun (c:string, pr ) -> 
            c.ToLower() = code.ToLower() && pr >=90
        ) coinsCodeAndProfile
    )
    |> Seq.sortByDescending (fun (_,_, v) -> v)
    |> List.ofSeq

coinsInIntervalWithHighProfile |> Seq.length   

