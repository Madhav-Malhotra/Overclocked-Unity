using System;
using Unity.AI.Assistant.Data;
using Unity.AI.Toolkit.Accounts.Services;
using UnityEditor;
using UnityEngine.Analytics;

namespace Unity.AI.Assistant.Editor.Analytics
{
    internal static partial class AIAssistantAnalytics
    {
        const string k_VendorKey = "unity.ai.assistant";
        const string k_SendMessageEvent = "AIAssistantSendUserMessageEvent";

        static void SendGatedEditorAnalytic(IAnalytic analytic)
        {
            if (!Account.sessionStatus.IsUsable)
                return;

            EditorAnalytics.SendAnalytic(analytic);
        }

        #region SendMessageEvent

        [Serializable]
        internal class SendUserMessageEventData : IAnalytic.IData
        {
            public string userPrompt;
            public string commandMode;
            public string conversationId;
            public string messageId;
        }

        [AnalyticInfo(eventName: k_SendMessageEvent, vendorKey: k_VendorKey)]
        class SendUserMessageEvent : IAnalytic
        {
            readonly SendUserMessageEventData m_Data;

            public SendUserMessageEvent(SendUserMessageEventData data)
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

        internal static void ReportUserMessageSentEvent(string userPrompt, AssistantMessageId messageId)
        {
            var data = new SendUserMessageEventData
            {
                userPrompt = userPrompt,
                commandMode = string.Empty,
                messageId = messageId.FragmentId,
                conversationId = messageId.ConversationId.IsValid ? messageId.ConversationId.Value : null
            };

            SendGatedEditorAnalytic(new SendUserMessageEvent(data));
        }

        #endregion
    }
}
