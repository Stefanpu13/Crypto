open System

let (|ValidUSDPrice|_|) (price: string) = 
   let mutable v = 0L
   let priceStr = price.Trim().Substring(1).Replace(",", "")
   if Int64.TryParse(priceStr, &v) then Some(v)
   else None   