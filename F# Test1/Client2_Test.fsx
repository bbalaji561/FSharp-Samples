#time "on"
// #load "Bootstrap.fsx"
// #load "Akka"
// #load "Akka.Actor"
#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: Akka.TestKit"
#r "nuget: FSharp.Json"
#r "nuget: Akka.Serialization.Hyperion"

open FSharp.Json
open System
open Akka
open Akka.Configuration
open Akka.FSharp
open System.Threading
open Akka.TestKit
open Microsoft.FSharp.Quotations
open Hyperion

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
                serializers {
                    hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                }
                serialization-bindings {
                    ""System.Object"" = hyperion
                }
            }
        }")

let N = 10
let K = 2
type Information = Info of (int*int)

open Akka.Actor 
let system = ActorSystem.Create("Squares", configuration)

let Child (mailbox:Actor<_>) = 
    let rec loop () = actor {
        let! (msg : obj) = mailbox.Receive()
        let x : string = downcast msg
        printfn "Input is %s" x
        return! loop()
    }
    loop()

//let ChildActor (msg) = 
    //let (index, k) : Tuple<int, int> = downcast msg
    //let x : string = downcast msg
    //printfn "Hello Index %s" msg
    // let mutable sum = 0
    // for i in index .. index+k-1 do
    //     printfn "Hello Index %d" i 
    //     sum <- sum + (i*i)

    // let value = floor (sqrt (float sum))
    // if (float sum) = (value * value) then
    //     // let pref = select "akka.tcp://RemoteSquares@192.168.0.88:8777/user/Printer" system
    //     // pref <! index   
    //     printfn "Index at %d" index 
    // match x with
    // | Info(index,k) ->
    //         let mutable sum = 0
    //         for i in index .. index+k-1 do
    //             printfn "Hello Index %d" i 
    //             sum <- sum + (i*i)

    //         let value = floor (sqrt (float sum))
    //         if (float sum) = (value * value) then
    //             // let pref = select "akka.tcp://RemoteSquares@192.168.0.88:8777/user/Printer" system
    //             // pref <! index
    //             printfn "Index at %d" index 

//remote print actor
// type RemChildActor() =
//     inherit Actor()
//     override x.OnReceive message =
//         let x : Information = downcast message
//         match x with
//         | Info(index,k) -> 
//             let mutable sum = 0
//             for i in index .. index+k-1 do
//                 // printfn ""Hello Index %d"" i 
//                 sum <- sum + (i*i)

//             let value = floor (sqrt (float sum))
//             // printfn ""sum is %f sqrt is %f square is %f index is %d"" (float sum) value (value * value) index
//             if (float sum) = (value * value) then
//                 let pref = select "akka.tcp://RemoteSquares@192.168.0.88:8777/user/Printer" system
//                 pref <! sprintf "Found at index %d \n" index
//                 // printfn ""found at %d"" index
//                 //printfn "sum is %f sqrt is %f square is %f index is %d\n" (float sum) value (value * value) index     

//let remoteRef2 = spawne system (sprintf "my-actor") 
//                                    (<@ Child @>)
//                                        [SpawnOption.Deploy (Deploy(RemoteScope(Address.Parse "akka.tcp://RemoteSquares@192.168.0.88:8777/")))]

type RecordType = {
    StringMember: string
    IntMember: int
}

// let aref =
//     spawne system "my-actor1" (<@ actorOf (fun msg -> printfn "received %s" (Json.deserialize<RecordType> msg).StringMember) @>)
//             [SpawnOption.Deploy (Deploy(RemoteScope(Address.Parse "akka.tcp://RemoteSquares@192.168.0.88:8777/")))]

let aref =
    spawne system "my-actor1" <@ actorOf (fun msg -> printfn "received %s" msg) @>
            [SpawnOption.Deploy (Deploy(RemoteScope(Address.Parse "akka.tcp://RemoteSquares@192.168.0.88:8777/")))]

let BossActor (mailbox:Actor<_>) = 
    let rec loop () = actor {
        let! Info(n, k) = mailbox.Receive()
        for id in [1 .. n] do
            if (id % 2) = 0 then
                //let data: RecordType = { StringMember = "The string"; IntMember = 123 }
                // serialize record into JSON
                //let json = Json.serialize data
                aref <! "HI"            
            else    
                let localRef = spawn system (sprintf "my-actor_%d" id) Child
                localRef <! "Hi"//Info(id,k)
        
        return! loop()
    }
    printfn "inside boss after loop decl"
    loop()

let boss = spawn system "boss" BossActor
boss <! Info(N, K)
System.Console.ReadLine(); 
//Thread.Sleep(400000)
//system.Terminate()