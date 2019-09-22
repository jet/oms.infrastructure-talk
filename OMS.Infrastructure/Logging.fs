namespace OMS.Infrastructure

/// Mock logger - Used mostly for logging examples
type Logger (name) =

    member x.Error s format = ()
    member x.Info s format = ()

/// The Log module encapsulates consistent log details for error, timing, and more
module Log =
    open System.Diagnostics
    let private sw = Stopwatch.StartNew()

    /// creates a new Logger for a module / class / service
    let create name = Logger(name)

    /// Logs exceptions if they occur
    let logException (log:Logger) id tag f =
        async {
            let! result = f
            match result with
            | Choice1Of2 s -> ()
            | Choice2Of2 ex -> log.Error "%s: Failure encountered %s - %A" (tag, id, ex)
            return result
        }
    
    /// Calculates the time elapsed for an async computation and logs the results
    let logElapsed (log:Logger) id tag f =
        async {
            let before = sw.ElapsedMilliseconds
            let! result = f
            let after = sw.ElapsedMilliseconds
            log.Info "%s: %s; Time taken=%dms" (tag, id, (after-before))
            return result
        }
