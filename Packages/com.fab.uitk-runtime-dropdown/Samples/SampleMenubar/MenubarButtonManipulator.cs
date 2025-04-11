using UnityEngine.UIElements;

namespace Fab.UITKDropdown.MenubarSample
{
    public class MenubarButtonManipulator : Clickable
    {
        public static readonly string ussOpenedClassname = "menubar__button--opened";

        private bool isOpened;
        private Dropdown dropdown;
        private DropdownMenu menu;

        public MenubarButtonManipulator(Dropdown dropdown, DropdownMenu menu) : base(DefaultAction)
        {
            clickedWithEventInfo += ClickHandler;
            this.dropdown = dropdown;
            this.menu = menu;
        }

        private static void DefaultAction() { }

        protected override void RegisterCallbacksOnTarget()
        {
            base.RegisterCallbacksOnTarget();
            dropdown.BlockingLayer.RegisterCallback<AttachToPanelEvent>(OnDropdownOpen);
            dropdown.BlockingLayer.RegisterCallback<DetachFromPanelEvent>(OnDropdownClose);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            base.UnregisterCallbacksFromTarget();

            dropdown.BlockingLayer.UnregisterCallback<AttachToPanelEvent>(OnDropdownOpen);
            dropdown.BlockingLayer.UnregisterCallback<DetachFromPanelEvent>(OnDropdownClose);

            target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
            target.RemoveFromClassList(ussOpenedClassname);

            isOpened = false;
        }

        protected void ClickHandler(EventBase evt)
        {
            if (isOpened)
            {
                dropdown.Close();
                isOpened = false;
            }
            else
            {
                Open(evt);
            }
        }

        private void OnDropdownOpen(AttachToPanelEvent evt)
        {
            target.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        }

        private void OnDropdownClose(DetachFromPanelEvent evt)
        {
            isOpened = false;
            target.RemoveFromClassList(ussOpenedClassname);
            target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            Open(evt);
        }

        private void Open(EventBase evt)
        {
            if (isOpened)
                return;

            dropdown.Open(menu, evt);
            target.AddToClassList(ussOpenedClassname);
            isOpened = true;
        }
    }
}
