namespace Bighead
{
    public interface IToolbarButton
    {
        void OnClick();

        bool IsIcon();

        string Name();

        int Sort();
    }
}