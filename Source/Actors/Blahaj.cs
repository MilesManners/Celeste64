namespace Celeste64;

public class Blahaj : Collectable
{
    public Blahaj(string id, bool isLocked, string? unlockCondition, bool unlockSound, Vec3? bubbleTo) : base(id,
        isLocked, unlockCondition, unlockSound, bubbleTo, new SkinnedModel(Assets.Models["blahaj"]))
    {
    }
}