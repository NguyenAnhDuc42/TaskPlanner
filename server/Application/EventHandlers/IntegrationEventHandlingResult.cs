namespace Application.EventHandlers;

public enum IntegrationEventHandlingResult
{
    Success,
    Retry,
    DeadLetter,
    Skip
}
