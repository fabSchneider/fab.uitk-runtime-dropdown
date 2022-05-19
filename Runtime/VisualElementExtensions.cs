using UnityEngine.UIElements;

namespace Fab.UITKDropdown
{
    public static class VisualElementExtensions
    {
        public static VisualElement WithName(this VisualElement element, string name)
        {
            element.name = name;
            return element;
        }

        public static VisualElement WithClass(this VisualElement element, string className)
        {
            element.AddToClassList(className);
            return element;
        }

        public static VisualElement WithAbsoluteFill(this VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.right = 0f;
            element.style.top = 0f;
            element.style.bottom = 0f;

            return element;
        }
    }
}
