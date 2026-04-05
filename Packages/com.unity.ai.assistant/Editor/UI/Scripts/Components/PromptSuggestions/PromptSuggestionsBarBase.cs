using System;
using Unity.AI.Assistant.Editor.Analytics;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components.PromptSuggestions
{
    abstract class PromptSuggestionsBarBase : ManagedTemplate
    {
        protected struct PromptData
        {
            public readonly string Text;
            public readonly string UploadHintText;

            public PromptData(string text, string hint = null)
            {
                Text = text;
                UploadHintText = hint;
            }
        }

        protected struct TabData
        {
            public string Label;
            public PromptData[] Prompts;
        }

        protected static readonly TabData k_ImageToSceneTab = new()
        {
            Label  = "Image to scene",
            Prompts = new[]
            {
                new PromptData("Generate scene using primitives from the attached image", "Upload or drag & drop a file"),
                new PromptData("Generate game assets inspired by the attached image", "Upload or drag & drop a file"),
            }
        };

        readonly TabData[] m_VisibleTabs;
        protected Label[] m_TabButtons;
        protected int m_ActiveTabIndex = -1;

        public event Action<string> PromptSelected;

        protected PromptSuggestionsBarBase(TabData[] visibleTabs) : base(AssistantUIConstants.UIModulePath)
        {
            m_VisibleTabs = visibleTabs;
        }

        internal abstract void Collapse();

        protected void RefreshButtonStyles(string activeClass)
        {
            for (var i = 0; i < m_TabButtons.Length; i++)
                m_TabButtons[i].EnableInClassList(activeClass, i == m_ActiveTabIndex);
        }

        protected void BuildButtons(VisualElement container, string cssClass, Action<int> onSelected)
        {
            m_TabButtons = new Label[m_VisibleTabs.Length];

            for (var i = 0; i < m_VisibleTabs.Length; i++)
            {
                var index = i;
                var button = new Label(m_VisibleTabs[i].Label);
                button.AddToClassList(cssClass);
                button.AddManipulator(new Clickable(() => onSelected(index)));
                container.Add(button);
                m_TabButtons[i] = button;
            }
        }

        protected void RefreshPromptList(VisualElement container)
        {
            container.Clear();
            if (m_ActiveTabIndex < 0 || m_ActiveTabIndex >= m_VisibleTabs.Length) return;

            var categoryLabel = m_VisibleTabs[m_ActiveTabIndex].Label;
            foreach (var prompt in m_VisibleTabs[m_ActiveTabIndex].Prompts)
                container.Add(BuildPromptItem(prompt, categoryLabel));
        }

        VisualElement BuildPromptItem(PromptData prompt, string categoryLabel)
        {
            var row = new VisualElement();
            row.AddToClassList("mui-prompt-suggestions-item");

            var label = new Label(prompt.Text);
            label.AddToClassList("mui-prompt-suggestions-item-label");
            row.Add(label);

            if (prompt.UploadHintText != null)
                row.Add(BuildUploadHint(prompt.UploadHintText));

            row.AddManipulator(new Clickable(() =>
            {
                Collapse();
                AIAssistantAnalytics.ReportUITriggerLocalSuggestionPromptSelectedEvent(categoryLabel, prompt.Text);
                PromptSelected?.Invoke(prompt.Text);
            }));

            return row;
        }

        static VisualElement BuildUploadHint(string hintText)
        {
            var hint = new VisualElement();
            hint.AddToClassList("mui-prompt-suggestions-upload-hint");

            var hintLabel = new Label(hintText);
            hintLabel.AddToClassList("mui-prompt-suggestions-upload-hint-label");
            hint.Add(hintLabel);

            var hintIcon = new Image();
            hintIcon.AddToClassList("mui-prompt-suggestions-upload-hint-icon");
            hintIcon.AddToClassList("mui-icon-sort-descending");
            hint.Add(hintIcon);

            return hint;
        }
    }
}
