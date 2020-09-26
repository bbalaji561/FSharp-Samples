open System.Threading
#time "on"
#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
#r "nuget: Akka.Remote"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit
open Akka.FSharp
open System

// the most basic configuration of remote actor system
let config = """
akka {
    actor {
        provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
    }
    remote.helios.tcp {
        transport-protocol = tcp
        port = 8777
        hostname = 192.168.0.88
    }
}
"""

System.Console.Title <- "Remote: " + System.Diagnostics.Process.GetCurrentProcess().Id.ToString()
let system = System.create "RemoteSquares" (Configuration.parse config)
printfn "Remote Actor %s listening..." system.Name
System.Console.ReadLine() |> ignore
0