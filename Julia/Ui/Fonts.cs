using Julia.Interfaces.Drawing;

namespace Julia.Ui
{
    class Fonts
    {
        public static readonly Font Condensed;
        public static readonly Font Clocktopia;
        public static readonly Font Console;

        static Fonts()
        {
            Condensed = new Font(@"../Resources/Condensed.xml");
            Clocktopia = new Font(@"../Resources/Clocktopia.xml");
            Console = new Font(@"../Resources/Console.xml");
        }
    }
}
