namespace Bighead.Editor.Csv
{
    public class GenerateCsvButton : IToolbarButton
    {
        public void OnClick()
        {
            Excel2Csv.Generate();
        }

        public bool IsIcon()
        {
            return false;
        }

        public string Name()
        {
            return "生成\r\n表格";
        }

        public int Sort()
        {
            return 1;
        }
    }
}