using System.ComponentModel;

namespace Julia.Ui
{
    public enum MenuSpeed
    {
        [Description("No animation")]
        None = 0,
        [Description("Very fast")]
        VeryFast = 40,
        [Description("Fast")]
        Fast = 20,
        [Description("Normal")]
        Normal = 10,
        [Description("Slow")]
        Slow = 4
    }
}
