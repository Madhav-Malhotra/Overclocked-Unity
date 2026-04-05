using System;
using System.Threading.Tasks;
using Unity.AI.Assistant.FunctionCalling;

namespace Unity.AI.Assistant.Integrations.Sample.Editor
{
    class SampleApprovalInteraction : IInteractionSource<bool>, IApprovalInteraction
    {
        public string Action { get; }
        public string Detail { get; }
        public string AllowLabel => null;
        public string DenyLabel => null;
        public bool ShowScope => false;

        public event Action<bool> OnCompleted;
        public TaskCompletionSource<bool> TaskCompletionSource { get; } = new();

        public SampleApprovalInteraction(string action, string detail = null)
        {
            Action = action;
            Detail = detail;
        }

        public void Respond(ToolPermissions.UserAnswer answer)
        {
            var approved = answer == ToolPermissions.UserAnswer.AllowOnce
                || answer == ToolPermissions.UserAnswer.AllowAlways;
            TaskCompletionSource.TrySetResult(approved);
            OnCompleted?.Invoke(approved);
        }

        public void CancelInteraction()
        {
            TaskCompletionSource.TrySetCanceled();
        }
    }
}
