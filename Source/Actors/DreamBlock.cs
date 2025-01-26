namespace Celeste64;

public class DreamBlock : Solid
{
	public virtual bool BouncesPlayer { get; set; }

	public DreamBlock()
	{
		Model.Flags = ModelFlags.Transparent;
	}

	public virtual void HandleDash(Vec3 velocity)
	{
		// Audio.Play(Sfx.sfx_breakable_wall_wood, Position);
	}
}
