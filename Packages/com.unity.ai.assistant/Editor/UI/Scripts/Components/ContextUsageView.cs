using Unity.AI.Assistant.UI.Editor.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components
{
    class ContextUsageView : ManagedTemplate
    {
        const string k_WarningClass = "mui-context-usage--warning";

        Label m_Label;
        VisualElement m_Ring;
        VisualElement m_Root;
        float m_Progress;

        public ContextUsageView()
            : base(AssistantUIConstants.UIModulePath)
        {
        }

        public void SetUsage(int usedTokens, int maxTokens)
        {
            m_Progress = maxTokens > 0 ? (float)usedTokens / maxTokens : 0f;
            m_Label.text = Mathf.RoundToInt(m_Progress * 100) + "%";
            m_Root.SetDisplay(true);
            m_Root.EnableInClassList(k_WarningClass, m_Progress > 0.8f);
            m_Ring.MarkDirtyRepaint();
        }

        public void ResetUsage()
        {
            m_Progress = 0f;
            m_Root.SetDisplay(false);
            m_Root.EnableInClassList(k_WarningClass, false);
        }

        protected override void InitializeView(TemplateContainer view)
        {
            m_Root = view.Q<VisualElement>("contextUsageRoot");
            m_Label = view.Q<Label>("contextUsageLabel");
            m_Ring = view.Q<VisualElement>("contextUsageRing");
            m_Ring.generateVisualContent += DrawRing;
        }

        void DrawRing(MeshGenerationContext ctx)
        {
            var rect = m_Ring.contentRect;
            if (rect.width <= 0)
            {
                return;
            }

            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            var strokeWidth = 2f;

            var painter = ctx.painter2D;
            painter.lineWidth = strokeWidth;
            painter.lineCap = LineCap.Butt;

            painter.strokeColor = new Color(1f, 1f, 1f, 0.15f);
            painter.BeginPath();
            painter.Arc(center, radius - strokeWidth * 0.5f, 0, 360);
            painter.Stroke();

            if (m_Progress <= 0)
            {
                return;
            }

            painter.strokeColor = m_Progress > 0.8f ? new Color(1f, 0.71f, 0.24f) : new Color(1f, 1f, 1f, 0.7f);
            painter.BeginPath();
            painter.Arc(center, radius - strokeWidth * 0.5f, -90, -90 + m_Progress * 360f);
            painter.Stroke();
        }
    }
}
