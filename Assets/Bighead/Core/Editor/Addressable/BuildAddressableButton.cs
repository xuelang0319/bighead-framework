namespace Bighead
{
    public class BuildAddressableButton : IToolbarButton
    {
        public void OnClick()
        {
            AddressableManager.BuildAddressableForAllPlatforms();
        }

        public bool IsIcon()
        {
            return false;
        }

        public string Name()
        {
            return "打包\r\n资源";
        }

        public int Sort()
        {
            return 300;
        }
    }
}