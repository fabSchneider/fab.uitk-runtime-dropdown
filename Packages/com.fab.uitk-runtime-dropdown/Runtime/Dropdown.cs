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

        private static readonly string targetOpenClassname = classname + "-target--open";

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
#if UNITY_2023_2_OR_NEWER
                menu.focusController.IgnoreEvent(evt);
#else
                evt.PreventDefault();
#endif

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
                        FocusUtils.FindPreviousFocusableSibling(target).Focus();
                        break;
                    case NavigationMoveEvent.Direction.Right:
                        break;
                    case NavigationMoveEvent.Direction.Down:
                    case NavigationMoveEvent.Direction.Next:
                        FocusUtils.FindNextFocusableSibling(target).Focus();
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
                        FocusUtils.FindFirstFocusableChild(subMenu.menuContainer)?.Focus();
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

            public void ReturnToPool()
            {
                // return actionItems to pool
                for (int i = 0; i < actionItems.Count; i++)
                {
                    ActionItemManipulator item = actionItems[i];
                    item.target.RemoveFromHierarchy();
                    dropdown.actionItemPool.ReturnToPool(item);
                }

                // return subItems to pool
                for (int i = 0; i < subItems.Count; i++)
                {
                    SubItemManipulator item = subItems[i];
                    item.target.RemoveFromHierarchy();
                    dropdown.subItemPool.ReturnToPool(item);
                }

                //return separators to pool
                for (int i = 0; i < separators.Count; i++)
                {
                    VisualElement separator = separators[i];
                    separator.RemoveFromHierarchy();
                    dropdown.separatorPool.ReturnToPool(separator);
                }

                // close all sub-menus
                CloseSubMenus();

                // return sub menus to pool
                foreach (Menu subMenu in subMenus.Values)
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

                // Make sure that any open sub menu is closed before opening a new one
                CloseSubMenus();

                openSubMenu = menu;
                menu.target.AddToClassList(openedItemClassname);


                Add(menu);
#if UNITY_2023_1_OR_NEWER
                ApplyStylesAndPositionNextFrame(menu);
#else
                // wait a few milliseconds to add menu open style for fade in transition styles to work
                // the menu position is also only determined after the size of the menu is set
                menu.schedule.Execute(() =>
                {
                    SetSubMenuPosition(menu, menu.parentMenu.worldBound.position, dropdown.root.worldBound);
                    menu.AddToClassList(menuOpenClassname);
                }).ExecuteLater(10);
#endif
            }

#if UNITY_2023_1_OR_NEWER
            private async void ApplyStylesAndPositionNextFrame(Menu menu)
            {
                // wait until the next frame to add menu open style for fade in transition styles to work
                // the menu position is also only determined on the next frame after the size of the menu is set
                await Awaitable.NextFrameAsync();
                SetSubMenuPosition(menu, menu.parentMenu.worldBound.position, dropdown.root.worldBound);
                menu.AddToClassList(menuOpenClassname);
            }
#endif

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
                menu.style.bottom = StyleKeyword.Null;
                menu.style.right = StyleKeyword.Null;
            }

            public void CloseSubMenus()
            {
                if (openSubMenu == null)
                    return;

                for (int i = 0; i < openSubMenu.menuContainer.childCount; i++)
                {
                    VisualElement ve = openSubMenu.menuContainer[i];
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
                        ActionItemManipulator m = dropdown.actionItemPool.GetPooled();

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
                        VisualElement separator = dropdown.separatorPool.GetPooled();
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
                        SubItemManipulator m = dropdown.subItemPool.GetPooled();
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

                    subMenu.AddItem(item, path, level + 1);
                }
            }

            public DropdownMenuAction.Status SetSubItemStates()
            {
                bool hasEnabledItems = false;
                bool hasVisibleItems = false;
                foreach (ActionItemManipulator item in actionItems)
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

                foreach (SubItemManipulator subItem in subItems)
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
        private BlockingLayer blockingLayer;
        private Menu rootMenu;

        private VisualElement targetElement;
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
        ///  The blocking layer of the dropdown.
        /// </summary>
        public BlockingLayer BlockingLayer => blockingLayer;

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
        /// Constructs the dropdown.
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

            blockingLayer = new BlockingLayer() { blockingBehavior = BlockingBehavior.Closeable };
            blockingLayer.StretchToParentSize();
            blockingLayer.RegisterCallback<DetachFromPanelEvent>(evt => Close());
            blockingLayer.AddToClassList(classname);

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

            if (blockingLayer.parent != null)
                Close();

            targetRect = targetWorldBound;

            if (evt != null)
            {
                targetElement = evt.target as VisualElement;
                targetElement?.AddToClassList(targetOpenClassname);

            }

            // Dropdown menu only supports Mouse Events
            // In case the incoming event is a pointer event we need to
            // synthesize a new mouse event from the pointer event
            if (evt is IPointerEvent pointerEvent)
            {
                using MouseDownEvent mouseEvt = MouseDownEvent.GetPooled(pointerEvent.position, 0, 1, pointerEvent.deltaPosition, pointerEvent.modifiers);
                menu.PrepareForDisplay(mouseEvt);
            }
            else if (evt == null)
            {
                using MouseDownEvent mouseEvt = MouseDownEvent.GetPooled(targetRect.position, 0, 1, Vector2.zero);
                menu.PrepareForDisplay(mouseEvt);
            }
            else
            {
                menu.PrepareForDisplay(evt);
            }

            Build(menu);
            root.Add(blockingLayer);



#if UNITY_2023_1_OR_NEWER
            // Wait for next frame to add open class to allow for in transitions
            SetRootMenuOpenStyleNextFrame();
#else
            // wait a few milliseconds to add menu open style for fade in transition styles to work
            rootMenu.schedule.Execute(() =>
            {
                SetRootMenuPosition();
                rootMenu.AddToClassList(menuOpenClassname);
            }).ExecuteLater(10);
#endif
        }

