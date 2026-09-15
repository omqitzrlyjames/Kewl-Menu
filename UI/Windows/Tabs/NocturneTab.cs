using UnityEngine;

namespace SocietyMenu;

public class NocturneTab : ITab
{
    public string name => "Nocturne";

    public void Draw()
    {
        Nocturne.NocturneMenu menu = Nocturne.NocturnePlugin.Menu;
        if (menu == null)
        {
            GUILayout.Label("Nocturne is not initialized.", GUIStylePreset.TabSubtitle);
            return;
        }

        Nocturne.NocturneMenu.EmbeddedUi = true;
        Rect area = GUILayoutUtility.GetRect(720f, 610f, GUILayout.ExpandWidth(true));
        menu.DrawEmbedded(Mathf.Max(620f, area.width), 610f);
    }
}
