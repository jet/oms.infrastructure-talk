namespace OMS.Infrastructure

// mock of NLog Logger
type Logger (name) =

    member x.Error s format = ()
    member x.Info s format = ()

module Log =
    open System.Diagnostics
    let private sw = Stopwatch.StartNew()

    let create name = Logger(name)

    let logException (log:Logger) id tag f =
        async {
            let! result = f
            match result with
            | Choice1Of2 s -> ()
            | Choice2Of2 ex -> log.Error "%s: Failure encountered %s - %A" (tag, id, ex)
            return result
        }
    
    let logElappsed (log:Logger) id tag f =
        async {
            let before = sw.ElapsedMilliseconds
            let! result = f
            let after = sw.ElapsedMilliseconds
            log.Info "%s: %A; Time taken=%dms" (tag, id, (after-before))
            return result
        }
