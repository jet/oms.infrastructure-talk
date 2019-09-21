module OMS.Infrastructure.Service

open System
open FSharp.Control
open System.Threading.Tasks

let private sw = System.Diagnostics.Stopwatch.StartNew()


/// Jet's microservice pattern is based on the concept of stream processors.
/// A processor receives an incoming message, decodes it, runs some business logic on it, and generates a side-effect.
/// The microservice implements its own specific (decode, handle, and interpret) functions. Those functions are piped
/// into the OMS Service functions in the following order.
/// 
/// incomingStreamDefinitionUrl
/// |> Service.parse lookup
/// |> Service.consume 
/// |> Service.decode microserviceDecoder
/// |> Service.process log "MicroServiceName" microserviceHandle microServiceInterpret
/// |> Service.handle log
/// |> Service.start


/// parses a URI into a Stream Definition, using a lookup function to replace the host with a connection string 
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

/// Consume a stream of incoming messages from a data store
let consume (sd:StreamDefinition) : AsyncSeq<Incoming> =
    /// read a continuous stream from a service, each stream type will have its own unique implementation
    let consumeFromService () =
        asyncSeq {
            // read a stream from your source, and return an Incoming type
            // semantics for CloseAction and CancelAction are defined here
            let cancel = fun () -> async.Return(())
            let close = fun () -> async.Return(())

            // results are returned as an AsyncSeq
            yield! [] |> AsyncSeq.ofSeq
        }

    // define the consuming details for each service type
    match sd with
    | StreamDefinition.EventStore md -> consumeFromService ()
    | StreamDefinition.ServiceBusQueue md -> consumeFromService ()
    | StreamDefinition.ServiceBusTopic (topic, md) -> consumeFromService ()
    | StreamDefinition.Kafka md -> consumeFromService ()
    | StreamDefinition.CosmosDB md -> consumeFromService ()

/// Decodes incoming messages into domain-specific types used by the microservice
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

/// Orchestrates the handle -> interpret business logic for the microservice
let processInput (log:Logger) label (handle:'a -> Async<'b>) (interpret:'b -> Async<unit>) (input:'a) =
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

/// Handles the semantics for processing of incoming messages
let handle (log:Logger) (handler:'a ->Async<unit>) (incoming:AsyncSeq<Messages<'a option> * CloseAction * CancelAction>) =
    let handleEach state (each:Messages<'a option> * CloseAction * CancelAction) = async {
        let inputs, close, cancel = each

        // Since the decoder will return Option.None for every message that it doesn't need to process
        // it is necessary to parse 
        let useSerialBatchCompletion, messages = 
            match inputs with
            | Messages.OfBatch(m) -> true, m |> Array.choose id
            | Messages.OfBunch(m) -> false, m |> Array.choose id

        match messages.Length with
        | x when x > 0 ->
            // handler for a single message
            let handleSingleMessage message = message |> (handler >> Async.Catch) |> Async.StartAsTask
            
            // The processing semantics for Batches and Bunches will depend on how in-order and out-of-order
            // message processing is implemented. In the OMS, this was managed by using Tasks and Parallel
            // processing.

            // At the end of a Batch or Bunch process, make sure to call the Close Action for all handled messages
            // Depending on whether a batch or bunch failed, it might be necessary to Cancel all messages
            // or just a subset of messages.
            do! close ()
        | _ ->
            // If there is nothing to process, then Cancel and move on
            do! cancel ()

        return state
    }
    
    incoming
    |> AsyncSeq.scanAsync handleEach [||]
    
/// Starts processing the consumers. Note that handling is done on a separate taks.
/// When this method returns, it does not mean that all tasks completed.
let start f =
    f 
    |> AsyncSeq.iter (ignore)
    |> Async.RunSynchronously