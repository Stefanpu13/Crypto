#r @"bin/FSharp.Data.SqlClient.dll" 
open System.Collections.Generic
open FSharp.Data

#load "parsers.fsx"
open Parsers

module DB = 
    
    [<Literal>]
    let ConnectionString = 
        @"Data Source=.;
        Initial Catalog=Crypto;
        Integrated Security=True
        "

    [<Literal>]    
    let AzureConnectionString = @"
    Server=tcp:cryptospu.database.windows.net,1433;
    Initial Catalog=Crypto;Persist Security Info=False;
    User ID=crypto_db;Password=Stefan@2;
    MultipleActiveResultSets=False;
    Encrypt=True;
    TrustServerCertificate=False;
    Connection Timeout=30;
    "

    let readFromDb () =     
        use cmd = new SqlCommandProvider<"
        Select Date, PriceUSD, Exchange, Code 
        From Pairs
        ", ConnectionString>(ConnectionString)

        cmd.Execute()


    let readFromAzureDb () =    
        use cmd = new SqlCommandProvider<"
        Select Date, PriceUSD, Exchange, Code, Volume 
        From Pairs
        ", AzureConnectionString>(AzureConnectionString)

        cmd.Execute()    


let records = 
    DB.readFromAzureDb () 
    |> Seq.toList

let binanceRecords = 
    records |> List.filter(fun p -> p.Exchange.ToLower() ="binance")
let ltcbtc = 
    binanceRecords
    |> List.filter (fun p -> p.Code.ToLower() = "ltc/btc")

ltcbtc
|> List.sortBy(fun p -> p.Date)
|> List.map (fun p -> p.Date.ToLongTimeString(), p.Volume)
|> List.take 50
