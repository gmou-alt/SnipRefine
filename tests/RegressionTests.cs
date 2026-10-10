using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace SnapGrab
{
    internal static class RegressionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int passed;
        private static int failed;

        private static void TestPresets()
        {
            var names = new HashSet<string>();
            foreach (var preset in AiClient.Presets)
            {
                Check(names.Add(preset.Name), "Duplicate preset");
                if (preset.Models.Length == 0) continue;
                var provider = new AiProvider { Endpoint = "https://old.example/v1/chat/completions", ApiKey = "old-key" };
                preset.Fill(provider);
                Check(provider.ApiKey == "", "Cross-service credential leak");
                Check(provider.FirstModel == preset.Model && provider.Models.Count == preset.Models.Length, "Model list/default");
                provider.ApiKey = "same-service-key";
                preset.Fill(provider);
                Check(provider.ApiKey == "same-service-key", "Same endpoint key lost");
                Check(provider.Protocol == preset.Protocol && Uri.IsWellFormedUriString(provider.Endpoint, UriKind.Absolute), "Protocol/URL");
                provider.Models.Clear();
                Check(preset.Models.Length > 0, "Preset list mutated");
            }
            Check(AiClient.FindPreset("Anthropic Claude").Protocol == "anthropic", "Claude protocol");
        }

        private static void TestTlsPolicy()
        {
            var previous = System.Net.ServicePointManager.SecurityProtocol;
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;
                AiClient.ConfigureTls();
                Check(System.Net.ServicePointManager.SecurityProtocol == System.Net.SecurityProtocolType.Tls12, "Legacy TLS not replaced");
                AiClient.ConfigureTls();
                Check(System.Net.ServicePointManager.SecurityProtocol == System.Net.SecurityProtocolType.Tls12, "TLS configuration not stable");
            }
            finally { System.Net.ServicePointManager.SecurityProtocol = previous; }
        }

        private static void TestGlassBounds()
        {
            using (var bitmap = new Bitmap(640, 440))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                foreach (int width in new int[] { -1, 0, 1, 2, 3, 588 })
                foreach (int radius in new int[] { -1, 0, 1, 16 })
                for (int height = -1; height <= 420; height++)
                {
                    var bounds = new Rectangle(0, 0, width, height);
                    Dsh.DrawGlass(graphics, bounds, radius, false, true);
                    using (var path = Dsh.Round(bounds, radius))
                        if (width <= 0 || height <= 0) Check(path.PointCount == 0, "Invalid bounds not empty");
                }
                using (var card = new DshGlassCard())
                {
                    card.Width = 588;
                    var paint = typeof(DshGlassCard).GetMethod("OnPaint", Private);
                    foreach (int height in new int[] { 420, 32, 3, 2, 1, 0, 1, 2, 3, 420 })
                    {
                        card.Height = height;
                        using (var args = new PaintEventArgs(graphics, new Rectangle(0, 0, 588, height)))
                            paint.Invoke(card, new object[] { args });
                    }
                }
            }
        }

        private static void TestSelectorClip()
        {
            using (var selector = new DshSelect())
            {
                selector.Items.Add("PNG（无损，推荐）"); selector.SelectedIndex = 0;
                foreach (int height in new int[] { 30, 34, 36, 38, 44 })
                {
                    selector.Size = new Size(240, height);
                    using (var bitmap = new Bitmap(240, height))
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        RectangleF bounds = selector.Inner.Region.GetBounds(graphics);
                        bounds.Offset(selector.Inner.Left, selector.Inner.Top);
                        Check(bounds.Top >= 4 && bounds.Bottom <= height - 4, "Native child covers shell border");
                        Check(bounds.Left >= 10 && bounds.Right <= 240 - 34, "Native arrow exposed");
                    }
                }
            }
        }

        private static void TestReplyArrays()
        {
            string error;
            string reply = AiClient.ExtractContent("{\"choices\":[{\"message\":{\"content\":\"可用\"}}]}", out error);
            Check(error == null && reply == "可用", "Chat completion reply lost");
            Check(AiClient.AsArray(new System.Collections.ArrayList { "a", "b" }).Length == 2, "ArrayList unsupported");
            Check(AiClient.AsArray(new List<object> { "a" }).Length == 1, "List unsupported");
            Check(AiClient.AsArray("not-array") == null, "String accepted as array");
        }

        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Run("Workspace geometry across window sizes and AI collapse",WorkspaceLayoutTests.Editor);
                Run("Main window opens settings directly and reuses one window",WorkspaceLayoutTests.MainEntry);
                Run("Partial eraser restores source alpha and supports history",EraserTests.Run);
                Run("Contextual tool properties keep relevant parameters visible",TrialExperienceTests.Properties);
                Run("Image save state tracks successful and failed writes",TrialExperienceTests.SaveState);
                Run("Protected replacement text and mask brushes",ImageEditTests.BrushAndText);
                Run("Text preflight and adjustable mask brushes",ImageEditTests.PreflightAndBrushSize);
                Run("Toolbar invalidation and exclusive drawing AI mask state",ImageEditTests.ExclusiveToolbar);
                Run("Arrowheads stay pointed in four directions and widths",ImageEditTests.SharpArrows);
                Run("Direct arrow dragging while drawing at multiple zoom levels",ImageEditTests.DirectArrowDragging);
                Run("Photoshop circle center constraint geometry",EditorExperienceTests.Constraints);
                Run("Freehand privacy effects pixels history and hit testing",EditorExperienceTests.BrushPixels);
                Run("Arrow endpoint editing and keyboard nudges",EditorExperienceTests.EndpointsAndNudge);
                Run("Explicit save receipt collapse reopen and animation reversal",SettingsFeedbackTests.FoldAndRestore);
                Run("Save buttons on every settings page and no implicit language history save",SettingsFeedbackTests.MainPageButtons);
                Run("Invalid image service save retains editor and never shows success",SettingsFeedbackTests.InvalidDialogSave);
                Run("Small settings windows preserve full body and visible save buttons",SettingsFeedbackTests.SmallWindowContent);
                Run("Settings page saves isolate unrelated drafts",SettingsFeedbackTests.PageIsolation);
                Run("Save directory preflight and precise effect receipts",SettingsFeedbackTests.SaveValidationAndMessages);
                Run("Dirty settings revert and independent save state",SettingsFeedbackTests.DraftState);
                Run("Magnified result before after and reject workflow",ExperiencePreviewTests.Run);
                Run("Image edit HTTP adapter and safe service errors",ImageEditTransportTests.Run);
                Run("Image editing mask compositing protects pixels",ImageEditTests.MaskAndMerge);
                Run("Image editing multipart response and independent config",ImageEditTests.Protocol);
                Run("Generated image undo restores source and annotations",ImageEditTests.History);
                Run("AI region mouse mapping at multiple zoom levels",AiRegionTests.MouseCoordinates);
                Run("AI regional PNG and exact pixel mapping",AiRegionTests.UploadAndMapping);
                Run("AI regional invalid batches reject atomically",AiRegionTests.RejectInvalid);
                Run("AI regional output bounds and undo redo",AiRegionTests.ClipAndUndo);
                Run("Multi-display coordinates and DPI target sizes", ReleaseReadinessTests.Geometry);
                Run("Release version and product metadata", ReleaseReadinessTests.Metadata);
                Run("Language fallback and English offline help", ReleaseReadinessTests.Language);
                Run("Save actions, settings migration and text-only OCR",ExperienceTests.Run);
                if (args.Length>0 && args[0]=="--stress") return ResourceStress.Run();
                Run("Service error content is omitted from dialogue", PrivacyTests.ServiceErrors);
                Run("AI endpoints require secure remote transport", PrivacyTests.Endpoints);
                Run("Attachments opt in and API keys remain encrypted", PrivacyTests.Defaults);
                Run("Capture move, resize, crossing and bounds", CaptureModeTests.Geometry);
                Run("Three capture hotkeys and standalone PrintScreen", CaptureModeTests.Hotkeys);
                Run("Adjustable waits for confirmation, quick captures on release", CaptureModeTests.Overlay);
                Run("Chat cancellation rejects queued stale replies", UserUpgradeTests.Cancellation);
                Run("Help and preview open in both themes without transparency errors", UserUpgradeTests.DialogBackgrounds);
                Run("Offline help search and first-run review default", UserUpgradeTests.Guide);
                Run("Cancellation concurrency and timeout checks", delegate { Check(AiCancellationChecks.Main()==0,"Cancellation checks failed"); });
                Run("Selected annotation move, resize, style and delete undo", TestSelectionEdits);
                Run("AI comparison and cancellation prevent stale application", TestAiReview);
                Run("Reply JSON arrays and list types", TestReplyArrays);
                Run("Glass rendering handles every collapse height", TestGlassBounds);
                Run("Selector child never covers rounded shell", TestSelectorClip);
                Run("JSON prompt contains required keyword", delegate { Check(AiClient.JsonPrompt("你好").ToLowerInvariant().Contains("json"), "Missing json keyword"); Check(AiClient.JsonPrompt(null).Contains("JSON"), "Null prompt"); });
                Run("TLS policy replaces obsolete protocols", TestTlsPolicy);
                Run("Preset models, protocols and credential isolation", TestPresets);
                Run("Model discovery parses and deduplicates IDs", TestModelDiscovery);
                Run("Solid redaction hides source pixels", TestSolidRedaction);
                Run("Provider defaults, validation and snapshots", TestProviders);
                Run("Internal editing skill rejects malformed operations", TestInternalSkill);
                Run("Theme palette and native controls switch both ways", TestThemes);
                Run("History budget preserves retained undo/redo", TestHistoryBudget);
                Run("Drag retains start across direction changes", TestDragAnchor);
                Run("Editor input geometry remains current before paint and after resize", TestInputGeometry);
                Run("AI batch undo and redo", TestAiGroup);
                Run("Mixed AI crop response is one undo step", TestAiResponseHistory);
                Run("AI reply without ops preserves redo", TestAiReadOnly);
                Run("AI editor reserves canvas and lays out all entries", TestAiLayout);
                Run("Screenshot conversation context is bounded", TestAiContext);
                Run("AI pending response blocks final output", TestAiConfirmation);
                Run("AI crop executes without annotation count", TestAiCrop);
                Run("Crop undo restores annotations; redo and second crop", TestCropHistory);
                Run("Reset undo and redo restores source and annotations", TestResetHistory);
                Run("Multiple overlapping mosaics export consistently", TestMosaic);
                Run("Empty chat guidance stays outside history", TestEmptyChat);
                Run("Text Escape cancels input, preserving editor", TestTextEscape);
                Run("Recovered animation curve", TestCurve);
                Run("Sidebar motion numerical checks", SidebarMotionTests.Run);
                Run("Disabled glass button does not click", TestDisabledGlassButton);
                Run("Glass background does not reprint parent", TestGlassBackground);
                Run("Glass hover repaint is stable and keyboard works", TestGlassRepaint);
                Run("Text drawing preserves clipping and translation", TestTextBounds);
                Run("Crop guide keeps inside and outside pixels", TestTransparentGuide);
                Run("Text input stays outside canvas and commits", TestTransparentText);
                Console.WriteLine("RESULT: " + passed + " passed, " + failed + " failed regression tests");
                return failed==0 ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void Run(string name, Action test)
        {
            try { test(); passed++; Console.WriteLine("PASS: " + name); }
            catch(Exception ex) { failed++; Console.Error.WriteLine("FAIL: " + name + "\n" + ex); }
        }

        private static object Call(object obj, string name, params object[] args)
        {
            return obj.GetType().GetMethod(name, Private).Invoke(obj, args);
        }

        private static object Field(object obj, string name)
        {
            return obj.GetType().GetField(name, Private).GetValue(obj);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static Bitmap Pattern()
        {
            Bitmap bmp = new Bitmap(64, 48);
            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                    bmp.SetPixel(x, y, Color.FromArgb(x * 3, y * 5, (x + y) * 2));
            return bmp;
        }

        private static EditorForm Editor()
        {
            EditorForm editor = new EditorForm(null, Pattern(), new Config());
            editor.ClientSize = new Size(700, 450);
            return editor;
        }

        private static void AddRect(EditorForm editor)
        {
            Shape shape = new Shape();
            shape.Kind = Shape.K.Rect;
            shape.Color = Color.Red;
            shape.Width = 3;
            shape.Rect = new RectangleF(5, 5, 30, 22);
            Call(editor, "AddShapeTracked", shape);
        }

        private static void Equal(Bitmap expected, Bitmap actual, string reason)
        {
            Check(expected.Size == actual.Size, reason + ": dimensions");
            for (int y = 0; y < expected.Height; y++)
                for (int x = 0; x < expected.Width; x++)
                    Check(expected.GetPixel(x, y).ToArgb() == actual.GetPixel(x, y).ToArgb(), reason + ": pixels");
        }

        private static void TestAiResponseHistory()
        {
            using (EditorForm editor = Editor())
            {
                AddRect(editor);
                Call(editor, "CommitCrop", new RectangleF(2, 2, 50, 36));
                AddRect(editor);
                int count = ((List<UndoStep>)Field(editor, "undoSteps")).Count;
                using (Bitmap before = editor.RenderResult())
                {
                    AiReply reply = new AiReply();
                    reply.Say = "已标注并裁剪";
                    reply.Ops.Add(new AiOp { Kind = "rect", X = 4, Y = 4, W = 15, H = 12 });
                    reply.Ops.Add(new AiOp { Kind = "crop", X = 3, Y = 3, W = 30, H = 20 });
                    Call(editor, "AiHandleReply", reply, null);
                    Check(editor.SourceW == 30, "Response crop was not applied");
                    Check(((List<UndoStep>)Field(editor, "undoSteps")).Count == count + 1, "One response needs one undo");
                    using (Bitmap after = editor.RenderResult())
                    {
                        Call(editor, "DoUndo");
                        using (Bitmap restored = editor.RenderResult()) Equal(before, restored, "AI undo restores previous crop and annotations");
                        Call(editor, "DoRedo");
                        using (Bitmap restored = editor.RenderResult()) Equal(after, restored, "AI redo restores result");
                    }
                    Check(((TextBox)Field(editor, "aiLogBox")).Text.Contains(reply.Say), "Reply missing from conversation");
                }
            }
        }

        private static void TestAiReadOnly()
        {
            using (EditorForm editor = Editor())
            {
                AddRect(editor);
                Call(editor, "DoUndo");
                Call(editor, "AiHandleReply", new AiReply { Say = "仅说明" }, null);
                Check(((List<UndoStep>)Field(editor, "undoSteps")).Count == 0, "Chat must not add undo");
                Check(((List<UndoStep>)Field(editor, "undone")).Count == 1, "Chat must preserve redo");
            }
        }

        private static void TestAiLayout()
        {
            using (EditorForm editor = Editor())
            {
                Rectangle view = (Rectangle)editor.GetType().GetProperty("ViewRect", Private).GetValue(editor, null);
                Panel panel = (Panel)Field(editor, "aiBody");
                Check(view.Right <= panel.Left && view.Top == panel.Top, "AI must not overlap canvas");
                foreach (Control control in editor.Controls)
                    if (control.Text == "AI" || control.Text == "OCR")
                        Check(control.Width > 0 && control.Left >= 0 && control.Right <= editor.ClientSize.Width, "AI toolbar entry hidden");
                int width = view.Width;
                Call(editor, "CloseAiPanel");
                view = (Rectangle)editor.GetType().GetProperty("ViewRect", Private).GetValue(editor, null);
                Check(view.Width > width, "Collapsing AI must release canvas space");
            }
        }

        private static void TestAiContext()
        {
            List<string> history = new List<string>();
            for (int i = 0; i < 12; i++) history.Add("message-" + i + new string('x', 3000));
            string prompt = EditorAiSession.BuildPrompt(history, "latest-request");
            Check(!prompt.Contains("message-0") && prompt.Contains("message-11"), "Context must use recent conversation");
            Check(prompt.Length < 13000 && prompt.EndsWith("latest-request"), "Context must be bounded with current request last");
        }

        private static void TestAiConfirmation()
        {
            using (EditorForm editor = Editor())
            {
                editor.GetType().GetField("aiBusy", Private).SetValue(editor, true);
                Call(editor, "AcceptAndClose", true);
                Check(editor.ExitMode == EditorForm.EditorExit.Cancel && !editor.IsDisposed, "Pending AI must not confirm output");
                Call(editor, "AcceptAndClose", false);
                Check(editor.ExitMode == EditorForm.EditorExit.Cancel, "Pending AI must not copy output");
            }
        }

        private static void TestAiGroup()
        {
            using (EditorForm editor = Editor())
            {
                List<AiOp> ops = new List<AiOp>();
                for (int i = 0; i < 5; i++)
                    ops.Add(new AiOp { Kind = "rect", X = 3 + i * 3, Y = 3, W = 20, H = 20 });
                Check((int)Call(editor, "AiApplyOps", ops) == 5, "Expected five annotations");
                Call(editor, "DoUndo");
                Check(((List<Shape>)Field(editor, "shapes")).Count == 0, "One undo must remove the batch");
                Call(editor, "DoRedo");
                Check(((List<Shape>)Field(editor, "shapes")).Count == 5, "Redo must restore the batch");
            }
        }

        private static void TestAiCrop()
        {
            using (EditorForm editor = Editor())
            {
                List<AiOp> ops = new List<AiOp> { new AiOp { Kind = "crop", X = 4, Y = 3, W = 30, H = 20 } };
                Check((int)Call(editor, "AiApplyOps", ops) == 0, "Crop must not count as annotation");
                Check(editor.SourceW == 30 && editor.SourceH == 20, "Crop must change source dimensions");
                Call(editor, "DoUndo");
                Check(editor.SourceW == 64 && editor.SourceH == 48, "AI crop undo dimensions");
            }
        }

        private static void TestCropHistory()
        {
            using (EditorForm editor = Editor())
            {
                AddRect(editor);
                using (Bitmap before = editor.RenderResult())
                {
                    Call(editor, "CommitCrop", new RectangleF(3, 3, 40, 30));
                    using (Bitmap first = editor.RenderResult())
                    {
                        Call(editor, "DoUndo");
                        Check(((List<Shape>)Field(editor, "shapes")).Count == 1, "Undo crop lost annotation objects");
                        using (Bitmap restored = editor.RenderResult()) Equal(before, restored, "Undo crop");
                        Call(editor, "DoRedo");
                        using (Bitmap redone = editor.RenderResult()) Equal(first, redone, "Redo crop");
                        Call(editor, "CommitCrop", new RectangleF(2, 2, 20, 16));
                        using (Bitmap expected = first.Clone(new Rectangle(2, 2, 20, 16), first.PixelFormat))
                        using (Bitmap second = editor.RenderResult()) Equal(expected, second, "Second crop uses current source");
                        Call(editor, "DoUndo");
                        using (Bitmap restored = editor.RenderResult()) Equal(first, restored, "Undo second crop");
                        Call(editor, "EnsureComposite");
                        Check(Field(editor, "composite") != null, "Preview after crop");
                    }
                }
            }
        }

        private static void TestResetHistory()
        {
            using (EditorForm editor = Editor())
            {
                AddRect(editor);
                Call(editor, "CommitCrop", new RectangleF(3, 3, 40, 30));
                AddRect(editor);
                using (Bitmap before = editor.RenderResult())
                {
                    Call(editor, "ResetAll");
                    Check(editor.SourceW == 64, "Reset uses original source");
                    Call(editor, "DoUndo");
                    using (Bitmap restored = editor.RenderResult()) Equal(before, restored, "Undo reset");
                    Check(((List<Shape>)Field(editor, "shapes")).Count == 1, "Undo reset restores editable shapes");
                    Call(editor, "DoRedo");
                    using (Bitmap original = Pattern())
                    using (Bitmap reset = editor.RenderResult()) Equal(original, reset, "Redo reset");
                    Call(editor, "DoUndo");
                    AddRect(editor);
                    Check(((List<UndoStep>)Field(editor, "undone")).Count == 0, "New operation clears redo snapshots");
                }
            }
        }

        private static void TestMosaic()
        {
            using (EditorForm editor = Editor())
            {
                Shape a = new Shape { Kind = Shape.K.Mosaic, Rect = new RectangleF(8, 8, 16, 16), Block = 8 };
                Call(editor, "AddShapeTracked", a);
                using (Bitmap first = editor.RenderResult())
                {
                    Check(first.GetPixel(8, 8).ToArgb() == Color.FromArgb(34, 57, 46).ToArgb(), "Known block average");
                    long r = 0, g = 0, b = 0;
                    for (int y = 12; y < 20; y++)
                        for (int x = 12; x < 20; x++)
                        {
                            Color c = first.GetPixel(x, y); r += c.R; g += c.G; b += c.B;
                        }
                    Color expected = Color.FromArgb((int)(r / 64), (int)(g / 64), (int)(b / 64));
                    Shape overlap = new Shape { Kind = Shape.K.Mosaic, Rect = new RectangleF(12, 12, 8, 8), Block = 8 };
                    Call(editor, "AddShapeTracked", overlap);
                    Shape c2 = new Shape { Kind = Shape.K.Mosaic, Rect = new RectangleF(32, 16, 16, 16), Block = 8 };
                    Call(editor, "AddShapeTracked", c2);
                    using (Bitmap result = editor.RenderResult())
                    using (Bitmap again = editor.RenderResult())
                    {
                        Check(result.GetPixel(12, 12).ToArgb() == expected.ToArgb(), "Overlapping mosaic samples preceding result");
                        Check(result.GetPixel(32, 16).ToArgb() == Color.FromArgb(106, 97, 110).ToArgb(), "Second region average");
                        Equal(result, again, "Repeated mosaic export");
                    }
                    Call(editor, "EnsureComposite");
                }
            }
        }

        private static void TestEmptyChat()
        {
            AiChat.Clear();
            using (ChatPanel panel = new ChatPanel(null, new Config()))
            {
                ChatMessageView log = (ChatMessageView)Field(panel, "logBox");
                Check(log.Controls.Count == 1 && log.Controls[0] is Label, "Missing empty guidance");
                Check(AiChat.Count == 0 && AiChat.Transcript == "", "Guidance polluted history");
                AiChat.Append("你", "AI：用户消息中的冒号不改变角色");
                AiChat.Append("AI", "reply");
                Call(panel, "RefreshLog");
                Check(AiChat.Messages[0].Role == "你" && AiChat.Messages[1].Role == "AI", "Incorrect roles");
                Check(log.Controls.Count == 2, "Missing message rows");
                log.Width = 360;
                Check(log.Controls[0].Left > log.Controls[1].Left, "User message is not right aligned");
                AiChat.Clear(); Call(panel, "RefreshLog");
                Check(log.Controls.Count == 1 && log.Controls[0] is Label, "Guidance did not return");
            }
        }

        private static void TestTextEscape()
        {
            using (EditorForm editor = Editor())
            {
                AddRect(editor);
                editor.Show();
                Application.DoEvents();
                Call(editor, "StartTextInput", new PointF(8, 8));
                Application.DoEvents();
                TextBox input = editor.ActiveControl as TextBox;
                Check(input != null, "Text input not focused");
                input.Text = "discard";
                editor.TestProcessKey(Keys.Escape);
                Application.DoEvents();
                Check(input.IsDisposed, "Escape did not cancel text input");
                Check(!editor.IsDisposed && editor.DialogResult == DialogResult.None, "Escape closed editor");
                Check(((List<Shape>)Field(editor, "shapes")).Count == 1, "Escape lost existing annotations");
                editor.Hide();
            }
        }

        private static void TestSelectionEdits()
        {
            using (EditorForm editor=Editor())
            {
                Call(editor,"EnsureZoomInit"); Call(editor,"EnsureComposite");
                Shape original=new Shape { Kind=Shape.K.Rect, Rect=new RectangleF(10,10,20,15), Color=Color.Red, Width=2 };
                Call(editor,"AddShapeTracked",original);
                editor.GetType().GetField("tool",Private).SetValue(editor,Shape.K.Select);
                Call(editor,"BeginSelectionDrag",new PointF(15,15)); Call(editor,"MoveSelection",new PointF(20,20)); Call(editor,"FinishSelectionDrag");
                Check(original.Rect.X==15 && original.Rect.Y==15,"Move lost coordinates");
                Call(editor,"DoUndo"); Check(((List<Shape>)Field(editor,"shapes"))[0].Rect.X==10,"Move undo failed");
                Call(editor,"DoRedo"); Shape moved=((List<Shape>)Field(editor,"shapes"))[0];
                editor.GetType().GetField("selectedShape",Private).SetValue(editor,moved);
                Call(editor,"BeginSelectionDrag",new PointF(moved.Rect.Right,moved.Rect.Bottom));
                Call(editor,"MoveSelection",new PointF(moved.Rect.Right+5,moved.Rect.Bottom+4)); Call(editor,"FinishSelectionDrag");
                Check(moved.Rect.Width>20,"Resize failed");
                editor.GetType().GetField("color",Private).SetValue(editor,Color.Blue); Call(editor,"EditSelectedStyle");
                Check(moved.Color==Color.Blue,"Selected color did not change");
                Call(editor,"DeleteSelected"); Check(((List<Shape>)Field(editor,"shapes")).Count==0,"Delete failed");
                Call(editor,"DoUndo"); Check(((List<Shape>)Field(editor,"shapes")).Count==1,"Delete undo failed");
            }
        }
        private static void TestAiReview()
        {
            using (EditorForm editor=Editor())
            {
                editor.GetType().GetField("aiBusy",Private).SetValue(editor,true);
                editor.GetType().GetField("aiCancellation",Private).SetValue(editor,new AiRequestCancellation());
                int before=(int)Field(editor,"aiRequestId");
                Call(editor,"CancelAiRequest"); Check(!(bool)Field(editor,"aiBusy"),"Cancel kept editor busy");
                Check((int)Field(editor,"aiRequestId")>before,"Cancel did not invalidate reply ID");
                editor.GetType().GetField("aiBefore",Private).SetValue(editor,Pattern());
                editor.GetType().GetField("compareBefore",Private).SetValue(editor,true);
                Call(editor,"AcceptAndClose",false); Check(editor.DialogResult==DialogResult.None,"Original comparison allowed misleading output");
                Call(editor,"ClearAiComparison"); Check(Field(editor,"aiBefore")==null,"Comparison bitmap was retained");
            }
        }
        private static void TestModelDiscovery()
        {
            List<string> models=AiProviderDiscovery.ParseModels("{\"data\":[{\"id\":\"vision-a\"},{\"id\":\"vision-a\"},{\"id\":\"vision-b\"}]}");
            Check(models.Count==2,"Models were not deduplicated");
            Check(AiProviderDiscovery.ModelsUrl(new AiProvider { Endpoint="https://example.com/v1" })=="https://example.com/v1/models","Models URL incorrect");
        }
        private static void TestSolidRedaction()
        {
            using (EditorForm editor=Editor())
            {
                Call(editor,"AddShapeTracked",new Shape { Kind=Shape.K.Redact, Rect=new RectangleF(5,5,20,20), Color=Color.Black, Width=2 });
                using (Bitmap result=editor.RenderResult()) Check(result.GetPixel(10,10).ToArgb()==Color.Black.ToArgb(),"Solid cover exposes pixels");
                Call(editor,"DoUndo"); using (Bitmap result=editor.RenderResult()) Check(result.GetPixel(10,10).ToArgb()!=Color.Black.ToArgb(),"Redaction undo failed");
            }
        }

        private static void TestProviders()
        {
            AiProvider provider = new AiProvider { Id = "local", Endpoint = "http://localhost:11434/v1/chat/completions" };
            provider.Models.Add("model-a"); provider.Models.Add("model-b"); provider.SelectedModel = "model-b";
            Check(provider.FirstModel == "model-b", "Selected model ignored");
            Check(AiProvider.Deserialize(provider.Serialize()).FirstModel == "model-b", "Model selection not persisted");
            AiProvider copy = provider.Snapshot();
            provider.Models.Clear();
            Check(copy.Models.Count == 2, "Snapshot shares mutable model list");
            Check(copy.ValidationError() == null, "Local service incorrectly requires key");
            copy.Endpoint = "https://localhost.evil.example/v1/chat/completions";
            Check(copy.ValidationError() != null, "Remote host incorrectly bypasses key check");
            Check(AiProvider.NormalizeEndpoint("https://example.com/v1/", false) == "https://example.com/v1/chat/completions", "Base URL not normalized");
            Check(AiProvider.NormalizeEndpoint("https://example.com", true) == "https://example.com/v1/messages", "Anthropic base URL not normalized");
        }

        private static void TestInternalSkill()
        {
            Check(AiClient.SystemPrompt(true).Contains("最多 64 条操作"), "Internal skill is not included in request");
            using (EditorForm editor = Editor())
            {
                bool rejected = false;
                try { Call(editor, "AiApplyOps", new List<AiOp> { new AiOp { Kind = "rect", X = float.NaN } }); }
                catch (TargetInvocationException ex) { rejected = ex.InnerException is InvalidOperationException; }
                Check(rejected, "Invalid numeric operation was accepted");
                Check(((List<Shape>)Field(editor, "shapes")).Count == 0, "Invalid request partially changed image");
            }
        }

        private static void TestThemes()
        {
            Dsh.ApplyTheme(false, null);
            Color darkCanvas = Dsh.Canvas, darkInput = Dsh.GlassFillStrong, darkInk = Dsh.TextPrimary;
            using (Panel panel = new Panel())
            using (TextBox input = new TextBox())
            {
                panel.BackColor = darkCanvas;
                input.BackColor = darkInput;
                input.ForeColor = darkInk;
                panel.Controls.Add(input);
                Dsh.ApplyTheme(true, panel);
                Check(Dsh.Canvas.GetBrightness() > 0.85f, "Light surface is too dark");
                Check(Dsh.TextPrimary.GetBrightness() < 0.3f, "Light text contrast is low");
                Check(panel.BackColor == Dsh.Canvas && input.BackColor == Dsh.GlassFillStrong && input.ForeColor == Dsh.TextPrimary, "Native controls not themed");
                Dsh.ApplyTheme(false, panel);
                Check(panel.BackColor == darkCanvas && input.BackColor == darkInput && input.ForeColor == darkInk, "Dark palette restoration failed");
            }
        }

        private static void TestHistoryBudget()
        {
            using (EditorForm editor = Editor())
            {
                for (int i = 0; i < 60; i++)
                    Call(editor, "AddShapeTracked", new Shape { Kind = Shape.K.Rect, Rect = new RectangleF(i, 1, 2, 2) });
                Check(((List<UndoStep>)Field(editor, "undoSteps")).Count == 40, "History grew beyond 40 steps");
                for (int i = 0; i < 40; i++) Call(editor, "DoUndo");
                Check(((List<Shape>)Field(editor, "shapes")).Count == 20, "Undo removed evicted edits");
                for (int i = 0; i < 40; i++) Call(editor, "DoRedo");
                Check(((List<Shape>)Field(editor, "shapes")).Count == 60, "Redo lost retained edits");
                List<UndoStep> history = (List<UndoStep>)Field(editor, "undoSteps");
                history.Add(new UndoStep { Snapshot = new Bitmap(4000, 4000) });
                history.Add(new UndoStep { Snapshot = new Bitmap(4000, 4000) });
                Call(editor, "BoundHistory");
                Check(history.Count == 1, "Bitmap history exceeded memory budget");
            }
        }

        private static void TestInputGeometry()
        {
            using(EditorForm editor=Editor())
            {
                Call(editor,"EnsureZoomInit");
                foreach(Size size in new Size[] { new Size(700,450),new Size(940,620),new Size(640,480) })
                {
                    editor.ClientSize=size;
                    foreach(float scale in new float[] { 0.5f,1f,2f })
                    {
                        editor.GetType().GetField("zoom",Private).SetValue(editor,scale);
                        editor.GetType().GetField("commitVersion",Private).SetValue(editor,(int)Field(editor,"commitVersion")+1);
                        RectangleF before=(RectangleF)Call(editor,"ImageRectToScreen",new RectangleF(10,10,20,20));
                        Call(editor,"EnsureComposite");
                        RectangleF after=(RectangleF)Call(editor,"ImageRectToScreen",new RectangleF(10,10,20,20));
                        Check(before==after,"Painting changed input coordinates");
                        Rectangle view=(Rectangle)editor.GetType().GetProperty("ViewRect",Private).GetValue(editor,null);
                        Check(view.Contains(Point.Round(new PointF(before.X,before.Y))),"Image anchor lies outside canvas");
                    }
                }
            }
        }

        private static void TestDragAnchor()
        {
            using (EditorForm editor = Editor())
            {
                Call(editor, "EnsureZoomInit");
                RectangleF screen = (RectangleF)Call(editor, "ImageRectToScreen", new RectangleF(10, 10, 10, 10));
                editor.GetType().GetField("tool", Private).SetValue(editor, Shape.K.Rect);
                Call(editor, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, (int)screen.X, (int)screen.Y, 0));
                PointF anchor = (PointF)Field(editor, "dragAnchor");
                Call(editor, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, (int)screen.X - 3, (int)screen.Y - 3, 0));
                Call(editor, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, (int)screen.Right, (int)screen.Bottom, 0));
                Shape draft = (Shape)Field(editor, "draft");
                Check(Math.Abs(draft.Rect.X - anchor.X) < 0.01f && Math.Abs(draft.Rect.Y - anchor.Y) < 0.01f, "Direction change moved anchor");
                Call(editor, "OnMouseUp", new MouseEventArgs(MouseButtons.Right, 1, (int)screen.Right, (int)screen.Bottom, 0));
                Check(Field(editor, "draft") != null, "Right mouse release committed left drag");
            }
        }

        private static void TestTransparentGuide()
        {
            using (EditorForm editor = Editor())
            using (Bitmap bitmap = new Bitmap(editor.ClientSize.Width, editor.ClientSize.Height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                Call(editor, "EnsureZoomInit");
                Call(editor, "EnsureComposite");
                RectangleF crop = new RectangleF(10, 10, 25, 20);
                editor.GetType().GetField("cropRect", Private).SetValue(editor, crop);
                RectangleF screen = (RectangleF)Call(editor, "ImageRectToScreen", crop);
                graphics.Clear(Color.Orange);
                Call(editor, "DrawCropGuide", graphics);
                Check(bitmap.GetPixel((int)(screen.X + screen.Width / 2), (int)(screen.Y + screen.Height / 2)).ToArgb() == Color.Orange.ToArgb(), "Crop interior is opaque");
                Rectangle view = (Rectangle)editor.GetType().GetProperty("ViewRect", Private).GetValue(editor, null);
                Check(bitmap.GetPixel(5, view.Top + 5).ToArgb() == Color.Orange.ToArgb(), "Crop guide darkens outside");
            }
        }

        private static void TestTransparentText()
        {
            using (EditorForm editor = Editor())
            {
                Call(editor, "StartTextInput", new PointF(5, 5));
                TextBox input = (TextBox)Field(editor, "annotationInput");
                Rectangle view = (Rectangle)editor.GetType().GetProperty("ViewRect", Private).GetValue(editor, null);
                Check(input.Bottom <= view.Top, "Text input must not cover canvas");
                input.Text = "live text";
                ((EventHandler)input.Tag)(null, EventArgs.Empty);
                Check(Field(editor, "annotationInput") == null, "Input did not close");
                Check(((List<Shape>)Field(editor, "shapes")).Count == 1, "Text did not commit");
                Call(editor, "DoUndo");
                Check(((List<Shape>)Field(editor, "shapes")).Count == 0, "Text undo failed");
            }
        }

        private sealed class PaintProbe : Panel
        {
            internal int PaintCount;
            protected override void OnPaint(PaintEventArgs e) { PaintCount++; base.OnPaint(e); }
            protected override void OnPaintBackground(PaintEventArgs e) { PaintCount++; base.OnPaintBackground(e); }
        }

        private static void TestGlassBackground()
        {
            using (PaintProbe parent = new PaintProbe())
            using (GlassButton button = new GlassButton())
            using (Bitmap bitmap = new Bitmap(80, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                parent.BackColor = Color.FromArgb(22, 22, 25);
                button.Size = bitmap.Size;
                parent.Controls.Add(button);
                parent.PaintCount = 0;
                Call(button, "OnPaintBackground", new PaintEventArgs(graphics, button.ClientRectangle));
                Check(parent.PaintCount == 0, "Button must not invoke parent painting");
                Check(bitmap.GetPixel(10, 10).ToArgb() == parent.BackColor.ToArgb(), "Backing surface color mismatch");
            }
        }

        private static void TestGlassRepaint()
        {
            using (PaintProbe parent = new PaintProbe())
            using (GlassButton button = new GlassButton())
            using (Bitmap bitmap = new Bitmap(140, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                parent.BackColor = Color.FromArgb(22, 22, 25);
                parent.Controls.Add(button);
                button.Size = bitmap.Size;
                button.Text = "标注重点";
                Call(button, "OnMouseEnter", EventArgs.Empty);
                parent.PaintCount = 0;
                Call(button, "OnPaint", new PaintEventArgs(graphics, button.ClientRectangle));
                using (Bitmap first = (Bitmap)bitmap.Clone())
                {
                    for (int i = 0; i < 10; i++) Call(button, "OnPaint", new PaintEventArgs(graphics, button.ClientRectangle));
                    Equal(first, bitmap, "Repeated hover paint changed pixels");
                }
                Check(parent.PaintCount == 0, "Hover paint invoked parent");
                int clicks = 0;
                button.Click += delegate { clicks++; };
                Call(button, "OnKeyDown", new KeyEventArgs(Keys.Space));
                Call(button, "OnKeyUp", new KeyEventArgs(Keys.Space));
                Check(clicks == 1, "Space did not click once");
                button.Enabled = false;
                button.PerformClick();
                Check(clicks == 1, "Disabled button clicked");
            }
        }

        private static void TestTextBounds()
        {
            using (Bitmap bitmap = new Bitmap(180, 60))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (Font font = Dsh.Body)
            {
                graphics.Clear(Color.White);
                graphics.SetClip(new Rectangle(0, 0, 20, 40));
                Dsh.DrawText(graphics, "TEXTTEXTTEXT", font, Color.Black, new Rectangle(0, 0, 170, 40), ContentAlignment.TopLeft);
                for (int y = 0; y < 60; y++)
                    for (int x = 20; x < 180; x++)
                        Check(bitmap.GetPixel(x, y).ToArgb() == Color.White.ToArgb(), "Text escaped clip");
                graphics.ResetClip();
                graphics.Clear(Color.White);
                graphics.TranslateTransform(60, 0);
                Dsh.DrawText(graphics, "TEST", font, Color.Black, new Rectangle(0, 0, 100, 40), ContentAlignment.TopLeft);
                bool ink = false;
                for (int y = 0; y < 60; y++)
                    for (int x = 0; x < 180; x++)
                        if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb())
                        { Check(x >= 60, "Text ignored translated origin"); ink = true; }
                Check(ink, "Translation test produced no text");
                graphics.ResetTransform(); graphics.ResetClip(); graphics.Clear(Color.White);
                graphics.TranslateTransform(60,0); graphics.ScaleTransform(2,1);
                graphics.SetClip(new Rectangle(0,0,10,40));
                RectangleF clip=graphics.ClipBounds;
                float[] transform;
                using(var matrix=graphics.Transform) transform=matrix.Elements;
                Dsh.DrawText(graphics,"TEXTTEXTTEXT",font,Color.Black,new Rectangle(0,0,100,40),ContentAlignment.TopLeft);
                ink=false;
                for(int y=0;y<60;y++) for(int x=0;x<180;x++)
                    if(bitmap.GetPixel(x,y).ToArgb()!=Color.White.ToArgb())
                    { Check(x>=60 && x<80 && y<40,"Transformed text escaped the existing clip"); ink=true; }
                Check(ink,"Transformed clip produced no text");
                Check(graphics.ClipBounds==clip,"Drawing changed caller clipping");
                using(var matrix=graphics.Transform) for(int i=0;i<6;i++) Check(matrix.Elements[i]==transform[i],"Drawing changed caller transform");
            }
        }

        private static void TestDisabledGlassButton()
        {
            using (DshButton button = new DshButton("disabled"))
            {
                bool clicked = false;
                button.Click += delegate { clicked = true; };
                button.Enabled = false;
                button.PerformClick();
                Check(!clicked, "Disabled button must not invoke click");
            }
        }

        private static void TestCurve()
        {
            float[] curve = new float[] { 0.4f, 0, 0.2f, 1 };
            Check(MainForm.CubicBezier(curve, 0) == 0 && MainForm.CubicBezier(curve, 1) == 1, "Curve endpoints");
            Check(Math.Abs(MainForm.CubicBezier(curve, 0.5f) - 0.775561f) < 0.00001f, "Curve midpoint");
        }
    }
}
