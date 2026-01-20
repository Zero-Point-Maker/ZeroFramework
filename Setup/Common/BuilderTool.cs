namespace ZF.Setup
{
    [System.Serializable]
    public struct BuilderTool
    {
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.GUIColor("RGB(0, 1, 0)")]
#endif
        public string name;

        public string url;
    }
}