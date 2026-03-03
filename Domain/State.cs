namespace Domain;

public enum State
{
    Idle,
    RequestReceived,
    Processing,
    Completed,
    Error,
    Retry
}