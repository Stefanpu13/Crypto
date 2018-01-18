#r @"packages\FSharp.Data.SqlClient.dll" 
open System
open FSharp.Data

#load "parsers.fsx"
open Parsers

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