
#load "parsers.fsx"
open Parsers
open CoinMarketCap
open IsThisCoinAScam


// Take list of high profile coins with daily volume more than 5 000 000
let highVolumeCoins = 
    IsThisCoinAScam.coinsWithDailyVolumeInInterval 5000000.M 1000000000000.M

let highVolumeCoinsCodes = 
    highVolumeCoins 
    |> Seq.map (fun (_, CoinCode c, _) -> c.ToLower())
    |> Set.ofSeq

let highProfileHighVolumeCoins = 
    coinsCodeAndProfile
    |> Seq.filter(fun (CodeAndProfile (c, p)) -> 
        p >= 80 && Set.contains (c.ToLower()) highVolumeCoinsCodes
    )

highProfileHighVolumeCoins |> List.ofSeq |> Seq.length

let allCoinsInTop25Exchanges =
    CoinMarketCap.coinsPerExchanges (CoinMarketCap.getTop25Exchanges())

let uniqueCoinPairs  = 
    allCoinsInTop25Exchanges
    |> Seq.map snd
    |> Seq.collect id
    |> Seq.distinctBy (fun p -> (p.baseCurrency, p.quoteCurrency))

// unique pairs in top 25 echanges are less than all recorded pairs
// for example btc/usdt is traded in more than one exchange
uniqueCoinPairs |> Seq.length

let dailyRecordsCount = 
    allCoinsInTop25Exchanges
    |> Seq.map snd
    |> Seq.collect id
    |> Seq.length

// get unique coinPairs where the base qurrency is high volume and high profile

let uniqueHighVolumeAndProfilePairs = 
    uniqueCoinPairs 
    |> Seq.filter (fun p -> 
        Set.contains (p.baseCurrency.ToLower()) highVolumeCoinsCodes 
)


uniqueCoinPairs 
|> Seq.filter (fun p -> 
    Set.contains (p.baseCurrency.ToLower()) highVolumeCoinsCodes 
)
|> Seq.map (fun p -> p.baseCurrency, p.quoteCurrency)
|> List.ofSeq
|> Seq.length