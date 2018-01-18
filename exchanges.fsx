(*
    What data do i need to save for each exchange?
        - name
        - country of operation(or registration)
        - traded currencies (as pairs)
        - hourly/daily volume for each pair
        - hourly/daily exchange rates for each pair
    
    What other data is valuable?
        - added/removed currencies pairs
            * Adding a currency, especially on large and reputable exchange,
            is positive sign for the currency(although it will probably intially drop
            as early investors exit)
            * Removing currency is bad sign and will cause sell-off
        - differences in prices accross exchanges - market is decentralized and fragmented.
        That means that in the coming months/years, different things will affect individual 
        exchanges:
            * theft
            * regulations
            * bans
        The info will not be available immediately in the form of news. However,
        Informed people will do actions that will cause price distortions. For example,
            * Upcoming negative events will cause increased sells at particular
            echange. As result the prices of major coins will be lower than on other
            exchanges
                ** If prices are for a specific currency only, that might indicate
                problem with the currency (for exmaple, its about to be removed)
                ** IF prices are for several currencies, 
                than maybe the exchange has problems
*)
//#region https://coinmarketcap.com top exchanges by volume

#r @"packages\Fsharp.Data.dll"
open FSharp.Data

#load "utils.fsx"
open Utils

open System

module Exchanges = 
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

            if ems.IsEmpty 
            then false
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

    //#endregion


    (*
        For each of the top 20 exchanges get the list of the listed curerncies 
        and their daily volume
    *)

    type Code = string

    type Coin = {    
        code:Code  
    }

    type Price = {
        price: decimal
        excluded: bool
    }

    type Volume = {
        volume : decimal
        excluded: bool
    }

    type Pair = {
        exchangeName: string
        // the first currency in the pair is called base
        baseCurrency: Code
        quoteCurrency: Code
        pairPrice: Price
        volume: Volume
        // priceExcluded: bool    
    }


    let coinsPerExchanges exchanges = 
        exchanges
        |> Seq.map(fun exchangeName ->
            let exchangePage = 
                HtmlDocument.Load("https://coinmarketcap.com/exchanges/" + exchangeName)
            let listedCoinsRows = 
                exchangePage.CssSelect(".table-responsive tr") 
                // skip the header row
                |> Seq.skip 1 
                |> Seq.map(fun row ->
                    let children = row.Elements()
                    let pair = 
                        children
                        |> Seq.item 2
                        |> (fun td -> td.InnerText())
                    let baseAndQuote = pair.Split([|'/'|])               
                    let baseCurr, quoteCurr = baseAndQuote.[0], baseAndQuote.[1]
                    let pairVolume = 
                        children 
                        |> Seq.item 3
                        |> (fun td -> 
                            td.Elements() 
                            |> Seq.head 
                            |> (fun span -> span.InnerText())
                        )
                        |> (fun volume -> 
                            match volume with
                            | ValidPrice p -> {volume =p;excluded=false} 
                            | ExcludedPrice p  -> {volume=p;excluded=true} 
                            | InvalidPrice ->  {volume=0.M;excluded=false}
                        )
                    let pairPriceUSD = 
                        children
                        |> Seq.item 4
                        |> (fun td -> 
                            td.Elements() 
                            |> Seq.head 
                            |> (fun span -> span.InnerText())
                        )
                        |> (fun pairPrice -> 
                            match pairPrice with
                            | ValidPrice p -> {price=p;excluded=false} 
                            | ExcludedPrice p  -> {price=p;excluded=true} 
                            | InvalidPrice ->  {price=0.M;excluded=false}
                        )                   

                    {
                        exchangeName=exchangeName
                        baseCurrency= baseCurr
                        quoteCurrency= quoteCurr
                        pairPrice=pairPriceUSD
                        volume=pairVolume
                    }                                 
                ) 
                |> Set.ofSeq          
            
            exchangeName, listedCoinsRows    
        )
        |> List.ofSeq

    // List all top 20 exchanges where given coin is traded(is base currency)

    let exchangesOfCoin (coinCode: string) = 
        coinsPerExchanges top20Exchanges
        |> Seq.filter( fun coinsOnExchange ->
            coinsOnExchange
            |> snd
            |> Set.exists(fun c -> c.baseCurrency.ToLower() = coinCode.ToLower())
        )

    let coinsInExchange (exchange: string) = 
        coinsPerExchanges top20Exchanges
        |> Seq.find(fun (exch, _) -> exchange = exch)
        |> snd


// Exchanges.coinsInExchange "bittrex" 
// |> Seq.map(fun curr -> curr.baseCurrency) 
// |> Set.ofSeq

// Exchanges.exchangesOfCoin "XMR"
// |> Seq.map fst
// |> List.ofSeq

// Exchanges.top20exchangesVolume
