module OMS.Infrastructure.Metrics

let private sw = System.Diagnostics.Stopwatch.StartNew()

type Category = Unknown | Latency | Counter | Checkpoint

type Metric = {
    Group : string
    Category : Category
    Label : string
    Value : int64
    CorrelationIds : string list
    Tag : string
}

type MetricWriter = Metric -> Async<unit>

// Collection of writers that are added during service intialization (New Relic, Prometheus, Splunk, ...)
let writers = System.Collections.Generic.List<MetricWriter>()

let recordMetric (metric:Metric) =
    writers
    |> Seq.map (fun writer -> writer metric)
    |> Async.Parallel
    |> Async.Ignore

let record group category label tag value  = async {
    do!
        {
            Group = group
            Metric.Category = category
            CorrelationIds = []
            Label = label
            Tag = tag
            Value = value
        }
        |> recordMetric
}

let recordLatency group label tag f = async {
    let before = sw.ElapsedMilliseconds
    let! result = f
    let after = sw.ElapsedMilliseconds
    
    do! record group Category.Latency label tag (after - before)
    return result
}

let recordLatencyValue group label tag value = 
    record group Category.Latency label tag value

let recordCounter group label tag =
    record group Category.Counter label tag 1L