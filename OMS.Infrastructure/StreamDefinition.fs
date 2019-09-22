namespace OMS.Infrastructure

type EventStoreMetadata = {
    Repository : string
    Stream : string
    BatchSize : int
    Delay : int
}

type ServiceBusMetadata = {
    Connection : string
    Name : string
    Delay : float
    ReadFrom : string
    WriteTo : string
}

type KafkaMetadata = {
    BrokerList : string
    Topic : string
    ConsumerGroup : string
    BatchSize : int
    Delay : int
    CommitDelay : int
}

type CosmosDbMetadata = {
    EndpointURI : string
    Key : string
    DatabaseId : string
    CollectionId : string
    DocumentId : string
    PartitionKey : string
    PartitionId : string
}

type StreamDefinition =
    | EventStore of EventStoreMetadata
    | ServiceBusQueue of ServiceBusMetadata
    | ServiceBusTopic of string * ServiceBusMetadata
    | Kafka of KafkaMetadata
    | CosmosDB of CosmosDbMetadata

    member x.Identifier =
        match x with
        | EventStore md -> sprintf "EventStore=>%s" md.Stream
        | ServiceBusQueue md -> sprintf "ServiceBusQueue=>%s" md.Name
        | ServiceBusTopic (topic, md) -> sprintf "ServiceBusTopic=>%s" topic
        | Kafka md -> sprintf "Kafka=>%s" md.Topic
        | CosmosDB md -> sprintf "CosmosDB=>%s/%s" md.DatabaseId md.CollectionId