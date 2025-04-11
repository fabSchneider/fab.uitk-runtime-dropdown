using UnityEngine;
using UnityEngine.UIElements;

namespace Fab.UITKDropdown.MenubarSample
{
    public class MenubarSample : MonoBehaviour
    {
        [SerializeField]
        private UIDocument uiDoc;

        private DropdownMenu fileMenu;
        private DropdownMenu editMenu;
        private DropdownMenu addMenu;

        private Dropdown dropdown;

        private void Start()
        {
            VisualElement root = uiDoc.rootVisualElement;
            VisualElement view = root.Q("view");

            dropdown = new Dropdown(view);

            fileMenu = new DropdownMenu();
            fileMenu.AppendAction("New", null);
            fileMenu.AppendAction("Load", null);
            fileMenu.AppendAction("Save", null);

            root.Q<Button>("file-button").clickable = new MenubarButtonManipulator(dropdown, fileMenu);

            editMenu = new DropdownMenu();
            editMenu.AppendAction("Undo", null);
            editMenu.AppendAction("Redo", null);
            editMenu.AppendSeparator();
            editMenu.AppendAction("Copy", null);
            editMenu.AppendAction("Paste", null);

            root.Q<Button>("edit-button").clickable = new MenubarButtonManipulator(dropdown, editMenu);

            addMenu = new DropdownMenu();
            addMenu.AppendAction("Add Cube", null);
            addMenu.AppendAction("Add Sphere", null);
            addMenu.AppendAction("Add Cylinder", null);

            root.Q<Button>("add-button").clickable = new MenubarButtonManipulator(dropdown, addMenu);
        }
    }
}
