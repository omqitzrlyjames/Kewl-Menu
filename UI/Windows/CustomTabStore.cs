using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;

namespace SocietyMenu;

public sealed class CustomTabDefinition
{
    public string Name { get; set; }
    public bool OnlyShowSelectedOptions { get; set; } = true;
    public List<CustomTabPage> Pages { get; } = new();

    public CustomTabPage EnsurePage()
    {
        if (Pages.Count == 0)
            Pages.Add(new CustomTabPage { Name = "Page 1" });
        return Pages[0];
    }

    public IEnumerable<string> ToggleNames => Pages.SelectMany(page => page.Columns).SelectMany(column => column.ToggleNames);
}

public sealed class CustomTabPage
{
    public string Name { get; set; }
    public List<CustomTabColumn> Columns { get; } = new();

    public CustomTabColumn EnsureColumn()
    {
        if (Columns.Count == 0)
            Columns.Add(new CustomTabColumn { Heading = "Options" });
        return Columns[0];
    }
}

public sealed class CustomTabColumn
{
    public string Heading { get; set; }
    public List<string> ToggleNames { get; } = new();
}

public static class CustomTabStore
{
    private const string FileName = "SocietyMenuCustomTabs.txt";
    private static readonly List<CustomTabDefinition> Tabs = new();
    private static readonly List<string> TabOrder = new();
    private static bool _loaded;

    public static IReadOnlyList<CustomTabDefinition> Definitions
    {
        get
        {
            EnsureLoaded();
            return Tabs;
        }
    }

    public static IReadOnlyList<string> SavedOrder
    {
        get
        {
            EnsureLoaded();
            return TabOrder;
        }
    }

    public static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        string path = Path.Combine(Paths.ConfigPath, FileName);
        if (!File.Exists(path)) return;

        CustomTabDefinition current = null;
        CustomTabPage currentPage = null;
        CustomTabColumn currentColumn = null;
        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("[Tab]", StringComparison.Ordinal))
            {
                string name = line[5..].Trim();
                if (name.Length == 0) continue;
                current = new CustomTabDefinition { Name = name };
                current.Pages.Add(new CustomTabPage { Name = "Page 1" });
                current.Pages[0].Columns.Add(new CustomTabColumn { Heading = "Options" });
                currentPage = current.Pages[0];
                currentColumn = currentPage.Columns[0];
                Tabs.Add(current);
            }
            else if (current != null && line.StartsWith("[OnlySelected]", StringComparison.Ordinal))
            {
                if (bool.TryParse(line[14..].Trim(), out bool onlySelected))
                    current.OnlyShowSelectedOptions = onlySelected;
            }
            else if (current != null && line.StartsWith("[Page]", StringComparison.Ordinal))
            {
                string pageName = line[6..].Trim();
                if (current.Pages.Count == 1 && current.Pages[0].Name == "Page 1" && current.Pages[0].Columns.Count == 1 && current.Pages[0].Columns[0].ToggleNames.Count == 0)
                    current.Pages[0].Name = pageName;
                else
                    current.Pages.Add(new CustomTabPage { Name = pageName });
                currentPage = current.Pages[^1];
                currentColumn = null;
            }
            else if (current != null && line.StartsWith("[Column]", StringComparison.Ordinal))
            {
                CustomTabPage page = current.Pages.Count == 0 ? current.EnsurePage() : current.Pages[^1];
                string heading = line[8..].Trim();
                if (page.Columns.Count == 1 && page.Columns[0].Heading == "Options" && page.Columns[0].ToggleNames.Count == 0)
                    page.Columns[0].Heading = heading;
                else
                    page.Columns.Add(new CustomTabColumn { Heading = heading });
                currentPage = page;
                currentColumn = page.Columns[^1];
            }
            else if (current != null && line == "[EndColumn]")
            {
                currentColumn = null;
            }
            else if (current != null && line == "[EndPage]")
            {
                currentColumn = null;
                currentPage = null;
            }
            else if (line.StartsWith("[Order]", StringComparison.Ordinal))
            {
                string name = line[7..].Trim();
                if (name.Length > 0 && !TabOrder.Contains(name, StringComparer.OrdinalIgnoreCase))
                    TabOrder.Add(name);
                current = null;
                currentPage = null;
                currentColumn = null;
            }
            else if (current != null && currentColumn != null && CheatToggles.ToggleFields.ContainsKey(line))
            {
                if (!currentColumn.ToggleNames.Contains(line, StringComparer.OrdinalIgnoreCase))
                    currentColumn.ToggleNames.Add(line);
            }
        }
    }

    public static CustomTabDefinition Add(string name)
    {
        EnsureLoaded();
        string cleanName = (name ?? string.Empty).Trim();
        if (cleanName.Length == 0 || Tabs.Any(tab => string.Equals(tab.Name, cleanName, StringComparison.OrdinalIgnoreCase)))
            return null;

        var definition = new CustomTabDefinition { Name = cleanName };
        definition.Pages.Add(new CustomTabPage { Name = "Page 1" });
        definition.Pages[0].Columns.Add(new CustomTabColumn { Heading = "Options" });
        Tabs.Add(definition);
        Save();
        return definition;
    }

    public static void Save()
    {
        EnsureLoaded();
        Directory.CreateDirectory(Paths.ConfigPath);
        using StreamWriter writer = new(Path.Combine(Paths.ConfigPath, FileName));
        foreach (string name in TabOrder)
            writer.WriteLine("[Order] " + name);
        if (TabOrder.Count > 0)
            writer.WriteLine();

        foreach (CustomTabDefinition tab in Tabs)
        {
            writer.WriteLine("[Tab] " + tab.Name);
            writer.WriteLine("[OnlySelected] " + tab.OnlyShowSelectedOptions);
            foreach (CustomTabPage page in tab.Pages)
            {
                writer.WriteLine("[Page] " + page.Name);
                foreach (CustomTabColumn column in page.Columns)
                {
                    writer.WriteLine("[Column] " + column.Heading);
                    foreach (string toggleName in column.ToggleNames)
                        writer.WriteLine(toggleName);
                    writer.WriteLine("[EndColumn]");
                }
                writer.WriteLine("[EndPage]");
            }
            writer.WriteLine();
        }
    }

    public static void SaveOrder(IEnumerable<string> names)
    {
        EnsureLoaded();
        TabOrder.Clear();
        foreach (string name in names)
        {
            if (!string.IsNullOrWhiteSpace(name) && !TabOrder.Contains(name, StringComparer.OrdinalIgnoreCase))
                TabOrder.Add(name);
        }
        Save();
    }

    public static void Remove(string name)
    {
        EnsureLoaded();
        Tabs.RemoveAll(tab => string.Equals(tab.Name, name, StringComparison.OrdinalIgnoreCase));
        TabOrder.RemoveAll(tab => string.Equals(tab, name, StringComparison.OrdinalIgnoreCase));
        Save();
    }
}
