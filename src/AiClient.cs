using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace SnapGrab
{
    internal static class AiClient
    {
        internal static void ConfigureTls()
        {
            // .NET 4.5 defaults can negotiate obsolete TLS. Retain an OS default
            // on modern runtimes; replace explicitly selected legacy protocols.
            if (ServicePointManager.SecurityProtocol != (SecurityProtocolType)0)
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [ThreadStatic] internal static AiRequestCancellation RequestCancellation;
        [ThreadStatic] internal static bool UseSystemProxy;
        [ThreadStatic] internal static int OutputTokens;

        public const int MaxImageEdge = 1600;

        private const string EncPrefix = "enc:";

        public const string EditorNote = "用户当前在截图编辑器里。如果他的要求涉及标注、打码、裁剪或 OCR，请务必给出 ops 指令，客户端会照着画到图上。";

        public static readonly AiPreset[] Presets = new AiPreset[]
        {
            new AiPreset("OpenAI", "https://api.openai.com/v1/chat/completions", "gpt-4o-mini", "官方接口。支持看图分析、返回坐标；生成式改图走 images/generations。", "https://api.openai.com/v1/images/generations", "gpt-image-1"),
            new AiPreset("DeepSeek", "https://api.deepseek.com/chat/completions", "openai", new string[] { "deepseek-flash", "deepseek-v4-pro" }, "Flash 支持看图；V4 Pro 为文本模型。请先运行看图测试，再用于截图编辑。"),
            new AiPreset("小米 MiMo（按量付费）", "https://api.xiaomimimo.com/v1/chat/completions", "openai", new string[] { "mimo-v2.6-pro", "mimo-v2.6-flash" }, "多模态理解模型。使用按量 API Key；订阅密钥请选 Token Plan 预设。"),
            new AiPreset("小米 MiMo（Token Plan）", "https://token-plan-cn.xiaomimimo.com/v1/chat/completions", "openai", new string[] { "mimo-v2.6-pro", "mimo-v2.6-flash" }, "订阅专用接口，需 tp-/ttp- 密钥。模型权限以套餐为准。"),
            new AiPreset("MiniMax", "https://api.minimax.io/v1/chat/completions", "openai", new string[] { "MiniMax-M3", "MiniMax-M2.7", "MiniMax-M2.7-highspeed", "MiniMax-M2.5" }, "M3 支持看图；M2.x 适合文本对话。默认国际按量接口，国内或订阅用户请按控制台修改地址。"),
            new AiPreset("Anthropic Claude", "https://api.anthropic.com/v1/messages", "anthropic", new string[] { "claude-sonnet-5-5", "claude-haiku-4-5", "claude-opus-5-5", "claude-fable-5-1" }, "自动使用 Anthropic Messages 协议；填写 Claude API Key，可进行图片理解。"),
            new AiPreset("Google Gemini", "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", "openai", new string[] { "gemini-3.8-flash" }, "Gemini 官方 OpenAI 兼容接口，填写 AI Studio API Key；可获取账号可用模型列表。"),
            new AiPreset("阿里云百炼（通义千问）", "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions", "qwen-vl-max", "百炼的 OpenAI 兼容模式。qwen-vl 系列对坐标定位支持好，适合自动标注与 OCR。"),
            new AiPreset("智谱 GLM", "https://open.bigmodel.cn/api/paas/v4/chat/completions", "glm-4v-plus", "智谱开放平台，glm-4v 系列支持图片理解。"),
            new AiPreset("月之暗面 Kimi", "https://api.moonshot.cn/v1/chat/completions", "moonshot-v1-8k-vision-preview", "Kimi 视觉模型。"),
            new AiPreset("硅基流动 SiliconFlow", "https://api.siliconflow.cn/v1/chat/completions", "Qwen/Qwen2.5-VL-72B-Instruct", "聚合平台，模型名按平台文档填。"),
            new AiPreset("本地 Ollama", "http://127.0.0.1:11434/v1/chat/completions", "qwen2.5vl", "本地推理，无需联网。需先 ollama pull 对应视觉模型。"),
            new AiPreset("自定义（OpenAI 兼容）", "", "", "填入任意 OpenAI 兼容的 chat/completions 地址与模型名。")
        };

        public static AiPreset FindPreset(string name)
        {
            AiPreset[] presets = Presets;
            foreach (AiPreset aiPreset in presets)
            {
                if (string.Equals(aiPreset.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return aiPreset;
                }
            }
            return Presets[0];
        }

        public static AiPreset FindPresetExact(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            AiPreset[] presets = Presets;
            foreach (AiPreset aiPreset in presets)
            {
                if (string.Equals(aiPreset.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return aiPreset;
                }
            }
            return null;
        }

        public static string ProtectKey(string plain)
        {
            if (string.IsNullOrEmpty(plain))
            {
                return "";
            }
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(plain);
                byte[] inArray = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                return "enc:" + Convert.ToBase64String(inArray);
            }
            catch
            {
                throw new InvalidOperationException("API 密钥加密失败，已停止保存以避免明文泄露。");
            }
        }

        public static string UnprotectKey(string stored)
        {
            if (string.IsNullOrEmpty(stored))
            {
                return "";
            }
            if (!stored.StartsWith("enc:", StringComparison.Ordinal))
            {
                return stored;
            }
            try
            {
                byte[] encryptedData = Convert.FromBase64String(stored.Substring("enc:".Length));
                byte[] bytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "";
            }
        }

        public static bool IsProtected(string stored)
        {
            if (!string.IsNullOrEmpty(stored))
            {
                return stored.StartsWith("enc:", StringComparison.Ordinal);
            }
            return false;
        }

        public static string EncodeImage(Image img, int maxEdge, out float scale)
        {
            scale = 1f;
            if (img == null)
            {
                return "";
            }
            int width = img.Width;
            int height = img.Height;
            int num = Math.Max(width, height);
            Bitmap bitmap = null;
            try
            {
                Image img2 = img;
                if (maxEdge > 0 && num > maxEdge)
                {
                    scale = (float)maxEdge / (float)num;
                    int width2 = Math.Max(1, (int)Math.Round((float)width * scale));
                    int height2 = Math.Max(1, (int)Math.Round((float)height * scale));
                    bitmap = new Bitmap(width2, height2, PixelFormat.Format24bppRgb);
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.DrawImage(img, 0, 0, width2, height2);
                    }
                    img2 = bitmap;
                }
                else
                {
                    scale = 1f;
                }
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    SaveJpeg(img2, memoryStream, 90);
                    return "data:image/jpeg;base64," + Convert.ToBase64String(memoryStream.ToArray());
                }
            }
            finally
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
            }
        }

        private static void SaveJpeg(Image img, Stream to, int quality)
        {
            ImageCodecInfo imageCodecInfo = null;
            ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo imageCodecInfo2 in imageEncoders)
            {
                if (imageCodecInfo2.FormatID == ImageFormat.Jpeg.Guid)
                {
                    imageCodecInfo = imageCodecInfo2;
                    break;
                }
            }
            if (imageCodecInfo == null)
            {
                img.Save(to, ImageFormat.Jpeg);
                return;
            }
            using (EncoderParameters encoderParameters = new EncoderParameters(1))
            {
                encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                img.Save(to, imageCodecInfo, encoderParameters);
            }
        }

        public static string SystemPrompt(bool allowOps)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("你是截图标注助手。用户会给你一张截图，你要看懂它并按需给出标注建议。\n\n");
            if (allowOps)
            {
                stringBuilder.Append("【重要】只输出一个 JSON 对象，不要输出 Markdown 代码块、不要输出任何 JSON 以外的文字。\n");
                stringBuilder.Append("JSON 结构：\n");
                stringBuilder.Append("{\n");
                stringBuilder.Append("  \"say\": \"给用户看的中文说明，简明扼要\",\n");
                stringBuilder.Append("  \"ops\": [\n");
                stringBuilder.Append("    {\"kind\":\"rect\",   \"x\":100,\"y\":200,\"w\":300,\"h\":150, \"note\":\"圈出错误提示\"},\n");
                stringBuilder.Append("    {\"kind\":\"arrow\",  \"x\":100,\"y\":300,\"x2\":400,\"y2\":200, \"note\":\"指向这里\"},\n");
                stringBuilder.Append("    {\"kind\":\"ellipse\",\"x\":50, \"y\":60, \"w\":120,\"h\":80,  \"note\":\"\"},\n");
                stringBuilder.Append("    {\"kind\":\"text\",   \"x\":120,\"y\":240, \"text\":\"这里写错了\", \"note\":\"\"},\n");
                stringBuilder.Append("    {\"kind\":\"mosaic\", \"x\":10, \"y\":10, \"w\":200,\"h\":60,  \"block\":8, \"note\":\"遮挡敏感信息\"},\n");
                stringBuilder.Append("    {\"kind\":\"crop\",   \"x\":0,  \"y\":0,  \"w\":800,\"h\":600, \"note\":\"建议裁掉多余部分\"},\n");
                stringBuilder.Append("    {\"kind\":\"ocr\",    \"x\":0,  \"y\":0,  \"w\":800,\"h\":600, \"text\":\"识别出的文字\", \"note\":\"\"}\n");
                stringBuilder.Append("  ]\n");
                stringBuilder.Append("}\n\n");
                stringBuilder.Append("坐标规则（必须严格遵守）：\n");
                stringBuilder.Append("- x/y 是矩形左上角，w/h 是宽高，单位是**像素**，原点是图片左上角。\n");
                stringBuilder.Append("- arrow 用 x,y 表示起点、x2,y2 表示终点（箭头指向终点）。\n");
                stringBuilder.Append("- 所有坐标必须落在图片范围内，不要超出。\n");
                stringBuilder.Append("- 只在你确实需要标注时才产生 ops；纯聊天或纯建议时 ops 给空数组 []。\n");
                stringBuilder.Append("- 可选字段：color（red/yellow/green/blue/black/white），width（线宽 1-20）。\n\n");
                stringBuilder.Append("额外操作：redact 表示不透明实心遮挡，blur 表示视觉模糊；坐标同 rect。隐藏隐私优先实心遮挡。\n");
                stringBuilder.Append("能力约束：你看不到也无法生成图片本身，只能通过上面的指令让客户端去画。\n");
                stringBuilder.Append("不要编造图中不存在的内容；看不清就直说。\n");
            }
            else
            {
                stringBuilder.Append("请用中文简明描述你看到的画面，并指出值得注意的地方（例如敏感信息、异常报错、排版问题）。\n");
            }
            if (allowOps) stringBuilder.Append(AiImageEditSkill.Instructions);
            return stringBuilder.ToString();
        }

        public static string OcrPrompt()
        {
            return "识别这张截图里的全部文字，尽量保持原有的行序与结构。同时把每段文字所在区域的坐标一并给出。只输出 JSON：{\"say\":\"简要说明\",\"ops\":[{\"kind\":\"ocr\",\"x\":..,\"y\":..,\"w\":..,\"h\":..,\"text\":\"该区域文字\"}]}";
        }

        internal static string JsonPrompt(string prompt)
        {
            return (prompt ?? "") + "\n请只返回一个有效的 JSON 对象（json），不要使用 Markdown 代码块。";
        }

        public static string CallChat(string endpoint, string apiKey, string model, string systemPrompt, string userText, string imageDataUrl, double temperature, bool jsonMode, int timeoutMs)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new Exception("尚未填写接口地址");
            }
            if (string.IsNullOrEmpty(apiKey) && !IsLocal(endpoint))
            {
                throw new Exception("尚未填写 API Key");
            }
            if (jsonMode) systemPrompt = JsonPrompt(systemPrompt);
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = 67108864;
            List<object> list = new List<object>();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                list.Add(new Dictionary<string, object>
                {
                    { "role", "system" },
                    { "content", systemPrompt }
                });
            }
            if (!string.IsNullOrEmpty(imageDataUrl))
            {
                List<object> list2 = new List<object>();
                list2.Add(new Dictionary<string, object>
                {
                    { "type", "text" },
                    {
                        "text",
                        userText ?? ""
                    }
                });
                list2.Add(new Dictionary<string, object>
                {
                    { "type", "image_url" },
                    {
                        "image_url",
                        new Dictionary<string, object> { { "url", imageDataUrl } }
                    }
                });
                list.Add(new Dictionary<string, object>
                {
                    { "role", "user" },
                    { "content", list2 }
                });
            }
            else
            {
                list.Add(new Dictionary<string, object>
                {
                    { "role", "user" },
                    {
                        "content",
                        userText ?? ""
                    }
                });
            }
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["model"] = (string.IsNullOrEmpty(model) ? "gpt-4o-mini" : model);
            dictionary["messages"] = list;
            dictionary["temperature"] = temperature;
            dictionary["max_tokens"] = OutputTokens > 0 ? OutputTokens : 2048;
            if (jsonMode)
            {
                dictionary["response_format"] = new Dictionary<string, object> { { "type", "json_object" } };
            }
            if (OutputTokens > 0) dictionary["max_tokens"] = OutputTokens;
            string payload = javaScriptSerializer.Serialize(dictionary);
            return PostJson(endpoint, apiKey, payload, timeoutMs, true, null);
        }

        public static string CallProvider(AiProvider p, string systemPrompt, string userText, string imageDataUrl, double temperature, bool jsonMode, int timeoutMs)
        {
            if (p == null)
            {
                throw new Exception("尚未配置模型提供商");
            }
            p = p.Snapshot();
            p.Endpoint = AiProvider.NormalizeEndpoint(p.Endpoint, p.IsAnthropic);
            string configurationError = p.ValidationError();
            if (configurationError != null) throw new Exception(configurationError);
            string firstModel = p.FirstModel;
            if (!p.IsAnthropic)
            {
                return CallChat(p.Endpoint, p.ApiKey, firstModel, systemPrompt, userText, imageDataUrl, temperature, jsonMode, timeoutMs);
            }
            return CallAnthropic(p, firstModel, systemPrompt, userText, imageDataUrl, temperature, timeoutMs);
        }

        private static string CallAnthropic(AiProvider p, string model, string systemPrompt, string userText, string imageDataUrl, double temperature, int timeoutMs)
        {
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = 67108864;
            List<object> list = new List<object>();
            list.Add(new Dictionary<string, object>
            {
                { "type", "text" },
                {
                    "text",
                    userText ?? ""
                }
            });
            if (!string.IsNullOrEmpty(imageDataUrl))
            {
                string value = "image/jpeg";
                string value2 = imageDataUrl;
                int num = imageDataUrl.IndexOf(',');
                if (imageDataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && num > 0)
                {
                    string text = imageDataUrl.Substring(5, num - 5);
                    int num2 = text.IndexOf(';');
                    if (num2 > 0)
                    {
                        value = text.Substring(0, num2);
                    }
                    value2 = imageDataUrl.Substring(num + 1);
                }
                list.Add(new Dictionary<string, object>
                {
                    { "type", "image" },
                    {
                        "source",
                        new Dictionary<string, object>
                        {
                            { "type", "base64" },
                            { "media_type", value },
                            { "data", value2 }
                        }
                    }
                });
            }
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["model"] = model;
            dictionary["max_tokens"] = OutputTokens > 0 ? OutputTokens : 2048;
            dictionary["temperature"] = temperature;
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                dictionary["system"] = systemPrompt;
            }
            dictionary["messages"] = new object[1]
            {
                new Dictionary<string, object>
                {
                    { "role", "user" },
                    { "content", list }
                }
            };
            string url = ResolveAnthropicUrl(p.Endpoint);
            Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
            dictionary2["x-api-key"] = p.ApiKey;
            dictionary2["anthropic-version"] = "2023-06-01";
            return PostJson(url, null, javaScriptSerializer.Serialize(dictionary), timeoutMs, false, dictionary2);
        }

        private static string ResolveAnthropicUrl(string endpoint)
        {
            string text = endpoint.TrimEnd(new char[] { '/' });
            if (text.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
            {
                return text;
            }
            if (text.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return text + "/messages";
            }
            return text + "/v1/messages";
        }

        private static string ExtractAnthropicContent(string responseJson, out string error)
        {
            error = null;
            Dictionary<string, object> dictionary = ParseObject(responseJson);
            if (dictionary == null)
            {
                error = "返回不是合法 JSON";
                return null;
            }
            if (dictionary.ContainsKey("error") && dictionary["error"] != null)
            {
                                error = "模型服务返回错误，请检查密钥、模型权限及接口配置（原始详情已隐藏）。";
                return null;
            }
            object[] array = (dictionary.ContainsKey("content") ? (AsArray(dictionary["content"])) : null);
            if (array == null)
            {
                error = "返回里没有 content";
                return null;
            }
            StringBuilder stringBuilder = new StringBuilder();
            object[] array2 = array;
            foreach (object obj in array2)
            {
                Dictionary<string, object> dictionary3 = obj as Dictionary<string, object>;
                if (dictionary3 != null && dictionary3.ContainsKey("text") && dictionary3["text"] != null)
                {
                    stringBuilder.Append(dictionary3["text"]);
                }
            }
            if (stringBuilder.Length == 0)
            {
                error = "content 为空";
                return null;
            }
            return stringBuilder.ToString();
        }

        public static string ExtractContentFor(AiProvider p, string responseJson, out string error)
        {
            if (p != null && p.IsAnthropic)
            {
                return ExtractAnthropicContent(responseJson, out error);
            }
            return ExtractContent(responseJson, out error);
        }

        private static bool IsLocal(string endpoint)
        {
            Uri uri;
            return Uri.TryCreate(endpoint, UriKind.Absolute, out uri) && uri.IsLoopback;
        }

        private static string PostJson(string url, string apiKey, string payload, int timeoutMs, bool needAuth, Dictionary<string, string> extraHeaders)
        {
            ConfigureTls();
            PrivacyGuard.CheckEndpoint(url);
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.AllowAutoRedirect = false;
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Accept = "application/json";
            httpWebRequest.Timeout = timeoutMs;
            httpWebRequest.ReadWriteTimeout = timeoutMs;
            httpWebRequest.UserAgent = "SnipRefine";
            if (!UseSystemProxy) httpWebRequest.Proxy = null;
            AiRequestCancellation requestControl = RequestCancellation ?? new AiRequestCancellation();
            requestControl.Attach(httpWebRequest);
            if (needAuth && !string.IsNullOrEmpty(apiKey))
            {
                httpWebRequest.Headers["Authorization"] = "Bearer " + apiKey;
            }
            if (extraHeaders != null)
            {
                foreach (KeyValuePair<string, string> extraHeader in extraHeaders)
                {
                    if (!extraHeader.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        httpWebRequest.Headers[extraHeader.Key] = extraHeader.Value;
                    }
                }
            }
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            httpWebRequest.ContentLength = bytes.Length;
            requestControl.ThrowIfCancelled();
            using (Stream stream = requestControl.Await(httpWebRequest.GetRequestStreamAsync(),timeoutMs))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            try
            {
                using (HttpWebResponse httpWebResponse = (HttpWebResponse)requestControl.Await(httpWebRequest.GetResponseAsync(),timeoutMs))
                {
                    using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                string text = "";
                try
                {
                    if (ex.Response != null)
                    {
                        using (StreamReader streamReader2 = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
                        {
                            text = streamReader2.ReadToEnd();
                        }
                    }
                }
                catch
                {
                }
                HttpWebResponse failedResponse = ex.Response as HttpWebResponse;
                string text2 = ex.Message;
                if (failedResponse != null)
                {
                    int status = (int)failedResponse.StatusCode;
                    if (status == 401 || status == 403) text2 = "认证失败，请检查 API 密钥和模型访问权限";
                    else if (status == 404) text2 = "接口或模型不存在，请检查 API 地址和默认模型";
                    else if (status == 429) text2 = "请求限流或额度不足，请稍后重试并检查余额";
                    else if (status >= 500) text2 = "模型服务暂时不可用，请稍后重试";
                    text2 += "（HTTP " + status + "）";
                }
                else if (ex.Status == WebExceptionStatus.SecureChannelFailure || ex.Status == WebExceptionStatus.TrustFailure) text2 = "HTTPS 安全连接失败：请检查 Windows 系统时间、系统更新及代理证书；该错误发生在密钥验证之前";
                else if (ex.Status == WebExceptionStatus.Timeout) text2 = "请求超时，请检查网络或稍后重试";
                if (!string.IsNullOrEmpty(text))
                {
                    text2 += "（服务返回详情已隐藏）";
                }
                if (ex.Response != null) ex.Response.Close();
                if (!string.IsNullOrEmpty(apiKey)) text2 = text2.Replace(apiKey, "[密钥已隐藏]");
                if (extraHeaders != null && extraHeaders.ContainsKey("x-api-key") && !string.IsNullOrEmpty(extraHeaders["x-api-key"])) text2=text2.Replace(extraHeaders["x-api-key"],"[密钥已隐藏]");
                throw new Exception(text2);
            }
        }

        private static string Trim(string s, int n)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "";
            }
            s = s.Replace("\r", " ").Replace("\n", " ");
            if (s.Length > n)
            {
                return s.Substring(0, n) + "…";
            }
            return s;
        }

        public static string ExtractContent(string responseJson, out string error)
        {
            error = null;
            try
            {
                Dictionary<string, object> dictionary = ParseObject(responseJson);
                if (dictionary == null)
                {
                    error = "返回不是合法 JSON";
                    return null;
                }
                if (dictionary.ContainsKey("error") && dictionary["error"] != null)
                {
                                        error = "模型服务返回错误，请检查密钥、模型权限及接口配置（原始详情已隐藏）。";
                    return null;
                }
                object[] array = (dictionary.ContainsKey("choices") ? (AsArray(dictionary["choices"])) : null);
                if (array == null || array.Length == 0)
                {
                    error = dictionary.ContainsKey("choices") ? "模型返回的 choices 为空或格式不兼容" : "接口响应缺少 choices，请检查是否使用 Chat Completions 接口";
                    return null;
                }
                Dictionary<string, object> dictionary3 = array[0] as Dictionary<string, object>;
                if (dictionary3 == null || !dictionary3.ContainsKey("message"))
                {
                    error = "choices[0] 结构异常";
                    return null;
                }
                Dictionary<string, object> dictionary4 = dictionary3["message"] as Dictionary<string, object>;
                if (dictionary4 == null || !dictionary4.ContainsKey("content"))
                {
                    error = "message 里没有 content";
                    return null;
                }
                object obj = dictionary4["content"];
                if (obj == null)
                {
                    error = "content 为空";
                    return null;
                }
                object[] array2 = AsArray(obj);
                if (array2 != null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    object[] array3 = array2;
                    foreach (object obj2 in array3)
                    {
                        Dictionary<string, object> dictionary5 = obj2 as Dictionary<string, object>;
                        if (dictionary5 != null && dictionary5.ContainsKey("text") && dictionary5["text"] != null)
                        {
                            stringBuilder.Append(dictionary5["text"]);
                        }
                    }
                    return stringBuilder.ToString();
                }
                return obj.ToString();
            }
            catch (Exception ex)
            {
                error = "解析返回失败：" + ex.Message;
                return null;
            }
        }

        public static AiReply ParseReply(string content)
        {
            AiReply aiReply = new AiReply();
            aiReply.RawJson = content ?? "";
            if (string.IsNullOrEmpty(content))
            {
                return aiReply;
            }
            string text = ExtractJsonObject(content);
            if (text == null)
            {
                aiReply.Say = content.Trim();
                return aiReply;
            }
            Dictionary<string, object> dictionary = ParseObject(text);
            if (dictionary == null)
            {
                aiReply.Say = content.Trim();
                return aiReply;
            }
            if (dictionary.ContainsKey("say") && dictionary["say"] != null)
            {
                aiReply.Say = dictionary["say"].ToString();
            }
            object[] array = (dictionary.ContainsKey("ops") ? (AsArray(dictionary["ops"])) : null);
            if (array != null)
            {
                object[] array2 = array;
                foreach (object obj in array2)
                {
                    Dictionary<string, object> dictionary2 = obj as Dictionary<string, object>;
                    if (dictionary2 != null)
                    {
                        AiOp aiOp = new AiOp();
                        aiOp.Kind = Str(dictionary2, "kind");
                        aiOp.X = Num(dictionary2, "x");
                        aiOp.Y = Num(dictionary2, "y");
                        aiOp.W = Num(dictionary2, "w");
                        aiOp.H = Num(dictionary2, "h");
                        aiOp.X2 = Num(dictionary2, "x2");
                        aiOp.Y2 = Num(dictionary2, "y2");
                        aiOp.Text = Str(dictionary2, "text");
                        aiOp.Color = Str(dictionary2, "color");
                        aiOp.Width = Num(dictionary2, "width");
                        aiOp.Block = (int)Num(dictionary2, "block");
                        aiOp.Note = Str(dictionary2, "note");
                        if (!string.IsNullOrEmpty(aiOp.Kind))
                        {
                            aiReply.Ops.Add(aiOp);
                        }
                    }
                }
            }
            return aiReply;
        }

        private static string ExtractJsonObject(string text)
        {
            int num = text.IndexOf('{');
            if (num < 0)
            {
                return null;
            }
            int num2 = 0;
            bool flag = false;
            bool flag2 = false;
            for (int i = num; i < text.Length; i++)
            {
                char c = text[i];
                if (flag)
                {
                    if (flag2)
                    {
                        flag2 = false;
                        continue;
                    }
                    switch (c)
                    {
                    case '\\':
                        flag2 = true;
                        break;
                    case '"':
                        flag = false;
                        break;
                    }
                    continue;
                }
                switch (c)
                {
                case '"':
                    flag = true;
                    break;
                case '{':
                    num2++;
                    break;
                case '}':
                    num2--;
                    if (num2 == 0)
                    {
                        return text.Substring(num, i - num + 1);
                    }
                    break;
                }
            }
            return null;
        }

        internal static object[] AsArray(object value)
        {
            var array = value as object[];
            if (array != null) return array;
            var list = value as System.Collections.IList;
            if (list == null) return null;
            var result = new object[list.Count];
            list.CopyTo(result, 0);
            return result;
        }

        private static Dictionary<string, object> ParseObject(string json)
        {
            try
            {
                JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                javaScriptSerializer.MaxJsonLength = 67108864;
                return javaScriptSerializer.Deserialize<Dictionary<string, object>>(json);
            }
            catch
            {
                return null;
            }
        }

        private static string Str(Dictionary<string, object> d, string k)
        {
            if (!d.ContainsKey(k) || d[k] == null)
            {
                return "";
            }
            return d[k].ToString();
        }

        private static float Num(Dictionary<string, object> d, string k)
        {
            if (!d.ContainsKey(k) || d[k] == null)
            {
                return 0f;
            }
            float result;
            if (float.TryParse(d[k].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            return 0f;
        }

        public static Color ColorOf(string name, Color fallback)
        {
            if (string.IsNullOrEmpty(name))
            {
                return fallback;
            }
            switch (name.Trim().ToLowerInvariant())
            {
            case "red":
                return Color.FromArgb(232, 59, 59);
            case "yellow":
                return Color.FromArgb(255, 193, 7);
            case "green":
                return Color.FromArgb(24, 169, 91);
            case "blue":
                return Color.FromArgb(45, 140, 240);
            case "black":
                return Color.FromArgb(17, 17, 19);
            case "white":
                return Color.White;
            default:
                return fallback;
            }
        }
    }
}
