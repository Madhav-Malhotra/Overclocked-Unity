using System;
using Unity.AI.Assistant.FunctionCalling;
using Unity.AI.Assistant.UI.Editor.Scripts.Components.UserInteraction;
using Unity.AI.Assistant.UI.Editor.Scripts.Data;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components.ChatElements
{
    class AssistantWindowUiContainer : IToolUiContainer, IDisposable
    {
        readonly AssistantUIContext m_Context;

        public AssistantWindowUiContainer(AssistantUIContext context)
        {
            m_Context = context;
        }

        public void PushElement<TOutput>(ToolExecutionContext.CallInfo callInfo, IInteractionSource<TOutput> userInteraction)
        {
            if (userInteraction is IApprovalInteraction interaction)
            {
                var entry = EnqueueApproval(interaction.Action, interaction.Detail,
                    interaction.AllowLabel, interaction.DenyLabel,
                    interaction.Respond, interaction.ShowScope,
                    userInteraction.CancelInteraction);

                if (userInteraction is PermissionInteraction pi && pi.TryAutoResolve != null)
                {
                    entry.TryAutoResolve = () =>
                    {
                        var answer = pi.TryAutoResolve();
                        if (!answer.HasValue) return false;
                        pi.Complete(answer.Value);
                        return true;
                    };
                }

                return;
            }

            if (userInteraction is VisualElement visualElement)
            {
                EnqueueCustomContent(visualElement, userInteraction);
                return;
            }

            // Bare IInteractionSource with no IApprovalInteraction or VisualElement implementation:
            // fall back to a default Allow/Deny approval so the interaction isn't silently dropped.
            EnqueueApproval(null, null, null, null, answer =>
            {
                if (answer == ToolPermissions.UserAnswer.DenyOnce || answer == ToolPermissions.UserAnswer.DenyAlways)
                    userInteraction.CancelInteraction();
                else
                    userInteraction.TaskCompletionSource.TrySetResult(default);
            }, false, userInteraction.CancelInteraction);
        }

        UserInteractionEntry EnqueueApproval(string action, string detail,
            string allowLabel, string denyLabel,
            Action<ToolPermissions.UserAnswer> onRespond, bool showScope,
            Action onCancel)
        {
            var content = new ApprovalInteractionContent();
            content.SetApprovalData(allowLabel, denyLabel, onRespond, showScope);

            var entry = new UserInteractionEntry
            {
                Title = action != null ? "Assistant wants to <b>" + action + "</b>" : null,
                Detail = detail,
                ContentView = content,
                OnCancel = onCancel
            };

            m_Context.InteractionQueue.Enqueue(entry);
            return entry;
        }

        void EnqueueCustomContent<TOutput>(VisualElement visualElement, IInteractionSource<TOutput> userInteraction)
        {
            var entry = new UserInteractionEntry
            {
                CustomContent = visualElement,
                OnCancel = userInteraction.CancelInteraction
            };

            m_Context.InteractionQueue.Enqueue(entry);
            userInteraction.OnCompleted += _ => m_Context.InteractionQueue.Complete(entry);
        }

        public void PopElement<TOutput>(ToolExecutionContext.CallInfo callInfo, IInteractionSource<TOutput> userInteraction)
        {
            // No-op: the queue auto-advances when an entry is completed or cancelled.
        }

        public void Dispose()
        {
            m_Context.InteractionQueue.CancelAll();
        }

        ~AssistantWindowUiContainer()
        {
            Dispose();
        }
    }
}
