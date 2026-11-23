using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

// Renders a small Markdown subset into UI Toolkit elements.
//
// Block syntax:  # / ## / ### headings, "- " or "* " bullets, "1. " numbered items,
//                "> " callouts, ``` fenced code, "---" divider, blank-line separated paragraphs,
//                images:  ![caption](relative/path.png)  or  ![caption](path.png){width=400}
// Inline syntax: **bold**, *italic*, `code`.
// Topic links: put "[Title](topic:other-topic-id)" alone on a line to get a clickable button.
//   (Inline occurrences inside a sentence are only coloured, not clickable.)
//
// Image paths are relative to Assets/Resources/HelpContent/Images/ (extension optional).
public static class MarkdownView
{
    public const string ImageRoot = "HelpContent/Images/";
    const string Accent = "#22D3EE";
    const float DefaultMaxImageWidth = 620f;

    static readonly Regex ImageRx = new(@"^!\[(.*?)\]\((.+?)\)(?:\{width=(\d+)\})?\s*$");
    static readonly Regex NumberedRx = new(@"^(\d+)\.\s+(.*)$");
    static readonly Regex CodeRx = new(@"`([^`]+)`");
    static readonly Regex BoldRx = new(@"\*\*(.+?)\*\*");
    static readonly Regex ItalicRx = new(@"(?<![\*\w])\*(?!\s)(.+?)(?<!\s)\*(?![\*\w])");
    static readonly Regex TopicLineRx = new(@"^(?:[-*]\s+)?\[([^\]]+)\]\(topic:([^)]+)\)$");
    static readonly Regex TopicLinkRx = new(@"\[([^\]]+)\]\(topic:([^)]+)\)");
    static readonly Regex OtherLinkRx = new(@"\[([^\]]+)\]\((?!topic:)[^)]+\)");

    public static VisualElement Build(string markdown, Action<string> onTopicLink)
    {
        var container = new VisualElement();
        container.AddToClassList("help-md");

        string[] lines = (markdown ?? "").Replace("\r\n", "\n").Split('\n');
        var paragraph = new List<string>();
        int i = 0;

        void FlushParagraph()
        {
            if (paragraph.Count == 0) return;
            container.Add(MakeLabel(string.Join(" ", paragraph), "ds-body-1 help-md__p", onTopicLink));
            paragraph.Clear();
        }

        while (i < lines.Length)
        {
            string line = lines[i];
            string trimmed = line.Trim();

            if (trimmed.StartsWith("```"))
            {
                FlushParagraph();
                var sb = new StringBuilder();
                i++;
                while (i < lines.Length && !lines[i].Trim().StartsWith("```"))
                {
                    sb.AppendLine(lines[i]);
                    i++;
                }
                i++; // closing fence
                var code = new Label(sb.ToString().TrimEnd()) { enableRichText = false };
                code.AddToClassList("help-md__code");
                container.Add(code);
                continue;
            }

            if (trimmed.Length == 0) { FlushParagraph(); i++; continue; }

            Match img = ImageRx.Match(trimmed);
            if (img.Success)
            {
                FlushParagraph();
                container.Add(MakeImage(img.Groups[1].Value, img.Groups[2].Value, img.Groups[3].Value));
                i++;
                continue;
            }

            // A line that is only "[Title](topic:id)" becomes a clickable button to that topic.
            Match link = TopicLineRx.Match(trimmed);
            if (link.Success)
            {
                FlushParagraph();
                string topicId = link.Groups[2].Value.Trim();
                var btn = new Button(() => onTopicLink?.Invoke(topicId)) { text = link.Groups[1].Value };
                btn.AddToClassList("ds-btn");
                btn.AddToClassList("ds-btn--ghost");
                btn.AddToClassList("ds-btn--sm");
                btn.AddToClassList("help-md__link");
                container.Add(btn);
                i++;
                continue;
            }

            if (trimmed == "---" || trimmed == "***")
            {
                FlushParagraph();
                var hr = new VisualElement();
                hr.AddToClassList("help-md__hr");
                container.Add(hr);
                i++;
                continue;
            }

            if (trimmed.StartsWith("### ")) { FlushParagraph(); container.Add(MakeLabel(trimmed.Substring(4), "ds-h3 help-md__h3", onTopicLink)); i++; continue; }
            if (trimmed.StartsWith("## ")) { FlushParagraph(); container.Add(MakeLabel(trimmed.Substring(3), "ds-h2 help-md__h2", onTopicLink)); i++; continue; }
            if (trimmed.StartsWith("# ")) { FlushParagraph(); container.Add(MakeLabel(trimmed.Substring(2), "ds-h1 help-md__h1", onTopicLink)); i++; continue; }

            if (trimmed.StartsWith("> "))
            {
                FlushParagraph();
                var sb = new StringBuilder();
                while (i < lines.Length && lines[i].Trim().StartsWith(">"))
                {
                    sb.Append(lines[i].Trim().TrimStart('>').Trim()).Append(' ');
                    i++;
                }
                var box = new VisualElement();
                box.AddToClassList("help-md__callout");
                box.Add(MakeLabel(sb.ToString().Trim(), "ds-body-1", onTopicLink));
                container.Add(box);
                continue;
            }

            Match num = NumberedRx.Match(trimmed);
            bool bullet = trimmed.StartsWith("- ") || trimmed.StartsWith("* ");
            if (bullet || num.Success)
            {
                FlushParagraph();
                var row = new VisualElement();
                row.AddToClassList("help-md__li");
                var marker = new Label(bullet ? "•" : num.Groups[1].Value + ".");
                marker.AddToClassList("help-md__marker");
                row.Add(marker);
                string body = bullet ? trimmed.Substring(2) : num.Groups[2].Value;
                row.Add(MakeLabel(body, "ds-body-1 help-md__li-text", onTopicLink));
                container.Add(row);
                i++;
                continue;
            }

            paragraph.Add(trimmed);
            i++;
        }
        FlushParagraph();
        return container;
    }

