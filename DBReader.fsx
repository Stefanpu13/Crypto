#r @"packages\FSharp.Data.SqlClient.dll" 
open FSharp.Data

#load "parsers.fsx"
open Parsers

module DB = 
    
    [<Literal>]
    let ConnectionString = 
        @"Data Source=.;Initial Catalog=Crypto;Integrated Security=True"

    let readFromDb () =     
        use cmd = new SqlCommandProvider<"
        Select Date, PriceUSD, Exchange, Code 
        From PairPrice
        ", ConnectionString>(ConnectionString)

        cmd.Execute()

