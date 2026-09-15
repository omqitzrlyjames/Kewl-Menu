using System;
using System.Collections.Generic;
using UnityEngine;

namespace SocietyMenu;

public sealed class AllTab : ITab
{
    private readonly IReadOnlyList<ITab> _tabs;

    public AllTab(IReadOnlyList<ITab> tabs)
    {
        _tabs = tabs;
    }

    public string name => "All";

    public void Draw()
    {
        foreach (ITab tab in _tabs)
        {
            if (tab is AllTab || tab is SearchTab || !MenuUI.IsTabVisibleForAggregate(tab))
                continue;

            GUILayout.Label(tab.name, GUIStylePreset.TabTitle);
            try
            {
                tab.Draw();
            }
            catch (Exception exception)
            {
                GUILayout.Label($"Could not draw {tab.name}: {exception.Message}");
            }
            GUILayout.Space(14f);
        }

        Nocturne.NocturneMenu nocturne = Nocturne.NocturnePlugin.Menu;
        if (nocturne != null)
        {
            GUILayout.Label("Nocturne Features", GUIStylePreset.TabTitle);
            try
            {
                foreach (Nocturne.QuickItem item in Nocturne.NocturneQuick.Items)
                    item.Cfg.Value = UIHelpers.Toggle(item.Cfg.Value, item.Label);
            }
            catch (Exception exception)
            {
                GUILayout.Label($"Could not draw Nocturne features: {exception.Message}");
            }
            GUILayout.Space(14f);
        }
    }
}
