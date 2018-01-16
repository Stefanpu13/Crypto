
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

// for each of top20 exchanges:
// get all pairs prices, volume
 (*
     - pair code
     - DateTime in UTC
     - price
     - volume
 *)

#r @"packages\Fsharp.Data.dll" 
open FSharp.Data

#load "exchanges.fsx"
open Exchanges.Exchanges

top20Exchanges

top20ExchangesWithVolume

coinsPerExchanges


