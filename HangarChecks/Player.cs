﻿using NLog;
using QuantumHangar.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace QuantumHangar.HangarChecks
{

    //This is when a normal player runs hangar commands
    public class Player
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Chat Chat;
        private readonly ulong SteamID;
        private readonly long IdentityID;
        private readonly Vector3D PlayerPosition;
        private readonly MyCharacter UserCharacter;

        private Vector3D SpawnPosition = Vector3D.Zero;
        private bool LoadingAtSave = true;
        private PlayerHangar PlayersHanger;

        public static Settings Config { get { return Hangar.Config; } }
        private bool LoadFromSavePosition;



        public Player(CommandContext Context)
        {
            Chat = new Chat(Context);
            SteamID = Context.Player.SteamUserId;
            IdentityID = Context.Player.Identity.IdentityId;
            PlayerPosition = Context.Player.GetPosition();
            UserCharacter = (MyCharacter)Context.Player.Character;
        }

        private bool PerformMainChecks(bool IsSaving)
        {
            if (!Config.PluginEnabled)
                return false;


            if (PlayerHangar.IsServerSaving(Chat))
                return false;

            if (!CheckZoneRestrictions(IsSaving))
                return false;

            if (!CheckGravity())
                return false;


            PlayersHanger = new PlayerHangar(SteamID, Chat);


            if (!PlayersHanger.CheckPlayerTimeStamp())
                return false;

            if (CheckEnemyDistance(LoadingAtSave, SpawnPosition))
                return false;





            return true;
        }

        public void SaveGrid()
        {
            if (!PerformMainChecks(true))
                return;

            if (!PlayersHanger.CheckHanagarLimits())
                return;

            GridResult Result = new GridResult();

            //Gets grids player is looking at
            if (!Result.GetGrids(Chat, UserCharacter))
                return;

            //Calculates incoming grids data
            GridStamp GridData = Result.GenerateGridStamp();

            //Checks for single and all slot block and grid limits
            if (!PlayersHanger.ExtensiveLimitChecker(GridData))
                return;


            //Check For Price

            /*
            if (!RequireSaveCurrency(result))
                return;
            */

            GridUtilities.FormatGridName(PlayersHanger, GridData);

            GridUtilities GridUtils = new GridUtilities(Chat, SteamID);

            if (GridUtils.SaveGrids(Result, GridData))
            {
                PlayersHanger.SaveGridStamp(GridData, IdentityID);
                Chat.Respond("Save Complete!");
            }
            else
            {
                Chat.Respond("Saved Failed!");
                return;
            }

        }

       



        private bool CheckZoneRestrictions(bool IsSave)
        {
            if (Config.ZoneRestrictions.Count != 0)
            {
                //Get save point
                int ClosestPoint = -1;
                double Distance = -1;

                for (int i = 0; i < Config.ZoneRestrictions.Count(); i++)
                {

                    Vector3D ZoneCenter = new Vector3D(Config.ZoneRestrictions[i].X, Config.ZoneRestrictions[i].Y, Config.ZoneRestrictions[i].Z);

                    double PlayerDistance = Vector3D.Distance(ZoneCenter, PlayerPosition);

                    if (PlayerDistance <= Config.ZoneRestrictions[i].Radius)
                    {
                        //if player is within range

                        if (IsSave && !Config.ZoneRestrictions[i].AllowSaving)
                        {
                            Chat.Respond("You are not permitted to save grids in this zone");
                            return false;
                        }

                        if (!IsSave && !Config.ZoneRestrictions[i].AllowLoading)
                        {
                            Chat.Respond("You are not permitted to load grids in this zone");
                            return false;
                        }
                        return true;
                    }



                    if (IsSave && Config.ZoneRestrictions[i].AllowSaving)
                    {
                        if (ClosestPoint == -1 || PlayerDistance <= Distance)
                        {
                            ClosestPoint = i;
                            Distance = PlayerDistance;
                        }
                    }


                    if (!IsSave && Config.ZoneRestrictions[i].AllowLoading)
                    {
                        if (ClosestPoint == -1 || PlayerDistance <= Distance)
                        {
                            ClosestPoint = i;
                            Distance = PlayerDistance;
                        }
                    }



                }
                Vector3D ClosestZone = new Vector3D();
                try
                {
                    ClosestZone = new Vector3D(Config.ZoneRestrictions[ClosestPoint].X, Config.ZoneRestrictions[ClosestPoint].Y, Config.ZoneRestrictions[ClosestPoint].Z);
                }
                catch (Exception e)
                {

                    Chat.Respond("No areas found!");
                    //Log.Warn(e, "No suitable zones found! (Possible Error)");
                    return false;
                }




                if (IsSave)
                {
                    CharacterUtilities.SendGps(ClosestZone, Config.ZoneRestrictions[ClosestPoint].Name + " (within " + Config.ZoneRestrictions[ClosestPoint].Radius + "m)", IdentityID);
                    Chat.Respond("Nearest save area has been added to your HUD");
                    return false;
                }
                else
                {
                    CharacterUtilities.SendGps(ClosestZone, Config.ZoneRestrictions[ClosestPoint].Name + " (within " + Config.ZoneRestrictions[ClosestPoint].Radius + "m)", IdentityID);
                    //Chat chat = new Chat(Context);
                    Chat.Respond("Nearest load area has been added to your HUD");
                    return false;
                }
            }
            return true;
        }

        private bool CheckGravity()
        {
            if (!Config.AllowInGravity)
            {
                if (!Vector3D.IsZero(MyGravityProviderSystem.CalculateNaturalGravityInPoint(PlayerPosition)))
                {
                    Chat.Respond("Saving & Loading in gravity has been disabled!");
                    return false;
                }
            }
            else
            {
                if (Config.MaxGravityAmount == 0)
                {
                    return true;
                }

                float Gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(PlayerPosition).Length() / 9.81f;
                if (Gravity > Config.MaxGravityAmount)
                {
                    //Log.Warn("Players gravity amount: " + Gravity);
                    Chat.Respond("You are not permitted to Save/load in this gravity amount. Max amount: " + Config.MaxGravityAmount + "g");
                    return false;
                }
            }

            return true;
        }

        private bool CheckEnemyDistance(bool LoadingAtSavePoint = false, Vector3D Position = new Vector3D())
        {
            if (!LoadingAtSavePoint)
            {
                Position = PlayerPosition;
            }

            MyFaction PlayersFaction = MySession.Static.Factions.GetPlayerFaction(IdentityID);
            bool EnemyFoundFlag = false;
            if (Config.DistanceCheck > 0)
            {
                //Check enemy location! If under limit return!
                foreach (MyPlayer OnlinePlayer in MySession.Static.Players.GetOnlinePlayers())
                {
                    if (OnlinePlayer.Identity.IdentityId == IdentityID || MySession.Static.IsUserAdmin(OnlinePlayer.Id.SteamId))
                        continue;


                    MyFaction TargetPlayerFaction = MySession.Static.Factions.GetPlayerFaction(OnlinePlayer.Identity.IdentityId);
                    if (PlayersFaction != null && TargetPlayerFaction != null)
                    {
                        if (PlayersFaction.FactionId == TargetPlayerFaction.FactionId)
                            continue;

                        //Neutrals count as allies not friends for some reason
                        MyRelationsBetweenFactions Relation = MySession.Static.Factions.GetRelationBetweenFactions(PlayersFaction.FactionId, TargetPlayerFaction.FactionId).Item1;
                        if (Relation == MyRelationsBetweenFactions.Neutral || Relation == MyRelationsBetweenFactions.Friends)
                            continue;
                    }

                    if (Vector3D.Distance(Position, OnlinePlayer.GetPosition()) == 0)
                    {
                        continue;
                    }

                    if (Vector3D.Distance(Position, OnlinePlayer.GetPosition()) <= Config.DistanceCheck)
                    {
                        Chat.Respond("Unable to load grid! Enemy within " + Config.DistanceCheck + "m!");
                        EnemyFoundFlag = true;
                    }
                }
            }


            if (Config.GridDistanceCheck > 0 && Config.GridCheckMinBlock > 0 && EnemyFoundFlag == false)
            {
                BoundingSphereD SpawnSphere = new BoundingSphereD(Position, Config.GridDistanceCheck);

                List<MyEntity> entities = new List<MyEntity>();
                MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref SpawnSphere, entities);



                //This is looping through all grids in the specified range. If we find an enemy, we need to break and return/deny spawning
                foreach (MyCubeGrid Grid in entities.OfType<MyCubeGrid>())
                {
                    if (Grid.BigOwners.Count <= 0 || Grid.CubeBlocks.Count < Config.GridCheckMinBlock)
                        continue;

                    if (Grid.BigOwners.Contains(IdentityID))
                        continue;



                    //if the player isnt big owner, we need to scan for faction mates
                    bool FoundAlly = true;
                    foreach (long Owner in Grid.BigOwners)
                    {
                        MyFaction TargetPlayerFaction = MySession.Static.Factions.GetPlayerFaction(Owner);
                        if (PlayersFaction != null && TargetPlayerFaction != null)
                        {
                            if (PlayersFaction.FactionId == TargetPlayerFaction.FactionId)
                                continue;

                            MyRelationsBetweenFactions Relation = MySession.Static.Factions.GetRelationBetweenFactions(PlayersFaction.FactionId, TargetPlayerFaction.FactionId).Item1;
                            if (Relation == MyRelationsBetweenFactions.Enemies)
                            {
                                FoundAlly = false;
                                break;
                            }
                        }
                        else
                        {
                            FoundAlly = false;
                            break;
                        }
                    }


                    if (!FoundAlly)
                    {
                        //Stop loop
                        Chat.Respond("Unable to load grid! Enemy within " + Config.GridDistanceCheck + "m!");
                        EnemyFoundFlag = true;
                        break;
                    }
                }
            }
            return EnemyFoundFlag;

        }



    }
}
