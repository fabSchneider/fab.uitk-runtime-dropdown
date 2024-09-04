using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fab.UITKDropdown
{
    public class Dropdown
    {
        private static readonly string classname = "fab-dropdown";

        private static readonly string menuClassname = classname + "__menu";
        private static readonly string menuOpenClassname = menuClassname + "--open";
        private static readonly string menuDepthClassname = menuClassname + "--depth";
        private static readonly string menuRightwardsClassname = menuClassname + "--rightwards";
        private static readonly string menuUpwardsClassname = menuClassname + "--upwards";

        private static readonly string menuContainerClassname = classname + "__menu-container";
        private static readonly string itemClassname = classname + "__menu-item";
        private static readonly string subItemClassname = classname + "__sub-menu-item";
        private static readonly string separatorClassname = classname + "__separator";
        private static readonly string separatorLineClassname = classname + "-separator__line";

        private static readonly string openedItemClassname = subItemClassname + "--opened";

        private static readonly string hiddenItemClassname = itemClassname + "--hidden";
        private static readonly string checkedItemClassname = itemClassname + "--checked";
        private static readonly string disabledItemClassname = itemClassname + "--disabled";

        public static readonly string itemIconClassname = classname + "-menu-item__icon";
        public static readonly string itemTextClassname = classname + "-menu-item__text";
        public static readonly string itemArrowClassname = classname + "-menu-item__arrow";

        private abstract class ItemManipulator : Manipulator
        {
            protected bool cancel;
            public Menu menu;

            protected ItemManipulator() { }

            private bool enabled;

            public bool Enabled
            {
                get => enabled;
                set
                {
                    enabled = value;
                    target.EnableInClassList(disabledItemClassname, !enabled);
                }
            }

            private bool hidden;

            public bool Hidden
            {
                get => hidden;
                set
                {
                    hidden = value;
                    target.EnableInClassList(hiddenItemClassname, hidden);
                    target.focusable = !hidden;
                }
            }

            public abstract void Reset();

            protected void ExecuteIfEnabled()
            {
                if (Enabled)
                    ExecuteAction();
            }

            protected abstract void ExecuteAction();
            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerEnterEvent>(OnEnter);
                target.RegisterCallback<PointerLeaveEvent>(OnLeave);
                target.RegisterCallback<PointerUpEvent>(OnPointerUp);

                target.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit, TrickleDown.TrickleDown);
                target.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            }
            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<PointerEnterEvent>(OnEnter);
                target.UnregisterCallback<PointerLeaveEvent>(OnLeave);
                target.UnregisterCallback<PointerUpEvent>(OnPointerUp);

                target.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit, TrickleDown.TrickleDown);
                target.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            }

            protected virtual void OnNavigationSubmit(NavigationSubmitEvent evt)
            {
                ExecuteIfEnabled();
            }

            protected virtual void OnNavigationMove(NavigationMoveEvent evt)
            {
                // prevent default focus behavior
                evt.StopPropagation();
                evt.PreventDefault();

                switch (evt.direction)
                {
                    case NavigationMoveEvent.Direction.Left:
                        if (menu.parentMenu != null)
                        {
                            menu.parentMenu.CloseSubMenus();
                            menu.target.Focus();
                        }
                        break;
                    case NavigationMoveEvent.Direction.Up:
                    case NavigationMoveEvent.Direction.Previous:
                        // find previous focusable element
                        int idx = target.parent.IndexOf(target);
                        int prevIdx = (target.parent.childCount + idx - 1) % target.parent.childCount;
                        while (idx != prevIdx)
                        {
                            VisualElement prevElement = target.parent[prevIdx];
                            if (prevElement.focusable)
                            {
                                prevElement.Focus();
                                break;
                            }
                            prevIdx = (target.parent.childCount + prevIdx - 1) % target.parent.childCount;
                        }
                        break;
                    case NavigationMoveEvent.Direction.Right:
                        break;
                    case NavigationMoveEvent.Direction.Down:
                    case NavigationMoveEvent.Direction.Next:
                        // find next focusable element
                        idx = target.parent.IndexOf(target);
                        int nextIdx = (idx + 1) % target.parent.childCount;
                        while (idx != nextIdx)
                        {
                            VisualElement nextElement = target.parent[nextIdx];
                            if (nextElement.focusable)
                            {
                                nextElement.Focus();
                                break;
                            }
                            nextIdx = (nextIdx + 1) % target.parent.childCount;
                        }
                        break;
                    default:
                        break;
                }
            }

            protected virtual void OnEnter(PointerEnterEvent evt)
            {
                cancel = false;
                target.schedule.Execute(Activate).StartingIn(menu.dropdown.SubMenuOpenDelay);

                // cant reliably remove highlight on last item right now
                // due to a bug in UI Toolkit where it fires on enter again when the geometry has changed
                // Bug seems to be resolved in 2022.3.16f1
                if (menu.openSubMenu != null && menu.openSubMenu.target != target)
                {
                    menu.openSubMenu.target.RemoveFromClassList(openedItemClassname);
                }

                target.Focus();
            }

            protected virtual void OnLeave(PointerLeaveEvent evt)
            {
                cancel = true;
                target.Blur();
            }

            protected virtual void OnPointerUp(PointerUpEvent evt)
            {
                evt.StopPropagation();
                target.Blur();
                ExecuteIfEnabled();
            }

            protected virtual void Activate()
            {
                if (cancel)
                    return;

                menu.CloseSubMenus();
            }
        }
        private class ActionItemManipulator : ItemManipulator
        {
            private Action action;

            public ActionItemManipulator() { }

            public void Set(Action action, Menu menu, bool enabled, bool hidden)
            {
                this.action = action;
                this.menu = menu;
                Enabled = enabled;
                Hidden = hidden;
            }

            public override void Reset()
            {
                menu = null;
                action = null;
                Enabled = false;
                Hidden = false;
            }

            protected override void ExecuteAction()
            {
                menu.dropdown.Close();
                action.Invoke();
            }
        }
        private class SubItemManipulator : ItemManipulator
        {
            public Menu subMenu;

            public SubItemManipulator() { }

            public void Set(Menu menu, Menu subMenu)
            {
                this.menu = menu;
                this.subMenu = subMenu;
                Enabled = true;
                Hidden = false;
            }

            public override void Reset()
            {
                menu = null;
                subMenu = null;
                Enabled = false;
                Hidden = false;
            }

            protected override void Activate()
            {
                if (cancel)
                    return;

                ExecuteIfEnabled();
            }
            protected override void ExecuteAction()
            {
                target.AddToClassList(openedItemClassname);
                menu.OpenSubMenu(subMenu);
            }

            protected override void OnNavigationSubmit(NavigationSubmitEvent evt)
            {
                base.OnNavigationSubmit(evt);
                if (!Enabled)
                    return;

                // select first item of sub menu
                if (subMenu.menuContainer.childCount > 0)
                {
                    subMenu.menuContainer[0].Focus();
                }
            }

            protected override void OnNavigationMove(NavigationMoveEvent evt)
            {
                base.OnNavigationMove(evt);

                if (!Enabled)
                    return;


                switch (evt.direction)
                {
                    case NavigationMoveEvent.Direction.Right:
                        ExecuteAction();
                        // select first item of sub menu
                        if (subMenu.menuContainer.childCount > 0)
                        {
                            subMenu.menuContainer[0].Focus();
                        }
                        break;
                    case NavigationMoveEvent.Direction.Up:
                    case NavigationMoveEvent.Direction.Previous:
                    case NavigationMoveEvent.Direction.Down:
                    case NavigationMoveEvent.Direction.Next:
                        menu.CloseSubMenus();
                        break;
                    default:
                        break;
                }
            }
        }
        private class Menu : VisualElement
        {
            public readonly Dropdown dropdown;
            public readonly VisualElement menuContainer;

            private List<ActionItemManipulator> actionItems;
            private List<SubItemManipulator> subItems;
            private List<VisualElement> separators;
            public Dictionary<string, Menu> subMenus;

            public Menu parentMenu;
            public VisualElement target;

            public Menu openSubMenu;

            public Menu(Dropdown dropdown)
            {
                usageHints = UsageHints.DynamicTransform;
                this.dropdown = dropdown;
                AddToClassList(menuClassname);
                menuContainer = new VisualElement();
                menuContainer.AddToClassList(menuContainerClassname);
                Add(menuContainer);

                actionItems = new List<ActionItemManipulator>();
                subItems = new List<SubItemManipulator>();
                separators = new List<VisualElement>();
                subMenus = new Dictionary<string, Menu>();
            }

            public void Set(Menu parentMenu, VisualElement target)
            {
                this.target = target;
                this.parentMenu = parentMenu;
            }

            /// <summary>
            /// Returns this menu and all of its sub menus to the pool
            /// </summary>
            public void ReturnToPool()
            {
                // return actionItems to pool
                for (int i = 0; i < actionItems.Count; i++)
                {
                    var item = actionItems[i];
                    item.target.RemoveFromHierarchy();
                    dropdown.actionItemPool.ReturnToPool(item);
                }

                // return subItems to pool
                for (int i = 0; i < subItems.Count; i++)
                {
                    var item = subItems[i];
                    item.target.RemoveFromHierarchy();
                    dropdown.subItemPool.ReturnToPool(item);
                }

                //return separators to pool
                for (int i = 0; i < separators.Count; i++)
                {
                    var separator = separators[i];
                    separator.RemoveFromHierarchy();
                    dropdown.separatorPool.ReturnToPool(separator);
                }

                // close all sub-menus
                CloseSubMenus();

                // return sub menus to pool
                foreach (var subMenu in subMenus.Values)
                    subMenu.ReturnToPool();

                target = null;
                parentMenu = null;

                actionItems.Clear();
                if (actionItems.Capacity > 32) actionItems.Capacity = 32;
                subItems.Clear();
                if (subItems.Capacity > 32) subItems.Capacity = 32;
                separators.Clear();
                if (separators.Capacity > 32) separators.Capacity = 32;

                openSubMenu = null;
                subMenus.Clear();

                style.left = StyleKeyword.Null;
                style.right = StyleKeyword.Null;
                style.top = StyleKeyword.Null;
                style.bottom = StyleKeyword.Null;

                ClearClassList();
                AddToClassList(menuClassname);

                // clear all elements in the menu
                Clear();
                menuContainer.Clear();
                hierarchy.Add(menuContainer);

                dropdown.subMenuPool.ReturnToPool(this);
            }

            public void OpenSubMenu(Menu menu)
            {
                if (openSubMenu == menu)
                    return;

                // Make sure that any open sub menu is closed 
                // before opening a new one
                CloseSubMenus();

                openSubMenu = menu;
                menu.target.AddToClassList(openedItemClassname);

                Add(menu);

                // HACK: add menu open style with a small delay to allow
                // for fade in transition styles to work
                menu.schedule.Execute(() => menu.AddToClassList(menuOpenClassname)).ExecuteLater(10);
            }

            public void CloseSubMenus()
            {
                if (openSubMenu == null)
                    return;

                for (int i = 0; i < openSubMenu.menuContainer.childCount; i++)
                {
                    var ve = openSubMenu.menuContainer[i];
                    ve.RemoveFromClassList(openedItemClassname);
                }

                openSubMenu.RemoveFromHierarchy();
                openSubMenu.RemoveFromClassList(menuOpenClassname);
                openSubMenu.CloseConsecutive();
                openSubMenu = null;
            }

            private void CloseConsecutive()
            {
                target?.RemoveFromClassList(openedItemClassname);
                if (openSubMenu != null)
                {
                    openSubMenu.RemoveFromClassList(menuOpenClassname);
                    openSubMenu.RemoveFromHierarchy();
                    openSubMenu.CloseSubMenus();
                }

                openSubMenu = null;
            }

            public void AddItem(DropdownMenuItem item, string[] path, int level)
            {
                // leaf item
                if (level == path.Length - 1 || path.Length == 0)
                {
                    if (item is DropdownMenuAction action)
                    {
                        var m = dropdown.actionItemPool.GetPooled();

                        m.Set(action.Execute, this,
                            !action.status.HasFlag(DropdownMenuAction.Status.Disabled),
                            action.status.HasFlag(DropdownMenuAction.Status.Hidden));
                        dropdown.SetItem(m.target, item, path, level);
                        dropdown.SetItemStatus(m.target, action.status);
                        actionItems.Add(m);
                        menuContainer.Add(m.target);
                    }
                    else
                    {
                        var separator = dropdown.separatorPool.GetPooled();
                        separators.Add(separator);
                        menuContainer.Add(separator);
                    }
                }
                // create sub-menu item
                else
                {
                    if (!subMenus.TryGetValue(path[level], out Menu subMenu))
                    {
                        // create new sub-menu if it has not been created yet
                        var m = dropdown.subItemPool.GetPooled();
                        subMenu = dropdown.subMenuPool.GetPooled();
                        subMenu.Set(this, m.target);
                        m.Set(this, subMenu);
                        dropdown.SetItem(m.target, item, path, level);

                        menuContainer.Add(m.target);
                        subMenus.Add(path[level], subMenu);
                        subItems.Add(m);

                        subMenu.AddToClassList(menuDepthClassname + (level + 1).ToString());
                        dropdown.SetMenu?.Invoke(subMenu.menuContainer, path, level + 1);
                    }

                    // add menu to resolve its style
                    Add(subMenu);

                    subMenu.AddItem(item, path, level + 1);
                }
            }
            public void DetachAll()
            {
                foreach (var submenu in subMenus.Values)
                    submenu.DetachAll();

                RemoveFromHierarchy();
            }

            public DropdownMenuAction.Status SetSubItemStates()
            {
                bool hasEnabledItems = false;
                bool hasVisibleItems = false;
                foreach (var item in actionItems)
                {
                    if (!item.Hidden)
                    {
                        hasVisibleItems = true;
                    }

                    // hidden items are treated like disabled items
                    if (item.Enabled && !item.Hidden)
                    {
                        hasEnabledItems = true;
                    }

                    if (hasEnabledItems && hasVisibleItems)
                        break;
                }

                foreach (var subItem in subItems)
                {
                    DropdownMenuAction.Status subStatus = subItem.subMenu.SetSubItemStates();

                    if (subStatus == DropdownMenuAction.Status.Hidden)
                    {
                        subItem.Hidden = true;
                    }
                    else if (subStatus == DropdownMenuAction.Status.Disabled)
                    {
                        subItem.Enabled = false;
                        hasVisibleItems = true;
                    }
                    else
                    {
                        hasEnabledItems = true;
                        hasVisibleItems = true;
                    }
                }

                if (!hasVisibleItems)
                    return DropdownMenuAction.Status.Hidden;

                if (!hasEnabledItems)
                    return DropdownMenuAction.Status.Disabled;

                return DropdownMenuAction.Status.Normal;
            }
        }

        private VisualElement root;
        private VisualElement dropdownLayer;
        private Menu rootMenu;

        private Rect targetRect;

        private readonly Func<VisualElement> MakeItem;
        private readonly Action<VisualElement, DropdownMenuItem, string[], int> SetItem;
        private readonly Action<VisualElement, string[], int> SetMenu;

        private ObjectPool<ActionItemManipulator> actionItemPool;
        private ObjectPool<SubItemManipulator> subItemPool;
        private ObjectPool<VisualElement> separatorPool;
        private ObjectPool<Menu> subMenuPool;

        /// <summary>
        /// Delay in milliseconds before a hovered item opens its sub menu.
        /// </summary>
        public long SubMenuOpenDelay { get; set; } = 200;

        /// <summary>
        /// Creates a default menu item.
        /// </summary>
        public static VisualElement MakeDefaultItem()
        {
            VisualElement ve = new VisualElement();
            VisualElement icon = new VisualElement() { name = "icon" };
            icon.AddToClassList(itemIconClassname);
            ve.Add(icon);
            VisualElement text = new Label() { name = "text" };
            text.AddToClassList(itemTextClassname);
            ve.Add(text);
            VisualElement arrow = new VisualElement() { name = "arrow" };
            arrow.AddToClassList(itemArrowClassname);
            ve.Add(arrow);
            return ve;
        }

        /// <summary>
        /// Default method for setting menu items. 
        /// </summary>
        public static void SetDefaultItem(VisualElement ve, DropdownMenuItem item, string[] path, int level)
        {
            ve.Q<Label>(name: "text").text = path[level];
        }

        /// <summary>
        /// Creates a default menu item.
        /// </summary>
        public static VisualElement MakeDefaultSeparator()
        {
            VisualElement ve = new VisualElement();
            ve.AddToClassList(separatorClassname);
            VisualElement separator = new VisualElement();
            separator.AddToClassList(separatorLineClassname);
            ve.Add(separator);
            return ve;
        }

        /// <summary>
        /// Constructs the drop-down.
        /// </summary>
        /// <param name="root">The root element the drop-down will attach to.</param>
        /// <param name="makeItem">Optional function to customize item appearance.</param>
        /// <param name="setItem">Optional function to customize how items display their content. 
        /// Passes the path of the corresponding leaf menu item and the level of the current item.</param>
        /// <param name="makeSeparator">Optional function to customize the separator appearance.</param>
        /// <param name="setMenu">Optional function to customize menu appearances
        /// Passes the path of the corresponding leaf menu item and the level of the current menu.</param>
        public Dropdown(VisualElement root,
            Func<VisualElement> makeItem = null,
            Action<VisualElement, DropdownMenuItem, string[], int> setItem = null,
            Func<VisualElement> makeSeparator = null, 
            Action<VisualElement, string[], int> setMenu = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            this.root = root;

            SetupDropdownLayer();

            MakeItem = makeItem == null ? MakeDefaultItem : makeItem;
            SetItem = setItem == null ? SetDefaultItem : setItem;
            SetMenu = setMenu;

            actionItemPool = new ObjectPool<ActionItemManipulator>(32, true, MakeActionItem, ResetItem);
            subItemPool = new ObjectPool<SubItemManipulator>(32, true, MakeSubItem, ResetItem);

            separatorPool = new ObjectPool<VisualElement>(32, true, makeSeparator == null ? MakeDefaultSeparator : makeSeparator);
            subMenuPool = new ObjectPool<Menu>(16, true, () => new Menu(this));
        }

        /// <summary>
        /// Opens the drop-down anchored to the bottom border of the target world bounds.
        /// </summary>
        public void Open(DropdownMenu menu, Rect targetWorldBound, EventBase evt = null)
        {
            if (menu == null)
                throw new ArgumentNullException(nameof(menu));

            if (dropdownLayer.parent != null)
                Close();

            targetRect = targetWorldBound;

            // Dropdown menu only supports Mouse Events
            // In case the incoming event is a pointer event we need to
            // synthesize a new mouse event from the pointer event
            if (evt is IPointerEvent pointerEvent)
            {
                using var mouseEvt = MouseDownEvent.GetPooled(pointerEvent.position, 0, 1, pointerEvent.deltaPosition, pointerEvent.modifiers);
                menu.PrepareForDisplay(mouseEvt);
            }
            else if (evt == null)
            {
                using var mouseEvt = MouseDownEvent.GetPooled(targetWorldBound.position, 0, 1, Vector2.zero);
                menu.PrepareForDisplay(mouseEvt);
            }
            else
            {
                menu.PrepareForDisplay(evt);
            }

            Build(menu);

            rootMenu.RegisterCallback<GeometryChangedEvent>(OnBuildComplete);
            root.Add(dropdownLayer);

            // we have to blur the currently focused element
            // otherwise focusing of the blocking layer does not work reliably
            root.focusController.focusedElement?.Blur();
            dropdownLayer.Focus();
        }

        /// <summary>
        /// Opens the drop-down at the given world position.
        /// </summary>
        public void Open(DropdownMenu menu, Vector2 worldPosition, EventBase evt = null)
        {
            Open(menu, new Rect(worldPosition, Vector2.zero), evt);
        }

        /// <summary>
        /// Closes the drop-down.
        /// </summary>
        public void Close()
        {
            if (rootMenu != null)
            {
                rootMenu.CloseSubMenus();
            }
            dropdownLayer.RemoveFromHierarchy();
        }

        private void SetupDropdownLayer()
        {
            dropdownLayer = new VisualElement()
            {
                focusable = true,
                // set tab index to -1 to avoid blocking layer
                // being picked by the focus ring
                tabIndex = -1
            };
            dropdownLayer.StretchToParentSize();
            dropdownLayer.AddToClassList(classname);

            // closing when the pointer is down outside of any menu
            dropdownLayer.RegisterCallback<PointerDownEvent>(evt =>
            {

                if (evt.target == dropdownLayer)
                {
                    Close();

                    // resend the pointer event to pass it through the blocking layer to elements underneath
                    using (PointerDownEvent pointerDownEvent = PointerDownEvent.GetPooled(evt))
                    {
                        root.panel.visualTree.SendEvent(pointerDownEvent);
                    }
                }
            });

            // default closing behavior when pressing the navigation cancel event
            dropdownLayer.RegisterCallback<NavigationCancelEvent>(evt =>
            {
                Close();
            }, TrickleDown.TrickleDown);

            // override default focus behavior
            // if no item is focused using up or down navigation will 
            // focus the first or last item in the root menu
            dropdownLayer.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                if (evt.target != dropdownLayer)
                    return;

                // prevent default focusing behavior
                evt.StopPropagation();
                evt.PreventDefault();

                if (rootMenu.menuContainer.childCount > 0)
                {
                    // focus first or last item in the root menu
                    // depending on the direction of the navigation event
                    if (evt.direction == NavigationMoveEvent.Direction.Down)
                    {
                        rootMenu.menuContainer[0].Focus();
                    }
                    else if (evt.direction == NavigationMoveEvent.Direction.Up)
                    {
                        rootMenu.menuContainer[rootMenu.menuContainer.childCount - 1].Focus();
                    }
                }
            }, TrickleDown.TrickleDown);

            // take away focus from the any dropdown item as the pointer moves out of the menu
            dropdownLayer.RegisterCallback<PointerOverEvent>(evt =>
            {
                if (evt.target == dropdownLayer)
                    dropdownLayer.Focus();
            });
        }

        private void Build(DropdownMenu menu)
        {
            rootMenu?.ReturnToPool();
            rootMenu = subMenuPool.GetPooled();
            rootMenu.AddToClassList(menuDepthClassname + "0");
            rootMenu.schedule.Execute(() => rootMenu.AddToClassList(menuOpenClassname)).ExecuteLater(10);

            SetMenu?.Invoke(rootMenu.menuContainer, Array.Empty<string>(), 0);

            dropdownLayer.Add(rootMenu);
            foreach (var item in menu.MenuItems())
            {
                if (item is DropdownMenuSeparator s)
                    rootMenu.AddItem(item, s.subMenuPath.Split('/'), 0);
                else if (item is DropdownMenuAction a)
                    rootMenu.AddItem(item, a.name.Split('/'), 0);
            }

            rootMenu.SetSubItemStates();
        }

        private void OnBuildComplete(GeometryChangedEvent evt)
        {
            rootMenu.UnregisterCallback<GeometryChangedEvent>(OnBuildComplete);

            Rect rootWorldBound = root.worldBound;

            Vector2 worldPos = new Vector2(targetRect.xMin, targetRect.yMax);
            Vector2 offset = new Vector2(rootMenu.resolvedStyle.marginLeft, rootMenu.resolvedStyle.marginTop);
            Vector2 menuSize = rootMenu.worldBound.size;

            Vector2 maxPos = worldPos + offset + menuSize;

            // align root menu to the right side if it exceeds the bounds horizontally
            if (maxPos.x > rootWorldBound.xMax)
            {
                worldPos.x = rootWorldBound.xMax - menuSize.x;
            }

            // align root menu to the bottom if it exceeds the bounds vertically
            if (maxPos.y > rootWorldBound.yMax)
            {
                worldPos.y = rootWorldBound.yMax - menuSize.y;
            }

            foreach (Menu menu in rootMenu.subMenus.Values)
            {
                SetSubMenuPosition(menu, worldPos, in rootWorldBound);
            }

            // detach all menus that have been attached while building
            rootMenu.DetachAll();

            Vector2 localPos = root.WorldToLocal(worldPos);
            rootMenu.style.left = localPos.x;
            rootMenu.style.top = localPos.y;

            dropdownLayer.Add(rootMenu);
        }

        private void SetSubMenuPosition(Menu menu, Vector2 parentWorldPosition, in Rect rootWorldBound)
        {
            Vector2 anchor = new Vector2(menu.target.localBound.xMax, menu.target.localBound.yMin);
            Vector2 offset = new Vector2(menu.resolvedStyle.marginLeft, menu.resolvedStyle.marginTop);
            Vector2 menuSize = menu.worldBound.size;

            Vector2 maxPos = parentWorldPosition + anchor + offset + menuSize;

            // position to the right side if menu exceeds bounds
            if (maxPos.x > rootWorldBound.xMax)
            {
                menu.AddToClassList(menuRightwardsClassname);
                anchor.x = menu.target.localBound.xMin - menuSize.x - offset.x;

                // align to left border if adjusted menu exceeds left bounds
                if (parentWorldPosition.x + anchor.x < 0)
                {
                    anchor.x = -parentWorldPosition.x;
                }
            }

            // position upwards if menu exceeds bounds
            if (maxPos.y > rootWorldBound.yMax)
            {
                menu.AddToClassList(menuUpwardsClassname);
                anchor.y = menu.target.localBound.yMax - menuSize.y - offset.y;

                // align to top border if adjusted menu exceeds top bounds
                if (parentWorldPosition.y + anchor.y < 0)
                {
                    anchor.y = -parentWorldPosition.y;
                }
            }

            menu.style.left = anchor.x;
            menu.style.top = anchor.y;

            foreach (Menu subMenu in menu.subMenus.Values)
            {
                SetSubMenuPosition(subMenu, parentWorldPosition + anchor, rootWorldBound);
            }
        }
       
        private ActionItemManipulator MakeActionItem()
        {
            var ve = MakeItem();
            ve.AddToClassList(itemClassname);
            ve.focusable = true;
            var m = new ActionItemManipulator();
            ve.AddManipulator(m);

            return m;
        }

        private SubItemManipulator MakeSubItem()
        {
            var ve = MakeItem();
            ve.AddToClassList(itemClassname);
            ve.AddToClassList(subItemClassname);
            ve.focusable = true;
            var m = new SubItemManipulator();
            ve.AddManipulator(m);
            return m;
        }

        private void ResetItem(ItemManipulator item)
        {
            var target = item.target;

            item.Reset();

            // remove all classes that might have been added 
            // to the target while they were in use
            target.RemoveFromClassList(hiddenItemClassname);
            target.RemoveFromClassList(checkedItemClassname);
            target.RemoveFromClassList(disabledItemClassname);

            target.RemoveFromClassList(openedItemClassname);
        }

        private void SetItemStatus(VisualElement ve, DropdownMenuAction.Status status)
        {
            switch (status)
            {
                case DropdownMenuAction.Status.None:
                    ve.AddToClassList(hiddenItemClassname);
                    ve.RemoveFromClassList(checkedItemClassname);
                    ve.RemoveFromClassList(disabledItemClassname);
                    break;
                case DropdownMenuAction.Status.Normal:
                    ve.RemoveFromClassList(hiddenItemClassname);
                    ve.RemoveFromClassList(checkedItemClassname);
                    ve.RemoveFromClassList(disabledItemClassname);
                    break;
                case DropdownMenuAction.Status.Disabled:
                    ve.RemoveFromClassList(hiddenItemClassname);
                    ve.AddToClassList(disabledItemClassname);
                    break;
                case DropdownMenuAction.Status.Checked:
                    ve.RemoveFromClassList(hiddenItemClassname);
                    ve.AddToClassList(checkedItemClassname);
                    break;
                case DropdownMenuAction.Status.Hidden:
                    ve.AddToClassList(hiddenItemClassname);
                    break;
                default:
                    break;
            }

        }
    }
}
