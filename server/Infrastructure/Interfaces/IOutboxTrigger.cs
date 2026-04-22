namespace Infrastructure.Interfaces;


public interface IOutboxTrigger
{
    void Trigger();
}
