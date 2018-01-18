#r @"packages\Fsharp.Data.dll"
#r @"packages\FSharp.Data.SqlClient.dll" 
open FSharp.Data
open System

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
}

type CoinName = CoinName of string

type CoinCode = CoinCode of string
type CoinVolume = CoinVolume of decimal
// type
type BasicInfo = CoinName * CoinCode * CoinVolume

type CodeAndProfile = CodeAndProfile of string * int

module Utils = 
    let (|ValidUSDPrice|_|) (price: string) = 
       let mutable v = 0.M
       let priceStr = price.Trim().Substring(1).Replace(",", "")
       if Decimal.TryParse(priceStr, &v) then Some(v)
       else None   


    let (|ValidPrice|ExcludedPrice|InvalidPrice|) (price: string) = 
        let mutable v = 0.M 
         
         
        let priceStr = price.Trim().Substring(1).Replace(",", "")
        if priceStr.Contains("*") then
            let excludedPriceStr = priceStr.Replace("*", "")

            if Decimal.TryParse(excludedPriceStr, &v) 
            then ExcludedPrice v
            else InvalidPrice   
        else
            if Decimal.TryParse(priceStr, &v) 
            then ValidPrice v
            else InvalidPrice   
open Utils

module CoinMarketCap = 
    
    let exchangesUri = "https://coinmarketcap.com/exchanges/volume/24-hour/all/"
    let mutable exchangesPage = HtmlDocument.New(Seq.empty)
    let mutable exchangesInfoRows = List.Empty
    let mutable alreadyInitialized = false
    let count = 25   

    let reInit(uri: string) =         
        exchangesPage <- HtmlDocument.Load(uri)
        exchangesInfoRows <- exchangesPage.CssSelect(".table.table-condensed > tr")

    let getTop25Exchanges () =             
        exchangesInfoRows
        |> Seq.map(fun el -> el, el.AttributeValue("id"))
        |> Seq.filter(fun (_, attr) -> not <| String.IsNullOrEmpty(attr))
        |> Seq.map snd
        |> Seq.take count            

    let getTopExchangesWithVolume () = 
        let topExchanges = 
            getTop25Exchanges ()
            |> List.ofSeq

        let topExchangesVolume = 
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
            |> Seq.take count
            |> List.ofSeq

        List.zip topExchanges topExchangesVolume

    let coinsInExchange exchangeName = 
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
                        | Utils.ValidPrice p -> {price=p;excluded=false} 
                        | Utils.ExcludedPrice p  -> {price=p;excluded=true} 
                        | Utils.InvalidPrice ->  {price=0.M;excluded=false}
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

    let coinsPerExchanges exchanges = 
        exchanges
        |> Seq.map coinsInExchange
        |> List.ofSeq

    // List all top 20 exchanges where given coin is traded(is base currency)

    let exchangesOfCoin (coinCode: string) =             
        getTop25Exchanges ()            
        |> coinsPerExchanges
        |> Seq.filter( fun coinsOnExchange ->
            coinsOnExchange
            |> snd
            |> Set.exists(fun c -> c.baseCurrency.ToLower() = coinCode.ToLower())
        )

    let init() = 
        if not alreadyInitialized then
            exchangesPage <- HtmlDocument.Load(exchangesUri)
            exchangesInfoRows <- exchangesPage.CssSelect(".table.table-condensed > tr")

        alreadyInitialized <- true         
open CoinMarketCap

module IsThisCoinAScam =
    let allCoinsPage = HtmlDocument.Load("https://coinmarketcap.com/all/views/all/")

    let coinRows = allCoinsPage.CssSelect("#currencies-all > tbody > tr")

    // page displays a table but before javascript is executed all coins are
    // displayed in the html
    let allCoinsProfilesPage = HtmlDocument.Load("https://isthiscoinascam.com/#")

    let allCoinsProfiles = allCoinsProfilesPage.CssSelect("#example > tbody > tr")

    let mutable coinsBasicInfo = Seq.empty

    let getInnerText (parent: HtmlNode) selector = 
        match Seq.tryHead (parent.CssSelect(selector)) with
        |Some  el ->  el.InnerText() 
        |None -> "Element does not exist"


    let coinsCodeAndProfile = 
        allCoinsProfiles |> Seq.map (fun row ->
            let code = row.Elements() |> Seq.item 1 |> (fun el -> el.InnerText())
            let profileValue = 
                row.Elements() 
                |> Seq.last 
                |> (fun el -> getInnerText el ".progress-bar span")
                |> int

            CodeAndProfile (code, profileValue)        
        )    

    let getCoinsBasicInfo () = 
        coinRows 
        |> Seq.map(fun coinRow -> 
            let coinName = getInnerText coinRow ".currency-name-container"        
            let coinSymbol = getInnerText coinRow ".col-symbol"        
            let coinVolume = getInnerText coinRow ".volume"
                
            (
                CoinName coinName, 
                CoinCode coinSymbol, 
                CoinVolume (
                    match coinVolume with
                    | ValidUSDPrice p -> p 
                    |_ -> 0.M
                )
            )    
        )

    let coinsWithDailyVolumeInInterval low high = 
        Seq.filter (fun (_,_,CoinVolume v) -> 
            low <= v && v <= high 
        ) coinsBasicInfo

    let profileCoins profile coins ((_,CoinCode code,_)) =
        Seq.exists(fun (CodeAndProfile(c:string, pr )) -> 
            c.ToLower() = code.ToLower() && pr >= profile
        ) coins
    
    let init () = 
        coinsBasicInfo <- getCoinsBasicInfo () 
open IsThisCoinAScam    

module DB = 
    
    [<Literal>]
    let connectionString = 
        @"Data Source=.;Initial Catalog=Crypto;Integrated Security=True"

    let isBTC_USDT pair = 
        pair.baseCurrency.ToLower() = "btc" &&
        pair.quoteCurrency.ToLower() = "usdt"

    let writeToDb coinsInExchanges = 
        coinsInExchanges 
        |> Seq.map(fun (exchangeName, pairs) ->                
            match Seq.tryFind isBTC_USDT pairs with
            | Some p ->  (exchangeName, Some p)
            | None -> exchangeName, None        
        )
        |> Seq.iter (fun exchange ->        
            match exchange with
            |  (n, Some p) -> 
                use cmd = new SqlCommandProvider<"
                INSERT INTO PairPrice (Date, Price, Exchange, Code)     
                VALUES(@date, @price, @exchange, @code)
                ", connectionString>(connectionString)

                cmd.Execute(
                    date=DateTime.Now,
                    price=p.pairPrice.price,
                    exchange=n,
                    code=p.baseCurrency.ToLower() + "/" + p.quoteCurrency.ToLower()
                    ) |> ignore
                
            | (n, None) ->  printfn "Exchange %A does not have btc/usdt pair" n                   
        )

    let readFromDb () =     
        use cmd = new SqlCommandProvider<"
        Select Date, Price, Exchange, Code 
        From PairPrice
        ", connectionString>(connectionString)

        cmd.Execute()

open DB



let startWritingToDb () = 
    while true do
        System.Threading.Thread.Sleep 60000
        let coinsInExchanges = 
            CoinMarketCap.coinsPerExchanges (CoinMarketCap.getTop25Exchanges())

        DB.writeToDb coinsInExchanges

// startWritingToDb ()


CoinMarketCap.init()
IsThisCoinAScam.init()
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
    dataForExchange "binance" 
    |> snd 
    |> Seq.distinctBy(fun r -> r.Price)


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

let dailyRecordsCount = highProfilePairsLength * recordsPerHour * hours


// #load "parsers.fsx"
// open Parsers


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

    