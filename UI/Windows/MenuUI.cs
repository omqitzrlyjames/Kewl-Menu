using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Reflection;

namespace SocietyMenu;

public class MenuUI : MonoBehaviour
{
    public static readonly string[] ThemeNames = { "Material", "Glass", "Refract", "Paper", "Terminal", "Neon" };
    public static readonly string[] GlassTintThemes =
    {
        "Clear", "Red", "Orange", "Yellow", "Green", "Cyan", "Blue", "Violet",
        "Rainbow", "Sunset Gradient", "Ocean Gradient", "Forest Gradient", "Custom Gradient"
    };
    public static int windowHeight = 600;
    public static int windowWidth = 800;
    public static Rect windowRect;

    public static bool isGUIActive = false;
    public static bool IsMaterialLayoutActive { get; private set; }
    public static string CurrentTheme => NormalizeTheme(KEWLMENU.menuUiTheme?.Value);
    public static GUISkin MaterialSkin => _materialSkin;

    private static string NormalizeTheme(string theme)
    {
        if (string.Equals(theme, "Liquid Glass", StringComparison.OrdinalIgnoreCase))
            return "Glass";
        return ThemeNames.Contains(theme, StringComparer.OrdinalIgnoreCase) ?
            ThemeNames.First(name => string.Equals(name, theme, StringComparison.OrdinalIgnoreCase)) : "Material";
    }

    public static void SetTheme(int index)
    {
        string theme = ThemeNames[Mathf.Clamp(index, 0, ThemeNames.Length - 1)];
        KEWLMENU.menuUiTheme.Value = theme;
        KEWLMENU.menuMaterialLayout.Value = string.Equals(theme, "Material", StringComparison.Ordinal);
        DestroyAlternativeSkin();
        ResetMaterialContentScroll();
    }

    public static void SetGlassTintTheme(int index)
    {
        KEWLMENU.glassTintTheme.Value = GlassTintThemes[Mathf.Clamp(index, 0, GlassTintThemes.Length - 1)];
        DestroyAlternativeSkin();
    }

    public static float GlassOpacity
    {
        get => Mathf.Clamp01(KEWLMENU.glassOpacity?.Value ?? 0.78f);
        set
        {
            if (KEWLMENU.glassOpacity == null)
                return;
            KEWLMENU.glassOpacity.Value = Mathf.Clamp01(value);
            DestroyAlternativeSkin();
        }
    }

    public static float GlassTintStrength
    {
        get => Mathf.Clamp01(KEWLMENU.glassTintStrength?.Value ?? 0.35f);
        set
        {
            if (KEWLMENU.glassTintStrength == null)
                return;
            KEWLMENU.glassTintStrength.Value = Mathf.Clamp01(value);
            DestroyAlternativeSkin();
        }
    }

    public static bool DarkerTheme => KEWLMENU.menuDarkerTheme?.Value == true;

    public static void SetDarkerTheme(bool darker)
    {
        if (KEWLMENU.menuDarkerTheme == null || DarkerTheme == darker)
            return;
        KEWLMENU.menuDarkerTheme.Value = darker;
        ClearMaterialStyleCache();
        _materialSkin = null;
        _materialSkinSource = null;
    }

    public static GUISkin GetWindowSkin(GUISkin fallback)
    {
        if (KEWLMENU.menuMaterialLayout?.Value != true)
            return fallback;

        return GetMaterialSkin(_legacySkin ?? fallback);
    }

    private void OnDestroy()
    {
        ResetMaterialUiState();
        DestroyAlternativeSkin();

        if (_legacySkin != null)
        {
            UnityEngine.Object.Destroy(_legacySkin);
            _legacySkin = null;
        }
    }

    private List<ITab> _tabs = new();
    private int _selectedTab = 1;
    private Vector2 _tabScrollPosition = Vector2.zero;
    private Vector2 _contentScrollPosition = Vector2.zero;
    private Vector2 _materialTabScrollPosition = Vector2.zero;
    private Vector2 _materialTabScrollTarget = Vector2.zero;
    private Vector2 _materialContentScrollPosition = Vector2.zero;
    private Vector2 _materialContentScrollTarget = Vector2.zero;
    public static float hue; // For RGB mode
    private bool _wasInGameplay = false;
    private int _materialRenderedTab = -1;
    private float _materialPageProgress = 1f;
    private string _lastSceneName;
    private bool _lastGameplayState;
    private bool _lastPlayerAvailable;
    private bool _lastMaterialLayout;
    private bool _materialResizing;
    private Vector2 _materialResizeStart;
    private Vector2 _materialResizeOrigin;
    private bool _initialized;
    private string _toggleSearch = string.Empty;
    private readonly TextField _toggleSearchField = new();
    private bool _editMode;
    private string _newTabName = string.Empty;
    private readonly TextField _newTabNameField = new();
    private string _builderTabName;
    private string _builderToggleSearch = string.Empty;
    private readonly TextField _builderToggleSearchField = new();
    private int _builderPageIndex;
    private int _builderColumnIndex;
    private string _builderPageName = string.Empty;
    private string _builderColumnName = string.Empty;
    private readonly TextField _builderPageNameField = new();
    private readonly TextField _builderColumnNameField = new();
    private bool _builderEditorOpen;
    private Rect _builderEditorRect = new(130f, 70f, 800f, 680f);
    private Vector2 _builderEditorScrollPosition;
    private bool _menuDragging;
    private bool _builderEditorDragging;
    private readonly List<CustomTab> _customTabs = new();
    private GUISkin _unitySkin;
    private static GUISkin _legacySkin;
    private static readonly Dictionary<string, GUIStyle> MaterialStyles = new();
    private static readonly Dictionary<Color, Texture2D> RoundedTextures = new();
    private static readonly Dictionary<string, Texture2D> GradientTextures = new();
    private static readonly Dictionary<Color, Texture2D> SliderRailTextures = new();
    private static readonly List<Texture2D> AlternativeTextures = new();

    private static readonly string[] DefaultTabOrder =
    {
        "Builder", "All", "Search", "Movement", "Self", "ESP", "Roles", "Players", "Ship", "Sabotage", "Chat", "Animations",
        "Console", "Lobby", "Passive", "Troll", "Protections", "Anticheat", "Modes", "Config",
        "Profiles", "Skins", "Nocturne", "Settings", "Test"
    };

    private void Start()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;
        _tabs.Add(new BuilderTab(this));
        _tabs.Add(new AllTab(_tabs));
        _tabs.Add(new SearchTab(this));
        _tabs.Add(new MovementTab());
        _tabs.Add(new SelfTab());
        _tabs.Add(new ESPTab());
        _tabs.Add(new RolesTab());
        _tabs.Add(new PlayersTab());
        _tabs.Add(new ShipTab());
        _tabs.Add(new SabotageTab());
        _tabs.Add(new ChatTab());
        _tabs.Add(new AnimationsTab());
        _tabs.Add(new ConsoleTab());
        _tabs.Add(new LobbyTab());
        _tabs.Add(new PassiveTab());
        _tabs.Add(new TrollTab());
        _tabs.Add(new ProtectionsTab());
        _tabs.Add(new AnticheatTab());
        _tabs.Add(new ModesTab());
        _tabs.Add(new ConfigTab());
        _tabs.Add(new ProfilesTab());
        _tabs.Add(new SkinsTab());
        _tabs.Add(new NocturneTab());
        // _tabs.Add(new OverloadTab());

