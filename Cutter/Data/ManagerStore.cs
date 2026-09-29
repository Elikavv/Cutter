namespace Cutter.Data
{
    public class ManagerStore
    {
        public string ManagerId { get; set; } = string.Empty;
        public ApplicationUser Manager { get; set; } = null!;
        public Guid StoreId { get; set; }
        public Store Store { get; set; } = null!;
    }
}
