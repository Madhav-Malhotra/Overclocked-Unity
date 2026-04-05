using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.AI.Tracing;
using UnityEngine;

namespace Unity.AI.Assistant.Editor
{
    static class TracesUploader
    {
        const string k_UploadPath = "v1/assistant/traces";
        const string k_TraceFileName = "traces.jsonl";
        const string k_ProductionApiHost = "prd.azure.muse.unity.com";
        const long k_MaxPayloadBytes = 2 * 1024 * 1024; // 2 MB

        static readonly HttpClient s_HttpClient = new();
        static Dictionary<string, string> s_CachedHeaders;

        /// <summary>
        /// Cache credentials headers while on the main thread so they're available for background uploads.
        /// </summary>
        public static void CacheCredentials(CredentialsContext credentials)
        {
            s_CachedHeaders = credentials?.Headers;
        }

        public static void UploadTraces(string conversationId, string errorContext = null)
        {
            // Never upload traces in production — legal requirement
            if (AssistantEnvironment.ApiUrl.Contains(k_ProductionApiHost))
                return;

            string[] traceLines;

            try
            {
                var filePath = Path.Combine(TraceLogDir.LogDir, k_TraceFileName);
                if (File.Exists(filePath))
                {
                    var tailBytes = ReadTail(filePath, k_MaxPayloadBytes);
                    var tailText = Encoding.UTF8.GetString(tailBytes);
                    traceLines = tailText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    traceLines = Array.Empty<string>();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AI Assistant] Failed to read trace file: {e.Message}");
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    var clientTraces = new JArray();
                    foreach (var line in traceLines)
                    {
                        try
                        {
                            clientTraces.Add(JObject.Parse(line));
                        }
                        catch (JsonReaderException)
                        {
                            // Skip malformed lines
                        }
                    }

                    var payload = new JObject
                    {
                        ["conversation_id"] = conversationId ?? "",
                        ["trace_id"] = Guid.NewGuid().ToString(),
                        ["editor_version"] = Application.unityVersion,
                        ["package_version"] = GetPackageVersion(),
                        ["error_context"] = errorContext ?? "",
                        ["client_traces"] = clientTraces
                    };

                    var content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");
                    var url = $"{AssistantEnvironment.ApiUrl.TrimEnd('/')}/{k_UploadPath}";

                    using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                    if (s_CachedHeaders != null)
                    {
                        foreach (var header in s_CachedHeaders)
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }

                    var response = await s_HttpClient.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    Debug.Log($"[AI Assistant] Uploaded traces: {response.StatusCode} {responseBody}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AI Assistant] Failed to upload traces: {e.Message}");
                }
            });
        }

        static string GetPackageVersion()
        {
            try
            {
                var path = Path.Combine("Packages", AssistantConstants.PackageName, "package.json");
                if (!File.Exists(path))
                    return "unknown";

                var json = JObject.Parse(File.ReadAllText(path));
                return json["version"]?.ToString() ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        static byte[] ReadTail(string filePath, long maxBytes)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (stream.Length <= maxBytes)
            {
                var allBytes = new byte[stream.Length];
                stream.Read(allBytes, 0, allBytes.Length);
                return allBytes;
            }

            // Seek to approximately maxBytes from the end
            stream.Seek(-maxBytes, SeekOrigin.End);

            // Skip forward past the partial first line to the next newline
            int b;
            while ((b = stream.ReadByte()) != -1 && b != '\n') { }

            var remaining = (int)(stream.Length - stream.Position);
            var tailBytes = new byte[remaining];
            stream.Read(tailBytes, 0, remaining);
            return tailBytes;
        }
    }
}
