using Unity.AI.Assistant.Editor.Utils.Event;
using Unity.AI.Assistant.UI.Editor.Scripts.Data;
using Unity.AI.Assistant.UI.Editor.Scripts.Events;
using Unity.AI.Assistant.UI.Editor.Scripts.Utils;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components.UserInteraction
{
    class UserInteractionBar : ManagedTemplate
    {
        VisualElement m_BarRoot;
        AssistantImage m_TitleIcon;
        Label m_TitleLabel;
        Label m_DetailLabel;
        Label m_CounterLabel;
        VisualElement m_ContentSlot;

        BaseEventSubscriptionTicket m_QueueChangedSubscription;
        UserInteractionQueue m_ExplicitQueue;
        UserInteractionId m_LastDisplayedEntryId;
        InteractionContentView m_CurrentContentView;

        UserInteractionQueue Queue => m_ExplicitQueue ?? Context?.InteractionQueue;

        public UserInteractionBar()
            : base(AssistantUIConstants.UIModulePath)
        {
        }

        public UserInteractionBar(UserInteractionQueue queue)
            : base(AssistantUIConstants.UIModulePath)
        {
            m_ExplicitQueue = queue;
        }

        protected override void InitializeView(TemplateContainer view)
        {
            m_BarRoot = view.Q<VisualElement>("userInteractionBarRoot");
            m_TitleIcon = new AssistantImage(view.Q<Image>("titleIconElement"), autoHide: true);
            m_TitleLabel = view.Q<Label>("titleLabel");
            m_DetailLabel = view.Q<Label>("detailLabel");
            m_CounterLabel = view.Q<Label>("counterLabel");
            m_ContentSlot = view.Q<VisualElement>("contentSlot");

            RegisterAttachEvents(OnAttach, OnDetach);
            RefreshDisplay();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            m_QueueChangedSubscription = AssistantEvents.Subscribe<EventInteractionQueueChanged>(OnQueueChanged);
            RefreshDisplay();
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            AssistantEvents.Unsubscribe(ref m_QueueChangedSubscription);
        }

        void OnQueueChanged(EventInteractionQueueChanged evt)
        {
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            var queue = Queue;

            if (m_BarRoot == null || queue == null)
            {
                return;
            }

            if (!queue.HasPending)
            {
                m_BarRoot.SetDisplay(false);
                return;
            }

            m_BarRoot.SetDisplay(true);
            
            var entry = queue.Current;
            var maxPending = 1 + (queue.Total - queue.CurrentIndex);
            if (maxPending > 1)
            {
                m_CounterLabel.text = $"1 of {maxPending}";
                m_CounterLabel.SetDisplay(true);
            }
            else
            {
                m_CounterLabel.SetDisplay(false);
            }

            m_TitleLabel.enableRichText = true;
            m_TitleLabel.text = entry.Title ?? "";

            m_TitleIcon.SetIconClassName(entry.TitleIcon);

            m_DetailLabel.text = entry.Detail ?? "";
            m_DetailLabel.SetDisplay(!string.IsNullOrEmpty(entry.Detail));

            if (m_LastDisplayedEntryId != entry.Id)
            {
                m_LastDisplayedEntryId = entry.Id;
                SetContentView(entry);
            }
        }

        void SetContentView(UserInteractionEntry entry)
        {
            if (m_CurrentContentView != null)
            {
                m_CurrentContentView.Completed -= OnContentCompleted;
            }

            m_ContentSlot.Clear();
            m_CurrentContentView = entry.ContentView;

            if (m_CurrentContentView != null)
            {
                if (!m_CurrentContentView.IsInitialized)
                {
                    m_CurrentContentView.Initialize(Context);
                }

                m_CurrentContentView.Completed += OnContentCompleted;
                m_ContentSlot.Add(m_CurrentContentView);
            }
            else if (entry.CustomContent != null)
            {
                m_ContentSlot.Add(entry.CustomContent);
            }
        }

        void OnContentCompleted()
        {
            var queue = Queue;
            if (queue == null || !queue.HasPending)
            {
                return;
            }

            var entry = queue.Current;
            if (entry != null)
            {
                queue.Complete(entry);
            }
        }
    }
}
