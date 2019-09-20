namespace OMS.Infrastructure

open System

type Versioning =
    | SequenceNumber of int64
    | ETag of string

type DomainEvent = {
    Id : Guid
    Name : string
    Data : byte []
    Metadata : byte []
    Version : Versioning option
    Timestamp : DateTimeOffset option
} with
    static member New(id, name, data, metadata) = {
        Id = id
        Name = name
        Data = data
        Metadata = metadata
        Version = None
        Timestamp = None
    }
