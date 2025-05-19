//
// = The script is part of BigHead and the framework is individually owned by Eric Lee.
// = Cannot be commercially used without the authorization.
//
//  Author  |  UpdateTime     |   Desc  
//  Eric    |  2021年5月23日   |   Toolbar common button style.
//

using UnityEngine;

namespace Bighead
{
    static class ToolbarStyles
    {
        public static readonly GUIStyle commandButtonStyle;
        public static readonly GUIStyle textButtonStyle;

        static ToolbarStyles()
        {
            commandButtonStyle = new GUIStyle("Command")
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageAbove,
                fontStyle = FontStyle.Bold
            };
            textButtonStyle = new GUIStyle("Command")
            {
                fontSize = 8,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageAbove,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Overflow,
            };
        }
        
    }
}