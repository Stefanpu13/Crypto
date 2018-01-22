#r @"bin/FSharp.Data.SqlClient.dll" 
open System
open FSharp.Data

#load "parsers.fsx"
open Parsers

module DB = 
    
    [<Literal>]
    let ConnectionString = @"
    Data Source=.;
    Initial Catalog=Crypto;
    Integrated Security=True"

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

    type LocalDatabase = SqlProgrammabilityProvider<ConnectionString> 
    type AzureDatabase = SqlProgrammabilityProvider<AzureConnectionString> 

    let localTable = new LocalDatabase.dbo.Tables.Pairs()
    let azureTable = new AzureDatabase.dbo.Tables.Pairs()

    let writeToLocalDb coinsInExchanges =
        coinsInExchanges 
        |> Seq.iter (fun (n, pairs) ->
            pairs 
            |> Seq.iter (fun p -> 
                localTable.AddRow(
                    Date=DateTime.Now,
                    PriceUSD=p.pairPrice.price,
                    Exchange=n,
                    Code=p.baseCurrency.ToLower() + "/" + p.quoteCurrency.ToLower(),
                    Volume=p.volume.volume
                )
            )

            localTable.Update(batchSize = Seq.length pairs) 
            |> ignore
        )

    let writeToAzureDb coinsInExchanges =
        coinsInExchanges 
        |> Seq.iter (fun (n, pairs) ->
            pairs 
            |> Seq.iter (fun p -> 
                azureTable.AddRow(
                    Date=DateTime.Now,
                    PriceUSD=p.pairPrice.price,
                    Exchange=n,
                    Code=p.baseCurrency.ToLower() + "/" + p.quoteCurrency.ToLower(),
                    Volume=p.volume.volume
                )
            )

            azureTable.Update(batchSize = Seq.length pairs) 
            |> ignore
        )   

