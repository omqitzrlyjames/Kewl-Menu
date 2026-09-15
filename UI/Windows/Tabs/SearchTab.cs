namespace SocietyMenu;

public sealed class SearchTab : ITab
{
    private readonly MenuUI _menu;

    public SearchTab(MenuUI menu)
    {
        _menu = menu;
    }

    public string name => "Search";

    public void Draw()
    {
        _menu.DrawSearchTabContents();
    }
}
