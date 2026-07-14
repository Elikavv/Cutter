namespace Cutter.Data
{
    public class Detail
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double Length { get; set; }
        public double Width { get; set; }
        public string? Name { get; set; }
        public int Quantity { get; set; } = 1;
        public bool CanRotate { get; set; } = true;
    }
}
