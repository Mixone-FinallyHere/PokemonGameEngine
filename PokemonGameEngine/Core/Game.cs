﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonGameEngine.GUI;
using Kermalis.PokemonGameEngine.GUI.Battle;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Script;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Core
{
    internal sealed class Game
    {
        public static Game Instance { get; private set; }

        public Save Save { get; }
        public StringBuffers StringBuffers { get; }

        public readonly List<ScriptContext> Scripts = new List<ScriptContext>();
        public readonly List<MessageBox> MessageBoxes = new List<MessageBox>();

        public OverworldGUI OverworldGUI { get; }
        private FadeFromColorTransition _fadeFromTransition;
        private FadeToColorTransition _fadeToTransition;
        private SpiralTransition _battleTransition;
        public BattleGUI BattleGUI { get; private set; }
        private BagGUI _bagGUI;

        public Game()
        {
            Instance = this;
            Save = new Save(); // Load/initialize Save
            StringBuffers = new StringBuffers();
            var map = Map.LoadOrGet(0);
            const int x = 2;
            const int y = 29;
            PlayerObj.Player.Pos.X = x;
            PlayerObj.Player.Pos.Y = y;
            PlayerObj.Player.Map = map;
            map.Objs.Add(PlayerObj.Player);
            CameraObj.Camera.Pos = PlayerObj.Player.Pos;
            CameraObj.Camera.Map = map;
            map.Objs.Add(CameraObj.Camera);
            map.LoadObjEvents();
            OverworldGUI = new OverworldGUI();
        }

        public void TempWarp(IWarp warp)
        {
            void FadeToTransitionEnded()
            {
                Obj player = PlayerObj.Player;
                player.Warp(warp);
                void FadeFromTransitionEnded()
                {
                    _fadeFromTransition = null;
                }
                _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromTransitionEnded);
                if (player.QueuedScriptMovements.Count > 0)
                {
                    player.RunNextScriptMovement();
                }
                _fadeToTransition = null;
            }
            _fadeToTransition = new FadeToColorTransition(20, 0, FadeToTransitionEnded);
        }

        // Temp - start a test wild battle
        public void TempCreateWildBattle(Map map, Map.Layout.Block block, EncounterTable.Encounter encounter)
        {
            Save sav = Save;
            var me = new PBETrainerInfo(sav.PlayerParty, sav.PlayerName, inventory: sav.PlayerInventory.ToPBEInventory());
            var wildParty = new Party { new PartyPokemon(encounter) };
            var trainerParties = new Party[] { sav.PlayerParty, wildParty };
            var wild = new PBEWildInfo(wildParty);
            void OnBattleEnded()
            {
                void FadeFromTransitionEnded()
                {
                    _fadeFromTransition = null;
                }
                _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromTransitionEnded);
                BattleGUI = null;
            }

            PBEBattleTerrain terrain = Overworld.GetPBEBattleTerrainFromBlock(block.BlocksetBlock);
            BattleGUI = new BattleGUI(new PBEBattle(PBEBattleFormat.Single, PkmnConstants.PBESettings, me, wild,
                battleTerrain: terrain,
                weather: Overworld.GetPBEWeatherFromMap(map)),
                OnBattleEnded,
                trainerParties,
                isCave: terrain == PBEBattleTerrain.Cave,
                isDarkGrass: block.BlocksetBlock.Behavior == BlocksetBlockBehavior.Grass_SpecialEncounter,
                isFishing: false,
                isSurfing: block.BlocksetBlock.Behavior == BlocksetBlockBehavior.Surf,
                isUnderwater: false);
            void OnBattleTransitionEnded()
            {
                _battleTransition = null;
            }
            _battleTransition = new SpiralTransition(OnBattleTransitionEnded);
        }
        public void TempCreateWildBattle(PartyPokemon wildPkmn)
        {
            PlayerObj player = PlayerObj.Player;
            Map.Layout.Block block = player.GetBlock(out Map map);
            Save sav = Save;
            var me = new PBETrainerInfo(sav.PlayerParty, sav.PlayerName, inventory: sav.PlayerInventory.ToPBEInventory());
            var wildParty = new Party { wildPkmn };
            var trainerParties = new Party[] { sav.PlayerParty, wildParty };
            var wild = new PBEWildInfo(wildParty);
            void OnBattleEnded()
            {
                void FadeFromTransitionEnded()
                {
                    _fadeFromTransition = null;
                }
                _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromTransitionEnded);
                BattleGUI = null;
            }

            PBEBattleTerrain terrain = Overworld.GetPBEBattleTerrainFromBlock(block.BlocksetBlock);
            BattleGUI = new BattleGUI(new PBEBattle(PBEBattleFormat.Single, PkmnConstants.PBESettings, me, wild,
                battleTerrain: terrain,
                weather: Overworld.GetPBEWeatherFromMap(map)),
                OnBattleEnded,
                trainerParties,
                isCave: terrain == PBEBattleTerrain.Cave,
                isDarkGrass: block.BlocksetBlock.Behavior == BlocksetBlockBehavior.Grass_SpecialEncounter,
                isFishing: false,
                isSurfing: block.BlocksetBlock.Behavior == BlocksetBlockBehavior.Surf,
                isUnderwater: false);
            void OnBattleTransitionEnded()
            {
                _battleTransition = null;
            }
            _battleTransition = new SpiralTransition(OnBattleTransitionEnded);
        }

        public void OpenStartMenu()
        {
            void FadeToTransitionEnded()
            {
                void OnBagMenuGUIClosed()
                {
                    void FadeFromTransitionEnded()
                    {
                        _fadeFromTransition = null;
                    }
                    _fadeFromTransition = new FadeFromColorTransition(20, 0, FadeFromTransitionEnded);
                    _bagGUI = null;
                }
                _bagGUI = new BagGUI(Save.PlayerInventory, Save.PlayerParty, OnBagMenuGUIClosed);
                _fadeToTransition = null;
            }
            _fadeToTransition = new FadeToColorTransition(20, 0, FadeToTransitionEnded);
        }

        public void LogicTick()
        {
            DateTime time = DateTime.Now;
            foreach (ScriptContext ctx in Scripts.ToArray()) // Copy the list so a script ending/starting does not crash here
            {
                ctx.LogicTick();
            }
            foreach (MessageBox mb in MessageBoxes.ToArray())
            {
                mb.LogicTick();
            }
            Tileset.AnimationTick(); // TODO: Don't run in battles like we are now
            DayTint.LogicTick(time); // TODO: Don't run in locations where there's no day tint (and then set the tint automatically upon exit so there's no transition)
            if (_battleTransition != null || _fadeFromTransition != null || _fadeToTransition != null)
            {
                return;
            }
            if (_bagGUI != null)
            {
                _bagGUI.LogicTick();
                return;
            }
            if (BattleGUI != null)
            {
                BattleGUI.LogicTick();
                return;
            }
            OverworldGUI.LogicTick();
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight, string topLeftMessage)
        {
            if (_bagGUI != null)
            {
                _bagGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                goto transitions;
            }
            if (_battleTransition != null)
            {
                _battleTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                goto bottom;
            }
            if (BattleGUI != null)
            {
                BattleGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                goto bottom;
            }
            OverworldGUI.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        transitions:
            if (_fadeFromTransition != null)
            {
                _fadeFromTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                goto bottom;
            }
            if (_fadeToTransition != null)
            {
                _fadeToTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
                goto bottom;
            }
        bottom:
            foreach (MessageBox mb in MessageBoxes.ToArray())
            {
                mb.Render(bmpAddress, bmpWidth, bmpHeight);
            }
            if (topLeftMessage != null)
            {
                Font.Default.DrawString(bmpAddress, bmpWidth, bmpHeight, 0, 0, topLeftMessage, Font.DefaultFemale);
            }
        }
    }
}
