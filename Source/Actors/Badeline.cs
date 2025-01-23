
namespace Celeste64;

public class Badeline : NPC
{
	public const string TALK_FLAG = "BADELINE";

	private readonly Hair hair;
	private Color hairColor = 0x9B3FB5;

	public Badeline() : base(Assets.Models["badeline"])
	{
		Model.Play("Bad.Idle");

		foreach (var mat in Model.Materials)
		{
			if (mat.Name == "Hair")
			{
				mat.Color = hairColor;
				mat.Effects = 0;
			}
            mat.SilhouetteColor = hairColor;
		}

        hair = new()
        {
            Color = hairColor,
			ForwardOffsetPerNode = 0,
            Nodes = 10
        };

        InteractHoverOffset = new Vec3(0, -2, 16);
		InteractRadius = 32;
		CheckForDialog();
	}

    public override void Update()
    {
        base.Update();
		
		// update model
		Model.Transform = 
			Matrix.CreateScale(3) * 
			Matrix.CreateTranslation(0, 0, MathF.Sin(World.GeneralTimer * 2) * 1.0f - 1.5f);

		// update hair
		{
			var hairMatrix = Matrix.Identity;
			foreach (var it in Model.Instance.Armature.LogicalNodes)
				if (it.Name == "Head")
					hairMatrix = it.ModelMatrix * SkinnedModel.BaseTranslation * Model.Transform * Matrix;
			hair.Flags = Model.Flags;
			hair.Forward = -new Vec3(Facing, 0);
			hair.Materials[0].Effects = 0;
			hair.Update(hairMatrix);
		}
		
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
			var lines = Loc.Lines("BaddyRando");
			
			var item = Game.Instance.ArchipelagoManager.ScoutLocation($"Baddy{index}");
			var itemName = ArchipelagoManager.ItemIDToString[item.Item];
			var player = item.Player;
			var localPlayer = Game.Instance.ArchipelagoManager.Slot;
			var formattedItemName = localPlayer == player ? itemName : $"{Game.Instance.ArchipelagoManager.GetPlayerName(player)}'s {itemName}";
			
			List<Language.Line> newLines = [new (lines[0].Face, string.Format(lines[0].Text, formattedItemName), lines[0].Voice)];
			yield return Co.Run(cs.Say(newLines));
			Save.CurrentRecord.SetFlag($"Baddy{index}");
		}
		else
		{
			yield return Co.Run(cs.Say(Loc.Lines($"Baddy{index}")));
		}
		Save.CurrentRecord.IncFlag(TALK_FLAG);
		CheckForDialog();
	}

    public override void CollectModels(List<(Actor Actor, Model Model)> populate)
    {
		populate.Add((this, hair));
        base.CollectModels(populate);
    }

	private void CheckForDialog()
	{ 
		InteractEnabled = Loc.HasLines($"Baddy{Save.CurrentRecord.GetFlag(TALK_FLAG) + 1}");
		
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

