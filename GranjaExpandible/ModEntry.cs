using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Locations;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using xTile;

namespace GranjaExpandible
{
    public class ExpansionZone
    {
        public int Id { get; set; }
        public int ClickX1 { get; set; }
        public int ClickX2 { get; set; }
        public int ClickY { get; set; }
        public int ClearStartX { get; set; }
        public int ClearEndX { get; set; }
        public int ClearStartY { get; set; }
        public int ClearEndY { get; set; }
        public int CostGold { get; set; }
        public int CostDiamonds { get; set; }
        public string Key => $"GranjaExpandible_Zona{Id}";
    }

    public class ModEntry : Mod
    {
        private const string ModDataKeyX = "GranjaExpandible_ExactX";
        private const string ModDataKeyY = "GranjaExpandible_ExactY";
        private const string ModDataKeyComprado = "GranjaExpandible_Comprado";
        private const string BuildingId = "ExpansionAgricola";

        private bool wasInCarpenterMenu = false;
        private bool isWarpingToExpansion = false;

        private readonly List<ExpansionZone> zonasDeExpansion = new List<ExpansionZone>
        {
            new ExpansionZone { Id = 1, ClickX1 = 19, ClickX2 = 20, ClickY = 17, ClearStartX = 2, ClearEndX = 37, ClearStartY = 17, ClearEndY = 20, CostGold = 150000, CostDiamonds = 5 },
            new ExpansionZone { Id = 2, ClickX1 = 19, ClickX2 = 20, ClickY = 37, ClearStartX = 2, ClearEndX = 37, ClearStartY = 37, ClearEndY = 38, CostGold = 300000, CostDiamonds = 10 },
            new ExpansionZone { Id = 3, ClickX1 = 19, ClickX2 = 20, ClickY = 57, ClearStartX = 2, ClearEndX = 37, ClearStartY = 57, ClearEndY = 58, CostGold = 600000, CostDiamonds = 20 },
            new ExpansionZone { Id = 4, ClickX1 = 19, ClickX2 = 20, ClickY = 77, ClearStartX = 2, ClearEndX = 37, ClearStartY = 77, ClearEndY = 78, CostGold = 1200000, CostDiamonds = 40 },

            new ExpansionZone { Id = 5, ClickX1 = 59, ClickX2 = 60, ClickY = 87, ClearStartX = 40, ClearEndX = 77, ClearStartY = 85, ClearEndY = 87, CostGold = 1380000, CostDiamonds = 46 },
            new ExpansionZone { Id = 6, ClickX1 = 59, ClickX2 = 60, ClickY = 65, ClearStartX = 40, ClearEndX = 77, ClearStartY = 63, ClearEndY = 65, CostGold = 1587000, CostDiamonds = 53 },
            new ExpansionZone { Id = 7, ClickX1 = 59, ClickX2 = 60, ClickY = 48, ClearStartX = 40, ClearEndX = 77, ClearStartY = 46, ClearEndY = 48, CostGold = 1825050, CostDiamonds = 61 },
            new ExpansionZone { Id = 8, ClickX1 = 59, ClickX2 = 60, ClickY = 29, ClearStartX = 40, ClearEndX = 77, ClearStartY = 27, ClearEndY = 29, CostGold = 2098808, CostDiamonds = 70 }
        };

        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.World.BuildingListChanged += this.OnBuildingListChanged;
            helper.Events.Player.Warped += this.OnPlayerWarped;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("Maps/ExpansionAgricola"))
            {
                e.LoadFromModFile<xTile.Map>("assets/expansion1.tmx", AssetLoadPriority.Medium);
            }
            if (e.Name.IsEquivalentTo("Buildings/ExpansionAgricola"))
            {
                e.LoadFromModFile<Texture2D>("assets/expansion.png", AssetLoadPriority.Medium);
            }
            if (e.Name.IsEquivalentTo("Data/Locations"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, LocationData>().Data;
                    var locData = new LocationData { CreateOnLoad = new CreateLocationData { MapPath = "Maps/ExpansionAgricola" } };

                    // Copiamos la configuración de pesca de la granja principal para que el mapa reconozca los peces
                    if (data.TryGetValue("Farm", out var farmData) && farmData.Fish != null)
                    {
                        locData.Fish = farmData.Fish;
                    }

                    data[BuildingId] = locData;
                }, AssetEditPriority.Default);
            }
            if (e.Name.IsEquivalentTo("Strings/Buildings"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    data[BuildingId + "_Name"] = "Expansión Agrícola";
                    data[BuildingId + "_Description"] = "Añade una zona de cultivo conectada a tu granja.";
                }, AssetEditPriority.Default);
            }
            if (e.Name.IsEquivalentTo("Data/Buildings"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, BuildingData>().Data;
                    bool yaComprado = Context.IsWorldReady && Game1.player != null && Game1.player.modData.ContainsKey(ModDataKeyComprado);

                    data[BuildingId] = new BuildingData
                    {
                        Name = "Expansión Agrícola",
                        Description = "Añade una zona de cultivo conectada a tu granja.",
                        Texture = "Buildings\\ExpansionAgricola",
                        Builder = yaComprado ? "None" : "Robin",
                        BuildCost = 50000,
                        BuildDays = 0,
                        Size = new Point(3, 3),
                        MagicalConstruction = false,
                        SourceRect = new Rectangle(0, 0, 48, 48),
                        BuildMaterials = new List<BuildingMaterial> { new BuildingMaterial { ItemId = "(O)388", Amount = 200 } }
                    };
                }, AssetEditPriority.Default);
            }
        }

        private void OnBuildingListChanged(object? sender, BuildingListChangedEventArgs e)
        {
            if (e.Location is Farm farm && Game1.player != null)
            {
                foreach (var building in e.Added)
                {
                    if (building.buildingType.Value == BuildingId)
                    {
                        if (building.modData.ContainsKey("FalsoParaRobin")) continue;

                        int startX = building.tileX.Value;
                        int startY = building.tileY.Value;

                        Game1.player.modData[ModDataKeyX] = startX.ToString();
                        Game1.player.modData[ModDataKeyY] = startY.ToString();
                        Game1.player.modData[ModDataKeyComprado] = "true";

                        ReiniciarMapaInterior();

                        building.modData["BorradoPorElMod"] = "true";
                        farm.destroyStructure(building);

                        this.Helper.GameContent.InvalidateCache("Data/Buildings");
                        Game1.addHUDMessage(new HUDMessage("¡Expansión Agrícola construida con éxito!", 2));
                    }
                }

                foreach (var building in e.Removed)
                {
                    if (building.buildingType.Value == BuildingId)
                    {
                        if (building.modData.ContainsKey("BorradoPorElMod")) continue;

                        if (Game1.player.modData.TryGetValue(ModDataKeyX, out string? xStr) && Game1.player.modData.TryGetValue(ModDataKeyY, out string? yStr))
                        {
                            if (int.TryParse(xStr, out int startX) && int.TryParse(yStr, out int startY))
                            {
                                for (int x = startX; x < startX + 3; x++)
                                {
                                    for (int y = startY; y < startY + 3; y++)
                                    {
                                        Vector2 tile = new Vector2(x, y);
                                        if (farm.terrainFeatures.TryGetValue(tile, out var tf) && tf is Flooring)
                                            farm.terrainFeatures.Remove(tile);
                                    }
                                }
                            }
                        }

                        Game1.player.modData.Remove(ModDataKeyComprado);
                        Game1.player.modData.Remove(ModDataKeyX);
                        Game1.player.modData.Remove(ModDataKeyY);

                        foreach (var zona in zonasDeExpansion) Game1.player.modData.Remove(zona.Key);

                        this.Helper.GameContent.InvalidateCache("Data/Buildings");
                        Game1.addHUDMessage(new HUDMessage("Expansión Agrícola demolida y reseteada.", 2));
                    }
                }
            }
        }

        private void ReiniciarMapaInterior()
        {
            var loc = Game1.getLocationFromName(BuildingId);
            if (loc != null)
            {
                loc.objects.Clear();
                loc.terrainFeatures.Clear();
                loc.largeTerrainFeatures.Clear();
                loc.resourceClumps.Clear();

                loc.spawnWeedsAndStones(20, false, false);
                var backLayer = loc.map?.GetLayer("Back");
                if (backLayer != null)
                {
                    int[] validTiles = { 227, 226, 181, 207, 231 };
                    int arbolesPlantados = 0;

                    for (int i = 0; i < 30 && arbolesPlantados < 4; i++)
                    {
                        int x = Game1.random.Next(backLayer.LayerWidth);
                        int y = Game1.random.Next(backLayer.LayerHeight);
                        Vector2 tile = new Vector2(x, y);

                        if (!loc.terrainFeatures.ContainsKey(tile) && !loc.objects.ContainsKey(tile))
                        {
                            var t = backLayer.Tiles[x, y];
                            if (t != null && Array.IndexOf(validTiles, t.TileIndex) >= 0)
                            {
                                string treeType = Game1.random.Next(1, 4).ToString();
                                loc.terrainFeatures.Add(tile, new Tree(treeType, 0));
                                arbolesPlantados++;
                            }
                        }
                    }
                }
            }
        }

        private void OnPlayerWarped(object? sender, WarpedEventArgs e)
        {
            if (Game1.player == null) return;

            if (e.NewLocation.Name != BuildingId && e.NewLocation.Name != "Farm")
            {
                isWarpingToExpansion = false;
            }

            if (e.NewLocation is GameLocation customLoc && customLoc.Name == BuildingId)
            {
                ConfigurarExpansion(customLoc);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.currentLocation == null) return;

            if (Game1.currentLocation is Farm farmLoc && Game1.player.modData.ContainsKey(ModDataKeyComprado))
            {
                if (Game1.player.modData.TryGetValue(ModDataKeyX, out string? xStr) && Game1.player.modData.TryGetValue(ModDataKeyY, out string? yStr))
                {
                    if (int.TryParse(xStr, out int startX) && int.TryParse(yStr, out int startY))
                    {
                        for (int x = startX; x < startX + 3; x++)
                        {
                            for (int y = startY; y < startY + 3; y++)
                            {
                                Vector2 tile = new Vector2(x, y);
                                if (!farmLoc.terrainFeatures.TryGetValue(tile, out var tf) || !(tf is Flooring))
                                {
                                    farmLoc.terrainFeatures[tile] = new Flooring("6");
                                }
                                if (farmLoc.objects.ContainsKey(tile))
                                {
                                    farmLoc.objects.Remove(tile);
                                }
                            }
                        }

                        Vector2 pTile = Game1.player.Tile;
                        if (pTile.X >= startX && pTile.X < startX + 3 && pTile.Y >= startY && pTile.Y < startY + 3)
                        {
                            if (!isWarpingToExpansion)
                            {
                                isWarpingToExpansion = true;
                                Game1.warpFarmer(BuildingId, 19, 4, false);
                            }
                        }
                        else
                        {
                            isWarpingToExpansion = false;
                        }
                    }
                }
            }

            if (Game1.currentLocation.Name == BuildingId)
            {
                ConfigurarExpansion(Game1.currentLocation);
                ActualizarSalidaExpansion();
            }

            bool inCarpenter = Game1.activeClickableMenu is CarpenterMenu;
            if (inCarpenter && !wasInCarpenterMenu)
            {
                wasInCarpenterMenu = true;
                if (Game1.player.modData.TryGetValue(ModDataKeyComprado, out string? comprado) && comprado == "true")
                {
                    if (Game1.player.modData.TryGetValue(ModDataKeyX, out string? xS) && Game1.player.modData.TryGetValue(ModDataKeyY, out string? yS))
                    {
                        if (int.TryParse(xS, out int sX) && int.TryParse(yS, out int sY))
                        {
                            var fakeBuilding = new Building(BuildingId, new Vector2(sX, sY));
                            fakeBuilding.modData["FalsoParaRobin"] = "true";
                            Game1.getFarm().buildings.Add(fakeBuilding);
                        }
                    }
                }
            }
            else if (!inCarpenter && wasInCarpenterMenu)
            {
                wasInCarpenterMenu = false;
                var farm = Game1.getFarm();
                for (int i = farm.buildings.Count - 1; i >= 0; i--)
                {
                    var b = farm.buildings[i];
                    if (b.buildingType.Value == BuildingId && b.modData.ContainsKey("FalsoParaRobin"))
                    {
                        b.modData["BorradoPorElMod"] = "true";
                        farm.destroyStructure(b);
                    }
                }
            }

            if (Game1.activeClickableMenu == null && Game1.player != null && Game1.player.UsingTool && Game1.player.CurrentTool != null)
            {
                var tool = Game1.player.CurrentTool;
                Vector2 grabTile = Game1.player.GetGrabTile();

                if (tool.Name.Contains("Axe", StringComparison.OrdinalIgnoreCase) || tool.DisplayName.Contains("Hacha", StringComparison.OrdinalIgnoreCase))
                {
                    ProcesarGolpeHacha(grabTile, tool);
                }
                else if (tool.Name.Contains("Pickaxe", StringComparison.OrdinalIgnoreCase) || tool.DisplayName.Contains("Pico", StringComparison.OrdinalIgnoreCase))
                {
                    ProcesarGolpePico(grabTile, tool);
                }
            }
        }

        private void ConfigurarExpansion(GameLocation location)
        {
            location.IsFarm = true;
            location.IsOutdoors = true;

            if (location.map != null)
            {
                location.map.Properties["Farmable"] = "T";
                var backLayer = location.map.GetLayer("Back");

                if (backLayer != null)
                {
                    int[] validTiles = { 227, 226, 181, 207, 231 };

                    for (int x = 0; x < backLayer.LayerWidth; x++)
                    {
                        for (int y = 0; y < backLayer.LayerHeight; y++)
                        {
                            var tile = backLayer.Tiles[x, y];
                            if (tile != null)
                            {
                                if (Array.IndexOf(validTiles, tile.TileIndex) >= 0)
                                {
                                    tile.Properties["Diggable"] = "T";
                                }
                                else
                                {
                                    tile.Properties.Remove("Diggable");
                                    tile.Properties["Diggable"] = "F";
                                    if (tile.Properties.ContainsKey("Type") && tile.Properties["Type"].ToString() == "Dirt")
                                        tile.Properties.Remove("Type");
                                }

                                bool isWater = false;
                                if (x >= 45 && x <= 49 && y >= 5 && y <= 15) isWater = true;
                                if (x >= 15 && x <= 23 && y >= 26 && y <= 30) isWater = true;
                                if (x >= 70 && x <= 74 && y >= 4 && y <= 15) isWater = true;

                                if (isWater)
                                {
                                    tile.Properties["Water"] = "T";
                                }
                                else if (tile.Properties.ContainsKey("Water"))
                                {
                                    tile.Properties.Remove("Water");
                                }
                            }
                        }
                    }
                }
            }

            foreach (var zona in zonasDeExpansion)
            {
                if (Game1.player.modData.ContainsKey(zona.Key))
                {
                    AplicarDesbloqueo(location, zona);
                }
            }

            // INYECCIÓN DE PESCA INTERNA: Fuerza al motor a habilitar la pesca y generar los cuadros de agua virtuales
            if (location.waterTiles == null || location.waterTiles.Length == 0)
            {
                location.updateWaterTiles();
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            var loc = Game1.getLocationFromName(BuildingId);
            if (loc != null)
            {
                loc.spawnWeedsAndStones(15, false, false);
                var backLayer = loc.map?.GetLayer("Back");
                if (backLayer != null)
                {
                    int[] validTiles = { 227, 226, 181, 207, 231 };
                    int arbolesPlantados = 0;

                    for (int i = 0; i < 30 && arbolesPlantados < 4; i++)
                    {
                        int x = Game1.random.Next(backLayer.LayerWidth);
                        int y = Game1.random.Next(backLayer.LayerHeight);
                        Vector2 tile = new Vector2(x, y);

                        if (!loc.terrainFeatures.ContainsKey(tile) && !loc.objects.ContainsKey(tile))
                        {
                            var t = backLayer.Tiles[x, y];
                            if (t != null && Array.IndexOf(validTiles, t.TileIndex) >= 0)
                            {
                                string treeType = Game1.random.Next(1, 4).ToString();
                                loc.terrainFeatures.Add(tile, new Tree(treeType, 0));
                                arbolesPlantados++;
                            }
                        }
                    }
                }
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null || Game1.currentLocation == null) return;
            if (Game1.activeClickableMenu != null) return;

            Vector2 grabTile = Game1.player.GetGrabTile();
            Vector2 cursorTile = e.Cursor.Tile;

            if (e.Button.IsUseToolButton() || e.Button.IsActionButton() || e.Button == SButton.MouseLeft || e.Button == SButton.C)
            {
                if (Game1.currentLocation is Farm && Game1.player.modData.ContainsKey(ModDataKeyComprado))
                {
                    if (Game1.player.modData.TryGetValue(ModDataKeyX, out string? xStr) && Game1.player.modData.TryGetValue(ModDataKeyY, out string? yStr))
                    {
                        if (int.TryParse(xStr, out int startX) && int.TryParse(yStr, out int startY))
                        {
                            if ((grabTile.X >= startX && grabTile.X < startX + 3 && grabTile.Y >= startY && grabTile.Y < startY + 3) ||
                                (cursorTile.X >= startX && cursorTile.X < startX + 3 && cursorTile.Y >= startY && cursorTile.Y < startY + 3))
                            {
                                this.Helper.Input.Suppress(e.Button);
                                return;
                            }
                        }
                    }
                }
            }

            if (e.Button == SButton.MouseLeft || e.Button.IsActionButton() || e.Button == SButton.C)
            {
                if (Game1.currentLocation.Name == BuildingId)
                {
                    ExpansionZone? targetZone = null;
                    foreach (var z in zonasDeExpansion)
                    {
                        if (((grabTile.X == z.ClickX1 || grabTile.X == z.ClickX2) && grabTile.Y == z.ClickY) ||
                            ((cursorTile.X == z.ClickX1 || cursorTile.X == z.ClickX2) && cursorTile.Y == z.ClickY))
                        {
                            targetZone = z;
                            break;
                        }
                    }

                    if (targetZone != null && !Game1.player.modData.ContainsKey(targetZone.Key))
                    {
                        Game1.activeClickableMenu = new ExpansionPurchaseMenu(targetZone.CostGold, targetZone.CostDiamonds, () => ConfirmarCompra(targetZone));
                        this.Helper.Input.Suppress(e.Button);
                        return;
                    }
                }

                var tool = Game1.player.CurrentTool;
                if (tool != null)
                {
                    if (Game1.currentLocation.Name == BuildingId && (tool.Name.Contains("Hoe", StringComparison.OrdinalIgnoreCase) || tool.DisplayName.Contains("Azada", StringComparison.OrdinalIgnoreCase)))
                    {
                        var backLayer = Game1.currentLocation.map?.GetLayer("Back");
                        if (backLayer != null && grabTile.X >= 0 && grabTile.X < backLayer.LayerWidth && grabTile.Y >= 0 && grabTile.Y < backLayer.LayerHeight)
                        {
                            var tile = backLayer.Tiles[(int)grabTile.X, (int)grabTile.Y];
                            int[] validTiles = { 227, 226, 181, 207, 231 };

                            if (tile == null || Array.IndexOf(validTiles, tile.TileIndex) < 0)
                            {
                                this.Helper.Input.Suppress(e.Button);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void ConfirmarCompra(ExpansionZone zone)
        {
            Game1.player.Money -= zone.CostGold;
            ConsumirDiamantes(zone.CostDiamonds);
            Game1.player.modData[zone.Key] = "true";
            Game1.playSound("purchase");

            var loc = Game1.getLocationFromName(BuildingId);
            if (loc != null) AplicarDesbloqueo(loc, zone);

            Game1.addHUDMessage(new HUDMessage("¡Nueva zona desbloqueada con éxito!", 2));
        }

        public static int ContarDiamantes()
        {
            int count = 0;
            foreach (var item in Game1.player.Items)
            {
                if (item != null && (item.ItemId == "(O)72" || item.ItemId == "72" || item.ParentSheetIndex == 72))
                    count += item.Stack;
            }
            return count;
        }

        private void ConsumirDiamantes(int cantidad)
        {
            int remaining = cantidad;
            for (int i = 0; i < Game1.player.Items.Count; i++)
            {
                var item = Game1.player.Items[i];
                if (item != null && (item.ItemId == "(O)72" || item.ItemId == "72" || item.ParentSheetIndex == 72))
                {
                    if (item.Stack <= remaining)
                    {
                        remaining -= item.Stack;
                        Game1.player.Items[i] = null;
                    }
                    else
                    {
                        item.Stack -= remaining;
                        remaining = 0;
                    }
                    if (remaining <= 0) break;
                }
            }
        }

        private void AplicarDesbloqueo(GameLocation location, ExpansionZone zona)
        {
            var map = location.map;
            if (map != null)
            {
                var buildingsLayer = map.GetLayer("Buildings");

                int minX = Math.Min(zona.ClearStartX, zona.ClearEndX);
                int maxX = Math.Max(zona.ClearStartX, zona.ClearEndX);
                int minY = Math.Min(zona.ClearStartY, zona.ClearEndY);
                int maxY = Math.Max(zona.ClearStartY, zona.ClearEndY);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        if (buildingsLayer != null && x >= 0 && x < buildingsLayer.LayerWidth && y >= 0 && y < buildingsLayer.LayerHeight)
                        {
                            buildingsLayer.Tiles[x, y] = null;
                        }

                        Vector2 tile = new Vector2(x, y);
                        if (location.objects.ContainsKey(tile)) location.objects.Remove(tile);

                        Game1.currentLocation.removeTemporarySpritesWithID((int)(x * 64 + y));
                    }
                }

                if (zona.Id == 8 && buildingsLayer != null)
                {
                    for (int x = 38; x <= 39; x++)
                    {
                        for (int y = 9; y <= 12; y++)
                        {
                            if (x >= 0 && x < buildingsLayer.LayerWidth && y >= 0 && y < buildingsLayer.LayerHeight)
                            {
                                buildingsLayer.Tiles[x, y] = null;
                            }
                        }
                    }
                }
            }
        }

        private void ProcesarGolpePico(Vector2 grabTile, Tool tool)
        {
            var map = Game1.currentLocation.map;
            if (map != null)
            {
                var buildingsLayer = map.GetLayer("Buildings");
                int cx = (int)grabTile.X;
                int cy = (int)grabTile.Y;

                if (buildingsLayer != null && cx >= 0 && cx < buildingsLayer.LayerWidth && cy >= 0 && cy < buildingsLayer.LayerHeight)
                {
                    var tile = buildingsLayer.Tiles[cx, cy];
                    if (tile != null && tile.TileIndex == 232)
                    {
                        buildingsLayer.Tiles[cx, cy] = null;
                        var frontLayer = map.GetLayer("Front");
                        if (frontLayer != null && cx >= 0 && cx < frontLayer.LayerWidth && cy >= 0 && cy < frontLayer.LayerHeight)
                            frontLayer.Tiles[cx, cy] = null;

                        Game1.playSound("stoneCrack");
                        Game1.createObjectDebris("(O)390", cx, cy, 1);
                    }
                }
            }
        }

        private void ProcesarGolpeHacha(Vector2 grabTile, Tool tool)
        {
            Rectangle hitBox = new Rectangle((int)grabTile.X * 64, (int)grabTile.Y * 64, 64, 64);
            for (int i = Game1.currentLocation.largeTerrainFeatures.Count - 1; i >= 0; i--)
            {
                if (Game1.currentLocation.largeTerrainFeatures[i] is Bush bush && bush.getBoundingBox().Intersects(hitBox))
                {
                    string hitFrameKey = "GranjaExpandible_LastHitFrame";
                    long currentTick = Game1.ticks;
                    if (bush.modData.TryGetValue(hitFrameKey, out string? tickStr) && long.TryParse(tickStr, out long lastTick) && currentTick - lastTick < 15) return;
                    bush.modData[hitFrameKey] = currentTick.ToString();

                    int axeLevel = tool.UpgradeLevel;
                    int hitsRequired = Math.Max(1, 5 - axeLevel);

                    string hitsKey = "GranjaExpandible_BushHits";
                    int currentHits = 0;
                    if (bush.modData.TryGetValue(hitsKey, out string? hitsStr) && hitsStr != null) int.TryParse(hitsStr, out currentHits);

                    currentHits++;
                    if (currentHits >= hitsRequired)
                    {
                        Game1.currentLocation.largeTerrainFeatures.RemoveAt(i);
                        Game1.playSound("treethud");
                        Game1.createMultipleObjectDebris("(O)388", (int)grabTile.X, (int)grabTile.Y, Game1.random.Next(3, 8), Game1.player.UniqueMultiplayerID);
                    }
                    else
                    {
                        bush.modData[hitsKey] = currentHits.ToString();
                        Game1.playSound("axchop");
                        bush.shake(grabTile, true);
                    }
                    break;
                }
            }
        }

        private void ActualizarSalidaExpansion()
        {
            var expansionLoc = Game1.getLocationFromName(BuildingId);
            if (expansionLoc != null && expansionLoc.map != null && Game1.player.modData.TryGetValue(ModDataKeyX, out string? xStr) && Game1.player.modData.TryGetValue(ModDataKeyY, out string? yStr))
            {
                if (int.TryParse(xStr, out int startX) && int.TryParse(yStr, out int startY))
                {
                    var backLayer = expansionLoc.map.GetLayer("Back");
                    if (backLayer != null)
                    {
                        int returnX = startX + 1;
                        int returnY = startY + 3;
                        string warpString = $"Warp Farm {returnX} {returnY}";
                        var tile1 = backLayer.Tiles[19, 3];
                        if (tile1 != null) tile1.Properties["TouchAction"] = warpString;
                        var tile2 = backLayer.Tiles[20, 3];
                        if (tile2 != null) tile2.Properties["TouchAction"] = warpString;
                    }
                }
            }
        }
    }

    public class ExpansionPurchaseMenu : IClickableMenu
    {
        private ClickableTextureComponent okButton;
        private ClickableTextureComponent cancelButton;
        private Action? onConfirm;
        private int priceGold;
        private int priceDiamonds;

        public ExpansionPurchaseMenu(int priceGold, int priceDiamonds, Action? onConfirm)
            : base(Game1.uiViewport.Width / 2 - 400, Game1.uiViewport.Height / 2 - 175, 800, 350, true)
        {
            this.priceGold = priceGold;
            this.priceDiamonds = priceDiamonds;
            this.onConfirm = onConfirm;

            this.okButton = new ClickableTextureComponent("OK", new Rectangle(this.xPositionOnScreen + this.width - 128 - 20, this.yPositionOnScreen + this.height - 64 - 20, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1), 1f, false);
            this.cancelButton = new ClickableTextureComponent("Cancel", new Rectangle(this.xPositionOnScreen + this.width - 64 - 20, this.yPositionOnScreen + this.height - 64 - 20, 64, 64), null, null, Game1.mouseCursors, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47, -1, -1), 1f, false);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (this.okButton.containsPoint(x, y))
            {
                if (Game1.player.Money >= priceGold && ModEntry.ContarDiamantes() >= priceDiamonds)
                {
                    this.onConfirm?.Invoke();
                    Game1.exitActiveMenu();
                }
                else
                {
                    Game1.playSound("cancel");
                }
            }
            else if (this.cancelButton.containsPoint(x, y))
            {
                Game1.playSound("bigDeSelect");
                Game1.exitActiveMenu();
            }
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true, null, false, true);

            string title = "¿Desbloquear nueva área?";
            Vector2 tSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont, new Vector2(this.xPositionOnScreen + this.width / 2 - tSize.X / 2, this.yPositionOnScreen + 80), Game1.textColor);

            int currentGold = Game1.player.Money;
            int currentDiamonds = ModEntry.ContarDiamantes();

            Color goldColor = currentGold >= priceGold ? Game1.textColor : Color.Red;
            Color diamondColor = currentDiamonds >= priceDiamonds ? Game1.textColor : Color.Red;

            int startY = this.yPositionOnScreen + 160;
            int startX = this.xPositionOnScreen + 80;

            string goldText = $"{priceGold}g  (Tienes: {currentGold}g)";
            string diaText = $"{priceDiamonds} Diamantes  (Tienes: {currentDiamonds})";

            b.Draw(Game1.mouseCursors, new Vector2(startX, startY), new Rectangle(280, 411, 16, 16), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0.89f);
            Utility.drawTextWithShadow(b, goldText, Game1.dialogueFont, new Vector2(startX + 60, startY - 4), goldColor);

            var diamondData = ItemRegistry.GetDataOrErrorItem("(O)72");
            b.Draw(diamondData.GetTexture()!, new Vector2(startX, startY + 60), diamondData.GetSourceRect(), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0.89f);
            Utility.drawTextWithShadow(b, diaText, Game1.dialogueFont, new Vector2(startX + 60, startY + 60 - 4), diamondColor);

            this.okButton.draw(b);
            this.cancelButton.draw(b);
            this.drawMouse(b);
        }
    }
}