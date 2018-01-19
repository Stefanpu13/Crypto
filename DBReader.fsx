#r @"packages\FSharp.Data.SqlClient.dll" 
open FSharp.Data

#load "parsers.fsx"
open Parsers

CoinMarketCap.init()
IsThisCoinAScam.init()

module DB = 
    
    [<Literal>]
    let ConnectionString = 
        @"Data Source=.;Initial Catalog=Crypto;Integrated Security=True"

    let readFromDb () =     
        use cmd = new SqlCommandProvider<"
        Select Date, PriceUSD, Exchange, Code 
        From PairPrice
        ", ConnectionString>(ConnectionString)

        cmd.Execute()


// group records by exchange
let records = DB.readFromDb ()      

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

// let dailyRecordsCount = highProfilePairsLength * recordsPerHour * hours

// // #time
// CoinMarketCap.init()
// IsThisCoinAScam.init()

// CoinMarketCap.getTopExchangesWithVolume ()

// CoinMarketCap.coinsInExchange "binance"

// CoinMarketCap.coinsPerExchanges (CoinMarketCap.getTop25Exchanges())
// |> Seq.length
    

// let smallCapAltcoinsHighestProfile =
//     IsThisCoinAScam.coinsWithDailyVolumeInInterval  5_000_000.0M 1_000_000_000_000.0M
//     |> Seq.filter (IsThisCoinAScam.profileCoins 80 IsThisCoinAScam.coinsCodeAndProfile)    
//     |> Seq.sortByDescending (fun (_,_, v) -> v)
//     |> List.ofSeq