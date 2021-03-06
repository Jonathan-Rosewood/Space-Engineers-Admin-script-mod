﻿namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Messages;
    using Sandbox.Definitions;
    using Sandbox.Game.Entities;
    using Sandbox.Game.Entities.Cube;
    using Sandbox.ModAPI;
    using VRage.Game;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    public class CommandIdentify : ChatCommand
    {
        public CommandIdentify()
            : base(ChatCommandSecurity.Admin, "id", new[] { "/id" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.ShowMessage("/id", "Identifies the name of the object the player is looking at.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (messageText.Equals("/id", StringComparison.InvariantCultureIgnoreCase))
            {
                IMyEntity entity;
                double distance;
                Vector3D hitPoint;
                Support.FindLookAtEntity(MyAPIGateway.Session.ControlledObject, true, false, out entity, out distance, out hitPoint, true, true, true, true, true, true);
                if (entity != null)
                {
                    MessageTaggedEntityStore.RegisterIdentity(playerId, entity.EntityId);
                    string displayType;
                    string displayName;
                    StringBuilder description = new StringBuilder();
                    if (entity is IMyVoxelMap)
                    {
                        var voxelMap = (IMyVoxelMap)entity;
                        displayType = "asteroid";
                        displayName = voxelMap.StorageName;
                        var aabb = new BoundingBoxD(voxelMap.PositionLeftBottomCorner, voxelMap.PositionLeftBottomCorner + voxelMap.Storage.Size);
                        description.AppendFormat("Distance: {0:N} m\r\nSize: {1}\r\nBoundingBox Center: [X:{2:N} Y:{3:N} Z:{4:N}]\r\n\r\nUse /detail for more information on asteroid content.",
                            distance, voxelMap.Storage.Size,
                            aabb.Center.X, aabb.Center.Y, aabb.Center.Z);

                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description.ToString(), null, "OK");
                    }
                    else if (entity is Sandbox.Game.Entities.MyPlanet)
                    {
                        var planet = (Sandbox.Game.Entities.MyPlanet)entity;
                        displayType = "planet";
                        displayName = planet.StorageName;
                        description.AppendFormat("Distance: {0:N} m\r\nCenter: [X:{1:N} Y:{2:N} Z:{3:N}]\r\nMinimum Radius: {4:N} m\r\nMaximum Radius: {5:N} m\r\nAverage Radius: {6:N} m\r\nAtmosphere Radius: {7:N} m\r\nHas Atmosphere: {8}\r\nBreathable Atmosphere: {9}",
                            distance,
                            planet.WorldMatrix.Translation.X, planet.WorldMatrix.Translation.Y, planet.WorldMatrix.Translation.Z,
                            planet.MinimumRadius,
                            planet.MaximumRadius,
                            planet.AverageRadius,
                            planet.AtmosphereRadius,
                            planet.HasAtmosphere,
                            planet.Generator.Atmosphere.Breathable);

                        MySphericalNaturalGravityComponent naturalGravity = planet.Components.Get<MyGravityProviderComponent>() as MySphericalNaturalGravityComponent;
                        if (naturalGravity != null)
                        {
                            description.AppendLine();
                            description.AppendLine("Gravity Limit: {0:N} m",
                            naturalGravity.GravityLimit);
                        }

                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description.ToString(), null, "OK");
                    }
                    else if (entity is IMyCubeBlock || entity is IMyCubeGrid)
                    {
                        IMyCubeGrid gridCube;
                        IMyCubeBlock cubeBlock = null;

                        if (entity is IMyCubeGrid)
                            gridCube = (IMyCubeGrid)entity;
                        else
                        {
                            cubeBlock = (IMyCubeBlock)entity;
                            gridCube = (IMyCubeGrid)cubeBlock.GetTopMostParent();
                        }

                        var attachedGrids = gridCube.GetAttachedGrids(AttachedGrids.Static);
                        var blocks = new List<IMySlimBlock>();
                        gridCube.GetBlocks(blocks);
                        //var cockpits = entity.FindWorkingCockpits(); // TODO: determine if any cockpits are occupied.


                        var identities = new List<IMyIdentity>();
                        MyAPIGateway.Players.GetAllIdentites(identities);
                        var ownerCounts = new Dictionary<long, long>();

                        foreach (var block in blocks.Where(f => f.FatBlock != null && f.FatBlock.OwnerId != 0))
                        {
                            if (ownerCounts.ContainsKey(block.FatBlock.OwnerId))
                                ownerCounts[block.FatBlock.OwnerId]++;
                            else
                                ownerCounts.Add(block.FatBlock.OwnerId, 1);
                        }

                        var ownerList = new List<string>();
                        foreach (var ownerKvp in ownerCounts)
                        {
                            var owner = identities.FirstOrDefault(p => p.IdentityId == ownerKvp.Key);
                            if (owner == null)
                                continue;
                            ownerList.Add(string.Format("{0} [{1}]", owner.DisplayName, ownerKvp.Value));
                        }

                        // TODO: BuiltBy needs to be made available in IMySlimBlock.
                        //var builtByCounts = new Dictionary<long, long>();
                        //foreach (var block in blocks.Where(f => f.FatBlock != null && ((MyCubeBlock)f).BuiltBy != 0))
                        //{
                        //    if (ownerCounts.ContainsKey(block.FatBlock.OwnerId))
                        //        ownerCounts[block.FatBlock.OwnerId]++;
                        //    else
                        //        ownerCounts.Add(block.FatBlock.OwnerId, 1);
                        //}


                        //var damage = new StringBuilder();
                        //var buildComplete = new StringBuilder();
                        var incompleteBlocks = 0;

                        foreach (var block in blocks)
                        {
                            //damage.    cube.IntegrityPercent <= cube.BuildPercent;
                            //complete.    cube.BuildPercent;

                            // This information does not appear to work.
                            // Unsure if the API is broken, incomplete , or a temporary bug under 01.070.
                            //damage.AppendFormat("D={0:N} ", block.DamageRatio);  
                            //damage.AppendFormat("A={0:N} ", block.AccumulatedDamage);

                            if (!block.IsFullIntegrity)
                            {
                                incompleteBlocks++;
                                //buildComplete.AppendFormat("B={0:N} ", block.BuildLevelRatio);
                                //buildComplete.AppendFormat("I={0:N} ", block.BuildIntegrity);
                                //buildComplete.AppendFormat("M={0:N} ", block.MaxIntegrity);
                            }
                        }

                        displayType = gridCube.IsStatic ? "Station" : gridCube.GridSizeEnum.ToString() + " Ship";
                        displayName = gridCube.DisplayName;

                        description.AppendFormat("Distance: {0:N} m\r\n",
                            distance);

                        if (gridCube.Physics == null)
                            description.AppendFormat("Projection has no physics characteristics.\r\n");
                        else
                            description.AppendFormat("Mass: {0:N} kg\r\nVector: {1}\r\nVelocity: {2:N} m/s\r\nMass Center: {3}\r\n",
                                gridCube.Physics.Mass,
                                gridCube.Physics.LinearVelocity,
                                gridCube.Physics.LinearVelocity.Length(),
                                gridCube.Physics.CenterOfMassWorld);

                        description.AppendFormat("Size : {0}\r\nNumber of Blocks : {1:#,##0}\r\nAttached Grids : {2:#,##0} (including this one).\r\nOwners : {3}\r\nBuild : {4} blocks incomplete.",
                            gridCube.LocalAABB.Size,
                            blocks.Count,
                            attachedGrids.Count,
                            string.Join(", ", ownerList),
                            incompleteBlocks);

                        if (cubeBlock != null)
                        {
                            string ownerName = "";
                            var owner = identities.FirstOrDefault(p => p.IdentityId == cubeBlock.OwnerId);
                            if (owner != null)
                                ownerName = owner.DisplayName;

                            string builtByName = "";
                            var builtBy = identities.FirstOrDefault(p => p.IdentityId == ((MyCubeBlock)cubeBlock).BuiltBy);
                            if (builtBy != null)
                                builtByName = builtBy.DisplayName;
                            description.AppendFormat("\r\n\r\nCube;\r\n  Type : {1}\r\n  SubType : {0}\r\n  Name : {2}\r\n  Owner : {3}\r\n  BuiltBy : {4}", cubeBlock.BlockDefinition.SubtypeName, cubeBlock.DefinitionDisplayNameText, cubeBlock.DisplayNameText, ownerName, builtByName);
                        }

                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description.ToString(), null, "OK");
                    }
                    else if (entity is IMyCharacter)
                    {
                        displayType = "player";
                        displayName = entity.DisplayName;
                        description.AppendFormat("Distance: {0:N} m", distance);
                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description.ToString(), null, "OK");
                    }
                    else if (entity is MyInventoryBagEntity)
                    {
                        displayType = "Unknown";

                        var replicable = (MyInventoryBagEntity)entity;
                        if (replicable.DefinitionId.HasValue)
                        {
                            MyDefinitionBase definition;
                            if (MyDefinitionManager.Static.TryGetDefinition(replicable.DefinitionId.Value, out definition))
                                displayType = definition.Id.SubtypeName;
                        }

                        displayName = entity.DisplayName;
                        description.AppendFormat("Distance: {0:N} m", distance);
                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description.ToString(), null, "OK");
                    }
                    else
                    {
                        displayType = "unknown";
                        displayName = entity.DisplayName;
                        description.AppendFormat("Distance: {0:N} m", distance);
                        MyAPIGateway.Utilities.ShowMissionScreen(string.Format("ID {0}:", displayType), string.Format("'{0}'", displayName), " ", description.ToString(), null, "OK");
                    }

                    return true;
                }

                MyAPIGateway.Utilities.ShowMessage("ID", "Could not find object.");
                return true;
            }

            return false;
        }
    }
}
