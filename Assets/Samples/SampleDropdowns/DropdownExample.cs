using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fab.UITKDropdown.Sample
{
    [Flags]
    public enum ExampleOptions
    {
        None = 0,
        Koala = 1,
        Kangaroo = 2,
        Platypus = 4,
        Wombat = 8
    }

    public class DropdownExample : MonoBehaviour
    {

        [SerializeField]
        private UIDocument uiDoc;

        private DropdownMenu btnMenu;
        private DropdownMenu pointerMenu;

        private Dropdown dropdown;

        private ExampleOptions options = ExampleOptions.None;

        private void Start()
        {
            var root = uiDoc.rootVisualElement;

            dropdown = new Dropdown(root);

            //setup button menu;
            btnMenu = new DropdownMenu();
            btnMenu.AppendAction("Action 1", DoMenuAction);
            btnMenu.AppendAction("Action 2", DoMenuAction);
            btnMenu.AppendAction("Action 3", DoMenuAction);
            btnMenu.AppendAction("DisabledAction", DoMenuAction, DropdownMenuAction.AlwaysDisabled);
            btnMenu.AppendSeparator();
            btnMenu.AppendAction("Sub Menu/Action 4", DoMenuAction);
            btnMenu.AppendSeparator("Sub Menu/");
            btnMenu.AppendAction("Sub Menu/Action 5", DoMenuAction);
            btnMenu.AppendAction("Sub Menu/Another Sub Menu/Action 6", DoMenuAction);
            btnMenu.AppendAction("Sub Menu/Another Sub Menu/Action 7", DoMenuAction);
            btnMenu.AppendAction("DisabledSubMenu/Disabled", DoMenuAction, DropdownMenuAction.AlwaysDisabled);
            btnMenu.AppendAction("DisabledSubMenu/Hidden", DoMenuAction, DropdownMenuAction.Status.Hidden);
            btnMenu.AppendAction("Deep/Nested/Menu/That/Would/Annoy/Anyone/That/Has/To/Click/Through/It/But/Atleast/It/Wraps/Around/Nicely/When/It/Reaches/The/End/Of/The/Screen", DoMenuAction);

            var btn = root.Q<Button>(name: "dropdown-btn");
            btn.clickable.clicked += () => dropdown.Open(btnMenu, btn.worldBound);

            //setup pointer menu;
            pointerMenu = new DropdownMenu();
            pointerMenu.AppendAction("Koala", ToggleOption, GetOptionStatus);
            pointerMenu.AppendAction("Kangaroo", ToggleOption, GetOptionStatus);
            pointerMenu.AppendAction("Platypus", ToggleOption, GetOptionStatus);
            pointerMenu.AppendAction("Wombat", ToggleOption, GetOptionStatus);


            root.Q<VisualElement>(name: "dropdown-area").RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    Debug.Log("Click");
                    dropdown.Open(pointerMenu, evt.position);
                }
            });
        }

        private void DoMenuAction(DropdownMenuAction action)
        {
            Debug.Log(action.name);
        }

        private void ToggleOption(DropdownMenuAction action)
        {
            ExampleOptions option;

            switch (action.name)
            {
                case "Koala":
                    option = ExampleOptions.Koala;
                    break;
                case "Kangaroo":
                    option = ExampleOptions.Kangaroo;
                    break;
                case "Platypus":
                    option = ExampleOptions.Platypus;
                    break;
                case "Wombat":
                    option = ExampleOptions.Wombat;
                    break;
                default:
                    option = ExampleOptions.None;
                    break;
            }

            options ^= option;
        }

        private DropdownMenuAction.Status GetOptionStatus(DropdownMenuAction action)
        {
            DropdownMenuAction.Status status;

            switch (action.name)
            {
                case "Koala":
                    status = options.HasFlag(ExampleOptions.Koala) ?
                        DropdownMenuAction.Status.Checked :
                        DropdownMenuAction.Status.Normal;
                    break;
                case "Kangaroo":
                    status = options.HasFlag(ExampleOptions.Kangaroo) ?
                        DropdownMenuAction.Status.Checked :
                        DropdownMenuAction.Status.Normal;
                    break;
                case "Platypus":
                    status = options.HasFlag(ExampleOptions.Platypus) ?
                        DropdownMenuAction.Status.Checked :
                        DropdownMenuAction.Status.Normal;
                    break;
                case "Wombat":
                    status = options.HasFlag(ExampleOptions.Wombat) ?
                        DropdownMenuAction.Status.Checked :
                        DropdownMenuAction.Status.Normal;
                    break;
                default:
                    status = DropdownMenuAction.Status.Disabled;
                    break;
            }

            return status;
        }
    }
}
