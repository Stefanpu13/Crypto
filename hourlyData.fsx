(*
    Collect hourly data from coinamrket gap and see if it can be used 
    for as historical data
    Collect:
        * pairs price per excahnge
        * pairs volume per exchange

    Reqs:
        * Data must be collected even if my computer is not working
        * Data must be available on my computer on-demand or on-start
            ** IF on-demand, how should the demand be made?
        * Data must be avaialable somewhere else

    Variant 1:

    Use Azure functions to schedule hourly/daily collection of data and 
    save it to Azure SQL server instance

    - Start with smallest sql db - basic
    - choose sql access technology that works on core!
    (hint: type providers might not be working)
    - regularly get data from azure sql and write it to local sql
    - think how to notify myself for errors in data parsing
    (the sites html will probably change, and then I will have to update the code)
    - in case parsing is temporalily unavailable 
    and any other case that breaks schedule, find a way to gather missing data

*)


// while true do    
//     System.Threading.Thread.Sleep 2000
//     printfn "%A" System.DateTime.Now

// for each of top5 exchanges:
// get all pairs prices, volume for first BTC pair
 (*
     - pair code
     - DateTime in UTC
     - price
     - volume
 *)

#r @"bin/FSharp.Data.SqlClient.dll" 
open System
open FSharp.Data

#load "parsers.fsx"
#load "DBReader.fsx"
open Parsers
open DBReader


// group records by exchange
let records = DB.readFromAzureDb ()      

// get results for specific exchange
let dataForExchange (exchangeName: string) =     
    let pairsByExchange = 
        records
        |> Seq.groupBy (fun record -> record.Exchange.ToLower())

    let foundExchange = 
        pairsByExchange 
        |> Seq.tryFind (fun (en, _) -> 
            en.Trim().ToLower() = exchangeName.ToLower()
        ) 

    match foundExchange with
    | Some data -> data
    | None -> failwith "exchange not found"    
    

// get unique prices 
let uniqPricesForBinance = 
    dataForExchange "upbit" 
    |> snd 
    |> Seq.distinctBy(fun r -> r.PriceUSD)


// average time for update for binance
uniqPricesForBinance 
|> Seq.map(fun r -> r.Date)
|> Seq.pairwise
|> Seq.map(fun (f, s) -> 
    s.Subtract(f).Minutes
)
|> List.ofSeq
|> Seq.averageBy float


// #time
let allCoins = CoinMarketCap.coinsPerExchanges (CoinMarketCap.getTop25Exchanges())

let allPairsInTop20Exchanges = 
    allCoins |> Seq.map snd |> Seq.collect id |> Seq.length

// how much daily data will I have to write if I collect info
// for all 20 exchanges` pairs every 10 mins

let recordsPerHour = 60/20 
let hours = 24

allPairsInTop20Exchanges * recordsPerHour * hours

// how much daily data will I have to write if I collect info
// for all 20 exchanges` pairs, that are high profile,  every 10 mins

let highVolumeCoinsCodes = 
    IsThisCoinAScam.coinsWithDailyVolumeInInterval 5_000_000.M 1_000_000_000_000.M
    |> Seq.map (fun (_,CoinCode code,_) -> code.ToLower())
    |> Set.ofSeq

let highProfilePairsLength = 
    allCoins
    |> Seq.map snd 
    |> Seq.collect id
    |> Seq.map(fun p -> p.baseCurrency.ToLower())
    |> Seq.filter(fun code -> 
        highVolumeCoinsCodes |> Set.contains code
    )
    |> Seq.length
