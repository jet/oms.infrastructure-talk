module Microservice

open OMS.Infrastructure

/// mock lookup function - Given a host URI, returns the appropriate connection string
let lookup (hostUri:string) : string = ""

let log = Log.create "MicroServiceName"

type Input = MessageTypeOne of string | MessageTypeTwo of string
type Output = SideEffectOne of string | SideEffectTwo of string

/// Incoming messages will be deserialzied by the decode function
let decode (de:DomainEvent) : Input option =
    // deserialize de.data into the appropriate input type based on its name
    match de.Name with
    | "MessageTypeOne" -> MessageTypeOne "deserialized data" |> Some
    | "MessagTypeTwo" -> MessageTypeTwo "deserialized data" |> Some
    | _ ->
        // This service is not aware of how to handle these message types,
        // do not process theme
        None

/// The handle function is where all of the business logic is applied.
/// It prepares the output message as an async computation b/c it will
/// have to call into one or more data stores to help apply the 
let handle (input:Input) : Async<Output> = async {
    // process the message, accessing service-specific data stores and create the output message type
    match input with
    | MessageTypeOne data -> return SideEffectOne "New side effect"
    | MessageTypeTwo data -> return SideEffectTwo "New side effect"
}

// Side effects are created in the interpret function, it should not handle any business logic
let interpret (output:Output) : Async<unit> = async {
    // write each side effect to the appropriate output
    match output with
    | SideEffectOne data ->
        let sd = "kafka://my-broker/notifications" |> Service.parse lookup
        let bytes = [||] // serialize the data as bytes
        let de = DomainEvent.New(System.Guid.NewGuid(), "Push Notification", bytes, [||])

        let! result = Outputs.writeWithRetries sd de
        match result with
        // validate the write result and log the succes
        | Choice1Of2 wr -> return()
        // log the exception, and re-raise it
        | Choice2Of2 ex -> raise ex
    | SideEffectTwo data -> return ()
}

[<EntryPoint>]
let main argv =

    // consume order processed events from event store
    let incomingStream = 
        "eventstore://es-host/$et-orderprocessed"
        |> Service.parse lookup
        |> Service.consume
        |> Service.decode decode

    // create a handler for each message
    let handler = Service.processInput log "MicroserviceName" handle interpret

    // start handling the incoming stream
    incomingStream
    |> Service.handle log handler
    |> Service.start

    0 // return an integer exit code
