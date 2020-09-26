#time "on"
#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"

open System
open Akka.FSharp
open Akka.Actor
open Akka.Configuration

let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            log-config-on-start : on
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
            }
            remote.helios.tcp {
                transport-protocol = tcp
                port = 8778
                hostname = 192.168.0.88
            }
        }")

type Information = Info of (int64*int64*int64)
let mutable replies = 0


let printerActor (mailbox:Actor<_>) = 
    let rec loop () = actor {
        let! (index:int64) = mailbox.Receive()

        printfn "%d" index      
        return! loop()
    }
    loop()

let remoteDeploy systemPath =
    let address =
        match ActorPath.TryParseAddress systemPath with
        | false, _ -> failwith "Actor Path addr failed to be parsed"
        | true, a -> a
    Deploy(RemoteScope(address))

let remoteSystemAddress = "akka.tcp://RemoteSquares@192.168.0.29:8777"
let system = System.create "Squares" (configuration)
let printerRef = spawn system "Printer" printerActor
printerRef <! "start"

let remoter idnum =
    spawne system (sprintf "remote_%d" idnum)
        <@
            let rec add (i:int64) (ed:int64) (sm:int64) = 
                //printfn "sum = %d" sm
                let mutable sum = sm
                for j in i..ed do
                    let newval = sum + (i*i)
                    sum <- newval
                sum

            let squareroot (i:int64) = 
                let value = i |> float |> sqrt
                let vali = value |> int64
                vali

            let rec calcWindow (j:int64) (ed:int64) (sm:int64) (k:int64) (send:ActorSelection) = 
                let mutable smm = sm
                for i in j+1L .. ed do
                    //printfn "j+il = %d" i
                    let newval = smm + ((i+k-1L)*(i+k-1L)) - ((i-1L)*(i-1L))
                    smm <- newval
                    if smm < 0L then
                        printfn "summmmmy = %d" smm
                    let value = squareroot smm
                    if smm = (value * value) then
                        //printfn "newval: %d" value
                        send <! i   
                smm                     
                // let newsum = sm + ((i+k-1L)*(i+k-1L)) - ((i-1L)*(i-1L))
                // printfn "mewsum = %d" i 
                // if newsum >= 0L then
                //     let value = newsum |> float |> sqrt |> int64
                //     // let value = flt |> sqrt |> int64
                //     // printfn "Inside Calc1 Value: %d" value
                //     if i > ed then
                //         //printfn "Inside Calc Value: %d" value
                //         sm
                //     elif newsum = (value * value) then
                //         send <! i
                //         calcWindow (i+1L) ed newsum k send
                //     else
                //         calcWindow (i+1L) ed newsum k send
                // else
                //     0L

            fun mailbox ->
            let rec loop(): Cont<string, unit> =
                actor {
                    let! msg = mailbox.Receive()
                    let sender = mailbox.ActorSelection("akka.tcp://Squares@192.168.0.88:8778/user/Printer")
                    match msg with
                    | m ->
                        let given : string = m
                        let result = Seq.toList (given.Split ',')
                        let startind = result.[0] |> int64
                        let k = result.[1] |> int64
                        let endind = result.[2] |> int64
                        //let mutable sum = add 62500000L (62500000L+k-1L) (0L)
                        let sum = add startind (startind+k-1L) (0L)
                        printfn "Sum: %d\n" sum
                    //if sum >= 0L then 
                        let value = squareroot sum
                        printfn "Value: %d\n" value
                        let prod = value * value
                        printfn "Prod: %d\n" prod
                        if sum = prod then
                           sender <! startind
                           //ignore

                        // printfn "calc start= : %d\n" (startind+1L)
                        // printfn "end ind= : %d\n" endind
                        // printfn "sum = : %d\n" sum
                        let finalsum = calcWindow (startind+1L) endind sum k sender                        
                        ignore

                    | _ -> logErrorf mailbox "Received unexpected message: %A" msg
                        
                    return! loop()
                }
            loop()
        @> [ SpawnOption.Deploy(remoteDeploy remoteSystemAddress) ] 

let ChildActor (mailbox:Actor<_>) =
    let rec loop () = actor {
        let! (message : Information) = mailbox.Receive()
        let x : Information = message
        //printfn "RECEIEVED ...."
        match x with
        | Info(startind,k,endind) -> 
            let mutable sum = 0L
            for i in startind .. startind+k-1L do
                sum <- sum + (i*i)
                
            let value = sum |> double |> sqrt |> int64
            if sum = (value * value) then
                printerRef <! startind

            for i in startind+1L .. endind do
                sum <- sum + ((i+k-1L)*(i+k-1L)) - ((i-1L)*(i-1L))
                let value = sum |> double |> sqrt |> int64
                //printfn "Sum: %d" sum
                if sum = (value * value) then
                    printerRef <! i

        return! loop()
    }
    loop()

let delegator (mailbox:Actor<_>) = 
    let actorcount = System.Environment.ProcessorCount |> int64
    let splits = actorcount*2L
    let localPool = 
        [1L .. actorcount]
        |> List.map(fun id -> spawn system (sprintf "Local_%d" id) ChildActor)

    let localenum = [|for lp in localPool -> lp|]
    let localSystem = system.ActorOf(Props.Empty.WithRouter(Akka.Routing.RoundRobinGroup(localenum)))
    let remotePool = 
        [1L .. actorcount]
        |> List.map(fun id -> (remoter id))

    let remoteenum = [|for rp in remotePool -> rp|]
    let remoteSystem = system.ActorOf(Props.Empty.WithRouter(Akka.Routing.RoundRobinGroup(remoteenum)))

    let rec delloop () = 
        actor {
            let! mail = mailbox.Receive()
            let (n,k) : (int64*int64) = mail
            
            let subsize = n/splits
            let mutable startind = 1L

            for i in [1L..splits] do
                if i = splits then
                    localSystem <! Info(startind, k, n)        
                elif i%2L = 0L then
                    printfn "Entering local: %d" startind
                    let endind = startind + subsize - 1L
                    localSystem <! Info(startind, k, endind)
                    startind <- startind + subsize
                else
                    let remoteInput = string startind + "," + string k + "," + string (startind + subsize - 1L)
                    remoteSystem <! remoteInput
                    startind <- startind + subsize
                            
            return! delloop()
        }
    delloop()


let N = fsi.CommandLineArgs.[1] |> int64
let K = fsi.CommandLineArgs.[2] |> int64
let del = spawn system "delegator" delegator
del <! (N,K)

system.WhenTerminated.Wait()
0