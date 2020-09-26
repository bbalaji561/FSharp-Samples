#time "on"
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
            }
        }")

open Akka.Actor 
let system = ActorSystem.Create("Squares", configuration)

let N = 200
let K = 24
type Information = Info of (int*int*int*int)
let mutable trace = 0
let mutable count = 0

let splitLine = (fun (line : string) -> Seq.toList (line.Split ','))

//remote print actor
let ChildActor (mailbox:Actor<_>) =
    let rec loop () = actor {
        let! (message : Information) = mailbox.Receive()
        let x : Information = message
        match x with
        | Info(startind,k, endind,id) -> 
            let mutable sum: float = 0.0
            for i in startind .. startind+k-1 do
                sum <- sum + float (i*i)
                
            trace <- trace + 1
            let value = floor (sqrt sum)
            if sum = (value * value) then
                printfn "Index %d, id: %d\n" startind id
                count <- count + 1

            for i in startind+1 .. endind do
                trace <- trace + 1
                sum <- sum + float ((i+k-1)*(i+k-1)) - float ((i-1)*(i-1))
                let value = floor (sqrt sum)
                if sum = (value * value) then
                    printfn "Index %d, id: %d\n" i id
                    count <- count + 1
        return! loop()
    }
    loop()
            
let BossActor (mailbox:Actor<_>) =
    let actcount = 4
    let childActorsPool = 
        [1 .. actcount]
        |> List.map(fun id -> spawn system (sprintf "child_%d" id) ChildActor )

    let rec loop () = actor {
        let! Info(n, k, e, j) = mailbox.Receive()
        let mutable startind = 1
        let size = floor (float n/ float actcount)
        for id in [1 .. actcount] do
            if id = actcount then
                let splitLine = (fun (line : string) -> Seq.toList (line.Split ','))
                let remoteDeploy systemPath =
                    let address =
                        match ActorPath.TryParseAddress systemPath with
                        | false, _ -> failwith "ActorPath address cannot be parsed"
                        | true, a -> a
                    Deploy(RemoteScope(address))
                let remoteSystemAddress = "akka.tcp://RemoteSquares@192.168.0.29:8777"
                let remoter =
                    spawne system "remote"
                        <@
                            fun mailbox ->
                            let rec loop(): Cont<string, unit> =
                                actor {
                                    let! msg = mailbox.Receive()
                                    match msg with
                                    | m ->
                                        printfn "Remote actor received: %A" m
                                        let given : string = m
                                        let result = Seq.toList (given.Split ',')
                                        printfn "remote= %A" result
                                        //mailbox.Sender() <! "ECHO " + m
                                    | _ -> logErrorf mailbox "Received unexpected message: %A" msg
                                    return! loop()
                                }
                            loop()
                         @> [ SpawnOption.Deploy(remoteDeploy remoteSystemAddress) ] // Remote options
                let input = string startind + "," + string k + "," + string (startind + int size - 1) + "," + string id
                printfn "Input Given: %s\n" input
                remoter <! input 
            else
                printfn "id is %d\n" id
                //let input1 = string startind + "," + string k + "," + string (startind + int size - 1) + "," + string id
                id |> List.nth childActorsPool <! Info(startind, k, (startind + int size - 1), id)
                startind <- startind + int size
        return! loop()
    }
    loop()
 
let boss = spawn system "boss" BossActor
boss <! Info(N, K, 0, 0)
System.Console.ReadLine();