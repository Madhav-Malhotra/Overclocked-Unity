using System;
using Unity.AI.Assistant.Editor.Analytics;
using Unity.AI.Assistant.FunctionCalling;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components.ChatElements
{
    class EditorToolPermissions : ToolPermissions
    {
        readonly AssistantUIContext k_Context;

        public EditorToolPermissions(AssistantUIContext context, IToolUiContainer toolUiContainer, IPermissionsPolicyProvider policyProvider) : base(toolUiContainer, policyProvider)
        {
            k_Context = context;
        }

        protected override void OnPermissionRequested(
            ToolExecutionContext.CallInfo callInfo,
            PermissionType permissionType)
        {
            AIAssistantAnalytics.ReportUITriggerLocalPermissionRequestedEvent(k_Context.Blackboard.ActiveConversation.Id, callInfo, permissionType);
        }

        protected override void OnPermissionResponse(
            ToolExecutionContext.CallInfo callInfo,
            UserAnswer answer,
            PermissionType permissionType)
        {
            if (k_Context == null)
            {
                return;
            }

            AIAssistantAnalytics.ReportUITriggerLocalPermissionResponseEvent(k_Context.Blackboard.ActiveConversation.Id, callInfo, answer, permissionType);
        }

        PermissionInteraction CreatePermission(string action, string question, Func<UserAnswer?> tryAutoResolve)
        {
            return new PermissionInteraction(action, question) { TryAutoResolve = tryAutoResolve };
        }

        UserAnswer? CheckState(Func<bool> isAllowed, Func<bool> isDenied)
        {
            if (isAllowed()) return UserAnswer.AllowAlways;
            if (isDenied()) return UserAnswer.DenyAlways;
            return null;
        }

        protected override IInteractionSource<UserAnswer> CreateAssetGenerationElement(ToolExecutionContext.CallInfo callInfo, string path, Type type, long cost)
        {
            return CreatePermission(
                $"Generate {type.Name} asset",
                $"Save to {path}?",
                () => CheckState(State.AssetGeneration.IsAllowed, State.AssetGeneration.IsDenied));
        }

        protected override IInteractionSource<UserAnswer> CreateCodeExecutionElement(ToolExecutionContext.CallInfo callInfo, string code)
        {
            return CreatePermission(
                "Execute code", null,
                () => CheckState(State.CodeExecution.IsAllowed, State.CodeExecution.IsDenied));
        }

        protected override IInteractionSource<UserAnswer> CreateFileSystemAccessElement(ToolExecutionContext.CallInfo callInfo, IToolPermissions.ItemOperation operation, string path)
        {
            var action = PathUtils.IsFilePath(path)
                ? operation switch
                {
                    IToolPermissions.ItemOperation.Read => "Read file from disk",
                    IToolPermissions.ItemOperation.Create => "Create file",
                    IToolPermissions.ItemOperation.Delete => "Delete file",
                    IToolPermissions.ItemOperation.Modify => "Save file",
                    _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
                }
                : operation switch
                {
                    IToolPermissions.ItemOperation.Read => "Read from disk",
                    IToolPermissions.ItemOperation.Create => "Create directory",
                    IToolPermissions.ItemOperation.Delete => "Delete directory",
                    IToolPermissions.ItemOperation.Modify => "Change directory",
                    _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
                };

            var question = operation switch
            {
                IToolPermissions.ItemOperation.Read => $"Read from {path}?",
                IToolPermissions.ItemOperation.Create => $"Write to {path}?",
                IToolPermissions.ItemOperation.Delete => $"Delete {path}?",
                IToolPermissions.ItemOperation.Modify => $"Write to {path}?",
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };

            return CreatePermission(action, question,
                () => CheckState(
                    () => State.FileSystem.IsAllowed(operation, path),
                    () => State.FileSystem.IsDenied(operation, path)));
        }

        protected override IInteractionSource<UserAnswer> CreateScreenCaptureElement(ToolExecutionContext.CallInfo callInfo)
        {
            return CreatePermission(
                "Allow screen capture", null,
                () => CheckState(State.ScreenCapture.IsAllowed, State.ScreenCapture.IsDenied));
        }

        protected override IInteractionSource<UserAnswer> CreateToolExecutionElement(ToolExecutionContext.CallInfo callInfo)
        {
            var functionId = callInfo.FunctionId;
            return CreatePermission(
                "Execute tool", $"Execute {functionId}?",
                () => CheckState(
                    () => State.ToolExecution.IsAllowed(functionId),
                    () => State.ToolExecution.IsDenied(functionId)));
        }

        protected override IInteractionSource<UserAnswer> CreatePlayModeElement(ToolExecutionContext.CallInfo callInfo, IToolPermissions.PlayModeOperation operation)
        {
            var action = operation switch
            {
                IToolPermissions.PlayModeOperation.Enter => "Enter Play Mode",
                IToolPermissions.PlayModeOperation.Exit => "Exit Play Mode",
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };

            return CreatePermission(action, $"{action}?",
                () => CheckState(
                    () => State.PlayMode.IsAllowed(operation),
                    () => State.PlayMode.IsDenied(operation)));
        }

        protected override IInteractionSource<UserAnswer> CreateUnityObjectAccessElement(ToolExecutionContext.CallInfo callInfo, IToolPermissions.ItemOperation operation, Type type, UnityEngine.Object target)
        {
            var action = operation switch
            {
                IToolPermissions.ItemOperation.Read => "Read Object Data",
                IToolPermissions.ItemOperation.Create => "Create New Object",
                IToolPermissions.ItemOperation.Delete => "Delete Object",
                IToolPermissions.ItemOperation.Modify => "Modify Object",
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };

            var objectName = target != null
                ? $"{target.name} ({target.GetType().Name})"
                : type?.Name;

            var question = operation switch
            {
                IToolPermissions.ItemOperation.Read => $"Read from {objectName}?",
                IToolPermissions.ItemOperation.Create => $"Create {objectName}?",
                IToolPermissions.ItemOperation.Delete => $"Delete {objectName}?",
                IToolPermissions.ItemOperation.Modify => $"Modify {objectName}?",
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };

            return CreatePermission(action, question,
                () => CheckState(
                    () => State.UnityObject.IsAllowed(operation, type, target),
                    () => State.UnityObject.IsDenied(operation, type, target)));
        }
    }
}
