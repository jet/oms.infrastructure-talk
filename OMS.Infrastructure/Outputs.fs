module OMS.Infrastructure.Outputs

let private log = Log.create "OMS.Infrastructure.Outputs"

let private write (sd:StreamDefinition) (de:DomainEvent) : Async<WriteResult> = async {
    match sd with
    | StreamDefinition.EventStore md ->
        return EventStore (1337L, 42L)
    | StreamDefinition.ServiceBusQueue md ->
        return ServiceBus true
    | StreamDefinition.ServiceBusTopic (topic, md) ->
        return ServiceBus true
    | StreamDefinition.Kafka md ->
        return Kafka (10L, 1)
    | StreamDefinition.CosmosDB md ->
        return CosmosDB (42, "0xDEADBEEF")
}

let writeWithRetries (sd:StreamDefinition) (de:DomainEvent) =
    write sd de
    |> Async.retryBackoff 10 Backoff.ExponentialBoundedRandomized
    |> Async.Catch
    |> Log.logException log sd.Identifier "Write"
    |> Metrics.recordLatency "External" sd.Identifier "Write"

let writeWithSetRetries retryCount (sd:StreamDefinition) (de:DomainEvent) =
    write sd de
    |> Async.retryBackoff retryCount Backoff.ExponentialBoundedRandomized
    |> Async.Catch
    |> Log.logException log sd.Identifier "Write"
    |> Metrics.recordLatency "External" sd.Identifier "Write"

let writeAlways (sd:StreamDefinition) (de:DomainEvent) =
    write sd de
    |> Async.retryIndefinitely log
    |> Async.Catch
    |> Log.logException log sd.Identifier "Write"
    |> Metrics.recordLatency "External" sd.Identifier "Write"
