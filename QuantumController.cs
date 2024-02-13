using Common;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame;
using FezGame.Components;
using FezGame.Services;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace FEZEL
{
    public class QuantumController : DrawableGameComponent
    {
        private static readonly Random Random = new();
        private readonly List<TrileInstance> RandomInstances = new();
        private readonly List<TrileInstance> CleanInstances = new();
        private readonly List<Vector4> AllEmplacements = new();

        private int FreezeFrames;

        [ServiceDependency]
        public IGameCameraManager CameraManager { private get; set; }

        [ServiceDependency]
        public IPlayerManager PlayerManager { private get; set; }

        [ServiceDependency]
        public IGameLevelManager LevelManager { private get; set; }

        [ServiceDependency]
        public ILevelMaterializer LevelMaterializer { private get; set; }

        [ServiceDependency]
        public IGameStateManager GameState { private get; set; }


        public static Fez Fez { get; private set; }

        public IDetour QuantumizerDetour;

        public QuantumController(Game game) : base(game)
        {
            Fez = (Fez)game;
            Enabled = true;
            Visible = true;
            DrawOrder = 99999;
        }

        public override void Initialize()
        {
            base.Initialize();

            QuantumizerDetour = new Hook(
                typeof(Quantumizer).GetMethod("Update"),
                new Action<Action<Quantumizer, GameTime>, Quantumizer, GameTime>((orig, self, gameTime) => {
                    Quantumizer_Update(self, gameTime);
                })
            );
        }

        private void Quantumizer_Update(Quantumizer self, GameTime gameTime)
        {
            if (GameState.Loading || GameState.Paused || GameState.InMap || GameState.InFpsMode || GameState.InMenuCube || !CameraManager.Viewpoint.IsOrthographic())
            {
                return;
            }

            if (CameraManager.ProjectionTransitionNewlyReached)
            {
                LevelMaterializer.CullInstances();
            }

            if (!RefreshQuantumTriles(self))
            {
                return;
            }

            UpdateFreezeFrames();

            UpdateQuantumTriles(self);
            RecullQuantumTriles();

            base.Update(gameTime);
        }

        private bool RefreshQuantumTriles(Quantumizer self)
        {
            var BatchedInstances = (List<TrileInstance>)self.GetType().GetField("BatchedInstances", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(self);

            RandomInstances.Clear();
            CleanInstances.Clear();
            AllEmplacements.Clear();

            for (int num = BatchedInstances.Count - 1; num >= 0; num--)
            {
                var trileInstance = BatchedInstances[num];
                if (trileInstance.InstanceId == -1)
                {
                    BatchedInstances.RemoveAt(num);
                    trileInstance.RandomTracked = false;
                    continue;
                }

                if (ShouldBeQuantum(trileInstance))
                {
                    AllEmplacements.Add(trileInstance.Data.PositionPhi);
                    RandomInstances.Add(trileInstance);
                }
                else
                {
                    CleanInstances.Add(trileInstance);
                }
            }

            return BatchedInstances.Count > 0;
        }

        private bool ShouldBeQuantum(TrileInstance trileInstance)
        {
            var positionPhi = trileInstance.Data.PositionPhi;
            var trileScreenPos = PhiPosToScreenPos(positionPhi);

            return trileScreenPos.Length() > 0.5f;
        }


        private Vector2 PhiPosToScreenPos(Vector4 positionPhi)
        {
            var relativeDistance = CameraManager.Center - new Vector3(positionPhi.X, positionPhi.Y, positionPhi.Z);

            var screenX = relativeDistance.Dot(CameraManager.View.Right);
            var screenY = relativeDistance.Dot(CameraManager.View.Up);

            var sizeX = Math.Abs(CameraManager.Frustum.Left.D + CameraManager.Frustum.Right.D);
            var sizeY = Math.Abs(CameraManager.Frustum.Top.D + CameraManager.Frustum.Bottom.D);

            var screenPos = new Vector2(screenX / sizeX, screenY / sizeY);

            return screenPos;
        }


        private void UpdateFreezeFrames()
        {
            FreezeFrames--;
            if (FreezeFrames < 0)
            {
                if (RandomHelper.Probability(0.02))
                {
                    FreezeFrames = Random.Next(0, 15);
                }
            }
        }

        private void UpdateQuantumTriles(Quantumizer self)
        {
            var RandomTrileIds = (int[])self.GetType().GetField("RandomTrileIds", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);

            if (RandomHelper.Probability(0.9))
            {
                int instancesToRefresh = Random.Next(0, FreezeFrames >= 0 ? (RandomInstances.Count / 50) : RandomInstances.Count);
                while (instancesToRefresh-- >= 0 && RandomInstances.Count > 0)
                {
                    int trileInstanceIndex = Random.Next(0, RandomInstances.Count);
                    var trileInstance = RandomInstances[trileInstanceIndex];
                    RandomInstances.RemoveAt(trileInstanceIndex);

                    if (!trileInstance.VisualTrileId.HasValue || trileInstance.TrileId == trileInstance.VisualTrileId)
                    {
                        if (!LevelMaterializer.CullInstanceOut(trileInstance))
                        {
                            LevelMaterializer.CullInstanceOut(trileInstance, skipUnregister: true);
                        }
                        trileInstance.VisualTrileId = RandomHelper.InList(RandomTrileIds);
                        trileInstance.RefreshTrile();
                        LevelMaterializer.CullInstanceIn(trileInstance, forceAdd: true);
                    }
                    trileInstance.NeedsRandomCleanup = true;
                    if (trileInstance.InstanceId != -1)
                    {
                        int emplacementIndex = Random.Next(0, RandomInstances.Count);
                        var emplacement = AllEmplacements[emplacementIndex];
                        AllEmplacements.RemoveAt(emplacementIndex);
                        LevelMaterializer.GetTrileMaterializer(trileInstance.VisualTrile).FakeUpdate(trileInstance.InstanceId, emplacement);
                    }
                }
            }
        }


        private void RecullQuantumTriles()
        {
            var SsPosToRecull = new HashSet<Point>();

            foreach (var cleanInstance in CleanInstances)
            {
                if (cleanInstance.VisualTrileId.HasValue)
                {
                    if (!LevelMaterializer.CullInstanceOut(cleanInstance))
                    {
                        LevelMaterializer.CullInstanceOut(cleanInstance, skipUnregister: true);
                    }
                    cleanInstance.VisualTrileId = null;
                    cleanInstance.RefreshTrile();
                    if (CameraManager.ViewTransitionReached)
                    {
                        var emplacement = cleanInstance.Emplacement;
                        var screenSpaceHorizontalPos = (CameraManager.Viewpoint.SideMask() == Vector3.Right) ? emplacement.X : emplacement.Z;
                        SsPosToRecull.Add(new Point(screenSpaceHorizontalPos, emplacement.Y));
                    }
                    else
                    {
                        LevelMaterializer.CullInstanceIn(cleanInstance, forceAdd: true);
                    }
                }
                else if (cleanInstance.NeedsRandomCleanup)
                {
                    LevelMaterializer.GetTrileMaterializer(cleanInstance.Trile).UpdateInstance(cleanInstance);
                    cleanInstance.NeedsRandomCleanup = false;
                }
            }

            if (SsPosToRecull.Count > 0)
            {
                foreach (var item in SsPosToRecull)
                {
                    LevelManager.RecullAt(item);
                }
            }
        }
    }
}
