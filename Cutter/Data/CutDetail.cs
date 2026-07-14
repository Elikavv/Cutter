namespace Cutter.Data
{
    public class CutDetail
    {
        public string? DetailId { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool Rotated { get; set; }
        public string? Name { get; set; }
        public int SheetNumber { get; set; }
        public int SheetId { get; set; }
        public string? Id { get; set; }
    }
}
