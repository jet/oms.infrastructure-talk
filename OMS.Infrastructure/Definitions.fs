namespace OMS.Infrastructure

// Kafka
type Offset = int64
type Partition = int
type ProduceResult = Offset * Partition

// Event Store
type EventStoreResult = LogPosition * NextExpectedVersion
and LogPosition = int64
and NextExpectedVersion = int64

/// Returns the result of a read, usually the data or Not Found
type ReadResult = 
    | NotFound    
    | Found of DomainEvent list

/// Returns the result of a write, usually success/failure, or checkpoint
type WriteResult = 
    | Kafka of ProduceResult
    | EventStore of EventStoreResult
    | ServiceBus of bool
    | CosmosDB of int * string // status code, etag

/// Defines the type of incoming messages
type Incoming = 
    /// Single individual message
    | Message of DomainEvent * CloseAction * CancelAction
    /// Regular batch where messages are in a sequence and commit order matters
    | Batch of DomainEvent[] * CloseAction * CancelAction
    /// A bunch of messages read together but where commit order does not matter
    | Bunch of DomainEvent[] * CloseAction * CancelAction

/// Implements a close/commit action for a message that was processed
and CloseAction = unit -> Async<unit>

/// Implements a cancel/abort action for a message that was not processed
and CancelAction = unit -> Async<unit>

/// Decoded message types with ordering specified by Batch or Bunch
type Messages<'a> =
    /// Regular batch where messages are in a sequence and commit order matters
    | OfBatch of 'a array
    /// A bunch of messages read together but where commit order does not matter
    | OfBunch of 'a array
