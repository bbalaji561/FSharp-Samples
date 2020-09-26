open System.Threading
#time "on"
#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"
#r "nuget: Akka.Serialization.Hyperion"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit
open Hyperion

// #Remote Actor
// Actor is not only a concurrency model, it can also be used for distributed computing.
// This example builds an EchoServer using an Actor.
// Then it creates a client to access the Akka URL.
// The usage is the same as with a normal Actor.

                // serializers {
                //     json = ""Akka.Serialization.NewtonSoftJsonSerializer, Akka.Serialization.Serializer""
                // }
                // serialization-bindings {
                //     ""System.Object"" = json
                // }  

type Information = Info of (int*int*int*int)

let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
            log-config-on-start : on
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                serializers {
                    hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                }
                serialization-bindings {
                    ""System.Object"" = hyperion
                }
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }              
            }
            remote {
                helios.tcp {
                    port = 8777
                    hostname = 192.168.0.88
                }
            }
        }")

let system = ActorSystem.Create("RemoteSquares", configuration)

// let PrinterActor (mailbox:Actor<_>) = 
//     let rec loop () = actor {
//         let! index = mailbox.Receive()
//         printfn "Index at %d" index      
//         return! loop()
//     }
//     loop()

// let printerRef = spawn system "Printer" PrinterActor
//printerRef <! "start"
//system.WhenTerminated.Wait()

System.Console.Title <- "Remote: " + System.Diagnostics.Process.GetCurrentProcess().Id.ToString()

printfn "Remote Actor %s listening..." system.Name

System.Console.ReadLine() |> ignore
0



 
