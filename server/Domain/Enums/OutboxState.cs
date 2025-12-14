namespace Domain.Enums;

public enum OutboxState
{
    Pending,
    Sent,
    DeadLetter
}
