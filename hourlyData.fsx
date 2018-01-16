
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

#r @"packages\Fsharp.Data.dll" 
open FSharp.Data.Runtime.StructuralInference
#r @"packages\FSharp.Data.SqlClient.dll" 
open System

open FSharp.Data

#load "exchanges.fsx"
open Exchanges.Exchanges


[<Literal>]
let connectionString = 
    @"Data Source=.;Initial Catalog=Crypto;Integrated Security=True"

let isBTC_USDT pair = 
    pair.baseCurrency.ToLower() = "btc" &&
    pair.quoteCurrency.ToLower() = "usdt"

let startWritingToDb ()= 
    while true do
        System.Threading.Thread.Sleep 20000
        let coinsInExchanges = coinsPerExchanges (top20Exchanges |> Seq.take 3)

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

            // printfn "------------------------------------------"
        )

let readFromDb () = 
    // do
    use cmd = new SqlCommandProvider<"
    Select Date, Price, Exchange, Code 
    From PairPrice
    ", connectionString>(connectionString)

    cmd.Execute()


// group records by exchange
let records = readFromDb ()  

let pairsByExchange = 
    records
    |> Seq.groupBy (fun record -> record.Exchange.ToLower())
    
// get results for specific exchange
let dataForExchange (exchangeName: string) =     
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


// average time for update
uniqPricesForBinance 
|> List.ofSeq
|> List.map(fun r -> r.Date)
|> List.pairwise
|> List.map(fun (f, s) -> 
    s.Subtract(f).Minutes
)
