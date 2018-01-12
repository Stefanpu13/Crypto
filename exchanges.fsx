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

#load "utils.fsx"
open Utils

//#region https://coinmarketcap.com top exchanges by volume
#load "coinmarketgap.fsx"
open Coinmarketgap
open FSharp.Data

List.iter (printfn "%A") top20ExchangesWithVolume

//#endregion


(*
    For each of the top 20 exchanges get the list of the listed curerncies 
    and their daily volume
*)

type Code = string

type Coin = {    
    code:Code  
}

type Pair = {
    // the first currency in the pair is called base
    baseCurrency: Code
    quoteCurrency: Code
    volume: int64
    // volumePer: int
}


let coinsPerExchanges = 
    top20Exchanges
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
                        | ValidUSDPrice p -> p 
                        |_ -> 0L
                    )

                {
                    baseCurrency= baseCurr
                    quoteCurrency= quoteCurr
                    volume=pairVolume
                }                                 
            ) 
            |> Set.ofSeq          
        
        exchangeName, listedCoinsRows    
    )
    |> List.ofSeq

// List all top 20 exchanges where given coin is traded(is base currency)

let exchangesOfCoin (coinCode: string) = 
    coinsPerExchanges
    |> Seq.filter( fun coinsOnExchange ->
        coinsOnExchange
        |> snd
        |> Set.exists(fun c -> c.baseCurrency.ToLower() = coinCode.ToLower())
    )


exchangesOfCoin "XRP"
|> Seq.map fst
|> List.ofSeq
