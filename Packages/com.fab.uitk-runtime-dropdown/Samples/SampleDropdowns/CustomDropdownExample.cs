using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fab.UITKDropdown.Sample
{
    [Serializable]
    public class CustomDropdownItemData
    {
        public Texture2D icon;
        public string path;
        public string hotkey;
    }

    public class CustomDropdownExample : MonoBehaviour
    {
        private Dropdown customDropdown;
        private DropdownMenu customMenu;

        [SerializeField]
        private UIDocument uiDoc;

        [SerializeField]
        private CustomDropdownItemData[] items;

        private void Start()
        {
            // setup custom menu
            customMenu = new DropdownMenu();
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                customMenu.AppendAction(item.path, DoMenuAction, DropdownMenuAction.AlwaysEnabled, userData: item);
            }

            var root = uiDoc.rootVisualElement;

            customDropdown = new Dropdown(root, makeItem: MakeCustomItem, setItem: SetCustomItem, setMenu: SetCustomMenu);

            var customBtn = root.Q<Button>(name: "custom-dropdown-btn");

            customBtn.clickable.clickedWithEventInfo += evt =>
            {
                Rect worldBound = ((VisualElement)evt.target).worldBound;
                worldBound.x = worldBound.x - 42;
                worldBound.y = worldBound.y + 8;
                customDropdown.Open(customMenu, worldBound, evt);
            };
        }

        private void DoMenuAction(DropdownMenuAction action)
        {
            Debug.Log(action.name);
        }

        private VisualElement MakeCustomItem()
        {
            VisualElement ve = Dropdown.MakeDefaultItem();

            // add a hot key text
            var hotkeyText = new Label() { name = "hotkey-text" };
            hotkeyText.AddToClassList(Dropdown.itemTextClassname);
            hotkeyText.style.unityTextAlign = TextAnchor.MiddleRight;
            hotkeyText.style.marginLeft = 24f;

            // insert the hot key text before the arrow.
            ve.Insert(2, hotkeyText);

            return ve;
        }

        private void SetCustomItem(VisualElement ve, DropdownMenuItem item, string[] path, int level)
        {
            Dropdown.SetDefaultItem(ve, item, path, level);

            // don't style sub menu items
            if (path.Length - 1 == level &&
                item is DropdownMenuAction actionItem && actionItem.userData is CustomDropdownItemData data)
            {
                ve.Q(name: "icon").style.backgroundImage = data.icon;
                ve.Q<Label>(name: "hotkey-text").text = data.hotkey;
            }
            else
            {
                ve.Q(name: "icon").style.backgroundImage = StyleKeyword.Null;
                ve.Q<Label>(name: "hotkey-text").text = string.Empty;
            }
        }

        private void SetCustomMenu(VisualElement menu, string[] path, int level)
        {
            Label heading = new Label()
            {
                text = level == 0 ? "Root Menu" : path[level - 1]
            };

            heading.AddToClassList("custom-menu__heading");
            menu.Insert(0, heading);
        }
    }
}
