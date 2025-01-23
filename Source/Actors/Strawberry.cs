namespace Celeste64;

public class Strawberry : Collectable
{
    public Strawberry(string id, bool isLocked, string? unlockCondition, bool unlockSound, Vec3? bubbleTo) : base(id,
        isLocked, unlockCondition, unlockSound, bubbleTo, new SkinnedModel(Assets.Models["strawberry"]))
    {
    }
}