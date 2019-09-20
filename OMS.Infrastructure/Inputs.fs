namespace OMS.Infrastructure

module Inputs =

    let private log = Log.create "OMS.Infrastructure.Inputs"

    let private readDefault sd = async {
        match sd with
        | StreamDefinition.EventStore md -> return ReadResult.Found []
        | StreamDefinition.CosmosDB md -> return ReadResult.Found []
        | _ ->
            failwithf "Reads for definition %A are not supported" sd
            return ReadResult.NotFound
    }

    let read (sd:StreamDefinition) : Async<ReadResult> =
        async {
            return!
                sd
                |> readDefault
                |> Metrics.recordLatency "External" sd.Identifier "Read"
        }

    let readWithSetRetries retryCount (sd:StreamDefinition) =
        read sd
        |> Async.retryBackoff retryCount Backoff.ExponentialBoundedRandomized
        |> Async.Catch
        |> Log.logException log sd.Identifier "Read"

    let readWithRetries (sd:StreamDefinition) =
        readWithSetRetries 10 sd

