using System.Collections.Generic;
using System.Linq;
using Bighead.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace Bighead
{
    [InitializeOnLoad]
    public class ToolbarGenerate
    {
        private static List<IToolbarButton> _toolbarButtons;

        public static void FlushToolbarButtons()
        {
            _toolbarButtons = typeof(IToolbarButton).CreateAllDerivedClass<IToolbarButton>().OrderBy(b => b.Sort()).ToList();
        }

        static ToolbarGenerate()
        {
            FlushToolbarButtons();
            ToolbarExtender.RightToolbarGUI.Add(OnRightToolbarGUI);
        }

        static void OnRightToolbarGUI()
        {
            GUILayout.ExpandWidth(false);
            
            foreach (var button in _toolbarButtons)
            {
                var content = button.IsIcon()
                    ? EditorGUIUtility.IconContent(button.Name())
                    : EditorGUIUtility.TrTextContent(button.Name());
                var style = button.IsIcon() 
                    ? ToolbarStyles.commandButtonStyle 
                    : ToolbarStyles.textButtonStyle;
                if (GUILayout.Button(content, style))
                {
                    button.OnClick();
                }
            }
        }
    }
}