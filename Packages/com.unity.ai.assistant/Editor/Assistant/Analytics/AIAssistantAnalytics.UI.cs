using System;
using Unity.AI.Assistant.Bridge.Editor;
using Unity.AI.Assistant.Data;
using Unity.AI.Assistant.FunctionCalling;
using UnityEditor;
using UnityEngine.Analytics;

namespace Unity.AI.Assistant.Editor.Analytics
{
    internal enum UITriggerBackendEventSubType
    {
        FavoriteConversation,
        DeleteConversation,
        RenameConversation,
        LoadConversation,
        CancelRequest,
        CreateNewConversation
    }

    internal enum ContextSubType
    {
        PingAttachedContextObjectFromFlyout,
        ClearAllAttachedContext,
        RemoveSingleAttachedContext,
        DragDropAttachedContext,
        DragDropImageFileAttachedContext,
        ChooseContextFromFlyout,
        ScreenshotAttachedContext,
        AnnotationAttachedContext,
        UploadImageAttachedContext,
        ClipboardImageAttachedContext
    }

    internal enum UITriggerLocalEventSubType
    {
        OpenReferenceUrl,
        SaveCode,
        CopyCode,
        CopyResponse,
        ExpandCommandLogic,
        PermissionRequested,
        PermissionResponse,
        WindowClosed,
        PermissionSettingChanged,
        AutoRunSettingChanged,
        NewChatSuggestionsShown,
        SuggestionCategorySelected,
        SuggestionPromptSelected
    }

    internal static partial class AIAssistantAnalytics
    {
        #region Remote UI Events

        internal const string k_UITriggerBackendEvent = "AIAssistantUITriggerBackendEvent";

        [Serializable]
        internal class UITriggerBackendEventData : IAnalytic.IData
        {
            public UITriggerBackendEventData(UITriggerBackendEventSubType subType) => SubType = subType.ToString();

            public string SubType;
            public string ConversationId;
            public string MessageId;
            public string ResponseMessage;
            public string ConversationTitle;
            public string IsFavorite;
        }

        [AnalyticInfo(eventName: k_UITriggerBackendEvent, vendorKey: k_VendorKey)]
        class UITriggerBackendEvent : IAnalytic
        {
            private readonly UITriggerBackendEventData m_Data;

            public UITriggerBackendEvent(UITriggerBackendEventData data)
            {
                m_Data = data;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = m_Data;
                return true;
            }
        }

        static void ReportUITriggerBackendEvent(UITriggerBackendEventData data)
        {
            SendGatedEditorAnalytic(new UITriggerBackendEvent(data));
        }

        internal static void ReportUITriggerBackendCreateNewConversationEvent()
        {
            ReportUITriggerBackendEvent(new UITriggerBackendEventData(UITriggerBackendEventSubType.CreateNewConversation));
        }

        internal static void ReportUITriggerBackendCancelRequestEvent(AssistantConversationId conversationId)
        {
            ReportUITriggerBackendEvent(new UITriggerBackendEventData(UITriggerBackendEventSubType.CancelRequest)
            {
                ConversationId = conversationId.Value,
            });
        }

        internal static void ReportUITriggerBackendLoadConversationEvent(AssistantConversationId conversationId, string title)
        {
            ReportUITriggerBackendEvent(new UITriggerBackendEventData(UITriggerBackendEventSubType.LoadConversation)
            {
                ConversationId = conversationId.Value,
                ConversationTitle = title,
            });
        }

        internal static void ReportUITriggerBackendFavoriteConversationEvent(AssistantConversationId conversationId, string title, bool isFavorited)
        {
            ReportUITriggerBackendEvent(new UITriggerBackendEventData(UITriggerBackendEventSubType.FavoriteConversation)
            {
                ConversationId = conversationId.Value,
                ConversationTitle = title,
                IsFavorite = isFavorited.ToString(),
            });
        }

        internal static void ReportUITriggerBackendDeleteConversationEvent(AssistantConversationId conversationId, string title)
        {
            ReportUITriggerBackendEvent(new UITriggerBackendEventData(UITriggerBackendEventSubType.DeleteConversation)
            {
                ConversationId = conversationId.Value,
                ConversationTitle = title,
            });
        }

        internal static void ReportUITriggerBackendRenameConversationEvent(AssistantConversationId conversationId, string newTitle)
        {
            ReportUITriggerBackendEvent(new UITriggerBackendEventData(UITriggerBackendEventSubType.RenameConversation)
            {
                ConversationId = conversationId.Value,
                ConversationTitle = newTitle,
            });
        }

        #endregion

