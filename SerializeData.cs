﻿using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace QuantumHangar
{
    public enum CostType
    {
        BlockCount,
        PerGrid,
        Fixed
    };


    public enum MessageType
    {
        RequestAllItems,
        AddOne,
        RemoveOne,

        SendDefinition,
        PurchasedGrid,
    }

    [ProtoContract]
    public class Message
    {
        [ProtoMember(1)] public GridsForSale GridDefinitions = new GridsForSale();
        [ProtoMember(2)] public List<MarketList> MarketBoxItmes = new List<MarketList>();
        [ProtoMember(3)] public MessageType Type;
    }

    [ProtoContract]
    public class GridsForSale
    {
        //Items we send to block on request to preview the grid
        [ProtoMember(1)] public string name;
        [ProtoMember(2)] public byte[] GridDefinition;
        [ProtoMember(3)] public ulong SellerSteamid;
        [ProtoMember(4)] public ulong BuyerSteamid;
    }

    [ProtoContract]
    public class MarketList
    {
        //Items we will send to the block on load (Less lag)

        [ProtoMember(1)] public string Name;
        [ProtoMember(2)] public string Description;
        [ProtoMember(3)] public string Seller = "Sold by Server";
        [ProtoMember(4)] public long Price;
        [ProtoMember(5)] public double MarketValue;
        [ProtoMember(6)] public ulong Steamid;


        //New items
        [ProtoMember(7)] public string SellerFaction = "N/A";
        [ProtoMember(8)] public float GridMass = 0;
        [ProtoMember(9)] public int SmallGrids = 0;
        [ProtoMember(10)] public int LargeGrids = 0;
        [ProtoMember(11)] public int StaticGrids = 0;
        [ProtoMember(12)] public int NumberofBlocks = 0;
        [ProtoMember(13)] public float MaxPowerOutput = 0;
        [ProtoMember(14)] public float GridBuiltPercent = 0;
        [ProtoMember(15)] public long JumpDistance = 0;
        [ProtoMember(16)] public int NumberOfGrids = 0;
        [ProtoMember(17)] public int PCU = 0;


        //Server blocklimits Block
        [ProtoMember(18)] public Dictionary<string, int> BlockTypeCount = new Dictionary<string, int>();


        //Grid Stored Materials
        [ProtoMember(19)] public Dictionary<string, double> StoredMaterials = new Dictionary<string, double>();

        [ProtoMember(20)] public byte[] GridDefinition;

    }

    public class PlayerAccount
    {
        public string Name;
        public ulong SteamID;
        public long AccountBalance;
        public bool AccountAdjustment;

        public PlayerAccount()
        {
            Name = null;
            SteamID = 0;
            AccountBalance = 0;
        }


        public PlayerAccount(string Name, ulong SteamID, long AccountBalance, bool AccountAdjustment = false)
        {
            this.Name = Name;
            this.SteamID = SteamID;
            this.AccountBalance = AccountBalance;
            this.AccountAdjustment = AccountAdjustment;
        }


    }

    public class Accounts{

        public Dictionary<ulong, long> PlayerAccounts = new Dictionary<ulong, long>();
    }


    public class MarketData
    {
        public List<GridsForSale> GridDefinition = new List<GridsForSale>();
        public List<MarketList> List = new List<MarketList>();
    }

    public class CrossServerMessage
    {
        public CrossServer.MessageType Type;
        public List<GridsForSale> GridDefinition = new List<GridsForSale>();
        public List<MarketList> List = new List<MarketList>();
        public List<PlayerAccount> BalanceUpdate = new List<PlayerAccount>();
    }

    public class PublicOffers
    {
        public string Name { get; set; }
        public long Price { get; set; }
        public string Description { get; set; }
        public string Seller { get; set; }
        public string SellerFaction { get; set; }
        public int TotalAmount { get; set; }
        public bool Forsale { get; set; }
        public int NumberOfBuys { get; set; }
    }

    public class TimeStamp
    {
        public long PlayerID;
        public DateTime OldTime;
    }

    public class PlayerInfo
    {
        public List<GridStamp> Grids = new List<GridStamp>();
        public TimeStamp Timer;
    }

    public class GridStamp
    {
        public long GridID;
        public string GridName;
        public int GridPCU;
        public bool GridForSale = false;
        public double MarketValue = 0;




        public string SellerFaction = "N/A";
        public float GridMass = 0;
        public int StaticGrids = 0;
        public int SmallGrids = 0;
        public int LargeGrids = 0;
        public int NumberofBlocks = 0;
        public float MaxPowerOutput = 0;
        public float GridBuiltPercent = 0;
        public long JumpDistance = 0;
        public int NumberOfGrids = 0;


        //Server blocklimits Block
        public Dictionary<string, int> BlockTypeCount = new Dictionary<string, int>();


        //Grid Stored Materials
        public Dictionary<string, double> StoredMaterials = new Dictionary<string, double>();

    }
}