namespace SnapGrab
{
    internal class KeyDef
    {
        public uint Vk;

        public string Label;

        public string Aliases;

        public KeyDef(uint vk, string label, string aliases)
        {
            Vk = vk;
            Label = label;
            Aliases = aliases;
        }
    }
}
