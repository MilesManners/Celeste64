using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Exceptions;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Models;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Archipelago.MultiClient.Net.MessageLog.Messages;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Celeste64;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ArchipelagoConnectionInfo))]
public partial class ArchipelagoConnectionInfoContext : JsonSerializerContext { }
public record ArchipelagoConnectionInfo
{
    public string Url { get; init; } = "wss://archipelago.gg:38281";
    public string SlotName { get; init; } = "Madeline";
    public string Password { get; init; } = "";
}
public struct ArchipelagoMessage
{
    public string Text { get; init; } = "";
    public int RemainingTime { get; set; } = 300;

    public ArchipelagoMessage(string text)
    {
        Text = text;
    }
}

public class ArchipelagoManager(ArchipelagoConnectionInfo connectionInfo)
{
    private static readonly Version SupportedArchipelagoVersion = new(0, 4, 3);

    private readonly ArchipelagoConnectionInfo connectionInfo = connectionInfo;
    private ArchipelagoSession? session;
    private DeathLinkService? deathLinkService;
    private DateTime lastDeath;

    public bool GoalSent = false;

    public DeathLink? DeathLinkData { get; private set; }
    public bool IsDeathLinkSafe { get; set; }
    public bool Ready { get; private set; }
    public List<Tuple<int, NetworkItem>> ItemQueue { get; private set; } = [];
    public List<long> CollectedLocations { get; private set; } = [];
    public Dictionary<long, NetworkItem> LocationDictionary { get; private set; } = new();
    public HashSet<long> SentLocations { get; set; } = [];
    public List<ArchipelagoMessage> MessageLog { get; set; } = [];

    public int Slot => session.ConnectionInfo.Slot;
    public bool DeathLink => session.ConnectionInfo.Tags.Contains("DeathLink");
    public int HintPoints => session.RoomState.HintPoints;
    public int HintCost => session.RoomState.HintCost;
    public Hint[] Hints => session.DataStorage.GetHints();


    public int StrawberriesRequired { get; set; }
    public bool Friendsanity { get; set; }
    public bool Signsanity { get; set; }
    public bool Carsanity { get; set; }
    public bool MoveShuffle { get; set; }
    public int BadelineSource { get; set; }
    public int BadelineFrequency { get; set; }
    public int BadelineSpeed { get; set; }
    public bool BadelinesDisabled { get; set; }
    public int BadelinesDisableTimer = 0;
    public int DeathLinkAmnesty { get; set; }
    public int DeathsCounted;

    public static Dictionary<string, int> LocationStringToId { get; } = new()
    {
        { "1/0",  0xCA0000 },
        { "1/1",  0xCA0001 },
        { "1/2",  0xCA0002 },
        { "1/3",  0xCA0003 },
        { "1/4",  0xCA0004 },
        { "1/5",  0xCA0005 },
        { "1/6",  0xCA0006 },
        { "1/7",  0xCA0007 },
        { "1/8",  0xCA0008 },
        { "1/9",  0xCA0009 },
        { "1/10", 0xCA000A },
        { "1/11", 0xCA000B },
        { "1/12", 0xCA000C },
        { "1/13", 0xCA000D },
        { "1/14", 0xCA000E },
        { "1/15", 0xCA000F },
        { "1/16", 0xCA0010 },
        { "1/17", 0xCA0011 },
        { "1/18", 0xCA0012 },
        { "1/19", 0xCA0013 },
        { "1-1/0", 0xCA0014 },
        { "1-2/0", 0xCA0015 },
        { "1-3/0", 0xCA0016 },
        { "1-4/0", 0xCA0017 },
        { "1-5/0", 0xCA0018 },
        { "1-6/0", 0xCA0019 },
        { "1-7/0", 0xCA001A },
        { "1-8/0", 0xCA001B },
        { "1-9/0", 0xCA001C },
        { "1-10/0", 0xCA001D },

        { "Granny1", 0xCA0100 },
        { "Granny2", 0xCA0101 },
        { "Granny3", 0xCA0102 },
        { "Theo1",   0xCA0103 },
        { "Theo2",   0xCA0104 },
        { "Theo3",   0xCA0105 },
        { "Baddy1",  0xCA0106 },
        { "Baddy2",  0xCA0107 },
        { "Baddy3",  0xCA0108 },

        { "Sign1", 0xCA0200 },
        { "Sign2", 0xCA0201 },
        { "Sign3", 0xCA0202 },
        { "Sign4", 0xCA0203 },
        { "CreditsSign", 0xCA0204 },

        { "Car1", 0xCA0300 },
        { "Car2", 0xCA0301 },
    };