#if UNITY_2023_1_OR_NEWER
        private async void SetRootMenuOpenStyleNextFrame()
        {
            await Awaitable.NextFrameAsync();
            SetRootMenuPosition();
            rootMenu.AddToClassList(menuOpenClassname);
        }
#endif

        /// <summary>
        /// Opens the drop-down anchored to the bottom border of the target world bounds.
        /// </summary>
        public void Open(DropdownMenu menu, EventBase evt)
        {
            Rect worldBound = default;

            if (evt != null)
            {
                VisualElement targetElement = evt.target as VisualElement;
                if (targetElement != null)
                {
                    worldBound = targetElement.worldBound;
                }
            }

            Open(menu, worldBound, evt);
        }

        /// <summary>
        /// Opens the dropdown at the given world position.
        /// </summary>
        public void Open(DropdownMenu menu, Vector2 worldPosition, EventBase evt = null)
        {
            Open(menu, new Rect(worldPosition, Vector2.zero), evt);
        }

        /// <summary>
        /// Closes the dropdown.
        /// </summary>
        public void Close()
        {
            if (rootMenu != null)
            {
                rootMenu.CloseSubMenus();
            }
            blockingLayer.RemoveFromHierarchy();

            targetElement?.RemoveFromClassList(targetOpenClassname);
            targetElement = null;
        }

        private void Build(DropdownMenu menu)
        {
            rootMenu?.ReturnToPool();
            rootMenu = subMenuPool.GetPooled();
            rootMenu.AddToClassList(menuDepthClassname + "0");

            SetMenu?.Invoke(rootMenu.menuContainer, Array.Empty<string>(), 0);

            blockingLayer.Add(rootMenu);
            foreach (DropdownMenuItem item in menu.MenuItems())
            {
                if (item is DropdownMenuSeparator s)
                    rootMenu.AddItem(item, s.subMenuPath.Split('/'), 0);
                else if (item is DropdownMenuAction a)
                    rootMenu.AddItem(item, a.name.Split('/'), 0);
            }

            rootMenu.SetSubItemStates();
        }

        private void SetRootMenuPosition()
        {
            Rect rootWorldBound = root.worldBound;

            Vector2 worldPos = new Vector2(targetRect.xMin, targetRect.yMax);
            Vector2 localPos = root.WorldToLocal(worldPos);
            Vector2 offset = new Vector2(rootMenu.resolvedStyle.marginLeft, rootMenu.resolvedStyle.marginTop);
            Vector2 menuSize = rootMenu.worldBound.size;

            Vector2 maxPos = worldPos + offset + menuSize;

            // align root menu to the right side if it exceeds the bounds horizontally
            if (maxPos.x > rootWorldBound.xMax)
            {
                rootMenu.style.right = rootMenu.resolvedStyle.marginRight;
                rootMenu.style.left = StyleKeyword.Null;
                rootMenu.AddToClassList(menuRightwardsClassname);
            }
            else
            {
                rootMenu.style.left = localPos.x + rootMenu.resolvedStyle.marginLeft;
                rootMenu.style.right = StyleKeyword.Null;
            }

            // align root menu to the bottom if it exceeds the bounds vertically
            if (maxPos.y > rootWorldBound.yMax)
            {
                rootMenu.style.bottom = rootMenu.resolvedStyle.marginBottom;
                rootMenu.style.top = StyleKeyword.Null;

                rootMenu.AddToClassList(menuUpwardsClassname);
            }
            else
            {
                rootMenu.style.top = localPos.y + rootMenu.resolvedStyle.marginTop;
                rootMenu.style.bottom = StyleKeyword.Null;
            }
        }

        private ActionItemManipulator MakeActionItem()
        {
            VisualElement ve = MakeItem();
            ve.AddToClassList(itemClassname);
            ve.focusable = true;
            ActionItemManipulator m = new ActionItemManipulator();
            ve.AddManipulator(m);

            return m;
        }

        private SubItemManipulator MakeSubItem()
        {
            VisualElement ve = MakeItem();
            ve.AddToClassList(itemClassname);
            ve.AddToClassList(subItemClassname);
            ve.focusable = true;
            SubItemManipulator m = new SubItemManipulator();
            ve.AddManipulator(m);
            return m;
        }

        private void ResetItem(ItemManipulator item)
        {
            VisualElement target = item.target;

            item.Reset();

            // remove all classes that might have been added 
            // to the target while they were in use
            target.RemoveFromClassList(hiddenItemClassname);
            target.RemoveFromClassList(checkedItemClassname);
            target.RemoveFromClassList(disabledItemClassname);

            target.RemoveFromClassList(openedItemClassname);

            // Clear any bindings that might have been added to the element or its children (only from Unity Version 2023.2 or newer
            ClearBindingsRecursively(target);
        }

        private void ClearBindingsRecursively(VisualElement element)
        {
#if UNITY_2023_2_OR_NEWER
            element.ClearBindings();
            foreach (VisualElement child in element.Children())
                ClearBindingsRecursively(child);
#endif
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
