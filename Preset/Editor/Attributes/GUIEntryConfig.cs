using UnityEngine;

namespace SAIN.Attributes
{
    public class GUIEntryConfig
    {
        const float TARGT_WIDTH_SCALE = 1920;

        public GUIEntryConfig(float entryHeight = 25f)
        {
            EntryHeight = entryHeight;
        }

        public float EntryHeight = 25f;
        public float SliderWidth = 0.5f;
        public float ResultWidth = 0.04f;
        public float ResetWidth = 0.05f;
        public float SubList_Indent_Vertical = 3f;
        public float SubList_Indent_Horizontal = 25f;

        public GUILayoutOption[] Toggle => Params(SliderWidth);
        public GUILayoutOption[] Result => Params(ResultWidth);
        public GUILayoutOption[] Reset => Params(ResetWidth);

        GUILayoutOption[] Params(float width0to1) => new GUILayoutOption[]
        {
                GUILayout.Width(width0to1 * TARGT_WIDTH_SCALE),
                GUILayout.Height(EntryHeight)
        };
    }
}