        #region Context Events

        internal const string k_ContextEvent = "AIAssistantContextEvent";

        [Serializable]
        internal class ContextEventData : IAnalytic.IData
        {
            public ContextEventData(ContextSubType subType)
            {
                SubType = subType.ToString();
                Timestamp = (long)(EditorApplication.timeSinceStartup * 1_000_000_000L); // In Microseconds like analytics. See SecondsToMicroSeconds in PerformanceReporting.h
            }

            public string SubType;
            public string ContextContent;
            public string ContextType;
            public string IsSuccessful;
            public string MessageId;
            public string ConversationId;
            public long Timestamp;

            internal void StampMessageId(AssistantMessageId messageId)
            {
                MessageId = messageId.FragmentId;
                ConversationId = messageId.ConversationId.IsValid ? messageId.ConversationId.Value : null;
            }
        }

        /// <summary>
        /// Accumulates context attachment analytics events until the message is acknowledged by the
        /// backend, at which point all pending events are flushed with the real backend message ID.
        /// </summary>
        internal class ContextAnalyticsCache
        {
            readonly System.Collections.Generic.List<ContextEventData> m_PendingAttachEvents = new();

            internal void AddAttachEvent(ContextEventData data) => m_PendingAttachEvents.Add(data);

            /// <summary>
            /// Sends all pending attach events stamped with <paramref name="messageId"/>, then clears
            /// the cache.
            /// </summary>
            internal void FlushAll(AssistantMessageId messageId)
            {
                foreach (var data in m_PendingAttachEvents)
                {
                    data.StampMessageId(messageId);
                    ReportContextEvent(data);
                }
                m_PendingAttachEvents.Clear();
            }

