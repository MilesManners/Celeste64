namespace Celeste64;

public class DreamBlock : Solid, IDashTrigger
{
	public static readonly string[] GlassShards = ["shard_0", "shard_1", "shard_2"];
	public static readonly string[] WoodShards = ["wood_shard_0", "wood_shard_1", "wood_shard_2"];

	public virtual bool BouncesPlayer { get; set; }

	public DreamBlock()
	{
	}

	public virtual void HandleDash(Vec3 velocity)
	{
		var size = LocalBounds.Size;
		var amount = (size.X * size.Y * size.Z) / 200;
		var options = (Transparent ? GlassShards : WoodShards);

		if (Transparent)
			Audio.Play(Sfx.sfx_glassbreak, Position);
		else
			Audio.Play(Sfx.sfx_breakable_wall_wood, Position);

		for (int i = 0; i < amount; i++)
		{
			var offset = new Vec3(World.Rng.Float(size.X), World.Rng.Float(size.Y), World.Rng.Float(size.Z));
			var at = Vec3.Transform(offset - size / 2, Matrix);
			velocity = velocity.Normalized() * World.Rng.Float(100, 400);
			World.Request<Debris>().Init(at, velocity, options[World.Rng.Int(options.Length)]);
		}

		World.Destroy(this);
	}
}
