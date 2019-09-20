module OMS.Infrastructure.Service

open System
open FSharp.Control

let private sw = System.Diagnostics.Stopwatch.StartNew()

/// parses a URI into a 
let parse (lookup:string -> string) (uriString:string) : StreamDefinition =
    let uri = Uri(uriString)
    match uri.Scheme with
    | "eventstore" ->
        { Repository = ""; Stream = ""; BatchSize = 0; Delay = 0}
        |> StreamDefinition.EventStore
    | "sbqueue" ->
        { Connection = ""; Name = ""; Delay = 0.0; ReadFrom = ""; WriteTo = "" }
        |> StreamDefinition.ServiceBusQueue
    | "sbtopic" ->
        { Connection = ""; Name = ""; Delay = 0.0; ReadFrom = ""; WriteTo = "" }
        |> StreamDefinition.ServiceBusQueue
    | "kafka" ->
        { BrokerList = ""; Topic = ""; ConsumerGroup = ""; BatchSize = 0; Delay = 0; CommitDelay = 0 }
        |> StreamDefinition.Kafka
    | "cosmosdb" ->
        { EndpointURI = ""; Key = ""; DatabaseId = ""; CollectionId = ""; DocumentId = ""; PartitionKey = ""; PartitionId = "" }
        |> StreamDefinition.CosmosDB
    | _ ->
        failwith ("Unsupported scheme " + uri.Scheme)

let consume (sd:StreamDefinition) : AsyncSeq<Incoming> =
    // read a continuous stream from a service, each stream type will have its own unique implementation
    let consumeFromService () =
        asyncSeq {
            // read a stream from your source, and return an Incoming type
            // semantics for CloseAction and CancelAction are defined here
            let cancel = fun () -> async.Return(())
            let close = fun () -> async.Return(())

            yield! [] |> AsyncSeq.ofSeq
        }

    match sd with
    | StreamDefinition.EventStore md -> consumeFromService () 
    | StreamDefinition.ServiceBusQueue md -> consumeFromService ()
    | StreamDefinition.ServiceBusTopic (topic, md) -> consumeFromService ()
    | StreamDefinition.Kafka md -> consumeFromService ()
    | StreamDefinition.CosmosDB md -> consumeFromService ()


let decode<'a> (decoder:DomainEvent -> 'a option) (incoming:AsyncSeq<Incoming>) : AsyncSeq<Messages<'a option> * CloseAction * CancelAction> =
    // decode the incoming messages into a type that can be used by the microservice
    // any message that can be ignored should be decoded as Option.None
    incoming
    |> AsyncSeq.map (function 
        | Incoming.Message(de, close, cancel) -> 
            let input = [| decoder de |] |> Messages.OfBatch 
            input, close, cancel
        | Incoming.Batch(des, close, cancel) -> des |> Array.map decoder |> Messages.OfBatch, close, cancel
        | Incoming.Bunch(des, close, cancel) -> des |> Array.map decoder |> Messages.OfBunch, close, cancel)

let processInput<'a> (log:Logger) label (handle:'a -> Async<'b>) (interpret:'b -> Async<unit>) (input:'a) =
    async {
        try
            let handleStart = sw.ElapsedMilliseconds
            let! output = handle input |> Metrics.recordLatency "Microservice" label "handle"
            do! interpret output |> Metrics.recordLatency "Microservice" label "interpret"
            do! (sw.ElapsedMilliseconds - handleStart) |> Metrics.recordLatencyValue "Microservice" label "total"
        with ex ->
            log.Error "Exception processing message=%A. Exception=%A" (input, ex)
            do! Metrics.recordCounter "Microservice" label "error"
            raise ex
    }
    
//Note that when we start, handling is done on a separate task.
//so when this method returns, it does not mean all the tasks were completed
let start f =
    f 
    |> AsyncSeq.iter (ignore)
    |> Async.RunSynchronously