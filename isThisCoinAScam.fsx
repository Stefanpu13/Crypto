
(*
    get list of all altcoins that have daily volume more than 10 million usd
    Of these, get data from https://isthiscoinascam.com
        - possibility of coin being a scam: 'The profile is 100% complete' column
        
*)

#load "utils.fsx"
open Utils

#r @"packages\Fsharp.Data.dll"


open FSharp.Data

//#time


let allCoinsPage = HtmlDocument.Load("https://coinmarketcap.com/all/views/all/")

let coinRows = 
    allCoinsPage.CssSelect("#currencies-all > tbody > tr")

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
            |_ -> 0.M
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

let coinsWithDailyVolumeInInterval low high = 
    Seq.filter (fun (_,_,v) -> low <= v && v <= high ) coinsBasicInfo


let profileCoins profile coins (_,code:string,_) = 
    Seq.exists(fun (c:string, pr ) -> 
        c.ToLower() = code.ToLower() && pr >= profile
    ) coins

let highestProfileCoins = profileCoins 90
let highProfileCoins = profileCoins 80

let smallCapAltcoinsHighestProfile =
    coinsWithDailyVolumeInInterval 5_000_000.0M 90_000_000.0M
    |> Seq.filter (highestProfileCoins coinsCodeAndProfile)    
    |> Seq.sortByDescending (fun (_,_, v) -> v)
    |> List.ofSeq

smallCapAltcoinsHighestProfile |> Seq.length   

let majorCoinsHighProfile =
    coinsWithDailyVolumeInInterval 90_000_000.M 1000000000000.M
    |> Seq.filter (highProfileCoins coinsCodeAndProfile)    
    |> Seq.sortByDescending (fun (_,_, v) -> v)
    |> List.ofSeq

let coinsHighProfile = 
    coinsWithDailyVolumeInInterval 20_000_000.M 1_000_000_000_000.M

