using System;
using UnityEngine.UIElements;

namespace Fab.UITKDropdown
{
    [Flags]
    public enum BlockingBehavior
    {
        None = 0,
        PickThrough = 1,
        CloseOnClick = 2,
        CloseOnCancel = 4,
        Closeable = CloseOnClick | CloseOnCancel,
        All = PickThrough | CloseOnClick | CloseOnCancel
    }

    public class BlockingLayer : VisualElement
    {
        private BlockingBehavior m_blockingBehavior;

        /// <summary>
        /// When enabled clicking the layer will close it.
        /// </summary>
        public bool closeOnClick
        {
            get => m_blockingBehavior.HasFlag(BlockingBehavior.CloseOnClick);
            set => SetCloseOnClick(value);
        }

        /// <summary>
        /// When enabled a navigation cancel will close the layer.
        /// </summary>
        public bool closeOnCancel
        {
            get => m_blockingBehavior.HasFlag(BlockingBehavior.CloseOnCancel);
            set => SetCloseOnCancel(value);
        }

        /// <summary>
        /// When enabled pointer actions will effect elements underneath the layer
        /// </summary>
        public bool pickThrough
        {
            get => m_blockingBehavior.HasFlag(BlockingBehavior.PickThrough);
            set => SetPickThrough(value);
        }

        /// <summary>
        /// Sets the behavior of the blocking layer on interaction.
        /// </summary>
        public BlockingBehavior blockingBehavior
        {
            get => m_blockingBehavior;
            set
            {
                closeOnClick = value.HasFlag(BlockingBehavior.CloseOnClick);
                closeOnCancel = value.HasFlag(BlockingBehavior.CloseOnCancel);
                pickThrough = value.HasFlag(BlockingBehavior.PickThrough);
            }
        }

        public BlockingLayer()
        {
            focusable = true;
            //set tab index to -1 to avoid blocking layer
            // being picked by the focus ring
            tabIndex = -1;
            this.StretchToParentSize();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Focus();
            evt.destinationPanel.visualTree.RegisterCallback<FocusInEvent>(OnFocusIn);

            if (pickThrough)
                evt.destinationPanel.visualTree.RegisterCallback<PointerDownEvent>(OnPointerDownPickThrough, TrickleDown.TrickleDown);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            evt.originPanel.visualTree.UnregisterCallback<FocusInEvent>(OnFocusIn);
            evt.originPanel.visualTree.UnregisterCallback<PointerDownEvent>(OnPointerDownPickThrough, TrickleDown.TrickleDown);
        }


        private void OnFocusIn(FocusInEvent evt)
        {
            VisualElement focusTarget = evt.target as VisualElement;

            if (focusTarget == null || focusTarget == this)
                return;

            // check if focus target is 'below' the blocking layer in the hierarchy
            if (FocusUtils.GetRelativeOrderInVisualTree(focusTarget, this) <= 0)
            {
                // HACK: prevent elements under the blocking layer to be focused;
                Focus();
            }
        }

        private void SetPickThrough(bool value)
        {
            if (pickThrough == value)
                return;

            if (value)
                m_blockingBehavior |= BlockingBehavior.PickThrough;
            else
                m_blockingBehavior &= ~BlockingBehavior.PickThrough;

            if (value)
            {
                // ignore picking but register pointer down events on the root element to enable closing on click.
                pickingMode = PickingMode.Ignore;
                panel?.visualTree.RegisterCallback<PointerDownEvent>(OnPointerDownPickThrough, TrickleDown.TrickleDown);
            }
            else
            {
                panel?.visualTree.UnregisterCallback<PointerDownEvent>(OnPointerDownPickThrough, TrickleDown.TrickleDown);
                pickingMode = PickingMode.Position;
            }
        }

        private void SetCloseOnClick(bool value)
        {
            if (closeOnClick == value)
                return;

            if (value)
                m_blockingBehavior |= BlockingBehavior.CloseOnClick;
            else
                m_blockingBehavior &= ~BlockingBehavior.CloseOnClick;

            if (value && pickThrough == false)
            {
                RegisterCallback<PointerDownEvent>(OnPointerDown);
            }
            else if (!value && pickThrough == false)
            {
                UnregisterCallback<PointerDownEvent>(OnPointerDown);
            }
        }
        private void SetCloseOnCancel(bool value)
        {
            if (closeOnCancel == value)
                return;

            if (value)
            {
                m_blockingBehavior |= BlockingBehavior.CloseOnCancel;
                // closing behavior when pressing the navigation cancel event
                RegisterCallback<NavigationCancelEvent>(CloseOnNavigationCancel);
            }
            else
            {
                m_blockingBehavior &= ~BlockingBehavior.CloseOnCancel;
                UnregisterCallback<NavigationCancelEvent>(CloseOnNavigationCancel);
            }
        }

        private void OnPointerDownPickThrough(PointerDownEvent evt)
        {
            if (closeOnClick)
            {

                VisualElement pick = panel.Pick(evt.position);

                if (pick == null)
                {
                    // nothing has been picked, close the layer
                    RemoveFromHierarchy();
                }
                else
                {
                    // close the layer if the picked element is below it
                    if (FocusUtils.GetRelativeOrderInVisualTree(pick, this) < 1)
                    {
                        RemoveFromHierarchy();
                    }
                }
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.target == this)
            {
                IPanel panel = this.panel;

                if (closeOnClick)
                    RemoveFromHierarchy();

                if (!pickThrough)
                {
                    pickingMode = PickingMode.Ignore;
                    VisualElement pick = panel.Pick(evt.position);
                    pickingMode = PickingMode.Position;
                    if (pick != null)
                    {
                        // resend the pointer event to pass it through the blocking layer to elements underneath
                        using (PointerDownEvent pointerDownEvent = PointerDownEvent.GetPooled(evt))
                        {
                            pointerDownEvent.target = pick;
                            pick.SendEvent(pointerDownEvent);
                        }
                    }
                }
            }
        }
        private void CloseOnNavigationCancel(NavigationCancelEvent evt)
        {
            RemoveFromHierarchy();
        }
    }
}