    public static Dictionary<int, string> LocationIdToString { get; } = new()
    {
        { 0xCA0000, "1/0" },
        { 0xCA0001, "1/1" },
        { 0xCA0002, "1/2" },
        { 0xCA0003, "1/3" },
        { 0xCA0004, "1/4" },
        { 0xCA0005, "1/5" },
        { 0xCA0006, "1/6" },
        { 0xCA0007, "1/7" },
        { 0xCA0008, "1/8" },
        { 0xCA0009, "1/9" },
        { 0xCA000A, "1/10" },
        { 0xCA000B, "1/11" },
        { 0xCA000C, "1/12" },
        { 0xCA000D, "1/13" },
        { 0xCA000E, "1/14" },
        { 0xCA000F, "1/15" },
        { 0xCA0010, "1/16" },
        { 0xCA0011, "1/17" },
        { 0xCA0012, "1/18" },
        { 0xCA0013, "1/19" },
        { 0xCA0014, "1-1/0" },
        { 0xCA0015, "1-2/0" },
        { 0xCA0016, "1-3/0" },
        { 0xCA0017, "1-4/0" },
        { 0xCA0018, "1-5/0" },
        { 0xCA0019, "1-6/0" },
        { 0xCA001A, "1-7/0" },
        { 0xCA001B, "1-8/0" },
        { 0xCA001C, "1-9/0" },
        { 0xCA001D, "1-10/0" },

        // Don't need to !collect these
        // { 0xCA0100, "Granny1" },
        // { 0xCA0101, "Granny2" },
        // { 0xCA0102, "Granny3" },
        // { 0xCA0103, "Theo1" },
        // { 0xCA0104, "Theo2" },
        // { 0xCA0105, "Theo3" },
        // { 0xCA0106, "Baddy1" },
        // { 0xCA0107, "Baddy2" },
        // { 0xCA0108, "Baddy3" },

        // { 0xCA0200, "Sign1" },
        // { 0xCA0201, "Sign2" },
        // { 0xCA0202, "Sign3" },
        // { 0xCA0203, "Sign4" },
        // { 0xCA0204, "CreditsSign" },

        // { 0xCA0300, "Car1" },
        // { 0xCA0301, "Car2" },
    };

    public static Dictionary<long, string> ItemIdToString { get; set; } = new()
    {
        { 0xCA0000, "Strawberry" },
        { 0xCA0001, "Dash Refills" },
        { 0xCA0002, "Double Dash Refills" },
        { 0xCA0003, "Feathers" },
        { 0xCA0004, "Coins" },
        { 0xCA0005, "Cassettes" },
        { 0xCA0006, "Traffic Blocks" },
        { 0xCA0007, "Springs" },
        { 0xCA0008, "Breakable Blocks" },
        { 0xCA0009, "Raspberry" },
        { 0xCA000A, "Grounded Dash" },
        { 0xCA000B, "Air Dash" },
        { 0xCA000C, "Skid Jump" },
        { 0xCA000D, "Climb" },
    };

    private const string ApJsonFile = "AP.json";
    private static string? _connectionInfoPath;
    public static string ConnectionInfoPath
    {
        get
        {
	        if (_connectionInfoPath != null) return _connectionInfoPath;
	        
	        string baseFolder = AppContext.BaseDirectory;
            string searchUpPath = "";
            int up = 0;
            while (!File.Exists(Path.Join(baseFolder, searchUpPath, ApJsonFile)) && up++ < 5)
	            searchUpPath = Path.Join(searchUpPath, "..");
            if (!File.Exists(Path.Join(baseFolder, searchUpPath, ApJsonFile)))
	            throw new Exception($"Unable to find {ApJsonFile} File from '{baseFolder}'");
            _connectionInfoPath = Path.Join(baseFolder, searchUpPath, ApJsonFile);

            return _connectionInfoPath;
        }
    }

