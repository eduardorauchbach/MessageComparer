namespace MessageComparer.Engine.Models
{
    public class CompareData
    {
        public CompareData(int rowNum, string? rowBase, string? rowCompare)
        {
            RowNumber = rowNum;
            RowBase = rowBase;
            RowCompare = rowCompare;
        }

        public int RowNumber { get; set; }
        public string? RowBase { get; set; }
        public string? RowCompare { get; set; }

        public bool IsEqual { get { return RowBase == RowCompare; } }
    }
}