            /// <summary>
            /// Finds the most recent pending attach event whose SubType and ContextContent match
            /// <paramref name="contextEntry"/>, sends it stamped with <paramref name="messageId"/>,
            /// and removes it from the cache. Used for remove-single so the corresponding attach
            /// event is paired and sent together with the remove event.
            /// </summary>
            internal void FlushMatchingAttachEvent(AssistantContextEntry contextEntry, AssistantMessageId messageId)
            {
                var entryType = contextEntry.EntryType.ToString();
                var displayValue = contextEntry.DisplayValue;

                for (int i = m_PendingAttachEvents.Count - 1; i >= 0; i--)
                {
                    var pending = m_PendingAttachEvents[i];
                    if (pending.ContextType == entryType && pending.ContextContent == displayValue)
                    {
                        pending.StampMessageId(messageId);
                        ReportContextEvent(pending);
                        m_PendingAttachEvents.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        // Context Group
        [AnalyticInfo(eventName: k_ContextEvent, vendorKey: k_VendorKey)]
        class ContextEvent : IAnalytic
        {
            private readonly ContextEventData m_Data;

            public ContextEvent(ContextEventData data)
            {
                m_Data = data;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }
        }

        static void ReportContextEvent(ContextEventData data)
        {
            SendGatedEditorAnalytic(new ContextEvent(data));
        }

        internal static void CacheContextDragDropAttachedContextEvent(ContextAnalyticsCache cache, UnityEngine.Object obj)
        {
            cache.AddAttachEvent(new ContextEventData(ContextSubType.DragDropAttachedContext)
            {
                ContextContent = obj.name,
                ContextType = obj.GetType().Name,
                IsSuccessful = "false",
            });
        }

        internal static void CacheContextDragDropAttachedContextEvent(ContextAnalyticsCache cache, AssistantContextEntry contextEntry)
        {
            cache.AddAttachEvent(new ContextEventData(ContextSubType.DragDropAttachedContext)
            {
                ContextContent = contextEntry.DisplayValue,
                ContextType = contextEntry.ValueType,
                IsSuccessful = "true",
            });
        }

        internal static void CacheContextDragDropImageFileAttachedContextEvent(ContextAnalyticsCache cache, string fileName, string fileExtension)
        {
            cache.AddAttachEvent(new ContextEventData(ContextSubType.DragDropImageFileAttachedContext)
            {
                ContextContent = fileName,
                ContextType = fileExtension,
                IsSuccessful = "true",
            });
        }

        internal static void CacheContextChooseContextFromFlyoutEvent(ContextAnalyticsCache cache, LogData logData)
        {
            cache.AddAttachEvent(new ContextEventData(ContextSubType.ChooseContextFromFlyout)
            {
                ContextContent = logData.Message,
                ContextType = "LogData",
            });
        }

        internal static void CacheContextChooseContextFromFlyoutEvent(ContextAnalyticsCache cache, UnityEngine.Object obj)
        {
            cache.AddAttachEvent(new ContextEventData(ContextSubType.ChooseContextFromFlyout)
            {
                ContextContent = obj.name,
                ContextType = obj.GetType().ToString(),
            });
        }

        internal static void CacheContextPingAttachedContextObjectFromFlyoutEvent(ContextAnalyticsCache cache, UnityEngine.Object obj)
        {
            cache.AddAttachEvent(new ContextEventData(ContextSubType.PingAttachedContextObjectFromFlyout)
            {
                ContextContent = obj.name,
                ContextType = obj.GetType().ToString(),
            });
        }

        internal static void CacheContextScreenshotAttachedContextEvent(ContextAnalyticsCache cache, string displayName)
        {
            cache.AddAttachEvent(new ContextEventData(ContextSubType.ScreenshotAttachedContext)
            {
                ContextContent = displayName,
                ContextType = "Image",
            });
        }

        internal static void CacheContextAnnotationAttachedContextEvent(ContextAnalyticsCache cache, string displayName)
        {
            cache.AddAttachEvent(new ContextEventData(ContextSubType.AnnotationAttachedContext)
            {
                ContextContent = displayName,
                ContextType = "Image",
            });
        }

        internal static void CacheContextUploadImageAttachedContextEvent(ContextAnalyticsCache cache, string displayName, string fileExtension)
        {
            cache.AddAttachEvent(new ContextEventData(ContextSubType.UploadImageAttachedContext)
            {
                ContextContent = displayName,
                ContextType = fileExtension,
            });
        }

        internal static void CacheContextClipboardImageAttachedContextEvent(ContextAnalyticsCache cache)
        {
            cache.AddAttachEvent(new ContextEventData(ContextSubType.ClipboardImageAttachedContext)
            {
                ContextType = "Image",
            });
        }

        /// <summary>
        /// Flushes all pending attach events from <paramref name="cache"/> stamped with
        /// <paramref name="conversationId"/>, then sends a ClearAllAttachedContext event.
        /// Call this when context is cleared without a message being sent (new conversation,
        /// window close, or user manually clearing all context).
        /// </summary>
        internal static void ReportContextClearAllAttachedContextEvent(ContextAnalyticsCache cache, AssistantConversationId conversationId)
        {
            var messageId = new AssistantMessageId(conversationId, null, AssistantMessageIdType.Internal);
            cache.FlushAll(messageId);
            ReportContextEvent(new ContextEventData(ContextSubType.ClearAllAttachedContext)
            {
                ConversationId = conversationId.IsValid ? conversationId.Value : null,
            });
        }

        /// <summary>
        /// Flushes the matching pending attach event for <paramref name="contextEntry"/> from
        /// <paramref name="cache"/> stamped with <paramref name="conversationId"/>, then sends a
        /// RemoveSingleAttachedContext event. Call this when the user removes a single context item.
        /// </summary>
        internal static void ReportContextRemoveSingleAttachedContextEvent(ContextAnalyticsCache cache, AssistantConversationId conversationId, AssistantContextEntry contextEntry)
        {
            var messageId = new AssistantMessageId(conversationId, null, AssistantMessageIdType.Internal);
            cache.FlushMatchingAttachEvent(contextEntry, messageId);
            ReportContextEvent(new ContextEventData(ContextSubType.RemoveSingleAttachedContext)
            {
                ContextContent = contextEntry.DisplayValue,
                ContextType = contextEntry.EntryType.ToString(),
                ConversationId = conversationId.IsValid ? conversationId.Value : null,
            });
        }

        #endregion

        #region Local UI Events

        internal const string k_UITriggerLocalEvent = "AIAssistantUITriggerLocalEvent";

        [Serializable]
        internal class UITriggerLocalEventData : IAnalytic.IData
        {
            public UITriggerLocalEventData(UITriggerLocalEventSubType subType) => SubType = subType.ToString();

            public string SubType;
            public string UsedInspirationalPrompt;
            public string SuggestionCategory;
            public string ChosenMode;
            public string ReferenceUrl;
            public string ConversationId;
            public string MessageId;
            public string ResponseMessage;
            public string PreviewParameter;
            public string FunctionId;
            public string UserAnswer;
            public string PermissionType;
        }

        [AnalyticInfo(eventName: k_UITriggerLocalEvent, vendorKey: k_VendorKey)]
        class UITriggerLocalEvent : IAnalytic
        {
            private readonly UITriggerLocalEventData m_Data;

            public UITriggerLocalEvent(UITriggerLocalEventData data)
            {
                m_Data = data;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }
        }

        static void ReportUITriggerLocalEvent(UITriggerLocalEventData data)
        {
            SendGatedEditorAnalytic(new UITriggerLocalEvent(data));
        }

        internal static void ReportUITriggerLocalCopyResponseEvent(AssistantMessageId messageId, string responseMessage)
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.CopyResponse)
            {
                ConversationId = messageId.ConversationId.Value,
                MessageId = messageId.FragmentId,
                ResponseMessage = responseMessage,
            });
        }