    public async Task<LoginFailure?> TryConnect()
    {
        lastDeath = DateTime.MinValue;
        session = ArchipelagoSessionFactory.CreateSession(connectionInfo.Url);

        // (Re-)initialize state.
        DeathLinkData = null;
        IsDeathLinkSafe = false;
        Ready = false;
        ItemQueue = [];
        LocationDictionary = new Dictionary<long, NetworkItem>();

        // Watch for the following events.
        session.Socket.ErrorReceived += OnError;
        session.Socket.PacketReceived += OnPacketReceived;
        session.MessageLog.OnMessageReceived += OnMessageReceived;
        session.Items.ItemReceived += OnItemReceived;
        session.Locations.CheckedLocationsUpdated += OnLocationReceived;

        // Attempt to connect to the server.
        try
        {
            await session.ConnectAsync();
        }
        catch (Exception)
        {
            Disconnect();
            return new LoginFailure($"Unable to establish an initial connection to the Archipelago server @ {connectionInfo.Url}");
        }

        var result = await session.LoginAsync(
            "Celeste 64",
            connectionInfo.SlotName,
            ItemsHandlingFlags.AllItems,
            SupportedArchipelagoVersion,
            uuid: Guid.NewGuid().ToString(),
            password: connectionInfo.Password
        );

        if (!result.Successful)
        {
            Disconnect();
            return result as LoginFailure;
        }

        // Load randomizer data.
        StrawberriesRequired = Convert.ToInt32(((LoginSuccessful)result).SlotData["strawberries_required"]);
        Friendsanity = Convert.ToBoolean(((LoginSuccessful)result).SlotData["friendsanity"]);
        Signsanity = Convert.ToBoolean(((LoginSuccessful)result).SlotData["signsanity"]);
        Carsanity = Convert.ToBoolean(((LoginSuccessful)result).SlotData["carsanity"]);
        MoveShuffle = Convert.ToBoolean(((LoginSuccessful)result).SlotData["move_shuffle"]);
        BadelineSource = Convert.ToInt32(((LoginSuccessful)result).SlotData["badeline_chaser_source"]);
        BadelineFrequency = Convert.ToInt32(((LoginSuccessful)result).SlotData["badeline_chaser_frequency"]);
        BadelineSpeed = Convert.ToInt32(((LoginSuccessful)result).SlotData["badeline_chaser_speed"]);
        DeathLinkAmnesty = Convert.ToInt32(((LoginSuccessful)result).SlotData["death_link_amnesty"]);
        bool deathLinkEnabled = Convert.ToBoolean(((LoginSuccessful)result).SlotData["death_link"]);

        // Initialize DeathLink service.
        deathLinkService = session.CreateDeathLinkService();
        deathLinkService.OnDeathLinkReceived += OnDeathLink;
        if (deathLinkEnabled)
        {
            deathLinkService.EnableDeathLink();
        }

        // Build dictionary of locations with item information for fast lookup.
        await BuildLocationDictionary();

        // Return null to signify no error.
        Ready = true;
        return null;
    }
    
    public void Disconnect()
    {
        Ready = false;

        // Clear DeathLink events.
        if (deathLinkService != null)
        {
            deathLinkService.OnDeathLinkReceived -= OnDeathLink;
            deathLinkService = null;
        }

        // Clear events and session object.
        if (session == null) return;
        
        session.Socket.ErrorReceived -= OnError;
        session.Items.ItemReceived -= OnItemReceived;
        session.Locations.CheckedLocationsUpdated -= OnLocationReceived;
        session.Socket.PacketReceived -= OnPacketReceived;
        session.Socket.DisconnectAsync(); // It'll disconnect on its own time.
        session = null;
    }

    public void ClearDeathLink()
    {
        DeathLinkData = null;
        DeathsCounted = 0;
    }

    public void SendDeathLinkIfEnabled(string cause)
    {
        // Do not send any DeathLink messages if it's not enabled.
        if (!DeathLink)
        {
            return;
        }

        DeathsCounted = DeathsCounted + 1;
        if (DeathsCounted < DeathLinkAmnesty)
        {
            return;
        }

        DeathsCounted = 0;

        // Log our current time so we can make sure we ignore our own DeathLink.
        lastDeath = DateTime.Now;
        cause = $"{session.Players.GetPlayerAlias(Slot)} {cause}.";

        try
        {
            deathLinkService.SendDeathLink(new DeathLink(session.Players.GetPlayerAlias(Slot), cause));
        }
        catch (ArchipelagoSocketClosedException)
        {
            // TODO: Send a message to the client that connection has been dropped.
            Disconnect();
        }

        Game.Instance.ArchipelagoManager.ClearDeathLink();
    }

