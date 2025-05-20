namespace Bighead
{
    public class QuickTestButton : IToolbarButton
    {
        public void OnClick()
        {
            QuickTest.Run();
        }

        public bool IsIcon()
        {
            return true;
        }

        public string Name()
        {
            return "lightMeter/redLight";
        }

        public int Sort()
        {
            return 100;
        }
    }
}