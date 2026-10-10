using System;
namespace SnapGrab
{
    internal static class PrivacyTests
    {
        internal static void Endpoints()
        {
            foreach(string address in new string[] { "https://api.example.com/v1", "http://127.0.0.1:11434/v1", "http://[::1]:11434/v1", "http://localhost:11434/v1" }) PrivacyGuard.CheckEndpoint(address);
            foreach(string address in new string[] { "http://api.example.com/v1", "https://user:password@api.example.com/v1", "https://api.example.com/v1?key=private", "https://api.example.com/v1#secret", "file:///tmp/private" })
            {
                bool rejected=false; try { PrivacyGuard.CheckEndpoint(address); } catch(InvalidOperationException) { rejected=true; }
                if(!rejected) throw new Exception("Unsafe endpoint accepted");
            }
        }
        internal static void ServiceErrors()
        {
            string secret="example-sensitive-service-echo";
            string json="{\"error\":{\"message\":\""+secret+"\"}}"; string error;
            AiClient.ExtractContent(json,out error);
            if(error==null || error.Contains(secret)) throw new Exception("Service error leaked raw details");
            AiClient.ExtractContentFor(new AiProvider { Protocol="anthropic" },json,out error);
            if(error==null || error.Contains(secret)) throw new Exception("Anthropic error leaked raw details");
        }
        internal static void Defaults()
        {
            using(var panel=new ChatPanel(null,new Config()))
            {
                var toggle=(DshToggle)typeof(ChatPanel).GetField("chkIncludeShot",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(panel);
                if(toggle.Checked) throw new Exception("Screenshot attached without explicit selection");
            }
            using(var source=new System.Drawing.Bitmap(100,100))using(var editor=new EditorForm(null,source,new Config())){
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                var title=(System.Windows.Forms.Label)typeof(EditorForm).GetField("aiTitle",flags).GetValue(editor);
                if(!title.Text.Contains("整图"))throw new Exception("Full-image upload scope not visible");
                typeof(EditorForm).GetMethod("SetAiRegion",flags).Invoke(editor,new object[]{new System.Drawing.Rectangle(10,10,40,40)});
                if(!title.Text.Contains("仅上传选区"))throw new Exception("Region upload scope not visible");
                typeof(EditorForm).GetMethod("SetAiRegion",flags).Invoke(editor,new object[]{System.Drawing.Rectangle.Empty});
                if(!title.Text.Contains("整图"))throw new Exception("Clearing region keeps stale upload scope");
            }
            if(typeof(AiClient).GetMethod("CallImageGeneration")!=null)throw new Exception("Legacy URL download still exposed");
            string secret="local-test-secret";
            string encrypted=AiClient.ProtectKey(secret);
            if(!encrypted.StartsWith("enc:") || AiClient.UnprotectKey(encrypted)!=secret) throw new Exception("Key did not round-trip encrypted");
            string prefixed="enc:not-an-encrypted-key";
            if(AiClient.UnprotectKey(AiClient.ProtectKey(prefixed))!=prefixed) throw new Exception("Plain key bypassed encryption by prefix");
        }
    }
}
