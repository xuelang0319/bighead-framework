namespace Bighead.Upzy.Core
{
    public enum ChangeLevel
    {
        None,    // 无变更
        Patch,   // 热更位 W
        Feature, // 新功能 → Z +1, W=0
        Minor,   // 内容更新 → Y +1, ZW=0
        Major    // 大版本 → X +1, YZW=0
    }
}