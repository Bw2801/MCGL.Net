using MCGL;

namespace MCGLExample
{
    class Program
    {
        static void Main(string[] args)
        {
            new WarpExample();
        }

        private class WarpExample : Chain
        {
            // ====================
            // WARP ARROWS
            // >> scoreboard useBow has to be created as stat.useItem.minecraft.bow
            // ====================

            public WarpExample() : base(CommandType.REPEAT, true)
            {
                Run();
            }

            private void Run()
            {
                var arrow = Entities.GetSingle(EntityType.Arrow);
                var player = Entities.Player;

                PushExecutionAs(player.WithMinScore("useBow", 1));
                {
                    AddTag("teleport", arrow.InRadius(null, 2).WithTag("!teleport"));
                    GiveItem(player, ItemConstant.ARROW);
                    ResetScore(player, "useBow");
                }
                PopExecution();

                PushExecutionAs(arrow.WithTag("teleport"), NBTObject.InGround);
                {
                    TeleportToEntity(player, arrow.InRadius(null, 0));
                    Kill(arrow.InRadius(null, 0));
                }
                PopExecution();

                GenerateSchematics("warp.schematic");
            }
        }
    }
}
