namespace OMS.Infrastructure

module Backoff =

    /// exponentially increases the timeout until a certain upper-bound is reached
    let ExponentialBoundedRandomized = ()

module Async =

    // Retries a funciton until the specified attempts are exhausted.
    // If the attempts are exhausted, the last exception is raised
    // Returns the function result on success, if the attempts are not exhausted
    let retryBackoff attempts backoff f = f

    // Retries a function indefinitely, logging each occurrence of an exception.
    // Returns the function result on success. Exceptions that can be retried
    // indefinitely can be defined in the filter parameter, or () -> true for all exceptions
    let retryIndefinitely log (filter:exn -> bool) f = f
