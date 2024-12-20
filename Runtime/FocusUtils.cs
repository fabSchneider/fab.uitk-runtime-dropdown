using UnityEngine.UIElements;

namespace Fab.UITKDropdown
{
    public static class FocusUtils
    {

        /// <summary>
        /// Returns the relative order in the visual tree between two elements. 
        /// A negative return value indicates that the first element is 'below' the second element in the hierarchy. 
        /// A positive return value indicates that the first element is 'above' the second element in the hierarchy. 
        /// A return value of 0 means both elements are the same element or don't share the same visual tree.
        /// </summary>
        public static int GetRelativeOrderInVisualTree(VisualElement elem1, VisualElement elem2)
        {
            if (elem1 == null || elem2 == null || elem1 == elem2)
                return 0;

            VisualElement commonAncestor = elem1.FindCommonAncestor(elem2);
            if (commonAncestor == null)
                return 0;

            if (commonAncestor == elem1)
                return -1;

            if (commonAncestor == elem2)
                return 1;

            VisualElement elem1Parent = elem1;
            while (elem1Parent.hierarchy.parent != commonAncestor)
            {
                elem1Parent = elem1Parent.hierarchy.parent;
            }

            VisualElement elem2Parent = elem2;
            while (elem2Parent.hierarchy.parent != commonAncestor)
            {
                elem2Parent = elem2Parent.hierarchy.parent;
            }

            if (commonAncestor.IndexOf(elem1Parent) < commonAncestor.IndexOf(elem2Parent))
                return -1;

            return 1;
        }

        /// <summary>
        /// Returns the first previous sibling of an element that is focusable.
        /// </summary>
        public static VisualElement FindPreviousFocusableSibling(VisualElement element)
        {
            int idx = element.hierarchy.parent.IndexOf(element);
            int prevIdx = (element.hierarchy.parent.childCount + idx - 1) % element.hierarchy.parent.childCount;
            while (idx != prevIdx)
            {
                VisualElement prevElement = element.hierarchy.parent[prevIdx];
                if (prevElement.focusable)
                {
                    return prevElement;
                }
                prevIdx = (element.hierarchy.parent.childCount + prevIdx - 1) % element.hierarchy.parent.childCount;
            }

            return element;
        }

        /// <summary>
        /// Returns the first next sibling of an element that is focusable.
        /// </summary>
        public static VisualElement FindNextFocusableSibling(VisualElement element)
        {
            int idx = element.hierarchy.parent.IndexOf(element);
            int nextIdx = (idx + 1) % element.hierarchy.parent.childCount;
            while (idx != nextIdx)
            {
                VisualElement nextElement = element.hierarchy.parent[nextIdx];
                if (nextElement.focusable)
                {
                    return nextElement;
                }
                nextIdx = (nextIdx + 1) % element.hierarchy.parent.childCount;
            }
            return element;
        }

        /// <summary>
        /// Returns the first child of an element that is focusable.
        /// </summary>
        public static VisualElement FindFirstFocusableChild(VisualElement element)
        {
            foreach (VisualElement child in element.Children())
            {
                if (child.focusable)
                    return child;
            }
            return null;
        }

        /// <summary>
        /// Recursively looks for the first child of an element that is focusable.
        /// </summary>
        public static VisualElement FindFirstFocusableChildRecursive(VisualElement element)
        {
            foreach (VisualElement child in element.Children())
            {
                if (child.focusable)
                    return child;
                VisualElement nested = FindFirstFocusableChildRecursive(child);
                if (nested != null)
                    return nested;
            }
            return null;
        }
    }
}
