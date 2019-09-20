module OMS.Infrastructure.Metrics

let private sw = System.Diagnostics.Stopwatch.StartNew()

/// Defines the category of a metric, used by the MetricWriters to decide which construct to use when writing the metric
type Category = Unknown | Latency | Counter | Checkpoint

type Metric = {
    Group : string
    Category : Category
    Label : string
    Value : int64
    CorrelationIds : string list
    Tag : string
}

/// Given a Metric, a MetricWriter will output the appropriate metric to the its service (e.g. Splunk, Prometheus, Dynatrace, NewRelic, etc...)
type MetricWriter = Metric -> Async<unit>

/// Collection of writers that are added during service intialization (New Relic, Prometheus, Splunk, ...)
let writers = System.Collections.Generic.List<MetricWriter>()

/// Publishes the Metric to all registered Metric Writers
let recordMetric (metric:Metric) =
    writers
    |> Seq.map (fun writer -> writer metric)
    |> Async.Parallel
    |> Async.Ignore

/// Publishes a metrics to all registered Metric Writers
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

/// Calculates the time elapsed for an async computation and publishes the result as a metric
let recordLatency group label tag f = async {
    let before = sw.ElapsedMilliseconds
    let! result = f
    let after = sw.ElapsedMilliseconds
    
    do! record group Category.Latency label tag (after - before)
    return result
}

/// Records the provided value as a Latency Metric
let recordLatencyValue group label tag value = 
    record group Category.Latency label tag value

/// Records a single count as a Counter Metric
let recordCounter group label tag =
    record group Category.Counter label tag 1L