    public void CheckLocations(long[] locations)
    {
        foreach (long locationId in locations)
        {
            SentLocations.Add(locationId);
        }

        try
        {
            session.Locations.CompleteLocationChecks(locations);
        }
        catch (ArchipelagoSocketClosedException)
        {
            // TODO: Send a message to the client that connection has been dropped.
            Disconnect();
        }
    }
    
    public NetworkItem ScoutLocation(string location)
	{
		return LocationDictionary[LocationStringToId[location]];
	}

	public string LocationStringToFormattedName(string location)
	{
		var item = ScoutLocation(location);
		string itemName = ItemIdToString[item.Item];
		return Slot == item.Player ? itemName : $"{GetPlayerName(item.Player)}'s {itemName}";
	}
    
    public void UpdateGameStatus(ArchipelagoClientState state)
    {
        SendPacket(new StatusUpdatePacket { Status = state });
    }

    public string GetPlayerName(int slot)
    {
        if (slot == 0)
        {
            return "Archipelago";
        }

        string? name = session.Players.GetPlayerAlias(slot);
        return string.IsNullOrEmpty(name) ? $"Unknown Player {slot}" : name;
    }

    public string GetLocationName(long location)
    {
        string? name = session?.Locations.GetLocationNameFromId(location);
        return string.IsNullOrEmpty(name) ? $"Unknown Location {location}" : name;
    }

    public string GetItemName(long item)
    {
        string? name = session?.Items.GetItemName(item);
        return string.IsNullOrEmpty(name) ? $"Unknown Item {item}" : name;
    }

    public void EnableDeathLink()
    {
        deathLinkService.EnableDeathLink();
    }

    public void DisableDeathLink()
    {
        deathLinkService.DisableDeathLink();
    }

    public bool IsLocationChecked(long id)
    {
        // Verify location exists first, we'll treat locations that don't exist as already checked.
        return !session.Locations.AllLocations.Contains(id) || session.Locations.AllLocationsChecked.Contains(id);
    }

    public int LocationsCheckedCount()
    {
        return session.Locations.AllLocationsChecked.Count;
    }

    private void SendPacket(ArchipelagoPacketBase packet)
    {
        try
        {
            session.Socket.SendPacket(packet);
        }
        catch (ArchipelagoSocketClosedException)
        {
            // TODO: Send a message to the client that connection has been dropped.
            Disconnect();
        }
    }

    private void OnItemReceived(ReceivedItemsHelper helper)
    {
        int i = helper.Index;
        while (helper.Any())
            ItemQueue.Add(new Tuple<int, NetworkItem>(i++, helper.DequeueItem()));
    }

    private void OnLocationReceived(ReadOnlyCollection<long> newCheckedLocations)
    {
        foreach (long newLoc in newCheckedLocations)
            CollectedLocations.Add(newLoc);
    }

    private void OnDeathLink(DeathLink deathLink)
    {
        // If we receive a DeathLink that is after our last death, let's set it.
        if (!IsDeathLinkSafe && DateTime.Compare(deathLink.Timestamp, lastDeath) > 0)
        {
            DeathLinkData = deathLink;
        }
    }

    private async Task BuildLocationDictionary()
    {
        var locations = await session.Locations.ScoutLocationsAsync(false, session.Locations.AllLocations.ToArray());

        foreach (var item in locations.Locations)
        {
            LocationDictionary[item.Location] = item;
        }
    }

    private void OnMessageReceived(LogMessage message)
    {
        switch (message)
        {
            case ItemSendLogMessage itemSendMessage:
	            if (itemSendMessage is { IsRelatedToActivePlayer: true, IsReceiverTheActivePlayer: false })
                    MessageLog.Add(new ArchipelagoMessage(itemSendMessage.ToString()));
	            
                break;
        }
    }

    private void OnPacketReceived(ArchipelagoPacketBase packet)
    {

    }

    private static void OnError(Exception exception, string message)
    {

    }

