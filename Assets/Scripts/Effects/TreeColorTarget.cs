using ShiftedSignal.Garden.Tools;
using UnityEngine;

namespace ShiftedSignal.Garden.Effects
{
    public class TreeColorTarget : MonoBehaviour
    {
        [SerializeField] private int treeType;

        [Header("Renderers")]
        [SerializeField] private Renderer leavesRenderer;
        [SerializeField] private Renderer trunkRenderer;

        [Header("Shader Property Names")]
        [SerializeField] private string leavesColorProperty = "_LeavesColor";
        [SerializeField] private string leavesBorderColorProperty = "_LeavesBorderColor";
        [SerializeField] private string trunkColorProperty = "_TrunkColor";
        [SerializeField] private string trunkBorderColorProperty = "_TrunkBorderColor";

        private MaterialPropertyBlock leavesPropertyBlock;
        private MaterialPropertyBlock trunkPropertyBlock;

        public int TreeType => treeType;

        private void Awake()
        {
            leavesPropertyBlock = new MaterialPropertyBlock();
            trunkPropertyBlock = new MaterialPropertyBlock();
        }

        public void ApplyColors(ColorScheme.TreeColorEntry entry)
        {
            if (leavesRenderer != null)
            {
                leavesRenderer.GetPropertyBlock(leavesPropertyBlock);
                leavesPropertyBlock.SetColor(leavesColorProperty, entry.LeavesColor);
                leavesPropertyBlock.SetColor(leavesBorderColorProperty, entry.LeavesBorderColor);
                leavesRenderer.SetPropertyBlock(leavesPropertyBlock);
            }

            if (trunkRenderer != null)
            {
                trunkRenderer.GetPropertyBlock(trunkPropertyBlock);
                trunkPropertyBlock.SetColor(trunkColorProperty, entry.TrunkColor);
                trunkPropertyBlock.SetColor(trunkBorderColorProperty, entry.TrunkBorderColor);
                trunkRenderer.SetPropertyBlock(trunkPropertyBlock);
            }
        }
    }
}