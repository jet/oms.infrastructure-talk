/// Module for reading from data stores
module OMS.Infrastructure.Inputs

let private log = Log.create "OMS.Infrastructure.Inputs"

/// Internal reader that implements read requests to the data store using the meta data in the Stream Definition
let private readDefault sd = async {
    match sd with
    | StreamDefinition.EventStore md -> return ReadResult.Found []
    | StreamDefinition.CosmosDB md -> return ReadResult.Found []
    | _ ->
        failwithf "Reads for definition %A are not supported" sd
        return ReadResult.NotFound
}

/// Returns a single-attempt ReadResult from a data store
let read (sd:StreamDefinition) : Async<ReadResult> =
    async {
        return!
            sd
            |> readDefault
            |> Metrics.recordLatency "External" sd.Identifier "Read"
    }

/// Returns a Choice of ReadResult or Exception from a data store, retrying on errors until the given retryCount is exhausted
let readWithSetRetries retryCount (sd:StreamDefinition) =
    read sd
    |> Async.retryBackoff retryCount Backoff.ExponentialBoundedRandomized
    |> Async.Catch
    |> Log.logException log sd.Identifier "Read"

/// Returns a ReadResult from a data store, retrying 10 times before returning a Choice of ReadResult or Exception
let readWithRetries (sd:StreamDefinition) =
    readWithSetRetries 10 sd
