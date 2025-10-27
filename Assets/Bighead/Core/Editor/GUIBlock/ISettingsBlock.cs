namespace Bighead.Core.Editor
{
    public interface ISettingsBlock
    {
        string Id { get; }        // 稳定唯一ID（仅用于调试/定位）
        void Render();            // 渲染器内部自理所有GUI，包括标题等
    }
}
