#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// Bighead 架构的 Build System 项目设置页
    /// </summary>
    public class BuildSystemProvider : SettingsProvider
    {
        // 模块实例
        private HeaderSection _headerSection;
        private BuildSystemSetting _setting;
        private BuildConfigSection _configSection;

        private BuildSystemProvider(string path, SettingsScope scope) : base(path, scope)
        {
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new BuildSystemProvider("Project/Bighead/Build System", SettingsScope.Project);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // 初始化各个模块
            _headerSection = new HeaderSection();
            _setting = BuildSystemSetting.GetOrCreateSettings();
            _configSection = new BuildConfigSection(_setting);
        }

        public override void OnGUI(string searchContext)
        {
            // 顶部标题 + 版本号
            _headerSection?.Draw();
            GUILayout.Space(8);
            _configSection?.Draw();

            // 未来：平台选择、路径显示、构建按钮都可以继续调用各自的 Draw()
        }
    }
}
#endif