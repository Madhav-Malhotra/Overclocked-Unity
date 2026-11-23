using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// One tutorial pane, parsed from a Markdown file under Resources/HelpContent/Tutorial/.
public class TutorialTopic
{
    public string id;          // file name without extension; usable as a link target
    public string title;
    public string category = "General";
    public string icon = "book"; // ds-icon--<icon>; see Assets/DesignSystem/docs/ICONS.md
    public int order;
    public string[] keywords = Array.Empty<string>();
    public string body;        // markdown, frontmatter stripped
    public string searchText;  // lowercased title + keywords + body
}

// Loads and searches tutorial panes. File format (see Assets/Levels/README.md "Help content"):
//
//   ---
//   title: The ALU
//   category: Computer Architecture
//   icon: bolt
//   order: 30
//   keywords: alu, arithmetic, add, execute
//   ---
//   # Markdown body...
public static class TutorialLibrary
{
    public const string TutorialPath = "HelpContent/Tutorial";
    public const string AboutPath = "HelpContent/about";

    static List<TutorialTopic> cache;

    public static IReadOnlyList<TutorialTopic> Topics
    {
        get
        {
            if (cache == null) Reload();
            return cache;
        }
    }

    public static void Reload()
    {
        cache = new List<TutorialTopic>();
        foreach (TextAsset asset in Resources.LoadAll<TextAsset>(TutorialPath))
        {
            try { cache.Add(Parse(asset.name, asset.text)); }
            catch (Exception e) { Debug.LogError($"TutorialLibrary: failed to parse '{asset.name}': {e.Message}"); }
        }
        cache = cache.OrderBy(t => t.order).ThenBy(t => t.title, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static TutorialTopic Find(string id) =>
        Topics.FirstOrDefault(t => string.Equals(t.id, id, StringComparison.OrdinalIgnoreCase));

    // Categories ordered by their lowest-order topic; topics inside keep (order, title).
    public static List<(string category, List<TutorialTopic> topics)> Grouped(IEnumerable<TutorialTopic> source)
    {
        return source
            .GroupBy(t => t.category)
            .OrderBy(g => g.Min(t => t.order))
            .Select(g => (g.Key, g.OrderBy(t => t.order).ThenBy(t => t.title).ToList()))
            .ToList();
    }

    // Case-insensitive substring match over title + keywords + body. Title matches are listed first,
    // then keyword/body matches (OrderBy is stable, so each group keeps the usual (order, title) sort).
    public static List<TutorialTopic> Search(string query)
    {
        string q = query?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q)) return Topics.ToList();
        return Topics.Where(t => t.searchText.Contains(q))
                     .OrderBy(t => t.title.ToLowerInvariant().Contains(q) ? 0 : 1)
                     .ToList();
    }

    public static TutorialTopic Parse(string id, string raw)
    {
        // "03_instruction_bricks" -> "instruction-bricks" (leading NN_ is just for file sorting)
        string cleanId = System.Text.RegularExpressions.Regex.Replace(id, @"^\d+_", "").Replace('_', '-');
        var topic = new TutorialTopic { id = cleanId, title = id };
        string text = raw.Replace("\r\n", "\n").TrimStart('﻿');

        if (text.StartsWith("---\n"))
        {
            int end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
            if (end > 0)
            {
                foreach (string line in text.Substring(4, end - 4).Split('\n'))
                {
                    int colon = line.IndexOf(':');
                    if (colon <= 0) continue;
                    string key = line.Substring(0, colon).Trim().ToLowerInvariant();
                    string val = line.Substring(colon + 1).Trim();
                    switch (key)
                    {
                        case "title": topic.title = val; break;
                        case "category": topic.category = val; break;
                        case "icon": topic.icon = val; break;
                        case "order": int.TryParse(val, out topic.order); break;
                        case "keywords":
                            topic.keywords = val.Split(',').Select(k => k.Trim()).Where(k => k.Length > 0).ToArray();
                            break;
                    }
                }
                int bodyStart = text.IndexOf('\n', end + 1);
                text = bodyStart >= 0 ? text.Substring(bodyStart + 1) : "";
            }
        }

        topic.body = text.Trim('\n');
        topic.searchText = $"{topic.title} {string.Join(" ", topic.keywords)} {topic.body}".ToLowerInvariant();
        return topic;
    }
}
