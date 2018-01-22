#r @"bin/Fsharp.Data.dll"

#r @"bin/FSharp.Data.SqlClient.dll" 
open FSharp.Data
open System

#load "parsers.fsx"
#load "DBWriter.fsx"
open Parsers
open DBWriter

let startWritingToDb writingFn = 
    while true do        
        let coinsInExchanges = 
            CoinMarketCap.coinsPerExchanges (CoinMarketCap.getTop25Exchanges())            

        printfn "%A" (Seq.length coinsInExchanges)

        printfn "Start writing to db"
        writingFn coinsInExchanges
        printfn "Finish writing to db. Rows added:"
        System.Threading.Thread.Sleep 30000

startWritingToDb DB.writeToAzureDb

// startWritingToDb DB.writeToLocalDb
