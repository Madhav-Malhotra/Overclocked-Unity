using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.AI.Assistant.Backend;
using Unity.AI.Assistant.Data;
using Unity.AI.Assistant.FunctionCalling;
using Unity.AI.Assistant.Tools.Editor;
using Unity.AI.Assistant.UI.Editor.Scripts;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components.ChatElements
{
    abstract class ImagePreviewFunctionCallElementBase : VisualElement, IFunctionCallRenderer, IAssistantUIContextAware
    {
        public abstract string Title { get; }
        public string TitleDetails { get; private set; }
        public bool Expanded => true;
        public AssistantUIContext Context { get; set; }

        protected abstract string LoadingMessage { get; }
        protected abstract string ErrorPrefix { get; }
        protected abstract string LoadFailedMessage { get; }

        VisualElement m_ImageContainer;
        Texture2D m_PreviewTexture;

        public void OnCallRequest(AssistantFunctionCall functionCall)
        {
            TitleDetails = string.Empty;

            DestroyPreviewTexture();

            m_ImageContainer = new VisualElement();
            Add(m_ImageContainer);

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public void OnCallSuccess(string functionId, Guid callId, IFunctionCaller.CallResult result)
        {
            JObject jsonResult;
            try
            {
                jsonResult = result.Result is JObject jObj ? jObj : JObject.Parse(result.Result?.ToString() ?? "{}");
            }
            catch
            {
                m_ImageContainer.Add(new Label("Invalid response format."));
                return;
            }

            var imageUrl = jsonResult["sas_url"]?.ToString() ?? jsonResult["url"]?.ToString();

            if (string.IsNullOrEmpty(imageUrl))
            {
                m_ImageContainer.Add(new Label("No image URL available."));
                return;
            }

            var width = jsonResult["width"]?.ToObject<int>() ?? 0;
            var height = jsonResult["height"]?.ToObject<int>() ?? 0;
            DisplayImage(imageUrl, width, height);
        }

        public void OnCallError(string functionId, Guid callId, string error)
        {
            m_ImageContainer.Add(new Label(ErrorPrefix + error));
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            DestroyPreviewTexture();
        }

        void DestroyPreviewTexture()
        {
            if (m_PreviewTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(m_PreviewTexture);
                m_PreviewTexture = null;
            }
        }

        async void DisplayImage(string imageUrl, int width, int height)
        {
            var loadingLabel = new Label(LoadingMessage);
            loadingLabel.AddToClassList("screenshot-preview-loading");
            m_ImageContainer.Add(loadingLabel);

            var texture = await DownloadImageAsync(imageUrl, width, height);

            if (panel == null)
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                return;
            }

            loadingLabel.RemoveFromHierarchy();

            if (texture == null)
            {
                m_ImageContainer.Add(new Label(LoadFailedMessage));
                return;
            }

            DestroyPreviewTexture();
            m_PreviewTexture = texture;

            var image = new Image
            {
                image = m_PreviewTexture,
                scaleMode = ScaleMode.ScaleToFit
            };
            image.AddToClassList("screenshot-preview-image");
            m_ImageContainer.Add(image);
        }

        static async Task<Texture2D> DownloadImageAsync(string url, int width, int height)
        {
            using var request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                return null;

            var texture = new Texture2D(width > 0 ? width : 2, height > 0 ? height : 2);
            if (texture.LoadImage(request.downloadHandler.data))
                return texture;

            UnityEngine.Object.DestroyImmediate(texture);
            return null;
        }
    }
}
