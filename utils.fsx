open System

let (|ValidUSDPrice|_|) (price: string) = 
   let mutable v = 0.M
   let priceStr = price.Trim().Substring(1).Replace(",", "")
   if Decimal.TryParse(priceStr, &v) then Some(v)
   else None   


let (|ValidPrice|ExcludedPrice|InvalidPrice|) (price: string) = 
    let mutable v = 0.M 
     
     
    let priceStr = price.Trim().Substring(1).Replace(",", "")
    if priceStr.Contains("*") then
        let excludedPriceStr = priceStr.Replace("*", "")

        if Decimal.TryParse(excludedPriceStr, &v) 
        then ExcludedPrice v
        else InvalidPrice   
    else
        if Decimal.TryParse(priceStr, &v) 
        then ValidPrice v
        else InvalidPrice   
