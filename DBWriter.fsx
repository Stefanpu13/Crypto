#r @"packages\FSharp.Data.SqlClient.dll" 
open System
open FSharp.Data

#load "parsers.fsx"
open Parsers

module DB = 
    
    [<Literal>]
    let ConnectionString = 
        @"Data Source=.;Initial Catalog=Crypto;Integrated Security=True"

    type Database = SqlProgrammabilityProvider<ConnectionString> 

    let pairPriceTable = new Database.dbo.Tables.PairPrice()

    let writeToDb coinsInExchanges =        
        coinsInExchanges 
        |> Seq.iter (fun (n, pairs) ->
            pairs 
            |> Seq.iter (fun p -> 
                pairPriceTable.AddRow(
                    Date=DateTime.Now,
                    PriceUSD=p.pairPrice.price,
                    Exchange=n,
                    Code=p.baseCurrency.ToLower() + "/" + p.quoteCurrency.ToLower(),
                    Volume=p.volume.volume
                )
            )

            pairPriceTable.Update(batchSize = Seq.length pairs) |> ignore
        )        
