namespace JScr.CsImpl
{
    public static class CsImplBase
    {
        // Functions for finding methods and stuff idk
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class JScrMethodTargetAttribute : Attribute
    {
        public JScrMethodTargetAttribute(JScrMethodTargetMode mode = JScrMethodTargetMode.Normal, string? customMemberName = null) => Construct(mode, customMemberName);
        public JScrMethodTargetAttribute(string customMemberName) => Construct(default, customMemberName);
        public JScrMethodTargetAttribute(JScrMethodTargetMode mode) => Construct(mode, default);

        private void Construct(JScrMethodTargetMode mode = JScrMethodTargetMode.Normal, string? customMemberName = null)
        {
            Mode = mode;
            Name = customMemberName;
        }

        public JScrMethodTargetMode Mode { get; private set; }
        public string? Name { get; internal set; }
    }

    public enum JScrMethodTargetMode
    {
        Disable,
        Normal,
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class JScrClassTargetAttribute : Attribute
    {
        public JScrClassTargetAttribute(string namespace_, JScrClassTargetMode mode = JScrClassTargetMode.TargetsOnly)
        {
            Namespace = namespace_;
            Mode = mode;
        }

        public string Namespace { get; private set; }
        public JScrClassTargetMode Mode { get; private set; }
    }

    public enum JScrClassTargetMode
    {
        Disable,
        TargetsOnly,
        AllByDefault,
    }
}
