namespace ZF.Setup
{
    /// <summary>
    /// 模块索引结构
    /// 用于集中式 bin 文件的索引区
    /// </summary>
    [System.Serializable]
    public struct ModuleIndex
    {
        /// <summary>
        /// 模块标识：ModuleName_ComponentType_FolderPath
        /// </summary>
        public string name;
            
        /// <summary>
        /// 数据在文件中的偏移量
        /// </summary>
        public long offset;
            
        /// <summary>
        /// 数据大小
        /// </summary>
        public int size;
    }
}