    static Label MakeLabel(string markdownInline, string classes, Action<string> onTopicLink)
    {
        var label = new Label(ToRichText(markdownInline)) { enableRichText = true };
        foreach (string c in classes.Split(' ')) label.AddToClassList(c);
        return label;
    }

    public static string ToRichText(string s)
    {
        // Break any literal "<" so user text can't be parsed as a rich-text tag.
        s = s.Replace("<", "<​");
        s = CodeRx.Replace(s, $"<color={Accent}>$1</color>");
        s = BoldRx.Replace(s, "<b>$1</b>");
        s = ItalicRx.Replace(s, "<i>$1</i>");
        s = TopicLinkRx.Replace(s, $"<color={Accent}>$1</color>");
        s = OtherLinkRx.Replace(s, "$1");
        return s;
    }

    static VisualElement MakeImage(string caption, string relPath, string widthStr)
    {
        string resPath = ImageRoot + System.IO.Path.ChangeExtension(relPath.Trim().Replace('\\', '/'), null);
        Texture2D tex = Resources.Load<Texture2D>(resPath);

        var wrap = new VisualElement();
        wrap.AddToClassList("help-md__image-wrap");

        if (tex == null)
        {
            Debug.LogWarning($"MarkdownView: image not found at Resources/{resPath} (from '{relPath}')");
            var missing = new Label($"[missing image: {relPath}]");
            missing.AddToClassList("ds-caption");
            wrap.Add(missing);
            return wrap;
        }

        float width = Mathf.Min(tex.width, DefaultMaxImageWidth);
        if (int.TryParse(widthStr, out int explicitWidth)) width = explicitWidth;

        var image = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit };
        image.AddToClassList("help-md__image");
        image.style.width = width;
        image.style.height = width * tex.height / tex.width;
        wrap.Add(image);

        if (!string.IsNullOrWhiteSpace(caption))
        {
            var cap = new Label(caption);
            cap.AddToClassList("ds-caption");
            wrap.Add(cap);
        }
        return wrap;
    }
}
