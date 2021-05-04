using System;

using PolyOne.Utility;
using PolyOne.Scenes;
using PolyOne.Engine;
using PolyOne.LevelProcessor;

using Runner2.Platforms;

namespace Runner2
{
    public enum GameTags
    {
        None = 0,
        Player = 1,
        Solid = 2,
        Empty = 3,
        Exit = 4
    }

    public class Level : Scene, IDisposable
    {
        LevelTilesSolid tilesSolid;
        LevelTilesEmpty tilesEmpty;

        bool[,] collisionInfoSolid;
        bool[,] collisionInfoEmpty;

        public LevelTiler Tile { get; private set; }
        LevelData levelData = new LevelData();

        Player player;

        public Level()
        {
        }

        public void LoadLevel(string levelName)
        {
            LoadContent();

            Tile = new LevelTiler();

            levelData = Engine.Instance.Content.Load<LevelData>(levelName);
            Tile.LoadContent(levelData);

            collisionInfoSolid = LevelTiler.TileConverison(Tile.CollisionLayer, 2);
            tilesSolid = new LevelTilesSolid(collisionInfoSolid);
            this.Add(tilesSolid);

            collisionInfoEmpty = LevelTiler.TileConverison(Tile.CollisionLayer, 0);
            tilesEmpty = new LevelTilesEmpty(collisionInfoEmpty);
            this.Add(tilesEmpty);

            player = new Player(Tile.PlayerPosition[0]);
            this.Add(player);
            player.Added(this);
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }
        public override void UnloadContent()
        {
            base.UnloadContent();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Draw()
        {
            Engine.Begin(player.Camera.TransformMatrix);
            Tile.DrawImageBackground();
            Tile.DrawBackground();
            base.Draw();
            Engine.End();
        }

        public void Dispose()
        {
        }
    }
}
