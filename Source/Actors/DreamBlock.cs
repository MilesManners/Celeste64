namespace Celeste64;

public class DreamBlock : Solid, IDashTrigger
{
	public virtual bool BouncesPlayer { get; set; }

	public DreamBlock()
	{
	}

	public virtual void HandleDash(Vec3 velocity)
	{
		// Audio.Play(Sfx.sfx_breakable_wall_wood, Position);
	}
}