        internal static void ReportUITriggerLocalCopyCodeEvent(AssistantConversationId conversationId, string responseMessage)
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.CopyCode)
            {
                ConversationId = conversationId.Value,
                ResponseMessage = responseMessage,
            });
        }

        internal static void ReportUITriggerLocalSaveCodeEvent(AssistantConversationId conversationId, string responseMessage)
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.SaveCode)
            {
                ConversationId = conversationId.Value,
                ResponseMessage = responseMessage,
            });
        }

        internal static void ReportUITriggerLocalExpandCommandLogicEvent()
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.ExpandCommandLogic));
        }

        internal static void ReportUITriggerLocalPermissionRequestedEvent(AssistantConversationId conversationId, ToolExecutionContext.CallInfo callInfo, ToolPermissions.PermissionType permissionType)
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.PermissionRequested)
            {
                ConversationId = conversationId.Value,
                FunctionId = callInfo.FunctionId,
                PermissionType = permissionType.ToString(),
            });
        }

        internal static void ReportUITriggerLocalPermissionResponseEvent(AssistantConversationId conversationId, ToolExecutionContext.CallInfo callInfo, ToolPermissions.UserAnswer answer, ToolPermissions.PermissionType permissionType)
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.PermissionResponse)
            {
                ConversationId = conversationId.Value,
                FunctionId = callInfo.FunctionId,
                UserAnswer = answer.ToString(),
                PermissionType = permissionType.ToString(),
            });
        }

        internal static void ReportUITriggerLocalOpenReferenceUrlEvent(string referenceUrl)
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.OpenReferenceUrl)
            {
                ReferenceUrl = referenceUrl,
            });
        }

        internal static void ReportUITriggerLocalWindowClosedEvent()
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.WindowClosed));
        }

        /// <summary>
        /// Fired when the user changes a permission policy setting in the settings window.
        /// </summary>
        /// <param name="permissionSetting">The name of the permission setting that changed (e.g. "FileSystemReadProject")</param>
        /// <param name="newPolicy">The new policy value selected by the user</param>
        internal static void ReportUITriggerLocalPermissionSettingChangedEvent(string permissionSetting, IPermissionsPolicyProvider.PermissionPolicy newPolicy)
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.PermissionSettingChanged)
            {
                PermissionType = permissionSetting,
                UserAnswer = newPolicy.ToString(),
            });
        }

        /// <summary>
        /// Fired when the user toggles the Auto-Run setting on or off.
        /// </summary>
        /// <param name="enabled">The new value of the Auto-Run setting</param>
        internal static void ReportUITriggerLocalAutoRunSettingChangedEvent(bool enabled)
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.AutoRunSettingChanged)
            {
                UserAnswer = enabled.ToString(),
            });
        }

        /// <summary>
        /// Fired every time the new-conversation screen with suggestion prompts becomes visible.
        /// </summary>
        internal static void ReportUITriggerLocalNewChatSuggestionsShownEvent() =>
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.NewChatSuggestionsShown));

        /// <summary>
        /// Fired when the user clicks a suggestion category chip (e.g. "Troubleshoot", "Explore").
        /// </summary>
        /// <param name="categoryLabel">The label of the selected category chip.</param>
        internal static void ReportUITriggerLocalSuggestionCategorySelectedEvent(string categoryLabel)
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.SuggestionCategorySelected)
            {
                SuggestionCategory = categoryLabel,
            });
        }

        /// <summary>
        /// Fired when the user clicks a specific prompt within a suggestion category.
        /// </summary>
        /// <param name="categoryLabel">The category the prompt belongs to.</param>
        /// <param name="promptText">The text of the prompt that was clicked.</param>
        internal static void ReportUITriggerLocalSuggestionPromptSelectedEvent(string categoryLabel, string promptText)
        {
            ReportUITriggerLocalEvent(new UITriggerLocalEventData(UITriggerLocalEventSubType.SuggestionPromptSelected)
            {
                SuggestionCategory = categoryLabel,
                UsedInspirationalPrompt = promptText,
            });
        }

        #endregion
    }
}
