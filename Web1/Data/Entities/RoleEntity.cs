namespace Web1.Data.Entities;

public sealed class RoleEntity
{
    public byte Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<UserEntity> Users { get; set; } = new List<UserEntity>();
}
