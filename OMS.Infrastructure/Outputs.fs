/// Module for writing to data stores
module OMS.Infrastructure.Outputs

let private log = Log.create "OMS.Infrastructure.Outputs"

/// Internal writer that implements writes to the data store using the meta data in the Stream Definition
let private write (sd:StreamDefinition) (de:DomainEvent) : Async<WriteResult> = async {
    match sd with
    | StreamDefinition.EventStore md ->
        return EventStore (1337L, 42L)
    | StreamDefinition.ServiceBusQueue md ->
        return ServiceBus true
    | StreamDefinition.ServiceBusTopic (topic, md) ->
        return ServiceBus true
    | StreamDefinition.Kafka md ->
        return Kafka (0L, 0)
    | StreamDefinition.CosmosDB md ->
        return CosmosDB (42, "0xDEADBEEF")
}

/// Returns a WriteResult from a data store, retrying at most 10 times before returning the WriteResult or Exception
let writeWithRetries (sd:StreamDefinition) (de:DomainEvent) =
    write sd de
    |> Async.retryBackoff 10 Backoff.ExponentialBoundedRandomized
    |> Async.Catch
    |> Log.logException log sd.Identifier "Write"
    |> Metrics.recordLatency "External" sd.Identifier "Write"

/// Returns a WriteResult from a data store, retrying at most retryCOunt times before returning the WriteResult or Exception
let writeWithSetRetries retryCount (sd:StreamDefinition) (de:DomainEvent) =
    write sd de
    |> Async.retryBackoff retryCount Backoff.ExponentialBoundedRandomized
    |> Async.Catch
    |> Log.logException log sd.Identifier "Write"
    |> Metrics.recordLatency "External" sd.Identifier "Write"

/// Attempts to write to the data store indefinitely - Useful for when you want to enforce back pressure on errors
let writeAlways (sd:StreamDefinition) (de:DomainEvent) =
    write sd de
    |> Async.retryIndefinitely log (function e -> true)
    |> Async.Catch
    |> Log.logException log sd.Identifier "Write"
    |> Metrics.recordLatency "External" sd.Identifier "Write"
