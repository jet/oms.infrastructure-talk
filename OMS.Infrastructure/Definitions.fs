namespace OMS.Infrastructure

// Kafka
type Offset = int64
type Partition = int
type ProduceResult = Offset * Partition

// Event Store
type EventStoreResult = LogPosition * NextExpectedVersion
and LogPosition = int64
and NextExpectedVersion = int64

// Returns the result of a read, usually the data or Not Found
type ReadResult = 
    | NotFound    
    | Found of DomainEvent list

// Returns the result of a write, usually success/failure, or checkpoint
type WriteResult = 
    | Kafka of ProduceResult
    | EventStore of EventStoreResult
    | ServiceBus of bool
    | CosmosDB of int * string // status code, etag

type Incoming = 
    ///Single individual message
    | Message of DomainEvent * CloseAction * CancelAction
    ///Regular batch where messages are in a sequence and commit order matters
    | Batch of DomainEvent[] * CloseAction * CancelAction
    ///A bunch of messages read together but where commit order does not matter
    | Bunch of DomainEvent[] * CloseAction * CancelAction

and CloseAction = unit -> Async<unit>

and CancelAction = unit -> Async<unit>

type Messages<'a> =
| OfBatch of 'a array
| OfBunch of 'a array
