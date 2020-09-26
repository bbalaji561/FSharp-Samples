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
let N = 40
let K = 24
type Information = Info of (int*int)

//remote print actor

type ChildActor(id) =
    inherit Actor()
    override x.OnReceive message =
        let x : Information = downcast message
        match x with
        | Info(index,k) -> 
            let mutable sum = 0
            for i in index .. index+k-1 do
                // printfn "Hello Index %d" i 
                sum <- sum + (i*i)

            let value = floor (sqrt (float sum))
            // printfn "sum is %f sqrt is %f square is %f index is %d" (float sum) value (value * value) index
            if (float sum) = (value * value) then
                // printfn "found at %d" index
                printfn "sum is %f sqrt is %f square is %f index is %d\n" (float sum) value (value * value) index

let BossActor (mailbox:Actor<_>) = 
    let rand = Random(1234)
    let sref = select "akka.tcp://Squares@localhost:8777/user/RemoteBossActor" system
    let childActorsPool = 
        [1 .. 5]
        |> List.map(fun id ->   system.ActorOf(Props(typedefof<ChildActor>)))

    let rec loop () = actor {
        let! Info(n, k) = mailbox.Receive()
        printfn "Ipnut is %d" n
        let mutable remote = false
        for id in [1 .. n] do
            if remote then
                printfn "sending %d to remote\n" id
                sref <! Info(id,k)
                remote <- false
            else
                printfn "sending %d to self\n" id
                (rand.Next() % 5) |> List.nth childActorsPool <! Info(id,k)
                remote <- true
        
        return! loop()
    }
    printfn "inside boss after loop decl"
    loop()

let boss = spawn system "boss" BossActor
boss <! Info(N, K)
Thread.Sleep(1000000)
system.Terminate()