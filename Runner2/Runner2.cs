using Microsoft.Xna.Framework;

using PolyOne.Engine;
using PolyOne.Utility;

using Runner2.Screens;

namespace Runner2
{
    public class Runner2 : Engine
    {
        static readonly string[] preloadAssets =
       {
            "MenuAssets/gradient",
       };

        public Runner2()
            :base(640, 360, "Runner2.0", 2.0f, false)
        {
        }

        protected override void Initialize()
        {
            base.Initialize();

            TileInformation.TileDiemensions(16, 16);

            screenManager.AddScreen(new BackgroundScreen(), null);
            screenManager.AddScreen(new MainMenuScreen(), null);
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            foreach (string asset in preloadAssets)
            {
                Engine.Instance.Content.Load<object>(asset);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Runner2 game = new Runner2())
            {
                game.Run();
            }
        }
    }
}
