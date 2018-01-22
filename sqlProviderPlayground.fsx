open System.Data.SqlClient
open System.Data
open System



let AzureConnectionString = @"
Server=tcp:cryptospu.database.windows.net,1433;
Initial Catalog=Crypto;Persist Security Info=False;
User ID=crypto_db;Password=Stefan@2;
MultipleActiveResultSets=False;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
"


let cn = new SqlConnection(AzureConnectionString)
cn.Open()

let cmd = new SqlCommand("select * from Pairs", cn)
let reader = cmd.ExecuteReader()

while (reader.Read()) do    
    let res = reader.[0], reader.[1], reader.[2]
    res



#load "parsers.fsx"

open Parsers

let coinsInExchanges = 
    CoinMarketCap.coinsPerExchanges (CoinMarketCap.getTop25Exchanges())            

let values =
    coinsInExchanges
    |> Seq.map snd
    |> Seq.collect (fun pairs -> 
        Seq.map(fun p ->
            let date ="'" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + "'"
            let priceUSD= p.pairPrice.price.ToString() 
            let exchange ="'" + p.exchangeName + "'"
            let code = "'" + p.baseCurrency.ToLower() + "/" + p.quoteCurrency.ToLower()  + "'"
            let volume =p.volume.volume.ToString() 
            // sprintf """(%A, %A, %A, %A, %A)""" date priceUSD exchange code volume 
            "(" + date + ", " + priceUSD + ", " + exchange + ", " + code + ", " + volume + ")"  
        ) pairs
    )
    |> Seq.windowed 1000

values
|> Seq.sumBy(fun values -> 
    let allValuesInBatch = Seq.reduce (fun res v -> res + ", " + v) values
    // let firstRow = Seq.head values
    let insertCmd = @"
        INSERT INTO dbo.Pairs (Date, PriceUSD, Exchange, Code, Volume) 
        VALUES "
    
    let finalCmd = insertCmd + allValuesInBatch
    let cmd2 = new SqlCommand(finalCmd, cn)

    cmd2.ExecuteNonQuery()
) 
    
values |> Seq.length    
values |> Seq.head |> Seq.head
    
