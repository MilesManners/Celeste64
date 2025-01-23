
using System;

namespace Celeste64;

public class Signpost : NPC, IHaveModels
{
	public readonly string Conversation;

	public Signpost(string conversation) : base(Assets.Models["sign"])
	{
		Conversation = conversation;
		Model.Transform = 
			Matrix.CreateScale(4) *
			Matrix.CreateTranslation(0, 0, -1.5f);
		InteractHoverOffset = new Vec3(0, 0, 16);
		InteractRadius = 16;
		PushoutRadius = 6;
		CheckInteractable();
	}

    public override void Interact(Player player)
	{
		World.Add(new Cutscene(Talk));
	}

	private CoEnumerator Talk(Cutscene cs)
	{
		yield return Co.Run(cs.Face(World.Get<Player>(), Position));
		
		if (Game.Instance.ArchipelagoManager.Signsanity)
		{
			var lines = Loc.Lines("SignRando");
			
			var item = Game.Instance.ArchipelagoManager.ScoutLocation(Conversation);
			var itemName = ArchipelagoManager.ItemIDToString[item.Item];
			var player = item.Player;
			var localPlayer = Game.Instance.ArchipelagoManager.Slot;
			var formattedItemName = localPlayer == player ? itemName : $"{Game.Instance.ArchipelagoManager.GetPlayerName(player)}'s {itemName}";
			
			List<Language.Line> newLines = [new (lines[0].Face, string.Format(lines[0].Text, formattedItemName), lines[0].Voice)];
			yield return Co.Run(cs.Say(newLines));
			Save.CurrentRecord.SetFlag(Conversation);
			CheckInteractable();
		}
		else
		{
			yield return Co.Run(cs.Say(Loc.Lines(Conversation)));
		}
	}

	private void CheckInteractable()
	{
		if (Game.Instance.ArchipelagoManager.Signsanity && Save.CurrentRecord.GetFlag(Conversation) > 0)
		{
			InteractEnabled = false;
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
