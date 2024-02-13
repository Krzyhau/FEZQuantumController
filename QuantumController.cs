using Common;
using FezEngine.Components;
using FezEngine.Tools;
using FezGame;
using FezGame.Components;
using FezGame.Services;
using FEZEL.Helpers;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FEZEL
{
    public class FEZEL : DrawableGameComponent
    {
        public static Fez Fez { get; private set; }

        public static CodeMachineBridge CodeMachine { get; private set; }

        public FEZEL(Game game) : base(game)
        {
            Fez = (Fez)game;
            Enabled = true;
            Visible = true;
            DrawOrder = 99999;
        }

        public override void Initialize()
        {
            base.Initialize();

            DrawingTools.Init();
            CodeMachine = new CodeMachineBridge();
        }
         
        public override void Update(GameTime gameTime)
        {
            InputHelper.Update(gameTime);

            CodeMachine.SetBits(new int[36]
            {
                0, 1, 1, 1, 1, 1,
                1, 1, 1, 0, 0, 0,
                1, 1, 1, 0, 0, 0,
                1, 1, 1, 1, 1, 1,
                0, 1, 1, 0, 1, 1,
                0, 1, 1, 0, 1, 1,
            });
        }

        public override void Draw(GameTime gameTime)
        {
            //DrawingTools.BeginBatch();

            //DrawingTools.EndBatch();
        }
    }
}