    public void CheckReceivedItemQueue()
    {
        int audioGuard = 0;
        for (int index = Save.CurrentRecord.GetFlag("ItemRcv"); index < ItemQueue.Count; index++)
        {
            var item = ItemQueue[index].Item2;

            if (audioGuard < 3)
            {
                audioGuard++;
                Audio.Play(Sfx.sfx_secret);
            }

            Log.Info($"Received {ItemIdToString[item.Item]} from {GetPlayerName(item.Player)}.");
            MessageLog.Add(new ArchipelagoMessage($"Received {ItemIdToString[item.Item]} from {GetPlayerName(item.Player)}."));

            switch (item.Item)
            {
	            case 0xCA0000:
		            Save.CurrentRecord.IncFlag("Strawberries");
		            break;
	            case 0xCA0001:
		            Save.CurrentRecord.SetFlag("DashRefill");
		            break;
	            case 0xCA0002:
		            Save.CurrentRecord.SetFlag("DoubleDashRefill");
		            break;
	            case 0xCA0003:
		            Save.CurrentRecord.SetFlag("Feather");
		            break;
	            case 0xCA0004:
		            Save.CurrentRecord.SetFlag("Coin");
		            break;
	            case 0xCA0005:
		            Save.CurrentRecord.SetFlag("Cassette");
		            break;
	            case 0xCA0006:
		            Save.CurrentRecord.SetFlag("TrafficBlock");
		            break;
	            case 0xCA0007:
		            Save.CurrentRecord.SetFlag("Spring");
		            break;
	            case 0xCA0008:
		            Save.CurrentRecord.SetFlag("Breakables");
		            break;
	            case 0xCA000A:
		            Save.CurrentRecord.SetFlag("Grounded Dash");
		            break;
	            case 0xCA000B:
		            Save.CurrentRecord.SetFlag("Air Dash");
		            break;
	            case 0xCA000C:
		            Save.CurrentRecord.SetFlag("Skid Jump");
		            break;
	            case 0xCA000D:
		            Save.CurrentRecord.SetFlag("Climb");
		            break;
            }

            Save.CurrentRecord.SetFlag("ItemRcv", index + 1);
        }
    }

    public void CheckLocationsToSend()
    {
        var locationsToCheck = new List<long>();
        foreach (string strawbId in Save.CurrentRecord.Strawberries)
        {
            if (LocationStringToId.TryGetValue(strawbId, out int value))
            {
                long locationId = value;
                if (!SentLocations.Contains(locationId))
                    locationsToCheck.Add(locationId);
            }
            else
                Log.Info($"Untracked Strawberry: {strawbId}");
        }

        locationsToCheck.AddRange((from nameIdPair in LocationStringToId
	        where !Save.CurrentRecord.Strawberries.Contains(nameIdPair.Key)
	        where Save.CurrentRecord.GetFlag(nameIdPair.Key) > 0
	        select nameIdPair.Value
	        into locationId
	        where !SentLocations.Contains(locationId)
	        select locationId).Select(locationId => (long)locationId));

        CheckLocations(locationsToCheck.ToArray());
    }

    public void HandleCollectedLocations()
    {
        // Change this if we need to !collect non-Strawberry locations
        foreach (long newLoc in CollectedLocations)
        {
	        if (!LocationIdToString.ContainsKey((int)newLoc)) continue;
	        
	        string strawberryLocId = LocationIdToString[(int)newLoc];
	        Save.CurrentRecord.Strawberries.Add(strawberryLocId);

	        if (!strawberryLocId.Contains('-')) continue;
				
	        string subMapId = strawberryLocId.Split("/")[0];
	        Save.CurrentRecord.CompletedSubMaps.Add(subMapId);
        }
    }

    public void HandleMessageQueue(Batcher batch, SpriteFont font, Rect bounds)
    {
        for (int i = Math.Min(Math.Max(4, MessageLog.Count - 1), 4); i >= 0; i--)
        {
	        if (MessageLog.Count <= i) continue;
	        
	        batch.Text(font, Game.Instance.ArchipelagoManager.MessageLog[i].Text, bounds.BottomLeft, new Vec2(0, 5 - i), new Foster.Framework.Color(0xF5, 0x42, 0xC8, 0xFF));
	        var updatedMessage = Game.Instance.ArchipelagoManager.MessageLog[i];
	        updatedMessage.RemainingTime -= 1;
	        if (updatedMessage.RemainingTime <= 0)
		        Game.Instance.ArchipelagoManager.MessageLog.RemoveAt(i);
	        else
		        Game.Instance.ArchipelagoManager.MessageLog[i] = updatedMessage;
        }
    }
}
