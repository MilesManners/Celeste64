
namespace Celeste64;

public class Theo : NPC
{
	public const string TALK_FLAG = "THEO";

	public Theo() : base(Assets.Models["theo"])
	{
		Model.Transform = Matrix.CreateScale(3) * Matrix.CreateTranslation(0, 0, -1.5f);
		InteractHoverOffset = new Vec3(0, -2, 16);
		InteractRadius = 32;
		CheckForDialog();
	}

	public override void Interact(Player player)
	{
		World.Add(new Cutscene(Conversation));
	}

	private CoEnumerator Conversation(Cutscene cs)
	{
		yield return Co.Run(cs.MoveToDistance(World.Get<Player>(), Position.XY(), 16));
		yield return Co.Run(cs.FaceEachOther(World.Get<Player>(), this));

		int index = Save.CurrentRecord.GetFlag(TALK_FLAG) + 1;
		if (Game.Instance.ArchipelagoEnabled && Game.Instance.ArchipelagoManager.Friendsanity)
		{
			var lines = Loc.Lines("TheoRando");
			
			var item = Game.Instance.ArchipelagoManager.ScoutLocation($"Theo{index}");
			var itemName = ArchipelagoManager.ItemIDToString[item.Item];
			var player = item.Player;
			var localPlayer = Game.Instance.ArchipelagoManager.Slot;
			var formattedItemName = localPlayer == player ? itemName : $"{Game.Instance.ArchipelagoManager.GetPlayerName(player)}'s {itemName}";
			
			List<Language.Line> newLines = [new (lines[0].Face, string.Format(lines[0].Text, formattedItemName), lines[0].Voice)];
			yield return Co.Run(cs.Say(newLines));
			Save.CurrentRecord.SetFlag($"Theo{index}");
		}
		else
		{
			yield return Co.Run(cs.Say(Loc.Lines($"Theo{index}")));
		}
		Save.CurrentRecord.IncFlag(TALK_FLAG);
		CheckForDialog();
	}

	private void CheckForDialog()
	{ 
		InteractEnabled = Loc.HasLines($"Theo{Save.CurrentRecord.GetFlag(TALK_FLAG) + 1}");
		
		if (Game.Instance.ArchipelagoEnabled && Game.Instance.ArchipelagoManager.Friendsanity && !InteractEnabled)
		{
			Model.MakeMaterialsUnique();
			Model.Flags = ModelFlags.Transparent;	

			foreach (var mat in Model.Materials)
			{
				mat.Texture = Assets.Textures["white"];
				mat.Color = new Color(0x99ddf4) * 0.70f;
			}
		}
	}
}

