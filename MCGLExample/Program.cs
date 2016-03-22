using MCGL;

namespace MCGLExample
{
    class Program : Chain
    {
        static void Main(string[] args)
        {
            new Program(CommandType.REPEAT).Run();
        }

        public Program(CommandType type = CommandType.IMPULSE) : base(type, true)
        {
        }

        private void Run()
        {
            Entities.Player.WithTag("CustomTag").ToString();
            var a = 0;
        }
    }
}
