using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SocietyMenu;

public sealed class CustomTab : ITab
{
    private readonly CustomTabDefinition _definition;
    private int _page;

    public CustomTab(CustomTabDefinition definition)
    {
        _definition = definition;
    }

    public string name => _definition.Name;

    public void Draw()
    {
        if (_definition.Pages.Count == 0)
        {
            GUILayout.Label("No pages added yet.", GUIStylePreset.TabSubtitle);
            return;
        }

        _page = Mathf.Clamp(_page, 0, _definition.Pages.Count - 1);
        CustomTabPage page = _definition.Pages[_page];
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Previous", GUILayout.Width(80)) && _page > 0)
            _page--;
        GUILayout.Label(page.Name, GUIStylePreset.TabTitle);
        if (GUILayout.Button("Next", GUILayout.Width(80)) && _page < _definition.Pages.Count - 1)
            _page++;
        GUILayout.EndHorizontal();
        int selectedCount = 0;
        foreach (CustomTabColumn countColumn in page.Columns)
            selectedCount += countColumn.ToggleNames.Count;
        GUILayout.Label("Selected options: " + selectedCount, GUIStylePreset.TabSubtitle);

        if (page.Columns.Count == 0)
        {
            GUILayout.Label("No columns added yet.", GUIStylePreset.TabSubtitle);
            return;
        }

        GUILayout.BeginHorizontal();
        foreach (CustomTabColumn column in page.Columns)
        {
            GUILayout.BeginVertical(GUIStylePreset.ModernBox, GUILayout.ExpandWidth(true));
            GUILayout.Label(column.Heading, GUIStylePreset.SectionHeader);
            foreach (string toggleName in column.ToggleNames.ToList())
            {
                if (!CheatToggles.ToggleFields.TryGetValue(toggleName, out FieldInfo field))
                    continue;

                bool value = (bool)field.GetValue(null);
                bool nextValue = UIHelpers.Toggle(value, FormatToggleName(toggleName));
                if (nextValue != value)
                    field.SetValue(null, nextValue);
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
    }

    private static string FormatToggleName(string toggleName)
    {
        if (string.IsNullOrEmpty(toggleName)) return toggleName;
        string spaced = string.Concat(toggleName.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? " " + character : character.ToString()));
        return spaced.Trim();
    }
}
