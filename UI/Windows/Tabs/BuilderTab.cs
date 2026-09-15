using UnityEngine;

namespace SocietyMenu;

public sealed class BuilderTab : ITab
{
    private readonly MenuUI _menu;

    public BuilderTab(MenuUI menu)
    {
        _menu = menu;
    }

    public string name => "Builder";

    public void Draw()
    {
        _menu.DrawBuilderContents();
    }
}
