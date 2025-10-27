using UnityEditor;

namespace Bighead.Core.Editor
{
    public abstract class TitleBlock  : ISettingsBlock
    {
        public abstract string Id { get; }
        public abstract string Title { get; }

        public void Render()
        {
            EditorGUILayout.LabelField(Title, EditorStyles.boldLabel);
            OnRender();
        }

        protected abstract void OnRender();
    }
}