        // Instantiate 2D area of MenuUI
        windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
        _tabs.Add(new SettingsTab());
        _tabs.Add(new TestTab());
        foreach (CustomTabDefinition definition in CustomTabStore.Definitions)
        {
            CustomTab customTab = new(definition);
            _customTabs.Add(customTab);
            _tabs.Add(customTab);
        }
        ApplySavedTabOrder();
    }

    public void InitStyles()
    {
        GUI.skin.toggle.fontSize = GUI.skin.button.fontSize = GUI.skin.label.fontSize = 14;
        GUI.skin.window.padding = new RectOffset { left = 12, right = 12, top = 30, bottom = 12 };
        GUI.skin.window.margin = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 };
    }

    public void SelectTab(string tabName)
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].name != tabName || !IsTabVisible(_tabs[i])) continue;
            _selectedTab = i;
            ResetMaterialContentScroll();
            return;
        }
    }

    private static bool IsTabVisible(ITab tab)
    {
        if (tab is TestTab)
            return KEWLMENU.menuShowTestTab?.Value == true;
        return true;
    }

    internal static bool IsTabVisibleForAggregate(ITab tab)
    {
        return IsTabVisible(tab);
    }

    internal void DrawBuilderContents()
    {
        GUILayout.Label("Tab Builder", GUIStylePreset.TabTitle);
        GUILayout.Label("Build tabs with pages, columns, headings, and searchable options.", GUIStylePreset.TabSubtitle);
        GUILayout.Space(8f);

        bool nextEditMode = GUILayout.Toggle(_editMode, "Enable edit mode");
        if (nextEditMode != _editMode)
        {
            _editMode = nextEditMode;
            ResetMaterialContentScroll();
        }

        if (!_editMode)
        {
            GUILayout.Label("Enable edit mode to design custom tabs.", GUIStylePreset.TabSubtitle);
            return;
        }

        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();
        _newTabNameField.Content = _newTabName;
        _newTabNameField.Draw(180, 24);
        _newTabName = _newTabNameField.Content;
        if (GUILayout.Button("Create tab", GUILayout.Width(100)) && !string.IsNullOrWhiteSpace(_newTabName))
        {
            CustomTabDefinition definition = CustomTabStore.Add(_newTabName);
            if (definition != null)
            {
                CustomTab customTab = new(definition);
                _customTabs.Add(customTab);
                _tabs.Add(customTab);
                _newTabName = string.Empty;
                SaveTabOrder();
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(12f);
        GUILayout.Label("Custom tabs", GUIStylePreset.TabSubtitle);
        if (CustomTabStore.Definitions.Count == 0)
        {
            GUILayout.Label("Create a custom tab to begin.", GUIStylePreset.TabSubtitle);
        }
        else
        {
            foreach (CustomTabDefinition definition in CustomTabStore.Definitions)
            {
                GUILayout.BeginHorizontal(GUIStylePreset.ModernBox);
                GUILayout.Label(definition.Name, GUIStylePreset.SectionHeader, GUILayout.MinWidth(180));
                GUILayout.Label(definition.Pages.Count + " page(s)", GUIStylePreset.TabSubtitle, GUILayout.Width(90));
                int columnCount = 0;
                foreach (CustomTabPage page in definition.Pages)
                    columnCount += page.Columns.Count;
                GUILayout.Label(columnCount + " column(s)", GUIStylePreset.TabSubtitle, GUILayout.Width(100));
                if (GUILayout.Button("Open editor", GUIStylePreset.TabButton, GUILayout.Width(110)))
                {
                    _builderTabName = definition.Name;
                    _builderPageIndex = 0;
                    _builderColumnIndex = 0;
                    _builderEditorOpen = true;
                    ResetMaterialContentScroll();
                }
                if (GUILayout.Button("Delete", GUIStylePreset.NormalButton, GUILayout.Width(70)))
                {
                    CustomTabStore.Remove(definition.Name);
                    _customTabs.RemoveAll(tab => string.Equals(tab.name, definition.Name, StringComparison.OrdinalIgnoreCase));
                    _tabs.RemoveAll(tab => tab is CustomTab custom && string.Equals(custom.name, definition.Name, StringComparison.OrdinalIgnoreCase));
                    SaveTabOrder();
                    GUILayout.EndHorizontal();
                    break;
                }
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(12f);
        GUILayout.Label("Menu order", GUIStylePreset.TabSubtitle);
        for (int index = 0; index < _tabs.Count; index++)
        {
            ITab tab = _tabs[index];
            if (tab is BuilderTab || tab is AllTab || tab is SearchTab)
                continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(tab.name, GUILayout.MinWidth(180));
            GUI.enabled = index > 3;
            if (GUILayout.Button("Up", GUILayout.Width(48)))
                MoveTab(index, -1);
            GUI.enabled = index < _tabs.Count - 1;
            if (GUILayout.Button("Down", GUILayout.Width(58)))
                MoveTab(index, 1);
            GUI.enabled = true;
            if (tab is CustomTab && GUILayout.Button("Delete", GUILayout.Width(64)))
            {
                CustomTabStore.Remove(tab.name);
                _customTabs.Remove((CustomTab)tab);
                _tabs.RemoveAt(index);
                if (_selectedTab >= _tabs.Count)
                    _selectedTab = _tabs.Count - 1;
                ResetMaterialContentScroll();
                GUILayout.EndHorizontal();
                break;
            }
            GUILayout.EndHorizontal();
        }

    }

    private void MoveTab(int index, int direction)
    {
        int target = index + direction;
        while (target >= 0 && target < _tabs.Count && (_tabs[target] is BuilderTab || _tabs[target] is AllTab || _tabs[target] is SearchTab))
            target += direction;
        if (target < 0 || target >= _tabs.Count)
            return;

        ITab tab = _tabs[index];
        _tabs[index] = _tabs[target];
        _tabs[target] = tab;
        if (_selectedTab == index)
            _selectedTab = target;
        else if (_selectedTab == target)
            _selectedTab = index;
        SaveTabOrder();
    }

    private void SaveTabOrder()
    {
        CustomTabStore.SaveOrder(_tabs
            .Where(tab => tab is not BuilderTab && tab is not AllTab && tab is not SearchTab)
            .Select(tab => tab.name));
    }

    private void ApplySavedTabOrder()
    {
        IReadOnlyList<string> savedOrder = CustomTabStore.SavedOrder;
        if (savedOrder.Count == 0)
            return;

        List<ITab> fixedTabs = _tabs.Where(tab => tab is BuilderTab || tab is AllTab || tab is SearchTab).ToList();
        List<ITab> movableTabs = _tabs.Where(tab => tab is not BuilderTab && tab is not AllTab && tab is not SearchTab).ToList();
        List<ITab> ordered = new();
        foreach (string name in savedOrder)
        {
            ITab tab = movableTabs.FirstOrDefault(candidate => string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase));
            if (tab != null)
            {
                ordered.Add(tab);
                movableTabs.Remove(tab);
            }
        }
        ordered.AddRange(movableTabs);
        _tabs = fixedTabs.Concat(ordered).ToList();
    }

    private void Update()
    {
        EnsureInitialized();
        RefreshUiForContextChanges();

        if (Input.GetKeyDown(KeyCode.Delete) ||
            Input.GetKeyDown(Utils.StringToKeycode(KEWLMENU.menuKeybind.Value)))
        {
            // Enable or disable GUI
            isGUIActive = !isGUIActive;

            if (KEWLMENU.menuOpenOnMouse.Value)
            {
                // Teleport the window to the mouse for immediate use
                Vector2 mousePosition = Input.mousePosition;
                windowRect.position = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
            }

        }

        if (CheatToggles.rgbMode)
        {
            hue += Time.deltaTime * 0.3f;
            if (hue > 1f) hue -= 1f;
        }

        if (CheatToggles.stealthMode != KEWLMENU.inStealthMode)
        {
            KEWLMENU.inStealthMode = CheatToggles.stealthMode;
        }

        if (CheatToggles.panicMode) Utils.Panic();

        var stamp = ModManager.Instance.ModStamp;
        if (stamp) stamp.enabled = !(KEWLMENU.inStealthMode || KEWLMENU.isPanicked);

        if (CheatToggles.openConfig)
        {
            Utils.OpenConfigFile();
            CheatToggles.openConfig = false;
        }

        // Check if round just ended and disable sabotage cheats
        bool currentlyInGameplay = Utils.isPlayer && Utils.isShip;
        if (_wasInGameplay != currentlyInGameplay)
            ResetMaterialUiState();
        if (_wasInGameplay && !currentlyInGameplay)
        {
            DisableSabotageCheats();
        }
        _wasInGameplay = currentlyInGameplay;

        if (CheatToggles.reloadConfig)
        {
            KEWLMENU.Plugin.Config.Reload();
            CheatToggles.reloadConfig = false;
        }

        if (CheatToggles.saveProfile)
        {
            CheatToggles.saveProfile = false;
            CheatToggles.SaveTogglesToProfile();
        }

        if (CheatToggles.loadProfile)
        {
            CheatToggles.LoadTogglesFromProfile();
            CheatToggles.loadProfile = false;
        }

        // Turn off player-dependent cheats if local player doesn't exist
        if (!Utils.isPlayer)
        {
            CheatToggles.setFakeRole = false;
            CheatToggles.setFakeAlive = false;
            CheatToggles.killAll = false;
            CheatToggles.telekillPlayer = false;
            CheatToggles.killAllCrew = false;
            CheatToggles.killAllImps = false;
            CheatToggles.teleportPlayer = false;
            CheatToggles.spectate = false;
            CheatToggles.freecam = false;
            CheatToggles.killPlayer = false;
            CheatToggles.callMeeting = false;

            if (CheatToggles.runOverload)
            {
                OverloadUI.StopOverload();
            }
        }

        // Turn off ship-dependent cheats if ship doesn't exist
        if (!Utils.isShip)
        {
            DisableSabotageCheats();
            CheatToggles.completeMyTasks = false;
            CheatToggles.kickVents = false;
            CheatToggles.reportBody = false;
            CheatToggles.closeMeeting = false;
            CheatToggles.closeAllDoors = false;
            CheatToggles.openAllDoors = false;
            CheatToggles.spamCloseAllDoors = false;
            CheatToggles.spamOpenAllDoors = false;

            SocietyMenuCheats.StopShipAnimCheats();
            SocietyMenuCheats.CleanUpInjectedTasks();
        }

        // Turn off host-dependent cheats if not host or freeplay
        if (!Utils.isHost && !Utils.isFreePlay)
        {
            CheatToggles.killAll = false;
            CheatToggles.telekillPlayer = false;
            CheatToggles.killAllCrew = false;
            CheatToggles.killAllImps = false;
            CheatToggles.killPlayer = false;
            CheatToggles.ejectPlayer = false;
            CheatToggles.noKillCd = false;
            CheatToggles.killAnyone = false;
            CheatToggles.killVanished = false;
            CheatToggles.forceStartGame = false;
            CheatToggles.skipMeeting = false;
            CheatToggles.voteImmune = false;
            CheatToggles.noGameEnd = false;
            CheatToggles.showProtectMenu = false;
            CheatToggles.showRolesMenu = false;
            CheatToggles.noOptionsLimits = false;
        }

        // Turn off meeting-dependent cheats if not in a meeting
        if (!Utils.isMeeting)
        {
            CheatToggles.skipMeeting = false;
            CheatToggles.ejectPlayer = false;
        }
    }

    private void RefreshUiForContextChanges()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool gameplayState = Utils.isPlayer && Utils.isShip;
        bool playerAvailable = PlayerControl.LocalPlayer != null;
        bool materialLayout = string.Equals(CurrentTheme, "Material", StringComparison.Ordinal);
        if (_lastSceneName == null)
        {
            _lastSceneName = sceneName;
            _lastGameplayState = gameplayState;
            _lastPlayerAvailable = playerAvailable;
            _lastMaterialLayout = materialLayout;
            return;
        }

        if (sceneName == _lastSceneName && gameplayState == _lastGameplayState &&
            playerAvailable == _lastPlayerAvailable && materialLayout == _lastMaterialLayout)
            return;

        _lastSceneName = sceneName;
        _lastGameplayState = gameplayState;
        _lastPlayerAvailable = playerAvailable;
        _lastMaterialLayout = materialLayout;
        ResetMaterialUiState();
    }

    private void ResetMaterialUiState()
    {
        _materialRenderedTab = -1;
        _materialPageProgress = 1f;
        _materialTabScrollPosition = Vector2.zero;
        _materialTabScrollTarget = Vector2.zero;
        _materialContentScrollPosition = Vector2.zero;
        _materialContentScrollTarget = Vector2.zero;
        _contentScrollPosition = Vector2.zero;
        UIHelpers.ResetMaterialStyles();
        ClearMaterialStyleCache();
        _materialSkin = null;
        _materialSkinSource = null;
        IsMaterialLayoutActive = false;
        GUI.changed = true;
    }

    public void OnGUI()
    {
        EnsureInitialized();
        if (!isGUIActive || KEWLMENU.isPanicked) return;

        bool materialLayout = KEWLMENU.menuMaterialLayout?.Value == true;
        IsMaterialLayoutActive = materialLayout;
        if (_unitySkin == null)
            _unitySkin = GUI.skin;
        if (_legacySkin == null)
            _legacySkin = UnityEngine.Object.Instantiate(_unitySkin);
        GUI.skin = _legacySkin;
        InitStyles();
        UIHelpers.ApplyUIColor();

        if (materialLayout)
            GUI.skin = GetMaterialSkin(_legacySkin);
        else
            GUI.skin = GetAlternativeSkin();

        if (materialLayout && _selectedTab >= 0 && _selectedTab < _tabs.Count &&
            !IsTabVisible(_tabs[_selectedTab]))
        {
            _selectedTab = 0;
            ResetMaterialContentScroll();
        }

        GUI.WindowFunction renderer = materialLayout
            ? (GUI.WindowFunction)MaterialWindowFunction
            : (GUI.WindowFunction)AlternativeWindowFunction;

        string title = materialLayout ? string.Empty : "KEWLMENU by omqItzrlyjames v" + KEWLMENU.societyVersion;
        windowRect.width = Mathf.Min(windowRect.width, Mathf.Max(320f, Screen.width - 20f));
        windowRect.height = Mathf.Min(windowRect.height, Mathf.Max(260f, Screen.height - 20f));
        windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
        windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
        Color previousWindowColor = GUI.color;
        if (materialLayout)
            GUI.color = new Color(1f, 1f, 1f, MaterialWindowOpacity);
        windowRect = GUI.Window((int)WindowId.MenuUI, windowRect, renderer, title);
        if (materialLayout)
        {
            GUISkin resizeSkin = GUI.skin;
            GUI.skin = GetMaterialSkin(_legacySkin);
            DrawMaterialResizeGrip();
            GUI.skin = resizeSkin;
        }
        GUI.color = previousWindowColor;
        GUI.skin = _unitySkin;
        BlockUnderlyingClicks(windowRect);

        if (_builderEditorOpen)
        {
            GUISkin previousSkin = GUI.skin;
            Color previousColor = GUI.color;
            Color previousContentColor = GUI.contentColor;
            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.skin = materialLayout ? GetMaterialSkin(_legacySkin) : GetAlternativeSkin();
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.white;
            _builderEditorRect.width = Mathf.Min(_builderEditorRect.width, Mathf.Max(360f, Screen.width - 24f));
            _builderEditorRect.height = Mathf.Min(_builderEditorRect.height, Mathf.Max(320f, Screen.height - 24f));
            _builderEditorRect.x = Mathf.Clamp(_builderEditorRect.x, 8f, Mathf.Max(8f, Screen.width - _builderEditorRect.width - 8f));
            _builderEditorRect.y = Mathf.Clamp(_builderEditorRect.y, 8f, Mathf.Max(8f, Screen.height - _builderEditorRect.height - 8f));
            _builderEditorRect = GUI.Window((int)WindowId.MenuUI + 1, _builderEditorRect,
                (GUI.WindowFunction)DrawBuilderEditorWindow, materialLayout ? string.Empty : "Custom Tab Editor");
            GUI.skin = previousSkin;
            GUI.color = previousColor;
            GUI.contentColor = previousContentColor;
            GUI.backgroundColor = previousBackgroundColor;
            BlockUnderlyingClicks(_builderEditorRect);
        }

        if (Event.current != null && Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
            Event.current.Use();

    }

    private void DrawBuilderEditorWindow(int windowId)
    {
        CustomTabDefinition definition = null;
        foreach (CustomTabDefinition candidate in CustomTabStore.Definitions)
        {
            if (string.Equals(candidate.Name, _builderTabName, StringComparison.OrdinalIgnoreCase))
            {
                definition = candidate;
                break;
            }
        }
        if (definition == null)
        {
            _builderEditorOpen = false;
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = Color.white;
        GUIStyle editorSurface = IsMaterialLayoutActive ? CreatePanelStyle(MaterialSurface) : GUI.skin.window;
        GUI.Box(new Rect(0f, 0f, _builderEditorRect.width, _builderEditorRect.height), GUIContent.none, editorSurface);
        if (IsMaterialLayoutActive)
        {
            GUI.Box(new Rect(0f, 0f, _builderEditorRect.width, 34f), GUIContent.none, CreatePanelStyle(MaterialHeader));
            GUI.Label(new Rect(16f, 5f, _builderEditorRect.width - 32f, 24f), "CUSTOM TAB EDITOR", CreateHeaderStyle());
        }
        GUILayout.BeginArea(new Rect(14f, 40f, _builderEditorRect.width - 28f, _builderEditorRect.height - 52f));
        GUI.color = previousColor;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Design pages, columns, headings, and options.", GUIStylePreset.TabSubtitle,
            GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Close", GUILayout.Width(72)))
            _builderEditorOpen = false;
        GUILayout.EndHorizontal();

        bool onlySelectedOptions = UIHelpers.Toggle(definition.OnlyShowSelectedOptions, " Only show selected Options");
        if (onlySelectedOptions != definition.OnlyShowSelectedOptions)
        {
            definition.OnlyShowSelectedOptions = onlySelectedOptions;
            CustomTabStore.Save();
        }
        GUILayout.Label(definition.OnlyShowSelectedOptions
            ? "Builder picker: selected options only"
            : "Builder picker: every menu option",
            GUIStylePreset.TabSubtitle);

        CustomTabPage page = definition.EnsurePage();
        _builderPageIndex = Mathf.Clamp(_builderPageIndex, 0, definition.Pages.Count - 1);
        page = definition.Pages[_builderPageIndex];
        page.EnsureColumn();
        _builderColumnIndex = Mathf.Clamp(_builderColumnIndex, 0, page.Columns.Count - 1);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Page " + (_builderPageIndex + 1) + " / " + definition.Pages.Count, GUILayout.Width(110));
        if (GUILayout.Button("Previous", GUILayout.Width(78)) && _builderPageIndex > 0)
            _builderPageIndex--;
        if (GUILayout.Button("Next", GUILayout.Width(78)) && _builderPageIndex < definition.Pages.Count - 1)
            _builderPageIndex++;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        _builderPageNameField.Content = _builderPageName;
        _builderPageNameField.Draw(150, 24);
        _builderPageName = _builderPageNameField.Content;
        if (GUILayout.Button("Rename", GUILayout.Width(66)) && !string.IsNullOrWhiteSpace(_builderPageName))
        {
            page.Name = _builderPageName.Trim();
            _builderPageName = string.Empty;
            CustomTabStore.Save();
        }
        if (GUILayout.Button("+ Page", GUILayout.Width(65)))
        {
            definition.Pages.Add(new CustomTabPage { Name = "Page " + (definition.Pages.Count + 1) });
            _builderPageIndex = definition.Pages.Count - 1;
            _builderColumnIndex = 0;
            CustomTabStore.Save();
        }
        if (definition.Pages.Count > 1 && GUILayout.Button("Delete", GUILayout.Width(62)))
        {
            definition.Pages.RemoveAt(_builderPageIndex);
            _builderPageIndex = Mathf.Clamp(_builderPageIndex, 0, definition.Pages.Count - 1);
            _builderColumnIndex = 0;
            CustomTabStore.Save();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Columns", GUIStylePreset.SectionHeader, GUILayout.Width(90));
        for (int index = 0; index < page.Columns.Count; index++)
        {
            bool selected = index == _builderColumnIndex;
            if (GUILayout.Button(selected ? "[" + (index + 1) + "]" : (index + 1).ToString(),
                selected ? GUIStylePreset.TabButton : GUIStylePreset.NormalButton, GUILayout.Width(38)))
                _builderColumnIndex = index;
        }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Column", GUILayout.Width(82)))
        {
            page.Columns.Add(new CustomTabColumn { Heading = "Column " + (page.Columns.Count + 1) });
            _builderColumnIndex = page.Columns.Count - 1;
            CustomTabStore.Save();
        }
        if (page.Columns.Count > 1 && GUILayout.Button("Delete column", GUILayout.Width(100)))
        {
            page.Columns.RemoveAt(_builderColumnIndex);
            _builderColumnIndex = Mathf.Clamp(_builderColumnIndex, 0, page.Columns.Count - 1);
            CustomTabStore.Save();
        }
        GUILayout.EndHorizontal();

        CustomTabColumn column = page.Columns[_builderColumnIndex];
        GUILayout.BeginHorizontal();
        GUILayout.Label("Heading", GUIStylePreset.SectionHeader, GUILayout.Width(78));
        _builderColumnNameField.Content = _builderColumnName;
        _builderColumnNameField.Draw(190, 24);
        _builderColumnName = _builderColumnNameField.Content;
        if (GUILayout.Button("Apply heading", GUILayout.Width(105)) && !string.IsNullOrWhiteSpace(_builderColumnName))
        {
            column.Heading = _builderColumnName.Trim();
            _builderColumnName = string.Empty;
            CustomTabStore.Save();
        }
        GUILayout.Label("Editing: " + column.Heading, GUIStylePreset.TabSubtitle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Options in this column", GUIStylePreset.SectionHeader, GUILayout.Width(180));
        _builderToggleSearchField.Content = _builderToggleSearch;
        _builderToggleSearchField.Draw(220, 24);
        _builderToggleSearch = _builderToggleSearchField.Content;
        if (GUILayout.Button("Clear", GUILayout.Width(68)))
            _builderToggleSearch = string.Empty;
        GUILayout.EndHorizontal();

        GUILayout.Label(definition.OnlyShowSelectedOptions
            ? "Only selected options are listed. Turn this off to add more options."
            : "All options are listed. Selected options are highlighted.",
            GUIStylePreset.TabSubtitle);

        _builderEditorScrollPosition = UIHelpers.BeginScrollView(_builderEditorScrollPosition, false, true, GUILayout.Height(385));
        string query = _builderToggleSearch.Trim();
        foreach (FieldInfo field in CheatToggles.ToggleFields.Values.OrderBy(field => field.Name))
        {
            if (query.Length > 0 && field.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                FormatToggleName(field.Name).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (definition.OnlyShowSelectedOptions && !column.ToggleNames.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                continue;

            bool included = column.ToggleNames.Contains(field.Name, StringComparer.OrdinalIgnoreCase);
            bool nextIncluded = UIHelpers.Toggle(included, FormatToggleName(field.Name));
            if (nextIncluded != included)
            {
                if (nextIncluded)
                    column.ToggleNames.Add(field.Name);
                else
                    column.ToggleNames.RemoveAll(toggle => string.Equals(toggle, field.Name, StringComparison.OrdinalIgnoreCase));
                CustomTabStore.Save();
            }
        }
        GUILayout.EndScrollView();

        int optionCount = 0;
        foreach (CustomTabColumn previewColumn in page.Columns)
            optionCount += previewColumn.ToggleNames.Count;
        GUILayout.Label("Preview: " + page.Columns.Count + " column(s), " + optionCount + " option(s)",
            GUIStylePreset.TabSubtitle);
        GUILayout.EndArea();
        BlockUnderlyingClicks(new Rect(0f, 0f, _builderEditorRect.width, _builderEditorRect.height));
        HandleWindowDrag(ref _builderEditorRect,
            new Rect(0f, 0f, _builderEditorRect.width, 34f), ref _builderEditorDragging);
    }

    private static void HandleWindowDrag(ref Rect window, Rect dragRegion, ref bool dragging)
    {
        Event current = Event.current;
        if (current == null)
            return;

        if (current.type == EventType.MouseDown && current.button == 0 && dragRegion.Contains(current.mousePosition))
        {
            dragging = true;
            current.Use();
        }
        else if (current.type == EventType.MouseDrag && dragging)
        {
            window.position += current.delta;
            current.Use();
        }
        else if (current.type == EventType.MouseUp && dragging)
        {
            dragging = false;
            current.Use();
        }
    }

    private static void BlockUnderlyingClicks(Rect window)
    {
        if (KEWLMENU.menuAllowClickThrough?.Value == true || Event.current == null ||
            !window.Contains(Event.current.mousePosition))
            return;

        EventType eventType = Event.current.type;
        if (eventType == EventType.MouseDown || eventType == EventType.MouseUp ||
            eventType == EventType.MouseDrag || eventType == EventType.ScrollWheel)
            Event.current.Use();
    }

    private void DisableSabotageCheats()
    {
        CheatToggles.sabotageMap = false;
        CheatToggles.unfixableLights = false;
        CheatToggles.commsSab = false;
        CheatToggles.elecSab = false;
        CheatToggles.reactorSab = false;
        CheatToggles.oxygenSab = false;
        CheatToggles.mushSab = false;
        CheatToggles.mushSpore = false;
        CheatToggles.closeAllDoors = false;
        CheatToggles.openAllDoors = false;
        CheatToggles.spamCloseAllDoors = false;
        CheatToggles.spamOpenAllDoors = false;
    }

    private void DrawToggleTools()
    {
    }

    internal void DrawSearchTabContents()
    {
        GUILayout.Label("Search toggles", GUIStylePreset.TabTitle);
        GUILayout.BeginHorizontal();
        _toggleSearchField.Content = _toggleSearch;
        _toggleSearchField.Draw(240, 24);
        string nextSearch = _toggleSearchField.Content;
        if (!string.Equals(nextSearch, _toggleSearch, StringComparison.Ordinal))
        {
            _toggleSearch = nextSearch;
            ResetMaterialContentScroll();
        }

        if (GUILayout.Button("Clear", GUILayout.Width(72)))
            _toggleSearch = string.Empty;
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);

        if (string.IsNullOrWhiteSpace(_toggleSearch))
        {
            GUILayout.Label("Type a toggle name to search.", GUIStylePreset.TabSubtitle);
            return;
        }

        DrawToggleSearchResults();
    }

    private void DrawToggleSearchResults()
    {
        string query = _toggleSearch.Trim();
        bool selectedCustomTab = _selectedTab >= 0 && _selectedTab < _tabs.Count && _tabs[_selectedTab] is CustomTab;
        CustomTabDefinition selectedDefinition = null;
        if (selectedCustomTab)
        {
            string selectedName = _tabs[_selectedTab].name;
            foreach (CustomTabDefinition definition in CustomTabStore.Definitions)
            {
                if (string.Equals(definition.Name, selectedName, StringComparison.Ordinal))
                {
                    selectedDefinition = definition;
                    break;
                }
            }
        }

        foreach (FieldInfo field in CheatToggles.ToggleFields.Values
                     .Where(field => field.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                     .OrderBy(field => field.Name))
        {
            bool value = (bool)field.GetValue(null);
            GUILayout.BeginHorizontal();
            if (selectedDefinition != null && _editMode)
            {
                CustomTabColumn searchColumn = selectedDefinition.EnsurePage().EnsureColumn();
                bool included = searchColumn.ToggleNames.Contains(field.Name, StringComparer.OrdinalIgnoreCase);
                bool nextIncluded = GUILayout.Toggle(included, string.Empty, GUILayout.Width(24));
                if (nextIncluded != included)
                {
                    if (nextIncluded) searchColumn.ToggleNames.Add(field.Name);
                    else searchColumn.ToggleNames.RemoveAll(name => string.Equals(name, field.Name, StringComparison.OrdinalIgnoreCase));
                    CustomTabStore.Save();
                }
            }
            bool nextValue = UIHelpers.Toggle(value, FormatToggleName(field.Name));
            if (nextValue != value)
                field.SetValue(null, nextValue);
            GUILayout.EndHorizontal();
        }
    }

    private static string FormatToggleName(string toggleName)
    {
        if (string.IsNullOrEmpty(toggleName)) return toggleName;
        return string.Concat(toggleName.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? " " + character : character.ToString())).Trim();
    }

    public void WindowFunction(int windowID)
    {
        GUILayout.BeginHorizontal();

        // Left tab selector (20% width)
        GUILayout.BeginVertical(GUIStylePreset.ModernBox, GUILayout.Width(windowWidth * 0.2f));
        GUILayout.Space(2);

        _tabScrollPosition = UIHelpers.BeginScrollView(_tabScrollPosition, false, true);

        for (var i = 0; i < _tabs.Count; i++)
        {
            if (!IsTabVisible(_tabs[i]))
                continue;
            Color standardColor = GUI.backgroundColor;

            if (_selectedTab == i)
            {
                GUI.backgroundColor = new Color(0.35f, 0.42f, 0.55f, 1f);
            }

            if (GUILayout.Button(_tabs[i].name, GUIStylePreset.TabButton, GUILayout.Height(40)))
                _selectedTab = i;

            GUI.backgroundColor = standardColor;
        }

        GUILayout.EndScrollView();
        GUILayout.Space(4);
        GUILayout.EndVertical();

        GUILayout.Space(10f);

        // Right tab content and controls (80% width)
        GUILayout.BeginVertical(GUIStylePreset.ModernBox, GUILayout.Width(windowWidth * 0.8f));
        GUILayout.Space(2);
        DrawToggleTools();
        
        bool scrollContent = true;
        if (scrollContent)
            _contentScrollPosition = UIHelpers.BeginScrollView(_contentScrollPosition, false, true, GUILayout.Height(windowHeight - 58));

        if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
        {
            GUILayout.Label(_tabs[_selectedTab].name, GUIStylePreset.TabTitle);
            GUILayout.Box("", GUIStylePreset.Separator, GUILayout.Height(2f), GUILayout.ExpandWidth(true));
            GUILayout.Space(6);
            try
            {
                _tabs[_selectedTab].Draw();
            }
            catch (Exception exception)
            {
                GUILayout.Label($"Could not draw tab: {exception.Message}");
            }
        }

        if (scrollContent)
            GUILayout.EndScrollView();

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        BlockUnderlyingClicks(new Rect(0f, 0f, windowRect.width, windowRect.height));
        HandleWindowDrag(ref windowRect, new Rect(0f, 0f, windowRect.width, 40f), ref _menuDragging);
    }

    private void AlternativeWindowFunction(int windowID)
    {
        Color background;
        Color sidebar;
        Color content;
        Color accent;
        Color text;
        GetAlternativeTheme(out background, out sidebar, out content, out accent, out text);

        if (_materialRenderedTab != _selectedTab)
        {
            _materialRenderedTab = _selectedTab;
            _materialPageProgress = PageAnimationsEnabled ? 0f : 1f;
            _materialContentScrollPosition = Vector2.zero;
            _materialContentScrollTarget = Vector2.zero;
        }
        if (PageAnimationsEnabled)
        {
            float step = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f);
            _materialPageProgress = Mathf.Lerp(_materialPageProgress, 1f, step);
            if (_materialPageProgress < 0.999f)
                GUI.changed = true;
        }

        GUI.color = background;
        GUI.Box(new Rect(0f, 0f, windowRect.width, windowRect.height), GUIContent.none);
        if (CurrentTheme == "Refract")
            DrawRefractAtmosphere(windowRect.width, windowRect.height, accent);
        GUILayout.BeginArea(new Rect(12f, 42f, windowRect.width - 24f, windowRect.height - 54f));
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = sidebar;
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(windowWidth * 0.2f));
        Vector2 tabScrollInput = UIHelpers.BeginScrollView(_tabScrollPosition, false, true);
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (!IsTabVisible(_tabs[i])) continue;
            GUI.backgroundColor = _selectedTab == i ? accent : sidebar;
            if (GUILayout.Button(_tabs[i].name, GUILayout.Height(30f)))
                _selectedTab = i;
        }
        GUILayout.EndScrollView();
        UpdateMaterialScroll(ref _tabScrollPosition, ref _materialTabScrollTarget, tabScrollInput);
        GUILayout.EndVertical();

        GUILayout.Space(10f);
        GUI.backgroundColor = content;
        GUILayout.BeginVertical(GUI.skin.box);
        GUI.color = new Color(1f, 1f, 1f, _materialPageProgress);
        GUI.contentColor = text;
        GUILayout.Label(_tabs[_selectedTab].name, GUIStylePreset.TabTitle);
        Vector2 contentScrollInput = UIHelpers.BeginScrollView(_contentScrollPosition, false, true);
        try
        {
            _tabs[_selectedTab].Draw();
        }
        catch (Exception exception)
        {
            GUILayout.Label($"Could not draw tab: {exception.Message}");
        }
        GUILayout.EndScrollView();
        UpdateMaterialScroll(ref _contentScrollPosition, ref _materialContentScrollTarget, contentScrollInput);
        GUILayout.EndVertical();
        GUI.color = Color.white;
        GUI.contentColor = Color.white;
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        BlockUnderlyingClicks(new Rect(0f, 0f, windowRect.width, windowRect.height));
        HandleWindowDrag(ref windowRect, new Rect(0f, 0f, windowRect.width, 40f), ref _menuDragging);
    }

    private static void GetAlternativeTheme(out Color background, out Color sidebar, out Color content, out Color accent, out Color text)
    {
        switch (CurrentTheme)
        {
            case "Glass":
                GetGlassColors(out Color glassStart, out Color glassEnd);
                float glassOpacity = GlassOpacity;
                float tintStrength = GlassTintStrength;
                glassStart = Color.Lerp(new Color(0.12f, 0.14f, 0.18f, 1f), glassStart, tintStrength);
                glassEnd = Color.Lerp(new Color(0.12f, 0.14f, 0.18f, 1f), glassEnd, tintStrength);
                background = new Color(0.12f, 0.14f, 0.18f, glassOpacity);
                sidebar = new Color(glassStart.r, glassStart.g, glassStart.b, glassOpacity * 0.18f);
                content = new Color(glassEnd.r, glassEnd.g, glassEnd.b, glassOpacity * 0.12f);
                accent = new Color(glassStart.r, glassStart.g, glassStart.b, Mathf.Clamp01(glassOpacity * 0.72f));
                text = new Color(0.96f, 0.98f, 1f, 1f);
                break;
            case "Refract":
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.35f);
                background = new Color(0.025f, 0.075f, 0.095f, 0.92f);
                sidebar = new Color(0.04f, 0.16f, 0.18f, 0.78f);
                content = new Color(0.07f, 0.11f, 0.15f, 0.68f);
                accent = Color.Lerp(new Color(0.15f, 0.95f, 0.86f, 1f), new Color(1f, 0.43f, 0.58f, 1f), pulse);
                text = new Color(0.92f, 1f, 0.98f, 1f);
                break;
            case "Paper":
                background = new Color(0.86f, 0.84f, 0.78f, 1f);
                sidebar = new Color(0.73f, 0.70f, 0.63f, 1f);
                content = new Color(0.95f, 0.93f, 0.87f, 1f);
                accent = new Color(0.16f, 0.34f, 0.32f, 1f);
                text = new Color(0.10f, 0.12f, 0.12f, 1f);
                break;
            case "Terminal":
                background = new Color(0.015f, 0.02f, 0.018f, 1f);
                sidebar = new Color(0.025f, 0.06f, 0.035f, 1f);
                content = new Color(0.02f, 0.035f, 0.025f, 1f);
                accent = new Color(0.22f, 0.95f, 0.42f, 1f);
                text = new Color(0.62f, 1f, 0.70f, 1f);
                break;
            default:
                background = new Color(0.045f, 0.02f, 0.075f, 1f);
                sidebar = new Color(0.10f, 0.025f, 0.16f, 1f);
                content = new Color(0.075f, 0.025f, 0.12f, 1f);
                accent = new Color(1f, 0.20f, 0.62f, 1f);
                text = new Color(1f, 0.90f, 0.96f, 1f);
                break;
        }
    }

    private static void GetGlassColors(out Color start, out Color end)
    {
        string theme = KEWLMENU.glassTintTheme?.Value ?? "Clear";
        switch (theme)
        {
            case "Red": start = HexColor("#FF6B7A"); end = HexColor("#FFB3C1"); break;
            case "Orange": start = HexColor("#FF9A62"); end = HexColor("#FFD1A8"); break;
            case "Yellow": start = HexColor("#FFD966"); end = HexColor("#FFF4B0"); break;
            case "Green": start = HexColor("#6FE7A0"); end = HexColor("#C4FFD9"); break;
            case "Cyan": start = HexColor("#67E8F9"); end = HexColor("#CFFAFE"); break;
            case "Blue": start = HexColor("#72A7FF"); end = HexColor("#C9DDFF"); break;
            case "Violet": start = HexColor("#B794F4"); end = HexColor("#E9D5FF"); break;
            case "Rainbow":
                start = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.06f, 1f), 0.68f, 1f);
                end = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.06f + 0.20f, 1f), 0.42f, 1f);
                break;
            case "Sunset Gradient": start = HexColor("#FF6B6B"); end = HexColor("#FFD166"); break;
            case "Ocean Gradient": start = HexColor("#4FACFE"); end = HexColor("#00F2FE"); break;
            case "Forest Gradient": start = HexColor("#2E7D62"); end = HexColor("#B7E4C7"); break;
            case "Custom Gradient":
                start = HexColor(KEWLMENU.glassGradientStart?.Value);
                end = HexColor(KEWLMENU.glassGradientEnd?.Value);
                break;
            default: start = HexColor("#8FD3FF"); end = HexColor("#D6F1FF"); break;
        }
    }

    private static void DrawRefractAtmosphere(float width, float height, Color accent)
    {
        float t = Mathf.Repeat(Time.unscaledTime * 0.12f, 1f);
        Color cyan = new Color(0.16f, 0.95f, 0.88f, 0.08f);
        Color rose = new Color(1f, 0.35f, 0.58f, 0.06f);
        GUI.color = cyan;
        GUI.Box(new Rect(-width * 0.35f + t * width * 1.7f, 0f, width * 0.28f, height), GUIContent.none);
        GUI.color = rose;
        GUI.Box(new Rect(width * 0.78f - t * width * 1.35f, 0f, width * 0.20f, height), GUIContent.none);
        GUI.color = new Color(accent.r, accent.g, accent.b, 0.10f);
        GUI.Box(new Rect(0f, 0f, width, 2f), GUIContent.none);
        GUI.Box(new Rect(0f, height - 2f, width, 2f), GUIContent.none);
        GUI.color = Color.white;
    }

    private static Color HexColor(string value)
    {
        return ColorUtility.TryParseHtmlString(value, out Color color) ? color : new Color(0.56f, 0.83f, 1f, 1f);
    }

    private void MaterialWindowFunction(int windowID)
    {
        GUISkin previousSkin = GUI.skin;
        GUI.skin = GetMaterialSkin(_legacySkin ?? previousSkin);
        Color oldBackground = GUI.backgroundColor;
        Color oldContent = GUI.contentColor;
        Color oldColor = GUI.color;
        GUI.color = Color.white;
        GUI.contentColor = MaterialText;

        float width = windowRect.width;
        float height = windowRect.height;
        float sidebarWidth = Mathf.Clamp(width * 0.20f, 122f, 160f);
        float bodyX = sidebarWidth + 18f;
        float bodyWidth = width - bodyX - 12f;
        float bodyHeight = height - 48f;
        if (_materialRenderedTab != _selectedTab)
        {
            _materialRenderedTab = _selectedTab;
            _materialPageProgress = PageAnimationsEnabled ? 0f : 1f;
            _contentScrollPosition = Vector2.zero;
            _materialContentScrollPosition = Vector2.zero;
            _materialContentScrollTarget = Vector2.zero;
        }
        float pageEase = 1f;
        if (PageAnimationsEnabled)
        {
            float animationStep = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 10f);
            _materialPageProgress = Mathf.Lerp(_materialPageProgress, 1f, animationStep);
            pageEase = Mathf.SmoothStep(0f, 1f, _materialPageProgress);
        }
        if (_materialPageProgress < 0.999f)
            GUI.changed = true;

        GUI.Box(new Rect(0f, 0f, width, height), GUIContent.none, CreatePanelStyle(MaterialSurface));
        GUI.Box(new Rect(0f, 0f, width, 34f), GUIContent.none, CreatePanelStyle(MaterialHeader));
        GUI.Label(new Rect(18f, 6f, Mathf.Max(280f, width - 190f), 24f), "KEWLMENU BY OMQITZRLYJAMES // V1.3.0", CreateHeaderStyle());
        GUI.Label(new Rect(width - 120f, 8f, 100f, 20f), Utils.isPlayer ? "IN GAME" : "LOBBY", MaterialStatusStyle());
        GUI.Box(new Rect(0f, 34f, width, 3f), GUIContent.none, CreateGradientStyle(MaterialAccent, MaterialAccentEnd));

        GUI.Box(new Rect(10f, 48f, sidebarWidth, bodyHeight), GUIContent.none, CreatePanelStyle(MaterialNavigation));
        GUILayout.BeginArea(new Rect(18f, 56f, sidebarWidth - 16f, bodyHeight - 12f));
        GUILayout.Label("NAVIGATION", MaterialCaptionStyle());
        Vector2 tabScrollInput = UIHelpers.BeginScrollView(_materialTabScrollPosition, false, true);
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (!IsTabVisible(_tabs[i]))
                continue;
            if (GUILayout.Button(_tabs[i].name, _selectedTab == i ? CreateSelectedNavigationStyle() : CreateNavigationStyle(), GUILayout.Height(27)))
                _selectedTab = i;
        }
        GUILayout.EndScrollView();
        UpdateMaterialScroll(ref _materialTabScrollPosition, ref _materialTabScrollTarget, tabScrollInput);
        GUILayout.EndArea();

        GUI.Box(new Rect(bodyX, 48f, bodyWidth, bodyHeight), GUIContent.none, CreatePanelStyle(MaterialContent));
        GUILayout.BeginArea(new Rect(bodyX + 14f, 58f + Mathf.Lerp(10f, 0f, pageEase), bodyWidth - 28f, bodyHeight - 18f));
        if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
        {
            Color pageColor = GUI.color;
            GUI.color = new Color(pageColor.r, pageColor.g, pageColor.b, pageEase);
            GUILayout.BeginHorizontal(CreatePageHeaderStyle(), GUILayout.Height(54));
            GUILayout.BeginVertical();
            GUILayout.Label(GetMaterialPageTitle(), CreateContentTitleStyle());
            GUILayout.Label("gg/bH4Hy9YnVD", MaterialCaptionStyle());
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Box(string.Empty, CreateGradientStyle(MaterialAccent, MaterialAccentEnd), GUILayout.Height(2), GUILayout.ExpandWidth(true));
            GUILayout.Space(8);
            DrawToggleTools();
            GUILayout.BeginVertical(CreateWorkSurfaceStyle());
            Vector2 contentScrollInput = UIHelpers.BeginScrollView(_materialContentScrollPosition, false, true,
                GUILayout.Height(bodyHeight - 98f));
            _tabs[_selectedTab].Draw();
            GUILayout.EndScrollView();
            UpdateMaterialScroll(ref _materialContentScrollPosition, ref _materialContentScrollTarget, contentScrollInput);
            GUILayout.EndVertical();
            GUI.color = pageColor;
        }
        GUILayout.EndArea();

        GUI.backgroundColor = oldBackground;
        GUI.contentColor = oldContent;
        GUI.color = oldColor;
        GUI.skin = previousSkin;
        HandleWindowDrag(ref windowRect, new Rect(0f, 0f, windowRect.width, 40f), ref _menuDragging);
    }

    private void DrawMaterialResizeGrip()
    {
        const float gripSize = 22f;
        Rect grip = new(windowRect.xMax - gripSize, windowRect.yMax - gripSize, gripSize, gripSize);
        int controlId = GUIUtility.GetControlID(148237, FocusType.Passive);
        Event current = Event.current;

        if (current.type == EventType.MouseDown && current.button == 0 && grip.Contains(current.mousePosition))
        {
            _materialResizing = true;
            _materialResizeStart = current.mousePosition;
            _materialResizeOrigin = new Vector2(windowRect.width, windowRect.height);
            GUIUtility.hotControl = controlId;
            current.Use();
        }
        else if (_materialResizing && current.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
        {
            Vector2 delta = current.mousePosition - _materialResizeStart;
            windowRect.width = Mathf.Clamp(_materialResizeOrigin.x + delta.x, 520f, Screen.width - 20f);
            windowRect.height = Mathf.Clamp(_materialResizeOrigin.y + delta.y, 360f, Screen.height - 20f);
            GUI.changed = true;
            current.Use();
        }
        else if (_materialResizing && current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
        {
            _materialResizing = false;
            GUIUtility.hotControl = 0;
            current.Use();
        }

        Color previousColor = GUI.color;
        GUI.color = MaterialAccent;
        _materialResizeGripTexture ??= Texture2D.whiteTexture;
        GUI.Box(new Rect(windowRect.xMax - 24f, windowRect.yMax - 24f, 20f, 20f),
            GUIContent.none, CreateTextureStyle(_materialResizeGripTexture));
        GUI.color = previousColor;
    }

    private static GUIStyle CreateTextureStyle(Texture2D texture)
    {
        string key = "texture:" + texture.GetInstanceID();
        if (MaterialStyles.TryGetValue(key, out GUIStyle cached))
            return cached;

        GUIStyle style = new GUIStyle
        {
            normal = { background = texture },
            padding = new RectOffset(),
            margin = new RectOffset(),
            border = new RectOffset()
        };
        MaterialStyles[key] = style;
        return style;
    }

    private static void UpdateMaterialScroll(ref Vector2 position, ref Vector2 target, Vector2 input)
    {
        if ((input - position).sqrMagnitude > 0.01f)
            target = input;

        if (!SmoothScrollingEnabled)
        {
            position = target;
            return;
        }

        float scrollStep = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 14f);
        Vector2 nextPosition = Vector2.Lerp(position, target, scrollStep);
        if ((nextPosition - position).sqrMagnitude > 0.01f)
            GUI.changed = true;
        position = nextPosition;
    }

    private static bool SmoothScrollingEnabled => KEWLMENU.menuMaterialSmoothScrolling?.Value != false;

    private static bool PageAnimationsEnabled =>
        KEWLMENU.menuMaterialLowPerformance?.Value != true &&
        KEWLMENU.menuMaterialPageAnimations?.Value != false;

    private string GetMaterialPageTitle()
    {
        return _tabs[_selectedTab] switch
        {
            RolesTab roles => $"Roles / {roles.MaterialSectionName}",
            LobbyTab lobby => $"Lobby / {lobby.MaterialSectionName}",
            _ => _tabs[_selectedTab].name
        };
    }

    private static Color MaterialSurface => DarkerTheme ? new Color(0.018f, 0.019f, 0.021f, 1f) : new Color(0.045f, 0.048f, 0.052f, 1f);
    private static Color MaterialHeader => DarkerTheme ? new Color(0.045f, 0.047f, 0.050f, 1f) : new Color(0.085f, 0.090f, 0.098f, 1f);
    private static Color MaterialNavigation => DarkerTheme ? new Color(0.028f, 0.030f, 0.033f, 1f) : new Color(0.065f, 0.070f, 0.078f, 1f);
    private static Color MaterialContent => DarkerTheme ? new Color(0.060f, 0.063f, 0.067f, 1f) : new Color(0.105f, 0.110f, 0.118f, 1f);
    private static Color MaterialAccent = new(0.78f, 0.80f, 0.83f, 1f);
    private static Color MaterialAccentEnd = new(0.43f, 0.46f, 0.50f, 1f);
    private static readonly Color MaterialText = new(0.91f, 0.92f, 0.94f, 1f);

    private static GUIStyle CreatePanelStyle(Color color)
    {
        string key = "panel:" + color.ToString();
        if (MaterialStyles.TryGetValue(key, out GUIStyle cached))
            return cached;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            normal = { background = CreateRoundedTexture(color) },
            padding = new RectOffset { left = 12, right = 12, top = 10, bottom = 10 },
            margin = new RectOffset(),
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 }
        };
        MaterialStyles[key] = style;
        return style;
    }

    private static GUIStyle CreateGradientStyle(Color start, Color end)
    {
        string key = "gradient:" + start.ToString() + end.ToString();
        if (MaterialStyles.TryGetValue(key, out GUIStyle cached))
            return cached;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            normal = { background = CreateGradientTexture(start, end) },
            padding = new RectOffset(),
            margin = new RectOffset(),
            border = new RectOffset()
        };
        MaterialStyles[key] = style;
        return style;
    }

    private static GUIStyle CreateNavigationStyle(Color unused = default)
    {
        const string key = "navigation";
        if (MaterialStyles.TryGetValue(key, out GUIStyle cached))
            return cached;

        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            normal = { background = CreateRoundedTexture(MaterialNavigation), textColor = new Color(0.70f, 0.71f, 0.73f, 1f) },
            hover = { textColor = Color.white },
            active = { textColor = Color.white },
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset { left = 12, right = 8, top = 5, bottom = 5 },
            margin = new RectOffset { left = 0, right = 0, top = 2, bottom = 2 },
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
            wordWrap = false
        };
        MaterialStyles[key] = style;
        return style;
    }

    private static GUIStyle CreateSelectedNavigationStyle()
    {
        const string key = "navigation:selected";
        if (MaterialStyles.TryGetValue(key, out GUIStyle cached))
            return cached;

        GUIStyle style = new GUIStyle(CreateNavigationStyle());
        style.normal.background = CreateRoundedTexture(MaterialAccent);
        style.normal.textColor = Color.white;
        MaterialStyles[key] = style;
        return style;
    }

    public static GUIStyle CreateMaterialTabStyle(bool selected)
    {
        string key = selected ? "material-tab:selected" : "material-tab";
        if (MaterialStyles.TryGetValue(key, out GUIStyle cached))
            return cached;

        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.70f, 0.71f, 0.73f, 1f) },
            hover = { textColor = Color.white },
            active = { textColor = Color.white },
            padding = new RectOffset { left = 8, right = 8, top = 5, bottom = 5 },
            margin = new RectOffset(),
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
            wordWrap = false,
            fixedHeight = 28f
        };
        if (selected)
        {
            style.normal.background = CreateRoundedTexture(MaterialAccent);
            style.normal.textColor = Color.white;
        }
        MaterialStyles[key] = style;
        return style;
    }

    public static GUIStyle CreateMaterialToggleStyle(bool selected)
    {
        string key = selected ? "material-toggle:selected" : "material-toggle";
        if (MaterialStyles.TryGetValue(key, out GUIStyle cached))
            return cached;

        Color transparent = new Color(0f, 0f, 0f, 0f);
        Color selectedColor = Color.Lerp(MaterialAccent, MaterialSurface, 0.42f);
        Color selectedHoverColor = Color.Lerp(MaterialAccentEnd, MaterialSurface, 0.36f);
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { background = CreateRoundedTexture(selected ? selectedColor : transparent), textColor = selected ? Color.white : MaterialText },
            hover = { background = CreateRoundedTexture(selected ? selectedHoverColor : MaterialNavigation), textColor = Color.white },
            active = { background = CreateRoundedTexture(selectedHoverColor), textColor = Color.white },
            padding = new RectOffset { left = 12, right = 8, top = 5, bottom = 5 },
            margin = new RectOffset { left = 2, right = 2, top = 2, bottom = 2 },
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
            wordWrap = false,
            fixedHeight = 28f
        };
        MaterialStyles[key] = style;
        return style;
    }

    private static GUIStyle CreateHeaderStyle() => GetCachedStyle("header", static () => new GUIStyle(GUI.skin.label)
    {
        fontSize = 21,
        fontStyle = FontStyle.Bold,
        normal = { textColor = MaterialText },
        padding = new RectOffset(),
        margin = new RectOffset()
    });

    private static GUIStyle CreateContentTitleStyle() => GetCachedStyle("content-title", static () => new GUIStyle(GUI.skin.label)
    {
        fontSize = 20,
        fontStyle = FontStyle.Bold,
        normal = { textColor = MaterialText },
        padding = new RectOffset { bottom = 4 },
        margin = new RectOffset()
    });

    private static GUIStyle CreatePageHeaderStyle() => GetCachedStyle("page-header", static () => new GUIStyle(GUI.skin.box)
    {
        normal = { background = CreateRoundedTexture(new Color(0.080f, 0.083f, 0.087f, 1f)) },
        padding = new RectOffset { left = 10, right = 8, top = 6, bottom = 6 },
        margin = new RectOffset(),
        border = new RectOffset()
    });

    private static GUIStyle CreatePageNumberStyle() => GetCachedStyle("page-number", static () => new GUIStyle(GUI.skin.label)
    {
        fontSize = 24,
        fontStyle = FontStyle.Bold,
        alignment = TextAnchor.MiddleLeft,
        normal = { textColor = MaterialAccent },
        padding = new RectOffset(),
        margin = new RectOffset()
    });

    private static GUIStyle CreateWorkSurfaceStyle() => GetCachedStyle("work-surface", static () => new GUIStyle(GUI.skin.box)
    {
        normal = { background = CreateRoundedTexture(new Color(0.032f, 0.034f, 0.037f, 1f)) },
        padding = new RectOffset { left = 10, right = 10, top = 8, bottom = 8 },
        margin = new RectOffset(),
        border = new RectOffset()
    });

    private static GUIStyle MaterialCaptionStyle() => GetCachedStyle("caption", static () => new GUIStyle(GUI.skin.label)
    {
        fontSize = 10,
        fontStyle = FontStyle.Bold,
        normal = { textColor = new Color(0.56f, 0.57f, 0.60f, 1f) },
        padding = new RectOffset(),
        margin = new RectOffset { bottom = 2 }
    });

    private static GUIStyle MaterialStatusStyle() => GetCachedStyle("status", static () => new GUIStyle(GUI.skin.label)
    {
        fontSize = 11,
        fontStyle = FontStyle.Bold,
        alignment = TextAnchor.MiddleCenter,
        normal = { textColor = MaterialAccent },
        padding = new RectOffset { left = 10, right = 10, top = 5, bottom = 5 },
        margin = new RectOffset()
    });

    private static GUIStyle GetCachedStyle(string key, Func<GUIStyle> factory)
    {
        if (MaterialStyles.TryGetValue(key, out GUIStyle cached))
            return cached;

        GUIStyle style = factory();
        MaterialStyles[key] = style;
        return style;
    }

    private static Texture2D CreateTexture(Color color)
    {
        color.a *= MaterialWindowOpacity;
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static Texture2D CreateRoundedTexture(Color color)
    {
        if (RoundedTextures.TryGetValue(color, out Texture2D cached))
            return cached;

        const int size = 32;
        const float radius = 7f;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, 0f);
                dx = Mathf.Max(dx, x - (size - radius - 1f));
                float dy = Mathf.Max(radius - y, 0f);
                dy = Mathf.Max(dy, y - (size - radius - 1f));
                Color pixel = color;
                pixel.a *= MaterialWindowOpacity * Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, pixel);
            }
        }
        texture.Apply();
        RoundedTextures[color] = texture;
        return texture;
    }

    public static int MaterialColorPreset { get; private set; } = 8;
    private static float _materialWindowOpacity = 0.94f;
    public static float MaterialWindowOpacity
    {
        get => _materialWindowOpacity;
        set
        {
            if (Mathf.Approximately(_materialWindowOpacity, value))
                return;

            _materialWindowOpacity = value;
            ClearMaterialStyleCache();
            _materialSkin = null;
            _materialSkinSource = null;
        }
    }

    private static void ClearMaterialStyleCache()
    {
        if (_materialSkin != null)
        {
            UnityEngine.Object.Destroy(_materialSkin);
            _materialSkin = null;
            _materialSkinSource = null;
        }

        foreach (Texture2D texture in RoundedTextures.Values)
            UnityEngine.Object.Destroy(texture);
        foreach (Texture2D texture in GradientTextures.Values)
            UnityEngine.Object.Destroy(texture);
        foreach (Texture2D texture in SliderRailTextures.Values)
            UnityEngine.Object.Destroy(texture);

        MaterialStyles.Clear();
        RoundedTextures.Clear();
        GradientTextures.Clear();
        SliderRailTextures.Clear();
    }

    private static void DestroyAlternativeSkin()
    {
        if (_alternativeSkin != null)
        {
            UnityEngine.Object.Destroy(_alternativeSkin);
            _alternativeSkin = null;
        }

        foreach (Texture2D texture in AlternativeTextures)
        {
            if (texture != null)
                UnityEngine.Object.Destroy(texture);
        }

        AlternativeTextures.Clear();
        _alternativeSkinTheme = null;
    }

    public static Color GetMaterialAccentColor()
    {
        return MaterialAccent;
    }

    public static Color GetThemeAccentColor()
    {
        if (CurrentTheme == "Material")
            return MaterialAccent;

        GetAlternativeTheme(out _, out _, out _, out Color accent, out _);
        return accent;
    }

    public static void ResetMaterialContentScroll()
    {
        if (KEWLMENU.menuUI != null)
        {
            KEWLMENU.menuUI._materialContentScrollPosition = Vector2.zero;
            KEWLMENU.menuUI._materialContentScrollTarget = Vector2.zero;
        }
    }

    public static readonly string[] MaterialColorPresets =
        { "Teal / Amber", "Ocean / Lime", "Cardinal / Gold", "Violet / Rose", "Cobalt / Mint", "Copper / Sky", "Mono / Ice", "Plum / Peach", "Graphite / Coral" };

    public static void SetMaterialColorPreset(int preset)
    {
        MaterialColorPreset = Mathf.Clamp(preset, 0, MaterialColorPresets.Length - 1);
        switch (MaterialColorPreset)
        {
            case 1:
                MaterialAccent = new Color(0.12f, 0.60f, 0.95f, 1f);
                MaterialAccentEnd = new Color(0.45f, 0.82f, 0.28f, 1f);
                break;
            case 2:
                MaterialAccent = new Color(0.92f, 0.24f, 0.30f, 1f);
                MaterialAccentEnd = new Color(0.98f, 0.68f, 0.18f, 1f);
                break;
            case 3:
                MaterialAccent = new Color(0.62f, 0.34f, 0.94f, 1f);
                MaterialAccentEnd = new Color(0.98f, 0.34f, 0.62f, 1f);
                break;
            case 4:
                MaterialAccent = new Color(0.16f, 0.42f, 0.95f, 1f);
                MaterialAccentEnd = new Color(0.22f, 0.92f, 0.74f, 1f);
                break;
            case 5:
                MaterialAccent = new Color(0.90f, 0.38f, 0.16f, 1f);
                MaterialAccentEnd = new Color(0.24f, 0.70f, 0.94f, 1f);
                break;
            case 6:
                MaterialAccent = new Color(0.72f, 0.76f, 0.80f, 1f);
                MaterialAccentEnd = new Color(0.25f, 0.78f, 0.92f, 1f);
                break;
            case 7:
                MaterialAccent = new Color(0.72f, 0.25f, 0.62f, 1f);
                MaterialAccentEnd = new Color(1.00f, 0.58f, 0.38f, 1f);
                break;
            case 8:
                MaterialAccent = new Color(0.78f, 0.80f, 0.83f, 1f);
                MaterialAccentEnd = new Color(0.43f, 0.46f, 0.50f, 1f);
                break;
            default:
                MaterialAccent = new Color(0.10f, 0.78f, 0.68f, 1f);
                MaterialAccentEnd = new Color(0.95f, 0.55f, 0.25f, 1f);
                break;
        }
        UIHelpers.ResetMaterialStyles();
        ClearMaterialStyleCache();
        _materialSkin = null;
        _materialSkinSource = null;
    }

    private static Texture2D CreateSliderRailTexture(Color color)
    {
                    if (SliderRailTextures.TryGetValue(color, out Texture2D cached))
                        return cached;

                    const int size = 32;
                    const int railTop = 12;
                    const int railBottom = 20;
                    Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            bool insideRail = y >= railTop && y < railBottom;
                            float edgeDistance = Mathf.Min(Mathf.Min(x, size - 1 - x),
                                Mathf.Min(y - railTop, railBottom - 1 - y));
                            float alpha = insideRail ? Mathf.Clamp01(edgeDistance + 1f) : 0f;
                            texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * MaterialWindowOpacity * alpha));
                        }
                    }
                    texture.Apply();
                    SliderRailTextures[color] = texture;
                    return texture;
                }

    private static Texture2D CreateGradientTexture(Color start, Color end)
    {
        string key = start.ToString() + end.ToString();
        if (GradientTextures.TryGetValue(key, out Texture2D cached))
            return cached;

        start.a *= MaterialWindowOpacity;
        end.a *= MaterialWindowOpacity;
        Texture2D texture = new(2, 1);
        texture.SetPixel(0, 0, start);
        texture.SetPixel(1, 0, end);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        GradientTextures[key] = texture;
        return texture;
    }

    private static Texture2D CreateRoundedGradientTexture(Color start, Color end)
    {
        const int size = 32;
        const float radius = 7f;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, 0f);
                dx = Mathf.Max(dx, x - (size - radius - 1f));
                float dy = Mathf.Max(radius - y, 0f);
                dy = Mathf.Max(dy, y - (size - radius - 1f));
                Color pixel = Color.Lerp(start, end, (float)x / (size - 1));
                pixel.a *= MaterialWindowOpacity * Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, pixel);
            }
        }
        texture.Apply();
        return texture;
    }

    private static GUISkin _materialSkin;
    private static GUISkin _materialSkinSource;
    private static GUISkin _alternativeSkin;
    private static string _alternativeSkinTheme;
    private static Texture2D _materialResizeGripTexture;

    private static GUISkin GetMaterialSkin(GUISkin source)
    {
        if (_materialSkin != null && _materialSkinSource == source)
            return _materialSkin;

        _materialSkin = UnityEngine.Object.Instantiate(source);
        _materialSkinSource = source;
        _materialSkin.name = "SocietyArgonMaterialSkin";
        _materialSkin.window = new GUIStyle(source.window)
        {
            padding = new RectOffset(),
            margin = new RectOffset(),
            border = new RectOffset(),
            normal = { background = CreateRoundedTexture(MaterialSurface) }
        };

        _materialSkin.button = new GUIStyle(source.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset { left = 10, right = 10, top = 6, bottom = 6 },
            margin = new RectOffset { left = 2, right = 2, top = 3, bottom = 3 },
            normal = { background = CreateRoundedTexture(new Color(0.12f, 0.16f, 0.22f, 1f)), textColor = MaterialText },
            hover = { background = CreateRoundedGradientTexture(MaterialAccent, MaterialAccentEnd), textColor = Color.white },
            active = { background = CreateRoundedTexture(MaterialAccentEnd), textColor = Color.white },
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 }
        };

        _materialSkin.toggle = new GUIStyle(source.toggle)
        {
            fontSize = 13,
            padding = new RectOffset { left = 22, right = 8, top = 5, bottom = 5 },
            margin = new RectOffset { left = 2, right = 2, top = 2, bottom = 2 },
            normal = { textColor = new Color(0.76f, 0.84f, 0.92f, 1f) },
            onNormal = { textColor = Color.white },
            hover = { textColor = Color.white },
            onHover = { textColor = Color.white }
        };

        _materialSkin.label = new GUIStyle(source.label)
        {
            fontSize = 13,
            normal = { textColor = MaterialText },
            padding = new RectOffset { left = 4, right = 4, top = 3, bottom = 3 },
            margin = new RectOffset { left = 2, right = 2, top = 1, bottom = 1 },
            wordWrap = true
        };

        _materialSkin.box = new GUIStyle(source.box)
        {
            normal = { background = CreateRoundedTexture(MaterialContent) },
            padding = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
            margin = new RectOffset { left = 2, right = 2, top = 2, bottom = 2 },
            border = new RectOffset()
        };
        _materialSkin.scrollView = new GUIStyle()
        {
            normal = { background = CreateRoundedTexture(MaterialContent) },
            padding = new RectOffset(),
            margin = new RectOffset(),
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 }
        };

        _materialSkin.horizontalSlider = new GUIStyle()
        {
            normal = { background = CreateSliderRailTexture(new Color(0.18f, 0.25f, 0.34f, 1f)) },
            hover = { background = CreateSliderRailTexture(new Color(0.22f, 0.30f, 0.40f, 1f)) },
            active = { background = CreateSliderRailTexture(new Color(0.25f, 0.34f, 0.44f, 1f)) },
            focused = { background = CreateSliderRailTexture(new Color(0.25f, 0.34f, 0.44f, 1f)) },
            margin = new RectOffset(),
            fixedHeight = 18
        };
        _materialSkin.horizontalSliderThumb = new GUIStyle()
        {
            normal = { background = CreateRoundedTexture(MaterialAccent) },
            hover = { background = CreateRoundedTexture(MaterialAccentEnd) },
            active = { background = CreateRoundedTexture(Color.white) },
            focused = { background = CreateRoundedTexture(MaterialAccentEnd) },
            margin = new RectOffset(),
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
            fixedWidth = 18,
            fixedHeight = 18
        };
        _materialSkin.horizontalScrollbar = new GUIStyle()
        {
            normal = { background = CreateRoundedTexture(new Color(0.10f, 0.14f, 0.19f, 1f)) },
            hover = { background = CreateRoundedTexture(new Color(0.14f, 0.19f, 0.25f, 1f)) },
            active = { background = CreateRoundedTexture(new Color(0.18f, 0.24f, 0.31f, 1f)) },
            focused = { background = CreateRoundedTexture(new Color(0.18f, 0.24f, 0.31f, 1f)) },
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
            fixedHeight = 10,
            stretchHeight = false
        };
        _materialSkin.horizontalScrollbarThumb = new GUIStyle()
        {
            normal = { background = CreateRoundedTexture(MaterialAccent) },
            hover = { background = CreateRoundedTexture(MaterialAccentEnd) },
            active = { background = CreateRoundedTexture(Color.white) },
            focused = { background = CreateRoundedTexture(MaterialAccentEnd) },
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
            fixedWidth = 10,
            fixedHeight = 10,
            stretchHeight = false
        };
        _materialSkin.verticalScrollbar = new GUIStyle()
        {
            normal = { background = CreateRoundedTexture(new Color(0.10f, 0.14f, 0.19f, 1f)) },
            hover = { background = CreateRoundedTexture(new Color(0.14f, 0.19f, 0.25f, 1f)) },
            active = { background = CreateRoundedTexture(new Color(0.18f, 0.24f, 0.31f, 1f)) },
            focused = { background = CreateRoundedTexture(new Color(0.18f, 0.24f, 0.31f, 1f)) },
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
            fixedWidth = 10,
            stretchWidth = false
        };
        _materialSkin.verticalScrollbarThumb = new GUIStyle()
        {
            normal = { background = CreateRoundedTexture(MaterialAccent) },
            hover = { background = CreateRoundedTexture(MaterialAccentEnd) },
            active = { background = CreateRoundedTexture(Color.white) },
            focused = { background = CreateRoundedTexture(MaterialAccentEnd) },
            border = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
            fixedWidth = 10,
            fixedHeight = 10,
            stretchWidth = false
        };

        ApplyMaterialState(_materialSkin.horizontalSlider, _materialSkin.horizontalSlider.normal.background,
            _materialSkin.horizontalSlider.hover.background, _materialSkin.horizontalSlider.active.background);
        ApplyMaterialState(_materialSkin.horizontalSliderThumb, _materialSkin.horizontalSliderThumb.normal.background,
            _materialSkin.horizontalSliderThumb.hover.background, _materialSkin.horizontalSliderThumb.active.background);
        ApplyMaterialState(_materialSkin.horizontalScrollbar, _materialSkin.horizontalScrollbar.normal.background,
            _materialSkin.horizontalScrollbar.hover.background, _materialSkin.horizontalScrollbar.active.background);
        ApplyMaterialState(_materialSkin.horizontalScrollbarThumb, _materialSkin.horizontalScrollbarThumb.normal.background,
            _materialSkin.horizontalScrollbarThumb.hover.background, _materialSkin.horizontalScrollbarThumb.active.background);
        ApplyMaterialState(_materialSkin.verticalScrollbar, _materialSkin.verticalScrollbar.normal.background,
            _materialSkin.verticalScrollbar.hover.background, _materialSkin.verticalScrollbar.active.background);
        ApplyMaterialState(_materialSkin.verticalScrollbarThumb, _materialSkin.verticalScrollbarThumb.normal.background,
            _materialSkin.verticalScrollbarThumb.hover.background, _materialSkin.verticalScrollbarThumb.active.background);
        _materialSkin.horizontalScrollbarLeftButton = CreateHiddenScrollbarButtonStyle();
        _materialSkin.horizontalScrollbarRightButton = CreateHiddenScrollbarButtonStyle();
        _materialSkin.verticalScrollbarUpButton = CreateHiddenScrollbarButtonStyle();
        _materialSkin.verticalScrollbarDownButton = CreateHiddenScrollbarButtonStyle();

        return _materialSkin;
    }

    private static GUISkin GetAlternativeSkin()
    {
        string theme = CurrentTheme;
        if (_alternativeSkin != null && string.Equals(_alternativeSkinTheme, theme, StringComparison.Ordinal))
            return _alternativeSkin;

        DestroyAlternativeSkin();

        Color background;
        Color sidebar;
        Color content;
        Color accent;
        Color text;
        GetAlternativeTheme(out background, out sidebar, out content, out accent, out text);

        Texture2D backgroundTexture = CreateStandaloneTexture(background);
        Texture2D sidebarTexture = CreateStandaloneTexture(sidebar);
        Texture2D contentTexture = CreateStandaloneTexture(content);
        Texture2D accentTexture = CreateStandaloneTexture(accent);
        AlternativeTextures.Add(backgroundTexture);
        AlternativeTextures.Add(sidebarTexture);
        AlternativeTextures.Add(contentTexture);
        AlternativeTextures.Add(accentTexture);

        GUIStyle label = new GUIStyle
        {
            normal = { textColor = text },
            fontSize = 13,
            padding = new RectOffset { left = 4, right = 4, top = 3, bottom = 3 },
            wordWrap = true
        };
        GUIStyle button = new GUIStyle
        {
            normal = { background = sidebarTexture, textColor = text },
            hover = { background = accentTexture, textColor = Color.white },
            active = { background = accentTexture, textColor = Color.white },
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset { left = 8, right = 8, top = 6, bottom = 6 },
            border = new RectOffset { left = 1, right = 1, top = 1, bottom = 1 }
        };
        GUIStyle box = new GUIStyle
        {
            normal = { background = contentTexture },
            padding = new RectOffset { left = 8, right = 8, top = 8, bottom = 8 },
            border = new RectOffset { left = 1, right = 1, top = 1, bottom = 1 }
        };
        GUIStyle toggle = new GUIStyle(button)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset { left = 22, right = 8, top = 5, bottom = 5 }
        };
        GUIStyle window = new GUIStyle(box)
        {
            normal = { background = backgroundTexture },
            padding = new RectOffset()
        };

        _alternativeSkin = ScriptableObject.CreateInstance<GUISkin>();
        _alternativeSkin.name = "SocietyArgonStandalone" + theme.Replace(" ", string.Empty);
        _alternativeSkin.label = label;
        _alternativeSkin.button = button;
        _alternativeSkin.box = box;
        _alternativeSkin.toggle = toggle;
        _alternativeSkin.window = window;
        _alternativeSkin.textField = new GUIStyle(box) { normal = { textColor = text } };
        _alternativeSkin.textArea = new GUIStyle(_alternativeSkin.textField);
        _alternativeSkin.scrollView = new GUIStyle { normal = { background = contentTexture } };
        _alternativeSkin.horizontalScrollbar = new GUIStyle { normal = { background = sidebarTexture }, fixedHeight = 8f };
        _alternativeSkin.horizontalScrollbarThumb = new GUIStyle { normal = { background = accentTexture }, fixedHeight = 8f };
        _alternativeSkin.verticalScrollbar = new GUIStyle { normal = { background = sidebarTexture }, fixedWidth = 8f };
        _alternativeSkin.verticalScrollbarThumb = new GUIStyle { normal = { background = accentTexture }, fixedWidth = 8f };
        _alternativeSkin.horizontalScrollbarLeftButton = new GUIStyle();
        _alternativeSkin.horizontalScrollbarRightButton = new GUIStyle();
        _alternativeSkin.verticalScrollbarUpButton = new GUIStyle();
        _alternativeSkin.verticalScrollbarDownButton = new GUIStyle();
        _alternativeSkinTheme = theme;
        return _alternativeSkin;
    }

    private static Texture2D CreateStandaloneTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static void ApplyMaterialState(GUIStyle style, Texture2D normal, Texture2D hover, Texture2D active)
    {
        style.onNormal.background = normal;
        style.onHover.background = hover;
        style.onActive.background = active;
        style.onFocused.background = hover;
        style.focused.background = hover;
    }

    private static GUIStyle CreateHiddenScrollbarButtonStyle()
    {
        Texture2D transparent = CreateTexture(new Color(0f, 0f, 0f, 0f));
        GUIStyle style = new GUIStyle
        {
            normal = { background = transparent },
            hover = { background = transparent },
            active = { background = transparent },
            focused = { background = transparent },
            onNormal = { background = transparent },
            onHover = { background = transparent },
            onActive = { background = transparent },
            onFocused = { background = transparent },
            fixedWidth = 0f,
            fixedHeight = 0f,
            stretchWidth = false,
            stretchHeight = false,
            margin = new RectOffset(),
            padding = new RectOffset()
        };
        return style;
    }
}
