using UnityEngine;
using UnityEngine.UIElements;

namespace Fab.UITKDropdown.Sample
{
    public class DropdownStylesSample : MonoBehaviour
    {
        [SerializeField]
        private UIDocument uiDoc;

        [SerializeField]
        private ThemeStyleSheet[] themes;

        private DropdownMenu menu;

        private Dropdown dropdown;

        private void Start()
        {
            var root = uiDoc.rootVisualElement;

            dropdown = new Dropdown(root);

            menu = new DropdownMenu();
            for (int i = 0; i < themes.Length; i++)
            {
                ThemeStyleSheet theme = themes[i];
                menu.AppendAction(theme.name, 
                    action => uiDoc.panelSettings.themeStyleSheet = theme, 
                    action => uiDoc.panelSettings.themeStyleSheet == theme ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

            var btn = root.Q<Button>(name: "dropdown-style-btn");
            btn.clickable.clickedWithEventInfo += (evt) => dropdown.Open(menu, btn.worldBound, evt);

        }
    }
}
