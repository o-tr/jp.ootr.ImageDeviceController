#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace jp.ootr.ImageDeviceController.Editor
{
    public class BaseEditor : UnityEditor.Editor
    {
        private bool _debug;

        protected VisualElement Root;
        protected VisualElement InfoBlock;
        
        [SerializeField] private StyleSheet styleSheet;
        
        public virtual void OnEnable()
        {
            Root = new VisualElement();
            Root.styleSheets.Add(styleSheet);
            Root.AddToClassList("root");
            InfoBlock = new VisualElement();
            InfoBlock.AddToClassList("infoBlock");
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            Root.Clear();
            ShowScriptName();
            
            Root.Add(InfoBlock);
            
            Root.Add(GetLayout());
            
            ShowDebug();
            return Root;
        }
        
        protected virtual VisualElement GetLayout()
        {
            throw new System.NotImplementedException();
        }

        private void ShowScriptName()
        {
            var title = new Label(GetScriptName());
            title.AddToClassList("scriptName");
            Root.Add(title);
        }
        
        protected virtual string GetScriptName()
        {
            return "";
        }

        private void ShowDebug()
        {
            var foldout = new Foldout()
            {
                text = "Debug",
                value = false
            };
            
            foldout.Add(new IMGUIContainer(base.OnInspectorGUI));
            Root.Add(foldout);
        }
    }
}
#endif
