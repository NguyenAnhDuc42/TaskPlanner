namespace Domain;

public enum OutboxState
{
    Pending,
    Sent,
    DeadLetter
}

