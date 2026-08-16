namespace Updraft.Data.Entities;

public interface IChangeTracked
{
    Guid ChangeId { get; set; }
}