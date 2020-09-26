#time "on"
// #load "Bootstrap.fsx"
// #load "Akka"
// #load "Akka.Actor"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Threading
open Akka.TestKit

let system = ActorSystem.Create("Squares", Configuration.defaultConfig())
let N = 1000000000L
let K = 24L
type Information = Info of (int64*int64*int64)


// Printer Actor
let printerActor (mailbox:Actor<_>) = 
    let rec loop () = actor {
        let! (index:int64) = mailbox.Receive()
        printfn "Index at %d" index      
        return! loop()
    }
    loop()
let printerRef = spawn system "Printer" printerActor

// Worker Actors
let mutable count = 0L
let WorkerActor (mailbox:Actor<_>) =
    let rec loop () = actor {
        let! (message : Information) = mailbox.Receive()
        let x : Information = message
        match x with
        | Info(startind, k, endind) -> 
            let mutable sum = 0L

            for i in startind .. startind+k-1L do
                sum <- sum + (i*i)
                
            let value = sum |> double |> sqrt |> int64
            if sum = (value * value) then
                printerRef <! startind
                count <- count + 1L

            for i in startind+1L .. endind do
                sum <- sum + ((i+k-1L)*(i+k-1L)) - ((i-1L)*(i-1L))
                let value = sum |> double |> sqrt |> int64
                if sum = (value * value) then
                    printerRef <! i
                    count <- count + 1L

        return! loop()
    }
    loop()
             

let BossActor (mailbox:Actor<_>) =
    let actcount = System.Environment.ProcessorCount |> int64
    let workerActorsPool = 
            [1L .. actcount]
            |> List.map(fun id -> spawn system (sprintf "Local_%d" id) WorkerActor)

    let workerenum = [|for lp in workerActorsPool -> lp|]
    let workerSystem = system.ActorOf(Props.Empty.WithRouter(Akka.Routing.RoundRobinGroup(workerenum)))

    let rec loop () = actor {
        let! Info(n, k, e) = mailbox.Receive()
        let mutable startind = 1L
        let size = n/actcount
        for id in [1L .. actcount] do
            if id = actcount then
                workerSystem <! Info(startind, k, n)
            else
                workerSystem <! Info(startind, k, (startind + size - 1L))
                startind <- startind + size
       
        return! loop()
    }
    loop()

let boss = spawn system "boss" BossActor
boss <! Info(N, K, 0L)

System.Console.ReadLine()
printfn "Count %d" count 
system.Terminate()