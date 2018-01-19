#r @"packages\Fsharp.Data.dll"
#r @"packages\FSharp.Data.SqlClient.dll" 
open FSharp.Data
open System

#load "parsers.fsx"
#load "DBWriter.fsx"
open Parsers
open DBWriter

CoinMarketCap.init()
IsThisCoinAScam.init()

let startWritingToDb () = 
    while true do        
        let coinsInExchanges = 
            CoinMarketCap.coinsPerExchanges (CoinMarketCap.getTop25Exchanges())            

        printfn "%A" (Seq.length coinsInExchanges)

        printfn "Start writing to db"
        let rowsAdded = DB.writeToDb coinsInExchanges
        printfn "Finish writing to db. Rows added: %A" rowsAdded

        System.Threading.Thread.Sleep 30000

startWritingToDb ()
