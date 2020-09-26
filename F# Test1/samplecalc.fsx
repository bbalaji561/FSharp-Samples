#time "on"
let startind = 1
let k = 24
let endind = 1000000000
let mutable sum : uint64 = 0UL
let mutable count = 0

let checkvals startid interval endid = 
    for i in startid .. startind+interval-1 do
        sum <- sum + uint64 (i*i)
        //printfn "Sum: %d in Index: %d\n" sum i
    let value = (sqrt (double sum))
    if sum = uint64 (value * value) then
        count <- count + 1
        printfn "Index %d Sum %A value %f\n" startid sum value
            
    for i in startid+1 .. endid do
        sum <- sum + uint64 ((i+interval-1)*(i+interval-1)) - uint64 ((i-1)*(i-1))
        //printfn "Sum: %d Index: %d\n" sum i
        let value = (sqrt (double sum))
        if sum = uint64 (value * value) then
           printfn "Index %d Sum %A value %f\n" i sum value
           count <- count + 1

checkvals startind k endind
printfn "Count %d\n" count

